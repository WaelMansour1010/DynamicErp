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
    public class RewardTypeController : Controller
    {
        private readonly MySoftERPEntity db = new MySoftERPEntity();

        // GET: RewardType
        public async Task<ActionResult> Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة انواع المكافئات",
                EnAction = "Index",
                ControllerName = "RewardType",
                UserName = User.Identity.Name,
                UserId = userId,
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            ////-------------------- Notification-------------------------////
            Notification.GetNotification("RewardType", "View", "Index", null, null, "انواع المكافئات");
            ///////-----------------------------------------------------------------------

            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;

            IQueryable<RewardType> rewardTypes;
            if (string.IsNullOrEmpty(searchWord))
            {
                rewardTypes = db.RewardTypes.Where(c => c.IsDeleted == false).OrderBy(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.RewardTypes.Where(c => c.IsDeleted == false).Count();
            }
            else
            {
                rewardTypes = db.RewardTypes.Where(s => s.IsDeleted == false && (s.Code.Contains(searchWord) || s.ArName.Contains(searchWord) || s.EnName.Contains(searchWord))).OrderBy(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.RewardTypes.Where(s => s.IsDeleted == false && (s.Code.Contains(searchWord) || s.ArName.Contains(searchWord) || s.EnName.Contains(searchWord))).Count();
            }
            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;

            return View(await rewardTypes.ToListAsync());
        }

        // GET: RewardType/Edit/5
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
            RewardType rewardType = await db.RewardTypes.FindAsync(id);
            if (rewardType == null)
            {
                return HttpNotFound();
            }
            //ChartOfAcc
            ViewBag.ChartOfAccountId = new SelectList(db.ChartOfAccounts.Where(a => a.IsActive == true && a.IsDeleted == false && a.ClassificationId == 3).Select(a => new {
                Id = a.Id,
                ArName = a.Code + " - " + a.ArName
            }), "Id", "ArName",rewardType.ChartOfAccountId);
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
                }, "Id", "ArName",rewardType.CalculationMethodId);
            return View(rewardType);
        }

        [HttpPost]
        //[ValidateAntiForgeryToken]
        public async Task<ActionResult> AddEdit(RewardType rewardType)
        {
            if (ModelState.IsValid)
            {
                var id = rewardType.Id;
                rewardType.IsDeleted = false;
                if (rewardType.Id > 0)
                {
                    db.RewardTypeDetails.RemoveRange(db.RewardTypeDetails.Where(x => x.MainDocId == rewardType.Id));
                    var rewardTypeDetials = rewardType.RewardTypeDetails.ToList();
                    rewardTypeDetials.ForEach((x) => x.MainDocId = rewardType.Id);
                    rewardType.RewardTypeDetails = null;
                    db.Entry(rewardType).State = EntityState.Modified;
                    db.RewardTypeDetails.AddRange(rewardTypeDetials);
                }
                else
                {
                    rewardType.IsActive = true;
                    db.RewardTypes.Add(rewardType);
                }
                await db.SaveChangesAsync();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = id > 0 ? "تعديل انواع المكافئات" : "اضافة انواع المكافئات",
                    EnAction = "AddEdit",
                    ControllerName = "RewardType",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = rewardType.Id,
                    ArItemName = rewardType.ArName,
                    EnItemName = rewardType.EnName,
                    CodeOrDocNo = rewardType.Code
                });
                Notification.GetNotification("RewardType", id > 0 ? "Edit" : "Add", "AddEdit", rewardType.Id, null, "انواع المكافئات");
                return Json(new { success = true });
            }
            var errors = ModelState
                  .Where(x => x.Value.Errors.Count > 0)
                  .Select(x => new { x.Key, x.Value.Errors })
                  .ToArray();
            return Json(new { success = false, errors });
        }

        // POST: RewardType/Delete/5
        [HttpPost, ActionName("Delete")]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            RewardType rewardType = new RewardType() { Id = id, Code = "", ArName = "" };
            db.RewardTypes.Attach(rewardType);
            rewardType.IsDeleted = true;
            foreach (var detail in rewardType.RewardTypeDetails)
            {
                detail.IsDeleted = true;
            }
            db.Entry(rewardType).Property(x => x.IsDeleted).IsModified = true;

            await db.SaveChangesAsync();
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "حذف انواع المكافئات",
                EnAction = "AddEdit",
                ControllerName = "RewardType",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "POST",
                SelectedItem = id,
                EnItemName = rewardType.EnName,
                ArItemName = rewardType.ArName,
                CodeOrDocNo = rewardType.Code
            });
            Notification.GetNotification("RewardType", "Delete", "Delete", id, null, "انواع المكافئات");
            return Content("true");
        }

        [HttpPost]
        public async Task<ActionResult> ActivateDeactivate(int id)
        {
            RewardType rewardType = await db.RewardTypes.FindAsync(id);
            rewardType.IsActive = !rewardType.IsActive;
            db.Entry(rewardType).State = EntityState.Modified;
            await db.SaveChangesAsync();
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = rewardType.IsActive ? "تنشيط انواع المكافئات" : "إلغاء انواع المكافئات",
                EnAction = "AddEdit",
                ControllerName = "RewardType",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "POST",
                SelectedItem = rewardType.Id,
                EnItemName = rewardType.EnName,
                ArItemName = rewardType.ArName,
                CodeOrDocNo = rewardType.Code
            });
            Notification.GetNotification("RewardType", "Activate/Deactivate", "ActivateDeactivate", id, rewardType.IsActive, "انواع المكافئات");
            return Content("true");
        }

        [SkipERPAuthorize]
        public JsonResult SetCodeNum()
        {
            var code = QueryHelper.CodeLastNum("RewardType");
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
