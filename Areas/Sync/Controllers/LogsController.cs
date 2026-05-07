using System.Web.Mvc;
using MyERP.Areas.Sync.Data;
using MyERP.Areas.Sync.Security;

namespace MyERP.Areas.Sync.Controllers
{
    [SyncAuthorize(SyncPermissions.Diagnostics)]
    public class LogsController : SyncControllerBase
    {
        private readonly SyncReadRepository repository = new SyncReadRepository();

        public ActionResult Index(string syncKey, bool errorsOnly = false)
        {
            ViewBag.ErrorsOnly = errorsOnly;
            ViewBag.SyncKey = syncKey;
            return View(repository.GetLogs(syncKey, errorsOnly));
        }
    }
}
