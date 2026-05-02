using MyERP.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;

namespace MyERP.Controllers.SystemSettings
{
    public class ActivityController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        // GET: Activity
        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "قائمة الأنشطة",
                EnAction = "Index",
                ControllerName = "Activity",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });

            //--------------------- Notification --------------------//
            Notification.GetNotification("Activity", "View", "Index", null, null, "الأنشطة");

            //////////////////////////////////////////////////////////////////////////////////////
            //-------------------------------paging--------------------------------//
            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;

            //////////////////////////////////////////////////////////////////////////////////////
            //------------------------------- Search ------------------------------------------------//
            IQueryable<Activity> activities;
            if (string.IsNullOrEmpty(searchWord))
            {
                activities = db.Activities.Where(s => s.IsDeleted == false).OrderBy(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.Activities.Where(c => c.IsDeleted == false).Count();
            }
            else
            {
                activities = db.Activities.Where(s => s.IsDeleted == false && (s.ArName.Contains(searchWord) || s.EnName.Contains(searchWord) || s.Code.Contains(searchWord))).OrderBy(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.Activities.Where(s => s.IsDeleted == false && (s.ArName.Contains(searchWord) || s.EnName.Contains(searchWord) || s.Code.Contains(searchWord))).OrderBy(s => s.Id).Count();
            }
            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            return View(activities.ToList());
        }

        //////////////////////////////////////////////////////////////////////////////////////
        [SkipERPAuthorize]
        public JsonResult SetCodeNum()
        {
            var code = QueryHelper.CodeLastNum("Activity");
            return Json(code + 1, JsonRequestBehavior.AllowGet);
        }

        //////////////////////////////////////////////////////////////////////////////////////
        //--------------------------------- Add Or Edit -----------------------------------//
        public ActionResult AddEdit(int? id)
        {
            if (id == null)
            {
                var Code = SetCodeNum();
                ViewBag.Code = int.Parse(Code.Data.ToString());
                return View();
            }
            Activity activity = db.Activities.Find(id);

            if (activity == null)
            {
                return HttpNotFound();
            }

            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "اضافة او تعديل الأنشطة ",
                EnAction = "AddEdit",
                ControllerName = "Activity",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = id
            });
            ViewBag.Next = QueryHelper.Next((int)id, "Activity");
            ViewBag.Previous = QueryHelper.Previous((int)id, "Activity");
            ViewBag.Last = QueryHelper.GetLast("Activity");
            ViewBag.First = QueryHelper.GetFirst("Activity");


            return View(activity);
        }

        [HttpPost]
        public ActionResult AddEdit(Activity activity, string newBtn, HttpPostedFileBase upload)
        {
            string domainName = Request.Url.GetLeftPart(UriPartial.Authority);
            if (ModelState.IsValid)
            {
                var id = activity.Id;
                activity.IsDeleted = false;
                activity.IsActive = true;
                if (activity.Id > 0)
                {
                    activity.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    if (upload != null)
                    {
                        var folder = Server.MapPath("~/images/Activity/");
                        if (!Directory.Exists(folder))
                        {
                            Directory.CreateDirectory(folder);
                        }
                        upload.SaveAs(Server.MapPath("/images/Activity/") + upload.FileName);
                        activity.Image = domainName + ("/images/Activity/") + upload.FileName;
                    }
                    db.Entry(activity).State = EntityState.Modified;
                    ////-------------------- Notification for any edit -------------------------////
                    Notification.GetNotification("Activity", "Edit", "AddEdit", id, null, "الأنشطة");
                }
                else
                {
                    activity.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    activity.Code = (QueryHelper.CodeLastNum("Activity") + 1).ToString();
                    if (upload != null)
                    {
                        var folder = Server.MapPath("~/images/Activity/");
                        if (!Directory.Exists(folder))
                        {
                            Directory.CreateDirectory(folder);
                        }
                        upload.SaveAs(Server.MapPath("/images/Activity/") + upload.FileName);
                        activity.Image = domainName + ("/images/Activity/") + upload.FileName;
                    }
                    db.Activities.Add(activity);
                    ////-------------------- Notification for adding new shift -------------------------////
                    Notification.GetNotification("Activity", "Add", "AddEdit", activity.Id, null, "الأنشطة");
                }

                try
                {
                    db.SaveChanges();
                }
                catch (Exception ex)
                {
                    var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .Select(x => new { x.Key, x.Value.Errors })
                    .ToArray();
                }
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = id > 0 ? "تعديل الأنشطة" : "اضافة الأنشطة",
                    EnAction = "AddEdit",
                    ControllerName = "Activity",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    CodeOrDocNo = activity.Code

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

            return View(activity);
        }

        [HttpPost, ActionName("Delete")]

        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                Activity activity = db.Activities.Find(id);
                activity.IsDeleted = true;
                activity.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                db.Entry(activity).State = EntityState.Modified;
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "حذف الأنشطة",
                    EnAction = "AddEdit",
                    ControllerName = "Activity",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id
                });
                ////-------------------- Notification-------------------------////
                Notification.GetNotification("Activity", "Delete", "Delete", id, null, "الأنشطة");
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
                Activity activity = db.Activities.Find(id);
                if (activity.IsActive == true)
                {
                    activity.IsActive = false;
                }
                else
                {
                    activity.IsActive = true;
                }
                activity.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                db.Entry(activity).State = EntityState.Modified;

                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = (bool)activity.IsActive ? "تعديل الأنشطة" : "اضافة الأنشطة",
                    EnAction = "AddEdit",
                    ControllerName = "Activity",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = activity.Id,
                });
                ////-------------------- Notification-------------------------////
                if (activity.IsActive == true)
                {

                    Notification.GetNotification("Activity", "Activate/Deactivate", "ActivateDeactivate", id, true, "الأنشطة");
                }
                else
                {

                    Notification.GetNotification("Activity", "Activate/Deactivate", "ActivateDeactivate", id, false, "الأنشطة");
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