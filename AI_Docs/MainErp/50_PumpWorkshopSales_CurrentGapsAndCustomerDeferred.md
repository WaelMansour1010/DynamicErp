# Pump/Workshop Sales Current Gaps and Pump Deferred Customer Distribution

Date: 2026-05-07

Scope:

- Main ERP migration only.
- Source screen: `F:\Source Code\SatriahMain\Frm\FrmSaleBill6.frm`.
- Pump test database: `Nagahat`.
- Workshop/general test database remains `Eng` unless a richer sales database is selected.

## Implemented In This Pass

- Pump invoice details now read persisted deferred customer rows from `Transaction_DetailsPump`.
- The persisted pump distribution is matched to the correct invoice line by `Transaction_Details.LineID`, with a fallback to `Transaction_Details.ID`.
- The distribution editor now shows a customer lookup from `TblCustemers`, instead of relying only on manual customer codes.
- Existing selected customers are added to the lookup even if they are outside the first 300 lookup rows.
- Before preview/save, selected customer IDs are hydrated from `TblCustemers` and `ACCOUNTS`, so `DetailsPump` is rebuilt with the real customer name.
- Customer account display follows the ERP standard:
  - `Account_Serial - Account_Name`
  - raw `Account_Code` remains internal.
- Pump deferred save procedure updates only:
  - `Transaction_Details.DetailsPump`
  - `Transaction_Details.Deferred`
  - `Transaction_Details.DeferredQty`
  - `Transaction_DetailsPump` rows for the affected line

## VB6 Mapping

The VB6 mini form `FrmItemShowDet` builds `DetailsPump` in this row format:

```text
CustomerId#UnitId#Amount#CustomerName#Qty#UnitPrice#RecNo#@
```

VB6 `FrmSaleBill6` also inserts rows into `Transaction_DetailsPump` for pump invoices.

Important nuance:

- `Transaction_DetailsPump.LineID` maps to the VB6 grid row number / `Transaction_Details.LineID`.
- It is not always the same as `Transaction_Details.ID`.

## Validation

Tested against:

```text
Server: Wael\Sql2019
Database: Nagahat
Transaction_ID: 95484
```

Observed:

- Pump invoice lines: `24`
- Lines with deferred customer distribution: `1`
- Deferred distribution total: `116.50`
- Deferred quantity total: `50`
- Customer: `4 - نجاحات للتجارة والنقل`
- Account display: `1102020010011 - نجاحات للتجارة والنقل`
- Dry-run preview returned:

```text
4#1#116.5#نجاحات للتجارة والنقل#50#2.33#255#@
```

Build:

- `MyERP.sln` builds successfully.

## Still Pending

- Full invoice save for workshop and pump invoices.
- Real accounting voucher generation from `FrmSaleBill6`.
- Real inventory issue/receive voucher generation from `CreateIssueVoucher2` / `CreateRecieveVoucher`.
- Pump-specific `tblPumpType.PercentV` update remains pending until the full VB6 save flow is migrated.
- Print/report execution remains mapped but not enabled.
- No posting/delete/rebuild behavior is enabled yet.

## Safety

- No `Areas\Pos` changes.
- No `AllScripts.sql` changes.
- No production connection changes.
- No Notes/voucher/inventory posting writes were added in this pass.
