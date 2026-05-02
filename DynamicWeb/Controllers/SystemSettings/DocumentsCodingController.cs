using MyERP.Models;
using MyERP.Repository;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;

namespace MyERP.Controllers.SystemSettings
{
    public class DocumentsCodingController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();
        // GET: DocumentsCoding
        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة تكويد المستندات",
                EnAction = "Index",
                ControllerName = "DocumentsCoding",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            ////-------------------- Notification-------------------------////
            Notification.GetNotification("DocumentsCoding", "View", "Index", null, null, "تكويد المستندات");
            //////////-----------------------------------------------------------------------
            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;

            IQueryable<DocumentsCoding> documentsCodings;

            if (string.IsNullOrEmpty(searchWord))
            {
                documentsCodings = db.DocumentsCodings.Where(s => s.IsDeleted == false).OrderBy(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.DocumentsCodings.Where(s => s.IsDeleted == false).Count();

            }
            else
            {
                documentsCodings = db.DocumentsCodings.Where(s => s.IsDeleted == false && (s.Code.Contains(searchWord)|| s.Notes.Contains(searchWord))).OrderBy(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.DocumentsCodings.Where(s => s.IsDeleted == false && (s.Code.Contains(searchWord)|| s.Notes.Contains(searchWord))).Count();
            }
            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            return View(documentsCodings.ToList());
        }

        public ActionResult AddEdit(int? id)
        {
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            if (id == null)
            {
                ViewBag.DepartmentId = new SelectList(db.Departments.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.ArName
                }), "Id", "ArName");
                ViewBag.CodingTypeId = new SelectList(new List<dynamic>
                {
                    new { Id=1, ArName="آلي"},
                    new { Id = 2, ArName = "متصل شهري" },
                new { Id=3, ArName="متصل سنوي"}}, "Id", "ArName");
                ViewBag.YearFormat = new SelectList(new List<dynamic>
                {
                    new { Id=2, ArName="2"},
                    new { Id = 4, ArName = "4" }}, "Id", "ArName");
                return View();
            }
            DocumentsCoding documentsCoding = db.DocumentsCodings.Find(id);
            if (documentsCoding == null)
            {
                return HttpNotFound();
            }
            ViewBag.Next = QueryHelper.Next((int)id, "DocumentsCoding");
            ViewBag.Previous = QueryHelper.Previous((int)id, "DocumentsCoding");
            ViewBag.Last = QueryHelper.GetLast("DocumentsCoding");
            ViewBag.First = QueryHelper.GetFirst("DocumentsCoding");

            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح تفاصيل  تكويد المستندات",
                EnAction = "AddEdit",
                ControllerName = "DocumentsCoding",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = documentsCoding.Id,
                CodeOrDocNo = documentsCoding.Code
            });
            ViewBag.DepartmentId = new SelectList(db.Departments.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
            {
                Id = b.Id,
                ArName = b.ArName
            }), "Id", "ArName",documentsCoding.DepartmentId);
            ViewBag.CodingTypeId = new SelectList(new List<dynamic>
                {
                    new { Id=1, ArName="آلي"},
                    new { Id = 2, ArName = "متصل شهري" },
                new { Id=3, ArName="متصل سنوي"}}, "Id", "ArName",documentsCoding.CodingTypeId);
            ViewBag.YearFormat = new SelectList(new List<dynamic>
                {
                    new { Id=2, ArName="2"},
                    new { Id = 4, ArName = "4" }}, "Id", "ArName",documentsCoding.YearFormat);
            return View(documentsCoding);
        }

        [HttpPost]
        public ActionResult AddEdit(DocumentsCoding documentsCoding)
        {
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            if (ModelState.IsValid)
            {
                documentsCoding.IsDeleted = false;

                if (documentsCoding.Id > 0)
                {
                    documentsCoding.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    documentsCoding.IsActive = true;


                    db.Entry(documentsCoding).State = EntityState.Modified;
                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("DocumentsCoding", "Edit", "AddEdit", documentsCoding.Id, null, "تكويد المستندات");
                }
                else
                {
                    documentsCoding.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    documentsCoding.IsActive = true;
                    db.DocumentsCodings.Add(documentsCoding);

                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("DocumentsCoding", "Add", "AddEdit", documentsCoding.Id, null, "تكويد المستندات");
                }
                db.SaveChanges();

                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = documentsCoding.Id > 0 ? "تعديل تكويد المستندات" : "اضافة  تكويد المستندات",
                    EnAction = "AddEdit",
                    ControllerName = "DocumentsCoding",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "Post",
                    SelectedItem = documentsCoding.Id,
                    CodeOrDocNo = documentsCoding.Code
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
                DocumentsCoding documentsCoding = db.DocumentsCodings.Find(id);
                if (documentsCoding.IsActive == true)
                {
                    documentsCoding.IsActive = false;
                }
                else
                {
                    documentsCoding.IsActive = true;
                }
                documentsCoding.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                db.Entry(documentsCoding).State = EntityState.Modified;

                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = documentsCoding.Id > 0 ? "تنشيط تكويد المستندات" : "إلغاء تنشيط تكويد المستندات",
                    EnAction = "AddEdit",
                    ControllerName = "DocumentsCoding",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "Post",
                    SelectedItem = documentsCoding.Id,
                    CodeOrDocNo = documentsCoding.Code
                });
                ////-------------------- Notification-------------------------////
                if (documentsCoding.IsActive == true)
                {
                    Notification.GetNotification("DocumentsCoding", "Activate/Deactivate", "ActivateDeactivate", id, true, "تكويد المستندات");
                }
                else
                {

                    Notification.GetNotification("DocumentsCoding", "Activate/Deactivate", "ActivateDeactivate", id, false, "تكويد المستندات");
                }
                ///////-----------------------------------------------------------------------


                return Content("true");
            }
            catch (Exception ex)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
        }

        // POST: DocumentsCoding/Delete/5
        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                DocumentsCoding documentsCoding = db.DocumentsCodings.Find(id);
                documentsCoding.IsDeleted = true;
                documentsCoding.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                db.Entry(documentsCoding).State = EntityState.Modified;

                db.SaveChanges();

                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "حذف تكويد المستندات",
                    EnAction = "AddEdit",
                    ControllerName = "DocumentsCoding",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    CodeOrDocNo = documentsCoding.Code
                });
                ////-------------------- Notification-------------------------////
                Notification.GetNotification("DocumentsCoding", "Delete", "Delete", id, null, "تكويد المستندات");

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
            var code = QueryHelper.CodeLastNum("DocumentsCoding");
            return Json(code + 1, JsonRequestBehavior.AllowGet);
        }

        [SkipERPAuthorize]
        public JsonResult CheckDepartmentExist(int? DepartmentId)
        {
            if (DepartmentId != null) {
                var Department = db.DocumentsCodings.Where(a => a.IsDeleted == false && a.IsActive == true && a.DepartmentId == DepartmentId).FirstOrDefault();
                if (Department != null)
                {
                    return Json(new { success = "true"}, JsonRequestBehavior.AllowGet);
                }
                else
                    return Json("false", JsonRequestBehavior.AllowGet);
            }
            else
            {
                var Department = db.DocumentsCodings.Where(a => a.IsDeleted == false && a.IsActive == true && a.AllDepartments == true).FirstOrDefault();
                if (Department != null)
                {
                    return Json(new { success = "AllDepartments" }, JsonRequestBehavior.AllowGet);
                }
                else
                    return Json("false", JsonRequestBehavior.AllowGet);
            }   
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