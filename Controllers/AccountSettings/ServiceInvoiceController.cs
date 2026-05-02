using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Web;
using System.Web.Mvc;
using MyERP.Models;
using System.Security.Claims;
using System.Data.Entity.Core.Objects;
using MyERP.Models.CustomModels;
using MyERP.Repository;

namespace MyERP.Controllers.AccountSettings
{
    public class ServiceInvoiceController : Controller
    {
        private readonly MySoftERPEntity db = new MySoftERPEntity();
        // GET: ServiceInvoice
        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            SystemSetting systemSetting =  db.SystemSettings.Any() ? db.SystemSettings.FirstOrDefault() : new SystemSetting();
            ViewBag.InabilityToEditSalesAndPurchaseInvoicesAndReturns = systemSetting.InabilityToEditSalesAndPurchaseInvoicesAndReturns;

            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة فاتورة خدمات",
                EnAction = "Index",
                ControllerName = "ServiceInvoice",
                UserName = User.Identity.Name,
                UserId = userId,
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            ////-------------------- Notification-------------------------////
            Notification.GetNotification("ServiceInvoice", "View", "Index", null, null, "فاتورة خدمات");

            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;
            var systemPageid = db.SystemPages.Where(a => a.IsActive == true && a.IsDeleted == false && a.ControllerName == "ServiceInvoice").FirstOrDefault().Id;
            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;
            IQueryable<Models.ServiceInvoice> serviceInvoices;
            if (string.IsNullOrEmpty(searchWord))
            {
                serviceInvoices = db.ServiceInvoices.Where(c => c.IsDeleted == false && c.IsActive == true && c.SystemPageId == systemPageid).OrderByDescending(a => a.DocumentNumber).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.ServiceInvoices.Where(c => c.IsDeleted == false && c.IsActive == true && c.SystemPageId == systemPageid).Count();
            }
            else
            {
                serviceInvoices = db.ServiceInvoices.Where(s => s.IsDeleted == false && s.IsActive == true && s.SystemPageId == systemPageid && (s.Department.ArName.Contains(searchWord) || s.Department.EnName.Contains(searchWord) || s.DocumentNumber.Contains(searchWord))).OrderByDescending(a => a.DocumentNumber).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.ServiceInvoices.Where(s => s.IsDeleted == false && s.IsActive == true && s.SystemPageId == systemPageid && (s.Department.ArName.Contains(searchWord) || s.Department.EnName.Contains(searchWord) || s.DocumentNumber.Contains(searchWord))).Count();
            }
            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            return View(serviceInvoices.ToList());
        }

        public ActionResult AddEdit(int? id)
        {
            int userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            SystemSetting systemSetting = db.SystemSettings.Any() ? db.SystemSettings.FirstOrDefault() : new SystemSetting();
            ViewBag.ApplyTaxes = systemSetting.ApplyTaxesOnSalesIvoiceAuto;
            ViewBag.UseCostCenter = systemSetting.UseCostCenter;
            ViewBag.PrintReceiptInsteadOfSalesInvoice = systemSetting.PrintReceiptInsteadOfSalesInvoice;
            DepartmentRepository departmentRepository = new DepartmentRepository(db);
            var departments = departmentRepository.UserDepartments(1).ToList();//show all deprartments so that user can select factory department
            List<PaymentMethod> paymentMethods = db.PaymentMethods.Where(p => p.IsActive == true && p.IsDeleted == false).ToList();
            ViewBag.PaymentMethods = paymentMethods;
            CashboxReposistory cashboxReposistory = new CashboxReposistory(db);
            var cashBoxSelectList = new SelectList(cashboxReposistory.UserCashboxes(userId, systemSetting.DefaultDepartmentId).ToList(), "Id", "ArName", systemSetting.DefaultCashBoxId);
            ViewBag.CashBoxId = cashBoxSelectList;
            var banks = db.Banks.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }).ToList();
            if (id == null)
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


                ViewBag.DepartmentId = new SelectList(departmentRepository.UserDepartments(userId), "Id", "ArName", systemSetting.DefaultDepartmentId);
                ViewBag.CustomerId = new SelectList(db.Customers.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                ViewBag.CustomerRepId = new SelectList(db.CustomerReps.Where(a => (a.IsActive == true)).OrderBy(a => a.IsDefault).Select(b => new
                {
                    b.Employee.Id,
                    ArName = b.Employee.Code + " - " + b.Employee.ArName
                }), "Id", "ArName");
                ViewBag.CostCenterId = new SelectList(db.CostCenters.Where(a => a.IsActive == true && a.IsDeleted == false && a.TypeId == 2).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                ViewBag.VoucherDate = cTime.ToString("yyyy-MM-ddTHH:mm");
                ViewBag.DueDate = cTime.ToString("yyyy-MM-ddTHH:mm");
                return View();
            }
            Models.ServiceInvoice serviceInvoice = db.ServiceInvoices.Find(id);
            if (serviceInvoice == null)
            {
                return HttpNotFound();
            }
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "اضافة او تعديل فاتورة خدمات ",
                EnAction = "AddEdit",
                ControllerName = "ServiceInvoice",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = id
            });
            ViewBag.Next = QueryHelper.Next((int)id, "ServiceInvoice");
            ViewBag.Previous = QueryHelper.Previous((int)id, "ServiceInvoice");
            ViewBag.Last = QueryHelper.GetLast("ServiceInvoice");
            ViewBag.First = QueryHelper.GetFirst("ServiceInvoice");

            ViewBag.DepartmentId = new SelectList(departmentRepository.UserDepartments(userId), "Id", "ArName", serviceInvoice.DepartmentId);
            ViewBag.CustomerId = new SelectList(db.Customers.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", serviceInvoice.CustomerId);
            ViewBag.CustomerRepId = new SelectList(db.CustomerReps.Where(a => (a.IsActive == true)).OrderBy(a => a.IsDefault).Select(b => new
            {
                b.Employee.Id,
                ArName = b.Employee.Code + " - " + b.Employee.ArName
            }), "Id", "ArName", serviceInvoice.CustomerRepId);
            ViewBag.CostCenterId = new SelectList(db.CostCenters.Where(a => a.IsActive == true && a.IsDeleted == false && a.TypeId == 2).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", serviceInvoice.CostCenterId);

            ViewBag.VoucherDate = serviceInvoice.VoucherDate.ToString("yyyy-MM-ddTHH:mm");
            ViewBag.DueDate = serviceInvoice.DueDate.Value.ToString("yyyy-MM-ddTHH:mm");
            var cashBoxes = cashboxReposistory.UserCashboxes(userId, serviceInvoice.DepartmentId).ToList();

            foreach (var method in serviceInvoice.ServiceInvoicePaymentMethods)
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

            int sysPageId = QueryHelper.SourcePageId("ServiceInvoice");
            JournalEntry journal = db.JournalEntries.FirstOrDefault(j => j.SourceId == id && j.SourcePageId == sysPageId);
            ViewBag.Journal = journal;
            ViewBag.Journal.DocumentNumber = journal != null ? journal.DocumentNumber.Replace("-", "") : "";
            return View(serviceInvoice);
        }

        [HttpPost]
        public ActionResult AddEdit(Models.ServiceInvoice serviceInvoice)
        {
            var systemSetting = db.SystemSettings.FirstOrDefault();
            int userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            serviceInvoice.UserId = userId;
            serviceInvoice.SystemPageId = db.SystemPages.Where(a => a.IsActive == true && a.IsDeleted == false && a.ControllerName == "ServiceInvoice").FirstOrDefault().Id;
            if (ModelState.IsValid)
            {
                var id = serviceInvoice.Id;
                serviceInvoice.IsDeleted = false;
                var WarehouseId = db.Warehouses.Where(a => a.IsActive == true && a.IsDeleted == false && a.DepartmentId == serviceInvoice.DepartmentId).FirstOrDefault().Id;
                var VendorOrCustomerId = db.Customers.Where(a => a.IsActive == true && a.IsDeleted == false).FirstOrDefault().Id;
                var CustomerTypeId = db.CustomerTypes.Where(a => a.IsActive == true && a.IsDeleted == false).FirstOrDefault().Id;

                if (serviceInvoice.Id > 0)
                {
                    var InabilityToEditSalesAndPurchaseInvoicesAndReturns = systemSetting.InabilityToEditSalesAndPurchaseInvoicesAndReturns;
                    if (InabilityToEditSalesAndPurchaseInvoicesAndReturns == true)
                    {
                        return Json(new { success = "Cannot Be Edited" });
                    }

                    MyXML.xPathName = "Details";
                    var ServiceInvoiceDetails = MyXML.GetXML(serviceInvoice.ServiceInvoiceDetails);
                    MyXML.xPathName = "PaymentMethods";
                    var ServiceInvoicePaymentMethods = MyXML.GetXML(serviceInvoice.ServiceInvoicePaymentMethods);
                    db.ServiceInvoice_Update(serviceInvoice.Id,
                        serviceInvoice.DocumentNumber, 
                        serviceInvoice.DepartmentId,
                        serviceInvoice.VoucherDate,
                        serviceInvoice.CustomerId,
                        serviceInvoice.CurrencyId,
                        serviceInvoice.CurrencyEquivalent,
                        serviceInvoice.Total,
                        serviceInvoice.SalesTaxes,
                        serviceInvoice.TotalAfterTaxes,
                        serviceInvoice.VoucherDiscountValue,
                        serviceInvoice.VoucherDiscountPercentage,
                        serviceInvoice.NetTotal,
                        serviceInvoice.CostCenterId,
                        serviceInvoice.TotalItemDirectExpenses,
                        serviceInvoice.CommercialRevenueTaxAmount,
                        serviceInvoice.UserId,
                        serviceInvoice.IsDeleted,
                        serviceInvoice.Notes,
                        serviceInvoice.Image,
                        serviceInvoice.PaymentType,
                        serviceInvoice.CustomerRepId,
                        serviceInvoice.DueDate,
                        ServiceInvoiceDetails,
                        ServiceInvoicePaymentMethods);
                }
                else
                {
                    MyXML.xPathName = "Details";
                    var ServiceInvoiceDetails = MyXML.GetXML(serviceInvoice.ServiceInvoiceDetails);
                    MyXML.xPathName = "PaymentMethods";
                    var ServiceInvoicePaymentMethods = MyXML.GetXML(serviceInvoice.ServiceInvoicePaymentMethods);
                    var idResult = new ObjectParameter("Id", typeof(Int32));
                    db.ServiceInvoice_Insert(idResult, 
                        serviceInvoice.DepartmentId,
                        serviceInvoice.VoucherDate,
                        serviceInvoice.CustomerId,
                        serviceInvoice.CurrencyId,
                        serviceInvoice.CurrencyEquivalent,
                        serviceInvoice.Total,
                        serviceInvoice.SalesTaxes,
                        serviceInvoice.TotalAfterTaxes,
                        serviceInvoice.VoucherDiscountValue,
                        serviceInvoice.VoucherDiscountPercentage,
                        serviceInvoice.NetTotal,
                        serviceInvoice.CostCenterId,
                        serviceInvoice.TotalItemDirectExpenses,
                        serviceInvoice.CommercialRevenueTaxAmount,
                        serviceInvoice.UserId,
                        serviceInvoice.IsDeleted,
                        serviceInvoice.Notes,
                        serviceInvoice.Image,
                        serviceInvoice.PaymentType,
                        serviceInvoice.CustomerRepId,
                        serviceInvoice.DueDate,
                        ServiceInvoiceDetails,
                        ServiceInvoicePaymentMethods);
                    id = (int)idResult.Value;
                    //-------------------- Notification-------------------------////
                    Notification.GetNotification("ServiceInvoice", "Add", "AddEdit", id, null, "فاتورة خدمات");
                }

                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = serviceInvoice.Id > 0 ? "تعديل  فاتورة خدمات " : "اضافة  فاتورة خدمات",
                    EnAction = "AddEdit",
                    ControllerName = "ServiceInvoice",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = serviceInvoice.Id > 0 ? serviceInvoice.Id : db.ServiceInvoices.Max(i => i.Id),
                    CodeOrDocNo = serviceInvoice.DocumentNumber
                });
                return Json(new { success = "true", id });
            }
            var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .Select(x => new { x.Key, x.Value.Errors })
                    .ToArray();
            return Json(new { success = "false", errors });
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
            var lastObj = db.ServiceInvoices.Where(a => a.IsActive == true && a.IsDeleted == false && a.DepartmentId == id).OrderByDescending(a => a.Id).FirstOrDefault();
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
                    lastObj = db.ServiceInvoices.Where(a => a.IsActive == true && a.IsDeleted == false && a.DepartmentId == id && a.VoucherDate.Month == VoucherDate.Value.Month && a.VoucherDate.Year == VoucherDate.Value.Year).OrderByDescending(a => a.Id).FirstOrDefault();
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
                    lastObj = db.ServiceInvoices.Where(a => a.IsActive == true && a.IsDeleted == false && a.DepartmentId == id && a.VoucherDate.Year == VoucherDate.Value.Year).OrderByDescending(a => a.Id).FirstOrDefault();

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

        }
        [SkipERPAuthorize]
        public JsonResult GetServiceInVoiceItems()
        {
            var items = db.Items.Where(a => a.IsDeleted == false).Select(a => new
            {
                ItemId = a.Id,
                ItemName = a.ArName,
            }).ToList();
            return Json(items, JsonRequestBehavior.AllowGet);

        }

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            var systemSetting = db.SystemSettings.FirstOrDefault();
            var InabilityToEditSalesAndPurchaseInvoicesAndReturns = systemSetting.InabilityToEditSalesAndPurchaseInvoicesAndReturns;
            if (InabilityToEditSalesAndPurchaseInvoicesAndReturns == true)
            {
                return Content("Cannot Be Deleted");
            }
            Models.ServiceInvoice serviceInvoice = db.ServiceInvoices.Find(id);
            serviceInvoice.IsDeleted = true;
            foreach (var detail in serviceInvoice.ServiceInvoiceDetails)
            {
                detail.IsDeleted = true;
            }
            Random random = new Random();
            const string chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            var Code = new string(Enumerable.Repeat(chars, 6).Select(s => s[random.Next(s.Length)]).ToArray());
            serviceInvoice.DocumentNumber = Code;
            db.Entry(serviceInvoice).State = EntityState.Modified;

            var systemPageid = db.SystemPages.Where(a => a.IsActive == true && a.IsDeleted == false && a.ControllerName == "ServiceInvoice").FirstOrDefault().Id;
            JournalEntry journalEntry = db.JournalEntries.Where(a => a.IsActive == true && a.IsDeleted == false && a.SourcePageId == systemPageid && a.SourceId == id).FirstOrDefault();
            journalEntry.IsDeleted = true;
            foreach (var detail in journalEntry.JournalEntryDetails)
            {
                detail.IsDeleted = true;
            }
            db.Entry(journalEntry).State = EntityState.Modified;

            db.SaveChanges();
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "حذف فاتورة خدمات",
                EnAction = "AddEdit",
                ControllerName = "ServiceInvoice",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "POST",
                SelectedItem = id,
                CodeOrDocNo = serviceInvoice.DocumentNumber
            });
            Notification.GetNotification("ServiceInvoice", "Delete", "Delete", id, null, "فاتورة خدمات");
            return Content("true");
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