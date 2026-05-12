using System;
using System.Collections.Generic;

namespace MyERP.Areas.MainErp.ViewModels.Customers
{
    public class CustomersIndexViewModel
    {
        public CustomersIndexViewModel()
        {
            Results = new List<CustomerListItemViewModel>();
            Branches = new List<CustomerLookupItemViewModel>();
            Classes = new List<CustomerLookupItemViewModel>();
            Groups = new List<CustomerLookupItemViewModel>();
            Countries = new List<CustomerLookupItemViewModel>();
            Governments = new List<CustomerLookupItemViewModel>();
            Cities = new List<CustomerLookupItemViewModel>();
            Selected = new CustomerEditViewModel();
            Permissions = new CustomerPermissionsViewModel();
        }

        public string SearchText { get; set; }
        public int? CustomerType { get; set; }
        public int? BranchId { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public IList<CustomerListItemViewModel> Results { get; set; }
        public IList<CustomerLookupItemViewModel> Branches { get; set; }
        public IList<CustomerLookupItemViewModel> Classes { get; set; }
        public IList<CustomerLookupItemViewModel> Groups { get; set; }
        public IList<CustomerLookupItemViewModel> Countries { get; set; }
        public IList<CustomerLookupItemViewModel> Governments { get; set; }
        public IList<CustomerLookupItemViewModel> Cities { get; set; }
        public CustomerEditViewModel Selected { get; set; }
        public CustomerPermissionsViewModel Permissions { get; set; }
    }

    public class CustomerPermissionsViewModel
    {
        public bool CanView { get; set; }
        public bool CanAdd { get; set; }
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }
        public bool CanPrint { get; set; }
    }

    public class CustomerListItemViewModel
    {
        public int CusId { get; set; }
        public string CusName { get; set; }
        public string CusNameEnglish { get; set; }
        public string Phone { get; set; }
        public string Mobile { get; set; }
        public int? Type { get; set; }
        public string TypeName { get; set; }
        public string BranchName { get; set; }
        public string AccountCode { get; set; }
        public string AccountDisplay { get; set; }
        public double? OpenBalance { get; set; }
        public DateTime? RecordDate { get; set; }
        public string FullCode { get; set; }
        public string NationalNo { get; set; }
        public string VatNo { get; set; }
        public bool IsLocked { get; set; }
    }

    public class CustomerEditViewModel
    {
        public int? CusId { get; set; }
        public string CusName { get; set; }
        public string CusNameEnglish { get; set; }
        public string ResponsibleContact { get; set; }
        public string Phone { get; set; }
        public string Mobile { get; set; }
        public string FaxNumber { get; set; }
        public string Email { get; set; }
        public string Address { get; set; }
        public string AddressEnglish { get; set; }
        public string Remark { get; set; }
        public string Remark2 { get; set; }
        public int Type { get; set; }
        public string Prefix { get; set; }
        public string Code { get; set; }
        public string FullCode { get; set; }
        public bool CustomerAndVendor { get; set; }
        public int? BranchId { get; set; }
        public string BranchName { get; set; }
        public DateTime? RecordDate { get; set; }
        public double? OpenBalance { get; set; }
        public int? OpenBalanceType { get; set; }
        public double? OpenBalance1 { get; set; }
        public int? OpenBalanceType1 { get; set; }
        public double? OpenBalance2 { get; set; }
        public int? OpenBalanceType2 { get; set; }
        public DateTime? OpenBalanceDate { get; set; }
        public double? CreditLimit { get; set; }
        public double? CreditLimitCredit { get; set; }
        public int? DebitInterval { get; set; }
        public int? CreditInterval { get; set; }
        public int? PaymentType { get; set; }
        public string AccountCode { get; set; }
        public string AccountAsClient { get; set; }
        public string AccountAsSupplier { get; set; }
        public string ParentAccount { get; set; }
        public string AccountDisplay { get; set; }
        public int? CustomerTypeId { get; set; }
        public int? TypeCustomer { get; set; }
        public int? ClassCustomersId { get; set; }
        public int? GroupsCustomersId { get; set; }
        public int? SaleType { get; set; }
        public int? DiscountType { get; set; }
        public double? DiscountValue { get; set; }
        public int? PurchaseDiscountType { get; set; }
        public double? PurchaseDiscountValue { get; set; }
        public string NationalNo { get; set; }
        public string VatNo { get; set; }
        public string GeneralTaxNo { get; set; }
        public string TaxCardNo { get; set; }
        public string TaxStampNo { get; set; }
        public string WorkEarningTaxesNo { get; set; }
        public string CommercialRegisterNo { get; set; }
        public string ImportCardNo { get; set; }
        public string ExportCardNo { get; set; }
        public bool TaxExempt { get; set; }
        public int? CountryId { get; set; }
        public int? GovernmentId { get; set; }
        public int? CityId { get; set; }
        public int? CountryId2 { get; set; }
        public string StreetName { get; set; }
        public string AdditionalStreetName { get; set; }
        public string BuildingNumber { get; set; }
        public string PlotIdentification { get; set; }
        public string CityName { get; set; }
        public string CitySubdivisionName { get; set; }
        public string PostalZone { get; set; }
        public string CountrySubentity { get; set; }
        public string IdentificationCode { get; set; }
        public string Id700 { get; set; }
        public string BoxMil { get; set; }
        public string ZipCode { get; set; }
        public string BankName { get; set; }
        public string BankAccount { get; set; }
        public string BankCode { get; set; }
        public string BankIban { get; set; }
        public string BankAddress { get; set; }
        public string Iban { get; set; }
        public bool IsLocked { get; set; }
        public bool CreditLocked { get; set; }
        public bool Export { get; set; }
    }

    public class CustomerSaveResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public int? CusId { get; set; }
        public CustomerEditViewModel Customer { get; set; }
    }

    public class CustomerLookupItemViewModel
    {
        public string Id { get; set; }
        public string Text { get; set; }
    }
}
