using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using MyERP.Areas.MainErp.Interfaces;
using MyERP.Areas.MainErp.ViewModels;
using MyERP.Areas.MainErp.ViewModels.SalesInvoices;

namespace MyERP.Areas.MainErp.Repositories.SalesInvoices
{
    public class SalesInvoiceReadRepository
    {
        private readonly IMainErpDbConnectionFactory _connectionFactory;

        public SalesInvoiceReadRepository(IMainErpDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public PagedReadResult<SalesInvoiceListItemViewModel> Search(MainErpSalesInvoiceKind kind, string searchText, DateTime? fromDate, DateTime? toDate, int? branchId, int page, int pageSize)
        {
            var result = new PagedReadResult<SalesInvoiceListItemViewModel>();
            page = page < 1 ? 1 : page;
            pageSize = pageSize < 1 ? 20 : pageSize;

            try
            {
                using (var connection = _connectionFactory.CreateOpenConnection())
                using (var command = new SqlCommand("dbo.MainErp_SalesInvoice_Search", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.Add("@TypeInvoice", SqlDbType.Int).Value = kind == MainErpSalesInvoiceKind.Pump ? 2 : 1;
                    command.Parameters.Add("@SearchText", SqlDbType.NVarChar, 200).Value = string.IsNullOrWhiteSpace(searchText) ? (object)DBNull.Value : searchText.Trim();
                    command.Parameters.Add("@FromDate", SqlDbType.DateTime).Value = (object)fromDate ?? DBNull.Value;
                    command.Parameters.Add("@ToDate", SqlDbType.DateTime).Value = (object)toDate ?? DBNull.Value;
                    command.Parameters.Add("@BranchId", SqlDbType.Int).Value = (object)branchId ?? DBNull.Value;
                    command.Parameters.Add("@Page", SqlDbType.Int).Value = page;
                    command.Parameters.Add("@PageSize", SqlDbType.Int).Value = pageSize;

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            if (result.TotalCount == 0 && reader["TotalCount"] != DBNull.Value)
                            {
                                result.TotalCount = Convert.ToInt32(reader["TotalCount"]);
                            }

                            result.Items.Add(ReadListItem(reader));
                        }
                    }
                }

                if (kind == MainErpSalesInvoiceKind.Pump)
                {
                    result.Diagnostics = LoadDiagnostics(kind, result.TotalCount);
                }

                return result;
            }
            catch (SqlException ex)
            {
                if (!IsMissingStoredProcedure(ex))
                {
                    Trace.TraceWarning("MainErp sales invoice stored procedure search warning: " + ex);
                    result.Warning = "تعذر تشغيل إجراء العرض المحسن للفواتير؛ سيتم استخدام القراءة المباشرة المتاحة.";
                }
            }

            try
            {
                using (var connection = _connectionFactory.CreateOpenConnection())
                using (var command = new SqlCommand(@"
WITH InvoiceRows AS (
    SELECT
        ROW_NUMBER() OVER (ORDER BY t.Transaction_ID DESC) AS RowNo,
        COUNT(1) OVER() AS TotalCount,
        t.Transaction_ID,
        t.Transaction_Serial,
        t.NoteSerial,
        t.NoteSerial1,
        t.ManualNO,
        t.Transaction_Date,
        t.Transaction_Type,
        ISNULL(t.TypeInvoice, 0) AS TypeInvoice,
        COALESCE(c.CusName, t.CashCustomerName) AS CustomerName,
        t.CashCustomerName,
        b.branch_name AS BranchName,
        CONVERT(nvarchar(50), t.StoreID) AS StoreName,
        t.Total,
        t.NetValue,
        t.VAT,
        t.PayedValue,
        t.RemainValue,
        t.NoteId,
        t.Closed
    FROM Transactions t
    LEFT JOIN TblCustemers c ON t.CusID = c.CusID
    LEFT JOIN TblBranchesData b ON t.BranchId = b.branch_id
    WHERE t.Transaction_Type IN (21, 42, 38, 9)
      AND ((@TypeInvoice = 2 AND ISNULL(t.TypeInvoice, 0) = 2) OR (@TypeInvoice = 1 AND ISNULL(t.TypeInvoice, 0) <> 2))
      AND (@SearchText IS NULL
           OR t.NoteSerial1 LIKE @SearchLike
           OR t.NoteSerial LIKE @SearchLike
           OR t.Transaction_Serial LIKE @SearchLike
           OR t.ManualNO LIKE @SearchLike
           OR t.CashCustomerName LIKE @SearchLike
           OR c.CusName LIKE @SearchLike)
      AND (@FromDate IS NULL OR t.Transaction_Date >= @FromDate)
      AND (@ToDate IS NULL OR t.Transaction_Date < DATEADD(day, 1, @ToDate))
      AND (@BranchId IS NULL OR t.BranchId = @BranchId)
)
SELECT * FROM InvoiceRows WHERE RowNo BETWEEN @StartRow AND @EndRow ORDER BY RowNo;", connection))
                {
                    command.Parameters.Add("@TypeInvoice", SqlDbType.Int).Value = kind == MainErpSalesInvoiceKind.Pump ? 2 : 1;
                    command.Parameters.Add("@SearchText", SqlDbType.NVarChar, 200).Value = string.IsNullOrWhiteSpace(searchText) ? (object)DBNull.Value : searchText.Trim();
                    command.Parameters.Add("@SearchLike", SqlDbType.NVarChar, 220).Value = string.IsNullOrWhiteSpace(searchText) ? (object)DBNull.Value : "%" + searchText.Trim() + "%";
                    command.Parameters.Add("@FromDate", SqlDbType.DateTime).Value = (object)fromDate ?? DBNull.Value;
                    command.Parameters.Add("@ToDate", SqlDbType.DateTime).Value = (object)toDate ?? DBNull.Value;
                    command.Parameters.Add("@BranchId", SqlDbType.Int).Value = (object)branchId ?? DBNull.Value;
                    command.Parameters.Add("@StartRow", SqlDbType.Int).Value = ((page - 1) * pageSize) + 1;
                    command.Parameters.Add("@EndRow", SqlDbType.Int).Value = page * pageSize;

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            if (result.TotalCount == 0 && reader["TotalCount"] != DBNull.Value)
                            {
                                result.TotalCount = Convert.ToInt32(reader["TotalCount"]);
                            }

                            result.Items.Add(ReadListItem(reader));
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                Trace.TraceWarning("MainErp sales invoice search schema warning: " + ex);
                result.Warning = "تعذر تحميل فواتير المبيعات من قاعدة البيانات الحالية. قد تكون قاعدة التشغيل لا تحتوي على نفس حقول فواتير الورشة/المضخات الخاصة بالنظام الرئيسي.";
            }

            if (kind == MainErpSalesInvoiceKind.Pump)
            {
                result.Diagnostics = LoadDiagnostics(kind, result.TotalCount);
            }

            return result;
        }

        public SalesInvoiceDetailsViewModel GetDetails(MainErpSalesInvoiceKind kind, int id)
        {
            var model = new SalesInvoiceDetailsViewModel
            {
                Kind = kind,
                ArabicTitle = kind == MainErpSalesInvoiceKind.Pump ? "فاتورة مبيعات المضخات" : "فاتورة مبيعات الورشة"
            };

            try
            {
                using (var connection = _connectionFactory.CreateOpenConnection())
                using (var command = new SqlCommand("dbo.MainErp_SalesInvoice_GetDetails", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.Add("@TypeInvoice", SqlDbType.Int).Value = kind == MainErpSalesInvoiceKind.Pump ? 2 : 1;
                    command.Parameters.Add("@TransactionId", SqlDbType.Int).Value = id;

                    using (var reader = command.ExecuteReader())
                    {
                        if (!reader.Read())
                        {
                            model.TransactionId = id;
                            model.Warning = "لم يتم العثور على الفاتورة المطلوبة في نطاق الشاشة الحالية.";
                            return model;
                        }

                        ApplyHeader(model, reader);

                        if (reader.NextResult())
                        {
                            while (reader.Read())
                            {
                                AddLine(model, reader);
                            }
                        }

                        if (reader.NextResult())
                        {
                            while (reader.Read())
                            {
                                AddPayment(model, reader);
                            }
                        }

                        if (reader.NextResult())
                        {
                            while (reader.Read())
                            {
                                AddVoucherLine(model, reader);
                            }
                        }

                        if (reader.NextResult())
                        {
                            while (reader.Read())
                            {
                                AddRelatedInventoryTransaction(model, reader);
                            }
                        }
                    }

                    LoadPumpDeferredTableAllocations(connection, model);
                    LoadPumpDeferredCustomerAccounts(connection, model);
                    LoadAuditLogs(connection, model, id);
                    BuildSavePreview(model);
                    return model;
                }
            }
            catch (SqlException ex)
            {
                if (!IsMissingStoredProcedure(ex))
                {
                    Trace.TraceWarning("MainErp sales invoice stored procedure details warning: " + ex);
                    model.Warning = "تعذر تشغيل إجراء تفاصيل الفاتورة المحسن؛ سيتم استخدام القراءة المباشرة المتاحة.";
                }
            }

            try
            {
                using (var connection = _connectionFactory.CreateOpenConnection())
                {
                    LoadHeader(connection, model, kind, id);
                    if (!string.IsNullOrWhiteSpace(model.Warning))
                    {
                        return model;
                    }

                    LoadLines(connection, model, id);
                    LoadPayments(connection, model, id);
                    LoadVoucherLines(connection, model, id, model.NoteId);
                    LoadRelatedInventoryTransactions(connection, model, id);
                    LoadAuditLogs(connection, model, id);
                    LoadPumpDeferredTableAllocations(connection, model);
                    LoadPumpDeferredCustomerAccounts(connection, model);
                    BuildSavePreview(model);
                }
            }
            catch (SqlException ex)
            {
                Trace.TraceWarning("MainErp sales invoice details schema warning: " + ex);
                model.Warning = "تعذر تحميل تفاصيل الفاتورة من قاعدة البيانات الحالية. قد تكون قاعدة التشغيل لا تحتوي على نفس حقول فواتير الورشة/المضخات الخاصة بالنظام الرئيسي.";
            }

            return model;
        }

        public PumpDeferredDistributionEditViewModel GetPumpDeferredDistribution(int transactionId, int lineId)
        {
            var invoice = GetDetails(MainErpSalesInvoiceKind.Pump, transactionId);
            var line = invoice.Lines.FirstOrDefault(x => x.Id == lineId);
            var model = new PumpDeferredDistributionEditViewModel
            {
                TransactionId = transactionId,
                LineId = lineId,
                NoteSerial = invoice.NoteSerial1,
                TransactionDate = invoice.TransactionDate,
                CustomerName = string.IsNullOrWhiteSpace(invoice.CustomerName) ? invoice.CashCustomerName : invoice.CustomerName,
                IsLocked = invoice.Closed == true || invoice.Posted == true || invoice.Approved == true
            };

            if (!string.IsNullOrWhiteSpace(invoice.Warning))
            {
                model.Warnings.Add(invoice.Warning);
            }

            if (line == null)
            {
                model.Warnings.Add("Pump line was not found in Transaction_Details for this invoice.");
                EnsureEditableRows(model);
                LoadPumpDeferredCustomerOptions(model);
                return model;
            }

            model.ItemName = line.ItemName;
            model.PumpName = line.PumpName;
            model.CurrentDeferred = line.Deferred;
            model.CurrentDeferredQty = line.DeferredQty;
            model.LineQuantityLimit = line.CurrentQty ?? line.Quantity ?? line.ShowQty;
            model.NonDeferredQuantity = (line.CashQty ?? 0m) + (line.MadaQty ?? 0m) + (line.VisaQty ?? 0m);

            foreach (var allocation in line.DeferredAllocations)
            {
                model.Allocations.Add(new SalesInvoicePumpDeferredAllocationViewModel
                {
                    LineId = lineId,
                    CustomerId = allocation.CustomerId,
                    UnitId = allocation.UnitId,
                    Amount = allocation.Amount,
                    CustomerName = allocation.CustomerName,
                    Quantity = allocation.Quantity,
                    UnitPrice = allocation.UnitPrice,
                    ReferenceNo = allocation.ReferenceNo,
                    CustomerAccountDisplay = allocation.CustomerAccountDisplay,
                    CustomerAccountCodeInternal = allocation.CustomerAccountCodeInternal
                });
            }

            EnsureEditableRows(model);
            LoadPumpDeferredCustomerOptions(model);
            return model;
        }

        public PumpDeferredDistributionSaveResultViewModel SavePumpDeferredDistribution(PumpDeferredDistributionEditViewModel model, bool dryRun)
        {
            var normalizedAllocations = NormalizePumpDeferredAllocations(model);
            if (normalizedAllocations.Count == 0)
            {
                return new PumpDeferredDistributionSaveResultViewModel
                {
                    Success = false,
                    DryRun = dryRun,
                    Message = "At least one deferred customer allocation is required."
                };
            }

            HydratePumpDeferredAllocationCustomers(normalizedAllocations);
            var detailsPump = BuildDetailsPumpText(normalizedAllocations);
            var deferred = normalizedAllocations.Sum(x => x.Amount ?? 0m);
            var deferredQty = normalizedAllocations.Sum(x => x.Quantity ?? 0m);
            var invoice = GetDetails(MainErpSalesInvoiceKind.Pump, model.TransactionId);
            var line = invoice.Lines.FirstOrDefault(x => x.Id == model.LineId);
            if (line == null)
            {
                throw new InvalidOperationException("Pump line was not found in the current database.");
            }

            var lineQuantityLimit = line.CurrentQty ?? line.Quantity ?? line.ShowQty;
            var nonDeferredQuantity = (line.CashQty ?? 0m) + (line.MadaQty ?? 0m) + (line.VisaQty ?? 0m);
            if (lineQuantityLimit.HasValue && deferredQty + nonDeferredQuantity > lineQuantityLimit.Value)
            {
                throw new InvalidOperationException("إجمالي كمية الآجل يتجاوز الكمية المتاحة لهذا السطر.");
            }

            using (var connection = _connectionFactory.CreateOpenConnection())
            using (var command = new SqlCommand("dbo.MainErp_SalesInvoice_SavePumpDeferredDistribution", connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add("@TransactionId", SqlDbType.Int).Value = model.TransactionId;
                command.Parameters.Add("@LineId", SqlDbType.Int).Value = model.LineId;
                command.Parameters.Add("@DetailsPump", SqlDbType.NVarChar, -1).Value = detailsPump;
                command.Parameters.Add("@Deferred", SqlDbType.Decimal).Value = deferred;
                command.Parameters["@Deferred"].Precision = 18;
                command.Parameters["@Deferred"].Scale = 4;
                command.Parameters.Add("@DeferredQty", SqlDbType.Decimal).Value = deferredQty;
                command.Parameters["@DeferredQty"].Precision = 18;
                command.Parameters["@DeferredQty"].Scale = 4;
                command.Parameters.Add("@DryRun", SqlDbType.Bit).Value = dryRun;
                command.Parameters.Add("@EnableDraftWrite", SqlDbType.Bit).Value = !dryRun;

                using (var reader = command.ExecuteReader())
                {
                    var result = new PumpDeferredDistributionSaveResultViewModel
                    {
                        Success = true,
                        DryRun = dryRun,
                        Message = dryRun
                            ? "Preview completed. No database write was executed."
                            : "Pump deferred customer distribution was saved. No Notes, vouchers, inventory, or posting rows were created.",
                        DetailsPump = detailsPump,
                        Deferred = deferred,
                        DeferredQty = deferredQty
                    };

                    if (reader.Read())
                    {
                        result.Message = ReadString(reader, "SafetyMessage");
                    }

                    return result;
                }
            }
        }

        public PumpSalesEditViewModel GetPumpEdit(int? transactionId)
        {
            var model = new PumpSalesEditViewModel
            {
                TransactionId = transactionId,
                TransactionDate = DateTime.Today
            };

            if (transactionId.HasValue && transactionId.Value > 0)
            {
                var details = GetDetails(MainErpSalesInvoiceKind.Pump, transactionId.Value);
                model.TransactionId = details.TransactionId;
                model.NoteSerial1 = details.NoteSerial1;
                model.TransactionDate = details.TransactionDate ?? DateTime.Today;
                model.BranchId = details.BranchId;
                model.StoreId = details.StoreId;
                model.BoxId = details.BoxId;
                model.CustomerId = details.CustomerId;
                model.CashCustomerName = details.CashCustomerName;
                model.ManualNo = details.ManualNo;
                model.Remarks = details.Remarks;
                model.IsLocked = details.Closed == true || details.Posted == true || details.Approved == true || details.IsPosted == true;

                foreach (var line in details.Lines)
                {
                    model.Lines.Add(new PumpSalesEditLineViewModel
                    {
                        Id = line.Id,
                        LineNumber = line.LineNumber,
                        ItemId = line.ItemId,
                        UnitId = line.UnitId,
                        StoreId2 = line.StoreId2,
                        PumpId = line.PumpId,
                        PrevQty = line.PrevQty,
                        CurrentQty = line.CurrentQty,
                        ShowQty = line.ShowQty ?? line.Quantity,
                        Quantity = line.Quantity ?? line.ShowQty,
                        Price = line.ShowPrice ?? line.Price,
                        CostPrice = line.CostPrice,
                        Cash = line.Cash,
                        Mada = line.Mada,
                        Visa = line.Visa,
                        Deferred = line.Deferred,
                        CashQty = line.CashQty,
                        MadaQty = line.MadaQty,
                        VisaQty = line.VisaQty,
                        DeferredQty = line.DeferredQty,
                        AmountH = line.AmountH,
                        AmountHCommission = line.AmountHCommission,
                        AccountCodeInternal = line.AccountCodeInternal,
                        CommissionAccountCodeInternal = line.CommissionAccountCodeInternal,
                        IsOther = line.IsOther,
                        DetailsPump = line.PumpDetails
                    });
                }

                foreach (var payment in details.Payments)
                {
                    model.Payments.Add(new PumpSalesEditPaymentViewModel
                    {
                        Id = payment.Id,
                        PaymentId = payment.PaymentId,
                        Value = payment.Value,
                        CardNo = payment.CardNo,
                        MaxValue = payment.MaxValue
                    });
                }
            }

            EnsurePumpEditRows(model);
            LoadPumpEditLookups(model);
            return model;
        }

        public SalesInvoiceSaveResultViewModel SavePumpInvoiceDraft(PumpSalesEditViewModel model, bool dryRun)
        {
            if (model == null)
            {
                throw new ArgumentNullException("model");
            }

            var activeLines = (model.Lines ?? new List<PumpSalesEditLineViewModel>())
                .Where(x => x != null && x.ItemId.HasValue && x.ItemId.Value > 0)
                .ToList();

            if (activeLines.Count == 0)
            {
                return new SalesInvoiceSaveResultViewModel { Success = false, DryRun = dryRun, Message = "يجب إدخال بند مضخة واحد على الأقل." };
            }

            var badLine = activeLines.FirstOrDefault(x => Math.Abs(x.StillPumpQty) > 0.0001m);
            if (badLine != null)
            {
                return new SalesInvoiceSaveResultViewModel
                {
                    Success = false,
                    DryRun = dryRun,
                    Message = "لا يمكن الحفظ: توجد كمية غير موزعة في أحد سطور المضخات."
                };
            }

            HydratePumpEditLines(activeLines);
            var linesXml = BuildPumpLinesXml(activeLines);
            var paymentsXml = BuildPumpPaymentsXml(model.Payments ?? new List<PumpSalesEditPaymentViewModel>());
            var transactionId = model.TransactionId.GetValueOrDefault();

            using (var connection = _connectionFactory.CreateOpenConnection())
            using (var command = new SqlCommand("dbo.MainErp_PumpSales_SaveDraftFull", connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                var idParameter = command.Parameters.Add("@TransactionId", SqlDbType.Int);
                idParameter.Direction = ParameterDirection.InputOutput;
                idParameter.Value = transactionId > 0 ? (object)transactionId : DBNull.Value;
                command.Parameters.Add("@TransactionDate", SqlDbType.DateTime).Value = model.TransactionDate;
                command.Parameters.Add("@BranchId", SqlDbType.Int).Value = (object)model.BranchId ?? DBNull.Value;
                command.Parameters.Add("@StoreId", SqlDbType.Int).Value = (object)model.StoreId ?? DBNull.Value;
                command.Parameters.Add("@BoxId", SqlDbType.Int).Value = (object)model.BoxId ?? DBNull.Value;
                command.Parameters.Add("@CusId", SqlDbType.Int).Value = (object)model.CustomerId ?? DBNull.Value;
                command.Parameters.Add("@CashCustomerName", SqlDbType.NVarChar, 250).Value = string.IsNullOrWhiteSpace(model.CashCustomerName) ? (object)DBNull.Value : model.CashCustomerName.Trim();
                command.Parameters.Add("@ManualNo", SqlDbType.NVarChar, 100).Value = string.IsNullOrWhiteSpace(model.ManualNo) ? (object)DBNull.Value : model.ManualNo.Trim();
                command.Parameters.Add("@Remarks", SqlDbType.NVarChar, -1).Value = string.IsNullOrWhiteSpace(model.Remarks) ? (object)DBNull.Value : model.Remarks.Trim();
                command.Parameters.Add("@UserId", SqlDbType.Int).Value = DBNull.Value;
                command.Parameters.Add("@LinesXml", SqlDbType.Xml).Value = linesXml.ToString(SaveOptions.DisableFormatting);
                command.Parameters.Add("@PaymentsXml", SqlDbType.Xml).Value = paymentsXml.ToString(SaveOptions.DisableFormatting);
                command.Parameters.Add("@DryRun", SqlDbType.Bit).Value = dryRun;
                command.Parameters.Add("@EnableDraftWrite", SqlDbType.Bit).Value = !dryRun;

                using (var reader = command.ExecuteReader())
                {
                    var result = new SalesInvoiceSaveResultViewModel
                    {
                        Success = true,
                        DryRun = dryRun,
                        TransactionId = transactionId > 0 ? (int?)transactionId : null,
                        Message = dryRun ? "تمت معاينة حفظ فاتورة المضخات بدون كتابة." : "تم حفظ فاتورة المضخات كمسودة كاملة بدون ترحيل قيود أو مخزون."
                    };

                    if (reader.Read())
                    {
                        result.TransactionId = ReadIntIfExists(reader, "TransactionId") ?? result.TransactionId;
                        var safetyMessage = HasColumn(reader, "SafetyMessage") ? ReadString(reader, "SafetyMessage") : string.Empty;
                        var resultMessage = HasColumn(reader, "ResultMessage") ? ReadString(reader, "ResultMessage") : string.Empty;
                        if (!string.IsNullOrWhiteSpace(resultMessage) || !string.IsNullOrWhiteSpace(safetyMessage))
                        {
                            result.Message = string.IsNullOrWhiteSpace(resultMessage) ? safetyMessage : resultMessage;
                        }
                    }

                    if (!dryRun && idParameter.Value != DBNull.Value)
                    {
                        result.TransactionId = Convert.ToInt32(idParameter.Value);
                    }

                    return result;
                }
            }
        }

        public SalesInvoiceSaveResultViewModel PostPumpInvoice(int transactionId, int? userId, bool forceRebuild, bool dryRun, bool includeInventoryCost)
        {
            using (var connection = _connectionFactory.CreateOpenConnection())
            using (var command = new SqlCommand("dbo.MainErp_PumpSales_Post", connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add("@TransactionId", SqlDbType.Int).Value = transactionId;
                command.Parameters.Add("@UserId", SqlDbType.Int).Value = (object)userId ?? DBNull.Value;
                command.Parameters.Add("@ForceRebuild", SqlDbType.Bit).Value = forceRebuild;
                command.Parameters.Add("@DryRun", SqlDbType.Bit).Value = dryRun;
                command.Parameters.Add("@IncludeInventoryCost", SqlDbType.Bit).Value = includeInventoryCost;

                using (var reader = command.ExecuteReader())
                {
                    var result = new SalesInvoiceSaveResultViewModel
                    {
                        Success = true,
                        DryRun = dryRun,
                        TransactionId = transactionId,
                        Message = dryRun ? "تمت معاينة ترحيل فاتورة المضخات بدون كتابة." : "تم ترحيل فاتورة المضخات."
                    };

                    if (reader.Read())
                    {
                        var resultMessage = HasColumn(reader, "ResultMessage") ? ReadString(reader, "ResultMessage") : string.Empty;
                        if (!string.IsNullOrWhiteSpace(resultMessage))
                        {
                            result.Message = resultMessage;
                        }
                    }

                    return result;
                }
            }
        }

        public SalesInvoiceSaveResultViewModel DeletePumpInvoiceDraft(int transactionId, int? userId, bool dryRun)
        {
            using (var connection = _connectionFactory.CreateOpenConnection())
            using (var command = new SqlCommand("dbo.MainErp_PumpSales_DeleteDraft", connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add("@TransactionId", SqlDbType.Int).Value = transactionId;
                command.Parameters.Add("@UserId", SqlDbType.Int).Value = (object)userId ?? DBNull.Value;
                command.Parameters.Add("@DryRun", SqlDbType.Bit).Value = dryRun;

                using (var reader = command.ExecuteReader())
                {
                    var result = new SalesInvoiceSaveResultViewModel
                    {
                        Success = true,
                        DryRun = dryRun,
                        TransactionId = transactionId,
                        Message = dryRun ? "تمت معاينة حذف مسودة فاتورة المضخات بدون كتابة." : "تم حذف مسودة فاتورة المضخات."
                    };

                    if (reader.Read())
                    {
                        var resultMessage = HasColumn(reader, "ResultMessage") ? ReadString(reader, "ResultMessage") : string.Empty;
                        if (!string.IsNullOrWhiteSpace(resultMessage))
                        {
                            result.Message = resultMessage;
                        }
                    }

                    return result;
                }
            }
        }

        public SalesInvoiceSaveResultViewModel PreviewPumpInvoiceCancellation(int transactionId, int? userId)
        {
            return CancelPumpInvoice(transactionId, userId, true);
        }

        public SalesInvoiceSaveResultViewModel CancelPumpInvoice(int transactionId, int? userId, bool dryRun)
        {
            using (var connection = _connectionFactory.CreateOpenConnection())
            using (var command = new SqlCommand("dbo.MainErp_PumpSales_CancelPosted", connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add("@TransactionId", SqlDbType.Int).Value = transactionId;
                command.Parameters.Add("@UserId", SqlDbType.Int).Value = (object)userId ?? DBNull.Value;
                command.Parameters.Add("@DryRun", SqlDbType.Bit).Value = dryRun;

                using (var reader = command.ExecuteReader())
                {
                    var result = new SalesInvoiceSaveResultViewModel
                    {
                        Success = true,
                        DryRun = dryRun,
                        TransactionId = transactionId,
                        Message = "تمت معاينة إلغاء/عكس فاتورة المضخات بدون كتابة."
                    };

                    if (reader.Read())
                    {
                        var resultMessage = HasColumn(reader, "ResultMessage") ? ReadString(reader, "ResultMessage") : string.Empty;
                        if (!string.IsNullOrWhiteSpace(resultMessage))
                        {
                            result.Message = resultMessage;
                        }
                    }

                    return result;
                }
            }
        }

        public SalesInvoiceDiagnosticsViewModel LoadDiagnostics(MainErpSalesInvoiceKind kind, int rowCountFound)
        {
            var diagnostics = new SalesInvoiceDiagnosticsViewModel
            {
                FilterDescription = kind == MainErpSalesInvoiceKind.Pump
                    ? "Transactions.Transaction_Type IN (21,42,38,9) AND ISNULL(TypeInvoice,0) = 2"
                    : "Transactions.Transaction_Type IN (21,42,38,9) AND ISNULL(TypeInvoice,0) <> 2",
                RowCountFound = rowCountFound
            };

            try
            {
                using (var connection = _connectionFactory.CreateOpenConnection())
                {
                    diagnostics.DatabaseName = connection.Database;

                    using (var command = new SqlCommand(@"
SELECT ISNULL(TypeInvoice, 0) AS TypeInvoice, Transaction_Type, COUNT(1) AS RowsFound, MAX(Transaction_ID) AS MaxTransactionId
FROM Transactions
WHERE Transaction_Type IN (21, 42, 38, 9)
GROUP BY ISNULL(TypeInvoice, 0), Transaction_Type
ORDER BY ISNULL(TypeInvoice, 0), Transaction_Type;", connection))
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            diagnostics.TypeBreakdown.Add(new SalesInvoiceDiagnosticRowViewModel
                            {
                                TypeInvoice = ReadInt(reader, "TypeInvoice"),
                                TransactionType = ReadInt(reader, "Transaction_Type"),
                                Count = ReadInt(reader, "RowsFound").GetValueOrDefault(),
                                MaxTransactionId = ReadInt(reader, "MaxTransactionId")
                            });
                        }
                    }

                    using (var command = new SqlCommand(@"
SELECT TOP 10
    t.Transaction_ID,
    t.Transaction_Type,
    ISNULL(t.TypeInvoice, 0) AS TypeInvoice,
    t.NoteSerial1,
    t.Transaction_Date,
    COALESCE(c.CusName, t.CashCustomerName) AS CustomerName,
    COUNT(d.ID) AS PumpLineCount
FROM Transactions t
INNER JOIN Transaction_Details d ON t.Transaction_ID = d.Transaction_ID
LEFT JOIN TblCustemers c ON t.CusID = c.CusID
WHERE d.PumpId IS NOT NULL
   OR NULLIF(CONVERT(nvarchar(max), d.DetailsPump), '') IS NOT NULL
GROUP BY t.Transaction_ID, t.Transaction_Type, ISNULL(t.TypeInvoice, 0), t.NoteSerial1, t.Transaction_Date, COALESCE(c.CusName, t.CashCustomerName)
ORDER BY t.Transaction_ID DESC;", connection))
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            diagnostics.PumpCandidates.Add(new SalesInvoiceDiagnosticCandidateViewModel
                            {
                                TransactionId = ReadInt(reader, "Transaction_ID").GetValueOrDefault(),
                                TransactionType = ReadInt(reader, "Transaction_Type"),
                                TypeInvoice = ReadInt(reader, "TypeInvoice"),
                                NoteSerial = ReadString(reader, "NoteSerial1"),
                                TransactionDate = ReadDate(reader, "Transaction_Date"),
                                CustomerName = ReadString(reader, "CustomerName"),
                                PumpLineCount = ReadInt(reader, "PumpLineCount").GetValueOrDefault()
                            });
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                Trace.TraceWarning("MainErp pump sales diagnostics schema warning: " + ex);
                diagnostics.FilterDescription += " | تحذير تشخيصي: تعذر قراءة بعض مؤشرات المضخات بسبب اختلاف بنية قاعدة البيانات الحالية.";
            }

            return diagnostics;
        }

        private static SalesInvoiceListItemViewModel ReadListItem(IDataRecord reader)
        {
            return new SalesInvoiceListItemViewModel
            {
                TransactionId = ReadInt(reader, "Transaction_ID").GetValueOrDefault(),
                TransactionSerial = ReadString(reader, "Transaction_Serial"),
                NoteSerial = ReadString(reader, "NoteSerial"),
                NoteSerial1 = ReadString(reader, "NoteSerial1"),
                ManualNo = ReadString(reader, "ManualNO"),
                TransactionDate = ReadDate(reader, "Transaction_Date"),
                TransactionType = ReadInt(reader, "Transaction_Type"),
                TypeInvoice = ReadInt(reader, "TypeInvoice"),
                CustomerId = ReadIntIfExists(reader, "CusID"),
                BranchId = ReadIntIfExists(reader, "BranchId"),
                CustomerName = ReadString(reader, "CustomerName"),
                CashCustomerName = ReadString(reader, "CashCustomerName"),
                BranchName = ReadString(reader, "BranchName"),
                StoreName = ReadString(reader, "StoreName"),
                Total = ReadDecimal(reader, "Total"),
                NetValue = ReadDecimal(reader, "NetValue"),
                Vat = ReadDecimal(reader, "VAT"),
                PayedValue = ReadDecimal(reader, "PayedValue"),
                RemainValue = ReadDecimal(reader, "RemainValue"),
                NoteId = ReadInt(reader, "NoteId"),
                Closed = ReadBool(reader, "Closed")
            };
        }

        private static void ApplyHeader(SalesInvoiceDetailsViewModel model, IDataRecord reader)
        {
            var list = ReadListItem(reader);
            model.TransactionId = list.TransactionId;
            model.TransactionSerial = list.TransactionSerial;
            model.NoteSerial = list.NoteSerial;
            model.NoteSerial1 = list.NoteSerial1;
            model.ManualNo = list.ManualNo;
            model.TransactionDate = list.TransactionDate;
            model.TransactionType = list.TransactionType;
            model.TypeInvoice = list.TypeInvoice;
            model.CustomerId = list.CustomerId;
            model.BranchId = list.BranchId;
            model.CustomerName = list.CustomerName;
            model.CashCustomerName = list.CashCustomerName;
            model.BranchName = list.BranchName;
            model.StoreName = list.StoreName;
            model.Total = list.Total;
            model.NetValue = list.NetValue;
            model.Vat = list.Vat;
            model.PayedValue = list.PayedValue;
            model.RemainValue = list.RemainValue;
            model.NoteId = list.NoteId;
            model.Closed = list.Closed;
            model.Posted = ReadBoolFlexible(reader, "Posted");
            model.Approved = ReadBoolFlexible(reader, "Approved");
            model.IsPosted = ReadBoolFlexible(reader, "IsPosted");
            model.UserPosted = ReadIntIfExists(reader, "UserPosted");
            model.Prefix = ReadStringIfExists(reader, "Prefix");
            model.FullCode = ReadStringIfExists(reader, "Fullcode");
            model.CboBasedOn = ReadIntIfExists(reader, "CBoBasedON");
            model.PosBillType = ReadIntIfExists(reader, "POSBillType");
            model.TransactionNetValue = ReadDecimalIfExists(reader, "Transaction_NetValue");
            model.SumValueLine = ReadDecimalIfExists(reader, "SumValueLine");
            model.SumVatLine = ReadDecimalIfExists(reader, "SumVATLine");
            model.DateRec = ReadDateIfExists(reader, "DateRec");
            model.Remarks = ReadStringIfExists(reader, "Remarks");
            model.PaymentTypeLabel = ReadStringIfExists(reader, "PaymentType");
            model.CurrencyId = ReadStringIfExists(reader, "Currency_id");
            model.CurrencyRate = ReadDecimalIfExists(reader, "Currency_rate");
            model.OrderNo = ReadStringIfExists(reader, "order_no");
            model.StoreId = ReadIntIfExists(reader, "StoreID");
            model.BoxId = ReadIntIfExists(reader, "BoxID");
            model.PaymentNetId = ReadIntIfExists(reader, "PaymentNetid");
        }

        private static void AddLine(SalesInvoiceDetailsViewModel model, IDataRecord reader)
        {
            var line = new SalesInvoiceLineViewModel
            {
                Id = ReadInt(reader, "ID").GetValueOrDefault(),
                LineNumber = ReadIntIfExists(reader, "DetailLineNo"),
                ItemId = ReadInt(reader, "Item_ID"),
                ItemCode = ReadString(reader, "ItemCode"),
                ItemName = ReadString(reader, "ItemName"),
                UnitName = ReadString(reader, "UnitName"),
                UnitId = ReadIntIfExists(reader, "UnitId"),
                StoreId2 = ReadIntIfExists(reader, "StoreID2"),
                ShowQty = ReadDecimal(reader, "ShowQty"),
                Quantity = ReadDecimal(reader, "Quantity"),
                ShowPrice = ReadDecimal(reader, "ShowPrice"),
                Price = ReadDecimal(reader, "Price"),
                CostPrice = ReadDecimal(reader, "CostPrice"),
                DiscountValue = ReadDecimal(reader, "discountvalue"),
                TotalDiscountPerLine = ReadDecimal(reader, "TotalDiscountPerLine"),
                Vat = ReadDecimal(reader, "Vat"),
                VatYou = ReadDecimal(reader, "Vatyo"),
                Remarks = ReadString(reader, "Remarks"),
                AccountCodeInternal = ReadString(reader, "AccountCode"),
                AccountDisplay = FormatAccount(reader, "Account_Serial", "AccountName", "AccountCode"),
                CommissionAccountCodeInternal = ReadString(reader, "Account_CodeComm"),
                CommissionAccountDisplay = FormatAccount(reader, "CommissionAccountSerial", "CommissionAccountName", "Account_CodeComm"),
                PumpId = ReadInt(reader, "PumpId"),
                PumpName = ReadString(reader, "PumpName"),
                IsOther = ReadBoolFlexible(reader, "IsOther"),
                ColorId = ReadInt(reader, "ColorID"),
                PrevQty = ReadDecimal(reader, "PrevQty"),
                CurrentQty = ReadDecimal(reader, "CurrentQty"),
                Cash = ReadDecimal(reader, "Cash"),
                Mada = ReadDecimal(reader, "Mada"),
                Visa = ReadDecimal(reader, "Visa"),
                Deferred = ReadDecimal(reader, "Deferred"),
                CashQty = ReadDecimal(reader, "CashQty"),
                MadaQty = ReadDecimal(reader, "MadaQty"),
                VisaQty = ReadDecimal(reader, "VisaQty"),
                DeferredQty = ReadDecimal(reader, "DeferredQty"),
                AmountH = ReadDecimal(reader, "AmountH"),
                AmountHCommission = ReadDecimal(reader, "AmountHComm"),
                PumpDetails = ReadString(reader, "DetailsPump")
            };

            AddPumpDeferredAllocations(line);
            model.Lines.Add(line);
        }

        private static void AddPayment(SalesInvoiceDetailsViewModel model, IDataRecord reader)
        {
            model.Payments.Add(new SalesInvoicePaymentViewModel
            {
                Id = ReadInt(reader, "ID").GetValueOrDefault(),
                PaymentId = ReadInt(reader, "PaymentID"),
                Value = ReadDecimal(reader, "Value"),
                CardNo = ReadString(reader, "CardNo"),
                MaxValue = ReadDecimal(reader, "MaxValue")
            });
        }

        private static void AddVoucherLine(SalesInvoiceDetailsViewModel model, IDataRecord reader)
        {
            model.VoucherLines.Add(new SalesInvoiceVoucherLineViewModel
            {
                VoucherId = ReadInt(reader, "Double_Entry_Vouchers_ID").GetValueOrDefault(),
                LineNo = ReadInt(reader, "DEV_ID_Line_No"),
                NoteId = ReadInt(reader, "Notes_ID"),
                NoteSerial = ReadString(reader, "NoteSerial"),
                RecordDate = ReadDate(reader, "RecordDate"),
                AccountCodeInternal = ReadString(reader, "Account_Code"),
                AccountDisplay = FormatAccount(reader, "Account_Serial", "AccountName", "Account_Code"),
                Debit = ReadDecimal(reader, "Debit").GetValueOrDefault(),
                Credit = ReadDecimal(reader, "Credit").GetValueOrDefault(),
                Description = ReadString(reader, "Description")
            });
        }

        private static void AddRelatedInventoryTransaction(SalesInvoiceDetailsViewModel model, IDataRecord reader)
        {
            model.RelatedInventoryTransactions.Add(new SalesInvoiceRelatedInventoryTransactionViewModel
            {
                TransactionId = ReadInt(reader, "Transaction_ID").GetValueOrDefault(),
                TransactionSerial = ReadString(reader, "Transaction_Serial"),
                TransactionType = ReadInt(reader, "Transaction_Type"),
                TransactionDate = ReadDate(reader, "Transaction_Date"),
                NoteSerial = ReadString(reader, "NoteSerial"),
                NoteSerial1 = ReadString(reader, "NoteSerial1"),
                StoreName = ReadString(reader, "StoreName"),
                Total = ReadDecimal(reader, "Total"),
                NetValue = ReadDecimal(reader, "NetValue"),
                NoteId = ReadInt(reader, "NoteId"),
                LinkReason = ReadString(reader, "LinkReason")
            });
        }

        private static void AddPumpDeferredAllocations(SalesInvoiceLineViewModel line)
        {
            if (line == null || string.IsNullOrWhiteSpace(line.PumpDetails))
            {
                return;
            }

            var rows = line.PumpDetails
                .Replace("\r", string.Empty)
                .Split(new[] { '@', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var rawRow in rows)
            {
                var row = rawRow.Trim();
                if (string.IsNullOrWhiteSpace(row))
                {
                    continue;
                }

                var parts = row.Split('#');
                if (parts.Length < 6)
                {
                    continue;
                }

                line.DeferredAllocations.Add(new SalesInvoicePumpDeferredAllocationViewModel
                {
                    LineId = line.Id,
                    CustomerId = ParseInt(parts, 0),
                    UnitId = ParseInt(parts, 1),
                    Amount = ParseDecimal(parts, 2),
                    CustomerName = GetPart(parts, 3),
                    Quantity = ParseDecimal(parts, 4),
                    UnitPrice = ParseDecimal(parts, 5),
                    ReferenceNo = ParseInt(parts, 6)
                });
            }
        }

        private static void LoadPumpDeferredTableAllocations(SqlConnection connection, SalesInvoiceDetailsViewModel model)
        {
            if (model.Kind != MainErpSalesInvoiceKind.Pump || model.Lines.Count == 0 || !TableExists(connection, "Transaction_DetailsPump"))
            {
                return;
            }

            var byLineNumber = model.Lines
                .Where(x => x.LineNumber.HasValue)
                .ToDictionary(x => x.LineNumber.Value, x => x);
            var byDetailId = model.Lines.ToDictionary(x => x.Id, x => x);
            var touched = new HashSet<int>();

            using (var command = new SqlCommand(@"
SELECT
    p.ID,
    p.LineID,
    p.CusID,
    p.ItemId,
    p.Amount,
    p.Transaction_ID,
    p.RecNo,
    p.Qty,
    p.Price,
    c.CusName,
    c.Account_Code,
    a.Account_Serial,
    COALESCE(a.Account_Name, a.Account_NameEng) AS AccountName
FROM Transaction_DetailsPump p
LEFT JOIN TblCustemers c ON p.CusID = c.CusID
LEFT JOIN ACCOUNTS a ON c.Account_Code = a.Account_Code
WHERE p.Transaction_ID = @TransactionId
ORDER BY p.LineID, p.ID;", connection))
            {
                command.Parameters.Add("@TransactionId", SqlDbType.Int).Value = model.TransactionId;
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var lineId = ReadInt(reader, "LineID");
                        if (!lineId.HasValue)
                        {
                            continue;
                        }

                        SalesInvoiceLineViewModel line;
                        if (!byLineNumber.TryGetValue(lineId.Value, out line) && !byDetailId.TryGetValue(lineId.Value, out line))
                        {
                            continue;
                        }

                        if (!touched.Contains(line.Id))
                        {
                            line.DeferredAllocations.Clear();
                            touched.Add(line.Id);
                        }

                        line.DeferredAllocations.Add(new SalesInvoicePumpDeferredAllocationViewModel
                        {
                            LineId = line.Id,
                            CustomerId = ReadInt(reader, "CusID"),
                            UnitId = 1,
                            Amount = ReadDecimal(reader, "Amount"),
                            CustomerName = ReadString(reader, "CusName"),
                            Quantity = ReadDecimal(reader, "Qty"),
                            UnitPrice = ReadDecimal(reader, "Price"),
                            ReferenceNo = ReadInt(reader, "RecNo"),
                            CustomerAccountCodeInternal = ReadString(reader, "Account_Code"),
                            CustomerAccountDisplay = FormatAccount(reader, "Account_Serial", "AccountName", "Account_Code")
                        });
                    }
                }
            }
        }

        private static bool TableExists(SqlConnection connection, string tableName)
        {
            using (var command = new SqlCommand("SELECT COUNT(1) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = @TableName;", connection))
            {
                command.Parameters.Add("@TableName", SqlDbType.NVarChar, 128).Value = tableName;
                return Convert.ToInt32(command.ExecuteScalar()) > 0;
            }
        }

        private static bool ColumnExists(SqlConnection connection, string tableName, string columnName)
        {
            using (var command = new SqlCommand("SELECT COUNT(1) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @TableName AND COLUMN_NAME = @ColumnName;", connection))
            {
                command.Parameters.Add("@TableName", SqlDbType.NVarChar, 128).Value = tableName;
                command.Parameters.Add("@ColumnName", SqlDbType.NVarChar, 128).Value = columnName;
                return Convert.ToInt32(command.ExecuteScalar()) > 0;
            }
        }

        private static void EnsureEditableRows(PumpDeferredDistributionEditViewModel model)
        {
            while (model.Allocations.Count < 8)
            {
                model.Allocations.Add(new SalesInvoicePumpDeferredAllocationViewModel
                {
                    LineId = model.LineId,
                    UnitId = 1
                });
            }
        }

        private static void EnsurePumpEditRows(PumpSalesEditViewModel model)
        {
            while (model.Lines.Count < 12)
            {
                model.Lines.Add(new PumpSalesEditLineViewModel
                {
                    LineNumber = model.Lines.Count + 1,
                    UnitId = 1,
                    StoreId2 = model.StoreId
                });
            }

            while (model.Payments.Count < 6)
            {
                model.Payments.Add(new PumpSalesEditPaymentViewModel());
            }
        }

        private void LoadPumpEditLookups(PumpSalesEditViewModel model)
        {
            try
            {
                using (var connection = _connectionFactory.CreateOpenConnection())
                {
                    LoadLookup(connection, model.CustomerOptions, @"
SELECT TOP 500 c.CusID AS Id, CONVERT(nvarchar(50), c.CusID) + N' - ' + c.CusName AS Display, a.Account_Serial + N' - ' + COALESCE(a.Account_Name, a.Account_NameEng) AS AccountDisplay
FROM TblCustemers c
LEFT JOIN ACCOUNTS a ON c.Account_Code = a.Account_Code
ORDER BY c.CusName, c.CusID;");

                    LoadLookup(connection, model.BranchOptions, "SELECT branch_id AS Id, branch_name AS Display, NULL AS AccountDisplay FROM TblBranchesData ORDER BY branch_id;");
                    LoadLookup(connection, model.BoxOptions, "SELECT BoxID AS Id, BoxName AS Display, NULL AS AccountDisplay FROM TblBoxesData ORDER BY BoxID;");
                    LoadLookup(connection, model.ItemOptions, "SELECT TOP 1000 ItemID AS Id, COALESCE(ItemCode + N' - ', N'') + COALESCE(ItemName, ItemNamee) AS Display, NULL AS AccountDisplay FROM TblItems ORDER BY ItemName, ItemID;");
                    LoadLookup(connection, model.UnitOptions, "SELECT UnitID AS Id, UnitName AS Display, NULL AS AccountDisplay FROM TblUnites ORDER BY UnitName, UnitID;");
                    LoadLookup(connection, model.PumpOptions, "SELECT ID AS Id, CONVERT(nvarchar(50), ID) + N' - ' + COALESCE(Name, NameE) AS Display, NULL AS AccountDisplay FROM tblPumpType ORDER BY ID;");
                    LoadLookup(connection, model.PaymentOptions, "SELECT PaymentID AS Id, PaymentName AS Display, NULL AS AccountDisplay FROM TblPaymentType ORDER BY PaymentID;");
                    LoadStoreLookup(connection, model.StoreOptions);
                }
            }
            catch (SqlException ex)
            {
                Trace.TraceWarning("MainErp pump sales lookup schema warning: " + ex);
                model.Warnings.Add("تعذر تحميل بعض قوائم الاختيار بسبب اختلاف بنية قاعدة البيانات الحالية.");
            }
        }

        private static void LoadStoreLookup(SqlConnection connection, IList<SalesLookupOptionViewModel> options)
        {
            var candidates = new[] { "TblStoresData", "StoresData", "TblStoreData", "TblStores" };
            foreach (var table in candidates)
            {
                if (!TableExists(connection, table))
                {
                    continue;
                }

                LoadLookup(connection, options, "SELECT StoreID AS Id, CONVERT(nvarchar(50), StoreID) + N' - ' + StoreName AS Display, NULL AS AccountDisplay FROM " + table + " ORDER BY StoreID;");
                return;
            }

            LoadLookup(connection, options, "SELECT DISTINCT StoreID AS Id, CONVERT(nvarchar(50), StoreID) AS Display, NULL AS AccountDisplay FROM Transactions WHERE StoreID IS NOT NULL ORDER BY StoreID;");
        }

        private static void LoadLookup(SqlConnection connection, IList<SalesLookupOptionViewModel> options, string sql)
        {
            using (var command = new SqlCommand(sql, connection))
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    var id = ReadInt(reader, "Id");
                    if (!id.HasValue)
                    {
                        continue;
                    }

                    options.Add(new SalesLookupOptionViewModel
                    {
                        Id = id.Value,
                        Display = ReadString(reader, "Display"),
                        AccountDisplay = HasColumn(reader, "AccountDisplay") ? ReadString(reader, "AccountDisplay") : string.Empty
                    });
                }
            }
        }

        private void HydratePumpEditLines(IEnumerable<PumpSalesEditLineViewModel> lines)
        {
            foreach (var line in lines)
            {
                line.LineNumber = line.LineNumber ?? 0;
                line.ShowQty = line.ShowQty ?? line.Quantity ?? 1m;
                line.Quantity = line.Quantity ?? line.ShowQty ?? 1m;
                line.Price = line.Price ?? 0m;
                line.CostPrice = line.CostPrice ?? 0m;
                line.Cash = line.Cash ?? 0m;
                line.Mada = line.Mada ?? 0m;
                line.Visa = line.Visa ?? 0m;
                line.Deferred = line.Deferred ?? 0m;
                line.CashQty = line.CashQty ?? 0m;
                line.MadaQty = line.MadaQty ?? 0m;
                line.VisaQty = line.VisaQty ?? 0m;
                line.DeferredQty = line.DeferredQty ?? 0m;
                line.AmountH = line.AmountH ?? 0m;
                line.AmountHCommission = line.AmountHCommission ?? 0m;
            }
        }

        private static XDocument BuildPumpLinesXml(IEnumerable<PumpSalesEditLineViewModel> lines)
        {
            return new XDocument(new XElement("Lines", lines.Select(x => new XElement("Line",
                new XAttribute("Id", x.Id ?? 0),
                new XAttribute("LineNumber", x.LineNumber ?? 0),
                new XAttribute("ItemId", x.ItemId ?? 0),
                new XAttribute("UnitId", x.UnitId ?? 0),
                new XAttribute("StoreId2", x.StoreId2 ?? 0),
                new XAttribute("PumpId", x.PumpId ?? 0),
                new XAttribute("PrevQty", x.PrevQty ?? 0m),
                new XAttribute("CurrentQty", x.CurrentQty ?? 0m),
                new XAttribute("ShowQty", x.ShowQty ?? 0m),
                new XAttribute("Quantity", x.Quantity ?? 0m),
                new XAttribute("Price", x.Price ?? 0m),
                new XAttribute("CostPrice", x.CostPrice ?? 0m),
                new XAttribute("Cash", x.Cash ?? 0m),
                new XAttribute("Mada", x.Mada ?? 0m),
                new XAttribute("Visa", x.Visa ?? 0m),
                new XAttribute("Deferred", x.Deferred ?? 0m),
                new XAttribute("CashQty", x.CashQty ?? 0m),
                new XAttribute("MadaQty", x.MadaQty ?? 0m),
                new XAttribute("VisaQty", x.VisaQty ?? 0m),
                new XAttribute("DeferredQty", x.DeferredQty ?? 0m),
                new XAttribute("AmountH", x.AmountH ?? 0m),
                new XAttribute("AmountHComm", x.AmountHCommission ?? 0m),
                new XAttribute("AccountCode", x.AccountCodeInternal ?? string.Empty),
                new XAttribute("AccountCodeComm", x.CommissionAccountCodeInternal ?? string.Empty),
                new XAttribute("IsOther", x.IsOther == true),
                new XAttribute("DetailsPump", x.DetailsPump ?? string.Empty)))));
        }

        private static XDocument BuildPumpPaymentsXml(IEnumerable<PumpSalesEditPaymentViewModel> payments)
        {
            return new XDocument(new XElement("Payments", payments
                .Where(x => x != null && (x.Value ?? 0m) != 0m)
                .Select(x => new XElement("Payment",
                    new XAttribute("Id", x.Id ?? 0),
                    new XAttribute("PaymentId", x.PaymentId ?? 0),
                    new XAttribute("Value", x.Value ?? 0m),
                    new XAttribute("CardNo", x.CardNo ?? string.Empty),
                    new XAttribute("MaxValue", x.MaxValue ?? 0m)))));
        }

        private void LoadPumpDeferredCustomerOptions(PumpDeferredDistributionEditViewModel model)
        {
            try
            {
                using (var connection = _connectionFactory.CreateOpenConnection())
                using (var command = new SqlCommand(@"
SELECT TOP 300
    c.CusID,
    c.CusName,
    c.Account_Code,
    a.Account_Serial,
    COALESCE(a.Account_Name, a.Account_NameEng) AS AccountName
FROM TblCustemers c
LEFT JOIN ACCOUNTS a ON c.Account_Code = a.Account_Code
ORDER BY c.CusName, c.CusID;", connection))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var customerId = ReadInt(reader, "CusID");
                        if (!customerId.HasValue)
                        {
                            continue;
                        }

                        var customerName = ReadString(reader, "CusName");
                        var accountDisplay = FormatAccount(reader, "Account_Serial", "AccountName", "Account_Code");
                        model.CustomerOptions.Add(new SalesLookupOptionViewModel
                        {
                            Id = customerId.Value,
                            Display = customerId.Value.ToString(CultureInfo.InvariantCulture) + " - " + customerName,
                            AccountDisplay = accountDisplay
                        });
                    }
                }

                var loadedIds = new HashSet<int>(model.CustomerOptions.Select(x => x.Id));
                var selectedIds = model.Allocations
                    .Where(x => x.CustomerId.HasValue && !loadedIds.Contains(x.CustomerId.Value))
                    .Select(x => x.CustomerId.Value)
                    .Distinct()
                    .ToList();

                if (selectedIds.Count > 0)
                {
                    AddSelectedPumpCustomersToLookup(model, selectedIds);
                }
            }
            catch (SqlException ex)
            {
                Trace.TraceWarning("MainErp pump sales customer lookup schema warning: " + ex);
                model.Warnings.Add("تعذر تحميل قائمة العملاء؛ يمكن استخدام رقم العميل يدويًا لحين مراجعة بنية قاعدة البيانات.");
            }
        }

        private void AddSelectedPumpCustomersToLookup(PumpDeferredDistributionEditViewModel model, IList<int> customerIds)
        {
            var parameterNames = new List<string>();

            using (var connection = _connectionFactory.CreateOpenConnection())
            using (var command = connection.CreateCommand())
            {
                for (var index = 0; index < customerIds.Count; index++)
                {
                    var parameterName = "@CusId" + index.ToString(CultureInfo.InvariantCulture);
                    parameterNames.Add(parameterName);
                    command.Parameters.Add(parameterName, SqlDbType.Int).Value = customerIds[index];
                }

                command.CommandText = @"
SELECT c.CusID, c.CusName, c.Account_Code, a.Account_Serial, COALESCE(a.Account_Name, a.Account_NameEng) AS AccountName
FROM TblCustemers c
LEFT JOIN ACCOUNTS a ON c.Account_Code = a.Account_Code
WHERE c.CusID IN (" + string.Join(",", parameterNames) + ");";

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var customerId = ReadInt(reader, "CusID");
                        if (!customerId.HasValue)
                        {
                            continue;
                        }

                        var customerName = ReadString(reader, "CusName");
                        model.CustomerOptions.Add(new SalesLookupOptionViewModel
                        {
                            Id = customerId.Value,
                            Display = customerId.Value.ToString(CultureInfo.InvariantCulture) + " - " + customerName,
                            AccountDisplay = FormatAccount(reader, "Account_Serial", "AccountName", "Account_Code")
                        });
                    }
                }
            }
        }

        private void HydratePumpDeferredAllocationCustomers(IList<SalesInvoicePumpDeferredAllocationViewModel> allocations)
        {
            var customerIds = allocations
                .Where(x => x.CustomerId.HasValue)
                .Select(x => x.CustomerId.Value)
                .Distinct()
                .ToList();

            if (customerIds.Count == 0)
            {
                return;
            }

            var parameterNames = new List<string>();
            var customerMap = new Dictionary<int, Tuple<string, string, string>>();

            using (var connection = _connectionFactory.CreateOpenConnection())
            using (var command = connection.CreateCommand())
            {
                for (var index = 0; index < customerIds.Count; index++)
                {
                    var parameterName = "@CusId" + index.ToString(CultureInfo.InvariantCulture);
                    parameterNames.Add(parameterName);
                    command.Parameters.Add(parameterName, SqlDbType.Int).Value = customerIds[index];
                }

                command.CommandText = @"
SELECT c.CusID, c.CusName, c.Account_Code, a.Account_Serial, COALESCE(a.Account_Name, a.Account_NameEng) AS AccountName
FROM TblCustemers c
LEFT JOIN ACCOUNTS a ON c.Account_Code = a.Account_Code
WHERE c.CusID IN (" + string.Join(",", parameterNames) + ");";

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var customerId = ReadInt(reader, "CusID");
                        if (!customerId.HasValue)
                        {
                            continue;
                        }

                        customerMap[customerId.Value] = Tuple.Create(
                            ReadString(reader, "CusName"),
                            ReadString(reader, "Account_Code"),
                            FormatAccount(reader, "Account_Serial", "AccountName", "Account_Code"));
                    }
                }
            }

            foreach (var allocation in allocations)
            {
                if (!allocation.CustomerId.HasValue || !customerMap.ContainsKey(allocation.CustomerId.Value))
                {
                    continue;
                }

                var customer = customerMap[allocation.CustomerId.Value];
                allocation.CustomerName = customer.Item1;
                allocation.CustomerAccountCodeInternal = customer.Item2;
                allocation.CustomerAccountDisplay = customer.Item3;
            }
        }

        private static IList<SalesInvoicePumpDeferredAllocationViewModel> NormalizePumpDeferredAllocations(PumpDeferredDistributionEditViewModel model)
        {
            var allocations = new List<SalesInvoicePumpDeferredAllocationViewModel>();
            if (model == null || model.Allocations == null)
            {
                return allocations;
            }

            foreach (var allocation in model.Allocations)
            {
                if (allocation == null)
                {
                    continue;
                }

                var hasBusinessValue =
                    allocation.CustomerId.HasValue ||
                    !string.IsNullOrWhiteSpace(allocation.CustomerName) ||
                    (allocation.Amount ?? 0m) != 0m ||
                    (allocation.Quantity ?? 0m) != 0m;

                if (!hasBusinessValue)
                {
                    continue;
                }

                allocations.Add(new SalesInvoicePumpDeferredAllocationViewModel
                {
                    LineId = model.LineId,
                    CustomerId = allocation.CustomerId,
                    UnitId = allocation.UnitId ?? 1,
                    Amount = allocation.Amount ?? 0m,
                    CustomerName = (allocation.CustomerName ?? string.Empty).Trim(),
                    Quantity = allocation.Quantity ?? 0m,
                    UnitPrice = allocation.UnitPrice ?? 0m,
                    ReferenceNo = allocation.ReferenceNo
                });
            }

            return allocations;
        }

        private static string BuildDetailsPumpText(IEnumerable<SalesInvoicePumpDeferredAllocationViewModel> allocations)
        {
            var rows = allocations.Select(x =>
                (x.CustomerId.HasValue ? x.CustomerId.Value.ToString(CultureInfo.InvariantCulture) : string.Empty) + "#" +
                (x.UnitId.HasValue ? x.UnitId.Value.ToString(CultureInfo.InvariantCulture) : string.Empty) + "#" +
                (x.Amount ?? 0m).ToString("0.####", CultureInfo.InvariantCulture) + "#" +
                (x.CustomerName ?? string.Empty).Replace("#", " ").Replace("@", " ").Trim() + "#" +
                (x.Quantity ?? 0m).ToString("0.####", CultureInfo.InvariantCulture) + "#" +
                (x.UnitPrice ?? 0m).ToString("0.####", CultureInfo.InvariantCulture) + "#" +
                (x.ReferenceNo.HasValue ? x.ReferenceNo.Value.ToString(CultureInfo.InvariantCulture) : string.Empty) + "#@");

            return string.Concat(rows);
        }

        private static void LoadPumpDeferredCustomerAccounts(SqlConnection connection, SalesInvoiceDetailsViewModel model)
        {
            var customerIds = model.Lines
                .SelectMany(x => x.DeferredAllocations)
                .Where(x => x.CustomerId.HasValue)
                .Select(x => x.CustomerId.Value)
                .Distinct()
                .ToList();

            if (customerIds.Count == 0)
            {
                return;
            }

            var accountMap = new Dictionary<int, Tuple<string, string>>();
            var parameterNames = new List<string>();

            using (var command = connection.CreateCommand())
            {
                for (var index = 0; index < customerIds.Count; index++)
                {
                    var parameterName = "@CusId" + index.ToString(CultureInfo.InvariantCulture);
                    parameterNames.Add(parameterName);
                    command.Parameters.Add(parameterName, SqlDbType.Int).Value = customerIds[index];
                }

                command.CommandText = @"
SELECT c.CusID, c.Account_Code, a.Account_Serial, COALESCE(a.Account_Name, a.Account_NameEng) AS AccountName
FROM TblCustemers c
LEFT JOIN ACCOUNTS a ON c.Account_Code = a.Account_Code
WHERE c.CusID IN (" + string.Join(",", parameterNames) + ");";

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var customerId = ReadInt(reader, "CusID");
                        if (!customerId.HasValue)
                        {
                            continue;
                        }

                        var accountCode = ReadString(reader, "Account_Code");
                        var accountDisplay = FormatAccount(reader, "Account_Serial", "AccountName", "Account_Code");
                        accountMap[customerId.Value] = Tuple.Create(accountCode, accountDisplay);
                    }
                }
            }

            foreach (var allocation in model.Lines.SelectMany(x => x.DeferredAllocations))
            {
                if (!allocation.CustomerId.HasValue || !accountMap.ContainsKey(allocation.CustomerId.Value))
                {
                    continue;
                }

                allocation.CustomerAccountCodeInternal = accountMap[allocation.CustomerId.Value].Item1;
                allocation.CustomerAccountDisplay = accountMap[allocation.CustomerId.Value].Item2;
            }
        }

        private static void LoadHeader(SqlConnection connection, SalesInvoiceDetailsViewModel model, MainErpSalesInvoiceKind kind, int id)
        {
            using (var command = new SqlCommand(@"
SELECT TOP 1
    t.Transaction_ID,
    t.Transaction_Serial,
    t.NoteSerial,
    t.NoteSerial1,
    t.ManualNO,
    t.Transaction_Date,
    t.Transaction_Type,
    ISNULL(t.TypeInvoice, 0) AS TypeInvoice,
    t.CusID,
    t.BranchId,
    COALESCE(c.CusName, t.CashCustomerName) AS CustomerName,
    t.CashCustomerName,
    b.branch_name AS BranchName,
    CONVERT(nvarchar(50), t.StoreID) AS StoreName,
    t.Total,
    t.NetValue,
    t.VAT,
    t.PayedValue,
    t.RemainValue,
    t.NoteId,
    t.Closed,
    t.Posted,
    t.Approved,
    t.IsPosted,
    t.UserPosted,
    t.Prefix,
    t.Fullcode,
    t.CBoBasedON,
    t.POSBillType,
    t.Transaction_NetValue,
    t.SumValueLine,
    t.SumVATLine,
    t.DateRec,
    COALESCE(t.TransactionComment, t.remark, t.Nots) AS Remarks,
    t.PaymentType,
    t.Currency_id,
    t.Currency_rate,
    t.order_no,
    t.StoreID,
    t.BoxID,
    t.PaymentNetid
FROM Transactions t
LEFT JOIN TblCustemers c ON t.CusID = c.CusID
LEFT JOIN TblBranchesData b ON t.BranchId = b.branch_id
WHERE t.Transaction_ID = @Id
  AND t.Transaction_Type IN (21, 42, 38, 9)
  AND ((@TypeInvoice = 2 AND ISNULL(t.TypeInvoice, 0) = 2) OR (@TypeInvoice = 1 AND ISNULL(t.TypeInvoice, 0) <> 2));", connection))
            {
                command.Parameters.Add("@Id", SqlDbType.Int).Value = id;
                command.Parameters.Add("@TypeInvoice", SqlDbType.Int).Value = kind == MainErpSalesInvoiceKind.Pump ? 2 : 1;

                using (var reader = command.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        model.TransactionId = id;
                        model.Warning = "لم يتم العثور على الفاتورة المطلوبة في نطاق الشاشة الحالية.";
                        return;
                    }

                    var list = ReadListItem(reader);
                    model.TransactionId = list.TransactionId;
                    model.TransactionSerial = list.TransactionSerial;
                    model.NoteSerial = list.NoteSerial;
                    model.NoteSerial1 = list.NoteSerial1;
                    model.ManualNo = list.ManualNo;
                    model.TransactionDate = list.TransactionDate;
                    model.TransactionType = list.TransactionType;
                    model.TypeInvoice = list.TypeInvoice;
                    model.CustomerName = list.CustomerName;
                    model.CashCustomerName = list.CashCustomerName;
                    model.BranchName = list.BranchName;
                    model.StoreName = list.StoreName;
                    model.Total = list.Total;
                    model.NetValue = list.NetValue;
                    model.Vat = list.Vat;
                    model.PayedValue = list.PayedValue;
                    model.RemainValue = list.RemainValue;
                    model.NoteId = list.NoteId;
                    model.Closed = list.Closed;
                    model.Posted = ReadBoolFlexible(reader, "Posted");
                    model.Approved = ReadBoolFlexible(reader, "Approved");
                    model.Prefix = ReadString(reader, "Prefix");
                    model.FullCode = ReadString(reader, "Fullcode");
                    model.CboBasedOn = ReadInt(reader, "CBoBasedON");
                    model.PosBillType = ReadInt(reader, "POSBillType");
                    model.TransactionNetValue = ReadDecimal(reader, "Transaction_NetValue");
                    model.SumValueLine = ReadDecimal(reader, "SumValueLine");
                    model.SumVatLine = ReadDecimal(reader, "SumVATLine");
                    model.DateRec = ReadDate(reader, "DateRec");
                    model.Remarks = ReadString(reader, "Remarks");
                    model.PaymentTypeLabel = ReadString(reader, "PaymentType");
                    model.CurrencyId = ReadString(reader, "Currency_id");
                    model.CurrencyRate = ReadDecimal(reader, "Currency_rate");
                    model.OrderNo = ReadString(reader, "order_no");
                    model.StoreId = ReadInt(reader, "StoreID");
                    model.BoxId = ReadInt(reader, "BoxID");
                    model.PaymentNetId = ReadInt(reader, "PaymentNetid");
                }
            }
        }

        private static void LoadLines(SqlConnection connection, SalesInvoiceDetailsViewModel model, int id)
        {
            using (var command = new SqlCommand(@"
SELECT
    d.ID,
    d.LineID AS DetailLineNo,
    d.Item_ID,
    i.ItemCode,
    COALESCE(i.ItemName, i.ItemNamee) AS ItemName,
    u.UnitName,
    d.ShowQty,
    d.Quantity,
    d.ShowPrice,
    d.Price,
    d.CostPrice,
    d.discountvalue,
    d.TotalDiscountPerLine,
    d.Vat,
    d.Vatyo,
    d.Remarks,
    COALESCE(d.AccountCode, d.Account_Code) AS AccountCode,
    a.Account_Serial,
    COALESCE(a.Account_Name, a.Account_NameEng) AS AccountName,
    d.Account_CodeComm,
    ac.Account_Serial AS CommissionAccountSerial,
    COALESCE(ac.Account_Name, ac.Account_NameEng) AS CommissionAccountName,
    d.PumpId,
    p.Name AS PumpName,
    d.IsOther,
    d.ColorID,
    d.PrevQty,
    d.CurrentQty,
    d.Cash,
    d.Mada,
    d.Visa,
    d.Deferred,
    d.CashQty,
    d.MadaQty,
    d.VisaQty,
    d.DeferredQty,
    d.AmountH,
    d.AmountHComm,
    CONVERT(nvarchar(max), d.DetailsPump) AS DetailsPump
FROM Transaction_Details d
LEFT JOIN TblItems i ON d.Item_ID = i.ItemID
LEFT JOIN TblUnites u ON d.UnitId = u.UnitID
LEFT JOIN tblPumpType p ON d.PumpId = p.ID
LEFT JOIN ACCOUNTS a ON COALESCE(d.AccountCode, d.Account_Code) = a.Account_Code
LEFT JOIN ACCOUNTS ac ON d.Account_CodeComm = ac.Account_Code
WHERE d.Transaction_ID = @Id
ORDER BY d.ID;", connection))
            {
                command.Parameters.Add("@Id", SqlDbType.Int).Value = id;
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        model.Lines.Add(new SalesInvoiceLineViewModel
                        {
                            Id = ReadInt(reader, "ID").GetValueOrDefault(),
                            LineNumber = ReadIntIfExists(reader, "DetailLineNo"),
                            ItemId = ReadInt(reader, "Item_ID"),
                            ItemCode = ReadString(reader, "ItemCode"),
                            ItemName = ReadString(reader, "ItemName"),
                            UnitName = ReadString(reader, "UnitName"),
                            ShowQty = ReadDecimal(reader, "ShowQty"),
                            Quantity = ReadDecimal(reader, "Quantity"),
                            ShowPrice = ReadDecimal(reader, "ShowPrice"),
                            Price = ReadDecimal(reader, "Price"),
                            CostPrice = ReadDecimal(reader, "CostPrice"),
                            DiscountValue = ReadDecimal(reader, "discountvalue"),
                            TotalDiscountPerLine = ReadDecimal(reader, "TotalDiscountPerLine"),
                            Vat = ReadDecimal(reader, "Vat"),
                            VatYou = ReadDecimal(reader, "Vatyo"),
                            Remarks = ReadString(reader, "Remarks"),
                            AccountCodeInternal = ReadString(reader, "AccountCode"),
                            AccountDisplay = FormatAccount(reader, "Account_Serial", "AccountName", "AccountCode"),
                            CommissionAccountCodeInternal = ReadString(reader, "Account_CodeComm"),
                            CommissionAccountDisplay = FormatAccount(reader, "CommissionAccountSerial", "CommissionAccountName", "Account_CodeComm"),
                            PumpId = ReadInt(reader, "PumpId"),
                            PumpName = ReadString(reader, "PumpName"),
                            IsOther = ReadBoolFlexible(reader, "IsOther"),
                            ColorId = ReadInt(reader, "ColorID"),
                            PrevQty = ReadDecimal(reader, "PrevQty"),
                            CurrentQty = ReadDecimal(reader, "CurrentQty"),
                            Cash = ReadDecimal(reader, "Cash"),
                            Mada = ReadDecimal(reader, "Mada"),
                            Visa = ReadDecimal(reader, "Visa"),
                            Deferred = ReadDecimal(reader, "Deferred"),
                            CashQty = ReadDecimal(reader, "CashQty"),
                            MadaQty = ReadDecimal(reader, "MadaQty"),
                            VisaQty = ReadDecimal(reader, "VisaQty"),
                            DeferredQty = ReadDecimal(reader, "DeferredQty"),
                            AmountH = ReadDecimal(reader, "AmountH"),
                            AmountHCommission = ReadDecimal(reader, "AmountHComm"),
                            PumpDetails = ReadString(reader, "DetailsPump")
                        });
                    }
                }
            }
        }

        private static void LoadPayments(SqlConnection connection, SalesInvoiceDetailsViewModel model, int id)
        {
            using (var command = new SqlCommand(@"
SELECT ID, TransID, PaymentID, Value, CardNo, MaxValue
FROM TblSalesPayment
WHERE TransID = @Id
ORDER BY ID;", connection))
            {
                command.Parameters.Add("@Id", SqlDbType.Int).Value = id;
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        model.Payments.Add(new SalesInvoicePaymentViewModel
                        {
                            Id = ReadInt(reader, "ID").GetValueOrDefault(),
                            PaymentId = ReadInt(reader, "PaymentID"),
                            Value = ReadDecimal(reader, "Value"),
                            CardNo = ReadString(reader, "CardNo"),
                            MaxValue = ReadDecimal(reader, "MaxValue")
                        });
                    }
                }
            }
        }

        private static void LoadVoucherLines(SqlConnection connection, SalesInvoiceDetailsViewModel model, int id, int? noteId)
        {
            using (var command = new SqlCommand(@"
SELECT
    v.Double_Entry_Vouchers_ID,
    v.DEV_ID_Line_No,
    v.Notes_ID,
    n.NoteSerial,
    v.RecordDate,
    v.Account_Code,
    a.Account_Serial,
    COALESCE(a.Account_Name, a.Account_NameEng) AS AccountName,
    CASE WHEN v.Credit_Or_Debit = 0 THEN ISNULL(v.Value, 0) ELSE ISNULL(v.depet_value, 0) END AS Debit,
    CASE WHEN v.Credit_Or_Debit = 1 THEN ISNULL(v.Value, 0) ELSE ISNULL(v.credit_value, 0) END AS Credit,
    COALESCE(v.Double_Entry_Vouchers_Description, v.des) AS Description
FROM DOUBLE_ENTREY_VOUCHERS v
LEFT JOIN Notes n ON v.Notes_ID = n.NoteID
LEFT JOIN ACCOUNTS a ON v.Account_Code = a.Account_Code
WHERE v.Transaction_ID = @Id
   OR v.Transaction_ID1 = @Id
   OR (@NoteId IS NOT NULL AND v.Notes_ID = @NoteId)
ORDER BY v.Double_Entry_Vouchers_ID, v.DEV_ID_Line_No;", connection))
            {
                command.Parameters.Add("@Id", SqlDbType.Int).Value = id;
                command.Parameters.Add("@NoteId", SqlDbType.Int).Value = (object)noteId ?? DBNull.Value;
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        model.VoucherLines.Add(new SalesInvoiceVoucherLineViewModel
                        {
                            VoucherId = ReadInt(reader, "Double_Entry_Vouchers_ID").GetValueOrDefault(),
                            LineNo = ReadInt(reader, "DEV_ID_Line_No"),
                            NoteId = ReadInt(reader, "Notes_ID"),
                            NoteSerial = ReadString(reader, "NoteSerial"),
                            RecordDate = ReadDate(reader, "RecordDate"),
                            AccountCodeInternal = ReadString(reader, "Account_Code"),
                            AccountDisplay = FormatAccount(reader, "Account_Serial", "AccountName", "Account_Code"),
                            Debit = ReadDecimal(reader, "Debit").GetValueOrDefault(),
                            Credit = ReadDecimal(reader, "Credit").GetValueOrDefault(),
                            Description = ReadString(reader, "Description")
                        });
                    }
                }
            }
        }

        private static void LoadRelatedInventoryTransactions(SqlConnection connection, SalesInvoiceDetailsViewModel model, int id)
        {
            using (var command = new SqlCommand(@"
SELECT TOP 50
    t.Transaction_ID,
    t.Transaction_Serial,
    t.Transaction_Type,
    t.Transaction_Date,
    t.NoteSerial,
    t.NoteSerial1,
    CONVERT(nvarchar(50), t.StoreID) AS StoreName,
    t.Total,
    t.NetValue,
    t.NoteId,
    CASE
        WHEN t.Transaction_Type = 19 THEN N'Issue voucher generated from FrmSaleBill6'
        WHEN t.Transaction_Type = 20 THEN N'Receive voucher generated from FrmSaleBill6'
        ELSE N'Related inventory transaction'
    END AS LinkReason
FROM Transactions t
WHERE t.Transaction_Type IN (19, 20)
  AND (
        LTRIM(RTRIM(CONVERT(nvarchar(100), t.nots))) = CONVERT(nvarchar(100), @Id)
        OR (@NoteSerial1 IS NOT NULL AND LTRIM(RTRIM(CONVERT(nvarchar(100), t.nots2))) = @NoteSerial1)
      )
ORDER BY t.Transaction_ID DESC;", connection))
            {
                command.Parameters.Add("@Id", SqlDbType.Int).Value = id;
                command.Parameters.Add("@NoteSerial1", SqlDbType.NVarChar, 100).Value = string.IsNullOrWhiteSpace(model.NoteSerial1) ? (object)DBNull.Value : model.NoteSerial1.Trim();

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        model.RelatedInventoryTransactions.Add(new SalesInvoiceRelatedInventoryTransactionViewModel
                        {
                            TransactionId = ReadInt(reader, "Transaction_ID").GetValueOrDefault(),
                            TransactionSerial = ReadString(reader, "Transaction_Serial"),
                            TransactionType = ReadInt(reader, "Transaction_Type"),
                            TransactionDate = ReadDate(reader, "Transaction_Date"),
                            NoteSerial = ReadString(reader, "NoteSerial"),
                            NoteSerial1 = ReadString(reader, "NoteSerial1"),
                            StoreName = ReadString(reader, "StoreName"),
                            Total = ReadDecimal(reader, "Total"),
                            NetValue = ReadDecimal(reader, "NetValue"),
                            NoteId = ReadInt(reader, "NoteId"),
                            LinkReason = ReadString(reader, "LinkReason")
                        });
                    }
                }
            }
        }

        private static void LoadAuditLogs(SqlConnection connection, SalesInvoiceDetailsViewModel model, int id)
        {
            if (!TableExists(connection, "MainErp_AuditLog"))
            {
                return;
            }

            var hasSnapshots = ColumnExists(connection, "MainErp_AuditLog", "BeforeSnapshot")
                && ColumnExists(connection, "MainErp_AuditLog", "AfterSnapshot");

            var sql = @"
SELECT TOP 100
    a.AuditId,
    a.OperationName,
    a.EntityName,
    a.EntityKey,
    a.UserId,
    u.UserName AS UserDisplay,
    a.CorrelationId,
    a.Message,
    " + (hasSnapshots ? "a.BeforeSnapshot, a.AfterSnapshot," : "CAST(NULL AS nvarchar(max)) AS BeforeSnapshot, CAST(NULL AS nvarchar(max)) AS AfterSnapshot,") + @"
    a.CreatedAt
FROM dbo.MainErp_AuditLog a
LEFT JOIN dbo.TblUsers u ON u.UserID = a.UserId
WHERE EntityName = N'Transactions'
  AND EntityKey = CONVERT(nvarchar(100), @Id)
ORDER BY a.CreatedAt DESC, a.AuditId DESC;";

            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.Add("@Id", SqlDbType.Int).Value = id;

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        model.AuditLogs.Add(new SalesInvoiceAuditLogViewModel
                        {
                            AuditId = ReadInt(reader, "AuditId").GetValueOrDefault(),
                            OperationName = ReadString(reader, "OperationName"),
                            EntityName = ReadString(reader, "EntityName"),
                            EntityKey = ReadString(reader, "EntityKey"),
                            UserId = ReadInt(reader, "UserId"),
                            UserDisplay = ReadString(reader, "UserDisplay"),
                            CorrelationId = ReadGuid(reader, "CorrelationId"),
                            Message = ReadString(reader, "Message"),
                            BeforeSnapshot = ReadString(reader, "BeforeSnapshot"),
                            AfterSnapshot = ReadString(reader, "AfterSnapshot"),
                            CreatedAt = ReadDate(reader, "CreatedAt")
                        });
                    }
                }
            }
        }

        private static string FormatAccount(IDataRecord reader, string serialColumn, string nameColumn, string codeColumn)
        {
            var serial = ReadString(reader, serialColumn);
            var name = ReadString(reader, nameColumn);
            var code = ReadString(reader, codeColumn);

            if (!string.IsNullOrWhiteSpace(serial) && !string.IsNullOrWhiteSpace(name))
            {
                return serial + " - " + name;
            }

            if (!string.IsNullOrWhiteSpace(name))
            {
                return name;
            }

            return string.IsNullOrWhiteSpace(code) ? string.Empty : "الحساب غير موجود";
        }

        private static void BuildSavePreview(SalesInvoiceDetailsViewModel model)
        {
            model.SavePreview.HeaderEffects.Add("سيتم التعامل مع رأس الفاتورة في Transactions كمرجع أساسي، لكن هذه المعاينة لا تنفذ أي حفظ.");
            model.SavePreview.HeaderEffects.Add("نوع الحركة الحالي: " + (model.TransactionType.HasValue ? model.TransactionType.Value.ToString() : "غير محدد") + "، TypeInvoice: " + (model.TypeInvoice.HasValue ? model.TypeInvoice.Value.ToString() : "غير محدد"));
            model.SavePreview.DetailEffects.Add("عدد البنود المتوقع: " + model.Lines.Count);
            model.SavePreview.DetailEffects.Add("إجمالي كمية تأثير المخزون من Transaction_Details: " + model.InventoryQuantityTotal.ToString("n2"));
            model.SavePreview.DetailEffects.Add("إجمالي طرق الدفع المقروءة من TblSalesPayment: " + model.PaymentRowsTotal.ToString("n2"));
            model.SavePreview.DetailEffects.Add("إجمالي توزيع الآجل على عملاء المضخات من DetailsPump: " + model.PumpDeferredDistributionTotal.ToString("n2") + " / كمية: " + model.PumpDeferredDistributionQuantityTotal.ToString("n2"));
            model.SavePreview.InventoryEffects.Add("تأثير المخزون الحقيقي يحتاج مراجعة CreateIssueVoucher2/CreateRecieveVoucher قبل أي تنفيذ.");
            model.SavePreview.InventoryEffects.Add("فواتير صرف مرتبطة: " + model.RelatedInventoryIssueCount + " / فواتير استلام مرتبطة: " + model.RelatedInventoryReceiptCount);
            model.SavePreview.InventoryEffects.Add("التكلفة التقديرية من البنود الحالية: " + model.LinesCost.ToString("n2"));
            model.SavePreview.AccountingEffects.Add("سطور القيود المقروءة حاليًا: " + model.VoucherLines.Count);
            model.SavePreview.AccountingEffects.Add("إجمالي مدين: " + model.VoucherDebitTotal.ToString("n2") + " / إجمالي دائن: " + model.VoucherCreditTotal.ToString("n2"));

            if (model.Lines.Count == 0)
            {
                model.SavePreview.Warnings.Add("لا توجد بنود، لذلك لا يمكن محاكاة تأثير المخزون بدقة.");
            }

            if (model.VoucherLines.Count == 0)
            {
                model.SavePreview.Warnings.Add("لا توجد قيود مرتبطة مقروءة، لذلك معاينة القيد ستظل ناقصة حتى اكتمال ربط Notes/DOUBLE_ENTREY_VOUCHERS.");
            }

            if (model.RelatedInventoryTransactions.Count == 0)
            {
                model.SavePreview.Warnings.Add("لم يتم العثور على فواتير صرف/استلام مرتبطة عبر nots أو nots2.");
            }

            if (Math.Abs(model.VoucherBalanceDifference) >= 0.01m)
            {
                model.SavePreview.Warnings.Add("القيد المقروء غير متوازن، يجب مراجعة الربط أو سطور القيد قبل تفعيل أي حفظ.");
            }

            if (model.Kind == MainErpSalesInvoiceKind.Pump && model.Lines.Count == 0)
            {
                model.SavePreview.Warnings.Add("فاتورة المضخات تحتاج بيانات TypeInvoice=2 وبنود PumpId/DetailsPump لتأكيد المسار.");
            }
        }

        private static string ReadString(IDataRecord reader, string column)
        {
            var ordinal = reader.GetOrdinal(column);
            return reader.IsDBNull(ordinal) ? string.Empty : Convert.ToString(reader.GetValue(ordinal));
        }

        private static string ReadStringIfExists(IDataRecord reader, string column)
        {
            return HasColumn(reader, column) ? ReadString(reader, column) : string.Empty;
        }

        private static bool HasColumn(IDataRecord reader, string column)
        {
            for (var index = 0; index < reader.FieldCount; index++)
            {
                if (string.Equals(reader.GetName(index), column, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static int? ReadIntIfExists(IDataRecord reader, string column)
        {
            return HasColumn(reader, column) ? ReadInt(reader, column) : (int?)null;
        }

        private static int? ReadInt(IDataRecord reader, string column)
        {
            var ordinal = reader.GetOrdinal(column);
            return reader.IsDBNull(ordinal) ? (int?)null : Convert.ToInt32(reader.GetValue(ordinal));
        }

        private static DateTime? ReadDate(IDataRecord reader, string column)
        {
            var ordinal = reader.GetOrdinal(column);
            return reader.IsDBNull(ordinal) ? (DateTime?)null : Convert.ToDateTime(reader.GetValue(ordinal));
        }

        private static DateTime? ReadDateIfExists(IDataRecord reader, string column)
        {
            return HasColumn(reader, column) ? ReadDate(reader, column) : (DateTime?)null;
        }

        private static Guid? ReadGuid(IDataRecord reader, string column)
        {
            var ordinal = reader.GetOrdinal(column);
            return reader.IsDBNull(ordinal) ? (Guid?)null : (Guid)reader.GetValue(ordinal);
        }

        private static decimal? ReadDecimal(IDataRecord reader, string column)
        {
            var ordinal = reader.GetOrdinal(column);
            return reader.IsDBNull(ordinal) ? (decimal?)null : Convert.ToDecimal(reader.GetValue(ordinal));
        }

        private static decimal? ReadDecimalIfExists(IDataRecord reader, string column)
        {
            return HasColumn(reader, column) ? ReadDecimal(reader, column) : (decimal?)null;
        }

        private static string GetPart(string[] parts, int index)
        {
            return parts != null && parts.Length > index ? (parts[index] ?? string.Empty).Trim() : string.Empty;
        }

        private static int? ParseInt(string[] parts, int index)
        {
            var text = GetPart(parts, index);
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            int value;
            return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value) ? value : (int?)null;
        }

        private static decimal? ParseDecimal(string[] parts, int index)
        {
            var text = GetPart(parts, index);
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            decimal value;
            return decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out value) ? value : (decimal?)null;
        }

        private static bool? ReadBool(IDataRecord reader, string column)
        {
            var ordinal = reader.GetOrdinal(column);
            return reader.IsDBNull(ordinal) ? (bool?)null : Convert.ToBoolean(reader.GetValue(ordinal));
        }

        private static bool? ReadBoolFlexible(IDataRecord reader, string column)
        {
            if (!HasColumn(reader, column))
            {
                return null;
            }

            var ordinal = reader.GetOrdinal(column);
            if (reader.IsDBNull(ordinal))
            {
                return null;
            }

            var value = reader.GetValue(ordinal);
            if (value is bool)
            {
                return (bool)value;
            }

            var text = Convert.ToString(value);
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            decimal numericValue;
            if (decimal.TryParse(text, out numericValue))
            {
                return numericValue != 0m;
            }

            bool booleanValue;
            return bool.TryParse(text, out booleanValue) ? booleanValue : (bool?)null;
        }

        private static bool IsMissingStoredProcedure(SqlException ex)
        {
            return ex != null && ex.Number == 2812;
        }
    }
}
