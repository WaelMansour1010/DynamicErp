using System;
using System.Collections.Generic;

namespace MyERP.Areas.MainErp.ViewModels.LegacyHrFinance
{
    public class LegacyHrFinancePageViewModel
    {
        public LegacyHrFinancePageViewModel()
        {
            Metrics = new List<LegacyHrFinanceMetricViewModel>();
            Rows = new List<LegacyHrFinanceRowViewModel>();
            Components = new List<PayrollComponentEditViewModel>();
            Permissions = new LegacyHrFinancePermissionsViewModel();
        }

        public string ModuleKey { get; set; }
        public string Title { get; set; }
        public string SourceSystem { get; set; }
        public string SourceForm { get; set; }
        public string LegacyTable { get; set; }
        public string Warning { get; set; }
        public string SearchText { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public IList<LegacyHrFinanceMetricViewModel> Metrics { get; set; }
        public IList<LegacyHrFinanceRowViewModel> Rows { get; set; }
        public IList<PayrollComponentEditViewModel> Components { get; set; }
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

    public class LegacyHrFinancePermissionsViewModel
    {
        public bool CanView { get; set; }
        public bool CanAdd { get; set; }
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }
    }

    public class LegacyHrFinanceSaveResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public int? Id { get; set; }
    }
}
