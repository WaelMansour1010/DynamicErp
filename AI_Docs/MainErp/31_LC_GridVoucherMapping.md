# LC Grid Voucher Mapping

Date: 2026-05-07

Source of truth: `F:\Source Code\SatriahMain\Frm\New frm\FrmLC.frm`

This document maps the VB6 LC grids to the MainErp read-only trace implementation. No save, posting, account creation, or destructive rebuild was implemented.

## VB6 Grid Summary

| VB6 grid | Main table | Current web behavior | Voucher behavior in VB6 |
| --- | --- | --- | --- |
| `GrdBondHistory` | `TBLLCHistory` | Read-only dynamic grid when table exists | `createVoucher2` selects `TBLLCHistory`; `CREATE_VOUCHER_GE2` can create linked voucher rows. |
| `GrdMargin` / `GrdMargin2` | `TBLLCMargin` | Read-only dynamic grid when table exists | Used for margin rows and margin payments; normal voucher rows go to `DOUBLE_ENTREY_VOUCHERS`. |
| `GrdMargin3` | `tblLCOpenB` | Read-only dynamic grid when table exists | Opening balance grid path; can use `Notes1` and `DOUBLE_ENTREY_VOUCHERS1`. |
| `GrdMargin4` | `TBLLCMargin2` | Read-only dynamic grid when table exists | Refinance/acceptance/payment path; can use margin, bank, acceptance, and VAT-related account logic. |

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

## Pending Before Write Migration

- Exact column-level meaning for every grid column must be confirmed.
- `TypeGrid` values must be mapped fully.
- `mIsPay`, `mIsOpenBalance`, VAT, and negative-value debit/credit flipping must be implemented in preview before any save.
- No grid row save/post/delete is allowed until transaction and rollback behavior is reproduced.
