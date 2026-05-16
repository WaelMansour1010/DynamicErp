using System;
using System.Collections.Generic;
using System.Web.Mvc;

namespace MyERP.Common.StoreData
{
    public class StoreDataPermissions
    {
        public bool CanView { get; set; }
        public bool CanAdd { get; set; }
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }
    }

    public class StoreDataSearchRequest
    {
        public string SearchText { get; set; }
        public int? BranchId { get; set; }
        public string Mode { get; set; }
    }

    public class StoreDataIndexViewModel
    {
        public StoreDataIndexViewModel()
        {
            Stores = new List<StoreDataRowViewModel>();
            Branches = new List<SelectListItem>();
            Employees = new List<SelectListItem>();
            Users = new List<StoreUserLookupViewModel>();
            Boxes = new List<SelectListItem>();
            Accounts = new List<SelectListItem>();
            Selected = new StoreDataEditViewModel();
            Permissions = new StoreDataPermissions();
        }

        public string SearchText { get; set; }
        public int? BranchId { get; set; }
        public string Mode { get; set; }
        public int TotalCount { get; set; }
        public int LinkedCount { get; set; }
        public int UnlinkedCount { get; set; }
        public int LabCount { get; set; }
        public int NoEntryCount { get; set; }
        public string Warning { get; set; }
        public string SuccessMessage { get; set; }
        public StoreDataPermissions Permissions { get; set; }
        public StoreDataEditViewModel Selected { get; set; }
        public IList<StoreDataRowViewModel> Stores { get; set; }
        public IList<SelectListItem> Branches { get; set; }
        public IList<SelectListItem> Employees { get; set; }
        public IList<SelectListItem> Boxes { get; set; }
        public IList<SelectListItem> Accounts { get; set; }
        public IList<StoreUserLookupViewModel> Users { get; set; }
    }

    public class StoreDataRowViewModel
    {
        public int StoreId { get; set; }
        public string Code { get; set; }
        public string StoreName { get; set; }
        public string StoreNameEnglish { get; set; }
        public int? BranchId { get; set; }
        public string BranchName { get; set; }
        public string Phone { get; set; }
        public string EmployeeName { get; set; }
        public bool Linked { get; set; }
        public bool IsLab { get; set; }
        public bool IsNotCreateEntry { get; set; }
        public int UserCount { get; set; }
        public int TransactionCount { get; set; }
        public string InventoryAccount { get; set; }
        public string SettlementAccount { get; set; }
    }

    public class StoreDataEditViewModel
    {
        public StoreDataEditViewModel()
        {
            UserIds = new List<int>();
            AssignedUsers = new List<StoreUserLookupViewModel>();
            AutoCreateAccounts = true;
            Linked = true;
        }

        public int StoreId { get; set; }
        public string Code { get; set; }
        public string StoreName { get; set; }
        public string StoreNameEnglish { get; set; }
        public string Address { get; set; }
        public string Phone { get; set; }
        public string Remarks { get; set; }
        public int? BranchId { get; set; }
        public int? EmployeeId { get; set; }
        public int? SalesPersonId { get; set; }
        public int? PurchasePersonId { get; set; }
        public int? BoxId { get; set; }
        public bool Linked { get; set; }
        public bool IsLab { get; set; }
        public bool IsNotCreateEntry { get; set; }
        public bool AutoCreateAccounts { get; set; }
        public string ParentAccount { get; set; }
        public string InventoryAccount { get; set; }
        public string LossAccount { get; set; }
        public string SettlementAccount { get; set; }
        public string GiftAccount { get; set; }
        public string InventoryParentAccount { get; set; }
        public string LossParentAccount { get; set; }
        public string SettlementParentAccount { get; set; }
        public string GiftParentAccount { get; set; }
        public int TransactionCount { get; set; }
        public int VoucherAccountUseCount { get; set; }
        public IList<int> UserIds { get; set; }
        public IList<StoreUserLookupViewModel> AssignedUsers { get; set; }
    }

    public class StoreUserLookupViewModel
    {
        public int UserId { get; set; }
        public string UserName { get; set; }
        public string EmployeeCode { get; set; }
        public string DisplayName
        {
            get
            {
                return string.IsNullOrWhiteSpace(EmployeeCode)
                    ? UserName
                    : EmployeeCode + " - " + UserName;
            }
        }
    }

    public class StoreDataSaveResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public int? StoreId { get; set; }
    }

    public class OperationalStoreViewModel
    {
        public int StoreId { get; set; }
        public string Code { get; set; }
        public string StoreName { get; set; }
        public string StoreNameEnglish { get; set; }
        public int? BranchId { get; set; }
        public string BranchName { get; set; }
        public string Phone { get; set; }
        public bool Linked { get; set; }
        public bool IsLab { get; set; }
        public bool IsNotCreateEntry { get; set; }
        public int TransactionCount { get; set; }
        public DateTime? LastMovementDate { get; set; }
    }
}
