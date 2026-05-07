using System;
using System.Collections.Generic;

namespace SyncBranchAgent.Models
{
    public class InvoicePayload
    {
        public string SyncKey { get; set; }
        public int BranchId { get; set; }
        public string EntityType { get; set; }
        public string SourceTransactionId { get; set; }
        public string OldTransactionId { get; set; }
        public string PayloadHash { get; set; }
        public string PayloadSchemaVersion { get; set; }
        public string ConfigVersion { get; set; }
        public DateTime CollectedAtUtc { get; set; }
        public IDictionary<string, object> Header { get; set; }
    }
}
