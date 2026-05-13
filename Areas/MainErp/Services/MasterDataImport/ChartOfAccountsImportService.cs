using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using MyERP.Areas.MainErp.Interfaces;
using MyERP.Areas.MainErp.ViewModels.MasterDataImport;

namespace MyERP.Areas.MainErp.Services.MasterDataImport
{
    public class ChartOfAccountsImportService
    {
        private readonly IMainErpDbConnectionFactory _connectionFactory;

        public ChartOfAccountsImportService(IMainErpDbConnectionFactory connectionFactory)
        {
            if (connectionFactory == null)
            {
                throw new ArgumentNullException("connectionFactory");
            }

            _connectionFactory = connectionFactory;
        }

        public IList<MasterDataImportRowViewModel> Validate(IList<MasterDataImportRowViewModel> rows)
        {
            using (var connection = _connectionFactory.CreateOpenConnection())
            {
                return Validate(connection, null, rows);
            }
        }

        public MasterDataImportResultViewModel Import(MasterDataImportPreview preview, string userName, bool stopOnAnyError)
        {
            using (var connection = _connectionFactory.CreateOpenConnection())
            {
                var rows = Validate(connection, null, preview.Rows);
                var validRows = rows.Where(r => r.IsValid).OrderBy(r => r.Level ?? CountLevel(r.AccountCode)).ThenBy(r => r.RowNumber).ToList();

                if (stopOnAnyError && rows.Any(r => !r.IsValid))
                {
                    var stoppedBatchId = CreateBatch(connection, null, preview.FileName, userName, rows.Count, 0, rows.Count(r => !r.IsValid));
                    CompleteBatch(connection, null, stoppedBatchId, 0, rows.Count(r => !r.IsValid), "Import stopped because validation errors exist.");
                    return new MasterDataImportResultViewModel
                    {
                        BatchId = stoppedBatchId,
                        TotalRows = rows.Count,
                        SuccessRows = 0,
                        FailedRows = rows.Count(r => !r.IsValid),
                        Message = "Import stopped because validation errors exist."
                    };
                }

                var batchId = CreateBatch(connection, null, preview.FileName, userName, rows.Count, validRows.Count, rows.Count - validRows.Count);
                using (var transaction = connection.BeginTransaction(IsolationLevel.Serializable))
                {
                    var importedByRow = new Dictionary<int, string>();

                    try
                    {
                        foreach (var row in validRows)
                        {
                            if (IsRowParentReference(row.ResolvedParentAccountCode))
                            {
                                var parentRowNumber = Convert.ToInt32(row.ResolvedParentAccountCode.Substring(6));
                                if (!importedByRow.ContainsKey(parentRowNumber))
                                {
                                    throw new InvalidOperationException("Parent row " + parentRowNumber + " was not imported before row " + row.RowNumber + ".");
                                }

                                row.ResolvedParentAccountCode = importedByRow[parentRowNumber];
                            }

                            var accountCode = InsertAccount(connection, transaction, row);
                            row.ImportedAccountCode = accountCode;
                            importedByRow[row.RowNumber] = accountCode;
                            MarkParentAsGroup(connection, transaction, row.ResolvedParentAccountCode);
                        }

                        transaction.Commit();
                        CompleteBatch(connection, null, batchId, validRows.Count, rows.Count - validRows.Count, null);

                        return new MasterDataImportResultViewModel
                        {
                            BatchId = batchId,
                            TotalRows = rows.Count,
                            SuccessRows = validRows.Count,
                            FailedRows = rows.Count - validRows.Count,
                            Message = "Import completed successfully."
                        };
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        CompleteBatch(connection, null, batchId, 0, rows.Count, ex.Message);
                        throw;
                    }
                }
            }
        }

        private IList<MasterDataImportRowViewModel> Validate(SqlConnection connection, SqlTransaction transaction, IList<MasterDataImportRowViewModel> rows)
        {
            var byCode = rows.Where(r => !string.IsNullOrWhiteSpace(r.AccountCode)).GroupBy(r => r.AccountCode.Trim(), StringComparer.OrdinalIgnoreCase).ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);
            var bySerial = rows.Where(r => !string.IsNullOrWhiteSpace(r.AccountSerial)).GroupBy(r => r.AccountSerial.Trim(), StringComparer.OrdinalIgnoreCase).ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

            foreach (var row in rows)
            {
                row.Errors.Clear();
                row.AccountSerial = Clean(row.AccountSerial);
                row.AccountCode = Clean(row.AccountCode);
                row.AccountName = Clean(row.AccountName);
                row.AccountNameEnglish = Clean(row.AccountNameEnglish);
                row.ParentAccountCode = Clean(row.ParentAccountCode);
                row.ParentAccountSerial = Clean(row.ParentAccountSerial);
                row.CurrencyCode = string.IsNullOrWhiteSpace(row.CurrencyCode) ? "1" : Clean(row.CurrencyCode);

                if (string.IsNullOrWhiteSpace(row.AccountName))
                {
                    row.Errors.Add("Account name is required.");
                }

                if (string.IsNullOrWhiteSpace(row.AccountSerial) && string.IsNullOrWhiteSpace(row.AccountCode))
                {
                    row.Errors.Add("Account serial or technical account code is required.");
                }

                List<MasterDataImportRowViewModel> duplicateCodeRows;
                if (!string.IsNullOrWhiteSpace(row.AccountCode) && byCode.TryGetValue(row.AccountCode, out duplicateCodeRows) && duplicateCodeRows.Count > 1)
                {
                    row.Errors.Add("Duplicate Account_Code inside the Excel file.");
                }

                List<MasterDataImportRowViewModel> duplicateSerialRows;
                if (!string.IsNullOrWhiteSpace(row.AccountSerial) && bySerial.TryGetValue(row.AccountSerial, out duplicateSerialRows) && duplicateSerialRows.Count > 1)
                {
                    row.Errors.Add("Duplicate Account_Serial inside the Excel file.");
                }

                if (!string.IsNullOrWhiteSpace(row.AccountCode) && Exists(connection, transaction, "Account_Code", row.AccountCode))
                {
                    row.Errors.Add("Account_Code already exists in ACCOUNTS.");
                }

                if (!string.IsNullOrWhiteSpace(row.AccountSerial) && Exists(connection, transaction, "Account_Serial", row.AccountSerial))
                {
                    row.Errors.Add("Account_Serial already exists in ACCOUNTS.");
                }

                row.ResolvedParentAccountCode = ResolveParent(connection, transaction, row, byCode, bySerial);
                if (string.IsNullOrWhiteSpace(row.ResolvedParentAccountCode))
                {
                    row.Errors.Add("Parent account was not found in database or current Excel file.");
                }
                else if (IsFinalAccount(connection, transaction, row.ResolvedParentAccountCode) && !ParentWillReceiveChild(row.ResolvedParentAccountCode, rows))
                {
                    row.Errors.Add("Cannot import below an existing final account.");
                }

                var parentValues = GetParentProperties(connection, transaction, row.ResolvedParentAccountCode);
                row.AccountTypes = row.AccountTypes ?? parentValues.AccountTypes;
                row.AccountTab = row.AccountTab ?? parentValues.AccountTab;
                row.DebitOrCredit = row.DebitOrCredit ?? parentValues.DebitOrCredit;
                row.DifferentType = row.DifferentType ?? parentValues.DifferentType;
                row.Authority = row.Authority ?? parentValues.Authority;
                row.Level = row.Level ?? CountLevel(!string.IsNullOrEmpty(row.AccountCode) ? row.AccountCode : row.ResolvedParentAccountCode + "a1");
            }

            return rows;
        }

        private static bool ParentWillReceiveChild(string parentCode, IEnumerable<MasterDataImportRowViewModel> rows)
        {
            return rows.Any(r => string.Equals(r.AccountCode, parentCode, StringComparison.OrdinalIgnoreCase));
        }

        private string ResolveParent(SqlConnection connection, SqlTransaction transaction, MasterDataImportRowViewModel row, IDictionary<string, List<MasterDataImportRowViewModel>> byCode, IDictionary<string, List<MasterDataImportRowViewModel>> bySerial)
        {
            if (string.IsNullOrWhiteSpace(row.ParentAccountCode) && string.IsNullOrWhiteSpace(row.ParentAccountSerial))
            {
                return "r";
            }

            if (!string.IsNullOrWhiteSpace(row.ParentAccountCode))
            {
                if (Exists(connection, transaction, "Account_Code", row.ParentAccountCode) || byCode.ContainsKey(row.ParentAccountCode))
                {
                    return row.ParentAccountCode;
                }
            }

            if (!string.IsNullOrWhiteSpace(row.ParentAccountSerial))
            {
                var dbCode = LookupScalar(connection, transaction, "SELECT TOP 1 Account_Code FROM ACCOUNTS WHERE Account_Serial=@value", row.ParentAccountSerial);
                if (!string.IsNullOrWhiteSpace(dbCode))
                {
                    return dbCode;
                }

                List<MasterDataImportRowViewModel> parentRows;
                if (bySerial.TryGetValue(row.ParentAccountSerial, out parentRows) && parentRows.Count == 1)
                {
                    return !string.IsNullOrWhiteSpace(parentRows[0].AccountCode) ? parentRows[0].AccountCode : "__row:" + parentRows[0].RowNumber;
                }
            }

            return string.Empty;
        }

        private string InsertAccount(SqlConnection connection, SqlTransaction transaction, MasterDataImportRowViewModel row)
        {
            var accountCode = string.IsNullOrWhiteSpace(row.AccountCode) ? GenerateAccountCode(connection, transaction, row.ResolvedParentAccountCode) : row.AccountCode;
            var accountSerial = string.IsNullOrWhiteSpace(row.AccountSerial) ? GenerateAccountSerial(connection, transaction, row.ResolvedParentAccountCode, accountCode, row.IsFinalAccount) : row.AccountSerial;
            var accountNameEng = string.IsNullOrWhiteSpace(row.AccountNameEnglish) ? row.AccountName : row.AccountNameEnglish;

            using (var command = new SqlCommand(@"INSERT INTO ACCOUNTS
(AccountTypes, AccountTab, DepitOrCredit, Differenttype, Authority, [Block], Account_Code, Account_Name, Parent_Account_Code,
 last_account, cannot_del, Branch, Account_Serial, BasicAccount, DateCreated, Account_NameEng, currenct_code, mowazna,
 cost_center, Sum_account, cost_center_type, cost_center_id, ActivityTypeId)
VALUES
(@AccountTypes, @AccountTab, @DepitOrCredit, @Differenttype, @Authority, 0, @Account_Code, @Account_Name, @Parent_Account_Code,
 @last_account, 0, '0', @Account_Serial, 0, GETDATE(), @Account_NameEng, @currenct_code, 0,
 0, 0, 0, NULL, 0)", connection, transaction))
            {
                command.Parameters.AddWithValue("@AccountTypes", row.AccountTypes ?? 0);
                command.Parameters.AddWithValue("@AccountTab", row.AccountTab ?? 0);
                command.Parameters.AddWithValue("@DepitOrCredit", row.DebitOrCredit ?? 0);
                command.Parameters.AddWithValue("@Differenttype", row.DifferentType ?? 0);
                command.Parameters.AddWithValue("@Authority", row.Authority ?? 0);
                command.Parameters.AddWithValue("@Account_Code", accountCode);
                command.Parameters.AddWithValue("@Account_Name", row.AccountName);
                command.Parameters.AddWithValue("@Parent_Account_Code", row.ResolvedParentAccountCode);
                command.Parameters.AddWithValue("@last_account", row.IsFinalAccount);
                command.Parameters.AddWithValue("@Account_Serial", accountSerial);
                command.Parameters.AddWithValue("@Account_NameEng", accountNameEng);
                command.Parameters.AddWithValue("@currenct_code", row.CurrencyCode);
                command.ExecuteNonQuery();
            }

            return accountCode;
        }

        private static void MarkParentAsGroup(SqlConnection connection, SqlTransaction transaction, string parentCode)
        {
            if (string.IsNullOrWhiteSpace(parentCode) || parentCode == "-1")
            {
                return;
            }

            using (var command = new SqlCommand("UPDATE ACCOUNTS SET last_account = 0 WHERE Account_Code = @code AND last_account = 1", connection, transaction))
            {
                command.Parameters.AddWithValue("@code", parentCode);
                command.ExecuteNonQuery();
            }
        }

        private string GenerateAccountCode(SqlConnection connection, SqlTransaction transaction, string parentCode)
        {
            var max = 0;
            using (var command = new SqlCommand("SELECT Account_Code FROM ACCOUNTS WHERE Parent_Account_Code=@parent", connection, transaction))
            {
                command.Parameters.AddWithValue("@parent", parentCode);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        max = Math.Max(max, ExtractSuffix(Convert.ToString(reader["Account_Code"])));
                    }
                }
            }

            string candidate;
            do
            {
                max++;
                candidate = parentCode == "r" ? "a" + max : parentCode + "a" + max;
            } while (Exists(connection, transaction, "Account_Code", candidate));

            return candidate;
        }

        private string GenerateAccountSerial(SqlConnection connection, SqlTransaction transaction, string parentCode, string accountCode, bool isFinal)
        {
            var parentSerial = LookupScalar(connection, transaction, "SELECT TOP 1 Account_Serial FROM ACCOUNTS WHERE Account_Code=@value", parentCode);
            if (string.IsNullOrWhiteSpace(parentSerial))
            {
                return accountCode.Replace("a", string.Empty);
            }

            var level = CountLevel(accountCode);
            var digits = GetAccountLevelDigits(connection, transaction, level);
            var max = 0;
            using (var command = new SqlCommand("SELECT Account_Serial FROM ACCOUNTS WHERE Account_Serial LIKE @serial + '%' AND (LEN(Account_Code)-LEN(REPLACE(Account_Code,'a',''))) = @level", connection, transaction))
            {
                command.Parameters.AddWithValue("@serial", parentSerial);
                command.Parameters.AddWithValue("@level", level);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var serial = Convert.ToString(reader["Account_Serial"]);
                        if (serial.StartsWith(parentSerial, StringComparison.OrdinalIgnoreCase))
                        {
                            int value;
                            if (int.TryParse(serial.Substring(parentSerial.Length), out value))
                            {
                                max = Math.Max(max, value);
                            }
                        }
                    }
                }
            }

            return parentSerial + (max + 1).ToString(new string('0', Math.Max(1, digits)));
        }

        private int GetAccountLevelDigits(SqlConnection connection, SqlTransaction transaction, int level)
        {
            var value = LookupScalar(connection, transaction, "SELECT TOP 1 NoOfDigits FROM AccountsLevelsDetails WHERE [Level]=@value", level.ToString());
            int digits;
            return int.TryParse(value, out digits) && digits > 0 ? digits : 1;
        }

        private bool Exists(SqlConnection connection, SqlTransaction transaction, string fieldName, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            return !string.IsNullOrWhiteSpace(LookupScalar(connection, transaction, "SELECT TOP 1 " + fieldName + " FROM ACCOUNTS WHERE " + fieldName + "=@value", value));
        }

        private bool IsFinalAccount(SqlConnection connection, SqlTransaction transaction, string accountCode)
        {
            if (IsRowParentReference(accountCode))
            {
                return false;
            }

            var value = LookupScalar(connection, transaction, "SELECT TOP 1 last_account FROM ACCOUNTS WHERE Account_Code=@value", accountCode);
            return value == "True" || value == "1";
        }

        private ParentProperties GetParentProperties(SqlConnection connection, SqlTransaction transaction, string parentCode)
        {
            if (string.IsNullOrWhiteSpace(parentCode) || parentCode == "-1" || IsRowParentReference(parentCode))
            {
                return new ParentProperties();
            }

            using (var command = new SqlCommand("SELECT TOP 1 AccountTypes, AccountTab, DepitOrCredit, Differenttype, Authority FROM ACCOUNTS WHERE Account_Code=@code", connection, transaction))
            {
                command.Parameters.AddWithValue("@code", parentCode);
                using (var reader = command.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        return new ParentProperties();
                    }

                    return new ParentProperties
                    {
                        AccountTypes = ReadNullableInt(reader, "AccountTypes"),
                        AccountTab = ReadNullableInt(reader, "AccountTab"),
                        DebitOrCredit = ReadNullableInt(reader, "DepitOrCredit"),
                        DifferentType = ReadNullableInt(reader, "Differenttype"),
                        Authority = ReadNullableInt(reader, "Authority")
                    };
                }
            }
        }

        private static int? ReadNullableInt(IDataRecord reader, string name)
        {
            return reader[name] == DBNull.Value ? (int?)null : Convert.ToInt32(reader[name]);
        }

        private static string LookupScalar(SqlConnection connection, SqlTransaction transaction, string sql, string value)
        {
            using (var command = new SqlCommand(sql, connection, transaction))
            {
                command.Parameters.AddWithValue("@value", value ?? string.Empty);
                var result = command.ExecuteScalar();
                return result == null || result == DBNull.Value ? string.Empty : Convert.ToString(result);
            }
        }

        private static int ExtractSuffix(string accountCode)
        {
            if (string.IsNullOrWhiteSpace(accountCode))
            {
                return 0;
            }

            var index = accountCode.LastIndexOf('a');
            int suffix;
            return index >= 0 && int.TryParse(accountCode.Substring(index + 1), out suffix) ? suffix : 0;
        }

        private static int CountLevel(string accountCode)
        {
            return string.IsNullOrEmpty(accountCode) ? 0 : accountCode.Count(c => c == 'a');
        }

        private static string Clean(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        private static bool IsRowParentReference(string value)
        {
            return !string.IsNullOrWhiteSpace(value) && value.StartsWith("__row:", StringComparison.OrdinalIgnoreCase);
        }

        private int CreateBatch(SqlConnection connection, SqlTransaction transaction, string fileName, string userName, int totalRows, int successRows, int failedRows)
        {
            using (var command = new SqlCommand(@"INSERT INTO dbo.MasterDataImportBatch
(FileName, EntityType, ImportedBy, ImportStartedAt, TotalRows, SuccessRows, FailedRows, Status)
VALUES (@FileName, 'ChartOfAccounts', @ImportedBy, GETDATE(), @TotalRows, @SuccessRows, @FailedRows, 'Running');
SELECT CAST(SCOPE_IDENTITY() AS int);", connection, transaction))
            {
                command.Parameters.AddWithValue("@FileName", fileName ?? string.Empty);
                command.Parameters.AddWithValue("@ImportedBy", userName ?? string.Empty);
                command.Parameters.AddWithValue("@TotalRows", totalRows);
                command.Parameters.AddWithValue("@SuccessRows", successRows);
                command.Parameters.AddWithValue("@FailedRows", failedRows);
                return Convert.ToInt32(command.ExecuteScalar());
            }
        }

        private void CompleteBatch(SqlConnection connection, SqlTransaction transaction, int batchId, int successRows, int failedRows, string error)
        {
            using (var command = new SqlCommand(@"UPDATE dbo.MasterDataImportBatch
SET ImportFinishedAt=GETDATE(), SuccessRows=@SuccessRows, FailedRows=@FailedRows, Status=@Status, ErrorMessage=@ErrorMessage
WHERE BatchId=@BatchId", connection, transaction))
            {
                command.Parameters.AddWithValue("@BatchId", batchId);
                command.Parameters.AddWithValue("@SuccessRows", successRows);
                command.Parameters.AddWithValue("@FailedRows", failedRows);
                command.Parameters.AddWithValue("@Status", string.IsNullOrWhiteSpace(error) ? "Completed" : "Failed");
                command.Parameters.AddWithValue("@ErrorMessage", (object)error ?? DBNull.Value);
                command.ExecuteNonQuery();
            }
        }

        private class ParentProperties
        {
            public int? AccountTypes { get; set; }
            public int? AccountTab { get; set; }
            public int? DebitOrCredit { get; set; }
            public int? DifferentType { get; set; }
            public int? Authority { get; set; }
        }
    }
}
