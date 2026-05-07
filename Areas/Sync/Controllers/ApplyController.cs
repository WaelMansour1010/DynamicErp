using System.Net;
using System.Web.Mvc;
using MyERP.Areas.Sync.Data;
using MyERP.Areas.Sync.Security;

namespace MyERP.Areas.Sync.Controllers
{
    [SyncAuthorize(SyncPermissions.View)]
    public class ApplyController : SyncControllerBase
    {
        private readonly SyncReadRepository repository = new SyncReadRepository();

        [HttpGet]
        public ActionResult Index(string syncKey)
        {
            return View(repository.GetDiagnostics(syncKey));
        }

        [HttpPost]
        public ActionResult RequestApply(string syncKey)
        {
            Response.StatusCode = (int)HttpStatusCode.Forbidden;
            return Content("ApplyMode execution is blocked from the ERP UI. Use the approved offline pilot checklist only.");
        }
    }
}
