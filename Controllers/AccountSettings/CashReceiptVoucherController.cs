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
using System.Threading.Tasks;
using MyERP.Repository;
using System.Data.Entity.Core.Objects;
using System.Linq.Expressions;
using MyERP.Models.MyModels;

namespace MyERP.Controllers.AccountSettings
{
    public class CashReceiptVoucherController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        // GET: CashReceiptVoucher
        public async Task<ActionResult> Index(bool? report, int? id, int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "", int departmentId = 0)
        {
            ViewBag.PageIndex = pageIndex;
            ViewBag.OpenReport = report == true;
            if (report == true)
            {
                ViewBag.Id = id;
                ViewBag.Count = 0;
                return View();
            }
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            DepartmentRepository departmentRepository = new DepartmentRepository(db);
            var depIds = await departmentRepository.UserDepartmentsIds(userId);
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة سند القبض",
                EnAction = "Index",
                ControllerName = "CashReceiptVoucher",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            ////-------------------- Notification-------------------------////
            Notification.GetNotification("CashReceiptVoucher", "View", "Index", null, null, "سند القبض");

            //////////////-----------------------------------------------------------------------

            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;


            if (userId == 1)
            {
                ViewBag.DepartmentId = new SelectList(db.Departments.Where(d => d.IsActive == true && d.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName", departmentId);

            }
            else
            {
                ViewBag.DepartmentId = new SelectList(db.UserDepartments.Where(d => d.UserId == userId).Select(b => new
                {
                    Id = b.DepartmentId,
                    ArName = b.Department.Code + " - " + b.Department.ArName
                }), "Id", "ArName", departmentId);

            }
            /////////////////////////// Search ////////////////////

            IQueryable<CashReceiptVoucher> cashReceiptVouchers;
            if (string.IsNullOrEmpty(searchWord))
            {
                cashReceiptVouchers = db.CashReceiptVouchers.Where(s => s.IsDeleted == false && depIds.Contains(s.DepartmentId) && (departmentId == 0 || s.DepartmentId == departmentId)).OrderByDescending(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = await db.CashReceiptVouchers.Where(s => s.IsDeleted == false && depIds.Contains(s.DepartmentId) && (departmentId == 0 || s.DepartmentId == departmentId)).CountAsync();
            }
            else
            {
                cashReceiptVouchers = db.CashReceiptVouchers.Where(s => s.IsDeleted == false && depIds.Contains(s.DepartmentId) && (departmentId == 0 || s.DepartmentId == departmentId) && (s.DocumentNumber.Contains(searchWord) || s.Branch.ArName.Contains(searchWord) || s.CashReceiptSourceType.ArName.Contains(searchWord) || s.Currency.ArName.Contains(searchWord) || s.Notes.Contains(searchWord) || s.OtherSourceName.Contains(searchWord) || s.Vendor.ArName.Contains(searchWord) || s.CashBox.ArName.Contains(searchWord) || s.ChartOfAccount.ArName.Contains(searchWord) || s.Customer.ArName.Contains(searchWord) || s.Employee.ArName.Contains(searchWord) || s.Department.ArName.Contains(searchWord) || s.Techanician.ArName.Contains(searchWord))).OrderByDescending(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = await db.CashReceiptVouchers.Where(s => s.IsDeleted == false && depIds.Contains(s.DepartmentId) && (departmentId == 0 || s.DepartmentId == departmentId) && (s.DocumentNumber.Contains(searchWord) || s.Branch.ArName.Contains(searchWord) || s.CashReceiptSourceType.ArName.Contains(searchWord) || s.Currency.ArName.Contains(searchWord) || s.Notes.Contains(searchWord) || s.OtherSourceName.Contains(searchWord) || s.Vendor.ArName.Contains(searchWord) || s.CashBox.ArName.Contains(searchWord) || s.ChartOfAccount.ArName.Contains(searchWord) || s.Customer.ArName.Contains(searchWord) || s.Employee.ArName.Contains(searchWord) || s.Department.ArName.Contains(searchWord) || s.Techanician.ArName.Contains(searchWord))).CountAsync();
            }

            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            return View(await cashReceiptVouchers.ToListAsync());
        }


        /// <summary>
        /// جلب قائمة التصفيات النشطة
        /// </summary>
        [HttpGet]
        public JsonResult GetActiveSettlements()
        {
            try
            {
                var settlements = db.PropertyContractTerminations
                    .Where(s =>  !s.IsDeleted)
                    .OrderByDescending(s => s.Id)
                    .Select(s => new
                    {
                        Id = s.Id,
                        DocumentNumber = s.DocumentNumber,
                        VoucherDate = s.VoucherDate,
                        PropertyRenterId = s.PropertyRenterId,
                        RenterName = s.PropertyRenter.ArName,
                        RenterCode = s.PropertyRenter.Code,
                        PropertyName = s.PropertyContract.Property.ArName,
                        ContractNumber = s.PropertyContract.DocumentNumber,
                        TotalAmount = s.TotalUnpaidAmount ?? 0,
                        RenterBalance = s.RenterBalance ?? 0,
                        InsuranceAmount = s.InsuranceAmount ?? 0
                    })
                    .ToList();

                return Json(settlements, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// جلب تفاصيل تصفية معينة مع الدفعات
        /// </summary>
        /// <param name="id">رقم التصفية</param>
        [HttpGet]
        public JsonResult GetSettlementDetails(int id)
        {
            try
            {
                // ✅ 1) جلب سجل التصفية مع العقد والمستأجر والعقار
                var termination = db.PropertyContractTerminations
                    .Include("PropertyContract.Property")
                    .Include("PropertyContract.PropertyRenter")
                    .FirstOrDefault(pct => pct.Id == id && pct.IsDeleted == false);

                if (termination == null)
                {
                    return Json(new { success = false, message = "التصفية غير موجودة" }, JsonRequestBehavior.AllowGet);
                }

                var contract = termination.PropertyContract;
                if (contract == null)
                {
                    return Json(new { success = false, message = "العقد المرتبط بالتصفية غير موجود" }, JsonRequestBehavior.AllowGet);
                }

                // ✅ 2) جلب تفاصيل التصفية من جدول PropertyContractTerminationDetail
                var terminationDetails = db.PropertyContractTerminationDetails
                    .Where(d => d.MainDocId == termination.Id && d.IsDeleted == false)
                    .ToList();

                // ✅ 3) جلب أرقام الدفعات المرتبطة بالتصفية
                var batchIds = terminationDetails
                    .Where(d => d.PropertyContractBatchId.HasValue)
                    .Select(d => d.PropertyContractBatchId.Value)
                    .ToList();

                // ✅ 4) حساب إجمالي التلفيات من جدول PropertyContractTerminationDamage (Qty * Price)
                var totalDamages = db.PropertyContractTerminationDamages
             .Where(d => d.PropertyContractTerminationId == termination.Id)
             .ToList()   // هنا نطلعهم من الـ DB الأول
             .Sum(d => (decimal)(d.Qty ?? 0) * (d.Price ?? 0));


                // ✅ 5) جلب المدفوعات السابقة من جدول السداد
                var payments = db.CashReceiptVoucherPropertyContractBatches
                    .AsNoTracking()
                    .Where(p => p.PropertyContractBatchId.HasValue
                             && batchIds.Contains(p.PropertyContractBatchId.Value)
                             && p.Paid != 0)
                    .GroupBy(p => p.PropertyContractBatchId.Value)
                    .Select(g => new
                    {
                        PropertyContractBatchId = g.Key,
                        TotalPaid = g.Sum(x => x.Paid ?? 0),
                        IsFullyDelivered = g.Any(x => x.IsDelivered == true)
                    })
                    .ToList();

                // ✅ 6) جلب الدفعات الفعلية وحساب المتبقي
                var relevantBatches = db.PropertyContractBatches
                    .Where(b => batchIds.Contains(b.Id))
                    .ToList();

                var batchDetails = relevantBatches.Select(b =>
                {
                    var payment = payments.FirstOrDefault(p => p.PropertyContractBatchId == b.Id);
                    var termDetail = terminationDetails.FirstOrDefault(d => d.PropertyContractBatchId == b.Id);

                    decimal totalPaid = payment?.TotalPaid ?? 0;
                    bool isFullyPaid = payment?.IsFullyDelivered == true;

                    decimal batchRentOriginal =
                        (b.BatchRentValue ?? 0) + (b.BatchRentValueTaxes ?? 0);
                    decimal batchWaterOriginal =
                        (b.BatchWaterValue ?? 0) + (b.BatchWaterValueTaxes ?? 0);
                    decimal batchElectricityOriginal =
                        (b.BatchElectricityValue ?? 0) + (b.BatchElectricityValueTaxes ?? 0);
                    decimal batchGasOriginal =
                        (b.BatchGasValue ?? 0) + (b.BatchGasValueTaxes ?? 0);
                    decimal batchServicesOriginal =
                        (b.BatchServicesValue ?? 0) + (b.BatchServicesValueTaxes ?? 0);
                    decimal batchCommissionOriginal =
                        (b.BatchCommissionValue ?? 0) + (b.BatchCommissionValueTaxes ?? 0);
                    decimal batchInsuranceOriginal =
                        (b.BatchInsuranceValue ?? 0) + (b.BatchInsuranceValueTaxes ?? 0);

                    decimal batchTotalOriginal = b.BatchTotal ?? 0;

                    // المتبقي الحقيقي من الدفعة = إجمالي الفاتورة - مجموع المدفوعات
                    // أو استخدام القيمة المخزنة في تفاصيل التصفية
                    decimal remain = termDetail?.Remain ?? (batchTotalOriginal - totalPaid);
                    if (remain < 0) remain = 0;

                    return new
                    {
                        b.Id,
                        b.BatchNo,
                        b.BatchDate,

                        BatchRentValue = isFullyPaid || batchTotalOriginal <= 0 ? 0 :
                                        (remain > 0 ? (batchRentOriginal * remain / batchTotalOriginal) : 0),

                        BatchWaterValue = isFullyPaid || batchTotalOriginal <= 0 ? 0 :
                                         (remain > 0 ? (batchWaterOriginal * remain / batchTotalOriginal) : 0),

                        BatchElectricityValue = isFullyPaid || batchTotalOriginal <= 0 ? 0 :
                                               (remain > 0 ? (batchElectricityOriginal * remain / batchTotalOriginal) : 0),

                        BatchGasValue = isFullyPaid || batchTotalOriginal <= 0 ? 0 :
                                       (remain > 0 ? (batchGasOriginal * remain / batchTotalOriginal) : 0),

                        BatchServicesValue = isFullyPaid || batchTotalOriginal <= 0 ? 0 :
                                            (remain > 0 ? (batchServicesOriginal * remain / batchTotalOriginal) : 0),

                        BatchCommissionValue = isFullyPaid || batchTotalOriginal <= 0 ? 0 :
                                              (remain > 0 ? (batchCommissionOriginal * remain / batchTotalOriginal) : 0),

                        BatchInsuranceValue = isFullyPaid || batchTotalOriginal <= 0 ? 0 :
                                             (remain > 0 ? (batchInsuranceOriginal * remain / batchTotalOriginal) : 0),

                        Remain = remain,
                        OriginalTotal = batchTotalOriginal,
                        TotalPaid = totalPaid
                    };
                }).ToList();

                // ✅ 7) إجمالي المتبقي + إجمالي التأمين المتبقي
                decimal totalUnpaidAmount = batchDetails.Sum(b => b.Remain);
                decimal insuranceAmount = batchDetails.Sum(b => b.BatchInsuranceValue);

                // ✅ رصيد المستأجر من التصفية نفسها
                decimal renterBalance = termination.RenterBalance ?? 0;

                // ✅ 8) تجهيز النتيجة النهائية لسند القبض
                var result = new
                {
                    termination.Id,
                    termination.DocumentNumber,

                    termination.PropertyContractId,
                    ContractNumber = contract.DocumentNumber,

                    RenterName = contract.PropertyRenter?.ArName,
                    RenterCode = contract.PropertyRenter?.Code,
                    PropertyName = contract.Property?.ArName,

                    // أيام الزيادة
                    IncreaseDaysNo = termination.IncreaseDaysNo ?? 0,
                    IncreaseDayValue = termination.IncreaseDayValue ?? 0,
                    IncreaseDaysTotalValue = termination.IncreaseDaysTotalValue ?? 0,

                    // أيام النقص
                    DecreaseDaysNo = termination.DecreaseDaysNo ?? 0,
                    DecreaseDayValue = termination.DecreaseDayValue ?? 0,
                    DecreaseDaysTotalValue = termination.DecreaseDaysTotalValue ?? 0,

                    // ✅ إجمالي التلفيات
                    TotalDamages = totalDamages,

                    // ✅ التأمين من تفاصيل الدفعات
                    InsuranceAmount = insuranceAmount,

                    // ✅ رصيد المستأجر المجمع من التصفية
                    RenterBalance = renterBalance,

                    // ✅ إجمالي الدفعات غير المسددة
                    TotalUnpaidAmount = termination.DecreaseDayValue ?? 0, 

                    // ✅ تفاصيل الدفعات المتبقية
                    Details = batchDetails
                };

                return Json(new { success = true, data = result }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        [HttpGet]
        public async Task<ActionResult> ContractList(int Id)
        {



            List<SelectListItem> li = new List<SelectListItem>();
            var cntr = db.PropertyContracts.Where(c => c.IsDeleted == false && c.PropertyRenterId == Id);
            foreach (var contract in cntr)
            {
                {
                    li.Add(new SelectListItem { Text = contract.DocumentNumber + " - " + contract.UnifiedContractNumber, Value = contract.Id.ToString() });
                }
            }

            return Json(li, JsonRequestBehavior.AllowGet);
        }

        [Route("CashReceiptVoucher/AddEditForPropertyBill/{propBillId}")]
        public async Task<ActionResult> AddEditForPropertyBill(string propBillId)
        {
            int billId = int.Parse(propBillId);
            CashReceiptVoucher cashReceiptVoucher = new CashReceiptVoucher();
            PropertyBillRegisteration propBillRegisteration = await db.PropertyBillRegisterations.FindAsync(billId);

            SystemSetting systemSetting = await db.SystemSettings.AnyAsync() ? await db.SystemSettings.FirstOrDefaultAsync() : new SystemSetting();
            ViewBag.UseCostCenter = systemSetting.UseCostCenter;
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            DepartmentRepository departmentRepository = new DepartmentRepository(db);
            CashboxReposistory cashboxReposistory = new CashboxReposistory(db);
            List<int> year = new List<int>();
            int sysPageId = QueryHelper.SourcePageId("PropertyBillRegisteration");

            ViewBag.JE = db.GetJournalEntryBySourceIdAndSourcePageId(billId, sysPageId).FirstOrDefault();

            if (ViewBag.JE != null)
            {
                var JEDocNo = ViewBag.JE;
                ViewBag.JE.DocumentNumber = JEDocNo != null ? JEDocNo.DocumentNumber.Replace("-", "") : "";
                int JEId = ViewBag.JE.Id;
                int sourcePageId = ViewBag.JE.SourcePageId;
                
                ViewBag.JEDetails = SwapJournalEntry(billId, JEId, sourcePageId);
                cashReceiptVoucher.DepartmentId = ViewBag.JE.DepartmentId;
            }

            cashReceiptVoucher.PropertyContractId = propBillRegisteration.ContractId;
            cashReceiptVoucher.RenterId = propBillRegisteration.RenterId;
            cashReceiptVoucher.TransactionDate = propBillRegisteration.BillRegDate;
            cashReceiptVoucher.SourceTypeId = 12; //حساب تسجيل مصاريف
            cashReceiptVoucher.MoneyAmount = propBillRegisteration.ElectricityBillValue
                + propBillRegisteration.GasBillValue
                + propBillRegisteration.ViolationBillValue;
            cashReceiptVoucher.GasBillValue = propBillRegisteration.GasBillValue;
            cashReceiptVoucher.ElectricityBillValue = propBillRegisteration.ElectricityBillValue;
            cashReceiptVoucher.ViolationBillValue = propBillRegisteration.ViolationBillValue;
            
            
            var renters = db.PropertyRenters.Where(e => e.IsDeleted == false && e.IsActive == true);
            
            ViewBag.TechnicianId = new SelectList(db.Techanicians.Where(b => b.IsDeleted == false && b.IsActive == true).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName");

            ViewBag.AccountId = new SelectList(db.ChartOfAccounts.Where(c => c.IsDeleted == false && c.IsActive == true && c.ClassificationId == 3).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName");

            ViewBag.SourceTypeId = new SelectList(db.CashReceiptSourceTypes.Where(c => c.IsDeleted == false && c.IsActive == true && c.Id != 5), "Id", "ArName", cashReceiptVoucher.SourceTypeId);
            ViewBag.VendorId = new SelectList(db.Vendors.Where(v => v.IsDeleted == false && v.IsActive == true).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName");
            ViewBag.ShareholderId = new SelectList(db.ShareHolders.Where(v => v.IsDeleted == false && v.IsActive == true).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName");
            ViewBag.CustomerId = new SelectList(db.Customers.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName");

            ViewBag.RenterId = new SelectList(db.PropertyRenters.Where(e => e.IsDeleted == false && e.IsActive == true).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", cashReceiptVoucher.RenterId);


            ViewBag.EmployeeId = new SelectList(db.Employees.Where(e => e.IsDeleted == false && e.IsActive == true).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName");

            ViewBag.DirectRevenueId = new SelectList(db.DirectRevenues.Where(d => d.IsDeleted == false && d.IsActive == true).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName");

            ViewBag.DepartmentId = new SelectList(await departmentRepository.UserDepartments(userId).ToListAsync(), "Id", "ArName", cashReceiptVoucher.DepartmentId);
            ViewBag.CashBoxId = new SelectList(await cashboxReposistory.UserCashboxes(userId, systemSetting.DefaultDepartmentId).ToListAsync(), "Id", "ArName", systemSetting.DefaultCashBoxId);
            ViewBag.CostCenterId = new SelectList(db.CostCenters.Where(a => a.IsActive && !a.IsDeleted && a.TypeId == 2).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName");
            ViewBag.ChartOfAccountId = new SelectList(db.ChartOfAccounts.Where(c => c.IsDeleted == false && c.IsActive == true && c.ClassificationId == 3).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName");

            ViewBag.BankAccountId = new SelectList(db.BankAccounts.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
            {
                Id = b.Id,
                ArName = b.AccountNumber
            }), "Id", "ArName");

            ViewBag.CashReceiptPaymentMethodId = new SelectList(db.CashReceiptPaymentMethods.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName");
            //Month
            ViewBag.Month = new SelectList(new List<dynamic> {
            new { id=1,name="يناير"},
            new { id=2,name="فبراير"},
            new { id=3,name="مارس"},
            new { id=4,name="إبريل"},
            new { id=5,name="مايو"},
            new { id=6,name="يونيه"},
            new { id=7,name="يوليو"},
            new { id=8,name="اغسطس"},
            new { id=9,name="سبتمبر"},
            new { id=10,name="اكتوبر"},
            new { id=11,name="نوفمبر"},
            new { id=12,name="ديسمبر"}}, "id", "name");

            //year
            for (var i = 2019; i <= 2030; i++)
            {
                year.Add(i);
                ViewBag.Year = new SelectList(year);
            }
            ViewBag.ChildrenId = new SelectList(db.Children.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName");
            ViewBag.ElderId = new SelectList(db.Elders.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName");

            ViewBag.PropertyContractId = new SelectList(db.PropertyContracts.Where(c => c.IsDeleted == false).Select(b => new
            {
                Id = b.Id,
                ArName = b.DocumentNumber + " - " + b.UnifiedContractNumber
            }), "Id", "ArName", cashReceiptVoucher.PropertyContractId);

            //------ Time Zone Depends On Currency --------//
            var Currency = db.Currencies.FirstOrDefault(a => a.IsActive == true && a.IsDeleted == false && a.IsDefault == true);
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

            //ViewBag.Date = cTime.ToString("yyyy-MM-ddTHH:mm");
            ViewBag.Date = propBillRegisteration.BillRegDate?.ToString("yyyy-MM-ddTHH:mm");

            ViewBag.FromPropertyBillRegisteration = true;

            return View("AddEdit", cashReceiptVoucher);
        }
        // GET: CashReceiptVoucher/Edit/5
        public async Task<ActionResult> AddEdit(int? id, int? cid = null)
        {
            if (id == 0)
            {
                id = null;
            }
            SystemSetting systemSetting = await db.SystemSettings.AnyAsync() ? await db.SystemSettings.FirstOrDefaultAsync() : new SystemSetting();
            ViewBag.UseCostCenter = systemSetting.UseCostCenter;
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            DepartmentRepository departmentRepository = new DepartmentRepository(db);
            CashboxReposistory cashboxReposistory = new CashboxReposistory(db);
            List<int> year = new List<int>();

            if (id == null)
            {
                var renters = db.PropertyRenters.Where(e => e.IsDeleted == false && e.IsActive == true);
                int? firstRnter = 0;

                var cnt = db.PropertyContracts.FirstOrDefault(c => c.Id == cid);
                if (cnt != null)
                {
                    firstRnter = cnt.PropertyRenterId;
                }
                else
                {
                    firstRnter = renters.FirstOrDefault()?.Id ?? 0;
                }

                ViewBag.TechnicianId = new SelectList(db.Techanicians.Where(b => b.IsDeleted == false && b.IsActive == true).Select(b => new
                {
                    b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");

                ViewBag.AccountId = new SelectList(db.ChartOfAccounts.Where(c => c.IsDeleted == false && c.IsActive == true && c.ClassificationId == 3).Select(b => new
                {
                    b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");

                ViewBag.SourceTypeId = new SelectList(db.CashReceiptSourceTypes.Where(c => c.IsDeleted == false && c.IsActive == true && c.Id != 5), "Id", "ArName", cid != null ? 11 : 1);
                ViewBag.VendorId = new SelectList(db.Vendors.Where(v => v.IsDeleted == false && v.IsActive == true).Select(b => new
                {
                    b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                ViewBag.ShareholderId = new SelectList(db.ShareHolders.Where(v => v.IsDeleted == false && v.IsActive == true).Select(b => new
                {
                    b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                ViewBag.CustomerId = new SelectList(db.Customers.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
                {
                    b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");

                ViewBag.RenterId = new SelectList(db.PropertyRenters.Where(e => e.IsDeleted == false && e.IsActive == true).Select(b => new
                {
                    b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");


                ViewBag.EmployeeId = new SelectList(db.Employees.Where(e => e.IsDeleted == false && e.IsActive == true).Select(b => new
                {
                    b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");

                ViewBag.DirectRevenueId = new SelectList(db.DirectRevenues.Where(d => d.IsDeleted == false && d.IsActive == true).Select(b => new
                {
                    b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");

                ViewBag.DepartmentId = new SelectList(await departmentRepository.UserDepartments(userId).ToListAsync(), "Id", "ArName", systemSetting.DefaultDepartmentId);
                ViewBag.CashBoxId = new SelectList(await cashboxReposistory.UserCashboxes(userId, systemSetting.DefaultDepartmentId).ToListAsync(), "Id", "ArName", systemSetting.DefaultCashBoxId);
                ViewBag.CostCenterId = new SelectList(db.CostCenters.Where(a => a.IsActive && !a.IsDeleted && a.TypeId == 2).Select(b => new
                {
                    b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                ViewBag.ChartOfAccountId = new SelectList(db.ChartOfAccounts.Where(c => c.IsDeleted == false && c.IsActive == true && c.ClassificationId == 3).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");

                ViewBag.BankAccountId = new SelectList(db.BankAccounts.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.AccountNumber
                }), "Id", "ArName");

                ViewBag.CashReceiptPaymentMethodId = new SelectList(db.CashReceiptPaymentMethods.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                //Month
                ViewBag.Month = new SelectList(new List<dynamic> {
                new { id=1,name="يناير"},
                new { id=2,name="فبراير"},
                new { id=3,name="مارس"},
                new { id=4,name="إبريل"},
                new { id=5,name="مايو"},
                new { id=6,name="يونيه"},
                new { id=7,name="يوليو"},
                new { id=8,name="اغسطس"},
                new { id=9,name="سبتمبر"},
                new { id=10,name="اكتوبر"},
                new { id=11,name="نوفمبر"},
                new { id=12,name="ديسمبر"}}, "id", "name");

                //year
                for (var i = 2019; i <= 2030; i++)
                {
                    year.Add(i);
                    ViewBag.Year = new SelectList(year);
                }
                ViewBag.ChildrenId = new SelectList(db.Children.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                ViewBag.ElderId = new SelectList(db.Elders.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");

                ViewBag.RenterId = new SelectList(renters.Select(b => new
                {
                    b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName", firstRnter);

                ViewBag.PropertyContractId = new SelectList(db.PropertyContracts.Where(c => c.IsDeleted == false && c.PropertyRenterId == firstRnter).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.DocumentNumber + " - " + b.UnifiedContractNumber
                }), "Id", "ArName", cid);


                //------ Time Zone Depends On Currency --------//
                var Currency = db.Currencies.FirstOrDefault(a => a.IsActive == true && a.IsDeleted == false && a.IsDefault == true);
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

                ViewBag.Date = cTime.ToString("yyyy-MM-ddTHH:mm");
                return View();
            }
            int sysPageId = QueryHelper.SourcePageId("CashReceiptVoucher");
            ViewBag.JE = db.GetJournalEntryBySourceIdAndSourcePageId(id, sysPageId).FirstOrDefault();
            if (ViewBag.JE != null)
            {
                var JEDocNo = ViewBag.JE;
                ViewBag.JE.DocumentNumber = JEDocNo != null ? JEDocNo.DocumentNumber.Replace("-", "") : "";
                int JEId = ViewBag.JE.Id;
                int sourcePageId = ViewBag.JE.SourcePageId;
                ViewBag.JEDetails = db.JournalEntryDetails.Where(a => a.SourceId == id && a.JournalEntryId == JEId && a.SourcePageId == sourcePageId).OrderBy(a => a.Id).ToList();

            }
            ViewBag.Accounts = new SelectList(db.ChartOfAccounts.Where(a => a.ClassificationId == 3 && a.IsActive == true && a.IsDeleted == false), "Id", "ArName");

            CashReceiptVoucher cashReceiptVoucher = await db.CashReceiptVouchers.FindAsync(id);
            if (cashReceiptVoucher == null)
            {
                return HttpNotFound();
            }
            ViewBag.TechnicianId = new SelectList(db.Techanicians.Where(b => b.IsDeleted == false && b.IsActive == true).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", cashReceiptVoucher.TechnicianId);

            ViewBag.BranchId = new SelectList(db.Branches.Where(b => b.IsDeleted == false && b.IsActive == true).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", cashReceiptVoucher.BranchId);
            ViewBag.AccountId = new SelectList(db.ChartOfAccounts.Where(c => c.IsDeleted == false && c.IsActive == true && c.ClassificationId == 3).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", cashReceiptVoucher.AccountId);
            ViewBag.CurrencyId = new SelectList(db.Currencies.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", cashReceiptVoucher.CurrencyId);
            ViewBag.SourceTypeId = new SelectList(db.CashReceiptSourceTypes.Where(c => c.IsDeleted == false && c.IsActive == true && c.Id != 5), "Id", "ArName", cashReceiptVoucher.SourceTypeId);
            ViewBag.VendorId = new SelectList(db.Vendors.Where(v => v.IsDeleted == false && v.IsActive == true).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", cashReceiptVoucher.VendorId);
            ViewBag.ShareholderId = new SelectList(db.ShareHolders.Where(v => v.IsDeleted == false && v.IsActive == true).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", cashReceiptVoucher.ShareholderId);
            ViewBag.CustomerId = new SelectList(db.Customers.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", cashReceiptVoucher.CustomerId);
            ViewBag.EmployeeId = new SelectList(db.Employees.Where(e => e.IsDeleted == false && e.IsActive == true).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", cashReceiptVoucher.EmployeeId);

            cashReceiptVoucher.RenterId = cashReceiptVoucher.PropertyContract?.PropertyRenterId;
            ViewBag.RenterId = new SelectList(db.PropertyRenters.Where(e => e.IsDeleted == false && e.IsActive == true).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", cashReceiptVoucher.RenterId);

            ViewBag.DirectRevenueId = new SelectList(db.DirectRevenues.Where(d => d.IsDeleted == false && d.IsActive == true).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", cashReceiptVoucher.DirectRevenueId);
            ViewBag.DepartmentId = new SelectList(await departmentRepository.UserDepartments(userId).ToListAsync(), "Id", "ArName", cashReceiptVoucher.DepartmentId);
            ViewBag.CashBoxId = new SelectList(await cashboxReposistory.UserCashboxes(userId, cashReceiptVoucher.DepartmentId).ToListAsync(), "Id", "ArName", cashReceiptVoucher.CashBoxId);

            ViewBag.CostCenterId = new SelectList(db.CostCenters.Where(a => a.IsActive && !a.IsDeleted && a.TypeId == 2).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", cashReceiptVoucher.CostCenterId);
            ViewBag.ChartOfAccountId = new SelectList(db.ChartOfAccounts.Where(c => c.IsDeleted == false && c.IsActive == true && c.ClassificationId == 3).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", cashReceiptVoucher.ChartOfAccountId);

            ViewBag.BankAccountId = new SelectList(db.BankAccounts.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
            {
                Id = b.Id,
                ArName = b.AccountNumber
            }), "Id", "ArName", cashReceiptVoucher.BankAccountId);

            ViewBag.CashReceiptPaymentMethodId = new SelectList(db.CashReceiptPaymentMethods.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", cashReceiptVoucher.CashReceiptPaymentMethodId);
            ViewBag.ChildrenId = new SelectList(db.Children.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", cashReceiptVoucher.ChildrenId);
            ViewBag.ElderId = new SelectList(db.Elders.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", cashReceiptVoucher.ElderId);
            ViewBag.PropertyContractId = new SelectList(db.PropertyContracts.Where(c => c.IsDeleted == false).Select(b => new
            {
                Id = b.Id,
                ArName = b.DocumentNumber + " - " + b.UnifiedContractNumber
            }), "Id", "ArName", cashReceiptVoucher.PropertyContractId);

            //Month
            ViewBag.Month = new SelectList(new List<dynamic> {
                 new { id=1,name="يناير"},
                new { id=2,name="فبراير"},
                new { id=3,name="مارس"},
                new { id=4,name="إبريل"},
                new { id=5,name="مايو"},
                new { id=6,name="يونيه"},
                new { id=7,name="يوليو"},
                new { id=8,name="اغسطس"},
                new { id=9,name="سبتمبر"},
                new { id=10,name="اكتوبر"},
                new { id=11,name="نوفمبر"},
                new { id=12,name="ديسمبر"}}, "id", "name", cashReceiptVoucher.Month);

            //year
            for (var i = 2019; i <= 2030; i++)
            {
                year.Add(i);
                ViewBag.Year = new SelectList(year, cashReceiptVoucher.Year);
            }
            ViewBag.Next = QueryHelper.Next((int)id, "CashReceiptVoucher");
            ViewBag.Previous = QueryHelper.Previous((int)id, "CashReceiptVoucher");
            ViewBag.Last = QueryHelper.GetLast("CashReceiptVoucher");
            ViewBag.First = QueryHelper.GetFirst("CashReceiptVoucher");
            ViewBag.Date = cashReceiptVoucher.Date.ToString("yyyy-MM-ddTHH:mm");
            if (cashReceiptVoucher.TransactionDate != null)
            {
                ViewBag.TransactionDate = cashReceiptVoucher.TransactionDate.Value.ToString("yyyy-MM-ddTHH:mm");
            }

            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح تفاصيل سند القبض",
                EnAction = "AddEdit",
                ControllerName = "CashReceiptVoucher",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = cashReceiptVoucher.Id > 0 ? cashReceiptVoucher.Id : db.CashReceiptVouchers.Max(i => i.Id),
                CodeOrDocNo = cashReceiptVoucher.DocumentNumber
            });
            var voIds = cashReceiptVoucher.CashReceiptVoucherPropertyContractBatches
                .Select(t => t.PropertyContractBatchId).ToList();

            var props = db.PropertyDetails.Select(t => new UniteModel{
                Id = t.Id,
                PropertyUnitNo  = t.PropertyUnitNo??"0"
            }).ToList();

            var bathces = (from bt in db.CashReceiptVoucherPropertyContractBatches
                           where voIds.Contains(bt.PropertyContractBatchId)
                           select new { bt.PropertyContractBatchId, bt.Paid  }).GroupBy(t => t.PropertyContractBatchId).Select(t => new BatchPaidModel
                           {
                               Id = t.Key ?? 0,
                               Paid = t.Sum(n => n.Paid ?? 0)
                           }).ToList();

            ViewBag.BathcesPaid = bathces;

            ViewBag.PropertyUnits = props;
            return View(cashReceiptVoucher);
        }

        // POST: CashReceiptVoucher/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        //[ValidateAntiForgeryToken]
        public async Task<ActionResult> AddEdit(CashReceiptVoucher cashReceiptVoucher)
        {
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

            /*--- Document Coding ---*/
            var DocumentCoding = "";
            SystemSetting systemSetting = db.SystemSettings.Any() ? db.SystemSettings.FirstOrDefault() : new SystemSetting();
            if (systemSetting.DocumentCoding == true)
            {
                DocumentCoding = cashReceiptVoucher.DocumentNumber;
            }
            DocumentCoding = DocumentCoding.Length > 0 ? DocumentCoding : null;
            /*-------**************** End Of Document Coding *****************--------*/

            if (ModelState.IsValid)
            {
                var id = cashReceiptVoucher.Id;
                cashReceiptVoucher.IsDeleted = false;

                if (cashReceiptVoucher.SourceTypeId == 1)
                {
                    cashReceiptVoucher.ShareholderId = null;
                    cashReceiptVoucher.VendorId = null;
                    cashReceiptVoucher.EmployeeId = null;
                    cashReceiptVoucher.RenterId = null;
                    cashReceiptVoucher.DirectRevenueId = null;
                    cashReceiptVoucher.TechnicianId = null;
                    cashReceiptVoucher.AccountId = null;
                    cashReceiptVoucher.ChildrenId = null;
                    cashReceiptVoucher.ElderId = null;
                    cashReceiptVoucher.PropertyContractId = null;
                }
                else if (cashReceiptVoucher.SourceTypeId == 2)
                {
                    cashReceiptVoucher.ShareholderId = null;
                    cashReceiptVoucher.TechnicianId = null;
                    cashReceiptVoucher.CustomerId = null;
                    cashReceiptVoucher.DirectRevenueId = null;
                    cashReceiptVoucher.EmployeeId = null;
                    cashReceiptVoucher.RenterId = null;
                    cashReceiptVoucher.AccountId = null;
                    cashReceiptVoucher.ChildrenId = null;
                    cashReceiptVoucher.ElderId = null;
                    cashReceiptVoucher.PropertyContractId = null;
                }
                else if (cashReceiptVoucher.SourceTypeId == 3)
                {
                    cashReceiptVoucher.ShareholderId = null;
                    cashReceiptVoucher.TechnicianId = null;
                    cashReceiptVoucher.DirectRevenueId = null;
                    cashReceiptVoucher.VendorId = null;
                    cashReceiptVoucher.RenterId = null;
                    cashReceiptVoucher.CustomerId = null;
                    cashReceiptVoucher.AccountId = null;
                    cashReceiptVoucher.ChildrenId = null;
                    cashReceiptVoucher.ElderId = null;
                    cashReceiptVoucher.PropertyContractId = null;
                }

                else if (cashReceiptVoucher.SourceTypeId == 5)
                {
                    cashReceiptVoucher.ShareholderId = null;
                    cashReceiptVoucher.VendorId = null;
                    cashReceiptVoucher.CustomerId = null;
                    cashReceiptVoucher.RenterId = null;
                    cashReceiptVoucher.EmployeeId = null;
                    cashReceiptVoucher.DirectRevenueId = null;
                    cashReceiptVoucher.AccountId = null;
                    cashReceiptVoucher.ChildrenId = null;
                    cashReceiptVoucher.ElderId = null;
                    cashReceiptVoucher.PropertyContractId = null;
                }
                else if (cashReceiptVoucher.SourceTypeId == 4)
                {
                    cashReceiptVoucher.ShareholderId = null;
                    cashReceiptVoucher.TechnicianId = null;
                    cashReceiptVoucher.VendorId = null;
                    cashReceiptVoucher.CustomerId = null;
                    cashReceiptVoucher.EmployeeId = null;
                    cashReceiptVoucher.RenterId = null;
                    cashReceiptVoucher.AccountId = null;
                    cashReceiptVoucher.ChildrenId = null;
                    cashReceiptVoucher.ElderId = null;
                    cashReceiptVoucher.PropertyContractId = null;
                }
                else if (cashReceiptVoucher.SourceTypeId == 6)
                {
                    cashReceiptVoucher.ShareholderId = null;
                    cashReceiptVoucher.TechnicianId = null;
                    cashReceiptVoucher.VendorId = null;
                    cashReceiptVoucher.CustomerId = null;
                    cashReceiptVoucher.EmployeeId = null;
                    cashReceiptVoucher.RenterId = null;
                    cashReceiptVoucher.DirectRevenueId = null;
                    cashReceiptVoucher.ChildrenId = null;
                    cashReceiptVoucher.ElderId = null;
                    cashReceiptVoucher.PropertyContractId = null;
                }
                else if (cashReceiptVoucher.SourceTypeId == 8)
                {
                    cashReceiptVoucher.TechnicianId = null;
                    cashReceiptVoucher.VendorId = null;
                    cashReceiptVoucher.CustomerId = null;
                    cashReceiptVoucher.EmployeeId = null;
                    cashReceiptVoucher.RenterId = null;
                    cashReceiptVoucher.DirectRevenueId = null;
                    cashReceiptVoucher.AccountId = null;
                    cashReceiptVoucher.ChildrenId = null;
                    cashReceiptVoucher.ElderId = null;
                    cashReceiptVoucher.PropertyContractId = null;
                }
                else if (cashReceiptVoucher.SourceTypeId == 9) //Elder
                {
                    cashReceiptVoucher.TechnicianId = null;
                    cashReceiptVoucher.VendorId = null;
                    cashReceiptVoucher.CustomerId = null;
                    cashReceiptVoucher.EmployeeId = null;
                    cashReceiptVoucher.DirectRevenueId = null;
                    cashReceiptVoucher.RenterId = null;
                    cashReceiptVoucher.AccountId = null;
                    cashReceiptVoucher.ShareholderId = null;
                    cashReceiptVoucher.ChildrenId = null;
                    cashReceiptVoucher.PropertyContractId = null;
                }
                else if (cashReceiptVoucher.SourceTypeId == 10) //Children
                {
                    cashReceiptVoucher.TechnicianId = null;
                    cashReceiptVoucher.VendorId = null;
                    cashReceiptVoucher.CustomerId = null;
                    cashReceiptVoucher.EmployeeId = null;
                    cashReceiptVoucher.DirectRevenueId = null;
                    cashReceiptVoucher.RenterId = null;
                    cashReceiptVoucher.AccountId = null;
                    cashReceiptVoucher.ShareholderId = null;
                    cashReceiptVoucher.ElderId = null;
                    cashReceiptVoucher.PropertyContractId = null;
                }
                else if (cashReceiptVoucher.SourceTypeId == 11) //PropertyContract
                {
                    cashReceiptVoucher.TechnicianId = null;
                    cashReceiptVoucher.VendorId = null;
                    cashReceiptVoucher.CustomerId = null;
                    cashReceiptVoucher.EmployeeId = null;

                    cashReceiptVoucher.DirectRevenueId = null;
                    cashReceiptVoucher.AccountId = null;
                    cashReceiptVoucher.ShareholderId = null;
                    cashReceiptVoucher.ElderId = null;
                    cashReceiptVoucher.ChildrenId = null;
                }
                else if (cashReceiptVoucher.SourceTypeId == 12) //PropertyBillRegisteration
                {
                    cashReceiptVoucher.TechnicianId = null;
                    cashReceiptVoucher.VendorId = null;
                    cashReceiptVoucher.CustomerId = null;
                    cashReceiptVoucher.EmployeeId = null;

                    cashReceiptVoucher.DirectRevenueId = null;
                    cashReceiptVoucher.AccountId = null;
                    cashReceiptVoucher.ShareholderId = null;
                    cashReceiptVoucher.ElderId = null;
                    cashReceiptVoucher.ChildrenId = null;
                }

                if (cashReceiptVoucher.CashReceiptPaymentMethodId == 1)
                {
                    cashReceiptVoucher.ChartOfAccountId = null;
                    cashReceiptVoucher.BankAccountId = null;
                }
                else if (cashReceiptVoucher.CashReceiptPaymentMethodId == 2)
                {
                    cashReceiptVoucher.CashBoxId = null;
                    cashReceiptVoucher.ChartOfAccountId = null;
                }
                else if (cashReceiptVoucher.CashReceiptPaymentMethodId == 3)
                {
                    cashReceiptVoucher.CashBoxId = null;
                    cashReceiptVoucher.ChartOfAccountId = null;
                }
                else if (cashReceiptVoucher.CashReceiptPaymentMethodId == 4)
                {
                    cashReceiptVoucher.CashBoxId = null;
                    cashReceiptVoucher.BankAccountId = null;
                }
                

                if (cashReceiptVoucher.Id > 0)
                {
                    if (db.CashReceiptVouchers.Find(cashReceiptVoucher.Id).IsPosted == true)
                    {
                        return Content("false");
                    }
                    try
                    {

                   
                    cashReceiptVoucher.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    MyXML.xPathName = "SalesInvoiceActualPayment";
                    var SalesInvoiceActualPaymentXml = MyXML.GetXML(cashReceiptVoucher.SalesInvoiceActualPayments);
                    MyXML.xPathName = "CashReceiptVoucherPropertyContractBatches";
                    var CashReceiptVoucherPropertyContractBatches = MyXML.GetXML(cashReceiptVoucher.CashReceiptVoucherPropertyContractBatches);
                    db.CashReceiptVoucher_Update(
                        cashReceiptVoucher.Id, 
                        cashReceiptVoucher.DocumentNumber, 
                        cashReceiptVoucher.BranchId, 
                        cashReceiptVoucher.MoneyAmount, 
                        cashReceiptVoucher.SourceTypeId, 
                        cashReceiptVoucher.Date, 
                        cashReceiptVoucher.CurrencyId, 
                        cashReceiptVoucher.AccountId, 
                        cashReceiptVoucher.IsLinked, cashReceiptVoucher.IsPosted, cashReceiptVoucher.IsActive, cashReceiptVoucher.IsDeleted, 
                        cashReceiptVoucher.UserId, 
                        cashReceiptVoucher.Notes, cashReceiptVoucher.Image, 
                        cashReceiptVoucher.CustomerId, cashReceiptVoucher.VendorId, cashReceiptVoucher.EmployeeId, cashReceiptVoucher.TechnicianId, 
                        cashReceiptVoucher.CurrencyEquivalent, 
                        cashReceiptVoucher.DepartmentId, cashReceiptVoucher.CashBoxId, cashReceiptVoucher.DirectRevenueId, cashReceiptVoucher.ShareholderId, cashReceiptVoucher.CostCenterId, 
                        cashReceiptVoucher.IsInvoiceSelected, 
                        SalesInvoiceActualPaymentXml, 
                        cashReceiptVoucher.BankAccountId, 
                        cashReceiptVoucher.TransactionNo, cashReceiptVoucher.TransactionDate, 
                        cashReceiptVoucher.ChartOfAccountId, 
                        cashReceiptVoucher.CashReceiptPaymentMethodId, cashReceiptVoucher.VendorReceiptNumber, cashReceiptVoucher.Month, cashReceiptVoucher.Year, 
                        cashReceiptVoucher.ChildrenId, cashReceiptVoucher.ElderId, 
                        cashReceiptVoucher.PropertyContractId,  
                        cashReceiptVoucher.ElectricityBillValue,
                        cashReceiptVoucher.GasBillValue,
                        cashReceiptVoucher.ViolationBillValue,
                        CashReceiptVoucherPropertyContractBatches, cashReceiptVoucher.PropertyContractTerminationId);
                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("CashReceiptVoucher", "Edit", "AddEdit", id, null, " سند القبض");
                    ////////////////-----------------------------------------------------------------------
                    }
                    catch(Exception ex)
                    {
                        var r = ex;
                    }
                
                
                
                }
                else
                {
                    cashReceiptVoucher.IsActive = true;
                    cashReceiptVoucher.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    var idResult = new ObjectParameter("Id", typeof(Int32));
                    MyXML.xPathName = "SalesInvoiceActualPayment";
                    var SalesInvoiceActualPaymentXml = MyXML.GetXML(cashReceiptVoucher.SalesInvoiceActualPayments);
                    MyXML.xPathName = "CashReceiptVoucherPropertyContractBatches";
                    try
                    {
                        var CashReceiptVoucherPropertyContractBatches = MyXML.GetXML(cashReceiptVoucher.CashReceiptVoucherPropertyContractBatches);
                        db.CashReceiptVoucher_Insert(
                            idResult, 
                            cashReceiptVoucher.BranchId, 
                            cashReceiptVoucher.MoneyAmount, 
                            cashReceiptVoucher.SourceTypeId, 
                            cashReceiptVoucher.Date, 
                            cashReceiptVoucher.CurrencyId, cashReceiptVoucher.AccountId, 
                            cashReceiptVoucher.IsLinked, false, cashReceiptVoucher.IsActive, cashReceiptVoucher.IsDeleted, 
                            cashReceiptVoucher.UserId, 
                            cashReceiptVoucher.Notes, cashReceiptVoucher.Image, 
                            cashReceiptVoucher.CustomerId, cashReceiptVoucher.VendorId, cashReceiptVoucher.EmployeeId, cashReceiptVoucher.TechnicianId, 
                            cashReceiptVoucher.CurrencyEquivalent, 
                            cashReceiptVoucher.DepartmentId, cashReceiptVoucher.CashBoxId, cashReceiptVoucher.DirectRevenueId, cashReceiptVoucher.ShareholderId, cashReceiptVoucher.CostCenterId, 
                            cashReceiptVoucher.IsInvoiceSelected, 
                            SalesInvoiceActualPaymentXml, 
                            cashReceiptVoucher.BankAccountId, 
                            cashReceiptVoucher.TransactionNo, cashReceiptVoucher.TransactionDate, 
                            cashReceiptVoucher.ChartOfAccountId, 
                            cashReceiptVoucher.CashReceiptPaymentMethodId, 
                            DocumentCoding, 
                            cashReceiptVoucher.VendorReceiptNumber, 
                            cashReceiptVoucher.Month, cashReceiptVoucher.Year, 
                            cashReceiptVoucher.ChildrenId, cashReceiptVoucher.ElderId, 
                            cashReceiptVoucher.PropertyContractId,
                            

    // ✅✅✅ CRITICAL: إضافة PropertyContractTerminationId هنا
    cashReceiptVoucher.PropertyContractTerminationId,

    cashReceiptVoucher.ElectricityBillValue,
    cashReceiptVoucher.GasBillValue,
    cashReceiptVoucher.ViolationBillValue,
    CashReceiptVoucherPropertyContractBatches
                            );
                    }
                    catch (Exception ex)
                    {
                        var t = ex.Message;
                        throw;
                    }
                    id = (int)idResult.Value;

                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("CashReceiptVoucher", "Add", "AddEdit", cashReceiptVoucher.Id, null, "سند القبض");
                }
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = cashReceiptVoucher.Id > 0 ? "تعديل سند قبض" : "اضافة سند قبض",
                    EnAction = "AddEdit",
                    ControllerName = "CashReceiptVoucher",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    CodeOrDocNo = cashReceiptVoucher.DocumentNumber
                });
                if (cashReceiptVoucher.CashReceiptVoucherPropertyContractBatches.Count() > 0)
                {
                    foreach (var item in cashReceiptVoucher.CashReceiptVoucherPropertyContractBatches)
                    {
                        var Batches = db.PropertyContractBatches.Where(a => a.IsDeleted == false && a.Id == item.PropertyContractBatchId).FirstOrDefault();
                        if (Batches != null)
                        {
                            Batches.IsDelivered = item.IsDelivered;
                            db.Entry(Batches).State = EntityState.Modified;
                        }

                    }
                    db.SaveChanges();
                }
                return Json(new { success = "true", id });
            }
            else
            {
                var errors = ModelState
                    .Where(x => x.Value.Errors.Count > 0)
                    .Select(x => new { x.Key, x.Value.Errors })
                    .ToArray();
                DepartmentRepository departmentRepository = new DepartmentRepository(db);
                CashboxReposistory cashboxReposistory = new CashboxReposistory(db);
                ViewBag.TechnicianId = new SelectList(db.Techanicians.Where(b => b.IsDeleted == false && b.IsActive == true).Select(b => new
                {
                    b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName", cashReceiptVoucher.TechnicianId);

                ViewBag.BranchId = new SelectList(db.Branches.Where(b => b.IsDeleted == false && b.IsActive == true).Select(b => new
                {
                    b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName", cashReceiptVoucher.BranchId);
                ViewBag.AccountId = new SelectList(db.ChartOfAccounts.Where(c => c.IsDeleted == false && c.IsActive == true && c.ClassificationId == 3).Select(b => new
                {
                    b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName", cashReceiptVoucher.AccountId);
                ViewBag.CurrencyId = new SelectList(db.Currencies.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
                {
                    b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName", cashReceiptVoucher.CurrencyId);
                ViewBag.SourceTypeId = new SelectList(db.CashReceiptSourceTypes.Where(c => c.IsDeleted == false && c.IsActive == true && c.Id != 5), "Id", "ArName", cashReceiptVoucher.SourceTypeId);
                ViewBag.VendorId = new SelectList(db.Vendors.Where(v => v.IsDeleted == false && v.IsActive == true).Select(b => new
                {
                    b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName", cashReceiptVoucher.VendorId);
                ViewBag.ShareholderId = new SelectList(db.ShareHolders.Where(v => v.IsDeleted == false && v.IsActive == true).Select(b => new
                {
                    b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName", cashReceiptVoucher.ShareholderId);
                ViewBag.CustomerId = new SelectList(db.Customers.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
                {
                    b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName", cashReceiptVoucher.CustomerId);
                ViewBag.EmployeeId = new SelectList(db.Employees.Where(e => e.IsDeleted == false && e.IsActive == true).Select(b => new
                {
                    b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName", cashReceiptVoucher.EmployeeId);

                ViewBag.DirectRevenueId = new SelectList(db.DirectRevenues.Where(d => d.IsDeleted == false && d.IsActive == true).Select(b => new
                {
                    b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName", cashReceiptVoucher.DirectRevenueId);
                ViewBag.DepartmentId = new SelectList(await departmentRepository.UserDepartments(userId).ToListAsync(), "Id", "ArName", cashReceiptVoucher.DepartmentId);
                ViewBag.CashBoxId = new SelectList(await cashboxReposistory.UserCashboxes(userId, cashReceiptVoucher.DepartmentId).ToListAsync(), "Id", "ArName", cashReceiptVoucher.CashBoxId);
                ViewBag.ChartOfAccountId = new SelectList(db.ChartOfAccounts.Where(c => c.IsDeleted == false && c.IsActive == true && c.ClassificationId == 3).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName", cashReceiptVoucher.ChartOfAccountId);

                ViewBag.BankAccountId = new SelectList(db.BankAccounts.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.AccountNumber
                }), "Id", "ArName", cashReceiptVoucher.BankAccountId);

                ViewBag.CashReceiptPaymentMethodId = new SelectList(db.CashReceiptPaymentMethods.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName", cashReceiptVoucher.CashReceiptPaymentMethodId);
                ViewBag.ChildrenId = new SelectList(db.Children.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName", cashReceiptVoucher.ChildrenId);
                ViewBag.ElderId = new SelectList(db.Elders.Where(c => c.IsDeleted == false && c.IsActive == true).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName", cashReceiptVoucher.ElderId);
                //Month
                ViewBag.Month = new SelectList(new List<dynamic> {
                 new { id=1,name="يناير"},
                new { id=2,name="فبراير"},
                new { id=3,name="مارس"},
                new { id=4,name="إبريل"},
                new { id=5,name="مايو"},
                new { id=6,name="يونيه"},
                new { id=7,name="يوليو"},
                new { id=8,name="اغسطس"},
                new { id=9,name="سبتمبر"},
                new { id=10,name="اكتوبر"},
                new { id=11,name="نوفمبر"},
                new { id=12,name="ديسمبر"}}, "id", "name", cashReceiptVoucher.Month);

                //year
                List<int> year = new List<int>();
                for (var i = 2019; i <= 2030; i++)
                {
                    year.Add(i);
                    ViewBag.Year = new SelectList(year, cashReceiptVoucher.Year);
                }
                return View(cashReceiptVoucher);
                //var errors = ModelState
                //    .Where(x => x.Value.Errors.Count > 0)
                //    .Select(x => new { x.Key, x.Value.Errors })
                //    .ToArray();

                //return Json(new { success = "false", errors });
            }

        }

        // POST: ReceiptAndPaymentVoucher/Delete/5
        [HttpPost, ActionName("Delete")]
        //[ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                CashReceiptVoucher receiptAndPaymentVoucher = db.CashReceiptVouchers.Find(id);
                if (receiptAndPaymentVoucher.IsPosted == true)
                {
                    return Content("false");
                }
                db.CashReceiptVoucher_Delete(id, userId);
                if (receiptAndPaymentVoucher.CashReceiptVoucherPropertyContractBatches.Count() > 0)
                {
                    foreach (var item in receiptAndPaymentVoucher.CashReceiptVoucherPropertyContractBatches)
                    {
                        var Batches = db.PropertyContractBatches.Where(a => a.IsDeleted == false && a.Id == item.PropertyContractBatchId).FirstOrDefault();
                        if (Batches != null)
                        {
                            Batches.IsDelivered = false;
                            db.Entry(Batches).State = EntityState.Modified;
                        }
                    }
                    db.SaveChanges();
                }
                //receiptAndPaymentVoucher.IsDeleted = true;
                //receiptAndPaymentVoucher.UserId = userId;

                //db.Entry(receiptAndPaymentVoucher).State = EntityState.Modified;
                //db.Database.ExecuteSqlCommand($"update JournalEntry set IsDeleted = 1, UserId={userId} where SourcePageId = (select Id from SystemPage where TableName = 'CashReceiptVoucher') and SourceId = {id}; update JournalEntryDetail set IsDeleted = 1 where JournalEntryId = (select Id from JournalEntry where SourcePageId = (select Id from SystemPage where TableName = 'CashReceiptVoucher') and SourceId = {id})");
                //db.SaveChanges();

                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "حذف سند قبض",
                    EnAction = "AddEdit",
                    ControllerName = "CashReceiptVoucher",
                    UserName = User.Identity.Name,
                    UserId = userId,
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = receiptAndPaymentVoucher.Id,
                    CodeOrDocNo = receiptAndPaymentVoucher.DocumentNumber
                });
                ////-------------------- Notification-------------------------////
                Notification.GetNotification("CashReceiptVoucher", "Delete", "Delete", id, null, "سند القبض");

                ///////////////-----------------------------------------------------------------------

                return Content("true");
            }
            catch (Exception)
            {
                throw;
            }

        }

        public ActionResult OpenReport(string save)
        {
            ViewBag.SaveType = save;
            var lastId = QueryHelper.GetLast("CashReceiptVoucher");
            ViewBag.Id = lastId;
            ViewBag.ControllerName = "CashReceiptVoucher";
            ViewBag.ReportName = "CashReceiptVoucherDetails";
            SystemSetting sysObj = db.SystemSettings.FirstOrDefault();


            return View("OpenReport");
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
            var lastObj = db.CashReceiptVouchers.Where(a => a.IsActive == true && a.IsDeleted == false && a.DepartmentId == id).OrderByDescending(a => a.Id).FirstOrDefault();
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
                    lastObj = db.CashReceiptVouchers.Where(a => a.IsActive == true && a.IsDeleted == false && a.DepartmentId == id && a.Date.Month == VoucherDate.Value.Month && a.Date.Year == VoucherDate.Value.Year).OrderByDescending(a => a.Id).FirstOrDefault();
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
                    lastObj = db.CashReceiptVouchers.Where(a => a.IsActive == true && a.IsDeleted == false && a.DepartmentId == id && (a.Date.Year == VoucherDate.Value.Year)).OrderByDescending(a => a.Id).FirstOrDefault();

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
            ////var docNo = QueryHelper.DocLastNum(id, "CashReceiptVoucher");
            ////double i = (docNo) + 1;
            ////return Json(i, JsonRequestBehavior.AllowGet);
            ////-------------------------- Old ------------------------//
            //SystemSetting systemSetting = db.SystemSettings.Any() ? db.SystemSettings.FirstOrDefault() : new SystemSetting();
            //var NoOfDigits = systemSetting.NoOfDigits /*== null ? i.ToString().Length : systemSetting.NoOfDigits*/;
            //DateTime utcNow = date; //DateTime.UtcNow;
            //TimeZoneInfo info = TimeZoneInfo.FindSystemTimeZoneById("Egypt Standard Time");
            //DateTime cTime = TimeZoneInfo.ConvertTimeFromUtc(utcNow, info);
            //var Year = int.Parse(cTime.Year.ToString().Remove(0, 2));
            //var CompleteYear = int.Parse(cTime.Year.ToString());
            //var Month = cTime.Month;
            //Month = Month < 10 ? int.Parse("0" + Month) : Month;
            //var last = db.CashReceiptVouchers.Where(a => a.IsActive == true && a.IsDeleted == false && a.DepartmentId == id  && a.Date.Year == CompleteYear && a.Date.Month == Month).OrderByDescending(a => a.Id).FirstOrDefault();  
            //double docNo = 0;
            //if (last != null)
            //{
            //    var cc = last.DocumentNumber.Length;
            //    if (last.DocumentNumber.Length > NoOfDigits)
            //    {
            //        docNo = double.Parse(last.DocumentNumber.Substring(last.DocumentNumber.Length - (int)NoOfDigits));
            //    }
            //    else
            //    {
            //        docNo = double.Parse(last.DocumentNumber);
            //    }
            //}
            //double i = (docNo) + 1;
            ////--------------- Document Coding --------------//
            //var DocumentCoding = "";
            //if (systemSetting.DocumentCoding == true)
            //{
            //    var FixedPart = "";
            //    var Separator = "";
            //    if (systemSetting.IsFixedPart == true)
            //    {
            //        FixedPart = systemSetting.FixedPart;
            //    }
            //    if (systemSetting.IsSeparator == true)
            //    {
            //        Separator = systemSetting.Separator;
            //    }
            //    var DepartmentNo = db.Departments.Where(a => a.IsActive == true && a.IsDeleted == false && a.Id == id).FirstOrDefault().Code;
            //    DepartmentNo = int.Parse(DepartmentNo) < 10 ? "0" + DepartmentNo : DepartmentNo;
            //    //var RepNo = "00";
            //    var DocNo = i.ToString();
            //    var diff = NoOfDigits - DocNo.Length; 
            //    var ii = i.ToString();
            //    if (diff > 0)
            //    {
            //        for (var a = 0; a < diff; a++)
            //        {
            //            DocNo = DocNo.Insert(0, "0");
            //            ii = DocNo;
            //        }
            //    }
            //    DocNo = DocNo.Substring(DocNo.Length - (int)NoOfDigits);
            //    DocumentCoding = FixedPart + DepartmentNo + Separator + Year + Separator + Month /*+ Separator + RepNo */+ Separator + DocNo;
            //    //i = double.Parse(DocumentCoding);
            //    return Json(DocumentCoding, JsonRequestBehavior.AllowGet);
            // }
            //-------------------- End Of Document Coding ------------------//
            // return Json(i, JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public ActionResult ApproveNotApprove(int id)
        {
            try
            {
                CashReceiptVoucher cashReceiptVoucher = db.CashReceiptVouchers.Find(id);
                if (cashReceiptVoucher.IsActive == true)
                {
                    cashReceiptVoucher.IsActive = false;
                }
                else
                {
                    cashReceiptVoucher.IsActive = true;
                }
                cashReceiptVoucher.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                db.Entry(cashReceiptVoucher).State = EntityState.Modified;

                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = (bool)cashReceiptVoucher.IsActive ? "إعتماد سند القبض" : "إلغاء إعتماد سند القبض",
                    EnAction = "AddEdit",
                    ControllerName = "CashReceiptVoucher",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = cashReceiptVoucher.Id,
                    CodeOrDocNo = cashReceiptVoucher.DocumentNumber
                });
                if (cashReceiptVoucher.IsActive == true)
                {
                    Notification.GetNotification("CashReceiptVoucher", "Approve", "Approve", id, true, "سند القبض");
                }
                else
                {

                    Notification.GetNotification("CashReceiptVoucher", "Approve", "Approve", id, false, " سند القبض");
                }

                return Content("true");
            }
            catch (Exception ex)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
        }

        [SkipERPAuthorize]
        public JsonResult GetMonthlySubscription(int? Id, string Type/*, int? Month, int? Year*/)
        {
            decimal? Amount;
            if (Type == "Elder")
            {
                Amount = db.Elders.Where(a => a.IsActive == true && a.IsDeleted == false /*&& a.SubscriptionStartDate.Value.Month == Month && a.SubscriptionStartDate.Value.Year == Year*/ && a.Id == Id).Sum(a => a.MonthlySubscription);
            }
            else
            {
                Amount = db.Children.Where(a => a.IsActive == true && a.IsDeleted == false /*&& a.SubscriptionStartDate.Value.Month == Month && a.SubscriptionStartDate.Value.Year == Year*/ && a.Id == Id).Sum(a => a.MonthlySubscription);
            }
            Amount = Amount > 0 ? Amount : 0;
            return Json(Amount, JsonRequestBehavior.AllowGet);
        }

        public class ContractBatchModel
        {
            public int PropertyContractBatchId { get; set; }
            public int? BatchNo { get; set; }
            public decimal? BatchTotal { get; set; }
            public DateTime? BatchDate { get; set; }
            public bool? IsDelivered { get; set; }
            public string PropertyArName { get; set; }
            public string PropertyUnitArName { get; set; }
            public decimal? BatchRentValue { get; set; }
            public decimal? BatchRentValueTaxes { get; set; }
            public decimal? BatchElectricityValue { get; set; }
            public decimal? BatchElectricityValueTaxes { get; set; }
            public decimal? BatchWaterValue { get; set; }
            public decimal? BatchWaterValueTaxes { get; set; }
            public decimal? BatchCommissionValue { get; set; }
            public decimal? BatchCommissionValueTaxes { get; set; }
            public decimal? BatchGasValue { get; set; }
            public decimal? BatchGasValueTaxes { get; set; }
            public decimal? BatchInsuranceValue { get; set; }
            public decimal? BatchInsuranceValueTaxes { get; set; }
            public decimal? BatchServicesValue { get; set; }
            public decimal? BatchServicesValueTaxes { get; set; }
            public decimal Paid { get; set; }
            public decimal Remain { get; set; }
            public decimal TotalPaid { get; set; }
        }

        [SkipERPAuthorize]
        public JsonResult GetContractBatches(int? PropertyContractId   , int vid)
        {
            //TotalPaid
            var bathcespaid = (from bt in db.CashReceiptVoucherPropertyContractBatches
                where  bt.PropertyContractBatch.PropertyContract.Id == PropertyContractId
                select new { bt.PropertyContractBatchId, bt.Paid }).


            //    .new BatchPaidModel
            //{
            //    Id = t.Key ?? 0,
            //    Paid = t.Sum(n => n.Paid ?? 0)
            //}
            GroupBy(t => t.PropertyContractBatchId).Select(t => new BatchPaidModel
                {
                    Id = t.Key ?? 0,
                    Paid = t.Sum(n => n.Paid ?? 0)
                }).ToList();

            var bathces = (from bt in db.CashReceiptVoucherPropertyContractBatches
                where  bt.CashReceiptVoucherId == vid
                           select bt ).ToList();

                      var ContractBatches = (from a in db.PropertyContractBatches//.Where(t => t.IsDeleted == false && t.MainDocId == PropertyContractId)
                               join bet in  db.PropertyDetails on a.PropertyContract.PropertyUnitId equals  bet.Id into grpuntits 
                               let first = a.CashReceiptVoucherPropertyContractBatches.FirstOrDefault(c => c.PropertyContractBatchId == a.Id)
                               let paid  = first==null ? 0 : first.Paid 
          // let  remain = first == null ? 0 : first.Remain 
             join PropertyContractBatch in db.CashReceiptVoucherPropertyContractBatches on 
                                       first.PropertyContractBatchId equals PropertyContractBatch.PropertyContractBatchId into grp
                                     
           let totalb = grp.Sum(t => t.Paid )

                               where a.IsDeleted == false && a.MainDocId == PropertyContractId
                                   select new ContractBatchModel
                                   {
                                       PropertyContractBatchId = a.Id, BatchNo = a.BatchNo, BatchTotal = a.BatchTotal,
                                       BatchDate = a.BatchDate,
                                       IsDelivered = a.IsDelivered,
                                       PropertyArName = a.PropertyContract.Property.ArName,
                                       PropertyUnitArName = grpuntits.FirstOrDefault() != null ? grpuntits.FirstOrDefault().PropertyUnitNo : string.Empty,
                                       BatchRentValue = a.BatchRentValue,
                                       BatchRentValueTaxes = a.BatchRentValueTaxes,
                                       BatchElectricityValue = a.BatchElectricityValue,
                                       BatchElectricityValueTaxes = a.BatchElectricityValueTaxes,
                                       BatchWaterValue = a.BatchWaterValue,
                                       BatchWaterValueTaxes = a.BatchWaterValueTaxes,
                                       BatchCommissionValue = a.BatchCommissionValue,
                                       BatchCommissionValueTaxes = a.BatchCommissionValueTaxes,
                                       BatchGasValue = a.BatchGasValue,
                                       BatchGasValueTaxes = a.BatchGasValueTaxes,
                                       BatchInsuranceValue = a.BatchInsuranceValue,
                                       BatchInsuranceValueTaxes = a.BatchInsuranceValueTaxes,
                                       BatchServicesValue = a.BatchServicesValue,
                                       BatchServicesValueTaxes = a.BatchServicesValueTaxes,
                                       Paid = paid??0.0m,
                                       Remain = (a.BatchTotal??0) - ( (paid ?? 0 )+ (totalb ?? 0) ),
                                       TotalPaid = totalb ??0
                                   }).ToList();

            foreach (var batch in ContractBatches)
            {
                var vbatch = bathces.FirstOrDefault(t => t.PropertyContractBatchId == batch.PropertyContractBatchId);
                var btchsum = bathcespaid.FirstOrDefault(t => t.Id == batch.PropertyContractBatchId);
                if (vbatch != null)
                {
                    batch.Paid = vbatch.Paid ?? 0;
                    batch.Remain = (batch.BatchTotal??0m) - (batch.Paid);
                }
                else
                {
                    batch.Paid = 0;
                    batch.TotalPaid = btchsum?.Paid??0;
                    batch.Remain = (batch.BatchTotal ?? 0m) - (batch.Paid + btchsum?.Paid ?? 0m);

                }
            }
            return Json(ContractBatches, JsonRequestBehavior.AllowGet);
        }

        private List<JournalEntryDetail> SwapJournalEntry(int billId, int JEId, int sourcePageId)
        {
            var JEDetails = db.JournalEntryDetails.Where(a => a.SourceId == billId && a.JournalEntryId == JEId && a.SourcePageId == sourcePageId).OrderBy(a => a.Id).ToList();
            var cashReceiptJEDetails = new List<JournalEntryDetail>();
            foreach(var detail in JEDetails)
            {
                var CRJEDetail = new JournalEntryDetail();
                CRJEDetail = detail;
                var debitOld = detail.Debit;
                var ceditOld = detail.Credit;
                CRJEDetail.Credit = debitOld;
                CRJEDetail.Debit = ceditOld;
                cashReceiptJEDetails.Add(CRJEDetail);
            }
            return cashReceiptJEDetails;

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
