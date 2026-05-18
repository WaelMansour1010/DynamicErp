using System.Web.Mvc;
using MyERP.Areas.MainErp.Infrastructure;
using MyERP.Areas.MainErp.Infrastructure.Localization;
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
            MainErpCultureManager.ApplyCurrentCulture();

            var isPosNavigation = MainErpPosSessionBridge.IsExplicitPosNavigation(filterContext.HttpContext.Request);
            if (isPosNavigation)
            {
                var bridgeResult = MainErpPosSessionBridge.TryRestore(filterContext.HttpContext.Request, filterContext.HttpContext.Session);
                if (!bridgeResult.Success)
                {
                    if (bridgeResult.PosAuthenticated)
                    {
                        filterContext.Result = new HttpStatusCodeResult(403, bridgeResult.ErrorMessage);
                        return;
                    }

                    if (MainErpUserContext == null)
                    {
                        var returnUrl = filterContext.HttpContext.Request.RawUrl;
                        filterContext.Result = RedirectToAction("Index", "Login", new { area = "MainErp", returnUrl });
                        return;
                    }
                }
            }

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
