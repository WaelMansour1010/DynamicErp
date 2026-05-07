# Course Correction - Real MainErp Migration

## Decision

MainErp is not a new theoretical ERP. It is a web migration of the real VB6 ERP.

From this point:

- VB6 active forms are the primary source of truth.
- Kishny/POS web modules are reusable implementation references only where business-neutral.
- Generic placeholders are not acceptable unless explicitly marked as temporary technical test pages.
- Screens must be labeled as one of:
  - `Real migrated from VB6`
  - `Reused from Kishny generic module`
  - `Temporary technical test page`

## Immediate Correction

### LC

The prior LC list/details was only a primitive technical read page. It is now treated as a temporary test page embedded under a real LC workflow shell.

The real target is `F:\Source Code\SatriahMain\Frm\New frm\FrmLC.frm`.

The web LC screen must expose:

- بيانات الاعتماد
- نوع الاعتماد
- البنك
- المورد
- العملة وسعر الصرف
- قيمة الاعتماد
- حساب الاعتماد
- حساب الهامش
- حساب القبول
- حساب المصروفات
- التواريخ
- الرصيد الافتتاحي
- الملاحظات
- LC history
- margins
- margin payments
- open balance rows
- original toolbar actions

Save/post/delete remain disabled until exact VB6 behavior is migrated.

### Project Extracts

The real target is `F:\Source Code\SatriahMain\Frm\New frm\projectsbill.frm`.

The web screen must expose:

- بيانات المستخلص
- المشروع / العميل / مقاول الباطن
- Fg_Journal project lines
- previous/current/cumulative quantities and values
- deductions
- retention/performance bond
- advance payment allocation
- VAT
- approval flow
- accounting impact

Save/post/delete remain disabled until exact VB6 behavior is migrated.

### Kishny Reuse

Kishny generic modules must be adapted into MainErp where safe:

- purchases
- stock transfers
- journal entries
- accounting reports
- sales reports
- dashboard

Excluded:

- cards/tokens
- KYC
- commissions
- cashier closing
- POS service flows
- POS session behavior
- Kishny-only reports

## Current Implementation Labels

| Module | Current label | Notes |
| --- | --- | --- |
| LC | Real migrated from VB6 + temporary technical list | Layout reflects `FrmLC.frm`; save/post still disabled. |
| Project Extracts | Real migrated from VB6 + temporary technical list | Layout reflects `projectsbill.frm`; line grids still pending. |
| Purchases | Reused from Kishny generic module | MainErp-safe UI adapted; save/import disabled pending dependency split. |
| Stock Transfers | Reused from Kishny generic module | MainErp-safe UI adapted; save/import disabled pending dependency split. |
| Journal Entries | Reused from Kishny generic module | Read-only list/search using MainErp DB; richer voucher shell added. |
| Accounting Reports | Reused from Kishny generic module | Report chooser/filter UX adapted; uses MainErp read-only reports. |
| Sales Reports | Reused from Kishny generic module | Report hub adapted; POS/Kishny reports excluded. |
| Dashboard | Reused from Kishny generic module | Generic KPI/navigation dashboard; POS-only widgets excluded. |

## Next Real Migration Work

1. LC: load `TBLLCHistory`, `TBLLCMargin`, `TBLLCMargin2`, `tblLCOpenB` into real grids.
2. Project Extracts: load `project_bill_details` into `Fg_Journal` style grid.
3. Project Extracts: load `TblPayPrePayed` and `TblProjePayPrePayed`.
4. Purchases/Stock Transfers: create MainErp repositories against `MainErp_ConnectionString`, then enable read-only search and detail JSON.
5. Only after that, implement draft save matching VB6 or approved generic ERP save behavior.
