using System;
using System.Data.SqlClient;
using MyERP.Areas.MainErp.Interfaces;
using MyERP.Areas.MainErp.ViewModels.Reports;

namespace MyERP.Areas.MainErp.Repositories.Reports
{
    public class SalesReportRepository
    {
        private readonly IMainErpDbConnectionFactory _connectionFactory;

        public SalesReportRepository(IMainErpDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public SalesSummaryReportViewModel GetSalesSummary(DateTime? fromDate, DateTime? toDate, int? branchId, int? userId, int? customerId)
        {
            var model = new SalesSummaryReportViewModel
            {
                FromDate = fromDate,
                ToDate = toDate,
                BranchId = branchId,
                UserId = userId,
                CustomerId = customerId,
                Warning = "تم استخدام فلتر محافظ على أنواع الفواتير العامة Transaction_Type IN (22, 29). يحتاج التصنيف النهائي لأنواع فواتير المبيعات إلى اعتماد من تحليل ERP الرئيسي."
            };

            try
            {
                using (var connection = _connectionFactory.CreateOpenConnection())
                using (var command = new SqlCommand(@"
SELECT
    CAST(t.Transaction_Date AS date) AS SalesDate,
    b.branch_name AS BranchName,
    u.UserName,
    COUNT(1) AS InvoiceCount,
    SUM(CAST(ISNULL(t.Total, ISNULL(t.NetValue, 0)) AS decimal(19, 4))) AS TotalBeforeVat,
    SUM(CAST(ISNULL(t.VAT, ISNULL(t.TaxValue, ISNULL(t.TaxAddValue, 0))) AS decimal(19, 4))) AS VatValue,
    SUM(CAST(COALESCE(t.GTotal, t.NetValue + ISNULL(t.VAT, 0), t.Total + ISNULL(t.VAT, 0), t.Total, 0) AS decimal(19, 4))) AS TotalValue
FROM Transactions t
LEFT JOIN TblBranchesData b ON t.BranchId = b.branch_id
LEFT JOIN TblUsers u ON t.UserID = u.UserID
LEFT JOIN TblCustemers c ON t.CusID = c.CusID
WHERE t.Transaction_Date IS NOT NULL
  AND t.Transaction_Type IN (22, 29)
  AND (@FromDate IS NULL OR t.Transaction_Date >= @FromDate)
  AND (@ToDate IS NULL OR t.Transaction_Date < DATEADD(day, 1, @ToDate))
  AND (@BranchId IS NULL OR t.BranchId = @BranchId)
  AND (@UserId IS NULL OR t.UserID = @UserId)
  AND (@CustomerId IS NULL OR t.CusID = @CustomerId)
GROUP BY CAST(t.Transaction_Date AS date), b.branch_name, u.UserName
ORDER BY CAST(t.Transaction_Date AS date), b.branch_name, u.UserName;", connection))
                {
                    command.Parameters.AddWithValue("@FromDate", (object)fromDate ?? DBNull.Value);
                    command.Parameters.AddWithValue("@ToDate", (object)toDate ?? DBNull.Value);
                    command.Parameters.AddWithValue("@BranchId", (object)branchId ?? DBNull.Value);
                    command.Parameters.AddWithValue("@UserId", (object)userId ?? DBNull.Value);
                    command.Parameters.AddWithValue("@CustomerId", (object)customerId ?? DBNull.Value);

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var row = new SalesSummaryReportRowViewModel
                            {
                                Date = reader["SalesDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["SalesDate"]),
                                BranchName = Convert.ToString(reader["BranchName"]),
                                UserName = Convert.ToString(reader["UserName"]),
                                InvoiceCount = reader["InvoiceCount"] == DBNull.Value ? 0 : Convert.ToInt32(reader["InvoiceCount"]),
                                TotalBeforeVat = reader["TotalBeforeVat"] == DBNull.Value ? 0m : Convert.ToDecimal(reader["TotalBeforeVat"]),
                                Vat = reader["VatValue"] == DBNull.Value ? 0m : Convert.ToDecimal(reader["VatValue"]),
                                Total = reader["TotalValue"] == DBNull.Value ? 0m : Convert.ToDecimal(reader["TotalValue"])
                            };

                            model.InvoiceCount += row.InvoiceCount;
                            model.TotalBeforeVat += row.TotalBeforeVat;
                            model.TotalVat += row.Vat;
                            model.TotalAmount += row.Total;
                            model.Rows.Add(row);
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                model.Warning = "تعذر تشغيل ملخص المبيعات على قاعدة البيانات الحالية: " + ex.Message;
            }

            return model;
        }
    }
}
