using System;
using System.Collections.Generic;

namespace MyERP.Areas.MainErp.ViewModels.JournalEntries
{
    public class JournalEntriesIndexViewModel
    {
        public JournalEntriesIndexViewModel()
        {
            Items = new List<JournalEntryListItemViewModel>();
            ModeMessages = new List<string>();
        }

        public string Mode { get; set; }
        public bool IsOpeningBalance { get; set; }
        public bool IsPostingEnabled { get; set; }
        public string ScreenTitle { get; set; }
        public string ScreenIntro { get; set; }
        public string ModeBadge { get; set; }
        public string SearchActionName { get; set; }
        public string SearchText { get; set; }
        public int? BranchId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public string Warning { get; set; }
        public IList<string> ModeMessages { get; private set; }
        public IList<JournalEntryListItemViewModel> Items { get; private set; }
    }

    public class JournalEntryListItemViewModel
    {
        public int VoucherId { get; set; }
        public int? LineNo { get; set; }
        public int? NoteId { get; set; }
        public string NoteSerial { get; set; }
        public DateTime? RecordDate { get; set; }
        public string AccountCode { get; set; }
        public string AccountName { get; set; }
        public string AccountSerial { get; set; }
        public string AccountDisplay { get; set; }
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public string Description { get; set; }
        public int? BranchId { get; set; }
        public int? ProjectId { get; set; }
    }
}
