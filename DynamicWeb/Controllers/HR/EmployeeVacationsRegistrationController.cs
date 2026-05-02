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
    public class EmployeeVacationsRegistrationController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        // GET: EmployeeVacationsRegistration
        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {

            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة تسجيل الاجازات",
                EnAction = "Index",
                ControllerName = "EmployeeVacationsRegistration",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            ////-------------------- Notification-------------------------////
            Notification.GetNotification("EmployeeVacationsRegistration", "View", "Index", null, null, "تسجيل الاجازات");

            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;

            IQueryable<EmployeeVacationsRegistration> vacationsRegistrations;

            if (string.IsNullOrEmpty(searchWord))
            {
                vacationsRegistrations = db.EmployeeVacationsRegistrations.Where(s => s.IsDeleted == false)/*.Include(s => s.Department)*/.OrderByDescending(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.EmployeeVacationsRegistrations.Where(s => s.IsDeleted == false).Count();
            }
            else
            {
                vacationsRegistrations = db.EmployeeVacationsRegistrations.Where(s => s.IsDeleted == false && (s.DocumentNumber.Contains(searchWord)
                || s.Department.ArName.Contains(searchWord) || s.Department.EnName.Contains(searchWord)
                || s.Employee.ArName.Contains(searchWord) || s.Employee.EnName.Contains(searchWord)))
                   /* .Include(s => s.Department)*/.OrderByDescending(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.EmployeeVacationsRegistrations.Where(s => s.IsDeleted == false && (s.DocumentNumber.Contains(searchWord)
                || s.Department.ArName.Contains(searchWord) || s.Department.EnName.Contains(searchWord)
                || s.Employee.ArName.Contains(searchWord) || s.Employee.EnName.Contains(searchWord)
                )).Count();
            }

            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة تسجيل الاجازات",
                EnAction = "Index",
                ControllerName = "EmployeeVacationsRegistration",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            return View(vacationsRegistrations.ToList());
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
                ViewBag.VacationFrom = cTime.ToString("yyyy-MM-ddTHH:mm");
                ViewBag.VacationTo = cTime.ToString("yyyy-MM-ddTHH:mm");
                return View();
            }
            EmployeeVacationsRegistration vacationsRegistration = db.EmployeeVacationsRegistrations.Find(id);
            if (vacationsRegistration == null)
            {
                return HttpNotFound();
            }
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "اضافة او تعديل تسجيل الاجازات ",
                EnAction = "AddEdit",
                ControllerName = "EmployeeVacationsRegistration",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                CodeOrDocNo = vacationsRegistration.DocumentNumber,
                SelectedItem = id
            });

            ViewBag.Next = QueryHelper.Next((int)id, "EmployeeVacationsRegistration");
            ViewBag.Previous = QueryHelper.Previous((int)id, "EmployeeVacationsRegistration");
            ViewBag.Last = QueryHelper.GetLast("EmployeeVacationsRegistration");
            ViewBag.First = QueryHelper.GetFirst("EmployeeVacationsRegistration");
            // Departments
            if (userId == 1)
            {
                //DepartmentId
                ViewBag.DepartmentId = new SelectList(db.Departments.Where(d => d.IsActive == true && d.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName", vacationsRegistration.DepartmentId);
            }
            else
            {
                //DepartmentId
                ViewBag.DepartmentId = new SelectList(db.UserDepartments.Where(d => d.UserId == userId).Select(b => new
                {
                    Id = b.DepartmentId,
                    ArName = b.Department.Code + " - " + b.Department.ArName
                }), "Id", "ArName", vacationsRegistration.DepartmentId);
            }
            //EmployeeId
            ViewBag.EmployeeId = new SelectList(db.Employees.Where(d => d.IsActive == true && d.IsDeleted == false).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", vacationsRegistration.EmployeeId);

            ViewBag.Date = vacationsRegistration.Date.Value.ToString("yyyy-MM-ddTHH:mm");
            ViewBag.VacationFrom = vacationsRegistration.VacationFrom.Value.ToString("yyyy-MM-ddTHH:mm");
            ViewBag.VacationTo = vacationsRegistration.VacationTo.Value.ToString("yyyy-MM-ddTHH:mm");

            return View(vacationsRegistration);

        }

        [HttpPost]
        public ActionResult AddEdit(EmployeeVacationsRegistration vacationsRegistration)
        {
            if (ModelState.IsValid)
            {
                var id = vacationsRegistration.Id;
                vacationsRegistration.IsDeleted = false;
                vacationsRegistration.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                if (vacationsRegistration.Id > 0)
                {
                    // update 
                    db.Entry(vacationsRegistration).State = EntityState.Modified;
                    Notification.GetNotification("EmployeeVacationsRegistration", "Edit", "AddEdit", vacationsRegistration.Id, null, "تسجيل الاجازات");
                }
                else
                {
                    //insert 
                    vacationsRegistration.DocumentNumber = new JavaScriptSerializer().Serialize(SetDocNum((int)vacationsRegistration.DepartmentId, vacationsRegistration.Date).Data).ToString().Trim('"');
                    db.EmployeeVacationsRegistrations.Add(vacationsRegistration);
                    Notification.GetNotification("EmployeeVacationsRegistration", "Add", "AddEdit", vacationsRegistration.Id, null, "تسجيل الاجازات");
                }
                try
                {
                    db.SaveChanges();
                }
                catch (Exception ex)
                {
                    var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .Select(x => new { x.Key, x.Value.Errors })
                    .ToArray();
                    return Json(new { success = "false" ,errors });
                }
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = id > 0 ? "تعديل تسجيل الاجازات" : "اضافة تسجيل الاجازات",
                    EnAction = "AddEdit",
                    ControllerName = "EmployeeVacationsRegistration",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    CodeOrDocNo = vacationsRegistration.DocumentNumber,
                    SelectedItem = id,
                });
                return Json(new { success = "true" });
            }
            return Json(new { success = "false" });
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
            var lastObj = db.EmployeeVacationsRegistrations.Where(a => a.IsActive == true && a.IsDeleted == false && a.DepartmentId == id).OrderByDescending(a => a.Id).FirstOrDefault();
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
                    lastObj = db.EmployeeVacationsRegistrations.Where(a => a.IsActive == true && a.IsDeleted == false && a.DepartmentId == id && a.Date.Value.Month == VoucherDate.Value.Month && a.Date.Value.Year == VoucherDate.Value.Year).OrderByDescending(a => a.Id).FirstOrDefault();
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
                    lastObj = db.EmployeeVacationsRegistrations.Where(a => a.IsActive == true && a.IsDeleted == false && a.DepartmentId == id && a.Date.Value.Year == VoucherDate.Value.Year).OrderByDescending(a => a.Id).FirstOrDefault();

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
        [HttpPost, ActionName("Delete")]
        public ActionResult Delete(int id)
        {
            try
            {
                EmployeeVacationsRegistration vacationsRegistration = db.EmployeeVacationsRegistrations.Find(id);
                vacationsRegistration.IsDeleted = true;
                vacationsRegistration.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
               
                Random random = new Random();
                const string chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
                var Code = new string(Enumerable.Repeat(chars, 6).Select(s => s[random.Next(s.Length)]).ToArray());
                vacationsRegistration.DocumentNumber = Code;
                db.Entry(vacationsRegistration).State = EntityState.Modified;

                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "حذف تسجيل الاجازات",
                    EnAction = "AddEdit",
                    ControllerName = "EmployeeVacationsRegistration",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id
                });
                ////-------------------- Notification-------------------------////
                Notification.GetNotification("EmployeeVacationsRegistration", "Delete", "Delete", id, null, "تسجيل الاجازات");
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