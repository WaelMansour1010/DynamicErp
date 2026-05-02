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

namespace MyERP.Controllers.SystemSettings
{
    public class ServiceSettingController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();
        
        // GET: ServiceSetting/Edit/5
        public ActionResult AddEdit()
        {
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح اعدادات الخدمات",
                ControllerName = "ServiceSetting",

                EnAction = "AddEdit",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            ////-------------------- Notification-------------------------////
            Notification.GetNotification("ServiceSetting", "Edit", "AddEdit", null, null, "اعدادات الخدمات");
            
             var Count = db.ServiceSettings.Count();
            
            if (Count < 1)
            {
                ServiceSetting NewObj = new ServiceSetting();

                return View(NewObj);
            }

            try
            {
                return View(db.ServiceSettings.FirstOrDefault());
            }
            catch { 
                    return HttpNotFound();
            }
            
        }

        // POST: ServiceSetting/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddEdit([Bind(Include = "Id,CompanyFees,MinimumCharge,PointValue,AllowedDistance,OrderFeesPercentage")] ServiceSetting serviceSetting)
        {
            var Count = db.ServiceSettings.Count();
            if (ModelState.IsValid)
            {
                var id = serviceSetting.Id;
                if (Count>0)
                {
                    db.Entry(serviceSetting).State = EntityState.Modified;
                }
                else
                {
                    db.ServiceSettings.Add(serviceSetting);

                }
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = id> 0 ? "اعدادات الخدمات" : "اضافة اعدادات الخدمات",
                    EnAction = "AddEdit",
                    ControllerName = "ServiceSettings",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                   
                });
                ////-------------------- Notification-------------------------////
                Notification.GetNotification("ServiceSetting", "Edit", "AddEdit", id, null, "اعدادات الخدمات");

                //////////-----------------------------------------------------------------------
                return RedirectToAction("AddEdit");

            }
            return View(serviceSetting);
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
