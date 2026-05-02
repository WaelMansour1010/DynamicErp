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
    public class RecordVisitingDatesController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        // GET: RecordVisitingDates
        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة تسجيل مواعيد الزيارات",
                EnAction = "Index",
                ControllerName = "RecordVisitingDates",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });

            Notification.GetNotification("RecordVisitingDates", "View", "Index", null, null, "تسجيل مواعيد الزيارات");

            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;
            if (pageIndex > 1)
            {
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;
            }

            IQueryable<CRM_RecordVisitingDates> cRM_RecordVisitingDates;
            if (string.IsNullOrEmpty(searchWord))
            {
                cRM_RecordVisitingDates = db.CRM_RecordVisitingDates.Where(a => a.IsDeleted == false).OrderBy(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.CRM_RecordVisitingDates.Where(a => a.IsDeleted == false).Count();
            }
            else
            {
                cRM_RecordVisitingDates = db.CRM_RecordVisitingDates.Where(a => a.IsDeleted == false &&
                (a.ArName.Contains(searchWord) || a.EnName.Contains(searchWord) || a.Code.Contains(searchWord)))
                    .OrderBy(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.CRM_RecordVisitingDates.Where(a => a.IsDeleted == false && (a.ArName.Contains(searchWord) || a.EnName.Contains(searchWord) || a.Code.Contains(searchWord))).Count();
            }
            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;

            return View(cRM_RecordVisitingDates.ToList());
        }

        public ActionResult AddEdit(int? id)
        {
            if (id == null)
            {
                var Code = SetCodeNum();
                ViewBag.Code = int.Parse(Code.Data.ToString());
                return View();
            }
            CRM_RecordVisitingDates cRM_RecordVisitingDates = db.CRM_RecordVisitingDates.Find(id);
            if (cRM_RecordVisitingDates == null)
            {
                return HttpNotFound();
            }
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "اضافة او تعديل تسجيل مواعيد الزيارات",
                EnAction = "AddEdit",
                ControllerName = "RecordVisitingDates",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = id
            });

            ViewBag.Next = QueryHelper.Next((int)id, "CRM_RecordVisitingDates");
            ViewBag.Previous = QueryHelper.Previous((int)id, "CRM_RecordVisitingDates");
            ViewBag.Last = QueryHelper.GetLast("CRM_RecordVisitingDates");
            ViewBag.First = QueryHelper.GetFirst("CRM_RecordVisitingDates");

            return View(cRM_RecordVisitingDates);
        }

        [HttpPost]
        public ActionResult AddEdit(CRM_RecordVisitingDates cRM_RecordVisitingDates, string newBtn)
        {
            if (ModelState.IsValid)
            {
                var id = cRM_RecordVisitingDates.Id;
                cRM_RecordVisitingDates.IsDeleted = false;
                if (cRM_RecordVisitingDates.Id > 0)
                {
                    cRM_RecordVisitingDates.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    db.Entry(cRM_RecordVisitingDates).State = EntityState.Modified;
                    Notification.GetNotification("RecordVisitingDates", "Edit", "AddEdit", cRM_RecordVisitingDates.Id, null, "تسجيل مواعيد الزيارات");
                }
                else
                {
                    cRM_RecordVisitingDates.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    cRM_RecordVisitingDates.Code = (QueryHelper.CodeLastNum("CRM_RecordVisitingDates") + 1).ToString();
                    cRM_RecordVisitingDates.IsActive = true;
                    db.CRM_RecordVisitingDates.Add(cRM_RecordVisitingDates);

                    Notification.GetNotification("RecordVisitingDates", "Add", "AddEdit", cRM_RecordVisitingDates.Id, null, "تسجيل مواعيد الزيارات");
                }

                try
                {
                    db.SaveChanges();
                }
                catch (Exception ex)
                {
                    var mod = ModelState.First(c => c.Key == "Code");  // this
                    mod.Value.Errors.Add("هذا الكود موجود من قبل");
                    return View(cRM_RecordVisitingDates);
                }
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = id > 0 ? "تعديل تسجيل مواعيد الزيارات" : "اضافة تسجيل مواعيد الزيارات",
                    EnAction = "AddEdit",
                    ControllerName = "RecordVisitingDates",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    CodeOrDocNo = cRM_RecordVisitingDates.Code
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
            return View(cRM_RecordVisitingDates);
        }

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                CRM_RecordVisitingDates cRM_RecordVisitingDates = db.CRM_RecordVisitingDates.Find(id);
                cRM_RecordVisitingDates.IsDeleted = true;
                cRM_RecordVisitingDates.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                db.Entry(cRM_RecordVisitingDates).State = EntityState.Modified;

                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "حذف تسجيل مواعيد الزيارات",
                    EnAction = "AddEdit",
                    ControllerName = "RecordVisitingDates",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    EnItemName = cRM_RecordVisitingDates.EnName

                });
                Notification.GetNotification("RecordVisitingDates", "Delete", "Delete", id, null, "تسجيل مواعيد الزيارات");


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
                CRM_RecordVisitingDates cRM_RecordVisitingDates = db.CRM_RecordVisitingDates.Find(id);
                if (cRM_RecordVisitingDates.IsActive == true)
                {
                    cRM_RecordVisitingDates.IsActive = false;
                }
                else
                {
                    cRM_RecordVisitingDates.IsActive = true;
                }
                cRM_RecordVisitingDates.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                db.Entry(cRM_RecordVisitingDates).State = EntityState.Modified;

                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = (bool)cRM_RecordVisitingDates.IsActive ? "تنشيط تسجيل مواعيد الزيارات" : "إلغاء تنشيط تسجيل مواعيد الزيارات",
                    EnAction = "AddEdit",
                    ControllerName = "RecordVisitingDates",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = cRM_RecordVisitingDates.Id,
                    EnItemName = cRM_RecordVisitingDates.EnName,
                    ArItemName = cRM_RecordVisitingDates.ArName,
                    CodeOrDocNo = cRM_RecordVisitingDates.Code
                });
                if (cRM_RecordVisitingDates.IsActive == true)
                {
                    Notification.GetNotification("RecordVisitingDates", "Activate/Deactivate", "ActivateDeactivate", id, true, "تسجيل مواعيد الزيارات");
                }
                else
                {

                    Notification.GetNotification("RecordVisitingDates", "Activate/Deactivate", "ActivateDeactivate", id, false, "تسجيل مواعيد الزيارات");
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
            var code = QueryHelper.CodeLastNum("CRM_RecordVisitingDates");
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