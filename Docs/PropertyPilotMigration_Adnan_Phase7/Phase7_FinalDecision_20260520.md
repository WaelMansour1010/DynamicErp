# Phase 7 - Final Decision
Date: 2026-05-20

## Decision
Proceed toward a limited pilot on a real customer clone only after applying the Phase7 code fix and updating rollback cleanup for `PropertyPilotAdvancePaymentStaging`.

## What Is Fixed
- Payment methods are no longer blindly trusted by Id only in server-side receipt/payment posting paths.
- Custom pilot codes `CASH-PILOT` and `BANK-PILOT` can be accepted safely.
- `AccountId=NULL` in receipt/payment journals is prevented by validation of CashBox/BankAccount accounting links.
- Existing legacy behavior is preserved by normalizing to legacy IDs before stored procedure calls.

## What Still Needs Attention
1. UI still uses hardcoded IDs for field show/hide behavior. Backend is safe, but UI should eventually use type metadata.
2. Reports still classify cash/bank by legacy IDs. Current normalization preserves these reports for new vouchers.
3. CashIssueVoucher direct-expense accounting needs separate review: Phase7 synthetic cash issue was balanced/no NULL, but debit account was not the expected expense account.
4. Rollback procedure should include `PropertyPilotAdvancePaymentStaging` cleanup.

## Pilot Readiness
| Area | Status |
|---|---|
| Active contracts migration | Ready for clone pilot |
| Opening balance | Ready |
| Advance payments | Staged/reconciled, needs agreed final treatment |
| Cash receipt | Ready after Phase7 fix |
| Bank receipt | Ready after Phase7 fix |
| Termination | Ready in tested scenario |
| Cash issue/payment voucher | Not fully approved for business scenario until source-specific accounting is reviewed |
| Rollback | Safe after adding staging cleanup |

## Recommendation
Move to limited pilot clone for property contracts, opening balances, receipts, and termination testing. Keep payment-voucher scenarios under controlled test cases only, or run a focused Phase8 for CashIssueVoucher accounting source validation before allowing real payment vouchers in pilot.
