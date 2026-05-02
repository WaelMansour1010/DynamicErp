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
    public class HrDepartmentController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        // GET: HrDepartment
        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة الإدارات",
                EnAction = "Index",
                ControllerName = "HrDepartment",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });

            Notification.GetNotification("HrDepartment", "View", "Index", null, null, "الإدارات");

            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;
            if (pageIndex > 1)
            {
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;
            }

            IQueryable<HrDepartment> hrDepartments;
            if (string.IsNullOrEmpty(searchWord))
            {
                hrDepartments = db.HrDepartments.Where(a => a.IsDeleted == false).OrderBy(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.HrDepartments.Where(a => a.IsDeleted == false).Count();
            }
            else
            {
                hrDepartments = db.HrDepartments.Where(a => a.IsDeleted == false &&
                (a.ArName.Contains(searchWord) || a.EnName.Contains(searchWord) || a.Code.Contains(searchWord)))
                    .OrderBy(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.HrDepartments.Where(a => a.IsDeleted == false && (a.ArName.Contains(searchWord) || a.EnName.Contains(searchWord) || a.Code.Contains(searchWord))).Count();
            }
            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;

            return View(hrDepartments.ToList());
        }

        public ActionResult AddEdit(int? id)
        {
            if (id == null)
            {
                var Code = SetCodeNum();
                ViewBag.Code = int.Parse(Code.Data.ToString());

                // MainDepartment
                ViewBag.MainDepartmentId = new SelectList(db.HrDepartments.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
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
                //  Department
                ViewBag.DepartmentId = new SelectList(db.Departments.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                return View();
            }
            HrDepartment hrDepartment = db.HrDepartments.Find(id);
            if (hrDepartment == null)
            {
                return HttpNotFound();
            }
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "اضافة او تعديل الإدارات ",
                EnAction = "AddEdit",
                ControllerName = "HrDepartment",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = id
            });

            ViewBag.Next = QueryHelper.Next((int)id, "HrDepartment");
            ViewBag.Previous = QueryHelper.Previous((int)id, "HrDepartment");
            ViewBag.Last = QueryHelper.GetLast("HrDepartment");
            ViewBag.First = QueryHelper.GetFirst("HrDepartment");

            // MainDepartment
            ViewBag.MainDepartmentId = new SelectList(db.HrDepartments.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", hrDepartment.MainDepartmentId);
            //  Employee 
            ViewBag.EmployeeId = new SelectList(db.Employees.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", hrDepartment.EmployeeId);
            //  Department
            ViewBag.DepartmentId = new SelectList(db.Departments.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", hrDepartment.DepartmentId);

            return View(hrDepartment);
        }

        [HttpPost]
        public ActionResult AddEdit(HrDepartment hrDepartment, string newBtn)
        {
            if (ModelState.IsValid)
            {
                var id = hrDepartment.Id;
                hrDepartment.IsDeleted = false;
                if (hrDepartment.Id > 0)
                {
                    hrDepartment.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    db.Entry(hrDepartment).State = EntityState.Modified;
                    Notification.GetNotification("HrDepartment", "Edit", "AddEdit", hrDepartment.Id, null, "الإدارات");
                }
                else
                {
                    hrDepartment.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    hrDepartment.Code = (QueryHelper.CodeLastNum("HrDepartment") + 1).ToString();
                    hrDepartment.IsActive = true;
                    db.HrDepartments.Add(hrDepartment);

                    Notification.GetNotification("HrDepartment", "Add", "AddEdit", hrDepartment.Id, null, "الإدارات");
                }

                try
                {
                    db.SaveChanges();
                }
                catch (Exception ex)
                {
                    var mod = ModelState.First(c => c.Key == "Code");  // this
                    mod.Value.Errors.Add("هذا الكود موجود من قبل");
                    return View(hrDepartment);
                }
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = id > 0 ? "تعديل إدارة" : "اضافةإدارة",
                    EnAction = "AddEdit",
                    ControllerName = "HrDepartment",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    CodeOrDocNo = hrDepartment.Code
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
            // MainDepartment
            ViewBag.MainDepartmentId = new SelectList(db.HrDepartments.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", hrDepartment.MainDepartmentId);
            //  Employee 
            ViewBag.EmployeeId = new SelectList(db.Employees.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", hrDepartment.EmployeeId);
            //  Department
            ViewBag.DepartmentId = new SelectList(db.Departments.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", hrDepartment.DepartmentId);
            return View(hrDepartment);
        }

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                HrDepartment hrDepartment = db.HrDepartments.Find(id);
                hrDepartment.IsDeleted = true;
                hrDepartment.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                db.Entry(hrDepartment).State = EntityState.Modified;

                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "حذف إدارة",
                    EnAction = "AddEdit",
                    ControllerName = "HrDepartment",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    EnItemName = hrDepartment.EnName

                });
                Notification.GetNotification("HrDepartment", "Delete", "Delete", id, null, "الإدارات");


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
                HrDepartment hrDepartment = db.HrDepartments.Find(id);
                if (hrDepartment.IsActive == true)
                {
                    hrDepartment.IsActive = false;
                }
                else
                {
                    hrDepartment.IsActive = true;
                }
                hrDepartment.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                db.Entry(hrDepartment).State = EntityState.Modified;

                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = (bool)hrDepartment.IsActive ? "تنشيط الإدارات" : "إلغاء تنشيط الإدارات",
                    EnAction = "AddEdit",
                    ControllerName = "HrDepartment",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = hrDepartment.Id,
                    EnItemName = hrDepartment.EnName,
                    ArItemName = hrDepartment.ArName,
                    CodeOrDocNo = hrDepartment.Code
                });
                if (hrDepartment.IsActive == true)
                {
                    Notification.GetNotification("HrDepartment", "Activate/Deactivate", "ActivateDeactivate", id, true, "الإدارات");
                }
                else
                {

                    Notification.GetNotification("HrDepartment", "Activate/Deactivate", "ActivateDeactivate", id, false, "الإدارات");
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
            var code = QueryHelper.CodeLastNum("HrDepartment");
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