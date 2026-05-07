using System.Web.Mvc;
using MyERP.Areas.Sync.Data;
using MyERP.Areas.Sync.Security;

namespace MyERP.Areas.Sync.Controllers
{
    [SyncAuthorize(SyncPermissions.Diagnostics)]
    public class DiagnosticsController : SyncControllerBase
    {
        private readonly SyncReadRepository repository = new SyncReadRepository();

        public ActionResult Index(string syncKey)
        {
            return View(repository.GetDiagnostics(syncKey));
        }
    }
}
