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

namespace MyERP.Controllers.SystemSettings
{
    public class BankAccountController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            ////////////////// LOG    ///////////////////////// 
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة الحسابات البنكية",
                EnAction = "Index",
                ControllerName = "BankAccount",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            ////-------------------- Notification-------------------------////
            Notification.GetNotification("BankAccount", "View", "Index", null, null, "الحسابات البنكية");
            ////////////////-----------------------------------------------------------------------
            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;

            /////////////////////////// Search ////////////////////

            IQueryable<BankAccount> bankAccount;

            if (string.IsNullOrEmpty(searchWord))
            {
                bankAccount = db.BankAccounts.Where(s => s.IsDeleted == false).OrderBy(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.BankAccounts.Where(s => s.IsDeleted == false).Count();

            }
            else
            {
                bankAccount = db.BankAccounts.Where(s => s.IsDeleted == false && (s.AccountNumber.Contains(searchWord) || s.ChartOfAccount.ArName.Contains(searchWord) ||
                                                         s.OpeningBalance.ToString().Contains(searchWord) || s.Branch.Contains(searchWord) || s.ChartOfAccount1.ArName.Contains(searchWord) || s.Currency.ArName.Contains(searchWord) || s.Notes.Contains(searchWord))).OrderBy(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.BankAccounts.Where(s => s.IsDeleted == false && (s.AccountNumber.Contains(searchWord) || s.ChartOfAccount.ArName.Contains(searchWord) ||
                                                          s.OpeningBalance.ToString().Contains(searchWord) || s.Branch.Contains(searchWord) || s.ChartOfAccount1.ArName.Contains(searchWord) || s.Currency.ArName.Contains(searchWord) || s.Notes.Contains(searchWord))).Count();

                ///////////////////////////////////////////////////////////////////////////////

            }
            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            return View(bankAccount.ToList());

        }
        [SkipERPAuthorize]
        public JsonResult SetCodeNum()
        {
            var code = QueryHelper.CodeLastNum("BankAccount");
            return Json(code + 1, JsonRequestBehavior.AllowGet);
        }

        public ActionResult AddEdit(int? id)
        {
            var accounts = db.ChartOfAccounts.Where(c => c.IsDeleted == false && c.IsActive == true && c.ClassificationId == 3).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            });
            if (id == null)
            {

                BankAccount NewObj = new BankAccount();
                ViewBag.BankId = new SelectList(db.Banks.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                ViewBag.AccountId = new SelectList(accounts, "Id", "ArName");
                ViewBag.CurrencyId = new SelectList(db.Currencies.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                ViewBag.BankAccountPaymentId = new SelectList(accounts, "Id", "ArName");
                ViewBag.BankAccountReceiptId = new SelectList(accounts, "Id", "ArName");

                return View(NewObj);
            }
            BankAccount bankAccount = db.BankAccounts.Find(id);
            if (bankAccount == null)
            {
                return HttpNotFound();
            }
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح تفاصيل الحساب البنكى ",
                EnAction = "AddEdit",
                ControllerName = "BankAccount",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = bankAccount.Id,

                CodeOrDocNo = bankAccount.AccountNumber
            });
            ViewBag.BankId = new SelectList(db.Banks.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", bankAccount.BankId);
            ViewBag.AccountId = new SelectList(accounts, "Id", "ArName", bankAccount.AccountId);
            ViewBag.CurrencyId = new SelectList(db.Currencies.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", bankAccount.CurrencyId);
            ViewBag.BankAccountPaymentId = new SelectList(accounts, "Id", "ArName", bankAccount.BankAccountPaymentId);
            ViewBag.BankAccountReceiptId = new SelectList(accounts, "Id", "ArName", bankAccount.BankAccountReceiptId);

            bankAccount.OpeningBalance = (decimal?)Math.Round((double)bankAccount.OpeningBalance * 100) / 100;
            ViewBag.Next = QueryHelper.Next((int)id, "BankAccount");
            ViewBag.Previous = QueryHelper.Previous((int)id, "BankAccount");
            ViewBag.Last = QueryHelper.GetLast("BankAccount");
            ViewBag.First = QueryHelper.GetFirst("BankAccount");
            return View(bankAccount);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddEdit(BankAccount bankAccount, string newBtn)
        {
            if (ModelState.IsValid)
            {
                var account = db.ChartOfAccounts.Find(bankAccount.AccountId);
                if (account != null)
                {
                    var OldObDebit = account.ObDebit!=null? account.ObDebit:0;
                    var diff =  bankAccount.OpeningBalance- OldObDebit ;
                    account.ObDebit = OldObDebit + (diff);
                    db.Entry(account).State = EntityState.Modified;
                }
                bankAccount.IsDeleted = false;
                if (bankAccount.Id > 0)
                {
                    bankAccount.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    db.Entry(bankAccount).State = EntityState.Modified;

                    if (bankAccount.OpeningBalance == null)
                    {
                        bankAccount.OpeningBalance = 0;
                    }

                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("BankAccount", "Edit", "AddEdit", bankAccount.Id, null, "الحسابات البنكية");
                    ////////-----------------------------------------------------------------------


                }
                else
                {
                    if (bankAccount.OpeningBalance == null)
                    {
                        bankAccount.OpeningBalance = 0;
                    }
                    bankAccount.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                    bankAccount.IsActive = true;
                    db.BankAccounts.Add(bankAccount);
                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("BankAccount", "Add", "AddEdit", bankAccount.Id, null, "الحسابات البنكية");
                    ////////////////-----------------------------------------------------------------------

                }

                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = bankAccount.Id > 0 ? "تعديل حساب بنكى" : "اضافة حساب بنكى",
                    EnAction = "AddEdit",
                    ControllerName = "BankAccount",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = bankAccount.Id,

                    CodeOrDocNo = bankAccount.AccountNumber
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
            var accounts = db.ChartOfAccounts.Where(c => c.IsDeleted == false && c.IsActive == true && c.ClassificationId == 3).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            });
            ViewBag.BankId = new SelectList(db.Banks.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", bankAccount.BankId);
            ViewBag.AccountId = new SelectList(accounts, "Id", "ArName", bankAccount.AccountId);
            ViewBag.CurrencyId = new SelectList(db.Currencies.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", bankAccount.CurrencyId);
            ViewBag.BankAccountPaymentId = new SelectList(accounts, "Id", "ArName", bankAccount.BankAccountPaymentId);
            ViewBag.BankAccountReceiptId = new SelectList(accounts, "Id", "ArName", bankAccount.BankAccountReceiptId);
            return View(bankAccount);
        }


        [HttpPost, ActionName("Delete")]

        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                BankAccount bankAccount = db.BankAccounts.Find(id);
                bankAccount.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                bankAccount.IsDeleted = true;
                db.Entry(bankAccount).State = EntityState.Modified;
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "حذف حساب بنكى",
                    EnAction = "AddEdit",
                    ControllerName = "BankAccount",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,

                    CodeOrDocNo = bankAccount.AccountNumber
                });
                ////-------------------- Notification-------------------------////
                Notification.GetNotification("BankAccount", "Delete", "Delete", id, null, "الحسابات البنكية");

                /////////////-----------------------------------------------------------------------

                return Content("true");
            }
            catch (Exception)
            {

                throw;
            }
        }

        [HttpPost]
        public ActionResult ActivateDeactivate(int id)
        {
            try
            {
                BankAccount bankAccount = db.BankAccounts.Find(id);
                if (bankAccount.IsActive == true)
                {
                    bankAccount.IsActive = false;
                }
                else
                {
                    bankAccount.IsActive = true;
                }
                bankAccount.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                db.Entry(bankAccount).State = EntityState.Modified;

                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = (bool)bankAccount.IsActive ? "تنشيط حساب بنكى" : "إلغاء تنشيط حساب بنكى",
                    EnAction = "AddEdit",
                    ControllerName = "BankAccount",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = bankAccount.Id,
                    CodeOrDocNo = bankAccount.AccountNumber
                });
                ////-------------------- Notification-------------------------////
                if (bankAccount.IsActive == true)
                {
                    Notification.GetNotification("BankAccount", "Activate/Deactivate", "ActivateDeactivate", id, true, "حساب بنكى");
                }
                else
                {

                    Notification.GetNotification("BankAccount", "Activate/Deactivate", "ActivateDeactivate", id, false, "حساب بنكى");
                }
                /////////-----------------------------------------------------------------------

                return Content("true");
            }
            catch (Exception ex)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
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
