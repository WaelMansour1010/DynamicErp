using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using MyERP.Models;

namespace MyERP.Controllers.AccountSettings
{
    public class CashBoxController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        // GET: CashBox
        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة الصندوق",
                EnAction = "Index",
                ControllerName = "CashBox",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });

            ////-------------------- Notification-------------------------////
            Notification.GetNotification("CashBox", "View", "Index", null, null, "الصندوق");
            //////////////-----------------------------------------------------------------------
            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;

            /////////////////////////// Search ////////////////////

            IQueryable<CashBox> cashBoxs;
            if (string.IsNullOrEmpty(searchWord))
            {
                cashBoxs = db.CashBoxes.Where(s => s.IsDeleted == false).OrderBy(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.CashBoxes.Where(s => s.IsDeleted == false).Count();


            }
            else
            {
                cashBoxs = db.CashBoxes.Where(s => s.IsDeleted == false && (s.Code.Contains(searchWord) || s.ArName.Contains(searchWord) || s.EnName.Contains(searchWord) || s.ChartOfAccount.ArName.Contains(searchWord) || s.Employee.ArName.Contains(searchWord) || s.Notes.Contains(searchWord))).OrderBy(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.CashBoxes.Where(s => s.IsDeleted == false && (s.Code.Contains(searchWord) || s.ArName.Contains(searchWord) || s.EnName.Contains(searchWord) || s.ChartOfAccount.ArName.Contains(searchWord) || s.Employee.ArName.Contains(searchWord) || s.Notes.Contains(searchWord))).Count();
            }

            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            ///////////////////////////////////////////////////////////////////////////////
            return View(cashBoxs.ToList());
        }

        public ActionResult AddEdit(int? id)
        {
            var SystemPageId = db.SystemPages.Where(a => a.IsActive == true && a.IsDeleted == false && a.ControllerName == "CashBox").FirstOrDefault().Id;
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            //add
            if (id == null)
            {
                ViewBag.FieldsCodingId = new SelectList(db.FieldsCodings.Where(a => a.IsActive == true && a.IsDeleted == false && a.PageId == SystemPageId).Select(b => new
                {
                    b.Id,
                    ArName = b.FixedPart + " - " + b.ArName
                }).ToList(), "Id", "ArName");
                ViewBag.AccountId = new SelectList(db.ChartOfAccounts.Where(c => c.ChartOfAccountsClassification.Id == 3 && c.IsDeleted == false && c.IsActive == true).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }).ToList(), "Id", "ArName");
                ViewBag.TypeId = new SelectList(db.CashBoxTypes.Select(b => new
                {
                    Id = b.Id,
                    ArName = b.ArName
                }).ToList(), "Id", "ArName");
                ViewBag.DepartmentId = new SelectList(db.Department_ReportUserDepartments(userId), "Id", "ArName");
                return View();
            }
            //edit and details
            CashBox cashBox = db.CashBoxes.Find(id);
            if (cashBox == null)
            {
                return HttpNotFound();
            }
            ViewBag.FieldsCodingId = new SelectList(db.FieldsCodings.Where(a => a.IsActive == true && a.IsDeleted == false && a.PageId == SystemPageId).Select(b => new
            {
                b.Id,
                ArName = b.FixedPart + " - " + b.ArName
            }).ToList(), "Id", "ArName", cashBox.FieldsCodingId);
            ViewBag.TypeId = new SelectList(db.CashBoxTypes.Select(b => new
            {
                Id = b.Id,
                ArName = b.ArName
            }).ToList(), "Id", "ArName", cashBox.TypeId);
            ViewBag.AccountId = new SelectList(db.ChartOfAccounts.Where(c => c.ChartOfAccountsClassification.Id == 3 && c.IsDeleted == false && c.IsActive == true).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }).ToList(), "Id", "ArName", cashBox.AccountId);
            ViewBag.DepartmentId = new SelectList(db.Department_ReportUserDepartments(userId), "Id", "ArName", cashBox.DepartmentId);

            ViewBag.Next = QueryHelper.Next((int)id, "CashBox");
            ViewBag.Previous = QueryHelper.Previous((int)id, "CashBox");
            ViewBag.Last = QueryHelper.GetLast("CashBox");
            ViewBag.First = QueryHelper.GetFirst("CashBox");

            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح تفاصيل صندوق",
                EnAction = "AddEdit",
                ControllerName = "CashBox",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = cashBox.Id,
                ArItemName = cashBox.ArName,
                EnItemName = cashBox.EnName,
                CodeOrDocNo = cashBox.Code
            });

            return View(cashBox);
        }


        [HttpPost]
        public ActionResult AddEdit(CashBox cashBox, string newBtn, decimal? OldOpenningbalance)
        {
            var SystemPageId = db.SystemPages.Where(a => a.IsActive == true && a.IsDeleted == false && a.ControllerName == "CashBox").FirstOrDefault().Id;
            if (ModelState.IsValid)
            {
                if (cashBox.OpeningBalance == null)
                {
                    cashBox.OpeningBalance = 0;
                }
                var id = cashBox.Id;
                cashBox.IsDeleted = false;
                if (cashBox.Id > 0)
                {
                    // add opening balance to account.ObDebit
                    var account = db.ChartOfAccounts.Find(cashBox.AccountId);
                    //after Edit
                    var openningBalance = cashBox.OpeningBalance;
                    account.ObDebit = (account.ObDebit != null ? account.ObDebit : 0) - OldOpenningbalance + openningBalance;
                    db.Entry(account).State = EntityState.Modified;
                    ///////////////////////////////////
                    cashBox.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                    db.Entry(cashBox).State = EntityState.Modified;

                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("CashBox", "Edit", "AddEdit", id, null, "الصندوق");

                    //////////-----------------------------------------------------------------------
                    
                    // Add DB Change
                    QueryHelper.AddDBChange(new DBChange()
                    {
                        TableName = "CashBox",
                        SelectedId = cashBox.Id,
                        IsMasterChange = true,
                        IsNew = false,
                        IsTransaction = false
                    });

                }
                else
                {
                    // add opening balance to account.ObDebit
                    var account = db.ChartOfAccounts.Find(cashBox.AccountId);
                    account.ObDebit = (account.ObDebit != null ? account.ObDebit : 0) + cashBox.OpeningBalance;
                    db.Entry(account).State = EntityState.Modified;
                    ///////////////////////////////////
                    cashBox.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    cashBox.IsActive = true;
                    //cashBox.Code = (QueryHelper.CodeLastNum("CashBox") + 1).ToString();
                    cashBox.Code = new JavaScriptSerializer().Serialize(SetCodeNum(cashBox.FieldsCodingId).Data).ToString().Trim('"');

                    db.CashBoxes.Add(cashBox);
                    db.SaveChanges();
                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("CashBox", "Add", "AddEdit", cashBox.Id, null, "الصندوق");
                    /////////////-----------------------------------------------------------------------

                    // Add DB Change
                    var SelectedId = db.CashBoxes.OrderByDescending(r => r.Id).FirstOrDefault().Id;
                    QueryHelper.AddDBChange(new DBChange()
                    {
                        TableName = "CashBox",
                        SelectedId = SelectedId,
                        IsMasterChange = true,
                        IsNew = true,
                        IsTransaction = false
                    });
                }
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = id > 0 ? "تعديل صندوق" : "اضافة صندوق",
                    EnAction = "AddEdit",
                    ControllerName = "CashBox",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = cashBox.Id,
                    ArItemName = cashBox.ArName,
                    EnItemName = cashBox.EnName,
                    CodeOrDocNo = cashBox.Code
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
            ViewBag.FieldsCodingId = new SelectList(db.FieldsCodings.Where(a => a.IsActive == true && a.IsDeleted == false && a.PageId == SystemPageId).Select(b => new
            {
                b.Id,
                ArName = b.FixedPart + " - " + b.ArName
            }), "Id", "ArName", cashBox.FieldsCodingId);
            ViewBag.AccountId = new SelectList(db.ChartOfAccounts.Where(c => c.ChartOfAccountsClassification.Id == 3 && c.IsDeleted == false && c.IsActive == true).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", cashBox.AccountId);

            ViewBag.Next = QueryHelper.Next(cashBox.Id, "CashBox");
            ViewBag.Previous = QueryHelper.Previous(cashBox.Id, "CashBox");
            ViewBag.Last = QueryHelper.GetLast("CashBox");
            ViewBag.First = QueryHelper.GetFirst("CashBox");
            return View(cashBox);
        }

        // POST: CashBox/Delete/5
        [HttpPost, ActionName("Delete")]

        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                CashBox cashBox = db.CashBoxes.Find(id);
                // remove opening balance from account.ObDebit
                var account = db.ChartOfAccounts.Find(cashBox.AccountId);
                account.ObDebit = account.ObDebit - cashBox.OpeningBalance;
                db.Entry(account).State = EntityState.Modified;
                ///////////////////////////////////
                cashBox.IsDeleted = true;
                cashBox.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                Random random = new Random();
                const string chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
                var Code = new string(Enumerable.Repeat(chars, 6).Select(s => s[random.Next(s.Length)]).ToArray());
                cashBox.Code = Code;
                cashBox.FieldsCodingId = null;
                db.Entry(cashBox).State = EntityState.Modified;

                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "حذف صندوق",
                    EnAction = "AddEdit",
                    ControllerName = "CashBox",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    EnItemName = cashBox.EnName,
                    ArItemName = cashBox.ArName,
                    CodeOrDocNo = cashBox.Code
                });
                ////-------------------- Notification-------------------------////
                Notification.GetNotification("CashBox", "Delete", "Delete", id, null, "الصندوق");

                ////////////////-----------------------------------------------------------------------

                // Add DB Change
                QueryHelper.AddDBChange(new DBChange()
                {
                    TableName = "CashBox",
                    SelectedId = cashBox.Id,
                    IsMasterChange = true,
                    IsNew = false,
                    IsTransaction = false
                });

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
                CashBox cashBox = db.CashBoxes.Find(id);
                if (cashBox.IsActive == true)
                {
                    cashBox.IsActive = false;
                }
                else
                {
                    cashBox.IsActive = true;
                }
                cashBox.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                db.Entry(cashBox).State = EntityState.Modified;

                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = (bool)cashBox.IsActive ? "تنشيط صندوق" : "إلغاء صندوق",
                    EnAction = "AddEdit",
                    ControllerName = "CashBox",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = cashBox.Id,
                    EnItemName = cashBox.EnName,
                    ArItemName = cashBox.ArName,
                    CodeOrDocNo = cashBox.Code
                });
                if (cashBox.IsActive == true)
                {
                    Notification.GetNotification("CashBox", "Activate/Deactivate", "ActivateDeactivate", id, true, "الصندوق");
                }
                else
                {

                    Notification.GetNotification("CashBox", "Activate/Deactivate", "ActivateDeactivate", id, false, "الصندوق");
                }


                //Add DB Change
                QueryHelper.AddDBChange(new DBChange()
                {
                    TableName = "CashBox",
                    SelectedId = cashBox.Id,
                    IsMasterChange = true,
                    IsNew = false,
                    IsTransaction = false
                });

                return Content("true");
            }
            catch (Exception ex)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
        }
        [SkipERPAuthorize]
        public JsonResult SetCodeNum(int? FieldsCodingId)
        {
            if (FieldsCodingId > 0)
            {
                double result = 0;
                var fieldsCoding = db.FieldsCodings.Where(a => a.Id == FieldsCodingId).FirstOrDefault();
                var fixedPart = fieldsCoding.FixedPart;
                var noOfDigits = fieldsCoding.DigitsNo;
                var IsAutomaticSequence = fieldsCoding.IsAutomaticSequence;
                var IsZerosFills = fieldsCoding.IsZerosFills;
                if (string.IsNullOrEmpty(fixedPart))
                {
                    var code = db.CashBoxes.Where(a => a.FieldsCodingId == FieldsCodingId && a.IsActive == true && a.IsDeleted == false).OrderByDescending(a => a.Id).Select(a => a.Code);
                    if (code.FirstOrDefault() == null)
                    {
                        result = 0 + 1;
                    }
                    else
                    {
                        result = (double.Parse(code.FirstOrDefault().ToString())) + 1;
                    }
                    var CodeNo = "";
                    if (IsZerosFills == true)
                    {
                        if (result.ToString().Length < noOfDigits)
                        {
                            CodeNo = QueryHelper.FillsWithZeros(noOfDigits, result.ToString());
                        }
                        else
                        {
                            CodeNo = result.ToString();
                        }
                    }
                    else
                    {
                        CodeNo = result.ToString();
                    }
                    return Json(CodeNo, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    var FullNewCode = "";
                    var CodeNo = "";
                    var code = db.CashBoxes.Where(a => a.Code.Contains(fixedPart) && a.FieldsCodingId == FieldsCodingId && a.IsActive == true && a.IsDeleted == false).OrderByDescending(a => a.Id).Select(a => a.Code);
                    if (code.FirstOrDefault() == null)
                    {
                        result = 0 + 1;
                        if (IsZerosFills == true)
                        {
                            if (result.ToString().Length < noOfDigits)
                            {
                                CodeNo = QueryHelper.FillsWithZeros(noOfDigits /*- fixedPart.Length*/, result.ToString());
                            }
                            else
                            {
                                CodeNo = result.ToString();
                            }
                        }
                        else
                        {
                            CodeNo = result.ToString();
                        }
                        FullNewCode = fixedPart + CodeNo;
                    }
                    else
                    {
                        var LastCode = code.FirstOrDefault().ToString();
                        result = double.Parse(LastCode.Substring(LastCode.LastIndexOf(fixedPart) + fixedPart.Length)) + 1;
                        if (IsZerosFills == true)
                        {
                            if (result.ToString().Length < noOfDigits)
                            {
                                CodeNo = QueryHelper.FillsWithZeros(noOfDigits /*- fixedPart.Length*/, result.ToString());
                            }
                            else
                            {
                                CodeNo = result.ToString();
                            }
                        }
                        else
                        {
                            CodeNo = result.ToString();
                        }
                        FullNewCode = fixedPart + CodeNo;
                    }
                    return Json(FullNewCode, JsonRequestBehavior.AllowGet);
                }
            }
            else
            {
                double result = 0;
                var code = db.CashBoxes.Where(a => a.FieldsCodingId == null && a.IsActive == true && a.IsDeleted == false).OrderByDescending(a => a.Id).Select(a => a.Code);
                if (code.FirstOrDefault() == null)
                {
                    result = 0 + 1;
                }
                else
                {
                    result = double.Parse(code.FirstOrDefault().ToString()) + 1;
                }
                return Json(result, JsonRequestBehavior.AllowGet);
            }
            //var code = QueryHelper.CodeLastNum("CashBox");
            //return Json(code + 1, JsonRequestBehavior.AllowGet);
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
