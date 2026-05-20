# Phase 7 - Code Changes
Date: 2026-05-20

## Modified Files
- `F:\Source Code\DynamicErp\Controllers\AccountSettings\CashReceiptVoucherController.cs`
- `F:\Source Code\DynamicErp\Controllers\AccountSettings\CashIssueVoucherController.cs`

## CashReceiptVoucherController
Added `ResolveAndValidateReceiptPayment` before calling `CashReceiptVoucher_Insert/Update`.

Behavior:
- Resolves method kind from legacy ID or Code/Name.
- Validates required CashBox/BankAccount and their accounting links.
- Normalizes `CashReceiptPaymentMethodId` to legacy posting ID before stored procedure call.
- Blocks save with JSON error if payment method cannot safely produce a journal account.

## CashIssueVoucherController
Added `ResolveAndValidateIssuePayment` before both update and insert stored procedure paths.

Behavior:
- Resolves cash/bank/cheque/account from legacy ID or Code/Name.
- Validates CashBox/BankAccount links.
- Normalizes `CashIssuePaymentMethodId` to legacy posting ID before stored procedure call.
- Applies nulling of non-applicable cash/bank/account fields based on resolved kind.

## Build Result
MSBuild completed successfully for `MyERP.csproj`.
Existing warnings remain unrelated to this change.

## Security/Accounting Impact
- No permission bypass.
- No auth bypass.
- No production DB change.
- Blocks unsafe payment methods before posting.
- Preserves existing customers that already use IDs 1/2/3/4.

## Remaining Code Debt
Views still use hardcoded IDs for show/hide behavior. Backend validation now protects accounting, but future UI should use payment type metadata instead of hardcoded IDs.
