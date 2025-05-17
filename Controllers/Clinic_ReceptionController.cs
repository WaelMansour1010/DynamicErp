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
    public class Clinic_ReceptionController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        // GET: Clinic_Reception
        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            /*  QueryHelper.AddLog(new MyLog()
              {
                  ArAction = "فتح قائمة الإستقبال",
                  EnAction = "Index",
                  ControllerName = "Clinic_Reception",
                  UserName = User.Identity.Name,
                  UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                  LogDate = DateTime.Now,
                  RequestMethod = "GET"
              });

              Notification.GetNotification("Clinic_Reception", "View", "Index", null, null, "الإستقبال");

              ViewBag.PageIndex = pageIndex;
              int skipRowsNo = 0;
              if (pageIndex > 1)
              {
                  skipRowsNo = (pageIndex - 1) * wantedRowsNo;
              }

              IQueryable<Clinic_Reception> Clinic_Reception;
              if (string.IsNullOrEmpty(searchWord))
              {
                  Clinic_Reception = db.Clinic_Reception.Where(a => a.IsDeleted == false).OrderBy(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                  ViewBag.Count = db.Clinic_Reception.Where(a => a.IsDeleted == false).Count();
              }
              else
              {
                  Clinic_Reception = db.Clinic_Reception.Where(a => a.IsDeleted == false &&
                  (a.ArName.Contains(searchWord) || a.EnName.Contains(searchWord) || a.Code.Contains(searchWord)))
                      .OrderBy(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                  ViewBag.Count = db.Clinic_Reception.Where(a => a.IsDeleted == false && (a.ArName.Contains(searchWord) || a.EnName.Contains(searchWord) || a.Code.Contains(searchWord))).Count();
              }
              ViewBag.searchWord = searchWord;
              ViewBag.wantedRowsNo = wantedRowsNo;

              return View(Clinic_Reception.ToList());*/
            return View();
        }

        //[SkipERPAuthorize]
        //public JsonResult SetCodeNum()
        //{
        //    var code = QueryHelper.CodeLastNum("Clinic_Reception");
        //    return Json(code + 1, JsonRequestBehavior.AllowGet);
        //}


        public ActionResult AddEdit(int? id)
        {
            /*if (id == null)
            {
                var Code = SetCodeNum();
                ViewBag.Code = int.Parse(Code.Data.ToString());
                return View();
            }
            Clinic_Reception Clinic_Reception = db.Clinic_Reception.Find(id);
            if (Clinic_Reception == null)
            {
                return HttpNotFound();
            }
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "اضافة او تعديل الإستقبال ",
                EnAction = "AddEdit",
                ControllerName = "Clinic_Reception",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = id
            });

            ViewBag.Next = QueryHelper.Next((int)id, "Clinic_Reception");
            ViewBag.Previous = QueryHelper.Previous((int)id, "Clinic_Reception");
            ViewBag.Last = QueryHelper.GetLast("Clinic_Reception");
            ViewBag.First = QueryHelper.GetFirst("Clinic_Reception");
            return View(Clinic_Reception);*/
            return View();
        }

        /* [HttpPost]
       public ActionResult AddEdit(Clinic_Reception Clinic_Reception, string newBtn)
       {
          if (ModelState.IsValid)
            {
                var id = Clinic_Reception.Id;
                Clinic_Reception.IsDeleted = false;
                if (Clinic_Reception.Id > 0)
                {
                    Clinic_Reception.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    db.Entry(Clinic_Reception).State = EntityState.Modified;
                    Notification.GetNotification("Clinic_Reception", "Edit", "AddEdit", Clinic_Reception.Id, null, "الإستقبال");
                }
                else
                {
                    Clinic_Reception.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    Clinic_Reception.Code = (QueryHelper.CodeLastNum("Clinic_Reception") + 1).ToString();
                    Clinic_Reception.IsActive = true;
                    db.Clinic_Reception.Add(Clinic_Reception);

                    Notification.GetNotification("Clinic_Reception", "Add", "AddEdit", Clinic_Reception.Id, null, "الإستقبال");
                }

                try
                {
                    db.SaveChanges();
                }
                catch (Exception ex)
                {
                    var mod = ModelState.First(c => c.Key == "Code");  // this
                    mod.Value.Errors.Add("هذا الكود موجود من قبل");
                    return View(Clinic_Reception);
                }
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = id > 0 ? "تعديل الإستقبال" : "اضافةالإستقبال",
                    EnAction = "AddEdit",
                    ControllerName = "Clinic_Reception",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    CodeOrDocNo = Clinic_Reception.Code
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
            return View(Clinic_Reception);
       }*/


        public ActionResult AddEditCalendar()
        {
            return View();
        }

        [SkipERPAuthorize]
        public JsonResult GetDoctorCalendar()
        {
            var calendar = db.GetDoctorCalendar().ToList();
            return Json(calendar,JsonRequestBehavior.AllowGet);
        }


        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            /* try
             {
                 Clinic_Reception Clinic_Reception = db.Clinic_Reception.Find(id);
                 Clinic_Reception.IsDeleted = true;
                 Clinic_Reception.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                 db.Entry(Clinic_Reception).State = EntityState.Modified;

                 db.SaveChanges();
                 QueryHelper.AddLog(new MyLog()
                 {
                     ArAction = "حذف الإستقبال",
                     EnAction = "AddEdit",
                     ControllerName = "Clinic_Reception",
                     UserName = User.Identity.Name,
                     UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                     LogDate = DateTime.Now,
                     RequestMethod = "POST",
                     SelectedItem = id,
                     EnItemName = Clinic_Reception.EnName

                 });
                 Notification.GetNotification("Clinic_Reception", "Delete", "Delete", id, null, "الإستقبال");


                 return Content("true");
             }
             catch (Exception ex)
             {
                 return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
             }*/

            return Content("true");
        }
        //[HttpPost]
        //public ActionResult ActivateDeactivate(int id)
        //{
        //    try
        //    {
        //        Clinic_Reception Clinic_Reception = db.Clinic_Reception.Find(id);
        //        if (Clinic_Reception.IsActive == true)
        //        {
        //            Clinic_Reception.IsActive = false;
        //        }
        //        else
        //        {
        //            Clinic_Reception.IsActive = true;
        //        }
        //        Clinic_Reception.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

        //        db.Entry(Clinic_Reception).State = EntityState.Modified;

        //        db.SaveChanges();
        //        QueryHelper.AddLog(new MyLog()
        //        {
        //            ArAction = (bool)Clinic_Reception.IsActive ? "تنشيط الإستقبال" : "إلغاء تنشيط الإستقبال",
        //            EnAction = "AddEdit",
        //            ControllerName = "Clinic_Reception",
        //            UserName = User.Identity.Name,
        //            UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
        //            LogDate = DateTime.Now,
        //            RequestMethod = "POST",
        //            SelectedItem = Clinic_Reception.Id,
        //            EnItemName = Clinic_Reception.EnName,
        //            ArItemName = Clinic_Reception.ArName,
        //            CodeOrDocNo = Clinic_Reception.Code
        //        });
        //        if (Clinic_Reception.IsActive == true)
        //        {
        //            Notification.GetNotification("Clinic_Reception", "Activate/Deactivate", "ActivateDeactivate", id, true, "الإستقبال");
        //        }
        //        else
        //        {

        //            Notification.GetNotification("Clinic_Reception", "Activate/Deactivate", "ActivateDeactivate", id, false, "الإستقبال");
        //        }

        //        return Content("true");
        //    }
        //    catch (Exception ex)
        //    {
        //        return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
        //    }
        //}



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