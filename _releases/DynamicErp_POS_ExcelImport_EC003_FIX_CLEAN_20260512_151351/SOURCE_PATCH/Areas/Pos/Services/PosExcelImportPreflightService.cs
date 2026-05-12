using MyERP.Areas.Pos.Data;
using MyERP.Areas.Pos.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace MyERP.Areas.Pos.Services
{
    public class PosExcelImportPreflightService
    {
        private readonly PosSqlRepository _repository;

        public PosExcelImportPreflightService(PosSqlRepository repository)
        {
            if (repository == null)
            {
                throw new ArgumentNullException("repository");
            }

            _repository = repository;
        }

        public void Apply(PosExcelImportPreviewResult preview)
        {
            if (preview == null)
            {
                return;
            }

            preview.PreflightItems.Clear();
            ResolveBranch(preview);
            ResolveDefaults(preview);
            ResolveServiceItems(preview);
            ValidateImportantIpnRules(preview);
            MarkKycCreationNeeds(preview);
            FinalizeCounts(preview);
        }

        private void ResolveBranch(PosExcelImportPreviewResult preview)
        {
            var fileToken = Path.GetFileNameWithoutExtension(preview.SourceFileName ?? string.Empty) ?? string.Empty;
            var branches = _repository.GetBranches();
            var workbookHints = (preview.WorkbookBranchHints ?? new List<string>())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            var hasWorkbookBranchHints = workbookHints.Count > 0;
            var branchSourceText = hasWorkbookBranchHints ? string.Join(" | ", workbookHints) : fileToken;
            preview.DetectedBranchHint = branchSourceText;

            var candidates = hasWorkbookBranchHints
                ? MatchBranchesFromHints(workbookHints, branches)
                : MatchBranches(fileToken, branches);
            foreach (var candidate in candidates)
            {
                preview.BranchCandidates.Add(candidate);
            }

            if (candidates.Count == 1)
            {
                var branch = candidates[0];
                preview.DetectedBranch = new PosExcelImportDetectedBranch
                {
                    BranchId = branch.BranchId,
                    BranchCode = branch.BranchCode,
                    BranchName = branch.BranchName,
                    MatchRule = branch.MatchRule
                };

                AddPreflight(preview, "Branch", "Mapped", FormatBranch(branch), hasWorkbookBranchHints ? "تم تحديد الفرع تلقائيا من بيانات الفرع داخل الشيت." : "تم تحديد الفرع تلقائيا من اسم الملف.");
                return;
            }

            if (candidates.Count > 1)
            {
                AddPreflight(preview, "Branch", "Required", string.Join(" | ", candidates.Select(FormatBranch)), hasWorkbookBranchHints ? "بيانات الفرع داخل الشيت تطابق أكثر من فرع. أوقف الاستيراد وصحح كود/اسم الفرع داخل الملف." : "اسم الملف يطابق أكثر من فرع. أوقف الاستيراد وحدد قاعدة تسمية أدق للملف.");
                RejectAll(preview, hasWorkbookBranchHints ? "تحديد الفرع غير حتمي بسبب تعدد الفروع المطابقة لبيانات الشيت." : "تحديد الفرع غير حتمي بسبب تعدد الفروع المطابقة لاسم الملف.");
                return;
            }

            AddPreflight(preview, "Branch", "Required", branchSourceText, hasWorkbookBranchHints ? "تعذر تحديد الفرع من كود/اسم الفرع المكتوب داخل الشيت." : "تعذر تحديد الفرع من اسم الملف بالكود أو الاسم.");
            RejectAll(preview, hasWorkbookBranchHints ? "تعذر تحديد الفرع من بيانات الشيت." : "تعذر تحديد الفرع من اسم الملف.");
        }

        private void ResolveDefaults(PosExcelImportPreviewResult preview)
        {
            if (preview.DetectedBranch == null)
            {
                AddPreflight(preview, "POS Defaults", "Required", string.Empty, "لن يتم تحميل defaults قبل تحديد فرع واحد بشكل حتمي.");
                return;
            }

            var context = _repository.GetDefaultPosUserContextForBranch(preview.DetectedBranch.BranchId);
            if (context == null)
            {
                AddPreflight(preview, "POS User/Cashier", "Required", preview.DetectedBranch.BranchName, "لا يوجد مستخدم POS نشط لهذا الفرع لديه مخزن وخزنة ومندوب.");
                RejectAll(preview, "لا يوجد مستخدم POS افتراضي صالح للفرع.");
                return;
            }

            preview.EffectiveDefaults = new PosExcelImportDefaultContext
            {
                UserId = context.UserId,
                UserName = context.UserName,
                EmpId = context.EmpId,
                EmpName = context.EmpName,
                BranchId = context.BranchId,
                BranchName = context.BranchName,
                StoreId = context.StoreId,
                StoreName = context.StoreName,
                BoxId = context.BoxId,
                BoxName = context.BoxName,
                PaymentNetId = context.PaymentNetId,
                PaymentTypeId = context.PaymentTypeId,
                PaymentName = context.PaymentName,
                BankId = context.BankId,
                BankName = context.BankName
            };

            AddPreflight(preview, "POS User/Cashier", HasValue(context.UserId) ? "Mapped" : "Required", FormatIdName(context.UserId, context.UserName), "تم اختيار مستخدم POS افتراضي من نفس الفرع.");
            AddPreflight(preview, "Employee/Salesman", HasValue(context.EmpId) ? "Mapped" : "Required", FormatIdName(context.EmpId, context.EmpName), "المندوب مطلوب للحفظ والحسابات.");
            AddPreflight(preview, "Store", HasValue(context.StoreId) ? "Mapped" : "Required", FormatIdName(context.StoreId, context.StoreName), "المخزن مطلوب لتأثير المخزون والإذن.");
            AddPreflight(preview, "CashBox", HasValue(context.BoxId) ? "Mapped" : "Required", FormatIdName(context.BoxId, context.BoxName), "الخزنة مطلوبة للدفع والقيد.");
            AddPreflight(preview, "PaymentType", HasValue(context.PaymentTypeId) ? "Mapped" : "Required", FormatIdName(context.PaymentTypeId, context.PaymentName), "تم تحميل طريقة الدفع الافتراضية من إعدادات المستخدم.");

            if (!HasValue(context.UserId) || !HasValue(context.EmpId) || !HasValue(context.StoreId) || !HasValue(context.BoxId) || !HasValue(context.PaymentTypeId))
            {
                RejectAll(preview, "defaults المطلوبة للحفظ غير مكتملة للفرع المحدد.");
            }
        }

        private void ResolveServiceItems(PosExcelImportPreviewResult preview)
        {
            if (preview.DetectedBranch == null)
            {
                return;
            }

            var services = preview.Rows
                .Where(x => !string.IsNullOrWhiteSpace(x.ServiceType))
                .Select(x => x.ServiceType)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (var serviceText in services)
            {
                var internalType = ResolveInternalServiceType(serviceText);
                if (string.IsNullOrWhiteSpace(internalType))
                {
                    RejectRowsByService(preview, serviceText, "نوع الخدمة غير معروف ولا يوجد له mapping داخلي.");
                    AddPreflight(preview, "Service: " + serviceText, "Required", serviceText, "نوع الخدمة غير معروف.");
                    continue;
                }

                var items = IsCashInRechargeText(NormalizeArabic(serviceText))
                    ? _repository.FindServiceItemsByName(serviceText, internalType, preview.DetectedBranch.BranchId)
                    : _repository.GetDefaultServiceItem(internalType, null, preview.DetectedBranch.BranchId);

                if (items != null && items.Count > 1)
                {
                    RejectRowsByService(preview, serviceText, "يوجد أكثر من صنف POS مطابق لنص الخدمة؛ يجب ضبط اسم الخدمة أو الصنف لتجنب ترحيلها على صنف خطأ.");
                    AddPreflight(preview, "ServiceItem: " + serviceText, "Rejected", string.Join(", ", items.Select(x => x.Item_ID + " - " + x.ItemName)), "مطابقة صنف الخدمة غير حاسمة.");
                    continue;
                }

                if ((items == null || items.Count == 0) && string.Equals(internalType, "cash-in", StringComparison.OrdinalIgnoreCase))
                {
                    items = _repository.GetDefaultServiceItem(internalType, null, preview.DetectedBranch.BranchId);
                }

                var item = items == null ? null : items.FirstOrDefault();
                if (item == null || item.Item_ID <= 0)
                {
                    RejectRowsByService(preview, serviceText, "لا يوجد صنف POS افتراضي لنوع الخدمة.");
                    AddPreflight(preview, "Service: " + serviceText, "Required", internalType, "تعذر العثور على صنف الخدمة الافتراضي من منطق شاشة POS.");
                    continue;
                }

                foreach (var row in preview.Rows.Where(x => string.Equals(x.ServiceType, serviceText, StringComparison.OrdinalIgnoreCase)))
                {
                    row.InternalServiceType = internalType;
                    row.InternalServiceName = GetInternalServiceName(internalType);
                    row.ServiceItemId = item.Item_ID;
                    row.ServiceItemName = item.ItemName;
                }

                AddPreflight(preview, "Service: " + serviceText, "Mapped", serviceText + " -> " + internalType + " -> " + item.Item_ID + " / " + item.ItemName, "تم ربط الخدمة بصنف POS افتراضي معتمد.");
            }
        }

        private void ValidateImportantIpnRules(PosExcelImportPreviewResult preview)
        {
            var importantRows = preview.Rows
                .Where(IsImportantIpnRow)
                .ToList();

            foreach (var row in importantRows.Where(x => string.IsNullOrWhiteSpace(x.IPN)))
            {
                Reject(row, "رقم IPN مطلوب في كاش ان والكروت فقط.");
            }

            var duplicates = importantRows
                .Where(x => !string.IsNullOrWhiteSpace(x.IPN))
                .GroupBy(x => NormalizeEnglish(x.IPN))
                .Where(x => x.Count() > 1)
                .Select(x => x.Key)
                .ToList();

            foreach (var row in importantRows.Where(x => duplicates.Contains(NormalizeEnglish(x.IPN))))
            {
                var sameFileRows = importantRows
                    .Where(x => string.Equals(NormalizeEnglish(x.IPN), NormalizeEnglish(row.IPN), StringComparison.OrdinalIgnoreCase))
                    .Select(x => x.RowNumber.ToString(CultureInfo.InvariantCulture))
                    .ToList();
                Warn(row, "تنبيه Excel: رقم IPN مكرر داخل نفس الملف في الصفوف: " + string.Join(", ", sameFileRows) + ". سيتم الحفظ مع ملاحظة.");
            }

            foreach (var row in importantRows.Where(x => !string.IsNullOrWhiteSpace(x.IPN)))
            {
                var existing = _repository.FindImportantIpnDuplicatePosSale(row.IPN, null);
                if (existing != null)
                {
                    Warn(row, "تنبيه Excel: رقم IPN مكرر مع فاتورة سابقة رقم " + existing.Transaction_ID.ToString(CultureInfo.InvariantCulture) + (string.IsNullOrWhiteSpace(existing.NoteSerial1) ? string.Empty : " / " + existing.NoteSerial1) + ". سيتم حفظ فاتورة Excel مع ملاحظة.");
                }
            }

            AddPreflight(preview, "IPN uniqueness", "Mapped", "Excel warning only", "تكرار IPN في Excel لا يمنع الحفظ؛ يتم حفظ الفاتورة مع ملاحظة ويمكن فلترتها لاحقا.");
        }

        private void MarkKycCreationNeeds(PosExcelImportPreviewResult preview)
        {
            if (preview.DetectedBranch == null)
            {
                return;
            }

            foreach (var row in preview.Rows.Where(x => string.Equals(x.InternalServiceType, "card", StringComparison.OrdinalIgnoreCase)))
            {
                var token = row.MatchedToken;
                if (string.IsNullOrWhiteSpace(token))
                {
                    Reject(row, "لا يوجد توكن مرتبط بصف الكارت.");
                    continue;
                }

                var customer = _repository.LookupKeshniCardCustomer(token, preview.DetectedBranch.BranchId, false);
                if (customer == null)
                {
                    row.RequiresKycCreation = true;
                    Warn(row, "التوكن غير موجود في KYC؛ يجب إنشاء العميل والتوكن أولا ثم حفظ الفاتورة.");
                }
            }
        }

        private static IList<PosExcelImportBranchCandidate> MatchBranches(string fileToken, IList<PosBranchDto> branches)
        {
            branches = branches ?? new List<PosBranchDto>();
            var normalizedFile = NormalizeArabic(fileToken);
            var normalizedEnglish = NormalizeEnglish(fileToken);
            var codeTokens = GetBranchCodeTokens(fileToken);
            var exactCode = branches
                .Where(x =>
                {
                    var branchCode = NormalizeEnglish(x.BranchCode);
                    return !string.IsNullOrWhiteSpace(branchCode)
                        && (string.Equals(branchCode, normalizedEnglish, StringComparison.OrdinalIgnoreCase)
                            || codeTokens.Contains(branchCode, StringComparer.OrdinalIgnoreCase)
                            || (branchCode.Length >= 3 && normalizedEnglish.Contains(branchCode)));
                })
                .Select(x => ToCandidate(x, "Exact branch_Code"))
                .ToList();
            if (exactCode.Count > 0)
            {
                return exactCode;
            }

            var nameContains = branches
                .Where(x => !string.IsNullOrWhiteSpace(x.BranchName) && NormalizeArabic(x.BranchName).Contains(normalizedFile))
                .Select(x => ToCandidate(x, "branch_name contains filename"))
                .ToList();
            if (nameContains.Count > 0)
            {
                return nameContains;
            }

            var tokens = GetSignificantTokens(fileToken);
            var tokenMatches = branches
                .Where(x => tokens.Any(token => NormalizeArabic(x.BranchName).Contains(token) || NormalizeArabic(x.BranchNameEnglish).Contains(token)))
                .Select(x => ToCandidate(x, "filename significant token in branch_name"))
                .GroupBy(x => x.BranchId)
                .Select(x => x.First())
                .ToList();
            if (tokenMatches.Count > 0)
            {
                return tokenMatches;
            }

            return branches
                .Where(x => !string.IsNullOrWhiteSpace(x.BranchName) && normalizedFile.Contains(NormalizeArabic(x.BranchName)))
                .Select(x => ToCandidate(x, "filename contains branch_name"))
                .ToList();
        }

        private static IList<PosExcelImportBranchCandidate> MatchBranchesFromHints(IList<string> hints, IList<PosBranchDto> branches)
        {
            branches = branches ?? new List<PosBranchDto>();
            var codeTokens = (hints ?? new List<string>())
                .SelectMany(GetBranchCodeTokens)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            var exactCodeMatches = branches
                .Where(branch =>
                {
                    var branchCode = NormalizeEnglish(branch.BranchCode);
                    return !string.IsNullOrWhiteSpace(branchCode)
                        && codeTokens.Contains(branchCode, StringComparer.OrdinalIgnoreCase);
                })
                .Select(x => ToCandidate(x, "workbook explicit branch_Code"))
                .GroupBy(x => x.BranchId)
                .Select(x => x.First())
                .ToList();
            if (exactCodeMatches.Count > 0)
            {
                return exactCodeMatches;
            }

            return (hints ?? new List<string>())
                .SelectMany(hint => MatchBranches(hint, branches))
                .GroupBy(x => x.BranchId)
                .Select(x =>
                {
                    var candidate = x.First();
                    candidate.MatchRule = "workbook branch hint: " + candidate.MatchRule;
                    return candidate;
                })
                .ToList();
        }

        private static IList<string> GetBranchCodeTokens(string value)
        {
            return Regex.Matches(value ?? string.Empty, @"[A-Za-z]{1,8}\s*[-_ ]?\s*\d{1,8}|[A-Za-z0-9]{3,20}")
                .Cast<Match>()
                .Select(x => NormalizeEnglish(x.Value))
                .Where(x => x.Length >= 3)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static PosExcelImportBranchCandidate ToCandidate(PosBranchDto branch, string rule)
        {
            return new PosExcelImportBranchCandidate
            {
                BranchId = branch.BranchId,
                BranchCode = branch.BranchCode,
                BranchName = branch.BranchName,
                MatchRule = rule
            };
        }

        private static string ResolveInternalServiceType(string serviceText)
        {
            var value = NormalizeArabic(serviceText);
            if (IsCashInRechargeText(value))
            {
                return "cash-in";
            }

            if (ContainsAny(value, "\u0645\u062e\u0627\u0644\u0641"))
            {
                return "violations";
            }

            if (ContainsAny(value, "\u0643\u0627\u0631\u062a") && (ContainsAny(value, "\u0643\u064a\u0634\u0646\u064a") || ContainsAny(value, "\u0643\u064a\u0634\u0646\u0649")))
            {
                return "card";
            }

            if (ContainsAny(value, "\u0643\u0627\u0634") && ContainsAny(value, "\u0627\u0648\u062a"))
            {
                return "cash-out";
            }

            if (ContainsAny(value, "\u0643\u0627\u0634") && ContainsAny(value, "\u0627\u0646"))
            {
                return "cash-in";
            }

            var english = NormalizeEnglish(serviceText);
            if (english.Contains("cashout") || english.Contains("cash-out"))
            {
                return "cash-out";
            }

            if (english.Contains("cashin") || english.Contains("cash-in"))
            {
                return "cash-in";
            }

            if (english.Contains("violation"))
            {
                return "violations";
            }

            if (english.Contains("card"))
            {
                return "card";
            }

            return string.Empty;
        }

        private static bool IsCashInRechargeText(string normalizedArabic)
        {
            if (string.IsNullOrWhiteSpace(normalizedArabic))
            {
                return false;
            }

            return ContainsAny(normalizedArabic,
                "\u0634\u062d\u0646 \u062d \u0628\u0646\u0643\u064a",
                "\u0634\u062d\u0646 \u0645\u062d\u0641\u0638\u0647",
                "\u0634\u062d\u0646 \u0628\u0637\u0627\u0642\u0647 \u0627\u062e\u0631\u064a",
                "\u0634\u062d\u0646 \u0643\u064a\u0634\u0646\u064a",
                "\u0634\u062d\u0646");
        }

        private static bool ContainsAny(string value, params string[] tokens)
        {
            if (string.IsNullOrWhiteSpace(value) || tokens == null)
            {
                return false;
            }

            foreach (var token in tokens)
            {
                if (!string.IsNullOrWhiteSpace(token) && value.Contains(token))
                {
                    return true;
                }
            }

            return false;
        }

        private static string NormalizeArabic(string value)
        {
            value = (value ?? string.Empty).Trim();
            value = value.Replace("ـ", string.Empty)
                .Replace("أ", "ا")
                .Replace("إ", "ا")
                .Replace("آ", "ا")
                .Replace("ى", "ي")
                .Replace("ؤ", "و")
                .Replace("ئ", "ي");
            value = Regex.Replace(value, @"\s+", " ");
            return value.ToLowerInvariant();
        }

        private static string NormalizeEnglish(string value)
        {
            value = (value ?? string.Empty).Trim().ToLowerInvariant();
            value = Regex.Replace(value, @"[\s\-_()]+", string.Empty);
            return value;
        }

        private static IList<string> GetSignificantTokens(string value)
        {
            var normalized = NormalizeArabic(Regex.Replace(value ?? string.Empty, @"\([^)]+\)", " "));
            return normalized.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(x => x.Length >= 3 && !Regex.IsMatch(x, @"^\d+$"))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static void RejectRowsByService(PosExcelImportPreviewResult preview, string serviceText, string reason)
        {
            foreach (var row in preview.Rows.Where(x => string.Equals(x.ServiceType, serviceText, StringComparison.OrdinalIgnoreCase)))
            {
                Reject(row, reason);
            }
        }

        private static bool IsImportantIpnRow(PosExcelImportRowPreview row)
        {
            return row != null
                && (string.Equals(row.InternalServiceType, "cash-in", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(row.InternalServiceType, "card", StringComparison.OrdinalIgnoreCase));
        }

        private static void Warn(PosExcelImportRowPreview row, string reason)
        {
            if (!string.Equals(row.Status, "Rejected", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(row.Status, "ImportedBefore", StringComparison.OrdinalIgnoreCase))
            {
                row.Status = "Warning";
            }

            if (!row.Reasons.Contains(reason))
            {
                row.Reasons.Add(reason);
            }
        }

        private static void RejectAll(PosExcelImportPreviewResult preview, string reason)
        {
            foreach (var row in preview.Rows)
            {
                Reject(row, reason);
            }
        }

        private static void Reject(PosExcelImportRowPreview row, string reason)
        {
            if (string.Equals(row.Status, "ImportedBefore", StringComparison.OrdinalIgnoreCase))
            {
                if (!row.Reasons.Contains(reason))
                {
                    row.Reasons.Add(reason);
                }

                return;
            }

            row.Status = "Rejected";
            if (!row.Reasons.Contains(reason))
            {
                row.Reasons.Add(reason);
            }
        }

        private static void AddPreflight(PosExcelImportPreviewResult preview, string fieldName, string status, string value, string message)
        {
            preview.PreflightItems.Add(new PosExcelImportPreflightItem
            {
                FieldName = fieldName,
                Status = status,
                Value = value,
                Message = message
            });
        }

        private static void FinalizeCounts(PosExcelImportPreviewResult preview)
        {
            preview.ReadyCount = preview.Rows.Count(x => string.Equals(x.Status, "Ready", StringComparison.OrdinalIgnoreCase));
            preview.WarningCount = preview.Rows.Count(x => string.Equals(x.Status, "Warning", StringComparison.OrdinalIgnoreCase));
            preview.RejectedCount = preview.Rows.Count(x => string.Equals(x.Status, "Rejected", StringComparison.OrdinalIgnoreCase));
            preview.UnmatchedTokenCount = preview.Tokens.Count(x => !string.Equals(x.Status, "Matched", StringComparison.OrdinalIgnoreCase));
            preview.TotalAmount = preview.Rows.Where(x => !string.Equals(x.Status, "Rejected", StringComparison.OrdinalIgnoreCase)).Sum(x => x.Amount.GetValueOrDefault());
            preview.TotalFees = preview.Rows.Where(x => !string.Equals(x.Status, "Rejected", StringComparison.OrdinalIgnoreCase)).Sum(x => x.Fee.GetValueOrDefault());
            preview.TotalGross = preview.Rows.Where(x => !string.Equals(x.Status, "Rejected", StringComparison.OrdinalIgnoreCase)).Sum(x => x.GrossTotal.GetValueOrDefault());
        }

        private static bool HasValue(int? value)
        {
            return value.GetValueOrDefault() > 0;
        }

        private static string FormatBranch(PosExcelImportBranchCandidate branch)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0} - {1} - {2}", branch.BranchCode, branch.BranchName, branch.BranchId);
        }

        private static string FormatIdName(int? id, string name)
        {
            return HasValue(id) ? id.Value.ToString(CultureInfo.InvariantCulture) + " - " + (name ?? string.Empty) : string.Empty;
        }

        private static string GetInternalServiceName(string serviceType)
        {
            switch ((serviceType ?? string.Empty).Trim().ToLowerInvariant())
            {
                case "cash-in":
                    return "كاش ان";
                case "cash-out":
                    return "كاش اوت";
                case "violations":
                    return "مخالفات";
                case "card":
                    return "كارت كيشني";
                default:
                    return serviceType;
            }
        }
    }
}
