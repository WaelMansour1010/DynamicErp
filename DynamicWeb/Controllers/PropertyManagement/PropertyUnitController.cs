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
    public class PropertyUnitController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        // GET: PropertyUnit
        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمةالوحدة",
                EnAction = "Index",
                ControllerName = "PropertyUnit",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            Notification.GetNotification("PropertyUnit", "View", "Index", null, null, "الوحدة");
            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;
            if (pageIndex > 1)
            {
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;
            }
            IQueryable<PropertyUnit> propertyUnits;
            if (string.IsNullOrEmpty(searchWord))
            {
                propertyUnits = db.PropertyUnits.Where(a => a.IsDeleted == false).OrderBy(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.PropertyUnits.Where(a => a.IsDeleted == false).Count();
            }
            else
            {
                propertyUnits = db.PropertyUnits.Where(a => a.IsDeleted == false &&
                (a.ArName.Contains(searchWord) || a.EnName.Contains(searchWord) || a.Code.Contains(searchWord)))
                    .OrderBy(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.PropertyUnits.Where(a => a.IsDeleted == false && (a.ArName.Contains(searchWord) || a.EnName.Contains(searchWord) || a.Code.Contains(searchWord))).Count();
            }
            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            return View(propertyUnits.ToList());
        }

        public ActionResult AddEdit(int? id)
        {
            if (id == null)
            {
                ViewBag.PropertyUnitTypeId = new SelectList(db.PropertyUnitTypes.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                ViewBag.PropertyUnitStatusId = new SelectList(new List<dynamic> {
                new { id=1,name="تم التسكين"},
                 new { id=2,name="متاح"}}, "id", "name");

                return View();
            }
            PropertyUnit unit = db.PropertyUnits.Find(id);

            if (unit == null)
            {
                return HttpNotFound();
            }
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "اضافة او تعديل الوحدة ",
                EnAction = "AddEdit",
                ControllerName = "PropertyUnit",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = id
            });
            ViewBag.PropertyUnitTypeId = new SelectList(db.PropertyUnitTypes.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName",unit.PropertyUnitTypeId);
            ViewBag.PropertyUnitStatusId = new SelectList(new List<dynamic> {
                new { id=1,name="تم التسكين"},
                 new { id=2,name="متاح"}}, "id", "name", unit.PropertyUnitStatusId);

            ViewBag.Next = QueryHelper.Next((int)id, "PropertyUnit");
            ViewBag.Previous = QueryHelper.Previous((int)id, "PropertyUnit");
            ViewBag.Last = QueryHelper.GetLast("PropertyUnit");
            ViewBag.First = QueryHelper.GetFirst("PropertyUnit");
            return View(unit);
        }
        [HttpPost]
        public ActionResult AddEdit(PropertyUnit unit, string newBtn)
        {
            string domainName = Request.Url.GetLeftPart(UriPartial.Authority);

            if (ModelState.IsValid)
            {
                var id = unit.Id;
                unit.IsDeleted = false;
                unit.IsActive = true;
                unit.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                if (unit.Id > 0)
                {
                    db.Entry(unit).State = EntityState.Modified;
                    Notification.GetNotification("PropertyUnit", "Edit", "AddEdit", unit.Id, null, "الوحدة");
                }
                else
                {
                    unit.Code = (QueryHelper.CodeLastNum("PropertyUnit") + 1).ToString();
                    db.PropertyUnits.Add(unit);
                    Notification.GetNotification("PropertyUnit", "Add", "AddEdit", unit.Id, null, "الوحدة");
                }
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = id > 0 ? "تعديل الوحدة" : "اضافة الوحدة",
                    EnAction = "AddEdit",
                    ControllerName = "PropertyUnit",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    CodeOrDocNo = unit.Code
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
            return View(unit);
        }

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                PropertyUnit unit = db.PropertyUnits.Find(id);
                unit.IsDeleted = true;
                unit.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                db.Entry(unit).State = EntityState.Modified;
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "حذف الوحدة",
                    EnAction = "AddEdit",
                    ControllerName = "PropertyUnit",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id
                });
                Notification.GetNotification("PropertyUnit", "Delete", "Delete", id, null, "الوحدة");
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
                PropertyUnit unit = db.PropertyUnits.Find(id);
                if (unit.IsActive == true)
                {
                    unit.IsActive = false;
                }
                else
                {
                    unit.IsActive = true;
                }
                unit.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                db.Entry(unit).State = EntityState.Modified;
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = (bool)unit.IsActive ? "تنشيط الوحدة" : "إلغاء تنشيط الوحدة",
                    EnAction = "AddEdit",
                    ControllerName = "PropertyUnit",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = unit.Id,
                    CodeOrDocNo = unit.Code
                });
                if (unit.IsActive == true)
                {
                    Notification.GetNotification("PropertyUnit", "Activate/Deactivate", "ActivateDeactivate", id, true, "الوحدة");
                }
                else
                {
                    Notification.GetNotification("PropertyUnit", "Activate/Deactivate", "ActivateDeactivate", id, false, "الوحدة");
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
            var code = QueryHelper.CodeLastNum("PropertyUnit");
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