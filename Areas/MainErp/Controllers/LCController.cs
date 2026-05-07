using System.Web.Mvc;
using MyERP.Areas.MainErp.Infrastructure;
using MyERP.Areas.MainErp.Repositories.LC;
using MyERP.Areas.MainErp.ViewModels.LC;

namespace MyERP.Areas.MainErp.Controllers
{
    public class LCController : MainErpControllerBase
    {
        private readonly LcReadRepository _repository;

        public LCController()
            : this(new LcReadRepository(new MainErpDbConnectionFactory()))
        {
        }

        public LCController(LcReadRepository repository)
        {
            _repository = repository;
        }

        public ActionResult Index(string searchText, int? bankId, int? vendorId, int? branchId, int page = 1)
        {
            const int pageSize = 20;
            var data = _repository.Search(searchText, bankId, vendorId, branchId, page, pageSize);
            var model = new LCIndexViewModel
            {
                Title = "Letters of Credit",
                ArabicTitle = "Letters of Credit",
                AnalysisStatus = "Read-only migration validation. No save or posting actions are enabled.",
                SearchText = searchText,
                BankId = bankId,
                VendorId = vendorId,
                BranchId = branchId,
                Page = page,
                PageSize = pageSize,
                TotalCount = data.TotalCount,
                Warning = data.Warning
            };

            foreach (var item in data.Items)
            {
                model.Items.Add(item);
            }

            return View(model);
        }

        public ActionResult Details(int id)
        {
            return View(_repository.GetDetails(id));
        }
    }
}
