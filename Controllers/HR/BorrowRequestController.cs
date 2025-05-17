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
using MyERP.Repository;
using System.Threading.Tasks;
using System.Data.Entity.Core.Objects;

namespace MyERP.Controllers.HR
{
    public class BorrowRequestController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        // GET: BorrowRequest
        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "", int departmentId = 0)
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
                ArAction = "فتح قائمة طلب السُلفة",
                EnAction = "Index",
                ControllerName = "BorrowRequest",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });

            ////-------------------- Notification-------------------------////
            Notification.GetNotification("BorrowRequest", "View", "Index", null, null, "طلب السُلفة");
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;

            IQueryable<BorrowRequest> borrowRequests;
            if (string.IsNullOrEmpty(searchWord))
            {
                if (userId == 1)
                {
                    borrowRequests = db.BorrowRequests.Where(s => s.IsDeleted == false && (departmentId == 0 || s.DepartmentId == departmentId)).OrderByDescending(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                    ViewBag.Count = db.BorrowRequests.Where(s => s.IsDeleted == false && (departmentId == 0 || s.DepartmentId == departmentId)).Count();
                }
                else
                {
                    borrowRequests = db.BorrowRequests.Where(s => s.IsDeleted == false && (db.UserDepartments.Where(a => a.UserId == userId).Select(a => a.DepartmentId).Contains(s.DepartmentId)) && (departmentId == 0 || s.DepartmentId == departmentId)).OrderByDescending(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                    ViewBag.Count = db.BorrowRequests.Where(s => s.IsDeleted == false&& (db.UserDepartments.Where(a => a.UserId == userId).Select(a => a.DepartmentId).Contains(s.DepartmentId)) && (departmentId == 0 || s.DepartmentId == departmentId)).Count();
                }
            }
            else
            {
                if (userId == 1)
                {
                    borrowRequests = db.BorrowRequests.Where(s => s.IsDeleted == false && (departmentId == 0 || s.DepartmentId == departmentId) && (s.DocumentNumber.Contains(searchWord) || s.Notes.Contains(searchWord) || s.Department.ArName.Contains(searchWord))).OrderByDescending(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                    ViewBag.Count = db.BorrowRequests.Where(s => s.IsDeleted == false && (departmentId == 0 || s.DepartmentId == departmentId) && (s.DocumentNumber.Contains(searchWord) || s.Notes.Contains(searchWord) || s.Department.ArName.Contains(searchWord))).Count();
                }
                else
                {
                    borrowRequests = db.BorrowRequests.Where(s => s.IsDeleted == false&& (db.UserDepartments.Where(a => a.UserId == userId).Select(a => a.DepartmentId).Contains(s.DepartmentId)) && (departmentId == 0 || s.DepartmentId == departmentId) && (s.DocumentNumber.Contains(searchWord) || s.Notes.Contains(searchWord) || s.Department.ArName.Contains(searchWord))).OrderByDescending(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                    ViewBag.Count = db.BorrowRequests.Where(s => s.IsDeleted == false&& (db.UserDepartments.Where(a => a.UserId == userId).Select(a => a.DepartmentId).Contains(s.DepartmentId)) && (departmentId == 0 || s.DepartmentId == departmentId) && (s.DocumentNumber.Contains(searchWord) || s.Notes.Contains(searchWord) || s.Department.ArName.Contains(searchWord))).Count();
                }
            }

            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            ViewBag.PageIndex = pageIndex;
            return View(borrowRequests.ToList());
        }

        // GET: BorrowRequest/Edit/5
        public ActionResult AddEdit(int? id)
        {
            int userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            DepartmentRepository departmentRepository = new DepartmentRepository(db);
            SystemSetting systemSetting = db.SystemSettings.Any() ? db.SystemSettings.FirstOrDefault() : new SystemSetting();
            if (id == null)
            {
                ViewBag.EmployeeId = new SelectList(db.Employees.Where(e => e.IsDeleted == false && e.IsActive == true).Select(b => new
                {
                    b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                ViewBag.BorrowStatusId = new SelectList(new List<dynamic>
                {
                new { Id = 1, ArName = "في إنتظار الموافقة" },
                new { Id = 2, ArName = "تمت الموافقة" },
                new { Id = 3, ArName ="تم الرفض" }
                }, "Id", "ArName");
                ViewBag.DepartmentId = new SelectList(departmentRepository.UserDepartments(userId).ToList(), "Id", "ArName", systemSetting.DefaultDepartmentId);
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
                //ViewBag.BorrowRequestDate = cTime.ToString("yyyy-MM-dd");
                //ViewBag.InstallmentFirstDate = cTime.ToString("yyyy-MM-dd");
                return View();
            }
            BorrowRequest borrowRequest = db.BorrowRequests.Find(id);
            if (borrowRequest == null)
            {
                return HttpNotFound();
            }
            //int sysPageId = QueryHelper.SourcePageId("BorrowRequest");
            //ViewBag.JE = db.GetJournalEntryBySourceIdAndSourcePageId(id, sysPageId).FirstOrDefault();
            //if (ViewBag.JE != null)
            //{
            //    int JEId = ViewBag.JE.Id;
            //    int sourcePageId = ViewBag.JE.SourcePageId;
            //    ViewBag.JEDetails = db.JournalEntryDetails.Where(a => a.SourceId == id && a.JournalEntryId == JEId && a.SourcePageId == sourcePageId).OrderBy(a => a.Id).ToList();
            //}

            ViewBag.DepartmentId = new SelectList(departmentRepository.UserDepartments(userId).ToList(), "Id", "ArName", borrowRequest.DepartmentId);
            ViewBag.EmployeeId = new SelectList(db.Employees.Where(e => e.IsDeleted == false && e.IsActive == true).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", borrowRequest.EmployeeId);
            ViewBag.BorrowStatusId = new SelectList(new List<dynamic>
                {
                new { Id = 1, ArName = "في إنتظار الموافقة" },
                new { Id = 2, ArName = "تمت الموافقة" },
                new { Id = 3, ArName ="تم الرفض" }
                }, "Id", "ArName", borrowRequest.BorrowStatusId);

            ViewBag.Next = QueryHelper.Next((int)id, "BorrowRequest");
            ViewBag.Previous = QueryHelper.Previous((int)id, "BorrowRequest");
            ViewBag.Last = QueryHelper.GetLast("BorrowRequest");
            ViewBag.First = QueryHelper.GetFirst("BorrowRequest");
            try
            {
                ViewBag.VoucherDate = borrowRequest.VoucherDate.ToString("yyyy-MM-ddTHH:mm");
                ViewBag.BorrowRequestDate = borrowRequest.BorrowRequestDate.Value.ToString("yyyy-MM-dd");
                ViewBag.InstallmentFirstDate = borrowRequest.InstallmentFirstDate.Value.ToString("yyyy-MM-dd");

            }
            catch (Exception e)
            {
                var errors = e;
            }

            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح تفاصيل طلب السُلفة",
                EnAction = "AddEdit",
                ControllerName = "BorrowRequest",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = borrowRequest.Id,
                CodeOrDocNo = borrowRequest.DocumentNumber
            });
            return View(borrowRequest);
        }

        // POST: BorrowRequest/Edit/5
        [HttpPost]
        public ActionResult AddEdit(BorrowRequest borrowRequest)
        {
            int userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

            if (ModelState.IsValid)
            {
                var id = borrowRequest.Id;
                borrowRequest.IsDeleted = false;

                if (borrowRequest.Id > 0)
                {
                    borrowRequest.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    MyXML.xPathName = "Details";
                    var BorrowRequestDetails = MyXML.GetXML(borrowRequest.BorrowRequestDetails);
                    db.BorrowRequest_Update(borrowRequest.Id, borrowRequest.DocumentNumber, borrowRequest.VoucherDate, borrowRequest.DepartmentId, borrowRequest.EmployeeId, borrowRequest.BorrowRequestDate, borrowRequest.BorrowValue, borrowRequest.InstallmentFirstDate, borrowRequest.InstallmentValue, borrowRequest.UserId, borrowRequest.IsDeleted, borrowRequest.Notes, borrowRequest.Image, BorrowRequestDetails, borrowRequest.SystemPageId, borrowRequest.SelectedId, borrowRequest.IsDelivered, borrowRequest.IsAccepted, borrowRequest.IsLinked, borrowRequest.IsCompleted, borrowRequest.IsPosted, borrowRequest.AutoCreated, borrowRequest.InstallmentNo, borrowRequest.BorrowStatusId, borrowRequest.RefuseReason, borrowRequest.IsIssued);

                    //////-------------------- Notification-------------------------////
                    Notification.GetNotification("BorrowRequest", "Edit", "AddEdit", id, null, "طلب السُلفة");
                }
                else
                {
                    borrowRequest.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    var idResult = new ObjectParameter("Id", typeof(Int32));
                    MyXML.xPathName = "Details";
                    var BorrowRequestDetails = MyXML.GetXML(borrowRequest.BorrowRequestDetails);
                    db.BorrowRequest_Insert(idResult, borrowRequest.VoucherDate, borrowRequest.DepartmentId, borrowRequest.EmployeeId, borrowRequest.BorrowRequestDate, borrowRequest.BorrowValue, borrowRequest.InstallmentFirstDate, borrowRequest.InstallmentValue, borrowRequest.UserId, borrowRequest.IsDeleted, borrowRequest.Notes, borrowRequest.Image, BorrowRequestDetails, borrowRequest.SystemPageId, borrowRequest.SelectedId, borrowRequest.IsDelivered, borrowRequest.IsAccepted, borrowRequest.IsLinked, borrowRequest.IsCompleted, borrowRequest.IsPosted, borrowRequest.AutoCreated, borrowRequest.InstallmentNo, borrowRequest.BorrowStatusId, borrowRequest.RefuseReason, borrowRequest.IsIssued);
                    id = (int)idResult.Value;

                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("BorrowRequest", "Add", "AddEdit", borrowRequest.Id, null, "طلب السُلفة");
                }
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = id > 0 ? "تعديل طلب السُلفة" : "اضافة طلب السُلفة",
                    EnAction = "AddEdit",
                    ControllerName = "BorrowRequest",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    CodeOrDocNo = borrowRequest.DocumentNumber
                });
                return Json(new { success = "true", id });
            }
            var errors = ModelState
                   .Where(x => x.Value.Errors.Count > 0)
                   .Select(x => new { x.Key, x.Value.Errors })
                   .ToArray();
            return Json(new { success = false, errors });
        }

        // POST: BorrowRequest/Delete/5
        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                BorrowRequest borrowRequest = db.BorrowRequests.Find(id);
                borrowRequest.IsDeleted = true;
                borrowRequest.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                foreach (var item in borrowRequest.BorrowRequestDetails)
                {
                    item.IsDeleted = true;
                }
                Random random = new Random();
                const string chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
                var Code = new string(Enumerable.Repeat(chars, 6).Select(s => s[random.Next(s.Length)]).ToArray());
                borrowRequest.DocumentNumber = Code;
                db.Entry(borrowRequest).State = EntityState.Modified;
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "حذف طلب السُلفة",
                    EnAction = "AddEdit",
                    ControllerName = "BorrowRequest",
                    UserName = User.Identity.Name,
                    UserId = userId,
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = borrowRequest.Id,
                    CodeOrDocNo = borrowRequest.DocumentNumber
                });
                ////-------------------- Notification-------------------------////
                Notification.GetNotification("BorrowRequest", "Delete", "Delete", id, null, "طلب السُلفة");
                ///////////////-----------------------------------------------------------------------
                return Content("true");
            }
            catch (Exception)
            {
                throw;
            }
        }

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
            var lastObj = db.BorrowRequests.Where(a => a.IsDeleted == false && a.DepartmentId == id).OrderByDescending(a => a.Id).FirstOrDefault();
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
                    lastObj = db.BorrowRequests.Where(a => a.IsDeleted == false && a.DepartmentId == id && a.VoucherDate.Month == VoucherDate.Value.Month && a.VoucherDate.Year == VoucherDate.Value.Year).OrderByDescending(a => a.Id).FirstOrDefault();
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
                    lastObj = db.BorrowRequests.Where(a => a.IsDeleted == false && a.DepartmentId == id && a.VoucherDate.Year == VoucherDate.Value.Year).OrderByDescending(a => a.Id).FirstOrDefault();

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
            //var docNo = QueryHelper.DocLastNum(id, "BorrowRequest");
            //double i = (docNo) + 1;
            //return Json(i, JsonRequestBehavior.AllowGet);
        }
        [SkipERPAuthorize]
        public JsonResult GetEmployeeFinancialData(int? EmployeeId)
        {
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
            var BorrowBalance = db.BorrowRequests.Where(a => a.IsDeleted == false && a.EmployeeId == EmployeeId && a.IsIssued == true).Select(a => a.BorrowRequestDetails.Where(b => b.IsDeleted == false && b.IsInstallmentPaid != true).Sum(b => b.InstallmentValue)).Sum();
            var ContractEndTime = db.EmployeeContracts.Where(a => a.IsActive == true && a.IsDeleted == false && a.EmployeeId == EmployeeId).FirstOrDefault().ContractEndTime;
            var RemainContractPeriod = 12 * (ContractEndTime.Value.Year - cTime.Year) + ContractEndTime.Value.Month - cTime.Month;
            var RemainInstallmentNo = db.BorrowRequests.Where(a => a.IsDeleted == false && a.EmployeeId == EmployeeId && a.IsIssued == true).Select(a => a.BorrowRequestDetails.Where(b => b.IsDeleted == false && b.IsInstallmentPaid != true).Count()).Count();
            var TotalVacationAllocation = db.EmployeeMonthlyAllocations.Where(a => a.IsDeleted == false && a.AllocationTypeId == 1).Select(a => a.EmployeeMonthlyAllocationDetails.Where(b => b.EmployeeId == EmployeeId).Sum(b => b.Value)).Sum();
            var SalaryDueBalance = db.EmployeePayrollIssueDetails.Where(a => a.IsActive == true && a.IsDeleted == false && a.EmployeeId == EmployeeId && a.IsIssued != true).FirstOrDefault().NetSalary;
            return Json(new { BorrowBalance = BorrowBalance, RemainContractPeriod = RemainContractPeriod, RemainInstallmentNo = RemainInstallmentNo, TotalVacationAllocation = TotalVacationAllocation, SalaryDueBalance = SalaryDueBalance }, JsonRequestBehavior.AllowGet);
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