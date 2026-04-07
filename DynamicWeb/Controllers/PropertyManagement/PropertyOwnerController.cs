using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using MyERP.Models;
using MyERP.Repository;

namespace MyERP.Controllers.PropertyManagement
{
    public class PropertyOwnerController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        // GET: PropertyOwner
        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة الملاك",
                EnAction = "Index",
                ControllerName = "PropertyOwner",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            Notification.GetNotification("PropertyOwner", "View", "Index", null, null, "الملاك");
            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;
            if (pageIndex > 1)
            {
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;
            }
            IQueryable<PropertyOwner> owners;
            if (string.IsNullOrEmpty(searchWord))
            {
                owners = db.PropertyOwners.Where(a => a.IsDeleted == false).OrderBy(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.PropertyOwners.Where(a => a.IsDeleted == false).Count();
            }
            else
            {
                owners = db.PropertyOwners.Where(a => a.IsDeleted == false &&
                (a.ArName.Contains(searchWord) || a.EnName.Contains(searchWord) || a.Code.Contains(searchWord)))
                    .OrderBy(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.PropertyOwners.Where(a => a.IsDeleted == false && (a.ArName.Contains(searchWord) || a.EnName.Contains(searchWord) || a.Code.Contains(searchWord))).Count();
            }
            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            return View(owners.ToList());
        }
        public ActionResult AddEdit(int? id)
        {
            SystemSetting systemSetting = db.SystemSettings.Any() ? db.SystemSettings.FirstOrDefault() : new SystemSetting();
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            DepartmentRepository departmentRepository = new DepartmentRepository(db);
            var departments = departmentRepository.UserDepartments(1).ToList();//show all deprartments so that user can select factory department

            if (id == null)
            {
               
                ViewBag.DepartmentId = new SelectList(departmentRepository.UserDepartments(userId), "Id", "ArName", systemSetting.DefaultDepartmentId);
                ViewBag.AccountId = new SelectList(db.ChartOfAccounts.Where(c => c.IsDeleted == false && c.IsActive == true && c.ClassificationId == 3).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                return View();
            }
            PropertyOwner owner = db.PropertyOwners.Find(id);
            if (owner == null)
            {
                return HttpNotFound();
            }
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "اضافة او تعديل الملاك ",
                EnAction = "AddEdit",
                ControllerName = "PropertyOwner",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = id
            });
            ViewBag.Next = QueryHelper.Next((int)id, "PropertyOwner");
            ViewBag.Previous = QueryHelper.Previous((int)id, "PropertyOwner");
            ViewBag.Last = QueryHelper.GetLast("PropertyOwner");
            ViewBag.First = QueryHelper.GetFirst("PropertyOwner");

            ViewBag.AccountId = new SelectList(db.ChartOfAccounts.Where(c => c.IsDeleted == false && c.IsActive == true && c.ClassificationId == 3).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", owner.AccountId);
            ViewBag.DepartmentId = new SelectList(departmentRepository.UserDepartments(userId), "Id", "ArName", owner.DepartmentId);

            //DateTime utcNow = DateTime.UtcNow;
            //TimeZone curTimeZone = TimeZone.CurrentTimeZone;
            //// TimeZoneInfo info = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");
            //TimeZoneInfo info = TimeZoneInfo.FindSystemTimeZoneById(curTimeZone.StandardName);
            //DateTime cTime = TimeZoneInfo.ConvertTimeFromUtc(utcNow, info);

            //ViewBag.RegistrationDate = cTime.ToString("yyyy-MM-ddTHH:mm");
            //ViewBag.RegistrationDate = owner.date != null ? owner.RegistrationDate.Value.ToString("yyyy-MM-ddTHH:mm") : null;

            return View(owner);
        }
        [HttpPost]
        public ActionResult AddEdit(PropertyOwner owner, string newBtn)
        {
            if (ModelState.IsValid)
            {
                var id = owner.Id;
                owner.IsDeleted = false;
                owner.IsActive = true;
                owner.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                if (owner.Id > 0)
                {
                    
                    db.Entry(owner).State = EntityState.Modified;
                    Notification.GetNotification("PropertyOwner", "Edit", "AddEdit", owner.Id, null, "الملاك");
                }
                else
                {
                    owner.Code =  new JavaScriptSerializer().Serialize(SetCodeNum(owner.DepartmentId).Data).ToString().Trim('"');
                    db.PropertyOwners.Add(owner);
                    Notification.GetNotification("PropertyOwner", "Add", "AddEdit", owner.Id, null, "الملاك");
                }
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = id > 0 ? "تعديل الملاك" : "اضافة الملاك",
                    EnAction = "AddEdit",
                    ControllerName = "PropertyOwner",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    CodeOrDocNo = owner.Code
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
            var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .Select(x => new { x.Key, x.Value.Errors })
                    .ToArray();
            return View(owner);
        }

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                PropertyOwner owner = db.PropertyOwners.Find(id);
                owner.IsDeleted = true;
                owner.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                db.Entry(owner).State = EntityState.Modified;
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "حذف الملاك",
                    EnAction = "AddEdit",
                    ControllerName = "PropertyOwner",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    EnItemName = owner.EnName
                });
                Notification.GetNotification("PropertyOwner", "Delete", "Delete", id, null, "الملاك");
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
                PropertyOwner owner = db.PropertyOwners.Find(id);
                if (owner.IsActive == true)
                {
                    owner.IsActive = false;
                }
                else
                {
                    owner.IsActive = true;
                }
                owner.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                db.Entry(owner).State = EntityState.Modified;
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = (bool)owner.IsActive ? "تنشيط الملاك" : "إلغاء تنشيط الملاك",
                    EnAction = "AddEdit",
                    ControllerName = "PropertyOwner",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = owner.Id,
                    EnItemName = owner.EnName,
                    ArItemName = owner.ArName,
                    CodeOrDocNo = owner.Code
                });
                if (owner.IsActive == true)
                {
                    Notification.GetNotification("PropertyOwner", "Activate/Deactivate", "ActivateDeactivate", id, true, "الملاك");
                }
                else
                {
                    Notification.GetNotification("PropertyOwner", "Activate/Deactivate", "ActivateDeactivate", id, false, " الملاك");
                }
                return Content("true");
            }
            catch (Exception ex)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
        }

        [SkipERPAuthorize]
        public JsonResult SetCodeNum(int? id)
        {
            var LastCode = db.Database.SqlQuery<string>($"select isnull((select top(1) Code from PropertyOwner where [DepartmentId] = " + id + "order by  [Id] desc),0)");
            var _Code= double.Parse(LastCode.FirstOrDefault().ToString());
            double i = (_Code) + 1;
            return Json(i, JsonRequestBehavior.AllowGet);
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