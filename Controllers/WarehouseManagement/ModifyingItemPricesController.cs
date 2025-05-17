using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using MyERP.Models;
using MyERP.Controllers;

namespace MyERP.Controllers.WarehouseManagement
{
    public class ModifyingItemPricesController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();
        // GET: ModifyingItemPrices
        public ActionResult Index(bool? IsSearch, int? ItemGroupId, int? ItemCategoryId, int? ItemTypeId, int? DepartmentId)
        {
            ViewBag.ItemGroupId = new SelectList(db.ItemGroups.Where(a => a.IsActive == true && a.IsDeleted == false).Select(a => new
            {
                a.Id,
                ArName = a.ArName
            }), "Id", "ArName", ItemGroupId);
            ViewBag.ItemCategoryId = new SelectList(db.ItemCategories.Where(a => a.IsActive == true && a.IsDeleted == false).Select(a => new
            {
                a.Id,
                ArName = a.ArName
            }), "Id", "ArName", ItemCategoryId);
            ViewBag.ItemTypeId = new SelectList(db.ItemTypes.Where(a => a.IsActive == true && a.IsDeleted == false).Select(a => new
            {
                a.Id,
                ArName = a.ArName
            }), "Id", "ArName", ItemTypeId);
            ViewBag.DepartmentId = new SelectList(db.Departments.Where(a => a.IsActive == true && a.IsDeleted == false).Select(a => new
            {
                a.Id,
                ArName = a.ArName
            }), "Id", "ArName", DepartmentId);
            if (IsSearch == true)
            {
                var items = db.GetModifyingItemPrices(ItemGroupId, ItemCategoryId, ItemTypeId, DepartmentId).ToList();
                return View(items);
            }
            return View();
        }

        public ActionResult SaveModifyingItemPrices(int? ItemGroupId, int? ItemCategoryId, int? ItemTypeId, int? DepartmentId, bool? IsValue, decimal? ValueOrPercentage)
        {
            try
            {
                db.ModifyingItemPrices(ItemGroupId, ItemCategoryId, ItemTypeId, DepartmentId,IsValue,ValueOrPercentage);
                return Json(new { success = true }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false }, JsonRequestBehavior.AllowGet);
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
    public class ModifyingItemPrices
    {
        public int Id;
        public decimal Price;
    }
}