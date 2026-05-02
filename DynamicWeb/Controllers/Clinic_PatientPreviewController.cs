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
    public class Clinic_PatientPreviewController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        // GET: Clinic_PatientPreview
        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            /*  QueryHelper.AddLog(new MyLog()
              {
                  ArAction = "فتح قائمة بيان حالة المريض",
                  EnAction = "Index",
                  ControllerName = "Clinic_PatientPreview",
                  UserName = User.Identity.Name,
                  UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                  LogDate = DateTime.Now,
                  RequestMethod = "GET"
              });

              Notification.GetNotification("Clinic_PatientPreview", "View", "Index", null, null, "بيان حالة المريض");

              ViewBag.PageIndex = pageIndex;
              int skipRowsNo = 0;
              if (pageIndex > 1)
              {
                  skipRowsNo = (pageIndex - 1) * wantedRowsNo;
              }

              IQueryable<Clinic_PatientPreview> Clinic_PatientPreview;
              if (string.IsNullOrEmpty(searchWord))
              {
                  Clinic_PatientPreview = db.Clinic_PatientPreview.Where(a => a.IsDeleted == false).OrderBy(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                  ViewBag.Count = db.Clinic_PatientPreview.Where(a => a.IsDeleted == false).Count();
              }
              else
              {
                  Clinic_PatientPreview = db.Clinic_PatientPreview.Where(a => a.IsDeleted == false &&
                  (a.ArName.Contains(searchWord) || a.EnName.Contains(searchWord) || a.Code.Contains(searchWord)))
                      .OrderBy(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                  ViewBag.Count = db.Clinic_PatientPreview.Where(a => a.IsDeleted == false && (a.ArName.Contains(searchWord) || a.EnName.Contains(searchWord) || a.Code.Contains(searchWord))).Count();
              }
              ViewBag.searchWord = searchWord;
              ViewBag.wantedRowsNo = wantedRowsNo;

              return View(Clinic_PatientPreview.ToList());*/
            return View();
        }

        //[SkipERPAuthorize]
        //public JsonResult SetCodeNum()
        //{
        //    var code = QueryHelper.CodeLastNum("Clinic_PatientPreview");
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
            Clinic_PatientPreview Clinic_PatientPreview = db.Clinic_PatientPreview.Find(id);
            if (Clinic_PatientPreview == null)
            {
                return HttpNotFound();
            }
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "اضافة او تعديل بيان حالة المريض ",
                EnAction = "AddEdit",
                ControllerName = "Clinic_PatientPreview",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = id
            });

            ViewBag.Next = QueryHelper.Next((int)id, "Clinic_PatientPreview");
            ViewBag.Previous = QueryHelper.Previous((int)id, "Clinic_PatientPreview");
            ViewBag.Last = QueryHelper.GetLast("Clinic_PatientPreview");
            ViewBag.First = QueryHelper.GetFirst("Clinic_PatientPreview");
            return View(Clinic_PatientPreview);*/
            return View();
        }

        /* [HttpPost]
       public ActionResult AddEdit(Clinic_PatientPreview Clinic_PatientPreview, string newBtn)
       {
          if (ModelState.IsValid)
            {
                var id = Clinic_PatientPreview.Id;
                Clinic_PatientPreview.IsDeleted = false;
                if (Clinic_PatientPreview.Id > 0)
                {
                    Clinic_PatientPreview.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    db.Entry(Clinic_PatientPreview).State = EntityState.Modified;
                    Notification.GetNotification("Clinic_PatientPreview", "Edit", "AddEdit", Clinic_PatientPreview.Id, null, "بيان حالة المريض");
                }
                else
                {
                    Clinic_PatientPreview.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    Clinic_PatientPreview.Code = (QueryHelper.CodeLastNum("Clinic_PatientPreview") + 1).ToString();
                    Clinic_PatientPreview.IsActive = true;
                    db.Clinic_PatientPreview.Add(Clinic_PatientPreview);

                    Notification.GetNotification("Clinic_PatientPreview", "Add", "AddEdit", Clinic_PatientPreview.Id, null, "بيان حالة المريض");
                }

                try
                {
                    db.SaveChanges();
                }
                catch (Exception ex)
                {
                    var mod = ModelState.First(c => c.Key == "Code");  // this
                    mod.Value.Errors.Add("هذا الكود موجود من قبل");
                    return View(Clinic_PatientPreview);
                }
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = id > 0 ? "تعديل بيان حالة المريض" : "اضافةبيان حالة المريض",
                    EnAction = "AddEdit",
                    ControllerName = "Clinic_PatientPreview",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    CodeOrDocNo = Clinic_PatientPreview.Code
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
            return View(Clinic_PatientPreview);
       }*/

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            /* try
             {
                 Clinic_PatientPreview Clinic_PatientPreview = db.Clinic_PatientPreview.Find(id);
                 Clinic_PatientPreview.IsDeleted = true;
                 Clinic_PatientPreview.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                 db.Entry(Clinic_PatientPreview).State = EntityState.Modified;

                 db.SaveChanges();
                 QueryHelper.AddLog(new MyLog()
                 {
                     ArAction = "حذف بيان حالة المريض",
                     EnAction = "AddEdit",
                     ControllerName = "Clinic_PatientPreview",
                     UserName = User.Identity.Name,
                     UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                     LogDate = DateTime.Now,
                     RequestMethod = "POST",
                     SelectedItem = id,
                     EnItemName = Clinic_PatientPreview.EnName

                 });
                 Notification.GetNotification("Clinic_PatientPreview", "Delete", "Delete", id, null, "بيان حالة المريض");


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
        //        Clinic_PatientPreview Clinic_PatientPreview = db.Clinic_PatientPreview.Find(id);
        //        if (Clinic_PatientPreview.IsActive == true)
        //        {
        //            Clinic_PatientPreview.IsActive = false;
        //        }
        //        else
        //        {
        //            Clinic_PatientPreview.IsActive = true;
        //        }
        //        Clinic_PatientPreview.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

        //        db.Entry(Clinic_PatientPreview).State = EntityState.Modified;

        //        db.SaveChanges();
        //        QueryHelper.AddLog(new MyLog()
        //        {
        //            ArAction = (bool)Clinic_PatientPreview.IsActive ? "تنشيط بيان حالة المريض" : "إلغاء تنشيط بيان حالة المريض",
        //            EnAction = "AddEdit",
        //            ControllerName = "Clinic_PatientPreview",
        //            UserName = User.Identity.Name,
        //            UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
        //            LogDate = DateTime.Now,
        //            RequestMethod = "POST",
        //            SelectedItem = Clinic_PatientPreview.Id,
        //            EnItemName = Clinic_PatientPreview.EnName,
        //            ArItemName = Clinic_PatientPreview.ArName,
        //            CodeOrDocNo = Clinic_PatientPreview.Code
        //        });
        //        if (Clinic_PatientPreview.IsActive == true)
        //        {
        //            Notification.GetNotification("Clinic_PatientPreview", "Activate/Deactivate", "ActivateDeactivate", id, true, "بيان حالة المريض");
        //        }
        //        else
        //        {

        //            Notification.GetNotification("Clinic_PatientPreview", "Activate/Deactivate", "ActivateDeactivate", id, false, "بيان حالة المريض");
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