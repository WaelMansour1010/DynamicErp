using DevExpress.XtraPrinting;
using DevExpress.XtraReports.UI;
using MyERP.Models;
using MyERP.Repository;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace MyERP.Reporting
{
    [ReportAuthorize]
    [SkipERPAuthorize]
    public class ReportController : Controller
    {
        private MySoftERPEntity db = new MySoftERPEntity();
        //Report
        public ActionResult Index(int pageIndex = 1, int wantedRowsNo = 10, string searchWord = "", int ReportId = 0)
        {
            QueryHelper.AddLog(new MyLog()
            {
                ArAction = "فتح قائمة التقارير",
                EnAction = "Index",
                ControllerName = "Report",
                UserName = User.Identity.Name,
                UserId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value),
                LogDate = DateTime.Now,
                RequestMethod = "GET"
            });
            Notification.GetNotification("Report", "View", "Index", null, null, "التقارير");
            
            //2116--> Report
            //10270 --> Manufacturing Managment Report
            //10271 --> CarService Report
            ViewBag.Reports = new SelectList(db.SystemPages.Where(d => d.IsDeleted == false && d.ParentId == 2116 && d.Id != 10271 && d.Id != 10270 && d.IsActive == true).Select(b => new
            {
                Id = b.Id,
                ArName = b.ArName
            }), "Id", "ArName", ReportId);


            ViewBag.PageIndex = pageIndex;
            int skipRowsNo = 0;
            if (pageIndex > 1)
            {
                skipRowsNo = (pageIndex - 1) * wantedRowsNo;
            }

            IQueryable<SystemPage> systemPages;
            if (string.IsNullOrEmpty(searchWord))
            {
                //systemPages = db.SystemPages.Where(a => a.IsDeleted == false && a.IsActive == true && a.IsReport == true && a.IsModule == false && a.ShowInReportPage == true && (ReportId == 0 || a.ParentId == ReportId)).OrderBy(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);
                systemPages = db.SystemPages.Where(a => a.IsDeleted == false && a.IsActive == true && a.IsReport == true && a.IsModule == false && a.ShowInReportPage == true && (ReportId == 0 || a.ParentId == ReportId)).OrderBy(a => a.PageCode /*a.Id*/);
                ViewBag.Count = db.SystemPages.Where(a => a.IsDeleted == false && a.IsActive == true && a.IsReport == true && a.IsModule == false && a.ShowInReportPage == true && (ReportId == 0 || a.ParentId == ReportId)).Count();
            }
            else
            {
                //systemPages = db.SystemPages.Where(a => a.IsDeleted == false && a.IsActive == true && a.IsReport == true && a.IsModule == false && a.ShowInReportPage == true && (ReportId == 0 || a.ParentId == ReportId) &&
                //(a.ArName.Contains(searchWord) || a.EnName.Contains(searchWord) || a.Code.Contains(searchWord)))
                //    .OrderBy(a => a.Id).Skip(skipRowsNo).Take(wantedRowsNo);

                systemPages = db.SystemPages.Where(a => a.IsDeleted == false && a.IsActive == true && a.IsReport == true && a.IsModule == false && a.ShowInReportPage == true && (ReportId == 0 || a.ParentId == ReportId) &&
                (a.ArName.Contains(searchWord) || a.EnName.Contains(searchWord) || a.Code.Contains(searchWord)))
                    .OrderBy(a => a.PageCode /*a.Id*/);
                ViewBag.Count = db.SystemPages.Where(a => a.IsDeleted == false && a.IsActive == true && (ReportId == 0 || a.ParentId == ReportId) && a.IsReport == true && a.IsModule == false && a.ShowInReportPage == true && (a.ArName.Contains(searchWord) || a.EnName.Contains(searchWord) || a.Code.Contains(searchWord))).Count();
            }
            ViewBag.searchWord = searchWord;
            ViewBag.wantedRowsNo = wantedRowsNo;
            ViewBag.IsWarehouseInAllowedModule = db.AllowedModules.Where(a => a.SystemPageId == 3183 && a.IsSelected == true).Count() > 0 ? true : false;
            ViewBag.IsPurchaseInAllowedModule = db.AllowedModules.Where(a => a.SystemPageId == 3184 && a.IsSelected == true).Count() > 0 ? true : false;
            ViewBag.IsSalesInAllowedModule = db.AllowedModules.Where(a => a.SystemPageId == 3185 && a.IsSelected == true).Count() > 0 ? true : false;
            ViewBag.IsFinancialInAllowedModule = db.AllowedModules.Where(a => a.SystemPageId == 10276 && a.IsSelected == true).Count() > 0 ? true : false;
            ViewBag.IsMedicalInAllowedModule = db.AllowedModules.Where(a => a.SystemPageId == 10413 && a.IsSelected == true).Count() > 0 ? true : false;
            ViewBag.PointOfSaleInAllowedModule = db.AllowedModules.Where(a => a.SystemPageId == 10272 && a.IsSelected == true).Count() > 0 ? true : false;
            return View(systemPages.ToList());
        }
        public ActionResult ChartofAccounts(int? TypeId, int? ClassificationId, int? CategoryId, int? AccountId, bool? showReport, bool? print)
        {
            ChartOfAccounts_Report report = new ChartOfAccounts_Report(TypeId, ClassificationId, CategoryId, AccountId);
            report.Parameters["TypeId"].Value = TypeId > 0 ? TypeId : null;
            report.Parameters["ClassificationId"].Value = ClassificationId > 0 ? ClassificationId : null;
            report.Parameters["CategoryId"].Value = CategoryId > 0 ? CategoryId : null;
            report.Parameters["ParrentAccount"].Value = AccountId > 0 ? AccountId : null;
            report.RequestParameters = false;
            ViewBag.ShowReport = showReport;
            ViewBag.TypeId = TypeId;
            ViewBag.ClassificationId = ClassificationId;
            ViewBag.CategoryId = CategoryId;
            ViewBag.AccountId = AccountId;
            if (showReport == true)
            {
                return View(report);
            }
            else if (print == true)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    report.ExportToPdf(ms, new PdfExportOptions() { ShowPrintDialogOnOpen = false });
                    return File(ms.ToArray(), "application/pdf");
                }
            }
            else
            {
                var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                ViewBag.TypeId = new SelectList(db.ChartOfAccountsTypes.Select(b => new
                {
                    b.Id,
                    ArName = b.ArType
                }), "Id", "ArName", TypeId);

                ViewBag.ClassificationId = new SelectList(db.ChartOfAccountsClassifications.Select(b => new
                {
                    Id = b.Id,
                    ArName = b.ArClassification
                }), "Id", "ArName", ClassificationId);

                ViewBag.CategoryId = new SelectList(db.ChartOfAccountsCategories.Select(b => new
                {
                    Id = b.Id,
                    ArName = b.ArCategory
                }), "Id", "ArName", CategoryId);

                ViewBag.AccountId = new SelectList(db.ChartOfAccounts.Where(a => a.IsActive == true && a.IsDeleted == false && (a.ClassificationId == 1 || a.ClassificationId == 2)).Select(a => new
                {
                    Id = a.Id,
                    ArName = a.Code + " - " + a.ArName
                }), "Id", "ArName", AccountId);
                return View();
            }
        }

        public ActionResult UnitesStatus()
        {
            return View();
        }
        public ActionResult PropertyProp_Report()
        {
            return View();
        }

        public ActionResult PropertyContractsExpiredReport()
        {
            return View();
        }
        public ActionResult PropertyProphet()
        {
            return View();
        }
        public ActionResult UserPrivilege()
        {
            return View();
        }
        public ActionResult AccountsOpeningBalance()
        {

            return View();
        }
        public ActionResult ExpensesInPeriod(DateTime? From, DateTime? To, int? ExpenseId, int? DepartmentId, int? ActivityId, int? CompanyId, bool? showReport, bool? print)
        {
            ExpensesInPeriod_Report report = new ExpensesInPeriod_Report(DepartmentId, From, To, ExpenseId, ActivityId, CompanyId);
            report.Parameters["DepartmentId"].Value = DepartmentId > 0 ? DepartmentId : null;
            report.Parameters["DirectExpenses"].Value = ExpenseId > 0 ? ExpenseId : null;
            report.Parameters["ActivityId"].Value = ActivityId > 0 ? ActivityId : null;
            report.Parameters["CompanyId"].Value = CompanyId > 0 ? CompanyId : null;
            report.Parameters["From"].Value = From;
            report.Parameters["To"].Value = To;
            report.RequestParameters = false;
            ViewBag.ShowReport = showReport;
            ViewBag.From = From;
            ViewBag.To = To;
            ViewBag.ExpenseId = ExpenseId;
            ViewBag.ActivityId = ActivityId;
            ViewBag.CompanyId = CompanyId;
            ViewBag.DepartmentId = DepartmentId;
            if (showReport == true)
            {
                return View(report);
            }
            else if (print == true)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    report.ExportToPdf(ms, new PdfExportOptions() { ShowPrintDialogOnOpen = false });
                    return File(ms.ToArray(), "application/pdf");
                }
            }
            else
            {
                var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                ViewBag.ExpenseId = new SelectList(db.DirectExpenses.Where(a => a.IsActive == true && a.IsDeleted == false).Select(a => new
                {
                    Id = a.Id,
                    ArName = a.Code + " - " + a.ArName
                }), "Id", "ArName", ExpenseId);
                ViewBag.ActivityId = new SelectList(db.Activities.Where(a => a.IsActive == true && a.IsDeleted == false).Select(a => new
                {
                    Id = a.Id,
                    ArName = a.Code + " - " + a.ArName
                }), "Id", "ArName", ActivityId);
                ViewBag.CompanyId = new SelectList(db.Companies.Where(a => a.IsActive == true && a.IsDeleted == false).Select(a => new
                {
                    Id = a.Id,
                    ArName = a.Code + " - " + a.ArName
                }), "Id", "ArName", CompanyId);
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
                return View();
            }
        }
        public ActionResult BankAccounts()
        {

            return View();
        }
        public ActionResult TechniciansOB()
        {

            return View();
        }

        public ActionResult CashIssueReceiptVoucher()
        {
            return View();
        }

        public ActionResult AccountStatement(DateTime? From, DateTime? To, int? AccountId, int? DepartmentId, int? ActivityId, int? CompanyId, bool? showReport, bool? print)
        {
            AccountStatment_Report report = new AccountStatment_Report(From, To, AccountId, DepartmentId, ActivityId, CompanyId);
            report.Parameters["DepartmentId"].Value = DepartmentId > 0 ? DepartmentId : null;
            report.Parameters["ChartOfAccountId"].Value = AccountId > 0 ? AccountId : null;
            report.Parameters["FromDate"].Value = From;
            report.Parameters["ToDate"].Value = To;
            report.Parameters["ActivityId"].Value = ActivityId > 0 ? ActivityId : null;
            report.Parameters["CompanyId"].Value = CompanyId > 0 ? CompanyId : null;
            report.RequestParameters = false;
            ViewBag.ShowReport = showReport;
            ViewBag.From = From;
            ViewBag.To = To;
            ViewBag.AccountId = AccountId;
            ViewBag.DepartmentId = DepartmentId;
            ViewBag.ActivityId = ActivityId;
            ViewBag.CompanyId = CompanyId;
            if (showReport == true)
            {
                return View(report);
            }
            else if (print == true)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    report.ExportToPdf(ms, new PdfExportOptions() { ShowPrintDialogOnOpen = false });
                    return File(ms.ToArray(), "application/pdf");
                }
            }
            else
            {
                var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                ViewBag.AccountId = new SelectList(db.ChartOfAccounts.Where(a => a.IsActive == true && a.IsDeleted == false).Select(a => new
                {
                    Id = a.Id,
                    ArName = a.Code + " - " + a.ArName
                }), "Id", "ArName", AccountId);
                ViewBag.ActivityId = new SelectList(db.Activities.Where(a => a.IsActive == true && a.IsDeleted == false).Select(a => new
                {
                    Id = a.Id,
                    ArName = a.Code + " - " + a.ArName
                }), "Id", "ArName", ActivityId);
                ViewBag.CompanyId = new SelectList(db.Companies.Where(a => a.IsActive == true && a.IsDeleted == false).Select(a => new
                {
                    Id = a.Id,
                    ArName = a.Code + " - " + a.ArName
                }), "Id", "ArName", CompanyId);
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
                return View();
            }
        }

        public ActionResult AccountStatementDetails(DateTime? From, DateTime? To, int? AccountId, int? DepartmentId, int? ActivityId, int? CompanyId, bool? showReport, bool? print)
        {
            AccountStatmentDetails_Report rpt = new AccountStatmentDetails_Report(From, To, AccountId, DepartmentId, ActivityId, CompanyId);

            if (showReport == true)
            {
                rpt.Parameters["DepartmentId"].Value = DepartmentId > 0 ? DepartmentId : null;
                rpt.Parameters["AccountId"].Value = AccountId > 0 ? AccountId : null;
                rpt.Parameters["ActivityId"].Value = ActivityId > 0 ? ActivityId : null;
                rpt.Parameters["CompanyId"].Value = CompanyId > 0 ? CompanyId : null;
                rpt.Parameters["fromDate"].Value = From;
                rpt.Parameters["ToDate"].Value = To;
                rpt.RequestParameters = false;
                ViewBag.ShowReport = showReport;
                ViewBag.From = From;
                ViewBag.To = To;
                ViewBag.AccountId = AccountId;
                ViewBag.ActivityId = ActivityId;
                ViewBag.CompanyId = CompanyId;
                ViewBag.DepartmentId = DepartmentId;
                //if (AccountId != null)
                //{

                //}
                //if (print == true)
                //{
                //    using (MemoryStream ms = new MemoryStream())
                //    {
                //        rpt.ExportToPdf(ms, new PdfExportOptions() { ShowPrintDialogOnOpen = true });
                //        return File(ms, "application/pdf");
                //    }
                //}
                return View(rpt);
            }
            else if (print == true)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    rpt.ExportToPdf(ms, new PdfExportOptions() { ShowPrintDialogOnOpen = false });
                    return File(ms.ToArray(), "application/pdf");
                }
            }
            else
            {
                var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                ViewBag.AccountId = new SelectList(db.ChartOfAccounts.Where(a => a.IsActive == true && a.IsDeleted == false && a.ClassificationId == 3).Select(a => new
                {
                    Id = a.Id,
                    ArName = a.Code + " - " + a.ArName
                }), "Id", "ArName", AccountId);
                ViewBag.ActivityId = new SelectList(db.Activities.Where(a => a.IsActive == true && a.IsDeleted == false).Select(a => new
                {
                    Id = a.Id,
                    ArName = a.Code + " - " + a.ArName
                }), "Id", "ArName", ActivityId);
                ViewBag.CompanyId = new SelectList(db.Companies.Where(a => a.IsActive == true && a.IsDeleted == false).Select(a => new
                {
                    Id = a.Id,
                    ArName = a.Code + " - " + a.ArName
                }), "Id", "ArName", CompanyId);
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
                return View();
            }

        }

        public ActionResult IncomeStatement(int? DepartmentId, DateTime? From, DateTime? To, int? ActivityId, int? CompanyId, bool? showReport, bool? print)
        {
            IncomeList_Report rpt = new IncomeList_Report(DepartmentId, From, To, ActivityId, CompanyId);
            rpt.Parameters["DepId"].Value = DepartmentId > 0 ? DepartmentId : null;
            rpt.Parameters["ActivityId"].Value = ActivityId > 0 ? ActivityId : null;
            rpt.Parameters["CompanyId"].Value = CompanyId > 0 ? CompanyId : null;
            rpt.Parameters["FromDate"].Value = From;
            rpt.Parameters["ToDate"].Value = To;
            rpt.RequestParameters = false;
            ViewBag.ShowReport = showReport;
            ViewBag.From = From;
            ViewBag.To = To;
            ViewBag.DepartmentId = DepartmentId;
            ViewBag.ActivityId = ActivityId;
            ViewBag.CompanyId = CompanyId;
            if (showReport == true)
            {
                return View(rpt);
            }
            else if (print == true)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    rpt.ExportToPdf(ms, new PdfExportOptions() { ShowPrintDialogOnOpen = false });
                    return File(ms.ToArray(), "application/pdf");
                }
            }
            else
            {
                ViewBag.ActivityId = new SelectList(db.Activities.Where(d => d.IsActive == true && d.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName", ActivityId);
                ViewBag.CompanyId = new SelectList(db.Companies.Where(d => d.IsActive == true && d.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName", CompanyId);
                var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
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
                return View();
            }
        }

        public ActionResult FinancialStatement(int? DepartmentId, DateTime? From, DateTime? To, int? ActivityId, int? CompanyId, bool? showReport, bool? print)
        {
            FinancialStatement_Report rpt = new FinancialStatement_Report(DepartmentId, From, To, ActivityId, CompanyId);
            rpt.Parameters["DepId"].Value = DepartmentId > 0 ? DepartmentId : null;
            rpt.Parameters["ActivityId"].Value = ActivityId > 0 ? ActivityId : null;
            rpt.Parameters["CompanyId"].Value = CompanyId > 0 ? CompanyId : null;
            rpt.Parameters["DateFrom"].Value = From;
            rpt.Parameters["DateTo"].Value = To;
            rpt.RequestParameters = false;
            ViewBag.ShowReport = showReport;
            ViewBag.From = From;
            ViewBag.To = To;
            ViewBag.DepartmentId = DepartmentId;
            ViewBag.ActivityId = ActivityId;
            ViewBag.CompanyId = CompanyId;

            if (showReport == true)
            {
                return View(rpt);
            }
            else if (print == true)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    rpt.ExportToPdf(ms, new PdfExportOptions() { ShowPrintDialogOnOpen = false });
                    return File(ms.ToArray(), "application/pdf");
                }
            }
            else
            {
                ViewBag.ActivityId = new SelectList(db.Activities.Where(d => d.IsActive == true && d.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName", ActivityId);
                ViewBag.CompanyId = new SelectList(db.Companies.Where(d => d.IsActive == true && d.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName", CompanyId);
                var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
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
                return View();
            }
        }

        public ActionResult CashIssue()
        {
            CashIssueVoucher_Report rpt = new CashIssueVoucher_Report();
            return View(rpt);
        }

        public ActionResult CashReceipt()
        {

            return View();
        }
        public ActionResult CashIssueAndReceipt(int? departmentId, int? cashBoxId, DateTime? dateFrom, DateTime? dateTo, int? ActivityId, int? CompanyId, bool? showReport, bool? print)
        {
            CashIssueAndReceipt_Report report = new CashIssueAndReceipt_Report(departmentId, cashBoxId, dateFrom, dateTo, ActivityId, CompanyId);
            report.Parameters["DepartmentId"].Value = departmentId > 0 ? departmentId : null;
            report.Parameters["CashBoxId"].Value = cashBoxId > 0 ? cashBoxId : null;
            report.Parameters["ActivityId"].Value = ActivityId > 0 ? ActivityId : null;
            report.Parameters["CompanyId"].Value = CompanyId > 0 ? CompanyId : null;
            report.Parameters["DateFrom"].Value = dateFrom;
            report.Parameters["DateTo"].Value = dateTo;
            report.RequestParameters = false;
            ViewBag.ShowReport = showReport;
            ViewBag.DepartmentId = departmentId;
            ViewBag.CashBoxId = cashBoxId;
            ViewBag.ActivityId = ActivityId;
            ViewBag.CompanyId = CompanyId;
            ViewBag.dateFrom = dateFrom;
            ViewBag.dateTo = dateTo;
            if (showReport == true)
            {
                return View(report);
            }
            else if (print == true)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    report.ExportToPdf(ms, new PdfExportOptions() { ShowPrintDialogOnOpen = false });
                    return File(ms.ToArray(), "application/pdf");
                }
            }
            else
            {
                var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                ViewBag.CashBoxId = new SelectList(db.CashBoxes.Where(b => b.IsActive == true && b.IsDeleted == false && (userId == 1 || (db.UserCashBoxes.Where(a => a.UserId == userId && a.Privilege == true).Any(a => a.CashBoxId == b.Id))))
                    .Select(b => new
                    {
                        Id = b.Id,
                        ArName = b.Code + " - " + b.ArName
                    }), "Id", "ArName", cashBoxId);
                ViewBag.ActivityId = new SelectList(db.Activities.Where(b => b.IsActive == true && b.IsDeleted == false)
                    .Select(b => new
                    {
                        Id = b.Id,
                        ArName = b.Code + " - " + b.ArName
                    }), "Id", "ArName", ActivityId);
                ViewBag.CompanyId = new SelectList(db.Companies.Where(b => b.IsActive == true && b.IsDeleted == false)
                    .Select(b => new
                    {
                        Id = b.Id,
                        ArName = b.Code + " - " + b.ArName
                    }), "Id", "ArName", CompanyId);
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
                return View();
            }
        }

        public ActionResult JournalEntry()
        {
            return View();
        }
        public ActionResult WarehouseTransactionsInPeriod(int? WarehouseId, DateTime? From, DateTime? To, string TransactionType, bool? showReport, bool? print)
        {
            WarehouseTransactionsInPeriod_Report report = new WarehouseTransactionsInPeriod_Report(WarehouseId, From, To, TransactionType);
            report.Parameters["WarehouseId"].Value = WarehouseId > 0 ? WarehouseId : null;
            report.Parameters["DateFrom"].Value = From != null ? From : null;
            report.Parameters["DateTo"].Value = To != null ? To : null;
            report.Parameters["TransType"].Value = TransactionType != null ? TransactionType : null;
            report.RequestParameters = false;
            ViewBag.ShowReport = showReport;
            ViewBag.WarehouseId = WarehouseId;
            ViewBag.From = From;
            ViewBag.To = To;
            ViewBag.TransactionType = TransactionType;
            if (showReport == true)
            {
                return View(report);
            }
            else if (print == true)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    report.ExportToPdf(ms, new PdfExportOptions() { ShowPrintDialogOnOpen = false });
                    return File(ms.ToArray(), "application/pdf");
                }
            }
            else
            {
                var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                ViewBag.WarehouseId = new SelectList(db.Warehouses.Where(b => b.IsActive == true && b.IsDeleted == false && (userId == 1
                || db.UserWareHouses.Where(a => a.UserId == 1 || a.UserId == userId).Any(a => a.WareHouseId == b.Id))
                ).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName", WarehouseId);

                ViewBag.TransactionType = new SelectList(new List<dynamic>
                {
                new { Id = "رصيد", ArName = "رصيد افتتاحى" },
                new { Id = "صرف", ArName = "سند صرف" },
                new { Id = "توريد", ArName = "سند توريد" }
                }, "Id", "ArName", TransactionType);

                return View();
            }
        }
        public ActionResult StockTransferDetailsInPeriod(int? WarehouseId, int? DestinationWarehouseId, DateTime? From, DateTime? To, bool? showReport, bool? print)
        {
            StockTransferDetailsInPeriod_Report report = new StockTransferDetailsInPeriod_Report(WarehouseId, DestinationWarehouseId, From, To);
            report.Parameters["WarehouseId"].Value = WarehouseId > 0 ? WarehouseId : null;
            report.Parameters["DateFrom"].Value = From != null ? From : null;
            report.Parameters["DateTo"].Value = To != null ? To : null;
            report.Parameters["DestinationWarehouseId"].Value = DestinationWarehouseId != null ? DestinationWarehouseId : null;
            report.RequestParameters = false;
            ViewBag.ShowReport = showReport;
            ViewBag.WarehouseId = WarehouseId;
            ViewBag.From = From;
            ViewBag.To = To;
            ViewBag.DestinationWarehouseId = DestinationWarehouseId;
            if (showReport == true)
            {
                return View(report);
            }
            else if (print == true)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    report.ExportToPdf(ms, new PdfExportOptions() { ShowPrintDialogOnOpen = false });
                    return File(ms.ToArray(), "application/pdf");
                }
            }
            else
            {
                var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                ViewBag.WarehouseId = new SelectList(db.Warehouses.Where(b => b.IsActive == true && b.IsDeleted == false && (userId == 1
                || db.UserWareHouses.Where(a => a.UserId == 1 || a.UserId == userId).Any(a => a.WareHouseId == b.Id))
                ).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName", WarehouseId);

                ViewBag.DestinationWarehouseId = new SelectList(db.Warehouses.Where(b => b.IsActive == true && b.IsDeleted == false && (userId == 1
                || db.UserWareHouses.Where(a => a.UserId == 1 || a.UserId == userId).Any(a => a.WareHouseId == b.Id))
                ).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName", DestinationWarehouseId);

                return View();
            }
        }
        public ActionResult JournalEntryDetails(int? id, int? deptId, string docNo, bool? print)
        {
            JournalEntryDetails_Report rpt = new JournalEntryDetails_Report();
            if (id != null)
            {
                JournalEntry ct = db.JournalEntries.Find(id);
                rpt.Parameters["Id"].Value = id;
                rpt.Parameters["DocumentNumber"].Value = ct.DocumentNumber;
                rpt.Parameters["DepId"].Value = ct.DepartmentId;
                rpt.RequestParameters = false;
            }
            else if (deptId != null && docNo != null)
            {
                JournalEntry ct = db.JournalEntries.Where(a => a.DepartmentId == deptId && a.DocumentNumber == docNo).FirstOrDefault();
                rpt.Parameters["Id"].Value = id;
                rpt.Parameters["DocumentNumber"].Value = ct.DocumentNumber;
                rpt.Parameters["DepId"].Value = ct.DepartmentId;
                rpt.RequestParameters = false;

            }
            if (print == true)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    rpt.ExportToPdf(ms, new PdfExportOptions() { ShowPrintDialogOnOpen = true });
                    return File(ms.ToArray(), "application/pdf");
                }
            }

            rpt.RequestParameters = true;
            return View(rpt);

        }

        public ActionResult CashTransfer()
        {
            return View();
        }

        public ActionResult CashIssueVoucherDetails(int? id, bool? print)
        {
            CashIssueVoucherDetails_Report rpt = new CashIssueVoucherDetails_Report();
            if (id != null)
            {
                CashIssueVoucher civ = db.CashIssueVouchers.FirstOrDefault(t => t.Id == id);
                if (civ.SourceTypeId == 10)
                {
             var accname =civ.IssueAnalysis.FirstOrDefault()?.ChartOfAccount?.ArName;
               
                   rpt.lblPropertyOwner.Visible = true;
                     rpt.lblPropertyOwner.Text = accname;
                }
                //if (civ.SourceTypeId == 13)
                //{

                //    var owner = db.PropertyOwners.Where(t => t.Id == civ.PropertyOwnerId).Select(t => t.ArName).FirstOrDefault();
                //    civ.ReceivedPerson = owner;
                //    //rpt.lblPropertyOwner.Visible = true;
                //    //rpt.lblPropertyOwner.Text = owner;
                //}
                rpt.Parameters["Id"].Value = id;
                rpt.Parameters["DocumentNumber"].Value = civ.DocumentNumber;
                rpt.Parameters["DepId"].Value = civ.DepartmentId;
                rpt.Parameters["MoneyAmount"].Value = civ.MoneyAmount;
                rpt.Parameters["UserId"].Value = civ.UserId;
                rpt.RequestParameters = false;
            }
            if (print == true)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    rpt.ExportToPdf(ms, new PdfExportOptions() { ShowPrintDialogOnOpen = true });
                    return File(ms.ToArray(), "application/pdf");
                }
            }

            return View(rpt);
        }

        public ActionResult ChequeIssue()
        {
            return View();
        }
        public ActionResult Items(int? DepartmentId, int? GroupId, int? CategoryId, int? ActivityId, int? CompanyId, bool? showReport, bool? print)
        {
            Items_Report report = new Items_Report(DepartmentId, GroupId, CategoryId, ActivityId, CompanyId);
            report.Parameters["DepartmentId"].Value = DepartmentId > 0 ? DepartmentId : null;
            report.Parameters["ItemGroupId"].Value = GroupId > 0 ? GroupId : null;
            report.Parameters["ItemCategoryId"].Value = CategoryId > 0 ? CategoryId : null;
            report.Parameters["ActivityId"].Value = ActivityId > 0 ? ActivityId : null;
            report.Parameters["CompanyId"].Value = CompanyId > 0 ? CompanyId : null;
            report.RequestParameters = false;
            ViewBag.ShowReport = showReport;
            ViewBag.DepartmentId = DepartmentId;
            ViewBag.GroupId = GroupId;
            ViewBag.CategoryId = CategoryId;
            ViewBag.ActivityId = ActivityId;
            ViewBag.CompanyId = CompanyId;
            if (showReport == true)
            {
                return View(report);
            }
            else if (print == true)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    report.ExportToPdf(ms, new PdfExportOptions() { ShowPrintDialogOnOpen = false });
                    return File(ms.ToArray(), "application/pdf");
                }
            }

            else
            {
                var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                ViewBag.GroupId = new SelectList(db.ItemGroups.Where(b => b.IsActive == true && b.IsDeleted == false)
                    .Select(b => new
                    {
                        Id = b.Id,
                        ArName = b.Code + " - " + b.ArName
                    }), "Id", "ArName", GroupId);
                ViewBag.CategoryId = new SelectList(db.ItemCategories.Where(b => b.IsActive == true && b.IsDeleted == false)
                    .Select(b => new
                    {
                        Id = b.Id,
                        ArName = b.Code + " - " + b.ArName
                    }), "Id", "ArName", CategoryId);
                ViewBag.ActivityId = new SelectList(db.Activities.Where(b => b.IsActive == true && b.IsDeleted == false)
                    .Select(b => new
                    {
                        Id = b.Id,
                        ArName = b.Code + " - " + b.ArName
                    }), "Id", "ArName", ActivityId);
                ViewBag.CompanyId = new SelectList(db.Companies.Where(b => b.IsActive == true && b.IsDeleted == false)
                    .Select(b => new
                    {
                        Id = b.Id,
                        ArName = b.Code + " - " + b.ArName
                    }), "Id", "ArName", CompanyId);
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
                return View();
            }
        }

        public ActionResult FixedAssets(int? DepartmentId, DateTime? DateFrom, DateTime? DateTo, int? ActivityId, int? CompanyId, bool? showReport, bool? print)
        {
            FixedAssets_Report report = new FixedAssets_Report(DepartmentId, DateFrom, DateTo, ActivityId, CompanyId);
            report.Parameters["DepartmentId"].Value = DepartmentId > 0 ? DepartmentId : null;
            report.Parameters["ActivityId"].Value = ActivityId > 0 ? ActivityId : null;
            report.Parameters["CompanyId"].Value = CompanyId > 0 ? CompanyId : null;
            report.Parameters["DateFrom"].Value = DateFrom != null ? DateFrom : null;
            report.Parameters["DateTo"].Value = DateTo != null ? DateTo : null;
            report.RequestParameters = false;
            ViewBag.ShowReport = showReport;
            ViewBag.DepartmentId = DepartmentId;
            ViewBag.ActivityId = ActivityId;
            ViewBag.CompanyId = CompanyId;
            ViewBag.DateFrom = DateFrom;
            ViewBag.DateTo = DateTo;
            if (showReport == true)
            {
                return View(report);
            }
            else if (print == true)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    report.ExportToPdf(ms, new PdfExportOptions() { ShowPrintDialogOnOpen = false });
                    return File(ms.ToArray(), "application/pdf");
                }
            }

            else
            {
                ViewBag.ActivityId = new SelectList(db.Activities.Where(b => b.IsActive == true && b.IsDeleted == false)
                    .Select(b => new
                    {
                        Id = b.Id,
                        ArName = b.Code + " - " + b.ArName
                    }), "Id", "ArName", ActivityId);
                ViewBag.CompanyId = new SelectList(db.Companies.Where(b => b.IsActive == true && b.IsDeleted == false)
                    .Select(b => new
                    {
                        Id = b.Id,
                        ArName = b.Code + " - " + b.ArName
                    }), "Id", "ArName", CompanyId);
                var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);

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
                return View();
            }
        }

        public ActionResult ChequeReceipt()
        {
            return View();
        }
        public ActionResult VendorCurrentBalance(int? departmentId, int? ActivityId, int? CompanyId, bool? showReport, bool? print)
        {
            VendorCurrentBalance_Report report = new VendorCurrentBalance_Report(departmentId, ActivityId, CompanyId);
            report.Parameters["DepartmentId"].Value = departmentId > 0 ? departmentId : null;
            report.Parameters["ActivityId"].Value = ActivityId > 0 ? ActivityId : null;
            report.Parameters["CompanyId"].Value = CompanyId > 0 ? CompanyId : null;
            report.RequestParameters = false;
            ViewBag.ShowReport = showReport;
            ViewBag.Department = departmentId;
            ViewBag.ActivityId = ActivityId;
            ViewBag.CompanyId = CompanyId;
            if (showReport == true)
            {
                return View(report);
            }
            else if (print == true)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    report.ExportToPdf(ms, new PdfExportOptions() { ShowPrintDialogOnOpen = false });
                    return File(ms.ToArray(), "application/pdf");
                }
            }
            else
            {
                var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                SystemSetting systemSetting = db.SystemSettings.Any() ? db.SystemSettings.FirstOrDefault() : new SystemSetting();
                ViewBag.ActivityId = new SelectList(db.Activities.Where(b => b.IsActive == true && b.IsDeleted == false)
                   .Select(b => new
                   {
                       Id = b.Id,
                       ArName = b.Code + " - " + b.ArName
                   }), "Id", "ArName", ActivityId);
                ViewBag.CompanyId = new SelectList(db.Companies.Where(b => b.IsActive == true && b.IsDeleted == false)
                    .Select(b => new
                    {
                        Id = b.Id,
                        ArName = b.Code + " - " + b.ArName
                    }), "Id", "ArName", CompanyId);
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
                return View();
            }
        }
        public ActionResult CashReceiptVoucherDetails(int? id, int? deptId, bool? print, string docNo)
        {
            CashReceiptVoucherDetails_Report rpt = new CashReceiptVoucherDetails_Report();

            if (id != null)
            {
                CashReceiptVoucher civ = db.CashReceiptVouchers.Find(id);
                //SELECT d.Id,
                //d.PropertyUnitNo
                //    FROM PropertyDetail d
                //WHERE d.Id = 345

                rpt.Parameters["Id"].Value = id;
                rpt.Parameters["DocumentNumber"].Value = civ.DocumentNumber;
                rpt.Parameters["DepId"].Value = civ.DepartmentId;
                rpt.RequestParameters = false;
                if (civ.SourceTypeId == 11)
                {
                    rpt.txtproperty.Text = civ.PropertyContract?.Property?.ArName;
                    var bid = civ.CashReceiptVoucherPropertyContractBatches.FirstOrDefault();
                    var props = db.PropertyDetails.Where(t => t.Id == bid.PropertyContractBatch.PropertyContract.PropertyUnitId).Select(t => new
                    {
                        Id = t.Id,
                        PropertyUnitNo = t.PropertyUnitNo ?? "0"
                    }).FirstOrDefault();
                    rpt.txtrinter.Text = civ.PropertyContract?.PropertyRenter?.ArName;
                    rpt.txtunit.Text = props == null ? "" : props.PropertyUnitNo.ToString();  
                    rpt.txtrinter.Visible = true;
                    rpt.txtproperty.Visible = true;
                    rpt.txtunit.Visible = true;
                }
                else
                {
                    rpt.txtrinter.Visible = false;
                    rpt.txtproperty.Visible = false;
                    rpt.txtunit.Visible = false;
                }

            }
            else if (deptId != null && docNo != null)
            {
                CashReceiptVoucher civ = db.CashReceiptVouchers.Where(a => a.DepartmentId == deptId && a.DocumentNumber == docNo).FirstOrDefault();

                rpt.Parameters["Id"].Value = civ.Id;
                rpt.Parameters["DocumentNumber"].Value = civ.DocumentNumber;
                rpt.Parameters["DepId"].Value = civ.DepartmentId;
                rpt.RequestParameters = false;
                if (civ.SourceTypeId == 11)
                {
                    rpt.txtproperty.Text = civ.PropertyContract?.Property?.ArName;
                    var bid = civ.CashReceiptVoucherPropertyContractBatches.FirstOrDefault();
                    var props = db.PropertyDetails.Where(t => t.Id == bid.PropertyContractBatch.PropertyContract.PropertyUnitId).Select(t => new
                    {
                        Id = t.Id,
                        PropertyUnitNo = t.PropertyUnitNo ?? "0"
                    }).FirstOrDefault();
                    rpt.txtrinter.Text = civ.PropertyContract?.PropertyRenter?.ArName;
                    rpt.txtunit.Text = props == null ? "" : props.PropertyUnitNo.ToString();
                    rpt.txtrinter.Visible = true;
                    rpt.txtproperty.Visible = true;
                    rpt.txtunit.Visible = true;
                }
                else
                {
                    rpt.txtrinter.Visible = false;
                    rpt.txtproperty.Visible = false;
                    rpt.txtunit.Visible = false;
                }



            }
            if (print == true)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    rpt.ExportToPdf(ms, new PdfExportOptions() { ShowPrintDialogOnOpen = true });
                    return File(ms.ToArray(), "application/pdf");
                }
            }
            return View(rpt);

        }

        public ActionResult CashTransferDetails(int? id, int? deptId, string docNo, bool? print)
        {
            CashTransferDetails_Report rpt = new CashTransferDetails_Report();
            if (id != null)
            {
                CashTransfer ct = db.CashTransfers.Find(id);
                rpt.Parameters["Id"].Value = id;
                rpt.Parameters["DocumentNumber"].Value = ct.DocumentNumber;
                rpt.Parameters["DepId"].Value = ct.DepartmentId;
                rpt.RequestParameters = false;
            }
            else if (deptId != null && docNo != null)
            {

                CashTransfer ct = db.CashTransfers.Where(a => a.DepartmentId == deptId && a.DocumentNumber == docNo).FirstOrDefault();

                rpt.Parameters["Id"].Value = id;
                rpt.Parameters["DocumentNumber"].Value = ct.DocumentNumber;
                rpt.Parameters["DepId"].Value = ct.DepartmentId;
                rpt.RequestParameters = false;

            }
            if (print == true)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    rpt.ExportToPdf(ms, new PdfExportOptions() { ShowPrintDialogOnOpen = true });
                    return File(ms.ToArray(), "application/pdf");
                }
            }
            return View(rpt);

        }

        public ActionResult ChequeIssueDetails(int? id, int? deptId, string docNo, bool? print)
        {
            ChequeIssueDetails_Report rpt = new ChequeIssueDetails_Report();
            if (id != null)
            {
                ChequeIssue ct = db.ChequeIssues.Find(id);
                rpt.Parameters["Id"].Value = id;
                rpt.Parameters["DocumentNumber"].Value = ct.DocumentNumber;
                rpt.Parameters["DepId"].Value = ct.DepartmentId;
                rpt.RequestParameters = false;
            }
            else if (deptId != null && docNo != null)
            {
                ChequeIssue ct = db.ChequeIssues.Where(a => a.DepartmentId == deptId && a.DocumentNumber == docNo).FirstOrDefault();

                rpt.Parameters["Id"].Value = id;
                rpt.Parameters["DocumentNumber"].Value = ct.DocumentNumber;
                rpt.Parameters["DepId"].Value = ct.DepartmentId;
                rpt.RequestParameters = false;
            }
            if (print == true)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    rpt.ExportToPdf(ms, new PdfExportOptions() { ShowPrintDialogOnOpen = true });
                    return File(ms.ToArray(), "application/pdf");
                }
            }
            return View(rpt);

        }

        public ActionResult ChequeReceiptDetails(int? id, int? deptId, string docNo, bool? print)
        {
            ChequeReceiptDetails_Report rpt = new ChequeReceiptDetails_Report();
            if (id != null)
            {
                ChequeReceipt ct = db.ChequeReceipts.Find(id);
                rpt.Parameters["Id"].Value = id;
                rpt.Parameters["DocumentNumber"].Value = ct.DocumentNumber;
                rpt.Parameters["DepId"].Value = ct.DepartmentId;
                rpt.RequestParameters = false;
            }
            else if (deptId != null && docNo != null)
            {
                ChequeReceipt ct = db.ChequeReceipts.Where(a => a.DepartmentId == deptId && a.DocumentNumber == docNo).FirstOrDefault();

                rpt.Parameters["Id"].Value = id;
                rpt.Parameters["DocumentNumber"].Value = ct.DocumentNumber;
                rpt.Parameters["DepId"].Value = ct.DepartmentId;
                rpt.RequestParameters = false;

            }
            if (print == true)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    rpt.ExportToPdf(ms, new PdfExportOptions() { ShowPrintDialogOnOpen = true });
                    return File(ms.ToArray(), "application/pdf");
                }
            }
            return View(rpt);

        }

        public ActionResult StockIssueVoucher(int? id, int? deptId, string docNo, DateTime? From, DateTime? To, int? ActivityId, int? CompanyId, bool? showReport, bool? print)
        {
            StockIssueVoucher_Report rpt = new StockIssueVoucher_Report(id, deptId, docNo, From, To, ActivityId, CompanyId);

            if (showReport == true)
            {
                if (id != null)
                {
                    StockIssueVoucher ct = db.StockIssueVouchers.Find(id);
                    rpt.RequestParameters = false;
                    rpt.Parameters["Id"].Value = id > 0 ? id : 0;
                    rpt.Parameters["DocNum"].Value = ct.DocumentNumber == null || ct.DocumentNumber == "" ? "" : ct.DocumentNumber;
                    rpt.Parameters["DepId"].Value = ct.DepartmentId > 0 ? ct.DepartmentId : 0;
                    rpt.Parameters["ActivityId"].Value = ActivityId > 0 ? ActivityId : 0;
                    rpt.Parameters["CompanyId"].Value = CompanyId > 0 ? CompanyId : 0;
                    rpt.Parameters["DateFrom"].Value = From != null ? From : null;
                    rpt.Parameters["DateTo"].Value = To != null ? To : null;
                    rpt.RequestParameters = false;
                    ViewBag.ShowReport = showReport;
                    ViewBag.DepartmentId = deptId;
                    ViewBag.Id = id;
                    ViewBag.DocNum = docNo;
                    ViewBag.From = From;
                    ViewBag.To = To;
                    ViewBag.ActivityId = ActivityId;
                    ViewBag.CompanyId = CompanyId;
                }
                else if (deptId != null && docNo.Length > 0)
                {

                    StockIssueVoucher ct = db.StockIssueVouchers.Where(a => a.DepartmentId == deptId && a.DocumentNumber == docNo).FirstOrDefault();
                    rpt.Parameters["Id"].Value = id;
                    rpt.Parameters["DocNum"].Value = ct.DocumentNumber == null || ct.DocumentNumber == "" ? "" : ct.DocumentNumber;
                    rpt.Parameters["DepId"].Value = ct.DepartmentId;
                    rpt.Parameters["ActivityId"].Value = ActivityId > 0 ? ActivityId : 0;
                    rpt.Parameters["CompanyId"].Value = CompanyId > 0 ? CompanyId : 0;
                    rpt.Parameters["DateFrom"].Value = From != null ? From : null;
                    rpt.Parameters["DateTo"].Value = To != null ? To : null;
                    rpt.RequestParameters = false;
                    ViewBag.ShowReport = showReport;
                    ViewBag.DepartmentId = deptId;
                    ViewBag.Id = id;
                    ViewBag.DocNum = docNo;
                    ViewBag.From = From;
                    ViewBag.To = To;
                    ViewBag.ActivityId = ActivityId;
                    ViewBag.CompanyId = CompanyId;
                }
                else
                {
                    rpt.Parameters["Id"].Value = id > 0 ? id : null;
                    rpt.Parameters["DocNum"].Value = docNo == null || docNo == "" ? "" : docNo;
                    rpt.Parameters["DepId"].Value = deptId > 0 ? deptId : 0;
                    rpt.Parameters["ActivityId"].Value = ActivityId > 0 ? ActivityId : 0;
                    rpt.Parameters["CompanyId"].Value = CompanyId > 0 ? CompanyId : 0;
                    rpt.Parameters["DateFrom"].Value = From != null ? From : null;
                    rpt.Parameters["DateTo"].Value = To != null ? To : null;
                    rpt.RequestParameters = false;
                    ViewBag.ShowReport = showReport;
                    ViewBag.DepartmentId = deptId;
                    ViewBag.Id = id;
                    ViewBag.DocNum = docNo;
                    ViewBag.From = From;
                    ViewBag.To = To;
                    ViewBag.ActivityId = ActivityId;
                    ViewBag.CompanyId = CompanyId;
                }
                return View(rpt);
            }
            else if (print == true)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    rpt.ExportToPdf(ms, new PdfExportOptions() { ShowPrintDialogOnOpen = true });
                    return File(ms.ToArray(), "application/pdf");
                }
            }
            else
            {
                var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                ViewBag.ActivityId = new SelectList(db.Activities.Where(b => b.IsActive == true && b.IsDeleted == false)
                   .Select(b => new
                   {
                       Id = b.Id,
                       ArName = b.Code + " - " + b.ArName
                   }), "Id", "ArName", ActivityId);
                ViewBag.CompanyId = new SelectList(db.Companies.Where(b => b.IsActive == true && b.IsDeleted == false)
                    .Select(b => new
                    {
                        Id = b.Id,
                        ArName = b.Code + " - " + b.ArName
                    }), "Id", "ArName", CompanyId);
                if (userId == 1)
                {
                    ViewBag.DepartmentId = new SelectList(db.Departments.Where(d => d.IsActive == true && d.IsDeleted == false).Select(b => new
                    {
                        Id = b.Id,
                        ArName = b.Code + " - " + b.ArName
                    }), "Id", "ArName", deptId);
                }
                else
                {
                    ViewBag.DepartmentId = new SelectList(db.UserDepartments.Where(d => d.UserId == userId).Select(b => new
                    {
                        Id = b.DepartmentId,
                        ArName = b.Department.Code + " - " + b.Department.ArName
                    }), "Id", "ArName", deptId);
                }
                return View();
            }
        }

        public ActionResult StockReceiptVoucher(int? id, int? deptId, string docNo, bool? print)
        {
            StockReceiptVoucher_Report rpt = new StockReceiptVoucher_Report();
            if (id != null)
            {
                StockReceiptVoucher ct = db.StockReceiptVouchers.Find(id);
                rpt.Parameters["Id"].Value = id;
                rpt.Parameters["DocNum"].Value = ct.DocumentNumber;
                rpt.Parameters["DepId"].Value = ct.DepartmentId;
                rpt.RequestParameters = false;
            }
            else if (deptId != null && docNo != null)
            {
                StockReceiptVoucher ct = db.StockReceiptVouchers.Where(a => a.DepartmentId == deptId && a.DocumentNumber == docNo).FirstOrDefault();
                rpt.Parameters["Id"].Value = id;
                rpt.Parameters["DocNum"].Value = ct.DocumentNumber;
                rpt.Parameters["DepId"].Value = ct.DepartmentId;
                rpt.RequestParameters = false;

            }
            if (print == true)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    rpt.ExportToPdf(ms, new PdfExportOptions() { ShowPrintDialogOnOpen = true });
                    return File(ms.ToArray(), "application/pdf");
                }
            }
            return View(rpt);

        }

        public ActionResult SalesInvoice(int? id, int? deptId, string docNo, bool? print, string paperKind, bool? printPrice)
        {

            SalesInvoice_Report rpt = new SalesInvoice_Report(paperKind, printPrice);

            if (id != null)
            {
                SalesInvoice si = db.SalesInvoices.Find(id);
                rpt.Parameters["Id"].Value = id;
                rpt.Parameters["DocNum"].Value = si.DocumentNumber;
                rpt.Parameters["DepId"].Value = si.DepartmentId;
                rpt.Parameters["CustomerId"].Value = si.VendorOrCustomerId;

                rpt.RequestParameters = false;
            }
            else if (deptId != null && docNo != null)
            {
                SalesInvoice si = db.SalesInvoices.Where(a => a.DepartmentId == deptId && a.DocumentNumber == docNo).FirstOrDefault();

                rpt.Parameters["DocNum"].Value = si.DocumentNumber;
                rpt.Parameters["DepId"].Value = si.DepartmentId;
                rpt.Parameters["CustomerId"].Value = si.VendorOrCustomerId;

                rpt.RequestParameters = false;
            }
            if (print == true)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    rpt.ExportToPdf(ms, new PdfExportOptions() { ShowPrintDialogOnOpen = true });
                    return File(ms.ToArray(), "application/pdf");
                }
            }
            return View(rpt);
        }
        public ActionResult SalesQuotation(int? id, int? deptId, string docNo, bool? print, string paperKind, bool? printPrice)
        {
            string domainName = Request.Url.GetLeftPart(UriPartial.Authority);
            ViewBag.domainName = domainName;
            SalesQuotation_Report rpt = new SalesQuotation_Report(paperKind, printPrice, domainName);
            if (id != null)
            {
                SalesQuotation si = db.SalesQuotations.Find(id);
                rpt.Parameters["Id"].Value = id;
                rpt.Parameters["DocNum"].Value = si.DocumentNumber;
                rpt.Parameters["DepId"].Value = si.DepartmentId;
                rpt.Parameters["CustomerId"].Value = si.VendorOrCustomerId;
                rpt.RequestParameters = false;
            }
            else if (deptId != null && docNo != null)
            {
                SalesQuotation si = db.SalesQuotations.Where(a => a.DepartmentId == deptId && a.DocumentNumber == docNo).FirstOrDefault();
                rpt.Parameters["DocNum"].Value = si.DocumentNumber;
                rpt.Parameters["DepId"].Value = si.DepartmentId;
                rpt.Parameters["CustomerId"].Value = si.VendorOrCustomerId;
                rpt.RequestParameters = false;
            }
            if (print == true)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    rpt.ExportToPdf(ms, new PdfExportOptions() { ShowPrintDialogOnOpen = true });
                    return File(ms.ToArray(), "application/pdf");
                }
            }
            return View(rpt);
        }
        public ActionResult KimoSalesInvoice(int? id, int? deptId, string docNo, bool? print, string paperKind, bool? printPrice)
        {
            KimoSalesInvoice_Report rpt = new KimoSalesInvoice_Report();

            if (id != null)
            {
                SalesInvoice si = db.SalesInvoices.Find(id);
                rpt.Parameters["Id"].Value = id;
                rpt.Parameters["DocNum"].Value = si.DocumentNumber;
                rpt.Parameters["DepId"].Value = si.DepartmentId;
                rpt.Parameters["CustomerId"].Value = si.VendorOrCustomerId;

                rpt.RequestParameters = false;
            }
            else if (deptId != null && docNo != null)
            {
                SalesInvoice si = db.SalesInvoices.Where(a => a.DepartmentId == deptId && a.DocumentNumber == docNo).FirstOrDefault();

                rpt.Parameters["DocNum"].Value = si.DocumentNumber;
                rpt.Parameters["DepId"].Value = si.DepartmentId;
                rpt.Parameters["CustomerId"].Value = si.VendorOrCustomerId;

                rpt.RequestParameters = false;
            }
            if (print == true)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    rpt.ExportToPdf(ms, new PdfExportOptions() { ShowPrintDialogOnOpen = true });
                    return File(ms.ToArray(), "application/pdf");
                }
            }
            return View(rpt);

        }
        public ActionResult PosClosing(int? id, bool? print, string paperKind, bool? printPrice)
        {
            PosClosing_Report rpt = new PosClosing_Report(paperKind, printPrice);

            if (id != null)
            {
                PosClosing si = db.PosClosings.Find(id);
                rpt.Parameters["Id"].Value = id;

                rpt.RequestParameters = false;
            }
            if (print == true)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    rpt.ExportToPdf(ms, new PdfExportOptions() { ShowPrintDialogOnOpen = true });
                    return File(ms.ToArray(), "application/pdf");
                }
            }
            return View(rpt);

        }
        public ActionResult PosReceiptVoucher(int? id, bool? print, string paperKind, bool? printPrice)
        {
            PosReceiptVoucher_Report rpt = new PosReceiptVoucher_Report(paperKind, printPrice);

            if (id != null)
            {
                PosReceiptVoucher si = db.PosReceiptVouchers.Find(id);
                rpt.Parameters["Id"].Value = id;

                rpt.RequestParameters = false;
            }
            if (print == true)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    rpt.ExportToPdf(ms, new PdfExportOptions() { ShowPrintDialogOnOpen = true });
                    return File(ms.ToArray(), "application/pdf");
                }
            }
            return View(rpt);

        }
        [AllowAnonymous]
        public ActionResult CarEntrance(int? id, int? deptId, string docNo, bool? print, string paperKind)
        {
            string domainName = Request.Url.GetLeftPart(UriPartial.Authority);
            CarEntrance_Report rpt = new CarEntrance_Report(domainName);

            if (id != null)
            {
                CarEntrance si = db.CarEntrances.Find(id);
                rpt.Parameters["Id"].Value = id;
                rpt.Parameters["DocNum"].Value = si.DocumentNumber;
                rpt.Parameters["DepId"].Value = si.DepartmentId;
                rpt.Parameters["CustomerId"].Value = si.CustomerId;

                rpt.RequestParameters = false;
            }
            else if (deptId != null && docNo != null)
            {
                CarEntrance si = db.CarEntrances.Where(a => a.DepartmentId == deptId && a.DocumentNumber == docNo).FirstOrDefault();

                rpt.Parameters["DocNum"].Value = si.DocumentNumber;
                rpt.Parameters["DepId"].Value = si.DepartmentId;
                rpt.Parameters["CustomerId"].Value = si.CustomerId;

                rpt.RequestParameters = false;
            }
            if (print == true)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    rpt.ExportToPdf(ms, new PdfExportOptions() { ShowPrintDialogOnOpen = true });
                    return File(ms.ToArray(), "application/pdf");
                }
            }
            return View(rpt);

        }

        public ActionResult CarEntranceWorkOrder(int? id, int? deptId, string docNo, bool? print, string paperKind)
        {
            CarEntranceWorkOrder_Report rpt = new CarEntranceWorkOrder_Report();

            if (id != null)
            {
                CarEntrance si = db.CarEntrances.Find(id);
                rpt.Parameters["Id"].Value = id;
                rpt.Parameters["DocNum"].Value = si.DocumentNumber;
                rpt.Parameters["DepId"].Value = si.DepartmentId;
                rpt.Parameters["CustomerId"].Value = si.CustomerId;

                rpt.RequestParameters = false;
            }
            else if (deptId != null && docNo != null)
            {
                CarEntrance si = db.CarEntrances.Where(a => a.DepartmentId == deptId && a.DocumentNumber == docNo).FirstOrDefault();

                rpt.Parameters["DocNum"].Value = si.DocumentNumber;
                rpt.Parameters["DepId"].Value = si.DepartmentId;
                rpt.Parameters["CustomerId"].Value = si.CustomerId;

                rpt.RequestParameters = false;
            }
            if (print == true)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    rpt.ExportToPdf(ms, new PdfExportOptions() { ShowPrintDialogOnOpen = true });
                    return File(ms.ToArray(), "application/pdf");
                }
            }
            return View(rpt);

        }

        private FileResult ExportDocument(byte[] document, string format, string fileName, bool isInline)
        {
            string contentType;
            string disposition = (isInline) ? "inline" : "attachment";

            switch (format.ToLower())
            {
                case "docx":
                    contentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
                    break;
                case "xls":
                    contentType = "application/vnd.ms-excel";
                    break;
                case "xlsx":
                    contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                    break;
                case "mht":
                    contentType = "message/rfc822";
                    break;
                case "html":
                    contentType = "text/html";
                    break;
                case "txt":
                case "csv":
                    contentType = "text/plain";
                    break;
                case "png":
                    contentType = "image/png";
                    break;
                default:
                    contentType = String.Format("application/{0}", format);
                    break;
            }

            Response.AddHeader("Content-Disposition", String.Format("{0}; filename={1}", disposition, fileName));
            return File(document, contentType);
        }




        public async Task<ActionResult> CashierInvoice(int id, bool? print)
        {
            //SystemSetting systemSetting = db.SystemSettings.Any() ? db.SystemSettings.FirstOrDefault() : new SystemSetting();
            //var PrintEInvoiceInsteadOfSalesInvoice = systemSetting.PrintEInvoiceInsteadOfSalesInvoice;
            //if(PrintEInvoiceInsteadOfSalesInvoice==true)
            //{
            //    return E_Invoice(id, print);
            //}
            var invoiceSampleNo = db.SystemSettings.Select(s => s.InvoiceSampleNo).SingleOrDefault();

            if (invoiceSampleNo != null && invoiceSampleNo.Value == 2)
            {
                byte[] inputByteArray = Encoding.UTF8.GetBytes(id.ToString());
                byte[] rgbIV = { 0x21, 0x43, 0x56, 0x87, 0x10, 0xfd, 0xea, 0x1c };
                byte[] key = { };
                key = Encoding.UTF8.GetBytes("Z4a2rX3T");
                DESCryptoServiceProvider des = new DESCryptoServiceProvider();
                MemoryStream ms0 = new MemoryStream();
                CryptoStream cs = new CryptoStream(ms0, des.CreateEncryptor(key, rgbIV), CryptoStreamMode.Write);
                cs.Write(inputByteArray, 0, inputByteArray.Length);
                cs.FlushFinalBlock();
                var _id = Convert.ToBase64String(ms0.ToArray()).Replace("+", "_pl_");//.Replace("=", "_eq_").Replace("/", "_sl_").Replace(@"\", "_bsl_");

                Response.Redirect("~/Report/CashierInvoice2/" + _id + "?print=" + print);
            }
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            UserRepository userRepository = new UserRepository(db);
            int roleId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("RoleId").Value);
            var DisplayFactoryPrice = userId == 1 ? true : await db.UserPrivileges.Where(u => u.PageAction.PageId == 58 && u.PageAction.EnName == "DisplayFactoryPrice" && u.PageAction.Action == "DisplayFactoryPrice" && u.UserId == userId).Select(x => x.Privileged).FirstOrDefaultAsync();
            DisplayFactoryPrice = DisplayFactoryPrice ?? await db.RolePrivileges.Where(u => u.PageAction.PageId == 58 && u.PageAction.EnName == "DisplayFactoryPrice" && u.PageAction.Action == "DisplayFactoryPrice" && u.RoleId == roleId).Select(x => x.Privileged).FirstOrDefaultAsync();
            //var displayFactoryPrice = await userRepository.HasActionPrivilege(userId, "DisplayFactoryPrice", "DisplayFactoryPrice");
            CashierInvoice_Report rpt = new CashierInvoice_Report((bool)DisplayFactoryPrice);
            SalesInvoice si = await db.SalesInvoices.FindAsync(id);
            rpt.Parameters["Id"].Value = id;
            rpt.Parameters["DepId"].Value = si.DepartmentId;
            rpt.Parameters["DocNum"].Value = si.DocumentNumber;
            rpt.Parameters["CashierName"].Value = si.ERPUser.Name;
            decimal change = Math.Round(((si.Paid ?? 0) - (si.TotalAfterTaxes ?? 0)), 2);
            rpt.Parameters["ClientChange"].Value = change > 0 ? change : 0;

            string[] salesMan = si.SalesOrdersSalesInvoices.Select(x => x.SalesOrder.ERPUser.Name).Distinct().ToArray();

            rpt.Parameters["SalesManName"].Value = string.Join(", ", salesMan);

            rpt.RequestParameters = false;
            if (print == true)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    //rpt.ExportToPdf(ms, new PdfExportOptions() { ShowPrintDialogOnOpen = true });
                    //return File(ms.ToArray(), "application/pdf");
                    rpt.ExportToPdf(ms, new PdfExportOptions() { ShowPrintDialogOnOpen = true });
                    return ExportDocument(ms.ToArray(), "pdf", "Report.pdf", true);
                }
            }
            using (MemoryStream ms = new MemoryStream())
            {
                rpt.ExportToPdf(ms, new PdfExportOptions() { ShowPrintDialogOnOpen = true });
                return ExportDocument(ms.ToArray(), "pdf", "Report.pdf", true);
            }
            // return View(rpt);

        }

        [AllowAnonymous]
        public async Task<ActionResult> CashierInvoice2(string id, bool? print)
        {
            SystemSetting systemSetting = db.SystemSettings.Any() ? db.SystemSettings.FirstOrDefault() : new SystemSetting();
            var PrintEInvoiceInsteadOfSalesInvoice = systemSetting.PrintEInvoiceInsteadOfSalesInvoice;
            id = id.Replace("_pl_", "+");
            byte[] inputByteArray = new byte[id.Length + 1];
            byte[] rgbIV = { 0x21, 0x43, 0x56, 0x87, 0x10, 0xfd, 0xea, 0x1c };
            byte[] key = { };

            key = System.Text.Encoding.UTF8.GetBytes("Z4a2rX3T");
            DESCryptoServiceProvider des = new DESCryptoServiceProvider();

            inputByteArray = Convert.FromBase64String(id);
            MemoryStream ms0 = new MemoryStream();
            CryptoStream cs = new CryptoStream(ms0, des.CreateDecryptor(key, rgbIV), CryptoStreamMode.Write);
            cs.Write(inputByteArray, 0, inputByteArray.Length);
            cs.FlushFinalBlock();
            Encoding encoding = Encoding.UTF8;
            // var _Id = encoding.GetString(ms.ToArray());
            var _Id = Int32.Parse(encoding.GetString(ms0.ToArray()));
            ViewBag.id = id;
            if (PrintEInvoiceInsteadOfSalesInvoice == true)
            {
                return E_Invoice(_Id, print);
            }
            string domainName = Request.Url.GetLeftPart(UriPartial.Authority);
            ViewBag.domainName = domainName;
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            UserRepository userRepository = new UserRepository(db);
            int roleId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("RoleId").Value);
            var DisplayFactoryPrice = userId == 1 ? true : await db.UserPrivileges.Where(u => u.PageAction.PageId == 58 && u.PageAction.EnName == "DisplayFactoryPrice" && u.PageAction.Action == "DisplayFactoryPrice" && u.UserId == userId).Select(x => x.Privileged).FirstOrDefaultAsync();
            DisplayFactoryPrice = DisplayFactoryPrice ?? await db.RolePrivileges.Where(u => u.PageAction.PageId == 58 && u.PageAction.EnName == "DisplayFactoryPrice" && u.PageAction.Action == "DisplayFactoryPrice" && u.RoleId == roleId).Select(x => x.Privileged).FirstOrDefaultAsync();
            //var displayFactoryPrice = await userRepository.HasActionPrivilege(userId, "DisplayFactoryPrice", "DisplayFactoryPrice");
            CashierInvoice2_Report rpt = new CashierInvoice2_Report(id, (bool)DisplayFactoryPrice, domainName);
            SalesInvoice si = await db.SalesInvoices.FindAsync(_Id);
            rpt.Parameters["Id"].Value = si.Id;
            rpt.Parameters["DepId"].Value = si.DepartmentId;
            rpt.Parameters["DocNum"].Value = si.DocumentNumber;
            rpt.Parameters["CashierName"].Value = si.ERPUser.Name;
            decimal change = Math.Round(((si.Paid ?? 0) - (si.TotalAfterTaxes ?? 0)), 2);
            rpt.Parameters["ClientChange"].Value = change > 0 ? change : 0;

            string[] salesMan = si.SalesOrdersSalesInvoices.Select(x => x.SalesOrder.ERPUser.Name).Distinct().ToArray();

            rpt.Parameters["SalesManName"].Value = string.Join(", ", salesMan);

            rpt.RequestParameters = false;
            if (print == true)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    //rpt.ExportToPdf(ms, new PdfExportOptions() { ShowPrintDialogOnOpen = true });
                    //return File(ms.ToArray(), "application/pdf");
                    rpt.ExportToPdf(ms, new PdfExportOptions() { ShowPrintDialogOnOpen = true });
                    return ExportDocument(ms.ToArray(), "pdf", "Report.pdf", true);
                }
            }
            using (MemoryStream ms = new MemoryStream())
            {
                rpt.ExportToPdf(ms, new PdfExportOptions() { ShowPrintDialogOnOpen = true });
                return ExportDocument(ms.ToArray(), "pdf", "Report.pdf", true);
            }
            // return View(rpt);

        }

        public ActionResult ElbarakaSalesReturn(int? id, string docNo, bool? print, string paperKind)
        {
            PurchaseRequestDepartmentQty_Report rpt = new PurchaseRequestDepartmentQty_Report();



            if (id != null)
            {
                SalesReturn si = db.SalesReturns.Find(id);
                rpt.Parameters["Id"].Value = id;
                rpt.RequestParameters = false;

            }
            else if (id == null && docNo != null)
            {
                SalesReturn si = db.SalesReturns.Where(a => a.DocumentNumber == docNo).FirstOrDefault();

                rpt.Parameters["DocNo"].Value = si.DocumentNumber;


                rpt.RequestParameters = false;
            }
            if (print == true)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    rpt.ExportToPdf(ms, new PdfExportOptions() { ShowPrintDialogOnOpen = true });
                    return File(ms.ToArray(), "application/pdf");
                }
            }
            return View(rpt);
        }
        public ActionResult PurchaseInvoice(int? id, int? deptId, string docNo, bool? print, string paperKind, bool? printPrice, bool? printSerial)
        {
            PurchaseInvoice_Report rpt = new PurchaseInvoice_Report();
            if (id != null)
            {
                PurchaseInvoice si = db.PurchaseInvoices.Find(id);
                rpt.Parameters["Id"].Value = id;
                rpt.Parameters["DocNum"].Value = si.DocumentNumber;
                rpt.Parameters["DepId"].Value = si.DepartmentId;
                rpt.Parameters["VendorId"].Value = si.VendorOrCustomerId;

                rpt.RequestParameters = false;

            }
            else if (deptId != null && docNo != null)
            {
                PurchaseInvoice si = db.PurchaseInvoices.Where(a => a.DepartmentId == deptId && a.DocumentNumber == docNo).FirstOrDefault();
                rpt.Parameters["Id"].Value = id;
                rpt.Parameters["DocNum"].Value = si.DocumentNumber;
                rpt.Parameters["DepId"].Value = si.DepartmentId;
                rpt.Parameters["VendorId"].Value = si.VendorOrCustomerId;
                rpt.RequestParameters = false;

            }
            if (print == true)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    rpt.ExportToPdf(ms, new PdfExportOptions() { ShowPrintDialogOnOpen = true });
                    return File(ms.ToArray(), "application/pdf");
                }
            }
            return View(rpt);

        }

        public ActionResult EachItemQuantityWarehouses()
        {
            return View();
        }

        public ActionResult PurchasesOfVendor()
        {
            return View();
        }
        public ActionResult ItemQuantities(int? DepartmentId, int? WarehouseId, int? GroupId, int? CategoryId, int? CustomerGroupId, int? ActivityId, int? CompanyId, bool? showReport, bool? print)
        {
            ItemQuantities_Report2 report = new ItemQuantities_Report2(DepartmentId, WarehouseId, GroupId, CategoryId, CustomerGroupId, ActivityId, CompanyId);
            report.Parameters["DepId"].Value = DepartmentId > 0 ? DepartmentId : null;
            report.Parameters["WarehouseId"].Value = WarehouseId > 0 ? WarehouseId : null;
            report.Parameters["ItemGroupId"].Value = GroupId > 0 ? GroupId : null;
            report.Parameters["ItemCategoryId"].Value = CategoryId > 0 ? CategoryId : null;
            report.Parameters["CustomersGroupId"].Value = CustomerGroupId > 0 ? CustomerGroupId : null;
            report.Parameters["ActivityId"].Value = ActivityId > 0 ? ActivityId : null;
            report.Parameters["CompanyId"].Value = CompanyId > 0 ? CompanyId : null;
            report.RequestParameters = false;
            ViewBag.ShowReport = showReport;
            ViewBag.DepartmentId = DepartmentId;
            ViewBag.WarehouseId = WarehouseId;
            ViewBag.GroupId = GroupId;
            ViewBag.CategoryId = CategoryId;
            ViewBag.CustomerGroupId = CustomerGroupId;
            ViewBag.ActivityId = ActivityId;
            ViewBag.CompanyId = CompanyId;

            if (showReport == true)
            {
                return View(report);
            }
            else if (print == true)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    report.ExportToPdf(ms, new PdfExportOptions() { ShowPrintDialogOnOpen = false });
                    return File(ms.ToArray(), "application/pdf");
                }
            }
            else
            {
                var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                ViewBag.GroupId = new SelectList(db.ItemGroups.Where(b => b.IsActive == true && b.IsDeleted == false)
                    .Select(b => new
                    {
                        Id = b.Id,
                        ArName = b.Code + " - " + b.ArName
                    }), "Id", "ArName", GroupId);
                ViewBag.CategoryId = new SelectList(db.ItemCategories.Where(b => b.IsActive == true && b.IsDeleted == false)
                    .Select(b => new
                    {
                        Id = b.Id,
                        ArName = b.Code + " - " + b.ArName
                    }), "Id", "ArName", CategoryId);
                ViewBag.CustomerGroupId = new SelectList(db.CustomersGroups.Where(b => b.IsActive == true && b.IsDeleted == false)
                    .Select(b => new
                    {
                        Id = b.Id,
                        ArName = b.Code + " - " + b.ArName
                    }), "Id", "ArName", CustomerGroupId);
                ViewBag.ActivityId = new SelectList(db.Activities.Where(b => b.IsActive == true && b.IsDeleted == false)
                  .Select(b => new
                  {
                      Id = b.Id,
                      ArName = b.Code + " - " + b.ArName
                  }), "Id", "ArName", ActivityId);
                ViewBag.CompanyId = new SelectList(db.Companies.Where(b => b.IsActive == true && b.IsDeleted == false)
                    .Select(b => new
                    {
                        Id = b.Id,
                        ArName = b.Code + " - " + b.ArName
                    }), "Id", "ArName", CompanyId);

                ViewBag.WarehouseId = new SelectList(db.Warehouses.Where(b => b.IsActive == true && b.IsDeleted == false && (userId == 1
                || db.UserWareHouses.Where(a => a.UserId == 1 || a.UserId == userId).Any(a => a.WareHouseId == b.Id))
                ).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName", WarehouseId);
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
                return View();
            }
        }

        public ActionResult PurchaseRequestDepartmentQty()
        {
            return View();
        }
        public ActionResult SalesInvoiceDepartmentQty()
        {
            return View();
        }
        public ActionResult DamagedItemsDepartmentQty()
        {
            return View();
        }
        public ActionResult FactorySalesDepartmentQty()
        {
            return View();
        }
        public ActionResult DepartmentItemGroupsQty()
        {
            return View();
        }
        public ActionResult StockTransferVoucher(int? id, int? deptId, string docNo, bool? print)
        {
            StockTransferVoucher ct = new StockTransferVoucher();
            StockTransferVoucherDetails_Report rpt = new StockTransferVoucherDetails_Report();
            if (id != null)
            {
                ct = db.StockTransferVouchers.Find(id);
                rpt.Parameters["Id"].Value = id;
                rpt.Parameters["DocNum"].Value = ct.DocumentNumber;
                rpt.Parameters["DepId"].Value = ct.DepartmentId;
                rpt.RequestParameters = false;
            }
            else if (deptId != null && docNo != null)
            {
                ct = db.StockTransferVouchers.Where(a => a.DepartmentId == deptId && a.DocumentNumber == docNo).FirstOrDefault();
                rpt.Parameters["Id"].Value = id;
                rpt.Parameters["DocNum"].Value = ct.DocumentNumber;
                rpt.Parameters["DepId"].Value = ct.DepartmentId;
                rpt.RequestParameters = false;

            }


            if (print == true)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    rpt.ExportToPdf(ms, new PdfExportOptions() { ShowPrintDialogOnOpen = true });
                    return File(ms.ToArray(), "application/pdf");
                }
            }
            return View(rpt);
        }

        public ActionResult InventoryVoucher(int? id, int? deptId, string docNo, bool? print)
        {
            InventoryVoucherDetails_Report rpt = new InventoryVoucherDetails_Report();
            if (id != null)
            {
                InventoryVoucher ct = db.InventoryVouchers.Find(id);
                rpt.Parameters["Id"].Value = id;
                rpt.Parameters["DocNum"].Value = ct.DocumentNumber;
                rpt.Parameters["DepId"].Value = ct.DepartmentId;
                rpt.RequestParameters = false;
            }
            else if (deptId != null && docNo != null)
            {
                InventoryVoucher ct = db.InventoryVouchers.Where(a => a.DepartmentId == deptId && a.DocumentNumber == docNo).FirstOrDefault();

                rpt.Parameters["Id"].Value = id;
                rpt.Parameters["DocNum"].Value = ct.DocumentNumber;
                rpt.Parameters["DepId"].Value = ct.DepartmentId;
                rpt.RequestParameters = false;
            }
            if (print == true)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    rpt.ExportToPdf(ms, new PdfExportOptions() { ShowPrintDialogOnOpen = true });
                    return File(ms.ToArray(), "application/pdf");
                }
            }
            return View(rpt);
        }

        public ActionResult SalesTaxes()
        {
            return View();
        }

        public ActionResult PurchaseTaxes()
        {
            return View();
        }

        public ActionResult ItemTransaction(int? itemId, int? departmentId, DateTime? dateFrom, DateTime? dateTo, int? warehouseId, int? itemCategoryId, int? itemGroupId, string docType, int? ActivityId, int? CompanyId, bool? showReport, bool? print)
        {
            ItemTransaction_Report report = new ItemTransaction_Report(itemId, departmentId, dateFrom, dateTo, warehouseId, docType, itemGroupId, itemCategoryId, ActivityId, CompanyId);
            report.Parameters["ItemId"].Value = itemId > 0 ? itemId : null;
            report.Parameters["DepId"].Value = departmentId > 0 ? departmentId : null;
            report.Parameters["DateFrom"].Value = dateFrom;
            report.Parameters["DateTo"].Value = dateTo;
            report.Parameters["WarehouseId"].Value = warehouseId > 0 ? warehouseId : null;
            report.Parameters["DocType"].Value = docType;
            report.Parameters["ItemGroupId"].Value = itemGroupId > 0 ? itemGroupId : 0;
            report.Parameters["ItemCategoryId"].Value = itemCategoryId > 0 ? itemCategoryId : 0;
            report.Parameters["ActivityId"].Value = ActivityId > 0 ? ActivityId : 0;
            report.Parameters["CompanyId"].Value = CompanyId > 0 ? CompanyId : 0;

            report.RequestParameters = false;
            ViewBag.ShowReport = showReport;

            ViewBag.ItemId = itemId;
            ViewBag.DepartmentId = departmentId;
            ViewBag.DateFrom = dateFrom;
            ViewBag.DateTo = dateTo;
            ViewBag.WarehouseId = warehouseId;
            ViewBag.DocType = docType;
            ViewBag.ItemGroupId = itemGroupId;
            ViewBag.ItemCategoryId = itemCategoryId;
            ViewBag.ActivityId = ActivityId;
            ViewBag.CompanyId = CompanyId;

            if (showReport == true)
            {
                return View(report);
            }
            else if (print == true)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    report.ExportToPdf(ms, new PdfExportOptions() { ShowPrintDialogOnOpen = false });
                    return File(ms.ToArray(), "application/pdf");
                }
            }
            else
            {
                ViewBag.ActivityId = new SelectList(db.Activities.Where(b => b.IsActive == true && b.IsDeleted == false)
                 .Select(b => new
                 {
                     Id = b.Id,
                     ArName = b.Code + " - " + b.ArName
                 }), "Id", "ArName", ActivityId);
                ViewBag.CompanyId = new SelectList(db.Companies.Where(b => b.IsActive == true && b.IsDeleted == false)
                    .Select(b => new
                    {
                        Id = b.Id,
                        ArName = b.Code + " - " + b.ArName
                    }), "Id", "ArName", CompanyId);

                var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                SystemSetting systemSetting = db.SystemSettings.Any() ? db.SystemSettings.FirstOrDefault() : new SystemSetting();
                ViewBag.Item = new SelectList(db.Items.Where(a => (a.IsActive == true && a.IsDeleted == false)).Select(b => new
                {
                    b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName", itemId);
                ViewBag.DocType = new SelectList(new List<dynamic>
                {
                new { Id = "سند بيع", ArName = "بيع" },
                new { Id = "سند شراء", ArName = "شراء" },
                new { Id = "رصيد افتتاحي", ArName = "رصيد افتتاحي" },
                new { Id = "سند صرف - تحويل مخزنى", ArName = "سند صرف - تحويل مخزنى" },
                new { Id = "سند توريد - تحويل مخزنى", ArName = "سند توريد - تحويل مخزنى" },
                new { Id = "سند صرف - أمر تصنيع", ArName = "سند صرف - أمر تصنيع" },
                new { Id = "سند توريد - أمر تصنيع", ArName = "سند توريد - أمر تصنيع" },
                new { Id = "مرتجع بيع", ArName = "مرتجع بيع" },
                new { Id = "مرتجع شراء", ArName = "مرتجع شراء" },
                new { Id = "سند صرف - تالف", ArName = "سند صرف - تالف" },
                new { Id = "سند صرف - فاقد", ArName = "سند صرف - فاقد" }
                }, "Id", "ArName");
                ViewBag.ItemCategoryId = new SelectList(db.ItemCategories.Where(a => (a.IsActive == true && a.IsDeleted == false)).Select(b => new
                {
                    b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName", itemCategoryId);
                ViewBag.ItemGroupId = new SelectList(db.ItemGroups.Where(a => (a.IsActive == true && a.IsDeleted == false)).Select(b => new
                {
                    b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName", itemGroupId);
                ViewBag.WarehouseId = new SelectList(db.Warehouses.Where(b => b.IsActive == true && b.IsDeleted == false && (userId == 1
                || db.UserWareHouses.Where(a => a.UserId == 1 || a.UserId == userId).Any(a => a.WareHouseId == b.Id))
                ).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName", warehouseId);
                if (userId == 1)
                {
                    ViewBag.DepartmentId = new SelectList(db.Departments.Where(d => d.IsActive == true && d.IsDeleted == false).Select(b => new
                    {
                        Id = b.Id,
                        ArName = b.Code + " - " + b.ArName
                    }), "Id", "ArName", departmentId);
                    //ViewBag.WarehouseId = new SelectList(db.Warehouses.Where(b => b.IsActive == true && b.IsDeleted == false && b.DepartmentId == systemSetting.DefaultDepartmentId).Select(b => new
                    //{
                    //    Id = b.Id,
                    //    ArName = b.Code + " - " + b.ArName
                    //}), "Id", "ArName", warehouseId);
                }
                else
                {
                    ViewBag.DepartmentId = new SelectList(db.UserDepartments.Where(d => d.UserId == userId).Select(b => new
                    {
                        Id = b.DepartmentId,
                        ArName = b.Department.Code + " - " + b.Department.ArName
                    }), "Id", "ArName", departmentId);
                    //ViewBag.WarehouseId = new SelectList(db.UserWareHouses.Where(b => b.UserId == userId && b.Warehouse.IsDeleted == false && b.Warehouse.IsActive == true && b.Warehouse.DepartmentId == systemSetting.DefaultDepartmentId).Select(b => new
                    //{
                    //    Id = b.WareHouseId,
                    //    ArName = b.Warehouse.Code + " - " + b.Warehouse.ArName
                    //}), "Id", "ArName", warehouseId);
                }
                //ViewBag.ItemId = itemId;
                //ViewBag._DepartmentId = departmentId;
                //ViewBag.DateFrom = dateFrom;
                //ViewBag.DateTo = dateTo;
                //ViewBag._WarehouseId = warehouseId;
                //ViewBag._DocType = docType;
                //ViewBag._ItemGroupId = itemGroupId;
                //ViewBag._ItemCategoryId = itemCategoryId;
                //ViewBag.ActivityId = ActivityId;
                //ViewBag.CompanyId = CompanyId;
                return View();
            }
        }

        public ActionResult ItemProfit()
        {
            return View();
        }

        public ActionResult BalanceReview(int? DepartmentId, DateTime? From, DateTime? To, int? ActivityId, int? CompanyId, bool? showReport, bool? print)
        {
            BalanceReview_Report rpt = new BalanceReview_Report(DepartmentId, From, To, ActivityId, CompanyId);
            rpt.Parameters["DepId"].Value = DepartmentId > 0 ? DepartmentId : null;
            rpt.Parameters["FromDate"].Value = From;
            rpt.Parameters["ToDate"].Value = To;
            rpt.Parameters["ActivityId"].Value = ActivityId > 0 ? ActivityId : 0;
            rpt.Parameters["CompanyId"].Value = CompanyId > 0 ? CompanyId : 0;
            rpt.RequestParameters = false;
            ViewBag.ShowReport = showReport;
            ViewBag.From = From;
            ViewBag.To = To;
            ViewBag.DepartmentId = DepartmentId;
            ViewBag.ActivityId = ActivityId;
            ViewBag.CompanyId = CompanyId;
            if (showReport == true)
            {
                return View(rpt);
            }
            else if (print == true)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    rpt.ExportToPdf(ms, new PdfExportOptions() { ShowPrintDialogOnOpen = false });
                    return File(ms.ToArray(), "application/pdf");
                }
            }
            else
            {
                ViewBag.ActivityId = new SelectList(db.Activities.Where(b => b.IsActive == true && b.IsDeleted == false)
                  .Select(b => new
                  {
                      Id = b.Id,
                      ArName = b.Code + " - " + b.ArName
                  }), "Id", "ArName", ActivityId);
                ViewBag.CompanyId = new SelectList(db.Companies.Where(b => b.IsActive == true && b.IsDeleted == false)
                                    .Select(b => new
                                    {
                                        Id = b.Id,
                                        ArName = b.Code + " - " + b.ArName
                                    }), "Id", "ArName", CompanyId);
                var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
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
                return View();
            }
        }


        public ActionResult BalanceReviewDetails(int? DepartmentId, DateTime? From, DateTime? To, int? ActivityId, int? CompanyId, bool? showReport, bool? print)
        {
            BalanceReview2_Report rpt = new BalanceReview2_Report(DepartmentId, From, To, ActivityId, CompanyId);
            rpt.Parameters["DepId"].Value = DepartmentId > 0 ? DepartmentId : null;
            rpt.Parameters["FromDate"].Value = From;
            rpt.Parameters["ToDate"].Value = To;
            rpt.Parameters["ActivityId"].Value = ActivityId > 0 ? ActivityId : 0;
            rpt.Parameters["CompanyId"].Value = CompanyId > 0 ? CompanyId : 0;
            rpt.RequestParameters = false;
            ViewBag.ShowReport = showReport;
            ViewBag.From = From;
            ViewBag.To = To;
            ViewBag.DepartmentId = DepartmentId;
            ViewBag.ActivityId = ActivityId;
            ViewBag.CompanyId = CompanyId;
            if (showReport == true)
            {
                return View(rpt);
            }
            else if (print == true)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    rpt.ExportToPdf(ms, new PdfExportOptions() { ShowPrintDialogOnOpen = false });
                    return File(ms.ToArray(), "application/pdf");
                }
            }
            else
            {
                ViewBag.ActivityId = new SelectList(db.Activities.Where(b => b.IsActive == true && b.IsDeleted == false)
                  .Select(b => new
                  {
                      Id = b.Id,
                      ArName = b.Code + " - " + b.ArName
                  }), "Id", "ArName", ActivityId);
                ViewBag.CompanyId = new SelectList(db.Companies.Where(b => b.IsActive == true && b.IsDeleted == false)
                                    .Select(b => new
                                    {
                                        Id = b.Id,
                                        ArName = b.Code + " - " + b.ArName
                                    }), "Id", "ArName", CompanyId);
                var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
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
                return View();
            }
        }

        public ActionResult InventoryEvaluation(int? DepartmentId, int? WarehouseId, int? GroupId, int? ActivityId, int? CompanyId, bool? showReport, bool? print)
        {
            InventoryEvaluation_Report report = new InventoryEvaluation_Report(DepartmentId, WarehouseId, GroupId, ActivityId, CompanyId);
            report.Parameters["DepId"].Value = DepartmentId > 0 ? DepartmentId : null;
            report.Parameters["WarehouseId"].Value = WarehouseId > 0 ? WarehouseId : null;
            report.Parameters["ItemGroupId"].Value = GroupId > 0 ? GroupId : null;
            report.Parameters["ActivityId"].Value = ActivityId > 0 ? ActivityId : 0;
            report.Parameters["CompanyId"].Value = CompanyId > 0 ? CompanyId : 0;

            report.RequestParameters = false;
            ViewBag.ShowReport = showReport;
            ViewBag.DepartmentId = DepartmentId;
            ViewBag.WarehouseId = WarehouseId;
            ViewBag.GroupId = GroupId;
            ViewBag.ActivityId = ActivityId;
            ViewBag.CompanyId = CompanyId;

            if (showReport == true)
            {
                return View(report);
            }
            else if (print == true)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    report.ExportToPdf(ms, new PdfExportOptions() { ShowPrintDialogOnOpen = false });
                    return File(ms.ToArray(), "application/pdf");
                }
            }
            else
            {
                var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                ViewBag.GroupId = new SelectList(db.ItemGroups.Where(b => b.IsActive == true && b.IsDeleted == false)
                    .Select(b => new
                    {
                        Id = b.Id,
                        ArName = b.Code + " - " + b.ArName
                    }), "Id", "ArName", GroupId);

                ViewBag.WarehouseId = new SelectList(db.Warehouses.Where(b => b.IsActive == true && b.IsDeleted == false && (userId == 1
                || db.UserWareHouses.Where(a => a.UserId == 1 || a.UserId == userId).Any(a => a.WareHouseId == b.Id)))
                    .Select(b => new { Id = b.Id, ArName = b.Code + " - " + b.ArName }), "Id", "ArName", WarehouseId);
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
                ViewBag.ActivityId = new SelectList(db.Activities.Where(b => b.IsActive == true && b.IsDeleted == false)
                  .Select(b => new
                  {
                      Id = b.Id,
                      ArName = b.Code + " - " + b.ArName
                  }), "Id", "ArName", ActivityId);
                ViewBag.CompanyId = new SelectList(db.Companies.Where(b => b.IsActive == true && b.IsDeleted == false)
                  .Select(b => new
                  {
                      Id = b.Id,
                      ArName = b.Code + " - " + b.ArName
                  }), "Id", "ArName", CompanyId);
                return View();
            }
        }

        public ActionResult SlackItems(int? DepartmentId, DateTime? From, DateTime? To, int? GroupId, int? CategoryId, int? ActivityId, int? CompanyId, bool? showReport, bool? print)
        {
            SlackItems_Report rpt = new SlackItems_Report(DepartmentId, From, To, GroupId, CategoryId, ActivityId, CompanyId);

            rpt.Parameters["DepId"].Value = DepartmentId > 0 ? DepartmentId : null;
            rpt.Parameters["DateFrom"].Value = From;
            rpt.Parameters["DateTo"].Value = To;
            rpt.Parameters["ItemGroupId"].Value = GroupId > 0 ? GroupId : null;
            rpt.Parameters["ItemCategoryId"].Value = CategoryId > 0 ? CategoryId : null;
            rpt.Parameters["ActivityId"].Value = ActivityId > 0 ? ActivityId : 0;
            rpt.Parameters["CompanyId"].Value = CompanyId > 0 ? CompanyId : 0;

            rpt.RequestParameters = false;
            ViewBag.ShowReport = showReport;

            ViewBag.From = From;
            ViewBag.To = To;
            ViewBag.DepartmentId = DepartmentId;
            ViewBag.GroupId = GroupId;
            ViewBag.CategoryId = CategoryId;
            ViewBag.ActivityId = ActivityId;
            ViewBag.CompanyId = CompanyId;

            if (showReport == true)
            {
                return View(rpt);
            }
            else if (print == true)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    rpt.ExportToPdf(ms, new PdfExportOptions() { ShowPrintDialogOnOpen = false });
                    return File(ms.ToArray(), "application/pdf");
                }
            }
            else
            {
                ViewBag.ActivityId = new SelectList(db.Activities.Where(b => b.IsActive == true && b.IsDeleted == false)
                  .Select(b => new
                  {
                      Id = b.Id,
                      ArName = b.Code + " - " + b.ArName
                  }), "Id", "ArName", ActivityId);
                ViewBag.CompanyId = new SelectList(db.Companies.Where(b => b.IsActive == true && b.IsDeleted == false)
                                    .Select(b => new
                                    {
                                        Id = b.Id,
                                        ArName = b.Code + " - " + b.ArName
                                    }), "Id", "ArName", CompanyId);
                ViewBag.GroupId = new SelectList(db.ItemGroups.Where(b => b.IsActive == true && b.IsDeleted == false)
                    .Select(b => new
                    {
                        Id = b.Id,
                        ArName = b.Code + " - " + b.ArName
                    }), "Id", "ArName", GroupId);
                ViewBag.CategoryId = new SelectList(db.ItemCategories.Where(b => b.IsActive == true && b.IsDeleted == false)
                    .Select(b => new
                    {
                        Id = b.Id,
                        ArName = b.Code + " - " + b.ArName
                    }), "Id", "ArName", CategoryId);

                var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
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
                return View();
            }
        }

        public ActionResult CustomerProfit()
        {
            return View();
        }

        public ActionResult Customers()
        {
            return View();
        }

        public ActionResult OrderIntervals()
        {
            return View();
        }

        public ActionResult Orders()
        {
            return View();
        }

        public ActionResult ItemShortage(int? DepartmentId, int? WarehouseId, int? GroupId, int? CategoryId, int? ActivityId, int? CompanyId, bool? showReport, bool? print)
        {
            ItemShortage_Report report = new ItemShortage_Report(DepartmentId, WarehouseId, GroupId, CategoryId, ActivityId, CompanyId);
            report.Parameters["DepartmentId"].Value = DepartmentId > 0 ? DepartmentId : null;
            report.Parameters["ItemGroupId"].Value = GroupId > 0 ? GroupId : null;
            report.Parameters["ItemCategoryId"].Value = CategoryId > 0 ? CategoryId : null;
            report.Parameters["WarehouseId"].Value = WarehouseId > 0 ? WarehouseId : null;
            report.Parameters["ActivityId"].Value = ActivityId > 0 ? ActivityId : 0;
            report.Parameters["CompanyId"].Value = CompanyId > 0 ? CompanyId : 0;
            report.RequestParameters = false;
            ViewBag.ShowReport = showReport;
            ViewBag.DepartmentId = DepartmentId;
            ViewBag.GroupId = GroupId;
            ViewBag.CategoryId = CategoryId;
            ViewBag.WarehouseId = WarehouseId;
            ViewBag.ActivityId = ActivityId;
            ViewBag.CompanyId = CompanyId;
            if (showReport == true)
            {
                return View(report);
            }
            else if (print == true)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    report.ExportToPdf(ms, new PdfExportOptions() { ShowPrintDialogOnOpen = false });
                    return File(ms.ToArray(), "application/pdf");
                }
            }
            else
            {
                var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                ViewBag.GroupId = new SelectList(db.ItemGroups.Where(b => b.IsActive == true && b.IsDeleted == false)
                    .Select(b => new
                    {
                        Id = b.Id,
                        ArName = b.Code + " - " + b.ArName
                    }), "Id", "ArName", GroupId);
                ViewBag.CategoryId = new SelectList(db.ItemCategories.Where(b => b.IsActive == true && b.IsDeleted == false)
                    .Select(b => new
                    {
                        Id = b.Id,
                        ArName = b.Code + " - " + b.ArName
                    }), "Id", "ArName", CategoryId);
                ViewBag.WarehouseId = new SelectList(db.Warehouses.Where(b => b.IsActive == true && b.IsDeleted == false && (userId == 1
                || db.UserWareHouses.Where(a => a.UserId == 1 || a.UserId == userId).Any(a => a.WareHouseId == b.Id))
                ).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName", WarehouseId);
                ViewBag.ActivityId = new SelectList(db.Activities.Where(b => b.IsActive == true && b.IsDeleted == false)
                  .Select(b => new
                  {
                      Id = b.Id,
                      ArName = b.Code + " - " + b.ArName
                  }), "Id", "ArName", ActivityId);
                ViewBag.CompanyId = new SelectList(db.Companies.Where(b => b.IsActive == true && b.IsDeleted == false)
                  .Select(b => new
                  {
                      Id = b.Id,
                      ArName = b.Code + " - " + b.ArName
                  }), "Id", "ArName", CompanyId);
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
                return View();
            }
        }

        public ActionResult ItemPrice(int? DepartmentId, int? GroupId, int? CategoryId, int? CustomerGroupId, int? ActivityId, int? CompanyId, bool? showReport, bool? print)
        {
            ItemPrice_Report report = new ItemPrice_Report(DepartmentId, GroupId, CategoryId, CustomerGroupId, ActivityId, CompanyId);
            report.Parameters["DepartmentId"].Value = DepartmentId > 0 ? DepartmentId : null;
            report.Parameters["ItemGroupId"].Value = GroupId > 0 ? GroupId : null;
            report.Parameters["ItemCategoryId"].Value = CategoryId > 0 ? CategoryId : null;
            report.Parameters["CustomerGroupId"].Value = CustomerGroupId > 0 ? CustomerGroupId : null;
            report.Parameters["ActivityId"].Value = ActivityId > 0 ? ActivityId : 0;
            report.Parameters["CompanyId"].Value = CompanyId > 0 ? CompanyId : 0;
            report.RequestParameters = false;
            ViewBag.ShowReport = showReport;
            ViewBag.DepartmentId = DepartmentId;
            ViewBag.GroupId = GroupId;
            ViewBag.CategoryId = CategoryId;
            ViewBag.CustomerGroupId = CustomerGroupId;
            ViewBag.ActivityId = ActivityId;
            ViewBag.CompanyId = CompanyId;

            if (showReport == true)
            {
                return View(report);
            }
            else if (print == true)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    report.ExportToPdf(ms, new PdfExportOptions() { ShowPrintDialogOnOpen = false });
                    return File(ms.ToArray(), "application/pdf");
                }
            }
            else
            {
                var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                ViewBag.ActivityId = new SelectList(db.Activities.Where(b => b.IsActive == true && b.IsDeleted == false)
                  .Select(b => new
                  {
                      Id = b.Id,
                      ArName = b.Code + " - " + b.ArName
                  }), "Id", "ArName", ActivityId);
                ViewBag.CompanyId = new SelectList(db.Companies.Where(b => b.IsActive == true && b.IsDeleted == false)
                                    .Select(b => new
                                    {
                                        Id = b.Id,
                                        ArName = b.Code + " - " + b.ArName
                                    }), "Id", "ArName", CompanyId);
                ViewBag.GroupId = new SelectList(db.ItemGroups.Where(b => b.IsActive == true && b.IsDeleted == false)
                    .Select(b => new
                    {
                        Id = b.Id,
                        ArName = b.Code + " - " + b.ArName
                    }), "Id", "ArName", GroupId);
                ViewBag.CategoryId = new SelectList(db.ItemCategories.Where(b => b.IsActive == true && b.IsDeleted == false)
                    .Select(b => new
                    {
                        Id = b.Id,
                        ArName = b.Code + " - " + b.ArName
                    }), "Id", "ArName", CategoryId);
                ViewBag.CustomerGroupId = new SelectList(db.CustomersGroups.Where(b => b.IsActive == true && b.IsDeleted == false)
                    .Select(b => new
                    {
                        Id = b.Id,
                        ArName = b.Code + " - " + b.ArName
                    }), "Id", "ArName", CustomerGroupId);

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
                return View();
            }
        }

        public ActionResult CustomerBalanceSheet(int? customerId, int? departmentId, DateTime? dateFrom, DateTime? dateTo, int? ActivityId, int? CompanyId, bool? showReport, bool? print)
        {
            string domainName = Request.Url.GetLeftPart(UriPartial.Authority);
            ViewBag.domainName = domainName;
            CustomersBalanceSheet_Report report = new CustomersBalanceSheet_Report(customerId, departmentId, dateFrom, dateTo, domainName, ActivityId, CompanyId);
            report.Parameters["CustomerId"].Value = customerId;
            report.Parameters["DepartmentId"].Value = departmentId > 0 ? departmentId : null;
            report.Parameters["FromDate"].Value = dateFrom;
            report.Parameters["ToDate"].Value = dateTo;
            report.Parameters["ActivityId"].Value = ActivityId > 0 ? ActivityId : 0;
            report.Parameters["CompanyId"].Value = CompanyId > 0 ? CompanyId : 0;
            report.RequestParameters = false;
            ViewBag.ShowReport = showReport;
            ViewBag.CustomerId = customerId;
            ViewBag.DepartmentId = departmentId;
            ViewBag.DateFrom = dateFrom;
            ViewBag.DateTo = dateTo;
            ViewBag.ActivityId = ActivityId;
            ViewBag.CompanyId = CompanyId;
            if (showReport == true)
            {
                return View(report);
            }
            else if (print == true)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    report.ExportToPdf(ms, new PdfExportOptions() { ShowPrintDialogOnOpen = false });
                    return File(ms.ToArray(), "application/pdf");
                }
            }
            else
            {
                var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                ViewBag.ActivityId = new SelectList(db.Activities.Where(b => b.IsActive == true && b.IsDeleted == false)
                  .Select(b => new
                  {
                      Id = b.Id,
                      ArName = b.Code + " - " + b.ArName
                  }), "Id", "ArName", ActivityId);
                ViewBag.CompanyId = new SelectList(db.Companies.Where(b => b.IsActive == true && b.IsDeleted == false)
                  .Select(b => new
                  {
                      Id = b.Id,
                      ArName = b.Code + " - " + b.ArName
                  }), "Id", "ArName", CompanyId);
                ViewBag.CustomerId = new SelectList(db.Customers.Where(a => (a.IsActive == true && a.IsDeleted == false)).Select(b => new
                {
                    b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName", customerId);

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
                return View();
            }
        }

        public ActionResult VendorBalanceSheet(int? vendorId, int? departmentId, DateTime? dateFrom, DateTime? dateTo, bool? print)
        {
            var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
            SystemSetting systemSetting = db.SystemSettings.Any() ? db.SystemSettings.FirstOrDefault() : new SystemSetting();
            ViewBag.UseDifferentCurrencies = systemSetting.UseDifferentCurrencies;
            var Currency = db.Currencies.Where(a => a.IsActive == true && a.IsDeleted == false && a.IsDefault == true).FirstOrDefault();
            var CurrencyId = Currency != null ? Currency.Id : 0;
            var CurrencyName = Currency != null ? Currency.ArName : null;
            var CurrencyEquivalent = Currency != null ? Currency.Equivalent : null;
            ViewBag.CurrencyId = CurrencyId;
            ViewBag.CurrencyName = CurrencyName;
            ViewBag.CurrencyEquivalent = CurrencyEquivalent;

            ViewBag.VendorId = new SelectList(db.Vendors.Where(a => (a.IsActive == true && a.IsDeleted == false)).Select(b => new
            {
                b.Id,
                ArName = b.Code + " - " + b.ArName
            }), "Id", "ArName", vendorId);

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

            if (vendorId > 0)
            {
                VendorBalanceSheet_Report report = new VendorBalanceSheet_Report();
                report.Parameters["VendorId"].Value = vendorId;
                report.Parameters["DepartmentId"].Value = departmentId > 0 ? departmentId : null;
                report.Parameters["DateFrom"].Value = dateFrom;
                report.Parameters["DateTo"].Value = dateTo;

                report.RequestParameters = false;
                ViewBag.ShowData = true;
                if (print == true)
                {
                    using (MemoryStream ms = new MemoryStream())
                    {
                        report.ExportToPdf(ms, new PdfExportOptions() { ShowPrintDialogOnOpen = false });
                        return File(ms.ToArray(), "application/pdf");
                    }
                }
                return View(report);
            }
            else
            {
                ViewBag.ShowData = false;
                return View();
            }
        }

        public ActionResult MostSales()
        {
            return View();
        }

        public ActionResult TotalItemProfit()
        {
            return View();
        }

        public ActionResult TechnicianBalance()
        {
            return View();
        }

        public ActionResult CustomerSales()
        {
            return View();
        }

        public ActionResult CustomerCurrentBalance(int? DepartmentId, bool? ShowDepartmentCustomer, int? ActivityId, int? CompanyId, bool? showReport, bool? print)
        {
            CustomerCurrentBalance_Report report = new CustomerCurrentBalance_Report(DepartmentId, ShowDepartmentCustomer, ActivityId, CompanyId);
            report.Parameters["DepartmentId"].Value = DepartmentId > 0 ? DepartmentId : null;
            report.Parameters["ShowDepartmentCustomer"].Value = ShowDepartmentCustomer == true ? ShowDepartmentCustomer : false;
            report.Parameters["ActivityId"].Value = ActivityId > 0 ? ActivityId : 0;
            report.Parameters["CompanyId"].Value = CompanyId > 0 ? CompanyId : 0;
            report.RequestParameters = false;
            ViewBag.ShowReport = showReport;
            ViewBag.DepartmentId = DepartmentId;
            ViewBag.ShowDepartmentCustomer = ShowDepartmentCustomer;
            ViewBag.ActivityId = ActivityId;
            ViewBag.CompanyId = CompanyId;
            if (showReport == true)
            {
                return View(report);
            }
            else if (print == true)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    report.ExportToPdf(ms, new PdfExportOptions() { ShowPrintDialogOnOpen = false });
                    return File(ms.ToArray(), "application/pdf");
                }
            }
            else
            {
                ViewBag.ActivityId = new SelectList(db.Activities.Where(b => b.IsActive == true && b.IsDeleted == false)
                  .Select(b => new
                  {
                      Id = b.Id,
                      ArName = b.Code + " - " + b.ArName
                  }), "Id", "ArName", ActivityId);
                ViewBag.CompanyId = new SelectList(db.Companies.Where(b => b.IsActive == true && b.IsDeleted == false)
                                    .Select(b => new
                                    {
                                        Id = b.Id,
                                        ArName = b.Code + " - " + b.ArName
                                    }), "Id", "ArName", CompanyId);
                var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
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
                return View();
            }
        }

        public ActionResult CashBoxBalanceSheet(int? cashBoxId, bool? showReport, DateTime? dateFrom, DateTime? dateTo, bool? print)
        {
            CashBoxBalanceSheet_Report report = new CashBoxBalanceSheet_Report(cashBoxId, dateFrom, dateTo);
            report.Parameters["CashBoxId"].Value = cashBoxId > 0 ? cashBoxId : null;
            report.Parameters["DateFrom"].Value = dateFrom;
            report.Parameters["DateTo"].Value = dateTo;
            report.RequestParameters = false;
            ViewBag.ShowReport = showReport;
            ViewBag.CashBox = cashBoxId;
            ViewBag.dateFrom = dateFrom;
            ViewBag.dateTo = dateTo;
            if (showReport == true)
            {
                return View(report);
            }
            else if (print == true)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    report.ExportToPdf(ms, new PdfExportOptions() { ShowPrintDialogOnOpen = false });
                    return File(ms.ToArray(), "application/pdf");
                }
            }
            else
            {
                var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                //ViewBag.CashBoxId = new SelectList(db.UserCashBoxes.Where(b => b.UserId == userId && b.Privilege == true && b.CashBox.IsActive == true && b.CashBox.IsDeleted == false).Select(b => new
                //{
                //    Id = b.CashBoxId,
                //    ArName = b.CashBox.Code + " - " + b.CashBox.ArName
                //}), "Id", "ArName", cashBoxId);
                //var _userid = db.ERPUsers.Where(a => a.IsActive == true && a.IsDeleted == false).FirstOrDefault().Id;
                ViewBag.CashBoxId = new SelectList(db.CashBoxes.Where(b => b.IsActive == true && b.IsDeleted == false && (userId == 1 || (db.UserCashBoxes.Where(a => a.UserId == userId && a.Privilege == true).Any(a => a.CashBoxId == b.Id))))
                    .Select(b => new
                    {
                        Id = b.Id,
                        ArName = b.Code + " - " + b.ArName
                    }), "Id", "ArName", cashBoxId);


                return View();
            }
        }

        public ActionResult SalesInvoiceInstallmentToBePaid()
        {
            return View();
        }

        public ActionResult ShareholderEquity()
        {
            return View();
        }

        public ActionResult SalesInvoiceInsallmentDetails()
        {
            return View();
        }

        public ActionResult ShareholderBalanceSheet()
        {
            return View();
        }

        public ActionResult PurchaseRequest(int? id, int? deptId, string docNo, bool? print)
        {
            PurchaseRequest_Report rpt = new PurchaseRequest_Report();
            if (id != null)
            {
                PurchaseRequest ct = db.PurchaseRequests.Find(id);
                rpt.Parameters["Id"].Value = id;
                rpt.Parameters["DocNum"].Value = ct.DocumentNumber;
                rpt.Parameters["DepId"].Value = ct.DepartmentId;
                rpt.RequestParameters = false;
            }
            else if (deptId != null && docNo != null)
            {

                PurchaseRequest ct = db.PurchaseRequests.Where(a => a.DepartmentId == deptId && a.DocumentNumber == docNo).FirstOrDefault();
                rpt.Parameters["Id"].Value = id;
                rpt.Parameters["DocNum"].Value = ct.DocumentNumber;
                rpt.Parameters["DepId"].Value = ct.DepartmentId;
                rpt.RequestParameters = false;

            }
            if (print == true)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    rpt.ExportToPdf(ms, new PdfExportOptions() { ShowPrintDialogOnOpen = true });
                    return File(ms.ToArray(), "application/pdf");
                }
            }
            return View(rpt);
        }

        public ActionResult SalesReturn(int? id, int? deptId, string docNo, bool? print)
        {
            SalesReturn_Report rpt = new SalesReturn_Report();
            if (id != null)
            {
                SalesReturn ct = db.SalesReturns.Find(id);
                rpt.Parameters["Id"].Value = id;
                rpt.Parameters["DocNum"].Value = ct.DocumentNumber;
                rpt.Parameters["DepId"].Value = ct.DepartmentId;
                rpt.RequestParameters = false;
            }
            else if (deptId != null && docNo != null)
            {

                SalesReturn ct = db.SalesReturns.Where(a => a.DepartmentId == deptId && a.DocumentNumber == docNo).FirstOrDefault();
                rpt.Parameters["Id"].Value = id;
                rpt.Parameters["DocNum"].Value = ct.DocumentNumber;
                rpt.Parameters["DepId"].Value = ct.DepartmentId;
                rpt.RequestParameters = false;

            }
            if (print == true)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    rpt.ExportToPdf(ms, new PdfExportOptions() { ShowPrintDialogOnOpen = true });
                    return File(ms.ToArray(), "application/pdf");
                }
            }
            return View(rpt);
        }

        public ActionResult ManufacturingOrder(int? id, bool? print)
        {
            ManufacturingOrder_Report rpt = new ManufacturingOrder_Report();
            if (id != null)
            {
                rpt.Parameters["Id"].Value = id;
                rpt.RequestParameters = false;
            }
            if (print == true)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    rpt.ExportToPdf(ms, new PdfExportOptions() { ShowPrintDialogOnOpen = true });
                    return File(ms.ToArray(), "application/pdf");
                }
            }
            return View(rpt);
        }

        public ActionResult ManufacturingRequest(int? id, bool? print)
        {
            using (ManufacturingRequest_Report rpt = new ManufacturingRequest_Report())
            {
                if (id != null)
                {
                    rpt.Parameters["Id"].Value = id;
                    rpt.RequestParameters = false;
                }
                if (print == true)
                {
                    using (MemoryStream ms = new MemoryStream())
                    {
                        rpt.ExportToPdf(ms, new PdfExportOptions() { ShowPrintDialogOnOpen = true });
                        return File(ms.ToArray(), "application/pdf");
                    }
                }
                return View(rpt);
            }
        }

        public ActionResult SalesOrder(int? id, int? deptId, string docNo, bool? print, string paperKind, bool? printPrice)
        {
            SalesOrder_Report rpt = new SalesOrder_Report();

            if (id != null)
            {
                SalesOrder si = db.SalesOrders.Find(id);
                rpt.Parameters["Id"].Value = id;
                rpt.Parameters["DocNum"].Value = si.DocumentNumber;
                rpt.Parameters["DepId"].Value = si.DepartmentId;
                //rpt.Parameters["CustomerId"].Value = si.VendorOrCustomerId;

                rpt.RequestParameters = false;
            }
            else if (deptId != null && docNo != null)
            {
                SalesOrder si = db.SalesOrders.Where(a => a.DepartmentId == deptId && a.DocumentNumber == docNo).FirstOrDefault();

                rpt.Parameters["DocNum"].Value = si.DocumentNumber;
                rpt.Parameters["DepId"].Value = si.DepartmentId;
                rpt.Parameters["CustomerId"].Value = si.VendorOrCustomerId;

                rpt.RequestParameters = false;
            }
            if (print == true)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    rpt.ExportToPdf(ms, new PdfExportOptions() { ShowPrintDialogOnOpen = true });
                    return File(ms.ToArray(), "application/pdf");
                }
            }
            return View(rpt);

        }

        public ActionResult CostCenterTrialBalance(int? DepartmentId, DateTime? From, DateTime? To, int? ActivityId, int? CompanyId, bool? showReport, bool? print)
        {
            CostCenterTrialBalance_Report rpt = new CostCenterTrialBalance_Report(DepartmentId, From, To, ActivityId, CompanyId);
            rpt.Parameters["DepartmentId"].Value = DepartmentId > 0 ? DepartmentId : null;
            rpt.Parameters["DateFrom"].Value = From;
            rpt.Parameters["DateTo"].Value = To;
            rpt.Parameters["ActivityId"].Value = ActivityId > 0 ? ActivityId : 0;
            rpt.Parameters["CompanyId"].Value = CompanyId > 0 ? CompanyId : 0;
            rpt.RequestParameters = false;
            ViewBag.ShowReport = showReport;
            ViewBag.From = From;
            ViewBag.To = To;
            ViewBag.DepartmentId = DepartmentId;
            ViewBag.ActivityId = ActivityId;
            ViewBag.CompanyId = CompanyId;
            if (showReport == true)
            {
                return View(rpt);
            }
            else if (print == true)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    rpt.ExportToPdf(ms, new PdfExportOptions() { ShowPrintDialogOnOpen = false });
                    return File(ms.ToArray(), "application/pdf");
                }
            }
            else
            {
                ViewBag.ActivityId = new SelectList(db.Activities.Where(b => b.IsActive == true && b.IsDeleted == false)
                  .Select(b => new
                  {
                      Id = b.Id,
                      ArName = b.Code + " - " + b.ArName
                  }), "Id", "ArName", ActivityId);
                ViewBag.CompanyId = new SelectList(db.Companies.Where(b => b.IsActive == true && b.IsDeleted == false)
                   .Select(b => new
                   {
                       Id = b.Id,
                       ArName = b.Code + " - " + b.ArName
                   }), "Id", "ArName", CompanyId);
                var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
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
                return View();
            }
        }

        public ActionResult NearlyExpiredPatches(int? DaysNo, int? WarehouseId, DateTime? From, DateTime? To, int? DepartmentId, int? ActivityId, int? CompanyId, bool? showReport, bool? print)
        {
            NearlyExpiredPatches_Report rpt = new NearlyExpiredPatches_Report(DaysNo, WarehouseId, From, To, DepartmentId, ActivityId, CompanyId);
            rpt.Parameters["DepId"].Value = DepartmentId > 0 ? DepartmentId : null;
            rpt.Parameters["WarehouseId"].Value = WarehouseId > 0 ? WarehouseId : null;
            rpt.Parameters["DaysNo"].Value = DaysNo > 0 ? DaysNo : null;
            rpt.Parameters["DateFrom"].Value = From;
            rpt.Parameters["DateTo"].Value = To;
            rpt.Parameters["ActivityId"].Value = ActivityId > 0 ? ActivityId : 0;
            rpt.Parameters["CompanyId"].Value = CompanyId > 0 ? CompanyId : 0;
            rpt.RequestParameters = false;
            ViewBag.ShowReport = showReport;
            ViewBag.From = From;
            ViewBag.To = To;
            ViewBag.DepartmentId = DepartmentId;
            ViewBag.WarehouseId = WarehouseId;
            ViewBag.DaysNo = DaysNo;
            ViewBag.ActivityId = ActivityId;
            ViewBag.CompanyId = CompanyId;
            if (showReport == true)
            {
                return View(rpt);
            }
            else if (print == true)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    rpt.ExportToPdf(ms, new PdfExportOptions() { ShowPrintDialogOnOpen = false });
                    return File(ms.ToArray(), "application/pdf");
                }
            }
            else
            {
                ViewBag.ActivityId = new SelectList(db.Activities.Where(b => b.IsActive == true && b.IsDeleted == false)
                  .Select(b => new
                  {
                      Id = b.Id,
                      ArName = b.Code + " - " + b.ArName
                  }), "Id", "ArName", ActivityId);
                ViewBag.CompanyId = new SelectList(db.Companies.Where(b => b.IsActive == true && b.IsDeleted == false)
                                    .Select(b => new
                                    {
                                        Id = b.Id,
                                        ArName = b.Code + " - " + b.ArName
                                    }), "Id", "ArName", CompanyId);
                var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
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
                ViewBag.WarehouseId = new SelectList(db.Warehouses.Where(b => b.IsActive == true && b.IsDeleted == false && (userId == 1
                || db.UserWareHouses.Where(a => a.UserId == 1 || a.UserId == userId).Any(a => a.WareHouseId == b.Id))
               ).Select(b => new
               {
                   Id = b.Id,
                   ArName = b.Code + " - " + b.ArName
               }), "Id", "ArName", WarehouseId);
                return View();
            }
            //if (daysNo != null)
            //{
            //    NearlyExpiredPatches_Report rpt = new NearlyExpiredPatches_Report(daysNo.Value);
            //    rpt.Parameters["DaysNo"].Value = daysNo;
            //    rpt.RequestParameters = false;

            //    return View(rpt);
            //}
            //return View();
        }

        public ActionResult PurchaseRequestsInPeriod()
        {
            return View();
        }

        public ActionResult StockReceipt_StockIssueQuantitiesInPeriod()
        {
            return View();
        }

        public ActionResult DebitAgesTotal(int? DepartmentId, int? CustomerId, int? ActivityId, int? CompanyId, bool? showReport, bool? print)
        {
            DebitAgesTotal report = new DebitAgesTotal(DepartmentId, CustomerId, ActivityId, CompanyId);
            report.Parameters["DepartmentId"].Value = DepartmentId > 0 ? DepartmentId : null;
            report.Parameters["Customer"].Value = CustomerId > 0 ? CustomerId : null;
            report.Parameters["ActivityId"].Value = ActivityId > 0 ? ActivityId : 0;
            report.Parameters["CompanyId"].Value = CompanyId > 0 ? CompanyId : 0;
            report.RequestParameters = false;
            ViewBag.ShowReport = showReport;
            ViewBag.DepartmentId = DepartmentId;
            ViewBag.CustomerId = CustomerId;
            ViewBag.ActivityId = ActivityId;
            ViewBag.CompanyId = CompanyId;
            if (showReport == true)
            {
                return View(report);
            }
            else if (print == true)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    report.ExportToPdf(ms, new PdfExportOptions() { ShowPrintDialogOnOpen = false });
                    return File(ms.ToArray(), "application/pdf");
                }
            }
            else
            {
                ViewBag.ActivityId = new SelectList(db.Activities.Where(b => b.IsActive == true && b.IsDeleted == false)
                  .Select(b => new
                  {
                      Id = b.Id,
                      ArName = b.Code + " - " + b.ArName
                  }), "Id", "ArName", ActivityId);
                ViewBag.CompanyId = new SelectList(db.Companies.Where(b => b.IsActive == true && b.IsDeleted == false)
                                    .Select(b => new
                                    {
                                        Id = b.Id,
                                        ArName = b.Code + " - " + b.ArName
                                    }), "Id", "ArName", CompanyId);
                var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
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
                ViewBag.CustomerId = new SelectList(db.Customers.Where(b => b.IsActive == true && b.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName", CustomerId);

                return View();
            }
        }
        public ActionResult DebitAgesDetails(int? DepartmentId, int? CustomerId, int? RepId, int? ActivityId, int? CompanyId, bool? showReport, bool? print)
        {
            DebitAgesDetails report = new DebitAgesDetails(DepartmentId, CustomerId, RepId, ActivityId, CompanyId);
            report.Parameters["DepartmentId"].Value = DepartmentId > 0 ? DepartmentId : null;
            report.Parameters["CustomerId"].Value = CustomerId > 0 ? CustomerId : null;
            report.Parameters["RepId"].Value = RepId > 0 ? RepId : null;
            report.Parameters["ActivityId"].Value = ActivityId > 0 ? ActivityId : 0;
            report.Parameters["CompanyId"].Value = CompanyId > 0 ? CompanyId : 0;
            report.RequestParameters = false;
            ViewBag.ShowReport = showReport;
            ViewBag.DepartmentId = DepartmentId;
            ViewBag.CustomerId = CustomerId;
            ViewBag.RepId = RepId;
            ViewBag.ActivityId = ActivityId;
            ViewBag.CompanyId = CompanyId;
            if (showReport == true)
            {
                return View(report);
            }
            else if (print == true)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    report.ExportToPdf(ms, new PdfExportOptions() { ShowPrintDialogOnOpen = false });
                    return File(ms.ToArray(), "application/pdf");
                }
            }
            else
            {
                ViewBag.ActivityId = new SelectList(db.Activities.Where(b => b.IsActive == true && b.IsDeleted == false)
                  .Select(b => new
                  {
                      Id = b.Id,
                      ArName = b.Code + " - " + b.ArName
                  }), "Id", "ArName", ActivityId);
                ViewBag.CompanyId = new SelectList(db.Companies.Where(b => b.IsActive == true && b.IsDeleted == false)
                                    .Select(b => new
                                    {
                                        Id = b.Id,
                                        ArName = b.Code + " - " + b.ArName
                                    }), "Id", "ArName", CompanyId);
                var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
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
                ViewBag.CustomerId = new SelectList(db.Customers.Where(b => b.IsActive == true && b.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName", CustomerId);
                ViewBag.RepId = new SelectList(db.Employees.Where(b => b.IsActive == true && b.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName", RepId);

                return View();
            }
        }

        public ActionResult VendorDebitAgesTotal(int? DepartmentId, int? VendorId, int? ActivityId, int? CompanyId, bool? showReport, bool? print)
        {
            VendorDebitAgesTotal report = new VendorDebitAgesTotal(DepartmentId, VendorId, ActivityId, CompanyId);
            report.Parameters["DepartmentId"].Value = DepartmentId > 0 ? DepartmentId : null;
            report.Parameters["VendorId"].Value = VendorId > 0 ? VendorId : null;
            report.Parameters["ActivityId"].Value = ActivityId > 0 ? ActivityId : 0;
            report.Parameters["CompanyId"].Value = CompanyId > 0 ? CompanyId : 0;
            report.RequestParameters = false;
            ViewBag.ShowReport = showReport;
            ViewBag.DepartmentId = DepartmentId;
            ViewBag.VendorId = VendorId;
            ViewBag.ActivityId = ActivityId;
            ViewBag.CompanyId = CompanyId;
            if (showReport == true)
            {
                return View(report);
            }
            else if (print == true)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    report.ExportToPdf(ms, new PdfExportOptions() { ShowPrintDialogOnOpen = false });
                    return File(ms.ToArray(), "application/pdf");
                }
            }
            else
            {
                var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                ViewBag.ActivityId = new SelectList(db.Activities.Where(b => b.IsActive == true && b.IsDeleted == false)
                  .Select(b => new
                  {
                      Id = b.Id,
                      ArName = b.Code + " - " + b.ArName
                  }), "Id", "ArName", ActivityId);
                ViewBag.CompanyId = new SelectList(db.Companies.Where(b => b.IsActive == true && b.IsDeleted == false)
                                    .Select(b => new
                                    {
                                        Id = b.Id,
                                        ArName = b.Code + " - " + b.ArName
                                    }), "Id", "ArName", CompanyId);

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
                ViewBag.VendorId = new SelectList(db.Vendors.Where(b => b.IsActive == true && b.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName", VendorId);
                return View();
            }
        }
        public ActionResult VendorDebitAgesDetails(int? DepartmentId, int? VendorId, int? ActivityId, int? CompanyId, bool? showReport, bool? print)
        {
            VendorDebitAgesDetails report = new VendorDebitAgesDetails(DepartmentId, VendorId, ActivityId, CompanyId);
            report.Parameters["DepartmentId"].Value = DepartmentId > 0 ? DepartmentId : null;
            report.Parameters["VendorId"].Value = VendorId > 0 ? VendorId : null;
            report.Parameters["ActivityId"].Value = ActivityId > 0 ? ActivityId : 0;
            report.Parameters["CompanyId"].Value = CompanyId > 0 ? CompanyId : 0;
            report.RequestParameters = false;
            ViewBag.ShowReport = showReport;
            ViewBag.DepartmentId = DepartmentId;
            ViewBag.VendorId = VendorId;
            ViewBag.ActivityId = ActivityId;
            ViewBag.CompanyId = CompanyId;
            if (showReport == true)
            {
                return View(report);
            }
            else if (print == true)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    report.ExportToPdf(ms, new PdfExportOptions() { ShowPrintDialogOnOpen = false });
                    return File(ms.ToArray(), "application/pdf");
                }
            }
            else
            {
                var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                ViewBag.ActivityId = new SelectList(db.Activities.Where(b => b.IsActive == true && b.IsDeleted == false)
                   .Select(b => new
                   {
                       Id = b.Id,
                       ArName = b.Code + " - " + b.ArName
                   }), "Id", "ArName", ActivityId);
                ViewBag.CompanyId = new SelectList(db.Companies.Where(b => b.IsActive == true && b.IsDeleted == false)
                                    .Select(b => new
                                    {
                                        Id = b.Id,
                                        ArName = b.Code + " - " + b.ArName
                                    }), "Id", "ArName", CompanyId);
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
                ViewBag.VendorId = new SelectList(db.Vendors.Where(b => b.IsActive == true && b.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName", VendorId);
                return View();
            }
        }
        public ActionResult TotalDailySales()
        {
            return View();
        }
        public ActionResult DailySalesDetails()
        {
            return View();
        }
        public ActionResult ItemsDailySales()
        {
            return View();
        }
        public ActionResult GetRepSalesAndCollectingCommission()
        {
            return View();
        }

        [AllowAnonymous]
        public ActionResult E_Invoice(int id, bool? print)
        {
            string domainName = Request.Url.GetLeftPart(UriPartial.Authority);
            E_Invoice_Report rpt = new E_Invoice_Report(domainName);
            SalesInvoice si = db.SalesInvoices.Find(id);
            rpt.Parameters["Id"].Value = id;

            rpt.RequestParameters = false;
            if (print == true)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    rpt.ExportToPdf(ms, new PdfExportOptions() { ShowPrintDialogOnOpen = true });
                    ////return File(ms.ToArray(), "application/pdf", "Report.pdf");
                    //rpt.SaveLayoutToXml(ms);
                    //string reportFilePath = @"C:\Users\atifm\Desktop\E_Invoice\Report1.repx";
                    //rpt.SaveLayoutToXml(reportFilePath);
                    return ExportDocument(ms.ToArray(), "pdf", "Report.pdf", true);
                }
            }
            using (MemoryStream ms = new MemoryStream())
            {
                rpt.ExportToPdf(ms, new PdfExportOptions() { ShowPrintDialogOnOpen = true });
                return File(ms.ToArray(), "application/pdf", "Report.pdf");
                //return ExportDocument(ms.ToArray(), "pdf", "Report.pdf", true);
            }
        }
        public ActionResult EmployeeDues(int? DepartmentId, int? Year, int? Month, int? ActivityId, int? CompanyId, bool? showReport, bool? print)
        {

            EmployeeDues_Report report = new EmployeeDues_Report(DepartmentId, Year, Month, ActivityId, CompanyId);
            report.Parameters["Department"].Value = DepartmentId > 0 ? DepartmentId : null;
            report.Parameters["Month"].Value = Month > 0 ? Month : null;
            report.Parameters["Year"].Value = Year > 0 ? Year : null;
            report.Parameters["ActivityId"].Value = ActivityId > 0 ? ActivityId : 0;
            report.Parameters["CompanyId"].Value = CompanyId > 0 ? CompanyId : 0;
            report.RequestParameters = false;
            ViewBag.ShowReport = showReport;
            ViewBag.DepartmentId = DepartmentId;
            ViewBag.Month = Month;
            ViewBag.Year = Year;
            ViewBag.ActivityId = ActivityId;
            ViewBag.CompanyId = CompanyId;
            if (showReport == true)
            {
                return View(report);
            }
            else if (print == true)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    report.ExportToPdf(ms, new PdfExportOptions() { ShowPrintDialogOnOpen = false });
                    return File(ms.ToArray(), "application/pdf");
                }
            }
            else
            {
                var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                ViewBag.ActivityId = new SelectList(db.Activities.Where(b => b.IsActive == true && b.IsDeleted == false)
                   .Select(b => new
                   {
                       Id = b.Id,
                       ArName = b.Code + " - " + b.ArName
                   }), "Id", "ArName", ActivityId);
                ViewBag.CompanyId = new SelectList(db.Companies.Where(b => b.IsActive == true && b.IsDeleted == false)
                                    .Select(b => new
                                    {
                                        Id = b.Id,
                                        ArName = b.Code + " - " + b.ArName
                                    }), "Id", "ArName", CompanyId);

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
                ViewBag.Year = new SelectList(new int[] { 2019, 2020, 2021, 2022, 2023, 2024, 2025, 2026, 2027 }, Year);
                ViewBag.Month = new SelectList(new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 }, Month);
                return View();
            }
        }
        public ActionResult EmployeeMonthlyAllocation(int? DepartmentId, int? AllocationType, int? Year, int? Month, int? ActivityId, int? CompanyId, bool? showReport, bool? print)
        {
            EmployeeMonthlyAllocation_Report report = new EmployeeMonthlyAllocation_Report(DepartmentId, AllocationType, Year, Month, ActivityId, CompanyId);
            report.Parameters["Department"].Value = DepartmentId > 0 ? DepartmentId : null;
            report.Parameters["AllocationType"].Value = AllocationType > 0 ? AllocationType : null;
            report.Parameters["Year"].Value = Year > 0 ? Year : null;
            report.Parameters["Month"].Value = Month > 0 ? Month : null;
            report.Parameters["ActivityId"].Value = ActivityId > 0 ? ActivityId : 0;
            report.Parameters["CompanyId"].Value = CompanyId > 0 ? CompanyId : 0;
            report.RequestParameters = false;
            ViewBag.ShowReport = showReport;
            ViewBag.DepartmentId = DepartmentId;
            ViewBag.AllocationType = AllocationType;
            ViewBag.Year = Year;
            ViewBag.Month = Month;
            ViewBag.ActivityId = ActivityId;
            ViewBag.CompanyId = CompanyId;
            if (showReport == true)
            {
                return View(report);
            }
            else if (print == true)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    report.ExportToPdf(ms, new PdfExportOptions() { ShowPrintDialogOnOpen = false });
                    return File(ms.ToArray(), "application/pdf");
                }
            }
            else
            {
                var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                SystemSetting systemSetting = db.SystemSettings.Any() ? db.SystemSettings.FirstOrDefault() : new SystemSetting();
                ViewBag.AllocationType = new SelectList(db.AllocationTypes.Where(d => d.IsActive == true && d.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName", AllocationType);
                ViewBag.Year = new SelectList(new int[] { 2019, 2020, 2021, 2022, 2023, 2024, 2025, 2026, 2027 }, Year);
                ViewBag.Month = new SelectList(new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 }, Month);
                ViewBag.ActivityId = new SelectList(db.Activities.Where(b => b.IsActive == true && b.IsDeleted == false)
                  .Select(b => new
                  {
                      Id = b.Id,
                      ArName = b.Code + " - " + b.ArName
                  }), "Id", "ArName", ActivityId);
                ViewBag.CompanyId = new SelectList(db.Companies.Where(b => b.IsActive == true && b.IsDeleted == false)
                                    .Select(b => new
                                    {
                                        Id = b.Id,
                                        ArName = b.Code + " - " + b.ArName
                                    }), "Id", "ArName", CompanyId);
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
                return View();
            }
        }
        public ActionResult EmployeesNotInPayrollIssue(int? DepartmentId, int? Year, int? Month, int? ActivityId, int? CompanyId, bool? showReport, bool? print)
        {
            EmployeesNotInPayrollIssue report = new EmployeesNotInPayrollIssue(DepartmentId, Year, Month, ActivityId, CompanyId);
            report.Parameters["Department"].Value = DepartmentId > 0 ? DepartmentId : null;
            report.Parameters["Year"].Value = Year > 0 ? Year : null;
            report.Parameters["Month"].Value = Month > 0 ? Month : null;
            report.Parameters["ActivityId"].Value = ActivityId > 0 ? ActivityId : 0;
            report.Parameters["CompanyId"].Value = CompanyId > 0 ? CompanyId : 0;
            report.RequestParameters = false;
            ViewBag.ShowReport = showReport;
            ViewBag.DepartmentId = DepartmentId;
            ViewBag.Year = Year;
            ViewBag.Month = Month;
            ViewBag.ActivityId = ActivityId;
            ViewBag.CompanyId = CompanyId;
            if (showReport == true)
            {
                return View(report);
            }
            else if (print == true)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    report.ExportToPdf(ms, new PdfExportOptions() { ShowPrintDialogOnOpen = false });
                    return File(ms.ToArray(), "application/pdf");
                }
            }
            else
            {
                var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                SystemSetting systemSetting = db.SystemSettings.Any() ? db.SystemSettings.FirstOrDefault() : new SystemSetting();
                ViewBag.Year = new SelectList(new int[] { 2019, 2020, 2021, 2022, 2023, 2024, 2025, 2026, 2027 }, Year);
                ViewBag.Month = new SelectList(new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 }, Month);
                ViewBag.ActivityId = new SelectList(db.Activities.Where(b => b.IsActive == true && b.IsDeleted == false)
                  .Select(b => new
                  {
                      Id = b.Id,
                      ArName = b.Code + " - " + b.ArName
                  }), "Id", "ArName", ActivityId);
                ViewBag.CompanyId = new SelectList(db.Companies.Where(b => b.IsActive == true && b.IsDeleted == false)
                                    .Select(b => new
                                    {
                                        Id = b.Id,
                                        ArName = b.Code + " - " + b.ArName
                                    }), "Id", "ArName", CompanyId);
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
                return View();
            }
        }

        public ActionResult ReservedRooms(int? BuildingId, int? RoomId, DateTime? DateFrom, DateTime? DateTo, bool? showReport)
        {
            if (showReport == true)
            {
                ReservedRooms_Report report = new ReservedRooms_Report(BuildingId, RoomId, DateFrom, DateTo);
                report.Parameters["BuildingId"].Value = BuildingId > 0 ? BuildingId : null;
                report.Parameters["RoomId"].Value = RoomId > 0 ? RoomId : null;
                report.Parameters["DateFrom"].Value = DateFrom;
                report.Parameters["DateTo"].Value = DateTo;
                report.RequestParameters = false;
                ViewBag.ShowReport = showReport;
                ViewBag.BuildingId = BuildingId;
                ViewBag.RoomId = RoomId;
                ViewBag.DateFrom = DateFrom;
                ViewBag.DateTo = DateTo;
                return View(report);
            }
            else
            {
                ViewBag.BuildingId = new SelectList(db.Buildings.Where(b => b.IsActive == true && b.IsDeleted == false)
                    .Select(b => new
                    {
                        Id = b.Id,
                        ArName = b.Code + " - " + b.ArName
                    }), "Id", "ArName", BuildingId);
                ViewBag.RoomId = new SelectList(db.Rooms.Where(b => b.IsActive == true && b.IsDeleted == false)
                   .Select(b => new
                   {
                       Id = b.Id,
                       ArName = b.Code + " - " + b.RoomNumber
                   }), "Id", "ArName", RoomId);
                return View();
            }
        }
        public ActionResult RoomLeavePermissionInDate(DateTime? Date, bool? showReport)
        {
            if (showReport == true)
            {
                RoomLeavePermissionInDate_Report report = new RoomLeavePermissionInDate_Report(Date);
                report.Parameters["Date"].Value = Date;
                report.RequestParameters = false;
                ViewBag.ShowReport = showReport;
                ViewBag.Date = Date;
                return View(report);
            }
            else
            {
                return View();
            }
        }

        public ActionResult ReservedMeals(DateTime? DateFrom, DateTime? DateTo, bool? showReport)
        {
            if (showReport == true)
            {
                ReservedMeals_Report report = new ReservedMeals_Report(DateFrom, DateTo);
                report.Parameters["DateFrom"].Value = DateFrom;
                report.Parameters["DateTo"].Value = DateTo;
                report.RequestParameters = false;
                ViewBag.ShowReport = showReport;
                ViewBag.DateFrom = DateFrom;
                ViewBag.DateTo = DateTo;
                return View(report);
            }
            else
            {
                return View();
            }
        }

        public ActionResult DayUseReservation(DateTime? DateFrom, DateTime? DateTo, bool? showReport)
        {
            if (showReport == true)
            {
                DayUseReservation_Report report = new DayUseReservation_Report(DateFrom, DateTo);
                report.Parameters["DateFrom"].Value = DateFrom;
                report.Parameters["DateTo"].Value = DateTo;
                report.RequestParameters = false;
                ViewBag.ShowReport = showReport;
                ViewBag.DateFrom = DateFrom;
                ViewBag.DateTo = DateTo;
                return View(report);
            }
            else
            {
                return View();
            }
        }

        public ActionResult HotelManagementCarEntry(DateTime? DateFrom, DateTime? DateTo, bool? showReport, bool? print)
        {
            if (showReport == true)
            {
                HotelManagementCarEntry_Report report = new HotelManagementCarEntry_Report(DateFrom, DateTo);
                report.Parameters["DateFrom"].Value = DateFrom;
                report.Parameters["DateTo"].Value = DateTo;
                report.RequestParameters = false;
                ViewBag.ShowReport = showReport;
                ViewBag.DateFrom = DateFrom;
                ViewBag.DateTo = DateTo;
                return View(report);
            }
            else
            {
                return View();
            }
        }

        public ActionResult HotelManagementCarEntryReceipt(string id, bool? print)
        {
            id = id.Replace("_pl_", "+");
            byte[] inputByteArray = new byte[id.Length + 1];
            byte[] rgbIV = { 0x21, 0x43, 0x56, 0x87, 0x10, 0xfd, 0xea, 0x1c };
            byte[] key = { };

            key = System.Text.Encoding.UTF8.GetBytes("Z4a2rX3T");
            DESCryptoServiceProvider des = new DESCryptoServiceProvider();

            inputByteArray = Convert.FromBase64String(id);
            MemoryStream ms0 = new MemoryStream();
            CryptoStream cs = new CryptoStream(ms0, des.CreateDecryptor(key, rgbIV), CryptoStreamMode.Write);
            cs.Write(inputByteArray, 0, inputByteArray.Length);
            cs.FlushFinalBlock();
            Encoding encoding = Encoding.UTF8;
            var _Id = Int32.Parse(encoding.GetString(ms0.ToArray()));
            string domainName = Request.Url.GetLeftPart(UriPartial.Authority);
            //ViewBag.domainName = domainName;
            //ViewBag.id = id;
            HotelManagementCarEntryReceipt_Report rpt = new HotelManagementCarEntryReceipt_Report(id, domainName);

            rpt.Parameters["Id"].Value = _Id;

            rpt.RequestParameters = false;

            if (print == true)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    rpt.ExportToPdf(ms, new PdfExportOptions() { ShowPrintDialogOnOpen = true });
                    return File(ms.ToArray(), "application/pdf");
                }
            }
            using (MemoryStream ms = new MemoryStream())
            {
                rpt.ExportToPdf(ms, new PdfExportOptions() { ShowPrintDialogOnOpen = true });
                return File(ms.ToArray(), "application/pdf", "Report.pdf");
                //return ExportDocument(ms.ToArray(), "pdf", "Report.pdf", true);
            }
            // return View(rpt);
        }

        public ActionResult DayUseReservationReceipt(string id, bool? print)
        {
            id = id.Replace("_pl_", "+");
            byte[] inputByteArray = new byte[id.Length + 1];
            byte[] rgbIV = { 0x21, 0x43, 0x56, 0x87, 0x10, 0xfd, 0xea, 0x1c };
            byte[] key = { };

            key = System.Text.Encoding.UTF8.GetBytes("Z4a2rX3T");
            DESCryptoServiceProvider des = new DESCryptoServiceProvider();

            inputByteArray = Convert.FromBase64String(id);
            MemoryStream ms0 = new MemoryStream();
            CryptoStream cs = new CryptoStream(ms0, des.CreateDecryptor(key, rgbIV), CryptoStreamMode.Write);
            cs.Write(inputByteArray, 0, inputByteArray.Length);
            cs.FlushFinalBlock();
            Encoding encoding = Encoding.UTF8;
            var _Id = Int32.Parse(encoding.GetString(ms0.ToArray()));
            string domainName = Request.Url.GetLeftPart(UriPartial.Authority);
            //ViewBag.domainName = domainName;
            //ViewBag.id = id;
            DayUseReservationReceipt_Report rpt = new DayUseReservationReceipt_Report(id, domainName);
            rpt.Parameters["Id"].Value = _Id;
            rpt.RequestParameters = false;

            if (print == true)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    rpt.ExportToPdf(ms, new PdfExportOptions() { ShowPrintDialogOnOpen = true });
                    return File(ms.ToArray(), "application/pdf");
                }
            }
            using (MemoryStream ms = new MemoryStream())
            {
                rpt.ExportToPdf(ms, new PdfExportOptions() { ShowPrintDialogOnOpen = true });
                return File(ms.ToArray(), "application/pdf", "Report.pdf");
                //return ExportDocument(ms.ToArray(), "pdf", "Report.pdf", true);
            }
            // return View(rpt);
        }

        public ActionResult RoomBookingReceipt(string id, bool? print)
        {
            id = id.Replace("_pl_", "+");
            byte[] inputByteArray = new byte[id.Length + 1];
            byte[] rgbIV = { 0x21, 0x43, 0x56, 0x87, 0x10, 0xfd, 0xea, 0x1c };
            byte[] key = { };

            key = System.Text.Encoding.UTF8.GetBytes("Z4a2rX3T");
            DESCryptoServiceProvider des = new DESCryptoServiceProvider();

            inputByteArray = Convert.FromBase64String(id);
            MemoryStream ms0 = new MemoryStream();
            CryptoStream cs = new CryptoStream(ms0, des.CreateDecryptor(key, rgbIV), CryptoStreamMode.Write);
            cs.Write(inputByteArray, 0, inputByteArray.Length);
            cs.FlushFinalBlock();
            Encoding encoding = Encoding.UTF8;
            var _Id = Int32.Parse(encoding.GetString(ms0.ToArray()));
            string domainName = Request.Url.GetLeftPart(UriPartial.Authority);
            //ViewBag.domainName = domainName;
            //ViewBag.id = id;
            RoomBookingReceipt_Report rpt = new RoomBookingReceipt_Report(id, domainName);
            rpt.Parameters["Id"].Value = _Id;
            rpt.RequestParameters = false;
            if (print == true)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    rpt.ExportToPdf(ms, new PdfExportOptions() { ShowPrintDialogOnOpen = true });
                    return File(ms.ToArray(), "application/pdf");
                }
            }
            using (MemoryStream ms = new MemoryStream())
            {
                rpt.ExportToPdf(ms, new PdfExportOptions() { ShowPrintDialogOnOpen = true });
                return File(ms.ToArray(), "application/pdf", "Report.pdf");
                //return ExportDocument(ms.ToArray(), "pdf", "Report.pdf", true);
            }
            // return View(rpt);
        }
        public ActionResult RoomLeavePermissionReceipt(string id, bool? print)
        {
            id = id.Replace("_pl_", "+");
            byte[] inputByteArray = new byte[id.Length + 1];
            byte[] rgbIV = { 0x21, 0x43, 0x56, 0x87, 0x10, 0xfd, 0xea, 0x1c };
            byte[] key = { };

            key = System.Text.Encoding.UTF8.GetBytes("Z4a2rX3T");
            DESCryptoServiceProvider des = new DESCryptoServiceProvider();

            inputByteArray = Convert.FromBase64String(id);
            MemoryStream ms0 = new MemoryStream();
            CryptoStream cs = new CryptoStream(ms0, des.CreateDecryptor(key, rgbIV), CryptoStreamMode.Write);
            cs.Write(inputByteArray, 0, inputByteArray.Length);
            cs.FlushFinalBlock();
            Encoding encoding = Encoding.UTF8;
            var _Id = Int32.Parse(encoding.GetString(ms0.ToArray()));
            string domainName = Request.Url.GetLeftPart(UriPartial.Authority);
            //ViewBag.domainName = domainName;
            //ViewBag.id = id;
            RoomLeavePermissionReceipt_Report rpt = new RoomLeavePermissionReceipt_Report(id, domainName);

            rpt.Parameters["Id"].Value = _Id;

            rpt.RequestParameters = false;

            if (print == true)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    rpt.ExportToPdf(ms, new PdfExportOptions() { ShowPrintDialogOnOpen = true });
                    return File(ms.ToArray(), "application/pdf");
                }
            }
            using (MemoryStream ms = new MemoryStream())
            {
                rpt.ExportToPdf(ms, new PdfExportOptions() { ShowPrintDialogOnOpen = true });
                return File(ms.ToArray(), "application/pdf", "Report.pdf");
                //return ExportDocument(ms.ToArray(), "pdf", "Report.pdf", true);
            }
            // return View(rpt);
        }
        public ActionResult MealReservationReceipt(string id, bool? print)
        {
            id = id.Replace("_pl_", "+");
            byte[] inputByteArray = new byte[id.Length + 1];
            byte[] rgbIV = { 0x21, 0x43, 0x56, 0x87, 0x10, 0xfd, 0xea, 0x1c };
            byte[] key = { };

            key = System.Text.Encoding.UTF8.GetBytes("Z4a2rX3T");
            DESCryptoServiceProvider des = new DESCryptoServiceProvider();

            inputByteArray = Convert.FromBase64String(id);
            MemoryStream ms0 = new MemoryStream();
            CryptoStream cs = new CryptoStream(ms0, des.CreateDecryptor(key, rgbIV), CryptoStreamMode.Write);
            cs.Write(inputByteArray, 0, inputByteArray.Length);
            cs.FlushFinalBlock();
            Encoding encoding = Encoding.UTF8;
            var _Id = Int32.Parse(encoding.GetString(ms0.ToArray()));
            string domainName = Request.Url.GetLeftPart(UriPartial.Authority);
            //ViewBag.domainName = domainName;
            //ViewBag.id = id;
            MealReservationReceipt_Report rpt = new MealReservationReceipt_Report(id, domainName);

            rpt.Parameters["Id"].Value = _Id;

            rpt.RequestParameters = false;

            if (print == true)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    rpt.ExportToPdf(ms, new PdfExportOptions() { ShowPrintDialogOnOpen = true });
                    return File(ms.ToArray(), "application/pdf");
                }
            }
            using (MemoryStream ms = new MemoryStream())
            {
                rpt.ExportToPdf(ms, new PdfExportOptions() { ShowPrintDialogOnOpen = true });
                return File(ms.ToArray(), "application/pdf", "Report.pdf");
                //return ExportDocument(ms.ToArray(), "pdf", "Report.pdf", true);
            }
            // return View(rpt);
        }
        public ActionResult TaxDeclaration(DateTime? dateFrom, DateTime? dateTo)
        {
            TaxDeclaration_Report report = new TaxDeclaration_Report();
            report.Parameters["DateFrom"].Value = dateFrom;
            report.Parameters["DateTo"].Value = dateTo;
            report.RequestParameters = false;
            ViewBag.ShowData = true;
            return View(report);
        }

        public ActionResult OrderWorkOrder(int? id, bool? print)
        {
            OrderWorkOrder_Report rpt = new OrderWorkOrder_Report();
            if (id != null)
            {
                Order civ = db.Orders.Find(id);
                rpt.Parameters["Id"].Value = id;
                rpt.RequestParameters = false;
            }

            if (print == true)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    rpt.ExportToPdf(ms, new PdfExportOptions() { ShowPrintDialogOnOpen = true });
                    return File(ms.ToArray(), "application/pdf");
                }
            }
            return View(rpt);

        }

        [AllowAnonymous]
        public ActionResult SurgeryMedicine(string id, bool? print)
        {
            id = id.Replace("_pl_", "+");
            string domainName = Request.Url.GetLeftPart(UriPartial.Authority);
            ViewBag.domainName = domainName;
            byte[] inputByteArray = new byte[id.Length + 1];
            byte[] rgbIV = { 0x21, 0x43, 0x56, 0x87, 0x10, 0xfd, 0xea, 0x1c };
            byte[] key = { };

            key = System.Text.Encoding.UTF8.GetBytes("Z4a2rX3T");
            DESCryptoServiceProvider des = new DESCryptoServiceProvider();

            inputByteArray = Convert.FromBase64String(id);
            MemoryStream ms = new MemoryStream();
            CryptoStream cs = new CryptoStream(ms, des.CreateDecryptor(key, rgbIV), CryptoStreamMode.Write);
            cs.Write(inputByteArray, 0, inputByteArray.Length);
            cs.FlushFinalBlock();
            Encoding encoding = Encoding.UTF8;
            // var _Id = encoding.GetString(ms.ToArray());
            var _Id = Int32.Parse(encoding.GetString(ms.ToArray()));
            ViewBag.id = id;
            SurgeryMedicine_Report rpt = new SurgeryMedicine_Report(id, domainName);
            rpt.Parameters["Id"].Value = _Id;
            rpt.RequestParameters = false;
            if (print == true)
            {
                using (MemoryStream ms1 = new MemoryStream())
                {
                    rpt.ExportToPdf(ms1, new PdfExportOptions() { ShowPrintDialogOnOpen = true });
                    //return File(ms1.ToArray(), "application/pdf", "Report.pdf");
                    return ExportDocument(ms1.ToArray(), "pdf", "Report.pdf", true);
                }
            }
            using (MemoryStream ms1 = new MemoryStream())
            {
                rpt.ExportToPdf(ms1, new PdfExportOptions() { ShowPrintDialogOnOpen = true });
                return File(ms1.ToArray(), "application/pdf", "Report.pdf");
                //return ExportDocument(ms1.ToArray(), "pdf", "Report.pdf", true);
            }
        }
        public ActionResult SurgeriesInPeriod()
        {
            return View();
        }
        public ActionResult ExaminationsInPeriod()
        {
            return View();
        }
        public ActionResult DoctorBalanceSheet(int? DoctorId, DateTime? dateFrom, DateTime? dateTo, bool? showReport, bool? print)
        {
            DoctorBalanceSheet_Report report = new DoctorBalanceSheet_Report(DoctorId, dateFrom, dateTo);
            report.Parameters["DoctorId"].Value = DoctorId;
            report.Parameters["FromDate"].Value = dateFrom;
            report.Parameters["ToDate"].Value = dateTo;
            report.RequestParameters = false;
            ViewBag.ShowReport = showReport;
            ViewBag.DoctorId = DoctorId;
            ViewBag.DateFrom = dateFrom;
            ViewBag.DateTo = dateTo;
            if (showReport == true)
            {
                return View(report);
            }
            else if (print == true)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    report.ExportToPdf(ms, new PdfExportOptions() { ShowPrintDialogOnOpen = false });
                    return File(ms.ToArray(), "application/pdf");
                }
            }
            else
            {
                var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                ViewBag.DoctorId = new SelectList(db.Doctors.Where(a => (a.IsActive == true && a.IsDeleted == false)).Select(b => new
                {
                    b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName", DoctorId);

                return View();
            }
        }
        public ActionResult SmokingTax(DateTime? dateFrom, DateTime? dateTo)
        {
            SmokingTax_Report report = new SmokingTax_Report();
            report.Parameters["DateFrom"].Value = dateFrom;
            report.Parameters["DateTo"].Value = dateTo;
            report.RequestParameters = false;
            ViewBag.ShowData = true;
            return View(report);
        }
        public ActionResult EmployeesStatement(int? DepartmentId, int? WorkStatusId, int? HrDepartmentId, int? JobId, int? ContractsTypeId, int? NationalityId, int? ActivityId, int? CompanyId, bool? showReport, bool? print)
        {
            EmployeesStatement_Report report = new EmployeesStatement_Report(DepartmentId, WorkStatusId, HrDepartmentId, JobId, ContractsTypeId, NationalityId, ActivityId, CompanyId);
            report.Parameters["DepartmentId"].Value = DepartmentId;
            report.Parameters["WorkStatusId"].Value = WorkStatusId;
            report.Parameters["HrDepartmentId"].Value = HrDepartmentId;
            report.Parameters["JobId"].Value = JobId;
            report.Parameters["ContractsTypeId"].Value = ContractsTypeId;
            report.Parameters["NationalityId"].Value = NationalityId;
            report.Parameters["ActivityId"].Value = ActivityId > 0 ? ActivityId : 0;
            report.Parameters["CompanyId"].Value = CompanyId > 0 ? CompanyId : 0;
            report.RequestParameters = false;
            ViewBag.ShowData = true;
            ViewBag.DepartmentId = DepartmentId;
            ViewBag.WorkStatusId = WorkStatusId;
            ViewBag.HrDepartmentId = HrDepartmentId;
            ViewBag.JobId = JobId;
            ViewBag.ContractsTypeId = ContractsTypeId;
            ViewBag.NationalityId = NationalityId;
            ViewBag.ActivityId = ActivityId;
            ViewBag.CompanyId = CompanyId;
            ViewBag.ShowReport = showReport;
            if (showReport == true)
            {
                return View(report);
            }
            else if (print == true)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    report.ExportToPdf(ms, new PdfExportOptions() { ShowPrintDialogOnOpen = false });
                    return File(ms.ToArray(), "application/pdf");
                }
            }
            else
            {
                var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                ViewBag.WorkStatusId = new SelectList(db.WorkStatus.Select(b => new
                {
                    Id = b.Id,
                    ArName = b.ArName
                }), "Id", "ArName", WorkStatusId);

                ViewBag.HrDepartmentId = new SelectList(db.HrDepartments.Where(b => b.IsActive == true && b.IsDeleted == false)
                    .Select(b => new
                    {
                        Id = b.Id,
                        ArName = b.Code + " - " + b.ArName
                    }), "Id", "ArName", HrDepartmentId);
                ViewBag.JobId = new SelectList(db.Jobs.Where(b => b.IsActive == true && b.IsDeleted == false)
                    .Select(b => new
                    {
                        Id = b.Id,
                        ArName = b.Code + " - " + b.ArName
                    }), "Id", "ArName", JobId);
                ViewBag.ContractsTypeId = new SelectList(db.ContractsTypes.Where(b => b.IsActive == true && b.IsDeleted == false)
                   .Select(b => new
                   {
                       Id = b.Id,
                       ArName = b.Code + " - " + b.ArName
                   }), "Id", "ArName", ContractsTypeId);
                ViewBag.NationalityId = new SelectList(db.Nationalities.Where(b => b.IsActive == true && b.IsDeleted == false)
                    .Select(b => new
                    {
                        Id = b.Id,
                        ArName = b.Code + " - " + b.ArName
                    }), "Id", "ArName", NationalityId);
                ViewBag.ActivityId = new SelectList(db.Activities.Where(b => b.IsActive == true && b.IsDeleted == false)
                  .Select(b => new
                  {
                      Id = b.Id,
                      ArName = b.Code + " - " + b.ArName
                  }), "Id", "ArName", ActivityId);
                ViewBag.CompanyId = new SelectList(db.Companies.Where(b => b.IsActive == true && b.IsDeleted == false)
                                    .Select(b => new
                                    {
                                        Id = b.Id,
                                        ArName = b.Code + " - " + b.ArName
                                    }), "Id", "ArName", CompanyId);
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
                return View();
            }
        }
        public ActionResult SalariesStatement(int? DepartmentId, int? HrDepartmentId, int? Month, int? Year, int? ActivityId, int? CompanyId, bool? showReport, bool? print)
        {
            SalariesStatement_Report report = new SalariesStatement_Report(DepartmentId, HrDepartmentId, Month, Year, ActivityId, CompanyId);
            report.Parameters["DepartmentId"].Value = DepartmentId;
            report.Parameters["HrDepartmentId"].Value = HrDepartmentId;
            report.Parameters["Month"].Value = Month;
            report.Parameters["Year"].Value = Year;
            report.Parameters["ActivityId"].Value = ActivityId > 0 ? ActivityId : 0;
            report.Parameters["CompanyId"].Value = CompanyId > 0 ? CompanyId : 0;
            report.RequestParameters = false;
            ViewBag.ShowData = true;
            ViewBag.ShowReport = showReport;
            ViewBag.DepartmentId = DepartmentId;
            ViewBag.HrDepartmentId = HrDepartmentId;
            ViewBag.Month = Month;
            ViewBag.Year = Year;
            ViewBag.ActivityId = ActivityId;
            ViewBag.CompanyId = CompanyId;
            if (showReport == true)
            {
                return View(report);
            }
            else if (print == true)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    report.ExportToPdf(ms, new PdfExportOptions() { ShowPrintDialogOnOpen = false });
                    return File(ms.ToArray(), "application/pdf");
                }
            }
            else
            {
                var userId = int.Parse(((ClaimsIdentity)User.Identity).FindFirst("Id").Value);
                ViewBag.ActivityId = new SelectList(db.Activities.Where(b => b.IsActive == true && b.IsDeleted == false)
                   .Select(b => new
                   {
                       Id = b.Id,
                       ArName = b.Code + " - " + b.ArName
                   }), "Id", "ArName", ActivityId);
                ViewBag.CompanyId = new SelectList(db.Companies.Where(b => b.IsActive == true && b.IsDeleted == false)
                                    .Select(b => new
                                    {
                                        Id = b.Id,
                                        ArName = b.Code + " - " + b.ArName
                                    }), "Id", "ArName", CompanyId);

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

                ViewBag.HrDepartmentId = new SelectList(db.HrDepartments.Where(b => b.IsActive == true && b.IsDeleted == false).Select(b => new
                {
                    Id = b.Id,
                    ArName = b.Code + " - " + b.ArName
                }), "Id", "ArName", HrDepartmentId);
                ViewBag.Year = new SelectList(new int[] { 2019, 2020, 2021, 2022, 2023, 2024, 2025, 2026, 2027 }, Year);
                ViewBag.Month = new SelectList(new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 }, Month);
                return View();
            }
        }

        public ActionResult EmployeesContract(int Id)
        {
            ViewBag.Id = Id;
            EmployeesContract_Report report = new EmployeesContract_Report(Id);
            report.Parameters["Id"].Value = Id;
            report.RequestParameters = false;
            return View(report);
        }
        public ActionResult SalesInPeriodWithPurchasePrices()
        {
            return View();
        }
        public ActionResult InvoicesSmokingTax(DateTime? dateFrom, DateTime? dateTo)
        {
            InvoicesSmokingTax_Report report = new InvoicesSmokingTax_Report();
            report.Parameters["DateFrom"].Value = dateFrom;
            report.Parameters["DateTo"].Value = dateTo;
            report.RequestParameters = false;
            ViewBag.ShowData = true;
            return View(report);
        }
        public ActionResult WaiterSalesInPeriod(int? waiterId, DateTime? fromDate, DateTime? toDate, int? ShiftId)
        {
            WaiterSalesInPeriod_Report report = new WaiterSalesInPeriod_Report(waiterId, fromDate, toDate, ShiftId);
            report.Parameters["WaiterId"].Value = waiterId;
            report.Parameters["FromDate"].Value = fromDate;
            report.Parameters["ToDate"].Value = toDate;
            report.Parameters["ShiftId"].Value = ShiftId;
            report.RequestParameters = false;
            ViewBag.ShowData = true;
            ViewBag.WaiterId = waiterId;
            ViewBag.FromDate = fromDate;
            ViewBag.ToDate = toDate;
            ViewBag.ShiftId = ShiftId;
            return View(report);
        }
        public ActionResult PrintItemsBarcode(string id, bool? print)
        {
            id = id.Replace("_pl_", "+");
            byte[] inputByteArray = new byte[id.Length + 1];
            byte[] rgbIV = { 0x21, 0x43, 0x56, 0x87, 0x10, 0xfd, 0xea, 0x1c };
            byte[] key = { };

            key = System.Text.Encoding.UTF8.GetBytes("Z4a2rX3T");
            DESCryptoServiceProvider des = new DESCryptoServiceProvider();

            inputByteArray = Convert.FromBase64String(id);
            MemoryStream ms0 = new MemoryStream();
            CryptoStream cs = new CryptoStream(ms0, des.CreateDecryptor(key, rgbIV), CryptoStreamMode.Write);
            cs.Write(inputByteArray, 0, inputByteArray.Length);
            cs.FlushFinalBlock();
            Encoding encoding = Encoding.UTF8;
            var _Id = Int32.Parse(encoding.GetString(ms0.ToArray()));
            ViewBag.id = id;

            string domainName = Request.Url.GetLeftPart(UriPartial.Authority);
            ViewBag.domainName = domainName;

            PrintItemsBarcode_Report rpt = new PrintItemsBarcode_Report(id, domainName);
            rpt.Parameters["Id"].Value = _Id;
            rpt.RequestParameters = false;
            if (print == true)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    rpt.ExportToPdf(ms, new PdfExportOptions() { ShowPrintDialogOnOpen = true });
                    return ExportDocument(ms.ToArray(), "pdf", "Report.pdf", true);
                }
            }
            using (MemoryStream ms = new MemoryStream())
            {
                rpt.ExportToPdf(ms, new PdfExportOptions() { ShowPrintDialogOnOpen = true });
                return ExportDocument(ms.ToArray(), "pdf", "Report.pdf", true);
            }

        }

        [AllowAnonymous]
        public ActionResult ServiceInvoice(string id, bool? print)
        {
            id = id.Replace("_pl_", "+");
            byte[] inputByteArray = new byte[id.Length + 1];
            byte[] rgbIV = { 0x21, 0x43, 0x56, 0x87, 0x10, 0xfd, 0xea, 0x1c };
            byte[] key = { };

            key = System.Text.Encoding.UTF8.GetBytes("Z4a2rX3T");
            DESCryptoServiceProvider des = new DESCryptoServiceProvider();

            inputByteArray = Convert.FromBase64String(id);
            MemoryStream ms0 = new MemoryStream();
            CryptoStream cs = new CryptoStream(ms0, des.CreateDecryptor(key, rgbIV), CryptoStreamMode.Write);
            cs.Write(inputByteArray, 0, inputByteArray.Length);
            cs.FlushFinalBlock();
            Encoding encoding = Encoding.UTF8;
            var _Id = Int32.Parse(encoding.GetString(ms0.ToArray()));
            ViewBag.id = id;

            string domainName = Request.Url.GetLeftPart(UriPartial.Authority);
            ViewBag.domainName = domainName;

            ServiceInvoice_Report rpt = new ServiceInvoice_Report(id, domainName);
            rpt.Parameters["Id"].Value = _Id;
            rpt.RequestParameters = false;
            //if (print == true)
            //{
            using (MemoryStream ms = new MemoryStream())
            {
                rpt.ExportToPdf(ms, new PdfExportOptions() { ShowPrintDialogOnOpen = true });
                //return File(ms.ToArray(), "application/pdf", "Report.pdf");
                return ExportDocument(ms.ToArray(), "pdf", "Report.pdf", true);
            }
            //}
            //using (MemoryStream ms = new MemoryStream())
            //{
            //    rpt.ExportToPdf(ms, new PdfExportOptions() { ShowPrintDialogOnOpen = true });
            //    return File(ms.ToArray(), "application/pdf", "Report.pdf");
            //    //return ExportDocument(ms.ToArray(), "pdf", "Report.pdf", true);
            //}
        }

        [AllowAnonymous]
        public ActionResult ServiceInvoice2(string id, bool? print)
        {

            id = id.Replace("_pl_", "+");
            byte[] inputByteArray = new byte[id.Length + 1];
            byte[] rgbIV = { 0x21, 0x43, 0x56, 0x87, 0x10, 0xfd, 0xea, 0x1c };
            byte[] key = { };

            key = System.Text.Encoding.UTF8.GetBytes("Z4a2rX3T");
            DESCryptoServiceProvider des = new DESCryptoServiceProvider();

            inputByteArray = Convert.FromBase64String(id);
            MemoryStream ms0 = new MemoryStream();
            CryptoStream cs = new CryptoStream(ms0, des.CreateDecryptor(key, rgbIV), CryptoStreamMode.Write);
            cs.Write(inputByteArray, 0, inputByteArray.Length);
            cs.FlushFinalBlock();
            Encoding encoding = Encoding.UTF8;
            var _Id = Int32.Parse(encoding.GetString(ms0.ToArray()));
            ViewBag.id = id;

            string domainName = Request.Url.GetLeftPart(UriPartial.Authority);
            ViewBag.domainName = domainName;

            ServiceInvoice2_Report rpt = new ServiceInvoice2_Report(id, domainName);
            ServiceInvoice si = db.ServiceInvoices.Find(_Id);
            rpt.Parameters["Id"].Value = si.Id;
            rpt.RequestParameters = false;

            if (print == true)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    rpt.ExportToPdf(ms, new PdfExportOptions() { ShowPrintDialogOnOpen = true });
                    return ExportDocument(ms.ToArray(), "pdf", "Report.pdf", true);
                }
            }
            using (MemoryStream ms = new MemoryStream())
            {
                rpt.ExportToPdf(ms, new PdfExportOptions() { ShowPrintDialogOnOpen = true });
                return ExportDocument(ms.ToArray(), "pdf", "Report.pdf", true);
            }
        }

        public ActionResult DebitAndCreditNotification(int? id, int? deptId, bool? print, string docNo)
        {
            DebitAndCreditNotification_Report rpt = new DebitAndCreditNotification_Report(id, deptId, docNo);
            ViewBag.Id = id;
            string domainName = Request.Url.GetLeftPart(UriPartial.Authority);
            ViewBag.domainName = domainName;
            if (id != null)
            {
                DebitAndCreditNotification notification = db.DebitAndCreditNotifications.Find(id);
                rpt.Parameters["Id"].Value = id;
                rpt.Parameters["DocumentNumber"].Value = notification.DocumentNumber;
                rpt.Parameters["DepId"].Value = notification.DepartmentId;
                ViewBag.DocumentNumber = notification.DocumentNumber;
                ViewBag.DepartmentId = notification.DepartmentId;
                rpt.RequestParameters = false;
            }
            else if (deptId != null && docNo != null)
            {
                DebitAndCreditNotification notification = db.DebitAndCreditNotifications.Where(a => a.DepartmentId == deptId && a.DocumentNumber == docNo).FirstOrDefault();

                rpt.Parameters["Id"].Value = notification.Id;
                rpt.Parameters["DocumentNumber"].Value = notification.DocumentNumber;
                rpt.Parameters["DepId"].Value = notification.DepartmentId;
                ViewBag.DocumentNumber = notification.DocumentNumber;
                ViewBag.DepartmentId = notification.DepartmentId;
                rpt.RequestParameters = false;
            }
            if (print == true)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    rpt.ExportToPdf(ms, new PdfExportOptions() { ShowPrintDialogOnOpen = true });
                    return File(ms.ToArray(), "application/pdf");
                }
            }
            return View(rpt);
        }
        public ActionResult CompetitionResult()
        {
            return View();
        }
        public ActionResult Property()
        {
            return View();
        }
        public ActionResult PropertyRenter()
        {
            return View();
        }
        public ActionResult PropertyContractTotal()
        {
            return View();
        }
        public ActionResult PropertyDueRent()
        {
            return View();
        }
        public ActionResult DueOwnersPaymentsReport()
        {
            return View();
        }
        
        public ActionResult ChurchMembershipVisitInPeriod(DateTime? From, DateTime? To, bool? showReport)
        {
            if (showReport == true)
            {
                ChurchMembershipVisitInPeriod_Report report = new ChurchMembershipVisitInPeriod_Report(From, To);
                report.Parameters["DateFrom"].Value = From;
                report.Parameters["DateTo"].Value = To;
                report.RequestParameters = false;
                ViewBag.ShowData = true;
                ViewBag.ShowReport = showReport;
                ViewBag.From = From;
                ViewBag.To = To;
                return View(report);
            }
            else
            {

                return View();
            }
        }
        public ActionResult RepTotals()
        {
            return View();
        }
        public ActionResult SalesSituation()
        {
            return View();
        }
        public ActionResult GroupsProfits()
        {
            return View();
        }
        public ActionResult AllCustomersSales()
        {
            return View();
        }


    }
}