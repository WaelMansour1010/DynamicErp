# POS DEV_Serial Legacy Requirement Investigation - 2026-05-14

## Scope

- Legacy VB6 Kishny source under `F:\Source Code\SatriahMain\Cayshny`.
- DynamicErp POS code and SQL under `F:\Source Code\DynamicErp\Areas\Pos`.
- Shared SQL/report references were inspected only to understand `dbo.DOUBLE_ENTREY_VOUCHERS.DEV_Serial` usage.
- No production business logic was changed in this investigation.

## Executive Conclusion

`DEV_Serial` is not proven to be a strict business-critical key. The evidence shows it is a legacy daily display/search serial for accounting vouchers:

- VB6 generates it from `COUNT(DISTINCT Double_Entry_Vouchers_ID)` for the same `RecordDate`, formatted as `yyyyMMdd0<count>`.
- VB6 uses it as an optional field in `AddNewDev`; if a caller passes nothing, it is generated automatically.
- The data allows `NULL`, duplicates, gaps, and even multiple serials under the same `Double_Entry_Vouchers_ID`.
- No index or unique constraint enforces it.
- The durable accounting key remains `Double_Entry_Vouchers_ID` plus `DEV_ID_Line_No`, with `Notes_ID` / `Transaction_ID` linking.

So `DEV_Serial` should not be allowed to block the hot POS save path as if it were a mandatory, globally serialized business sequence.

## Code Path Evidence

### VB6 write path

Main function:

- `F:\Source Code\SatriahMain\Cayshny\Bas\ModAccounts.bas`
- `AddNewDev(...)`, around lines 964-1202.

Observed behavior:

- The function writes one row into `DOUBLE_ENTREY_VOUCHERS`.
- It receives `LngDevID` and `IntLineNO` from the caller.
- It accepts `Optional StrDEV_Serial As String = ""`.
- At lines 1160-1163 it does:

```vb
If StrDEV_Serial <> "" Then
    RsDev("DEV_Serial").value = StrDEV_Serial
Else
    RsDev("DEV_Serial").value = GetNewDEV_Serial(RecordDate)
End If
```

Generator:

- `GetNewDEV_Serial(RecordDate As Date)`, around lines 1353-1369.
- It runs:

```vb
Select Distinct Double_Entry_Vouchers_ID
From DOUBLE_ENTREY_VOUCHERS
Where RecordDate = <date>
```

- Then returns:

```vb
year(RecordDate) & MM & DD & "0" & LngSerialCount
```

This is a count-derived display serial. It is not a locked sequence and not gap-free under concurrency.

### VB6 POS invoice path

Main POS sale form:

- `F:\Source Code\SatriahMain\Cayshny\Frm\FrmSaleBill6.frm`
- `PG(...)`, around lines 24413 onward.

Observed behavior:

- `PG` creates a `Notes` row if needed.
- It allocates `LngDevID = new_id("DOUBLE_ENTREY_VOUCHERS", "Double_Entry_Vouchers_ID", "")`.
- It then calls `ModAccounts.AddNewDev(...)` repeatedly for accounting lines.
- It does not pass a meaningful `StrDEV_Serial`; generation is delegated to `AddNewDev`.

### VB6 display usage

Example:

- `F:\Source Code\SatriahMain\Cayshny\Frm\Voucher_search.frm`, around line 905.
- Grid column binds `DataField = "DEV_Serial"` and caption is `"NO"`.

This supports display/search usage, not accounting linkage.

## DynamicErp/Web POS Evidence

Current web POS save:

- `F:\Source Code\DynamicErp\Areas\Pos\Sql\30_POS_SaveTransaction_UnicodeText.sql`

Relevant path:

- `Double_Entry_Vouchers_ID` is allocated through `dbo.GetNextID_FromSequence`.
- `DEV_Serial` is allocated through `dbo.POS_DEVSerialAllocator` by `SerialDate`.
- Accounting rows are inserted into `dbo.DOUBLE_ENTREY_VOUCHERS` with `DEV_Serial`.

The current web implementation is stricter than VB6 because it serializes `DEV_Serial` through a per-day allocator row. VB6 did not protect it with a transaction-safe sequence.

## SQL/Data Audit

Audit script:

- `F:\Source Code\DynamicErp\Areas\Pos\Sql\MANUAL_96_POS_DEVSerial_UsageAudit.sql`

Executed on:

- Server: `Wael\Sql2019`
- Database: `Cash`
- Output: `F:\Source Code\DynamicErp\Areas\Pos\Logs\DEVSerial_UsageAudit_Cash_20260514.txt`

Key results:

| Check | Result |
|---|---:|
| Total `DOUBLE_ENTREY_VOUCHERS` rows | 7,227,939 |
| Distinct voucher headers | 1,348,387 |
| Rows where `DEV_Serial` is NULL/empty | 32,230 |
| Distinct `DEV_Serial` values | 18,046 |
| `DEV_Serial` column nullable | Yes |
| Indexes/constraints touching `DEV_Serial` | None |

Recent examples:

- `2026-05-11`: 13,334 rows, 2,291 voucher headers, 2,192 distinct `DEV_Serial` values.
- Same-day duplicate serial example: `2026051101` appears on 95 different voucher headers.
- Same voucher with multiple serials example: `Double_Entry_Vouchers_ID = 1462011` has `2026051102291` and `2026051102292`.
- Prefix mismatch examples exist, e.g. `RecordDate = 2026-05-02` with `DEV_Serial = 26202605012`, not `202605020...`.

These facts reject any assumption that `DEV_Serial` is unique, gap-free, or a reliable voucher identity.

## Evidence Table

| Question | VB6 Evidence | SQL/Data Evidence | Conclusion |
|---|---|---|---|
| Is `DEV_Serial` required to write accounting rows? | `AddNewDev` writes it, but accepts optional `StrDEV_Serial`; if blank it generates automatically. | Column is nullable; 32,230 existing rows are NULL/empty. | Not required as a strict database/business key. |
| Is it globally unique? | VB6 uses daily `COUNT(DISTINCT Double_Entry_Vouchers_ID)`, not global allocation. | Same value appears on multiple voucher headers, e.g. `2026051101` on 95 headers. | Not globally unique. |
| Is it unique per day? | VB6 has no lock/constraint around count-based generation. | Duplicate same-day `DEV_Serial` values exist. | Not unique per day. |
| Is it one value per journal voucher? | VB6 calls `GetNewDEV_Serial` per `AddNewDev` line unless a value is passed. | Some voucher headers have multiple `DEV_Serial` values. | Not reliably one per voucher in existing data. |
| Is it sequential without gaps? | Count-based generation can duplicate or skip under deletes/concurrency. | Gaps, prefix mismatches, and duplicates exist. | Not gap-free. |
| Is it used for display/search/reporting? | `Voucher_search.frm` binds it as grid field captioned `NO`. | SQL views mention it, but no constraint/index makes it a key. | Yes, legacy display/search/reporting field. |
| Can `Double_Entry_Vouchers_ID` replace identity semantics? | VB6 allocates `LngDevID` separately and uses line numbers. | Modern reports order by `Double_Entry_Vouchers_ID`, `DEV_ID_Line_No`, `NoteSerial`, dates. | `Double_Entry_Vouchers_ID` is the real durable key. |

## Risk Assessment

Before:

- Web POS treats `DEV_Serial` as a serialized per-day allocator.
- During peak save, all users on the same business date compete on the same `POS_DEVSerialAllocator.SerialDate` row.
- Previous full-save correlation showed `DEV_Serial allocation` p99 around seconds under heavy mixed load, so it can amplify transaction duration.

After possible change:

- If `DEV_Serial` is removed from the hot path or made deferred, accounting identity/linking should remain intact through `Double_Entry_Vouchers_ID`, `DEV_ID_Line_No`, `Notes_ID`, and `Transaction_ID`.
- Legacy reports that display `DEV_Serial` may show blank/zero/computed later unless adjusted.
- Any change must be phased and tested against accounting print/search screens.

## Recommended Safe Options

### Option 1 - safest immediate mitigation

Keep writing `DEV_Serial`, but do not block POS save for a strict daily sequence:

- Generate a non-blocking value from the already allocated `Double_Entry_Vouchers_ID`, e.g. `yyyyMMdd0 + Double_Entry_Vouchers_ID`.
- Preserves date prefix and display value.
- Does not preserve VB6 count-style daily numbering.
- Removes same-day allocator row contention.

### Option 2 - deferred serial

Allow POS save to insert `NULL` or `0` for `DEV_Serial`, then backfill it asynchronously per date for display.

- Most faithful to "display-only" conclusion.
- Needs report/search screens to tolerate missing value until backfill completes.
- Avoids serial work entirely inside POS save transaction.

### Option 3 - preserve exact display semantics, shorten lock

Keep the per-day allocator, but allocate outside the larger save transaction or in the shortest possible section.

- Least visible behavior change.
- Still keeps a per-day choke point.
- Better than current only if transaction scope around allocation is shortened.

## Recommended Next Step

Do not optimize `POS_DEVSerialAllocator` as a permanent bottleneck yet. First make a test-DB-only variant of `dbo.usp_POS_SaveTransaction` that replaces the `DEV_Serial` allocator with Option 1 or Option 2, then rerun the realistic 100/150 user POS save load test.

Acceptance metric:

- `DEV_Serial allocation` should disappear or become sub-millisecond.
- No missing/double accounting rows.
- `Double_Entry_Vouchers_ID`, `Notes_ID`, and `Transaction_ID` links must remain valid.
- Voucher search/report screens must either show the fallback serial or remain functionally correct.

