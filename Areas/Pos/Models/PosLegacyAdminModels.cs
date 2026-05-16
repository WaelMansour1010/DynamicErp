using System.Collections.Generic;

namespace MyERP.Areas.Pos.Models
{
    public class PosLegacyLookupDto
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string Display { get; set; }
    }

    public class PosLegacyUsersIndexModel
    {
        public PosLegacyUsersIndexModel()
        {
            Users = new List<PosLegacyUserListItem>();
            Branches = new List<PosLegacyLookupDto>();
            Stores = new List<PosLegacyLookupDto>();
            Boxes = new List<PosLegacyLookupDto>();
            Employees = new List<PosLegacyLookupDto>();
            Accounts = new List<PosLegacyLookupDto>();
            ProductLines = new List<PosLegacyLookupDto>();
            Selected = new PosLegacyUserEditModel();
        }

        public string SearchText { get; set; }
        public IList<PosLegacyUserListItem> Users { get; set; }
        public PosLegacyUserEditModel Selected { get; set; }
        public IList<PosLegacyLookupDto> Branches { get; set; }
        public IList<PosLegacyLookupDto> Stores { get; set; }
        public IList<PosLegacyLookupDto> Boxes { get; set; }
        public IList<PosLegacyLookupDto> Employees { get; set; }
        public IList<PosLegacyLookupDto> Accounts { get; set; }
        public IList<PosLegacyLookupDto> ProductLines { get; set; }
    }

    public class PosLegacyUserListItem
    {
        public int UserId { get; set; }
        public string UserName { get; set; }
        public string EmployeeName { get; set; }
        public string BranchName { get; set; }
        public string StoreName { get; set; }
        public string BoxName { get; set; }
        public bool IsDeactivated { get; set; }
    }

    public class PosLegacyUserEditModel
    {
        public PosLegacyUserEditModel()
        {
            BranchIds = new List<int>();
            StoreIds = new List<int>();
            BoxIds = new List<int>();
            AccountIds = new List<int>();
            ProductLineIds = new List<int>();
        }

        public int? UserID { get; set; }
        public string UserName { get; set; }
        public string PassWord { get; set; }
        public string PassConfirm { get; set; }
        public int? UserType { get; set; }
        public int? Empid { get; set; }
        public int? BranchId { get; set; }
        public int? StoreID { get; set; }
        public int? StoreID1 { get; set; }
        public int? StoreID2 { get; set; }
        public int? StoreID3 { get; set; }
        public int? BoxID { get; set; }
        public int? BoxID1 { get; set; }
        public int? BoxID2 { get; set; }
        public int? BankID { get; set; }
        public int? Custid { get; set; }
        public int? Custid1 { get; set; }
        public string Account_Code { get; set; }
        public string ReportName { get; set; }
        public string ReportName1 { get; set; }
        public string ReportName2 { get; set; }
        public decimal? CreditLimitSalesMan { get; set; }
        public bool ChangePW { get; set; }
        public bool CustomerService { get; set; }
        public bool HidLowering { get; set; }
        public bool AllowSelectEmp { get; set; }
        public bool IsDeactivated { get; set; }
        public bool CanEditKYC { get; set; }
        public bool IsFullAccsesCustomerService { get; set; }
        public bool IsReturnAllowed { get; set; }
        public bool CanEditSalesInvoice { get; set; }
        public bool CanEditSalesInvoicePos { get; set; }
        public bool CanCancelClose { get; set; }
        public string UserCategory { get; set; }
        public IList<int> BranchIds { get; set; }
        public IList<int> StoreIds { get; set; }
        public IList<int> BoxIds { get; set; }
        public IList<int> AccountIds { get; set; }
        public IList<int> ProductLineIds { get; set; }
    }

    public class PosBranchesDataIndexModel
    {
        public PosBranchesDataIndexModel()
        {
            Activities = new List<PosActivityListItem>();
            Branches = new List<PosBranchDataEditModel>();
            Regions = new List<PosLegacyLookupDto>();
            Stores = new List<PosLegacyLookupDto>();
            Selected = new PosActivityEditModel();
        }

        public IList<PosActivityListItem> Activities { get; set; }
        public PosActivityEditModel Selected { get; set; }
        public IList<PosBranchDataEditModel> Branches { get; set; }
        public IList<PosLegacyLookupDto> Regions { get; set; }
        public IList<PosLegacyLookupDto> Stores { get; set; }
    }

    public class PosActivityListItem
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string NameEnglish { get; set; }
        public int BranchCount { get; set; }
    }

    public class PosActivityEditModel
    {
        public int? Id { get; set; }
        public string Name { get; set; }
        public string NameEnglish { get; set; }
    }

    public class PosBranchDataEditModel
    {
        public int? BranchId { get; set; }
        public int? ActivityTypeId { get; set; }
        public string BranchCode { get; set; }
        public string BranchName { get; set; }
        public string BranchNameEnglish { get; set; }
        public string Manager { get; set; }
        public string Telephone { get; set; }
        public string Remarks { get; set; }
        public string AccountCode { get; set; }
        public string Users { get; set; }
        public string VatNo { get; set; }
        public int? RegionId { get; set; }
        public int? StoreId { get; set; }
        public bool ShowLogoInReports { get; set; }
        public bool IsStopped { get; set; }
    }

    public class PosLegacySaveResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public int? Id { get; set; }
    }
}
