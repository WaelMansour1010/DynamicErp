using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using MyERP.Models;
using System.IO;
using System.Security.Claims;

namespace MyERP.Controllers
{
    public class ChurchController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        // GET: Church
        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة الكنائس",
                EnAction = "Index",
                ControllerName = "Church",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            ////-------------------- Notification-------------------------////
            Notification.GetNotification("Church", "View", "Index", null, null, "الكنائس");
            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;

            IQueryable<Church> churches;

            if (string.IsNullOrEmpty(searchWord))
            {
                churches = db.Churches.Where(s => s.IsDeleted == false).OrderBy(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.Churches.Where(s => s.IsDeleted == false).Count();
            }
            else
            {
                churches = db.Churches.Where(s => s.IsDeleted == false && (s.Code.Contains(searchWord) || s.ArName.Contains(searchWord) || s.EnName.Contains(searchWord) || s.ERPUser.Name.Contains(searchWord) || s.Notes.Contains(searchWord))).OrderBy(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.Churches.Where(s => s.IsDeleted == false && (s.Code.Contains(searchWord) || s.ArName.Contains(searchWord) || s.EnName.Contains(searchWord) || s.ERPUser.Name.Contains(searchWord) || s.Notes.Contains(searchWord))).Count();
            }
            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            return View(churches.ToList());

        }

        public ActionResult AddEdit(int? id)
        {

            if (id == null)
            {
                Church NewObj = new Church();

                return View(NewObj);
            }
            Church church = db.Churches.Find(id);
            if (church == null)
            {
                return HttpNotFound();
            }
            ViewBag.Next = QueryHelper.Next((int)id, "Church");
            ViewBag.Previous = QueryHelper.Previous((int)id, "Church");
            ViewBag.Last = QueryHelper.GetLast("Church");
            ViewBag.First = QueryHelper.GetFirst("Church");
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح تفاصيل الكنائس",
                EnAction = "AddEdit",
                ControllerName = "Church",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = church.Id,
                ArItemName = church.ArName,
                EnItemName = church.EnName,
                CodeOrDocNo = church.Code
            });
            return View(church);
        }


        [HttpPost]
        public ActionResult AddEdit(Church church, string newBtn)
        {
            if (ModelState.IsValid)
            {
                var id = church.Id;
                church.IsDeleted = false;
                if (church.Id > 0)
                {
                    church.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                    db.Entry(church).State = EntityState.Modified;

                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("Church", "Edit", "AddEdit", id, null, "الكنائس");


                }
                else
                {
                    church.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    church.Code = (QueryHelper.CodeLastNum("Church") + 1).ToString();
                    church.IsActive = true;
                    db.Churches.Add(church);

                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("Church", "Add", "AddEdit", church.Id, null, "الكنائس");

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
                    //var mod = ModelState.First(c => c.Key == "Code");  // this
                    //mod.Value.Errors.Add("هذا الكود موجود من قبل");
                    return View(church);
                }
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = id > 0 ? "تعديل الكنائس" : "اضافة الكنائس",
                    EnAction = "AddEdit",
                    ControllerName = "Church",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = church.Id,
                    ArItemName = church.ArName,
                    EnItemName = church.EnName,
                    CodeOrDocNo = church.Code
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

            return View(church);
        }

        [HttpPost]
        public ActionResult ActivateDeactivate(int id)
        {
            try
            {
                Church church = db.Churches.Find(id);
                if (church.IsActive == true)
                {
                    church.IsActive = false;
                }
                else
                {
                    church.IsActive = true;
                }
                church.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                db.Entry(church).State = EntityState.Modified;

                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = (bool)church.IsActive ? "تنشيط الكنائس" : "إلغاء الكنائس",
                    EnAction = "AddEdit",
                    ControllerName = "Church",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = church.Id,
                    EnItemName = church.EnName,
                    ArItemName = church.ArName,
                    CodeOrDocNo = church.Code
                });
                ////-------------------- Notification-------------------------////
                if (church.IsActive == true)
                {
                    Notification.GetNotification("Church", "Activate/Deactivate", "ActivateDeactivate", id, true, "الكنائس");
                }
                else
                {

                    Notification.GetNotification("Church", "Activate/Deactivate", "ActivateDeactivate", id, false, "الكنائس");
                }


                return Content("true");
            }
            catch (Exception ex)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
        }

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                Church church = db.Churches.Find(id);
                church.IsDeleted = true;
                church.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                db.Entry(church).State = EntityState.Modified;
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "حذف الكنائس",
                    EnAction = "AddEdit",
                    ControllerName = "Church",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    EnItemName = church.EnName,
                    ArItemName = church.ArName,
                    CodeOrDocNo = church.Code
                });
                ////-------------------- Notification-------------------------////
                Notification.GetNotification("Church", "Delete", "Delete", id, null, "الكنائس");



                return Content("true");
            }
            catch (Exception)
            {

                throw;
            }
        }

        [SkipERPAuthorize]
        public JsonResult SetCodeNum()
        {
            var code = QueryHelper.CodeLastNum("Church");
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