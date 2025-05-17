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
    

    public class ServiceCategoriesController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        // GET: ServiceCategories
        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            ////////////////// LOG    ///////////////////////// 
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة انواع الخدمات",
                EnAction = "Index",
                ControllerName = "ServiceCategories",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });

            ////-------------------- Notification-------------------------////
            Notification.GetNotification("ServiceCategories", "View", "Index", null, null, "انواع الخدمات");

            //////////////////-----------------------------------------------------------------------
            
            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;

            /////////////////////////// Search ////////////////////

            IQueryable<ServicesCategory> servicesCategories;

            if (string.IsNullOrEmpty(searchWord))
            {
                servicesCategories = db.ServicesCategories.Where(s => s.IsDeleted == false).OrderBy(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
               
                ViewBag.Count = db.ServicesCategories.Where(c => c.IsDeleted == false).Count();

            }
            else
            {
                servicesCategories = db.ServicesCategories.Where(s => s.IsDeleted == false && (s.Code.Contains(searchWord) || s.ArName.Contains(searchWord) ||
                                                         s.EnName.ToString().Contains(searchWord) || s.Price.ToString().Contains(searchWord) ||  s.Notes.Contains(searchWord))).OrderBy(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = servicesCategories.Count();

                ///////////////////////////////////////////////////////////////////////////////

            }
            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            return View(servicesCategories.ToList());

            }




        //[SkipERPAuthorize]
        //public JsonResult SetCodeNum()
        //{
        //    var code = QueryHelper.CodeLastNum("ServicesCategory");
        //    return Json(code + 1, JsonRequestBehavior.AllowGet);
        //}
        // POST: ServiceCategories/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
      
        // GET: ServiceCategories/Edit/5
        public ActionResult AddEdit(int? id)
        {
            if (id == null)
            {
                ServicesCategory NewObj=new ServicesCategory();

                ViewBag.ParentId = new SelectList(db.ServicesCategories.Where(a => (a.Type != 3 && a.IsActive == true && a.IsDeleted == false)).Select(b => new {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");


             

                return View(NewObj);
            }
            ServicesCategory servicesCategory = db.ServicesCategories.Find(id);
            if (servicesCategory == null)
            {
                return HttpNotFound();
            }


            ViewBag.ParentId = new SelectList(db.ServicesCategories.Where(a => (a.Type != 3 && a.IsActive == true && a.IsDeleted == false)).Select(b => new {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName",servicesCategory.ParentId);

            ViewBag.Next = QueryHelper.Next((int)id, "ServicesCategory");
            ViewBag.Previous = QueryHelper.Previous((int)id, "ServicesCategory");
            ViewBag.Last = QueryHelper.GetLast("ServicesCategory");
            ViewBag.First = QueryHelper.GetFirst("ServicesCategory");


            QueryHelper.AddLog(new MyLog()
            {
               
                ArAction = "فتح تفاصيل انواع الخدمات",
                ControllerName = "ServiceCategories",

                EnAction = "AddEdit",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = servicesCategory.Id,
                ArItemName = servicesCategory.ArName,
                EnItemName = servicesCategory.EnName,
                CodeOrDocNo = servicesCategory.Code
            });

            return View(servicesCategory);

        }

        // POST: ServiceCategories/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        //[ValidateAntiForgeryToken]
        public ActionResult AddEdit(ServicesCategory servicesCategory, string newBtn, HttpPostedFileBase upload)
        {
            string domainName = Request.Url.GetLeftPart(UriPartial.Authority);
            if (servicesCategory.HasDetails==null)
            {
                servicesCategory.HasDetails = false;
            }
            if (ModelState.IsValid)
            {
                var id = servicesCategory.Id;
                servicesCategory.IsDeleted = false;
                if (servicesCategory.Id > 0)
                {
                    if (servicesCategory.Type == 1)
                    {
                        servicesCategory.ParentId = null;

                    }
                    servicesCategory.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    if (upload != null)
                    {
                        upload.SaveAs(Server.MapPath("/images/ServiceCategoriesImages/") + upload.FileName);

                        servicesCategory.Image = domainName + ("/images/ServiceCategoriesImages/") + upload.FileName;

                    }
                    db.Entry(servicesCategory).State = EntityState.Modified;

                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("ServiceCategories", "Edit", "AddEdit", id, null, "انواع الخدمات");
/////-------------------------------------------------------------
                }
                else
                {
                    servicesCategory.IsActive = true;
                    if (servicesCategory.Type == 1)
                    {
                        servicesCategory.ParentId = null;

                    }
                    if (upload != null)
                    {
                        upload.SaveAs(Server.MapPath("/images/ServiceCategoriesImages/") + upload.FileName);

                        servicesCategory.Image = domainName + ("/images/ServiceCategoriesImages/") + upload.FileName;

                    }
                    servicesCategory.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);


                    db.ServicesCategories.Add(servicesCategory);

                    ////-------------------- Notification-------------------------////

                    Notification.GetNotification("ServiceCategories", "Add", "AddEdit", servicesCategory.Id, null, "انواع الخدمات");

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
                    ViewBag.ParentId = new SelectList(db.ServicesCategories.Where(c => c.Type != 3 && c.IsActive == true), "Id", "ArName");

                    return View(servicesCategory);
                }

                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = id > 0 ? "تعديل انواع الخدمات" : "اضافة انواع الخدمات",
                    EnAction = "AddEdit",
                    ControllerName = "ServiceCategories",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = servicesCategory.Id ,
                    ArItemName = servicesCategory.ArName,
                    EnItemName = servicesCategory.EnName,
                    CodeOrDocNo = servicesCategory.Code
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
                ViewBag.ParentId = new SelectList(db.ServicesCategories.Where(a => (a.Type != 3 && a.IsActive == true && a.IsDeleted == false)).Select(b => new {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");

                return View(servicesCategory);
            }

        }

        // GET: ServiceCategories/Delete/5


        // POST: ServiceCategories/Delete/5
        [HttpPost, ActionName("Delete")]
        
        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                ServicesCategory servicesCategory = db.ServicesCategories.Find(id);
                servicesCategory.IsDeleted = true;
                db.Entry(servicesCategory).State = EntityState.Modified;
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "حذف انواع الخدمات",
                    EnAction = "AddEdit",
                    ControllerName = "ServiceCategories",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = servicesCategory.Id,
                    EnItemName = servicesCategory.EnName,
                    ArItemName = servicesCategory.ArName,
                    CodeOrDocNo = servicesCategory.Code
                });
                ////-------------------- Notification-------------------------////
                Notification.GetNotification("ServiceCategories", "Delete", "Delete", id, null, "انواع الخدمات");

               /////////////-----------------------------------------------------------------------

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
                ServicesCategory servicesCategory = db.ServicesCategories.Find(id);
                if (servicesCategory.IsActive == true)
                {
                    servicesCategory.IsActive = false;
                }
                else
                {
                    servicesCategory.IsActive = true;
                }

                db.Entry(servicesCategory).State = EntityState.Modified;

                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = (bool)servicesCategory.IsActive ? "تنشيط انواع الخدمات" : "إلغاء انواع الخدمات",
                    EnAction = "AddEdit",
                    ControllerName = "ServiceCategories",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = servicesCategory.Id,
                    EnItemName = servicesCategory.EnName,
                    ArItemName = servicesCategory.ArName,
                    CodeOrDocNo = servicesCategory.Code
                });
                if (servicesCategory.IsActive == true)
                {
                    Notification.GetNotification("ServiceCategories", "Activate/Deactivate", "ActivateDeactivate", id, true, "انواع الخدمات");
                }
                else
                {

                    Notification.GetNotification("ServiceCategories", "Activate/Deactivate", "ActivateDeactivate", id, false, "انواع الخدمات");
                }
                
                ///////////////-----------------------------------------------------------------------

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
