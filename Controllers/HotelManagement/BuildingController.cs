using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;
using MyERP.Models;

namespace MyERP.Controllers.HotelManagement
{
    public class BuildingController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        // GET: Building
        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة المنشأت",
                EnAction = "Index",
                ControllerName = "Building",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            Notification.GetNotification("Building", "View", "Index", null, null, "المنشأت");
            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;
            if (pageIndex > 1)
            {
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;
            }
            IQueryable<Building> buildings;
            if (string.IsNullOrEmpty(searchWord))
            {
                buildings = db.Buildings.Where(a => a.IsDeleted == false).OrderBy(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.Buildings.Where(a => a.IsDeleted == false).Count();
            }
            else
            {
                buildings = db.Buildings.Where(a => a.IsDeleted == false &&
                (a.ArName.Contains(searchWord) || a.EnName.Contains(searchWord) || a.Code.Contains(searchWord)))
                    .OrderBy(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.Buildings.Where(a => a.IsDeleted == false && (a.ArName.Contains(searchWord) || a.EnName.Contains(searchWord) || a.Code.Contains(searchWord))).Count();
            }
            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            return View(buildings.ToList());
        }
        public ActionResult AddEdit(int? id)
        {
            if (id == null)
            {
                return View();
            }
            Building building = db.Buildings.Find(id);
            if (building == null)
            {
                return HttpNotFound();
            }
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "اضافة او تعديل المنشأت ",
                EnAction = "AddEdit",
                ControllerName = "Building",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = id
            });
            ViewBag.Next = QueryHelper.Next((int)id, "Building");
            ViewBag.Previous = QueryHelper.Previous((int)id, "Building");
            ViewBag.Last = QueryHelper.GetLast("Building");
            ViewBag.First = QueryHelper.GetFirst("Building");
            return View(building);
        }
        [HttpPost]
        public ActionResult AddEdit(Building building, string newBtn)
        {
            if (ModelState.IsValid)
            {
                var id = building.Id;
                building.IsDeleted = false;
                building.IsActive = true;
                if (building.Id > 0)
                {
                    building.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    db.Entry(building).State = EntityState.Modified;
                    Notification.GetNotification("Building", "Edit", "AddEdit", building.Id, null, "المنشأت");
                }
                else
                {
                    building.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    building.Code = (QueryHelper.CodeLastNum("Building") + 1).ToString();
                    db.Buildings.Add(building);
                    Notification.GetNotification("Building", "Add", "AddEdit", building.Id, null, "المنشأت");
                }
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = id > 0 ? "تعديل المنشأت" : "اضافة المنشأت",
                    EnAction = "AddEdit",
                    ControllerName = "Building",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    CodeOrDocNo = building.Code
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
            return View(building);
        }

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                Building building = db.Buildings.Find(id);
                building.IsDeleted = true;
                building.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                db.Entry(building).State = EntityState.Modified;
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "حذف المنشأت",
                    EnAction = "AddEdit",
                    ControllerName = "Building",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    EnItemName = building.EnName
                });
                Notification.GetNotification("Building", "Delete", "Delete", id, null, "المنشأت");
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
                Building building = db.Buildings.Find(id);
                if (building.IsActive == true)
                {
                    building.IsActive = false;
                }
                else
                {
                    building.IsActive = true;
                }
                building.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                db.Entry(building).State = EntityState.Modified;
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = (bool)building.IsActive ? "تنشيط المنشأت" : "إلغاء تنشيط المنشأت",
                    EnAction = "AddEdit",
                    ControllerName = "Building",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = building.Id,
                    EnItemName = building.EnName,
                    ArItemName = building.ArName,
                    CodeOrDocNo = building.Code
                });
                if (building.IsActive == true)
                {
                    Notification.GetNotification("Building", "Activate/Deactivate", "ActivateDeactivate", id, true, "المنشأت");
                }
                else
                {
                    Notification.GetNotification("Building", "Activate/Deactivate", "ActivateDeactivate", id, false, " المنشأت");
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
            var code = QueryHelper.CodeLastNum("Building");
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