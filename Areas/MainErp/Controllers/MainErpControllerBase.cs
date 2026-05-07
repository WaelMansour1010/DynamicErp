using System.Web.Mvc;
using MyERP.Areas.MainErp.Models.Security;
using MyERP.Areas.MainErp.Security;

namespace MyERP.Areas.MainErp.Controllers
{
    [AllowAnonymous]
    [SkipERPAuthorize]
    public abstract class MainErpControllerBase : Controller
    {
        protected MainErpUserContext MainErpUserContext
        {
            get { return Session[MainErpSessionKeys.Context] as MainErpUserContext; }
        }

        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (MainErpUserContext == null)
            {
                var returnUrl = filterContext.HttpContext.Request.RawUrl;
                filterContext.Result = RedirectToAction("Index", "Login", new { area = "MainErp", returnUrl });
                return;
            }

            base.OnActionExecuting(filterContext);
        }
    }
}
