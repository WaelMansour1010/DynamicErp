using System;
using System.Collections.Generic;

namespace MyERP.Areas.MainErp.ViewModels.Payments
{
    public class PaymentVoucherSearchViewModel
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string Serial { get; set; }
        public string Party { get; set; }
        public int? BranchId { get; set; }
        public string CashboxOrBank { get; set; }
        public decimal? Amount { get; set; }
        public string Warning { get; set; }
        public int TotalCount { get; set; }
        public List<PaymentVoucherListItemViewModel> Items { get; private set; }

        public PaymentVoucherSearchViewModel()
        {
            Items = new List<PaymentVoucherListItemViewModel>();
        }
    }

    public class PaymentVoucherListItemViewModel
    {
        public int NoteId { get; set; }
        public DateTime? NoteDate { get; set; }
        public string NoteSerial { get; set; }
        public string PartyDisplay { get; set; }
        public string BranchDisplay { get; set; }
        public string CashboxOrBankDisplay { get; set; }
        public decimal Amount { get; set; }
        public decimal Vat { get; set; }
        public decimal Total { get; set; }
        public string BalancedStatus { get; set; }
    }

    public class PaymentVoucherDetailsViewModel
    {
        public PaymentVoucherHeaderViewModel Header { get; set; }
        public List<PaymentVoucherAllocationLineViewModel> Allocations { get; private set; }
        public List<PaymentVoucherAccountingLineViewModel> AccountingEntries { get; private set; }
        public List<PaymentVoucherNoteTraceViewModel> RelatedNotes { get; private set; }
        public decimal DebitTotal { get; set; }
        public decimal CreditTotal { get; set; }
        public string Warning { get; set; }

        public PaymentVoucherDetailsViewModel()
        {
            Header = new PaymentVoucherHeaderViewModel();
            Allocations = new List<PaymentVoucherAllocationLineViewModel>();
            AccountingEntries = new List<PaymentVoucherAccountingLineViewModel>();
            RelatedNotes = new List<PaymentVoucherNoteTraceViewModel>();
        }

        public bool IsBalanced
        {
            get { return Math.Abs(DebitTotal - CreditTotal) < 0.01m; }
        }
    }

    public class PaymentVoucherHeaderViewModel
    {
        public int NoteId { get; set; }
        public string NoteSerial { get; set; }
        public DateTime? NoteDate { get; set; }
        public string ManualNo { get; set; }
        public string OrderNo { get; set; }
        public string PartyDisplay { get; set; }
        public string AccountDisplay { get; set; }
        public string BranchDisplay { get; set; }
        public string CashboxDisplay { get; set; }
        public string BankDisplay { get; set; }
        public string ChequeNumber { get; set; }
        public DateTime? ChequeDueDate { get; set; }
        public decimal CurrencyRate { get; set; }
        public string CurrencyDisplay { get; set; }
        public string CostCenterDisplay { get; set; }
        public string ProjectDisplay { get; set; }
        public bool IncludeVat { get; set; }
        public string PayDescription { get; set; }
        public string PayDescription2 { get; set; }
        public string PaymentMethodDisplay { get; set; }
        public string CashingTypeDisplay { get; set; }
        public string ReceiptClassDisplay { get; set; }
        public string Remark { get; set; }
        public decimal Amount { get; set; }
        public decimal Vat { get; set; }
        public decimal Total { get; set; }
        public int? VoucherId { get; set; }
        public string ReportName { get; set; }
    }

    public class PaymentVoucherAllocationLineViewModel
    {
        public string Source { get; set; }
        public string Serial { get; set; }
        public DateTime? Date { get; set; }
        public decimal OriginalValue { get; set; }
        public decimal PaidValue { get; set; }
        public decimal RemainingValue { get; set; }
        public string Description { get; set; }
    }

    public class PaymentVoucherAccountingLineViewModel
    {
        public int VoucherId { get; set; }
        public int LineNo { get; set; }
        public DateTime? RecordDate { get; set; }
        public string AccountDisplay { get; set; }
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public string Description { get; set; }
        public int? BranchId { get; set; }
    }

    public class PaymentVoucherNoteTraceViewModel
    {
        public int NoteId { get; set; }
        public string NoteSerial { get; set; }
        public int? NoteType { get; set; }
        public DateTime? NoteDate { get; set; }
        public decimal Amount { get; set; }
        public string Remark { get; set; }
    }
}
