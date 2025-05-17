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

namespace MyERP.Controllers
{
    public class EmpPreviousVacationController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        // GET: EmpPreviousVacation
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
                ArAction = "فتح قائمة تسجيل بيانات الاجازات السابقة",
                EnAction = "Index",
                ControllerName = "EmpPreviousVacation",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            ////-------------------- Notification-------------------------////
            Notification.GetNotification("EmpPreviousVacation", "View", "Index", null, null, "تسجيل بيانات الاجازات السابقة");

            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;

            IQueryable<EmpPreviousVacation> emps;

            if (string.IsNullOrEmpty(searchWord))
            {
                if (userId == 1)
                {
                    emps = db.EmpPreviousVacations.Where(s => s.IsDeleted == false &&s.IsActive==true && (departmentId == 0 || s.DepartmentId == departmentId)).Include(s => s.Department).OrderByDescending(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                    ViewBag.Count = db.EmpPreviousVacations.Where(s => s.IsDeleted == false && s.IsActive == true && (departmentId == 0 || s.DepartmentId == departmentId)).Count();
                }
                else
                {
                    emps = db.EmpPreviousVacations.Where(s => s.IsDeleted == false&&s.IsActive==true && (db.UserDepartments.Where(a => a.UserId == userId).Select(a => a.DepartmentId).Contains(s.DepartmentId)) && (departmentId == 0 || s.DepartmentId == departmentId)).Include(s => s.Department).OrderByDescending(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                    ViewBag.Count = db.EmpPreviousVacations.Where(s => s.IsDeleted == false&&s.IsActive==true && (db.UserDepartments.Where(a => a.UserId == userId).Select(a => a.DepartmentId).Contains(s.DepartmentId)) && (departmentId == 0 || s.DepartmentId == departmentId)).Count();
                }
            }
            else
            {
                if (userId == 1)
                {
                    emps = db.EmpPreviousVacations.Where(s => s.IsDeleted == false&&s.IsActive==true && (s.DocumentNumber.Contains(searchWord) || s.Department.ArName.Contains(searchWord)) && (departmentId == 0 || s.DepartmentId == departmentId)).Include(s => s.Department).OrderByDescending(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                    ViewBag.Count = db.EmpPreviousVacations.Where(s => s.IsDeleted == false&&s.IsActive==true && (s.DocumentNumber.Contains(searchWord) || s.Department.ArName.Contains(searchWord)) && (departmentId == 0 || s.DepartmentId == departmentId)).Count();
                }
                else
                {
                    emps = db.EmpPreviousVacations.Where(s => s.IsDeleted == false&&s.IsActive==true && (db.UserDepartments.Where(a => a.UserId == userId).Select(a => a.DepartmentId).Contains(s.DepartmentId)) && (s.DocumentNumber.Contains(searchWord) || s.Department.ArName.Contains(searchWord)) && (departmentId == 0 || s.DepartmentId == departmentId)).Include(s => s.Department).OrderByDescending(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                    ViewBag.Count = db.EmpPreviousVacations.Where(s => s.IsDeleted == false&&s.IsActive==true && (db.UserDepartments.Where(a => a.UserId == userId).Select(a => a.DepartmentId).Contains(s.DepartmentId)) && (s.DocumentNumber.Contains(searchWord) || s.Department.ArName.Contains(searchWord)) && (departmentId == 0 || s.DepartmentId == departmentId)).Count();
                }
            }

            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة تسجيل بيانات الاجازات السابقة",
                EnAction = "Index",
                ControllerName = "EmpPreviousVacation",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            return View(emps.ToList());
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
                
                return View();
            }
            EmpPreviousVacation emp = db.EmpPreviousVacations.Find(id);
            if (emp == null)
            {
                return HttpNotFound();
            }
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "اضافة او تعديل تسجيل بيانات الاجازات السابقة ",
                EnAction = "AddEdit",
                ControllerName = "EmpPreviousVacation",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                CodeOrDocNo = emp.DocumentNumber,
                SelectedItem = id
            });

            ViewBag.Next = QueryHelper.Next((int)id, "EmpPreviousVacation");
            ViewBag.Previous = QueryHelper.Previous((int)id, "EmpPreviousVacation");
            ViewBag.Last = QueryHelper.GetLast("EmpPreviousVacation");
            ViewBag.First = QueryHelper.GetFirst("EmpPreviousVacation");
            // Departments
            if (userId == 1)
            {
                //DepartmentId
                ViewBag.DepartmentId = new SelectList(db.Departments.Where(d => d.IsActive == true && d.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName", emp.DepartmentId);
            }
            else
            {
                //DepartmentId
                ViewBag.DepartmentId = new SelectList(db.UserDepartments.Where(d => d.UserId == userId).Select(b => new
                {
                    Id = b.DepartmentId,
                    ArName = b.Department.Code + " - " + b.Department.ArName
                }), "Id", "ArName", emp.DepartmentId);
            }
            //Date
            ViewBag.Date = emp.Date.Value.ToString("yyyy-MM-ddTHH:mm");
            foreach(var item in emp.EmpPreviousVacationDetails)
            {
                //EmployeeId
                ViewBag.EmployeeId = new SelectList(db.Employees.Where(d => d.IsActive == true && d.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName",item.EmployeeId);
                //VacationStartDate
                ViewBag.VacationStartDate = item.VacationStartDate.Value.ToString("yyyy-MM-ddTHH:mm");
                //VacationEndDate
                ViewBag.VacationEndDate = item.VacationEndDate.Value.ToString("yyyy-MM-ddTHH:mm");
                //VacationActualReturnDate
                ViewBag.VacationActualReturnDate = item.VacationActualReturnDate.Value.ToString("yyyy-MM-ddTHH:mm");
            }
           
            return View(emp);

        }

        [HttpPost]
        public ActionResult AddEdit(EmpPreviousVacation emp)
        {
            if (ModelState.IsValid)
            {
                var id = emp.Id;
                emp.IsDeleted = false;
                emp.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
               
                if (emp.Id > 0)
                {
                    if (db.EmpPreviousVacations.Find(emp.Id).IsPosted == true)
                    {
                        return Content("false");
                    }
                    // update procedure 
                    MyXML.xPathName = "Details";
                    var EmpDetails = MyXML.GetXML(emp.EmpPreviousVacationDetails);
                    db.EmpPreviousVacation_Update(emp.Id, emp.DepartmentId, emp.Date, emp.UserId, emp.IsActive, emp.IsDeleted, null, null, EmpDetails,emp.IsPosted);
                    Notification.GetNotification("EmpPreviousVacation", "Edit", "AddEdit", emp.Id, null, "تسجيل بيانات الاجازات السابقة");
                }
                else
                {
                    //insert procedure
                    MyXML.xPathName = "Details";
                   // var EmpDetails = MyXML.GetXML(emp.EmpPreviousVacationDetails.Where(a => a.MainDocId == id));
                    var EmpDetails = MyXML.GetXML(emp.EmpPreviousVacationDetails);
                    var idOut = new ObjectParameter("Id", typeof(Int32));
                    db.EmpPreviousVacation_Insert(idOut, emp.DepartmentId, emp.Date, emp.UserId, emp.IsActive, emp.IsDeleted, null, null, EmpDetails,emp.IsPosted);
                    id = (int)idOut.Value;
                    Notification.GetNotification("EmpPreviousVacation", "Add", "AddEdit", emp.Id, null, "تسجيل بيانات الاجازات السابقة");
                }
               
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = id > 0 ? "تعديل تسجيل بيانات الاجازات السابقة" : "اضافة تسجيل بيانات الاجازات السابقة",
                    EnAction = "AddEdit",
                    ControllerName = "EmpPreviousVacation",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    CodeOrDocNo = emp.DocumentNumber,
                    SelectedItem = id,
                });
                return Json(new { success = "true" });
            }
            return View(emp);
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
            var lastObj = db.EmpPreviousVacations.Where(a => a.IsActive == true && a.IsDeleted == false && a.DepartmentId == id).OrderByDescending(a => a.Id).FirstOrDefault();
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
                    lastObj = db.EmpPreviousVacations.Where(a => a.IsActive == true && a.IsDeleted == false && a.DepartmentId == id && a.Date.Value.Month == VoucherDate.Value.Month && a.Date.Value.Year == VoucherDate.Value.Year).OrderByDescending(a => a.Id).FirstOrDefault();
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
                    lastObj = db.EmpPreviousVacations.Where(a => a.IsActive == true && a.IsDeleted == false && a.DepartmentId == id && a.Date.Value.Year == VoucherDate.Value.Year).OrderByDescending(a => a.Id).FirstOrDefault();

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
            //var docNo = QueryHelper.DocLastNum(id, "EmpPreviousVacation");
            //double i = (docNo) + 1;
            //return Json(i, JsonRequestBehavior.AllowGet);
        }

        [HttpPost, ActionName("Delete")]
        public ActionResult Delete(int id)
        {
            try
            {
                EmpPreviousVacation emp = db.EmpPreviousVacations.Find(id);
                if (emp.IsPosted == true)
                {
                    return Content("false");
                }
                emp.IsDeleted = true;
                emp.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                foreach (var e in emp.EmpPreviousVacationDetails)
                {
                    e.IsDeleted = true;
                }
                Random random = new Random();
                const string chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
                var Code = new string(Enumerable.Repeat(chars, 6).Select(s => s[random.Next(s.Length)]).ToArray());
                emp.DocumentNumber = Code;
                db.Entry(emp).State = EntityState.Modified;

                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "حذف تسجيل بيانات الاجازات السابقة",
                    EnAction = "AddEdit",
                    ControllerName = "EmpPreviousVacation",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id
                });
                ////-------------------- Notification-------------------------////
                Notification.GetNotification("EmpPreviousVacation", "Delete", "Delete", id, null, "تسجيل بيانات الاجازات السابقة");


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