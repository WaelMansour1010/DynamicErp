using System.Diagnostics;
using MyERP.Areas.MainErp.Interfaces;
using MyERP.Areas.MainErp.Models.Accounting;

namespace MyERP.Areas.MainErp.Services.Accounting
{
    public class AuditLogger : IAuditLogger
    {
        public void Log(AuditEntry entry)
        {
            if (entry == null)
            {
                return;
            }

            Trace.TraceInformation(
                "MainErpAudit CorrelationId={0} Operation={1} Entity={2} Key={3} User={4} Message={5}",
                entry.CorrelationId,
                entry.OperationType,
                entry.EntityName,
                entry.KeyValue,
                entry.UserName,
                entry.Message);
        }
    }
}
