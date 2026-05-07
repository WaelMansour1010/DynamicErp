# Sales Representatives Performance Dashboard

## 1. تحليل ملف Excel

الملف محل التحليل:

`C:\RDPPrintQueue\Excel\العبور (2).xlsx`

الشيتات:

- شيتات يومية من `1 ` إلى `31`.
- شيت `Total`.
- شيت `كــــــــروت`.

كل شيت يومي حجمه تقريبًا `226` صفًا و`59/60` عمودًا. جدول إدخال العمليات اليومي موجود في النطاق `B3:J226`، وأعمدته:

- `B`: مسلسل.
- `C`: IPN.
- `D`: اسم العميل.
- `E`: رقم التليفون.
- `F`: قيمة الشحنة.
- `G`: قيمة الرسوم / العمولة.
- `H`: الإجمالي.
- `I`: نوع الخدمة.
- `J`: التاريخ.

أنواع الخدمة الظاهرة في معادلات الملف:

- `كاش ان`
- `كاش اوت`
- `مخالفات`

### خلايا الملخص اليومي

النمط الثابت في الشيتات اليومية:

- `M3`: تاريخ اليوم.
- `M4`: رصيد العهدة أول اليوم.
- `M5`: كارت مصر مباع.
- `M6`: كارت أهلي مباع.
- `M7`: المردودات.
- `M8`: وارد كارت مصر.
- `M9`: وارد كارت أهلي.

ملخص اليوم في `O:P`:

- `P3 = M5 + M6`: مبيعات الكارت عددًا.
- `P4 = M4`: رصيد العهدة أول اليوم.
- `P5 = SUMIF([نوع الخدمة],"كاش ان",[قيمة الشحنه])`: مبلغ الشحن.
- `P6 = P3 * 150`: مبلغ الكروت اليومي.
- `P7 = SUMIF([نوع الخدمة],"مخالفات",[الاجمالي])`: مبلغ رسوم المخالفات اليومي في الإكسيل.
- `P8 = M7`: المردودات.
- `P9 = SUMIF([نوع الخدمة],"كاش ان",[قيمة الرسوم / العموله])`: رسوم Cash In.
- `P10 = SUMIF([نوع الخدمة],"كاش اوت",[قيمة الشحنه])`: Cash Out بدون الرسوم.
- `P11 = (P5 + P6 + P9 + P7 + P4) - P10 - P8`: التوريد النقدي / الدرج.
- `P12 = SUMIF([نوع الخدمة],"كاش اوت",[الاجمالي])`: Cash Out بالرسوم.
- `P14/P15` حسب بعض الشيتات: عدد المخالفات.
- `P15/P16`: عدد حركات Cash In.
- `P16/P17`: عدد حركات Cash Out.
- في شيت `1 ` تحديدًا `P17 = P15 + P16`: إجمالي عدد حركات Cash In وCash Out.

يوجد اختلاف بسيط في إزاحة صفوف العد بين بعض الشيتات، لذلك عند تحويل المنطق إلى تقرير ويب لا نعتمد على أرقام الصفوف للعد، بل على نوع الخدمة نفسه.

### بلوك التارجت اليومي

البلوك المطلوب كـ Component مستقل موجود في نفس الخلايا في الشيتات اليومية `1 ` إلى `31`:

- `S4`: عنوان إجمالي الشحنات.
- `T4`: عنوان إجمالي الكروت.
- `S5 = P5 + P9 + P12 + P7`: إجمالي الشحنات/المحقق المالي اليومي حسب منطق الملف.
- `T5 = P3`: إجمالي الكروت.
- `R8`: الدايلي تارجت للشحنات.
- `S8`: تحقيق اليوم للشحنات.
- `T8`: نسبة الشحنات.
- `U8`: الدايلي كروت.
- `V8`: تحقيق اليوم للكروت.
- `W8`: نسبة الكروت.
- `R9 = Total!H5 / 24`: التارجت اليومي للشحنات.
- `S9 = S5`: تحقيق الشحنات اليوم.
- `T9 = S9 / R9`: نسبة تحقيق الشحنات.
- `U9 = Total!K5 / 24`: التارجت اليومي للكروت.
- `V9 = T5`: تحقيق الكروت اليوم.
- `W9 = V9 / U9`: نسبة تحقيق الكروت.
- `R10:W10` خلية مدمجة، ومعادلتها `R10 = (W9 + T9) / 2`: النسبة الإجمالية.

الاستنتاج المهم: قيمة `103%` أو أي نسبة أخرى ليست ثابتة. النسبة ناتجة من متوسط نسبتي الشحنات والكروت. التارجت اليومي في الشيتات اليومية محسوب من تارجت شهري موجود في `Total!H5` للشحنات و`Total!K5` للكروت، لكنه مقسوم على `24` داخل الشيت اليومي. وفي شيت `Total` توجد منطقة أخرى تقسم على أيام الشهر/أيام العمل مثل `H5/25` و`K5/25`، ما يعني أن عدد أيام العمل مدخل/افتراضي وليس مستنتجًا من تقويم النظام.

### شيت Total

أهم الخلايا:

- `F5`: كود الفرع/المندوب في الملف، قيمته `69`.
- `G5`: اسم الفرع، قيمته `العبور`.
- `H5`: تارجت الشحنات الشهري، قيمته في الملف `700000`.
- `I5 = C36`: المحقق من الشحنات، حيث `C36 = SUM(C5:C35)`.
- `J5 = I5 / H5`: نسبة تحقيق الشحنات.
- `K5`: تارجت الكروت الشهري، قيمته في الملف `70`.
- `L5 = D36`: المحقق من الكروت، حيث `D36 = SUM(D5:D35)`.
- `M5 = L5 / K5`: نسبة تحقيق الكروت.
- `N5 = (M5 + J5) / 2`: النسبة الإجمالية.
- `W10 = X6 - W6`: عدد الأيام المتبقية.
- `W16 = (H5 - I5) / W10`: المطلوب يوميًا للشحنات لتحقيق التارجت.
- `W19 = (K5 - L5) / W10`: المطلوب يوميًا للكروت لتحقيق التارجت.
- `AE6 = AA6 / AD6`: التارجت اليومي للشحنات.
- `AF6 = AE6 * AC6`: التارجت المتوقع حتى تاريخ اليوم.
- `AG6 = I5 / AF6`: Projection للشحنات كنسبة.
- `AE9 = AA9 / AD9`: التارجت اليومي للكروت.
- `AF9 = AE9 * AC9`: التارجت المتوقع للكروت حتى تاريخ اليوم.
- `AG9 = L5 / AF9`: Projection للكروت كنسبة.
- `AA25 = (AA16 + AA19) / 2`: Projection إجمالي.

## 2. Mapping بين Excel والداتا الحقيقية

تم فحص قاعدة `Cash` التي يستخدمها POS من `KishnyCashConnection`.

الجداول والحقول التي تم التحقق منها فعليًا:

- `dbo.Transactions`
  - `Transaction_ID`
  - `Transaction_Date`
  - `Transaction_Type`
  - `UserID`
  - `Emp_ID`
  - `empid`
  - `BranchId`
  - `RechargeValue`
  - `NetValue`
  - `VAT`
  - `Transaction_NetValue`
  - `PayedValue`
  - `IsCashOut`
  - `IsPOS`
  - `TrafficViolations`
  - `ViolationsValue`
  - `VisaNumber`
  - `ItemIDService`
  - `ItemIDService2`
- `dbo.Transaction_Details`
  - `Transaction_ID`
  - `Item_ID`
  - `Price`
  - `ShowQty`
  - `TotalPrice`
  - `Commisionvalue`
  - `Vat`
- `dbo.TblUsers`
  - `UserID`
  - `UserName`
  - `Empid`
  - `BranchId`
  - `UserType`
  - `UserCategory`
  - `isDeactivated`
- `dbo.POS_UserPermissions`
  - `UserID`
  - `PermissionKey`
  - `IsAllowed`
- `dbo.TBLSalesRepData`, `dbo.TBLSalesRepData2`, `dbo.TBLSalesRepData3`
  - `EmpID`
  - `BranchId`
  - `GroupID`
- `dbo.TblEmployee`
  - `Emp_ID`
  - `Emp_Name`
  - `BranchId`
- `dbo.TblBranchesData`
  - `branch_id`
  - `branch_name`
  - `branch_namee`
- `dbo.TblItems`
  - `ItemID`
  - `ItemName`
  - `ItemType`
  - `TrafficViolations`
- `dbo.TransactionTypes`
  - `Transaction_Type = 21` هو `Sales Invoice From Issue Voucher` ويستخدمه POS.
- `dbo.TblPaymentType`, `dbo.TblSalesPayment`, `dbo.TblTransactionPayments` تم فحصها، لكنها ليست مصدر التصنيف الأساسي لهذا التقرير.

علاقة المندوب:

- المصدر التشغيلي هو `Transactions.UserID`.
- اسم المندوب يعرض من `TblEmployee.Emp_Name` عبر `COALESCE(Transactions.Emp_ID, Transactions.empid, TblUsers.Empid)`.
- عند عدم وجود اسم موظف، يتم الرجوع إلى `TblUsers.UserName`.
- التقرير لا يعرض كل المستخدمين. المستخدم يدخل التقرير فقط إذا كان:
  - `TblUsers.UserCategory` يساوي `تلر` أو `Teller`.
  - أو لديه `POS_UserPermissions.PermissionKey = CanTeller` وقيمتها مفعلة.
  - أو الموظف المرتبط به مسجل كبياع في `TBLSalesRepData` أو `TBLSalesRepData2` أو `TBLSalesRepData3`.

علاقة الفرع:

- `Transactions.BranchId = TblBranchesData.branch_id`.

تصنيف الخدمات مطابق للكود الحالي في POS:

- `TrafficViolations = 1` => `violations`.
- `IsPOS = 1` أو وجود `VisaNumber` => `card`.
- `IsCashOut = 1` => `cash-out`.
- غير ذلك => `cash-in`.

Mapping المقاييس:

- قيمة الشحنة في Excel => `Transactions.RechargeValue`.
- الرسوم / العمولة => `Transactions.NetValue + Transactions.VAT`.
- الإجمالي => حسب الخدمة:
  - Cash Out بالرسوم = `RechargeValue + NetValue + VAT`.
  - Card Total = `Transaction_NetValue` أو `PayedValue`.
  - Violations Fees = `NetValue + VAT`.
- المخالفات الفعلية => `Transactions.ViolationsValue`.
- عدد الحركات => `COUNT(*)` حسب التصنيف.
- التوريد المتوقع => تطبيق منطق Excel المتاح من الداتا: `CashIn + CardsTotal + CashInFees + ViolationsFees - CashOutWithoutFees`. العهدة أول اليوم والمردودات ليستا متاحتين في `Transactions` بنفس خلايا Excel، لذلك لم يتم افتراض مصدر غير موثق لهما.

ملاحظة عن التارجت:

لم يظهر جدول موثق لتارجت شهري للشحنات والكروت بنفس معنى `Total!H5` و`Total!K5`. الجداول `TblSalesTarget` و`TblSalesRepCommTarget` تخص شرائح/عمولات المبيعات ولا تثبت أنها تارجت شحنات/كروت شهري. لذلك تم جعل تارجت الشحنات والكروت مدخلات فلترة اختيارية، مع الحفاظ على المعادلات نفسها.

## 3. تصميم التقرير

اسم التقرير:

`Sales Representatives Performance Dashboard`

الواجهة:

- RTL عربي كامل.
- Bootstrap 3.
- كروت KPI أعلى الصفحة.
- فلتر:
  - من تاريخ.
  - إلى تاريخ.
  - الفرع.
  - المندوب.
  - نوع الخدمة.
  - تارجت الشحنات الشهري.
  - تارجت الكروت الشهري.
  - أيام العمل بالشهر.
- جدول تفصيلي للمناديب.
- Export Excel بصيغة HTML Excel آمنة بدون مكتبات جديدة.
- طباعة عبر `window.print`.

Partial View المستقلة:

`Areas/Pos/Views/SalesRepresentativesPerformance/_SalesTargetAchievementCard.cshtml`

تعرض:

- إجمالي الشحنات.
- إجمالي الكروت.
- تارجت الشحنات.
- تحقيق الشحنات.
- نسبة تحقيق الشحنات.
- تارجت الكروت.
- تحقيق الكروت.
- نسبة تحقيق الكروت.
- النسبة الإجمالية.
- Progress Bar بألوان:
  - أخضر `>= 100%`.
  - أصفر من `80%` إلى `99%`.
  - أحمر أقل من `80%`.

## 4. الملفات التي تم تعديلها أو إضافتها

- `Areas/Pos/Sql/48_POS_SalesRepresentativesPerformanceDashboard.sql`
- `Areas/Pos/Models/SalesRepresentativesPerformanceModels.cs`
- `Areas/Pos/Data/PosSalesPerformanceRepository.cs`
- `Areas/Pos/Controllers/SalesRepresentativesPerformanceController.cs`
- `Areas/Pos/Views/SalesRepresentativesPerformance/Index.cshtml`
- `Areas/Pos/Views/SalesRepresentativesPerformance/_SalesTargetAchievementCard.cshtml`
- `Areas/Pos/Views/PosDashboard/_Sidebar.cshtml`
- `Areas/Pos/AI_Docs/SalesRepresentativesPerformanceDashboard.md`

## 5. طريقة الاختبار

1. تشغيل سكريبت SQL:
   - `Areas/Pos/Sql/48_POS_SalesRepresentativesPerformanceDashboard.sql`
2. فتح التقرير:
   - `/Pos/SalesRepresentativesPerformance/Index`
3. اختبار الفلاتر:
   - فترة بها عمليات POS فعلية.
   - فرع محدد.
   - مندوب محدد.
   - نوع خدمة منفرد مثل `cash-in` أو `cash-out`.
4. إدخال تارجت شهري للشحنات والكروت للتحقق من:
   - نسب التحقيق.
   - المطلوب يوميًا.
   - Projection.
   - ألوان الحالة.
5. تجربة:
   - زر `Export Excel`.
   - زر الطباعة.
6. التأكد أن التقرير Read Only:
   - Stored Procedure يستخدم SELECT فقط.
   - لا توجد INSERT/UPDATE/DELETE على بيانات POS.
 

## 6. شاشة إدارة تارجت المناديب

تمت إضافة شاشة صغيرة لإدارة التارجت من داخل POS:

- الرابط: `/Pos/SalesTargets/Index`
- الصلاحية: Admin / Full Access فقط.
- تدعم اختيار الفترة، الفرع، كل المناديب أو مجموعة محددة، تارجت الشحنات الشهري، تارجت الكروت الشهري، وعدد أيام العمل.

مصدر البيانات الجديد:

- جدول: `dbo.POS_SalesRepresentativeTargets`
- إجراءات:
  - `dbo.usp_POS_SalesTargets_List`
  - `dbo.usp_POS_SalesTargets_Save`
  - `dbo.usp_POS_SalesTargets_Deactivate`
- سكريبت: `Areas/Pos/Sql/49_POS_SalesRepresentativeTargets.sql`

قواعد التشغيل:

- عند حفظ تارجت جديد، يتم إيقاف التارجت النشط المتداخل لنفس النطاق بدل الحذف.
- تارجت المندوب المحدد يأخذ أولوية أعلى من تارجت "كل المناديب".
- تارجت الفرع المحدد يأخذ أولوية أعلى من تارجت "كل الفروع".
- شاشة ملخص اليوم في فاتورة البيع تقرأ من `POS_SalesRepresentativeTargets` أولًا، ثم ترجع لإعدادات `Web.config` القديمة كـ fallback فقط.
- تقرير أداء المناديب يقرأ التارجت المحفوظ في الجدول الجديد إذا لم يتم تمرير تارجت يدوي من فلتر التقرير.

ملفات إضافية ضمن هذا التحديث:

- `Areas/Pos/Controllers/SalesTargetsController.cs`
- `Areas/Pos/Views/SalesTargets/Index.cshtml`
- `Areas/Pos/Sql/49_POS_SalesRepresentativeTargets.sql`
- تحديث `Areas/Pos/Data/PosSqlRepository.cs` لقراءة تارجت ملخص اليوم من قاعدة البيانات.
- تحديث `Areas/Pos/Sql/48_POS_SalesRepresentativesPerformanceDashboard.sql` لقراءة التارجت المحفوظ في التقرير.

اختبارات تمت:

- تطبيق `49_POS_SalesRepresentativeTargets.sql` على قاعدة `Cash`.
- إعادة تطبيق `48_POS_SalesRepresentativesPerformanceDashboard.sql` بعد ربطه بجدول التارجت.
- Smoke test للإجراءات:
  - `usp_POS_SalesTargets_List` يعمل ويرجع 0 صف عند عدم وجود تارجت نشط.
  - `usp_POS_SalesRepresentativesPerformanceDashboard` يرجع 5 مناديب لفترة مايو 2026.
- Build للمشروع باستخدام Visual Studio 2022 Community MSBuild نجح.
