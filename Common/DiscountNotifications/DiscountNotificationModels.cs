using System;
using System.Collections.Generic;

namespace MyERP.Common.DiscountNotifications
{
    public class DiscountNotificationIndexViewModel
    {
        public DiscountNotificationIndexViewModel()
        {
            Items = new List<DiscountNotificationListItem>();
        }

        public string SearchText { get; set; }
        public int? BranchId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int? SelectedId { get; set; }
        public string Warning { get; set; }
        public IList<DiscountNotificationListItem> Items { get; private set; }
        public DiscountNotificationDetailsViewModel SelectedDetails { get; set; }
        public bool IsPosMode { get; set; }
    }

    public class DiscountNotificationListItem
    {
        public int NoteId { get; set; }
        public string NoteSerial { get; set; }
        public string NoteSerial1 { get; set; }
        public DateTime? NoteDate { get; set; }
        public int? NoteType { get; set; }
        public string NotificationTypeName { get; set; }
        public string CustomerName { get; set; }
        public string BranchName { get; set; }
        public decimal NoteValue { get; set; }
        public decimal TotalValue { get; set; }
        public decimal Vat { get; set; }
        public string Remark { get; set; }
        public string OrderNo { get; set; }
    }

    public class DiscountNotificationDetailsViewModel : DiscountNotificationListItem
    {
        public DiscountNotificationDetailsViewModel()
        {
            VoucherLines = new List<DiscountNotificationVoucherLine>();
            Warnings = new List<string>();
        }

        public string DebitAccountDisplay { get; set; }
        public string CreditAccountDisplay { get; set; }
        public string CurrencyCode { get; set; }
        public decimal CurrencyRate { get; set; }
        public string FiterWaiverNoteSerial { get; set; }
        public int? InvoiceType { get; set; }
        public string InvoiceTypeCodeName { get; set; }
        public int VoucherLineCount { get; set; }
        public decimal TotalDebit { get; set; }
        public decimal TotalCredit { get; set; }
        public decimal Difference { get { return TotalDebit - TotalCredit; } }
        public IList<DiscountNotificationVoucherLine> VoucherLines { get; private set; }
        public IList<string> Warnings { get; private set; }
    }

    public class DiscountNotificationVoucherLine
    {
        public int VoucherId { get; set; }
        public int? LineNo { get; set; }
        public int? NoteId { get; set; }
        public DateTime? RecordDate { get; set; }
        public string AccountCode { get; set; }
        public string AccountSerial { get; set; }
        public string AccountName { get; set; }
        public string AccountDisplay { get; set; }
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public string Description { get; set; }
        public int? BranchId { get; set; }
    }
}
