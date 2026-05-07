using System;

namespace MyERP.Areas.MainErp.Models.Accounting
{
    public class NumberingPreview
    {
        public string NumberingType { get; set; }
        public int BranchId { get; set; }
        public DateTime Date { get; set; }
        public long NextNumber { get; set; }
        public bool Success { get; set; }
        public string Warning { get; set; }
    }
}
