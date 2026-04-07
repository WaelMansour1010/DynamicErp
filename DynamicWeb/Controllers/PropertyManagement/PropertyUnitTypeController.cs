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
    public class PropertyUnitTypeController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        // GET: PropertyUnitType
        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمةأنواع الوحدات",
                EnAction = "Index",
                ControllerName = "PropertyUnitType",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            Notification.GetNotification("PropertyUnitType", "View", "Index", null, null, "أنواع الوحدات");
            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;
            if (pageIndex > 1)
            {
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;
            }
            IQueryable<PropertyUnitType> unitTypes;
            if (string.IsNullOrEmpty(searchWord))
            {
                unitTypes = db.PropertyUnitTypes.Where(a => a.IsDeleted == false).OrderBy(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.PropertyUnitTypes.Where(a => a.IsDeleted == false).Count();
            }
            else
            {
                unitTypes = db.PropertyUnitTypes.Where(a => a.IsDeleted == false &&
                (a.ArName.Contains(searchWord) || a.EnName.Contains(searchWord) || a.Code.Contains(searchWord)))
                    .OrderBy(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.PropertyUnitTypes.Where(a => a.IsDeleted == false && (a.ArName.Contains(searchWord) || a.EnName.Contains(searchWord) || a.Code.Contains(searchWord))).Count();
            }
            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            return View(unitTypes.ToList());
        }

        public ActionResult AddEdit(int? id)
        {
            if (id == null)
            {

                return View();
            }
            PropertyUnitType unitType = db.PropertyUnitTypes.Find(id);

            if (unitType == null)
            {
                return HttpNotFound();
            }
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "اضافة او تعديل أنواع الوحدات ",
                EnAction = "AddEdit",
                ControllerName = "PropertyUnitType",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = id
            });
            ViewBag.Next = QueryHelper.Next((int)id, "PropertyUnitType");
            ViewBag.Previous = QueryHelper.Previous((int)id, "PropertyUnitType");
            ViewBag.Last = QueryHelper.GetLast("PropertyUnitType");
            ViewBag.First = QueryHelper.GetFirst("PropertyUnitType");
            return View(unitType);
        }
        [HttpPost]
        public ActionResult AddEdit(PropertyUnitType unitType, string newBtn)
        {
            string domainName = Request.Url.GetLeftPart(UriPartial.Authority);

            if (ModelState.IsValid)
            {
                var id = unitType.Id;
                unitType.IsDeleted = false;
                unitType.IsActive = true;
                unitType.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                if (unitType.Id > 0)
                {
                    db.Entry(unitType).State = EntityState.Modified;
                    Notification.GetNotification("PropertyUnitType", "Edit", "AddEdit", unitType.Id, null, "أنواع الوحدات");
                }
                else
                {
                    unitType.Code = (QueryHelper.CodeLastNum("PropertyUnitType") + 1).ToString();
                    db.PropertyUnitTypes.Add(unitType);
                    Notification.GetNotification("PropertyUnitType", "Add", "AddEdit", unitType.Id, null, "أنواع الوحدات");
                }
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = id > 0 ? "تعديل أنواع الوحدات" : "اضافة أنواع الوحدات",
                    EnAction = "AddEdit",
                    ControllerName = "PropertyUnitType",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    CodeOrDocNo = unitType.Code
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
            return View(unitType);
        }

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                PropertyUnitType unitType = db.PropertyUnitTypes.Find(id);
                unitType.IsDeleted = true;
                unitType.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                db.Entry(unitType).State = EntityState.Modified;
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "حذف أنواع الوحدات",
                    EnAction = "AddEdit",
                    ControllerName = "PropertyUnitType",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id
                });
                Notification.GetNotification("PropertyUnitType", "Delete", "Delete", id, null, "أنواع الوحدات");
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
                PropertyUnitType unitType = db.PropertyUnitTypes.Find(id);
                if (unitType.IsActive == true)
                {
                    unitType.IsActive = false;
                }
                else
                {
                    unitType.IsActive = true;
                }
                unitType.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                db.Entry(unitType).State = EntityState.Modified;
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = (bool)unitType.IsActive ? "تنشيط أنواع الوحدات" : "إلغاء تنشيط أنواع الوحدات",
                    EnAction = "AddEdit",
                    ControllerName = "PropertyUnitType",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = unitType.Id,
                    CodeOrDocNo = unitType.Code
                });
                if (unitType.IsActive == true)
                {
                    Notification.GetNotification("PropertyUnitType", "Activate/Deactivate", "ActivateDeactivate", id, true, "أنواع الوحدات");
                }
                else
                {
                    Notification.GetNotification("PropertyUnitType", "Activate/Deactivate", "ActivateDeactivate", id, false, "أنواع الوحدات");
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
            var code = QueryHelper.CodeLastNum("PropertyUnitType");
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