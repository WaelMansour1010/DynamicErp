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
    public class PropertyStatusController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        // GET: PropertyStatus
        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة حالات العقارات",
                EnAction = "Index",
                ControllerName = "PropertyStatus",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            Notification.GetNotification("PropertyStatus", "View", "Index", null, null, "حالات العقارات");
            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;
            if (pageIndex > 1)
            {
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;
            }
            IQueryable<PropertyStatu> propertyStatus;
            if (string.IsNullOrEmpty(searchWord))
            {
                propertyStatus = db.PropertyStatus.Where(a => a.IsDeleted == false).OrderBy(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.PropertyStatus.Where(a => a.IsDeleted == false).Count();
            }
            else
            {
                propertyStatus = db.PropertyStatus.Where(a => a.IsDeleted == false &&
                (a.ArName.Contains(searchWord) || a.EnName.Contains(searchWord) || a.Code.Contains(searchWord)))
                    .OrderBy(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.PropertyStatus.Where(a => a.IsDeleted == false && (a.ArName.Contains(searchWord) || a.EnName.Contains(searchWord) || a.Code.Contains(searchWord))).Count();
            }
            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            return View(propertyStatus.ToList());
        }

        public ActionResult AddEdit(int? id)
        {
            if (id == null)
            {

                return View();
            }
            PropertyStatu propertyStatus = db.PropertyStatus.Find(id);

            if (propertyStatus == null)
            {
                return HttpNotFound();
            }
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "اضافة او تعديل حالات العقارات ",
                EnAction = "AddEdit",
                ControllerName = "PropertyStatus",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = id
            });
            ViewBag.Next = QueryHelper.Next((int)id, "PropertyStatus");
            ViewBag.Previous = QueryHelper.Previous((int)id, "PropertyStatus");
            ViewBag.Last = QueryHelper.GetLast("PropertyStatus");
            ViewBag.First = QueryHelper.GetFirst("PropertyStatus");
            return View(propertyStatus);
        }
        [HttpPost]
        public ActionResult AddEdit(PropertyStatu propertyStatu, string newBtn)
        {
            string domainName = Request.Url.GetLeftPart(UriPartial.Authority);

            if (ModelState.IsValid)
            {
                var id = propertyStatu.Id;
                propertyStatu.IsDeleted = false;
                propertyStatu.IsActive = true;
                propertyStatu.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                if (propertyStatu.Id > 0)
                {
                    db.Entry(propertyStatu).State = EntityState.Modified;
                    Notification.GetNotification("PropertyStatus", "Edit", "AddEdit", propertyStatu.Id, null, "حالات العقارات");
                }
                else
                {
                    propertyStatu.Code = (QueryHelper.CodeLastNum("PropertyStatus") + 1).ToString();
                    db.PropertyStatus.Add(propertyStatu);
                    Notification.GetNotification("PropertyStatus", "Add", "AddEdit", propertyStatu.Id, null, "حالات العقارات");
                }
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = id > 0 ? "تعديل حالات العقارات" : "اضافة حالات العقارات",
                    EnAction = "AddEdit",
                    ControllerName = "PropertyStatus",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    CodeOrDocNo = propertyStatu.Code
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
            return View(propertyStatu);
        }

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                PropertyStatu propertyStatu = db.PropertyStatus.Find(id);
                propertyStatu.IsDeleted = true;
                propertyStatu.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                db.Entry(propertyStatu).State = EntityState.Modified;
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "حذف حالات العقارات",
                    EnAction = "AddEdit",
                    ControllerName = "PropertyStatus",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id
                });
                Notification.GetNotification("PropertyStatus", "Delete", "Delete", id, null, "حالات العقارات");
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
                PropertyStatu propertyStatu = db.PropertyStatus.Find(id);
                if (propertyStatu.IsActive == true)
                {
                    propertyStatu.IsActive = false;
                }
                else
                {
                    propertyStatu.IsActive = true;
                }
                propertyStatu.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                db.Entry(propertyStatu).State = EntityState.Modified;
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = (bool)propertyStatu.IsActive ? "تنشيط حالات العقارات" : "إلغاء تنشيط حالات العقارات",
                    EnAction = "AddEdit",
                    ControllerName = "PropertyStatus",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = propertyStatu.Id,
                    CodeOrDocNo = propertyStatu.Code
                });
                if (propertyStatu.IsActive == true)
                {
                    Notification.GetNotification("PropertyStatus", "Activate/Deactivate", "ActivateDeactivate", id, true, "حالات العقارات");
                }
                else
                {
                    Notification.GetNotification("PropertyStatus", "Activate/Deactivate", "ActivateDeactivate", id, false, " حالات العقارات");
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
            var code = QueryHelper.CodeLastNum("PropertyStatus");
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