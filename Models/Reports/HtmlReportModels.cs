using System;
using System.Collections.Generic;

namespace MyERP.Models.Reports
{
    public class HtmlReportFilterModel
    {
        public string ReportKey { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int? BranchId { get; set; }
        public int? StoreId { get; set; }
        public string SerialSearch { get; set; }
        public string AccountFrom { get; set; }
        public string AccountTo { get; set; }
        public string AccountCodes { get; set; }
                public int? CostCenterId { get; set; }
        public int? ProjectId { get; set; }
        public int? ActivityId { get; set; }
        public int? RegionId { get; set; }
        public int? NoteType { get; set; }
        public int? AccountLevel { get; set; }
        public bool? HideZeroBalance { get; set; }
        public bool? Detailed { get; set; }}

    public class HtmlReportPageViewModel
    {
        public string PageTitle { get; set; }
        public HtmlReportFilterModel Filter { get; set; }
        public IList<HtmlReportDefinition> Reports { get; set; }
        public IList<HtmlReportLookupItem> Branches { get; set; }
        public IList<HtmlReportLookupItem> Stores { get; set; }
        public HtmlReportDefinition ActiveReport { get; set; }
        public HtmlReportResultModel Result { get; set; }
        public string Message { get; set; }

        public HtmlReportPageViewModel()
        {
            Filter = new HtmlReportFilterModel();
            Reports = new List<HtmlReportDefinition>();
            Branches = new List<HtmlReportLookupItem>();
            Stores = new List<HtmlReportLookupItem>();
        }
    }

    public class HtmlReportDefinition
    {
        public string Key { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string SourceName { get; set; }
        public bool SupportsStoreFilter { get; set; }
    }

    public class HtmlReportLookupItem
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class HtmlReportResultModel
    {
        public string Title { get; set; }
        public IList<HtmlReportColumnModel> Columns { get; set; }
        public IList<IDictionary<string, object>> Rows { get; set; }
        public IDictionary<string, decimal> Totals { get; set; }

        public HtmlReportResultModel()
        {
            Columns = new List<HtmlReportColumnModel>();
            Rows = new List<IDictionary<string, object>>();
            Totals = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        }
    }

    public class HtmlReportColumnModel
    {
        public string Key { get; set; }
        public string Title { get; set; }
        public bool IsNumeric { get; set; }
        public bool ShowTotal { get; set; }
    }
}

