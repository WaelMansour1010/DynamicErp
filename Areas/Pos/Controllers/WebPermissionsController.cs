using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Text;
using System.Web.Mvc;
using MyERP.Areas.MainErp.Infrastructure;
using MyERP.Areas.MainErp.Interfaces;
using MyERP.Areas.MainErp.ViewModels.Security;
using MyERP.Areas.Pos.Data;
using MyERP.Areas.Pos.Models;

namespace MyERP.Areas.Pos.Controllers
{
    public class WebPermissionsController : Controller
    {
        private readonly PosSqlRepository _repository;
        private readonly WebScreenPermissionService _permissionService;

        public WebPermissionsController()
            : this(new PosSqlRepository(), new WebScreenPermissionService(new PosWebPermissionConnectionFactory()))
        {
        }

        public WebPermissionsController(PosSqlRepository repository, WebScreenPermissionService permissionService)
        {
            _repository = repository;
            _permissionService = permissionService;
        }

        public ActionResult Index()
        {
            var context = GetPosContext();
            if (context == null)
            {
                return RedirectToAction("Index", "PosLogin", new { area = "Pos" });
            }

            if (!_permissionService.Can(context, "POS.Admin.WebPermissions", "View"))
            {
                return new HttpStatusCodeResult(403, "لا توجد صلاحية كافية لفتح شاشة الصلاحيات على الشاشات.");
            }

            ViewBag.ActiveScreen = "main-erp-permissions";
            var model = new MainErpPermissionsIndexViewModel
            {
                IsAdminView = IsAdmin(context),
                Host = "pos",
                DefaultAreaFilter = "POS",
                ActiveAreaScope = "POS",
                Users = _permissionService.GetUsers(),
                Modules = _permissionService.GetModules("POS", false),
                Templates = _permissionService.GetTemplates()
            };

            return View("~/Areas/MainErp/Views/Permissions/Index.cshtml", model);
        }

        [HttpGet]
        public JsonResult Matrix(WebPermissionMatrixRequest request)
        {
            var context = GetPosContext();
            if (context == null)
            {
                Response.StatusCode = 401;
                return Json(new { success = false, message = "يجب تسجيل دخول نقطة البيع أولا." }, JsonRequestBehavior.AllowGet);
            }

            if (!_permissionService.Can(context, "POS.Admin.WebPermissions", "View"))
            {
                Response.StatusCode = 403;
                return Json(new { success = false, message = "لا توجد صلاحية كافية لفتح شاشة الصلاحيات على الشاشات." }, JsonRequestBehavior.AllowGet);
            }

            try
            {
                request = NormalizeRequest(request);
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
            var context = GetPosContext();
            if (!IsAdmin(context))
            {
                Response.StatusCode = context == null ? 401 : 403;
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
            var context = GetPosContext();
            if (!IsAdmin(context))
            {
                Response.StatusCode = context == null ? 401 : 403;
                return Json(new { success = false, message = "هذه الشاشة للمدير فقط." });
            }

            if (request == null || request.SourceUserId <= 0 || request.TargetUserId <= 0 || request.SourceUserId == request.TargetUserId)
            {
                Response.StatusCode = 400;
                return Json(new { success = false, message = "اختر مستخدم مصدر ومستخدم هدف مختلفين." });
            }

            try
            {
                var rows = _permissionService.CopyPermissions(request.SourceUserId, request.TargetUserId, "POS");
                return Json(new { success = true, message = "تم نسخ صلاحيات POS فقط.", affectedRows = rows });
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
            var context = GetPosContext();
            if (!IsAdmin(context))
            {
                Response.StatusCode = context == null ? 401 : 403;
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

        [HttpPost]
        public JsonResult BulkApply(WebPermissionBulkApplyRequest request)
        {
            var context = GetPosContext();
            if (!IsAdmin(context))
            {
                Response.StatusCode = context == null ? 401 : 403;
                return Json(new { success = false, message = "هذه الشاشة للمدير فقط." });
            }

            if (request == null || request.UserIds == null || request.UserIds.Count == 0)
            {
                Response.StatusCode = 400;
                return Json(new { success = false, message = "اختر مستخدما واحدا على الأقل." });
            }

            try
            {
                request.AreaName = "POS";
                var rows = _permissionService.ApplyBulk(request);
                return Json(new { success = true, message = "تم تطبيق التعديل الجماعي على POS فقط.", affectedRows = rows });
            }
            catch (Exception ex)
            {
                Response.StatusCode = 500;
                return Json(new { success = false, message = "تعذر تطبيق التعديل الجماعي.", technicalMessage = ex.Message });
            }
        }

        [HttpGet]
        public FileResult Export(int userId)
        {
            var table = _permissionService.ExportMatrix(userId, "POS", false);
            var content = BuildExcelFriendlyTsv(table);
            var preamble = Encoding.UTF8.GetPreamble();
            var data = Encoding.UTF8.GetBytes(content);
            var output = new byte[preamble.Length + data.Length];
            Buffer.BlockCopy(preamble, 0, output, 0, preamble.Length);
            Buffer.BlockCopy(data, 0, output, preamble.Length, data.Length);
            return File(output, "application/vnd.ms-excel", "pos-web-screen-permissions.xls");
        }

        private PosUserContext GetPosContext()
        {
            return PosLoginController.RestorePosContext(Request, Session, _repository);
        }

        private static bool IsAdmin(PosUserContext context)
        {
            return context != null && (context.IsFullAccess || context.UserType.GetValueOrDefault(-1) == 0 || context.CanChangeDefaults);
        }

        private static WebPermissionMatrixRequest NormalizeRequest(WebPermissionMatrixRequest request)
        {
            request = request ?? new WebPermissionMatrixRequest();
            request.Host = "pos";
            request.AreaName = "POS";
            request.ShowAllAreas = false;
            return request;
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

        private sealed class PosWebPermissionConnectionFactory : IMainErpDbConnectionFactory
        {
            public SqlConnection CreateOpenConnection()
            {
                var setting = ConfigurationManager.ConnectionStrings["KishnyCashConnection"];
                if (setting == null || string.IsNullOrWhiteSpace(setting.ConnectionString))
                {
                    throw new ConfigurationErrorsException("Missing connection string: KishnyCashConnection");
                }

                var connection = new SqlConnection(setting.ConnectionString);
                connection.Open();
                return connection;
            }
        }
    }
}
