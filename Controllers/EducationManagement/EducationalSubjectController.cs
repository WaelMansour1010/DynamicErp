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
    public class EducationalSubjectController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        // GET: EducationalSubject
        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة المناهج الدراسية",
                EnAction = "Index",
                ControllerName = "EducationalSubject",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            Notification.GetNotification("EducationalSubject", "View", "Index", null, null, "المناهج الدراسية");
            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;
            if (pageIndex > 1)
            {
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;
            }
            IQueryable<EducationalSubject> educationalSubjects;
            if (string.IsNullOrEmpty(searchWord))
            {
                educationalSubjects = db.EducationalSubjects.Where(a => a.IsDeleted == false).OrderBy(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.EducationalSubjects.Where(a => a.IsDeleted == false).Count();
            }
            else
            {
                educationalSubjects = db.EducationalSubjects.Where(a => a.IsDeleted == false &&
                (a.ArName.Contains(searchWord) || a.EnName.Contains(searchWord) || a.Code.Contains(searchWord)))
                    .OrderBy(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.EducationalSubjects.Where(a => a.IsDeleted == false && (a.ArName.Contains(searchWord) || a.EnName.Contains(searchWord) || a.Code.Contains(searchWord))).Count();
            }
            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            return View(educationalSubjects.ToList());
        }
        public ActionResult AddEdit(int? id)
        {
            if (id == null)
            {
                ViewBag.AdministratorId = new SelectList(db.Employees.Where(a=>a.IsActive==true&&a.IsDeleted==false).Select(a=>new {a.Id,ArName=a.Code+" - "+a.ArName }), "Id", "ArName");
                return View();
            }
            EducationalSubject educationalSubject = db.EducationalSubjects.Find(id);
            if (educationalSubject == null)
            {
                return HttpNotFound();
            }
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "اضافة او تعديل المناهج الدراسية ",
                EnAction = "AddEdit",
                ControllerName = "EducationalSubject",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = id
            });
            ViewBag.Next = QueryHelper.Next((int)id, "EducationalSubject");
            ViewBag.Previous = QueryHelper.Previous((int)id, "EducationalSubject");
            ViewBag.Last = QueryHelper.GetLast("EducationalSubject");
            ViewBag.First = QueryHelper.GetFirst("EducationalSubject");

            ViewBag.AdministratorId = new SelectList(db.Employees.Where(a => a.IsActive == true && a.IsDeleted == false).Select(a => new { a.Id, ArName = a.Code + " - " + a.ArName }), "Id", "ArName",educationalSubject.AdministratorId);
            return View(educationalSubject);
        }
        [HttpPost]
        public ActionResult AddEdit(EducationalSubject educationalSubject)
        {
            if (ModelState.IsValid)
            {
                var id = educationalSubject.Id;
                educationalSubject.IsDeleted = false;
                educationalSubject.IsActive = true;
                educationalSubject.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                string domainName = Request.Url.GetLeftPart(UriPartial.Authority);
                if (educationalSubject.Id > 0)
                {
                    //---------------- Image -----------------//
                    if (educationalSubject.Image != null && educationalSubject.Image.Contains("base64"))
                    {
                        string fileName = "";
                        fileName = "/images/EducationalSubject/" + educationalSubject.Code + "-" + educationalSubject.ArName + ".jpeg";
                        //to Check If Image Name Exist before
                        var file = Server.MapPath("/images/EducationalSubject/" + educationalSubject.Code + "-" + educationalSubject.ArName + ".jpeg").Replace('\\', '/');
                        //if (System.IO.File.Exists(file))
                        if (System.IO.File.Exists(file))
                        {
                            System.IO.File.Delete(file);
                        }
                        var bytes = new byte[7000];
                        if (educationalSubject.Image.Contains("jpeg"))
                        {
                            bytes = Convert.FromBase64String(educationalSubject.Image.Replace("data:image/jpeg;base64,", ""));
                        }
                        else
                        {
                            bytes = Convert.FromBase64String(educationalSubject.Image.Replace("data:image/png;base64,", ""));
                        }
                        using (var imageFile = new FileStream(Server.MapPath(fileName), FileMode.Create))
                        {
                            imageFile.Write(bytes, 0, bytes.Length);
                            imageFile.Flush();
                        }
                        educationalSubject.Image = domainName + fileName;
                    }
                    else
                    {
                        educationalSubject.Image = educationalSubject.Image;
                    }

                    db.Entry(educationalSubject).State = EntityState.Modified;
                    Notification.GetNotification("EducationalSubject", "Edit", "AddEdit", educationalSubject.Id, null, "المناهج الدراسية");
                }
                else
                {
                    //---------------- Image -----------------//
                    if (educationalSubject.Image != null && educationalSubject.Image.Contains("base64"))
                    {
                        string fileName = "";
                        fileName = "/images/EducationalSubject/" + educationalSubject.Code + "-" + educationalSubject.ArName + ".jpeg";
                        var bytes = new byte[7000];
                        if (educationalSubject.Image.Contains("jpeg"))
                        {
                            bytes = Convert.FromBase64String(educationalSubject.Image.Replace("data:image/jpeg;base64,", ""));
                        }
                        else
                        {
                            bytes = Convert.FromBase64String(educationalSubject.Image.Replace("data:image/png;base64,", ""));
                        }
                        using (var imageFile = new FileStream(Server.MapPath(fileName), FileMode.Create))
                        {
                            imageFile.Write(bytes, 0, bytes.Length);
                            imageFile.Flush();
                        }
                        educationalSubject.Image = domainName + fileName;
                    }
                    educationalSubject.Code = (QueryHelper.CodeLastNum("EducationalSubject") + 1).ToString();
                    db.EducationalSubjects.Add(educationalSubject);
                    Notification.GetNotification("EducationalSubject", "Add", "AddEdit", educationalSubject.Id, null, "المناهج الدراسية");
                }
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = id > 0 ? "تعديل المناهج الدراسية" : "اضافة المناهج الدراسية",
                    EnAction = "AddEdit",
                    ControllerName = "EducationalSubject",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    CodeOrDocNo = educationalSubject.Code
                });
                return Json(new { success = true });
            }
            var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .Select(x => new { x.Key, x.Value.Errors })
                    .ToArray();
            return Json(new { success = false });
        }

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                EducationalSubject educationalSubject = db.EducationalSubjects.Find(id);
                educationalSubject.IsDeleted = true;
                educationalSubject.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                db.Entry(educationalSubject).State = EntityState.Modified;
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "حذف المناهج الدراسية",
                    EnAction = "AddEdit",
                    ControllerName = "EducationalSubject",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    EnItemName = educationalSubject.EnName
                });
                Notification.GetNotification("EducationalSubject", "Delete", "Delete", id, null, "المناهج الدراسية");
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
                EducationalSubject educationalSubject = db.EducationalSubjects.Find(id);
                if (educationalSubject.IsActive == true)
                {
                    educationalSubject.IsActive = false;
                }
                else
                {
                    educationalSubject.IsActive = true;
                }
                educationalSubject.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                db.Entry(educationalSubject).State = EntityState.Modified;
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = (bool)educationalSubject.IsActive ? "تنشيط المناهج الدراسية" : "إلغاء تنشيط المناهج الدراسية",
                    EnAction = "AddEdit",
                    ControllerName = "EducationalSubject",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = educationalSubject.Id,
                    EnItemName = educationalSubject.EnName,
                    ArItemName = educationalSubject.ArName,
                    CodeOrDocNo = educationalSubject.Code
                });
                if (educationalSubject.IsActive == true)
                {
                    Notification.GetNotification("EducationalSubject", "Activate/Deactivate", "ActivateDeactivate", id, true, "المناهج الدراسية");
                }
                else
                {
                    Notification.GetNotification("EducationalSubject", "Activate/Deactivate", "ActivateDeactivate", id, false, " المناهج الدراسية");
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
            var code = QueryHelper.CodeLastNum("EducationalSubject");
            return Json(code + 1, JsonRequestBehavior.AllowGet);
        }
       
        [SkipERPAuthorize]
        public JsonResult GetLectureDetails(int? Id)
        {
            var LectureDetails = db.Lessons.Where(a=>a.IsActive==true&&a.IsDeleted==false&&a.EducationalSubjectId==Id).Select(a=>new {a.LecturerName,a.ArName,a.PDFLink,a.PowerPointLink,a.VideoLink }).ToList();
            return Json(LectureDetails , JsonRequestBehavior.AllowGet);
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