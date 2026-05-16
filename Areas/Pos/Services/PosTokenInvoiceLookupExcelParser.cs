using ExcelDataReader;
using MyERP.Areas.Pos.Models;
using System;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace MyERP.Areas.Pos.Services
{
    public class PosTokenInvoiceLookupExcelParser
    {
        public PosTokenInvoiceLookupUploadResult Parse(Stream stream)
        {
            var result = new PosTokenInvoiceLookupUploadResult();
            if (stream == null)
            {
                return result;
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
                    return result;
                }

                foreach (DataTable sheet in dataSet.Tables)
                {
                    ReadSheet(sheet, result);
                }
            }

            FinalizeSummary(result);
            return result;
        }

        public PosTokenInvoiceLookupUploadResult ParsePlainText(Stream stream)
        {
            var result = new PosTokenInvoiceLookupUploadResult();
            if (stream == null)
            {
                return result;
            }

            if (stream.CanSeek)
            {
                stream.Position = 0;
            }

            using (var reader = new StreamReader(stream, Encoding.UTF8, true))
            {
                var rowNumber = 0;
                while (!reader.EndOfStream)
                {
                    rowNumber++;
                    var line = reader.ReadLine();
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }

                    foreach (var part in Regex.Split(line, @"[,;\t|]+"))
                    {
                        AddToken(result, NormalizeToken(part), rowNumber);
                    }
                }
            }

            FinalizeSummary(result);
            return result;
        }

        private static void ReadSheet(DataTable sheet, PosTokenInvoiceLookupUploadResult result)
        {
            if (sheet == null || sheet.Rows.Count == 0)
            {
                return;
            }

            var tokenColumn = ResolveTokenColumn(sheet);
            if (tokenColumn.HasValue)
            {
                var firstDataRow = HasRecognizedHeader(sheet, tokenColumn.Value) ? 1 : 0;
                for (var rowIndex = firstDataRow; rowIndex < sheet.Rows.Count; rowIndex++)
                {
                    AddToken(result, NormalizeToken(ReadText(sheet, rowIndex, tokenColumn.Value)), rowIndex + 1);
                }

                return;
            }

            for (var rowIndex = 0; rowIndex < sheet.Rows.Count; rowIndex++)
            {
                for (var columnIndex = 0; columnIndex < sheet.Columns.Count; columnIndex++)
                {
                    AddToken(result, NormalizeToken(ReadText(sheet, rowIndex, columnIndex)), rowIndex + 1);
                }
            }
        }

        private static int? ResolveTokenColumn(DataTable sheet)
        {
            if (sheet == null || sheet.Rows.Count == 0)
            {
                return null;
            }

            for (var columnIndex = 0; columnIndex < sheet.Columns.Count; columnIndex++)
            {
                var header = NormalizeHeader(ReadText(sheet, 0, columnIndex));
                if (IsTokenHeader(header))
                {
                    return columnIndex;
                }
            }

            var bestColumn = -1;
            var bestCount = 0;
            for (var columnIndex = 0; columnIndex < sheet.Columns.Count; columnIndex++)
            {
                var count = 0;
                for (var rowIndex = 0; rowIndex < sheet.Rows.Count; rowIndex++)
                {
                    if (LooksLikeToken(NormalizeToken(ReadText(sheet, rowIndex, columnIndex))))
                    {
                        count++;
                    }
                }

                if (count > bestCount)
                {
                    bestCount = count;
                    bestColumn = columnIndex;
                }
            }

            return bestCount > 0 ? (int?)bestColumn : null;
        }

        private static bool HasRecognizedHeader(DataTable sheet, int tokenColumn)
        {
            return IsTokenHeader(NormalizeHeader(ReadText(sheet, 0, tokenColumn)));
        }

        private static bool IsTokenHeader(string header)
        {
            return header == "token"
                || header == "tokens"
                || header == "cardtoken"
                || header == "cardtokens"
                || header == "cardno"
                || header == "serial"
                || header == "itemserial"
                || header == "توكن"
                || header == "التوكن"
                || header == "الكارت"
                || header == "رقمالكارت"
                || header == "سيريال"
                || header == "السيريال";
        }

        private static void AddToken(PosTokenInvoiceLookupUploadResult result, string token, int rowNumber)
        {
            if (result == null || string.IsNullOrWhiteSpace(token) || !LooksLikeToken(token))
            {
                return;
            }

            result.Summary.UploadedTokensCount++;
            var item = result.Tokens.FirstOrDefault(x => string.Equals(x.Token, token, StringComparison.Ordinal));
            if (item == null)
            {
                item = new PosTokenUploadItem
                {
                    Token = token,
                    FirstRowNumber = rowNumber,
                    UploadedCount = 0
                };
                result.Tokens.Add(item);
            }

            item.UploadedCount++;
        }

        private static void FinalizeSummary(PosTokenInvoiceLookupUploadResult result)
        {
            result.Summary.UniqueTokensCount = result.Tokens.Count;
            result.Summary.DuplicatedInUploadedFileCount = result.Tokens.Count(x => x.UploadedCount > 1);
        }

        private static string NormalizeToken(string value)
        {
            value = ConvertArabicDigits(value);
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            value = Regex.Replace(value.Trim(), @"[\s\-_/\\]+", string.Empty);
            return value.ToUpperInvariant();
        }

        private static bool LooksLikeToken(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            if (IsTokenHeader(NormalizeHeader(value)))
            {
                return false;
            }

            var tokenChars = value.Count(char.IsLetterOrDigit);
            return tokenChars >= 4 && tokenChars == value.Length;
        }

        private static string NormalizeHeader(string value)
        {
            value = ConvertArabicDigits(value);
            value = (value ?? string.Empty).Trim()
                .Replace("\u0640", string.Empty)
                .Replace("أ", "ا")
                .Replace("إ", "ا")
                .Replace("آ", "ا")
                .Replace("ى", "ي")
                .Replace("ة", "ه");
            value = Regex.Replace(value, @"[\s\(\)\/\\_\-]+", string.Empty);
            return value.ToLowerInvariant();
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

            decimal numeric;
            var text = Convert.ToString(value, CultureInfo.InvariantCulture);
            if (decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out numeric) && numeric == decimal.Truncate(numeric))
            {
                return decimal.Truncate(numeric).ToString("0", CultureInfo.InvariantCulture);
            }

            return (text ?? string.Empty).Trim();
        }

        private static string ConvertArabicDigits(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            var builder = new StringBuilder(value.Length);
            foreach (var ch in value)
            {
                if (ch >= '\u0660' && ch <= '\u0669') builder.Append((char)('0' + (ch - '\u0660')));
                else if (ch >= '\u06F0' && ch <= '\u06F9') builder.Append((char)('0' + (ch - '\u06F0')));
                else builder.Append(ch);
            }

            return builder.ToString();
        }
    }
}
