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
    
    public class CountryController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة الدول",
                EnAction = "Index",
                ControllerName = "Country",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            ////-------------------- Notification-------------------------////
            Notification.GetNotification("Country", "View", "Index", null, null, "الدول");
            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;

            IQueryable<Country> countries;

            if (string.IsNullOrEmpty(searchWord))
            {
                countries = db.Countries.Where(s => s.IsDeleted == false).OrderBy(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.Countries.Where(s => s.IsDeleted == false).Count();
            }
            else
            {
                countries = db.Countries.Where(s => s.IsDeleted == false && (s.Code.Contains(searchWord) || s.ArName.Contains(searchWord) || s.EnName.Contains(searchWord)  || s.ERPUser.Name.Contains(searchWord) || s.Notes.Contains(searchWord))).OrderBy(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = countries.Count();
            }
            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            return View(countries.ToList());


        }
        [SkipERPAuthorize]
        public JsonResult SetCodeNum()
        {
            var code = QueryHelper.CodeLastNum("Country");
            return Json(code + 1, JsonRequestBehavior.AllowGet);
        }
        public ActionResult AddEdit(int? id)
        {

            if (id == null)
            {
                Country NewObj = new Country();
               
                return View(NewObj);
            }
            Country country = db.Countries.Find(id);
            if (country == null)
            {
                return HttpNotFound();
            }
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح تفاصيل الدولة ",
                EnAction = "AddEdit",
                ControllerName = "Country",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = country.Id,
                ArItemName = country.ArName,
                EnItemName = country.EnName,
                CodeOrDocNo = country.Code
            });
            ViewBag.Next = QueryHelper.Next((int)id, "Country");
            ViewBag.Previous = QueryHelper.Previous((int)id, "Country");
            ViewBag.Last = QueryHelper.GetLast("Country");
            ViewBag.First = QueryHelper.GetFirst("Country");
            return View(country);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddEdit(Country country, string newBtn)
        {
            if (ModelState.IsValid)
            {
                country.IsDeleted = false;
                if (country.Id > 0)
                {
                    country.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                    db.Entry(country).State = EntityState.Modified;

                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("Country", "Edit", "AddEdit", country.Id, null, "الدول");
                }
                else
                {
                    country.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                    country.IsActive = true;
                    db.Countries.Add(country);

                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("Country", "Add", "AddEdit", country.Id, null, "الدول");

                }

                try
                {
                    db.SaveChanges();
                }
                catch (Exception ex)
                {

                    var mod = ModelState.First(c => c.Key == "Code");  // this

                    mod.Value.Errors.Add("هذا الكود موجود من قبل");
                    return View(country);
                }
                    QueryHelper.AddLog(new MyLog()
                {
                    ArAction = country.Id > 0 ? "تعديل الدولة" : "اضافة دولة",
                    EnAction = "AddEdit",
                    ControllerName = "Country",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = country.Id,
                    ArItemName = country.ArName,
                    EnItemName = country.EnName,
                    CodeOrDocNo = country.Code
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
  
            return View(country);
        }


        [HttpPost, ActionName("Delete")]
    
        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                Country country = db.Countries.Find(id);
                country.IsDeleted = true;
                country.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                db.Entry(country).State = EntityState.Modified;
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "حذف الدولة",
                    EnAction = "AddEdit",
                    ControllerName = "Country",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    EnItemName = country.EnName,
                    ArItemName = country.ArName,
                    CodeOrDocNo = country.Code
                });
                ////-------------------- Notification-------------------------////
                Notification.GetNotification("Country", "Delete", "Delete", id, null, "الدول");
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
                Country country = db.Countries.Find(id);
                if (country.IsActive == true)
                {
                    country.IsActive = false;
                }
                else
                {
                    country.IsActive = true;
                }
                country.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                db.Entry(country).State = EntityState.Modified;

                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = (bool)country.IsActive ? "تنشيط الدولة" : "إلغاء تنشيط الدولة",
                    EnAction = "AddEdit",
                    ControllerName = "Country",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = country.Id,
                    EnItemName = country.EnName,
                    ArItemName = country.ArName,
                    CodeOrDocNo = country.Code
                });
                ////-------------------- Notification-------------------------////
                if (country.IsActive == true)
                {
                    Notification.GetNotification("Country", "Activate/Deactivate", "ActivateDeactivate", id, true, "الدول");
                }
                else
                {

                    Notification.GetNotification("Country", "Activate/Deactivate", "ActivateDeactivate", country.Id, false, "الدول");
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
