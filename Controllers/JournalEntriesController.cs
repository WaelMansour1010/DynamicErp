using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using MyERP.Models;
using System.Threading.Tasks;
using System.Security.Claims;
using MyERP.Repository;
using System.Web.Script.Serialization;

namespace MyERP.Controllers
{
    public class JournalEntriesController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        // GET: JournalEntries
        public async Task<ActionResult> Index(DateTime? dateFrom, DateTime? dateTo, int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            DepartmentRepository departmentRepository = new DepartmentRepository(db);
            Repository<JournalEntry> repository = new Repository<JournalEntry>(db);
            var depIds = await departmentRepository.UserDepartmentsIds(userId);
            ////-------------------- Notification-------------------------////
            Notification.GetNotification("JournalEntries", "View", "Index", null, null, "القيود");

            ViewBag.PageIndex = pageIndex;

            //DateTime utcNow = DateTime.UtcNow;
            //TimeZoneInfo info = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");
            //DateTime cTime = TimeZoneInfo.ConvertTimeFromUtc(utcNow, info);
            //ViewBag.Date = cTime.ToString("yyyy-MM-ddTHH:mm");

            //try
            //{
            //    ViewBag.dateFrom = db.JournalEntries.Min(j => j.Date).ToString("yyyy-MM-ddTHH:mm");
            //}
            //catch (Exception)
            //{
            //    ViewBag.dateFrom = DateTime.MinValue.AddYears(1970).ToString("yyyy-MM-ddTHH:mm");
            //}



            if (dateFrom == null)
            {
                try
                {
                    ViewBag.dateFrom = db.JournalEntries.Min(j => j.Date).ToString("yyyy-MM-ddTHH:mm");
                }
                catch (Exception)
                {
                    ViewBag.dateFrom = DateTime.MinValue.AddYears(1970).ToString("yyyy-MM-ddTHH:mm");
                }
            }
            else
                ViewBag.dateFrom = DateTime.Parse(dateFrom.ToString()).ToString("yyyy-MM-ddTHH:mm");
            if (dateTo == null)
            {
                try
                {
                    ViewBag.dateTo = db.JournalEntries.Max(j => j.Date).ToString("yyyy-MM-ddTHH:mm");
                }
                catch (Exception)
                {
                    ViewBag.dateFrom = DateTime.Now.ToString("yyyy-MM-ddTHH:mm");
                }
            }
            else

                ViewBag.dateTo = DateTime.Parse(dateTo.ToString()).ToString("yyyy-MM-ddTHH:mm");

            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;


            IQueryable<JournalEntry> journalEntries;
            IQueryable<int> journalEntriesDetails;
            if (string.IsNullOrEmpty(searchWord))
            {
                journalEntries = repository.GetAll().Where(s => s.IsDeleted == false && depIds.Contains(s.DepartmentId) && (dateFrom == null || s.Date > dateFrom) && (dateTo == null || s.Date < dateTo) && depIds.Contains(s.DepartmentId)).OrderByDescending(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = await repository.GetAll().Where(s => s.IsDeleted == false && depIds.Contains(s.DepartmentId) && (dateFrom == null || s.Date > dateFrom) && (dateTo == null || s.Date < dateTo) && depIds.Contains(s.DepartmentId)).CountAsync();
            }
            else
            {
                journalEntriesDetails = db.JournalEntryDetails.Where(d => d.Notes.Contains(searchWord) || d.Debit.ToString().Replace(".00", "") == searchWord || d.Credit.ToString().Replace(".00", "") == searchWord || d.ChartOfAccount.ArName.Contains(searchWord) || d.ChartOfAccount.Code.Contains(searchWord)).Select(d => d.JournalEntryId);
                int x = db.JournalEntryDetails.Where(d => d.Notes.Contains(searchWord) || d.Debit.ToString() == searchWord || d.Credit.ToString() == searchWord).Select(d => d.Id).Count();
                journalEntries = repository.GetAll().Where(s => s.IsDeleted == false && depIds.Contains(s.DepartmentId) && (dateFrom == null || s.Date > dateFrom) && (dateTo == null || s.Date < dateTo) && (s.DocumentNumber.Contains(searchWord) || s.Company.ArName.Contains(searchWord) || s.Branch.ArName.Contains(searchWord) || s.Notes.Contains(searchWord) || s.Currency.ArName.Contains(searchWord) || s.Department.ArName.Contains(searchWord) || journalEntriesDetails.Contains(s.Id))).OrderByDescending(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = await repository.GetAll().Where(s => s.IsDeleted == false && depIds.Contains(s.DepartmentId) && (dateFrom == null || s.Date > dateFrom) && (dateTo == null || s.Date < dateTo) && (s.DocumentNumber.Contains(searchWord) || s.Company.ArName.Contains(searchWord) || s.Branch.ArName.Contains(searchWord) || s.Notes.Contains(searchWord) || s.Currency.ArName.Contains(searchWord) || s.Department.ArName.Contains(searchWord) || journalEntriesDetails.Contains(s.Id))).CountAsync();
            }

            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة القيود",
                EnAction = "Index",
                ControllerName = "JournalEntries",
                UserName = User.Identity.Name,
                UserId = userId,
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });

            return View(await journalEntries.ToListAsync());
        }
       
        public async Task<ActionResult> AddEdit(int? id)
        {
            SystemSetting systemSetting = db.SystemSettings.Any() ? db.SystemSettings.FirstOrDefault() : new SystemSetting();
            DepartmentRepository departmentRepository = new DepartmentRepository(db);

            ViewBag.UseCostCenter = systemSetting.UseCostCenter;
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

            if (id == null)
            {
                JournalEntry Newobj = new JournalEntry();
                ViewBag.DepartmentId = new SelectList(await departmentRepository.UserDepartments(userId).ToListAsync(), "Id", "ArName");

                ViewBag.Accounts = new SelectList(db.ChartOfAccounts.Where(a => a.ClassificationId == 3 && a.IsActive == true && a.IsDeleted == false).Select(b => new
                {
                    b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
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
                return View(Newobj);
            }

            JournalEntry journalEntry = db.JournalEntries.Find(id);
            if (journalEntry == null)
            {
                return HttpNotFound();
            }

            try
            {
                if (journalEntry.SourcePageId == 21)
                {
                    var cashIssue = db.CashIssueVouchers.Where(c => c.Id == journalEntry.SourceId).FirstOrDefault();
                    if (cashIssue.SourceTypeId == 1)
                    {

                        foreach (var item in journalEntry.JournalEntryDetails)
                        {
                            if (item.Debit > 0)
                                item.Notes += "-" + cashIssue.Customer.ArName;
                        }
                    }
                    else if (cashIssue.SourceTypeId == 2)
                    {

                        foreach (var item in journalEntry.JournalEntryDetails)
                        {
                            if (item.Debit > 0)
                                item.Notes += "-" + cashIssue.Vendor.ArName;
                        }
                    }

                }
                else if (journalEntry.SourcePageId == 22)
                {
                    var cashReceipt = db.CashReceiptVouchers.Where(c => c.Id == journalEntry.SourceId).FirstOrDefault();
                    if (cashReceipt.SourceTypeId == 1)
                    {

                        foreach (var item in journalEntry.JournalEntryDetails)
                        {
                            if (item.Credit > 0)
                                item.Notes += "-" + cashReceipt.Customer.ArName;
                        }
                    }
                    else if (cashReceipt.SourceTypeId == 2)
                    {

                        foreach (var item in journalEntry.JournalEntryDetails)
                        {
                            if (item.Credit > 0)
                                item.Notes += "-" + cashReceipt.Vendor.ArName;
                        }
                    }
                }
            }
            catch { }
            if (journalEntry.SourceId != null && journalEntry.SourcePageId != null)
            {
                var sysPage = db.SystemPages.Where(a => a.Id == journalEntry.SourcePageId).FirstOrDefault();
                var table = sysPage.TableName;
                //journalEntry.SourceId = int.Parse((db.Database.SqlQuery<string>($"select top(1)([DocumentNumber]) from[{ table}] where [Id] = " + journalEntry.SourceId).FirstOrDefault()).Replace("-",""));
                ViewBag.SourceId = /*int.Parse(*/(db.Database.SqlQuery<string>($"select top(1)([DocumentNumber]) from[{ table}] where [Id] = " + journalEntry.SourceId).FirstOrDefault()).Replace("-","")/*)*/;

            }
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح تفاصيل القيد ",
                EnAction = "AddEdit",
                ControllerName = "JournalEntries",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = journalEntry.Id,

                CodeOrDocNo = journalEntry.DocumentNumber
            });

            ViewBag.DepartmentId = new SelectList(await departmentRepository.UserDepartments(userId).ToListAsync(), "Id", "ArName", journalEntry.DepartmentId);

            ViewBag.Accounts = new SelectList(db.ChartOfAccounts.Where(a => a.ClassificationId == 3 && a.IsActive == true && a.IsDeleted == false).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName");
            ViewBag.Date = journalEntry.Date.ToString("yyyy-MM-ddTHH:mm");
            ViewBag.Next = QueryHelper.Next((int)id, "JournalEntry");
            ViewBag.Previous = QueryHelper.Previous((int)id, "JournalEntry");
            ViewBag.Last = QueryHelper.GetLast("JournalEntry");
            ViewBag.First = QueryHelper.GetFirst("JournalEntry");
            return View(journalEntry);
        }

        [HttpPost]
        public async Task<JsonResult> AddEdit(JournalEntry journalEntry)
        {
            if (ModelState.IsValid)
            {
                var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                var id = journalEntry.Id;
                if (id == 0)
                {
                    journalEntry.DocumentNumber = new JavaScriptSerializer().Serialize(SetDocNum(journalEntry.DepartmentId, journalEntry.Date).Data).ToString().Trim('"');
                    db.JournalEntries.Add(journalEntry);    
                }
                else
                {
                    db.JournalEntryDetails.RemoveRange(db.JournalEntryDetails.Where(x => x.JournalEntryId == journalEntry.Id));
                    List<JournalEntryDetail> journalEntryDetails = journalEntry.JournalEntryDetails.ToList();
                    journalEntry.JournalEntryDetails = null;
                    db.Entry(journalEntry).State = EntityState.Modified;
                    db.JournalEntryDetails.AddRange(journalEntryDetails);
                    QueryHelper.AddLog(new MyLog()
                    {
                        ArAction = "تعديل قيد",
                        EnAction = "AddEdit",
                        ControllerName = "JournalEntries",
                        UserName = User.Identity.Name,
                        UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                        LogDate = DateTime.Now,
                        RequestMethod = "POST",
                        SelectedItem = journalEntry.Id,
                        CodeOrDocNo = journalEntry.DocumentNumber
                    });

                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("JournalEntries", "Edit", "InsertJournalEntry", id, null, "القيود");

                    //////////-----------------------------------------------------------------------
                }
                await db.SaveChangesAsync();
                if (id == 0)
                {
                    id = db.JournalEntries.Max(j => j.Id);
                    QueryHelper.AddLog(new MyLog()
                    {
                        ArAction = "اضافة قيد",
                        EnAction = "AddEdit",
                        ControllerName = "JournalEntries",
                        UserName = User.Identity.Name,
                        UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                        LogDate = DateTime.Now,
                        RequestMethod = "POST",
                        SelectedItem = db.JournalEntries.Max(i => i.Id),
                        CodeOrDocNo = journalEntry.DocumentNumber
                    });

                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("JournalEntries", "Add", "InsertJournalEntry", id, null, "القيود");
                    /////////////-----------------------------------------------------------------------
                }
                return Json(new { Id = id, success = "true" });
            }
            var errors = ModelState
                   .Where(x => x.Value.Errors.Count > 0)
                   .Select(x => new { x.Key, x.Value.Errors })
                   .ToArray();

            return Json(new { success = "false", errors });
        }
        //to fill Accounts DDL in View script
        [SkipERPAuthorize]
        public JsonResult GetAccounts()
        {
            return Json(db.ChartOfAccounts.Where(i => i.IsDeleted == false && i.IsActive == true && i.ClassificationId == 3).Select(i => new
            {
                ArName = i.Code + " - " + i.ArName,
                i.Id,
                i.CategoryId
            }), JsonRequestBehavior.AllowGet);
        }

        [SkipERPAuthorize]
        public async Task<JsonResult> GetCostCenters()
        {
            return Json(await db.CostCenters.Where(i => i.IsDeleted == false && i.IsActive == true && i.TypeId == 2).Select(i => new
            {
                ArName = i.Code + " - " + i.ArName,
                i.Id,
            }).ToListAsync(), JsonRequestBehavior.AllowGet);
        }

        // POST: ReceiptAndPaymentVoucher/Delete/5
        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            JournalEntry JournalEntry = db.JournalEntries.Find(id);
            List<JournalEntryDetail> journalEntryDetails = db.JournalEntryDetails.Where(a => a.JournalEntryId == id).ToList();
            foreach (var journalEntryDetail in journalEntryDetails)
            {
                journalEntryDetail.IsDeleted = true;

            }
            JournalEntry.IsDeleted = true;
            JournalEntry.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            Random random = new Random();
            const string chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            var Code = new string(Enumerable.Repeat(chars, 6).Select(s => s[random.Next(s.Length)]).ToArray());
            JournalEntry.DocumentNumber = Code;

            db.Entry(JournalEntry).State = EntityState.Modified;

            db.SaveChanges();
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "حذف القيد",
                EnAction = "AddEdit",
                ControllerName = "JournalEntries",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "POST",
                SelectedItem = id,

                CodeOrDocNo = JournalEntry.DocumentNumber
            });

            ////-------------------- Notification-------------------------////
            Notification.GetNotification("JournalEntries", "Delete", "Delete", id, null, "القيد");

            return Content("true");
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
            var lastObj = db.JournalEntries.Where(a => a.IsActive == true && a.IsDeleted == false && a.DepartmentId == id).OrderByDescending(a => a.Id).FirstOrDefault();
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
                    lastObj = db.JournalEntries.Where(a => a.IsActive == true && a.IsDeleted == false && a.DepartmentId == id && a.Date.Month == VoucherDate.Value.Month && a.Date.Year == VoucherDate.Value.Year).OrderByDescending(a => a.Id).FirstOrDefault();
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
                    lastObj = db.JournalEntries.Where(a => a.IsActive == true && a.IsDeleted == false && a.DepartmentId == id && a.Date.Year == VoucherDate.Value.Year).OrderByDescending(a => a.Id).FirstOrDefault();

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

            //var docNo = QueryHelper.DocLastNum(id, "JournalEntry");
            //double i = (docNo) + 1;
            //return Json(i, JsonRequestBehavior.AllowGet);
        }

        [SkipERPAuthorize]
        public JsonResult CheckValueAddedTaxes(int? AccountId, int? DepartmentId)
        {
            var IncludeValueAddedTax = db.ChartOfAccounts.Where(a => a.IsActive == true && a.IsDeleted == false && a.Id == AccountId).FirstOrDefault().IncludeValueAddedTax;
            var TaxAccount = db.Departments.Where(a => a.IsActive == true && a.IsDeleted == false && a.Id == DepartmentId).Select(a=>new {
            a.TaxAccountId,
            TaxAccountArName = db.ChartOfAccounts.Where(b=>b.Id==a.TaxAccountId).Select(b=>b.ArName).FirstOrDefault()}).FirstOrDefault();
            return Json(new { IncludeValueAddedTax= IncludeValueAddedTax, TaxAccount= TaxAccount }, JsonRequestBehavior.AllowGet);
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
