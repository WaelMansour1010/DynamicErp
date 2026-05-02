using MyERP.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MyERP.Controllers
{
    public class DriversController : Controller
    {
        private readonly MySoftERPEntity db = new MySoftERPEntity();

        // GET: Drivers
        public ActionResult AddEdit()
        {
            var employees = db.Employees.Where(e => e.IsActive == true && e.IsDeleted == false).ToList();
            var cars = db.FixedAssets.Where(f => f.FixedAssetTypeId == 1 && f.IsActive == true && f.IsDeleted == false).ToList();

            var drivers = db.Employees.Where(e => e.IsActive == true && e.IsDeleted == false && e.IsDriver == true).ToList();

            ViewBag.Employees = employees;
            ViewBag.Cars = cars;

            return View(drivers);
        }

        //public ActionResult GetDriversData()
        //{
        //    var employees = db.Employees.Where(e => e.IsActive == true && e.IsDeleted == false).ToList();
        //    var cars = db.FixedAssets.Where(f => f.FixedAssetTypeId == 1 && f.IsActive == true && f.IsDeleted == false).ToList();

        //    var drivers = db.Employees.Where(e => e.IsActive == true && e.IsDeleted == false && e.IsDriver == true).ToList();

        //    ViewBag.Employees = employees;
        //    ViewBag.Cars = cars;

        //    return View(drivers);
        //}


        [HttpPost]
        public ActionResult AddEdit(int empId, int fixedAssetId)
        {
            var employeeInDb = db.Employees.Find(empId);
            var result = new { Result = false, AddedBefore = false };

            if (employeeInDb != null)
            {
                if (employeeInDb.IsDriver != true)
                {
                    employeeInDb.IsDriver = true;
                    employeeInDb.FixedAssetId = fixedAssetId;
                    db.SaveChanges();
                    result = new { Result = true, AddedBefore = false};
                    return Json(result, JsonRequestBehavior.AllowGet);
                }

                result = new { Result = false, AddedBefore = true };
                return Json(result, JsonRequestBehavior.AllowGet);
            }
            else
                return Json(result, JsonRequestBehavior.AllowGet);

        }

        [HttpPost]
        public ActionResult RemoveDriver(int empId)
        {
            var employeeInDb = db.Employees.Find(empId);
            if (employeeInDb != null)
            {
                employeeInDb.IsDriver = false;
                employeeInDb.FixedAssetId = null;
                db.SaveChanges();
                return Json(true, JsonRequestBehavior.AllowGet);
            }
            else
                return Json(false, JsonRequestBehavior.AllowGet);

        }
    }
}