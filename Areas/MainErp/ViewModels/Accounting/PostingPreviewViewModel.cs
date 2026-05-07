using System.Collections.Generic;
using MyERP.Areas.MainErp.Models.Accounting;

namespace MyERP.Areas.MainErp.ViewModels.Accounting
{
    public class PostingPreviewViewModel
    {
        public PostingPreviewViewModel()
        {
            Entries = new List<VoucherEntry>();
            Warnings = new List<string>();
            Errors = new List<string>();
        }

        public string Title { get; set; }
        public string ArabicTitle { get; set; }
        public decimal TotalDebit { get; set; }
        public decimal TotalCredit { get; set; }
        public bool IsBalanced { get; set; }
        public IList<VoucherEntry> Entries { get; private set; }
        public IList<string> Warnings { get; private set; }
        public IList<string> Errors { get; private set; }
    }
}
