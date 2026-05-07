using System;
using System.Collections.Generic;

namespace MyERP.Areas.MainErp.ViewModels.JournalEntries
{
    public class JournalEntryDetailsViewModel
    {
        public JournalEntryDetailsViewModel()
        {
            Lines = new List<JournalEntryDetailLineViewModel>();
            Warnings = new List<string>();
        }

        public int? NoteId { get; set; }
        public int? VoucherId { get; set; }
        public string SourceTable { get; set; }
        public DateTime? NoteDate { get; set; }
        public string NoteType { get; set; }
        public string NoteSerial { get; set; }
        public decimal? NoteValue { get; set; }
        public string Remark { get; set; }
        public int? TblLCID { get; set; }
        public decimal TotalDebit { get; set; }
        public decimal TotalCredit { get; set; }
        public decimal Difference { get { return TotalDebit - TotalCredit; } }
        public string Warning { get; set; }
        public IList<string> Warnings { get; private set; }
        public IList<JournalEntryDetailLineViewModel> Lines { get; private set; }
    }

    public class JournalEntryDetailLineViewModel
    {
        public string SourceTable { get; set; }
        public int VoucherId { get; set; }
        public int? LineNo { get; set; }
        public int? NoteId { get; set; }
        public DateTime? RecordDate { get; set; }
        public string AccountCode { get; set; }
        public string AccountName { get; set; }
        public string AccountSerial { get; set; }
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public string Description { get; set; }
        public int? BranchId { get; set; }
        public int? ProjectId { get; set; }
        public double? OpeningBalanceVoucherId { get; set; }
    }
}
