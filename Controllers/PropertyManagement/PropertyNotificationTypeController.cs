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

namespace MyERP.Controllers.PropertyManagement
{
    public class PropertyNotificationTypeController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        // GET: PropertyNotificationType
        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة أنواع الإشعارات",
                EnAction = "Index",
                ControllerName = "PropertyNotificationType",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            Notification.GetNotification("PropertyNotificationType", "View", "Index", null, null, "أنواع الإشعارات");
            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;
            if (pageIndex > 1)
            {
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;
            }
            IQueryable<PropertyNotificationType> propertyNotificationTypes;
            if (string.IsNullOrEmpty(searchWord))
            {
                propertyNotificationTypes = db.PropertyNotificationTypes.Where(a => a.IsDeleted == false).OrderBy(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.PropertyNotificationTypes.Where(a => a.IsDeleted == false).Count();
            }
            else
            {
                propertyNotificationTypes = db.PropertyNotificationTypes.Where(a => a.IsDeleted == false &&
                (a.ArName.Contains(searchWord) || a.EnName.Contains(searchWord) || a.Code.Contains(searchWord)))
                    .OrderBy(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.PropertyNotificationTypes.Where(a => a.IsDeleted == false && (a.ArName.Contains(searchWord) || a.EnName.Contains(searchWord) || a.Code.Contains(searchWord))).Count();
            }
            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            return View(propertyNotificationTypes.ToList());
        }

        public ActionResult AddEdit(int? id)
        {
            if (id == null)
            {

                return View();
            }
            PropertyNotificationType propertyNotificationType = db.PropertyNotificationTypes.Find(id);

            if (propertyNotificationType == null)
            {
                return HttpNotFound();
            }
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "اضافة او تعديل أنواع الإشعارات ",
                EnAction = "AddEdit",
                ControllerName = "PropertyNotificationType",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = id
            });
            ViewBag.Next = QueryHelper.Next((int)id, "PropertyNotificationType");
            ViewBag.Previous = QueryHelper.Previous((int)id, "PropertyNotificationType");
            ViewBag.Last = QueryHelper.GetLast("PropertyNotificationType");
            ViewBag.First = QueryHelper.GetFirst("PropertyNotificationType");
            return View(propertyNotificationType);
        }
        [HttpPost]
        public ActionResult AddEdit(PropertyNotificationType propertyNotificationType, string newBtn)
        {
            string domainName = Request.Url.GetLeftPart(UriPartial.Authority);

            if (ModelState.IsValid)
            {
                var id = propertyNotificationType.Id;
                propertyNotificationType.IsDeleted = false;
                propertyNotificationType.IsActive = true;
                propertyNotificationType.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                if (propertyNotificationType.Id > 0)
                {
                    db.Entry(propertyNotificationType).State = EntityState.Modified;
                    Notification.GetNotification("PropertyNotificationType", "Edit", "AddEdit", propertyNotificationType.Id, null, "أنواع الإشعارات");
                }
                else
                {
                    propertyNotificationType.Code = (QueryHelper.CodeLastNum("PropertyNotificationType") + 1).ToString();
                    db.PropertyNotificationTypes.Add(propertyNotificationType);
                    Notification.GetNotification("PropertyNotificationType", "Add", "AddEdit", propertyNotificationType.Id, null, "أنواع الإشعارات");
                }
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = id > 0 ? "تعديل أنواع الإشعارات" : "اضافة أنواع الإشعارات",
                    EnAction = "AddEdit",
                    ControllerName = "PropertyNotificationType",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    CodeOrDocNo = propertyNotificationType.Code
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
            return View(propertyNotificationType);
        }

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                PropertyNotificationType propertyNotificationType = db.PropertyNotificationTypes.Find(id);
                propertyNotificationType.IsDeleted = true;
                propertyNotificationType.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                db.Entry(propertyNotificationType).State = EntityState.Modified;
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "حذف أنواع الإشعارات",
                    EnAction = "AddEdit",
                    ControllerName = "PropertyNotificationType",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id
                });
                Notification.GetNotification("PropertyNotificationType", "Delete", "Delete", id, null, "أنواع الإشعارات");
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
                PropertyNotificationType propertyNotificationType = db.PropertyNotificationTypes.Find(id);
                if (propertyNotificationType.IsActive == true)
                {
                    propertyNotificationType.IsActive = false;
                }
                else
                {
                    propertyNotificationType.IsActive = true;
                }
                propertyNotificationType.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                db.Entry(propertyNotificationType).State = EntityState.Modified;
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = (bool)propertyNotificationType.IsActive ? "تنشيط أنواع الإشعارات" : "إلغاء تنشيط أنواع الإشعارات",
                    EnAction = "AddEdit",
                    ControllerName = "PropertyNotificationType",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = propertyNotificationType.Id,
                    CodeOrDocNo = propertyNotificationType.Code
                });
                if (propertyNotificationType.IsActive == true)
                {
                    Notification.GetNotification("PropertyNotificationType", "Activate/Deactivate", "ActivateDeactivate", id, true, "أنواع الإشعارات");
                }
                else
                {
                    Notification.GetNotification("PropertyNotificationType", "Activate/Deactivate", "ActivateDeactivate", id, false, " أنواع الإشعارات");
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
            var code = QueryHelper.CodeLastNum("PropertyNotificationType");
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