using System;
using System.Collections.Generic;

namespace MyERP.Areas.Sync.ViewModels
{
    public class DashboardViewModel
    {
        public string TargetDatabase { get; set; }
        public int PendingCount { get; set; }
        public int ConflictCount { get; set; }
        public int FailedCount { get; set; }
        public int AppliedCount { get; set; }
        public int BlockedCount { get; set; }
        public IList<SyncBatchRow> RecentBatches { get; set; }
        public IList<ChartPoint> ProfileUsage { get; set; }
        public IList<ChartPoint> BranchActivity { get; set; }
        public IList<ChartPoint> ConflictTrend { get; set; }
        public IList<ChartPoint> RetryStats { get; set; }
        public IList<ChartPoint> ProblemBranches { get; set; }
        public IList<AdminAuditRow> RecentDangerousOperations { get; set; }
        public IList<BranchHeartbeatRow> BranchHeartbeats { get; set; }
        public IList<BranchUploadRow> RecentBranchUploads { get; set; }
    }

    public class QueueFilter
    {
        public string SyncKey { get; set; }
        public string BranchId { get; set; }
        public string OldTransactionId { get; set; }
        public string Status { get; set; }
        public string ProfileName { get; set; }
        public string PayloadHash { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }

    public class QueueViewModel
    {
        public QueueFilter Filter { get; set; }
        public IList<SyncQueueRow> Rows { get; set; }
        public int TotalRows { get; set; }
    }

    public class SyncQueueRow
    {
        public long SyncId { get; set; }
        public string SyncKey { get; set; }
        public int BranchId { get; set; }
        public string EntityType { get; set; }
        public string EntityKey { get; set; }
        public string OldTransactionId { get; set; }
        public string ProfileName { get; set; }
        public string Status { get; set; }
        public string PayloadHash { get; set; }
        public int TryCount { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string LastError { get; set; }
        public string ConflictReason { get; set; }
    }

    public class SyncBatchRow
    {
        public long BatchId { get; set; }
        public string ProfileName { get; set; }
        public string Status { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public int PendingCount { get; set; }
        public int AppliedCount { get; set; }
        public int FailedCount { get; set; }
        public int ConflictCount { get; set; }
    }

    public class LogRow
    {
        public long Id { get; set; }
        public DateTime? CreatedAt { get; set; }
        public string Level { get; set; }
        public string SyncKey { get; set; }
        public string Operation { get; set; }
        public string Message { get; set; }
        public string Details { get; set; }
    }

    public class DiagnosticViewModel
    {
        public string SyncKey { get; set; }
        public SyncQueueRow QueueRow { get; set; }
        public IList<ObjectMapRow> ObjectMapRows { get; set; }
        public IList<LogRow> Logs { get; set; }
        public IList<LogRow> Errors { get; set; }
        public IList<ReadinessCheckRow> Checks { get; set; }
    }

    public class ObjectMapRow
    {
        public long ObjectMapId { get; set; }
        public string SourceObjectType { get; set; }
        public string SourceObjectKey { get; set; }
        public string DestinationObjectType { get; set; }
        public string DestinationObjectKey { get; set; }
        public string SyncKey { get; set; }
        public string PayloadHash { get; set; }
        public DateTime? CreatedAt { get; set; }
    }

    public class ProfileViewModel
    {
        public string ProfileName { get; set; }
        public IList<ProfileSettingRow> Settings { get; set; }
        public IList<string> Warnings { get; set; }
    }

    public class ProfileSettingRow
    {
        public string SettingKey { get; set; }
        public string SettingValue { get; set; }
        public bool IsEnabled { get; set; }
    }

    public class PilotViewModel
    {
        public string TargetDatabase { get; set; }
        public IList<ReadinessCheckRow> Checks { get; set; }
        public IList<AdminApprovalRow> Approvals { get; set; }
    }

    public class ReadinessCheckRow
    {
        public string CheckName { get; set; }
        public string Status { get; set; }
        public string Message { get; set; }
        public bool IsHardBlocker { get; set; }
    }

    public class AdminAuditRow
    {
        public long Id { get; set; }
        public DateTime? CreatedAt { get; set; }
        public string UserName { get; set; }
        public string MachineName { get; set; }
        public string IpAddress { get; set; }
        public string Operation { get; set; }
        public string Permission { get; set; }
        public string ProfileName { get; set; }
        public string SyncKey { get; set; }
        public string Result { get; set; }
        public string Reason { get; set; }
    }

    public class AdminApprovalRow
    {
        public long Id { get; set; }
        public DateTime? CreatedAt { get; set; }
        public string RequestedBy { get; set; }
        public string ApprovedBy { get; set; }
        public string Operation { get; set; }
        public string SyncKey { get; set; }
        public string Status { get; set; }
        public string Reason { get; set; }
    }

    public class ChartPoint
    {
        public string Label { get; set; }
        public decimal Value { get; set; }
    }

    public class AdminOperationRequest
    {
        public string OperationType { get; set; }
        public string SyncKey { get; set; }
        public string ProfileName { get; set; }
        public string Reason { get; set; }
        public bool ApprovalConfirmed { get; set; }
        public string PasswordConfirmation { get; set; }
        public bool ApplySingleSyncKeyOnly { get; set; }
        public int MaxInvoicesPerRun { get; set; }
    }

    public class AdminOperationViewModel
    {
        public AdminOperationRequest Request { get; set; }
        public IList<AdminOperationRow> Operations { get; set; }
        public IList<ReadinessCheckRow> Checks { get; set; }
    }

    public class AdminOperationRow
    {
        public long AdminOperationId { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string RequestedBy { get; set; }
        public string ApprovedBy { get; set; }
        public string OperationType { get; set; }
        public string Permission { get; set; }
        public string ProfileName { get; set; }
        public string SyncKey { get; set; }
        public string Status { get; set; }
        public string Result { get; set; }
        public string Reason { get; set; }
        public string WorkerName { get; set; }
        public string LastError { get; set; }
    }

    public class NotificationRow
    {
        public long NotificationId { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? ReadAt { get; set; }
        public string NotificationType { get; set; }
        public string Severity { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public string SyncKey { get; set; }
        public string BranchId { get; set; }
        public string Status { get; set; }
    }

    public class RolePermissionViewModel
    {
        public IList<RolePermissionRow> Roles { get; set; }
    }

    public class RolePermissionRow
    {
        public string RoleName { get; set; }
        public string Permission { get; set; }
        public bool IsEnabled { get; set; }
        public string Notes { get; set; }
    }

    public class BranchHeartbeatRow
    {
        public int BranchId { get; set; }
        public string MachineName { get; set; }
        public DateTime? LastSeenAt { get; set; }
        public string AgentVersion { get; set; }
        public string ConfigVersion { get; set; }
        public string PayloadSchemaVersion { get; set; }
        public int PendingOutboxCount { get; set; }
        public int FailedOutboxCount { get; set; }
        public int RejectedPayloadCount { get; set; }
        public int AuthFailureCount { get; set; }
        public DateTime? LastAuthFailureAt { get; set; }
        public long LastTransactionId { get; set; }
        public string LastPayloadSyncKey { get; set; }
        public string LastError { get; set; }
    }

    public class BranchUploadRow
    {
        public long UploadId { get; set; }
        public DateTime? CreatedAt { get; set; }
        public int BranchId { get; set; }
        public string SyncKey { get; set; }
        public string PayloadHash { get; set; }
        public string Status { get; set; }
        public string Message { get; set; }
    }
}
