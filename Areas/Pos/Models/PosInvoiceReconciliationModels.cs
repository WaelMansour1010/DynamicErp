using System;
using System.Collections.Generic;

namespace MyERP.Areas.Pos.Models
{
    public class PosInvoiceReconciliationIndexViewModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public int? BranchId { get; set; }
        public string ServiceType { get; set; }
        public int? UserId { get; set; }
        public string ImportSource { get; set; }
        public string Month { get; set; }
        public decimal? MinAmount { get; set; }
        public decimal? MaxAmount { get; set; }
        public string Token { get; set; }
        public string Phone { get; set; }
        public string NationalId { get; set; }
        public string CustomerName { get; set; }
        public string RiskLevel { get; set; }
        public string SearchTerm { get; set; }
        public bool OnlyBothSources { get; set; }
        public bool SuspiciousOnly { get; set; }
        public bool CanDeleteInvoices { get; set; }
        public IList<PosBranchDto> Branches { get; set; }
        public IList<PosPermissionUserDto> Users { get; set; }
        public PosInvoiceReconciliationResult Result { get; set; }
        public PosSavedInvoiceReconciliationResult SavedResult { get; set; }
        public string ErrorMessage { get; set; }

        public PosInvoiceReconciliationIndexViewModel()
        {
            FromDate = DateTime.Today;
            ToDate = DateTime.Today;
            Branches = new List<PosBranchDto>();
            Users = new List<PosPermissionUserDto>();
            ImportSource = "Both";
        }
    }

    public class PosExcelInvoiceRow
    {
        public string SheetName { get; set; }
        public int RowNumber { get; set; }
        public IDictionary<string, string> Values { get; set; }

        public PosExcelInvoiceRow()
        {
            Values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }

    public class PosExcelInvoiceNormalizedRow
    {
        public string SheetName { get; set; }
        public int RowNumber { get; set; }
        public DateTime? InvoiceDate { get; set; }
        public string InvoiceDateText { get; set; }
        public string InvoiceNumber { get; set; }
        public string Token { get; set; }
        public string Phone { get; set; }
        public string CustomerName { get; set; }
        public string NationalId { get; set; }
        public decimal? Amount { get; set; }
        public string ServiceType { get; set; }
        public string Branch { get; set; }
        public string Store { get; set; }
        public string User { get; set; }
        public string Notes { get; set; }
        public IList<string> Warnings { get; set; }
        public IList<string> Errors { get; set; }

        public PosExcelInvoiceNormalizedRow()
        {
            Warnings = new List<string>();
            Errors = new List<string>();
        }
    }

    public class PosInvoiceDbMatch
    {
        public int TransactionId { get; set; }
        public DateTime? InvoiceDate { get; set; }
        public string InvoiceNumber { get; set; }
        public string ManualNo { get; set; }
        public string Token { get; set; }
        public string Phone { get; set; }
        public string CustomerName { get; set; }
        public string NationalId { get; set; }
        public decimal Amount { get; set; }
        public decimal NetAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public string ServiceType { get; set; }
        public int? BranchId { get; set; }
        public string BranchName { get; set; }
        public string StoreName { get; set; }
        public string UserName { get; set; }
        public int DetailRows { get; set; }
        public int TokenDetailRows { get; set; }
        public bool HasIssueVoucher { get; set; }
        public bool IsIssueVoucher { get; set; }
    }

    public class PosInvoiceReconciliationResult
    {
        public string SourceFileName { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public int? BranchId { get; set; }
        public string ServiceType { get; set; }
        public IList<PosInvoiceReconciliationColumnMapping> ColumnMappings { get; set; }
        public IList<PosExcelInvoiceNormalizedRow> InvalidRows { get; set; }
        public IList<PosInvoiceReconciliationRow> Rows { get; set; }
        public PosInvoiceReconciliationSummary Summary { get; set; }

        public PosInvoiceReconciliationResult()
        {
            ColumnMappings = new List<PosInvoiceReconciliationColumnMapping>();
            InvalidRows = new List<PosExcelInvoiceNormalizedRow>();
            Rows = new List<PosInvoiceReconciliationRow>();
            Summary = new PosInvoiceReconciliationSummary();
        }
    }

    public class PosInvoiceReconciliationColumnMapping
    {
        public string FieldKey { get; set; }
        public string FieldTitle { get; set; }
        public string HeaderText { get; set; }
        public int? ColumnIndex { get; set; }
        public string Confidence { get; set; }
    }

    public class PosInvoiceReconciliationSummary
    {
        public int TotalExcelRows { get; set; }
        public int ValidRows { get; set; }
        public int InvalidRows { get; set; }
        public int ExactMatches { get; set; }
        public int ProbableDuplicates { get; set; }
        public int PossibleMatches { get; set; }
        public int NotFound { get; set; }
        public int AmountMismatches { get; set; }
        public int CustomerMismatches { get; set; }
        public int DateMismatches { get; set; }
        public int DatabaseDuplicates { get; set; }
        public int ExcelDuplicates { get; set; }
    }

    public class PosInvoiceReconciliationRow
    {
        public int ExcelRowNumber { get; set; }
        public string SheetName { get; set; }
        public string Status { get; set; }
        public int MatchScore { get; set; }
        public string Reason { get; set; }
        public DateTime? ExcelInvoiceDate { get; set; }
        public DateTime? DbInvoiceDate { get; set; }
        public string ExcelInvoiceNumber { get; set; }
        public int? DbTransactionId { get; set; }
        public string DbSerial { get; set; }
        public string Token { get; set; }
        public string Phone { get; set; }
        public string ExcelCustomerName { get; set; }
        public string DbCustomerName { get; set; }
        public decimal? ExcelAmount { get; set; }
        public decimal? DbAmount { get; set; }
        public decimal? Difference { get; set; }
        public string ExcelBranch { get; set; }
        public string DbBranch { get; set; }
        public string ExcelUser { get; set; }
        public string DbUser { get; set; }
        public string ServiceType { get; set; }
        public IList<string> Warnings { get; set; }

        public PosInvoiceReconciliationRow()
        {
            Warnings = new List<string>();
        }
    }

    public class PosInvoiceReconciliationRequest
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public int? BranchId { get; set; }
        public string ServiceType { get; set; }
        public int UserId { get; set; }
        public int? TellerUserId { get; set; }
        public string ImportSource { get; set; }
        public string Month { get; set; }
        public decimal? MinAmount { get; set; }
        public decimal? MaxAmount { get; set; }
        public string Token { get; set; }
        public string Phone { get; set; }
        public string NationalId { get; set; }
        public string CustomerName { get; set; }
        public string RiskLevel { get; set; }
        public string SearchTerm { get; set; }
        public bool OnlyBothSources { get; set; }
        public bool SuspiciousOnly { get; set; }
        public bool CanSeeAllBranches { get; set; }
    }

    public class PosSavedInvoiceReconciliationResult
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public int? BranchId { get; set; }
        public int? TellerUserId { get; set; }
        public string ServiceType { get; set; }
        public string ImportSource { get; set; }
        public string Month { get; set; }
        public decimal? MinAmount { get; set; }
        public decimal? MaxAmount { get; set; }
        public string Token { get; set; }
        public string Phone { get; set; }
        public string NationalId { get; set; }
        public string CustomerName { get; set; }
        public string RiskLevel { get; set; }
        public string SearchTerm { get; set; }
        public bool OnlyBothSources { get; set; }
        public bool SuspiciousOnly { get; set; }
        public bool CanDeleteInvoices { get; set; }
        public bool HasUncertainSource { get; set; }
        public string SourceDetectionWarning { get; set; }
        public PosSavedInvoicePeriodSummary PeriodSummary { get; set; }
        public IList<PosSavedInvoiceBranchSummary> BranchSummaries { get; set; }
        public IList<PosSavedInvoiceDaySummary> DaySummaries { get; set; }
        public IList<PosSavedInvoiceDimensionSummary> ServiceTypeSummaries { get; set; }
        public IList<PosSavedInvoiceDimensionSummary> TellerSummaries { get; set; }
        public IList<PosSavedInvoiceDimensionSummary> SourceSummaries { get; set; }
        public IList<PosSavedInvoiceDayBranchSummary> DayBranchSummaries { get; set; }
        public IList<PosSavedInvoiceListItem> ExcelInvoices { get; set; }
        public IList<PosSavedInvoiceListItem> SystemInvoices { get; set; }
        public IList<PosSavedInvoiceDuplicatePair> DuplicatePairs { get; set; }
        public IList<PosSavedInvoiceDuplicatePair> TokenConflicts { get; set; }
        public IList<PosReconciliationActionLogItem> ActionLog { get; set; }

        public PosSavedInvoiceReconciliationResult()
        {
            PeriodSummary = new PosSavedInvoicePeriodSummary();
            BranchSummaries = new List<PosSavedInvoiceBranchSummary>();
            DaySummaries = new List<PosSavedInvoiceDaySummary>();
            ServiceTypeSummaries = new List<PosSavedInvoiceDimensionSummary>();
            TellerSummaries = new List<PosSavedInvoiceDimensionSummary>();
            SourceSummaries = new List<PosSavedInvoiceDimensionSummary>();
            DayBranchSummaries = new List<PosSavedInvoiceDayBranchSummary>();
            ExcelInvoices = new List<PosSavedInvoiceListItem>();
            SystemInvoices = new List<PosSavedInvoiceListItem>();
            DuplicatePairs = new List<PosSavedInvoiceDuplicatePair>();
            TokenConflicts = new List<PosSavedInvoiceDuplicatePair>();
            ActionLog = new List<PosReconciliationActionLogItem>();
            ImportSource = "Both";
        }
    }

    public class PosSavedInvoicePeriodSummary
    {
        public int ExcelImportedCount { get; set; }
        public decimal ExcelImportedAmount { get; set; }
        public int NormalPosCount { get; set; }
        public decimal NormalPosAmount { get; set; }
        public int CountDifference { get; set; }
        public decimal AmountDifference { get; set; }
        public int SuspiciousDuplicatePairs { get; set; }
        public int HighRiskPairs { get; set; }
        public string RiskLevel { get; set; }
        public string Reason { get; set; }
    }

    public class PosSavedInvoiceBranchSummary
    {
        public int? BranchId { get; set; }
        public string BranchName { get; set; }
        public int ExcelImportedCount { get; set; }
        public decimal ExcelImportedAmount { get; set; }
        public int NormalPosCount { get; set; }
        public decimal NormalPosAmount { get; set; }
        public int CountDifference { get; set; }
        public decimal AmountDifference { get; set; }
        public int SuspiciousDuplicatePairs { get; set; }
        public string RiskLevel { get; set; }
        public string Reason { get; set; }
    }

    public class PosSavedInvoiceDaySummary
    {
        public DateTime Date { get; set; }
        public int ExcelImportedCount { get; set; }
        public decimal ExcelImportedAmount { get; set; }
        public int NormalPosCount { get; set; }
        public decimal NormalPosAmount { get; set; }
        public int CountDifference { get; set; }
        public decimal AmountDifference { get; set; }
        public int SuspiciousDuplicatePairs { get; set; }
        public string RiskLevel { get; set; }
        public string Reason { get; set; }
    }

    public class PosSavedInvoiceDimensionSummary
    {
        public string DimensionKey { get; set; }
        public string DimensionName { get; set; }
        public int ExcelImportedCount { get; set; }
        public decimal ExcelImportedAmount { get; set; }
        public int NormalPosCount { get; set; }
        public decimal NormalPosAmount { get; set; }
        public int CountDifference { get; set; }
        public decimal AmountDifference { get; set; }
        public int SuspiciousDuplicatePairs { get; set; }
        public string RiskLevel { get; set; }
        public string Reason { get; set; }
    }

    public class PosSavedInvoiceDayBranchSummary
    {
        public DateTime Date { get; set; }
        public int? BranchId { get; set; }
        public string BranchName { get; set; }
        public string ServiceType { get; set; }
        public int ExcelImportedCount { get; set; }
        public decimal ExcelImportedAmount { get; set; }
        public int NormalPosCount { get; set; }
        public decimal NormalPosAmount { get; set; }
        public int CountDifference { get; set; }
        public decimal AmountDifference { get; set; }
        public int PotentialDuplicateMatches { get; set; }
        public string RiskLevel { get; set; }
        public string Reason { get; set; }
    }

    public class PosSavedInvoiceListItem
    {
        public int TransactionId { get; set; }
        public DateTime? InvoiceDate { get; set; }
        public string Serial { get; set; }
        public string ManualNo { get; set; }
        public string Token { get; set; }
        public string Phone { get; set; }
        public string CustomerName { get; set; }
        public string NationalId { get; set; }
        public decimal Amount { get; set; }
        public string ServiceType { get; set; }
        public int? BranchId { get; set; }
        public string BranchName { get; set; }
        public int? UserId { get; set; }
        public string UserName { get; set; }
        public string Source { get; set; }
        public string SourceConfidence { get; set; }
        public long? ImportBatchId { get; set; }
        public string SourceFileName { get; set; }
        public string SourceSheet { get; set; }
        public int? SourceRow { get; set; }
        public string RiskLevel { get; set; }
        public string ReviewStatus { get; set; }
    }

    public class PosReconciliationDeleteRequest
    {
        public IList<int> TransactionIds { get; set; }
        public string Password { get; set; }
        public string Reason { get; set; }

        public PosReconciliationDeleteRequest()
        {
            TransactionIds = new List<int>();
        }
    }

    public class PosReconciliationActionLogItem
    {
        public DateTime ActionDate { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; }
        public string ActionType { get; set; }
        public int TransactionId { get; set; }
        public string Source { get; set; }
        public string Reason { get; set; }
        public bool PasswordVerified { get; set; }
        public string ResultMessage { get; set; }
    }

    public class PosSavedInvoiceDuplicatePair
    {
        public int ExcelTransactionId { get; set; }
        public DateTime? ExcelInvoiceDate { get; set; }
        public string ExcelSerial { get; set; }
        public long? ExcelImportBatchId { get; set; }
        public string ExcelSourceFileName { get; set; }
        public string ExcelSourceSheet { get; set; }
        public int? ExcelSourceRow { get; set; }
        public string ExcelSource { get; set; }
        public string ExcelSourceConfidence { get; set; }
        public int NormalTransactionId { get; set; }
        public DateTime? NormalInvoiceDate { get; set; }
        public string NormalSerial { get; set; }
        public string NormalSource { get; set; }
        public string NormalSourceConfidence { get; set; }
        public string ServiceType { get; set; }
        public string BranchName { get; set; }
        public string ExcelUserName { get; set; }
        public string NormalUserName { get; set; }
        public string ExcelToken { get; set; }
        public string NormalToken { get; set; }
        public string ExcelPhone { get; set; }
        public string NormalPhone { get; set; }
        public string ExcelNationalId { get; set; }
        public string NormalNationalId { get; set; }
        public string ExcelCustomerName { get; set; }
        public string NormalCustomerName { get; set; }
        public decimal ExcelAmount { get; set; }
        public decimal NormalAmount { get; set; }
        public decimal AmountDifference { get; set; }
        public double? TimeDifferenceMinutes { get; set; }
        public int MatchScore { get; set; }
        public string MatchClassification { get; set; }
        public IList<string> Reasons { get; set; }
        public IList<string> Warnings { get; set; }

        public PosSavedInvoiceDuplicatePair()
        {
            Reasons = new List<string>();
            Warnings = new List<string>();
        }
    }
}
