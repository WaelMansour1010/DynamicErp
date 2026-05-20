# Phase 8 - Code Changes
Date: 2026-05-20

## Modified File
- `F:\Source Code\DynamicErp\Controllers\AccountSettings\CashIssueVoucherController.cs`

## Changes
Added server-side validation before `CashIssueVoucher_Insert` and `CashIssueVoucher_Update`:
- Resolve payment method by legacy ID or Code/Name, continuing Phase7 hybrid strategy.
- Resolve expected debit account using the same source-account rules used by the stored procedures.
- Resolve expected credit account from CashBox/BankAccount/ChartOfAccount.
- Block save if debit account is missing.
- Block save if credit account is missing.
- Block save if debit account equals credit account.
- For Issue Analysis, block missing row account and block analysis account equal to payment account.

## User Message
`لا يمكن حفظ سند الدفع لأن حساب المصروف أو المصدر غير محدد أو يساوي حساب الخزنة/البنك.`

## Compatibility
The change is backward-compatible: valid existing vouchers with distinct debit/credit accounts continue to save. Unsafe vouchers are now blocked before creating misleading journals.

## Build
MSBuild succeeded. Existing warnings are unrelated.
