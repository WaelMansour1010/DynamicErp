using System;
using System.Net;
using System.Web.Mvc;
using MyERP.Areas.Sync.Data;
using MyERP.Areas.Sync.Security;
using MyERP.Areas.Sync.ViewModels;

namespace MyERP.Areas.Sync.Controllers
{
    [SyncAuthorize(SyncPermissions.View)]
    public class AdminOperationsController : SyncControllerBase
    {
        private readonly SyncAdminRepository repository = new SyncAdminRepository();

        public ActionResult Index(AdminOperationRequest request)
        {
            return View(repository.GetOperations(request));
        }

        [HttpPost]
        [SyncAuthorize(SyncPermissions.AdminOperations)]
        public ActionResult Queue(AdminOperationRequest request)
        {
            try
            {
                var id = repository.QueueOperation(request, HttpContext);
                TempData["SyncAdminMessage"] = "تم تسجيل طلب العملية في قائمة انتظار التنفيذ برقم " + id + ".";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                Response.StatusCode = (int)HttpStatusCode.BadRequest;
                ModelState.AddModelError("", ex.Message);
                return View("Index", repository.GetOperations(request));
            }
        }
    }
}
