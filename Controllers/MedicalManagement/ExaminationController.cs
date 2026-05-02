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

namespace MyERP.Controllers.MedicalManagement
{
    public class ExaminationController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        // GET: Examination
        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة الكشوفات",
                EnAction = "Index",
                ControllerName = "Examination",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            ////-------------------- Notification-------------------------////
            Notification.GetNotification("Examination", "View", "Index", null, null, "الكشوفات");

            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;

            IQueryable<Examination> examinations;

            if (string.IsNullOrEmpty(searchWord))
            {
                examinations = db.Examinations.Where(s => s.IsDeleted == false).OrderByDescending(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.Examinations.Where(s => s.IsDeleted == false).Count();
            }
            else
            {
                examinations = db.Examinations.Where(s => s.IsDeleted == false && (s.DocumentNumber.Contains(searchWord) || s.Doctor.ArName.Contains(searchWord) || s.Patient.ArName.Contains(searchWord))).OrderByDescending(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.Examinations.Where(s => s.IsDeleted == false && (s.DocumentNumber.Contains(searchWord) || s.Doctor.ArName.Contains(searchWord) || s.Patient.ArName.Contains(searchWord))).Count();
            }

            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة الكشوفات",
                EnAction = "Index",
                ControllerName = "Examination",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            return View(examinations.ToList());
        }
        public ActionResult AddEdit(int? id)
        {
            ViewBag.PatientPopUpChronicDiseaseId = new SelectList(new List<dynamic> {
                    new { Id=1, ArName="ضغط"},
                    new { Id=2, ArName="سكر"}}, "Id", "ArName");

            ViewBag.ItemId = new SelectList(db.Items.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName");

            if (id == null)
            {

                ViewBag.DoctorId = new SelectList(db.Doctors.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                ViewBag.MedicalSpecialtyId = new SelectList(db.MedicalSpecialties.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                ViewBag.PatientId = new SelectList(db.Patients.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
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
                ViewBag.BookingDate = cTime.ToString("yyyy-MM-ddTHH:mm");
                ViewBag.ExaminationDate = cTime.ToString("yyyy-MM-ddTHH:mm");
                return View();
            }
            //--------------------------------------------------------------------------------------------//
            Examination examination = db.Examinations.Find(id);

            ViewBag.DoctorId = new SelectList(db.Doctors.Where(a => a.IsActive == true && a.IsDeleted == false && a.MedicalSpecialtyId == examination.MedicalSpecialtyId).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", examination.DoctorId);
            ViewBag.MedicalSpecialtyId = new SelectList(db.MedicalSpecialties.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", examination.MedicalSpecialtyId);
            ViewBag.PatientId = new SelectList(db.Patients.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", examination.PatientId);

            if (examination == null)
            {
                return HttpNotFound();
            }
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "اضافة او تعديل الكشوفات ",
                EnAction = "AddEdit",
                ControllerName = "Examination",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = id
            });

            ViewBag.BookingDate = examination.BookingDate != null ? examination.BookingDate.Value.ToString("yyyy-MM-ddTHH:mm") : null;
            ViewBag.ExaminationDate = examination.ExaminationDate != null ? examination.ExaminationDate.Value.ToString("yyyy-MM-ddTHH:mm") : null;

            ViewBag.Next = QueryHelper.Next((int)id, "Examination");
            ViewBag.Previous = QueryHelper.Previous((int)id, "Examination");
            ViewBag.Last = QueryHelper.GetLast("Examination");
            ViewBag.First = QueryHelper.GetFirst("Examination");
            return View(examination);
        }

        [HttpPost]
        public ActionResult AddEdit(Examination examination)
        {
            if (ModelState.IsValid)
            {
                var id = examination.Id;
                examination.IsDeleted = false;

                examination.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                string domainName = Request.Url.GetLeftPart(UriPartial.Authority);
                var bytes = new byte[7000];
                string fileName = "";
                var ExaminationRaysImageList = new List<ExaminationRayImage>();
                var ExaminationAnalysisImageList = new List<ExaminationAnalysisImage>();

                if (examination.Id > 0)
                {
                    if (examination.ExaminationRayImages != null)
                    {
                        var lastRayImage = db.ExaminationRayImages.OrderByDescending(a => a.Id).FirstOrDefault();
                        int LastItemID = (lastRayImage != null ? lastRayImage.Id : 0) + 1;

                        foreach (var img in examination.ExaminationRayImages)
                        {

                            if (img != null && img.Image.Contains("base64"))
                            {
                                fileName = "/Rays" + LastItemID.ToString() /*+ "_" + roomType.ArName*/ + ".jpeg";
                                if (img.Image.Contains("jpeg"))
                                {
                                    bytes = Convert.FromBase64String(img.Image.Replace("data:image/jpeg;base64,", ""));
                                }
                                else
                                {
                                    bytes = Convert.FromBase64String(img.Image.Replace("data:image/png;base64,", ""));
                                }
                                using (var imageFile = new FileStream(Server.MapPath(fileName), FileMode.Create))
                                {
                                    imageFile.Write(bytes, 0, bytes.Length);
                                    imageFile.Flush();
                                }
                                ExaminationRaysImageList.Add(new ExaminationRayImage()
                                {
                                    Image = domainName + fileName,
                                    MainDocId = examination.Id
                                });
                                LastItemID++;
                            }
                            else //previous images
                            {
                                ExaminationRaysImageList.Add(new ExaminationRayImage()
                                {
                                    Image = img.Image,
                                    MainDocId = examination.Id
                                });
                            }
                        }
                    }

                    if (examination.ExaminationAnalysisImages != null)
                    {

                        var lastAnalysisImage = db.ExaminationAnalysisImages.OrderByDescending(a => a.Id).FirstOrDefault();
                        int LastItemID = (lastAnalysisImage != null ? lastAnalysisImage.Id : 0) + 1;

                        foreach (var img in examination.ExaminationAnalysisImages)
                        {

                            if (img != null && img.Image.Contains("base64"))
                            {
                                fileName = "/Analysis" + LastItemID.ToString() /*+ "_" + roomType.ArName*/ + ".jpeg";
                                if (img.Image.Contains("jpeg"))
                                {
                                    bytes = Convert.FromBase64String(img.Image.Replace("data:image/jpeg;base64,", ""));
                                }
                                else
                                {
                                    bytes = Convert.FromBase64String(img.Image.Replace("data:image/png;base64,", ""));
                                }
                                using (var imageFile = new FileStream(Server.MapPath(fileName), FileMode.Create))
                                {
                                    imageFile.Write(bytes, 0, bytes.Length);
                                    imageFile.Flush();
                                }
                                ExaminationAnalysisImageList.Add(new ExaminationAnalysisImage()
                                {
                                    Image = domainName + fileName,
                                    MainDocId = examination.Id
                                });
                                LastItemID++;
                            }
                            else //previous images
                            {
                                ExaminationAnalysisImageList.Add(new ExaminationAnalysisImage()
                                {
                                    Image = img.Image,
                                    MainDocId = examination.Id
                                });
                            }
                        }
                    }


                    db.ExaminationAnalysis.RemoveRange(db.ExaminationAnalysis.Where(x => x.MainDocId == examination.Id));
                    var examinationAnalysis = examination.ExaminationAnalysis.ToList();
                    examinationAnalysis.ForEach((x) => x.MainDocId = examination.Id);
                    examination.ExaminationAnalysis = null;

                    db.ExaminationAnalysisImages.RemoveRange(db.ExaminationAnalysisImages.Where(x => x.MainDocId == examination.Id));
                    var examinationAnalysisImages = examination.ExaminationAnalysisImages.ToList();
                    examinationAnalysisImages.ForEach((x) => x.MainDocId = examination.Id);
                    examination.ExaminationAnalysisImages = null;

                    db.ExaminationMedicines.RemoveRange(db.ExaminationMedicines.Where(x => x.MainDocId == examination.Id));
                    var examinationMedicines = examination.ExaminationMedicines.ToList();
                    examinationMedicines.ForEach((x) => x.MainDocId = examination.Id);
                    examination.ExaminationMedicines = null;

                    db.ExaminationRays.RemoveRange(db.ExaminationRays.Where(x => x.MainDocId == examination.Id));
                    var examinationRays = examination.ExaminationRays.ToList();
                    examinationRays.ForEach((x) => x.MainDocId = examination.Id);
                    examination.ExaminationRays = null;

                    db.ExaminationRayImages.RemoveRange(db.ExaminationRayImages.Where(x => x.MainDocId == examination.Id));
                    var examinationRayImages = examination.ExaminationRayImages.ToList();
                    examinationRayImages.ForEach((x) => x.MainDocId = examination.Id);
                    examination.ExaminationRayImages = null;

                    db.Entry(examination).State = EntityState.Modified;
                    db.ExaminationAnalysis.AddRange(examinationAnalysis);
                    db.ExaminationAnalysisImages.AddRange(examinationAnalysisImages);
                    db.ExaminationMedicines.AddRange(examinationMedicines);
                    db.ExaminationRays.AddRange(examinationRays);
                    db.ExaminationRayImages.AddRange(examinationRayImages);


                    Notification.GetNotification("Examination", "Edit", "AddEdit", examination.Id, null, "الكشوفات");
                }
                else
                {
                    if (examination.ExaminationRayImages != null)
                    {
                        var lastRayImage = db.ExaminationRayImages.OrderByDescending(a => a.Id).FirstOrDefault();
                        int LastItemID = (lastRayImage != null ? lastRayImage.Id : 0) + 1;

                        foreach (var img in examination.ExaminationRayImages)
                        {
                            if (img != null && img.Image.Contains("base64"))
                            {
                                fileName = "/Rays" + LastItemID.ToString() /*+ "_" + roomType.ArName*/ + ".jpeg";
                                if (img.Image.Contains("jpeg"))
                                {
                                    bytes = Convert.FromBase64String(img.Image.Replace("data:image/jpeg;base64,", ""));
                                }
                                else
                                {
                                    bytes = Convert.FromBase64String(img.Image.Replace("data:image/png;base64,", ""));
                                }
                                using (var imageFile = new FileStream(Server.MapPath(fileName), FileMode.Create))
                                {
                                    imageFile.Write(bytes, 0, bytes.Length);
                                    imageFile.Flush();
                                }
                                ExaminationRaysImageList.Add(new ExaminationRayImage()
                                {
                                    Image = domainName + fileName,
                                    MainDocId = LastItemID
                                });
                            }
                            LastItemID++;
                        }

                    }


                    if (examination.ExaminationAnalysisImages != null)
                    {

                        var lastAnalysisImage = db.ExaminationAnalysisImages.OrderByDescending(a => a.Id).FirstOrDefault();
                        int LastItemID = (lastAnalysisImage != null ? lastAnalysisImage.Id : 0) + 1;

                        foreach (var img in examination.ExaminationAnalysisImages)
                        {

                            if (img != null && img.Image.Contains("base64"))
                            {
                                fileName = "/Analysis" + LastItemID.ToString() /*+ "_" + roomType.ArName*/ + ".jpeg";
                                if (img.Image.Contains("jpeg"))
                                {
                                    bytes = Convert.FromBase64String(img.Image.Replace("data:image/jpeg;base64,", ""));
                                }
                                else
                                {
                                    bytes = Convert.FromBase64String(img.Image.Replace("data:image/png;base64,", ""));
                                }
                                using (var imageFile = new FileStream(Server.MapPath(fileName), FileMode.Create))
                                {
                                    imageFile.Write(bytes, 0, bytes.Length);
                                    imageFile.Flush();
                                }
                                ExaminationAnalysisImageList.Add(new ExaminationAnalysisImage()
                                {
                                    Image = domainName + fileName,
                                    MainDocId = examination.Id
                                });
                                LastItemID++;
                            }
                        }
                    }



                    examination.DocumentNumber = new JavaScriptSerializer().Serialize(SetDocNum().Data).ToString().Trim('"');
                    db.Examinations.Add(examination);
                    Notification.GetNotification("Examination", "Add", "AddEdit", examination.Id, null, "الكشوفات");
                }
                try
                {
                    db.SaveChanges();
                }
                catch (Exception ex)
                {
                    var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .Select(x => new { x.Key, x.Value.Errors })
                    .ToArray();
                }
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = id > 0 ? "تعديل الكشوفات" : "اضافة الكشوفات",
                    EnAction = "AddEdit",
                    ControllerName = "Examination",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    CodeOrDocNo = examination.DocumentNumber
                });
                return Json(new { success = true });
            }
            return Json(new { success = false });
        }

        [HttpPost, ActionName("Delete")]
        public ActionResult Delete(int id)
        {
            try
            {
                Examination examination = db.Examinations.Find(id);
                examination.IsDeleted = true;
                examination.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                db.Entry(examination).State = EntityState.Modified;
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "حذف الكشوفات",
                    EnAction = "AddEdit",
                    ControllerName = "Examination",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id
                });
                ////-------------------- Notification-------------------------////
                Notification.GetNotification("Examination", "Delete", "Delete", id, null, "الكشوفات");
                return Content("true");
            }
            catch (Exception ex)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
        }

        [SkipERPAuthorize]
        public JsonResult SetDocNum()
        {
            double DocNo = 0;
            var code = db.Database.SqlQuery<string>($"select top(1)[DocumentNumber] from [Examination] order by [Id] desc");
            if (code.FirstOrDefault() == null)
            {
                DocNo = 0;
            }
            else
            {
                DocNo = double.Parse(code.FirstOrDefault().ToString());
            }
            return Json(DocNo + 1, JsonRequestBehavior.AllowGet);
        }
        [SkipERPAuthorize]
        public JsonResult GetPatientHistory(int? PatientId)
        {
            var History = db.Examinations.Where(a => a.IsDeleted == false && a.PatientId == PatientId).Select(a => new
            {
                ExaminationId=a.Id,
                PatientArName = a.Patient.ArName,
                DoctorArName = a.Doctor.ArName,
                Diagnosis = a.Diagnosis,
                ExaminationDate = a.ExaminationDate,
                MedicalSpecialtyArName = a.MedicalSpecialty.ArName,
                BookingDate = a.BookingDate
            });
            return Json(History, JsonRequestBehavior.AllowGet);
        } 
        //[SkipERPAuthorize]
        //public JsonResult GetPatientHistoryDetails(int? ExaminationId)
        //{
        //    var HistoryDetailes = db.Examinations.Where(a => a.IsDeleted == false && a.Id == ExaminationId).Select(a => new
        //    {
        //        ExaminationId=a.Id,
        //        PatientArName = a.Patient.ArName,
        //        DoctorArName = a.Doctor.ArName,
        //        Diagnosis = a.Diagnosis,
        //        ExaminationDate = a.ExaminationDate,
        //        MedicalSpecialtyArName = a.MedicalSpecialty.ArName,
        //        BookingDate = a.BookingDate,
        //        a.FromTime,
        //        a.ToTime,
        //        a.TurnNo,
        //        a.Price
        //    });
        //    return Json(HistoryDetailes, JsonRequestBehavior.AllowGet);
        //}
        [SkipERPAuthorize]
        public JsonResult GetSpecialtyDoctor(int? MedicalSpecialtyId)
        {
            var Doctors = db.Doctors.Where(a => a.IsDeleted == false && a.IsActive == true && a.MedicalSpecialtyId == MedicalSpecialtyId).Select(a => new
            {
                Id = a.Id,
                ArName = a.Code + "- " + a.ArName
            });
            return Json(Doctors, JsonRequestBehavior.AllowGet);
        }
        [SkipERPAuthorize]
        public JsonResult GetPatients()
        {
            var Patients = db.Patients.Where(a => a.IsDeleted == false && a.IsActive == true).Select(a => new
            {
                a.Id,
                a.Code,
                a.ArName,
                a.Address,
                a.BirthDate,
                a.BloodGroup,
                a.ChronicDiseaseId,
                ChronicDiseaseName = a.ChronicDiseaseId == 1 ? "ضغط" : "سكر",
                a.Mobile,
            });
            return Json(Patients, JsonRequestBehavior.AllowGet);
        }
        [SkipERPAuthorize]
        public JsonResult GetTurnNo(DateTime? ExaminationDate, int? DoctorId)
        {
            var Num = db.Examinations.Where(a => a.IsDeleted == false && (a.ExaminationDate.Value.Year == ExaminationDate.Value.Year && a.ExaminationDate.Value.Month == ExaminationDate.Value.Month && a.ExaminationDate.Value.Day == ExaminationDate.Value.Day) && a.DoctorId == DoctorId).OrderByDescending(a => a.Id).FirstOrDefault();
            var TurnNo = Num != null && Num.TurnNo != null ? (Num.TurnNo + 1) : 1;
            return Json(TurnNo, JsonRequestBehavior.AllowGet);
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