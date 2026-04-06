using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;
using MyERP.Models;
using System.Web.Script.Serialization;
using MyERP.Repository;
using System.Data.Entity.Core.Objects;
using System.IO;

namespace MyERP.Controllers.EducationManagement
{
    public class RegisterAttendanceAndAbsenceController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        // GET: RegisterAttendanceAndAbsence
        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة تسجيل الحضور والغياب",
                EnAction = "Index",
                ControllerName = "RegisterAttendanceAndAbsence",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            Notification.GetNotification("RegisterAttendanceAndAbsence", "View", "Index", null, null, "تسجيل الحضور والغياب");
            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;
            if (pageIndex > 1)
            {
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;
            }
            IQueryable<RegisterAttendanceAndAbsence> registerAttendanceAndAbsences;
            if (string.IsNullOrEmpty(searchWord))
            {
                registerAttendanceAndAbsences = db.RegisterAttendanceAndAbsences.Where(a => a.IsDeleted == false).OrderBy(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.RegisterAttendanceAndAbsences.Where(a => a.IsDeleted == false).Count();
            }
            else
            {
                registerAttendanceAndAbsences = db.RegisterAttendanceAndAbsences.Where(a => a.IsDeleted == false &&
                (/*a.Child.ArName.Contains(searchWord) || a.Child.EnName.Contains(searchWord)||*/
                 a.EducationalSubject.ArName.Contains(searchWord) || a.EducationalSubject.EnName.Contains(searchWord)
                || a.Lesson.ArName.Contains(searchWord) || a.Lesson.EnName.Contains(searchWord)
                || a.Code.Contains(searchWord)))
                    .OrderBy(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.RegisterAttendanceAndAbsences.Where(a => a.IsDeleted == false &&
                (/*a.Child.ArName.Contains(searchWord) || a.Child.EnName.Contains(searchWord)||*/
                 a.EducationalSubject.ArName.Contains(searchWord) || a.EducationalSubject.EnName.Contains(searchWord)
                || a.Lesson.ArName.Contains(searchWord) || a.Lesson.EnName.Contains(searchWord)
                || a.Code.Contains(searchWord))).Count();
            }
            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            return View(registerAttendanceAndAbsences.ToList());
        }
        public ActionResult AddEdit(int? id)
        {
            ViewBag.StudentId = new SelectList(db.Children.Where(a => a.IsActive == true && a.IsDeleted == false).Select(a => new { a.Id, ArName = a.Code + " - " + a.ArName }), "Id", "ArName");
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


            if (id == null)
            {
                ViewBag.LessonId = new SelectList(db.Lessons.Where(a => a.IsActive == true && a.IsDeleted == false).Select(a => new { a.Id, ArName = a.Code + " - " + a.ArName }), "Id", "ArName");
                ViewBag.SubjectId = new SelectList(db.EducationalSubjects.Where(a => a.IsActive == true && a.IsDeleted == false).Select(a => new { a.Id, ArName = a.Code + " - " + a.ArName }), "Id", "ArName");
                ViewBag.Date = cTime.ToString("yyyy-MM-ddTHH:mm");
                return View();
            }
            RegisterAttendanceAndAbsence registerAttendanceAndAbsence = db.RegisterAttendanceAndAbsences.Find(id);
            if (registerAttendanceAndAbsence == null)
            {
                return HttpNotFound();
            }
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "اضافة او تعديل تسجيل الحضور والغياب ",
                EnAction = "AddEdit",
                ControllerName = "RegisterAttendanceAndAbsence",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = id
            });
            ViewBag.Next = QueryHelper.Next((int)id, "RegisterAttendanceAndAbsence");
            ViewBag.Previous = QueryHelper.Previous((int)id, "RegisterAttendanceAndAbsence");
            ViewBag.Last = QueryHelper.GetLast("RegisterAttendanceAndAbsence");
            ViewBag.First = QueryHelper.GetFirst("RegisterAttendanceAndAbsence");

            ViewBag.LessonId = new SelectList(db.Lessons.Where(a => a.IsActive == true && a.IsDeleted == false).Select(a => new { a.Id, ArName = a.Code + " - " + a.ArName }), "Id", "ArName", registerAttendanceAndAbsence.LessonId);
            //ViewBag.StudentId = new SelectList(db.Children.Where(a => a.IsActive == true && a.IsDeleted == false).Select(a => new { a.Id, ArName = a.Code + " - " + a.ArName }), "Id", "ArName", registerAttendanceAndAbsence.StudentId);
            ViewBag.SubjectId = new SelectList(db.EducationalSubjects.Where(a => a.IsActive == true && a.IsDeleted == false).Select(a => new { a.Id, ArName = a.Code + " - " + a.ArName }), "Id", "ArName", registerAttendanceAndAbsence.SubjectId);
            ViewBag.Date = registerAttendanceAndAbsence.Date != null ? registerAttendanceAndAbsence.Date.Value.ToString("yyyy-MM-ddTHH:mm") : null;
            return View(registerAttendanceAndAbsence);
        }
        [HttpPost]
        public ActionResult AddEdit(RegisterAttendanceAndAbsence registerAttendanceAndAbsence)
        {
            if (ModelState.IsValid)
            {
                var id = registerAttendanceAndAbsence.Id;
                registerAttendanceAndAbsence.IsDeleted = false;
                registerAttendanceAndAbsence.IsActive = true;
                registerAttendanceAndAbsence.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                if (registerAttendanceAndAbsence.Id > 0)
                {
                    db.RegisterAttendanceAndAbsenceDetails.RemoveRange(db.RegisterAttendanceAndAbsenceDetails.Where(x => x.MainDocId == registerAttendanceAndAbsence.Id));
                    var registerAttendanceAndAbsenceDetails = registerAttendanceAndAbsence.RegisterAttendanceAndAbsenceDetails.ToList();
                    registerAttendanceAndAbsenceDetails.ForEach((x) => x.MainDocId = registerAttendanceAndAbsence.Id);
                    registerAttendanceAndAbsence.RegisterAttendanceAndAbsenceDetails = null;
                    db.Entry(registerAttendanceAndAbsence).State = EntityState.Modified;
                    db.RegisterAttendanceAndAbsenceDetails.AddRange(registerAttendanceAndAbsenceDetails);
                    Notification.GetNotification("RegisterAttendanceAndAbsence", "Edit", "AddEdit", registerAttendanceAndAbsence.Id, null, "تسجيل الحضور والغياب");
                }
                else
                {
                    registerAttendanceAndAbsence.Code = (QueryHelper.CodeLastNum("RegisterAttendanceAndAbsence") + 1).ToString();
                    db.RegisterAttendanceAndAbsences.Add(registerAttendanceAndAbsence);
                    Notification.GetNotification("RegisterAttendanceAndAbsence", "Add", "AddEdit", registerAttendanceAndAbsence.Id, null, "تسجيل الحضور والغياب");
                }
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = id > 0 ? "تعديل تسجيل الحضور والغياب" : "اضافة تسجيل الحضور والغياب",
                    EnAction = "AddEdit",
                    ControllerName = "RegisterAttendanceAndAbsence",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    CodeOrDocNo = registerAttendanceAndAbsence.Code
                });
                return Json(new { success = true });
            }
            var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .Select(x => new { x.Key, x.Value.Errors })
                    .ToArray();
            return Json(new { success = false });
        }

        //--------------------------- Add Through Details Table -------------------------------------//
        [HttpPost]
        public ActionResult AddEditDetail(RegisterAttendanceAndAbsence registerAttendanceAndAbsence, RegisterAttendanceAndAbsenceDetail Detail)
        {
            if (ModelState.IsValid)
            {
                var id = registerAttendanceAndAbsence.Id;
                registerAttendanceAndAbsence.IsDeleted = false;
                registerAttendanceAndAbsence.IsActive = true;
                registerAttendanceAndAbsence.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                if (registerAttendanceAndAbsence.Id > 0)
                {
                    var Exist = db.RegisterAttendanceAndAbsenceDetails.Where(a => a.MainDocId == registerAttendanceAndAbsence.Id && a.StudentId == Detail.StudentId).FirstOrDefault();
                    if (Exist != null)
                    {
                        return Json(new { success = "Exist" });
                    }
                    db.Entry(registerAttendanceAndAbsence).State = EntityState.Modified;
                    db.RegisterAttendanceAndAbsenceDetails.Add(Detail);
                    Notification.GetNotification("RegisterAttendanceAndAbsence", "Edit", "AddEdit", registerAttendanceAndAbsence.Id, null, "تسجيل الحضور والغياب");
                }
                else
                {
                    registerAttendanceAndAbsence.Code = (QueryHelper.CodeLastNum("RegisterAttendanceAndAbsence") + 1).ToString();
                    db.RegisterAttendanceAndAbsences.Add(registerAttendanceAndAbsence);
                    db.RegisterAttendanceAndAbsenceDetails.Add(Detail);
                    Notification.GetNotification("RegisterAttendanceAndAbsence", "Add", "AddEdit", registerAttendanceAndAbsence.Id, null, "تسجيل الحضور والغياب");
                }
                db.SaveChanges();
                if (id == 0)
                {
                    db.Database.ExecuteSqlCommand($"update RegisterAttendanceAndAbsenceDetail set MainDocId={registerAttendanceAndAbsence.Id} where id= (select max(Id) from RegisterAttendanceAndAbsenceDetail where MainDocId is NULL)");
                }
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = id > 0 ? "تعديل تسجيل الحضور والغياب" : "اضافة تسجيل الحضور والغياب",
                    EnAction = "AddEdit",
                    ControllerName = "RegisterAttendanceAndAbsence",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    CodeOrDocNo = registerAttendanceAndAbsence.Code
                });
                return Json(new { success = true, Id = registerAttendanceAndAbsence.Id });
            }
            var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .Select(x => new { x.Key, x.Value.Errors })
                    .ToArray();
            return Json(new { success = false });
        }

        [HttpPost, ActionName("DeleteDetail")]
        public ActionResult DeleteDetail(int id)
        {
            try
            {
                RegisterAttendanceAndAbsenceDetail registerAttendanceAndAbsenceDetail = db.RegisterAttendanceAndAbsenceDetails.Find(id);
                registerAttendanceAndAbsenceDetail.IsDeleted = true;
                registerAttendanceAndAbsenceDetail.IsAttended = false;
                registerAttendanceAndAbsenceDetail.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                db.Entry(registerAttendanceAndAbsenceDetail).State = EntityState.Modified;
                db.SaveChanges();
                
                return Content("true");
            }
            catch (Exception ex)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
        }

        public ActionResult AdvancedEntry(DateTime Date, int LessonId, int SubjectId, int? Id, string Code, string MasterCode)
        {
            ViewBag.logo = db.SystemSettings.FirstOrDefault().Logo;
            ViewBag.Date = Date;
            ViewBag.LessonId = LessonId;
            ViewBag.SubjectId = SubjectId;
            ViewBag.Id = Id;
            ViewBag.Code = Code;
            ViewBag.MasterCode = MasterCode;
            return View();
        }
        [SkipERPAuthorize]
        public JsonResult SearchStudentByBarcode(string Barcode)
        {
            var Student = db.Children.Where(a => a.IsActive == true && a.IsDeleted == false && a.Code == Barcode).Select(a => new { a.Id, a.ArName,Service=a.ChurchService.ArName }).FirstOrDefault();
            return Json(new { Student }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                RegisterAttendanceAndAbsence registerAttendanceAndAbsence = db.RegisterAttendanceAndAbsences.Find(id);
                registerAttendanceAndAbsence.IsDeleted = true;
                registerAttendanceAndAbsence.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                db.Entry(registerAttendanceAndAbsence).State = EntityState.Modified;
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "حذف تسجيل الحضور والغياب",
                    EnAction = "AddEdit",
                    ControllerName = "RegisterAttendanceAndAbsence",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                });
                Notification.GetNotification("RegisterAttendanceAndAbsence", "Delete", "Delete", id, null, "تسجيل الحضور والغياب");
                return Content("true");
            }
            catch (Exception ex)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
        }

        [HttpPost]
        public ActionResult ActivateDeactivate(int id)
        {
            try
            {
                RegisterAttendanceAndAbsence registerAttendanceAndAbsence = db.RegisterAttendanceAndAbsences.Find(id);
                if (registerAttendanceAndAbsence.IsActive == true)
                {
                    registerAttendanceAndAbsence.IsActive = false;
                }
                else
                {
                    registerAttendanceAndAbsence.IsActive = true;
                }
                registerAttendanceAndAbsence.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                db.Entry(registerAttendanceAndAbsence).State = EntityState.Modified;
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = (bool)registerAttendanceAndAbsence.IsActive ? "تنشيط تسجيل الحضور والغياب" : "إلغاء تنشيط تسجيل الحضور والغياب",
                    EnAction = "AddEdit",
                    ControllerName = "RegisterAttendanceAndAbsence",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = registerAttendanceAndAbsence.Id,
                    CodeOrDocNo = registerAttendanceAndAbsence.Code
                });
                if (registerAttendanceAndAbsence.IsActive == true)
                {
                    Notification.GetNotification("RegisterAttendanceAndAbsence", "Activate/Deactivate", "ActivateDeactivate", id, true, "تسجيل الحضور والغياب");
                }
                else
                {
                    Notification.GetNotification("RegisterAttendanceAndAbsence", "Activate/Deactivate", "ActivateDeactivate", id, false, " تسجيل الحضور والغياب");
                }
                return Content("true");
            }
            catch (Exception ex)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
        }

        [SkipERPAuthorize]
        public JsonResult SetCodeNum()
        {
            var code = QueryHelper.CodeLastNum("RegisterAttendanceAndAbsence");
            return Json(code + 1, JsonRequestBehavior.AllowGet);
        }
        [SkipERPAuthorize]
        public JsonResult SetDetailCodeNum(int Id)
        {
            var DetailCode = 0;
            var code = db.RegisterAttendanceAndAbsenceDetails.Where(a => a.MainDocId == Id).OrderByDescending(a => a.Code).FirstOrDefault().Code;
            if (code == null)
            {
                DetailCode = 0;
            }
            else
            {
                DetailCode = int.Parse(code.ToString());
            }
            return Json(DetailCode + 1, JsonRequestBehavior.AllowGet);
        }
        [SkipERPAuthorize]
        public JsonResult SearchStudent(string Name)
        {
            var Student = db.Children.Where(a => a.IsActive == true && a.IsDeleted == false && (a.ArName.Contains(Name) || a.EnName.Contains(Name))).Select(a => new
            {
                a.Id,
                a.ArName
            }
            ).ToList();
            return Json(Student, JsonRequestBehavior.AllowGet);
        }
        [SkipERPAuthorize]
        public JsonResult CloseLecture(int? Id)
        {
            try
            {
                db.CloseLecture(Id);
                return Json(new { success = true, Id }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                return Json(new { success = false, e }, JsonRequestBehavior.AllowGet);
            }
        }
        [SkipERPAuthorize]
        public JsonResult CheckIfRegisteredBefore(int? LessonId, int? SubjectId)
        {
            var check = db.RegisterAttendanceAndAbsences.Where(a => a.LessonId == LessonId && a.SubjectId == SubjectId && a.IsDeleted == false && a.IsActive == true).FirstOrDefault();
            if (check != null)
            {
                return Json(new { success = "Exist", Id = check.Id },JsonRequestBehavior.AllowGet);
            }
            return Json(new { success = "NotExist" }, JsonRequestBehavior.AllowGet);
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