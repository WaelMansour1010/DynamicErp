using MyERP.Areas.Pos.Data;
using MyERP.Areas.Pos.Models;
using MyERP.Areas.Pos.Services;
using System;
using System.Text;
using System.Web.Mvc;

namespace MyERP.Areas.Pos.Controllers
{
    public class PosSystemHealthController : Controller
    {
        private readonly PosSqlRepository _posRepository = new PosSqlRepository();
        private readonly PosSystemHealthRepository _healthRepository = new PosSystemHealthRepository();

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
                return new HttpStatusCodeResult(403, "ليست لديك صلاحية مراقبة النظام");
            }

            Response.ContentEncoding = Encoding.UTF8;
            Response.Charset = "utf-8";
            ViewBag.PosContext = context;
            return View();
        }

        [HttpGet]
        public JsonResult Snapshot()
        {
            Response.ContentEncoding = Encoding.UTF8;
            Response.Charset = "utf-8";

            var context = GetPosContext();
            if (context == null)
            {
                Response.StatusCode = 401;
                Response.TrySkipIisCustomErrors = true;
                return Json(new { success = false, message = "انتهت الجلسة، برجاء تسجيل الدخول مرة أخرى" }, JsonRequestBehavior.AllowGet);
            }

            if (!IsAdmin(context))
            {
                Response.StatusCode = 403;
                Response.TrySkipIisCustomErrors = true;
                return Json(new { success = false, message = "ليست لديك صلاحية مراقبة النظام" }, JsonRequestBehavior.AllowGet);
            }

            var snapshot = new PosSystemHealthSnapshotDto
            {
                GeneratedAt = DateTime.Now,
                Core = PosSystemHealthMonitor.GetCoreMetrics(),
                Pos = PosSystemHealthMonitor.GetPosMetrics(),
                Database = _healthRepository.GetDatabaseHealth()
            };
            AddResourceHints(snapshot);
            AddAlerts(snapshot);
            return Json(new { success = true, data = snapshot }, JsonRequestBehavior.AllowGet);
        }

        private static void AddResourceHints(PosSystemHealthSnapshotDto snapshot)
        {
            snapshot.Resources.Add(new PosSystemHealthResourceDto
            {
                Name = "مصدر قياسات التطبيق",
                Value = "عدادات داخلية آخر 10 دقائق",
                Status = "ok"
            });
            snapshot.Resources.Add(new PosSystemHealthResourceDto
            {
                Name = "موارد السيرفر",
                Value = "يمكن ربط CPU/Memory لاحقا من Performance Counters حسب صلاحيات السيرفر",
                Status = "info"
            });
        }

        private static void AddAlerts(PosSystemHealthSnapshotDto snapshot)
        {
            if (snapshot.Core.AverageResponseMs > 800)
            {
                snapshot.Alerts.Add(Alert("High", "!", "زمن الاستجابة مرتفع", "متوسط زمن الاستجابة أكبر من 800ms في آخر دقائق."));
            }
            else if (snapshot.Core.AverageResponseMs > 300)
            {
                snapshot.Alerts.Add(Alert("Medium", "!", "زمن الاستجابة يحتاج متابعة", "متوسط زمن الاستجابة أكبر من 300ms."));
            }

            if (snapshot.Core.ErrorRatePercent >= 5)
            {
                snapshot.Alerts.Add(Alert("High", "!", "زيادة في الأخطاء", "معدل الأخطاء الحالي " + snapshot.Core.ErrorRatePercent.ToString("0.##") + "%."));
            }

            if (snapshot.Core.SessionRestoresLastHour > 30)
            {
                snapshot.Alerts.Add(Alert("High", "!", "ارتفاع غير طبيعي في استعادة الجلسات", "عدد استعادة جلسات POS في آخر ساعة مرتفع."));
            }
            else if (snapshot.Core.SessionRestoresLastHour > 10)
            {
                snapshot.Alerts.Add(Alert("Medium", "!", "استعادة الجلسات أعلى من المعتاد", "راجع إعدادات Session/AppPool إذا تكرر ذلك."));
            }

            if (!string.IsNullOrWhiteSpace(snapshot.Database.StatusMessage))
            {
                snapshot.Alerts.Add(Alert("Medium", "i", "صلاحيات مؤشرات قاعدة البيانات", snapshot.Database.StatusMessage));
            }

            if (snapshot.Database.BlockingSessions.Count > 0)
            {
                snapshot.Alerts.Add(Alert("High", "!", "يوجد عمليات حجز في قاعدة البيانات", "تم رصد جلسات SQL تقوم بحجز جلسات أخرى."));
            }

            if (snapshot.Pos.FailedSavesCount > 0)
            {
                snapshot.Alerts.Add(Alert("High", "!", "فشل في حفظ فواتير", "يوجد محاولات حفظ فواتير فشلت خلال آخر ساعة."));
            }

            if (snapshot.Alerts.Count == 0)
            {
                snapshot.Alerts.Add(Alert("Low", "OK", "النظام مستقر", "لا توجد إنذارات تشغيلية واضحة في القياسات الحالية."));
            }
        }

        private static PosSystemHealthAlertDto Alert(string severity, string icon, string title, string message)
        {
            return new PosSystemHealthAlertDto
            {
                Severity = severity,
                Icon = icon,
                Title = title,
                Message = message
            };
        }

        private static bool IsAdmin(PosUserContext context)
        {
            return context != null && (context.UserType.GetValueOrDefault(-1) == 0 || context.IsFullAccess);
        }

        private PosUserContext GetPosContext()
        {
            return PosLoginController.RestorePosContext(Request, Session, _posRepository);
        }
    }
}
