using System;
using System.Collections.Generic;

namespace MyERP.Areas.Pos.Models
{
    public class PosSqlUpdateDashboardViewModel
    {
        public PosSqlUpdateStatusResult Status { get; set; }
        public PosSqlUpdateRunResult LastRun { get; set; }
        public string Message { get; set; }
        public bool IsError { get; set; }

        public PosSqlUpdateDashboardViewModel()
        {
            Status = new PosSqlUpdateStatusResult();
        }
    }

    public class PosSqlUpdateStatusResult
    {
        public string ModuleName { get; set; }
        public string DatabaseName { get; set; }
        public string ServerName { get; set; }
        public bool IsPosDatabase { get; set; }
        public string StatusText { get; set; }
        public string StatusCssClass { get; set; }
        public int AppliedCount { get; set; }
        public int PendingCount { get; set; }
        public int FailedCount { get; set; }
        public int HashMismatchCount { get; set; }
        public int ManualCount { get; set; }
        public DateTime? LastAppliedOn { get; set; }
        public int? LastRunId { get; set; }
        public bool CanApplyUpdates { get; set; }
        public bool RequiresDdlPermission { get; set; }
        public string PermissionMessage { get; set; }
        public IList<PosSqlUpdateScriptViewModel> Scripts { get; set; }
        public IList<PosSqlUpdateRunSummary> RecentRuns { get; set; }

        public PosSqlUpdateStatusResult()
        {
            Scripts = new List<PosSqlUpdateScriptViewModel>();
            RecentRuns = new List<PosSqlUpdateRunSummary>();
            IsPosDatabase = true;
            CanApplyUpdates = true;
        }
    }

    public class PosSqlUpdateScriptViewModel
    {
        public decimal Order { get; set; }
        public string ScriptName { get; set; }
        public string Purpose { get; set; }
        public string RelativePath { get; set; }
        public string Hash { get; set; }
        public string Status { get; set; }
        public string StatusArabic { get; set; }
        public string StatusCssClass { get; set; }
        public DateTime? AppliedOn { get; set; }
        public string AppliedBy { get; set; }
        public string LastErrorSummary { get; set; }
        public bool IsManual { get; set; }
    }

    public class PosSqlUpdateRunRequest
    {
        public bool ConfirmBackup { get; set; }
        public bool IgnoreHashMismatch { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; }
        public string ClientIp { get; set; }
        public string ReleaseNo { get; set; }
    }

    public class PosSqlUpdateRunResult
    {
        public bool Success { get; set; }
        public bool IsDryRun { get; set; }
        public string Message { get; set; }
        public int? RunId { get; set; }
        public int AppliedCount { get; set; }
        public int SkippedCount { get; set; }
        public int FailedCount { get; set; }
        public int PendingCount { get; set; }
        public int HashMismatchCount { get; set; }
        public IList<PosSqlUpdateScriptViewModel> Scripts { get; set; }

        public PosSqlUpdateRunResult()
        {
            Scripts = new List<PosSqlUpdateScriptViewModel>();
        }
    }

    public class PosSqlUpdateRunSummary
    {
        public int RunId { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? FinishedAt { get; set; }
        public string Mode { get; set; }
        public string Status { get; set; }
        public string StartedBy { get; set; }
        public string UserName { get; set; }
        public string ClientIp { get; set; }
        public string DatabaseName { get; set; }
        public string ServerName { get; set; }
        public int TotalScripts { get; set; }
        public int AppliedCount { get; set; }
        public int SkippedCount { get; set; }
        public int FailedCount { get; set; }
        public int WarningCount { get; set; }
    }
}
