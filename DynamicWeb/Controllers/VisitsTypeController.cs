using MyERP.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;

namespace MyERP.Controllers
{
    public class VisitsTypeController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();
        // GET: Area
        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح انواع الزيارات",
                EnAction = "Index",
                ControllerName = "VisitsType",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });

            Notification.GetNotification("VisitsType", "View", "Index", null, null, "اعدادات الوقت");

            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;
            if (pageIndex > 1)
            {
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;
            }

            IQueryable<CRM_VisitsType> visitsTypes;
            if (string.IsNullOrEmpty(searchWord))
            {
                visitsTypes = db.CRM_VisitsType.Where(a => a.IsDeleted == false).OrderBy(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.CRM_VisitsType.Where(a => a.IsDeleted == false).Count();
            }
            else
            {
                visitsTypes = db.CRM_VisitsType.Where(a => a.IsDeleted == false &&
                (a.ArName.Contains(searchWord) || a.EnName.Contains(searchWord) || a.Code.Contains(searchWord)))
                    .OrderBy(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.CRM_VisitsType.Where(a => a.IsDeleted == false && (a.ArName.Contains(searchWord) || a.EnName.Contains(searchWord) || a.Code.Contains(searchWord))).Count();
            }
            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;

            return View(visitsTypes.ToList());
        }

        [HttpGet]
        public ActionResult AddEdit(int? id)
        {
            if (id == null)
            {

                return View();
            }

            CRM_VisitsType cRM_VisitsType = db.CRM_VisitsType.Find(id);
            if (cRM_VisitsType == null)
            {
                return HttpNotFound();
            }
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "اضافة او تعديل انواع الزيارات ",
                EnAction = "AddEdit",
                ControllerName = "VisitsType",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = id
            });

            ViewBag.Next = QueryHelper.Next((int)id, "CRM_VisitsType");
            ViewBag.Previous = QueryHelper.Previous((int)id, "CRM_VisitsType");
            ViewBag.Last = QueryHelper.GetLast("CRM_VisitsType");
            ViewBag.First = QueryHelper.GetFirst("CRM_VisitsType");
            
            return View(cRM_VisitsType);
        }

        [HttpPost]
        public ActionResult AddEdit(CRM_VisitsType cRM_VisitsType, string newBtn)
        {
            if (ModelState.IsValid)
            {
                var id = cRM_VisitsType.Id;
                cRM_VisitsType.IsDeleted = false;
                if (cRM_VisitsType.Id > 0)
                {
                    cRM_VisitsType.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    db.Entry(cRM_VisitsType).State = EntityState.Modified;
                    Notification.GetNotification("VisitsType", "Edit", "AddEdit", cRM_VisitsType.Id, null, "انواع الزيارات");
                }
                else
                {
                    cRM_VisitsType.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    cRM_VisitsType.Code = (QueryHelper.CodeLastNum("CRM_VisitsType") + 1).ToString();
                    cRM_VisitsType.IsActive = true;
                    db.CRM_VisitsType.Add(cRM_VisitsType);

                    Notification.GetNotification("VisitsType", "Add", "AddEdit", cRM_VisitsType.Id, null, "انواع الزيارات");
                }

                try
                {
                    db.SaveChanges();
                }
                catch (Exception ex)
                {
                    var mod = ModelState.First(c => c.Key == "Code");  // this
                    mod.Value.Errors.Add("هذا الكود موجود من قبل");
                    return View(cRM_VisitsType);
                }
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = id > 0 ? "تعديل انواع الزيارات" : "اضافة انواع الزيارات",
                    EnAction = "AddEdit",
                    ControllerName = "VisitsType",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    CodeOrDocNo = cRM_VisitsType.Code
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

            return View(cRM_VisitsType);
        }

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                Area area = db.Areas.Find(id);
                area.IsDeleted = true;
                area.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                db.Entry(area).State = EntityState.Modified;

                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "حذف منطقة",
                    EnAction = "AddEdit",
                    ControllerName = "Area",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    EnItemName = area.EnName

                });
                Notification.GetNotification("Area", "Delete", "Delete", id, null, "المناطق و القطاعات");


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
                CRM_VisitsType cRM_VisitsType = db.CRM_VisitsType.Find(id);
                if (cRM_VisitsType.IsActive == true)
                {
                    cRM_VisitsType.IsActive = false;
                }
                else
                {
                    cRM_VisitsType.IsActive = true;
                }
                cRM_VisitsType.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                db.Entry(cRM_VisitsType).State = EntityState.Modified;

                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = (bool)cRM_VisitsType.IsActive ? "تنشيط انواع الزيارات" : "إلغاء تنشيط انواع الزيارات",
                    EnAction = "AddEdit",
                    ControllerName = "VisitsType",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = cRM_VisitsType.Id,
                    EnItemName = cRM_VisitsType.EnName,
                    ArItemName = cRM_VisitsType.ArName,
                    CodeOrDocNo = cRM_VisitsType.Code
                });
                if (cRM_VisitsType.IsActive == true)
                {
                    Notification.GetNotification("VisitsType", "Activate/Deactivate", "ActivateDeactivate", id, true, "انواع الزيارات");
                }
                else
                {

                    Notification.GetNotification("VisitsType", "Activate/Deactivate", "ActivateDeactivate", id, false, " انواع الزيارات");
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
            var code = QueryHelper.CodeLastNum("CRM_VisitsType");
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