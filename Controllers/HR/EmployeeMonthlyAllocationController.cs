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
    public class EmployeeMonthlyAllocationController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        // GET: EmployeeMonthlyAllocation
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
                ArAction = "فتح قائمة المخصصات الشهرية للموظفين",
                EnAction = "Index",
                ControllerName = "EmployeeMonthlyAllocation",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });

            ////-------------------- Notification-------------------------////
            Notification.GetNotification("EmployeeMonthlyAllocation", "View", "Index", null, null, "المخصصات الشهرية للموظفين");
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;

            IQueryable<EmployeeMonthlyAllocation> employeeMonthlyAllocations;
            if (string.IsNullOrEmpty(searchWord))
            {
                if (userId == 1)
                {
                    employeeMonthlyAllocations = db.EmployeeMonthlyAllocations.Where(s => s.IsDeleted == false && (departmentId == 0 || s.DepartmentId == departmentId)).OrderByDescending(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                    ViewBag.Count = db.EmployeeMonthlyAllocations.Where(s => s.IsDeleted == false && (departmentId == 0 || s.DepartmentId == departmentId)).Count();
                }
                else
                {
                    employeeMonthlyAllocations = db.EmployeeMonthlyAllocations.Where(s => s.IsDeleted == false && (db.UserDepartments.Where(a => a.UserId == userId).Select(a => a.DepartmentId).Contains(s.DepartmentId)) && (departmentId == 0 || s.DepartmentId == departmentId)).OrderByDescending(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                    ViewBag.Count = db.EmployeeMonthlyAllocations.Where(s => s.IsDeleted == false&& (db.UserDepartments.Where(a => a.UserId == userId).Select(a => a.DepartmentId).Contains(s.DepartmentId)) && (departmentId == 0 || s.DepartmentId == departmentId)).Count();
                }
            }
            else
            {
                if (userId == 1)
                {
                    employeeMonthlyAllocations = db.EmployeeMonthlyAllocations.Where(s => s.IsDeleted == false && (departmentId == 0 || s.DepartmentId == departmentId) && (s.DocumentNumber.Contains(searchWord) || s.Notes.Contains(searchWord) || s.Department.ArName.Contains(searchWord))).OrderByDescending(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                    ViewBag.Count = db.EmployeeMonthlyAllocations.Where(s => s.IsDeleted == false && (departmentId == 0 || s.DepartmentId == departmentId) && (s.DocumentNumber.Contains(searchWord) || s.Notes.Contains(searchWord) || s.Department.ArName.Contains(searchWord))).Count();
                }
                else
                {
                    employeeMonthlyAllocations = db.EmployeeMonthlyAllocations.Where(s => s.IsDeleted == false&& (db.UserDepartments.Where(a => a.UserId == userId).Select(a => a.DepartmentId).Contains(s.DepartmentId)) && (departmentId == 0 || s.DepartmentId == departmentId) && (s.DocumentNumber.Contains(searchWord) || s.Notes.Contains(searchWord) || s.Department.ArName.Contains(searchWord))).OrderByDescending(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                    ViewBag.Count = db.EmployeeMonthlyAllocations.Where(s => s.IsDeleted == false&& (db.UserDepartments.Where(a => a.UserId == userId).Select(a => a.DepartmentId).Contains(s.DepartmentId)) && (departmentId == 0 || s.DepartmentId == departmentId) && (s.DocumentNumber.Contains(searchWord) || s.Notes.Contains(searchWord) || s.Department.ArName.Contains(searchWord))).Count();
                }
            }

            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            ViewBag.PageIndex = pageIndex;
            return View(employeeMonthlyAllocations.ToList());
        }

        // GET: EmployeeMonthlyAllocation/Edit/5
        public ActionResult AddEdit(int? id)
        {
            int userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            DepartmentRepository departmentRepository = new DepartmentRepository(db);
            SystemSetting systemSetting = db.SystemSettings.Any() ? db.SystemSettings.FirstOrDefault() : new SystemSetting();
            if (id == null)
            {
                ViewBag.AllocationTypeId = new SelectList(db.AllocationTypes.Where(e => e.IsDeleted == false && e.IsActive == true).Select(b => new
                {
                    b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");

                ViewBag.DepartmentId = new SelectList(departmentRepository.UserDepartments(userId).ToList(), "Id", "ArName", systemSetting.DefaultDepartmentId);
                ViewBag.Year = new SelectList(new int[] { 2019, 2020, 2021, 2022, 2023, 2024, 2025, 2026, 2027 });
                ViewBag.Month = new SelectList(new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 });
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
            EmployeeMonthlyAllocation employeeMonthlyAllocation = db.EmployeeMonthlyAllocations.Find(id);
            if (employeeMonthlyAllocation == null)
            {
                return HttpNotFound();
            }
            int sysPageId = QueryHelper.SourcePageId("EmployeeMonthlyAllocation");
            ViewBag.JE = db.GetJournalEntryBySourceIdAndSourcePageId(id, sysPageId).FirstOrDefault();
            if (ViewBag.JE != null)
            {
                var JEDocNo = ViewBag.JE;
                ViewBag.JE.DocumentNumber = JEDocNo != null ? JEDocNo.DocumentNumber.Replace("-", "") : "";
                int JEId = ViewBag.JE.Id;
                int sourcePageId = ViewBag.JE.SourcePageId;
                ViewBag.JEDetails = db.JournalEntryDetails.Where(a => a.SourceId == id && a.JournalEntryId == JEId && a.SourcePageId == sourcePageId).OrderBy(a => a.Id).ToList();
            }

            ViewBag.DepartmentId = new SelectList(departmentRepository.UserDepartments(userId).ToList(), "Id", "ArName", employeeMonthlyAllocation.DepartmentId);
            ViewBag.AllocationTypeId = new SelectList(db.AllocationTypes.Where(e => e.IsDeleted == false && e.IsActive == true).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", employeeMonthlyAllocation.AllocationTypeId);
            ViewBag.Year = new SelectList(new int[] { 2019, 2020, 2021, 2022, 2023, 2024, 2025, 2026, 2027 }, employeeMonthlyAllocation.Year);
            ViewBag.Month = new SelectList(new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 }, employeeMonthlyAllocation.Month);

            ViewBag.Next = QueryHelper.Next((int)id, "EmployeeMonthlyAllocation");
            ViewBag.Previous = QueryHelper.Previous((int)id, "EmployeeMonthlyAllocation");
            ViewBag.Last = QueryHelper.GetLast("EmployeeMonthlyAllocation");
            ViewBag.First = QueryHelper.GetFirst("EmployeeMonthlyAllocation");
            try
            {
                ViewBag.VoucherDate = employeeMonthlyAllocation.VoucherDate.Value.ToString("yyyy-MM-ddTHH:mm");

            }
            catch (Exception e)
            {
            }

            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح تفاصيل المخصصات الشهرية للموظفين",
                EnAction = "AddEdit",
                ControllerName = "EmployeeMonthlyAllocation",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = employeeMonthlyAllocation.Id,
                CodeOrDocNo = employeeMonthlyAllocation.DocumentNumber
            });
            return View(employeeMonthlyAllocation);
        }

        // POST: EmployeeMonthlyAllocation/Edit/5
        [HttpPost]
        public ActionResult AddEdit(EmployeeMonthlyAllocation employeeMonthlyAllocation)
        {
            int userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

            if (ModelState.IsValid)
            {
                var id = employeeMonthlyAllocation.Id;
                employeeMonthlyAllocation.IsDeleted = false;

                if (employeeMonthlyAllocation.Id > 0)
                {
                    employeeMonthlyAllocation.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    MyXML.xPathName = "Details";
                    var EmployeeMonthlyAllocationDetails = MyXML.GetXML(employeeMonthlyAllocation.EmployeeMonthlyAllocationDetails);
                    db.EmployeeMonthlyAllocation_Update(employeeMonthlyAllocation.Id, employeeMonthlyAllocation.DocumentNumber, employeeMonthlyAllocation.VoucherDate, employeeMonthlyAllocation.DepartmentId, employeeMonthlyAllocation.Month, employeeMonthlyAllocation.Year, employeeMonthlyAllocation.AllocationTypeId, userId, false, employeeMonthlyAllocation.Notes, employeeMonthlyAllocation.Image, EmployeeMonthlyAllocationDetails);

                    //////-------------------- Notification-------------------------////
                    Notification.GetNotification("EmployeeMonthlyAllocation", "Edit", "AddEdit", id, null, "المخصصات الشهرية للموظفين");
                }
                else
                {
                    employeeMonthlyAllocation.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    var idResult = new ObjectParameter("Id", typeof(Int32));
                    MyXML.xPathName = "Details";
                    var EmployeeMonthlyAllocationDetails = MyXML.GetXML(employeeMonthlyAllocation.EmployeeMonthlyAllocationDetails);
                    db.EmployeeMonthlyAllocation_Insert(idResult, employeeMonthlyAllocation.VoucherDate, employeeMonthlyAllocation.DepartmentId, employeeMonthlyAllocation.Month, employeeMonthlyAllocation.Year, employeeMonthlyAllocation.AllocationTypeId, userId, false, employeeMonthlyAllocation.Notes, employeeMonthlyAllocation.Image, EmployeeMonthlyAllocationDetails);
                    id = (int)idResult.Value;

                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("EmployeeMonthlyAllocation", "Add", "AddEdit", employeeMonthlyAllocation.Id, null, "المخصصات الشهرية للموظفين");
                }
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = id > 0 ? "تعديل المخصصات الشهرية للموظفين" : "اضافة المخصصات الشهرية للموظفين",
                    EnAction = "AddEdit",
                    ControllerName = "EmployeeMonthlyAllocation",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    CodeOrDocNo = employeeMonthlyAllocation.DocumentNumber
                });
                return Json(new { success = "true", id });
            }
            var errors = ModelState
                   .Where(x => x.Value.Errors.Count > 0)
                   .Select(x => new { x.Key, x.Value.Errors })
                   .ToArray();
            return Json(new { success = false, errors });
            //DepartmentRepository departmentRepository = new DepartmentRepository(db);
            //ViewBag.DepartmentId = new SelectList( departmentRepository.UserDepartments(userId).ToList(), "Id", "ArName", employeeMonthlyAllocation.DepartmentId);
            //ViewBag.Year = new SelectList(new int[] { 2019, 2020, 2021, 2022, 2023, 2024, 2025, 2026, 2027 }, employeeMonthlyAllocation.Year);
            //ViewBag.Month = new SelectList(new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 }, employeeMonthlyAllocation.Month);
            //ViewBag.AllocationTypeId = new SelectList(db.AllocationTypes.Where(e => e.IsDeleted == false && e.IsActive == true).Select(b => new
            //{
            //    b.Id,
            //    ArName = b.Code + " - " + b.ArName
            //}), "Id", "ArName", employeeMonthlyAllocation.AllocationTypeId);

            //try
            //{
            //    ViewBag.VoucherDate = employeeMonthlyAllocation.VoucherDate.Value.ToString("yyyy-MM-ddTHH:mm");
            //}
            //catch (Exception e)
            //{
            //}
            //return View(employeeMonthlyAllocation);
        }

        // POST: EmployeeMonthlyAllocation/Delete/5
        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                EmployeeMonthlyAllocation employeeMonthlyAllocation = db.EmployeeMonthlyAllocations.Find(id);

                db.EmployeeMonthlyAllocation_Delete(id, userId);

                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "حذف المخصصات الشهرية للموظفين",
                    EnAction = "AddEdit",
                    ControllerName = "EmployeeMonthlyAllocation",
                    UserName = User.Identity.Name,
                    UserId = userId,
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = employeeMonthlyAllocation.Id,
                    CodeOrDocNo = employeeMonthlyAllocation.DocumentNumber
                });
                ////-------------------- Notification-------------------------////
                Notification.GetNotification("EmployeeMonthlyAllocation", "Delete", "Delete", id, null, "المخصصات الشهرية للموظفين");
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
            var lastObj = db.EmployeeMonthlyAllocations.Where(a => a.IsDeleted == false && a.DepartmentId == id).OrderByDescending(a => a.Id).FirstOrDefault();
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
                    lastObj = db.EmployeeMonthlyAllocations.Where(a => a.IsDeleted == false && a.DepartmentId == id && a.VoucherDate.Value.Month == VoucherDate.Value.Month && a.VoucherDate.Value.Year == VoucherDate.Value.Year).OrderByDescending(a => a.Id).FirstOrDefault();
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
                    lastObj = db.EmployeeMonthlyAllocations.Where(a => a.IsDeleted == false && a.DepartmentId == id && a.VoucherDate.Value.Year == VoucherDate.Value.Year).OrderByDescending(a => a.Id).FirstOrDefault();

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
            //var docNo = QueryHelper.DocLastNum(id, "EmployeeMonthlyAllocation");
            //double i = (docNo) + 1;
            //return Json(i, JsonRequestBehavior.AllowGet);
        }

        [SkipERPAuthorize]
        public JsonResult GetEmployeeMonthlyAllocation(int? departmentId, int? allocationType)
        {
            var employees = db.GetEmployeeMonthlyAllocation(departmentId, allocationType).ToList();
            return Json(employees, JsonRequestBehavior.AllowGet);
        }

        [SkipERPAuthorize]
        public JsonResult PreviousEmployeeMonthlyAllocation(/*int? departmentId,*/ int? allocationType, int? month, int? year)
        {
            db.Configuration.ProxyCreationEnabled = false;
            var PreviousEmployees = db.EmployeeMonthlyAllocations.Where(a => a.IsDeleted == false/*&&a.DepartmentId==departmentId*/&& a.AllocationTypeId == allocationType && a.Month == month && a.Year == year).ToList();
            return Json(PreviousEmployees, JsonRequestBehavior.AllowGet);
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