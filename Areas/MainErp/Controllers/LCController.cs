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

        public ActionResult Index(string searchText, int? bankId, int? vendorId, int? branchId, int? selectedId, int page = 1)
        {
            ViewBag.ActiveScreen = "lc";
            const int pageSize = 20;
            var data = _repository.Search(searchText, bankId, vendorId, branchId, page, pageSize);
            var model = new LCIndexViewModel
            {
                Title = "Letters of Credit",
                ArabicTitle = "الاعتمادات المستندية",
                AnalysisStatus = "Safe Shell migrated from FrmLC.frm. Read/search/view only; save, delete, vouchers and posting are disabled.",
                SearchText = searchText,
                BankId = bankId,
                VendorId = vendorId,
                BranchId = branchId,
                SelectedId = selectedId,
                Page = page,
                PageSize = pageSize,
                TotalCount = data.TotalCount,
                Warning = data.Warning
            };

            foreach (var item in data.Items)
            {
                model.Items.Add(item);
            }

            var detailId = selectedId ?? (model.Items.Count > 0 ? (int?)model.Items[0].TblLCID : null);
            if (detailId.HasValue)
            {
                model.SelectedId = detailId;
                model.SelectedDetails = _repository.GetDetails(detailId.Value);
                if (string.IsNullOrWhiteSpace(model.Warning) && !string.IsNullOrWhiteSpace(model.SelectedDetails.Warning))
                {
                    model.Warning = model.SelectedDetails.Warning;
                }
            }

            return View(model);
        }

        public ActionResult Details(int id)
        {
            ViewBag.ActiveScreen = "lc";
            return View(_repository.GetDetails(id));
        }
    }
}
