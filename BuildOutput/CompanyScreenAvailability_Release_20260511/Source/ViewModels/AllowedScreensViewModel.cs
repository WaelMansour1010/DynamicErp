using System.Collections.Generic;

namespace MyERP.ViewModels
{
    public class AllowedScreensViewModel
    {
        public int SystemPageId { get; set; }
        public string SystemPageName { get; set; }
        public string ControllerName { get; set; }
        public int? ModuleId { get; set; }
        public string ModuleName { get; set; }
        public bool IsSelected { get; set; }
        public bool IsCritical { get; set; }
    }

    public class AllowedScreensSaveItem
    {
        public int SystemPageId { get; set; }
        public bool IsSelected { get; set; }
    }

    public class AllowedScreensMenuResult
    {
        public bool IsConfigured { get; set; }
        public IEnumerable<string> Controllers { get; set; }
    }
}
