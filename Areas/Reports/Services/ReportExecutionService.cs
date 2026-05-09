using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using MyERP.Areas.Reports.Models;

namespace MyERP.Areas.Reports.Services
{
    public class ReportExecutionService
    {
        private readonly DynamicReportConnectionFactory _connectionFactory;
        private readonly ReportDefinitionService _definitionService;
        private readonly ReportPermissionService _permissionService;

        public ReportExecutionService()
            : this(new DynamicReportConnectionFactory(), new ReportDefinitionService(), new ReportPermissionService())
        {
        }

        public ReportExecutionService(DynamicReportConnectionFactory connectionFactory, ReportDefinitionService definitionService, ReportPermissionService permissionService)
        {
            _connectionFactory = connectionFactory;
            _definitionService = definitionService;
            _permissionService = permissionService;
        }

        public DynamicReportExecutionResult Execute(DynamicReportExecutionRequest request, DynamicReportUserContext user)
        {
            var result = new DynamicReportExecutionResult();
            if (request == null)
            {
                result.Message = "Invalid report execution request.";
                return result;
            }
            if (request.Parameters == null)
            {
                request.Parameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }
            var definition = _definitionService.GetDefinition(request.ReportId, user.ProjectScope);
            if (definition == null)
            {
                result.Message = "Report definition was not found.";
                return result;
            }

            if (!_permissionService.CanView(user, definition))
            {
                result.Message = "You do not have permission to run this report.";
                return result;
            }

            var validationMessage = ValidateRequiredParameters(definition, request);
            if (!string.IsNullOrEmpty(validationMessage))
            {
                result.Message = validationMessage;
                return result;
            }

            try
            {
                using (var connection = _connectionFactory.CreateOpenConnection(user.ProjectScope))
                using (var command = CreateCommand(connection, definition, request))
                using (var adapter = new SqlDataAdapter(command))
                {
                    var table = new DataTable();
                    adapter.Fill(table);
                    result.Columns = definition.Columns.Count > 0 ? definition.Columns : BuildColumns(table);
                    result.Rows = ToRows(table);
                    result.RowCount = result.Rows.Count;
                    result.MaxRows = definition.MaxRows;
                    result.Success = true;
                    result.Message = result.RowCount >= definition.MaxRows ? "Report returned the maximum configured rows." : "OK";
                }
            }
            catch
            {
                result.Success = false;
                result.Message = "Report execution failed. Please verify parameters and report definition.";
            }

            return result;
        }

        private static string ValidateRequiredParameters(DynamicReportDefinition definition, DynamicReportExecutionRequest request)
        {
            foreach (var parameter in definition.Parameters)
            {
                var name = ReportSqlSafety.NormalizeParameterName(parameter.ParameterName);
                string value;
                request.Parameters.TryGetValue(name, out value);
                if (string.IsNullOrWhiteSpace(value)) value = parameter.DefaultValue;
                if (parameter.IsRequired && string.IsNullOrWhiteSpace(value))
                {
                    return "Required report parameter is missing: " + (parameter.CaptionAr ?? parameter.CaptionEn ?? parameter.ParameterName);
                }
            }
            return null;
        }

        private static SqlCommand CreateCommand(SqlConnection connection, DynamicReportDefinition definition, DynamicReportExecutionRequest request)
        {
            SqlCommand command;
            if (string.Equals(definition.SourceType, "View", StringComparison.OrdinalIgnoreCase))
            {
                command = new SqlCommand("SELECT TOP (" + Math.Max(1, definition.MaxRows) + ") * FROM " + ReportSqlSafety.QuoteObjectName(definition.SourceName), connection);
                command.CommandType = CommandType.Text;
            }
            else
            {
                command = new SqlCommand(ReportSqlSafety.QuoteObjectName(definition.SourceName), connection);
                command.CommandType = CommandType.StoredProcedure;
                foreach (var parameter in definition.Parameters)
                {
                    var name = ReportSqlSafety.NormalizeParameterName(parameter.ParameterName);
                    var sqlParameter = command.Parameters.Add("@" + name, ReportSqlSafety.ToSqlDbType(parameter.DataType));
                    if (sqlParameter.SqlDbType == SqlDbType.Decimal)
                    {
                        sqlParameter.Precision = 18;
                        sqlParameter.Scale = 4;
                    }
                    string value;
                    request.Parameters.TryGetValue(name, out value);
                    if (string.IsNullOrWhiteSpace(value)) value = parameter.DefaultValue;
                    sqlParameter.Value = ReportSqlSafety.ConvertValue(value, parameter.DataType);
                }
            }
            command.CommandTimeout = Math.Max(5, Math.Min(definition.CommandTimeoutSeconds, 300));
            return command;
        }

        private static IList<DynamicReportColumn> BuildColumns(DataTable table)
        {
            var columns = new List<DynamicReportColumn>();
            for (var i = 0; i < table.Columns.Count; i++)
            {
                var column = table.Columns[i];
                columns.Add(new DynamicReportColumn
                {
                    FieldName = column.ColumnName,
                    CaptionAr = column.ColumnName,
                    CaptionEn = column.ColumnName,
                    DataType = column.DataType.Name,
                    IsVisibleDefault = true,
                    IsFilterable = true,
                    IsSortable = true,
                    IsSummable = column.DataType == typeof(decimal) || column.DataType == typeof(int) || column.DataType == typeof(double),
                    Width = 140,
                    SortOrder = i
                });
            }
            return columns;
        }

        private static IList<IDictionary<string, object>> ToRows(DataTable table)
        {
            var rows = new List<IDictionary<string, object>>();
            foreach (DataRow row in table.Rows)
            {
                var item = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                foreach (DataColumn column in table.Columns)
                {
                    item[column.ColumnName] = row[column] == DBNull.Value ? null : row[column];
                }
                rows.Add(item);
            }
            return rows;
        }
    }
}
