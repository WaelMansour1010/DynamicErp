using System;
using System.Collections.Generic;

namespace MyERP.Common.EmployeePayroll
{
    public class EmployeePayrollLookups
    {
        public IList<LookupItem> Branches { get; set; }
        public IList<LookupItem> Departments { get; set; }
        public IList<LookupItem> Jobs { get; set; }
        public IList<LookupItem> MedicalInsuranceProviders { get; set; }
        public IList<LookupItem> MedicalInsurancePlans { get; set; }

        public EmployeePayrollLookups()
        {
            Branches = new List<LookupItem>();
            Departments = new List<LookupItem>();
            Jobs = new List<LookupItem>();
            MedicalInsuranceProviders = new List<LookupItem>();
            MedicalInsurancePlans = new List<LookupItem>();
        }
    }

    public class LookupItem
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class EmployeeSearchFilter
    {
        public string Term { get; set; }
        public int? BranchId { get; set; }
        public int? DepartmentId { get; set; }
        public bool? IsActive { get; set; }
    }

    public class EmployeeSummary
    {
        public int EmployeeId { get; set; }
        public string EmployeeCode { get; set; }
        public string EmployeeName { get; set; }
        public int? BranchId { get; set; }
        public string BranchName { get; set; }
        public int? DepartmentId { get; set; }
        public string DepartmentName { get; set; }
        public int? JobTypeId { get; set; }
        public string JobTypeName { get; set; }
        public DateTime? HiringDate { get; set; }
        public bool IsActive { get; set; }
        public decimal BasicSalary { get; set; }
        public string AccountCode { get; set; }
        public string AccruedSalaryAccountCode { get; set; }
        public string VacationProvisionAccountCode { get; set; }
        public string AdvancePaymentAccountCode { get; set; }
        public string EndOfServiceAccountCode { get; set; }
        public string TicketProvisionAccountCode { get; set; }
        public string Phone { get; set; }
        public string Mobile { get; set; }
        public string Email { get; set; }
        public string Notes { get; set; }
        public EmployeeMedicalInsurance MedicalInsurance { get; set; }
        public IList<EmployeeMedicalInsurance> MedicalInsuranceHistory { get; set; }

        public EmployeeSummary()
        {
            MedicalInsuranceHistory = new List<EmployeeMedicalInsurance>();
        }
    }

    public class EmployeeSaveRequest
    {
        public int? EmployeeId { get; set; }
        public string EmployeeCode { get; set; }
        public string EmployeeName { get; set; }
        public int? BranchId { get; set; }
        public int? DepartmentId { get; set; }
        public int? JobTypeId { get; set; }
        public DateTime? HiringDate { get; set; }
        public bool IsActive { get; set; }
        public decimal BasicSalary { get; set; }
        public string AccountCode { get; set; }
        public string AccruedSalaryAccountCode { get; set; }
        public string Phone { get; set; }
        public string Mobile { get; set; }
        public string Email { get; set; }
        public string Notes { get; set; }
        public EmployeeMedicalInsurance MedicalInsurance { get; set; }
    }

    public class MedicalInsuranceProvider
    {
        public int? ProviderId { get; set; }
        public string ProviderNameAr { get; set; }
        public string ProviderNameEn { get; set; }
        public string Phone { get; set; }
        public string Notes { get; set; }
        public bool IsActive { get; set; }
    }

    public class MedicalInsurancePlan
    {
        public int? PlanId { get; set; }
        public int ProviderId { get; set; }
        public string ProviderName { get; set; }
        public string PlanNameAr { get; set; }
        public string PlanNameEn { get; set; }
        public decimal DefaultMonthlyCost { get; set; }
        public string DefaultEmployeeShareType { get; set; }
        public decimal DefaultEmployeeShareValue { get; set; }
        public string DefaultCompanyShareType { get; set; }
        public decimal DefaultCompanyShareValue { get; set; }
        public string EmployeeDeductionAccountCode { get; set; }
        public string CompanyCostAccountCode { get; set; }
        public string LifecycleStatus { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime? PayrollStartDate { get; set; }
        public DateTime? SuspensionDate { get; set; }
        public DateTime? CancellationDate { get; set; }
        public string CostCenterCode { get; set; }
        public string PayrollDeductionType { get; set; }
        public bool IsMonthlyDeduction { get; set; }
        public bool AutoStopAtEndDate { get; set; }
        public bool ShowInPayroll { get; set; }
        public bool DistributeByDepartment { get; set; }
        public bool DistributeByCostCenter { get; set; }
        public string TaxMode { get; set; }
        public int MaxDependents { get; set; }
        public int ChildrenMaxAge { get; set; }
        public decimal SpouseAdditionalCost { get; set; }
        public decimal ChildAdditionalCost { get; set; }
        public decimal ParentAdditionalCost { get; set; }
        public decimal DefaultCoveragePercent { get; set; }
        public int AutoEnrollAfterDays { get; set; }
        public string AutoEnrollCriteria { get; set; }
        public string RulesJson { get; set; }
        public string DependentsTemplateJson { get; set; }
        public bool IsActive { get; set; }
        public string Notes { get; set; }
    }

    public class EmployeeMedicalInsurance
    {
        public int? Id { get; set; }
        public int EmployeeId { get; set; }
        public int? PlanId { get; set; }
        public string PlanName { get; set; }
        public string ProviderName { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IsMonthly { get; set; }
        public bool IsActive { get; set; }
        public decimal MonthlyCost { get; set; }
        public string EmployeeShareType { get; set; }
        public decimal EmployeeShareValue { get; set; }
        public string CompanyShareType { get; set; }
        public decimal CompanyShareValue { get; set; }
        public decimal EmployeeMonthlyDeduction { get; set; }
        public decimal CompanyMonthlyCost { get; set; }
        public string LifecycleStatus { get; set; }
        public DateTime? PayrollStartDate { get; set; }
        public DateTime? SuspensionDate { get; set; }
        public DateTime? CancellationDate { get; set; }
        public string CostCenterCode { get; set; }
        public string PayrollDeductionType { get; set; }
        public bool AutoStopAtEndDate { get; set; }
        public bool ShowInPayroll { get; set; }
        public string TaxMode { get; set; }
        public string Notes { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public decimal Amount
        {
            get { return EmployeeShareValue; }
            set { EmployeeShareValue = value; }
        }

        public decimal PercentValue
        {
            get { return EmployeeShareValue; }
            set { EmployeeShareValue = value; }
        }

        public string DeductionType
        {
            get { return EmployeeShareType; }
            set { EmployeeShareType = value; }
        }
    }

    public class MedicalInsuranceCalculation
    {
        public decimal EmployeeDeduction { get; set; }
        public decimal CompanyCost { get; set; }
    }

    public class SalaryRunRequest
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public int? BranchId { get; set; }
        public int? DepartmentId { get; set; }
        public int? EmployeeId { get; set; }
        public bool IncludeSavedDrafts { get; set; }
        public int RowLimit { get; set; }
        public int JournalPreviewLimit { get; set; }
    }

    public class SalaryRunPreview
    {
        public SalaryRunRequest Request { get; set; }
        public IList<SalaryRunEmployeeRow> Rows { get; set; }
        public IList<SalaryRunJournalLine> JournalPreview { get; set; }
        public IList<PayrollCompatibilityWarning> CompatibilityWarnings { get; set; }
        public decimal TotalBasic { get; set; }
        public decimal TotalAdditions { get; set; }
        public decimal TotalDeductions { get; set; }
        public decimal TotalMedicalInsurance { get; set; }
        public decimal TotalMedicalInsuranceCompanyCost { get; set; }
        public decimal TotalAdvance { get; set; }
        public decimal TotalNet { get; set; }
        public int TotalRows { get; set; }
        public int TotalJournalPreviewRows { get; set; }
        public bool PayloadIsTruncated { get; set; }
        public bool HasExistingApprovedRows { get; set; }
        public string Message { get; set; }

        public SalaryRunPreview()
        {
            Rows = new List<SalaryRunEmployeeRow>();
            JournalPreview = new List<SalaryRunJournalLine>();
            CompatibilityWarnings = new List<PayrollCompatibilityWarning>();
        }
    }

    public class SalaryRunEmployeeRow
    {
        public bool Selected { get; set; }
        public int EmployeeId { get; set; }
        public string EmployeeCode { get; set; }
        public string EmployeeName { get; set; }
        public int? BranchId { get; set; }
        public string BranchName { get; set; }
        public int? DepartmentId { get; set; }
        public string DepartmentName { get; set; }
        public int? ProjectId { get; set; }
        public string ProjectName { get; set; }
        public string ProjectSalaryAccountCode { get; set; }
        public decimal BasicSalary { get; set; }
        public decimal SalaryAllowances { get; set; }
        public decimal VariableAdditions { get; set; }
        public decimal TotalBeforeDeductions { get; set; }
        public decimal AdvanceDeduction { get; set; }
        public decimal ExistingDiscounts { get; set; }
        public int? MedicalInsuranceId { get; set; }
        public string MedicalInsurancePlanName { get; set; }
        public decimal MedicalInsuranceMonthlyCost { get; set; }
        public decimal MedicalInsuranceDeduction { get; set; }
        public decimal MedicalInsuranceCompanyCost { get; set; }
        public decimal TotalDeductions { get; set; }
        public decimal NetSalary { get; set; }
        public int? ExistingSalaryRowId { get; set; }
        public bool IsApproved { get; set; }
        public string EmployeeAccountCode { get; set; }
        public string AccruedSalaryAccountCode { get; set; }
        public string AdvancePaymentAccountCode { get; set; }
        public string VacationProvisionAccountCode { get; set; }
        public bool IsLegacySnapshot { get; set; }
        public decimal CountDays { get; set; }
        public decimal AbsentDays { get; set; }
        public decimal RemainingDays { get; set; }
        public decimal VacationDeduction { get; set; }
        public decimal TotalInsuranceLegacy { get; set; }
        public string CompatibilityStatus { get; set; }
        public DateTime? HiringDate { get; set; }
        public DateTime? LastHolidayDate { get; set; }
        public bool MonthIs30Days { get; set; }
        public int? PayrollMonthDays { get; set; }
        public int PayrollSalaryDigits { get; set; }
        public PayrollCompatibilityInsuranceTrace InsuranceTrace { get; set; }
        public IList<PayrollCompatibilityComponent> Components { get; set; }

        public SalaryRunEmployeeRow()
        {
            Components = new List<PayrollCompatibilityComponent>();
        }
    }

    public class PayrollCompatibilityComponent
    {
        public int ComponentNo { get; set; }
        public string ComponentColumn { get; set; }
        public string ComponentNameAr { get; set; }
        public string ComponentNameEn { get; set; }
        public bool AddOrDiscount { get; set; }
        public bool FixedOrChanged { get; set; }
        public bool ViewComponent { get; set; }
        public bool ZmamAccount { get; set; }
        public bool AdvancePaymentAccount { get; set; }
        public bool Insurances { get; set; }
        public bool ShowMofradAll { get; set; }
        public int? Culc30OrReminder { get; set; }
        public string AccountCode { get; set; }
        public string AccountCode1 { get; set; }
        public decimal SnapshotValue { get; set; }
        public decimal SourceValue { get; set; }
        public decimal RawSourceValue { get; set; }
        public decimal TemporalAdjustedValue { get; set; }
        public decimal FixedSourceValue { get; set; }
        public decimal ChangedSourceValue { get; set; }
        public decimal OverrideSourceValue { get; set; }
        public string SourceKind { get; set; }
        public string PrecedenceDecision { get; set; }
        public bool TemporalCountFlag { get; set; }
        public bool TemporalProrationApplied { get; set; }
        public bool TemporalProrationBypassed { get; set; }
        public decimal TemporalNumeratorDays { get; set; }
        public decimal TemporalDenominatorDays { get; set; }
        public decimal TemporalMonthDayNo { get; set; }
        public decimal TemporalActualMonthDays { get; set; }
        public string TemporalRulePath { get; set; }
        public string TemporalDenominatorReason { get; set; }
    }

    public class PayrollCompatibilityWarning
    {
        public string Code { get; set; }
        public string Message { get; set; }
        public int? EmployeeId { get; set; }
    }

    public class PayrollCompatibilityParityReport
    {
        public SalaryRunRequest Request { get; set; }
        public IList<PayrollCompatibilityParityRow> Rows { get; set; }
        public int TotalRows { get; set; }
        public int LegacySnapshotRows { get; set; }
        public int ReconstructedRows { get; set; }
        public int ComponentMismatchRows { get; set; }
        public int TotalMismatchRows { get; set; }
        public decimal LegacyNetTotal { get; set; }
        public decimal ReconstructedNetTotal { get; set; }
        public decimal NetDifference { get; set; }
        public string SafetyStatus { get; set; }

        public PayrollCompatibilityParityReport()
        {
            Rows = new List<PayrollCompatibilityParityRow>();
        }
    }

    public class PayrollCompatibilityParityRow
    {
        public int EmployeeId { get; set; }
        public string EmployeeCode { get; set; }
        public string EmployeeName { get; set; }
        public string CompatibilityStatus { get; set; }
        public bool IsLegacySnapshot { get; set; }
        public int ComponentMismatchCount { get; set; }
        public decimal LegacyTotal1 { get; set; }
        public decimal ReconstructedTotal1 { get; set; }
        public decimal Total1Diff { get; set; }
        public decimal LegacyTotal2 { get; set; }
        public decimal ReconstructedTotal2 { get; set; }
        public decimal Total2Diff { get; set; }
        public decimal LegacyInsurance { get; set; }
        public decimal RuntimeInsurance { get; set; }
        public string InsuranceSource { get; set; }
        public decimal LegacyNet { get; set; }
        public decimal ReconstructedNet { get; set; }
        public decimal NetDiff { get; set; }
        public IList<PayrollCompatibilityComponentDiff> ComponentDiffs { get; set; }

        public PayrollCompatibilityParityRow()
        {
            ComponentDiffs = new List<PayrollCompatibilityComponentDiff>();
        }
    }

    public class PayrollCompatibilityComponentDiff
    {
        public int ComponentNo { get; set; }
        public string ComponentColumn { get; set; }
        public string ComponentNameAr { get; set; }
        public decimal LegacyValue { get; set; }
        public decimal ReconstructedValue { get; set; }
        public decimal Difference { get; set; }
        public string SourceKind { get; set; }
        public string MismatchCategory { get; set; }
        public string LikelySource { get; set; }
        public string PrecedenceDecision { get; set; }
        public decimal ConfidenceScore { get; set; }
    }

    public class PayrollCompatibilityExplainRequest : SalaryRunRequest
    {
        public int ComponentNo { get; set; }
    }

    public class PayrollCompatibilityExplainResult
    {
        public int EmployeeId { get; set; }
        public string EmployeeCode { get; set; }
        public string EmployeeName { get; set; }
        public int ComponentNo { get; set; }
        public string ComponentColumn { get; set; }
        public string ComponentNameAr { get; set; }
        public string CompatibilityStatus { get; set; }
        public decimal LegacyValue { get; set; }
        public decimal ReconstructedValue { get; set; }
        public decimal Difference { get; set; }
        public decimal FixedSourceValue { get; set; }
        public decimal ChangedSourceValue { get; set; }
        public decimal OverrideSourceValue { get; set; }
        public decimal RawSourceValue { get; set; }
        public decimal TemporalAdjustedValue { get; set; }
        public string PrecedenceDecision { get; set; }
        public string MismatchCategory { get; set; }
        public string LikelySource { get; set; }
        public decimal ConfidenceScore { get; set; }
        public PayrollCompatibilityProrationTrace Proration { get; set; }
        public PayrollCompatibilityInsuranceTrace Insurance { get; set; }
        public IList<string> Explanation { get; set; }

        public PayrollCompatibilityExplainResult()
        {
            Explanation = new List<string>();
        }
    }

    public class PayrollCompatibilityProrationTrace
    {
        public decimal CountDays { get; set; }
        public decimal AbsentDays { get; set; }
        public decimal RemainingDays { get; set; }
        public DateTime? HiringDate { get; set; }
        public DateTime? LastHolidayDate { get; set; }
        public bool MonthIs30Days { get; set; }
        public int? PayrollMonthDays { get; set; }
        public int PayrollSalaryDigits { get; set; }
        public bool ShowMofradAll { get; set; }
        public int? Culc30OrReminder { get; set; }
        public decimal CalendarMonthDays { get; set; }
        public decimal PayrollMonthDayNo { get; set; }
        public decimal PayrollDays { get; set; }
        public decimal ActualDenominator { get; set; }
        public decimal ExpectedDenominator { get; set; }
        public decimal NumeratorDays { get; set; }
        public bool CountFlag { get; set; }
        public bool ProrationApplied { get; set; }
        public bool ProrationBypassed { get; set; }
        public bool VacationOverlap { get; set; }
        public string BranchProjectScope { get; set; }
        public string DenominatorReason { get; set; }
        public string RulePath { get; set; }
        public string ProrationCategory { get; set; }
    }

    public class PayrollCompatibilityInsuranceTrace
    {
        public int EmployeeId { get; set; }
        public string SourceProject { get; set; }
        public string SourceForm { get; set; }
        public string SourceFunction { get; set; }
        public string SourceTables { get; set; }
        public decimal SnapshotToalInsurance { get; set; }
        public decimal RuntimeFunctionInsurance { get; set; }
        public decimal TBLInsurancesJoinTotal { get; set; }
        public decimal TBLInsurancesJoinBase { get; set; }
        public decimal CompanyRate { get; set; }
        public decimal WorkDays { get; set; }
        public int? InsuranceId { get; set; }
        public int? InsuranceMonth { get; set; }
        public int? InsuranceYear { get; set; }
        public bool IsPaid { get; set; }
        public string Nationality { get; set; }
        public decimal InsuranceComponentBase { get; set; }
        public decimal CitizenPercent { get; set; }
        public decimal ResidentPercent { get; set; }
        public string EmployeeAccruedSalaryAccount { get; set; }
        public string InsuranceCreditAccount { get; set; }
        public string EmployerDebitAccount { get; set; }
        public string EmployerCreditAccount { get; set; }
        public string PostingRule { get; set; }
        public string ExclusionReason { get; set; }
        public decimal InsuranceAdjustedTotal { get; set; }
    }

    public class PayrollAccountingParityTraceRequest : SalaryRunRequest
    {
        public int? NoteType { get; set; }
    }

    public class PayrollAccountingParityTrace
    {
        public SalaryRunRequest Request { get; set; }
        public string SourceProject { get; set; }
        public string SourceForm { get; set; }
        public string SourceModule { get; set; }
        public string SafetyStatus { get; set; }
        public int NotesCount { get; set; }
        public decimal NotesTotal { get; set; }
        public int VoucherLineCount { get; set; }
        public decimal VoucherDebitTotal { get; set; }
        public decimal VoucherCreditTotal { get; set; }
        public decimal VoucherBalance { get; set; }
        public IList<PayrollAccountingNoteTrace> Notes { get; set; }
        public IList<PayrollAccountingVoucherTrace> VoucherLines { get; set; }

        public PayrollAccountingParityTrace()
        {
            Notes = new List<PayrollAccountingNoteTrace>();
            VoucherLines = new List<PayrollAccountingVoucherTrace>();
        }
    }

    public class PayrollAccountingNoteTrace
    {
        public int NoteId { get; set; }
        public int? NoteType { get; set; }
        public string Salary { get; set; }
        public DateTime? NoteDate { get; set; }
        public string NoteSerial { get; set; }
        public decimal NoteValue { get; set; }
        public int? BranchId { get; set; }
        public string Remark { get; set; }
    }

    public class PayrollAccountingVoucherTrace
    {
        public int VoucherId { get; set; }
        public int? NoteId { get; set; }
        public int? LineNo { get; set; }
        public string AccountCode { get; set; }
        public decimal Value { get; set; }
        public bool IsCredit { get; set; }
        public int? BranchId { get; set; }
        public int? DepartmentId { get; set; }
        public int? EmployeeId { get; set; }
        public int? ProjectId { get; set; }
        public string Description { get; set; }
    }

    public class PayrollAccountingReplayRequest : SalaryRunRequest
    {
        public int? NoteType { get; set; }
        public bool IncludeLineDetails { get; set; }
    }

    public class PayrollAccountingReplayReport
    {
        public SalaryRunRequest Request { get; set; }
        public string SourceProject { get; set; }
        public string SourceForm { get; set; }
        public string SourceModule { get; set; }
        public string SafetyStatus { get; set; }
        public IList<PayrollReplayedNote> ReplayedNotes { get; set; }
        public IList<PayrollReplayedVoucherLine> ReplayedLines { get; set; }
        public PayrollAccountingParityTrace LegacyTrace { get; set; }
        public IList<PayrollAccountingReplayComparison> AccountComparisons { get; set; }
        public IList<PayrollAccountingReplayComparison> BranchComparisons { get; set; }
        public IList<PayrollAccountingReplayComparison> DepartmentComparisons { get; set; }
        public IList<PayrollAccountingReplayComparison> ProjectComparisons { get; set; }
        public IList<PayrollDistributionMismatchSummary> DistributionMismatchCategories { get; set; }
        public IList<PayrollLegacyConsistencySummary> LegacyConsistencySummaries { get; set; }
        public PayrollDistributionOptions DistributionOptions { get; set; }
        public decimal ReplayedDebitTotal { get; set; }
        public decimal ReplayedCreditTotal { get; set; }
        public decimal LegacyDebitTotal { get; set; }
        public decimal LegacyCreditTotal { get; set; }
        public decimal DebitDifference { get; set; }
        public decimal CreditDifference { get; set; }
        public decimal ReplayedBalance { get; set; }
        public decimal LegacyBalance { get; set; }

        public PayrollAccountingReplayReport()
        {
            ReplayedNotes = new List<PayrollReplayedNote>();
            ReplayedLines = new List<PayrollReplayedVoucherLine>();
            AccountComparisons = new List<PayrollAccountingReplayComparison>();
            BranchComparisons = new List<PayrollAccountingReplayComparison>();
            DepartmentComparisons = new List<PayrollAccountingReplayComparison>();
            ProjectComparisons = new List<PayrollAccountingReplayComparison>();
            DistributionMismatchCategories = new List<PayrollDistributionMismatchSummary>();
            LegacyConsistencySummaries = new List<PayrollLegacyConsistencySummary>();
        }
    }

    public class PayrollReplayedNote
    {
        public int? LegacyNoteId { get; set; }
        public int NoteType { get; set; }
        public string Salary { get; set; }
        public int? BranchId { get; set; }
        public decimal NoteValue { get; set; }
        public string Rule { get; set; }
        public string Explanation { get; set; }
    }

    public class PayrollReplayedVoucherLine
    {
        public int LineNo { get; set; }
        public string AccountCode { get; set; }
        public decimal Value { get; set; }
        public bool IsCredit { get; set; }
        public int? BranchId { get; set; }
        public int? DepartmentId { get; set; }
        public int? ProjectId { get; set; }
        public int? EmployeeId { get; set; }
        public int? ComponentNo { get; set; }
        public string ComponentName { get; set; }
        public string RuleId { get; set; }
        public string AccountRoutingPath { get; set; }
        public string Trigger { get; set; }
        public string Explanation { get; set; }
        public string AllocationSource { get; set; }
        public string BranchProjectDepartmentPath { get; set; }
        public string OverrideReason { get; set; }
        public string FallbackReason { get; set; }
        public string DistributionMismatchCategory { get; set; }
        public string LegacyBehaviorClassification { get; set; }
        public decimal StabilityScore { get; set; }
        public decimal HistoricalConsistencyScore { get; set; }
        public decimal ReplayConfidenceScore { get; set; }
        public bool IsHistoricallyDeterministic { get; set; }
        public bool IsHistoricallyInconsistent { get; set; }
        public bool IsSafeForFuturePosting { get; set; }
        public bool LikelyLegacyBug { get; set; }
        public bool LikelyOperationalWorkaround { get; set; }
        public string LegacyConsistencyExplanation { get; set; }
    }

    public class PayrollAccountingReplayComparison
    {
        public string Dimension { get; set; }
        public string Key { get; set; }
        public decimal LegacyDebit { get; set; }
        public decimal ReplayDebit { get; set; }
        public decimal DebitDiff { get; set; }
        public decimal LegacyCredit { get; set; }
        public decimal ReplayCredit { get; set; }
        public decimal CreditDiff { get; set; }
        public int LegacyLines { get; set; }
        public int ReplayLines { get; set; }
        public string MismatchCategory { get; set; }
        public string Explanation { get; set; }
        public string LegacyBehaviorClassification { get; set; }
        public decimal StabilityScore { get; set; }
        public decimal HistoricalConsistencyScore { get; set; }
        public decimal ReplayConfidenceScore { get; set; }
        public bool IsHistoricallyDeterministic { get; set; }
        public bool IsHistoricallyInconsistent { get; set; }
        public bool IsSafeForFuturePosting { get; set; }
        public bool LikelyLegacyBug { get; set; }
        public bool LikelyOperationalWorkaround { get; set; }
        public string Recommendation { get; set; }
    }

    public class PayrollTestPostingRequest : PayrollAccountingReplayRequest
    {
        public string Password { get; set; }
        public string ConfirmationPhrase { get; set; }
    }

    public class PayrollTestPostingCleanupRequest
    {
        public Guid TestPostingBatchId { get; set; }
        public string Password { get; set; }
        public string ConfirmationPhrase { get; set; }
    }

    public class PayrollTestPostingResult
    {
        public bool IsAllowedDatabase { get; set; }
        public bool IsDryRun { get; set; }
        public bool IsGenerated { get; set; }
        public bool IsCleaned { get; set; }
        public Guid? TestPostingBatchId { get; set; }
        public string DatabaseName { get; set; }
        public string SafetyStatus { get; set; }
        public string Message { get; set; }
        public int NotesCount { get; set; }
        public int VoucherLinesCount { get; set; }
        public int CleanedNotesCount { get; set; }
        public int CleanedVoucherLinesCount { get; set; }
        public decimal DebitTotal { get; set; }
        public decimal CreditTotal { get; set; }
        public decimal Balance { get; set; }
        public IList<PayrollTestPostingDimensionTotal> AffectedAccounts { get; set; }
        public IList<PayrollTestPostingDimensionTotal> AffectedBranches { get; set; }
        public IList<PayrollTestPostingDimensionTotal> AffectedProjects { get; set; }
        public IList<PayrollTestPostingDimensionTotal> AffectedDepartments { get; set; }
        public IList<string> Warnings { get; set; }

        public PayrollTestPostingResult()
        {
            AffectedAccounts = new List<PayrollTestPostingDimensionTotal>();
            AffectedBranches = new List<PayrollTestPostingDimensionTotal>();
            AffectedProjects = new List<PayrollTestPostingDimensionTotal>();
            AffectedDepartments = new List<PayrollTestPostingDimensionTotal>();
            Warnings = new List<string>();
        }
    }

    public class PayrollTestPostingDimensionTotal
    {
        public string Key { get; set; }
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public int Lines { get; set; }
    }

    public class PayrollLegacyConsistencySummary
    {
        public string RuleFamily { get; set; }
        public string AccountCode { get; set; }
        public int? ProjectId { get; set; }
        public int? BranchId { get; set; }
        public int PeriodsWithAllocation { get; set; }
        public int PeriodsWithVoucherFootprint { get; set; }
        public int PeriodsWithBoth { get; set; }
        public int PeriodsWithAllocationOnly { get; set; }
        public int PeriodsWithVoucherOnly { get; set; }
        public decimal AllocationTotal { get; set; }
        public decimal VoucherDebitTotal { get; set; }
        public decimal AmountDifference { get; set; }
        public decimal StabilityScore { get; set; }
        public decimal HistoricalConsistencyScore { get; set; }
        public decimal ReplayConfidenceScore { get; set; }
        public string LegacyBehaviorClassification { get; set; }
        public string Recommendation { get; set; }
        public string Explanation { get; set; }
    }

    public class PayrollDistributionOptions
    {
        public bool ProjectEmployeeGV { get; set; }
        public int ProjectDiscountPolicy { get; set; }
        public bool SalaryJournalByManagement { get; set; }
        public string SourceTable { get; set; }
        public string Explanation { get; set; }
    }

    public class PayrollDistributionMismatchSummary
    {
        public string Category { get; set; }
        public int Count { get; set; }
        public decimal DebitDiff { get; set; }
        public decimal CreditDiff { get; set; }
    }

    public class SalaryRunJournalLine
    {
        public string AccountCode { get; set; }
        public string AccountName { get; set; }
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public string Description { get; set; }
        public int? BranchId { get; set; }
        public int? DepartmentId { get; set; }
        public int? EmployeeId { get; set; }
    }

    public class SalaryRunSaveResult
    {
        public int InsertedRows { get; set; }
        public int UpdatedRows { get; set; }
        public decimal TotalNet { get; set; }
        public string Message { get; set; }
    }

    public class MedicalInsuranceReportFilter
    {
        public DateTime? PeriodFrom { get; set; }
        public DateTime? PeriodTo { get; set; }
        public int? ProviderId { get; set; }
        public int? PlanId { get; set; }
        public bool ActiveOnly { get; set; }
    }

    public class MedicalInsuranceSubscriptionReportRow
    {
        public int EmployeeId { get; set; }
        public string EmployeeCode { get; set; }
        public string EmployeeName { get; set; }
        public string ProviderName { get; set; }
        public string PlanName { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IsActive { get; set; }
        public decimal MonthlyCost { get; set; }
        public decimal EmployeeMonthlyDeduction { get; set; }
        public decimal CompanyMonthlyCost { get; set; }
    }

    public class MedicalInsuranceDeductionReportRow
    {
        public int EmployeeId { get; set; }
        public string EmployeeCode { get; set; }
        public string EmployeeName { get; set; }
        public string PlanName { get; set; }
        public DateTime PeriodFrom { get; set; }
        public DateTime PeriodTo { get; set; }
        public decimal EmployeeDeduction { get; set; }
        public decimal CompanyCost { get; set; }
    }

    public class MedicalInsuranceOperationalFilter
    {
        public string Term { get; set; }
        public int? BranchId { get; set; }
        public int? DepartmentId { get; set; }
        public string Status { get; set; }
        public int RenewalDays { get; set; }
    }

    public class MedicalInsuranceOperationalDashboard
    {
        public bool SchemaReady { get; set; }
        public string Message { get; set; }
        public int TotalEmployees { get; set; }
        public int UninsuredEmployees { get; set; }
        public int ActiveInsured { get; set; }
        public int Suspended { get; set; }
        public int Expired { get; set; }
        public int UpcomingRenewals { get; set; }
        public int OverdueInstallments { get; set; }
        public decimal MonthlyEmployeeShare { get; set; }
        public decimal MonthlyCompanyShare { get; set; }
        public decimal MonthlyPayable { get; set; }
        public IList<MedicalInsuranceOperationalEmployee> Employees { get; set; }
        public IList<MedicalInsuranceAccountingPreviewLine> AccountingPreview { get; set; }
        public IList<MedicalInsuranceDimensionSummary> BranchCosts { get; set; }
        public IList<MedicalInsuranceDimensionSummary> DepartmentCosts { get; set; }
        public IList<MedicalInsuranceAlert> Alerts { get; set; }

        public MedicalInsuranceOperationalDashboard()
        {
            Employees = new List<MedicalInsuranceOperationalEmployee>();
            AccountingPreview = new List<MedicalInsuranceAccountingPreviewLine>();
            BranchCosts = new List<MedicalInsuranceDimensionSummary>();
            DepartmentCosts = new List<MedicalInsuranceDimensionSummary>();
            Alerts = new List<MedicalInsuranceAlert>();
        }
    }

    public class MedicalInsuranceOperationalEmployee
    {
        public int EmployeeId { get; set; }
        public string EmployeeCode { get; set; }
        public string EmployeeName { get; set; }
        public string BranchName { get; set; }
        public string DepartmentName { get; set; }
        public string ProviderName { get; set; }
        public string PlanName { get; set; }
        public string MembershipNumber { get; set; }
        public string AvatarText { get; set; }
        public string Status { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime? RenewalDate { get; set; }
        public decimal MonthlyCost { get; set; }
        public decimal EmployeeMonthlyDeduction { get; set; }
        public decimal CompanyMonthlyCost { get; set; }
        public int DependentsCount { get; set; }
        public int OverdueInstallments { get; set; }
        public decimal OverdueAmount { get; set; }
        public bool PayrollLinked { get; set; }
        public bool NeedsRenewal { get; set; }
        public IList<MedicalInsuranceDependentSummary> Dependents { get; set; }

        public MedicalInsuranceOperationalEmployee()
        {
            Dependents = new List<MedicalInsuranceDependentSummary>();
        }
    }

    public class MedicalInsuranceDependentSummary
    {
        public string Name { get; set; }
        public string Relation { get; set; }
        public int Age { get; set; }
        public decimal CoveragePercent { get; set; }
        public bool IsActive { get; set; }
    }

    public class MedicalInsuranceDimensionSummary
    {
        public string Name { get; set; }
        public int Employees { get; set; }
        public decimal EmployeeShare { get; set; }
        public decimal CompanyShare { get; set; }
        public decimal TotalCost { get; set; }
    }

    public class MedicalInsuranceAlert
    {
        public string AlertType { get; set; }
        public string Severity { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string EmployeeName { get; set; }
        public string BranchName { get; set; }
        public DateTime? DueDate { get; set; }
    }

    public class MedicalInsuranceAccountingPreviewLine
    {
        public string Step { get; set; }
        public string DebitAccount { get; set; }
        public string CreditAccount { get; set; }
        public decimal Amount { get; set; }
        public string Explanation { get; set; }
    }

    public class EmployeeAccountCodes
    {
        public string EmployeeAccountCode { get; set; }
        public string AccruedSalaryAccountCode { get; set; }
        public string VacationProvisionAccountCode { get; set; }
        public string AdvancePaymentAccountCode { get; set; }
        public string EndOfServiceAccountCode { get; set; }
        public string TicketProvisionAccountCode { get; set; }
    }

    public class EmployeeAccountParents
    {
        public string EmployeeAccountParentCode { get; set; }
        public string AccruedSalaryParentCode { get; set; }
        public string VacationProvisionParentCode { get; set; }
        public string AdvancePaymentParentCode { get; set; }
        public string EndOfServiceParentCode { get; set; }
        public string TicketProvisionParentCode { get; set; }
    }

    public class AccountDefinition
    {
        public string AccountCode { get; set; }
        public string AccountSerial { get; set; }
        public bool LastAccount { get; set; }
        public bool Budget { get; set; }
        public string CurrencyCode { get; set; }
        public bool CostCenter { get; set; }
        public bool SumAccount { get; set; }
        public int? CostCenterType { get; set; }
        public string CostCenterId { get; set; }
        public int? ActivityTypeId { get; set; }
        public int? AccountTypes { get; set; }
        public int? AccountTab { get; set; }
        public int? DepitOrCredit { get; set; }
        public int? DifferentType { get; set; }
        public int? Authority { get; set; }
        public int? UserGroupId { get; set; }
        public int? UserId { get; set; }
        public string Branch { get; set; }
        public int? BranchId { get; set; }
    }
}
