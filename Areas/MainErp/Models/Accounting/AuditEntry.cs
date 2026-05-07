using System;

namespace MyERP.Areas.MainErp.Models.Accounting
{
    public class AuditEntry
    {
        public Guid CorrelationId { get; set; }
        public string OperationType { get; set; }
        public string EntityName { get; set; }
        public string KeyValue { get; set; }
        public string UserName { get; set; }
        public DateTime TimestampUtc { get; set; }
        public string BeforeSnapshot { get; set; }
        public string AfterSnapshot { get; set; }
        public string Message { get; set; }
    }
}
