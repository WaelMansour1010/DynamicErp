using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;
using MyERP.Models;

namespace MyERP.Controllers
{
    public class ShareHolderController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        // GET: ShareHolder
        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "قائمة المساهمين",
                EnAction = "Index",
                ControllerName = "ShareHolder",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            Notification.GetNotification("ShareHolder", "View", "Index", null, null, "المساهمين");
            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;

            IQueryable<ShareHolder> shareHolders;
            if (string.IsNullOrEmpty(searchWord))
            {
                shareHolders = db.ShareHolders.Where(s => s.IsDeleted == false).OrderBy(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.ShareHolders.Where(s => s.IsDeleted == false).Count();
            }
            else
            {
                shareHolders = db.ShareHolders.Where(s => s.IsDeleted == false && (s.Code.Contains(searchWord) || s.ArName.Contains(searchWord) || s.EnName.Contains(searchWord) || s.Notes.Contains(searchWord))).OrderBy(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.ShareHolders.Where(s => s.IsDeleted == false && (s.Code.Contains(searchWord) || s.ArName.Contains(searchWord) || s.EnName.Contains(searchWord)  || s.Notes.Contains(searchWord))).Count();

            }
            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            return View(shareHolders.ToList());
        }

        // GET: ShareHolder/Edit/5
        public ActionResult AddEdit(int? id)
        {
            if (id == null)
            {
                ViewBag.AccountId = new SelectList(db.ChartOfAccounts.Where(x => x.IsActive == true && x.IsDeleted == false && (x.ClassificationId == 2 || x.ClassificationId == 3)).Select(x => new { x.Id, ArName = x.Code + " - " + x.ArName }), "Id", "ArName");
                return View();
            }
            ShareHolder shareHolder = db.ShareHolders.Find(id);
            if (shareHolder == null)
            {
                return HttpNotFound();
            }
            ViewBag.AccountId = new SelectList(db.ChartOfAccounts.Where(x => x.IsActive == true && x.IsDeleted == false && (x.ClassificationId == 2 || x.ClassificationId == 3)).Select(x => new {x.Id, ArName=x.Code+" - "+x.ArName }), "Id", "ArName", shareHolder.AccountId);
            return View(shareHolder);
        }

        // POST: ShareHolder/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddEdit([Bind(Include = "Id,Code,ArName,EnName,PhoneNumber,Email,Address,NationalId,AccountId,Notes,IsActive,IsDeleted")] ShareHolder shareHolder, string newBtn)
        {
            if (ModelState.IsValid)
            {
                var id = shareHolder.Id;
                shareHolder.IsDeleted =false;
                if (shareHolder.Id>0)
                {
                    db.Entry(shareHolder).State = EntityState.Modified;
                }
                else
                {
                    shareHolder.Code= (QueryHelper.CodeLastNum("ShareHolder") + 1).ToString();
                    shareHolder.IsActive = true;
                    db.ShareHolders.Add(shareHolder);
                }
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = id > 0 ? "تعديل المساهمين" : "اضافة المساهمين",
                    EnAction = "AddEdit",
                    ControllerName = "ShareHolder",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = shareHolder.Id,
                    ArItemName = shareHolder.ArName,
                    EnItemName = shareHolder.EnName,
                    CodeOrDocNo = shareHolder.Code
                });
                db.SaveChanges();
                if (newBtn == "saveAndNew")
                    return RedirectToAction("AddEdit");
                return RedirectToAction("Index");
            }
            ViewBag.AccountId = new SelectList(db.ChartOfAccounts.Where(x => x.IsActive == true && x.IsDeleted == false && (x.ClassificationId == 2 || x.ClassificationId == 3)).Select(x => new { x.Id, ArName = x.Code + " - " + x.ArName }), "Id", "ArName", shareHolder.AccountId);
            return View(shareHolder);
        }

        // POST: ShareHolder/Delete/5
        [HttpPost, ActionName("Delete")]

        public ActionResult DeleteConfirmed(int id)
        {
            ShareHolder shareHolder = db.ShareHolders.Find(id);
            shareHolder.IsDeleted = true;
            db.Entry(shareHolder).State = EntityState.Modified;
            db.SaveChanges();
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "حذف الفرع",
                EnAction = "AddEdit",
                ControllerName = "Departments",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "POST",
                SelectedItem = id,
                EnItemName = shareHolder.EnName,
                ArItemName = shareHolder.ArName,
                CodeOrDocNo = shareHolder.Code
            });
            Notification.GetNotification("ShareHolder", "Delete", "Delete", id, null, "المساهمين");

            return Content("true");
        }

        [HttpPost]
        public ActionResult ActivateDeactivate(int id)
        {
            try
            {
                ShareHolder shareHolder = db.ShareHolders.Find(id);
                shareHolder.IsActive = shareHolder.IsActive ? false : true;

                db.Entry(shareHolder).State = EntityState.Modified;

                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = (bool)shareHolder.IsActive ? "تنشيط المساهمين" : "إلغاء المساهمين",
                    EnAction = "AddEdit",
                    ControllerName = "ShareHolder",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = shareHolder.Id,
                    EnItemName = shareHolder.EnName,
                    ArItemName = shareHolder.ArName,
                    CodeOrDocNo = shareHolder.Code
                });
                ////-------------------- Notification-------------------------////
                
                    Notification.GetNotification("ShareHolder", "Activate/Deactivate", "ActivateDeactivate", id, shareHolder.IsActive, "المساهمين");
               
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
            var code = QueryHelper.CodeLastNum("ShareHolder");
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
