using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using MyERP.Models;
using System.Security.Claims;

namespace MyERP.Controllers.SystemSettings
{

    public class CouponController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح كوبونات الخصم",
                EnAction = "Index",
                ControllerName = "Coupon",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            ////-------------------- Notification-------------------------////
            Notification.GetNotification("Coupon", "View", "Index", null, null, "كوبونات الخصم");
            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;

            IQueryable<Coupon> Coupons;

            if (string.IsNullOrEmpty(searchWord))
            {
                Coupons = db.Coupons.Where(s => s.IsDeleted == false).OrderBy(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.Coupons.Where(s => s.IsDeleted == false).Count();
            }
            else
            {
                Coupons = db.Coupons.Where(s => s.IsDeleted == false && (s.CouponCode.Contains(searchWord) || s.NoteAr.Contains(searchWord))).OrderBy(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = Coupons.Count();
            }
            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            return View(Coupons.ToList());


        }

        //[SkipERPAuthorize]
        //public JsonResult SetCodeNum()
        //{
        //    var code = QueryHelper.CodeLastNum("Coupon");
        //    return Json(code + 1, JsonRequestBehavior.AllowGet);
        //}

        public ActionResult AddEdit(int? id)
        {

            if (id == null)
            {
                Coupon NewObj = new Coupon();

                return View(NewObj);
            }
            Coupon Coupon = db.Coupons.Find(id);
            if (Coupon == null)
            {
                return HttpNotFound();
            }
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح تفاصيل كوبون الخصم ",
                EnAction = "AddEdit",
                ControllerName = "Coupon",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = Coupon.Id,
                ArItemName = Coupon.NoteAr,
                EnItemName = Coupon.NoteEn,
                CodeOrDocNo = Coupon.CouponCode
            });
            ViewBag.Next = QueryHelper.Next((int)id, "Coupon");
            ViewBag.Previous = QueryHelper.Previous((int)id, "Coupon");
            ViewBag.Last = QueryHelper.GetLast("Coupon");
            ViewBag.First = QueryHelper.GetFirst("Coupon");
            return View(Coupon);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddEdit(Coupon Coupon, string newBtn)
        {
            if (ModelState.IsValid)
            {
                Coupon.IsDeleted = false;
                Coupon.NoteKr = string.IsNullOrEmpty(Coupon.NoteKr) ? "" : Coupon.NoteKr;
                Coupon.NoteEn = string.IsNullOrEmpty(Coupon.NoteEn) ? "" : Coupon.NoteEn;

                if (Coupon.Id > 0)
                {
                    Coupon.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                    db.Entry(Coupon).State = EntityState.Modified;

                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("Coupon", "Edit", "AddEdit", Coupon.Id, null, "كوبونات الخصم");
                }
                else
                {
                    Coupon.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                    Coupon.IsActive = true;
                    db.Coupons.Add(Coupon);

                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("Coupon", "Add", "AddEdit", Coupon.Id, null, "كوبونات الخصم");

                }

                try
                {
                    db.SaveChanges();
                }
                catch (Exception ex)
                {

                    var mod = ModelState.First(c => c.Key == "CouponCode");  // this

                    mod.Value.Errors.Add("هذا الكود موجود من قبل");
                    return View(Coupon);
                }
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = Coupon.Id > 0 ? "تعديل كوبونات الخصم" : "اضافة كوبون خصم",
                    EnAction = "AddEdit",
                    ControllerName = "Coupon",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = Coupon.Id,
                    ArItemName = Coupon.NoteAr,
                    EnItemName = Coupon.NoteEn,
                    CodeOrDocNo = Coupon.CouponCode
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

            return View(Coupon);
        }


        [HttpPost, ActionName("Delete")]

        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                Coupon Coupon = db.Coupons.Find(id);
                Coupon.IsDeleted = true;
                Coupon.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                db.Entry(Coupon).State = EntityState.Modified;
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "حذف كوبونات الخصم",
                    EnAction = "AddEdit",
                    ControllerName = "Coupon",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    EnItemName = Coupon.NoteEn,
                    ArItemName = Coupon.NoteAr,
                    CodeOrDocNo = Coupon.CouponCode
                });
                ////-------------------- Notification-------------------------////
                Notification.GetNotification("Coupon", "Delete", "Delete", id, null, "كوبونات الخصم");
                return Content("true");
            }
            catch (Exception)
            {

                throw;
            }
        }

        [HttpPost]
        public ActionResult ActivateDeactivate(int id)
        {
            try
            {
                Coupon Coupon = db.Coupons.Find(id);
                if (Coupon.IsActive == true)
                {
                    Coupon.IsActive = false;
                }
                else
                {
                    Coupon.IsActive = true;
                }
                Coupon.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                db.Entry(Coupon).State = EntityState.Modified;

                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = (bool)Coupon.IsActive ? "تنشيط كوبونات الخصم" : "إلغاء تنشيط كوبونات الخصم",
                    EnAction = "AddEdit",
                    ControllerName = "Coupon",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = Coupon.Id,
                    EnItemName = Coupon.NoteEn,
                    ArItemName = Coupon.NoteAr,
                    CodeOrDocNo = Coupon.CouponCode
                });
                ////-------------------- Notification-------------------------////
                if (Coupon.IsActive == true)
                {
                    Notification.GetNotification("Coupon", "Activate/Deactivate", "ActivateDeactivate", id, true, "كوبونات الخصم");
                }
                else
                {

                    Notification.GetNotification("Coupon", "Activate/Deactivate", "ActivateDeactivate", Coupon.Id, false, "كوبونات الخصم");
                }


                return Content("true");
            }
            catch (Exception ex)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
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
