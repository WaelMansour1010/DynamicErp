using System.Web.Mvc;

namespace MyERP.Areas.MainErp.Controllers
{
    public class DashboardController : MainErpControllerBase
    {
        public ActionResult Index()
        {
            return View();
        }
    }
}
