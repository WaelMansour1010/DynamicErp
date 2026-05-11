# KishnyPOS_Hotfix_CardHide_ID_IPN_20260511

## ماذا تم
- إخفاء حقلي `ID` و `IPN` (Label + Input) عند اختيار خدمة `كارت كيشني` فقط.
- إخفاء/تنظيف رسائل الـ Validation الخاصة بـ `ID/IPN` فور التحويل إلى `card` بدون Refresh.
- تعطيل Required Validation لـ `ID` و `IPN` في حالة `card` فقط (واجهة + Controller).
- تعطيل Duplicate Check الخاص بـ `IPN` لحالة `card` فقط (Controller + SQL procedure).
- السماح بحفظ فاتورة `card` بدون `ID/IPN` مع بقاء مسار حفظ الكارت والقيود والطباعة كما هو.

## ماذا لم يتم
- لم يتم تغيير أي Mapping داخل قاعدة البيانات.
- لم يتم عكس أو تغيير:
  - `Transactions.IPN`
  - `Transactions.ManualNo`
- لم يتم المساس بمنطق القيود، السيريالات، أو Voucher Coding.
- لم يتم تعديل Session/Permissions/Reports/KYC/Printing.
- لم يتم تضمين أي ملفات خارج نطاق الـ Hotfix.

## الملفات المضمنة في هذه الـ Release فقط
### Deploy
- `Deploy/Areas/Pos/Views/PosTransaction/Index.cshtml`
- `Deploy/Areas/Pos/Scripts/pos-transaction.js`
- `Deploy/bin/MyERP.dll`

### SQL (ضروري)
- `Sql/30_POS_SaveTransaction_UnicodeText.sql`

## خطوات الاختبار
1. اختيار نوع العملية `كارت كيشني`.
2. التأكد أن `ID` و `IPN` غير ظاهرين بالكامل.
3. حفظ فاتورة كارت بدون إدخال `ID/IPN`.
4. التأكد عدم ظهور أي Error تخص `ID/IPN` للكارت.
5. التبديل إلى `كاش إن` ثم التحقق أن `ID/IPN` يظهران ويعود Required Validation طبيعي.
6. محاولة حفظ `كاش إن` بنفس `IPN` مكرر للتأكد أن Duplicate Validation ما زالت تعمل.
7. مراجعة استمرار الحفظ والطباعة والقيد في عملية الكارت.

## نتيجة الاختبارات
- تم تنفيذ Build بنجاح عبر MSBuild (`Configuration=Release`) وإنتاج `bin/MyERP.dll`.
- تحقق كودي من مسارات الإخفاء/الـ Validation/الـ Duplicate محصور على `card` فقط.
- الاختبارات التشغيلية النهائية (UI + DB + Printing + Journal) يجب تنفيذها على بيئة QA/Production-like أثناء الرفع وفق خطوات الاختبار أعلاه.
