using System.Web.Mvc;

namespace MyERP.Areas.Sync
{
    public class SyncAreaRegistration : AreaRegistration
    {
        public override string AreaName
        {
            get { return "Sync"; }
        }

        public override void RegisterArea(AreaRegistrationContext context)
        {
            context.MapRoute(
                "Sync_root",
                "sync",
                new { controller = "Dashboard", action = "Index" },
                new[] { "MyERP.Areas.Sync.Controllers" }
            );

            context.MapRoute(
                "Sync_audit",
                "sync/audit",
                new { controller = "AdminAudit", action = "Index" },
                new[] { "MyERP.Areas.Sync.Controllers" }
            );

            context.MapRoute(
                "Sync_branch_api_heartbeat",
                "sync/api/branch/heartbeat",
                new { controller = "BranchApi", action = "Heartbeat" },
                new[] { "MyERP.Areas.Sync.Controllers" }
            );

            context.MapRoute(
                "Sync_branch_api_ping",
                "sync/api/branch/ping",
                new { controller = "BranchApi", action = "Ping" },
                new[] { "MyERP.Areas.Sync.Controllers" }
            );

            context.MapRoute(
                "Sync_branch_api_outbox_ack",
                "sync/api/branch/outbox/{syncKey}/ack",
                new { controller = "BranchApi", action = "Ack" },
                new[] { "MyERP.Areas.Sync.Controllers" }
            );

            context.MapRoute(
                "Sync_branch_api_outbox",
                "sync/api/branch/outbox",
                new { controller = "BranchApi", action = "Outbox" },
                new[] { "MyERP.Areas.Sync.Controllers" }
            );

            context.MapRoute(
                "Sync_default",
                "sync/{controller}/{action}/{id}",
                new { controller = "Dashboard", action = "Index", id = UrlParameter.Optional },
                new[] { "MyERP.Areas.Sync.Controllers" }
            );
        }
    }
}
