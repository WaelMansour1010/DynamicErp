using System;
using System.Web.Mvc;
using MyERP.Areas.MainErp.Infrastructure;
using MyERP.Areas.MainErp.Repositories.JournalEntries;
using MyERP.Areas.MainErp.ViewModels.JournalEntries;

namespace MyERP.Areas.MainErp.Controllers
{
    public class JournalEntriesController : MainErpControllerBase
    {
        private readonly JournalEntryReadRepository _repository;

        public JournalEntriesController()
            : this(new JournalEntryReadRepository(new MainErpDbConnectionFactory()))
        {
        }

        public JournalEntriesController(JournalEntryReadRepository repository)
        {
            _repository = repository;
        }

        public ActionResult Index(string searchText, int? branchId, DateTime? fromDate, DateTime? toDate, int page = 1)
        {
            const int pageSize = 25;
            var data = _repository.Search(searchText, branchId, fromDate, toDate, page, pageSize);
            var model = new JournalEntriesIndexViewModel
            {
                SearchText = searchText,
                BranchId = branchId,
                FromDate = fromDate,
                ToDate = toDate,
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
    }
}
