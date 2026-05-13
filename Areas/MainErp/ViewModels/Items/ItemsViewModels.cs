using System.Collections.Generic;

namespace MyERP.Areas.MainErp.ViewModels.Items
{
    public class ItemsIndexViewModel
    {
        public ItemsIndexViewModel()
        {
            Results = new List<ItemListItemViewModel>();
            Groups = new List<ItemLookupViewModel>();
            GroupTree = new List<GroupListItemViewModel>();
            Units = new List<ItemLookupViewModel>();
            Selected = new ItemDetailsViewModel();
            Permissions = new ItemsPermissionsViewModel();
        }

        public string SearchText { get; set; }
        public int? GroupId { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public string Message { get; set; }
        public IList<ItemListItemViewModel> Results { get; set; }
        public IList<ItemLookupViewModel> Groups { get; set; }
        public IList<GroupListItemViewModel> GroupTree { get; set; }
        public IList<ItemLookupViewModel> Units { get; set; }
        public ItemDetailsViewModel Selected { get; set; }
        public ItemsPermissionsViewModel Permissions { get; set; }
    }

    public class ItemsPermissionsViewModel
    {
        public bool CanView { get; set; }
        public bool CanAdd { get; set; }
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }
    }

    public class ItemListItemViewModel
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string NameEn { get; set; }
        public string GroupName { get; set; }
        public string Barcode { get; set; }
        public decimal PurchasePrice { get; set; }
        public decimal SalePrice { get; set; }
        public string DefaultUnitName { get; set; }
        public bool IsArchived { get; set; }
    }

    public class ItemDetailsViewModel
    {
        public ItemDetailsViewModel()
        {
            Units = new List<ItemUnitLineViewModel>();
        }

        public int Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string NameEn { get; set; }
        public int? GroupId { get; set; }
        public int? ItemType { get; set; }
        public string PartNo { get; set; }
        public string Barcode { get; set; }
        public string CatalogNo { get; set; }
        public string FactoryNo { get; set; }
        public decimal PurchasePrice { get; set; }
        public decimal SalePrice { get; set; }
        public decimal CustomerPrice { get; set; }
        public decimal DealerPrice { get; set; }
        public decimal CostPrice { get; set; }
        public decimal MinQty { get; set; }
        public decimal MaxQty { get; set; }
        public int? RequestLimit { get; set; }
        public bool HaveSerial { get; set; }
        public bool HaveGuarantee { get; set; }
        public int? GuaranteeValue { get; set; }
        public int? GuaranteeType { get; set; }
        public bool IsArchived { get; set; }
        public string ShortName { get; set; }
        public string BinLocation { get; set; }
        public string Notes { get; set; }
        public IList<ItemUnitLineViewModel> Units { get; set; }
    }

    public class ItemUnitLineViewModel
    {
        public int? UnitId { get; set; }
        public string UnitName { get; set; }
        public decimal UnitFactor { get; set; }
        public decimal SalePrice { get; set; }
        public decimal PurchasePrice { get; set; }
        public decimal MinSellingPrice { get; set; }
        public decimal MaxSellingPrice { get; set; }
        public decimal WholeSalePrice { get; set; }
        public bool IsDefault { get; set; }
        public string Barcode { get; set; }
    }

    public class ItemLookupViewModel
    {
        public string Id { get; set; }
        public string Text { get; set; }
        public string Extra { get; set; }
    }

    public class GroupListItemViewModel
    {
        public int Id { get; set; }
        public int? ParentId { get; set; }
        public string Code { get; set; }
        public string FullCode { get; set; }
        public string Name { get; set; }
        public string NameEn { get; set; }
        public bool IsLastGroup { get; set; }
        public bool IsMaterial { get; set; }
        public bool IsPosGroup { get; set; }
        public bool IsProducer { get; set; }
        public bool IsAdditions { get; set; }
        public bool HoldingMaterials { get; set; }
        public bool Separate { get; set; }
        public bool IsTransfere { get; set; }
        public bool IsShowCover { get; set; }
        public bool TaxExempt { get; set; }
        public int ItemsCount { get; set; }
        public int ChildrenCount { get; set; }
        public string ParentName { get; set; }
    }

    public class GroupSaveRequest
    {
        public int? Id { get; set; }
        public int? ParentId { get; set; }
        public string Code { get; set; }
        public string FullCode { get; set; }
        public string Name { get; set; }
        public string NameEn { get; set; }
        public bool IsLastGroup { get; set; }
        public bool IsMaterial { get; set; }
        public bool IsPosGroup { get; set; }
        public bool IsProducer { get; set; }
        public bool IsAdditions { get; set; }
        public bool HoldingMaterials { get; set; }
        public bool Separate { get; set; }
        public bool IsTransfere { get; set; }
        public bool IsShowCover { get; set; }
        public bool TaxExempt { get; set; }
    }

    public class GroupSaveResult
    {
        public bool Success { get; set; }
        public int Id { get; set; }
        public string Code { get; set; }
        public string Message { get; set; }
    }

    public class ItemSaveRequest
    {
        public int? Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public string NameEn { get; set; }
        public int? GroupId { get; set; }
        public int? ItemType { get; set; }
        public string PartNo { get; set; }
        public string Barcode { get; set; }
        public string CatalogNo { get; set; }
        public string FactoryNo { get; set; }
        public decimal PurchasePrice { get; set; }
        public decimal SalePrice { get; set; }
        public decimal CustomerPrice { get; set; }
        public decimal DealerPrice { get; set; }
        public decimal CostPrice { get; set; }
        public decimal MinQty { get; set; }
        public decimal MaxQty { get; set; }
        public int? RequestLimit { get; set; }
        public bool HaveSerial { get; set; }
        public bool HaveGuarantee { get; set; }
        public int? GuaranteeValue { get; set; }
        public int? GuaranteeType { get; set; }
        public bool IsArchived { get; set; }
        public string ShortName { get; set; }
        public string BinLocation { get; set; }
        public string Notes { get; set; }
        public IList<ItemUnitLineViewModel> Units { get; set; }
    }

    public class ItemSaveResult
    {
        public bool Success { get; set; }
        public int Id { get; set; }
        public string Code { get; set; }
        public string Message { get; set; }
    }
}
