using System;

namespace SyncBranchAgent.Models
{
    public class BranchHeartbeat
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
}
