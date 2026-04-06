using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using MyERP.Models;
using System.Security.Claims;
using System.Threading.Tasks;
using MyERP.Repository;
using Newtonsoft.Json;


namespace MyERP.Controllers.WarehouseManagement
{
    public class SalesReturnController : ViewToStringController//: Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        // GET: SalesReturn
        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            SystemSetting systemSetting = db.SystemSettings.Any() ? db.SystemSettings.FirstOrDefault() : new SystemSetting();
            ViewBag.ShowSerialNumbers = systemSetting.ShowSerialNumbers == true;
            ViewBag.InabilityToEditSalesAndPurchaseInvoicesAndReturns = systemSetting.InabilityToEditSalesAndPurchaseInvoicesAndReturns;
            ////-------------------- Notification-------------------------////
            Notification.GetNotification("SalesReturn", "View", "Index", null, null, "مرتجع بيع");
            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;

            IQueryable<SalesReturn> salesReturns;
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

            var depIds = db.Departments.Where(d => (userId == 1 || db.UserDepartments.Where(u => u.UserId == userId && u.Privilege == true).Select(u => u.DepartmentId).Contains(d.Id)) && d.IsDeleted == false && d.IsActive == true).Select(d => (int?)d.Id);
            if (string.IsNullOrEmpty(searchWord))
            {
                salesReturns = db.SalesReturns.Where(s => s.IsDeleted == false).OrderByDescending(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                //&& depIds.Contains(s.DepartmentId) && s.PosId == null).Include(s => s.Branch).Include(s => s.Currency).Include(s => s.Department).Include(s => s.Warehouse).OrderByDescending(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.SalesReturns.Where(s => s.IsDeleted == false).Count();/*&& depIds.Contains(s.DepartmentId) && s.PosId == null).Count();*/
            }
            else
            {
                salesReturns = db.SalesReturns.Where(s => s.IsDeleted == false && depIds.Contains(s.DepartmentId) && (s.DocumentNumber.Contains(searchWord) || s.Branch.ArName.Contains(searchWord) || s.Department.ArName.Contains(searchWord) || s.Warehouse.ArName.Contains(searchWord) || s.Notes.Contains(searchWord) || s.Customer.ArName.Contains(searchWord)) && s.PosId == null).Include(s => s.Department).Include(s => s.Warehouse).OrderByDescending(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.SalesReturns.Where(s => s.IsDeleted == false && depIds.Contains(s.DepartmentId) && (s.DocumentNumber.Contains(searchWord) || s.Branch.ArName.Contains(searchWord) || s.Department.ArName.Contains(searchWord) || s.Warehouse.ArName.Contains(searchWord) || s.Notes.Contains(searchWord) || s.Customer.ArName.Contains(searchWord)) && s.PosId == null).Count();
            }
            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;

            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة مرتجع البيع",
                EnAction = "Index",
                ControllerName = "SalesReturn",
                UserName = User.Identity.Name,
                UserId = userId,
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            ; return View(salesReturns.ToList());
        }

        public async Task<ActionResult> Panel()
        {
            SystemSetting systemSetting = await db.SystemSettings.AnyAsync() ? await db.SystemSettings.FirstOrDefaultAsync() : new SystemSetting();
            ViewBag.ServiceFeesPercentage = systemSetting.ServiceFeesPercentage.HasValue ? systemSetting.ServiceFeesPercentage : 0;
            ViewBag.EnableAdditionalItemsOnPos = systemSetting.EnableAdditionalItemsOnPos;

            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            ViewBag.CashBoxId = await db.UserCashBoxes.Where(x => x.UserId == userId && x.Privilege == true).Select(x => x.CashBoxId).FirstOrDefaultAsync();
            if (userId == 1)
            {
                ViewBag.DepartmentId = new SelectList(await db.Departments.Where(d => d.IsActive == true && d.IsDeleted == false).Select(b => new
                {
                    b.Id,
                    ArName = b.Code + " - " + b.ArName
                }).ToListAsync(), "Id", "ArName", systemSetting.DefaultDepartmentId);
                ViewBag.WarehouseId = new SelectList(await db.Warehouses.Where(b => b.IsActive == true && b.IsDeleted == false && b.DepartmentId == systemSetting.DefaultDepartmentId).Select(b => new
                {
                    b.Id,
                    ArName = b.Code + " - " + b.ArName
                }).ToListAsync(), "Id", "ArName", systemSetting.DefaultWarehouseId);

            }
            else
            {
                ViewBag.DepartmentId = new SelectList(await db.UserDepartments.Where(d => d.UserId == userId && d.Department.IsDeleted == false && d.Department.IsActive == true && d.Privilege == true).Select(b => new
                {
                    Id = b.DepartmentId,
                    ArName = b.Department.Code + " - " + b.Department.ArName
                }).ToListAsync(), "Id", "ArName", systemSetting.DefaultDepartmentId);
                ViewBag.WarehouseId = new SelectList(await db.UserWareHouses.Where(b => b.UserId == userId && b.Warehouse.DepartmentId == systemSetting.DefaultDepartmentId).Select(b => new
                {
                    Id = b.WareHouseId,
                    ArName = b.Warehouse.Code + " - " + b.Warehouse.ArName
                }).ToListAsync(), "Id", "ArName", systemSetting.DefaultWarehouseId);
            }

            ViewBag.VendorOrCustomerId = new SelectList(await db.Customers.Where(a => a.IsActive == true && a.IsDeleted == false && a.IncludeFees == true).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }).ToListAsync(), "Id", "ArName");

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

            return View(await db.ItemGroups.Where(x => x.IsActive == true && x.IsDeleted == false).Include(x => x.Items).ToListAsync());
        }

        // GET: SalesReturn/Edit/5
        public async Task<ActionResult> AddEdit(int? id)
        {
            UserRepository userRepository = new UserRepository(db);
            DepartmentRepository departmentRepository = new DepartmentRepository(db);
            WarehouseRepository warehouseRepository = new WarehouseRepository(db);
            CashboxReposistory cashboxReposistory = new CashboxReposistory(db);
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            SystemSetting systemSetting = await db.SystemSettings.AnyAsync() ? await db.SystemSettings.FirstOrDefaultAsync() : new SystemSetting();
            ViewBag.ApplyTaxes = systemSetting.ApplyTaxesOnSalesIvoiceAuto;
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
                ViewBag.BankIdForVisa = new SelectList(banks, "Id", "ArName", systemSetting.DefaultBankId);
                ViewBag.BankAccountIdForVisa = new SelectList(bankAccounts, "Id", "AccountNumber");
                ViewBag.BankIdForCheque = new SelectList(banks, "Id", "ArName", systemSetting.DefaultBankId);
                ViewBag.BankAccountIdForCheque = new SelectList(bankAccounts, "Id", "AccountNumber");
                ViewBag.DepartmentId = new SelectList(departmentRepository.UserDepartments(userId), "Id", "ArName", systemSetting.DefaultDepartmentId);

                ViewBag.WarehouseId = new SelectList(warehouseRepository.UserWarehouses(userId, systemSetting.DefaultDepartmentId), "Id", "ArName", systemSetting.DefaultWarehouseId);
                ViewBag.CashBoxId = new SelectList(cashboxReposistory.UserCashboxes(userId, systemSetting.DefaultDepartmentId), "Id", "ArName", systemSetting.DefaultCashBoxId);
                List<PaymentMethod> paymentMethods = db.PaymentMethods.Where(p => p.IsActive == true && p.IsDeleted == false).ToList();
                ViewBag.PaymentMethods = paymentMethods;
                ViewBag.VendorOrCustomerId = new SelectList(db.Customers.Where(a => (a.IsActive == true && a.IsDeleted == false)).Select(b => new
                {
                    b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                ViewBag.CustomerRepId = new SelectList(db.CustomerReps.Where(a => (a.IsActive == true)).OrderBy(a => a.IsDefault).Select(b => new
                {
                    b.Employee.Id,
                    ArName = b.Employee.Code + " - " + b.Employee.ArName
                }), "Id", "ArName");
                ViewBag.CostCenterId = new SelectList(db.CostCenters.Where(a => a.IsActive && !a.IsDeleted && a.TypeId == 2).Select(b => new
                {
                    b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");


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
            SalesReturn salesReturn = db.SalesReturns.Find(id);
            if (salesReturn == null)
            {
                return HttpNotFound();
            }
            var SalesInvoiceId = salesReturn.SalesReturnsDetails.FirstOrDefault().SalesInvoiceId;//.Select(a=>a.DocumentNumber) ;
            if (SalesInvoiceId > 0)
            {
                var doc = db.SalesInvoices.Where(a => a.IsActive == true && a.IsDeleted == false && a.Id == SalesInvoiceId).Select(a => a.DocumentNumber).FirstOrDefault();
                ViewBag.SalesIvnvoiceDocNo = doc;
            }
            var cashBoxes = await cashboxReposistory.UserCashboxes(userId, salesReturn.DepartmentId).ToListAsync();

            ViewBag.VendorOrCustomerId = new SelectList(db.Customers.Where(a => (a.IsActive == true && a.IsDeleted == false)).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", salesReturn.VendorOrCustomerId);
            ViewBag.CustomerRepId = new SelectList(db.CustomerReps.Where(a => (a.IsActive == true)).OrderBy(a => a.IsDefault).Select(b => new
            {
                b.Employee.Id,
                ArName = b.Employee.Code + " - " + b.Employee.ArName
            }), "Id", "ArName", salesReturn.CustomerRepId);
            ViewBag.DepartmentId = new SelectList(departmentRepository.UserDepartments(userId), "Id", "ArName", salesReturn.DepartmentId);
            ViewBag.WarehouseId = new SelectList(warehouseRepository.UserWarehouses(userId, salesReturn.DepartmentId), "Id", "ArName", salesReturn.WarehouseId);
            ViewBag.CostCenterId = new SelectList(db.CostCenters.Where(a => a.IsActive && !a.IsDeleted && a.TypeId == 2).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", salesReturn.CostCenterId);

            var sysPageId = QueryHelper.SourcePageId("SalesReturns");
            JournalEntry journal = db.JournalEntries.FirstOrDefault(j => j.SourceId == id && j.SourcePageId == sysPageId);

            foreach (var method in salesReturn.SalesReturnPaymentMethods)
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
            ViewBag.Journal = journal;
            ViewBag.JournalDocumentNumber = journal != null&&journal.DocumentNumber != null ? journal.DocumentNumber.Replace("-", "") : "";
            ViewBag.VoucherDate = salesReturn.VoucherDate.ToString("yyyy-MM-ddTHH:mm"); ;
            return View(salesReturn);
        }

        [HttpPost]
        //[ValidateAntiForgeryToken]
        public ActionResult AddEdit(SalesReturn salesReturn, ICollection<PurchaseSaleSerialNumber> serialNumbers)
        {
           
            int userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            var ProjectName = System.Web.Configuration.WebConfigurationManager.AppSettings["ProjectName"];
            salesReturn.UserId = userId;
            int? cashierUserId = null;
            int? posId = null, shiftId = null;
            var pos = db.Pos.Where(p => p.CurrentCashierUserId == userId && p.PosStatusId == 2).FirstOrDefault();
            if (pos != null)
            {
                posId = pos.Id;
                shiftId = pos.CurrentShiftId;
                cashierUserId = userId;
            }
            if (posId == null)

            {
                if (ProjectName == "Genoise")
                {
                    if (salesReturn.DepartmentId != 28)
                    {
                        return Json(new { success = "false", errors = "NotCashier" });
                    }
                }


            }
            if (ModelState.IsValid)
            {
                // Handle datetime zone hours
                foreach (var detail in salesReturn.SalesReturnsDetails)
                {
                    if (detail.ExpireDate.HasValue)
                        detail.ExpireDate = detail.ExpireDate.Value.AddHours(6);
                }

                var id = salesReturn.Id;
                salesReturn.IsDeleted = false;
                if (salesReturn.Id > 0)
                {
                    var systemSetting = db.SystemSettings.FirstOrDefault();
                    var InabilityToEditSalesAndPurchaseInvoicesAndReturns = systemSetting.InabilityToEditSalesAndPurchaseInvoicesAndReturns;
                    if (InabilityToEditSalesAndPurchaseInvoicesAndReturns == true)
                    {
                        return Json(new { success = "Cannot Be Edited" });
                    }

                    if (db.SalesReturns.Find(salesReturn.Id).IsPosted == true)
                    {
                        return Content("false");
                    }

                    MyXML.xPathName = "Details";
                    var salesReturnDetails = MyXML.GetXML(salesReturn.SalesReturnsDetails);
                    MyXML.xPathName = "PaymentMethods";
                    var PaymentMethods = MyXML.GetXML(salesReturn.SalesReturnPaymentMethods);

                    MyXML.xPathName = "SerialNumbers";
                    var SerialNumbersXML = MyXML.GetXML(serialNumbers);

                    db.SalesReturns_Update(salesReturn.Id, salesReturn.DocumentNumber, salesReturn.BranchId, salesReturn.WarehouseId, salesReturn.DepartmentId, salesReturn.VoucherDate, salesReturn.VendorOrCustomerId, salesReturn.CurrencyId, salesReturn.CurrencyEquivalent, salesReturn.Total, salesReturn.TotalItemsDiscount, salesReturn.SalesTaxes, salesReturn.TotalAfterTaxes, salesReturn.VoucherDiscountValue, salesReturn.VoucherDiscountPercentage, salesReturn.NetTotal, salesReturn.Paid, salesReturn.ValidityPeriod, salesReturn.DeliveryPeriod, salesReturn.CostCenterId, salesReturn.CurrentQuantity, salesReturn.DestinationWarehouseId, salesReturn.SystemPageId, salesReturn.SelectedId, salesReturn.TotalCostPrice, salesReturn.TotalItemDirectExpenses, salesReturn.IsDelivered, salesReturn.IsAccepted, salesReturn.IsLinked, salesReturn.IsCompleted, salesReturn.IsPosted, salesReturn.UserId, salesReturn.IsActive, salesReturn.IsDeleted, salesReturn.AutoCreated, salesReturn.Notes, salesReturn.Image, salesReturn.UpdatedId, salesReturnDetails, PaymentMethods, SerialNumbersXML, posId, cashierUserId, shiftId, false, false, salesReturn.CustomerRepId, salesReturn.SalesInvoiceId, salesReturn.ServiceFees, salesReturn.DeliveryCost, salesReturn.RefundableInsurance, salesReturn.PosCustomerId, salesReturn.CustomerType);
                    Notification.GetNotification("SalesReturn", id > 0 ? "Edit" : "Add", "AddEdit", id, null, "مرتجع بيع");
                    QueryHelper.AddLog(new MyLog()
                    {
                        ArAction = "تعديل  مرتجع بيع ",
                        EnAction = "AddEdit",
                        ControllerName = "SalesReturn",
                        UserName = User.Identity.Name,
                        UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                        LogDate = DateTime.Now,
                        RequestMethod = "POST",
                        SelectedItem = salesReturn.Id,
                        CodeOrDocNo = salesReturn.DocumentNumber
                    });

                    return Json(new { success = "true", id = salesReturn.Id });

                }
                else
                {
                    salesReturn.IsActive = true;
                    MyXML.xPathName = "Details";
                    var salesReturnDetails = MyXML.GetXML(salesReturn.SalesReturnsDetails);
                    MyXML.xPathName = "PaymentMethods";
                    var PaymentMethods = MyXML.GetXML(salesReturn.SalesReturnPaymentMethods);
                    MyXML.xPathName = "SerialNumbers";
                    var SerialNumbersXML = MyXML.GetXML(serialNumbers);
                    var idresult = new System.Data.Entity.Core.Objects.ObjectParameter("Id", typeof(Int32));
                    db.SalesReturns_Insert(idresult, salesReturn.BranchId, salesReturn.WarehouseId, salesReturn.DepartmentId, salesReturn.VoucherDate, salesReturn.VendorOrCustomerId, salesReturn.CurrencyId, salesReturn.CurrencyEquivalent, salesReturn.Total, salesReturn.TotalItemsDiscount, salesReturn.SalesTaxes, salesReturn.TotalAfterTaxes, salesReturn.VoucherDiscountValue, salesReturn.VoucherDiscountPercentage, salesReturn.NetTotal, salesReturn.Paid, salesReturn.ValidityPeriod, salesReturn.DeliveryPeriod, salesReturn.CostCenterId, salesReturn.CurrentQuantity, salesReturn.DestinationWarehouseId, salesReturn.SystemPageId, salesReturn.SelectedId, salesReturn.TotalCostPrice, salesReturn.TotalItemDirectExpenses, salesReturn.IsDelivered, salesReturn.IsAccepted, salesReturn.IsLinked, salesReturn.IsCompleted, false, salesReturn.UserId, salesReturn.IsActive, salesReturn.IsDeleted, salesReturn.AutoCreated, salesReturn.Notes, salesReturn.Image, salesReturn.UpdatedId, salesReturnDetails, PaymentMethods, SerialNumbersXML, posId, cashierUserId, shiftId, false, false, salesReturn.CustomerRepId, salesReturn.SalesInvoiceId, salesReturn.ServiceFees, salesReturn.DeliveryCost, salesReturn.RefundableInsurance, salesReturn.PosCustomerId, salesReturn.CustomerType);

                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("SalesReturn", id > 0 ? "Edit" : "Add", "AddEdit", id, null, "مرتجع بيع");
                    QueryHelper.AddLog(new MyLog()
                    {
                        ArAction = "اضافة مرتجع بيع",
                        EnAction = "AddEdit",
                        ControllerName = "SalesReturn",
                        UserName = User.Identity.Name,
                        UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                        LogDate = DateTime.Now,
                        RequestMethod = "POST",
                        SelectedItem = db.SalesReturns.Max(i => i.Id),
                        CodeOrDocNo = salesReturn.DocumentNumber
                    });

                    return Json(new { success = "true", id = idresult.Value });

                }

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

            var SerialNumbers = JsonConvert.SerializeObject(db.PurchaseSaleSerialNumbers.Where(s => s.SelectedId == invoiceId && s.ItemId == itemId && s.PageSourceId == db.SystemPages.FirstOrDefault(d => d.TableName == "SalesReturns").Id).Select(d => new { d.ItemId, d.SerialNumber }));


            return Json(SerialNumbers, JsonRequestBehavior.AllowGet);
        }

        // GET
        public ActionResult AddEditSerialNumber(int? id, int itemId)
        {
            SystemSetting sysObj = db.SystemSettings.FirstOrDefault();
            ViewData["ShowSerialNumbers"] = true;


            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            SalesReturn salesReturn = db.SalesReturns.Find(id);
            if (salesReturn == null)
            {
                return HttpNotFound();
            }
            if (itemId == 0)
            {
                var x = db.SalesReturnsDetails.Where(p => p.MainDocId == id).FirstOrDefault().ItemId;
                itemId = int.Parse(x.ToString());
            }
            ViewBag.GeneralItemId = itemId;
            int sysPageId = QueryHelper.SourcePageId("SalesReturns");


            ViewBag.BranchId = new SelectList(db.Branches.Where(a => (a.IsActive == true && a.IsDeleted == false)).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", salesReturn.BranchId);
            if (userId == 1)
            {
                ViewBag.DepartmentId = new SelectList(db.Departments.Where(d => d.IsActive == true && d.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName", salesReturn.DepartmentId);
                ViewBag.WarehouseId = new SelectList(db.Warehouses.Where(b => b.IsActive == true && b.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName", salesReturn.WarehouseId);

            }
            else
            {
                ViewBag.DepartmentId = new SelectList(db.UserDepartments.Where(d => d.UserId == userId).Select(b => new
                {
                    Id = b.DepartmentId,
                    ArName = b.Department.Code + " - " + b.Department.ArName
                }), "Id", "ArName", salesReturn.DepartmentId);
                ViewBag.WarehouseId = new SelectList(db.UserWareHouses.Where(b => b.UserId == userId).Select(b => new
                {
                    Id = b.WareHouseId,
                    ArName = b.Warehouse.Code + " - " + b.Warehouse.ArName
                }), "Id", "ArName", salesReturn.WarehouseId);

            }
            ViewBag.SerialNumbers = JsonConvert.SerializeObject(db.PurchaseSaleSerialNumbers.Where(s => s.SelectedId == id && s.ItemId == itemId && s.PageSourceId == db.SystemPages.FirstOrDefault(d => d.TableName == "SalesReturns").Id).Select(d => new { d.ItemId, d.SerialNumber }));



            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح سيريال مرتجع البيع",
                EnAction = "AddEdit",
                ControllerName = "SalesReturn",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = salesReturn.Id,
                CodeOrDocNo = salesReturn.DocumentNumber
            });

            return View(salesReturn);
        }

        [HttpPost]
        //[ValidateAntiForgeryToken]
        public JsonResult AddEditSerialNumber(int SalesReturnId, int itemId, ICollection<PurchaseSaleSerialNumber> serialNumbers)
        {
            if (ModelState.IsValid)
            {
                if (SalesReturnId == 0)
                {
                    return Json("false");
                }
                if (SalesReturnId > 0)
                {
                    if (db.SalesReturns.Find(SalesReturnId).IsPosted == true)
                    {
                        return Json("false");
                    }
                    MyXML.xPathName = "SerialNumbers";
                    var SerialNumbersXML = MyXML.GetXML(serialNumbers);
                    db.AddEditSerialNumber(SalesReturnId, itemId, QueryHelper.SourcePageId("SalesReturns"), SerialNumbersXML);

                    Notification.GetNotification("SalesReturn", "AddEditSerialNumber", "AddEditSerialNumber", SalesReturnId, null, "  سيريال مرتجع البيع ");
                }

                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = SalesReturnId > 0 ? "تعديل سيريال مرتجع بيع " : "تعديل سيريال مرتجع بيع",
                    EnAction = "AddEditSerialNumber",
                    ControllerName = "SalesReturn",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = SalesReturnId,
                    CodeOrDocNo = db.SalesReturns.Where(p => p.Id == SalesReturnId).FirstOrDefault().DocumentNumber
                });

                return Json(new { success = "true", id = SalesReturnId });
            }
            var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .Select(x => new { x.Key, x.Value.Errors })
                    .ToArray();

            return Json(errors);
        }

        // POST: SalesReturn/Delete/5
        [HttpPost, ActionName("Delete")]
        //[ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                var systemSetting = db.SystemSettings.FirstOrDefault();
                var InabilityToEditSalesAndPurchaseInvoicesAndReturns = systemSetting.InabilityToEditSalesAndPurchaseInvoicesAndReturns;
                if (InabilityToEditSalesAndPurchaseInvoicesAndReturns == true)
                {
                    return Content("Cannot Be Deleted");
                }
                SalesReturn salesReturn = db.SalesReturns.Find(id);
                if (salesReturn.IsPosted == true)
                {
                    return Content("false");
                }
                var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                db.SalesReturns_Delete(id, userId);
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = " حذف فاتورة مرتجع بيع",
                    EnAction = "AddEdit",
                    ControllerName = "SalesReturn",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    CodeOrDocNo = salesReturn.DocumentNumber
                });

                ////-------------------- Notification-------------------------////
                Notification.GetNotification("SalesReturn", "Delete", "Delete", id, null, "مرتجع بيع");
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
            var lastObj = db.SalesReturns.Where(a => a.IsActive == true && a.IsDeleted == false && a.DepartmentId == id).OrderByDescending(a => a.Id).FirstOrDefault();
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
                    lastObj = db.SalesReturns.Where(a => a.IsActive == true && a.IsDeleted == false && a.DepartmentId == id && a.VoucherDate.Month == VoucherDate.Value.Month && a.VoucherDate.Year == VoucherDate.Value.Year).OrderByDescending(a => a.Id).FirstOrDefault();
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
                    lastObj = db.SalesReturns.Where(a => a.IsActive == true && a.IsDeleted == false && a.DepartmentId == id && a.VoucherDate.Year == VoucherDate.Value.Year).OrderByDescending(a => a.Id).FirstOrDefault();

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
            //var docNo = QueryHelper.DocLastNum(id, "SalesReturns");
            //double i = (docNo) + 1;
            //return Json(i, JsonRequestBehavior.AllowGet);
        }

        //[SkipERPAuthorize]
        //public JsonResult CustomerSales(int customerId, int? itemId)
        //{
        //    return Json(db.Customer_Sales(customerId, null, null, itemId).OrderByDescending(x => x.VoucherDate).Select(x => new { x.ArName, x.DocumentNumber, x.Id, x.ItemId, x.Price, x.Qty, VoucherDate = x.VoucherDate.ToString("yyyy-MM-dd HH:mm") }), JsonRequestBehavior.AllowGet);
        //}
        [SkipERPAuthorize]
        public JsonResult CustomerSales(int? departmentId, string docNum)
        {
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

            SystemSetting systemSetting = db.SystemSettings.Any() ? db.SystemSettings.FirstOrDefault() : new SystemSetting();
            ViewBag.UseExpiryDateForItems = systemSetting.UseExpiryDateForItems;
            var salesInvoice = db.SalesInvoices.Where(s => s.DocumentNumber == docNum && s.DepartmentId == departmentId).FirstOrDefault();
            if (salesInvoice != null)
            {
                //var salesInvoiceDetails = await db.SalesInvoiceDetails.Where(x => x.MainDocId == salesInvoice.Id&&(accessoryReturn==false ||x.Item.IsAccessory==true)).ToListAsync();
                var salesInvoiceDetails = db.LinkSalesReturnWithSalesInvoice(salesInvoice.Id, salesInvoice.DepartmentId).ToList();
                if (salesInvoiceDetails.Count() == 0)
                {
                    return Json(new { success = "NoData" }, JsonRequestBehavior.AllowGet);
                }
                else
                {
                    var view = RenderRazorViewToString("LinkWithSalesInvoice", salesInvoiceDetails);
                    return Json(new { view, Paid = salesInvoice.Paid, VoucherDate = cTime.ToString("yyyy-MM-ddTHH:mm"), customerId = salesInvoice.VendorOrCustomerId, SalesInvoiceId = salesInvoice.Id, CustomerType = salesInvoice.CustomerType, DepartmentId = salesInvoice.DepartmentId }, JsonRequestBehavior.AllowGet);
                }
            }
            else
                return Json(new { success = "false" }, JsonRequestBehavior.AllowGet);
        }

        [SkipERPAuthorize]
        public JsonResult ValidateItemQuantity(int salesInvoiceId, int itemId)
        {
            var soldQuantity = db.SalesInvoiceDetails.Where(x => x.MainDocId == salesInvoiceId && x.ItemId == itemId).Sum(x => x.Qty);
            var returnedQuantity = db.SalesReturnsDetails.Where(x => x.SalesInvoiceId == salesInvoiceId && x.ItemId == itemId).Sum(x => x.Qty);
            var diff = (soldQuantity > 0 ? soldQuantity : 0) - (returnedQuantity > 0 ? returnedQuantity : 0);
            return Json(diff > 0 ? diff : 0, JsonRequestBehavior.AllowGet);
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
