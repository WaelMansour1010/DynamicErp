using System.Web.Mvc;
using MyERP.Areas.MainErp.Infrastructure;
using MyERP.Areas.MainErp.Repositories.Accounting;
using MyERP.Areas.MainErp.Services.Accounting;

namespace MyERP.Areas.MainErp.Controllers
{
    public class AccountingController : MainErpControllerBase
    {
        public ActionResult PreviewTest()
        {
            ViewBag.ActiveScreen = "voucher-preview";
            var factory = new MainErpDbConnectionFactory();
            var manualIds = new ManualIdGenerator(factory);
            var voucherService = new VoucherPostingService(manualIds, new VoucherRepository());
            var previewService = new PostingPreviewService(voucherService);
            return View(previewService.BuildDemoPreview());
        }
    }
}
