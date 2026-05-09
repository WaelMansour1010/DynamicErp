using MyERP.Areas.MainErp.Infrastructure;
using MyERP.Areas.MainErp.Models.Security;
using MyERP.Areas.MainErp.ViewModels.DatabaseMigration;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
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
        private const string HistoryTableName = "dbo.DatabaseMigrationHistory";
        private const string RunTableName = "dbo.DatabaseMigrationRun";
        private const string RunDetailTableName = "dbo.DatabaseMigrationRunDetail";
        private readonly MainErpDbConnectionFactory _connectionFactory;

        public DatabaseMigrationService()
            : this(new MainErpDbConnectionFactory())
        {
        }

        public DatabaseMigrationService(MainErpDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public DatabaseMigrationDashboardViewModel BuildDashboard(MigrationRunResult lastRunResult)
        {
            using (var connection = _connectionFactory.CreateOpenConnection())
            {
                EnsureMetadataTables(connection);

                var files = DiscoverMigrationFiles();
                var history = ReadHistory(connection, 500);
                var classified = files.Where(x => x.IsClassified).ToList();
                MarkStates(files, history);

                var appliedHistory = history.Where(x => x.Success).OrderByDescending(x => x.AppliedOn).ToList();
                var failedHistory = history.Where(x => !x.Success).OrderByDescending(x => x.AppliedOn).ToList();
                var pending = classified.Where(x => !x.HasHashMismatch && x.ValidationStatus == "Pending").ToList();
                var mismatches = classified.Where(x => x.HasHashMismatch).ToList();
                var runLog = ReadRuns(connection, 50);

                return new DatabaseMigrationDashboardViewModel
                {
                    ServerName = SafeServerName(connection),
                    DatabaseName = connection.Database,
                    EnvironmentName = ConfigurationManager.AppSettings["DatabaseMigrationEnvironment"] ?? "Current",
                    Status = failedHistory.Any() ? "Error" : (mismatches.Any() ? "Warning" : (pending.Any() ? "Pending" : "Up to date")),
                    LastAppliedOn = appliedHistory.Select(x => (DateTime?)x.AppliedOn).FirstOrDefault(),
                    AppliedCount = appliedHistory.Count,
                    PendingCount = pending.Count,
                    FailedCount = failedHistory.Count,
                    HashMismatchCount = mismatches.Count,
                    Pending = pending,
                    HashMismatches = mismatches,
                    AppliedHistory = appliedHistory,
                    FailedHistory = failedHistory,
                    Sources = GetSources(),
                    ExecutionLog = runLog,
                    LastRunResult = lastRunResult,
                    Warnings = files.Where(x => !x.IsClassified).Select(x => new MigrationWarning
                    {
                        Severity = "Warning",
                        Code = "Unclassified",
                        Message = x.ScriptName + " is not numbered with the required migration prefix and will not be applied automatically."
                    }).Take(25).ToList()
                };
            }
        }

        public MigrationRunResult DryRun(MainErpUserContext user)
        {
            using (var connection = _connectionFactory.CreateOpenConnection())
            {
                EnsureMetadataTables(connection);
                var files = DiscoverMigrationFiles();
                var history = ReadHistory(connection, 5000);
                MarkStates(files, history);

                var result = BuildPlanResult(files, "DryRun");
                result.RunId = InsertRun(connection, "DryRun", user, result);
                foreach (var pending in result.Pending)
                {
                    InsertRunDetail(connection, result.RunId.Value, pending.ScriptName, pending.ModuleName, "Pending", null, null, pending.ScriptHash);
                }
                foreach (var mismatch in result.HashMismatches)
                {
                    InsertRunDetail(connection, result.RunId.Value, mismatch.ScriptName, mismatch.ModuleName, "HashMismatch", null, "Script name was applied before with a different hash.", mismatch.ScriptHash);
                }
                FinishRun(connection, result.RunId.Value, result.HashMismatches.Any() ? "Warning" : "Completed", result.AppliedCount, result.FailedCount, result.WarningCount);
                return result;
            }
        }

        public MigrationRunResult Apply(MigrationRunRequest request, MainErpUserContext user, bool applyAll)
        {
            if (request == null)
            {
                request = new MigrationRunRequest();
            }

            if (!string.Equals((request.ConfirmText ?? string.Empty).Trim(), "APPLY", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Confirmation word is required before applying database updates.");
            }

            using (var connection = _connectionFactory.CreateOpenConnection())
            {
                EnsureMetadataTables(connection);
                var files = DiscoverMigrationFiles();
                var history = ReadHistory(connection, 5000);
                MarkStates(files, history);

                var candidates = files
                    .Where(x => x.IsClassified && x.ValidationStatus == "Pending" && !x.HasHashMismatch)
                    .ToList();

                if (!applyAll)
                {
                    var selected = new HashSet<string>(request.ScriptKeys ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
                    candidates = candidates.Where(x => selected.Contains(x.ScriptKey)).ToList();
                }

                var result = BuildPlanResult(files, "Apply");
                result.Pending = candidates;
                result.TotalScripts = candidates.Count;
                result.RunId = InsertRun(connection, "Apply", user, result);

                if (result.HashMismatches.Any())
                {
                    foreach (var mismatch in result.HashMismatches)
                    {
                        InsertRunDetail(connection, result.RunId.Value, mismatch.ScriptName, mismatch.ModuleName, "HashMismatch", null, "Script name was applied before with a different hash.", mismatch.ScriptHash);
                    }

                    result.Status = "Blocked";
                    result.WarningCount = result.HashMismatches.Count;
                    FinishRun(connection, result.RunId.Value, "Blocked", 0, 0, result.WarningCount);
                    return result;
                }

                foreach (var migration in candidates.OrderBy(x => x.MigrationNumber ?? int.MaxValue).ThenBy(x => x.ScriptName, StringComparer.OrdinalIgnoreCase))
                {
                    var detail = ApplySingle(connection, migration, request, user);
                    if (detail.Status == "Applied")
                    {
                        result.Applied.Add(detail);
                    }
                    else
                    {
                        result.Failed.Add(detail);
                    }

                    InsertRunDetail(connection, result.RunId.Value, detail.ScriptName, detail.ModuleName, detail.Status, detail.DurationMs, detail.ErrorMessage, detail.ScriptHash);

                    if (detail.Status == "Failed" && request.StopOnError)
                    {
                        break;
                    }
                }

                result.AppliedCount = result.Applied.Count;
                result.FailedCount = result.Failed.Count;
                result.WarningCount = result.HashMismatches.Count + result.Warnings.Count;
                result.Status = result.Failed.Any() ? "Failed" : "Completed";
                result.FinishedAt = DateTime.Now;
                FinishRun(connection, result.RunId.Value, result.Status, result.AppliedCount, result.FailedCount, result.WarningCount);
                return result;
            }
        }

        public MigrationPreviewViewModel PreviewScript(string scriptKey)
        {
            var file = DiscoverMigrationFiles().FirstOrDefault(x => string.Equals(x.ScriptKey, scriptKey, StringComparison.OrdinalIgnoreCase));
            if (file == null)
            {
                throw new FileNotFoundException("Migration script was not found in configured folders.");
            }

            var fullPath = ResolveFullPath(file.ScriptPath);
            if (!IsUnderAllowedSource(fullPath))
            {
                throw new UnauthorizedAccessException("Script path is outside the configured migration folders.");
            }

            var text = File.ReadAllText(fullPath);
            return new MigrationPreviewViewModel
            {
                File = file,
                ScriptText = text,
                Warnings = DetectDangerousSql(text).ToList()
            };
        }

        public byte[] ExportReport()
        {
            var model = BuildDashboard(null);
            var builder = new StringBuilder();
            builder.AppendLine("Section,ScriptName,Module,Status,AppliedOn,AppliedBy,DurationMs,Hash,Path,Error");
            foreach (var item in model.Pending)
            {
                AppendCsv(builder, "Pending", item.ScriptName, item.ModuleName, item.ValidationStatus, "", "", "", item.ScriptHash, item.ScriptPath, "");
            }
            foreach (var item in model.HashMismatches)
            {
                AppendCsv(builder, "HashMismatch", item.ScriptName, item.ModuleName, "HashMismatch", "", "", "", item.ScriptHash, item.ScriptPath, "Applied script name has a different hash");
            }
            foreach (var item in model.AppliedHistory)
            {
                AppendCsv(builder, "AppliedHistory", item.ScriptName, item.ModuleName, item.Success ? "Applied" : "Failed", item.AppliedOn.ToString("yyyy-MM-dd HH:mm:ss"), item.AppliedBy, Convert.ToString(item.DurationMs), item.ScriptHash, item.ScriptPath, SafeError(item.ErrorMessage));
            }
            foreach (var item in model.FailedHistory)
            {
                AppendCsv(builder, "FailedHistory", item.ScriptName, item.ModuleName, "Failed", item.AppliedOn.ToString("yyyy-MM-dd HH:mm:ss"), item.AppliedBy, Convert.ToString(item.DurationMs), item.ScriptHash, item.ScriptPath, SafeError(item.ErrorMessage));
            }
            return Encoding.UTF8.GetBytes(builder.ToString());
        }

        private MigrationRunDetailViewModel ApplySingle(SqlConnection connection, MigrationFileInfoViewModel migration, MigrationRunRequest request, MainErpUserContext user)
        {
            var stopwatch = Stopwatch.StartNew();
            SqlTransaction transaction = null;
            try
            {
                var fullPath = ResolveFullPath(migration.ScriptPath);
                if (!IsUnderAllowedSource(fullPath))
                {
                    throw new UnauthorizedAccessException("Script path is outside the configured migration folders.");
                }

                var sql = File.ReadAllText(fullPath);
                transaction = connection.BeginTransaction();
                foreach (var batch in SplitSqlBatches(sql))
                {
                    ExecuteNonQuery(connection, batch, transaction);
                }
                transaction.Commit();
                transaction.Dispose();
                transaction = null;
                stopwatch.Stop();

                InsertHistory(connection, migration, stopwatch.ElapsedMilliseconds, true, null, request.ReleaseNo, user);
                return new MigrationRunDetailViewModel
                {
                    ScriptName = migration.ScriptName,
                    ModuleName = migration.ModuleName,
                    Status = "Applied",
                    DurationMs = Convert.ToInt32(Math.Min(stopwatch.ElapsedMilliseconds, int.MaxValue)),
                    ScriptHash = migration.ScriptHash
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                if (transaction != null)
                {
                    try { transaction.Rollback(); } catch { }
                    transaction.Dispose();
                }

                Trace.TraceError("Database migration failed. Script={0}. {1}", migration.ScriptName, ex);
                var safeMessage = SafeError(ex.Message);
                InsertHistory(connection, migration, stopwatch.ElapsedMilliseconds, false, safeMessage, request.ReleaseNo, user);
                return new MigrationRunDetailViewModel
                {
                    ScriptName = migration.ScriptName,
                    ModuleName = migration.ModuleName,
                    Status = "Failed",
                    DurationMs = Convert.ToInt32(Math.Min(stopwatch.ElapsedMilliseconds, int.MaxValue)),
                    ErrorMessage = safeMessage,
                    ScriptHash = migration.ScriptHash
                };
            }
        }

        private List<MigrationFileInfoViewModel> DiscoverMigrationFiles()
        {
            var results = new List<MigrationFileInfoViewModel>();
            foreach (var source in GetSources().Where(x => x.Exists))
            {
                foreach (var file in Directory.GetFiles(source.ResolvedPath, "*.sql", SearchOption.AllDirectories))
                {
                    var info = new FileInfo(file);
                    var relative = ToAppRelativePath(file);
                    var text = File.ReadAllText(file);
                    var header = ParseHeader(text);
                    var number = ParseMigrationNumber(info.Name);
                    var classified = number.HasValue && Regex.IsMatch(info.Name, @"^\d{4}_[A-Za-z0-9]+_.+\.sql$", RegexOptions.IgnoreCase);
                    var warnings = DetectDangerousSql(text).ToList();
                    if (!classified)
                    {
                        warnings.Add(new MigrationWarning { Severity = "Warning", Code = "Unclassified", Message = "Old or unnumbered SQL file. It is visible for review but will not be applied automatically." });
                    }

                    results.Add(new MigrationFileInfoViewModel
                    {
                        ScriptKey = Sha256Text(Path.GetFullPath(file).ToUpperInvariant()),
                        MigrationNumber = number,
                        ScriptName = info.Name,
                        ScriptPath = relative,
                        ScriptHash = Sha256File(file),
                        ModuleName = header.ContainsKey("Module") ? header["Module"] : InferModule(info.Name, relative),
                        LastModifiedOn = info.LastWriteTime,
                        UpdateType = DetectUpdateType(text),
                        SafeToRerun = header.ContainsKey("Safe to rerun?") && header["Safe to rerun?"].IndexOf("Yes", StringComparison.OrdinalIgnoreCase) >= 0,
                        Dependencies = header.ContainsKey("Dependencies") ? header["Dependencies"] : "",
                        ValidationStatus = classified ? "Pending" : "Unclassified",
                        IsClassified = classified,
                        Warnings = warnings
                    });
                }
            }

            return results
                .GroupBy(x => x.ScriptPath, StringComparer.OrdinalIgnoreCase)
                .Select(x => x.First())
                .OrderBy(x => x.MigrationNumber ?? int.MaxValue)
                .ThenBy(x => x.ScriptName, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private void MarkStates(IList<MigrationFileInfoViewModel> files, IList<MigrationHistoryViewModel> history)
        {
            var grouped = history.GroupBy(x => x.ScriptName, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(x => x.Key, x => x.ToList(), StringComparer.OrdinalIgnoreCase);

            foreach (var file in files)
            {
                if (!file.IsClassified)
                {
                    file.ValidationStatus = "Unclassified";
                    continue;
                }

                List<MigrationHistoryViewModel> rows;
                if (!grouped.TryGetValue(file.ScriptName, out rows))
                {
                    file.ValidationStatus = "Pending";
                    continue;
                }

                if (rows.Any(x => x.Success && string.Equals(x.ScriptHash, file.ScriptHash, StringComparison.OrdinalIgnoreCase)))
                {
                    file.ValidationStatus = "Applied";
                    continue;
                }

                if (rows.Any(x => x.Success && !string.Equals(x.ScriptHash, file.ScriptHash, StringComparison.OrdinalIgnoreCase)))
                {
                    file.ValidationStatus = "HashMismatch";
                    file.HasHashMismatch = true;
                    file.Warnings.Add(new MigrationWarning { Severity = "Danger", Code = "HashMismatch", Message = "This script name was applied before with different content." });
                    continue;
                }

                file.ValidationStatus = "Pending";
            }
        }

        private MigrationRunResult BuildPlanResult(IList<MigrationFileInfoViewModel> files, string mode)
        {
            var pending = files.Where(x => x.IsClassified && x.ValidationStatus == "Pending" && !x.HasHashMismatch).ToList();
            var mismatches = files.Where(x => x.IsClassified && x.HasHashMismatch).ToList();
            var warnings = files.SelectMany(x => x.Warnings).ToList();
            return new MigrationRunResult
            {
                Mode = mode,
                Status = mismatches.Any() ? "Warning" : "Ready",
                StartedAt = DateTime.Now,
                FinishedAt = mode == "DryRun" ? (DateTime?)DateTime.Now : null,
                Pending = pending,
                HashMismatches = mismatches,
                TotalScripts = pending.Count,
                WarningCount = warnings.Count + mismatches.Count,
                Warnings = warnings
            };
        }

        private IList<MigrationSourceViewModel> GetSources()
        {
            var configured = ConfigurationManager.AppSettings["DatabaseMigrationFolders"];
            if (string.IsNullOrWhiteSpace(configured))
            {
                configured = "~/Database/Migrations";
            }

            return configured.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(x => x.Length > 0)
                .Select(x =>
                {
                    var resolved = ResolveConfiguredPath(x);
                    return new MigrationSourceViewModel
                    {
                        ConfiguredPath = x,
                        ResolvedPath = resolved,
                        Exists = Directory.Exists(resolved),
                        SqlFileCount = Directory.Exists(resolved) ? Directory.GetFiles(resolved, "*.sql", SearchOption.AllDirectories).Length : 0
                    };
                })
                .ToList();
        }

        private static Dictionary<string, string> ParseHeader(string text)
        {
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var match = Regex.Match(text ?? "", @"/\*(.*?)\*/", RegexOptions.Singleline);
            if (!match.Success)
            {
                return values;
            }

            foreach (var line in match.Groups[1].Value.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None))
            {
                var index = line.IndexOf(':');
                if (index <= 0)
                {
                    continue;
                }

                values[line.Substring(0, index).Trim()] = line.Substring(index + 1).Trim();
            }

            return values;
        }

        private static IEnumerable<MigrationWarning> DetectDangerousSql(string text)
        {
            var sql = StripComments(text ?? "");
            if (Regex.IsMatch(sql, @"\bDROP\s+TABLE\b", RegexOptions.IgnoreCase))
            {
                yield return new MigrationWarning { Severity = "Danger", Code = "DropTable", Message = "Contains DROP TABLE." };
            }
            if (Regex.IsMatch(sql, @"\bTRUNCATE\s+TABLE\b", RegexOptions.IgnoreCase))
            {
                yield return new MigrationWarning { Severity = "Danger", Code = "Truncate", Message = "Contains TRUNCATE TABLE." };
            }
            if (Regex.IsMatch(sql, @"\bALTER\s+TABLE\b[\s\S]{0,250}\bALTER\s+COLUMN\b", RegexOptions.IgnoreCase))
            {
                yield return new MigrationWarning { Severity = "Warning", Code = "AlterColumn", Message = "Contains ALTER COLUMN." };
            }
            if (Regex.IsMatch(sql, @"\bEXEC\s*\(|\bsp_executesql\b", RegexOptions.IgnoreCase))
            {
                yield return new MigrationWarning { Severity = "Warning", Code = "DynamicSql", Message = "Contains dynamic SQL execution." };
            }
            if (HasDmlWithoutWhere(sql, "DELETE"))
            {
                yield return new MigrationWarning { Severity = "Danger", Code = "DeleteWithoutWhere", Message = "Contains DELETE statement without a visible WHERE clause." };
            }
            if (HasDmlWithoutWhere(sql, "UPDATE"))
            {
                yield return new MigrationWarning { Severity = "Danger", Code = "UpdateWithoutWhere", Message = "Contains UPDATE statement without a visible WHERE clause." };
            }
        }

        private static bool HasDmlWithoutWhere(string sql, string keyword)
        {
            foreach (Match match in Regex.Matches(sql, @"\b" + keyword + @"\b[\s\S]*?(?:;|\bGO\b|$)", RegexOptions.IgnoreCase))
            {
                if (!Regex.IsMatch(match.Value, @"\bWHERE\b", RegexOptions.IgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static string DetectUpdateType(string text)
        {
            var sql = StripComments(text ?? "");
            if (Regex.IsMatch(sql, @"\bCREATE\s+PROCEDURE\b|\bALTER\s+PROCEDURE\b|\bDROP\s+PROCEDURE\b", RegexOptions.IgnoreCase)) return "SP";
            if (Regex.IsMatch(sql, @"\bCREATE\s+VIEW\b|\bALTER\s+VIEW\b|\bDROP\s+VIEW\b", RegexOptions.IgnoreCase)) return "View";
            if (Regex.IsMatch(sql, @"\bCREATE\s+(UNIQUE\s+)?INDEX\b|\bDROP\s+INDEX\b", RegexOptions.IgnoreCase)) return "Index";
            if (Regex.IsMatch(sql, @"\bALTER\s+TABLE\b|\bCREATE\s+TABLE\b", RegexOptions.IgnoreCase)) return "Table";
            if (Regex.IsMatch(sql, @"\bINSERT\b|\bUPDATE\b|\bDELETE\b|\bMERGE\b", RegexOptions.IgnoreCase)) return "Data Fix";
            return "SQL";
        }

        private static string StripComments(string text)
        {
            var withoutBlock = Regex.Replace(text, @"/\*.*?\*/", "", RegexOptions.Singleline);
            return Regex.Replace(withoutBlock, @"--.*?$", "", RegexOptions.Multiline);
        }

        private static IList<string> SplitSqlBatches(string sql)
        {
            var batches = new List<string>();
            var buffer = new StringBuilder();
            using (var reader = new StringReader(sql ?? ""))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (Regex.IsMatch(line, @"^\s*GO\s*(--.*)?$", RegexOptions.IgnoreCase))
                    {
                        if (!string.IsNullOrWhiteSpace(buffer.ToString()))
                        {
                            batches.Add(buffer.ToString());
                        }
                        buffer.Clear();
                    }
                    else
                    {
                        buffer.AppendLine(line);
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(buffer.ToString()))
            {
                batches.Add(buffer.ToString());
            }

            return batches;
        }

        private IList<MigrationHistoryViewModel> ReadHistory(SqlConnection connection, int maxRows)
        {
            var rows = new List<MigrationHistoryViewModel>();
            if (!TableExists(connection, "DatabaseMigrationHistory"))
            {
                return rows;
            }

            using (var command = new SqlCommand(@"
SELECT TOP (@MaxRows) MigrationId, ScriptName, ScriptPath, ScriptHash, ModuleName, AppliedOn, AppliedBy,
       MachineName, DatabaseName, DurationMs, Success, ErrorMessage, BatchNo, ReleaseNo
FROM dbo.DatabaseMigrationHistory
ORDER BY MigrationId DESC;", connection))
            {
                command.Parameters.Add("@MaxRows", SqlDbType.Int).Value = maxRows;
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        rows.Add(new MigrationHistoryViewModel
                        {
                            MigrationId = Convert.ToInt32(reader["MigrationId"]),
                            ScriptName = Convert.ToString(reader["ScriptName"]),
                            ScriptPath = Convert.ToString(reader["ScriptPath"]),
                            ScriptHash = Convert.ToString(reader["ScriptHash"]),
                            ModuleName = Convert.ToString(reader["ModuleName"]),
                            AppliedOn = Convert.ToDateTime(reader["AppliedOn"]),
                            AppliedBy = Convert.ToString(reader["AppliedBy"]),
                            MachineName = Convert.ToString(reader["MachineName"]),
                            DatabaseName = Convert.ToString(reader["DatabaseName"]),
                            DurationMs = reader["DurationMs"] == DBNull.Value ? (int?)null : Convert.ToInt32(reader["DurationMs"]),
                            Success = Convert.ToBoolean(reader["Success"]),
                            ErrorMessage = reader["ErrorMessage"] == DBNull.Value ? null : Convert.ToString(reader["ErrorMessage"]),
                            BatchNo = reader["BatchNo"] == DBNull.Value ? null : Convert.ToString(reader["BatchNo"]),
                            ReleaseNo = reader["ReleaseNo"] == DBNull.Value ? null : Convert.ToString(reader["ReleaseNo"])
                        });
                    }
                }
            }

            return rows;
        }

        private IList<MigrationRunViewModel> ReadRuns(SqlConnection connection, int maxRows)
        {
            var rows = new List<MigrationRunViewModel>();
            if (!TableExists(connection, "DatabaseMigrationRun"))
            {
                return rows;
            }

            using (var command = new SqlCommand(@"
SELECT TOP (@MaxRows) RunId, StartedAt, FinishedAt, StartedBy, DatabaseName, ServerName, Mode,
       Status, TotalScripts, AppliedCount, FailedCount, WarningCount
FROM dbo.DatabaseMigrationRun
ORDER BY RunId DESC;", connection))
            {
                command.Parameters.Add("@MaxRows", SqlDbType.Int).Value = maxRows;
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        rows.Add(new MigrationRunViewModel
                        {
                            RunId = Convert.ToInt64(reader["RunId"]),
                            StartedAt = Convert.ToDateTime(reader["StartedAt"]),
                            FinishedAt = reader["FinishedAt"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["FinishedAt"]),
                            StartedBy = Convert.ToString(reader["StartedBy"]),
                            DatabaseName = Convert.ToString(reader["DatabaseName"]),
                            ServerName = Convert.ToString(reader["ServerName"]),
                            Mode = Convert.ToString(reader["Mode"]),
                            Status = Convert.ToString(reader["Status"]),
                            TotalScripts = Convert.ToInt32(reader["TotalScripts"]),
                            AppliedCount = Convert.ToInt32(reader["AppliedCount"]),
                            FailedCount = Convert.ToInt32(reader["FailedCount"]),
                            WarningCount = Convert.ToInt32(reader["WarningCount"])
                        });
                    }
                }
            }

            return rows;
        }

        private void EnsureMetadataTables(SqlConnection connection)
        {
            foreach (var batch in SplitSqlBatches(MetadataSql))
            {
                ExecuteNonQuery(connection, batch, null);
            }
        }

        private static bool TableExists(SqlConnection connection, string tableName)
        {
            using (var command = new SqlCommand("SELECT CASE WHEN OBJECT_ID(N'dbo." + tableName + "', N'U') IS NULL THEN 0 ELSE 1 END", connection))
            {
                return Convert.ToInt32(command.ExecuteScalar()) == 1;
            }
        }

        private static void ExecuteNonQuery(SqlConnection connection, string sql, SqlTransaction transaction)
        {
            using (var command = new SqlCommand(sql, connection, transaction))
            {
                command.CommandTimeout = 0;
                command.ExecuteNonQuery();
            }
        }

        private long InsertRun(SqlConnection connection, string mode, MainErpUserContext user, MigrationRunResult result)
        {
            using (var command = new SqlCommand(@"
INSERT INTO dbo.DatabaseMigrationRun
(
    StartedAt, StartedBy, DatabaseName, ServerName, Mode, Status,
    TotalScripts, AppliedCount, FailedCount, WarningCount
)
VALUES
(
    GETDATE(), @StartedBy, DB_NAME(), @@SERVERNAME, @Mode, @Status,
    @TotalScripts, 0, 0, @WarningCount
);
SELECT CONVERT(BIGINT, SCOPE_IDENTITY());", connection))
            {
                command.Parameters.Add("@StartedBy", SqlDbType.NVarChar, 256).Value = user == null ? "Unknown" : user.UserName;
                command.Parameters.Add("@Mode", SqlDbType.NVarChar, 20).Value = mode;
                command.Parameters.Add("@Status", SqlDbType.NVarChar, 30).Value = result.Status ?? "Started";
                command.Parameters.Add("@TotalScripts", SqlDbType.Int).Value = result.TotalScripts;
                command.Parameters.Add("@WarningCount", SqlDbType.Int).Value = result.WarningCount;
                return Convert.ToInt64(command.ExecuteScalar());
            }
        }

        private static void FinishRun(SqlConnection connection, long runId, string status, int appliedCount, int failedCount, int warningCount)
        {
            using (var command = new SqlCommand(@"
UPDATE dbo.DatabaseMigrationRun
SET FinishedAt = GETDATE(),
    Status = @Status,
    AppliedCount = @AppliedCount,
    FailedCount = @FailedCount,
    WarningCount = @WarningCount
WHERE RunId = @RunId;", connection))
            {
                command.Parameters.Add("@RunId", SqlDbType.BigInt).Value = runId;
                command.Parameters.Add("@Status", SqlDbType.NVarChar, 30).Value = status;
                command.Parameters.Add("@AppliedCount", SqlDbType.Int).Value = appliedCount;
                command.Parameters.Add("@FailedCount", SqlDbType.Int).Value = failedCount;
                command.Parameters.Add("@WarningCount", SqlDbType.Int).Value = warningCount;
                command.ExecuteNonQuery();
            }
        }

        private static void InsertRunDetail(SqlConnection connection, long runId, string scriptName, string moduleName, string status, int? durationMs, string errorMessage, string scriptHash)
        {
            using (var command = new SqlCommand(@"
INSERT INTO dbo.DatabaseMigrationRunDetail
(RunId, ScriptName, ModuleName, Status, DurationMs, ErrorMessage, ScriptHash)
VALUES
(@RunId, @ScriptName, @ModuleName, @Status, @DurationMs, @ErrorMessage, @ScriptHash);", connection))
            {
                command.Parameters.Add("@RunId", SqlDbType.BigInt).Value = runId;
                command.Parameters.Add("@ScriptName", SqlDbType.NVarChar, 260).Value = scriptName;
                command.Parameters.Add("@ModuleName", SqlDbType.NVarChar, 100).Value = moduleName ?? "";
                command.Parameters.Add("@Status", SqlDbType.NVarChar, 30).Value = status;
                command.Parameters.Add("@DurationMs", SqlDbType.Int).Value = durationMs.HasValue ? (object)durationMs.Value : DBNull.Value;
                command.Parameters.Add("@ErrorMessage", SqlDbType.NVarChar).Value = string.IsNullOrWhiteSpace(errorMessage) ? (object)DBNull.Value : errorMessage;
                command.Parameters.Add("@ScriptHash", SqlDbType.Char, 64).Value = scriptHash ?? "";
                command.ExecuteNonQuery();
            }
        }

        private static void InsertHistory(SqlConnection connection, MigrationFileInfoViewModel migration, long durationMs, bool success, string errorMessage, string releaseNo, MainErpUserContext user)
        {
            using (var command = new SqlCommand(@"
INSERT INTO dbo.DatabaseMigrationHistory
(
    ScriptName, ScriptPath, ScriptHash, ModuleName, AppliedOn, AppliedBy,
    MachineName, DatabaseName, DurationMs, Success, ErrorMessage, BatchNo, ReleaseNo
)
VALUES
(
    @ScriptName, @ScriptPath, @ScriptHash, @ModuleName, GETDATE(), @AppliedBy,
    HOST_NAME(), DB_NAME(), @DurationMs, @Success, @ErrorMessage, NULL, @ReleaseNo
);", connection))
            {
                command.Parameters.Add("@ScriptName", SqlDbType.NVarChar, 260).Value = migration.ScriptName;
                command.Parameters.Add("@ScriptPath", SqlDbType.NVarChar, 1000).Value = migration.ScriptPath;
                command.Parameters.Add("@ScriptHash", SqlDbType.Char, 64).Value = migration.ScriptHash;
                command.Parameters.Add("@ModuleName", SqlDbType.NVarChar, 100).Value = migration.ModuleName ?? "";
                command.Parameters.Add("@AppliedBy", SqlDbType.NVarChar, 256).Value = user == null ? "Unknown" : user.UserName;
                command.Parameters.Add("@DurationMs", SqlDbType.Int).Value = Convert.ToInt32(Math.Min(durationMs, int.MaxValue));
                command.Parameters.Add("@Success", SqlDbType.Bit).Value = success;
                command.Parameters.Add("@ErrorMessage", SqlDbType.NVarChar).Value = string.IsNullOrWhiteSpace(errorMessage) ? (object)DBNull.Value : errorMessage;
                command.Parameters.Add("@ReleaseNo", SqlDbType.NVarChar, 100).Value = string.IsNullOrWhiteSpace(releaseNo) ? (object)DBNull.Value : releaseNo.Trim();
                command.ExecuteNonQuery();
            }
        }

        private static string ResolveConfiguredPath(string configured)
        {
            if (configured.StartsWith("~/", StringComparison.Ordinal))
            {
                return HttpContext.Current.Server.MapPath(configured);
            }

            return Path.IsPathRooted(configured)
                ? configured
                : HttpContext.Current.Server.MapPath("~/" + configured.TrimStart('~', '/', '\\'));
        }

        private static string ToAppRelativePath(string fullPath)
        {
            var root = HttpContext.Current.Server.MapPath("~/");
            var normalizedRoot = Path.GetFullPath(root).TrimEnd('\\') + "\\";
            var normalizedPath = Path.GetFullPath(fullPath);
            if (normalizedPath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase))
            {
                return normalizedPath.Substring(normalizedRoot.Length).Replace('\\', '/');
            }

            return normalizedPath;
        }

        private static string ResolveFullPath(string scriptPath)
        {
            if (Path.IsPathRooted(scriptPath))
            {
                return Path.GetFullPath(scriptPath);
            }

            return Path.GetFullPath(HttpContext.Current.Server.MapPath("~/" + scriptPath.TrimStart('/', '\\')));
        }

        private bool IsUnderAllowedSource(string fullPath)
        {
            var normalized = Path.GetFullPath(fullPath);
            return GetSources().Where(x => x.Exists).Any(x =>
            {
                var source = Path.GetFullPath(x.ResolvedPath).TrimEnd('\\') + "\\";
                return normalized.StartsWith(source, StringComparison.OrdinalIgnoreCase);
            });
        }

        private static int? ParseMigrationNumber(string fileName)
        {
            var match = Regex.Match(fileName ?? "", @"^(\d{4})_");
            return match.Success ? Convert.ToInt32(match.Groups[1].Value) : (int?)null;
        }

        private static string InferModule(string fileName, string path)
        {
            var combined = (fileName + " " + path).ToUpperInvariant();
            if (combined.Contains("_POS_") || combined.Contains("/POS/")) return "POS";
            if (combined.Contains("_MAINERP_") || combined.Contains("/MAINERP/")) return "MainErp";
            if (combined.Contains("_SYNC_") || combined.Contains("/SYNC/")) return "Sync";
            if (combined.Contains("_REPORT") || combined.Contains("/REPORTS/")) return "Reports";
            return "Shared";
        }

        private static string Sha256File(string path)
        {
            using (var sha = SHA256.Create())
            using (var stream = File.OpenRead(path))
            {
                return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToUpperInvariant();
            }
        }

        private static string Sha256Text(string text)
        {
            using (var sha = SHA256.Create())
            {
                return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(text ?? ""))).Replace("-", "").ToUpperInvariant();
            }
        }

        private static string SafeServerName(SqlConnection connection)
        {
            using (var command = new SqlCommand("SELECT CONVERT(NVARCHAR(128), @@SERVERNAME)", connection))
            {
                return Convert.ToString(command.ExecuteScalar());
            }
        }

        private static string SafeError(string error)
        {
            if (string.IsNullOrWhiteSpace(error))
            {
                return "";
            }

            var sanitized = Regex.Replace(error, @"(?i)(password\s*=\s*)[^;]+", "$1***");
            sanitized = Regex.Replace(sanitized, @"(?i)(user\s+id\s*=\s*)[^;]+", "$1***");
            return sanitized.Length > 500 ? sanitized.Substring(0, 500) : sanitized;
        }

        private static void AppendCsv(StringBuilder builder, params string[] values)
        {
            builder.AppendLine(string.Join(",", values.Select(EscapeCsv)));
        }

        private static string EscapeCsv(string value)
        {
            value = value ?? "";
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        private const string MetadataSql = @"
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;

IF OBJECT_ID(N'dbo.DatabaseMigrationHistory', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DatabaseMigrationHistory
    (
        MigrationId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_DatabaseMigrationHistory PRIMARY KEY,
        ScriptName NVARCHAR(260) NOT NULL,
        ScriptPath NVARCHAR(1000) NOT NULL,
        ScriptHash CHAR(64) NOT NULL,
        ModuleName NVARCHAR(100) NOT NULL,
        AppliedOn DATETIME NOT NULL CONSTRAINT DF_DatabaseMigrationHistory_AppliedOn DEFAULT (GETDATE()),
        AppliedBy NVARCHAR(256) NOT NULL,
        MachineName NVARCHAR(128) NOT NULL,
        DatabaseName SYSNAME NOT NULL,
        DurationMs INT NULL,
        Success BIT NOT NULL,
        ErrorMessage NVARCHAR(MAX) NULL,
        BatchNo NVARCHAR(100) NULL,
        ReleaseNo NVARCHAR(100) NULL
    );
END;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_DatabaseMigrationHistory_ScriptName_ScriptHash_Success' AND object_id = OBJECT_ID(N'dbo.DatabaseMigrationHistory', N'U'))
BEGIN
    CREATE UNIQUE INDEX UX_DatabaseMigrationHistory_ScriptName_ScriptHash_Success
    ON dbo.DatabaseMigrationHistory (ScriptName, ScriptHash, Success)
    WHERE Success = 1;
END;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_DatabaseMigrationHistory_ModuleName_AppliedOn' AND object_id = OBJECT_ID(N'dbo.DatabaseMigrationHistory', N'U'))
BEGIN
    CREATE INDEX IX_DatabaseMigrationHistory_ModuleName_AppliedOn
    ON dbo.DatabaseMigrationHistory (ModuleName, AppliedOn);
END;
GO
IF OBJECT_ID(N'dbo.DatabaseMigrationRun', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DatabaseMigrationRun
    (
        RunId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_DatabaseMigrationRun PRIMARY KEY,
        StartedAt DATETIME NOT NULL CONSTRAINT DF_DatabaseMigrationRun_StartedAt DEFAULT (GETDATE()),
        FinishedAt DATETIME NULL,
        StartedBy NVARCHAR(256) NOT NULL,
        DatabaseName SYSNAME NOT NULL,
        ServerName NVARCHAR(128) NOT NULL,
        Mode NVARCHAR(20) NOT NULL,
        Status NVARCHAR(30) NOT NULL,
        TotalScripts INT NOT NULL CONSTRAINT DF_DatabaseMigrationRun_TotalScripts DEFAULT (0),
        AppliedCount INT NOT NULL CONSTRAINT DF_DatabaseMigrationRun_AppliedCount DEFAULT (0),
        FailedCount INT NOT NULL CONSTRAINT DF_DatabaseMigrationRun_FailedCount DEFAULT (0),
        WarningCount INT NOT NULL CONSTRAINT DF_DatabaseMigrationRun_WarningCount DEFAULT (0)
    );
END;
GO
IF OBJECT_ID(N'dbo.DatabaseMigrationRunDetail', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DatabaseMigrationRunDetail
    (
        RunDetailId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_DatabaseMigrationRunDetail PRIMARY KEY,
        RunId BIGINT NOT NULL,
        ScriptName NVARCHAR(260) NOT NULL,
        ModuleName NVARCHAR(100) NOT NULL,
        Status NVARCHAR(30) NOT NULL,
        DurationMs INT NULL,
        ErrorMessage NVARCHAR(MAX) NULL,
        ScriptHash CHAR(64) NOT NULL,
        CONSTRAINT FK_DatabaseMigrationRunDetail_Run FOREIGN KEY (RunId)
            REFERENCES dbo.DatabaseMigrationRun(RunId)
    );
END;
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_DatabaseMigrationRunDetail_RunId' AND object_id = OBJECT_ID(N'dbo.DatabaseMigrationRunDetail', N'U'))
BEGIN
    CREATE INDEX IX_DatabaseMigrationRunDetail_RunId
    ON dbo.DatabaseMigrationRunDetail (RunId, RunDetailId);
END;
GO";
    }
}

