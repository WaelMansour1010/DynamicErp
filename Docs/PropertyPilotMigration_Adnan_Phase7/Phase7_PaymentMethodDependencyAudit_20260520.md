# Phase 7 - Payment Method Dependency Audit
Date: 2026-05-20
Scope: DynamicErp main web project only. POS ignored.
Sandbox: Alromaizan_PropertyPilot_Adnan_20260520
Source: Adnan read-only

## Executive Finding
Phase 6 exposed a real accounting risk: `CashReceiptPaymentMethodId=5` (CASH-PILOT) was not recognized by legacy posting logic that expects IDs 1/2/3/4, so the receipt journal was created with `AccountId=NULL`. Phase 7 confirms the dependency exists in receipt/payment controllers, receipt/payment views, and cash issue/receipt report expressions.

## Dependency Audit
| File | Method/Area | Line/Context | Current Logic | Risk | Proposed Fix |
|---|---|---:|---|---|---|
| Controllers/AccountSettings/CashReceiptVoucherController.cs | AddEdit POST | 1045-1064 before fix | Branching by `CashReceiptPaymentMethodId == 1/2/3/4` | Custom methods like CASH-PILOT/BANK-PILOT are not normalized; SP may create NULL account line | Implemented hybrid resolver by legacy ID or Code/Name, validates cashbox/bank account, maps to legacy posting ID |
| Controllers/AccountSettings/CashIssueVoucherController.cs | AddEdit POST update path | 750 | Update path now validates with resolver before SP | Without this, edited issue voucher could post with wrong/null account | Implemented validation before update SP |
| Controllers/AccountSettings/CashIssueVoucherController.cs | AddEdit POST insert path | 873-891 | Previously required CashBox only when ID=1 and BankAccount only when ID=2/3 | CASH-PILOT Id=5 bypassed required-account validation | Implemented resolver + account validation + legacy ID normalization |
| Views/CashReceiptVoucher/AddEdit.cshtml | JS change/validation | 1171-1204, 1796-1819 | UI shows/hides fields by IDs 1/2/3/4/12 | Custom methods may show wrong required fields client-side | Backend fix protects accounting; future UI improvement should use type metadata/API |
| Views/CashIssueVoucher/AddEdit.cshtml | JS change/validation | 1451-1493, 2579-2604 | UI shows/hides fields by IDs 1/2/3/4 | Custom methods may show wrong required fields client-side | Backend fix protects accounting; future UI improvement should use type metadata/API |
| Reporting/Reports/CashIssueAndReceipt_Report .cs | Report calculated fields | 930, 937, 944 | Report totals classify cash/bank using IDs 1/2 | Reports may misclassify custom methods if vouchers store custom IDs | Current fix normalizes saved voucher to 1/2, preserving report behavior |
| DynamicWeb duplicate files | Shadow/generated copy | multiple | Same patterns exist in duplicate DynamicWeb tree | If that copy is deployed independently it has same risk | Not modified in this phase; main project runtime files were fixed |
| Stored procedures CashReceiptVoucher_Insert/Update | SQL Server procedures | inspected from Sandbox | Receive numeric PaymentMethodId and appear compatible with legacy IDs | SP cannot infer custom method type because table has no type/account columns | Hybrid code normalizes to legacy ID before SP, avoiding SP rewrite now |
| Stored procedures CashIssueVoucher_Insert/Update | SQL Server procedures | inspected from Sandbox | Receive numeric PaymentMethodId and rely on existing posting/account rules | Custom IDs are unsafe unless normalized or seeded | Hybrid code normalizes to legacy ID before SP |

## Reference Model Inspection
`CashReceiptPaymentMethod` and `CashIssuePaymentMethod` contain only: `Id`, `Code`, `ArName`, `EnName`, `IsActive`, `IsDeleted`.

There is no `IsCash`, `IsBank`, `PaymentType`, `AccountId`, `CashBoxId`, or `BankAccountId` on payment method tables.

Posting accounts come from:
- Cash: `CashBox.AccountId`
- Receipt bank: `BankAccount.BankAccountReceiptId` or fallback `BankAccount.AccountId`
- Issue bank: `BankAccount.BankAccountPaymentId` or fallback `BankAccount.AccountId`

## Phase 7 Sandbox Reference Data
| Table | Id | Code | Meaning |
|---|---:|---|---|
| CashReceiptPaymentMethod | 1 | CASH-COMPAT | legacy-compatible cash |
| CashReceiptPaymentMethod | 2 | BANK-COMPAT | legacy-compatible bank |
| CashReceiptPaymentMethod | 5 | CASH-PILOT | pilot cash alias |
| CashReceiptPaymentMethod | 6 | BANK-PILOT | pilot bank alias |
| CashIssuePaymentMethod | 1 | CASH-COMPAT | legacy-compatible cash |
| CashIssuePaymentMethod | 2 | BANK-COMPAT | legacy-compatible bank |
| CashIssuePaymentMethod | 5 | CASH-PILOT | pilot cash alias |
| CashIssuePaymentMethod | 6 | BANK-PILOT | pilot bank alias |
