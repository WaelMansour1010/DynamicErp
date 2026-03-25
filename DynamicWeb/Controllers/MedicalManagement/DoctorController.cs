using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;
using MyERP.Models;
using MyERP.Repository;

namespace MyERP.Controllers.MedicalManagement
{
    public class DoctorController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        // GET: Doctor
        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة الأطباء",
                EnAction = "Index",
                ControllerName = "Doctor",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            Notification.GetNotification("Doctor", "View", "Index", null, null, "الأطباء");
            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;
            if (pageIndex > 1)
            {
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;
            }
            IQueryable<Doctor> doctors;
            if (string.IsNullOrEmpty(searchWord))
            {
                doctors = db.Doctors.Where(a => a.IsDeleted == false).OrderBy(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.Doctors.Where(a => a.IsDeleted == false).Count();
            }
            else
            {
                doctors = db.Doctors.Where(a => a.IsDeleted == false &&
                (a.ArName.Contains(searchWord) || a.EnName.Contains(searchWord) || a.Code.Contains(searchWord)))
                    .OrderBy(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.Doctors.Where(a => a.IsDeleted == false && (a.ArName.Contains(searchWord) || a.EnName.Contains(searchWord) || a.Code.Contains(searchWord))).Count();
            }
            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            return View(doctors.ToList());
        }
        public ActionResult AddEdit(int? id)
        {
            if (id == null)
            {
                ViewBag.MedicalSpecialtyId = new SelectList(db.MedicalSpecialties.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");

                return View();
            }
            Doctor doctor = db.Doctors.Find(id);
            if (doctor == null)
            {
                return HttpNotFound();
            }
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "اضافة او تعديل الأطباء ",
                EnAction = "AddEdit",
                ControllerName = "Doctor",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = id
            });
            ViewBag.Next = QueryHelper.Next((int)id, "Doctor");
            ViewBag.Previous = QueryHelper.Previous((int)id, "Doctor");
            ViewBag.Last = QueryHelper.GetLast("Doctor");
            ViewBag.First = QueryHelper.GetFirst("Doctor");
            ViewBag.MedicalSpecialtyId = new SelectList(db.MedicalSpecialties.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", doctor.MedicalSpecialtyId);
            return View(doctor);
        }
        [HttpPost]
        public ActionResult AddEdit(Doctor doctor, string newBtn)
        {
            if (ModelState.IsValid)
            {
                var id = doctor.Id;
                doctor.IsDeleted = false;
                doctor.IsActive = true;
                doctor.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                if (doctor.Id > 0)
                {
                    db.Entry(doctor).State = EntityState.Modified;
                    Notification.GetNotification("Doctor", "Edit", "AddEdit", doctor.Id, null, "الأطباء");
                }
                else
                {
                    doctor.Code = (QueryHelper.CodeLastNum("Doctor") + 1).ToString();
                    db.Doctors.Add(doctor);
                    Notification.GetNotification("Doctor", "Add", "AddEdit", doctor.Id, null, "الأطباء");
                }
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = id > 0 ? "تعديل الأطباء" : "اضافة الأطباء",
                    EnAction = "AddEdit",
                    ControllerName = "Doctor",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    CodeOrDocNo = doctor.Code
                });
                if (newBtn == "saveAndNew")
                {
                    return RedirectToAction("AddEdit");
                }
                else
                {
                    return RedirectToAction("Index");
                }
            }
            var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .Select(x => new { x.Key, x.Value.Errors })
                    .ToArray();
            return View(doctor);
        }

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                Doctor doctor = db.Doctors.Find(id);
                doctor.IsDeleted = true;
                doctor.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                db.Entry(doctor).State = EntityState.Modified;
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "حذف الأطباء",
                    EnAction = "AddEdit",
                    ControllerName = "Doctor",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    EnItemName = doctor.EnName
                });
                Notification.GetNotification("Doctor", "Delete", "Delete", id, null, "الأطباء");
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
                Doctor doctor = db.Doctors.Find(id);
                if (doctor.IsActive == true)
                {
                    doctor.IsActive = false;
                }
                else
                {
                    doctor.IsActive = true;
                }
                doctor.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                db.Entry(doctor).State = EntityState.Modified;
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = (bool)doctor.IsActive ? "تنشيط الأطباء" : "إلغاء تنشيط الأطباء",
                    EnAction = "AddEdit",
                    ControllerName = "Doctor",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = doctor.Id,
                    EnItemName = doctor.EnName,
                    ArItemName = doctor.ArName,
                    CodeOrDocNo = doctor.Code
                });
                if (doctor.IsActive == true)
                {
                    Notification.GetNotification("Doctor", "Activate/Deactivate", "ActivateDeactivate", id, true, "الأطباء");
                }
                else
                {
                    Notification.GetNotification("Doctor", "Activate/Deactivate", "ActivateDeactivate", id, false, " الأطباء");
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
            var code = QueryHelper.CodeLastNum("Doctor");
            return Json(code + 1, JsonRequestBehavior.AllowGet);
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