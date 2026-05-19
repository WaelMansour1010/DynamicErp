using ExcelDataReader;
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
    public class PosExcelImportParser
    {
        private const int HeaderRowIndex = 2;
        private const int FirstDataRowIndex = 3;
        private const int ImportStatusColumnIndex = 13;

        public PosExcelImportPreviewResult Parse(Stream stream, string fileName, PosExcelImportMappingDraft mapping)
        {
            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }

            var buffer = ReadAllBytes(stream);
            var result = new PosExcelImportPreviewResult
            {
                SourceFileName = Path.GetFileName(fileName ?? string.Empty),
                SourceFileHash = ComputeSha256(buffer),
                WorkbookType = "OperationalDailyTransactions",
                DetectedBranchHint = DetectBranchHint(fileName)
            };

            using (var workbookStream = new MemoryStream(buffer))
            using (var reader = ExcelReaderFactory.CreateReader(workbookStream))
            {
                var dataSet = reader.AsDataSet(new ExcelDataSetConfiguration
                {
                    ConfigureDataTable = _ => new ExcelDataTableConfiguration
                    {
                        UseHeaderRow = false
                    }
                });

                var sheetCount = dataSet.Tables.Count;
                foreach (DataTable sheet in dataSet.Tables)
                {
                    CollectBranchHints(sheet, result);

                    if (!ShouldParseSheet(sheet, sheetCount))
                    {
                        continue;
                    }

                    ParseDailySheet(sheet, result, mapping);
                }
            }

            if (result.WorkbookBranchHints.Count > 0)
            {
                result.DetectedBranchHint = string.Join(" | ", result.WorkbookBranchHints.Take(4));
            }

            MarkDuplicateTokens(result);
            ApplySequentialTokenMatching(result);
            FinalizeCounts(result);
            return result;
        }

        private static void ParseDailySheet(DataTable sheet, PosExcelImportPreviewResult result, PosExcelImportMappingDraft mapping)
        {
            if (!LooksLikeOperationalSheet(sheet))
            {
                result.Warnings.Add("تم تجاهل الشيت " + sheet.TableName + " لأن رؤوس الأعمدة لا تطابق بنية معاملات التشغيل.");
                return;
            }

            var sheetSummary = new PosExcelImportSheetSummary
            {
                SheetName = sheet.TableName,
                SheetDateText = ReadText(sheet, HeaderRowIndex, 12)
            };

            var lastRow = FindLastMeaningfulDataRow(sheet);
            for (var rowIndex = FirstDataRowIndex; rowIndex <= lastRow; rowIndex++)
            {
                var row = ParseTransactionRow(sheet, rowIndex, mapping);
                if (row != null)
                {
                    result.Rows.Add(row);
                    sheetSummary.TransactionRows++;
                    sheetSummary.SheetTotal += row.GrossTotal.GetValueOrDefault();
                }

                var token = ParseTokenRow(sheet, rowIndex);
                if (token != null)
                {
                    result.Tokens.Add(token);
                    sheetSummary.TokenRows++;
                }
            }

            if (sheetSummary.TransactionRows > 0 || sheetSummary.TokenRows > 0)
            {
                result.Sheets.Add(sheetSummary);
            }
        }

        private static int FindLastMeaningfulDataRow(DataTable sheet)
        {
            if (sheet == null || sheet.Rows.Count <= FirstDataRowIndex)
            {
                return FirstDataRowIndex - 1;
            }

            for (var rowIndex = sheet.Rows.Count - 1; rowIndex >= FirstDataRowIndex; rowIndex--)
            {
                if (HasMeaningfulDataRow(sheet, rowIndex))
                {
                    return rowIndex;
                }
            }

            return FirstDataRowIndex - 1;
        }

        private static bool HasMeaningfulDataRow(DataTable sheet, int rowIndex)
        {
            return !string.IsNullOrWhiteSpace(ReadText(sheet, rowIndex, 1))
                || !string.IsNullOrWhiteSpace(ReadText(sheet, rowIndex, 2))
                || !string.IsNullOrWhiteSpace(ReadText(sheet, rowIndex, 3))
                || !string.IsNullOrWhiteSpace(ReadText(sheet, rowIndex, 4))
                || !string.IsNullOrWhiteSpace(ReadText(sheet, rowIndex, 5))
                || !string.IsNullOrWhiteSpace(ReadText(sheet, rowIndex, 8))
                || !string.IsNullOrWhiteSpace(ReadText(sheet, rowIndex, 9))
                || !string.IsNullOrWhiteSpace(ReadText(sheet, rowIndex, 12));
        }

        private static PosExcelImportRowPreview ParseTransactionRow(DataTable sheet, int rowIndex, PosExcelImportMappingDraft mapping)
        {
            var ipn = ReadText(sheet, rowIndex, 2);
            var customer = ReadText(sheet, rowIndex, 3);
            var phone = NormalizeExcelPhone(ReadText(sheet, rowIndex, 4));
            var amountText = ReadText(sheet, rowIndex, 5);
            var serviceType = ReadText(sheet, rowIndex, 8);

            if (string.IsNullOrWhiteSpace(ipn)
                && string.IsNullOrWhiteSpace(customer)
                && string.IsNullOrWhiteSpace(phone)
                && string.IsNullOrWhiteSpace(amountText)
                && string.IsNullOrWhiteSpace(serviceType))
            {
                return null;
            }

            var row = new PosExcelImportRowPreview
            {
                SheetName = sheet.TableName,
                RowNumber = rowIndex + 1,
                SequenceNo = ReadText(sheet, rowIndex, 1),
                IPN = ipn,
                CustomerName = customer,
                Phone = phone,
                Amount = ReadDecimal(sheet, rowIndex, 5),
                Fee = ReadDecimal(sheet, rowIndex, 6),
                GrossTotal = ReadDecimal(sheet, rowIndex, 7),
                ServiceType = serviceType,
                TransactionDateText = ReadText(sheet, rowIndex, 9),
                TransactionDate = ReadDate(sheet, rowIndex, 9)
            };
            ApplyViolationDefaultPricing(row);
            ApplyExistingImportMarker(sheet, rowIndex, row);
            ValidateTransactionRow(row, mapping);
            return row;
        }

        private static void ApplyExistingImportMarker(DataTable sheet, int rowIndex, PosExcelImportRowPreview row)
        {
            var marker = ReadText(sheet, rowIndex, ImportStatusColumnIndex);
            if (string.IsNullOrWhiteSpace(marker))
            {
                return;
            }

            var normalized = NormalizeServiceText(marker);
            if (normalized.Contains("imported") || normalized.Contains("مرحله") || normalized.Contains("تم ترحيله") || normalized.Contains("تم الترحيل"))
            {
                row.Status = "ImportedBefore";
                row.Reasons.Add("الصف معلّم في ملف Excel بأنه تم ترحيله سابقا؛ سيتم تجاهله.");
            }
        }

        private static PosExcelImportTokenPreview ParseTokenRow(DataTable sheet, int rowIndex)
        {
            var token = ReadText(sheet, rowIndex, 12);
            if (string.IsNullOrWhiteSpace(token) || token.Equals("التوكن", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            if (token.Length < 8 || !token.Any(char.IsLetter))
            {
                return null;
            }

            return new PosExcelImportTokenPreview
            {
                SheetName = sheet.TableName,
                RowNumber = rowIndex + 1,
                Token = token
            };
        }

        private static void ValidateTransactionRow(PosExcelImportRowPreview row, PosExcelImportMappingDraft mapping)
        {
            if (row.TransactionDate == null)
            {
                Reject(row, "تاريخ العملية غير موجود أو غير صالح.");
            }

            if (string.IsNullOrWhiteSpace(row.ServiceType))
            {
                Reject(row, "نوع الخدمة مفقود.");
            }

            if (string.IsNullOrWhiteSpace(row.IPN))
            {
                Warn(row, "رقم IPN مفقود؛ سيتم منع commit حتى توجد وسيلة تتبع بديلة.");
            }

            var isViolation = IsViolationRow(row);

            if (!row.Amount.HasValue || row.Amount.Value <= 0)
            {
                Reject(row, "قيمة الشحنة غير صالحة.");
            }

            if (row.Fee.HasValue && row.Fee.Value < 0)
            {
                Reject(row, "قيمة الرسوم لا يمكن أن تكون سالبة.");
            }

            if (!row.GrossTotal.HasValue || row.GrossTotal.Value <= 0)
            {
                Reject(row, "الإجمالي غير صالح.");
            }

            if (!isViolation && row.Amount.HasValue && row.Fee.HasValue && row.GrossTotal.HasValue)
            {
                var expected = row.Amount.Value + row.Fee.Value;
                if (Math.Abs(expected - row.GrossTotal.Value) > 0.02m)
                {
                    Reject(row, "الإجمالي لا يساوي قيمة الشحنة + الرسوم.");
                }
            }
        }

        private static void MarkDuplicateIpns(PosExcelImportPreviewResult result)
        {
            var duplicates = result.Rows
                .Where(x => !string.IsNullOrWhiteSpace(x.IPN) && !string.Equals(x.Status, "ImportedBefore", StringComparison.OrdinalIgnoreCase))
                .GroupBy(x => NormalizeKey(x.IPN))
                .Where(x => x.Count() > 1)
                .Select(x => x.Key)
                .ToList();

            if (duplicates.Count == 0)
            {
                return;
            }

            foreach (var row in result.Rows.Where(x => !string.Equals(x.Status, "ImportedBefore", StringComparison.OrdinalIgnoreCase) && duplicates.Contains(NormalizeKey(x.IPN))))
            {
                Reject(row, "رقم IPN مكرر داخل نفس الملف.");
            }
        }

        private static void MarkDuplicateTokens(PosExcelImportPreviewResult result)
        {
            var duplicates = result.Tokens
                .GroupBy(x => NormalizeKey(x.Token))
                .Where(x => x.Count() > 1)
                .Select(x => x.Key)
                .ToList();

            if (duplicates.Count == 0)
            {
                return;
            }

            foreach (var token in result.Tokens.Where(x => duplicates.Contains(NormalizeKey(x.Token))))
            {
                token.Status = "Rejected";
                token.Reasons.Add("التوكن مكرر داخل نفس الملف.");
            }
        }

        private static void ApplySequentialTokenMatching(PosExcelImportPreviewResult result)
        {
            var eligibleRowsBySheet = result.Rows
                .Where(IsTokenEligibleRow)
                .GroupBy(x => x.SheetName)
                .ToDictionary(x => x.Key, x => x.OrderBy(r => r.RowNumber).ToList(), StringComparer.OrdinalIgnoreCase);

            foreach (var tokenGroup in result.Tokens.GroupBy(x => x.SheetName))
            {
                var rows = eligibleRowsBySheet.ContainsKey(tokenGroup.Key)
                    ? eligibleRowsBySheet[tokenGroup.Key]
                    : new List<PosExcelImportRowPreview>();
                var tokens = tokenGroup.OrderBy(x => x.RowNumber).ToList();
                var pairCount = Math.Min(rows.Count, tokens.Count);

                for (var i = 0; i < pairCount; i++)
                {
                    tokens[i].Status = "Matched";
                    result.TokenMatches.Add(new PosExcelImportTokenMatchPreview
                    {
                        SheetName = tokenGroup.Key,
                        SourceRowNumber = rows[i].RowNumber,
                        IPN = rows[i].IPN,
                        CustomerName = rows[i].CustomerName,
                        GrossTotal = rows[i].GrossTotal,
                        TokenRowNumber = tokens[i].RowNumber,
                        Token = tokens[i].Token,
                        MatchStatus = "Matched",
                        Strategy = "Sequential"
                    });
                    rows[i].MatchedToken = tokens[i].Token;
                }

                foreach (var row in rows.Skip(pairCount))
                {
                    Warn(row, "صف مؤهل لتوكن لكن لا يوجد توكن مقابل له باستراتيجية المطابقة المتسلسلة.");
                }

                foreach (var token in tokens.Skip(pairCount))
                {
                    token.Status = "Unmatched";
                    token.Reasons.Add("توكن بلا صف عملية مقابل في نفس الشيت.");
                }
            }
        }

        private static bool IsTokenEligibleRow(PosExcelImportRowPreview row)
        {
            if (row == null
                || string.Equals(row.Status, "Rejected", StringComparison.OrdinalIgnoreCase)
                || string.Equals(row.Status, "ImportedBefore", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var serviceText = NormalizeServiceText(row.ServiceType);
            return IsKeshniCardServiceText(serviceText);
        }

        private static void ApplyViolationDefaultPricing(PosExcelImportRowPreview row)
        {
            if (!IsViolationRow(row))
            {
                return;
            }

            var usedDefault = false;
            if (!row.Amount.HasValue || row.Amount.Value <= 0)
            {
                row.Amount = 50m;
                usedDefault = true;
            }

            if (!row.Fee.HasValue || row.Fee.Value <= 0)
            {
                row.Fee = 50m;
                usedDefault = true;
            }

            if (!row.GrossTotal.HasValue || row.GrossTotal.Value <= 0)
            {
                row.GrossTotal = 50m;
                usedDefault = true;
            }

            if (usedDefault)
            {
                Warn(row, "تم استخدام قيمة 50 جنيه للمخالفات في السعر/الإجمالي حسب فواتير المخالفات السابقة.");
            }
        }

        private static bool IsViolationRow(PosExcelImportRowPreview row)
        {
            return row != null && NormalizeServiceText(row.ServiceType).Contains("مخالف");
        }

        private static bool IsKeshniCardServiceText(string normalizedServiceText)
        {
            if (string.IsNullOrWhiteSpace(normalizedServiceText))
            {
                return false;
            }

            return normalizedServiceText.Contains("كارت")
                && normalizedServiceText.Contains("كيشني")
                && !normalizedServiceText.Contains("شحن");
        }

        private static string NormalizeServiceText(string value)
        {
            return (value ?? string.Empty).Trim()
                .Replace("ـ", string.Empty)
                .Replace("أ", "ا")
                .Replace("إ", "ا")
                .Replace("آ", "ا")
                .Replace("ى", "ي")
                .ToLowerInvariant();
        }

        private static void ApplyMappingPreflight(PosExcelImportPreviewResult result, PosExcelImportMappingDraft mapping)
        {
            mapping = mapping ?? new PosExcelImportMappingDraft();

            result.PreflightItems.Add(new PosExcelImportPreflightItem
            {
                FieldName = "Branch",
                Status = mapping.BranchId.HasValue ? "Mapped" : "Required",
                Value = mapping.BranchId.HasValue ? mapping.BranchId.Value.ToString(CultureInfo.InvariantCulture) : result.DetectedBranchHint,
                Message = mapping.BranchId.HasValue ? "تم تحديد الفرع من إعدادات المعاينة." : "الفرع غير موجود في Excel ويحتاج mapping من اسم الملف أو إعدادات الاستيراد."
            });

            result.PreflightItems.Add(new PosExcelImportPreflightItem
            {
                FieldName = "Store",
                Status = mapping.StoreId.HasValue ? "Mapped" : "Required",
                Value = mapping.StoreId.HasValue ? mapping.StoreId.Value.ToString(CultureInfo.InvariantCulture) : "",
                Message = mapping.StoreId.HasValue ? "تم تحديد المخزن." : "المخزن غير موجود في Excel ويجب تحديده قبل commit."
            });

            result.PreflightItems.Add(new PosExcelImportPreflightItem
            {
                FieldName = "PaymentType",
                Status = mapping.PaymentTypeId.HasValue ? "Mapped" : "DefaultNeeded",
                Value = mapping.PaymentTypeId.HasValue ? mapping.PaymentTypeId.Value.ToString(CultureInfo.InvariantCulture) : "",
                Message = mapping.PaymentTypeId.HasValue ? "تم تحديد طريقة الدفع." : "طريقة الدفع غير موجودة في Excel؛ يجب استخدام Default من مستخدم POS أو mapping صريح."
            });

            result.PreflightItems.Add(new PosExcelImportPreflightItem
            {
                FieldName = "ServiceItem: كاش ان",
                Status = mapping.ServiceItemMap.ContainsKey("كاش ان") && mapping.ServiceItemMap["كاش ان"].HasValue ? "Mapped" : "Required",
                Value = mapping.ServiceItemMap.ContainsKey("كاش ان") && mapping.ServiceItemMap["كاش ان"].HasValue ? mapping.ServiceItemMap["كاش ان"].Value.ToString(CultureInfo.InvariantCulture) : "",
                Message = "نوع الخدمة يجب ربطه بصنف خدمة POS قبل تنفيذ الحفظ."
            });
        }

        private static bool TryResolveServiceItem(string serviceType, PosExcelImportMappingDraft mapping, out int? itemId)
        {
            itemId = null;
            if (mapping == null || mapping.ServiceItemMap == null || string.IsNullOrWhiteSpace(serviceType))
            {
                return false;
            }

            var key = serviceType.Trim();
            if (!mapping.ServiceItemMap.ContainsKey(key) || !mapping.ServiceItemMap[key].HasValue || mapping.ServiceItemMap[key].Value <= 0)
            {
                return false;
            }

            itemId = mapping.ServiceItemMap[key];
            return true;
        }

        private static void FinalizeCounts(PosExcelImportPreviewResult result)
        {
            result.ReadyCount = result.Rows.Count(x => string.Equals(x.Status, "Ready", StringComparison.OrdinalIgnoreCase));
            result.WarningCount = result.Rows.Count(x => string.Equals(x.Status, "Warning", StringComparison.OrdinalIgnoreCase));
            result.RejectedCount = result.Rows.Count(x => string.Equals(x.Status, "Rejected", StringComparison.OrdinalIgnoreCase));
            result.UnmatchedTokenCount = result.Tokens.Count(x => !string.Equals(x.Status, "Matched", StringComparison.OrdinalIgnoreCase));
            result.TotalAmount = result.Rows.Where(x => !string.Equals(x.Status, "Rejected", StringComparison.OrdinalIgnoreCase)).Sum(x => x.Amount.GetValueOrDefault());
            result.TotalFees = result.Rows.Where(x => !string.Equals(x.Status, "Rejected", StringComparison.OrdinalIgnoreCase)).Sum(x => x.Fee.GetValueOrDefault());
            result.TotalGross = result.Rows.Where(x => !string.Equals(x.Status, "Rejected", StringComparison.OrdinalIgnoreCase)).Sum(x => x.GrossTotal.GetValueOrDefault());
        }

        private static bool LooksLikeOperationalSheet(DataTable sheet)
        {
            return ReadText(sheet, HeaderRowIndex, 1) == "م"
                && ReadText(sheet, HeaderRowIndex, 2).Equals("IPN", StringComparison.OrdinalIgnoreCase)
                && ReadText(sheet, HeaderRowIndex, 3).Contains("العميل")
                && ReadText(sheet, HeaderRowIndex, 8).Contains("الخدمة");
        }

        private static bool ShouldParseSheet(DataTable sheet, int workbookSheetCount)
        {
            if (sheet == null)
            {
                return false;
            }

            if (LooksLikeOperationalSheet(sheet))
            {
                return true;
            }

            if (IsDailySheet(sheet.TableName) || IsDefaultWorksheetName(sheet.TableName))
            {
                return true;
            }

            return workbookSheetCount == 1;
        }

        private static bool IsDailySheet(string sheetName)
        {
            int day;
            return int.TryParse((sheetName ?? string.Empty).Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out day)
                && day >= 1
                && day <= 31;
        }

        private static bool IsDefaultWorksheetName(string sheetName)
        {
            var normalized = Regex.Replace((sheetName ?? string.Empty).Trim().ToLowerInvariant(), @"\s+", string.Empty);
            return normalized == "sheet1"
                || normalized == "sheet01"
                || normalized == "ورقة1"
                || normalized == "ورقه1";
        }

        private static void CollectBranchHints(DataTable sheet, PosExcelImportPreviewResult result)
        {
            if (sheet == null || result == null || sheet.Rows.Count == 0)
            {
                return;
            }

            var maxRow = Math.Min(sheet.Rows.Count - 1, 50);
            var maxColumn = sheet.Columns.Count - 1;
            for (var rowIndex = 0; rowIndex <= maxRow; rowIndex++)
            {
                var rowValues = new List<string>();
                for (var columnIndex = 0; columnIndex <= maxColumn; columnIndex++)
                {
                    var text = ReadText(sheet, rowIndex, columnIndex);
                    if (IsBranchCodeCell(text) || (rowIndex <= HeaderRowIndex && columnIndex <= 10 && IsBranchHintCell(text)))
                    {
                        AddBranchHint(result, text);
                        rowValues.Add(text);
                    }
                }

                if (rowValues.Count > 1)
                {
                    AddBranchHint(result, string.Join(" ", rowValues));
                }
            }
        }

        private static bool IsBranchHintCell(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            var text = Regex.Replace(value.Trim(), @"\s+", " ");
            if (text.Length < 2 || text.Length > 80 || Regex.IsMatch(text, @"^\d+([.,]\d+)?$"))
            {
                return false;
            }

            var normalized = NormalizeServiceText(text);
            var blockedTokens = new[]
            {
                "ipn", "easycash", "ايزي", "كاش", "العميل", "الخدمه", "الخدمة", "التاريخ",
                "الشحن", "الرسوم", "الاجمالي", "التليفون", "التوكن", "serial", "token"
            };

            return !blockedTokens.Any(token => normalized.Contains(token));
        }

        private static bool IsBranchCodeCell(string value)
        {
            var text = Regex.Replace((value ?? string.Empty).Trim(), @"\s+", string.Empty);
            return Regex.IsMatch(text, @"^[A-Za-z]{1,4}\d{2,6}$");
        }

        private static void AddBranchHint(PosExcelImportPreviewResult result, string hint)
        {
            hint = Regex.Replace((hint ?? string.Empty).Trim(), @"\s+", " ");
            if (string.IsNullOrWhiteSpace(hint))
            {
                return;
            }

            if (!result.WorkbookBranchHints.Any(x => string.Equals(x, hint, StringComparison.OrdinalIgnoreCase)))
            {
                result.WorkbookBranchHints.Add(hint);
            }
        }

        private static string ReadText(DataTable table, int rowIndex, int columnIndex)
        {
            if (table == null || rowIndex < 0 || columnIndex < 0 || rowIndex >= table.Rows.Count || columnIndex >= table.Columns.Count)
            {
                return string.Empty;
            }

            var value = table.Rows[rowIndex][columnIndex];
            return value == null || value == DBNull.Value ? string.Empty : Convert.ToString(value, CultureInfo.InvariantCulture).Trim();
        }

        private static string NormalizeExcelPhone(string value)
        {
            value = (value ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var text = NormalizeArabicDigits(value);
            decimal numeric;
            if ((decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out numeric)
                    || decimal.TryParse(text, NumberStyles.Any, CultureInfo.CurrentCulture, out numeric))
                && numeric == decimal.Truncate(numeric))
            {
                text = decimal.Truncate(numeric).ToString("0", CultureInfo.InvariantCulture);
            }

            var digits = Regex.Replace(text, @"\D+", string.Empty);
            if (digits.Length == 10 && digits[0] == '1')
            {
                return "0" + digits;
            }

            return digits.Length > 0 ? digits : value;
        }

        private static string NormalizeArabicDigits(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            var builder = new StringBuilder(value.Length);
            foreach (var ch in value)
            {
                if (ch >= '\u0660' && ch <= '\u0669')
                {
                    builder.Append((char)('0' + (ch - '\u0660')));
                }
                else if (ch >= '\u06F0' && ch <= '\u06F9')
                {
                    builder.Append((char)('0' + (ch - '\u06F0')));
                }
                else
                {
                    builder.Append(ch);
                }
            }

            return builder.ToString();
        }

        private static decimal? ReadDecimal(DataTable table, int rowIndex, int columnIndex)
        {
            var text = ReadText(table, rowIndex, columnIndex);
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            decimal value;
            if (decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out value)
                || decimal.TryParse(text, NumberStyles.Any, CultureInfo.CurrentCulture, out value))
            {
                return value;
            }

            return null;
        }

        private static DateTime? ReadDate(DataTable table, int rowIndex, int columnIndex)
        {
            var text = ReadText(table, rowIndex, columnIndex);
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            DateTime value;
            var formats = new[] { "d.M.yyyy", "dd.M.yyyy", "d.MM.yyyy", "dd.MM.yyyy", "yyyy-MM-dd" };
            if (DateTime.TryParseExact(text, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out value)
                || DateTime.TryParse(text, CultureInfo.CurrentCulture, DateTimeStyles.None, out value)
                || DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out value))
            {
                return value.Date;
            }

            return null;
        }

        private static void Reject(PosExcelImportRowPreview row, string reason)
        {
            if (string.Equals(row.Status, "ImportedBefore", StringComparison.OrdinalIgnoreCase))
            {
                row.Reasons.Add(reason);
                return;
            }

            row.Status = "Rejected";
            row.Reasons.Add(reason);
        }

        private static void Warn(PosExcelImportRowPreview row, string reason)
        {
            if (!string.Equals(row.Status, "Rejected", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(row.Status, "ImportedBefore", StringComparison.OrdinalIgnoreCase))
            {
                row.Status = "Warning";
            }

            row.Reasons.Add(reason);
        }

        private static string NormalizeKey(string value)
        {
            return (value ?? string.Empty).Trim().ToUpperInvariant();
        }

        private static string DetectBranchHint(string fileName)
        {
            var name = Path.GetFileNameWithoutExtension(fileName ?? string.Empty) ?? string.Empty;
            if (name.IndexOf("سنورس", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "سنورس";
            }

            if (name.IndexOf("ميت غمر", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "ميت غمر";
            }

            return string.Empty;
        }

        private static byte[] ReadAllBytes(Stream stream)
        {
            if (stream.CanSeek)
            {
                stream.Position = 0;
            }

            using (var memory = new MemoryStream())
            {
                stream.CopyTo(memory);
                return memory.ToArray();
            }
        }

        private static string ComputeSha256(byte[] data)
        {
            using (var sha = SHA256.Create())
            {
                return BitConverter.ToString(sha.ComputeHash(data ?? new byte[0])).Replace("-", string.Empty);
            }
        }
    }
}
