# تحليل شاشة Projects من VB6 الأساسي

المصدر الذي تم فحصه:
`F:\Source Code\SatriahMain\Frm\New frm\projects\projects.frm`

> المسار المطلوب في التكليف بحروف `New fm` غير موجود على القرص، وتم استخدام نسخة المشروع الأساسي خارج `Cayshny`.

## الحقول الظاهرة الرئيسية

- كود المشروع: `DCPreFix + txtid` ويحفظ في `projects.Fullcode`، مع `prifix` و`Code`.
- اسم المشروع عربي/إنجليزي: `TXTprojectname`, `TXTprojectnamee`.
- العميل النهائي ومقاول الباطن: `DcAccount2`, `DcAccount4` مع حسابات داخلية.
- قيمة المشروع، الخصم، نسبة الخصم، الصافي: `TxtProjectCosts`, `txt_total_discount`, `TxtDiscountPercentage`, `total_after_discount`.
- الحالة والفرع والعملة ونوع العقد: `DataCombo1`, `dcBranch`, `DcCurrency`, `DataCombo5`.
- التواريخ: بداية، نهاية، أقرب نهاية.
- بيانات إدارية: مدير الموقع، المندوب، الإدارة، رقم العقد، الملاحظات.
- أرصدة افتتاحية متعددة مرتبطة بحسابات المصروفات/الإيرادات/المواد/الأجور/المستخلصات/حسن الأداء/الدفعة المقدمة.
- بيانات ضمان وتأمين: رقم الضمان، قيمته، تواريخه، البنك، التأمين.
- بنود المشروع الرئيسية: `ProjectMainDes`.
- دفعات/شروط المشروع: `Projectssub`.
- عمليات البنود والمواد والعمالة والمصاريف موجودة في الشاشة القديمة لكنها أوسع من دورة CRUD المطلوبة هنا.

## الأزرار والمنطق

- جديد: يحجز `id` يدويًا من `new_id("projects","id")` ثم يضيف صفًا مؤقتًا في `projects`.
- حفظ: `SaveData` ينفذ validations ثم يحفظ نفس صف `projects` ويعيد بناء تفاصيل مثل `ProjectMainDes` و`Projectssub`.
- تعديل: يغير الحالة إلى Edit ويفتح الجداول للتعديل ثم يعيد الحفظ.
- بحث: يفتح `FrmProjectSearch`.
- حذف/تراجع/مرفقات/تقارير: موجودة في VB6، ولم يتم نقل الحذف لأن التكليف ركز على إضافة/حفظ/تعديل/بحث وتكامل المستخلص.

## validations المنقولة

- العميل النهائي مطلوب.
- اسم المشروع مطلوب.
- قيمة المشروع رقمية.
- الخصم رقمي ولا يتجاوز قيمة المشروع في شاشة الويب.
- الفرع مطلوب.
- حالة المشروع مطلوبة.
- كود المشروع لا يتكرر على `Fullcode`.
- تاريخ النهاية لا يسبق تاريخ البداية.

## الجداول والعلاقات التي تم فحصها

- `projects`: جدول المشروع الأساسي، و`id` ليس Identity.
- `project_status`: حالات المشروع.
- `TblBranchesData`: الفروع.
- `TblCustemers`: العملاء/المقاولون.
- `ACCOUNTS`: الحسابات، ويعرض المستخدم `Account_Serial - Account_Name` عند الحاجة.
- `project_billl`: رأس مستخلص المشروع، و`id` ليس Identity.
- `project_bill_details`: تفاصيل المستخلصات الحالية.
- `Notes`: الربط المحاسبي للمستخلصات القديمة.
- `ProjectMainDes`, `Projectssub`, `TblProjectUser`: تفاصيل إضافية من شاشة VB6.

## Mapping مختصر

| VB6 | Database | Web |
| --- | --- | --- |
| `txt_project_id` | `projects.id` | `Id` |
| `DCPreFix` | `projects.prifix` | `Prefix` |
| `txtid` | `projects.Code` | `Code` |
| `DCPreFix + txtid` | `projects.Fullcode` | `FullCode` |
| `TXTprojectname` | `projects.Project_name` | `ProjectName` |
| `TXTprojectnamee` | `projects.Project_nameE` | `ProjectNameEnglish` |
| `DcAccount2.BoundText` | `projects.End_user_id` | `EndUserId` |
| `DcAccount4.BoundText` | `projects.sub_contractor_id` | `SubContractorId` |
| `dcBranch.BoundText` | `projects.branch_no` | `BranchNo` |
| `DataCombo1.BoundText` | `projects.Project_status` | `StatusId` |
| `TxtProjectCosts` | `projects.project_cost` | `ProjectCost` |
| `txt_total_discount` | `projects.general_discount` | `GeneralDiscount` |
| `total_after_discount` | `projects.cost_after_discount`, `projects.net` | `CostAfterDiscount` |
| `DTStartDate`, `DTEnddate` | `projects.StartDate`, `projects.EndDate` | `StartDate`, `EndDate` |
| `TxtRemarks` | `projects.Remarkss` | `Remarks` |

## فروقات واعية

- لم يتم تحميل combos ضخمة بلا حدود؛ القوائم الكبيرة محدودة بـ TOP 300 في العملاء/الحسابات/الموظفين.
- لم يتم نقل حذف المشروع لأن علاقاته واسعة وخطرة، ويحتاج سياسة حذف منفصلة.
- لم يتم إنشاء حسابات تلقائيًا مثل VB6؛ الشاشة تحفظ المشروع فعليًا وتعرض الحسابات بالـ serial/name عند الحاجة، بدون تخمين شجرة الحسابات.
