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
using MyERP.Repository;
using System.Web.Script.Serialization;

namespace MyERP.Controllers.HR
{
    public class EmployeeFollowUpController : Controller
    {
        private readonly MySoftERPEntity db = new MySoftERPEntity();

        // GET: EmployeeFollowUp
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
                ArAction = "فتح قائمة مباشرة موظف",
                EnAction = "Index",
                ControllerName = "EmployeeFollowUp",
                UserName = User.Identity.Name,
                UserId = userId,
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            ////-------------------- Notification-------------------------////
            Notification.GetNotification("EmployeeFollowUp", "View", "Index", null, null, "مباشرة موظف");

            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;
            IQueryable<EmployeeFollowUp> employeeFollowUps;
            if (string.IsNullOrEmpty(searchWord))
            {
                if (userId == 1)
                {
                    employeeFollowUps = db.EmployeeFollowUps.Where(c => c.IsDeleted == false && (departmentId == 0 || c.DepartmentId == departmentId)).OrderByDescending(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                    ViewBag.Count = db.EmployeeFollowUps.Where(c => c.IsDeleted == false && (departmentId == 0 || c.DepartmentId == departmentId)).Count();
                }
                else
                {
                    employeeFollowUps = db.EmployeeFollowUps.Where(c => c.IsDeleted == false && (db.UserDepartments.Where(a => a.UserId == userId).Select(a => a.DepartmentId).Contains(c.DepartmentId)) && (departmentId == 0 || c.DepartmentId == departmentId)).OrderByDescending(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                    ViewBag.Count = db.EmployeeFollowUps.Where(c => c.IsDeleted == false && (db.UserDepartments.Where(a => a.UserId == userId).Select(a => a.DepartmentId).Contains(c.DepartmentId)) && (departmentId == 0 || c.DepartmentId == departmentId)).Count();
                }
            }
            else
            {
                if (userId == 1)
                {
                    employeeFollowUps = db.EmployeeFollowUps.Where(s => s.IsDeleted == false && (departmentId == 0 || s.DepartmentId == departmentId) && (s.Employee.ArName.Contains(searchWord) || s.Employee.EnName.Contains(searchWord) || s.Department.ArName.Contains(searchWord) || s.Department.EnName.Contains(searchWord) || s.DocumentNumber.Contains(searchWord))).OrderByDescending(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                    ViewBag.Count = db.EmployeeFollowUps.Where(s => s.IsDeleted == false && (departmentId == 0 || s.DepartmentId == departmentId) && (s.Employee.ArName.Contains(searchWord) || s.Employee.EnName.Contains(searchWord) || s.Department.ArName.Contains(searchWord) || s.Department.EnName.Contains(searchWord) || s.DocumentNumber.Contains(searchWord))).Count();
                }
                else
                {
                    employeeFollowUps = db.EmployeeFollowUps.Where(s => s.IsDeleted == false && (db.UserDepartments.Where(a => a.UserId == userId).Select(a => a.DepartmentId).Contains(s.DepartmentId)) && (departmentId == 0 || s.DepartmentId == departmentId) && (s.Employee.ArName.Contains(searchWord) || s.Employee.EnName.Contains(searchWord) || s.Department.ArName.Contains(searchWord) || s.Department.EnName.Contains(searchWord) || s.DocumentNumber.Contains(searchWord))).OrderByDescending(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                    ViewBag.Count = db.EmployeeFollowUps.Where(s => s.IsDeleted == false && (db.UserDepartments.Where(a => a.UserId == userId).Select(a => a.DepartmentId).Contains(s.DepartmentId)) && (departmentId == 0 || s.DepartmentId == departmentId) && (s.Employee.ArName.Contains(searchWord) || s.Employee.EnName.Contains(searchWord) || s.Department.ArName.Contains(searchWord) || s.Department.EnName.Contains(searchWord) || s.DocumentNumber.Contains(searchWord))).Count();
                }
            }
            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            return View(employeeFollowUps.ToList());
        }

        public ActionResult AddEdit(int? id)
        {
            int userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            if (id == null)
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
                ViewBag.Date = cTime.ToString("yyyy-MM-ddTHH:mm");
                ViewBag.FollowUpDate = cTime.ToString("yyyy-MM-dd");
                ViewBag.EmployeeId = new SelectList(db.Employees.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                ViewBag.DepartmentId = new SelectList(db.Departments.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                return View();
            }
            EmployeeFollowUp employeeFollowUp = db.EmployeeFollowUps.Find(id);
            if (employeeFollowUp == null)
            {
                return HttpNotFound();
            }
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "اضافة او تعديل مباشرة موظف ",
                EnAction = "AddEdit",
                ControllerName = "EmployeeFollowUp",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = id
            });
            ViewBag.Next = QueryHelper.Next((int)id, "EmployeeFollowUp");
            ViewBag.Previous = QueryHelper.Previous((int)id, "EmployeeFollowUp");
            ViewBag.Last = QueryHelper.GetLast("EmployeeFollowUp");
            ViewBag.First = QueryHelper.GetFirst("EmployeeFollowUp");

            ViewBag.DepartmentId = new SelectList(db.Departments.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", employeeFollowUp.DepartmentId);
            ViewBag.Date = employeeFollowUp.Date.Value.ToString("yyyy-MM-ddTHH:mm");
            ViewBag.FollowUpDate = employeeFollowUp.FollowUpDate.Value.ToString("yyyy-MM-dd");
            if (employeeFollowUp.VacationStartDate != null)
            {
                ViewBag.VacationStartDate = employeeFollowUp.VacationStartDate.Value.ToString("yyyy-MM-dd");
            }
            if (employeeFollowUp.VacationEndDate != null)
            {
                ViewBag.VacationEndDate = employeeFollowUp.VacationEndDate.Value.ToString("yyyy-MM-dd");
            }
            ViewBag.EmployeeId = new SelectList(db.Employees.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", employeeFollowUp.EmployeeId);

            return View(employeeFollowUp);
        }

        [HttpPost]
        public ActionResult AddEdit(EmployeeFollowUp employeeFollowUp)
        {
            int userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            employeeFollowUp.UserId = userId;
            if (ModelState.IsValid)
            {
                var id = employeeFollowUp.Id;
                employeeFollowUp.IsDeleted = false;
                if (employeeFollowUp.Id > 0)
                {
                    db.Entry(employeeFollowUp).State = EntityState.Modified;
                }
                else
                {
                    employeeFollowUp.DocumentNumber = new JavaScriptSerializer().Serialize(SetDocNum((int)employeeFollowUp.DepartmentId, employeeFollowUp.Date).Data).ToString().Trim('"');
                    db.EmployeeFollowUps.Add(employeeFollowUp);

                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("EmployeeFollowUp", "Add", "AddEdit", id, null, "مباشرة موظف");
                }
                try
                {
                    db.SaveChanges();
                }
                catch (Exception ex)
                {
                    var mod = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .Select(x => new { x.Key, x.Value.Errors })
                    .ToArray();
                }
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = employeeFollowUp.Id > 0 ? "تعديل  مباشرة موظف " : "اضافة  مباشرة موظف",
                    EnAction = "AddEdit",
                    ControllerName = "EmployeeFollowUp",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = employeeFollowUp.Id > 0 ? employeeFollowUp.Id : db.EmployeeFollowUps.Max(i => i.Id),
                    CodeOrDocNo = employeeFollowUp.DocumentNumber
                });
                return Json(new { success = "true", id });
            }
            var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .Select(x => new { x.Key, x.Value.Errors })
                    .ToArray();
            return Json(new { success = "false", errors });
        }

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            EmployeeFollowUp employeeFollowUp = new EmployeeFollowUp() { Id = id, DocumentNumber = "" };
            db.EmployeeFollowUps.Attach(employeeFollowUp);
            employeeFollowUp.IsDeleted = true;
            Random random = new Random();
            const string chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            var Code = new string(Enumerable.Repeat(chars, 6).Select(s => s[random.Next(s.Length)]).ToArray());
            employeeFollowUp.DocumentNumber = Code;
            db.Entry(employeeFollowUp).Property(x => x.IsDeleted).IsModified = true;
            db.SaveChanges();
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "حذف مباشرة موظف",
                EnAction = "AddEdit",
                ControllerName = "EmployeeFollowUp",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "POST",
                SelectedItem = id,
                CodeOrDocNo = employeeFollowUp.DocumentNumber
            });
            Notification.GetNotification("EmployeeFollowUp", "Delete", "Delete", id, null, "مباشرة موظف");
            return Content("true");
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
            var lastObj = db.EmployeeFollowUps.Where(a => a.IsDeleted == false && a.DepartmentId == id).OrderByDescending(a => a.Id).FirstOrDefault();
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
                    lastObj = db.EmployeeFollowUps.Where(a => a.IsDeleted == false && a.DepartmentId == id && a.Date.Value.Month == VoucherDate.Value.Month && a.Date.Value.Year == VoucherDate.Value.Year).OrderByDescending(a => a.Id).FirstOrDefault();
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
                    lastObj = db.EmployeeFollowUps.Where(a => a.IsDeleted == false && a.DepartmentId == id && a.Date.Value.Year == VoucherDate.Value.Year).OrderByDescending(a => a.Id).FirstOrDefault();

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
            //var docNo = QueryHelper.DocLastNum(id, "EmployeeFollowUp");
            //double i = (docNo) + 1;
            //return Json(i, JsonRequestBehavior.AllowGet);
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