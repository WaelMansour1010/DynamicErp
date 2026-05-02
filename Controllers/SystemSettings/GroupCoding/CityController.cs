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
    
    public class CityController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة المدن",
                EnAction = "Index",
                ControllerName = "City",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            ////-------------------- Notification-------------------------////
            Notification.GetNotification("City", "View", "Index", null, null, "المدن");

            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;

            IQueryable<City> cities;

            if (string.IsNullOrEmpty(searchWord))
            {
                cities = db.Cities.Where(s => s.IsDeleted == false).OrderBy(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.Cities.Where(s => s.IsDeleted == false).Count();
            }
            else
            {
                cities = db.Cities.Where(s => s.IsDeleted == false && (s.Code.Contains(searchWord) || s.ArName.Contains(searchWord) || s.EnName.Contains(searchWord)  || s.ERPUser.Name.Contains(searchWord) || s.Notes.Contains(searchWord))).OrderBy(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
           //     ViewBag.Count = db.Cities.Where(s => s.IsDeleted == false && (s.Code.Contains(searchWord) || s.ArName.Contains(searchWord) || s.EnName.Contains(searchWord) ||s.ERPUser.Name.Contains(searchWord) || s.Notes.Contains(searchWord))).Count();
                ViewBag.Count = cities.Count();
            }
            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            return View(cities.ToList());

        }
        [SkipERPAuthorize]
        public JsonResult SetCodeNum()
        {
            var code = QueryHelper.CodeLastNum("City");
            return Json(code + 1, JsonRequestBehavior.AllowGet);
        }


        public ActionResult AddEdit(int? id)
        {

            if (id == null)
            {
                var Code = SetCodeNum();
                ViewBag.Code = int.Parse(Code.Data.ToString());
                //  City NewObj = new City();
                ViewBag.CountryId = new SelectList(db.Countries.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                return View();
            }
            City city = db.Cities.Find(id);
            if (city == null)
            {
                return HttpNotFound();
            }
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح تفاصيل المدينة ",
                EnAction = "AddEdit",
                ControllerName = "City",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = city.Id,
                ArItemName = city.ArName,
                EnItemName = city.EnName,
                CodeOrDocNo = city.Code
            });
            ViewBag.CountryId = new SelectList(db.Countries.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName",city.CountryId);
            ViewBag.Next = QueryHelper.Next((int)id, "City");
            ViewBag.Previous = QueryHelper.Previous((int)id, "City");
            ViewBag.Last = QueryHelper.GetLast("City");
            ViewBag.First = QueryHelper.GetFirst("City");
            return View(city);
        }


        [HttpPost]
        //[ValidateAntiForgeryToken]
        public ActionResult AddEdit(City city, string newBtn)
        {
            if (ModelState.IsValid)
            {
                var id = city.Id;
                city.IsDeleted = false;
                if (city.Id > 0)
                {
                    city.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                    db.Entry(city).State = EntityState.Modified;

                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("City", "Edit", "AddEdit", city.Id, null, "المدن");

                    //int pageid = db.Get_PageId("City").SingleOrDefault().Value;
                    //var actionId = db.PageActions.FirstOrDefault(c => c.Action == "AddEdit" && c.EnName == "Edit" && c.PageId == pageid).Id;
                    //int userid = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    //var UserName = User.Identity.Name;
                    //db.Sp_OccuredNotification(actionId, $"بتعديل بيانات في شاشة المدن  {UserName}قام المستخدم ");
                    //////////////////-----------------------------------------------------------------------

                }
                else
                {
                    city.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    city.Code= (QueryHelper.CodeLastNum("City") + 1).ToString();
                    city.IsActive = true;
                    db.Cities.Add(city);

                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("City", "Add", "AddEdit", city.Id, null, "المدن");

                    //int pageid = db.Get_PageId("City").SingleOrDefault().Value;
                    //var actionId = db.PageActions.FirstOrDefault(c => c.Action == "AddEdit" && c.EnName == "Add" && c.PageId == pageid).Id;
                    //int userid = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    //var UserName = User.Identity.Name;
                    //db.Sp_OccuredNotification(actionId, $"باضافة بيانات في شاشة المدن  {UserName}قام المستخدم  ");

                    ////////////////-----------------------------------------------------------------------

                }

                try
                {
                    db.SaveChanges();
                }
                catch (Exception ex)
                {

                    var mod = ModelState.First(c => c.Key == "Code");  // this

                    mod.Value.Errors.Add("هذا الكود موجود من قبل");
                   // ViewBag.CountryId = new SelectList(db.Countries.Where(c => c.IsDeleted == false && c.IsActive == true), "Id", "ArName", city.CountryId);

                    return View(city);
                }
                    QueryHelper.AddLog(new MyLog()
                {
                    ArAction = city.Id > 0 ? "تعديل مدينة" : "اضافة مدينة",
                    EnAction = "AddEdit",
                    ControllerName = "City",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = city.Id > 0 ? city.Id : db.Cities.Max(i => i.Id),
                    ArItemName = city.ArName,
                    EnItemName = city.EnName,
                    CodeOrDocNo = city.Code
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
            ViewBag.CountryId = new SelectList(db.Countries.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", city.CountryId);

            return View(city);
        }


        [HttpPost, ActionName("Delete")]
 
        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                City city = db.Cities.Find(id);
                city.IsDeleted = true;
                city.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                db.Entry(city).State = EntityState.Modified;
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "حذف مدينة",
                    EnAction = "AddEdit",
                    ControllerName = "City",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    EnItemName = city.EnName,
                    ArItemName = city.ArName,
                    CodeOrDocNo = city.Code
                });
                ////-------------------- Notification-------------------------////
                Notification.GetNotification("City", "Delete", "Delete", id, null, "المدن");

                //int pageid = db.Get_PageId("City").SingleOrDefault().Value;
                //var actionId = db.PageActions.FirstOrDefault(c => c.Action == "Delete" && c.EnName == "Delete" && c.PageId == pageid).Id;
                //int userid = db.UserNotifications.FirstOrDefault(c => c.ActionId == actionId && c.PageId == pageid).UserId.Value;
                //var UserName = User.Identity.Name;
                //db.Sp_OccuredNotification(actionId, $"بحذف بيانات في شاشة المدن  {UserName}قام المستخدم  ");
                //////////////////-----------------------------------------------------------------------

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
                City city = db.Cities.Find(id);
                if (city.IsActive == true)
                {
                    city.IsActive = false;
                }
                else
                {
                    city.IsActive = true;
                }
                city.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                db.Entry(city).State = EntityState.Modified;

                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = (bool)city.IsActive ? "تنشيط المدينة" : "إلغاء تنشيط المدينة",
                    EnAction = "AddEdit",
                    ControllerName = "City",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = city.Id,
                    EnItemName = city.EnName,
                    ArItemName = city.ArName,
                    CodeOrDocNo = city.Code
                });
                ////-------------------- Notification-------------------------////

                //int pageid = db.Get_PageId("City").SingleOrDefault().Value;
                //var actionId = db.PageActions.FirstOrDefault(c => c.Action == "ActivateDeactivate" && c.EnName == "Activate/Deactivate" && c.PageId == pageid).Id;
                //int userid = db.UserNotifications.FirstOrDefault(c => c.ActionId == actionId && c.PageId == pageid).UserId.Value;
                //var UserName = User.Identity.Name;
                //db.Sp_OccuredNotification(actionId, (bool)city.IsActive ? $" تنشيط  في شاشة المدن{UserName}قام المستخدم  " : $"إلغاء تنشيط  في شاشة المدن{UserName}قام المستخدم  ");
                //////////////////-----------------------------------------------------------------------
                if (city.IsActive == true)
                {
                    Notification.GetNotification("City", "Activate/Deactivate", "ActivateDeactivate", id, true, "المدن");
                }
                else
                {

                    Notification.GetNotification("City", "Activate/Deactivate", "ActivateDeactivate", id, false, "المدن");
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
