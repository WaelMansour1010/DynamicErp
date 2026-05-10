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

    public class SuggestionBundle
    {
        public SuggestionBundle()
        {
            CaptionsAr = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            Formatting = new Dictionary<string, ColumnFormatting>(StringComparer.OrdinalIgnoreCase);
            Widths = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            Alignment = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            AggregateFunctions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            SortHints = new List<SortHint>();
            GroupableHints = new List<string>();
            FilterableHints = new List<string>();
            SortableHints = new List<string>();
        }

        public IDictionary<string, string> CaptionsAr { get; set; }
        public IDictionary<string, ColumnFormatting> Formatting { get; set; }
        public IDictionary<string, int> Widths { get; set; }
        public IDictionary<string, string> Alignment { get; set; }
        public IDictionary<string, string> AggregateFunctions { get; set; }
        public IList<SortHint> SortHints { get; set; }
        public IList<string> GroupableHints { get; set; }
        public IList<string> FilterableHints { get; set; }
        public IList<string> SortableHints { get; set; }
    }

    public class ColumnFormatting
    {
        public string Format { get; set; }
        public int? Decimals { get; set; }
        public string TextAlign { get; set; }
        public int? Width { get; set; }
        public string AggregateFunction { get; set; }
    }

    public class SortHint
    {
        public string Field { get; set; }
        public string Direction { get; set; }
    }

    public class LifecycleResult
    {
        public LifecycleResult()
        {
            Errors = new List<string>();
        }

        public bool Success { get; set; }
        public string NewLifecycleStatus { get; set; }
        public string NewStatus { get; set; }
        public string NewCertificationLevel { get; set; }
        public string Message { get; set; }
        public ValidationReport LastValidation { get; set; }
        public IList<string> Errors { get; set; }
    }

    public class ApplySuggestionsRequest
    {
        public bool ApplyCaptions { get; set; }
        public bool ApplyFormatting { get; set; }
        public bool ApplyWidthAlignment { get; set; }
        public bool ApplySort { get; set; }
        public bool ApplyGroupable { get; set; }
        public bool ApplyFilterable { get; set; }
        public bool ApplySortable { get; set; }
        public bool ApplyAggregate { get; set; }
        public string Field { get; set; }
        public string Kind { get; set; }
    }

    public class ReviewPageModel
    {
        public DynamicReportDefinition Definition { get; set; }
        public SuggestionBundle Suggestions { get; set; }
        public CatalogEntry CatalogEntry { get; set; }
        public string RiskFlags { get; set; }
        public string ApiBase { get; set; }
        public string Scope { get; set; }
        public int CurrentUserId { get; set; }
    }
}
