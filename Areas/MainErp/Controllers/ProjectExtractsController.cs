using System.Web.Mvc;
using MyERP.Areas.MainErp.Infrastructure;
using MyERP.Areas.MainErp.Repositories.ProjectExtracts;
using MyERP.Areas.MainErp.ViewModels.ProjectExtracts;

namespace MyERP.Areas.MainErp.Controllers
{
    public class ProjectExtractsController : MainErpControllerBase
    {
        private readonly ProjectExtractReadRepository _repository;

        public ProjectExtractsController()
            : this(new ProjectExtractReadRepository(new MainErpDbConnectionFactory()))
        {
        }

        public ProjectExtractsController(ProjectExtractReadRepository repository)
        {
            _repository = repository;
        }

        public ActionResult Index(string searchText, int? projectId, int? branchId, int page = 1)
        {
            ViewBag.ActiveScreen = "project-extracts";
            const int pageSize = 20;
            var data = _repository.Search(searchText, projectId, branchId, page, pageSize);
            var model = new ProjectExtractsIndexViewModel
            {
                Title = "Project Extracts",
                ArabicTitle = "Project Extracts",
                AnalysisStatus = "Read-only migration validation. No edit, save, or posting actions are enabled.",
                SearchText = searchText,
                ProjectId = projectId,
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
            ViewBag.ActiveScreen = "project-extracts";
            return View(_repository.GetDetails(id));
        }

        public ActionResult Report(int id)
        {
            ViewBag.ActiveScreen = "project-extracts";
            return View(_repository.GetDetails(id));
        }
    }
}
