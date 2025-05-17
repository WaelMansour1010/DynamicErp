using MyERP.Models;
using MyERP.Repository;
using System;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;

namespace MyERP.Controllers.SystemSettings
{
    public class FieldsCodingController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        // GET: FieldsCoding
        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة تكويد الحقول",
                EnAction = "Index",
                ControllerName = "FieldsCoding",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            ////-------------------- Notification-------------------------////
            Notification.GetNotification("FieldsCoding", "View", "Index", null, null, "تكويد الحقول");
            //////////-----------------------------------------------------------------------
            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;

            IQueryable<FieldsCoding> fieldsCodings;

            if (string.IsNullOrEmpty(searchWord))
            {
                fieldsCodings = db.FieldsCodings.Where(s => s.IsDeleted == false).OrderBy(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.FieldsCodings.Where(s => s.IsDeleted == false).Count();

            }
            else
            {
                fieldsCodings = db.FieldsCodings.Where(s => s.IsDeleted == false && (s.Code.Contains(searchWord) || s.ArName.Contains(searchWord) || s.EnName.Contains(searchWord) || s.SystemPage.ArName.Contains(searchWord) || s.SystemPage.EnName.Contains(searchWord) || s.SystemPage.ControllerName.Contains(searchWord) || s.Notes.Contains(searchWord))).OrderBy(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.FieldsCodings.Where(s => s.IsDeleted == false && (s.Code.Contains(searchWord) || s.ArName.Contains(searchWord) || s.EnName.Contains(searchWord) || s.SystemPage.ArName.Contains(searchWord) || s.SystemPage.EnName.Contains(searchWord) || s.SystemPage.ControllerName.Contains(searchWord) || s.Notes.Contains(searchWord))).Count();
            }
            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            return View(fieldsCodings.ToList());
        }

        public ActionResult AddEdit(int? id)
        {
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            if (id == null)
            {
                ViewBag.PageId = new SelectList(db.SystemPages.Where(a => a.IsActive == true && a.IsDeleted == false && a.IsMasterFile == true).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.ArName
                }), "Id", "ArName");
                return View();
            }
            FieldsCoding fieldsCoding = db.FieldsCodings.Find(id);
            if (fieldsCoding == null)
            {
                return HttpNotFound();
            }
            ViewBag.Next = QueryHelper.Next((int)id, "FieldsCoding");
            ViewBag.Previous = QueryHelper.Previous((int)id, "FieldsCoding");
            ViewBag.Last = QueryHelper.GetLast("FieldsCoding");
            ViewBag.First = QueryHelper.GetFirst("FieldsCoding");

            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح تفاصيل  تكويد الحقول",
                EnAction = "AddEdit",
                ControllerName = "FieldsCoding",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = fieldsCoding.Id,
                ArItemName = fieldsCoding.ArName,
                EnItemName = fieldsCoding.EnName,
                CodeOrDocNo = fieldsCoding.Code
            });
            ViewBag.PageId = new SelectList(db.SystemPages.Where(a => a.IsActive == true && a.IsDeleted == false && a.IsMasterFile == true).Select(b => new
            {
                Id = b.Id,
                ArName = b.ArName
            }), "Id", "ArName", fieldsCoding.PageId);
            return View(fieldsCoding);
        }

        [HttpPost]
        public ActionResult AddEdit(FieldsCoding fieldsCoding)
        {
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            if (ModelState.IsValid)
            {
                fieldsCoding.IsDeleted = false;

                if (fieldsCoding.Id > 0)
                {
                    fieldsCoding.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    fieldsCoding.IsActive = true;


                    db.Entry(fieldsCoding).State = EntityState.Modified;
                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("FieldsCoding", "Edit", "AddEdit", fieldsCoding.Id, null, "تكويد الحقول");
                }
                else
                {
                    fieldsCoding.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    fieldsCoding.IsActive = true;
                    db.FieldsCodings.Add(fieldsCoding);

                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("FieldsCoding", "Add", "AddEdit", fieldsCoding.Id, null, "تكويد الحقول");
                }
                db.SaveChanges();

                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = fieldsCoding.Id > 0 ? "تعديل تكويد الحقول" : "اضافة  تكويد الحقول",
                    EnAction = "AddEdit",
                    ControllerName = "FieldsCoding",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "Post",
                    SelectedItem = fieldsCoding.Id,
                    ArItemName = fieldsCoding.ArName,
                    EnItemName = fieldsCoding.EnName,
                    CodeOrDocNo = fieldsCoding.Code
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
                FieldsCoding fieldsCoding = db.FieldsCodings.Find(id);
                if (fieldsCoding.IsActive == true)
                {
                    fieldsCoding.IsActive = false;
                }
                else
                {
                    fieldsCoding.IsActive = true;
                }
                fieldsCoding.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                db.Entry(fieldsCoding).State = EntityState.Modified;

                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = fieldsCoding.Id > 0 ? "تنشيط تكويد الحقول" : "إلغاء تنشيط تكويد الحقول",
                    EnAction = "AddEdit",
                    ControllerName = "FieldsCoding",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "Post",
                    SelectedItem = fieldsCoding.Id,
                    ArItemName = fieldsCoding.ArName,
                    EnItemName = fieldsCoding.EnName,
                    CodeOrDocNo = fieldsCoding.Code
                });
                ////-------------------- Notification-------------------------////
                if (fieldsCoding.IsActive == true)
                {
                    Notification.GetNotification("FieldsCoding", "Activate/Deactivate", "ActivateDeactivate", id, true, "تكويد الحقول");
                }
                else
                {

                    Notification.GetNotification("FieldsCoding", "Activate/Deactivate", "ActivateDeactivate", id, false, "تكويد الحقول");
                }
                ///////-----------------------------------------------------------------------


                return Content("true");
            }
            catch (Exception ex)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
        }
        // POST: FieldsCoding/Delete/5
        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                FieldsCoding fieldsCoding = db.FieldsCodings.Find(id);
                fieldsCoding.IsDeleted = true;
                fieldsCoding.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                db.Entry(fieldsCoding).State = EntityState.Modified;

                db.SaveChanges();

                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "حذف تكويد الحقول",
                    EnAction = "AddEdit",
                    ControllerName = "FieldsCoding",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    EnItemName = fieldsCoding.EnName,
                    ArItemName = fieldsCoding.ArName,
                    CodeOrDocNo = fieldsCoding.Code
                });
                ////-------------------- Notification-------------------------////
                Notification.GetNotification("FieldsCoding", "Delete", "Delete", id, null, "تكويد الحقول");

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
            var code = QueryHelper.CodeLastNum("FieldsCoding");
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