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
    public class CarColorController : Controller
    { 
        private MySoftERPEntity db = new MySoftERPEntity();

        // GET: CarColor    
        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة الوان السيارات",
                EnAction = "Index",
                ControllerName = "CarColor",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });

            Notification.GetNotification("CarColor", "View", "Index", null, null, "الوان السيارات");

            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;
            if (pageIndex > 1)
            {
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;
            }

            IQueryable<CarColor> carColors;
            if (string.IsNullOrEmpty(searchWord))
            {
                carColors = db.CarColors.Where(a => a.IsDeleted == false).OrderBy(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.CarColors.Where(a => a.IsDeleted == false).Count();
            }
            else
            {
                carColors = db.CarColors.Where(a => a.IsDeleted == false &&
                (a.ArName.Contains(searchWord) || a.EnName.Contains(searchWord) || a.Code.Contains(searchWord)))
                    .OrderBy(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.CarColors.Where(a => a.IsDeleted == false && (a.ArName.Contains(searchWord) || a.EnName.Contains(searchWord) || a.Code.Contains(searchWord))).Count();
            }
            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;

            return View(carColors.ToList());
        }

        public ActionResult AddEdit(int? id)
        {
            if (id == null)
            {
                var Code = SetCodeNum();
                ViewBag.Code = int.Parse(Code.Data.ToString());
                return View();
            }
            CarColor carColor = db.CarColors.Find(id);
            if (carColor == null)
            {
                return HttpNotFound();
            }
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "اضافة او تعديل الوان السيارات ",
                EnAction = "AddEdit",
                ControllerName = "CarColor",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = id
            });

            ViewBag.Next = QueryHelper.Next((int)id, "CarColor");
            ViewBag.Previous = QueryHelper.Previous((int)id, "CarColor");
            ViewBag.Last = QueryHelper.GetLast("CarColor");
            ViewBag.First = QueryHelper.GetFirst("CarColor");
            return View(carColor);
        }

        [HttpPost]
        public ActionResult AddEdit(CarColor carColor, string newBtn)
        {
            if (ModelState.IsValid)
            {
                var id = carColor.Id;
                carColor.IsDeleted = false;
                if (carColor.Id > 0)
                {
                    carColor.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    db.Entry(carColor).State = EntityState.Modified;
                    Notification.GetNotification("CarColor", "Edit", "AddEdit", carColor.Id, null, "الوان السيارات");
                }
                else
                {
                    carColor.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    carColor.Code = (QueryHelper.CodeLastNum("CarColor") + 1).ToString();
                    carColor.IsActive = true;
                    db.CarColors.Add(carColor);

                    Notification.GetNotification("CarColor", "Add", "AddEdit", carColor.Id, null, "الوان السيارات");
                }

                try
                {
                    db.SaveChanges();
                }
                catch (Exception ex)
                {
                    var mod = ModelState.First(c => c.Key == "Code");  // this
                    mod.Value.Errors.Add("هذا الكود موجود من قبل");
                    return View(carColor);
                }
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = id > 0 ? "تعديل لون سيارة" : "اضافة لون سيارة",
                    EnAction = "AddEdit",
                    ControllerName = "CarColor",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    CodeOrDocNo = carColor.Code
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

            return View(carColor);
        }

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                CarColor carColor = db.CarColors.Find(id);
                carColor.IsDeleted = true;
                carColor.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                db.Entry(carColor).State = EntityState.Modified;

                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "حذف لون سيارة",
                    EnAction = "AddEdit",
                    ControllerName = "CarColor",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    EnItemName = carColor.EnName

                });
                Notification.GetNotification("CarColor", "Delete", "Delete", id, null, "الوان السيارات");


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
                CarColor carColor = db.CarColors.Find(id);
                if (carColor.IsActive == true)
                {
                    carColor.IsActive = false;
                }
                else
                {
                    carColor.IsActive = true;
                }
                carColor.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                db.Entry(carColor).State = EntityState.Modified;

                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = (bool)carColor.IsActive ? "تنشيط الوان السيارات" : "إلغاء تنشيط الوان السيارات",
                    EnAction = "AddEdit",
                    ControllerName = "CarColor",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = carColor.Id,
                    EnItemName = carColor.EnName,
                    ArItemName = carColor.ArName,
                    CodeOrDocNo = carColor.Code
                });
                if (carColor.IsActive == true)
                {
                    Notification.GetNotification("CarColor", "Activate/Deactivate", "ActivateDeactivate", id, true, "الوان السيارات");
                }
                else
                {

                    Notification.GetNotification("CarColor", "Activate/Deactivate", "ActivateDeactivate", id, false, " الوان السيارات");
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
            var code = QueryHelper.CodeLastNum("CarColor");
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