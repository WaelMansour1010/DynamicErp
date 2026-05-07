using System.Web.Mvc;

namespace MyERP.Areas.MainErp.Controllers
{
    public class HomeController : MainErpControllerBase
    {
        public ActionResult Index()
        {
            return RedirectToAction("Index", "Dashboard", new { area = "MainErp" });
        }
    }
}
