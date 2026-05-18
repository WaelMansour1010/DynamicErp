using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace MyERP.Areas.Shared.CriticalRecovery
{
    public static class CriticalRecoveryMode
    {
        public const string CancelOnly = "CancelOnly";
        public const string ReverseAccountingOnly = "ReverseAccountingOnly";
        public const string ReverseInventoryOnly = "ReverseInventoryOnly";
        public const string FullRollback = "FullRollback";
        public const string PeriodCleanup = "PeriodCleanup";
        public const string BranchCleanup = "BranchCleanup";
    }

    public class CriticalRecoveryIndexViewModel
    {
        public CriticalRecoveryIndexViewModel()
        {
            Filter = new CriticalRecoveryFilterViewModel();
            Request = new CriticalRecoveryRequestViewModel();
            Restore = new CriticalRecoveryRestoreViewModel();
            Impact = new CriticalRecoveryImpactViewModel();
            SnapshotBatches = new List<CriticalRecoverySnapshotBatchViewModel>();
            AuditItems = new List<CriticalRecoveryAuditViewModel>();
            BranchOptions = new List<CriticalRecoveryLookupOption>();
        }

        public string AreaName { get; set; }
        public CriticalRecoveryFilterViewModel Filter { get; set; }
        public CriticalRecoveryRequestViewModel Request { get; set; }
        public CriticalRecoveryRestoreViewModel Restore { get; set; }
        public CriticalRecoveryImpactViewModel Impact { get; set; }
        public IList<CriticalRecoverySnapshotBatchViewModel> SnapshotBatches { get; set; }
        public IList<CriticalRecoveryAuditViewModel> AuditItems { get; set; }
        public IList<CriticalRecoveryLookupOption> BranchOptions { get; set; }
    }

    public class CriticalRecoveryFilterViewModel
    {
        public CriticalRecoveryFilterViewModel()
        {
            InvoiceScope = "SalesOnly";
        }

        public int? BranchId { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public int? InvoiceType { get; set; }
        public string InvoiceScope { get; set; }
        public string InvoiceNo { get; set; }
        public string CashierUserId { get; set; }
        public string CustomerSearch { get; set; }
        public string ClosingStatus { get; set; }
        public string PostedStatus { get; set; }
        public bool HasAccountingEntry { get; set; }
        public bool HasStockMovement { get; set; }
        public bool HasKycLink { get; set; }
        public bool HasGeneratedVoucher { get; set; }
        public string SelectedTransactionIds { get; set; }
    }

    public class CriticalRecoveryRequestViewModel
    {
        public CriticalRecoveryRequestViewModel()
        {
            Mode = CriticalRecoveryMode.CancelOnly;
        }

        public int? RequestId { get; set; }
        public string Mode { get; set; }
        [Required]
        public string Reason { get; set; }
        [Required]
        public string SecondaryPassword { get; set; }
        public string DangerConfirmation { get; set; }
        public bool DryRun { get; set; }
        public bool AllowPhysicalDelete { get; set; }
        public bool RequestHigherApprovalForClosedPeriod { get; set; }
        public bool DeleteOrphanKycRecords { get; set; }
        public string ApproverUserName { get; set; }
        public string ApproverSecondaryPassword { get; set; }
    }

    public class CriticalRecoveryRestoreViewModel
    {
        public long? SnapshotBatchId { get; set; }
        public long? TransactionId { get; set; }
        public string RestoreScope { get; set; }
        public string Reason { get; set; }
        public string SecondaryPassword { get; set; }
    }

    public class CriticalRecoveryImpactViewModel
    {
        public CriticalRecoveryImpactViewModel()
        {
            Invoices = new List<CriticalRecoveryInvoiceImpactViewModel>();
            Dependencies = new List<CriticalRecoveryDependencyViewModel>();
            Warnings = new List<string>();
        }

        public int InvoiceCount { get { return Invoices.Count; } }
        public decimal TotalValue { get { return Invoices.Sum(i => i.Value); } }
        public int DependencyCount { get { return Dependencies.Sum(i => i.RowCount); } }
        public IList<CriticalRecoveryInvoiceImpactViewModel> Invoices { get; set; }
        public IList<CriticalRecoveryDependencyViewModel> Dependencies { get; set; }
        public IList<string> Warnings { get; set; }
    }

    public class CriticalRecoveryInvoiceImpactViewModel
    {
        public long TransactionId { get; set; }
        public string InvoiceNo { get; set; }
        public int InvoiceType { get; set; }
        public int BranchId { get; set; }
        public DateTime TransactionDate { get; set; }
        public string CustomerName { get; set; }
        public decimal Value { get; set; }
        public bool IsPosted { get; set; }
        public bool IsClosed { get; set; }
        public string KycReferenceIds { get; set; }
    }

    public class CriticalRecoveryDependencyViewModel
    {
        public long TransactionId { get; set; }
        public string ModuleName { get; set; }
        public string TableName { get; set; }
        public int RowCount { get; set; }
        public bool IsProtected { get; set; }
        public string ActionPolicy { get; set; }
    }

    public class CriticalRecoverySnapshotBatchViewModel
    {
        public long SnapshotBatchId { get; set; }
        public DateTime CreatedAt { get; set; }
        public string RequestedBy { get; set; }
        public string ApprovedBy { get; set; }
        public string Mode { get; set; }
        public string Reason { get; set; }
        public string Status { get; set; }
        public int InvoiceCount { get; set; }
        public int SnapshotRowCount { get; set; }
    }

    public class CriticalRecoveryAuditViewModel
    {
        public long AuditId { get; set; }
        public DateTime CreatedAt { get; set; }
        public string ActionName { get; set; }
        public string OperatorName { get; set; }
        public string ApproverName { get; set; }
        public string Result { get; set; }
        public string Message { get; set; }
    }

    public class CriticalRecoveryOperationResult
    {
        public bool Success { get; set; }
        public long? SnapshotBatchId { get; set; }
        public int? RequestId { get; set; }
        public string Message { get; set; }
    }

    public class CriticalRecoveryLookupOption
    {
        public string Value { get; set; }
        public string Text { get; set; }
    }
}
