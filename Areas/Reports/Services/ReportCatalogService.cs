using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using MyERP.Areas.Reports.Models;

namespace MyERP.Areas.Reports.Services
{
    public class ReportCatalogService
    {
        private const int ScanLimit = 2000;
        private readonly DynamicReportConnectionFactory _connectionFactory;
        private readonly ReportPermissionService _permissionService;
        private readonly ReportClassificationEngine _classificationEngine;
        private readonly ReportMetadataService _metadataService;

        public ReportCatalogService()
            : this(new DynamicReportConnectionFactory(), new ReportPermissionService(), new ReportClassificationEngine())
        {
        }

        public ReportCatalogService(DynamicReportConnectionFactory connectionFactory, ReportPermissionService permissionService, ReportClassificationEngine classificationEngine)
        {
            _connectionFactory = connectionFactory;
            _permissionService = permissionService;
            _classificationEngine = classificationEngine;
            _metadataService = new ReportMetadataService(connectionFactory);
        }

        public Task<CatalogDiscoveryResult> DiscoverAsync(string scope, DynamicReportUserContext user)
        {
            RequireDesigner(user);
            var result = new CatalogDiscoveryResult();
            var normalizedScope = DynamicReportScopes.Normalize(scope);
            using (var connection = _connectionFactory.CreateOpenConnection(normalizedScope))
            {
                var sources = LoadSources(connection);
                if (sources.Count > ScanLimit)
                {
                    throw new InvalidOperationException("Scan limit reached. تواصل مع DBA لتقسيم العملية.");
                }

                foreach (var source in sources)
                {
                    try
                    {
                        source.Parameters = source.SourceType == "StoredProcedure"
                            ? LoadParameterMeta(connection, source.SourceSchema + "." + source.SourceName)
                            : new List<CatalogParameterMeta>();
                        source.Columns = LoadColumnMeta(connection, normalizedScope, source.SourceType, source.SourceSchema + "." + source.SourceName);
                        var classification = source.SourceType == "View"
                            ? _classificationEngine.ClassifyView(source.ToClassificationMeta())
                            : _classificationEngine.ClassifyProc(source.ToClassificationMeta());
                        var inserted = UpsertCatalog(connection, normalizedScope, source, classification);
                        if (inserted) result.DiscoveredCount++;
                        else result.UpdatedCount++;
                    }
                    catch (Exception ex)
                    {
                        result.ErrorCount++;
                        result.Errors.Add(source.SourceSchema + "." + source.SourceName + ": " + ex.Message);
                    }
                }
            }

            return Task.FromResult(result);
        }

        public IList<CatalogEntry> List(string scope, string status, DynamicReportUserContext user)
        {
            RequireDesigner(user);
            var list = new List<CatalogEntry>();
            const string sql = @"
SELECT CatalogId, ProjectScope, SourceType, SourceSchema, SourceName, DiscoveredAt, LastSeenAt,
       ClassificationStatus, ClassificationScore, RiskFlags, SuggestedReportName, ApprovedBy,
       ApprovedAt, RejectionReason, ImportedReportId, ImportedAt, Notes
FROM dbo.DynamicReportCatalog
WHERE ProjectScope = @ProjectScope
  AND (@Status IS NULL OR ClassificationStatus = @Status)
ORDER BY ClassificationStatus, ClassificationScore DESC, SourceType, SourceName;";
            using (var connection = _connectionFactory.CreateOpenConnection(DynamicReportScopes.Normalize(scope)))
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.Add("@ProjectScope", SqlDbType.NVarChar, 20).Value = DynamicReportScopes.Normalize(scope);
                command.Parameters.Add("@Status", SqlDbType.NVarChar, 20).Value = string.IsNullOrWhiteSpace(status) ? (object)DBNull.Value : status;
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read()) list.Add(ReadCatalogEntry(reader));
                }
            }

            return list;
        }

        public CatalogDetail GetDetail(int catalogId, string scope, DynamicReportUserContext user)
        {
            RequireDesigner(user);
            using (var connection = _connectionFactory.CreateOpenConnection(DynamicReportScopes.Normalize(scope)))
            {
                var entry = GetEntry(connection, catalogId, DynamicReportScopes.Normalize(scope));
                if (entry == null) throw new InvalidOperationException("العنصر غير موجود في الكتالوج.");
                var objectName = entry.SourceSchema + "." + entry.SourceName;
                return new CatalogDetail
                {
                    Entry = entry,
                    Columns = ToReportColumns(LoadColumnMeta(connection, entry.ProjectScope, entry.SourceType, objectName)),
                    Parameters = ToReportParameters(LoadParameterMeta(connection, objectName)),
                    BodyExcerpt = LoadBodyExcerpt(connection, objectName)
                };
            }
        }

        public void Approve(int catalogId, string suggestedName, DynamicReportUserContext user)
        {
            RequireDesigner(user);
            const string sql = @"
UPDATE dbo.DynamicReportCatalog
SET ClassificationStatus = N'Approved',
    SuggestedReportName = @SuggestedReportName,
    ApprovedBy = @UserId,
    ApprovedAt = GETDATE(),
    RejectionReason = NULL
WHERE CatalogId = @CatalogId AND ProjectScope = @ProjectScope;";
            using (var connection = _connectionFactory.CreateOpenConnection(user.ProjectScope))
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.Add("@CatalogId", SqlDbType.Int).Value = catalogId;
                command.Parameters.Add("@ProjectScope", SqlDbType.NVarChar, 20).Value = user.ProjectScope;
                command.Parameters.Add("@SuggestedReportName", SqlDbType.NVarChar, 200).Value = TrimOrDbNull(suggestedName, 200);
                command.Parameters.Add("@UserId", SqlDbType.Int).Value = user.UserId > 0 ? (object)user.UserId : DBNull.Value;
                if (command.ExecuteNonQuery() == 0) throw new InvalidOperationException("لم يتم العثور على العنصر داخل النطاق الحالي.");
            }
        }

        public void Reject(int catalogId, string reason, DynamicReportUserContext user)
        {
            RequireDesigner(user);
            const string sql = @"
UPDATE dbo.DynamicReportCatalog
SET ClassificationStatus = N'Rejected',
    RejectionReason = @Reason,
    ApprovedBy = @UserId,
    ApprovedAt = GETDATE()
WHERE CatalogId = @CatalogId AND ProjectScope = @ProjectScope;";
            using (var connection = _connectionFactory.CreateOpenConnection(user.ProjectScope))
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.Add("@CatalogId", SqlDbType.Int).Value = catalogId;
                command.Parameters.Add("@ProjectScope", SqlDbType.NVarChar, 20).Value = user.ProjectScope;
                command.Parameters.Add("@Reason", SqlDbType.NVarChar, 500).Value = TrimOrDbNull(reason, 500);
                command.Parameters.Add("@UserId", SqlDbType.Int).Value = user.UserId > 0 ? (object)user.UserId : DBNull.Value;
                if (command.ExecuteNonQuery() == 0) throw new InvalidOperationException("لم يتم العثور على العنصر داخل النطاق الحالي.");
            }
        }

        public CatalogImportResult Import(int catalogId, DynamicReportUserContext user)
        {
            RequireDesigner(user);
            using (var connection = _connectionFactory.CreateOpenConnection(user.ProjectScope))
            using (var transaction = connection.BeginTransaction())
            {
                try
                {
                    var entry = GetEntry(connection, transaction, catalogId, user.ProjectScope);
                    if (entry == null) throw new InvalidOperationException("العنصر غير موجود في الكتالوج.");
                    if (entry.ClassificationStatus != DynamicReportCatalogStatus.Approved) throw new InvalidOperationException("لا يمكن الاستيراد إلا بعد الاعتماد اليدوي.");
                    if (entry.ImportedReportId.HasValue) throw new InvalidOperationException("تم استيراد هذا العنصر من قبل.");

                    var objectName = entry.SourceSchema + "." + entry.SourceName;
                    var columns = ToReportColumns(LoadColumnMeta(connection, transaction, entry.SourceType, objectName));
                    if (columns.Count == 0) throw new InvalidOperationException("لا يمكن استيراد هذا التقرير لتعذر اكتشاف الأعمدة. أنشئه يدويًا من Definition Panel.");
                    var parameters = ToReportParameters(LoadParameterMeta(connection, transaction, objectName));
                    var reportCode = BuildUniqueReportCode(connection, transaction, entry.ProjectScope, entry.SourceSchema, entry.SourceName);
                    var reportId = InsertDefinition(connection, transaction, entry, reportCode, user);
                    InsertColumns(connection, transaction, reportId, columns);
                    InsertParameters(connection, transaction, reportId, parameters);
                    MarkImported(connection, transaction, catalogId, user.ProjectScope, reportId);
                    transaction.Commit();
                    return new CatalogImportResult
                    {
                        NewReportId = reportId,
                        ReportCode = reportCode,
                        Message = "تم استيراد التقرير كمسودة غير مفعلة."
                    };
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        private void RequireDesigner(DynamicReportUserContext user)
        {
            if (!_permissionService.CanDesign(user)) throw new InvalidOperationException("ليست لديك صلاحية لإدارة كتالوج التقارير.");
        }

        private static IList<CatalogSourceRow> LoadSources(SqlConnection connection)
        {
            var list = new List<CatalogSourceRow>();
            const string sql = @"
SELECT TOP (2001)
       CASE WHEN o.type = 'V' THEN N'View' ELSE N'StoredProcedure' END AS SourceType,
       SCHEMA_NAME(o.schema_id) AS SourceSchema,
       o.name AS SourceName,
       OBJECT_DEFINITION(o.object_id) AS Body
FROM sys.objects o
WHERE o.type IN ('P','V')
  AND o.is_ms_shipped = 0
  AND SCHEMA_NAME(o.schema_id) = N'dbo'
ORDER BY o.type, o.name;";
            using (var command = new SqlCommand(sql, connection))
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    list.Add(new CatalogSourceRow
                    {
                        SourceType = Convert.ToString(reader["SourceType"]),
                        SourceSchema = Convert.ToString(reader["SourceSchema"]),
                        SourceName = Convert.ToString(reader["SourceName"]),
                        Body = Convert.ToString(reader["Body"])
                    });
                }
            }

            return list;
        }

        private IList<CatalogColumnMeta> LoadColumnMeta(SqlConnection connection, string scope, string sourceType, string objectName)
        {
            if (sourceType == "View")
            {
                try
                {
                    var columns = _metadataService.LoadColumns(scope, sourceType, objectName);
                    if (columns.Count > 0)
                    {
                        return LoadColumnMetaFromSys(connection, objectName);
                    }
                }
                catch
                {
                    return new List<CatalogColumnMeta>();
                }
            }

            return LoadColumnMetaFromDescribe(connection, null, objectName);
        }

        private static IList<CatalogColumnMeta> LoadColumnMeta(SqlConnection connection, SqlTransaction transaction, string sourceType, string objectName)
        {
            return sourceType == "View"
                ? LoadColumnMetaFromSys(connection, objectName, transaction)
                : LoadColumnMetaFromDescribe(connection, transaction, objectName);
        }

        private static IList<CatalogColumnMeta> LoadColumnMetaFromSys(SqlConnection connection, string objectName, SqlTransaction transaction = null)
        {
            var list = new List<CatalogColumnMeta>();
            const string sql = @"
SELECT c.name AS ColumnName, t.name AS TypeName
FROM sys.columns c
INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
WHERE c.object_id = OBJECT_ID(@ObjectName)
ORDER BY c.column_id;";
            using (var command = new SqlCommand(sql, connection, transaction))
            {
                command.Parameters.Add("@ObjectName", SqlDbType.NVarChar, 256).Value = objectName;
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new CatalogColumnMeta
                        {
                            Name = Convert.ToString(reader["ColumnName"]),
                            SqlType = Convert.ToString(reader["TypeName"])
                        });
                    }
                }
            }

            return list;
        }

        private static IList<CatalogColumnMeta> LoadColumnMetaFromDescribe(SqlConnection connection, SqlTransaction transaction, string objectName)
        {
            var list = new List<CatalogColumnMeta>();
            const string sql = @"
SELECT name, system_type_name
FROM sys.dm_exec_describe_first_result_set_for_object(OBJECT_ID(@ObjectName), 0)
WHERE is_hidden = 0 AND error_number IS NULL
ORDER BY column_ordinal;";
            try
            {
                using (var command = new SqlCommand(sql, connection, transaction))
                {
                    command.CommandTimeout = 5;
                    command.Parameters.Add("@ObjectName", SqlDbType.NVarChar, 256).Value = objectName;
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new CatalogColumnMeta
                            {
                                Name = Convert.ToString(reader["name"]),
                                SqlType = Convert.ToString(reader["system_type_name"])
                            });
                        }
                    }
                }
            }
            catch
            {
                return new List<CatalogColumnMeta>();
            }

            return list;
        }

        private static IList<CatalogParameterMeta> LoadParameterMeta(SqlConnection connection, string objectName)
        {
            return LoadParameterMeta(connection, null, objectName);
        }

        private static IList<CatalogParameterMeta> LoadParameterMeta(SqlConnection connection, SqlTransaction transaction, string objectName)
        {
            var list = new List<CatalogParameterMeta>();
            const string sql = @"
SELECT p.name, t.name AS TypeName, p.is_output
FROM sys.parameters p
INNER JOIN sys.types t ON p.user_type_id = t.user_type_id
WHERE p.object_id = OBJECT_ID(@ObjectName)
ORDER BY p.parameter_id;";
            using (var command = new SqlCommand(sql, connection, transaction))
            {
                command.Parameters.Add("@ObjectName", SqlDbType.NVarChar, 256).Value = objectName;
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new CatalogParameterMeta
                        {
                            Name = Convert.ToString(reader["name"]),
                            SqlType = Convert.ToString(reader["TypeName"]),
                            IsOutput = Convert.ToBoolean(reader["is_output"])
                        });
                    }
                }
            }

            return list;
        }

        private static bool UpsertCatalog(SqlConnection connection, string scope, CatalogSourceRow source, ClassificationResult classification)
        {
            const string sql = @"
DECLARE @ExistingStatus NVARCHAR(20);
SELECT @ExistingStatus = ClassificationStatus
FROM dbo.DynamicReportCatalog
WHERE ProjectScope = @ProjectScope AND SourceType = @SourceType AND SourceSchema = @SourceSchema AND SourceName = @SourceName;

IF @ExistingStatus IS NULL
BEGIN
    INSERT INTO dbo.DynamicReportCatalog
    (ProjectScope, SourceType, SourceSchema, SourceName, ClassificationStatus, ClassificationScore, RiskFlags, SuggestedReportName, Notes)
    VALUES (@ProjectScope, @SourceType, @SourceSchema, @SourceName, @Status, @Score, @RiskFlags, @SuggestedReportName, @Notes);
    SELECT CAST(1 AS BIT);
END
ELSE
BEGIN
    UPDATE dbo.DynamicReportCatalog
    SET LastSeenAt = GETDATE(),
        ClassificationScore = @Score,
        RiskFlags = @RiskFlags,
        ClassificationStatus = CASE WHEN ClassificationStatus IN (N'Approved', N'Imported') OR (ClassificationStatus = N'Rejected' AND ApprovedBy IS NOT NULL) THEN ClassificationStatus ELSE @Status END,
        SuggestedReportName = CASE WHEN SuggestedReportName IS NULL THEN @SuggestedReportName ELSE SuggestedReportName END,
        Notes = @Notes
    WHERE ProjectScope = @ProjectScope AND SourceType = @SourceType AND SourceSchema = @SourceSchema AND SourceName = @SourceName;
    SELECT CAST(0 AS BIT);
END";
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.Add("@ProjectScope", SqlDbType.NVarChar, 20).Value = scope;
                command.Parameters.Add("@SourceType", SqlDbType.NVarChar, 20).Value = source.SourceType;
                command.Parameters.Add("@SourceSchema", SqlDbType.NVarChar, 64).Value = source.SourceSchema;
                command.Parameters.Add("@SourceName", SqlDbType.NVarChar, 128).Value = source.SourceName;
                command.Parameters.Add("@Status", SqlDbType.NVarChar, 20).Value = classification.Status;
                command.Parameters.Add("@Score", SqlDbType.Int).Value = classification.Score;
                command.Parameters.Add("@RiskFlags", SqlDbType.NVarChar, 500).Value = string.Join(",", classification.RiskFlags.ToArray());
                command.Parameters.Add("@SuggestedReportName", SqlDbType.NVarChar, 200).Value = source.SourceName;
                command.Parameters.Add("@Notes", SqlDbType.NVarChar, 1000).Value = DBNull.Value;
                return Convert.ToBoolean(command.ExecuteScalar());
            }
        }

        private static CatalogEntry GetEntry(SqlConnection connection, int catalogId, string scope)
        {
            return GetEntry(connection, null, catalogId, scope);
        }

        private static CatalogEntry GetEntry(SqlConnection connection, SqlTransaction transaction, int catalogId, string scope)
        {
            const string sql = @"
SELECT CatalogId, ProjectScope, SourceType, SourceSchema, SourceName, DiscoveredAt, LastSeenAt,
       ClassificationStatus, ClassificationScore, RiskFlags, SuggestedReportName, ApprovedBy,
       ApprovedAt, RejectionReason, ImportedReportId, ImportedAt, Notes
FROM dbo.DynamicReportCatalog
WHERE CatalogId = @CatalogId AND ProjectScope = @ProjectScope;";
            using (var command = new SqlCommand(sql, connection, transaction))
            {
                command.Parameters.Add("@CatalogId", SqlDbType.Int).Value = catalogId;
                command.Parameters.Add("@ProjectScope", SqlDbType.NVarChar, 20).Value = scope;
                using (var reader = command.ExecuteReader())
                {
                    return reader.Read() ? ReadCatalogEntry(reader) : null;
                }
            }
        }

        private static CatalogEntry ReadCatalogEntry(IDataRecord reader)
        {
            return new CatalogEntry
            {
                CatalogId = Convert.ToInt32(reader["CatalogId"]),
                ProjectScope = Convert.ToString(reader["ProjectScope"]),
                SourceType = Convert.ToString(reader["SourceType"]),
                SourceSchema = Convert.ToString(reader["SourceSchema"]),
                SourceName = Convert.ToString(reader["SourceName"]),
                DiscoveredAt = Convert.ToDateTime(reader["DiscoveredAt"]),
                LastSeenAt = Convert.ToDateTime(reader["LastSeenAt"]),
                ClassificationStatus = Convert.ToString(reader["ClassificationStatus"]),
                ClassificationScore = Convert.ToInt32(reader["ClassificationScore"]),
                RiskFlags = Convert.ToString(reader["RiskFlags"]),
                SuggestedReportName = Convert.ToString(reader["SuggestedReportName"]),
                ApprovedBy = reader["ApprovedBy"] == DBNull.Value ? null : (int?)Convert.ToInt32(reader["ApprovedBy"]),
                ApprovedAt = reader["ApprovedAt"] == DBNull.Value ? null : (DateTime?)Convert.ToDateTime(reader["ApprovedAt"]),
                RejectionReason = Convert.ToString(reader["RejectionReason"]),
                ImportedReportId = reader["ImportedReportId"] == DBNull.Value ? null : (int?)Convert.ToInt32(reader["ImportedReportId"]),
                ImportedAt = reader["ImportedAt"] == DBNull.Value ? null : (DateTime?)Convert.ToDateTime(reader["ImportedAt"]),
                Notes = Convert.ToString(reader["Notes"])
            };
        }

        private static string LoadBodyExcerpt(SqlConnection connection, string objectName)
        {
            const string sql = "SELECT LEFT(OBJECT_DEFINITION(OBJECT_ID(@ObjectName)), 4000);";
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.Add("@ObjectName", SqlDbType.NVarChar, 256).Value = objectName;
                return Convert.ToString(command.ExecuteScalar());
            }
        }

        private static IList<DynamicReportColumn> ToReportColumns(IList<CatalogColumnMeta> columns)
        {
            var list = new List<DynamicReportColumn>();
            for (var i = 0; i < columns.Count; i++)
            {
                list.Add(new DynamicReportColumn
                {
                    FieldName = columns[i].Name,
                    CaptionAr = columns[i].Name,
                    CaptionEn = columns[i].Name,
                    DataType = MapSqlType(columns[i].SqlType),
                    IsVisibleDefault = true,
                    IsFilterable = true,
                    IsSortable = true,
                    IsGroupable = true,
                    IsSummable = IsNumericSql(columns[i].SqlType),
                    Width = 140,
                    SortOrder = i
                });
            }

            return list;
        }

        private static IList<DynamicReportParameter> ToReportParameters(IList<CatalogParameterMeta> parameters)
        {
            var list = new List<DynamicReportParameter>();
            for (var i = 0; i < parameters.Count; i++)
            {
                if (parameters[i].IsOutput) continue;
                list.Add(new DynamicReportParameter
                {
                    ParameterName = ReportSqlSafety.NormalizeParameterName(parameters[i].Name),
                    CaptionAr = parameters[i].Name,
                    CaptionEn = parameters[i].Name,
                    DataType = MapSqlType(parameters[i].SqlType),
                    IsRequired = true,
                    SortOrder = i
                });
            }

            return list;
        }

        private static string BuildUniqueReportCode(SqlConnection connection, SqlTransaction transaction, string scope, string schema, string sourceName)
        {
            var baseCode = ("IMP_" + scope + "_" + schema + "_" + sourceName).Replace(".", "_").Replace(" ", "_");
            if (baseCode.Length > 85) baseCode = baseCode.Substring(0, 85);
            var candidate = baseCode;
            var index = 2;
            while (ReportCodeExists(connection, transaction, candidate))
            {
                candidate = baseCode + "_" + index;
                index++;
            }

            return candidate;
        }

        private static bool ReportCodeExists(SqlConnection connection, SqlTransaction transaction, string code)
        {
            using (var command = new SqlCommand("SELECT TOP (1) 1 FROM dbo.DynamicReportDefinitions WHERE ReportCode = @ReportCode;", connection, transaction))
            {
                command.Parameters.Add("@ReportCode", SqlDbType.NVarChar, 100).Value = code;
                return command.ExecuteScalar() != null;
            }
        }

        private static int InsertDefinition(SqlConnection connection, SqlTransaction transaction, CatalogEntry entry, string reportCode, DynamicReportUserContext user)
        {
            const string sql = @"
INSERT INTO dbo.DynamicReportDefinitions
(ReportCode, ReportNameAr, ReportNameEn, ProjectScope, SourceType, SourceName, RequireDateRange, MaxRows, CommandTimeoutSeconds, IsActive, CreatedBy, CreatedAt)
VALUES
(@ReportCode, @ReportNameAr, @ReportNameEn, @ProjectScope, @SourceType, @SourceName, 0, 5000, 30, 0, @CreatedBy, GETDATE());
SELECT CAST(SCOPE_IDENTITY() AS INT);";
            using (var command = new SqlCommand(sql, connection, transaction))
            {
                command.Parameters.Add("@ReportCode", SqlDbType.NVarChar, 100).Value = reportCode;
                command.Parameters.Add("@ReportNameAr", SqlDbType.NVarChar, 250).Value = string.IsNullOrWhiteSpace(entry.SuggestedReportName) ? entry.SourceName : entry.SuggestedReportName;
                command.Parameters.Add("@ReportNameEn", SqlDbType.NVarChar, 250).Value = entry.SourceName;
                command.Parameters.Add("@ProjectScope", SqlDbType.NVarChar, 20).Value = entry.ProjectScope;
                command.Parameters.Add("@SourceType", SqlDbType.NVarChar, 30).Value = entry.SourceType;
                command.Parameters.Add("@SourceName", SqlDbType.NVarChar, 256).Value = entry.SourceSchema + "." + entry.SourceName;
                command.Parameters.Add("@CreatedBy", SqlDbType.Int).Value = user.UserId > 0 ? (object)user.UserId : DBNull.Value;
                return Convert.ToInt32(command.ExecuteScalar());
            }
        }

        private static void InsertColumns(SqlConnection connection, SqlTransaction transaction, int reportId, IList<DynamicReportColumn> columns)
        {
            const string sql = @"
INSERT INTO dbo.DynamicReportColumns
(ReportId, FieldName, CaptionAr, CaptionEn, DataType, IsVisibleDefault, IsFilterable, IsSortable, IsGroupable, IsSummable, Width, SortOrder)
VALUES
(@ReportId, @FieldName, @CaptionAr, @CaptionEn, @DataType, 1, 1, 1, 1, @IsSummable, @Width, @SortOrder);";
            foreach (var column in columns)
            {
                using (var command = new SqlCommand(sql, connection, transaction))
                {
                    command.Parameters.Add("@ReportId", SqlDbType.Int).Value = reportId;
                    command.Parameters.Add("@FieldName", SqlDbType.NVarChar, 128).Value = column.FieldName;
                    command.Parameters.Add("@CaptionAr", SqlDbType.NVarChar, 250).Value = column.CaptionAr;
                    command.Parameters.Add("@CaptionEn", SqlDbType.NVarChar, 250).Value = column.CaptionEn;
                    command.Parameters.Add("@DataType", SqlDbType.NVarChar, 50).Value = column.DataType;
                    command.Parameters.Add("@IsSummable", SqlDbType.Bit).Value = column.IsSummable;
                    command.Parameters.Add("@Width", SqlDbType.Int).Value = column.Width ?? 140;
                    command.Parameters.Add("@SortOrder", SqlDbType.Int).Value = column.SortOrder;
                    command.ExecuteNonQuery();
                }
            }
        }

        private static void InsertParameters(SqlConnection connection, SqlTransaction transaction, int reportId, IList<DynamicReportParameter> parameters)
        {
            const string sql = @"
INSERT INTO dbo.DynamicReportParameters
(ReportId, ParameterName, CaptionAr, CaptionEn, DataType, IsRequired, DefaultValue, LookupKey, LookupSql, SortOrder)
VALUES
(@ReportId, @ParameterName, @CaptionAr, @CaptionEn, @DataType, 1, NULL, NULL, NULL, @SortOrder);";
            foreach (var parameter in parameters)
            {
                using (var command = new SqlCommand(sql, connection, transaction))
                {
                    command.Parameters.Add("@ReportId", SqlDbType.Int).Value = reportId;
                    command.Parameters.Add("@ParameterName", SqlDbType.NVarChar, 128).Value = parameter.ParameterName;
                    command.Parameters.Add("@CaptionAr", SqlDbType.NVarChar, 250).Value = parameter.CaptionAr;
                    command.Parameters.Add("@CaptionEn", SqlDbType.NVarChar, 250).Value = parameter.CaptionEn;
                    command.Parameters.Add("@DataType", SqlDbType.NVarChar, 30).Value = parameter.DataType;
                    command.Parameters.Add("@SortOrder", SqlDbType.Int).Value = parameter.SortOrder;
                    command.ExecuteNonQuery();
                }
            }
        }

        private static void MarkImported(SqlConnection connection, SqlTransaction transaction, int catalogId, string scope, int reportId)
        {
            const string sql = @"
UPDATE dbo.DynamicReportCatalog
SET ImportedReportId = @ReportId,
    ImportedAt = GETDATE(),
    ClassificationStatus = N'Imported'
WHERE CatalogId = @CatalogId
  AND ProjectScope = @ProjectScope
  AND ImportedReportId IS NULL;";
            using (var command = new SqlCommand(sql, connection, transaction))
            {
                command.Parameters.Add("@CatalogId", SqlDbType.Int).Value = catalogId;
                command.Parameters.Add("@ProjectScope", SqlDbType.NVarChar, 20).Value = scope;
                command.Parameters.Add("@ReportId", SqlDbType.Int).Value = reportId;
                if (command.ExecuteNonQuery() == 0) throw new InvalidOperationException("تعذر تحديث حالة الاستيراد.");
            }
        }

        private static object TrimOrDbNull(string value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value)) return DBNull.Value;
            value = value.Trim();
            return value.Length > maxLength ? value.Substring(0, maxLength) : value;
        }

        private static string MapSqlType(string sqlType)
        {
            var value = (sqlType ?? string.Empty).ToLowerInvariant();
            if (value.Contains("int")) return "Int";
            if (value.Contains("decimal") || value.Contains("numeric") || value.Contains("money") || value.Contains("float") || value.Contains("real")) return "Decimal";
            if (value.Contains("date") && !value.Contains("time")) return "Date";
            if (value.Contains("datetime") || value.Contains("time")) return "DateTime";
            if (value.Contains("bit")) return "Bool";
            if (value.Contains("uniqueidentifier")) return "Guid";
            return "String";
        }

        private static bool IsNumericSql(string sqlType)
        {
            var value = (sqlType ?? string.Empty).ToLowerInvariant();
            return value.Contains("int") || value.Contains("decimal") || value.Contains("numeric") ||
                   value.Contains("money") || value.Contains("float") || value.Contains("real");
        }

        private class CatalogSourceRow
        {
            public CatalogSourceRow()
            {
                Parameters = new List<CatalogParameterMeta>();
                Columns = new List<CatalogColumnMeta>();
            }

            public string SourceType { get; set; }
            public string SourceSchema { get; set; }
            public string SourceName { get; set; }
            public string Body { get; set; }
            public IList<CatalogParameterMeta> Parameters { get; set; }
            public IList<CatalogColumnMeta> Columns { get; set; }

            public CatalogSourceMeta ToClassificationMeta()
            {
                return new CatalogSourceMeta
                {
                    Schema = SourceSchema,
                    Name = SourceName,
                    Body = Body,
                    Parameters = Parameters,
                    Columns = Columns
                };
            }
        }
    }
}
