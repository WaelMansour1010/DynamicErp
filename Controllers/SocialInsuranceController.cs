using DevExpress.DataProcessing.InMemoryDataProcessor;
using DevExpress.XtraRichEdit.Model;
using Microsoft.Ajax.Utilities;
using MyERP.Models;
using MyERP.Repository;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;

namespace MyERP.Controllers
{
    public class SocialInsuranceController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        // GET: SocialInsurance
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
                ArAction = "فتح قائمة تسجيل إثبات التأمين",
                EnAction = "Index",
                ControllerName = "SocialInsurance",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            ////-------------------- Notification-------------------------////
            Notification.GetNotification("SocialInsurance", "View", "Index", null, null, "تسجيل إثبات التأمين");

            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;

            IQueryable<SocialInsurance> socialInsurances;

            if (string.IsNullOrEmpty(searchWord))
            {
                if (userId == 1)
                {
                    socialInsurances = db.SocialInsurances.Where(s => s.IsDeleted == false && (departmentId == 0 || s.DepartmentId == departmentId)).Include(s => s.Department).OrderByDescending(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                    ViewBag.Count = db.SocialInsurances.Where(s => s.IsDeleted == false && (departmentId == 0 || s.DepartmentId == departmentId)).Count();
                }
                else
                {
                    socialInsurances = db.SocialInsurances.Where(s => s.IsDeleted == false && (db.UserDepartments.Where(a => a.UserId == userId).Select(a => a.DepartmentId).Contains(s.DepartmentId)) && (departmentId == 0 || s.DepartmentId == departmentId)).Include(s => s.Department).OrderByDescending(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                    ViewBag.Count = db.SocialInsurances.Where(s => s.IsDeleted == false && (db.UserDepartments.Where(a => a.UserId == userId).Select(a => a.DepartmentId).Contains(s.DepartmentId)) && (departmentId == 0 || s.DepartmentId == departmentId)).Count();
                }
            }
            else
            {
                if (userId == 1)
                {
                    socialInsurances = db.SocialInsurances.Where(s => s.IsDeleted == false && (s.DocumentNumber.Contains(searchWord) || s.Department.ArName.Contains(searchWord)) && (departmentId == 0 || s.DepartmentId == departmentId)).Include(s => s.Department).OrderByDescending(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                    ViewBag.Count = db.SocialInsurances.Where(s => s.IsDeleted == false && (s.DocumentNumber.Contains(searchWord) || s.Department.ArName.Contains(searchWord)) && (departmentId == 0 || s.DepartmentId == departmentId)).Count();
                }
                else
                {
                    socialInsurances = db.SocialInsurances.Where(s => s.IsDeleted == false && (db.UserDepartments.Where(a => a.UserId == userId).Select(a => a.DepartmentId).Contains(s.DepartmentId)) && (s.DocumentNumber.Contains(searchWord) || s.Department.ArName.Contains(searchWord)) && (departmentId == 0 || s.DepartmentId == departmentId)).Include(s => s.Department).OrderByDescending(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                    ViewBag.Count = db.SocialInsurances.Where(s => s.IsDeleted == false && (db.UserDepartments.Where(a => a.UserId == userId).Select(a => a.DepartmentId).Contains(s.DepartmentId)) && (s.DocumentNumber.Contains(searchWord) || s.Department.ArName.Contains(searchWord)) && (departmentId == 0 || s.DepartmentId == departmentId)).Count();
                }
            }

            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة تسجيل إثبات التأمين",
                EnAction = "Index",
                ControllerName = "SocialInsurance",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            return View(socialInsurances.ToList());
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
                    //EmployeeDepartmentId
                    ViewBag.EmployeeDepartmentId = new SelectList(db.Departments.Where(d => d.IsActive == true && d.IsDeleted == false).Select(b => new
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
                    //EmployeeDepartmentId
                    ViewBag.EmployeeDepartmentId = new SelectList(db.UserDepartments.Where(d => d.UserId == userId).Select(b => new
                    {
                        Id = b.DepartmentId,
                        ArName = b.Department.Code + " - " + b.Department.ArName
                    }), "Id", "ArName");
                }

                //Month
                ViewBag.Month = new SelectList(new List<dynamic> {
                new { id=0,name="يناير"},
                new { id=1,name="فبراير"},
                new { id=2,name="مارس"},
                new { id=3,name="إبريل"},
                new { id=4,name="مايو"},
                new { id=5,name="يونيه"},
                new { id=6,name="يوليو"},
                new { id=7,name="اغسطس"},
                new { id=8,name="سبتمبر"},
                new { id=9,name="اكتوبر"},
                new { id=10,name="نوفمبر"},
                new { id=11,name="ديسمبر"}}, "id", "name");

                //year
                List<int> year = new List<int>();
                for (var i = 2020; i <= 2030; i++)
                {
                    year.Add(i);
                    ViewBag.Year = new SelectList(year);
                }
                return View();
            }

            //------------------------------------ Edit----------------------//
            SocialInsurance socialInsurance = db.SocialInsurances.Find(id);
            if (socialInsurance == null)
            {
                return HttpNotFound();
            }

            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "اضافة او تعديل تسجيل إثبات التأمين ",
                EnAction = "AddEdit",
                ControllerName = "SocialInsurance",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                CodeOrDocNo = socialInsurance.DocumentNumber,
                SelectedItem = id
            });

            ViewBag.Next = QueryHelper.Next((int)id, "SocialInsurance");
            ViewBag.Previous = QueryHelper.Previous((int)id, "SocialInsurance");
            ViewBag.Last = QueryHelper.GetLast("SocialInsurance");
            ViewBag.First = QueryHelper.GetFirst("SocialInsurance");

            //Month
            ViewBag.Month = new SelectList(new List<dynamic> {
                new { id=0,name="يناير"},
                new { id=1,name="فبراير"},
                new { id=2,name="مارس"},
                new { id=3,name="إبريل"},
                new { id=4,name="مايو"},
                new { id=5,name="يونيه"},
                new { id=6,name="يوليو"},
                new { id=7,name="اغسطس"},
                new { id=8,name="سبتمبر"},
                new { id=9,name="اكتوبر"},
                new { id=10,name="نوفمبر"},
                new { id=11,name="ديسمبر"}}, "id", "name", socialInsurance.Month);

            //year 
            List<int> Eyear = new List<int>();
            for (var i = 2020; i <= 2030; i++)
            {
                Eyear.Add(i);
                ViewBag.Year = new SelectList(Eyear, socialInsurance.Year);
            }

            // Departments
            if (userId == 1)
            {
                //DepartmentId
                ViewBag.DepartmentId = new SelectList(db.Departments.Where(d => d.IsActive == true && d.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName", socialInsurance.DepartmentId);
                //EmployeeDepartmentId
                ViewBag.EmployeeDepartmentId = new SelectList(db.Departments.Where(d => d.IsActive == true && d.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName", socialInsurance.EmployeeDepartmentId);
            }
            else
            {
                //DepartmentId
                ViewBag.DepartmentId = new SelectList(db.UserDepartments.Where(d => d.UserId == userId).Select(b => new
                {
                    Id = b.DepartmentId,
                    ArName = b.Department.Code + " - " + b.Department.ArName
                }), "Id", "ArName", socialInsurance.DepartmentId);
                //EmployeeDepartmentId
                ViewBag.DepartmentId = new SelectList(db.UserDepartments.Where(d => d.UserId == userId).Select(b => new
                {
                    Id = b.DepartmentId,
                    ArName = b.Department.Code + " - " + b.Department.ArName
                }), "Id", "ArName", socialInsurance.EmployeeDepartmentId);
            }
            //Date
            ViewBag.Date = socialInsurance.Date.Value.ToString("yyyy-MM-ddTHH:mm");

            return View(socialInsurance);
        }
        [HttpPost]
        public ActionResult AddEdit(SocialInsurance socialInsurance)
        {
            if (ModelState.IsValid)
            {
                var id = socialInsurance.Id;
                socialInsurance.IsDeleted = false;
                if (socialInsurance.Id > 0)
                {
                    socialInsurance.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    // use another object to prevent entity error
                    var old = db.SocialInsurances.Find(id);
                    db.SocialInsuranceDetails.RemoveRange(db.SocialInsuranceDetails.Where(p => p.SocialInsuranceId == old.Id).ToList());
                    old.Date = socialInsurance.Date;
                    old.DepartmentId = socialInsurance.DepartmentId;
                    old.EmployeeDepartmentId = socialInsurance.EmployeeDepartmentId;
                    old.DocumentNumber = socialInsurance.DocumentNumber;
                    old.Month = socialInsurance.Month;
                    old.Year = socialInsurance.Year;
                    old.UserId = socialInsurance.UserId;
                    old.IsDeleted = socialInsurance.IsDeleted;
                    old.TotalCompanyPercentage = socialInsurance.TotalCompanyPercentage;
                    old.IsPosted = socialInsurance.IsPosted;
                    foreach (var item in socialInsurance.SocialInsuranceDetails)
                    {
                        old.SocialInsuranceDetails.Add(item);
                    }
                    db.Entry(old).State = EntityState.Modified;
                    Notification.GetNotification("SocialInsurance", "Edit", "AddEdit", socialInsurance.Id, null, "تسجيل إثبات التأمين");
                }
                else
                {
                    socialInsurance.DocumentNumber = new JavaScriptSerializer().Serialize(SetDocNum((int)socialInsurance.DepartmentId, socialInsurance.Date).Data).ToString().Trim('"');
                    socialInsurance.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    db.SocialInsurances.Add(socialInsurance);
                    Notification.GetNotification("SocialInsurance", "Add", "AddEdit", socialInsurance.Id, null, "تسجيل إثبات التأمين");
                }
                try
                {
                    db.SaveChanges();
                }
                catch (Exception ex)
                {
                    var mod = ModelState.First(c => c.Key == "Code");  // this
                    mod.Value.Errors.Add("هذا الكود موجود من قبل");
                    return View(socialInsurance);
                }
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = id > 0 ? "تعديل تسجيل إثبات التأمين" : "اضافة تسجيل إثبات التأمين",
                    EnAction = "AddEdit",
                    ControllerName = "SocialInsurance",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    CodeOrDocNo = socialInsurance.DocumentNumber,
                    SelectedItem = id,
                });
                return Json(new { success = "true" });
            }
            return View(socialInsurance);
        }

        [SkipERPAuthorize]
        public JsonResult GetSalaryValue(int? id)
        {
            return Json(db.SocialInsuranceGetItemValueOfSalaryItem(id), JsonRequestBehavior.AllowGet);
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
            var lastObj = db.SocialInsurances.Where(a => a.IsDeleted == false && a.DepartmentId == id).OrderByDescending(a => a.Id).FirstOrDefault();
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
                    lastObj = db.SocialInsurances.Where(a => a.IsDeleted == false && a.DepartmentId == id && a.Date.Value.Month == VoucherDate.Value.Month && a.Date.Value.Year == VoucherDate.Value.Year).OrderByDescending(a => a.Id).FirstOrDefault();
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
                    lastObj = db.SocialInsurances.Where(a => a.IsDeleted == false && a.DepartmentId == id && a.Date.Value.Year == VoucherDate.Value.Year).OrderByDescending(a => a.Id).FirstOrDefault();

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
            //var docNo = QueryHelper.DocLastNum(id, "SocialInsurance");
            //double i = (docNo) + 1;
            //return Json(i, JsonRequestBehavior.AllowGet);
        }

        [HttpPost, ActionName("Delete")]
        public ActionResult Delete(int id)
        {
            try
            {
                SocialInsurance socialInsurance = db.SocialInsurances.Find(id);
                socialInsurance.IsDeleted = true;
                socialInsurance.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                foreach (var insurance in socialInsurance.SocialInsuranceDetails)
                {
                    insurance.IsDeleted = true;
                }

                Random random = new Random();
                const string chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
                var Code = new string(Enumerable.Repeat(chars, 6).Select(s => s[random.Next(s.Length)]).ToArray());
                socialInsurance.DocumentNumber = Code;
                db.Entry(socialInsurance).State = EntityState.Modified;

                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "حذف تسجيل إثبات التأمين",
                    EnAction = "AddEdit",
                    ControllerName = "SocialInsurance",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id
                });
                ////-------------------- Notification-------------------------////
                Notification.GetNotification("SocialInsurance", "Delete", "Delete", id, null, "تسجيل إثبات التأمين");


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