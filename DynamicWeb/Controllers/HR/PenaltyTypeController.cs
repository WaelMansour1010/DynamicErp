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
    public class PenaltyTypeController : Controller
    {
        private readonly MySoftERPEntity db = new MySoftERPEntity();

        // GET: PenaltyType
        public async Task<ActionResult> Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة انواع الجزاءات",
                EnAction = "Index",
                ControllerName = "PenaltyType",
                UserName = User.Identity.Name,
                UserId = userId,
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            ////-------------------- Notification-------------------------////
            Notification.GetNotification("PenaltyType", "View", "Index", null, null, "انواع الجزاءات");

            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;
            IQueryable<PenaltyType> penaltyTypes;
            if (string.IsNullOrEmpty(searchWord))
            {
                penaltyTypes = db.PenaltyTypes.Where(c => c.IsDeleted == false).OrderBy(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.PenaltyTypes.Where(c => c.IsDeleted == false).Count();
            }
            else
            {
                penaltyTypes = db.PenaltyTypes.Where(s => s.IsDeleted == false && (s.Code.Contains(searchWord) || s.ArName.Contains(searchWord) || s.EnName.Contains(searchWord))).OrderBy(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.PenaltyTypes.Where(s => s.IsDeleted == false && (s.Code.Contains(searchWord) || s.ArName.Contains(searchWord) || s.EnName.Contains(searchWord))).Count();
            }

            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;

            return View(await penaltyTypes.ToListAsync());
        }

        // GET: PenaltyType/Edit/5
        public async Task<ActionResult> AddEdit(int? id)
        {
            if (id == null)
            {
                //ChartOfAcc
                ViewBag.ChartOfAccountId = new SelectList(db.ChartOfAccounts.Where(a => a.IsActive == true && a.IsDeleted == false && a.ClassificationId == 3).Select(a => new
                {
                    Id = a.Id,
                    ArName = a.Code + " - " + a.ArName
                }), "Id", "ArName");
                //SalaryItem
                ViewBag.SalaryItemId = new SelectList(db.SalaryItems.Where(a => a.IsActive == true && a.IsDeleted == false).Select(a => new
                {
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
            PenaltyType penaltyType = await db.PenaltyTypes.FindAsync(id);
            if (penaltyType == null)
            {
                return HttpNotFound();
            }
            //ChartOfAcc
            ViewBag.ChartOfAccountId = new SelectList(db.ChartOfAccounts.Where(a => a.IsActive == true && a.IsDeleted == false && a.ClassificationId == 3).Select(a => new
            {
                Id = a.Id,
                ArName = a.Code + " - " + a.ArName
            }), "Id", "ArName", penaltyType.ChartOfAccountId);
            //SalaryItem
            ViewBag.SalaryItemId = new SelectList(db.SalaryItems.Where(a => a.IsActive == true && a.IsDeleted == false).Select(a => new
            {
                Id = a.Id,
                ArName = a.Code + " - " + a.ArName
            }), "Id", "ArName");
            //CalculationMethodId
            ViewBag.CalculationMethodId = new SelectList(new List<dynamic>
                {
                    new { Id=1, ArName="ساعة"},
                    new { Id = 2, ArName = "يوم" },
                    new { Id = 3, ArName = "قيمة" }
                }, "Id", "ArName", penaltyType.CalculationMethodId);
            return View(penaltyType);
        }

        [HttpPost]
        // [ValidateAntiForgeryToken]
        public async Task<ActionResult> AddEdit(PenaltyType penaltyType/*, string newBtn*/)
        {
            if (ModelState.IsValid)
            {
                var id = penaltyType.Id;
                penaltyType.IsDeleted = false;
                if (penaltyType.Id > 0)
                {
                    db.PenaltyTypeDetails.RemoveRange(db.PenaltyTypeDetails.Where(x => x.MainDocId == penaltyType.Id));
                    var penaltyTypeDetials = penaltyType.PenaltyTypeDetails.ToList();
                    penaltyTypeDetials.ForEach((x) => x.MainDocId = penaltyType.Id);
                    penaltyType.PenaltyTypeDetails = null;
                    db.Entry(penaltyType).State = EntityState.Modified;
                    db.PenaltyTypeDetails.AddRange(penaltyTypeDetials);
                }
                else
                {
                    penaltyType.IsActive = true;
                    db.PenaltyTypes.Add(penaltyType);
                }
                await db.SaveChangesAsync();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = id > 0 ? "تعديل انواع الجزاءات" : "اضافة انواع الجزاءات",
                    EnAction = "AddEdit",
                    ControllerName = "PenaltyType",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = penaltyType.Id,
                    ArItemName = penaltyType.ArName,
                    EnItemName = penaltyType.EnName,
                    CodeOrDocNo = penaltyType.Code
                });
                Notification.GetNotification("PenaltyType", id > 0 ? "Edit" : "Add", "AddEdit", penaltyType.Id, null, "انواع الجزاءات");
                //if (newBtn == "saveAndNew")
                //    return RedirectToAction("AddEdit");
                //else
                //    return RedirectToAction("Index");
                return Json(new { success = true });

            }
            var errors = ModelState
                  .Where(x => x.Value.Errors.Count > 0)
                  .Select(x => new { x.Key, x.Value.Errors })
                  .ToArray();
            return Json(new { success = false, errors });
        }


        // POST: PenaltyType/Delete/5
        [HttpPost, ActionName("Delete")]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            PenaltyType penaltyType = new PenaltyType() { Id = id, Code = "", ArName = "" };
            db.PenaltyTypes.Attach(penaltyType);
            penaltyType.IsDeleted = true;
            foreach (var detail in penaltyType.PenaltyTypeDetails)
            {
                detail.IsDeleted = true;
            }
            db.Entry(penaltyType).Property(x => x.IsDeleted).IsModified = true;

            await db.SaveChangesAsync();
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "حذف انواع الجزاءات",
                EnAction = "AddEdit",
                ControllerName = "PenaltyType",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "POST",
                SelectedItem = id,
                EnItemName = penaltyType.EnName,
                ArItemName = penaltyType.ArName,
                CodeOrDocNo = penaltyType.Code
            });
            Notification.GetNotification("PenaltyType", "Delete", "Delete", id, null, "انواع الجزاءات");
            return Content("true");
        }

        [HttpPost]
        public async Task<ActionResult> ActivateDeactivate(int id)
        {
            PenaltyType penaltyType = await db.PenaltyTypes.FindAsync(id);
            penaltyType.IsActive = !penaltyType.IsActive;
            db.Entry(penaltyType).State = EntityState.Modified;
            await db.SaveChangesAsync();
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = penaltyType.IsActive ? "تنشيط انواع الجزاءات" : "إلغاء انواع الجزاءات",
                EnAction = "AddEdit",
                ControllerName = "PenaltyType",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "POST",
                SelectedItem = penaltyType.Id,
                EnItemName = penaltyType.EnName,
                ArItemName = penaltyType.ArName,
                CodeOrDocNo = penaltyType.Code
            });
            Notification.GetNotification("PenaltyType", "Activate/Deactivate", "ActivateDeactivate", id, penaltyType.IsActive, "انواع الجزاءات");
            return Content("true");
        }

        [SkipERPAuthorize]
        public JsonResult SetCodeNum()
        {
            var code = QueryHelper.CodeLastNum("PenaltyType");
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
