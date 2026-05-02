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
    [ERPAuthorize]
    

   
    public class ServiceController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        // GET: Service

        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة الخدمات",
                EnAction = "Index",
                ControllerName = "Service",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            ////-------------------- Notification-------------------------////
            Notification.GetNotification("Service", "View", "Index", null, null, "الخدمات");

            //////////////////-----------------------------------------------------------------------
            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;
            /////////////////////////// Search ////////////////////

            IQueryable<Service> service;

            if (string.IsNullOrEmpty(searchWord))
            {
                service = db.Services.Where(s => s.IsDeleted == false).OrderBy(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.Services.Where(c => c.IsDeleted == false).Count();

            }
            else
            {
                service = db.Services.Where(s => s.IsDeleted == false && (s.Code.Contains(searchWord) || s.ArServiceName.Contains(searchWord) ||
                                                                      s.EnServiceName.ToString().Contains(searchWord) || s.MinBalance.ToString().Contains(searchWord) || s.Points.ToString().Contains(searchWord) || s.CompanyPercentage.ToString().Contains(searchWord) || s.Notes.Contains(searchWord))).OrderBy(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = service.Count();

                ///////////////////////////////////////////////////////////////////////////////

            }
            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            return View(service.ToList());

        }
        [SkipERPAuthorize]
        public JsonResult SetCodeNum()
        {
            var code = QueryHelper.CodeLastNum("Service");
            return Json(code + 1, JsonRequestBehavior.AllowGet);
        }
   

        public ActionResult AddEdit(int? id)
        {  
            //TypeId==>ParentId
            if (id == null)
            {
                Service NewObj = new Service();


                //ViewBag.CategoryId = new SelectList(db.ServicesCategories.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new {
                //    Id = b.Id,
                //    ArName = b.Code + " - " + b.ArName
                //}), "Id", "ArName");
                ViewBag.TypeId = new SelectList(db.Services.Where(a => (a.Type != 3 && a.IsActive == true && a.IsDeleted == false)).Select(b => new {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArServiceName
                }), "Id", "ArName");
                return View(NewObj);
            }
           

            Service service = db.Services.Find(id);

            if (service == null)
            {
                return HttpNotFound();
            }

            //ViewBag.CategoryId = new SelectList(db.ServicesCategories.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new {
            //    Id = b.Id,
            //    ArName = b.Code + " - " + b.ArName
            //}), "Id", "ArName",service.CategoryId);


            ViewBag.TypeId = new SelectList(db.Services.Where(a => (a.Type != 3 && a.IsActive == true && a.IsDeleted == false)).Select(b => new {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArServiceName
            }), "Id", "ArName", service.TypeId);

            ViewBag.Next = QueryHelper.Next((int)id, "Service");
            ViewBag.Previous = QueryHelper.Previous((int)id, "Service");
            ViewBag.Last = QueryHelper.GetLast("Service");
            ViewBag.First = QueryHelper.GetFirst("Service");


            QueryHelper.AddLog(new MyLog()
            {

                ArAction = "فتح تفاصيل الخدمات",
                ControllerName = "Service",

                EnAction = "AddEdit",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = service.Id,
                ArItemName = service.ArServiceName,
                EnItemName = service.EnServiceName,
                CodeOrDocNo = service.Code
            });

            return View(service);
            //
        }

        // POST: Service/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddEdit([Bind(Include = "Id,ArServiceName,EnServiceName,CategoryId,IsActive,IsDeleted,Notes,UserId,Image,Code,MinBalance,Points,CompanyPercentage,Type,TypeId")] Service service, string newBtn)
        {
            if (ModelState.IsValid)
            {
                var id = service.Id;
                service.IsDeleted = false;
                if (service.Id > 0)
                {
                    if (service.Type == 1)
                    {
                        service.TypeId = null;
                        service.Points = null;
                        service.CompanyPercentage = null;
                    }
                    else if(service.Type == 2)
                    {
                        service.Points = null;
                        service.CompanyPercentage = null;
                    }
                    service.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                    db.Entry(service).State = EntityState.Modified;

                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("Service", "Edit", "AddEdit", id, null, "الخدمات");
                    /////////////-----------------------------------------------------------------------
                }
                else
                {
                    service.IsActive = true;
                    if (service.Type == 1)
                    {
                        service.TypeId = null;

                    }
                    service.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);


                    db.Services.Add(service);
                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("Service", "Add", "AddEdit", service.Id, null, "الخدمات");

                   ////////////////-----------------------------------------------------------------------

                }
                try
                {
                    db.SaveChanges();
                }
                catch (Exception ex)
                {

                    var mod = ModelState.First(c => c.Key == "Code");  // this

                    mod.Value.Errors.Add("هذا الكود موجود من قبل");

                    ViewBag.TypeId = new SelectList(db.Services.Where(a => (a.Type != 3 && a.IsActive == true && a.IsDeleted == false)).Select(b => new {
                        Id = b.Id,
                        ArName = b.Code + " - " + b.ArServiceName
                    }), "Id", "ArServiceName");
                    return View(service);
                }

                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = id> 0 ? "تعديل الخدمات" : "اضافة الخدمات",
                    EnAction = "AddEdit",
                    ControllerName = "Service",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = service.Id > 0 ? service.Id : db.Services.Max(i => i.Id),
                    ArItemName = service.ArServiceName,
                    EnItemName = service.EnServiceName,
                    CodeOrDocNo = service.Code
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

            else
            {
                var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .Select(x => new { x.Key, x.Value.Errors })
                    .ToArray();
                ViewBag.TypeId = new SelectList(db.Services.Where(a => a.Type != 3 && a.IsActive == true && a.IsDeleted == false), "Id", "ArServiceName",service.TypeId);
                return View(service);
            }
        }
        
        // POST: Service/Delete/5
        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                Service service = db.Services.Find(id);
                service.IsDeleted = true;
                db.Entry(service).State = EntityState.Modified;

                db.SaveChanges();


                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "حذف الخدمات",
                    EnAction = "AddEdit",
                    ControllerName = "Service",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = service.Id,
                    EnItemName = service.EnServiceName,
                    ArItemName = service.ArServiceName,
                    CodeOrDocNo = service.Code
                });
                ////-------------------- Notification-------------------------////
                Notification.GetNotification("Service", "Delete", "Delete", id, null, "الخدمات");

            //////////////////-----------------------------------------------------------------------

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
                Service service = db.Services.Find(id);
                if (service.IsActive == true)
                {
                    service.IsActive = false;
                }
                else
                {
                    service.IsActive = true;
                }

                db.Entry(service).State = EntityState.Modified;

                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = (bool)service.IsActive ? "تنشيط الخدمات" : "إلغاء الخدمات",
                    EnAction = "AddEdit",
                    ControllerName = "Service",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = service.Id,
                    EnItemName = service.EnServiceName,
                    ArItemName = service.ArServiceName,
                    CodeOrDocNo = service.Code
                });
                ////-------------------- Notification-------------------------////
                if (service.IsActive == true)
                {
                    Notification.GetNotification("Service", "Activate/Deactivate", "ActivateDeactivate", id, true, "الخدمات");
                }
                else
                {

                    Notification.GetNotification("Service", "Activate/Deactivate", "ActivateDeactivate", id, false, "الخدمات");
                }
                //////////////-----------------------------------------------------------------------

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
