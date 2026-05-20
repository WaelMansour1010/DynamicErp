# Phase 7 - Recommended Payment Method Strategy
Date: 2026-05-20

## Selected Strategy: Option C - Hybrid
Use backward compatibility with legacy IDs 1/2/3/4 while adding Code/Name-based recognition and mandatory accounting validation.

## Why Option C
- Lowest risk for existing customers because stored procedures and reports continue receiving legacy IDs.
- Avoids adding schema columns before a wider design review.
- Allows pilot/custom methods like `CASH-PILOT` and `BANK-PILOT` to be accepted safely.
- Prevents the exact Phase 6 failure mode: journal line with `AccountId=NULL`.

## Rules Implemented
- ID 1 = Cash, ID 2 = Bank, ID 3 = Cheque, ID 4 = Account remain supported.
- Codes/names starting with `CASH` resolve to Cash.
- Codes/names starting with `BANK` or containing transfer/bank markers resolve to Bank.
- Codes/names starting with `CHEQUE`/`CHECK` resolve to Cheque.
- Unknown payment methods are blocked with a clear message.
- Cash requires active CashBox with `AccountId`.
- Receipt bank requires active BankAccount with `BankAccountReceiptId` or `AccountId`.
- Issue bank requires active BankAccount with `BankAccountPaymentId` or `AccountId`.

## User-Facing Validation Messages
- `يجب اختيار طريقة الدفع.`
- `طريقة الدفع غير موجودة أو غير مفعلة.`
- `طريقة الدفع غير معرفة محاسبياً. برجاء استخدام كود يبدأ بـ CASH أو BANK أو مراجعة إعدادات طرق الدفع.`
- `يجب اختيار الخزنة لطريقة الدفع النقدي.`
- `الخزنة المختارة غير مرتبطة بحساب محاسبي، ولا يمكن إنشاء قيد آمن.`
- `يجب اختيار الحساب البنكي لطريقة الدفع البنكية.`
- `الحساب البنكي المختار غير مرتبط بحساب محاسبي للقبض، ولا يمكن إنشاء قيد آمن.`
- `الحساب البنكي المختار غير مرتبط بحساب محاسبي للدفع، ولا يمكن إنشاء قيد آمن.`

## Production Guidance
Do not force seed IDs 1/2 in a production/customer DB until diagnostics confirm they are missing or already represent cash/bank. If IDs 1/2 are occupied by different meanings, create a controlled data fix instead.
