# POS Sales Validation UX - 2026-05-08

## Scope

تم تحسين تجربة التحقق في شاشة مبيعات POS:

- `/Pos/PosTransaction/Index`

التعديل واجهة وتحقق فقط. لم يتم تعديل إجراءات الحفظ أو القيود أو الترحيل أو SQL.

## Files Changed

- `Areas/Pos/Scripts/pos-transaction.js`
- `Areas/Pos/Content/pos-transaction.css`
- `Areas/Pos/Controllers/PosTransactionController.cs`
- `AI_Docs/Pos/POS_SalesValidationUX_20260508.md`

## Fields Covered

تم إضافة ربط field-key إلى عناصر الشاشة للحقول الآتية:

- `TransactionType` -> نوع العملية
- `CashCustomerPhone` / `CustomerPhone` / `PhoneNo2` -> رقم التليفون
- `CashCustomerName` / `Name` -> اسم العميل
- `IPN` -> ID
- `ManualNO` -> IPN المهم للكاش إن والكروت
- `WalletNumber` / `Tet_NumPoket` -> رقم المحفظة
- `ViolationsValue` -> قيمة المخالفات
- `ViolationPayType` -> طريقة دفع المخالفات
- `RechargeValue` / `RechargeAmount` -> مبلغ الشحن
- `ServiceType` / `ItemIDService` -> نوع الخدمة
- `ItemIDService2` / `WalletBankId` -> المحفظة/البنك الفرعي
- `PaymentType` -> طريقة الدفع
- `PaymentAmount` / `PayedValue` -> المدفوع
- `BoxID` -> الخزنة
- `BranchId` -> الفرع
- `StoreID` -> المخزن
- `Emp_ID` -> المندوب
- `VisaNumber` / `CardNo` / `TblCusCshId` -> كارت كيشني/KYC
- `Items` -> جدول الأصناف

## Service-Type Rules

التحقق في العميل يحترم وضع الخدمة الحالي والحقول الظاهرة:

- الحقول العامة: نوع العملية، بيانات العميل، الفرع، المخزن، الخزنة، طريقة الدفع، والأصناف.
- كاش إن وكارت كيشني: يتم اعتبار `ManualNO` مطلوبا فقط في الخدمات التي تعتمد عليه.
- كاش أوت: يتم طلب رقم المحفظة عند تفعيل خدمة المحفظة.
- كارت كيشني: يتم طلب رقم الكارت والتأكد من وجود عميل KYC مفعل قبل الحفظ.
- المخالفات: يتم طلب قيمة المخالفات وطريقة الدفع ورقم المحفظة الخاص بالمخالفات.
- عند تغيير نوع الخدمة يتم مسح علامات التحقق من الحقول التي أصبحت مخفية أو غير مطلوبة.

## Client Validation UX

تمت إضافة مساعدات عامة في `pos-transaction.js`:

- `markFieldInvalid(fieldSelector, message)`
- `clearFieldInvalid(fieldSelector)`
- `focusFirstInvalidField()`
- `clearAllValidationHighlights()`

عند فشل التحقق:

- تظهر رسالة عربية عامة: `برجاء استكمال الحقول المطلوبة`.
- يتم تمييز الحقل نفسه بإطار أحمر خفيف.
- تظهر رسالة صغيرة أسفل الحقل.
- يتم التمرير والتركيز على أول حقل غير صالح.
- تزال علامة الخطأ تلقائيا عند إدخال قيمة صحيحة أو تغيير الاختيار.

## Server Validation Mapping

تم توسيع رد الخادم في `PosTransactionController` ليعيد:

```json
{
  "success": false,
  "validationErrorsDetailed": [
    { "field": "WalletNumber", "message": "رقم المحفظة مطلوب" }
  ]
}
```

الواجهة ما زالت تدعم الشكل القديم `validationErrors` كقاموس، وتحتوي على mapping مؤقت لبعض الرسائل العربية المعروفة إذا لم يصل field-key من الخادم.

## CSS

تمت إضافة:

- `.pos-field-invalid`
- `.pos-validation-message`
- `.pos-field-pulse`
- `.pos-field-invalid-wrap`

التصميم خفيف ومناسب للشاشة العربية RTL، بدون تغيير Layout شاشة البيع.

## Validation Checklist

- ترك رقم المحفظة فارغا في حالة تتطلبه يميز الحقل ويركز عليه.
- الكتابة داخل الحقل تزيل التمييز الخاص به فقط.
- تغيير نوع الخدمة يمسح أخطاء الحقول المخفية.
- أخطاء الخادم ذات field-key يتم عرضها على الحقول نفسها.
- الحفظ الناجح يستمر في نفس flow القديم بدون أي تغيير في المحاسبة أو الترحيل.

## Safety Notes

- لا توجد تغييرات SQL.
- لا توجد تغييرات على `usp_POS_SaveTransaction`.
- لا توجد تغييرات على القيود أو سندات الصرف أو ترحيل الفاتورة.
- لا توجد تغييرات في MainErp أو legacy `AllScripts.sql`.
