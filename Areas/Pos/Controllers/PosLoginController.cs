using MyERP.Areas.Pos.Data;
using MyERP.Areas.Pos.Models;
using MyERP.Areas.Pos.Services;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;

namespace MyERP.Areas.Pos.Controllers
{
    [AllowAnonymous]
    [SkipERPAuthorize]
    public class PosLoginController : Controller
    {
        public const string PosContextSessionKey = "PosUserContext";
        public const string PosContextCookieName = "POSCTX";
        public const string PosSessionExpiredMessage = "انتهت الجلسة، برجاء تسجيل الدخول مرة أخرى";
        private const string PosContextCookiePurpose = "DynamicErp.PosContext.v1";
        private const string PosContextRestoreAttemptKey = "DynamicErp.PosContext.RestoreAttempted";
        private const int PosContextCookieLifetimeHours = 12;
        private const int PosContextCookieRefreshMinutes = 30;
        private const int PosContextRestoreLogThrottleMinutes = 5;

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
            if (TempData["PosLoginMessage"] != null)
            {
                ViewBag.ErrorMessage = TempData["PosLoginMessage"];
            }

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
            SetPosContextCookie(Response, context);
            PosSystemHealthMonitor.TouchUser(context, Request);

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
            ExpirePosContextCookie(Response);
            return RedirectToAction("Index");
        }

        public static PosUserContext RestorePosContext(HttpRequestBase request, HttpSessionStateBase session, PosSqlRepository repository)
        {
            var context = session != null ? session[PosContextSessionKey] as PosUserContext : null;
            if (context != null)
            {
                PosSystemHealthMonitor.TouchUser(context, request);
                RefreshPosContextCookieIfNeeded(context);
                return context;
            }

            if (System.Web.HttpContext.Current != null && System.Web.HttpContext.Current.Items[PosContextRestoreAttemptKey] != null)
            {
                return null;
            }

            if (System.Web.HttpContext.Current != null)
            {
                System.Web.HttpContext.Current.Items[PosContextRestoreAttemptKey] = true;
            }

            bool invalidCookie;
            var userId = TryReadPosUserIdFromCookie(request, out invalidCookie);
            if (invalidCookie)
            {
                ExpireCurrentPosContextCookie();
            }

            if (!userId.HasValue || repository == null)
            {
                return null;
            }

            context = repository.GetPosUserDefaults(userId.Value);
            if (context == null)
            {
                ExpireCurrentPosContextCookie();
                return null;
            }

            if (session != null)
            {
                session[PosContextSessionKey] = context;
                session["PosUserId"] = context.UserId;
                session["PosUserName"] = context.UserName;
                session["PosEmpId"] = context.EmpId;
            }

            LogPosContextRestore(request, context);
            PosSystemHealthMonitor.RecordSessionRestore(context);
            PosSystemHealthMonitor.TouchUser(context, request);
            RefreshPosContextCookieIfNeeded(context);
            return context;
        }

        public static int? TryReadPosUserIdFromCookie(HttpRequestBase request)
        {
            bool invalidCookie;
            return TryReadPosUserIdFromCookie(request, out invalidCookie);
        }

        private static int? TryReadPosUserIdFromCookie(HttpRequestBase request, out bool invalidCookie)
        {
            invalidCookie = false;
            if (request == null || request.Cookies == null)
            {
                return null;
            }

            var cookie = request.Cookies[PosContextCookieName];
            if (cookie == null || string.IsNullOrWhiteSpace(cookie.Value))
            {
                return null;
            }

            try
            {
                var protectedBytes = Convert.FromBase64String(cookie.Value);
                var payloadBytes = MachineKey.Unprotect(protectedBytes, PosContextCookiePurpose);
                if (payloadBytes == null || payloadBytes.Length == 0)
                {
                    return null;
                }

                var payload = Encoding.UTF8.GetString(payloadBytes);
                var parts = payload.Split('|');
                int userId;
                if (parts.Length > 0 && int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out userId) && userId > 0)
                {
                    return userId;
                }
            }
            catch
            {
                invalidCookie = true;
                return null;
            }

            invalidCookie = true;
            return null;
        }

        private static void SetPosContextCookie(HttpResponseBase response, PosUserContext context)
        {
            if (response == null || context == null || context.UserId <= 0)
            {
                return;
            }

            var payload = context.UserId.ToString(CultureInfo.InvariantCulture) + "|" + DateTime.UtcNow.Ticks.ToString(CultureInfo.InvariantCulture);
            var protectedBytes = MachineKey.Protect(Encoding.UTF8.GetBytes(payload), PosContextCookiePurpose);
            var cookie = new HttpCookie(PosContextCookieName, Convert.ToBase64String(protectedBytes))
            {
                HttpOnly = true,
                Expires = DateTime.UtcNow.AddHours(PosContextCookieLifetimeHours),
                Path = "/",
                SameSite = SameSiteMode.Lax
            };

            if (System.Web.HttpContext.Current != null && System.Web.HttpContext.Current.Request != null)
            {
                cookie.Secure = System.Web.HttpContext.Current.Request.IsSecureConnection;
            }

            response.Cookies.Set(cookie);
        }

        private static void RefreshPosContextCookieIfNeeded(PosUserContext context)
        {
            try
            {
                if (context == null || context.UserId <= 0 || System.Web.HttpContext.Current == null || System.Web.HttpContext.Current.Response == null)
                {
                    return;
                }

                var cacheKey = "DynamicErp.PosContext.CookieRefresh." + context.UserId.ToString(CultureInfo.InvariantCulture);
                if (System.Web.HttpRuntime.Cache[cacheKey] != null)
                {
                    return;
                }

                SetPosContextCookie(new HttpResponseWrapper(System.Web.HttpContext.Current.Response), context);
                System.Web.HttpRuntime.Cache.Insert(cacheKey, true, null, DateTime.UtcNow.AddMinutes(PosContextCookieRefreshMinutes), System.Web.Caching.Cache.NoSlidingExpiration);
            }
            catch
            {
                // Cookie renewal is a resilience helper; do not fail the POS request if renewal cannot be written.
            }
        }

        private static void ExpireCurrentPosContextCookie()
        {
            try
            {
                if (System.Web.HttpContext.Current == null || System.Web.HttpContext.Current.Response == null)
                {
                    return;
                }

                ExpirePosContextCookie(new HttpResponseWrapper(System.Web.HttpContext.Current.Response));
            }
            catch
            {
                // Invalid cookies should not fail the POS request pipeline.
            }
        }

        private static void ExpirePosContextCookie(HttpResponseBase response)
        {
            if (response == null)
            {
                return;
            }

            var cookie = new HttpCookie(PosContextCookieName, string.Empty)
            {
                HttpOnly = true,
                Expires = DateTime.UtcNow.AddDays(-1),
                Path = "/",
                SameSite = SameSiteMode.Lax
            };

            if (System.Web.HttpContext.Current != null && System.Web.HttpContext.Current.Request != null)
            {
                cookie.Secure = System.Web.HttpContext.Current.Request.IsSecureConnection;
            }

            response.Cookies.Set(cookie);
        }

        private static void LogPosContextRestore(HttpRequestBase request, PosUserContext context)
        {
            try
            {
                var logRoot = System.Web.HttpContext.Current == null
                    ? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data", "Logs")
                    : System.Web.HttpContext.Current.Server.MapPath("~/App_Data/Logs");
                Directory.CreateDirectory(logRoot);

                var route = request != null && request.RequestContext != null && request.RequestContext.RouteData != null
                    ? request.RequestContext.RouteData.Values
                    : null;

                var controller = route != null && route.ContainsKey("controller") ? Convert.ToString(route["controller"], CultureInfo.InvariantCulture) : string.Empty;
                var action = route != null && route.ContainsKey("action") ? Convert.ToString(route["action"], CultureInfo.InvariantCulture) : string.Empty;
                var throttleKey = "DynamicErp.PosContext.RestoreLog." + (context != null ? context.UserId.ToString(CultureInfo.InvariantCulture) : "0");
                if (System.Web.HttpRuntime.Cache[throttleKey] != null)
                {
                    return;
                }

                var lines = new List<string>
                {
                    "------------------------------------------------------------",
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture),
                    "Action: RestorePosContext",
                    "UserId: " + (context != null ? context.UserId.ToString(CultureInfo.InvariantCulture) : string.Empty),
                    "BranchId: " + (context != null && context.BranchId.HasValue ? context.BranchId.Value.ToString(CultureInfo.InvariantCulture) : string.Empty),
                    "Controller: " + controller,
                    "MvcAction: " + action,
                    "RemoteIP: " + (request != null ? request.UserHostAddress : string.Empty)
                };

                var path = Path.Combine(logRoot, "pos-session-restore-" + DateTime.Today.ToString("yyyyMMdd", CultureInfo.InvariantCulture) + ".log");
                System.IO.File.AppendAllLines(path, lines, Encoding.UTF8);
                System.Web.HttpRuntime.Cache.Insert(throttleKey, true, null, DateTime.UtcNow.AddMinutes(PosContextRestoreLogThrottleMinutes), System.Web.Caching.Cache.NoSlidingExpiration);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("Failed to write POS session restore log: " + ex);
            }
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
