# تعليمات تطبيق تعديلات شاشة العقد - Property Contract

## نظرة عامة
تم تعديل شاشة العقد للسماح بتعديل الفرع والوحدة بعد الحفظ، مع التعامل التلقائي مع:
- تحديث السيريال عند تغيير الفرع
- إرجاع الوحدات القديمة إلى حالة "متاحة"
- تحديث الوحدات الجديدة إلى حالة "مشغولة"

---

## التعديلات المطبقة

### 1️⃣ تعديلات الـ View (تم تطبيقها بالفعل ✅)
**الملف**: `Views/PropertyContract/AddEdit.cshtml`

#### التعديلات:
1. **إلغاء قفل حقل الفرع** (السطر 1122-1123)
   ```javascript
   // تم تحويل السطر إلى تعليق
   //$("#DepartmentId").prop('disabled', true);
   ```

2. **إلغاء قفل حقل نوع الوحدة** (السطر 233)
   ```csharp
   @Html.DropDownListFor(model => model.PropertyUnitTypeId, null, null,
       htmlAttributes: new {@class = "form-control", required = "required" })
   ```

3. **إلغاء قفل حقل الوحدة** (السطر 241)
   ```csharp
   @Html.DropDownListFor(model => model.PropertyUnitId, null, null,
       htmlAttributes: new {@class = "form-control", required = "required" })
   ```

4. **تحديث السيريال عند تغيير الفرع** (السطر 1143-1147)
   ```javascript
   $('#DepartmentId').on('change', function () {
       GetCurrentTaxPercentage();
       // تحديث السيريال عند تغيير الفرع سواء في الإضافة أو التعديل
       SetDocNum();
   });
   ```

5. **إضافة تحذير عند تغيير الوحدة** (السطر 1266-1287)
   ```javascript
   $('#PropertyUnitId').on('change', function () {
       let mval = $("#PropertyUnitId").val();

       // تحذير المستخدم عند تغيير الوحدة في وضع التعديل
       if ($('#Id').val() && $('#Id').val() > 0) {
           var currentUnitText = $("#PropertyUnitId option:selected").text();
           if (!confirm('تنبيه: عند تغيير الوحدة سيتم إرجاع الوحدة القديمة إلى "متاحة" وتحديث الوحدة الجديدة "' + currentUnitText + '" إلى "مشغولة". هل تريد المتابعة؟')) {
               $(this).val($(this).data('previous-value')).trigger('change.select2');
               return false;
           }
       }

       $(this).data('previous-value', mval);
       // ... باقي الكود
   });
   ```

---

### 2️⃣ تعديلات الـ Stored Procedure (يحتاج تطبيق يدوي ⚠️)
**الملف**: `PropertyContract_Update_Fixed.sql`

#### الخطوات المطلوبة:

**الخطوة 1:** افتح SQL Server Management Studio

**الخطوة 2:** اتصل بقاعدة البيانات `MyErp`

**الخطوة 3:** افتح ملف `PropertyContract_Update_Fixed.sql`

**الخطوة 4:** نفذ الـ SQL Script بالكامل

#### ما الذي تم إضافته في الـ Stored Procedure:

##### أ) حفظ الوحدات القديمة (في بداية الـ Procedure)
```sql
--//-------- حفظ الوحدة القديمة لإرجاع حالتها إلى متاحة --------//--
DECLARE @OldPropertyUnitId INT
DECLARE @OldMergedUnits TABLE (PropertyUnitId INT)

-- حفظ الوحدة الأساسية القديمة
SELECT @OldPropertyUnitId = PropertyUnitId
FROM PropertyContract
WHERE Id = @Id

-- حفظ الوحدات المدمجة القديمة
INSERT INTO @OldMergedUnits (PropertyUnitId)
SELECT PropertyUnitId
FROM PropertyContractMergedUnit
WHERE PropertyContractId = @Id
```

##### ب) إرجاع الوحدات القديمة إلى متاحة (بعد تحديث MergedUnits)
```sql
--//-------- إرجاع الوحدات القديمة إلى متاحة (StatusId = 0) --------//--
-- إرجاع الوحدة الأساسية القديمة إلى متاحة (إذا تم تغييرها)
IF @OldPropertyUnitId IS NOT NULL AND @OldPropertyUnitId != @PropertyUnitId
BEGIN
    UPDATE PropertyDetail
    SET StatusId = 0
    WHERE Id = @OldPropertyUnitId
END

-- إرجاع الوحدات المدمجة القديمة التي تم إزالتها إلى متاحة
UPDATE PropertyDetail
SET StatusId = 0
WHERE Id IN (
    SELECT PropertyUnitId
    FROM @OldMergedUnits
    WHERE PropertyUnitId NOT IN (
        SELECT PropertyUnitId
        FROM PropertyContractMergedUnit
        WHERE PropertyContractId = @Id
    )
)
```

##### ج) تحديث الوحدات الجديدة إلى مشغولة (موجود بالفعل، تم الاحتفاظ به)
```sql
--//-------- تحديث الوحدات الجديدة إلى مشغولة (StatusId = 1) --------//--
UPDATE PropertyDetail
SET StatusId = 1
WHERE Id = @PropertyUnitId
   OR Id IN (
        SELECT PropertyUnitId
        FROM PropertyContractMergedUnit
        WHERE PropertyContractId = @Id
   );
```

---

## آلية العمل

### سيناريو 1: تغيير الفرع
1. المستخدم يختار فرع جديد من القائمة
2. يتم استدعاء دالة `SetDocNum()` تلقائياً
3. يتم إنشاء سيريال جديد مناسب للفرع الجديد
4. عند الحفظ، يتم تحديث العقد بالفرع والسيريال الجديد

### سيناريو 2: تغيير الوحدة
1. المستخدم يختار وحدة جديدة من القائمة
2. يظهر تحذير للمستخدم: "تنبيه: عند تغيير الوحدة سيتم إرجاع الوحدة القديمة إلى متاحة..."
3. إذا وافق المستخدم:
   - عند الحفظ، يقوم الـ Stored Procedure بـ:
     - إرجاع الوحدة القديمة إلى حالة "متاحة" (StatusId = 0)
     - تحديث الوحدة الجديدة إلى حالة "مشغولة" (StatusId = 1)
     - إرجاع أي وحدات مدمجة تم إزالتها إلى "متاحة"
     - تحديث الوحدات المدمجة الجديدة إلى "مشغولة"
4. إذا ألغى المستخدم، تعود القيمة للوحدة القديمة

---

## الاختبارات المقترحة

### اختبار 1: تغيير الفرع
- [ ] افتح عقد موجود
- [ ] غير الفرع
- [ ] تأكد من تحديث السيريال تلقائياً
- [ ] احفظ العقد
- [ ] تأكد من حفظ الفرع الجديد والسيريال الجديد

### اختبار 2: تغيير الوحدة الأساسية
- [ ] افتح عقد موجود (مثلاً الوحدة القديمة: 101)
- [ ] غير الوحدة إلى وحدة جديدة (مثلاً: 102)
- [ ] تأكد من ظهور رسالة التحذير
- [ ] احفظ العقد
- [ ] تحقق من حالة الوحدات:
  - الوحدة 101 → متاحة (StatusId = 0)
  - الوحدة 102 → مشغولة (StatusId = 1)

### اختبار 3: تغيير الوحدات المدمجة
- [ ] افتح عقد يحتوي على وحدات مدمجة
- [ ] أضف/احذف بعض الوحدات المدمجة
- [ ] احفظ العقد
- [ ] تحقق من:
  - الوحدات المدمجة الجديدة → مشغولة
  - الوحدات المدمجة المحذوفة → متاحة

### اختبار 4: إلغاء تغيير الوحدة
- [ ] افتح عقد موجود
- [ ] غير الوحدة
- [ ] عند ظهور رسالة التحذير، اختر "إلغاء"
- [ ] تأكد من عودة القيمة للوحدة القديمة

---

## ملاحظات مهمة ⚠️

1. **النسخ الاحتياطي**: قبل تطبيق تعديلات الـ Stored Procedure، احتفظ بنسخة احتياطية من الـ Procedure القديم

2. **حالات الوحدات**:
   - `StatusId = 0` → متاحة (Available)
   - `StatusId = 1` → مشغولة (Not Available / Occupied)

3. **الدفعات المربوطة بسند قبض**: عند وجود دفعات مربوطة بسند قبض، لن يتم حذف الدفعات القديمة (كما هو موجود في الكود الأصلي)

4. **القيود اليومية**: عند تغيير الفرع، سيتم تحديث القيد اليومي (إن وُجد) بحسابات الفرع الجديد

---

## استعلامات SQL مفيدة للتحقق

### التحقق من حالة الوحدات
```sql
-- عرض جميع الوحدات وحالتها
SELECT
    pd.Id,
    pd.PropertyUnitNo,
    pd.StatusId,
    CASE
        WHEN pd.StatusId = 0 THEN 'متاحة'
        WHEN pd.StatusId = 1 THEN 'مشغولة'
        ELSE 'غير معروف'
    END AS StatusName,
    pc.Id AS ContractId,
    pc.DocumentNumber AS ContractNumber
FROM PropertyDetail pd
LEFT JOIN PropertyContract pc ON pc.PropertyUnitId = pd.Id AND pc.IsDeleted = 0
WHERE pd.IsDeleted = 0
ORDER BY pd.Id DESC
```

### التحقق من الوحدات المدمجة في عقد معين
```sql
-- استبدل @ContractId برقم العقد
DECLARE @ContractId INT = 6349

SELECT
    pd.Id,
    pd.PropertyUnitNo,
    pd.StatusId,
    CASE
        WHEN pd.StatusId = 0 THEN 'متاحة'
        WHEN pd.StatusId = 1 THEN 'مشغولة'
    END AS StatusName
FROM PropertyContractMergedUnit pcmu
INNER JOIN PropertyDetail pd ON pd.Id = pcmu.PropertyUnitId
WHERE pcmu.PropertyContractId = @ContractId
```

---

## الدعم

في حالة وجود أي مشاكل:
1. تحقق من رسائل الخطأ في SQL Server
2. تحقق من Console في المتصفح (F12)
3. راجع الخطوات في هذا الملف

---

**تاريخ التطبيق**: 2026/01/01
**الإصدار**: 1.0
**الحالة**: جاهز للتطبيق ✅
