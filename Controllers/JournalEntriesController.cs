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
            searchWord = (searchWord ?? string.Empty).Trim();

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



            var today = DateTime.Today;
            var effectiveDateFrom = dateFrom ?? today;
            var displayDateTo = dateTo ?? today.AddHours(23).AddMinutes(59);
            var effectiveDateTo = dateTo.HasValue
                ? (dateTo.Value.TimeOfDay == TimeSpan.Zero ? dateTo.Value.Date.AddDays(1).AddTicks(-1) : dateTo.Value.AddMinutes(1).AddTicks(-1))
                : today.AddDays(1).AddTicks(-1);

            ViewBag.dateFrom = effectiveDateFrom.ToString("yyyy-MM-ddTHH:mm");
            ViewBag.dateTo = displayDateTo.ToString("yyyy-MM-ddTHH:mm");

            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;


            IQueryable<JournalEntry> journalEntries;

            // فلتر أساسي يتكرر في الحالتين
            var baseQuery =
                repository.GetAll()
                          .Where(s =>
                                 !s.IsDeleted &&
                                 depIds.Contains(s.DepartmentId) &&
                                 s.Date >= effectiveDateFrom &&
                                 s.Date <= effectiveDateTo);

            if (string.IsNullOrEmpty(searchWord))
            {
                ViewBag.Count = await baseQuery.CountAsync();
                journalEntries = baseQuery
                                    .OrderByDescending(s => s.Id)
                                    .Skip(skipRowsNo)
                                    .Take(wantedRowsNo);
            }
            else
            {
                var word = searchWord;
                var normalizedWord = word.Replace("-", "");
                decimal numericWord;
                var hasNumericWord = decimal.TryParse(word, out numericWord);

                // مطابقات تفاصيل القيد الحالية (ملاحظتك الأصلية)
                var detailsMatches =
                    db.JournalEntryDetails
                      .Where(d =>
                             !d.IsDeleted &&
                             ((d.Notes != null && d.Notes.Contains(word)) ||
                             (hasNumericWord && (d.Debit == numericWord || d.Credit == numericWord)) ||
                             (d.ChartOfAccount != null &&
                                ((d.ChartOfAccount.ArName != null && d.ChartOfAccount.ArName.Contains(word)) ||
                                 (d.ChartOfAccount.Code != null && d.ChartOfAccount.Code.Contains(word))))))
                      .Select(d => d.JournalEntryId);

                // ====== مطابقات بالطرف PartyId/PartyType ======
                var jedByVendors =
                    from d in db.JournalEntryDetails
                    where !d.IsDeleted && d.PartyType == 1 && d.PartyId != null
                    join v in db.Vendors on d.PartyId equals v.Id
                    where ((v.ArName != null && v.ArName.Contains(word)) || (v.EnName != null && v.EnName.Contains(word)))
                    select d.JournalEntryId;

                var jedByCustomers =
                    from d in db.JournalEntryDetails
                    where !d.IsDeleted && d.PartyType == 2 && d.PartyId != null
                    join c in db.Customers on d.PartyId equals c.Id
                    where ((c.ArName != null && c.ArName.Contains(word)) || (c.EnName != null && c.EnName.Contains(word)))
                    select d.JournalEntryId;

                var jedByEmployees =
                    from d in db.JournalEntryDetails
                    where !d.IsDeleted && d.PartyType == 3 && d.PartyId != null
                    join e in db.Employees on d.PartyId equals e.Id
                    where ((e.ArName != null && e.ArName.Contains(word)) || (e.EnName != null && e.EnName.Contains(word)))
                    select d.JournalEntryId;

                var jedByRenters =
                    from d in db.JournalEntryDetails
                    where !d.IsDeleted && d.PartyType == 4 && d.PartyId != null
                    join r in db.PropertyRenters on d.PartyId equals r.Id
                    where ((r.ArName != null && r.ArName.Contains(word)) || (r.EnName != null && r.EnName.Contains(word)))
                    select d.JournalEntryId;

                var jedByOwners =
                    from d in db.JournalEntryDetails
                    where !d.IsDeleted && d.PartyType == 5 && d.PartyId != null
                    join o in db.PropertyOwners on d.PartyId equals o.Id
                    where ((o.ArName != null && o.ArName.Contains(word)) || (o.EnName != null && o.EnName.Contains(word)))
                    select d.JournalEntryId;

                var partyMatches = jedByVendors
                                    .Concat(jedByCustomers)
                                    .Concat(jedByEmployees)
                                    .Concat(jedByRenters)
                                    .Concat(jedByOwners);

                var allDetailMatches = detailsMatches
                                        .Concat(partyMatches)
                                        .Distinct();

                var searchQuery =
                    baseQuery.Where(s =>
                        (s.DocumentNumber != null && (s.DocumentNumber.Contains(word) || s.DocumentNumber.Replace("-", "").Contains(normalizedWord))) ||
                        (s.Company != null && s.Company.ArName != null && s.Company.ArName.Contains(word)) ||
                        (s.Branch != null && s.Branch.ArName != null && s.Branch.ArName.Contains(word)) ||
                        (s.Notes != null && s.Notes.Contains(word)) ||
                        (s.Currency != null && s.Currency.ArName != null && s.Currency.ArName.Contains(word)) ||
                        (s.Department != null && s.Department.ArName != null && s.Department.ArName.Contains(word)) ||
                        (s.SystemPage != null && s.SystemPage.ArName != null && s.SystemPage.ArName.Contains(word)) ||
                        allDetailMatches.Contains(s.Id));

                ViewBag.Count = await searchQuery.CountAsync();
                journalEntries = searchQuery
                                    .OrderByDescending(s => s.Id)
                                    .Skip(skipRowsNo)
                                    .Take(wantedRowsNo);
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
                var departmentsRaw = await departmentRepository.UserDepartments(userId).ToListAsync();
                var departments = departmentsRaw.Select(d => new
                {
                    Id = (int)d.Id,
                    Code = (string)d.Code,
                    ArName = (string)d.ArName
                }).ToList();

                ViewBag.DepartmentId = new SelectList(departments, "Id", "ArName");
                ViewBag.DepartmentsJson = new JavaScriptSerializer().Serialize(departments);

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

            var editDepartmentsRaw = await departmentRepository.UserDepartments(userId).ToListAsync();
            var editDepartments = editDepartmentsRaw.Select(d => new
            {
                Id = (int)d.Id,
                Code = (string)d.Code,
                ArName = (string)d.ArName
            }).ToList();

            ViewBag.DepartmentId = new SelectList(editDepartments, "Id", "ArName", journalEntry.DepartmentId);
            ViewBag.DepartmentsJson = new JavaScriptSerializer().Serialize(editDepartments);

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
            try
            {
                var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                if (journalEntry == null)
                    return Json(new { success = "false", message = "بيانات القيد غير صحيحة." });

                var id = journalEntry.Id;

                var dep = db.Departments.FirstOrDefault(d => d.Id == journalEntry.DepartmentId);
                var details = NormalizeJournalEntryDetails(journalEntry, dep);

                if (id == 0)
                {
                    // توليد رقم المستند
                    journalEntry.DocumentNumber = new JavaScriptSerializer()
                        .Serialize(SetDocNum(journalEntry.DepartmentId, journalEntry.Date).Data)
                        .Trim('"');
                    journalEntry.UserId = userId;
                    journalEntry.IsActive = true;
                    journalEntry.IsDeleted = false;
                    journalEntry.JournalEntryDetails = details;

                    // إضافة القيد (الهيدر + التفاصيل القادمة من الـ View)
                    db.JournalEntries.Add(journalEntry);
                    await db.SaveChangesAsync();

                    id = journalEntry.Id;

                    // Log + Notification
                    QueryHelper.AddLog(new MyLog
                    {
                        ArAction = "اضافة قيد",
                        EnAction = "AddEdit",
                        ControllerName = "JournalEntries",
                        UserName = User.Identity.Name,
                        UserId = userId,
                        LogDate = DateTime.Now,
                        RequestMethod = "POST",
                        SelectedItem = id,
                        CodeOrDocNo = journalEntry.DocumentNumber
                    });
                    Notification.GetNotification("JournalEntries", "Add", "InsertJournalEntry", id, null, "القيود");
                }
                else
                {
                    // تعديل
                    var entity = db.JournalEntries
                                   .Include(x => x.JournalEntryDetails)
                                   .FirstOrDefault(x => x.Id == id);

                    if (entity == null)
                        return Json(new { success = "false", message = "القيد غير موجود." });

                    // تحديث بيانات الهيدر
                    entity.DocumentNumber = journalEntry.DocumentNumber;
                    entity.Date = journalEntry.Date;
                    entity.DepartmentId = journalEntry.DepartmentId;
                    entity.IsPosted = journalEntry.IsPosted;
                    entity.Notes = journalEntry.Notes;

                    // استبدال التفاصيل
                    if (entity.JournalEntryDetails != null && entity.JournalEntryDetails.Any())
                        db.JournalEntryDetails.RemoveRange(entity.JournalEntryDetails.ToList());

                    foreach (var d in details)
                    {
                        d.JournalEntryId = entity.Id;
                        db.JournalEntryDetails.Add(d);
                    }

                    await db.SaveChangesAsync();

                    // Log + Notification
                    QueryHelper.AddLog(new MyLog
                    {
                        ArAction = "تعديل قيد",
                        EnAction = "AddEdit",
                        ControllerName = "JournalEntries",
                        UserName = User.Identity.Name,
                        UserId = userId,
                        LogDate = DateTime.Now,
                        RequestMethod = "POST",
                        SelectedItem = entity.Id,
                        CodeOrDocNo = entity.DocumentNumber
                    });
                    Notification.GetNotification("JournalEntries", "Edit", "InsertJournalEntry", entity.Id, null, "القيود");
                }

                return Json(new { Id = id, success = "true" });
            }
            catch (Exception ex)
            {
                var msg = ex.GetBaseException()?.Message ?? ex.Message;

                // لوج مختصر بدون خاصية Notes
                try
                {
                    QueryHelper.AddLog(new MyLog
                    {
                        ArAction = "فشل حفظ القيد: " + msg,
                        EnAction = "AddEditError",
                        ControllerName = "JournalEntries",
                        UserName = User.Identity.Name,
                        UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                        LogDate = DateTime.Now,
                        RequestMethod = "POST"
                        // SelectedItem و CodeOrDocNo اختياريين لو حابب تضيفهم
                    });
                }
                catch { /* تجاهل أي فشل في اللوج نفسه */ }

                return Json(new { success = "false", message = msg });
            }
        }

        [SkipERPAuthorize]
        public JsonResult GetAccounts()
        {
            return Json(db.ChartOfAccounts.Where(i => i.IsDeleted == false && i.IsActive == true && i.ClassificationId == 3).Select(i => new
            {
                Code = i.Code,
                ArName = i.ArName,
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
            try
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
            var department = db.Departments.FirstOrDefault(a => a.Id == id);
            var DepartmentCode = department != null ? department.Code : null;
            if (string.IsNullOrWhiteSpace(DepartmentCode))
            {
                DepartmentCode = (id ?? 0).ToString();
            }
            int parsedDepartmentCode;
            if (int.TryParse(DepartmentCode, out parsedDepartmentCode))
            {
                DepartmentCode = parsedDepartmentCode < 10 ? "0" + parsedDepartmentCode : parsedDepartmentCode.ToString();
            }
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
            }
            catch
            {
                return Json("DOC-" + DateTime.Now.ToString("yyMMddHHmmssfff"), JsonRequestBehavior.AllowGet);
            }
        }

        private static List<JournalEntryDetail> NormalizeJournalEntryDetails(JournalEntry journalEntry, Department dep)
        {
            var submittedDetails = (journalEntry.JournalEntryDetails ?? Enumerable.Empty<JournalEntryDetail>())
                .Where(d => d != null)
                .ToList();

            if (submittedDetails.Any(d => !d.AccountId.HasValue && (d.Debit != 0 || d.Credit != 0)))
                throw new InvalidOperationException("يوجد سطر قيد به مبلغ بدون حساب.");

            var details = submittedDetails
                .Where(d => d.AccountId.HasValue && (d.Debit != 0 || d.Credit != 0))
                .ToList();

            if (details.Count < 2)
                throw new InvalidOperationException("يجب إدخال طرفين على الأقل للقيد.");

            foreach (var d in details)
            {
                if (d.Debit < 0 || d.Credit < 0)
                    throw new InvalidOperationException("لا يمكن حفظ قيد بقيم مدينة أو دائنة سالبة.");

                if (d.Debit > 0 && d.Credit > 0)
                    throw new InvalidOperationException("لا يمكن أن يكون السطر مدين ودائن في نفس الوقت.");

                d.Id = 0;
                d.DepartmentId = d.DepartmentId.HasValue && d.DepartmentId.Value > 0 ? d.DepartmentId : journalEntry.DepartmentId;
                d.CurrencyId = journalEntry.CurrencyId;
                d.Equivalent = journalEntry.Equivalent;
                d.SourcePageId = journalEntry.SourcePageId;
                d.SourceId = journalEntry.SourceId;
                d.IsPosted = journalEntry.IsPosted;
                d.IsDeleted = false;
                d.IsActive = true;

                if (d.CostCenterId.HasValue && d.CostCenterId.Value == 0)
                    d.CostCenterId = null;

                if (d.PartyId.HasValue)
                {
                    if (!d.PartyType.HasValue || d.PartyType.Value == 0)
                        d.PartyType = DeterminePartyType(dep, d.AccountId.Value);
                }
                else
                {
                    d.PartyType = null;
                }

                d.ChartOfAccount = null;
                d.CostCenter = null;
                d.JournalEntry = null;
                d.SystemPage = null;
                d.Vendor = null;
            }

            var totalDebit = Math.Round(details.Sum(d => d.Debit), 2);
            var totalCredit = Math.Round(details.Sum(d => d.Credit), 2);

            if (totalDebit != totalCredit)
                throw new InvalidOperationException("مجموع المدين يجب أن يساوي مجموع الدائن.");

            return details;
        }

        private byte? DeterminePartyType(int departmentId, int accountId)
        {
            var dep = db.Departments.FirstOrDefault(d => d.Id == departmentId);
            if (dep == null) return null;

            if (dep.VendorsAccountId == accountId) return 1;           // Vendor
            if (dep.CustomersAccountId == accountId) return 2;         // Customer
            if (dep.EmployeeReceivableAccountId == accountId) return 3;// Employee
            if (dep.RenterAndBuyerAccountId == accountId) return 4;    // Renter
            if (dep.OwnerAccountId == accountId) return 5;             // Owner

            return null;
        }

        // (اختياري) Overload أسرع لو هتجيب القسم مرة وتعدّي الكائن نفسه
        private static byte? DeterminePartyType(Department dep, int accountId)
        {
            if (dep == null) return null;
            if (dep.VendorsAccountId == accountId) return 1;
            if (dep.CustomersAccountId == accountId) return 2;
            if (dep.EmployeeReceivableAccountId == accountId) return 3;
            if (dep.RenterAndBuyerAccountId == accountId) return 4;
            if (dep.OwnerAccountId == accountId) return 5;
            return null;
        }
        [SkipERPAuthorize]
        public JsonResult CheckValueAddedTaxes(int? AccountId, int? DepartmentId)
        {
            var IncludeValueAddedTax = db.ChartOfAccounts.Where(a => a.IsActive == true && a.IsDeleted == false && a.Id == AccountId).FirstOrDefault()?.IncludeValueAddedTax;
            var TaxAccount = db.Departments.Where(a => a.Id == DepartmentId).Select(a=>new {
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
