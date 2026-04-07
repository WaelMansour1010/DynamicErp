using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using MyERP.Models;
using System.Security.Claims;
using Newtonsoft.Json;
using MyERP.Repository;
using System.Threading.Tasks;

namespace MyERP.Controllers.WarehouseManagement
{
    public class PurchaseReturnController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        // GET: PurchaseReturn
        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            SystemSetting systemSetting =  db.SystemSettings.Any() ?  db.SystemSettings.FirstOrDefault() : new SystemSetting();
            ViewBag.InabilityToEditSalesAndPurchaseInvoicesAndReturns = systemSetting.InabilityToEditSalesAndPurchaseInvoicesAndReturns;

            Notification.GetNotification("PurchaseReturn", "View", "Index", null, null, "مرتجع شراء");
            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;

            IQueryable<PurchaseReturn> purchaseReturns;
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

            var depIds = db.Departments.Where(d => (userId == 1 || db.UserDepartments.Where(u => u.UserId == userId && u.Privilege == true).Select(u => u.DepartmentId).Contains(d.Id)) && d.IsDeleted == false && d.IsActive == true).Select(d => (int?)d.Id);
            if (string.IsNullOrEmpty(searchWord))
            {
                purchaseReturns = db.PurchaseReturns.Where(s => s.IsDeleted == false && depIds.Contains(s.DepartmentId)).Include(s => s.Branch).Include(s => s.Currency).Include(s => s.Department).Include(s => s.Warehouse).OrderByDescending(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.PurchaseReturns.Where(s => s.IsDeleted == false && depIds.Contains(s.DepartmentId)).Count();
            }
            else
            {
                purchaseReturns = db.PurchaseReturns.Where(s => s.IsDeleted == false && depIds.Contains(s.DepartmentId) && (s.DocumentNumber.Contains(searchWord) || s.Branch.ArName.Contains(searchWord) || s.Department.ArName.Contains(searchWord) || s.Warehouse.ArName.Contains(searchWord) || s.Notes.Contains(searchWord) || s.Vendor.ArName.Contains(searchWord))).Include(s => s.Department).Include(s => s.Warehouse).OrderByDescending(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.PurchaseReturns.Where(s => s.IsDeleted == false && depIds.Contains(s.DepartmentId) && (s.DocumentNumber.Contains(searchWord) || s.Branch.ArName.Contains(searchWord) || s.Department.ArName.Contains(searchWord) || s.Warehouse.ArName.Contains(searchWord) || s.Notes.Contains(searchWord) || s.Vendor.ArName.Contains(searchWord))).Count();
            }
            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة مرتجع الشراء",
                EnAction = "Index",
                ControllerName = "PurchaseReturn",
                UserName = User.Identity.Name,
                UserId = userId,
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            return View(purchaseReturns.ToList());
        }

        // GET: PurchaseReturn/Edit/5
        public async Task<ActionResult> AddEdit(int? id)
        {
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            UserRepository userRepository = new UserRepository(db);
            DepartmentRepository departmentRepository = new DepartmentRepository(db);
            WarehouseRepository warehouseRepository = new WarehouseRepository(db);
            CashboxReposistory cashboxReposistory = new CashboxReposistory(db);
            SystemSetting systemSetting = db.SystemSettings.Any() ? db.SystemSettings.FirstOrDefault() : new SystemSetting();
            int roleId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("RoleId").Value);
            var CanChangeItemPrice = userId == 1 ? true : db.UserPrivileges.Where(u => u.PageAction.PageId == 58 && u.PageAction.EnName == "ChangeItemPrice" && u.PageAction.Action == "ChangeItemPrice" && u.UserId == userId).Select(x => x.Privileged).FirstOrDefault();
            CanChangeItemPrice = CanChangeItemPrice ?? db.RolePrivileges.Where(u => u.PageAction.PageId == 58 && u.PageAction.EnName == "ChangeItemPrice" && u.PageAction.Action == "ChangeItemPrice" && u.RoleId == roleId).Select(x => x.Privileged).FirstOrDefault();
            //var canChangeItemPrice = await userRepository.HasActionPrivilege(userId, "ChangeItemPrice", "ChangeItemPrice");
            ViewBag.CanChangeItemPrice = CanChangeItemPrice == true;
            ViewBag.ShowSerialNumbers = systemSetting.ShowSerialNumbers == true;
            ViewBag.PayViaCarryOver = systemSetting.PayViaCarryOver == true;
            ViewBag.PayViaCash = systemSetting.PayViaCash == true;
            ViewBag.PayViaCheque = systemSetting.PayViaCheque == true;
            ViewBag.PayViaVisa = systemSetting.PayViaVisa == true;
            ViewBag.UseCostCenter = systemSetting.UseCostCenter;
            ViewBag.UseExpiryDateForItems = systemSetting.UseExpiryDateForItems;

            //DepartmentRepository departmentRepository = new DepartmentRepository(db);
            var departments = await departmentRepository.UserDepartments(1).ToListAsync();//show all deprartments so that user can select factory department
            ViewBag.LinkWithDocModalDepartmentId = new SelectList(departments, "Id", "ArName", systemSetting.DefaultDepartmentId);


            var banks = db.Banks.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }).ToList();

            if (id == null)
            {
                var bankAccounts = db.BankAccounts.Where(a => a.IsActive == true && a.IsDeleted == false && a.BankId == systemSetting.DefaultBankId).Select(x => new { x.Id, x.AccountNumber }).ToList();

                ViewBag.VendorOrCustomerId = new SelectList(db.Vendors.Where(a => (a.IsActive == true && a.IsDeleted == false)).Select(b => new
                {
                    b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");

                ViewBag.DepartmentId = new SelectList(departmentRepository.UserDepartments(userId), "Id", "ArName", systemSetting.DefaultDepartmentId);

                ViewBag.WarehouseId = new SelectList(warehouseRepository.UserWarehouses(userId, systemSetting.DefaultDepartmentId), "Id", "ArName", systemSetting.DefaultWarehouseId);
                ViewBag.CashBoxId = new SelectList(cashboxReposistory.UserCashboxes(userId, systemSetting.DefaultDepartmentId), "Id", "ArName", systemSetting.DefaultCashBoxId);
                ViewBag.CostCenterId = new SelectList(db.CostCenters.Where(a => a.IsActive && !a.IsDeleted && a.TypeId == 2).Select(b => new
                {
                    b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");

                ViewBag.BankIdForVisa = new SelectList(banks, "Id", "ArName", systemSetting.DefaultBankId);
                ViewBag.BankAccountIdForVisa = new SelectList(bankAccounts, "Id", "AccountNumber");
                ViewBag.BankIdForCheque = new SelectList(banks, "Id", "ArName", systemSetting.DefaultBankId);
                ViewBag.BankAccountIdForCheque = new SelectList(bankAccounts, "Id", "AccountNumber");
                List<PaymentMethod> paymentMethods = db.PaymentMethods.Where(p => p.IsActive == true && p.IsDeleted == false).ToList();
                ViewBag.PaymentMethods = paymentMethods;
                //------ Time Zone Depends On Currency --------//
                var Currency = db.Currencies.Where(a => a.IsActive == true && a.IsDeleted == false && a.IsDefault == true).FirstOrDefault();
                var CurrencyCode = Currency != null ? Currency.Code : "";
                TimeZoneInfo info;
                if (CurrencyCode == "SAR")
                {
                    //info = TimeZoneInfo.FindSystemTimeZoneById("Arabian Standard Time");//+2H from Egypt Standard Time
                    info = TimeZoneInfo.FindSystemTimeZoneById("Arab Standard Time");// +1H from Egypt Standard Time
                }
                else
                {
                    info = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");
                }
                DateTime utcNow = DateTime.UtcNow;
                DateTime cTime = TimeZoneInfo.ConvertTimeFromUtc(utcNow, info);
                //----------------- End of Time Zone Depends On Currency --------------------//

                ViewBag.VoucherDate = cTime.ToString("yyyy-MM-ddTHH:mm");
                return View();
            }
            PurchaseReturn purchaseReturn = await db.PurchaseReturns.FindAsync(id);
            if (purchaseReturn == null)
            {
                return HttpNotFound();
            }
            var cashBoxes = await cashboxReposistory.UserCashboxes(userId, purchaseReturn.DepartmentId).ToListAsync();

            ViewBag.VendorOrCustomerId = new SelectList(db.Vendors.Where(a => (a.IsActive == true && a.IsDeleted == false)).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", purchaseReturn.VendorOrCustomerId);

            foreach (var method in purchaseReturn.PurchaseReturnPaymentMethods)
            {
                if (method.PaymentMethodId == 1)
                {
                    ViewBag.CashBoxId = new SelectList(cashBoxes, "Id", "ArName", method.CashBoxId);
                }
                else if (method.PaymentMethodId == 3)
                {

                    ViewBag.BankIdForCheque = new SelectList(banks, "Id", "ArName", method.BankId);
                    ViewBag.BankAccountIdForCheque = new SelectList(db.BankAccounts.Where(a => a.IsActive == true && a.IsDeleted == false && a.BankId == method.BankId), "Id", "AccountNumber", method.BankAccountId);
                }
                else
                {
                    ViewBag.BankIdForVisa = new SelectList(banks, "Id", "ArName", method.BankId);
                    ViewBag.BankAccountIdForVisa = new SelectList(db.BankAccounts.Where(a => a.IsActive == true && a.IsDeleted == false && a.BankId == method.BankId), "Id", "AccountNumber", method.BankAccountId);
                }
            }

            ViewBag.DepartmentId = new SelectList(departmentRepository.UserDepartments(userId), "Id", "ArName", purchaseReturn.DepartmentId);
            ViewBag.WarehouseId = new SelectList(warehouseRepository.UserWarehouses(userId, purchaseReturn.DepartmentId), "Id", "ArName", purchaseReturn.WarehouseId);
            ViewBag.CostCenterId = new SelectList(db.CostCenters.Where(a => a.IsActive && !a.IsDeleted && a.TypeId == 2).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", purchaseReturn.CostCenterId);
            var sysPageId = QueryHelper.SourcePageId("PurchaseReturns");
            JournalEntry journal = db.JournalEntries.FirstOrDefault(j => j.SourceId == id && j.SourcePageId == sysPageId);

            ViewBag.Journal = journal;
            ViewBag.VoucherDate = purchaseReturn.VoucherDate.ToString("yyyy-MM-ddTHH:mm"); ;
            ViewBag.Journal.DocumentNumber= journal != null ? journal.DocumentNumber.Replace("-", "") : "";
            return View(purchaseReturn);
        }

        // POST: PurchaseReturn/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        [HttpPost]
        //[ValidateAntiForgeryToken]
        public ActionResult AddEdit(PurchaseReturn purchaseReturn, ICollection<PurchaseSaleSerialNumber> serialNumbers)
        {
            int userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            purchaseReturn.UserId = userId;
           
            if (ModelState.IsValid)
            {
                // Handle datetime zone hours
                foreach (var detail in purchaseReturn.PurchaseReturnsDetails)
                {
                    if (detail.ExpireDate.HasValue)
                        detail.ExpireDate = detail.ExpireDate.Value.AddHours(6);
                }

                var id = purchaseReturn.Id;
                purchaseReturn.IsDeleted = false;
                if (purchaseReturn.Id > 0)
                {
                    var systemSetting = db.SystemSettings.FirstOrDefault();
                    var InabilityToEditSalesAndPurchaseInvoicesAndReturns = systemSetting.InabilityToEditSalesAndPurchaseInvoicesAndReturns;
                    if (InabilityToEditSalesAndPurchaseInvoicesAndReturns == true)
                    {
                        return Content("Cannot Be Edited");
                    }

                    if (db.PurchaseReturns.Find(purchaseReturn.Id).IsPosted == true)
                    {
                        return Content("false");
                    }

                    MyXML.xPathName = "Details";
                    var purchaseReturnDetails = MyXML.GetXML(purchaseReturn.PurchaseReturnsDetails);
                    MyXML.xPathName = "PaymentMethods";
                    var PaymentMethods = MyXML.GetXML(purchaseReturn.PurchaseReturnPaymentMethods);

                    MyXML.xPathName = "SerialNumbers";
                    var SerialNumbersXML = MyXML.GetXML(serialNumbers);

                    db.PurchaseReturns_Update(purchaseReturn.Id, purchaseReturn.DocumentNumber, purchaseReturn.BranchId, purchaseReturn.WarehouseId, purchaseReturn.DepartmentId, purchaseReturn.VoucherDate, purchaseReturn.VendorOrCustomerId, purchaseReturn.CurrencyId, purchaseReturn.CurrencyEquivalent, purchaseReturn.Total, purchaseReturn.TotalItemsDiscount, purchaseReturn.SalesTaxes, purchaseReturn.TotalAfterTaxes, purchaseReturn.VoucherDiscountValue, purchaseReturn.VoucherDiscountPercentage, purchaseReturn.NetTotal, purchaseReturn.Paid, purchaseReturn.ValidityPeriod, purchaseReturn.DeliveryPeriod, purchaseReturn.CostCenterId, purchaseReturn.CurrentQuantity, purchaseReturn.DestinationWarehouseId, purchaseReturn.SystemPageId, purchaseReturn.SelectedId, purchaseReturn.TotalCostPrice, purchaseReturn.TotalItemDirectExpenses, purchaseReturn.IsDelivered, purchaseReturn.IsAccepted, purchaseReturn.IsLinked, purchaseReturn.IsCompleted, purchaseReturn.IsPosted, purchaseReturn.UserId, purchaseReturn.IsActive, purchaseReturn.IsDeleted, purchaseReturn.AutoCreated, purchaseReturn.Notes, purchaseReturn.Image, purchaseReturn.UpdatedId, purchaseReturnDetails, PaymentMethods, SerialNumbersXML);

                }
                else
                {
                    purchaseReturn.IsActive = true;
                    MyXML.xPathName = "Details";
                    var purchaseReturnDetails = MyXML.GetXML(purchaseReturn.PurchaseReturnsDetails);
                    MyXML.xPathName = "PaymentMethods";
                    var PaymentMethods = MyXML.GetXML(purchaseReturn.PurchaseReturnPaymentMethods);
                    MyXML.xPathName = "SerialNumbers";
                    var SerialNumbersXML = MyXML.GetXML(serialNumbers);
                    var idResult = new System.Data.Entity.Core.Objects.ObjectParameter("Id", typeof(Int32));
                    db.PurchaseReturns_Insert(idResult, purchaseReturn.BranchId, purchaseReturn.WarehouseId, purchaseReturn.DepartmentId, purchaseReturn.VoucherDate, purchaseReturn.VendorOrCustomerId, purchaseReturn.CurrencyId, purchaseReturn.CurrencyEquivalent, purchaseReturn.Total, purchaseReturn.TotalItemsDiscount, purchaseReturn.SalesTaxes, purchaseReturn.TotalAfterTaxes, purchaseReturn.VoucherDiscountValue, purchaseReturn.VoucherDiscountPercentage, purchaseReturn.NetTotal, purchaseReturn.Paid, purchaseReturn.ValidityPeriod, purchaseReturn.DeliveryPeriod, purchaseReturn.CostCenterId, purchaseReturn.CurrentQuantity, purchaseReturn.DestinationWarehouseId, purchaseReturn.SystemPageId, purchaseReturn.SelectedId, purchaseReturn.TotalCostPrice, purchaseReturn.TotalItemDirectExpenses, purchaseReturn.IsDelivered, purchaseReturn.IsAccepted, purchaseReturn.IsLinked, purchaseReturn.IsCompleted, false, purchaseReturn.UserId, purchaseReturn.IsActive, purchaseReturn.IsDeleted, purchaseReturn.AutoCreated, purchaseReturn.Notes, purchaseReturn.Image, purchaseReturn.UpdatedId, purchaseReturnDetails, PaymentMethods, SerialNumbersXML);

                }
                ////-------------------- Notification-------------------------////
                Notification.GetNotification("PurchaseReturn", id > 0 ? "Edit" : "Add", "AddEdit", id, null, "مرتجع شراء");
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = id > 0 ? "تعديل  مرتجع شراء " : "اضافة مرتجع شراء",
                    EnAction = "AddEdit",
                    ControllerName = "PurchaseReturn",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = purchaseReturn.Id > 0 ? purchaseReturn.Id : db.PurchaseReturns.Max(i => i.Id),
                    CodeOrDocNo = purchaseReturn.DocumentNumber
                });

                return Content("true");
            }
            var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .Select(x => new { x.Key, x.Value.Errors })
                    .ToArray();

            return Content("false");
        }


        [SkipERPAuthorize]
        public JsonResult GetSerialNumber(int? invoiceId, int? itemId)
        {

            var SerialNumbers = JsonConvert.SerializeObject(db.PurchaseSaleSerialNumbers.Where(s => s.SelectedId == invoiceId && s.ItemId == itemId && s.PageSourceId == db.SystemPages.FirstOrDefault(d => d.TableName == "PurchaseReturns").Id).Select(d => new { d.ItemId, d.SerialNumber }));


            return Json(SerialNumbers, JsonRequestBehavior.AllowGet);
        }

        // GET
        public ActionResult AddEditSerialNumber(int? id, int itemId)
        {
            SystemSetting sysObj = db.SystemSettings.FirstOrDefault();
            ViewData["ShowSerialNumbers"] = true;


            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            PurchaseReturn purchaseReturn = db.PurchaseReturns.Find(id);
            if (purchaseReturn == null)
            {
                return HttpNotFound();
            }
            if (itemId == 0)
            {
                var x = db.PurchaseReturnsDetails.Where(p => p.MainDocId == id).FirstOrDefault().ItemId;
                itemId = int.Parse(x.ToString());
            }
            ViewBag.GeneralItemId = itemId;
            int sysPageId = QueryHelper.SourcePageId("PurchaseReturns");
            ViewBag.VendorId = new SelectList(db.Vendors.Where(a => (a.IsActive == true && a.IsDeleted == false)).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName + " - " + b.EnName
            }), "Id", "ArName", purchaseReturn.VendorOrCustomerId);
            ViewBag.VendorOrCustomerId = new SelectList(db.Vendors.Where(a => (a.IsActive == true && a.IsDeleted == false)).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName + " - " + b.EnName
            }), "Id", "ArName", purchaseReturn.VendorOrCustomerId);

            ViewBag.BranchId = new SelectList(db.Branches.Where(a => (a.IsActive == true && a.IsDeleted == false)).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", purchaseReturn.BranchId);
            if (userId == 1)
            {
                ViewBag.DepartmentId = new SelectList(db.Departments.Where(d => d.IsActive == true && d.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName", purchaseReturn.DepartmentId);
                ViewBag.WarehouseId = new SelectList(db.Warehouses.Where(b => b.IsActive == true && b.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName", purchaseReturn.WarehouseId);

            }
            else
            {
                ViewBag.DepartmentId = new SelectList(db.UserDepartments.Where(d => d.UserId == userId).Select(b => new
                {
                    Id = b.DepartmentId,
                    ArName = b.Department.Code + " - " + b.Department.ArName
                }), "Id", "ArName", purchaseReturn.DepartmentId);
                ViewBag.WarehouseId = new SelectList(db.UserWareHouses.Where(b => b.UserId == userId).Select(b => new
                {
                    Id = b.WareHouseId,
                    ArName = b.Warehouse.Code + " - " + b.Warehouse.ArName
                }), "Id", "ArName", purchaseReturn.WarehouseId);

            }
            ViewBag.SerialNumbers = JsonConvert.SerializeObject(db.PurchaseSaleSerialNumbers.Where(s => s.SelectedId == id && s.ItemId == itemId && s.PageSourceId == db.SystemPages.FirstOrDefault(d => d.TableName == "PurchaseReturns").Id).Select(d => new { d.ItemId, d.SerialNumber }));



            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح سيريال مرتجع الشراء",
                EnAction = "AddEdit",
                ControllerName = "AddEditSerialNumber",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = purchaseReturn.Id,
                CodeOrDocNo = purchaseReturn.DocumentNumber
            });

            return View(purchaseReturn);
        }



        [HttpPost]
        //[ValidateAntiForgeryToken]
        public JsonResult AddEditSerialNumber(int purchaseReturnId, int itemId, ICollection<PurchaseSaleSerialNumber> serialNumbers)
        {
            if (ModelState.IsValid)
            {
                if (purchaseReturnId == 0)
                {
                    return Json("false");
                }
                if (purchaseReturnId > 0)
                {
                    if (db.PurchaseReturns.Find(purchaseReturnId).IsPosted == true)
                    {
                        return Json("false");
                    }
                    MyXML.xPathName = "SerialNumbers";
                    var SerialNumbersXML = MyXML.GetXML(serialNumbers);
                    db.AddEditSerialNumber(purchaseReturnId, itemId, QueryHelper.SourcePageId("PurchaseReturns"), SerialNumbersXML);

                    Notification.GetNotification("PurchaseReturn", "AddEditSerialNumber", "AddEditSerialNumber", purchaseReturnId, null, "  سيريال مرتجع المشتريات");
                }

                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = purchaseReturnId > 0 ? "تعديل سيريال مرتجع شراء " : "تعديل سيريال مرتجع شراء",
                    EnAction = "AddEditSerialNumber",
                    ControllerName = "PurchaseReturn",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = purchaseReturnId,
                    CodeOrDocNo = db.PurchaseReturns.Where(p => p.Id == purchaseReturnId).FirstOrDefault().DocumentNumber
                });

                return Json(new { success = "true", id = purchaseReturnId });
            }
            var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .Select(x => new { x.Key, x.Value.Errors })
                    .ToArray();

            return Json(errors);
        }


        // POST: PurchaseReturn/Delete/5
        [HttpPost, ActionName("Delete")]
        //[ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            var systemSetting = db.SystemSettings.FirstOrDefault();
            var InabilityToEditSalesAndPurchaseInvoicesAndReturns = systemSetting.InabilityToEditSalesAndPurchaseInvoicesAndReturns;
            if (InabilityToEditSalesAndPurchaseInvoicesAndReturns == true)
            {
                return Content("Cannot Be Deleted");
            }

            int userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            PurchaseReturn purchaseReturn = db.PurchaseReturns.Find(id);
            try
            {
                if (purchaseReturn.IsPosted == true)
                {
                    return Content("false");
                }
                db.PurchaseReturns_Delete(id, userId);
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = " حذف فاتورة مرتجع شراء",
                    EnAction = "AddEdit",
                    ControllerName = "PurchaseReturn",
                    UserName = User.Identity.Name,
                    UserId = userId,
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    CodeOrDocNo = purchaseReturn.DocumentNumber
                });

                ////-------------------- Notification-------------------------////
                Notification.GetNotification("PurchaseReturn", "Delete", "Delete", id, null, "مرتجع شراء");
                return Content("true");
            }
            catch (Exception)
            {
                throw;
            }
        }

        [SkipERPAuthorize]
        public JsonResult SetDocNum(int id, DateTime? VoucherDate)
        {
            bool IsExistInDocumentsCoding = false;
            var noOfDigits = (int?)0;
            var YearFormat = (int?)0;
            var CodingTypeId = (int?)0;
            var IsZerosFills = (bool?)null;
            var newDocNo = "";
            var GeneratedDocNo = "";
            var lastObj = db.PurchaseReturns.Where(a => a.IsActive == true && a.IsDeleted == false && a.DepartmentId == id).OrderByDescending(a => a.Id).FirstOrDefault();
            var lastDocNo = lastObj != null ? lastObj.DocumentNumber : "0";
            var DepartmentCode = db.Departments.Where(a => a.IsActive == true && a.IsDeleted == false && a.Id == id).FirstOrDefault().Code;
            DepartmentCode = double.Parse(DepartmentCode) < 10 ? "0" + DepartmentCode : DepartmentCode;
            var DepartmentDoc = db.DocumentsCodings.Where(a => a.IsActive == true && a.IsDeleted == false && a.DepartmentId == id).FirstOrDefault();
            var MonthFormat = VoucherDate.Value.Month < 10 ? "0" + VoucherDate.Value.Month.ToString() : VoucherDate.Value.Month.ToString();
            if (DepartmentDoc == null)
            {
                DepartmentDoc = db.DocumentsCodings.Where(a => a.IsActive == true && a.IsDeleted == false && a.AllDepartments == true).FirstOrDefault();
                if (DepartmentDoc == null)
                {
                    IsExistInDocumentsCoding = false;
                }
                else
                {
                    IsExistInDocumentsCoding = true;
                }
            }
            else
            {
                IsExistInDocumentsCoding = true;
            }
            if (IsExistInDocumentsCoding == true)
            {
                noOfDigits = DepartmentDoc.DigitsNo;
                YearFormat = DepartmentDoc.YearFormat;
                CodingTypeId = DepartmentDoc.CodingTypeId;
                IsZerosFills = DepartmentDoc.IsZerosFills;
                YearFormat = YearFormat == 2 ? int.Parse(VoucherDate.Value.Year.ToString().Substring(2, 2)) : int.Parse(VoucherDate.Value.Year.ToString());

                if (CodingTypeId == 1)//آلي
                {
                    if (lastDocNo.Contains("-"))
                    {
                        var ar = lastDocNo.Split('-');
                        if (IsZerosFills == true)
                        {
                            newDocNo = QueryHelper.FillsWithZeros(noOfDigits, (double.Parse(ar[3]) + 1).ToString());
                        }
                        else
                        {
                            newDocNo = (double.Parse(ar[3]) + 1).ToString();
                        }
                    }
                    else
                    {
                        if (IsZerosFills == true)
                        {
                            newDocNo = QueryHelper.FillsWithZeros(noOfDigits, (double.Parse(lastDocNo) + 1).ToString());
                        }
                        else
                        {
                            newDocNo = (double.Parse(lastDocNo) + 1).ToString();
                        }
                    }
                    GeneratedDocNo = DepartmentCode + "-" + YearFormat + "-" + MonthFormat + "-" + newDocNo;
                }
                else if (CodingTypeId == 2)//متصل شهري
                {
                    lastObj = db.PurchaseReturns.Where(a => a.IsActive == true && a.IsDeleted == false && a.DepartmentId == id && a.VoucherDate.Month == VoucherDate.Value.Month && a.VoucherDate.Year == VoucherDate.Value.Year).OrderByDescending(a => a.Id).FirstOrDefault();
                    if (lastObj != null)
                    {
                        if (lastObj.DocumentNumber.Contains("-"))
                        {
                            var ar = lastObj.DocumentNumber.Split('-');
                            if (double.Parse(ar[2]) == VoucherDate.Value.Month)
                            {
                                if (IsZerosFills == true)
                                {
                                    newDocNo = QueryHelper.FillsWithZeros(noOfDigits, (double.Parse(ar[3]) + 1).ToString());
                                }
                                else
                                {
                                    newDocNo = (double.Parse(ar[3]) + 1).ToString();
                                }
                            }
                            else
                            {
                                if (IsZerosFills == true)
                                {
                                    newDocNo = QueryHelper.FillsWithZeros(noOfDigits, "1").ToString();
                                }
                                else
                                {
                                    newDocNo = "1";
                                }
                            }
                        }
                        else
                        {
                            if (IsZerosFills == true)
                            {
                                newDocNo = QueryHelper.FillsWithZeros(noOfDigits, (double.Parse(lastObj.DocumentNumber) + 1).ToString()).ToString();
                            }
                            else
                            {
                                newDocNo = (double.Parse(lastObj.DocumentNumber) + 1).ToString();
                            }
                        }
                    }
                    else
                    {
                        if (IsZerosFills == true)
                        {
                            newDocNo = QueryHelper.FillsWithZeros(noOfDigits, "1").ToString();
                        }
                        else
                        {
                            newDocNo = "1";
                        }
                    }
                    GeneratedDocNo = DepartmentCode + "-" + YearFormat + "-" + MonthFormat + "-" + newDocNo;
                }
                else if (CodingTypeId == 3)//متصل سنوي
                {
                    lastObj = db.PurchaseReturns.Where(a => a.IsActive == true && a.IsDeleted == false && a.DepartmentId == id && a.VoucherDate.Year == VoucherDate.Value.Year).OrderByDescending(a => a.Id).FirstOrDefault();

                    if (lastObj != null)
                    {
                        if (lastObj.DocumentNumber.Contains("-"))
                        {
                            var ar = lastObj.DocumentNumber.Split('-');
                            var VoucherDateFormate = int.Parse(ar[1]).ToString().Length == 2 ? int.Parse((VoucherDate.Value.Year.ToString()).Substring(2, 2)) : VoucherDate.Value.Year;
                            if (double.Parse(ar[1]) == VoucherDateFormate)
                            {
                                if (IsZerosFills == true)
                                {
                                    newDocNo = QueryHelper.FillsWithZeros(noOfDigits, (double.Parse(ar[3]) + 1).ToString());
                                }
                                else
                                {
                                    newDocNo = (double.Parse(ar[3]) + 1).ToString();
                                }
                            }
                            else
                            {
                                if (IsZerosFills == true)
                                {
                                    newDocNo = QueryHelper.FillsWithZeros(noOfDigits, "1").ToString();
                                }
                                else
                                {
                                    newDocNo = "1";
                                }
                            }
                        }
                        else
                        {
                            if (IsZerosFills == true)
                            {
                                newDocNo = QueryHelper.FillsWithZeros(noOfDigits, (double.Parse(lastObj.DocumentNumber) + 1).ToString()).ToString();
                            }
                            else
                            {
                                newDocNo = (double.Parse(lastObj.DocumentNumber) + 1).ToString();
                            }
                        }

                    }
                    else
                    {
                        if (IsZerosFills == true)
                        {
                            newDocNo = QueryHelper.FillsWithZeros(noOfDigits, "1").ToString();
                        }
                        else
                        {
                            newDocNo = "1";
                        }
                    }
                    GeneratedDocNo = DepartmentCode + "-" + YearFormat + "-" + MonthFormat + "-" + newDocNo;
                }
            }
            else
            {
                if (lastDocNo.Contains("-"))
                {
                    var ar = lastDocNo.Split('-');
                    newDocNo = (double.Parse(ar[3]) + 1).ToString();
                }
                else
                {
                    newDocNo = (double.Parse(lastDocNo) + 1).ToString();
                }
                GeneratedDocNo = newDocNo;
            }
            return Json(GeneratedDocNo, JsonRequestBehavior.AllowGet);
            //var docNo = QueryHelper.DocLastNum(id, "PurchaseReturns");
            //double i = (docNo) + 1;
            //return Json(i, JsonRequestBehavior.AllowGet);
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
