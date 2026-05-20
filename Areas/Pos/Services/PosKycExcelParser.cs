using ExcelDataReader;
using MyERP.Areas.Pos.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace MyERP.Areas.Pos.Services
{
    public class PosKycExcelParser
    {
        public IList<PosKycExcelPreviewRow> Parse(Stream stream, IList<PosBranchDto> branches, PosUserContext context)
        {
            var rows = new List<PosKycExcelPreviewRow>();
            if (stream == null)
            {
                return rows;
            }

            if (stream.CanSeek)
            {
                stream.Position = 0;
            }

            using (var reader = ExcelReaderFactory.CreateReader(stream))
            {
                var dataSet = reader.AsDataSet(new ExcelDataSetConfiguration
                {
                    ConfigureDataTable = _ => new ExcelDataTableConfiguration
                    {
                        UseHeaderRow = false
                    }
                });

                if (dataSet == null || dataSet.Tables.Count == 0)
                {
                    return rows;
                }

                var sheet = dataSet.Tables[0];
                if (sheet.Rows.Count < 2)
                {
                    return rows;
                }

                var headerMap = BuildHeaderMap(sheet);
                var lastRow = Math.Min(sheet.Rows.Count - 1, 1000);
                for (var rowIndex = 1; rowIndex <= lastRow; rowIndex++)
                {
                    var row = BuildRow(sheet, rowIndex, headerMap, branches, context);
                    if (row != null)
                    {
                        rows.Add(row);
                    }
                }
            }

            return rows;
        }

        private static IDictionary<string, int> BuildHeaderMap(DataTable sheet)
        {
            var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            if (sheet == null || sheet.Rows.Count == 0)
            {
                return map;
            }

            for (var columnIndex = 0; columnIndex < sheet.Columns.Count; columnIndex++)
            {
                var header = NormalizeHeader(ReadText(sheet, 0, columnIndex));
                if (string.IsNullOrWhiteSpace(header))
                {
                    continue;
                }

                if (header.Contains("تاريخبيع") && header.Contains("كارت")) { map["OrderDate"] = columnIndex; }
                else if (header.Contains("توكين") || header.Contains("توكن") || header.Contains("باركود")) { map["CardNo"] = columnIndex; }
                else if (header.Contains("عربي") && header.Contains("اسم")) { map["ArabicName"] = columnIndex; }
                else if (header.Contains("انجليزي") && header.Contains("اسم")) { map["EnglishName"] = columnIndex; }
                else if (header.Contains("قومي")) { map["NationalId"] = columnIndex; }
                else if (header.Contains("عنوان") && header.Contains("عربي")) { map["ArabicAddress"] = columnIndex; }
                else if (header.Contains("عنوان") && header.Contains("انجليزي")) { map["EnglishAddress"] = columnIndex; }
                else if (header.Contains("موبايل")) { map["Mobile"] = columnIndex; }
                else if (header.Contains("ميلاد")) { map["BirthDate"] = columnIndex; }
                else if (header.Contains("اصدار")) { map["CardDate"] = columnIndex; }
                else if (header.Contains("انتهاء")) { map["CardEndDate"] = columnIndex; }
                else if (header.Contains("كود") && header.Contains("فرع")) { map["BranchCode"] = columnIndex; }
            }

            AddFallback(map, "OrderDate", 0);
            AddFallback(map, "CardNo", 1);
            AddFallback(map, "ArabicName", 2);
            AddFallback(map, "EnglishName", 3);
            AddFallback(map, "NationalId", 4);
            AddFallback(map, "ArabicAddress", 5);
            AddFallback(map, "EnglishAddress", 6);
            AddFallback(map, "Mobile", 7);
            AddFallback(map, "BirthDate", 8);
            AddFallback(map, "CardDate", 9);
            AddFallback(map, "CardEndDate", 10);
            AddFallback(map, "BranchCode", 11);
            return map;
        }

        private static void AddFallback(IDictionary<string, int> map, string key, int columnIndex)
        {
            if (!map.ContainsKey(key))
            {
                map[key] = columnIndex;
            }
        }

        private static PosKycExcelPreviewRow BuildRow(DataTable sheet, int rowIndex, IDictionary<string, int> headerMap, IList<PosBranchDto> branches, PosUserContext context)
        {
            var cardNo = NormalizeCardToken(ReadColumn(sheet, rowIndex, headerMap, "CardNo"));
            var arabicName = ReadColumn(sheet, rowIndex, headerMap, "ArabicName");
            var englishName = ReadColumn(sheet, rowIndex, headerMap, "EnglishName");
            var nationalId = DigitsOnly(ReadColumn(sheet, rowIndex, headerMap, "NationalId"));
            var mobile = NormalizeExcelPhone(ReadColumn(sheet, rowIndex, headerMap, "Mobile"));
            if (string.IsNullOrWhiteSpace(cardNo)
                && string.IsNullOrWhiteSpace(arabicName)
                && string.IsNullOrWhiteSpace(englishName)
                && string.IsNullOrWhiteSpace(nationalId)
                && string.IsNullOrWhiteSpace(mobile))
            {
                return null;
            }

            var branchCode = ReadColumn(sheet, rowIndex, headerMap, "BranchCode");
            var resolvedBranch = ResolveBranch(branchCode, branches);
            if (resolvedBranch == null)
            {
                var scannedBranchCode = FindBranchCodeInRow(sheet, rowIndex, branches);
                if (!string.IsNullOrWhiteSpace(scannedBranchCode))
                {
                    branchCode = scannedBranchCode;
                    resolvedBranch = ResolveBranch(branchCode, branches);
                }
            }
            var row = new PosKycExcelPreviewRow
            {
                RowNumber = rowIndex + 1,
                BranchCode = branchCode
            };

            int? branchId = context == null ? null : context.BranchId;
            var branchName = context == null ? string.Empty : context.BranchName;
            if (resolvedBranch != null)
            {
                if (context != null && context.CanChangeDefaults)
                {
                    branchId = resolvedBranch.BranchId;
                    branchName = resolvedBranch.BranchName;
                }
                else if (context != null && context.BranchId.HasValue && context.BranchId.Value != resolvedBranch.BranchId)
                {
                    row.Warnings.Add("كود الفرع في الشيت مختلف عن فرع الجلسة؛ سيتم استخدام فرع الجلسة عند الحفظ.");
                }
            }
            else if (!string.IsNullOrWhiteSpace(branchCode))
            {
                row.Warnings.Add("كود الفرع الموجود في الشيت غير معروف: " + branchCode);
            }

            var arabicParts = SplitNameParts(arabicName, 4);
            var englishParts = SplitNameParts(englishName, 4);
            var englishAddressText = ReadColumn(sheet, rowIndex, headerMap, "EnglishAddress");
            var englishAddressParts = string.IsNullOrWhiteSpace(englishAddressText)
                ? new[] { "Egypt", "Egypt", "Egypt" }
                : SplitAddressParts(englishAddressText);
            var arabicAddress = ReadColumn(sheet, rowIndex, headerMap, "ArabicAddress");
            if (string.IsNullOrWhiteSpace(arabicAddress))
            {
                arabicAddress = "مصر مصر";
            }
            row.BranchName = branchName;
            row.Customer = new PosCustomerLookupDto
            {
                CustomerID = 0,
                Name = arabicName,
                CustomerName = arabicName,
                NameE = englishName,
                ArabicName0 = arabicParts[0],
                ArabicName1 = arabicParts[1],
                ArabicName2 = arabicParts[2],
                ArabicName3 = arabicParts[3],
                EnglishName0 = englishParts[0],
                EnglishName1 = englishParts[1],
                EnglishName2 = englishParts[2],
                EnglishName3 = englishParts[3],
                EnglishName5 = englishAddressParts[0],
                EnglishName6 = englishAddressParts[1],
                EnglishName7 = englishAddressParts[2],
                Phone = mobile,
                Phone2 = mobile,
                VisaNumber = cardNo,
                CardNo = cardNo,
                CardId = cardNo,
                Tet_NumPoket = nationalId,
                Address = arabicAddress,
                MailAdress = string.Join(" ", englishAddressParts.Where(x => !string.IsNullOrWhiteSpace(x))),
                BirthDate = ReadDate(sheet, rowIndex, headerMap, "BirthDate"),
                CardDate = ReadDate(sheet, rowIndex, headerMap, "CardDate"),
                CardEndDate = ReadDate(sheet, rowIndex, headerMap, "CardEndDate"),
                OrderDate = ReadDate(sheet, rowIndex, headerMap, "OrderDate"),
                EasyCashType = 0,
                BranchId = branchId,
                BranchName = branchName,
                CreatedDate = ReadDate(sheet, rowIndex, headerMap, "OrderDate")
            };

            return row;
        }

        private static string ReadColumn(DataTable sheet, int rowIndex, IDictionary<string, int> headerMap, string key)
        {
            int columnIndex;
            return headerMap != null && headerMap.TryGetValue(key, out columnIndex)
                ? ReadText(sheet, rowIndex, columnIndex)
                : string.Empty;
        }

        private static string ReadText(DataTable sheet, int rowIndex, int columnIndex)
        {
            if (sheet == null || rowIndex < 0 || columnIndex < 0 || rowIndex >= sheet.Rows.Count || columnIndex >= sheet.Columns.Count)
            {
                return string.Empty;
            }

            var value = sheet.Rows[rowIndex][columnIndex];
            if (value == null || value == DBNull.Value)
            {
                return string.Empty;
            }

            if (value is DateTime)
            {
                return ((DateTime)value).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            }

            return Convert.ToString(value, CultureInfo.InvariantCulture).Trim();
        }

        private static DateTime? ReadDate(DataTable sheet, int rowIndex, IDictionary<string, int> headerMap, string key)
        {
            int columnIndex;
            if (headerMap == null || !headerMap.TryGetValue(key, out columnIndex)
                || sheet == null || rowIndex < 0 || columnIndex < 0 || rowIndex >= sheet.Rows.Count || columnIndex >= sheet.Columns.Count)
            {
                return null;
            }

            var raw = sheet.Rows[rowIndex][columnIndex];
            if (raw == null || raw == DBNull.Value)
            {
                return null;
            }

            if (raw is DateTime)
            {
                return ((DateTime)raw).Date;
            }

            double serial;
            if (double.TryParse(Convert.ToString(raw, CultureInfo.InvariantCulture), NumberStyles.Any, CultureInfo.InvariantCulture, out serial)
                && serial > 20000 && serial < 90000)
            {
                return DateTime.FromOADate(serial).Date;
            }

            var text = Convert.ToString(raw, CultureInfo.InvariantCulture).Trim();
            DateTime parsed;
            var formats = new[] { "d-M-yyyy", "dd-M-yyyy", "d-MM-yyyy", "dd-MM-yyyy", "d/M/yyyy", "dd/MM/yyyy", "yyyy-MM-dd", "yyyy-MM-dd HH:mm:ss", "M/d/yyyy h:mm:ss tt" };
            if (DateTime.TryParseExact(text, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed)
                || DateTime.TryParse(text, CultureInfo.CurrentCulture, DateTimeStyles.None, out parsed)
                || DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed))
            {
                return parsed.Date;
            }

            return null;
        }

        private static PosBranchDto ResolveBranch(string branchCode, IList<PosBranchDto> branches)
        {
            var code = NormalizeEnglishKey(branchCode);
            return string.IsNullOrWhiteSpace(code) || branches == null
                ? null
                : branches.FirstOrDefault(x => string.Equals(NormalizeEnglishKey(x.BranchCode), code, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(NormalizeEnglishKey(x.BranchName), code, StringComparison.OrdinalIgnoreCase));
        }

        private static string FindBranchCodeInRow(DataTable sheet, int rowIndex, IList<PosBranchDto> branches)
        {
            if (sheet == null || branches == null || rowIndex < 0 || rowIndex >= sheet.Rows.Count)
            {
                return null;
            }

            for (var columnIndex = 0; columnIndex < sheet.Columns.Count; columnIndex++)
            {
                var text = ReadText(sheet, rowIndex, columnIndex);
                var branch = ResolveBranch(text, branches);
                if (branch != null)
                {
                    return branch.BranchCode;
                }
            }

            return null;
        }

        private static string NormalizeHeader(string value)
        {
            value = (value ?? string.Empty).Trim()
                .Replace("ـ", string.Empty)
                .Replace("أ", "ا")
                .Replace("إ", "ا")
                .Replace("آ", "ا")
                .Replace("ى", "ي")
                .Replace("ة", "ه");
            value = Regex.Replace(value, @"[\s\(\)\/\\_\-]+", string.Empty);
            return value.ToLowerInvariant();
        }

        private static string NormalizeEnglishKey(string value)
        {
            return Regex.Replace((value ?? string.Empty).Trim().ToLowerInvariant(), @"[\s\-_()]+", string.Empty);
        }

        private static string DigitsOnly(string value)
        {
            return Regex.Replace(value ?? string.Empty, @"\D+", string.Empty);
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

        private static string NormalizeExcelPhone(string value)
        {
            var text = NormalizeArabicDigits(value);
            decimal numeric;
            if ((decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out numeric)
                    || decimal.TryParse(text, NumberStyles.Any, CultureInfo.CurrentCulture, out numeric))
                && numeric == decimal.Truncate(numeric))
            {
                text = decimal.Truncate(numeric).ToString("0", CultureInfo.InvariantCulture);
            }

            var digits = DigitsOnly(text);
            if (digits.Length == 10 && digits[0] == '1')
            {
                return "0" + digits;
            }

            return digits;
        }

        private static string NormalizeArabicDigits(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            var chars = value.ToCharArray();
            for (var i = 0; i < chars.Length; i++)
            {
                if (chars[i] >= '\u0660' && chars[i] <= '\u0669')
                {
                    chars[i] = (char)('0' + (chars[i] - '\u0660'));
                }
                else if (chars[i] >= '\u06F0' && chars[i] <= '\u06F9')
                {
                    chars[i] = (char)('0' + (chars[i] - '\u06F0'));
                }
            }

            return new string(chars);
        }

        private static string[] SplitNameParts(string value, int count)
        {
            var result = new string[count];
            var parts = Regex.Split((value ?? string.Empty).Trim(), @"\s+")
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();
            for (var i = 0; i < count; i++)
            {
                result[i] = i < parts.Count ? parts[i] : string.Empty;
            }

            if (parts.Count > count)
            {
                result[count - 1] = string.Join(" ", parts.Skip(count - 1));
            }

            return result;
        }

        private static string[] SplitAddressParts(string value)
        {
            var result = new[] { string.Empty, string.Empty, string.Empty };
            value = Regex.Replace(value ?? string.Empty, @"\s+", " ").Trim();
            if (string.IsNullOrWhiteSpace(value))
            {
                return result;
            }

            for (var i = 0; i < result.Length && value.Length > 0; i++)
            {
                if (value.Length <= 35)
                {
                    result[i] = value;
                    break;
                }

                var take = value.LastIndexOf(' ', Math.Min(35, value.Length - 1));
                if (take < 15)
                {
                    take = 35;
                }

                result[i] = value.Substring(0, take).Trim();
                value = value.Substring(Math.Min(take, value.Length)).Trim();
            }

            return result;
        }
    }
}
