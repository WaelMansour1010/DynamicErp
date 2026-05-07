using MyERP.Areas.MainErp.Models.Accounting;

namespace MyERP.Areas.MainErp.Interfaces
{
    public interface IAuditLogger
    {
        void Log(AuditEntry entry);
    }
}
