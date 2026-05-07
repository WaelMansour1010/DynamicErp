namespace MyERP.Areas.MainErp.ViewModels.LC
{
    public class LCIndexViewModel
    {
        public LCIndexViewModel()
        {
            Items = new System.Collections.Generic.List<LCListItemViewModel>();
        }

        public string Title { get; set; }
        public string ArabicTitle { get; set; }
        public string AnalysisStatus { get; set; }
        public string SearchText { get; set; }
        public int? BankId { get; set; }
        public int? VendorId { get; set; }
        public int? BranchId { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public string Warning { get; set; }
        public System.Collections.Generic.IList<LCListItemViewModel> Items { get; private set; }
    }

    public class LCListItemViewModel
    {
        public int TblLCID { get; set; }
        public string LCNO { get; set; }
        public string Name { get; set; }
        public string BankName { get; set; }
        public string CurrencyName { get; set; }
        public string VendorName { get; set; }
        public decimal? Value { get; set; }
        public System.DateTime? FromDate { get; set; }
        public System.DateTime? ToDate { get; set; }
        public int? BranchID { get; set; }
        public string AccountCode { get; set; }
    }

    public class LCDetailsViewModel : LCListItemViewModel
    {
        public string Remarks { get; set; }
        public string MarginAccountCode { get; set; }
        public string AcceptAccountCode { get; set; }
        public string ExpenseAccountCode { get; set; }
        public double? OpenBalance { get; set; }
        public int? OpenBalanceType { get; set; }
        public string Warning { get; set; }
    }
}
