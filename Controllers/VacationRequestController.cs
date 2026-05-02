using MyERP.Models;
using MyERP.Repository;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;

namespace MyERP.Controllers
{
    public class VacationRequestController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        // GET: VacationRequest
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
                ArAction = "فتح طلبات الإجازة",
                EnAction = "Index",
                ControllerName = "VacationRequest",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            ////-------------------- Notification-------------------------////
            Notification.GetNotification("VacationRequest", "View", "Index", null, null, "طلبات الإجازة");
            //////////-----------------------------------------------------------------------
            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;

            IQueryable<VacationRequest> vacationRequests;

            if (string.IsNullOrEmpty(searchWord))
            {
                if (userId == 1)
                {
                    vacationRequests = db.VacationRequests.Where(s => s.IsDeleted == false&&s.IsActive==true && (departmentId == 0 || s.DepartmentId == departmentId)).OrderByDescending(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                    ViewBag.Count = db.VacationRequests.Where(s => s.IsDeleted == false && s.IsActive == true && (departmentId == 0 || s.DepartmentId == departmentId)).Count();
                }
                else
                {
                    vacationRequests = db.VacationRequests.Where(s => s.IsDeleted == false && s.IsActive == true && (db.UserDepartments.Where(a => a.UserId == userId).Select(a => a.DepartmentId).Contains(s.DepartmentId)) && (departmentId == 0 || s.DepartmentId == departmentId)).OrderByDescending(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                    ViewBag.Count = db.VacationRequests.Where(s => s.IsDeleted == false&&s.IsActive==true&& (db.UserDepartments.Where(a => a.UserId == userId).Select(a => a.DepartmentId).Contains(s.DepartmentId)) && (departmentId == 0 || s.DepartmentId == departmentId)).Count();
                }
            }
            else
            {
                if (userId == 1)
                {
                    vacationRequests = db.VacationRequests.Where(s => s.IsDeleted == false && s.IsActive == true && (departmentId == 0 || s.DepartmentId == departmentId) && (s.DocumentNumber.Contains(searchWord) || s.Employee.ArName.Contains(searchWord) || s.Date.ToString().Contains(searchWord) || s.Notes.Contains(searchWord))).OrderBy(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                    ViewBag.Count = db.VacationRequests.Where(s => s.IsDeleted == false&&s.IsActive==true && (departmentId == 0 || s.DepartmentId == departmentId) && (s.DocumentNumber.Contains(searchWord) || s.Employee.ArName.Contains(searchWord) || s.Date.ToString().Contains(searchWord) || s.Notes.Contains(searchWord))).Count();
                }else
                {
                    vacationRequests = db.VacationRequests.Where(s => s.IsDeleted == false&&s.IsActive==true&& (db.UserDepartments.Where(a => a.UserId == userId).Select(a => a.DepartmentId).Contains(s.DepartmentId)) && (departmentId == 0 || s.DepartmentId == departmentId) && (s.DocumentNumber.Contains(searchWord) || s.Employee.ArName.Contains(searchWord) || s.Date.ToString().Contains(searchWord) || s.Notes.Contains(searchWord))).OrderBy(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                    ViewBag.Count = db.VacationRequests.Where(s => s.IsDeleted == false&&s.IsActive==true&& (db.UserDepartments.Where(a => a.UserId == userId).Select(a => a.DepartmentId).Contains(s.DepartmentId)) && (departmentId == 0 || s.DepartmentId == departmentId) && (s.DocumentNumber.Contains(searchWord) || s.Employee.ArName.Contains(searchWord) || s.Date.ToString().Contains(searchWord) || s.Notes.Contains(searchWord))).Count();
                }
            }
            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            return View(vacationRequests.ToList());
        }

        // GET: VacationRequest/Edit/5
        public async Task<ActionResult> AddEdit(int? id)
        {
            SystemSetting systemSetting = await db.SystemSettings.AnyAsync() ? await db.SystemSettings.FirstOrDefaultAsync() : new SystemSetting();
            UserRepository userRepository = new UserRepository(db);
            DepartmentRepository departmentRepository = new DepartmentRepository(db);

            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            if (id == null)
            {
                ViewBag.DepartmentId = new SelectList(departmentRepository.UserDepartments(userId), "Id", "ArName", systemSetting.DefaultDepartmentId);

                ViewBag.EmployeeId = new SelectList(db.Employees.Where(a => (a.IsActive == true && a.IsDeleted == false)).Select(b => new
                {
                    b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");

                ViewBag.JobId = new SelectList(db.Jobs.Where(a => a.IsActive && !a.IsDeleted).Select(b => new
                {
                    b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");

                ViewBag.LocationId = new SelectList(db.Locations.Where(a => a.IsActive && !a.IsDeleted).Select(b => new
                {
                    b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");

                ViewBag.HrDepartmentId = new SelectList(db.HrDepartments.Where(a => a.IsActive && !a.IsDeleted).Select(b => new
                {
                    b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");

                ViewBag.DirectManagerId = new SelectList(db.Employees.Where(a => a.IsActive && !a.IsDeleted).Select(b => new
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

                return View();
            }

            VacationRequest vacationRequest = await db.VacationRequests.FindAsync(id);
            if (vacationRequest == null)
                return HttpNotFound();

            int sysPageId = QueryHelper.SourcePageId("VacationRequest");


            ViewBag.Next = QueryHelper.Next((int)id, "VacationRequest");
            ViewBag.Previous = QueryHelper.Previous((int)id, "VacationRequest");
            ViewBag.Last = QueryHelper.GetLast("VacationRequest");
            ViewBag.First = QueryHelper.GetFirst("VacationRequest");

            ViewBag.DepartmentId = new SelectList(departmentRepository.UserDepartments(userId), "Id", "ArName", vacationRequest.DepartmentId);

            ViewBag.EmployeeId = new SelectList(db.Employees.Where(a => (a.IsActive == true && a.IsDeleted == false)).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", vacationRequest.EmployeeId);

            ViewBag.JobId = new SelectList(db.Jobs.Where(a => a.IsActive && !a.IsDeleted).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName");

            ViewBag.LocationId = new SelectList(db.Locations.Where(a => a.IsActive && !a.IsDeleted).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName");

            ViewBag.HrDepartmentId = new SelectList(db.HrDepartments.Where(a => a.IsActive && !a.IsDeleted).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName");

            ViewBag.DirectManagerId = new SelectList(db.Employees.Where(a => a.IsActive && !a.IsDeleted).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName");


            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح تفاصيل طلب إجازة",
                EnAction = "AddEdit",
                ControllerName = "VacationRequest",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = vacationRequest.Id,
                CodeOrDocNo = vacationRequest.DocumentNumber
            });


            try
            {
                ViewBag.Date = vacationRequest.Date.Value.ToString("yyyy-MM-ddTHH:mm");
                ViewBag.VacationStartDate = vacationRequest.VacationStartDate.Value.ToString("yyyy-MM-ddTHH:mm");
                ViewBag.VacationEndDate = vacationRequest.VacationEndDate.Value.ToString("yyyy-MM-ddTHH:mm");
                ViewBag.ReturnToWorkDate = vacationRequest.ReturnToWorkDate.Value.ToString("yyyy-MM-ddTHH:mm");
                //ViewBag.Date = vacationRequest.VacationStartDate.Value.ToString("yyyy-MM-ddTHH:mm");
                //ViewBag.Date = vacationRequest.VacationEndDate.Value.ToString("yyyy-MM-ddTHH:mm");
                //ViewBag.Date = vacationRequest.ReturnToWorkDate.Value.ToString("yyyy-MM-ddTHH:mm");

            }
            catch (Exception)
            {
            }

            return View(vacationRequest);
        }

        [HttpPost]
        //[ValidateAntiForgeryToken]
        public ActionResult AddEdit(VacationRequest vacationRequest)
        {
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            DepartmentRepository departmentRepository = new DepartmentRepository(db);

            if (ModelState.IsValid)
            {
                var id = vacationRequest.Id;
                vacationRequest.IsDeleted = false;
                if (vacationRequest.IsAcceptedByManager == null)
                {
                    vacationRequest.IsAcceptedByManager = false;
                }

                if (vacationRequest.Id > 0)
                {
                    vacationRequest.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                    // use another object to prevent entity error
                    var old = db.VacationRequests.Find(id);

                    old.DocumentNumber = vacationRequest.DocumentNumber;
                    old.DepartmentId = vacationRequest.DepartmentId;
                    old.Date = vacationRequest.Date;
                    old.EmployeeId = vacationRequest.EmployeeId;

                    old.IsPaid = vacationRequest.IsPaid;
                    old.IsAcceptedByManager = vacationRequest.IsAcceptedByManager;
                    old.VacationTypeId = vacationRequest.VacationTypeId;
                    old.AnotherVacationType = vacationRequest.AnotherVacationType;

                    old.Visa = vacationRequest.Visa;
                    old.VisaCosts = vacationRequest.VisaCosts;
                    old.OnEmployeeOrCompany = vacationRequest.OnEmployeeOrCompany;
                    old.ForEmployeeOrFamily = vacationRequest.ForEmployeeOrFamily;
                    old.RoundTripOrOneWay = vacationRequest.RoundTripOrOneWay;

                    old.VacationStartDate = vacationRequest.VacationStartDate;
                    old.VacationEndDate = vacationRequest.VacationEndDate;
                    old.ReturnToWorkDate = vacationRequest.ReturnToWorkDate;
                    old.NumberOfVacationDays = vacationRequest.NumberOfVacationDays;

                    old.VacationReason = vacationRequest.VacationReason;
                    old.VacationAddress = vacationRequest.VacationAddress;
                    old.PhoneNumber = vacationRequest.PhoneNumber;
                    old.AnotherCommunication = vacationRequest.AnotherCommunication;

                    old.Image = vacationRequest.Image;
                    old.Notes = vacationRequest.Notes;

                    db.Entry(old).State = EntityState.Modified;

                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("VacationRequest", "Edit", "AddEdit", vacationRequest.Id, null, "طلب إجازة");

                }
                else
                {

                    vacationRequest.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    vacationRequest.IsActive = true;
                    vacationRequest.DocumentNumber = new JavaScriptSerializer().Serialize(SetDocNum((int)vacationRequest.DepartmentId, vacationRequest.Date).Data).ToString().Trim('"');

                    db.VacationRequests.Add(vacationRequest);
                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("VacationRequest", "Add", "AddEdit", vacationRequest.Id, null, "طلب إجازة");
                }
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = vacationRequest.Id > 0 ? "تعديل طلب إجازة" : "اضافة طلب إجازة",
                    EnAction = "AddEdit",
                    ControllerName = "VacationRequest",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "Post",
                    SelectedItem = vacationRequest.Id,
                    CodeOrDocNo = vacationRequest.DocumentNumber
                });

                return Json(new { success = "true" });

            }


            ViewBag.DepartmentId = new SelectList(departmentRepository.UserDepartments(userId), "Id", "ArName", vacationRequest.DepartmentId);

            ViewBag.EmployeeId = new SelectList(db.Employees.Where(a => (a.IsActive == true && a.IsDeleted == false)).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName");

            ViewBag.JobId = new SelectList(db.Jobs.Where(a => a.IsActive && !a.IsDeleted).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName");

            ViewBag.LocationId = new SelectList(db.Locations.Where(a => a.IsActive && !a.IsDeleted).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName");

            ViewBag.HrDepartmentId = new SelectList(db.HrDepartments.Where(a => a.IsActive && !a.IsDeleted).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName");

            ViewBag.DirectManagerId = new SelectList(db.Employees.Where(a => a.IsActive && !a.IsDeleted).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName");

            return View(vacationRequest);
        }

        // POST: VacationRequest/Delete/5
        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            int userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            db.CarEntrance_Delete(id, userId);

            VacationRequest vacationRequest = db.VacationRequests.Find(id);
            vacationRequest.IsDeleted = true;
            vacationRequest.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            Random random = new Random();
            const string chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            var Code = new string(Enumerable.Repeat(chars, 6).Select(s => s[random.Next(s.Length)]).ToArray());
            vacationRequest.DocumentNumber = Code;
            db.Entry(vacationRequest).State = EntityState.Modified;

            db.SaveChanges();

            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "حذف طلب إجازة",
                EnAction = "Delete",
                ControllerName = "VacationRequest",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "POST",
                SelectedItem = id,
                CodeOrDocNo = vacationRequest.DocumentNumber
            });

            ////-------------------- Notification-------------------------////
            Notification.GetNotification("VacationRequest", "Delete", "Delete", id, null, "طلبات الإجازة");

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
            var lastObj = db.VacationRequests.Where(a => a.IsActive == true && a.IsDeleted == false && a.DepartmentId == id).OrderByDescending(a => a.Id).FirstOrDefault();
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
                    lastObj = db.VacationRequests.Where(a => a.IsActive == true && a.IsDeleted == false && a.DepartmentId == id && a.Date.Value.Month == VoucherDate.Value.Month && a.Date.Value.Year == VoucherDate.Value.Year).OrderByDescending(a => a.Id).FirstOrDefault();
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
                    lastObj = db.VacationRequests.Where(a => a.IsActive == true && a.IsDeleted == false && a.DepartmentId == id && a.Date.Value.Year == VoucherDate.Value.Year).OrderByDescending(a => a.Id).FirstOrDefault();

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
            //var docNo = QueryHelper.DocLastNum(id, "VacationRequest");
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