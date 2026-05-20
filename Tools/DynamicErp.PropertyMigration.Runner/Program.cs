using System.Data;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Data.SqlClient;

namespace DynamicErp.PropertyMigration.Runner;

internal static class Program
{
    private static readonly string[] SafeTargetMarkers = ["Clone", "Sandbox", "PropertyPilot", "ReadyToTest", "PilotClone", "Migration"];
    private static readonly HashSet<string> BlockedDatabaseNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "Alromaizan", "MyErp", "Adnan", "RSMDB"
    };

    private static async Task<int> Main(string[] args)
    {
        var startedAt = DateTimeOffset.Now;
        var options = RunnerOptions.Parse(args);
        if (options.ShowHelp)
        {
            Console.WriteLine(RunnerOptions.HelpText);
            return 0;
        }

        if (string.IsNullOrWhiteSpace(options.ConfigPath))
        {
            Console.Error.WriteLine("Missing --config <path>. Use --help for usage.");
            return 2;
        }

        MigrationRunReport? report = null;
        try
        {
            var config = await MigrationRunnerConfig.LoadAsync(options.ConfigPath);
            config.ApplyCliOverrides(options);
            report = new MigrationRunReport(config, startedAt, options.Execute ? "Execute" : "DryRun");

            var runner = new MigrationRunner(config, options, report);
            await runner.RunAsync();

            report.CompletedAt = DateTimeOffset.Now;
            report.Status = report.Errors.Count == 0 ? "Completed" : "CompletedWithErrors";
            var reportPath = await report.WriteAsync(config.GetReportDirectory());

            Console.WriteLine();
            Console.WriteLine("Property Migration Runner finished.");
            Console.WriteLine($"Status: {report.Status}");
            Console.WriteLine($"Report: {reportPath}");
            return report.Errors.Count == 0 ? 0 : 1;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("Runner failed: " + ex.Message);
            if (report != null)
            {
                report.Status = "Failed";
                report.Errors.Add(new RunMessage("Runner", "Fatal", ex.Message));
                report.CompletedAt = DateTimeOffset.Now;
                var reportPath = await report.WriteAsync(report.Config.GetReportDirectory());
                Console.Error.WriteLine("Failure report: " + reportPath);
            }
            return 1;
        }
    }

    private sealed class MigrationRunner
    {
        private readonly MigrationRunnerConfig _config;
        private readonly RunnerOptions _options;
        private readonly MigrationRunReport _report;
        private readonly string _toolkitSqlPath;

        public MigrationRunner(MigrationRunnerConfig config, RunnerOptions options, MigrationRunReport report)
        {
            _config = config;
            _options = options;
            _report = report;
            _toolkitSqlPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "Docs", "PropertyMigrationToolkit", "Sql"));
            if (!Directory.Exists(_toolkitSqlPath) && !string.IsNullOrWhiteSpace(config.ToolkitSqlPath))
            {
                _toolkitSqlPath = Path.GetFullPath(config.ToolkitSqlPath);
            }
        }

        public async Task RunAsync()
        {
            AddInfo("Runner", $"Mode={_config.MigrationMode}, Execution={(_options.Execute ? "Execute" : "DryRun")}, BatchId={_config.BatchId}");

            await RunStepAsync("Preflight", PreflightAsync);
            if (!_options.Execute || _config.DryRun)
            {
                await RunStepAsync("ReadOnlySafetyValidation", ReadOnlySafetyValidationAsync);
                await RunStepAsync("ExecutionPlan", DryRunPlanAsync);
                await RunStepAsync("ReadyToTestDelivery", ReadyToTestDeliveryAsync);
                return;
            }

            await RunStepAsync("CoreSetup", EnsureCoreTablesAsync);
            await RunStepAsync("Discovery", () => RunSqlTemplateAsync("Discovery_SELECT_ONLY_Generic.sql", readOnly: true));
            await RunStepAsync("Diagnostics", () => RunSqlTemplateAsync("Diagnostics_Generic.sql", readOnly: false));
            await RunStepAsync("MappingValidation", MappingValidationAsync);
            if (_config.IncludeIntelligence)
            {
                await RunStepAsync("IntelligenceDiscovery", IntelligenceAsync);
            }
            if (_config.IncludeAccountIntelligence)
            {
                await RunStepAsync("AccountDiscovery", AccountIntelligenceAsync);
            }
            await RunStepAsync("Migration", MigrationAsync);
            await RunStepAsync("Reconciliation", () => RunSqlTemplateAsync("Reconciliation_Generic.sql", readOnly: true));
            await RunStepAsync("ReadyToTestDelivery", ReadyToTestDeliveryAsync);
        }

        private async Task RunStepAsync(string stepName, Func<Task> action)
        {
            if (_config.SkipStages.Contains(stepName, StringComparer.OrdinalIgnoreCase))
            {
                _report.StepResults.Add(new StepResult(stepName, "Skipped", DateTimeOffset.Now, DateTimeOffset.Now, "Skipped by config."));
                AddInfo(stepName, "Skipped by config.");
                return;
            }

            var start = DateTimeOffset.Now;
            _report.StepResults.Add(new StepResult(stepName, "Started", start, null, null));
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {stepName}...");
            try
            {
                await action();
                _report.StepResults.Add(new StepResult(stepName, "Passed", start, DateTimeOffset.Now, null));
            }
            catch (Exception ex)
            {
                _report.StepResults.Add(new StepResult(stepName, "Failed", start, DateTimeOffset.Now, ex.Message));
                _report.Errors.Add(new RunMessage(stepName, "Error", ex.Message));
                throw;
            }
        }

        private async Task PreflightAsync()
        {
            if (string.IsNullOrWhiteSpace(_config.ConnectionString))
            {
                throw new InvalidOperationException("ConnectionString is required.");
            }

            if (string.IsNullOrWhiteSpace(_config.SourceDatabaseName) || string.IsNullOrWhiteSpace(_config.TargetCloneDatabaseName))
            {
                throw new InvalidOperationException("SourceDatabaseName and TargetCloneDatabaseName are required.");
            }

            if (BlockedDatabaseNames.Contains(_config.TargetCloneDatabaseName))
            {
                throw new InvalidOperationException($"Blocked target database name: {_config.TargetCloneDatabaseName}.");
            }

            if (!SafeTargetMarkers.Any(m => _config.TargetCloneDatabaseName.Contains(m, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException("Target database name must contain Clone, Sandbox, PropertyPilot, ReadyToTest, PilotClone, or Migration.");
            }

            if (_options.Execute && (!_config.BackupVerified || !_config.ExecutionPlanApproved))
            {
                throw new InvalidOperationException("Execute mode requires BackupVerified=true and ExecutionPlanApproved=true in config.");
            }

        if (!Enum.TryParse<MigrationMode>(_config.MigrationMode, ignoreCase: true, out _))
            {
                throw new InvalidOperationException("MigrationMode must be Strict, Tolerant, or Hybrid.");
            }

            if (_config.BatchId == Guid.Empty)
            {
                _config.BatchId = Guid.NewGuid();
                AddInfo("Preflight", $"Generated new BatchId={_config.BatchId}.");
            }

            await using var master = new SqlConnection(BuildConnectionString("master"));
            await master.OpenAsync();
            await EnsureDatabaseExistsAsync(master, _config.SourceDatabaseName, "Source");
            await EnsureDatabaseExistsAsync(master, _config.TargetCloneDatabaseName, "TargetClone");

            await using var target = new SqlConnection(BuildConnectionString(_config.TargetCloneDatabaseName));
            await target.OpenAsync();
            var dbName = (string)await ExecuteScalarAsync(target, "SELECT DB_NAME();");
            if (!dbName.Equals(_config.TargetCloneDatabaseName, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Connected database mismatch. Expected {_config.TargetCloneDatabaseName}, got {dbName}.");
            }

            if (!Directory.Exists(_toolkitSqlPath))
            {
                throw new DirectoryNotFoundException($"Toolkit SQL folder not found: {_toolkitSqlPath}");
            }

            AddInfo("Preflight", "Source/target databases exist and target name passed clone safety guard.");
        }

        private async Task MappingValidationAsync()
        {
            await using var target = new SqlConnection(BuildConnectionString(_config.TargetCloneDatabaseName));
            await target.OpenAsync();

            var sql = @"
SELECT
    Warnings = ISNULL((SELECT COUNT(*) FROM dbo.PropertyMigrationWarning WHERE MigrationBatchId=@BatchId),0),
    Errors = ISNULL((SELECT COUNT(*) FROM dbo.PropertyMigrationError WHERE MigrationBatchId=@BatchId),0),
    AutoFixes = ISNULL((SELECT COUNT(*) FROM dbo.PropertyMigrationAutoFix WHERE MigrationBatchId=@BatchId),0),
    SuspenseItems = ISNULL((SELECT COUNT(*) FROM dbo.PropertyMigrationSuspenseMapping WHERE MigrationBatchId=@BatchId AND Status <> N'Closed'),0),
    OpenReviewItems = ISNULL((SELECT COUNT(*) FROM dbo.PropertyMigrationReviewQueue WHERE MigrationBatchId=@BatchId AND Status NOT IN (N'Closed',N'Approved')),0);";
            await using var cmd = new SqlCommand(sql, target);
            cmd.Parameters.AddWithValue("@BatchId", _config.BatchId);
            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                _report.Summary.Warnings = reader.GetInt32(0);
                _report.Summary.Errors = reader.GetInt32(1);
                _report.Summary.AutoFixes = reader.GetInt32(2);
                _report.Summary.SuspenseItems = reader.GetInt32(3);
                _report.Summary.OpenReviewItems = reader.GetInt32(4);
            }

            if (_report.Summary.Errors > 0)
            {
                throw new InvalidOperationException("Mapping validation found recorded errors. Review PropertyMigrationError before migration.");
            }

            AddInfo("MappingValidation", $"Warnings={_report.Summary.Warnings}, AutoFixes={_report.Summary.AutoFixes}, OpenReview={_report.Summary.OpenReviewItems}.");
        }

        private async Task MigrationAsync()
        {
            if (_config.SkipCustomerSpecificMigrationTemplates)
            {
                AddInfo("Migration", "Customer migration templates skipped by config. This option is deprecated and should only be used for pipeline-only proof runs.");
                return;
            }

            var migrationScripts = new[]
            {
                "Migration_DefaultEntitiesSeed_Generic.sql",
                "Migration_Owners_Generic.sql",
                "Migration_MasterData_Generic.sql",
                "Migration_PropertyOwnerLinks_Generic.sql",
                "Migration_Contracts_Generic.sql",
                "Migration_Installments_Generic.sql",
                "Migration_OpeningBalance_Generic.sql",
                "Migration_AdvancePayments_Generic.sql"
            };

            foreach (var script in migrationScripts)
            {
                await RunSqlTemplateAsync(script, readOnly: false);
            }

            if (_config.IncludeAccounting)
            {
                await RunSqlTemplateAsync("Migration_Receipts_Generic.sql", readOnly: false);
                if (_config.IncludeIssues) await RunSqlTemplateAsync("Migration_Issues_Generic.sql", readOnly: false);
                if (_config.IncludeOwnerPayments) await RunSqlTemplateAsync("Migration_OwnerPayments_Generic.sql", readOnly: false);
                if (_config.IncludeJournalEntries) await RunSqlTemplateAsync("Migration_Journals_Generic.sql", readOnly: false);
                if (_config.IncludeTerminations) await RunSqlTemplateAsync("Migration_Terminations_Generic.sql", readOnly: false);
            }

            await AccountingSafetyGateAsync();
        }

        private async Task IntelligenceAsync()
        {
            if (_config.SourceDatabaseName.Equals("RSMDB", StringComparison.OrdinalIgnoreCase))
            {
                await RunSqlTemplateAsync("RSMDB_IntelligenceLayer_20260520.sql", readOnly: false);
                AddInfo("IntelligenceDiscovery", "Executed RSMDB intelligence stages: JournalResolution, ReceiptMatching, OwnerPaymentClassification, ConfidenceScoring, ReviewReduction.");
                return;
            }

            AddInfo("IntelligenceDiscovery", $"No customer-specific intelligence template is registered for SourceDatabase={_config.SourceDatabaseName}; skipped.");
        }

        private async Task AccountIntelligenceAsync()
        {
            if (_config.SourceDatabaseName.Equals("RSMDB", StringComparison.OrdinalIgnoreCase))
            {
                await RunSqlTemplateAsync("RSMDB_AccountMappingIntelligence_20260520.sql", readOnly: false);
                AddInfo("AccountDiscovery", "Executed RSMDB account intelligence stages: AccountDiscovery, AccountMatching, AccountConfidenceScoring, AccountFamilyDetection, AccountReviewQueue.");
                return;
            }

            AddInfo("AccountDiscovery", $"No customer-specific account intelligence template is registered for SourceDatabase={_config.SourceDatabaseName}; skipped.");
        }

        private async Task ReadOnlySafetyValidationAsync()
        {
            await using var target = new SqlConnection(BuildConnectionString(_config.TargetCloneDatabaseName));
            await target.OpenAsync();

            var sql = @"
SELECT
    NullAccountLines = CASE WHEN OBJECT_ID(N'dbo.JournalEntryDetail') IS NULL THEN 0 ELSE (SELECT COUNT(*) FROM dbo.JournalEntryDetail WHERE AccountId IS NULL) END,
    UnbalancedJournals = CASE WHEN OBJECT_ID(N'dbo.JournalEntry') IS NULL OR OBJECT_ID(N'dbo.JournalEntryDetail') IS NULL THEN 0 ELSE (SELECT COUNT(*) FROM (SELECT je.Id FROM dbo.JournalEntry je JOIN dbo.JournalEntryDetail jd ON jd.JournalEntryId=je.Id AND ISNULL(jd.IsDeleted,0)=0 WHERE ISNULL(je.IsDeleted,0)=0 GROUP BY je.Id HAVING ABS(SUM(ISNULL(jd.Debit,0))-SUM(ISNULL(jd.Credit,0)))>0.01) q) END,
    PropertyContracts = CASE WHEN OBJECT_ID(N'dbo.PropertyContract') IS NULL THEN 0 ELSE (SELECT COUNT(*) FROM dbo.PropertyContract WHERE ISNULL(IsDeleted,0)=0) END,
    CashReceipts = CASE WHEN OBJECT_ID(N'dbo.CashReceiptVoucher') IS NULL THEN 0 ELSE (SELECT COUNT(*) FROM dbo.CashReceiptVoucher WHERE ISNULL(IsDeleted,0)=0) END,
    JournalEntries = CASE WHEN OBJECT_ID(N'dbo.JournalEntry') IS NULL THEN 0 ELSE (SELECT COUNT(*) FROM dbo.JournalEntry WHERE ISNULL(IsDeleted,0)=0) END;";

            await using var cmd = new SqlCommand(sql, target) { CommandTimeout = _config.CommandTimeoutSeconds };
            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                var nullAccountLines = reader.GetInt32(0);
                var unbalancedJournals = reader.GetInt32(1);
                _report.Summary.Contracts = reader.GetInt32(2);
                _report.Summary.Receipts = reader.GetInt32(3);
                _report.Summary.Journals = reader.GetInt32(4);
                AddInfo("ReadOnlySafetyValidation", $"Contracts={_report.Summary.Contracts}, Receipts={_report.Summary.Receipts}, Journals={_report.Summary.Journals}, AccountIdNullLines={nullAccountLines}, UnbalancedJournals={unbalancedJournals}.");

                if (nullAccountLines > 0 || unbalancedJournals > 0)
                {
                    throw new InvalidOperationException($"Critical accounting errors detected. AccountIdNullLines={nullAccountLines}, UnbalancedJournals={unbalancedJournals}.");
                }
            }
        }

        private async Task ReadyToTestDeliveryAsync()
        {
            if (!_options.Execute || _config.DryRun)
            {
                AddInfo("ReadyToTestDelivery", "DryRun mode: final database summary was not queried. Report contains the planned run only.");
                return;
            }

            await using var target = new SqlConnection(BuildConnectionString(_config.TargetCloneDatabaseName));
            await target.OpenAsync();

            var summarySql = @"
DECLARE @BatchId uniqueidentifier = @MigrationBatchId;
SELECT Contracts = CASE WHEN OBJECT_ID(N'dbo.PropertyContract') IS NULL THEN 0 ELSE (SELECT COUNT(*) FROM dbo.PropertyContract WHERE ISNULL(IsDeleted,0)=0) END,
       Receipts = CASE WHEN OBJECT_ID(N'dbo.CashReceiptVoucher') IS NULL THEN 0 ELSE (SELECT COUNT(*) FROM dbo.CashReceiptVoucher WHERE ISNULL(IsDeleted,0)=0) END,
       Issues = CASE WHEN OBJECT_ID(N'dbo.CashIssueVoucher') IS NULL THEN 0 ELSE (SELECT COUNT(*) FROM dbo.CashIssueVoucher WHERE ISNULL(IsDeleted,0)=0) END,
       Journals = CASE WHEN OBJECT_ID(N'dbo.JournalEntry') IS NULL THEN 0 ELSE (SELECT COUNT(*) FROM dbo.JournalEntry WHERE ISNULL(IsDeleted,0)=0) END,
       JournalLines = CASE WHEN OBJECT_ID(N'dbo.JournalEntryDetail') IS NULL THEN 0 ELSE (SELECT COUNT(*) FROM dbo.JournalEntryDetail WHERE ISNULL(IsDeleted,0)=0) END,
       AccountIdNullLines = CASE WHEN OBJECT_ID(N'dbo.JournalEntryDetail') IS NULL THEN 0 ELSE (SELECT COUNT(*) FROM dbo.JournalEntryDetail WHERE ISNULL(IsDeleted,0)=0 AND AccountId IS NULL) END,
       UnbalancedJournals = CASE WHEN OBJECT_ID(N'dbo.JournalEntry') IS NULL OR OBJECT_ID(N'dbo.JournalEntryDetail') IS NULL THEN 0 ELSE (SELECT COUNT(*) FROM (SELECT je.Id FROM dbo.JournalEntry je JOIN dbo.JournalEntryDetail jd ON jd.JournalEntryId=je.Id AND ISNULL(jd.IsDeleted,0)=0 WHERE ISNULL(je.IsDeleted,0)=0 GROUP BY je.Id HAVING ABS(SUM(ISNULL(jd.Debit,0))-SUM(ISNULL(jd.Credit,0)))>0.01) q) END,
       ExcludedRecords = CASE WHEN OBJECT_ID(N'dbo.PropertyMigrationExcludedRecord') IS NULL THEN 0 ELSE (SELECT COUNT(*) FROM dbo.PropertyMigrationExcludedRecord WHERE MigrationBatchId=@BatchId) END,
       ReconciliationResults = CASE WHEN OBJECT_ID(N'dbo.PropertyMigrationReconciliationResult') IS NULL THEN 0 ELSE (SELECT COUNT(*) FROM dbo.PropertyMigrationReconciliationResult WHERE MigrationBatchId=@BatchId) END;";
            await using var cmd = new SqlCommand(summarySql, target);
            cmd.Parameters.AddWithValue("@MigrationBatchId", _config.BatchId);
            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                _report.Summary.Contracts = reader.GetInt32(0);
                _report.Summary.Receipts = reader.GetInt32(1);
                _report.Summary.Issues = reader.GetInt32(2);
                _report.Summary.Journals = reader.GetInt32(3);
                _report.Summary.JournalLines = reader.GetInt32(4);
                _report.Summary.AccountIdNullLines = reader.GetInt32(5);
                _report.Summary.UnbalancedJournals = reader.GetInt32(6);
                _report.Summary.ExcludedRecords = reader.GetInt32(7);
                _report.Summary.ReconciliationResults = reader.GetInt32(8);
            }

            if (_report.Summary.SuspenseItems > 0 && !_config.FinanceSignOff)
            {
                _report.Warnings.Add(new RunMessage("ReadyToTestDelivery", "Warning", "Open suspense items exist. GoLive must remain blocked until FinanceSignOff=true and suspense is closed/reviewed."));
            }

            AddInfo("ReadyToTestDelivery", "Summary collected. Final report will be written locally; no GoLive action is performed.");
        }

        private async Task EnsureCoreTablesAsync()
        {
            await RunSqlTemplateAsync("00_ToolkitCore_ConfigAndXref_Generic.sql", readOnly: false);
            await RunSqlTemplateAsync("01_SourceStagingTables_Generic.sql", readOnly: false);
            await EnsureConfigAndBatchRowsAsync();
        }

        private Task DryRunPlanAsync()
        {
            AddInfo("ExecutionPlan", "DryRun only. No SQL templates will be executed and no database objects will be changed.");
            AddInfo("ExecutionPlan", "Planned stages: CoreSetup, Discovery, Diagnostics, MappingValidation, Migration, Reconciliation, ReadyToTestDelivery.");
            if (_config.IncludeIntelligence)
            {
                AddInfo("ExecutionPlan", "Intelligence stages enabled: IntelligenceDiscovery, JournalResolution, ReceiptMatching, OwnerPaymentClassification, ConfidenceScoring, ReviewReduction.");
            }
            if (_config.IncludeAccountIntelligence)
            {
                AddInfo("ExecutionPlan", "Account intelligence stages enabled: AccountDiscovery, AccountMatching, AccountConfidenceScoring, AccountFamilyDetection, AccountReviewQueue.");
            }
            AddInfo("ExecutionPlan", $"Selected modules: Accounting={_config.IncludeAccounting}, Receipts={_config.IncludeHistoricalReceipts}, Issues={_config.IncludeIssues}, Journals={_config.IncludeJournalEntries}, AdvancePayments={_config.IncludeAdvancePayments}, Terminations={_config.IncludeTerminations}.");
            AddInfo("ExecutionPlan", $"ControlledPipelineOnly={_config.SkipCustomerSpecificMigrationTemplates}.");
            AddInfo("ExecutionPlan", $"SkipStages={string.Join(",", _config.SkipStages)}.");
            AddInfo("ExecutionPlan", $"Safety: target guard passed; execute requires BackupVerified={_config.BackupVerified}, ExecutionPlanApproved={_config.ExecutionPlanApproved}.");
            return Task.CompletedTask;
        }

        private async Task EnsureConfigAndBatchRowsAsync()
        {
            await using var target = new SqlConnection(BuildConnectionString(_config.TargetCloneDatabaseName));
            await target.OpenAsync();

            var sql = @"
DECLARE @ConfigId int;
SELECT @ConfigId = ConfigId
FROM dbo.PropertyMigrationConfig
WHERE CustomerCode=@CustomerCode AND SourceDatabaseName=@SourceDatabaseName AND TargetCloneDatabaseName=@TargetCloneDatabaseName;

IF @ConfigId IS NULL
BEGIN
    INSERT INTO dbo.PropertyMigrationConfig(
        CustomerCode, SourceDatabaseName, TargetCloneDatabaseName, CutoffDate, MigrationMode, DryRun,
        IncludeHistoricalReceipts, IncludeHistoricalIssues, IncludeJournalEntries, IncludeAdvancePayments, IncludeTerminations,
        ExcludeUnsafeOwnerPayments, AllowUnknownUnits, AllowUnknownProperties, AllowSuspenseAccounts,
        AllowFallbackPaymentMethods, AllowDefaultCashBox, AllowDefaultBank, AllowTemporaryRenterAccounts,
        AutoCreateMissingAccounts, AutoCreateMissingLookups, IsApproved, Notes)
    VALUES(
        @CustomerCode, @SourceDatabaseName, @TargetCloneDatabaseName, @CutoffDate, @MigrationMode, @DryRun,
        @IncludeHistoricalReceipts, @IncludeIssues, @IncludeJournalEntries, @IncludeAdvancePayments, @IncludeTerminations,
        @ExcludeUnsafePayments, @AllowUnknownUnits, @AllowUnknownProperties, @AllowSuspenseAccounts,
        @AllowFallbackPaymentMethods, @AllowDefaultCashBox, @AllowDefaultBank, @AllowTemporaryRenterAccounts,
        @AutoCreateMissingAccounts, @AutoCreateMissingLookups, @ExecutionPlanApproved,
        N'Created by DynamicErp.PropertyMigration.Runner');
    SET @ConfigId = SCOPE_IDENTITY();
END

IF NOT EXISTS (SELECT 1 FROM dbo.PropertyMigrationBatch WHERE MigrationBatchId=@BatchId)
BEGIN
    INSERT INTO dbo.PropertyMigrationBatch(
        MigrationBatchId, CustomerCode, ConfigId, SourceDatabaseName, TargetDatabaseName, CutoffDate,
        MigrationMode, Stage, Status, CreatedBy, Notes)
    VALUES(
        @BatchId, @CustomerCode, @ConfigId, @SourceDatabaseName, @TargetCloneDatabaseName, @CutoffDate,
        @MigrationMode, N'Preflight', N'Running', SUSER_SNAME(), N'Created by DynamicErp.PropertyMigration.Runner');
END";

            await using var cmd = new SqlCommand(sql, target) { CommandTimeout = _config.CommandTimeoutSeconds };
            cmd.Parameters.AddWithValue("@CustomerCode", _config.CustomerCode);
            cmd.Parameters.AddWithValue("@SourceDatabaseName", _config.SourceDatabaseName);
            cmd.Parameters.AddWithValue("@TargetCloneDatabaseName", _config.TargetCloneDatabaseName);
            cmd.Parameters.AddWithValue("@CutoffDate", _config.CutoffDate.Date);
            cmd.Parameters.AddWithValue("@MigrationMode", _config.MigrationMode);
            cmd.Parameters.AddWithValue("@DryRun", _config.DryRun);
            cmd.Parameters.AddWithValue("@IncludeHistoricalReceipts", _config.IncludeHistoricalReceipts);
            cmd.Parameters.AddWithValue("@IncludeIssues", _config.IncludeIssues);
            cmd.Parameters.AddWithValue("@IncludeJournalEntries", _config.IncludeJournalEntries);
            cmd.Parameters.AddWithValue("@IncludeAdvancePayments", _config.IncludeAdvancePayments);
            cmd.Parameters.AddWithValue("@IncludeTerminations", _config.IncludeTerminations);
            cmd.Parameters.AddWithValue("@ExcludeUnsafePayments", _config.ExcludeUnsafePayments);
            cmd.Parameters.AddWithValue("@AllowUnknownUnits", _config.AutoFix.AllowUnknownUnits);
            cmd.Parameters.AddWithValue("@AllowUnknownProperties", _config.AutoFix.AllowUnknownProperties);
            cmd.Parameters.AddWithValue("@AllowSuspenseAccounts", _config.AutoFix.AllowSuspenseAccounts);
            cmd.Parameters.AddWithValue("@AllowFallbackPaymentMethods", _config.AutoFix.AllowFallbackPaymentMethods);
            cmd.Parameters.AddWithValue("@AllowDefaultCashBox", _config.AutoFix.AllowDefaultCashBox);
            cmd.Parameters.AddWithValue("@AllowDefaultBank", _config.AutoFix.AllowDefaultBank);
            cmd.Parameters.AddWithValue("@AllowTemporaryRenterAccounts", _config.AutoFix.AllowTemporaryRenterAccounts);
            cmd.Parameters.AddWithValue("@AutoCreateMissingAccounts", _config.AutoFix.AutoCreateMissingAccounts);
            cmd.Parameters.AddWithValue("@AutoCreateMissingLookups", _config.AutoFix.AutoCreateMissingLookups);
            cmd.Parameters.AddWithValue("@ExecutionPlanApproved", _config.ExecutionPlanApproved);
            cmd.Parameters.AddWithValue("@BatchId", _config.BatchId);
            await cmd.ExecuteNonQueryAsync();
            AddInfo("CoreSetup", "Core config and batch rows are ready on the target clone.");
        }

        private async Task RunSqlTemplateAsync(string scriptName, bool readOnly)
        {
            var path = Path.Combine(_toolkitSqlPath, scriptName);
            if (!File.Exists(path))
            {
                throw new FileNotFoundException("SQL template not found.", path);
            }

            if (!_options.Execute)
            {
                AddInfo(scriptName, "DryRun: skipped SQL template execution.");
                return;
            }

            var text = await File.ReadAllTextAsync(path, Encoding.UTF8);
            text = ReplaceTokens(text);
            await using var target = new SqlConnection(BuildConnectionString(_config.TargetCloneDatabaseName));
            await target.OpenAsync();
            await WriteRunLogAsync(target, scriptName, "Started", 0, null);
            var affectedRows = 0;
            foreach (var batch in SplitSqlBatches(text))
            {
                if (string.IsNullOrWhiteSpace(batch)) continue;
                try
                {
                    await using var cmd = new SqlCommand(batch, target) { CommandTimeout = _config.CommandTimeoutSeconds };
                    var batchRows = await cmd.ExecuteNonQueryAsync();
                    if (batchRows > 0) affectedRows += batchRows;
                }
                catch (Exception ex)
                {
                    await WriteRunLogAsync(target, scriptName, "Failed", affectedRows, ex.Message);
                    throw;
                }
            }
            await WriteRunLogAsync(target, scriptName, "Completed", affectedRows, null);
            AddInfo(scriptName, (readOnly ? "Executed SELECT/diagnostic template." : "Executed target clone template.") + $" RowsAffected={affectedRows}.");
        }

        private async Task WriteRunLogAsync(SqlConnection target, string stepName, string status, int affectedRows, string? message)
        {
            if (!await ObjectExistsAsync(target, "dbo.PropertyMigrationRunLog")) return;

            var sql = @"
INSERT INTO dbo.PropertyMigrationRunLog(MigrationBatchId,CustomerCode,SourceDatabaseName,TargetDatabaseName,Stage,StepName,Status,StartedAt,EndedAt,Message)
VALUES(@BatchId,@CustomerCode,@SourceDatabaseName,@TargetDatabaseName,N'TemplateExecution',@StepName,@Status,GETDATE(),GETDATE(),@Message);";
            await using var cmd = new SqlCommand(sql, target) { CommandTimeout = _config.CommandTimeoutSeconds };
            cmd.Parameters.AddWithValue("@BatchId", _config.BatchId);
            cmd.Parameters.AddWithValue("@CustomerCode", _config.CustomerCode);
            cmd.Parameters.AddWithValue("@SourceDatabaseName", _config.SourceDatabaseName);
            cmd.Parameters.AddWithValue("@TargetDatabaseName", _config.TargetCloneDatabaseName);
            cmd.Parameters.AddWithValue("@StepName", stepName);
            cmd.Parameters.AddWithValue("@Status", status);
            cmd.Parameters.AddWithValue("@Message", (object?)((message ?? "RowsAffected=" + affectedRows.ToString())) ?? DBNull.Value);
            await cmd.ExecuteNonQueryAsync();
        }

        private static async Task<bool> ObjectExistsAsync(SqlConnection connection, string objectName)
        {
            await using var cmd = new SqlCommand("SELECT CASE WHEN OBJECT_ID(@name) IS NULL THEN 0 ELSE 1 END;", connection);
            cmd.Parameters.AddWithValue("@name", objectName);
            return Convert.ToInt32(await cmd.ExecuteScalarAsync()) == 1;
        }

        private async Task AccountingSafetyGateAsync()
        {
            await using var target = new SqlConnection(BuildConnectionString(_config.TargetCloneDatabaseName));
            await target.OpenAsync();
            var sql = @"
SELECT CriticalCount =
    (CASE WHEN OBJECT_ID(N'dbo.JournalEntryDetail') IS NULL THEN 0 ELSE (SELECT COUNT(*) FROM dbo.JournalEntryDetail WHERE AccountId IS NULL) END)
  + (CASE WHEN OBJECT_ID(N'dbo.JournalEntry') IS NULL OR OBJECT_ID(N'dbo.JournalEntryDetail') IS NULL THEN 0 ELSE (SELECT COUNT(*) FROM (SELECT je.Id FROM dbo.JournalEntry je JOIN dbo.JournalEntryDetail jd ON jd.JournalEntryId=je.Id AND ISNULL(jd.IsDeleted,0)=0 WHERE ISNULL(je.IsDeleted,0)=0 GROUP BY je.Id HAVING ABS(SUM(ISNULL(jd.Debit,0))-SUM(ISNULL(jd.Credit,0)))>0.01) q) END);";
            var criticalCount = Convert.ToInt32(await ExecuteScalarAsync(target, sql));
            if (criticalCount > 0)
            {
                throw new InvalidOperationException($"Critical accounting safety gate failed. Issues={criticalCount}.");
            }
        }

        private async Task EnsureDatabaseExistsAsync(SqlConnection connection, string databaseName, string label)
        {
            await using var cmd = new SqlCommand("SELECT COUNT(*) FROM sys.databases WHERE name=@name;", connection);
            cmd.Parameters.AddWithValue("@name", databaseName);
            var count = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            if (count == 0)
            {
                throw new InvalidOperationException($"{label} database not found: {databaseName}");
            }
        }

        private async Task<object> ExecuteScalarAsync(SqlConnection connection, string sql)
        {
            await using var cmd = new SqlCommand(sql, connection) { CommandTimeout = _config.CommandTimeoutSeconds };
            return (await cmd.ExecuteScalarAsync())!;
        }

        private string BuildConnectionString(string databaseName)
        {
            var builder = new SqlConnectionStringBuilder(_config.ConnectionString)
            {
                InitialCatalog = databaseName,
                TrustServerCertificate = true
            };
            return builder.ConnectionString;
        }

        private string ReplaceTokens(string sql)
        {
            return sql
                .Replace("$(MigrationBatchId)", _config.BatchId.ToString(), StringComparison.OrdinalIgnoreCase)
                .Replace("$(CustomerCode)", EscapeSqlLiteral(_config.CustomerCode), StringComparison.OrdinalIgnoreCase)
                .Replace("$(SourceDatabaseName)", EscapeSqlLiteral(_config.SourceDatabaseName), StringComparison.OrdinalIgnoreCase)
                .Replace("$(TargetCloneDatabaseName)", EscapeSqlLiteral(_config.TargetCloneDatabaseName), StringComparison.OrdinalIgnoreCase)
                .Replace("$(CutoffDate)", _config.CutoffDate.ToString("yyyy-MM-dd"), StringComparison.OrdinalIgnoreCase)
                .Replace("$(MigrationMode)", EscapeSqlLiteral(_config.MigrationMode), StringComparison.OrdinalIgnoreCase);
        }

        private static string EscapeSqlLiteral(string value) => value.Replace("'", "''");

        private static IEnumerable<string> SplitSqlBatches(string sql)
        {
            var sb = new StringBuilder();
            using var reader = new StringReader(sql);
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                if (line.Trim().Equals("GO", StringComparison.OrdinalIgnoreCase))
                {
                    yield return sb.ToString();
                    sb.Clear();
                }
                else
                {
                    sb.AppendLine(line);
                }
            }
            if (sb.Length > 0) yield return sb.ToString();
        }

        private void AddInfo(string step, string message)
        {
            _report.Messages.Add(new RunMessage(step, "Info", message));
            Console.WriteLine("  " + message);
        }
    }
}

internal enum MigrationMode
{
    Strict,
    Tolerant,
    Hybrid
}

internal sealed class RunnerOptions
{
    public string? ConfigPath { get; private set; }
    public bool Execute { get; private set; }
    public bool ShowHelp { get; private set; }
    public string? SourceDatabase { get; private set; }
    public string? TargetDatabase { get; private set; }
    public string? MigrationMode { get; private set; }

    public static string HelpText => """
DynamicErp.PropertyMigration.Runner

Usage:
  dotnet run --project Tools/DynamicErp.PropertyMigration.Runner -- --config <config.json> [--dry-run]
  dotnet run --project Tools/DynamicErp.PropertyMigration.Runner -- --config <config.json> --execute

Options:
  --config <path>       Required customer migration config JSON.
  --execute             Execute clone-safe steps. Without this flag runner is DryRun.
  --source-db <name>    Override SourceDatabaseName from config.
  --target-db <name>    Override TargetCloneDatabaseName from config.
  --mode <mode>         Override MigrationMode: Strict, Tolerant, Hybrid.
  --help                Show help.
""";

    public static RunnerOptions Parse(string[] args)
    {
        var options = new RunnerOptions();
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            switch (arg.ToLowerInvariant())
            {
                case "--help":
                case "-h":
                    options.ShowHelp = true;
                    break;
                case "--config":
                    options.ConfigPath = RequireValue(args, ref i, arg);
                    break;
                case "--execute":
                    options.Execute = true;
                    break;
                case "--dry-run":
                    options.Execute = false;
                    break;
                case "--source-db":
                    options.SourceDatabase = RequireValue(args, ref i, arg);
                    break;
                case "--target-db":
                    options.TargetDatabase = RequireValue(args, ref i, arg);
                    break;
                case "--mode":
                    options.MigrationMode = RequireValue(args, ref i, arg);
                    break;
                default:
                    throw new ArgumentException("Unknown argument: " + arg);
            }
        }
        return options;
    }

    private static string RequireValue(string[] args, ref int index, string option)
    {
        if (index + 1 >= args.Length) throw new ArgumentException($"{option} requires a value.");
        index++;
        return args[index];
    }
}

internal sealed class MigrationRunnerConfig
{
    public string CustomerCode { get; set; } = "CUSTOMER";
    public string SourceDatabaseName { get; set; } = "";
    public string TargetCloneDatabaseName { get; set; } = "";
    public string ConnectionString { get; set; } = "";
    public string? ConnectionStringEnvironmentVariable { get; set; }
    public string MigrationMode { get; set; } = "Hybrid";
    public Guid BatchId { get; set; } = Guid.NewGuid();
    public DateTime CutoffDate { get; set; } = DateTime.Today;
    public bool DryRun { get; set; } = true;
    public bool IncludeAccounting { get; set; }
    public bool IncludeContracts { get; set; } = true;
    public bool IncludeInstallments { get; set; } = true;
    public bool IncludeOpeningBalance { get; set; } = true;
    public bool IncludeHistoricalReceipts { get; set; }
    public bool IncludeIssues { get; set; }
    public bool IncludeOwnerPayments { get; set; }
    public bool IncludeJournalEntries { get; set; }
    public bool IncludeAdvancePayments { get; set; } = true;
    public bool IncludeTerminations { get; set; }
    public bool IncludeIntelligence { get; set; }
    public bool IncludeAccountIntelligence { get; set; }
    public bool ExcludeUnsafePayments { get; set; } = true;
    public bool BackupVerified { get; set; }
    public bool ExecutionPlanApproved { get; set; }
    public bool FinanceSignOff { get; set; }
    public bool SkipCustomerSpecificMigrationTemplates { get; set; }
    public List<string> SkipStages { get; set; } = [];
    public int CommandTimeoutSeconds { get; set; } = 120;
    public string? ToolkitSqlPath { get; set; }
    public string? ReportDirectory { get; set; }

    public AutoFixOptions AutoFix { get; set; } = new();

    public static async Task<MigrationRunnerConfig> LoadAsync(string path)
    {
        var json = await File.ReadAllTextAsync(path, Encoding.UTF8);
        var config = JsonSerializer.Deserialize<MigrationRunnerConfig>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        });
        if (config == null)
        {
            throw new InvalidOperationException("Unable to parse config JSON.");
        }
        if (string.IsNullOrWhiteSpace(config.ConnectionString) && !string.IsNullOrWhiteSpace(config.ConnectionStringEnvironmentVariable))
        {
            config.ConnectionString = Environment.GetEnvironmentVariable(config.ConnectionStringEnvironmentVariable) ?? "";
        }
        return config;
    }

    public void ApplyCliOverrides(RunnerOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.SourceDatabase)) SourceDatabaseName = options.SourceDatabase;
        if (!string.IsNullOrWhiteSpace(options.TargetDatabase)) TargetCloneDatabaseName = options.TargetDatabase;
        if (!string.IsNullOrWhiteSpace(options.MigrationMode)) MigrationMode = options.MigrationMode;
        if (options.Execute) DryRun = false;
    }

    public string GetReportDirectory()
    {
        if (!string.IsNullOrWhiteSpace(ReportDirectory)) return Path.GetFullPath(ReportDirectory);
        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Reports"));
    }
}

internal sealed class AutoFixOptions
{
    public bool AllowUnknownUnits { get; set; }
    public bool AllowUnknownProperties { get; set; }
    public bool AllowSuspenseAccounts { get; set; }
    public bool AllowFallbackPaymentMethods { get; set; }
    public bool AllowDefaultCashBox { get; set; }
    public bool AllowDefaultBank { get; set; }
    public bool AllowTemporaryRenterAccounts { get; set; }
    public bool AutoCreateMissingAccounts { get; set; }
    public bool AutoCreateMissingLookups { get; set; }
}

internal sealed class MigrationRunReport
{
    public MigrationRunnerConfig Config { get; }
    public DateTimeOffset StartedAt { get; }
    public DateTimeOffset? CompletedAt { get; set; }
    public string ExecutionMode { get; }
    public string Status { get; set; } = "Running";
    public MigrationSummary Summary { get; } = new();
    public List<StepResult> StepResults { get; } = [];
    public List<RunMessage> Messages { get; } = [];
    public List<RunMessage> Warnings { get; } = [];
    public List<RunMessage> Errors { get; } = [];

    public MigrationRunReport(MigrationRunnerConfig config, DateTimeOffset startedAt, string executionMode)
    {
        Config = config;
        StartedAt = startedAt;
        ExecutionMode = executionMode;
    }

    public async Task<string> WriteAsync(string directory)
    {
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, $"PropertyMigrationRunnerReport_{Config.CustomerCode}_{DateTime.Now:yyyyMMdd_HHmmss}.md");
        var sb = new StringBuilder();
        sb.AppendLine("# Property Migration Runner Report");
        sb.AppendLine();
        sb.AppendLine($"- CustomerCode: `{Config.CustomerCode}`");
        sb.AppendLine($"- SourceDatabase: `{Config.SourceDatabaseName}`");
        sb.AppendLine($"- TargetCloneDatabase: `{Config.TargetCloneDatabaseName}`");
        sb.AppendLine($"- BatchId: `{Config.BatchId}`");
        sb.AppendLine($"- MigrationMode: `{Config.MigrationMode}`");
        sb.AppendLine($"- ExecutionMode: `{ExecutionMode}`");
        sb.AppendLine($"- Status: `{Status}`");
        sb.AppendLine($"- StartedAt: `{StartedAt:O}`");
        sb.AppendLine($"- CompletedAt: `{CompletedAt:O}`");
        sb.AppendLine();
        sb.AppendLine("## Summary");
        sb.AppendLine();
        sb.AppendLine($"- Contracts: `{Summary.Contracts}`");
        sb.AppendLine($"- Receipts: `{Summary.Receipts}`");
        sb.AppendLine($"- Issues: `{Summary.Issues}`");
        sb.AppendLine($"- Journals: `{Summary.Journals}`");
        sb.AppendLine($"- JournalLines: `{Summary.JournalLines}`");
        sb.AppendLine($"- AccountIdNullLines: `{Summary.AccountIdNullLines}`");
        sb.AppendLine($"- UnbalancedJournals: `{Summary.UnbalancedJournals}`");
        sb.AppendLine($"- Warnings: `{Summary.Warnings}`");
        sb.AppendLine($"- Errors: `{Summary.Errors}`");
        sb.AppendLine($"- AutoFixes: `{Summary.AutoFixes}`");
        sb.AppendLine($"- SuspenseItems: `{Summary.SuspenseItems}`");
        sb.AppendLine($"- OpenReviewItems: `{Summary.OpenReviewItems}`");
        sb.AppendLine($"- ExcludedRecords: `{Summary.ExcludedRecords}`");
        sb.AppendLine($"- ReconciliationResults: `{Summary.ReconciliationResults}`");
        sb.AppendLine();
        sb.AppendLine("## Steps");
        foreach (var step in StepResults)
        {
            sb.AppendLine($"- `{step.StepName}`: `{step.Status}` {step.ErrorMessage}");
        }
        sb.AppendLine();
        sb.AppendLine("## Messages");
        foreach (var message in Messages.Concat(Warnings).Concat(Errors))
        {
            sb.AppendLine($"- `{message.Step}` `{message.Level}`: {message.Message}");
        }
        await File.WriteAllTextAsync(path, sb.ToString(), Encoding.UTF8);
        return path;
    }
}

internal sealed class MigrationSummary
{
    public int Contracts { get; set; }
    public int Receipts { get; set; }
    public int Issues { get; set; }
    public int Journals { get; set; }
    public int JournalLines { get; set; }
    public int AccountIdNullLines { get; set; }
    public int UnbalancedJournals { get; set; }
    public int Warnings { get; set; }
    public int Errors { get; set; }
    public int AutoFixes { get; set; }
    public int SuspenseItems { get; set; }
    public int OpenReviewItems { get; set; }
    public int ExcludedRecords { get; set; }
    public int ReconciliationResults { get; set; }
}

internal sealed record StepResult(string StepName, string Status, DateTimeOffset StartedAt, DateTimeOffset? CompletedAt, string? ErrorMessage);
internal sealed record RunMessage(string Step, string Level, string Message);
