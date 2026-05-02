using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;
using MyERP.Models;

namespace MyERP.Controllers
{
    public class SystemPagesController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        // GET: SystemPages
        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "", bool OnlyModules = false)
        {
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة صفحات النظام",
                EnAction = "Index",
                ControllerName = "SystemPages",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            ////-------------------- Notification-------------------------////
            Notification.GetNotification("SystemPages", "View", "Index", null, null, "صفحات النظام");
            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;

            IQueryable<SystemPage> systemPages;

            if (string.IsNullOrEmpty(searchWord))
            {
                if (OnlyModules == true)
                {

                    systemPages = db.SystemPages.Where(a => a.IsModule == true && a.ParentId == null && a.Id != 2116).OrderBy(s => s.Id)
                       .Union(db.SystemPages.Where(a => a.ParentId == 2116 && a.IsModule == true).OrderBy(s => s.Id));
                    ViewBag.Count = db.SystemPages.Where(a => a.IsModule == true && a.ParentId == null && a.Id != 2116).OrderBy(s => s.Id)
                       .Union(db.SystemPages.Where(a => a.ParentId == 2116 && a.IsModule == true).OrderBy(s => s.Id)).Count();
                   
                }
                else
                {
                    systemPages = db.SystemPages.OrderBy(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                    ViewBag.Count = db.SystemPages.Count();
                }

            }
            else
            {
                if (OnlyModules == true)
                {
                    systemPages = db.SystemPages.Where(s => s.IsModule == true && s.ParentId == null && s.Id != 2116 && (s.Code.Contains(searchWord) || s.ArName.Contains(searchWord) || s.EnName.Contains(searchWord))).OrderBy(s => s.Id).Union(db.SystemPages.Where(s => s.ParentId == 2116 && s.IsModule == true).OrderBy(s => s.Id)).Skip(skipRowsNo).Take(wantedRowsNo);
                    ViewBag.Count = systemPages.Where(s => s.IsModule == true && s.ParentId == null && s.Id != 2116 && (s.Code.Contains(searchWord) || s.ArName.Contains(searchWord) || s.EnName.Contains(searchWord))).OrderBy(s => s.Id).Union(db.SystemPages.Where(s => s.ParentId == 2116 && s.IsModule == true).OrderBy(s => s.Id)).Count();
                }
                else
                {
                    systemPages = db.SystemPages.Where(s => s.IsModule == true && (s.Code.Contains(searchWord) || s.ArName.Contains(searchWord) || s.EnName.Contains(searchWord))).OrderBy(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                    ViewBag.Count = systemPages.Count();
                }

            }
            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            ViewBag.check = OnlyModules;
            return View(systemPages.ToList());
        }

        // GET: SystemPages/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            SystemPage systemPage = db.SystemPages.Find(id);
            if (systemPage == null)
            {
                return HttpNotFound();
            }
            return View(systemPage);
        }

        // GET: SystemPages/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: SystemPages/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Id,Code,ArName,EnName,IsMasterFile,TableName,ControllerName")] SystemPage systemPage)
        {
            if (ModelState.IsValid)
            {
                db.SystemPages.Add(systemPage);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(systemPage);
        }

        // GET: SystemPages/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            SystemPage systemPage = db.SystemPages.Find(id);
            if (systemPage == null)
            {
                return HttpNotFound();
            }
            return View(systemPage);
        }

        // POST: SystemPages/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Id,Code,ArName,EnName,IsMasterFile,TableName,ControllerName")] SystemPage systemPage)
        {
            if (ModelState.IsValid)
            {
                db.Entry(systemPage).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(systemPage);
        }

        // GET: SystemPages/Delete/5
        /*    public ActionResult Delete(int? id)
            {
                if (id == null)
                {
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
                }
                SystemPage systemPage = db.SystemPages.Find(id);
                if (systemPage == null)
                {
                    return HttpNotFound();
                }
                return View(systemPage);
            }*/

        // POST: SystemPages/Delete/5
        /*  [HttpPost, ActionName("Delete")]
          [ValidateAntiForgeryToken]
          public ActionResult DeleteConfirmed(int id)
          {
              SystemPage systemPage = db.SystemPages.Find(id);
              db.SystemPages.Remove(systemPage);
              db.SaveChanges();
              return RedirectToAction("Index");
          }*/

        //[HttpPost, ActionName("Delete")]
        ////   [ValidateAntiForgeryToken]
        //public ActionResult Delete(int? id)
        //{
        //    try
        //    {
        //        SystemPage systemPages = db.SystemPages.Find(id);
        //        if (systemPages.IsDeleted == true)
        //        {
        //            systemPages.IsDeleted = false;
        //        }
        //        else
        //        {
        //            systemPages.IsDeleted = true;
        //        }

        //        db.Entry(systemPages).State = EntityState.Modified;

        //        db.SaveChanges();
        //        QueryHelper.AddLog(new MyLog()
        //        {
        //            ArAction = (bool)systemPages.IsActive ? "حذف صفحات النظام" : "إلغاء حذف صفحات النظام",
        //            EnAction = "AddEdit",
        //            ControllerName = "SystemPages",
        //            UserName = User.Identity.Name,
        //            UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
        //            LogDate = DateTime.Now,
        //            RequestMethod = "POST",
        //            SelectedItem = systemPages.Id,
        //            EnItemName = systemPages.EnName,
        //            ArItemName = systemPages.ArName,
        //            CodeOrDocNo = systemPages.Code
        //        });
        //        if (systemPages.IsActive == true)
        //        {
        //            Notification.GetNotification("SystemPages", "Delete", "Delete", id, true, "صفحات النظام");
        //        }
        //        else
        //        {

        //            Notification.GetNotification("SystemPages", "Delete", "Delete", id, false, "صفحات النظام");
        //        }

        //        return Content("true");
        //    }
        //    catch (Exception ex)
        //    {
        //        return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
        //    }

        //}


        [HttpPost]
        public ActionResult ActivateDeactivate(int id)
        {
            try
            {
                // SystemPage systemPages = db.SystemPages.Find(id);
                var systemPages = db.SystemPages.Where(a =>a.ParentId == id || a.Id==id).ToList();
                foreach (var item in systemPages)
                {


                    if (item.IsActive == true)
                    {
                        item.IsActive = false;
                    }
                    else
                    {
                        item.IsActive = true;
                    }

                    db.Entry(item).State = EntityState.Modified;

                    QueryHelper.AddLog(new MyLog()
                    {
                        ArAction = (bool)item.IsActive ? "تنشيط صفحات النظام" : "إلغاء تنشيط صفحات النظام",
                        EnAction = "AddEdit",
                        ControllerName = "SystemPages",
                        UserName = User.Identity.Name,
                        UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                        LogDate = DateTime.Now,
                        RequestMethod = "POST",
                        SelectedItem = item.Id,
                        EnItemName = item.EnName,
                        ArItemName = item.ArName,
                        CodeOrDocNo = item.Code
                    });
                    if (item.IsActive == true)
                    {
                        Notification.GetNotification("SystemPages", "Activate/Deactivate", "ActivateDeactivate", id, true, "صفحات النظام");
                    }
                    else
                    {

                        Notification.GetNotification("SystemPages", "Activate/Deactivate", "ActivateDeactivate", id, false, "صفحات النظام");
                    }
                }
                db.SaveChanges();

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
