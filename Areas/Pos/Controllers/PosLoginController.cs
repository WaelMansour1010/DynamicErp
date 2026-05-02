using MyERP.Areas.Pos.Data;
using MyERP.Areas.Pos.Models;
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
            var context = _repository.LoginPosUser(request != null ? request.UserName : null, request != null ? request.Password : null);
            if (context == null)
            {
                ViewBag.ErrorMessage = "اسم المستخدم أو كلمة المرور غير صحيحة";
                return View(request);
            }

            var contextError = ValidateContextDefaults(context);
            if (!string.IsNullOrWhiteSpace(contextError))
            {
                ViewBag.ErrorMessage = contextError;
                return View(request);
            }

            Session[PosContextSessionKey] = context;
            Session["PosUserId"] = context.UserId;
            Session["PosUserName"] = context.UserName;
            Session["PosEmpId"] = context.EmpId;

            return RedirectToAction("Index", "PosTransaction", new { area = "Pos" });
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

        private static string ValidateContextDefaults(PosUserContext context)
        {
            if (context.BranchId.GetValueOrDefault() <= 0)
            {
                return "لا يوجد فرع افتراضي مضبوط لهذا المستخدم";
            }

            if (context.EmpId.GetValueOrDefault() <= 0)
            {
                return "لا يوجد موظف / مندوب مبيعات مضبوط لهذا المستخدم";
            }

            if (context.StoreId.GetValueOrDefault() <= 0)
            {
                return "لا يوجد مخزن افتراضي مضبوط لهذا المستخدم";
            }

            if (context.BoxId.GetValueOrDefault() <= 0)
            {
                return "لا توجد خزنة افتراضية مضبوطة لهذا المستخدم";
            }

            if (context.PaymentTypeId.GetValueOrDefault() <= 0)
            {
                return "لا توجد طريقة دفع نقدي افتراضية مضبوطة لهذا المستخدم";
            }

            return null;
        }
    }
}
