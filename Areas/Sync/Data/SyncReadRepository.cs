using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using MyERP.Areas.Sync.ViewModels;

namespace MyERP.Areas.Sync.Data
{
    public class SyncReadRepository
    {
        public DashboardViewModel GetDashboard()
        {
            using (var connection = SyncDb.Open())
            {
                return new DashboardViewModel
                {
                    TargetDatabase = SyncDb.TargetName,
                    PendingCount = CountByStatus(connection, "Pending"),
                    ConflictCount = CountByStatus(connection, "Conflict"),
                    FailedCount = CountByStatus(connection, "Failed"),
                    AppliedCount = CountByStatus(connection, "Applied"),
                    BlockedCount = CountByStatus(connection, "Blocked"),
                    RecentBatches = GetRecentBatches(connection),
                    ProfileUsage = GetProfileUsage(connection),
                    BranchActivity = GetBranchActivity(connection),
                    ConflictTrend = GetConflictTrend(connection),
                    RetryStats = GetRetryStats(connection),
                    ProblemBranches = GetProblemBranches(connection),
                    RecentDangerousOperations = GetAudit(connection, 6, true),
                    BranchHeartbeats = GetBranchHeartbeats(connection),
                    RecentBranchUploads = GetBranchUploads(connection)
                };
            }
        }

        public QueueViewModel GetQueue(QueueFilter filter)
        {
            filter = filter ?? new QueueFilter();
            using (var connection = SyncDb.Open())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
IF OBJECT_ID('dbo.Sync_Outbox', 'U') IS NULL
BEGIN
    SELECT TOP (0)
        CAST(0 AS BIGINT) AS SyncId,
        CAST(N'' AS NVARCHAR(100)) AS SyncKey,
        CAST(0 AS INT) AS BranchId,
        CAST(N'' AS NVARCHAR(50)) AS EntityType,
        CAST(N'' AS NVARCHAR(100)) AS EntityKey,
        CAST(N'' AS NVARCHAR(MAX)) AS ProfileName,
        CAST(N'' AS NVARCHAR(20)) AS Status,
        CAST(N'' AS NVARCHAR(64)) AS PayloadHash,
        CAST(0 AS INT) AS TryCount,
        CAST(NULL AS DATETIME) AS CreatedAt,
        CAST(NULL AS DATETIME) AS CompletedAt,
        CAST(N'' AS NVARCHAR(MAX)) AS LastError,
        CAST(N'' AS NVARCHAR(MAX)) AS ConflictReason;
END
ELSE
BEGIN
SELECT TOP (250)
    SyncId,
    EntityKey AS SyncKey,
    BranchId,
    EntityType,
    EntityKey,
    ISNULL(PayloadSummary, N'') AS ProfileName,
    Status,
    CONVERT(NVARCHAR(64), PayloadHash, 2) AS PayloadHash,
    TryCount,
    CreatedAt,
    CompletedAt,
    LastError,
    LastError AS ConflictReason
FROM dbo.Sync_Outbox
WHERE (@SyncKey IS NULL OR EntityKey LIKE '%' + @SyncKey + '%')
  AND (@BranchId IS NULL OR BranchId = @BranchIdValue)
  AND (@OldTransactionId IS NULL OR EntityKey = @OldTransactionId)
  AND (@Status IS NULL OR Status = @Status)
  AND (@ProfileName IS NULL OR PayloadSummary LIKE '%' + @ProfileName + '%')
  AND (@PayloadHash IS NULL OR CONVERT(NVARCHAR(64), PayloadHash, 2) LIKE '%' + @PayloadHash + '%')
  AND (@FromDate IS NULL OR CreatedAt >= @FromDate)
  AND (@ToDate IS NULL OR CreatedAt < DATEADD(DAY, 1, @ToDate))
ORDER BY CreatedAt DESC, SyncId DESC;
END";

                AddNullable(command, "@SyncKey", filter.SyncKey);
                AddNullable(command, "@OldTransactionId", filter.OldTransactionId);
                AddNullable(command, "@Status", filter.Status);
                AddNullable(command, "@ProfileName", filter.ProfileName);
                AddNullable(command, "@PayloadHash", filter.PayloadHash);
                AddNullable(command, "@FromDate", filter.FromDate);
                AddNullable(command, "@ToDate", filter.ToDate);
                AddNullable(command, "@BranchId", filter.BranchId);
                command.Parameters.Add("@BranchIdValue", SqlDbType.Int).Value = ParseInt(filter.BranchId);

                var rows = new List<SyncQueueRow>();
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        rows.Add(ReadQueueRow(reader));
                    }
                }

                return new QueueViewModel { Filter = filter, Rows = rows, TotalRows = rows.Count };
            }
        }

        public DiagnosticViewModel GetDiagnostics(string syncKey)
        {
            using (var connection = SyncDb.Open())
            {
                return new DiagnosticViewModel
                {
                    SyncKey = syncKey,
                    QueueRow = GetQueueRow(connection, syncKey),
                    ObjectMapRows = GetObjectMap(connection, syncKey),
                    Logs = GetLogs(connection, syncKey, 80, false),
                    Errors = GetLogs(connection, syncKey, 80, true),
                    Checks = GetReadinessChecks(connection, syncKey)
                };
            }
        }

        public IList<LogRow> GetLogs(string syncKey, bool errorsOnly)
        {
            using (var connection = SyncDb.Open())
            {
                return GetLogs(connection, syncKey, 200, errorsOnly);
            }
        }

        public IList<ProfileViewModel> GetProfiles()
        {
            using (var connection = SyncDb.Open())
            {
                var profiles = new Dictionary<string, ProfileViewModel>(StringComparer.OrdinalIgnoreCase);
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"
IF OBJECT_ID('dbo.Sync_Config', 'U') IS NOT NULL
BEGIN
    SELECT N'Sync_Config' AS ProfileName, ConfigKey, ConfigValue
    FROM dbo.Sync_Config
    ORDER BY ConfigKey;
END";

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var name = GetString(reader, "ProfileName", "POSOnly");
                            if (!profiles.ContainsKey(name))
                            {
                                profiles[name] = new ProfileViewModel
                                {
                                    ProfileName = name,
                                    Settings = new List<ProfileSettingRow>(),
                                    Warnings = new List<string>()
                                };
                            }

                            var value = GetString(reader, "ConfigValue", "");
                            profiles[name].Settings.Add(new ProfileSettingRow
                            {
                                SettingKey = GetString(reader, "ConfigKey", ""),
                                SettingValue = value,
                                IsEnabled = IsTruthy(value)
                            });
                        }
                    }
                }

                EnsureProfile(profiles, "POSOnly", "قراءة افتراضية: لا يتم نسخ القيود أو المخزون.");
                EnsureProfile(profiles, "FullAccountingCopy", "خطر مرتفع: النسخ المحاسبي لا يعمل من الواجهة.");
                EnsureProfile(profiles, "Retail", "ملف تجزئة للمراقبة فقط.");
                EnsureProfile(profiles, "Cards/Serials", "السيريالات والكروت معطلة من الواجهة.");
                return new List<ProfileViewModel>(profiles.Values);
            }
        }

        public PilotViewModel GetPilotReadiness()
        {
            using (var connection = SyncDb.Open())
            {
                return new PilotViewModel
                {
                    TargetDatabase = SyncDb.TargetName,
                    Checks = GetReadinessChecks(connection, null),
                    Approvals = GetApprovals(connection)
                };
            }
        }

        public IList<AdminAuditRow> GetAudit(int maxRows, bool dangerousOnly)
        {
            using (var connection = SyncDb.Open())
            {
                return GetAudit(connection, maxRows, dangerousOnly);
            }
        }

        private static int CountByStatus(SqlConnection connection, string status)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
IF OBJECT_ID('dbo.Sync_Outbox', 'U') IS NULL
    SELECT 0
ELSE
    SELECT COUNT(1) FROM dbo.Sync_Outbox WHERE Status = @Status;";
                command.Parameters.Add("@Status", SqlDbType.NVarChar, 50).Value = status;
                return Convert.ToInt32(command.ExecuteScalar());
            }
        }

        private static IList<SyncBatchRow> GetRecentBatches(SqlConnection connection)
        {
            var rows = new List<SyncBatchRow>();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
IF OBJECT_ID('dbo.Sync_Batch', 'U') IS NOT NULL
BEGIN
    SELECT TOP (12) BatchId, N'' AS ProfileName, Status, StartedAt, CompletedAt,
           TotalCount AS PendingCount, AppliedCount, FailedCount, 0 AS ConflictCount
    FROM dbo.Sync_Batch
    ORDER BY StartedAt DESC, BatchId DESC;
END";
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        rows.Add(new SyncBatchRow
                        {
                            BatchId = GetInt64(reader, "BatchId"),
                            ProfileName = GetString(reader, "ProfileName", ""),
                            Status = GetString(reader, "Status", ""),
                            StartedAt = GetDate(reader, "StartedAt"),
                            CompletedAt = GetDate(reader, "CompletedAt"),
                            PendingCount = GetInt(reader, "PendingCount"),
                            AppliedCount = GetInt(reader, "AppliedCount"),
                            FailedCount = GetInt(reader, "FailedCount"),
                            ConflictCount = GetInt(reader, "ConflictCount")
                        });
                    }
                }
            }

            return rows;
        }

        private static IList<ChartPoint> GetProfileUsage(SqlConnection connection)
        {
            return GetChart(connection, @"
IF OBJECT_ID('dbo.Sync_Outbox', 'U') IS NOT NULL
    SELECT TOP (8) ISNULL(NULLIF(CONVERT(NVARCHAR(200), PayloadSummary), N''), N'غير محدد') AS Label, COUNT(1) AS Value
    FROM dbo.Sync_Outbox GROUP BY ISNULL(NULLIF(CONVERT(NVARCHAR(200), PayloadSummary), N''), N'غير محدد') ORDER BY COUNT(1) DESC;");
        }

        private static IList<ChartPoint> GetBranchActivity(SqlConnection connection)
        {
            return GetChart(connection, @"
IF OBJECT_ID('dbo.Sync_Outbox', 'U') IS NOT NULL
    SELECT TOP (8) CONVERT(NVARCHAR(20), BranchId) AS Label, COUNT(1) AS Value
    FROM dbo.Sync_Outbox GROUP BY BranchId ORDER BY COUNT(1) DESC;");
        }

        private static IList<ChartPoint> GetConflictTrend(SqlConnection connection)
        {
            return GetChart(connection, @"
IF OBJECT_ID('dbo.Sync_Outbox', 'U') IS NOT NULL
    SELECT TOP (14) CONVERT(NVARCHAR(10), CAST(CreatedAt AS DATE), 120) AS Label, COUNT(1) AS Value
    FROM dbo.Sync_Outbox
    WHERE Status = 'Conflict'
    GROUP BY CAST(CreatedAt AS DATE)
    ORDER BY CAST(CreatedAt AS DATE) DESC;");
        }

        private static IList<ChartPoint> GetRetryStats(SqlConnection connection)
        {
            return GetChart(connection, @"
IF OBJECT_ID('dbo.Sync_Outbox', 'U') IS NOT NULL
    SELECT TOP (8) CONVERT(NVARCHAR(20), TryCount) AS Label, COUNT(1) AS Value
    FROM dbo.Sync_Outbox
    GROUP BY TryCount
    ORDER BY TryCount;");
        }

        private static IList<ChartPoint> GetProblemBranches(SqlConnection connection)
        {
            return GetChart(connection, @"
IF OBJECT_ID('dbo.Sync_Outbox', 'U') IS NOT NULL
    SELECT TOP (8) CONVERT(NVARCHAR(20), BranchId) AS Label, COUNT(1) AS Value
    FROM dbo.Sync_Outbox
    WHERE Status IN ('Conflict', 'Failed', 'Blocked')
    GROUP BY BranchId
    ORDER BY COUNT(1) DESC;");
        }

        private static IList<BranchHeartbeatRow> GetBranchHeartbeats(SqlConnection connection)
        {
            var rows = new List<BranchHeartbeatRow>();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
IF OBJECT_ID('dbo.Sync_BranchHeartbeat', 'U') IS NOT NULL
BEGIN
    SELECT TOP (20) BranchId, MachineName, LastSeenAt, AgentVersion, ConfigVersion, PayloadSchemaVersion,
           PendingOutboxCount, FailedOutboxCount, RejectedPayloadCount, AuthFailureCount, LastAuthFailureAt,
           LastTransactionId, LastPayloadSyncKey, LastError
    FROM dbo.Sync_BranchHeartbeat
    ORDER BY LastSeenAt DESC;
END";
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        rows.Add(new BranchHeartbeatRow
                        {
                            BranchId = GetInt(reader, "BranchId"),
                            MachineName = GetString(reader, "MachineName", ""),
                            LastSeenAt = GetDate(reader, "LastSeenAt"),
                            AgentVersion = GetString(reader, "AgentVersion", ""),
                            ConfigVersion = GetString(reader, "ConfigVersion", ""),
                            PayloadSchemaVersion = GetString(reader, "PayloadSchemaVersion", ""),
                            PendingOutboxCount = GetInt(reader, "PendingOutboxCount"),
                            FailedOutboxCount = GetInt(reader, "FailedOutboxCount"),
                            RejectedPayloadCount = GetInt(reader, "RejectedPayloadCount"),
                            AuthFailureCount = GetInt(reader, "AuthFailureCount"),
                            LastAuthFailureAt = GetDate(reader, "LastAuthFailureAt"),
                            LastTransactionId = GetInt64(reader, "LastTransactionId"),
                            LastPayloadSyncKey = GetString(reader, "LastPayloadSyncKey", ""),
                            LastError = GetString(reader, "LastError", "")
                        });
                    }
                }
            }

            return rows;
        }

        private static IList<BranchUploadRow> GetBranchUploads(SqlConnection connection)
        {
            var rows = new List<BranchUploadRow>();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
IF OBJECT_ID('dbo.Sync_BranchUpload', 'U') IS NOT NULL
BEGIN
    SELECT TOP (20) UploadId, CreatedAt, BranchId, SyncKey, CONVERT(NVARCHAR(64), PayloadHash, 2) AS PayloadHash,
           Status, Message
    FROM dbo.Sync_BranchUpload
    ORDER BY CreatedAt DESC, UploadId DESC;
END";
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        rows.Add(new BranchUploadRow
                        {
                            UploadId = GetInt64(reader, "UploadId"),
                            CreatedAt = GetDate(reader, "CreatedAt"),
                            BranchId = GetInt(reader, "BranchId"),
                            SyncKey = GetString(reader, "SyncKey", ""),
                            PayloadHash = GetString(reader, "PayloadHash", ""),
                            Status = GetString(reader, "Status", ""),
                            Message = GetString(reader, "Message", "")
                        });
                    }
                }
            }

            return rows;
        }

        private static IList<ChartPoint> GetChart(SqlConnection connection, string sql)
        {
            var rows = new List<ChartPoint>();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = sql;
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        rows.Add(new ChartPoint { Label = GetString(reader, "Label", ""), Value = GetDecimal(reader, "Value") });
                    }
                }
            }

            return rows;
        }

        private static SyncQueueRow GetQueueRow(SqlConnection connection, string syncKey)
        {
            if (String.IsNullOrWhiteSpace(syncKey))
            {
                return null;
            }

            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
IF OBJECT_ID('dbo.Sync_Outbox', 'U') IS NULL
BEGIN
    SELECT TOP (0)
        CAST(0 AS BIGINT) AS SyncId,
        CAST(N'' AS NVARCHAR(100)) AS SyncKey,
        CAST(0 AS INT) AS BranchId,
        CAST(N'' AS NVARCHAR(50)) AS EntityType,
        CAST(N'' AS NVARCHAR(100)) AS EntityKey,
        CAST(N'' AS NVARCHAR(MAX)) AS ProfileName,
        CAST(N'' AS NVARCHAR(20)) AS Status,
        CAST(N'' AS NVARCHAR(64)) AS PayloadHash,
        CAST(0 AS INT) AS TryCount,
        CAST(NULL AS DATETIME) AS CreatedAt,
        CAST(NULL AS DATETIME) AS CompletedAt,
        CAST(N'' AS NVARCHAR(MAX)) AS LastError,
        CAST(N'' AS NVARCHAR(MAX)) AS ConflictReason;
END
ELSE
BEGIN
SELECT TOP (1) SyncId, EntityKey AS SyncKey, BranchId, EntityType, EntityKey,
       ISNULL(PayloadSummary, N'') AS ProfileName, Status,
       CONVERT(NVARCHAR(64), PayloadHash, 2) AS PayloadHash, TryCount, CreatedAt, CompletedAt, LastError, LastError AS ConflictReason
FROM dbo.Sync_Outbox
WHERE EntityKey = @SyncKey
ORDER BY SyncId DESC;
END";
                command.Parameters.Add("@SyncKey", SqlDbType.NVarChar, 200).Value = syncKey;
                using (var reader = command.ExecuteReader())
                {
                    return reader.Read() ? ReadQueueRow(reader) : null;
                }
            }
        }

        private static IList<ObjectMapRow> GetObjectMap(SqlConnection connection, string syncKey)
        {
            var rows = new List<ObjectMapRow>();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
IF OBJECT_ID('dbo.Sync_ObjectMap', 'U') IS NOT NULL
BEGIN
    SELECT TOP (80) ObjectMapId, EntityType AS SourceObjectType, SourceKey AS SourceObjectKey, EntityType AS DestinationObjectType,
           DestinationKey AS DestinationObjectKey, SourceKey AS SyncKey, CONVERT(NVARCHAR(64), OriginalPayloadHash, 2) AS PayloadHash, CreatedAt
    FROM dbo.Sync_ObjectMap
    WHERE (@SyncKey IS NULL OR SourceKey = @SyncKey)
    ORDER BY CreatedAt DESC, ObjectMapId DESC;
END";
                AddNullable(command, "@SyncKey", syncKey);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        rows.Add(new ObjectMapRow
                        {
                            ObjectMapId = GetInt64(reader, "ObjectMapId"),
                            SourceObjectType = GetString(reader, "SourceObjectType", ""),
                            SourceObjectKey = GetString(reader, "SourceObjectKey", ""),
                            DestinationObjectType = GetString(reader, "DestinationObjectType", ""),
                            DestinationObjectKey = GetString(reader, "DestinationObjectKey", ""),
                            SyncKey = GetString(reader, "SyncKey", ""),
                            PayloadHash = GetString(reader, "PayloadHash", ""),
                            CreatedAt = GetDate(reader, "CreatedAt")
                        });
                    }
                }
            }

            return rows;
        }

        private static IList<LogRow> GetLogs(SqlConnection connection, string syncKey, int maxRows, bool errorsOnly)
        {
            var rows = new List<LogRow>();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
IF OBJECT_ID('dbo.Sync_Error', 'U') IS NOT NULL AND @ErrorsOnly = 1
BEGIN
    SELECT TOP (@MaxRows) ErrorId AS Id, CreatedAt, N'Error' AS Level, EntityKey AS SyncKey, EntityType AS Operation, ErrorMessage AS Message, LastSql AS Details
    FROM dbo.Sync_Error
    WHERE (@SyncKey IS NULL OR EntityKey = @SyncKey)
    ORDER BY CreatedAt DESC, ErrorId DESC;
END
ELSE IF OBJECT_ID('dbo.Sync_Log', 'U') IS NOT NULL
BEGIN
    SELECT TOP (@MaxRows) LogId AS Id, CreatedAt, Status AS Level, EntityKey AS SyncKey, EntityType AS Operation, Message, NULL AS Details
    FROM dbo.Sync_Log
    WHERE (@SyncKey IS NULL OR EntityKey = @SyncKey)
    ORDER BY CreatedAt DESC, LogId DESC;
END";
                command.Parameters.Add("@MaxRows", SqlDbType.Int).Value = maxRows;
                command.Parameters.Add("@ErrorsOnly", SqlDbType.Bit).Value = errorsOnly;
                AddNullable(command, "@SyncKey", syncKey);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        rows.Add(new LogRow
                        {
                            Id = GetInt64(reader, "Id"),
                            CreatedAt = GetDate(reader, "CreatedAt"),
                            Level = GetString(reader, "Level", ""),
                            SyncKey = GetString(reader, "SyncKey", ""),
                            Operation = GetString(reader, "Operation", ""),
                            Message = GetString(reader, "Message", ""),
                            Details = GetString(reader, "Details", "")
                        });
                    }
                }
            }

            return rows;
        }

        private static IList<ReadinessCheckRow> GetReadinessChecks(SqlConnection connection, string syncKey)
        {
            var rows = new List<ReadinessCheckRow>();
            rows.Add(new ReadinessCheckRow { CheckName = "Apply execution", Status = "Blocked", Message = "التنفيذ من الواجهة غير مفعّل.", IsHardBlocker = false });
            rows.Add(new ReadinessCheckRow { CheckName = "Batch apply", Status = "Blocked", Message = "التنفيذ الجماعي محجوب.", IsHardBlocker = false });
            rows.Add(new ReadinessCheckRow { CheckName = "ApplySingleSyncKey", Status = "Required", Message = "يجب اختيار SyncKey واحد فقط.", IsHardBlocker = false });

            if (!String.IsNullOrWhiteSpace(syncKey))
            {
                var row = GetQueueRow(connection, syncKey);
                rows.Add(new ReadinessCheckRow
                {
                    CheckName = "Queue item",
                    Status = row == null ? "HardBlocker" : row.Status,
                    Message = row == null ? "لم يتم العثور على العنصر في قائمة الانتظار." : "العنصر موجود للمعاينة فقط.",
                    IsHardBlocker = row == null
                });
            }

            return rows;
        }

        private static IList<AdminApprovalRow> GetApprovals(SqlConnection connection)
        {
            var rows = new List<AdminApprovalRow>();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
IF OBJECT_ID('dbo.Sync_AdminApproval', 'U') IS NOT NULL
BEGIN
    SELECT TOP (50) AdminApprovalId AS Id, CreatedAt, RequestedBy, ApprovedBy,
           Operation, SyncKey, Status, Reason
    FROM dbo.Sync_AdminApproval
    ORDER BY CreatedAt DESC, AdminApprovalId DESC;
END";
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        rows.Add(new AdminApprovalRow
                        {
                            Id = GetInt64(reader, "Id"),
                            CreatedAt = GetDate(reader, "CreatedAt"),
                            RequestedBy = GetString(reader, "RequestedBy", ""),
                            ApprovedBy = GetString(reader, "ApprovedBy", ""),
                            Operation = GetString(reader, "Operation", ""),
                            SyncKey = GetString(reader, "SyncKey", ""),
                            Status = GetString(reader, "Status", ""),
                            Reason = GetString(reader, "Reason", "")
                        });
                    }
                }
            }

            return rows;
        }

        private static IList<AdminAuditRow> GetAudit(SqlConnection connection, int maxRows, bool dangerousOnly)
        {
            var rows = new List<AdminAuditRow>();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
IF OBJECT_ID('dbo.Sync_AdminAudit', 'U') IS NOT NULL
BEGIN
    SELECT TOP (@MaxRows) AdminAuditId AS Id, CreatedAt, UserName, MachineName, IpAddress,
           Operation, Permission, ProfileName, SyncKey, Result, Reason
    FROM dbo.Sync_AdminAudit
    WHERE (@DangerousOnly = 0 OR Operation IN ('PrepareApply', 'PrepareApplySingle', 'ApplyRequest', 'RollbackRequest', 'ProfileChange', 'WorkerPoll'))
    ORDER BY CreatedAt DESC, AdminAuditId DESC;
END";
                command.Parameters.Add("@MaxRows", SqlDbType.Int).Value = maxRows;
                command.Parameters.Add("@DangerousOnly", SqlDbType.Bit).Value = dangerousOnly;
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        rows.Add(new AdminAuditRow
                        {
                            Id = GetInt64(reader, "Id"),
                            CreatedAt = GetDate(reader, "CreatedAt"),
                            UserName = GetString(reader, "UserName", ""),
                            MachineName = GetString(reader, "MachineName", ""),
                            IpAddress = GetString(reader, "IpAddress", ""),
                            Operation = GetString(reader, "Operation", ""),
                            Permission = GetString(reader, "Permission", ""),
                            ProfileName = GetString(reader, "ProfileName", ""),
                            SyncKey = GetString(reader, "SyncKey", ""),
                            Result = GetString(reader, "Result", ""),
                            Reason = GetString(reader, "Reason", "")
                        });
                    }
                }
            }

            return rows;
        }

        private static SyncQueueRow ReadQueueRow(SqlDataReader reader)
        {
            var entityKey = GetString(reader, "EntityKey", "");
            return new SyncQueueRow
            {
                SyncId = GetInt64(reader, "SyncId"),
                SyncKey = GetString(reader, "SyncKey", ""),
                BranchId = GetInt(reader, "BranchId"),
                EntityType = GetString(reader, "EntityType", ""),
                EntityKey = entityKey,
                OldTransactionId = entityKey,
                ProfileName = GetString(reader, "ProfileName", ""),
                Status = GetString(reader, "Status", ""),
                PayloadHash = GetString(reader, "PayloadHash", ""),
                TryCount = GetInt(reader, "TryCount"),
                CreatedAt = GetDate(reader, "CreatedAt"),
                CompletedAt = GetDate(reader, "CompletedAt"),
                LastError = GetString(reader, "LastError", ""),
                ConflictReason = GetString(reader, "ConflictReason", "")
            };
        }

        private static void EnsureProfile(IDictionary<string, ProfileViewModel> profiles, string name, string warning)
        {
            if (!profiles.ContainsKey(name))
            {
                profiles[name] = new ProfileViewModel { ProfileName = name, Settings = new List<ProfileSettingRow>(), Warnings = new List<string>() };
            }

            profiles[name].Warnings.Add(warning);
        }

        private static bool IsTruthy(string value)
        {
            return String.Equals(value, "true", StringComparison.OrdinalIgnoreCase) || value == "1" || String.Equals(value, "yes", StringComparison.OrdinalIgnoreCase);
        }

        private static void AddNullable(SqlCommand command, string name, object value)
        {
            command.Parameters.AddWithValue(name, value == null || String.IsNullOrWhiteSpace(Convert.ToString(value)) ? (object)DBNull.Value : value);
        }

        private static int ParseInt(string value)
        {
            int parsed;
            return Int32.TryParse(value, out parsed) ? parsed : 0;
        }

        private static string GetString(SqlDataReader reader, string name, string fallback)
        {
            var ordinal = reader.GetOrdinal(name);
            return reader.IsDBNull(ordinal) ? fallback : Convert.ToString(reader.GetValue(ordinal));
        }

        private static int GetInt(SqlDataReader reader, string name)
        {
            var ordinal = reader.GetOrdinal(name);
            return reader.IsDBNull(ordinal) ? 0 : Convert.ToInt32(reader.GetValue(ordinal));
        }

        private static long GetInt64(SqlDataReader reader, string name)
        {
            var ordinal = reader.GetOrdinal(name);
            return reader.IsDBNull(ordinal) ? 0 : Convert.ToInt64(reader.GetValue(ordinal));
        }

        private static decimal GetDecimal(SqlDataReader reader, string name)
        {
            var ordinal = reader.GetOrdinal(name);
            return reader.IsDBNull(ordinal) ? 0 : Convert.ToDecimal(reader.GetValue(ordinal));
        }

        private static DateTime? GetDate(SqlDataReader reader, string name)
        {
            var ordinal = reader.GetOrdinal(name);
            return reader.IsDBNull(ordinal) ? (DateTime?)null : Convert.ToDateTime(reader.GetValue(ordinal));
        }
    }
}
