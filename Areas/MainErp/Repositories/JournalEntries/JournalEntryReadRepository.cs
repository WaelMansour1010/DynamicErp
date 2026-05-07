using System;
using System.Data.SqlClient;
using MyERP.Areas.MainErp.Infrastructure.Localization;
using MyERP.Areas.MainErp.Interfaces;
using MyERP.Areas.MainErp.ViewModels;
using MyERP.Areas.MainErp.ViewModels.JournalEntries;

namespace MyERP.Areas.MainErp.Repositories.JournalEntries
{
    public class JournalEntryReadRepository
    {
        private readonly IMainErpDbConnectionFactory _connectionFactory;

        public JournalEntryReadRepository(IMainErpDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public PagedReadResult<JournalEntryListItemViewModel> Search(string searchText, int? branchId, DateTime? fromDate, DateTime? toDate, int page, int pageSize)
        {
            var result = new PagedReadResult<JournalEntryListItemViewModel>();
            page = page < 1 ? 1 : page;
            pageSize = pageSize < 1 ? 25 : pageSize;

            try
            {
                using (var connection = _connectionFactory.CreateOpenConnection())
                using (var command = new SqlCommand(@"
WITH JournalRows AS (
    SELECT
        ROW_NUMBER() OVER (ORDER BY v.RecordDate DESC, v.Double_Entry_Vouchers_ID DESC, v.DEV_ID_Line_No) AS RowNo,
        COUNT(1) OVER() AS TotalCount,
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
        n.NoteSerial
    FROM DOUBLE_ENTREY_VOUCHERS v
    LEFT JOIN ACCOUNTS a ON v.Account_Code = a.Account_Code
    LEFT JOIN Notes n ON v.Notes_ID = n.NoteID
    WHERE (@SearchText IS NULL
        OR v.Account_Code LIKE @SearchLike
        OR a.Account_Name LIKE @SearchLike
        OR v.Double_Entry_Vouchers_Description LIKE @SearchLike
        OR CONVERT(nvarchar(50), v.Double_Entry_Vouchers_ID) LIKE @SearchLike
        OR CONVERT(nvarchar(50), n.NoteSerial) LIKE @SearchLike)
      AND (@BranchId IS NULL OR v.branch_id = @BranchId)
      AND (@FromDate IS NULL OR v.RecordDate >= @FromDate)
      AND (@ToDate IS NULL OR v.RecordDate < DATEADD(day, 1, @ToDate))
)
SELECT *
FROM JournalRows
WHERE RowNo BETWEEN @StartRow AND @EndRow
ORDER BY RowNo;", connection))
                {
                    command.Parameters.AddWithValue("@SearchText", string.IsNullOrWhiteSpace(searchText) ? (object)DBNull.Value : searchText);
                    command.Parameters.AddWithValue("@SearchLike", string.IsNullOrWhiteSpace(searchText) ? (object)DBNull.Value : "%" + searchText + "%");
                    command.Parameters.AddWithValue("@BranchId", (object)branchId ?? DBNull.Value);
                    command.Parameters.AddWithValue("@FromDate", (object)fromDate ?? DBNull.Value);
                    command.Parameters.AddWithValue("@ToDate", (object)toDate ?? DBNull.Value);
                    command.Parameters.AddWithValue("@StartRow", ((page - 1) * pageSize) + 1);
                    command.Parameters.AddWithValue("@EndRow", page * pageSize);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            if (result.TotalCount == 0 && reader["TotalCount"] != DBNull.Value)
                            {
                                result.TotalCount = Convert.ToInt32(reader["TotalCount"]);
                            }

                            var value = reader["Value"] == DBNull.Value ? 0m : Convert.ToDecimal(reader["Value"]);
                            var direction = Convert.ToString(reader["Credit_Or_Debit"]);
                            var isCredit = direction == "1" || string.Equals(direction, "Credit", StringComparison.OrdinalIgnoreCase);

                            result.Items.Add(new JournalEntryListItemViewModel
                            {
                                VoucherId = Convert.ToInt32(reader["Double_Entry_Vouchers_ID"]),
                                LineNo = reader["DEV_ID_Line_No"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["DEV_ID_Line_No"]),
                                NoteId = reader["Notes_ID"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["Notes_ID"]),
                                NoteSerial = Convert.ToString(reader["NoteSerial"]),
                                RecordDate = reader["RecordDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["RecordDate"]),
                                AccountCode = Convert.ToString(reader["Account_Code"]),
                                AccountName = Convert.ToString(reader["Account_Name"]),
                                AccountSerial = Convert.ToString(reader["Account_Serial"]),
                                AccountDisplay = MainErpEntityLocalization.AccountDisplay(
                                    Convert.ToString(reader["Account_Serial"]),
                                    Convert.ToString(reader["Account_Name"]),
                                    Convert.ToString(reader["Account_NameEng"])),
                                Debit = isCredit ? 0m : value,
                                Credit = isCredit ? value : 0m,
                                Description = Convert.ToString(reader["Double_Entry_Vouchers_Description"]),
                                BranchId = reader["branch_id"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["branch_id"]),
                                ProjectId = reader["project_id"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["project_id"])
                            });
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                result.Warning = MainErpLocalizationService.T("JournalEntries") + ": " + ex.Message;
            }

            return result;
        }

        public JournalEntryDetailsViewModel GetDetailsByNote(int noteId)
        {
            var model = new JournalEntryDetailsViewModel { NoteId = noteId };

            try
            {
                using (var connection = _connectionFactory.CreateOpenConnection())
                {
                    LoadNoteHeader(connection, model, "Notes", noteId);
                    LoadNoteHeader(connection, model, "Notes1", noteId);
                    LoadJournalLines(connection, model, "DOUBLE_ENTREY_VOUCHERS", "Notes_ID = @NoteId", noteId, null);
                    LoadJournalLines(connection, model, "DOUBLE_ENTREY_VOUCHERS1", "Notes_ID = @NoteId", noteId, null);
                    FinalizeTotals(model);
                }
            }
            catch (SqlException ex)
            {
                model.Warning = MainErpLocalizationService.T("JournalEntryDetails") + ": " + ex.Message;
            }

            return model;
        }

        public JournalEntryDetailsViewModel GetDetailsByVoucher(int voucherId)
        {
            var model = new JournalEntryDetailsViewModel { VoucherId = voucherId };

            try
            {
                using (var connection = _connectionFactory.CreateOpenConnection())
                {
                    LoadJournalLines(connection, model, "DOUBLE_ENTREY_VOUCHERS", "Double_Entry_Vouchers_ID = @VoucherId", null, voucherId);
                    LoadJournalLines(connection, model, "DOUBLE_ENTREY_VOUCHERS1", "Double_Entry_Vouchers_ID = @VoucherId", null, voucherId);
                    if (model.Lines.Count > 0 && model.Lines[0].NoteId.HasValue)
                    {
                        model.NoteId = model.Lines[0].NoteId;
                        LoadNoteHeader(connection, model, "Notes", model.Lines[0].NoteId.Value);
                        LoadNoteHeader(connection, model, "Notes1", model.Lines[0].NoteId.Value);
                    }
                    FinalizeTotals(model);
                }
            }
            catch (SqlException ex)
            {
                model.Warning = MainErpLocalizationService.T("JournalEntryDetails") + ": " + ex.Message;
            }

            return model;
        }

        private static void LoadNoteHeader(SqlConnection connection, JournalEntryDetailsViewModel model, string tableName, int noteId)
        {
            if (!TableExists(connection, tableName))
            {
                return;
            }

            using (var command = new SqlCommand(@"
SELECT TOP 1 NoteID, NoteDate, NoteType, NoteSerial, Note_Value, Remark, TblLCID
FROM " + Bracket(tableName) + @"
WHERE NoteID = @NoteId;", connection))
            {
                command.Parameters.AddWithValue("@NoteId", noteId);
                using (var reader = command.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        return;
                    }

                    if (string.IsNullOrWhiteSpace(model.SourceTable))
                    {
                        model.SourceTable = tableName;
                        model.NoteId = ReadInt(reader, "NoteID");
                        model.NoteDate = ReadDate(reader, "NoteDate");
                        model.NoteType = ReadString(reader, "NoteType");
                        model.NoteSerial = ReadString(reader, "NoteSerial");
                        model.NoteValue = ReadDecimal(reader, "Note_Value");
                        model.Remark = ReadString(reader, "Remark");
                        model.TblLCID = ReadInt(reader, "TblLCID");
                    }
                }
            }
        }

        private static void LoadJournalLines(SqlConnection connection, JournalEntryDetailsViewModel model, string tableName, string predicate, int? noteId, int? voucherId)
        {
            if (!TableExists(connection, tableName))
            {
                model.Warnings.Add(tableName + " " + MainErpLocalizationService.T("TableNotFound"));
                return;
            }

            using (var command = new SqlCommand(@"
SELECT
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
    " + (tableName == "DOUBLE_ENTREY_VOUCHERS1" ? "v.opening_balance_voucher_id" : "CAST(NULL AS float) AS opening_balance_voucher_id") + @"
FROM " + Bracket(tableName) + @" v
LEFT JOIN ACCOUNTS a ON v.Account_Code = a.Account_Code
WHERE " + predicate + @"
ORDER BY v.Double_Entry_Vouchers_ID, v.DEV_ID_Line_No;", connection))
            {
                command.Parameters.AddWithValue("@NoteId", (object)noteId ?? DBNull.Value);
                command.Parameters.AddWithValue("@VoucherId", (object)voucherId ?? DBNull.Value);

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var value = ReadDecimal(reader, "Value").GetValueOrDefault();
                        var direction = ReadString(reader, "Credit_Or_Debit");
                        var isCredit = direction == "1" || string.Equals(direction, "Credit", StringComparison.OrdinalIgnoreCase);

                        model.Lines.Add(new JournalEntryDetailLineViewModel
                        {
                            SourceTable = tableName,
                            VoucherId = ReadInt(reader, "Double_Entry_Vouchers_ID").GetValueOrDefault(),
                            LineNo = ReadInt(reader, "DEV_ID_Line_No"),
                            NoteId = ReadInt(reader, "Notes_ID"),
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
                        });
                    }
                }
            }
        }

        private static void FinalizeTotals(JournalEntryDetailsViewModel model)
        {
            foreach (var line in model.Lines)
            {
                model.TotalDebit += line.Debit;
                model.TotalCredit += line.Credit;
                if (!model.VoucherId.HasValue)
                {
                    model.VoucherId = line.VoucherId;
                }
            }
        }

        private static bool TableExists(SqlConnection connection, string tableName)
        {
            using (var command = new SqlCommand("SELECT COUNT(1) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = @TableName;", connection))
            {
                command.Parameters.AddWithValue("@TableName", tableName);
                return Convert.ToInt32(command.ExecuteScalar()) > 0;
            }
        }

        private static string Bracket(string name)
        {
            return "[" + name.Replace("]", "]]") + "]";
        }

        private static bool HasColumn(System.Data.IDataRecord reader, string columnName)
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

        private static string ReadString(System.Data.IDataRecord reader, string columnName)
        {
            return !HasColumn(reader, columnName) || reader[columnName] == DBNull.Value ? null : Convert.ToString(reader[columnName]);
        }

        private static int? ReadInt(System.Data.IDataRecord reader, string columnName)
        {
            return !HasColumn(reader, columnName) || reader[columnName] == DBNull.Value ? (int?)null : Convert.ToInt32(reader[columnName]);
        }

        private static decimal? ReadDecimal(System.Data.IDataRecord reader, string columnName)
        {
            return !HasColumn(reader, columnName) || reader[columnName] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(reader[columnName]);
        }

        private static double? ReadDouble(System.Data.IDataRecord reader, string columnName)
        {
            return !HasColumn(reader, columnName) || reader[columnName] == DBNull.Value ? (double?)null : Convert.ToDouble(reader[columnName]);
        }

        private static DateTime? ReadDate(System.Data.IDataRecord reader, string columnName)
        {
            return !HasColumn(reader, columnName) || reader[columnName] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader[columnName]);
        }
    }
}
