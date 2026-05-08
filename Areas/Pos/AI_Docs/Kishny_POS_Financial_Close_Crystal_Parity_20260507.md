# Kishny POS - مطابقة تقرير الإغلاق المالي الشامل على مستوى الفروع

## مكان الأصل في VB6

- الشاشة الأصلية: `project_status`
- الحدث: `Command15_Click`
- الملف المرجعي: `F:\Source Code\SatriahMain\Cayshny\Frm\New frm\project_status.frm`
- يبدأ الحدث عند السطر 9735.
- تقرير Crystal المستخدم: `F:\Source Code\SatriahMain\Cayshny\Reports\REPORTS NEW\CloseReprotTotal2FastWithDisc.rpt`

## مصدر البيانات الأصلي

حدث VB6 يفتح تقرير Crystal ثم يمرر له Recordset من:

```sql
dbo.BranchDailyRollup_Parity
```

ببارامترات:

- `@FromDate`
- `@ToDate`
- `@PosAccountCode`
- `@BankChargeAccountCode`
- `@P_BranchList`

كما يمرر Crystal Parameters منها اسم الشركة، الفرع، من/إلى تاريخ، وأرصدة بنكية/POS حسب فهارس التقرير.

## ما كان ناقصا في .NET

المسار الفعلي في .NET:

- الشاشة: `Areas/Pos/Views/PosReports/Index.cshtml`
- الكارت: `finance-closing-discounts`
- Controller: `Areas/Pos/Controllers/PosReportsController.cs`
- SQL: `Areas/Pos/Sql/27_POS_ReportStoredProcedures.sql`
- Stored Procedure: `dbo.usp_POS_Report_Run`

قبل التعديل كان تقرير `finance-closing-discounts` يرجع سطور خام من `TBLClosePos` و `Notes`، وليس تجميعا على مستوى الفرع مثل Crystal. كان ينقصه ترتيب أعمدة Crystal وصيغ إجمالي التوريد، Wallet، أصل الشحن، الخصم، الرسوم شامل الضريبة، المرتجعات، صافي Cash Out، والعهدة.

## ما تم تعديله

- تم تعديل فرع `@reportKey = N'finance-closing-discounts'` داخل `Areas/Pos/Sql/27_POS_ReportStoredProcedures.sql`.
- أصبح التقرير يجمع البيانات حسب `BranchID`.
- تم استخدام أعمدة `TBLClosePos` المطابقة لحقول Crystal:
  - `Net`
  - `TotalSaleDay2Vat`
  - `TotalRevPOS`
  - `NetPOS`
  - `CashOutTotal`
  - `CashOut`
  - `CashOutDisc`
  - `TotalRechargeValue`
  - `TotalRev2`
  - `TotalRevvat`
  - `BoxBalance` كـ Alias باسم `BoxValue`
- تم حساب المرتجعات من `Transactions` لنوع الحركة `9` داخل نفس الفترة، لأن `TBLClosePos` لا يخزن `TotalReturns` كعمود مستقل في التنفيذ الحالي.
- تم تعديل عنوان الكارت/التقرير في `PosReportsController.cs` إلى:
  `تقرير الإغلاق المالي الشامل على مستوى الفروع`
- تم إضافة عناوين الأعمدة العربية المطابقة.
- تم إضافة Render خاص داخل `Index.cshtml` لهذا التقرير فقط، حتى لا يظهر جدول الأعمدة الخام من الستورد، ويخرج بدلًا منه ترتيب Crystal المطلوب.
- تم إضافة فلاتر تشغيل إضافية: من فرع، إلى فرع، إظهار الفروع الفاضية، وفلتر خدمة/نشاط ذكي.
- تم ضبط ترميز نص `ClosingStatus` العربي داخل SQL، ويجب تطبيق السكريبت بأداة تقرأ UTF-8 مثل `sqlcmd -f 65001`.
- تم إضافة زر طباعة عام لكل التقارير الموجودة في شاشة تقارير POS، مع نافذة طباعة مستقلة تعرض عنوان التقرير والفترة والفلاتر والجدول.
- تم إضافة إجمالي عام أسفل جدول العرض.
- تم إضافة صف إجمالي عام في Excel.
- تم توحيد نفس العنوان وعناوين الأعمدة في `HtmlReportsController.cs` لنفس `reportKey`.

## الصيغ المنقولة

```text
إجمالي التوريد =
SUM(Net)
+ SUM(TotalSaleDay2Vat)
+ SUM(TotalRevPOS)
+ SUM(NetPOS)
- SUM(TotalReturns)

رصيد Wallet =
CashOutTotal + CashOut

توريد Wallet =
CashOutTotal + (CashOut - CashOutDisc)

أصل مبلغ الشحن =
TotalRechargeValue

الخصم =
TotalRev2

رسوم الشحن شامل الضريبة =
TotalRev2 + TotalRevvat

العهدة =
BoxValue
```

## الملفات المعدلة

- `Areas/Pos/Sql/27_POS_ReportStoredProcedures.sql`
- `Areas/Pos/Controllers/PosReportsController.cs`
- `Areas/Pos/Controllers/HtmlReportsController.cs`
- `Areas/Pos/Views/PosReports/Index.cshtml`
- `Areas/Pos/AI_Docs/Kishny_POS_Financial_Close_Crystal_Parity_20260507.md`

## اختبار التشغيل

خطوات الاختبار اليدوي:

1. تطبيق SQL الموجود في `Areas/Pos/Sql/27_POS_ReportStoredProcedures.sql`.
   - تم تطبيقه محليًا على قاعدة `Cash` باستخدام `KishnyCashConnection` في 2026-05-07 بالأمر `sqlcmd -f 65001` للحفاظ على الترميز العربي.
2. فتح لوحة تشغيل كيشني.
3. فتح تبويب `تقارير الإغلاقات`.
4. تشغيل `تقرير الإغلاق المالي الشامل على مستوى الفروع`.
5. اختيار نفس FromDate / ToDate المستخدمة في Crystal.
6. مقارنة الأعمدة والقيم مع `CloseReprotTotal2FastWithDisc.rpt`.
7. تجربة زر `تصدير Excel` والتأكد من ظهور صف `الإجمالي العام`.

## اختلاف متبق عن Crystal

- Crystal يعرض Summary جانبي/سفلي غني برسوميات وتفاصيل بنكية؛ التنفيذ الحالي أضاف إجمالي عام داخل جدول HTML/Excel، ولم ينسخ تخطيط Crystal الرسومي بالكامل.
- `BoxValue` غير مخزن بهذا الاسم داخل `TBLClosePos` في .NET، لذلك تم عمل Alias من `BoxBalance` إلى `BoxValue`.
- `TotalReturns` غير مخزن كعمود مستقل داخل `TBLClosePos` في .NET، لذلك تم حسابه من حركات المرتجعات `Transactions.Transaction_Type = 9` ضمن نفس الفترة.
