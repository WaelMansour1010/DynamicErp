using System;
using System.Collections.Generic;

namespace MyERP.Areas.MainErp.ViewModels.DefinCompItem
{
    public class DefinCompItemIndexViewModel
    {
        public DefinCompItemIndexViewModel()
        {
            Results = new List<DefinCompItemListItemViewModel>();
            Branches = new List<DefinCompItemLookupItemViewModel>();
            Stores = new List<DefinCompItemLookupItemViewModel>();
            Customers = new List<DefinCompItemLookupItemViewModel>();
            Selected = new DefinCompItemDetailsViewModel();
            Permissions = new DefinCompItemPermissionsViewModel();
        }

        public string SearchText { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int? BranchId { get; set; }
        public int? StoreId { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public string Message { get; set; }
        public IList<DefinCompItemListItemViewModel> Results { get; set; }
        public IList<DefinCompItemLookupItemViewModel> Branches { get; set; }
        public IList<DefinCompItemLookupItemViewModel> Stores { get; set; }
        public IList<DefinCompItemLookupItemViewModel> Customers { get; set; }
        public DefinCompItemDetailsViewModel Selected { get; set; }
        public DefinCompItemPermissionsViewModel Permissions { get; set; }
    }

    public class DefinCompItemPermissionsViewModel
    {
        public bool CanView { get; set; }
        public bool CanAdd { get; set; }
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }
        public bool CanPrint { get; set; }
    }

    public class DefinCompItemListItemViewModel
    {
        public int Id { get; set; }
        public string MaxNo { get; set; }
        public string MaxName { get; set; }
        public DateTime? RecordDate { get; set; }
        public string BranchName { get; set; }
        public string StoreName { get; set; }
        public string StoreName2 { get; set; }
        public string StoreName3 { get; set; }
        public string ItemName { get; set; }
        public string ItemCode { get; set; }
        public decimal TotalWithVat { get; set; }
        public decimal Net { get; set; }
        public decimal Vat2 { get; set; }
        public int? TransactionId1 { get; set; }
        public int? TransactionId2 { get; set; }
        public string NoteSerial11 { get; set; }
        public string NoteSerial12 { get; set; }
        public string OrderNo { get; set; }
        public bool? Allocated { get; set; }
        public bool? AlloPay { get; set; }
        public bool? AlloRecep { get; set; }
    }

    public class DefinCompItemDetailsViewModel
    {
        public DefinCompItemDetailsViewModel()
        {
            Components = new List<DefinCompItemComponentLineViewModel>();
            Outputs = new List<DefinCompItemOutputLineViewModel>();
            OutputGroups = new List<DefinCompItemOutputGroupViewModel>();
            LinkedTransactions = new List<DefinCompItemLinkedTransactionViewModel>();
            Warnings = new List<string>();
        }

        public int Id { get; set; }
        public DateTime? RecordDate { get; set; }
        public int? BranchId { get; set; }
        public string BranchName { get; set; }
        public int? StoreId { get; set; }
        public string StoreName { get; set; }
        public int? StoreId2 { get; set; }
        public string StoreName2 { get; set; }
        public int? StoreId3 { get; set; }
        public string StoreName3 { get; set; }
        public int? CusId { get; set; }
        public string CustomerName { get; set; }
        public int? ItemNameId { get; set; }
        public string ItemName { get; set; }
        public string ItemCode { get; set; }
        public string MaxNo { get; set; }
        public string MaxName { get; set; }
        public string OrderNo { get; set; }
        public int? GroupId { get; set; }
        public int? PaymentType { get; set; }
        public int? EmpId { get; set; }
        public int? BoxId { get; set; }
        public double? Period { get; set; }
        public decimal Qty1 { get; set; }
        public decimal Price { get; set; }
        public decimal TotalAdd { get; set; }
        public decimal TotalDisc { get; set; }
        public decimal Net { get; set; }
        public decimal TotalWithVat { get; set; }
        public decimal Vat2 { get; set; }
        public int? TransactionId1 { get; set; }
        public int? TransactionId2 { get; set; }
        public int? TransactionId3 { get; set; }
        public int? TransactionId4 { get; set; }
        public int? TransactionId5 { get; set; }
        public int? TransactionId6 { get; set; }
        public string NoteSerial11 { get; set; }
        public string NoteSerial12 { get; set; }
        public string NoteSerial13 { get; set; }
        public string NoteSerial14 { get; set; }
        public string NoteSerial15 { get; set; }
        public string NoteSerial16 { get; set; }
        public decimal ComponentQtyTotal { get; set; }
        public decimal ComponentCostTotal { get; set; }
        public decimal OutputQtyTotal { get; set; }
        public decimal OutputCostTotal { get; set; }
        public decimal Difference { get; set; }
        public IList<DefinCompItemComponentLineViewModel> Components { get; set; }
        public IList<DefinCompItemOutputLineViewModel> Outputs { get; set; }
        public IList<DefinCompItemOutputGroupViewModel> OutputGroups { get; set; }
        public IList<DefinCompItemLinkedTransactionViewModel> LinkedTransactions { get; set; }
        public IList<string> Warnings { get; set; }
    }

    public class DefinCompItemComponentLineViewModel
    {
        public int Id { get; set; }
        public int? ItemId2 { get; set; }
        public string ItemCode2 { get; set; }
        public string ItemName2 { get; set; }
        public int? ItemId { get; set; }
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public int? UnitId { get; set; }
        public string UnitName { get; set; }
        public double? Qty { get; set; }
        public double? Cost { get; set; }
        public double? Price { get; set; }
        public double? Total { get; set; }
        public bool IsDeleted { get; set; }
        public bool? IsAdd { get; set; }
        public int? LineId { get; set; }
        public int? SpecId1 { get; set; }
        public int? SpecId2 { get; set; }
        public int? SpecId3 { get; set; }
        public int? SpecId4 { get; set; }
        public double? Width { get; set; }
        public double? Height { get; set; }
        public double? Length { get; set; }
        public double? Thickness { get; set; }
        public double? Diameter { get; set; }
        public string Remark { get; set; }
    }

    public class DefinCompItemOutputLineViewModel
    {
        public int Id { get; set; }
        public int? ItemId { get; set; }
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public int? UnitId { get; set; }
        public string UnitName { get; set; }
        public double? Qty { get; set; }
        public double? Cost { get; set; }
        public double? Price { get; set; }
        public double? Total { get; set; }
        public int? TransactionId4 { get; set; }
        public string NoteSerial14 { get; set; }
        public int? LineId { get; set; }
        public double? QtyBySmallUnit { get; set; }
        public string Remark { get; set; }
    }

    public class DefinCompItemOutputGroupViewModel
    {
        public int Id { get; set; }
        public int? ItemId { get; set; }
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public int? UnitId { get; set; }
        public string UnitName { get; set; }
        public double? Qty { get; set; }
        public double? Cost { get; set; }
        public double? Price { get; set; }
        public double? Total { get; set; }
        public int? TransactionId4 { get; set; }
        public string NoteSerial14 { get; set; }
        public int? LineId { get; set; }
        public double? QtyBySmallUnit { get; set; }
        public string Remark { get; set; }
        public IList<DefinCompItemComponentLineViewModel> Components { get; set; }

        public DefinCompItemOutputGroupViewModel()
        {
            Components = new List<DefinCompItemComponentLineViewModel>();
        }
    }

    public class DefinCompItemLinkedTransactionViewModel
    {
        public int TransactionId { get; set; }
        public int TransactionType { get; set; }
        public string TransactionSerial { get; set; }
        public DateTime? TransactionDate { get; set; }
        public decimal Total { get; set; }
        public decimal NetValue { get; set; }
        public decimal Vat { get; set; }
        public string NoteSerial1 { get; set; }
        public int? NoteId { get; set; }
        public int? InvoiceOrderNo { get; set; }
        public string Description { get; set; }
        public string DebitAccount { get; set; }
        public string CreditAccount { get; set; }
    }

    public class DefinCompItemLookupItemViewModel
    {
        public string Id { get; set; }
        public string Text { get; set; }
        public string Extra { get; set; }
    }

    public class DefinCompItemSaveRequest
    {
        public string Mode { get; set; }
        public int? Id { get; set; }
        public DateTime? RecordDate { get; set; }
        public int? BranchId { get; set; }
        public int? StoreId { get; set; }
        public int? StoreId2 { get; set; }
        public int? StoreId3 { get; set; }
        public int? CusId { get; set; }
        public int? ItemNameId { get; set; }
        public string MaxNo { get; set; }
        public string MaxName { get; set; }
        public string OrderNo { get; set; }
        public int? GroupId { get; set; }
        public int? PaymentType { get; set; }
        public int? EmpId { get; set; }
        public int? BoxId { get; set; }
        public double? Period { get; set; }
        public decimal Qty1 { get; set; }
        public decimal Price { get; set; }
        public decimal TotalAdd { get; set; }
        public decimal TotalDisc { get; set; }
        public decimal Net { get; set; }
        public decimal TotalWithVat { get; set; }
        public decimal Vat2 { get; set; }
        public string TransactionComment { get; set; }
        public bool ForceRebuild { get; set; }
        public IList<DefinCompItemLineRequest> Components { get; set; }
        public IList<DefinCompItemLineRequest> Outputs { get; set; }
        public IList<DefinCompItemOutputGroupRequest> OutputGroups { get; set; }
    }

    public class DefinCompItemLineRequest
    {
        public int? Id { get; set; }
        public int? ItemId2 { get; set; }
        public int? ItemId { get; set; }
        public int? UnitId { get; set; }
        public double? Qty { get; set; }
        public double? Cost { get; set; }
        public double? Price { get; set; }
        public double? Total { get; set; }
        public bool IsDeleted { get; set; }
        public bool? IsAdd { get; set; }
        public int? LineId { get; set; }
        public int? SpecId1 { get; set; }
        public int? SpecId2 { get; set; }
        public int? SpecId3 { get; set; }
        public int? SpecId4 { get; set; }
        public double? Width { get; set; }
        public double? Height { get; set; }
        public double? Length { get; set; }
        public double? Thickness { get; set; }
        public double? Diameter { get; set; }
        public string Remark { get; set; }
    }

    public class DefinCompItemOutputGroupRequest : DefinCompItemLineRequest
    {
        public DefinCompItemOutputGroupRequest()
        {
            Components = new List<DefinCompItemLineRequest>();
        }

        public IList<DefinCompItemLineRequest> Components { get; set; }
    }

    public class DefinCompItemSaveResult
    {
        public DefinCompItemSaveResult()
        {
            Warnings = new List<string>();
        }

        public bool Success { get; set; }
        public string Message { get; set; }
        public int? Id { get; set; }
        public int? IssueTransactionId { get; set; }
        public int? ReceiptTransactionId { get; set; }
        public string IssueSerial { get; set; }
        public string ReceiptSerial { get; set; }
        public decimal ComponentQtyTotal { get; set; }
        public decimal ComponentCostTotal { get; set; }
        public decimal OutputQtyTotal { get; set; }
        public decimal OutputCostTotal { get; set; }
        public decimal Difference { get; set; }
        public IList<string> Warnings { get; set; }
    }
}
