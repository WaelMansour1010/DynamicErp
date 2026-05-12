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
        public PosExcelImportCommitResult CommitResult { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class PosExcelImportPreviewResult
    {
        public string SourceFileName { get; set; }
        public string SourceFileHash { get; set; }
        public string StoredWorkbookPath { get; set; }
        public string WorkbookType { get; set; }
        public string DetectedBranchHint { get; set; }
        public IList<string> WorkbookBranchHints { get; set; }
        public string TokenMatchingStrategy { get; set; }
        public IList<PosExcelImportSheetSummary> Sheets { get; set; }
        public IList<PosExcelImportRowPreview> Rows { get; set; }
        public IList<PosExcelImportTokenPreview> Tokens { get; set; }
        public IList<PosExcelImportTokenMatchPreview> TokenMatches { get; set; }
        public IList<PosExcelImportPreflightItem> PreflightItems { get; set; }
        public IList<PosExcelImportBranchCandidate> BranchCandidates { get; set; }
        public IList<string> Warnings { get; set; }
        public PosExcelImportDetectedBranch DetectedBranch { get; set; }
        public PosExcelImportDefaultContext EffectiveDefaults { get; set; }
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
            BranchCandidates = new List<PosExcelImportBranchCandidate>();
            WorkbookBranchHints = new List<string>();
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
        public string InternalServiceType { get; set; }
        public string InternalServiceName { get; set; }
        public int? ServiceItemId { get; set; }
        public string ServiceItemName { get; set; }
        public string MatchedToken { get; set; }
        public bool RequiresKycCreation { get; set; }
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

    public class PosExcelImportBranchCandidate
    {
        public int BranchId { get; set; }
        public string BranchCode { get; set; }
        public string BranchName { get; set; }
        public string MatchRule { get; set; }
    }

    public class PosExcelImportDetectedBranch
    {
        public int BranchId { get; set; }
        public string BranchCode { get; set; }
        public string BranchName { get; set; }
        public string MatchRule { get; set; }
    }

    public class PosExcelImportDefaultContext
    {
        public int? UserId { get; set; }
        public string UserName { get; set; }
        public int? EmpId { get; set; }
        public string EmpName { get; set; }
        public int? BranchId { get; set; }
        public string BranchName { get; set; }
        public int? StoreId { get; set; }
        public string StoreName { get; set; }
        public int? BoxId { get; set; }
        public string BoxName { get; set; }
        public int? PaymentNetId { get; set; }
        public int? PaymentTypeId { get; set; }
        public string PaymentName { get; set; }
        public int? BankId { get; set; }
        public string BankName { get; set; }
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

    public class PosExcelImportCommitResult
    {
        public long BatchId { get; set; }
        public string Status { get; set; }
        public int ImportedCount { get; set; }
        public int FailedCount { get; set; }
        public int SkippedCount { get; set; }
        public string MarkedWorkbookFileName { get; set; }
        public IList<PosExcelImportCommitRowResult> Rows { get; set; }

        public PosExcelImportCommitResult()
        {
            Rows = new List<PosExcelImportCommitRowResult>();
            Status = "Pending";
        }
    }

    public class PosExcelImportCommitRowResult
    {
        public string SheetName { get; set; }
        public int RowNumber { get; set; }
        public string IPN { get; set; }
        public string ServiceType { get; set; }
        public string Status { get; set; }
        public int? TransactionId { get; set; }
        public string NoteSerial1 { get; set; }
        public string Message { get; set; }
    }

    public class PosExcelImportCommitProgress
    {
        public string JobId { get; set; }
        public string Status { get; set; }
        public int TotalCount { get; set; }
        public int ProcessedCount { get; set; }
        public int ImportedCount { get; set; }
        public int FailedCount { get; set; }
        public int SkippedCount { get; set; }
        public string CurrentSheet { get; set; }
        public int CurrentRowNumber { get; set; }
        public string CurrentServiceType { get; set; }
        public string CurrentMessage { get; set; }
        public string MarkedWorkbookFileName { get; set; }
        public PosExcelImportCommitResult Result { get; set; }

        public int Percent
        {
            get
            {
                if (TotalCount <= 0)
                {
                    return 0;
                }

                var value = (int)Math.Round((ProcessedCount * 100.0) / TotalCount, MidpointRounding.AwayFromZero);
                if (value < 0) { return 0; }
                if (value > 100) { return 100; }
                return value;
            }
        }
    }

    public class PosExcelImportRollbackResult
    {
        public long BatchId { get; set; }
        public string Status { get; set; }
        public int RolledBackCount { get; set; }
        public int FailedCount { get; set; }
        public string ClearedWorkbookFileName { get; set; }
        public IList<PosExcelImportCommitRowResult> Rows { get; set; }

        public PosExcelImportRollbackResult()
        {
            Status = "Pending";
            Rows = new List<PosExcelImportCommitRowResult>();
        }
    }

    public class PosExcelImportOverlapResult
    {
        public bool HasOverlap { get; set; }
        public int InvoiceCount { get; set; }
        public long? BatchId { get; set; }
        public string SourceFileName { get; set; }
        public DateTime? ExistingFromDate { get; set; }
        public DateTime? ExistingToDate { get; set; }
    }
}
