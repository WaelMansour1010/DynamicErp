using System;
using System.Collections.Generic;

namespace MyERP.Areas.Reports.Models
{
    public static class DynamicReportLifecycleStatus
    {
        public const string Draft = "Draft";
        public const string NeedsMapping = "NeedsMapping";
        public const string ValidationErrors = "ValidationErrors";
        public const string ReadyForActivation = "ReadyForActivation";
        public const string Active = "Active";
        public const string Disabled = "Disabled";
        public const string Archived = "Archived";
    }

    public static class LifecycleStatusEnum
    {
        public const string Draft = DynamicReportLifecycleStatus.Draft;
        public const string NeedsMapping = DynamicReportLifecycleStatus.NeedsMapping;
        public const string ValidationErrors = DynamicReportLifecycleStatus.ValidationErrors;
        public const string ReadyForActivation = DynamicReportLifecycleStatus.ReadyForActivation;
        public const string Active = DynamicReportLifecycleStatus.Active;
        public const string Disabled = DynamicReportLifecycleStatus.Disabled;
        public const string Archived = DynamicReportLifecycleStatus.Archived;
    }

    public static class DynamicReportCertificationLevel
    {
        public const string Internal = "Internal";
        public const string Reviewed = "Reviewed";
        public const string ProductionReady = "ProductionReady";
        public const string Certified = "Certified";
    }

    public class ValidationReport
    {
        public ValidationReport()
        {
            CheckResults = new List<ValidationCheckResult>();
            ExecStats = new ExecStats();
            ValidatedAt = DateTime.Now;
        }

        public IList<ValidationCheckResult> CheckResults { get; set; }
        public int ErrorCount { get; set; }
        public int WarningCount { get; set; }
        public int InfoCount { get; set; }
        public ExecStats ExecStats { get; set; }
        public DateTime ValidatedAt { get; set; }
    }

    public class ValidationCheckResult
    {
        public string Id { get; set; }
        public string Level { get; set; }
        public string Message { get; set; }
        public string Hint { get; set; }
    }

    public class ExecStats
    {
        public int Ms { get; set; }
        public int RowCount { get; set; }
        public bool Truncated { get; set; }
        public int ColumnCount { get; set; }
        public string Error { get; set; }
    }

    public class LifecycleResult
    {
        public bool Success { get; set; }
        public string NewLifecycleStatus { get; set; }
        public string NewCertificationLevel { get; set; }
        public string Message { get; set; }
        public ValidationReport LastValidation { get; set; }
    }

    public class ReviewPageModel
    {
        public DynamicReportDefinition Definition { get; set; }
        public CatalogEntry CatalogEntry { get; set; }
        public string RiskFlags { get; set; }
        public string ApiBase { get; set; }
        public string Scope { get; set; }
        public int CurrentUserId { get; set; }
    }
}
