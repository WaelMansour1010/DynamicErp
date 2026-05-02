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
using System.Data.Entity.Core.Objects;
using System.Web.Script.Serialization;

namespace MyERP.Controllers.AccountSettings
{
    public class CashTransfersController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        // GET: CashTransfers
        public ActionResult Index(bool? report, int? id, int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            ViewBag.PageIndex = pageIndex;
            ViewBag.OpenReport = report == true;
            if (report == true)
            {
                ViewBag.Id = id;
                ViewBag.Count = 0;
                return View();
            }
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

            var depIds = db.Departments.Where(d => (userId == 1 || db.UserDepartments.Where(u => u.UserId == userId && u.Privilege == true).Select(u => u.DepartmentId).Contains(d.Id)) && d.IsDeleted == false && d.IsActive == true).Select(d => (int?)d.Id);
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة تحويل النقدية",
                EnAction = "Index",
                ControllerName = "CashTransfers",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            Notification.GetNotification("CashTransfers", "View", "Index", null, null, "تحويل النقدية");
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;

            IQueryable<CashTransfer> cashTransfers;
            if (string.IsNullOrEmpty(searchWord))
            {
                cashTransfers = db.CashTransfers.Where(s => s.IsDeleted == false && s.IsActive == true && depIds.Contains(s.DepartmentId)).OrderByDescending(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.CashTransfers.Where(s => s.IsDeleted == false && s.IsActive == true && depIds.Contains(s.DepartmentId)).Count();
            }
            else
            {
                cashTransfers = db.CashTransfers.Where(s => s.IsDeleted == false && s.IsActive == true && depIds.Contains(s.DepartmentId) && (s.DocumentNumber.Contains(searchWord) || s.Currency.ArName.Contains(searchWord) || s.Branch.ArName.Contains(searchWord) || s.CurrencyEquivalent.ToString().Contains(searchWord) || s.BankAccount.AccountNumber.Contains(searchWord) || s.Date.ToString().Contains(searchWord) || s.BankAccount1.AccountNumber.Contains(searchWord) || s.Notes.Contains(searchWord) || s.BankBranchFrom.Contains(searchWord) || s.BankBranchTo.Contains(searchWord) || s.CashBox.ArName.Contains(searchWord) || s.CashBox1.ArName.Contains(searchWord) || s.CurrencyEquivalent.ToString().Contains(searchWord) || s.TotalMoneyAmount.ToString().Contains(searchWord) || s.CashTransferType.ArName.Contains(searchWord) || s.Department.ArName.Contains(searchWord))).OrderByDescending(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);

                ViewBag.Count = db.CashTransfers.Where(s => s.IsDeleted == false && s.IsActive == true && depIds.Contains(s.DepartmentId) && (s.DocumentNumber.Contains(searchWord) || s.Currency.ArName.Contains(searchWord) || s.Branch.ArName.Contains(searchWord) || s.CurrencyEquivalent.ToString().Contains(searchWord) || s.BankAccount.AccountNumber.Contains(searchWord) || s.Date.ToString().Contains(searchWord) || s.BankAccount1.AccountNumber.Contains(searchWord) || s.Notes.Contains(searchWord) || s.BankBranchFrom.Contains(searchWord) || s.BankBranchTo.Contains(searchWord) || s.CashBox.ArName.Contains(searchWord) || s.CashBox1.ArName.Contains(searchWord) || s.CurrencyEquivalent.ToString().Contains(searchWord) || s.TotalMoneyAmount.ToString().Contains(searchWord) || s.CashTransferType.ArName.Contains(searchWord) || s.Department.ArName.Contains(searchWord))).Count();
            }
            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            return View(cashTransfers.ToList());
        }

        // GET: CashTransfers/Edit/5
        public ActionResult AddEdit(int? id)
        {
            SystemSetting sysObj = db.SystemSettings.Any() ? db.SystemSettings.FirstOrDefault() : new SystemSetting();
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

            var bankAccounts = db.BankAccounts.Where(b => b.IsDeleted == false && b.IsActive == true);
            var banks = db.Banks.Where(b => b.IsDeleted == false && b.IsActive == true).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            });

            // Load Chart of Accounts for Account option
            var chartOfAccounts = db.ChartOfAccounts.Where(a => a.IsActive == true && a.IsDeleted == false).Select(a => new
            {
                a.Id,
                ArName = a.Code + " - " + a.ArName
            });

            if (id == null)
            {
                ViewBag.BankIdFrom = new SelectList(banks, "Id", "ArName", sysObj.DefaultBankId);
                ViewBag.BankIdTo = new SelectList(banks, "Id", "ArName", sysObj.DefaultBankId);
                ViewBag.BankAccountIdFrom = new SelectList(bankAccounts, "Id", "AccountNumber");
                ViewBag.BankAccountIdTo = new SelectList(bankAccounts, "Id", "AccountNumber");
                ViewBag.ChartOfAccountIdFrom = new SelectList(chartOfAccounts, "Id", "ArName");
                ViewBag.ChartOfAccountIdTo = new SelectList(chartOfAccounts, "Id", "ArName");
                ViewBag.BranchId = new SelectList(db.Branches.Where(b => b.IsDeleted == false && b.IsActive == true).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");

                ViewBag.CurrencyId = new SelectList(db.Currencies.Where(b => b.IsDeleted == false && b.IsActive == true).Select(b => new
                {
                    b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                if (userId == 1)
                {
                    ViewBag.DepartmentId = new SelectList(db.Departments.Where(d => d.IsActive == true && d.IsDeleted == false).Select(b => new
                    {
                        b.Id,
                        ArName = b.Code + " - " + b.ArName
                    }), "Id", "ArName", sysObj.DefaultDepartmentId);
                    //DepartmentIdTo
                    ViewBag.DepartmentIdTo = new SelectList(db.Departments.Where(d => d.IsActive == true && d.IsDeleted == false).Select(b => new
                    {
                        b.Id,
                        ArName = b.Code + " - " + b.ArName
                    }), "Id", "ArName", sysObj.DefaultDepartmentId);

                    var cashboxes = db.CashBoxes.Where(x => x.IsActive == true && x.IsDeleted == false && x.DepartmentId == sysObj.DefaultDepartmentId).Select(x => new
                    {
                        x.Id,
                        ArName = x.Code + " - " + x.ArName
                    });
                    ViewBag.CashBoxIdFrom = new SelectList(cashboxes, "Id", "ArName", sysObj.DefaultCashBoxId);
                    ViewBag.CashBoxIdTo = new SelectList(cashboxes, "Id", "ArName", sysObj.DefaultCashBoxId);
                }
                else
                {
                    ViewBag.DepartmentId = new SelectList(db.UserDepartments.Where(d => d.UserId == userId).Select(b => new
                    {
                        Id = b.DepartmentId,
                        ArName = b.Department.Code + " - " + b.Department.ArName
                    }), "Id", "ArName", sysObj.DefaultDepartmentId);

                    //DepartmentIdTo
                    ViewBag.DepartmentIdTo = new SelectList(db.UserDepartments.Where(d => d.UserId == userId).Select(b => new
                    {
                        Id = b.DepartmentId,
                        ArName = b.Department.Code + " - " + b.Department.ArName
                    }), "Id", "ArName", sysObj.DefaultDepartmentId);

                    var cashboxes = db.UserCashBoxes.Where(x => x.CashBox.IsActive == true && x.CashBox.IsDeleted == false && x.CashBox.DepartmentId == sysObj.DefaultDepartmentId && x.UserId == userId && x.Privilege == true).Select(x => new
                    {
                        x.CashBox.Id,
                        ArName = x.CashBox.Code + " - " + x.CashBox.ArName
                    });
                    ViewBag.CashBoxIdFrom = new SelectList(cashboxes, "Id", "ArName", sysObj.DefaultCashBoxId);
                    ViewBag.CashBoxIdTo = new SelectList(cashboxes, "Id", "ArName", sysObj.DefaultCashBoxId);
                }

                //------ Time Zone Depends On Currency --------//
                var Currency = db.Currencies.Where(a => a.IsActive == true && a.IsDeleted == false && a.IsDefault == true).FirstOrDefault();
                var CurrencyCode = Currency != null ? Currency.Code : "";
                TimeZoneInfo info;
                if (CurrencyCode == "SAR")
                {
                    //info = TimeZoneInfo.FindSystemTimeZoneById("Arabian Standard Time");//+2H from Egypt Standard Time
                    info = TimeZoneInfo.FindSystemTimeZoneById("Arab Standard Time");// +1H from Egypt Standard Time
                }
                else
                {
                    info = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");
                }
                DateTime utcNow = DateTime.UtcNow;
                DateTime cTime = TimeZoneInfo.ConvertTimeFromUtc(utcNow, info);
                //----------------- End of Time Zone Depends On Currency --------------------//

                ViewBag.Date = cTime.ToString("yyyy-MM-ddTHH:mm");

                return View();
            }
            CashTransfer cashTransfer = db.CashTransfers.Find(id);
            if (cashTransfer == null)
            {
                return HttpNotFound();
            }
            int sysPageId = QueryHelper.SourcePageId("CashTransfer");

            ViewBag.JE = db.GetJournalEntryBySourceIdAndSourcePageId(id, sysPageId).FirstOrDefault();
            if (ViewBag.JE != null)
            {
                var JEDocNo = ViewBag.JE;
                ViewBag.JE.DocumentNumber = JEDocNo != null ? JEDocNo.DocumentNumber.Replace("-", "") : "";
                int JEId = ViewBag.JE.Id;
                // int sourcePageId = ViewBag.JE.SourcePageId;
                ViewBag.JEDetails = db.JournalEntryDetails.Where(a => a.JournalEntryId == JEId).OrderBy(a => a.Id).ToList();

            }
            if (cashTransfer.DepartmentId != cashTransfer.DepartmentIdTo)//transaction has two journals
            {
                // Get all journal entries for this transaction

                var allJournalEntries = db.GetJournalEntryBySourceIdAndSourcePageId(id, sysPageId).ToList();
                if (allJournalEntries.Count > 1)
                {
                    ViewBag.JE2 = allJournalEntries[1];

                    

                // Check if there is a second journal entry (for different departments)

                    if (ViewBag.JE2 != null)
                    {
                        var JEDocNo = ViewBag.JE2;
                        ViewBag.JE2.DocumentNumber = JEDocNo != null ? JEDocNo.DocumentNumber.Replace("-", "") : "";
                        int JEId = ViewBag.JE2.Id;
                        //int sourcePageId = ViewBag.JE2.SourcePageId;
                        ViewBag.JEDetails2 = db.JournalEntryDetails.Where(a => a.JournalEntryId == JEId).OrderBy(a => a.Id).ToList();
                    }
                }
            }

            if (cashTransfer.SystemPageId != null)
            {
                var x = QueryHelper.GetSystemPageDocNo(cashTransfer.SystemPageId, cashTransfer.SelectedId);
                ViewBag.SourceDocNo = QueryHelper.GetSystemPageDocNo(cashTransfer.SystemPageId, cashTransfer.SelectedId);
            }
            ViewBag.Accounts = new SelectList(db.ChartOfAccounts.Where(a => a.ClassificationId == 3 && a.IsActive == true && a.IsDeleted == false), "Id", "ArName");
            ViewBag.BankIdFrom = new SelectList(banks, "Id", "ArName", cashTransfer.BankIdFrom);
            ViewBag.BankIdTo = new SelectList(banks, "Id", "ArName", cashTransfer.BankIdTo);
            ViewBag.BankAccountIdFrom = new SelectList(bankAccounts, "Id", "AccountNumber", cashTransfer.BankAccountIdFrom);
            ViewBag.BankAccountIdTo = new SelectList(bankAccounts, "Id", "AccountNumber", cashTransfer.BankAccountIdTo);
            ViewBag.ChartOfAccountIdFrom = new SelectList(chartOfAccounts, "Id", "ArName", cashTransfer.ChartOfAccountIdFrom);
            ViewBag.ChartOfAccountIdTo = new SelectList(chartOfAccounts, "Id", "ArName", cashTransfer.ChartOfAccountIdTo);
            ViewBag.BranchId = new SelectList(db.Branches.Where(b => b.IsDeleted == false && b.IsActive == true).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", cashTransfer.BranchId);

            ViewBag.CurrencyId = new SelectList(db.Currencies.Where(b => b.IsDeleted == false && b.IsActive == true).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", cashTransfer.CurrencyId);
            if (userId == 1)
            {
                ViewBag.DepartmentId = new SelectList(db.Departments.Where(d => d.IsActive == true && d.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName", cashTransfer.DepartmentId);

                //DepartmentIdTo
                ViewBag.DepartmentIdTo = new SelectList(db.Departments.Where(d => d.IsActive == true && d.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName", cashTransfer.DepartmentIdTo);

                var cashboxesFrom = db.CashBoxes.Where(x => x.IsActive == true && x.IsDeleted == false && x.DepartmentId == cashTransfer.DepartmentId).Select(x => new
                {
                    x.Id,
                    ArName = x.Code + " - " + x.ArName
                });
                var cashboxesTo = db.CashBoxes.Where(x => x.IsActive == true && x.IsDeleted == false && x.DepartmentId == cashTransfer.DepartmentIdTo).Select(x => new
                {
                    x.Id,
                    ArName = x.Code + " - " + x.ArName
                });
                ViewBag.CashBoxIdFrom = new SelectList(cashboxesFrom, "Id", "ArName", cashTransfer.CashBoxIdFrom);
                ViewBag.CashBoxIdTo = new SelectList(cashboxesTo, "Id", "ArName", cashTransfer.CashBoxIdTo);
            }
            else
            {
                ViewBag.DepartmentId = new SelectList(db.UserDepartments.Where(d => d.UserId == userId).Select(b => new
                {
                    Id = b.DepartmentId,
                    ArName = b.Department.Code + " - " + b.Department.ArName
                }), "Id", "ArName", cashTransfer.DepartmentId);

                //DepartmentIdTo
                ViewBag.DepartmentIdTo = new SelectList(db.UserDepartments.Where(d => d.UserId == userId).Select(b => new
                {
                    Id = b.DepartmentId,
                    ArName = b.Department.Code + " - " + b.Department.ArName
                }), "Id", "ArName", cashTransfer.DepartmentIdTo);
                var cashboxesFrom = db.UserCashBoxes.Where(x => x.CashBox.IsActive == true && x.CashBox.IsDeleted == false && x.CashBox.DepartmentId == cashTransfer.DepartmentId && x.UserId == userId && x.Privilege == true).Select(x => new
                {
                    x.CashBox.Id,
                    ArName = x.CashBox.Code + " - " + x.CashBox.ArName
                });
                var cashboxesTo = db.UserCashBoxes.Where(x => x.CashBox.IsActive == true && x.CashBox.IsDeleted == false && x.CashBox.DepartmentId == cashTransfer.DepartmentIdTo && x.UserId == userId && x.Privilege == true).Select(x => new
                {
                    x.CashBox.Id,
                    ArName = x.CashBox.Code + " - " + x.CashBox.ArName
                });
                ViewBag.CashBoxIdFrom = new SelectList(cashboxesFrom, "Id", "ArName", cashTransfer.CashBoxIdFrom);
                ViewBag.CashBoxIdTo = new SelectList(cashboxesTo, "Id", "ArName", cashTransfer.CashBoxIdTo);
            }

            ViewBag.Next = QueryHelper.Next((int)id, "CashTransfer");
            ViewBag.Previous = QueryHelper.Previous((int)id, "CashTransfer");
            ViewBag.Last = QueryHelper.GetLast("CashTransfer");
            ViewBag.First = QueryHelper.GetFirst("CashTransfer");
            try
            {
                ViewBag.Date = cashTransfer.Date.Value.ToString("yyyy-MM-ddTHH:mm");
            }
            catch (Exception)
            {
            }

            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح تفاصيل تحويل النقدية",
                EnAction = "AddEdit",
                ControllerName = "CashTransfer",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = cashTransfer.Id,
                CodeOrDocNo = cashTransfer.DocumentNumber
            });
            return View(cashTransfer);
        }

        // POST: CashTransfers/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        //[ValidateAntiForgeryToken]
        public ActionResult AddEdit([Bind(Include = "Id,DocumentNumber,BranchId,BankIdFrom,BankAccountIdFrom,BankBranchFrom,BankIdTo,BankAccountIdTo,BankBranchTo,CashBoxIdFrom,CashBoxIdTo,ChartOfAccountIdFrom,ChartOfAccountIdTo,CurrencyId,CurrencyEquivalent,TotalMoneyAmount,TransferTypeId,Date,UserId,IsActive,IsDeleted,IsLinked,IsPosted,Notes,Image,DepartmentId,DepartmentIdTo")] CashTransfer cashTransfer, string newBtn)
        {
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            if (ModelState.IsValid)
            {
                var id = cashTransfer.Id;
                cashTransfer.IsDeleted = false;

                if (cashTransfer.TransferTypeId == 1)
                {
                    cashTransfer.CashBoxIdFrom = null;
                    cashTransfer.CashBoxIdTo = null;
                    cashTransfer.ChartOfAccountIdFrom = null;
                    cashTransfer.ChartOfAccountIdTo = null;
                }
                else if (cashTransfer.TransferTypeId == 2)
                {
                    cashTransfer.CashBoxIdFrom = null;
                    cashTransfer.BankIdTo = null;
                    cashTransfer.BankAccountIdTo = null;
                    cashTransfer.ChartOfAccountIdFrom = null;
                    cashTransfer.ChartOfAccountIdTo = null;
                }
                else if (cashTransfer.TransferTypeId == 3)
                {
                    cashTransfer.BankIdFrom = null;
                    cashTransfer.BankAccountIdFrom = null;
                    cashTransfer.BankIdTo = null;
                    cashTransfer.BankAccountIdTo = null;
                    cashTransfer.ChartOfAccountIdFrom = null;
                    cashTransfer.ChartOfAccountIdTo = null;
                }
                else if (cashTransfer.TransferTypeId == 4)
                {
                    cashTransfer.BankIdFrom = null;
                    cashTransfer.BankAccountIdFrom = null;
                    cashTransfer.CashBoxIdTo = null;
                    cashTransfer.ChartOfAccountIdFrom = null;
                    cashTransfer.ChartOfAccountIdTo = null;
                }
                // New transfer types for Account option
                else if (cashTransfer.TransferTypeId == 5) // Bank to Account
                {
                    cashTransfer.CashBoxIdFrom = null;
                    cashTransfer.CashBoxIdTo = null;
                    cashTransfer.ChartOfAccountIdFrom = null;
                }
                else if (cashTransfer.TransferTypeId == 6) // Account to Bank
                {
                    cashTransfer.CashBoxIdFrom = null;
                    cashTransfer.CashBoxIdTo = null;
                    cashTransfer.ChartOfAccountIdTo = null;
                }
                else if (cashTransfer.TransferTypeId == 7) // CashBox to Account
                {
                    cashTransfer.BankIdFrom = null;
                    cashTransfer.BankAccountIdFrom = null;
                    cashTransfer.BankIdTo = null;
                    cashTransfer.BankAccountIdTo = null;
                    cashTransfer.ChartOfAccountIdFrom = null;
                }
                else if (cashTransfer.TransferTypeId == 8) // Account to CashBox
                {
                    cashTransfer.BankIdFrom = null;
                    cashTransfer.BankAccountIdFrom = null;
                    cashTransfer.BankIdTo = null;
                    cashTransfer.BankAccountIdTo = null;
                    cashTransfer.ChartOfAccountIdTo = null;
                }
                else if (cashTransfer.TransferTypeId == 9) // Account to Account
                {
                    cashTransfer.BankIdFrom = null;
                    cashTransfer.BankAccountIdFrom = null;
                    cashTransfer.BankIdTo = null;
                    cashTransfer.BankAccountIdTo = null;
                    cashTransfer.CashBoxIdFrom = null;
                    cashTransfer.CashBoxIdTo = null;
                }

                if (cashTransfer.Id > 0)
                {
                    if (db.CashTransfers.Find(cashTransfer.Id).IsPosted == true)
                    {
                        return Content("false");
                    }
                    cashTransfer.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                    db.CashTransfer_Update(cashTransfer.Id, cashTransfer.DocumentNumber, cashTransfer.BranchId, cashTransfer.BankIdFrom, cashTransfer.BankAccountIdFrom, cashTransfer.BankBranchFrom, cashTransfer.BankIdTo, cashTransfer.BankAccountIdTo, cashTransfer.BankBranchTo, cashTransfer.CashBoxIdFrom, cashTransfer.CashBoxIdTo, cashTransfer.ChartOfAccountIdFrom, cashTransfer.ChartOfAccountIdTo, cashTransfer.CurrencyId, cashTransfer.CurrencyEquivalent, cashTransfer.TotalMoneyAmount, cashTransfer.Date, cashTransfer.UserId, cashTransfer.IsActive, cashTransfer.IsDeleted, cashTransfer.IsLinked, cashTransfer.IsPosted, cashTransfer.Notes, cashTransfer.Image, cashTransfer.TransferTypeId, cashTransfer.DepartmentId, null, null, cashTransfer.DepartmentIdTo);
                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("CashTransfers", "Edit", "AddEdit", id, null, "تحويل النقدية");
                    ////////////-----------------------------------------------------------------------

                }
                else
                {
                    cashTransfer.IsActive = true;
                    //cashTransfer.DocumentNumber = (QueryHelper.DocLastNum(cashTransfer.DepartmentId, "CashTransfer") + 1).ToString();
                    cashTransfer.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    var idResult = new ObjectParameter("Id", typeof(Int32));
                    //-----------------------------------------------------
                    db.CashTransfer_Insert(idResult, cashTransfer.BranchId, cashTransfer.BankIdFrom, cashTransfer.BankAccountIdFrom, cashTransfer.BankBranchFrom, cashTransfer.BankIdTo, cashTransfer.BankAccountIdTo, cashTransfer.BankBranchTo, cashTransfer.CashBoxIdFrom, cashTransfer.CashBoxIdTo, cashTransfer.ChartOfAccountIdFrom, cashTransfer.ChartOfAccountIdTo ,cashTransfer.CurrencyId, cashTransfer.CurrencyEquivalent, cashTransfer.TotalMoneyAmount, cashTransfer.Date, cashTransfer.UserId, cashTransfer.IsActive, cashTransfer.IsDeleted, cashTransfer.IsLinked, false, cashTransfer.Notes, cashTransfer.Image, cashTransfer.TransferTypeId, cashTransfer.DepartmentId, null, null, cashTransfer.DepartmentIdTo);

                    id = (int)idResult.Value;
                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("CashTransfers", "Add", "AddEdit", cashTransfer.Id, null, "تحويل النقدية");

                }
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = cashTransfer.Id > 0 ? "تعديل تحويل النقدية" : "اضافة تحويل النقدية",
                    EnAction = "AddEdit",
                    ControllerName = "CashTransfers",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    CodeOrDocNo = cashTransfer.DocumentNumber
                });

                if (newBtn == "saveAndNew")
                {
                    return RedirectToAction("AddEdit");
                }
                else if (newBtn == "Report")
                {
                    return RedirectToAction("Index", new { report = true, id });
                }
                else
                {
                    return RedirectToAction("Index");
                }
            }
            var bankAccounts = db.BankAccounts.Where(b => b.IsDeleted == false && b.IsActive == true);
            var banks = db.Banks.Where(b => b.IsDeleted == false && b.IsActive == true);

            ViewBag.BankIdFrom = new SelectList(banks, "Id", "ArName", cashTransfer.BankIdFrom);
            ViewBag.BankIdTo = new SelectList(banks, "Id", "ArName", cashTransfer.BankIdTo);
            ViewBag.BankAccountIdFrom = new SelectList(bankAccounts, "Id", "AccountNumber", cashTransfer.BankAccountIdFrom);
            ViewBag.BankAccountIdTo = new SelectList(bankAccounts, "Id", "AccountNumber", cashTransfer.BankAccountIdTo);
            ViewBag.BranchId = new SelectList(db.Branches.Where(b => b.IsDeleted == false && b.IsActive == true).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", cashTransfer.BranchId);

            ViewBag.CurrencyId = new SelectList(db.Currencies.Where(b => b.IsDeleted == false && b.IsActive == true).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", cashTransfer.CurrencyId);
            if (userId == 1)
            {
                ViewBag.DepartmentId = new SelectList(db.Departments.Where(d => d.IsActive == true && d.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName", cashTransfer.DepartmentId);

                //DepartmentIdTo
                ViewBag.DepartmentIdTo = new SelectList(db.Departments.Where(d => d.IsActive == true && d.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName", cashTransfer.DepartmentIdTo);

                var cashboxes = db.CashBoxes.Where(x => x.IsActive == true && x.IsDeleted == false && x.DepartmentId == cashTransfer.DepartmentId).Select(x => new
                {
                    x.Id,
                    ArName = x.Code + " - " + x.ArName
                });
                ViewBag.CashBoxIdFrom = new SelectList(cashboxes, "Id", "ArName", cashTransfer.CashBoxIdFrom);
                ViewBag.CashBoxIdTo = new SelectList(cashboxes, "Id", "ArName", cashTransfer.CashBoxIdTo);
            }
            else
            {
                ViewBag.DepartmentId = new SelectList(db.UserDepartments.Where(d => d.UserId == userId).Select(b => new
                {
                    Id = b.DepartmentId,
                    ArName = b.Department.Code + " - " + b.Department.ArName
                }), "Id", "ArName", cashTransfer.DepartmentId);

                // DepartmentIdTo
                ViewBag.DepartmentIdTo = new SelectList(db.UserDepartments.Where(d => d.UserId == userId).Select(b => new
                {
                    Id = b.DepartmentId,
                    ArName = b.Department.Code + " - " + b.Department.ArName
                }), "Id", "ArName", cashTransfer.DepartmentIdTo);
                var cashboxes = db.UserCashBoxes.Where(x => x.CashBox.IsActive == true && x.CashBox.IsDeleted == false && x.CashBox.DepartmentId == cashTransfer.DepartmentId && x.UserId == userId && x.Privilege == true).Select(x => new
                {
                    x.CashBox.Id,
                    ArName = x.CashBox.Code + " - " + x.CashBox.ArName
                });
                ViewBag.CashBoxIdFrom = new SelectList(cashboxes, "Id", "ArName", cashTransfer.CashBoxIdFrom);
                ViewBag.CashBoxIdTo = new SelectList(cashboxes, "Id", "ArName", cashTransfer.CashBoxIdTo);
            }

            ViewBag.Next = QueryHelper.Next(cashTransfer.Id, "CashTransfer");
            ViewBag.Previous = QueryHelper.Previous(cashTransfer.Id, "CashTransfer");
            ViewBag.Last = QueryHelper.GetLast("CashTransfer");
            ViewBag.First = QueryHelper.GetFirst("CashTransfer");
            return View(cashTransfer);
        }

        // POST: CashTransfers/Delete/5
        [HttpPost, ActionName("Delete")]
        //[ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                CashTransfer cashTransfer = db.CashTransfers.Find(id);
                if (cashTransfer.IsPosted == true)
                {
                    return Content("false");
                }
                cashTransfer.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                cashTransfer.IsDeleted = true;
                Random random = new Random();
                const string chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
                var Code = new string(Enumerable.Repeat(chars, 6).Select(s => s[random.Next(s.Length)]).ToArray());
                cashTransfer.DocumentNumber = Code;
                var JournalEntryDoc= new string(Enumerable.Repeat(chars, 6).Select(s => s[random.Next(s.Length)]).ToArray()).ToString();
                db.Entry(cashTransfer).State = EntityState.Modified;
                db.Database.ExecuteSqlCommand($"update JournalEntry set IsDeleted = 1, UserId={userId} ,DocumentNumber=N'{JournalEntryDoc}' where SourcePageId = (select Id from SystemPage where TableName = 'CashTransfer') and SourceId = {id}; update JournalEntryDetail set IsDeleted = 1 where JournalEntryId = (select Id from JournalEntry where SourcePageId = (select Id from SystemPage where TableName = 'CashTransfer') and SourceId = {id})");
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "حذف تحويل النقدية",
                    EnAction = "AddEdit",
                    ControllerName = "CashTransfers",
                    UserName = User.Identity.Name,
                    UserId = userId,
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = cashTransfer.Id,
                    CodeOrDocNo = cashTransfer.DocumentNumber
                });


                ////-------------------- Notification-------------------------////
                Notification.GetNotification("CashTransfers", "Delete", "Delete", id, null, "تحويل النقدية");

                ///////////////-----------------------------------------------------------------------



                return Content("true");
            }
            catch (Exception e)
            {

                throw e;
            }
        }
        [SkipERPAuthorize]
        //[JsAction]
        [HttpGet]
        public JsonResult BankAccountsByBankId(int id)
        {
            var accounts = db.BankAccounts.Where(a => a.BankId == id && a.IsDeleted == false && a.IsActive == true).Select(a => new { a.Id, a.AccountNumber });
            return Json(accounts, JsonRequestBehavior.AllowGet);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
        [SkipERPAuthorize]
        public JsonResult SetDocNum(int? id, DateTime? VoucherDate)
        {
            bool IsExistInDocumentsCoding = false;
            var noOfDigits = (int?)0;
            var YearFormat = (int?)0;
            var CodingTypeId = (int?)0;
            var IsZerosFills = (bool?)null;
            var newDocNo = "";
            var GeneratedDocNo = "";
            var lastObj = db.CashTransfers.Where(a => a.IsActive == true && a.IsDeleted == false && a.DepartmentId == id).OrderByDescending(a => a.Id).FirstOrDefault();
            var lastDocNo = lastObj != null ? lastObj.DocumentNumber : "0";
            var DepartmentCode = db.Departments.Where(a => a.IsActive == true && a.IsDeleted == false && a.Id == id).FirstOrDefault().Code;
            DepartmentCode = double.Parse(DepartmentCode) < 10 ? "0" + DepartmentCode : DepartmentCode;
            var DepartmentDoc = db.DocumentsCodings.Where(a => a.IsActive == true && a.IsDeleted == false && a.DepartmentId == id).FirstOrDefault();
            var MonthFormat = VoucherDate.Value.Month < 10 ? "0" + VoucherDate.Value.Month.ToString() : VoucherDate.Value.Month.ToString();
            if (DepartmentDoc == null)
            {
                DepartmentDoc = db.DocumentsCodings.Where(a => a.IsActive == true && a.IsDeleted == false && a.AllDepartments == true).FirstOrDefault();
                if (DepartmentDoc == null)
                {
                    IsExistInDocumentsCoding = false;
                }
                else
                {
                    IsExistInDocumentsCoding = true;
                }
            }
            else
            {
                IsExistInDocumentsCoding = true;
            }
            if (IsExistInDocumentsCoding == true)
            {
                noOfDigits = DepartmentDoc.DigitsNo;
                YearFormat = DepartmentDoc.YearFormat;
                CodingTypeId = DepartmentDoc.CodingTypeId;
                IsZerosFills = DepartmentDoc.IsZerosFills;
                YearFormat = YearFormat == 2 ? int.Parse(VoucherDate.Value.Year.ToString().Substring(2, 2)) : int.Parse(VoucherDate.Value.Year.ToString());

                if (CodingTypeId == 1)//آلي
                {
                    if (lastDocNo.Contains("-"))
                    {
                        var ar = lastDocNo.Split('-');
                        if (IsZerosFills == true)
                        {
                            newDocNo = QueryHelper.FillsWithZeros(noOfDigits, (double.Parse(ar[3]) + 1).ToString());
                        }
                        else
                        {
                            newDocNo = (double.Parse(ar[3]) + 1).ToString();
                        }
                    }
                    else
                    {
                        if (IsZerosFills == true)
                        {
                            newDocNo = QueryHelper.FillsWithZeros(noOfDigits, (double.Parse(lastDocNo) + 1).ToString());
                        }
                        else
                        {
                            newDocNo = (double.Parse(lastDocNo) + 1).ToString();
                        }
                    }
                    GeneratedDocNo = DepartmentCode + "-" + YearFormat + "-" + MonthFormat + "-" + newDocNo;
                }
                else if (CodingTypeId == 2)//متصل شهري
                {
                    lastObj = db.CashTransfers.Where(a => a.IsActive == true && a.IsDeleted == false && a.DepartmentId == id && a.Date.Value.Month == VoucherDate.Value.Month && a.Date.Value.Year == VoucherDate.Value.Year).OrderByDescending(a => a.Id).FirstOrDefault();
                    if (lastObj != null)
                    {
                        if (lastObj.DocumentNumber.Contains("-"))
                        {
                            var ar = lastObj.DocumentNumber.Split('-');
                            if (double.Parse(ar[2]) == VoucherDate.Value.Month)
                            {
                                if (IsZerosFills == true)
                                {
                                    newDocNo = QueryHelper.FillsWithZeros(noOfDigits, (double.Parse(ar[3]) + 1).ToString());
                                }
                                else
                                {
                                    newDocNo = (double.Parse(ar[3]) + 1).ToString();
                                }
                            }
                            else
                            {
                                if (IsZerosFills == true)
                                {
                                    newDocNo = QueryHelper.FillsWithZeros(noOfDigits, "1").ToString();
                                }
                                else
                                {
                                    newDocNo = "1";
                                }
                            }
                        }
                        else
                        {
                            if (IsZerosFills == true)
                            {
                                newDocNo = QueryHelper.FillsWithZeros(noOfDigits, (double.Parse(lastObj.DocumentNumber) + 1).ToString()).ToString();
                            }
                            else
                            {
                                newDocNo = (double.Parse(lastObj.DocumentNumber) + 1).ToString();
                            }
                        }
                    }
                    else
                    {
                        if (IsZerosFills == true)
                        {
                            newDocNo = QueryHelper.FillsWithZeros(noOfDigits, "1").ToString();
                        }
                        else
                        {
                            newDocNo = "1";
                        }
                    }
                    GeneratedDocNo = DepartmentCode + "-" + YearFormat + "-" + MonthFormat + "-" + newDocNo;
                }
                else if (CodingTypeId == 3)//متصل سنوي
                {
                    lastObj = db.CashTransfers.Where(a => a.IsActive == true && a.IsDeleted == false && a.DepartmentId == id && a.Date.Value.Year == VoucherDate.Value.Year).OrderByDescending(a => a.Id).FirstOrDefault();

                    if (lastObj != null)
                    {
                        if (lastObj.DocumentNumber.Contains("-"))
                        {
                            var ar = lastObj.DocumentNumber.Split('-');
                            var VoucherDateFormate = int.Parse(ar[1]).ToString().Length == 2 ? int.Parse((VoucherDate.Value.Year.ToString()).Substring(2, 2)) : VoucherDate.Value.Year;
                            if (double.Parse(ar[1]) == VoucherDateFormate)
                            {
                                if (IsZerosFills == true)
                                {
                                    newDocNo = QueryHelper.FillsWithZeros(noOfDigits, (double.Parse(ar[3]) + 1).ToString());
                                }
                                else
                                {
                                    newDocNo = (double.Parse(ar[3]) + 1).ToString();
                                }
                            }
                            else
                            {
                                if (IsZerosFills == true)
                                {
                                    newDocNo = QueryHelper.FillsWithZeros(noOfDigits, "1").ToString();
                                }
                                else
                                {
                                    newDocNo = "1";
                                }
                            }
                        }
                        else
                        {
                            if (IsZerosFills == true)
                            {
                                newDocNo = QueryHelper.FillsWithZeros(noOfDigits, (double.Parse(lastObj.DocumentNumber) + 1).ToString()).ToString();
                            }
                            else
                            {
                                newDocNo = (double.Parse(lastObj.DocumentNumber) + 1).ToString();
                            }
                        }

                    }
                    else
                    {
                        if (IsZerosFills == true)
                        {
                            newDocNo = QueryHelper.FillsWithZeros(noOfDigits, "1").ToString();
                        }
                        else
                        {
                            newDocNo = "1";
                        }
                    }
                    GeneratedDocNo = DepartmentCode + "-" + YearFormat + "-" + MonthFormat + "-" + newDocNo;
                }
            }
            else
            {
                if (lastDocNo.Contains("-"))
                {
                    var ar = lastDocNo.Split('-');
                    newDocNo = (double.Parse(ar[3]) + 1).ToString();
                }
                else
                {
                    newDocNo = (double.Parse(lastDocNo) + 1).ToString();
                }
                GeneratedDocNo = newDocNo;
            }
            return Json(GeneratedDocNo, JsonRequestBehavior.AllowGet);
            //var docNo = QueryHelper.DocLastNum(id, "CashTransfer");
            //double i = (docNo) + 1;
            //return Json(i, JsonRequestBehavior.AllowGet);
        }
      
    }
}
