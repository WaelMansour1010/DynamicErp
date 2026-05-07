using System.Web.Mvc;
using MyERP.Areas.Sync.Data;
using MyERP.Areas.Sync.Security;

namespace MyERP.Areas.Sync.Controllers
{
    [SyncAuthorize(SyncPermissions.Notifications)]
    public class NotificationsController : SyncControllerBase
    {
        private readonly SyncAdminRepository repository = new SyncAdminRepository();

        public ActionResult Index()
        {
            return View(repository.GetNotifications());
        }
    }
}
