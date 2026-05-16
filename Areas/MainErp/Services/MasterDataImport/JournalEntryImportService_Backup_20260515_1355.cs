using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using MyERP.Areas.MainErp.Infrastructure;
using MyERP.Areas.MainErp.ViewModels.MasterDataImport;

namespace MyERP.Areas.MainErp.Services.MasterDataImport
{
    public class JournalEntryImportService
    {
        private const int ManualJournalNoteType = 57;
        private readonly string _connectionString;

        public JournalEntryImportService()
        {
            _connectionString = MainErpDbConnectionFactory.ResolveActiveConnectionString();
        }

        public IList<JournalEntryImportRowViewModel> Validate(MasterDataImportPreview preview)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                EnsureJournalImportTables(connection, null);
                return Validate(connection, null, preview);
            }
        }

        public MasterDataImportResultViewModel Import(MasterDataImportPreview preview, string userName, bool stopOnAnyError)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                EnsureJournalImportTables(connection, null);
                var rows = Validate(connection, null, preview);
                if (preview.EntityType == MasterDataImportEntityType.OpeningBalances)
                {
                    return ImportOpeningBalances(connection, preview, userName, stopOnAnyError, rows);
                }

                var validGroups = rows.GroupBy(r => r.GroupKey).Where(g => g.All(r => r.IsValid)).ToList();

                if (stopOnAnyError && rows.Any(r => !r.IsValid))
                {
                    var stoppedBatchId = CreateBatch(connection, null, preview.FileName, userName, rows.Count, 0, rows.Count(r => !r.IsValid), 0, "Failed", "Import stopped because validation errors exist.");
                    return new MasterDataImportResultViewModel
                    {
                        BatchId = stoppedBatchId,
                        TotalRows = rows.Count,
                        SuccessRows = 0,
                        FailedRows = rows.Count(r => !r.IsValid),
                        ImportedJournalCount = 0,
                        Message = "Import stopped because validation errors exist."
                    };
                }

                var successRows = validGroups.Sum(g => g.Count());
                var failedRows = rows.Count - successRows;
                var batchId = CreateBatch(connection, null, preview.FileName, userName, rows.Count, successRows, failedRows, validGroups.Count, "Running", null);

                using (var transaction = connection.BeginTransaction(IsolationLevel.Serializable))
                {
                    try
                    {
                        var importedCount = 0;
                        foreach (var group in validGroups)
                        {
                            ImportJournalGroup(connection, transaction, group.ToList(), userName);
                            importedCount++;
                        }

                        foreach (var hash in preview.FileHashes)
                        {
                            InsertFileLog(connection, transaction, batchId, hash.Key, hash.Value, userName, importedCount);
                        }

                        CompleteBatch(connection, transaction, batchId, successRows, failedRows, importedCount, null);
                        transaction.Commit();

                        return new MasterDataImportResultViewModel
                        {
                            BatchId = batchId,
                            TotalRows = rows.Count,
                            SuccessRows = successRows,
                            FailedRows = failedRows,
                            ImportedJournalCount = importedCount,
                            Message = "Journal import completed successfully."
                        };
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        CompleteBatch(connection, null, batchId, 0, rows.Count, 0, ex.Message);
                        throw;
                    }
                }
            }
        }

        private IList<JournalEntryImportRowViewModel> Validate(SqlConnection connection, SqlTransaction transaction, MasterDataImportPreview preview)
        {
            var rows = preview.JournalRows ?? new List<JournalEntryImportRowViewModel>();
            var accountsBySerial = LoadAccounts(connection, transaction, "Account_Serial");
            var accountsByName = LoadAccounts(connection, transaction, "Account_Name");
            var branches = LoadBranches(connection, transaction);
            var defaultBranchId = branches.Count == 0 ? 1 : branches.Values.First();

            foreach (var row in rows)
            {
                row.Errors.Clear();
                row.AccountSerial = Clean(row.AccountSerial);
                row.AccountName = Clean(row.AccountName);
                row.Branch = Clean(row.Branch);
                row.Description = Clean(row.Description);
                row.BranchId = defaultBranchId;
                row.AccountWillBeCreated = false;

                if (IsFileHashImported(connection, transaction, row.FileHash))
                {
                    row.Errors.Add("This file was imported before. Duplicate upload is blocked.");
                }

                if (!row.EntryDate.HasValue)
                {
                    row.Errors.Add("Entry date is required or invalid.");
                }

                if (string.IsNullOrWhiteSpace(row.AccountSerial) && string.IsNullOrWhiteSpace(row.AccountName))
                {
                    row.Errors.Add("Account serial or account name is required.");
                }

                if (!IsDecimalTextValid(row.DebitText))
                {
                    row.Errors.Add("Debit value is invalid.");
                }

                if (!IsDecimalTextValid(row.CreditText))
                {
                    row.Errors.Add("Credit value is invalid.");
                }

                if (row.Debit > 0 && row.Credit > 0)
                {
                    row.Errors.Add("A journal line cannot have both debit and credit.");
                }

                if (row.Debit == 0 && row.Credit == 0 && !(preview.EntityType == MasterDataImportEntityType.OpeningBalances && !string.IsNullOrWhiteSpace(row.OpeningBalanceText)))
                {
                    row.Errors.Add("A journal line must have debit or credit value.");
                }

                if (preview.EntityType == MasterDataImportEntityType.OpeningBalances && !string.IsNullOrWhiteSpace(row.OpeningBalanceText) && !IsDecimalTextValid(row.OpeningBalanceText))
                {
                    row.Errors.Add("Opening balance value is invalid.");
                }

                if (!string.IsNullOrWhiteSpace(row.Branch))
                {
                    int branchId;
                    if (branches.TryGetValue(Normalize(row.Branch), out branchId))
                    {
                        row.BranchId = branchId;
                    }
                    else
                    {
                        row.Errors.Add("Branch was not found.");
                    }
                }

                ResolveAccount(connection, transaction, row, accountsBySerial, accountsByName);
            }

            foreach (var duplicateHash in rows.Where(r => !string.IsNullOrWhiteSpace(r.FileHash)).GroupBy(r => r.FileHash).Where(g => g.Select(r => r.FileName).Distinct(StringComparer.OrdinalIgnoreCase).Count() > 1))
            {
                foreach (var row in duplicateHash)
                {
                    row.Errors.Add("The same file content was uploaded more than once in this batch.");
                }
            }

            if (preview.EntityType != MasterDataImportEntityType.OpeningBalances)
            {
                foreach (var group in rows.GroupBy(r => r.GroupKey))
                {
                    var debit = group.Sum(r => r.Debit);
                    var credit = group.Sum(r => r.Credit);
                    if (Math.Abs(debit - credit) > 0.01m)
                    {
                        foreach (var row in group)
                        {
                            row.Errors.Add("Journal entry is not balanced. Difference: " + (debit - credit).ToString("0.00"));
                        }
                    }
                }
            }
            else
            {
                foreach (var row in rows.Where(r => r.Debit == 0m && r.Credit == 0m && !string.IsNullOrWhiteSpace(r.OpeningBalanceText)))
                {
                    ApplyOpeningBalanceByAccountNature(row, accountsBySerial, accountsByName);
                }

                if (preview.AutoBalanceOpening)
                {
                    var intermediate = FindOpeningIntermediateAccount(connection, transaction);
                    if (intermediate == null)
                    {
                        foreach (var row in rows)
                        {
                            row.Errors.Add("Opening intermediate parent/account was not found. Create حساب وسيط افتتاحي first or disable auto-balance.");
                        }
                    }
                    else
                    {
                        foreach (var row in rows)
                        {
                            row.IntermediateAccountCode = intermediate.AccountCode;
                            row.IntermediateAccountName = intermediate.AccountName;
                        }
                    }
                }
            }

            return rows;
        }

        private MasterDataImportResultViewModel ImportOpeningBalances(SqlConnection connection, MasterDataImportPreview preview, string userName, bool stopOnAnyError, IList<JournalEntryImportRowViewModel> rows)
        {
            var validRows = rows.Where(r => r.IsValid).ToList();
            if (stopOnAnyError && rows.Any(r => !r.IsValid))
            {
                var stoppedBatchId = CreateBatch(connection, null, preview.FileName, userName, rows.Count, 0, rows.Count(r => !r.IsValid), 0, "Failed", "Opening balance import stopped because validation errors exist.");
                return new MasterDataImportResultViewModel
                {
                    BatchId = stoppedBatchId,
                    TotalRows = rows.Count,
                    SuccessRows = 0,
                    FailedRows = rows.Count(r => !r.IsValid),
                    ImportedJournalCount = 0,
                    Message = "Opening balance import stopped because validation errors exist."
                };
            }

            var batchId = CreateBatch(connection, null, preview.FileName, userName, rows.Count, validRows.Count, rows.Count - validRows.Count, 1, "Running", null);
            using (var transaction = connection.BeginTransaction(IsolationLevel.Serializable))
            {
                try
                {
                    var accountsCreated = 0;
                    foreach (var row in validRows.Where(r => r.AccountWillBeCreated))
                    {
                        row.AccountCode = CreateNormalAccount(connection, transaction, row);
                        accountsCreated++;
                    }

                    var voucherId = NextId(connection, transaction, "DOUBLE_ENTREY_VOUCHERS1", "Double_Entry_Vouchers_ID");
                    var openingVoucherId = NextId(connection, transaction, "DOUBLE_ENTREY_VOUCHERS1", "opening_balance_voucher_id");
                    var lineNo = 1;
                    foreach (var row in validRows)
                    {
                        var value = row.Debit > 0 ? row.Debit : row.Credit;
                        var creditOrDebit = row.Debit > 0 ? 0 : 1;
                        InsertOpeningDevLine(connection, transaction, voucherId, lineNo, openingVoucherId, row, value, creditOrDebit, userName);
                        UpdateAccountOpeningBalance(connection, transaction, row.AccountCode, value, creditOrDebit);
                        lineNo++;
                    }

                    var debit = validRows.Sum(r => r.Debit);
                    var credit = validRows.Sum(r => r.Credit);
                    var difference = debit - credit;
                    if (preview.AutoBalanceOpening && difference != 0m)
                    {
                        var intermediate = FindOpeningIntermediateAccount(connection, transaction);
                        if (intermediate == null)
                        {
                            throw new InvalidOperationException("Opening intermediate account was not found.");
                        }

                        var balanceRow = new JournalEntryImportRowViewModel
                        {
                            AccountCode = intermediate.AccountCode,
                            Description = "إقفال الفرق في حساب وسيط افتتاحي - Excel",
                            EntryDate = validRows.Select(r => r.EntryDate).FirstOrDefault(d => d.HasValue) ?? DateTime.Today,
                            BranchId = validRows.Select(r => r.BranchId).FirstOrDefault(b => b.HasValue) ?? 1
                        };
                        InsertOpeningDevLine(connection, transaction, voucherId, lineNo, openingVoucherId, balanceRow, Math.Abs(difference), difference > 0m ? 1 : 0, userName);
                    }

                    foreach (var hash in preview.FileHashes)
                    {
                        InsertFileLog(connection, transaction, batchId, hash.Key, hash.Value, userName, 1);
                    }

                    CompleteBatch(connection, transaction, batchId, validRows.Count, rows.Count - validRows.Count, 1, null);
                    transaction.Commit();

                    return new MasterDataImportResultViewModel
                    {
                        BatchId = batchId,
                        TotalRows = rows.Count,
                        SuccessRows = validRows.Count,
                        FailedRows = rows.Count - validRows.Count,
                        ImportedJournalCount = 1,
                        Message = "Opening balances imported. Accounts created: " + accountsCreated + ". Lines: " + validRows.Count + ". Difference: " + difference.ToString("0.00") + ". Voucher: " + openingVoucherId + "."
                    };
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    CompleteBatch(connection, null, batchId, 0, rows.Count, 0, ex.Message);
                    throw;
                }
            }
        }

        private void ResolveAccount(SqlConnection connection, SqlTransaction transaction, JournalEntryImportRowViewModel row, IDictionary<string, AccountInfo> bySerial, IDictionary<string, AccountInfo> byName)
        {
            AccountInfo account;
            if (!string.IsNullOrWhiteSpace(row.AccountSerial) && bySerial.TryGetValue(Normalize(row.AccountSerial), out account))
            {
                row.AccountCode = account.AccountCode;
                return;
            }

            if (!string.IsNullOrWhiteSpace(row.AccountName) && byName.TryGetValue(Normalize(row.AccountName), out account))
            {
                row.AccountCode = account.AccountCode;
                return;
            }

            if (string.IsNullOrWhiteSpace(row.AccountSerial))
            {
                row.Errors.Add("Account was not found and cannot be created without Account Serial.");
                return;
            }

            var parent = InferParentAccount(row.AccountSerial, bySerial);
            if (parent == null)
            {
                row.Errors.Add("Account was not found and parent account could not be inferred from Account Serial.");
                return;
            }

            if (LooksEntityManaged(parent.PathText))
            {
                row.Errors.Add("Missing account appears to belong to an entity-managed parent. Create the entity from its approved screen/import first.");
                return;
            }

            row.AccountWillBeCreated = true;
        }

        private static void ApplyOpeningBalanceByAccountNature(JournalEntryImportRowViewModel row, IDictionary<string, AccountInfo> bySerial, IDictionary<string, AccountInfo> byName)
        {
            var balance = ParseDecimal(row.OpeningBalanceText);
            if (balance == 0m)
            {
                row.Errors.Add("Opening balance cannot be zero.");
                return;
            }

            AccountInfo account = null;
            if (!string.IsNullOrWhiteSpace(row.AccountSerial))
            {
                bySerial.TryGetValue(Normalize(row.AccountSerial), out account);
            }
            if (account == null && !string.IsNullOrWhiteSpace(row.AccountName))
            {
                byName.TryGetValue(Normalize(row.AccountName), out account);
            }

            var type = (row.BalanceType ?? string.Empty).Trim().ToLowerInvariant();
            bool? debitNature = null;
            if (type == "debit" || type == "مدين" || type == "depit")
            {
                debitNature = true;
            }
            else if (type == "credit" || type == "دائن")
            {
                debitNature = false;
            }
            else if (account != null)
            {
                debitNature = account.DebitOrCredit == 0;
            }

            if (!debitNature.HasValue)
            {
                row.Errors.Add("Opening balance side cannot be detected safely. Provide Balance Type, Debit, or Credit.");
                return;
            }

            var putDebit = balance > 0 ? debitNature.Value : !debitNature.Value;
            if (putDebit)
            {
                row.Debit = Math.Abs(balance);
                row.DebitText = row.Debit.ToString("0.00");
            }
            else
            {
                row.Credit = Math.Abs(balance);
                row.CreditText = row.Credit.ToString("0.00");
            }
        }

        private void ImportJournalGroup(SqlConnection connection, SqlTransaction transaction, IList<JournalEntryImportRowViewModel> groupRows, string userName)
        {
            foreach (var row in groupRows.Where(r => r.AccountWillBeCreated))
            {
                row.AccountCode = CreateNormalAccount(connection, transaction, row);
            }

            var first = groupRows.First();
            var noteValue = groupRows.Sum(r => r.Debit);
            var noteId = InsertNote(connection, transaction, first, noteValue, userName);
            var devId = NextId(connection, transaction, "DOUBLE_ENTREY_VOUCHERS", "Double_Entry_Vouchers_ID");
            var lineNo = 1;

            foreach (var row in groupRows)
            {
                var value = row.Debit > 0 ? row.Debit : row.Credit;
                var creditOrDebit = row.Debit > 0 ? 0 : 1;
                InsertDevLine(connection, transaction, devId, lineNo, row, noteId, value, creditOrDebit, userName);
                lineNo++;
            }
        }

        private long InsertNote(SqlConnection connection, SqlTransaction transaction, JournalEntryImportRowViewModel row, decimal noteValue, string userName)
        {
            var noteId = NextId(connection, transaction, "Notes", "NoteID");
            var serial = NextNoteSerial(connection, transaction, row.BranchId ?? 1, row.EntryDate.Value);
            var values = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                { "NoteID", noteId },
                { "NoteDate", row.EntryDate.Value },
                { "NoteType", ManualJournalNoteType },
                { "Note_Value", noteValue },
                { "Remark", row.Description },
                { "UserID", 0 },
                { "NotePosted", 0 },
                { "Branch_NO", row.BranchId ?? 1 },
                { "NoteSerial", serial },
                { "NoteSerial1", serial },
                { "Ser", serial },
                { "ManualNo", FirstNonEmpty(row.ReferenceNo, row.EntryNo, row.VoucherNo) }
            };

            InsertDynamic(connection, transaction, "Notes", values);
            return noteId;
        }

        private void InsertDevLine(SqlConnection connection, SqlTransaction transaction, long devId, int lineNo, JournalEntryImportRowViewModel row, long noteId, decimal value, int creditOrDebit, string userName)
        {
            var values = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                { "Double_Entry_Vouchers_ID", devId },
                { "DEV_ID_Line_No", lineNo },
                { "Account_Code", row.AccountCode },
                { "Value", value },
                { "Credit_Or_Debit", creditOrDebit },
                { "Double_Entry_Vouchers_Description", row.Description },
                { "Notes_ID", noteId },
                { "Account_Interval_ID", GetCurrentAccountInterval(connection, transaction) },
                { "RecordDate", row.EntryDate.Value },
                { "RecordDateH", string.Empty },
                { "UserID", 0 },
                { "currency", string.Empty },
                { "rate", 1 },
                { "branch_id", row.BranchId ?? 1 },
                { "DueDate", row.EntryDate.Value }
            };

            InsertDynamic(connection, transaction, "DOUBLE_ENTREY_VOUCHERS", values);
        }

        private void InsertOpeningDevLine(SqlConnection connection, SqlTransaction transaction, long devId, int lineNo, long openingVoucherId, JournalEntryImportRowViewModel row, decimal value, int creditOrDebit, string userName)
        {
            var values = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                { "Double_Entry_Vouchers_ID", devId },
                { "DEV_ID_Line_No", lineNo },
                { "Account_Code", row.AccountCode },
                { "Value", value },
                { "Credit_Or_Debit", creditOrDebit },
                { "Double_Entry_Vouchers_Description", (row.Description ?? "قيد افتتاحي مستورد من Excel") + " - " + row.FileName },
                { "Notes_ID", 1 },
                { "Account_Interval_ID", GetCurrentAccountInterval(connection, transaction) },
                { "RecordDate", row.EntryDate.HasValue ? row.EntryDate.Value : DateTime.Today },
                { "RecordDateH", string.Empty },
                { "UserID", 0 },
                { "currency", string.Empty },
                { "rate", 1 },
                { "branch_id", row.BranchId ?? 1 },
                { "DueDate", row.EntryDate.HasValue ? row.EntryDate.Value : DateTime.Today },
                { "opening_balance_voucher_id", openingVoucherId }
            };

            InsertDynamic(connection, transaction, "DOUBLE_ENTREY_VOUCHERS1", values);
        }

        private static void UpdateAccountOpeningBalance(SqlConnection connection, SqlTransaction transaction, string accountCode, decimal value, int creditOrDebit)
        {
            if (!ColumnExists(connection, transaction, "ACCOUNTS", "opening_balance"))
            {
                return;
            }

            using (var command = new SqlCommand(@"UPDATE dbo.ACCOUNTS
SET opening_balance = ISNULL(opening_balance,0) + @SignedValue,
    opening_balance_type = @Type
WHERE Account_Code = @AccountCode", connection, transaction))
            {
                command.Parameters.AddWithValue("@AccountCode", accountCode);
                command.Parameters.AddWithValue("@SignedValue", creditOrDebit == 0 ? value : -value);
                command.Parameters.AddWithValue("@Type", creditOrDebit);
                command.ExecuteNonQuery();
            }
        }

        private static AccountInfo FindOpeningIntermediateAccount(SqlConnection connection, SqlTransaction transaction)
        {
            using (var command = new SqlCommand(@"SELECT TOP 1 Account_Code, Account_Serial, Account_Name, Parent_Account_Code, AccountTypes, AccountTab, DepitOrCredit, Differenttype, Authority
FROM dbo.ACCOUNTS
WHERE Account_Serial LIKE N'52%' OR Account_Name LIKE N'%وسيط%افتتاح%'
ORDER BY CASE WHEN Account_Name LIKE N'%عام%' THEN 0 ELSE 1 END, LEN(Account_Serial), Account_Serial", connection, transaction))
            using (var reader = command.ExecuteReader())
            {
                if (!reader.Read())
                {
                    return CreateOpeningIntermediateAccount(connection, transaction);
                }

                return new AccountInfo
                {
                    AccountCode = Convert.ToString(reader["Account_Code"]),
                    AccountSerial = Convert.ToString(reader["Account_Serial"]),
                    AccountName = Convert.ToString(reader["Account_Name"]),
                    ParentAccountCode = Convert.ToString(reader["Parent_Account_Code"]),
                    AccountTypes = ReadInt(reader, "AccountTypes"),
                    AccountTab = ReadInt(reader, "AccountTab"),
                    DebitOrCredit = ReadInt(reader, "DepitOrCredit"),
                    DifferentType = ReadInt(reader, "Differenttype"),
                    Authority = ReadInt(reader, "Authority")
                };
            }
        }

        private static AccountInfo CreateOpeningIntermediateAccount(SqlConnection connection, SqlTransaction transaction)
        {
            AccountInfo parent = null;
            using (var command = new SqlCommand(@"SELECT TOP 1 Account_Code, Account_Serial, Account_Name, Parent_Account_Code, AccountTypes, AccountTab, DepitOrCredit, Differenttype, Authority
FROM dbo.ACCOUNTS
WHERE Account_Name LIKE N'%حسابات%نظام%' OR Account_Name LIKE N'%النظام%'
ORDER BY LEN(Account_Serial), Account_Serial", connection, transaction))
            using (var reader = command.ExecuteReader())
            {
                if (reader.Read())
                {
                    parent = new AccountInfo
                    {
                        AccountCode = Convert.ToString(reader["Account_Code"]),
                        AccountSerial = Convert.ToString(reader["Account_Serial"]),
                        AccountName = Convert.ToString(reader["Account_Name"]),
                        ParentAccountCode = Convert.ToString(reader["Parent_Account_Code"]),
                        AccountTypes = ReadInt(reader, "AccountTypes"),
                        AccountTab = ReadInt(reader, "AccountTab"),
                        DebitOrCredit = ReadInt(reader, "DepitOrCredit"),
                        DifferentType = ReadInt(reader, "Differenttype"),
                        Authority = ReadInt(reader, "Authority")
                    };
                }
            }

            if (parent == null)
            {
                return null;
            }

            var accountCode = GenerateAccountCode(connection, transaction, parent.AccountCode);
            var accountSerial = GenerateChildSerial(connection, transaction, parent.AccountSerial, accountCode);
            var values = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                { "AccountTypes", parent.AccountTypes },
                { "AccountTab", parent.AccountTab },
                { "DepitOrCredit", parent.DebitOrCredit },
                { "Differenttype", parent.DifferentType },
                { "Authority", parent.Authority },
                { "Block", 0 },
                { "Account_Code", accountCode },
                { "Account_Name", "حساب وسيط افتتاحي" },
                { "Parent_Account_Code", parent.AccountCode },
                { "last_account", 1 },
                { "cannot_del", 0 },
                { "Branch", "0" },
                { "Account_Serial", accountSerial },
                { "BasicAccount", 0 },
                { "DateCreated", DateTime.Now },
                { "Account_NameEng", "Opening Balance Intermediate Account" },
                { "currenct_code", "1" },
                { "mowazna", 0 },
                { "cost_center", 0 },
                { "Sum_account", 0 },
                { "cost_center_type", 0 },
                { "ActivityTypeId", 0 }
            };

            InsertDynamic(connection, transaction, "ACCOUNTS", values);
            UpdateParentAsGroup(connection, transaction, parent.AccountCode);
            return new AccountInfo { AccountCode = accountCode, AccountSerial = accountSerial, AccountName = "حساب وسيط افتتاحي", ParentAccountCode = parent.AccountCode };
        }

        private static string GenerateChildSerial(SqlConnection connection, SqlTransaction transaction, string parentSerial, string accountCode)
        {
            if (string.IsNullOrWhiteSpace(parentSerial))
            {
                return accountCode.Replace("a", string.Empty);
            }

            var max = 0;
            using (var command = new SqlCommand("SELECT Account_Serial FROM dbo.ACCOUNTS WHERE Account_Serial LIKE @Serial + '%'", connection, transaction))
            {
                command.Parameters.AddWithValue("@Serial", parentSerial);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var serial = Convert.ToString(reader["Account_Serial"]);
                        if (serial.Length > parentSerial.Length)
                        {
                            int suffix;
                            if (int.TryParse(serial.Substring(parentSerial.Length), out suffix))
                            {
                                max = Math.Max(max, suffix);
                            }
                        }
                    }
                }
            }

            return parentSerial + (max + 1).ToString("000");
        }

        private string CreateNormalAccount(SqlConnection connection, SqlTransaction transaction, JournalEntryImportRowViewModel row)
        {
            var accountsBySerial = LoadAccounts(connection, transaction, "Account_Serial");
            var parent = InferParentAccount(row.AccountSerial, accountsBySerial);
            if (parent == null)
            {
                throw new InvalidOperationException("Parent account was not found for " + row.AccountSerial);
            }

            var accountCode = GenerateAccountCode(connection, transaction, parent.AccountCode);
            var values = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                { "AccountTypes", parent.AccountTypes },
                { "AccountTab", parent.AccountTab },
                { "DepitOrCredit", parent.DebitOrCredit },
                { "Differenttype", parent.DifferentType },
                { "Authority", parent.Authority },
                { "Block", 0 },
                { "Account_Code", accountCode },
                { "Account_Name", row.AccountName },
                { "Parent_Account_Code", parent.AccountCode },
                { "last_account", 1 },
                { "cannot_del", 0 },
                { "Branch", "0" },
                { "Account_Serial", row.AccountSerial },
                { "BasicAccount", 0 },
                { "DateCreated", DateTime.Now },
                { "Account_NameEng", row.AccountName },
                { "currenct_code", "1" },
                { "mowazna", 0 },
                { "cost_center", 0 },
                { "Sum_account", 0 },
                { "cost_center_type", 0 },
                { "ActivityTypeId", 0 }
            };

            InsertDynamic(connection, transaction, "ACCOUNTS", values);
            UpdateParentAsGroup(connection, transaction, parent.AccountCode);
            return accountCode;
        }

        private static void InsertDynamic(SqlConnection connection, SqlTransaction transaction, string tableName, IDictionary<string, object> values)
        {
            var columns = values.Keys.Where(c => ColumnExists(connection, transaction, tableName, c)).ToList();
            if (columns.Count == 0)
            {
                throw new InvalidOperationException("No matching columns found for table " + tableName + ".");
            }

            var sql = "INSERT INTO dbo." + tableName + " (" + string.Join(",", columns.Select(c => "[" + c + "]")) + ") VALUES (" + string.Join(",", columns.Select(c => "@" + c)) + ")";
            using (var command = new SqlCommand(sql, connection, transaction))
            {
                foreach (var column in columns)
                {
                    command.Parameters.AddWithValue("@" + column, values[column] ?? DBNull.Value);
                }

                command.ExecuteNonQuery();
            }
        }

        private static bool ColumnExists(SqlConnection connection, SqlTransaction transaction, string tableName, string columnName)
        {
            using (var command = new SqlCommand("SELECT COL_LENGTH('dbo.' + @table, @column)", connection, transaction))
            {
                command.Parameters.AddWithValue("@table", tableName);
                command.Parameters.AddWithValue("@column", columnName);
                var result = command.ExecuteScalar();
                return result != null && result != DBNull.Value;
            }
        }

        private static long NextId(SqlConnection connection, SqlTransaction transaction, string tableName, string columnName)
        {
            using (var command = new SqlCommand("SELECT ISNULL(MAX(CAST([" + columnName + "] AS BIGINT)),0)+1 FROM dbo.[" + tableName + "] WITH (UPDLOCK, HOLDLOCK)", connection, transaction))
            {
                return Convert.ToInt64(command.ExecuteScalar());
            }
        }

        private static long NextNoteSerial(SqlConnection connection, SqlTransaction transaction, int branchId, DateTime noteDate)
        {
            using (var command = new SqlCommand(@"SELECT ISNULL(MAX(CAST(NoteSerial AS BIGINT)),0)+1
FROM dbo.Notes WITH (UPDLOCK, HOLDLOCK)
WHERE NoteType=@NoteType
  AND YEAR(NoteDate)=@Year
  AND MONTH(NoteDate)=@Month
  AND (Branch_NO=@BranchId OR Branch_NO IS NULL)", connection, transaction))
            {
                command.Parameters.AddWithValue("@NoteType", ManualJournalNoteType);
                command.Parameters.AddWithValue("@Year", noteDate.Year);
                command.Parameters.AddWithValue("@Month", noteDate.Month);
                command.Parameters.AddWithValue("@BranchId", branchId);
                return Convert.ToInt64(command.ExecuteScalar());
            }
        }

        private static int GetCurrentAccountInterval(SqlConnection connection, SqlTransaction transaction)
        {
            if (!TableExists(connection, transaction, "AccountIntervals"))
            {
                return 0;
            }

            using (var command = new SqlCommand("SELECT TOP 1 Account_Interval_ID FROM AccountIntervals ORDER BY Account_Interval_ID DESC", connection, transaction))
            {
                var result = command.ExecuteScalar();
                return result == null || result == DBNull.Value ? 0 : Convert.ToInt32(result);
            }
        }

        private static IDictionary<string, AccountInfo> LoadAccounts(SqlConnection connection, SqlTransaction transaction, string fieldName)
        {
            var result = new Dictionary<string, AccountInfo>(StringComparer.OrdinalIgnoreCase);
            using (var command = new SqlCommand(@"SELECT Account_Code, Account_Serial, Account_Name, Parent_Account_Code, AccountTypes, AccountTab, DepitOrCredit, Differenttype, Authority
FROM dbo.ACCOUNTS", connection, transaction))
            using (var reader = command.ExecuteReader())
            {
                var items = new List<AccountInfo>();
                while (reader.Read())
                {
                    items.Add(new AccountInfo
                    {
                        AccountCode = Convert.ToString(reader["Account_Code"]),
                        AccountSerial = Convert.ToString(reader["Account_Serial"]),
                        AccountName = Convert.ToString(reader["Account_Name"]),
                        ParentAccountCode = Convert.ToString(reader["Parent_Account_Code"]),
                        AccountTypes = ReadInt(reader, "AccountTypes"),
                        AccountTab = ReadInt(reader, "AccountTab"),
                        DebitOrCredit = ReadInt(reader, "DepitOrCredit"),
                        DifferentType = ReadInt(reader, "Differenttype"),
                        Authority = ReadInt(reader, "Authority")
                    });
                }

                foreach (var item in items)
                {
                    item.PathText = BuildPathText(item, items);
                    var key = Normalize(fieldName == "Account_Name" ? item.AccountName : item.AccountSerial);
                    if (!string.IsNullOrWhiteSpace(key) && !result.ContainsKey(key))
                    {
                        result.Add(key, item);
                    }
                }
            }

            return result;
        }

        private static AccountInfo InferParentAccount(string accountSerial, IDictionary<string, AccountInfo> accountsBySerial)
        {
            accountSerial = Clean(accountSerial);
            for (var length = accountSerial.Length - 1; length > 0; length--)
            {
                AccountInfo account;
                if (accountsBySerial.TryGetValue(Normalize(accountSerial.Substring(0, length)), out account))
                {
                    return account;
                }
            }

            return null;
        }

        private static string BuildPathText(AccountInfo account, IList<AccountInfo> accounts)
        {
            var names = new List<string>();
            var current = account;
            for (var i = 0; i < 20 && current != null; i++)
            {
                names.Add(current.AccountName ?? string.Empty);
                current = accounts.FirstOrDefault(a => string.Equals(a.AccountCode, current.ParentAccountCode, StringComparison.OrdinalIgnoreCase));
            }

            return string.Join(" ", names);
        }

        private static bool LooksEntityManaged(string value)
        {
            value = (value ?? string.Empty).ToLowerInvariant();
            return value.Contains("customer") || value.Contains("supplier") || value.Contains("employee") || value.Contains("bank") || value.Contains("store")
                || value.Contains("عميل") || value.Contains("عملاء") || value.Contains("مورد") || value.Contains("موظف") || value.Contains("بنك") || value.Contains("مخزن") || value.Contains("خزنة") || value.Contains("عهد");
        }

        private static IDictionary<string, int> LoadBranches(SqlConnection connection, SqlTransaction transaction)
        {
            var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            if (!TableExists(connection, transaction, "branches"))
            {
                return result;
            }

            var idColumn = ColumnExists(connection, transaction, "branches", "branch_id") ? "branch_id" : "BranchId";
            var nameColumn = ColumnExists(connection, transaction, "branches", "branch_name") ? "branch_name" : (ColumnExists(connection, transaction, "branches", "BranchName") ? "BranchName" : idColumn);
            using (var command = new SqlCommand("SELECT " + idColumn + " AS Id, " + nameColumn + " AS Name FROM dbo.branches", connection, transaction))
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    var id = Convert.ToInt32(reader["Id"]);
                    result[Normalize(Convert.ToString(reader["Name"]))] = id;
                    result[Normalize(Convert.ToString(id))] = id;
                }
            }

            return result;
        }

        private static bool TableExists(SqlConnection connection, SqlTransaction transaction, string tableName)
        {
            using (var command = new SqlCommand("SELECT OBJECT_ID('dbo.' + @table, 'U')", connection, transaction))
            {
                command.Parameters.AddWithValue("@table", tableName);
                var result = command.ExecuteScalar();
                return result != null && result != DBNull.Value;
            }
        }

        private static string GenerateAccountCode(SqlConnection connection, SqlTransaction transaction, string parentCode)
        {
            var max = 0;
            using (var command = new SqlCommand("SELECT Account_Code FROM dbo.ACCOUNTS WHERE Parent_Account_Code=@parent", connection, transaction))
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
            } while (AccountCodeExists(connection, transaction, candidate));

            return candidate;
        }

        private static void UpdateParentAsGroup(SqlConnection connection, SqlTransaction transaction, string parentCode)
        {
            using (var command = new SqlCommand("UPDATE dbo.ACCOUNTS SET last_account=0 WHERE Account_Code=@AccountCode", connection, transaction))
            {
                command.Parameters.AddWithValue("@AccountCode", parentCode);
                command.ExecuteNonQuery();
            }
        }

        private static bool AccountCodeExists(SqlConnection connection, SqlTransaction transaction, string accountCode)
        {
            using (var command = new SqlCommand("SELECT TOP 1 1 FROM dbo.ACCOUNTS WHERE Account_Code=@AccountCode", connection, transaction))
            {
                command.Parameters.AddWithValue("@AccountCode", accountCode);
                return command.ExecuteScalar() != null;
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

        private static void EnsureJournalImportTables(SqlConnection connection, SqlTransaction transaction)
        {
            using (var command = new SqlCommand(@"IF OBJECT_ID('dbo.MasterDataImportJournalFileLog','U') IS NULL
BEGIN
    CREATE TABLE dbo.MasterDataImportJournalFileLog
    (
        Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_MasterDataImportJournalFileLog PRIMARY KEY,
        BatchId int NULL,
        FileName nvarchar(260) NOT NULL,
        FileHash nvarchar(64) NOT NULL,
        UploadedBy nvarchar(128) NULL,
        UploadedAt datetime NOT NULL CONSTRAINT DF_MasterDataImportJournalFileLog_UploadedAt DEFAULT (GETDATE()),
        ImportedJournalCount int NOT NULL CONSTRAINT DF_MasterDataImportJournalFileLog_JournalCount DEFAULT (0)
    );
    CREATE UNIQUE INDEX UX_MasterDataImportJournalFileLog_FileHash ON dbo.MasterDataImportJournalFileLog(FileHash);
END", connection, transaction))
            {
                command.ExecuteNonQuery();
            }

            using (var command = new SqlCommand(@"IF OBJECT_ID('dbo.MasterDataImportBatch','U') IS NULL
BEGIN
    CREATE TABLE dbo.MasterDataImportBatch
    (
        BatchId int IDENTITY(1,1) NOT NULL CONSTRAINT PK_MasterDataImportBatch PRIMARY KEY,
        FileName nvarchar(260) NOT NULL,
        EntityType nvarchar(50) NOT NULL,
        ImportedBy nvarchar(128) NULL,
        ImportStartedAt datetime NOT NULL CONSTRAINT DF_MasterDataImportBatch_StartedAt DEFAULT(GETDATE()),
        ImportFinishedAt datetime NULL,
        TotalRows int NOT NULL CONSTRAINT DF_MasterDataImportBatch_TotalRows DEFAULT(0),
        SuccessRows int NOT NULL CONSTRAINT DF_MasterDataImportBatch_SuccessRows DEFAULT(0),
        FailedRows int NOT NULL CONSTRAINT DF_MasterDataImportBatch_FailedRows DEFAULT(0),
        Status nvarchar(50) NOT NULL CONSTRAINT DF_MasterDataImportBatch_Status DEFAULT(N'Running'),
        ErrorMessage nvarchar(max) NULL
    );
END
IF COL_LENGTH('dbo.MasterDataImportBatch','ErrorMessage') IS NULL
BEGIN
    ALTER TABLE dbo.MasterDataImportBatch ADD ErrorMessage nvarchar(max) NULL;
END", connection, transaction))
            {
                command.ExecuteNonQuery();
            }
        }

        private static bool IsFileHashImported(SqlConnection connection, SqlTransaction transaction, string fileHash)
        {
            if (string.IsNullOrWhiteSpace(fileHash))
            {
                return false;
            }

            using (var command = new SqlCommand("SELECT TOP 1 1 FROM dbo.MasterDataImportJournalFileLog WHERE FileHash=@FileHash", connection, transaction))
            {
                command.Parameters.AddWithValue("@FileHash", fileHash);
                return command.ExecuteScalar() != null;
            }
        }

        private static void InsertFileLog(SqlConnection connection, SqlTransaction transaction, int batchId, string fileName, string fileHash, string userName, int importedCount)
        {
            using (var command = new SqlCommand(@"INSERT INTO dbo.MasterDataImportJournalFileLog(BatchId, FileName, FileHash, UploadedBy, UploadedAt, ImportedJournalCount)
VALUES (@BatchId, @FileName, @FileHash, @UploadedBy, GETDATE(), @ImportedJournalCount)", connection, transaction))
            {
                command.Parameters.AddWithValue("@BatchId", batchId);
                command.Parameters.AddWithValue("@FileName", fileName ?? string.Empty);
                command.Parameters.AddWithValue("@FileHash", fileHash ?? string.Empty);
                command.Parameters.AddWithValue("@UploadedBy", userName ?? string.Empty);
                command.Parameters.AddWithValue("@ImportedJournalCount", importedCount);
                command.ExecuteNonQuery();
            }
        }

        private static int CreateBatch(SqlConnection connection, SqlTransaction transaction, string fileName, string userName, int totalRows, int successRows, int failedRows, int journalCount, string status, string error)
        {
            using (var command = new SqlCommand(@"INSERT INTO dbo.MasterDataImportBatch
(FileName, EntityType, ImportedBy, ImportStartedAt, TotalRows, SuccessRows, FailedRows, Status, ErrorMessage)
VALUES (@FileName, 'JournalEntries', @ImportedBy, GETDATE(), @TotalRows, @SuccessRows, @FailedRows, @Status, @ErrorMessage);
SELECT CAST(SCOPE_IDENTITY() AS int);", connection, transaction))
            {
                command.Parameters.AddWithValue("@FileName", fileName ?? string.Empty);
                command.Parameters.AddWithValue("@ImportedBy", userName ?? string.Empty);
                command.Parameters.AddWithValue("@TotalRows", totalRows);
                command.Parameters.AddWithValue("@SuccessRows", successRows);
                command.Parameters.AddWithValue("@FailedRows", failedRows);
                command.Parameters.AddWithValue("@Status", status);
                command.Parameters.AddWithValue("@ErrorMessage", (object)error ?? DBNull.Value);
                return Convert.ToInt32(command.ExecuteScalar());
            }
        }

        private static void CompleteBatch(SqlConnection connection, SqlTransaction transaction, int batchId, int successRows, int failedRows, int journalCount, string error)
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

        private static int ReadInt(IDataRecord reader, string name)
        {
            return reader[name] == DBNull.Value ? 0 : Convert.ToInt32(reader[name]);
        }

        private static bool IsDecimalTextValid(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return true;
            }

            decimal parsed;
            return decimal.TryParse(value.Replace(",", string.Empty), out parsed);
        }

        private static decimal ParseDecimal(string value)
        {
            decimal parsed;
            value = Clean(value).Replace(",", string.Empty);
            return decimal.TryParse(value, out parsed) ? parsed : 0m;
        }

        private static string FirstNonEmpty(params string[] values)
        {
            return values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v)) ?? string.Empty;
        }

        private static string Clean(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().Trim('\u200f', '\u200e');
        }

        private static string Normalize(string value)
        {
            return Clean(value).Replace(" ", string.Empty).Replace("_", string.Empty).Replace("-", string.Empty).ToLowerInvariant();
        }

        private class AccountInfo
        {
            public string AccountCode { get; set; }
            public string AccountSerial { get; set; }
            public string AccountName { get; set; }
            public string ParentAccountCode { get; set; }
            public string PathText { get; set; }
            public int AccountTypes { get; set; }
            public int AccountTab { get; set; }
            public int DebitOrCredit { get; set; }
            public int DifferentType { get; set; }
            public int Authority { get; set; }
        }
    }
}
