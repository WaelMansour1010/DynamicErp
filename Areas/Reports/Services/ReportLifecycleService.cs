using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web.Script.Serialization;
using MyERP.Areas.Reports.Models;

namespace MyERP.Areas.Reports.Services
{
    public class ReportLifecycleService
    {
        private readonly DynamicReportConnectionFactory _connectionFactory;
        private readonly ReportDefinitionService _definitionService;
        private readonly ReportValidationService _validationService;
        private readonly ReportPermissionService _permissionService;
        private readonly ReportClassificationEngine _classificationEngine;

        public ReportLifecycleService()
            : this(new DynamicReportConnectionFactory(), new ReportDefinitionService(), new ReportValidationService(), new ReportPermissionService(), new ReportClassificationEngine())
        {
        }

        public ReportLifecycleService(DynamicReportConnectionFactory connectionFactory, ReportDefinitionService definitionService, ReportValidationService validationService, ReportPermissionService permissionService, ReportClassificationEngine classificationEngine)
        {
            _connectionFactory = connectionFactory;
            _definitionService = definitionService;
            _validationService = validationService;
            _permissionService = permissionService;
            _classificationEngine = classificationEngine;
        }

        public LifecycleResult TransitionStatus(int reportId, string toStatus, DynamicReportUserContext user)
        {
            if (!_permissionService.CanDesign(user)) return Fail(null, null, "ليست لديك صلاحية إدارة دورة حياة التقرير.");
            toStatus = NormalizeStatus(toStatus);
            if (string.IsNullOrWhiteSpace(toStatus)) return Fail(null, null, "حالة التقرير المطلوبة غير صالحة.");
            if (toStatus == LifecycleStatusEnum.ReadyForActivation || toStatus == LifecycleStatusEnum.NeedsMapping || toStatus == LifecycleStatusEnum.ValidationErrors)
            {
                return Fail(null, null, "هذه الحالة تُحسب آليًا من الفحص ولا يمكن الانتقال إليها يدويًا.");
            }

            var definition = _definitionService.GetDefinition(reportId, user.ProjectScope);
            if (definition == null) return Fail(null, null, "تعريف التقرير غير موجود في النطاق الحالي.");

            var fromStatus = NormalizeStatus(definition.LifecycleStatus);
            if (string.IsNullOrWhiteSpace(fromStatus)) fromStatus = definition.IsActive ? LifecycleStatusEnum.Active : LifecycleStatusEnum.Disabled;
            if (!IsAllowed(fromStatus, toStatus)) return Fail(fromStatus, definition.CertificationLevel, "هذا الانتقال غير مسموح: " + fromStatus + " -> " + toStatus);

            ValidationReport validation = null;
            if (toStatus == LifecycleStatusEnum.Active)
            {
                validation = _validationService.ValidateAsync(definition, user, true);
                if (validation.ErrorCount > 0)
                {
                    SaveLifecycle(reportId, user.ProjectScope, LifecycleStatusEnum.ValidationErrors, false, DynamicReportCertificationLevel.Internal, user.UserId, validation, false, false);
                    return new LifecycleResult
                    {
                        Success = false,
                        NewLifecycleStatus = LifecycleStatusEnum.ValidationErrors,
                        NewStatus = LifecycleStatusEnum.ValidationErrors,
                        NewCertificationLevel = DynamicReportCertificationLevel.Internal,
                        Message = "لا يمكن تفعيل التقرير قبل حل أخطاء الفحص.",
                        LastValidation = validation,
                        Errors = validation.CheckResults.Where(x => x.Level == "Error").Take(5).Select(x => x.Message).ToList()
                    };
                }
            }

            var certification = toStatus == LifecycleStatusEnum.Active
                ? DynamicReportCertificationLevel.Internal
                : DynamicReportCertificationLevel.Internal;
            SaveLifecycle(reportId, user.ProjectScope, toStatus, toStatus == LifecycleStatusEnum.Active, certification, user.UserId, validation, toStatus == LifecycleStatusEnum.Active, false);
            return new LifecycleResult
            {
                Success = true,
                NewLifecycleStatus = toStatus,
                NewStatus = toStatus,
                NewCertificationLevel = certification,
                Message = "تم تغيير حالة التقرير إلى " + toStatus + ".",
                LastValidation = validation
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

            var keepReview = status == LifecycleStatusEnum.Active;
            SaveLifecycle(reportId, scope, status, status == LifecycleStatusEnum.Active, keepReview ? current.CertificationLevel : DynamicReportCertificationLevel.Internal, user == null ? 0 : user.UserId, report, false, keepReview);
            return status;
        }

        public LifecycleResult MarkReviewed(int reportId, DynamicReportUserContext user)
        {
            if (!_permissionService.CanDesign(user)) return Fail(null, null, "ليست لديك صلاحية مراجعة التقرير.");
            var definition = _definitionService.GetDefinition(reportId, user.ProjectScope);
            if (definition == null) return Fail(null, null, "تعريف التقرير غير موجود.");
            if (definition.LifecycleStatus != LifecycleStatusEnum.Active || !definition.IsActive) return Fail(definition.LifecycleStatus, definition.CertificationLevel, "لا يمكن مراجعة تقرير غير نشط.");
            if (definition.CreatedBy.HasValue && definition.CreatedBy.Value == user.UserId) return Fail(definition.LifecycleStatus, definition.CertificationLevel, "لا يمكنك مراجعة تقرير أنشأته بنفسك.");
            if (definition.CertificationLevel != DynamicReportCertificationLevel.Internal) return Fail(definition.LifecycleStatus, definition.CertificationLevel, "لا يمكن تغيير مستوى الاعتماد الحالي إلا من المسار الصحيح.");

            const string sql = @"
UPDATE dbo.DynamicReportDefinitions
SET CertificationLevel = N'Reviewed',
    ReviewedBy = @UserId,
    ReviewedAt = GETDATE(),
    UpdatedBy = @UserId,
    UpdatedAt = GETDATE()
WHERE ReportId = @ReportId
  AND (ProjectScope = @Scope OR ProjectScope = N'Shared' OR @Scope = N'Shared')
  AND LifecycleStatus = N'Active'
  AND IsActive = 1;";
            using (var connection = _connectionFactory.CreateOpenConnection(user.ProjectScope))
            using (var transaction = connection.BeginTransaction())
            using (var command = new SqlCommand(sql, connection, transaction))
            {
                command.Parameters.Add("@ReportId", SqlDbType.Int).Value = reportId;
                command.Parameters.Add("@Scope", SqlDbType.NVarChar, 20).Value = user.ProjectScope;
                command.Parameters.Add("@UserId", SqlDbType.Int).Value = user.UserId > 0 ? (object)user.UserId : DBNull.Value;
                if (command.ExecuteNonQuery() == 0) throw new InvalidOperationException("تعذر حفظ مراجعة التقرير.");
                InsertAudit(connection, transaction, reportId, user.ProjectScope, "MarkReviewed", DynamicReportCertificationLevel.Internal, DynamicReportCertificationLevel.Reviewed, user.UserId, "Certification review");
                transaction.Commit();
            }

            return new LifecycleResult { Success = true, NewLifecycleStatus = LifecycleStatusEnum.Active, NewStatus = LifecycleStatusEnum.Active, NewCertificationLevel = DynamicReportCertificationLevel.Reviewed, Message = "تم اعتماد التقرير كمراجع." };
        }
        public LifecycleResult RevertReview(int reportId, DynamicReportUserContext user)
        {
            if (!_permissionService.CanDesign(user)) return Fail(null, null, "ليست لديك صلاحية مراجعة التقرير.");
            const string sql = @"
UPDATE dbo.DynamicReportDefinitions
SET CertificationLevel = N'Internal',
    ReviewedBy = NULL,
    ReviewedAt = NULL,
    UpdatedBy = @UserId,
    UpdatedAt = GETDATE()
WHERE ReportId = @ReportId
  AND (ProjectScope = @Scope OR ProjectScope = N'Shared' OR @Scope = N'Shared');";
            using (var connection = _connectionFactory.CreateOpenConnection(user.ProjectScope))
            using (var transaction = connection.BeginTransaction())
            using (var command = new SqlCommand(sql, connection, transaction))
            {
                command.Parameters.Add("@ReportId", SqlDbType.Int).Value = reportId;
                command.Parameters.Add("@Scope", SqlDbType.NVarChar, 20).Value = user.ProjectScope;
                command.Parameters.Add("@UserId", SqlDbType.Int).Value = user.UserId > 0 ? (object)user.UserId : DBNull.Value;
                if (command.ExecuteNonQuery() == 0) throw new InvalidOperationException("تعذر إلغاء المراجعة.");
                InsertAudit(connection, transaction, reportId, user.ProjectScope, "RevertReview", DynamicReportCertificationLevel.Reviewed, DynamicReportCertificationLevel.Internal, user.UserId, "Certification review reverted");
                transaction.Commit();
            }

            var definition = _definitionService.GetDefinition(reportId, user.ProjectScope);
            return new LifecycleResult { Success = true, NewLifecycleStatus = definition == null ? null : definition.LifecycleStatus, NewStatus = definition == null ? null : definition.LifecycleStatus, NewCertificationLevel = DynamicReportCertificationLevel.Internal, Message = "تم إرجاع الاعتماد إلى Internal." };
        }
        public LifecycleResult MarkProductionReady(int reportId, DynamicReportUserContext user)
        {
            var gate = ValidateCertificationGate(reportId, user, DynamicReportCertificationLevel.Reviewed);
            if (!gate.Success) return gate;
            return UpdateCertification(reportId, user, DynamicReportCertificationLevel.ProductionReady, gate.LastValidation, "تم اعتماد التقرير كجاهز للإنتاج.");
        }

        public LifecycleResult MarkCertified(int reportId, DynamicReportUserContext user)
        {
            var gate = ValidateCertificationGate(reportId, user, DynamicReportCertificationLevel.ProductionReady);
            if (!gate.Success) return gate;
            var definition = _definitionService.GetDefinition(reportId, user.ProjectScope);
            if (definition != null && definition.CreatedBy.HasValue && definition.CreatedBy.Value == user.UserId)
                return Fail(definition.LifecycleStatus, definition.CertificationLevel, "لا يمكنك اعتماد تقرير أنشأته بنفسك.");
            if (definition != null && definition.ReviewedBy.HasValue && definition.ReviewedBy.Value == user.UserId)
                return Fail(definition.LifecycleStatus, definition.CertificationLevel, "يفضل أن يتم الاعتماد النهائي بواسطة أدمن مختلف عن المراجع.");
            return UpdateCertification(reportId, user, DynamicReportCertificationLevel.Certified, gate.LastValidation, "تم اعتماد التقرير نهائيًا.");
        }

        private void SaveLifecycle(int reportId, string scope, string status, bool isActive, string certificationLevel, int userId, ValidationReport validation, bool activated, bool preserveReview)
        {
            const string sql = @"
UPDATE dbo.DynamicReportDefinitions
SET LifecycleStatus = @Status,
    IsActive = @IsActive,
    CertificationLevel = @CertificationLevel,
    LastValidatedAt = CASE WHEN @ValidationJson IS NOT NULL THEN GETDATE() ELSE LastValidatedAt END,
    LastValidationLog = CASE WHEN @ValidationJson IS NOT NULL THEN @ValidationJson ELSE LastValidationLog END,
    ActivatedBy = CASE WHEN @Activated = 1 THEN @UserId ELSE ActivatedBy END,
    ActivatedAt = CASE WHEN @Activated = 1 THEN GETDATE() ELSE ActivatedAt END,
    ReviewedBy = CASE WHEN @PreserveReview = 1 THEN ReviewedBy ELSE NULL END,
    ReviewedAt = CASE WHEN @PreserveReview = 1 THEN ReviewedAt ELSE NULL END,
    UpdatedBy = @UserId,
    UpdatedAt = GETDATE()
WHERE ReportId = @ReportId
  AND (ProjectScope = @Scope OR ProjectScope = N'Shared' OR @Scope = N'Shared');";
            using (var connection = _connectionFactory.CreateOpenConnection(scope))
            using (var transaction = connection.BeginTransaction())
            using (var command = new SqlCommand(sql, connection, transaction))
            {
                command.Parameters.Add("@ReportId", SqlDbType.Int).Value = reportId;
                command.Parameters.Add("@Scope", SqlDbType.NVarChar, 20).Value = DynamicReportScopes.Normalize(scope);
                command.Parameters.Add("@Status", SqlDbType.NVarChar, 20).Value = status;
                command.Parameters.Add("@IsActive", SqlDbType.Bit).Value = isActive;
                command.Parameters.Add("@CertificationLevel", SqlDbType.NVarChar, 20).Value = certificationLevel ?? DynamicReportCertificationLevel.Internal;
                command.Parameters.Add("@ValidationJson", SqlDbType.NVarChar, -1).Value = validation == null ? (object)DBNull.Value : new JavaScriptSerializer { MaxJsonLength = int.MaxValue }.Serialize(validation);
                command.Parameters.Add("@Activated", SqlDbType.Bit).Value = activated;
                command.Parameters.Add("@PreserveReview", SqlDbType.Bit).Value = preserveReview;
                command.Parameters.Add("@UserId", SqlDbType.Int).Value = userId > 0 ? (object)userId : DBNull.Value;
                command.ExecuteNonQuery();
                InsertAudit(connection, transaction, reportId, scope, status, null, status, userId, "Lifecycle transition");
                transaction.Commit();
            }
        }

        private LifecycleResult ValidateCertificationGate(int reportId, DynamicReportUserContext user, string requiredLevel)
        {
            if (!_permissionService.CanDesign(user)) return Fail(null, null, "ليست لديك صلاحية اعتماد التقرير.");
            var definition = _definitionService.GetDefinition(reportId, user.ProjectScope);
            if (definition == null) return Fail(null, null, "تعريف التقرير غير موجود.");
            if (definition.LifecycleStatus != LifecycleStatusEnum.Active || !definition.IsActive)
                return Fail(definition.LifecycleStatus, definition.CertificationLevel, "لا يمكن اعتماد تقرير غير نشط.");
            if (!string.Equals(definition.CertificationLevel, requiredLevel, StringComparison.OrdinalIgnoreCase))
                return Fail(definition.LifecycleStatus, definition.CertificationLevel, "مستوى الاعتماد الحالي لا يسمح بهذه العملية.");

            var validation = _validationService.ValidateAsync(definition, user, true);
            if (validation.ErrorCount > 0)
                return new LifecycleResult
                {
                    Success = false,
                    NewLifecycleStatus = definition.LifecycleStatus,
                    NewStatus = definition.LifecycleStatus,
                    NewCertificationLevel = definition.CertificationLevel,
                    Message = "لا يمكن الاعتماد قبل معالجة أخطاء الفحص.",
                    LastValidation = validation,
                    Errors = validation.CheckResults.Where(x => x.Level == "Error").Take(5).Select(x => x.Message).ToList()
                };

            var sourceRisk = FindImportedSourceRisk(definition, user);
            if (!string.IsNullOrWhiteSpace(sourceRisk))
                return Fail(definition.LifecycleStatus, definition.CertificationLevel, sourceRisk);

            return new LifecycleResult
            {
                Success = true,
                NewLifecycleStatus = definition.LifecycleStatus,
                NewStatus = definition.LifecycleStatus,
                NewCertificationLevel = definition.CertificationLevel,
                LastValidation = validation
            };
        }

        private LifecycleResult UpdateCertification(int reportId, DynamicReportUserContext user, string level, ValidationReport validation, string message)
        {
            const string sql = @"
UPDATE dbo.DynamicReportDefinitions
SET CertificationLevel = @Level,
    LastValidatedAt = GETDATE(),
    LastValidationLog = @ValidationJson,
    UpdatedBy = @UserId,
    UpdatedAt = GETDATE()
WHERE ReportId = @ReportId
  AND (ProjectScope = @Scope OR ProjectScope = N'Shared' OR @Scope = N'Shared')
  AND LifecycleStatus = N'Active'
  AND IsActive = 1;";
            using (var connection = _connectionFactory.CreateOpenConnection(user.ProjectScope))
            using (var transaction = connection.BeginTransaction())
            using (var command = new SqlCommand(sql, connection, transaction))
            {
                command.Parameters.Add("@ReportId", SqlDbType.Int).Value = reportId;
                command.Parameters.Add("@Scope", SqlDbType.NVarChar, 20).Value = user.ProjectScope;
                command.Parameters.Add("@Level", SqlDbType.NVarChar, 20).Value = level;
                command.Parameters.Add("@ValidationJson", SqlDbType.NVarChar, -1).Value = new JavaScriptSerializer { MaxJsonLength = int.MaxValue }.Serialize(validation);
                command.Parameters.Add("@UserId", SqlDbType.Int).Value = user.UserId > 0 ? (object)user.UserId : DBNull.Value;
                if (command.ExecuteNonQuery() == 0) throw new InvalidOperationException("تعذر تحديث مستوى الاعتماد.");
                InsertAudit(connection, transaction, reportId, user.ProjectScope, level, null, level, user.UserId, "Certification");
                transaction.Commit();
            }

            return new LifecycleResult
            {
                Success = true,
                NewLifecycleStatus = LifecycleStatusEnum.Active,
                NewStatus = LifecycleStatusEnum.Active,
                NewCertificationLevel = level,
                Message = message,
                LastValidation = validation
            };
        }

        private string FindImportedSourceRisk(DynamicReportDefinition definition, DynamicReportUserContext user)
        {
            const string catalogSql = @"
SELECT TOP (1) SourceType, SourceSchema, SourceName
FROM dbo.DynamicReportCatalog
WHERE ImportedReportId = @ReportId AND ProjectScope = @Scope;";
            try
            {
                using (var connection = _connectionFactory.CreateOpenConnection(user.ProjectScope))
                using (var command = new SqlCommand(catalogSql, connection))
                {
                    command.Parameters.Add("@ReportId", SqlDbType.Int).Value = definition.ReportId;
                    command.Parameters.Add("@Scope", SqlDbType.NVarChar, 20).Value = user.ProjectScope;
                    using (var reader = command.ExecuteReader())
                    {
                        if (!reader.Read()) return null;
                        var sourceType = Convert.ToString(reader["SourceType"]);
                        var schema = Convert.ToString(reader["SourceSchema"]);
                        var name = Convert.ToString(reader["SourceName"]);
                        reader.Close();
                        var meta = LoadClassificationMeta(connection, schema, name);
                        var result = string.Equals(sourceType, "View", StringComparison.OrdinalIgnoreCase)
                            ? _classificationEngine.ClassifyView(meta)
                            : _classificationEngine.ClassifyProc(meta);
                        var serious = result.RiskFlags.Where(IsSeriousRiskFlag).ToList();
                        return serious.Count == 0 ? null : "تم منع الاعتماد لأن مصدر التقرير أصبح يحمل مخاطر جديدة: " + string.Join(", ", serious);
                    }
                }
            }
            catch
            {
                return "تعذر إعادة فحص مصدر التقرير المستورد قبل الاعتماد.";
            }
        }

        private static CatalogSourceMeta LoadClassificationMeta(SqlConnection connection, string schema, string name)
        {
            var objectName = schema + "." + name;
            var meta = new CatalogSourceMeta { Schema = schema, Name = name, Body = "" };
            using (var command = new SqlCommand("SELECT OBJECT_DEFINITION(OBJECT_ID(@ObjectName));", connection))
            {
                command.Parameters.Add("@ObjectName", SqlDbType.NVarChar, 256).Value = objectName;
                meta.Body = Convert.ToString(command.ExecuteScalar());
            }
            using (var command = new SqlCommand("SELECT p.name, t.name AS TypeName, p.is_output FROM sys.parameters p INNER JOIN sys.types t ON p.user_type_id = t.user_type_id WHERE p.object_id = OBJECT_ID(@ObjectName);", connection))
            {
                command.Parameters.Add("@ObjectName", SqlDbType.NVarChar, 256).Value = objectName;
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read()) meta.Parameters.Add(new CatalogParameterMeta { Name = Convert.ToString(reader["name"]), SqlType = Convert.ToString(reader["TypeName"]), IsOutput = Convert.ToBoolean(reader["is_output"]) });
                }
            }
            using (var command = new SqlCommand("SELECT c.name, t.name AS TypeName FROM sys.columns c INNER JOIN sys.types t ON c.user_type_id = t.user_type_id WHERE c.object_id = OBJECT_ID(@ObjectName);", connection))
            {
                command.Parameters.Add("@ObjectName", SqlDbType.NVarChar, 256).Value = objectName;
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read()) meta.Columns.Add(new CatalogColumnMeta { Name = Convert.ToString(reader["name"]), SqlType = Convert.ToString(reader["TypeName"]) });
                }
            }
            return meta;
        }

        private static bool IsSeriousRiskFlag(string flag)
        {
            return flag == "HasInsert" || flag == "HasUpdate" || flag == "HasDelete" || flag == "HasMerge" ||
                   flag == "HasTruncate" || flag == "HasDrop" || flag == "HasAlter" || flag == "HasCreate" ||
                   flag == "HasExec" || flag == "HasDynamicSql" || flag == "HasOutputParam" ||
                   flag == "NameSensitive" || flag == "EmptyBody";
        }

        private static void InsertAudit(SqlConnection connection, SqlTransaction transaction, int reportId, string scope, string actionType, string oldValue, string newValue, int userId, string notes)
        {
            const string sql = @"
IF OBJECT_ID('dbo.DynamicReportAuditLog', 'U') IS NOT NULL
BEGIN
    INSERT INTO dbo.DynamicReportAuditLog
    (ReportId, ProjectScope, ActionType, OldValue, NewValue, PerformedBy, Notes)
    VALUES (@ReportId, @ProjectScope, @ActionType, @OldValue, @NewValue, @UserId, @Notes);
END";
            using (var command = new SqlCommand(sql, connection, transaction))
            {
                command.Parameters.Add("@ReportId", SqlDbType.Int).Value = reportId;
                command.Parameters.Add("@ProjectScope", SqlDbType.NVarChar, 20).Value = scope;
                command.Parameters.Add("@ActionType", SqlDbType.NVarChar, 50).Value = actionType;
                command.Parameters.Add("@OldValue", SqlDbType.NVarChar, -1).Value = (object)oldValue ?? DBNull.Value;
                command.Parameters.Add("@NewValue", SqlDbType.NVarChar, -1).Value = (object)newValue ?? DBNull.Value;
                command.Parameters.Add("@UserId", SqlDbType.Int).Value = userId > 0 ? (object)userId : DBNull.Value;
                command.Parameters.Add("@Notes", SqlDbType.NVarChar, 1000).Value = (object)notes ?? DBNull.Value;
                command.ExecuteNonQuery();
            }
        }

        private static bool IsAllowed(string fromStatus, string toStatus)
        {
            if (toStatus == LifecycleStatusEnum.Draft) return fromStatus != LifecycleStatusEnum.Active;
            if (fromStatus == LifecycleStatusEnum.Archived) return toStatus == LifecycleStatusEnum.Disabled;
            if (toStatus == LifecycleStatusEnum.Active) return fromStatus != LifecycleStatusEnum.Archived;
            if (fromStatus == LifecycleStatusEnum.Active) return toStatus == LifecycleStatusEnum.Disabled || toStatus == LifecycleStatusEnum.Archived;
            if (fromStatus == LifecycleStatusEnum.Disabled) return toStatus == LifecycleStatusEnum.Active || toStatus == LifecycleStatusEnum.Archived || toStatus == LifecycleStatusEnum.Draft;
            if (fromStatus == LifecycleStatusEnum.Draft || fromStatus == LifecycleStatusEnum.NeedsMapping || fromStatus == LifecycleStatusEnum.ValidationErrors || fromStatus == LifecycleStatusEnum.ReadyForActivation)
            {
                return toStatus == LifecycleStatusEnum.Active || toStatus == LifecycleStatusEnum.Disabled || toStatus == LifecycleStatusEnum.Archived;
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

        private static LifecycleResult Fail(string status, string certification, string message)
        {
            return new LifecycleResult
            {
                Success = false,
                NewLifecycleStatus = status,
                NewStatus = status,
                NewCertificationLevel = certification,
                Message = message,
                Errors = new List<string> { message }
            };
        }
    }
}
