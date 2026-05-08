# LC Completion Pass

Date: 2026-05-07

## Scope

This pass continued the MainErp `FrmLC.frm` migration and focused on completing day-to-day usability. Grid voucher posting moved to the next documented phase: `58_LC_GridVoucherPosting.md`.

Implemented:

- LC workbench search filters now use real lookup lists instead of manual numeric entry for:
  - bank,
  - supplier,
  - branch.
- LC create/edit grids now open with multiple editable placeholder rows, including new LCs, so users can enter rows for:
  - `TBLLCHistory`,
  - `TBLLCMargin`,
  - `TBLLCMargin2`,
  - `tblLCOpenB`.
- MainErp LC actions are wired to legacy-style permissions:
  - add/edit/save use `ScreenJuncUser` add/edit access for `FrmLC`,
  - delete uses delete access,
  - voucher posting/rebuild actions are limited to admin/system users with edit access.
- The LC editor styling now covers `select` controls consistently with text fields.

## Still Intentionally Pending

- Safe row deletion from LC grids with linked note/voucher cleanup.
- Full audit UI for each LC write/post/rebuild/delete action.

## Safety

- No `Areas\Pos` files were modified by this LC pass.
- No `AllScripts.sql` changes.
- No database schema changes.
- Existing implemented LC header save and core voucher posting paths remain unchanged.
