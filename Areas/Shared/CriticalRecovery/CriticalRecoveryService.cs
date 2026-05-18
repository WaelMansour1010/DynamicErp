using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Text;
using System.Web;

namespace MyERP.Areas.Shared.CriticalRecovery
{
    public class CriticalRecoveryService
    {
        private readonly string _connectionString;

        public CriticalRecoveryService(string areaName)
        {
            var connection = (!string.IsNullOrWhiteSpace(areaName) ? ConfigurationManager.ConnectionStrings[areaName + "Connection"] : null)
                ?? (!string.IsNullOrWhiteSpace(areaName) ? ConfigurationManager.ConnectionStrings[areaName + "ErpConnection"] : null)
                ?? (string.Equals(areaName, "Pos", StringComparison.OrdinalIgnoreCase) ? ConfigurationManager.ConnectionStrings["KishnyCashConnection"] : null)
                ?? (string.Equals(areaName, "MainErp", StringComparison.OrdinalIgnoreCase) ? ConfigurationManager.ConnectionStrings["MainErp_ConnectionString"] : null)
                ?? ConfigurationManager.ConnectionStrings["DefaultConnection"]
                ?? ConfigurationManager.ConnectionStrings["MyERPConnection"]
                ?? ConfigurationManager.ConnectionStrings["ErpConnection"]
                ?? ConfigurationManager.ConnectionStrings["MyErpConnectionString"];

            if (connection == null)
            {
                throw new InvalidOperationException("No ERP SQL connection string was found. Expected DefaultConnection, MyERPConnection, or ErpConnection.");
            }

            _connectionString = connection.ConnectionString;
        }

        public CriticalRecoveryIndexViewModel BuildIndex(string areaName)
        {
            return new CriticalRecoveryIndexViewModel
            {
                AreaName = areaName,
                Filter = BuildDefaultFilter(),
                BranchOptions = GetBranchOptions(),
                SnapshotBatches = GetSnapshotBatches(),
                AuditItems = GetAuditItems()
            };
        }

        public CriticalRecoveryImpactViewModel Analyze(CriticalRecoveryFilterViewModel filter)
        {
            var model = new CriticalRecoveryImpactViewModel();
            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand("dbo.usp_CriticalRecovery_AnalyzeInvoices", connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.CommandTimeout = 120;
                AddFilterParameters(command, filter);
                connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        model.Invoices.Add(new CriticalRecoveryInvoiceImpactViewModel
                        {
                            TransactionId = ReadLong(reader, "Transaction_ID"),
                            InvoiceNo = Convert.ToString(reader["InvoiceNo"]),
                            InvoiceType = ReadInt(reader, "InvoiceType"),
                            OperationTypeName = Convert.ToString(reader["OperationTypeName"]),
                            BranchId = ReadInt(reader, "BranchId"),
                            TransactionDate = ReadDate(reader, "Transaction_Date"),
                            CustomerName = Convert.ToString(reader["CustomerName"]),
                            Value = ReadDecimal(reader, "Note_Value"),
                            IsPosted = ReadInt(reader, "IsPosted") != 0,
                            IsClosed = ReadInt(reader, "IsClosed") != 0,
                            KycReferenceIds = Convert.ToString(reader["KycReferenceIds"])
                        });
                    }

                    if (reader.NextResult())
                    {
                        while (reader.Read())
                        {
                            model.Dependencies.Add(new CriticalRecoveryDependencyViewModel
                            {
                                TransactionId = ReadLong(reader, "Transaction_ID"),
                                ModuleName = Convert.ToString(reader["ModuleName"]),
                                TableName = Convert.ToString(reader["TableName"]),
                                RowCount = ReadInt(reader, "RowCount"),
                                IsProtected = ReadInt(reader, "IsProtected") != 0,
                                ActionPolicy = Convert.ToString(reader["ActionPolicy"])
                            });
                        }
                    }

                    if (reader.NextResult())
                    {
                        while (reader.Read())
                        {
                            model.Warnings.Add(Convert.ToString(reader["WarningMessage"]));
                        }
                    }
                }
            }

            if (filter != null && filter.HasKycLink)
            {
                model.Warnings.Add("بيانات KYC الأساسية محمية. يتم حفظ روابط الفاتورة فقط للاسترجاع، ولا يتم حذف بيانات العميل أو الكارت أو المرفقات.");
            }

            return model;
        }

        public CriticalRecoveryOperationResult Initiate(CriticalRecoveryFilterViewModel filter, CriticalRecoveryRequestViewModel request, string userName, HttpRequestBase httpRequest)
        {
            ValidateRequest(request, false);
            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand("dbo.usp_CriticalRecovery_InitiateRequest", connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.CommandTimeout = 180;
                AddFilterParameters(command, filter);
                AddRequestParameters(command, request, userName, httpRequest);
                connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new CriticalRecoveryOperationResult
                        {
                            Success = ReadInt(reader, "Success") != 0,
                            RequestId = ReadNullableInt(reader, "RequestId"),
                            SnapshotBatchId = ReadNullableLong(reader, "SnapshotBatchId"),
                            Message = Convert.ToString(reader["Message"])
                        };
                    }
                }
            }

            return new CriticalRecoveryOperationResult { Success = false, Message = "No initiation result was returned." };
        }

        public CriticalRecoveryOperationResult ApproveAndExecute(CriticalRecoveryRequestViewModel request, string userName, HttpRequestBase httpRequest)
        {
            ValidateRequest(request, true);
            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand("dbo.usp_CriticalRecovery_ApproveAndExecute", connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.CommandTimeout = 600;
                command.Parameters.AddWithValue("@RequestId", request.RequestId.GetValueOrDefault());
                command.Parameters.AddWithValue("@ApprovedBy", userName ?? string.Empty);
                command.Parameters.AddWithValue("@ApproverSecondaryPassword", request.ApproverSecondaryPassword ?? request.SecondaryPassword ?? string.Empty);
                command.Parameters.AddWithValue("@DangerConfirmation", request.DangerConfirmation ?? string.Empty);
                command.Parameters.AddWithValue("@AllowPhysicalDelete", request.AllowPhysicalDelete);
                command.Parameters.AddWithValue("@DeleteOrphanKycRecords", request.DeleteOrphanKycRecords);
                AddRequestMetadata(command, httpRequest);
                connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new CriticalRecoveryOperationResult
                        {
                            Success = ReadInt(reader, "Success") != 0,
                            RequestId = ReadNullableInt(reader, "RequestId"),
                            SnapshotBatchId = ReadNullableLong(reader, "SnapshotBatchId"),
                            Message = Convert.ToString(reader["Message"])
                        };
                    }
                }
            }

            return new CriticalRecoveryOperationResult { Success = false, Message = "No execution result was returned." };
        }

        public CriticalRecoveryOperationResult Restore(CriticalRecoveryRestoreViewModel restore, string userName, HttpRequestBase httpRequest)
        {
            if (restore == null || !restore.SnapshotBatchId.HasValue)
            {
                throw new InvalidOperationException("Snapshot batch is required for restore.");
            }

            if (string.IsNullOrWhiteSpace(restore.SecondaryPassword))
            {
                throw new InvalidOperationException("Restore requires current admin password.");
            }

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand("dbo.usp_CriticalRecovery_Restore", connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.CommandTimeout = 600;
                command.Parameters.AddWithValue("@SnapshotBatchId", restore.SnapshotBatchId.Value);
                command.Parameters.AddWithValue("@TransactionId", (object)restore.TransactionId ?? DBNull.Value);
                command.Parameters.AddWithValue("@RestoreScope", restore.RestoreScope ?? "Full");
                command.Parameters.AddWithValue("@RestoreBy", userName ?? string.Empty);
                command.Parameters.AddWithValue("@SecondaryPassword", restore.SecondaryPassword ?? string.Empty);
                command.Parameters.AddWithValue("@Reason", string.IsNullOrWhiteSpace(restore.Reason) ? "Admin password confirmed restore." : restore.Reason);
                AddRequestMetadata(command, httpRequest);
                connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new CriticalRecoveryOperationResult
                        {
                            Success = ReadInt(reader, "Success") != 0,
                            SnapshotBatchId = ReadNullableLong(reader, "SnapshotBatchId"),
                            Message = Convert.ToString(reader["Message"])
                        };
                    }
                }
            }

            return new CriticalRecoveryOperationResult { Success = false, Message = "No restore result was returned." };
        }

        public byte[] BuildExcelExport(CriticalRecoveryImpactViewModel impact)
        {
            var html = new StringBuilder();
            html.Append("<html><head><meta charset=\"utf-8\" /></head><body>");
            html.Append("<h2>معاينة مركز الاسترجاع الحرج</h2>");
            html.Append("<table border=\"1\"><tr><th>رقم الحركة</th><th>رقم الفاتورة</th><th>نوع الفاتورة</th><th>الفرع</th><th>التاريخ</th><th>العميل</th><th>القيمة</th><th>مرحل</th><th>مغلق</th><th>مراجع KYC</th></tr>");
            foreach (var invoice in impact.Invoices)
            {
                html.Append("<tr><td>").Append(invoice.TransactionId).Append("</td><td>").Append(HttpUtility.HtmlEncode(invoice.InvoiceNo)).Append("</td><td>")
                    .Append(HttpUtility.HtmlEncode(invoice.OperationTypeName)).Append("</td><td>").Append(invoice.BranchId).Append("</td><td>").Append(invoice.TransactionDate.ToString("yyyy-MM-dd"))
                    .Append("</td><td>").Append(HttpUtility.HtmlEncode(invoice.CustomerName)).Append("</td><td>").Append(invoice.Value.ToString("0.00"))
                    .Append("</td><td>").Append(invoice.IsPosted).Append("</td><td>").Append(invoice.IsClosed).Append("</td><td>")
                    .Append(HttpUtility.HtmlEncode(invoice.KycReferenceIds)).Append("</td></tr>");
            }

            html.Append("</table><h2>التأثيرات المرتبطة</h2><table border=\"1\"><tr><th>رقم الحركة</th><th>الموديول</th><th>الجدول</th><th>عدد الصفوف</th><th>محمي</th><th>السياسة</th></tr>");
            foreach (var dependency in impact.Dependencies)
            {
                html.Append("<tr><td>").Append(dependency.TransactionId).Append("</td><td>").Append(HttpUtility.HtmlEncode(dependency.ModuleName)).Append("</td><td>")
                    .Append(HttpUtility.HtmlEncode(dependency.TableName)).Append("</td><td>").Append(dependency.RowCount).Append("</td><td>")
                    .Append(dependency.IsProtected).Append("</td><td>").Append(HttpUtility.HtmlEncode(dependency.ActionPolicy)).Append("</td></tr>");
            }

            html.Append("</table></body></html>");
            return Encoding.UTF8.GetBytes(html.ToString());
        }

        private static CriticalRecoveryFilterViewModel BuildDefaultFilter()
        {
            var today = DateTime.Today;
            return new CriticalRecoveryFilterViewModel
            {
                DateFrom = today,
                DateTo = today,
                InvoiceScope = "SalesOnly",
                InvoiceSource = "Both",
                OperationKind = "All"
            };
        }

        private IList<CriticalRecoveryLookupOption> GetBranchOptions()
        {
            var result = new List<CriticalRecoveryLookupOption>();
            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(@"
IF OBJECT_ID('dbo.TblBranchesData','U') IS NOT NULL
BEGIN
    SELECT CONVERT(nvarchar(20), branch_id) AS Value,
           COALESCE(NULLIF(branch_name, N''), NULLIF(branch_namee, N''), N'فرع ' + CONVERT(nvarchar(20), branch_id)) AS Text
    FROM dbo.TblBranchesData
    WHERE branch_id IS NOT NULL
    ORDER BY branch_id;
END", connection))
            {
                connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(new CriticalRecoveryLookupOption
                        {
                            Value = Convert.ToString(reader["Value"]),
                            Text = Convert.ToString(reader["Text"])
                        });
                    }
                }
            }

            return result;
        }

        private IList<CriticalRecoverySnapshotBatchViewModel> GetSnapshotBatches()
        {
            var result = new List<CriticalRecoverySnapshotBatchViewModel>();
            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(@"IF OBJECT_ID('dbo.CriticalRecoverySnapshotBatch','U') IS NOT NULL
SELECT TOP 25 SnapshotBatchId, CreatedAt, RequestedBy, ApprovedBy, Mode, Reason, Status, InvoiceCount, SnapshotRowCount
FROM dbo.CriticalRecoverySnapshotBatch
ORDER BY SnapshotBatchId DESC", connection))
            {
                connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(new CriticalRecoverySnapshotBatchViewModel
                        {
                            SnapshotBatchId = ReadLong(reader, "SnapshotBatchId"),
                            CreatedAt = ReadDate(reader, "CreatedAt"),
                            RequestedBy = Convert.ToString(reader["RequestedBy"]),
                            ApprovedBy = Convert.ToString(reader["ApprovedBy"]),
                            Mode = Convert.ToString(reader["Mode"]),
                            Reason = Convert.ToString(reader["Reason"]),
                            Status = Convert.ToString(reader["Status"]),
                            InvoiceCount = ReadInt(reader, "InvoiceCount"),
                            SnapshotRowCount = ReadInt(reader, "SnapshotRowCount")
                        });
                    }
                }
            }

            return result;
        }

        private IList<CriticalRecoveryAuditViewModel> GetAuditItems()
        {
            var result = new List<CriticalRecoveryAuditViewModel>();
            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(@"IF OBJECT_ID('dbo.CriticalRecoveryAudit','U') IS NOT NULL
SELECT TOP 25 AuditId, CreatedAt, ActionName, OperatorName, ApproverName, Result, Message
FROM dbo.CriticalRecoveryAudit
ORDER BY AuditId DESC", connection))
            {
                connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(new CriticalRecoveryAuditViewModel
                        {
                            AuditId = ReadLong(reader, "AuditId"),
                            CreatedAt = ReadDate(reader, "CreatedAt"),
                            ActionName = Convert.ToString(reader["ActionName"]),
                            OperatorName = Convert.ToString(reader["OperatorName"]),
                            ApproverName = Convert.ToString(reader["ApproverName"]),
                            Result = Convert.ToString(reader["Result"]),
                            Message = Convert.ToString(reader["Message"])
                        });
                    }
                }
            }

            return result;
        }

        private static void ValidateRequest(CriticalRecoveryRequestViewModel request, bool approval)
        {
            if (request == null)
            {
                throw new InvalidOperationException("Critical operation request is required.");
            }

            if (approval && !request.RequestId.HasValue)
            {
                throw new InvalidOperationException("Request id is required for approval.");
            }

            if (!approval && string.IsNullOrWhiteSpace(request.SecondaryPassword))
            {
                throw new InvalidOperationException("Current admin password is required.");
            }

            if (approval && string.IsNullOrWhiteSpace(request.SecondaryPassword) && string.IsNullOrWhiteSpace(request.ApproverSecondaryPassword))
            {
                throw new InvalidOperationException("Current admin password is required.");
            }

            if (request.DeleteOrphanKycRecords && !request.AllowPhysicalDelete)
            {
                throw new InvalidOperationException("Orphan KYC cleanup requires explicit physical delete mode and dual approval.");
            }
        }

        private static void AddFilterParameters(SqlCommand command, CriticalRecoveryFilterViewModel filter)
        {
            filter = filter ?? new CriticalRecoveryFilterViewModel();
            command.Parameters.AddWithValue("@BranchId", (object)filter.BranchId ?? DBNull.Value);
            command.Parameters.AddWithValue("@DateFrom", (object)filter.DateFrom ?? DBNull.Value);
            command.Parameters.AddWithValue("@DateTo", (object)filter.DateTo ?? DBNull.Value);
            command.Parameters.AddWithValue("@InvoiceType", (object)filter.InvoiceType ?? DBNull.Value);
            command.Parameters.AddWithValue("@InvoiceScope", filter.InvoiceScope ?? "SalesOnly");
            command.Parameters.AddWithValue("@InvoiceSource", filter.InvoiceSource ?? "Both");
            command.Parameters.AddWithValue("@OperationKind", filter.OperationKind ?? "All");
            command.Parameters.AddWithValue("@InvoiceNo", filter.InvoiceNo ?? string.Empty);
            command.Parameters.AddWithValue("@CashierUserId", filter.CashierUserId ?? string.Empty);
            command.Parameters.AddWithValue("@CustomerSearch", filter.CustomerSearch ?? string.Empty);
            command.Parameters.AddWithValue("@ClosingStatus", filter.ClosingStatus ?? string.Empty);
            command.Parameters.AddWithValue("@PostedStatus", filter.PostedStatus ?? string.Empty);
            command.Parameters.AddWithValue("@HasAccountingEntry", filter.HasAccountingEntry);
            command.Parameters.AddWithValue("@HasStockMovement", filter.HasStockMovement);
            command.Parameters.AddWithValue("@HasKycLink", filter.HasKycLink);
            command.Parameters.AddWithValue("@HasGeneratedVoucher", filter.HasGeneratedVoucher);
            command.Parameters.AddWithValue("@SelectedTransactionIds", filter.SelectedTransactionIds ?? string.Empty);
        }

        private static void AddRequestParameters(SqlCommand command, CriticalRecoveryRequestViewModel request, string userName, HttpRequestBase httpRequest)
        {
            command.Parameters.AddWithValue("@Mode", request.Mode ?? CriticalRecoveryMode.CancelOnly);
            command.Parameters.AddWithValue("@Reason", string.IsNullOrWhiteSpace(request.Reason) ? "Admin password confirmed critical recovery." : request.Reason);
            command.Parameters.AddWithValue("@RequestedBy", userName ?? string.Empty);
            command.Parameters.AddWithValue("@SecondaryPassword", request.SecondaryPassword ?? string.Empty);
            command.Parameters.AddWithValue("@DangerConfirmation", "I UNDERSTAND");
            command.Parameters.AddWithValue("@DryRun", request.DryRun);
            command.Parameters.AddWithValue("@AllowPhysicalDelete", request.AllowPhysicalDelete);
            command.Parameters.AddWithValue("@RequestHigherApprovalForClosedPeriod", request.RequestHigherApprovalForClosedPeriod);
            command.Parameters.AddWithValue("@DeleteOrphanKycRecords", request.DeleteOrphanKycRecords);
            AddRequestMetadata(command, httpRequest);
        }

        private static void AddRequestMetadata(SqlCommand command, HttpRequestBase httpRequest)
        {
            command.Parameters.AddWithValue("@MachineName", Environment.MachineName);
            command.Parameters.AddWithValue("@IpAddress", httpRequest == null ? string.Empty : httpRequest.UserHostAddress ?? string.Empty);
            command.Parameters.AddWithValue("@SessionId", HttpContext.Current == null || HttpContext.Current.Session == null ? string.Empty : HttpContext.Current.Session.SessionID);
        }

        private static int ReadInt(IDataRecord reader, string name)
        {
            return reader[name] == DBNull.Value ? 0 : Convert.ToInt32(reader[name]);
        }

        private static int? ReadNullableInt(IDataRecord reader, string name)
        {
            return reader[name] == DBNull.Value ? (int?)null : Convert.ToInt32(reader[name]);
        }

        private static long ReadLong(IDataRecord reader, string name)
        {
            return reader[name] == DBNull.Value ? 0 : Convert.ToInt64(reader[name]);
        }

        private static long? ReadNullableLong(IDataRecord reader, string name)
        {
            return reader[name] == DBNull.Value ? (long?)null : Convert.ToInt64(reader[name]);
        }

        private static decimal ReadDecimal(IDataRecord reader, string name)
        {
            return reader[name] == DBNull.Value ? 0m : Convert.ToDecimal(reader[name]);
        }

        private static DateTime ReadDate(IDataRecord reader, string name)
        {
            return reader[name] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(reader[name]);
        }
    }
}
