# Phase 9 - Pilot Clone Setup
Date: 2026-05-20
Project: F:\Source Code\DynamicErp
Scope: DynamicErp main web property module only, not POS.

## Clone Used
| Item | Value |
|---|---|
| Pilot Clone DB | $clone |
| Source DB for clone | Alromaizan_PropertyPilot_Adnan_20260520 |
| Backup file | $dir\Alromaizan_PropertyPilot_Adnan_20260520_to_PilotClone_20260520.bak |
| Restore folder | F:\DataBase\MyErp\Alromaizan_PropertyPilot_Adnan_PilotClone_20260520 |
| Safety marker | DB name contains PropertyPilot and PilotClone |
| Production DBs touched | None |
| Source DB Adnan modified | No |

## Isolation
- The clone is separate from Alromaizan and Adnan.
- All execution scripts contain or inherit sandbox/PilotClone guards.
- No Users/Passwords were migrated from Adnan.
- The web was directed to the clone through local DevStart debug database selection.

## Included Fixes
| Phase | Fix included |
|---|---|
| Phase 6 | Login/Auth logging fix and layout script section fix in codebase |
| Phase 7 | Payment method resolver/validator for receipt/issue methods |
| Phase 8 | CashIssue same debit/credit validation and safe direct expense account setup |
| Phase 8 | Rollback cleanup includes PropertyPilotAdvancePaymentStaging |

## Operational Seed Kept
- Pilot Branch: Id=1, Code=ADNAN-PILOT.
- Department: Id=44 with safe DirectExpensesAccountId=805.
- CashBox: Id=1022, AccountId=629.
- BankAccount: Id=2024, AccountId=631.
- Payment methods: compatibility 1/2 and pilot methods 5/6 for receipt and issue.
