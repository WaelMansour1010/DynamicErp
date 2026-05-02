using MyERP.Models;
using MyERP.Repository;
using System;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;

namespace MyERP.Controllers
{
    public class QualificationController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();
        // GET: Qualification
        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة المؤهلات",
                EnAction = "Index",
                ControllerName = "Qualification",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            ////-------------------- Notification-------------------------////
            Notification.GetNotification("Qualification", "View", "Index", null, null, "المؤهلات");
            //////////-----------------------------------------------------------------------
            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;

            IQueryable<Qualification> qualifications;

            if (string.IsNullOrEmpty(searchWord))
            {
                qualifications = db.Qualifications.Where(s => s.IsDeleted == false).OrderBy(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.Qualifications.Where(s => s.IsDeleted == false).Count();

            }
            else
            {
                qualifications = db.Qualifications.Where(s => s.IsDeleted == false && (s.Code.Contains(searchWord) || s.ArName.Contains(searchWord) || s.EnName.Contains(searchWord) || s.Notes.Contains(searchWord))).OrderBy(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.Qualifications.Where(s => s.IsDeleted == false && (s.Code.Contains(searchWord) || s.ArName.Contains(searchWord) || s.EnName.Contains(searchWord) || s.Notes.Contains(searchWord))).Count();
            }
            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            return View(qualifications.ToList());
        }

        public ActionResult AddEdit(int? id)
        {
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            if (id == null)
            {
                // drop down list of QualificationType
                ViewBag.QualificationTypeId = new SelectList(db.QualificationTypes.Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                return View();
            }
            Qualification qualification = db.Qualifications.Find(id);
            if (qualification == null)
            {
                return HttpNotFound();
            }
            ViewBag.Next = QueryHelper.Next((int)id, "Qualification");
            ViewBag.Previous = QueryHelper.Previous((int)id, "Qualification");
            ViewBag.Last = QueryHelper.GetLast("Qualification");
            ViewBag.First = QueryHelper.GetFirst("Qualification");

            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح تفاصيل  المؤهل",
                EnAction = "AddEdit",
                ControllerName = "Qualification",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = qualification.Id,
                ArItemName = qualification.ArName,
                EnItemName = qualification.EnName,
                CodeOrDocNo = qualification.Code
            });
            // drop down list of QualificationType
            ViewBag.QualificationTypeId = new SelectList(db.QualificationTypes.Select(b => new
            {
                Id = b.Id,
                ArName = b.Code+" - "+ b.ArName
            }), "Id", "ArName",qualification.QualificationTypeId);
            return View(qualification);
        }

        [HttpPost]
        public ActionResult AddEdit(Qualification qualification)
        {
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            if (ModelState.IsValid)
            {
                qualification.IsDeleted = false;
                if (qualification.Id > 0)
                {
                    qualification.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    qualification.IsActive = true;

                    //// use another object to prevent entity error
                    //var old = db.Qualifications.Find(qualification.Id);
                    //old.Code = qualification.Code;
                    //old.ArName = qualification.ArName;
                    //old.EnName = qualification.EnName;
                    //old.Notes = qualification.Notes;
                    //old.IsActive = qualification.IsActive;
                    //old.IsDeleted = qualification.IsDeleted;
                    //old.UserId = qualification.UserId;
                    //db.EmployeeQualifications.RemoveRange(db.EmployeeQualifications.Where(p => p.QualificationId == old.Id).ToList());
                    //foreach (var item in qualification.EmployeeQualifications)
                    //{
                    //    old.EmployeeQualifications.Add(item);
                    //}
                    
                    //db.Entry(old).State = EntityState.Modified;
                    db.Entry(qualification).State = EntityState.Modified;
                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("Qualification", "Edit", "AddEdit", qualification.Id, null, "المؤهلات");
                }
                else
                {
                    qualification.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    //qualification.Code= (QueryHelper.CodeLastNum("Qualification") + 1).ToString();
                    qualification.IsActive = true;
                    db.Qualifications.Add(qualification);

                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("Qualification", "Add", "AddEdit", qualification.Id, null, "المؤهلات");
                }
                db.SaveChanges();

                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = qualification.Id > 0 ? "تعديل مؤهل" : "اضافة  مؤهل",
                    EnAction = "AddEdit",
                    ControllerName = "Qualification",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "Post",
                    SelectedItem = qualification.Id,
                    ArItemName = qualification.ArName,
                    EnItemName = qualification.EnName,
                    CodeOrDocNo = qualification.Code
                });
                return Json(new { success = "true" });
            }

            var errors = ModelState
                     .Where(x => x.Value.Errors.Count > 0)
                     .Select(x => new { x.Key, x.Value.Errors })
                     .ToArray();

            return Json(new { success = "false", errors });
        }

        [HttpPost]
        public ActionResult ActivateDeactivate(int id)
        {
            try
            {
                Qualification qualification = db.Qualifications.Find(id);
                if (qualification.IsActive == true)
                {
                    qualification.IsActive = false;
                }
                else
                {
                    qualification.IsActive = true;
                }
                qualification.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                db.Entry(qualification).State = EntityState.Modified;

                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = qualification.Id > 0 ? "تنشيط مؤهل" : "إلغاء تنشيط مؤهل",
                    EnAction = "AddEdit",
                    ControllerName = "Qualification",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "Post",
                    SelectedItem = qualification.Id,
                    ArItemName = qualification.ArName,
                    EnItemName = qualification.EnName,
                    CodeOrDocNo = qualification.Code
                });
                ////-------------------- Notification-------------------------////
                if (qualification.IsActive == true)
                {
                    Notification.GetNotification("Qualification", "Activate/Deactivate", "ActivateDeactivate", id, true, "المؤهلات");
                }
                else
                {

                    Notification.GetNotification("Qualification", "Activate/Deactivate", "ActivateDeactivate", id, false, "المؤهلات");
                }
                ///////-----------------------------------------------------------------------


                return Content("true");
            }
            catch (Exception ex)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
        }

        // POST: Qualification/Delete/5
        [HttpPost, ActionName("Delete")]
        //[ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                Qualification qualification = db.Qualifications.Find(id);
                qualification.IsDeleted = true;
                qualification.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                db.Entry(qualification).State = EntityState.Modified;

                db.SaveChanges();

                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "حذف مؤهل",
                    EnAction = "AddEdit",
                    ControllerName = "Qualification",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    EnItemName = qualification.EnName,
                    ArItemName = qualification.ArName,
                    CodeOrDocNo = qualification.Code
                });
                ////-------------------- Notification-------------------------////
                Notification.GetNotification("Qualification", "Delete", "Delete", id, null, "المؤهلات");

                ///////-----------------------------------------------------------------------

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
            var code = QueryHelper.CodeLastNum("Qualification");
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