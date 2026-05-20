# Phase 4 Final Decision - 2026-05-20

## Direct Answers

1. Employees: No, we do not need to migrate old Adnan employees for the next Sandbox validation. Use existing Sandbox employee/admin.
2. Users: No, we do not need to migrate Adnan users. Do not copy real passwords. Use existing Sandbox `ErpAdmin` or create a Sandbox-only pilot admin after approval.
3. Branches: Use Mapping/minimal setup, not full migration. Create one Sandbox-only Pilot Branch because `Branch` currently has 0 rows.
4. Cash boxes and banks: Partially ready. Sandbox has one active cash box and one bank/bank account with account links, but receipt/issue payment method tables are empty and need seed rows.
5. Receipt/payment testing: Yes, after Phase4 operational seed plus authenticated login and re-running the Phase3 fixed migration.
6. Minimum safe setup: existing active ERPUser + existing Employee + Department 44 + existing CashBox + existing BankAccount + one Pilot Branch + pilot cash/bank payment methods + UserCashBox/UserDepartment permission.
7. Can we re-run Dry Run after fixes? Yes, after applying Phase3 fixed scripts, Phase4 operational seed, lookup mapping, and auth access.
8. Scope remains 283 contracts only. The 10 bad-link contracts stay Archive Only / excluded.

## Go / No-Go

Current status:

`Go for Sandbox preparation scripts review`

Not yet Go for real pilot or UAT.

## Required Before Phase 5 / Second Dry Run

1. Approve `04_SandboxOperationalSeed_DRAFT_SANDBOX_ONLY_20260520.sql`.
2. Approve advance payment treatment: stage as prepaid/advance allocation, no gross schedule reduction.
3. Approve unit type lookup seeding/mapping.
4. Provide valid Sandbox admin login or approve Sandbox-only pilot admin creation through safe app/user flow.
5. Re-run Phase3 fixed migration after rollback.
6. Execute reconciliation and web checklist.

## Highest Risk Remaining

Authentication and operational permissions are now the blocker for real screen testing. Financially, the highest remaining issue is the `55,592.89` future advance handling.
