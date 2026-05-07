namespace MyERP.Areas.MainErp.Models.Accounting
{
    public class AccountCodePreview
    {
        public string ParentAccountCode { get; set; }
        public string NextAccountCode { get; set; }
        public bool ParentExists { get; set; }
        public bool ParentIsLastAccount { get; set; }
        public bool Success { get; set; }
        public string Warning { get; set; }
    }
}
