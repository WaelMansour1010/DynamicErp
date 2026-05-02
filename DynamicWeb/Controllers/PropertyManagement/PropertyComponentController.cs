using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;
using MyERP.Models;
using MyERP.Repository;

namespace MyERP.Controllers.PropertyManagement
{
    public class PropertyComponentController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();
        // GET: PropertyComponent
        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة مكونات العقار",
                EnAction = "Index",
                ControllerName = "PropertyComponent",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            Notification.GetNotification("PropertyComponent", "View", "Index", null, null, "مكونات العقار");
            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;
            if (pageIndex > 1)
            {
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;
            }
            IQueryable<PropertyComponent> components;
            if (string.IsNullOrEmpty(searchWord))
            {
                components = db.PropertyComponents.Where(a => a.IsDeleted == false).OrderBy(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.PropertyComponents.Where(a => a.IsDeleted == false).Count();
            }
            else
            {
                components = db.PropertyComponents.Where(a => a.IsDeleted == false &&
                (a.ArName.Contains(searchWord) || a.EnName.Contains(searchWord) || a.Code.Contains(searchWord)))
                    .OrderBy(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.PropertyComponents.Where(a => a.IsDeleted == false && (a.ArName.Contains(searchWord) || a.EnName.Contains(searchWord) || a.Code.Contains(searchWord))).Count();
            }
            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            return View(components.ToList());
        }

        public ActionResult AddEdit(int? id)
        {
            var systemSetting = db.SystemSettings.FirstOrDefault();
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            var subAccounts =  db.ChartOfAccounts.Where(c => c.IsDeleted == false && c.IsActive == true && (c.ClassificationId == 3)).Select(b => new
            {
                b.Id,
                ArName =  b.Code + " - " + b.ArName
            }).ToList();
            //------ Time Zone Depends On Currency --------//
            var Currency = db.Currencies.Where(a => a.IsActive == true && a.IsDeleted == false && a.IsDefault == true).FirstOrDefault();
            var CurrencyCode = Currency != null ? Currency.Code : "";
            TimeZoneInfo info;
            if (CurrencyCode == "SAR")
            {
                //info = TimeZoneInfo.FindSystemTimeZoneById("Arabian Standard Time");//+2H from Egypt Standard Time
                info = TimeZoneInfo.FindSystemTimeZoneById("Arab Standard Time");// +1H from Egypt Standard Time
            }
            else
            {
                info = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");
            }
            DateTime utcNow = DateTime.UtcNow;
            DateTime cTime = TimeZoneInfo.ConvertTimeFromUtc(utcNow, info);
            //----------------- End of Time Zone Depends On Currency --------------------//
            DepartmentRepository departmentRepository = new DepartmentRepository(db);
            if (id == null)
            {
                ViewBag.ExpenseAccountId = new SelectList(subAccounts, "Id", "ArName");
                return View();
            }
            PropertyComponent component = db.PropertyComponents.Find(id);

            if (component == null)
            {
                return HttpNotFound();
            }

            ViewBag.ExpenseAccountId = new SelectList(subAccounts, "Id", "ArName", component.ExpenseAccountId);


            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "اضافة او تعديل مكونات العقار ",
                EnAction = "AddEdit",
                ControllerName = "PropertyComponent",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = id
            });

            ViewBag.Next = QueryHelper.Next((int)id, "PropertyComponent");
            ViewBag.Previous = QueryHelper.Previous((int)id, "PropertyComponent");
            ViewBag.Last = QueryHelper.GetLast("PropertyComponent");
            ViewBag.First = QueryHelper.GetFirst("PropertyComponent");
            return View(component);
        }
        [HttpPost]
        public ActionResult AddEdit(PropertyComponent component)
        {
            string domainName = Request.Url.GetLeftPart(UriPartial.Authority);

            if (ModelState.IsValid)
            {
                var id = component.Id;
                component.IsDeleted = false;
                component.IsActive = true;
                component.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                if (component.Id > 0)
                {
                    db.PropertyComponentDetails.RemoveRange(db.PropertyComponentDetails.Where(x => x.PropertyComponentId == component.Id));
                    var propertyComponentDetials = component.PropertyComponentDetails.ToList();
                    propertyComponentDetials.ForEach((x) => x.PropertyComponentId = component.Id);
                    component.PropertyComponentDetails = null;
                    db.Entry(component).State = EntityState.Modified;
                    db.PropertyComponentDetails.AddRange(propertyComponentDetials);

                    Notification.GetNotification("PropertyComponent", "Edit", "AddEdit", component.Id, null, "مكونات العقار");
                }
                else
                {
                    component.Code = (QueryHelper.CodeLastNum("PropertyComponent") + 1).ToString();
                    db.PropertyComponents.Add(component);
                    Notification.GetNotification("PropertyComponent", "Add", "AddEdit", component.Id, null, "مكونات العقار");
                }
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = id > 0 ? "تعديل مكونات العقار" : "اضافة مكونات العقار",
                    EnAction = "AddEdit",
                    ControllerName = "PropertyComponent",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    CodeOrDocNo = component.Code
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
                PropertyComponent component = db.PropertyComponents.Find(id);
                component.IsDeleted = true;
                component.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
               
                foreach (var item in component.PropertyComponentDetails)
                {
                    item.IsDeleted = true;
                }
                db.Entry(component).State = EntityState.Modified;
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "حذف مكونات العقار",
                    EnAction = "AddEdit",
                    ControllerName = "PropertyComponent",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id
                });
                Notification.GetNotification("PropertyComponent", "Delete", "Delete", id, null, "مكونات العقار");
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
                PropertyComponent component = db.PropertyComponents.Find(id);
                if (component.IsActive == true)
                {
                    component.IsActive = false;
                }
                else
                {
                    component.IsActive = true;
                }
                component.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                db.Entry(component).State = EntityState.Modified;
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = (bool)component.IsActive ? "تنشيط مكونات العقار" : "إلغاء تنشيط مكونات العقار",
                    EnAction = "AddEdit",
                    ControllerName = "PropertyComponent",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = component.Id,
                    CodeOrDocNo = component.Code
                });
                if (component.IsActive == true)
                {
                    Notification.GetNotification("PropertyComponent", "Activate/Deactivate", "ActivateDeactivate", id, true, "مكونات العقار");
                }
                else
                {
                    Notification.GetNotification("PropertyComponent", "Activate/Deactivate", "ActivateDeactivate", id, false, "مكونات العقار");
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
            var code = QueryHelper.CodeLastNum("PropertyComponent");
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