using MyERP.Models;
using MyERP.Common.DatabaseUpdates;
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
            SharedDatabaseSchemaBootstrapper.EnsureRequiredColumns(db);
            return db.Database.SqlQuery<bool>(@"
IF OBJECT_ID(N'dbo.SystemSetting', N'U') IS NULL
   OR COL_LENGTH(N'dbo.SystemSetting', N'IsZatcaLinked') IS NULL
BEGIN
    SELECT CAST(0 AS bit);
END
ELSE
BEGIN
    SELECT TOP (1) CAST(ISNULL(IsZatcaLinked, 0) AS bit)
    FROM dbo.SystemSetting;
END").FirstOrDefault();
        }
    }
}
