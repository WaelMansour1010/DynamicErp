using System.Web.Mvc;
using MyERP.Areas.MainErp.Infrastructure.Localization;

namespace MyERP.Areas.MainErp.Controllers
{
    [AllowAnonymous]
    [SkipERPAuthorize]
    public class LocalizationController : Controller
    {
        public ActionResult Set(string culture, string returnUrl)
        {
            MainErpCultureManager.SetCulture(culture);
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index", "Dashboard", new { area = "MainErp" });
        }
    }
}
