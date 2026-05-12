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
    }

    public class SalaryRunPreview
    {
        public SalaryRunRequest Request { get; set; }
        public IList<SalaryRunEmployeeRow> Rows { get; set; }
        public IList<SalaryRunJournalLine> JournalPreview { get; set; }
        public decimal TotalBasic { get; set; }
        public decimal TotalAdditions { get; set; }
        public decimal TotalDeductions { get; set; }
        public decimal TotalMedicalInsurance { get; set; }
        public decimal TotalMedicalInsuranceCompanyCost { get; set; }
        public decimal TotalAdvance { get; set; }
        public decimal TotalNet { get; set; }
        public bool HasExistingApprovedRows { get; set; }
        public string Message { get; set; }

        public SalaryRunPreview()
        {
            Rows = new List<SalaryRunEmployeeRow>();
            JournalPreview = new List<SalaryRunJournalLine>();
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
