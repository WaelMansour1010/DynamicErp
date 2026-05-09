using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using MyERP.Areas.Reports.Models;

namespace MyERP.Areas.Reports.Services
{
    public class ReportMetadataService
    {
        private readonly DynamicReportConnectionFactory _connectionFactory;

        public ReportMetadataService()
            : this(new DynamicReportConnectionFactory())
        {
        }

        public ReportMetadataService(DynamicReportConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public IList<DynamicReportColumn> LoadColumns(string scope, string sourceType, string sourceName)
        {
            ReportSqlSafety.ValidateSourceName(sourceName);
            var columns = new List<DynamicReportColumn>();
            using (var connection = _connectionFactory.CreateOpenConnection(scope))
            using (var command = CreateSchemaCommand(connection, sourceType, sourceName))
            using (var reader = command.ExecuteReader(CommandBehavior.SchemaOnly | CommandBehavior.KeyInfo))
            {
                var schema = reader.GetSchemaTable();
                if (schema == null) return columns;
                var order = 0;
                foreach (DataRow row in schema.Rows)
                {
                    var fieldName = Convert.ToString(row["ColumnName"]);
                    var dataType = row["DataType"] as Type;
                    if (string.IsNullOrWhiteSpace(fieldName)) continue;
                    columns.Add(new DynamicReportColumn
                    {
                        FieldName = fieldName,
                        CaptionAr = fieldName,
                        CaptionEn = fieldName,
                        DataType = dataType != null ? dataType.Name : "String",
                        IsVisibleDefault = true,
                        IsFilterable = true,
                        IsSortable = true,
                        IsGroupable = false,
                        IsSummable = IsNumeric(dataType),
                        Width = 140,
                        SortOrder = order++
                    });
                }
            }
            return columns;
        }

        public IList<DynamicReportParameter> LoadStoredProcedureParameters(string scope, string sourceType, string sourceName)
        {
            var list = new List<DynamicReportParameter>();
            if (!string.Equals(sourceType, "StoredProcedure", StringComparison.OrdinalIgnoreCase)) return list;
            ReportSqlSafety.ValidateSourceName(sourceName);
            var objectName = sourceName.IndexOf(".", StringComparison.Ordinal) > 0 ? sourceName : "dbo." + sourceName;
            const string sql = @"
SELECT p.name, t.name AS TypeName, p.is_output
FROM sys.parameters p
INNER JOIN sys.types t ON p.user_type_id = t.user_type_id
WHERE p.object_id = OBJECT_ID(@ObjectName)
ORDER BY p.parameter_id;";
            using (var connection = _connectionFactory.CreateOpenConnection(scope))
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.Add("@ObjectName", SqlDbType.NVarChar, 256).Value = objectName;
                using (var reader = command.ExecuteReader())
                {
                    var order = 0;
                    while (reader.Read())
                    {
                        if (Convert.ToBoolean(reader["is_output"])) continue;
                        var name = Convert.ToString(reader["name"]);
                        list.Add(new DynamicReportParameter
                        {
                            ParameterName = ReportSqlSafety.NormalizeParameterName(name),
                            CaptionAr = name,
                            CaptionEn = name,
                            DataType = MapSqlType(Convert.ToString(reader["TypeName"])),
                            IsRequired = false,
                            SortOrder = order++
                        });
                    }
                }
            }
            return list;
        }

        private static SqlCommand CreateSchemaCommand(SqlConnection connection, string sourceType, string sourceName)
        {
            if (string.Equals(sourceType, "View", StringComparison.OrdinalIgnoreCase))
            {
                return new SqlCommand("SELECT TOP (0) * FROM " + ReportSqlSafety.QuoteObjectName(sourceName), connection);
            }

            var command = new SqlCommand(ReportSqlSafety.QuoteObjectName(sourceName), connection);
            command.CommandType = CommandType.StoredProcedure;
            try
            {
                SqlCommandBuilder.DeriveParameters(command);
                foreach (SqlParameter parameter in command.Parameters)
                {
                    if (parameter.Direction == ParameterDirection.Input || parameter.Direction == ParameterDirection.InputOutput)
                    {
                        parameter.Value = DBNull.Value;
                    }
                }
            }
            catch
            {
                command.Parameters.Clear();
            }
            return command;
        }

        private static bool IsNumeric(Type type)
        {
            return type == typeof(decimal) || type == typeof(double) || type == typeof(float) ||
                   type == typeof(int) || type == typeof(long) || type == typeof(short);
        }

        private static string MapSqlType(string sqlType)
        {
            if (sqlType == "int" || sqlType == "bigint" || sqlType == "smallint") return "Int";
            if (sqlType == "decimal" || sqlType == "numeric" || sqlType == "money") return "Decimal";
            if (sqlType == "date") return "Date";
            if (sqlType == "datetime" || sqlType == "datetime2") return "DateTime";
            if (sqlType == "bit") return "Bool";
            if (sqlType == "uniqueidentifier") return "Guid";
            return "String";
        }
    }
}
