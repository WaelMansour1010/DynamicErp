# Active Source Map

Purpose: identify the real production VB6 source files for LC and Project Extracts before migration.

## Rule Used

`F:\Source Code\SatriahMain\Account.vbp` is treated as the active production project map. Files referenced there are the active source. Duplicate files outside those references are reference/obsolete unless later proven otherwise.

Kishny/Cayshny files under `F:\Source Code\SatriahMain\Cayshny\` are not Main ERP source.

## Active LC Sources

| Feature | Active file | `Account.vbp` line | Notes |
| --- | --- | ---: | --- |
| LC main form | `F:\Source Code\SatriahMain\Frm\New frm\FrmLC.frm` | 301 | Active LC save/accounting source. |
| LC types | `F:\Source Code\SatriahMain\Frm\New frm\FrmLCTypes.frm` | 305 | LC type lookup maintenance. |
| LC search | `F:\Source Code\SatriahMain\Frm\New frm\FrmLC_search.frm` | 669 | Search/list form; opens `FrmLC.Search(ID)`. |
| LC report | `F:\Source Code\SatriahMain\Frm\New frm\FrmLC_Report.frm` | 670 | Filtered Crystal report screen. |

Observed duplicate: `F:\Source Code\SatriahMain\Frm\FrmLC.frm` exists but is not referenced by `Account.vbp`. It is smaller/older than the `New frm` version and should not be migrated as the active source.

## Active Project Extract Sources

| Feature | Active file | `Account.vbp` line | Notes |
| --- | --- | ---: | --- |
| Project extract main form | `F:\Source Code\SatriahMain\Frm\New frm\projectsbill.frm` | 242 | Active save/accounting/detail source. |
| Project reports | `F:\Source Code\SatriahMain\Frm\New frm\frmProjectsReports.frm` | 353 | Project accounting/status/extract report hub. |
| Project date operations | `F:\Source Code\SatriahMain\Frm\New frm\projects\FrmDateOpProject.frm` | 639 | Project operation/date support. |
| Project extract search | `F:\Source Code\SatriahMain\Frm\New frm\projectsbill_search.frm` | 673 | Extract list/search form. |
| Project master | `F:\Source Code\SatriahMain\Frm\New frm\projects\projects.frm` | 691 | Project master data source. |
| Project alarms | `F:\Source Code\SatriahMain\Frm\New frm\FrmProjectAlarm.frm` | 715 | Project alerts. |
| Monthly project bill | `F:\Source Code\SatriahMain\Frm\New frm\projects\FrmProjectMonthBill.frm` | 794 | Periodic billing support. |
| Project bill alarms | `F:\Source Code\SatriahMain\Frm\New frm\ProjectsBillAlarm1.frm` | 826 | Large operational/reporting form with project-related reports and alarms. |
| Project BOQ/items | `F:\Source Code\SatriahMain\Frm\New frm\projects\FrmPands.frm` | 850 | Project item/BOQ support. |
| Retention/destruction | `F:\Source Code\SatriahMain\Frm\New frm\projects\FrmDestructionRet.frm` | 853 | Retention/destruction support. |
| Subcontractor contract | `F:\Source Code\SatriahMain\Frm\New frm\frmSubcontractorContractl.frm` | 908 | Subcontractor contract support. |
| Project search | `F:\Source Code\SatriahMain\Frm\New frm\FrmProjectSearch.frm` | 914 | Project master search. |

## Shared Accounting Helpers

| Helper | Active file | Migration relevance |
| --- | --- | --- |
| Account create/edit/voucher row helper | `F:\Source Code\SatriahMain\Bas\ModAccounts.bas` | Required for account generation and `AddNewDev` posting behavior. |
| Registry/global accounting helpers | `F:\Source Code\SatriahMain\Class\registry.bas` | Required for `get_opening_balance_voucher_id`, `get_account_code_branch`, `Voucher_coding`, opening balance helpers. |

## Obsolete/Duplicate Handling

- Older forms under `F:\Source Code\SatriahMain\Frm\...` without `New frm` references should not drive migration.
- Cayshny copies are Kishny/POS reference only.
- If a duplicate differs materially, compare it only to explain historical behavior; do not migrate from it unless `Account.vbp` is changed by the legacy team.

## Open Follow-Up

Before implementation, search `Account.vbp` for modules/classes defining `new_id`, `CreateNotes`, `Notes_coding`, `Voucher_coding`, `SendTopost`, `SaveQRCode`, and e-invoice helpers, then map those active files explicitly.
