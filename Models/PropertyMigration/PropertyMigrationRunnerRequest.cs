using System;
using System.Collections.Generic;

namespace MyERP.Models.PropertyMigration
{
    public class PropertyMigrationRunnerRequest
    {
        public string SourceDatabaseName { get; set; }
        public string TargetCloneDatabaseName { get; set; }
        public string MigrationMode { get; set; }
        public string Stage { get; set; }
        public string CustomerCode { get; set; }
        public string BatchId { get; set; }
        public DateTime? CutoffDate { get; set; }
        public bool IncludeOperationalMigration { get; set; }
        public bool IncludeAccounting { get; set; }
        public bool IncludeHistoricalReceipts { get; set; }
        public bool IncludeIssues { get; set; }
        public bool IncludeOwnerPayments { get; set; }
        public bool IncludeTerminations { get; set; }
        public bool Include9088 { get; set; }
        public bool IncludeAdvancePayments { get; set; }
        public bool ExecuteMode { get; set; }
        public bool BackupVerified { get; set; }
        public bool ExecutionPlanApproved { get; set; }
        public string ExecuteConfirmation { get; set; }
    }

    public class PropertyMigrationRunnerResult
    {
        public bool Success { get; set; }
        public bool WasExecuted { get; set; }
        public string Status { get; set; }
        public string Message { get; set; }
        public string BatchId { get; set; }
        public string ConfigPath { get; set; }
        public string ReportPath { get; set; }
        public string StandardOutput { get; set; }
        public string StandardError { get; set; }
        public int? ExitCode { get; set; }
        public IList<string> ValidationErrors { get; private set; }

        public PropertyMigrationRunnerResult()
        {
            ValidationErrors = new List<string>();
        }
    }
}
