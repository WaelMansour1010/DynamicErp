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
        private int _activeBatchId;

        public ChartOfAccountsImportService(IMainErpDbConnectionFactory connectionFactory)
        {
            if (connectionFactory == null)
            {
                throw new ArgumentNullException("connectionFactory");
            }

            _connectionFactory = connectionFactory;
        }

        public IList<MasterDataImportRowViewModel> Validate(IList<MasterDataImportRowViewModel> rows, string importMode)
        {
            using (var connection = _connectionFactory.CreateOpenConnection())
            {
                return Validate(connection, null, rows, MasterDataImportMode.IsReplace(importMode));
            }
        }

        public MasterDataImportResultViewModel Import(MasterDataImportPreview preview, string userName, bool stopOnAnyError, string importMode)
        {
            using (var connection = _connectionFactory.CreateOpenConnection())
            {
                var replaceExisting = MasterDataImportMode.IsReplace(importMode);
                var rows = Validate(connection, null, preview.Rows, replaceExisting);
                var validRows = rows.Where(r => r.IsValid).OrderBy(r => r.Level ?? CountLevel(r.AccountCode)).ThenBy(r => r.RowNumber).ToList();

                if (stopOnAnyError && rows.Any(r => !r.IsValid))
                {
                    var validationSummary = BuildValidationSummary(rows);
                    var stoppedBatchId = CreateBatch(connection, null, preview.FileName, userName, rows.Count, 0, rows.Count(r => !r.IsValid));
                    CompleteBatch(connection, null, stoppedBatchId, 0, rows.Count(r => !r.IsValid), validationSummary);
                    return new MasterDataImportResultViewModel
                    {
                        BatchId = stoppedBatchId,
                        TotalRows = rows.Count,
                        SuccessRows = 0,
                        FailedRows = rows.Count(r => !r.IsValid),
                        Message = validationSummary
                    };
                }

                var batchId = CreateBatch(connection, null, preview.FileName, userName, rows.Count, validRows.Count, rows.Count - validRows.Count);
                using (var transaction = connection.BeginTransaction(IsolationLevel.Serializable))
                {
                    var importedByRow = new Dictionary<int, string>();

                    try
                    {
                        _activeBatchId = batchId;
                        if (replaceExisting)
                        {
                            ClearAccounts(connection, transaction);
                        }

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

                            var accountCode = replaceExisting ? InsertAccount(connection, transaction, row) : UpsertAccount(connection, transaction, row);
                            row.ImportedAccountCode = accountCode;
                            importedByRow[row.RowNumber] = accountCode;
                            MarkParentAsGroup(connection, transaction, row.ResolvedParentAccountCode);
                            EnsureOperationalMasterRecords(connection, transaction, row, accountCode);
                        }

                        transaction.Commit();
                        CompleteBatch(connection, null, batchId, validRows.Count, rows.Count - validRows.Count, null);

                        return new MasterDataImportResultViewModel
                        {
                            BatchId = batchId,
                            TotalRows = rows.Count,
                            SuccessRows = validRows.Count,
                            FailedRows = rows.Count - validRows.Count,
                            Message = replaceExisting ? "Chart of accounts was replaced successfully." : "Chart of accounts was merged successfully."
                        };
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        CompleteBatch(connection, null, batchId, 0, rows.Count, ex.Message);
                        throw;
                    }
                    finally
                    {
                        _activeBatchId = 0;
                    }
                }
            }
        }

        public IList<MasterDataImportBatchViewModel> GetRecentBatches(int take)
        {
            using (var connection = _connectionFactory.CreateOpenConnection())
            {
                EnsureBatchTable(connection, null);
                using (var command = new SqlCommand(@"
SELECT TOP (@Take)
    b.BatchId,
    b.FileName,
    b.EntityType,
    b.ImportStartedAt,
    b.SuccessRows,
    b.FailedRows,
    b.Status,
    CreatedAccounts = (
        SELECT COUNT(1)
        FROM dbo.MasterDataImportBatchDetail d
        WHERE d.BatchId = b.BatchId AND d.TableName = N'ACCOUNTS' AND d.ActionType = N'Created'
    )
FROM dbo.MasterDataImportBatch b
ORDER BY b.BatchId DESC;", connection))
                {
                    command.Parameters.Add("@Take", SqlDbType.Int).Value = take <= 0 ? 8 : take;
                    var rows = new List<MasterDataImportBatchViewModel>();
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            rows.Add(new MasterDataImportBatchViewModel
                            {
                                BatchId = Convert.ToInt32(reader["BatchId"]),
                                FileName = Convert.ToString(reader["FileName"]),
                                EntityType = Convert.ToString(reader["EntityType"]),
                                ImportStartedAt = Convert.ToDateTime(reader["ImportStartedAt"]),
                                SuccessRows = Convert.ToInt32(reader["SuccessRows"]),
                                FailedRows = Convert.ToInt32(reader["FailedRows"]),
                                Status = Convert.ToString(reader["Status"]),
                                CreatedAccounts = Convert.ToInt32(reader["CreatedAccounts"])
                            });
                        }
                    }

                    return rows;
                }
            }
        }

        public MasterDataImportResultViewModel RollbackBatch(int batchId, string userName)
        {
            using (var connection = _connectionFactory.CreateOpenConnection())
            {
                EnsureBatchTable(connection, null);
                string status;
                string entityType;
                using (var command = new SqlCommand("SELECT TOP 1 Status, EntityType FROM dbo.MasterDataImportBatch WHERE BatchId=@value", connection))
                {
                    command.Parameters.AddWithValue("@value", batchId.ToString());
                    using (var reader = command.ExecuteReader())
                    {
                        if (!reader.Read())
                        {
                            throw new InvalidOperationException("Import batch was not found.");
                        }

                        status = Convert.ToString(reader["Status"]);
                        entityType = Convert.ToString(reader["EntityType"]);
                    }
                }

                if (string.IsNullOrWhiteSpace(status))
                {
                    throw new InvalidOperationException("Import batch was not found.");
                }

                if (!string.Equals(entityType, MasterDataImportEntityType.ChartOfAccounts, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("Rollback is available only for Chart of Accounts import batches.");
                }

                if (!string.Equals(status, "Completed", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("Only completed batches can be rolled back.");
                }

                var detailCount = Convert.ToInt32(Scalar(connection, null, "SELECT COUNT(1) FROM dbo.MasterDataImportBatchDetail WHERE BatchId=@BatchId AND TableName=N'ACCOUNTS' AND ActionType=N'Created'", batchId));
                if (detailCount == 0)
                {
                    detailCount = SeedRollbackDetailsFromAccountDates(connection, null, batchId);
                }

                if (detailCount == 0)
                {
                    throw new InvalidOperationException("This batch has no rollback details and no created accounts could be identified by DateCreated.");
                }

                using (var transaction = connection.BeginTransaction(IsolationLevel.Serializable))
                {
                    try
                    {
                        var deletedAccounts = DeleteCreatedAccounts(connection, transaction, batchId);
                        using (var command = new SqlCommand(@"UPDATE dbo.MasterDataImportBatch
SET Status=N'RolledBack',
    ErrorMessage=COALESCE(ErrorMessage + CHAR(13) + CHAR(10), N'') + @Message
WHERE BatchId=@BatchId", connection, transaction))
                        {
                            command.Parameters.AddWithValue("@BatchId", batchId);
                            command.Parameters.AddWithValue("@Message", "Rolled back by " + (userName ?? string.Empty) + " at " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + ". Deleted accounts: " + deletedAccounts);
                            command.ExecuteNonQuery();
                        }

                        transaction.Commit();
                        return new MasterDataImportResultViewModel
                        {
                            BatchId = batchId,
                            SuccessRows = deletedAccounts,
                            FailedRows = 0,
                            Message = "Rollback completed for batch #" + batchId + ". Deleted accounts: " + deletedAccounts + "."
                        };
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        public MasterDataImportResultViewModel SyncOperationalMasterRecordsFromExistingChart(string userName)
        {
            using (var connection = _connectionFactory.CreateOpenConnection())
            {
                EnsureBatchTable(connection, null);
                var rows = LoadExistingChartRows(connection, null);
                var batchId = CreateBatch(connection, null, "Existing chart operational sync", userName, rows.Count, 0, 0);

                using (var transaction = connection.BeginTransaction(IsolationLevel.Serializable))
                {
                    try
                    {
                        var beforeCounts = CountOperationalTables(connection, transaction);
                        var processed = 0;

                        foreach (var row in rows)
                        {
                            EnsureOperationalMasterRecords(connection, transaction, row, row.AccountCode);
                            processed++;
                        }

                        var afterCounts = CountOperationalTables(connection, transaction);
                        var created = afterCounts.Sum(x => x.Value) - beforeCounts.Sum(x => x.Value);

                        CompleteBatch(connection, transaction, batchId, processed, 0, null);
                        transaction.Commit();

                        return new MasterDataImportResultViewModel
                        {
                            BatchId = batchId,
                            TotalRows = rows.Count,
                            SuccessRows = processed,
                            FailedRows = 0,
                            Message = "Operational master records were synchronized from the existing chart. Scanned accounts: " + rows.Count + ", linked records created: " + created + "."
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

        private IList<MasterDataImportRowViewModel> Validate(SqlConnection connection, SqlTransaction transaction, IList<MasterDataImportRowViewModel> rows, bool replaceExisting)
        {
            rows = rows.Where(r => !IsIgnorableChartRow(r)).ToList();
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

                row.ResolvedParentAccountCode = ResolveParent(connection, transaction, row, byCode, bySerial, replaceExisting);
                if (string.IsNullOrWhiteSpace(row.ResolvedParentAccountCode))
                {
                    row.Errors.Add("Parent account was not found in database or current Excel file.");
                }
                else if (!replaceExisting
                    && IsFinalAccount(connection, transaction, row.ResolvedParentAccountCode)
                    && !ParentIsIncludedInImport(connection, transaction, row.ResolvedParentAccountCode, rows))
                {
                    row.Errors.Add("Cannot import below an existing final account unless the parent account is included in the same Excel file.");
                }

                var parentValues = GetParentProperties(connection, transaction, row.ResolvedParentAccountCode);
                row.AccountTypes = row.AccountTypes ?? parentValues.AccountTypes;
                row.AccountTab = row.AccountTab ?? parentValues.AccountTab;
                row.DebitOrCredit = row.DebitOrCredit ?? parentValues.DebitOrCredit;
                row.DifferentType = row.DifferentType ?? parentValues.DifferentType;
                row.Authority = row.Authority ?? parentValues.Authority;
                row.Level = row.Level ?? CountLevel(!string.IsNullOrEmpty(row.AccountCode) ? row.AccountCode : row.ResolvedParentAccountCode + "a1");
                row.ImportedAccountCode = replaceExisting ? string.Empty : FindExistingAccountCode(connection, transaction, row);
                if (!replaceExisting && !CanUpdateExistingAccount(connection, transaction, row))
                {
                    row.Errors.Add("Existing account cannot be updated because the requested serial/code is already used by another account.");
                }
            }

            return rows;
        }

        private static bool IsIgnorableChartRow(MasterDataImportRowViewModel row)
        {
            return row != null
                && !row.Level.HasValue
                && string.IsNullOrWhiteSpace(row.AccountSerial)
                && string.IsNullOrWhiteSpace(row.AccountCode)
                && string.IsNullOrWhiteSpace(row.ParentAccountSerial)
                && string.IsNullOrWhiteSpace(row.ParentAccountCode);
        }

        private string ResolveParent(SqlConnection connection, SqlTransaction transaction, MasterDataImportRowViewModel row, IDictionary<string, List<MasterDataImportRowViewModel>> byCode, IDictionary<string, List<MasterDataImportRowViewModel>> bySerial, bool replaceExisting)
        {
            if (string.IsNullOrWhiteSpace(row.ParentAccountCode) && string.IsNullOrWhiteSpace(row.ParentAccountSerial))
            {
                return "r";
            }

            if (!string.IsNullOrWhiteSpace(row.ParentAccountCode))
            {
                if ((!replaceExisting && Exists(connection, transaction, "Account_Code", row.ParentAccountCode)) || byCode.ContainsKey(row.ParentAccountCode))
                {
                    return row.ParentAccountCode;
                }
            }

            if (!string.IsNullOrWhiteSpace(row.ParentAccountSerial))
            {
                if (!replaceExisting)
                {
                    var dbCode = LookupScalar(connection, transaction, "SELECT TOP 1 Account_Code FROM ACCOUNTS WHERE Account_Serial=@value", row.ParentAccountSerial);
                    if (!string.IsNullOrWhiteSpace(dbCode))
                    {
                        return dbCode;
                    }
                }

                List<MasterDataImportRowViewModel> parentRows;
                if (bySerial.TryGetValue(row.ParentAccountSerial, out parentRows) && parentRows.Count == 1)
                {
                    return !string.IsNullOrWhiteSpace(parentRows[0].AccountCode) ? parentRows[0].AccountCode : "__row:" + parentRows[0].RowNumber;
                }
            }

            return string.Empty;
        }

        private static bool ParentIsIncludedInImport(SqlConnection connection, SqlTransaction transaction, string parentCode, IEnumerable<MasterDataImportRowViewModel> rows)
        {
            if (string.IsNullOrWhiteSpace(parentCode) || parentCode == "r" || IsRowParentReference(parentCode))
            {
                return true;
            }

            var parentSerial = LookupScalar(connection, transaction, "SELECT TOP 1 Account_Serial FROM ACCOUNTS WHERE Account_Code=@value", parentCode);
            return rows.Any(r =>
                string.Equals(r.AccountCode, parentCode, StringComparison.OrdinalIgnoreCase)
                || string.Equals(r.ImportedAccountCode, parentCode, StringComparison.OrdinalIgnoreCase)
                || (!string.IsNullOrWhiteSpace(parentSerial) && string.Equals(r.AccountSerial, parentSerial, StringComparison.OrdinalIgnoreCase)));
        }

        private string FindExistingAccountCode(SqlConnection connection, SqlTransaction transaction, MasterDataImportRowViewModel row)
        {
            if (!string.IsNullOrWhiteSpace(row.AccountSerial))
            {
                var bySerial = LookupScalar(connection, transaction, "SELECT TOP 1 Account_Code FROM ACCOUNTS WHERE Account_Serial=@value", row.AccountSerial);
                if (!string.IsNullOrWhiteSpace(bySerial))
                {
                    return bySerial;
                }
            }

            if (!string.IsNullOrWhiteSpace(row.AccountCode))
            {
                var byCode = LookupScalar(connection, transaction, "SELECT TOP 1 Account_Code FROM ACCOUNTS WHERE Account_Code=@value", row.AccountCode);
                if (!string.IsNullOrWhiteSpace(byCode))
                {
                    return byCode;
                }
            }

            if (!string.IsNullOrWhiteSpace(row.AccountName))
            {
                var byNameAndParent = LookupExistingAccountByName(connection, transaction, row.AccountName, row.ResolvedParentAccountCode);
                if (!string.IsNullOrWhiteSpace(byNameAndParent))
                {
                    return byNameAndParent;
                }

                return LookupExistingAccountByName(connection, transaction, row.AccountName, null);
            }

            return string.Empty;
        }

        private static string LookupExistingAccountByName(SqlConnection connection, SqlTransaction transaction, string accountName, string parentCode)
        {
            var sql = string.IsNullOrWhiteSpace(parentCode) || IsRowParentReference(parentCode)
                ? "SELECT TOP 1 Account_Code FROM ACCOUNTS WHERE LTRIM(RTRIM(Account_Name))=@name ORDER BY LEN(Account_Code)"
                : "SELECT TOP 1 Account_Code FROM ACCOUNTS WHERE LTRIM(RTRIM(Account_Name))=@name AND Parent_Account_Code=@parent ORDER BY LEN(Account_Code)";

            using (var command = new SqlCommand(sql, connection, transaction))
            {
                command.Parameters.AddWithValue("@name", Clean(accountName));
                if (!string.IsNullOrWhiteSpace(parentCode) && !IsRowParentReference(parentCode))
                {
                    command.Parameters.AddWithValue("@parent", parentCode);
                }

                var result = command.ExecuteScalar();
                return result == null || result == DBNull.Value ? string.Empty : Convert.ToString(result);
            }
        }

        private bool CanUpdateExistingAccount(SqlConnection connection, SqlTransaction transaction, MasterDataImportRowViewModel row)
        {
            if (string.IsNullOrWhiteSpace(row.ImportedAccountCode))
            {
                return true;
            }

            if (!string.IsNullOrWhiteSpace(row.AccountSerial))
            {
                var owner = LookupScalar(connection, transaction, "SELECT TOP 1 Account_Code FROM ACCOUNTS WHERE Account_Serial=@value", row.AccountSerial);
                if (!string.IsNullOrWhiteSpace(owner) && !string.Equals(owner, row.ImportedAccountCode, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            if (!string.IsNullOrWhiteSpace(row.AccountCode))
            {
                var owner = LookupScalar(connection, transaction, "SELECT TOP 1 Account_Code FROM ACCOUNTS WHERE Account_Code=@value", row.AccountCode);
                if (!string.IsNullOrWhiteSpace(owner) && !string.Equals(owner, row.ImportedAccountCode, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            return !string.Equals(row.ImportedAccountCode, row.ResolvedParentAccountCode, StringComparison.OrdinalIgnoreCase);
        }

        private string UpsertAccount(SqlConnection connection, SqlTransaction transaction, MasterDataImportRowViewModel row)
        {
            var existingCode = !string.IsNullOrWhiteSpace(row.ImportedAccountCode)
                ? row.ImportedAccountCode
                : FindExistingAccountCode(connection, transaction, row);

            return string.IsNullOrWhiteSpace(existingCode)
                ? InsertAccount(connection, transaction, row)
                : UpdateAccount(connection, transaction, row, existingCode);
        }

        private string UpdateAccount(SqlConnection connection, SqlTransaction transaction, MasterDataImportRowViewModel row, string existingCode)
        {
            var accountNameEng = string.IsNullOrWhiteSpace(row.AccountNameEnglish) ? row.AccountName : row.AccountNameEnglish;
            var parentCode = string.Equals(existingCode, row.ResolvedParentAccountCode, StringComparison.OrdinalIgnoreCase)
                ? LookupScalar(connection, transaction, "SELECT TOP 1 Parent_Account_Code FROM ACCOUNTS WHERE Account_Code=@value", existingCode)
                : row.ResolvedParentAccountCode;

            using (var command = new SqlCommand(@"UPDATE ACCOUNTS SET
AccountTypes=@AccountTypes,
AccountTab=@AccountTab,
DepitOrCredit=@DepitOrCredit,
Differenttype=@Differenttype,
Authority=@Authority,
Account_Name=@Account_Name,
Parent_Account_Code=@Parent_Account_Code,
last_account=@last_account,
Account_Serial=CASE WHEN NULLIF(@Account_Serial, N'') IS NULL THEN Account_Serial ELSE @Account_Serial END,
Account_NameEng=@Account_NameEng,
currenct_code=@currenct_code
WHERE Account_Code=@ExistingAccountCode", connection, transaction))
            {
                command.Parameters.AddWithValue("@AccountTypes", row.AccountTypes ?? 0);
                command.Parameters.AddWithValue("@AccountTab", row.AccountTab ?? 0);
                command.Parameters.AddWithValue("@DepitOrCredit", row.DebitOrCredit ?? 0);
                command.Parameters.AddWithValue("@Differenttype", row.DifferentType ?? 0);
                command.Parameters.AddWithValue("@Authority", row.Authority ?? 0);
                command.Parameters.AddWithValue("@Account_Name", row.AccountName);
                command.Parameters.AddWithValue("@Parent_Account_Code", parentCode);
                command.Parameters.AddWithValue("@last_account", row.IsFinalAccount);
                command.Parameters.AddWithValue("@Account_Serial", row.AccountSerial ?? string.Empty);
                command.Parameters.AddWithValue("@Account_NameEng", accountNameEng);
                command.Parameters.AddWithValue("@currenct_code", row.CurrencyCode);
                command.Parameters.AddWithValue("@ExistingAccountCode", existingCode);
                command.ExecuteNonQuery();
            }

            return existingCode;
        }

        private string InsertAccount(SqlConnection connection, SqlTransaction transaction, MasterDataImportRowViewModel row)
        {
            var accountCode = string.IsNullOrWhiteSpace(row.AccountCode) ? GenerateAccountCode(connection, transaction, row.ResolvedParentAccountCode) : row.AccountCode;
            var accountSerial = string.IsNullOrWhiteSpace(row.AccountSerial) ? GenerateAccountSerial(connection, transaction, row.ResolvedParentAccountCode, accountCode, row.IsFinalAccount) : row.AccountSerial;
            var accountNameEng = string.IsNullOrWhiteSpace(row.AccountNameEnglish) ? row.AccountName : row.AccountNameEnglish;
            var branch = GetDefaultBranch(connection, transaction);
            var branchText = branch.BranchId > 0 ? branch.BranchId.ToString() : string.Empty;

            using (var command = new SqlCommand(@"INSERT INTO ACCOUNTS
(AccountTypes, AccountTab, DepitOrCredit, Differenttype, Authority, [Block], Account_Code, Account_Name, Parent_Account_Code,
 last_account, cannot_del, Branch, BranchID, Account_Serial, BasicAccount, DateCreated, Account_NameEng, currenct_code, mowazna,
 cost_center, Sum_account, cost_center_type, cost_center_id, ActivityTypeId)
VALUES
(@AccountTypes, @AccountTab, @DepitOrCredit, @Differenttype, @Authority, 0, @Account_Code, @Account_Name, @Parent_Account_Code,
 @last_account, 0, @BranchText, @BranchId, @Account_Serial, 0, GETDATE(), @Account_NameEng, @currenct_code, 0,
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
                command.Parameters.AddWithValue("@BranchText", branchText);
                command.Parameters.AddWithValue("@BranchId", branch.BranchId > 0 ? (object)branch.BranchId : DBNull.Value);
                command.Parameters.AddWithValue("@Account_Serial", accountSerial);
                command.Parameters.AddWithValue("@Account_NameEng", accountNameEng);
                command.Parameters.AddWithValue("@currenct_code", row.CurrencyCode);
                command.ExecuteNonQuery();
            }

            LogCreatedAccount(connection, transaction, accountCode, accountSerial, row.RowNumber);
            return accountCode;
        }

        private void LogCreatedAccount(SqlConnection connection, SqlTransaction transaction, string accountCode, string accountSerial, int rowNumber)
        {
            if (_activeBatchId <= 0 || string.IsNullOrWhiteSpace(accountCode))
            {
                return;
            }

            EnsureBatchTable(connection, transaction);
            using (var command = new SqlCommand(@"INSERT INTO dbo.MasterDataImportBatchDetail
(BatchId, TableName, RecordKey, RecordSerial, RowNumber, ActionType)
VALUES (@BatchId, N'ACCOUNTS', @RecordKey, @RecordSerial, @RowNumber, N'Created');", connection, transaction))
            {
                command.Parameters.AddWithValue("@BatchId", _activeBatchId);
                command.Parameters.AddWithValue("@RecordKey", accountCode);
                command.Parameters.AddWithValue("@RecordSerial", (object)accountSerial ?? DBNull.Value);
                command.Parameters.AddWithValue("@RowNumber", rowNumber <= 0 ? (object)DBNull.Value : rowNumber);
                command.ExecuteNonQuery();
            }
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

        private static void ClearAccounts(SqlConnection connection, SqlTransaction transaction)
        {
            using (var command = new SqlCommand("DELETE FROM dbo.ACCOUNTS", connection, transaction))
            {
                command.ExecuteNonQuery();
            }
        }

        private static IList<MasterDataImportRowViewModel> LoadExistingChartRows(SqlConnection connection, SqlTransaction transaction)
        {
            var rows = new List<MasterDataImportRowViewModel>();
            using (var command = new SqlCommand(@"SELECT
    Account_Code,
    Parent_Account_Code,
    Account_Name,
    Account_NameEng,
    Account_Serial,
    last_account,
    currenct_code,
    AccountTypes,
    AccountTab,
    DepitOrCredit,
    Differenttype,
    Authority
FROM dbo.ACCOUNTS
WHERE ISNULL(Account_Code, N'') <> N''
  AND Account_Code <> N'r'
ORDER BY LEN(Account_Code), Account_Code", connection, transaction))
            using (var reader = command.ExecuteReader())
            {
                var rowNumber = 1;
                while (reader.Read())
                {
                    var accountCode = Convert.ToString(reader["Account_Code"]);
                    var parentCode = Convert.ToString(reader["Parent_Account_Code"]);
                    rows.Add(new MasterDataImportRowViewModel
                    {
                        RowNumber = rowNumber++,
                        AccountCode = accountCode,
                        ImportedAccountCode = accountCode,
                        AccountSerial = Convert.ToString(reader["Account_Serial"]),
                        AccountName = Convert.ToString(reader["Account_Name"]),
                        AccountNameEnglish = Convert.ToString(reader["Account_NameEng"]),
                        ParentAccountCode = parentCode,
                        ResolvedParentAccountCode = parentCode,
                        IsFinalAccount = ReadBool(reader, "last_account"),
                        CurrencyCode = Convert.ToString(reader["currenct_code"]),
                        AccountTypes = ReadNullableInt(reader, "AccountTypes"),
                        AccountTab = ReadNullableInt(reader, "AccountTab"),
                        DebitOrCredit = ReadNullableInt(reader, "DepitOrCredit"),
                        DifferentType = ReadNullableInt(reader, "Differenttype"),
                        Authority = ReadNullableInt(reader, "Authority")
                    });
                }
            }

            return rows;
        }

        private static IDictionary<string, int> CountOperationalTables(SqlConnection connection, SqlTransaction transaction)
        {
            var tables = new[]
            {
                "BanksData",
                "TblBoxesData",
                "TblStore",
                "TblCustemers",
                "TblEmployee",
                "projects",
                "FixedAssetsGroup",
                "ExpensesType",
                "TblRevenuesTypes"
            };

            var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var table in tables)
            {
                counts[table] = TableExists(connection, transaction, table)
                    ? Convert.ToInt32(Scalar(connection, transaction, "SELECT COUNT(1) FROM dbo." + table))
                    : 0;
            }

            return counts;
        }

        public void EnsureOperationalMasterRecords(SqlConnection connection, SqlTransaction transaction, MasterDataImportRowViewModel row, string accountCode)
        {
            if (string.IsNullOrWhiteSpace(accountCode))
            {
                return;
            }

            var branch = GetDefaultBranch(connection, transaction);
            var ownText = (row.AccountName ?? string.Empty) + " " + (row.AccountNameEnglish ?? string.Empty);
            var directParentCode = GetDirectParentCode(connection, transaction, accountCode);
            var directParentText = GetDirectParentText(connection, transaction, accountCode);
            var path = GetAccountPathText(connection, transaction, accountCode) + " " + ownText;
            var isFixedAsset = LooksLikeFixedAsset(path);
            if (!row.IsFinalAccount && !isFixedAsset)
            {
                return;
            }

            if (LooksLikeExpenseType(path, directParentText) && IsWithinBranchTree(connection, transaction, accountCode, branch.GetAccount(33)))
            {
                EnsureExpenseTypeData(connection, transaction, row, accountCode, directParentCode, branch.GetAccount(33));
                return;
            }

            if (LooksLikeRevenueType(path, directParentText) && IsWithinBranchTree(connection, transaction, accountCode, branch.GetAccount(34)))
            {
                EnsureRevenueTypeData(connection, transaction, row, accountCode, directParentCode, branch.GetAccount(34));
                return;
            }

            if (LooksLikeBank(path) && IsWithinBranchTree(connection, transaction, accountCode, branch.GetAccount(20)))
            {
                EnsureBankData(connection, transaction, row, accountCode);
                return;
            }

            if (LooksLikeCustodyBox(path) && IsWithinBranchTree(connection, transaction, accountCode, branch.GetAccount(35)))
            {
                EnsureCashBoxData(connection, transaction, row, accountCode, true);
                return;
            }

            if (LooksLikeCashBox(path) && IsWithinBranchTree(connection, transaction, accountCode, branch.GetAccount(6)))
            {
                EnsureCashBoxData(connection, transaction, row, accountCode, false);
                return;
            }

            if (LooksLikeEmployeeReceivable(path) && IsEmployeeReceivableOperationalAccount(path, ownText))
            {
                EnsureEmployeeData(connection, transaction, row, accountCode);
                return;
            }

            if (LooksLikeSubContractor(path))
            {
                EnsureCustomerOrSupplierData(connection, transaction, row, accountCode, 3);
                return;
            }

            if (LooksLikeSupplier(path))
            {
                EnsureCustomerOrSupplierData(connection, transaction, row, accountCode, 2);
                return;
            }

            if (LooksLikeCustomer(path))
            {
                EnsureCustomerOrSupplierData(connection, transaction, row, accountCode, 1);
                return;
            }

            if (LooksLikeStore(ownText) || LooksLikeStore(directParentText))
            {
                EnsureStoreData(connection, transaction, row, accountCode);
                return;
            }

            if (LooksLikeProject(path))
            {
                EnsureProjectData(connection, transaction, row, accountCode);
                return;
            }

            if (isFixedAsset)
            {
                EnsureFixedAssetGroupData(connection, transaction, row, accountCode);
                return;
            }
        }

        private static string GetDirectParentCode(SqlConnection connection, SqlTransaction transaction, string accountCode)
        {
            using (var command = new SqlCommand("SELECT TOP 1 Parent_Account_Code FROM dbo.ACCOUNTS WHERE Account_Code = @AccountCode", connection, transaction))
            {
                command.Parameters.AddWithValue("@AccountCode", accountCode);
                var result = command.ExecuteScalar();
                return result == null || result == DBNull.Value ? string.Empty : Convert.ToString(result);
            }
        }

        private static string GetDirectParentText(SqlConnection connection, SqlTransaction transaction, string accountCode)
        {
            using (var command = new SqlCommand(@"SELECT TOP 1 COALESCE(p.Account_Name, N'') + N' ' + COALESCE(p.Account_NameEng, N'')
FROM dbo.ACCOUNTS a
INNER JOIN dbo.ACCOUNTS p ON p.Account_Code = a.Parent_Account_Code
WHERE a.Account_Code = @AccountCode", connection, transaction))
            {
                command.Parameters.AddWithValue("@AccountCode", accountCode);
                var result = command.ExecuteScalar();
                return result == null || result == DBNull.Value ? string.Empty : Convert.ToString(result);
            }
        }

        private static string GetAccountPathText(SqlConnection connection, SqlTransaction transaction, string accountCode)
        {
            using (var command = new SqlCommand(@";WITH AccountPath AS
(
    SELECT Account_Code, Parent_Account_Code, Account_Name, Account_NameEng, 0 AS Depth
    FROM dbo.ACCOUNTS
    WHERE Account_Code = @AccountCode
    UNION ALL
    SELECT p.Account_Code, p.Parent_Account_Code, p.Account_Name, p.Account_NameEng, ap.Depth + 1
    FROM dbo.ACCOUNTS p
    INNER JOIN AccountPath ap ON p.Account_Code = ap.Parent_Account_Code
    WHERE ap.Depth < 20
)
SELECT COALESCE(Account_Name, N'') + N' ' + COALESCE(Account_NameEng, N'') + N' '
FROM AccountPath
FOR XML PATH(N''), TYPE", connection, transaction))
            {
                command.Parameters.AddWithValue("@AccountCode", accountCode);
                var result = command.ExecuteScalar();
                return result == null || result == DBNull.Value ? string.Empty : Convert.ToString(result);
            }
        }

        private static bool LooksLikeBank(string value)
        {
            value = (value ?? string.Empty).ToLowerInvariant();
            return value.Contains("bank") || value.Contains("\u0628\u0646\u0643") || value.Contains("\u0628\u0646\u0648\u0643");
        }

        private static bool LooksLikeExpenseType(string pathText, string directParentText)
        {
            var text = ((pathText ?? string.Empty) + " " + (directParentText ?? string.Empty)).ToLowerInvariant();
            var hasExpenseWord = text.Contains("expense")
                || text.Contains("expenses")
                || text.Contains("\u0645\u0635\u0631\u0648\u0641")
                || text.Contains("\u0645\u0635\u0627\u0631\u064a\u0641")
                || text.Contains("\u0639\u0645\u0648\u0644\u0629")
                || text.Contains("\u0639\u0645\u0648\u0644\u0627\u062a")
                || text.Contains("\u0641\u0648\u0627\u0626\u062f");
            return hasExpenseWord;
        }

        private static bool LooksLikeRevenueType(string pathText, string directParentText)
        {
            var text = ((pathText ?? string.Empty) + " " + (directParentText ?? string.Empty)).ToLowerInvariant();
            return text.Contains("revenue")
                || text.Contains("revenues")
                || text.Contains("income")
                || text.Contains("\u0627\u064a\u0631\u0627\u062f")
                || text.Contains("\u0625\u064a\u0631\u0627\u062f")
                || text.Contains("\u0627\u0644\u0627\u064a\u0631\u0627\u062f")
                || text.Contains("\u0627\u0644\u0625\u064a\u0631\u0627\u062f");
        }

        private static bool IsWithinBranchTree(SqlConnection connection, SqlTransaction transaction, string accountCode, string rootAccountCode)
        {
            accountCode = Clean(accountCode);
            rootAccountCode = Clean(rootAccountCode);
            if (string.IsNullOrWhiteSpace(accountCode) || string.IsNullOrWhiteSpace(rootAccountCode))
            {
                return false;
            }

            using (var command = new SqlCommand(@";WITH AccountPath AS
(
    SELECT Account_Code, Parent_Account_Code, 0 AS Depth
    FROM dbo.ACCOUNTS
    WHERE Account_Code = @AccountCode
    UNION ALL
    SELECT p.Account_Code, p.Parent_Account_Code, ap.Depth + 1
    FROM dbo.ACCOUNTS p
    INNER JOIN AccountPath ap ON p.Account_Code = ap.Parent_Account_Code
    WHERE ap.Depth < 30
)
SELECT TOP 1 1
FROM AccountPath
WHERE Account_Code = @RootAccountCode", connection, transaction))
            {
                command.Parameters.AddWithValue("@AccountCode", accountCode);
                command.Parameters.AddWithValue("@RootAccountCode", rootAccountCode);
                return command.ExecuteScalar() != null;
            }
        }

        private static bool LooksLikeCashBox(string value)
        {
            value = (value ?? string.Empty).ToLowerInvariant();
            return value.Contains("cash")
                || value.Contains("box")
                || value.Contains("\u062e\u0632\u064a\u0646\u0629")
                || value.Contains("\u062e\u0632\u0627\u0626\u0646")
                || value.Contains("\u0635\u0646\u062f\u0648\u0642");
        }

        private static bool LooksLikeCustodyBox(string value)
        {
            value = (value ?? string.Empty).ToLowerInvariant();
            return value.Contains("custody")
                || value.Contains("cash on hand")
                || value.Contains("\u0639\u0647\u062f")
                || value.Contains("\u0639\u0647\u062f\u0629")
                || value.Contains("\u0639\u0647\u062f\u0647");
        }

        private static bool LooksLikeStore(string value)
        {
            value = (value ?? string.Empty).ToLowerInvariant();
            return value.Contains("warehouse")
                || value.Contains("store")
                || value.Contains("\u0645\u062e\u0632\u0648\u0646")
                || value.Contains("\u0645\u062e\u0627\u0632\u0646")
                || value.Contains("\u0645\u0633\u062a\u0648\u062f\u0639");
        }

        private static bool LooksLikeCustomer(string value)
        {
            value = (value ?? string.Empty).ToLowerInvariant();
            return value.Contains("customer")
                || value.Contains("client")
                || value.Contains("\u0639\u0645\u064a\u0644")
                || value.Contains("\u0639\u0645\u0644\u0627\u0621")
                || value.Contains("\u0627\u0644\u0639\u0645\u0644\u0627\u0621");
        }

        private static bool LooksLikeSupplier(string value)
        {
            value = (value ?? string.Empty).ToLowerInvariant();
            return value.Contains("supplier")
                || value.Contains("vendor")
                || value.Contains("\u0645\u0648\u0631\u062f")
                || value.Contains("\u0645\u0648\u0631\u062f\u064a\u0646");
        }

        private static bool LooksLikeSubContractor(string value)
        {
            value = (value ?? string.Empty).ToLowerInvariant();
            return value.Contains("subcontractor")
                || value.Contains("contractor")
                || value.Contains("\u0645\u0642\u0627\u0648\u0644")
                || value.Contains("\u0645\u0642\u0627\u0648\u0644\u064a\u0646");
        }

        private static bool LooksLikeEmployeeReceivable(string value)
        {
            value = (value ?? string.Empty).ToLowerInvariant();
            return value.Contains("employee receivable")
                || value.Contains("employees receivable")
                || value.Contains("staff receivable")
                || value.Contains("receivable employee")
                || value.Contains("\u0630\u0645\u0645")
                || value.Contains("\u0630\u0645\u0645 \u0627\u0644\u0645\u0648\u0638\u0641\u064a\u0646")
                || value.Contains("\u0630\u0645\u0629 \u0645\u0648\u0638\u0641")
                || value.Contains("\u0630\u0645\u0645 \u0627\u0644\u0639\u0627\u0645\u0644\u064a\u0646");
        }

        private static bool IsEmployeeReceivableOperationalAccount(string pathText, string ownText)
        {
            var path = (pathText ?? string.Empty).ToLowerInvariant();
            var own = (ownText ?? string.Empty).ToLowerInvariant();
            var hasEmployeeReceivableParent =
                path.Contains("\u0630\u0645\u0645 \u0627\u0644\u0645\u0648\u0638\u0641\u064a\u0646")
                || path.Contains("\u0630\u0645\u0645 \u0627\u0644\u0639\u0627\u0645\u0644\u064a\u0646")
                || path.Contains("\u0630\u0645\u0629 \u0645\u0648\u0638\u0641")
                || path.Contains("employee receivable")
                || path.Contains("employees receivable")
                || path.Contains("staff receivable");

            if (!hasEmployeeReceivableParent)
            {
                return false;
            }

            return !IsEmployeeReceivableGroupName(own);
        }

        private static bool IsEmployeeReceivableGroupName(string value)
        {
            value = (value ?? string.Empty).Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(value))
            {
                return true;
            }

            var normalized = value
                .Replace("\u0630\u0645\u0645", " ")
                .Replace("\u0630\u0645\u0629", " ")
                .Replace("\u0630\u0645\u0647", " ")
                .Replace("\u0627\u0644\u0645\u0648\u0638\u0641\u064a\u0646", " ")
                .Replace("\u0627\u0644\u0639\u0627\u0645\u0644\u064a\u0646", " ")
                .Replace("employee", " ")
                .Replace("employees", " ")
                .Replace("receivable", " ")
                .Replace("staff", " ");

            normalized = new string(normalized.Where(c => !char.IsDigit(c) && !char.IsPunctuation(c) && !char.IsSymbol(c)).ToArray()).Trim();
            return string.IsNullOrWhiteSpace(normalized);
        }

        private static bool LooksLikeProject(string value)
        {
            value = (value ?? string.Empty).ToLowerInvariant();
            return value.Contains("project")
                || value.Contains("\u0645\u0634\u0631\u0648\u0639")
                || value.Contains("\u0645\u0634\u0627\u0631\u064a\u0639");
        }

        private static bool LooksLikeFixedAsset(string value)
        {
            value = (value ?? string.Empty).ToLowerInvariant();
            var hasAssetWord = value.Contains("asset")
                || value.Contains("\u0627\u0635\u0644")
                || value.Contains("\u0623\u0635\u0644")
                || value.Contains("\u0627\u0635\u0648\u0644")
                || value.Contains("\u0623\u0635\u0648\u0644")
                || value.Contains("\u0645\u0648\u062c\u0648\u062f");
            var hasFixedWord = value.Contains("fixed")
                || value.Contains("\u062b\u0627\u0628")
                || value.Contains("\u062b\u0627\u064a\u062a");

            return value.Contains("fixed asset")
                || value.Contains("asset")
                || (hasAssetWord && hasFixedWord)
                || value.Contains("\u0627\u0635\u0644 \u062b\u0627\u0628\u062a")
                || value.Contains("\u0627\u0635\u0648\u0644 \u062b\u0627\u0628\u062a\u0629")
                || value.Contains("\u0623\u0635\u0648\u0644 \u062b\u0627\u0628\u062a\u0629")
                || value.Contains("\u0627\u0644\u0627\u0635\u0648\u0644 \u0627\u0644\u062b\u0627\u0628\u062a\u0629")
                || value.Contains("\u0627\u0644\u0623\u0635\u0648\u0644 \u0627\u0644\u062b\u0627\u0628\u062a\u0629")
                || value.Contains("\u0645\u0648\u062c\u0648\u062f\u0627\u062a \u062b\u0627\u0628\u062a\u0629")
                || value.Contains("\u0627\u0644\u0645\u0648\u062c\u0648\u062f\u0627\u062a \u0627\u0644\u062b\u0627\u0628\u062a\u0629");
        }

        private void EnsureBankData(SqlConnection connection, SqlTransaction transaction, MasterDataImportRowViewModel row, string accountCode)
        {
            if (!TableExists(connection, transaction, "BanksData"))
            {
                return;
            }

            var branch = GetDefaultBranch(connection, transaction);
            var options = GetSystemOptions(connection, transaction);
            var bankName = Limit(row.AccountName, 50);
            var bankNameEnglish = Limit(string.IsNullOrWhiteSpace(row.AccountNameEnglish) ? (row.AccountName ?? string.Empty) : row.AccountNameEnglish, 255);
            var parentAccount = branch.GetAccount(20);
            var groupParent = options.BankAccounts ? EnsureChildAccount(connection, transaction, parentAccount, bankName, bankNameEnglish, false, null) : string.Empty;
            var accountCode1 = !string.IsNullOrWhiteSpace(groupParent) ? EnsureChildAccount(connection, transaction, groupParent, bankName + "  \u0634\u064a\u0643\u0627\u062a \u062a\u062d\u062a \u0627\u0644\u062a\u062d\u0635\u064a\u0644 ", bankNameEnglish + " Under Collection Cheque", true, null) : string.Empty;
            var accountCode2 = !string.IsNullOrWhiteSpace(groupParent) ? EnsureChildAccount(connection, transaction, groupParent, bankName + " \u0634\u064a\u0643\u0627\u062a \u0645\u0624\u062c\u0644\u0629 \u0639\u0644\u0649 \u0644\u0634\u0631\u0643\u0629", bankNameEnglish + " Pending Cheque", true, null) : string.Empty;
            var accountCode3 = options.BankCommission ? EnsureChildAccount(connection, transaction, branch.GetAccount(50), bankName + "  \u0639\u0648\u0644\u0627\u062a", bankNameEnglish + " Commission ", true, null) : string.Empty;

            using (var command = new SqlCommand(@"IF EXISTS (SELECT 1 FROM dbo.BanksData WHERE Account_Code = @AccountCode)
BEGIN
    UPDATE dbo.BanksData
    SET BankName = COALESCE(NULLIF(@BankName, N''), BankName),
        BankNamee = COALESCE(NULLIF(@NameEnglish, N''), BankNamee),
        AccountName = COALESCE(NULLIF(@AccountName, N''), AccountName),
        Account_Code1 = COALESCE(NULLIF(@AccountCode1, N''), Account_Code1),
        Account_Code2 = COALESCE(NULLIF(@AccountCode2, N''), Account_Code2),
        Account_Code3 = COALESCE(NULLIF(@AccountCode3, N''), Account_Code3),
        ParetnAccount = COALESCE(NULLIF(@GroupParent, N''), ParetnAccount),
        parent_account = COALESCE(NULLIF(@ParentAccount, N''), parent_account),
        Branch = @BranchText,
        BranchId = @BranchId,
        Currency_ID = COALESCE(Currency_ID, 1),
        Currency = COALESCE(Currency, 1)
    WHERE Account_Code = @AccountCode;
END
ELSE IF EXISTS (SELECT 1 FROM dbo.TblEmployee WHERE Emp_Code = @Code)
BEGIN
    UPDATE dbo.TblEmployee
    SET Emp_Name = COALESCE(NULLIF(@Name, N''), Emp_Name),
        Emp_Namee = COALESCE(NULLIF(@NameEnglish, N''), Emp_Namee),
        Emp_Name1 = COALESCE(NULLIF(@Name1, N''), Emp_Name1),
        Emp_Name2 = COALESCE(NULLIF(@Name2, N''), Emp_Name2),
        Emp_Name3 = COALESCE(NULLIF(@Name3, N''), Emp_Name3),
        Emp_Name4 = COALESCE(NULLIF(@Name4, N''), Emp_Name4),
        Emp_Namee1 = COALESCE(NULLIF(@NameEnglish1, N''), Emp_Namee1),
        Emp_Namee2 = COALESCE(NULLIF(@NameEnglish2, N''), Emp_Namee2),
        Emp_Namee3 = COALESCE(NULLIF(@NameEnglish3, N''), Emp_Namee3),
        Emp_Namee4 = COALESCE(NULLIF(@NameEnglish4, N''), Emp_Namee4),
        Account_code = COALESCE(NULLIF(Account_code, N''), @AccountCode),
        Account_code1 = COALESCE(NULLIF(Account_code1, N''), @AccountCode1),
        Account_Code2 = COALESCE(NULLIF(Account_Code2, N''), @AccountCode2),
        Account_Code3 = COALESCE(NULLIF(Account_Code3, N''), @AccountCode3),
        Account_Code4 = COALESCE(NULLIF(Account_Code4, N''), @AccountCode4),
        Account_Code5 = COALESCE(NULLIF(Account_Code5, N''), @AccountCode5),
        BranchId = @BranchId
    WHERE Emp_Code = @Code;
END
ELSE
BEGIN
    INSERT INTO dbo.BanksData
    (BankID, BankName, BankNamee, Account_Code, Account_Code1, Account_Code2, Account_Code3, ParetnAccount, parent_account, Branch, BranchId, Currency_ID, Currency, AccountName, Commision)
    VALUES
    ((SELECT ISNULL(MAX(BankID), 0) + 1 FROM dbo.BanksData), @BankName, @NameEnglish, @AccountCode, @AccountCode1, @AccountCode2, @AccountCode3, @GroupParent, @ParentAccount, @BranchText, @BranchId, 1, 1, @AccountName, 0);
END", connection, transaction))
            {
                command.Parameters.AddWithValue("@AccountCode", accountCode);
                command.Parameters.AddWithValue("@BankName", bankName);
                command.Parameters.AddWithValue("@AccountName", Limit(row.AccountName, 255));
                command.Parameters.AddWithValue("@NameEnglish", bankNameEnglish);
                command.Parameters.AddWithValue("@AccountCode1", Limit(accountCode1, 50));
                command.Parameters.AddWithValue("@AccountCode2", Limit(accountCode2, 50));
                command.Parameters.AddWithValue("@AccountCode3", Limit(accountCode3, 50));
                command.Parameters.AddWithValue("@GroupParent", Limit(groupParent, 255));
                command.Parameters.AddWithValue("@ParentAccount", Limit(parentAccount, 400));
                command.Parameters.AddWithValue("@BranchId", branch.BranchId);
                command.Parameters.AddWithValue("@BranchText", branch.BranchId > 0 ? branch.BranchId.ToString() : string.Empty);
                command.ExecuteNonQuery();
            }
        }

        private void EnsureExpenseTypeData(SqlConnection connection, SqlTransaction transaction, MasterDataImportRowViewModel row, string accountCode, string directParentCode, string branchExpenseAccount)
        {
            if (!TableExists(connection, transaction, "ExpensesType"))
            {
                return;
            }

            var name = Limit(Clean(row.AccountName), 50);
            if (string.IsNullOrWhiteSpace(name))
            {
                return;
            }

            var nameEnglish = Limit(string.IsNullOrWhiteSpace(row.AccountNameEnglish) ? name : Clean(row.AccountNameEnglish), 255);
            var parentAccount = Limit(string.IsNullOrWhiteSpace(directParentCode) ? branchExpenseAccount : directParentCode, 500);

            using (var command = new SqlCommand(@"IF EXISTS (SELECT 1 FROM dbo.ExpensesType WHERE Account_Code = @AccountCode)
BEGIN
    UPDATE dbo.ExpensesType
    SET Name = COALESCE(NULLIF(@Name, N''), Name),
        Namee = COALESCE(NULLIF(@NameEnglish, N''), Namee),
        Remarks = COALESCE(NULLIF(@Remarks, N''), Remarks),
        parent_account = COALESCE(NULLIF(@ParentAccount, N''), parent_account),
        ManualEntrty = 0
    WHERE Account_Code = @AccountCode;
END
ELSE IF EXISTS (SELECT 1 FROM dbo.ExpensesType WHERE Name = @Name)
BEGIN
    UPDATE dbo.ExpensesType
    SET Namee = COALESCE(NULLIF(@NameEnglish, N''), Namee),
        Remarks = COALESCE(NULLIF(@Remarks, N''), Remarks),
        Account_Code = COALESCE(NULLIF(@AccountCode, N''), Account_Code),
        parent_account = COALESCE(NULLIF(@ParentAccount, N''), parent_account),
        ManualEntrty = 0
    WHERE Name = @Name;
END
ELSE
BEGIN
    INSERT INTO dbo.ExpensesType
    (ID, Name, Namee, Remarks, Account_Code, parent_account, TypicalProduction, IndirectCosts, ManualEntrty, ComposeExpenses, Transportation, DataTypeExchangeCode)
    VALUES
    ((SELECT ISNULL(MAX(ID), 0) + 1 FROM dbo.ExpensesType), @Name, @NameEnglish, @Remarks, @AccountCode, @ParentAccount, 0, 0, 0, 0, 0, NULL);
END", connection, transaction))
            {
                command.Parameters.AddWithValue("@AccountCode", Limit(accountCode, 500));
                command.Parameters.AddWithValue("@Name", name);
                command.Parameters.AddWithValue("@NameEnglish", nameEnglish);
                command.Parameters.AddWithValue("@Remarks", DBNull.Value);
                command.Parameters.AddWithValue("@ParentAccount", (object)parentAccount ?? DBNull.Value);
                command.ExecuteNonQuery();
            }
        }

        private void EnsureRevenueTypeData(SqlConnection connection, SqlTransaction transaction, MasterDataImportRowViewModel row, string accountCode, string directParentCode, string branchRevenueAccount)
        {
            if (!TableExists(connection, transaction, "TblRevenuesTypes"))
            {
                return;
            }

            var name = Limit(Clean(row.AccountName), 50);
            if (string.IsNullOrWhiteSpace(name))
            {
                return;
            }

            var nameEnglish = Limit(string.IsNullOrWhiteSpace(row.AccountNameEnglish) ? name : Clean(row.AccountNameEnglish), 255);
            var parentAccount = Limit(string.IsNullOrWhiteSpace(directParentCode) ? branchRevenueAccount : directParentCode, 500);

            using (var command = new SqlCommand(@"IF EXISTS (SELECT 1 FROM dbo.TblRevenuesTypes WHERE Account_Code = @AccountCode)
BEGIN
    UPDATE dbo.TblRevenuesTypes
    SET RevenuesName = COALESCE(NULLIF(@Name, N''), RevenuesName),
        RevenuesNamee = COALESCE(NULLIF(@NameEnglish, N''), RevenuesNamee),
        Remarks = COALESCE(NULLIF(@Remarks, N''), Remarks),
        parent_account = COALESCE(NULLIF(@ParentAccount, N''), parent_account),
        ManualEntrty = 0
    WHERE Account_Code = @AccountCode;
END
ELSE IF EXISTS (SELECT 1 FROM dbo.TblRevenuesTypes WHERE RevenuesName = @Name)
BEGIN
    UPDATE dbo.TblRevenuesTypes
    SET RevenuesNamee = COALESCE(NULLIF(@NameEnglish, N''), RevenuesNamee),
        Remarks = COALESCE(NULLIF(@Remarks, N''), Remarks),
        Account_Code = COALESCE(NULLIF(@AccountCode, N''), Account_Code),
        parent_account = COALESCE(NULLIF(@ParentAccount, N''), parent_account),
        ManualEntrty = 0
    WHERE RevenuesName = @Name;
END
ELSE
BEGIN
    INSERT INTO dbo.TblRevenuesTypes
    (RevenuesID, RevenuesName, RevenuesNamee, Remarks, Account_Code, parent_account, ManualEntrty)
    VALUES
    ((SELECT ISNULL(MAX(RevenuesID), 0) + 1 FROM dbo.TblRevenuesTypes), @Name, @NameEnglish, @Remarks, @AccountCode, @ParentAccount, 0);
END", connection, transaction))
            {
                command.Parameters.AddWithValue("@AccountCode", Limit(accountCode, 500));
                command.Parameters.AddWithValue("@Name", name);
                command.Parameters.AddWithValue("@NameEnglish", nameEnglish);
                command.Parameters.AddWithValue("@Remarks", DBNull.Value);
                command.Parameters.AddWithValue("@ParentAccount", (object)parentAccount ?? DBNull.Value);
                command.ExecuteNonQuery();
            }
        }

        private void EnsureCashBoxData(SqlConnection connection, SqlTransaction transaction, MasterDataImportRowViewModel row, string accountCode, bool isCustody)
        {
            if (!TableExists(connection, transaction, "TblBoxesData"))
            {
                return;
            }

            var branch = GetDefaultBranch(connection, transaction);
            var options = GetSystemOptions(connection, transaction);
            var boxName = Limit(row.AccountName, 100);
            var boxNameEnglish = Limit(string.IsNullOrWhiteSpace(row.AccountNameEnglish) ? (row.AccountName ?? string.Empty) : row.AccountNameEnglish, 255);
            var parentAccount = branch.GetAccount(isCustody ? 35 : 6);
            var createChequeBox = options.ChequeBox && !isCustody;
            var groupParent = createChequeBox || options.BoxLossAndIncrease ? EnsureChildAccount(connection, transaction, parentAccount, boxName, boxNameEnglish, false, null) : string.Empty;
            var accountCode1 = createChequeBox ? EnsureChildAccount(connection, transaction, groupParent, " \u062d\u0627\u0641\u0638\u0629 \u0634\u064a\u0643\u0627\u062a  " + boxName, boxNameEnglish + "  Cheque Box", true, null) : string.Empty;
            var accountCode2 = options.BoxLossAndIncrease ? EnsureChildAccount(connection, transaction, groupParent, " \u0639\u062c\u0632 \u0648\u0632\u064a\u0627\u062f\u0629 \u0627\u0644\u0646\u0642\u062f\u064a\u0629-    " + boxName, boxNameEnglish + "  Loss And Increase", true, null) : string.Empty;

            using (var command = new SqlCommand(@"IF EXISTS (SELECT 1 FROM dbo.TblBoxesData WHERE Account_Code = @AccountCode)
BEGIN
    UPDATE dbo.TblBoxesData
    SET BoxName = COALESCE(NULLIF(@BoxName, N''), BoxName),
        BoxNameE = COALESCE(NULLIF(@NameEnglish, N''), BoxNameE),
        Type = @BoxType,
        BTtype = CASE WHEN @BoxType = 1 THEN COALESCE(BTtype, 1) ELSE BTtype END,
        BranchId = @BranchId,
        ParentAccount = COALESCE(NULLIF(@ParentAccount, N''), ParentAccount),
        parent_account = COALESCE(NULLIF(@BaseParentAccount, N''), parent_account),
        Account_Code1 = COALESCE(NULLIF(@AccountCode1, N''), Account_Code1),
        Account_Code2 = COALESCE(NULLIF(@AccountCode2, N''), Account_Code2),
        ChequeBox = @ChequeBox
    WHERE Account_Code = @AccountCode;
END
ELSE
BEGIN
    INSERT INTO dbo.TblBoxesData
    (BoxID, BoxName, BoxNameE, Account_Code, Account_Code1, Account_Code2, ParentAccount, parent_account, Type, BTtype, BranchId, ChequeBox, boxValue)
    VALUES
    ((SELECT ISNULL(MAX(BoxID), 0) + 1 FROM dbo.TblBoxesData), @BoxName, @NameEnglish, @AccountCode, @AccountCode1, @AccountCode2, @ParentAccount, @BaseParentAccount, @BoxType, CASE WHEN @BoxType = 1 THEN 1 ELSE NULL END, @BranchId, @ChequeBox, 0);
END", connection, transaction))
            {
                command.Parameters.AddWithValue("@AccountCode", accountCode);
                command.Parameters.AddWithValue("@BoxName", boxName);
                command.Parameters.AddWithValue("@NameEnglish", boxNameEnglish);
                command.Parameters.AddWithValue("@BranchId", branch.BranchId);
                command.Parameters.AddWithValue("@ParentAccount", Limit(groupParent, 255));
                command.Parameters.AddWithValue("@BaseParentAccount", Limit(parentAccount, 55));
                command.Parameters.AddWithValue("@AccountCode1", Limit(accountCode1, 255));
                command.Parameters.AddWithValue("@AccountCode2", Limit(accountCode2, 255));
                command.Parameters.AddWithValue("@ChequeBox", createChequeBox);
                command.Parameters.AddWithValue("@BoxType", isCustody ? 1 : 0);
                command.ExecuteNonQuery();
            }
        }

        private void EnsureStoreData(SqlConnection connection, SqlTransaction transaction, MasterDataImportRowViewModel row, string accountCode)
        {
            if (!TableExists(connection, transaction, "TblStore"))
            {
                return;
            }

            var branch = GetDefaultBranch(connection, transaction);
            var options = GetSystemOptions(connection, transaction);
            var storeName = Limit(row.AccountName, 50);
            var storeNameEnglish = Limit(string.IsNullOrWhiteSpace(row.AccountNameEnglish) ? row.AccountName : row.AccountNameEnglish, 255);
            var inventoryParent = branch.GetAccount(0);
            var settlementParent = branch.GetAccount(11);
            var lossParent = branch.GetAccount(options.EachStoreHaveLossAccount ? 10 : 75);
            var giftParent = branch.GetAccount(options.EachStoreHaveGiftAccount ? 17 : 76);
            var settlementAccount = options.StoreAccountHaveSettlement
                ? EnsureChildAccount(connection, transaction, row.ResolvedParentAccountCode, "    التسويات الجردية  " + storeName, storeNameEnglish + " Accumulated depreciation", true, null)
                : EnsureChildAccount(connection, transaction, settlementParent, "    التسويات الجردية  " + storeName, storeNameEnglish + " Accumulated Depreciation", true, null);
            var lossAccount = options.EachStoreHaveLossAccount
                ? EnsureChildAccount(connection, transaction, lossParent, "  فقد وتلف   " + storeName, storeNameEnglish + " - Loss and damage", true, null)
                : lossParent;
            var giftAccount = options.EachStoreHaveGiftAccount
                ? EnsureChildAccount(connection, transaction, giftParent, "  هدايا وعينات     " + storeName, storeNameEnglish + " -Gifts & Samples", true, null)
                : giftParent;

            using (var command = new SqlCommand(@"IF EXISTS (SELECT 1 FROM dbo.TblStore WHERE Account_Code = @AccountCode)
BEGIN
    UPDATE dbo.TblStore
    SET StoreName = COALESCE(NULLIF(@StoreName, N''), StoreName),
        StoreNamee = COALESCE(NULLIF(@StoreNameEnglish, N''), StoreNamee),
        Code = COALESCE(NULLIF(@Code, N''), Code),
        ParetnAccount = COALESCE(NULLIF(@ParentAccount, N''), ParetnAccount),
        Account_Code0 = COALESCE(NULLIF(@InventoryParent, N''), Account_Code0),
        Account_Code11 = COALESCE(NULLIF(@LossParent, N''), Account_Code11),
        Account_Code22 = COALESCE(NULLIF(@SettlementParent, N''), Account_Code22),
        Account_Code33 = COALESCE(NULLIF(@GiftParent, N''), Account_Code33),
        Account_Code1 = COALESCE(NULLIF(@LossAccount, N''), Account_Code1),
        Account_Code2 = COALESCE(NULLIF(@SettlementAccount, N''), Account_Code2),
        Account_Code3 = COALESCE(NULLIF(@GiftAccount, N''), Account_Code3),
        BranchId = @BranchId,
        linked = 1
    WHERE Account_Code = @AccountCode;
END
ELSE
BEGIN
    INSERT INTO dbo.TblStore
    (StoreID, StoreName, StoreNamee, Account_Code, Account_Code1, Account_Code2, Account_Code3, linked, BranchId, Code, ParetnAccount,
     Account_Code0, Account_Code11, Account_Code22, Account_Code33, IsLab, IsNotCreateEntry)
    VALUES
    ((SELECT ISNULL(MAX(StoreID), 0) + 1 FROM dbo.TblStore), @StoreName, @StoreNameEnglish, @AccountCode, @LossAccount, @SettlementAccount, @GiftAccount,
     1, @BranchId, @Code, @ParentAccount, @InventoryParent, @LossParent, @SettlementParent, @GiftParent, 0, 0);
END", connection, transaction))
            {
                command.Parameters.AddWithValue("@AccountCode", accountCode);
                command.Parameters.AddWithValue("@StoreName", storeName);
                command.Parameters.AddWithValue("@StoreNameEnglish", storeNameEnglish);
                command.Parameters.AddWithValue("@Code", Limit(string.IsNullOrWhiteSpace(row.AccountSerial) ? accountCode : row.AccountSerial, 255));
                command.Parameters.AddWithValue("@ParentAccount", Limit(row.ResolvedParentAccountCode, 255));
                command.Parameters.AddWithValue("@InventoryParent", Limit(inventoryParent, 255));
                command.Parameters.AddWithValue("@LossParent", Limit(lossParent, 255));
                command.Parameters.AddWithValue("@SettlementParent", Limit(settlementParent, 255));
                command.Parameters.AddWithValue("@GiftParent", Limit(giftParent, 255));
                command.Parameters.AddWithValue("@LossAccount", Limit(lossAccount, 50));
                command.Parameters.AddWithValue("@SettlementAccount", Limit(settlementAccount, 50));
                command.Parameters.AddWithValue("@GiftAccount", Limit(giftAccount, 50));
                command.Parameters.AddWithValue("@BranchId", branch.BranchId);
                command.ExecuteNonQuery();
            }
        }

        private void EnsureCustomerOrSupplierData(SqlConnection connection, SqlTransaction transaction, MasterDataImportRowViewModel row, string accountCode, int type)
        {
            if (!TableExists(connection, transaction, "TblCustemers"))
            {
                return;
            }

            var branch = GetDefaultBranch(connection, transaction);
            var options = GetSystemOptions(connection, transaction);
            var name = row.AccountName ?? string.Empty;
            var nameEnglish = string.IsNullOrWhiteSpace(row.AccountNameEnglish) ? name : row.AccountNameEnglish;
            var code = Limit(string.IsNullOrWhiteSpace(row.AccountSerial) ? accountCode : row.AccountSerial, 255);
            var parentAccount = type == 1 ? branch.GetAccount(8) : type == 2 ? branch.GetAccount(9) : branch.GetAccount(36);
            var threeAccounts = type == 1 ? options.CustomerHaveThreeAccounts : type == 2 ? options.SupplierHaveThreeAccounts : options.SubContractorHaveThreeAccounts;
            var createFourAccounts = type == 1 ? options.CustomerCreateFourAccounts : type == 2 ? options.SupplierCreateFourAccounts : options.SupplierCreateFourAccounts;
            var accountCode1 = string.Empty;
            var accountCode2 = string.Empty;
            var accountCodeAss2 = string.Empty;
            var accountCodeHi1 = string.Empty;
            var accountCodeHi2 = string.Empty;
            var parentAccountCurrentAss = string.Empty;
            var parentAccountCurrentHih = string.Empty;
            var groupParent = string.Empty;

            if (threeAccounts)
            {
                groupParent = EnsureChildAccount(connection, transaction, parentAccount, name, nameEnglish, false, null);
                if (!string.IsNullOrWhiteSpace(groupParent))
                {
                    accountCode1 = EnsureChildAccount(connection, transaction, groupParent, name + "   شيكات  تحت التحصيل ", nameEnglish + "  Under Collection Cheque  ", true, null);
                    accountCode2 = EnsureChildAccount(connection, transaction, groupParent, name + "   دفعات مقدمة   ", nameEnglish + " Advanced Payment  ", true, null);
                }
            }
            else if (createFourAccounts)
            {
                if (type == 1 && !string.IsNullOrWhiteSpace(branch.GetAccount(217)))
                {
                    parentAccount = branch.GetAccount(217);
                }

                parentAccountCurrentAss = branch.GetAccount(type == 1 ? 217 : type == 2 ? 218 : 223);
                parentAccountCurrentHih = branch.GetAccount(type == 1 ? 219 : type == 2 ? 218 : 221);
                if (type == 1)
                {
                    accountCodeAss2 = EnsureBranchChildAccount(connection, transaction, branch, 218, name + " ضمان الاعمال ", nameEnglish + " retention  ", code);
                    accountCodeHi1 = EnsureBranchChildAccount(connection, transaction, branch, 219, name + " دفعات مقدمة ", nameEnglish + " Advance payment   ", code);
                    accountCodeHi2 = EnsureBranchChildAccount(connection, transaction, branch, 220, name + " مواد ", nameEnglish + " Materials   ", code);
                }
                else if (type == 3)
                {
                    if (!string.IsNullOrWhiteSpace(branch.GetAccount(223)))
                    {
                        parentAccount = branch.GetAccount(223);
                    }

                    accountCodeAss2 = EnsureBranchChildAccount(connection, transaction, branch, 224, name + " \u0636\u0645\u0627\u0646 \u0627\u0644\u0627\u0639\u0645\u0627\u0644 ", nameEnglish + " retention  ", code);
                    accountCodeHi1 = EnsureBranchChildAccount(connection, transaction, branch, 221, name + " \u062f\u0641\u0639\u0627\u062a \u0645\u0642\u062f\u0645\u0629 ", nameEnglish + " Advance payment   ", code);
                    accountCodeHi2 = EnsureBranchChildAccount(connection, transaction, branch, 222, name + " \u0645\u0648\u0627\u062f ", nameEnglish + " Materials  ", code);
                }
            }

            using (var command = new SqlCommand(@"IF EXISTS (SELECT 1 FROM dbo.TblCustemers WHERE [Type] = @Type AND Account_Code = @AccountCode)
BEGIN
    UPDATE dbo.TblCustemers
    SET CusName = COALESCE(NULLIF(@Name, N''), CusName),
        CusNamee = COALESCE(NULLIF(@NameEnglish, N''), CusNamee),
        code = COALESCE(NULLIF(@Code, N''), code),
        Fullcode = COALESCE(NULLIF(@Code, N''), Fullcode),
        parent_account = COALESCE(NULLIF(@ParentAccount, N''), parent_account),
        ParentAccount = COALESCE(NULLIF(@GroupParent, N''), ParentAccount),
        Account_Code1 = COALESCE(NULLIF(@AccountCode1, N''), Account_Code1),
        Account_Code2 = COALESCE(NULLIF(@AccountCode2, N''), Account_Code2),
        ParentAccountCurrentAss = COALESCE(NULLIF(@ParentAccountCurrentAss, N''), ParentAccountCurrentAss),
        ParentAccountCurrentHih = COALESCE(NULLIF(@ParentAccountCurrentHih, N''), ParentAccountCurrentHih),
        Account_CodeAss2 = COALESCE(NULLIF(@AccountCodeAss2, N''), Account_CodeAss2),
        Account_CodeHi1 = COALESCE(NULLIF(@AccountCodeHi1, N''), Account_CodeHi1),
        Account_CodeHi2 = COALESCE(NULLIF(@AccountCodeHi2, N''), Account_CodeHi2),
        BranchId = @BranchId,
        CurrncyID = COALESCE(CurrncyID, 1)
    WHERE [Type] = @Type AND Account_Code = @AccountCode;
END
ELSE
BEGIN
    INSERT INTO dbo.TblCustemers
    (CusID, CusName, CusNamee, [Type], OpenBalance, Account_Code, Account_Code1, Account_Code2, ParentAccount,
     ParentAccountCurrentAss, ParentAccountCurrentHih, Account_CodeAss2, Account_CodeHi1, Account_CodeHi2,
     parent_account, code, Fullcode, BranchId, RecordDate, CurrncyID, CustomerandVendor, locked, Trans_Discount, Trans_DiscountType,
     Trans_DiscountPur, Trans_DiscountTypePur, DepitInterval, CreditInterval)
    VALUES
    ((SELECT ISNULL(MAX(CusID), 0) + 1 FROM dbo.TblCustemers), @Name, @NameEnglish, @Type, 0, @AccountCode, @AccountCode1, @AccountCode2, @GroupParent,
     @ParentAccountCurrentAss, @ParentAccountCurrentHih, @AccountCodeAss2, @AccountCodeHi1, @AccountCodeHi2,
     @ParentAccount, @Code, @Code, @BranchId, GETDATE(), 1, 0, 0, 0, 0, 0, 0, 0, 0);
END", connection, transaction))
            {
                command.Parameters.AddWithValue("@Type", type);
                command.Parameters.AddWithValue("@AccountCode", accountCode);
                command.Parameters.AddWithValue("@Name", name);
                command.Parameters.AddWithValue("@NameEnglish", nameEnglish);
                command.Parameters.AddWithValue("@Code", code);
                command.Parameters.AddWithValue("@ParentAccount", Limit(string.IsNullOrWhiteSpace(parentAccount) ? row.ResolvedParentAccountCode : parentAccount, 500));
                command.Parameters.AddWithValue("@GroupParent", Limit(groupParent, 255));
                command.Parameters.AddWithValue("@AccountCode1", Limit(accountCode1, 255));
                command.Parameters.AddWithValue("@AccountCode2", Limit(accountCode2, 255));
                command.Parameters.AddWithValue("@ParentAccountCurrentAss", Limit(parentAccountCurrentAss, 400));
                command.Parameters.AddWithValue("@ParentAccountCurrentHih", Limit(parentAccountCurrentHih, 400));
                command.Parameters.AddWithValue("@AccountCodeAss2", Limit(accountCodeAss2, 400));
                command.Parameters.AddWithValue("@AccountCodeHi1", Limit(accountCodeHi1, 400));
                command.Parameters.AddWithValue("@AccountCodeHi2", Limit(accountCodeHi2, 400));
                command.Parameters.AddWithValue("@BranchId", branch.BranchId);
                command.ExecuteNonQuery();
            }
        }

        private void EnsureEmployeeData(SqlConnection connection, SqlTransaction transaction, MasterDataImportRowViewModel row, string accountCode)
        {
            if (!TableExists(connection, transaction, "TblEmployee"))
            {
                return;
            }

            var branch = GetDefaultBranch(connection, transaction);
            var employeeName = CleanEmployeeName(row.AccountName);
            var employeeNameEnglish = CleanEmployeeName(string.IsNullOrWhiteSpace(row.AccountNameEnglish) ? row.AccountName : row.AccountNameEnglish);
            var nameParts = SplitEmployeeName(employeeName, 50);
            var englishNameParts = SplitEmployeeName(employeeNameEnglish, 255);
            var name = Limit(employeeName, 50);
            var nameEnglish = Limit(employeeNameEnglish, 255);
            var code = Limit(string.IsNullOrWhiteSpace(row.AccountSerial) ? accountCode : row.AccountSerial, 50);
            var accountCode1 = EnsureChildAccount(connection, transaction, branch.GetAccount(29), row.AccountName + "- اجور مستحقة", nameEnglish + "- Salary", true, null);
            var accountCode3 = EnsureChildAccount(connection, transaction, branch.GetAccount(65), row.AccountName + "مدفوعات مقدمة  ", nameEnglish + " -Adv. Payments", true, null);
            var accountCode5 = EnsureChildAccount(connection, transaction, branch.GetAccount(93), row.AccountName + "مخصص تذاكر    ", nameEnglish + " -Adv. Payments", true, null);
            var accountCode4 = EnsureChildAccount(connection, transaction, branch.GetAccount(74), row.AccountName + "مخصصات  نهاية خدمة", nameEnglish + " -Reserved End Services", true, null);
            var accountCode2Name = string.IsNullOrWhiteSpace(accountCode4) ? row.AccountName + "مخصصات " : row.AccountName + "مخصصات اجازة ";
            var accountCode2English = string.IsNullOrWhiteSpace(accountCode4) ? nameEnglish + " -Reserved" : nameEnglish + " -Reserved Vacation";
            var accountCode2 = EnsureChildAccount(connection, transaction, branch.GetAccount(30), accountCode2Name, accountCode2English, true, null);

            using (var command = new SqlCommand(@"IF EXISTS (SELECT 1 FROM dbo.TblEmployee WHERE Account_code = @AccountCode)
BEGIN
    UPDATE dbo.TblEmployee
    SET Emp_Name = COALESCE(NULLIF(@Name, N''), Emp_Name),
        Emp_Namee = COALESCE(NULLIF(@NameEnglish, N''), Emp_Namee),
        Emp_Name1 = COALESCE(NULLIF(@Name1, N''), Emp_Name1),
        Emp_Name2 = COALESCE(NULLIF(@Name2, N''), Emp_Name2),
        Emp_Name3 = COALESCE(NULLIF(@Name3, N''), Emp_Name3),
        Emp_Name4 = COALESCE(NULLIF(@Name4, N''), Emp_Name4),
        Emp_Namee1 = COALESCE(NULLIF(@NameEnglish1, N''), Emp_Namee1),
        Emp_Namee2 = COALESCE(NULLIF(@NameEnglish2, N''), Emp_Namee2),
        Emp_Namee3 = COALESCE(NULLIF(@NameEnglish3, N''), Emp_Namee3),
        Emp_Namee4 = COALESCE(NULLIF(@NameEnglish4, N''), Emp_Namee4),
        Emp_Code = COALESCE(NULLIF(@Code, N''), Emp_Code),
        Fullcode = COALESCE(NULLIF(@Code, N''), Fullcode),
        Account_code1 = COALESCE(NULLIF(@AccountCode1, N''), Account_code1),
        Account_Code2 = COALESCE(NULLIF(@AccountCode2, N''), Account_Code2),
        Account_Code3 = COALESCE(NULLIF(@AccountCode3, N''), Account_Code3),
        Account_Code4 = COALESCE(NULLIF(@AccountCode4, N''), Account_Code4),
        Account_Code5 = COALESCE(NULLIF(@AccountCode5, N''), Account_Code5),
        BranchId = @BranchId
    WHERE Account_code = @AccountCode;
END
ELSE
BEGIN
    INSERT INTO dbo.TblEmployee
    (Emp_ID, Emp_Code, Emp_Name, Emp_Name1, Emp_Name2, Emp_Name3, Emp_Name4,
     Account_code, Account_code1, Account_Code2, Account_Code3, Account_Code4, Account_Code5,
     BranchId, Emp_Namee, Emp_Namee1, Emp_Namee2, Emp_Namee3, Emp_Namee4, Fullcode, workstate, BignDateWork, SalaryType, PayType)
    VALUES
    ((SELECT ISNULL(MAX(Emp_ID), 0) + 1 FROM dbo.TblEmployee), @Code, @Name, @Name1, @Name2, @Name3, @Name4,
     @AccountCode, @AccountCode1, @AccountCode2, @AccountCode3, @AccountCode4, @AccountCode5,
     @BranchId, @NameEnglish, @NameEnglish1, @NameEnglish2, @NameEnglish3, @NameEnglish4, @Code, 0, GETDATE(), 0, 0);
END", connection, transaction))
            {
                command.Parameters.AddWithValue("@AccountCode", accountCode);
                command.Parameters.AddWithValue("@Name", name);
                command.Parameters.AddWithValue("@NameEnglish", nameEnglish);
                command.Parameters.AddWithValue("@Name1", nameParts.Part1);
                command.Parameters.AddWithValue("@Name2", nameParts.Part2);
                command.Parameters.AddWithValue("@Name3", nameParts.Part3);
                command.Parameters.AddWithValue("@Name4", nameParts.Part4);
                command.Parameters.AddWithValue("@NameEnglish1", englishNameParts.Part1);
                command.Parameters.AddWithValue("@NameEnglish2", englishNameParts.Part2);
                command.Parameters.AddWithValue("@NameEnglish3", englishNameParts.Part3);
                command.Parameters.AddWithValue("@NameEnglish4", englishNameParts.Part4);
                command.Parameters.AddWithValue("@Code", code);
                command.Parameters.AddWithValue("@AccountCode1", Limit(accountCode1, 50));
                command.Parameters.AddWithValue("@AccountCode2", Limit(accountCode2, 50));
                command.Parameters.AddWithValue("@AccountCode3", Limit(accountCode3, 255));
                command.Parameters.AddWithValue("@AccountCode4", Limit(accountCode4, 255));
                command.Parameters.AddWithValue("@AccountCode5", Limit(accountCode5, 255));
                command.Parameters.AddWithValue("@BranchId", branch.BranchId);
                command.ExecuteNonQuery();
            }
        }

        private static string CleanEmployeeName(string value)
        {
            value = Clean(value);
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            value = value
                .Replace("\t", " ")
                .Replace("/", " ")
                .Replace("\\", " ")
                .Replace("-", " ")
                .Replace(":", " ")
                .Replace("\u2013", " ")
                .Replace("\u2014", " ");

            var prefixes = new[]
            {
                "\u0630\u0645\u0645 \u0645\u062f\u064a\u0646\u0629",
                "\u0630\u0645\u0645 \u0627\u0644\u0645\u0648\u0638\u0641\u064a\u0646",
                "\u0630\u0645\u0645",
                "\u0630\u0645\u0629",
                "\u0630\u0645\u0647",
                "employee receivable",
                "employees receivable",
                "employee",
                "staff"
            };

            foreach (var prefix in prefixes)
            {
                if (value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    value = value.Substring(prefix.Length).Trim();
                    break;
                }
            }

            return string.Join(" ", value.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
        }

        private static EmployeeNameParts SplitEmployeeName(string value, int partLength)
        {
            value = Clean(value);
            var parts = value.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var result = new EmployeeNameParts();
            if (parts.Length == 0)
            {
                return result;
            }

            result.Part1 = Limit(parts[0], partLength);
            result.Part2 = parts.Length > 1 ? Limit(parts[1], partLength) : string.Empty;
            result.Part3 = parts.Length > 2 ? Limit(parts[2], partLength) : string.Empty;
            result.Part4 = parts.Length > 3 ? Limit(string.Join(" ", parts.Skip(3).ToArray()), partLength) : string.Empty;
            return result;
        }

        private void EnsureProjectData(SqlConnection connection, SqlTransaction transaction, MasterDataImportRowViewModel row, string accountCode)
        {
            if (!TableExists(connection, transaction, "projects"))
            {
                return;
            }

            var branch = GetDefaultBranch(connection, transaction);
            var name = row.AccountName ?? string.Empty;
            var nameEnglish = string.IsNullOrWhiteSpace(row.AccountNameEnglish) ? name : row.AccountNameEnglish;
            var code = Limit(string.IsNullOrWhiteSpace(row.AccountSerial) ? accountCode : row.AccountSerial, 50);
            var expenses = EnsureChildAccount(connection, transaction, branch.GetAccount(14), name + " -\u0645\u0635\u0631\u0648\u0641\u0627\u062a ", nameEnglish + " -EXPANSES", true, null);
            var revenue = EnsureChildAccount(connection, transaction, branch.GetAccount(15), name + "-\u0627\u064a\u0631\u0627\u062f\u0627\u062a ", nameEnglish + " -REVENUE", true, null);
            var material = EnsureChildAccount(connection, transaction, branch.GetAccount(27), name + " -\u0645\u0648\u0627\u062f  ", nameEnglish + " -Material ", true, null);
            var salary = EnsureChildAccount(connection, transaction, branch.GetAccount(28), name + " -\u0627\u062c\u0648\u0631 ", nameEnglish + " -salary", true, null);
            var legal = EnsureChildAccount(connection, transaction, branch.GetAccount(32), name + " -\u0645\u0633\u062a\u062e\u0644\u0635\u0627\u062a ", nameEnglish + " -legal", true, null);
            var good = EnsureChildAccount(connection, transaction, branch.GetAccount(152), name + " -\u062d\u0633\u0646 \u0627\u0644\u0627\u062f\u0627\u0621 ", nameEnglish + " -Good performance", true, null);
            var underImp = EnsureChildAccount(connection, transaction, branch.GetAccount(142), name + " -\u062a\u062d\u062a \u0627\u0644\u062a\u0646\u0641\u064a\u0630 ", nameEnglish + " -Under implementation", true, null);

            using (var command = new SqlCommand(@"IF EXISTS (SELECT 1 FROM dbo.projects WHERE Project_account = @AccountCode OR Fullcode = @Code)
BEGIN
    UPDATE dbo.projects
    SET Project_name = COALESCE(NULLIF(@Name, N''), Project_name),
        Project_nameE = COALESCE(NULLIF(@NameEnglish, N''), Project_nameE),
        Project_account = COALESCE(NULLIF(@AccountCode, N''), Project_account),
        expanses_account = COALESCE(NULLIF(@Expenses, N''), expanses_account),
        REVENUE_account = COALESCE(NULLIF(@Revenue, N''), REVENUE_account),
        Material_account = COALESCE(NULLIF(@Material, N''), Material_account),
        Salary_account = COALESCE(NULLIF(@Salary, N''), Salary_account),
        legal = COALESCE(NULLIF(@Legal, N''), legal),
        AcountGood = COALESCE(NULLIF(@Good, N''), AcountGood),
        AccountUnderImp = COALESCE(NULLIF(@UnderImp, N''), AccountUnderImp),
        branch_no = @BranchId
    WHERE Project_account = @AccountCode OR Fullcode = @Code;
END
ELSE
BEGIN
    INSERT INTO dbo.projects
    (id, Code, Fullcode, Project_name, Project_nameE, Project_account, expanses_account, REVENUE_account,
     Material_account, Salary_account, legal, AcountGood, AccountUnderImp, branch_no, CurrencyID, StartDate)
    VALUES
    ((SELECT ISNULL(MAX(id), 0) + 1 FROM dbo.projects), @Code, @Code, @Name, @NameEnglish, @AccountCode, @Expenses, @Revenue,
     @Material, @Salary, @Legal, @Good, @UnderImp, @BranchId, 1, GETDATE());
END", connection, transaction))
            {
                command.Parameters.AddWithValue("@AccountCode", accountCode);
                command.Parameters.AddWithValue("@Code", code);
                command.Parameters.AddWithValue("@Name", name);
                command.Parameters.AddWithValue("@NameEnglish", nameEnglish);
                command.Parameters.AddWithValue("@Expenses", Limit(expenses, 50));
                command.Parameters.AddWithValue("@Revenue", Limit(revenue, 50));
                command.Parameters.AddWithValue("@Material", Limit(material, 50));
                command.Parameters.AddWithValue("@Salary", Limit(salary, 50));
                command.Parameters.AddWithValue("@Legal", Limit(legal, 50));
                command.Parameters.AddWithValue("@Good", Limit(good, 55));
                command.Parameters.AddWithValue("@UnderImp", Limit(underImp, 55));
                command.Parameters.AddWithValue("@BranchId", branch.BranchId);
                command.ExecuteNonQuery();
            }
        }

        private void EnsureFixedAssetGroupData(SqlConnection connection, SqlTransaction transaction, MasterDataImportRowViewModel row, string accountCode)
        {
            if (!TableExists(connection, transaction, "FixedAssetsGroup"))
            {
                return;
            }

            var branch = GetDefaultBranch(connection, transaction);
            var options = GetSystemOptions(connection, transaction);
            var name = row.AccountName ?? string.Empty;
            var nameEnglish = string.IsNullOrWhiteSpace(row.AccountNameEnglish) ? name : row.AccountNameEnglish;
            var code = Limit(string.IsNullOrWhiteSpace(row.AccountSerial) ? accountCode : row.AccountSerial, 255);
            var assetParent = branch.GetAccount(24);
            var expenseParent = branch.GetAccount(25);
            var accumulatedParent = branch.GetAccount(26);
            var parentGroupId = ResolveFixedAssetGroupParentId(connection, transaction, row);
            var assetValue = accountCode;
            var expense = EnsureChildAccount(connection, transaction, expenseParent, "  \u0645\u0635\u0631\u0648\u0641\u0627 \u062a  " + name, nameEnglish + " Expenses ", true, null);
            var accumulated = EnsureChildAccount(connection, transaction, accumulatedParent, "   \u0645\u062c\u0645\u0639 \u0627\u0647\u0644\u0627\u0643   " + name, nameEnglish + " Accumulated depreciation", true, null);
            var saleProfit = options.AssetAccount1 ? EnsureChildAccount(connection, transaction, branch.GetAccount(31), "  \u0627\u0631\u0628\u0627\u062d \u0628\u064a\u0639   " + name, nameEnglish + " Sale Profit ", true, null) : string.Empty;
            var saleLoss = options.AssetAccount1 ? EnsureChildAccount(connection, transaction, branch.GetAccount(40), " \u062e\u0633\u0627\u0631\u0629 \u0628\u064a\u0639   " + name, nameEnglish + " Sale Loss ", true, null) : string.Empty;
            var lastGroup = row.IsFinalAccount ? 1 : 0;

            using (var command = new SqlCommand(@"IF EXISTS (SELECT 1 FROM dbo.FixedAssetsGroup WHERE Account_Code = @AccountCode OR Fullcode = @Code)
BEGIN
    UPDATE dbo.FixedAssetsGroup
    SET GroupName = COALESCE(NULLIF(@Name, N''), GroupName),
        GroupNamee = COALESCE(NULLIF(@NameEnglish, N''), GroupNamee),
        Account_Code = @AssetValue,
        Account_Code1 = COALESCE(NULLIF(@Expense, N''), Account_Code1),
        Account_Code2 = COALESCE(NULLIF(@Accumulated, N''), Account_Code2),
        Account_Code3 = COALESCE(NULLIF(@SaleProfit, N''), Account_Code3),
        Account_Code4 = COALESCE(NULLIF(@SaleLoss, N''), Account_Code4),
        ParentExpensesAccount = COALESCE(NULLIF(@ExpenseParent, N''), ParentExpensesAccount),
        ParentEAssetAccount = COALESCE(NULLIF(@AssetParent, N''), ParentEAssetAccount),
        LastGroup = @LastGroup,
        ParentID = CASE WHEN ParentID IS NULL OR ParentID = 0 THEN @ParentGroupId ELSE ParentID END
    WHERE Account_Code = @AccountCode OR Fullcode = @Code;
END
ELSE
BEGIN
    INSERT INTO dbo.FixedAssetsGroup
    (GroupID, GroupName, GroupNamee, GroupCode, Fullcode, LastGroup, ParentID, Account_Code, Account_Code1, Account_Code2,
     Account_Code3, Account_Code4, ParentExpensesAccount, ParentEAssetAccount, Percentage1, Percentage2, DepType)
    VALUES
    ((SELECT ISNULL(MAX(GroupID), 0) + 1 FROM dbo.FixedAssetsGroup), @Name, @NameEnglish, @Code, @Code, @LastGroup, @ParentGroupId, @AssetValue, @Expense, @Accumulated,
     @SaleProfit, @SaleLoss, @ExpenseParent, @AssetParent, 0, 0, 1);
END", connection, transaction))
            {
                command.Parameters.AddWithValue("@AccountCode", accountCode);
                command.Parameters.AddWithValue("@Code", code);
                command.Parameters.AddWithValue("@Name", name);
                command.Parameters.AddWithValue("@NameEnglish", nameEnglish);
                command.Parameters.AddWithValue("@AssetValue", assetValue);
                command.Parameters.AddWithValue("@Expense", Limit(expense, 255));
                command.Parameters.AddWithValue("@Accumulated", Limit(accumulated, 255));
                command.Parameters.AddWithValue("@SaleProfit", Limit(saleProfit, 255));
                command.Parameters.AddWithValue("@SaleLoss", Limit(saleLoss, 255));
                command.Parameters.AddWithValue("@ExpenseParent", Limit(expenseParent, 255));
                command.Parameters.AddWithValue("@AssetParent", Limit(assetParent, 255));
                command.Parameters.AddWithValue("@ParentGroupId", parentGroupId);
                command.Parameters.AddWithValue("@LastGroup", lastGroup);
                command.ExecuteNonQuery();
            }
        }

        private int ResolveFixedAssetGroupParentId(SqlConnection connection, SqlTransaction transaction, MasterDataImportRowViewModel row)
        {
            var parentAccountCode = row == null ? string.Empty : row.ResolvedParentAccountCode;
            if (!string.IsNullOrWhiteSpace(parentAccountCode) && !string.Equals(parentAccountCode, "r", StringComparison.OrdinalIgnoreCase))
            {
                using (var command = new SqlCommand(@"SELECT TOP 1 GroupID
FROM dbo.FixedAssetsGroup
WHERE Account_Code = @ParentAccountCode
   OR Fullcode = @ParentAccountCode
ORDER BY GroupID", connection, transaction))
                {
                    command.Parameters.AddWithValue("@ParentAccountCode", parentAccountCode);
                    var result = command.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                    {
                        return Convert.ToInt32(result);
                    }
                }
            }

            return EnsureDefaultFixedAssetGroup(connection, transaction);
        }

        private int EnsureDefaultFixedAssetGroup(SqlConnection connection, SqlTransaction transaction)
        {
            using (var lookup = new SqlCommand(@"SELECT TOP 1 GroupID
FROM dbo.FixedAssetsGroup
WHERE ParentID IS NULL
   OR ParentID = 0
   OR GroupName LIKE N'%اصول%'
   OR GroupName LIKE N'%أصول%'
   OR GroupName LIKE N'%موجودات%'
   OR GroupName LIKE N'%اصول%'
   OR GroupName LIKE N'%موجودات%'
ORDER BY CASE WHEN GroupID = 1 THEN 0 ELSE 1 END, GroupID", connection, transaction))
            {
                var existing = lookup.ExecuteScalar();
                if (existing != null && existing != DBNull.Value)
                {
                    return Convert.ToInt32(existing);
                }
            }

            using (var insert = new SqlCommand(@"DECLARE @GroupId int;
SELECT @GroupId = ISNULL(MAX(GroupID), 0) + 1 FROM dbo.FixedAssetsGroup;

INSERT INTO dbo.FixedAssetsGroup
(GroupID, GroupName, GroupNamee, GroupCode, Fullcode, LastGroup, ParentID, Percentage1, Percentage2, DepType)
VALUES
(@GroupId, N'الاصول الثابتة', N'Fixed Assets', N'DEFAULT-ASSETS', N'DEFAULT-ASSETS', 0, NULL, 0, 0, 1);

SELECT @GroupId;", connection, transaction))
            {
                return Convert.ToInt32(insert.ExecuteScalar());
            }
        }

        private string EnsureBranchChildAccount(SqlConnection connection, SqlTransaction transaction, BranchDefaults branch, int index, string name, string nameEnglish, string rowCode)
        {
            var parent = branch.GetAccount(index);
            if (string.IsNullOrWhiteSpace(parent))
            {
                return string.Empty;
            }

            var prefix = branch.GetSerialPrefix(index);
            var parentSerial = LookupScalar(connection, transaction, "SELECT TOP 1 Account_Serial FROM ACCOUNTS WHERE Account_Code=@value", parent);
            var serial = string.IsNullOrWhiteSpace(prefix) || string.IsNullOrWhiteSpace(parentSerial) || string.IsNullOrWhiteSpace(rowCode)
                ? null
                : parentSerial + prefix + rowCode;

            return EnsureChildAccount(connection, transaction, parent, name, nameEnglish, true, serial);
        }

        private string EnsureChildAccount(SqlConnection connection, SqlTransaction transaction, string parentCode, string name, string nameEnglish, bool isFinal, string serial)
        {
            parentCode = Clean(parentCode);
            name = Clean(name);
            if (string.IsNullOrWhiteSpace(parentCode) || string.IsNullOrWhiteSpace(name) || parentCode == "NO account" || parentCode == "NO branch")
            {
                return string.Empty;
            }

            if (!Exists(connection, transaction, "Account_Code", parentCode))
            {
                return string.Empty;
            }

            var existing = LookupExistingAccountByName(connection, transaction, name, parentCode);
            var row = new MasterDataImportRowViewModel
            {
                AccountName = name,
                AccountNameEnglish = string.IsNullOrWhiteSpace(nameEnglish) ? name : nameEnglish,
                ResolvedParentAccountCode = parentCode,
                IsFinalAccount = isFinal,
                CurrencyCode = "1"
            };

            if (!string.IsNullOrWhiteSpace(serial))
            {
                var serialOwner = LookupScalar(connection, transaction, "SELECT TOP 1 Account_Code FROM ACCOUNTS WHERE Account_Serial=@value", serial);
                if (string.IsNullOrWhiteSpace(serialOwner) || string.Equals(serialOwner, existing, StringComparison.OrdinalIgnoreCase))
                {
                    row.AccountSerial = serial;
                }
            }

            var parentValues = GetParentProperties(connection, transaction, parentCode);
            row.AccountTypes = parentValues.AccountTypes;
            row.AccountTab = parentValues.AccountTab;
            row.DebitOrCredit = parentValues.DebitOrCredit;
            row.DifferentType = parentValues.DifferentType;
            row.Authority = parentValues.Authority;

            MarkParentAsGroup(connection, transaction, parentCode);
            return string.IsNullOrWhiteSpace(existing)
                ? InsertAccount(connection, transaction, row)
                : UpdateAccount(connection, transaction, row, existing);
        }

        private static BranchDefaults GetDefaultBranch(SqlConnection connection, SqlTransaction transaction)
        {
            if (!TableExists(connection, transaction, "branches"))
            {
                return new BranchDefaults { BranchId = GetFirstBranchId(connection, transaction) };
            }

            using (var command = new SqlCommand("SELECT TOP 1 * FROM dbo.branches ORDER BY branch_id", connection, transaction))
            using (var reader = command.ExecuteReader())
            {
                if (!reader.Read())
                {
                    return new BranchDefaults { BranchId = GetFirstBranchId(connection, transaction) };
                }

                var branch = new BranchDefaults { BranchId = reader["branch_id"] == DBNull.Value ? 0 : Convert.ToInt32(reader["branch_id"]) };
                for (var index = 0; index < reader.FieldCount; index++)
                {
                    var name = reader.GetName(index);
                    var value = reader[index] == DBNull.Value ? string.Empty : Convert.ToString(reader[index]);
                    branch.Values[name] = Clean(value);
                }

                if (branch.BranchId <= 0)
                {
                    branch.BranchId = GetFirstBranchId(connection, transaction);
                }

                return branch;
            }
        }

        private static int GetFirstBranchId(SqlConnection connection, SqlTransaction transaction)
        {
            if (TableExists(connection, transaction, "branches"))
            {
                var branchId = LookupInt(connection, transaction, "SELECT TOP 1 branch_id FROM dbo.branches WHERE branch_id IS NOT NULL ORDER BY branch_id");
                if (branchId > 0)
                {
                    return branchId;
                }
            }

            if (TableExists(connection, transaction, "TblBranchesData"))
            {
                var branchId = LookupInt(connection, transaction, "SELECT TOP 1 branch_id FROM dbo.TblBranchesData WHERE branch_id IS NOT NULL ORDER BY branch_id");
                if (branchId > 0)
                {
                    return branchId;
                }
            }

            return 0;
        }

        private static int LookupInt(SqlConnection connection, SqlTransaction transaction, string sql)
        {
            using (var command = new SqlCommand(sql, connection, transaction))
            {
                var result = command.ExecuteScalar();
                return result == null || result == DBNull.Value ? 0 : Convert.ToInt32(result);
            }
        }

        private static SystemOptionValues GetSystemOptions(SqlConnection connection, SqlTransaction transaction)
        {
            return new SystemOptionValues
            {
                CustomerHaveThreeAccounts = GetOptionBool(connection, transaction, "CustomerhavethreeAccounts"),
                SupplierHaveThreeAccounts = GetOptionBool(connection, transaction, "SupplierhavethreeAccounts"),
                SubContractorHaveThreeAccounts = GetOptionBool(connection, transaction, "SubContactorHave3Account"),
                CustomerCreateFourAccounts = GetOptionBool(connection, transaction, "CustCreat4Acc"),
                SupplierCreateFourAccounts = GetOptionBool(connection, transaction, "SuppCreat4Acc"),
                StoreAccountHaveSettlement = GetOptionBool(connection, transaction, "StoreAccountHaveSettelment"),
                EachStoreHaveLossAccount = GetOptionBool(connection, transaction, "eachStoreHaveLossAccount"),
                EachStoreHaveGiftAccount = GetOptionBool(connection, transaction, "eachStoreHaveGiftAccount"),
                BankAccounts = GetOptionBool(connection, transaction, "banks_Accounts"),
                BankCommission = GetOptionBool(connection, transaction, "BankComm"),
                ChequeBox = GetOptionBool(connection, transaction, "ChequeBox"),
                BoxLossAndIncrease = GetOptionBool(connection, transaction, "BoxLossandIncreae"),
                AssetAccount = GetOptionBool(connection, transaction, "AssetAccount"),
                AssetAccount1 = GetOptionBool(connection, transaction, "AssetAccount1")
            };
        }

        private static bool GetOptionBool(SqlConnection connection, SqlTransaction transaction, string columnName)
        {
            if (!ColumnExists(connection, transaction, "TblOptions", columnName))
            {
                return false;
            }

            using (var command = new SqlCommand("SELECT TOP 1 [" + columnName + "] FROM dbo.TblOptions", connection, transaction))
            {
                var result = command.ExecuteScalar();
                if (result == null || result == DBNull.Value)
                {
                    return false;
                }

                return Convert.ToBoolean(result);
            }
        }

        private static string Limit(string value, int maxLength)
        {
            value = value ?? string.Empty;
            return value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }

        private static bool TableExists(SqlConnection connection, SqlTransaction transaction, string tableName)
        {
            using (var command = new SqlCommand("SELECT OBJECT_ID(N'dbo.' + @TableName, N'U')", connection, transaction))
            {
                command.Parameters.AddWithValue("@TableName", tableName);
                var result = command.ExecuteScalar();
                return result != null && result != DBNull.Value;
            }
        }

        private static bool ColumnExists(SqlConnection connection, SqlTransaction transaction, string tableName, string columnName)
        {
            using (var command = new SqlCommand(@"SELECT 1
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = @TableName AND COLUMN_NAME = @ColumnName", connection, transaction))
            {
                command.Parameters.AddWithValue("@TableName", tableName);
                command.Parameters.AddWithValue("@ColumnName", columnName);
                var result = command.ExecuteScalar();
                return result != null && result != DBNull.Value;
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
                        DifferentType = ReadNullableInt(reader, "Differenttype") ?? 1,
                        Authority = ReadNullableInt(reader, "Authority")
                    };
                }
            }
        }

        private static int? ReadNullableInt(IDataRecord reader, string name)
        {
            return reader[name] == DBNull.Value ? (int?)null : Convert.ToInt32(reader[name]);
        }

        private static bool ReadBool(IDataRecord reader, string name)
        {
            return reader[name] != DBNull.Value && Convert.ToBoolean(reader[name]);
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

        private static string BuildValidationSummary(IEnumerable<MasterDataImportRowViewModel> rows)
        {
            var errors = rows.Where(r => !r.IsValid)
                .Take(5)
                .Select(r => "Row " + r.RowNumber + ": " + r.ErrorDetails)
                .ToList();

            return errors.Count == 0
                ? "Import stopped because validation errors exist."
                : "Import stopped because validation errors exist. " + string.Join(" | ", errors);
        }

        private int CreateBatch(SqlConnection connection, SqlTransaction transaction, string fileName, string userName, int totalRows, int successRows, int failedRows)
        {
            EnsureBatchTable(connection, transaction);

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

        private static void EnsureBatchTable(SqlConnection connection, SqlTransaction transaction)
        {
            using (var command = new SqlCommand(@"
IF OBJECT_ID(N'dbo.MasterDataImportBatch', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.MasterDataImportBatch
    (
        BatchId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_MasterDataImportBatch PRIMARY KEY,
        FileName nvarchar(260) NOT NULL,
        EntityType nvarchar(80) NOT NULL,
        ImportedBy nvarchar(256) NULL,
        ImportStartedAt datetime NOT NULL,
        ImportFinishedAt datetime NULL,
        TotalRows int NOT NULL,
        SuccessRows int NOT NULL,
        FailedRows int NOT NULL,
        Status nvarchar(30) NOT NULL,
        ErrorMessage nvarchar(max) NULL
    );
END
IF OBJECT_ID(N'dbo.MasterDataImportBatchDetail', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.MasterDataImportBatchDetail
    (
        DetailId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_MasterDataImportBatchDetail PRIMARY KEY,
        BatchId int NOT NULL,
        TableName nvarchar(128) NOT NULL,
        RecordKey nvarchar(255) NOT NULL,
        RecordSerial nvarchar(255) NULL,
        RowNumber int NULL,
        ActionType nvarchar(30) NOT NULL,
        CreatedAt datetime NOT NULL CONSTRAINT DF_MasterDataImportBatchDetail_CreatedAt DEFAULT(GETDATE())
    );
    CREATE INDEX IX_MasterDataImportBatchDetail_Batch ON dbo.MasterDataImportBatchDetail(BatchId, TableName, ActionType);
END
IF COL_LENGTH('dbo.MasterDataImportBatchDetail', 'RecordSerial') IS NULL
    ALTER TABLE dbo.MasterDataImportBatchDetail ADD RecordSerial nvarchar(255) NULL;
IF COL_LENGTH('dbo.MasterDataImportBatchDetail', 'RowNumber') IS NULL
    ALTER TABLE dbo.MasterDataImportBatchDetail ADD RowNumber int NULL;", connection, transaction))
            {
                command.ExecuteNonQuery();
            }
        }

        private static object Scalar(SqlConnection connection, SqlTransaction transaction, string sql)
        {
            using (var command = new SqlCommand(sql, connection, transaction))
            {
                var result = command.ExecuteScalar();
                return result == null || result == DBNull.Value ? 0 : result;
            }
        }

        private static object Scalar(SqlConnection connection, SqlTransaction transaction, string sql, int batchId)
        {
            using (var command = new SqlCommand(sql, connection, transaction))
            {
                command.Parameters.AddWithValue("@BatchId", batchId);
                var result = command.ExecuteScalar();
                return result == null || result == DBNull.Value ? 0 : result;
            }
        }

        private static int SeedRollbackDetailsFromAccountDates(SqlConnection connection, SqlTransaction transaction, int batchId)
        {
            EnsureBatchTable(connection, transaction);
            using (var command = new SqlCommand(@"
DECLARE @Started datetime;
DECLARE @Finished datetime;

SELECT
    @Started = ImportStartedAt,
    @Finished = ISNULL(ImportFinishedAt, DATEADD(minute, 10, ImportStartedAt))
FROM dbo.MasterDataImportBatch
WHERE BatchId = @BatchId
  AND EntityType = N'ChartOfAccounts'
  AND Status = N'Completed';

IF @Started IS NULL
BEGIN
    SELECT 0;
    RETURN;
END

INSERT INTO dbo.MasterDataImportBatchDetail
(BatchId, TableName, RecordKey, RecordSerial, RowNumber, ActionType)
SELECT @BatchId, N'ACCOUNTS', a.Account_Code, a.Account_Serial, NULL, N'Created'
FROM dbo.ACCOUNTS a
WHERE a.DateCreated >= @Started
  AND a.DateCreated <= DATEADD(minute, 2, @Finished)
  AND NOT EXISTS
  (
      SELECT 1
      FROM dbo.MasterDataImportBatchDetail d
      WHERE d.BatchId = @BatchId
        AND d.TableName = N'ACCOUNTS'
        AND d.RecordKey = a.Account_Code
        AND d.ActionType = N'Created'
  );

SELECT @@ROWCOUNT;", connection, transaction))
            {
                command.Parameters.AddWithValue("@BatchId", batchId);
                return Convert.ToInt32(command.ExecuteScalar());
            }
        }

        private static int DeleteCreatedAccounts(SqlConnection connection, SqlTransaction transaction, int batchId)
        {
            using (var command = new SqlCommand(@"
IF OBJECT_ID('tempdb..#RollbackAccounts') IS NOT NULL DROP TABLE #RollbackAccounts;

;WITH Seed AS
(
    SELECT DISTINCT RecordKey AS Account_Code
    FROM dbo.MasterDataImportBatchDetail
    WHERE BatchId=@BatchId AND TableName=N'ACCOUNTS' AND ActionType=N'Created'
),
Tree AS
(
    SELECT a.Account_Code, a.Parent_Account_Code, 0 AS Depth
    FROM dbo.ACCOUNTS a
    INNER JOIN Seed s ON s.Account_Code = a.Account_Code
    UNION ALL
    SELECT c.Account_Code, c.Parent_Account_Code, t.Depth + 1
    FROM dbo.ACCOUNTS c
    INNER JOIN Tree t ON c.Parent_Account_Code = t.Account_Code
)
SELECT Account_Code, MAX(Depth) AS Depth
INTO #RollbackAccounts
FROM Tree
GROUP BY Account_Code;

DELETE c FROM dbo.TblCustemers c INNER JOIN #RollbackAccounts r ON r.Account_Code = c.Account_Code;
DELETE s FROM dbo.TblStore s INNER JOIN #RollbackAccounts r ON r.Account_Code = s.Account_Code;
DELETE b FROM dbo.BanksData b INNER JOIN #RollbackAccounts r ON r.Account_Code = b.Account_Code;
DELETE x FROM dbo.TblBoxesData x INNER JOIN #RollbackAccounts r ON r.Account_Code = x.Account_Code;
DELETE ex FROM dbo.ExpensesType ex INNER JOIN #RollbackAccounts r ON r.Account_Code = ex.Account_Code;
DELETE rv FROM dbo.TblRevenuesTypes rv INNER JOIN #RollbackAccounts r ON r.Account_Code = rv.Account_Code;
DELETE f FROM dbo.FixedAssetsGroup f INNER JOIN #RollbackAccounts r ON r.Account_Code = f.Account_Code;
DELETE e FROM dbo.TblEmployee e INNER JOIN #RollbackAccounts r ON r.Account_Code = e.Account_code;
DELETE p FROM dbo.projects p INNER JOIN #RollbackAccounts r ON r.Account_Code = p.Project_account;

DECLARE @Deleted int;
DELETE a FROM dbo.ACCOUNTS a INNER JOIN #RollbackAccounts r ON r.Account_Code = a.Account_Code;
SET @Deleted = @@ROWCOUNT;
SELECT @Deleted;", connection, transaction))
            {
                command.Parameters.AddWithValue("@BatchId", batchId);
                return Convert.ToInt32(command.ExecuteScalar());
            }
        }

        private void CompleteBatch(SqlConnection connection, SqlTransaction transaction, int batchId, int successRows, int failedRows, string error)
        {
            EnsureBatchTable(connection, transaction);

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

        private class BranchDefaults
        {
            public BranchDefaults()
            {
                Values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }

            public int BranchId { get; set; }
            public IDictionary<string, string> Values { get; private set; }

            public string GetAccount(int index)
            {
                return GetValue("a" + index);
            }

            public string GetSerialPrefix(int index)
            {
                return GetValue("T" + index);
            }

            private string GetValue(string key)
            {
                string value;
                return Values.TryGetValue(key, out value) ? value : string.Empty;
            }
        }

        private class EmployeeNameParts
        {
            public string Part1 { get; set; }
            public string Part2 { get; set; }
            public string Part3 { get; set; }
            public string Part4 { get; set; }

            public EmployeeNameParts()
            {
                Part1 = string.Empty;
                Part2 = string.Empty;
                Part3 = string.Empty;
                Part4 = string.Empty;
            }
        }

        private class SystemOptionValues
        {
            public bool CustomerHaveThreeAccounts { get; set; }
            public bool SupplierHaveThreeAccounts { get; set; }
            public bool SubContractorHaveThreeAccounts { get; set; }
            public bool CustomerCreateFourAccounts { get; set; }
            public bool SupplierCreateFourAccounts { get; set; }
            public bool StoreAccountHaveSettlement { get; set; }
            public bool EachStoreHaveLossAccount { get; set; }
            public bool EachStoreHaveGiftAccount { get; set; }
            public bool BankAccounts { get; set; }
            public bool BankCommission { get; set; }
            public bool ChequeBox { get; set; }
            public bool BoxLossAndIncrease { get; set; }
            public bool AssetAccount { get; set; }
            public bool AssetAccount1 { get; set; }
        }
    }
}
