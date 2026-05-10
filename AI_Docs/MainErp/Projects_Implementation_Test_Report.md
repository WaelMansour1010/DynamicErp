# تقرير تنفيذ شاشة المشاريع MainErp

## الملفات المضافة

- `Areas/MainErp/Controllers/ProjectsController.cs`
- `Areas/MainErp/Repositories/Projects/ProjectRepository.cs`
- `Areas/MainErp/ViewModels/Projects/ProjectViewModels.cs`
- `Areas/MainErp/Views/Projects/Index.cshtml`
- `Areas/MainErp/Views/Projects/Edit.cshtml`
- `Areas/MainErp/Views/ProjectExtracts/Create.cshtml`
- `Areas/MainErp/Sql/07_MainErp_Projects_ReadWrite_Procedures.sql`

## الملفات المعدلة

- `Areas/MainErp/Controllers/ProjectExtractsController.cs`
- `Areas/MainErp/Views/Shared/_MainErpSidebar.cshtml`
- `Areas/MainErp/MainErpAreaRegistration.cs`
- `MyERP.csproj`

## ملاحظات قاعدة البيانات

- لم يتم تعديل schema.
- جدول `projects.id` و`project_billl.id` ليسا Identity، لذلك تم استخدام توليد رقم يدوي داخل Transaction مع `UPDLOCK, HOLDLOCK`.
- Stored Procedure script مرفق بصيغة DROP ثم CREATE ومتوافق مع SQL Server 2012، لكنه غير مفروض على التطبيق لأن Repository الحالي يستخدم SQL parameterized مباشرًا.

## نتائج الاختبار

سيتم تحديث هذا الملف بعد دورة الاختبار النهائية.
