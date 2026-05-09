using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Web.Mvc;
using MyERP.Areas.Reports.Models;

namespace MyERP.Areas.Reports.Services
{
    public class ReportValidationService
    {
        private readonly ReportDefinitionService _definitionService;
        private readonly ReportExecutionService _executionService;
        private readonly DynamicReportConnectionFactory _connectionFactory;

        public ReportValidationService()
            : this(new ReportDefinitionService(), new ReportExecutionService(), new DynamicReportConnectionFactory())
        {
        }

        public ReportValidationService(ReportDefinitionService definitionService, ReportExecutionService executionService, DynamicReportConnectionFactory connectionFactory)
        {
            _definitionService = definitionService;
            _executionService = executionService;
            _connectionFactory = connectionFactory;
        }

        public ValidationReport ValidateAsync(DynamicReportDefinition definition, DynamicReportUserContext user, bool includeExec)
        {
            return ValidateAsync(definition, user, includeExec, null);
        }

        public ValidationReport ValidateAsync(DynamicReportDefinition definition, DynamicReportUserContext user, bool includeExec, ControllerContext controllerContext)
        {
            var report = new ValidationReport();
            if (definition == null)
            {
                Add(report, "meta.exists", "Error", "تعريف التقرير غير موجود.", null);
                Count(report);
                return report;
            }

            ValidateMetadata(report, definition);
            ValidateCodeUniqueness(report, definition);
            ValidateColumns(report, definition);
            ValidateParameters(report, definition);

            if (includeExec)
            {
                ValidateExecution(report, definition, user);
                ValidatePrint(report, controllerContext);
            }

            Count(report);
            return report;
        }

        public DynamicReportExecutionResult RunSample(int reportId, DynamicReportUserContext user, IDictionary<string, string> formValues)
        {
            var definition = _definitionService.GetDefinition(reportId, user.ProjectScope);
            var request = new DynamicReportExecutionRequest
            {
                ReportId = reportId,
                ProjectScope = user.ProjectScope,
                Parameters = BuildSampleParameters(definition, formValues)
            };
            return _executionService.Execute(request, user);
        }

        public IDictionary<string, string> BuildSampleParameters(DynamicReportDefinition definition, IDictionary<string, string> formValues)
        {
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (formValues != null)
            {
                foreach (var pair in formValues)
                {
                    values[pair.Key] = pair.Value;
                }
            }

            if (definition == null || definition.Parameters == null) return values;
            foreach (var parameter in definition.Parameters)
            {
                if (parameter == null || string.IsNullOrWhiteSpace(parameter.ParameterName)) continue;
                var key = ReportSqlSafety.NormalizeParameterName(parameter.ParameterName);
                string current;
                if (values.TryGetValue(key, out current) && !string.IsNullOrWhiteSpace(current)) continue;
                if (!string.IsNullOrWhiteSpace(parameter.DefaultValue))
                {
                    values[key] = parameter.DefaultValue;
                    continue;
                }

                values[key] = DefaultValueFor(parameter.DataType);
            }

            return values;
        }

        public bool CanRenderPrint(ControllerContext controllerContext)
        {
            if (controllerContext == null) return true;
            ViewEngineResult result = null;
            try
            {
                result = ViewEngines.Engines.FindView(controllerContext, "~/Areas/Reports/Views/Viewer/Print.cshtml", null);
                return result != null && result.View != null;
            }
            finally
            {
                if (result != null && result.View != null)
                {
                    result.ViewEngine.ReleaseView(controllerContext, result.View);
                }
            }
        }

        private static void ValidateMetadata(ValidationReport report, DynamicReportDefinition definition)
        {
            if (string.IsNullOrWhiteSpace(definition.ReportCode))
            {
                Add(report, "meta.code", "Error", "كود التقرير مطلوب.", null);
            }
            else
            {
                try
                {
                    ReportSqlSafety.ValidateSourceName(definition.ReportCode);
                }
                catch
                {
                    Add(report, "meta.code", "Error", "كود التقرير يجب أن يكون معرفًا صالحًا بدون مسافات أو رموز خطرة.", null);
                }
            }

            if (string.IsNullOrWhiteSpace(definition.ReportNameAr) || definition.ReportNameAr.Trim().Length <= 2)
            {
                Add(report, "meta.nameAr", "Error", "اسم التقرير العربي مطلوب ويجب أن يزيد عن حرفين.", null);
            }

            if (string.IsNullOrWhiteSpace(definition.ReportNameEn))
            {
                Add(report, "meta.nameEn", "Warning", "يفضل إدخال اسم إنجليزي للتقرير.", null);
            }

            var scope = DynamicReportScopes.Normalize(definition.ProjectScope);
            if (!(scope == DynamicReportScopes.Web || scope == DynamicReportScopes.Pos || scope == DynamicReportScopes.MainErp || scope == DynamicReportScopes.Shared))
            {
                Add(report, "meta.scope", "Error", "نطاق التقرير غير صالح.", null);
            }

            if (!string.Equals(definition.SourceType, "StoredProcedure", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(definition.SourceType, "View", StringComparison.OrdinalIgnoreCase))
            {
                Add(report, "source.type", "Error", "نوع المصدر يجب أن يكون StoredProcedure أو View.", null);
            }

            try
            {
                ReportSqlSafety.ValidateSourceName(definition.SourceName);
            }
            catch
            {
                Add(report, "source.name", "Error", "اسم المصدر غير صالح أو يحتوي على رموز غير مسموحة.", null);
            }
        }

        private void ValidateCodeUniqueness(ValidationReport report, DynamicReportDefinition definition)
        {
            if (string.IsNullOrWhiteSpace(definition.ReportCode)) return;
            const string sql = "SELECT COUNT(1) FROM dbo.DynamicReportDefinitions WHERE ReportCode = @ReportCode AND ReportId <> @ReportId;";
            try
            {
                using (var connection = _connectionFactory.CreateOpenConnection(definition.ProjectScope))
                using (var command = new SqlCommand(sql, connection))
                {
                    command.Parameters.Add("@ReportCode", SqlDbType.NVarChar, 100).Value = definition.ReportCode.Trim();
                    command.Parameters.Add("@ReportId", SqlDbType.Int).Value = definition.ReportId;
                    if (Convert.ToInt32(command.ExecuteScalar()) > 0)
                    {
                        Add(report, "meta.code", "Error", "كود التقرير مكرر.", null);
                    }
                }
            }
            catch
            {
                Add(report, "meta.code", "Warning", "تعذر التأكد من تكرار كود التقرير.", null);
            }
        }

        private static void ValidateColumns(ValidationReport report, DynamicReportDefinition definition)
        {
            var columns = definition.Columns == null ? new List<DynamicReportColumn>() : definition.Columns.ToList();
            if (!columns.Any(c => c != null && c.IsVisibleDefault))
            {
                Add(report, "cols.count", "Error", "يجب أن يحتوي التقرير على عمود ظاهر واحد على الأقل.", "Mapping");
            }

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var column in columns)
            {
                if (column == null) continue;
                if (string.IsNullOrWhiteSpace(column.FieldName))
                {
                    Add(report, "cols.fieldNames", "Error", "يوجد عمود بدون FieldName.", "Mapping");
                    continue;
                }

                try
                {
                    ReportSqlSafety.NormalizeParameterName(column.FieldName);
                }
                catch
                {
                    Add(report, "cols.fieldNames", "Error", "اسم العمود غير صالح: " + column.FieldName, "Mapping");
                }

                if (!seen.Add(column.FieldName))
                {
                    Add(report, "cols.fieldNames", "Error", "اسم العمود مكرر: " + column.FieldName, "Mapping");
                }

                if (string.IsNullOrWhiteSpace(column.CaptionAr))
                {
                    Add(report, "cols.captions", "Warning", "العمود " + column.FieldName + " يحتاج Caption عربي.", "Mapping");
                }

                if (!IsKnownDataType(column.DataType))
                {
                    Add(report, "cols.types", "Warning", "نوع بيانات العمود " + column.FieldName + " غير معروف وسيعامل كنص.", null);
                }
            }
        }

        private static void ValidateParameters(ValidationReport report, DynamicReportDefinition definition)
        {
            var parameters = definition.Parameters == null ? new List<DynamicReportParameter>() : definition.Parameters.ToList();
            foreach (var parameter in parameters)
            {
                if (parameter == null) continue;
                if (parameter.IsRequired && string.IsNullOrWhiteSpace(parameter.CaptionAr))
                {
                    Add(report, "params.captions", "Warning", "المعامل " + parameter.ParameterName + " يحتاج Caption عربي.", "Mapping");
                }

                if (!string.IsNullOrWhiteSpace(parameter.DefaultValue) && !CanParse(parameter.DefaultValue, parameter.DataType))
                {
                    Add(report, "params.defaults", "Warning", "القيمة الافتراضية للمعامل " + parameter.ParameterName + " لا تطابق نوعه.", null);
                }
            }
        }

        private void ValidateExecution(ValidationReport report, DynamicReportDefinition definition, DynamicReportUserContext user)
        {
            var watch = Stopwatch.StartNew();
            try
            {
                var request = new DynamicReportExecutionRequest
                {
                    ReportId = definition.ReportId,
                    ProjectScope = user.ProjectScope,
                    Parameters = BuildSampleParameters(definition, null)
                };
                var result = _executionService.Execute(request, user);
                watch.Stop();
                report.ExecStats.Ms = (int)Math.Min(int.MaxValue, watch.ElapsedMilliseconds);
                report.ExecStats.RowCount = result == null ? 0 : result.RowCount;
                report.ExecStats.ColumnCount = result == null || result.Columns == null ? 0 : result.Columns.Count;
                report.ExecStats.Truncated = result != null && result.MaxRows > 0 && result.RowCount >= result.MaxRows;

                if (result == null || !result.Success)
                {
                    report.ExecStats.Error = result == null ? "No result." : result.Message;
                    Add(report, "exec.run", "Error", "تشغيل العينة فشل. حدّد قيمًا افتراضية للمعاملات قبل تشغيل العينة.", null);
                    return;
                }

                if (watch.ElapsedMilliseconds > 30000)
                {
                    Add(report, "exec.timing", "Error", "تشغيل العينة تجاوز 30 ثانية.", null);
                }
                else if (watch.ElapsedMilliseconds > 3000)
                {
                    Add(report, "exec.timing", "Warning", "تشغيل العينة أبطأ من المتوقع.", null);
                }
                else
                {
                    Add(report, "exec.timing", "Info", "زمن تشغيل العينة مناسب.", null);
                }

                if (result.RowCount == 0) Add(report, "exec.rows", "Warning", "التقرير لم يرجع صفوفًا في العينة.", null);
                else Add(report, "exec.rows", "Info", "عدد صفوف العينة: " + result.RowCount, null);
                if (report.ExecStats.Truncated) Add(report, "exec.truncated", "Warning", "تم الوصول إلى حد الصفوف أثناء تشغيل العينة.", null);
            }
            catch (Exception ex)
            {
                watch.Stop();
                report.ExecStats.Ms = (int)Math.Min(int.MaxValue, watch.ElapsedMilliseconds);
                report.ExecStats.Error = ex.Message;
                Add(report, "exec.run", "Error", "تشغيل العينة فشل بصورة غير متوقعة.", null);
            }
        }

        private void ValidatePrint(ValidationReport report, ControllerContext controllerContext)
        {
            try
            {
                if (CanRenderPrint(controllerContext))
                {
                    Add(report, "print.render", "Info", "صفحة الطباعة متاحة للرندر.", null);
                }
                else
                {
                    Add(report, "print.render", "Error", "تعذر العثور على صفحة معاينة الطباعة.", null);
                }
            }
            catch
            {
                Add(report, "print.render", "Error", "تعذر رندر صفحة معاينة الطباعة.", null);
            }
        }

        private static void Add(ValidationReport report, string id, string level, string message, string hint)
        {
            report.CheckResults.Add(new ValidationCheckResult { Id = id, Level = level, Message = message, Hint = hint });
        }

        private static void Count(ValidationReport report)
        {
            report.ErrorCount = report.CheckResults.Count(x => string.Equals(x.Level, "Error", StringComparison.OrdinalIgnoreCase));
            report.WarningCount = report.CheckResults.Count(x => string.Equals(x.Level, "Warning", StringComparison.OrdinalIgnoreCase));
            report.InfoCount = report.CheckResults.Count(x => string.Equals(x.Level, "Info", StringComparison.OrdinalIgnoreCase));
        }

        private static bool IsKnownDataType(string dataType)
        {
            return string.IsNullOrWhiteSpace(dataType) ||
                   string.Equals(dataType, "String", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(dataType, "Int", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(dataType, "Decimal", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(dataType, "Date", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(dataType, "DateTime", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(dataType, "Bool", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(dataType, "Guid", StringComparison.OrdinalIgnoreCase);
        }

        private static bool CanParse(string value, string dataType)
        {
            return ReportSqlSafety.ConvertValue(value, dataType) != DBNull.Value;
        }

        private static string DefaultValueFor(string dataType)
        {
            if (string.Equals(dataType, "Int", StringComparison.OrdinalIgnoreCase)) return "0";
            if (string.Equals(dataType, "Decimal", StringComparison.OrdinalIgnoreCase)) return "0";
            if (string.Equals(dataType, "Date", StringComparison.OrdinalIgnoreCase) || string.Equals(dataType, "DateTime", StringComparison.OrdinalIgnoreCase)) return DateTime.Today.ToString("yyyy-MM-dd");
            if (string.Equals(dataType, "Bool", StringComparison.OrdinalIgnoreCase)) return "false";
            return string.Empty;
        }
    }
}
