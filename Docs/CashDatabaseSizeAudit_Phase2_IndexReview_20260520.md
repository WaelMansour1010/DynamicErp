# Cash Database Size Audit - Phase 2 Index Review (2026-05-20)

## نطاق Phase 2
- المراجعة تمت على قاعدة `Cash` فقط (Instance: `WAEL\SQL2019`).
- النمط: **Review + Scripts فقط**.
- لم يتم تنفيذ أي `DROP INDEX` أو `REBUILD/REORGANIZE` أو `SHRINK` أو تعديل Recovery Model.

## ملاحظة مهمة عن جدول Payments
- الجدول `dbo.Payments` غير موجود بالاسم المباشر داخل القاعدة وقت المراجعة.
- تم رصد جداول دفع بديلة (`TblSalesPayment`, `TblTransactionPayments`, `TblBillBuyPayment`... إلخ) وهي صغيرة نسبيًا مقارنة بالجداول الأربعة الكبرى.

## منهجية التحليل (ليس DMV فقط)
1. تحليل `usage stats` من DMVs (مع التحذير من reset بعد restart).
2. تحليل تعريفات الفهارس (keys/includes/filter).
3. تحليل التداخل والتكرار (Duplicate/Overlapping signatures).
4. فحص الاعتماد في كائنات SQL (SP/FN/Views) عبر dependencies و `sys.sql_modules`.
5. مراجعة مراجع من المشروع نفسه (سورس) للجداول الحرجة.

## الجداول والحمولة الأساسية
- `DOUBLE_ENTREY_VOUCHERS`: ~`13,626.55 MB` فهارس مستخدمة.
- `Transactions`: ~`6,987.04 MB` فهارس مستخدمة.
- `Transaction_Details`: ~`5,833.31 MB` فهارس مستخدمة.
- `Notes`: ~`4,285.99 MB` فهارس مستخدمة.

## النتائج الأساسية

### 1) Duplicate Indexes
- على `dbo.Notes`:
  - `IX_Notes_Transaction_ID`
  - `IX_POS_Notes_Transaction_ID`
- نفس key/include (تكرار فعلي).
- مساحة المؤشر الأصغر تقريبًا: `~19.89 MB`.
- **Risk**: Low-Medium.

### 2) Overlapping Indexes
#### `DOUBLE_ENTREY_VOUCHERS`
- زوج `Transaction_ID`:
  - `IX_DOUBLE_ENTREY_VOUCHERS_Transaction_ID` (~170.49 MB)
  - `IX_POS_DEV_Transaction_ID` (~164.25 MB)
- زوج `Notes_ID`:
  - `IX_DOUBLE_ENTREY_VOUCHERS_Notes_ID` (~166.38 MB)
  - `IX_POS_DEV_Notes_ID` (~163.63 MB)
- مجموعة `RecordDate` المتداخلة:
  - `IX_DEV_RecordDate` (~612.51 MB)
  - `<IndexRecordDate, sysname,>` (~207.46 MB)
  - `IX_POS_DEV_RecordDate` (~163.70 MB)

#### `Transaction_Details`
- تداخل واسع حول `Transaction_ID` و `Item_ID` خاصة مع:
  - `<IndxTT, sysname,>` (~118.74 MB)
  - `IX_Transaction_Details_TransactionID_ID`
  - `IX_TransactionDetails_CloseReport`
  - `IDX_Transaction_Details_Transaction_Price_Item`
- التداخل موجود لكن الخطورة أعلى بسبب كثافة الاعتماد التقاريري/التشغيلي.

#### `Transactions`
- يوجد كثافة فهارس كبيرة ومتداخلة على أبعاد `StoreID/BranchId/Transaction_Type/Transaction_Date`.
- لكن نسبة الاعتماد الوظيفي العالية (بحث فواتير/تقارير تشغيل/إغلاق يومي) تجعل الحذف المباشر عالي المخاطر.

### 3) Unused / Low-usage
- أغلب الفهارس في snapshot الحالي تظهر usage منخفضًا أو صفر.
- **لا يعتمد القرار على DMV فقط** بسبب احتمال restart؛ تم رفض أي حذف واسع بناءً على DMV منفرد.

### 4) مراجعة الاستعلامات والاعتماد
- تم رصد اعتماد كبير من Stored Procedures/Functions/Reports على الجداول الأربعة الأساسية.
- أمثلة SP تقاريرية/تشغيلية مرتبطة: `usp_POS_SalesInvoices_Search`, `usp_POS_Journal_Search`, `usp_POS_Accounting_Report`, `RPT_CloseReportTotal*` وغيرها.
- مراجع المشروع (VB/.NET) تؤكد اعتماد مباشر على `Transactions`, `Transaction_Details`, `DOUBLE_ENTREY_VOUCHERS`, `Notes` في التدفق التشغيلي.

## الفهارس المقترح حذفها (Draft فقط)
1. `dbo.Notes.IX_POS_Notes_Transaction_ID`
- السبب: duplicate exact مع `IX_Notes_Transaction_ID`.
- التوفير التقريبي: `~19.89 MB`.
- المخاطر: **Low-Medium**.

2. `dbo.DOUBLE_ENTREY_VOUCHERS.IX_POS_DEV_Transaction_ID`
- السبب: overlap كبير مع `IX_DOUBLE_ENTREY_VOUCHERS_Transaction_ID`.
- التوفير التقريبي: `~164.25 MB`.
- المخاطر: **Medium**.

3. `dbo.DOUBLE_ENTREY_VOUCHERS.IX_POS_DEV_Notes_ID`
- السبب: overlap كبير مع `IX_DOUBLE_ENTREY_VOUCHERS_Notes_ID`.
- التوفير التقريبي: `~163.63 MB`.
- المخاطر: **Medium**.

4. `dbo.DOUBLE_ENTREY_VOUCHERS.<IndexRecordDate, sysname,>`
- السبب: تغطية جزئية ضمن فهارس `RecordDate` أكبر.
- التوفير التقريبي: `~207.46 MB`.
- المخاطر: **Medium-High**.

5. `dbo.Transaction_Details.<IndxTT, sysname,>`
- السبب: overlap واضح مع فهارس أوسع.
- التوفير التقريبي: `~118.74 MB`.
- المخاطر: **High** (لا يُنفذ إلا بعد replay/benchmark).

## الفهارس التي لا يجب لمسها الآن
- كل PK/Clustered الأساسية:
  - `PK_DOUBLE_ENTREY_VOUCHERS`
  - `PK_Transactions`
  - `IX_Transaction_Details` (Clustered)
  - `PK_Notes`
- فهارس البحث الحرجة في `Transactions` (ManualNO/NoteSerial/Visa وغيرها) قبل قياس أثر فعلي على شاشات البحث والتقارير.

## اقتراحات Rebuild/Reorganize (Draft فقط)
- `REBUILD` مقترح:
  - `Transaction_Details.IX_Transaction_Details` (frag ~54.09%, page_count كبير جدًا)
- `REORGANIZE` مقترح (عينات أعلى):
  - `Transactions.IX_POS_Transactions_Card_VisaNumber`
  - `Transactions.IX_POS_Transactions_Search_VisaNumber`
  - `Transactions.IX_POS_KycAvailableCards_Transactions`
  - `Transaction_Details.IX_POS_TransactionDetails_ItemSerial_Transaction`
  - `Transactions.IX_POS_Transactions_Report_ServiceSearch`
  - `Notes.IX_POS_Notes_Transaction_ID`
  - `Transaction_Details.IX_POS_TransactionDetails_StoreSerials_Report`
  - `Notes.IX_Notes_Header_NoteSerial`
  - `Transactions.IX_Transactions__UserID`

## تقدير المساحة الممكن توفيرها
- سيناريو محافظ (Low+Medium فقط):
  - `~555.23 MB` تقريبًا.
- سيناريو موسّع (بإضافة High risk `<IndxTT>`):
  - `~673.97 MB` تقريبًا.

## تقييم طبيعي/غير طبيعي في Phase 2
- الحجم الكبير ما زال مرتبطًا أساسًا بحمولة بيانات فعلية + تصميم فهارس كثيف.
- تقليل الحجم عبر حذف فهارس فقط سيكون **محدودًا نسبيًا** مقارنة بإجمالي القاعدة، لكنه يحسن footprint وقد يقلل كلفة الصيانة.

## المخرجات المطلوبة (تم تجهيزها)
- SELECT-only:
  - `F:\Source Code\DynamicErp\Docs\CashDatabaseSizeAudit_Phase2_IndexReview_SELECT_ONLY.sql`
- DROP INDEX Draft (commented بالكامل):
  - `F:\Source Code\DynamicErp\Docs\CashDatabaseSizeAudit_Phase2_DropIndex_DRAFT_NOT_EXECUTE.sql`
- Index Maintenance Draft:
  - `F:\Source Code\DynamicErp\Docs\CashDatabaseSizeAudit_Phase2_IndexMaintenance_DRAFT_NOT_EXECUTE.sql`

## توصية قرار قبل التنفيذ (Phase 3-ready gate)
- تنفيذ A/B على بيئة اختبار أو نافذة صيانة قصيرة مع baseline واضح (زمن SP التقاريرية + زمن شاشات البحث).
- حذف تدريجي (واحد واحد) مع قياس فوري وrollback plan.
- عدم تنفيذ أي حزمة حذف جماعية دفعة واحدة.
