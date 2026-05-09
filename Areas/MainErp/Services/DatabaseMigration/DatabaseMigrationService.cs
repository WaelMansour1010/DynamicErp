using MyERP.Areas.MainErp.Infrastructure;
using MyERP.Areas.MainErp.Models.Security;
using MyERP.Areas.MainErp.ViewModels.DatabaseMigration;
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

namespace MyERP.Areas.MainErp.Services.DatabaseMigration
{
    public class DatabaseMigrationService
    {
        private readonly MainErpDbConnectionFactory _connectionFactory;
        private static readonly Regex NumberedScript = new Regex(@"^(?<number>\d{4})_(?<module>[A-Za-z0-9]+)_.+\.sql$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public DatabaseMigrationService() : this(new MainErpDbConnectionFactory()) { }
        public DatabaseMigrationService(MainErpDbConnectionFactory connectionFactory) { _connectionFactory = connectionFactory; }

        public DatabaseMigrationDashboardViewModel BuildDashboard(MigrationRunResult lastRun)
        {
            using (var connection = _connectionFactory.CreateOpenConnection())
            {
                var files = DiscoverMigrationFiles();
                var history = ReadHistory(connection);
                MarkStates(files, history);
                var applied = history.Where(x => x.Success).OrderByDescending(x => x.AppliedOn).ToList();
                var failed = history.Where(x => !x.Success).OrderByDescending(x => x.AppliedOn).ToList();
                var pending = files.Where(x => x.IsClassified && x.ValidationStatus == "Pending" && !x.HasHashMismatch).ToList();
                var mismatches = files.Where(x => x.HasHashMismatch).ToList();
                return new DatabaseMigrationDashboardViewModel
                {
                    ServerName = ScalarString(connection, "SELECT CONVERT(NVARCHAR(128), @@SERVERNAME)"),
                    DatabaseName = connection.Database,
                    EnvironmentName = ConfigurationManager.AppSettings["DatabaseMigrationEnvironment"] ?? "Current",
                    Status = failed.Any() ? "Error" : (mismatches.Any() ? "Warning" : (pending.Any() ? "Pending" : "Up to date")),
                    LastAppliedOn = applied.Select(x => (DateTime?)x.AppliedOn).FirstOrDefault(),
                    AppliedCount = applied.Count,
                    PendingCount = pending.Count,
                    FailedCount = failed.Count,
                    HashMismatchCount = mismatches.Count,
                    Pending = pending,
                    HashMismatches = mismatches,
                    AppliedHistory = applied,
                    FailedHistory = failed,
                    Sources = GetSources(),
                    Runs = ReadRuns(connection),
                    LastRunResult = lastRun,
                    Warnings = files.Where(x => !x.IsClassified).Select(x => new MigrationWarning { Severity = "Warning", Code = "Unclassified", Message = x.ScriptName + " is unclassified and will not be applied automatically." }).ToList()
                };
            }
        }

        public MigrationRunResult DryRun(MainErpUserContext user)
        {
            using (var connection = _connectionFactory.CreateOpenConnection())
            {
                var files = DiscoverMigrationFiles();
                MarkStates(files, ReadHistory(connection));
                return new MigrationRunResult
                {
                    Mode = "DryRun",
                    Status = files.Any(x => x.HasHashMismatch) ? "Warning" : "Ready",
                    Pending = files.Where(x => x.IsClassified && x.ValidationStatus == "Pending" && !x.HasHashMismatch).ToList(),
                    HashMismatches = files.Where(x => x.HasHashMismatch).ToList(),
                    TotalScripts = files.Count(x => x.IsClassified && x.ValidationStatus == "Pending" && !x.HasHashMismatch),
                    WarningCount = files.Count(x => x.HasHashMismatch || !x.IsClassified)
                };
            }
        }

        public MigrationRunResult Apply(MigrationRunRequest request, MainErpUserContext user, bool applyAll)
        {
            if (request == null) request = new MigrationRunRequest();
            if (!String.Equals(request.ConfirmText, "APPLY", StringComparison.Ordinal)) throw new InvalidOperationException("Type APPLY to confirm database changes.");

            using (var connection = _connectionFactory.CreateOpenConnection())
            {
                EnsureMetadata(connection);
                var files = DiscoverMigrationFiles();
                MarkStates(files, ReadHistory(connection));
                var mismatches = files.Where(x => x.HasHashMismatch).ToList();
                var pending = files.Where(x => x.IsClassified && x.ValidationStatus == "Pending" && !x.HasHashMismatch).ToList();
                if (!applyAll)
                {
                    var selected = new HashSet<string>(request.ScriptKeys ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
                    pending = pending.Where(x => selected.Contains(x.ScriptKey)).ToList();
                }

                var result = new MigrationRunResult { Mode = "Apply", Status = mismatches.Any() ? "Blocked" : "Started", Pending = pending, HashMismatches = mismatches, TotalScripts = pending.Count, WarningCount = mismatches.Count };
                if (mismatches.Any()) return result;

                var runId = InsertRun(connection, user, "Apply", pending.Count, mismatches.Count);
                result.RunId = runId;
                foreach (var file in pending.OrderBy(x => x.MigrationNumber ?? 999999).ThenBy(x => x.ScriptName))
                {
                    var sw = Stopwatch.StartNew();
                    try
                    {
                        ExecuteScript(connection, file.FullPath);
                        sw.Stop();
                        InsertHistory(connection, file, user, true, null, (int)sw.ElapsedMilliseconds, request.ReleaseNo);
                        InsertRunDetail(connection, runId, file, "Applied", (int)sw.ElapsedMilliseconds, null);
                        result.Applied.Add(new MigrationRunDetailViewModel { RunId = runId, ScriptName = file.ScriptName, ModuleName = file.ModuleName, Status = "Applied", DurationMs = (int)sw.ElapsedMilliseconds, ScriptHash = file.ScriptHash });
                    }
                    catch (Exception ex)
                    {
                        sw.Stop();
                        InsertHistory(connection, file, user, false, SafeSqlError(ex), (int)sw.ElapsedMilliseconds, request.ReleaseNo);
                        InsertRunDetail(connection, runId, file, "Failed", (int)sw.ElapsedMilliseconds, SafeSqlError(ex));
                        result.Failed.Add(new MigrationRunDetailViewModel { RunId = runId, ScriptName = file.ScriptName, ModuleName = file.ModuleName, Status = "Failed", DurationMs = (int)sw.ElapsedMilliseconds, ErrorMessage = SafeSqlError(ex), ScriptHash = file.ScriptHash });
                        Trace.TraceError("Database migration failed: " + ex);
                        if (request.StopOnError) break;
                    }
                }

                result.AppliedCount = result.Applied.Count;
                result.FailedCount = result.Failed.Count;
                result.Status = result.FailedCount > 0 ? "Failed" : "Completed";
                UpdateRun(connection, runId, result.Status, result.AppliedCount, result.FailedCount, result.WarningCount);
                return result;
            }
        }

        public MigrationScriptPreviewViewModel PreviewScript(string key)
        {
            var file = DiscoverMigrationFiles().FirstOrDefault(x => String.Equals(x.ScriptKey, key, StringComparison.OrdinalIgnoreCase));
            if (file == null) throw new FileNotFoundException();
            var content = File.ReadAllText(file.FullPath);
            return new MigrationScriptPreviewViewModel { ScriptKey = file.ScriptKey, ScriptName = file.ScriptName, ScriptPath = file.ScriptPath, Content = content, Warnings = DetectWarnings(content) };
        }

        public byte[] ExportReport()
        {
            using (var connection = _connectionFactory.CreateOpenConnection())
            {
                var model = BuildDashboard(null);
                var sb = new StringBuilder();
                sb.AppendLine("Section,ScriptName,ModuleName,Status,Hash,AppliedOn,ErrorMessage");
                foreach (var p in model.Pending) sb.AppendCsv("Pending", p.ScriptName, p.ModuleName, p.ValidationStatus, p.ScriptHash, "", "");
                foreach (var m in model.HashMismatches) sb.AppendCsv("HashMismatch", m.ScriptName, m.ModuleName, "Hash mismatch", m.ScriptHash, "", "");
                foreach (var h in model.AppliedHistory) sb.AppendCsv("History", h.ScriptName, h.ModuleName, h.Success ? "Applied" : "Failed", h.ScriptHash, h.AppliedOn.ToString("s"), h.ErrorMessage);
                return Encoding.UTF8.GetBytes(sb.ToString());
            }
        }

        private List<MigrationFileInfoViewModel> DiscoverMigrationFiles()
        {
            var files = new List<MigrationFileInfoViewModel>();
            foreach (var source in GetSources().Where(x => x.Exists))
            {
                foreach (var path in Directory.GetFiles(source.ResolvedPath, "*.sql", SearchOption.AllDirectories))
                {
                    var name = Path.GetFileName(path);
                    var match = NumberedScript.Match(name);
                    var content = File.ReadAllText(path);
                    var info = new MigrationFileInfoViewModel
                    {
                        FullPath = path,
                        ScriptName = name,
                        ScriptPath = ToAppRelative(path),
                        ScriptHash = CalculateHash(path),
                        LastModifiedOn = File.GetLastWriteTime(path),
                        IsClassified = match.Success,
                        ValidationStatus = match.Success ? "Pending" : "Unclassified",
                        ModuleName = match.Success ? match.Groups["module"].Value : ParseHeader(content, "Module") ?? "Unclassified",
                        Dependencies = ParseHeader(content, "Dependencies") ?? "None",
                        SafeToRerun = String.Equals(ParseHeader(content, "Safe to rerun?") ?? ParseHeader(content, "Safe to rerun"), "Yes", StringComparison.OrdinalIgnoreCase),
                        UpdateType = ClassifyType(content),
                        ScriptKey = HttpServerUtility.UrlTokenEncode(Encoding.UTF8.GetBytes(path.ToUpperInvariant()))
                    };
                    int number;
                    if (match.Success && Int32.TryParse(match.Groups["number"].Value, out number)) info.MigrationNumber = number;
                    info.Warnings = DetectWarnings(content);
                    files.Add(info);
                }
            }
            return files.OrderBy(x => x.MigrationNumber ?? 999999).ThenBy(x => x.ScriptName).ToList();
        }

        private void MarkStates(IList<MigrationFileInfoViewModel> files, IList<MigrationHistoryViewModel> history)
        {
            foreach (var file in files)
            {
                var sameName = history.Where(x => String.Equals(x.ScriptName, file.ScriptName, StringComparison.OrdinalIgnoreCase) && x.Success).ToList();
                file.HasHashMismatch = sameName.Any() && !sameName.Any(x => String.Equals(x.ScriptHash, file.ScriptHash, StringComparison.OrdinalIgnoreCase));
                if (!file.IsClassified) file.ValidationStatus = "Unclassified";
                else if (file.HasHashMismatch) file.ValidationStatus = "Hash mismatch";
                else if (sameName.Any(x => String.Equals(x.ScriptHash, file.ScriptHash, StringComparison.OrdinalIgnoreCase))) file.ValidationStatus = "Applied";
                else file.ValidationStatus = "Pending";
            }
        }

        private List<MigrationSourceViewModel> GetSources()
        {
            var configured = (ConfigurationManager.AppSettings["DatabaseMigrationFolders"] ?? "~/Database/Migrations").Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries);
            return configured.Select(x => x.Trim()).Where(x => x.Length > 0).Select(x =>
            {
                var resolved = ResolvePath(x);
                return new MigrationSourceViewModel { ConfiguredPath = x, ResolvedPath = resolved, Exists = Directory.Exists(resolved), SqlFileCount = Directory.Exists(resolved) ? Directory.GetFiles(resolved, "*.sql", SearchOption.AllDirectories).Length : 0 };
            }).ToList();
        }

        private static string ResolvePath(string path)
        {
            if (path.StartsWith("~/")) return HttpContext.Current.Server.MapPath(path);
            return Path.GetFullPath(path);
        }

        private static string ToAppRelative(string path)
        {
            var root = HttpContext.Current.Server.MapPath("~/");
            return "~/" + path.Substring(root.Length).Replace('\\', '/');
        }

        private static string CalculateHash(string path)
        {
            using (var sha = SHA256.Create())
            using (var stream = File.OpenRead(path)) return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "");
        }

        private static string ParseHeader(string content, string key)
        {
            var rx = new Regex(@"^\s*" + Regex.Escape(key) + @"\s*:?[ \t]*(?<v>.+)$", RegexOptions.IgnoreCase | RegexOptions.Multiline);
            var m = rx.Match(content ?? "");
            return m.Success ? m.Groups["v"].Value.Trim() : null;
        }

        private static string ClassifyType(string sql)
        {
            var s = sql ?? "";
            if (Regex.IsMatch(s, @"\b(PROCEDURE|PROC)\b", RegexOptions.IgnoreCase)) return "SP";
            if (Regex.IsMatch(s, @"\bALTER\s+TABLE\b|\bCREATE\s+TABLE\b", RegexOptions.IgnoreCase)) return "Table";
            if (Regex.IsMatch(s, @"\bCREATE\s+VIEW\b|\bALTER\s+VIEW\b", RegexOptions.IgnoreCase)) return "View";
            if (Regex.IsMatch(s, @"\bCREATE\s+(UNIQUE\s+)?INDEX\b", RegexOptions.IgnoreCase)) return "Index";
            if (Regex.IsMatch(s, @"\bINSERT\b|\bUPDATE\b|\bDELETE\b", RegexOptions.IgnoreCase)) return "Data Fix";
            return "Script";
        }

        private static List<MigrationWarning> DetectWarnings(string sql)
        {
            var warnings = new List<MigrationWarning>();
            AddWarning(warnings, sql, @"\bDROP\s+TABLE\b", "DropTable", "DROP TABLE detected.");
            AddWarning(warnings, sql, @"\bTRUNCATE\b", "Truncate", "TRUNCATE detected.");
            AddWarning(warnings, sql, @"\bALTER\s+COLUMN\b", "AlterColumn", "ALTER COLUMN detected.");
            AddWarning(warnings, sql, @"\bEXEC\s*\(|sp_executesql", "DynamicSql", "Dynamic SQL detected.");
            if (Regex.IsMatch(sql ?? "", @"\bUPDATE\b", RegexOptions.IgnoreCase) && !Regex.IsMatch(sql ?? "", @"\bWHERE\b", RegexOptions.IgnoreCase)) warnings.Add(new MigrationWarning { Severity = "Danger", Code = "UpdateWithoutWhere", Message = "UPDATE without WHERE detected." });
            if (Regex.IsMatch(sql ?? "", @"\bDELETE\b", RegexOptions.IgnoreCase) && !Regex.IsMatch(sql ?? "", @"\bWHERE\b", RegexOptions.IgnoreCase)) warnings.Add(new MigrationWarning { Severity = "Danger", Code = "DeleteWithoutWhere", Message = "DELETE without WHERE detected." });
            return warnings;
        }

        private static void AddWarning(ICollection<MigrationWarning> warnings, string sql, string pattern, string code, string message)
        {
            if (Regex.IsMatch(sql ?? "", pattern, RegexOptions.IgnoreCase)) warnings.Add(new MigrationWarning { Severity = "Danger", Code = code, Message = message });
        }

        private List<MigrationHistoryViewModel> ReadHistory(SqlConnection connection)
        {
            if (!TableExists(connection, "dbo.DatabaseMigrationHistory")) return new List<MigrationHistoryViewModel>();
            var list = new List<MigrationHistoryViewModel>();
            using (var cmd = new SqlCommand("SELECT MigrationId, ScriptName, ScriptPath, ScriptHash, ModuleName, AppliedOn, AppliedBy, MachineName, DatabaseName, DurationMs, Success, ErrorMessage, ReleaseNo FROM dbo.DatabaseMigrationHistory ORDER BY AppliedOn DESC, MigrationId DESC", connection))
            using (var r = cmd.ExecuteReader())
                while (r.Read()) list.Add(new MigrationHistoryViewModel { MigrationId = r.GetInt32(0), ScriptName = r.GetString(1), ScriptPath = r.GetString(2), ScriptHash = r.GetString(3), ModuleName = r.GetString(4), AppliedOn = r.GetDateTime(5), AppliedBy = r.GetString(6), MachineName = r.GetString(7), DatabaseName = r.GetString(8), DurationMs = r.IsDBNull(9) ? (int?)null : r.GetInt32(9), Success = r.GetBoolean(10), ErrorMessage = r.IsDBNull(11) ? null : r.GetString(11), ReleaseNo = r.IsDBNull(12) ? null : r.GetString(12) });
            return list;
        }

        private List<MigrationRunViewModel> ReadRuns(SqlConnection connection)
        {
            var runs = new List<MigrationRunViewModel>();
            if (!TableExists(connection, "dbo.DatabaseMigrationRun")) return runs;
            using (var cmd = new SqlCommand("SELECT TOP 20 RunId, StartedAt, FinishedAt, StartedBy, DatabaseName, ServerName, Mode, Status, TotalScripts, AppliedCount, FailedCount, WarningCount FROM dbo.DatabaseMigrationRun ORDER BY RunId DESC", connection))
            using (var r = cmd.ExecuteReader())
                while (r.Read()) runs.Add(new MigrationRunViewModel { RunId = Convert.ToInt64(r.GetValue(0)), StartedAt = r.GetDateTime(1), FinishedAt = r.IsDBNull(2) ? (DateTime?)null : r.GetDateTime(2), StartedBy = r.GetString(3), DatabaseName = r.GetString(4), ServerName = r.GetString(5), Mode = r.GetString(6), Status = r.GetString(7), TotalScripts = r.GetInt32(8), AppliedCount = r.GetInt32(9), FailedCount = r.GetInt32(10), WarningCount = r.GetInt32(11) });
            if (runs.Count == 0 || !TableExists(connection, "dbo.DatabaseMigrationRunDetail")) return runs;
            var ids = String.Join(",", runs.Select(x => x.RunId.ToString(CultureInfo.InvariantCulture)).ToArray());
            using (var cmd = new SqlCommand("SELECT RunDetailId, RunId, ScriptName, ModuleName, Status, DurationMs, ErrorMessage, ScriptHash FROM dbo.DatabaseMigrationRunDetail WHERE RunId IN (" + ids + ") ORDER BY RunDetailId", connection))
            using (var r = cmd.ExecuteReader())
                while (r.Read())
                {
                    var runId = Convert.ToInt64(r.GetValue(1));
                    var run = runs.FirstOrDefault(x => x.RunId == runId);
                    if (run != null) run.Details.Add(new MigrationRunDetailViewModel { RunDetailId = Convert.ToInt64(r.GetValue(0)), RunId = runId, ScriptName = r.GetString(2), ModuleName = r.GetString(3), Status = r.GetString(4), DurationMs = r.IsDBNull(5) ? (int?)null : r.GetInt32(5), ErrorMessage = r.IsDBNull(6) ? null : r.GetString(6), ScriptHash = r.GetString(7) });
                }
            return runs;
        }

        private static bool TableExists(SqlConnection connection, string tableName)
        {
            using (var cmd = new SqlCommand("SELECT CASE WHEN OBJECT_ID(@n, N'U') IS NULL THEN 0 ELSE 1 END", connection))
            { cmd.Parameters.AddWithValue("@n", tableName); return Convert.ToInt32(cmd.ExecuteScalar()) == 1; }
        }

        private static string ScalarString(SqlConnection connection, string sql)
        {
            using (var cmd = new SqlCommand(sql, connection)) return Convert.ToString(cmd.ExecuteScalar());
        }

        private static void ExecuteScript(SqlConnection connection, string path)
        {
            var batches = SplitBatches(File.ReadAllText(path)).Where(x => !String.IsNullOrWhiteSpace(x)).ToList();
            using (var transaction = connection.BeginTransaction())
            {
                try
                {
                    foreach (var batch in batches) using (var cmd = new SqlCommand(batch, connection, transaction)) { cmd.CommandTimeout = 0; cmd.ExecuteNonQuery(); }
                    transaction.Commit();
                }
                catch { transaction.Rollback(); throw; }
            }
        }

        private static IEnumerable<string> SplitBatches(string sql)
        {
            var sb = new StringBuilder();
            using (var reader = new StringReader(sql ?? ""))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (Regex.IsMatch(line, @"^\s*GO\s*(--.*)?$", RegexOptions.IgnoreCase)) { yield return sb.ToString(); sb.Length = 0; }
                    else sb.AppendLine(line);
                }
            }
            if (sb.Length > 0) yield return sb.ToString();
        }

        private static string SafeSqlError(Exception ex)
        {
            return ex.Message.Length > 2000 ? ex.Message.Substring(0, 2000) : ex.Message;
        }

        private void EnsureMetadata(SqlConnection connection)
        {
            foreach (var batch in SplitBatches(MetadataSql)) using (var cmd = new SqlCommand(batch, connection)) cmd.ExecuteNonQuery();
        }

        private long InsertRun(SqlConnection connection, MainErpUserContext user, string mode, int total, int warnings)
        {
            using (var cmd = new SqlCommand(@"INSERT dbo.DatabaseMigrationRun (StartedAt, StartedBy, DatabaseName, ServerName, Mode, Status, TotalScripts, WarningCount) VALUES (GETDATE(), @u, DB_NAME(), CONVERT(NVARCHAR(128), @@SERVERNAME), @m, N'Started', @t, @w); SELECT SCOPE_IDENTITY();", connection))
            { cmd.Parameters.AddWithValue("@u", UserName(user)); cmd.Parameters.AddWithValue("@m", mode); cmd.Parameters.AddWithValue("@t", total); cmd.Parameters.AddWithValue("@w", warnings); return Convert.ToInt64(cmd.ExecuteScalar()); }
        }

        private static void UpdateRun(SqlConnection connection, long runId, string status, int applied, int failed, int warnings)
        {
            using (var cmd = new SqlCommand("UPDATE dbo.DatabaseMigrationRun SET FinishedAt = GETDATE(), Status = @s, AppliedCount = @a, FailedCount = @f, WarningCount = @w WHERE RunId = @id", connection))
            { cmd.Parameters.AddWithValue("@s", status); cmd.Parameters.AddWithValue("@a", applied); cmd.Parameters.AddWithValue("@f", failed); cmd.Parameters.AddWithValue("@w", warnings); cmd.Parameters.AddWithValue("@id", runId); cmd.ExecuteNonQuery(); }
        }

        private static void InsertHistory(SqlConnection connection, MigrationFileInfoViewModel file, MainErpUserContext user, bool success, string error, int duration, string releaseNo)
        {
            using (var cmd = new SqlCommand(@"INSERT dbo.DatabaseMigrationHistory (ScriptName, ScriptPath, ScriptHash, ModuleName, AppliedBy, MachineName, DatabaseName, DurationMs, Success, ErrorMessage, ReleaseNo) VALUES (@sn, @sp, @sh, @m, @u, @machine, DB_NAME(), @d, @ok, @e, @r)", connection))
            {
                cmd.Parameters.AddWithValue("@sn", file.ScriptName); cmd.Parameters.AddWithValue("@sp", file.ScriptPath); cmd.Parameters.AddWithValue("@sh", file.ScriptHash); cmd.Parameters.AddWithValue("@m", file.ModuleName); cmd.Parameters.AddWithValue("@u", UserName(user)); cmd.Parameters.AddWithValue("@machine", Environment.MachineName); cmd.Parameters.AddWithValue("@d", duration); cmd.Parameters.AddWithValue("@ok", success); cmd.Parameters.AddWithValue("@e", (object)error ?? DBNull.Value); cmd.Parameters.AddWithValue("@r", (object)releaseNo ?? DBNull.Value); cmd.ExecuteNonQuery();
            }
        }

        private static void InsertRunDetail(SqlConnection connection, long runId, MigrationFileInfoViewModel file, string status, int duration, string error)
        {
            using (var cmd = new SqlCommand(@"INSERT dbo.DatabaseMigrationRunDetail (RunId, ScriptName, ModuleName, Status, DurationMs, ErrorMessage, ScriptHash) VALUES (@rid, @sn, @m, @s, @d, @e, @h)", connection))
            { cmd.Parameters.AddWithValue("@rid", runId); cmd.Parameters.AddWithValue("@sn", file.ScriptName); cmd.Parameters.AddWithValue("@m", file.ModuleName); cmd.Parameters.AddWithValue("@s", status); cmd.Parameters.AddWithValue("@d", duration); cmd.Parameters.AddWithValue("@e", (object)error ?? DBNull.Value); cmd.Parameters.AddWithValue("@h", file.ScriptHash); cmd.ExecuteNonQuery(); }
        }

        private static string UserName(MainErpUserContext user)
        {
            return user == null || String.IsNullOrWhiteSpace(user.UserName) ? "Unknown" : user.UserName;
        }

        private const string MetadataSql = @"
IF OBJECT_ID(N'dbo.DatabaseMigrationHistory', N'U') IS NULL
BEGIN
CREATE TABLE dbo.DatabaseMigrationHistory (MigrationId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_DatabaseMigrationHistory PRIMARY KEY, ScriptName NVARCHAR(260) NOT NULL, ScriptPath NVARCHAR(1000) NOT NULL, ScriptHash CHAR(64) NOT NULL, ModuleName NVARCHAR(100) NOT NULL, AppliedOn DATETIME NOT NULL CONSTRAINT DF_DatabaseMigrationHistory_AppliedOn DEFAULT (GETDATE()), AppliedBy NVARCHAR(256) NOT NULL, MachineName NVARCHAR(128) NOT NULL, DatabaseName SYSNAME NOT NULL, DurationMs INT NULL, Success BIT NOT NULL, ErrorMessage NVARCHAR(MAX) NULL, BatchNo NVARCHAR(100) NULL, ReleaseNo NVARCHAR(100) NULL)
END
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_DatabaseMigrationHistory_ScriptName_ScriptHash_Success' AND object_id = OBJECT_ID(N'dbo.DatabaseMigrationHistory', N'U')) CREATE UNIQUE INDEX UX_DatabaseMigrationHistory_ScriptName_ScriptHash_Success ON dbo.DatabaseMigrationHistory (ScriptName, ScriptHash, Success) WHERE Success = 1
GO
IF OBJECT_ID(N'dbo.DatabaseMigrationRun', N'U') IS NULL CREATE TABLE dbo.DatabaseMigrationRun (RunId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_DatabaseMigrationRun PRIMARY KEY, StartedAt DATETIME NOT NULL CONSTRAINT DF_DatabaseMigrationRun_StartedAt DEFAULT (GETDATE()), FinishedAt DATETIME NULL, StartedBy NVARCHAR(256) NOT NULL, DatabaseName SYSNAME NOT NULL, ServerName NVARCHAR(128) NOT NULL, Mode NVARCHAR(20) NOT NULL, Status NVARCHAR(30) NOT NULL, TotalScripts INT NOT NULL CONSTRAINT DF_DatabaseMigrationRun_TotalScripts DEFAULT (0), AppliedCount INT NOT NULL CONSTRAINT DF_DatabaseMigrationRun_AppliedCount DEFAULT (0), FailedCount INT NOT NULL CONSTRAINT DF_DatabaseMigrationRun_FailedCount DEFAULT (0), WarningCount INT NOT NULL CONSTRAINT DF_DatabaseMigrationRun_WarningCount DEFAULT (0))
GO
IF OBJECT_ID(N'dbo.DatabaseMigrationRunDetail', N'U') IS NULL CREATE TABLE dbo.DatabaseMigrationRunDetail (RunDetailId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_DatabaseMigrationRunDetail PRIMARY KEY, RunId INT NOT NULL, ScriptName NVARCHAR(260) NOT NULL, ModuleName NVARCHAR(100) NOT NULL, Status NVARCHAR(30) NOT NULL, DurationMs INT NULL, ErrorMessage NVARCHAR(MAX) NULL, ScriptHash CHAR(64) NOT NULL, CONSTRAINT FK_DatabaseMigrationRunDetail_Run FOREIGN KEY (RunId) REFERENCES dbo.DatabaseMigrationRun(RunId))
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_DatabaseMigrationRunDetail_RunId' AND object_id = OBJECT_ID(N'dbo.DatabaseMigrationRunDetail', N'U')) CREATE INDEX IX_DatabaseMigrationRunDetail_RunId ON dbo.DatabaseMigrationRunDetail (RunId)
GO";
    }

    internal static class CsvExtensions
    {
        public static void AppendCsv(this StringBuilder sb, params string[] values)
        {
            sb.AppendLine(String.Join(",", values.Select(Escape).ToArray()));
        }

        private static string Escape(string value)
        {
            value = value ?? "";
            return "\"" + value.Replace("\"", "\"\"").Replace("\r", " ").Replace("\n", " ") + "\"";
        }
    }
}

