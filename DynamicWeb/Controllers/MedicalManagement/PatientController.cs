using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;
using MyERP.Models;
using MyERP.Repository;

namespace MyERP.Controllers.MedicalManagement
{
    public class PatientController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        // GET: Patient
        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة المرضى",
                EnAction = "Index",
                ControllerName = "Patient",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            Notification.GetNotification("Patient", "View", "Index", null, null, "المرضى");
            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;
            if (pageIndex > 1)
            {
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;
            }
            IQueryable<Patient> patients;
            if (string.IsNullOrEmpty(searchWord))
            {
                patients = db.Patients.Where(a => a.IsDeleted == false).OrderBy(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.Patients.Where(a => a.IsDeleted == false).Count();
            }
            else
            {
                patients = db.Patients.Where(a => a.IsDeleted == false &&
                (a.ArName.Contains(searchWord) || a.EnName.Contains(searchWord) || a.Code.Contains(searchWord)))
                    .OrderBy(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.Patients.Where(a => a.IsDeleted == false && (a.ArName.Contains(searchWord) || a.EnName.Contains(searchWord) || a.Code.Contains(searchWord))).Count();
            }
            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            return View(patients.ToList());
        }
        public ActionResult AddEdit(int? id)
        {
            if (id == null)
            {
                ViewBag.ChronicDiseaseId = new SelectList(new List<dynamic> {
                    new { Id=1, ArName="ضغط"},
                    new { Id=2, ArName="سكر"}}, "Id", "ArName");

                return View();
            }
            Patient patient = db.Patients.Find(id);
            if (patient == null)
            {
                return HttpNotFound();
            }
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "اضافة او تعديل المرضى ",
                EnAction = "AddEdit",
                ControllerName = "Patient",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = id
            });
            ViewBag.Next = QueryHelper.Next((int)id, "Patient");
            ViewBag.Previous = QueryHelper.Previous((int)id, "Patient");
            ViewBag.Last = QueryHelper.GetLast("Patient");
            ViewBag.First = QueryHelper.GetFirst("Patient");
            ViewBag.ChronicDiseaseId = new SelectList(new List<dynamic> {
                    new { Id=1, ArName="ضغط"},
                    new { Id=2, ArName="سكر"}}, "Id", "ArName", patient.ChronicDiseaseId);
            ViewBag.BirthDate = patient.BirthDate != null ? patient.BirthDate.Value.ToString("yyyy-MM-ddTHH:mm") : null;
            return View(patient);
        }
        [HttpPost]
        public ActionResult AddEdit(Patient patient, string newBtn)
        {
            if (ModelState.IsValid)
            {
                var id = patient.Id;
                patient.IsDeleted = false;
                patient.IsActive = true;
                patient.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                if (patient.Id > 0)
                {
                    db.Entry(patient).State = EntityState.Modified;
                    Notification.GetNotification("Patient", "Edit", "AddEdit", patient.Id, null, "المرضى");
                }
                else
                {
                    patient.Code = (QueryHelper.CodeLastNum("Patient") + 1).ToString();
                    db.Patients.Add(patient);
                    Notification.GetNotification("Patient", "Add", "AddEdit", patient.Id, null, "المرضى");
                }
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = id > 0 ? "تعديل المرضى" : "اضافة المرضى",
                    EnAction = "AddEdit",
                    ControllerName = "Patient",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    CodeOrDocNo = patient.Code
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
            return View(patient);
        }

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                Patient patient = db.Patients.Find(id);
                patient.IsDeleted = true;
                patient.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                db.Entry(patient).State = EntityState.Modified;
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "حذف المرضى",
                    EnAction = "AddEdit",
                    ControllerName = "Patient",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    EnItemName = patient.EnName
                });
                Notification.GetNotification("Patient", "Delete", "Delete", id, null, "المرضى");
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
                Patient patient = db.Patients.Find(id);
                if (patient.IsActive == true)
                {
                    patient.IsActive = false;
                }
                else
                {
                    patient.IsActive = true;
                }
                patient.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                db.Entry(patient).State = EntityState.Modified;
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = (bool)patient.IsActive ? "تنشيط المرضى" : "إلغاء تنشيط المرضى",
                    EnAction = "AddEdit",
                    ControllerName = "Patient",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = patient.Id,
                    EnItemName = patient.EnName,
                    ArItemName = patient.ArName,
                    CodeOrDocNo = patient.Code
                });
                if (patient.IsActive == true)
                {
                    Notification.GetNotification("Patient", "Activate/Deactivate", "ActivateDeactivate", id, true, "المرضى");
                }
                else
                {
                    Notification.GetNotification("Patient", "Activate/Deactivate", "ActivateDeactivate", id, false, " المرضى");
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
            var code = QueryHelper.CodeLastNum("Patient");
            return Json(code + 1, JsonRequestBehavior.AllowGet);
        }



        public ActionResult FaceDetection()
        {
                            return View();
        }
        [HttpPost]
        public ActionResult FaceDetection(bool? IsTrue, string Image)
        {
            if (IsTrue == true)
            {
                string fileName = "";
                fileName = "/images/JobApp/EmployeeImg/x.jpeg";
                var bytes = new byte[7000];
                if (Image.Contains("jpeg"))
                {
                    bytes = Convert.FromBase64String(Image.Replace("data:image/jpeg;base64,", ""));
                }
                else
                {
                    bytes = Convert.FromBase64String(Image.Replace("data:image/png;base64,", ""));
                }


                Bitmap bm = new Bitmap(fileName);
                //Image<Bgr, Byte> img = new Image<Bgr, Byte>(bm); //where bmp is a Bitmap
                //var _fram = img.Resize(320, 240, INTER.CV_INTER_CUBIC);
                //var r = _fram;


                //using (var imageFile = new FileStream(Server.MapPath(fileName), FileMode.Create))
                //{
                //    imageFile.Write(bytes, 0, bytes.Length);
                //    imageFile.Flush();
                //}
                return Json(new { success = true }, JsonRequestBehavior.AllowGet);
            }
            else
                return View();
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