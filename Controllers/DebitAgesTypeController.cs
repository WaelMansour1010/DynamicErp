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

namespace MyERP.Controllers
{
    public class DebitAgesTypeController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();
        // GET: DebitAgesType
        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة أنواع أعمار الديون",
                EnAction = "Index",
                ControllerName = "DebitAgesType",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            Notification.GetNotification("DebitAgesType", "View", "Index", null, null, "أنواع أعمار الديون");
            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;
            if (pageIndex > 1)
            {
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;
            }
            IQueryable<DebitAgesType> debitAgesTypes;
            if (string.IsNullOrEmpty(searchWord))
            {
                debitAgesTypes = db.DebitAgesTypes.Where(a => a.IsDeleted == false).OrderBy(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.DebitAgesTypes.Where(a => a.IsDeleted == false).Count();
            }
            else
            {
                debitAgesTypes = db.DebitAgesTypes.Where(a => a.IsDeleted == false &&
                (a.ArName.Contains(searchWord) || a.EnName.Contains(searchWord) || a.Code.Contains(searchWord)))
                    .OrderBy(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.DebitAgesTypes.Where(a => a.IsDeleted == false && (a.ArName.Contains(searchWord) || a.EnName.Contains(searchWord) || a.Code.Contains(searchWord))).Count();
            }
            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            return View(debitAgesTypes.ToList());
        }
        public ActionResult AddEdit(int? id)
        {
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            if (id == null)
            {
                var Code = SetCodeNum();
                ViewBag.Code = int.Parse(Code.Data.ToString());
                return View();
            }
            DebitAgesType debitAgesType = db.DebitAgesTypes.Find(id);
            if (debitAgesType == null)
            {
                return HttpNotFound();
            }
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "اضافة او تعديل أنواع أعمار الديون ",
                EnAction = "AddEdit",
                ControllerName = "DebitAgesType",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = id
            });
            ViewBag.Next = QueryHelper.Next((int)id, "DebitAgesType");
            ViewBag.Previous = QueryHelper.Previous((int)id, "DebitAgesType");
            ViewBag.Last = QueryHelper.GetLast("DebitAgesType");
            ViewBag.First = QueryHelper.GetFirst("DebitAgesType");
            return View(debitAgesType);
        }
        [HttpPost]
        public ActionResult AddEdit(DebitAgesType debitAgesType, string newBtn)
        {
            if (ModelState.IsValid)
            {
                var id = debitAgesType.Id;
                debitAgesType.IsDeleted = false;
                if (debitAgesType.Id > 0)
                {
                    debitAgesType.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    db.Entry(debitAgesType).State = EntityState.Modified;
                    Notification.GetNotification("DebitAgesType", "Edit", "AddEdit", debitAgesType.Id, null, "أنواع أعمار الديون");
                }
                else
                {
                    debitAgesType.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    debitAgesType.Code = (QueryHelper.CodeLastNum("DebitAgesType") + 1).ToString();
                    debitAgesType.IsActive = true;
                    db.DebitAgesTypes.Add(debitAgesType);
                    Notification.GetNotification("DebitAgesType", "Add", "AddEdit", debitAgesType.Id, null, "أنواع أعمار الديون");
                }
                try
                {
                    db.SaveChanges();
                }
                catch (Exception ex)
                {
                    var mod = ModelState.First(c => c.Key == "Code");  // this
                    mod.Value.Errors.Add("هذا الكود موجود من قبل");
                    return View(debitAgesType);
                }
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = id > 0 ? "تعديل أنواع أعمار الديون" : "اضافة أنواع أعمار الديون",
                    EnAction = "AddEdit",
                    ControllerName = "DebitAgesType",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    CodeOrDocNo = debitAgesType.Code
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
            return View(debitAgesType);
        }
        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                DebitAgesType debitAgesType = db.DebitAgesTypes.Find(id);
                debitAgesType.IsDeleted = true;
                debitAgesType.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                db.Entry(debitAgesType).State = EntityState.Modified;
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "حذف أنواع أعمار الديون",
                    EnAction = "AddEdit",
                    ControllerName = "DebitAgesType",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    EnItemName = debitAgesType.EnName
                });
                Notification.GetNotification("DebitAgesType", "Delete", "Delete", id, null, "أنواع أعمار الديون");
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
                DebitAgesType debitAgesType = db.DebitAgesTypes.Find(id);
                if (debitAgesType.IsActive == true)
                {
                    debitAgesType.IsActive = false;
                }
                else
                {
                    debitAgesType.IsActive = true;
                }
                debitAgesType.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                db.Entry(debitAgesType).State = EntityState.Modified;
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = (bool)debitAgesType.IsActive ? "تنشيط أنواع أعمار الديون" : "إلغاء تنشيط أنواع أعمار الديون",
                    EnAction = "AddEdit",
                    ControllerName = "DebitAgesType",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = debitAgesType.Id,
                    EnItemName = debitAgesType.EnName,
                    ArItemName = debitAgesType.ArName,
                    CodeOrDocNo = debitAgesType.Code
                });
                if (debitAgesType.IsActive == true)
                {
                    Notification.GetNotification("DebitAgesType", "Activate/Deactivate", "ActivateDeactivate", id, true, "أنواع أعمار الديون");
                }
                else
                {
                    Notification.GetNotification("DebitAgesType", "Activate/Deactivate", "ActivateDeactivate", id, false, " أنواع أعمار الديون");
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
            var code = QueryHelper.CodeLastNum("DebitAgesType");
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