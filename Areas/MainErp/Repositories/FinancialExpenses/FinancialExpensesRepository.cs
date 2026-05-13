using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using MyERP.Areas.MainErp.Interfaces;
using MyERP.Areas.MainErp.Models.Security;
using MyERP.Areas.MainErp.ViewModels.FinancialExpenses;

namespace MyERP.Areas.MainErp.Repositories.FinancialExpenses
{
    public class FinancialExpensesRepository
    {
        private readonly IMainErpDbConnectionFactory _connectionFactory;

        public FinancialExpensesRepository(IMainErpDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public IList<FinancialExpenseListItem> Search(int noteType, string searchText)
        {
            var rows = new List<FinancialExpenseListItem>();
            using (var connection = _connectionFactory.CreateOpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
SELECT TOP 100 n.NoteID, n.NoteType, n.NoteSerial, n.NoteSerial1, n.NoteDate, n.Note_Value, n.general_des, b.branch_name
FROM dbo.notes_all n
LEFT JOIN dbo.TblBranchesData b ON b.branch_id = n.branch_no
WHERE n.NoteType = @NoteType AND ISNULL(n.bill_type, 0) <> 2
  AND (@Search IS NULL OR CONVERT(nvarchar(30), n.NoteID) = @Search OR CONVERT(nvarchar(50), n.NoteSerial1) LIKE @LikeSearch OR n.general_des LIKE @LikeSearch)
ORDER BY n.NoteID DESC;";
                command.Parameters.Add("@NoteType", SqlDbType.Int).Value = noteType;
                AddNullable(command, "@Search", SqlDbType.NVarChar, string.IsNullOrWhiteSpace(searchText) ? null : searchText.Trim(), 100);
                AddNullable(command, "@LikeSearch", SqlDbType.NVarChar, string.IsNullOrWhiteSpace(searchText) ? null : "%" + searchText.Trim() + "%", 120);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        rows.Add(new FinancialExpenseListItem
                        {
                            Id = GetInt(reader, "NoteID"),
                            NoteType = GetNullableInt(reader, "NoteType") ?? noteType,
                            JournalSerial = GetString(reader, "NoteSerial"),
                            VoucherSerial = GetString(reader, "NoteSerial1"),
                            Date = GetDate(reader, "NoteDate"),
                            Value = GetDecimal(reader, "Note_Value"),
                            BranchName = GetString(reader, "branch_name"),
                            Remark = GetString(reader, "general_des")
                        });
                    }
                }
            }

            return rows;
        }

        public FinancialExpenseDetails GetDetails(int id)
        {
            FinancialExpenseDetails model = null;
            using (var connection = _connectionFactory.CreateOpenConnection())
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"
SELECT NoteID, A_NoteID, NoteType, NoteDate, NoteSerial, NoteSerial1, bill_Type, NoteCashingType, branch_no,
       BoxID, BankID, CusID, ChqueNum, DueDate, ExpensesID, Note_Value, too, general_des, Remark
FROM dbo.notes_all
WHERE NoteID = @Id AND NoteType IN (80, 350);";
                    command.Parameters.Add("@Id", SqlDbType.Int).Value = id;
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            var noteType = GetNullableInt(reader, "NoteType") ?? 80;
                            model = new FinancialExpenseDetails
                            {
                                Id = GetInt(reader, "NoteID"),
                                DetailNoteId = GetNullableInt(reader, "A_NoteID"),
                                NoteType = noteType,
                                Mode = noteType == 350 ? "FrmExpenses30" : "FrmExpenses3",
                                Date = GetDate(reader, "NoteDate"),
                                JournalSerial = GetString(reader, "NoteSerial"),
                                VoucherSerial = GetString(reader, "NoteSerial1"),
                                BillType = GetNullableInt(reader, "bill_Type") ?? 0,
                                PaymentType = GetNullableInt(reader, "NoteCashingType") ?? 0,
                                BranchId = GetNullableInt(reader, "branch_no"),
                                BoxId = GetNullableInt(reader, "BoxID"),
                                BankId = GetNullableInt(reader, "BankID"),
                                VendorId = GetNullableInt(reader, "CusID"),
                                ChequeNumber = GetString(reader, "ChqueNum"),
                                ChequeDueDate = GetDate(reader, "DueDate"),
                                ExpensesTypeId = GetNullableInt(reader, "ExpensesID"),
                                Value = GetDecimal(reader, "Note_Value"),
                                PaidTo = GetString(reader, "too"),
                                GeneralDescription = GetString(reader, "general_des"),
                                Remarks = GetString(reader, "Remark")
                            };
                        }
                    }
                }

                if (model == null)
                {
                    return null;
                }

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"
SELECT d.Account_Code, a.Account_Name, d.Value, d.Double_Entry_Vouchers_Description, d.Vat, d.Vatyo, d.branch_id, d.Billno
FROM dbo.DOUBLE_ENTREY_VOUCHERS d
LEFT JOIN dbo.ACCOUNTS a ON a.Account_Code = d.Account_Code
WHERE ISNULL(d.hideline, 0) = 0
  AND d.Credit_Or_Debit = 0
  AND d.notes_all = @Id
ORDER BY d.DEV_ID_Line_No;";
                    command.Parameters.Add("@Id", SqlDbType.Int).Value = id;
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            model.Lines.Add(new FinancialExpenseLine
                            {
                                AccountCode = GetString(reader, "Account_Code"),
                                AccountName = GetString(reader, "Account_Name"),
                                Value = GetDecimal(reader, "Value"),
                                Description = GetString(reader, "Double_Entry_Vouchers_Description"),
                                Vat = GetDecimal(reader, "Vat"),
                                VatPercent = GetDecimal(reader, "Vatyo"),
                                BranchId = GetNullableInt(reader, "branch_id"),
                                BillNo = GetString(reader, "Billno")
                            });
                        }
                    }
                }
            }

            return model;
        }

        public FinancialExpenseSaveResult Save(FinancialExpenseSaveRequest request, MainErpUserContext user)
        {
            var noteType = ResolveNoteType(request.Mode);
            var voucherNumberingType = noteType == 350 ? 35 : 8;
            using (var connection = _connectionFactory.CreateOpenConnection())
            using (var transaction = connection.BeginTransaction())
            {
                try
                {
                    var isNew = !request.Id.HasValue || request.Id.Value <= 0;
                    var id = isNew ? NextManualId(connection, transaction, "notes_all", "NoteID", null) : request.Id.Value;
                    var detailNoteId = isNew || !request.DetailNoteId.HasValue || request.DetailNoteId <= 0
                        ? NextManualId(connection, transaction, "Notes", "NoteID", null)
                        : request.DetailNoteId.Value;
                    var journalSerial = string.IsNullOrWhiteSpace(request.JournalSerial)
                        ? NextManualNumber(connection, transaction, "notes_all", "NoteSerial", "NoteType=" + noteType)
                        : request.JournalSerial.Trim();
                    var voucherSerial = string.IsNullOrWhiteSpace(request.VoucherSerial)
                        ? NextManualNumber(connection, transaction, "notes_all", "NoteSerial1", "NoteType=" + noteType)
                        : request.VoucherSerial.Trim();

                    if (isNew)
                    {
                        InsertNotesAll(connection, transaction, request, user, id, detailNoteId, noteType, voucherNumberingType, journalSerial, voucherSerial);
                    }
                    else
                    {
                        DeleteChildren(connection, transaction, id, noteType);
                        UpdateNotesAll(connection, transaction, request, user, id, detailNoteId, noteType, voucherNumberingType, journalSerial, voucherSerial);
                    }

                    InsertNote(connection, transaction, request, user, id, detailNoteId, noteType, voucherNumberingType, journalSerial, voucherSerial);
                    InsertDebitLines(connection, transaction, request, user, id, detailNoteId);
                    InsertCreditLine(connection, transaction, request, user, id, detailNoteId);

                    transaction.Commit();
                    return new FinancialExpenseSaveResult
                    {
                        Success = true,
                        Message = "تم حفظ المستند بنجاح.",
                        Id = id,
                        DetailNoteId = detailNoteId,
                        JournalSerial = journalSerial,
                        VoucherSerial = voucherSerial
                    };
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return new FinancialExpenseSaveResult { Success = false, Message = "تعذر حفظ المستند: " + ex.Message };
                }
            }
        }

        public FinancialExpenseSaveResult Delete(int id)
        {
            using (var connection = _connectionFactory.CreateOpenConnection())
            using (var transaction = connection.BeginTransaction())
            {
                try
                {
                    DeleteChildren(connection, transaction, id, null);
                    using (var command = connection.CreateCommand())
                    {
                        command.Transaction = transaction;
                        command.CommandText = "DELETE FROM dbo.notes_all WHERE NoteID = @Id AND NoteType IN (80, 350);";
                        command.Parameters.Add("@Id", SqlDbType.Int).Value = id;
                        command.ExecuteNonQuery();
                    }

                    transaction.Commit();
                    return new FinancialExpenseSaveResult { Success = true, Message = "تم حذف المستند." };
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return new FinancialExpenseSaveResult { Success = false, Message = "تعذر حذف المستند: " + ex.Message };
                }
            }
        }

        public IList<FinancialExpenseLookupItem> LoadBranches() { return LoadLookup("SELECT branch_id, branch_name, NULL AccountCode FROM dbo.TblBranchesData ORDER BY branch_name", "branch_id", "branch_name", "AccountCode"); }
        public IList<FinancialExpenseLookupItem> LoadBoxes() { return LoadLookup("SELECT BoxID, BoxName, Account_Code FROM dbo.TblBoxesData ORDER BY BoxName", "BoxID", "BoxName", "Account_Code"); }
        public IList<FinancialExpenseLookupItem> LoadBanks() { return LoadLookup("SELECT BankID, BankName, Account_Code FROM dbo.BanksData ORDER BY BankName", "BankID", "BankName", "Account_Code"); }
        public IList<FinancialExpenseLookupItem> LoadVendors() { return LoadLookup("SELECT TOP 500 CusID, CusName, Account_Code FROM dbo.TblCustemers WHERE ISNULL(Type, 0) IN (3,4) ORDER BY CusName", "CusID", "CusName", "Account_Code"); }
        public IList<FinancialExpenseLookupItem> LoadExpensesTypes() { return LoadLookup("SELECT ID, Name, Account_Code FROM dbo.ExpensesType ORDER BY Name", "ID", "Name", "Account_Code"); }
        public IList<FinancialExpenseLookupItem> LoadAccounts() { return LoadLookup("SELECT TOP 800 Account_Code, Account_Name, Account_Code AccountCode FROM dbo.ACCOUNTS WHERE ISNULL(last_account,0)=1 ORDER BY Account_Name", "Account_Code", "Account_Name", "AccountCode"); }

        private static void InsertNotesAll(SqlConnection connection, SqlTransaction transaction, FinancialExpenseSaveRequest request, MainErpUserContext user, int id, int detailNoteId, int noteType, int numberingType1, string journalSerial, string voucherSerial)
        {
            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = @"
INSERT INTO dbo.notes_all
(NoteID, A_NoteID, NoteType, NoteDate, NoteSerial, NoteSerial1, OldNoteSerial1, Note_Value, Remark, too, general_des,
 bill_Type, NoteCashingType, branch_no, BoxID, BankID, CusID, ChqueNum, DueDate, ExpensesID, UserID, Buy, foxy_no,
 numbering_type, numbering_type1, sanad_year, sanad_month)
VALUES
(@Id, @DetailNoteId, @NoteType, @Date, @JournalSerial, @VoucherSerial, @VoucherSerial, @Value, @Remark, @PaidTo, @GeneralDescription,
 @BillType, @PaymentType, @BranchId, @BoxId, @BankId, @VendorId, @ChequeNumber, @ChequeDueDate, @ExpensesTypeId, @UserId, '0', 0,
 0, @NumberingType1, YEAR(@Date), MONTH(@Date));";
                AddNotesParameters(command, request, user, id, detailNoteId, noteType, numberingType1, journalSerial, voucherSerial);
                command.ExecuteNonQuery();
            }
        }

        private static void UpdateNotesAll(SqlConnection connection, SqlTransaction transaction, FinancialExpenseSaveRequest request, MainErpUserContext user, int id, int detailNoteId, int noteType, int numberingType1, string journalSerial, string voucherSerial)
        {
            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = @"
UPDATE dbo.notes_all
SET A_NoteID = @DetailNoteId, NoteDate = @Date, NoteSerial = @JournalSerial, NoteSerial1 = @VoucherSerial,
    Note_Value = @Value, Remark = @Remark, too = @PaidTo, general_des = @GeneralDescription, bill_Type = @BillType,
    NoteCashingType = @PaymentType, branch_no = @BranchId, BoxID = @BoxId, BankID = @BankId, CusID = @VendorId,
    ChqueNum = @ChequeNumber, DueDate = @ChequeDueDate, ExpensesID = @ExpensesTypeId, UserID = @UserId,
    numbering_type1 = @NumberingType1, sanad_year = YEAR(@Date), sanad_month = MONTH(@Date)
WHERE NoteID = @Id AND NoteType = @NoteType;";
                AddNotesParameters(command, request, user, id, detailNoteId, noteType, numberingType1, journalSerial, voucherSerial);
                command.ExecuteNonQuery();
            }
        }

        private static void InsertNote(SqlConnection connection, SqlTransaction transaction, FinancialExpenseSaveRequest request, MainErpUserContext user, int notesAllId, int noteId, int noteType, int numberingType1, string journalSerial, string voucherSerial)
        {
            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = @"
INSERT INTO dbo.Notes
(NoteID, notes_all, NoteType, NoteDate, NoteSerial, NoteSerial1, Note_Value, Remark, too, branch_no,
 BoxID, BankID, CusID, ChqueNum, DueDate, ExpensesID, UserID, Buy, numbering_type, numbering_type1, sanad_year, sanad_month)
VALUES
(@DetailNoteId, @Id, @NoteType, @Date, @JournalSerial, @VoucherSerial, @Value, @Remark, @PaidTo, @BranchId,
 @BoxId, @BankId, @VendorId, @ChequeNumber, @ChequeDueDate, @ExpensesTypeId, @UserId, '0', 0, @NumberingType1, YEAR(@Date), MONTH(@Date));";
                AddNotesParameters(command, request, user, notesAllId, noteId, noteType, numberingType1, journalSerial, voucherSerial);
                command.ExecuteNonQuery();
            }
        }

        private static void InsertDebitLines(SqlConnection connection, SqlTransaction transaction, FinancialExpenseSaveRequest request, MainErpUserContext user, int notesAllId, int noteId)
        {
            var lineNo = 1;
            foreach (var line in request.Lines)
            {
                if (line == null || string.IsNullOrWhiteSpace(line.AccountCode) || line.Value <= 0)
                {
                    continue;
                }

                InsertVoucherLine(connection, transaction, notesAllId, noteId, lineNo++, line.AccountCode, line.Value, 0, line.Description, request.Date, user, request.BranchId, line);
            }
        }

        private static void InsertCreditLine(SqlConnection connection, SqlTransaction transaction, FinancialExpenseSaveRequest request, MainErpUserContext user, int notesAllId, int noteId)
        {
            InsertVoucherLine(connection, transaction, notesAllId, noteId, 999, request.CreditAccountCode, request.Value, 1, request.GeneralDescription, request.Date, user, request.BranchId, null);
        }

        private static void InsertVoucherLine(SqlConnection connection, SqlTransaction transaction, int notesAllId, int noteId, int lineNo, string accountCode, decimal value, int debitCredit, string description, DateTime? date, MainErpUserContext user, int? branchId, FinancialExpenseLine sourceLine)
        {
            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = @"
INSERT INTO dbo.DOUBLE_ENTREY_VOUCHERS
(Double_Entry_Vouchers_ID, DEV_ID_Line_No, DEV_ID_Line_No1, Account_Code, Value, Credit_Or_Debit,
 Double_Entry_Vouchers_Description, RecordDate, Notes_ID, UserID, Account_Interval_ID, notes_all, branch_id,
 Vat, Vatyo, Billno, hideline)
VALUES
(@Id, @LineNo, @LineNo1, @AccountCode, @Value, @DebitCredit, @Description, @Date, @NoteId, @UserId, NULL, @NotesAllId, @BranchId,
 @Vat, @VatPercent, @BillNo, 0);";
                command.Parameters.Add("@Id", SqlDbType.Int).Value = NextManualId(connection, transaction, "DOUBLE_ENTREY_VOUCHERS", "Double_Entry_Vouchers_ID", null);
                command.Parameters.Add("@LineNo", SqlDbType.Int).Value = lineNo;
                command.Parameters.Add("@LineNo1", SqlDbType.Float).Value = lineNo;
                command.Parameters.Add("@AccountCode", SqlDbType.NVarChar, 100).Value = accountCode;
                command.Parameters.Add("@Value", SqlDbType.Money).Value = value;
                command.Parameters.Add("@DebitCredit", SqlDbType.SmallInt).Value = debitCredit;
                AddNullable(command, "@Description", SqlDbType.NVarChar, description, 500);
                command.Parameters.Add("@Date", SqlDbType.DateTime).Value = date.HasValue ? (object)date.Value : DateTime.Today;
                command.Parameters.Add("@NoteId", SqlDbType.Int).Value = noteId;
                command.Parameters.Add("@UserId", SqlDbType.Int).Value = user == null ? (object)DBNull.Value : user.UserId;
                command.Parameters.Add("@NotesAllId", SqlDbType.Int).Value = notesAllId;
                AddNullable(command, "@BranchId", SqlDbType.Int, sourceLine != null && sourceLine.BranchId.HasValue ? sourceLine.BranchId : branchId);
                command.Parameters.Add("@Vat", SqlDbType.Float).Value = sourceLine == null ? 0 : Convert.ToDouble(sourceLine.Vat);
                command.Parameters.Add("@VatPercent", SqlDbType.Float).Value = sourceLine == null ? 0 : Convert.ToDouble(sourceLine.VatPercent);
                AddNullable(command, "@BillNo", SqlDbType.NVarChar, sourceLine == null ? null : sourceLine.BillNo, 100);
                command.ExecuteNonQuery();
            }
        }

        private static void DeleteChildren(SqlConnection connection, SqlTransaction transaction, int id, int? noteType)
        {
            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = @"
DELETE FROM dbo.DOUBLE_ENTREY_VOUCHERS WHERE notes_all = @Id;
DELETE FROM dbo.Notes WHERE notes_all = @Id AND (@NoteType IS NULL OR NoteType = @NoteType);
DELETE FROM dbo.ExpensesDetails WHERE Noteid = @Id OR NoteSerial1 IN (SELECT CONVERT(nvarchar(50), NoteSerial1) FROM dbo.notes_all WHERE NoteID = @Id);";
                command.Parameters.Add("@Id", SqlDbType.Int).Value = id;
                AddNullable(command, "@NoteType", SqlDbType.Int, noteType);
                command.ExecuteNonQuery();
            }
        }

        private static void AddNotesParameters(SqlCommand command, FinancialExpenseSaveRequest request, MainErpUserContext user, int id, int detailNoteId, int noteType, int numberingType1, string journalSerial, string voucherSerial)
        {
            command.Parameters.Add("@Id", SqlDbType.Int).Value = id;
            command.Parameters.Add("@DetailNoteId", SqlDbType.Int).Value = detailNoteId;
            command.Parameters.Add("@NoteType", SqlDbType.Int).Value = noteType;
            command.Parameters.Add("@Date", SqlDbType.DateTime).Value = request.Date.HasValue ? (object)request.Date.Value : DateTime.Today;
            command.Parameters.Add("@JournalSerial", SqlDbType.Float).Value = ToDouble(journalSerial);
            command.Parameters.Add("@VoucherSerial", SqlDbType.Float).Value = ToDouble(voucherSerial);
            command.Parameters.Add("@Value", SqlDbType.Float).Value = Convert.ToDouble(request.Value);
            AddNullable(command, "@Remark", SqlDbType.NVarChar, request.Remarks ?? request.GeneralDescription, 500);
            AddNullable(command, "@PaidTo", SqlDbType.NVarChar, request.PaidTo, 500);
            AddNullable(command, "@GeneralDescription", SqlDbType.VarChar, request.GeneralDescription, 500);
            command.Parameters.Add("@BillType", SqlDbType.Int).Value = request.BillType;
            command.Parameters.Add("@PaymentType", SqlDbType.Int).Value = request.PaymentType;
            AddNullable(command, "@BranchId", SqlDbType.Int, request.BranchId);
            AddNullable(command, "@BoxId", SqlDbType.Int, request.PaymentType == 0 ? request.BoxId : null);
            AddNullable(command, "@BankId", SqlDbType.Int, request.PaymentType == 1 || request.PaymentType == 3 ? request.BankId : null);
            AddNullable(command, "@VendorId", SqlDbType.Int, request.PaymentType == 2 ? request.VendorId : null);
            AddNullable(command, "@ChequeNumber", SqlDbType.NVarChar, request.ChequeNumber, 100);
            AddNullable(command, "@ChequeDueDate", SqlDbType.DateTime, request.ChequeDueDate);
            AddNullable(command, "@ExpensesTypeId", SqlDbType.Int, request.ExpensesTypeId);
            command.Parameters.Add("@UserId", SqlDbType.Int).Value = user == null ? (object)DBNull.Value : user.UserId;
            command.Parameters.Add("@NumberingType1", SqlDbType.Int).Value = numberingType1;
        }

        private IList<FinancialExpenseLookupItem> LoadLookup(string sql, string idColumn, string textColumn, string accountColumn)
        {
            var rows = new List<FinancialExpenseLookupItem>();
            using (var connection = _connectionFactory.CreateOpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = sql;
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        rows.Add(new FinancialExpenseLookupItem
                        {
                            Id = Convert.ToString(reader[idColumn]),
                            Text = GetString(reader, textColumn),
                            AccountCode = GetString(reader, accountColumn)
                        });
                    }
                }
            }

            return rows;
        }

        private static int ResolveNoteType(string mode)
        {
            return string.Equals(mode, "FrmExpenses30", StringComparison.OrdinalIgnoreCase) ? 350 : 80;
        }

        private static int NextManualId(SqlConnection connection, SqlTransaction transaction, string tableName, string columnName, string whereClause)
        {
            using (var lockCommand = new SqlCommand("sp_getapplock", connection, transaction))
            {
                lockCommand.CommandType = CommandType.StoredProcedure;
                lockCommand.Parameters.AddWithValue("@Resource", "MainErp:" + tableName + ":" + columnName + ":" + (whereClause ?? "all"));
                lockCommand.Parameters.AddWithValue("@LockMode", "Exclusive");
                lockCommand.Parameters.AddWithValue("@LockOwner", "Transaction");
                lockCommand.Parameters.AddWithValue("@LockTimeout", 15000);
                var returnValue = lockCommand.Parameters.Add("@ReturnValue", SqlDbType.Int);
                returnValue.Direction = ParameterDirection.ReturnValue;
                lockCommand.ExecuteNonQuery();
                if (Convert.ToInt32(returnValue.Value) < 0)
                {
                    throw new TimeoutException("تعذر حجز رقم جديد.");
                }
            }

            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = "SELECT ISNULL(MAX(CASE WHEN ISNUMERIC(" + columnName + ") = 1 THEN CONVERT(int, " + columnName + ") ELSE 0 END), 0) + 1 FROM dbo." + tableName + " WITH (UPDLOCK, HOLDLOCK)" + (string.IsNullOrWhiteSpace(whereClause) ? "" : " WHERE " + whereClause);
                return Convert.ToInt32(command.ExecuteScalar());
            }
        }

        private static string NextManualNumber(SqlConnection connection, SqlTransaction transaction, string tableName, string columnName, string whereClause)
        {
            using (var lockCommand = new SqlCommand("sp_getapplock", connection, transaction))
            {
                lockCommand.CommandType = CommandType.StoredProcedure;
                lockCommand.Parameters.AddWithValue("@Resource", "MainErp:" + tableName + ":" + columnName + ":" + (whereClause ?? "all"));
                lockCommand.Parameters.AddWithValue("@LockMode", "Exclusive");
                lockCommand.Parameters.AddWithValue("@LockOwner", "Transaction");
                lockCommand.Parameters.AddWithValue("@LockTimeout", 15000);
                var returnValue = lockCommand.Parameters.Add("@ReturnValue", SqlDbType.Int);
                returnValue.Direction = ParameterDirection.ReturnValue;
                lockCommand.ExecuteNonQuery();
                if (Convert.ToInt32(returnValue.Value) < 0)
                {
                    throw new TimeoutException("تعذر حجز رقم جديد.");
                }
            }

            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = "SELECT CONVERT(varchar(50), CONVERT(decimal(38,0), ISNULL(MAX(CASE WHEN ISNUMERIC(" + columnName + ") = 1 THEN CONVERT(decimal(38,0), " + columnName + ") ELSE 0 END), 0) + 1)) FROM dbo." + tableName + " WITH (UPDLOCK, HOLDLOCK)" + (string.IsNullOrWhiteSpace(whereClause) ? "" : " WHERE " + whereClause);
                return Convert.ToString(command.ExecuteScalar());
            }
        }

        private static void AddNullable(SqlCommand command, string name, SqlDbType type, object value, int size = 0)
        {
            var parameter = size > 0 ? command.Parameters.Add(name, type, size) : command.Parameters.Add(name, type);
            parameter.Value = value == null || (value is string && string.IsNullOrWhiteSpace((string)value)) ? DBNull.Value : value;
        }

        private static double ToDouble(string value)
        {
            double result;
            return double.TryParse(value, out result) ? result : 0;
        }

        private static string GetString(IDataRecord record, string name)
        {
            var value = record[name];
            return value == DBNull.Value ? string.Empty : Convert.ToString(value);
        }

        private static int GetInt(IDataRecord record, string name)
        {
            return Convert.ToInt32(record[name]);
        }

        private static int? GetNullableInt(IDataRecord record, string name)
        {
            var value = record[name];
            return value == DBNull.Value ? (int?)null : Convert.ToInt32(value);
        }

        private static DateTime? GetDate(IDataRecord record, string name)
        {
            var value = record[name];
            return value == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(value);
        }

        private static decimal GetDecimal(IDataRecord record, string name)
        {
            var value = record[name];
            return value == DBNull.Value ? 0 : Convert.ToDecimal(value);
        }
    }
}
