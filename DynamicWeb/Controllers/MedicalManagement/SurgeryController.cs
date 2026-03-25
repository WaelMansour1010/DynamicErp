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
using System.Text;
using System.Security.Cryptography;

namespace MyERP.Controllers.MedicalManagement
{
    public class SurgeryController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        // GET: Surgery
        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            string domainName = Request.Url.GetLeftPart(UriPartial.Authority);
            ViewBag.domainName = domainName;

            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة العمليات الجراحية",
                EnAction = "Index",
                ControllerName = "Surgery",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            ////-------------------- Notification-------------------------////
            Notification.GetNotification("Surgery", "View", "Index", null, null, "العمليات الجراحية");

            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;

            IQueryable<Surgery> surgeries;

            if (string.IsNullOrEmpty(searchWord))
            {
                surgeries = db.Surgeries.Where(s => s.IsDeleted == false).OrderByDescending(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.Surgeries.Where(s => s.IsDeleted == false).Count();
            }
            else
            {
                surgeries = db.Surgeries.Where(s => s.IsDeleted == false && (s.DocumentNumber.Contains(searchWord) || s.Doctor.ArName.Contains(searchWord) || s.Patient.ArName.Contains(searchWord))).OrderByDescending(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.Surgeries.Where(s => s.IsDeleted == false && (s.DocumentNumber.Contains(searchWord) || s.Doctor.ArName.Contains(searchWord) || s.Patient.ArName.Contains(searchWord))).Count();
            }

            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة العمليات الجراحية",
                EnAction = "Index",
                ControllerName = "Surgery",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            return View(surgeries.ToList());
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
                ViewBag.AssistantDoctorId = new SelectList(db.Doctors.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                ViewBag.AssistantNurseId = new SelectList(db.Employees.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                ViewBag.InsuranceCompanyId = new SelectList(db.Customers.Where(a => a.IsActive == true && a.IsDeleted == false && a.InsuranceCompany == true).Select(b => new
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
                ViewBag.SurgeryDate = cTime.ToString("yyyy-MM-ddTHH:mm");
                return View();
            }
            //--------------------------------------------------------------------------------------------//
            Surgery surgery = db.Surgeries.Find(id);

            ViewBag.DoctorId = new SelectList(db.Doctors.Where(a => a.IsActive == true && a.IsDeleted == false && a.MedicalSpecialtyId == surgery.MedicalSpecialtyId).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", surgery.DoctorId);
            ViewBag.MedicalSpecialtyId = new SelectList(db.MedicalSpecialties.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", surgery.MedicalSpecialtyId);
            ViewBag.PatientId = new SelectList(db.Patients.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", surgery.PatientId);
            ViewBag.AssistantDoctorId = new SelectList(db.Doctors.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", surgery.AssistantDoctorId);
            ViewBag.AssistantNurseId = new SelectList(db.Employees.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", surgery.AssistantNurseId);
            ViewBag.InsuranceCompanyId = new SelectList(db.Customers.Where(a => a.IsActive == true && a.IsDeleted == false&&a.InsuranceCompany==true).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", surgery.InsuranceCompanyId);

            if (surgery == null)
            {
                return HttpNotFound();
            }
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "اضافة او تعديل العمليات الجراحية ",
                EnAction = "AddEdit",
                ControllerName = "Surgery",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = id
            });

            ViewBag.BookingDate = surgery.BookingDate != null ? surgery.BookingDate.Value.ToString("yyyy-MM-ddTHH:mm") : null;
            ViewBag.SurgeryDate = surgery.SurgeryDate != null ? surgery.SurgeryDate.Value.ToString("yyyy-MM-ddTHH:mm") : null;

            ViewBag.Next = QueryHelper.Next((int)id, "Surgery");
            ViewBag.Previous = QueryHelper.Previous((int)id, "Surgery");
            ViewBag.Last = QueryHelper.GetLast("Surgery");
            ViewBag.First = QueryHelper.GetFirst("Surgery");
            return View(surgery);
        }

        [HttpPost]
        public ActionResult AddEdit(Surgery surgery)
        {
            if (ModelState.IsValid)
            {
                var id = surgery.Id;
                surgery.IsDeleted = false;

                surgery.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                string domainName = Request.Url.GetLeftPart(UriPartial.Authority);
                var bytes = new byte[7000];
                string fileName = "";
                var SurgeryRaysImageList = new List<SurgeryRayImage>();
                var SurgeryAnalysisImageList = new List<SurgeryAnalysisImage>();
                List<SurgeryImage> surgeryImageList = db.SurgeryImages.Where(i => i.MainDocId == surgery.Id).ToList();

                if (surgery.Id > 0)
                {
                    //-- Surgery Images --//
                    if (surgery.SurgeryImages != null)
                    {  
                        var lastSurgeryImage = db.SurgeryImages.OrderByDescending(a => a.Id).FirstOrDefault();
                        int LastItemID = (lastSurgeryImage != null ? lastSurgeryImage.Id : 0) + 1;

                        foreach (var img in surgery.SurgeryImages)
                        {
                            if (img != null && img.Image.Contains("base64"))
                            {
                                fileName = "/images/MedicalManagement/Surgery/SurgeryImages" + LastItemID.ToString() + ".jpeg";
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
                                surgeryImageList.Add(new SurgeryImage()
                                {
                                    Image = domainName + fileName,
                                    MainDocId = surgery.Id
                                });
                                LastItemID++;
                            }
                            else //previous images
                            {
                                surgeryImageList.Add(new SurgeryImage()
                                {
                                    Image = img.Image,
                                    MainDocId = surgery.Id
                                });
                            }
                        }                        
                    }
                    //--------------------------------------------------------------//
                    if (surgery.SurgeryRayImages != null)
                    {
                        var lastRayImage = db.SurgeryRayImages.OrderByDescending(a => a.Id).FirstOrDefault();
                        int LastItemID = (lastRayImage != null ? lastRayImage.Id : 0) + 1;

                        foreach (var img in surgery.SurgeryRayImages)
                        {

                            if (img != null && img.Image.Contains("base64"))
                            {
                                fileName = "/images/MedicalManagement/Surgery/SurgeryRays" + LastItemID.ToString() + ".jpeg";
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
                                SurgeryRaysImageList.Add(new SurgeryRayImage()
                                {
                                    Image = domainName + fileName,
                                    MainDocId = surgery.Id
                                });
                                LastItemID++;
                            }
                            else //previous images
                            {
                                SurgeryRaysImageList.Add(new SurgeryRayImage()
                                {
                                    Image = img.Image,
                                    MainDocId = surgery.Id
                                });
                            }
                        }
                    }

                    if (surgery.SurgeryAnalysisImages != null)
                    {
                        var lastAnalysisImage = db.SurgeryAnalysisImages.OrderByDescending(a => a.Id).FirstOrDefault();
                        int LastItemID = (lastAnalysisImage != null ? lastAnalysisImage.Id : 0) + 1;

                        foreach (var img in surgery.SurgeryAnalysisImages)
                        {

                            if (img != null && img.Image.Contains("base64"))
                            {
                                fileName = "/images/MedicalManagement/Surgery/SurgeryAnalysis" + LastItemID.ToString() + ".jpeg";
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
                                SurgeryAnalysisImageList.Add(new SurgeryAnalysisImage()
                                {
                                    Image = domainName + fileName,
                                    MainDocId = surgery.Id
                                });
                                LastItemID++;
                            }
                            else //previous images
                            {
                                SurgeryAnalysisImageList.Add(new SurgeryAnalysisImage()
                                {
                                    Image = img.Image,
                                    MainDocId = surgery.Id
                                });
                            }
                        }
                    }

                    db.SurgeryAnalysis.RemoveRange(db.SurgeryAnalysis.Where(x => x.MainDocId == surgery.Id));
                    var surgeryAnalysis = surgery.SurgeryAnalysis.ToList();
                    surgeryAnalysis.ForEach((x) => x.MainDocId = surgery.Id);
                    surgery.SurgeryAnalysis = null;

                    db.SurgeryAnalysisImages.RemoveRange(db.SurgeryAnalysisImages.Where(x => x.MainDocId == surgery.Id));
                    var surgeryAnalysisImages = surgery.SurgeryAnalysisImages.ToList();
                    surgeryAnalysisImages.ForEach((x) => x.MainDocId = surgery.Id);
                    surgery.SurgeryAnalysisImages = null;

                    db.SurgeryMedicines.RemoveRange(db.SurgeryMedicines.Where(x => x.MainDocId == surgery.Id));
                    var surgeryMedicines = surgery.SurgeryMedicines.ToList();
                    surgeryMedicines.ForEach((x) => x.MainDocId = surgery.Id);
                    surgery.SurgeryMedicines = null;

                    db.SurgeryRays.RemoveRange(db.SurgeryRays.Where(x => x.MainDocId == surgery.Id));
                    var surgeryRays = surgery.SurgeryRays.ToList();
                    surgeryRays.ForEach((x) => x.MainDocId = surgery.Id);
                    surgery.SurgeryRays = null;

                    db.SurgeryRayImages.RemoveRange(db.SurgeryRayImages.Where(x => x.MainDocId == surgery.Id));
                    var surgeryRayImages = surgery.SurgeryRayImages.ToList();
                    surgeryRayImages.ForEach((x) => x.MainDocId = surgery.Id);
                    surgery.SurgeryRayImages = null;

                    db.SurgeryImages.RemoveRange(db.SurgeryImages.Where(x => x.MainDocId == surgery.Id));
                    var SurgeryImages = surgery.SurgeryImages.ToList();
                    SurgeryImages.ForEach((x) => x.MainDocId = surgery.Id);
                    surgery.SurgeryImages = null;

                    db.Entry(surgery).State = EntityState.Modified;
                    db.SurgeryAnalysis.AddRange(surgeryAnalysis);
                    db.SurgeryAnalysisImages.AddRange(surgeryAnalysisImages);
                    db.SurgeryMedicines.AddRange(surgeryMedicines);
                    db.SurgeryRays.AddRange(surgeryRays);
                    db.SurgeryRayImages.AddRange(surgeryRayImages);
                    db.SurgeryImages.AddRange(SurgeryImages);

                    Notification.GetNotification("Surgery", "Edit", "AddEdit", surgery.Id, null, "العمليات الجراحية");
                }
                else
                {
                    //-- Surgery Images --//
                    if (surgery.SurgeryImages != null)
                    {
                        var lastSurgeryImage = db.SurgeryImages.OrderByDescending(a => a.Id).FirstOrDefault();
                        int LastItemID = (lastSurgeryImage != null ? lastSurgeryImage.Id : 0) + 1;

                        foreach (var img in surgery.SurgeryImages)
                        {

                            if (img != null && img.Image.Contains("base64"))
                            {
                                fileName = "/images/MedicalManagement/Surgery/SurgeryImages" + LastItemID.ToString() + ".jpeg";
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
                                surgeryImageList.Add(new SurgeryImage()
                                {
                                    Image = domainName + fileName,
                                    MainDocId = surgery.Id
                                });
                                LastItemID++;
                            } 
                        }
                    }
                    //--------------------------------------------------------------//

                    if (surgery.SurgeryRayImages != null)
                    {
                        var lastRayImage = db.SurgeryRayImages.OrderByDescending(a => a.Id).FirstOrDefault();
                        int LastItemID = (lastRayImage != null ? lastRayImage.Id : 0) + 1;

                        foreach (var img in surgery.SurgeryRayImages)
                        {
                            if (img != null && img.Image.Contains("base64"))
                            {
                                fileName = "/images/MedicalManagement/Surgery/SurgeryRays" + LastItemID.ToString() + ".jpeg";
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
                                SurgeryRaysImageList.Add(new SurgeryRayImage()
                                {
                                    Image = domainName + fileName,
                                    MainDocId = LastItemID
                                });
                            }
                            LastItemID++;
                        }
                    }

                    if (surgery.SurgeryAnalysisImages != null)
                    {

                        var lastAnalysisImage = db.SurgeryAnalysisImages.OrderByDescending(a => a.Id).FirstOrDefault();
                        int LastItemID = (lastAnalysisImage != null ? lastAnalysisImage.Id : 0) + 1;

                        foreach (var img in surgery.SurgeryAnalysisImages)
                        {

                            if (img != null && img.Image.Contains("base64"))
                            {
                                fileName = "/images/MedicalManagement/Surgery/SurgeryAnalysis" + LastItemID.ToString() + ".jpeg";
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
                                SurgeryAnalysisImageList.Add(new SurgeryAnalysisImage()
                                {
                                    Image = domainName + fileName,
                                    MainDocId = surgery.Id
                                });
                                LastItemID++;
                            }
                        }
                    }

                    surgery.DocumentNumber = new JavaScriptSerializer().Serialize(SetDocNum().Data).ToString().Trim('"');
                    db.Surgeries.Add(surgery);
                    Notification.GetNotification("Surgery", "Add", "AddEdit", surgery.Id, null, "العمليات الجراحية");
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
                    ArAction = id > 0 ? "تعديل العمليات الجراحية" : "اضافة العمليات الجراحية",
                    EnAction = "AddEdit",
                    ControllerName = "Surgery",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    CodeOrDocNo = surgery.DocumentNumber
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
                Surgery surgery = db.Surgeries.Find(id);
                surgery.IsDeleted = true;
                surgery.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                db.Entry(surgery).State = EntityState.Modified;
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "حذف العمليات الجراحية",
                    EnAction = "AddEdit",
                    ControllerName = "Surgery",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id
                });
                ////-------------------- Notification-------------------------////
                Notification.GetNotification("Surgery", "Delete", "Delete", id, null, "العمليات الجراحية");
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
            var code = db.Database.SqlQuery<string>($"select top(1)[DocumentNumber] from [Surgery] order by [Id] desc");
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
        public JsonResult GetInsuranceCompanyPercentage(int? InsuranceCompanyId)
        {
            var InsuranceCompanyPercentage = db.Customers.Where(a => a.IsDeleted == false && a.IsActive == true && a.Id == InsuranceCompanyId).FirstOrDefault().InsuranceCompanyPercentage;
            
            return Json(InsuranceCompanyPercentage, JsonRequestBehavior.AllowGet);
        }

        //[AllowAnonymous]
        //public ActionResult EncodeId(int id)
        //{
        //    try
        //    {
        //        byte[] inputByteArray = Encoding.UTF8.GetBytes(id.ToString());
        //        byte[] rgbIV = { 0x21, 0x43, 0x56, 0x87, 0x10, 0xfd, 0xea, 0x1c };
        //        byte[] key = { };
        //        key = Encoding.UTF8.GetBytes("Z4a2rX3T");
        //        DESCryptoServiceProvider des = new DESCryptoServiceProvider();
        //        MemoryStream ms = new MemoryStream();
        //        CryptoStream cs = new CryptoStream(ms, des.CreateEncryptor(key, rgbIV), CryptoStreamMode.Write);
        //        cs.Write(inputByteArray, 0, inputByteArray.Length);
        //        cs.FlushFinalBlock();
        //        var str = Convert.ToBase64String(ms.ToArray());//.Replace("+", "_pl_").Replace("=", "_eq_").Replace("/", "_sl_").Replace(@"\", "_bsl_");
        //        return Json(str, JsonRequestBehavior.AllowGet);
        //    }
        //    catch
        //    {
        //        return null;
        //    }
        //}
        [AllowAnonymous]
        public ActionResult AddEditCalendar()
        {
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