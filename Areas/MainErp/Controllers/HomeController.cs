using System.Collections.Generic;
using System.Web.Mvc;
using MyERP.Areas.MainErp.ViewModels;

namespace MyERP.Areas.MainErp.Controllers
{
    public class HomeController : MainErpControllerBase
    {
        public ActionResult Index()
        {
            var model = new MainErpDashboardViewModel
            {
                Sections = new List<MainErpModuleTileViewModel>
                {
                    new MainErpModuleTileViewModel
                    {
                        Title = "Executive Dashboard",
                        ArabicTitle = "الداشبورد",
                        Status = "Neutral shell, no POS widgets",
                        Url = Url.Action("Index", "Dashboard", new { area = "MainErp" })
                    },
                    new MainErpModuleTileViewModel
                    {
                        Title = "Purchases",
                        ArabicTitle = "المشتريات",
                        Status = "Route reserved, no save",
                        Url = Url.Action("Index", "Purchases", new { area = "MainErp" })
                    },
                    new MainErpModuleTileViewModel
                    {
                        Title = "Stock Transfers",
                        ArabicTitle = "التحويلات المخزنية",
                        Status = "Route reserved, no save",
                        Url = Url.Action("Index", "StockTransfers", new { area = "MainErp" })
                    },
                    new MainErpModuleTileViewModel
                    {
                        Title = "Journal Entries",
                        ArabicTitle = "القيود اليومية",
                        Status = "Read-only search",
                        Url = Url.Action("Index", "JournalEntries", new { area = "MainErp" })
                    },
                    new MainErpModuleTileViewModel
                    {
                        Title = "Accounting Reports",
                        ArabicTitle = "التقارير المحاسبية",
                        Status = "Shell only",
                        Url = Url.Action("Index", "AccountingReports", new { area = "MainErp" })
                    },
                    new MainErpModuleTileViewModel
                    {
                        Title = "Sales Reports",
                        ArabicTitle = "تقارير المبيعات",
                        Status = "Shell only",
                        Url = Url.Action("Index", "SalesReports", new { area = "MainErp" })
                    },
                    new MainErpModuleTileViewModel
                    {
                        Title = "Letters of Credit",
                        ArabicTitle = "Letters of Credit",
                        Status = "Read-only list and details",
                        Url = Url.Action("Index", "LC", new { area = "MainErp" })
                    },
                    new MainErpModuleTileViewModel
                    {
                        Title = "Project Extracts",
                        ArabicTitle = "Project Extracts",
                        Status = "Read-only list and details",
                        Url = Url.Action("Index", "ProjectExtracts", new { area = "MainErp" })
                    },
                    new MainErpModuleTileViewModel
                    {
                        Title = "Accounting Preview",
                        ArabicTitle = "Accounting Preview",
                        Status = "Foundation validation only",
                        Url = Url.Action("PreviewTest", "Accounting", new { area = "MainErp" })
                    }
                }
            };

            return View(model);
        }
    }
}
