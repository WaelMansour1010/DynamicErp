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
    public class NationalityController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();
        // GET: Nationality
        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "قائمة الجنسيات",
                EnAction = "Index",
                ControllerName = "Nationality",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });

            //--------------------- Notification --------------------//
            Notification.GetNotification("Nationality", "View", "Index", null, null, "الجنسيات");

            //////////////////////////////////////////////////////////////////////////////////////
            //-------------------------------paging--------------------------------//
            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;

            //////////////////////////////////////////////////////////////////////////////////////
            //------------------------------- Search ------------------------------------------------//
            IQueryable<Nationality> Nationality;
            if (string.IsNullOrEmpty(searchWord))
            {
                Nationality = db.Nationalities.Where(s => s.IsDeleted == false).OrderBy(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.Nationalities.Where(c => c.IsDeleted == false).Count();
            }
            else
            {
                Nationality = db.Nationalities.Where(s => s.IsDeleted == false && (s.ArName.Contains(searchWord) || s.EnName.Contains(searchWord) || s.Code.Contains(searchWord))).OrderBy(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.Nationalities.Where(s => s.IsDeleted == false && (s.ArName.Contains(searchWord) || s.EnName.Contains(searchWord) || s.Code.Contains(searchWord))).OrderBy(s => s.Id).Count();
            }
            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            return View(Nationality.ToList());
        }

        //////////////////////////////////////////////////////////////////////////////////////
        //--------------------------- Authorization --------------------------------//
        [SkipERPAuthorize]
        public JsonResult SetCodeNum()
        {
            var code = QueryHelper.CodeLastNum("Nationality");
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
            Nationality nationality = db.Nationalities.Find(id);

            if (nationality == null)
            {
                return HttpNotFound();
            }

            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "اضافة او تعديل جنسية ",
                EnAction = "AddEdit",
                ControllerName = "Nationality",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = id
            });



            ViewBag.Next = QueryHelper.Next((int)id, "Nationality");
            ViewBag.Previous = QueryHelper.Previous((int)id, "Nationality");
            ViewBag.Last = QueryHelper.GetLast("Nationality");
            ViewBag.First = QueryHelper.GetFirst("Nationality");


            return View(nationality);
        }

        [HttpPost]
        public ActionResult AddEdit(Nationality nationality, string newBtn)
        {
            if (ModelState.IsValid)
            {
                //--------------------------- Edit if there is id ------------------------//
                var id = nationality.Id;
                nationality.IsDeleted = false;
                if (nationality.Id > 0)
                {
                    nationality.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                    db.Entry(nationality).State = EntityState.Modified;

                    ////-------------------- Notification for any edit -------------------------////
                    Notification.GetNotification("Nationality", "Edit", "AddEdit", id, null, "الجنسيات");
                }
                else
                {
                    nationality.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    nationality.Code = (QueryHelper.CodeLastNum("Nationality") + 1).ToString();
                    nationality.IsActive = true;
                    db.Nationalities.Add(nationality);


                    ////-------------------- Notification for adding new shift -------------------------////
                    Notification.GetNotification("Nationality", "Add", "AddEdit", nationality.Id, null, "الجنسيات");
                }

                try
                {
                    db.SaveChanges();
                }
                catch (Exception ex)
                {

                    var mod = ModelState.First(c => c.Key == "Code");  // this
                    mod.Value.Errors.Add("هذا الكود موجود من قبل");
                    return View(nationality);
                }
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = id > 0 ? "تعديل جنسية" : "اضافة جنسية",
                    EnAction = "AddEdit",
                    ControllerName = "Nationality",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    CodeOrDocNo = nationality.Code

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

            return View(nationality);
        }

        [HttpPost, ActionName("Delete")]

        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                Nationality nationality = db.Nationalities.Find(id);
                nationality.IsDeleted = true;
                nationality.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                db.Entry(nationality).State = EntityState.Modified;
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "حذف الجنسية",
                    EnAction = "AddEdit",
                    ControllerName = "Nationality",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id
                });
                ////-------------------- Notification-------------------------////
                Notification.GetNotification("Nationality", "Delete", "Delete", id, null, "الجنسيات");
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
                Nationality nationality = db.Nationalities.Find(id);
                if (nationality.IsActive == true)
                {
                    nationality.IsActive = false;
                }
                else
                {
                    nationality.IsActive = true;
                }
                nationality.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                db.Entry(nationality).State = EntityState.Modified;

                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = (bool)nationality.IsActive ? "تعديل جنسية" : "اضافة جنسية",
                    EnAction = "AddEdit",
                    ControllerName = "Nationality",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = nationality.Id,
                });
                ////-------------------- Notification-------------------------////
                if (nationality.IsActive == true)
                {

                    Notification.GetNotification("Nationality", "Activate/Deactivate", "ActivateDeactivate", id, true, "الجنسيات");
                }
                else
                {

                    Notification.GetNotification("Nationality", "Activate/Deactivate", "ActivateDeactivate", id, false, "الجنسيات");
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