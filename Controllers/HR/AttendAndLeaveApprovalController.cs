using MyERP.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;
using Excel = Microsoft.Office.Interop.Excel;
using System.IO;
using DevExpress.Charts.Native;
using System.Data.Entity;
using MyERP.Repository;
using System.Net;
using System.Data.Entity.Core.Objects;
using System.Globalization;
using System.Data;
using System.Web.Script.Serialization;

namespace MyERP.Controllers.HR
{
    public class AttendAndLeaveApprovalController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();
        // GET: AttendAndLeaveApproval
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
                ArAction = "فتح قائمة إعتماد الحضور والإنصراف",
                EnAction = "Index",
                ControllerName = "AttendAndLeaveApproval",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            ////--------------------Notification------------------------ -////
            Notification.GetNotification("AttendAndLeaveApproval", "View", "Index", null, null, "  إعتماد الحضور والإنصراف");
            ////-----------------------------------------------------------------------
            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;

            IQueryable<AttendAndLeaveApproval> attendAndLeaveApprovals;

            if (string.IsNullOrEmpty(searchWord))
            {
                if (userId == 1)
                {
                    attendAndLeaveApprovals = db.AttendAndLeaveApprovals.Where(s => s.IsDeleted == false && (departmentId == 0 || s.DepartmentId == departmentId)).OrderByDescending(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                    ViewBag.Count = db.AttendAndLeaveApprovals.Where(s => s.IsDeleted == false && (departmentId == 0 || s.DepartmentId == departmentId)).Count();
                }
                else
                {
                    attendAndLeaveApprovals = db.AttendAndLeaveApprovals.Where(s => s.IsDeleted == false && (db.UserDepartments.Where(a => a.UserId == userId).Select(a => a.DepartmentId).Contains(s.DepartmentId)) && (departmentId == 0 || s.DepartmentId == departmentId)).OrderByDescending(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                    ViewBag.Count = db.AttendAndLeaveApprovals.Where(s => s.IsDeleted == false&& (db.UserDepartments.Where(a => a.UserId == userId).Select(a => a.DepartmentId).Contains(s.DepartmentId)) && (departmentId == 0 || s.DepartmentId == departmentId)).Count();
                }
            }
            else
            {
                if (userId == 1)
                {
                    attendAndLeaveApprovals = db.AttendAndLeaveApprovals.Where(s => s.IsDeleted == false && (departmentId == 0 || s.DepartmentId == departmentId) && (s.DocumentNumber.Contains(searchWord) || s.Department.ArName.Contains(searchWord) || s.Department.EnName.Contains(searchWord) || s.Date.ToString().Contains(searchWord) || s.Notes.Contains(searchWord))).OrderByDescending(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                    ViewBag.Count = db.AttendAndLeaveApprovals.Where(s => s.IsDeleted == false && (departmentId == 0 || s.DepartmentId == departmentId) && (s.DocumentNumber.Contains(searchWord) || s.Department.ArName.Contains(searchWord) || s.Department.EnName.Contains(searchWord) || s.Date.ToString().Contains(searchWord) || s.Notes.Contains(searchWord))).Count();
                }
                else
                {
                    attendAndLeaveApprovals = db.AttendAndLeaveApprovals.Where(s => s.IsDeleted == false&& (db.UserDepartments.Where(a => a.UserId == userId).Select(a => a.DepartmentId).Contains(s.DepartmentId)) && (departmentId == 0 || s.DepartmentId == departmentId) && (s.DocumentNumber.Contains(searchWord) || s.Department.ArName.Contains(searchWord) || s.Department.EnName.Contains(searchWord) || s.Date.ToString().Contains(searchWord) || s.Notes.Contains(searchWord))).OrderByDescending(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                    ViewBag.Count = db.AttendAndLeaveApprovals.Where(s => s.IsDeleted == false&& (db.UserDepartments.Where(a => a.UserId == userId).Select(a => a.DepartmentId).Contains(s.DepartmentId)) && (departmentId == 0 || s.DepartmentId == departmentId) && (s.DocumentNumber.Contains(searchWord) || s.Department.ArName.Contains(searchWord) || s.Department.EnName.Contains(searchWord) || s.Date.ToString().Contains(searchWord) || s.Notes.Contains(searchWord))).Count();
                }
            }
            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            return View(attendAndLeaveApprovals.ToList());
        }
        public ActionResult AddEdit(int? id)
        {
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
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
            AttendAndLeaveApproval e = db.AttendAndLeaveApprovals.Find(id);
            if (e == null)
            {
                return HttpNotFound();
            }
            ViewBag.Next = QueryHelper.Next((int)id, "AttendAndLeaveApproval");
            ViewBag.Previous = QueryHelper.Previous((int)id, "AttendAndLeaveApproval");
            ViewBag.Last = QueryHelper.GetLast("AttendAndLeaveApproval");
            ViewBag.First = QueryHelper.GetFirst("AttendAndLeaveApproval");
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح تفاصيل إعتماد الحضور والإنصراف",
                EnAction = "AddEdit",
                ControllerName = "AttendAndLeaveApproval",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = e.Id,
                CodeOrDocNo = e.DocumentNumber
            });
            // Departments
            if (userId == 1)
            {
                //DepartmentId
                ViewBag.DepartmentId = new SelectList(db.Departments.Where(d => d.IsActive == true && d.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName", e.DepartmentId);
            }
            else
            {
                //DepartmentId
                ViewBag.DepartmentId = new SelectList(db.UserDepartments.Where(d => d.UserId == userId).Select(b => new
                {
                    Id = b.DepartmentId,
                    ArName = b.Department.Code + " - " + b.Department.ArName
                }), "Id", "ArName", e.DepartmentId);
            }
            //EmployeeId
            ViewBag.EmployeeId = new SelectList(db.Employees.Where(d => d.IsActive == true && d.IsDeleted == false).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName");
            try
            {
                ViewBag.Date = e.Date.Value.ToString("yyyy-MM-ddTHH:mm");
                ViewBag.DateFrom = e.DateFrom.Value.ToString("yyyy-MM-ddTHH:mm");
                ViewBag.DateTo = e.DateTo.Value.ToString("yyyy-MM-ddTHH:mm");
            }
            catch (Exception)
            {
            }
            return View(e);
        }
        [HttpPost]
        public ActionResult AddEdit(AttendAndLeaveApproval attendAndLeaveApproval)
        {
            if (ModelState.IsValid)
            {
                var id = attendAndLeaveApproval.Id;
                attendAndLeaveApproval.IsDeleted = false;
                attendAndLeaveApproval.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                // IsIgnored "Delete All Operations"
                foreach (var detail in attendAndLeaveApproval.AttendAndLeaveApprovalDetails)
                {
                    if (detail.IsIgnored == true)
                    {
                        if (detail.OvertimeIssueId != null)
                        {
                            var overtime = db.OvertimeIssues.Find(detail.OvertimeIssueId);
                            if (overtime != null)
                            {
                                overtime.IsDeleted = true;
                                foreach (var i in overtime.OvertimeIssueDetials)
                                {
                                    i.IsDeleted = true;
                                }
                            }
                            db.Entry(overtime).State = EntityState.Modified;
                        }
                        if (detail.RewardIssueId != null)
                        {
                            var reward = db.RewardIssues.Find(detail.RewardIssueId);
                            if (reward != null)
                            {
                                reward.IsDeleted = true;
                                foreach (var i in reward.RewardIssueDetials)
                                {
                                    i.IsDeleted = true;
                                }
                            }
                            db.Entry(reward).State = EntityState.Modified;
                        }
                        if (detail.PenaltyIssueId != null)
                        {
                            var penalty = db.PenaltyIssues.Find(detail.PenaltyIssueId);
                            if (penalty != null)
                            {
                                penalty.IsDeleted = true;
                                foreach (var i in penalty.PenaltyIssueDetails)
                                {
                                    i.IsDeleted = true;
                                }
                            }
                            db.Entry(penalty).State = EntityState.Modified;
                        }
                        if (detail.EmployeeLatenessId != null)
                        {
                            var lateness = db.EmployeeLatenesses.Find(detail.EmployeeLatenessId);
                            if (lateness != null)
                            {
                                lateness.IsDeleted = true;
                            }
                            db.Entry(lateness).State = EntityState.Modified;
                        }
                        if (detail.EmployeeAbsenceId != null)
                        {
                            var absence = db.EmployeeAbsences.Find(detail.EmployeeAbsenceId);
                            if (absence != null)
                            {
                                absence.IsDeleted = true;
                            }
                            db.Entry(absence).State = EntityState.Modified;
                        }
                    }
                }
                //------------------ End of IsIgnored ----------------------------//

                if (attendAndLeaveApproval.Id > 0)
                {
                    // update 
                    var old = db.AttendAndLeaveApprovals.Find(id);
                    db.AttendAndLeaveApprovalDetails.RemoveRange(db.AttendAndLeaveApprovalDetails.Where(p => p.MainDocId == old.Id).ToList());
                    old.Date = attendAndLeaveApproval.Date;
                    old.DateFrom = attendAndLeaveApproval.DateFrom;
                    old.DateTo = attendAndLeaveApproval.DateTo;
                    old.DepartmentId = attendAndLeaveApproval.DepartmentId;
                    old.EmployeeId = attendAndLeaveApproval.EmployeeId;
                    old.Image = attendAndLeaveApproval.Image;
                    old.Notes = attendAndLeaveApproval.Notes;
                    old.IsPosted = attendAndLeaveApproval.IsPosted;
                    old.IsDeleted = attendAndLeaveApproval.IsDeleted;
                    old.SelectedId = attendAndLeaveApproval.SelectedId;
                    old.SystemPageId = attendAndLeaveApproval.SystemPageId;
                    old.UserId = attendAndLeaveApproval.UserId;
                    foreach (var item in attendAndLeaveApproval.AttendAndLeaveApprovalDetails)
                    {
                        old.AttendAndLeaveApprovalDetails.Add(item);
                    }
                    db.Entry(old).State = EntityState.Modified;
                    Notification.GetNotification("AttendAndLeaveApproval", "Edit", "AddEdit", attendAndLeaveApproval.Id, null, "إعتماد الحضور والإنصراف");
                }
                else
                {
                    //insert 
                    attendAndLeaveApproval.DocumentNumber = new JavaScriptSerializer().Serialize(SetDocNum((int)attendAndLeaveApproval.DepartmentId, attendAndLeaveApproval.Date).Data).ToString().Trim('"');
                    db.AttendAndLeaveApprovals.Add(attendAndLeaveApproval);
                    Notification.GetNotification("AttendAndLeaveApproval", "Add", "AddEdit", attendAndLeaveApproval.Id, null, "إعتماد الحضور والإنصراف");
                }
                db.SaveChanges();
                db.AttendAndLeaveApproval_Ignore(attendAndLeaveApproval.Id);
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = id > 0 ? "تعديل إعتماد الحضور والإنصراف" : "اضافة إعتماد الحضور والإنصراف",
                    EnAction = "AddEdit",
                    ControllerName = "AttendAndLeaveApproval",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    CodeOrDocNo = attendAndLeaveApproval.DocumentNumber,
                    SelectedItem = id,
                });
                return Json(new { success = "true" });
            }
            var errors = ModelState
                  .Where(x => x.Value.Errors.Count > 0)
                  .Select(x => new { x.Key, x.Value.Errors })
                  .ToArray();

            return Json(new { success = false, errors });

            //return View(attendAndLeaveApproval);
        }
        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                AttendAndLeaveApproval e = db.AttendAndLeaveApprovals.Find(id);
                if (e.IsPosted == true)
                {
                    return Content("false");
                }
                List<AttendAndLeaveApprovalDetail> attendAndLeaveApprovalDetails = db.AttendAndLeaveApprovalDetails.Where(a => a.MainDocId == id).ToList();
                e.IsDeleted = true;
                e.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                for (int i = 0; i < attendAndLeaveApprovalDetails.Count(); i++)
                {
                    attendAndLeaveApprovalDetails[i].IsDeleted = true;
                }
                Random random = new Random();
                const string chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
                var Code = new string(Enumerable.Repeat(chars, 6).Select(s => s[random.Next(s.Length)]).ToArray());
                e.DocumentNumber = Code;
                db.Entry(e).State = EntityState.Modified;

                db.SaveChanges();

                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "حذف إعتماد الحضور والإنصراف",
                    EnAction = "AddEdit",
                    ControllerName = "AttendAndLeaveApproval",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    CodeOrDocNo = e.DocumentNumber
                });
                ////--------------------Notification------------------------ -////
                Notification.GetNotification("AttendAndLeaveApproval", "Delete", "Delete", id, null, " إعتماد الحضور والإنصراف ");

                // -----------------------------------------------------------------------//

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
            var lastObj = db.AttendAndLeaveApprovals.Where(a => a.IsDeleted == false && a.DepartmentId == id).OrderByDescending(a => a.Id).FirstOrDefault();
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
                    lastObj = db.AttendAndLeaveApprovals.Where(a => a.IsDeleted == false && a.DepartmentId == id && a.Date.Value.Month == VoucherDate.Value.Month && a.Date.Value.Year == VoucherDate.Value.Year).OrderByDescending(a => a.Id).FirstOrDefault();
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
                    lastObj = db.AttendAndLeaveApprovals.Where(a => a.IsDeleted == false && a.DepartmentId == id && a.Date.Value.Year == VoucherDate.Value.Year).OrderByDescending(a => a.Id).FirstOrDefault();

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

            //var docNo = QueryHelper.DocLastNum(id, "AttendAndLeaveApproval");
            //double i = (docNo) + 1;
            //return Json(i, JsonRequestBehavior.AllowGet);
        }
        [SkipERPAuthorize]
        public JsonResult GetAttendAndLeaveApproval(DateTime? DateFrom, DateTime? DateTo, int DepartmentId, int EmployeeId)
        {
            var attendAndLeaveApproval = db.GetAttendAndLeaveApprovalData(EmployeeId, DepartmentId, DateFrom, DateTo).ToList();
            return Json(attendAndLeaveApproval, JsonRequestBehavior.AllowGet);
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