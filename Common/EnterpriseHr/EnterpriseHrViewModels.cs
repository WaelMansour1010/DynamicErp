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
            Advances = new List<EmployeeAdvanceViewModel>();
            Employees = new List<EnterpriseHrEmployeeLookupViewModel>();
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
        public string AdvanceDetailsUrl { get; set; }
        public string SaveAdvanceUrl { get; set; }
        public string DeleteAdvanceUrl { get; set; }
        public string EmployeeLookupUrl { get; set; }
        public int? EmployeeId { get; set; }
        public string DateFrom { get; set; }
        public string DateTo { get; set; }
        public string AdvanceStatus { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public IList<LegacyHrFinanceMetricViewModel> Metrics { get; set; }
        public IList<LegacyHrFinanceRowViewModel> Rows { get; set; }
        public IList<PayrollComponentEditViewModel> Components { get; set; }
        public IList<EmployeeAdvanceViewModel> Advances { get; set; }
        public IList<EnterpriseHrEmployeeLookupViewModel> Employees { get; set; }
        public LegacyHrFinancePermissionsViewModel Permissions { get; set; }
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
        public bool Posted { get; set; }
        public bool AccountingApproved { get; set; }
        public bool Rejected { get; set; }
        public string Reason { get; set; }
        public decimal BasicSalary { get; set; }
        public decimal OldAdvance { get; set; }
        public decimal Balance { get; set; }
        public int PaidPartsCount { get; set; }
        public int PartsCount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal RemainingAmount { get; set; }
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }
        public string LockReason { get; set; }
        public IList<EmployeeAdvancePartViewModel> Parts { get; set; }

        public EmployeeAdvanceViewModel()
        {
            Parts = new List<EmployeeAdvancePartViewModel>();
        }
    }

    public class EmployeeAdvancePartViewModel
    {
        public int PartNo { get; set; }
        public decimal PartValue { get; set; }
        public string PartDate { get; set; }
        public bool Payed { get; set; }
        public string Remark { get; set; }
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
