using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using MyERP.Models;
using System.IO;
using System.Security.Claims;
using System.Web.Script.Serialization;

namespace MyERP.Controllers.SystemSettings
{
    public class BankController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            //////////////////  LOG //////////////////////////
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة البنوك",
                EnAction = "Index",
                ControllerName = "Bank",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity) User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });

            ////-------------------- Notification-------------------------////
            Notification.GetNotification("Bank", "View", "Index", null, null, "البنوك");
            ////////////////-----------------------------------------------------------------------

            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;

            /////////////////////////// Search ////////////////////

            IQueryable<Bank> bank;

            if (string.IsNullOrEmpty(searchWord))
            {
                bank = db.Banks.Where(s => s.IsDeleted == false).OrderBy(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.Banks.Where(s => s.IsDeleted == false).Count();

              
            }
            else
            {
                bank = db.Banks.Where(s => s.IsDeleted == false &&( s.Code.Contains(searchWord) || s.ArName.Contains(searchWord) ||
                                s.EnName.Contains(searchWord) || s.Notes.Contains(searchWord))).OrderBy(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = bank.Count();

             

            }
              ViewBag.searchWord = searchWord;
                ViewBag.wantedRowsNo = wantedRowsNo;

                ///////////////////////////////////////////////////////////////////////////////
                return View(bank.ToList());
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
                    var code = db.Banks.Where(a => a.FieldsCodingId == FieldsCodingId && a.IsActive == true && a.IsDeleted == false).OrderByDescending(a => a.Id).Select(a => a.Code);
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
                    var code = db.Banks.Where(a => a.Code.Contains(fixedPart) && a.FieldsCodingId == FieldsCodingId && a.IsActive == true && a.IsDeleted == false).OrderByDescending(a => a.Id).Select(a => a.Code);
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
                var code = db.Banks.Where(a => a.FieldsCodingId == null && a.IsActive == true && a.IsDeleted == false).OrderByDescending(a => a.Id).Select(a => a.Code);
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
            //var code = QueryHelper.CodeLastNum("Bank");
            //return Json(code + 1, JsonRequestBehavior.AllowGet);
        }

        public ActionResult AddEdit(int? id)
        {
            var SystemPageId = db.SystemPages.Where(a => a.IsActive == true && a.IsDeleted == false && a.ControllerName == "Bank").FirstOrDefault().Id;

            if (id == null)
            {
                ViewBag.FieldsCodingId = new SelectList(db.FieldsCodings.Where(a => a.IsActive == true && a.IsDeleted == false && a.PageId == SystemPageId).Select(b => new
                {
                    b.Id,
                    ArName = b.FixedPart + " - " + b.ArName
                }), "Id", "ArName");
                return View();
            }
            Bank bank = db.Banks.Find(id);
            if (bank == null)
            {
                return HttpNotFound();
            }
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح تفاصيل البنك ",
                EnAction = "AddEdit",
                ControllerName = "Bank",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = bank.Id,
                ArItemName = bank.ArName,
                EnItemName = bank.EnName,
                CodeOrDocNo = bank.Code
            });
            ViewBag.FieldsCodingId = new SelectList(db.FieldsCodings.Where(a => a.IsActive == true && a.IsDeleted == false && a.PageId == SystemPageId).Select(b => new
            {
                b.Id,
                ArName = b.FixedPart + " - " + b.ArName
            }), "Id", "ArName",bank.FieldsCodingId);
            ViewBag.Next = QueryHelper.Next((int)id, "Bank");
            ViewBag.Previous = QueryHelper.Previous((int)id, "Bank");
            ViewBag.Last = QueryHelper.GetLast("Bank");
            ViewBag.First = QueryHelper.GetFirst("Bank");
            return View(bank);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddEdit(Bank bank, string newBtn)
        {
            var SystemPageId = db.SystemPages.Where(a => a.IsActive == true && a.IsDeleted == false && a.ControllerName == "Bank").FirstOrDefault().Id;

            if (ModelState.IsValid)
            {
                bank.IsDeleted = false;
                if (bank.Id > 0)
                {
                    bank.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                    db.Entry(bank).State = EntityState.Modified;

                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("Bank", "Edit", "AddEdit", bank.Id, null, "البنوك");
                }
                else
                {
                    bank.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    //bank.Code= (QueryHelper.CodeLastNum("Bank") + 1).ToString();
                    bank.Code = new JavaScriptSerializer().Serialize(SetCodeNum(bank.FieldsCodingId).Data).ToString().Trim('"');
                    bank.IsActive = true;
                    db.Banks.Add(bank);

                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("Bank", "Add", "AddEdit", bank.Id, null, "البنوك");
                }

                try
                {
                    db.SaveChanges();
                }
                catch (Exception ex)
                {

                    var mod = ModelState.First(c => c.Key == "Code");  // this

                    mod.Value.Errors.Add("هذا الكود موجود من قبل");
                    return View(bank);
                }
                    QueryHelper.AddLog(new MyLog()
                {
                    ArAction = bank.Id > 0 ? "تعديل البنك" : "اضافة بنك",
                    EnAction = "AddEdit",
                    ControllerName = "Bank",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = bank.Id ,
                    ArItemName = bank.ArName,
                    EnItemName = bank.EnName,
                    CodeOrDocNo = bank.Code
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
            }), "Id", "ArName", bank.FieldsCodingId);
            return View(bank);
        }


        [HttpPost, ActionName("Delete")]

        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                Bank bank = db.Banks.Find(id);
                bank.IsDeleted = true;
                bank.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                Random random = new Random();
                const string chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
                var Code = new string(Enumerable.Repeat(chars, 6).Select(s => s[random.Next(s.Length)]).ToArray());
                bank.Code = Code;
                bank.FieldsCodingId = null;
                db.Entry(bank).State = EntityState.Modified;
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "حذف البنك",
                    EnAction = "AddEdit",
                    ControllerName = "Bank",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    EnItemName = bank.EnName,
                    ArItemName = bank.ArName,
                    CodeOrDocNo = bank.Code
                });

                ////-------------------- Notification-------------------------////
                Notification.GetNotification("Bank", "Delete", "Delete", id, null, "البنوك");

                //int pageid = db.Get_PageId("Bank").SingleOrDefault().Value;
                //var actionId = db.PageActions.FirstOrDefault(c => c.Action == "Delete" && c.EnName == "Delete" && c.PageId == pageid).Id;
                //int userid = db.UserNotifications.FirstOrDefault(c => c.ActionId == actionId && c.PageId == pageid).UserId.Value;
                //var UserName = User.Identity.Name;
                //db.Sp_OccuredNotification(actionId, $"بحذف بيانات في شاشة البنوك  {UserName}قام المستخدم  ");
                ////////////////-----------------------------------------------------------------------

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
                Bank bank = db.Banks.Find(id);
                if (bank.IsActive == true)
                {
                    bank.IsActive = false;
                }
                else
                {
                    bank.IsActive = true;
                }
                bank.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                db.Entry(bank).State = EntityState.Modified;

                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = (bool)bank.IsActive ? "تنشيط البنك" : "إلغاء تنشيط البنك",
                    EnAction = "AddEdit",
                    ControllerName = "Bank",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = bank.Id,
                    EnItemName = bank.EnName,
                    ArItemName = bank.ArName,
                    CodeOrDocNo = bank.Code
                });
                ////-------------------- Notification-------------------------////
                if (bank.IsActive == true)
                {
                    Notification.GetNotification("Bank", "Activate/Deactivate", "ActivateDeactivate", id, true, "البنك");
                }
                else
                {

                    Notification.GetNotification("Bank", "Activate/Deactivate", "ActivateDeactivate", id, false, "البنك");
                }
              ////////////////-----------------------------------------------------------------------
              
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
