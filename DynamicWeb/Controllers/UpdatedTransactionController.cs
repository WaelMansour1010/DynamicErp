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
    
    public class UpdatedTransactionController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        public ActionResult Index(DateTime? fromDate, DateTime? toDate, string SystemPages)
        {
            ViewBag.SystemPages = new SelectList(db.SystemPages.Where(s => s.IsMasterFile == false&&s.IsTransaction==true&& s.IsUpdated==true), "TableName", "ArName", SystemPages);
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة الحركات المعدلة",
                EnAction = "Index",
                ControllerName = "UpdatedTransaction",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            ////-------------------- Notification-------------------------////
            Notification.GetNotification("UpdatedTransaction", "View", "Index", null, null, " الحركات المعدلة");
            
            int systemPageId = db.Database.SqlQuery<int>($"select [Id] from [SystemPage] where [TableName]='{SystemPages}'").FirstOrDefault();

            if (fromDate == null && toDate == null)
            {
                List<UpdatedTransaction_GetUpdatedList_Result> EmpList = new List<UpdatedTransaction_GetUpdatedList_Result>();
                return View(EmpList);
            }
            var updatedList = db.UpdatedTransaction_GetUpdatedList(fromDate, toDate, systemPageId).ToList();
            ViewBag.fromDate = fromDate.Value.ToString("yyyy-MM-ddTHH:mm");
            ViewBag.toDate = toDate.Value.ToString("yyyy-MM-ddTHH:mm");


            return View(updatedList);
            
        }
        public ActionResult GetDetails(int id, string table)
        {
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

            var updatedTransaction = db.UpdatedTransactions.Find(id);
            var currencyEq = db.Currencies.Where(c => c.IsDeleted == false && c.IsActive == true).Select(c => c.Equivalent);

            ViewBag.Currencies = JsonConvert.SerializeObject(currencyEq);
            ViewBag.PaymentMethodId = db.PaymentMethods.ToList();
            var updatedPymentMethods = db.UpdatedTransactionPaymentMethods.Where(a => a.UpdatedTransactionId == id).ToList();
            foreach (var method in updatedPymentMethods)
            {
                if (method.PaymentMethodId == 1)
                {

                }
                else if (method.PaymentMethodId == 2)
                {
                    ViewBag.CashBoxId = new SelectList(db.UserCashBoxes.Where(d => d.UserId == userId).Select(b => new {
                        Id = b.CashBoxId,
                        ArName = b.CashBox.Code + " - " + b.CashBox.ArName
                    }), "Id", "ArName", method.CashBoxId);
                }
                else if (method.PaymentMethodId == 3)
                {

                    ViewBag.BankIdForCheque = new SelectList(db.Banks.Where(a => (a.IsActive == true && a.IsDeleted == false)).Select(b => new
                    {
                        Id = b.Id,
                        ArName = b.Code + " - " + b.ArName
                    }), "Id", "ArName", method.BankId);
                    ViewBag.BankAccountIdForCheque = new SelectList(db.BankAccounts.Where(a => (a.IsActive == true && a.IsDeleted == false)), "Id", "AccountNumber", method.BankAccountId);

                }
                else
                {
                    ViewBag.BankIdForVisa = new SelectList(db.Banks.Where(a => (a.IsActive == true && a.IsDeleted == false)).Select(b => new
                    {
                        Id = b.Id,
                        ArName = b.Code + " - " + b.ArName
                    }), "Id", "ArName", method.BankId);
                    ViewBag.BankAccountIdForVisa = new SelectList(db.BankAccounts.Where(a => (a.IsActive == true && a.IsDeleted == false)), "Id", "AccountNumber", method.BankAccountId);
                }
            }

            ViewBag.VendorId = new SelectList(db.Vendors.Where(a => (a.IsActive == true && a.IsDeleted == false)).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", updatedTransaction.VendorOrCustomerId);
            ViewBag.CustomerId = new SelectList(db.Customers.Where(a => (a.IsActive == true && a.IsDeleted == false)).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", updatedTransaction.VendorOrCustomerId);
            ViewBag.BranchId = new SelectList(db.Branches.Where(a => (a.IsActive == true && a.IsDeleted == false)).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", updatedTransaction.BranchId);
            ViewBag.CurrencyId = new SelectList(db.Currencies.Where(a => (a.IsActive == true && a.IsDeleted == false)).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", updatedTransaction.CurrencyId);
            ViewBag.DepartmentId = new SelectList(db.UserDepartments.Where(b => b.UserId == userId).Select(b => new
            {
                Id = b.DepartmentId,
                ArName = b.Department.Code + " - " + b.Department.ArName
            }), "Id", "ArName", updatedTransaction.DepartmentId);
            ViewBag.WarehouseId = new SelectList(db.UserWareHouses.Where(b => b.UserId == userId).Select(b => new
            {
                Id = b.WareHouseId,
                ArName = b.Warehouse.Code + " - " + b.Warehouse.ArName
            }), "Id", "ArName", updatedTransaction.WarehouseId);

            try
            {
                ViewBag.VoucherDate = updatedTransaction.VoucherDate.Value.ToString("yyyy-MM-ddTHH:mm");
            }
            catch (Exception)
            {
            }
          

            return View(updatedTransaction);
        }
        public ActionResult StockDetails(int id, string table)
        {
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

            var updatedTransaction = db.UpdatedTransactions.Find(id);
            var currencyEq = db.Currencies.Where(c => c.IsDeleted == false && c.IsActive == true).Select(c => c.Equivalent);
            ViewBag.DestinationWarehouseId = new SelectList(db.Warehouses.Where(a => (a.IsActive == true && a.IsDeleted == false)).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", updatedTransaction.DestinationWarehouseId);

            ViewBag.Currencies = JsonConvert.SerializeObject(currencyEq);
            
            ViewBag.BranchId = new SelectList(db.Branches.Where(a => (a.IsActive == true && a.IsDeleted == false)).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", updatedTransaction.BranchId);
            ViewBag.CurrencyId = new SelectList(db.Currencies.Where(a => (a.IsActive == true && a.IsDeleted == false)).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", updatedTransaction.CurrencyId);
            ViewBag.DepartmentId = new SelectList(db.UserDepartments.Where(b => b.UserId == userId).Select(b => new
            {
                Id = b.DepartmentId,
                ArName = b.Department.Code + " - " + b.Department.ArName
            }), "Id", "ArName", updatedTransaction.DepartmentId);
            ViewBag.WarehouseId = new SelectList(db.UserWareHouses.Where(b => b.UserId == userId).Select(b => new
            {
                Id = b.WareHouseId,
                ArName = b.Warehouse.Code + " - " + b.Warehouse.ArName
            }), "Id", "ArName", updatedTransaction.WarehouseId);
           
            //ViewBag.ItemId = updatedTransaction.SerializeObject(db.Items.Where(i => i.IsDeleted == false && i.IsActive == true).Select(i => new { i.ArName, i.Id }));

           
            try
            {
                ViewBag.VoucherDate = updatedTransaction.VoucherDate.Value.ToString("yyyy-MM-ddTHH:mm");
            }
            catch (Exception)
            {
            }


            return View(updatedTransaction);
        }

    }
}