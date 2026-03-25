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
    public class LessonController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        // GET: Lesson
        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة الدروس",
                EnAction = "Index",
                ControllerName = "Lesson",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            Notification.GetNotification("Lesson", "View", "Index", null, null, "الدروس");
            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;
            if (pageIndex > 1)
            {
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;
            }
            IQueryable<Lesson> lessons;
            if (string.IsNullOrEmpty(searchWord))
            {
                lessons = db.Lessons.Where(a => a.IsDeleted == false).OrderBy(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.Lessons.Where(a => a.IsDeleted == false).Count();
            }
            else
            {
                lessons = db.Lessons.Where(a => a.IsDeleted == false &&
                (a.ArName.Contains(searchWord) || a.EnName.Contains(searchWord) || a.Code.Contains(searchWord)))
                    .OrderBy(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.Lessons.Where(a => a.IsDeleted == false && (a.ArName.Contains(searchWord) || a.EnName.Contains(searchWord) || a.Code.Contains(searchWord))).Count();
            }
            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            return View(lessons.ToList());
        }
        public ActionResult AddEdit(int? id)
        {
            if (id == null)
            {
                ViewBag.EducationalSubjectId = new SelectList(db.EducationalSubjects.Where(a => a.IsActive == true && a.IsDeleted == false).Select(a => new { a.Id, ArName = a.Code + " - " + a.ArName }), "Id", "ArName");

                return View();
            }
            Lesson lesson = db.Lessons.Find(id);
            if (lesson == null)
            {
                return HttpNotFound();
            }
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "اضافة او تعديل الدروس ",
                EnAction = "AddEdit",
                ControllerName = "Lesson",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = id
            });
            ViewBag.Next = QueryHelper.Next((int)id, "Lesson");
            ViewBag.Previous = QueryHelper.Previous((int)id, "Lesson");
            ViewBag.Last = QueryHelper.GetLast("Lesson");
            ViewBag.First = QueryHelper.GetFirst("Lesson");

            ViewBag.EducationalSubjectId = new SelectList(db.EducationalSubjects.Where(a => a.IsActive == true && a.IsDeleted == false).Select(
                a => new { a.Id, ArName = a.Code + " - " + a.ArName }), "Id", "ArName", lesson.EducationalSubjectId);


            return View(lesson);
        }
        [HttpPost]
        public ActionResult AddEdit(Lesson lesson)
        {
            if (ModelState.IsValid)
            {
                var id = lesson.Id;
                lesson.IsDeleted = false;
                lesson.IsActive = true;
                lesson.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                string domainName = Request.Url.GetLeftPart(UriPartial.Authority);
                var bytes = new byte[7000];
                string fileName = "";
                var LessonImageList = new List<LessonImage>();
                if (lesson.Id > 0)
                {
                    if (lesson.LessonImages != null)
                    {
                        var lastImage = db.LessonImages.OrderByDescending(a => a.Id).FirstOrDefault();
                        int LastItemID = (lastImage != null ? lastImage.Id : 0) + 1;

                        foreach (var img in lesson.LessonImages)
                        {

                            if (img != null && img.Image.Contains("base64"))
                            {
                                fileName = "/Lesson" + LastItemID.ToString() /*+ "_" + roomType.ArName*/ + ".jpeg";
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
                                LessonImageList.Add(new LessonImage()
                                {
                                    Image = domainName + fileName,
                                    LessonId = lesson.Id
                                });
                                LastItemID++;
                            }
                            else //previous images
                            {
                                LessonImageList.Add(new LessonImage()
                                {
                                    Image = img.Image,
                                    LessonId = lesson.Id
                                });
                            }
                        }
                    }

                    db.LessonImages.RemoveRange(db.LessonImages.Where(x => x.LessonId == lesson.Id));
                    var lessonImages = lesson.LessonImages.ToList();
                    lessonImages.ForEach((x) => x.LessonId = lesson.Id);
                    lesson.LessonImages = null;

                    db.Entry(lesson).State = EntityState.Modified;
                    db.LessonImages.AddRange(lessonImages);



                    Notification.GetNotification("Lesson", "Edit", "AddEdit", lesson.Id, null, "الدروس");
                }
                else
                {
                    if (lesson.LessonImages != null)
                    {
                        var lastImage = db.LessonImages.OrderByDescending(a => a.Id).FirstOrDefault();
                        int LastItemID = (lastImage != null ? lastImage.Id : 0) + 1;

                        foreach (var img in lesson.LessonImages)
                        {
                            if (img != null && img.Image.Contains("base64"))
                            {
                                fileName = "/Lesson" + LastItemID.ToString() /*+ "_" + roomType.ArName*/ + ".jpeg";
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
                                LessonImageList.Add(new LessonImage()
                                {
                                    Image = domainName + fileName,
                                    LessonId = LastItemID
                                });
                            }
                            LastItemID++;
                        }

                    }

                    lesson.Code = (QueryHelper.CodeLastNum("Lesson") + 1).ToString();
                    db.Lessons.Add(lesson);
                    Notification.GetNotification("Lesson", "Add", "AddEdit", lesson.Id, null, "الدروس");
                }
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = id > 0 ? "تعديل الدروس" : "اضافة الدروس",
                    EnAction = "AddEdit",
                    ControllerName = "Lesson",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    CodeOrDocNo = lesson.Code
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
                Lesson lesson = db.Lessons.Find(id);
                lesson.IsDeleted = true;
                lesson.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                db.Entry(lesson).State = EntityState.Modified;
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "حذف الدروس",
                    EnAction = "AddEdit",
                    ControllerName = "Lesson",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    EnItemName = lesson.EnName
                });
                Notification.GetNotification("Lesson", "Delete", "Delete", id, null, "الدروس");
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
                Lesson lesson = db.Lessons.Find(id);
                if (lesson.IsActive == true)
                {
                    lesson.IsActive = false;
                }
                else
                {
                    lesson.IsActive = true;
                }
                lesson.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                db.Entry(lesson).State = EntityState.Modified;
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = (bool)lesson.IsActive ? "تنشيط الدروس" : "إلغاء تنشيط الدروس",
                    EnAction = "AddEdit",
                    ControllerName = "Lesson",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = lesson.Id,
                    EnItemName = lesson.EnName,
                    ArItemName = lesson.ArName,
                    CodeOrDocNo = lesson.Code
                });
                if (lesson.IsActive == true)
                {
                    Notification.GetNotification("Lesson", "Activate/Deactivate", "ActivateDeactivate", id, true, "الدروس");
                }
                else
                {
                    Notification.GetNotification("Lesson", "Activate/Deactivate", "ActivateDeactivate", id, false, " الدروس");
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
            var code = QueryHelper.CodeLastNum("Lesson");
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