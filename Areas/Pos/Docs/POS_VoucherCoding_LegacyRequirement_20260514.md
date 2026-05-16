# POS Voucher Coding Legacy Requirement Audit - 2026-05-14

## Scope

Primary objects:

- `dbo.usp_Voucher_coding_V2`
- `dbo.usp_GetNextSerial_V2`
- `dbo.SerialCounters_V2`
- `dbo.SerialTableMapping`
- `dbo.sanad_numbering`
- `dbo.Transactions.NoteSerial1`

POS paths:

- Sales invoice: `Transaction_Type = 21`, `Sanad_No = 7`
- Card issue voucher: `Transaction_Type = 19`, `Sanad_No = 10`

## Current Web POS Behavior

`dbo.usp_POS_SaveTransaction` calls `dbo.usp_Voucher_coding_V2` before inserting the invoice transaction. For card mode it calls it again for the issue voucher.

`dbo.usp_Voucher_coding_V2` is a wrapper. It maps the request to `SourceTable = 'Transactions'` when `@Transaction_Type <> 0`, then calls `dbo.usp_GetNextSerial_V2`.

`dbo.usp_GetNextSerial_V2` allocates the next tail by updating `dbo.SerialCounters_V2`:

- key shape: `SourceTable + BranchID + TypeCode + Prefix + StoreID + YearNum + MonthNum`
- lock hint: `WITH (UPDLOCK, SERIALIZABLE)`
- first-use fallback: dynamic `MAX(NoteSerial1)` scan from the source table
- format: branch code + year + month + padded tail for monthly numbering

## Generated Values

| Value | Where Stored | Business Meaning | Strict Requirement |
| --- | --- | --- | --- |
| `@Result` / `NoteSerial1` | `Transactions.NoteSerial1` | visible invoice/voucher number used in UI, printing, reports, and search | Must keep readable format and avoid new duplicates within the configured POS scope. Not proven gap-free. Not globally unique. |
| `@mSerInv` / tail number | procedure output only | tail used by caller for diagnostics/display | Not a durable key. Can remain an implementation output. |
| `SerialCounters_V2.CurrentTail` | allocator state | next tail source for fast allocation | Implementation detail, not customer-facing. |
| formatted branch/year/month prefix | part of `NoteSerial1` | legacy-readable code | Preserve current POS display format unless business owner approves a visible number change. |

## VB6 Evidence

Legacy VB6 function `Voucher_coding` in:

`F:\Source Code\SatriahMain\Cayshny\Bas\registry04082020.bas`

Observed behavior:

- Reads `sanad_numbering` by `branch_no` and `sanad_no`.
- For monthly numbering, runs `SELECT MAX(NoteSerial1)` against `Transactions` or `Notes` filtered by branch, type, year, month, prefix, and sometimes store.
- Builds `auto_sanad_no` from year/month and padded tail.
- Applies branch/store display prefix logic.
- Uses normal ADODB reads; no `UPDLOCK`, `HOLDLOCK`, `SERIALIZABLE`, `TABLOCKX`, or `sp_getapplock` was found in this path.

Conclusion: VB6 treated this as a legacy display/business voucher number generated from current data, not as a globally serialized allocator row. The web implementation is stricter because it centralizes allocation into `SerialCounters_V2`.

## Data Evidence From Test Copy

Sample database used for inspection: `Cash_FullSaveDEV_20260514`.

`SerialTableMapping`:

| SourceTable | BranchField | DateField | SerialField | TypeField | SerialDataType |
| --- | --- | --- | --- | --- | --- |
| `Transactions` | `BranchId` | `Transaction_Date` | `NoteSerial1` | `Transaction_Type` | `VARCHAR` |
| `Notes` | `branch_no` | `NoteDate` | `NoteSerial1` | `NoteType` | `FLOAT` |

`sanad_numbering` for sampled branches and POS sanad numbers:

| Sanad_No | Meaning | numbering_id | no_of_digit | YearDigit |
| --- | --- | --- | --- | --- |
| 7 | sales invoice | 2 monthly | 3 | 2 |
| 10 | issue voucher | 2 monthly | 3 | 2 |

Store scope correction:

| Sanad_No | StoreCoding min | StoreCoding max | Conclusion |
| --- | ---: | ---: | --- |
| 7 | 0 | 0 | StoreID must not participate in the effective POS serial scope for current Kishny data. |
| 10 | 0 | 0 | StoreID must not participate in the effective POS serial scope for current Kishny data. |

Recent May 2026 pressure showed POS invoice volume concentrated per branch/store/type/month. Examples:

| Branch | Store | Type | Rows | Distinct Serials |
| --- | --- | --- | --- | --- |
| 45 | 44 | 21 | 602 | 602 |
| 44 | 43 | 21 | 592 | 592 |
| 23 | 26 | 21 | 572 | 572 |
| 54 | 56 | 21 | 482 | 482 |
| 69 | 66 | 21 | 464 | 462 |

Duplicate `NoteSerial1` values exist in recent historical data for `Transaction_Type = 21`, especially in high-volume branch/month ranges. This proves the field is not historically gap-free and not globally unique. It is still important as a human-facing invoice/voucher number, so removing it from the save path is not safe.

## Requirement Classification

| Question | Evidence | Conclusion |
| --- | --- | --- |
| Is `NoteSerial1` a real business key? | Used in UI, printing, descriptions, search, and reports. | It is business-visible, but not the durable relational key. |
| Is it printed to customer? | POS save descriptions and reports use invoice number text. VB6 sales reports include `Transactions.NoteSerial1`. | Yes, preserve format. |
| Is it required to be unique globally? | Number includes branch/month/tail and duplicates exist across data. | No global uniqueness. |
| Is it required to be unique inside branch/type/month/store? | Counter scope and VB6 filters imply this intended scope. Some duplicates exist historically. | Preserve best effort going forward inside this scope. |
| Is it required to be sequential? | VB6 uses `MAX + 1`; web counter increments. | Monotonic display sequence is expected. |
| Is it required to be gap-free? | No rollback-safe or VB6-safe guarantee; existing data has gaps/duplicates. | No. |
| Is it branch/year/month scoped? | `sanad_numbering` uses monthly numbering and the generated format includes branch/year/month. | Yes for POS `Sanad_No` 7 and 10. |
| Did VB6 enforce locking? | VB6 path uses reads and `MAX`, not explicit serializable/update locks. | No. |
| Did web POS make it stricter? | Web path centralizes every save through one counter row per scope using `UPDLOCK, SERIALIZABLE`. | Yes. |

## Answer

`dbo.usp_Voucher_coding_V2` is protecting a real human-facing voucher/invoice number, not a disposable display field like `DEV_Serial`.

However, the evidence does not support gap-free or global serialization requirements. The strict synchronized allocator is stronger than VB6 behavior and should be optimized only within the real scope:

- source table
- branch
- transaction type
- prefix
- store when supplied
- year/month according to `sanad_numbering`

The next safe direction is to preserve the visible number and scope while shortening the lock path around `SerialCounters_V2`.

## Configurable Scope

Added system setting:

`TblOptions.POSVoucherSerialScope`

Allowed values:

- `Company`
- `Branch`
- `BranchStore`

Default is `Company` for backward compatibility. The effective scope still respects `sanad_numbering.StoreCoding`:

| Configured Scope | StoreCoding | Effective Scope |
| --- | ---: | --- |
| Company | any | Company |
| Branch | any | Branch |
| BranchStore | 0 | Branch |
| BranchStore | 1 | BranchStore |

For current Kishny POS data, `StoreCoding = 0` for `Sanad_No = 7` and `10`, so `BranchStore` resolves to `Branch`. Passing `StoreID` from POS no longer automatically creates per-store counters.
