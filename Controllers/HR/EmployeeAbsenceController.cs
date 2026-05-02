using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Web;
using System.Web.Mvc;
using MyERP.Models;
using System.Security.Claims;

namespace MyERP.Controllers.HR
{
    public class EmployeeAbsenceController : Controller
    {
        private readonly MySoftERPEntity db = new MySoftERPEntity();

        // GET: EmployeeAbsence
        public ActionResult Index(DateTime? date, int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة غياب الموظفين",
                EnAction = "Index",
                ControllerName = "EmployeeAbsence",
                UserName = User.Identity.Name,
                UserId = userId,
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            ////-------------------- Notification-------------------------////
            Notification.GetNotification("EmployeeAbsence", "View", "Index", null, null, "غياب الموظفين");

            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;
            IQueryable<EmployeeAbsence> employeeAbsences;
            if (string.IsNullOrEmpty(searchWord))
            {
                employeeAbsences = db.EmployeeAbsences.Where(c => c.IsDeleted == false&&(date==null||(c.Date.Value.Month==date.Value.Month&&c.Date.Value.Year==date.Value.Year))).OrderByDescending(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.EmployeeAbsences.Where(c => c.IsDeleted == false).Count();
            }
            else
            {
                employeeAbsences = db.EmployeeAbsences.Where(s => s.IsDeleted == false && s.Date.Value.Month == date.Value.Month && s.Date.Value.Year == date.Value.Year && (s.Employee.ArName.Contains(searchWord) || s.Employee.EnName.Contains(searchWord))).OrderByDescending(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.EmployeeAbsences.Where(s => s.IsDeleted == false &&(date==null||(s.Date.Value.Month == date.Value.Month && s.Date.Value.Year == date.Value.Year)) && (s.Employee.ArName.Contains(searchWord) || s.Employee.EnName.Contains(searchWord))).Count();
            }
            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            ViewBag.Date = DateTime.Now.ToString("yyyy-MM-dd");
            return View(employeeAbsences.ToList());
        }

        public ActionResult AddEdit(int? id)
        {
            if (id == null)
            {
                ViewBag.EmployeeId = new SelectList(db.Employees.Where(x => x.IsActive && !x.IsDeleted).Select(x => new { x.Id, x.ArName }), "Id", "ArName");
                ViewBag.Date = DateTime.Now.ToString("yyyy-MM-dd");
                var DocumentNumber =  db.EmployeeAbsences.OrderByDescending(x => x.Id).Select(x => x.DocumentNumber).FirstOrDefault();
                ViewBag.DocumentNumber = string.IsNullOrEmpty(DocumentNumber) ? 1 : int.Parse(DocumentNumber) + 1;
                return View();
            }
            EmployeeAbsence employeeAbsence =  db.EmployeeAbsences.Find(id);
            if (employeeAbsence == null)
            {
                return HttpNotFound();
            }
            ViewBag.EmployeeId = new SelectList(db.Employees.Where(x => x.IsActive && !x.IsDeleted).Select(x => new { x.Id, x.ArName }), "Id", "ArName", employeeAbsence.EmployeeId);
            ViewBag.Date = employeeAbsence.Date.Value.ToString("yyyy-MM-dd");
            ViewBag.DocumentNumber = employeeAbsence.DocumentNumber;

            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح تفاصيل غياب الموظفين",
                EnAction = "AddEdit",
                ControllerName = "EmployeeAbsence",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = employeeAbsence.Id,
                CodeOrDocNo = employeeAbsence.DocumentNumber
            });
            return View(employeeAbsence);
        }

        [HttpPost]
        public ActionResult AddEdit(EmployeeAbsence employeeAbsence)
        {
            if (ModelState.IsValid)
            {
                var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                employeeAbsence.UserId = userId;
                var id = employeeAbsence.Id;
                if (employeeAbsence.Id > 0)
                {
                    db.Entry(employeeAbsence).State = EntityState.Modified;
                }
                else
                {
                    db.EmployeeAbsences.Add(employeeAbsence);
                }
                 db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = id > 0 ? "تعديل غياب الموظفين" : "اضافة غياب الموظفين",
                    EnAction = "AddEdit",
                    ControllerName = "EmployeeAbsence",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = employeeAbsence.Id,
                    CodeOrDocNo = employeeAbsence.DocumentNumber

                });
                Notification.GetNotification("EmployeeAbsence", id > 0 ? "Edit" : "Add", "AddEdit", employeeAbsence.Id, null, "غياب الموظفين");
                return Json(new { success = true });
            }
            var errors = ModelState
                   .Where(x => x.Value.Errors.Count > 0)
                   .Select(x => new { x.Key, x.Value.Errors })
                   .ToArray();
            return Json(new { success = false, errors });
        }



        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            EmployeeAbsence employeeAbsence = new EmployeeAbsence() { Id = id, DocumentNumber = "" };
            db.EmployeeAbsences.Attach(employeeAbsence);
            employeeAbsence.IsDeleted = true;
            db.Entry(employeeAbsence).Property(x => x.IsDeleted).IsModified = true;

             db.SaveChanges();
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "حذف غياب الموظفين",
                EnAction = "AddEdit",
                ControllerName = "EmployeeAbsence",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "POST",
                SelectedItem = id,
                CodeOrDocNo = employeeAbsence.DocumentNumber
            });
            Notification.GetNotification("EmployeeAbsence", "Delete", "Delete", id, null, "غياب الموظفين");
            return Content("true");
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