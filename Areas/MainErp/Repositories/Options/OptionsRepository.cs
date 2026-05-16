using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Text;
using MyERP.Areas.MainErp.Interfaces;
using MyERP.Areas.MainErp.ViewModels.Options;

namespace MyERP.Areas.MainErp.Repositories.Options
{
    public class OptionsRepository
    {
        private const string TableName = "TblOptions";
        private readonly IMainErpDbConnectionFactory _connectionFactory;

        public OptionsRepository(IMainErpDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public OptionsIndexViewModel Load()
        {
            using (var connection = _connectionFactory.CreateOpenConnection())
            {
                var columns = LoadColumns(connection);
                var values = LoadRow(connection, columns);
                var fields = columns.Select(x => BuildField(x, values.ContainsKey(x.Name) ? values[x.Name] : null)).ToList();
                var categories = BuildCategories(fields);

                return new OptionsIndexViewModel
                {
                    Categories = categories,
                    Summary = new OptionsSummaryViewModel
                    {
                        CompanyArabicName = GetValue(values, "Company_Arabic_Name"),
                        CompanyEnglishName = GetValue(values, "Company_Name_Eng"),
                        VatRegistrationNumber = GetValue(values, "VATRegNo"),
                        Website = GetValue(values, "WEBSite"),
                        FieldsCount = fields.Count,
                        BooleanFieldsCount = fields.Count(x => x.IsBoolean),
                        EmptyFieldsCount = fields.Count(x => string.IsNullOrWhiteSpace(x.Value))
                    }
                };
            }
        }

        public OptionsSaveResult Save(OptionSaveRequest request)
        {
            if (request == null || request.Fields == null || request.Fields.Count == 0)
            {
                return new OptionsSaveResult { Success = false, Message = "لا توجد بيانات للحفظ." };
            }

            using (var connection = _connectionFactory.CreateOpenConnection())
            using (var transaction = connection.BeginTransaction())
            {
                var rowCount = CountRows(connection, transaction);
                if (rowCount != 1)
                {
                    transaction.Rollback();
                    return new OptionsSaveResult { Success = false, Message = "TblOptions يجب أن يحتوي على صف إعدادات واحد فقط. العدد الحالي: " + rowCount + "." };
                }

                var columns = LoadColumns(connection, transaction).Where(x => IsEditableType(x.DataType)).ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);
                var updates = new List<OptionFieldValueViewModel>();
                foreach (var field in request.Fields)
                {
                    if (field == null || string.IsNullOrWhiteSpace(field.Name) || !columns.ContainsKey(field.Name))
                    {
                        continue;
                    }

                    updates.Add(field);
                }

                if (updates.Count == 0)
                {
                    return new OptionsSaveResult { Success = false, Message = "لا توجد حقول قابلة للحفظ." };
                }

                ValidateEinvoiceSettings(updates);

                var sql = new StringBuilder();
                sql.Append("UPDATE dbo.").Append(QuoteName(TableName)).Append(" SET ");

                var command = new SqlCommand();
                command.Connection = connection;
                command.Transaction = transaction;

                for (var i = 0; i < updates.Count; i++)
                {
                    var column = columns[updates[i].Name];
                    if (i > 0)
                    {
                        sql.Append(", ");
                    }

                    var parameterName = "@p" + i.ToString(CultureInfo.InvariantCulture);
                    sql.Append(QuoteName(column.Name)).Append(" = ").Append(parameterName);
                    command.Parameters.AddWithValue(parameterName, ConvertValue(column, updates[i].Value));
                }

                command.CommandText = sql.ToString();
                var affected = command.ExecuteNonQuery();
                if (affected == 0)
                {
                    transaction.Rollback();
                    return new OptionsSaveResult { Success = false, Message = "لم يتم العثور على صف إعدادات في TblOptions." };
                }

                transaction.Commit();
                return new OptionsSaveResult { Success = true, Message = "تم حفظ إعدادات النظام بنجاح.", UpdatedFields = updates.Count };
            }
        }

        private static IList<OptionCategoryViewModel> BuildCategories(IList<OptionFieldViewModel> fields)
        {
            var definitions = new[]
            {
                Category("company", "بيانات الشركة والفاتورة الإلكترونية", "fas fa-building"),
                Category("inventory", "المخزون والأصناف", "fas fa-boxes"),
                Category("sales", "المبيعات والعملاء", "fas fa-receipt"),
                Category("purchases", "المشتريات والموردين", "fas fa-file-invoice"),
                Category("accounting", "الحسابات والربط المالي", "fas fa-book"),
                Category("hr", "الموظفون والرواتب", "fas fa-users"),
                Category("reports", "الطباعة والتقارير", "fas fa-print"),
                Category("system", "النظام والحماية", "fas fa-shield-alt"),
                Category("advanced", "إعدادات متقدمة", "fas fa-sliders-h")
            };

            var categories = definitions.Select(x => new OptionCategoryViewModel
            {
                Key = x.Key,
                Title = x.Title,
                IconCssClass = x.Icon
            }).ToList();

            foreach (var field in fields.OrderBy(x => x.Ordinal))
            {
                var category = categories.First(x => x.Key == field.CategoryKey);
                category.Fields.Add(field);
            }

            return categories.Where(x => x.Fields.Count > 0).ToList();
        }

        private OptionFieldViewModel BuildField(OptionColumn column, object rawValue)
        {
            var field = new OptionFieldViewModel
            {
                Name = column.Name,
                Label = ResolveLabel(column.Name),
                DataType = column.DataType,
                MaxLength = column.MaxLength,
                IsNullable = column.IsNullable,
                Ordinal = column.Ordinal,
                CategoryKey = ResolveCategory(column.Name),
                Value = FormatValue(rawValue, column.DataType),
                IsBoolean = IsBoolean(column.DataType),
                IsNumber = IsNumber(column.DataType),
                IsDate = IsDate(column.DataType),
                IsLongText = IsLongText(column),
                IsSensitive = IsSensitive(column.Name),
                IsEditable = IsEditableType(column.DataType),
                Choices = ResolveChoices(column.Name)
            };

            field.HelpText = field.IsEditable
                ? "TblOptions." + column.Name + " (" + column.DataType + ")"
                : "حقل ثنائي أو غير قابل للتحرير من هذه الشاشة.";

            return field;
        }

        private IList<OptionColumn> LoadColumns(SqlConnection connection, SqlTransaction transaction = null)
        {
            const string sql = @"
SELECT COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH, IS_NULLABLE, ORDINAL_POSITION
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = @TableName
ORDER BY ORDINAL_POSITION;";

            using (var command = new SqlCommand(sql, connection, transaction))
            {
                command.Parameters.AddWithValue("@TableName", TableName);
                using (var reader = command.ExecuteReader())
                {
                    var columns = new List<OptionColumn>();
                    while (reader.Read())
                    {
                        columns.Add(new OptionColumn
                        {
                            Name = Convert.ToString(reader["COLUMN_NAME"], CultureInfo.InvariantCulture),
                            DataType = Convert.ToString(reader["DATA_TYPE"], CultureInfo.InvariantCulture),
                            MaxLength = reader["CHARACTER_MAXIMUM_LENGTH"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["CHARACTER_MAXIMUM_LENGTH"], CultureInfo.InvariantCulture),
                            IsNullable = string.Equals(Convert.ToString(reader["IS_NULLABLE"], CultureInfo.InvariantCulture), "YES", StringComparison.OrdinalIgnoreCase),
                            Ordinal = Convert.ToInt32(reader["ORDINAL_POSITION"], CultureInfo.InvariantCulture)
                        });
                    }

                    return columns;
                }
            }
        }

        private IDictionary<string, object> LoadRow(SqlConnection connection, IList<OptionColumn> columns)
        {
            var values = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            var selectList = string.Join(", ", columns.Select(x => QuoteName(x.Name)));
            using (var command = new SqlCommand("SELECT TOP (1) " + selectList + " FROM dbo." + QuoteName(TableName), connection))
            using (var reader = command.ExecuteReader())
            {
                if (!reader.Read())
                {
                    return values;
                }

                foreach (var column in columns)
                {
                    values[column.Name] = reader[column.Name] == DBNull.Value ? null : reader[column.Name];
                }
            }

            return values;
        }

        private static int CountRows(SqlConnection connection, SqlTransaction transaction)
        {
            using (var command = new SqlCommand("SELECT COUNT(*) FROM dbo." + QuoteName(TableName), connection, transaction))
            {
                return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture);
            }
        }

        private static void ValidateEinvoiceSettings(IEnumerable<OptionFieldValueViewModel> fields)
        {
            var values = fields.ToDictionary(x => x.Name, x => x.Value ?? "", StringComparer.OrdinalIgnoreCase);
            var applyEinvoice = values.ContainsKey("ApplyEinvoice") && IsTrue(values["ApplyEinvoice"]);
            if (!applyEinvoice)
            {
                return;
            }

            Require(values, "Company_Arabic_Name", "اسم الشركة عربي إلزامي عند تفعيل الفاتورة الإلكترونية.");
            Require(values, "Company_Name_Eng", "اسم الشركة انجليزي إلزامي عند تفعيل الفاتورة الإلكترونية.");
            Require(values, "Company_Comment", "رقم السجل إلزامي عند تفعيل الفاتورة الإلكترونية.");
            RequireLength(values, "VATRegNo", 15, "الرقم الضريبي يجب ألا يقل عن 15 خانة.");
            RequireLength(values, "BuildingNumber", 4, "رقم المبنى يجب ألا يقل عن 4 خانات.");
            RequireLength(values, "PostalZone", 5, "الرمز البريدي يجب ألا يقل عن 5 خانات.");
            Require(values, "StreetName", "اسم الشارع إلزامي عند تفعيل الفاتورة الإلكترونية.");
            RequireLength(values, "IdentificationCode", 2, "كود الدولة يجب ألا يقل عن خانتين.");
            Require(values, "CityName", "المدينة إلزامية عند تفعيل الفاتورة الإلكترونية.");
            Require(values, "CitySubdivisionName", "الحي إلزامي عند تفعيل الفاتورة الإلكترونية.");
        }

        private static void Require(IDictionary<string, string> values, string key, string message)
        {
            if (values.ContainsKey(key) && string.IsNullOrWhiteSpace(values[key]))
            {
                throw new InvalidOperationException(message);
            }
        }

        private static void RequireLength(IDictionary<string, string> values, string key, int length, string message)
        {
            if (values.ContainsKey(key) && (values[key] ?? "").Trim().Length < length)
            {
                throw new InvalidOperationException(message);
            }
        }

        private static object ConvertValue(OptionColumn column, string value)
        {
            var text = (value ?? "").Trim();
            if (string.IsNullOrWhiteSpace(text) && column.IsNullable)
            {
                return DBNull.Value;
            }

            if (IsBoolean(column.DataType))
            {
                return IsTrue(text);
            }

            if (IsInteger(column.DataType))
            {
                if (string.IsNullOrWhiteSpace(text))
                {
                    return 0;
                }

                long integer;
                if (!long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out integer) &&
                    !long.TryParse(text, NumberStyles.Integer, CultureInfo.CurrentCulture, out integer))
                {
                    throw new InvalidOperationException("القيمة الرقمية غير صحيحة في الحقل " + column.Name + ".");
                }

                return integer;
            }

            if (IsDecimal(column.DataType))
            {
                if (string.IsNullOrWhiteSpace(text))
                {
                    return 0m;
                }

                decimal number;
                if (!decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out number) &&
                    !decimal.TryParse(text, NumberStyles.Any, CultureInfo.CurrentCulture, out number))
                {
                    throw new InvalidOperationException("القيمة الرقمية غير صحيحة في الحقل " + column.Name + ".");
                }

                return number;
            }

            if (IsDate(column.DataType))
            {
                if (string.IsNullOrWhiteSpace(text))
                {
                    return column.IsNullable ? (object)DBNull.Value : DateTime.Now.Date;
                }

                DateTime date;
                if (!DateTime.TryParse(text, CultureInfo.CurrentCulture, DateTimeStyles.None, out date) &&
                    !DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
                {
                    throw new InvalidOperationException("التاريخ غير صحيح في الحقل " + column.Name + ".");
                }

                return date;
            }

            if (column.MaxLength.HasValue && column.MaxLength.Value > 0 && text.Length > column.MaxLength.Value)
            {
                throw new InvalidOperationException("قيمة الحقل " + column.Name + " أطول من الحد المسموح (" + column.MaxLength.Value + ").");
            }

            return text;
        }

        private static IList<OptionChoiceViewModel> ResolveChoices(string name)
        {
            if (EqualsName(name, "MainStockCostType"))
            {
                return Choices("0", "المتوسط", "2", "FIFO", "4", "تحديد فواتير", "5", "آخر تكلفة");
            }

            if (EqualsName(name, "InvDate") || EqualsName(name, "PurDate") || EqualsName(name, "CashDate"))
            {
                return Choices("0", "تاريخ الجهاز", "1", "تاريخ آخر حركة", "2", "تاريخ السيرفر");
            }

            if (EqualsName(name, "DateOpt"))
            {
                return Choices("0", "ميلادي", "1", "هجري");
            }

            if (EqualsName(name, "Save_options"))
            {
                return Choices("0", "حفظ فقط", "1", "حفظ ومعاينة", "2", "حفظ وطباعة", "3", "حفظ وطباعة وفتح جديد");
            }

            if (EqualsName(name, "ChasingStatus"))
            {
                return Choices("3", "الكل", "1", "نشط", "2", "متوقف", "7", "مؤجل");
            }

            if (EqualsName(name, "IdentificationCode"))
            {
                return Choices("SA", "SA", "EG", "EG", "AE", "AE");
            }

            return new List<OptionChoiceViewModel>();
        }

        private static IList<OptionChoiceViewModel> Choices(params string[] values)
        {
            var choices = new List<OptionChoiceViewModel>();
            for (var i = 0; i + 1 < values.Length; i += 2)
            {
                choices.Add(new OptionChoiceViewModel { Value = values[i], Text = values[i + 1] });
            }

            return choices;
        }

        private static string ResolveCategory(string name)
        {
            var lower = name.ToLowerInvariant();
            if (lower.StartsWith("company") || lower.Contains("vat") || lower.Contains("zakat") || lower.Contains("einvoice") ||
                lower.Contains("street") || lower.Contains("city") || lower.Contains("postal") || lower.Contains("building") ||
                lower.Contains("csr") || lower.Contains("secretkey") || lower.Contains("publickey") || lower.Contains("privatekey") ||
                lower.Contains("website") || lower.Contains("fax") || lower.Contains("membership") || lower.Contains("computer"))
            {
                return "company";
            }

            if (lower.Contains("stock") || lower.Contains("store") || lower.Contains("item") || lower.Contains("qty") ||
                lower.Contains("barcode") || lower.Contains("cost") || lower.Contains("fifo") || lower.Contains("gard") ||
                lower.Contains("production") || lower.Contains("rawmater"))
            {
                return "inventory";
            }

            if (lower.Contains("sale") || lower.Contains("sales") || lower.Contains("customer") || lower.Contains("client") ||
                lower.Contains("discount") || lower.Contains("cashdate") || lower.Contains("cashing"))
            {
                return "sales";
            }

            if (lower.Contains("pur") || lower.Contains("purchase") || lower.Contains("supplier") || lower.Contains("vendor"))
            {
                return "purchases";
            }

            if (lower.Contains("account") || lower.Contains("bank") || lower.Contains("box") || lower.Contains("cheque") ||
                lower.Contains("currency") || lower.Contains("ked") || lower.Contains("gl") || lower.Contains("jl") ||
                lower.Contains("voucher") || lower.Contains("branch") || lower.Contains("expense"))
            {
                return "accounting";
            }

            if (lower.Contains("emp") || lower.Contains("salary") || lower.Contains("hr") || lower.Contains("vacation") ||
                lower.Contains("finger") || lower.Contains("ekama") || lower.Contains("license"))
            {
                return "hr";
            }

            if (lower.Contains("print") || lower.Contains("report") || lower.Contains("logo") || lower.Contains("zoom") ||
                lower.Contains("image") || lower.Contains("path"))
            {
                return "reports";
            }

            if (lower.Contains("user") || lower.Contains("password") || lower.Contains("domain") || lower.Contains("server") ||
                lower.Contains("dbname") || lower.Contains("demo") || lower.Contains("interface") || lower.Contains("run"))
            {
                return "system";
            }

            return "advanced";
        }

        private static string ResolveLabel(string name)
        {
            var labels = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "Company_Arabic_Name", "اسم الشركة عربي" },
                { "Company_Name_Eng", "اسم الشركة انجليزي" },
                { "Company_Comment", "رقم السجل" },
                { "Company_Address", "العنوان" },
                { "Company_Phone", "الهاتف" },
                { "Company_Mobile", "الجوال" },
                { "Company_Maile", "البريد الإلكتروني" },
                { "Company_Responsable", "اسم المسؤول" },
                { "VATRegNo", "رقم التسجيل VAT" },
                { "WEBSite", "الموقع الإلكتروني" },
                { "Fax", "الفاكس" },
                { "StreetName", "اسم الشارع" },
                { "BuildingNumber", "رقم المبنى" },
                { "CitySubdivisionName", "الحي" },
                { "CityName", "المدينة" },
                { "PostalZone", "الرمز البريدي" },
                { "IdentificationCode", "كود الدولة" },
                { "AdditionalStreetName", "اسم الشارع 2" },
                { "PlotIdentification", "رقم المخطط" },
                { "CountrySubentity", "المدينة الفرعية" },
                { "ApplyEinvoice", "تفعيل الفاتورة الإلكترونية" },
                { "ApplyEinvoiceWithBranch", "الفاتورة الإلكترونية حسب الفرع" },
                { "ApplyEinvoiceWithActive", "الفاتورة الإلكترونية حسب النشاط" },
                { "ShowLogoInReports", "إظهار شعار الشركة في التقارير" },
                { "LogoWidth", "عرض اللوجو" },
                { "Logoheight", "ارتفاع اللوجو" },
                { "ImagesPath", "مسار الصور" },
                { "reportPath", "مسار التقارير" },
                { "InvDate", "تاريخ فاتورة المبيعات الافتراضي" },
                { "PurDate", "تاريخ فاتورة المشتريات الافتراضي" },
                { "CashDate", "تاريخ سند القبض الافتراضي" },
                { "DateOpt", "نوع التاريخ" },
                { "SalesBoxID", "الصندوق الافتراضي للمبيعات" },
                { "MainStockCostType", "طريقة حساب تكلفة المخزون" },
                { "AllowStockNegative", "السحب على المكشوف من المخزن" },
                { "AllowBoxNegative", "السحب على المكشوف من الصندوق" },
                { "CurrencyDigts", "العلامات العشرية للعملة" },
                { "QtyDigts", "العلامات العشرية للكمية" },
                { "PriceDigtsInst", "العلامات العشرية للتقسيط" },
                { "DefaultQtyTrans", "الكمية الافتراضية للشاشات" },
                { "itemSeprator", "فاصل تكويد حقول الأصناف" },
                { "NoBooking", "عدد الحجوزات" },
                { "CountPrint", "عدد مرات الطباعة" },
                { "Save_options", "خيارات الحفظ" },
                { "LimitDefaultCredit", "الحد الائتماني الافتراضي" },
                { "LimitDefaultCreditDays", "مدة الائتمان الافتراضية" }
            };

            if (labels.ContainsKey(name))
            {
                return labels[name];
            }

            return name.Replace("_", " ");
        }

        private static string FormatValue(object value, string dataType)
        {
            if (value == null)
            {
                return "";
            }

            if (IsBoolean(dataType))
            {
                return Convert.ToBoolean(value, CultureInfo.InvariantCulture) ? "true" : "false";
            }

            if (value is DateTime)
            {
                return ((DateTime)value).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            }

            if (value is byte[])
            {
                return "";
            }

            return Convert.ToString(value, CultureInfo.InvariantCulture);
        }

        private static string GetValue(IDictionary<string, object> values, string key)
        {
            return values.ContainsKey(key) && values[key] != null ? Convert.ToString(values[key], CultureInfo.InvariantCulture) : "";
        }

        private static bool IsEditableType(string dataType)
        {
            var type = (dataType ?? "").ToLowerInvariant();
            return type != "image" && type != "binary" && type != "varbinary" && type != "timestamp" && type != "rowversion";
        }

        private static bool IsBoolean(string dataType)
        {
            return string.Equals(dataType, "bit", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsNumber(string dataType)
        {
            return IsInteger(dataType) || IsDecimal(dataType);
        }

        private static bool IsInteger(string dataType)
        {
            var type = (dataType ?? "").ToLowerInvariant();
            return type == "int" || type == "bigint" || type == "smallint" || type == "tinyint";
        }

        private static bool IsDecimal(string dataType)
        {
            var type = (dataType ?? "").ToLowerInvariant();
            return type == "decimal" || type == "numeric" || type == "money" || type == "smallmoney" || type == "float" || type == "real";
        }

        private static bool IsDate(string dataType)
        {
            var type = (dataType ?? "").ToLowerInvariant();
            return type == "datetime" || type == "smalldatetime" || type == "date" || type == "datetime2";
        }

        private static bool IsLongText(OptionColumn column)
        {
            var type = (column.DataType ?? "").ToLowerInvariant();
            return type == "text" || type == "ntext" || column.MaxLength == -1 || (column.MaxLength.HasValue && column.MaxLength.Value > 500);
        }

        private static bool IsSensitive(string name)
        {
            var lower = (name ?? "").ToLowerInvariant();
            return lower.Contains("password") || lower.Contains("secret") || lower.Contains("privatekey") || lower.Contains("publickey") || lower.Contains("csr");
        }

        private static bool IsTrue(string value)
        {
            var text = (value ?? "").Trim().ToLowerInvariant();
            return text == "true" || text == "1" || text == "on" || text == "yes";
        }

        private static bool EqualsName(string source, string target)
        {
            return string.Equals(source, target, StringComparison.OrdinalIgnoreCase);
        }

        private static string QuoteName(string name)
        {
            return "[" + (name ?? "").Replace("]", "]]") + "]";
        }

        private static OptionCategoryDefinition Category(string key, string title, string icon)
        {
            return new OptionCategoryDefinition { Key = key, Title = title, Icon = icon };
        }

        private class OptionColumn
        {
            public string Name { get; set; }
            public string DataType { get; set; }
            public int? MaxLength { get; set; }
            public bool IsNullable { get; set; }
            public int Ordinal { get; set; }
        }

        private class OptionCategoryDefinition
        {
            public string Key { get; set; }
            public string Title { get; set; }
            public string Icon { get; set; }
        }
    }
}
