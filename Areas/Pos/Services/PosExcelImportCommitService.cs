using MyERP.Areas.Pos.Data;
using MyERP.Areas.Pos.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace MyERP.Areas.Pos.Services
{
    public class PosExcelImportCommitService
    {
        private readonly PosSqlRepository _repository;

        public PosExcelImportCommitService(PosSqlRepository repository)
        {
            if (repository == null)
            {
                throw new ArgumentNullException("repository");
            }

            _repository = repository;
        }

        public PosExcelImportCommitResult Commit(PosExcelImportPreviewResult preview, PosUserContext importContext, Action<PosExcelImportCommitProgress> progressCallback = null)
        {
            if (preview == null)
            {
                throw new InvalidOperationException("لا توجد معاينة محفوظة للترحيل.");
            }

            if (importContext == null)
            {
                throw new InvalidOperationException("يجب تسجيل دخول نقطة البيع قبل الترحيل.");
            }

            ValidateCommitReadiness(preview);

            var defaults = preview.EffectiveDefaults;
            var branchId = defaults.BranchId.GetValueOrDefault();
            var importRange = GetImportDateRange(preview);
            return _repository.ExecuteWithPosExcelImportBranchLock(branchId, delegate
            {
                var overlap = _repository.GetPosExcelImportDateOverlap(branchId, importRange.Item1, importRange.Item2);
                if (overlap != null && overlap.HasOverlap)
                {
                    throw new InvalidOperationException(BuildOverlapMessage(defaults, importRange.Item1, importRange.Item2, overlap));
                }

            var batchId = _repository.CreatePosExcelImportBatch(preview.SourceFileName, preview.SourceFileHash, importContext.UserId, defaults.BranchId);
            var result = new PosExcelImportCommitResult { BatchId = batchId, Status = "Committing" };
            var totalCount = preview.Rows.Count;
            var processedCount = 0;
            ReportProgress(progressCallback, result, totalCount, processedCount, null, "بدأ الترحيل");

            foreach (var row in preview.Rows.OrderBy(x => x.SheetName).ThenBy(x => x.RowNumber))
            {
                ReportProgress(progressCallback, result, totalCount, processedCount, row, "جاري معالجة الصف");
                var rowResult = new PosExcelImportCommitRowResult
                {
                    SheetName = row.SheetName,
                    RowNumber = row.RowNumber,
                    IPN = row.IPN,
                    ServiceType = row.InternalServiceName,
                    Status = "Pending"
                };

                try
                {
                    if (!IsCommitReadyRow(row))
                    {
                        rowResult.Status = "Skipped";
                        rowResult.Message = BuildSkipReason(row);
                        result.SkippedCount++;
                        result.Rows.Add(rowResult);
                        processedCount++;
                        ReportProgress(progressCallback, result, totalCount, processedCount, row, rowResult.Message);
                        continue;
                    }

                    if (_repository.PosExcelImportSourceRowExists(preview.SourceFileHash, row.SheetName, row.RowNumber))
                    {
                        rowResult.Status = "Skipped";
                        rowResult.Message = "تم ترحيل نفس صف Excel سابقا؛ تم تجاهله.";
                        result.SkippedCount++;
                        result.Rows.Add(rowResult);
                        processedCount++;
                        ReportProgress(progressCallback, result, totalCount, processedCount, row, rowResult.Message);
                        continue;
                    }

                    var request = BuildSaveRequest(preview, row, defaults, importContext);
                    var saved = _repository.SaveTransaction(request);
                    if (saved == null || saved.Transaction_ID <= 0 || !_repository.TransactionExists(saved.Transaction_ID) || !_repository.TransactionHasDetails(saved.Transaction_ID))
                    {
                        throw new InvalidOperationException("تم استدعاء حفظ POS لكن لم يتم تأكيد الفاتورة والتفاصيل.");
                    }

                    VerifyPersistedServiceType(saved.Transaction_ID, row.InternalServiceType, row.ServiceItemId);

                    rowResult.Status = "Imported";
                    rowResult.TransactionId = saved.Transaction_ID;
                    rowResult.NoteSerial1 = saved.NoteSerial1;
                    rowResult.Message = "تم الترحيل";
                    result.ImportedCount++;
                }
                catch (Exception ex)
                {
                    rowResult.Status = "Failed";
                    rowResult.Message = ex.Message;
                    result.FailedCount++;
                }

                if (rowResult.Status == "Imported" || rowResult.Status == "Failed")
                {
                    _repository.InsertPosExcelImportBatchRow(batchId, row.SheetName, row.RowNumber, row.IPN, row.MatchedToken, rowResult.Status, rowResult.TransactionId, rowResult.Message);
                }

                result.Rows.Add(rowResult);
                processedCount++;
                ReportProgress(progressCallback, result, totalCount, processedCount, row, rowResult.Message);
            }

            result.Status = result.FailedCount > 0 ? "ImportedWithErrors" : "Imported";
            _repository.UpdatePosExcelImportBatch(batchId, result.Status, result.ImportedCount, result.FailedCount);
            ReportProgress(progressCallback, result, totalCount, processedCount, null, "انتهى الترحيل");
            return result;
            });
        }

        private static Tuple<DateTime, DateTime> GetImportDateRange(PosExcelImportPreviewResult preview)
        {
            var dates = preview.Rows
                .Where(x => x != null && x.TransactionDate.HasValue)
                .Select(x => x.TransactionDate.Value.Date)
                .ToList();

            if (dates.Count == 0)
            {
                throw new InvalidOperationException("لا يمكن تحديد فترة ملف Excel لأن الصفوف لا تحتوي على تاريخ صالح.");
            }

            return Tuple.Create(dates.Min(), dates.Max());
        }

        private static string BuildOverlapMessage(PosExcelImportDefaultContext defaults, DateTime importFrom, DateTime importTo, PosExcelImportOverlapResult overlap)
        {
            var branchName = defaults == null || string.IsNullOrWhiteSpace(defaults.BranchName)
                ? "الفرع المحدد"
                : defaults.BranchName;
            var existingFrom = overlap.ExistingFromDate.HasValue ? overlap.ExistingFromDate.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : "-";
            var existingTo = overlap.ExistingToDate.HasValue ? overlap.ExistingToDate.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : "-";
            return string.Format(
                CultureInfo.InvariantCulture,
                "تم رفض ترحيل ملف Excel بالكامل: توجد {0} فاتورة Excel سابقة للفرع {1} داخل فترة متداخلة. فترة الملف {2} إلى {3}. الفترة الموجودة {4} إلى {5}. لا يتم حفظ أي فاتورة من الملف.",
                overlap.InvoiceCount,
                branchName,
                importFrom.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                importTo.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                existingFrom,
                existingTo);
        }

        private static void ReportProgress(Action<PosExcelImportCommitProgress> progressCallback, PosExcelImportCommitResult result, int totalCount, int processedCount, PosExcelImportRowPreview row, string message)
        {
            if (progressCallback == null)
            {
                return;
            }

            progressCallback(new PosExcelImportCommitProgress
            {
                Status = result.Status,
                TotalCount = totalCount,
                ProcessedCount = processedCount,
                ImportedCount = result.ImportedCount,
                FailedCount = result.FailedCount,
                SkippedCount = result.SkippedCount,
                CurrentSheet = row == null ? null : row.SheetName,
                CurrentRowNumber = row == null ? 0 : row.RowNumber,
                CurrentServiceType = row == null ? null : row.InternalServiceName,
                CurrentMessage = message
            });
        }

        private static void ValidateCommitReadiness(PosExcelImportPreviewResult preview)
        {
            if (preview.DetectedBranch == null || preview.EffectiveDefaults == null)
            {
                throw new InvalidOperationException("الفرع أو defaults غير مكتملة.");
            }

            if (preview.EffectiveDefaults.UserId.GetValueOrDefault() <= 0
                || preview.EffectiveDefaults.EmpId.GetValueOrDefault() <= 0
                || preview.EffectiveDefaults.StoreId.GetValueOrDefault() <= 0
                || preview.EffectiveDefaults.BoxId.GetValueOrDefault() <= 0
                || preview.EffectiveDefaults.PaymentTypeId.GetValueOrDefault() <= 0)
            {
                throw new InvalidOperationException("بيانات المستخدم/المندوب/المخزن/الخزنة/الدفع غير مكتملة.");
            }

            var hasReadyRows = preview.Rows.Any(IsCommitReadyRow);
            if (!hasReadyRows)
            {
                throw new InvalidOperationException("لا توجد صفوف جاهزة للترحيل. سيتم ترك الصفوف المرفوضة كما هي.");
            }
        }

        private static bool IsCommitReadyRow(PosExcelImportRowPreview row)
        {
            if (row == null
                || string.Equals(row.Status, "Rejected", StringComparison.OrdinalIgnoreCase)
                || string.Equals(row.Status, "ImportedBefore", StringComparison.OrdinalIgnoreCase)
                || string.IsNullOrWhiteSpace(row.InternalServiceType))
            {
                return false;
            }

            if (string.Equals(row.InternalServiceType, "card", StringComparison.OrdinalIgnoreCase)
                && string.IsNullOrWhiteSpace(row.MatchedToken))
            {
                return false;
            }

            return true;
        }

        private static string BuildSkipReason(PosExcelImportRowPreview row)
        {
            if (row == null)
            {
                return "صف غير صالح.";
            }

            if (row.Reasons != null && row.Reasons.Count > 0)
            {
                return string.Join(" - ", row.Reasons);
            }

            if (string.Equals(row.Status, "Rejected", StringComparison.OrdinalIgnoreCase))
            {
                return "صف مرفوض في preflight.";
            }

            if (string.Equals(row.Status, "ImportedBefore", StringComparison.OrdinalIgnoreCase))
            {
                return "الصف معلّم في ملف Excel بأنه تم ترحيله سابقا.";
            }

            if (string.IsNullOrWhiteSpace(row.InternalServiceType))
            {
                return "نوع الخدمة غير معروف.";
            }

            if (string.Equals(row.InternalServiceType, "card", StringComparison.OrdinalIgnoreCase)
                && string.IsNullOrWhiteSpace(row.MatchedToken))
            {
                return "صف كارت كيشني بلا توكن مطابق.";
            }

            return "تم ترك الصف لأنه غير جاهز للترحيل.";
        }

        private PosSaveTransactionRequest BuildSaveRequest(PosExcelImportPreviewResult preview, PosExcelImportRowPreview row, PosExcelImportDefaultContext defaults, PosUserContext importContext)
        {
            var item = _repository.GetDefaultServiceItem(row.InternalServiceType, row.ServiceItemId, defaults.BranchId).FirstOrDefault();
            if (item == null)
            {
                throw new InvalidOperationException("لم يتم العثور على صنف الخدمة الافتراضي.");
            }

            var amount = row.Amount.GetValueOrDefault();
            var total = row.GrossTotal.GetValueOrDefault(amount);
            var paymentTypeId = defaults.PaymentTypeId.GetValueOrDefault();
            var sourceKey = BuildSourceKey(preview, row);
            var screenId = string.IsNullOrWhiteSpace(row.IPN) ? sourceKey : row.IPN.Trim();
            var request = new PosSaveTransactionRequest
            {
                TransactionType = row.InternalServiceType,
                TransactionDate = (row.TransactionDate ?? DateTime.Today).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                BranchId = defaults.BranchId,
                StoreID = defaults.StoreId,
                UserID = defaults.UserId,
                Emp_ID = defaults.EmpId,
                CustomerID = 2,
                DefaultCustomerId = 2,
                PaymentType = paymentTypeId,
                BoxID = defaults.BoxId,
                PaymentNetid = defaults.PaymentNetId,
                PayedValue = total,
                NetValue = total,
                RemainValue = 0,
                PayType = 1,
                POSBillType = 0,
                STableID = -1,
                SessionD = -1,
                BillBasedOn = 0,
                CashCustomerName = Clean(row.CustomerName, "عميل Excel"),
                CashCustomerPhone = Clean(row.Phone, string.Empty),
                IPN = screenId,
                ManualNO = IsImportantIpn(row.InternalServiceType) ? screenId : Clean(row.IPN, string.Empty),
                NoID = PosSqlRepository.WebInvoiceSourceMarker,
                ManualNo2 = sourceKey,
                RechargeType = row.ServiceType,
                ItemIDService = item.Item_ID,
                Items = new List<PosTransactionItemDto> { ToTransactionItem(item, row.InternalServiceType) },
                SalesPayments = new List<PosSalesPaymentDto>
                {
                    new PosSalesPaymentDto
                    {
                        PaymentID = paymentTypeId,
                        PaymentName = defaults.PaymentName,
                        Value = total,
                        MaxValue = total
                    }
                }
            };

            ApplyServiceSpecificValues(request, row, item, importContext, defaults);
            NormalizeServiceFlags(request, row.InternalServiceType);
            return request;
        }

        private void ApplyServiceSpecificValues(PosSaveTransactionRequest request, PosExcelImportRowPreview row, PosItemLookupDto item, PosUserContext importContext, PosExcelImportDefaultContext defaults)
        {
            var serviceType = row.InternalServiceType ?? string.Empty;
            if (string.Equals(serviceType, "violations", StringComparison.OrdinalIgnoreCase))
            {
                request.PayedValue = 50;
                request.NetValue = 50;
                request.ViolationsValue = row.Amount.GetValueOrDefault(50);
                request.TrafficViolations = true;
                request.ViolationPayType = 1;
                request.Tet_NumPoket = Clean(row.Phone, row.IPN);
                return;
            }

            if (string.Equals(serviceType, "cash-out", StringComparison.OrdinalIgnoreCase))
            {
                request.IsCashOut = true;
                request.IsWallet = true;
                request.RechargeValue = row.Amount;
                request.Tet_NumPoket = Clean(row.Phone, row.IPN);
                return;
            }

            if (string.Equals(serviceType, "card", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(row.MatchedToken))
                {
                    throw new InvalidOperationException("صف الكارت لا يحتوي على توكن مطابق.");
                }

                if (string.IsNullOrWhiteSpace(row.Phone))
                {
                    throw new InvalidOperationException("لا يمكن إنشاء KYC للكارت بدون رقم هاتف من الشيت.");
                }

                var customer = _repository.LookupKeshniCardCustomer(row.MatchedToken, defaults.BranchId, true);
                if (customer == null)
                {
                    customer = _repository.SaveCashCustomer(new PosCashCustomerSaveRequest
                    {
                        Name = Clean(row.CustomerName, "عميل Excel"),
                        ArabicName0 = Clean(row.CustomerName, "عميل Excel"),
                        PhoneNo2 = row.Phone.Trim(),
                        PhoneNo = row.Phone.Trim(),
                        CardNo = row.MatchedToken.Trim(),
                        CardId = row.MatchedToken.Trim(),
                        CardSource = "Excel Import",
                        Tet_NumPoket = row.IPN,
                        OrderDate = row.TransactionDate ?? DateTime.Today,
                        EasyCashType = 0,
                        BranchId = defaults.BranchId,
                        UserId = defaults.UserId ?? importContext.UserId,
                        EmpId = defaults.EmpId ?? importContext.EmpId
                    });
                }

                request.IsPOS = true;
                request.RechargeValue = 0;
                request.TblCusCshId = customer.CustomerID;
                request.CashCustomerName = string.IsNullOrWhiteSpace(customer.CustomerName) ? request.CashCustomerName : customer.CustomerName;
                request.CashCustomerPhone = string.IsNullOrWhiteSpace(customer.Phone) ? request.CashCustomerPhone : customer.Phone;
                request.Phone2 = string.IsNullOrWhiteSpace(customer.Phone2) ? row.Phone : customer.Phone2;
                request.VisaNumber = string.IsNullOrWhiteSpace(customer.VisaNumber) ? row.MatchedToken : customer.VisaNumber;
                request.CardSerial = request.VisaNumber;
                request.Tet_NumPoket = row.IPN;
                return;
            }

            request.IsRecharg = true;
            request.IsCashOut = false;
            request.IsWallet = false;
            request.IsPOS = false;
            request.TrafficViolations = false;
            request.RechargeValue = row.Amount;
        }

        private static void NormalizeServiceFlags(PosSaveTransactionRequest request, string serviceType)
        {
            var type = (serviceType ?? string.Empty).Trim().ToLowerInvariant();

            request.TrafficViolations = false;
            request.IsCashOut = false;
            request.IsWallet = false;
            request.IsPOS = false;
            request.IsRecharg = false;
            request.HaveGuarantee = false;

            if (!string.Equals(type, "card", StringComparison.OrdinalIgnoreCase))
            {
                request.VisaNumber = null;
                request.CardSerial = null;
            }

            switch (type)
            {
                case "violations":
                    request.TrafficViolations = true;
                    request.RechargeValue = null;
                    request.PayType = request.ViolationPayType.HasValue ? request.ViolationPayType.Value : 1;
                    break;
                case "cash-out":
                    request.IsCashOut = true;
                    request.IsWallet = true;
                    break;
                case "card":
                    request.IsPOS = true;
                    request.RechargeValue = 0;
                    break;
                case "cash-in":
                default:
                    request.IsRecharg = true;
                    break;
            }
        }

        private void VerifyPersistedServiceType(int transactionId, string expectedServiceType, int? expectedItemId)
        {
            var expected = (expectedServiceType ?? string.Empty).Trim().ToLowerInvariant();
            var actual = (_repository.GetPosTransactionServiceType(transactionId) ?? string.Empty).Trim().ToLowerInvariant();
            var actualItemId = _repository.GetPosTransactionServiceItemId(transactionId);
            if (string.Equals(expected, actual, StringComparison.OrdinalIgnoreCase)
                && (!expectedItemId.HasValue || expectedItemId.GetValueOrDefault() <= 0 || actualItemId == expectedItemId))
            {
                return;
            }

            _repository.DeletePosExcelImportedTransactionForFailedImport(transactionId);
            throw new InvalidOperationException("تم إلغاء الفاتورة لأن نوع الخدمة/الصنف المحفوظ لا يطابق Excel. Excel=" + expected + " item=" + expectedItemId + " / Saved=" + actual + " item=" + actualItemId);
        }

        private static PosTransactionItemDto ToTransactionItem(PosItemLookupDto item, string serviceType)
        {
            var price = string.Equals(serviceType, "violations", StringComparison.OrdinalIgnoreCase) ? 50 : item.Price;
            return new PosTransactionItemDto
            {
                Item_ID = item.Item_ID,
                ItemName = item.ItemName,
                UnitId = item.UnitId.GetValueOrDefault(1),
                Quantity = 1,
                ShowQty = 1,
                QtyBySmalltUnit = item.QtyBySmalltUnit <= 0 ? 1 : item.QtyBySmalltUnit,
                Price = price,
                ShowPrice = price,
                TotalPrice = price,
                Vat = 0,
                Vatyo = 0,
                DiscountValue = 0,
                TotalDiscountPerLine = 0,
                StoreID2 = item.StoreID2,
                ItemCase = item.ItemCase <= 0 ? 1 : item.ItemCase,
                CostPrice = item.CostPrice,
                SavedItemType = string.Equals(serviceType, "violations", StringComparison.OrdinalIgnoreCase) ? 1 : item.SavedItemType
            };
        }

        private static bool IsImportantIpn(string serviceType)
        {
            return string.Equals(serviceType, "cash-in", StringComparison.OrdinalIgnoreCase)
                || string.Equals(serviceType, "card", StringComparison.OrdinalIgnoreCase);
        }

        private static string BuildSourceKey(PosExcelImportPreviewResult preview, PosExcelImportRowPreview row)
        {
            return string.Format(CultureInfo.InvariantCulture, "ExcelImport|{0}|{1}|{2}", preview.SourceFileHash, row.SheetName, row.RowNumber);
        }

        private static string Clean(string value, string fallback)
        {
            return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        }
    }
}
