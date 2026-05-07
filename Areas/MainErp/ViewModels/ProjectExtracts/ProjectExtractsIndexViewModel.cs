namespace MyERP.Areas.MainErp.ViewModels.ProjectExtracts
{
    public class ProjectExtractsIndexViewModel
    {
        public ProjectExtractsIndexViewModel()
        {
            Items = new System.Collections.Generic.List<ProjectExtractListItemViewModel>();
        }

        public string Title { get; set; }
        public string ArabicTitle { get; set; }
        public string AnalysisStatus { get; set; }
        public string SearchText { get; set; }
        public int? ProjectId { get; set; }
        public int? BranchId { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public string Warning { get; set; }
        public System.Collections.Generic.IList<ProjectExtractListItemViewModel> Items { get; private set; }
    }

    public class ProjectExtractListItemViewModel
    {
        public int Id { get; set; }
        public System.DateTime? BillDate { get; set; }
        public string NoteSerial { get; set; }
        public string ManualNo { get; set; }
        public string ProjectName { get; set; }
        public string ProjectFullCode { get; set; }
        public string CustomerName { get; set; }
        public decimal? Total { get; set; }
        public double? Results { get; set; }
        public double? VatValue { get; set; }
        public double? NetValue { get; set; }
        public int? BranchNo { get; set; }
        public int? NoteId { get; set; }
    }

    public class ProjectExtractDetailsViewModel : ProjectExtractListItemViewModel
    {
        public string Remarks { get; set; }
        public string RevenueAccount { get; set; }
        public string EndUserAccount { get; set; }
        public string SubUserAccount { get; set; }
        public double? AdvancedPayment { get; set; }
        public double? PerformanceBond { get; set; }
        public double? PreVat { get; set; }
        public string VatAccountCode { get; set; }
        public string Warning { get; set; }
    }
}
