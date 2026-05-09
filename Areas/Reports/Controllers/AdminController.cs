using System;
using System.Diagnostics;
using System.IO;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using MyERP.Areas.Reports.Models;
using MyERP.Areas.Reports.Services;

namespace MyERP.Areas.Reports.Controllers
{
    [AllowAnonymous]
    [SkipERPAuthorize]
    public class AdminController : Controller
    {
        private readonly ReportDefinitionService _definitionService = new ReportDefinitionService();
        private readonly ReportMetadataService _metadataService = new ReportMetadataService();
        private readonly ReportPermissionService _permissionService = new ReportPermissionService();

        public virtual ActionResult Index(string scope)
        {
            var user = CurrentUser(scope);
            if (!IsAuthenticatedForScope(user))
            {
                return RedirectToScopeLogin(user.ProjectScope);
            }
            if (!_permissionService.CanDesign(user))
            {
                return new HttpStatusCodeResult(403, "Dynamic report administration requires admin permission.");
            }

            PrepareView(user, "~/Reports/Admin", "~/Views/Shared/_Layout.cshtml");
            return View("~/Areas/Reports/Views/Admin/Index.cshtml");
        }

        [HttpGet]
        public JsonResult List(string scope)
        {
            var user = CurrentUser(scope);
            if (!_permissionService.CanDesign(user)) return ForbiddenJsonGet();
            return Json(new { success = true, data = _definitionService.GetDefinitions(user.ProjectScope, true) }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult Get(int id, string scope)
        {
            var user = CurrentUser(scope);
            if (!_permissionService.CanDesign(user)) return ForbiddenJsonGet();
            return Json(new { success = true, data = _definitionService.GetDefinition(id, user.ProjectScope) }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public JsonResult ListPermissions(int reportId, string scope)
        {
            try
            {
                var user = RequireDesigner(scope);
                return Json(new { success = true, data = _permissionService.ListPermissions(reportId, user) }, JsonRequestBehavior.AllowGet);
            }
            catch (HttpException)
            {
                return ForbiddenJsonGet();
            }
            catch (Exception ex)
            {
                Trace.TraceError("Dynamic report permissions list failed: " + ex);
                return BadRequestJsonGet("تعذر تحميل صلاحيات التقرير.");
            }
        }

        [HttpGet]
        public JsonResult ListRoles(string scope)
        {
            try
            {
                var user = RequireDesigner(scope);
                return Json(new { success = true, data = _permissionService.ListRoles(user) }, JsonRequestBehavior.AllowGet);
            }
            catch (HttpException)
            {
                return ForbiddenJsonGet();
            }
            catch (Exception ex)
            {
                Trace.TraceError("Dynamic report roles list failed: " + ex);
                return BadRequestJsonGet("تعذر تحميل قائمة الأدوار.");
            }
        }

        [HttpGet]
        public JsonResult ListUsersLite(string scope, string q)
        {
            try
            {
                var user = RequireDesigner(scope);
                return Json(new { success = true, data = _permissionService.ListUsersLite(user, q) }, JsonRequestBehavior.AllowGet);
            }
            catch (HttpException)
            {
                return ForbiddenJsonGet();
            }
            catch (Exception ex)
            {
                Trace.TraceError("Dynamic report users list failed: " + ex);
                return BadRequestJsonGet("تعذر تحميل قائمة المستخدمين.");
            }
        }

        [HttpPost]
        public JsonResult Save(string scope)
        {
            var user = CurrentUser(scope);
            if (!_permissionService.CanDesign(user)) return ForbiddenJson();
            try
            {
                var definition = ReadJson<DynamicReportDefinition>();
                if (definition == null) return Json(new { success = false, message = "Invalid report payload." });
                if (string.IsNullOrWhiteSpace(definition.ProjectScope)) definition.ProjectScope = user.ProjectScope;
                var id = _definitionService.SaveDefinition(definition, user);
                return Json(new { success = true, reportId = id });
            }
            catch (Exception ex)
            {
                Trace.TraceError("Dynamic report save failed: " + ex);
                Response.StatusCode = 400;
                return Json(new { success = false, message = "تعذر حفظ التقرير. راجع البيانات المدخلة وتأكد أن الكود والمصدر صحيحان." });
            }
        }

        [HttpPost]
        public JsonResult LoadMetadata(string scope, string sourceType, string sourceName)
        {
            var user = CurrentUser(scope);
            if (!_permissionService.CanDesign(user)) return ForbiddenJson();
            try
            {
                var columns = _metadataService.LoadColumns(user.ProjectScope, sourceType, sourceName);
                var parameters = _metadataService.LoadStoredProcedureParameters(user.ProjectScope, sourceType, sourceName);
                return Json(new { success = true, columns, parameters });
            }
            catch
            {
                Response.StatusCode = 400;
                return Json(new { success = false, message = "Unable to read metadata. Verify the source exists and is safe." });
            }
        }

        [HttpPost]
        public JsonResult SavePermission(string scope)
        {
            try
            {
                var user = RequireDesigner(scope);
                var input = ReadJson<DynamicReportPermissionInput>();
                var saved = _permissionService.Grant(input, user);
                return Json(new { success = true, data = saved });
            }
            catch (HttpException)
            {
                return ForbiddenJson();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequestJson(ex.Message);
            }
            catch (Exception ex)
            {
                Trace.TraceError("Dynamic report permission save failed: " + ex);
                return BadRequestJson("تعذر حفظ صلاحية التقرير.");
            }
        }

        [HttpPost]
        public JsonResult DeletePermission(int permissionId, string scope)
        {
            try
            {
                var user = RequireDesigner(scope);
                _permissionService.Revoke(permissionId, user);
                return Json(new { success = true });
            }
            catch (HttpException)
            {
                return ForbiddenJson();
            }
            catch (Exception ex)
            {
                Trace.TraceError("Dynamic report permission delete failed: " + ex);
                return BadRequestJson("تعذر حذف صلاحية التقرير.");
            }
        }

        protected DynamicReportUserContext CurrentUser(string scope)
        {
            return DynamicReportSecurity.Build(HttpContext, scope);
        }

        protected DynamicReportUserContext RequireDesigner(string scope)
        {
            var user = DynamicReportSecurity.Build(HttpContext, scope);
            if (!_permissionService.CanDesign(user))
            {
                throw new HttpException(403, "Forbidden");
            }
            return user;
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
            if (scope == DynamicReportScopes.Pos)
            {
                return RedirectToAction("Index", "PosLogin", new { area = "Pos", returnUrl = Request.RawUrl });
            }
            if (scope == DynamicReportScopes.MainErp)
            {
                return RedirectToAction("Index", "Login", new { area = "MainErp", returnUrl = Request.RawUrl });
            }
            return Redirect("~/Login?ReturnUrl=" + Server.UrlEncode(Request.RawUrl));
        }

        protected JsonResult ForbiddenJson()
        {
            Response.StatusCode = 403;
            return Json(new { success = false, message = "ليست لديك صلاحية لتنفيذ هذه العملية." });
        }

        protected JsonResult ForbiddenJsonGet()
        {
            Response.StatusCode = 403;
            return Json(new { success = false, message = "ليست لديك صلاحية لتنفيذ هذه العملية." }, JsonRequestBehavior.AllowGet);
        }

        protected JsonResult BadRequestJson(string message)
        {
            Response.StatusCode = 400;
            return Json(new { success = false, message });
        }

        protected JsonResult BadRequestJsonGet(string message)
        {
            Response.StatusCode = 400;
            return Json(new { success = false, message }, JsonRequestBehavior.AllowGet);
        }

        protected T ReadJson<T>() where T : class
        {
            Request.InputStream.Position = 0;
            using (var reader = new StreamReader(Request.InputStream))
            {
                var body = reader.ReadToEnd();
                return string.IsNullOrWhiteSpace(body) ? null : new JavaScriptSerializer { MaxJsonLength = int.MaxValue }.Deserialize<T>(body);
            }
        }
    }
}
