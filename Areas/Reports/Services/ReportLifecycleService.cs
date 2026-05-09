using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using MyERP.Areas.Reports.Models;

namespace MyERP.Areas.Reports.Services
{
    public class ReportLifecycleService
    {
        private readonly DynamicReportConnectionFactory _connectionFactory;
        private readonly ReportDefinitionService _definitionService;
        private readonly ReportValidationService _validationService;
        private readonly ReportPermissionService _permissionService;

        public ReportLifecycleService()
            : this(new DynamicReportConnectionFactory(), new ReportDefinitionService(), new ReportValidationService(), new ReportPermissionService())
        {
        }

        public ReportLifecycleService(DynamicReportConnectionFactory connectionFactory, ReportDefinitionService definitionService, ReportValidationService validationService, ReportPermissionService permissionService)
        {
            _connectionFactory = connectionFactory;
            _definitionService = definitionService;
            _validationService = validationService;
            _permissionService = permissionService;
        }

        public LifecycleResult TransitionStatus(int reportId, string toStatus, DynamicReportUserContext user)
        {
            if (!_permissionService.CanDesign(user)) return Fail(null, "ليست لديك صلاحية إدارة دورة حياة التقرير.");
            toStatus = NormalizeStatus(toStatus);
            if (string.IsNullOrWhiteSpace(toStatus)) return Fail(null, "حالة التقرير المطلوبة غير صالحة.");

            var definition = _definitionService.GetDefinition(reportId, user.ProjectScope);
            if (definition == null) return Fail(null, "تعريف التقرير غير موجود في النطاق الحالي.");

            var fromStatus = NormalizeStatus(definition.LifecycleStatus);
            if (string.IsNullOrWhiteSpace(fromStatus)) fromStatus = definition.IsActive ? LifecycleStatusEnum.Active : LifecycleStatusEnum.Disabled;
            if (!IsAllowed(fromStatus, toStatus)) return Fail(fromStatus, "هذا الانتقال غير مسموح: " + fromStatus + " → " + toStatus);

            ValidationReport validation = null;
            if (RequiresValidation(toStatus, fromStatus))
            {
                validation = _validationService.ValidateAsync(definition, user, true);
                if (validation.ErrorCount > 0)
                {
                    UpdateStatus(reportId, user.ProjectScope, LifecycleStatusEnum.ValidationErrors, false, user.UserId, true);
                    return new LifecycleResult
                    {
                        Success = false,
                        NewStatus = LifecycleStatusEnum.ValidationErrors,
                        Message = "لا يمكن تفعيل التقرير قبل حل أخطاء الفحص.",
                        Errors = validation.CheckResults.Where(x => x.Level == "Error").Select(x => x.Message).ToList()
                    };
                }
            }

            UpdateStatus(reportId, user.ProjectScope, toStatus, toStatus == LifecycleStatusEnum.Active, user.UserId, validation != null);
            return new LifecycleResult
            {
                Success = true,
                NewStatus = toStatus,
                Message = "تم تغيير حالة التقرير إلى " + toStatus + "."
            };
        }

        public string MarkAfterValidation(int reportId, string scope, ValidationReport report, DynamicReportUserContext user)
        {
            var current = _definitionService.GetDefinition(reportId, scope);
            var status = report != null && report.ErrorCount > 0
                ? LifecycleStatusEnum.ValidationErrors
                : report != null && report.CheckResults.Any(x => string.Equals(x.Hint, "Mapping", StringComparison.OrdinalIgnoreCase))
                    ? LifecycleStatusEnum.NeedsMapping
                    : LifecycleStatusEnum.ReadyForActivation;
            if (current != null && current.LifecycleStatus == LifecycleStatusEnum.Active && status == LifecycleStatusEnum.ReadyForActivation)
            {
                status = LifecycleStatusEnum.Active;
            }

            UpdateStatus(reportId, scope, status, status == LifecycleStatusEnum.Active, user == null ? 0 : user.UserId, true);
            return status;
        }

        private void UpdateStatus(int reportId, string scope, string status, bool isActive, int userId, bool validated)
        {
            const string sql = @"
UPDATE dbo.DynamicReportDefinitions
SET LifecycleStatus = @Status,
    IsActive = @IsActive,
    LastValidatedAt = CASE WHEN @Validated = 1 THEN GETDATE() ELSE LastValidatedAt END,
    ActivatedBy = CASE WHEN @Status = N'Active' THEN @UserId ELSE ActivatedBy END,
    ActivatedAt = CASE WHEN @Status = N'Active' THEN GETDATE() ELSE ActivatedAt END,
    UpdatedBy = @UserId,
    UpdatedAt = GETDATE()
WHERE ReportId = @ReportId;";
            using (var connection = _connectionFactory.CreateOpenConnection(scope))
            using (var transaction = connection.BeginTransaction())
            using (var command = new SqlCommand(sql, connection, transaction))
            {
                command.Parameters.Add("@ReportId", SqlDbType.Int).Value = reportId;
                command.Parameters.Add("@Status", SqlDbType.NVarChar, 20).Value = status;
                command.Parameters.Add("@IsActive", SqlDbType.Bit).Value = isActive;
                command.Parameters.Add("@Validated", SqlDbType.Bit).Value = validated;
                command.Parameters.Add("@UserId", SqlDbType.Int).Value = userId > 0 ? (object)userId : DBNull.Value;
                command.ExecuteNonQuery();
                transaction.Commit();
            }
        }

        private static bool RequiresValidation(string toStatus, string fromStatus)
        {
            return toStatus == LifecycleStatusEnum.Active || toStatus == LifecycleStatusEnum.ReadyForActivation ||
                   (fromStatus == LifecycleStatusEnum.Disabled && toStatus == LifecycleStatusEnum.Active);
        }

        private static bool IsAllowed(string fromStatus, string toStatus)
        {
            if (toStatus == LifecycleStatusEnum.Draft)
            {
                return fromStatus != LifecycleStatusEnum.Active;
            }

            if (fromStatus == LifecycleStatusEnum.Draft || fromStatus == LifecycleStatusEnum.NeedsMapping || fromStatus == LifecycleStatusEnum.ValidationErrors)
            {
                return toStatus == LifecycleStatusEnum.ReadyForActivation || toStatus == LifecycleStatusEnum.Active || toStatus == LifecycleStatusEnum.Disabled || toStatus == LifecycleStatusEnum.Archived;
            }

            if (fromStatus == LifecycleStatusEnum.ReadyForActivation)
            {
                return toStatus == LifecycleStatusEnum.Active || toStatus == LifecycleStatusEnum.Draft || toStatus == LifecycleStatusEnum.Disabled || toStatus == LifecycleStatusEnum.Archived;
            }

            if (fromStatus == LifecycleStatusEnum.Active)
            {
                return toStatus == LifecycleStatusEnum.Disabled || toStatus == LifecycleStatusEnum.Archived;
            }

            if (fromStatus == LifecycleStatusEnum.Disabled)
            {
                return toStatus == LifecycleStatusEnum.Active || toStatus == LifecycleStatusEnum.Archived || toStatus == LifecycleStatusEnum.Draft;
            }

            if (fromStatus == LifecycleStatusEnum.Archived)
            {
                return toStatus == LifecycleStatusEnum.Disabled || toStatus == LifecycleStatusEnum.Draft;
            }

            return false;
        }

        private static string NormalizeStatus(string status)
        {
            var valid = new[]
            {
                LifecycleStatusEnum.Draft,
                LifecycleStatusEnum.NeedsMapping,
                LifecycleStatusEnum.ValidationErrors,
                LifecycleStatusEnum.ReadyForActivation,
                LifecycleStatusEnum.Active,
                LifecycleStatusEnum.Disabled,
                LifecycleStatusEnum.Archived
            };
            return valid.FirstOrDefault(x => string.Equals(x, status, StringComparison.OrdinalIgnoreCase));
        }

        private static LifecycleResult Fail(string status, string message)
        {
            return new LifecycleResult
            {
                Success = false,
                NewStatus = status,
                Message = message,
                Errors = new List<string> { message }
            };
        }
    }
}
