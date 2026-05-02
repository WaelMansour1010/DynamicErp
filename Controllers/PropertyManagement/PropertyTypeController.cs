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
    public class PropertyTypeController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        // GET: PropertyType
        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة أنواع العقارات",
                EnAction = "Index",
                ControllerName = "PropertyType",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            Notification.GetNotification("PropertyType", "View", "Index", null, null, "أنواع العقارات");
            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;
            if (pageIndex > 1)
            {
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;
            }
            IQueryable<PropertyType> propertyTypes;
            if (string.IsNullOrEmpty(searchWord))
            {
                propertyTypes = db.PropertyTypes.Where(a => a.IsDeleted == false).OrderBy(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.PropertyTypes.Where(a => a.IsDeleted == false).Count();
            }
            else
            {
                propertyTypes = db.PropertyTypes.Where(a => a.IsDeleted == false &&
                (a.ArName.Contains(searchWord) || a.EnName.Contains(searchWord) || a.Code.Contains(searchWord)))
                    .OrderBy(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.PropertyTypes.Where(a => a.IsDeleted == false && (a.ArName.Contains(searchWord) || a.EnName.Contains(searchWord) || a.Code.Contains(searchWord))).Count();
            }
            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            return View(propertyTypes.ToList());
        }

        public ActionResult AddEdit(int? id)
        {
            if (id == null)
            {

                return View();
            }
            PropertyType propertyType = db.PropertyTypes.Find(id);

            if (propertyType == null)
            {
                return HttpNotFound();
            }
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "اضافة او تعديل أنواع العقارات ",
                EnAction = "AddEdit",
                ControllerName = "PropertyType",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = id
            });
            ViewBag.Next = QueryHelper.Next((int)id, "PropertyType");
            ViewBag.Previous = QueryHelper.Previous((int)id, "PropertyType");
            ViewBag.Last = QueryHelper.GetLast("PropertyType");
            ViewBag.First = QueryHelper.GetFirst("PropertyType");
            return View(propertyType);
        }
        [HttpPost]
        public ActionResult AddEdit(PropertyType propertyType, string newBtn)
        {
            string domainName = Request.Url.GetLeftPart(UriPartial.Authority);

            if (ModelState.IsValid)
            {
                var id = propertyType.Id;
                propertyType.IsDeleted = false;
                propertyType.IsActive = true;
                propertyType.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                if (propertyType.Id > 0)
                {
                    db.Entry(propertyType).State = EntityState.Modified;
                    Notification.GetNotification("PropertyType", "Edit", "AddEdit", propertyType.Id, null, "أنواع العقارات");
                }
                else
                {
                    propertyType.Code = (QueryHelper.CodeLastNum("PropertyType") + 1).ToString();
                    db.PropertyTypes.Add(propertyType);
                    Notification.GetNotification("PropertyType", "Add", "AddEdit", propertyType.Id, null, "أنواع العقارات");
                }
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = id > 0 ? "تعديل أنواع العقارات" : "اضافة أنواع العقارات",
                    EnAction = "AddEdit",
                    ControllerName = "PropertyType",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    CodeOrDocNo = propertyType.Code
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
            return View(propertyType);
        }

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                PropertyType propertyType = db.PropertyTypes.Find(id);
                propertyType.IsDeleted = true;
                propertyType.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                db.Entry(propertyType).State = EntityState.Modified;
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "حذف أنواع العقارات",
                    EnAction = "AddEdit",
                    ControllerName = "PropertyType",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id
                });
                Notification.GetNotification("PropertyType", "Delete", "Delete", id, null, "أنواع العقارات");
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
                PropertyType propertyType = db.PropertyTypes.Find(id);
                if (propertyType.IsActive == true)
                {
                    propertyType.IsActive = false;
                }
                else
                {
                    propertyType.IsActive = true;
                }
                propertyType.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                db.Entry(propertyType).State = EntityState.Modified;
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = (bool)propertyType.IsActive ? "تنشيط أنواع العقارات" : "إلغاء تنشيط أنواع العقارات",
                    EnAction = "AddEdit",
                    ControllerName = "PropertyType",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = propertyType.Id,
                    CodeOrDocNo = propertyType.Code
                });
                if (propertyType.IsActive == true)
                {
                    Notification.GetNotification("PropertyType", "Activate/Deactivate", "ActivateDeactivate", id, true, "أنواع العقارات");
                }
                else
                {
                    Notification.GetNotification("PropertyType", "Activate/Deactivate", "ActivateDeactivate", id, false, " أنواع العقارات");
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
            var code = QueryHelper.CodeLastNum("PropertyType");
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