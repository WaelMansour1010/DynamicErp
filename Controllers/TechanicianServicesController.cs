using MyERP.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MyERP.Controllers.SystemSettings
{
    public class TechanicianServicesController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        // GET: UserPrivilege/Edit/5
        public ActionResult Index()
        {
            //ViewBag.TechnicianId = new SelectList(db.Techanicians.Where(u => u.IsDeleted == false && u.IsActive == true).Select(b => new
            //{
            //    Id = b.Id,
            //    Name = b.ArName + " - " + b.Code
            //}), "Id", "Name");

            //ViewBag.ServiceCategoryId = new SelectList(db.ServicesCategories.Where(u => u.IsDeleted == false && u.IsActive == true && u.ParentId == null).Select(b => new
            //{
            //    Id = b.Id,
            //    Name = b.ArName + " - " + b.Code
            //}), "Id", "Name");

            ViewBag.EmployeeId = new SelectList(db.Employees.Where(u => u.IsDeleted == false && u.IsActive == true).Select(b => new
            {
                Id = b.Id,
                Name = b.ArName + " - " + b.Code
            }), "Id", "Name");

            return View();
        }
        [HttpPost]
        public ActionResult Index(List<TechanicianService> techanicianServiceList, /*int ParentId*/int EmployeeId)
        {
            if (ModelState.IsValid)
            {
                //Get only Checked ThirdLevel Services
                var ChoosenServices = techanicianServiceList.Where(a => a.Choosen == true).ToList();
                //var techServicesObj = techanicianServiceList.FirstOrDefault();
                //var techId = techServicesObj.TechanicianId;
                //MyXML.xPathName = "TechniciansServices";
                //var TechniciansServices = MyXML.GetXML(ChoosenServices);
                //db.TechnicianServices_Insert(ParentId, techId, TechniciansServices);

                db.TechanicianServices.RemoveRange(db.TechanicianServices.Where(r => r.EmployeeId == EmployeeId));
                db.TechanicianServices.AddRange(ChoosenServices);
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .Select(x => new { x.Key, x.Value.Errors })
                    .ToArray();
            //ViewBag.TechnicianId = new SelectList(db.Techanicians.Where(u => u.IsDeleted == false && u.IsActive == true).Select(b => new
            //{
            //    Id = b.Id,
            //    Name = b.ArName + " - " + b.Code
            //}), "Id", "Name");

            //ViewBag.ServicesCategoryId = new SelectList(db.ServicesCategories.Where(u => u.IsDeleted == false && u.IsActive == true&&u.ParentId==null).Select(b => new
            //{
            //    Id = b.Code,
            //    Name = b.ArName + " - " + b.Code
            //}), "Id", "Name");
            ViewBag.EmployeeId = new SelectList(db.Employees.Where(u => u.IsDeleted == false && u.IsActive == true).Select(b => new
            {
                Id = b.Id,
                Name = b.ArName + " - " + b.Code
            }), "Id", "Name");

            return Content("false");
        }

        [SkipERPAuthorize]
        public JsonResult SubServiceCategories(int id)
        {
            ServicesCategory serCat = db.ServicesCategories.Find(id);
            if (serCat.HasDetails != true)
            {
                return Json(new { HasDetails = "false", ServicesCategories = db.ServicesCategories.Where(s => s.ParentId == id && s.IsDeleted == false && s.IsActive == true).Select(s => new { s.Id, ArName = s.Code + " - " + s.ArName }) }, JsonRequestBehavior.AllowGet);
            }
            return Json(new { HasDetails = "true", ServicesCategories = db.ServicesCategories.Where(s => s.ParentId == id && s.IsDeleted == false && s.IsActive == true).Select(s => new { s.Id, ArName = s.Code + " - " + s.ArName }) }, JsonRequestBehavior.AllowGet);
        }
        [SkipERPAuthorize]
        public ActionResult _TechnicianServices(int techId,int parentId,int hasDetails)
        {
                return PartialView(db.GetTechnicianServices(techId, parentId, hasDetails));
        }

        [SkipERPAuthorize]
        public ActionResult GetServiceCategory(int EmployeeId)
        {
            var Services = db.GetServiceCategory(EmployeeId).ToList();
            return PartialView(Services);
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