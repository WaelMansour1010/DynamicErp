using MyERP.Models;
using MyERP.Repository;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core.Objects;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace MyERP.Controllers.HR
{
    public class EmployeeVacationOpeningBalanceController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        // GET: EmployeeVacationOpeningBalance
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
                ArAction = "فتح الارصدة الافتتاحية الموظفين",
                EnAction = "Index",
                ControllerName = "EmployeeVacationOpeningBalance",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            ////-------------------- Notification-------------------------////
            Notification.GetNotification("EmployeeVacationOpeningBalance", "View", "Index", null, null, "الارصدة الافتتاحية لإجازات الموظفين");
            //////////-----------------------------------------------------------------------
            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;

            IQueryable<EmployeeVacationOpeningBalance> employeeVacationOpeningBalance;

            if (string.IsNullOrEmpty(searchWord))
            {
                if (userId == 1)
                {
                    employeeVacationOpeningBalance = db.EmployeeVacationOpeningBalances.Where(s => s.IsDeleted == false && s.IsActive == true && (departmentId == 0 || s.DepartmentId == departmentId)).OrderByDescending(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                    ViewBag.Count = db.EmployeeVacationOpeningBalances.Where(s => s.IsDeleted == false && s.IsActive == true && (departmentId == 0 || s.DepartmentId == departmentId)).Count();
                }
                else
                {
                    employeeVacationOpeningBalance = db.EmployeeVacationOpeningBalances.Where(s => s.IsDeleted == false && s.IsActive == true && (db.UserDepartments.Where(a => a.UserId == userId).Select(a => a.DepartmentId).Contains(s.DepartmentId)) && (departmentId == 0 || s.DepartmentId == departmentId)).OrderByDescending(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                    ViewBag.Count = db.EmployeeVacationOpeningBalances.Where(s => s.IsDeleted == false && s.IsActive == true&& (db.UserDepartments.Where(a => a.UserId == userId).Select(a => a.DepartmentId).Contains(s.DepartmentId)) && (departmentId == 0 || s.DepartmentId == departmentId)).Count();

                }
            }
            else
            {
                if (userId == 1)
                {
                    employeeVacationOpeningBalance = db.EmployeeVacationOpeningBalances.Where(s => s.IsDeleted == false && s.IsActive == true && (departmentId == 0 || s.DepartmentId == departmentId) && (s.DocumentNumber.Contains(searchWord) || s.Department.ArName.Contains(searchWord) || s.Date.ToString().Contains(searchWord) || s.Notes.Contains(searchWord))).OrderBy(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                    ViewBag.Count = db.EmployeeVacationOpeningBalances.Where(s => s.IsDeleted == false && s.IsActive == true && (departmentId == 0 || s.DepartmentId == departmentId) && (s.DocumentNumber.Contains(searchWord) || s.Department.ArName.Contains(searchWord)) || s.Date.ToString().Contains(searchWord) || s.Notes.Contains(searchWord)).Count();
                }
                else
                {
                    employeeVacationOpeningBalance = db.EmployeeVacationOpeningBalances.Where(s => s.IsDeleted == false && s.IsActive == true && (db.UserDepartments.Where(a => a.UserId == userId).Select(a => a.DepartmentId).Contains(s.DepartmentId)) && (departmentId == 0 || s.DepartmentId == departmentId) && (s.DocumentNumber.Contains(searchWord) || s.Department.ArName.Contains(searchWord) || s.Date.ToString().Contains(searchWord) || s.Notes.Contains(searchWord))).OrderBy(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                    ViewBag.Count = db.EmployeeVacationOpeningBalances.Where(s => s.IsDeleted == false && s.IsActive == true&& (db.UserDepartments.Where(a => a.UserId == userId).Select(a => a.DepartmentId).Contains(s.DepartmentId)) && (departmentId == 0 || s.DepartmentId == departmentId) && (s.DocumentNumber.Contains(searchWord) || s.Department.ArName.Contains(searchWord)) || s.Date.ToString().Contains(searchWord) || s.Notes.Contains(searchWord)).Count();
                }
            }
            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            return View(employeeVacationOpeningBalance.ToList());
        }

        public async Task<ActionResult> AddEdit(int? id)
        {
            SystemSetting systemSetting = await db.SystemSettings.AnyAsync() ? await db.SystemSettings.FirstOrDefaultAsync() : new SystemSetting();
            
            UserRepository userRepository = new UserRepository(db);
            DepartmentRepository departmentRepository = new DepartmentRepository(db);

            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);


            if (id == null)
            {

                ViewBag.DepartmentId = new SelectList(departmentRepository.UserDepartments(userId), "Id", "ArName", systemSetting.DefaultDepartmentId);
                ViewBag.EmployeeDepartmentId = new SelectList(departmentRepository.UserDepartments(userId), "Id", "ArName", systemSetting.DefaultDepartmentId);

                ViewBag.AdministrativeDepartmentId = new SelectList(db.AdministrativeDepartments.Where(a => (a.IsActive == true && a.IsDeleted == false)).Select(b => new
                {
                    b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");

                ViewBag.EmployeeId = new SelectList(db.Employees.Where(a => (a.IsActive == true && a.IsDeleted == false)).Select(b => new
                {
                    b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");

                ViewBag.InputMethods = new SelectList( 
                new List<SelectListItem> 
                {
                   new SelectListItem{Text="موظف",Value = "1"},
                   new SelectListItem{Text="فرع",Value = "2"},
                   new SelectListItem{Text="إدارة",Value = "3"},
                   new SelectListItem{Text="كل الموظفين",Value = "4"},
                }, "Value", "Text");


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

            EmployeeVacationOpeningBalance employeeVacationOpeningBalance = await db.EmployeeVacationOpeningBalances.FindAsync(id);
            if (employeeVacationOpeningBalance == null)
                return HttpNotFound();

         

            ViewBag.Next = QueryHelper.Next((int)id, "EmployeeVacationOpeningBalance");
            ViewBag.Previous = QueryHelper.Previous((int)id, "EmployeeVacationOpeningBalance");
            ViewBag.Last = QueryHelper.GetLast("EmployeeVacationOpeningBalance");
            ViewBag.First = QueryHelper.GetFirst("EmployeeVacationOpeningBalance");

            int sysPageId = QueryHelper.SourcePageId("EmployeeVacationOpeningBalance");
            //JournalEntry journal = db.JournalEntries.FirstOrDefault(j => j.SourceId == id && j.SourcePageId == sysPageId);
            //ViewBag.Journal = journal;
            //var SourceDocumentNumber = db.EmployeeVacationOpeningBalances.Where(a => a.Id == employeeVacationOpeningBalance.SelectedId).Select(a => a.DocumentNumber).FirstOrDefault();
            //ViewBag.SourceDocumentNumber = SourceDocumentNumber;



            ViewBag.DepartmentId = new SelectList(departmentRepository.UserDepartments(userId), "Id", "ArName", employeeVacationOpeningBalance.DepartmentId);

            ViewBag.EmployeeDepartmentId = new SelectList(departmentRepository.UserDepartments(userId), "Id", "ArName", systemSetting.DefaultDepartmentId);

            ViewBag.AdministrativeDepartmentId = new SelectList(db.AdministrativeDepartments.Where(a => (a.IsActive == true && a.IsDeleted == false)).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName");

            ViewBag.EmployeeId = new SelectList(db.Employees.Where(a => (a.IsActive == true && a.IsDeleted == false)).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName");


            ViewBag.InputMethods = new  SelectList( 
                new List<SelectListItem>
                {
                   new SelectListItem{Text="موظف",Value = "1"},
                   new SelectListItem{Text="فرع",Value = "2"},
                   new SelectListItem{Text="إدارة",Value = "3"},
                   new SelectListItem{Text="كل الموظفين",Value = "4"},
                },"Value", "Text", employeeVacationOpeningBalance.InputMethodId);
            
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح ارصدة إفتتاحية للموظفين",
                EnAction = "AddEdit",
                ControllerName = "EmployeeVacationOpeningBalance",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = employeeVacationOpeningBalance.Id,
                CodeOrDocNo = employeeVacationOpeningBalance.DocumentNumber
            });


            try
            {
                ViewBag.Date = employeeVacationOpeningBalance.Date.Value.ToString("yyyy-MM-ddTHH:mm");
            }
            catch (Exception)
            {
            }

            return View(employeeVacationOpeningBalance);
        }


        [HttpPost]
        public JsonResult AddEdit(EmployeeVacationOpeningBalance employeeVacationOpeningBalance)
        {
            int userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            employeeVacationOpeningBalance.UserId = userId;


            if (ModelState.IsValid)
            {
                var id = employeeVacationOpeningBalance.Id;
                employeeVacationOpeningBalance.IsDeleted = false;
                int SystemPageId = db.SystemPages.Where(s => s.TableName == "EmployeeVacationOpeningBalance").Select(s => s.Id).FirstOrDefault();

                if (id > 0)
                {
                    if (db.EmployeeVacationOpeningBalances.Find(employeeVacationOpeningBalance.Id).IsPosted == true)
                    {
                        return Json("false");
                    }
                    MyXML.xPathName = "Details";
                    var EmployeeVacationOpeningBalanceIdDetails = MyXML.GetXML(employeeVacationOpeningBalance.EmployeeVacationOpeningBalanceDetails.Select(x => new { x.MainDocId, x.IsDeleted, SystemPageId, x.EmployeeId, x.StartDate, x.EndDate, x.VacationBalance, x.VacationWithoutSalary, x.Absence, x.StartDateHijri, x.EndDateHijri, SelectedId = employeeVacationOpeningBalance.Id }));

                    //procedure response for update

                    db.EmployeeVacationOB_Update(id,
                        employeeVacationOpeningBalance.DocumentNumber,
                        employeeVacationOpeningBalance.DepartmentId,
                        employeeVacationOpeningBalance.Date,
                        userId,
                        true,
                        false,
                        employeeVacationOpeningBalance.Notes,
                        employeeVacationOpeningBalance.Image,
                        SystemPageId,
                        employeeVacationOpeningBalance.Id,
                        employeeVacationOpeningBalance.InputMethodId,
                        EmployeeVacationOpeningBalanceIdDetails,employeeVacationOpeningBalance.IsPosted);

                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("EmployeeVacationOpeningBalance", "Edit", "AddEdit", id, null, "الارصدة الافتتاحية لإجازات الموظفين");

                }
                else
                {
                    employeeVacationOpeningBalance.IsActive = true;

                    if (employeeVacationOpeningBalance.SelectedId != null)
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
                        employeeVacationOpeningBalance.Date = cTime;
                    }

                    MyXML.xPathName = "Details";
                    var EmployeeVacationOpeningBalanceIdDetails = MyXML.GetXML(employeeVacationOpeningBalance.EmployeeVacationOpeningBalanceDetails.Select(x => new { x.MainDocId, x.IsDeleted, SystemPageId, x.EmployeeId, x.StartDate, x.EndDate, x.VacationBalance, x.VacationWithoutSalary, x.Absence, x.StartDateHijri, x.EndDateHijri, SelectedId = employeeVacationOpeningBalance.Id }));


                    var idResult = new ObjectParameter("Id", typeof(Int32));
                    //procedure response for insert
                    db.EmployeeVacationOB_Insert(idResult,
                        employeeVacationOpeningBalance.DepartmentId,
                        employeeVacationOpeningBalance.Date,
                        userId,
                        true,
                        false,
                        employeeVacationOpeningBalance.Notes,
                        employeeVacationOpeningBalance.Image,
                        SystemPageId,
                        employeeVacationOpeningBalance.Id,
                        employeeVacationOpeningBalance.InputMethodId,
                        EmployeeVacationOpeningBalanceIdDetails,employeeVacationOpeningBalance.IsPosted);

                    id = (int)idResult.Value;

                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("EmployeeVacationOpeningBalance", "Add", "AddEdit", id, null, "الارصدة الافتتاحية لإجازات الموظفين");

                }

                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = employeeVacationOpeningBalance.Id > 0 ? "تعديل ارصدة إفتتاحية للموظفين" : "إضافة ارصدة إفتتاحية للموظفين",
                    EnAction = "AddEdit",
                    ControllerName = "EmployeeVacationOpeningBalance",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = employeeVacationOpeningBalance.Id > 0 ? employeeVacationOpeningBalance.Id : db.EmployeeVacationOpeningBalances.Max(i => i.Id),
                    CodeOrDocNo = employeeVacationOpeningBalance.DocumentNumber
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
            int userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            
            EmployeeVacationOpeningBalance employeeVacationOpeningBalance = db.EmployeeVacationOpeningBalances.Find(id);
            if (employeeVacationOpeningBalance.IsPosted == true)
            {
                return Content("false");
            }
            List<EmployeeVacationOpeningBalanceDetail> VacationOpeningBalanceDetails = db.EmployeeVacationOpeningBalanceDetails.Where(a => a.MainDocId == id).ToList();
            employeeVacationOpeningBalance.IsDeleted = true;
            employeeVacationOpeningBalance.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

            for (int i = 0; i < VacationOpeningBalanceDetails.Count(); i++)
            {
                VacationOpeningBalanceDetails[i].IsDeleted = true;
            }
            Random random = new Random();
            const string chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            var Code = new string(Enumerable.Repeat(chars, 6).Select(s => s[random.Next(s.Length)]).ToArray());
            employeeVacationOpeningBalance.DocumentNumber = Code;
            db.Entry(employeeVacationOpeningBalance).State = EntityState.Modified;

            db.SaveChanges();

            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "حذف مستند ارصدة إفتتاحية للموظفين",
                EnAction = "Delete",
                ControllerName = "EmployeeVacationOpeningBalance",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "POST",
                SelectedItem = id,
                //CodeOrDocNo = employeeVacationOpeningBalance.DocumentNumber
            });

            ////-------------------- Notification-------------------------////
            Notification.GetNotification("EmployeeVacationOpeningBalance", "Delete", "Delete", id, null, "الارصدة الافتتاحية لإجازات الموظفين");

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
            var lastObj = db.EmployeeVacationOpeningBalances.Where(a => a.IsActive == true && a.IsDeleted == false && a.DepartmentId == id).OrderByDescending(a => a.Id).FirstOrDefault();
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
                    lastObj = db.EmployeeVacationOpeningBalances.Where(a => a.IsActive == true && a.IsDeleted == false && a.DepartmentId == id && a.Date.Value.Month == VoucherDate.Value.Month && a.Date.Value.Year == VoucherDate.Value.Year).OrderByDescending(a => a.Id).FirstOrDefault();
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
                    lastObj = db.EmployeeVacationOpeningBalances.Where(a => a.IsActive == true && a.IsDeleted == false && a.DepartmentId == id && a.Date.Value.Year == VoucherDate.Value.Year).OrderByDescending(a => a.Id).FirstOrDefault();

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
            //var docNo = QueryHelper.DocLastNum(id, "EmployeeVacationOpeningBalance");
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