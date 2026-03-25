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
    public class LateAttendancePermissionController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();
        // GET: LateAttendancePermission
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
                ArAction = "فتح قائمة إذن حضور متأخر",
                EnAction = "Index",
                ControllerName = "LateAttendancePermission",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            Notification.GetNotification("LateAttendancePermission", "View", "Index", null, null, "إذن حضور متأخر");
            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;
            if (pageIndex > 1)
            {
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;
            }
            IQueryable<LateAttendancePermission> lateAttendancePermissions;
            if (string.IsNullOrEmpty(searchWord))
            {
                if (userId == 1)
                {
                    lateAttendancePermissions = db.LateAttendancePermissions.Where(a => a.IsDeleted == false && (departmentId == 0 || a.DepartmentId == departmentId)).OrderByDescending(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                    ViewBag.Count = db.LateAttendancePermissions.Where(a => a.IsDeleted == false && (departmentId == 0 || a.DepartmentId == departmentId)).Count();
                }
                else
                {
                    lateAttendancePermissions = db.LateAttendancePermissions.Where(a => a.IsDeleted == false&& (db.UserDepartments.Where(s => s.UserId == userId).Select(s => s.DepartmentId).Contains(a.DepartmentId)) && (departmentId == 0 || a.DepartmentId == departmentId)).OrderByDescending(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                    ViewBag.Count = db.LateAttendancePermissions.Where(a => a.IsDeleted == false&&(db.UserDepartments.Where(s => s.UserId == userId).Select(s => s.DepartmentId).Contains(a.DepartmentId)) && (departmentId == 0 || a.DepartmentId == departmentId)).Count();
                }
            }
            else
            {
                if (userId == 1)
                {
                    lateAttendancePermissions = db.LateAttendancePermissions.Where(a => a.IsDeleted == false && (departmentId == 0 || a.DepartmentId == departmentId) &&
  (a.Shift.ArName.Contains(searchWord) || a.Shift.ArName.Contains(searchWord) || a.Department.ArName.Contains(searchWord) || a.Employee.ArName.Contains(searchWord) || a.Employee.EnName.Contains(searchWord) || a.Shift.Code.Contains(searchWord) || a.Employee.Code.Contains(searchWord)))
      .OrderByDescending(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                    ViewBag.Count = db.LateAttendancePermissions.Where(a => a.IsDeleted == false && (departmentId == 0 || a.DepartmentId == departmentId) && (a.Shift.ArName.Contains(searchWord) || a.Shift.ArName.Contains(searchWord) || a.Department.ArName.Contains(searchWord) || a.Employee.ArName.Contains(searchWord) || a.Employee.EnName.Contains(searchWord) || a.Shift.Code.Contains(searchWord) || a.Employee.Code.Contains(searchWord))).Count();
                }
                else
                {
                    lateAttendancePermissions = db.LateAttendancePermissions.Where(a => a.IsDeleted == false&&(db.UserDepartments.Where(s => s.UserId == userId).Select(s => s.DepartmentId).Contains(a.DepartmentId)) && (departmentId == 0 || a.DepartmentId == departmentId) &&
              (a.Shift.ArName.Contains(searchWord) || a.Shift.ArName.Contains(searchWord) || a.Department.ArName.Contains(searchWord) || a.Employee.ArName.Contains(searchWord) || a.Employee.EnName.Contains(searchWord) || a.Shift.Code.Contains(searchWord) || a.Employee.Code.Contains(searchWord)))
                  .OrderByDescending(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                    ViewBag.Count = db.LateAttendancePermissions.Where(a => a.IsDeleted == false&&(db.UserDepartments.Where(s => s.UserId == userId).Select(s => s.DepartmentId).Contains(a.DepartmentId)) && (departmentId == 0 || a.DepartmentId == departmentId) && (a.Shift.ArName.Contains(searchWord) || a.Shift.ArName.Contains(searchWord) || a.Department.ArName.Contains(searchWord) || a.Employee.ArName.Contains(searchWord) || a.Employee.EnName.Contains(searchWord) || a.Shift.Code.Contains(searchWord) || a.Employee.Code.Contains(searchWord))).Count();
                }
            }
            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            return View(lateAttendancePermissions.ToList());
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
            LateAttendancePermission lateAttendancePermission = db.LateAttendancePermissions.Find(id);
            if (lateAttendancePermission == null)
            {
                return HttpNotFound();
            }
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "اضافة او تعديل إذن حضور متأخر ",
                EnAction = "AddEdit",
                ControllerName = "LateAttendancePermission",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                CodeOrDocNo = lateAttendancePermission.DocumentNumber,
                SelectedItem = id
            });

            ViewBag.Next = QueryHelper.Next((int)id, "LateAttendancePermission");
            ViewBag.Previous = QueryHelper.Previous((int)id, "LateAttendancePermission");
            ViewBag.Last = QueryHelper.GetLast("LateAttendancePermission");
            ViewBag.First = QueryHelper.GetFirst("LateAttendancePermission");
            // Departments
            if (userId == 1)
            {
                //DepartmentId
                ViewBag.DepartmentId = new SelectList(db.Departments.Where(d => d.IsActive == true && d.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName", lateAttendancePermission.DepartmentId);
            }
            else
            {
                //DepartmentId
                ViewBag.DepartmentId = new SelectList(db.UserDepartments.Where(d => d.UserId == userId).Select(b => new
                {
                    Id = b.DepartmentId,
                    ArName = b.Department.Code + " - " + b.Department.ArName
                }), "Id", "ArName", lateAttendancePermission.DepartmentId);
            }
            //EmployeeId
            ViewBag.EmployeeId = new SelectList(db.Employees.Where(d => d.IsActive == true && d.IsDeleted == false).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", lateAttendancePermission.EmployeeId);
            //ShiftId
            ViewBag.ShiftId = new SelectList(db.Shifts.Where(d => d.IsActive == true && d.IsDeleted == false).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", lateAttendancePermission.ShiftId);
            //Date
            ViewBag.Date = lateAttendancePermission.Date.Value.ToString("yyyy-MM-ddTHH:mm");
            //PermissionDate
            ViewBag.PermissionDate = lateAttendancePermission.PermissionDate.Value.ToString("yyyy-MM-ddTHH:mm");
            return View(lateAttendancePermission);

        }
        [HttpPost]
        public ActionResult AddEdit(LateAttendancePermission lateAttendancePermission)
        {
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            if (ModelState.IsValid)
            {
                var id = lateAttendancePermission.Id;
                lateAttendancePermission.IsDeleted = false;
                lateAttendancePermission.UserId = userId;
                if (lateAttendancePermission.Id > 0)
                {
                    // update 
                    db.Entry(lateAttendancePermission).State = EntityState.Modified;
                    Notification.GetNotification("LateAttendancePermission", "Edit", "AddEdit", lateAttendancePermission.Id, null, "إذن حضور متأخر");
                }
                else
                {
                    //insert 
                    lateAttendancePermission.DocumentNumber = new JavaScriptSerializer().Serialize(SetDocNum((int)lateAttendancePermission.DepartmentId, lateAttendancePermission.Date).Data).ToString().Trim('"');
                    db.LateAttendancePermissions.Add(lateAttendancePermission);
                    Notification.GetNotification("LateAttendancePermission", "Add", "AddEdit", lateAttendancePermission.Id, null, "إذن حضور متأخر");
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
                    return View(lateAttendancePermission);
                }
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = id > 0 ? "تعديل إذن حضور متأخر" : "اضافة إذن حضور متأخر",
                    EnAction = "AddEdit",
                    ControllerName = "LateAttendancePermission",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    CodeOrDocNo = lateAttendancePermission.DocumentNumber,
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
                }), "Id", "ArName", lateAttendancePermission.DepartmentId);
            }
            else
            {
                //DepartmentId
                ViewBag.DepartmentId = new SelectList(db.UserDepartments.Where(d => d.UserId == userId).Select(b => new
                {
                    Id = b.DepartmentId,
                    ArName = b.Department.Code + " - " + b.Department.ArName
                }), "Id", "ArName", lateAttendancePermission.DepartmentId);
            }
            //EmployeeId
            ViewBag.EmployeeId = new SelectList(db.Employees.Where(d => d.IsActive == true && d.IsDeleted == false).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", lateAttendancePermission.EmployeeId);
            //ShiftId
            ViewBag.ShiftId = new SelectList(db.Shifts.Where(d => d.IsActive == true && d.IsDeleted == false).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", lateAttendancePermission.ShiftId);
            return View(lateAttendancePermission);
        }
        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                LateAttendancePermission lateAttendancePermission = db.LateAttendancePermissions.Find(id);
                lateAttendancePermission.IsDeleted = true;
                lateAttendancePermission.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                Random random = new Random();
                const string chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
                var Code = new string(Enumerable.Repeat(chars, 6).Select(s => s[random.Next(s.Length)]).ToArray());
                lateAttendancePermission.DocumentNumber = Code;
                db.Entry(lateAttendancePermission).State = EntityState.Modified;
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "حذف إذن حضور متأخر",
                    EnAction = "AddEdit",
                    ControllerName = "LateAttendancePermission",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                });
                Notification.GetNotification("LateAttendancePermission", "Delete", "Delete", id, null, "إذن حضور متأخر");
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
            var lastObj = db.LateAttendancePermissions.Where(a => a.IsDeleted == false && a.DepartmentId == id).OrderByDescending(a => a.Id).FirstOrDefault();
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
                    lastObj = db.LateAttendancePermissions.Where(a => a.IsDeleted == false && a.DepartmentId == id && a.Date.Value.Month == VoucherDate.Value.Month && a.Date.Value.Year == VoucherDate.Value.Year).OrderByDescending(a => a.Id).FirstOrDefault();
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
                    lastObj = db.LateAttendancePermissions.Where(a => a.IsDeleted == false && a.DepartmentId == id && a.Date.Value.Year == VoucherDate.Value.Year).OrderByDescending(a => a.Id).FirstOrDefault();

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
            //var docNo = QueryHelper.DocLastNum(id, "LateAttendancePermission");
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