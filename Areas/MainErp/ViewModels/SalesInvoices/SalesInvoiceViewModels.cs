using System;
using System.Collections.Generic;
using System.Linq;

namespace MyERP.Areas.MainErp.ViewModels.SalesInvoices
{
    public enum MainErpSalesInvoiceKind
    {
        Workshop = 1,
        Pump = 2
    }

    public class SalesInvoiceIndexViewModel
    {
        public SalesInvoiceIndexViewModel()
        {
            Items = new List<SalesInvoiceListItemViewModel>();
        }

        public MainErpSalesInvoiceKind Kind { get; set; }
        public string ArabicTitle { get; set; }
        public string SearchText { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int? BranchId { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public string Warning { get; set; }
        public IList<SalesInvoiceListItemViewModel> Items { get; private set; }
        public SalesInvoiceDiagnosticsViewModel Diagnostics { get; set; }
    }

    public class SalesInvoiceListItemViewModel
    {
        public int TransactionId { get; set; }
        public string TransactionSerial { get; set; }
        public string NoteSerial { get; set; }
        public string NoteSerial1 { get; set; }
        public string ManualNo { get; set; }
        public DateTime? TransactionDate { get; set; }
        public int? TransactionType { get; set; }
        public int? TypeInvoice { get; set; }
        public int? CustomerId { get; set; }
        public int? BranchId { get; set; }
        public string CustomerName { get; set; }
        public string CashCustomerName { get; set; }
        public string BranchName { get; set; }
        public string StoreName { get; set; }
        public decimal? Total { get; set; }
        public decimal? NetValue { get; set; }
        public decimal? Vat { get; set; }
        public decimal? PayedValue { get; set; }
        public decimal? RemainValue { get; set; }
        public int? NoteId { get; set; }
        public bool? Closed { get; set; }
    }

    public class SalesInvoiceDetailsViewModel : SalesInvoiceListItemViewModel
    {
        public SalesInvoiceDetailsViewModel()
        {
            Lines = new List<SalesInvoiceLineViewModel>();
            VoucherLines = new List<SalesInvoiceVoucherLineViewModel>();
            Payments = new List<SalesInvoicePaymentViewModel>();
            RelatedInventoryTransactions = new List<SalesInvoiceRelatedInventoryTransactionViewModel>();
            AuditLogs = new List<SalesInvoiceAuditLogViewModel>();
            SavePreview = new SalesInvoiceSavePreviewViewModel();
        }

        public MainErpSalesInvoiceKind Kind { get; set; }
        public string ArabicTitle { get; set; }
        public string Remarks { get; set; }
        public string PaymentTypeLabel { get; set; }
        public string CurrencyId { get; set; }
        public decimal? CurrencyRate { get; set; }
        public string OrderNo { get; set; }
        public int? StoreId { get; set; }
        public int? BoxId { get; set; }
        public int? PaymentNetId { get; set; }
        public bool? Posted { get; set; }
        public bool? Approved { get; set; }
        public bool? IsPosted { get; set; }
        public int? UserPosted { get; set; }
        public string Prefix { get; set; }
        public string FullCode { get; set; }
        public int? CboBasedOn { get; set; }
        public int? PosBillType { get; set; }
        public decimal? TransactionNetValue { get; set; }
        public decimal? SumValueLine { get; set; }
        public decimal? SumVatLine { get; set; }
        public DateTime? DateRec { get; set; }
        public string Warning { get; set; }
        public IList<SalesInvoiceLineViewModel> Lines { get; private set; }
        public IList<SalesInvoiceVoucherLineViewModel> VoucherLines { get; private set; }
        public IList<SalesInvoicePaymentViewModel> Payments { get; private set; }
        public IList<SalesInvoiceRelatedInventoryTransactionViewModel> RelatedInventoryTransactions { get; private set; }
        public IList<SalesInvoiceAuditLogViewModel> AuditLogs { get; private set; }
        public SalesInvoiceSavePreviewViewModel SavePreview { get; set; }

        public decimal LinesTotal
        {
            get { return Lines.Sum(x => x.LineTotal); }
        }

        public decimal LinesVat
        {
            get { return Lines.Sum(x => x.Vat ?? 0m); }
        }

        public decimal LinesCost
        {
            get { return Lines.Sum(x => (x.CostPrice ?? 0m) * (x.Quantity ?? 0m)); }
        }

        public decimal VoucherDebitTotal
        {
            get { return VoucherLines.Sum(x => x.Debit); }
        }

        public decimal VoucherCreditTotal
        {
            get { return VoucherLines.Sum(x => x.Credit); }
        }

        public decimal VoucherBalanceDifference
        {
            get { return VoucherDebitTotal - VoucherCreditTotal; }
        }

        public int AffectedAccountCount
        {
            get { return VoucherLines.Where(x => !string.IsNullOrWhiteSpace(x.AccountCodeInternal)).Select(x => x.AccountCodeInternal).Distinct().Count(); }
        }

        public decimal InventoryQuantityTotal
        {
            get { return Lines.Sum(x => x.ShowQty ?? x.Quantity ?? 0m); }
        }

        public decimal PaymentRowsTotal
        {
            get { return Payments.Sum(x => x.Value ?? 0m); }
        }

        public decimal PumpLinePaymentTotal
        {
            get { return Lines.Sum(x => (x.Cash ?? 0m) + (x.Mada ?? 0m) + (x.Visa ?? 0m) + (x.Deferred ?? 0m)); }
        }

        public decimal PumpDeferredDistributionTotal
        {
            get { return Lines.SelectMany(x => x.DeferredAllocations).Sum(x => x.Amount ?? 0m); }
        }

        public decimal PumpDeferredDistributionQuantityTotal
        {
            get { return Lines.SelectMany(x => x.DeferredAllocations).Sum(x => x.Quantity ?? 0m); }
        }

        public int PumpDeferredCustomerCount
        {
            get { return Lines.SelectMany(x => x.DeferredAllocations).Where(x => x.CustomerId.HasValue).Select(x => x.CustomerId.Value).Distinct().Count(); }
        }

        public int RelatedInventoryIssueCount
        {
            get { return RelatedInventoryTransactions.Count(x => x.TransactionType == 19); }
        }

        public int RelatedInventoryReceiptCount
        {
            get { return RelatedInventoryTransactions.Count(x => x.TransactionType == 20); }
        }
    }

    public class SalesInvoiceLineViewModel
    {
        public SalesInvoiceLineViewModel()
        {
            DeferredAllocations = new List<SalesInvoicePumpDeferredAllocationViewModel>();
        }

        public int Id { get; set; }
        public int? LineNumber { get; set; }
        public int? ItemId { get; set; }
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public string UnitName { get; set; }
        public int? UnitId { get; set; }
        public int? StoreId2 { get; set; }
        public decimal? ShowQty { get; set; }
        public decimal? Quantity { get; set; }
        public decimal? ShowPrice { get; set; }
        public decimal? Price { get; set; }
        public decimal? CostPrice { get; set; }
        public decimal? DiscountValue { get; set; }
        public decimal? TotalDiscountPerLine { get; set; }
        public decimal? Vat { get; set; }
        public decimal? VatYou { get; set; }
        public string Remarks { get; set; }
        public string AccountDisplay { get; set; }
        public string AccountCodeInternal { get; set; }
        public string CommissionAccountDisplay { get; set; }
        public string CommissionAccountCodeInternal { get; set; }
        public int? PumpId { get; set; }
        public string PumpName { get; set; }
        public bool? IsOther { get; set; }
        public int? ColorId { get; set; }
        public decimal? PrevQty { get; set; }
        public decimal? CurrentQty { get; set; }
        public decimal? Cash { get; set; }
        public decimal? Mada { get; set; }
        public decimal? Visa { get; set; }
        public decimal? Deferred { get; set; }
        public decimal? CashQty { get; set; }
        public decimal? MadaQty { get; set; }
        public decimal? VisaQty { get; set; }
        public decimal? DeferredQty { get; set; }
        public decimal? AmountH { get; set; }
        public decimal? AmountHCommission { get; set; }
        public string PumpDetails { get; set; }
        public IList<SalesInvoicePumpDeferredAllocationViewModel> DeferredAllocations { get; private set; }

        public decimal LineTotal
        {
            get { return (ShowQty ?? Quantity ?? 0m) * (ShowPrice ?? Price ?? 0m) - (DiscountValue ?? 0m) - (TotalDiscountPerLine ?? 0m); }
        }
    }

    public class SalesInvoicePumpDeferredAllocationViewModel
    {
        public int LineId { get; set; }
        public int? CustomerId { get; set; }
        public int? UnitId { get; set; }
        public decimal? Amount { get; set; }
        public string CustomerName { get; set; }
        public decimal? Quantity { get; set; }
        public decimal? UnitPrice { get; set; }
        public int? ReferenceNo { get; set; }
        public string CustomerAccountDisplay { get; set; }
        public string CustomerAccountCodeInternal { get; set; }
    }

    public class SalesInvoiceAuditLogViewModel
    {
        public int AuditId { get; set; }
        public string OperationName { get; set; }
        public string EntityName { get; set; }
        public string EntityKey { get; set; }
        public int? UserId { get; set; }
        public string UserDisplay { get; set; }
        public Guid? CorrelationId { get; set; }
        public string Message { get; set; }
        public string BeforeSnapshot { get; set; }
        public string AfterSnapshot { get; set; }
        public DateTime? CreatedAt { get; set; }
    }

    public class PumpDeferredDistributionEditViewModel
    {
        public PumpDeferredDistributionEditViewModel()
        {
            Allocations = new List<SalesInvoicePumpDeferredAllocationViewModel>();
            CustomerOptions = new List<SalesLookupOptionViewModel>();
            Warnings = new List<string>();
        }

        public int TransactionId { get; set; }
        public int LineId { get; set; }
        public string NoteSerial { get; set; }
        public DateTime? TransactionDate { get; set; }
        public string CustomerName { get; set; }
        public string ItemName { get; set; }
        public string PumpName { get; set; }
        public decimal? CurrentDeferred { get; set; }
        public decimal? CurrentDeferredQty { get; set; }
        public bool IsLocked { get; set; }
        public decimal? LineQuantityLimit { get; set; }
        public decimal? NonDeferredQuantity { get; set; }
        public string Message { get; set; }
        public IList<string> Warnings { get; private set; }
        public IList<SalesInvoicePumpDeferredAllocationViewModel> Allocations { get; set; }
        public IList<SalesLookupOptionViewModel> CustomerOptions { get; private set; }

        public decimal AllocationAmountTotal
        {
            get { return Allocations == null ? 0m : Allocations.Sum(x => x.Amount ?? 0m); }
        }

        public decimal AllocationQuantityTotal
        {
            get { return Allocations == null ? 0m : Allocations.Sum(x => x.Quantity ?? 0m); }
        }

        public decimal RemainingQuantityAfterDistribution
        {
            get { return (LineQuantityLimit ?? 0m) - (NonDeferredQuantity ?? 0m) - AllocationQuantityTotal; }
        }
    }

    public class SalesLookupOptionViewModel
    {
        public int Id { get; set; }
        public string Display { get; set; }
        public string AccountDisplay { get; set; }
    }

    public class PumpSalesEditViewModel
    {
        public PumpSalesEditViewModel()
        {
            Lines = new List<PumpSalesEditLineViewModel>();
            Payments = new List<PumpSalesEditPaymentViewModel>();
            CustomerOptions = new List<SalesLookupOptionViewModel>();
            BranchOptions = new List<SalesLookupOptionViewModel>();
            StoreOptions = new List<SalesLookupOptionViewModel>();
            BoxOptions = new List<SalesLookupOptionViewModel>();
            ItemOptions = new List<SalesLookupOptionViewModel>();
            UnitOptions = new List<SalesLookupOptionViewModel>();
            PumpOptions = new List<SalesLookupOptionViewModel>();
            PaymentOptions = new List<SalesLookupOptionViewModel>();
            Warnings = new List<string>();
        }

        public int? TransactionId { get; set; }
        public string NoteSerial1 { get; set; }
        public DateTime TransactionDate { get; set; }
        public int? BranchId { get; set; }
        public int? StoreId { get; set; }
        public int? BoxId { get; set; }
        public int? CustomerId { get; set; }
        public string CashCustomerName { get; set; }
        public string ManualNo { get; set; }
        public string Remarks { get; set; }
        public bool IsLocked { get; set; }
        public string Message { get; set; }
        public IList<string> Warnings { get; private set; }
        public IList<PumpSalesEditLineViewModel> Lines { get; set; }
        public IList<PumpSalesEditPaymentViewModel> Payments { get; set; }
        public IList<SalesLookupOptionViewModel> CustomerOptions { get; private set; }
        public IList<SalesLookupOptionViewModel> BranchOptions { get; private set; }
        public IList<SalesLookupOptionViewModel> StoreOptions { get; private set; }
        public IList<SalesLookupOptionViewModel> BoxOptions { get; private set; }
        public IList<SalesLookupOptionViewModel> ItemOptions { get; private set; }
        public IList<SalesLookupOptionViewModel> UnitOptions { get; private set; }
        public IList<SalesLookupOptionViewModel> PumpOptions { get; private set; }
        public IList<SalesLookupOptionViewModel> PaymentOptions { get; private set; }

        public decimal LinesTotal
        {
            get { return Lines == null ? 0m : Lines.Sum(x => x.LineTotal); }
        }

        public decimal VatTotal
        {
            get { return Lines == null ? 0m : Lines.Sum(x => x.AmountH ?? 0m); }
        }

        public decimal CashTotal
        {
            get { return Lines == null ? 0m : Lines.Sum(x => x.Cash ?? 0m); }
        }

        public decimal MadaTotal
        {
            get { return Lines == null ? 0m : Lines.Sum(x => x.Mada ?? 0m); }
        }

        public decimal VisaTotal
        {
            get { return Lines == null ? 0m : Lines.Sum(x => x.Visa ?? 0m); }
        }

        public decimal DeferredTotal
        {
            get { return Lines == null ? 0m : Lines.Sum(x => x.Deferred ?? 0m); }
        }

        public decimal PaymentTotal
        {
            get { return Payments == null ? 0m : Payments.Sum(x => x.Value ?? 0m); }
        }
    }

    public class PumpSalesEditLineViewModel
    {
        public int? Id { get; set; }
        public int? LineNumber { get; set; }
        public int? ItemId { get; set; }
        public int? UnitId { get; set; }
        public int? StoreId2 { get; set; }
        public int? PumpId { get; set; }
        public decimal? PrevQty { get; set; }
        public decimal? CurrentQty { get; set; }
        public decimal? ShowQty { get; set; }
        public decimal? Quantity { get; set; }
        public decimal? Price { get; set; }
        public decimal? CostPrice { get; set; }
        public decimal? Cash { get; set; }
        public decimal? Mada { get; set; }
        public decimal? Visa { get; set; }
        public decimal? Deferred { get; set; }
        public decimal? CashQty { get; set; }
        public decimal? MadaQty { get; set; }
        public decimal? VisaQty { get; set; }
        public decimal? DeferredQty { get; set; }
        public decimal? AmountH { get; set; }
        public decimal? AmountHCommission { get; set; }
        public string AccountCodeInternal { get; set; }
        public string CommissionAccountCodeInternal { get; set; }
        public bool? IsOther { get; set; }
        public string DetailsPump { get; set; }

        public decimal LineTotal
        {
            get { return (Cash ?? 0m) + (Mada ?? 0m) + (Visa ?? 0m) + (Deferred ?? 0m); }
        }

        public decimal DistributedQty
        {
            get { return (CashQty ?? 0m) + (MadaQty ?? 0m) + (VisaQty ?? 0m) + (DeferredQty ?? 0m); }
        }

        public decimal StillPumpQty
        {
            get { return (CurrentQty ?? 0m) - (PrevQty ?? 0m) - DistributedQty; }
        }
    }

    public class PumpSalesEditPaymentViewModel
    {
        public int? Id { get; set; }
        public int? PaymentId { get; set; }
        public decimal? Value { get; set; }
        public string CardNo { get; set; }
        public decimal? MaxValue { get; set; }
    }

    public class SalesInvoiceSaveResultViewModel
    {
        public bool Success { get; set; }
        public bool DryRun { get; set; }
        public int? TransactionId { get; set; }
        public string Message { get; set; }
    }

    public class PumpDeferredDistributionSaveResultViewModel
    {
        public bool Success { get; set; }
        public bool DryRun { get; set; }
        public string Message { get; set; }
        public string DetailsPump { get; set; }
        public decimal Deferred { get; set; }
        public decimal DeferredQty { get; set; }
    }

    public class SalesInvoiceVoucherLineViewModel
    {
        public int VoucherId { get; set; }
        public int? LineNo { get; set; }
        public int? NoteId { get; set; }
        public string NoteSerial { get; set; }
        public DateTime? RecordDate { get; set; }
        public string AccountDisplay { get; set; }
        public string AccountCodeInternal { get; set; }
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public string Description { get; set; }
    }

    public class SalesInvoicePaymentViewModel
    {
        public int Id { get; set; }
        public int? PaymentId { get; set; }
        public decimal? Value { get; set; }
        public string CardNo { get; set; }
        public decimal? MaxValue { get; set; }
    }

    public class SalesInvoiceRelatedInventoryTransactionViewModel
    {
        public int TransactionId { get; set; }
        public string TransactionSerial { get; set; }
        public int? TransactionType { get; set; }
        public DateTime? TransactionDate { get; set; }
        public string NoteSerial { get; set; }
        public string NoteSerial1 { get; set; }
        public string StoreName { get; set; }
        public decimal? Total { get; set; }
        public decimal? NetValue { get; set; }
        public int? NoteId { get; set; }
        public string LinkReason { get; set; }
    }

    public class SalesInvoiceDiagnosticsViewModel
    {
        public SalesInvoiceDiagnosticsViewModel()
        {
            TypeBreakdown = new List<SalesInvoiceDiagnosticRowViewModel>();
            PumpCandidates = new List<SalesInvoiceDiagnosticCandidateViewModel>();
        }

        public string DatabaseName { get; set; }
        public string FilterDescription { get; set; }
        public int RowCountFound { get; set; }
        public IList<SalesInvoiceDiagnosticRowViewModel> TypeBreakdown { get; private set; }
        public IList<SalesInvoiceDiagnosticCandidateViewModel> PumpCandidates { get; private set; }
    }

    public class SalesInvoiceDiagnosticRowViewModel
    {
        public int? TypeInvoice { get; set; }
        public int? TransactionType { get; set; }
        public int Count { get; set; }
        public int? MaxTransactionId { get; set; }
    }

    public class SalesInvoiceDiagnosticCandidateViewModel
    {
        public int TransactionId { get; set; }
        public int? TransactionType { get; set; }
        public int? TypeInvoice { get; set; }
        public string NoteSerial { get; set; }
        public DateTime? TransactionDate { get; set; }
        public string CustomerName { get; set; }
        public int PumpLineCount { get; set; }
    }

    public class SalesInvoiceSavePreviewViewModel
    {
        public SalesInvoiceSavePreviewViewModel()
        {
            HeaderEffects = new List<string>();
            DetailEffects = new List<string>();
            AccountingEffects = new List<string>();
            InventoryEffects = new List<string>();
            Warnings = new List<string>();
        }

        public IList<string> HeaderEffects { get; private set; }
        public IList<string> DetailEffects { get; private set; }
        public IList<string> AccountingEffects { get; private set; }
        public IList<string> InventoryEffects { get; private set; }
        public IList<string> Warnings { get; private set; }
    }
}
