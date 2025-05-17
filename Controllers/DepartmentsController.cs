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

namespace MyERP.Controllers
{
    public class DepartmentsController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();

        // GET: Departments
        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "")
        {
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "قائمة الفروع",
                EnAction = "Index",
                ControllerName = "Departments",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            ////-------------------- Notification-------------------------////
            Notification.GetNotification("Departments", "View", "Index", null, null, "الفروع");
            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;

            if (pageIndex > 1)
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;

            IQueryable<Department> departments;

            if (string.IsNullOrEmpty(searchWord))
            {
                departments = db.Departments.Where(s => s.IsDeleted == false).OrderBy(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.Departments.Where(s => s.IsDeleted == false).Count();
            }
            else
            {
                departments = db.Departments.Where(s => s.IsDeleted == false && (s.Code.Contains(searchWord) || s.ArName.Contains(searchWord) || s.EnName.Contains(searchWord) || s.ERPUser.Name.Contains(searchWord) || s.Notes.Contains(searchWord))).OrderBy(s => s.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                ViewBag.Count = db.Departments.Where(s => s.IsDeleted == false && (s.Code.Contains(searchWord) || s.ArName.Contains(searchWord) || s.EnName.Contains(searchWord) || s.ERPUser.Name.Contains(searchWord) || s.Notes.Contains(searchWord))).Count();

            }
            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            return View(departments.ToList());

        }

        // GET: Departments/Edit/5
        public async Task<ActionResult> AddEdit(int? id, bool? repeat)
        {
            var session = Session["lang"] != null ? Session["lang"].ToString() : "ar";
            var subAccounts = await db.ChartOfAccounts.Where(c => c.IsDeleted == false && c.IsActive == true && (c.ClassificationId == 3)).Select(b => new
            {
                b.Id,
                ArName = session.ToString() == "en" && b.EnName != null ? b.Code + " - " + b.EnName : b.Code + " - " + b.ArName
            }).ToListAsync();
            var generalAccounts = await db.ChartOfAccounts.Where(c => c.IsDeleted == false && c.IsActive == true && (c.ClassificationId == 2 || c.ClassificationId == 3)).Select(b => new
            {
                b.Id,
                ArName = session.ToString() == "en" && b.EnName != null ? b.Code + " - " + b.EnName : b.Code + " - " + b.ArName
            }).ToListAsync();
            var basicAccounts = await db.ChartOfAccounts.Where(c => c.IsDeleted == false && c.IsActive == true && c.ClassificationId == 1).Select(b => new
            {
                b.Id,
                ArName = session.ToString() == "en" && b.EnName != null ? b.Code + " - " + b.EnName : b.Code + " - " + b.ArName
            }).ToListAsync();
            // لو إدارة املاك هاظهر الحسابات بتاعتها 
            ViewBag.IsPropertyManagement = db.AllowedModules.Where(a => a.IsSelected == true && a.SystemPageId == 10429).FirstOrDefault() != null ? true : false;
            // لو الإدارة المالية هاظهر الحسابات بتاعتها 
            // فيها اعدادات المركز المالى و قائمة الدخل
            ViewBag.IsGeneralAccounts = db.AllowedModules.Where(a => a.IsSelected == true && a.SystemPageId == 3157).FirstOrDefault() != null ? true : false;


            if (id == null)
            {
                //ViewBag.BankAccountId = new SelectList(db.BankAccounts.Where(c => c.IsDeleted == false && c.IsActive == true), "Id", "AccountNumber");
                ViewBag.AcquiredDeductionAccountId = new SelectList(subAccounts, "Id", "ArName");
                ViewBag.DiscountAllowedAccountId = new SelectList(subAccounts, "Id", "ArName");
                ViewBag.InventoryAccountId = new SelectList(subAccounts, "Id", "ArName");
                ViewBag.FixedAssetsDepreciationExpensesAccountId = new SelectList(subAccounts, "Id", "ArName");
                ViewBag.SalesAccountId = new SelectList(subAccounts, "Id", "ArName");
                ViewBag.CostOfSalesAccountId = new SelectList(subAccounts, "Id", "ArName");
                ViewBag.GeneralExpensesAccountId = new SelectList(subAccounts, "Id", "ArName");
                ViewBag.TaxAccountId = new SelectList(subAccounts, "Id", "ArName");
                ViewBag.CommercialRevenueTaxAccountId = new SelectList(subAccounts, "Id", "ArName");
                ViewBag.ShareHolderAccountId = new SelectList(generalAccounts, "Id", "ArName");
                ViewBag.CurrentShareHolderAccountId = new SelectList(generalAccounts, "Id", "ArName");
                ViewBag.FixedAssetsAccountId = new SelectList(generalAccounts, "Id", "ArName");
                ViewBag.CurrentAssetsAccountId = new SelectList(generalAccounts, "Id", "ArName");
                ViewBag.CustomersAccountId = new SelectList(subAccounts, "Id", "ArName");
                ViewBag.VendorsAccountId = new SelectList(subAccounts, "Id", "ArName");
                ViewBag.ExpensesAccountId = new SelectList(basicAccounts, "Id", "ArName");
                ViewBag.RevenueAccountId = new SelectList(basicAccounts, "Id", "ArName");
                ViewBag.InstallmentRevenueAccountId = new SelectList(subAccounts, "Id", "ArName");
                ViewBag.VisaAccountId = new SelectList(subAccounts, "Id", "ArName");
                ViewBag.VisaCommissionAccountId = new SelectList(subAccounts, "Id", "ArName");
                ViewBag.DirectExpensesAccountId = new SelectList(subAccounts, "Id", "ArName");
                ViewBag.AccruedSalariesAccountId = new SelectList(subAccounts, "Id", "ArName");
                //EmpVacationAccumulatedAccountId
                ViewBag.EmpVacationAccumulatedAccountId = new SelectList(subAccounts, "Id", "ArName");
                //TravelingTicketsAccumulatedAccountId
                ViewBag.TravelingTicketsAccumulatedAccountId = new SelectList(subAccounts, "Id", "ArName");
                //EndOfServiceAccumulatedAccountId
                ViewBag.EndOfServiceAccumulatedAccountId = new SelectList(subAccounts, "Id", "ArName");
                //PaidAllowancesAccumulatedAccountId
                ViewBag.PaidAllowancesAccumulatedAccountId = new SelectList(subAccounts, "Id", "ArName");
                //EmpVacationExpensesAccountId
                ViewBag.EmpVacationExpensesAccountId = new SelectList(subAccounts, "Id", "ArName");
                //TravelingTicketsExpensesAccountId
                ViewBag.TravelingTicketsExpensesAccountId = new SelectList(subAccounts, "Id", "ArName");
                //EndOfServiceExpensesAccountId
                ViewBag.EndOfServiceExpensesAccountId = new SelectList(subAccounts, "Id", "ArName");
                //PaidAllowancesExpensesAccountId
                ViewBag.PaidAllowancesExpensesAccountId = new SelectList(subAccounts, "Id", "ArName");
                //SalesReturnsAccountId
                ViewBag.SalesReturnsAccountId = new SelectList(subAccounts, "Id", "ArName");
                //ReservationPaymentAccountId
                ViewBag.ReservationPaymentAccountId = new SelectList(subAccounts, "Id", "ArName");
                //DepartmentCurrentAccountId 
                ViewBag.DepartmentCurrentAccountId = new SelectList(subAccounts, "Id", "ArName");
                //DepartmentsInventoryTransferAccountId
                ViewBag.DepartmentsInventoryTransferAccountId = new SelectList(subAccounts, "Id", "ArName");
                //EmployeeReceivableAccountId
                ViewBag.EmployeeReceivableAccountId = new SelectList(subAccounts, "Id", "ArName");
                //DueSalariesAccountId
                ViewBag.DueSalariesAccountId = new SelectList(subAccounts, "Id", "ArName");
                //RefundableInsuranceAccountId
                ViewBag.RefundableInsuranceAccountId = new SelectList(subAccounts, "Id", "ArName");
                ViewBag.InventoryVarianceAccountId = new SelectList(subAccounts, "Id", "ArName");
                ViewBag.LostAndDamagedAccountId = new SelectList(subAccounts, "Id", "ArName");
                ViewBag.GiftsAndSamplesAccountId = new SelectList(subAccounts, "Id", "ArName");
                ViewBag.HotelRevenueAccountId = new SelectList(subAccounts, "Id", "ArName");
                ViewBag.SmokingTaxAccountId = new SelectList(subAccounts, "Id", "ArName");
                ViewBag.ServiceRevenueAccountId = new SelectList(subAccounts, "Id", "ArName");
                ViewBag.ChildSubscriptionAccountId = new SelectList(subAccounts, "Id", "ArName");
                ViewBag.ElderHostingAccountId = new SelectList(subAccounts, "Id", "ArName");
                ViewBag.DebitValueAddedTaxesAccountId = new SelectList(subAccounts, "Id", "ArName");
                ViewBag.CreditValueAddedTaxesAccountId = new SelectList(subAccounts, "Id", "ArName");
                ViewBag.GasDueRevenueId = new SelectList(subAccounts, "Id", "ArName");
                ViewBag.GasRevenueId = new SelectList(subAccounts, "Id", "ArName");


                ViewBag.CompanyId = new SelectList(db.Companies.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                ViewBag.ActivityId = new SelectList(db.Activities.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName");
                //-------------- Property Management Accounts -----------------------------------//
                ViewBag.QuestAndCommissionRevenueId = new SelectList(subAccounts, "Id", "ArName");
                ViewBag.WaterRevenueId = new SelectList(subAccounts, "Id", "ArName");
                ViewBag.ElectricityRevenueId = new SelectList(subAccounts, "Id", "ArName");
                ViewBag.ServicesRevenueId = new SelectList(subAccounts, "Id", "ArName");
                ViewBag.RentRevenueId = new SelectList(subAccounts, "Id", "ArName");
                ViewBag.RentsDueToOthersId = new SelectList(subAccounts, "Id", "ArName");
                ViewBag.MediatorARentsDueToOthersId = new SelectList(subAccounts, "Id", "ArName");
                ViewBag.CommissionDueFromOthersOwningId = new SelectList(subAccounts, "Id", "ArName");
                ViewBag.OwnerAccountId = new SelectList(subAccounts, "Id", "ArName");
                ViewBag.OwnerDueExpensesId = new SelectList(subAccounts, "Id", "ArName");
                ViewBag.RenterAndBuyerAccountId = new SelectList(subAccounts, "Id", "ArName");
                ViewBag.PropertyRefundInsuranceId = new SelectList(subAccounts, "Id", "ArName");
                ViewBag.DueRentId = new SelectList(subAccounts, "Id", "ArName");
                ViewBag.CompanyDueCommissionId = new SelectList(subAccounts, "Id", "ArName");
                ViewBag.WaterDueRevenueId = new SelectList(subAccounts, "Id", "ArName");
                ViewBag.ElectricityDueRevenueId = new SelectList(subAccounts, "Id", "ArName");
                ViewBag.ServicesDueRevenueId = new SelectList(subAccounts, "Id", "ArName");
                ViewBag.MediatorAInsuranceForOthersToAllRentersId = new SelectList(subAccounts, "Id", "ArName");
                ViewBag.BookingBatchesId = new SelectList(subAccounts, "Id", "ArName");
                ViewBag.ElectricityExpensesAndInvoicesId = new SelectList(subAccounts, "Id", "ArName");
                ViewBag.RepsCommissionAccountId = new SelectList(subAccounts, "Id", "ArName");
                ViewBag.CommissionAccountId = new SelectList(subAccounts, "Id", "ArName");
                ViewBag.PropertyExpensesAccountId = new SelectList(subAccounts, "Id", "ArName");
                ViewBag.RefundableInsuranceAccountForRentersId = new SelectList(subAccounts, "Id", "ArName");
                ViewBag.DueBatchesId = new SelectList(subAccounts, "Id", "ArName");
                ViewBag.ViolationsDueRevenueId = new SelectList(subAccounts, "Id", "ArName");
                ViewBag.ViolationsRevenueId = new SelectList(subAccounts, "Id", "ArName");
                //--------------------------------------------------------------------------------------------------------------------------------------//

                return View();
            }
            Department department = await db.Departments.FindAsync(id);

            if (department == null)
            {
                return HttpNotFound();
            }
            ViewBag.IsRepeat = repeat == true;

            //ViewBag.BankAccountId = new SelectList(db.BankAccounts.Where(c => c.IsDeleted == false && c.IsActive == true), "Id", "AccountNumber", department.BankAccountId);
            ViewBag.AcquiredDeductionAccountId = new SelectList(subAccounts, "Id", "ArName", department.AcquiredDeductionAccountId);
            ViewBag.DiscountAllowedAccountId = new SelectList(subAccounts, "Id", "ArName", department.DiscountAllowedAccountId);
            ViewBag.InventoryAccountId = new SelectList(subAccounts, "Id", "ArName", department.InventoryAccountId);
            ViewBag.FixedAssetsDepreciationExpensesAccountId = new SelectList(subAccounts, "Id", "ArName", department.FixedAssetsDepreciationExpensesAccountId);
            ViewBag.SalesAccountId = new SelectList(subAccounts, "Id", "ArName", department.SalesAccountId);
            ViewBag.CostOfSalesAccountId = new SelectList(subAccounts, "Id", "ArName", department.CostOfSalesAccountId);
            ViewBag.GeneralExpensesAccountId = new SelectList(subAccounts, "Id", "ArName", department.GeneralExpensesAccountId);
            ViewBag.TaxAccountId = new SelectList(subAccounts, "Id", "ArName", department.TaxAccountId);
            ViewBag.CommercialRevenueTaxAccountId = new SelectList(subAccounts, "Id", "ArName", department.CommercialRevenueTaxAccountId);
            ViewBag.ShareHolderAccountId = new SelectList(generalAccounts, "Id", "ArName", department.ShareHolderAccountId);
            ViewBag.CurrentShareHolderAccountId = new SelectList(generalAccounts, "Id", "ArName", department.CurrentShareHolderAccountId);
            ViewBag.FixedAssetsAccountId = new SelectList(generalAccounts, "Id", "ArName", department.FixedAssetsAccountId);
            ViewBag.CurrentAssetsAccountId = new SelectList(generalAccounts, "Id", "ArName", department.CurrentAssetsAccountId);
            ViewBag.CustomersAccountId = new SelectList(subAccounts, "Id", "ArName", department.CustomersAccountId);
            ViewBag.VendorsAccountId = new SelectList(subAccounts, "Id", "ArName", department.VendorsAccountId);
            ViewBag.ExpensesAccountId = new SelectList(basicAccounts, "Id", "ArName", department.ExpensesAccountId);
            ViewBag.RevenueAccountId = new SelectList(basicAccounts, "Id", "ArName", department.RevenueAccountId);
            ViewBag.InstallmentRevenueAccountId = new SelectList(subAccounts, "Id", "ArName", department.InstallmentRevenueAccountId);
            ViewBag.VisaAccountId = new SelectList(subAccounts, "Id", "ArName", department.VisaAccountId);
            ViewBag.VisaCommissionAccountId = new SelectList(subAccounts, "Id", "ArName", department.VisaCommissionAccountId);
            ViewBag.DirectExpensesAccountId = new SelectList(subAccounts, "Id", "ArName", department.DirectExpensesAccountId);
            ViewBag.AccruedSalariesAccountId = new SelectList(subAccounts, "Id", "ArName", department.AccruedSalariesAccountId);
            //SalesReturnsAccountId
            ViewBag.SalesReturnsAccountId = new SelectList(subAccounts, "Id", "ArName", department.SalesReturnsAccountId);
            //EmpVacationAccumulatedAccountId
            ViewBag.EmpVacationAccumulatedAccountId = new SelectList(subAccounts, "Id", "ArName", department.EmpVacationAccumulatedAccountId);
            //EmpVacationExpensesAccountId
            ViewBag.EmpVacationExpensesAccountId = new SelectList(subAccounts, "Id", "ArName", department.EmpVacationExpensesAccountId);
            //TravelingTicketsAccumulatedAccountId
            ViewBag.TravelingTicketsAccumulatedAccountId = new SelectList(subAccounts, "Id", "ArName", department.TravelingTicketsAccumulatedAccountId);
            //TravelingTicketsExpensesAccountId
            ViewBag.TravelingTicketsExpensesAccountId = new SelectList(subAccounts, "Id", "ArName", department.TravelingTicketsExpensesAccountId);
            //EndOfServiceAccumulatedAccountId
            ViewBag.EndOfServiceAccumulatedAccountId = new SelectList(subAccounts, "Id", "ArName", department.EndOfServiceAccumulatedAccountId);
            //EndOfServiceExpensesAccountId
            ViewBag.EndOfServiceExpensesAccountId = new SelectList(subAccounts, "Id", "ArName", department.EndOfServiceExpensesAccountId);
            //PaidAllowancesAccumulatedAccountId
            ViewBag.PaidAllowancesAccumulatedAccountId = new SelectList(subAccounts, "Id", "ArName", department.PaidAllowancesAccumulatedAccountId);
            //PaidAllowancesExpensesAccountId
            ViewBag.PaidAllowancesExpensesAccountId = new SelectList(subAccounts, "Id", "ArName", department.PaidAllowancesExpensesAccountId);
            //ReservationPaymentAccountId
            ViewBag.ReservationPaymentAccountId = new SelectList(subAccounts, "Id", "ArName", department.ReservationPaymentAccountId);
            //DepartmentCurrentAccountId 
            ViewBag.DepartmentCurrentAccountId = new SelectList(subAccounts, "Id", "ArName", department.DepartmentCurrentAccountId);
            //DepartmentsInventoryTransferAccountId
            ViewBag.DepartmentsInventoryTransferAccountId = new SelectList(subAccounts, "Id", "ArName", department.DepartmentsInventoryTransferAccountId);
            //EmployeeReceivableAccountId
            ViewBag.EmployeeReceivableAccountId = new SelectList(subAccounts, "Id", "ArName", department.EmployeeReceivableAccountId);
            //DueSalariesAccountId
            ViewBag.DueSalariesAccountId = new SelectList(subAccounts, "Id", "ArName", department.DueSalariesAccountId);
            //RefundableInsuranceAccountId
            ViewBag.RefundableInsuranceAccountId = new SelectList(subAccounts, "Id", "ArName", department.RefundableInsuranceAccountId);
            ViewBag.InventoryVarianceAccountId = new SelectList(subAccounts, "Id", "ArName", department.InventoryVarianceAccountId);
            ViewBag.LostAndDamagedAccountId = new SelectList(subAccounts, "Id", "ArName", department.LostAndDamagedAccountId);
            ViewBag.GiftsAndSamplesAccountId = new SelectList(subAccounts, "Id", "ArName", department.GiftsAndSamplesAccountId);
            ViewBag.HotelRevenueAccountId = new SelectList(subAccounts, "Id", "ArName", department.HotelRevenueAccountId);
            ViewBag.SmokingTaxAccountId = new SelectList(subAccounts, "Id", "ArName", department.SmokingTaxAccountId);
            ViewBag.ServiceRevenueAccountId = new SelectList(subAccounts, "Id", "ArName", department.ServiceRevenueAccountId);
            ViewBag.ChildSubscriptionAccountId = new SelectList(subAccounts, "Id", "ArName", department.ChildSubscriptionAccountId);
            ViewBag.ElderHostingAccountId = new SelectList(subAccounts, "Id", "ArName", department.ElderHostingAccountId);
            ViewBag.DebitValueAddedTaxesAccountId = new SelectList(subAccounts, "Id", "ArName", department.DebitValueAddedTaxesAccountId);
            ViewBag.CreditValueAddedTaxesAccountId = new SelectList(subAccounts, "Id", "ArName", department.CreditValueAddedTaxesAccountId);

            ViewBag.CompanyId = new SelectList(db.Companies.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", department.CompanyId);
            ViewBag.ActivityId = new SelectList(db.Activities.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", department.ActivityId);

            //-------------- Property Management Accounts -----------------------------------//
            ViewBag.QuestAndCommissionRevenueId = new SelectList(subAccounts, "Id", "ArName", department.QuestAndCommissionRevenueId);
            ViewBag.WaterRevenueId = new SelectList(subAccounts, "Id", "ArName", department.WaterRevenueId);
            ViewBag.ElectricityRevenueId = new SelectList(subAccounts, "Id", "ArName", department.ElectricityRevenueId);
            ViewBag.ServicesRevenueId = new SelectList(subAccounts, "Id", "ArName", department.ServicesRevenueId);
            ViewBag.RentRevenueId = new SelectList(subAccounts, "Id", "ArName", department.RentRevenueId);
            ViewBag.RentsDueToOthersId = new SelectList(subAccounts, "Id", "ArName", department.RentsDueToOthersId);
            ViewBag.MediatorARentsDueToOthersId = new SelectList(subAccounts, "Id", "ArName", department.MediatorARentsDueToOthersId);
            ViewBag.CommissionDueFromOthersOwningId = new SelectList(subAccounts, "Id", "ArName", department.CommissionDueFromOthersOwningId);
            ViewBag.OwnerAccountId = new SelectList(subAccounts, "Id", "ArName", department.OwnerAccountId);
            ViewBag.OwnerDueExpensesId = new SelectList(subAccounts, "Id", "ArName", department.OwnerDueExpensesId);
            ViewBag.RenterAndBuyerAccountId = new SelectList(subAccounts, "Id", "ArName", department.RenterAndBuyerAccountId);
            ViewBag.PropertyRefundInsuranceId = new SelectList(subAccounts, "Id", "ArName", department.PropertyRefundInsuranceId);
            ViewBag.DueRentId = new SelectList(subAccounts, "Id", "ArName", department.DueRentId);
            ViewBag.CompanyDueCommissionId = new SelectList(subAccounts, "Id", "ArName", department.CompanyDueCommissionId);
            ViewBag.WaterDueRevenueId = new SelectList(subAccounts, "Id", "ArName", department.WaterDueRevenueId);
            ViewBag.ElectricityDueRevenueId = new SelectList(subAccounts, "Id", "ArName", department.ElectricityDueRevenueId);
            ViewBag.ServicesDueRevenueId = new SelectList(subAccounts, "Id", "ArName", department.ServicesDueRevenueId);
            ViewBag.MediatorAInsuranceForOthersToAllRentersId = new SelectList(subAccounts, "Id", "ArName", department.MediatorAInsuranceForOthersToAllRentersId);
            ViewBag.BookingBatchesId = new SelectList(subAccounts, "Id", "ArName", department.BookingBatchesId);
            ViewBag.ElectricityExpensesAndInvoicesId = new SelectList(subAccounts, "Id", "ArName", department.ElectricityExpensesAndInvoicesId);
            ViewBag.RepsCommissionAccountId = new SelectList(subAccounts, "Id", "ArName", department.RepsCommissionAccountId);
            ViewBag.CommissionAccountId = new SelectList(subAccounts, "Id", "ArName", department.CommissionAccountId);
            ViewBag.PropertyExpensesAccountId = new SelectList(subAccounts, "Id", "ArName", department.PropertyExpensesAccountId);
            ViewBag.RefundableInsuranceAccountForRentersId = new SelectList(subAccounts, "Id", "ArName", department.RefundableInsuranceAccountForRentersId);
            ViewBag.DueBatchesId = new SelectList(subAccounts, "Id", "ArName", department.DueBatchesId);
            ViewBag.ViolationsDueRevenueId = new SelectList(subAccounts, "Id", "ArName", department.ViolationsDueRevenueId);
            ViewBag.ViolationsRevenueId = new SelectList(subAccounts, "Id", "ArName", department.ViolationsRevenueId);
            //--------------------------------------------------------------------------------------------------------------------------------------//
            ViewBag.GasDueRevenueId = new SelectList(subAccounts, "Id", "ArName", department.GasDueRevenueId);
            ViewBag.GasRevenueId = new SelectList(subAccounts, "Id", "ArName", department.GasRevenueId);

            ViewBag.Next = QueryHelper.Next((int)id, "Department");
            ViewBag.Previous = QueryHelper.Previous((int)id, "Department");
            ViewBag.Last = QueryHelper.GetLast("Department");
            ViewBag.First = QueryHelper.GetFirst("Department");
            if (repeat == true)
            {
                var dept = db.Departments.Find(id);
                dept.Id = 0;
                return View(dept);
            }
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "تفاصيل الفرع",
                EnAction = "AddEdit",
                ControllerName = "Departments",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET",
                SelectedItem = department.Id,
                ArItemName = department.ArName,
                EnItemName = department.EnName,
                CodeOrDocNo = department.Code
            });
            return View(department);

        }

        // POST: Departments/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        //[ValidateAntiForgeryToken]
        public ActionResult AddEdit(Department department, string newBtn)
        {
            var session = Session["lang"] != null ? Session["lang"].ToString() : "ar";
            if (ModelState.IsValid)
            {
                var id = department.Id;
                department.IsDeleted = false;
                department.PurchaseAccountId = department.InventoryAccountId;
                if (department.Id > 0)
                {
                    department.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                    db.Entry(department).State = EntityState.Modified;
                    db.SaveChanges();
                    ////-------------------- Notification-------------------------////
                    Notification.GetNotification("Departments", "Edit", "AddEdit", id, null, "الفروع");

                    // Add DB Change
                    QueryHelper.AddDBChange(new DBChange()
                    {
                        TableName = "Department",
                        SelectedId = department.Id,
                        IsMasterChange = true,
                        IsNew = false,
                        IsTransaction = false
                    });
                }
                else
                {
                    department.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                    department.Code = (QueryHelper.CodeLastNum("Department") + 1).ToString();
                    department.IsActive = true;
                    //if (db.Departments.Where(x => x.IsDeleted == false).Count() > 1)
                    //    return RedirectToAction("Index");
                    db.Departments.Add(department);
                    db.SaveChanges();
                    ////-------------------- Notification-------------------------////

                    Notification.GetNotification("Departments", "Add", "AddEdit", department.Id, null, "الفروع");

                    // Add DB Change
                    var SelectedId = db.Departments.OrderByDescending(r => r.Id).FirstOrDefault().Id;
                    QueryHelper.AddDBChange(new DBChange()
                    {
                        TableName = "Department",
                        SelectedId = SelectedId,
                        IsMasterChange = true,
                        IsNew = true,
                        IsTransaction = false
                    });
                }

                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = id > 0 ? "تعديل الفرع" : "اضافة الفرع",
                    EnAction = "AddEdit",
                    ControllerName = "Departments",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = department.Id,
                    ArItemName = department.ArName,
                    EnItemName = department.EnName,
                    CodeOrDocNo = department.Code
                });
                if (newBtn == "saveAndNew")
                {
                    return RedirectToAction("AddEdit");

                }
                else
                {
                    return RedirectToAction("Index");
                }
            }

            var generalAccounts = db.ChartOfAccounts.Where(c => c.IsDeleted == false && c.IsActive == true && (c.ClassificationId == 2 || c.ClassificationId == 3)).Select(b => new
            {
                Id = b.Id,
                ArName = session.ToString() == "en" && b.EnName != null ? b.Code + " - " + b.EnName : b.Code + " - " + b.ArName
            });
            var basicAccounts = db.ChartOfAccounts.Where(c => c.IsDeleted == false && c.IsActive == true && c.ClassificationId == 1).Select(b => new
            {
                Id = b.Id,
                ArName = session.ToString() == "en" && b.EnName != null ? b.Code + " - " + b.EnName : b.Code + " - " + b.ArName
            });
            var subAccounts = db.ChartOfAccounts.Where(c => c.IsDeleted == false && c.IsActive == true && (c.ClassificationId == 3)).Select(b => new
            {
                b.Id,
                ArName = session.ToString() == "en" && b.EnName != null ? b.Code + " - " + b.EnName : b.Code + " - " + b.ArName
            }).ToList();

            //ViewBag.BankAccountId = new SelectList(db.BankAccounts.Where(c => c.IsDeleted == false && c.IsActive == true), "Id", "AccountNumber", department.BankAccountId);
            ViewBag.AcquiredDeductionAccountId = new SelectList(generalAccounts, "Id", "ArName", department.AcquiredDeductionAccountId);
            ViewBag.DiscountAllowedAccountId = new SelectList(generalAccounts, "Id", "ArName", department.DiscountAllowedAccountId);
            ViewBag.InventoryAccountId = new SelectList(generalAccounts, "Id", "ArName", department.InventoryAccountId);
            ViewBag.FixedAssetsDepreciationExpensesAccountId = new SelectList(generalAccounts, "Id", "ArName", department.FixedAssetsDepreciationExpensesAccountId);
            ViewBag.SalesAccountId = new SelectList(generalAccounts, "Id", "ArName", department.SalesAccountId);
            ViewBag.CostOfSalesAccountId = new SelectList(generalAccounts, "Id", "ArName", department.CostOfSalesAccountId);
            ViewBag.GeneralExpensesAccountId = new SelectList(generalAccounts, "Id", "ArName", department.GeneralExpensesAccountId);
            ViewBag.TaxAccountId = new SelectList(generalAccounts, "Id", "ArName", department.TaxAccountId);
            ViewBag.ShareHolderAccountId = new SelectList(generalAccounts, "Id", "ArName", department.ShareHolderAccountId);
            ViewBag.CurrentShareHolderAccountId = new SelectList(generalAccounts, "Id", "ArName", department.CurrentShareHolderAccountId);
            ViewBag.FixedAssetsAccountId = new SelectList(generalAccounts, "Id", "ArName", department.FixedAssetsAccountId);
            ViewBag.CurrentAssetsAccountId = new SelectList(generalAccounts, "Id", "ArName", department.CurrentAssetsAccountId);
            ViewBag.CustomersAccountId = new SelectList(generalAccounts, "Id", "ArName", department.CustomersAccountId);
            ViewBag.VendorsAccountId = new SelectList(generalAccounts, "Id", "ArName", department.VendorsAccountId);
            ViewBag.ExpensesAccountId = new SelectList(basicAccounts, "Id", "ArName", department.ExpensesAccountId);
            ViewBag.RevenueAccountId = new SelectList(generalAccounts, "Id", "ArName", department.RevenueAccountId);
            ViewBag.InstallmentRevenueAccountId = new SelectList(basicAccounts, "Id", "ArName", department.InstallmentRevenueAccountId);
            ViewBag.DirectExpensesAccountId = new SelectList(generalAccounts, "Id", "ArName", department.DirectExpensesAccountId);
            ViewBag.InventoryVarianceAccountId = new SelectList(subAccounts, "Id", "ArName", department.InventoryVarianceAccountId);
            ViewBag.LostAndDamagedAccountId = new SelectList(subAccounts, "Id", "ArName", department.LostAndDamagedAccountId);
            ViewBag.GiftsAndSamplesAccountId = new SelectList(subAccounts, "Id", "ArName", department.GiftsAndSamplesAccountId);
            ViewBag.RefundableInsuranceAccountId = new SelectList(subAccounts, "Id", "ArName", department.RefundableInsuranceAccountId);
            ViewBag.HotelRevenueAccountId = new SelectList(subAccounts, "Id", "ArName", department.HotelRevenueAccountId);
            ViewBag.SmokingTaxAccountId = new SelectList(subAccounts, "Id", "ArName", department.SmokingTaxAccountId);
            ViewBag.ServiceRevenueAccountId = new SelectList(subAccounts, "Id", "ArName", department.ServiceRevenueAccountId);
            ViewBag.ChildSubscriptionAccountId = new SelectList(subAccounts, "Id", "ArName", department.ChildSubscriptionAccountId);
            ViewBag.ElderHostingAccountId = new SelectList(subAccounts, "Id", "ArName", department.ElderHostingAccountId);
            ViewBag.DebitValueAddedTaxesAccountId = new SelectList(subAccounts, "Id", "ArName", department.DebitValueAddedTaxesAccountId);
            ViewBag.CreditValueAddedTaxesAccountId = new SelectList(subAccounts, "Id", "ArName", department.CreditValueAddedTaxesAccountId);

            ViewBag.CompanyId = new SelectList(db.Companies.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", department.CompanyId);
            ViewBag.ActivityId = new SelectList(db.Activities.Where(a => a.IsActive == true && a.IsDeleted == false).Select(b => new
            {
                Id = b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", department.ActivityId);
            //-------------- Property Management Accounts -----------------------------------//
            ViewBag.QuestAndCommissionRevenueId = new SelectList(subAccounts, "Id", "ArName", department.QuestAndCommissionRevenueId);
            ViewBag.WaterRevenueId = new SelectList(subAccounts, "Id", "ArName", department.WaterRevenueId);
            ViewBag.ElectricityRevenueId = new SelectList(subAccounts, "Id", "ArName", department.ElectricityRevenueId);
            ViewBag.ServicesRevenueId = new SelectList(subAccounts, "Id", "ArName", department.ServicesRevenueId);
            ViewBag.RentRevenueId = new SelectList(subAccounts, "Id", "ArName", department.RentRevenueId);
            ViewBag.RentsDueToOthersId = new SelectList(subAccounts, "Id", "ArName", department.RentsDueToOthersId);
            ViewBag.MediatorARentsDueToOthersId = new SelectList(subAccounts, "Id", "ArName", department.MediatorARentsDueToOthersId);
            ViewBag.CommissionDueFromOthersOwningId = new SelectList(subAccounts, "Id", "ArName", department.CommissionDueFromOthersOwningId);
            //--------------------------------------------------------------------------------------------------------------------------------------//
            ViewBag.Next = QueryHelper.Next(department.Id, "Department");
            ViewBag.Previous = QueryHelper.Previous(department.Id, "Department");
            ViewBag.Last = QueryHelper.GetLast("Department");
            ViewBag.First = QueryHelper.GetFirst("Department");
            return View(department);
        }

        // POST: Departments/Delete/5
        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(int id)
        {
            try
            {
                Department department = db.Departments.Find(id);
                department.IsDeleted = true;
                department.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                db.Entry(department).State = EntityState.Modified;
                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = "حذف الفرع",
                    EnAction = "AddEdit",
                    ControllerName = "Departments",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = id,
                    EnItemName = department.EnName,
                    ArItemName = department.ArName,
                    CodeOrDocNo = department.Code
                });
                ////-------------------- Notification-------------------------////
                Notification.GetNotification("Departments", "Delete", "Delete", id, null, "الفروع");

                //int pageid = db.Get_PageId("Departments").SingleOrDefault().Value;
                //var actionId = db.PageActions.FirstOrDefault(c => c.Action == "Delete" && c.EnName == "Delete" && c.PageId == pageid).Id;
                //int userid = db.UserNotifications.FirstOrDefault(c => c.ActionId == actionId && c.PageId == pageid).UserId.Value;
                //var UserName = User.Identity.Name;
                //db.Sp_OccuredNotification(actionId, $"بحذف بيانات في شاشة الفروع  {UserName}قام المستخدم  ");
                ////////////////-----------------------------------------------------------------------

                // Add DB Change

                QueryHelper.AddDBChange(new DBChange()
                {
                    TableName = "Department",
                    SelectedId = department.Id,
                    IsMasterChange = true,
                    IsNew = false,
                    IsTransaction = false
                });
                return Content("true");
            }
            catch (Exception)
            {

                throw;
            }
        }

        [HttpPost]
        public ActionResult ActivateDeactivate(int id)
        {
            try
            {
                Department department = db.Departments.Find(id);
                if (department.IsActive == true)
                {
                    department.IsActive = false;
                }
                else
                {
                    department.IsActive = true;
                }
                department.UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

                db.Entry(department).State = EntityState.Modified;

                db.SaveChanges();
                QueryHelper.AddLog(new MyLog()
                {
                    ArAction = (bool)department.IsActive ? "تنشيط الفرع" : "إلغاء الفرع",
                    EnAction = "AddEdit",
                    ControllerName = "Departments",
                    UserName = User.Identity.Name,
                    UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                    LogDate = DateTime.Now,
                    RequestMethod = "POST",
                    SelectedItem = department.Id,
                    EnItemName = department.EnName,
                    ArItemName = department.ArName,
                    CodeOrDocNo = department.Code
                });
                ////-------------------- Notification-------------------------////
                if (department.IsActive == true)
                {
                    Notification.GetNotification("Departments", "Activate/Deactivate", "ActivateDeactivate", id, true, "الفروع");
                }
                else
                {

                    Notification.GetNotification("Departments", "Activate/Deactivate", "ActivateDeactivate", id, false, "الفروع");
                }
                //int pageid = db.Get_PageId("Departments").SingleOrDefault().Value;
                //var actionId = db.PageActions.FirstOrDefault(c => c.Action == "ActivateDeactivate" && c.EnName == "Activate/Deactivate" && c.PageId == pageid).Id;
                //int userid = db.UserNotifications.FirstOrDefault(c => c.ActionId == actionId && c.PageId == pageid).UserId.Value;
                //var UserName = User.Identity.Name;
                //db.Sp_OccuredNotification(actionId, (bool)department.IsActive ? $" تنشيط  في شاشة الفروع{UserName}قام المستخدم  " : $"إلغاء تنشيط  في شاشة الفروع{UserName}قام المستخدم  ");
                ////////////////-----------------------------------------------------------------------


                QueryHelper.AddDBChange(new DBChange()
                {
                    TableName = "Department",
                    SelectedId = department.Id,
                    IsMasterChange = true,
                    IsNew = false,
                    IsTransaction = false
                });
                return Content("true");
            }
            catch (Exception ex)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
        }
        [SkipERPAuthorize]
        public JsonResult SetCodeNum()
        {
            var code = QueryHelper.CodeLastNum("Department");
            return Json(code + 1, JsonRequestBehavior.AllowGet);
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
