using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using MyERP.Models;

namespace MyERP.Controllers
{
    public class PageActionsController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        // GET: PageActions
        public ActionResult Index()
        {
            var pageActions = db.PageActions.Include(p => p.SystemPage);
            return View(pageActions.ToList());
        }

        // GET: PageActions/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            PageAction pageAction = db.PageActions.Find(id);
            if (pageAction == null)
            {
                return HttpNotFound();
            }
            return View(pageAction);
        }

        // GET: PageActions/Create
        public ActionResult Create()
        {
            ViewBag.PageId = new SelectList(db.SystemPages, "Id", "Code");
            return View();
        }

        // POST: PageActions/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Id,PageId,Action,EnName,ArName,IsActive")] PageAction pageAction)
        {
            if (ModelState.IsValid)
            {
                db.PageActions.Add(pageAction);
                db.SaveChanges();
                return RedirectToAction("Create");

            }

            ViewBag.PageId = new SelectList(db.SystemPages, "Id", "Code", pageAction.PageId);
            return View(pageAction);
        }

        // GET: PageActions/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            PageAction pageAction = db.PageActions.Find(id);
            if (pageAction == null)
            {
                return HttpNotFound();
            }
            ViewBag.PageId = new SelectList(db.SystemPages, "Id", "Code", pageAction.PageId);
            return View(pageAction);
        }

        // POST: PageActions/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Id,PageId,Action,EnName,ArName,IsActive")] PageAction pageAction)
        {
            if (ModelState.IsValid)
            {
                db.Entry(pageAction).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.PageId = new SelectList(db.SystemPages, "Id", "Code", pageAction.PageId);
            return View(pageAction);
        }

        // GET: PageActions/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            PageAction pageAction = db.PageActions.Find(id);
            if (pageAction == null)
            {
                return HttpNotFound();
            }
            return View(pageAction);
        }

        // POST: PageActions/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            PageAction pageAction = db.PageActions.Find(id);
            db.PageActions.Remove(pageAction);
            db.SaveChanges();
            return RedirectToAction("Index");
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
