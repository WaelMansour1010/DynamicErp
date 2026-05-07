using System;

namespace SyncBranchAgent.Models
{
    public class OutboxEnvelope
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
        public DateTime? LastAttemptAtUtc { get; set; }
        public DateTime? NextAttemptAtUtc { get; set; }
        public DateTime? SentAtUtc { get; set; }
        public string LastError { get; set; }
        public InvoicePayload Payload { get; set; }
    }
}
