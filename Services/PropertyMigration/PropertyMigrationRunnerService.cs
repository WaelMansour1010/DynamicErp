using MyERP.Models.PropertyMigration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace MyERP.Services.PropertyMigration
{
    public class PropertyMigrationRunnerService
    {
        private static readonly string[] SafeTargetMarkers = { "Clone", "Sandbox", "PropertyPilot", "ReadyToTest", "PilotClone", "Migration" };
        private static readonly string[] UnsafeTargetMarkers = { "Production", "Prod", "Live", "GoLive" };
        private static readonly string[] BlockedTargetNames = { "Alromaizan", "MyErp", "Adnan", "RSMDB" };

        private readonly string _applicationRoot;

        public PropertyMigrationRunnerService()
            : this(HttpRuntime.AppDomainAppPath)
        {
        }

        public PropertyMigrationRunnerService(string applicationRoot)
        {
            _applicationRoot = string.IsNullOrWhiteSpace(applicationRoot)
                ? AppDomain.CurrentDomain.BaseDirectory
                : applicationRoot;
        }

        public PropertyMigrationRunnerResult Run(PropertyMigrationRunnerRequest request, bool dryRunOnly)
        {
            var result = new PropertyMigrationRunnerResult();
            request = request ?? new PropertyMigrationRunnerRequest();

            var validationErrors = Validate(request, dryRunOnly);
            foreach (var error in validationErrors)
            {
                result.ValidationErrors.Add(error);
            }

            if (result.ValidationErrors.Count > 0)
            {
                result.Success = false;
                result.Status = "ValidationFailed";
                result.Message = string.Join(Environment.NewLine, result.ValidationErrors);
                return result;
            }

            var batchId = NormalizeBatchId(request.BatchId, request.ExecuteMode && !dryRunOnly);
            result.BatchId = batchId;

            var configPath = WriteTemporaryConfig(request, batchId, dryRunOnly || !request.ExecuteMode);
            result.ConfigPath = configPath;

            var runnerProjectPath = GetRunnerProjectPath();
            if (!File.Exists(runnerProjectPath))
            {
                result.Success = false;
                result.Status = "RunnerMissing";
                result.Message = "Property Migration Runner project was not found: " + runnerProjectPath;
                return result;
            }

            var args = "run --project \"" + runnerProjectPath + "\" -- --config \"" + configPath + "\" " + ((dryRunOnly || !request.ExecuteMode) ? "--dry-run" : "--execute");
            var psi = new ProcessStartInfo("dotnet", args)
            {
                WorkingDirectory = Path.GetDirectoryName(runnerProjectPath),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using (var process = Process.Start(psi))
            {
                if (process == null)
                {
                    result.Success = false;
                    result.Status = "ProcessStartFailed";
                    result.Message = "Unable to start dotnet runner process.";
                    return result;
                }

                var output = process.StandardOutput.ReadToEnd();
                var error = process.StandardError.ReadToEnd();
                if (!process.WaitForExit(300000))
                {
                    try { process.Kill(); } catch { }
                    result.Success = false;
                    result.Status = "TimedOut";
                    result.Message = "Runner timed out after 5 minutes.";
                    result.StandardOutput = output;
                    result.StandardError = error;
                    return result;
                }

                result.ExitCode = process.ExitCode;
                result.StandardOutput = output;
                result.StandardError = error;
                result.ReportPath = ExtractReportPath(output + Environment.NewLine + error);
                result.Success = process.ExitCode == 0;
                result.WasExecuted = request.ExecuteMode && !dryRunOnly;
                result.Status = result.Success ? "Completed" : "Failed";
                result.Message = result.Success ? "Runner completed." : "Runner failed. Review output and report.";
                return result;
            }
        }

        public string GetReportsDirectory()
        {
            return Path.Combine(_applicationRoot, "Tools", "DynamicErp.PropertyMigration.Runner", "Reports");
        }

        public string GetToolkitDocsDirectory()
        {
            return Path.Combine(_applicationRoot, "Docs", "PropertyMigrationToolkit");
        }

        public string GetLatestReportPath()
        {
            var reportsDirectory = GetReportsDirectory();
            if (!Directory.Exists(reportsDirectory))
            {
                return null;
            }

            return Directory.GetFiles(reportsDirectory, "*.md")
                .OrderByDescending(File.GetLastWriteTimeUtc)
                .FirstOrDefault();
        }

        public IEnumerable<string> GetDefaultSourceDatabases()
        {
            return new[] { "Adnan", "RSMDB" };
        }

        public IEnumerable<string> GetDefaultTargetDatabases()
        {
            return new[]
            {
                "Alromaizan_PropertyPilot_Adnan_ReadyToTest_20260520",
                "Alromaizan_PropertyPilot_RSMDB_StagingClone_20260520"
            };
        }

        private IEnumerable<string> Validate(PropertyMigrationRunnerRequest request, bool dryRunOnly)
        {
            if (string.IsNullOrWhiteSpace(request.SourceDatabaseName))
            {
                yield return "Source database is required.";
            }

            if (string.IsNullOrWhiteSpace(request.TargetCloneDatabaseName))
            {
                yield return "Target clone database is required.";
            }

            if (!string.IsNullOrWhiteSpace(request.SourceDatabaseName)
                && string.Equals(request.SourceDatabaseName.Trim(), request.TargetCloneDatabaseName != null ? request.TargetCloneDatabaseName.Trim() : null, StringComparison.OrdinalIgnoreCase))
            {
                yield return "Source and Target must be different.";
            }

            if (!IsSafeTargetName(request.TargetCloneDatabaseName))
            {
                yield return "Target database must be an isolated clone and include Clone, Sandbox, PropertyPilot, ReadyToTest, PilotClone, or Migration.";
            }

            if (BlockedTargetNames.Any(n => string.Equals(n, request.TargetCloneDatabaseName, StringComparison.OrdinalIgnoreCase)))
            {
                yield return "Target database is a blocked production/source name.";
            }

            if (UnsafeTargetMarkers.Any(m => Contains(request.TargetCloneDatabaseName, m)))
            {
                yield return "Target database contains a production/live marker.";
            }

            if (!IsValidMode(request.MigrationMode))
            {
                yield return "Migration mode must be Strict, Tolerant, or Hybrid.";
            }

            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("PROPERTY_MIGRATION_SQL_CONNECTION")))
            {
                yield return "Environment variable PROPERTY_MIGRATION_SQL_CONNECTION is required. Passwords are not accepted from the web screen.";
            }

            if (request.ExecuteMode && !dryRunOnly)
            {
                if (!request.BackupVerified)
                {
                    yield return "Execute requires BackupVerified=true.";
                }

                if (!request.ExecutionPlanApproved)
                {
                    yield return "Execute requires ExecutionPlanApproved=true.";
                }

                if (string.IsNullOrWhiteSpace(request.BatchId))
                {
                    yield return "Execute requires an explicit BatchId.";
                }

                if (!string.Equals((request.ExecuteConfirmation ?? string.Empty).Trim(), "EXECUTE CLONE", StringComparison.OrdinalIgnoreCase))
                {
                    yield return "Execute requires confirmation text: EXECUTE CLONE.";
                }
            }
        }

        private string WriteTemporaryConfig(PropertyMigrationRunnerRequest request, string batchId, bool dryRun)
        {
            var reportDirectory = GetReportsDirectory();
            Directory.CreateDirectory(reportDirectory);

            var tempDirectory = Path.Combine(Path.GetTempPath(), "DynamicErp.PropertyMigration");
            Directory.CreateDirectory(tempDirectory);

            var config = new Dictionary<string, object>
            {
                { "CustomerCode", string.IsNullOrWhiteSpace(request.CustomerCode) ? "LOCAL-DEBUG" : request.CustomerCode.Trim() },
                { "SourceDatabaseName", request.SourceDatabaseName.Trim() },
                { "TargetCloneDatabaseName", request.TargetCloneDatabaseName.Trim() },
                { "ConnectionString", string.Empty },
                { "ConnectionStringEnvironmentVariable", "PROPERTY_MIGRATION_SQL_CONNECTION" },
                { "MigrationMode", string.IsNullOrWhiteSpace(request.MigrationMode) ? "Hybrid" : request.MigrationMode.Trim() },
                { "BatchId", batchId },
                { "CutoffDate", (request.CutoffDate ?? DateTime.Today).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) },
                { "DryRun", dryRun },
                { "IncludeContracts", request.IncludeOperationalMigration },
                { "IncludeInstallments", request.IncludeOperationalMigration },
                { "IncludeOpeningBalance", request.IncludeOperationalMigration },
                { "IncludeAdvancePayments", request.IncludeAdvancePayments },
                { "IncludeAccounting", request.IncludeAccounting },
                { "IncludeHistoricalReceipts", request.IncludeHistoricalReceipts },
                { "IncludeIssues", request.IncludeIssues },
                { "IncludeOwnerPayments", request.IncludeOwnerPayments },
                { "IncludeJournalEntries", request.IncludeAccounting },
                { "IncludeTerminations", request.IncludeTerminations },
                { "ExcludeUnsafePayments", true },
                { "BackupVerified", request.BackupVerified },
                { "ExecutionPlanApproved", request.ExecutionPlanApproved },
                { "FinanceSignOff", false },
                { "SkipCustomerSpecificMigrationTemplates", dryRun },
                { "SkipStages", BuildSkipStages(request.Stage, dryRun) },
                { "CommandTimeoutSeconds", 120 },
                { "ReportDirectory", reportDirectory },
                { "ToolkitSqlPath", Path.Combine(_applicationRoot, "Docs", "PropertyMigrationToolkit", "Sql") },
                { "IncludeIntelligence", IsStage(request.Stage, "Intelligence") },
                { "IncludeAccountIntelligence", IsStage(request.Stage, "AccountIntelligence") },
                { "IncludeFinanceApproval", IsStage(request.Stage, "FinanceApprovalSimulation") },
                { "IncludeCashingType8FinanceApproval", IsStage(request.Stage, "FinanceApprovalSimulation") },
                { "IncludeMiniAccountingPilotExecute", IsStage(request.Stage, "MiniAccountingPilot") && request.IncludeAccounting && !dryRun },
                { "IncludeMiniAccountingPilotRollback", IsStage(request.Stage, "Rollback") && !dryRun },
                { "AutoFix", new Dictionary<string, object>
                    {
                        { "AllowUnknownUnits", true },
                        { "AllowUnknownProperties", true },
                        { "AllowSuspenseAccounts", false },
                        { "AllowFallbackPaymentMethods", true },
                        { "AllowDefaultCashBox", true },
                        { "AllowDefaultBank", true },
                        { "AllowTemporaryRenterAccounts", true },
                        { "AutoCreateMissingAccounts", false },
                        { "AutoCreateMissingLookups", true }
                    }
                }
            };

            var path = Path.Combine(tempDirectory, "property-migration-" + DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture) + ".json");
            File.WriteAllText(path, JsonConvert.SerializeObject(config, Formatting.Indented), Encoding.UTF8);
            return path;
        }

        private static string[] BuildSkipStages(string stage, bool dryRun)
        {
            if (dryRun || IsStage(stage, "DryRunPreflight"))
            {
                return new[] { "Migration", "Reconciliation", "ReadyToTestDelivery" };
            }

            if (IsStage(stage, "Discovery"))
            {
                return new[] { "Migration", "Reconciliation", "ReadyToTestDelivery" };
            }

            if (IsStage(stage, "Staging"))
            {
                return new[] { "Migration", "Reconciliation", "ReadyToTestDelivery" };
            }

            return new string[0];
        }

        private string GetRunnerProjectPath()
        {
            return Path.Combine(_applicationRoot, "Tools", "DynamicErp.PropertyMigration.Runner", "DynamicErp.PropertyMigration.Runner.csproj");
        }

        private static string NormalizeBatchId(string value, bool requireExisting)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }

            return requireExisting ? string.Empty : Guid.NewGuid().ToString();
        }

        private static bool IsSafeTargetName(string value)
        {
            return !string.IsNullOrWhiteSpace(value) && SafeTargetMarkers.Any(m => Contains(value, m));
        }

        private static bool IsValidMode(string value)
        {
            return string.Equals(value, "Strict", StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, "Tolerant", StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, "Hybrid", StringComparison.OrdinalIgnoreCase)
                || string.IsNullOrWhiteSpace(value);
        }

        private static bool Contains(string value, string marker)
        {
            return !string.IsNullOrWhiteSpace(value) && value.IndexOf(marker, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsStage(string value, string expected)
        {
            return string.Equals(value, expected, StringComparison.OrdinalIgnoreCase);
        }

        private static string ExtractReportPath(string output)
        {
            if (string.IsNullOrWhiteSpace(output))
            {
                return null;
            }

            var match = Regex.Match(output, @"Report:\s*(?<path>.+\.md)", RegexOptions.IgnoreCase);
            return match.Success ? match.Groups["path"].Value.Trim() : null;
        }
    }
}
