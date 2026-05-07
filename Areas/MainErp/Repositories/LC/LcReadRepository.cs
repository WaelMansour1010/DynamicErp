using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using MyERP.Areas.MainErp.Interfaces;
using MyERP.Areas.MainErp.Infrastructure.Localization;
using MyERP.Areas.MainErp.ViewModels;
using MyERP.Areas.MainErp.ViewModels.LC;

namespace MyERP.Areas.MainErp.Repositories.LC
{
    public class LcReadRepository
    {
        private readonly IMainErpDbConnectionFactory _connectionFactory;

        public LcReadRepository(IMainErpDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public PagedReadResult<LCListItemViewModel> Search(string searchText, int? bankId, int? vendorId, int? branchId, int page, int pageSize)
        {
            var result = new PagedReadResult<LCListItemViewModel>();
            page = page < 1 ? 1 : page;
            pageSize = pageSize < 1 ? 20 : pageSize;

            try
            {
                using (var connection = _connectionFactory.CreateOpenConnection())
                using (var command = new SqlCommand(@"
WITH LcRows AS (
    SELECT
        ROW_NUMBER() OVER (ORDER BY l.TblLCID DESC) AS RowNo,
        COUNT(1) OVER() AS TotalCount,
        l.TblLCID,
        l.LCNO,
        l.Name,
        l.Value,
        l.FromDate,
        l.Todate,
        l.BranchID,
        l.Account_Code,
        b.BankName,
        c.name AS CurrencyName,
        cust.CusName AS VendorName
    FROM TblLC l
    LEFT JOIN BanksData b ON l.BankId = b.BankID
    LEFT JOIN currency c ON l.CurrencyId = c.id
    LEFT JOIN TblCustemers cust ON l.VendorId = cust.CusID
    WHERE (@SearchText IS NULL OR l.LCNO LIKE @SearchLike OR l.Name LIKE @SearchLike)
      AND (@BankId IS NULL OR l.BankId = @BankId)
      AND (@VendorId IS NULL OR l.VendorId = @VendorId)
      AND (@BranchId IS NULL OR l.BranchID = @BranchId)
)
SELECT * FROM LcRows WHERE RowNo BETWEEN @StartRow AND @EndRow ORDER BY RowNo;", connection))
                {
                    AddSearchParameters(command, searchText, bankId, vendorId, branchId, page, pageSize);
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            if (result.TotalCount == 0 && reader["TotalCount"] != DBNull.Value)
                            {
                                result.TotalCount = Convert.ToInt32(reader["TotalCount"]);
                            }

                            result.Items.Add(new LCListItemViewModel
                            {
                                TblLCID = Convert.ToInt32(reader["TblLCID"]),
                                LCNO = Convert.ToString(reader["LCNO"]),
                                Name = Convert.ToString(reader["Name"]),
                                Value = reader["Value"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(reader["Value"]),
                                FromDate = reader["FromDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["FromDate"]),
                                ToDate = reader["Todate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["Todate"]),
                                BranchID = reader["BranchID"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["BranchID"]),
                                AccountCode = Convert.ToString(reader["Account_Code"]),
                                BankName = Convert.ToString(reader["BankName"]),
                                CurrencyName = Convert.ToString(reader["CurrencyName"]),
                                VendorName = Convert.ToString(reader["VendorName"])
                            });
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                result.Warning = "LC read model is not available in the configured database yet: " + ex.Message;
            }

            return result;
        }

        public LCDetailsViewModel GetDetails(int id)
        {
            try
            {
                using (var connection = _connectionFactory.CreateOpenConnection())
                {
                    var model = LoadHeader(connection, id);
                    if (model == null)
                    {
                        return new LCDetailsViewModel { TblLCID = id, Warning = "LC record was not found." };
                    }

                    LoadAccountLinks(connection, model);
                    LoadLinkedNotes(connection, model);
                    LoadGridSections(connection, model);
                    LoadVoucherTrace(connection, model);
                    return model;
                }
            }
            catch (SqlException ex)
            {
                return new LCDetailsViewModel { TblLCID = id, Warning = "LC details are not available in the configured database yet: " + ex.Message };
            }
        }

        private static LCDetailsViewModel LoadHeader(SqlConnection connection, int id)
        {
            using (var command = new SqlCommand(@"
SELECT TOP 1
    l.TblLCID, l.LCNO, l.Name, l.Value, l.FromDate, l.Todate, l.BranchID,
    l.Account_Code, l.Remarks, l.Account_CodeMargin, l.MarginAccount_Code,
    l.AcceptAccount_Code, l.AccountExpensCode, l.OpenBalance, l.OpenBalanceType,
    l.OpenValue, l.Currency_rate, l.LCTyperId, l.BankId, l.BankID2, l.BoxID,
    l.CurrencyId, l.VendorId, l.CountryId, l.project_id, l.projectName,
    l.PaymentTypeID, l.ChequeNumber, l.ChequeDueDate, l.opening_balance_voucher_id,
    l.OpenBalanceDate, l.CloseDate, l.LastParcilDate, l.AccountExpProject,
    l.Locked, l.userid, l.AccountLGParent, l.AccountMarginParent,
    l.AccountAcceptanceParent, l.AccountExpensParent, l.NoteID, l.NoteSerial,
    l.NoteID2, l.NoteSerial2, l.NoteIDOpen, l.NoteSerialOpen,
    b.BankName, c.name AS CurrencyName, cust.CusName AS VendorName
FROM TblLC l
LEFT JOIN BanksData b ON l.BankId = b.BankID
LEFT JOIN currency c ON l.CurrencyId = c.id
LEFT JOIN TblCustemers cust ON l.VendorId = cust.CusID
WHERE l.TblLCID = @Id;", connection))
            {
                command.Parameters.AddWithValue("@Id", id);
                using (var reader = command.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        return null;
                    }

                    return new LCDetailsViewModel
                    {
                        TblLCID = ReadInt(reader, "TblLCID").GetValueOrDefault(),
                        LCNO = ReadString(reader, "LCNO"),
                        Name = ReadString(reader, "Name"),
                        Value = ReadDecimal(reader, "Value"),
                        FromDate = ReadDate(reader, "FromDate"),
                        ToDate = ReadDate(reader, "Todate"),
                        BranchID = ReadInt(reader, "BranchID"),
                        AccountCode = ReadString(reader, "Account_Code"),
                        Remarks = ReadString(reader, "Remarks"),
                        MarginAccountCode = FirstText(ReadString(reader, "Account_CodeMargin"), ReadString(reader, "MarginAccount_Code")),
                        AcceptAccountCode = ReadString(reader, "AcceptAccount_Code"),
                        ExpenseAccountCode = ReadString(reader, "AccountExpensCode"),
                        AccountExpProject = ReadString(reader, "AccountExpProject"),
                        AccountLGParent = ReadString(reader, "AccountLGParent"),
                        AccountMarginParent = ReadString(reader, "AccountMarginParent"),
                        AccountAcceptanceParent = ReadString(reader, "AccountAcceptanceParent"),
                        AccountExpensParent = ReadString(reader, "AccountExpensParent"),
                        OpenValue = ReadDecimal(reader, "OpenValue"),
                        CurrencyRate = ReadDouble(reader, "Currency_rate"),
                        LcTypeId = ReadInt(reader, "LCTyperId"),
                        BankId = ReadInt(reader, "BankId"),
                        BankId2 = ReadInt(reader, "BankID2"),
                        BoxId = ReadInt(reader, "BoxID"),
                        CurrencyId = ReadInt(reader, "CurrencyId"),
                        VendorId = ReadInt(reader, "VendorId"),
                        CountryId = ReadInt(reader, "CountryId"),
                        ProjectId = ReadInt(reader, "project_id"),
                        ProjectName = ReadString(reader, "projectName"),
                        PaymentTypeId = ReadInt(reader, "PaymentTypeID"),
                        ChequeNumber = ReadString(reader, "ChequeNumber"),
                        ChequeDueDate = ReadDate(reader, "ChequeDueDate"),
                        OpeningBalanceVoucherId = ReadDouble(reader, "opening_balance_voucher_id"),
                        CloseDate = ReadDate(reader, "CloseDate"),
                        LastParcilDate = ReadDate(reader, "LastParcilDate"),
                        Locked = ReadBool(reader, "Locked"),
                        UserId = ReadInt(reader, "userid"),
                        NoteID = ReadInt(reader, "NoteID"),
                        NoteSerial = ReadString(reader, "NoteSerial"),
                        NoteID2 = ReadInt(reader, "NoteID2"),
                        NoteSerial2 = ReadString(reader, "NoteSerial2"),
                        NoteIDOpen = ReadInt(reader, "NoteIDOpen"),
                        NoteSerialOpen = ReadString(reader, "NoteSerialOpen"),
                        OpenBalance = ReadDouble(reader, "OpenBalance"),
                        OpenBalanceType = ReadInt(reader, "OpenBalanceType"),
                        BankName = ReadString(reader, "BankName"),
                        CurrencyName = ReadString(reader, "CurrencyName"),
                        VendorName = ReadString(reader, "VendorName")
                    };
                }
            }
        }

        private static void LoadAccountLinks(SqlConnection connection, LCDetailsViewModel model)
        {
            AddAccountLink(connection, model, "LcAccount", model.AccountCode, model.AccountLGParent);
            AddAccountLink(connection, model, "MarginAccount", model.MarginAccountCode, model.AccountMarginParent);
            AddAccountLink(connection, model, "AcceptanceAccount", model.AcceptAccountCode, model.AccountAcceptanceParent);
            AddAccountLink(connection, model, "ExpenseAccount", model.ExpenseAccountCode, model.AccountExpensParent);
            AddAccountLink(connection, model, "ProjectExpenseAccount", model.AccountExpProject, null);
        }

        private static void AddAccountLink(SqlConnection connection, LCDetailsViewModel model, string roleKey, string accountCode, string parentCode)
        {
            var account = GetAccount(connection, accountCode);
            var parent = GetAccount(connection, parentCode);
            var link = new LCAccountLinkViewModel
            {
                Role = MainErpLocalizationService.T(roleKey),
                AccountCode = accountCode,
                AccountSerial = account == null ? null : account.AccountSerial,
                AccountName = account == null ? null : account.AccountName,
                AccountDisplay = BuildAccountDisplay(account),
                ParentAccountCode = FirstText(parentCode, account == null ? null : account.ParentCode),
                ParentAccountSerial = parent == null ? null : parent.AccountSerial,
                ParentAccountName = parent == null ? null : parent.AccountName,
                ParentAccountDisplay = BuildAccountDisplay(parent),
                ParentIsLastAccount = parent == null ? (bool?)null : parent.LastAccount
            };

            if (string.IsNullOrWhiteSpace(accountCode))
            {
                link.Warning = MainErpLocalizationService.T("AccountNotFound");
            }
            else if (account == null)
            {
                link.Warning = MainErpLocalizationService.T("AccountNotFound");
            }
            else if (!string.IsNullOrWhiteSpace(link.ParentAccountCode) && parent == null)
            {
                link.Warning = "الحساب الأب غير موجود أو لم يتم العثور عليه.";
            }
            else if (parent != null && parent.LastAccount == true)
            {
                link.Warning = "الحساب الأب معلّم كآخر حساب؛ إنشاء حساب ابن تحته يحتاج مراجعة.";
            }

            model.AccountLinks.Add(link);
        }

        private static string BuildAccountDisplay(AccountInfo account)
        {
            if (account == null)
            {
                return "الحساب غير موجود";
            }

            var accountName = MainErpEntityLocalization.Localize(account.AccountName, account.AccountNameEnglish);
            if (!string.IsNullOrWhiteSpace(account.AccountSerial) && !string.IsNullOrWhiteSpace(accountName))
            {
                return account.AccountSerial + " - " + accountName;
            }

            if (!string.IsNullOrWhiteSpace(account.AccountSerial))
            {
                return account.AccountSerial;
            }

            if (!string.IsNullOrWhiteSpace(accountName))
            {
                return accountName;
            }

            return MainErpLocalizationService.T("AccountNotFound");
        }

        private static AccountInfo GetAccount(SqlConnection connection, string accountCode)
        {
            if (string.IsNullOrWhiteSpace(accountCode))
            {
                return null;
            }

            using (var command = new SqlCommand(@"
SELECT TOP 1 Account_Code, Account_Name, Account_NameEng, Parent_Account_Code, last_account, Account_Serial
FROM ACCOUNTS
WHERE Account_Code = @AccountCode;", connection))
            {
                command.Parameters.AddWithValue("@AccountCode", accountCode);
                using (var reader = command.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        return null;
                    }

                    return new AccountInfo
                    {
                        AccountCode = ReadString(reader, "Account_Code"),
                        AccountName = ReadString(reader, "Account_Name"),
                        AccountNameEnglish = ReadString(reader, "Account_NameEng"),
                        ParentCode = ReadString(reader, "Parent_Account_Code"),
                        LastAccount = ReadBool(reader, "last_account"),
                        AccountSerial = ReadString(reader, "Account_Serial")
                    };
                }
            }
        }

        private static void LoadLinkedNotes(SqlConnection connection, LCDetailsViewModel model)
        {
            LoadNotesTable(connection, model, "Notes");
            LoadNotesTable(connection, model, "Notes1");
        }

        private static void LoadNotesTable(SqlConnection connection, LCDetailsViewModel model, string tableName)
        {
            if (!TableExists(connection, tableName))
            {
                if (tableName == "Notes1")
                {
                    model.Warnings.Add("Notes1 table was not found; opening-balance note trace is unavailable.");
                }
                return;
            }

            var columns = GetColumns(connection, tableName);
            if (!columns.Contains("TblLCID") || !columns.Contains("NoteID"))
            {
                model.Warnings.Add(tableName + " does not expose TblLCID/NoteID columns for LC trace.");
                return;
            }

            var selectColumns = SelectExisting(columns, new[]
            {
                "NoteID", "NoteDate", "NoteType", "NoteSerial", "Note_Value", "Remark", "Double_Entry_Vouchers_ID", "TblLCID"
            });

            try
            {
                using (var command = new SqlCommand("SELECT TOP 100 " + string.Join(",", selectColumns.Select(Bracket)) + " FROM " + Bracket(tableName) + " WHERE TblLCID = @Id ORDER BY NoteID DESC;", connection))
                {
                    command.Parameters.AddWithValue("@Id", model.TblLCID);
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            model.LinkedNotes.Add(new LCLinkedNoteViewModel
                            {
                                SourceTable = tableName,
                                NoteID = ReadInt(reader, "NoteID").GetValueOrDefault(),
                                NoteDate = ReadDate(reader, "NoteDate"),
                                NoteType = ReadString(reader, "NoteType"),
                                NoteSerial = ReadString(reader, "NoteSerial"),
                                NoteValue = ReadDecimal(reader, "Note_Value"),
                                DoubleEntryVoucherId = ReadInt(reader, "Double_Entry_Vouchers_ID"),
                                Remark = ReadString(reader, "Remark")
                            });
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                model.Warnings.Add(tableName + " trace failed: " + ex.Message);
            }
        }

        private static void LoadGridSections(SqlConnection connection, LCDetailsViewModel model)
        {
            LoadGridSection(connection, model, "LC history / تاريخ الاعتماد", "GrdBondHistory", "TBLLCHistory", "Saved from GrdBondHistory; createVoucher2 type 2 can generate linked vouchers.");
            LoadGridSection(connection, model, "Margins / هامش الاعتماد", "GrdMargin / GrdMargin2", "TBLLCMargin", "Saved from margin grids; payment rows can create normal vouchers.");
            LoadGridSection(connection, model, "Margin payments / refinance", "GrdMargin4", "TBLLCMargin2", "Saved from GrdMargin4; can use bank/margin/acceptance accounts and VAT paths.");
            LoadGridSection(connection, model, "Opening balance rows", "GrdMargin3", "tblLCOpenB", "Saved from GrdMargin3; can use Notes1 and DOUBLE_ENTREY_VOUCHERS1.");
        }

        private static void LoadGridSection(SqlConnection connection, LCDetailsViewModel model, string title, string gridName, string tableName, string behavior)
        {
            var section = new LCGridSectionViewModel
            {
                Title = title,
                Vb6GridName = gridName,
                SourceTable = tableName,
                Behavior = behavior
            };

            model.GridSections.Add(section);

            if (!TableExists(connection, tableName))
            {
                section.Warning = tableName + " was not found in the configured database.";
                return;
            }

            var columns = GetColumns(connection, tableName);
            if (!columns.Contains("TblLCID"))
            {
                section.Warning = tableName + " has no TblLCID column; cannot safely link it to LC.";
                return;
            }

            var preferred = SelectExisting(columns, new[]
            {
                "ID", "Id", "TblLCID", "Date", "NoteDate", "MarginNo", "OrderDate", "GuaranteeDate",
                "Amount", "Value", "PayedAmount", "StillAmount", "OpenBalance", "MarginAccountCode",
                "MarginAccount", "BankAccountCode", "BankAccount", "AccountMargen2", "NoteID", "NoteId",
                "NoteID2", "NoteID3", "NoteSerial", "IsOpenBalance", "opening_balance_voucher_id",
                "Remarks", "Remark", "Description"
            });

            if (preferred.Count == 0)
            {
                preferred = columns.Take(12).ToList();
            }

            foreach (var column in preferred)
            {
                section.Columns.Add(column);
            }

            var orderColumn = columns.Contains("ID") ? "ID" : (columns.Contains("Id") ? "Id" : columns.First());
            using (var command = new SqlCommand("SELECT TOP 100 " + string.Join(",", preferred.Select(Bracket)) + " FROM " + Bracket(tableName) + " WHERE TblLCID = @Id ORDER BY " + Bracket(orderColumn) + " DESC;", connection))
            {
                command.Parameters.AddWithValue("@Id", model.TblLCID);
                using (var reader = command.ExecuteReader())
                {
                    var rowNumber = 0;
                    while (reader.Read())
                    {
                        rowNumber++;
                        var row = new LCGridRowViewModel
                        {
                            RowNumber = rowNumber,
                            NoteId = FirstInt(ReadInt(reader, "NoteID"), ReadInt(reader, "NoteId")),
                            NoteId2 = ReadInt(reader, "NoteID2"),
                            NoteId3 = ReadInt(reader, "NoteID3"),
                            OpeningBalanceVoucherId = ReadDouble(reader, "opening_balance_voucher_id")
                        };

                        foreach (var column in preferred)
                        {
                            row.Values[column] = ReadDisplay(reader, column);
                        }

                        section.Rows.Add(row);
                    }
                }
            }
        }

        private static void LoadVoucherTrace(SqlConnection connection, LCDetailsViewModel model)
        {
            LoadNormalVoucherTrace(connection, model);
            LoadOpeningVoucherTrace(connection, model);
        }

        private static void LoadNormalVoucherTrace(SqlConnection connection, LCDetailsViewModel model)
        {
            if (!TableExists(connection, "DOUBLE_ENTREY_VOUCHERS") || !TableExists(connection, "Notes"))
            {
                model.Warnings.Add("Normal voucher trace requires Notes and DOUBLE_ENTREY_VOUCHERS.");
                return;
            }

            using (var command = new SqlCommand(@"
SELECT TOP 300
    'DOUBLE_ENTREY_VOUCHERS' AS SourceTable,
    v.Double_Entry_Vouchers_ID,
    v.DEV_ID_Line_No,
    v.Notes_ID,
    v.RecordDate,
    v.Account_Code,
    a.Account_Name,
    a.Account_NameEng,
    a.Account_Serial,
    v.Value,
    v.Credit_Or_Debit,
    v.Double_Entry_Vouchers_Description,
    v.branch_id,
    v.project_id,
    CAST(NULL AS float) AS opening_balance_voucher_id,
    n.NoteSerial
FROM DOUBLE_ENTREY_VOUCHERS v
INNER JOIN Notes n ON v.Notes_ID = n.NoteID
LEFT JOIN ACCOUNTS a ON v.Account_Code = a.Account_Code
WHERE n.TblLCID = @Id
ORDER BY v.RecordDate DESC, v.Double_Entry_Vouchers_ID DESC, v.DEV_ID_Line_No;", connection))
            {
                command.Parameters.AddWithValue("@Id", model.TblLCID);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        model.VoucherLines.Add(ReadVoucherLine(reader));
                    }
                }
            }
        }

        private static void LoadOpeningVoucherTrace(SqlConnection connection, LCDetailsViewModel model)
        {
            if (!TableExists(connection, "DOUBLE_ENTREY_VOUCHERS1"))
            {
                model.Warnings.Add("Opening voucher table DOUBLE_ENTREY_VOUCHERS1 was not found.");
                return;
            }

            var hasNotes1 = TableExists(connection, "Notes1");
            var sql = hasNotes1
                ? @"
SELECT TOP 300
    'DOUBLE_ENTREY_VOUCHERS1' AS SourceTable,
    v.Double_Entry_Vouchers_ID,
    v.DEV_ID_Line_No,
    v.Notes_ID,
    v.RecordDate,
    v.Account_Code,
    a.Account_Name,
    a.Account_NameEng,
    a.Account_Serial,
    v.Value,
    v.Credit_Or_Debit,
    v.Double_Entry_Vouchers_Description,
    v.branch_id,
    v.project_id,
    v.opening_balance_voucher_id,
    n.NoteSerial
FROM DOUBLE_ENTREY_VOUCHERS1 v
LEFT JOIN Notes1 n ON v.Notes_ID = n.NoteID
LEFT JOIN ACCOUNTS a ON v.Account_Code = a.Account_Code
WHERE n.TblLCID = @Id OR (@OpeningId IS NOT NULL AND v.opening_balance_voucher_id = @OpeningId)
ORDER BY v.RecordDate DESC, v.Double_Entry_Vouchers_ID DESC, v.DEV_ID_Line_No;"
                : @"
SELECT TOP 300
    'DOUBLE_ENTREY_VOUCHERS1' AS SourceTable,
    v.Double_Entry_Vouchers_ID,
    v.DEV_ID_Line_No,
    v.Notes_ID,
    v.RecordDate,
    v.Account_Code,
    a.Account_Name,
    a.Account_NameEng,
    a.Account_Serial,
    v.Value,
    v.Credit_Or_Debit,
    v.Double_Entry_Vouchers_Description,
    v.branch_id,
    v.project_id,
    v.opening_balance_voucher_id,
    CAST(NULL AS nvarchar(255)) AS NoteSerial
FROM DOUBLE_ENTREY_VOUCHERS1 v
LEFT JOIN ACCOUNTS a ON v.Account_Code = a.Account_Code
WHERE @OpeningId IS NOT NULL AND v.opening_balance_voucher_id = @OpeningId
ORDER BY v.RecordDate DESC, v.Double_Entry_Vouchers_ID DESC, v.DEV_ID_Line_No;";

            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddWithValue("@Id", model.TblLCID);
                command.Parameters.AddWithValue("@OpeningId", (object)model.OpeningBalanceVoucherId ?? DBNull.Value);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        model.VoucherLines.Add(ReadVoucherLine(reader));
                    }
                }
            }
        }

        private static LCVoucherLineViewModel ReadVoucherLine(IDataRecord reader)
        {
            var value = ReadDecimal(reader, "Value").GetValueOrDefault();
            var direction = ReadString(reader, "Credit_Or_Debit");
            var isCredit = direction == "1" || string.Equals(direction, "Credit", StringComparison.OrdinalIgnoreCase);
            return new LCVoucherLineViewModel
            {
                SourceTable = ReadString(reader, "SourceTable"),
                VoucherId = ReadInt(reader, "Double_Entry_Vouchers_ID").GetValueOrDefault(),
                LineNo = ReadInt(reader, "DEV_ID_Line_No"),
                NoteId = ReadInt(reader, "Notes_ID"),
                NoteSerial = ReadString(reader, "NoteSerial"),
                RecordDate = ReadDate(reader, "RecordDate"),
                AccountCode = ReadString(reader, "Account_Code"),
                AccountName = ReadString(reader, "Account_Name"),
                AccountSerial = ReadString(reader, "Account_Serial"),
                AccountDisplay = MainErpEntityLocalization.AccountDisplay(
                    ReadString(reader, "Account_Serial"),
                    ReadString(reader, "Account_Name"),
                    ReadString(reader, "Account_NameEng")),
                Debit = isCredit ? 0m : value,
                Credit = isCredit ? value : 0m,
                Description = ReadString(reader, "Double_Entry_Vouchers_Description"),
                BranchId = ReadInt(reader, "branch_id"),
                ProjectId = ReadInt(reader, "project_id"),
                OpeningBalanceVoucherId = ReadDouble(reader, "opening_balance_voucher_id")
            };
        }

        private static bool TableExists(SqlConnection connection, string tableName)
        {
            using (var command = new SqlCommand("SELECT COUNT(1) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = @TableName;", connection))
            {
                command.Parameters.AddWithValue("@TableName", tableName);
                return Convert.ToInt32(command.ExecuteScalar()) > 0;
            }
        }

        private static HashSet<string> GetColumns(SqlConnection connection, string tableName)
        {
            var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            using (var command = new SqlCommand("SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @TableName ORDER BY ORDINAL_POSITION;", connection))
            {
                command.Parameters.AddWithValue("@TableName", tableName);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        columns.Add(Convert.ToString(reader["COLUMN_NAME"]));
                    }
                }
            }

            return columns;
        }

        private static List<string> SelectExisting(HashSet<string> columns, IEnumerable<string> requested)
        {
            var result = new List<string>();
            foreach (var column in requested)
            {
                if (columns.Contains(column) && !result.Any(x => string.Equals(x, column, StringComparison.OrdinalIgnoreCase)))
                {
                    result.Add(column);
                }
            }

            return result;
        }

        private static string Bracket(string name)
        {
            return "[" + name.Replace("]", "]]") + "]";
        }

        private static string FirstText(string first, string second)
        {
            return !string.IsNullOrWhiteSpace(first) ? first : second;
        }

        private static int? FirstInt(int? first, int? second)
        {
            return first.HasValue ? first : second;
        }

        private static bool HasColumn(IDataRecord reader, string columnName)
        {
            for (var i = 0; i < reader.FieldCount; i++)
            {
                if (string.Equals(reader.GetName(i), columnName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static string ReadString(IDataRecord reader, string columnName)
        {
            return !HasColumn(reader, columnName) || reader[columnName] == DBNull.Value ? null : Convert.ToString(reader[columnName]);
        }

        private static string ReadDisplay(IDataRecord reader, string columnName)
        {
            if (!HasColumn(reader, columnName) || reader[columnName] == DBNull.Value)
            {
                return string.Empty;
            }

            var value = reader[columnName];
            if (value is DateTime)
            {
                return ((DateTime)value).ToString("yyyy-MM-dd");
            }

            return Convert.ToString(value);
        }

        private static int? ReadInt(IDataRecord reader, string columnName)
        {
            return !HasColumn(reader, columnName) || reader[columnName] == DBNull.Value ? (int?)null : Convert.ToInt32(reader[columnName]);
        }

        private static decimal? ReadDecimal(IDataRecord reader, string columnName)
        {
            return !HasColumn(reader, columnName) || reader[columnName] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(reader[columnName]);
        }

        private static double? ReadDouble(IDataRecord reader, string columnName)
        {
            return !HasColumn(reader, columnName) || reader[columnName] == DBNull.Value ? (double?)null : Convert.ToDouble(reader[columnName]);
        }

        private static DateTime? ReadDate(IDataRecord reader, string columnName)
        {
            return !HasColumn(reader, columnName) || reader[columnName] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader[columnName]);
        }

        private static bool? ReadBool(IDataRecord reader, string columnName)
        {
            return !HasColumn(reader, columnName) || reader[columnName] == DBNull.Value ? (bool?)null : Convert.ToBoolean(reader[columnName]);
        }

        private static void AddSearchParameters(SqlCommand command, string searchText, int? bankId, int? vendorId, int? branchId, int page, int pageSize)
        {
            command.Parameters.AddWithValue("@SearchText", string.IsNullOrWhiteSpace(searchText) ? (object)DBNull.Value : searchText);
            command.Parameters.AddWithValue("@SearchLike", string.IsNullOrWhiteSpace(searchText) ? (object)DBNull.Value : "%" + searchText + "%");
            command.Parameters.AddWithValue("@BankId", (object)bankId ?? DBNull.Value);
            command.Parameters.AddWithValue("@VendorId", (object)vendorId ?? DBNull.Value);
            command.Parameters.AddWithValue("@BranchId", (object)branchId ?? DBNull.Value);
            command.Parameters.AddWithValue("@StartRow", ((page - 1) * pageSize) + 1);
            command.Parameters.AddWithValue("@EndRow", page * pageSize);
        }

        private class AccountInfo
        {
            public string AccountCode { get; set; }
            public string AccountName { get; set; }
            public string AccountNameEnglish { get; set; }
            public string ParentCode { get; set; }
            public bool? LastAccount { get; set; }
            public string AccountSerial { get; set; }
        }
    }
}
