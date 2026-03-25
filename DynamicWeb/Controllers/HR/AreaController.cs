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
    public class AreaController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();
        // GET: Area
        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة المناطق والقطاعات",
                EnAction = "Index",
                ControllerName = "Area",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });

            Notification.GetNotification("Area", "View", "Index", null, null, "المناطق و القطاعات");

            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;
            if (pageIndex > 1)
            {
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;
            }

            IQueryable<Area> areas;
            if (string.IsNullOrEmpty(searchWord))
            {
                areas = db.Areas.Where(a => a.IsDeleted == false).OrderBy(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.Areas.Where(a => a.IsDeleted == false).Count();
            }
            else
            {
                areas = db.Areas.Where(a => a.IsDeleted == false &&
                (a.ArName.Contains(searchWord) || a.EnName.Contains(searchWord) || a.Code.Contains(searchWord)))
                    .OrderBy(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.Areas.Where(a => a.IsDeleted == false && (a.ArName.Contains(searchWord) || a.EnName.Contains(searchWord) || a.Code.Contains(searchWord))).Count();
            }
            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;

            return View(areas.ToList());
        }

        public ActionResult AddEdit(int? id)
        {
            if (id == null)
            {
                var Code = SetCodeNum();
                ViewBag.Code = int.Parse(Code.Data.ToString());
                //CityId
                ViewBag.CityId = new SelectList(db.Cities.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                return View();
            }
            Area area = db.Areas.Find(id);
            if (area == null)
            {
                return HttpNotFound();
            }
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "اضافة او تعديل المناطق ",
                EnAction = "AddEdit",
                ControllerName = "Area",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = id
            });
            
            ViewBag.Next = QueryHelper.Next((int)id, "Area");
            ViewBag.Previous = QueryHelper.Previous((int)id, "Area");
            ViewBag.Last = QueryHelper.GetLast("Area");
            ViewBag.First = QueryHelper.GetFirst("Area");
            //CityId
            ViewBag.CityId = new SelectList(db.Cities.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName",area.CityId);
            return View(area);
        }

        [HttpPost]
        public ActionResult AddEdit(Area area, string newBtn)
        {
            if (ModelState.IsValid)
            {
                var id = area.Id;
                area.IsDeleted = false;
                if (area.Id > 0)
                {
                    area.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    db.Entry(area).State = EntityState.Modified;
                    Notification.GetNotification("Area", "Edit", "AddEdit", area.Id, null, "المناطق و القطاعات");
                }
                else
                {
                    area.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    area.Code = (QueryHelper.CodeLastNum("Area") + 1).ToString();
                    area.IsActive = true;
                    db.Areas.Add(area);

                    Notification.GetNotification("Area", "Add", "AddEdit", area.Id, null, "المناطق و القطاعات");
                }

                try
                {
                    db.SaveChanges();
                }
                catch (Exception ex)
                {
                    var mod = ModelState.First(c => c.Key == "Code");  // this
                    mod.Value.Errors.Add("هذا الكود موجود من قبل");
                    return View(area);
                }
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = id > 0 ? "تعديل منطقة" : "اضافة منطقة",
                    EnAction = "AddEdit",
                    ControllerName = "Area",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    CodeOrDocNo = area.Code
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
            //CityId
            ViewBag.CityId = new SelectList(db.Cities.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", area.CityId);
            return View(area);
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
                Area area = db.Areas.Find(id);
                if (area.IsActive == true)
                {
                    area.IsActive = false;
                }
                else
                {
                    area.IsActive = true;
                }
                area.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                db.Entry(area).State = EntityState.Modified;

                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = (bool)area.IsActive ? "تنشيط المناطق و القطاعات" : "إلغاء تنشيط المناطق و القطاعات",
                    EnAction = "AddEdit",
                    ControllerName = "Area",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = area.Id,
                    EnItemName = area.EnName,
                    ArItemName = area.ArName,
                    CodeOrDocNo = area.Code
                });
                if (area.IsActive == true)
                {
                    Notification.GetNotification("Area", "Activate/Deactivate", "ActivateDeactivate", id, true, "المناطق و القطاعات");
                }
                else
                {

                    Notification.GetNotification("Area", "Activate/Deactivate", "ActivateDeactivate", id, false, " المناطق و القطاعات");
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
            var code = QueryHelper.CodeLastNum("Area");
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