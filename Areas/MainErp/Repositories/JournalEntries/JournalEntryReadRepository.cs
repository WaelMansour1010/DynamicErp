using System;
using System.Data.SqlClient;
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
                result.Warning = "Journal-entry read model is not available in the configured database yet: " + ex.Message;
            }

            return result;
        }
    }
}
