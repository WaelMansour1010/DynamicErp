# KYC/Card Activation Critical Fix - 2026-05-11

## Scope

This fix blocks duplicate KYC/card activation and duplicate stock issue for physical card tokens.

## Rules Enforced

- One active token can be assigned to one KYC customer only.
- One customer can have one 18-character token card and one non-18 token card only.
- National ID is the primary identity key. Mobile is used only when National ID is missing.
- A card token must exist in stock receipt movement (`Transactions.Transaction_Type = 20`, `Transaction_Details.ItemSerial`).
- A card token must not already exist in stock issue movement (`Transactions.Transaction_Type = 19`, `Transaction_Details.ItemSerial`).
- Final protection is inside SQL transactions using `SERIALIZABLE`, `UPDLOCK`, `HOLDLOCK`, and `sp_getapplock`.

## Changed Areas

- KYC save now uses a transaction-safe backend path before inserting/updating `TblCusCsh`.
- POS card invoice save now revalidates the token inside `dbo.usp_POS_SaveTransaction` before creating the issue voucher.
- UI token entry calls a backend availability endpoint for friendly helper feedback only.
- Diagnostic and optional database-protection scripts were added.

## SQL Scripts

- `Areas/Pos/Sql/60_POS_KYC_CardActivation_Diagnostics.sql`
  - Lists duplicate tokens.
  - Lists same customer with multiple 18-length cards.
  - Lists same customer with multiple non-18 cards.
  - Lists activated tokens still appearing available in stock.
  - Lists tokens issued more than once.
  - Lists KYC records without matching issue movement.

- `Areas/Pos/Sql/61_POS_KYC_CardActivation_Protection.sql`
  - Creates `dbo.usp_POS_ValidateKeshniCardActivation`.
  - Adds computed columns for normalized token/card type.
  - Creates safe unique indexes only if diagnostics are clean.
  - Stops with an explicit error if dirty duplicates exist.

## Required Test Cases

| Case | Scenario | Expected Result |
|---|---|---|
| A | Same customer + same 18-length token type again | Blocked with Arabic same-type message |
| B | Same customer + different 18-length token but same type | Blocked with Arabic same-type message |
| C | Same customer + non-18 token while already has 18 token | Allowed if token is available in stock |
| D | Same customer + another non-18 token while already has non-18 | Blocked with Arabic same-type message |
| E | Same token with another customer | Blocked with token already activated message |
| F | Same token with same customer again as new activation | Blocked with token already activated message |
| G | Token not found in stock receipt movement | Blocked with stock unavailable message |
| H | Token already issued in stock movement | Blocked with stock unavailable message |
| I | Two users activate/save same token concurrently | Only one succeeds; the second is blocked by SQL lock/validation |
| J | Existing search/load/print flows | Continue working; KYC print and declaration print are unchanged |

## Arabic Messages

- `هذا الكارت/التوكن تم تفعيله من قبل ولا يمكن استخدامه مرة أخرى.`
- `هذا العميل لديه كارت مفعل بالفعل من نفس النوع. مسموح فقط بكارت واحد من كل نوع.`
- `هذا الكارت غير متاح بالمخزون أو تم صرفه/استخدامه من قبل.`
