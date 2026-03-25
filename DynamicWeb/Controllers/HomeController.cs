using MyERP.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;

namespace MyERP.Controllers
{
    public class HomeController : Controller
    {
        private MyERP.Models.MySoftERPEntity db = new MyERP.Models.MySoftERPEntity();
        [SkipERPAuthorize]
        public ActionResult Index()
        {
            ViewBag.CustomersCount = db.Customers.Where(c => c.IsActive == true && c.IsDeleted == false).Count();
            ViewBag.VendorsCount = db.Vendors.Where(c => c.IsActive == true && c.IsDeleted == false).Count();
            ViewBag.PurchaseRequests = db.PurchaseRequests.Where(c => c.IsActive == true && c.IsDeleted == false).Count();
            ViewBag.SalesInvoices = db.SalesInvoices.Where(c => c.IsActive == true && c.IsDeleted == false).Count();
            var systemSetting = db.SystemSettings.FirstOrDefault();
            ViewBag.ShowDashBoard = systemSetting.ShowDashBoard != true ? false : true;

            var UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            ViewBag.ShowDashBoardForUser = db.ERPUsers.Where(a => a.Id == UserId && a.IsActive == true && a.IsDeleted == false).FirstOrDefault().ShowDashBoardForUser;
            ViewBag.CreatedByUser = db.UserTasks.Where(a => a.CreatedBy == UserId).OrderByDescending(t => t.Id).ToList(); // الموجهة منه 
            ViewBag.AssignedToUser = db.UserTasks.Where(a => a.AssignedTo == UserId).OrderByDescending(t => t.Id).ToList(); //الموجهة له 
            ViewBag.TaskStatus = db.TaskStatus.ToList();
            ViewBag.ShowUserTasks = db.SystemSettings.Select(a => a.ShowUserTasks).FirstOrDefault();
            ViewBag.ShowUserIcons = db.SystemSettings.Select(a => a.ShowUserIcons).FirstOrDefault();

            //ViewBag.MenuExpanded = true;
            Session["MenuExpanded"] = true;

            // Nearly expired
            var numberOfDays = systemSetting.DaysNo;
            var nearlyExpiredPatches = db.GetNearlyExpiredPatches(numberOfDays, null, null, null, null, null, null).ToList();
            if (nearlyExpiredPatches.Count() > 0)
            {
                ViewBag.NearlyExpiredPatchesCount = nearlyExpiredPatches.Count();
            }
            else
            {
                ViewBag.NearlyExpiredPatchesCount = 0;
            }

            return View();
        }

        [SkipERPAuthorize]
        public ActionResult Unauthorized()
        {
            return View();
        }

        public ActionResult ItemQuantitiesInWarehouses(int id)
        {
            var model = new object[0];
            return PartialView("_ItemQuantitiesInWarehouses", model);
        }

     [SkipERPAuthorize]
        public ActionResult Profile()
   
        {
            var UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            var EmployeeId = db.ERPUsers.Where(c => c.Id == UserId).Select(x => x.EmployeeId).FirstOrDefault();

            ViewBag.img = db.Employees.Where(x => x.Id == EmployeeId).Select(x => x.Image).FirstOrDefault();

            return PartialView("_Profile", ViewBag.img);
        }

        [SkipERPAuthorize]
        public ActionResult UnderDevolop()
        {
            return View();
        }

        [SkipERPAuthorize]
        public ActionResult OrderRequest()
        {
            var UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            var UserName = User.Identity.Name;
            var EmployeeId = db.ERPUsers.Where(c => c.Id == UserId && c.UserName == UserName).FirstOrDefault();
            if (EmployeeId != null)
            {
                var empId = Convert.ToInt32(EmployeeId.EmployeeId);

                ViewBag.count = db.OrderRequests.Where(o => o.EmployeeId == empId && o.Status == false).ToList().Count;

            }
            else
            {
                ViewBag.count = 0;
            }
            return PartialView("OrderRequest", ViewBag.count);

        }
        [SkipERPAuthorize]
        public JsonResult OrderRequestclose()
        {
            var UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            var UserName = User.Identity.Name;
            var EmployeeId = db.ERPUsers.Where(c => c.Id == UserId && c.UserName == UserName).FirstOrDefault();
            if (EmployeeId != null)
            {
                var empId = Convert.ToInt32(EmployeeId.EmployeeId);

                var countclose = db.OrderRequests.Where(o => o.EmployeeId == empId && o.Status == true).ToList().Count;
                return Json(countclose, JsonRequestBehavior.AllowGet);
            }
            else
            {

            }
            return Json(0, JsonRequestBehavior.AllowGet);

        }
        [SkipERPAuthorize]
        public JsonResult OrderRequestopen()
        {
            var UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            var UserName = User.Identity.Name;
            var EmployeeId = db.ERPUsers.Where(c => c.Id == UserId && c.UserName == UserName).FirstOrDefault();
            if (EmployeeId != null)
            {
                var empId = Convert.ToInt32(EmployeeId.EmployeeId);

                var countopen = db.OrderRequests.Where(o => o.EmployeeId == empId && o.Status == false).ToList().Count;
                return Json(countopen, JsonRequestBehavior.AllowGet);
            }
            else
            {

            }
            return Json(0, JsonRequestBehavior.AllowGet);

        }
        [SkipERPAuthorize]
        public ActionResult OrderRemaining()
        {
            var UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

            DateTime utcNow = DateTime.UtcNow;
            TimeZoneInfo info = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");
            DateTime cTime = TimeZoneInfo.ConvertTimeFromUtc(utcNow, info);

            var datenow = (Convert.ToDateTime(cTime.ToString("G")));
            var ordersremain = db.remainingOrders(datenow, UserId).ToList();



            return PartialView("OrderRemaining", ordersremain);

        }

        //[SkipERPAuthorize]
        //public ActionResult FetchData()
        //{
        //    var depId = 1;
        //    var DBChanges = db.GetOnlineDBChangesCount(depId);
        //    return Json(DBChanges.ToList(), JsonRequestBehavior.AllowGet);

        //}

        [SkipERPAuthorize]
        public ActionResult Sync()
        {
            var depId = 1;
            var DBChanges = QueryHelper.GetOnlineDBChange("[SQL5060.SITE4NOW.NET].DB_A396C8_MyERP.dbo.");
            var UserChanges = new List<ERPUser>();
            var UserWareHouse = new List<UserWareHouse>();
            var UserDepartment = new List<UserDepartment>();
            var UserPos = new List<UserPos>();
            var UserCashBox = new List<UserCashBox>();
            QueryHelper.CopyTableData("[SQL5060.SITE4NOW.NET].DB_A396C8_MyERP.dbo.");
            return Json(true, JsonRequestBehavior.AllowGet);

        }

        [SkipERPAuthorize]
        public ActionResult GetTotalSalesAndProfitInWeek()
        {
            var total = db.GetTotalSalesAndProfitInWeek().ToList();
            return Json(total, JsonRequestBehavior.AllowGet);
        }

        [SkipERPAuthorize]
        public ActionResult GetPaymentMethodAmountInDay()
        {
            var Method = db.GetPaymentMethodAmountInDay().ToList();
            return Json(Method, JsonRequestBehavior.AllowGet);
        } 
        [SkipERPAuthorize]
        public ActionResult GetCustomer_VendorCurrentBalance()
        {
            var customer = db.GetTopCustomerBalance().ToList();
            var vendor = db.GetTopVendorBalance().ToList();
            return Json(new { customer ,vendor}, JsonRequestBehavior.AllowGet);
        } 
    }
}