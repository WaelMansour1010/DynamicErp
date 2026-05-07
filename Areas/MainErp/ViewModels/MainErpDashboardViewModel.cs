using System.Collections.Generic;

namespace MyERP.Areas.MainErp.ViewModels
{
    public class MainErpDashboardViewModel
    {
        public IList<MainErpModuleTileViewModel> Sections { get; set; }
    }

    public class MainErpModuleTileViewModel
    {
        public string Title { get; set; }
        public string ArabicTitle { get; set; }
        public string Status { get; set; }
        public string Url { get; set; }
    }
}
