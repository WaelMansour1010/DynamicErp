# POS UI Layout Audit And Fixes - 2026-05-09

## النطاق

تم حصر العمل داخل `Areas/Pos` فقط. لم يتم تعديل أي ملف خارج POS. توجد تعديلات كثيرة مسبقة في working tree قبل هذا العمل، لذلك هذا التقرير يوثق تعديلات واجهات POS التي تمت في هذه الجولة فقط.

## الشاشات التي تمت مراجعتها

- `PosReports/Index`: شاشة تقارير كيشني وفلاتر التقارير.
- `PosDashboard/Index`: فلاتر لوحة التحكم التنفيذية داخل لوحة تشغيل كيشني.
- `PosTransaction/Index`: شاشة البيع وفلاتر فواتير المبيعات.
- `PosLogin/Index`: صفحة الدخول الأساسية.
- `EmployeePayroll/MedicalInsurance` و partials الخاصة بها: شاشة إعدادات التأمين الطبي.
- مراجعة ثابتة للشاشات ذات فلاتر/جداول واضحة داخل POS مثل `Cashing`, `Payments`, `StockTransfer`, `SalesRepresentativesPerformance`, `FinancialIntelligence`, `HtmlReports`, و `ExcelImport`.

## المشاكل التي وجدت

- فلاتر التقارير كانت تعتمد على `four-cols` ثابتة مع عناصر كثيرة، ما يسبب ضغط وتداخل على العروض المتوسطة والصغيرة.
- checkbox `إظهار الفروع` في التقارير كان يستخدم `padding-top` لمحاكاة المحاذاة، وهذا يسبب كسرًا عند تغير ارتفاع الحقول أو عرض الشاشة.
- الجداول الناتجة من التقارير تحتاج إطار scroll أفقي ثابت بدل كسر الصفحة.
- شاشة التأمين الطبي كانت بحاجة إلى form grid أوضح: label فوق الحقل، أعمدة responsive، وفصل cards النماذج عن cards الجداول.
- جداول التأمين كانت تحتاج table-card واضح و scroll عند الحاجة، مع تقليل scroll غير الضروري في جدول الشركات.
- بعض checkboxes داخل POS، مثل `فواتير Excel فقط` في شاشة البيع، كانت ترث ارتفاع inputs العام فتظهر كمربع كبير وتكسر المحاذاة.
- شاشة التأمين الطبي لم تكن تحتوي `viewport` meta، ما يضعف سلوك mobile responsive.

## ما تم إصلاحه

- إضافة CSS موحد قابل لإعادة الاستخدام داخل `pos-transaction.css`:
  - `pos-card`
  - `pos-filter-card`
  - `pos-filter-grid`
  - `pos-form-grid`
  - `pos-field`
  - `pos-check-field`
  - تحسين `responsive-table` و `pos-table-wrap`
- إعادة تنظيم فلاتر `PosReports/Index` داخل card بعنوان `فلاتر التقارير`.
- تثبيت كل حقول تقارير كيشني كـ field blocks: label فوق input/select بدون widths ثابتة أو تداخل.
- إصلاح checkbox `إظهار الفروع الخاضعة / الفاضية` بمحاذاة واضحة داخل filter grid.
- جعل report cards و result tables تتحمل العرض الصغير، مع scroll أفقي للجداول العريضة.
- تحسين grid فلاتر dashboard لأنها تشارك `reports-filter-grid`.
- تحسين checkboxes العامة داخل `pos-grid` حتى لا تظهر بحجم input كامل.
- إضافة `viewport` إلى شاشة إعدادات التأمين الطبي.
- تحسين `employee-payroll.css`:
  - grids responsive للنماذج.
  - table cards واضحة.
  - حقول labels فوق inputs.
  - أحجام min-width مناسبة لجداول الشركات والخطط.
  - منع overflow غير المقصود على الصفحة.

## فحص مشكلة ظهور العربي كـ ?????

- مسار الحفظ والقراءة الحالي في `EmployeePayrollController` يستخدم `Common.EmployeePayroll.EmployeePayrollRepository`.
- راجعت مسار repository للـ providers/plans: parameters الخاصة بالأسماء والملاحظات تستخدم `SqlDbType.NVarChar`.
- راجعت DDL المرجعي للتأمين الطبي: الأعمدة `ProviderNameAr`, `ProviderNameEn`, `PlanNameAr`, `PlanNameEn`, `Notes` معرفة كـ `NVARCHAR`.
- ملف POS SQL الحالي `Areas/Pos/Sql/52_POS_EmployeePayroll_MedicalInsurance.sql` هو pointer للسكريبت canonical ولا ينفذ DDL خاص به.
- بناء على ذلك، إن ظهرت بيانات محفوظة فعليًا كـ `?????` داخل الجداول فهي غالبًا data corruption قديمة أو إدخال سابق عبر مسار لا يستخدم Unicode. لم أعدل SQL لأن المسار الحالي لا يظهر مشكلة `varchar` لهذه الشاشة.

## الاختبارات

- تم تشغيل IIS Express محليًا على `http://localhost:55209`.
- HTTP 200:
  - `/Pos/PosLogin/Index`
  - `/Pos/PosReports/Index`
  - `/Pos/EmployeePayroll/MedicalInsurance`
  - `/Pos/PosTransaction/Index`
  - `/Pos/PosDashboard/Index`
- فحص بصري بالمتصفح الداخلي:
  - Reports على 1920px و1366px و1024px وmobile width.
  - Medical Insurance على 1366px و1024px وmobile width.
  - PosTransaction على 1366px بعد إصلاح checkbox.
- Developer console: لا توجد أخطاء JS جديدة في الصفحات التي تم فتحها.
- `dotnet build MyERP.csproj --no-restore` فشل بسبب غياب `Microsoft.WebApplication.targets` من بيئة dotnet SDK، وهو متطلب ASP.NET Web Application/Visual Studio وليس خطأ في تعديلات POS.

## ما لم يتم اختباره بالكامل

- لم يتم تنفيذ حفظ فعلي لشركة/خطة تأمين أو تشغيل تقارير ببيانات إنتاجية لتجنب تغيير بيانات قاعدة البيانات.
- لم يتم اختبار دورة login credentials جديدة لأن الجلسة الحالية أعادت سياق admin في IIS Express.
- لم يتم التحقق من بيانات `?????` داخل قاعدة البيانات مباشرة، لأن الواجهة الحالية وDDL/parameters تدعم Unicode؛ يلزم فحص DB rows المتضررة لتحديد هل الفساد قديم.

## الملفات المعدلة في هذه الجولة

- `Areas/Pos/Content/pos-transaction.css`
- `Areas/Pos/Content/employee-payroll.css`
- `Areas/Pos/Views/PosReports/Index.cshtml`
- `Areas/Pos/Views/EmployeePayroll/MedicalInsurance.cshtml`
- `Areas/Pos/Views/EmployeePayroll/_MedicalInsuranceSettings.cshtml`
- `Areas/Pos/Docs/POS_UI_Layout_Audit_And_Fixes.md`
