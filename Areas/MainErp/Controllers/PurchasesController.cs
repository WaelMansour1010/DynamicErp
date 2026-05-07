using System.Web.Mvc;

namespace MyERP.Areas.MainErp.Controllers
{
    public class PurchasesController : MainErpControllerBase
    {
        public ActionResult Index()
        {
            ViewBag.ActiveScreen = "purchases";
            return View();
        }
    }
}
