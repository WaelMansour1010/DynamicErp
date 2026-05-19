using MyERP.Models;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;

namespace MyERP
{
    public static class ZatcaComplianceWarning
    {
        public static void Apply(Controller controller, MySoftERPEntity db, string context, bool showOnOpen = true)
        {
            if (controller == null || db == null)
            {
                return;
            }

            if (IsLinked(db))
            {
                return;
            }

            controller.ViewBag.ZatcaComplianceWarningRequired = true;
            controller.ViewBag.ZatcaComplianceWarningContext = context;
            controller.ViewBag.ZatcaComplianceWarningShowOnOpen = showOnOpen;
        }

        private static bool IsLinked(MySoftERPEntity db)
        {
            return db.SystemSettings
                .AsNoTracking()
                .Select(setting => setting.IsZatcaLinked)
                .FirstOrDefault() == true;
        }
    }
}
