# Projects & Project Extracts - Final UAT (2026-05-20)

## 1) ملخص المشكلة الأصلية
كانت شاشتا **المشاريع** و**مستخلص المشاريع** غير جاهزتين للإنتاج ضمن النطاق المختبَر بسبب مشكلات حرجة في:
- إضافة بنود المشروع داخل الـ Grid.
- فشل حفظ بنود المشروع بسبب عدم تمرير `ProjectMainDes.ID` في بيئة `Eng`.
- عدم اتساق حساب `CurrentValue` والإجماليات في شاشة المستخلص قبل/أثناء الحفظ.

## 2) الملفات المعدلة
- `Areas/MainErp/Views/Projects/_ProjectMainDesGrid.cshtml`
- `Areas/MainErp/Views/Projects/Edit.cshtml`
- `Areas/MainErp/Repositories/Projects/ProjectRepository.cs`
- `Areas/MainErp/Views/ProjectExtracts/Create.cshtml`
- `Areas/MainErp/ViewModels/Projects/ProjectViewModels.cs`

## 3) Bugs التي تم إصلاحها
1. **Critical - Project items grid add rows**
   - تم إصلاح إضافة الصفوف فعليًا داخل الـ Grid.
   - تم تثبيت naming/binding للمدخلات بنمط MVC صحيح مع إعادة ترقيم indexes عند الإضافة/الحذف قبل الإرسال.
   - بدون DevExpress.

2. **Critical - Project save failing بسبب `ProjectMainDes.ID`**
   - تم التعامل مع كون `ID` غير nullable وغير identity في بيئة الاختبار.
   - تم توليد `ID` عند الإدراج وفق النمط القائم بالمشروع وبشكل آمن داخل transaction.

3. **High - CurrentValue inconsistency في Project Extracts**
   - تم تفعيل إعادة الحساب الفوري client-side.
   - تم تنفيذ إعادة حساب نهائية قبل submit.
   - تم تعزيز server-side validation/recalculation وعدم الاعتماد على قيمة `CurrentValue` القادمة من الواجهة فقط.

## 4) نتيجة UAT النهائية على Eng
تم اعتماد النتيجة النهائية للشاشتين ضمن النطاق المختبَر (**within tested scope**) مع نجاح السيناريوهات الأساسية المطلوبة:
- إنشاء/حفظ/إعادة فتح مشروع ببنود.
- إنشاء/حفظ/إعادة فتح/تعديل مستخلص.
- ثبات الإجماليات وعدم تضاعف `QtyExe` و`TotalExe`.
- تحقق القيد وعدم التكرار ضمن سيناريو الاختبار المعتمد.
- عدم ظهور أخطاء تشغيل حرجة تمنع الاستخدام ضمن النطاق.

## 5) بيانات الاختبار المعتمدة
- **Project ID = 50**
- **Extract ID = 3510**

## 6) تأكيد Build
- **Build = 0 Errors** (آخر نتيجة بناء معتمدة).

## 7) تأكيد قاعدة البيانات
- **لا توجد أي DB schema changes** ضمن هذه المرحلة.
- **لا توجد Stored Procedures جديدة أو تعديلات schema** ضمن هذا الإغلاق.

## 8) تأكيد عدم المساس بمنطق الإعدادات المركزية
تم التأكيد على عدم المساس أو تبسيط منطق:
- `TblOptions`
- `Branches`
- `TblBranchesData`

وبالأخص أي ربط محاسبي/تشغيلي مرتبط بها.

## 9) Technical Debt
- استخدام نمط `MAX(ID)+1` مع `UPDLOCK,HOLDLOCK` **مقبول حاليًا** لأنه نمط قائم داخل النظام.
- التحسين المستقبلي الموصى به: الانتقال إلى **Sequence/Identity** أكثر أمانًا للتوازي العالي.

## 10) الحكم النهائي
**Production-ready within tested scope.**
