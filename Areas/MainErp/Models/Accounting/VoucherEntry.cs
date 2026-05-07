using System;

namespace MyERP.Areas.MainErp.Models.Accounting
{
    public class VoucherEntry
    {
        public int LineNumber { get; set; }
        public string AccountCode { get; set; }
        public decimal Value { get; set; }
        public VoucherEntryType EntryType { get; set; }
        public string Description { get; set; }
        public string DescriptionEnglish { get; set; }
        public DateTime RecordDate { get; set; }
        public int? NotesId { get; set; }
        public int? TransactionId { get; set; }
        public int? UserId { get; set; }
        public int? AccountIntervalId { get; set; }
        public int? ProjectBillNo { get; set; }
        public int? ProjectId { get; set; }
        public int? BillId { get; set; }
        public int? BranchId { get; set; }
        public decimal? CurrencyRate { get; set; }
        public decimal? OriginalCurrencyValue { get; set; }
        public decimal? VatValue { get; set; }
        public decimal? VatPercent { get; set; }
        public bool IsHiddenInvoice { get; set; }
    }
}
