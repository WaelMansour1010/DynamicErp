using System.Collections.Generic;
using System.Linq;

namespace MyERP.Areas.MainErp.Models.Accounting
{
    public class PostingResult
    {
        public PostingResult()
        {
            Entries = new List<VoucherEntry>();
            Errors = new List<string>();
            Warnings = new List<string>();
        }

        public bool Success { get; set; }
        public bool PreviewOnly { get; set; }
        public long? VoucherId { get; set; }
        public decimal TotalDebit { get; set; }
        public decimal TotalCredit { get; set; }
        public IList<VoucherEntry> Entries { get; private set; }
        public IList<string> Errors { get; private set; }
        public IList<string> Warnings { get; private set; }

        public string ErrorSummary
        {
            get { return string.Join(" | ", Errors.ToArray()); }
        }
    }
}
