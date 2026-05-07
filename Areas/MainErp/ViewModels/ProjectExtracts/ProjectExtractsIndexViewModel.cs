namespace MyERP.Areas.MainErp.ViewModels.ProjectExtracts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class ProjectExtractsIndexViewModel
    {
        public ProjectExtractsIndexViewModel()
        {
            Items = new List<ProjectExtractListItemViewModel>();
        }

        public string Title { get; set; }
        public string ArabicTitle { get; set; }
        public string AnalysisStatus { get; set; }
        public string SearchText { get; set; }
        public int? ProjectId { get; set; }
        public int? BranchId { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public string Warning { get; set; }
        public IList<ProjectExtractListItemViewModel> Items { get; private set; }
    }

    public class ProjectExtractListItemViewModel
    {
        public int Id { get; set; }
        public DateTime? BillDate { get; set; }
        public string NoteSerial { get; set; }
        public string ManualNo { get; set; }
        public string ProjectName { get; set; }
        public string ProjectFullCode { get; set; }
        public string CustomerName { get; set; }
        public decimal? Total { get; set; }
        public double? Results { get; set; }
        public double? VatValue { get; set; }
        public double? NetValue { get; set; }
        public int? BranchNo { get; set; }
        public int? NoteId { get; set; }
    }

    public class ProjectExtractDetailsViewModel : ProjectExtractListItemViewModel
    {
        public ProjectExtractDetailsViewModel()
        {
            DetailLines = new List<ProjectExtractDetailLineViewModel>();
            AdvancePayments = new List<ProjectExtractAdvancePaymentViewModel>();
            VoucherLines = new List<ProjectExtractVoucherLineViewModel>();
        }

        public string Remarks { get; set; }
        public string RevenueAccount { get; set; }
        public string AccountUnderImplementation { get; set; }
        public string EndUserAccount { get; set; }
        public string SubUserAccount { get; set; }
        public double? AdvancedPayment { get; set; }
        public double? PerformanceBond { get; set; }
        public double? PreVat { get; set; }
        public string VatAccountCode { get; set; }
        public string Warning { get; set; }
        public IList<ProjectExtractDetailLineViewModel> DetailLines { get; private set; }
        public IList<ProjectExtractAdvancePaymentViewModel> AdvancePayments { get; private set; }
        public IList<ProjectExtractVoucherLineViewModel> VoucherLines { get; private set; }

        public int DetailLineCount
        {
            get { return DetailLines.Count; }
        }

        public decimal DetailCurrentValueTotal
        {
            get { return DetailLines.Sum(x => x.CurrValue ?? 0m); }
        }

        public decimal DetailVatTotal
        {
            get { return DetailLines.Sum(x => x.LineVat ?? 0m); }
        }

        public decimal DetailFinalTotal
        {
            get { return DetailLines.Sum(x => x.LineFinal ?? 0m); }
        }

        public decimal DetailDiscountTotal
        {
            get { return DetailLines.Sum(x => x.LineDiscount ?? 0m); }
        }

        public decimal AdvancePaidTotal
        {
            get { return AdvancePayments.Sum(x => x.PayedValue ?? 0m); }
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
    }

    public class ProjectExtractDetailLineViewModel
    {
        public int Id { get; set; }
        public string Item { get; set; }
        public string FullCode { get; set; }
        public string Unit { get; set; }
        public decimal? Quantity { get; set; }
        public decimal? Price { get; set; }
        public decimal? Cost { get; set; }
        public decimal? PreQuantity { get; set; }
        public decimal? PreValue { get; set; }
        public decimal? CurrQuantity { get; set; }
        public decimal? CurrValue { get; set; }
        public decimal? TotalQuantity { get; set; }
        public decimal? TotalValue { get; set; }
        public decimal? CurrPercent { get; set; }
        public decimal? TotalPercent { get; set; }
        public decimal? LineDiscount { get; set; }
        public decimal? NetBeforeVat { get; set; }
        public decimal? LineVat { get; set; }
        public decimal? NetWithVat { get; set; }
        public decimal? PerformanceLineDiscount { get; set; }
        public decimal? LineFinal { get; set; }
        public string AccountDisplay { get; set; }
        public string AccountCodeInternal { get; set; }
    }

    public class ProjectExtractAdvancePaymentViewModel
    {
        public string SourceTable { get; set; }
        public int Id { get; set; }
        public int? NoteId { get; set; }
        public int? TransactionId { get; set; }
        public string NoteSerial { get; set; }
        public DateTime? NoteDate { get; set; }
        public decimal? NoteValue { get; set; }
        public decimal? PayedValue { get; set; }
        public decimal? TransPayedValue { get; set; }
        public decimal? RemainingValue { get; set; }
        public decimal? NetValue { get; set; }
        public decimal? Vat { get; set; }
        public int? TypeTrans { get; set; }
        public int? NCashingType { get; set; }
        public string BranchName { get; set; }
        public string AccountDisplay { get; set; }
        public string AccountCodeInternal { get; set; }
    }

    public class ProjectExtractVoucherLineViewModel
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
        public string BranchName { get; set; }
        public string ProjectName { get; set; }
    }
}
