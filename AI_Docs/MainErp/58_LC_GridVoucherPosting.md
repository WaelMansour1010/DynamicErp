# LC Grid Voucher Posting Phase

Date: 2026-05-07

## Scope

This phase continues the real `FrmLC.frm` migration by adding a guarded posting path for LC grid rows. It is intended for the legacy test database `Eng` and uses `MainErp_ConnectionString`.

Implemented posting coverage:

- `TBLLCHistory` / VB6 `GrdBondHistory`, `TypeGrid=3`
  - history voucher: `NoteType=22004`
  - voucher value follows VB6: `(AmountPlus - AmountMin) * PercentV / 100`
  - debit/credit direction reverses when the calculated value is negative.
- `TBLLCMargin` / VB6 `GrdMargin2`, `TypeGrid=1`
  - amount voucher: `NoteType=22002`
  - payment voucher: `NoteType=22003`
- `TBLLCMargin2` / VB6 `GrdMargin4`, `TypeGrid=6`
  - amount voucher: `NoteType=22008`
  - payment voucher: `NoteType=22009`
  - opening-balance rows use `Notes1` and `DOUBLE_ENTREY_VOUCHERS1` with `NoteType=101`
- `tblLCOpenB` / VB6 `GrdMargin3`, `TypeGrid=4`
  - opening/guarantee voucher: `NoteType=22006`
  - insurance, expense, VAT, and bank lines are split according to the VB6 `CREATE_VOUCHER_GE2` path.

## Safety Behavior

- The new action creates missing grid vouchers only.
- Existing grid voucher rows are not deleted or rebuilt.
- Existing grid note links are preserved during core LC voucher rebuild.
- LC posting/rebuild/delete operations write to `MainErp_AuditLog` when the table exists.
- Full LC delete still removes the LC, its grids, normal notes, opening notes, and generated LC accounts, but remains protected by confirmation and permissions.
- No `Areas\Pos` changes.
- No `AllScripts.sql` changes.
- No `AllScripts.sql` changes. A MainErp-local audit setup script was added under `Areas\MainErp\Sql`.

## UI

Added an LC workbench action:

- `إنشاء قيود الجريدات`
- `تاريخ العمليات` now reads `MainErp_AuditLog` and displays LC operations, users, timestamps, and optional before/after snapshots.

The button is visible only for users allowed to post LC vouchers. It is separate from core voucher rebuild because grid vouchers are row-level accounting effects and should not be silently recreated with header vouchers.

## VB6 Mapping

Primary VB6 routines used:

- `createVoucher2`
- `CREATE_VOUCHER_GE2`
- `CmdCreateV_Click`

Mapped grid routing:

- `TypeGrid < 3` maps to `TBLLCMargin`.
- `TypeGrid = 4` maps to `tblLCOpenB`.
- `TypeGrid = 6` maps to `TBLLCMargin2`.

## 2026-05-07 Runtime UI Validation on Eng

Tested from the actual MainErp UI:

- Route: `/MainErp/LC?selectedId=198`
- User: `admin`
- Database: `Wael\Sql2019 / Eng`
- Action: `إنشاء قيود الجريدات`
- Sample row: `TBLLCMargin2.ID=50858`, amount `321.45`

Result:

- Created `Notes.NoteID = 222101`.
- Created `Notes.NoteSerial = 202604465`.
- Created 2 rows in `DOUBLE_ENTREY_VOUCHERS`.
- Debit total: `321.45`.
- Credit total: `321.45`.
- Difference: `0.00`.
- Audit row written: `LC.PostGridVouchers`.
- The LC accounting timeline showed the generated voucher link.
- `/MainErp/JournalEntries/DetailsByNote/222101` opened successfully and showed the two balanced voucher lines.

## Row-Level Delete and Grid Rebuild

Implemented after the runtime test:

- Safe row delete action for the whitelisted LC grid tables only:
  - `TBLLCHistory`
  - `TBLLCMargin`
  - `TBLLCMargin2`
  - `tblLCOpenB`
- Confirmation format:
  - `DELETE-LC-GRID-{TblLCID}-{SourceTable}-{RowID}`
- Delete behavior:
  - verifies the row belongs to the selected `TblLCID`.
  - deletes only row-level `DOUBLE_ENTREY_VOUCHERS` / `Notes`.
  - deletes `DOUBLE_ENTREY_VOUCHERS1` / `Notes1` only for `TBLLCMargin2.IsOpenBalance = 1`.
  - deletes the grid row itself.
  - writes `LC.DeleteGridRow` to `MainErp_AuditLog`.
  - does not touch header/core LC vouchers.

Runtime delete validation:

- Deleted `TBLLCMargin2.ID=50858` from `/MainErp/LC?selectedId=198`.
- Verified `TBLLCMargin2` row count for `ID=50858`: `0`.
- Verified `Notes.NoteID=222101`: `0`.
- Verified `DOUBLE_ENTREY_VOUCHERS.Notes_ID=222101`: `0`.
- Audit row written: `LC.DeleteGridRow`.

Also implemented a protected grid-only rebuild action:

- Confirmation format:
  - `REBUILD-LC-GRIDS-{TblLCID}`
- Deletes and recreates grid row vouchers only.
- Does not clear or rebuild the LC header/opening/close vouchers.

Still pending:

- Runtime validation for `TBLLCHistory` once a real Eng sample row exists.
- Dedicated LC Crystal/Web report wiring.
- Final granular permission names such as `MainErp.LC.PostHeader`, `MainErp.LC.PostGrids`, `MainErp.LC.Rebuild`, `MainErp.LC.Delete`, and `MainErp.LC.Reports`.
- A richer audit detail screen with filtering and before/after highlighting.

## Eng Read-Only Validation

Sample checks against `Eng`:

- `TblLCID=195`, `LCNO=IMCC045026`
  - `TBLLCMargin`: 1 row.
  - `TBLLCMargin2`: 1 row.
  - Both rows already had note IDs; missing payment voucher rows were detected in read-only diagnostics.
- `TblLCID=197`, `LCNO=IMCC045825`
  - `TBLLCMargin2`: 2 rows.
- `TblLCID=198`, `LCNO=WEBTEST-LC-20260507184202`
  - test LC with `TBLLCMargin2` row missing both note IDs.
- `TBLLCHistory`
  - table exists in `Eng` but currently has no rows, so the implementation is compile-validated and mapped from VB6 but still needs a sample row validation.

No direct data-changing test was executed in this documentation pass. The code path is transaction-wrapped and should be tested on an agreed LC sample before use on non-test data.

## Audit Setup

Script:

- `Areas\MainErp\Sql\05_MainErp_AuditLog.sql`

Applied to `Wael\Sql2019 / Eng` on 2026-05-07.

Validation:

- `dbo.MainErp_AuditLog` exists in `Eng`.
- Initial row count after setup was `0`.
