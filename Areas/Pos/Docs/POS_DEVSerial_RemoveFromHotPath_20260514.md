# POS DEV_Serial Remove From Hot Path - 2026-05-14

## Decision

`DEV_Serial` is now treated as a legacy text/display field only.

It is not a business key, not unique, not sequential, and not gap-free. POS save must not wait on a `DEV_Serial` allocator.

## Implemented Change

| Area | Change | Reason |
| --- | --- | --- |
| `dbo.usp_POS_SaveTransaction` source script | Removed the `POS_DEVSerialAllocator` update/insert block and the `DEV_Serial allocation` timing stage. `DEV_Serial` is now assigned as `yyyyMMdd-DoubleEntryVoucherId`. | Removes same-day centralized allocator locking from POS save while keeping a readable display value. |
| POS closing accounting helper | Replaced `COUNT(DISTINCT Double_Entry_Vouchers_ID) + 1` DEV serial calculation with `yyyyMMdd-voucherId`. | Avoids scanning `DOUBLE_ENTREY_VOUCHERS` for a display-only field. |
| Save retry classification | Removed the obsolete retry classification for DEV serial allocation failure. | There is no allocator failure path after this change. |
| Runtime preflight | Removed `POS_DEVSerialAllocator` as a required runtime object. | POS save no longer depends on this table. |
| Full-save correlation query | Removed DEV serial stage aggregation. | The timing stage should disappear from new save runs. |

## Preserved Accounting Keys

The durable accounting identity remains unchanged:

| Key | Status |
| --- | --- |
| `Double_Entry_Vouchers_ID` | Preserved |
| `DEV_ID_Line_No` | Preserved |
| `Notes_ID` | Preserved |
| `Transaction_ID` | Preserved |
| debit/credit amounts | Preserved |

## Validation

Quick validation against `Cash_FullSaveDEV_20260514`:

| Check | Result |
| --- | --- |
| `dbo.usp_POS_SaveTransaction` contains `POS_DEVSerialAllocator` | No |
| `dbo.usp_POS_SaveTransaction` contains `DEV_Serial allocation` stage | No |
| cheap text assignment exists | Yes |

No repeated DEV serial benchmark was run after the final business decision. This is intentional.

## Rollback

Use the manual rollback path in:

`Areas/Pos/Sql/97_POS_DEVSerial_RemoveFromHotPath.sql`

The script stores the previous procedure definition in `dbo.POS_DEVSerialHotPathRollback` before applying the change.
