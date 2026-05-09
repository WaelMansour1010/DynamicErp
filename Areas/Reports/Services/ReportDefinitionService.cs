using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using MyERP.Areas.Reports.Models;

namespace MyERP.Areas.Reports.Services
{
    public class ReportDefinitionService
    {
        private readonly DynamicReportConnectionFactory _connectionFactory;

        public ReportDefinitionService()
            : this(new DynamicReportConnectionFactory())
        {
        }

        public ReportDefinitionService(DynamicReportConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public IList<DynamicReportDefinition> GetDefinitions(string scope, bool includeInactive)
        {
            var list = new List<DynamicReportDefinition>();
            const string sql = @"
SELECT ReportId, ReportCode, ReportNameAr, ReportNameEn, ProjectScope, SourceType, SourceName,
       RequireDateRange, MaxRows, CommandTimeoutSeconds, IsActive,
       LifecycleStatus, LastValidatedAt, ActivatedBy, ActivatedAt
FROM dbo.DynamicReportDefinitions
WHERE (@includeInactive = 1 OR IsActive = 1)
  AND (ProjectScope = @scope OR ProjectScope = N'Shared' OR @scope = N'Shared')
ORDER BY ReportNameAr, ReportNameEn;";
            using (var connection = _connectionFactory.CreateOpenConnection(scope))
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.Add("@scope", SqlDbType.NVarChar, 20).Value = DynamicReportScopes.Normalize(scope);
                command.Parameters.Add("@includeInactive", SqlDbType.Bit).Value = includeInactive;
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read()) list.Add(ReadDefinition(reader));
                }
            }

            return list;
        }

        public DynamicReportDefinition GetDefinition(int reportId, string scope)
        {
            const string sql = @"
SELECT ReportId, ReportCode, ReportNameAr, ReportNameEn, ProjectScope, SourceType, SourceName,
       RequireDateRange, MaxRows, CommandTimeoutSeconds, IsActive,
       LifecycleStatus, LastValidatedAt, ActivatedBy, ActivatedAt
FROM dbo.DynamicReportDefinitions
WHERE ReportId = @reportId;";
            DynamicReportDefinition definition = null;
            using (var connection = _connectionFactory.CreateOpenConnection(scope))
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.Add("@reportId", SqlDbType.Int).Value = reportId;
                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read()) definition = ReadDefinition(reader);
                }
            }

            if (definition != null)
            {
                definition.Parameters = GetParameters(reportId, scope);
                definition.Columns = GetColumns(reportId, scope);
            }

            return definition;
        }

        public int SaveDefinition(DynamicReportDefinition definition, DynamicReportUserContext user)
        {
            if (definition == null) throw new ArgumentNullException("definition");
            definition.ProjectScope = DynamicReportScopes.Normalize(definition.ProjectScope);
            definition.SourceType = string.Equals(definition.SourceType, "View", StringComparison.OrdinalIgnoreCase) ? "View" : "StoredProcedure";
            ValidateDefinition(definition);
            ReportSqlSafety.ValidateSourceName(definition.SourceName);

            using (var connection = _connectionFactory.CreateOpenConnection(definition.ProjectScope))
            using (var transaction = connection.BeginTransaction())
            {
                var id = definition.ReportId > 0
                    ? UpdateDefinition(connection, transaction, definition, user)
                    : InsertDefinition(connection, transaction, definition, user);
                ReplaceParameters(connection, transaction, id, definition.Parameters);
                ReplaceColumns(connection, transaction, id, definition.Columns);
                transaction.Commit();
                return id;
            }
        }

        private static void ValidateDefinition(DynamicReportDefinition definition)
        {
            if (string.IsNullOrWhiteSpace(definition.ReportCode)) throw new InvalidOperationException("Report code is required.");
            if (string.IsNullOrWhiteSpace(definition.ReportNameAr) && string.IsNullOrWhiteSpace(definition.ReportNameEn)) throw new InvalidOperationException("Report name is required.");
            if (string.IsNullOrWhiteSpace(definition.SourceName)) throw new InvalidOperationException("Report source is required.");
            definition.ReportCode = definition.ReportCode.Trim();
            definition.ReportNameAr = string.IsNullOrWhiteSpace(definition.ReportNameAr) ? definition.ReportNameEn : definition.ReportNameAr.Trim();
            definition.ReportNameEn = string.IsNullOrWhiteSpace(definition.ReportNameEn) ? definition.ReportNameAr : definition.ReportNameEn.Trim();
            definition.MaxRows = Math.Max(1, Math.Min(definition.MaxRows, 50000));
            definition.CommandTimeoutSeconds = Math.Max(5, Math.Min(definition.CommandTimeoutSeconds, 300));
        }

        public IList<DynamicReportParameter> GetParameters(int reportId, string scope)
        {
            var list = new List<DynamicReportParameter>();
            const string sql = @"
SELECT ParameterId, ReportId, ParameterName, CaptionAr, CaptionEn, DataType, IsRequired,
       DefaultValue, LookupKey, LookupSql, SortOrder
FROM dbo.DynamicReportParameters
WHERE ReportId = @reportId
ORDER BY SortOrder, ParameterId;";
            using (var connection = _connectionFactory.CreateOpenConnection(scope))
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.Add("@reportId", SqlDbType.Int).Value = reportId;
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new DynamicReportParameter
                        {
                            ParameterId = Convert.ToInt32(reader["ParameterId"]),
                            ReportId = Convert.ToInt32(reader["ReportId"]),
                            ParameterName = Convert.ToString(reader["ParameterName"]),
                            CaptionAr = Convert.ToString(reader["CaptionAr"]),
                            CaptionEn = Convert.ToString(reader["CaptionEn"]),
                            DataType = Convert.ToString(reader["DataType"]),
                            IsRequired = Convert.ToBoolean(reader["IsRequired"]),
                            DefaultValue = Convert.ToString(reader["DefaultValue"]),
                            LookupKey = Convert.ToString(reader["LookupKey"]),
                            LookupSql = Convert.ToString(reader["LookupSql"]),
                            SortOrder = Convert.ToInt32(reader["SortOrder"])
                        });
                    }
                }
            }

            return list;
        }

        public IList<DynamicReportColumn> GetColumns(int reportId, string scope)
        {
            var list = new List<DynamicReportColumn>();
            const string sql = @"
SELECT ColumnId, ReportId, FieldName, CaptionAr, CaptionEn, DataType, IsVisibleDefault,
       IsFilterable, IsSortable, IsGroupable, IsSummable, Width, SortOrder
FROM dbo.DynamicReportColumns
WHERE ReportId = @reportId
ORDER BY SortOrder, ColumnId;";
            using (var connection = _connectionFactory.CreateOpenConnection(scope))
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.Add("@reportId", SqlDbType.Int).Value = reportId;
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read()) list.Add(ReadColumn(reader));
                }
            }

            return list;
        }

        private static int InsertDefinition(SqlConnection connection, SqlTransaction transaction, DynamicReportDefinition definition, DynamicReportUserContext user)
        {
            const string sql = @"
INSERT INTO dbo.DynamicReportDefinitions
(ReportCode, ReportNameAr, ReportNameEn, ProjectScope, SourceType, SourceName, RequireDateRange, MaxRows, CommandTimeoutSeconds, IsActive, LifecycleStatus, LastValidatedAt, ActivatedBy, ActivatedAt, CreatedBy, CreatedAt)
VALUES
(@ReportCode, @ReportNameAr, @ReportNameEn, @ProjectScope, @SourceType, @SourceName, @RequireDateRange, @MaxRows, @CommandTimeoutSeconds, 0, N'Draft', NULL, NULL, NULL, @UserId, GETDATE());
SELECT CAST(SCOPE_IDENTITY() AS INT);";
            using (var command = new SqlCommand(sql, connection, transaction))
            {
                AddDefinitionParameters(command, definition, user);
                return Convert.ToInt32(command.ExecuteScalar());
            }
        }

        private static int UpdateDefinition(SqlConnection connection, SqlTransaction transaction, DynamicReportDefinition definition, DynamicReportUserContext user)
        {
            const string sql = @"
UPDATE dbo.DynamicReportDefinitions
SET ReportCode = @ReportCode, ReportNameAr = @ReportNameAr, ReportNameEn = @ReportNameEn,
    ProjectScope = @ProjectScope, SourceType = @SourceType, SourceName = @SourceName,
    RequireDateRange = @RequireDateRange, MaxRows = @MaxRows, CommandTimeoutSeconds = @CommandTimeoutSeconds,
    UpdatedBy = @UserId, UpdatedAt = GETDATE()
WHERE ReportId = @ReportId;";
            using (var command = new SqlCommand(sql, connection, transaction))
            {
                AddDefinitionParameters(command, definition, user);
                command.Parameters.Add("@ReportId", SqlDbType.Int).Value = definition.ReportId;
                command.ExecuteNonQuery();
                return definition.ReportId;
            }
        }

        private static void ReplaceParameters(SqlConnection connection, SqlTransaction transaction, int reportId, IEnumerable<DynamicReportParameter> parameters)
        {
            using (var delete = new SqlCommand("DELETE FROM dbo.DynamicReportParameters WHERE ReportId = @ReportId", connection, transaction))
            {
                delete.Parameters.Add("@ReportId", SqlDbType.Int).Value = reportId;
                delete.ExecuteNonQuery();
            }
            if (parameters == null) return;
            const string sql = @"INSERT INTO dbo.DynamicReportParameters
(ReportId, ParameterName, CaptionAr, CaptionEn, DataType, IsRequired, DefaultValue, LookupKey, LookupSql, SortOrder)
VALUES (@ReportId, @ParameterName, @CaptionAr, @CaptionEn, @DataType, @IsRequired, @DefaultValue, @LookupKey, @LookupSql, @SortOrder);";
            foreach (var p in parameters)
            {
                if (string.IsNullOrWhiteSpace(p.ParameterName)) continue;
                using (var command = new SqlCommand(sql, connection, transaction))
                {
                    command.Parameters.Add("@ReportId", SqlDbType.Int).Value = reportId;
                    command.Parameters.Add("@ParameterName", SqlDbType.NVarChar, 128).Value = ReportSqlSafety.NormalizeParameterName(p.ParameterName);
                    command.Parameters.Add("@CaptionAr", SqlDbType.NVarChar, 250).Value = (object)p.CaptionAr ?? DBNull.Value;
                    command.Parameters.Add("@CaptionEn", SqlDbType.NVarChar, 250).Value = (object)p.CaptionEn ?? DBNull.Value;
                    command.Parameters.Add("@DataType", SqlDbType.NVarChar, 30).Value = ReportSqlSafety.NormalizeDataType(p.DataType);
                    command.Parameters.Add("@IsRequired", SqlDbType.Bit).Value = p.IsRequired;
                    command.Parameters.Add("@DefaultValue", SqlDbType.NVarChar, 1000).Value = (object)p.DefaultValue ?? DBNull.Value;
                    command.Parameters.Add("@LookupKey", SqlDbType.NVarChar, 100).Value = (object)p.LookupKey ?? DBNull.Value;
                    command.Parameters.Add("@LookupSql", SqlDbType.NVarChar).Value = (object)p.LookupSql ?? DBNull.Value;
                    command.Parameters.Add("@SortOrder", SqlDbType.Int).Value = p.SortOrder;
                    command.ExecuteNonQuery();
                }
            }
        }

        private static void ReplaceColumns(SqlConnection connection, SqlTransaction transaction, int reportId, IEnumerable<DynamicReportColumn> columns)
        {
            using (var delete = new SqlCommand("DELETE FROM dbo.DynamicReportColumns WHERE ReportId = @ReportId", connection, transaction))
            {
                delete.Parameters.Add("@ReportId", SqlDbType.Int).Value = reportId;
                delete.ExecuteNonQuery();
            }
            if (columns == null) return;
            const string sql = @"INSERT INTO dbo.DynamicReportColumns
(ReportId, FieldName, CaptionAr, CaptionEn, DataType, IsVisibleDefault, IsFilterable, IsSortable, IsGroupable, IsSummable, Width, SortOrder)
VALUES (@ReportId, @FieldName, @CaptionAr, @CaptionEn, @DataType, @IsVisibleDefault, @IsFilterable, @IsSortable, @IsGroupable, @IsSummable, @Width, @SortOrder);";
            foreach (var c in columns)
            {
                if (string.IsNullOrWhiteSpace(c.FieldName)) continue;
                using (var command = new SqlCommand(sql, connection, transaction))
                {
                    command.Parameters.Add("@ReportId", SqlDbType.Int).Value = reportId;
                    command.Parameters.Add("@FieldName", SqlDbType.NVarChar, 128).Value = c.FieldName.Trim();
                    command.Parameters.Add("@CaptionAr", SqlDbType.NVarChar, 250).Value = (object)c.CaptionAr ?? DBNull.Value;
                    command.Parameters.Add("@CaptionEn", SqlDbType.NVarChar, 250).Value = (object)c.CaptionEn ?? DBNull.Value;
                    command.Parameters.Add("@DataType", SqlDbType.NVarChar, 50).Value = (object)c.DataType ?? DBNull.Value;
                    command.Parameters.Add("@IsVisibleDefault", SqlDbType.Bit).Value = c.IsVisibleDefault;
                    command.Parameters.Add("@IsFilterable", SqlDbType.Bit).Value = c.IsFilterable;
                    command.Parameters.Add("@IsSortable", SqlDbType.Bit).Value = c.IsSortable;
                    command.Parameters.Add("@IsGroupable", SqlDbType.Bit).Value = c.IsGroupable;
                    command.Parameters.Add("@IsSummable", SqlDbType.Bit).Value = c.IsSummable;
                    command.Parameters.Add("@Width", SqlDbType.Int).Value = (object)c.Width ?? DBNull.Value;
                    command.Parameters.Add("@SortOrder", SqlDbType.Int).Value = c.SortOrder;
                    command.ExecuteNonQuery();
                }
            }
        }

        private static void AddDefinitionParameters(SqlCommand command, DynamicReportDefinition definition, DynamicReportUserContext user)
        {
            command.Parameters.Add("@ReportCode", SqlDbType.NVarChar, 100).Value = (definition.ReportCode ?? string.Empty).Trim();
            command.Parameters.Add("@ReportNameAr", SqlDbType.NVarChar, 250).Value = (definition.ReportNameAr ?? string.Empty).Trim();
            command.Parameters.Add("@ReportNameEn", SqlDbType.NVarChar, 250).Value = (object)definition.ReportNameEn ?? DBNull.Value;
            command.Parameters.Add("@ProjectScope", SqlDbType.NVarChar, 20).Value = definition.ProjectScope;
            command.Parameters.Add("@SourceType", SqlDbType.NVarChar, 30).Value = definition.SourceType;
            command.Parameters.Add("@SourceName", SqlDbType.NVarChar, 256).Value = (definition.SourceName ?? string.Empty).Trim();
            command.Parameters.Add("@RequireDateRange", SqlDbType.Bit).Value = definition.RequireDateRange;
            command.Parameters.Add("@MaxRows", SqlDbType.Int).Value = Math.Max(1, Math.Min(definition.MaxRows, 50000));
            command.Parameters.Add("@CommandTimeoutSeconds", SqlDbType.Int).Value = Math.Max(5, Math.Min(definition.CommandTimeoutSeconds, 300));
            command.Parameters.Add("@IsActive", SqlDbType.Bit).Value = definition.IsActive;
            command.Parameters.Add("@UserId", SqlDbType.Int).Value = user != null && user.UserId > 0 ? (object)user.UserId : DBNull.Value;
        }

        private static DynamicReportDefinition ReadDefinition(IDataRecord reader)
        {
            return new DynamicReportDefinition
            {
                ReportId = Convert.ToInt32(reader["ReportId"]),
                ReportCode = Convert.ToString(reader["ReportCode"]),
                ReportNameAr = Convert.ToString(reader["ReportNameAr"]),
                ReportNameEn = Convert.ToString(reader["ReportNameEn"]),
                ProjectScope = Convert.ToString(reader["ProjectScope"]),
                SourceType = Convert.ToString(reader["SourceType"]),
                SourceName = Convert.ToString(reader["SourceName"]),
                RequireDateRange = Convert.ToBoolean(reader["RequireDateRange"]),
                MaxRows = Convert.ToInt32(reader["MaxRows"]),
                CommandTimeoutSeconds = Convert.ToInt32(reader["CommandTimeoutSeconds"]),
                IsActive = Convert.ToBoolean(reader["IsActive"]),
                LifecycleStatus = reader["LifecycleStatus"] == DBNull.Value ? (Convert.ToBoolean(reader["IsActive"]) ? LifecycleStatusEnum.Active : LifecycleStatusEnum.Disabled) : Convert.ToString(reader["LifecycleStatus"]),
                LastValidatedAt = reader["LastValidatedAt"] == DBNull.Value ? null : (DateTime?)Convert.ToDateTime(reader["LastValidatedAt"]),
                ActivatedBy = reader["ActivatedBy"] == DBNull.Value ? null : (int?)Convert.ToInt32(reader["ActivatedBy"]),
                ActivatedAt = reader["ActivatedAt"] == DBNull.Value ? null : (DateTime?)Convert.ToDateTime(reader["ActivatedAt"])
            };
        }

        private static DynamicReportColumn ReadColumn(IDataRecord reader)
        {
            return new DynamicReportColumn
            {
                ColumnId = Convert.ToInt32(reader["ColumnId"]),
                ReportId = Convert.ToInt32(reader["ReportId"]),
                FieldName = Convert.ToString(reader["FieldName"]),
                CaptionAr = Convert.ToString(reader["CaptionAr"]),
                CaptionEn = Convert.ToString(reader["CaptionEn"]),
                DataType = Convert.ToString(reader["DataType"]),
                IsVisibleDefault = Convert.ToBoolean(reader["IsVisibleDefault"]),
                IsFilterable = Convert.ToBoolean(reader["IsFilterable"]),
                IsSortable = Convert.ToBoolean(reader["IsSortable"]),
                IsGroupable = Convert.ToBoolean(reader["IsGroupable"]),
                IsSummable = Convert.ToBoolean(reader["IsSummable"]),
                Width = reader["Width"] == DBNull.Value ? null : (int?)Convert.ToInt32(reader["Width"]),
                SortOrder = Convert.ToInt32(reader["SortOrder"])
            };
        }
    }
}
