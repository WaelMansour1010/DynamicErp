using MyERP.Models;
using MyERP.Repository;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core.Objects;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;

namespace MyERP.Controllers.HR
{
    public class EmployeeVacationDueController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        // GET: EmployeeVacationDue
        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "", int departmentId = 0)
        {
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            DepartmentRepository departmentRepository = new DepartmentRepository(db);

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
                ArAction = "فتح قائمة مستحقات القيام بإجازة",
                EnAction = "Index",
                ControllerName = "EmployeeVacationDue",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            ////-------------------- Notification-------------------------////
            Notification.GetNotification("EmployeeVacationDue", "View", "Index", null, null, "مستحقات القيام بإجازة");

            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;

            IQueryable<EmployeeVacationDue> dues;

            if (string.IsNullOrEmpty(searchWord))
            {
                if (userId == 1)
                {
                    dues = db.EmployeeVacationDues.Where(s => s.IsDeleted == false && s.IsActive == true && (departmentId == 0 || s.DepartmentId == departmentId)).Include(s => s.Department).OrderByDescending(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                    ViewBag.Count = db.EmployeeVacationDues.Where(s => s.IsDeleted == false && s.IsActive == true && (departmentId == 0 || s.DepartmentId == departmentId)).Count();
                }
                else
                {
                    dues = db.EmployeeVacationDues.Where(s => s.IsDeleted == false && s.IsActive == true && (db.UserDepartments.Where(a => a.UserId == userId).Select(a => a.DepartmentId).Contains(s.DepartmentId)) && (departmentId == 0 || s.DepartmentId == departmentId)).Include(s => s.Department).OrderByDescending(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                    ViewBag.Count = db.EmployeeVacationDues.Where(s => s.IsDeleted == false && s.IsActive == true && (db.UserDepartments.Where(a => a.UserId == userId).Select(a => a.DepartmentId).Contains(s.DepartmentId)) && (departmentId == 0 || s.DepartmentId == departmentId)).Count();
                }
            }
            else
            {
                if (userId == 1)
                {
                    dues = db.EmployeeVacationDues.Where(s => s.IsDeleted == false && s.IsActive == true && (s.DocumentNumber.Contains(searchWord) || s.Department.ArName.Contains(searchWord)) && (departmentId == 0 || s.DepartmentId == departmentId)).Include(s => s.Department).OrderByDescending(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                    ViewBag.Count = db.EmployeeVacationDues.Where(s => s.IsDeleted == false && s.IsActive == true && (s.DocumentNumber.Contains(searchWord) || s.Department.ArName.Contains(searchWord)) && (departmentId == 0 || s.DepartmentId == departmentId)).Count();
                }
                else
                {
                    dues = db.EmployeeVacationDues.Where(s => s.IsDeleted == false && s.IsActive == true && (db.UserDepartments.Where(a => a.UserId == userId).Select(a => a.DepartmentId).Contains(s.DepartmentId)) && (s.DocumentNumber.Contains(searchWord) || s.Department.ArName.Contains(searchWord)) && (departmentId == 0 || s.DepartmentId == departmentId)).Include(s => s.Department).OrderByDescending(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                    ViewBag.Count = db.EmployeeVacationDues.Where(s => s.IsDeleted == false && s.IsActive == true && (db.UserDepartments.Where(a => a.UserId == userId).Select(a => a.DepartmentId).Contains(s.DepartmentId)) && (s.DocumentNumber.Contains(searchWord) || s.Department.ArName.Contains(searchWord)) && (departmentId == 0 || s.DepartmentId == departmentId)).Count();
                }
            }

            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة مستحقات القيام بإجازة",
                EnAction = "Index",
                ControllerName = "EmployeeVacationDue",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            return View(dues.ToList());
        }

        public ActionResult AddEdit(int? id)
        {
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            //************************************ ADD******************************//
            // Departments
            DepartmentRepository departmentRepository = new DepartmentRepository(db);
            if (id == null)
            {
                if (userId == 1)
                {
                    //DepartmentId
                    ViewBag.DepartmentId = new SelectList(db.Departments.Where(d => d.IsActive == true && d.IsDeleted == false).Select(b => new
                    {
                        Id = b.Id,
                        ArName = b.Code + " - " + b.ArName
                    }), "Id", "ArName");
                }
                else
                {
                    //DepartmentId
                    ViewBag.DepartmentId = new SelectList(db.UserDepartments.Where(d => d.UserId == userId).Select(b => new
                    {
                        Id = b.DepartmentId,
                        ArName = b.Department.Code + " - " + b.Department.ArName
                    }), "Id", "ArName");
                }
                //EmployeeId
                ViewBag.EmployeeId = new SelectList(db.Employees.Where(d => d.IsActive == true && d.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                //VacationRequestId
                ViewBag.VacationRequestId = new SelectList(db.VacationRequests.Where(d => d.IsActive == true && d.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,

                    // ArName = b.Code + " - " + b.ArName
                }), "Id", "Id");

                return View();
            }
            EmployeeVacationDue due = db.EmployeeVacationDues.Find(id);
            if (due == null)
            {
                return HttpNotFound();
            }
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "اضافة او تعديل مستحقات القيام بإجازة ",
                EnAction = "AddEdit",
                ControllerName = "EmployeeVacationDue",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                CodeOrDocNo = due.DocumentNumber,
                SelectedItem = id
            });

            ViewBag.Next = QueryHelper.Next((int)id, "EmployeeVacationDue");
            ViewBag.Previous = QueryHelper.Previous((int)id, "EmployeeVacationDue");
            ViewBag.Last = QueryHelper.GetLast("EmployeeVacationDue");
            ViewBag.First = QueryHelper.GetFirst("EmployeeVacationDue");
            // Departments
            if (userId == 1)
            {
                //DepartmentId
                ViewBag.DepartmentId = new SelectList(db.Departments.Where(d => d.IsActive == true && d.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName", due.DepartmentId);
            }
            else
            {
                //DepartmentId
                ViewBag.DepartmentId = new SelectList(db.UserDepartments.Where(d => d.UserId == userId).Select(b => new
                {
                    Id = b.DepartmentId,
                    ArName = b.Department.Code + " - " + b.Department.ArName
                }), "Id", "ArName", due.DepartmentId);
            }
            //EmployeeId
            ViewBag.EmployeeId = new SelectList(db.Employees.Where(d => d.IsActive == true && d.IsDeleted == false).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", due.EmployeeId);

            //VacationRequestId
            ViewBag.VacationRequestId = new SelectList(db.VacationRequests.Where(d => d.IsActive == true && d.IsDeleted == false && d.EmployeeId == due.EmployeeId).Select(b => new
            {
                Id = b.Id,

                // ArName = b.Code + " - " + b.ArName
            }), "Id", "Id", due.VacationRequestId);

            //Date
            ViewBag.Date = due.Date.Value.ToString("yyyy-MM-ddTHH:mm");
            //AdjustmentDate
            ViewBag.AdjustmentDate = due.AdjustmentDate.Value.ToString("yyyy-MM-ddTHH:mm");

            return View(due);

        }

        [HttpPost]
        public ActionResult AddEdit(EmployeeVacationDue due)
        {
            if (ModelState.IsValid)
            {
                var id = due.Id;
                due.IsDeleted = false;
                due.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                if (due.Id > 0)
                {
                    // update 
                    var old = db.EmployeeVacationDues.Find(id);
                    db.EmployeeVacationDueDetails.RemoveRange(db.EmployeeVacationDueDetails.Where(p => p.MainDocId == old.Id).ToList());
                    old.AdjustmentDate = due.AdjustmentDate;
                    old.Date = due.Date;
                    old.Notes = due.Notes;
                    old.Image = due.Image;
                    old.DepartmentId = due.DepartmentId;
                    old.DocumentNumber = due.DocumentNumber;
                    old.EmployeeId = due.EmployeeId;
                    old.InsuranceAmount = due.InsuranceAmount;
                    old.CurrentSalaryAmount = due.CurrentSalaryAmount;
                    old.IsCurrentSalary = due.IsCurrentSalary;
                    old.IsInsurance = due.IsInsurance;
                    old.IsLoanDue = due.IsLoanDue;
                    old.IsOtherAdditional = due.IsOtherAdditional;
                    old.IsOtherPenalty = due.IsOtherPenalty;
                    old.IsPreviousSalary = due.IsPreviousSalary;
                    old.IsTicket = due.IsTicket;
                    old.IsVacationSalaryDue = due.IsVacationSalaryDue;
                    old.IsVariableAdditional = due.IsVariableAdditional;
                    old.IsVariableDeduction = due.IsVariableDeduction;
                    old.LoanDueAmount = due.LoanDueAmount;
                    old.NetDueAmount = due.NetDueAmount;
                    old.OtherAdditionalAmount = due.OtherAdditionalAmount;
                    old.OtherPenaltyAmount = due.OtherPenaltyAmount;
                    old.PreviousSalaryAmount = due.PreviousSalaryAmount;
                    old.RecommendedPaymentAmount = due.RecommendedPaymentAmount;
                    old.TicketAmount = due.TicketAmount;
                    old.TotalDeductionAmount = due.TotalDeductionAmount;
                    old.TotalDueAmount = due.TotalDueAmount;
                    old.VacationRequestId = due.VacationRequestId;
                    old.VacationSalaryDueAmount = due.VacationSalaryDueAmount;
                    old.VariableAdditionalAmount = due.VariableAdditionalAmount;
                    old.VariableDeductionAmount = due.VariableDeductionAmount;
                    foreach (var item in due.EmployeeVacationDueDetails)
                    {
                        old.EmployeeVacationDueDetails.Add(item);
                    }

                    db.Entry(old).State = EntityState.Modified;
                    Notification.GetNotification("EmployeeVacationDue", "Edit", "AddEdit", due.Id, null, "مستحقات القيام بإجازة");
                }
                else
                {
                    //insert 
                    var req = db.VacationRequests.Find(due.VacationRequestId);
                    req.IsLinked = true;
                    due.DocumentNumber = new JavaScriptSerializer().Serialize(SetDocNum((int)due.DepartmentId, due.Date).Data).ToString().Trim('"');
                    db.EmployeeVacationDues.Add(due);
                    Notification.GetNotification("EmployeeVacationDue", "Add", "AddEdit", due.Id, null, "مستحقات القيام بإجازة");
                }
                try
                {
                    db.SaveChanges();
                }
                catch (Exception ex)
                {
                    var mod = ModelState.First(c => c.Key == "Code");  // this
                    mod.Value.Errors.Add("هذا الكود موجود من قبل");
                    return View(due);
                }
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = id > 0 ? "تعديل مستحقات القيام بإجازة" : "اضافة مستحقات القيام بإجازة",
                    EnAction = "AddEdit",
                    ControllerName = "EmployeeVacationDue",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    CodeOrDocNo = due.DocumentNumber,
                    SelectedItem = id,
                });
                return Json(new { success = "true" });
            }
            return View(due);
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
            var lastObj = db.EmployeeVacationDues.Where(a => a.IsActive == true && a.IsDeleted == false && a.DepartmentId == id).OrderByDescending(a => a.Id).FirstOrDefault();
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
                    lastObj = db.EmployeeVacationDues.Where(a => a.IsActive == true && a.IsDeleted == false && a.DepartmentId == id && a.Date.Value.Month == VoucherDate.Value.Month && a.Date.Value.Year == VoucherDate.Value.Year).OrderByDescending(a => a.Id).FirstOrDefault();
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
                    lastObj = db.EmployeeVacationDues.Where(a => a.IsActive == true && a.IsDeleted == false && a.DepartmentId == id && a.Date.Value.Year == VoucherDate.Value.Year).OrderByDescending(a => a.Id).FirstOrDefault();

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
            //var docNo = QueryHelper.DocLastNum(id, "EmployeeVacationDue");
            //double i = (docNo) + 1;
            //return Json(i, JsonRequestBehavior.AllowGet);
        }

        [SkipERPAuthorize]
        public JsonResult EmployeeVacationDueGetDetails(int id)
        {
            return Json(db.EmployeeVacationDue_GetDetails(id), JsonRequestBehavior.AllowGet);
        }

        [SkipERPAuthorize]
        public JsonResult GetEmployeeData(int id)
        {
            db.Configuration.ProxyCreationEnabled = false;
            var employee = db.Employees.Where(a => a.IsActive == true && a.IsDeleted == false && a.Id == id).Select(a => new
            {
                Id = a.Id,
                ArName = a.ArName,
                DepartmentId = a.DepartmentId,
                DepartmentArName = a.Department.ArName,
                HireDate = a.HireDate,
                AdministrativeDepartmentId = a.AdministrativeDepartmentId,
                AdministrativeDepartmentArName = a.AdministrativeDepartment.ArName,
                JobId = a.JobId,
                JobArName = a.Job.ArName,
                vacationWithoutSalaryBalance = a.EmployeeVacationOpeningBalanceDetails.Where(x => x.EmployeeId == a.Id).FirstOrDefault().VacationWithoutSalary,
                vacationwithoutSalaryRecord = a.VacationRequests.Where(z => z.EmployeeId == a.Id && z.IsAcceptedByManager == true && z.IsPaid == false && z.IsActive == true && z.IsDeleted == false).Select(z => z.NumberOfVacationDays).Sum()
            });
            return Json(employee, JsonRequestBehavior.AllowGet);
        }

        [SkipERPAuthorize]
        public JsonResult GetVacationRequest(int id)
        {
            db.Configuration.ProxyCreationEnabled = false;

            var VacationRequest = db.VacationRequests.Where(v => v.IsActive == true && v.IsDeleted == false && v.EmployeeId == id).Select(v => new
            {
                Id = v.Id,
                Code = v.DocumentNumber
            });
            return Json(VacationRequest, JsonRequestBehavior.AllowGet);
        }

        [SkipERPAuthorize]
        public JsonResult GetVacationRequestDetails(int id, DateTime? date)
        {
            db.Configuration.ProxyCreationEnabled = false;
            var VacationRequest = db.VacationRequests.Where(r => r.IsActive == true && r.IsDeleted == false && r.Id == id).FirstOrDefault();
            //الأضافات المتغيرة
            var rewards = db.RewardIssueDetials.Where(a => a.IsDeleted == false && a.RewardIssue.EmployeeId == VacationRequest.EmployeeId && a.RewardIssue.Month == date.Value.Month && a.RewardIssue.Year == date.Value.Year).ToList();
            var TotalRewards = rewards.Count() > 0 ? rewards.Sum(a => a.Total) : 0;
            var overtime = db.OvertimeIssueDetials.Where(a => a.IsDeleted == false && a.OvertimeIssue.EmployeeId == VacationRequest.EmployeeId && a.OvertimeIssue.Month == date.Value.Month && a.OvertimeIssue.Year == date.Value.Year).ToList();
            var TotalOverTime = overtime.Count() > 0 ? overtime.Sum(a => a.Total) : 0;
            var VariableAdditionalAmount = TotalRewards + TotalOverTime;
            // الاستقطاعات المتغير
            var Penality = db.PenaltyIssueDetails.Where(a => a.IsDeleted == false && a.PenaltyIssue.EmployeeId == VacationRequest.EmployeeId && a.PenaltyIssue.Month == date.Value.Month && a.PenaltyIssue.Year == date.Value.Year).ToList();
            var VariableDeductionAmount = Penality.Count() > 0 ? Penality.Sum(a => a.Total) : 0;
            //راتب الاجازة المستحقة
            var DueSalary = db.EmployeeMonthlyAllocationDetails.Where(a => a.IsDeleted == false && a.EmployeeMonthlyAllocation.Month == date.Value.Month && a.EmployeeMonthlyAllocation.Year == date.Value.Year && a.EmployeeId == VacationRequest.EmployeeId && a.EmployeeMonthlyAllocation.AllocationTypeId == 1).ToList();
            var VacationSalaryDueAmount = DueSalary.Count() > 0 ? DueSalary.Sum(a => a.Value) : 0;
            //الأجور السابقة
            var PreviousSalary = db.EmployeePayrollIssueDetails.Where(a => a.EmployeeId == VacationRequest.EmployeeId && a.IsIssued != true).ToList();
            var PreviousSalaryAmount = PreviousSalary.Count() > 0 ? PreviousSalary.Sum(a => a.NetSalary) : 0;
            // سلفة مستحقة
            var borrowDue = db.BorrowRequestDetails.Where(a => a.IsDeleted == false && a.BorrowRequest.EmployeeId == VacationRequest.EmployeeId && a.IsInstallmentPaid != true).ToList();
            var LoanDueAmount = borrowDue.Count() > 0 ? borrowDue.Sum(a => a.InstallmentValue) : 0;

            var VacationRequestDetail = db.VacationRequests.Where(r => r.IsActive == true && r.IsDeleted == false && r.Id == id).Select(r => new
            {
                Id = r.Id,
                NumberOfVacationDays = r.NumberOfVacationDays,
                VacationEndDate = r.VacationEndDate,
                VacationStartDate = r.VacationStartDate,
                lastVacation = db.VacationRequests.Where(a => a.IsActive == true && a.IsDeleted == false && a.Id == id).OrderByDescending(a => a.Id).FirstOrDefault().VacationStartDate,
                AbsenceDays = r.AbsenceDays,
                DutyDays = r.DutyDays,
                VacationWithoutSalaryDays = r.VacationWithoutSalaryDays,
                WorkDays = r.WorkDays,
                CarryOverBalanceDays = r.CarryOverBalanceDays,
                DeductionDays = r.DeductionDays,
                VacationDaysBeforeDeduction = r.NumberOfVacationDays,
                // ---- Financial Dues ---- //
                //راتب الشهر الحالى
                CurrentSalaryAmount = (r.Employee.EmployeeContracts.Where(a => a.IsDeleted == false && a.EmployeeId == r.EmployeeId).Select(a => a.EmployeeTotalSalary).FirstOrDefault()) / 30,
                //الأضافات المتغيرة
                VariableAdditionalAmount = VariableAdditionalAmount,
                //راتب الاجازة المستحقة
                VacationSalaryDueAmount = VacationSalaryDueAmount,
                //الأجور السابقة
                PreviousSalaryAmount = PreviousSalaryAmount,
                // سلفة مستحقة
                LoanDueAmount = LoanDueAmount,
                // الاستقطاعات المتغير
                VariableDeductionAmount = VariableDeductionAmount,
                // قيمة التأمين
                InsuranceAmount = db.SocialInsuranceDetails.Where(a => a.IsDeleted == false && a.EmployeeId == r.EmployeeId && a.SocialInsurance.Month == date.Value.Month && a.SocialInsurance.Year == date.Value.Year).Sum(a => a.NetSocialInsuranceSalary),
            });


            return Json(VacationRequestDetail, JsonRequestBehavior.AllowGet);
        }

        [HttpPost, ActionName("Delete")]
        public ActionResult Delete(int id)
        {
            try
            {
                EmployeeVacationDue due = db.EmployeeVacationDues.Find(id);
                due.IsDeleted = true;
                due.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                foreach (var d in due.EmployeeVacationDueDetails)
                {
                    d.IsDeleted = true;
                }
                Random random = new Random();
                const string chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
                var Code = new string(Enumerable.Repeat(chars, 6).Select(s => s[random.Next(s.Length)]).ToArray());
                due.DocumentNumber = Code;
                db.Entry(due).State = EntityState.Modified;

                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "حذف مستحقات القيام بإجازة",
                    EnAction = "AddEdit",
                    ControllerName = "EmployeeVacationDue",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id
                });
                ////-------------------- Notification-------------------------////
                Notification.GetNotification("EmployeeVacationDue", "Delete", "Delete", id, null, "مستحقات القيام بإجازة");


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