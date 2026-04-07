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

namespace MyERP.Controllers
{
    
    public class FixedAssetsGroupsController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        // GET: FixedAssetsGroups
         
        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;

            IQueryable<FixedAssetsGroup> fixedAssetsGroups;

            if (string.IsNullOrEmpty(searchWord))
            {
                fixedAssetsGroups = db.FixedAssetsGroups.Where(s => s.IsDeleted == false).OrderBy(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.FixedAssetsGroups.Where(s => s.IsDeleted == false).OrderBy(s => s.Id).Count();
            }
            else
            {
                fixedAssetsGroups = db.FixedAssetsGroups.Where(s => s.IsDeleted == false &&( s.Code.Contains(searchWord) || s.ArName.Contains(searchWord) || s.EnName.Contains(searchWord) ||s.Notes.Contains(searchWord) || s.ERPUser.Name.Contains(searchWord))).OrderBy(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.FixedAssetsGroups.Where(s => s.IsDeleted == false && (s.Code.Contains(searchWord) || s.ArName.Contains(searchWord) || s.EnName.Contains(searchWord)  || s.Notes.Contains(searchWord) || s.ERPUser.Name.Contains(searchWord))).Count();
            }
            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            ////-------------------- Notification-------------------------////
            Notification.GetNotification("FixedAssetsGroups", "View", "Index", null, null, "مجموعات الأصول الثابتة");
            return View(fixedAssetsGroups.ToList());

        }
        
        // GET: FixedAssetsGroups/Create

        public ActionResult AddEdit(int? id)
        {
            var SystemPageId = db.SystemPages.Where(a => a.IsActive == true && a.IsDeleted == false && a.ControllerName == "FixedAssetsGroups").FirstOrDefault().Id;

            ViewBag.Id = id;
            var accountsAssets = db.ChartOfAccounts.Where(x => x.IsActive == true && x.IsDeleted == false && x.ClassificationId == 3 && x.CategoryId == 1).Select(x => new { x.Id, ArName = x.Code + " - " + x.ArName });
            var accountsExpenses = db.ChartOfAccounts.Where(x => x.IsActive == true && x.IsDeleted == false && x.ClassificationId == 3 && x.CategoryId == 3).Select(x => new { x.Id, ArName = x.Code + " - " + x.ArName });
            var accountsLiabilities = db.ChartOfAccounts.Where(x => x.IsActive == true && x.IsDeleted == false && x.ClassificationId == 3 && x.CategoryId == 2).Select(x => new { x.Id, ArName = x.Code + " - " + x.ArName });
            var accountsSub = db.ChartOfAccounts.Where(x => x.IsActive == true && x.IsDeleted == false && x.ClassificationId == 3).Select(x => new { x.Id, ArName = x.Code + " - " + x.ArName });

            if (id == null)
            {
                ViewBag.FieldsCodingId = new SelectList(db.FieldsCodings.Where(a => a.IsActive == true && a.IsDeleted == false && a.PageId == SystemPageId).Select(b => new
                {
                    b.Id,
                    ArName = b.FixedPart + " - " + b.ArName
                }), "Id", "ArName");
                var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                 ViewBag.ChartOfAccountsIdOriginalAccounts = new SelectList(accountsAssets, "Id", "ArName");
                ViewBag.ChartOfAccountsIdDepracition = new SelectList(accountsExpenses, "Id", "ArName");
                ViewBag.ChartOfAccountsIdTotalDepracition = new SelectList(accountsLiabilities, "Id", "ArName");
                ViewBag.CapitalGainsAccounts = new SelectList(accountsSub, "Id", "ArName");
                ViewBag.CapitalLossesAccounts = new SelectList(accountsSub, "Id", "ArName");
                ViewBag.DepreciationMethod = new SelectList(new List<dynamic> { new { Id = 0, ArName = "قسط ثابت" }, new { Id = 1, ArName = "قسط متناقص" } }, "Id", "ArName");

                ViewBag.ParentId = new SelectList(db.FixedAssetsGroups.Where(a => (a.Type != 3 && a.IsActive == true && a.IsDeleted == false)).Select(b => new {
                    b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                return View();
            }
            FixedAssetsGroup fixedAssetsGroup = db.FixedAssetsGroups.Find(id);
            if (fixedAssetsGroup == null)
            {
                return HttpNotFound();
            }
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "فتح تفاصيل مجموعة الأصل الثابتة",
                    EnAction = "AddEdit",
                    ControllerName = "FixedAssetsGroups",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "GET",
                    SelectedItem = fixedAssetsGroup.Id,
                    ArItemName = fixedAssetsGroup.ArName,
                    EnItemName = fixedAssetsGroup.EnName,
                    CodeOrDocNo = fixedAssetsGroup.Code
                });

            ViewBag.ChartOfAccountsIdOriginalAccounts = new SelectList(accountsAssets, "Id", "ArName",fixedAssetsGroup.ChartOfAccountsIdOriginalAccounts);
            ViewBag.ChartOfAccountsIdDepracition = new SelectList(accountsExpenses, "Id", "ArName", fixedAssetsGroup.ChartOfAccountsIdDepracition);
            ViewBag.ChartOfAccountsIdTotalDepracition = new SelectList(accountsLiabilities, "Id", "ArName", fixedAssetsGroup.ChartOfAccountsIdTotalDepracition);
            ViewBag.CapitalGainsAccounts = new SelectList(accountsSub, "Id", "ArName", fixedAssetsGroup.CapitalGainsAccounts);
            ViewBag.CapitalLossesAccounts = new SelectList(accountsSub, "Id", "ArName", fixedAssetsGroup.CapitalLossesAccounts);
            ViewBag.DepreciationMethod = new SelectList(new List<dynamic> { new { Id = 0, ArName = "قسط ثابت" }, new { Id = 1, ArName = "قسط متناقص" } }, "Id", "ArName", fixedAssetsGroup.DepreciationMethod);
            ViewBag.ParentId = new SelectList(db.FixedAssetsGroups.Where(a => (a.Type != 3 && a.IsActive == true && a.IsDeleted == false)).Select(b => new {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName", fixedAssetsGroup.ParentId);
            ViewBag.FieldsCodingId = new SelectList(db.FieldsCodings.Where(a => a.IsActive == true && a.IsDeleted == false && a.PageId == SystemPageId).Select(b => new
            {
                b.Id,
                ArName = b.FixedPart + " - " + b.ArName
            }), "Id", "ArName",fixedAssetsGroup.FieldsCodingId);
            ViewBag.Next = QueryHelper.Next((int)id, "FixedAssetsGroup");
                ViewBag.Previous = QueryHelper.Previous((int)id, "FixedAssetsGroup");
                ViewBag.Last = QueryHelper.GetLast("FixedAssetsGroup");
                ViewBag.First = QueryHelper.GetFirst("FixedAssetsGroup");
                return View(fixedAssetsGroup);
           
        }
        // POST: FixedAssetsGroups/Create
        [HttpPost]
        public ActionResult AddEdit(FixedAssetsGroup fixedAssetsGroup, string newBtn)
        {
            var SystemPageId = db.SystemPages.Where(a => a.IsActive == true && a.IsDeleted == false && a.ControllerName == "FixedAssetsGroups").FirstOrDefault().Id;

            if (ModelState.IsValid)
            {
                fixedAssetsGroup.IsDeleted = false;
                var accountsAssets = db.ChartOfAccounts.Where(x => x.IsActive == true && x.IsDeleted == false && x.ClassificationId == 3 && x.CategoryId == 1).Select(x => new { x.Id, ArName = x.Code + " - " + x.ArName });
                var accountsExpenses = db.ChartOfAccounts.Where(x => x.IsActive == true && x.IsDeleted == false && x.ClassificationId == 3 && x.CategoryId == 3).Select(x => new { x.Id, ArName = x.Code + " - " + x.ArName });
                var accountsLiabilities = db.ChartOfAccounts.Where(x => x.IsActive == true && x.IsDeleted == false && x.ClassificationId == 3 && x.CategoryId == 2).Select(x => new { x.Id, ArName = x.Code + " - " + x.ArName });
                var accountsSub = db.ChartOfAccounts.Where(x => x.IsActive == true && x.IsDeleted == false && x.ClassificationId == 3).Select(x => new { x.Id, ArName = x.Code + " - " + x.ArName });

                var id = fixedAssetsGroup.Id;
                if (fixedAssetsGroup.Id > 0)
                {
                    if (fixedAssetsGroup.Type == 1)
                    {
                        fixedAssetsGroup.ParentId = null;
                        
                    }
                    fixedAssetsGroup.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                    db.Entry(fixedAssetsGroup).State = EntityState.Modified;

                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("FixedAssetsGroups", "Edit", "AddEdit", id, null, "مجموعات الأصول الثابتة");
                }
                else
                {
                    fixedAssetsGroup.IsActive = true;
                    if (fixedAssetsGroup.Type == 1)
                    {
                        fixedAssetsGroup.ParentId = null;
                    }
                    fixedAssetsGroup.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    //fixedAssetsGroup.Code= (QueryHelper.CodeLastNum("FixedAssetsGroup") + 1).ToString();
                    fixedAssetsGroup.Code = new JavaScriptSerializer().Serialize(SetCodeNum(fixedAssetsGroup.FieldsCodingId).Data).ToString().Trim('"');
                    db.FixedAssetsGroups.Add(fixedAssetsGroup);

                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("FixedAssetsGroups", "Add", "AddEdit", id, null, "مجموعات الأصول الثابتة");
                }
                try
                {
                    db.SaveChanges();
                }
                catch (Exception ex)
                {

                    var mod = ModelState.First(c => c.Key == "Code");  // this

                    mod.Value.Errors.Add("هذا الكود موجود من قبل");
                    ViewBag.ParentId = db.FixedAssetsGroups.Where(a => a.Type != 3 && a.IsActive == true && a.IsDeleted == false).ToList();
                    ViewBag.ChartOfAccountsIdOriginalAccounts = new SelectList(accountsAssets, "Id", "ArName", fixedAssetsGroup.ChartOfAccountsIdOriginalAccounts);
                    ViewBag.ChartOfAccountsIdDepracition = new SelectList(accountsExpenses, "Id", "ArName", fixedAssetsGroup.ChartOfAccountsIdDepracition);
                    ViewBag.ChartOfAccountsIdTotalDepracition = new SelectList(accountsLiabilities, "Id", "ArName", fixedAssetsGroup.ChartOfAccountsIdTotalDepracition);
                    ViewBag.CapitalGainsAccounts = new SelectList(accountsSub, "Id", "ArName", fixedAssetsGroup.CapitalGainsAccounts);
                    ViewBag.CapitalLossesAccounts = new SelectList(accountsSub, "Id", "ArName", fixedAssetsGroup.CapitalLossesAccounts);
                    ViewBag.DepreciationMethod = new SelectList(new List<dynamic> { new { Id = 0, ArName = "قسط ثابت" }, new { Id = 1, ArName = "قسط متناقص" } }, "Id", "ArName", fixedAssetsGroup.DepreciationMethod);

                    return View(fixedAssetsGroup);
                }
                    QueryHelper.AddLog(new MyLog()
                {
                    ArAction = id> 0 ? "تعديل مجموعة اصل ثابتة" : "اضافة مجموعة اصل ثابتة",
                    EnAction = "AddEdit",
                    ControllerName = "FixedAssetsGroups",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = fixedAssetsGroup.Id > 0 ? fixedAssetsGroup.Id : db.FixedAssetsGroups.Max(i => i.Id),
                    ArItemName = fixedAssetsGroup.ArName,
                    EnItemName = fixedAssetsGroup.EnName,
                    CodeOrDocNo = fixedAssetsGroup.Code
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
            else
            {
                var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .Select(x => new { x.Key, x.Value.Errors })
                    .ToArray();

                ViewBag.ParentId = db.FixedAssetsGroups.Where(a => a.Type != 3&&a.IsActive == true && a.IsDeleted == false).ToList();
                var accountsAssets = db.ChartOfAccounts.Where(x => x.IsActive == true && x.IsDeleted == false && x.ClassificationId == 3 && x.CategoryId == 1).Select(x => new { x.Id, ArName = x.Code + " - " + x.ArName });
                var accountsExpenses = db.ChartOfAccounts.Where(x => x.IsActive == true && x.IsDeleted == false && x.ClassificationId == 3 && x.CategoryId == 3).Select(x => new { x.Id, ArName = x.Code + " - " + x.ArName });
                var accountsLiabilities = db.ChartOfAccounts.Where(x => x.IsActive == true && x.IsDeleted == false && x.ClassificationId == 3 && x.CategoryId == 2).Select(x => new { x.Id, ArName = x.Code + " - " + x.ArName });
                var accountsSub = db.ChartOfAccounts.Where(x => x.IsActive == true && x.IsDeleted == false && x.ClassificationId == 3).Select(x => new { x.Id, ArName = x.Code + " - " + x.ArName });
                ViewBag.ChartOfAccountsIdOriginalAccounts = new SelectList(accountsAssets, "Id", "ArName", fixedAssetsGroup.ChartOfAccountsIdOriginalAccounts);
                ViewBag.ChartOfAccountsIdDepracition = new SelectList(accountsExpenses, "Id", "ArName", fixedAssetsGroup.ChartOfAccountsIdDepracition);
                ViewBag.ChartOfAccountsIdTotalDepracition = new SelectList(accountsLiabilities, "Id", "ArName", fixedAssetsGroup.ChartOfAccountsIdTotalDepracition);
                ViewBag.CapitalGainsAccounts = new SelectList(accountsSub, "Id", "ArName", fixedAssetsGroup.CapitalGainsAccounts);
                ViewBag.CapitalLossesAccounts = new SelectList(accountsSub, "Id", "ArName", fixedAssetsGroup.CapitalLossesAccounts);
                ViewBag.DepreciationMethod = new SelectList(new List<dynamic> { new { Id = 0, ArName = "قسط ثابت" }, new { Id = 1, ArName = "قسط متناقص" } }, "Id", "ArName", fixedAssetsGroup.DepreciationMethod);
                ViewBag.FieldsCodingId = new SelectList(db.FieldsCodings.Where(a => a.IsActive == true && a.IsDeleted == false && a.PageId == SystemPageId).Select(b => new
                {
                    b.Id,
                    ArName = b.FixedPart + " - " + b.ArName
                }), "Id", "ArName", fixedAssetsGroup.FieldsCodingId);
                return View(fixedAssetsGroup);
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
                    var code = db.FixedAssetsGroups.Where(a => a.FieldsCodingId == FieldsCodingId && a.IsActive == true && a.IsDeleted == false).OrderByDescending(a => a.Id).Select(a => a.Code);
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
                    var code = db.FixedAssetsGroups.Where(a => a.Code.Contains(fixedPart) && a.FieldsCodingId == FieldsCodingId && a.IsActive == true && a.IsDeleted == false).OrderByDescending(a => a.Id).Select(a => a.Code);
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
                var code = db.FixedAssetsGroups.Where(a => a.FieldsCodingId == null && a.IsActive == true && a.IsDeleted == false).OrderByDescending(a => a.Id).Select(a => a.Code);
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

            //var code = QueryHelper.CodeLastNum("FixedAssetsGroup");
            //return Json(code + 1, JsonRequestBehavior.AllowGet);
        }


        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                FixedAssetsGroup fixedAssetsGroup = db.FixedAssetsGroups.Find(id);
                fixedAssetsGroup.IsDeleted = true;
                fixedAssetsGroup.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                Random random = new Random();
                const string chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
                var Code = new string(Enumerable.Repeat(chars, 6).Select(s => s[random.Next(s.Length)]).ToArray());
                fixedAssetsGroup.Code = Code;
                fixedAssetsGroup.FieldsCodingId = null;
                db.Entry(fixedAssetsGroup).State = EntityState.Modified;

                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "حذف مجموعةأصل ثابتة",
                    EnAction = "AddEdit",
                    ControllerName = "FixedAssetsGroups",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    EnItemName = fixedAssetsGroup.EnName,
                    ArItemName = fixedAssetsGroup.ArName,
                    CodeOrDocNo = fixedAssetsGroup.Code
                });
                ////-------------------- Notification-------------------------////
                Notification.GetNotification("FixedAssetsGroups", "Delete", "Delete", id, null, "مجموعات الأصول الثابتة");

                //int pageid = db.Get_PageId("FixedAssetsGroups").SingleOrDefault().Value;
                //var actionId = db.PageActions.FirstOrDefault(c => c.Action == "Delete" && c.EnName == "Delete" && c.PageId == pageid).Id;
                //int userid = db.UserNotifications.FirstOrDefault(c => c.ActionId == actionId && c.PageId == pageid).UserId.Value;
                //var UserName = User.Identity.Name;
                //db.Sp_OccuredNotification(actionId, $"بحذف بيانات في شاشة مجموعات الأصول الثابتة  {UserName}قام المستخدم  ");
                ////////////////-----------------------------------------------------------------------

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
                FixedAssetsGroup fixedAssetsGroup = db.FixedAssetsGroups.Find(id);
                if (fixedAssetsGroup.IsActive == true)
                {
                    fixedAssetsGroup.IsActive = false;
                }
                else
                {
                    fixedAssetsGroup.IsActive = true;
                }
                fixedAssetsGroup.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                db.Entry(fixedAssetsGroup).State = EntityState.Modified;

                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = (bool)fixedAssetsGroup.IsActive ? "تنشيط مجموعة أصل ثابتة" : "إلغاء تنشيط مجموعة أصل ثابتة",
                    EnAction = "AddEdit",
                    ControllerName = "FixedAssetsGroups",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = fixedAssetsGroup.Id,
                    EnItemName = fixedAssetsGroup.EnName,
                    ArItemName = fixedAssetsGroup.ArName,
                    CodeOrDocNo = fixedAssetsGroup.Code
                });
                ////-------------------- Notification-------------------------////
                if (fixedAssetsGroup.IsActive == true)
                {
                    Notification.GetNotification("FixedAssetsGroups", "Activate/Deactivate", "ActivateDeactivate", id, true, "مجموعات الأصول الثابتة");
                }
                else
                {

                    Notification.GetNotification("FixedAssetsGroups", "Activate/Deactivate", "ActivateDeactivate", id, false, "مجموعات الأصول الثابتة");
                }
                //int pageid = db.Get_PageId("FixedAssetsGroups").SingleOrDefault().Value;
                //var actionId = db.PageActions.FirstOrDefault(c => c.Action == "ActivateDeactivate" && c.EnName == "Activate/Deactivate" && c.PageId == pageid).Id;
                //int userid = db.UserNotifications.FirstOrDefault(c => c.ActionId == actionId && c.PageId == pageid).UserId.Value;
                //var UserName = User.Identity.Name;
                //db.Sp_OccuredNotification(actionId, (bool)fixedAssetsGroup.IsActive ? $" تنشيط  في شاشة مجموعات الأصول الثابتة{UserName}قام المستخدم  " : $"إلغاء تنشيط  في شاشة مجموعات الأصول الثابتة{UserName}قام المستخدم  ");
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
