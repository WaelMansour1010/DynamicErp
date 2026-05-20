# Property Migration Toolkit Account Matching Rules - 2026-05-20

## Rule Priority
1. Exact source/target account code match: AutoApproved if found.
2. Exact Arabic account name: HighConfidence.
3. Arabic contains match: NeedsFinanceReview.
4. Family parent suggestion: NeedsFinanceReview.
5. Unknown family/no target suggestion: Blocked.

## Signals
- Source account code.
- Arabic/English account names.
- Parent account code.
- Leaf flag.
- Debit/credit dominance.
- Related NoteTypes.
- Usage frequency.
- Target active/non-deleted ChartOfAccount rows.

## Non-Negotiable Accounting Rules
No account mapping can be used for posting if it is blocked, low-confidence, or missing finance approval.
