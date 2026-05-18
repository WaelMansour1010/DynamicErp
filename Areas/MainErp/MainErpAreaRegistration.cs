using System.Configuration;
using System.Web.Mvc;

namespace MyERP.Areas.MainErp
{
    public class MainErpAreaRegistration : AreaRegistration
    {
        public override string AreaName
        {
            get { return "MainErp"; }
        }

        public override void RegisterArea(AreaRegistrationContext context)
        {
            if (!IsMainErpEnabled())
            {
                return;
            }

            var rootRoute = context.MapRoute(
                "MainErp_root",
                "MainErp",
                new { controller = "Home", action = "Index" },
                new[] { "MyERP.Areas.MainErp.Controllers" }
            );
            rootRoute.DataTokens["UseNamespaceFallback"] = false;

            var lcRoute = context.MapRoute(
                "MainErp_lc",
                "MainErp/LC",
                new { controller = "LC", action = "Index" },
                new[] { "MyERP.Areas.MainErp.Controllers" }
            );
            lcRoute.DataTokens["UseNamespaceFallback"] = false;

            var projectsRoute = context.MapRoute(
                "MainErp_projects",
                "MainErp/Projects",
                new { controller = "Projects", action = "Index" },
                new[] { "MyERP.Areas.MainErp.Controllers" }
            );
            projectsRoute.DataTokens["UseNamespaceFallback"] = false;

            var projectExtractRoute = context.MapRoute(
                "MainErp_project_extracts",
                "MainErp/ProjectExtracts",
                new { controller = "ProjectExtracts", action = "Index" },
                new[] { "MyERP.Areas.MainErp.Controllers" }
            );
            projectExtractRoute.DataTokens["UseNamespaceFallback"] = false;

            var journalOpeningBalanceRoute = context.MapRoute(
                "MainErp_journal_opening_balance",
                "MainErp/JournalEntries/OpeningBalance",
                new { controller = "JournalEntries", action = "OpeningBalance" },
                new[] { "MyERP.Areas.MainErp.Controllers" }
            );
            journalOpeningBalanceRoute.DataTokens["UseNamespaceFallback"] = false;

            var journalNoteRoute = context.MapRoute(
                "MainErp_journal_details_by_note",
                "MainErp/JournalEntries/DetailsByNote/{noteId}",
                new { controller = "JournalEntries", action = "DetailsByNote", noteId = UrlParameter.Optional },
                new[] { "MyERP.Areas.MainErp.Controllers" }
            );
            journalNoteRoute.DataTokens["UseNamespaceFallback"] = false;

            var journalVoucherRoute = context.MapRoute(
                "MainErp_journal_details_by_voucher",
                "MainErp/JournalEntries/DetailsByVoucher/{voucherId}",
                new { controller = "JournalEntries", action = "DetailsByVoucher", voucherId = UrlParameter.Optional },
                new[] { "MyERP.Areas.MainErp.Controllers" }
            );
            journalVoucherRoute.DataTokens["UseNamespaceFallback"] = false;

            var financePaymentVoucherRoute = context.MapRoute(
                "MainErp_finance_payment_voucher",
                "MainErp/Finance/PaymentVoucher",
                new { controller = "Finance", action = "PaymentVoucher" },
                new[] { "MyERP.Areas.MainErp.Controllers" }
            );
            financePaymentVoucherRoute.DataTokens["UseNamespaceFallback"] = false;

            var defaultRoute = context.MapRoute(
                "MainErp_default",
                "MainErp/{controller}/{action}/{id}",
                new { controller = "Home", action = "Index", id = UrlParameter.Optional },
                new[] { "MyERP.Areas.MainErp.Controllers" }
            );
            defaultRoute.DataTokens["UseNamespaceFallback"] = false;
        }

        private static bool IsMainErpEnabled()
        {
            bool enabled;
            return bool.TryParse(ConfigurationManager.AppSettings["EnableMainErpMigration"], out enabled) && enabled;
        }
    }
}
