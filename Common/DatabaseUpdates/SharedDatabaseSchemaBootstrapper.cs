using MyERP.Models;
using System;
using System.Data.Entity;
using System.Diagnostics;
using System.Linq;

namespace MyERP.Common.DatabaseUpdates
{
    public static class SharedDatabaseSchemaBootstrapper
    {
        public static void EnsureRequiredColumns(MySoftERPEntity db)
        {
            if (db == null)
            {
                return;
            }

            try
            {
                db.Database.ExecuteSqlCommand(@"
IF OBJECT_ID(N'dbo.SystemSetting', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.SystemSetting', N'IsZatcaLinked') IS NULL
BEGIN
    ALTER TABLE dbo.SystemSetting
        ADD IsZatcaLinked bit NOT NULL
            CONSTRAINT DF_SystemSetting_IsZatcaLinked DEFAULT (0);
END");
            }
            catch (Exception ex)
            {
                Trace.TraceWarning("Shared database schema bootstrap failed: " + ex);
            }
        }

        public static bool AreRequiredColumnsAvailable(MySoftERPEntity db)
        {
            if (db == null)
            {
                return false;
            }

            try
            {
                return db.Database.SqlQuery<int>(@"
SELECT CASE
    WHEN OBJECT_ID(N'dbo.SystemSetting', N'U') IS NOT NULL
     AND COL_LENGTH(N'dbo.SystemSetting', N'IsZatcaLinked') IS NOT NULL
    THEN 1
    ELSE 0
END").FirstOrDefault() == 1;
            }
            catch (Exception ex)
            {
                Trace.TraceWarning("Shared database schema check failed: " + ex);
                return false;
            }
        }
    }
}
