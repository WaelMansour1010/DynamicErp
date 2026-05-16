using System.Collections.Generic;

namespace MyERP.Areas.MainErp.ViewModels.Security
{
    public class MainErpPermissionsIndexViewModel
    {
        public string SearchText { get; set; }
        public bool IsAdminView { get; set; }
        public int TotalUsers { get; set; }
        public int TotalScreens { get; set; }
        public int TotalPermissionRows { get; set; }
        public IList<MainErpPermissionUserSummary> Users { get; set; }
        public IList<MainErpPermissionScreenRow> Screens { get; set; }

        public MainErpPermissionsIndexViewModel()
        {
            Users = new List<MainErpPermissionUserSummary>();
            Screens = new List<MainErpPermissionScreenRow>();
        }
    }

    public class MainErpPermissionUserSummary
    {
        public int UserId { get; set; }
        public string UserName { get; set; }
        public int ScreenCount { get; set; }
        public int FullAccessCount { get; set; }
        public int HiddenCount { get; set; }
    }

    public class MainErpPermissionScreenRow
    {
        public string ScreenName { get; set; }
        public string DisplayName { get; set; }
        public string ModuleName { get; set; }
        public int UsersCount { get; set; }
        public bool CanShow { get; set; }
        public bool CanAdd { get; set; }
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }
        public bool CanPrint { get; set; }
        public bool CanSearch { get; set; }
        public bool FullAccess { get; set; }
    }
}
