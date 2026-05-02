using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;
using MyERP.Models;
using MyERP.Repository;

namespace MyERP.Controllers.ProjectsManagement
{
    public class ProjectController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();
        // GET: Project
        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة المشروع",
                EnAction = "Index",
                ControllerName = "Project",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            Notification.GetNotification("Project", "View", "Index", null, null, "المشروع");
            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;
            if (pageIndex > 1)
            {
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;
            }
            IQueryable<Project> projects;
            if (string.IsNullOrEmpty(searchWord))
            {
                projects = db.Projects.Where(a => a.IsDeleted == false && a.IsActive == true).OrderBy(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.Projects.Where(a => a.IsDeleted == false && a.IsActive == true).Count();
            }
            else
            {
                projects = db.Projects.Where(a => a.IsDeleted == false && a.IsActive == true &&
                (a.ArName.Contains(searchWord) || a.EnName.Contains(searchWord) || a.Code.Contains(searchWord)))
                    .OrderBy(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.Projects.Where(a => a.IsDeleted == false && a.IsActive == true && (a.ArName.Contains(searchWord) || a.EnName.Contains(searchWord) || a.Code.Contains(searchWord))).Count();
            }
            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            return View(projects.ToList());
        }

        public ActionResult AddEdit(int? id)
        {
            SystemSetting systemSetting = db.SystemSettings.Any() ? db.SystemSettings.FirstOrDefault() : new SystemSetting();
            DepartmentRepository departmentRepository = new DepartmentRepository(db);
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            ViewBag.ItemUnitId = new SelectList(db.ItemUnits.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName");
           
            if (id == null)
            {
                //var Code = SetCodeNum();
                //ViewBag.Code = int.Parse(Code.Data.ToString());
                ViewBag.DepartmentId = new SelectList(departmentRepository.UserDepartments(userId), "Id", "ArName", systemSetting.DefaultDepartmentId);
                ViewBag.ProjectStatusId = new SelectList(new List<dynamic> {
                    new { Id=1, ArName="جديد"},
                    new { Id=2, ArName="إفتتاحي"}}, "Id", "ArName");
                ViewBag.CurrencyId = new SelectList(db.Currencies.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");

                ViewBag.ContractTypeId = new SelectList(new List<dynamic> { new { Id=1, ArName="إبتدائي"},
            new { Id=2, ArName="ثانوي"}}, "Id", "ArName");
                ViewBag.EndCustomerId = new SelectList(db.Customers.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                ViewBag.SubCustomerId = new SelectList(db.Customers.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                ViewBag.CustomerRepId = new SelectList(db.Employees.Where(a => a.IsActive == true).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                ViewBag.HrDepartmentId = new SelectList(db.HrDepartments.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                ViewBag.SiteManagerId = new SelectList(db.Employees.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                ViewBag.SecretarialId = new SelectList(new List<dynamic> { new { Id=1, ArName="أمانة1"},
            new { Id=2, ArName="أمانة2"}}, "Id", "ArName");
                ViewBag.MunicipalityId = new SelectList(new List<dynamic> { new { Id=1, ArName="بلدية1"},
            new { Id=2, ArName="بلدية2"}}, "Id", "ArName");
                ViewBag.ExecutiveChargeId = new SelectList(new List<dynamic> { new { Id=1, ArName="الشركة"},
            new { Id=2, ArName="مقاول الباطن"}}, "Id", "ArName");
                ViewBag.GuaranteeBankId = new SelectList(db.Banks.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");

                DateTime utcNow = DateTime.UtcNow;
                TimeZoneInfo info = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");
                DateTime cTime = TimeZoneInfo.ConvertTimeFromUtc(utcNow, info);
                ViewBag.StartDate = cTime.ToString("yyyy-MM-ddTHH:mm");
                ViewBag.EndDate = cTime.ToString("yyyy-MM-ddTHH:mm");
                ViewBag.GuaranteeStartDate = cTime.ToString("yyyy-MM-ddTHH:mm");
                ViewBag.GuaranteeEndDate = cTime.ToString("yyyy-MM-ddTHH:mm");
                ViewBag.NearestEnd = cTime.ToString("yyyy-MM-ddTHH:mm");

                return View();
            }
            Project project = db.Projects.Find(id);
            if (project == null)
            {
                return HttpNotFound();
            }
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "اضافة او تعديل المشروع ",
                EnAction = "AddEdit",
                ControllerName = "Project",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = id
            });

            ViewBag.DepartmentId = new SelectList(departmentRepository.UserDepartments(userId), "Id", "ArName", project.DepartmentId);

            ViewBag.ProjectStatusId = new SelectList(new List<dynamic> { new { Id=1, ArName="جديد"},
            new { Id=2, ArName="إفتتاحي"}}, "Id", "ArName", project.ProjectStatusId);
            ViewBag.CurrencyId = new SelectList(db.Currencies.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", project.CurrencyId);

            ViewBag.ContractTypeId = new SelectList(new List<dynamic> { new { Id=1, ArName="إبتدائي"},
            new { Id=2, ArName="ثانوي"}}, "Id", "ArName", project.ContractTypeId);

            ViewBag.EndCustomerId = new SelectList(db.Customers.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", project.EndCustomerId);
            ViewBag.SubCustomerId = new SelectList(db.Customers.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", project.SubCustomerId);
            ViewBag.CustomerRepId = new SelectList(db.Employees.Where(a => a.IsActive == true ).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", project.CustomerRepId);
            ViewBag.HrDepartmentId = new SelectList(db.HrDepartments.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", project.HrDepartmentId);
            ViewBag.SiteManagerId = new SelectList(db.Employees.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", project.SiteManagerId);
            ViewBag.SecretarialId = new SelectList(new List<dynamic> { new { Id=1, ArName="أمانة1"},
            new { Id=2, ArName="أمانة2"}}, "Id", "ArName", project.SecretarialId);
            ViewBag.MunicipalityId = new SelectList(new List<dynamic> { new { Id=1, ArName="بلدية1"},
            new { Id=2, ArName="بلدية2"}}, "Id", "ArName", project.MunicipalityId);
            ViewBag.ExecutiveChargeId = new SelectList(new List<dynamic> { new { Id=1, ArName="الشركة"},
            new { Id=2, ArName="مقاول الباطن"}}, "Id", "ArName", project.ExecutiveChargeId);
            ViewBag.GuaranteeBankId = new SelectList(db.Banks.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", project.GuaranteeBankId);
           

            ViewBag.StartDate = project.StartDate!=null? project.StartDate.Value.ToString("yyyy-MM-ddTHH:mm"):null;
            ViewBag.EndDate = project.EndDate!=null? project.EndDate.Value.ToString("yyyy-MM-ddTHH:mm"): null;
            ViewBag.GuaranteeStartDate = project.GuaranteeStartDate != null? project.GuaranteeStartDate.Value.ToString("yyyy-MM-ddTHH:mm"): null;
            ViewBag.GuaranteeEndDate = project.GuaranteeEndDate != null? project.GuaranteeEndDate.Value.ToString("yyyy-MM-ddTHH:mm"): null;
            ViewBag.NearestEnd = project.NearestEnd != null ? project.NearestEnd.Value.ToString("yyyy-MM-ddTHH:mm") : null;


            ViewBag.Next = QueryHelper.Next((int)id, "Project");
            ViewBag.Previous = QueryHelper.Previous((int)id, "Project");
            ViewBag.Last = QueryHelper.GetLast("Project");
            ViewBag.First = QueryHelper.GetFirst("Project");
            return View(project);
        }
        [HttpPost]
        public ActionResult AddEdit(Project project)
        {
            if (ModelState.IsValid)
            {
                var id = project.Id;
                project.IsDeleted = false;
                if (project.Id > 0)
                {
                    project.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    db.ProjectDetails.RemoveRange(db.ProjectDetails.Where(x => x.MainDocId == project.Id));
                    var projectDetails = project.ProjectDetails.ToList();
                    projectDetails.ForEach((x) => x.MainDocId = project.Id);
                    project.ProjectDetails = null;

                    db.ProjectItemOperations.RemoveRange(db.ProjectItemOperations.Where(x => x.ProjectId == project.Id));
                    var projectItemOperations = project.ProjectItemOperations.ToList();
                    projectItemOperations.ForEach((x) => x.ProjectId = project.Id);
                    project.ProjectItemOperations = null;
                    db.Entry(project).State = EntityState.Modified;
                    db.ProjectDetails.AddRange(projectDetails);
                    db.ProjectItemOperations.AddRange(projectItemOperations);

                    Notification.GetNotification("Project", "Edit", "AddEdit", project.Id, null, "المشروع");
                }
                else
                {
                    project.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    project.Code = (QueryHelper.CodeLastNum("Project") + 1).ToString();
                    project.IsActive = true;
                    db.Projects.Add(project);
                    Notification.GetNotification("Project", "Add", "AddEdit", project.Id, null, "المشروع");
                }
                try
                {
                    db.SaveChanges();
                }
                catch (Exception ex)
                {
                    var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .Select(x => new { x.Key, x.Value.Errors })
                    .ToArray();

                    return View(project);
                }
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = id > 0 ? "تعديل المشروع" : "اضافة المشروع",
                    EnAction = "AddEdit",
                    ControllerName = "Project",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    CodeOrDocNo = project.Code
                });
                return Json(new { success = true });

            }
            return Json(new { success = false });
        }

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                Project project = db.Projects.Find(id);
                project.IsDeleted = true;
                project.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                foreach(var item in project.ProjectDetails)
                {
                    item.IsDeleted = true;
                }
                db.Entry(project).State = EntityState.Modified;
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "حذف المشروع",
                    EnAction = "AddEdit",
                    ControllerName = "Project",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    EnItemName = project.EnName
                });
                Notification.GetNotification("Project", "Delete", "Delete", id, null, "المشروع");
                return Content("true");
            }
            catch (Exception ex)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
        }

        [HttpPost]
        public ActionResult ActivateDeactivate(int id)
        {
            try
            {
                Project project = db.Projects.Find(id);
                if (project.IsActive == true)
                {
                    project.IsActive = false;
                }
                else
                {
                    project.IsActive = true;
                }
                project.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                db.Entry(project).State = EntityState.Modified;
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = (bool)project.IsActive ? "تنشيط المشروع" : "إلغاء تنشيط المشروع",
                    EnAction = "AddEdit",
                    ControllerName = "Project",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = project.Id,
                    EnItemName = project.EnName,
                    ArItemName = project.ArName,
                    CodeOrDocNo = project.Code
                });
                if (project.IsActive == true)
                {
                    Notification.GetNotification("Project", "Activate/Deactivate", "ActivateDeactivate", id, true, "المشروع");
                }
                else
                {
                    Notification.GetNotification("Project", "Activate/Deactivate", "ActivateDeactivate", id, false, " المشروع");
                }
                return Content("true");
            }
            catch (Exception ex)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
        }

        [SkipERPAuthorize]
        public JsonResult SetCodeNum()
        {
            var code = QueryHelper.CodeLastNum("Project");
            return Json(code + 1, JsonRequestBehavior.AllowGet);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}