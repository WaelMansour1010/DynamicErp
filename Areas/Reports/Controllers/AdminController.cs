using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
        private readonly ReportCatalogService _catalogService = new ReportCatalogService();
        private readonly ReportValidationService _validationService = new ReportValidationService();
        private readonly ReportSuggestionService _suggestionService = new ReportSuggestionService();
        private readonly ReportLifecycleService _lifecycleService = new ReportLifecycleService();
        private readonly DynamicReportConnectionFactory _connectionFactory = new DynamicReportConnectionFactory();

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

        [HttpGet]
        public JsonResult CatalogList(string scope, string status)
        {
            try
            {
                var user = RequireDesigner(scope);
                return Json(new { success = true, data = _catalogService.List(user.ProjectScope, status, user) }, JsonRequestBehavior.AllowGet);
            }
            catch (HttpException)
            {
                return ForbiddenJsonGet();
            }
            catch (Exception ex)
            {
                Trace.TraceError("Dynamic report catalog list failed: " + ex);
                return BadRequestJsonGet("تعذر تحميل كتالوج التقارير.");
            }
        }

        [HttpGet]
        public JsonResult CatalogDetail(int catalogId, string scope)
        {
            try
            {
                var user = RequireDesigner(scope);
                return Json(new { success = true, data = _catalogService.GetDetail(catalogId, user.ProjectScope, user) }, JsonRequestBehavior.AllowGet);
            }
            catch (HttpException)
            {
                return ForbiddenJsonGet();
            }
            catch (Exception ex)
            {
                Trace.TraceError("Dynamic report catalog detail failed: " + ex);
                return BadRequestJsonGet("تعذر تحميل تفاصيل المصدر.");
            }
        }

        [HttpPost]
        public JsonResult CatalogDiscover(string scope)
        {
            try
            {
                var user = RequireDesigner(scope);
                var result = _catalogService.DiscoverAsync(user.ProjectScope, user).GetAwaiter().GetResult();
                return Json(new { success = true, data = result });
            }
            catch (HttpException)
            {
                return ForbiddenJson();
            }
            catch (Exception ex)
            {
                Trace.TraceError("Dynamic report catalog discovery failed: " + ex);
                return BadRequestJson("تعذر تنفيذ الاكتشاف. راجع السجل الداخلي.");
            }
        }

        [HttpPost]
        public JsonResult CatalogApprove(int catalogId, string scope, string suggestedName)
        {
            try
            {
                var user = RequireDesigner(scope);
                _catalogService.Approve(catalogId, suggestedName, user);
                return Json(new { success = true });
            }
            catch (HttpException)
            {
                return ForbiddenJson();
            }
            catch (Exception ex)
            {
                Trace.TraceError("Dynamic report catalog approve failed: " + ex);
                return BadRequestJson("تعذر اعتماد المصدر.");
            }
        }

        [HttpPost]
        public JsonResult CatalogReject(int catalogId, string scope, string reason)
        {
            try
            {
                var user = RequireDesigner(scope);
                _catalogService.Reject(catalogId, reason, user);
                return Json(new { success = true });
            }
            catch (HttpException)
            {
                return ForbiddenJson();
            }
            catch (Exception ex)
            {
                Trace.TraceError("Dynamic report catalog reject failed: " + ex);
                return BadRequestJson("تعذر رفض المصدر.");
            }
        }

        [HttpPost]
        public JsonResult CatalogImport(int catalogId, string scope)
        {
            try
            {
                var user = RequireDesigner(scope);
                var result = _catalogService.Import(catalogId, user);
                return Json(new { success = true, data = result });
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
                Trace.TraceError("Dynamic report catalog import failed: " + ex);
                return BadRequestJson("تعذر استيراد التقرير.");
            }
        }

        [HttpGet]
        public ActionResult Review(int id, string scope)
        {
            try
            {
                var user = RequireDesigner(scope);
                var definition = _definitionService.GetDefinition(id, user.ProjectScope);
                if (definition == null) return HttpNotFound("Dynamic report definition was not found.");
                PrepareView(user, ResolveAdminApiBase(user.ProjectScope), ResolveAdminLayout(user.ProjectScope));
                var model = new ReviewPageModel
                {
                    Definition = definition,
                    Suggestions = _suggestionService.BuildSuggestions(definition),
                    RiskFlags = GetImportedRiskFlags(definition.ReportId, user.ProjectScope),
                    ApiBase = ViewBag.DynamicReportsApiBase as string,
                    Scope = user.ProjectScope
                };
                return View("~/Areas/Reports/Views/Admin/Review.cshtml", model);
            }
            catch (HttpException)
            {
                return new HttpStatusCodeResult(403, "Forbidden");
            }
        }

        [HttpPost]
        public JsonResult ValidateReport(int id, string scope)
        {
            try
            {
                var user = RequireDesigner(scope);
                var definition = _definitionService.GetDefinition(id, user.ProjectScope);
                var report = _validationService.ValidateAsync(definition, user, true, ControllerContext);
                var status = _lifecycleService.MarkAfterValidation(id, user.ProjectScope, report, user);
                return Json(new { success = true, data = report, status });
            }
            catch (HttpException)
            {
                return ForbiddenJson();
            }
            catch (Exception ex)
            {
                Trace.TraceError("Dynamic report validation failed: " + ex);
                return BadRequestJson("تعذر فحص التقرير. راجع البيانات ثم أعد المحاولة.");
            }
        }

        [HttpPost]
        public JsonResult RunSample(int id, string scope, FormCollection form)
        {
            try
            {
                var user = RequireDesigner(scope);
                var result = _validationService.RunSample(id, user, FormToDictionary(form));
                return Json(new { success = result.Success, data = result, message = result.Message });
            }
            catch (HttpException)
            {
                return ForbiddenJson();
            }
            catch (Exception ex)
            {
                Trace.TraceError("Dynamic report sample failed: " + ex);
                return BadRequestJson("تعذر تشغيل عينة التقرير.");
            }
        }

        [HttpPost]
        public JsonResult PrintCheck(int id, string scope)
        {
            try
            {
                RequireDesigner(scope);
                var ok = _validationService.CanRenderPrint(ControllerContext);
                return Json(new { success = ok, message = ok ? "صفحة الطباعة جاهزة." : "تعذر العثور على صفحة الطباعة." });
            }
            catch (HttpException)
            {
                return ForbiddenJson();
            }
            catch (Exception ex)
            {
                Trace.TraceError("Dynamic report print check failed: " + ex);
                return BadRequestJson("تعذر فحص صفحة الطباعة.");
            }
        }

        [HttpPost]
        public JsonResult ApplySuggestions(int id, string scope)
        {
            try
            {
                var user = RequireDesigner(scope);
                var req = ReadJson<ApplySuggestionsRequest>() ?? new ApplySuggestionsRequest();
                var definition = _definitionService.GetDefinition(id, user.ProjectScope);
                if (definition == null) return BadRequestJson("تعريف التقرير غير موجود.");
                var count = ApplySuggestionsToDefinition(definition, req, user);
                var refreshed = _definitionService.GetDefinition(id, user.ProjectScope);
                return Json(new { success = true, updated = count, data = refreshed, suggestions = _suggestionService.BuildSuggestions(refreshed) });
            }
            catch (HttpException)
            {
                return ForbiddenJson();
            }
            catch (Exception ex)
            {
                Trace.TraceError("Dynamic report apply suggestions failed: " + ex);
                return BadRequestJson("تعذر تطبيق الاقتراحات.");
            }
        }

        [HttpPost]
        public JsonResult TransitionStatus(int id, string scope, string toStatus)
        {
            try
            {
                var user = RequireDesigner(scope);
                var result = _lifecycleService.TransitionStatus(id, toStatus, user);
                if (!result.Success) Response.StatusCode = 400;
                return Json(new { success = result.Success, data = result, message = result.Message });
            }
            catch (HttpException)
            {
                return ForbiddenJson();
            }
            catch (Exception ex)
            {
                Trace.TraceError("Dynamic report transition failed: " + ex);
                return BadRequestJson("تعذر تغيير حالة التقرير.");
            }
        }

        [HttpPost]
        public JsonResult MarkReviewed(int id, string scope)
        {
            try
            {
                var user = RequireDesigner(scope);
                var result = _lifecycleService.MarkReviewed(id, user);
                if (!result.Success) Response.StatusCode = 400;
                return Json(new { success = result.Success, data = result, message = result.Message });
            }
            catch (HttpException)
            {
                return ForbiddenJson();
            }
            catch (Exception ex)
            {
                Trace.TraceError("Dynamic report mark reviewed failed: " + ex);
                return BadRequestJson("تعذر حفظ مراجعة التقرير.");
            }
        }

        [HttpPost]
        public JsonResult RevertReview(int id, string scope)
        {
            try
            {
                var user = RequireDesigner(scope);
                var result = _lifecycleService.RevertReview(id, user);
                if (!result.Success) Response.StatusCode = 400;
                return Json(new { success = result.Success, data = result, message = result.Message });
            }
            catch (HttpException)
            {
                return ForbiddenJson();
            }
            catch (Exception ex)
            {
                Trace.TraceError("Dynamic report revert review failed: " + ex);
                return BadRequestJson("تعذر إلغاء مراجعة التقرير.");
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

        protected string ResolveAdminApiBase(string scope)
        {
            scope = DynamicReportScopes.Normalize(scope);
            if (scope == DynamicReportScopes.Pos) return "~/Pos/DynamicReportsAdmin";
            if (scope == DynamicReportScopes.MainErp) return "~/MainErp/DynamicReportsAdmin";
            return "~/Reports/Admin";
        }

        protected string ResolveAdminLayout(string scope)
        {
            scope = DynamicReportScopes.Normalize(scope);
            if (scope == DynamicReportScopes.Pos) return null;
            if (scope == DynamicReportScopes.MainErp) return "~/Areas/MainErp/Views/Shared/_MainErpLayout.cshtml";
            return "~/Views/Shared/_Layout.cshtml";
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

        private string GetImportedRiskFlags(int reportId, string scope)
        {
            const string sql = "SELECT TOP (1) RiskFlags FROM dbo.DynamicReportCatalog WHERE ImportedReportId = @ReportId AND ProjectScope = @ProjectScope ORDER BY ImportedAt DESC;";
            try
            {
                using (var connection = _connectionFactory.CreateOpenConnection(scope))
                using (var command = new SqlCommand(sql, connection))
                {
                    command.Parameters.Add("@ReportId", SqlDbType.Int).Value = reportId;
                    command.Parameters.Add("@ProjectScope", SqlDbType.NVarChar, 20).Value = DynamicReportScopes.Normalize(scope);
                    return Convert.ToString(command.ExecuteScalar());
                }
            }
            catch
            {
                return string.Empty;
            }
        }

        private int ApplySuggestionsToDefinition(DynamicReportDefinition definition, ApplySuggestionsRequest request, DynamicReportUserContext user)
        {
            var suggestions = _suggestionService.BuildSuggestions(definition);
            var updated = 0;
            foreach (var column in definition.Columns)
            {
                if (column == null || string.IsNullOrWhiteSpace(column.FieldName)) continue;
                if (!string.IsNullOrWhiteSpace(request.Field) && !string.Equals(request.Field, column.FieldName, StringComparison.OrdinalIgnoreCase)) continue;

                if (request.ApplyCaptions || string.Equals(request.Kind, "caption", StringComparison.OrdinalIgnoreCase))
                {
                    string caption;
                    if ((string.IsNullOrWhiteSpace(column.CaptionAr) || column.CaptionAr.Trim().StartsWith("⚠", StringComparison.Ordinal)) &&
                        suggestions.CaptionsAr.TryGetValue(column.FieldName, out caption))
                    {
                        column.CaptionAr = caption;
                        updated++;
                    }
                }

                if (request.ApplyGroupable || string.Equals(request.Kind, "group", StringComparison.OrdinalIgnoreCase))
                {
                    if (suggestions.GroupableHints.Contains(column.FieldName, StringComparer.OrdinalIgnoreCase) && !column.IsGroupable)
                    {
                        column.IsGroupable = true;
                        updated++;
                    }
                }

                if (request.ApplyFormatting || string.Equals(request.Kind, "format", StringComparison.OrdinalIgnoreCase))
                {
                    if (suggestions.Formatting.ContainsKey(column.FieldName) && !column.IsSummable)
                    {
                        column.IsSummable = true;
                        updated++;
                    }
                }
            }

            if (request.ApplySort && suggestions.SortHints.Count > 0)
            {
                var hint = suggestions.SortHints[0];
                var index = 0;
                foreach (var column in definition.Columns.OrderBy(c => string.Equals(c.FieldName, hint.Field, StringComparison.OrdinalIgnoreCase) ? 0 : 1).ThenBy(c => c.SortOrder))
                {
                    column.SortOrder = index++;
                }
                updated++;
            }

            _definitionService.SaveDefinition(definition, user);
            return updated;
        }

        private static IDictionary<string, string> FormToDictionary(FormCollection form)
        {
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (form == null) return values;
            foreach (var key in form.AllKeys)
            {
                if (string.IsNullOrWhiteSpace(key)) continue;
                values[key.TrimStart('@')] = form[key];
            }
            return values;
        }
    }
}
