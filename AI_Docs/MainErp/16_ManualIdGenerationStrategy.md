# Manual ID Generation Strategy

Implemented files:

- `Areas\MainErp\Interfaces\IManualIdGenerator.cs`
- `Areas\MainErp\Infrastructure\ManualIdTarget.cs`
- `Areas\MainErp\Infrastructure\ManualIdAllocation.cs`
- `Areas\MainErp\Services\Accounting\ManualIdGenerator.cs`

## Supported Targets

- `Notes.NoteID`
- `TblLC.TblLCID`
- `project_billl.id`
- `DOUBLE_ENTREY_VOUCHERS.Double_Entry_Vouchers_ID`
- `DOUBLE_ENTREY_VOUCHERS1.Double_Entry_Vouchers_ID`

## Current Safe Placeholder

The first implementation does not use blind `MAX + 1`. It uses explicit caller-owned transaction, SQL Server `sp_getapplock` with transaction ownership, `UPDLOCK, HOLDLOCK` during next-id read, and whitelisted target table/column names only.

This is still a placeholder because the VB6 `new_id` function must be fully mapped before final posting. The placeholder is intentionally centralized so future replacement does not leak through business services.

## Preview Mode

Preview takes the same lock shape and rolls back, so operators can see the candidate id without reserving it.

## Risks

- `get_opening_balance_voucher_id` in VB6 currently returns `MyTime`, not a normal sequence.
- Production may have external VB6 writers during migration; this requires deployment-time coordination.
- Final strategy may need a dedicated sequence/allocation table because SQL Server 2012 has no native sequence equivalent suitable for all legacy semantics.
