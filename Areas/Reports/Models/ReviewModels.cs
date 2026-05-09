using System;
using System.Collections.Generic;

namespace MyERP.Areas.Reports.Models
{
    public static class LifecycleStatusEnum
    {
        public const string Draft = "Draft";
        public const string NeedsMapping = "NeedsMapping";
        public const string ValidationErrors = "ValidationErrors";
        public const string ReadyForActivation = "ReadyForActivation";
        public const string Active = "Active";
        public const string Disabled = "Disabled";
        public const string Archived = "Archived";
    }

    public class ValidationReport
    {
        public ValidationReport()
        {
            CheckResults = new List<ValidationCheckResult>();
            ExecStats = new ExecStats();
        }

        public IList<ValidationCheckResult> CheckResults { get; set; }
        public int ErrorCount { get; set; }
        public int WarningCount { get; set; }
        public int InfoCount { get; set; }
        public ExecStats ExecStats { get; set; }
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
            SortHints = new List<SortHint>();
            GroupableHints = new List<string>();
        }

        public IDictionary<string, string> CaptionsAr { get; set; }
        public IDictionary<string, ColumnFormatting> Formatting { get; set; }
        public IList<SortHint> SortHints { get; set; }
        public IList<string> GroupableHints { get; set; }
    }

    public class ColumnFormatting
    {
        public string Format { get; set; }
        public int? Decimals { get; set; }
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
        public string NewStatus { get; set; }
        public string Message { get; set; }
        public IList<string> Errors { get; set; }
    }

    public class ApplySuggestionsRequest
    {
        public bool ApplyCaptions { get; set; }
        public bool ApplyFormatting { get; set; }
        public bool ApplySort { get; set; }
        public bool ApplyGroupable { get; set; }
        public string Field { get; set; }
        public string Kind { get; set; }
    }

    public class ReviewPageModel
    {
        public DynamicReportDefinition Definition { get; set; }
        public SuggestionBundle Suggestions { get; set; }
        public string RiskFlags { get; set; }
        public string ApiBase { get; set; }
        public string Scope { get; set; }
    }
}
