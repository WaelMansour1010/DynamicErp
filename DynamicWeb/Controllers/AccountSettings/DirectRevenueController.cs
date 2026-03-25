using System;
using System.Collections.Generic;
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

    public class DirectRevenueController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        // GET: DirectRevenue
        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة الإيرادات المباشرة",
                EnAction = "Index",
                ControllerName = "DirectRevenue",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            ////-------------------- Notification-------------------------////
            Notification.GetNotification("DirectRevenue", "View", "Index", null, null, "الإيرادات المباشرة");
            ////////////-----------------------------------------------------------------------

            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;

            /////////////////////////// Search ////////////////////

            IQueryable<DirectRevenue> directRevenues;
            if (string.IsNullOrEmpty(searchWord))
            {
                directRevenues = db.DirectRevenues.Where(s => s.IsDeleted == false).OrderBy(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.DirectRevenues.Where(s => s.IsDeleted == false).Count();


            }
            else
            {
                directRevenues = db.DirectRevenues.Where(s => s.IsDeleted == false && (s.Code.Contains(searchWord) || s.ArName.Contains(searchWord) ||
                                                                                       s.EnName.Contains(searchWord) ||
                                                                                       s.ChartOfAccount.ArName.Contains(searchWord) ||
                                                                                       s.Notes.Contains(searchWord))).OrderBy(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = directRevenues.Count();
            }
            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            ///////////////////////////////////////////////////////////////////////////////
            return View(directRevenues.ToList());
        }

        public ActionResult AddEdit(int? id)
        {
            var SystemPageId = db.SystemPages.Where(a => a.IsActive == true && a.IsDeleted == false && a.ControllerName == "DirectRevenue").FirstOrDefault().Id;

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
            DirectRevenue directRevenue = db.DirectRevenues.Find(id);
            if (directRevenue == null)
            {
                return HttpNotFound();
            }
            ViewBag.AccountId = new SelectList(db.ChartOfAccounts.Where(c => c.IsDeleted == false && c.IsActive == true && c.ClassificationId == 3).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", directRevenue.AccountId);
            ViewBag.FieldsCodingId = new SelectList(db.FieldsCodings.Where(a => a.IsActive == true && a.IsDeleted == false && a.PageId == SystemPageId).Select(b => new
            {
                b.Id,
                ArName = b.FixedPart + " - " + b.ArName
            }), "Id", "ArName",directRevenue.FieldsCodingId);
            ViewBag.Next = QueryHelper.Next((int)id, "directRevenue");
            ViewBag.Previous = QueryHelper.Previous((int)id, "directRevenue");
            ViewBag.Last = QueryHelper.GetLast("directRevenue");
            ViewBag.First = QueryHelper.GetFirst("directRevenue");

            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح تفاصيل الإيرادات المباشرة",
                EnAction = "AddEdit",
                ControllerName = "directRevenue",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = directRevenue.Id,
                ArItemName = directRevenue.ArName,
                EnItemName = directRevenue.EnName,
                CodeOrDocNo = directRevenue.Code
            });

            return View(directRevenue);
        }

        // POST: DirectExpens/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        //[ValidateAntiForgeryToken]
        public ActionResult AddEdit(DirectRevenue directRevenue, string newBtn)
        {
            var SystemPageId = db.SystemPages.Where(a => a.IsActive == true && a.IsDeleted == false && a.ControllerName == "DirectRevenue").FirstOrDefault().Id;
            if (ModelState.IsValid)
            {
                var id = directRevenue.Id;
                directRevenue.IsDeleted = false;
                if (directRevenue.Id > 0)
                {

                    directRevenue.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                    db.Entry(directRevenue).State = EntityState.Modified;

                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("directRevenue", "Edit", "AddEdit", id, null, "الإيرادات المباشرة");

                    /////////////-----------------------------------------------------------------------

                }
                else
                {
                    directRevenue.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    directRevenue.IsActive = true;
                    // directRevenue.Code = (QueryHelper.CodeLastNum("directRevenue") + 1).ToString();
                    directRevenue.Code = new JavaScriptSerializer().Serialize(SetCodeNum(directRevenue.FieldsCodingId).Data).ToString().Trim('"');
                    db.DirectRevenues.Add(directRevenue);
                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("directRevenue", "Add", "AddEdit", directRevenue.Id, null, "الإيرادات المباشرة");

                    ///////////-----------------------------------------------------------------------

                }
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = id > 0 ? "تعديل الإيرادات المباشرة" : "اضافة الإيرادات المباشرة",
                    EnAction = "AddEdit",
                    ControllerName = "directRevenue",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "Post",
                    SelectedItem = directRevenue.Id,
                    ArItemName = directRevenue.ArName,
                    EnItemName = directRevenue.EnName,
                    CodeOrDocNo = directRevenue.Code
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
            ViewBag.AccountId = new SelectList(db.ChartOfAccounts.Where(c => c.IsDeleted == false && c.IsActive == true && c.ClassificationId == 3).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", directRevenue.AccountId);
            ViewBag.FieldsCodingId = new SelectList(db.FieldsCodings.Where(a => a.IsActive == true && a.IsDeleted == false && a.PageId == SystemPageId).Select(b => new
            {
                b.Id,
                ArName = b.FixedPart + " - " + b.ArName
            }), "Id", "ArName", directRevenue.FieldsCodingId);
            ViewBag.Next = QueryHelper.Next(directRevenue.Id, "directRevenue");
            ViewBag.Previous = QueryHelper.Previous(directRevenue.Id, "directRevenue");
            ViewBag.Last = QueryHelper.GetLast("directRevenue");
            ViewBag.First = QueryHelper.GetFirst("directRevenue");
            return View(directRevenue);
        }

        // POST: DirectExpens/Delete/5
        [HttpPost, ActionName("Delete")]
        //[ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                DirectRevenue directRevenue = db.DirectRevenues.Find(id);
                directRevenue.IsDeleted = true;
                directRevenue.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                Random random = new Random();
                const string chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
                var Code = new string(Enumerable.Repeat(chars, 6).Select(s => s[random.Next(s.Length)]).ToArray());
                directRevenue.Code = Code;
                directRevenue.FieldsCodingId = null;
                db.Entry(directRevenue).State = EntityState.Modified;

                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "حذف الإيرادات المباشرة",
                    EnAction = "AddEdit",
                    ControllerName = "directRevenue",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "Post",
                    SelectedItem = directRevenue.Id,
                    ArItemName = directRevenue.ArName,
                    EnItemName = directRevenue.EnName,
                    CodeOrDocNo = directRevenue.Code
                });
                Notification.GetNotification("directRevenue", "Delete", "Delete", id, null, "الإيرادات المباشرة");

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
                DirectRevenue directRevenue = db.DirectRevenues.Find(id);
                if (directRevenue.IsActive == true)
                {
                    directRevenue.IsActive = false;
                }
                else
                {
                    directRevenue.IsActive = true;
                }
                directRevenue.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                db.Entry(directRevenue).State = EntityState.Modified;

                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = directRevenue.Id > 0 ? "تنشيط الإيرادات المباشرة" : "إلغاء الإيرادات المباشرة",
                    EnAction = "AddEdit",
                    ControllerName = "directRevenue",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "Post",
                    SelectedItem = directRevenue.Id,
                    ArItemName = directRevenue.ArName,
                    EnItemName = directRevenue.EnName,
                    CodeOrDocNo = directRevenue.Code
                });
                if (directRevenue.IsActive == true)
                {
                    Notification.GetNotification("directRevenue", "Activate/Deactivate", "ActivateDeactivate", id, true, "الإيرادات المباشرة");
                }
                else
                {

                    Notification.GetNotification("directRevenue", "Activate/Deactivate", "ActivateDeactivate", id, false, "الإيرادات المباشرة");
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
                    var code = db.DirectRevenues.Where(a => a.FieldsCodingId == FieldsCodingId && a.IsActive == true && a.IsDeleted == false).OrderByDescending(a => a.Id).Select(a => a.Code);
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
                    var code = db.DirectRevenues.Where(a => a.Code.Contains(fixedPart) && a.FieldsCodingId == FieldsCodingId && a.IsActive == true && a.IsDeleted == false).OrderByDescending(a => a.Id).Select(a => a.Code);
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
                var code = db.DirectRevenues.Where(a => a.FieldsCodingId == null && a.IsActive == true && a.IsDeleted == false).OrderByDescending(a => a.Id).Select(a => a.Code);
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
            //var code = QueryHelper.CodeLastNum("directRevenue");
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
