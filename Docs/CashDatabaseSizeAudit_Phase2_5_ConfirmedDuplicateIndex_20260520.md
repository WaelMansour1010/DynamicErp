# Cash Database Size Audit - Phase 2.5 Confirmed Duplicate Index (2026-05-20)

## نتيجة المقارنة
تمت المقارنة الدقيقة بين:
- `IX_Notes_Transaction_ID`
- `IX_POS_Notes_Transaction_ID`

على جدول `dbo.Notes`، وكانت النتيجة:
- Key columns: متطابق (`Transaction_ID`)
- Included columns: متطابق (`NoteID`)
- `filter_definition`: متطابق (NULL)
- `is_unique`: متطابق (`0`)
- `fill_factor`: متطابق (`0`)
- Compression: متطابق (`NONE`)
- Filegroup/Data space: متطابق (`PRIMARY` / `ROWS_FILEGROUP`)
- Usage stats: كلاهما بدون reads في snapshot الحالي (صفر)

الخلاصة: **الفهرسان متطابقان 100% فعليًا (duplicate confirmed).**

## الفهرس المقترح حذفه
- المقترح حذف: `IX_POS_Notes_Transaction_ID`
- السبب:
  - duplicate كامل مع `IX_Notes_Transaction_ID`
  - الإبقاء على الاسم الأساسي الأقدم منطقيًا (`IX_Notes_Transaction_ID`) وتقليل الالتباس
  - يحقق نفس التغطية للاستعلامات على `Notes.Transaction_ID`

## المساحة المتوقع توفيرها
- التوفير التقريبي: **~19.89 MB** (حجم الفهرس المقترح حذفه وقت القياس).

## المخاطر
- مستوى المخاطر: **Low-Medium**
- سبب عدم تصنيفها Low بالكامل:
  - احتمال وجود اعتماد اسمي (تشغيلي/توثيقي) على اسم الفهرس في أدوات خارجية.
  - استخدام DMV لوحده لا يكفي تاريخيًا (قد يكون restart سابق).

## Rollback
- تم تجهيز سكريبت Rollback لإعادة إنشاء الفهرس بنفس التعريف.
- في حال ظهور أي أثر غير متوقع بعد الحذف، يتم تشغيل rollback فورًا.

## الملفات
- Execute review script:
  - `F:\Source Code\DynamicErp\Docs\CashDatabaseSizeAudit_Phase2_5_DropConfirmedDuplicateIndex_EXECUTE_REVIEW.sql`
- Rollback script:
  - `F:\Source Code\DynamicErp\Docs\CashDatabaseSizeAudit_Phase2_5_DropConfirmedDuplicateIndex_ROLLBACK.sql`
- Post-check script (SELECT-only):
  - `F:\Source Code\DynamicErp\Docs\CashDatabaseSizeAudit_Phase2_5_PostCheck_SELECT_ONLY.sql`

## ملاحظات تنفيذية
- لم يتم تنفيذ أي `DROP INDEX` في هذه المرحلة.
- السكريبت التنفيذي يحذف **فهرس واحد فقط** إذا كان موجودًا.
