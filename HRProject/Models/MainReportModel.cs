using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EazyCash.Models
{
    public class MainReportModel
    {
        public string? ArabicName { get; set; }
        public string? EnglishName { get; set; }
        public string? NationalID { get; set; }
        public int Result { get; set; }
        public int Violation { get; set; }
        public string Description { get; set; }
        public DateTime OrderDate { get; set; }
        public string   BranchName { get; set; }
        public string     UserName { get; set; }
        public string ResNotes { get; set; }
    }

    public class MainReportParamsModel
    {
        public string FromDate { get; set; }
        public string ToDate { get; set; }
        public string BranchId { get; set; }
        public List<SelectListItem> Branches { get; set; }
        public List<MainReportModel> Report { get; set; }
         
    }
}
