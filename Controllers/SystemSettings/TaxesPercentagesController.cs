using MyERP.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace MyERP.Controllers.SystemSettings
{
    public class TaxesPercentagesController : Controller
    {
        private readonly MySoftERPEntity db = new MySoftERPEntity();

        // GET: TaxesPercentage
        public async Task<ActionResult> Index(int pageIndex = 1, int wantedRowsNo = 10)
        {
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح أعدادات الضريبة",
                EnAction = "Index",
                ControllerName = "TaxesPercentages",
                UserName = User.Identity.Name,
                UserId = userId,
                LogDate = DateTime.Now,
                RequestMethod = "GET",
            });
            ////-------------------- Notification-------------------------////
            Notification.GetNotification("TaxesPercentages", "View", "Index", null, null, "إعدادات الضريبة");
            ///////-----------------------------------------------------------------------

            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;

            IQueryable<TaxesPercentage> taxesPercentages;

                taxesPercentages = db.TaxesPercentages.Where(c => c.IsDeleted == false).OrderBy(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.TaxesPercentages.Where(c => c.IsDeleted == false).Count();
            ViewBag.wantedRowsNo = wantedRowsNo;

            return View(await taxesPercentages.ToListAsync());
        }

        // GET: TaxesPercentage/Edit/5
        public async Task<ActionResult> AddEdit(int? id)
        {
            if (id == null)
            {
                DateTime utcNow = DateTime.UtcNow;
                TimeZoneInfo info = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");
                DateTime cTime = TimeZoneInfo.ConvertTimeFromUtc(utcNow, info);
                ViewBag.DateFrom= cTime.ToString("yyyy-MM-ddTHH:mm");
                ViewBag.DateTo= cTime.ToString("yyyy-MM-ddTHH:mm");
                return View();
            }
            TaxesPercentage  taxesPercentage = await db.TaxesPercentages.FindAsync(id);
            if (taxesPercentage == null)
            {
                return HttpNotFound();
            }
            ViewBag.DateFrom = taxesPercentage.DateFrom?.ToString("yyyy-MM-ddTHH:mm");
            ViewBag.DateTo = taxesPercentage.DateTo?.ToString("yyyy-MM-ddTHH:mm");
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح إعدادات الضريبة",
                EnAction = "AddEdit",
                ControllerName = "TaxesPercentages",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = id,
            });
            return View(taxesPercentage);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> AddEdit([Bind(Include = "Id,DateFrom,DateTo,TaxPercentage,IsDeleted,IsActive")] TaxesPercentage taxesPercentage, string newBtn)
        {
            if (ModelState.IsValid)
            {
                var id = taxesPercentage.Id;
                taxesPercentage.IsDeleted = false;
                if (taxesPercentage.Id > 0)
                    db.Entry(taxesPercentage).State = EntityState.Modified;
                else
                {
                    taxesPercentage.IsActive = true;
                    db.TaxesPercentages.Add(taxesPercentage);
                }
                await db.SaveChangesAsync();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = id > 0 ? "تعديل إعدادات الضريبة" : "اضافة  إعدادات الضريبة",
                    EnAction = "AddEdit",
                    ControllerName = "TaxesPercentages",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                });
                Notification.GetNotification("TaxesPercentages", id > 0 ? "Edit" : "Add", "AddEdit", taxesPercentage.Id, null, "إعدادات الضريبة ");
                if (newBtn == "saveAndNew")
                    return RedirectToAction("AddEdit");
                else
                    return RedirectToAction("Index");
            }
            return View(taxesPercentage);
        }

        // POST: TaxesPercentage/Delete/5
        [HttpPost, ActionName("Delete")]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            TaxesPercentage  taxesPercentage = new TaxesPercentage() { Id = id };
            db.TaxesPercentages.Attach(taxesPercentage);
            taxesPercentage.IsDeleted = true;
            db.Entry(taxesPercentage).Property(x => x.IsDeleted).IsModified = true;

            await db.SaveChangesAsync();
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "حذف إعدادات الضريبة ",
                EnAction = "AddEdit",
                ControllerName = "TaxesPercentages",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "POST",
                SelectedItem = id,
            });
            Notification.GetNotification("TaxesPercentages", "Delete", "Delete", id, null, "إعدادات الضريبة");
            return Content("true");
        }

        [HttpPost]
        public async Task<ActionResult> ActivateDeactivate(int id)
        {
            TaxesPercentage taxesPercentage = await db.TaxesPercentages.FindAsync(id);
            taxesPercentage.IsActive = !taxesPercentage.IsActive;
            db.Entry(taxesPercentage).State = EntityState.Modified;
            await db.SaveChangesAsync();
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = taxesPercentage.IsActive ? "تنشيط إعدادات الضريبة " : "إلغاء إعدادات الضريبة ",
                EnAction = "AddEdit",
                ControllerName = "TaxesPercentages",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "POST",
                SelectedItem = taxesPercentage.Id,

            });
            Notification.GetNotification("TaxesPercentages", "Activate/Deactivate", "ActivateDeactivate", id, taxesPercentage.IsActive, "إعدادات الضريبة ");
            return Content("true");
        }

        [SkipERPAuthorize]
        public JsonResult SetCodeNum()
        {
            var code = QueryHelper.CodeLastNum("TaxesPercentage");
            return Json(code + 1, JsonRequestBehavior.AllowGet);
        }

        [SkipERPAuthorize]
        public JsonResult GetCurrentTaxPercentage(DateTime date)
        {
            double? perc=14;
            var taxPercentage = db.TaxesPercentages.Where(t => t.IsActive == true && t.IsDeleted == false && t.DateFrom <= date && t.DateTo >= date).FirstOrDefault();
            if (taxPercentage != null)
                perc= taxPercentage.TaxPercentage;
            return Json(perc, JsonRequestBehavior.AllowGet);
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
