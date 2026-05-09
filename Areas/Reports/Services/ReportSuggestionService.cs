using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using MyERP.Areas.Reports.Models;

namespace MyERP.Areas.Reports.Services
{
    public class ReportSuggestionService
    {
        private static readonly Regex WordRegex = new Regex(@"([A-Z]?[a-z]+|[A-Z]+(?![a-z])|\d+)", RegexOptions.Compiled);

        private static readonly Dictionary<string, string> ArDictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "Id", "رقم" }, { "No", "رقم" }, { "Number", "رقم" }, { "Code", "الرمز" },
            { "At", "" }, { "Is", "" }, { "Has", "" },
            { "Name", "الاسم" }, { "Ar", "عربي" }, { "En", "إنجليزي" }, { "Desc", "الوصف" },
            { "Description", "الوصف" }, { "Type", "النوع" }, { "Status", "الحالة" }, { "State", "الحالة" },
            { "Invoice", "الفاتورة" }, { "Bill", "الفاتورة" }, { "Voucher", "القيد" }, { "Receipt", "إيصال" },
            { "Payment", "دفع" }, { "Order", "طلب" }, { "Sale", "بيع" }, { "Sales", "المبيعات" },
            { "Purchase", "شراء" }, { "Return", "مرتجع" }, { "Customer", "العميل" }, { "Client", "العميل" },
            { "Supplier", "المورد" }, { "Vendor", "المورد" }, { "Product", "المنتج" }, { "Item", "الصنف" },
            { "Branch", "الفرع" }, { "Store", "المخزن" }, { "Warehouse", "المخزن" }, { "Company", "الشركة" },
            { "Account", "الحساب" }, { "Bank", "البنك" }, { "Cash", "النقدية" }, { "Treasury", "الخزينة" },
            { "Date", "التاريخ" }, { "Time", "الوقت" }, { "Created", "تاريخ الإنشاء" }, { "Updated", "تاريخ التعديل" },
            { "From", "من" }, { "To", "إلى" }, { "Start", "البداية" }, { "End", "النهاية" },
            { "Total", "الإجمالي" }, { "Subtotal", "الفرعي" }, { "Net", "الصافي" }, { "Gross", "الإجمالي" },
            { "Tax", "الضريبة" }, { "Vat", "ضريبة القيمة المضافة" }, { "Discount", "الخصم" }, { "Amount", "المبلغ" },
            { "Value", "القيمة" }, { "Balance", "الرصيد" }, { "Remain", "المتبقي" }, { "Paid", "المدفوع" },
            { "Due", "المستحق" }, { "Quantity", "الكمية" }, { "Qty", "الكمية" }, { "Price", "السعر" },
            { "Cost", "التكلفة" }, { "Currency", "العملة" }, { "Rate", "المعدل" }, { "Percent", "النسبة" },
            { "User", "المستخدم" }, { "Employee", "الموظف" }, { "Salesman", "المندوب" }, { "Driver", "السائق" },
            { "Phone", "الهاتف" }, { "Mobile", "الجوال" }, { "Email", "البريد الإلكتروني" }, { "Address", "العنوان" },
            { "City", "المدينة" }, { "Area", "المنطقة" }, { "Category", "الفئة" }, { "Group", "المجموعة" },
            { "Class", "التصنيف" }, { "Level", "المستوى" }, { "Serial", "السيريال" }, { "Batch", "التشغيلة" },
            { "Unit", "الوحدة" }, { "Weight", "الوزن" }, { "Size", "الحجم" }, { "Color", "اللون" },
            { "Ref", "المرجع" }, { "Reference", "المرجع" }, { "Notes", "ملاحظات" }, { "Note", "ملاحظة" },
            { "Active", "نشط" }, { "Deleted", "محذوف" }, { "Approved", "معتمد" }, { "Posted", "مرحل" },
            { "Open", "مفتوح" }, { "Closed", "مغلق" }, { "Cancel", "إلغاء" }, { "Cancelled", "ملغي" },
            { "Year", "السنة" }, { "Month", "الشهر" }, { "Day", "اليوم" }, { "Period", "الفترة" },
            { "Contract", "العقد" }, { "Project", "المشروع" }, { "Department", "القسم" }, { "Location", "الموقع" },
            { "Point", "نقطة" }, { "Shift", "الوردية" }, { "Table", "الطاولة" }, { "Service", "الخدمة" },
            { "Delivery", "التوصيل" }, { "Expense", "المصروف" }, { "Revenue", "الإيراد" }, { "Profit", "الربح" }
        };

        public int DictionarySize
        {
            get { return ArDictionary.Count; }
        }

        public SuggestionBundle BuildSuggestions(DynamicReportDefinition definition)
        {
            var bundle = new SuggestionBundle();
            var columns = definition == null || definition.Columns == null ? new List<DynamicReportColumn>() : definition.Columns.ToList();
            foreach (var column in columns)
            {
                if (column == null || string.IsNullOrWhiteSpace(column.FieldName)) continue;
                bundle.CaptionsAr[column.FieldName] = SuggestArabicCaption(column.FieldName);
                var numeric = SuggestNumericFormat(column.DataType, column.FieldName);
                if (numeric != null) bundle.Formatting[column.FieldName] = numeric;
                var date = SuggestDateFormat(column.DataType);
                if (date != null) bundle.Formatting[column.FieldName] = date;
                if (SuggestGroupable(column)) bundle.GroupableHints.Add(column.FieldName);
            }

            foreach (var hint in SuggestSort(columns))
            {
                bundle.SortHints.Add(hint);
            }

            return bundle;
        }

        public string SuggestArabicCaption(string fieldName)
        {
            var words = SplitWords(fieldName).ToList();
            if (words.Count == 0) return string.Empty;
            var translated = 0;
            var output = new List<string>();
            foreach (var word in words)
            {
                string value;
                if (ArDictionary.TryGetValue(word, out value))
                {
                    translated++;
                    output.Add(value);
                }
                else
                {
                    output.Add(word);
                }
            }

            var cleaned = output.Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
            if (cleaned.Count > 1 && (cleaned[cleaned.Count - 1] == "رقم" || cleaned[cleaned.Count - 1] == "الاسم"))
            {
                var last = cleaned[cleaned.Count - 1];
                if (last == "الاسم") last = "اسم";
                cleaned.RemoveAt(cleaned.Count - 1);
                cleaned.Insert(0, last);
            }

            var caption = string.Join(" ", cleaned).Trim();
            if (translated * 2 < words.Count)
            {
                caption = "⚠ " + ToTitleWords(words);
            }

            return caption.Length > 100 ? caption.Substring(0, 100) : caption;
        }

        public ColumnFormatting SuggestNumericFormat(string dataType, string fieldName)
        {
            if ((fieldName ?? string.Empty).IndexOf("Percent", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return new ColumnFormatting { Format = "0.00%", Decimals = 2 };
            }

            if (IsDecimal(dataType)) return new ColumnFormatting { Format = "#,##0.00", Decimals = 2 };
            if (IsInteger(dataType)) return new ColumnFormatting { Format = "#,##0", Decimals = 0 };
            return null;
        }

        public ColumnFormatting SuggestDateFormat(string dataType)
        {
            if (string.Equals(dataType, "Date", StringComparison.OrdinalIgnoreCase)) return new ColumnFormatting { Format = "yyyy-MM-dd" };
            if (string.Equals(dataType, "DateTime", StringComparison.OrdinalIgnoreCase)) return new ColumnFormatting { Format = "yyyy-MM-dd HH:mm" };
            return null;
        }

        public IList<SortHint> SuggestSort(IEnumerable<DynamicReportColumn> columns)
        {
            var list = (columns ?? new List<DynamicReportColumn>()).Where(c => c != null && !string.IsNullOrWhiteSpace(c.FieldName)).OrderBy(c => c.SortOrder).ToList();
            var dateColumn = list.FirstOrDefault(c => ContainsAny(c.FieldName, "Created", "Date", "Time"));
            if (dateColumn != null) return new List<SortHint> { new SortHint { Field = dateColumn.FieldName, Direction = "desc" } };
            var first = list.FirstOrDefault();
            return first == null ? new List<SortHint>() : new List<SortHint> { new SortHint { Field = first.FieldName, Direction = "asc" } };
        }

        public bool SuggestGroupable(DynamicReportColumn column)
        {
            if (column == null) return false;
            if (ContainsAny(column.FieldName, "Status", "Type", "Branch", "Category")) return true;
            return string.Equals(column.DataType, "Bool", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(column.DataType, "tinyint", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(column.DataType, "smallint", StringComparison.OrdinalIgnoreCase);
        }

        private static IEnumerable<string> SplitWords(string fieldName)
        {
            var normalized = (fieldName ?? string.Empty).Replace("_", " ").Replace("-", " ");
            foreach (var part in normalized.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var matches = WordRegex.Matches(part);
                if (matches.Count == 0) yield return part;
                foreach (Match match in matches) yield return match.Value;
            }
        }

        private static string ToTitleWords(IEnumerable<string> words)
        {
            var builder = new StringBuilder();
            foreach (var word in words)
            {
                if (builder.Length > 0) builder.Append(' ');
                builder.Append(word);
            }
            return builder.ToString();
        }

        private static bool ContainsAny(string value, params string[] needles)
        {
            value = value ?? string.Empty;
            return needles.Any(n => value.IndexOf(n, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static bool IsDecimal(string dataType)
        {
            return string.Equals(dataType, "Decimal", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(dataType, "Money", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(dataType, "Numeric", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(dataType, "Double", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsInteger(string dataType)
        {
            return string.Equals(dataType, "Int", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(dataType, "BigInt", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(dataType, "Short", StringComparison.OrdinalIgnoreCase);
        }
    }
}
