using MyERP.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;

namespace MyERP.Controllers.HR
{
    public class EndOfServiceRewardAccountController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();
        // GET: EndOfServiceRewardAccount
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
                ArAction = "فتح قائمة حساب مكافأة نهاية الخدمة",
                EnAction = "Index",
                ControllerName = "EndOfServiceRewardAccount",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            ////-------------------- Notification-------------------------////
            Notification.GetNotification("EndOfServiceRewardAccount", "View", "Index", null, null, " حساب مكافأة نهاية الخدمة");
            //////////-----------------------------------------------------------------------
            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;

            IQueryable<EndOfServiceRewardAccount> endOfServiceRewardAccounts;
            if (string.IsNullOrEmpty(searchWord))
            {
                if (userId == 1)
                {
                    endOfServiceRewardAccounts = db.EndOfServiceRewardAccounts.Where(s => s.IsDeleted == false && (departmentId == 0 || s.DepartmentId == departmentId)).OrderByDescending(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                    ViewBag.Count = db.EndOfServiceRewardAccounts.Where(s => s.IsDeleted == false && (departmentId == 0 || s.DepartmentId == departmentId)).Count();
                }
                else
                {
                    endOfServiceRewardAccounts = db.EndOfServiceRewardAccounts.Where(s => s.IsDeleted == false && (db.UserDepartments.Where(a => a.UserId == userId).Select(a => a.DepartmentId).Contains(s.DepartmentId)) && (departmentId == 0 || s.DepartmentId == departmentId)).OrderByDescending(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                    ViewBag.Count = db.EndOfServiceRewardAccounts.Where(s => s.IsDeleted == false&& (db.UserDepartments.Where(a => a.UserId == userId).Select(a => a.DepartmentId).Contains(s.DepartmentId)) && (departmentId == 0 || s.DepartmentId == departmentId)).Count();
                }
            }
            else
            {
                if (userId == 1)
                {
                    endOfServiceRewardAccounts = db.EndOfServiceRewardAccounts.Where(s => s.IsDeleted == false && (departmentId == 0 || s.DepartmentId == departmentId) && (s.DocumentNumber.Contains(searchWord) || s.Department.ArName.Contains(searchWord) || s.Department.EnName.Contains(searchWord) || s.Notes.Contains(searchWord))).OrderByDescending(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                    ViewBag.Count = db.EndOfServiceRewardAccounts.Where(s => s.IsDeleted == false && (departmentId == 0 || s.DepartmentId == departmentId) && (s.DocumentNumber.Contains(searchWord) || s.Department.ArName.Contains(searchWord) || s.Department.EnName.Contains(searchWord) || s.Notes.Contains(searchWord))).Count();
                }
                else
                {
                    endOfServiceRewardAccounts = db.EndOfServiceRewardAccounts.Where(s => s.IsDeleted == false&& (db.UserDepartments.Where(a => a.UserId == userId).Select(a => a.DepartmentId).Contains(s.DepartmentId)) && (departmentId == 0 || s.DepartmentId == departmentId) && (s.DocumentNumber.Contains(searchWord) || s.Department.ArName.Contains(searchWord) || s.Department.EnName.Contains(searchWord) || s.Notes.Contains(searchWord))).OrderByDescending(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                    ViewBag.Count = db.EndOfServiceRewardAccounts.Where(s => s.IsDeleted == false&& (db.UserDepartments.Where(a => a.UserId == userId).Select(a => a.DepartmentId).Contains(s.DepartmentId)) && (departmentId == 0 || s.DepartmentId == departmentId) && (s.DocumentNumber.Contains(searchWord) || s.Department.ArName.Contains(searchWord) || s.Department.EnName.Contains(searchWord) || s.Notes.Contains(searchWord))).Count();
                }
            }
            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            return View(endOfServiceRewardAccounts.ToList());
        }
        // GET: EndOfServiceRewardAccount/Edit/5
        public ActionResult AddEdit(int? id)
        {
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            if (id == null)
            {
                ViewBag.EmployeeId = new SelectList(db.Employees.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.ArName
                }), "Id", "ArName");

                ViewBag.DepartmentId = new SelectList(db.Departments.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.ArName
                }), "Id", "ArName");
                ViewBag.EndOfServiceTypeId = new SelectList(db.EndOfServiceTypes.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.ArName
                }), "Id", "ArName");
                ViewBag.JobId = new SelectList(db.Jobs.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.ArName
                }), "Id", "ArName");
                ViewBag.TerminatorEmployeeId = new SelectList(db.Employees.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.ArName
                }), "Id", "ArName");
                return View();
            }
            EndOfServiceRewardAccount endOfServiceRewardAccount = db.EndOfServiceRewardAccounts.Find(id);
            if (endOfServiceRewardAccount == null)
            {
                return HttpNotFound();
            }
            ViewBag.Next = QueryHelper.Next((int)id, "EndOfServiceRewardAccount");
            ViewBag.Previous = QueryHelper.Previous((int)id, "EndOfServiceRewardAccount");
            ViewBag.Last = QueryHelper.GetLast("EndOfServiceRewardAccount");
            ViewBag.First = QueryHelper.GetFirst("EndOfServiceRewardAccount");

            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح تفاصيل حساب مكافأة نهاية الخدمة",
                EnAction = "AddEdit",
                ControllerName = "EndOfServiceRewardAccount",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = endOfServiceRewardAccount.Id,
                CodeOrDocNo = endOfServiceRewardAccount.DocumentNumber
            });

            ViewBag.EmployeeId = new SelectList(db.Employees.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
            {
                Id = b.Id,
                ArName = b.ArName
            }), "Id", "ArName", endOfServiceRewardAccount.EmployeeId);

            ViewBag.DepartmentId = new SelectList(db.Departments.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
            {
                Id = b.Id,
                ArName = b.ArName
            }), "Id", "ArName", endOfServiceRewardAccount.DepartmentId);
            ViewBag.EndOfServiceTypeId = new SelectList(db.EndOfServiceTypes.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
            {
                Id = b.Id,
                ArName = b.ArName
            }), "Id", "ArName", endOfServiceRewardAccount.EndOfServiceTypeId);
            ViewBag.JobId = new SelectList(db.Jobs.Select(b => new
            {
                Id = b.Id,
                ArName = b.ArName
            }), "Id", "ArName", endOfServiceRewardAccount.JobId);
            ViewBag.TerminatorEmployeeId = new SelectList(db.Employees.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
            {
                Id = b.Id,
                ArName = b.ArName
            }), "Id", "ArName", endOfServiceRewardAccount.TerminatorEmployeeId);
            try
            {
                ViewBag.VoucherDate = endOfServiceRewardAccount.VoucherDate.Value.ToString("yyyy-MM-ddTHH:mm");
                ViewBag.WorkStartDate = endOfServiceRewardAccount.WorkStartDate.Value.ToString("yyyy-MM-ddTHH:mm");
                ViewBag.WorkEndDate = endOfServiceRewardAccount.WorkEndDate.Value.ToString("yyyy-MM-ddTHH:mm");
            }
            catch (Exception e)
            {
                throw e;
            }
            return View(endOfServiceRewardAccount);
        }
        [HttpPost]
        public ActionResult AddEdit(EndOfServiceRewardAccount endOfServiceRewardAccount/*, string newBtn*/)
        {
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            if (ModelState.IsValid)
            {
                var id = endOfServiceRewardAccount.Id;
                endOfServiceRewardAccount.IsDeleted = false;
                if (endOfServiceRewardAccount.Id > 0)
                {
                    endOfServiceRewardAccount.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    db.EndOfServiceSalaryItems.RemoveRange(db.EndOfServiceSalaryItems.Where(x => x.EndOfServiceId == endOfServiceRewardAccount.Id));
                    var endOfServiceSalaryItems = endOfServiceRewardAccount.EndOfServiceSalaryItems.ToList();
                    //endOfServiceSalaryItems.ForEach((x) => x.EndOfServiceId = endOfServiceRewardAccount.Id);
                    //endOfServiceRewardAccount.EndOfServiceSalaryItems = null;
                    //db.EndOfServiceSalaryItems.AddRange(endOfServiceSalaryItems);
                    db.Entry(endOfServiceRewardAccount).State = EntityState.Modified;
                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("EndOfServiceRewardAccount", "Edit", "AddEdit", endOfServiceRewardAccount.Id, null, "حساب مكافأة نهاية الخدمة");
                }
                else
                {
                    endOfServiceRewardAccount.DocumentNumber = new JavaScriptSerializer().Serialize(SetDocNum((int)endOfServiceRewardAccount.DepartmentId, endOfServiceRewardAccount.VoucherDate).Data).ToString().Trim('"');
                    endOfServiceRewardAccount.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    db.EndOfServiceRewardAccounts.Add(endOfServiceRewardAccount);
                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("EndOfServiceRewardAccount", "Add", "AddEdit", endOfServiceRewardAccount.Id, null, "حساب مكافأة نهاية الخدمة");
                }
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = endOfServiceRewardAccount.Id > 0 ? "تعديل حساب مكافأة نهاية الخدمة" : "اضافة حساب مكافأة نهاية الخدمة",
                    EnAction = "AddEdit",
                    ControllerName = "EndOfServiceRewardAccount",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "Post",
                    SelectedItem = endOfServiceRewardAccount.Id,
                    CodeOrDocNo = endOfServiceRewardAccount.DocumentNumber
                });
                //if (newBtn == "saveAndNew")
                //{
                //    return RedirectToAction("AddEdit");

                //}
                //else
                //{
                //    return RedirectToAction("Index");
                //}

                return Json(new { success = "true" });

            }

            var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .Select(x => new { x.Key, x.Value.Errors })
                    .ToArray();
            return Json(new { success = "false", errors });

        }

        // POST: EndOfServiceRewardAccount/Delete/5
        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                EndOfServiceRewardAccount endOfServiceRewardAccount = db.EndOfServiceRewardAccounts.Find(id);
                endOfServiceRewardAccount.IsDeleted = true;
                endOfServiceRewardAccount.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                foreach (var item in endOfServiceRewardAccount.EndOfServiceSalaryItems)
                {
                    item.IsDeleted = true;
                }
                Random random = new Random();
                const string chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
                var Code = new string(Enumerable.Repeat(chars, 6).Select(s => s[random.Next(s.Length)]).ToArray());
                endOfServiceRewardAccount.DocumentNumber = Code;
                db.Entry(endOfServiceRewardAccount).State = EntityState.Modified;
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "حذف حساب مكافأة نهاية الخدمة",
                    EnAction = "AddEdit",
                    ControllerName = "EndOfServiceRewardAccount",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    CodeOrDocNo = endOfServiceRewardAccount.DocumentNumber
                });
                ////-------------------- Notification-------------------------////
                Notification.GetNotification("EndOfServiceRewardAccount", "Delete", "Delete", id, null, " حساب مكافأة نهاية الخدمة");
                ///////-----------------------------------------------------------------------
                return Content("true");
            }
            catch (Exception ex)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
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
            var lastObj = db.EndOfServiceRewardAccounts.Where(a => a.IsDeleted == false && a.DepartmentId == id).OrderByDescending(a => a.Id).FirstOrDefault();
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
                    lastObj = db.EndOfServiceRewardAccounts.Where(a => a.IsDeleted == false && a.DepartmentId == id && a.VoucherDate.Value.Month == VoucherDate.Value.Month && a.VoucherDate.Value.Year == VoucherDate.Value.Year).OrderByDescending(a => a.Id).FirstOrDefault();
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
                    lastObj = db.EndOfServiceRewardAccounts.Where(a => a.IsDeleted == false && a.DepartmentId == id && a.VoucherDate.Value.Year == VoucherDate.Value.Year).OrderByDescending(a => a.Id).FirstOrDefault();

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
            //var docNo = QueryHelper.DocLastNum(id, "EndOfServiceRewardAccount");
            //double i = (docNo) + 1;
            //return Json(i, JsonRequestBehavior.AllowGet);
        }

        [SkipERPAuthorize]
        public JsonResult GetEmployeeContractSalaryItem(int? employeeId)
        {
            // اجمالى مفردات الراتب
            var employeeSalaryItems = db.EmployeeContractSalaryItems.Where(a => a.EmployeeId == employeeId).ToList();
            var employeeSalaryItem = employeeSalaryItems != null ? employeeSalaryItems.Sum(a => a.ItemValue) : 0;

            //مخصص نهاية الخدمة
            var endOfServiceAllocation = db.EmployeeMonthlyAllocationDetails.Where(a => a.EmployeeMonthlyAllocation.AllocationTypeId == 3 && a.EmployeeId == employeeId && a.IsDeleted == false).ToList();
            var endOfServiceAllocationDue = endOfServiceAllocation != null ? endOfServiceAllocation.Sum(a => a.Value) : 0;
            // مخصص الاجازات
            var VacationAllocation = db.EmployeeMonthlyAllocationDetails.Where(a => a.EmployeeMonthlyAllocation.AllocationTypeId == 1 && a.EmployeeId == employeeId && a.IsDeleted == false).ToList();
            var vacationAllocationDue = VacationAllocation != null ? VacationAllocation.Sum(a => a.Value) : 0;
            // مخصص التذاكر
            var ticketAllocation = db.EmployeeMonthlyAllocationDetails.Where(a => a.EmployeeMonthlyAllocation.AllocationTypeId == 2 && a.EmployeeId == employeeId && a.IsDeleted == false).ToList();
            var ticketAllocationDue = ticketAllocation != null ? ticketAllocation.Sum(a => a.Value) : 0;
            // تاريخ التعيين
            var WorkStartDates = db.EmployeeContracts.Where(a => a.IsActive == true && a.IsDeleted == false && a.EmployeeId == employeeId).FirstOrDefault();
            var WorkStartDate = WorkStartDates != null ? WorkStartDates.HireDate.Value.ToString("yyyy-MM-ddTHH:mm") : null;
            //راتب الشهر الحالى
            var CurrentSalaryAmounts = db.EmployeeContracts.Where(a => a.IsDeleted == false && a.EmployeeId == employeeId).Select(a => a.EmployeeTotalSalary).FirstOrDefault();
            var CurrentSalaryAmount = CurrentSalaryAmounts != null ? CurrentSalaryAmounts / 30 : 0;
            // عدد ايام الاجازة بدون راتب
            var vacationWithoutSalaryBalances = db.EmployeeVacationOpeningBalanceDetails.Where(x => x.EmployeeId == employeeId && x.IsDeleted == false).FirstOrDefault();
            var vacationWithoutSalaryBalance = vacationWithoutSalaryBalances != null ? vacationWithoutSalaryBalances.VacationWithoutSalary : 0;
            var vacationwithoutSalaryRecords = db.VacationRequests.Where(z => z.EmployeeId == employeeId && z.IsAcceptedByManager == true && z.IsPaid == false && z.IsActive == true && z.IsDeleted == false).Select(z => z.NumberOfVacationDays).ToList();
            var vacationwithoutSalaryRecord = vacationwithoutSalaryRecords != null ? vacationwithoutSalaryRecords.Sum() : 0;
            var VacationWithoutSalaryDay = vacationWithoutSalaryBalance + vacationWithoutSalaryBalance;
            // ايام الغياب
            var EmployeeAbsence = db.EmployeeAbsences.Where(a => a.IsActive == true && a.IsDeleted == false && a.EmployeeId == employeeId).ToList();
            var NoOfAbsenceDays = EmployeeAbsence != null ? EmployeeAbsence.Count() : 0;
            // اجمالى السلف
            var borrowDue = db.BorrowRequestDetails.Where(a => a.IsDeleted == false && a.BorrowRequest.EmployeeId == employeeId && a.IsInstallmentPaid != true).ToList();
            var TotalBorrow = borrowDue.Count() > 0 ? borrowDue.Sum(a => a.InstallmentValue) : 0;
            // اجمالى العهدة
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            var ErpUsers = db.ERPUsers.Where(a => a.EmployeeId == employeeId && a.IsActive == true && a.IsDeleted == false).FirstOrDefault();
            var ErpUser = ErpUsers != null ? ErpUsers.Id : 0;
            var _UserCashBox = db.UserCashBoxes.Where(b => b.UserId == ErpUser && b.Privilege == true).FirstOrDefault();
            var cashboxid = _UserCashBox != null ? _UserCashBox.CashBoxId : null;
            //var cashboxid = userId == 1 ? db.CashBoxes.Where(a => a.IsDeleted == false && a.IsActive == true).FirstOrDefault().Id : db.UserCashBoxes.Where(b => b.UserId == ErpUser && b.Privilege == true).FirstOrDefault().CashBoxId;
            var TotalCustody = cashboxid != null ? db.CashBox_Balances(cashboxid).FirstOrDefault().Balance : 0;
            var prevEmp = db.EndOfServiceRewardAccounts.Where(a => a.IsDeleted == false && a.EmployeeId == employeeId).FirstOrDefault();
            var prev = prevEmp != null ? prevEmp.Id : 0;
            var jobId = db.Employees.Where(a => a.IsActive == true && a.IsDeleted == false && a.Id == employeeId).FirstOrDefault().JobId;
            var EmployeeJob = db.Jobs.Where(a => a.IsActive == true && a.IsDeleted == false && a.Id == jobId).Select(a => new
            {
                a.Id,
                a.ArName
            });
            return Json(new { prev = prev, EmployeeJob = EmployeeJob, employeeSalaryItem = employeeSalaryItem, vacationAllocationDue = vacationAllocationDue, ticketAllocationDue = ticketAllocationDue, endOfServiceAllocationDue = endOfServiceAllocationDue, WorkStartDate = WorkStartDate, CurrentSalaryAmount = CurrentSalaryAmount, VacationWithoutSalaryDay = VacationWithoutSalaryDay, TotalBorrow = TotalBorrow, NoOfAbsenceDays = NoOfAbsenceDays, TotalCustody = TotalCustody }, JsonRequestBehavior.AllowGet);
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