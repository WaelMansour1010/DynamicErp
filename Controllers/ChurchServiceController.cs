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
    public class ChurchServiceController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        // GET: ChurchService
        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة الخدمات الكنسية",
                EnAction = "Index",
                ControllerName = "ChurchService",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            ////-------------------- Notification-------------------------////
            Notification.GetNotification("ChurchService", "View", "Index", null, null, "الخدمات الكنسية");
            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;

            IQueryable<ChurchService> churchServices;

            if (string.IsNullOrEmpty(searchWord))
            {
                churchServices = db.ChurchServices.Where(s => s.IsDeleted == false).OrderBy(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.ChurchServices.Where(s => s.IsDeleted == false).Count();
            }
            else
            {
                churchServices = db.ChurchServices.Where(s => s.IsDeleted == false && (s.Code.Contains(searchWord) || s.ArName.Contains(searchWord) || s.EnName.Contains(searchWord) || s.ERPUser.Name.Contains(searchWord) || s.Notes.Contains(searchWord))).OrderBy(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.ChurchServices.Where(s => s.IsDeleted == false && (s.Code.Contains(searchWord) || s.ArName.Contains(searchWord) || s.EnName.Contains(searchWord) || s.ERPUser.Name.Contains(searchWord) || s.Notes.Contains(searchWord))).Count();
            }
            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            return View(churchServices.ToList());

        }

        public ActionResult AddEdit(int? id)
        {
            if (id == null)
            {

                ViewBag.ServiceTrusteeId = new SelectList(db.Children.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");

                ViewBag.ParentId = new SelectList(db.ChurchServices.Where(a => a.IsActive == true && a.IsDeleted == false && a.ParentId == null).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");

                ViewBag.ServiceTypeId = new SelectList(new List<dynamic> {
                new { Id=1,ArName="رئيسي"},
                new { Id=2,ArName="فرعي"}}, "Id", "ArName");
                return View();
            }
            ChurchService churchService = db.ChurchServices.Find(id);
            if (churchService == null)
            {
                return HttpNotFound();
            }
            ViewBag.Next = QueryHelper.Next((int)id, "ChurchService");
            ViewBag.Previous = QueryHelper.Previous((int)id, "ChurchService");
            ViewBag.Last = QueryHelper.GetLast("ChurchService");
            ViewBag.First = QueryHelper.GetFirst("ChurchService");

            ViewBag.ServiceTrusteeId = new SelectList(db.Children.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName",churchService.ServiceTrusteeId);

            ViewBag.ParentId = new SelectList(db.ChurchServices.Where(a => a.IsActive == true && a.IsDeleted == false && a.ParentId == null).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", churchService.ParentId);

            ViewBag.ServiceTypeId = new SelectList(new List<dynamic> {
                new { Id=1,ArName="رئيسي"},
                new { Id=2,ArName="فرعي"}}, "Id", "ArName", churchService.ServiceTypeId);

            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح تفاصيل الخدمات الكنسية",
                EnAction = "AddEdit",
                ControllerName = "ChurchService",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = churchService.Id,
                ArItemName = churchService.ArName,
                EnItemName = churchService.EnName,
                CodeOrDocNo = churchService.Code
            });
            return View(churchService);
        }


        [HttpPost]
        public ActionResult AddEdit(ChurchService churchService, string newBtn)
        {
            if (ModelState.IsValid)
            {
                var id = churchService.Id;
                churchService.IsDeleted = false;
                churchService.IsActive = true;
                churchService.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                churchService.ParentId = churchService.ServiceTypeId == 1 ? null : churchService.ParentId;
                if (churchService.Id > 0)
                {

                    db.Entry(churchService).State = EntityState.Modified;

                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("ChurchService", "Edit", "AddEdit", id, null, "الخدمات الكنسية");
                }
                else
                {
                    churchService.Code = (QueryHelper.CodeLastNum("ChurchService") + 1).ToString();
                    db.ChurchServices.Add(churchService);
                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("ChurchService", "Add", "AddEdit", churchService.Id, null, "الخدمات الكنسية");

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
                   
                    return View(churchService);
                }
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = id > 0 ? "تعديل الخدمات الكنسية" : "اضافة الخدمات الكنسية",
                    EnAction = "AddEdit",
                    ControllerName = "ChurchService",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = churchService.Id,
                    ArItemName = churchService.ArName,
                    EnItemName = churchService.EnName,
                    CodeOrDocNo = churchService.Code
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
            
            ViewBag.ServiceTrusteeId = new SelectList(db.Children.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName",churchService.ServiceTrusteeId);

            ViewBag.ParentId = new SelectList(db.ChurchServices.Where(a => a.IsActive == true && a.IsDeleted == false&&a.ParentId==null).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", churchService.ParentId);

            ViewBag.ServiceTypeId = new SelectList(new List<dynamic> {
                new { Id=1,ArName="رئيسي"},
                new { Id=2,ArName="فرعي"}}, "Id", "ArName", churchService.ServiceTypeId);
            return View(churchService);
        }

        [HttpPost]
        public ActionResult ActivateDeactivate(int id)
        {
            try
            {
                ChurchService churchService = db.ChurchServices.Find(id);
                if (churchService.IsActive == true)
                {
                    churchService.IsActive = false;
                }
                else
                {
                    churchService.IsActive = true;
                }
                churchService.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                db.Entry(churchService).State = EntityState.Modified;

                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = (bool)churchService.IsActive ? "تنشيط الخدمات الكنسية" : "إلغاء الخدمات الكنسية",
                    EnAction = "AddEdit",
                    ControllerName = "ChurchService",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = churchService.Id,
                    EnItemName = churchService.EnName,
                    ArItemName = churchService.ArName,
                    CodeOrDocNo = churchService.Code
                });
                ////-------------------- Notification-------------------------////
                if (churchService.IsActive == true)
                {
                    Notification.GetNotification("ChurchService", "Activate/Deactivate", "ActivateDeactivate", id, true, "الخدمات الكنسية");
                }
                else
                {

                    Notification.GetNotification("ChurchService", "Activate/Deactivate", "ActivateDeactivate", id, false, "الخدمات الكنسية");
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
                ChurchService churchService = db.ChurchServices.Find(id);
                churchService.IsDeleted = true;
                churchService.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                db.Entry(churchService).State = EntityState.Modified;
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "حذف الخدمات الكنسية",
                    EnAction = "AddEdit",
                    ControllerName = "ChurchService",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    EnItemName = churchService.EnName,
                    ArItemName = churchService.ArName,
                    CodeOrDocNo = churchService.Code
                });
                ////-------------------- Notification-------------------------////
                Notification.GetNotification("ChurchService", "Delete", "Delete", id, null, "الخدمات الكنسية");



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
            var code = QueryHelper.CodeLastNum("ChurchService");
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