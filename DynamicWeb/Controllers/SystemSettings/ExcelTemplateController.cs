using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using MyERP.Models;
using System.Security.Claims;

namespace MyERP.Controllers.SystemSettings
{
    public class ExcelTemplateController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        // GET: ExcelTemplate
        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة تحميل نماذج الاكسل",
                EnAction = "Index",
                ControllerName = "ExcelTemplate",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            ////-------------------- Notification-------------------------////
            Notification.GetNotification("ExcelTemplate", "View", "Index", null, null, "تحميل نماذج الاكسل");
            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;

            IQueryable<ExcelTemplate> excelTemplates;

            if (string.IsNullOrEmpty(searchWord))
            {
                excelTemplates = db.ExcelTemplates.Where(s => s.IsDeleted == false && s.IsActive == true).OrderBy(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.ExcelTemplates.Where(s => s.IsDeleted == false && s.IsActive == true).Count();
            }
            else
            {
                excelTemplates = db.ExcelTemplates.Where(s => s.IsDeleted == false && s.FileName.Contains(searchWord)).OrderBy(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.ExcelTemplates.Where(s => s.IsDeleted == false && s.FileName.Contains(searchWord)).Count();
            }
            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            return View(excelTemplates.ToList());
        }

        public ActionResult AddEdit(int? id)
        {
            if (id == null)
            {
                return View();
            }
            ExcelTemplate excelTemplate = db.ExcelTemplates.Find(id);

            if (excelTemplate == null)
            {
                return HttpNotFound();
            }

            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "اضافة او تعديل تحميل نماذج الاكسل ",
                EnAction = "AddEdit",
                ControllerName = "ExcelTemplate",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = id
            });
            ViewBag.Next = QueryHelper.Next((int)id, "ExcelTemplate");
            ViewBag.Previous = QueryHelper.Previous((int)id, "ExcelTemplate");
            ViewBag.Last = QueryHelper.GetLast("ExcelTemplate");
            ViewBag.First = QueryHelper.GetFirst("ExcelTemplate");

            return View(excelTemplate);
        }

        [HttpPost]
        public ActionResult AddEdit(ExcelTemplate excelTemplate, string newBtn)
        {
            if (ModelState.IsValid)
            {
                var id = excelTemplate.Id;
                excelTemplate.IsDeleted = false;
                excelTemplate.IsActive = true;
                if (excelTemplate.Id > 0)
                {
                    db.Entry(excelTemplate).State = EntityState.Modified;
                    ////-------------------- Notification for any edit -------------------------////
                    Notification.GetNotification("ExcelTemplate", "Edit", "AddEdit", id, null, "تحميل نماذج الاكسل");
                }
                else
                {
                    db.ExcelTemplates.Add(excelTemplate);
                    ////-------------------- Notification for adding new shift -------------------------////
                    Notification.GetNotification("ExcelTemplate", "Add", "AddEdit", excelTemplate.Id, null, "تحميل نماذج الاكسل");
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
                    ArAction = id > 0 ? "تعديل تحميل نماذج الاكسل" : "اضافة تحميل نماذج الاكسل",
                    EnAction = "AddEdit",
                    ControllerName = "ExcelTemplate",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
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
            return View(excelTemplate);
        }

        [HttpPost, ActionName("Delete")]

        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                ExcelTemplate excelTemplate = db.ExcelTemplates.Find(id);
                excelTemplate.IsDeleted = true;
                db.Entry(excelTemplate).State = EntityState.Modified;
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "حذف تحميل نماذج الاكسل",
                    EnAction = "AddEdit",
                    ControllerName = "ExcelTemplate",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id
                });
                ////-------------------- Notification-------------------------////
                Notification.GetNotification("ExcelTemplate", "Delete", "Delete", id, null, "تحميل نماذج الاكسل");
                return Content("true");
            }
            catch (Exception)
            {
                throw;
            }
        }

        [HttpPost]
        public ActionResult ActivateDeactivate(int id)
        {
            try
            {
                ExcelTemplate excelTemplate = db.ExcelTemplates.Find(id);
                if (excelTemplate.IsActive == true)
                {
                    excelTemplate.IsActive = false;
                }
                else
                {
                    excelTemplate.IsActive = true;
                }
                db.Entry(excelTemplate).State = EntityState.Modified;

                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = (bool)excelTemplate.IsActive ? "تعديل تحميل نماذج الاكسل" : "اضافة تحميل نماذج الاكسل",
                    EnAction = "AddEdit",
                    ControllerName = "ExcelTemplate",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = excelTemplate.Id,
                });
                ////-------------------- Notification-------------------------////
                if (excelTemplate.IsActive == true)
                {
                    Notification.GetNotification("ExcelTemplate", "Activate/Deactivate", "ActivateDeactivate", id, true, "تحميل نماذج الاكسل");
                }
                else
                {
                    Notification.GetNotification("ExcelTemplate", "Activate/Deactivate", "ActivateDeactivate", id, false, "تحميل نماذج الاكسل");
                }

                return Content("true");
            }
            catch (Exception ex)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
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