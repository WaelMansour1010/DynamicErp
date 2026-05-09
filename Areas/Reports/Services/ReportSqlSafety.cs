using System;
using System.Collections.Generic;
using System.Data;
using System.Text.RegularExpressions;

namespace MyERP.Areas.Reports.Services
{
    public static class ReportSqlSafety
    {
        private static readonly Regex ObjectNameRegex = new Regex(@"^[A-Za-z_][A-Za-z0-9_]*(\.[A-Za-z_][A-Za-z0-9_]*)?$", RegexOptions.Compiled);
        private static readonly Regex ParameterRegex = new Regex(@"^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.Compiled);
        private static readonly ISet<string> DataTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "String", "Int", "Decimal", "DateTime", "Date", "Bool", "Guid"
        };

        public static void ValidateSourceName(string sourceName)
        {
            if (string.IsNullOrWhiteSpace(sourceName) || !ObjectNameRegex.IsMatch(sourceName.Trim()))
            {
                throw new InvalidOperationException("Invalid report source name.");
            }
        }

        public static string QuoteObjectName(string sourceName)
        {
            ValidateSourceName(sourceName);
            var parts = sourceName.Trim().Split('.');
            return parts.Length == 1 ? "[dbo].[" + parts[0] + "]" : "[" + parts[0] + "].[" + parts[1] + "]";
        }

        public static string NormalizeParameterName(string parameterName)
        {
            var value = (parameterName ?? string.Empty).Trim().TrimStart('@');
            if (!ParameterRegex.IsMatch(value))
            {
                throw new InvalidOperationException("Invalid report parameter name.");
            }

            return value;
        }

        public static string NormalizeDataType(string dataType)
        {
            return DataTypes.Contains(dataType ?? string.Empty) ? dataType : "String";
        }

        public static SqlDbType ToSqlDbType(string dataType)
        {
            var type = NormalizeDataType(dataType);
            if (string.Equals(type, "Int", StringComparison.OrdinalIgnoreCase)) return SqlDbType.Int;
            if (string.Equals(type, "Decimal", StringComparison.OrdinalIgnoreCase)) return SqlDbType.Decimal;
            if (string.Equals(type, "Date", StringComparison.OrdinalIgnoreCase)) return SqlDbType.Date;
            if (string.Equals(type, "DateTime", StringComparison.OrdinalIgnoreCase)) return SqlDbType.DateTime;
            if (string.Equals(type, "Bool", StringComparison.OrdinalIgnoreCase)) return SqlDbType.Bit;
            if (string.Equals(type, "Guid", StringComparison.OrdinalIgnoreCase)) return SqlDbType.UniqueIdentifier;
            return SqlDbType.NVarChar;
        }

        public static object ConvertValue(string value, string dataType)
        {
            if (string.IsNullOrWhiteSpace(value)) return DBNull.Value;
            var type = NormalizeDataType(dataType);
            int intValue; decimal decimalValue; DateTime dateValue; bool boolValue; Guid guidValue;
            if (type == "Int") return int.TryParse(value, out intValue) ? (object)intValue : DBNull.Value;
            if (type == "Decimal") return decimal.TryParse(value, out decimalValue) ? (object)decimalValue : DBNull.Value;
            if (type == "Date" || type == "DateTime") return DateTime.TryParse(value, out dateValue) ? (object)dateValue : DBNull.Value;
            if (type == "Bool") return bool.TryParse(value, out boolValue) ? (object)boolValue : value == "1";
            if (type == "Guid") return Guid.TryParse(value, out guidValue) ? (object)guidValue : DBNull.Value;
            return value;
        }
    }
}
