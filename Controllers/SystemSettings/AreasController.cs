using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using MyERP.Models;
using System.Security.Claims;

namespace MyERP.Controllers.SystemSettings
{
    
    public class AreasController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        // GET: areas
        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة المناطق",
                EnAction = "Index",
                ControllerName = "Areas",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            ////-------------------- Notification-------------------------////
            Notification.GetNotification("Areas", "View", "Index", null, null, "المناطق");
            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;

            IQueryable<Area> areas;

            if (string.IsNullOrEmpty(searchWord))
            {
                areas = db.Areas.Where(s => s.IsDeleted == false).OrderBy(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.Areas.Where(s => s.IsDeleted == false).Count();
            }
            else
            {
                areas = db.Areas.Where(s => s.IsDeleted == false && (s.Code.Contains(searchWord) || s.ArName.Contains(searchWord) || s.EnName.Contains(searchWord)   || s.ERPUser.Name.Contains(searchWord) || s.Notes.Contains(searchWord) )).OrderBy(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = areas.Count();
            }
            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            return View(areas.ToList());
        }
        [SkipERPAuthorize]
        public JsonResult SetCodeNum()
        {
            var code = QueryHelper.CodeLastNum("Area");
            return Json(code + 1, JsonRequestBehavior.AllowGet);
        }


        public ActionResult AddEdit(int? id)
        {

            if (id == null)
            {
                Area NewObj = new Area();
                ViewBag.CityId = new SelectList(db.Cities.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                ViewBag.CountryId = new SelectList(db.Countries.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");

                return View(NewObj);
            }
            Area area = db.Areas.Find(id);
            if (area == null)
            {
                return HttpNotFound();
            }

            ViewBag.CityId = new SelectList(db.Cities.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName",area.CityId);
            ViewBag.CountryId = new SelectList(db.Countries.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", area.CountryId);
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح تفاصيل منطقة",
                EnAction = "AddEdit",
                ControllerName = "Areas",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = area.Id,
                ArItemName = area.ArName,
                EnItemName = area.EnName,
                CodeOrDocNo = area.Code
            });
            ViewBag.Next = QueryHelper.Next((int)id, "Area");
            ViewBag.Previous = QueryHelper.Previous((int)id, "Area");
            ViewBag.Last = QueryHelper.GetLast("Area");
            ViewBag.First = QueryHelper.GetFirst("Area");
            return View(area);
        }

   
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddEdit( Area area,string newBtn)
        {
            if (ModelState.IsValid)
            {
                var id = area.Id;
                area.IsDeleted = false;
                if (area.Id > 0)
                {
                    area.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                    db.Entry(area).State = EntityState.Modified;
                    Notification.GetNotification("Areas", "Edit", "AddEdit", id, null, "المناطق");

                   
                }
                else
                {
                    area.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    area.Code= (QueryHelper.CodeLastNum("Area") + 1).ToString();
                    area.IsActive = true;
                    db.Areas.Add(area);

                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("Areas", "Add", "AddEdit", area.Id, null, "المناطق");

                 

                }
                try
                {
                    db.SaveChanges();
                }
                catch (Exception ex)
                {

                    var mod = ModelState.First(c => c.Key == "Code");  // this

                    mod.Value.Errors.Add("هذا الكود موجود من قبل");
                    ViewBag.CityId = new SelectList(db.Cities.Where(c => c.IsDeleted == false && c.IsActive == true), "Id", "ArName", area.CityId);
                    ViewBag.CountryId = new SelectList(db.Countries.Where(c => c.IsDeleted == false && c.IsActive == true), "Id", "ArName", area.CountryId);
                    return View(area);
                }
                    QueryHelper.AddLog(new MyLog()
                {
                    ArAction = id> 0 ? "تعديل منطقة" : "اضافة منطقة",
                    EnAction = "AddEdit",
                    ControllerName = "Areas",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = area.Id ,
                    ArItemName = area.ArName,
                    EnItemName = area.EnName,
                    CodeOrDocNo = area.Code
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
            ViewBag.CityId = new SelectList(db.Cities.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", area.CityId);
            ViewBag.CountryId = new SelectList(db.Countries.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", area.CountryId);
            return View(area);
        }
        [SkipERPAuthorize]
        public JsonResult CitiesByCountryId(int id)
        {
            var cites = db.Cities.Where(w => w.IsActive == true && w.IsDeleted == false && w.CountryId == id).Select(w => new { w.Id, w.ArName });
            return Json(cites, JsonRequestBehavior.AllowGet);
        }
        [HttpPost, ActionName("Delete")]
       
        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                Area area = db.Areas.Find(id);
                area.IsDeleted = true;
                area.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                db.Entry(area).State = EntityState.Modified;
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "حذف منطقة",
                    EnAction = "AddEdit",
                    ControllerName = "Areas",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    EnItemName = area.EnName,
                    ArItemName = area.ArName,
                    CodeOrDocNo = area.Code
                });
                ////-------------------- Notification-------------------------////

            
                Notification.GetNotification("Areas", "Delete", "Delete", id, null, "المناطق");


                return Content("true");
            }
            catch (Exception)
            {

                throw;
            }
        }

        [HttpPost]
        public ActionResult ActivateDeactivate(int id)
        {
            try
            {
                Area area = db.Areas.Find(id);
                if (area.IsActive == true)
                {
                    area.IsActive = false;
                }
                else
                {
                    area.IsActive = true;
                }
                area.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                db.Entry(area).State = EntityState.Modified;

                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = (bool)area.IsActive ? "تنشيط منطقة" : "إلغاء منطقة",
                    EnAction = "AddEdit",
                    ControllerName = "Areas",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = area.Id,
                    EnItemName = area.EnName,
                    ArItemName = area.ArName,
                    CodeOrDocNo = area.Code
                });
                ////-------------------- Notification-------------------------////
                if (area.IsActive == true)
                {
                    Notification.GetNotification("Areas", "Activate/Deactivate", "ActivateDeactivate", id, true, "المناطق");
                }
                else
                {

                    Notification.GetNotification("Areas", "Activate/Deactivate", "ActivateDeactivate", id, false, "المناطق");
                }
             

                return Content("true");
            }
            catch (Exception ex)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
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
