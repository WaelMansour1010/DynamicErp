using System.Web.Mvc;
using MyERP.Areas.MainErp.Infrastructure;
using MyERP.Areas.MainErp.Repositories.ProjectExtracts;
using MyERP.Areas.MainErp.Repositories.Projects;
using MyERP.Areas.MainErp.ViewModels.ProjectExtracts;
using MyERP.Areas.MainErp.ViewModels.Projects;

namespace MyERP.Areas.MainErp.Controllers
{
    public class ProjectExtractsController : MainErpControllerBase
    {
        private readonly ProjectExtractReadRepository _repository;
        private readonly ProjectRepository _projectRepository;

        public ProjectExtractsController()
            : this(new ProjectExtractReadRepository(new MainErpDbConnectionFactory()), new ProjectRepository(new MainErpDbConnectionFactory()))
        {
        }

        public ProjectExtractsController(ProjectExtractReadRepository repository, ProjectRepository projectRepository)
        {
            _repository = repository;
            _projectRepository = projectRepository;
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

        public ActionResult Create(int projectId)
        {
            ViewBag.ActiveScreen = "project-extracts";
            var model = _projectRepository.BuildExtractCreateModel(projectId);
            if (model == null)
            {
                TempData["Projects.Success"] = "المشروع المطلوب غير موجود.";
                return RedirectToAction("Index", "Projects", new { area = "MainErp" });
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(ProjectExtractCreateViewModel model)
        {
            ViewBag.ActiveScreen = "project-extracts";
            if (model.Total.GetValueOrDefault() < 0 || model.VatValue.GetValueOrDefault() < 0 || model.NetValue.GetValueOrDefault() < 0)
            {
                ModelState.AddModelError("", "لا يمكن حفظ قيم سالبة في المستخلص.");
            }

            if (!ModelState.IsValid)
            {
                var rebuilt = _projectRepository.BuildExtractCreateModel(model.ProjectId.GetValueOrDefault());
                if (rebuilt != null)
                {
                    model.ProjectName = rebuilt.ProjectName;
                    model.ProjectFullCode = rebuilt.ProjectFullCode;
                    model.BranchNo = rebuilt.BranchNo;
                }

                return View(model);
            }

            try
            {
                var userId = MainErpUserContext == null ? (int?)null : MainErpUserContext.UserId;
                var id = _projectRepository.CreateExtract(model, userId);
                return RedirectToAction("Details", new { id });
            }
            catch (System.Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
                var rebuilt = _projectRepository.BuildExtractCreateModel(model.ProjectId.GetValueOrDefault());
                if (rebuilt != null)
                {
                    model.ProjectName = rebuilt.ProjectName;
                    model.ProjectFullCode = rebuilt.ProjectFullCode;
                    model.BranchNo = rebuilt.BranchNo;
                }

                return View(model);
            }
        }

        public ActionResult Report(int id)
        {
            ViewBag.ActiveScreen = "project-extracts";
            return View(_repository.GetDetails(id));
        }
    }
}
