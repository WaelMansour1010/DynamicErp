# Cash Database Size Audit - Phase 3 Archive Strategy (2026-05-20)

## Scope
- Phase 3 = تشخيص + تصميم استراتيجية أرشفة فقط.
- لا يوجد أي `DELETE` أو `INSERT` أو `CREATE DATABASE` أو `SHRINK` أو تعديل فهارس.

## Phase 2.5 Status (Executed)
- تم تنفيذ حذف duplicate المؤكد في `dbo.Notes`:
  - المحذوف: `IX_POS_Notes_Transaction_ID`
  - المتبقي: `IX_Notes_Transaction_ID`
- تم التحقق لاحقًا باستعلام مباشر أن الفهرس المكرر غير موجود.

## أكبر الجداول المستهدفة
- `DOUBLE_ENTREY_VOUCHERS` ~ 4,672 MB data
- `Transaction_Details` ~ 3,772 MB data
- `Notes` ~ 3,726 MB data
- `Transactions` ~ 3,345 MB data

## التوزيع الزمني (سنوي)
- `Transactions` (Transaction_Date):
  - 2023: 6,207
  - 2024: 293,798
  - 2025: 685,119
  - 2026: 271,133
- `Transaction_Details` (via parent Transactions.Transaction_Date):
  - 2023: 19,285
  - 2024: 527,800
  - 2025: 1,072,885
  - 2026: 412,444
- `DOUBLE_ENTREY_VOUCHERS` (RecordDate):
  - 2003: 2
  - 2023: 53,006
  - 2024: 1,740,582
  - 2025: 3,932,294
  - 2026: 1,527,063
- `Notes` (NoteDate):
  - 2003: 1
  - 2023: 9,231
  - 2024: 322,219
  - 2025: 729,652
  - 2026: 296,612

## هل الأرشفة قبل 2023 أو قبل 2024 مفيدة؟
- قبل 2023: شبه معدوم (صفوف قليلة جدًا) => تأثير حجم غير ملموس.
- قبل 2024: موجود لكنه ما زال محدودًا نسبيًا:
  - `Transactions`: 6,207 صف (~0.49%)
  - `Transaction_Details`: 19,285 صف (~0.95%)
  - `DOUBLE_ENTREY_VOUCHERS`: 53,008 صف (~0.73%)
  - `Notes`: 9,232 صف (~0.68%)
- تقدير تقريبي للـ Data MB الممكن عزلها قبل 2024 (proportional):
  - `Transaction_Details`: ~35.54 MB
  - `DOUBLE_ENTREY_VOUCHERS`: ~34.07 MB
  - `Notes`: ~25.26 MB
  - `Transactions`: ~16.45 MB

الخلاصة العملية:
- الأرشفة حتى نهاية 2023 فقط لن تعطي خفضًا كبيرًا.
- العائد الحقيقي يبدأ غالبًا عند تصميم أرشفة لاحقة (مثلا سنوات أقدم من 2025 مستقبلًا) أو حسب سياسة احتفاظ أطول زمنيًا.

## تحليل العلاقات وسلامة النقل المستقبلي
فحوصات الترابط:
- `Transaction_Details` بدون parent في `Transactions`: 0
- `DOUBLE_ENTREY_VOUCHERS` يتيم مقابل `Transactions`: 42 صف (تحتاج معالجة خاصة عند الأرشفة)
- `DOUBLE_ENTREY_VOUCHERS` يتيم مقابل `Notes`: 0
- `Notes` يتيم مقابل `Transactions`: 0

مبدأ النقل الآمن:
1. تحديد Transactions المرشحة حسب cutoff date.
2. إدراج جميع `Transaction_Details` المرتبطة بها.
3. إدراج `Notes` المرتبطة (أو الأقدم حسب سياسة التاريخ).
4. إدراج `DOUBLE_ENTREY_VOUCHERS` المرتبطة بـ Transaction_ID و/أو Notes_ID لضمان عدم كسر القيود المحاسبية.

## خطة أرشفة آمنة مقترحة
1. إنشاء قاعدة منفصلة لاحقًا: `Cash_Archive` (في Phase تنفيذ لاحقة فقط).
2. النقل يكون batch-based مع مفاتيح مرحلية وعلاقات كاملة.
3. توفير قراءة تاريخية عبر:
   - تقارير تاريخية مباشرة على `Cash_Archive` أو
   - Views/UNION ALL مدروسة الأداء.
4. منع كسر التقارير:
   - اختبار SP/Views الحرجة قبل/بعد النقل.
5. منع كسر ترقيم السندات:
   - الترقيم يظل على قاعدة `Cash` الحالية فقط.
6. منع كسر القيود المحاسبية:
   - نقل الحزمة المحاسبية كاملة (Transaction + Details + Notes + DEV) كوحدة واحدة.

## توصية تنفيذية
- لا أنصح ببدء نقل فعلي الآن على cutoff `< 2024-01-01` بهدف تقليل الحجم فقط؛ الأثر محدود.
- الأفضل اعتماد إطار أرشفة تشغيلي دوري (سنوي/نصف سنوي) مع cutoff يعطي حجمًا فعليًا أكبر، بعد اعتماد الأعمال.

## الملفات المرفقة
- SELECT-only diagnostics:
  - `F:\Source Code\DynamicErp\Docs\CashDatabaseSizeAudit_Phase3_ArchiveStrategy_SELECT_ONLY.sql`
- Draft move (non-executable design):
  - `F:\Source Code\DynamicErp\Docs\CashDatabaseSizeAudit_Phase3_ArchiveMove_DRAFT_NOT_EXECUTE.sql`
