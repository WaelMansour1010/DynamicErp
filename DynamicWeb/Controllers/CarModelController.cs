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
    public class CarModelController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        // GET: CarModel
        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة موديلات السيارات",
                EnAction = "Index",
                ControllerName = "CarModel",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            ////-------------------- Notification-------------------------////
            Notification.GetNotification("CarModel", "View", "Index", null, null, "موديلات السيارات");
            //////////-----------------------------------------------------------------------
            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;

            IQueryable<CarModel> carModels;

            if (string.IsNullOrEmpty(searchWord))
            {
                carModels = db.CarModels.Where(s => s.IsDeleted == false).OrderBy(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.CarModels.Where(s => s.IsDeleted == false).Count();

            }
            else
            {
                carModels = db.CarModels.Where(s => s.IsDeleted == false && (s.Code.Contains(searchWord) || s.ArName.Contains(searchWord) || s.EnName.Contains(searchWord) || s.Notes.Contains(searchWord))).OrderBy(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.CarModels.Where(s => s.IsDeleted == false && (s.Code.Contains(searchWord) || s.ArName.Contains(searchWord) || s.EnName.Contains(searchWord) ||  s.Notes.Contains(searchWord))).Count();
            }
            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            return View(carModels.ToList());
        }


        public ActionResult AddEdit(int? id)
        {

            if (id == null)
            {
                ViewBag.CarTypeId = new SelectList(db.CarTypes.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");

                return View();
            }
            CarModel carModel = db.CarModels.Find(id);
            if (carModel == null)
            {
                return HttpNotFound();
            }

            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح تفاصيل موديل السيارة ",
                EnAction = "AddEdit",
                ControllerName = "CarModel",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = carModel.Id,
                ArItemName = carModel.ArName,
                EnItemName = carModel.EnName,

            });
            ViewBag.CarTypeId = new SelectList(db.CarTypes.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", carModel.CarTypeId);
            
            ViewBag.Next = QueryHelper.Next((int)id, "CarModel");
            ViewBag.Previous = QueryHelper.Previous((int)id, "CarModel");
            ViewBag.Last = QueryHelper.GetLast("CarModel");
            ViewBag.First = QueryHelper.GetFirst("CarModel");
            return View(carModel);
        }


        [HttpPost]
        //[ValidateAntiForgeryToken]
        public ActionResult AddEdit(CarModel carModel, string newBtn)
        {
            if (ModelState.IsValid)
            {
                var id = carModel.Id;
                carModel.IsDeleted = false;

                if (carModel.Id > 0)
                {
                    //CarModel.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                    db.Entry(carModel).State = EntityState.Modified;

                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("CarModel", "Edit", "AddEdit", id, null, "طرق الدفع");

                    /////////-----------------------------------------------------------------------


                }
                else
                {
            
                    carModel.IsActive = true;
                    db.CarModels.Add(carModel);

                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("CarModel", "Add", "AddEdit", carModel.Id, null, "طرق الدفع");

                    ///////////-----------------------------------------------------------------------

                }

               
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = id > 0 ? "تعديل موديل سيارة" : "اضافة موديل سيارة",
                    EnAction = "AddEdit",
                    ControllerName = "ItemGroups",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = carModel.Id,
                    ArItemName = carModel.ArName,
                    EnItemName = carModel.EnName,

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

            return View(carModel);
        }


        [HttpPost, ActionName("Delete")]

        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                CarModel carModel = db.CarModels.Find(id);
                carModel.IsDeleted = true;
                db.Entry(carModel).State = EntityState.Modified;
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "حذف موديل سيارة",
                    EnAction = "AddEdit",
                    ControllerName = "CarModel",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    EnItemName = carModel.EnName,
                    ArItemName = carModel.ArName,

                });
                ////-------------------- Notification-------------------------////
                Notification.GetNotification("CarModel", "Delete", "Delete", id, null, "طرق دفع نقاط البيع");

                return Content("true");
            }
            catch (Exception)
            {
                throw;
            }
        }


        [SkipERPAuthorize]
        public JsonResult GetModels(int id)
        {
            var CarModels = db.CarModels.Where(a=>a.CarTypeId == id).Select(a=> new 
            {
                Id = a.Id,
                ArName = a.Code + " - " + a.ArName
            }).ToList();
            return Json(CarModels, JsonRequestBehavior.AllowGet);
        }

        [SkipERPAuthorize]
        public JsonResult SetCodeNum()
        {
            var code = QueryHelper.CodeLastNum("CarModel");
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

        [HttpPost]
        public ActionResult ActivateDeactivate(int id)
        {
            try
            {
                CarModel carModel = db.CarModels.Find(id);
                if (carModel.IsActive == true)
                {
                    carModel.IsActive = false;
                }
                else
                {
                    carModel.IsActive = true;
                }
                carModel.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                db.Entry(carModel).State = EntityState.Modified;

                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = carModel.Id > 0 ? "تنشيط موديل سيارة" : "إلغاء تنشيط موديل سيارة",
                    EnAction = "AddEdit",
                    ControllerName = "CarModel",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "Post",
                    SelectedItem = carModel.Id,
                    ArItemName = carModel.ArName,
                    EnItemName = carModel.EnName,
                    CodeOrDocNo = carModel.Code
                });
                ////-------------------- Notification-------------------------////
                if (carModel.IsActive == true)
                {
                    Notification.GetNotification("CarModel", "Activate/Deactivate", "ActivateDeactivate", id, true, "موديلات السيارات");
                }
                else
                {

                    Notification.GetNotification("CarModel", "Activate/Deactivate", "ActivateDeactivate", id, false, "موديلات السيارات");
                }
                ///////-----------------------------------------------------------------------


                return Content("true");
            }
            catch (Exception ex)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
        }
    }
}