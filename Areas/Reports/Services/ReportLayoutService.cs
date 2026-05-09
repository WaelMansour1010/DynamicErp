using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using MyERP.Areas.Reports.Models;

namespace MyERP.Areas.Reports.Services
{
    public class ReportLayoutService
    {
        private readonly DynamicReportConnectionFactory _connectionFactory;
        private readonly ReportDesignerStateService _designerStateService = new ReportDesignerStateService();

        public ReportLayoutService()
            : this(new DynamicReportConnectionFactory())
        {
        }

        public ReportLayoutService(DynamicReportConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public IList<DynamicReportLayout> GetLayouts(int reportId, DynamicReportUserContext user)
        {
            var list = new List<DynamicReportLayout>();
            const string sql = @"SELECT LayoutId, ReportId, UserId, ProjectScope, LayoutName, LayoutJson, IsDefault, CreatedAt, UpdatedAt
FROM dbo.DynamicReportLayouts
WHERE ReportId = @ReportId AND UserId = @UserId AND ProjectScope = @ProjectScope
ORDER BY IsDefault DESC, LayoutName;";
            using (var connection = _connectionFactory.CreateOpenConnection(user.ProjectScope))
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.Add("@ReportId", SqlDbType.Int).Value = reportId;
                command.Parameters.Add("@UserId", SqlDbType.Int).Value = user.UserId;
                command.Parameters.Add("@ProjectScope", SqlDbType.NVarChar, 20).Value = user.ProjectScope;
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new DynamicReportLayout
                        {
                            LayoutId = Convert.ToInt32(reader["LayoutId"]),
                            ReportId = Convert.ToInt32(reader["ReportId"]),
                            UserId = Convert.ToInt32(reader["UserId"]),
                            ProjectScope = Convert.ToString(reader["ProjectScope"]),
                            LayoutName = Convert.ToString(reader["LayoutName"]),
                            LayoutJson = Convert.ToString(reader["LayoutJson"]),
                            IsDefault = Convert.ToBoolean(reader["IsDefault"]),
                            CreatedAt = Convert.ToDateTime(reader["CreatedAt"]),
                            UpdatedAt = reader["UpdatedAt"] == DBNull.Value ? null : (DateTime?)Convert.ToDateTime(reader["UpdatedAt"])
                        });
                    }
                }
            }
            return list;
        }

        public int SaveLayout(int reportId, string layoutName, string layoutJson, bool isDefault, DynamicReportUserContext user)
        {
            if (string.IsNullOrWhiteSpace(layoutName)) layoutName = "Default";
            layoutJson = _designerStateService.NormalizeLayoutJson(layoutJson);
            layoutName = layoutName.Trim();
            using (var connection = _connectionFactory.CreateOpenConnection(user.ProjectScope))
            using (var transaction = connection.BeginTransaction())
            {
                if (isDefault)
                {
                    using (var clear = new SqlCommand("UPDATE dbo.DynamicReportLayouts SET IsDefault = 0 WHERE ReportId = @ReportId AND UserId = @UserId AND ProjectScope = @ProjectScope", connection, transaction))
                    {
                        clear.Parameters.Add("@ReportId", SqlDbType.Int).Value = reportId;
                        clear.Parameters.Add("@UserId", SqlDbType.Int).Value = user.UserId;
                        clear.Parameters.Add("@ProjectScope", SqlDbType.NVarChar, 20).Value = user.ProjectScope;
                        clear.ExecuteNonQuery();
                    }
                }

                int existingLayoutId;
                using (var find = new SqlCommand(@"SELECT TOP (1) LayoutId
FROM dbo.DynamicReportLayouts
WHERE ReportId = @ReportId AND UserId = @UserId AND ProjectScope = @ProjectScope AND LayoutName = @LayoutName
ORDER BY LayoutId;", connection, transaction))
                {
                    find.Parameters.Add("@ReportId", SqlDbType.Int).Value = reportId;
                    find.Parameters.Add("@UserId", SqlDbType.Int).Value = user.UserId;
                    find.Parameters.Add("@ProjectScope", SqlDbType.NVarChar, 20).Value = user.ProjectScope;
                    find.Parameters.Add("@LayoutName", SqlDbType.NVarChar, 250).Value = layoutName;
                    var existing = find.ExecuteScalar();
                    existingLayoutId = existing == null || existing == DBNull.Value ? 0 : Convert.ToInt32(existing);
                }

                if (existingLayoutId > 0)
                {
                    using (var update = new SqlCommand(@"UPDATE dbo.DynamicReportLayouts
SET LayoutJson = @LayoutJson, IsDefault = @IsDefault, UpdatedAt = GETDATE()
WHERE LayoutId = @LayoutId AND UserId = @UserId AND ProjectScope = @ProjectScope;", connection, transaction))
                    {
                        update.Parameters.Add("@LayoutId", SqlDbType.Int).Value = existingLayoutId;
                        update.Parameters.Add("@UserId", SqlDbType.Int).Value = user.UserId;
                        update.Parameters.Add("@ProjectScope", SqlDbType.NVarChar, 20).Value = user.ProjectScope;
                        update.Parameters.Add("@LayoutJson", SqlDbType.NVarChar).Value = layoutJson;
                        update.Parameters.Add("@IsDefault", SqlDbType.Bit).Value = isDefault;
                        update.ExecuteNonQuery();
                        transaction.Commit();
                        return existingLayoutId;
                    }
                }

                using (var command = new SqlCommand(@"INSERT INTO dbo.DynamicReportLayouts
(ReportId, UserId, ProjectScope, LayoutName, LayoutJson, IsDefault, CreatedAt)
VALUES (@ReportId, @UserId, @ProjectScope, @LayoutName, @LayoutJson, @IsDefault, GETDATE());
SELECT CAST(SCOPE_IDENTITY() AS INT);", connection, transaction))
                {
                    command.Parameters.Add("@ReportId", SqlDbType.Int).Value = reportId;
                    command.Parameters.Add("@UserId", SqlDbType.Int).Value = user.UserId;
                    command.Parameters.Add("@ProjectScope", SqlDbType.NVarChar, 20).Value = user.ProjectScope;
                    command.Parameters.Add("@LayoutName", SqlDbType.NVarChar, 250).Value = layoutName;
                    command.Parameters.Add("@LayoutJson", SqlDbType.NVarChar).Value = layoutJson;
                    command.Parameters.Add("@IsDefault", SqlDbType.Bit).Value = isDefault;
                    var id = Convert.ToInt32(command.ExecuteScalar());
                    transaction.Commit();
                    return id;
                }
            }
        }

        public void DeleteLayout(int layoutId, DynamicReportUserContext user)
        {
            const string sql = @"DELETE FROM dbo.DynamicReportLayouts
WHERE LayoutId = @LayoutId AND UserId = @UserId AND ProjectScope = @ProjectScope;";
            using (var connection = _connectionFactory.CreateOpenConnection(user.ProjectScope))
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.Add("@LayoutId", SqlDbType.Int).Value = layoutId;
                command.Parameters.Add("@UserId", SqlDbType.Int).Value = user.UserId;
                command.Parameters.Add("@ProjectScope", SqlDbType.NVarChar, 20).Value = user.ProjectScope;
                command.ExecuteNonQuery();
            }
        }
    }
}
