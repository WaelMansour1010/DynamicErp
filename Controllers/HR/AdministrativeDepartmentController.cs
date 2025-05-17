using DevExpress.Data.WcfLinq.Helpers;
using MyERP.Models;
using System;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Web.Mvc;


namespace MyERP.Controllers
{
    public class AdministrativeDepartmentController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        // GET: AdministrativeDepartment
        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة الأقسام الادارية",
                EnAction = "Index",
                ControllerName = "AdministrativeDepartment",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });

            Notification.GetNotification("AdministrativeDepartment", "View", "Index", null, null, "الأقسام الادارية");

            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;
            if (pageIndex > 1)
            {
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;
            }

            IQueryable<AdministrativeDepartment> administrativeDepartments;
            if (string.IsNullOrEmpty(searchWord))
            {
                administrativeDepartments = db.AdministrativeDepartments.Where(a => a.IsDeleted == false).OrderBy(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.AdministrativeDepartments.Where(a => a.IsDeleted == false).Count();
            }
            else
            {
                administrativeDepartments = db.AdministrativeDepartments.Where(a => a.IsDeleted == false &&
                (a.ArName.Contains(searchWord) || a.EnName.Contains(searchWord) || a.Code.Contains(searchWord)))
                    .OrderBy(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.AdministrativeDepartments.Where(a => a.IsDeleted == false && (a.ArName.Contains(searchWord) || a.EnName.Contains(searchWord) || a.Code.Contains(searchWord))).Count();
            }
            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;

            return View(administrativeDepartments.ToList());
        }

        public ActionResult AddEdit(int? id)
        {
            if (id == null)
            {
                var Code = SetCodeNum();
                ViewBag.Code = int.Parse(Code.Data.ToString());

                // HrDepartment
                ViewBag.HrDepartmentId = new SelectList(db.HrDepartments.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                //  Employee 
                ViewBag.EmployeeId = new SelectList(db.Employees.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");

                return View();
            }
            AdministrativeDepartment administrativeDepartment = db.AdministrativeDepartments.Find(id);
            if (administrativeDepartment == null)
            {
                return HttpNotFound();
            }
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "اضافة او تعديل الأقسام الادارية ",
                EnAction = "AddEdit",
                ControllerName = "AdministrativeDepartment",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = id
            });

            ViewBag.Next = QueryHelper.Next((int)id, "AdministrativeDepartment");
            ViewBag.Previous = QueryHelper.Previous((int)id, "AdministrativeDepartment");
            ViewBag.Last = QueryHelper.GetLast("AdministrativeDepartment");
            ViewBag.First = QueryHelper.GetFirst("AdministrativeDepartment");


            // HrDepartment
            ViewBag.HrDepartmentId = new SelectList(db.HrDepartments.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", administrativeDepartment.HrDepartmentId);
            //  Employee 
            ViewBag.EmployeeId = new SelectList(db.Employees.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", administrativeDepartment.EmployeeId);


            return View(administrativeDepartment);
        }

        [HttpPost]
        public ActionResult AddEdit(AdministrativeDepartment administrative, string newBtn)
        {
            if (ModelState.IsValid)
            {
                var id = administrative.Id;
                administrative.IsDeleted = false;
                if (administrative.Id > 0)
                {
                    administrative.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    db.Entry(administrative).State = EntityState.Modified;
                    Notification.GetNotification("AdministrativeDepartment", "Edit", "AddEdit", administrative.Id, null, "الأقسام الادارية");
                }
                else
                {
                    administrative.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    administrative.Code = (QueryHelper.CodeLastNum("AdministrativeDepartment") + 1).ToString();
                    administrative.IsActive = true;
                    db.AdministrativeDepartments.Add(administrative);

                    Notification.GetNotification("AdministrativeDepartment", "Add", "AddEdit", administrative.Id, null, "الأقسام الادارية");
                }

                try
                {
                    db.SaveChanges();
                }
                catch (Exception ex)
                {
                    var mod = ModelState.First(c => c.Key == "Code");  // this
                    mod.Value.Errors.Add("هذا الكود موجود من قبل");
                    return View(administrative);
                }
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = id > 0 ? "تعديل قسم" : "اضافة قسم",
                    EnAction = "AddEdit",
                    ControllerName = "AdministrativeDepartment",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    CodeOrDocNo = administrative.Code
                });
                if (newBtn == "saveAndNew")
                {
                    return RedirectToAction("AddEdit");

                }
                else
                {
                    return RedirectToAction("Index");
                }
            }
            // HrDepartment
            ViewBag.HrDepartmentId = new SelectList(db.HrDepartments.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", administrative.HrDepartmentId);
            //  Employee 
            ViewBag.EmployeeId = new SelectList(db.Employees.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", administrative.EmployeeId);

            return View(administrative);
        }

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                AdministrativeDepartment administrativeDepartment = db.AdministrativeDepartments.Find(id);
                administrativeDepartment.IsDeleted = true;
                administrativeDepartment.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                db.Entry(administrativeDepartment).State = EntityState.Modified;

                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "حذف قسم",
                    EnAction = "AddEdit",
                    ControllerName = "AdministrativeDepartment",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    EnItemName = administrativeDepartment.EnName

                });
                Notification.GetNotification("AdministrativeDepartment", "Delete", "Delete", id, null, "الأقسام الادارية");


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
                AdministrativeDepartment administrative = db.AdministrativeDepartments.Find(id);
                if (administrative.IsActive == true)
                {
                    administrative.IsActive = false;
                }
                else
                {
                    administrative.IsActive = true;
                }
                administrative.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                db.Entry(administrative).State = EntityState.Modified;

                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = (bool)administrative.IsActive ? "تنشيط الأقسام الادارية" : "إلغاء تنشيط الأقسام الادارية",
                    EnAction = "AddEdit",
                    ControllerName = "AdministrativeDepartment",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = administrative.Id,
                    EnItemName = administrative.EnName,
                    ArItemName = administrative.ArName,
                    CodeOrDocNo = administrative.Code
                });
                if (administrative.IsActive == true)
                {
                    Notification.GetNotification("AdministrativeDepartment", "Activate/Deactivate", "ActivateDeactivate", id, true, "الأقسام الادارية");
                }
                else
                {

                    Notification.GetNotification("AdministrativeDepartment", "Activate/Deactivate", "ActivateDeactivate", id, false, "الأقسام الادارية");
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
            var code = QueryHelper.CodeLastNum("AdministrativeDepartment");
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