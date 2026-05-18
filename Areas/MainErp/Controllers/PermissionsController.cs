using System;
using System.Data;
using System.Text;
using System.Web.Mvc;
using MyERP.Areas.MainErp.Infrastructure;
using MyERP.Areas.MainErp.ViewModels.Security;

namespace MyERP.Areas.MainErp.Controllers
{
    public class PermissionsController : MainErpControllerBase
    {
        private readonly WebScreenPermissionService _permissionService;

        public PermissionsController()
            : this(new WebScreenPermissionService())
        {
        }

        public PermissionsController(WebScreenPermissionService permissionService)
        {
            _permissionService = permissionService;
        }

        public ActionResult Index(string host = "", bool showAllAreas = false)
        {
            if (!_permissionService.Can(ResolvePermissionScreenKey(host), "View"))
            {
                return new HttpStatusCodeResult(403, "لا توجد صلاحية كافية لفتح شاشة صلاحيات الشاشات.");
            }

            ViewBag.ActiveScreen = string.Equals(host, "pos", StringComparison.OrdinalIgnoreCase)
                ? "main-erp-permissions"
                : "permissions";

            var isAdmin = MainErpUserContext != null && (MainErpUserContext.IsAdmin || MainErpUserContext.UserType.GetValueOrDefault(-1) == 0);
            var areaScope = string.Equals(host, "pos", StringComparison.OrdinalIgnoreCase) ? "POS" : "MainERP";
            var model = new MainErpPermissionsIndexViewModel
            {
                IsAdminView = isAdmin,
                Host = host,
                DefaultAreaFilter = areaScope,
                ActiveAreaScope = areaScope,
                Users = _permissionService.GetUsers(),
                Modules = _permissionService.GetModules(areaScope, false),
                Templates = _permissionService.GetTemplates()
            };

            return View(model);
        }

        [HttpGet]
        public JsonResult Matrix(WebPermissionMatrixRequest request)
        {
            try
            {
                request = NormalizeRequest(request);
                if (!_permissionService.Can(ResolvePermissionScreenKey(request.Host), "View"))
                {
                    Response.StatusCode = 403;
                    return Json(new { success = false, message = "لا توجد صلاحية كافية لفتح شاشة صلاحيات الشاشات." }, JsonRequestBehavior.AllowGet);
                }

                return Json(_permissionService.Search(request), JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                Response.StatusCode = 500;
                return Json(new { success = false, message = "تعذر تحميل صلاحيات الشاشات.", technicalMessage = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpPost]
        public JsonResult Save(WebPermissionSaveRequest request)
        {
            if (!IsAdmin())
            {
                Response.StatusCode = 403;
                return Json(new { success = false, message = "هذه الشاشة للمدير فقط." });
            }

            if (request == null || request.UserId <= 0)
            {
                Response.StatusCode = 400;
                return Json(new { success = false, message = "اختر المستخدم أولا." });
            }

            try
            {
                _permissionService.Save(request.UserId, request.Items);
                return Json(new { success = true, message = "تم حفظ الصلاحيات دفعة واحدة." });
            }
            catch (Exception ex)
            {
                Response.StatusCode = 500;
                return Json(new { success = false, message = "تعذر حفظ الصلاحيات.", technicalMessage = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult Copy(WebPermissionCopyRequest request)
        {
            if (!IsAdmin())
            {
                Response.StatusCode = 403;
                return Json(new { success = false, message = "هذه الشاشة للمدير فقط." });
            }

            if (request == null || request.SourceUserId <= 0 || request.TargetUserId <= 0 || request.SourceUserId == request.TargetUserId)
            {
                Response.StatusCode = 400;
                return Json(new { success = false, message = "اختر مستخدم مصدر ومستخدم هدف مختلفين." });
            }

            try
            {
                var rows = _permissionService.CopyPermissions(request.SourceUserId, request.TargetUserId, request.AreaName);
                return Json(new { success = true, message = "تم نسخ الصلاحيات.", affectedRows = rows });
            }
            catch (Exception ex)
            {
                Response.StatusCode = 500;
                return Json(new { success = false, message = "تعذر نسخ الصلاحيات.", technicalMessage = ex.Message });
            }
        }

        [HttpPost]
        public JsonResult ApplyTemplate(WebPermissionTemplateApplyRequest request)
        {
            if (!IsAdmin())
            {
                Response.StatusCode = 403;
                return Json(new { success = false, message = "هذه الشاشة للمدير فقط." });
            }

            if (request == null || request.TemplateId <= 0 || request.UserId <= 0)
            {
                Response.StatusCode = 400;
                return Json(new { success = false, message = "اختر المستخدم والقالب." });
            }

            try
            {
                var rows = _permissionService.ApplyTemplate(request.TemplateId, request.UserId);
                return Json(new { success = true, message = "تم تطبيق القالب على المستخدم.", affectedRows = rows });
            }
            catch (Exception ex)
            {
                Response.StatusCode = 500;
                return Json(new { success = false, message = "تعذر تطبيق القالب.", technicalMessage = ex.Message });
            }
        }

        [HttpGet]
        public FileResult Export(int userId, string areaName = "", bool showAllAreas = false)
        {
            var table = _permissionService.ExportMatrix(userId, ResolveAreaScope(Request["host"]), false);
            var content = BuildExcelFriendlyTsv(table);
            var bytes = Encoding.UTF8.GetPreamble();
            var data = Encoding.UTF8.GetBytes(content);
            var output = new byte[bytes.Length + data.Length];
            Buffer.BlockCopy(bytes, 0, output, 0, bytes.Length);
            Buffer.BlockCopy(data, 0, output, bytes.Length, data.Length);
            return File(output, "application/vnd.ms-excel", "web-screen-permissions.xls");
        }

        private WebPermissionMatrixRequest NormalizeRequest(WebPermissionMatrixRequest request)
        {
            request = request ?? new WebPermissionMatrixRequest();
            var isAdmin = IsAdmin();
            request.AreaName = ResolveAreaScope(request.Host);
            request.ShowAllAreas = false;
            return request;
        }

        private bool IsAdmin()
        {
            return MainErpUserContext != null && (MainErpUserContext.IsAdmin || MainErpUserContext.UserType.GetValueOrDefault(-1) == 0);
        }

        private static string ResolveAreaScope(string host)
        {
            return string.Equals(host, "pos", StringComparison.OrdinalIgnoreCase) ? "POS" : "MainERP";
        }

        private static string ResolvePermissionScreenKey(string host)
        {
            return string.Equals(host, "pos", StringComparison.OrdinalIgnoreCase) ? "POS.Admin.WebPermissions" : "MainERP.Admin.WebPermissions";
        }

        private static string BuildExcelFriendlyTsv(DataTable table)
        {
            var builder = new StringBuilder();
            for (var i = 0; i < table.Columns.Count; i++)
            {
                if (i > 0) builder.Append('\t');
                builder.Append(EscapeTsv(table.Columns[i].ColumnName));
            }

            builder.AppendLine();
            foreach (DataRow row in table.Rows)
            {
                for (var i = 0; i < table.Columns.Count; i++)
                {
                    if (i > 0) builder.Append('\t');
                    builder.Append(EscapeTsv(Convert.ToString(row[i])));
                }

                builder.AppendLine();
            }

            return builder.ToString();
        }

        private static string EscapeTsv(string value)
        {
            return (value ?? string.Empty).Replace("\t", " ").Replace("\r", " ").Replace("\n", " ");
        }
    }
}
