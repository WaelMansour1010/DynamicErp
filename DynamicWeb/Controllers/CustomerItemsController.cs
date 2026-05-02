
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using MyERP.Models;
using System.Security.Claims;
using Newtonsoft.Json;

namespace MyERP.Controllers.SystemSettings
    {
        
        public class CustomerItemsController : Controller
        {
            private MySoftERPEntity db = new MySoftERPEntity();

            public ActionResult Index(int? DepartmentId, int? itemId, int? customerId,int? TypeId)
            {
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            SystemSetting sysObj = db.SystemSettings.FirstOrDefault();
            ViewBag.ItemId = new SelectList(db.Items.Where(i=>i.IsActive==true&&i.IsDeleted==false).Select(b => new {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName");
            ViewBag.CustomerId = new SelectList(db.Customers.Where(a =>a.IsActive == true && a.IsDeleted == false).Select(b => new {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName");
            if (userId == 1)
            {
                ViewBag.DepartmentId = new SelectList(db.Departments.Where(d => d.IsActive == true && d.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName", DepartmentId);

            }
            else
            {
                ViewBag.DepartmentId = new SelectList(db.UserDepartments.Where(d => d.UserId == userId).Select(b => new
                {
                    Id = b.DepartmentId,
                    ArName = b.Department.Code + " - " + b.Department.ArName
                }), "Id", "ArName", DepartmentId);
            }
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "فتح قائمة مبيعات  صنف لعميل",
                    EnAction = "Index",
                    ControllerName = "CustomerItems",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "GET"
                });
                ////-------------------- Notification-------------------------////
                Notification.GetNotification("CustomerItems", "View", "Index", null, null, " مبيعات صنف لعميل");
                ViewBag.TypeId = TypeId;
                List<GetCustomerSalesItems_Result> ResultList = db.GetCustomerSalesItems(itemId,customerId,TypeId).ToList();
                return View(ResultList);
                

            }
        

    }
    }