# Property Migration Toolkit - Accounting Safety
Date: 2026-05-20

## Non-Negotiable Accounting Rules
Blocked in all modes:
- `AccountId=NULL` in posted/migrated journal details.
- Unbalanced journal entries.
- Unknown debit/credit direction.
- Same account on debit and credit by accident.
- Voucher without understandable accounting effect.

## Allowed With Controls
| Case | Control |
|---|---|
| Unknown account | Use suspense only if config allows and ReviewQueue created |
| Missing payment method | Use fallback only for voucher shell; posting requires account validation |
| Unsafe owner payment | Exclude or Manual Review |
| Historical journals | Only migrate if linked to approved migrated voucher |

## GoLive Gates
GoLive must be blocked if:
- Open critical accounting errors exist.
- Open suspense items remain without finance sign-off.
- Any journal is unbalanced.
- Any active journal detail has null account.
- Owner payment flow is required but unresolved.
