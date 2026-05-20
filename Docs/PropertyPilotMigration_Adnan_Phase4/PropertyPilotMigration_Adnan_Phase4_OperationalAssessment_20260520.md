# Property Pilot Migration Adnan - Phase 4 Operational Assessment - 2026-05-20

## 1. Executive Summary

Phase 4 assessed the operational master data required to make the Adnan property pilot usable inside DynamicErp Sandbox, especially for web login, receipts, payments, and contract termination.

No production migration was executed. No changes were made to `Adnan` or original `Alromaizan`.

Sandbox assessed:

`Alromaizan_PropertyPilot_Adnan_20260520`

Key conclusion:

The property data pipeline can move 283 contracts, but the operational setup is not yet complete for a real user test of receipt/payment/termination. The minimum safe setup should use current Sandbox master data where possible, seed only missing Sandbox operational records, and avoid importing old Adnan users/passwords.

## 2. Operational Master Data Inventory

| Entity | Adnan Tables | DynamicErp Tables | Sandbox Current State | Phase 3 Migrated? | Recommendation |
|---|---|---|---|---|---|
| Employees | `TblEmployee`, `TblEmpData` | `Employee` | 10 employees | No | Do not migrate old employees for pilot. Use existing Sandbox employee/admin. |
| Users | `TblUsers` | `ERPUser`, `Users`, `ERPRole`, `UserPrivilege`, `RolePrivilege` | 9 ERP users, 1 active | No | Do not migrate users/passwords. Use existing active `ErpAdmin` or create Sandbox-only pilot admin after approval. |
| Branches | `TblBranchesData`, `TblUsersBranches` | `Branch`, `Department`, `UserDepartment` | `Branch` has 0 rows; `Department` has 1 row | No | Use one Pilot Branch in Sandbox mapped to active Department 44. |
| Cash boxes | `TblBoxesData`, `TblUsersBoxes` | `CashBox`, `UserCashBox` | 1 active cash box | No | Use existing Sandbox cash box for first web validation. Do not import all Adnan boxes. |
| Banks | `BanksData` | `Bank`, `BankAccount` | 1 bank and 1 bank account | No | Use existing Sandbox bank for first validation; map old banks later if historical receipt audit is needed. |
| Payment methods | `TblPaymentType`, `tblPaymentClass`, `TblPaymentUser` | `CashReceiptPaymentMethod`, `CashIssuePaymentMethod`, `PaymentMethod` | POS/general `PaymentMethod` exists; receipt/issue method tables are empty | No | Seed Cash/Bank pilot payment methods in Sandbox. |
| User branch permission | `TblUsersBranches` | mostly `UserDepartment`; `BranchId` on vouchers | 1 UserDepartment row | No | Keep Department permission; add Pilot Branch for voucher FK. |
| User cashbox permission | `TblUsersBoxes` | `UserCashBox`, `ERPUser.CustodyBoxId`, `ERPUser.IsCashier` | 1 UserCashBox row | No | Ensure active admin has cashbox privilege and CustodyBoxId. |
| Cash/bank accounting links | `TblBoxesData.Account_Code`, `BanksData.Account_Code` | `CashBox.AccountId`, `BankAccount.AccountId` | Existing links present | No | Use existing links for Sandbox test; do not map all old cash/bank accounts yet. |

## 3. Current Counts

| Metric | Value |
|---|---:|
| Adnan active users | 5 |
| Adnan employees | 16 |
| Adnan branches | 3 |
| Adnan boxes | 6 |
| Adnan banks | 12 |
| Adnan payment types | 2 |
| Sandbox active ERP users | 1 |
| Sandbox employees | 10 |
| Sandbox Branch rows | 0 |
| Sandbox active departments | 1 |
| Sandbox active cash boxes | 1 |
| Sandbox banks / bank accounts | 1 / 1 |
| Sandbox CashReceiptPaymentMethod rows | 0 |
| Sandbox CashIssuePaymentMethod rows | 0 |
| Sandbox UserCashBox rows | 1 |
| Sandbox UserDepartment rows | 1 |

## 4. Branch Strategy

Adnan strict-active contracts are distributed across two old branches:

| Old Branch | Name | Contract Count |
|---:|---|---:|
| 1 | الفرع الرئيسى- شركه عدنان وعادل الحميدان | 211 |
| 2 | فرع جدة - شركه عدنان وعادل الحميدان | 72 |

Options:

| Option | Decision |
|---|---|
| Migrate Adnan branches as new DynamicErp branches | Not recommended for first validation; branch/company/security setup may expand scope. |
| Map old branches to existing Alromaizan branch | Not possible directly because Sandbox `Branch` has 0 rows. |
| Use one Pilot branch | Recommended for first Sandbox web validation. |

Recommended Phase 4 approach:

Create one Sandbox-only branch: `ADNAN-PILOT`, linked to the existing company and used only for voucher FK requirements. Keep department/accounting logic on Department 44.

Impact:

- Contracts: no immediate impact unless BranchId is required by posting/procedures.
- Receipts/payments: BranchId FK can be satisfied.
- Journals: Department remains the safer accounting dimension.
- Reports: branch-level historical Adnan reporting is not validated in first pilot.
- Permissions: user should retain `UserDepartment` and cashbox access.

## 5. CashBox / Bank Strategy

Adnan receipts for strict-active contracts mostly use bank-like receipt modes:

| NoteCashingType | Meaning from VB6 | Observation |
|---:|---|---|
| 0 | Cash box | 3 receipts, BoxID 1 |
| 2 | Bank/electronic style | Majority of receipt value |
| 4 | Special/electronic/unclassified | 90 receipts |

Sandbox has:

- 1 active CashBox with account link.
- 1 Bank and 1 BankAccount with account link.
- 0 CashReceiptPaymentMethod rows.
- 0 CashIssuePaymentMethod rows.

Recommended minimum setup:

1. Use existing Sandbox CashBox for cash receipt test.
2. Use existing Sandbox BankAccount for bank receipt test if required.
3. Seed two receipt methods: `CASH-PILOT`, `BANK-PILOT`.
4. Seed two issue methods: `CASH-PILOT`, `BANK-PILOT`.
5. Do not import all 6 Adnan boxes or 12 Adnan banks for first validation.

## 6. Users / Employees Strategy

Do not migrate `Adnan.TblUsers` into DynamicErp for Pilot.

Reasons:

- Passwords must not be copied.
- DynamicErp `ERPUser` requires `EmployeeId` and `RoleId`.
- Sandbox already has an active `ErpAdmin` with employee and role.
- The goal is operational validation, not user migration.

Minimum safe setup:

1. Use existing active Sandbox `ErpAdmin` if password is known/available.
2. Ensure `ErpAdmin.IsCashier = 1` and `CustodyBoxId` points to the active cash box.
3. Ensure `UserCashBox` exists for active cash box.
4. Ensure `UserDepartment` exists for Department 44.
5. If login is unavailable, create a Sandbox-only pilot admin through the application/user management flow or an approved hash method. Do not copy Adnan passwords.

## 7. Advance Payments Strategy

Phase 3 identified:

| Item | Value |
|---|---:|
| Future gross installments | 19,234,398.7085 |
| Future paid/advance | 55,592.89 |
| Future net remain expected | 19,178,805.8185 |

Recommended treatment:

Stage the future paid amount as advance/prepaid allocation against future installments. Do not reduce the gross contractual schedule. Do not post journals until accounting treatment is approved.

Rationale:

- Contract batches should preserve the contractual schedule.
- Advance payments should be visible separately as paid/credit allocation.
- Reducing future batch totals would distort contract value.
- A renter credit opening balance can work, but needs careful receipt allocation behavior.

Phase 4 script `05_AdvancePaymentsHandling_DRAFT_SANDBOX_ONLY_20260520.sql` creates/stages advance amounts only; it does not post accounting.

## 8. Property Type / Unit Type Strategy

DynamicErp target lookups:

- `PropertyType`: 2 rows, codes `1`, `2`.
- `PropertyUnitType`: 3 rows only: مطبخ، غرفة، مساحة تخزين.

Adnan active unit types include: شقة، ملحق، غرفة صغيرة، محل، فيلا، برج اتصالات، مستودع، حضانة، مجمع.

Recommendation:

- Map `aqartypeid=1` to residential, `aqartypeid=2` to commercial.
- For null/unknown property type, leave NULL or decide default by property evidence.
- Seed or manually map Adnan unit types before next Dry Run; existing target unit types are insufficient.

## 9. Can Receipts/Payments Be Tested After Phase 4?

After executing the Sandbox-only operational seed script and resolving authenticated login, receipt and payment screens should be testable at a minimal level.

Still required before meaningful business validation:

1. Execute operational seed on Sandbox only.
2. Re-run Dry Run because Phase 3 rollback removed migrated contracts.
3. Authenticate as active user.
4. Use `CASH-PILOT` payment method and existing CashBox.
5. Keep accounting results sandbox-only.

## 10. Key Decision

Phase 4 should not import old operational users, all old branches, all old cash boxes, or all banks.

Use one safe Sandbox operational setup first. Expand mapping only after property screens, receipt creation, and termination behavior are proven.
