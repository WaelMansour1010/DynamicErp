using MyERP.Areas.MainErp.Models.Security;
using MyERP.Areas.MainErp.Infrastructure;
using MyERP.Areas.MainErp.Security;
using MyERP.Areas.MainErp.Services.Security;
using MyERP.Areas.MainErp.ViewModels.Security;
using System.Web.Mvc;

namespace MyERP.Areas.MainErp.Controllers
{
    [AllowAnonymous]
    [SkipERPAuthorize]
    public class LoginController : Controller
    {
        private readonly MainErpLoginService _loginService;

        public LoginController()
            : this(new MainErpLoginService())
        {
        }

        public LoginController(MainErpLoginService loginService)
        {
            _loginService = loginService;
        }

        [HttpGet]
        public ActionResult Index(string returnUrl)
        {
            if (GetContext() != null && IsSafeLocalReturnUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return View(BuildModel(returnUrl, null));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Index(MainErpLoginViewModel request)
        {
            string errorMessage;
            var context = _loginService.Login(request != null ? request.UserName : null, request != null ? request.Password : null, out errorMessage);
            if (context == null)
            {
                var model = BuildModel(request != null ? request.ReturnUrl : null, errorMessage);
                model.UserName = request != null ? request.UserName : null;
                return View(model);
            }

            StoreContext(context);

            var returnUrl = request != null ? request.ReturnUrl : null;
            if (IsSafeLocalReturnUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index", "Home", new { area = "MainErp" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Logout()
        {
            Session.Remove(MainErpSessionKeys.Context);
            Session.Remove(MainErpSessionKeys.UserId);
            Session.Remove(MainErpSessionKeys.UserName);
            Session.Remove(MainErpSessionKeys.EmpId);
            Session.Remove(MainErpSessionKeys.BranchId);
            Session.Remove(MainErpSessionKeys.StoreId);
            Session.Remove(MainErpSessionKeys.BoxId);
            return RedirectToAction("Index");
        }

        private MainErpLoginViewModel BuildModel(string returnUrl, string errorMessage)
        {
            return new MainErpLoginViewModel
            {
                ReturnUrl = returnUrl,
                ErrorMessage = errorMessage,
                CurrentDatabaseName = MainErpDebugDatabaseOverride.GetDisplayDatabaseName(),
                IsDebugDatabaseOverrideEnabled = MainErpDebugDatabaseOverride.IsEnabled()
            };
        }

        private void StoreContext(MainErpUserContext context)
        {
            Session[MainErpSessionKeys.Context] = context;
            Session[MainErpSessionKeys.UserId] = context.UserId;
            Session[MainErpSessionKeys.UserName] = context.UserName;
            Session[MainErpSessionKeys.EmpId] = context.EmpId;
            Session[MainErpSessionKeys.BranchId] = context.BranchId;
            Session[MainErpSessionKeys.StoreId] = context.StoreId;
            Session[MainErpSessionKeys.BoxId] = context.BoxId;
        }

        private MainErpUserContext GetContext()
        {
            return Session[MainErpSessionKeys.Context] as MainErpUserContext;
        }

        private bool IsSafeLocalReturnUrl(string returnUrl)
        {
            return !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl);
        }
    }
}
