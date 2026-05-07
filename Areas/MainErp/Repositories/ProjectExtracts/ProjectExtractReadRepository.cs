using System;
using System.Data.SqlClient;
using MyERP.Areas.MainErp.Interfaces;
using MyERP.Areas.MainErp.ViewModels;
using MyERP.Areas.MainErp.ViewModels.ProjectExtracts;

namespace MyERP.Areas.MainErp.Repositories.ProjectExtracts
{
    public class ProjectExtractReadRepository
    {
        private readonly IMainErpDbConnectionFactory _connectionFactory;

        public ProjectExtractReadRepository(IMainErpDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public PagedReadResult<ProjectExtractListItemViewModel> Search(string searchText, int? projectId, int? branchId, int page, int pageSize)
        {
            var result = new PagedReadResult<ProjectExtractListItemViewModel>();
            page = page < 1 ? 1 : page;
            pageSize = pageSize < 1 ? 20 : pageSize;

            try
            {
                using (var connection = _connectionFactory.CreateOpenConnection())
                using (var command = new SqlCommand(@"
WITH ExtractRows AS (
    SELECT
        ROW_NUMBER() OVER (ORDER BY pb.id DESC) AS RowNo,
        COUNT(1) OVER() AS TotalCount,
        pb.id,
        pb.bill_date,
        pb.NoteSerial,
        pb.ManualNO,
        pb.project_name,
        p.Fullcode AS ProjectFullCode,
        cust.CusName AS CustomerName,
        pb.total,
        pb.Results,
        pb.FATValue,
        pb.NetValue,
        pb.Branch_NO,
        pb.note_id
    FROM project_billl pb
    LEFT JOIN projects p ON pb.project_no = CONVERT(nvarchar(50), p.id)
    LEFT JOIN Notes n ON pb.note_id = n.NoteID
    LEFT JOIN TblCustemers cust ON n.CusID = cust.CusID
    WHERE (@SearchText IS NULL OR pb.NoteSerial LIKE @SearchLike OR pb.ManualNO LIKE @SearchLike OR pb.project_name LIKE @SearchLike)
      AND (@ProjectId IS NULL OR pb.project_no = CONVERT(nvarchar(50), @ProjectId))
      AND (@BranchId IS NULL OR pb.Branch_NO = @BranchId)
)
SELECT * FROM ExtractRows WHERE RowNo BETWEEN @StartRow AND @EndRow ORDER BY RowNo;", connection))
                {
                    command.Parameters.AddWithValue("@SearchText", string.IsNullOrWhiteSpace(searchText) ? (object)DBNull.Value : searchText);
                    command.Parameters.AddWithValue("@SearchLike", string.IsNullOrWhiteSpace(searchText) ? (object)DBNull.Value : "%" + searchText + "%");
                    command.Parameters.AddWithValue("@ProjectId", (object)projectId ?? DBNull.Value);
                    command.Parameters.AddWithValue("@BranchId", (object)branchId ?? DBNull.Value);
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

                            result.Items.Add(new ProjectExtractListItemViewModel
                            {
                                Id = Convert.ToInt32(reader["id"]),
                                BillDate = reader["bill_date"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["bill_date"]),
                                NoteSerial = Convert.ToString(reader["NoteSerial"]),
                                ManualNo = Convert.ToString(reader["ManualNO"]),
                                ProjectName = Convert.ToString(reader["project_name"]),
                                ProjectFullCode = Convert.ToString(reader["ProjectFullCode"]),
                                CustomerName = Convert.ToString(reader["CustomerName"]),
                                Total = reader["total"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(reader["total"]),
                                Results = reader["Results"] == DBNull.Value ? (double?)null : Convert.ToDouble(reader["Results"]),
                                VatValue = reader["FATValue"] == DBNull.Value ? (double?)null : Convert.ToDouble(reader["FATValue"]),
                                NetValue = reader["NetValue"] == DBNull.Value ? (double?)null : Convert.ToDouble(reader["NetValue"]),
                                BranchNo = reader["Branch_NO"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["Branch_NO"]),
                                NoteId = reader["note_id"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["note_id"])
                            });
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                result.Warning = "Project Extract read model is not available in the configured database yet: " + ex.Message;
            }

            return result;
        }

        public ProjectExtractDetailsViewModel GetDetails(int id)
        {
            try
            {
                using (var connection = _connectionFactory.CreateOpenConnection())
                using (var command = new SqlCommand(@"
SELECT TOP 1
    pb.id, pb.bill_date, pb.NoteSerial, pb.ManualNO, pb.project_name,
    p.Fullcode AS ProjectFullCode, cust.CusName AS CustomerName,
    pb.total, pb.Results, pb.FATValue, pb.NetValue, pb.Branch_NO, pb.note_id,
    pb.Remarks, pb.revenue_account, pb.End_user_account, pb.Sub_user_account,
    pb.advancedPayment, pb.PerformanceBond, pb.PreVAT, pb.AccountCodeVat
FROM project_billl pb
LEFT JOIN projects p ON pb.project_no = CONVERT(nvarchar(50), p.id)
LEFT JOIN Notes n ON pb.note_id = n.NoteID
LEFT JOIN TblCustemers cust ON n.CusID = cust.CusID
WHERE pb.id = @Id;", connection))
                {
                    command.Parameters.AddWithValue("@Id", id);
                    using (var reader = command.ExecuteReader())
                    {
                        if (!reader.Read())
                        {
                            return new ProjectExtractDetailsViewModel { Id = id, Warning = "Project extract record was not found." };
                        }

                        return new ProjectExtractDetailsViewModel
                        {
                            Id = Convert.ToInt32(reader["id"]),
                            BillDate = reader["bill_date"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["bill_date"]),
                            NoteSerial = Convert.ToString(reader["NoteSerial"]),
                            ManualNo = Convert.ToString(reader["ManualNO"]),
                            ProjectName = Convert.ToString(reader["project_name"]),
                            ProjectFullCode = Convert.ToString(reader["ProjectFullCode"]),
                            CustomerName = Convert.ToString(reader["CustomerName"]),
                            Total = reader["total"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(reader["total"]),
                            Results = reader["Results"] == DBNull.Value ? (double?)null : Convert.ToDouble(reader["Results"]),
                            VatValue = reader["FATValue"] == DBNull.Value ? (double?)null : Convert.ToDouble(reader["FATValue"]),
                            NetValue = reader["NetValue"] == DBNull.Value ? (double?)null : Convert.ToDouble(reader["NetValue"]),
                            BranchNo = reader["Branch_NO"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["Branch_NO"]),
                            NoteId = reader["note_id"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["note_id"]),
                            Remarks = Convert.ToString(reader["Remarks"]),
                            RevenueAccount = Convert.ToString(reader["revenue_account"]),
                            EndUserAccount = Convert.ToString(reader["End_user_account"]),
                            SubUserAccount = Convert.ToString(reader["Sub_user_account"]),
                            AdvancedPayment = reader["advancedPayment"] == DBNull.Value ? (double?)null : Convert.ToDouble(reader["advancedPayment"]),
                            PerformanceBond = reader["PerformanceBond"] == DBNull.Value ? (double?)null : Convert.ToDouble(reader["PerformanceBond"]),
                            PreVat = reader["PreVAT"] == DBNull.Value ? (double?)null : Convert.ToDouble(reader["PreVAT"]),
                            VatAccountCode = Convert.ToString(reader["AccountCodeVat"])
                        };
                    }
                }
            }
            catch (SqlException ex)
            {
                return new ProjectExtractDetailsViewModel { Id = id, Warning = "Project extract details are not available in the configured database yet: " + ex.Message };
            }
        }
    }
}
