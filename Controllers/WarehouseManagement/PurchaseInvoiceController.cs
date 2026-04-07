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
using System.Data.Entity.Core.Objects;
using System.Threading.Tasks;
using MyERP.Repository;
using Newtonsoft.Json;
using System.Xml.Linq;
using System.Text.RegularExpressions;

namespace MyERP.Controllers
{
    public class PurchaseInvoiceController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        // GET: PurchaseInvoice
        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            SystemSetting sysObj = db.SystemSettings.FirstOrDefault();
            ViewBag.UseExpiryDateForItems = sysObj.UseExpiryDateForItems;
            ViewBag.InabilityToEditSalesAndPurchaseInvoicesAndReturns = sysObj.InabilityToEditSalesAndPurchaseInvoicesAndReturns;

            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة فواتير المشتريات",
                EnAction = "Index",
                ControllerName = "PurchaseInvoice",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            ////-------------------- Notification-------------------------////
            Notification.GetNotification("PurchaseInvoice", "View", "Index", null, null, "فواتير المشتريات");
            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

            var depIds = db.Departments.Where(d => (userId == 1 || db.UserDepartments.Where(u => u.UserId == userId && u.Privilege == true).Select(u => u.DepartmentId).Contains(d.Id)) && d.IsDeleted == false && d.IsActive == true).Select(d => (int?)d.Id);
            IQueryable<PurchaseInvoice> purchaseInvoices;

            if (string.IsNullOrEmpty(searchWord))
            {
                purchaseInvoices = db.PurchaseInvoices.Where(s => s.IsDeleted == false && depIds.Contains(s.DepartmentId)).OrderByDescending(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.PurchaseInvoices.Where(s => s.IsDeleted == false && depIds.Contains(s.DepartmentId)).Count();

            }
            else
            {
                purchaseInvoices = db.PurchaseInvoices.Where(s => s.IsDeleted == false && depIds.Contains(s.DepartmentId) && (s.DocumentNumber.Contains(searchWord) || s.Branch.ArName.Contains(searchWord) || s.Department.ArName.Contains(searchWord) || s.Warehouse.ArName.Contains(searchWord) || s.Notes.Contains(searchWord) || s.Vendor.ArName.Contains(searchWord) || s.VoucherDate.ToString().Contains(searchWord))).OrderByDescending(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.PurchaseInvoices.Where(s => s.IsDeleted == false && depIds.Contains(s.DepartmentId) && (s.DocumentNumber.Contains(searchWord) || s.Branch.ArName.Contains(searchWord) || s.Department.ArName.Contains(searchWord) || s.Warehouse.ArName.Contains(searchWord) || s.Notes.Contains(searchWord) || s.Notes.Contains(searchWord) || s.Vendor.ArName.Contains(searchWord) || s.VoucherDate.ToString().Contains(searchWord))).Count();
            }

            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            return View(purchaseInvoices.ToList());

        }

        // GET: PurchaseInvoice/Edit/5
        public async Task<ActionResult> AddEdit(int? id)
        {
            var session = Session["lang"] != null ? Session["lang"].ToString() : "ar";
            FillVehicleLookupData(session);
            //-- Check if this Purchase Invoice Exist In Cash Issue Voucher
            var check = db.CheckPurchaseInvoiceExistInCashIssueVoucher(id).FirstOrDefault();
            ViewBag.check = check;
            SystemSetting systemSetting = await db.SystemSettings.AnyAsync() ? await db.SystemSettings.FirstOrDefaultAsync() : new SystemSetting();
            ViewData["ShowDirectExpense"] = systemSetting.ShowDirectExpense == true;
            if (System.Web.Configuration.WebConfigurationManager.AppSettings["ProjectName"] == "Kimo")
                ViewData["ShowSalesPrices"] = true;
            ViewData["ShowSerialNumbers"] = systemSetting.ShowSerialNumbers == true;
            ViewBag.WarehouseItemsDistribution = systemSetting.WarehouseItemsDistribution == true;
            ViewBag.PayViaCarryOver = systemSetting.PayViaCarryOver == true;
            ViewBag.PayViaCash = systemSetting.PayViaCash == true;
            ViewBag.PayViaCheque = systemSetting.PayViaCheque == true;
            ViewBag.PayViaVisa = systemSetting.PayViaVisa == true;
            ViewBag.UseCostCenter = systemSetting.UseCostCenter;
            ViewBag.UseExpiryDateForItems = systemSetting.UseExpiryDateForItems;
            ViewBag.UseDifferentCurrencies = systemSetting.UseDifferentCurrencies;
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            UserRepository userRepository = new UserRepository(db);
            DepartmentRepository departmentRepository = new DepartmentRepository(db);
            WarehouseRepository warehouseRepository = new WarehouseRepository(db);
            CashboxReposistory cashboxReposistory = new CashboxReposistory(db);

            int roleId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("RoleId").Value);
            var DisplayPrice = userId == 1 ? true : await db.UserPrivileges.Where(u => u.PageAction.PageId == 57 && u.PageAction.EnName == "DisplayPrice" && u.PageAction.Action == "DisplayPrice" && u.UserId == userId).Select(x => x.Privileged).FirstOrDefaultAsync();
            DisplayPrice = DisplayPrice ?? await db.RolePrivileges.Where(u => u.PageAction.PageId == 57 && u.PageAction.EnName == "DisplayPrice" && u.PageAction.Action == "DisplayPrice" && u.RoleId == roleId).Select(x => x.Privileged).FirstOrDefaultAsync();
            ViewBag.DisplayPrice = DisplayPrice;
            // ViewBag.DisplayPrice = await userRepository.HasActionPrivilege(userId, "DisplayPrice", "DisplayPrice");
            var banks = db.Banks.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
            {
                b.Id,
                ArName = session.ToString() == "en" && b.EnName != null ? b.Code + " - " + b.EnName : b.Code + " - " + b.ArName
            }).ToList();
            var departments = await departmentRepository.UserDepartments(1).ToListAsync();//show all deprartments so that user can select factory department

            ViewBag.LinkWithDocModalDepartmentId = new SelectList(departments, "Id", "ArName", systemSetting.DefaultDepartmentId);
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
                ViewBag.VendorOrCustomerId = new SelectList(db.Vendors.Where(a => (a.IsActive == true && a.IsDeleted == false)).Select(b => new
                {
                    b.Id,
                    ArName = session.ToString() == "en" && b.EnName != null ? b.Code + " - " + b.EnName : b.Code + " - " + b.ArName
                }), "Id", "ArName");
                ViewBag.CostCenterId = new SelectList(db.CostCenters.Where(a => a.IsActive && !a.IsDeleted && a.TypeId == 2).Select(b => new
                {
                    b.Id,
                    ArName = session.ToString() == "en" && b.EnName != null ? b.Code + " - " + b.EnName : b.Code + " - " + b.ArName
                }), "Id", "ArName");


                // DirectExpensesPaymentMethodId //ADD

                ViewBag.ExpensesPaymentMethod = new SelectList(new List<dynamic>
                {
                new { Id = 1, ArName =session.ToString() == "en"?"Cash": "نقدى" },
                new { Id = 2, ArName =session.ToString() == "en"?"Vendor": "مورد" },
                new { Id = 3, ArName =session.ToString() == "en"?"Custody": "عهدة" }
                }, "Id", "ArName");

                ViewBag.DirectExpensesId = new SelectList(db.DirectExpenses.Where(a => (a.IsActive == true && a.IsDeleted == false)).Select(b => new
                {
                    b.Id,
                    ArName = session.ToString() == "en" && b.EnName != null ? b.Code + " - " + b.EnName : b.Code + " - " + b.ArName
                }), "Id", "ArName");
                //  ViewBag.DirectExpenses = JsonConvert.SerializeObject(db.DirectExpenses.Where(i => i.IsDeleted == false && i.IsActive == true).Select(d => new { d.Id, ArName = d.Code + " - " + d.ArName }));

                ViewBag.CashBoxExpensesId = new SelectList(cashboxReposistory.UserCashboxes(userId, systemSetting.DefaultDepartmentId), "Id", "ArName", systemSetting.DefaultCashBoxId);

                ViewBag.VendorExpensesId = new SelectList(db.Vendors.Where(a => (a.IsActive == true && a.IsDeleted == false)).Select(b => new
                {
                    b.Id,
                    ArName = session.ToString() == "en" && b.EnName != null ? b.Code + " - " + b.EnName : b.Code + " - " + b.ArName
                }), "Id", "ArName");

                ViewBag.EmployeeExpensesId = new SelectList(db.Employees.Where(a => (a.IsActive == true && a.IsDeleted == false)).Select(b => new
                {
                    b.Id,
                    ArName = session.ToString() == "en" && b.EnName != null ? b.Code + " - " + b.EnName : b.Code + " - " + b.ArName
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
            PurchaseInvoice purchaseInvoice = await db.PurchaseInvoices.FindAsync(id);
            if (purchaseInvoice == null)
            {
                return HttpNotFound();
            }
            var cashBoxes = await cashboxReposistory.UserCashboxes(userId, purchaseInvoice.DepartmentId).ToListAsync();

            int sysPageId = QueryHelper.SourcePageId("PurchaseInvoice");

            JournalEntry journal = db.JournalEntries.FirstOrDefault(j => j.SourceId == id && j.SourcePageId == sysPageId);
            ViewBag.Journal = journal;
            ViewBag.Journal.DocumentNumber = journal != null ? journal.DocumentNumber.Replace("-", "") : "";
            ViewBag.VendorOrCustomerId = new SelectList(db.Vendors.Where(a => (a.IsActive == true && a.IsDeleted == false)).Select(b => new
            {
                b.Id,
                ArName = session.ToString() == "en" && b.EnName != null ? b.Code + " - " + b.EnName : b.Code + " - " + b.ArName
            }), "Id", "ArName", purchaseInvoice.VendorOrCustomerId);
            ViewBag.CostCenterId = new SelectList(db.CostCenters.Where(a => a.IsActive && !a.IsDeleted && a.TypeId == 2).Select(b => new
            {
                b.Id,
                ArName = session.ToString() == "en" && b.EnName != null ? b.Code + " - " + b.EnName : b.Code + " - " + b.ArName
            }), "Id", "ArName", purchaseInvoice.CostCenterId);
            foreach (var method in purchaseInvoice.PurchaseInvoicePaymentMethods)
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
            ViewBag.SerialNumbers = JsonConvert.SerializeObject(db.PurchaseSaleSerialNumbers.Where(s => s.SelectedId == id && s.PageSourceId == db.SystemPages.FirstOrDefault(d => d.TableName == "PurchaseInvoice").Id).Select(d => new { d.ItemId, d.SerialNumber }));
            ViewBag.DepartmentId = new SelectList(departmentRepository.UserDepartments(userId), "Id", "ArName", purchaseInvoice.DepartmentId);
            ViewBag.WarehouseId = new SelectList(warehouseRepository.UserWarehouses(userId, purchaseInvoice.DepartmentId), "Id", "ArName", purchaseInvoice.WarehouseId);

            if (purchaseInvoice.SystemPageId == 58)
            {
                ViewBag.SalesInoiveDocNum = await db.SalesInvoices.Where(x => x.Id == purchaseInvoice.SelectedId).Select(x => x.DocumentNumber).FirstOrDefaultAsync();
            }

            ViewBag.VoucherDate = purchaseInvoice.VoucherDate.ToString("yyyy-MM-ddTHH:mm");

            // DirectExpensesPaymentMethodId //Edit
            ViewBag.DirectExpensesId = new SelectList(db.DirectExpenses.Where(a => (a.IsActive == true && a.IsDeleted == false)).Select(b => new
            {
                b.Id,
                ArName = session.ToString() == "en" && b.EnName != null ? b.Code + " - " + b.EnName : b.Code + " - " + b.ArName
            }), "Id", "ArName");
            ViewBag.CashBoxExpensesId = new SelectList(cashBoxes, "Id", "ArName");


            ViewBag.VendorExpensesId = new SelectList(db.Vendors.Where(a => (a.IsActive == true && a.IsDeleted == false)).Select(b => new
            {
                b.Id,
                ArName = session.ToString() == "en" && b.EnName != null ? b.Code + " - " + b.EnName : b.Code + " - " + b.ArName
            }), "Id", "ArName");

            ViewBag.EmployeeExpensesId = new SelectList(db.Employees.Where(a => (a.IsActive == true && a.IsDeleted == false)).Select(b => new
            {
                b.Id,
                ArName = session.ToString() == "en" && b.EnName != null ? b.Code + " - " + b.EnName : b.Code + " - " + b.ArName
            }), "Id", "ArName");

            ViewBag.ExpensesPaymentMethod = new SelectList(new List<dynamic>
                {
                 new { Id = 1, ArName =session.ToString() == "en"?"Cash": "نقدى" },
                new { Id = 2, ArName =session.ToString() == "en"?"Vendor": "مورد" },
                new { Id = 3, ArName =session.ToString() == "en"?"Custody": "عهدة" }
                }, "Id", "ArName");
            //---------------------------------------------------------------------------------------------------//

            ViewBag.Next = QueryHelper.Next((int)id, "PurchaseInvoice");
            ViewBag.Previous = QueryHelper.Previous((int)id, "PurchaseInvoice");
            ViewBag.Last = QueryHelper.GetLast("PurchaseInvoice");
            ViewBag.First = QueryHelper.GetFirst("PurchaseInvoice");

            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح تفاصيل فاتورة الشراء",
                EnAction = "AddEdit",
                ControllerName = "PurchaseInvoice",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = purchaseInvoice.Id,
                CodeOrDocNo = purchaseInvoice.DocumentNumber
            });

            return View(purchaseInvoice);
        }

        // POST: PurchaseInvoice/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        //[ValidateAntiForgeryToken]
        public JsonResult AddEdit(PurchaseInvoice purchaseInvoice, ICollection<PurchaseSaleSerialNumber> serialNumbers, bool? alterPrices, ICollection<AlteredPrices> AlteredPrices, bool? distributeItemsOnWarehouses)
        {
            
            if (ModelState.IsValid)
            {
                var chassisValidationMessage = ValidateVehicleChassisNumbers(purchaseInvoice.PurchaseInvoiceDetails, purchaseInvoice.Id);
                if (!string.IsNullOrEmpty(chassisValidationMessage))
                    return Json(new { isValid = false, message = chassisValidationMessage }, JsonRequestBehavior.AllowGet);

                db.Database.CommandTimeout = 300;
                var id = purchaseInvoice.Id;
                purchaseInvoice.IsDeleted = false;
                purchaseInvoice.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                // Patch details
                DataTable patches = new DataTable("PatchDetails");
                DataColumn ItemId = new DataColumn("ItemId", typeof(int));
                DataColumn ExpireDate = new DataColumn("ExpireDate", typeof(DateTime));
                DataColumn PatchCode = new DataColumn("PatchCode", typeof(string));

                /*DataTable dt = new DataTable();
                dt.Columns.Add("ColName", typeof(int));
                dt.Rows.Add();
                dt.Rows[0][0] = DBNull.Value;
                dt.Rows.Add();
                dt.Rows[1][0] = 1;*/

                patches.Columns.Add(ItemId);
                patches.Columns.Add(ExpireDate);
                patches.Columns.Add(PatchCode);
                foreach (var detail in purchaseInvoice.PurchaseInvoiceDetails)
                {
                    if (detail.ExpireDate != null)
                    {
                        detail.ExpireDate = detail.ExpireDate.Value.AddHours(6);
                        var patch = db.Patches.Where(e => e.ExpireDate.Value.Day == detail.ExpireDate.Value.Day &&
                        e.ExpireDate.Value.Month == detail.ExpireDate.Value.Month &&
                        e.ExpireDate.Value.Year == detail.ExpireDate.Value.Year &&
                        e.ItemId == detail.ItemId).Any();
                        if (patch != true)
                        {
                            DataRow row = patches.NewRow();
                            row["ItemId"] = detail.ItemId;
                            row["ExpireDate"] = detail.ExpireDate.Value.AddHours(6);
                            row["PatchCode"] = detail.PatchCode;
                            patches.Rows.Add(row);
                        }
                    }
                }

                MyXML.xPathName = "PatchDetails";
                var purchaseInvoicePatchDetails = MyXML.GetXML(patches);

                if (purchaseInvoice.Id > 0)
                {
                    var systemSetting = db.SystemSettings.FirstOrDefault();
                    var InabilityToEditSalesAndPurchaseInvoicesAndReturns = systemSetting.InabilityToEditSalesAndPurchaseInvoicesAndReturns;
                    if (InabilityToEditSalesAndPurchaseInvoicesAndReturns == true)
                    {
                        return Json(new { success = "Cannot Be Edited" });
                    }

                    if (db.PurchaseInvoices.Find(purchaseInvoice.Id).IsPosted == true)
                    {
                        return Json("false");
                    }
                    MyXML.xPathName = "Details";

                    foreach (var d in purchaseInvoice.PurchaseInvoiceDetails)
                    {
                        var type = d.GetType();
                        System.Diagnostics.Debug.WriteLine("==== DETAIL TYPE: " + type.FullName);

                        foreach (var p in type.GetProperties())
                        {
                            object value = null;
                            string valueType = "null";

                            try
                            {
                                value = p.GetValue(d, null);
                                valueType = value == null ? "null" : value.GetType().FullName;
                            }
                            catch (Exception ex)
                            {
                                valueType = "ERR: " + ex.Message;
                            }

                            System.Diagnostics.Debug.WriteLine(p.Name + " => " + valueType);
                        }
                    }
                    var detail = purchaseInvoice.PurchaseInvoiceDetails.FirstOrDefault();
                    if (detail != null)
                    {
                        foreach (var p in detail.GetType().GetProperties())
                        {
                            try
                            {
                                var value = p.GetValue(detail, null);
                                var typeName = value == null ? "null" : value.GetType().FullName;
                                System.Diagnostics.Debug.WriteLine(p.Name + " => " + typeName);
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine(p.Name + " => ERROR: " + ex.Message);
                            }
                        }
                    }
                    db.Configuration.ProxyCreationEnabled = false;
                    var PurchaseInvoiceDetails = MyXML.GetXML(purchaseInvoice.PurchaseInvoiceDetails);
                    MyXML.xPathName = "PaymentMethods";
                    var InvoicePaymentMethods = MyXML.GetXML(purchaseInvoice.PurchaseInvoicePaymentMethods);
                    MyXML.xPathName = "SerialNumbers";
                    var SerialNumbersXML = MyXML.GetXML(serialNumbers);
                    MyXML.xPathName = "AlteredPrices";
                    var AlteredPricesXML = MyXML.GetXML(AlteredPrices);
                    MyXML.xPathName = "DirectExpenses";
                    var DirectExpenses = MyXML.GetXML(purchaseInvoice.PurchaseInvoiceDirectExpenses);
                    try
                    {
                        db.PurchaseInvoice_Update(
                            purchaseInvoice.Id,
                            purchaseInvoice.DocumentNumber,
                            purchaseInvoice.BranchId,
                            purchaseInvoice.WarehouseId,
                            purchaseInvoice.DepartmentId,
                            purchaseInvoice.VoucherDate,
                            purchaseInvoice.VendorOrCustomerId,
                            purchaseInvoice.CurrencyId,
                            purchaseInvoice.CurrencyEquivalent,
                            purchaseInvoice.Total,
                            purchaseInvoice.TotalItemsDiscount,
                            purchaseInvoice.SalesTaxes,
                            purchaseInvoice.TotalAfterTaxes,
                            purchaseInvoice.VoucherDiscountValue,
                            purchaseInvoice.VoucherDiscountPercentage,
                            purchaseInvoice.NetTotal,
                            purchaseInvoice.Paid,
                            purchaseInvoice.ValidityPeriod,
                            purchaseInvoice.DeliveryPeriod,
                            purchaseInvoice.CostCenterId,
                            purchaseInvoice.CurrentQuantity,
                            purchaseInvoice.DestinationWarehouseId,
                            purchaseInvoice.SystemPageId,
                            purchaseInvoice.SelectedId,
                            purchaseInvoice.TotalCostPrice,
                            purchaseInvoice.CommercialRevenueTaxAmount,
                            purchaseInvoice.TotalItemDirectExpenses,
                            purchaseInvoice.AddedPrecentageCost,
                            purchaseInvoice.IsDelivered,
                            purchaseInvoice.IsAccepted,
                            purchaseInvoice.IsLinked,
                            purchaseInvoice.IsCompleted,
                            purchaseInvoice.IsPosted,
                            purchaseInvoice.UserId,
                            purchaseInvoice.IsActive,
                            purchaseInvoice.IsDeleted,
                            purchaseInvoice.AutoCreated,
                            purchaseInvoice.Notes,
                            purchaseInvoice.Image,
                            purchaseInvoice.UpdatedId,
                            PurchaseInvoiceDetails,
                            InvoicePaymentMethods,
                            DirectExpenses,
                            SerialNumbersXML,
                            alterPrices,
                            AlteredPricesXML,
                            distributeItemsOnWarehouses,
                            purchaseInvoicePatchDetails,
                            purchaseInvoice.DueDate,
                            purchaseInvoice.VendorInvoiceNumber
                        );
                    }
                    catch (Exception ex)
                    {
                        var msg = ex.ToString();
                        var inner = ex.InnerException != null ? ex.InnerException.ToString() : "";
                        throw new Exception("PurchaseInvoice_Update failed.\r\n" + msg + "\r\nINNER:\r\n" + inner, ex);
                    }

                    //db.PurchaseInvoice_Update(purchaseInvoice.Id, purchaseInvoice.DocumentNumber, purchaseInvoice.BranchId, purchaseInvoice.WarehouseId, purchaseInvoice.DepartmentId, purchaseInvoice.VoucherDate, purchaseInvoice.VendorOrCustomerId, purchaseInvoice.CurrencyId, purchaseInvoice.CurrencyEquivalent, purchaseInvoice.Total, purchaseInvoice.TotalItemsDiscount, purchaseInvoice.SalesTaxes, purchaseInvoice.TotalAfterTaxes, purchaseInvoice.VoucherDiscountValue, purchaseInvoice.VoucherDiscountPercentage, purchaseInvoice.NetTotal, purchaseInvoice.Paid, purchaseInvoice.ValidityPeriod, purchaseInvoice.DeliveryPeriod, purchaseInvoice.CostCenterId, purchaseInvoice.CurrentQuantity, purchaseInvoice.DestinationWarehouseId, purchaseInvoice.SystemPageId, purchaseInvoice.SelectedId, purchaseInvoice.TotalCostPrice, purchaseInvoice.CommercialRevenueTaxAmount, purchaseInvoice.TotalItemDirectExpenses, purchaseInvoice.AddedPrecentageCost, purchaseInvoice.IsDelivered, purchaseInvoice.IsAccepted, purchaseInvoice.IsLinked, purchaseInvoice.IsCompleted, purchaseInvoice.IsPosted, purchaseInvoice.UserId, purchaseInvoice.IsActive, purchaseInvoice.IsDeleted, purchaseInvoice.AutoCreated, purchaseInvoice.Notes, purchaseInvoice.Image, purchaseInvoice.UpdatedId, PurchaseInvoiceDetails, InvoicePaymentMethods, DirectExpenses, SerialNumbersXML, alterPrices, AlteredPricesXML, distributeItemsOnWarehouses, purchaseInvoicePatchDetails, purchaseInvoice.DueDate, purchaseInvoice.VendorInvoiceNumber);
                    SyncVehicleStockForPurchaseInvoice(purchaseInvoice.Id);
                    Notification.GetNotification("PurchaseInvoice", "Edit", "AddEdit", id, null, " فواتير المشتريات");
                }
                else
                {
                    purchaseInvoice.IsActive = true;
                    MyXML.xPathName = "Details";
                    var PurchaseInvoiceDetails = MyXML.GetXML(purchaseInvoice.PurchaseInvoiceDetails);

                    MyXML.xPathName = "PaymentMethods";
                    var InvoicePaymentMethods = MyXML.GetXML(purchaseInvoice.PurchaseInvoicePaymentMethods);

                    MyXML.xPathName = "SerialNumbers";
                    var SerialNumbersXML = MyXML.GetXML(serialNumbers);
                    MyXML.xPathName = "DirectExpenses";
                    var DirectExpenses = MyXML.GetXML(purchaseInvoice.PurchaseInvoiceDirectExpenses);
                    MyXML.xPathName = "AlteredPrices";
                    var AlteredPricesXML = MyXML.GetXML(AlteredPrices);
                    var idResult = new ObjectParameter("Id", typeof(Int32));
                    // تحقق من أن كل الحسابات المستخدمة موجودة فعلاً
                    // بناء على XML

                 



                    db.PurchaseInvoice_Insert(idResult, purchaseInvoice.BranchId, purchaseInvoice.WarehouseId, purchaseInvoice.DepartmentId, purchaseInvoice.VoucherDate, purchaseInvoice.VendorOrCustomerId, purchaseInvoice.CurrencyId, purchaseInvoice.CurrencyEquivalent, purchaseInvoice.Total, purchaseInvoice.TotalItemsDiscount, purchaseInvoice.SalesTaxes, purchaseInvoice.TotalAfterTaxes, purchaseInvoice.VoucherDiscountValue, purchaseInvoice.VoucherDiscountPercentage, purchaseInvoice.NetTotal, purchaseInvoice.Paid, purchaseInvoice.ValidityPeriod, purchaseInvoice.DeliveryPeriod, purchaseInvoice.CostCenterId, purchaseInvoice.CurrentQuantity, purchaseInvoice.DestinationWarehouseId, purchaseInvoice.SystemPageId, purchaseInvoice.SelectedId, purchaseInvoice.TotalCostPrice, purchaseInvoice.TotalItemDirectExpenses, purchaseInvoice.CommercialRevenueTaxAmount, purchaseInvoice.AddedPrecentageCost, purchaseInvoice.IsDelivered, purchaseInvoice.IsAccepted, purchaseInvoice.IsLinked, purchaseInvoice.IsCompleted, false, purchaseInvoice.UserId, purchaseInvoice.IsActive, purchaseInvoice.IsDeleted, purchaseInvoice.AutoCreated, purchaseInvoice.Notes, purchaseInvoice.Image, purchaseInvoice.UpdatedId, PurchaseInvoiceDetails, InvoicePaymentMethods, DirectExpenses, SerialNumbersXML, alterPrices, AlteredPricesXML, distributeItemsOnWarehouses, purchaseInvoicePatchDetails, purchaseInvoice.DueDate, purchaseInvoice.VendorInvoiceNumber);

                    SyncVehicleStockForPurchaseInvoice((int)idResult.Value);

                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("PurchaseInvoice", "Add", "AddEdit", id, null, " فواتير المشتريات");

                    //int pageid = db.Get_PageId("PurchaseInvoice").SingleOrDefault().Value;
                    //var actionId = db.PageActions.FirstOrDefault(c => c.Action == "AddEdit" && c.EnName == "Add" && c.PageId == pageid).Id;
                    //int userid = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    //var UserName = User.Identity.Name;
                    //db.Sp_OccuredNotification(actionId, $"باضافة بيانات في شاشة فواتير المشتريات  {UserName}قام المستخدم  ");
                    QueryHelper.AddLog(new MyLog()
                    {
                        ArAction = id > 0 ? "تعديل  فاتورة شراء " : "اضافة   فاتورة شراء",
                        EnAction = "AddEdit",
                        ControllerName = "PurchaseInvoice",
                        UserName = User.Identity.Name,
                        UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                        LogDate = DateTime.Now,
                        RequestMethod = "POST",
                        SelectedItem = purchaseInvoice.Id > 0 ? purchaseInvoice.Id : db.PurchaseInvoices.Max(i => i.Id),
                        CodeOrDocNo = purchaseInvoice.DocumentNumber
                    });
                    ////////////////-----------------------------------------------------------------------
                    return Json(new { success = "true", id = idResult.Value });
                }
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = id > 0 ? "تعديل  فاتورة شراء " : "اضافة   فاتورة شراء",
                    EnAction = "AddEdit",
                    ControllerName = "PurchaseInvoice",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = purchaseInvoice.Id > 0 ? purchaseInvoice.Id : db.PurchaseInvoices.Max(i => i.Id),
                    CodeOrDocNo = purchaseInvoice.DocumentNumber
                });
                if (System.Web.Configuration.WebConfigurationManager.AppSettings["ProjectName"] == "Kimo")
                {
                    // HelperController.UpdateQuantitiesOnWebsite(purchaseInvoice.PurchaseInvoiceDetails.Select(s => new { Id = s.ItemId }));
                    if (alterPrices == true)
                        HelperController.UpdatePricesOnWebsite(purchaseInvoice.PurchaseInvoiceDetails.Select(s => new { Id = s.ItemId }));
                }
                return Json(new { success = "true", id = purchaseInvoice.Id });
            }
            //var errors = ModelState
            //        .Where(x => x.Value.Errors.Count > 0)
            //        .Select(x => new { x.Key, x.Value.Errors })
            //        .ToArray();

            var errors = ModelState
    .Where(x => x.Value.Errors.Count > 0)
    .Select(x => new
    {
        Field = x.Key,
        Errors = x.Value.Errors.Select(e => e.ErrorMessage).ToList()
    }).ToList();
            return Json(errors);
        }

        private void FillVehicleLookupData(string session)
        {
            ViewBag.CarTypesJson = JsonConvert.SerializeObject(db.CarTypes.Where(a => a.IsActive && !a.IsDeleted).Select(b => new
            {
                b.Id,
                ArName = session == "en" && b.EnName != null ? b.Code + " - " + b.EnName : b.Code + " - " + b.ArName
            }).ToList());
            ViewBag.CarModelsJson = JsonConvert.SerializeObject(db.CarModels.Where(a => a.IsActive && !a.IsDeleted).Select(b => new
            {
                b.Id,
                b.CarTypeId,
                ArName = session == "en" && b.EnName != null ? b.Code + " - " + b.EnName : b.Code + " - " + b.ArName
            }).ToList());
            ViewBag.CarColorsJson = JsonConvert.SerializeObject(db.CarColors.Where(a => a.IsActive && !a.IsDeleted).Select(b => new
            {
                b.Id,
                ArName = session == "en" && b.EnName != null ? b.Code + " - " + b.EnName : b.Code + " - " + b.ArName
            }).ToList());
        }
        //private string ValidateVehicleChassisNumbers(ICollection<PurchaseInvoiceDetail> details, int invoiceId)
        //{
        //    if (details == null) return null;
        //    var normalizedInInvoice = new HashSet<string>();
        //    foreach (var detail in details)
        //    {
        //        var hasVehicleData = detail.CarTypeId.HasValue || detail.CarModelId.HasValue || detail.CarColorId.HasValue || !string.IsNullOrWhiteSpace(detail.EngineNo) || detail.ManufacturingYear.HasValue || !string.IsNullOrWhiteSpace(detail.PlateNo);
        //        var chassis = (detail.ChassisNo ?? "").Trim();
        //        if (hasVehicleData && string.IsNullOrWhiteSpace(chassis))
        //            return "رقم الشاسيه مطلوب عند إدخال بيانات سيارة.";
        //        if (!string.IsNullOrWhiteSpace(chassis))
        //        {
        //            var normalized = Regex.Replace(chassis.ToLower(), "\\s+", "");
        //            if (normalizedInInvoice.Contains(normalized))
        //                return "رقم الشاسيه مكرر داخل نفس الفاتورة.";
        //            normalizedInInvoice.Add(normalized);
        //            var existsInPurchase = db.PurchaseInvoiceDetails.Where(x => !x.IsDeleted && x.MainDocId != invoiceId && x.ChassisNo != null)
        //                .Select(x => x.ChassisNo).ToList().Any(x => Regex.Replace(x.ToLower(), "\\s+", "") == normalized);
        //            if (existsInPurchase)
        //                return "رقم الشاسيه مستخدم مسبقاً في فاتورة شراء أخرى.";
        //            var existsInSales = db.SalesInvoiceDetails.Where(x => !x.IsDeleted && x.MainDocId != invoiceId && x.ChassisNo != null)
        //                .Select(x => x.ChassisNo).ToList().Any(x => Regex.Replace(x.ToLower(), "\\s+", "") == normalized);
        //            if (existsInSales)
        //                return "رقم الشاسيه مستخدم مسبقاً في فاتورة بيع.";
        //        }
        //    }
        //    return null;
        //}

        private string ValidateVehicleChassisNumbers(ICollection<PurchaseInvoiceDetail> details, int invoiceId)
        {
            if (details == null || !details.Any())
                return null;

            Func<string, string> normalize = s =>
                string.IsNullOrWhiteSpace(s)
                    ? string.Empty
                    : Regex.Replace(s.Trim().ToLower(), "\\s+", "");

            // الشواسي الأصلية الموجودة حاليًا في نفس فاتورة الشراء قبل الحفظ
            var existingInvoiceDetails = db.PurchaseInvoiceDetails
                .Where(x => !x.IsDeleted && x.MainDocId == invoiceId)
                .Select(x => new
                {
                    x.Id,
                    x.ChassisNo
                })
                .ToList();

            var existingChassisInCurrentInvoice = existingInvoiceDetails
                .Where(x => !string.IsNullOrWhiteSpace(x.ChassisNo))
                .Select(x => normalize(x.ChassisNo))
                .ToHashSet();

            // كل الشواسي الموجودة في فواتير شراء أخرى
            var purchaseChassisRows = db.PurchaseInvoiceDetails
                .Where(x => !x.IsDeleted && x.MainDocId != invoiceId && x.ChassisNo != null)
                .Select(x => new
                {
                    x.Id,
                    x.ChassisNo
                })
                .ToList();

            var purchaseChassisSet = purchaseChassisRows
                .Where(x => !string.IsNullOrWhiteSpace(x.ChassisNo))
                .Select(x => normalize(x.ChassisNo))
                .ToHashSet();

            // كل الشواسي الموجودة في فواتير البيع
            var salesChassisSet = db.SalesInvoiceDetails
                .Where(x => !x.IsDeleted && x.ChassisNo != null)
                .Select(x => x.ChassisNo)
                .ToList()
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => normalize(x))
                .ToHashSet();

            var normalizedInInvoice = new HashSet<string>();

            foreach (var detail in details)
            {
                if (detail == null)
                    continue;

                var hasVehicleData =
                    detail.CarTypeId.HasValue ||
                    detail.CarModelId.HasValue ||
                    detail.CarColorId.HasValue ||
                    !string.IsNullOrWhiteSpace(detail.EngineNo) ||
                    detail.ManufacturingYear.HasValue ||
                    !string.IsNullOrWhiteSpace(detail.PlateNo);

                var chassis = (detail.ChassisNo ?? "").Trim();

                if (hasVehicleData && string.IsNullOrWhiteSpace(chassis))
                    return "رقم الشاسيه مطلوب عند إدخال بيانات سيارة.";

                if (string.IsNullOrWhiteSpace(chassis))
                    continue;

                var normalized = normalize(chassis);

                // تكرار داخل نفس الفاتورة الحالية
                if (!normalizedInInvoice.Add(normalized))
                    return "رقم الشاسيه مكرر داخل نفس الفاتورة.";

                // مستخدم في فاتورة شراء أخرى
                if (purchaseChassisSet.Contains(normalized))
                    return "رقم الشاسيه مستخدم مسبقاً في فاتورة شراء أخرى.";

                // لو الشاسيه موجود في البيع:
                // نمنعه فقط إذا كان شاسيه جديدًا على الفاتورة الحالية
                // أما لو كان موجود أصلًا في نفس فاتورة الشراء ثم تم بيعه بعد ذلك،
                // فيُسمح بالتعديل على الفاتورة بدون تغيير هذا الشاسيه.
                bool existedOriginallyInCurrentInvoice = existingChassisInCurrentInvoice.Contains(normalized);

                if (salesChassisSet.Contains(normalized) && !existedOriginallyInCurrentInvoice)
                    return "رقم الشاسيه مستخدم مسبقاً في فاتورة بيع.";
            }

            return null;
        }

        private bool VehicleStockTableExists()
        {
            return db.Database.SqlQuery<int>("SELECT CASE WHEN OBJECT_ID('dbo.VehicleStock','U') IS NULL THEN 0 ELSE 1 END").FirstOrDefault() == 1;
        }

        private void SyncVehicleStockForPurchaseInvoice(int purchaseInvoiceId)
        {
            if (!VehicleStockTableExists()) return;
            var invoice = db.PurchaseInvoices.FirstOrDefault(x => x.Id == purchaseInvoiceId);
            if (invoice == null) return;
            var details = db.PurchaseInvoiceDetails.Where(x => x.MainDocId == purchaseInvoiceId && !x.IsDeleted && x.ChassisNo != null && x.ChassisNo.Trim() != "").ToList();
            foreach (var detail in details)
            {
                var sql = @"DECLARE @VehicleId INT;
SELECT TOP 1 @VehicleId = Id FROM dbo.VehicleStock WHERE IsDeleted = 0 AND LOWER(LTRIM(RTRIM(ChassisNo))) = LOWER(LTRIM(RTRIM(@ChassisNo)));
IF @VehicleId IS NULL
BEGIN
    INSERT INTO dbo.VehicleStock(ItemId, ChassisNo, EngineNo, CarTypeId, CarModelId, CarColorId, ManufacturingYear, PlateNo, VehicleNotes,
        PurchaseInvoiceId, PurchaseInvoiceDetailId, PurchaseDate, PurchaseCost, WarehouseId, BranchId, VehicleStatusId, IsDeleted, UserId, CreatedDate, UpdatedDate)
    VALUES(@ItemId, LTRIM(RTRIM(@ChassisNo)), @EngineNo, @CarTypeId, @CarModelId, @CarColorId, @ManufacturingYear, @PlateNo, @VehicleNotes,
        @PurchaseInvoiceId, @PurchaseInvoiceDetailId, @PurchaseDate, @PurchaseCost, @WarehouseId, @BranchId, 1, 0, @UserId, GETDATE(), GETDATE());
END
ELSE
BEGIN
    UPDATE dbo.VehicleStock SET
        ItemId=@ItemId, EngineNo=@EngineNo, CarTypeId=@CarTypeId, CarModelId=@CarModelId, CarColorId=@CarColorId, ManufacturingYear=@ManufacturingYear,
        PlateNo=@PlateNo, VehicleNotes=@VehicleNotes, PurchaseInvoiceId=@PurchaseInvoiceId, PurchaseInvoiceDetailId=@PurchaseInvoiceDetailId,
        PurchaseDate=@PurchaseDate, PurchaseCost=@PurchaseCost, WarehouseId=@WarehouseId, BranchId=@BranchId, VehicleStatusId=1, IsDeleted=0,
        SalesInvoiceId=NULL, SalesInvoiceDetailId=NULL, SalesDate=NULL, SalePrice=NULL, UpdatedDate=GETDATE()
    WHERE Id=@VehicleId;
END";
                db.Database.ExecuteSqlCommand(sql,
                    new System.Data.SqlClient.SqlParameter("@ChassisNo", (object)(detail.ChassisNo ?? "").Trim()),
                    new System.Data.SqlClient.SqlParameter("@ItemId", detail.ItemId),
                    new System.Data.SqlClient.SqlParameter("@EngineNo", (object)detail.EngineNo ?? DBNull.Value),
                    new System.Data.SqlClient.SqlParameter("@CarTypeId", (object)detail.CarTypeId ?? DBNull.Value),
                    new System.Data.SqlClient.SqlParameter("@CarModelId", (object)detail.CarModelId ?? DBNull.Value),
                    new System.Data.SqlClient.SqlParameter("@CarColorId", (object)detail.CarColorId ?? DBNull.Value),
                    new System.Data.SqlClient.SqlParameter("@ManufacturingYear", (object)detail.ManufacturingYear ?? DBNull.Value),
                    new System.Data.SqlClient.SqlParameter("@PlateNo", (object)detail.PlateNo ?? DBNull.Value),
                    new System.Data.SqlClient.SqlParameter("@VehicleNotes", (object)detail.VehicleNotes ?? DBNull.Value),
                    new System.Data.SqlClient.SqlParameter("@PurchaseInvoiceId", purchaseInvoiceId),
                    new System.Data.SqlClient.SqlParameter("@PurchaseInvoiceDetailId", detail.Id),
                    new System.Data.SqlClient.SqlParameter("@PurchaseDate", (object)invoice.VoucherDate ?? DBNull.Value),
                    new System.Data.SqlClient.SqlParameter("@PurchaseCost", (object)detail.CostPrice ?? (object)detail.Price),
                    new System.Data.SqlClient.SqlParameter("@WarehouseId", (object)invoice.WarehouseId ?? DBNull.Value),
                    new System.Data.SqlClient.SqlParameter("@BranchId", (object)invoice.BranchId ?? DBNull.Value),
                    new System.Data.SqlClient.SqlParameter("@UserId", (object)invoice.UserId ?? DBNull.Value));
            }
        }

        [SkipERPAuthorize]
        public JsonResult GetSerialNumber(int? invoiceId, int? itemId)
        {

            var SerialNumbers = JsonConvert.SerializeObject(db.PurchaseSaleSerialNumbers.Where(s => s.SelectedId == invoiceId && s.ItemId == itemId && s.PageSourceId == db.SystemPages.FirstOrDefault(d => d.TableName == "PurchaseInvoice").Id).Select(d => new { d.ItemId, d.SerialNumber }));


            return Json(SerialNumbers, JsonRequestBehavior.AllowGet);
        }

        // GET
        public ActionResult AddEditSerialNumber(int? id, int itemId)
        {
            var session = Session["lang"] != null ? Session["lang"].ToString() : "ar";
            SystemSetting sysObj = db.SystemSettings.FirstOrDefault();
            ViewData["ShowSerialNumbers"] = true;


            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            PurchaseInvoice purchaseInvoice = db.PurchaseInvoices.Find(id);
            if (purchaseInvoice == null)
            {
                return HttpNotFound();
            }
            if (itemId == 0)
            {
                var x = db.PurchaseInvoiceDetails.Where(p => p.MainDocId == id).FirstOrDefault().ItemId;
                itemId = int.Parse(x.ToString());
            }
            ViewBag.GeneralItemId = itemId;
            int sysPageId = QueryHelper.SourcePageId("PurchaseInvoice");
            ViewBag.VendorId = new SelectList(db.Vendors.Where(a => (a.IsActive == true && a.IsDeleted == false)).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName + " - " + b.EnName
            }), "Id", "ArName", purchaseInvoice.VendorOrCustomerId);
            ViewBag.VendorOrCustomerId = new SelectList(db.Vendors.Where(a => (a.IsActive == true && a.IsDeleted == false)).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName + " - " + b.EnName
            }), "Id", "ArName", purchaseInvoice.VendorOrCustomerId);

            ViewBag.BranchId = new SelectList(db.Branches.Where(a => (a.IsActive == true && a.IsDeleted == false)).Select(b => new
            {
                Id = b.Id,
                ArName = session.ToString() == "en" && b.EnName != null ? b.Code + " - " + b.EnName : b.Code + " - " + b.ArName
            }), "Id", "ArName", purchaseInvoice.BranchId);
            if (userId == 1)
            {
                ViewBag.DepartmentId = new SelectList(db.Departments.Where(d => d.IsActive == true && d.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = session.ToString() == "en" && b.EnName != null ? b.Code + " - " + b.EnName : b.Code + " - " + b.ArName
                }), "Id", "ArName", purchaseInvoice.DepartmentId);
                ViewBag.WarehouseId = new SelectList(db.Warehouses.Where(b => b.IsActive == true && b.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = session.ToString() == "en" && b.EnName != null ? b.Code + " - " + b.EnName : b.Code + " - " + b.ArName
                }), "Id", "ArName", purchaseInvoice.WarehouseId);

            }
            else
            {
                ViewBag.DepartmentId = new SelectList(db.UserDepartments.Where(d => d.UserId == userId).Select(b => new
                {
                    Id = b.DepartmentId,
                    ArName = session.ToString() == "en" && b.Department.EnName != null ? b.Department.Code + " - " + b.Department.EnName : b.Department.Code + " - " + b.Department.ArName
                }), "Id", "ArName", purchaseInvoice.DepartmentId);
                ViewBag.WarehouseId = new SelectList(db.UserWareHouses.Where(b => b.UserId == userId).Select(b => new
                {
                    Id = b.WareHouseId,
                    ArName = session.ToString() == "en" && b.Warehouse.EnName != null ? b.Warehouse.Code + " - " + b.Warehouse.EnName : b.Warehouse.Code + " - " + b.Warehouse.ArName
                }), "Id", "ArName", purchaseInvoice.WarehouseId);

            }
            ViewBag.SerialNumbers = JsonConvert.SerializeObject(db.PurchaseSaleSerialNumbers.Where(s => s.SelectedId == id && s.ItemId == itemId && s.PageSourceId == db.SystemPages.FirstOrDefault(d => d.TableName == "PurchaseInvoice").Id).Select(d => new { d.ItemId, d.SerialNumber }));



            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح سيريال فاتورة الشراء",
                EnAction = "AddEdit",
                ControllerName = "AddEditSerialNumber",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = purchaseInvoice.Id,
                CodeOrDocNo = purchaseInvoice.DocumentNumber
            });

            return View(purchaseInvoice);
        }

        // GET
        [SkipERPAuthorize]
        public ActionResult PurchaseInvoiceBarcode(int? id, int itemId)
        {
            var session = Session["lang"] != null ? Session["lang"].ToString() : "ar";
            SystemSetting sysObj = db.SystemSettings.FirstOrDefault();

            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            PurchaseInvoice purchaseInvoice = db.PurchaseInvoices.Find(id);
            if (purchaseInvoice == null)
            {
                return HttpNotFound();
            }

            var invoiceItems = db.PurchaseInvoiceDetails.Where(p => p.MainDocId == id).ToList();

            ViewBag.InvoiceItems = invoiceItems;

            ViewBag.GeneralItemId = itemId;

            ViewBag.VendorId = new SelectList(db.Vendors.Where(a => (a.IsActive == true && a.IsDeleted == false)).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName + " - " + b.EnName
            }), "Id", "ArName", purchaseInvoice.VendorOrCustomerId);
            ViewBag.VendorOrCustomerId = new SelectList(db.Vendors.Where(a => (a.IsActive == true && a.IsDeleted == false)).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName + " - " + b.EnName
            }), "Id", "ArName", purchaseInvoice.VendorOrCustomerId);

            ViewBag.BranchId = new SelectList(db.Branches.Where(a => (a.IsActive == true && a.IsDeleted == false)).Select(b => new
            {
                Id = b.Id,
                ArName = session.ToString() == "en" && b.EnName != null ? b.Code + " - " + b.EnName : b.Code + " - " + b.ArName
            }), "Id", "ArName", purchaseInvoice.BranchId);
            if (userId == 1)
            {
                ViewBag.DepartmentId = new SelectList(db.Departments.Where(d => d.IsActive == true && d.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = session.ToString() == "en" && b.EnName != null ? b.Code + " - " + b.EnName : b.Code + " - " + b.ArName
                }), "Id", "ArName", purchaseInvoice.DepartmentId);
                ViewBag.WarehouseId = new SelectList(db.Warehouses.Where(b => b.IsActive == true && b.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = session.ToString() == "en" && b.EnName != null ? b.Code + " - " + b.EnName : b.Code + " - " + b.ArName
                }), "Id", "ArName", purchaseInvoice.WarehouseId);

            }
            else
            {
                ViewBag.DepartmentId = new SelectList(db.UserDepartments.Where(d => d.UserId == userId).Select(b => new
                {
                    Id = b.DepartmentId,
                    ArName = session.ToString() == "en" && b.Department.EnName != null ? b.Department.Code + " - " + b.Department.EnName : b.Department.Code + " - " + b.Department.ArName
                }), "Id", "ArName", purchaseInvoice.DepartmentId);
                ViewBag.WarehouseId = new SelectList(db.UserWareHouses.Where(b => b.UserId == userId).Select(b => new
                {
                    Id = b.WareHouseId,
                    ArName = session.ToString() == "en" && b.Warehouse.EnName != null ? b.Warehouse.Code + " - " + b.Warehouse.EnName : b.Warehouse.Code + " - " + b.Warehouse.ArName
                }), "Id", "ArName", purchaseInvoice.WarehouseId);
            }

            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح باركود فاتورة الشراء",
                EnAction = "AddEdit",
                ControllerName = "PurchaseInvoiceBarcode",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = purchaseInvoice.Id,
                CodeOrDocNo = purchaseInvoice.DocumentNumber
            });

            return View(purchaseInvoice);
        }

        [HttpPost]
        //[ValidateAntiForgeryToken]
        public JsonResult AddEditSerialNumber(int purchaseInvoiceId, int itemId, ICollection<PurchaseSaleSerialNumber> serialNumbers)
        {
            if (ModelState.IsValid)
            {
                if (purchaseInvoiceId == 0)
                {
                    return Json("false");
                }
                if (purchaseInvoiceId > 0)
                {
                    if (db.PurchaseInvoices.Find(purchaseInvoiceId).IsPosted == true)
                    {
                        return Json("false");
                    }
                    MyXML.xPathName = "SerialNumbers";
                    var SerialNumbersXML = MyXML.GetXML(serialNumbers);
                    db.AddEditSerialNumber(purchaseInvoiceId, itemId, QueryHelper.SourcePageId("PurchaseInvoice"), SerialNumbersXML);

                    Notification.GetNotification("PurchaseInvoice", "AddEditSerialNumber", "AddEditSerialNumber", purchaseInvoiceId, null, "  سيريال فواتير المشتريات");
                }

                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = purchaseInvoiceId > 0 ? "تعديل سيريال فاتورة شراء " : "تعديل سيريال فاتورة شراء",
                    EnAction = "AddEditSerialNumber",
                    ControllerName = "PurchaseInvoice",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = purchaseInvoiceId,
                    CodeOrDocNo = db.PurchaseInvoices.Where(p => p.Id == purchaseInvoiceId).FirstOrDefault().DocumentNumber
                });

                return Json(new { success = "true", id = purchaseInvoiceId });
            }
            var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .Select(x => new { x.Key, x.Value.Errors })
                    .ToArray();

            return Json(errors);
        }

        // POST: PurchaseInvoice/Delete/5
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
            //-- Check if this Purchase Invoice Exist In Cash Issue Voucher
            var check = db.CheckPurchaseInvoiceExistInCashIssueVoucher(id).FirstOrDefault();
            if (check > 0)
            {
                return Content("False");
            }
            else
            {
                PurchaseInvoice purchaseInvoice = db.PurchaseInvoices.Find(id);
                if (purchaseInvoice.IsPosted == true)
                {
                    return Content("false");
                }
                var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                db.PurchaseInvoice_Delete(id, userId);
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = " حذف فاتورة شراء",
                    EnAction = "AddEdit",
                    ControllerName = "PurchaseInvoice",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    CodeOrDocNo = purchaseInvoice.DocumentNumber
                });

                ////-------------------- Notification-------------------------////
                Notification.GetNotification("PurchaseInvoice", "Delete", "Delete", id, null, "فواتير المشتريات");

                return Content("true");
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
            var lastObj = db.PurchaseInvoices.Where(a => a.IsActive == true && a.IsDeleted == false && a.DepartmentId == id).OrderByDescending(a => a.Id).FirstOrDefault();
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
                    lastObj = db.PurchaseInvoices.Where(a => a.IsActive == true && a.IsDeleted == false && a.DepartmentId == id && a.VoucherDate.Month == VoucherDate.Value.Month && a.VoucherDate.Year == VoucherDate.Value.Year).OrderByDescending(a => a.Id).FirstOrDefault();
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
                    lastObj = db.PurchaseInvoices.Where(a => a.IsActive == true && a.IsDeleted == false && a.DepartmentId == id && a.VoucherDate.Year == VoucherDate.Value.Year).OrderByDescending(a => a.Id).FirstOrDefault();

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
            //var docNo = QueryHelper.DocLastNum(id, "PurchaseInvoice");
            //double i = (docNo) + 1;
            //return Json(i, JsonRequestBehavior.AllowGet);
        }
        [SkipERPAuthorize]
        public JsonResult GetItemPrices(int? itemId)
        {
            var lastPrice = db.PurchaseInvoiceDetails.Where(p => p.ItemId == itemId && p.IsDeleted == false && p.PurchaseInvoice.IsDeleted == false).OrderByDescending(p => p.Id).Select(p => p.Price).FirstOrDefault();
            return Json(new { LastPrice = lastPrice }, JsonRequestBehavior.AllowGet);
        }

        [SkipERPAuthorize]
        public JsonResult SetBankAccounts(int bankId)
        {
            var BankAccountsList = db.GetBankAccountByBankId(bankId).ToList();
            return Json(BankAccountsList, JsonRequestBehavior.AllowGet);
        }

        [SkipERPAuthorize]
        public JsonResult ChangeCustomerGroupItemPrices(ICollection<ItemPrice> itemPrices)
        {
            try
            {
                foreach (var item in itemPrices)
                {
                    var itemPrice = db.ItemPrices.Find(item.Id);
                    itemPrice.Price = item.Price;
                    db.Entry(itemPrice).State = EntityState.Modified;
                }
                db.SaveChanges();
                return Json(new { success = "true" });
            }
            catch (Exception)
            {
                var errors = ModelState
                        .Where(x => x.Value.Errors.Count > 0)
                        .Select(x => new { x.Key, x.Value.Errors })
                        .ToArray();

                return Json(errors);
            }
        }
        [SkipERPAuthorize]
        public async Task<ActionResult> SalesInvoicesByDepartment(int id)
        {
            return PartialView(await db.SalesInvoices.Where(x => x.IsDeleted == false && x.IsLinked != true && x.DepartmentId == id).OrderByDescending(x => x.Id).ToListAsync());
        }
        [SkipERPAuthorize]
        public async Task<ActionResult> SalesInvoiceDetails(int id)
        {
            return PartialView(await db.SalesInvoiceDetails.Where(x => x.MainDocId == id).ToListAsync());
        }

        [SkipERPAuthorize]
        public ActionResult Barcode(string barcode)
        {
            ViewBag.Barcode = barcode;
            return View();
        }
        [SkipERPAuthorize]
        public JsonResult ItemsByDepId(int? departmentId, bool? isDefault)
        {
            bool calculateQty = false;
            if (System.Web.Configuration.WebConfigurationManager.AppSettings["ProjectName"] == "Kimo")
            {
                isDefault = true;
                calculateQty = true;
            }
            if (System.Web.Configuration.WebConfigurationManager.AppSettings["ProjectName"] == "Genoise")
            {
                departmentId = null;
            }
            var DontShowEndProductInPurchaseInvoice = db.SystemSettings.FirstOrDefault().DontShowEndProductInPurchaseInvoice;

            var items = DontShowEndProductInPurchaseInvoice == true ? db.Item_AllByDepIdAndName_PriceAndQuantity(departmentId, null, "", isDefault, calculateQty).Where(a => a.ItemTypeId != 2 && a.ItemTypeId != 3) : db.Item_AllByDepIdAndName_PriceAndQuantity(departmentId, null, "", isDefault, calculateQty);
            return Json(items, JsonRequestBehavior.AllowGet);
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
