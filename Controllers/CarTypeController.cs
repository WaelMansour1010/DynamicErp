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
    public class CarTypeController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        // GET: CarType
        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة انواع السيارات",
                EnAction = "Index",
                ControllerName = "CarType",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });

            Notification.GetNotification("CarType", "View", "Index", null, null, "انواع السيارات");

            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;
            if (pageIndex > 1)
            {
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;
            }

            IQueryable<CarType> carTypes;
            if (string.IsNullOrEmpty(searchWord))
            {
                carTypes = db.CarTypes.Where(a => a.IsDeleted == false).OrderBy(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.CarTypes.Where(a => a.IsDeleted == false).Count();
            }
            else
            {
                carTypes = db.CarTypes.Where(a => a.IsDeleted == false &&
                (a.ArName.Contains(searchWord) || a.EnName.Contains(searchWord) || a.Code.Contains(searchWord)))
                    .OrderBy(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.CarTypes.Where(a => a.IsDeleted == false && (a.ArName.Contains(searchWord) || a.EnName.Contains(searchWord) || a.Code.Contains(searchWord))).Count();
            }
            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;

            return View(carTypes.ToList());
        }

        public ActionResult AddEdit(int? id)
        {
            if (id == null)
            {
                var Code = SetCodeNum();
                ViewBag.Code = int.Parse(Code.Data.ToString());
                return View();
            }
            CarType carType = db.CarTypes.Find(id);
            if (carType == null)
            {
                return HttpNotFound();
            }
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "اضافة او تعديل انواع السيارات ",
                EnAction = "AddEdit",
                ControllerName = "CarType",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = id
            });

            ViewBag.Next = QueryHelper.Next((int)id, "CarType");
            ViewBag.Previous = QueryHelper.Previous((int)id, "CarType");
            ViewBag.Last = QueryHelper.GetLast("CarType");
            ViewBag.First = QueryHelper.GetFirst("CarType");
            return View(carType);
        }

        [HttpPost]
        public ActionResult AddEdit(CarType carType, string newBtn)
        {
            if (ModelState.IsValid)
            {
                var id = carType.Id;
                carType.IsDeleted = false;
                if (carType.Id > 0)
                {
                    carType.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    db.Entry(carType).State = EntityState.Modified;
                    Notification.GetNotification("CarType", "Edit", "AddEdit", carType.Id, null, "انواع السيارات");
                }
                else
                {
                    carType.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    carType.Code = (QueryHelper.CodeLastNum("CarType") + 1).ToString();
                    carType.IsActive = true;
                    db.CarTypes.Add(carType);

                    Notification.GetNotification("CarType", "Add", "AddEdit", carType.Id, null, "انواع السيارات");
                }

                try
                {
                    db.SaveChanges();
                }
                catch (Exception ex)
                {
                    var mod = ModelState.First(c => c.Key == "Code");  // this
                    mod.Value.Errors.Add("هذا الكود موجود من قبل");
                    return View(carType);
                }
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = id > 0 ? "تعديل نوع سيارة" : "اضافة نوع سيارة",
                    EnAction = "AddEdit",
                    ControllerName = "CarType",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    CodeOrDocNo = carType.Code
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

            return View(carType);
        }

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                CarType carType = db.CarTypes.Find(id);
                carType.IsDeleted = true;
                carType.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                db.Entry(carType).State = EntityState.Modified;

                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "حذف نوع سيارة",
                    EnAction = "AddEdit",
                    ControllerName = "CarType",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    EnItemName = carType.EnName

                });
                Notification.GetNotification("CarType", "Delete", "Delete", id, null, "انواع السيارات");


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
                CarType carType = db.CarTypes.Find(id);
                if (carType.IsActive == true)
                {
                    carType.IsActive = false;
                }
                else
                {
                    carType.IsActive = true;
                }
                carType.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                db.Entry(carType).State = EntityState.Modified;

                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = (bool)carType.IsActive ? "تنشيط انواع السيارات" : "إلغاء تنشيط انواع السيارات",
                    EnAction = "AddEdit",
                    ControllerName = "CarType",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = carType.Id,
                    EnItemName = carType.EnName,
                    ArItemName = carType.ArName,
                    CodeOrDocNo = carType.Code
                });
                if (carType.IsActive == true)
                {
                    Notification.GetNotification("CarType", "Activate/Deactivate", "ActivateDeactivate", id, true, "انواع السيارات");
                }
                else
                {

                    Notification.GetNotification("CarType", "Activate/Deactivate", "ActivateDeactivate", id, false, " انواع السيارات");
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
            var code = QueryHelper.CodeLastNum("CarType");
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