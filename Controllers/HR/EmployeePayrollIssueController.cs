using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Web;
using System.Web.Mvc;
using MyERP.Models;
using System.Security.Claims;
using System.Data.Entity.Core.Objects;

namespace MyERP.Controllers.HR
{
    public class EmployeePayrollIssueController : Controller
    {
        private readonly MySoftERPEntity db = new MySoftERPEntity();

        // GET: EmployeePayrollIssue
        public async Task<ActionResult> Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "", int departmentId = 0)
        {
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            if (userId == 1)
            {
                ViewBag.DepartmentId = new SelectList(db.Departments.Where(d => d.IsActive == true && d.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName", departmentId);
            }
            else
            {
                ViewBag.DepartmentId = new SelectList(db.UserDepartments.Where(d => d.UserId == userId).Select(b => new
                {
                    Id = b.DepartmentId,
                    ArName = b.Department.Code + " - " + b.Department.ArName
                }), "Id", "ArName", departmentId);
            }

            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة اصدار الراتب",
                EnAction = "Index",
                ControllerName = "EmployeePayrollIssue",
                UserName = User.Identity.Name,
                UserId = userId,
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            ////-------------------- Notification-------------------------////
            Notification.GetNotification("EmployeePayrollIssue", "View", "Index", null, null, "اصدار الراتب");

            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;
            IQueryable<EmployeePayrollIssue> employeePayrollIssues;
            if (string.IsNullOrEmpty(searchWord))
            {
                if (userId == 1)
                {
                    employeePayrollIssues = db.EmployeePayrollIssues.Where(c => c.IsDeleted == false && c.IsActive == true && (departmentId == 0 || c.DepartmentId == departmentId)).OrderByDescending(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                    ViewBag.Count = db.EmployeePayrollIssues.Where(c => c.IsDeleted == false && c.IsActive == true && (departmentId == 0 || c.DepartmentId == departmentId)).Count();
                }
                else
                {
                    employeePayrollIssues = db.EmployeePayrollIssues.Where(c => c.IsDeleted == false && c.IsActive == true && (db.UserDepartments.Where(a => a.UserId == userId).Select(a => a.DepartmentId).Contains(c.DepartmentId)) && (departmentId == 0 || c.DepartmentId == departmentId)).OrderByDescending(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                    ViewBag.Count = db.EmployeePayrollIssues.Where(c => c.IsDeleted == false && c.IsActive == true && (db.UserDepartments.Where(a => a.UserId == userId).Select(a => a.DepartmentId).Contains(c.DepartmentId)) && (departmentId == 0 || c.DepartmentId == departmentId)).Count();
                }
            }
            else
            {
                if (userId == 1)
                {
                    employeePayrollIssues = db.EmployeePayrollIssues.Where(s => s.IsDeleted == false && s.IsActive == true && (departmentId == 0 || s.DepartmentId == departmentId) && (s.DocumentNumber.Contains(searchWord))).OrderByDescending(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                    ViewBag.Count = db.EmployeePayrollIssues.Where(s => s.IsDeleted == false && s.IsActive == true && (departmentId == 0 || s.DepartmentId == departmentId) && (s.DocumentNumber.Contains(searchWord))).Count();
                }
                else
                {
                    employeePayrollIssues = db.EmployeePayrollIssues.Where(s => s.IsDeleted == false && s.IsActive == true && (db.UserDepartments.Where(a => a.UserId == userId).Select(a => a.DepartmentId).Contains(s.DepartmentId)) && (departmentId == 0 || s.DepartmentId == departmentId) && (s.DocumentNumber.Contains(searchWord))).OrderByDescending(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                    ViewBag.Count = db.EmployeePayrollIssues.Where(s => s.IsDeleted == false && s.IsActive == true && (db.UserDepartments.Where(a => a.UserId == userId).Select(a => a.DepartmentId).Contains(s.DepartmentId)) && (departmentId == 0 || s.DepartmentId == departmentId) && (s.DocumentNumber.Contains(searchWord))).Count();

                }
            }
            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            return View(await employeePayrollIssues.ToListAsync());
        }

        // GET: EmployeePayrollIssue/Edit/5
        public async Task<ActionResult> AddEdit(int? id)
        {
            if (id == null)
            {
                ViewBag.EmployeeId = new SelectList(db.Employees.Where(x => x.IsActive && !x.IsDeleted).Select(x => new { x.Id, x.ArName }), "Id", "ArName");
                ViewBag.DepartmentId = new SelectList(db.Departments.Where(x => x.IsActive && !x.IsDeleted).Select(x => new { x.Id, x.ArName }), "Id", "ArName");
                ViewBag.HrDepartmentId = new SelectList(db.HrDepartments.Where(x => x.IsActive && !x.IsDeleted).Select(x => new { x.Id, x.ArName }), "Id", "ArName");
                ViewBag.AdministrativeDepartmentId = new SelectList(db.AdministrativeDepartments.Where(x => x.IsActive && !x.IsDeleted).Select(x => new { x.Id, x.ArName }), "Id", "ArName");
                ViewBag.Year = new SelectList(new int[] { 2019, 2020, 2021, 2022, 2023, 2024, 2025, 2026, 2027 });
                ViewBag.Month = new SelectList(new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 });
                // var DocumentNumber = await db.EmployeePayrollIssues.OrderByDescending(x => x.Id).Select(x => x.DocumentNumber).FirstOrDefaultAsync();
                //ViewBag.DocumentNumber = string.IsNullOrEmpty(DocumentNumber) ? 1 : int.Parse(DocumentNumber) + 1;
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
                ViewBag.VoucherDate = cTime.ToString("yyyy-MM-ddTHH:mm");
                return View();
            }
            EmployeePayrollIssue employeePayrollIssue = await db.EmployeePayrollIssues.FindAsync(id);
            if (employeePayrollIssue == null)
            {
                return HttpNotFound();
            }
            ViewBag.EmployeeId = new SelectList(db.Employees.Where(x => x.IsActive && !x.IsDeleted).Select(x => new { x.Id, x.ArName }), "Id", "ArName", employeePayrollIssue.EmployeeId);
            ViewBag.DepartmentId = new SelectList(db.Departments.Where(x => x.IsActive && !x.IsDeleted).Select(x => new { x.Id, x.ArName }), "Id", "ArName", employeePayrollIssue.DepartmentId);
            ViewBag.HrDepartmentId = new SelectList(db.HrDepartments.Where(x => x.IsActive && !x.IsDeleted).Select(x => new { x.Id, x.ArName }), "Id", "ArName",employeePayrollIssue.HrDepartmentId);
            ViewBag.AdministrativeDepartmentId = new SelectList(db.AdministrativeDepartments.Where(x => x.IsActive && !x.IsDeleted).Select(x => new { x.Id, x.ArName }), "Id", "ArName", employeePayrollIssue.AdministrativeDepartmentId);
            ViewBag.Year = new SelectList(new int[] { 2019, 2020, 2021, 2022, 2023, 2024, 2025, 2026, 2027 }, employeePayrollIssue.Year);
            ViewBag.Month = new SelectList(new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 }, employeePayrollIssue.Month);
            //ViewBag.DocumentNumber = employeePayrollIssue.DocumentNumber;
            //ViewBag.Due = employeePayrollIssue.TotalDueItems + employeePayrollIssue.TotalOvertime + employeePayrollIssue.TotalReward;
            //ViewBag.Deduction = employeePayrollIssue.TotalPenalty + employeePayrollIssue.TotalDeductionItems;
            ViewBag.VoucherDate = employeePayrollIssue.VoucherDate.ToString("yyyy-MM-ddTHH:mm");
            int sysPageId = QueryHelper.SourcePageId("EmployeePayrollIssue");
            ViewBag.JE = db.GetJournalEntryBySourceIdAndSourcePageId(id, sysPageId).FirstOrDefault();
            if (ViewBag.JE != null)
            {
                var JEDocNo = ViewBag.JE;
                ViewBag.JE.DocumentNumber = JEDocNo != null ? JEDocNo.DocumentNumber.Replace("-", "") : "";
                int JEId = ViewBag.JE.Id;
                int sourcePageId = ViewBag.JE.SourcePageId;
                ViewBag.JEDetails = db.JournalEntryDetails.Where(a => a.SourceId == id && a.JournalEntryId == JEId && a.SourcePageId == sourcePageId).OrderBy(a => a.Id).ToList();

            }
            ViewBag.Accounts = new SelectList(db.ChartOfAccounts.Where(a => a.ClassificationId == 3 && a.IsActive == true && a.IsDeleted == false), "Id", "ArName");
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح تفاصيل اصدار الراتب",
                EnAction = "AddEdit",
                ControllerName = "EmployeePayrollIssue",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = employeePayrollIssue.Id,
                CodeOrDocNo = employeePayrollIssue.DocumentNumber
            });
            return View(employeePayrollIssue);
        }

        
        [HttpPost]
        public async Task<JsonResult> AddEdit(EmployeePayrollIssue employeePayrollIssue)
        {
            if (!ModelState.IsValid)
            {
                var errs = ModelState
                    .Where(x => x.Value.Errors != null && x.Value.Errors.Count > 0)
                    .Select(x => new {
                        Key = x.Key,
                        Errors = x.Value.Errors.Select(e => e.ErrorMessage).ToList()
                    }).ToList();

                return Json(new { success = false, errors = errs });
            }



            try
            {
                var id = employeePayrollIssue.Id;
                int userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                if (employeePayrollIssue.Id > 0)
                {
                    MyXML.xPathName = "Details";
                    var detailsXml = MyXML.GetXML(employeePayrollIssue.EmployeePayrollIssueDetails);

                    db.EmployeePayrollIssue_Update(employeePayrollIssue.Id,
                        employeePayrollIssue.Month, employeePayrollIssue.Year, employeePayrollIssue.DepartmentId,
                        employeePayrollIssue.VoucherDate, userId, employeePayrollIssue.EmployeeId,
                        employeePayrollIssue.HrDepartmentId, employeePayrollIssue.AdministrativeDepartmentId,
                        detailsXml);
                }
                else
                {
                    var idResult = new ObjectParameter("Id", typeof(Int32));
                    MyXML.xPathName = "Details";
                    var detailsXml = MyXML.GetXML(employeePayrollIssue.EmployeePayrollIssueDetails);

                    db.EmployeePayrollIssue_Insert(idResult,
                        employeePayrollIssue.Month, employeePayrollIssue.Year, employeePayrollIssue.DepartmentId,
                        employeePayrollIssue.VoucherDate, userId, employeePayrollIssue.EmployeeId,
                        employeePayrollIssue.HrDepartmentId, employeePayrollIssue.AdministrativeDepartmentId,
                        detailsXml);

                    // لو محتاج ترجع Id الجديد:
                    // employeePayrollIssue.Id = (int)idResult.Value;
                }

                await db.SaveChangesAsync(); // هنا غالبًا لو فيه FK/Null/Constraint هيقع

                return Json(new { success = true });
            }
            catch (System.Data.Entity.Validation.DbEntityValidationException ex)
            {
                var msgs = ex.EntityValidationErrors
                    .SelectMany(e => e.ValidationErrors)
                    .Select(v => $"{v.PropertyName}: {v.ErrorMessage}")
                    .ToList();
                return Json(new { success = false, error = "DbEntityValidationException", details = msgs });
            }
            catch (Exception ex)
            {
                string msg = ex.Message;
                if (ex.InnerException != null) msg += " | INNER: " + ex.InnerException.Message;
                if (ex.InnerException?.InnerException != null) msg += " | INNER2: " + ex.InnerException.InnerException.Message;

                return Json(new { success = false, error = msg });
            }
        }


        // POST: EmployeePayrollIssue/Delete/5
        [HttpPost, ActionName("Delete")]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            EmployeePayrollIssue employeePayrollIssue = db.EmployeePayrollIssues.Find(id);
            if (employeePayrollIssue.IsPosted == true)
            {
                return Content("false");
            }
            employeePayrollIssue.IsDeleted = true;
            employeePayrollIssue.UserId = userId;
            foreach (var detail in employeePayrollIssue.EmployeePayrollIssueDetails)
            {
                detail.IsDeleted = true;
            }
            Random random = new Random();
            const string chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            var Code = new string(Enumerable.Repeat(chars, 6).Select(s => s[random.Next(s.Length)]).ToArray());
            employeePayrollIssue.DocumentNumber = Code;
            var JournalEntryDoc = new string(Enumerable.Repeat(chars, 6).Select(s => s[random.Next(s.Length)]).ToArray());
            db.Entry(employeePayrollIssue).State = EntityState.Modified;
            db.Database.ExecuteSqlCommand($"update JournalEntry set IsDeleted = 1, UserId={userId} ,DocumentNumber=N'{JournalEntryDoc}' where SourcePageId = (select Id from SystemPage where TableName = 'EmployeePayrollIssue') and SourceId = {id}; update JournalEntryDetail set IsDeleted = 1 where JournalEntryId = (select Id from JournalEntry where SourcePageId = (select Id from SystemPage where TableName = 'EmployeePayrollIssue') and SourceId = {id})");

            await db.SaveChangesAsync();
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "حذف اصدار الراتب",
                EnAction = "AddEdit",
                ControllerName = "EmployeePayrollIssue",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "POST",
                SelectedItem = id,
                CodeOrDocNo = employeePayrollIssue.DocumentNumber
            });
            Notification.GetNotification("EmployeePayrollIssue", "Delete", "Delete", id, null, "اصدار الراتب");
            return Content("true");
        }

        //[SkipERPAuthorize]
        //public async Task<JsonResult > PayrollDetails(int employeeId, int month, int year)
        //{
        //    var due= await db.EmployeeSalaryItems.Where(x=>x.EmployeeId==employeeId&&x.SalaryItem.Type==0).Select(x => x.Amount).ToListAsync();
        //    var deduction= await db.EmployeeSalaryItems.Where(x => x.EmployeeId == employeeId && x.SalaryItem.Type == 1).Select(x => (decimal?)x.Amount).SumAsync();
        //    var reward= await db.RewardIssueDetials.Where(x => x.RewardIssue.EmployeeId == employeeId && x.RewardIssue.Month == month && x.RewardIssue.Year == year && !x.RewardIssue.IsDeleted).Select(x =>(decimal?) x.Total).SumAsync();
        //    var overtime= await db.OvertimeIssueDetials.Where(x => x.OvertimeIssue.EmployeeId == employeeId && x.OvertimeIssue.Month == month && x.OvertimeIssue.Year == year && !x.OvertimeIssue.IsDeleted).Select(x =>(decimal?) x.Total).SumAsync();
        //    var penalty= await db.PenaltyIssueDetails.Where(x => x.PenaltyIssue.EmployeeId == employeeId && x.PenaltyIssue.Month == month && x.PenaltyIssue.Year == year && !x.PenaltyIssue.IsDeleted).Select(x =>(decimal?) x.Total).SumAsync();
        //    return Json(new {due= due.Sum(), deduction, reward, overtime, penalty }, JsonRequestBehavior.AllowGet);
        //}

        //[SkipERPAuthorize]
        //public async Task<JsonResult> PreviousPayrollMonths(int employeeId, int year)
        //{
        //    return Json(await db.EmployeePayrollIssues.Where(x => x.EmployeeId == employeeId && !x.IsDeleted && x.Year == year).Select(x => x.Month).ToArrayAsync(), JsonRequestBehavior.AllowGet);
        //}

        [SkipERPAuthorize]
        public JsonResult SetDocNum(int id, DateTime? VoucherDate)
        {
            bool IsExistInDocumentsCoding = false;
            var noOfDigits = (int?)0;
            var YearFormat = (int?)0;
            var CodingTypeId = (int?)0;
            var IsZerosFills = (bool?)null;
            var newDocNo = "";
            var GeneratedDocNo = "";
            var lastObj = db.EmployeePayrollIssues.Where(a => a.IsActive == true && a.IsDeleted == false && a.DepartmentId == id).OrderByDescending(a => a.Id).FirstOrDefault();
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
                    lastObj = db.EmployeePayrollIssues.Where(a => a.IsActive == true && a.IsDeleted == false && a.DepartmentId == id && a.VoucherDate.Month == VoucherDate.Value.Month && a.VoucherDate.Year == VoucherDate.Value.Year).OrderByDescending(a => a.Id).FirstOrDefault();
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
                    lastObj = db.EmployeePayrollIssues.Where(a => a.IsActive == true && a.IsDeleted == false && a.DepartmentId == id && a.VoucherDate.Year == VoucherDate.Value.Year).OrderByDescending(a => a.Id).FirstOrDefault();

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

            //var docNo = QueryHelper.DocLastNum(id, "EmployeePayrollIssue");
            //double i = (docNo) + 1;
            //return Json(i, JsonRequestBehavior.AllowGet);
        }

        [SkipERPAuthorize]
        public JsonResult GetEmployeeContractDetailsByDepartmentId(int? DepartmentId, int? year, int? month,int?EmployeeId, int?HrDepartmentId, int?AdministrativeDepartmentId)
        {
            db.Configuration.ProxyCreationEnabled = false;
            var Employees = db.GetEmployeeContractDetailsByDepartmentId(DepartmentId, year, month,EmployeeId,HrDepartmentId,AdministrativeDepartmentId).ToList();
            return Json(Employees, JsonRequestBehavior.AllowGet);
        }

        [SkipERPAuthorize]
        public JsonResult GetAppearInPayRollSalaryItem(int?HrDepartmentId)
        {
            db.Configuration.ProxyCreationEnabled = false;
            List<SalaryItem> AppearInPayRoll;
            if(HrDepartmentId==null)
            {
                AppearInPayRoll = db.SalaryItems.Where(a => a.IsActive == true && a.IsDeleted == false && a.AppearInPayroll == true).ToList();
            }
            else
            {
                AppearInPayRoll = db.SalaryItems.Where(a => a.IsActive == true && a.IsDeleted == false && a.AppearInPayroll == true && (a.HrDepartmentId == null || a.HrDepartmentId == HrDepartmentId)).ToList();
            }
          //  var AppearInPayRoll = db.SalaryItems.Where(a=>a.IsActive==true&& a.IsDeleted==false&&a.AppearInPayroll==true&&(HrDepartmentId==null&&a.HrDepartmentId==HrDepartmentId)).ToList();
            return Json(AppearInPayRoll, JsonRequestBehavior.AllowGet);
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
