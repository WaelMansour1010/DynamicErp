using System.Web.Mvc;
using MyERP.Areas.Sync.Data;
using MyERP.Areas.Sync.Security;

namespace MyERP.Areas.Sync.Controllers
{
    [SyncAuthorize(SyncPermissions.View)]
    public class LiveStatusController : SyncControllerBase
    {
        private readonly SyncReadRepository repository = new SyncReadRepository();

        public ActionResult Snapshot()
        {
            var model = repository.GetDashboard();
            return Json(new
            {
                pending = model.PendingCount,
                conflicts = model.ConflictCount,
                failed = model.FailedCount,
                applied = model.AppliedCount,
                blocked = model.BlockedCount
            }, JsonRequestBehavior.AllowGet);
        }
    }
}
