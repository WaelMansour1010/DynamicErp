using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace MyERP.Common.DiscountNotifications
{
    public class DiscountNotificationReadRepository
    {
        private static readonly int[] NoteTypes = { 9, 10, 8034, 9082, 9083, 9089, 9090, 9099 };
        private readonly Func<SqlConnection> _createOpenConnection;

        public DiscountNotificationReadRepository(Func<SqlConnection> createOpenConnection)
        {
            _createOpenConnection = createOpenConnection;
        }

        public DiscountNotificationIndexViewModel Search(string searchText, int? branchId, DateTime? fromDate, DateTime? toDate, int? selectedId, bool isPosMode)
        {
            var model = new DiscountNotificationIndexViewModel
            {
                SearchText = searchText,
                BranchId = branchId,
                FromDate = fromDate,
                ToDate = toDate,
                SelectedId = selectedId,
                IsPosMode = isPosMode
            };

            try
            {
                using (var connection = _createOpenConnection())
                using (var command = new SqlCommand(@"
SELECT TOP 100
    n.NoteID,
    CONVERT(nvarchar(50), n.NoteSerial) AS NoteSerial,
    CONVERT(nvarchar(50), n.NoteSerial1) AS NoteSerial1,
    n.NoteDate,
    n.NoteType,
    n.Note_Value,
    n.TotalValue,
    n.VAT,
    n.Remark,
    n.ORDER_NO,
    c.CusName,
    c.CusNamee,
    b.branch_name,
    b.branch_namee
FROM Notes n
LEFT JOIN TblCustemers c ON n.CusID = c.CusID
LEFT JOIN TblBranchesData b ON n.branch_no = b.branch_id
WHERE n.NoteType IN (9, 10, 8034, 9082, 9083, 9089, 9090, 9099)
  AND (@SearchText IS NULL
       OR CONVERT(nvarchar(50), n.NoteSerial) LIKE @SearchLike
       OR CONVERT(nvarchar(50), n.NoteSerial1) LIKE @SearchLike
       OR CONVERT(nvarchar(50), n.NoteID) LIKE @SearchLike
       OR n.Remark LIKE @SearchLike
       OR c.CusName LIKE @SearchLike
       OR c.CusNamee LIKE @SearchLike)
  AND (@BranchId IS NULL OR n.branch_no = @BranchId)
  AND (@FromDate IS NULL OR n.NoteDate >= @FromDate)
  AND (@ToDate IS NULL OR n.NoteDate < DATEADD(day, 1, @ToDate))
ORDER BY n.NoteDate DESC, n.NoteID DESC;", connection))
                {
                    command.Parameters.Add("@SearchText", SqlDbType.NVarChar, 200).Value = string.IsNullOrWhiteSpace(searchText) ? (object)DBNull.Value : searchText.Trim();
                    command.Parameters.Add("@SearchLike", SqlDbType.NVarChar, 220).Value = string.IsNullOrWhiteSpace(searchText) ? (object)DBNull.Value : "%" + searchText.Trim() + "%";
                    command.Parameters.Add("@BranchId", SqlDbType.Int).Value = (object)branchId ?? DBNull.Value;
                    command.Parameters.Add("@FromDate", SqlDbType.DateTime).Value = (object)fromDate ?? DBNull.Value;
                    command.Parameters.Add("@ToDate", SqlDbType.DateTime).Value = (object)toDate ?? DBNull.Value;

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            model.Items.Add(ReadListItem(reader));
                        }
                    }
                }

                if (selectedId.HasValue)
                {
                    model.SelectedDetails = GetDetails(selectedId.Value);
                }
                else if (model.Items.Count > 0)
                {
                    model.SelectedId = model.Items[0].NoteId;
                    model.SelectedDetails = GetDetails(model.Items[0].NoteId);
                }
            }
            catch (SqlException ex)
            {
                model.Warning = "تعذر قراءة شاشة الإشعارات من قاعدة البيانات الحالية: " + ex.Message;
            }

            return model;
        }

        public DiscountNotificationDetailsViewModel GetDetails(int noteId)
        {
            var model = new DiscountNotificationDetailsViewModel { NoteId = noteId };

            try
            {
                using (var connection = _createOpenConnection())
                {
                    LoadHeader(connection, model, noteId);
                    LoadVoucherLines(connection, model, noteId);
                    if (model.NoteId == 0)
                    {
                        model.NoteId = noteId;
                        model.Warnings.Add("الإشعار غير موجود أو ليس ضمن أنواع FrmDiscounts.");
                    }
                }
            }
            catch (SqlException ex)
            {
                model.Warnings.Add("تعذر فتح تفاصيل الإشعار: " + ex.Message);
            }

            return model;
        }

        private static void LoadHeader(SqlConnection connection, DiscountNotificationDetailsViewModel model, int noteId)
        {
            using (var command = new SqlCommand(@"
SELECT TOP 1
    n.NoteID,
    CONVERT(nvarchar(50), n.NoteSerial) AS NoteSerial,
    CONVERT(nvarchar(50), n.NoteSerial1) AS NoteSerial1,
    n.NoteDate,
    n.NoteType,
    n.Note_Value,
    n.TotalValue,
    n.VAT,
    n.VATYou,
    n.Remark,
    n.ORDER_NO,
    n.DocumentCurrencyCode,
    n.Currency_rate,
    n.FiterWaiverNoteSerial,
    n.Invoicetype,
    n.InvoiceTypeCodename,
    c.CusName,
    c.CusNamee,
    b.branch_name,
    b.branch_namee
FROM Notes n
LEFT JOIN TblCustemers c ON n.CusID = c.CusID
LEFT JOIN TblBranchesData b ON n.branch_no = b.branch_id
WHERE n.NoteID = @NoteId
  AND n.NoteType IN (9, 10, 8034, 9082, 9083, 9089, 9090, 9099);", connection))
            {
                command.Parameters.Add("@NoteId", SqlDbType.Int).Value = noteId;
                using (var reader = command.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        return;
                    }

                    var item = ReadListItem(reader);
                    model.NoteId = item.NoteId;
                    model.NoteSerial = item.NoteSerial;
                    model.NoteSerial1 = item.NoteSerial1;
                    model.NoteDate = item.NoteDate;
                    model.NoteType = item.NoteType;
                    model.NotificationTypeName = item.NotificationTypeName;
                    model.CustomerName = item.CustomerName;
                    model.BranchName = item.BranchName;
                    model.NoteValue = item.NoteValue;
                    model.TotalValue = item.TotalValue;
                    model.Vat = item.Vat;
                    model.Remark = item.Remark;
                    model.OrderNo = item.OrderNo;
                    model.CurrencyCode = ReadString(reader, "DocumentCurrencyCode");
                    model.CurrencyRate = ReadDecimal(reader, "Currency_rate").GetValueOrDefault(1m);
                    model.FiterWaiverNoteSerial = ReadString(reader, "FiterWaiverNoteSerial");
                    model.InvoiceType = ReadInt(reader, "Invoicetype");
                    model.InvoiceTypeCodeName = ReadString(reader, "InvoiceTypeCodename");
                }
            }
        }

        private static void LoadVoucherLines(SqlConnection connection, DiscountNotificationDetailsViewModel model, int noteId)
        {
            if (!TableExists(connection, "DOUBLE_ENTREY_VOUCHERS"))
            {
                model.Warnings.Add("جدول DOUBLE_ENTREY_VOUCHERS غير موجود في قاعدة البيانات الحالية.");
                return;
            }

            using (var command = new SqlCommand(@"
SELECT
    v.Double_Entry_Vouchers_ID,
    v.DEV_ID_Line_No,
    v.Notes_ID,
    v.RecordDate,
    v.Account_Code,
    a.Account_Serial,
    a.Account_Name,
    a.Account_NameEng,
    v.Value,
    v.Credit_Or_Debit,
    v.Double_Entry_Vouchers_Description,
    v.branch_id
FROM DOUBLE_ENTREY_VOUCHERS v
LEFT JOIN ACCOUNTS a ON v.Account_Code = a.Account_Code
WHERE v.Notes_ID = @NoteId
ORDER BY v.Double_Entry_Vouchers_ID, v.DEV_ID_Line_No;", connection))
            {
                command.Parameters.Add("@NoteId", SqlDbType.Int).Value = noteId;
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var value = ReadDecimal(reader, "Value").GetValueOrDefault();
                        var direction = ReadString(reader, "Credit_Or_Debit");
                        var isCredit = direction == "1" || string.Equals(direction, "Credit", StringComparison.OrdinalIgnoreCase);
                        var line = new DiscountNotificationVoucherLine
                        {
                            VoucherId = ReadInt(reader, "Double_Entry_Vouchers_ID").GetValueOrDefault(),
                            LineNo = ReadInt(reader, "DEV_ID_Line_No"),
                            NoteId = ReadInt(reader, "Notes_ID"),
                            RecordDate = ReadDate(reader, "RecordDate"),
                            AccountCode = ReadString(reader, "Account_Code"),
                            AccountSerial = ReadString(reader, "Account_Serial"),
                            AccountName = ReadString(reader, "Account_Name"),
                            AccountDisplay = AccountDisplay(ReadString(reader, "Account_Serial"), ReadString(reader, "Account_Name"), ReadString(reader, "Account_NameEng")),
                            Debit = isCredit ? 0m : value,
                            Credit = isCredit ? value : 0m,
                            Description = ReadString(reader, "Double_Entry_Vouchers_Description"),
                            BranchId = ReadInt(reader, "branch_id")
                        };

                        model.TotalDebit += line.Debit;
                        model.TotalCredit += line.Credit;
                        model.VoucherLines.Add(line);
                    }
                }
            }

            model.VoucherLineCount = model.VoucherLines.Count;
            if (model.VoucherLines.Count > 0)
            {
                model.DebitAccountDisplay = model.VoucherLines[0].AccountDisplay;
                if (model.VoucherLines.Count > 1)
                {
                    model.CreditAccountDisplay = model.VoucherLines[model.VoucherLines.Count - 1].AccountDisplay;
                }
            }
        }

        private static DiscountNotificationListItem ReadListItem(IDataRecord reader)
        {
            var noteType = ReadInt(reader, "NoteType");
            return new DiscountNotificationListItem
            {
                NoteId = ReadInt(reader, "NoteID").GetValueOrDefault(),
                NoteSerial = ReadString(reader, "NoteSerial"),
                NoteSerial1 = ReadString(reader, "NoteSerial1"),
                NoteDate = ReadDate(reader, "NoteDate"),
                NoteType = noteType,
                NotificationTypeName = GetNotificationTypeName(noteType),
                CustomerName = First(ReadString(reader, "CusName"), ReadString(reader, "CusNamee")),
                BranchName = First(ReadString(reader, "branch_name"), ReadString(reader, "branch_namee")),
                NoteValue = ReadDecimal(reader, "Note_Value").GetValueOrDefault(),
                TotalValue = ReadDecimal(reader, "TotalValue").GetValueOrDefault(),
                Vat = ReadDecimal(reader, "VAT").GetValueOrDefault(),
                Remark = ReadString(reader, "Remark"),
                OrderNo = ReadString(reader, "ORDER_NO")
            };
        }

        private static string GetNotificationTypeName(int? noteType)
        {
            switch (noteType.GetValueOrDefault())
            {
                case 9: return "خصم مسموح به";
                case 10: return "خصم مكتسب";
                case 8034: return "ديون معدومة";
                case 9082: return "إشعار ضريبي";
                case 9083: return "إشعار ضريبي عكسي";
                case 9089: return "إشعار مرتبط بالفاتورة";
                case 9090: return "إشعار مرتبط بالفاتورة";
                case 9099: return "إشعار خاص";
                default: return "إشعار";
            }
        }

        private static string AccountDisplay(string serial, string nameAr, string nameEn)
        {
            var name = First(nameAr, nameEn);
            if (string.IsNullOrWhiteSpace(serial) && string.IsNullOrWhiteSpace(name))
            {
                return "الحساب غير موجود";
            }

            return string.IsNullOrWhiteSpace(serial) ? name : serial + " - " + name;
        }

        private static bool TableExists(SqlConnection connection, string tableName)
        {
            using (var command = new SqlCommand("SELECT COUNT(1) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = @TableName;", connection))
            {
                command.Parameters.Add("@TableName", SqlDbType.NVarChar, 128).Value = tableName;
                return Convert.ToInt32(command.ExecuteScalar()) > 0;
            }
        }

        private static string First(params string[] values)
        {
            foreach (var value in values)
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
            }

            return null;
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

        private static int? ReadInt(IDataRecord reader, string columnName)
        {
            return !HasColumn(reader, columnName) || reader[columnName] == DBNull.Value ? (int?)null : Convert.ToInt32(reader[columnName]);
        }

        private static decimal? ReadDecimal(IDataRecord reader, string columnName)
        {
            return !HasColumn(reader, columnName) || reader[columnName] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(reader[columnName]);
        }

        private static DateTime? ReadDate(IDataRecord reader, string columnName)
        {
            return !HasColumn(reader, columnName) || reader[columnName] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader[columnName]);
        }
    }
}
