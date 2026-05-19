using ExcelDataReader;
using MyERP.Areas.Pos.Data;
using MyERP.Areas.Pos.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace MyERP.Areas.Pos.Services
{
    public class PosKishnyTokenInvoiceImportService
    {
        private readonly PosSqlRepository _repository;

        public PosKishnyTokenInvoiceImportService(PosSqlRepository repository)
        {
            if (repository == null)
            {
                throw new ArgumentNullException("repository");
            }

            _repository = repository;
        }

        public PosKishnyTokenInvoiceImportPreview Preview(Stream stream, string fileName, DateTime? defaultImportDate, PosUserContext importContext, Action<int, int, string> progressCallback = null)
        {
            var preview = new PosKishnyTokenInvoiceImportPreview
            {
                SourceFileName = Path.GetFileName(fileName ?? "TokenImport.xlsx"),
                DefaultImportDate = defaultImportDate
            };

            ReportPreviewProgress(progressCallback, 0, 0, "جاري استقبال الملف...");
            if (stream == null)
            {
                ReportPreviewProgress(progressCallback, 0, 0, "لم يتم العثور على ملف للقراءة.");
                return preview;
            }

            byte[] bytes;
            using (var memory = new MemoryStream())
            {
                stream.CopyTo(memory);
                bytes = memory.ToArray();
            }

            ReportPreviewProgress(progressCallback, 0, 0, "تم رفع الملف، جاري فتح ملف Excel...");
            preview.SourceFileHash = ComputeSha256(bytes);
            var branches = _repository.GetBranches();
            var defaultsCache = new Dictionary<int, PosUserContext>();

            using (var reader = ExcelReaderFactory.CreateReader(new MemoryStream(bytes)))
            {
                var dataSet = reader.AsDataSet(new ExcelDataSetConfiguration
                {
                    ConfigureDataTable = _ => new ExcelDataTableConfiguration { UseHeaderRow = false }
                });

                if (dataSet == null)
                {
                    ReportPreviewProgress(progressCallback, 0, 0, "تعذر قراءة محتوى ملف Excel.");
                    return preview;
                }

                var totalRows = dataSet.Tables
                    .Cast<DataTable>()
                    .Where(x => x != null && x.Rows.Count > 0)
                    .Sum(x => x.Rows.Count);
                var processedRows = 0;
                ReportPreviewProgress(progressCallback, processedRows, totalRows, "تم فتح الملف، جاري تحليل الأعمدة والصفوف...");

                foreach (DataTable sheet in dataSet.Tables)
                {
                    if (sheet == null || sheet.Rows.Count == 0)
                    {
                        continue;
                    }

                    ReportPreviewProgress(progressCallback, processedRows, totalRows, "جاري قراءة شيت: " + sheet.TableName);
                    var mapping = DetectMapping(sheet);
                    preview.Mapping = mapping;
                    var firstDataRow = mapping.UsedHeaderRow ? 1 : 0;
                    for (var rowIndex = firstDataRow; rowIndex < sheet.Rows.Count; rowIndex++)
                    {
                        var row = BuildRow(sheet, rowIndex, mapping, branches, defaultImportDate, importContext, defaultsCache);
                        if (row != null)
                        {
                            preview.Rows.Add(row);
                        }

                        processedRows++;
                        ReportPreviewProgress(progressCallback, processedRows, totalRows, "جاري فحص الصف " + (rowIndex + 1).ToString(CultureInfo.InvariantCulture) + " من شيت " + sheet.TableName);
                    }
                }
            }

            ReportPreviewProgress(progressCallback, preview.Rows.Count, preview.Rows.Count, "جاري تحديد التكرارات داخل الملف...");
            MarkExcelDuplicates(preview.Rows);
            ReportPreviewProgress(progressCallback, preview.Rows.Count, preview.Rows.Count, "انتهت المعاينة.");
            return preview;
        }

        private static void ReportPreviewProgress(Action<int, int, string> progressCallback, int processedRows, int totalRows, string message)
        {
            if (progressCallback != null)
            {
                progressCallback(processedRows, totalRows, message);
            }
        }

        public PosKishnyTokenInvoiceImportResult Commit(PosKishnyTokenInvoiceImportPreview preview, PosUserContext importContext, Action<PosExcelImportCommitProgress> progressCallback = null)
        {
            var result = new PosKishnyTokenInvoiceImportResult();
            if (preview == null)
            {
                result.Status = "Failed";
                return result;
            }

            result.TotalRows = preview.Rows.Count;
            ReportCommitProgress(progressCallback, preview.Rows.Count, 0, 0, 0, 0, "بدأ ترحيل فواتير التوكنات", null);
            var batchBranchIds = preview.Rows
                .Where(x => x != null && x.BranchId.HasValue)
                .Select(x => x.BranchId.Value)
                .Distinct()
                .ToList();
            var batchBranchId = batchBranchIds.Count == 1 ? (int?)batchBranchIds[0] : null;
            result.BatchId = _repository.CreatePosExcelImportBatch(preview.SourceFileName, preview.SourceFileHash, importContext == null ? 0 : importContext.UserId, batchBranchId);
            var processedCount = 0;
            foreach (var row in preview.Rows)
            {
                var committed = Clone(row);
                try
                {
                    if (!IsReady(row))
                    {
                        committed.Status = "Failed";
                        committed.Messages.Add("الصف غير جاهز للترحيل.");
                        result.FailedRowsCount++;
                        InsertTokenInvoiceAuditRow(result.BatchId, committed);
                        result.Rows.Add(committed);
                        processedCount++;
                        ReportCommitProgress(progressCallback, preview.Rows.Count, processedCount, result.ImportedInvoicesCount, result.FailedRowsCount, result.DuplicateTokenCount, "تم رفض الصف لأنه غير جاهز للترحيل", committed);
                        continue;
                    }

                    var duplicate = _repository.FindKeshniCardInvoiceDuplicate(row.Token, null);
                    if (duplicate != null)
                    {
                        committed.Status = "Failed";
                        committed.InvoiceStatus = "DuplicateToken";
                        committed.Messages.Add("التوكن تم بيعه من قبل في فاتورة رقم " + duplicate.Transaction_ID.ToString(CultureInfo.InvariantCulture));
                        result.DuplicateTokenCount++;
                        result.FailedRowsCount++;
                        InsertTokenInvoiceAuditRow(result.BatchId, committed);
                        result.Rows.Add(committed);
                        processedCount++;
                        ReportCommitProgress(progressCallback, preview.Rows.Count, processedCount, result.ImportedInvoicesCount, result.FailedRowsCount, result.DuplicateTokenCount, "تم رفض الصف بسبب تكرار التوكن", committed);
                        continue;
                    }

                    var defaults = _repository.GetDefaultPosUserContextForBranch(row.BranchId.GetValueOrDefault());
                    if (defaults == null)
                    {
                        throw new InvalidOperationException("لا توجد إعدادات POS افتراضية للفرع.");
                    }

                    var customer = _repository.LookupKeshniCardCustomer(row.Token, row.BranchId, true)
                        ?? _repository.LookupKeshniCardCustomer(row.NationalId, row.BranchId, true)
                        ?? (!string.IsNullOrWhiteSpace(row.Mobile) ? _repository.LookupKeshniCardCustomer(row.Mobile, row.BranchId, true) : null);
                    var createdKyc = false;
                    if (customer == null)
                    {
                        customer = _repository.SaveKeshniCardCustomer(BuildKycRequest(row, defaults));
                        createdKyc = true;
                    }

                    var item = _repository.GetDefaultServiceItem("card", row.CardItemId, row.BranchId).FirstOrDefault();
                    if (item == null)
                    {
                        throw new InvalidOperationException("تعذر تحميل صنف الكارت المحدد.");
                    }

                    var request = BuildSaveRequest(preview, row, defaults, customer, item);
                    var saved = _repository.SaveTransaction(request);
                    if (saved == null || saved.Transaction_ID <= 0 || !_repository.TransactionExists(saved.Transaction_ID) || !_repository.TransactionHasDetails(saved.Transaction_ID))
                    {
                        throw new InvalidOperationException("تم استدعاء حفظ POS لكن لم يتم تأكيد الفاتورة بعد الحفظ.");
                    }

                    _repository.SyncSalesInvoiceVatFromDetails(saved.Transaction_ID);

                    committed.Status = "Imported";
                    committed.InvoiceStatus = "Imported";
                    committed.TransactionId = saved.Transaction_ID;
                    committed.InvoiceNumber = saved.NoteSerial1;
                    committed.KycCustomerId = customer.CustomerID;
                    committed.KycStatus = createdKyc ? "Created" : "Existing";
                    committed.Messages.Add("تم إنشاء فاتورة كارت كيشني بنفس مسار شاشة البيع.");
                    result.ImportedInvoicesCount++;
                    if (createdKyc) { result.CreatedKycCount++; } else { result.LinkedExistingKycCount++; }
                }
                catch (Exception ex)
                {
                    committed.Status = "Failed";
                    committed.InvoiceStatus = "Failed";
                    committed.Messages.Add(FriendlyMessage(ex));
                    result.FailedRowsCount++;
                }

                InsertTokenInvoiceAuditRow(result.BatchId, committed);
                result.Rows.Add(committed);
                processedCount++;
                ReportCommitProgress(progressCallback, preview.Rows.Count, processedCount, result.ImportedInvoicesCount, result.FailedRowsCount, result.DuplicateTokenCount, committed.Status == "Imported" ? "تم ترحيل فاتورة التوكن" : "فشل ترحيل الصف", committed);
            }

            result.Status = result.FailedRowsCount > 0 && result.ImportedInvoicesCount > 0
                ? "ImportedWithErrors"
                : (result.FailedRowsCount > 0 ? "Failed" : "Imported");
            _repository.UpdatePosExcelImportBatch(result.BatchId, result.Status, result.ImportedInvoicesCount, result.FailedRowsCount);
            ReportCommitProgress(progressCallback, preview.Rows.Count, preview.Rows.Count, result.ImportedInvoicesCount, result.FailedRowsCount, result.DuplicateTokenCount, "انتهى ترحيل فواتير التوكنات", null);
            return result;
        }

        private static void ReportCommitProgress(Action<PosExcelImportCommitProgress> progressCallback, int totalCount, int processedCount, int importedCount, int failedCount, int skippedCount, string message, PosKishnyTokenInvoiceImportRow row)
        {
            if (progressCallback == null)
            {
                return;
            }

            progressCallback(new PosExcelImportCommitProgress
            {
                TotalCount = totalCount,
                ProcessedCount = processedCount,
                ImportedCount = importedCount,
                FailedCount = failedCount,
                SkippedCount = skippedCount,
                CurrentSheet = row == null ? null : row.SheetName,
                CurrentRowNumber = row == null ? 0 : row.RowNumber,
                CurrentServiceType = row == null ? null : row.Token,
                CurrentMessage = message
            });
        }

        private PosKishnyTokenInvoiceImportRow BuildRow(DataTable sheet, int rowIndex, PosKishnyTokenInvoiceColumnMapping mapping, IList<PosBranchDto> branches, DateTime? defaultImportDate, PosUserContext importContext, IDictionary<int, PosUserContext> defaultsCache)
        {
            var row = new PosKishnyTokenInvoiceImportRow
            {
                SheetName = sheet.TableName,
                RowNumber = rowIndex + 1,
                Token = NormalizeCardToken(ReadCell(sheet, rowIndex, mapping.TokenColumn)),
                NationalId = DigitsOnly(ReadCell(sheet, rowIndex, mapping.NationalIdColumn)),
                Mobile = NormalizePhone(ReadCell(sheet, rowIndex, mapping.MobileColumn)),
                FullName = CleanSpaces(ReadCell(sheet, rowIndex, mapping.FullNameColumn)),
                BranchCode = CleanSpaces(ReadCell(sheet, rowIndex, mapping.BranchColumn)),
                InvoiceDateText = CleanSpaces(ReadCell(sheet, rowIndex, mapping.DateColumn))
            };
            if (ResolveBranch(row.BranchCode, branches) == null)
            {
                var scannedBranchCode = FindBranchCodeInRow(sheet, rowIndex, branches);
                if (!string.IsNullOrWhiteSpace(scannedBranchCode))
                {
                    row.BranchCode = scannedBranchCode;
                }
            }

            if (string.IsNullOrWhiteSpace(row.Token)
                && string.IsNullOrWhiteSpace(row.NationalId)
                && string.IsNullOrWhiteSpace(row.Mobile)
                && string.IsNullOrWhiteSpace(row.FullName)
                && string.IsNullOrWhiteSpace(row.BranchCode))
            {
                return null;
            }

            row.InvoiceDate = ParseDate(row.InvoiceDateText);
            if (!row.InvoiceDate.HasValue && defaultImportDate.HasValue)
            {
                row.InvoiceDate = defaultImportDate.Value.Date;
                row.Messages.Add("تم استخدام تاريخ الاستيراد الافتراضي لأن تاريخ الصف غير موجود.");
            }

            ValidateBasic(row, branches, importContext);
            if (row.BranchId.HasValue)
            {
                PosUserContext defaults;
                if (!defaultsCache.TryGetValue(row.BranchId.Value, out defaults))
                {
                    defaults = _repository.GetDefaultPosUserContextForBranch(row.BranchId.Value);
                    defaultsCache[row.BranchId.Value] = defaults;
                }

                if (defaults == null || !defaults.StoreId.HasValue || !defaults.BoxId.HasValue || !defaults.PaymentTypeId.HasValue || !defaults.EmpId.HasValue)
                {
                    Reject(row, "إعدادات POS الافتراضية للفرع غير مكتملة: المستخدم/المخزن/الخزنة/الدفع/المندوب.");
                }
                else
                {
                    row.DefaultUserId = defaults.UserId;
                    row.DefaultStoreId = defaults.StoreId;
                    row.DefaultBoxId = defaults.BoxId;
                    row.DefaultPaymentTypeId = defaults.PaymentTypeId;
                    ResolveCardItem(row, defaults.StoreId);
                    ResolveKycStatus(row);
                }
            }

            return row;
        }

        private void ResolveCardItem(PosKishnyTokenInvoiceImportRow row, int? storeId)
        {
            if (string.IsNullOrWhiteSpace(row.Token) || !storeId.HasValue)
            {
                return;
            }

            var duplicate = _repository.FindKeshniCardInvoiceDuplicate(row.Token, null);
            if (duplicate != null)
            {
                row.InvoiceStatus = "DuplicateToken";
                Reject(row, "التوكن تم بيعه من قبل في فاتورة رقم " + duplicate.Transaction_ID.ToString(CultureInfo.InvariantCulture));
                return;
            }

            var stock = _repository.GetKeshniCardTokenCurrentStockByItem(row.Token, storeId);
            var positive = stock.Where(x => x != null && x.ItemId.HasValue && x.AvailableQty > 0).ToList();
            if (positive.Count == 0)
            {
                row.InvoiceStatus = "DuplicateToken";
                Reject(row, "التوكن غير متاح كمخزون موجب في مخزن الفرع أو سبق استخدامه.");
                return;
            }

            if (positive.Count > 1)
            {
                Reject(row, "التوكن له رصيد موجب على أكثر من صنف كارت. يجب مراجعته يدويا.");
                return;
            }

            row.CardItemId = positive[0].ItemId;
            row.CardItemName = positive[0].ItemName;
            var item = _repository.GetDefaultServiceItem("card", row.CardItemId, row.BranchId).FirstOrDefault();
            if (item != null)
            {
                row.CardPrice = item.Price;
                row.CardItemName = item.ItemName;
            }
        }

        private void ResolveKycStatus(PosKishnyTokenInvoiceImportRow row)
        {
            var byToken = _repository.LookupKeshniCardCustomer(row.Token, row.BranchId, true);
            if (byToken != null)
            {
                row.KycStatus = "Existing";
                row.KycCustomerId = byToken.CustomerID;
                return;
            }

            var byNational = _repository.LookupKeshniCardCustomer(row.NationalId, row.BranchId, true);
            if (byNational != null && !string.IsNullOrWhiteSpace(byNational.CardNo) && !string.Equals(NormalizeCardToken(byNational.CardNo), row.Token, StringComparison.OrdinalIgnoreCase))
            {
                row.KycStatus = "Conflict";
                Reject(row, "الرقم القومي موجود على KYC آخر بتوكن مختلف. لن يتم تحديث بيانات حساسة تلقائيا.");
                return;
            }

            if (!string.IsNullOrWhiteSpace(row.Mobile))
            {
                var byMobile = _repository.LookupKeshniCardCustomer(row.Mobile, row.BranchId, true);
                if (byMobile != null && !string.IsNullOrWhiteSpace(byMobile.Tet_NumPoket) && !string.Equals(DigitsOnly(byMobile.Tet_NumPoket), row.NationalId, StringComparison.OrdinalIgnoreCase))
                {
                    row.KycStatus = "Conflict";
                    Reject(row, "رقم المحمول موجود مع رقم قومي مختلف. يجب مراجعته يدويا.");
                }
            }
        }

        private static void ValidateBasic(PosKishnyTokenInvoiceImportRow row, IList<PosBranchDto> branches, PosUserContext importContext)
        {
            if (string.IsNullOrWhiteSpace(row.Token)) { Reject(row, "التوكن مطلوب."); }
            if (string.IsNullOrWhiteSpace(row.NationalId) || row.NationalId.Length != 14) { Reject(row, "الرقم القومي مطلوب ويجب أن يكون 14 رقم."); }
            if (!row.InvoiceDate.HasValue) { row.InvoiceStatus = "InvalidDate"; Reject(row, "التاريخ غير صالح أو غير موجود ولم يتم تحديد تاريخ افتراضي."); }
            if (string.IsNullOrWhiteSpace(row.BranchCode)) { row.InvoiceStatus = "InvalidBranch"; Reject(row, "كود الفرع مطلوب."); return; }

            var branch = ResolveBranch(row.BranchCode, branches);
            if (branch == null)
            {
                row.InvoiceStatus = "InvalidBranch";
                Reject(row, "كود الفرع غير معروف: " + row.BranchCode);
                return;
            }

            row.BranchId = branch.BranchId;
            row.BranchName = branch.BranchName;
            if (importContext != null && !importContext.CanChangeDefaults && !importContext.IsFullAccess && importContext.BranchId.HasValue && importContext.BranchId.Value != branch.BranchId)
            {
                Reject(row, "ليس لديك صلاحية الاستيراد لهذا الفرع.");
            }
        }

        private static PosCashCustomerSaveRequest BuildKycRequest(PosKishnyTokenInvoiceImportRow row, PosUserContext defaults)
        {
            var name = string.IsNullOrWhiteSpace(row.FullName) ? "عميل كيشني" : row.FullName.Trim();
            var parts = SplitNameParts(name, 4);
            var invoiceDate = row.InvoiceDate ?? DateTime.Today;
            var birthDate = TryGetBirthDateFromEgyptianNationalId(row.NationalId);
            return new PosCashCustomerSaveRequest
            {
                Name = name,
                NameE = "Kishny Card Customer",
                ArabicName0 = parts[0],
                ArabicName1 = parts[1],
                ArabicName2 = parts[2],
                ArabicName3 = parts[3],
                EnglishName0 = "Kishny",
                EnglishName1 = "Card",
                EnglishName2 = "Customer",
                EnglishName3 = "Egypt",
                EnglishName5 = "Egypt",
                EnglishName6 = "Egypt",
                EnglishName7 = "Egypt",
                PhoneNo2 = row.Mobile,
                PhoneNo = row.Mobile,
                CardNo = row.Token,
                CardId = row.Token,
                CardSource = "Excel Token Invoice Import",
                Tet_NumPoket = row.NationalId,
                Address = "مصر",
                MailAdress = "Egypt",
                BirthDate = birthDate,
                OrderDate = invoiceDate,
                CardDate = invoiceDate,
                CardEndDate = invoiceDate.AddYears(5),
                Tel = row.Mobile,
                Card = row.Token,
                EasyCashType = 0,
                BranchId = row.BranchId,
                StoreId = defaults.StoreId,
                UserId = defaults.UserId,
                EmpId = defaults.EmpId
            };
        }

        private void InsertTokenInvoiceAuditRow(long batchId, PosKishnyTokenInvoiceImportRow row)
        {
            if (batchId <= 0 || row == null)
            {
                return;
            }

            var message = row.Messages == null
                ? null
                : string.Join(" | ", row.Messages.Where(x => !string.IsNullOrWhiteSpace(x)));
            _repository.InsertPosExcelImportBatchRow(batchId, row.SheetName, row.RowNumber, row.Token, row.Token, row.Status, row.TransactionId, message);
        }

        private static DateTime? TryGetBirthDateFromEgyptianNationalId(string nationalId)
        {
            var id = DigitsOnly(nationalId);
            if (string.IsNullOrWhiteSpace(id) || id.Length != 14)
            {
                return null;
            }

            var century = id[0] == '2' ? 1900 : (id[0] == '3' ? 2000 : 0);
            int year;
            int month;
            int day;
            if (century == 0
                || !int.TryParse(id.Substring(1, 2), NumberStyles.None, CultureInfo.InvariantCulture, out year)
                || !int.TryParse(id.Substring(3, 2), NumberStyles.None, CultureInfo.InvariantCulture, out month)
                || !int.TryParse(id.Substring(5, 2), NumberStyles.None, CultureInfo.InvariantCulture, out day))
            {
                return null;
            }

            try
            {
                return new DateTime(century + year, month, day);
            }
            catch (ArgumentOutOfRangeException)
            {
                return null;
            }
        }

        private static PosSaveTransactionRequest BuildSaveRequest(PosKishnyTokenInvoiceImportPreview preview, PosKishnyTokenInvoiceImportRow row, PosUserContext defaults, PosCustomerLookupDto customer, PosItemLookupDto item)
        {
            var total = item.Price;
            var sourceKey = string.Format(CultureInfo.InvariantCulture, "TokenInvoiceExcel|{0}|{1}|{2}", preview.SourceFileHash, row.SheetName, row.RowNumber);
            return new PosSaveTransactionRequest
            {
                TransactionType = "card",
                TransactionDate = (row.InvoiceDate ?? DateTime.Today).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                BranchId = row.BranchId,
                StoreID = defaults.StoreId,
                UserID = defaults.UserId,
                Emp_ID = defaults.EmpId,
                CustomerID = 2,
                DefaultCustomerId = 2,
                PaymentType = defaults.PaymentTypeId.GetValueOrDefault(),
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
                CashCustomerName = string.IsNullOrWhiteSpace(customer.CustomerName) ? row.FullName : customer.CustomerName,
                CashCustomerPhone = string.IsNullOrWhiteSpace(customer.Phone2) ? row.Mobile : customer.Phone2,
                Phone2 = string.IsNullOrWhiteSpace(customer.Phone2) ? row.Mobile : customer.Phone2,
                IPN = sourceKey,
                ManualNO = sourceKey,
                NoID = PosSqlRepository.WebInvoiceSourceMarker,
                ManualNo2 = sourceKey,
                RechargeType = "كارت كيشني",
                ItemIDService = item.Item_ID,
                IsPOS = true,
                RechargeValue = 0,
                TblCusCshId = customer.CustomerID,
                VisaNumber = row.Token,
                CardSerial = row.Token,
                Tet_NumPoket = row.NationalId,
                Items = new List<PosTransactionItemDto> { ToTransactionItem(item, row.Token) },
                SalesPayments = new List<PosSalesPaymentDto>
                {
                    new PosSalesPaymentDto
                    {
                        PaymentID = defaults.PaymentTypeId.GetValueOrDefault(),
                        PaymentName = defaults.PaymentName,
                        Value = total,
                        MaxValue = total
                    }
                }
            };
        }

        private static PosTransactionItemDto ToTransactionItem(PosItemLookupDto item, string token)
        {
            return new PosTransactionItemDto
            {
                Item_ID = item.Item_ID,
                ItemName = item.ItemName,
                UnitId = item.UnitId.GetValueOrDefault(1),
                Quantity = 1,
                ShowQty = 1,
                QtyBySmalltUnit = item.QtyBySmalltUnit <= 0 ? 1 : item.QtyBySmalltUnit,
                Price = item.Price,
                ShowPrice = item.Price,
                TotalPrice = item.Price,
                Vat = item.Vat,
                Vatyo = item.Vatyo,
                DiscountValue = 0,
                TotalDiscountPerLine = 0,
                StoreID2 = item.StoreID2,
                ItemCase = item.ItemCase <= 0 ? 1 : item.ItemCase,
                CostPrice = item.CostPrice,
                SavedItemType = item.SavedItemType
            };
        }

        private static PosKishnyTokenInvoiceColumnMapping DetectMapping(DataTable sheet)
        {
            var headerRow = FindHeaderRow(sheet);
            var mapping = new PosKishnyTokenInvoiceColumnMapping { UsedHeaderRow = headerRow == 0 };
            if (headerRow >= 0)
            {
                for (var c = 0; c < sheet.Columns.Count; c++)
                {
                    var header = NormalizeHeader(ReadCell(sheet, headerRow, c));
                    var columnName = c.ToString(CultureInfo.InvariantCulture);
                    if (IsTokenHeaderExtended(header)) { mapping.TokenColumn = columnName; }
                    else if (IsNationalHeader(header)) { mapping.NationalIdColumn = columnName; }
                    else if (IsMobileHeader(header)) { mapping.MobileColumn = columnName; }
                    else if (IsNameHeader(header)) { mapping.FullNameColumn = columnName; }
                    else if (IsBranchHeader(header)) { mapping.BranchColumn = columnName; }
                    else if (IsDateHeader(header)) { mapping.DateColumn = columnName; }
                }
            }

            InferMissingColumns(sheet, headerRow >= 0 ? 1 : 0, mapping);
            return mapping;
        }

        private static int FindHeaderRow(DataTable sheet)
        {
            var maxRows = Math.Min(5, sheet.Rows.Count);
            for (var r = 0; r < maxRows; r++)
            {
                var score = 0;
                for (var c = 0; c < sheet.Columns.Count; c++)
                {
                    var h = NormalizeHeader(ReadCell(sheet, r, c));
                    if (IsTokenHeaderExtended(h) || IsNationalHeader(h) || IsMobileHeader(h) || IsNameHeader(h) || IsBranchHeader(h) || IsDateHeader(h))
                    {
                        score++;
                    }
                }

                if (score >= 2)
                {
                    return r;
                }
            }

            return -1;
        }

        private static void InferMissingColumns(DataTable sheet, int startRow, PosKishnyTokenInvoiceColumnMapping mapping)
        {
            var scores = new Dictionary<string, int[]>
            {
                { "Token", new int[sheet.Columns.Count] },
                { "National", new int[sheet.Columns.Count] },
                { "Mobile", new int[sheet.Columns.Count] },
                { "Branch", new int[sheet.Columns.Count] },
                { "Date", new int[sheet.Columns.Count] }
            };

            for (var r = startRow; r < Math.Min(sheet.Rows.Count, startRow + 40); r++)
            {
                for (var c = 0; c < sheet.Columns.Count; c++)
                {
                    var text = ReadCell(sheet, r, c);
                    var tokenText = NormalizeCardToken(text);
                    var digits = DigitsOnly(text);
                    var isDateValue = ParseDate(text).HasValue;
                    if (!isDateValue && IsLikelyTokenValue(tokenText)) { scores["Token"][c]++; }
                    if (digits.Length == 14) { scores["National"][c] += 3; }
                    if (NormalizePhone(text).Length == 11) { scores["Mobile"][c] += 3; }
                    if (Regex.IsMatch(text ?? string.Empty, @"[A-Za-z]{1,8}\s*\d{1,8}")) { scores["Branch"][c] += 3; }
                    if (isDateValue) { scores["Date"][c] += 2; }
                }
            }

            if (string.IsNullOrWhiteSpace(mapping.TokenColumn)) { mapping.TokenColumn = Best(scores["Token"]); }
            if (string.IsNullOrWhiteSpace(mapping.NationalIdColumn)) { mapping.NationalIdColumn = Best(scores["National"], mapping.TokenColumn); }
            if (string.IsNullOrWhiteSpace(mapping.MobileColumn)) { mapping.MobileColumn = Best(scores["Mobile"], mapping.TokenColumn, mapping.NationalIdColumn); }
            if (string.IsNullOrWhiteSpace(mapping.BranchColumn)) { mapping.BranchColumn = Best(scores["Branch"]); }
            if (string.IsNullOrWhiteSpace(mapping.DateColumn)) { mapping.DateColumn = Best(scores["Date"]); }
            if (string.IsNullOrWhiteSpace(mapping.FullNameColumn))
            {
                for (var c = 0; c < sheet.Columns.Count; c++)
                {
                    var key = c.ToString(CultureInfo.InvariantCulture);
                    if (key != mapping.TokenColumn && key != mapping.NationalIdColumn && key != mapping.MobileColumn && key != mapping.BranchColumn && key != mapping.DateColumn)
                    {
                        mapping.FullNameColumn = key;
                        break;
                    }
                }
            }
        }

        private static string Best(int[] scores, params string[] excluded)
        {
            var excludedSet = new HashSet<int>((excluded ?? new string[0]).Select(ParseColumn).Where(x => x >= 0));
            var bestIndex = -1;
            var bestScore = 0;
            for (var i = 0; i < scores.Length; i++)
            {
                if (excludedSet.Contains(i)) { continue; }
                if (scores[i] > bestScore)
                {
                    bestScore = scores[i];
                    bestIndex = i;
                }
            }

            return bestIndex < 0 ? null : bestIndex.ToString(CultureInfo.InvariantCulture);
        }

        private static string ReadCell(DataTable sheet, int rowIndex, string columnName)
        {
            return ReadCell(sheet, rowIndex, ParseColumn(columnName));
        }

        private static string ReadCell(DataTable sheet, int rowIndex, int columnIndex)
        {
            if (sheet == null || rowIndex < 0 || columnIndex < 0 || rowIndex >= sheet.Rows.Count || columnIndex >= sheet.Columns.Count)
            {
                return string.Empty;
            }

            var value = sheet.Rows[rowIndex][columnIndex];
            if (value == null || value == DBNull.Value) { return string.Empty; }
            if (value is DateTime) { return ((DateTime)value).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture); }
            return Convert.ToString(value, CultureInfo.InvariantCulture).Trim();
        }

        private static int ParseColumn(string value)
        {
            int index;
            return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out index) ? index : -1;
        }

        private static PosBranchDto ResolveBranch(string branchCode, IList<PosBranchDto> branches)
        {
            var code = NormalizeEnglish(branchCode);
            return string.IsNullOrWhiteSpace(code) || branches == null
                ? null
                : branches.FirstOrDefault(x => string.Equals(NormalizeEnglish(x.BranchCode), code, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(NormalizeEnglish(x.BranchName), code, StringComparison.OrdinalIgnoreCase));
        }

        private static string FindBranchCodeInRow(DataTable sheet, int rowIndex, IList<PosBranchDto> branches)
        {
            if (sheet == null || branches == null || rowIndex < 0 || rowIndex >= sheet.Rows.Count)
            {
                return null;
            }

            for (var columnIndex = 0; columnIndex < sheet.Columns.Count; columnIndex++)
            {
                var text = ReadCell(sheet, rowIndex, columnIndex);
                var branch = ResolveBranch(text, branches);
                if (branch != null)
                {
                    return branch.BranchCode;
                }
            }

            return null;
        }

        private static void MarkExcelDuplicates(IList<PosKishnyTokenInvoiceImportRow> rows)
        {
            var duplicates = (rows ?? new List<PosKishnyTokenInvoiceImportRow>())
                .Where(x => !string.IsNullOrWhiteSpace(x.Token))
                .GroupBy(x => x.Token, StringComparer.OrdinalIgnoreCase)
                .Where(x => x.Count() > 1)
                .Select(x => x.Key)
                .ToList();
            foreach (var row in rows.Where(x => duplicates.Contains(x.Token, StringComparer.OrdinalIgnoreCase)))
            {
                Reject(row, "التوكن مكرر داخل نفس ملف Excel.");
            }
        }

        private static bool IsReady(PosKishnyTokenInvoiceImportRow row)
        {
            return row != null && string.Equals(row.Status, "Ready", StringComparison.OrdinalIgnoreCase) && row.CardItemId.HasValue && row.BranchId.HasValue && row.InvoiceDate.HasValue;
        }

        private static void Reject(PosKishnyTokenInvoiceImportRow row, string message)
        {
            row.Status = "Rejected";
            if (string.Equals(row.InvoiceStatus, "Ready", StringComparison.OrdinalIgnoreCase)) { row.InvoiceStatus = "Rejected"; }
            if (!string.IsNullOrWhiteSpace(message)) { row.Messages.Add(message); }
        }

        private static PosKishnyTokenInvoiceImportRow Clone(PosKishnyTokenInvoiceImportRow row)
        {
            var clone = new PosKishnyTokenInvoiceImportRow
            {
                SheetName = row.SheetName,
                RowNumber = row.RowNumber,
                Token = row.Token,
                NationalId = row.NationalId,
                Mobile = row.Mobile,
                FullName = row.FullName,
                BranchCode = row.BranchCode,
                BranchId = row.BranchId,
                BranchName = row.BranchName,
                InvoiceDate = row.InvoiceDate,
                InvoiceDateText = row.InvoiceDateText,
                DefaultUserId = row.DefaultUserId,
                DefaultStoreId = row.DefaultStoreId,
                DefaultBoxId = row.DefaultBoxId,
                DefaultPaymentTypeId = row.DefaultPaymentTypeId,
                CardItemId = row.CardItemId,
                CardItemName = row.CardItemName,
                CardPrice = row.CardPrice,
                KycStatus = row.KycStatus,
                KycCustomerId = row.KycCustomerId,
                InvoiceStatus = row.InvoiceStatus,
                Status = row.Status,
                TransactionId = row.TransactionId,
                InvoiceNumber = row.InvoiceNumber
            };
            foreach (var message in row.Messages) { clone.Messages.Add(message); }
            return clone;
        }

        private static DateTime? ParseDate(string value)
        {
            value = NormalizeArabicDigits((value ?? string.Empty).Trim());
            if (string.IsNullOrWhiteSpace(value)) { return null; }
            double serial;
            if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out serial) && serial > 20000 && serial < 90000)
            {
                return DateTime.FromOADate(serial).Date;
            }

            DateTime parsed;
            var formats = new[] { "dd.MM.yyyy", "d.M.yyyy", "dd/MM/yyyy", "d/M/yyyy", "yyyy-MM-dd", "yyyy/MM/dd", "dd-MM-yyyy", "d-M-yyyy" };
            return DateTime.TryParseExact(value, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed)
                || DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed)
                ? (DateTime?)parsed.Date
                : null;
        }

        private static string NormalizePhone(string value)
        {
            var digits = DigitsOnly(NormalizeArabicDigits(value));
            if (digits.StartsWith("20") && digits.Length == 12) { digits = "0" + digits.Substring(2); }
            if (digits.Length == 10 && digits.StartsWith("1")) { digits = "0" + digits; }
            return digits;
        }

        private static string DigitsOnly(string value)
        {
            return Regex.Replace(NormalizeArabicDigits(value) ?? string.Empty, @"\D+", string.Empty);
        }

        private static string NormalizeCardToken(string value)
        {
            value = NormalizeArabicDigits(value);
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            return Regex.Replace(value.Trim(), @"[^0-9A-Za-z]+", string.Empty).ToUpperInvariant();
        }

        private static bool IsLikelyTokenValue(string tokenText)
        {
            if (string.IsNullOrWhiteSpace(tokenText))
            {
                return false;
            }

            if (tokenText.Length < 6 || tokenText.Length > 24)
            {
                return false;
            }

            return Regex.IsMatch(tokenText, @"[A-Za-z]")
                || tokenText.Length == 8
                || tokenText.Length == 18;
        }

        private static string NormalizeArabicDigits(string value)
        {
            if (string.IsNullOrEmpty(value)) { return string.Empty; }
            var chars = value.ToCharArray();
            for (var i = 0; i < chars.Length; i++)
            {
                if (chars[i] >= '\u0660' && chars[i] <= '\u0669') { chars[i] = (char)('0' + (chars[i] - '\u0660')); }
                else if (chars[i] >= '\u06F0' && chars[i] <= '\u06F9') { chars[i] = (char)('0' + (chars[i] - '\u06F0')); }
            }
            return new string(chars);
        }

        private static string NormalizeHeader(string value)
        {
            value = NormalizeArabicDigits(value ?? string.Empty).Trim().ToLowerInvariant();
            value = value.Replace("أ", "ا").Replace("إ", "ا").Replace("آ", "ا").Replace("ى", "ي").Replace("ة", "ه");
            return Regex.Replace(value, @"[\s\(\)\/\\_\-\.:]+", string.Empty);
        }

        private static string NormalizeEnglish(string value)
        {
            return Regex.Replace((value ?? string.Empty).Trim().ToLowerInvariant(), @"[\s\-_()]+", string.Empty);
        }

        private static string CleanSpaces(string value)
        {
            return Regex.Replace(value ?? string.Empty, @"\s+", " ").Trim();
        }

        private static bool IsTokenHeader(string h) { return h == "token" || h.Contains("token") || h.Contains("توكن") || h.Contains("الكارت") || h.Contains("رقمالكارت"); }
        private static bool IsNationalHeader(string h) { return h.Contains("nationalid") || h.Contains("national") || h.Contains("قومي") || h.Contains("الرقمالقومي"); }
        private static bool IsMobileHeader(string h) { return h.Contains("mobile") || h.Contains("phone") || h.Contains("موبايل") || h.Contains("الهاتف") || h.Contains("رقمالهاتف"); }
        private static bool IsNameHeader(string h) { return h.Contains("fullname") || h == "name" || h.Contains("اسم") || h.Contains("الاسم"); }
        private static bool IsBranchHeader(string h) { return h.Contains("branchcode") || h.Contains("كودالفرع") || (h.Contains("branch") && h.Contains("code")); }
        private static bool IsDateHeader(string h) { return h == "date" || h.Contains("التاريخ") || h.Contains("invoicedate"); }

        private static bool IsTokenHeaderExtended(string h)
        {
            return IsTokenHeader(h)
                || h.Contains("barcode")
                || h.Contains("scan")
                || h.Contains("\u062a\u0648\u0643\u064a\u0646")
                || h.Contains("\u0627\u0644\u062a\u0648\u0643\u064a\u0646")
                || h.Contains("\u0628\u0627\u0631\u0643\u0648\u062f")
                || h.Contains("\u0627\u0644\u0628\u0627\u0631\u0643\u0648\u062f")
                || h.Contains("\u0633\u0643\u0627\u0646");
        }

        private static string[] SplitNameParts(string value, int count)
        {
            var result = new string[count];
            var parts = Regex.Split((value ?? string.Empty).Trim(), @"\s+").Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
            for (var i = 0; i < count; i++) { result[i] = i < parts.Count ? parts[i] : string.Empty; }
            if (parts.Count > count) { result[count - 1] = string.Join(" ", parts.Skip(count - 1)); }
            return result;
        }

        private static string ComputeSha256(byte[] bytes)
        {
            using (var sha = SHA256.Create())
            {
                return BitConverter.ToString(sha.ComputeHash(bytes ?? new byte[0])).Replace("-", string.Empty);
            }
        }

        private static string FriendlyMessage(Exception ex)
        {
            return ex == null || string.IsNullOrWhiteSpace(ex.Message) ? "تعذر ترحيل الصف." : ex.Message;
        }
    }
}
