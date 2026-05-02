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
using System.Web.Script.Serialization;

namespace MyERP.Controllers.AccountSettings
{
    public class DirectExpensController : Controller
    {
        private readonly MySoftERPEntity db = new MySoftERPEntity();

        // GET: DirectExpens
        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة المصروفات المباشرة",
                EnAction = "Index",
                ControllerName = "DirectExpens",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            ////-------------------- Notification-------------------------////
            Notification.GetNotification("DirectExpens", "View", "Index", null, null, "المصروفات المباشرة");
            ////////////-----------------------------------------------------------------------

            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;

            IQueryable<DirectExpens> directExpenses;
            if (string.IsNullOrEmpty(searchWord))
            {
                directExpenses = db.DirectExpenses.Where(s => s.IsDeleted == false).OrderBy(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.DirectExpenses.Where(s => s.IsDeleted == false).Count();
            }
            else
            {
                directExpenses = db.DirectExpenses.Where(s => s.IsDeleted == false && (s.Code.Contains(searchWord) || s.ArName.Contains(searchWord) || s.EnName.Contains(searchWord) || s.ChartOfAccount.ArName.Contains(searchWord) || s.Notes.Contains(searchWord))).OrderBy(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = directExpenses.Count();
            }
            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            return View(directExpenses.ToList());
        }

        // GET: DirectExpens/Edit/5
        public ActionResult AddEdit(int? id)
        {
            var SystemPageId = db.SystemPages.Where(a => a.IsActive == true && a.IsDeleted == false && a.ControllerName == "DirectExpens").FirstOrDefault().Id;
            if (id == null)
            {
                ViewBag.AccountId = new SelectList(db.ChartOfAccounts.Where(c => c.IsDeleted == false && c.IsActive == true && c.ClassificationId == 3).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                ViewBag.FieldsCodingId = new SelectList(db.FieldsCodings.Where(a => a.IsActive == true && a.IsDeleted == false && a.PageId == SystemPageId).Select(b => new
                {
                    b.Id,
                    ArName = b.FixedPart + " - " + b.ArName
                }), "Id", "ArName");
                return View();
            }
            DirectExpens directExpens = db.DirectExpenses.Find(id);
            if (directExpens == null)
            {
                return HttpNotFound();
            }
            ViewBag.AccountId = new SelectList(db.ChartOfAccounts.Where(c => c.IsDeleted == false && c.IsActive == true && c.ClassificationId == 3).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", directExpens.AccountId);
            ViewBag.FieldsCodingId = new SelectList(db.FieldsCodings.Where(a => a.IsActive == true && a.IsDeleted == false && a.PageId == SystemPageId).Select(b => new
            {
                b.Id,
                ArName = b.FixedPart + " - " + b.ArName
            }), "Id", "ArName",directExpens.FieldsCodingId);

            ViewBag.Next = QueryHelper.Next((int)id, "DirectExpenses");
            ViewBag.Previous = QueryHelper.Previous((int)id, "DirectExpenses");
            ViewBag.Last = QueryHelper.GetLast("DirectExpenses");
            ViewBag.First = QueryHelper.GetFirst("DirectExpenses");

            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح تفاصيل المصروفات المباشرة",
                EnAction = "AddEdit",
                ControllerName = "DirectExpenses",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = directExpens.Id,
                ArItemName = directExpens.ArName,
                EnItemName = directExpens.EnName,
                CodeOrDocNo = directExpens.Code
            });

            return View(directExpens);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddEdit(DirectExpens directExpens, string newBtn)
        {
            var SystemPageId = db.SystemPages.Where(a => a.IsActive == true && a.IsDeleted == false && a.ControllerName == "DirectExpens").FirstOrDefault().Id;
            if (ModelState.IsValid)
            {
                var id = directExpens.Id;
                directExpens.IsDeleted = false;
                if (directExpens.Id > 0)
                {
                    directExpens.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                    db.Entry(directExpens).State = EntityState.Modified;
                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("DirectExpens", "Edit", "AddEdit", id, null, "المصروفات المباشرة");
                }
                else
                {
                    directExpens.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    directExpens.IsActive = true;
                    // directExpens.Code = (QueryHelper.CodeLastNum("DirectExpenses") + 1).ToString();
                    directExpens.Code = new JavaScriptSerializer().Serialize(SetCodeNum(directExpens.FieldsCodingId).Data).ToString().Trim('"');
                    db.DirectExpenses.Add(directExpens);
                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("DirectExpens", "Add", "AddEdit", directExpens.Id, null, "المصروفات المباشرة");
                }
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = id > 0 ? "تعديل المصروفات المباشرة" : "اضافة المصروفات المباشرة",
                    EnAction = "AddEdit",
                    ControllerName = "DirectExpens",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "Post",
                    SelectedItem = directExpens.Id,
                    ArItemName = directExpens.ArName,
                    EnItemName = directExpens.EnName,
                    CodeOrDocNo = directExpens.Code
                });
                if (newBtn == "saveAndNew")
                    return RedirectToAction("AddEdit");
                else
                    return RedirectToAction("Index");
            }
            ViewBag.AccountId = new SelectList(db.ChartOfAccounts.Where(c => c.IsDeleted == false && c.IsActive == true && c.ClassificationId == 3).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", directExpens.AccountId);
            ViewBag.FieldsCodingId = new SelectList(db.FieldsCodings.Where(a => a.IsActive == true && a.IsDeleted == false && a.PageId == SystemPageId).Select(b => new
            {
                b.Id,
                ArName = b.FixedPart + " - " + b.ArName
            }), "Id", "ArName", directExpens.FieldsCodingId);
            ViewBag.Next = QueryHelper.Next(directExpens.Id, "DirectExpenses");
            ViewBag.Previous = QueryHelper.Previous(directExpens.Id, "DirectExpenses");
            ViewBag.Last = QueryHelper.GetLast("DirectExpenses");
            ViewBag.First = QueryHelper.GetFirst("DirectExpenses");
            return View(directExpens);
        }

        // POST: DirectExpens/Delete/5
        [HttpPost, ActionName("Delete")]
        //[ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                DirectExpens directExpens = db.DirectExpenses.Find(id);
                directExpens.IsDeleted = true;
                directExpens.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                Random random = new Random();
                const string chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
                var Code = new string(Enumerable.Repeat(chars, 6).Select(s => s[random.Next(s.Length)]).ToArray());
                directExpens.Code = Code;
                directExpens.FieldsCodingId = null;
                db.Entry(directExpens).State = EntityState.Modified;

                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "حذف المصروفات المباشرة",
                    EnAction = "AddEdit",
                    ControllerName = "DirectExpenses",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "Post",
                    SelectedItem = directExpens.Id,
                    ArItemName = directExpens.ArName,
                    EnItemName = directExpens.EnName,
                    CodeOrDocNo = directExpens.Code
                });
                Notification.GetNotification("DirectExpens", "Delete", "Delete", id, null, "المصروفات المباشرة");

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
                DirectExpens directExpens = db.DirectExpenses.Find(id);
                if (directExpens.IsActive == true)
                {
                    directExpens.IsActive = false;
                }
                else
                {
                    directExpens.IsActive = true;
                }
                directExpens.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                db.Entry(directExpens).State = EntityState.Modified;

                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = directExpens.Id > 0 ? "تنشيط المصروفات المباشرة" : "إلغاء المصروفات المباشرة",
                    EnAction = "AddEdit",
                    ControllerName = "DirectExpens",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "Post",
                    SelectedItem = directExpens.Id,
                    ArItemName = directExpens.ArName,
                    EnItemName = directExpens.EnName,
                    CodeOrDocNo = directExpens.Code
                });
                if (directExpens.IsActive == true)
                {
                    Notification.GetNotification("DirectExpens", "Activate/Deactivate", "ActivateDeactivate", id, true, "المصروفات المباشرة");
                }
                else
                {

                    Notification.GetNotification("DirectExpens", "Activate/Deactivate", "ActivateDeactivate", id, false, "المصروفات المباشرة");
                }

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
                    var code = db.DirectExpenses.Where(a => a.FieldsCodingId == FieldsCodingId && a.IsActive == true && a.IsDeleted == false).OrderByDescending(a => a.Id).Select(a => a.Code);
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
                    var code = db.DirectExpenses.Where(a => a.Code.Contains(fixedPart) && a.FieldsCodingId == FieldsCodingId && a.IsActive == true && a.IsDeleted == false).OrderByDescending(a => a.Id).Select(a => a.Code);
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
                var code = db.DirectExpenses.Where(a => a.FieldsCodingId == null && a.IsActive == true && a.IsDeleted == false).OrderByDescending(a => a.Id).Select(a => a.Code);
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
            //var code = QueryHelper.CodeLastNum("DirectExpenses");
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
