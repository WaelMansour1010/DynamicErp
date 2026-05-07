using System.Web.Mvc;

namespace MyERP.Areas.MainErp.Controllers
{
    public class StockTransfersController : MainErpControllerBase
    {
        public ActionResult Index()
        {
            ViewBag.ActiveScreen = "stock-transfers";
            return View();
        }
    }
}
