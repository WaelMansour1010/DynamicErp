using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MyERP.Areas.MainErp.ViewModels.AccountCharts
{
    public class AccountChartsIndexViewModel
    {
        public AccountChartsIndexViewModel()
        {
            Tree = new List<AccountTreeNodeViewModel>();
            Accounts = new List<AccountLookupViewModel>();
            Currencies = new List<LookupItemViewModel>();
            CostCenters = new List<LookupItemViewModel>();
            ActivityTypes = new List<LookupItemViewModel>();
            Branches = new List<LookupItemViewModel>();
            Users = new List<LookupItemViewModel>();
            Groups = new List<LookupItemViewModel>();
            Permissions = new AccountChartsPermissionsViewModel();
        }

        public IList<AccountTreeNodeViewModel> Tree { get; set; }
        public IList<AccountLookupViewModel> Accounts { get; set; }
        public IList<LookupItemViewModel> Currencies { get; set; }
        public IList<LookupItemViewModel> CostCenters { get; set; }
        public IList<LookupItemViewModel> ActivityTypes { get; set; }
        public IList<LookupItemViewModel> Branches { get; set; }
        public IList<LookupItemViewModel> Users { get; set; }
        public IList<LookupItemViewModel> Groups { get; set; }
        public AccountChartsPermissionsViewModel Permissions { get; set; }
        public int TotalAccounts { get; set; }
    }

    public class AccountChartsPermissionsViewModel
    {
        public bool CanView { get; set; }
        public bool CanAdd { get; set; }
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }
        public bool CanPrint { get; set; }
    }

    public class AccountTreeNodeViewModel
    {
        public string AccountCode { get; set; }
        public string ParentAccountCode { get; set; }
        public string AccountSerial { get; set; }
        public string AccountName { get; set; }
        public string AccountNameEnglish { get; set; }
        public bool IsLastAccount { get; set; }
        public int Level { get; set; }
        public bool IsBlocked { get; set; }
    }

    public class AccountLookupViewModel
    {
        public string AccountCode { get; set; }
        public string ParentAccountCode { get; set; }
        public string AccountSerial { get; set; }
        public string AccountName { get; set; }
        public bool IsLastAccount { get; set; }
    }

    public class LookupItemViewModel
    {
        public string Id { get; set; }
        public string Text { get; set; }
    }

    public class AccountDetailsViewModel
    {
        public AccountDetailsViewModel()
        {
            BranchIds = new List<int>();
            UserIds = new List<int>();
        }

        public int AccountId { get; set; }
        public string AccountCode { get; set; }
        public string ParentAccountCode { get; set; }
        public string AccountSerial { get; set; }
        public string AccountName { get; set; }
        public string AccountNameEnglish { get; set; }
        public bool IsLastAccount { get; set; }
        public bool HasBudget { get; set; }
        public bool HasCostCenter { get; set; }
        public bool IsSummaryAccount { get; set; }
        public bool IsBlocked { get; set; }
        public string CurrencyCode { get; set; }
        public string CostCenterId { get; set; }
        public int CostCenterType { get; set; }
        public int? ActivityTypeId { get; set; }
        public int? AccountTypes { get; set; }
        public int? AccountTab { get; set; }
        public int? DebitOrCredit { get; set; }
        public int? DifferentType { get; set; }
        public int? Authority { get; set; }
        public int? UserGroupId { get; set; }
        public int? UserId { get; set; }
        public DateTime? DateCreated { get; set; }
        public int Level { get; set; }
        public bool CanDeleteSafely { get; set; }
        public string DeleteBlockReason { get; set; }
        public IList<int> BranchIds { get; set; }
        public IList<int> UserIds { get; set; }
    }

    public class AccountSaveRequest
    {
        public string Mode { get; set; }
        public int? AccountId { get; set; }
        public string AccountCode { get; set; }
        public string ParentAccountCode { get; set; }

        [Required]
        public string AccountSerial { get; set; }

        [Required]
        public string AccountName { get; set; }

        public string AccountNameEnglish { get; set; }
        public bool IsLastAccount { get; set; }
        public bool HasBudget { get; set; }
        public bool HasCostCenter { get; set; }
        public bool IsSummaryAccount { get; set; }
        public bool IsBlocked { get; set; }
        public string CurrencyCode { get; set; }
        public string CostCenterId { get; set; }
        public int CostCenterType { get; set; }
        public int? ActivityTypeId { get; set; }
        public int? AccountTypes { get; set; }
        public int? AccountTab { get; set; }
        public int? DebitOrCredit { get; set; }
        public int? DifferentType { get; set; }
        public int? Authority { get; set; }
        public int? UserGroupId { get; set; }
        public int? UserId { get; set; }
        public int[] BranchIds { get; set; }
        public int[] UserIds { get; set; }
    }

    public class AccountSaveResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string AccountCode { get; set; }
        public AccountDetailsViewModel Account { get; set; }
    }
}
