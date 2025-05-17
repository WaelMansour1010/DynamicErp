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
    public class PropertyNotificationController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        // GET: PropertyNotification
        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة إشعار العقار",
                EnAction = "Index",
                ControllerName = "PropertyNotification",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            Notification.GetNotification("PropertyNotification", "View", "Index", null, null, "إشعار العقار");
            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;
            if (pageIndex > 1)
            {
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;
            }
            IQueryable<PropertyNotification> notifications;
            if (string.IsNullOrEmpty(searchWord))
            {
                notifications = db.PropertyNotifications.Where(a => a.IsDeleted == false).OrderBy(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.PropertyNotifications.Where(a => a.IsDeleted == false).Count();
            }
            else
            {
                notifications = db.PropertyNotifications.Where(a => a.IsDeleted == false &&
                (a.ContractNo.Contains(searchWord) || a.Code.Contains(searchWord)))
                    .OrderBy(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.PropertyNotifications.Where(a => a.IsDeleted == false && (a.ContractNo.Contains(searchWord) || a.Code.Contains(searchWord))).Count();
            }
            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            return View(notifications.ToList());
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

                ViewBag.PropertyTypeId = new SelectList(db.PropertyTypes.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                ViewBag.PropertyUnitTypeId = new SelectList(db.PropertyUnitTypes.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                ViewBag.PropertyRenterId = new SelectList(db.PropertyRenters.Where(c => c.IsActive == true).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                ViewBag.PropertyOwnerId = new SelectList(db.PropertyOwners.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");

                DateTime utcNow = DateTime.UtcNow;
                TimeZone curTimeZone = TimeZone.CurrentTimeZone;
                // TimeZoneInfo info = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");
                TimeZoneInfo info = TimeZoneInfo.FindSystemTimeZoneById(curTimeZone.StandardName);
                DateTime cTime = TimeZoneInfo.ConvertTimeFromUtc(utcNow, info);
                ViewBag.Date = cTime.ToString("yyyy-MM-ddTHH:mm");
                ViewBag.ContractEndDate = cTime.ToString("yyyy-MM-ddTHH:mm");
                return View();
            }
            PropertyNotification notification = db.PropertyNotifications.Find(id);
            if (notification == null)
            {
                return HttpNotFound();
            }
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "اضافة او تعديل إشعار العقار ",
                EnAction = "AddEdit",
                ControllerName = "PropertyNotification",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = id
            });
            ViewBag.Next = QueryHelper.Next((int)id, "PropertyNotification");
            ViewBag.Previous = QueryHelper.Previous((int)id, "PropertyNotification");
            ViewBag.Last = QueryHelper.GetLast("PropertyNotification");
            ViewBag.First = QueryHelper.GetFirst("PropertyNotification");

            ViewBag.PropertyTypeId = new SelectList(db.PropertyTypes.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", notification.PropertyTypeId);
            ViewBag.PropertyUnitTypeId = new SelectList(db.PropertyUnitTypes.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", notification.PropertyUnitTypeId);
            ViewBag.PropertyRenterId = new SelectList(db.PropertyRenters.Where(c => c.IsActive == true).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", notification.PropertyRenterId);
            ViewBag.PropertyOwnerId = new SelectList(db.PropertyOwners.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", notification.PropertyOwnerId);
           
            ViewBag.DepartmentId = new SelectList(departmentRepository.UserDepartments(userId), "Id", "ArName", notification.DepartmentId);

            ViewBag.Date = notification.Date != null ? notification.Date.Value.ToString("yyyy-MM-ddTHH:mm") : null;
            ViewBag.ContractEndDate = notification.ContractEndDate != null ? notification.ContractEndDate.Value.ToString("yyyy-MM-ddTHH:mm") : null;

            return View(notification);
        }
        [HttpPost]
        public ActionResult AddEdit(PropertyNotification notification, string newBtn)
        {
            if (ModelState.IsValid)
            {
                var id = notification.Id;
                notification.IsDeleted = false;
                notification.IsActive = true;
                notification.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                if (notification.Id > 0)
                {

                    db.Entry(notification).State = EntityState.Modified;
                    Notification.GetNotification("PropertyNotification", "Edit", "AddEdit", notification.Id, null, "إشعار العقار");
                }
                else
                {
                    notification.Code = new JavaScriptSerializer().Serialize(SetCodeNum(notification.DepartmentId).Data).ToString().Trim('"');
                    db.PropertyNotifications.Add(notification);
                    Notification.GetNotification("PropertyNotification", "Add", "AddEdit", notification.Id, null, "إشعار العقار");
                }
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = id > 0 ? "تعديل إشعار العقار" : "اضافة إشعار العقار",
                    EnAction = "AddEdit",
                    ControllerName = "PropertyNotification",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    CodeOrDocNo = notification.Code
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
            return View(notification);
        }

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                PropertyNotification notification = db.PropertyNotifications.Find(id);
                notification.IsDeleted = true;
                notification.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                db.Entry(notification).State = EntityState.Modified;
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "حذف إشعار العقار",
                    EnAction = "AddEdit",
                    ControllerName = "PropertyNotification",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    CodeOrDocNo=notification.Code
                });
                Notification.GetNotification("PropertyNotification", "Delete", "Delete", id, null, "إشعار العقار");
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
                PropertyNotification notification = db.PropertyNotifications.Find(id);
                if (notification.IsActive == true)
                {
                    notification.IsActive = false;
                }
                else
                {
                    notification.IsActive = true;
                }
                notification.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                db.Entry(notification).State = EntityState.Modified;
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = (bool)notification.IsActive ? "تنشيط إشعار العقار" : "إلغاء تنشيط إشعار العقار",
                    EnAction = "AddEdit",
                    ControllerName = "PropertyNotification",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = notification.Id,
                    CodeOrDocNo = notification.Code
                });
                if (notification.IsActive == true)
                {
                    Notification.GetNotification("PropertyNotification", "Activate/Deactivate", "ActivateDeactivate", id, true, "إشعار العقار");
                }
                else
                {
                    Notification.GetNotification("PropertyNotification", "Activate/Deactivate", "ActivateDeactivate", id, false, " إشعار العقار");
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
            var LastCode = db.Database.SqlQuery<string>($"select isnull((select top(1) Code from PropertyNotification where [DepartmentId] = " + id + "order by  [Id] desc),0)");
            var _Code = double.Parse(LastCode.FirstOrDefault().ToString());
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