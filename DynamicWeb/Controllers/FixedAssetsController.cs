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
using System.Data.Entity.Validation;
using System.Web.Script.Serialization;

namespace MyERP.Controllers
{

    public class FixedAssetsController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {

            ////-------------------- Notification-------------------------////
            Notification.GetNotification("FixedAssets", "View", "Index", null, null, "الأصول الثابتة");
            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;

            IQueryable<FixedAsset> fixedAssets;

            if (string.IsNullOrEmpty(searchWord))
            {
                fixedAssets = db.FixedAssets.Where(s => s.IsDeleted == false&&s.IsActive==true).OrderBy(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.FixedAssets.Where(s => s.IsDeleted == false && s.IsActive == true).Count();
            }
            else
            {
                fixedAssets = db.FixedAssets.Where(s => s.IsDeleted == false && s.IsActive == true && (s.Code.Contains(searchWord) || s.ArName.Contains(searchWord) || s.EnName.Contains(searchWord) || s.PurchaseDate.ToString().Contains(searchWord) || s.Branch.ArName.Contains(searchWord) || s.FixedAssetsGroup.ArName.Contains(searchWord) || s.Currency.ArName.Contains(searchWord) || s.Employee.ArName.Contains(searchWord) || s.StartDepraciationDate.ToString().Contains(searchWord))).OrderBy(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.FixedAssets.Where(s => s.IsDeleted == false && s.IsActive == true && (s.Code.Contains(searchWord) || s.ArName.Contains(searchWord) || s.EnName.Contains(searchWord) || s.PurchaseDate.ToString().Contains(searchWord) || s.Branch.ArName.Contains(searchWord) || s.FixedAssetsGroup.ArName.Contains(searchWord) || s.Currency.ArName.Contains(searchWord) || s.Employee.ArName.Contains(searchWord) || s.StartDepraciationDate.ToString().Contains(searchWord))).Count();

            }
            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمةالأصول الثابتة",
                EnAction = "Index",
                ControllerName = "FixedAssets",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            return View(fixedAssets.ToList());
        }

        //AddEdit
        public ActionResult AddEdit(int? id)
        {
            var SystemPageId = db.SystemPages.Where(a => a.IsActive == true && a.IsDeleted == false && a.ControllerName == "FixedAssets").FirstOrDefault().Id;
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            var accountsAssets = db.ChartOfAccounts.Where(x => x.IsActive == true && x.IsDeleted == false && x.ClassificationId == 3 && x.CategoryId == 1).Select(x => new { x.Id, ArName = x.Code + " - " + x.ArName });
            var accountsExpenses = db.ChartOfAccounts.Where(x => x.IsActive == true && x.IsDeleted == false && x.ClassificationId == 3 && x.CategoryId == 3).Select(x => new { x.Id, ArName = x.Code + " - " + x.ArName });
            var accountsLiabilities = db.ChartOfAccounts.Where(x => x.IsActive == true && x.IsDeleted == false && x.ClassificationId == 3 && x.CategoryId == 2).Select(x => new { x.Id, ArName = x.Code + " - " + x.ArName });
            var accountsSub = db.ChartOfAccounts.Where(x => x.IsActive == true && x.IsDeleted == false && x.ClassificationId == 3).Select(x => new { x.Id, ArName = x.Code + " - " + x.ArName });
            if (id == null)
            {
                ViewBag.ChartOfAccountsIdOriginalAccounts = new SelectList(accountsAssets, "Id", "ArName");
                ViewBag.ChartOfAccountsIdDepracition = new SelectList(accountsExpenses, "Id", "ArName");
                ViewBag.ChartOfAccountsIdTotalDepracition = new SelectList(accountsLiabilities, "Id", "ArName");
                ViewBag.CapitalGainsAccounts = new SelectList(accountsSub, "Id", "ArName");
                ViewBag.CapitalLossesAccounts = new SelectList(accountsSub, "Id", "ArName");
                ViewBag.BranchId = new SelectList(db.Branches.Where(a => (a.IsActive == true && a.IsDeleted == false)).Select(b => new
                {
                    b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                ViewBag.CurrencyId = new SelectList(db.Currencies.Where(a => (a.IsActive == true && a.IsDeleted == false)).Select(b => new
                {
                    b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                ViewBag.EmployeeId = new SelectList(db.Employees.Where(a => (a.IsActive == true && a.IsDeleted == false)).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");

                ViewBag.FixedAssetTypeId = new SelectList(db.FixedAssetsTypes.Where(a => (a.IsActive == true && a.IsDeleted == false)).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");

                if (userId == 1)
                {
                    ViewBag.DepartmentId = new SelectList(db.Departments.Where(d => d.IsActive == true && d.IsDeleted == false).Select(b => new
                    {
                        b.Id,
                        ArName = b.Code + " - " + b.ArName
                    }), "Id", "ArName");
                }
                else
                {
                    ViewBag.DepartmentId = new SelectList(db.UserDepartments.Where(d => d.UserId == userId && d.Department.IsDeleted == false && d.Department.IsActive == true && d.Privilege == true).Select(b => new
                    {
                        Id = b.DepartmentId,
                        ArName = b.Department.Code + " - " + b.Department.ArName
                    }), "Id", "ArName");
                }
                ViewBag.FixedAssetsGroupId = new SelectList(db.FixedAssetsGroups.Where(a => (a.IsActive == true && a.IsDeleted == false && a.Type == 3)).Select(b => new
                {
                    b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                ViewBag.FixedAssetStatusId = new SelectList(db.FixedAssetStatus.Select(b => new
                {
                    b.Id,
                    b.ArName
                }), "Id", "ArName");
                ViewBag.DepreciationMethod = new SelectList(new List<dynamic> { new { Id = 0, ArName = "قسط ثابت" }, new { Id = 1, ArName = "قسط متناقص" } }, "Id", "ArName");
                ViewBag.FieldsCodingId = new SelectList(db.FieldsCodings.Where(a => a.IsActive == true && a.IsDeleted == false && a.PageId == SystemPageId).Select(b => new
                {
                    b.Id,
                    ArName = b.FixedPart + " - " + b.ArName
                }), "Id", "ArName");
                return View();
            }
            FixedAsset fixedAsset = db.FixedAssets.Find(id);
            if (fixedAsset == null)
            {
                return HttpNotFound();
            }
            else
            {
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "فتح تفاصيل الأصل الثابت",
                    EnAction = "AddEdit",
                    ControllerName = "FixedAssets",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "GET",
                    SelectedItem = fixedAsset.Id,
                    ArItemName = fixedAsset.ArName,
                    EnItemName = fixedAsset.EnName,
                    CodeOrDocNo = fixedAsset.Code
                });
                ViewBag.ChartOfAccountsIdOriginalAccounts = new SelectList(accountsAssets, "Id", "ArName", fixedAsset.ChartOfAccountsIdOriginalAccounts);
                ViewBag.ChartOfAccountsIdDepracition = new SelectList(accountsExpenses, "Id", "ArName", fixedAsset.ChartOfAccountsIdDepracition);
                if (userId == 1)
                {
                    ViewBag.DepartmentId = new SelectList(db.Departments.Where(d => d.IsActive == true && d.IsDeleted == false).Select(b => new
                    {
                        b.Id,
                        ArName = b.Code + " - " + b.ArName
                    }), "Id", "ArName", fixedAsset.DepartmentId);
                }
                else
                {
                    ViewBag.DepartmentId = new SelectList(db.UserDepartments.Where(d => d.UserId == userId && d.Department.IsDeleted == false && d.Department.IsActive == true && d.Privilege == true).Select(b => new
                    {
                        Id = b.DepartmentId,
                        ArName = b.Department.Code + " - " + b.Department.ArName
                    }), "Id", "ArName", fixedAsset.DepartmentId);
                }
                ViewBag.ChartOfAccountsIdTotalDepracition = new SelectList(accountsLiabilities, "Id", "ArName", fixedAsset.ChartOfAccountsIdTotalDepracition);
                ViewBag.CapitalGainsAccounts = new SelectList(accountsSub, "Id", "ArName", fixedAsset.CapitalGainsAccounts);
                ViewBag.CapitalLossesAccounts = new SelectList(accountsSub, "Id", "ArName", fixedAsset.CapitalLossesAccounts);
                ViewBag.BranchId = new SelectList(db.Branches.Where(a => (a.IsActive == true && a.IsDeleted == false)).Select(b => new
                {
                    b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName", fixedAsset.BranchId);
                ViewBag.CurrencyId = new SelectList(db.Currencies.Where(a => (a.IsActive == true && a.IsDeleted == false)).Select(b => new
                {
                    b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName", fixedAsset.CurrencyId);
                ViewBag.EmployeeId = new SelectList(db.Employees.Where(a => (a.IsActive == true && a.IsDeleted == false)).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName", fixedAsset.EmployeeId);

                ViewBag.FixedAssetTypeId = new SelectList(db.FixedAssetsTypes.Where(a => (a.IsActive == true && a.IsDeleted == false)).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName", fixedAsset.FixedAssetTypeId);
                ViewBag.FixedAssetsGroupId = new SelectList(db.FixedAssetsGroups.Where(a => (a.IsActive == true && a.IsDeleted == false && a.Type == 3)).Select(b => new
                {
                    b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName", fixedAsset.FixedAssetsGroupId);
                ViewBag.FixedAssetStatusId = new SelectList(db.FixedAssetStatus.Select(b => new
                {
                    b.Id,
                    b.ArName
                }), "Id", "ArName", fixedAsset.FixedAssetStatusId);
                ViewBag.DepreciationMethod = new SelectList(new List<dynamic> { new { Id = 0, ArName = "قسط ثابت" }, new { Id = 1, ArName = "قسط متناقص" } }, "Id", "ArName", fixedAsset.DepreciationMethod);
                ViewBag.FieldsCodingId = new SelectList(db.FieldsCodings.Where(a => a.IsActive == true && a.IsDeleted == false && a.PageId == SystemPageId).Select(b => new
                {
                    b.Id,
                    ArName = b.FixedPart + " - " + b.ArName
                }), "Id", "ArName",fixedAsset.FieldsCodingId);
                ViewBag.Next = QueryHelper.Next((int)id, "FixedAsset");
                ViewBag.Previous = QueryHelper.Previous((int)id, "FixedAsset");
                ViewBag.Last = QueryHelper.GetLast("FixedAsset");
                ViewBag.First = QueryHelper.GetFirst("FixedAsset");
                try
                {
                    ViewBag.PurchaseDate = fixedAsset.PurchaseDate.Value.ToString("yyyy-MM-dd");
                    ViewBag.StartDepraciationDate = fixedAsset.StartDepraciationDate.Value.ToString("yyyy-MM-dd");

                }
                catch (Exception)
                {
                }
                return View(fixedAsset);
            }

        }

        [HttpPost]
        public ActionResult AddEdit(FixedAsset fixedAsset, string newBtn)
        {
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

            if (ModelState.IsValid)
            {
                var id = fixedAsset.Id;
                fixedAsset.IsDeleted = false;
                if (fixedAsset.FixedAssetStatusId == null)
                    fixedAsset.FixedAssetStatusId = 1;
                ChartOfAccount ChartOfAccountsIdOriginalAccounts = db.ChartOfAccounts.Find(fixedAsset.ChartOfAccountsIdOriginalAccounts);
                ChartOfAccount ChartOfAccountsIdTotalDepracition = db.ChartOfAccounts.Find(fixedAsset.ChartOfAccountsIdTotalDepracition);

                if (fixedAsset.Id > 0)
                {
                    fixedAsset.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    var fixedAssedOld = db.FixedAssets.Where(x => x.Id == fixedAsset.Id).Select(x => new { x.ValueOfAsset, x.TotalDepracaition, x.ChartOfAccountsIdTotalDepracition, x.ChartOfAccountsIdOriginalAccounts }).FirstOrDefault();

                    db.Entry(fixedAsset).State = EntityState.Modified;
                    if (fixedAsset.IsNew != true)
                    {
                        if (fixedAssedOld.ChartOfAccountsIdOriginalAccounts == fixedAsset.ChartOfAccountsIdOriginalAccounts)
                            ChartOfAccountsIdOriginalAccounts.ObDebit = (ChartOfAccountsIdOriginalAccounts.ObDebit != null ? ChartOfAccountsIdOriginalAccounts.ObDebit : 0) - (fixedAssedOld.ValueOfAsset != null ? fixedAssedOld.ValueOfAsset : 0) + (fixedAsset.ValueOfAsset != null ? fixedAsset.ValueOfAsset : 0);
                        else
                        {
                            ChartOfAccountsIdOriginalAccounts.ObDebit = (ChartOfAccountsIdOriginalAccounts.ObDebit != null ? ChartOfAccountsIdOriginalAccounts.ObDebit : 0) + fixedAsset.ValueOfAsset != null ? fixedAsset.ValueOfAsset : 0;
                            var ChartOfAccountsIdOriginalAccountsOld = db.ChartOfAccounts.Find(fixedAssedOld.ChartOfAccountsIdOriginalAccounts);
                            ChartOfAccountsIdOriginalAccountsOld.ObDebit = (ChartOfAccountsIdOriginalAccounts.ObDebit != null ? ChartOfAccountsIdOriginalAccounts.ObDebit : 0) - (fixedAssedOld.ValueOfAsset != null ? fixedAssedOld.ValueOfAsset : 0);
                            db.Entry(ChartOfAccountsIdOriginalAccountsOld).State = EntityState.Modified;
                        }

                        if (ChartOfAccountsIdTotalDepracition != null)
                        {
                            if (fixedAssedOld.ChartOfAccountsIdTotalDepracition == fixedAsset.ChartOfAccountsIdTotalDepracition)
                                ChartOfAccountsIdTotalDepracition.ObCredit = (ChartOfAccountsIdTotalDepracition.ObCredit != null ? ChartOfAccountsIdTotalDepracition.ObCredit : 0) - (fixedAssedOld.TotalDepracaition != null ? fixedAssedOld.TotalDepracaition : 0) + (fixedAsset.TotalDepracaition != null ? fixedAsset.TotalDepracaition : 0);
                            else
                            {
                                ChartOfAccountsIdTotalDepracition.ObCredit = (ChartOfAccountsIdTotalDepracition.ObCredit != null ? ChartOfAccountsIdTotalDepracition.ObCredit : 0) + fixedAsset.TotalDepracaition != null ? fixedAsset.TotalDepracaition : 0;
                                var ChartOfAccountsIdTotalDepracitionOld = db.ChartOfAccounts.Find(fixedAssedOld.ChartOfAccountsIdTotalDepracition);
                                ChartOfAccountsIdTotalDepracitionOld.ObDebit = (ChartOfAccountsIdTotalDepracition.ObCredit != null ? ChartOfAccountsIdTotalDepracition.ObCredit : 0) - (fixedAssedOld.TotalDepracaition != null ? fixedAsset.TotalDepracaition : 0);
                                db.Entry(ChartOfAccountsIdTotalDepracitionOld).State = EntityState.Modified;
                            }
                            db.Entry(ChartOfAccountsIdTotalDepracition).State = EntityState.Modified;
                        }
                        db.Entry(ChartOfAccountsIdOriginalAccounts).State = EntityState.Modified;
                    }
                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("FixedAssets", "Edit", "AddEdit", id, null, "الأصول الثابتة");

                    //////////-----------------------------------------------------------------------
                }
                else
                {
                    fixedAsset.IsActive = true;
                    fixedAsset.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    // fixedAsset.Code = (QueryHelper.CodeLastNum("FixedAsset") + 1).ToString();
                    fixedAsset.Code = new JavaScriptSerializer().Serialize(SetCodeNum(fixedAsset.FieldsCodingId).Data).ToString().Trim('"');
                    db.FixedAssets.Add(fixedAsset);
                    if (fixedAsset.IsNew != true)
                    {
                        ChartOfAccountsIdOriginalAccounts.ObDebit = (ChartOfAccountsIdOriginalAccounts.ObDebit != null ? ChartOfAccountsIdOriginalAccounts.ObDebit : 0) + fixedAsset.ValueOfAsset != null ? fixedAsset.ValueOfAsset : 0;
                        if (ChartOfAccountsIdTotalDepracition != null)
                        {
                            ChartOfAccountsIdTotalDepracition.ObCredit = (ChartOfAccountsIdTotalDepracition.ObCredit != null ? ChartOfAccountsIdTotalDepracition.ObCredit : 0) + fixedAsset.TotalDepracaition != null ? fixedAsset.TotalDepracaition : 0;
                            db.Entry(ChartOfAccountsIdTotalDepracition).State = EntityState.Modified;
                        }
                        db.Entry(ChartOfAccountsIdOriginalAccounts).State = EntityState.Modified;
                    }
                    ////-------------------- Notification-------------------------////

                    Notification.GetNotification("FixedAssets", "Add", "AddEdit", id, null, "الأصول الثابتة");

                    ////////////////-----------------------------------------------------------------------

                }
                db.SaveChanges();
                // Your code...
                // Could also be before try if you know the exception occurs in SaveChanges

                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = id > 0 ? "تعديل أصل الثابت" : "اضافة أصل الثابت",
                    EnAction = "AddEdit",
                    ControllerName = "FixedAssets",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = fixedAsset.Id > 0 ? fixedAsset.Id : db.FixedAssets.Max(i => i.Id),
                    ArItemName = fixedAsset.ArName,
                    EnItemName = fixedAsset.EnName,
                    CodeOrDocNo = fixedAsset.Code
                });
                if (newBtn == "saveAndNew")
                    return RedirectToAction("AddEdit");
                else
                    return RedirectToAction("Index");
            }
            else
            {
                var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .Select(x => new { x.Key, x.Value.Errors })
                    .ToArray();
                var accounts = db.ChartOfAccounts.Where(x => x.IsActive == true && x.IsDeleted == false && x.ClassificationId == 3).Select(x => new { x.Id, ArName = x.Code + " - " + x.ArName });
                ViewBag.ChartOfAccountsIdOriginalAccounts = new SelectList(accounts, "Id", "ArName", fixedAsset.ChartOfAccountsIdOriginalAccounts);
                ViewBag.ChartOfAccountsIdDepracition = new SelectList(accounts, "Id", "ArName", fixedAsset.ChartOfAccountsIdDepracition);
                if (userId == 1)
                {
                    ViewBag.DepartmentId = new SelectList(db.Departments.Where(d => d.IsActive == true && d.IsDeleted == false).Select(b => new
                    {
                        b.Id,
                        ArName = b.Code + " - " + b.ArName
                    }), "Id", "ArName", fixedAsset.DepartmentId);
                }
                else
                {
                    ViewBag.DepartmentId = new SelectList(db.UserDepartments.Where(d => d.UserId == userId && d.Department.IsDeleted == false && d.Department.IsActive == true && d.Privilege == true).Select(b => new
                    {
                        Id = b.DepartmentId,
                        ArName = b.Department.Code + " - " + b.Department.ArName
                    }), "Id", "ArName", fixedAsset.DepartmentId);
                }
                ViewBag.ChartOfAccountsIdTotalDepracition = new SelectList(accounts, "Id", "ArName", fixedAsset.ChartOfAccountsIdTotalDepracition);
                ViewBag.CapitalGainsAccounts = new SelectList(accounts, "Id", "ArName", fixedAsset.CapitalGainsAccounts);
                ViewBag.CapitalLossesAccounts = new SelectList(accounts, "Id", "ArName", fixedAsset.CapitalLossesAccounts);
                ViewBag.BranchId = new SelectList(db.Branches.Where(a => (a.IsActive == true && a.IsDeleted == false)).Select(b => new
                {
                    b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName", fixedAsset.BranchId);
                ViewBag.CurrencyId = new SelectList(db.Currencies.Where(a => (a.IsActive == true && a.IsDeleted == false)).Select(b => new
                {
                    b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName", fixedAsset.CurrencyId);
                ViewBag.EmployeeId = new SelectList(db.Employees.Where(a => (a.IsActive == true && a.IsDeleted == false)).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName", fixedAsset.EmployeeId);

                ViewBag.FixedAssetTypeId = new SelectList(db.FixedAssetsTypes.Where(a => (a.IsActive == true && a.IsDeleted == false)).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName", fixedAsset.FixedAssetTypeId);
                ViewBag.FixedAssetsGroupId = new SelectList(db.FixedAssetsGroups.Where(a => (a.IsActive == true && a.IsDeleted == false && a.Type == 3)).Select(b => new
                {
                    b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName", fixedAsset.FixedAssetsGroupId);
                ViewBag.FixedAssetStatusId = new SelectList(db.FixedAssetStatus.Select(b => new
                {
                    b.Id,
                    b.ArName
                }), "Id", "ArName", fixedAsset.FixedAssetStatusId);
                ViewBag.DepreciationMethod = new SelectList(new List<dynamic> { new { Id = 0, ArName = "قسط ثابت" }, new { Id = 1, ArName = "قسط متناقص" } }, "Id", "ArName", fixedAsset.DepreciationMethod);

                try
                {
                    ViewBag.PurchaseDate = fixedAsset.PurchaseDate.Value.ToString("yyyy-MM-dd");
                    ViewBag.StartDepraciationDate = fixedAsset.StartDepraciationDate.Value.ToString("yyyy-MM-dd");

                }
                catch (Exception)
                {
                }

                return View(fixedAsset);
            }



        }

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                //-- Check if this FixedAsset is used in other Transactions 
                var check = db.CheckFixedAssetExistanceInOtherTransactions(id).FirstOrDefault();
                if (check > 0)
                {
                    return Content("False");
                }
                else
                {
                    FixedAsset fixedAsset = db.FixedAssets.Find(id);
                    fixedAsset.IsDeleted = true;
                    fixedAsset.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    Random random = new Random();
                    const string chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
                    var Code = new string(Enumerable.Repeat(chars, 6).Select(s => s[random.Next(s.Length)]).ToArray());
                    fixedAsset.Code = Code;
                    fixedAsset.FieldsCodingId = null;
                    db.Entry(fixedAsset).State = EntityState.Modified;
                    var fixedAssedOld = db.FixedAssets.Where(x => x.Id == fixedAsset.Id).Select(x => new { x.ValueOfAsset, x.TotalDepracaition, x.ChartOfAccountsIdTotalDepracition, x.ChartOfAccountsIdOriginalAccounts }).FirstOrDefault();

                    if (fixedAsset.IsNew != true)
                    {
                        ChartOfAccount ChartOfAccountsIdTotalDepracition = db.ChartOfAccounts.Find(fixedAsset.ChartOfAccountsIdTotalDepracition);
                        ChartOfAccount ChartOfAccountsIdOriginalAccounts = db.ChartOfAccounts.Find(fixedAsset.ChartOfAccountsIdOriginalAccounts);

                        if (ChartOfAccountsIdOriginalAccounts != null)
                        {
                            ChartOfAccountsIdOriginalAccounts.ObDebit = (fixedAsset.ValueOfAsset != null ? fixedAsset.ValueOfAsset : 0) - ((ChartOfAccountsIdOriginalAccounts.ObDebit != null ? ChartOfAccountsIdOriginalAccounts.ObDebit : 0) - (fixedAssedOld.ValueOfAsset != null ? fixedAssedOld.ValueOfAsset : 0) + (fixedAsset.ValueOfAsset != null ? fixedAsset.ValueOfAsset : 0));
                            db.Entry(ChartOfAccountsIdOriginalAccounts).State = EntityState.Modified;
                        }
                        if (ChartOfAccountsIdTotalDepracition != null)
                        {
                            ChartOfAccountsIdTotalDepracition.ObCredit = (fixedAsset.TotalDepracaition != null ? fixedAsset.TotalDepracaition : 0) - ((ChartOfAccountsIdTotalDepracition.ObCredit != null ? ChartOfAccountsIdTotalDepracition.ObCredit : 0) - (fixedAssedOld.TotalDepracaition != null ? fixedAssedOld.TotalDepracaition : 0) + (fixedAsset.TotalDepracaition != null ? fixedAsset.TotalDepracaition : 0));
                            db.Entry(ChartOfAccountsIdTotalDepracition).State = EntityState.Modified;
                        }
                    }

                    db.SaveChanges();
                    QueryHelper.AddLog(new MyLog()
                    {
                        ArAction = "حذف أصل ثابت",
                        EnAction = "AddEdit",
                        ControllerName = "FixedAssets",
                        UserName = User.Identity.Name,
                        UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                        LogDate = DateTime.Now,
                        RequestMethod = "POST",
                        SelectedItem = id,
                        EnItemName = fixedAsset.EnName,
                        ArItemName = fixedAsset.ArName,
                        CodeOrDocNo = fixedAsset.Code
                    });

                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("FixedAssets", "Delete", "Delete", id, null, " الأصول الثابتة");

                    return Content("true");
                }
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

                FixedAsset fixedAsset = db.FixedAssets.Find(id);
                if (fixedAsset.IsActive == true)
                {
                    fixedAsset.IsActive = false;
                }
                else
                {
                    fixedAsset.IsActive = true;
                }
                fixedAsset.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                db.Entry(fixedAsset).State = EntityState.Modified;

                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = (bool)fixedAsset.IsActive ? "تنشيط أصل ثابت" : "إلغاء تنشيط أصل ثابت",
                    EnAction = "AddEdit",
                    ControllerName = "FixedAssets",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = fixedAsset.Id,
                    EnItemName = fixedAsset.EnName,
                    ArItemName = fixedAsset.ArName,
                    CodeOrDocNo = fixedAsset.Code
                });
                ////-------------------- Notification-------------------------////
                if (fixedAsset.IsActive == true)
                {
                    Notification.GetNotification("FixedAssets", "Activate/Deactivate", "ActivateDeactivate", id, true, "الاصول الثابتة");
                }
                else
                {
                    Notification.GetNotification("FixedAssets", "Activate/Deactivate", "ActivateDeactivate", id, false, "الاصول الثابتة");
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
                    var code = db.FixedAssets.Where(a => a.FieldsCodingId == FieldsCodingId && a.IsActive == true && a.IsDeleted == false).OrderByDescending(a => a.Id).Select(a => a.Code);
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
                    var code = db.FixedAssets.Where(a => a.Code.Contains(fixedPart) && a.FieldsCodingId == FieldsCodingId && a.IsActive == true && a.IsDeleted == false).OrderByDescending(a => a.Id).Select(a => a.Code);
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
                var code = db.FixedAssets.Where(a => a.FieldsCodingId == null && a.IsActive == true && a.IsDeleted == false).OrderByDescending(a => a.Id).Select(a => a.Code);
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
            //var code = QueryHelper.CodeLastNum("FixedAsset");
            //return Json(code + 1, JsonRequestBehavior.AllowGet);
        }
       
        [SkipERPAuthorize]
        public float GetCurrancyEquivalent(int currencyId)
        {
            var currencyObj = db.Currencies.Find(currencyId);
            var equivalent = currencyObj.Equivalent;
            return (float)equivalent;
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
