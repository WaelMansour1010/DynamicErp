using DevExpress.Data.WcfLinq.Helpers;
using MyERP.Models;
using System;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Web.Mvc;


namespace MyERP.Controllers
{
    public class VisitRecordController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        // GET: VisitRecord
        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة تسجيل الزيارة",
                EnAction = "Index",
                ControllerName = "VisitRecord",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });

            Notification.GetNotification("VisitRecord", "View", "Index", null, null, "تسجيل الزيارة");

            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;
            if (pageIndex > 1)
            {
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;
            }

            IQueryable<CRM_VisitRecord> cRM_VisitRecords;
            if (string.IsNullOrEmpty(searchWord))
            {
                cRM_VisitRecords = db.CRM_VisitRecord.Where(a => a.IsDeleted == false).OrderBy(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.CRM_VisitRecord.Where(a => a.IsDeleted == false).Count();
            }
            else
            {
                cRM_VisitRecords = db.CRM_VisitRecord.Where(a => a.IsDeleted == false &&
                (a.ArName.Contains(searchWord) || a.EnName.Contains(searchWord) || a.Code.Contains(searchWord)))
                    .OrderBy(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.CRM_VisitRecord.Where(a => a.IsDeleted == false && (a.ArName.Contains(searchWord) || a.EnName.Contains(searchWord) || a.Code.Contains(searchWord))).Count();
            }
            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;

            return View(cRM_VisitRecords.ToList());
        }

        public ActionResult Location()
        {
            return View();
        }
        public ActionResult AddEdit(int? id)
        {
            if (id == null)
            {
                var Code = SetCodeNum();
                ViewBag.Code = int.Parse(Code.Data.ToString());
                return View();
            }
            CRM_VisitRecord cRM_VisitRecord = db.CRM_VisitRecord.Find(id);
            if (cRM_VisitRecord == null)
            {
                return HttpNotFound();
            }
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "اضافة او تعديل تسجيل الزيارة ",
                EnAction = "AddEdit",
                ControllerName = "VisitRecord",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = id
            });

            ViewBag.Next = QueryHelper.Next((int)id, "CRM_VisitRecord");
            ViewBag.Previous = QueryHelper.Previous((int)id, "CRM_VisitRecord");
            ViewBag.Last = QueryHelper.GetLast("CRM_VisitRecord");
            ViewBag.First = QueryHelper.GetFirst("CRM_VisitRecord");

            return View(cRM_VisitRecord);
        }

        [HttpPost]
        public ActionResult AddEdit(CRM_VisitRecord cRM_VisitRecord, string newBtn)
        {
            if (ModelState.IsValid)
            {
                var id = cRM_VisitRecord.Id;
                cRM_VisitRecord.IsDeleted = false;
                if (cRM_VisitRecord.Id > 0)
                {
                    cRM_VisitRecord.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    db.Entry(cRM_VisitRecord).State = EntityState.Modified;
                    Notification.GetNotification("VisitRecord", "Edit", "AddEdit", cRM_VisitRecord.Id, null, "تسجيل الزيارة");
                }
                else
                {
                    cRM_VisitRecord.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    cRM_VisitRecord.Code = (QueryHelper.CodeLastNum("CRM_VisitRecord") + 1).ToString();
                    cRM_VisitRecord.IsActive = true;
                    db.CRM_VisitRecord.Add(cRM_VisitRecord);

                    Notification.GetNotification("VisitRecord", "Add", "AddEdit", cRM_VisitRecord.Id, null, "تسجيل الزيارة");
                }

                try
                {
                    db.SaveChanges();
                }
                catch (Exception ex)
                {
                    var mod = ModelState.First(c => c.Key == "Code");  // this
                    mod.Value.Errors.Add("هذا الكود موجود من قبل");
                    return View(cRM_VisitRecord);
                }
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = id > 0 ? "تعديل تسجيل الزيارة" : "اضافة تسجيل الزيارة",
                    EnAction = "AddEdit",
                    ControllerName = "VisitRecord",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    CodeOrDocNo = cRM_VisitRecord.Code
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
           
            return View(cRM_VisitRecord);
        }

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                CRM_VisitRecord cRM_VisitRecord = db.CRM_VisitRecord.Find(id);
                cRM_VisitRecord.IsDeleted = true;
                cRM_VisitRecord.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                db.Entry(cRM_VisitRecord).State = EntityState.Modified;

                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "حذف تسجيل الزيارة",
                    EnAction = "AddEdit",
                    ControllerName = "VisitRecord",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    EnItemName = cRM_VisitRecord.EnName

                });
                Notification.GetNotification("VisitRecord", "Delete", "Delete", id, null, "تسجيل الزيارة");


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
                CRM_VisitRecord cRM_VisitRecord = db.CRM_VisitRecord.Find(id);
                if (cRM_VisitRecord.IsActive == true)
                {
                    cRM_VisitRecord.IsActive = false;
                }
                else
                {
                    cRM_VisitRecord.IsActive = true;
                }
                cRM_VisitRecord.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                db.Entry(cRM_VisitRecord).State = EntityState.Modified;

                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = (bool)cRM_VisitRecord.IsActive ? "تنشيط تسجيل الزيارة" : "إلغاء تنشيط تسجيل الزيارة",
                    EnAction = "AddEdit",
                    ControllerName = "VisitRecord",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = cRM_VisitRecord.Id,
                    EnItemName = cRM_VisitRecord.EnName,
                    ArItemName = cRM_VisitRecord.ArName,
                    CodeOrDocNo = cRM_VisitRecord.Code
                });
                if (cRM_VisitRecord.IsActive == true)
                {
                    Notification.GetNotification("VisitRecord", "Activate/Deactivate", "ActivateDeactivate", id, true, "تسجيل الزيارة");
                }
                else
                {

                    Notification.GetNotification("VisitRecord", "Activate/Deactivate", "ActivateDeactivate", id, false, "تسجيل الزيارة");
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
            var code = QueryHelper.CodeLastNum("CRM_VisitRecord");
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