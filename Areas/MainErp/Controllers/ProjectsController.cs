using System;
using System.Linq;
using System.Web.Mvc;
using MyERP.Areas.MainErp.Infrastructure;
using MyERP.Areas.MainErp.Repositories.Projects;
using MyERP.Areas.MainErp.ViewModels.Projects;


namespace MyERP.Areas.MainErp.Controllers
{
    public class ProjectsController : MainErpControllerBase
    {
        private readonly ProjectRepository _repository;

        public ProjectsController()
            : this(new ProjectRepository(new MainErpDbConnectionFactory()))
        {
        }

        public ProjectsController(ProjectRepository repository)
        {
            _repository = repository;
        }

        public ActionResult Index(string searchText, int? statusId, int? branchId, int page = 1)
        {
            ViewBag.ActiveScreen = "projects";
            const int pageSize = 20;
            var data = _repository.Search(searchText, statusId, branchId, page, pageSize);
            var model = new ProjectsIndexViewModel
            {
                SearchText = searchText,
                StatusId = statusId,
                BranchId = branchId,
                Page = page < 1 ? 1 : page,
                PageSize = pageSize,
                TotalCount = data.TotalCount,
                Warning = data.Warning,
                SuccessMessage = TempData["Projects.Success"] as string
            };

            foreach (var item in data.Items)
            {
                model.Items.Add(item);
            }

            _repository.PopulateIndexLookups(model);
            return View(model);
        }

        public ActionResult New()
        {
            ViewBag.ActiveScreen = "projects";
            return View("Edit", _repository.New());
        }

        public ActionResult Edit(int id)
        {
            ViewBag.ActiveScreen = "projects";
            var model = _repository.Get(id);
            if (model == null)
            {
                TempData["Projects.Success"] = "المشروع المطلوب غير موجود.";
                return RedirectToAction("Index");
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Save(ProjectEditViewModel model)
        {
            ViewBag.ActiveScreen = "projects";
            ValidateBusinessRules(model);
            if (!ModelState.IsValid)
            {
                _repository.PopulateLookups(model);
                return View("Edit", model);
            }

            try
            {
                var id = _repository.Save(model);
                model.IsNewProject = false;
                TempData["Projects.Success"] = "تم حفظ المشروع بنجاح.";
                return RedirectToAction("Edit", new { id });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
                _repository.PopulateLookups(model);
                return View("Edit", model);
            }
        }

        [HttpPost]
        public ActionResult SaveJson(ProjectEditViewModel model)
        {
            try
            {
                ValidateBusinessRules(model);
                if (!ModelState.IsValid)
                {
                    var errors = string.Join(" | ", ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage));
                    return Json(new { success = false, message = errors });
                }

                var id = _repository.Save(model);
                return Json(new { success = true, id });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        private void ValidateBusinessRules(ProjectEditViewModel model)
        {
            if (model.StartDate.HasValue && model.EndDate.HasValue && model.EndDate.Value.Date < model.StartDate.Value.Date)
            {
                ModelState.AddModelError("EndDate", "تاريخ الانتهاء يجب أن يكون بعد تاريخ البداية.");
            }

            if (model.GeneralDiscount.GetValueOrDefault() > model.ProjectCost.GetValueOrDefault())
            {
                ModelState.AddModelError("GeneralDiscount", "قيمة الخصم لا يمكن أن تتجاوز قيمة المشروع.");
            }
        }

    }
}
