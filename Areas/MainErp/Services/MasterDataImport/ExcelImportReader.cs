using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using ExcelDataReader;
using MyERP.Areas.MainErp.ViewModels.MasterDataImport;

namespace MyERP.Areas.MainErp.Services.MasterDataImport
{
    public class ExcelImportReader
    {
        private static readonly string[] EmptyValues = new[] { null, "" };

        public IList<MasterDataImportRowViewModel> ReadChartOfAccounts(string filePath)
        {
            var table = ReadFirstWorksheet(filePath);
            var rows = new List<MasterDataImportRowViewModel>();

            var legacyTawjerLayout = LooksLikeLegacyTawjerAccountTree(table);
            var lastCodeByLevel = new Dictionary<int, string>();
            var lastSerialByLevel = new Dictionary<int, string>();

            for (var i = 0; i < table.Rows.Count; i++)
            {
                var dataRow = table.Rows[i];
                if (IsEmptyRow(dataRow))
                {
                    continue;
                }

                MasterDataImportRowViewModel row;
                if (legacyTawjerLayout)
                {
                    row = ReadLegacyTawjerRow(dataRow, i + 2, lastSerialByLevel);
                }
                else
                {
                    row = new MasterDataImportRowViewModel
                    {
                        RowNumber = i + 2,
                        AccountCode = Get(dataRow, "Account_Code", "Account Code", "كود الحساب الداخلي"),
                        AccountSerial = Get(dataRow, "Account_Serial", "Account Serial", "رقم الحساب", "كود الحساب"),
                        AccountName = Get(dataRow, "Account_Name", "Account Name", "اسم الحساب", "الاسم العربي"),
                        AccountNameEnglish = Get(dataRow, "Account_NameEng", "English Name", "الاسم الانجليزي"),
                        ParentAccountCode = Get(dataRow, "Parent_Account_Code", "Parent Account Code", "كود الحساب الاب الداخلي"),
                        ParentAccountSerial = Get(dataRow, "Parent_Account_Serial", "Parent Serial", "رقم حساب الاب", "الحساب الاب"),
                        IsFinalAccount = ParseBoolean(Get(dataRow, "last_account", "Is Final", "Final", "حساب نهائي"), true),
                        Level = ParseInt(Get(dataRow, "Level", "Account Level", "المستوى")),
                        CurrencyCode = Get(dataRow, "currenct_code", "Currency Code", "كود العملة")
                    };
                }

                if (!string.IsNullOrWhiteSpace(row.AccountCode) && row.Level.HasValue)
                {
                    lastCodeByLevel[row.Level.Value] = row.AccountCode;
                }

                rows.Add(row);
            }

            return rows;
        }

        public IList<MasterDataImportRowViewModel> ReadAccountBalanceMasterFile(string filePath, string entityType)
        {
            var table = ReadFirstWorksheet(filePath);
            var rows = new List<MasterDataImportRowViewModel>();
            var accountBalanceLayout = LooksLikeLegacyAccountBalanceReport(table);

            for (var i = 0; i < table.Rows.Count; i++)
            {
                var dataRow = table.Rows[i];
                if (IsEmptyRow(dataRow))
                {
                    continue;
                }

                if (accountBalanceLayout)
                {
                    var serialFromReport = GetByIndex(dataRow, 7);
                    var nameFromReport = GetByIndex(dataRow, 6);
                    if (IsLikelyHeader(serialFromReport, nameFromReport))
                    {
                        continue;
                    }

                    var signedBalance = ParseSignedBalance(GetByIndex(dataRow, 5));
                    rows.Add(new MasterDataImportRowViewModel
                    {
                        RowNumber = i + 2,
                        EntityType = entityType,
                        EntityCode = serialFromReport,
                        EntityName = nameFromReport,
                        AccountSerial = serialFromReport,
                        AccountName = nameFromReport,
                        OpeningBalance = signedBalance.HasValue ? Math.Abs(signedBalance.Value) : (decimal?)null,
                        OpeningBalanceType = ParseBalanceTypeFromLabel(GetByIndex(dataRow, 4), GetByIndex(dataRow, 0), signedBalance),
                        CurrencyCode = "1"
                    });
                    continue;
                }

                var serial = Get(dataRow, "Account_Serial", "Account Serial", "رقم الحساب", "كود الحساب");
                var name = Get(dataRow, "Account_Name", "Account Name", "اسم الحساب", "الاسم العربي");
                if (string.IsNullOrWhiteSpace(serial) && table.Columns.Count >= 8)
                {
                    serial = GetByIndex(dataRow, 7);
                }

                if (string.IsNullOrWhiteSpace(name) && table.Columns.Count >= 7)
                {
                    name = GetByIndex(dataRow, 6);
                }

                if (IsLikelyHeader(serial, name))
                {
                    continue;
                }

                rows.Add(new MasterDataImportRowViewModel
                {
                    RowNumber = i + 2,
                    EntityType = entityType,
                    EntityCode = serial,
                    EntityName = name,
                    AccountSerial = serial,
                    AccountName = name,
                    OpeningBalance = ParseDecimal(GetByIndex(dataRow, 1)),
                    OpeningBalanceType = ParseBalanceType(GetByIndex(dataRow, 1), GetByIndex(dataRow, 0)),
                    CurrencyCode = "1"
                });
            }

            return rows;
        }

        private static DataTable ReadFirstWorksheet(string filePath)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = ExcelReaderFactory.CreateReader(stream))
            {
                var table = new DataTable();
                var headersReady = false;
                var columnCount = reader.FieldCount;

                while (reader.Read())
                {
                    if (!headersReady)
                    {
                        for (var i = 0; i < columnCount; i++)
                        {
                            var header = SafeCellText(reader.GetValue(i));
                            if (string.IsNullOrWhiteSpace(header))
                            {
                                header = "Column" + (i + 1).ToString(CultureInfo.InvariantCulture);
                            }

                            if (table.Columns.Contains(header))
                            {
                                header = header + "_" + (i + 1).ToString(CultureInfo.InvariantCulture);
                            }

                            table.Columns.Add(header, typeof(string));
                        }

                        headersReady = true;
                        continue;
                    }

                    var row = table.NewRow();
                    for (var i = 0; i < columnCount; i++)
                    {
                        row[i] = SafeCellText(reader.GetValue(i));
                    }

                    table.Rows.Add(row);
                }

                if (!headersReady)
                {
                    throw new InvalidOperationException("No worksheet was found in the uploaded Excel file.");
                }

                return table;
            }
        }

        private static string SafeCellText(object value)
        {
            if (value == null || value == DBNull.Value)
            {
                return string.Empty;
            }

            if (value is DateTime)
            {
                return ((DateTime)value).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            }

            if (value is IFormattable)
            {
                return ((IFormattable)value).ToString(null, CultureInfo.InvariantCulture);
            }

            return Convert.ToString(value, CultureInfo.InvariantCulture);
        }

        private static string Get(DataRow row, params string[] names)
        {
            foreach (var name in names)
            {
                var column = row.Table.Columns.Cast<DataColumn>()
                    .FirstOrDefault(c => string.Equals(Normalize(c.ColumnName), Normalize(name), StringComparison.OrdinalIgnoreCase));

                if (column == null)
                {
                    continue;
                }

                return Clean(Convert.ToString(row[column]));
            }

            return string.Empty;
        }

        private static MasterDataImportRowViewModel ReadLegacyTawjerRow(DataRow dataRow, int rowNumber, IDictionary<int, string> lastSerialByLevel)
        {
            var level = ParseInt(GetByIndex(dataRow, 2));
            var accountKind = GetByIndex(dataRow, 3);
            var serial = GetByIndex(dataRow, 6);
            var row = new MasterDataImportRowViewModel
            {
                RowNumber = rowNumber,
                AccountSerial = serial,
                AccountName = GetByIndex(dataRow, 5),
                ParentAccountSerial = level.HasValue && level.Value > 1 && lastSerialByLevel.ContainsKey(level.Value - 1)
                    ? lastSerialByLevel[level.Value - 1]
                    : string.Empty,
                IsFinalAccount = accountKind.IndexOf("فرعي", StringComparison.OrdinalIgnoreCase) >= 0,
                Level = level,
                CurrencyCode = "1"
            };

            if (level.HasValue && !string.IsNullOrWhiteSpace(serial))
            {
                lastSerialByLevel[level.Value] = serial;
            }

            return row;
        }

        private static bool LooksLikeLegacyTawjerAccountTree(DataTable table)
        {
            if (table.Columns.Count < 7 || table.Rows.Count == 0)
            {
                return false;
            }

            var hits = 0;
            foreach (DataRow row in table.Rows.Cast<DataRow>().Take(10))
            {
                var kind = GetByIndex(row, 3);
                var name = GetByIndex(row, 5);
                var serial = GetByIndex(row, 6);
                if ((kind == "رئيسي" || kind == "فرعي") && !string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(serial))
                {
                    hits++;
                }
            }

            return hits >= 2;
        }

        private static bool LooksLikeLegacyAccountBalanceReport(DataTable table)
        {
            if (table.Columns.Count < 8 || table.Rows.Count == 0)
            {
                return false;
            }

            var hits = 0;
            foreach (DataRow row in table.Rows.Cast<DataRow>().Take(20))
            {
                var name = GetByIndex(row, 6);
                var serial = GetByIndex(row, 7);
                if (!string.IsNullOrWhiteSpace(name) && LooksLikeNumericCode(serial))
                {
                    hits++;
                }
            }

            return hits >= 2;
        }

        private static string GetByIndex(DataRow row, int index)
        {
            return row.Table.Columns.Count > index ? Clean(Convert.ToString(row[index])) : string.Empty;
        }

        private static bool IsEmptyRow(DataRow row)
        {
            return row.ItemArray.All(v => EmptyValues.Contains(Clean(Convert.ToString(v))));
        }

        private static string Clean(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            return value.Trim().Trim('\u200f', '\u200e');
        }

        private static string Normalize(string value)
        {
            return Clean(value).Replace(" ", string.Empty).Replace("_", string.Empty).Replace("-", string.Empty);
        }

        private static int? ParseInt(string value)
        {
            int result;
            return int.TryParse(value, out result) ? result : (int?)null;
        }

        private static bool ParseBoolean(string value, bool defaultValue)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return defaultValue;
            }

            value = value.Trim().ToLowerInvariant();
            return value == "1" || value == "true" || value == "yes" || value == "y" || value == "نهائي" || value == "حساب نهائي";
        }

        private static decimal? ParseDecimal(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            decimal result;
            return decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out result) ? result : (decimal?)null;
        }

        private static decimal? ParseSignedBalance(string value)
        {
            return ParseDecimal(value);
        }

        private static int? ParseBalanceType(string debit, string credit)
        {
            var debitValue = ParseDecimal(debit).GetValueOrDefault();
            var creditValue = ParseDecimal(credit).GetValueOrDefault();
            if (debitValue > 0)
            {
                return 0;
            }

            if (creditValue > 0)
            {
                return 1;
            }

            return null;
        }

        private static int? ParseBalanceTypeFromLabel(string primaryLabel, string fallbackLabel, decimal? signedBalance)
        {
            var label = Clean(primaryLabel);
            if (string.IsNullOrWhiteSpace(label))
            {
                label = Clean(fallbackLabel);
            }

            if (IsDebitLabel(label))
            {
                return 0;
            }

            if (IsCreditLabel(label))
            {
                return 1;
            }

            if (signedBalance.HasValue)
            {
                return signedBalance.Value < 0 ? 1 : 0;
            }

            return null;
        }

        private static bool LooksLikeNumericCode(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            return value.All(c => char.IsDigit(c) || c == '.' || c == '-' || c == '+');
        }

        private static bool IsDebitLabel(string value)
        {
            return value == "مدين" || value == "ظ…ط¯ظٹظ†" || string.Equals(value, "debit", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsCreditLabel(string value)
        {
            return value == "دائن" || value == "ط¯ط§ط¦ظ†" || string.Equals(value, "credit", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsLikelyHeader(string serial, string name)
        {
            var combined = (serial + " " + name).ToLowerInvariant();
            return combined.Contains("account") || combined.Contains("serial") || combined.Contains("الحساب") || combined.Contains("اسم");
        }

        public byte[] BuildTemplate()
        {
            var headers = new[]
            {
                "Account_Serial",
                "Account_Name",
                "Account_NameEng",
                "Parent_Account_Serial",
                "Account_Code",
                "Parent_Account_Code",
                "Level",
                "last_account",
                "currenct_code"
            };

            return BuildExcelHtml(headers, new[]
            {
                new[] { "1000", "الأصول", "Assets", "", "a1", "r", "1", "0", "1" },
                new[] { "1010", "الصندوق", "Cash", "1000", "", "", "2", "1", "1" }
            });
        }

        public byte[] BuildAccountLinkedTemplate(string entityType)
        {
            var headers = new[]
            {
                "Debit",
                "Credit",
                "Phone",
                "Mobile",
                "Email",
                "Notes",
                "Name",
                "Account_Serial"
            };

            var name = entityType == MasterDataImportEntityType.Employees
                ? "موظف تجريبي"
                : entityType == MasterDataImportEntityType.Suppliers
                    ? "مورد تجريبي"
                    : "عميل تجريبي";

            return BuildExcelHtml(headers, new[]
            {
                new[] { "0", "0", "", "", "", "", name, "110101" }
            });
        }

        public byte[] BuildErrorReport(IList<MasterDataImportRowViewModel> rows)
        {
            var headers = new[]
            {
                "RowNumber",
                "Account_Serial",
                "EntityCode",
                "EntityName",
                "Account_Name",
                "Parent_Account_Serial",
                "Account_Code",
                "Parent_Account_Code",
                "IsValid",
                "Errors"
            };

            var data = rows.Select(r => new[]
            {
                r.RowNumber.ToString(),
                r.AccountSerial,
                r.EntityCode,
                r.EntityName,
                r.AccountName,
                r.ParentAccountSerial,
                r.AccountCode,
                r.ParentAccountCode,
                r.IsValid ? "Valid" : "Error",
                r.ErrorDetails
            });

            return BuildExcelHtml(headers, data);
        }

        private static byte[] BuildExcelHtml(IEnumerable<string> headers, IEnumerable<IEnumerable<string>> rows)
        {
            var sb = new StringBuilder();
            sb.Append("<html><head><meta charset=\"utf-8\" /></head><body><table border=\"1\"><tr>");
            foreach (var header in headers)
            {
                sb.Append("<th>").Append(Escape(header)).Append("</th>");
            }
            sb.Append("</tr>");
            foreach (var row in rows)
            {
                sb.Append("<tr>");
                foreach (var cell in row)
                {
                    sb.Append("<td style=\"mso-number-format:'\\@';\">").Append(Escape(cell)).Append("</td>");
                }
                sb.Append("</tr>");
            }
            sb.Append("</table></body></html>");
            return Encoding.UTF8.GetBytes(sb.ToString());
        }

        private static string Escape(string value)
        {
            return System.Web.HttpUtility.HtmlEncode(value ?? string.Empty);
        }
    }
}
