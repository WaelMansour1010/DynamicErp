using MyERP.Areas.Pos.Data;
using MyERP.Areas.Pos.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Web.Mvc;

namespace MyERP.Areas.Pos.Controllers
{
    [AllowAnonymous]
    [SkipERPAuthorize]
    public class PosLoginController : Controller
    {
        public const string PosContextSessionKey = "PosUserContext";

        private readonly PosSqlRepository _repository;

        public PosLoginController()
        {
            _repository = new PosSqlRepository();
        }

        [HttpGet]
        public ActionResult Root()
        {
            return Redirect("~/Pos/PosLogin/Index");
        }

        [HttpGet]
        public ActionResult Index()
        {
            return View(new PosLoginRequest());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Index(PosLoginRequest request)
        {
            var context = TryEmergencyAdminLogin(request);
            if (context == null)
            {
                context = _repository.LoginPosUser(request != null ? request.UserName : null, request != null ? request.Password : null);
            }

            if (context == null)
            {
                ViewBag.ErrorMessage = "اسم المستخدم أو كلمة المرور غير صحيحة";
                return View(request);
            }

            Session[PosContextSessionKey] = context;
            Session["PosUserId"] = context.UserId;
            Session["PosUserName"] = context.UserName;
            Session["PosEmpId"] = context.EmpId;

            return RedirectToAction("Index", "PosDashboard", new { area = "Pos" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Logout()
        {
            Session.Remove(PosContextSessionKey);
            Session.Remove("PosUserId");
            Session.Remove("PosUserName");
            Session.Remove("PosEmpId");
            return RedirectToAction("Index");
        }

        private PosUserContext TryEmergencyAdminLogin(PosLoginRequest request)
        {
            if (!IsEnabled("PosEmergencyAdminEnabled"))
            {
                return null;
            }

            var password = request != null ? request.Password : null;
            if (!VerifyEmergencyPassword(password))
            {
                return null;
            }

            var adminUserName = ConfigurationManager.AppSettings["PosEmergencyAdminUserName"];
            if (string.IsNullOrWhiteSpace(adminUserName))
            {
                LogEmergencyLogin("Rejected: PosEmergencyAdminUserName is empty.", request, null);
                return null;
            }

            var context = _repository.GetActiveAdminUserContextByUserName(adminUserName);
            if (context == null || !context.IsFullAccess)
            {
                LogEmergencyLogin("Rejected: configured emergency user is not active admin.", request, null);
                return null;
            }

            LogEmergencyLogin("Accepted: emergency admin login.", request, context);
            return context;
        }

        private static bool VerifyEmergencyPassword(string password)
        {
            var saltText = ConfigurationManager.AppSettings["PosEmergencyAdminPasswordSalt"];
            var hashText = ConfigurationManager.AppSettings["PosEmergencyAdminPasswordHash"];
            if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(saltText) || string.IsNullOrWhiteSpace(hashText))
            {
                return false;
            }

            try
            {
                var salt = Convert.FromBase64String(saltText);
                var expectedHash = Convert.FromBase64String(hashText);
                using (var derive = new Rfc2898DeriveBytes(password, salt, 100000))
                {
                    var actualHash = derive.GetBytes(expectedHash.Length);
                    return FixedTimeEquals(actualHash, expectedHash);
                }
            }
            catch
            {
                return false;
            }
        }

        private static bool FixedTimeEquals(byte[] left, byte[] right)
        {
            if (left == null || right == null || left.Length != right.Length)
            {
                return false;
            }

            var diff = 0;
            for (var i = 0; i < left.Length; i++)
            {
                diff |= left[i] ^ right[i];
            }

            return diff == 0;
        }

        private static bool IsEnabled(string key)
        {
            return string.Equals(ConfigurationManager.AppSettings[key], "true", StringComparison.OrdinalIgnoreCase);
        }

        private static void LogEmergencyLogin(string action, PosLoginRequest request, PosUserContext context)
        {
            try
            {
                var logRoot = System.Web.HttpContext.Current == null
                    ? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data", "Logs")
                    : System.Web.HttpContext.Current.Server.MapPath("~/App_Data/Logs");
                Directory.CreateDirectory(logRoot);

                var lines = new List<string>
                {
                    "------------------------------------------------------------",
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture),
                    "Action: " + action,
                    "EnteredUserName: " + Mask(request != null ? request.UserName : null),
                    "AdminUserName: " + (context != null ? context.UserName : string.Empty),
                    "RemoteIP: " + (System.Web.HttpContext.Current != null ? System.Web.HttpContext.Current.Request.UserHostAddress : string.Empty)
                };

                var path = Path.Combine(logRoot, "pos-emergency-login-" + DateTime.Today.ToString("yyyyMMdd", CultureInfo.InvariantCulture) + ".log");
                System.IO.File.AppendAllLines(path, lines, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("Failed to write POS emergency login log: " + ex);
            }
        }

        private static string Mask(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            value = value.Trim();
            if (value.Length <= 2)
            {
                return new string('*', value.Length);
            }

            return value.Substring(0, 1) + new string('*', value.Length - 2) + value.Substring(value.Length - 1);
        }
    }
}
