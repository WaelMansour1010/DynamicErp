using System;
using System.Collections.Generic;

namespace MyERP.Common.EnterpriseHr
{
    public class LegacyHrFinancePageViewModel
    {
        public LegacyHrFinancePageViewModel()
        {
            Metrics = new List<LegacyHrFinanceMetricViewModel>();
            Rows = new List<LegacyHrFinanceRowViewModel>();
            Components = new List<PayrollComponentEditViewModel>();
            ChangedComponents = new List<ChangedComponentEntryViewModel>();
            Advances = new List<EmployeeAdvanceViewModel>();
            Vacations = new List<EmployeeVacationRequestViewModel>();
            VacationTypes = new List<EnterpriseHrLookupItemViewModel>();
            Employees = new List<EnterpriseHrEmployeeLookupViewModel>();
            Branches = new List<EnterpriseHrLookupViewModel>();
            Departments = new List<EnterpriseHrLookupViewModel>();
            Permissions = new LegacyHrFinancePermissionsViewModel();
        }

        public string ModuleKey { get; set; }
        public string Title { get; set; }
        public string SourceSystem { get; set; }
        public string SourceForm { get; set; }
        public string LegacyTable { get; set; }
        public string Warning { get; set; }
        public string SearchText { get; set; }
        public string EmployeeStatus { get; set; }
        public string ScreenKey { get; set; }
        public string HostContext { get; set; }
        public string ComponentDetailsUrl { get; set; }
        public string SaveComponentUrl { get; set; }
        public string ChangedComponentDetailsUrl { get; set; }
        public string SaveChangedComponentUrl { get; set; }
        public string DeleteChangedComponentUrl { get; set; }
        public string PreviewChangedComponentBulkUrl { get; set; }
        public string SaveChangedComponentBulkUrl { get; set; }
        public string AdvanceDetailsUrl { get; set; }
        public string SaveAdvanceUrl { get; set; }
        public string DeleteAdvanceUrl { get; set; }
        public string DisburseAdvanceUrl { get; set; }
        public string SendAdvanceForApprovalUrl { get; set; }
        public string ApproveAdvanceUrl { get; set; }
        public string CancelAdvanceUrl { get; set; }
        public string AdvanceAccountingBoundaryUrl { get; set; }
        public string VacationBalanceUrl { get; set; }
        public string VacationDetailsUrl { get; set; }
        public string SaveVacationUrl { get; set; }
        public string ManagerApproveVacationUrl { get; set; }
        public string HrApproveVacationUrl { get; set; }
        public string RejectVacationUrl { get; set; }
        public string CancelVacationUrl { get; set; }
        public string DeleteVacationUrl { get; set; }
        public string CreateVacationEntitlementUrl { get; set; }
        public string DeleteVacationEntitlementUrl { get; set; }
        public string SaveVacationReturnToWorkUrl { get; set; }
        public string DeleteVacationReturnToWorkUrl { get; set; }
        public string EmployeeLookupUrl { get; set; }
        public string PayrollRunUrl { get; set; }
        public int? EmployeeId { get; set; }
        public int? ComponentId { get; set; }
        public int? BranchId { get; set; }
        public int? DepartmentId { get; set; }
        public int? YearFilter { get; set; }
        public int? MonthFilter { get; set; }
        public string ComponentType { get; set; }
        public string StatusFilter { get; set; }
        public string DateFrom { get; set; }
        public string DateTo { get; set; }
        public string AdvanceStatus { get; set; }
        public string VacationStatus { get; set; }
        public string VacationType { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public IList<LegacyHrFinanceMetricViewModel> Metrics { get; set; }
        public IList<LegacyHrFinanceRowViewModel> Rows { get; set; }
        public IList<PayrollComponentEditViewModel> Components { get; set; }
        public IList<ChangedComponentEntryViewModel> ChangedComponents { get; set; }
        public IList<EmployeeAdvanceViewModel> Advances { get; set; }
        public IList<EmployeeVacationRequestViewModel> Vacations { get; set; }
        public IList<EnterpriseHrLookupItemViewModel> VacationTypes { get; set; }
        public IList<EnterpriseHrEmployeeLookupViewModel> Employees { get; set; }
        public IList<EnterpriseHrLookupViewModel> Branches { get; set; }
        public IList<EnterpriseHrLookupViewModel> Departments { get; set; }
        public LegacyHrFinancePermissionsViewModel Permissions { get; set; }
    }

    public class EnterpriseHrLookupItemViewModel
    {
        public string Value { get; set; }
        public string Text { get; set; }
    }

    public class EnterpriseHrLookupViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class LegacyHrFinanceMetricViewModel
    {
        public string Label { get; set; }
        public string Value { get; set; }
        public string Hint { get; set; }
    }

    public class LegacyHrFinanceRowViewModel
    {
        public LegacyHrFinanceRowViewModel()
        {
            Tags = new List<string>();
        }

        public int Id { get; set; }
        public string Primary { get; set; }
        public string Secondary { get; set; }
        public string Status { get; set; }
        public string Amount { get; set; }
        public string Period { get; set; }
        public string Details { get; set; }
        public IList<string> Tags { get; set; }
    }

    public class PayrollComponentEditViewModel
    {
        public int? Id { get; set; }
        public string Name { get; set; }
        public string NameEnglish { get; set; }
        public bool AddOrDiscount { get; set; }
        public bool FixedOrChanged { get; set; }
        public int? Unit { get; set; }
        public string AccountCode { get; set; }
        public string AccountCode1 { get; set; }
        public bool ViewComponent { get; set; }
        public bool Salary { get; set; }
        public bool Absence { get; set; }
        public bool Late { get; set; }
        public bool Overtime { get; set; }
        public bool Insurance { get; set; }
        public bool Reward { get; set; }
        public int? AllowIntroduction { get; set; }
    }

    public class ChangedComponentEntryViewModel
    {
        public int? Id { get; set; }
        public int? HeaderId { get; set; }
        public int? EmployeeId { get; set; }
        public string EmployeeCode { get; set; }
        public string EmployeeName { get; set; }
        public int? BranchId { get; set; }
        public string BranchName { get; set; }
        public int? DepartmentId { get; set; }
        public string DepartmentName { get; set; }
        public int? ProjectId { get; set; }
        public string ProjectName { get; set; }
        public int? ComponentId { get; set; }
        public string ComponentName { get; set; }
        public bool AddOrDiscount { get; set; }
        public int? Unit { get; set; }
        public string RecordDate { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
        public decimal Value { get; set; }
        public decimal NoOfDays { get; set; }
        public decimal NoOfHours { get; set; }
        public decimal NoOfMinutes { get; set; }
        public decimal HourRate { get; set; }
        public decimal Salary { get; set; }
        public string Remarks { get; set; }
        public int DetailCount { get; set; }
        public bool PayrollUsed { get; set; }
        public int? PayrollRunId { get; set; }
        public string PayrollRunName { get; set; }
        public bool PayrollRunPosted { get; set; }
        public string PayrollUsageSource { get; set; }
        public bool IsLegacyBulk { get; set; }
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }
        public string LockReason { get; set; }
    }

    public class ChangedComponentBulkRequestViewModel
    {
        public ChangedComponentBulkRequestViewModel()
        {
            Entries = new List<ChangedComponentEntryViewModel>();
        }

        public string Mode { get; set; }
        public string EmployeeTokens { get; set; }
        public int? ComponentId { get; set; }
        public int? SourceComponentId { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
        public int? SourceYear { get; set; }
        public int? SourceMonth { get; set; }
        public string RecordDate { get; set; }
        public decimal Value { get; set; }
        public decimal NoOfDays { get; set; }
        public decimal NoOfHours { get; set; }
        public decimal NoOfMinutes { get; set; }
        public decimal HourRate { get; set; }
        public decimal Salary { get; set; }
        public int? ProjectId { get; set; }
        public string Remarks { get; set; }
        public IList<ChangedComponentEntryViewModel> Entries { get; set; }
    }

    public class ChangedComponentBulkPreviewRowViewModel
    {
        public int RowNo { get; set; }
        public int? EmployeeId { get; set; }
        public string EmployeeCode { get; set; }
        public string EmployeeName { get; set; }
        public int? ComponentId { get; set; }
        public string ComponentName { get; set; }
        public string ComponentType { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
        public decimal Value { get; set; }
        public bool IsValid { get; set; }
        public string Status { get; set; }
        public string Message { get; set; }
    }

    public class ChangedComponentBulkPreviewViewModel
    {
        public ChangedComponentBulkPreviewViewModel()
        {
            Rows = new List<ChangedComponentBulkPreviewRowViewModel>();
            Entries = new List<ChangedComponentEntryViewModel>();
        }

        public bool Success { get; set; }
        public string Message { get; set; }
        public int TotalRows { get; set; }
        public int ValidRows { get; set; }
        public int InvalidRows { get; set; }
        public decimal TotalValue { get; set; }
        public IList<ChangedComponentBulkPreviewRowViewModel> Rows { get; set; }
        public IList<ChangedComponentEntryViewModel> Entries { get; set; }
    }

    public class EmployeeAdvanceViewModel
    {
        public int? Id { get; set; }
        public int? EmployeeId { get; set; }
        public string EmployeeCode { get; set; }
        public string EmployeeName { get; set; }
        public int? BranchId { get; set; }
        public string BranchName { get; set; }
        public int? DepartmentId { get; set; }
        public string DepartmentName { get; set; }
        public string AdvanceDate { get; set; }
        public decimal AdvanceValue { get; set; }
        public int PaymentCounts { get; set; }
        public int? FirstMonthPayment { get; set; }
        public int? FirstYearPayment { get; set; }
        public string FirstDate { get; set; }
        public bool AutoDiscount { get; set; }
        public bool Approved { get; set; }
        public bool Submitted { get; set; }
        public bool Posted { get; set; }
        public bool AccountingApproved { get; set; }
        public bool Rejected { get; set; }
        public string StatusText { get; set; }
        public string Reason { get; set; }
        public decimal BasicSalary { get; set; }
        public decimal OldAdvance { get; set; }
        public decimal Balance { get; set; }
        public int PaidPartsCount { get; set; }
        public int PartsCount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal RemainingAmount { get; set; }
        public decimal InstallmentsTotal { get; set; }
        public decimal MonthlyDueAmount { get; set; }
        public bool InstallmentsValid { get; set; }
        public string InstallmentValidationMessage { get; set; }
        public int? ActualAdvanceId { get; set; }
        public bool IsDisbursed { get; set; }
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }
        public bool CanDisburse { get; set; }
        public bool CanSendApproval { get; set; }
        public bool CanApprove { get; set; }
        public bool CanCancel { get; set; }
        public string LockReason { get; set; }
        public IList<EmployeeAdvancePartViewModel> Parts { get; set; }
        public IList<EmployeeAdvanceApprovalHistoryViewModel> ApprovalHistory { get; set; }

        public EmployeeAdvanceViewModel()
        {
            Parts = new List<EmployeeAdvancePartViewModel>();
            ApprovalHistory = new List<EmployeeAdvanceApprovalHistoryViewModel>();
        }
    }

    public class EmployeeAdvancePartViewModel
    {
        public int? TableId { get; set; }
        public int PartNo { get; set; }
        public decimal PartValue { get; set; }
        public string PartDate { get; set; }
        public bool Payed { get; set; }
        public string PaidDate { get; set; }
        public string StatusText { get; set; }
        public decimal RemainingValue { get; set; }
        public int? PayrollRunId { get; set; }
        public int? PayrollDeductionId { get; set; }
        public bool PayrollLinked { get; set; }
        public bool PayrollPosted { get; set; }
        public string PayrollPostedAt { get; set; }
        public bool Locked { get; set; }
        public string Remark { get; set; }
    }

    public class EmployeeAdvanceApprovalHistoryViewModel
    {
        public int Id { get; set; }
        public string ScreenName { get; set; }
        public int? Level { get; set; }
        public int? EmployeeId { get; set; }
        public string EmployeeName { get; set; }
        public string ApprovedAt { get; set; }
        public string CancelledAt { get; set; }
        public string Remarks { get; set; }
        public string FromUser { get; set; }
        public bool IsCurrentCursor { get; set; }
    }

    public class AdvanceAccountingBoundaryViewModel
    {
        public int RequestId { get; set; }
        public int? ActualAdvanceId { get; set; }
        public bool HasActualAdvance { get; set; }
        public bool CreatesJournalOnDisbursement { get; set; }
        public bool CreatesPaymentVoucherOnDisbursement { get; set; }
        public bool CanCreateFinancePaymentVoucher { get; set; }
        public string BoundaryStatus { get; set; }
        public string BoundaryMessage { get; set; }
        public int? NoteId { get; set; }
        public string NoteSerial { get; set; }
        public string VoucherSerial { get; set; }
        public int NormalJournalLineCount { get; set; }
        public int OpeningJournalLineCount { get; set; }
        public int PayrollDeductionLineCount { get; set; }
        public int PostedPayrollDeductionLineCount { get; set; }
        public decimal PayrollDeductionTotal { get; set; }
        public bool HasPayrollDeduction { get; set; }
        public bool HasAnyAccountingTrace { get; set; }
        public bool HasUnsupportedAccountingTrace { get; set; }
        public string UnsupportedReason { get; set; }
        public IList<AdvancePayrollDeductionTraceViewModel> PayrollDeductionLinks { get; set; }
        public IList<AdvanceJournalTraceViewModel> DirectJournalTraces { get; set; }
        public IList<AdvanceJournalTraceViewModel> OpeningBalanceTraces { get; set; }

        public AdvanceAccountingBoundaryViewModel()
        {
            PayrollDeductionLinks = new List<AdvancePayrollDeductionTraceViewModel>();
            DirectJournalTraces = new List<AdvanceJournalTraceViewModel>();
            OpeningBalanceTraces = new List<AdvanceJournalTraceViewModel>();
        }
    }

    public class AdvancePayrollDeductionTraceViewModel
    {
        public int PayrollRunId { get; set; }
        public string RunName { get; set; }
        public int? PeriodYear { get; set; }
        public int? PeriodMonth { get; set; }
        public int PartNo { get; set; }
        public string PartDate { get; set; }
        public decimal PartValue { get; set; }
        public bool IsPosted { get; set; }
        public string PostedAt { get; set; }
        public int? NoteId { get; set; }
        public string StatusText { get; set; }
    }

    public class AdvanceJournalTraceViewModel
    {
        public int? NoteId { get; set; }
        public string NoteSerial { get; set; }
        public string AccountSerial { get; set; }
        public string AccountName { get; set; }
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public string Description { get; set; }
    }

    public class EmployeeVacationRequestViewModel
    {
        public EmployeeVacationRequestViewModel()
        {
            ApprovalHistory = new List<EmployeeAdvanceApprovalHistoryViewModel>();
        }

        public int? Id { get; set; }
        public int? EmployeeId { get; set; }
        public string EmployeeCode { get; set; }
        public string EmployeeName { get; set; }
        public int? BranchId { get; set; }
        public string BranchName { get; set; }
        public int? DepartmentId { get; set; }
        public string DepartmentName { get; set; }
        public int? ManagerId { get; set; }
        public string ManagerName { get; set; }
        public int? JobId { get; set; }
        public string JobName { get; set; }
        public string RecordDate { get; set; }
        public string FromDate { get; set; }
        public string ToDate { get; set; }
        public string ResumeWork { get; set; }
        public decimal NoVacation { get; set; }
        public string Reason { get; set; }
        public string VacationType { get; set; }
        public bool WithSalary { get; set; }
        public bool WithoutSalary { get; set; }
        public bool ManagerApproved { get; set; }
        public bool HrApproved { get; set; }
        public bool Submitted { get; set; }
        public bool Rejected { get; set; }
        public bool PaidOrSettled { get; set; }
        public bool LinkedToEntitlement { get; set; }
        public int? EntitlementId { get; set; }
        public int? EmbarkationId { get; set; }
        public string ActualReturnDate { get; set; }
        public decimal ActualVacationDays { get; set; }
        public decimal DelayDays { get; set; }
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }
        public bool CanCreateEntitlement { get; set; }
        public bool CanDeleteEntitlement { get; set; }
        public bool CanRecordReturnToWork { get; set; }
        public bool CanDeleteReturnToWork { get; set; }
        public bool CanManagerApprove { get; set; }
        public bool CanHrApprove { get; set; }
        public bool CanReject { get; set; }
        public bool CanCancel { get; set; }
        public string StatusText { get; set; }
        public string LockReason { get; set; }
        public IList<EmployeeAdvanceApprovalHistoryViewModel> ApprovalHistory { get; set; }
    }

    public class VacationReturnToWorkViewModel
    {
        public int? EntitlementId { get; set; }
        public string ActualReturnDate { get; set; }
        public decimal? ActualVacationDays { get; set; }
        public decimal? DelayDays { get; set; }
        public string DelayTreatment { get; set; }
        public string Remarks { get; set; }
    }

    public class VacationBalanceRequestViewModel
    {
        public int EmployeeId { get; set; }
        public string AsOfDate { get; set; }
        public string VacationStartDate { get; set; }
        public string VacationEndDate { get; set; }
        public decimal? RequestedDays { get; set; }
        public int? ExcludeVacationId { get; set; }
        public int? ExcludeEntitlementId { get; set; }
    }

    public class VacationBalanceViewModel
    {
        public VacationBalanceViewModel()
        {
            Lines = new List<VacationBalanceLineViewModel>();
            Warnings = new List<string>();
            Errors = new List<string>();
        }

        public int EmployeeId { get; set; }
        public string EmployeeCode { get; set; }
        public string EmployeeName { get; set; }
        public bool IsEmployeeActive { get; set; }
        public string EmployeeStatusMessage { get; set; }
        public string AsOfDate { get; set; }
        public string CalculationMode { get; set; }
        public decimal AnnualEntitlementDays { get; set; }
        public decimal AccruedDays { get; set; }
        public decimal OpeningBalanceDays { get; set; }
        public decimal CarryOverDays { get; set; }
        public decimal ScheduledDueDays { get; set; }
        public decimal PaidVacationConsumedDays { get; set; }
        public decimal PendingApprovedDays { get; set; }
        public decimal UnpaidLeaveDays { get; set; }
        public decimal AbsenceDeductionDays { get; set; }
        public decimal RequestedDays { get; set; }
        public decimal AvailableBeforeRequest { get; set; }
        public decimal AvailableAfterRequest { get; set; }
        public bool NegativeBalancePrevented { get; set; }
        public bool CanPostPaidVacation { get; set; }
        public bool HasOverlappingVacationWarning { get; set; }
        public string OverlappingVacationMessage { get; set; }
        public IList<VacationBalanceLineViewModel> Lines { get; set; }
        public IList<string> Warnings { get; set; }
        public IList<string> Errors { get; set; }
    }

    public class VacationBalanceLineViewModel
    {
        public string Source { get; set; }
        public string SourceId { get; set; }
        public string SourceDate { get; set; }
        public string Description { get; set; }
        public decimal Days { get; set; }
        public string Effect { get; set; }
    }

    public class EnterpriseHrEmployeeLookupViewModel
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public int? BranchId { get; set; }
        public string BranchName { get; set; }
        public int? DepartmentId { get; set; }
        public string DepartmentName { get; set; }
        public decimal BasicSalary { get; set; }
    }

    public class LegacyHrFinancePermissionsViewModel
    {
        public bool CanView { get; set; }
        public bool CanAdd { get; set; }
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }
        public bool CanPrint { get; set; }
        public bool CanExport { get; set; }
    }

    public class LegacyHrFinanceSaveResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public int? Id { get; set; }
    }
}
