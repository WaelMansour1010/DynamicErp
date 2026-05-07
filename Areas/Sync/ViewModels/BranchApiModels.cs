using System;
using System.Collections.Generic;

namespace MyERP.Areas.Sync.ViewModels
{
    public class BranchOutboxEnvelope
    {
        public string SyncKey { get; set; }
        public int BranchId { get; set; }
        public string EntityType { get; set; }
        public string PayloadHash { get; set; }
        public string PayloadSchemaVersion { get; set; }
        public string ConfigVersion { get; set; }
        public int TryCount { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public BranchInvoicePayload Payload { get; set; }
    }

    public class BranchInvoicePayload
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

    public class BranchHeartbeatRequest
    {
        public int BranchId { get; set; }
        public string MachineName { get; set; }
        public DateTime SentAtUtc { get; set; }
        public string AgentVersion { get; set; }
        public string ConfigVersion { get; set; }
        public string PayloadSchemaVersion { get; set; }
        public int PendingOutboxCount { get; set; }
        public int FailedOutboxCount { get; set; }
        public long LastTransactionId { get; set; }
    }

    public class BranchApiResult
    {
        public bool Accepted { get; set; }
        public string Status { get; set; }
        public string Message { get; set; }
        public string SyncKey { get; set; }
    }
}
