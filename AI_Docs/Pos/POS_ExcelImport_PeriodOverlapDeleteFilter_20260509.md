# POS Excel Import Period Overlap + Delete Filter - 2026-05-09

## Scope

تعديل محدود على تنفيذ Excel Import الحالي في Kishny POS.

لم يتم تغيير:

- منطق حفظ الفاتورة.
- السيريال أو Voucher_coding.
- القيود أو السندات.
- جداول جديدة أو حقول جديدة.
- legacy `AllScripts.sql`.

## Files Reviewed

- `Areas/Pos/Doc/POS_SalesInvoiceExcelImport_Discovery_20260505.md`
- `Areas/Pos/AI_Docs/POS_ExcelImport_Defaults_ServiceMapping_20260507.md`
- `Areas/Pos/Services/PosExcelImportCommitService.cs`
- `Areas/Pos/Data/PosSqlRepository.cs`
- `Areas/Pos/Models/PosExcelImportModels.cs`
- `Areas/Pos/Models/PosSaveTransactionRequest.cs`
- `Areas/Pos/Controllers/PosTransactionController.cs`
- `Areas/Pos/Views/PosTransaction/Index.cshtml`
- `Areas/Pos/Scripts/pos-transaction.js`

## Files Changed

- `Areas/Pos/Services/PosExcelImportCommitService.cs`
- `Areas/Pos/Data/PosSqlRepository.cs`
- `Areas/Pos/Models/PosExcelImportModels.cs`
- `Areas/Pos/Models/PosSaveTransactionRequest.cs`
- `Areas/Pos/Controllers/PosTransactionController.cs`
- `Areas/Pos/Scripts/pos-transaction.js`
- `AI_Docs/Pos/POS_ExcelImport_PeriodOverlapDeleteFilter_20260509.md`

## Excel Invoice Source Detection

النظام الحالي يميز فواتير Excel من خلال:

- `POS_ImportBatchRow.Status = N'Imported'`
- `POS_ImportBatchRow.TransactionId = Transactions.Transaction_ID`

هذا هو نفس الأسلوب المستخدم سابقا في:

- قائمة الفواتير.
- فلتر `Excel فقط`.
- حذف فواتير Excel.
- Rollback الخاص بالاستيراد.

لذلك لم يتم إضافة flag جديد ولم يتم تعديل جدول `Transactions`.

## Period Overlap Rule

أثناء commit:

1. يتم استخراج فترة ملف Excel من الصفوف المقروءة:
   - أقل `TransactionDate`.
   - أكبر `TransactionDate`.
2. يتم استخدام الفرع المكتشف بالفعل في preflight/defaults.
3. قبل إنشاء أي فاتورة، يتم البحث عن أي فاتورة Excel سابقة لنفس الفرع داخل فترة الملف.
4. إذا وجدت فواتير متداخلة، يتم رفض الملف بالكامل برسالة واضحة.
5. لا يتم إنشاء batch ولا حفظ أي فاتورة من الملف.

تمت إضافة session application lock باستخدام `sp_getapplock` على مستوى الفرع:

- Resource: `POS_EXCEL_IMPORT_BRANCH_{BranchId}`

الغرض هو منع تنزيل ملفين Excel لنفس الفرع في نفس الوقت قبل فحص التداخل.

## Delete Filter Rule

حذف فواتير Excel ما زال يستخدم نفس منطق الحذف الحالي:

- حذف الفاتورة وآثارها.
- تحديث صفوف `POS_ImportBatchRow` من `Imported` إلى `Deleted`.
- عدم حذف الفواتير اليدوية لأنها غير مرتبطة بصف imported في `POS_ImportBatchRow`.

تم توسيع طلب الحذف ليأخذ:

- فرع.
- من تاريخ / إلى تاريخ.
- نوع الحركة إن كان محددا من شاشة المبيعات.

أنواع الحركة المدعومة هي نفس تصنيف شاشة المبيعات:

- `cash-in`
- `cash-out`
- `card`
- `violations`

## SQL

لا يوجد SQL script جديد.

كل الاستعلامات تمت داخل `PosSqlRepository` باستخدام SQL parameterized، واعتمدت على الجداول الموجودة بالفعل.

## Test Cases

1. إنزال شيت أول مرة لنفس الفرع والفترة:
   - يجب أن يمر إذا لا توجد فواتير Excel سابقة لنفس الفرع داخل نفس الفترة.

2. إنزال نفس الفترة لنفس الفرع مرة ثانية:
   - يجب رفض الملف بالكامل قبل حفظ أي فاتورة.
   - تظهر رسالة توضح عدد فواتير Excel السابقة والفترة المتداخلة.

3. نفس الفترة لفرع آخر:
   - لا يتم رفضها بسبب الفرع الأول، لأن الفحص scoped على `BranchId`.

4. حذف فواتير Excel بفرع:
   - اختر الفرع والفترة واضغط حذف فواتير Excel.
   - يجب حذف فواتير Excel فقط لهذا الفرع.

5. حذف فواتير Excel بتاريخ:
   - اختر من/إلى بدون فرع لمستخدم full access.
   - للمستخدم غير full access يتم تقييد الحذف بفرعه الحالي.

6. حذف فواتير Excel بفرع + تاريخ:
   - يجب تطبيق الشرطين معا.

7. حذف فواتير Excel بنوع حركة:
   - اختر نوع الفاتورة من فلتر الشاشة ثم احذف.
   - يجب حذف Excel فقط من هذا النوع.

8. التأكد من الفواتير اليدوية:
   - أي فاتورة لا يوجد لها `POS_ImportBatchRow.Status = Imported` لا تدخل في الحذف.

## Validation

- `node --check Areas/Pos/Scripts/pos-transaction.js` succeeded.
- `MSBuild MyERP.csproj /p:Configuration=Debug /p:Platform=AnyCPU /v:m` succeeded.
- No changes were made to `AllScripts.sql`.
