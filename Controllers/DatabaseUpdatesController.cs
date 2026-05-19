using MyERP.Common.DatabaseUpdates;
using MyERP.Models;
using System;
using System.Security.Claims;
using System.Web.Mvc;

namespace MyERP.Controllers
{
    public class DatabaseUpdatesController : Controller
    {
        private readonly MySoftERPEntity _db = new MySoftERPEntity();
        private readonly SharedDatabaseUpdateService _service = new SharedDatabaseUpdateService();

        [HttpGet]
        public ActionResult Index()
        {
            var denied = RequireAdmin();
            if (denied != null)
            {
                return denied;
            }

            return View(_service.BuildDashboard(_db, TempData["DatabaseUpdates.Message"] as string, TempData["DatabaseUpdates.IsError"] as bool? == true));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ApplyPending(bool? confirmBackup)
        {
            var denied = RequireAdmin();
            if (denied != null)
            {
                return denied;
            }

            if (confirmBackup != true)
            {
                TempData["DatabaseUpdates.Message"] = "يجب تأكيد أخذ نسخة احتياطية قبل تطبيق تحديثات قاعدة البيانات.";
                TempData["DatabaseUpdates.IsError"] = true;
                return RedirectToAction("Index");
            }

            var result = _service.ApplyPending(_db, User.Identity.Name);
            TempData["DatabaseUpdates.Message"] = result.Message;
            TempData["DatabaseUpdates.IsError"] = !result.Success;
            return RedirectToAction("Index");
        }

        private ActionResult RequireAdmin()
        {
            if (!Request.IsAuthenticated)
            {
                return RedirectToAction("Index", "LogIn", new { returnUrl = Request.RawUrl });
            }

            var claim = ((ClaimsIdentity)User.Identity).FindFirst("Id");
            int userId;
            if (claim == null || !int.TryParse(claim.Value, out userId) || userId != 1)
            {
                return new HttpStatusCodeResult(403, "ليست لديك صلاحية إدارة تحديثات قاعدة البيانات.");
            }

            return null;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _db.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
