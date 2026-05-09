# Employee Payroll + Medical Insurance Full Cycle Test - 2026-05-09

## Scope

تم تنفيذ اختبار فعلي على قاعدة `Cash` لأنها نفس قاعدة كيشني التي ظهر بها الخلط سابقا. الاختبار تم على كود الريبو المشترك المستخدم من POS و MainErp، مع التأكد أن الموديول غير مربوط بالويب الرئيسي العام.

## Fixes Applied During Test

- ربط إنشاء الموظف بمنطق إنشاء حسابات محاسبية تلقائي:
  - `Account_code`: ذمم العاملين.
  - `Account_code1`: أجور مستحقة.
  - `Account_Code2`: مخصصات إجازة.
  - `Account_Code3`: مدفوعات مقدمة.
  - `Account_Code4`: مخصصات نهاية خدمة.
  - `Account_Code5`: مخصص تذاكر.
- الحسابات لا تستخدم آباء ثابتين عمياء؛ يتم استنتاج Parent account من نمط الموظفين الموجودين داخل نفس القاعدة، ثم fallback محدود إذا كان نفس الأب موجودا.
- تم احترام أعمدة Identity في `ACCOUNTS` و `emp_salary` داخل Cash/Eng وعدم إدخال `Account_ID` أو `id` يدويا.
- تم منع تكرار خصم التأمين عند إعادة حفظ نفس المسير:
  - الخصم الطبي المحفوظ سابقا في `TotalDiscount` يطرح من الخصومات القديمة قبل إعادة إضافة الخصم الحالي.
- تم منع إنشاء نسخة اشتراك تأمين جديدة عند تعديل نفس الموظف بنفس الخطة وتاريخ البداية بدون إرسال `EmployeeMedicalInsurance.Id`.

## Full Cycle Data

- Database: `Cash`
- Provider created: `ProviderId = 3`
- Plan created: `PlanId = 3`
- Plan cost: `1000`
- Employee share: `200` ثابت
- Company share: `800` AutoBalance
- Employee created: `Emp_ID = 333`
- Employee code: `T0509171715`
- Basic salary after edit: `5200`

## Employee Account Creation Result

تم إنشاء الحسابات التالية وربطها بالموظف:

- `Account_code = a1a2a4a1a335`
- `Account_code1 = a2a2a4a1a334`
- `Account_Code2 = a2a2a3a1a601`
- `Account_Code3 = a1a2a5a3a334`
- `Account_Code4 = a2a2a3a5a334`
- `Account_Code5 = a2a2a3a1a602`

كل الحسابات تم إنشاؤها كحسابات نهائية `last_account = 1` وبـ serial متسلسل تحت آبائها، مثل نمط شاشة VB6 القديمة.

## Payroll Cycle Result

Period: July 2026, `sgn = 20267`

Preview before save:

- Basic salary: `5200`
- Medical employee deduction: `200`
- Company medical cost: `800`
- Net salary: `5000`
- Journal preview lines: `3`

First save:

- Inserted salary rows: `1`
- Updated salary rows: `0`

Second save / recalculation:

- Inserted salary rows: `0`
- Updated salary rows: `1`
- Medical deduction stayed: `200`
- Net salary stayed: `5000`

Persisted `emp_salary` row:

- `id = 9268`
- `TotalDiscount = 200`
- `total2 = 200`
- `EmpTotalNet = 5000`

Persisted medical audit:

- `PayrollMedicalInsuranceDeduction` rows for employee/period: `1`
- `EmployeeDeduction = 200`
- `CompanyCost = 800`

## Stop Insurance Test

تم إيقاف الاشتراك بنهاية July 2026 ثم حساب Preview لشهر August 2026:

- Medical employee deduction: `0`
- Company medical cost: `0`
- Net salary: `5200`

## Main Web Isolation

تم فحص مراجع SQL modules داخل القاعدة للتأكد من عدم وجود ربط عام باسم `EmployeePayroll` خارج سياق الموديول، والنتيجة:

- `MainWebEmployeePayrollRefs = 0`

كما أن الكود المعدل موجود داخل:

- `Common/EmployeePayroll/EmployeePayrollRepository.cs`
- Controllers الخاصة بـ POS و MainErp تستخدم نفس الريبو لكن كل Area له connection string مستقل.

## Verification Commands

- Build:
  - `MSBuild MyERP.sln /p:Configuration=Debug /m /v:m`
- Result:
  - Build succeeded.
  - التحذيرات الموجودة من ملفات قديمة وغير مرتبطة بالتعديل.

## Notes

- قيد التأمين الطبي في هذه المرحلة يظهر داخل `JournalPreview` كخصم على حساب الموظف بنفس فلسفة الخصومات. لم يتم ترحيل قيد محاسبي فعلي إلى `Notes/DOUBLE_ENTREY_VOUCHERS` لأن شاشة VB6 تستخدم مسار ترحيل محاسبي كبير وحساس، وتم ترك الترحيل الفعلي للمرحلة التالية بعد اعتماد Mapping الحسابات لتفادي تغيير اتجاهات قيود Production.
