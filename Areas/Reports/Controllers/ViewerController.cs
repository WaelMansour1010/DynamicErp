using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using MyERP.Areas.Reports.Models;
using MyERP.Areas.Reports.Services;

namespace MyERP.Areas.Reports.Controllers
{
    [AllowAnonymous]
    [SkipERPAuthorize]
    public class ViewerController : Controller
    {
        private readonly ReportDefinitionService _definitionService = new ReportDefinitionService();
        private readonly ReportPermissionService _permissionService = new ReportPermissionService();
        private readonly ReportExecutionService _executionService = new ReportExecutionService();
        private readonly ReportLayoutService _layoutService = new ReportLayoutService();

        public virtual ActionResult Index(string scope)
        {
            var user = CurrentUser(scope);
            if (!IsAuthenticatedForScope(user))
            {
                return RedirectToScopeLogin(user.ProjectScope);
            }
            PrepareView(user, "~/Reports/Viewer", "~/Views/Shared/_Layout.cshtml");
            return View("~/Areas/Reports/Views/Viewer/Index.cshtml");
        }

        [HttpGet]
        public JsonResult List(string scope)
        {
            var user = CurrentUser(scope);
            var allowed = new List<DynamicReportDefinition>();
            foreach (var definition in _definitionService.GetDefinitions(user.ProjectScope, false))
            {
                if (_permissionService.CanView(user, definition)) allowed.Add(definition);
            }
            return Json(new { success = true, data = allowed }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult Definition(int id, string scope)
        {
            var user = CurrentUser(scope);
            var definition = _definitionService.GetDefinition(id, user.ProjectScope);
            if (!_permissionService.CanView(user, definition)) return ForbiddenJsonGet();
            return Json(new { success = true, data = definition }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult Execute(string scope)
        {
            var user = CurrentUser(scope);
            var request = ReadJson<DynamicReportExecutionRequest>();
            if (request == null) return Json(new { success = false, message = "Invalid execution payload." });
            request.ProjectScope = user.ProjectScope;
            var result = _executionService.Execute(request, user);
            if (!result.Success) Response.StatusCode = 400;
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult Layouts(int reportId, string scope)
        {
            var user = CurrentUser(scope);
            var definition = _definitionService.GetDefinition(reportId, user.ProjectScope);
            if (!_permissionService.CanView(user, definition)) return ForbiddenJsonGet();
            return Json(new { success = true, data = _layoutService.GetLayouts(reportId, user) }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult SaveLayout(int reportId, string layoutName, bool? isDefault, string scope)
        {
            var user = CurrentUser(scope);
            var definition = _definitionService.GetDefinition(reportId, user.ProjectScope);
            if (!_permissionService.CanView(user, definition)) return ForbiddenJson();
            var id = _layoutService.SaveLayout(reportId, layoutName, ReadRawBody(), isDefault.GetValueOrDefault(false), user);
            return Json(new { success = true, layoutId = id });
        }

        [HttpPost]
        public JsonResult DeleteLayout(int layoutId, string scope)
        {
            var user = CurrentUser(scope);
            _layoutService.DeleteLayout(layoutId, user);
            return Json(new { success = true });
        }

        [HttpPost]
        public virtual ActionResult Print(int reportId, int? layoutId, string scope, FormCollection form)
        {
            var user = CurrentUser(scope);
            if (!IsAuthenticatedForScope(user))
            {
                return RedirectToScopeLogin(user.ProjectScope);
            }

            var definition = _definitionService.GetDefinition(reportId, user.ProjectScope);
            if (!_permissionService.CanView(user, definition))
            {
                return new HttpStatusCodeResult(403, "Print preview is not allowed for this report.");
            }

            var parameters = new Dictionary<string, string>();
            foreach (var key in form.AllKeys)
            {
                if (string.IsNullOrWhiteSpace(key)) continue;
                if (string.Equals(key, "reportId", System.StringComparison.OrdinalIgnoreCase)) continue;
                if (string.Equals(key, "layoutId", System.StringComparison.OrdinalIgnoreCase)) continue;
                if (string.Equals(key, "scope", System.StringComparison.OrdinalIgnoreCase)) continue;
                parameters[key] = form[key];
            }

            var request = new DynamicReportExecutionRequest
            {
                ReportId = reportId,
                ProjectScope = user.ProjectScope,
                Parameters = parameters
            };
            var result = _executionService.Execute(request, user);
            if (!result.Success)
            {
                Response.StatusCode = 400;
            }

            string layoutJson = null;
            if (layoutId.GetValueOrDefault() > 0)
            {
                var layout = _layoutService.GetLayouts(reportId, user).FirstOrDefault(x => x.LayoutId == layoutId.Value);
                if (layout != null)
                {
                    layoutJson = layout.LayoutJson;
                }
            }

            ViewBag.Definition = definition;
            ViewBag.Result = result;
            ViewBag.LayoutJson = layoutJson;
            ViewBag.User = user;
            ViewBag.Parameters = parameters;
            ViewBag.GeneratedAt = System.DateTime.Now;
            return View("~/Areas/Reports/Views/Viewer/Print.cshtml");
        }

        protected DynamicReportUserContext CurrentUser(string scope)
        {
            return DynamicReportSecurity.Build(HttpContext, scope);
        }

        protected void PrepareView(DynamicReportUserContext user, string apiBase, string layout)
        {
            ViewBag.Scope = user.ProjectScope;
            ViewBag.DynamicReportsApiBase = Url.Content(apiBase);
            ViewBag.LayoutPath = layout;
        }

        protected bool IsAuthenticatedForScope(DynamicReportUserContext user)
        {
            if (user.ProjectScope == DynamicReportScopes.Web) return Request.IsAuthenticated && user.UserId > 0;
            return user.UserId > 0;
        }

        protected ActionResult RedirectToScopeLogin(string scope)
        {
            if (scope == DynamicReportScopes.Pos) return RedirectToAction("Index", "PosLogin", new { area = "Pos", returnUrl = Request.RawUrl });
            if (scope == DynamicReportScopes.MainErp) return RedirectToAction("Index", "Login", new { area = "MainErp", returnUrl = Request.RawUrl });
            return Redirect("~/Login?ReturnUrl=" + Server.UrlEncode(Request.RawUrl));
        }

        protected JsonResult ForbiddenJsonGet()
        {
            Response.StatusCode = 403;
            return Json(new { success = false, message = "ليست لديك صلاحية لتنفيذ هذه العملية." }, JsonRequestBehavior.AllowGet);
        }

        protected JsonResult ForbiddenJson()
        {
            Response.StatusCode = 403;
            return Json(new { success = false, message = "ليست لديك صلاحية لتنفيذ هذه العملية." });
        }

        protected T ReadJson<T>() where T : class
        {
            var body = ReadRawBody();
            return string.IsNullOrWhiteSpace(body) ? null : new JavaScriptSerializer { MaxJsonLength = int.MaxValue }.Deserialize<T>(body);
        }

        protected string ReadRawBody()
        {
            Request.InputStream.Position = 0;
            using (var reader = new StreamReader(Request.InputStream))
            {
                return reader.ReadToEnd();
            }
        }
    }
}
