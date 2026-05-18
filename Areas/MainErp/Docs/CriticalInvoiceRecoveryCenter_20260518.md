# Controlled Invoice Reversal & Recovery Center

## Purpose

This module is a restricted enterprise recovery center for sales invoices shared by MainERP and POS. It is designed for reversible recovery, not casual deletion.

Default behavior:
- Preserve KYC master data.
- Analyze dependencies before execution.
- Require super-admin/critical permission.
- Require secondary password, reason, danger confirmation, and dual approval.
- Snapshot all affected rows before any reversal.

## Access

Routes:
- `/MainErp/CriticalRecovery`
- `/Pos/CriticalRecovery`

Required permission/role:
- `CanAccessCriticalRecoveryCenter`
- or `SuperAdmin`
- or `Administrators`

Secondary critical-operation password:
- Configure per authorized admin with `dbo.usp_CriticalRecovery_SetSecondaryPassword`.
- Normal login password is not reused by this module.
- Initiator, approver, and restore operator each need a configured secondary credential.

Database script:
- `Areas/MainErp/Sql/20260518_CriticalInvoiceRecoveryCenter.sql`

## Workflow

1. Search invoices by branch, period, invoice type, invoice number, cashier/user, customer/token/phone, or selected `Transaction_ID` values.
2. Run preview/dry-run. The dependency analyzer returns affected invoices, accounting rows, inventory rows, POS closure links, generated voucher links, commission rows, and KYC link references.
3. Export affected rows to Excel before execution.
4. First admin initiates the request with reason, secondary password, and `I UNDERSTAND`.
5. Second authorized admin approves and executes.
6. The snapshot engine writes:
   - `CriticalRecoverySnapshotBatch`
   - `CriticalRecoverySnapshotRow`
   - `CriticalRecoveryArchive_<OriginalTable>` copies
   - `CriticalRecoveryAudit`
7. Execution soft-marks invoices by default. Physical delete is only allowed when explicitly selected and still never deletes KYC master tables.
8. Restore can rebuild from archive-copy tables for the batch, one invoice, or selected scope.

## Affected Modules

Core:
- `Transactions`
- `Transaction_Details`
- `Notes`
- `DOUBLE_ENTREY_VOUCHERS`

Extended when present:
- `Payments`
- `POSClosures`
- `TblTransctionIDES`
- `TblSalesrepComm`
- `TransactionKycLinks`

Protected KYC master tables:
- `KycCustomers`
- `KycAttachments`
- `KycCards`

These KYC tables are registered as protected and are not deleted by the recovery procedures. Only transactional invoice-to-KYC links are archived/restored.

## Closing Protection

Policy is stored in `CriticalRecoveryPolicy`.

Default:
- `ClosedPeriodPolicy = RequireHigherApproval`

Recommended production hardening:
- Add local closed-day/audited-period/export-period tables to `CriticalRecoveryTableMap`.
- Extend `usp_CriticalRecovery_AnalyzeInvoices` to set `IsClosed=1` from the client-specific closure tables.
- Block execution when the policy is `Block`.
- Rebuild affected POS closures after approved rollback when the policy is `Rebuild`.

## KYC Policy

Mandatory default:
- KYC master records remain untouched.
- KYC attachment, national ID, phone, card, token, and customer identity rows are not deleted.
- Snapshot stores KYC reference IDs for reconnecting restored invoices.
- Restore reconnects to existing KYC records.

Optional orphan KYC deletion:
- The included execution procedures intentionally block this path.
- If a client insists on physical orphan cleanup, create a separate compliance-approved procedure that validates no invoice, active card/token, audit/history/security, closure/report/compliance, or non-target reference exists.
- Keep the UI option off by default.

## Testing Checklist

- Unauthorized user cannot open MainERP or POS recovery routes.
- Super admin can preview without data changes.
- Missing reason is rejected.
- Missing secondary password is rejected.
- Incorrect danger phrase is rejected.
- Same user cannot approve their own request.
- Snapshot row count is greater than zero before execution.
- Physical delete option off: invoice is soft-marked only.
- Physical delete option on: dependent rows are removed only after archive copy exists.
- KYC master tables remain unchanged after all modes.
- Restore full batch rebuilds missing core rows.
- Restore single invoice rebuilds only that invoice rows.
- Restore accounting-only and inventory-only scopes are validated against client table map before production enablement.
- Failure during execution rolls back transaction and records audit failure.
- Concurrent execution is blocked by `CriticalRecoveryExecutionLock`.
- Excel export opens and matches preview counts.

## Disaster Recovery Scenario

1. Pick one test branch and one old test invoice.
2. Back up the database.
3. Run preview and export affected rows.
4. Initiate request as admin A.
5. Approve as admin B in dry-run mode.
6. Confirm no business data changed and snapshot/audit rows exist.
7. Repeat with `CancelOnly`.
8. Confirm reports exclude/flag the cancelled invoice according to business rules.
9. Restore the invoice from the created snapshot batch.
10. Compare restored invoice, accounting, inventory, payments, KYC link references, and reports against the Excel export.
11. Repeat with forced failure by locking one dependent table and confirm rollback plus audit failure.
12. Restore the database backup and compare counts with the restored state.
