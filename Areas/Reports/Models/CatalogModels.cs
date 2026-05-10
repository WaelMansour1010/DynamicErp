using System;
using System.Collections.Generic;

namespace MyERP.Areas.Reports.Models
{
    public class CatalogEntry
    {
        public int CatalogId { get; set; }
        public string ProjectScope { get; set; }
        public string SourceType { get; set; }
        public string SourceSchema { get; set; }
        public string SourceName { get; set; }
        public DateTime DiscoveredAt { get; set; }
        public DateTime LastSeenAt { get; set; }
        public string ClassificationStatus { get; set; }
        public int ClassificationScore { get; set; }
        public string RiskFlags { get; set; }
        public string SuggestedReportName { get; set; }
        public int? ApprovedBy { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public string RejectionReason { get; set; }
        public int? ImportedReportId { get; set; }
        public DateTime? ImportedAt { get; set; }
        public string Notes { get; set; }
    }

    public class CatalogDiscoveryResult
    {
        public CatalogDiscoveryResult()
        {
            Errors = new List<string>();
        }

        public int DiscoveredCount { get; set; }
        public int UpdatedCount { get; set; }
        public int SkippedMsShipped { get; set; }
        public int ErrorCount { get; set; }
        public IList<string> Errors { get; set; }
    }

    public class ClassificationResult
    {
        public ClassificationResult()
        {
            RiskFlags = new List<string>();
        }

        public string Status { get; set; }
        public int Score { get; set; }
        public IList<string> RiskFlags { get; set; }
    }

    public class CatalogDetail
    {
        public CatalogDetail()
        {
            Entry = new CatalogEntry();
            Columns = new List<DynamicReportColumn>();
            Parameters = new List<DynamicReportParameter>();
        }

        public CatalogEntry Entry { get; set; }
        public IList<DynamicReportColumn> Columns { get; set; }
        public IList<DynamicReportParameter> Parameters { get; set; }
        public string BodyExcerpt { get; set; }
    }

    public class CatalogImportResult
    {
        public int NewReportId { get; set; }
        public string ReportCode { get; set; }
        public string Message { get; set; }
        public bool AlreadyImported { get; set; }
    }
}
