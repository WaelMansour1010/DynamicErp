using System;
using System.Collections.Generic;

namespace MyERP.Areas.MainErp.ViewModels.FinancialAdministration
{
    public class FinancialAdministrationIndexViewModel
    {
        public FinancialAdministrationIndexViewModel()
        {
            Banks = new List<FinancialBankListItemViewModel>();
            Boxes = new List<FinancialBoxListItemViewModel>();
            Branches = new List<FinancialLookupViewModel>();
            Accounts = new List<FinancialLookupViewModel>();
            Currencies = new List<FinancialLookupViewModel>();
            Employees = new List<FinancialLookupViewModel>();
            CurrencyBreakdown = new List<FinancialMetricViewModel>();
            BranchBreakdown = new List<FinancialMetricViewModel>();
            RecentBankMovements = new List<FinancialMovementViewModel>();
            RecentBoxMovements = new List<FinancialMovementViewModel>();
            Search = new FinancialAdministrationSearchViewModel();
            Summary = new FinancialAdministrationSummaryViewModel();
            Permissions = new FinancialAdministrationPermissionViewModel();
        }

        public FinancialAdministrationSearchViewModel Search { get; set; }
        public FinancialAdministrationSummaryViewModel Summary { get; set; }
        public IList<FinancialBankListItemViewModel> Banks { get; set; }
        public IList<FinancialBoxListItemViewModel> Boxes { get; set; }
        public IList<FinancialLookupViewModel> Branches { get; set; }
        public IList<FinancialLookupViewModel> Accounts { get; set; }
        public IList<FinancialLookupViewModel> Currencies { get; set; }
        public IList<FinancialLookupViewModel> Employees { get; set; }
        public IList<FinancialMetricViewModel> CurrencyBreakdown { get; set; }
        public IList<FinancialMetricViewModel> BranchBreakdown { get; set; }
        public IList<FinancialMovementViewModel> RecentBankMovements { get; set; }
        public IList<FinancialMovementViewModel> RecentBoxMovements { get; set; }
        public FinancialAdministrationPermissionViewModel Permissions { get; set; }
    }

    public class FinancialAdministrationSearchViewModel
    {
        public string SearchText { get; set; }
        public string Scope { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }

    public class FinancialAdministrationSummaryViewModel
    {
        public int BanksCount { get; set; }
        public int BoxesCount { get; set; }
        public int PosTerminalBoxesCount { get; set; }
        public int WalletBoxesCount { get; set; }
        public int LinkedBankAccountsCount { get; set; }
        public int LinkedBoxAccountsCount { get; set; }
        public decimal BankOpeningBalance { get; set; }
        public decimal BoxOpeningBalance { get; set; }
    }

    public class FinancialBankListItemViewModel
    {
        public int BankId { get; set; }
        public string BankName { get; set; }
        public string BankNameEnglish { get; set; }
        public string AccountCode { get; set; }
        public string AccountName { get; set; }
        public string BranchName { get; set; }
        public string CurrencyCode { get; set; }
        public string Iban { get; set; }
        public string Telephone { get; set; }
        public decimal OpeningBalance { get; set; }
        public int? OpeningBalanceType { get; set; }
        public bool ApprovalRequired { get; set; }
        public bool LoanBank { get; set; }
        public DateTime? LastMovementDate { get; set; }
        public decimal LastMovementValue { get; set; }
    }

    public class FinancialBoxListItemViewModel
    {
        public int BoxId { get; set; }
        public string BoxName { get; set; }
        public string BoxNameEnglish { get; set; }
        public string AccountCode { get; set; }
        public string AccountName { get; set; }
        public string BranchName { get; set; }
        public string EmployeeName { get; set; }
        public string Comments { get; set; }
        public int? Type { get; set; }
        public int? BalanceType { get; set; }
        public decimal OpeningBalance { get; set; }
        public decimal LimitValue { get; set; }
        public bool HasChequeBox { get; set; }
        public bool IsWallet { get; set; }
        public bool IsTerminalPos { get; set; }
        public DateTime? LastMovementDate { get; set; }
        public decimal LastMovementValue { get; set; }
    }

    public class FinancialMetricViewModel
    {
        public string Key { get; set; }
        public string Name { get; set; }
        public int Count { get; set; }
        public decimal Value { get; set; }
    }

    public class FinancialMovementViewModel
    {
        public int Id { get; set; }
        public DateTime? MovementDate { get; set; }
        public string SourceName { get; set; }
        public string Serial { get; set; }
        public decimal Value { get; set; }
        public string Remarks { get; set; }
    }

    public class FinancialLookupViewModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }
    }

    public class FinancialBankEditViewModel
    {
        public int? BankId { get; set; }
        public int? Id
        {
            get { return BankId; }
            set { BankId = value; }
        }
        public string BankName { get; set; }
        public string BankNameEnglish { get; set; }
        public string Remarks { get; set; }
        public string AccountCode { get; set; }
        public string AccountCode1 { get; set; }
        public string AccountCode2 { get; set; }
        public string AccountCode3 { get; set; }
        public string ParentAccount { get; set; }
        public int? BranchId { get; set; }
        public int? CurrencyId { get; set; }
        public string AccountNo { get; set; }
        public string Iban { get; set; }
        public string BranchNo { get; set; }
        public string Telephone { get; set; }
        public string Address { get; set; }
        public string Email { get; set; }
        public decimal OpeningBalance { get; set; }
        public int? OpeningBalanceType { get; set; }
        public decimal Commission { get; set; }
        public bool ApprovalRequired { get; set; }
        public bool LoanBank { get; set; }
    }

    public class FinancialBoxEditViewModel
    {
        public int? BoxId { get; set; }
        public int? Id
        {
            get { return BoxId; }
            set { BoxId = value; }
        }
        public string BoxName { get; set; }
        public string BoxNameEnglish { get; set; }
        public string Comments { get; set; }
        public string AccountCode { get; set; }
        public string AccountCode1 { get; set; }
        public string AccountCode2 { get; set; }
        public string ParentAccount { get; set; }
        public int? Type { get; set; }
        public int? EmployeeId { get; set; }
        public int? BranchId { get; set; }
        public bool HasChequeBox { get; set; }
        public DateTime? OpeningBalanceDate { get; set; }
        public int? OpeningBalanceType { get; set; }
        public decimal OpeningBalance { get; set; }
        public int? DriverId { get; set; }
        public int? BalanceType { get; set; }
        public decimal LimitValue { get; set; }
        public int? Period { get; set; }
        public int? PeriodMode { get; set; }
    }

    public class FinancialAdministrationSaveResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public int? Id { get; set; }
    }

    public class FinancialAdministrationPermissionViewModel
    {
        public bool CanViewBanks { get; set; }
        public bool CanViewBoxes { get; set; }
        public bool CanAddBanks { get; set; }
        public bool CanEditBanks { get; set; }
        public bool CanAddBoxes { get; set; }
        public bool CanEditBoxes { get; set; }
    }
}
