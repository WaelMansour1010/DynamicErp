using System;

namespace SyncBranchAgent.Models
{
    public class HealthSnapshot
    {
        public int BranchId { get; set; }
        public string MachineName { get; set; }
        public string AgentVersion { get; set; }
        public string ConfigVersion { get; set; }
        public string PayloadSchemaVersion { get; set; }
        public DateTime? LastScanUtc { get; set; }
        public DateTime? LastSendUtc { get; set; }
        public DateTime? LastHeartbeatUtc { get; set; }
        public int PendingLocalOutboxCount { get; set; }
        public int FailedLocalOutboxCount { get; set; }
        public bool SendEnabled { get; set; }
        public bool DryRunSend { get; set; }
        public bool CentralConnectivityOk { get; set; }
        public string CentralConnectivityMessage { get; set; }
    }
}
