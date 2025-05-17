using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Web;
using System.Web.Mvc;
using MyERP.Models;
using System.Security.Claims;

namespace MyERP.Controllers.HR
{
    public class OvertimeTypeController : Controller
    {
        private readonly MySoftERPEntity db = new MySoftERPEntity();

        // GET: OvertimeType
        public async Task<ActionResult> Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة انواع الوقت الاضافي",
                EnAction = "Index",
                ControllerName = "OvertimeType",
                UserName = User.Identity.Name,
                UserId = userId,
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            ////-------------------- Notification-------------------------////
            Notification.GetNotification("OvertimeType", "View", "Index", null, null, "انواع الوقت الاضافي");
            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;
            IQueryable<OvertimeType> overtimeTypes;
            if (string.IsNullOrEmpty(searchWord))
            {
                overtimeTypes = db.OvertimeTypes.Where(c => c.IsDeleted == false).OrderBy(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.OvertimeTypes.Where(c => c.IsDeleted == false).Count();
            }
            else
            {
                overtimeTypes = db.OvertimeTypes.Where(s => s.IsDeleted == false && (s.Code.Contains(searchWord) || s.ArName.Contains(searchWord) || s.EnName.Contains(searchWord))).OrderBy(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.OvertimeTypes.Where(s => s.IsDeleted == false && (s.Code.Contains(searchWord) || s.ArName.Contains(searchWord) || s.EnName.Contains(searchWord))).Count();
            }

            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;

            return View(await overtimeTypes.ToListAsync());
        }

        // GET: OvertimeType/Edit/5
        public async Task<ActionResult> AddEdit(int? id)
        {
            if (id == null)
            {
                //ChartOfAcc
                ViewBag.ChartOfAccountId = new SelectList(db.ChartOfAccounts.Where(a => a.IsActive == true && a.IsDeleted == false && a.ClassificationId == 3).Select(a => new {
                    Id = a.Id,
                    ArName = a.Code + " - " + a.ArName
                }), "Id", "ArName");
                //SalaryItem
                ViewBag.SalaryItemId = new SelectList(db.SalaryItems.Where(a => a.IsActive == true && a.IsDeleted == false).Select(a => new {
                    Id = a.Id,
                    ArName = a.Code + " - " + a.ArName
                }), "Id", "ArName");
                //CalculationMethodId
                ViewBag.CalculationMethodId = new SelectList(new List<dynamic>
                {
                    new { Id=1, ArName="ساعة"},
                    new { Id = 2, ArName = "يوم" },
                    new { Id = 3, ArName = "قيمة" }
                }, "Id", "ArName");
                return View();
            }
            OvertimeType overtimeType = await db.OvertimeTypes.FindAsync(id);
            if (overtimeType == null)
            {
                return HttpNotFound();
            }
            //ChartOfAcc
            ViewBag.ChartOfAccountId = new SelectList(db.ChartOfAccounts.Where(a => a.IsActive == true && a.IsDeleted == false && a.ClassificationId == 3).Select(a => new {
                Id = a.Id,
                ArName = a.Code + " - " + a.ArName
            }), "Id", "ArName", overtimeType.ChartOfAccountId);
            //SalaryItem
            ViewBag.SalaryItemId = new SelectList(db.SalaryItems.Where(a => a.IsActive == true && a.IsDeleted == false).Select(a => new {
                Id = a.Id,
                ArName = a.Code + " - " + a.ArName
            }), "Id", "ArName");
            //CalculationMethodId
            ViewBag.CalculationMethodId = new SelectList(new List<dynamic>
                {
                    new { Id=1, ArName="ساعة"},
                    new { Id = 2, ArName = "يوم" },
                    new { Id = 3, ArName = "قيمة" }
                }, "Id", "ArName",overtimeType.CalculationMethodId);
            return View(overtimeType);
        }

        [HttpPost]
        //[ValidateAntiForgeryToken]
        public async Task<ActionResult> AddEdit(OvertimeType overtimeType/*, string newBtn*/)
        {
            if (ModelState.IsValid)
            {
                var id = overtimeType.Id;
                overtimeType.IsDeleted = false;
                if (overtimeType.Id > 0)
                {
                    db.OvertimeTypeDetails.RemoveRange(db.OvertimeTypeDetails.Where(x => x.MainDocId == overtimeType.Id));
                    var overtimeTypeDetials = overtimeType.OvertimeTypeDetails.ToList();
                    overtimeTypeDetials.ForEach((x) => x.MainDocId = overtimeType.Id);
                    overtimeType.OvertimeTypeDetails = null;
                    db.Entry(overtimeType).State = EntityState.Modified;
                    db.OvertimeTypeDetails.AddRange(overtimeTypeDetials);
                }
                else
                {
                    overtimeType.IsActive = true;
                    db.OvertimeTypes.Add(overtimeType);
                }
                await db.SaveChangesAsync();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = id > 0 ? "تعديل انواع الوقت الاضافي" : "اضافة انواع الوقت الاضافي",
                    EnAction = "AddEdit",
                    ControllerName = "OvertimeType",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = overtimeType.Id,
                    ArItemName = overtimeType.ArName,
                    EnItemName = overtimeType.EnName,
                    CodeOrDocNo = overtimeType.Code
                });
                Notification.GetNotification("OvertimeType", id > 0 ? "Edit" : "Add", "AddEdit", overtimeType.Id, null, "انواع الوقت الاضافي");
                //if (newBtn == "saveAndNew")
                //    return RedirectToAction("AddEdit");
                //else
                //    return RedirectToAction("Index");
                return Json(new { success = true });

            }
            //return View(overtimeType);
            var errors = ModelState
                  .Where(x => x.Value.Errors.Count > 0)
                  .Select(x => new { x.Key, x.Value.Errors })
                  .ToArray();
            return Json(new { success = false, errors });
        }

        // POST: OvertimeType/Delete/5
        [HttpPost, ActionName("Delete")]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            OvertimeType overtimeType = new OvertimeType() { Id = id, Code = "", ArName = "" };
            db.OvertimeTypes.Attach(overtimeType);
            overtimeType.IsDeleted = true;
            foreach (var detail in overtimeType.OvertimeTypeDetails)
            {
                detail.IsDeleted = true;
            }
            db.Entry(overtimeType).Property(x => x.IsDeleted).IsModified = true;

            await db.SaveChangesAsync();
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "حذف انواع الوقت الاضافي",
                EnAction = "AddEdit",
                ControllerName = "OvertimeType",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "POST",
                SelectedItem = id,
                EnItemName = overtimeType.EnName,
                ArItemName = overtimeType.ArName,
                CodeOrDocNo = overtimeType.Code
            });
            Notification.GetNotification("OvertimeType", "Delete", "Delete", id, null, "انواع الوقت الاضافي");
            return Content("true");
        }

        [HttpPost]
        public async Task<ActionResult> ActivateDeactivate(int id)
        {
            OvertimeType overtimeType = await db.OvertimeTypes.FindAsync(id);
            overtimeType.IsActive = !overtimeType.IsActive;
            db.Entry(overtimeType).State = EntityState.Modified;
            await db.SaveChangesAsync();
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = overtimeType.IsActive ? "تنشيط انواع الوقت الاضافي" : "إلغاء انواع الوقت الاضافي",
                EnAction = "AddEdit",
                ControllerName = "OvertimeType",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "POST",
                SelectedItem = overtimeType.Id,
                EnItemName = overtimeType.EnName,
                ArItemName = overtimeType.ArName,
                CodeOrDocNo = overtimeType.Code
            });
            Notification.GetNotification("OvertimeType", "Activate/Deactivate", "ActivateDeactivate", id, overtimeType.IsActive, "انواع الوقت الاضافي");
            return Content("true");
        }

        [SkipERPAuthorize]
        public JsonResult SetCodeNum()
        {
            var code = QueryHelper.CodeLastNum("OvertimeType");
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
