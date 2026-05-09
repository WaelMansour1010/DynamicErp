using System;
using System.Collections.Generic;

namespace MyERP.Areas.MainErp.ViewModels.DatabaseMigration
{
    public class DatabaseMigrationDashboardViewModel
    {
        public DatabaseMigrationDashboardViewModel()
        {
            Pending = new List<MigrationFileInfoViewModel>();
            HashMismatches = new List<MigrationFileInfoViewModel>();
            AppliedHistory = new List<MigrationHistoryViewModel>();
            FailedHistory = new List<MigrationHistoryViewModel>();
            Sources = new List<MigrationSourceViewModel>();
            Runs = new List<MigrationRunViewModel>();
            Warnings = new List<MigrationWarning>();
        }
        public string ServerName { get; set; }
        public string DatabaseName { get; set; }
        public string EnvironmentName { get; set; }
        public string Status { get; set; }
        public DateTime? LastAppliedOn { get; set; }
        public int AppliedCount { get; set; }
        public int PendingCount { get; set; }
        public int FailedCount { get; set; }
        public int HashMismatchCount { get; set; }
        public MigrationRunResult LastRunResult { get; set; }
        public IList<MigrationFileInfoViewModel> Pending { get; set; }
        public IList<MigrationFileInfoViewModel> HashMismatches { get; set; }
        public IList<MigrationHistoryViewModel> AppliedHistory { get; set; }
        public IList<MigrationHistoryViewModel> FailedHistory { get; set; }
        public IList<MigrationSourceViewModel> Sources { get; set; }
        public IList<MigrationRunViewModel> Runs { get; set; }
        public IList<MigrationWarning> Warnings { get; set; }
    }

    public class MigrationFileInfoViewModel
    {
        public MigrationFileInfoViewModel() { Warnings = new List<MigrationWarning>(); }
        public string ScriptKey { get; set; }
        public int? MigrationNumber { get; set; }
        public string ScriptName { get; set; }
        public string ScriptPath { get; set; }
        public string FullPath { get; set; }
        public string ScriptHash { get; set; }
        public string ModuleName { get; set; }
        public DateTime LastModifiedOn { get; set; }
        public string UpdateType { get; set; }
        public bool SafeToRerun { get; set; }
        public string Dependencies { get; set; }
        public string ValidationStatus { get; set; }
        public bool IsClassified { get; set; }
        public bool HasHashMismatch { get; set; }
        public IList<MigrationWarning> Warnings { get; set; }
    }

    public class MigrationHistoryViewModel
    {
        public int MigrationId { get; set; }
        public string ScriptName { get; set; }
        public string ScriptPath { get; set; }
        public string ScriptHash { get; set; }
        public string ModuleName { get; set; }
        public DateTime AppliedOn { get; set; }
        public string AppliedBy { get; set; }
        public string MachineName { get; set; }
        public string DatabaseName { get; set; }
        public int? DurationMs { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public string ReleaseNo { get; set; }
    }

    public class MigrationRunRequest
    {
        public MigrationRunRequest() { ScriptKeys = new List<string>(); }
        public IList<string> ScriptKeys { get; set; }
        public string ConfirmText { get; set; }
        public bool StopOnError { get; set; }
        public string ReleaseNo { get; set; }
    }

    public class MigrationRunResult
    {
        public MigrationRunResult()
        {
            Applied = new List<MigrationRunDetailViewModel>();
            Failed = new List<MigrationRunDetailViewModel>();
            HashMismatches = new List<MigrationFileInfoViewModel>();
            Pending = new List<MigrationFileInfoViewModel>();
        }
        public long? RunId { get; set; }
        public string Mode { get; set; }
        public string Status { get; set; }
        public int TotalScripts { get; set; }
        public int AppliedCount { get; set; }
        public int FailedCount { get; set; }
        public int WarningCount { get; set; }
        public IList<MigrationFileInfoViewModel> Pending { get; set; }
        public IList<MigrationRunDetailViewModel> Applied { get; set; }
        public IList<MigrationRunDetailViewModel> Failed { get; set; }
        public IList<MigrationFileInfoViewModel> HashMismatches { get; set; }
    }

    public class MigrationRunViewModel
    {
        public MigrationRunViewModel() { Details = new List<MigrationRunDetailViewModel>(); }
        public long RunId { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? FinishedAt { get; set; }
        public string StartedBy { get; set; }
        public string DatabaseName { get; set; }
        public string ServerName { get; set; }
        public string Mode { get; set; }
        public string Status { get; set; }
        public int TotalScripts { get; set; }
        public int AppliedCount { get; set; }
        public int FailedCount { get; set; }
        public int WarningCount { get; set; }
        public IList<MigrationRunDetailViewModel> Details { get; set; }
    }

    public class MigrationRunDetailViewModel
    {
        public long RunDetailId { get; set; }
        public long RunId { get; set; }
        public string ScriptName { get; set; }
        public string ModuleName { get; set; }
        public string Status { get; set; }
        public int? DurationMs { get; set; }
        public string ErrorMessage { get; set; }
        public string ScriptHash { get; set; }
    }

    public class MigrationWarning
    {
        public string Severity { get; set; }
        public string Code { get; set; }
        public string Message { get; set; }
    }

    public class MigrationSourceViewModel
    {
        public string ConfiguredPath { get; set; }
        public string ResolvedPath { get; set; }
        public bool Exists { get; set; }
        public int SqlFileCount { get; set; }
    }

    public class MigrationScriptPreviewViewModel
    {
        public string ScriptKey { get; set; }
        public string ScriptName { get; set; }
        public string ScriptPath { get; set; }
        public string Content { get; set; }
        public IList<MigrationWarning> Warnings { get; set; }
    }
}
