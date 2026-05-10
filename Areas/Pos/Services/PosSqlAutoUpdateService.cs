using MyERP.Areas.Pos.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace MyERP.Areas.Pos.Services
{
    public class PosSqlAutoUpdateService
    {
        private const string DefaultConnectionName = "KishnyCashConnection";
        private const string ManifestVirtualPath = "~/Areas/Pos/Sql/POS_SQL_AutoUpdate_Manifest.json";
        private readonly string _connectionString;
        private readonly string _manifestPath;

        public PosSqlAutoUpdateService()
            : this(null, null)
        {
        }

        public PosSqlAutoUpdateService(string connectionString, string manifestPath)
        {
            _connectionString = string.IsNullOrWhiteSpace(connectionString) ? ResolveConnectionString() : connectionString;
            _manifestPath = string.IsNullOrWhiteSpace(manifestPath) ? MapPath(ManifestVirtualPath) : manifestPath;
        }

        public PosSqlUpdateStatusResult GetStatus()
        {
            var manifest = LoadManifest();
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var result = BuildStatus(connection, manifest, false);
                result.RecentRuns = GetRecentRuns(connection);
                return result;
            }
        }

        public PosSqlUpdateRunResult DryRun()
        {
            var status = GetStatus();
            return new PosSqlUpdateRunResult
            {
                Success = status.HashMismatchCount == 0 && status.IsPosDatabase,
                IsDryRun = true,
                AppliedCount = 0,
                SkippedCount = status.AppliedCount,
                FailedCount = status.FailedCount,
                PendingCount = status.PendingCount,
                HashMismatchCount = status.HashMismatchCount,
                Message = BuildDryRunMessage(status),
                Scripts = status.Scripts
            };
        }

        public PosSqlUpdateRunResult ApplyPending(PosSqlUpdateRunRequest request)
        {
            if (request == null || !request.ConfirmBackup)
            {
                return Fail("يجب تأكيد أخذ نسخة احتياطية كاملة من قاعدة البيانات قبل تطبيق التحديثات.");
            }

            var manifest = LoadManifest();
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                EnsureHistoryTables(connection);

                var status = BuildStatus(connection, manifest, true);
                if (!status.IsPosDatabase)
                {
                    return Fail("تم إيقاف التنفيذ لأن قاعدة البيانات الحالية لا تطابق قاعدة POS المطلوبة.");
                }

                if (status.HashMismatchCount > 0)
                {
                    return Fail("تم إيقاف التنفيذ بسبب اختلاف Hash في سكريبت تم تطبيقه سابقاً.");
                }

                var pending = status.Scripts.Where(s => string.Equals(s.Status, "Pending", StringComparison.OrdinalIgnoreCase)).ToList();
                var runId = CreateRun(connection, request, "Apply", "Started", pending.Count, status.AppliedCount, status.HashMismatchCount);
                var applied = 0;
                var failed = 0;

                foreach (var script in pending)
                {
                    var stopwatch = Stopwatch.StartNew();
                    try
                    {
                        ExecuteScript(connection, script);
                        stopwatch.Stop();
                        InsertHistory(connection, runId, script, true, null, stopwatch.ElapsedMilliseconds, request);
                        applied++;
                    }
                    catch (Exception ex)
                    {
                        stopwatch.Stop();
                        failed++;
                        InsertHistory(connection, runId, script, false, SafeError(ex), stopwatch.ElapsedMilliseconds, request);
                        UpdateRun(connection, runId, "Failed", applied, failed);

                        var failedResult = new PosSqlUpdateRunResult
                        {
                            Success = false,
                            RunId = runId,
                            AppliedCount = applied,
                            SkippedCount = status.AppliedCount,
                            FailedCount = failed,
                            PendingCount = pending.Count,
                            HashMismatchCount = status.HashMismatchCount,
                            Message = "فشل تنفيذ السكريبت " + script.ScriptName + ". تم إيقاف التنفيذ وتسجيل الخطأ."
                        };
                        failedResult.Scripts = BuildStatus(connection, manifest, true).Scripts;
                        return failedResult;
                    }
                }

                UpdateRun(connection, runId, "Completed", applied, failed);
                var finalStatus = BuildStatus(connection, manifest, true);
                return new PosSqlUpdateRunResult
                {
                    Success = true,
                    RunId = runId,
                    AppliedCount = applied,
                    SkippedCount = status.AppliedCount,
                    FailedCount = failed,
                    PendingCount = pending.Count,
                    HashMismatchCount = 0,
                    Message = applied == 0 ? "لا توجد تحديثات جديدة للتطبيق." : "تم تطبيق التحديثات المنتظرة بنجاح.",
                    Scripts = finalStatus.Scripts
                };
            }
        }

        public string BuildRunLog(int runId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                if (!TableExists(connection, "dbo.POS_SqlUpdateRun"))
                {
                    return "لا يوجد سجل تشغيل لتحديثات قاعدة البيانات.";
                }

                var builder = new StringBuilder();
                using (var command = new SqlCommand(@"
SELECT RunId, StartedAt, FinishedAt, Mode, Status, StartedBy, UserName, ClientIP, DatabaseName, ServerName,
       TotalScripts, AppliedCount, SkippedCount, FailedCount, WarningCount
FROM dbo.POS_SqlUpdateRun
WHERE RunId = @RunId;", connection))
                {
                    command.Parameters.Add("@RunId", SqlDbType.Int).Value = runId;
                    using (var reader = command.ExecuteReader())
                    {
                        if (!reader.Read())
                        {
                            return "لم يتم العثور على سجل التشغيل المطلوب.";
                        }

                        builder.AppendLine("POS SQL Update Run");
                        builder.AppendLine("RunId: " + runId.ToString(CultureInfo.InvariantCulture));
                        builder.AppendLine("StartedAt: " + ReadDateTime(reader, "StartedAt"));
                        builder.AppendLine("FinishedAt: " + ReadDateTime(reader, "FinishedAt"));
                        builder.AppendLine("Mode: " + ReadString(reader, "Mode"));
                        builder.AppendLine("Status: " + ReadString(reader, "Status"));
                        builder.AppendLine("StartedBy: " + ReadString(reader, "StartedBy"));
                        builder.AppendLine("UserName: " + ReadString(reader, "UserName"));
                        builder.AppendLine("ClientIP: " + ReadString(reader, "ClientIP"));
                        builder.AppendLine("DatabaseName: " + ReadString(reader, "DatabaseName"));
                        builder.AppendLine("ServerName: " + ReadString(reader, "ServerName"));
                        builder.AppendLine("TotalScripts: " + ReadInt(reader, "TotalScripts"));
                        builder.AppendLine("AppliedCount: " + ReadInt(reader, "AppliedCount"));
                        builder.AppendLine("SkippedCount: " + ReadInt(reader, "SkippedCount"));
                        builder.AppendLine("FailedCount: " + ReadInt(reader, "FailedCount"));
                        builder.AppendLine("WarningCount: " + ReadInt(reader, "WarningCount"));
                        builder.AppendLine();
                    }
                }

                if (TableExists(connection, "dbo.POS_SqlUpdateHistory"))
                {
                    using (var command = new SqlCommand(@"
SELECT ScriptOrder, ScriptName, ScriptHash, AppliedOn, AppliedBy, MachineName, DatabaseName, DurationMs, Success, ErrorMessage
FROM dbo.POS_SqlUpdateHistory
WHERE RunId = @RunId
ORDER BY HistoryId;", connection))
                    {
                        command.Parameters.Add("@RunId", SqlDbType.Int).Value = runId;
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                builder.AppendLine(ReadDecimal(reader, "ScriptOrder") + " | " + ReadString(reader, "ScriptName") + " | " + (ReadBool(reader, "Success") ? "Success" : "Failed"));
                                builder.AppendLine("Hash: " + ReadString(reader, "ScriptHash"));
                                builder.AppendLine("AppliedOn: " + ReadDateTime(reader, "AppliedOn"));
                                builder.AppendLine("AppliedBy: " + ReadString(reader, "AppliedBy"));
                                builder.AppendLine("DurationMs: " + ReadInt(reader, "DurationMs"));
                                var error = ReadString(reader, "ErrorMessage");
                                if (!string.IsNullOrWhiteSpace(error))
                                {
                                    builder.AppendLine("Error: " + error);
                                }
                                builder.AppendLine();
                            }
                        }
                    }
                }

                return builder.ToString();
            }
        }

        private PosSqlUpdateStatusResult BuildStatus(SqlConnection connection, Manifest manifest, bool ensureHistoryShape)
        {
            if (ensureHistoryShape)
            {
                EnsureHistoryTables(connection);
            }

            var history = GetHistory(connection);
            var result = new PosSqlUpdateStatusResult
            {
                ModuleName = manifest.Module,
                DatabaseName = connection.Database,
                ServerName = Convert.ToString(ExecuteScalar(connection, "SELECT CONVERT(NVARCHAR(128), @@SERVERNAME)"), CultureInfo.InvariantCulture),
                IsPosDatabase = TestPosDatabase(connection, manifest)
            };

            var scriptRoot = ResolveScriptRoot(manifest);
            foreach (var entry in manifest.Scripts.Where(s => s.AutoApply).OrderBy(s => s.Order).ThenBy(s => s.File, StringComparer.OrdinalIgnoreCase))
            {
                if (entry.File.StartsWith("MANUAL_", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var absolutePath = SafeScriptPath(scriptRoot, entry.File);
                var hash = File.Exists(absolutePath) ? GetSha256(absolutePath) : string.Empty;
                var scriptHistory = history.Where(h => string.Equals(h.ScriptName, entry.File, StringComparison.OrdinalIgnoreCase)).ToList();
                var successful = scriptHistory.Where(h => h.Success).OrderByDescending(h => h.AppliedOn).ToList();
                var sameHash = successful.FirstOrDefault(h => string.Equals(h.ScriptHash, hash, StringComparison.OrdinalIgnoreCase));
                var failed = scriptHistory.Where(h => !h.Success).OrderByDescending(h => h.AppliedOn).FirstOrDefault();
                var hashMismatch = successful.Count > 0 && sameHash == null;
                var status = hashMismatch ? "HashMismatch" : (sameHash != null ? "Applied" : "Pending");

                result.Scripts.Add(new PosSqlUpdateScriptViewModel
                {
                    Order = entry.Order,
                    ScriptName = entry.File,
                    Purpose = entry.Purpose,
                    RelativePath = "Areas/Pos/Sql/" + entry.File,
                    Hash = hash,
                    Status = status,
                    StatusArabic = ToArabicStatus(status),
                    StatusCssClass = ToCssStatus(status),
                    AppliedOn = sameHash != null ? sameHash.AppliedOn : (DateTime?)null,
                    AppliedBy = sameHash != null ? sameHash.AppliedBy : string.Empty,
                    LastErrorSummary = failed != null ? failed.ErrorMessage : string.Empty
                });
            }

            foreach (var manual in manifest.ManualScripts.OrderBy(s => s.File, StringComparer.OrdinalIgnoreCase))
            {
                var fileName = manual.File;
                var resolved = SafeScriptPath(scriptRoot, fileName, true);
                var hash = File.Exists(resolved) ? GetSha256(resolved) : string.Empty;
                result.Scripts.Add(new PosSqlUpdateScriptViewModel
                {
                    Order = 0,
                    ScriptName = fileName,
                    Purpose = manual.Reason,
                    RelativePath = "Areas/Pos/Sql/" + fileName,
                    Hash = hash,
                    Status = "Manual",
                    StatusArabic = ToArabicStatus("Manual"),
                    StatusCssClass = ToCssStatus("Manual"),
                    IsManual = true
                });
            }

            result.AppliedCount = result.Scripts.Count(s => string.Equals(s.Status, "Applied", StringComparison.OrdinalIgnoreCase));
            result.PendingCount = result.Scripts.Count(s => string.Equals(s.Status, "Pending", StringComparison.OrdinalIgnoreCase));
            result.HashMismatchCount = result.Scripts.Count(s => string.Equals(s.Status, "HashMismatch", StringComparison.OrdinalIgnoreCase));
            result.ManualCount = result.Scripts.Count(s => s.IsManual);
            result.FailedCount = history.Count(h => !h.Success);
            var appliedDates = history.Where(h => h.Success).Select(h => h.AppliedOn).ToList();
            result.LastAppliedOn = appliedDates.Count == 0 ? (DateTime?)null : appliedDates.Max();
            result.LastRunId = GetLastRunId(connection);

            if (!result.IsPosDatabase)
            {
                result.StatusText = "قاعدة غير مخصصة لـ POS";
                result.StatusCssClass = "blocked";
            }
            else if (result.HashMismatchCount > 0)
            {
                result.StatusText = "يوجد اختلاف Hash";
                result.StatusCssClass = "danger";
            }
            else if (result.PendingCount > 0)
            {
                result.StatusText = "تحديثات منتظرة";
                result.StatusCssClass = "pending";
            }
            else
            {
                result.StatusText = "محدثة";
                result.StatusCssClass = "ok";
            }

            return result;
        }

        private void ExecuteScript(SqlConnection connection, PosSqlUpdateScriptViewModel script)
        {
            var absolutePath = MapPath("~/" + script.RelativePath);
            var sql = File.ReadAllText(absolutePath, Encoding.UTF8);
            var batches = SplitSqlBatches(sql).ToList();
            using (var transaction = connection.BeginTransaction())
            {
                try
                {
                    foreach (var batch in batches)
                    {
                        using (var command = new SqlCommand(batch, connection, transaction))
                        {
                            command.CommandTimeout = 0;
                            command.ExecuteNonQuery();
                        }
                    }

                    transaction.Commit();
                }
                catch
                {
                    try
                    {
                        transaction.Rollback();
                    }
                    catch
                    {
                    }

                    throw;
                }
            }
        }

        private static IEnumerable<string> SplitSqlBatches(string sql)
        {
            if (string.IsNullOrWhiteSpace(sql))
            {
                yield break;
            }

            var builder = new StringBuilder();
            using (var reader = new StringReader(sql))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (Regex.IsMatch(line, @"^\s*GO\s*(--.*)?$", RegexOptions.IgnoreCase))
                    {
                        var batch = builder.ToString();
                        if (!string.IsNullOrWhiteSpace(batch))
                        {
                            yield return batch;
                        }

                        builder.Length = 0;
                    }
                    else
                    {
                        builder.AppendLine(line);
                    }
                }
            }

            var tail = builder.ToString();
            if (!string.IsNullOrWhiteSpace(tail))
            {
                yield return tail;
            }
        }

        private void EnsureHistoryTables(SqlConnection connection)
        {
            ExecuteNonQuery(connection, @"
IF OBJECT_ID(N'dbo.POS_SqlUpdateRun', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.POS_SqlUpdateRun
    (
        RunId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_POS_SqlUpdateRun PRIMARY KEY,
        StartedAt DATETIME NOT NULL CONSTRAINT DF_POS_SqlUpdateRun_StartedAt DEFAULT (GETDATE()),
        FinishedAt DATETIME NULL,
        Mode NVARCHAR(20) NOT NULL,
        Status NVARCHAR(30) NOT NULL,
        StartedBy NVARCHAR(256) NOT NULL,
        UserID INT NULL,
        UserName NVARCHAR(256) NULL,
        ClientIP NVARCHAR(64) NULL,
        MachineName NVARCHAR(128) NOT NULL,
        DatabaseName SYSNAME NOT NULL,
        ServerName NVARCHAR(128) NOT NULL,
        ReleaseNo NVARCHAR(100) NULL,
        TotalScripts INT NOT NULL CONSTRAINT DF_POS_SqlUpdateRun_TotalScripts DEFAULT (0),
        AppliedCount INT NOT NULL CONSTRAINT DF_POS_SqlUpdateRun_AppliedCount DEFAULT (0),
        SkippedCount INT NOT NULL CONSTRAINT DF_POS_SqlUpdateRun_SkippedCount DEFAULT (0),
        FailedCount INT NOT NULL CONSTRAINT DF_POS_SqlUpdateRun_FailedCount DEFAULT (0),
        WarningCount INT NOT NULL CONSTRAINT DF_POS_SqlUpdateRun_WarningCount DEFAULT (0)
    );
END;
IF OBJECT_ID(N'dbo.POS_SqlUpdateHistory', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.POS_SqlUpdateHistory
    (
        HistoryId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_POS_SqlUpdateHistory PRIMARY KEY,
        RunId INT NULL,
        ScriptOrder DECIMAL(10,2) NOT NULL,
        ScriptName NVARCHAR(260) NOT NULL,
        ScriptPath NVARCHAR(1000) NOT NULL,
        ScriptHash CHAR(64) NOT NULL,
        AppliedOn DATETIME NOT NULL CONSTRAINT DF_POS_SqlUpdateHistory_AppliedOn DEFAULT (GETDATE()),
        AppliedBy NVARCHAR(256) NOT NULL,
        MachineName NVARCHAR(128) NOT NULL,
        DatabaseName SYSNAME NOT NULL,
        DurationMs INT NULL,
        Success BIT NOT NULL,
        ErrorMessage NVARCHAR(MAX) NULL,
        ReleaseNo NVARCHAR(100) NULL
    );
END;
IF COL_LENGTH('dbo.POS_SqlUpdateRun', 'UserID') IS NULL ALTER TABLE dbo.POS_SqlUpdateRun ADD UserID INT NULL;
IF COL_LENGTH('dbo.POS_SqlUpdateRun', 'UserName') IS NULL ALTER TABLE dbo.POS_SqlUpdateRun ADD UserName NVARCHAR(256) NULL;
IF COL_LENGTH('dbo.POS_SqlUpdateRun', 'ClientIP') IS NULL ALTER TABLE dbo.POS_SqlUpdateRun ADD ClientIP NVARCHAR(64) NULL;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_POS_SqlUpdateHistory_ScriptName_Hash_Success' AND object_id = OBJECT_ID(N'dbo.POS_SqlUpdateHistory', N'U'))
BEGIN
    CREATE UNIQUE INDEX UX_POS_SqlUpdateHistory_ScriptName_Hash_Success
        ON dbo.POS_SqlUpdateHistory (ScriptName, ScriptHash, Success)
        WHERE Success = 1;
END;");
        }

        private int CreateRun(SqlConnection connection, PosSqlUpdateRunRequest request, string mode, string status, int totalScripts, int skippedCount, int warningCount)
        {
            using (var command = new SqlCommand(@"
INSERT dbo.POS_SqlUpdateRun
(
    Mode, Status, StartedBy, UserID, UserName, ClientIP, MachineName, DatabaseName, ServerName,
    ReleaseNo, TotalScripts, SkippedCount, WarningCount
)
VALUES
(
    @Mode, @Status, SUSER_SNAME(), @UserID, @UserName, @ClientIP, HOST_NAME(), DB_NAME(), CONVERT(NVARCHAR(128), @@SERVERNAME),
    @ReleaseNo, @TotalScripts, @SkippedCount, @WarningCount
);
SELECT CONVERT(INT, SCOPE_IDENTITY());", connection))
            {
                command.Parameters.Add("@Mode", SqlDbType.NVarChar, 20).Value = mode;
                command.Parameters.Add("@Status", SqlDbType.NVarChar, 30).Value = status;
                command.Parameters.Add("@UserID", SqlDbType.Int).Value = request.UserId <= 0 ? (object)DBNull.Value : request.UserId;
                command.Parameters.Add("@UserName", SqlDbType.NVarChar, 256).Value = NullIfEmpty(request.UserName);
                command.Parameters.Add("@ClientIP", SqlDbType.NVarChar, 64).Value = NullIfEmpty(request.ClientIp);
                command.Parameters.Add("@ReleaseNo", SqlDbType.NVarChar, 100).Value = NullIfEmpty(request.ReleaseNo);
                command.Parameters.Add("@TotalScripts", SqlDbType.Int).Value = totalScripts;
                command.Parameters.Add("@SkippedCount", SqlDbType.Int).Value = skippedCount;
                command.Parameters.Add("@WarningCount", SqlDbType.Int).Value = warningCount;
                return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture);
            }
        }

        private static void UpdateRun(SqlConnection connection, int runId, string status, int applied, int failed)
        {
            using (var command = new SqlCommand(@"
UPDATE dbo.POS_SqlUpdateRun
SET FinishedAt = GETDATE(), Status = @Status, AppliedCount = @AppliedCount, FailedCount = @FailedCount
WHERE RunId = @RunId;", connection))
            {
                command.Parameters.Add("@Status", SqlDbType.NVarChar, 30).Value = status;
                command.Parameters.Add("@AppliedCount", SqlDbType.Int).Value = applied;
                command.Parameters.Add("@FailedCount", SqlDbType.Int).Value = failed;
                command.Parameters.Add("@RunId", SqlDbType.Int).Value = runId;
                command.ExecuteNonQuery();
            }
        }

        private static void InsertHistory(SqlConnection connection, int runId, PosSqlUpdateScriptViewModel script, bool success, string error, long durationMs, PosSqlUpdateRunRequest request)
        {
            using (var command = new SqlCommand(@"
INSERT dbo.POS_SqlUpdateHistory
(
    RunId, ScriptOrder, ScriptName, ScriptPath, ScriptHash, AppliedBy, MachineName,
    DatabaseName, DurationMs, Success, ErrorMessage, ReleaseNo
)
VALUES
(
    @RunId, @ScriptOrder, @ScriptName, @ScriptPath, @ScriptHash, @AppliedBy, HOST_NAME(),
    DB_NAME(), @DurationMs, @Success, @ErrorMessage, @ReleaseNo
);", connection))
            {
                command.Parameters.Add("@RunId", SqlDbType.Int).Value = runId;
                command.Parameters.Add("@ScriptOrder", SqlDbType.Decimal).Value = script.Order;
                command.Parameters["@ScriptOrder"].Precision = 10;
                command.Parameters["@ScriptOrder"].Scale = 2;
                command.Parameters.Add("@ScriptName", SqlDbType.NVarChar, 260).Value = script.ScriptName;
                command.Parameters.Add("@ScriptPath", SqlDbType.NVarChar, 1000).Value = script.RelativePath;
                command.Parameters.Add("@ScriptHash", SqlDbType.Char, 64).Value = script.Hash;
                command.Parameters.Add("@AppliedBy", SqlDbType.NVarChar, 256).Value = string.IsNullOrWhiteSpace(request.UserName) ? "POS Web Admin" : request.UserName;
                command.Parameters.Add("@DurationMs", SqlDbType.Int).Value = durationMs > int.MaxValue ? int.MaxValue : (int)durationMs;
                command.Parameters.Add("@Success", SqlDbType.Bit).Value = success;
                command.Parameters.Add("@ErrorMessage", SqlDbType.NVarChar).Value = string.IsNullOrWhiteSpace(error) ? (object)DBNull.Value : error;
                command.Parameters.Add("@ReleaseNo", SqlDbType.NVarChar, 100).Value = NullIfEmpty(request.ReleaseNo);
                command.ExecuteNonQuery();
            }
        }

        private List<HistoryRow> GetHistory(SqlConnection connection)
        {
            var rows = new List<HistoryRow>();
            if (!TableExists(connection, "dbo.POS_SqlUpdateHistory"))
            {
                return rows;
            }

            using (var command = new SqlCommand(@"
SELECT ScriptName, ScriptHash, Success, AppliedOn, AppliedBy, ErrorMessage
FROM dbo.POS_SqlUpdateHistory;", connection))
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    rows.Add(new HistoryRow
                    {
                        ScriptName = ReadString(reader, "ScriptName"),
                        ScriptHash = ReadString(reader, "ScriptHash"),
                        Success = ReadBool(reader, "Success"),
                        AppliedOn = ReadDateTimeValue(reader, "AppliedOn"),
                        AppliedBy = ReadString(reader, "AppliedBy"),
                        ErrorMessage = ReadString(reader, "ErrorMessage")
                    });
                }
            }

            return rows;
        }

        private IList<PosSqlUpdateRunSummary> GetRecentRuns(SqlConnection connection)
        {
            var rows = new List<PosSqlUpdateRunSummary>();
            if (!TableExists(connection, "dbo.POS_SqlUpdateRun"))
            {
                return rows;
            }

            var hasUserName = ColumnExists(connection, "dbo.POS_SqlUpdateRun", "UserName");
            var hasClientIp = ColumnExists(connection, "dbo.POS_SqlUpdateRun", "ClientIP");
            var sql = @"
SELECT TOP (10) RunId, StartedAt, FinishedAt, Mode, Status, StartedBy,
       " + (hasUserName ? "UserName" : "CAST(NULL AS NVARCHAR(256))") + @" AS UserName,
       " + (hasClientIp ? "ClientIP" : "CAST(NULL AS NVARCHAR(64))") + @" AS ClientIP,
       DatabaseName, ServerName, TotalScripts, AppliedCount, SkippedCount, FailedCount, WarningCount
FROM dbo.POS_SqlUpdateRun
ORDER BY RunId DESC;";

            using (var command = new SqlCommand(sql, connection))
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    rows.Add(new PosSqlUpdateRunSummary
                    {
                        RunId = ReadInt(reader, "RunId").GetValueOrDefault(),
                        StartedAt = ReadDateTimeValue(reader, "StartedAt"),
                        FinishedAt = ReadNullableDateTime(reader, "FinishedAt"),
                        Mode = ReadString(reader, "Mode"),
                        Status = ReadString(reader, "Status"),
                        StartedBy = ReadString(reader, "StartedBy"),
                        UserName = ReadString(reader, "UserName"),
                        ClientIp = ReadString(reader, "ClientIP"),
                        DatabaseName = ReadString(reader, "DatabaseName"),
                        ServerName = ReadString(reader, "ServerName"),
                        TotalScripts = ReadInt(reader, "TotalScripts").GetValueOrDefault(),
                        AppliedCount = ReadInt(reader, "AppliedCount").GetValueOrDefault(),
                        SkippedCount = ReadInt(reader, "SkippedCount").GetValueOrDefault(),
                        FailedCount = ReadInt(reader, "FailedCount").GetValueOrDefault(),
                        WarningCount = ReadInt(reader, "WarningCount").GetValueOrDefault()
                    });
                }
            }

            return rows;
        }

        private int? GetLastRunId(SqlConnection connection)
        {
            if (!TableExists(connection, "dbo.POS_SqlUpdateRun"))
            {
                return null;
            }

            var value = ExecuteScalar(connection, "SELECT MAX(RunId) FROM dbo.POS_SqlUpdateRun");
            return value == DBNull.Value || value == null ? (int?)null : Convert.ToInt32(value, CultureInfo.InvariantCulture);
        }

        private bool TestPosDatabase(SqlConnection connection, Manifest manifest)
        {
            foreach (var requiredObject in manifest.RequiredProbeObjects)
            {
                using (var command = new SqlCommand("SELECT CASE WHEN OBJECT_ID(@ObjectName) IS NULL THEN 0 ELSE 1 END", connection))
                {
                    command.Parameters.Add("@ObjectName", SqlDbType.NVarChar, 256).Value = requiredObject;
                    if (Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture) != 1)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private Manifest LoadManifest()
        {
            if (!File.Exists(_manifestPath))
            {
                throw new FileNotFoundException("POS SQL update manifest was not found.", _manifestPath);
            }

            var manifest = JsonConvert.DeserializeObject<Manifest>(File.ReadAllText(_manifestPath, Encoding.UTF8));
            if (manifest == null)
            {
                throw new InvalidOperationException("POS SQL update manifest is empty or invalid.");
            }

            manifest.Scripts = manifest.Scripts ?? new List<ManifestScript>();
            manifest.ManualScripts = manifest.ManualScripts ?? new List<ManifestManualScript>();
            manifest.RequiredProbeObjects = manifest.RequiredProbeObjects ?? new List<string>();
            return manifest;
        }

        private string ResolveScriptRoot(Manifest manifest)
        {
            var manifestDir = Path.GetDirectoryName(_manifestPath);
            var root = string.IsNullOrWhiteSpace(manifest.ScriptRoot) ? "." : manifest.ScriptRoot;
            var combined = Path.GetFullPath(Path.Combine(manifestDir, root));
            var allowedRoot = Path.GetFullPath(MapPath("~/Areas/Pos/Sql"));
            if (!combined.StartsWith(allowedRoot, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Manifest script root is outside the approved POS SQL folder.");
            }

            return combined;
        }

        private string SafeScriptPath(string scriptRoot, string fileName, bool allowManualFallback)
        {
            if (string.IsNullOrWhiteSpace(fileName) || fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                throw new InvalidOperationException("Invalid script file name in POS SQL manifest.");
            }

            var fullPath = Path.GetFullPath(Path.Combine(scriptRoot, fileName));
            if (!fullPath.StartsWith(scriptRoot, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Manifest script path is outside the approved POS SQL folder.");
            }

            if (allowManualFallback && !File.Exists(fullPath) && fileName.StartsWith("MANUAL_", StringComparison.OrdinalIgnoreCase))
            {
                fullPath = Path.GetFullPath(Path.Combine(scriptRoot, fileName.Substring("MANUAL_".Length)));
            }

            return fullPath;
        }

        private string SafeScriptPath(string scriptRoot, string fileName)
        {
            var path = SafeScriptPath(scriptRoot, fileName, false);
            if (!File.Exists(path))
            {
                throw new FileNotFoundException("Manifest SQL script was not found.", fileName);
            }

            return path;
        }

        private static string ResolveConnectionString()
        {
            var configuredName = ConfigurationManager.AppSettings["PosSqlAutoUpdateConnectionStringName"];
            var name = string.IsNullOrWhiteSpace(configuredName) ? DefaultConnectionName : configuredName;
            var connectionString = ConfigurationManager.ConnectionStrings[name];
            if (connectionString == null || string.IsNullOrWhiteSpace(connectionString.ConnectionString))
            {
                throw new ConfigurationErrorsException("Missing connection string: " + name);
            }

            return connectionString.ConnectionString;
        }

        private static string MapPath(string virtualPath)
        {
            if (HttpContext.Current != null && HttpContext.Current.Server != null)
            {
                return HttpContext.Current.Server.MapPath(virtualPath);
            }

            var root = AppDomain.CurrentDomain.BaseDirectory;
            return Path.GetFullPath(Path.Combine(root, virtualPath.TrimStart('~', '/', '\\').Replace('/', Path.DirectorySeparatorChar)));
        }

        private static string GetSha256(string path)
        {
            using (var sha = SHA256.Create())
            using (var stream = File.OpenRead(path))
            {
                return string.Concat(sha.ComputeHash(stream).Select(b => b.ToString("x2", CultureInfo.InvariantCulture))).ToUpperInvariant();
            }
        }

        private static bool TableExists(SqlConnection connection, string tableName)
        {
            using (var command = new SqlCommand("SELECT CASE WHEN OBJECT_ID(@TableName, N'U') IS NULL THEN 0 ELSE 1 END", connection))
            {
                command.Parameters.Add("@TableName", SqlDbType.NVarChar, 256).Value = tableName;
                return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture) == 1;
            }
        }

        private static bool ColumnExists(SqlConnection connection, string tableName, string columnName)
        {
            using (var command = new SqlCommand("SELECT CASE WHEN COL_LENGTH(@TableName, @ColumnName) IS NULL THEN 0 ELSE 1 END", connection))
            {
                command.Parameters.Add("@TableName", SqlDbType.NVarChar, 256).Value = tableName;
                command.Parameters.Add("@ColumnName", SqlDbType.NVarChar, 128).Value = columnName;
                return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture) == 1;
            }
        }

        private static object ExecuteScalar(SqlConnection connection, string sql)
        {
            using (var command = new SqlCommand(sql, connection))
            {
                command.CommandTimeout = 0;
                return command.ExecuteScalar();
            }
        }

        private static void ExecuteNonQuery(SqlConnection connection, string sql)
        {
            using (var command = new SqlCommand(sql, connection))
            {
                command.CommandTimeout = 0;
                command.ExecuteNonQuery();
            }
        }

        private static object NullIfEmpty(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? (object)DBNull.Value : value.Trim();
        }

        private static string SafeError(Exception ex)
        {
            var message = ex.Message ?? string.Empty;
            return message.Length > 3500 ? message.Substring(0, 3500) : message;
        }

        private static PosSqlUpdateRunResult Fail(string message)
        {
            return new PosSqlUpdateRunResult { Success = false, Message = message };
        }

        private static string BuildDryRunMessage(PosSqlUpdateStatusResult status)
        {
            if (!status.IsPosDatabase)
            {
                return "قاعدة البيانات الحالية لا تطابق قاعدة POS. لن يتم تطبيق أي تحديث.";
            }

            if (status.HashMismatchCount > 0)
            {
                return "يوجد اختلاف Hash في سكريبتات مطبقة سابقاً. يجب المراجعة قبل التنفيذ.";
            }

            return status.PendingCount == 0
                ? "لا توجد تحديثات منتظرة."
                : "يوجد " + status.PendingCount.ToString(CultureInfo.InvariantCulture) + " تحديث منتظر للتطبيق.";
        }

        private static string ToArabicStatus(string status)
        {
            switch ((status ?? string.Empty).ToLowerInvariant())
            {
                case "applied": return "تم التطبيق";
                case "pending": return "منتظر";
                case "failed": return "فشل";
                case "hashmismatch": return "اختلاف Hash";
                case "manual": return "يدوي";
                default: return status;
            }
        }

        private static string ToCssStatus(string status)
        {
            switch ((status ?? string.Empty).ToLowerInvariant())
            {
                case "applied": return "applied";
                case "pending": return "pending";
                case "failed": return "failed";
                case "hashmismatch": return "hash-mismatch";
                case "manual": return "manual";
                default: return "unknown";
            }
        }

        private static string ReadString(IDataRecord reader, string name)
        {
            var ordinal = reader.GetOrdinal(name);
            return reader.IsDBNull(ordinal) ? string.Empty : Convert.ToString(reader.GetValue(ordinal), CultureInfo.InvariantCulture);
        }

        private static bool ReadBool(IDataRecord reader, string name)
        {
            var ordinal = reader.GetOrdinal(name);
            return !reader.IsDBNull(ordinal) && Convert.ToBoolean(reader.GetValue(ordinal), CultureInfo.InvariantCulture);
        }

        private static int? ReadInt(IDataRecord reader, string name)
        {
            var ordinal = reader.GetOrdinal(name);
            return reader.IsDBNull(ordinal) ? (int?)null : Convert.ToInt32(reader.GetValue(ordinal), CultureInfo.InvariantCulture);
        }

        private static decimal ReadDecimal(IDataRecord reader, string name)
        {
            var ordinal = reader.GetOrdinal(name);
            return reader.IsDBNull(ordinal) ? 0m : Convert.ToDecimal(reader.GetValue(ordinal), CultureInfo.InvariantCulture);
        }

        private static string ReadDateTime(IDataRecord reader, string name)
        {
            var ordinal = reader.GetOrdinal(name);
            return reader.IsDBNull(ordinal) ? string.Empty : Convert.ToDateTime(reader.GetValue(ordinal), CultureInfo.InvariantCulture).ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
        }

        private static DateTime ReadDateTimeValue(IDataRecord reader, string name)
        {
            var ordinal = reader.GetOrdinal(name);
            return Convert.ToDateTime(reader.GetValue(ordinal), CultureInfo.InvariantCulture);
        }

        private static DateTime? ReadNullableDateTime(IDataRecord reader, string name)
        {
            var ordinal = reader.GetOrdinal(name);
            return reader.IsDBNull(ordinal) ? (DateTime?)null : Convert.ToDateTime(reader.GetValue(ordinal), CultureInfo.InvariantCulture);
        }

        private class HistoryRow
        {
            public string ScriptName { get; set; }
            public string ScriptHash { get; set; }
            public bool Success { get; set; }
            public DateTime AppliedOn { get; set; }
            public string AppliedBy { get; set; }
            public string ErrorMessage { get; set; }
        }

        private class Manifest
        {
            [JsonProperty("module")]
            public string Module { get; set; }

            [JsonProperty("scriptRoot")]
            public string ScriptRoot { get; set; }

            [JsonProperty("requiredProbeObjects")]
            public IList<string> RequiredProbeObjects { get; set; }

            [JsonProperty("scripts")]
            public IList<ManifestScript> Scripts { get; set; }

            [JsonProperty("manualScripts")]
            public IList<ManifestManualScript> ManualScripts { get; set; }
        }

        private class ManifestScript
        {
            [JsonProperty("order")]
            public decimal Order { get; set; }

            [JsonProperty("file")]
            public string File { get; set; }

            [JsonProperty("autoApply")]
            public bool AutoApply { get; set; }

            [JsonProperty("purpose")]
            public string Purpose { get; set; }
        }

        private class ManifestManualScript
        {
            [JsonProperty("file")]
            public string File { get; set; }

            [JsonProperty("reason")]
            public string Reason { get; set; }
        }
    }
}
