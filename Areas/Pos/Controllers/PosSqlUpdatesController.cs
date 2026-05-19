using MyERP.Areas.Pos.Data;
using MyERP.Areas.Pos.Models;
using MyERP.Areas.Pos.Services;
using System;
using System.Text;
using System.Web.Mvc;

namespace MyERP.Areas.Pos.Controllers
{
    public class PosSqlUpdatesController : Controller
    {
        private readonly PosSqlRepository _posRepository;
        private readonly PosSqlAutoUpdateService _updateService;

        public PosSqlUpdatesController()
        {
            _posRepository = new PosSqlRepository();
            _updateService = new PosSqlAutoUpdateService();
        }

        [HttpGet]
        public ActionResult Index()
        {
            var context = GetPosContext();
            if (context == null)
            {
                TempData["PosLoginMessage"] = PosLoginController.PosSessionExpiredMessage;
                return RedirectToAction("Index", "PosLogin", new { area = "Pos" });
            }

            if (!IsAdmin(context))
            {
                return AccessDenied();
            }

            ViewBag.PosContext = context;
            return View(new PosSqlUpdateDashboardViewModel { Status = _updateService.GetStatus() });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DryRun()
        {
            var context = GetPosContext();
            if (context == null)
            {
                TempData["PosLoginMessage"] = PosLoginController.PosSessionExpiredMessage;
                return RedirectToAction("Index", "PosLogin", new { area = "Pos" });
            }

            if (!IsAdmin(context))
            {
                return AccessDenied();
            }

            var run = _updateService.DryRun();
            ViewBag.PosContext = context;
            return View("Index", new PosSqlUpdateDashboardViewModel
            {
                Status = _updateService.GetStatus(),
                LastRun = run,
                Message = run.Message,
                IsError = !run.Success
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ApplyPending(bool? confirmBackup, bool? ignoreHashMismatch)
        {
            var context = GetPosContext();
            if (context == null)
            {
                TempData["PosLoginMessage"] = PosLoginController.PosSessionExpiredMessage;
                return RedirectToAction("Index", "PosLogin", new { area = "Pos" });
            }

            if (!IsAdmin(context))
            {
                return AccessDenied();
            }

            var ignoreHashMismatchValue = ignoreHashMismatch.GetValueOrDefault(false)
                || IsPostedTrue("ignoreHashMismatch")
                || IsPostedTrue("IgnoreHashMismatch")
                || IsPostedTrue("forceIgnoreHashMismatch");

            var run = _updateService.ApplyPending(new PosSqlUpdateRunRequest
            {
                ConfirmBackup = confirmBackup.GetValueOrDefault(false),
                IgnoreHashMismatch = ignoreHashMismatchValue,
                UserId = context.UserId,
                UserName = context.UserName,
                ClientIp = GetClientIp(),
                ReleaseNo = "POS_WEB_SQL_UPDATE"
            });

            ViewBag.PosContext = context;
            return View("Index", new PosSqlUpdateDashboardViewModel
            {
                Status = _updateService.GetStatus(),
                LastRun = run,
                Message = run.Message,
                IsError = !run.Success
            });
        }

        [HttpGet]
        public ActionResult DownloadLog(int id)
        {
            var context = GetPosContext();
            if (context == null)
            {
                TempData["PosLoginMessage"] = PosLoginController.PosSessionExpiredMessage;
                return RedirectToAction("Index", "PosLogin", new { area = "Pos" });
            }

            if (!IsAdmin(context))
            {
                return AccessDenied();
            }

            var log = _updateService.BuildRunLog(id);
            var bytes = Encoding.UTF8.GetBytes(log);
            return File(bytes, "text/plain", "POS_SQL_Update_Run_" + id + ".txt");
        }

        private ActionResult AccessDenied()
        {
            Response.TrySkipIisCustomErrors = true;
            return new HttpStatusCodeResult(403, "ليست لديك صلاحية إدارة تحديثات قاعدة البيانات");
        }

        private bool IsPostedTrue(string key)
        {
            var value = Request == null || Request.Form == null ? null : Request.Form[key];
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            foreach (var part in value.Split(','))
            {
                var normalized = (part ?? string.Empty).Trim();
                if (string.Equals(normalized, "true", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(normalized, "on", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(normalized, "1", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(normalized, "yes", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsAdmin(PosUserContext context)
        {
            return context != null && (context.UserType.GetValueOrDefault(-1) == 0 || context.IsFullAccess);
        }

        private PosUserContext GetPosContext()
        {
            return PosLoginController.RestorePosContext(Request, Session, _posRepository);
        }

        private string GetClientIp()
        {
            var forwarded = Request.Headers["X-Forwarded-For"];
            if (!string.IsNullOrWhiteSpace(forwarded))
            {
                return forwarded.Split(',')[0].Trim();
            }

            return Request.UserHostAddress ?? string.Empty;
        }
    }
}
