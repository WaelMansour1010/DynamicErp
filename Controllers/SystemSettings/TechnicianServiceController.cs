using System;
using System.Collections.Generic;
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
    

    public class TechnicianServiceController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        // GET: TechnicianService
        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة خدمات الفنيين",
                EnAction = "Index",
                ControllerName = "TechnicianService",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            //////-------------------- Notification-------------------------////
            Notification.GetNotification("TechnicianService", "View", "Index", null, null, "خدمات الفنيين");
            //////////////////-----------------------------------------------------------------------
            ViewBag.TechanicianId = new SelectList(db.Techanicians.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName");
            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;

            /////////////////////////// Search ////////////////////

            IQueryable<TechanicianService> techanicianServices;

            if (string.IsNullOrEmpty(searchWord))
            {
                techanicianServices = db.TechanicianServices.Where(s => s.IsDeleted == false).OrderBy(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);

                ViewBag.Count = db.TechanicianServices.Where(c => c.IsDeleted == false).Count();
            }
            else
            {
                techanicianServices = db.TechanicianServices.Where(s => s.IsDeleted == false && (s.Techanician.ArName.Contains(searchWord) || s.ServicesCategory.ArName.Contains(searchWord) || s.Notes.Contains(searchWord))).OrderBy(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count =techanicianServices.Count();

                ///////////////////////////////////////////////////////////////////////////////

            }
            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            return View(techanicianServices.ToList());

        }

        [SkipERPAuthorize]
        public ActionResult GetAllServices(int id)
        {
            return PartialView(db.GetCustomerOB(id));
        }
        public ActionResult AddEdit(int? id)
        {
            if (id == null)
            {
                TechanicianService NewObj = new TechanicianService();
                ViewBag.TechanicianId = new SelectList(db.Techanicians.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                ViewBag.ServiceId= new SelectList(db.ServicesCategories.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                return View(NewObj);
            }
            TechanicianService TechanicianService = db.TechanicianServices.Find(id);


            if (TechanicianService == null)
            {
                return HttpNotFound();
            }
            ViewBag.TechanicianId = new SelectList(db.Techanicians.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName",TechanicianService.TechanicianId);
            ViewBag.ServiceId = new SelectList(db.ServicesCategories.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName",TechanicianService.ServiceId);


            ViewBag.Next = QueryHelper.Next((int)id, "TechanicianService");
            ViewBag.Previous = QueryHelper.Previous((int)id, "TechanicianService");
            ViewBag.Last = QueryHelper.GetLast("TechanicianService");
            ViewBag.First = QueryHelper.GetFirst("TechanicianService");


            QueryHelper.AddLog(new MyLog()
            {

                ArAction = "فتح تفاصيل خدمات الفنيين",
                ControllerName = "TechnicianService",

                EnAction = "AddEdit",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = TechanicianService.Id,
                ArItemName = TechanicianService.ServicesCategory.ArName,
              
            });

            return View(TechanicianService);


        }

        // POST: Techanician/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddEdit( TechanicianService TechanicianService, string newBtn)
        {
            if (ModelState.IsValid)
            {
                var id = TechanicianService.Id;
                TechanicianService.IsDeleted = false;
                if (TechanicianService.Id > 0)
                {
                    TechanicianService.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    db.Entry(TechanicianService).State = EntityState.Modified;

                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("TechnicianService", "Edit", "AddEdit", id, null, "خدمات الفنيين");

               
                    //////////////////-----------------------------------------------------------------------
                }
                else
                {
                    TechanicianService.IsActive = true;

                    TechanicianService.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);


                    db.TechanicianServices.Add(TechanicianService);

                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("TechnicianService", "Add", "AddEdit", TechanicianService.Id, null, "خدمات الفنيين");

                 

                    ////////////////-----------------------------------------------------------------------
                }

                db.SaveChanges();
               
            

                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = id > 0 ? "تعديل خدمات الفنيين" : "اضافة خدمات الفنيين",
                    EnAction = "AddEdit",
                    ControllerName = "TechnicianService",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = TechanicianService.Id,
                
                   
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


                
                ViewBag.TechanicianId = new SelectList(db.Techanicians.Where(a => a.IsActive == true && a.IsDeleted == false), "Id", "ArName", TechanicianService.TechanicianId);
                ViewBag.ServiceId = new SelectList(db.ServicesCategories.Where(a => a.IsActive == true && a.IsDeleted == false), "Id", "ArName", TechanicianService.ServiceId);

                return View(TechanicianService);
            }
        }

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                TechanicianService TechanicianService = db.TechanicianServices.Find(id);
                TechanicianService.IsDeleted = true;
                db.Entry(TechanicianService).State = EntityState.Modified;

                db.SaveChanges();


                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "حذف خدمات الفنيين",
                    EnAction = "AddEdit",
                    ControllerName = "technicianService",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = TechanicianService.Id,
                    EnItemName = TechanicianService.ServicesCategory.EnName,
                    
                });
                ////-------------------- Notification-------------------------////
                Notification.GetNotification("technicianService", "Delete", "Delete", id, null, "خدمات الفنيين");

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
                TechanicianService TechanicianService = db.TechanicianServices.Find(id);
                if (TechanicianService.IsActive == true)
                {
                    TechanicianService.IsActive = false;
                }
                else
                {
                    TechanicianService.IsActive = true;
                }

                db.Entry(TechanicianService).State = EntityState.Modified;

                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = (bool)TechanicianService.IsActive ? "تنشيط خدمات الفنيين" : "إلغاء خدمات الفنيين",
                    EnAction = "AddEdit",
                    ControllerName = "TechnicianService",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = TechanicianService.Id,
                    ArItemName = TechanicianService.ServicesCategory.ArName
                });
                ////-------------------- Notification-------------------------////
                if (TechanicianService.IsActive == true)
                {
                    Notification.GetNotification("TechnicianService", "Activate/Deactivate", "ActivateDeactivate", id, true, "خدمات الفنيين");
                }
                else
                {

                    Notification.GetNotification("TechnicianService", "Activate/Deactivate", "ActivateDeactivate", id, false, "خدمات الفنيين");
                }
                
                //////////////////-----------------------------------------------------------------------

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