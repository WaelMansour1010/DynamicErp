using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;
using MyERP.Models;

namespace MyERP.Controllers.MedicalManagement
{
    public class MedicalSpecialtyController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        // GET: MedicalSpecialty
        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة التخصصات الطبية",
                EnAction = "Index",
                ControllerName = "MedicalSpecialty",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            Notification.GetNotification("MedicalSpecialty", "View", "Index", null, null, "التخصصات الطبية");
            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;
            if (pageIndex > 1)
            {
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;
            }
            IQueryable<MedicalSpecialty> medicalSpecialties;
            if (string.IsNullOrEmpty(searchWord))
            {
                medicalSpecialties = db.MedicalSpecialties.Where(a => a.IsDeleted == false).OrderBy(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.MedicalSpecialties.Where(a => a.IsDeleted == false).Count();
            }
            else
            {
                medicalSpecialties = db.MedicalSpecialties.Where(a => a.IsDeleted == false &&
                (a.ArName.Contains(searchWord) || a.EnName.Contains(searchWord) || a.Code.Contains(searchWord)))
                    .OrderBy(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.MedicalSpecialties.Where(a => a.IsDeleted == false && (a.ArName.Contains(searchWord) || a.EnName.Contains(searchWord) || a.Code.Contains(searchWord))).Count();
            }
            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            return View(medicalSpecialties.ToList());
        }
        public ActionResult AddEdit(int? id)
        {
            if (id == null)
            {
                return View();
            }
            MedicalSpecialty medicalSpecialty = db.MedicalSpecialties.Find(id);
            if (medicalSpecialty == null)
            {
                return HttpNotFound();
            }
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "اضافة او تعديل التخصصات الطبية ",
                EnAction = "AddEdit",
                ControllerName = "MedicalSpecialty",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = id
            });
            ViewBag.Next = QueryHelper.Next((int)id, "MedicalSpecialty");
            ViewBag.Previous = QueryHelper.Previous((int)id, "MedicalSpecialty");
            ViewBag.Last = QueryHelper.GetLast("MedicalSpecialty");
            ViewBag.First = QueryHelper.GetFirst("MedicalSpecialty");
            return View(medicalSpecialty);
        }
        [HttpPost]
        public ActionResult AddEdit(MedicalSpecialty medicalSpecialty, string newBtn)
        {
            if (ModelState.IsValid)
            {
                var id = medicalSpecialty.Id;
                medicalSpecialty.IsDeleted = false;
                medicalSpecialty.IsActive = true;
                medicalSpecialty.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                if (medicalSpecialty.Id > 0)
                {
                    db.Entry(medicalSpecialty).State = EntityState.Modified;
                    Notification.GetNotification("MedicalSpecialty", "Edit", "AddEdit", medicalSpecialty.Id, null, "التخصصات الطبية");
                }
                else
                {
                    medicalSpecialty.Code = (QueryHelper.CodeLastNum("MedicalSpecialty") + 1).ToString();
                    db.MedicalSpecialties.Add(medicalSpecialty);
                    Notification.GetNotification("MedicalSpecialty", "Add", "AddEdit", medicalSpecialty.Id, null, "التخصصات الطبية");
                }
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = id > 0 ? "تعديل التخصصات الطبية" : "اضافة التخصصات الطبية",
                    EnAction = "AddEdit",
                    ControllerName = "MedicalSpecialty",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    CodeOrDocNo = medicalSpecialty.Code
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
            return View(medicalSpecialty);
        }

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                MedicalSpecialty medicalSpecialty = db.MedicalSpecialties.Find(id);
                medicalSpecialty.IsDeleted = true;
                medicalSpecialty.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                db.Entry(medicalSpecialty).State = EntityState.Modified;
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "حذف التخصصات الطبية",
                    EnAction = "AddEdit",
                    ControllerName = "MedicalSpecialty",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    EnItemName = medicalSpecialty.EnName
                });
                Notification.GetNotification("MedicalSpecialty", "Delete", "Delete", id, null, "التخصصات الطبية");
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
                MedicalSpecialty medicalSpecialty = db.MedicalSpecialties.Find(id);
                if (medicalSpecialty.IsActive == true)
                {
                    medicalSpecialty.IsActive = false;
                }
                else
                {
                    medicalSpecialty.IsActive = true;
                }
                medicalSpecialty.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                db.Entry(medicalSpecialty).State = EntityState.Modified;
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = (bool)medicalSpecialty.IsActive ? "تنشيط التخصصات الطبية" : "إلغاء تنشيط التخصصات الطبية",
                    EnAction = "AddEdit",
                    ControllerName = "MedicalSpecialty",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = medicalSpecialty.Id,
                    EnItemName = medicalSpecialty.EnName,
                    ArItemName = medicalSpecialty.ArName,
                    CodeOrDocNo = medicalSpecialty.Code
                });
                if (medicalSpecialty.IsActive == true)
                {
                    Notification.GetNotification("MedicalSpecialty", "Activate/Deactivate", "ActivateDeactivate", id, true, "التخصصات الطبية");
                }
                else
                {
                    Notification.GetNotification("MedicalSpecialty", "Activate/Deactivate", "ActivateDeactivate", id, false, " التخصصات الطبية");
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
            var code = QueryHelper.CodeLastNum("MedicalSpecialty");
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