using System;
using System.Collections.Generic;

namespace MyERP.Areas.Pos.Models
{
    public class PosExcelImportIndexViewModel
    {
        public IList<PosExcelImportBatchSummaryDto> Batches { get; set; }

        public PosExcelImportIndexViewModel()
        {
            Batches = new List<PosExcelImportBatchSummaryDto>();
        }
    }

    public class PosExcelImportBatchSummaryDto
    {
        public long BatchId { get; set; }
        public string SourceFileName { get; set; }
        public string Status { get; set; }
        public int ImportedInvoicesCount { get; set; }
        public int FailedRowsCount { get; set; }
        public DateTime? CreatedAt { get; set; }
        public string CreatedBy { get; set; }
    }

    public class PosExcelImportPreviewViewModel
    {
        public PosExcelImportPreviewResult Preview { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class PosExcelImportPreviewResult
    {
        public string SourceFileName { get; set; }
        public string SourceFileHash { get; set; }
        public string WorkbookType { get; set; }
        public string DetectedBranchHint { get; set; }
        public string TokenMatchingStrategy { get; set; }
        public IList<PosExcelImportSheetSummary> Sheets { get; set; }
        public IList<PosExcelImportRowPreview> Rows { get; set; }
        public IList<PosExcelImportTokenPreview> Tokens { get; set; }
        public IList<PosExcelImportTokenMatchPreview> TokenMatches { get; set; }
        public IList<PosExcelImportPreflightItem> PreflightItems { get; set; }
        public IList<string> Warnings { get; set; }
        public int ReadyCount { get; set; }
        public int WarningCount { get; set; }
        public int RejectedCount { get; set; }
        public int UnmatchedTokenCount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal TotalFees { get; set; }
        public decimal TotalGross { get; set; }

        public PosExcelImportPreviewResult()
        {
            Sheets = new List<PosExcelImportSheetSummary>();
            Rows = new List<PosExcelImportRowPreview>();
            Tokens = new List<PosExcelImportTokenPreview>();
            TokenMatches = new List<PosExcelImportTokenMatchPreview>();
            PreflightItems = new List<PosExcelImportPreflightItem>();
            Warnings = new List<string>();
            TokenMatchingStrategy = "Sequential";
        }
    }

    public class PosExcelImportSheetSummary
    {
        public string SheetName { get; set; }
        public string SheetDateText { get; set; }
        public int TransactionRows { get; set; }
        public int TokenRows { get; set; }
        public decimal SheetTotal { get; set; }
    }

    public class PosExcelImportRowPreview
    {
        public string SheetName { get; set; }
        public int RowNumber { get; set; }
        public string SequenceNo { get; set; }
        public string IPN { get; set; }
        public string CustomerName { get; set; }
        public string Phone { get; set; }
        public decimal? Amount { get; set; }
        public decimal? Fee { get; set; }
        public decimal? GrossTotal { get; set; }
        public string ServiceType { get; set; }
        public DateTime? TransactionDate { get; set; }
        public string TransactionDateText { get; set; }
        public string Status { get; set; }
        public IList<string> Reasons { get; set; }

        public PosExcelImportRowPreview()
        {
            Reasons = new List<string>();
            Status = "Ready";
        }
    }

    public class PosExcelImportTokenPreview
    {
        public string SheetName { get; set; }
        public int RowNumber { get; set; }
        public string Token { get; set; }
        public string Status { get; set; }
        public IList<string> Reasons { get; set; }

        public PosExcelImportTokenPreview()
        {
            Reasons = new List<string>();
            Status = "Unmatched";
        }
    }

    public class PosExcelImportTokenMatchPreview
    {
        public string SheetName { get; set; }
        public int SourceRowNumber { get; set; }
        public string IPN { get; set; }
        public string CustomerName { get; set; }
        public decimal? GrossTotal { get; set; }
        public int TokenRowNumber { get; set; }
        public string Token { get; set; }
        public string MatchStatus { get; set; }
        public string Strategy { get; set; }
    }

    public class PosExcelImportPreflightItem
    {
        public string FieldName { get; set; }
        public string Status { get; set; }
        public string Value { get; set; }
        public string Message { get; set; }
    }

    public class PosExcelImportMappingDraft
    {
        public string BranchHint { get; set; }
        public int? BranchId { get; set; }
        public int? StoreId { get; set; }
        public int? ImportUserId { get; set; }
        public int? PaymentTypeId { get; set; }
        public IDictionary<string, int?> ServiceItemMap { get; set; }

        public PosExcelImportMappingDraft()
        {
            ServiceItemMap = new Dictionary<string, int?>(StringComparer.OrdinalIgnoreCase);
        }
    }
}
