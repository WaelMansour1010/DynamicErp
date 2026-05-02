using MyERP.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;

namespace MyERP.Controllers
{
    [AllowAnonymous]
    [SkipERPAuthorize]
    public class HousingController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();
        // GET: Housing
        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            //QueryHelper.AddLog(new MyLog()
            //{
            //    ArAction = "قائمة التسكين",
            //    EnAction = "Index",
            //    ControllerName = "Housing",
            //    UserName = User.Identity.Name,
            //    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
            //    LogDate = DateTime.Now,
            //    RequestMethod = "GET"
            //});

            //--------------------- Notification --------------------//
            Notification.GetNotification("Housing", "View", "Index", null, null, "التسكين");

            //////////////////////////////////////////////////////////////////////////////////////
            //-------------------------------paging--------------------------------//
            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;

            //////////////////////////////////////////////////////////////////////////////////////
            //------------------------------- Search ------------------------------------------------//
            IQueryable<Housing> housings;
            if (string.IsNullOrEmpty(searchWord))
            {
                housings = db.Housings.Where(s => s.IsDeleted == false).OrderBy(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.Housings.Where(c => c.IsDeleted == false).Count();
            }
            else
            {
                housings = db.Housings.Where(s => s.IsDeleted == false && (s.RoomNo.Contains(searchWord) || s.RoomContent.Contains(searchWord)||s.ArName.Contains(searchWord) || s.EnName.Contains(searchWord) || s.Code.Contains(searchWord))).OrderBy(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.Housings.Where(s => s.IsDeleted == false && (s.RoomNo.Contains(searchWord) || s.RoomContent.Contains(searchWord) || s.ArName.Contains(searchWord) || s.EnName.Contains(searchWord) || s.Code.Contains(searchWord))).OrderBy(s => s.Id).Count();
            }
            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            return View(housings.ToList());
        }

        //--------------------------------- Add Or Edit -----------------------------------//
        public ActionResult AddEdit(int? id)
        {
            if (id == null)
            {
                var Code = SetCodeNum();
                ViewBag.Code = int.Parse(Code.Data.ToString());
                return View();
            }
            Housing housing = db.Housings.Find(id);
            if (housing == null)
            {
                return HttpNotFound();
            }
            //QueryHelper.AddLog(new MyLog()
            //{
            //    ArAction = "اضافة او تعديل التسكين ",
            //    EnAction = "AddEdit",
            //    ControllerName = "Housing",
            //    UserName = User.Identity.Name,
            //    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
            //    LogDate = DateTime.Now,
            //    RequestMethod = "GET",
            //    SelectedItem = id
            //});
            ViewBag.Next = QueryHelper.Next((int)id, "Housing");
            ViewBag.Previous = QueryHelper.Previous((int)id, "Housing");
            ViewBag.Last = QueryHelper.GetLast("Housing");
            ViewBag.First = QueryHelper.GetFirst("Housing");
            return View(housing);
        }

        [HttpPost]
        public ActionResult AddEdit(Housing housing, string newBtn)
        {
            string domainName = Request.Url.GetLeftPart(UriPartial.Authority);
            if (ModelState.IsValid)
            {
                var id = housing.Id;
                housing.IsDeleted = false;
                housing.IsActive = true;
                housing.UserId = 1;//int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                if (housing.Id > 0)
                {
                    db.Entry(housing).State = EntityState.Modified;
                    ////-------------------- Notification for any edit -------------------------////
                    Notification.GetNotification("Housing", "Edit", "AddEdit", id, null, "التسكين");
                }
                else
                {
                    housing.Code = (QueryHelper.CodeLastNum("Housing") + 1).ToString();
                    db.Housings.Add(housing);
                    ////-------------------- Notification for adding new shift -------------------------////
                    Notification.GetNotification("Housing", "Add", "AddEdit", housing.Id, null, "التسكين");
                }

                try
                {
                    db.SaveChanges();
                }
                catch (Exception ex)
                {
                    var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .Select(x => new { x.Key, x.Value.Errors })
                    .ToArray();
                }
                //QueryHelper.AddLog(new MyLog()
                //{
                //    ArAction = id > 0 ? "تعديل التسكين" : "اضافة التسكين",
                //    EnAction = "AddEdit",
                //    ControllerName = "Housing",
                //    UserName = User.Identity.Name,
                //    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                //    LogDate = DateTime.Now,
                //    RequestMethod = "POST",
                //    SelectedItem = id,
                //    CodeOrDocNo = housing.Code
                //});

                if (newBtn == "saveAndNew")
                {
                    return RedirectToAction("AddEdit");
                }
                else
                {
                    return RedirectToAction("Index");
                }
            }
            return View(housing);
        }
        //------------------------------ Add Edit 2 -----------------------------------//
        public ActionResult AddEdit2()
        {
            ViewBag.Id = new SelectList(db.Housings.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName");
            return View();
        }

        [HttpPost]
        public ActionResult AddEdit2(Housing housing, string newBtn)
        {
            string domainName = Request.Url.GetLeftPart(UriPartial.Authority);
            if (ModelState.IsValid)
            {
                var id = housing.Id;
                housing.IsDeleted = false;
                housing.IsActive = true;
                housing.UserId = 1;// int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                if (housing.Id > 0)
                {
                    var OldHousing = db.Housings.AsNoTracking().Where(a=>a.Id==housing.Id).FirstOrDefault();
                    if(OldHousing!=null)
                    {
                        housing.Code = OldHousing.Code;
                        housing.ArName = OldHousing.ArName;
                        housing.EnName = OldHousing.EnName;
                        housing.RoomContent = OldHousing.RoomContent;
                        housing.RoomNo = OldHousing.RoomNo;
                        housing.IsConfirmed = OldHousing.IsConfirmed;
                    }
                    db.Entry(housing).State = EntityState.Modified;
                    ////-------------------- Notification for any edit -------------------------////
                    Notification.GetNotification("Housing", "Edit", "AddEdit", id, null, "التسكين");
                }
                else
                {
                    ViewBag.Id = new SelectList(db.Housings.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
                    {
                        b.Id,
                        ArName = b.Code + " - " + b.ArName
                    }), "Id", "ArName",housing.Id);
                    return View(housing);
                }
                try
                {
                    db.SaveChanges();
                }
                catch (Exception ex)
                {
                    var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .Select(x => new { x.Key, x.Value.Errors })
                    .ToArray();
                }
                //QueryHelper.AddLog(new MyLog()
                //{
                //    ArAction = id > 0 ? "تعديل التسكين" : "اضافة التسكين",
                //    EnAction = "AddEdit",
                //    ControllerName = "Housing",
                //    UserName = User.Identity.Name,
                //    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                //    LogDate = DateTime.Now,
                //    RequestMethod = "POST",
                //    SelectedItem = id,
                //    CodeOrDocNo = housing.Code
                //});

                if (newBtn == "saveAndNew")
                {
                    return RedirectToAction("AddEdit2");
                }
                else
                {
                    return RedirectToAction("AddEdit2/");
                }
            }
            ViewBag.Id = new SelectList(db.Housings.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName",housing.Id);
            return View(housing);
        }
        //----------------------------------------------------------------------------//

        [HttpPost, ActionName("Delete")]

        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                Housing housing = db.Housings.Find(id);
                housing.IsDeleted = true;
                housing.UserId = 1;// int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                db.Entry(housing).State = EntityState.Modified;
                db.SaveChanges();
                //QueryHelper.AddLog(new MyLog()
                //{
                //    ArAction = "حذف التسكين",
                //    EnAction = "AddEdit",
                //    ControllerName = "Housing",
                //    UserName = User.Identity.Name,
                //    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                //    LogDate = DateTime.Now,
                //    RequestMethod = "POST",
                //    SelectedItem = id
                //});
                ////-------------------- Notification-------------------------////
                Notification.GetNotification("Housing", "Delete", "Delete", id, null, "التسكين");
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
                Housing housing = db.Housings.Find(id);
                if (housing.IsActive == true)
                {
                    housing.IsActive = false;
                }
                else
                {
                    housing.IsActive = true;
                }
                housing.UserId = 1;// int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                db.Entry(housing).State = EntityState.Modified;

                db.SaveChanges();
                //QueryHelper.AddLog(new MyLog()
                //{
                //    ArAction = (bool)housing.IsActive ? "تعديل التسكين" : "اضافة التسكين",
                //    EnAction = "AddEdit",
                //    ControllerName = "Housing",
                //    UserName = User.Identity.Name,
                //    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                //    LogDate = DateTime.Now,
                //    RequestMethod = "POST",
                //    SelectedItem = housing.Id,
                //});
                ////-------------------- Notification-------------------------////
                if (housing.IsActive == true)
                {
                    Notification.GetNotification("Housing", "Activate/Deactivate", "ActivateDeactivate", id, true, "التسكين");
                }
                else
                {
                    Notification.GetNotification("Housing", "Activate/Deactivate", "ActivateDeactivate", id, false, "التسكين");
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
            var code = QueryHelper.CodeLastNum("Housing");
            return Json(code + 1, JsonRequestBehavior.AllowGet);
        }
        [SkipERPAuthorize]
        public JsonResult SearchHousingInformation(string NationalId)
        {
            var result = db.Housings.Where(a => a.IsActive == true && a.IsDeleted == false && a.NationalNo.Contains(NationalId)).Select(a => new
            {
                a.ArName,
                a.EnName,
                a.Code,
                a.Id,
                a.Mobile,
                a.RoomContent,
                a.RoomNo,
                a.IsConfirmed,
                a.NationalNo,
            }).FirstOrDefault();
            return Json(result, JsonRequestBehavior.AllowGet);
        }
        public ActionResult Information()
        {
            return View();
        }

        public JsonResult ChangeIsConfirmedState(int? Id, bool? IsConfirmed)
        {
            var housing = db.Housings.Find(Id);
            if (housing != null)
            {
                housing.IsConfirmed = IsConfirmed;
                db.Entry(housing).State = EntityState.Modified;
                db.SaveChanges();
                return Json(IsConfirmed=housing.IsConfirmed, JsonRequestBehavior.AllowGet);
            }
            return Json("Error", JsonRequestBehavior.AllowGet);
        }

        [SkipERPAuthorize]
        public JsonResult GetHousingDetails(int? Id)
        {
            var result = db.Housings.Where(a => a.IsActive == true && a.IsDeleted == false && a.Id==Id).Select(a => new
            {
                a.NationalNo,
                a.Mobile,
            }).FirstOrDefault();
            return Json(result, JsonRequestBehavior.AllowGet);
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