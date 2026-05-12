# توثيق نقل/إعادة هيكلة شاشة `FrmDefinCompItem`

## ملخص المنطق المنقول من VB6

- جدول رأس المستند: `TblDefComItem`.
- تفاصيل المكونات: `TblDefComItemDet`.
- تفاصيل الصنف المنتج النهائي: `TblDefComItemData`.
- عند الحفظ تُنشأ قيود مخزون بنوعي الحركة:
  - `Transaction_Type = 27` (سند صرف)
  - `Transaction_Type = 28` (سند إضافة)
- في حالة إعادة الحفظ (`ReSave` في VB6):
  - يتم تنظيف القيود السابقة المرتبطة بالـ header أولًا عبر `InvoiceOrderNo / IDDefCIT`.
  - ثم يُعاد إنشاء قيود الصرف والإضافة للسند الحالي.
- محاسبة الحركة:
  - يتم إنشاء سجل في `Notes`.
  - ثم يُنشأ الزوج القياسي في `DOUBLE_ENTREY_VOUCHERS`.

## مكونات MainErp المستخدمة في الشاشة

- الكنترولر: `Areas/MainErp/Controllers/DefinCompItemController.cs`
- الريبو: `Areas/MainErp/Repositories/DefinCompItem/DefinCompItemRepository.cs`
- الخدمة: `Areas/MainErp/Services/DefinCompItem/DefinCompItemService.cs`
- نماذج العرض: `Areas/MainErp/ViewModels/DefinCompItem/DefinCompItemViewModels.cs`
- الواجهة: `Areas/MainErp/Views/DefinCompItem/Index.cshtml`
- JavaScript: `Areas/MainErp/Scripts/defin-comp-item.js`
- الستايل: `Areas/MainErp/Content/defin-comp-item.css`

## ملاحظات تنفيذية مهمة

- الإجراءات المساعدة في VB6 مثل `CreateNotes` و `CREATE_VOUCHER_GE*` و `UpdateTransactionsCost` غير متاحة بصيغة جاهزة في هذا النظام، لذلك تم اعتماد تنفيذ مباشر داخل الريبو الحالي على جداول `Transactions` و `Transaction_Details` و `Notes` و`DOUBLE_ENTREY_VOUCHERS`.
- الربط بين سند التجميع والقيود تتم عبر:
  - `InvoiceOrderNo = TblDefComItem.ID`
  - `IDDefCIT = TblDefComItem.ID`
- تم اعتماد نمط الربط القديم في إعادة الحفظ (Delete → Insert) مع الحماية من تعديل قيود معتمدة بدون إعادة بناء صريحة.
