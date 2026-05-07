# LC Analysis Index

## Prior Analysis Found

- `F:\Source Code\SatriahMain\AI_Docs\Screens\FrmLC_Verified.md`
- `F:\Source Code\SatriahMain\FrmLC_Verified.md`

## Main Legacy VB6 Files Found

- `F:\Source Code\SatriahMain\Frm\New frm\FrmLC.frm`
- `F:\Source Code\SatriahMain\Frm\New frm\FrmLCTypes.frm`
- `F:\Source Code\SatriahMain\Frm\New frm\FrmLC_search.frm`
- `F:\Source Code\SatriahMain\Frm\New frm\FrmLC_Report.frm`
- `F:\Source Code\SatriahMain\Frm\FrmLC.frm`

`Account.vbp` references:

- `Form=Frm\New frm\FrmLC.frm`
- `Form=Frm\New frm\FrmLCTypes.frm`
- `Form=Frm\New frm\FrmLC_search.frm`
- `Form=Frm\New frm\FrmLC_Report.frm`

## Legacy Tables And Accounting Objects Mentioned

- `TBLLC`
- `ACCOUNTS`
- `DOUBLE_ENTREY_VOUCHERS1`
- Opening balance voucher fields including `opening_balance_voucher_id`

## Key Legacy Functions Mentioned

- `ModAccounts.AddNewAccount`
- `ModAccounts.EditAccount`
- `ModAccounts.AddNewDev`
- `get_account_code_branch`
- `get_opening_balance_voucher_id`
- `new_id`

## Verified Behavior Summary

- `FrmLC.SaveData` creates or updates an LC master record.
- New LC records create a chart-of-accounts account linked by `TblLCID`.
- Opening balance logic creates debit and credit rows in `DOUBLE_ENTREY_VOUCHERS1` when complete accounting is enabled.
- Edit flow cleans existing opening balance rows by voucher id before recreating them.
- The VB6 flow uses a transaction with rollback on error.

## AllScripts.sql Status

Searched `F:\Source Code\SatriahMain\Main Script\AllScripts.sql` for `TBLLC`, `TblLC`, and direct LC table definitions. No direct LC table definition was found in the current file. Existing procedure definitions in the script should still be reviewed before any LC SQL is added.

## Gaps Before Implementation

- Confirm live schema for `TBLLC`, `ACCOUNTS`, and `DOUBLE_ENTREY_VOUCHERS1`.
- Confirm the active source version between `Frm\New frm\FrmLC.frm` and older `Frm\FrmLC.frm`.
- Confirm permission names and report requirements.
- Confirm whether LC reports should be migrated from Crystal reports or rebuilt.
