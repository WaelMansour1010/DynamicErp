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
    public class EmployeeTypeController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        // GET: EmployeeType
        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة أنواع الموظفين",
                EnAction = "Index",
                ControllerName = "EmployeeType",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });

            Notification.GetNotification("EmployeeType", "View", "Index", null, null, "أنواع الموظفين");

            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;
            if (pageIndex > 1)
            {
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;
            }

            IQueryable<EmployeeType> employeeTypes;
            if (string.IsNullOrEmpty(searchWord))
            {
                employeeTypes = db.EmployeeTypes.Where(a => a.IsDeleted == false).OrderBy(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.EmployeeTypes.Where(a => a.IsDeleted == false).Count();
            }
            else
            {
                employeeTypes = db.EmployeeTypes.Where(a => a.IsDeleted == false &&
                (a.ArName.Contains(searchWord) || a.EnName.Contains(searchWord) || a.Code.Contains(searchWord)))
                    .OrderBy(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.EmployeeTypes.Where(a => a.IsDeleted == false && (a.ArName.Contains(searchWord) || a.EnName.Contains(searchWord) || a.Code.Contains(searchWord))).Count();
            }
            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;

            return View(employeeTypes.ToList());
        }

        public ActionResult AddEdit(int? id)
        {
            if (id == null)
            {
                var Code = SetCodeNum();
                ViewBag.Code = int.Parse(Code.Data.ToString());
                return View();
            }
            EmployeeType employeeType = db.EmployeeTypes.Find(id);
            if (employeeType == null)
            {
                return HttpNotFound();
            }
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "اضافة او تعديل أنواع الموظفين ",
                EnAction = "AddEdit",
                ControllerName = "EmployeeType",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = id
            });

            ViewBag.Next = QueryHelper.Next((int)id, "EmployeeType");
            ViewBag.Previous = QueryHelper.Previous((int)id, "EmployeeType");
            ViewBag.Last = QueryHelper.GetLast("EmployeeType");
            ViewBag.First = QueryHelper.GetFirst("EmployeeType");
            return View(employeeType);
        }

        [HttpPost]
        public ActionResult AddEdit(EmployeeType employeeType, string newBtn)
        {
            if (ModelState.IsValid)
            {
                var id = employeeType.Id;
                employeeType.IsDeleted = false;
                if (employeeType.Id > 0)
                {
                    employeeType.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    db.Entry(employeeType).State = EntityState.Modified;
                    Notification.GetNotification("EmployeeType", "Edit", "AddEdit", employeeType.Id, null, "أنواع الموظفين");
                }
                else
                {
                    employeeType.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    employeeType.Code = (QueryHelper.CodeLastNum("EmployeeType") + 1).ToString();
                    employeeType.IsActive = true;
                    db.EmployeeTypes.Add(employeeType);

                    Notification.GetNotification("EmployeeType", "Add", "AddEdit", employeeType.Id, null, "أنواع الموظفين");
                }

                try
                {
                    db.SaveChanges();
                }
                catch (Exception ex)
                {
                    var mod = ModelState.First(c => c.Key == "Code");  // this
                    mod.Value.Errors.Add("هذا الكود موجود من قبل");
                    return View(employeeType);
                }
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = id > 0 ? "تعديل نوع موظف" : "اضافة نوع موظف",
                    EnAction = "AddEdit",
                    ControllerName = "EmployeeType",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    CodeOrDocNo = employeeType.Code
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

            return View(employeeType);
        }

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                EmployeeType employeeType = db.EmployeeTypes.Find(id);
                employeeType.IsDeleted = true;
                employeeType.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                db.Entry(employeeType).State = EntityState.Modified;

                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "حذف نوع موظف",
                    EnAction = "AddEdit",
                    ControllerName = "EmployeeType",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    EnItemName = employeeType.EnName

                });
                Notification.GetNotification("EmployeeType", "Delete", "Delete", id, null, "أنواع الموظفين");


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
                EmployeeType employeeType = db.EmployeeTypes.Find(id);
                if (employeeType.IsActive == true)
                {
                    employeeType.IsActive = false;
                }
                else
                {
                    employeeType.IsActive = true;
                }
                employeeType.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                db.Entry(employeeType).State = EntityState.Modified;

                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = (bool)employeeType.IsActive ? "تنشيط أنواع الموظفين" : "إلغاء تنشيط أنواع الموظفين",
                    EnAction = "AddEdit",
                    ControllerName = "EmployeeType",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = employeeType.Id,
                    EnItemName = employeeType.EnName,
                    ArItemName = employeeType.ArName,
                    CodeOrDocNo = employeeType.Code
                });
                if (employeeType.IsActive == true)
                {
                    Notification.GetNotification("EmployeeType", "Activate/Deactivate", "ActivateDeactivate", id, true, "أنواع الموظفين");
                }
                else
                {

                    Notification.GetNotification("EmployeeType", "Activate/Deactivate", "ActivateDeactivate", id, false, " أنواع الموظفين");
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
            var code = QueryHelper.CodeLastNum("EmployeeType");
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