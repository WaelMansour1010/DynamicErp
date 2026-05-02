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

namespace MyERP.Controllers
{
    
    [ERPAuthorize]
    public class CustomerSourceTypeController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        // GET: CustomerSourceType
        public ActionResult Index()
        {
            return View(db.CustomerSourceTypes.Where(c=>c.IsDeleted==false).ToList());
        }
        
        // GET: CustomerSourceType/Edit/5
        public ActionResult AddEdit(int? id)
        {
            if (id == null)
            {
                return View();
            }
            CustomerSourceType customerSourceType = db.CustomerSourceTypes.Find(id);
            if (customerSourceType == null)
            {
                return HttpNotFound();
            }
            ViewBag.Next = QueryHelper.Next((int)id, "CustomerSourceType");
            ViewBag.Previous = QueryHelper.Previous((int)id, "CustomerSourceType");
            ViewBag.Last = QueryHelper.GetLast("CustomerSourceType");
            ViewBag.First = QueryHelper.GetFirst("CustomerSourceType");
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "تفاصيل مصدر العميل",
                EnAction = "AddEdit",
                ControllerName = "Departments",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = customerSourceType.Id,
                ArItemName = customerSourceType.ArName,
                EnItemName = customerSourceType.EnName
            });
            return View(customerSourceType);
        }

        // POST: CustomerSourceType/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]

        public ActionResult AddEdit([Bind(Include = "Id,ArName,EnName,IsActive,IsDeleted")] CustomerSourceType customerSourceType, string newBtn)
        {
            if (ModelState.IsValid)
            {
                var id = customerSourceType.Id;
                customerSourceType.IsDeleted = false;
                customerSourceType.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                if (id>0)
                {
                    db.Entry(customerSourceType).State = EntityState.Modified;
                    
                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("CustomerSourceType", "Edit", "AddEdit", id, null, "مصدر العميل");
                }
                else
                {
                    customerSourceType.IsActive = true;
                    db.CustomerSourceTypes.Add(customerSourceType);
                    Notification.GetNotification("CustomerSourceType", "Add", "AddEdit", customerSourceType.Id, null, "مصدر العميل");
                }
                
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = id > 0 ? "تعديل مصدر العميل" : "اضافة مصدر العميل",
                    EnAction = "AddEdit",
                    ControllerName = "CustomerSourceType",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = customerSourceType.Id ,
                    ArItemName = customerSourceType.ArName,
                    EnItemName = customerSourceType.EnName
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
            return View(customerSourceType);
        }

        // POST: CustomerSourceType/Delete/5
        [HttpPost, ActionName("Delete")]
        //[ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
CustomerSourceType customerSourceType = db.CustomerSourceTypes.Find(id);
                customerSourceType.IsDeleted = true;
                customerSourceType.UserId= int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                db.Entry(customerSourceType).State = EntityState.Modified;
            db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "حذف مصدر العميل",
                    EnAction = "AddEdit",
                    ControllerName = "CustomerSourceType",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    EnItemName = customerSourceType.EnName,
                    ArItemName = customerSourceType.ArName
                });
                ////-------------------- Notification-------------------------////
                Notification.GetNotification("CustomerSourceType", "Delete", "Delete", id, null, "مصدر العميل");
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
                CustomerSourceType customerSourceType = db.CustomerSourceTypes.Find(id);
                if (customerSourceType.IsActive == true)
                {
                    customerSourceType.IsActive = false;
                }
                else
                {
                    customerSourceType.IsActive = true;
                }
                customerSourceType.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                db.Entry(customerSourceType).State = EntityState.Modified;

                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = (bool)customerSourceType.IsActive ? "تنشيط مصدر العميل" : "إلغاء مصدر العميل",
                    EnAction = "AddEdit",
                    ControllerName = "CustomerSourceType",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = customerSourceType.Id,
                    EnItemName = customerSourceType.EnName,
                    ArItemName = customerSourceType.ArName
                });
                ////-------------------- Notification-------------------------////
                if (customerSourceType.IsActive == true)
                {
                    Notification.GetNotification("CustomerSourceType", "Activate/Deactivate", "ActivateDeactivate", id, true, "مصدر العميل");
                }
                else
                {

                    Notification.GetNotification("CustomerSourceType", "Activate/Deactivate", "ActivateDeactivate", id, false, "مصدر العميل");
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
