# Database Update Manager

## الفكرة

`Database Update Manager` هي شاشة داخل MainErp لإدارة تحديثات قاعدة البيانات من داخل الويب بدل تشغيل PowerShell يدويًا عند كل عميل. PowerShell بقي أداة مساعدة للمطورين فقط، بينما التشغيل العملي يكون من الشاشة.

## مكان الشاشة

المسار:

```text
/MainErp/DatabaseMigration
```

وتظهر في قائمة MainErp تحت:

```text
إدارة النظام > تحديثات قاعدة البيانات
```

## الصلاحيات

الشاشة محمية server-side ولا تظهر إلا لمستخدم MainErp Admin:

- `DatabaseMigration.View`
- `DatabaseMigration.Manage`
- `DatabaseMigration.Apply`

المستخدم العادي لا يستطيع الوصول حتى بالـ URL. الشاشة لا تعرض ConnectionString أو Password، ولا تقبل مسار سكريبت من المستخدم.

## مصادر السكريبتات

تقرأ الشاشة من whitelist فقط في `Web.config`:

```xml
<add key="DatabaseMigrationFolders" value="~/Database/Migrations" />
<add key="DatabaseMigrationEnvironment" value="Current" />
```

يمكن إضافة أكثر من مصدر بالفاصلة أو الفاصلة المنقوطة، مثل:

```xml
<add key="DatabaseMigrationFolders" value="~/Database/Migrations;~/Areas/Pos/Database/Migrations" />
```

لا تضف `Areas/Pos/Sql` أو `Areas/MainErp/Sql` إلى العميل إلا بعد تصنيف الملفات القديمة وتحويلها إلى migrations مرقمة وآمنة.

## قواعد تسمية السكريبتات

الملف القابل للتطبيق تلقائيًا يجب أن يكون بالشكل:

```text
0001_POS_CreateTables.sql
0002_POS_SaveTransaction.sql
0003_MainErp_AddReports.sql
```

أي ملف قديم أو غير مرقم يظهر كـ `Unclassified` ولا يتم تطبيقه تلقائيًا.

## Header مطلوب

```sql
/*
Migration number: 0001
Module: POS
Purpose: Short purpose
Safe to rerun? Yes/No
Dependencies: None
Date: YYYY-MM-DD
Author/Agent: Name
*/
```

## Dry Run

زر `فحص بدون تنفيذ` يعرض:

- Pending scripts
- Hash mismatch warnings
- Unclassified files
- التحذيرات الخطرة داخل السكريبت

Dry Run لا يطبق scripts ولا ينشئ objects خاصة بالـ migrations.

## Apply

التطبيق لا يحدث مباشرة:

1. راجع Pending وWarnings.
2. تأكد من وجود Backup.
3. اكتب كلمة التأكيد `APPLY`.
4. اختر `Apply Selected` أو `Apply All Pending`.

وقت التنفيذ يتم إنشاء/تحديث:

- `dbo.DatabaseMigrationHistory`
- `dbo.DatabaseMigrationRun`
- `dbo.DatabaseMigrationRunDetail`

كل سكريبت ينفذ server-side فقط داخل transaction عند الإمكان، ويتم تسجيل النجاح أو الفشل.

## Preview

زر Preview يعرض السكريبت read-only، ويظهر تحذيرات عند وجود:

- `DROP TABLE`
- `DELETE` بدون `WHERE`
- `UPDATE` بدون `WHERE`
- `TRUNCATE`
- `ALTER COLUMN`
- Dynamic SQL

التحذير لا يعني المنع دائمًا، لكنه إشارة مراجعة قوية قبل Apply.

## عند الفشل

- راجع تبويب `Failed / Errors`.
- راجع `Execution Log`.
- لا تعدل migration مطبق بنجاح.
- أصلح المشكلة في سكريبت جديد أو صحح السكريبت الفاشل قبل نجاحه.
- أعد تشغيل Dry Run قبل Apply جديد.

## عند Hash Mismatch

Hash mismatch يعني أن نفس `ScriptName` تم تطبيقه سابقًا لكن محتوى الملف الحالي تغير. التصرف الصحيح:

- لا تطبق الملف المعدل.
- أعد الملف لنفس محتواه السابق، أو
- أنشئ migration جديد برقم جديد يصف التعديل.

## Production Warning

في Production:

1. اعمل Backup أولًا.
2. شغّل Dry Run.
3. صدّر التقرير.
4. راجع التحذيرات.
5. نفذ Apply بعد الموافقة فقط.

