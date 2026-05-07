using System.Web.Mvc;
using MyERP.Areas.Sync.Data;
using MyERP.Areas.Sync.Security;

namespace MyERP.Areas.Sync.Controllers
{
    [SyncAuthorize(SyncPermissions.Audit)]
    public class AdminAuditController : SyncControllerBase
    {
        private readonly SyncReadRepository repository = new SyncReadRepository();

        public ActionResult Index(bool dangerousOnly = false)
        {
            ViewBag.DangerousOnly = dangerousOnly;
            return View(repository.GetAudit(250, dangerousOnly));
        }
    }
}
