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
    public class TimeSettingController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();
        // GET: Area
        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح اعدادات الوقت",
                EnAction = "Index",
                ControllerName = "TimeSetting",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });

            Notification.GetNotification("TimeSetting", "View", "Index", null, null, "اعدادات الوقت");

            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;
            if (pageIndex > 1)
            {
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;
            }

            IQueryable<CRM_TimeSetting> timeSettings;
            if (string.IsNullOrEmpty(searchWord))
            {
                timeSettings = db.CRM_TimeSetting.Where(a => a.IsDeleted == false).OrderBy(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.CRM_TimeSetting.Where(a => a.IsDeleted == false).Count();
            }
            else
            {
                timeSettings = db.CRM_TimeSetting.Where(a => a.IsDeleted == false &&
                (a.ArName.Contains(searchWord) || a.EnName.Contains(searchWord) || a.Code.Contains(searchWord)))
                    .OrderBy(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.CRM_TimeSetting.Where(a => a.IsDeleted == false && (a.ArName.Contains(searchWord) || a.EnName.Contains(searchWord) || a.Code.Contains(searchWord))).Count();
            }
            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;

            return View(timeSettings.ToList());
        }

        [HttpGet]
        public ActionResult AddEdit(int? id)
        {
            if (id == null)
            {
               
                return View();
            }

            CRM_TimeSetting cRM_TimeSetting = db.CRM_TimeSetting.Find(id);
            if (cRM_TimeSetting == null)
            {
                return HttpNotFound();
            }
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "اضافة او تعديل اعدادات الوقت ",
                EnAction = "AddEdit",
                ControllerName = "TimeSetting",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = id
            });

            ViewBag.Next = QueryHelper.Next((int)id, "CRM_TimeSetting");
            ViewBag.Previous = QueryHelper.Previous((int)id, "CRM_TimeSetting");
            ViewBag.Last = QueryHelper.GetLast("CRM_TimeSetting");
            ViewBag.First = QueryHelper.GetFirst("CRM_TimeSetting");
            /*//CityId
            ViewBag.CityId = new SelectList(db.Cities.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", area.CityId);*/
            return View(cRM_TimeSetting);
        }

        [HttpPost]
        public ActionResult AddEdit(CRM_TimeSetting cRM_TimeSetting, string newBtn)
        {
            if (ModelState.IsValid)
            {
                var id = cRM_TimeSetting.Id;
                cRM_TimeSetting.IsDeleted = false;
                if (cRM_TimeSetting.Id > 0)
                {
                    cRM_TimeSetting.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    db.Entry(cRM_TimeSetting).State = EntityState.Modified;
                    Notification.GetNotification("TimeSetting", "Edit", "AddEdit", cRM_TimeSetting.Id, null, "اعدادات الوقت");
                }
                else
                {
                    cRM_TimeSetting.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    cRM_TimeSetting.Code = (QueryHelper.CodeLastNum("CRM_TimeSetting") + 1).ToString();
                    cRM_TimeSetting.IsActive = true;
                    db.CRM_TimeSetting.Add(cRM_TimeSetting);

                    Notification.GetNotification("TimeSetting", "Add", "AddEdit", cRM_TimeSetting.Id, null, "اعدادات الوقت");
                }

                try
                {
                    db.SaveChanges();
                }
                catch (Exception ex)
                {
                    var mod = ModelState.First(c => c.Key == "Code");  // this
                    mod.Value.Errors.Add("هذا الكود موجود من قبل");
                    return View(cRM_TimeSetting);
                }
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = id > 0 ? "تعديل اعدادات الوقت" : "اضافة اعدادات الوقت",
                    EnAction = "AddEdit",
                    ControllerName = "TimeSetting",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    CodeOrDocNo = cRM_TimeSetting.Code
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

            return View(cRM_TimeSetting);
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
                CRM_TimeSetting cRM_TimeSetting = db.CRM_TimeSetting.Find(id);
                if (cRM_TimeSetting.IsActive == true)
                {
                    cRM_TimeSetting.IsActive = false;
                }
                else
                {
                    cRM_TimeSetting.IsActive = true;
                }
                cRM_TimeSetting.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                db.Entry(cRM_TimeSetting).State = EntityState.Modified;

                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = (bool)cRM_TimeSetting.IsActive ? "تنشيط اعداد الوقت" : "إلغاء تنشيط اعداد الوقت",
                    EnAction = "AddEdit",
                    ControllerName = "TimeSetting",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = cRM_TimeSetting.Id,
                    EnItemName = cRM_TimeSetting.EnName,
                    ArItemName = cRM_TimeSetting.ArName,
                    CodeOrDocNo = cRM_TimeSetting.Code
                });
                if (cRM_TimeSetting.IsActive == true)
                {
                    Notification.GetNotification("TimeSetting", "Activate/Deactivate", "ActivateDeactivate", id, true, "اعدادات الوقت");
                }
                else
                {

                    Notification.GetNotification("TimeSetting", "Activate/Deactivate", "ActivateDeactivate", id, false, " اعدادات الوقت");
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
            var code = QueryHelper.CodeLastNum("CRM_TimeSetting");
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