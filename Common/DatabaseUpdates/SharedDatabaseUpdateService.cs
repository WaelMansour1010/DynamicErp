using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Entity.Core.EntityClient;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using MyERP.Models;

namespace MyERP.Common.DatabaseUpdates
{
    public class SharedDatabaseUpdateService
    {
        private const string HistoryTableName = "dbo.DatabaseMigrationHistory";
        private const string SharedMigrationVirtualPath = "~/Database/Migrations/Shared";

        public SharedDatabaseUpdateDashboard BuildDashboard(MySoftERPEntity db, string message = null, bool isError = false)
        {
            using (var connection = OpenSqlConnection(db))
            {
                EnsureHistoryTable(connection);
                var scripts = LoadScripts(connection);
                return new SharedDatabaseUpdateDashboard
                {
                    DatabaseName = connection.Database,
                    ServerName = Convert.ToString(ExecuteScalar(connection, "SELECT CONVERT(NVARCHAR(128), @@SERVERNAME)")),
                    PendingCount = scripts.Count(s => s.IsPending),
                    AppliedCount = scripts.Count(s => string.Equals(s.Status, "Applied", StringComparison.OrdinalIgnoreCase)),
                    HashMismatchCount = scripts.Count(s => s.HasHashMismatch),
                    Message = message,
                    IsError = isError,
                    Scripts = scripts,
                    RecentHistory = ReadHistory(connection).Take(25).ToList()
                };
            }
        }

        public SharedDatabaseUpdateApplyResult ApplyPending(MySoftERPEntity db, string userName)
        {
            using (var connection = OpenSqlConnection(db))
            {
                EnsureHistoryTable(connection);
                var pending = LoadScripts(connection)
                    .Where(s => s.IsPending && !s.HasHashMismatch)
                    .OrderBy(s => s.ScriptName, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                var applied = 0;
                var failed = 0;

                foreach (var script in pending)
                {
                    var transaction = connection.BeginTransaction();
                    try
                    {
                        var sql = File.ReadAllText(MapPath("~/" + script.RelativePath), Encoding.UTF8);
                        foreach (var batch in SplitSqlBatches(sql))
                        {
                            using (var command = new SqlCommand(batch, connection, transaction))
                            {
                                command.CommandTimeout = 0;
                                command.ExecuteNonQuery();
                            }
                        }

                        transaction.Commit();
                        InsertHistory(connection, script, userName, true, null);
                        applied++;
                    }
                    catch (Exception ex)
                    {
                        try { transaction.Rollback(); } catch { }
                        InsertHistory(connection, script, userName, false, SafeError(ex));
                        failed++;
                        return new SharedDatabaseUpdateApplyResult
                        {
                            Success = false,
                            AppliedCount = applied,
                            FailedCount = failed,
                            Message = "فشل تنفيذ السكربت: " + script.ScriptName + ". تم إيقاف التنفيذ وتسجيل الخطأ."
                        };
                    }
                    finally
                    {
                        transaction.Dispose();
                    }
                }

                return new SharedDatabaseUpdateApplyResult
                {
                    Success = true,
                    AppliedCount = applied,
                    FailedCount = failed,
                    Message = applied == 0 ? "لا توجد تحديثات مشتركة منتظرة." : "تم تطبيق التحديثات المشتركة بنجاح. العدد: " + applied
                };
            }
        }

        private static IList<SharedDatabaseUpdateScript> LoadScripts(SqlConnection connection)
        {
            var root = MapPath(SharedMigrationVirtualPath);
            var history = ReadHistory(connection).Where(h => h.Success).ToList();
            var result = new List<SharedDatabaseUpdateScript>();
            if (!Directory.Exists(root))
            {
                return result;
            }

            foreach (var file in Directory.GetFiles(root, "*.sql", SearchOption.TopDirectoryOnly).OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase))
            {
                var scriptName = Path.GetFileName(file);
                var hash = GetSha256(file);
                var matching = history.FirstOrDefault(h => string.Equals(h.ScriptName, scriptName, StringComparison.OrdinalIgnoreCase) && string.Equals(h.ScriptHash, hash, StringComparison.OrdinalIgnoreCase));
                var previous = history.Any(h => string.Equals(h.ScriptName, scriptName, StringComparison.OrdinalIgnoreCase));
                var hashMismatch = previous && matching == null;
                var text = File.ReadAllText(file, Encoding.UTF8);

                result.Add(new SharedDatabaseUpdateScript
                {
                    ScriptName = scriptName,
                    RelativePath = "Database/Migrations/Shared/" + scriptName,
                    Hash = hash,
                    Status = hashMismatch ? "HashMismatch" : (matching != null ? "Applied" : "Pending"),
                    IsPending = matching == null && !hashMismatch,
                    HasHashMismatch = hashMismatch,
                    LastModifiedOn = File.GetLastWriteTime(file),
                    AppliedOn = matching != null ? matching.AppliedOn : (DateTime?)null,
                    AppliedBy = matching != null ? matching.AppliedBy : string.Empty,
                    Purpose = ParsePurpose(text)
                });
            }

            return result;
        }

        private static void EnsureHistoryTable(SqlConnection connection)
        {
            ExecuteNonQuery(connection, @"
IF OBJECT_ID(N'dbo.DatabaseMigrationHistory', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DatabaseMigrationHistory
    (
        MigrationId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_DatabaseMigrationHistory PRIMARY KEY,
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
END;");
        }

        private static IList<SharedDatabaseUpdateHistory> ReadHistory(SqlConnection connection)
        {
            var rows = new List<SharedDatabaseUpdateHistory>();
            EnsureHistoryTable(connection);

            using (var command = new SqlCommand(@"
SELECT ScriptName, ScriptHash, AppliedOn, AppliedBy, Success, ErrorMessage
FROM dbo.DatabaseMigrationHistory
WHERE ModuleName = N'Shared'
ORDER BY MigrationId DESC;", connection))
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    rows.Add(new SharedDatabaseUpdateHistory
                    {
                        ScriptName = Convert.ToString(reader["ScriptName"]),
                        ScriptHash = Convert.ToString(reader["ScriptHash"]),
                        AppliedOn = Convert.ToDateTime(reader["AppliedOn"]),
                        AppliedBy = Convert.ToString(reader["AppliedBy"]),
                        Success = Convert.ToBoolean(reader["Success"]),
                        ErrorMessage = reader["ErrorMessage"] == DBNull.Value ? string.Empty : Convert.ToString(reader["ErrorMessage"])
                    });
                }
            }

            return rows;
        }

        private static void InsertHistory(SqlConnection connection, SharedDatabaseUpdateScript script, string userName, bool success, string error)
        {
            using (var command = new SqlCommand(@"
INSERT dbo.DatabaseMigrationHistory
(ScriptName, ScriptPath, ScriptHash, ModuleName, AppliedBy, MachineName, DatabaseName, DurationMs, Success, ErrorMessage, ReleaseNo)
VALUES
(@ScriptName, @ScriptPath, @ScriptHash, N'Shared', @AppliedBy, @MachineName, DB_NAME(), NULL, @Success, @ErrorMessage, N'MAIN_WEB_SHARED_UPDATES');", connection))
            {
                command.Parameters.Add("@ScriptName", SqlDbType.NVarChar, 260).Value = script.ScriptName;
                command.Parameters.Add("@ScriptPath", SqlDbType.NVarChar, 1000).Value = script.RelativePath;
                command.Parameters.Add("@ScriptHash", SqlDbType.Char, 64).Value = script.Hash;
                command.Parameters.Add("@AppliedBy", SqlDbType.NVarChar, 256).Value = string.IsNullOrWhiteSpace(userName) ? Environment.UserName : userName;
                command.Parameters.Add("@MachineName", SqlDbType.NVarChar, 128).Value = Environment.MachineName;
                command.Parameters.Add("@Success", SqlDbType.Bit).Value = success;
                command.Parameters.Add("@ErrorMessage", SqlDbType.NVarChar, -1).Value = string.IsNullOrWhiteSpace(error) ? (object)DBNull.Value : error;
                command.ExecuteNonQuery();
            }
        }

        private static SqlConnection OpenSqlConnection(MySoftERPEntity db)
        {
            var connectionString = db.Database.Connection.ConnectionString;
            if (connectionString.StartsWith("name=", StringComparison.OrdinalIgnoreCase))
            {
                var name = connectionString.Substring(5).Trim();
                var configured = ConfigurationManager.ConnectionStrings[name];
                if (configured != null)
                {
                    connectionString = configured.ConnectionString;
                }
            }

            if (connectionString.IndexOf("metadata=", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                var builder = new EntityConnectionStringBuilder(connectionString);
                connectionString = builder.ProviderConnectionString;
            }

            var connection = new SqlConnection(connectionString);
            connection.Open();
            return connection;
        }

        private static IEnumerable<string> SplitSqlBatches(string sql)
        {
            return Regex.Split(sql ?? string.Empty, @"^\s*GO\s*;?\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase)
                .Select(batch => batch.Trim())
                .Where(batch => !string.IsNullOrWhiteSpace(batch));
        }

        private static string ParsePurpose(string text)
        {
            var match = Regex.Match(text ?? string.Empty, @"Purpose:\s*(.+)", RegexOptions.IgnoreCase);
            return match.Success ? match.Groups[1].Value.Trim() : string.Empty;
        }

        private static string GetSha256(string path)
        {
            using (var sha = SHA256.Create())
            using (var stream = File.OpenRead(path))
            {
                return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty).ToLowerInvariant();
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

        private static object ExecuteScalar(SqlConnection connection, string sql)
        {
            using (var command = new SqlCommand(sql, connection))
            {
                return command.ExecuteScalar();
            }
        }

        private static string MapPath(string virtualPath)
        {
            if (HttpContext.Current != null && HttpContext.Current.Server != null)
            {
                return HttpContext.Current.Server.MapPath(virtualPath);
            }

            return Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, virtualPath.TrimStart('~', '/', '\\').Replace('/', Path.DirectorySeparatorChar)));
        }

        private static string SafeError(Exception ex)
        {
            return ex == null ? string.Empty : ex.Message;
        }
    }
}
