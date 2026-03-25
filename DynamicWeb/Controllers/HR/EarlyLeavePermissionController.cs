using DevExpress.Data.WcfLinq.Helpers;
using MyERP.Models;
using MyERP.Repository;
using System;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Web.Mvc;
using System.Web.Script.Serialization;

namespace MyERP.Controllers.HR
{
    public class EarlyLeavePermissionController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();
        // GET: EarlyLeavePermission
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
                ArAction = "فتح قائمة إذن إنصراف مبكر",
                EnAction = "Index",
                ControllerName = "EarlyLeavePermission",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            Notification.GetNotification("EarlyLeavePermission", "View", "Index", null, null, "إذن إنصراف مبكر");
            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;
            if (pageIndex > 1)
            {
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;
            }
            IQueryable<EarlyLeavePermission> earlyLeavePermissions;
            if (string.IsNullOrEmpty(searchWord))
            {
                if (userId == 1)
                {
                    earlyLeavePermissions = db.EarlyLeavePermissions.Where(a => a.IsDeleted == false && (departmentId == 0 || a.DepartmentId == departmentId)).OrderByDescending(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                    ViewBag.Count = db.EarlyLeavePermissions.Where(a => a.IsDeleted == false && (departmentId == 0 || a.DepartmentId == departmentId)).Count();
                }
                else
                {
                    earlyLeavePermissions = db.EarlyLeavePermissions.Where(a => a.IsDeleted == false && (db.UserDepartments.Where(s => s.UserId == userId).Select(s => s.DepartmentId).Contains(a.DepartmentId)) && (departmentId == 0 || a.DepartmentId == departmentId)).OrderByDescending(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                    ViewBag.Count = db.EarlyLeavePermissions.Where(a => a.IsDeleted == false && (db.UserDepartments.Where(s => s.UserId == userId).Select(s => s.DepartmentId).Contains(a.DepartmentId)) && (departmentId == 0 || a.DepartmentId == departmentId)).Count();
                }
            }
            else
            {
                if (userId == 1)
                {
                    earlyLeavePermissions = db.EarlyLeavePermissions.Where(a => a.IsDeleted == false && (departmentId == 0 || a.DepartmentId == departmentId) &&
                (a.Shift.ArName.Contains(searchWord) || a.Shift.ArName.Contains(searchWord) || a.Department.ArName.Contains(searchWord) || a.Employee.ArName.Contains(searchWord) || a.Employee.EnName.Contains(searchWord) || a.Shift.Code.Contains(searchWord) || a.Employee.Code.Contains(searchWord)))
                    .OrderByDescending(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                    ViewBag.Count = db.EarlyLeavePermissions.Where(a => a.IsDeleted == false && (departmentId == 0 || a.DepartmentId == departmentId) && (a.Shift.ArName.Contains(searchWord) || a.Shift.ArName.Contains(searchWord) || a.Department.ArName.Contains(searchWord) || a.Employee.ArName.Contains(searchWord) || a.Employee.EnName.Contains(searchWord) || a.Shift.Code.Contains(searchWord) || a.Employee.Code.Contains(searchWord))).Count();
                }
                else
                {
                    earlyLeavePermissions = db.EarlyLeavePermissions.Where(a => a.IsDeleted == false && (db.UserDepartments.Where(s => s.UserId == userId).Select(s => s.DepartmentId).Contains(a.DepartmentId)) && (departmentId == 0 || a.DepartmentId == departmentId) &&
                (a.Shift.ArName.Contains(searchWord) || a.Shift.ArName.Contains(searchWord) || a.Department.ArName.Contains(searchWord) || a.Employee.ArName.Contains(searchWord) || a.Employee.EnName.Contains(searchWord) || a.Shift.Code.Contains(searchWord) || a.Employee.Code.Contains(searchWord)))
                    .OrderByDescending(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                    ViewBag.Count = db.EarlyLeavePermissions.Where(a => a.IsDeleted == false && (db.UserDepartments.Where(s => s.UserId == userId).Select(s => s.DepartmentId).Contains(a.DepartmentId)) && (departmentId == 0 || a.DepartmentId == departmentId) && (a.Shift.ArName.Contains(searchWord) || a.Shift.ArName.Contains(searchWord) || a.Department.ArName.Contains(searchWord) || a.Employee.ArName.Contains(searchWord) || a.Employee.EnName.Contains(searchWord) || a.Shift.Code.Contains(searchWord) || a.Employee.Code.Contains(searchWord))).Count();
                }
            }
            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            return View(earlyLeavePermissions.ToList());
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
                //ShiftId
                ViewBag.ShiftId = new SelectList(db.Shifts.Where(d => d.IsActive == true && d.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                return View();
            }
            EarlyLeavePermission earlyLeavePermission = db.EarlyLeavePermissions.Find(id);
            if (earlyLeavePermission == null)
            {
                return HttpNotFound();
            }
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "اضافة او تعديل إذن إنصراف مبكر ",
                EnAction = "AddEdit",
                ControllerName = "EarlyLeavePermission",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                CodeOrDocNo = earlyLeavePermission.DocumentNumber,
                SelectedItem = id
            });
            ViewBag.Next = QueryHelper.Next((int)id, "EarlyLeavePermission");
            ViewBag.Previous = QueryHelper.Previous((int)id, "EarlyLeavePermission");
            ViewBag.Last = QueryHelper.GetLast("EarlyLeavePermission");
            ViewBag.First = QueryHelper.GetFirst("EarlyLeavePermission");
            // Departments
            if (userId == 1)
            {
                //DepartmentId
                ViewBag.DepartmentId = new SelectList(db.Departments.Where(d => d.IsActive == true && d.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName", earlyLeavePermission.DepartmentId);
            }
            else
            {
                //DepartmentId
                ViewBag.DepartmentId = new SelectList(db.UserDepartments.Where(d => d.UserId == userId).Select(b => new
                {
                    Id = b.DepartmentId,
                    ArName = b.Department.Code + " - " + b.Department.ArName
                }), "Id", "ArName", earlyLeavePermission.DepartmentId);
            }
            //EmployeeId
            ViewBag.EmployeeId = new SelectList(db.Employees.Where(d => d.IsActive == true && d.IsDeleted == false).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", earlyLeavePermission.EmployeeId);
            //ShiftId
            ViewBag.ShiftId = new SelectList(db.Shifts.Where(d => d.IsActive == true && d.IsDeleted == false).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", earlyLeavePermission.ShiftId);
            //Date
            ViewBag.Date = earlyLeavePermission.Date.Value.ToString("yyyy-MM-ddTHH:mm");
            //PermissionDate
            ViewBag.PermissionDate = earlyLeavePermission.PermissionDate.Value.ToString("yyyy-MM-ddTHH:mm");
            return View(earlyLeavePermission);
        }
        [HttpPost]
        public ActionResult AddEdit(EarlyLeavePermission earlyLeavePermission)
        {
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            earlyLeavePermission.IsDeleted = false;
            earlyLeavePermission.UserId = userId;
            if (ModelState.IsValid)
            {
                var id = earlyLeavePermission.Id;

                if (earlyLeavePermission.Id > 0)
                {
                    // update 
                    db.Entry(earlyLeavePermission).State = EntityState.Modified;
                    Notification.GetNotification("EarlyLeavePermission", "Edit", "AddEdit", earlyLeavePermission.Id, null, "إذن إنصراف مبكر");
                }
                else
                {
                    //insert 
                    earlyLeavePermission.DocumentNumber = new JavaScriptSerializer().Serialize(SetDocNum((int)earlyLeavePermission.DepartmentId, earlyLeavePermission.Date).Data).ToString().Trim('"');
                    db.EarlyLeavePermissions.Add(earlyLeavePermission);
                    Notification.GetNotification("EarlyLeavePermission", "Add", "AddEdit", earlyLeavePermission.Id, null, "إذن إنصراف مبكر");
                }
                try
                {
                    db.SaveChanges();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.InnerException.InnerException.Message);
                    var mod = ModelState.First(c => c.Key == "Code");  // this
                    mod.Value.Errors.Add("هذا الكود موجود من قبل");
                    return View(earlyLeavePermission);
                }
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = id > 0 ? "تعديل إذن إنصراف مبكر" : "اضافة إذن إنصراف مبكر",
                    EnAction = "AddEdit",
                    ControllerName = "EarlyLeavePermission",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    CodeOrDocNo = earlyLeavePermission.DocumentNumber,
                    SelectedItem = id,
                });
                return Json(new { success = "true" });
            }
            if (userId == 1)
            {
                //DepartmentId
                ViewBag.DepartmentId = new SelectList(db.Departments.Where(d => d.IsActive == true && d.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName", earlyLeavePermission.DepartmentId);
            }
            else
            {
                //DepartmentId
                ViewBag.DepartmentId = new SelectList(db.UserDepartments.Where(d => d.UserId == userId).Select(b => new
                {
                    Id = b.DepartmentId,
                    ArName = b.Department.Code + " - " + b.Department.ArName
                }), "Id", "ArName", earlyLeavePermission.DepartmentId);
            }
            //EmployeeId
            ViewBag.EmployeeId = new SelectList(db.Employees.Where(d => d.IsActive == true && d.IsDeleted == false).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", earlyLeavePermission.EmployeeId);
            //ShiftId
            ViewBag.ShiftId = new SelectList(db.Shifts.Where(d => d.IsActive == true && d.IsDeleted == false).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", earlyLeavePermission.ShiftId);
            return View(earlyLeavePermission);
        }
        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                EarlyLeavePermission earlyLeavePermission = db.EarlyLeavePermissions.Find(id);
                earlyLeavePermission.IsDeleted = true;
                earlyLeavePermission.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                Random random = new Random();
                const string chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
                var Code = new string(Enumerable.Repeat(chars, 6).Select(s => s[random.Next(s.Length)]).ToArray());
                earlyLeavePermission.DocumentNumber = Code;
                db.Entry(earlyLeavePermission).State = EntityState.Modified;
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "حذف إذن إنصراف مبكر",
                    EnAction = "AddEdit",
                    ControllerName = "EarlyLeavePermission",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                });
                Notification.GetNotification("EarlyLeavePermission", "Delete", "Delete", id, null, "إذن إنصراف مبكر");
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
            var lastObj = db.EarlyLeavePermissions.Where(a => a.IsDeleted == false && a.DepartmentId == id).OrderByDescending(a => a.Id).FirstOrDefault();
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
                    lastObj = db.EarlyLeavePermissions.Where(a => a.IsDeleted == false && a.DepartmentId == id && a.Date.Value.Month == VoucherDate.Value.Month && a.Date.Value.Year == VoucherDate.Value.Year).OrderByDescending(a => a.Id).FirstOrDefault();
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
                    lastObj = db.EarlyLeavePermissions.Where(a => a.IsDeleted == false && a.DepartmentId == id && a.Date.Value.Year == VoucherDate.Value.Year).OrderByDescending(a => a.Id).FirstOrDefault();

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
            //var docNo = QueryHelper.DocLastNum(id, "EarlyLeavePermission");
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