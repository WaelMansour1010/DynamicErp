using System.Web.Mvc;
using MyERP.Areas.Sync.Data;
using MyERP.Areas.Sync.Security;
using MyERP.Areas.Sync.ViewModels;

namespace MyERP.Areas.Sync.Controllers
{
    [SyncAuthorize(SyncPermissions.View)]
    public class QueueController : SyncControllerBase
    {
        private readonly SyncReadRepository repository = new SyncReadRepository();

        public ActionResult Index(QueueFilter filter)
        {
            return View(repository.GetQueue(filter));
        }
    }
}
