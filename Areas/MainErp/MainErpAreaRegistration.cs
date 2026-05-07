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
            context.MapRoute(
                "MainErp_root",
                "MainErp",
                new { controller = "Home", action = "Index" },
                new[] { "MyERP.Areas.MainErp.Controllers" }
            );

            context.MapRoute(
                "MainErp_lc",
                "MainErp/LC",
                new { controller = "LC", action = "Index" },
                new[] { "MyERP.Areas.MainErp.Controllers" }
            );

            context.MapRoute(
                "MainErp_project_extracts",
                "MainErp/ProjectExtracts",
                new { controller = "ProjectExtracts", action = "Index" },
                new[] { "MyERP.Areas.MainErp.Controllers" }
            );

            context.MapRoute(
                "MainErp_journal_details_by_note",
                "MainErp/JournalEntries/DetailsByNote/{noteId}",
                new { controller = "JournalEntries", action = "DetailsByNote", noteId = UrlParameter.Optional },
                new[] { "MyERP.Areas.MainErp.Controllers" }
            );

            context.MapRoute(
                "MainErp_journal_details_by_voucher",
                "MainErp/JournalEntries/DetailsByVoucher/{voucherId}",
                new { controller = "JournalEntries", action = "DetailsByVoucher", voucherId = UrlParameter.Optional },
                new[] { "MyERP.Areas.MainErp.Controllers" }
            );

            context.MapRoute(
                "MainErp_default",
                "MainErp/{controller}/{action}/{id}",
                new { controller = "Home", action = "Index", id = UrlParameter.Optional },
                new[] { "MyERP.Areas.MainErp.Controllers" }
            );
        }
    }
}
