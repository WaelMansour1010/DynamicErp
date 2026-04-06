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
    public class SalaryItemController : Controller
    {
        private readonly MySoftERPEntity db = new MySoftERPEntity();

        // GET: SalaryItem
        public async Task<ActionResult> Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة بنود الراتب",
                EnAction = "Index",
                ControllerName = "SalaryItem",
                UserName = User.Identity.Name,
                UserId = userId,
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            ////-------------------- Notification-------------------------////
            Notification.GetNotification("SalaryItem", "View", "Index", null, null, "بنود الراتب");
            ///////-----------------------------------------------------------------------

            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;

            IQueryable<SalaryItem> salaryItems;
            if (string.IsNullOrEmpty(searchWord))
            {
                salaryItems = db.SalaryItems.Where(c => c.IsDeleted == false).OrderBy(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.SalaryItems.Where(c => c.IsDeleted == false).Count();
            }
            else
            {
                salaryItems = db.SalaryItems.Where(s => s.IsDeleted == false && (s.Code.Contains(searchWord) || s.ArName.Contains(searchWord) || s.EnName.Contains(searchWord))).OrderBy(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.SalaryItems.Where(s => s.IsDeleted == false && (s.Code.Contains(searchWord) || s.ArName.Contains(searchWord) || s.EnName.Contains(searchWord))).Count();
            }
            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;

            return View(await salaryItems.ToListAsync());
        }

        // GET: SalaryItem/Edit/5
        public async Task<ActionResult> AddEdit(int? id)
        {
            if (id == null)
            {
                ViewBag.Type = new SelectList(new List<dynamic> { new { id=0,name="مستحق"},
                new { id=1,name="مستقطع"}}, "id", "name");

                //ChartOfAcc
                ViewBag.ChartOfAccountId = new SelectList(db.ChartOfAccounts.Where(a => a.IsActive == true && a.IsDeleted == false && a.ClassificationId == 3).Select(a => new
                {
                    Id = a.Id,
                    ArName = a.Code + " - " + a.ArName
                }), "Id", "ArName");
                //SalaryItemCalcMethodId
                ViewBag.SalaryItemCalcMethodId = new SelectList(db.SalaryItemCalcMethods.Where(a => a.IsActive == true && a.IsDeleted == false).Select(a => new
                {
                    Id = a.Id,
                    ArName = a.Code + " - " + a.ArName
                }), "Id", "ArName");
                //SalaryItemUnitId
                ViewBag.SalaryItemUnitId = new SelectList(db.SalaryItemUnits.Where(a => a.IsActive == true && a.IsDeleted == false).Select(a => new
                {
                    Id = a.Id,
                    ArName = a.Code + " - " + a.ArName
                }), "Id", "ArName");
                //SalaryItemNatureId
                ViewBag.SalaryItemNatureId = new SelectList(db.SalaryItemNatures.Where(a => a.IsActive == true && a.IsDeleted == false).Select(a => new
                {
                    Id = a.Id,
                    ArName = a.Code + " - " + a.ArName
                }), "Id", "ArName");
                //SalaryItemValueCalcMethod
                ViewBag.SalaryItemValueCalcMethodId = new SelectList(db.SalaryItemValueCalcMethods.Where(a => a.IsActive == true && a.IsDeleted == false).Select(a => new
                {
                    Id = a.Id,
                    ArName = a.Code + " - " + a.ArName
                }), "Id", "ArName");

                //Operation
                ViewBag.Operation = new SelectList(new List<dynamic> {
                new { id=1,name="+"},
                new { id=2,name="-"},
                new { id=3,name="*"},
                new { id=4,name="/"},
                }, "id", "name");
                //SalaryItem
                ViewBag.SalaryItemId = new SelectList(db.SalaryItems.Where(a => a.IsActive == true && a.IsDeleted == false).Select(a => new
                {
                    Id = a.Id,
                    ArName = a.ArName
                }), "Id", "ArName");
                ViewBag.HrDepartmentId = new SelectList(db.HrDepartments.Where(a => a.IsActive == true && a.IsDeleted == false).Select(a => new
                {
                    Id = a.Id,
                    ArName = a.ArName
                }), "Id", "ArName");

                return View();
            }
            SalaryItem salaryItem = await db.SalaryItems.FindAsync(id);
            if (salaryItem == null)
            {
                return HttpNotFound();
            }
            ViewBag.Type = new SelectList(new List<dynamic> { new { id=0,name="مستحق"},
                new { id=1,name="مستقطع"}}, "id", "name", salaryItem.Type);
            //Operation
            ViewBag.Operation = new SelectList(new List<dynamic> {
                new { id=1,name="+"},
                new { id=2,name="-"},
                new { id=3,name="*"},
                new { id=4,name="/"},
                }, "id", "name");

            //ChartOfAcc
            ViewBag.ChartOfAccountId = new SelectList(db.ChartOfAccounts.Where(a => a.IsActive == true && a.IsDeleted == false && a.ClassificationId == 3).Select(a => new
            {
                Id = a.Id,
                ArName = a.Code + " - " + a.ArName
            }), "Id", "ArName", salaryItem.ChartOfAccountId);
            //SalaryItemCalcMethodId
            ViewBag.SalaryItemCalcMethodId = new SelectList(db.SalaryItemCalcMethods.Where(a => a.IsActive == true && a.IsDeleted == false).Select(a => new
            {
                Id = a.Id,
                ArName = a.Code + " - " + a.ArName
            }), "Id", "ArName", salaryItem.SalaryItemCalcMethodId);
            //SalaryItemUnitId
            ViewBag.SalaryItemUnitId = new SelectList(db.SalaryItemUnits.Where(a => a.IsActive == true && a.IsDeleted == false).Select(a => new
            {
                Id = a.Id,
                ArName = a.Code + " - " + a.ArName
            }), "Id", "ArName", salaryItem.SalaryItemUnitId);
            //SalaryItemNatureId
            ViewBag.SalaryItemNatureId = new SelectList(db.SalaryItemNatures.Where(a => a.IsActive == true && a.IsDeleted == false).Select(a => new
            {
                Id = a.Id,
                ArName = a.Code + " - " + a.ArName
            }), "Id", "ArName", salaryItem.SalaryItemNatureId);

            //SalaryItemValueCalcMethod
            ViewBag.SalaryItemValueCalcMethodId = new SelectList(db.SalaryItemValueCalcMethods.Where(a => a.IsActive == true && a.IsDeleted == false).Select(a => new
            {
                Id = a.Id,
                ArName = a.Code + " - " + a.ArName
            }), "Id", "ArName", salaryItem.SalaryItemValueCalcMethodId);

            //SalaryItem
            ViewBag.SalaryItemId = new SelectList(db.SalaryItems.Where(a => a.IsActive == true && a.IsDeleted == false).Select(a => new
            {
                Id = a.Id,
                ArName = a.ArName
            }), "Id", "ArName");
            ViewBag.HrDepartmentId = new SelectList(db.HrDepartments.Where(a => a.IsActive == true && a.IsDeleted == false).Select(a => new
            {
                Id = a.Id,
                ArName = a.ArName
            }), "Id", "ArName", salaryItem.HrDepartmentId);
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح تفاصيل بنود الراتب",
                EnAction = "AddEdit",
                ControllerName = "SalaryItem",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = salaryItem.Id,
                ArItemName = salaryItem.ArName,
                EnItemName = salaryItem.EnName,
                CodeOrDocNo = salaryItem.Code
            });
            return View(salaryItem);
        }

        [HttpPost]
        //   [ValidateAntiForgeryToken]
        public async Task<ActionResult> AddEdit(SalaryItem salaryItem)
        {
            if (ModelState.IsValid)
            {
                var id = salaryItem.Id;
                salaryItem.IsDeleted = false;
                if (salaryItem.Id > 0)
                    db.Entry(salaryItem).State = EntityState.Modified;
                else
                {
                    salaryItem.IsActive = true;
                    db.SalaryItems.Add(salaryItem);
                }
                await db.SaveChangesAsync();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = id > 0 ? "تعديل بنود الراتب" : "اضافة بنود الراتب",
                    EnAction = "AddEdit",
                    ControllerName = "SalaryItem",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = salaryItem.Id,
                    ArItemName = salaryItem.ArName,
                    EnItemName = salaryItem.EnName,
                    CodeOrDocNo = salaryItem.Code
                });
                Notification.GetNotification("SalaryItem", id > 0 ? "Edit" : "Add", "AddEdit", salaryItem.Id, null, "بنود الراتب");
                return Json(new { success = "true" });
            }

            return View(salaryItem);
        }

        // POST: SalaryItem/Delete/5
        [HttpPost, ActionName("Delete")]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            SalaryItem salaryItem = new SalaryItem() { Id = id, Code = "", ArName = "" };
            db.SalaryItems.Attach(salaryItem);
            salaryItem.IsDeleted = true;
            db.Entry(salaryItem).Property(x => x.IsDeleted).IsModified = true;

            await db.SaveChangesAsync();
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "حذف بنود الراتب",
                EnAction = "AddEdit",
                ControllerName = "SalaryItem",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "POST",
                SelectedItem = id,
                EnItemName = salaryItem.EnName,
                ArItemName = salaryItem.ArName,
                CodeOrDocNo = salaryItem.Code
            });
            Notification.GetNotification("SalaryItem", "Delete", "Delete", id, null, "بنود الراتب");
            return Content("true");
        }

        [HttpPost]
        public async Task<ActionResult> ActivateDeactivate(int id)
        {
            SalaryItem salaryItem = await db.SalaryItems.FindAsync(id);
            salaryItem.IsActive = !salaryItem.IsActive;
            db.Entry(salaryItem).State = EntityState.Modified;
            await db.SaveChangesAsync();
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = salaryItem.IsActive ? "تنشيط بنود الراتب" : "إلغاء بنود الراتب",
                EnAction = "AddEdit",
                ControllerName = "SalaryItem",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "POST",
                SelectedItem = salaryItem.Id,
                EnItemName = salaryItem.EnName,
                ArItemName = salaryItem.ArName,
                CodeOrDocNo = salaryItem.Code
            });
            Notification.GetNotification("SalaryItem", "Activate/Deactivate", "ActivateDeactivate", id, salaryItem.IsActive, "بنود الراتب");
            return Content("true");
        }

        [SkipERPAuthorize]
        public JsonResult SetCodeNum()
        {
            var code = QueryHelper.CodeLastNum("SalaryItem");
            return Json(code + 1, JsonRequestBehavior.AllowGet);
        }

        [SkipERPAuthorize]
        public JsonResult GetSalaryItem(int id)
        {
            db.Configuration.ProxyCreationEnabled = false;
            var salaryItem = db.SalaryItems.FirstOrDefault(a => a.Id == id);
            return Json(salaryItem, JsonRequestBehavior.AllowGet);
        }
        [SkipERPAuthorize]
        public JsonResult ChangeSalaryItemTypeIntoDue()
        {
            try
            {
                db.Database.ExecuteSqlCommand($"update SalaryItem set Type = 0");
                return Json("success",JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(ex, JsonRequestBehavior.AllowGet);
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
