using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;
using MyERP.Models;
using System.Web.Script.Serialization;
using MyERP.Repository;
using System.Data.Entity.Core.Objects;
using System.IO;

namespace MyERP.Controllers
{
    public class ChurchFatherController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();
        // GET: ChurchFather
        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة الأباء",
                EnAction = "Index",
                ControllerName = "ChurchFather",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            Notification.GetNotification("ChurchFather", "View", "Index", null, null, "الأباء");
            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;
            if (pageIndex > 1)
            {
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;
            }
            IQueryable<ChurchFather> fathers;
            if (string.IsNullOrEmpty(searchWord))
            {
                fathers = db.ChurchFathers.Where(a => a.IsDeleted == false).OrderBy(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.ChurchFathers.Where(a => a.IsDeleted == false).Count();
            }
            else
            {
                fathers = db.ChurchFathers.Where(a => a.IsDeleted == false &&
                (a.ArName.Contains(searchWord) || a.EnName.Contains(searchWord) || a.Code.Contains(searchWord)))
                    .OrderBy(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.ChurchFathers.Where(a => a.IsDeleted == false && (a.ArName.Contains(searchWord) || a.EnName.Contains(searchWord) || a.Code.Contains(searchWord))).Count();
            }
            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            return View(fathers.ToList());
        }
        public ActionResult AddEdit(int? id)
        {
            if (id == null)
            {
                ViewBag.ERPUserId = new SelectList(db.ERPUsers.Where(a => a.IsActive == true && a.IsDeleted == false).Select(a => new {
                    a.Id,
                    ArName = a.UserName
                }), "Id", "ArName");
                return View();
            }
            ChurchFather father = db.ChurchFathers.Find(id);
            if (father == null)
            {
                return HttpNotFound();
            }
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "اضافة او تعديل الأباء ",
                EnAction = "AddEdit",
                ControllerName = "ChurchFather",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = id
            });
            ViewBag.Next = QueryHelper.Next((int)id, "ChurchFather");
            ViewBag.Previous = QueryHelper.Previous((int)id, "ChurchFather");
            ViewBag.Last = QueryHelper.GetLast("ChurchFather");
            ViewBag.First = QueryHelper.GetFirst("ChurchFather");

            ViewBag.ERPUserId = new SelectList(db.ERPUsers.Where(a => a.IsActive == true && a.IsDeleted == false).Select(a => new {
                a.Id,
                ArName = a.UserName 
            }), "Id", "ArName", father.ERPUserId);


            return View(father);
        }
        [HttpPost]
        public ActionResult AddEdit(ChurchFather father)
        {
            if (ModelState.IsValid)
            {
                var id = father.Id;
                father.IsDeleted = false;
                father.IsActive = true;
                father.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                string domainName = Request.Url.GetLeftPart(UriPartial.Authority);
                if (father.Id > 0)
                {
                   db.Entry(father).State = EntityState.Modified;
                    var IsLinkedWithUser = db.ChurchFathers.Where(a => a.IsActive == true && a.IsDeleted == false && a.ERPUserId == father.ERPUserId&&a.Id!=father.Id).FirstOrDefault() != null ? true : false;
                    if (IsLinkedWithUser == true)
                    {
                        return Json(new { success = "Exist" });

                    }
                    Notification.GetNotification("ChurchFather", "Edit", "AddEdit", father.Id, null, "الأباء");
                }
                else
                {
                    father.Code = (QueryHelper.CodeLastNum("ChurchFather") + 1).ToString();
                    db.ChurchFathers.Add(father);
                    Notification.GetNotification("ChurchFather", "Add", "AddEdit", father.Id, null, "الأباء");
                    var IsLinkedWithUser = db.ChurchFathers.Where(a => a.IsActive == true && a.IsDeleted == false && a.ERPUserId == father.ERPUserId).FirstOrDefault() != null ? true : false;
                    if (IsLinkedWithUser == true)
                    {
                        return Json(new { success = "Exist" });

                    }
                }
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = id > 0 ? "تعديل الأباء" : "اضافة الأباء",
                    EnAction = "AddEdit",
                    ControllerName = "ChurchFather",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    CodeOrDocNo = father.Code
                });
                return Json(new { success = true });
            }
            var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .Select(x => new { x.Key, x.Value.Errors })
                    .ToArray();
            return Json(new { success = false });
        }

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                ChurchFather father = db.ChurchFathers.Find(id);
                father.IsDeleted = true;
                father.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                db.Entry(father).State = EntityState.Modified;
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "حذف الأباء",
                    EnAction = "AddEdit",
                    ControllerName = "ChurchFather",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    EnItemName = father.EnName
                });
                Notification.GetNotification("ChurchFather", "Delete", "Delete", id, null, "الأباء");
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
                ChurchFather father = db.ChurchFathers.Find(id);
                if (father.IsActive == true)
                {
                    father.IsActive = false;
                }
                else
                {
                    father.IsActive = true;
                }
                father.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                db.Entry(father).State = EntityState.Modified;
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = (bool)father.IsActive ? "تنشيط الأباء" : "إلغاء تنشيط الأباء",
                    EnAction = "AddEdit",
                    ControllerName = "ChurchFather",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = father.Id,
                    EnItemName = father.EnName,
                    ArItemName = father.ArName,
                    CodeOrDocNo = father.Code
                });
                if (father.IsActive == true)
                {
                    Notification.GetNotification("ChurchFather", "Activate/Deactivate", "ActivateDeactivate", id, true, "الأباء");
                }
                else
                {
                    Notification.GetNotification("ChurchFather", "Activate/Deactivate", "ActivateDeactivate", id, false, " الأباء");
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
            var code = QueryHelper.CodeLastNum("ChurchFather");
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