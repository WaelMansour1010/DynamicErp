using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using MyERP.Models;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Threading;
using System.Globalization;

namespace MyERP.Controllers.AccountSettings
{
    public class ChartOfAccountsController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        // GET: ChartOfAccounts
        public async Task<ActionResult> Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
           
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة دليل الحسابات",
                EnAction = "Index",
                ControllerName = "ChartOfAccounts",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            ////-------------------- Notification-------------------------////
            Notification.GetNotification("ChartOfAccounts", "View", "Index", null, null, " دليل الحسابات");

            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;

            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;

            IQueryable<ChartOfAccount> chartOfAccounts;

            if (string.IsNullOrEmpty(searchWord))
            {
                chartOfAccounts = db.ChartOfAccounts.Where(s => s.IsDeleted == false).OrderBy(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo).OrderBy(s=>s.Code);
                var _count = await db.ChartOfAccounts.Where(s => s.IsDeleted == false).CountAsync();
                if (_count < wantedRowsNo)
                {
                    ViewBag.wantedRowsNo = _count;
                }
                ViewBag.Count = _count;//await db.ChartOfAccounts.Where(s => s.IsDeleted == false).CountAsync();
            }
            else
            {
                chartOfAccounts = db.ChartOfAccounts.Where(s => s.IsDeleted == false && (s.Code.Contains(searchWord) || s.ArName.Contains(searchWord) || s.EnName.Contains(searchWord) || s.ChartOfAccountsType.ArType.Contains(searchWord) || s.ChartOfAccountsClassification.ArClassification.Contains(searchWord) || s.ChartOfAccountsCategory.ArCategory.Contains(searchWord) || s.Notes.Contains(searchWord))).OrderBy(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo).OrderBy(s => s.Code);
                var _count = await db.ChartOfAccounts.Where(s => s.IsDeleted == false && (s.Code.Contains(searchWord) || s.ArName.Contains(searchWord) || s.EnName.Contains(searchWord) || s.ChartOfAccountsType.ArType.Contains(searchWord) || s.ChartOfAccountsClassification.ArClassification.Contains(searchWord) || s.ChartOfAccountsCategory.ArCategory.Contains(searchWord) || s.Notes.Contains(searchWord))).CountAsync();
                if (_count < wantedRowsNo)
                {
                    ViewBag.wantedRowsNo = _count;
                }
                ViewBag.Count = _count;// await db.ChartOfAccounts.Where(s => s.IsDeleted == false && (s.Code.Contains(searchWord) || s.ArName.Contains(searchWord) || s.EnName.Contains(searchWord) || s.ChartOfAccountsType.ArType.Contains(searchWord) || s.ChartOfAccountsClassification.ArClassification.Contains(searchWord) || s.ChartOfAccountsCategory.ArCategory.Contains(searchWord) || s.Notes.Contains(searchWord))).CountAsync();
            }
           
            return View(await chartOfAccounts.ToListAsync());
        }

        public JsonResult GetChartOfAccounts()
        {
            return Json(db.GetChartOfAccounts(), JsonRequestBehavior.AllowGet);
        }

        // GET: ChartOfAccounts/Edit/5
        public async Task<ActionResult> AddEdit(int? id)
        {
            var session = Session["lang"]!=null?Session["lang"].ToString():"ar";
            if (id == null)
            {
                ViewBag.TypeId = new SelectList(await db.ChartOfAccountsTypes.Select(b => new
                {
                    b.Id,
                    ArType = session == "en" && b.EnType != null ? b.EnType:b.ArType
                }).ToListAsync(), "Id", "ArType");
                ViewBag.ParentId = new SelectList(await db.ChartOfAccounts.Where(c => c.IsActive == true && c.IsDeleted == false).Select(b => new
                {
                    b.Id,
                    ArName = session == "en" && b.EnName != null ? b.Code + " - " + b.EnName : b.Code + " - " + b.ArName
                }).ToListAsync(), "Id", "ArName");
                ViewBag.CategoryId = new SelectList(await db.ChartOfAccountsCategories.Select(b => new
                {
                    b.Id,
                    ArCategory=session == "en" && b.EnCategory != null ? b.EnCategory : b.ArCategory
                }).ToListAsync(), "Id", "ArCategory");
                ViewBag.ClassificationId = new SelectList(await db.ChartOfAccountsClassifications.Select(b => new
                {
                    b.Id,
                    ArClassification= session == "en" && b.EnClassification != null ? b.EnClassification : b.ArClassification

                }).ToListAsync(), "Id", "ArClassification", 3);
                return View();
            }
            ChartOfAccount chartOfAccount = await db.ChartOfAccounts.FindAsync(id);
            if (chartOfAccount == null)
            {
                return HttpNotFound();
            }
            ViewBag.TypeId = new SelectList(await db.ChartOfAccountsTypes.Select(b => new
            {
                b.Id,
                ArType= session.ToString() == "en" && b.EnType != null ? b.EnType : b.ArType
            }).ToListAsync(), "Id", "ArType", chartOfAccount.TypeId);
            ViewBag.ParentId = new SelectList(await db.ChartOfAccounts.Where(c => c.IsActive == true && c.IsDeleted == false).Select(b => new
            {
                b.Id,
                ArName = session == "en" && b.EnName != null ? b.Code + " - " + b.EnName : b.Code + " - " + b.ArName
            }).ToListAsync(), "Id", "ArName", chartOfAccount.ParentId);
            ViewBag.CategoryId = new SelectList(await db.ChartOfAccountsCategories.Select(b => new
            {
                b.Id,
                ArCategory= session == "en" && b.EnCategory != null ? b.EnCategory : b.ArCategory
            }).ToListAsync(), "Id", "ArCategory", chartOfAccount.CategoryId);
            ViewBag.ClassificationId = new SelectList(await db.ChartOfAccountsClassifications.Select(b => new
            {
                b.Id,
                ArClassification= session == "en" && b.EnClassification != null ? b.EnClassification : b.ArClassification
            }).ToListAsync(), "Id", "ArClassification", chartOfAccount.ClassificationId);
            int childrenCount = db.ChartOfAccounts.Where(c => c.ParentId == id && c.IsDeleted == false).Count();
            ViewBag.ChildrenCount = childrenCount;
            ViewBag.Next = QueryHelper.Next((int)id, "ChartOfAccount");
            ViewBag.Previous = QueryHelper.Previous((int)id, "ChartOfAccount");
            ViewBag.Last = QueryHelper.GetLast("ChartOfAccount");
            ViewBag.First = QueryHelper.GetFirst("ChartOfAccount");
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح تفاصيل الحساب",
                EnAction = "AddEdit",
                ControllerName = "ChartOfAccounts",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = chartOfAccount.Id,
                ArItemName = chartOfAccount.ArName,
                EnItemName = chartOfAccount.EnName,
                CodeOrDocNo = chartOfAccount.Code
            });
            return View(chartOfAccount);
        }

        // POST: ChartOfAccounts/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        //[ValidateAntiForgeryToken]
        public ActionResult AddEdit(ChartOfAccount chartOfAccount, string newBtn)
        {
            var session = Session["lang"] != null ? Session["lang"].ToString() : "ar";
            if (ModelState.IsValid)
            {
                chartOfAccount.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                var id = chartOfAccount.Id;
                chartOfAccount.IsDeleted = false;
                if (chartOfAccount.Id > 0)
                {
                    db.Entry(chartOfAccount).State = EntityState.Modified;

                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("ChartOfAccounts", "Edit", "AddEdit", id, null, "دليل الحسابات");

                    ///////////////-----------------------------------------------------------------------

                }
                else
                {
                    chartOfAccount.IsActive = true;
                    db.ChartOfAccounts.Add(chartOfAccount);

                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("ChartOfAccounts", "Add", "AddEdit", chartOfAccount.Id, null, "دليل الحسابات");

                    //////////////////-----------------------------------------------------------------------

                }
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = id > 0 ? "تعديل حساب" : "اضافة حساب",
                    EnAction = "AddEdit",
                    ControllerName = "ChartOfAccounts",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = chartOfAccount.Id,
                    ArItemName = chartOfAccount.ArName,
                    EnItemName = chartOfAccount.EnName,
                    CodeOrDocNo = chartOfAccount.Code
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
            ViewBag.TypeId = new SelectList(db.ChartOfAccountsTypes, "Id", "ArType", chartOfAccount.TypeId);
            ViewBag.ParentId = new SelectList(db.ChartOfAccounts.Where(c => c.IsActive == true && c.IsDeleted == false).Select(b => new
            {
                Id = b.Id,
                ArName = session == "en" && b.EnName != null ? b.Code + " - " + b.EnName : b.Code + " - " + b.ArName
            }), "Id", "ArName", chartOfAccount.ParentId);
            ViewBag.CategoryId = new SelectList(db.ChartOfAccountsCategories, "Id", "ArCategory", chartOfAccount.CategoryId);
            ViewBag.ClassificationId = new SelectList(db.ChartOfAccountsClassifications, "Id", "ArClassification", chartOfAccount.ClassificationId);
            return View(chartOfAccount);
        }

        // POST: ChartOfAccounts/Delete/5
        [HttpPost, ActionName("Delete")]
        //[ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                ChartOfAccount chartOfAccount = db.ChartOfAccounts.Find(id);
                int childrenCount = db.ChartOfAccounts.Where(c=>c.ParentId==id &&c.IsDeleted==false).Count();
                if (childrenCount > 0)
                    return Content("hasChild");
                int usage = db.JournalEntryDetails.Where(j => j.IsDeleted == false && j.AccountId == id).Count();
                if (usage > 0)
                    return Content("usedBefore");
                chartOfAccount.IsDeleted = true;
                // chartOfAccount.Code = chartOfAccount.Id.ToString() + "**";//to prevent conflict if user add another account with the same code
                Random random = new Random();
                const string chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
                var Code = new string(Enumerable.Repeat(chars, 6).Select(s => s[random.Next(s.Length)]).ToArray());
                chartOfAccount.Code = Code;
                chartOfAccount.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                db.Entry(chartOfAccount).State = EntityState.Modified;

                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "حذف حساب",
                    EnAction = "AddEdit",
                    ControllerName = "ChartOfAccounts",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = chartOfAccount.Id,
                    ArItemName = chartOfAccount.ArName,
                    EnItemName = chartOfAccount.EnName,
                    CodeOrDocNo = chartOfAccount.Code
                });
                Notification.GetNotification("ChartOfAccounts", "Delete", "Delete", id, null, "دليل الحسابات");

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
                ChartOfAccount chartOfAccount = db.ChartOfAccounts.Find(id);
                if (chartOfAccount.IsActive == true)
                {
                    chartOfAccount.IsActive = false;
                }
                else
                {
                    chartOfAccount.IsActive = true;
                }
                chartOfAccount.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                db.Entry(chartOfAccount).State = EntityState.Modified;

                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = chartOfAccount.Id > 0 ? "تنشيط حساب" : "إلغاء حساب",
                    EnAction = "AddEdit",
                    ControllerName = "ChartOfAccounts",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = chartOfAccount.Id,
                    ArItemName = chartOfAccount.ArName,
                    EnItemName = chartOfAccount.EnName,
                    CodeOrDocNo = chartOfAccount.Code
                });
                //////-------------------- Notification-------------------------////
                if (chartOfAccount.IsActive == true)
                {
                    Notification.GetNotification("ChartOfAccounts", "Activate/Deactivate", "ActivateDeactivate", id, true, "دليل الحسابات");
                }
                else
                {

                    Notification.GetNotification("ChartOfAccounts", "Activate/Deactivate", "ActivateDeactivate", id, false, "دليل الحسابات");
                }
                //////////////////-----------------------------------------------------------------------

                return Content("true");
            }
            catch (Exception ex)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
        }
        [SkipERPAuthorize]
        [HttpGet]
        public JsonResult AccountsByClassificationId(int id)
        {
            var session = Session["lang"] != null ? Session["lang"].ToString() : "ar";
            if (id == 3)
            {
                var accounts = db.ChartOfAccounts.Where(a => a.ClassificationId == 2 && a.IsDeleted == false && a.IsActive == true).Select(b => new
                {
                    Id = b.Id,
                    ArName = session == "en" && b.EnName != null ? b.Code + " - " + b.EnName : b.Code + " - " + b.ArName
                });
                return Json(accounts, JsonRequestBehavior.AllowGet);
            }
            else if (id == 2)
            {
                var accounts = db.ChartOfAccounts.Where(a => (a.ClassificationId == 1 || a.ClassificationId == 2) && a.IsDeleted == false && a.IsActive == true).Select(b => new
                {
                    Id = b.Id,
                    ArName = session == "en" && b.EnName != null ? b.Code + " - " + b.EnName : b.Code + " - " + b.ArName
                });
                return Json(accounts, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return Json(0, JsonRequestBehavior.AllowGet);
            }

        }
        [SkipERPAuthorize]
        [HttpGet]
        public JsonResult CategoryIdByAccount(int id)
        {
            return Json(db.ChartOfAccounts.Find(id).CategoryId, JsonRequestBehavior.AllowGet);
        }

        [SkipERPAuthorize]
        [HttpGet]
        public JsonResult GetMaxChild(int id)
        {
            if (db.ChartOfAccounts.Where(c => c.ParentId == id && c.IsActive == true && c.IsDeleted == false).Count() > 0)
            {
                var MaxChildCode = db.ChartOfAccounts.Where(a => a.ParentId == id && a.IsActive == true && a.IsDeleted == false).Max(a => a.Code);
                return Json(MaxChildCode, JsonRequestBehavior.AllowGet);
            }
            else
                return Json(0, JsonRequestBehavior.AllowGet);
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
