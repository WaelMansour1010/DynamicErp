using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Security.Claims;
using System.Web;
using MyERP.Areas.Sync.Security;
using MyERP.Areas.Sync.ViewModels;

namespace MyERP.Areas.Sync.Data
{
    public class SyncAdminRepository
    {
        public AdminOperationViewModel GetOperations(AdminOperationRequest request)
        {
            using (var connection = SyncDb.Open())
            {
                return new AdminOperationViewModel
                {
                    Request = request ?? NewDefaultRequest(),
                    Operations = GetRecentOperations(connection),
                    Checks = GetOperationChecks(request)
                };
            }
        }

        public long QueueOperation(AdminOperationRequest request, HttpContextBase context)
        {
            ValidateRequest(request, context);

            using (var connection = SyncDb.Open())
            using (var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted))
            {
                EnsureAdminTablesExist(connection, transaction);
                var id = InsertOperation(connection, transaction, request, context);
                InsertApproval(connection, transaction, request, context);
                InsertAudit(connection, transaction, request, context, "Queued", "Queued admin operation; execution not performed by browser.");
                transaction.Commit();
                return id;
            }
        }

        public IList<NotificationRow> GetNotifications()
        {
            using (var connection = SyncDb.Open())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
IF OBJECT_ID('dbo.Sync_AdminNotification','U') IS NULL
BEGIN
    SELECT TOP (0)
        CAST(0 AS BIGINT) AS NotificationId,
        CAST(NULL AS DATETIME) AS CreatedAt,
        CAST(NULL AS DATETIME) AS ReadAt,
        CAST(N'' AS NVARCHAR(50)) AS NotificationType,
        CAST(N'' AS NVARCHAR(20)) AS Severity,
        CAST(N'' AS NVARCHAR(200)) AS Title,
        CAST(N'' AS NVARCHAR(MAX)) AS Message,
        CAST(N'' AS NVARCHAR(100)) AS SyncKey,
        CAST(N'' AS NVARCHAR(20)) AS BranchId,
        CAST(N'' AS NVARCHAR(20)) AS Status;
END
ELSE
BEGIN
    SELECT TOP (100) NotificationId, CreatedAt, ReadAt, NotificationType, Severity, Title, Message, SyncKey, BranchId, Status
    FROM dbo.Sync_AdminNotification
    ORDER BY CreatedAt DESC, NotificationId DESC;
END";
                return ReadNotifications(command);
            }
        }

        public RolePermissionViewModel GetRolePermissions()
        {
            using (var connection = SyncDb.Open())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
IF OBJECT_ID('dbo.Sync_AdminRolePermission','U') IS NULL
BEGIN
    SELECT TOP (0)
        CAST(N'' AS NVARCHAR(100)) AS RoleName,
        CAST(N'' AS NVARCHAR(100)) AS Permission,
        CAST(0 AS BIT) AS IsEnabled,
        CAST(N'' AS NVARCHAR(MAX)) AS Notes;
END
ELSE
BEGIN
    SELECT RoleName, Permission, IsEnabled, Notes
    FROM dbo.Sync_AdminRolePermission
    ORDER BY RoleName, Permission;
END";
                var rows = new List<RolePermissionRow>();
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        rows.Add(new RolePermissionRow
                        {
                            RoleName = GetString(reader, "RoleName"),
                            Permission = GetString(reader, "Permission"),
                            IsEnabled = GetBool(reader, "IsEnabled"),
                            Notes = GetString(reader, "Notes")
                        });
                    }
                }

                if (rows.Count == 0)
                {
                    rows.Add(new RolePermissionRow { RoleName = "SyncViewer", Permission = SyncPermissions.View, IsEnabled = true, Notes = "Read-only dashboard and queue." });
                    rows.Add(new RolePermissionRow { RoleName = "SyncDiagnostics", Permission = SyncPermissions.Diagnostics, IsEnabled = true, Notes = "Diagnostics, logs, and comparisons." });
                    rows.Add(new RolePermissionRow { RoleName = "SyncOperator", Permission = SyncPermissions.AdminOperations, IsEnabled = false, Notes = "Can request queued operations after approval gates." });
                    rows.Add(new RolePermissionRow { RoleName = "SyncAdmin", Permission = SyncPermissions.ApplyMode, IsEnabled = false, Notes = "Required for future approved apply queue requests." });
                }

                return new RolePermissionViewModel { Roles = rows };
            }
        }

        private static AdminOperationRequest NewDefaultRequest()
        {
            return new AdminOperationRequest
            {
                OperationType = "PrepareApplySingle",
                ProfileName = "POSOnly",
                ApplySingleSyncKeyOnly = true,
                MaxInvoicesPerRun = 1
            };
        }

        private static IList<AdminOperationRow> GetRecentOperations(SqlConnection connection)
        {
            var rows = new List<AdminOperationRow>();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
IF OBJECT_ID('dbo.Sync_AdminOperation','U') IS NOT NULL
BEGIN
    SELECT TOP (100) AdminOperationId, CreatedAt, ApprovedAt, StartedAt, CompletedAt,
           RequestedBy, ApprovedBy, OperationType, Permission, ProfileName, SyncKey,
           Status, Result, Reason, WorkerName, LastError
    FROM dbo.Sync_AdminOperation
    ORDER BY CreatedAt DESC, AdminOperationId DESC;
END";
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        rows.Add(new AdminOperationRow
                        {
                            AdminOperationId = GetInt64(reader, "AdminOperationId"),
                            CreatedAt = GetDate(reader, "CreatedAt"),
                            ApprovedAt = GetDate(reader, "ApprovedAt"),
                            StartedAt = GetDate(reader, "StartedAt"),
                            CompletedAt = GetDate(reader, "CompletedAt"),
                            RequestedBy = GetString(reader, "RequestedBy"),
                            ApprovedBy = GetString(reader, "ApprovedBy"),
                            OperationType = GetString(reader, "OperationType"),
                            Permission = GetString(reader, "Permission"),
                            ProfileName = GetString(reader, "ProfileName"),
                            SyncKey = GetString(reader, "SyncKey"),
                            Status = GetString(reader, "Status"),
                            Result = GetString(reader, "Result"),
                            Reason = GetString(reader, "Reason"),
                            WorkerName = GetString(reader, "WorkerName"),
                            LastError = GetString(reader, "LastError")
                        });
                    }
                }
            }

            return rows;
        }

        private static IList<ReadinessCheckRow> GetOperationChecks(AdminOperationRequest request)
        {
            var checks = new List<ReadinessCheckRow>();
            checks.Add(new ReadinessCheckRow { CheckName = "Direct browser apply", Status = "Blocked", Message = "No direct ApplyMode execution from browser.", IsHardBlocker = false });
            checks.Add(new ReadinessCheckRow { CheckName = "ApplySingleSyncKey", Status = "Required", Message = "Only one SyncKey can be queued.", IsHardBlocker = false });
            checks.Add(new ReadinessCheckRow { CheckName = "MaxInvoicesPerRun", Status = "1", Message = "Batch apply remains blocked.", IsHardBlocker = false });
            if (request != null && String.IsNullOrWhiteSpace(request.SyncKey))
            {
                checks.Add(new ReadinessCheckRow { CheckName = "SyncKey", Status = "Needs review", Message = "SyncKey is required before queuing.", IsHardBlocker = true });
            }

            return checks;
        }

        private static void ValidateRequest(AdminOperationRequest request, HttpContextBase context)
        {
            if (request == null)
            {
                throw new InvalidOperationException("Missing operation request.");
            }

            if (String.IsNullOrWhiteSpace(request.SyncKey) || request.SyncKey.IndexOf(":Invoice:", StringComparison.OrdinalIgnoreCase) < 0)
            {
                throw new InvalidOperationException("A single invoice SyncKey is required.");
            }

            if (!request.ApplySingleSyncKeyOnly || request.MaxInvoicesPerRun != 1)
            {
                throw new InvalidOperationException("ApplySingleSyncKeyOnly=true and MaxInvoicesPerRun=1 are mandatory.");
            }

            if (!request.ApprovalConfirmed)
            {
                throw new InvalidOperationException("Approval checkbox is required.");
            }

            if (String.IsNullOrWhiteSpace(request.Reason) || request.Reason.Trim().Length < 10)
            {
                throw new InvalidOperationException("Approval reason must be at least 10 characters.");
            }

            if (String.IsNullOrWhiteSpace(request.PasswordConfirmation))
            {
                throw new InvalidOperationException("Password confirmation is required.");
            }
        }

        private static void EnsureAdminTablesExist(SqlConnection connection, SqlTransaction transaction)
        {
            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = @"
IF OBJECT_ID('dbo.Sync_AdminOperation','U') IS NULL
    RAISERROR('Sync_AdminOperation table is missing. Apply approved SQL script 002 first.', 16, 1);
IF OBJECT_ID('dbo.Sync_AdminAudit','U') IS NULL
    RAISERROR('Sync_AdminAudit table is missing. Apply approved SQL script 002 first.', 16, 1);
IF OBJECT_ID('dbo.Sync_AdminApproval','U') IS NULL
    RAISERROR('Sync_AdminApproval table is missing. Apply approved SQL script 002 first.', 16, 1);";
                command.ExecuteNonQuery();
            }
        }

        private static long InsertOperation(SqlConnection connection, SqlTransaction transaction, AdminOperationRequest request, HttpContextBase context)
        {
            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = @"
INSERT dbo.Sync_AdminOperation
(
    CreatedAt, RequestedBy, ApprovedBy, ApprovedAt, MachineName, IpAddress,
    OperationType, Permission, ProfileName, SyncKey, Status, Result, Reason,
    ApplySingleSyncKeyOnly, MaxInvoicesPerRun
)
VALUES
(
    GETDATE(), @UserName, @UserName, GETDATE(), @MachineName, @IpAddress,
    @OperationType, @Permission, @ProfileName, @SyncKey, N'PendingWorker', N'Queued', @Reason,
    1, 1
);
SELECT SCOPE_IDENTITY();";
                AddCommonParameters(command, request, context);
                return Convert.ToInt64(command.ExecuteScalar());
            }
        }

        private static void InsertAudit(SqlConnection connection, SqlTransaction transaction, AdminOperationRequest request, HttpContextBase context, string result, string details)
        {
            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = @"
INSERT dbo.Sync_AdminAudit
(CreatedAt, UserName, MachineName, IpAddress, Operation, Permission, ProfileName, SyncKey, Result, Reason, Details)
VALUES
(GETDATE(), @UserName, @MachineName, @IpAddress, @OperationType, @Permission, @ProfileName, @SyncKey, @Result, @Reason, @Details);";
                AddCommonParameters(command, request, context);
                command.Parameters.Add("@Result", SqlDbType.NVarChar, 50).Value = result;
                command.Parameters.Add("@Details", SqlDbType.NVarChar).Value = details;
                command.ExecuteNonQuery();
            }
        }

        private static void InsertApproval(SqlConnection connection, SqlTransaction transaction, AdminOperationRequest request, HttpContextBase context)
        {
            using (var command = connection.CreateCommand())
            {
                command.Transaction = transaction;
                command.CommandText = @"
INSERT dbo.Sync_AdminApproval
(CreatedAt, RequestedBy, ApprovedBy, Operation, ProfileName, SyncKey, Status, Reason, ApprovedAt)
VALUES
(GETDATE(), @UserName, @UserName, @OperationType, @ProfileName, @SyncKey, N'ApprovedForQueue', @Reason, GETDATE());";
                AddCommonParameters(command, request, context);
                command.ExecuteNonQuery();
            }
        }

        private static void AddCommonParameters(SqlCommand command, AdminOperationRequest request, HttpContextBase context)
        {
            command.Parameters.Add("@UserName", SqlDbType.NVarChar, 256).Value = GetUserName(context);
            command.Parameters.Add("@MachineName", SqlDbType.NVarChar, 256).Value = Environment.MachineName;
            command.Parameters.Add("@IpAddress", SqlDbType.NVarChar, 64).Value = context != null && context.Request != null ? (object)context.Request.UserHostAddress : DBNull.Value;
            command.Parameters.Add("@OperationType", SqlDbType.NVarChar, 100).Value = request.OperationType ?? "PrepareApplySingle";
            command.Parameters.Add("@Permission", SqlDbType.NVarChar, 100).Value = SyncPermissions.ApplyMode;
            command.Parameters.Add("@ProfileName", SqlDbType.NVarChar, 100).Value = request.ProfileName ?? "POSOnly";
            command.Parameters.Add("@SyncKey", SqlDbType.NVarChar, 100).Value = request.SyncKey;
            command.Parameters.Add("@Reason", SqlDbType.NVarChar).Value = request.Reason;
        }

        private static string GetUserName(HttpContextBase context)
        {
            if (context != null && context.User != null && context.User.Identity != null && context.User.Identity.IsAuthenticated)
            {
                return context.User.Identity.Name;
            }

            return "local-sync-admin";
        }

        private static IList<NotificationRow> ReadNotifications(SqlCommand command)
        {
            var rows = new List<NotificationRow>();
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    rows.Add(new NotificationRow
                    {
                        NotificationId = GetInt64(reader, "NotificationId"),
                        CreatedAt = GetDate(reader, "CreatedAt"),
                        ReadAt = GetDate(reader, "ReadAt"),
                        NotificationType = GetString(reader, "NotificationType"),
                        Severity = GetString(reader, "Severity"),
                        Title = GetString(reader, "Title"),
                        Message = GetString(reader, "Message"),
                        SyncKey = GetString(reader, "SyncKey"),
                        BranchId = GetString(reader, "BranchId"),
                        Status = GetString(reader, "Status")
                    });
                }
            }

            return rows;
        }

        private static string GetString(SqlDataReader reader, string name)
        {
            var ordinal = reader.GetOrdinal(name);
            return reader.IsDBNull(ordinal) ? "" : Convert.ToString(reader.GetValue(ordinal));
        }

        private static bool GetBool(SqlDataReader reader, string name)
        {
            var ordinal = reader.GetOrdinal(name);
            return !reader.IsDBNull(ordinal) && Convert.ToBoolean(reader.GetValue(ordinal));
        }

        private static long GetInt64(SqlDataReader reader, string name)
        {
            var ordinal = reader.GetOrdinal(name);
            return reader.IsDBNull(ordinal) ? 0 : Convert.ToInt64(reader.GetValue(ordinal));
        }

        private static DateTime? GetDate(SqlDataReader reader, string name)
        {
            var ordinal = reader.GetOrdinal(name);
            return reader.IsDBNull(ordinal) ? (DateTime?)null : Convert.ToDateTime(reader.GetValue(ordinal));
        }
    }
}
