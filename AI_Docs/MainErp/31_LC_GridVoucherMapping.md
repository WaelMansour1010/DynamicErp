# LC Grid Voucher Mapping

Date: 2026-05-07

Source of truth: `F:\Source Code\SatriahMain\Frm\New frm\FrmLC.frm`

This document maps the VB6 LC grids to the MainErp trace and guarded grid voucher posting implementation.

## VB6 Grid Summary

| VB6 grid | Main table | Current web behavior | Voucher behavior in VB6 |
| --- | --- | --- | --- |
| `GrdBondHistory` | `TBLLCHistory` | editable grid plus guarded missing-voucher creation | `NoteType=22004`; amount is `(AmountPlus - AmountMin) * PercentV / 100`; negative value reverses debit/credit. |
| `GrdMargin` / `GrdMargin2` | `TBLLCMargin` | editable grid plus guarded missing-voucher creation | `NoteType=22002` for amount and `22003` for payment; normal voucher rows go to `DOUBLE_ENTREY_VOUCHERS`. |
| `GrdMargin3` | `tblLCOpenB` | editable grid plus guarded missing-voucher creation | `NoteType=22006`; insurance, expense, VAT, and bank split; normal voucher rows go to `DOUBLE_ENTREY_VOUCHERS`. |
| `GrdMargin4` | `TBLLCMargin2` | editable grid plus guarded missing-voucher creation | `NoteType=22008` for amount and `22009` for payment; `IsOpenBalance` amount rows use `Notes1` and `DOUBLE_ENTREY_VOUCHERS1`. |

## VB6 Functions

- `createVoucher2(Row, mIsPay, TypeGrid)` chooses the metadata table:
  - `TBLLCMargin`
  - `TBLLCHistory`
  - `tblLCOpenB`
  - `TBLLCMargin2`
- `CREATE_VOUCHER_GE2(...)` writes the actual double-entry rows.
- When `mIsOpenBalance=True`, rows go to `DOUBLE_ENTREY_VOUCHERS1`.
- Otherwise rows go to `DOUBLE_ENTREY_VOUCHERS`.
- Negative values can flip debit/credit account selection.

## MainErp Read-Only Mapping

Implemented in:

- `Areas\MainErp\Repositories\LC\LcReadRepository.cs`
- `Areas\MainErp\ViewModels\LC\LCIndexViewModel.cs`
- `Areas\MainErp\Views\LC\Index.cshtml`

Repository methods:

- `LoadGridSections`
- `LoadGridSection`
- `LoadLinkedNotes`
- `LoadVoucherTrace`
- `LoadNormalVoucherTrace`
- `LoadOpeningVoucherTrace`

The repository checks each table dynamically:

- table exists,
- has `TblLCID`,
- selects known important columns if present,
- avoids crashing when columns/tables differ between databases.

## MainErp Posting Mapping

Implemented in:

- `Areas\MainErp\Repositories\LC\LcWriteRepository.cs`
- `Areas\MainErp\Controllers\LCController.cs`
- `Areas\MainErp\Views\LC\Index.cshtml`

Entry point:

- `CreateGridVouchers(int tblLcId, int? userId)`

Safety:

- Creates missing row vouchers only.
- Does not delete or rebuild existing row vouchers.
- Header voucher rebuild does not delete grid notes.
- Full LC delete still removes grid rows and all linked notes/vouchers, behind confirmation and permissions.

## Voucher Opening

Grid rows expose voucher links if the row contains:

- `NoteID` or `NoteId`
- `NoteID2`
- `NoteID3`

Routes:

- `/MainErp/JournalEntries/DetailsByNote/{noteId}`
- `/MainErp/JournalEntries/DetailsByVoucher/{voucherId}`

## Current Sample Validation

Validated by read-only SQL against:

- Server: `Wael\Sql2019`
- Database: `Eng`
- LC sample: `TblLCID = 195`

Observed:

- `Notes` rows exist for LC 195.
- `TBLLCMargin` had rows.
- `TBLLCMargin2` had rows.
- Normal voucher lines were found through `Notes -> DOUBLE_ENTREY_VOUCHERS`.
- Opening table path returned no rows for the sampled opening voucher query, which is acceptable and should be validated with additional LCs.

## Pending

- Individual grid-row delete/rebuild workflow with linked note cleanup.
- Runtime validation for `TBLLCHistory`, because `Eng` currently has no rows in that table.
- Full audit UI for row voucher creation, rebuild, and delete operations.
