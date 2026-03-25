using MyERP.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;

namespace MyERP.Controllers
{
    public class LocationController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();
        // GET: Location
        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "قائمة المواقع",
                EnAction = "Index",
                ControllerName = "Location",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });

            //--------------------- Notification --------------------//
            Notification.GetNotification("Location", "View", "Index", null, null, "المواقع");

            //////////////////////////////////////////////////////////////////////////////////////
            //-------------------------------paging--------------------------------//
            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;

            //////////////////////////////////////////////////////////////////////////////////////
            //------------------------------- Search ------------------------------------------------//
            IQueryable<Location> locations;
            if (string.IsNullOrEmpty(searchWord))
            {
                locations = db.Locations.Where(s => s.IsDeleted == false).OrderBy(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.Locations.Where(c => c.IsDeleted == false).Count();
            }
            else
            {
                locations = db.Locations.Where(s => s.IsDeleted == false && (s.ArName.Contains(searchWord) || s.EnName.Contains(searchWord) || s.Code.Contains(searchWord))).OrderBy(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.Locations.Where(s => s.IsDeleted == false && (s.ArName.Contains(searchWord) || s.EnName.Contains(searchWord) || s.Code.Contains(searchWord))).OrderBy(s => s.Id).Count();
            }
            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            return View(locations.ToList());
        }

        //////////////////////////////////////////////////////////////////////////////////////
        //--------------------------- Authorization --------------------------------//
        [SkipERPAuthorize]
        public JsonResult SetCodeNum()
        {
            var code = QueryHelper.CodeLastNum("Location");
            return Json(code + 1, JsonRequestBehavior.AllowGet);
        }

        //////////////////////////////////////////////////////////////////////////////////////
        //--------------------------------- Add Or Edit -----------------------------------//
        public ActionResult AddEdit(int? id)
        {

            if (id == null)
            {
                var Code = SetCodeNum();
                ViewBag.Code = int.Parse(Code.Data.ToString());
                return View();
            }
            Location location = db.Locations.Find(id);

            if (location == null)
            {
                return HttpNotFound();
            }

            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "اضافة او تعديل موقع ",
                EnAction = "AddEdit",
                ControllerName = "Location",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = id
            });



            ViewBag.Next = QueryHelper.Next((int)id, "Location");
            ViewBag.Previous = QueryHelper.Previous((int)id, "Location");
            ViewBag.Last = QueryHelper.GetLast("Location");
            ViewBag.First = QueryHelper.GetFirst("Location");


            return View(location);
        }

        [HttpPost]
        public ActionResult AddEdit(Location location, string newBtn)
        {
            if (ModelState.IsValid)
            {
                //--------------------------- Edit if there is id ------------------------//
                var id = location.Id;
                location.IsDeleted = false;
                if (location.Id > 0)
                {
                    location.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                    db.Entry(location).State = EntityState.Modified;

                    ////-------------------- Notification for any edit -------------------------////
                    Notification.GetNotification("Location", "Edit", "AddEdit", id, null, "المواقع");
                }
                else
                {
                    location.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    location.Code = (QueryHelper.CodeLastNum("Location") + 1).ToString();
                    location.IsActive = true;
                    db.Locations.Add(location);


                    ////-------------------- Notification for adding new shift -------------------------////
                    Notification.GetNotification("Location", "Add", "AddEdit", location.Id, null, "المواقع");
                }

                try
                {
                    db.SaveChanges();
                }
                catch (Exception ex)
                {

                    var mod = ModelState.First(c => c.Key == "Code");  // this
                    mod.Value.Errors.Add("هذا الكود موجود من قبل");
                    return View(location);
                }
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = id > 0 ? "تعديل موقع" : "اضافة موقع",
                    EnAction = "AddEdit",
                    ControllerName = "Location",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    CodeOrDocNo = location.Code

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

            return View(location);
        }

        [HttpPost, ActionName("Delete")]

        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                Location location = db.Locations.Find(id);
                location.IsDeleted = true;
                location.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                db.Entry(location).State = EntityState.Modified;
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "حذف الموقع",
                    EnAction = "AddEdit",
                    ControllerName = "Location",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id
                });
                ////-------------------- Notification-------------------------////
                Notification.GetNotification("Location", "Delete", "Delete", id, null, "المواقع");
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
                Location location = db.Locations.Find(id);
                if (location.IsActive == true)
                {
                    location.IsActive = false;
                }
                else
                {
                    location.IsActive = true;
                }
                location.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                db.Entry(location).State = EntityState.Modified;

                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = (bool)location.IsActive ? "تعديل موقع" : "اضافة موقع",
                    EnAction = "AddEdit",
                    ControllerName = "Location",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = location.Id,
                });
                ////-------------------- Notification-------------------------////
                if (location.IsActive == true)
                {

                    Notification.GetNotification("Location", "Activate/Deactivate", "ActivateDeactivate", id, true, "المواقع");
                }
                else
                {

                    Notification.GetNotification("Location", "Activate/Deactivate", "ActivateDeactivate", id, false, "المواقع");
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