using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;
using MyERP.Models;
using MyERP.Repository;

namespace MyERP.Controllers.PartyManagement
{
    public class SingerController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();
        // GET: Singer
        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة المطربين",
                EnAction = "Index",
                ControllerName = "Singer",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            Notification.GetNotification("Singer", "View", "Index", null, null, "المطربين");
            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;
            if (pageIndex > 1)
            {
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;
            }
            IQueryable<Singer> singers;
            if (string.IsNullOrEmpty(searchWord))
            {
                singers = db.Singers.Where(a => a.IsDeleted == false).OrderBy(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.Singers.Where(a => a.IsDeleted == false).Count();
            }
            else
            {
                singers = db.Singers.Where(a => a.IsDeleted == false &&
                (a.ArName.Contains(searchWord) || a.EnName.Contains(searchWord) || a.Code.Contains(searchWord)))
                    .OrderBy(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.Singers.Where(a => a.IsDeleted == false && (a.ArName.Contains(searchWord) || a.EnName.Contains(searchWord) || a.Code.Contains(searchWord))).Count();
            }
            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            return View(singers.ToList());
        }

        public ActionResult AddEdit(int? id)
        {
            if (id == null)
            {
               
                return View();
            }
            Singer singer = db.Singers.Find(id);
           
            if (singer == null)
            {
                return HttpNotFound();
            }
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "اضافة او تعديل المطربين ",
                EnAction = "AddEdit",
                ControllerName = "Singer",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = id
            });
            ViewBag.Next = QueryHelper.Next((int)id, "Singer");
            ViewBag.Previous = QueryHelper.Previous((int)id, "Singer");
            ViewBag.Last = QueryHelper.GetLast("Singer");
            ViewBag.First = QueryHelper.GetFirst("Singer");
            return View(singer);
        }
        [HttpPost]
        public ActionResult AddEdit(Singer singer, string newBtn, HttpPostedFileBase upload)
        {
            string domainName = Request.Url.GetLeftPart(UriPartial.Authority);

            if (ModelState.IsValid)
            {
                var id = singer.Id;
                singer.IsDeleted = false;
                singer.IsActive = true;
                singer.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                if (singer.Id > 0)
                {
                    if (upload != null)
                    {
                        upload.SaveAs(Server.MapPath("/images/PartyManagement/Singer/") + upload.FileName);

                        singer.Image = domainName + ("/images/PartyManagement/Singer/") + upload.FileName;

                    }
                    db.Entry(singer).State = EntityState.Modified;
                    Notification.GetNotification("Singer", "Edit", "AddEdit", singer.Id, null, "المطربين");
                }
                else
                {
                    if (upload != null)
                    {
                        upload.SaveAs(Server.MapPath("/images/PartyManagement/Singer/") + upload.FileName);

                        singer.Image = domainName + ("/images/PartyManagement/Singer/") + upload.FileName;
                    }
                    singer.Code = (QueryHelper.CodeLastNum("Singer") + 1).ToString();
                    db.Singers.Add(singer);
                    Notification.GetNotification("Singer", "Add", "AddEdit", singer.Id, null, "المطربين");
                }
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = id > 0 ? "تعديل المطربين" : "اضافة المطربين",
                    EnAction = "AddEdit",
                    ControllerName = "Singer",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    CodeOrDocNo = singer.Code
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
            var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .Select(x => new { x.Key, x.Value.Errors })
                    .ToArray();
            return View(singer);
        }

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                Singer singer = db.Singers.Find(id);
                singer.IsDeleted = true;
                singer.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                db.Entry(singer).State = EntityState.Modified;
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "حذف المطربين",
                    EnAction = "AddEdit",
                    ControllerName = "Singer",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id
                });
                Notification.GetNotification("Singer", "Delete", "Delete", id, null, "المطربين");
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
                Singer singer = db.Singers.Find(id);
                if (singer.IsActive == true)
                {
                    singer.IsActive = false;
                }
                else
                {
                    singer.IsActive = true;
                }
                singer.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                db.Entry(singer).State = EntityState.Modified;
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = (bool)singer.IsActive ? "تنشيط المطربين" : "إلغاء تنشيط المطربين",
                    EnAction = "AddEdit",
                    ControllerName = "Singer",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = singer.Id,
                    CodeOrDocNo = singer.Code
                });
                if (singer.IsActive == true)
                {
                    Notification.GetNotification("Singer", "Activate/Deactivate", "ActivateDeactivate", id, true, "المطربين");
                }
                else
                {
                    Notification.GetNotification("Singer", "Activate/Deactivate", "ActivateDeactivate", id, false, " المطربين");
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
            var code = QueryHelper.CodeLastNum("Singer");
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