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
    public class CarServiceCategoryController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        // GET: CarServiceCategory
        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة فئات خدمات السيارات",
                EnAction = "Index",
                ControllerName = "CarServiceCategory",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });

            Notification.GetNotification("CarServiceCategory", "View", "Index", null, null, "فئات خدمات السيارات");

            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;
            if (pageIndex > 1)
            {
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;
            }

            IQueryable<CarServiceCategory> carServiceCategories;
            if (string.IsNullOrEmpty(searchWord))
            {
                carServiceCategories = db.CarServiceCategories.Where(a => a.IsDeleted == false).OrderBy(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.CarServiceCategories.Where(a => a.IsDeleted == false).Count();
            }
            else
            {
                carServiceCategories = db.CarServiceCategories.Where(a => a.IsDeleted == false &&
                (a.ArName.Contains(searchWord) || a.EnName.Contains(searchWord) || a.Code.Contains(searchWord)))
                    .OrderBy(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.CarServiceCategories.Where(a => a.IsDeleted == false && (a.ArName.Contains(searchWord) || a.EnName.Contains(searchWord) || a.Code.Contains(searchWord))).Count();
            }
            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;

            return View(carServiceCategories.ToList());
        }

        public ActionResult AddEdit(int? id)
        {
            if (id == null)
            {
                var Code = SetCodeNum();
                ViewBag.Code = int.Parse(Code.Data.ToString());
                return View();
            }
            CarServiceCategory carServiceCategory = db.CarServiceCategories.Find(id);
            if (carServiceCategory == null)
            {
                return HttpNotFound();
            }
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "اضافة او تعديل فئات خدمات السيارات ",
                EnAction = "AddEdit",
                ControllerName = "CarServiceCategory",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = id
            });

            ViewBag.Next = QueryHelper.Next((int)id, "CarServiceCategory");
            ViewBag.Previous = QueryHelper.Previous((int)id, "CarServiceCategory");
            ViewBag.Last = QueryHelper.GetLast("CarServiceCategory");
            ViewBag.First = QueryHelper.GetFirst("CarServiceCategory");
            return View(carServiceCategory);
        }

        [HttpPost]
        public ActionResult AddEdit(CarServiceCategory carServiceCategory, string newBtn)
        {
            if (ModelState.IsValid)
            {
                var id = carServiceCategory.Id;
                carServiceCategory.IsDeleted = false;
                if (carServiceCategory.Id > 0)
                {
                    carServiceCategory.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    db.Entry(carServiceCategory).State = EntityState.Modified;
                    Notification.GetNotification("CarServiceCategory", "Edit", "AddEdit", carServiceCategory.Id, null, "فئات خدمات السيارات");
                }
                else
                {
                    carServiceCategory.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    carServiceCategory.Code = (QueryHelper.CodeLastNum("CarServiceCategory") + 1).ToString();
                    carServiceCategory.IsActive = true;
                    db.CarServiceCategories.Add(carServiceCategory);

                    Notification.GetNotification("CarServiceCategory", "Add", "AddEdit", carServiceCategory.Id, null, "فئات خدمات السيارات");
                }

                try
                {
                    db.SaveChanges();
                }
                catch (Exception ex)
                {
                    var mod = ModelState.First(c => c.Key == "Code");  // this
                    mod.Value.Errors.Add("هذا الكود موجود من قبل");
                    return View(carServiceCategory);
                }
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = id > 0 ? "تعديل فئات خدمات السيارات" : "اضافة فئات خدمات السيارات",
                    EnAction = "AddEdit",
                    ControllerName = "CarServiceCategory",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    CodeOrDocNo = carServiceCategory.Code
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

            return View(carServiceCategory);
        }

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                CarServiceCategory carServiceCategory = db.CarServiceCategories.Find(id);
                carServiceCategory.IsDeleted = true;
                carServiceCategory.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                db.Entry(carServiceCategory).State = EntityState.Modified;

                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "حذف فئات خدمات السيارات",
                    EnAction = "AddEdit",
                    ControllerName = "CarServiceCategory",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    EnItemName = carServiceCategory.EnName

                });
                Notification.GetNotification("CarServiceCategory", "Delete", "Delete", id, null, "فئات خدمات السيارات");


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
                CarServiceCategory carServiceCategory = db.CarServiceCategories.Find(id);
                if (carServiceCategory.IsActive == true)
                {
                    carServiceCategory.IsActive = false;
                }
                else
                {
                    carServiceCategory.IsActive = true;
                }
                carServiceCategory.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                db.Entry(carServiceCategory).State = EntityState.Modified;

                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = (bool)carServiceCategory.IsActive ? "تنشيط فئات خدمات السيارات" : "إلغاء تنشيط فئات خدمات السيارات",
                    EnAction = "AddEdit",
                    ControllerName = "CarServiceCategory",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = carServiceCategory.Id,
                    EnItemName = carServiceCategory.EnName,
                    ArItemName = carServiceCategory.ArName,
                    CodeOrDocNo = carServiceCategory.Code
                });
                if (carServiceCategory.IsActive == true)
                {
                    Notification.GetNotification("CarServiceCategory", "Activate/Deactivate", "ActivateDeactivate", id, true, "فئات خدمات السيارات");
                }
                else
                {

                    Notification.GetNotification("CarServiceCategory", "Activate/Deactivate", "ActivateDeactivate", id, false, " فئات خدمات السيارات");
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
            var code = QueryHelper.CodeLastNum("CarServiceCategory");
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