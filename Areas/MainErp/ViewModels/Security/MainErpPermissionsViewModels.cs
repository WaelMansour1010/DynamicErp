using System.Collections.Generic;

namespace MyERP.Areas.MainErp.ViewModels.Security
{
    public class MainErpPermissionsIndexViewModel
    {
        public bool IsAdminView { get; set; }
        public string Host { get; set; }
        public string DefaultAreaFilter { get; set; }
        public string ActiveAreaScope { get; set; }
        public IList<WebPermissionLookupItem> Users { get; set; }
        public IList<WebPermissionLookupItem> Modules { get; set; }
        public IList<WebPermissionLookupItem> Templates { get; set; }

        public MainErpPermissionsIndexViewModel()
        {
            Users = new List<WebPermissionLookupItem>();
            Modules = new List<WebPermissionLookupItem>();
            Templates = new List<WebPermissionLookupItem>();
        }
    }

    public class WebPermissionLookupItem
    {
        public int Id { get; set; }
        public string Key { get; set; }
        public string Name { get; set; }
        public string GroupName { get; set; }
    }

    public class WebPermissionDashboardDto
    {
        public int ActiveUsers { get; set; }
        public int WebScreens { get; set; }
        public int GrantedPermissions { get; set; }
        public int UsersWithoutPermissions { get; set; }
    }

    public class WebPermissionScreenDto
    {
        public int WebScreenId { get; set; }
        public int WebModuleId { get; set; }
        public string ModuleKey { get; set; }
        public string AreaName { get; set; }
        public string ScreenKey { get; set; }
        public string ArabicCaption { get; set; }
        public string EnglishCaption { get; set; }
        public string RouteUrl { get; set; }
        public string ControllerName { get; set; }
        public string ActionName { get; set; }
        public string IconCss { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; }
        public bool IsMenuVisible { get; set; }
        public WebPermissionFlags Permissions { get; set; }

        public WebPermissionScreenDto()
        {
            Permissions = new WebPermissionFlags();
        }
    }

    public class WebPermissionUserAccessDto
    {
        public int UserId { get; set; }
        public string UserName { get; set; }
        public string UserCategory { get; set; }
        public WebPermissionFlags Permissions { get; set; }

        public WebPermissionUserAccessDto()
        {
            Permissions = new WebPermissionFlags();
        }
    }

    public class WebPermissionFlags
    {
        public bool CanView { get; set; }
        public bool CanAdd { get; set; }
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }
        public bool CanPrint { get; set; }
        public bool CanExport { get; set; }
        public bool CanApprove { get; set; }
    }

    public class WebPermissionMatrixRequest
    {
        public int? UserId { get; set; }
        public int? ScreenId { get; set; }
        public string AreaName { get; set; }
        public string ModuleKey { get; set; }
        public string SearchText { get; set; }
        public string PermissionFilter { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public bool ShowAllAreas { get; set; }
        public string Host { get; set; }
    }

    public class WebPermissionMatrixResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalRows { get; set; }
        public WebPermissionDashboardDto Dashboard { get; set; }
        public IList<WebPermissionScreenDto> Screens { get; set; }
        public IList<WebPermissionUserAccessDto> Users { get; set; }

        public WebPermissionMatrixResponse()
        {
            Dashboard = new WebPermissionDashboardDto();
            Screens = new List<WebPermissionScreenDto>();
            Users = new List<WebPermissionUserAccessDto>();
        }
    }

    public class WebPermissionSaveRequest
    {
        public int UserId { get; set; }
        public IList<WebPermissionSaveItem> Items { get; set; }

        public WebPermissionSaveRequest()
        {
            Items = new List<WebPermissionSaveItem>();
        }
    }

    public class WebPermissionSaveItem : WebPermissionFlags
    {
        public int WebScreenId { get; set; }
    }

    public class WebPermissionCopyRequest
    {
        public int SourceUserId { get; set; }
        public int TargetUserId { get; set; }
        public string AreaName { get; set; }
    }

    public class WebPermissionTemplateApplyRequest
    {
        public int TemplateId { get; set; }
        public int UserId { get; set; }
    }

    public class WebPermissionBulkApplyRequest
    {
        public IList<int> UserIds { get; set; }
        public string Host { get; set; }
        public string AreaName { get; set; }
        public string ModuleKey { get; set; }
        public int? WebScreenId { get; set; }
        public string Mode { get; set; }

        public WebPermissionBulkApplyRequest()
        {
            UserIds = new List<int>();
        }
    }
}
