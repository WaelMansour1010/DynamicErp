using DevExpress.Data.WcfLinq.Helpers;
using MyERP.Models;
using MyERP.Repository;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Web.Mvc;

namespace MyERP.Controllers
{
    public class CarServiceController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        // GET: CarService
        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة تعريف الخدمات",
                EnAction = "Index",
                ControllerName = "CarService",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });

            Notification.GetNotification("CarService", "View", "Index", null, null, " تعريف الخدمات");

            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;
            if (pageIndex > 1)
            {
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;
            }

            IQueryable<CarService> carServices;
            if (string.IsNullOrEmpty(searchWord))
            {
                carServices = db.CarServices.Where(a => a.IsDeleted == false).OrderBy(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.CarServices.Where(a => a.IsDeleted == false).Count();
            }
            else
            {
                carServices = db.CarServices.Where(a => a.IsDeleted == false &&
                (a.ArName.Contains(searchWord) || a.EnName.Contains(searchWord) || a.Code.Contains(searchWord)))
                    .OrderBy(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.CarServices.Where(a => a.IsDeleted == false && (a.ArName.Contains(searchWord) || a.EnName.Contains(searchWord) || a.Code.Contains(searchWord))).Count();
            }
            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;

            return View(carServices.ToList());
        }

        public ActionResult AddEdit(int? id)
        {
            //Helper/GetItemAvgPriceبنستخدمه فى حساب سعر البيح من ال 
            ViewBag.DepartmentId = db.Departments.Where(a => a.IsActive == true && a.IsDeleted == false).FirstOrDefault().Id;

            if (id == null)
            {
                var Code = SetCodeNum();
                ViewBag.Code = int.Parse(Code.Data.ToString());

                ViewBag.TypeId = new SelectList(new List<dynamic>
                {
                    new { Id=1, ArName="خدمة"},
                    new { Id=2, ArName="إضافة"}
                }, "Id", "ArName");
                return View();
            }
            CarService carService = db.CarServices.Find(id);
            if (carService == null)
            {
                return HttpNotFound();
            }
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "اضافة او تعديل تعريف الخدمات ",
                EnAction = "AddEdit",
                ControllerName = "CarService",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = id
            });

            ViewBag.Next = QueryHelper.Next((int)id, "CarService");
            ViewBag.Previous = QueryHelper.Previous((int)id, "CarService");
            ViewBag.Last = QueryHelper.GetLast("CarService");
            ViewBag.First = QueryHelper.GetFirst("CarService");

            ViewBag.TypeId = new SelectList(new List<dynamic>
                {
                    new { Id=1, ArName="خدمة"},
                    new { Id=2, ArName="إضافة"}
                }, "Id", "ArName", carService.TypeId);

            return View(carService);
        }

        [HttpPost]
        public ActionResult AddEdit(CarService carService)
        {
            if (ModelState.IsValid)
            {
                var id = carService.Id;
                carService.IsDeleted = false;
                if (carService.Id > 0)
                {
                    carService.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                    // use another object to prevent entity error
                    var old = db.CarServices.Find(id);
                    db.CarServiceDetails.RemoveRange(db.CarServiceDetails.Where(p => p.CarServiceId == old.Id).ToList());
                    old.ArName = carService.ArName;
                    old.Code = carService.Code;
                    old.EnName = carService.EnName;
                    old.IsActive = carService.IsActive;
                    old.Price = carService.Price;
                    old.UserId = carService.UserId;
                    old.IsDeleted = carService.IsDeleted;
                    old.TypeId = carService.TypeId;
                    old.StandardDuration = carService.StandardDuration;

                    foreach (var service in carService.CarServiceDetails)
                    {
                        old.CarServiceDetails.Add(service);
                    }
                    db.Entry(old).State = EntityState.Modified;

                    Notification.GetNotification("CarService", "Edit", "AddEdit", carService.Id, null, "تعريف الخدمات");
                }
                else
                {
                    carService.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    carService.Code = (QueryHelper.CodeLastNum("CarService") + 1).ToString();
                    carService.IsActive = true;
                    db.CarServices.Add(carService);

                    Notification.GetNotification("CarService", "Add", "AddEdit", carService.Id, null, "تعريف الخدمات");
                }
                try
                {
                    db.SaveChanges();
                }
                catch (Exception ex)
                {
                    var mod = ModelState.First(c => c.Key == "Code");  // this
                    mod.Value.Errors.Add("هذا الكود موجود من قبل");
                    return View(carService);
                }
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = id > 0 ? "تعديل تعريف الخدمات" : "اضافة تعريف الخدمات",
                    EnAction = "AddEdit",
                    ControllerName = "CarService",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    CodeOrDocNo = carService.Code
                });

                ViewBag.TypeId = new SelectList(new List<dynamic>
                {
                    new { Id=1, ArName="خدمة"},
                    new { Id=2, ArName="إضافة"}
                }, "Id", "ArName", carService.TypeId);


                return Json(new { success = "true" });

            }
            var errors = ModelState
                               .Where(x => x.Value.Errors.Count > 0)
                               .Select(x => new { x.Key, x.Value.Errors })
                               .ToArray();
            return Json(new { success = "false", errors });
        }

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                CarService carService = db.CarServices.Find(id);
                carService.IsDeleted = true;
                carService.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                foreach(var service in carService.CarServiceDetails)
                {
                    service.IsDeleted = true;
                }

                db.Entry(carService).State = EntityState.Modified;

                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "حذف تعريف الخدمات",
                    EnAction = "AddEdit",
                    ControllerName = "CarService",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    EnItemName = carService.EnName

                });
                Notification.GetNotification("CarService", "Delete", "Delete", id, null, "تعريف الخدمات");


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
                CarService carService = db.CarServices.Find(id);
                if (carService.IsActive == true)
                {
                    carService.IsActive = false;
                }
                else
                {
                    carService.IsActive = true;
                }
                carService.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                db.Entry(carService).State = EntityState.Modified;

                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = (bool)carService.IsActive ? "تنشيط تعريف الخدمات" : "إلغاء تنشيط تعريف الخدمات",
                    EnAction = "AddEdit",
                    ControllerName = "CarService",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = carService.Id,
                    EnItemName = carService.EnName,
                    ArItemName = carService.ArName,
                    CodeOrDocNo = carService.Code
                });
                if (carService.IsActive == true)
                {
                    Notification.GetNotification("CarService", "Activate/Deactivate", "ActivateDeactivate", id, true, "تعريف الخدمات");
                }
                else
                {

                    Notification.GetNotification("CarService", "Activate/Deactivate", "ActivateDeactivate", id, false, " تعريف الخدمات");
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
            var code = QueryHelper.CodeLastNum("CarService");
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