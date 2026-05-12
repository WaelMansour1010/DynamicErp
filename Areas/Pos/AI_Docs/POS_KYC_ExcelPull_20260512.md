# POS KYC Excel Pull

Date: 2026-05-12

## Scope

Added a limited KYC section to the existing POS Excel Import screen to pull and preview customer KYC data from an Excel sheet. This does not save the customer automatically and does not create invoices.

## Excel Model Reviewed

File:

- `C:/Users/Wael/Downloads/kyc.xlsx`

Detected columns:

1. `تاريخ بيع الكارت`
2. `التوكين ( بالباركود سكان )`
3. `اسم العميل بالعربي بالكامل`
4. `اسم العميل بالكامل بالانجليزي`
5. `رقم القومي يكون مضبوط وصحيح`
6. `عنوان العميل بالكامل بالعربي`
7. `عنوان العميل بالكامل بالانجليزي`
8. `رقم الموبايل العميل مضبوط وصحيح`
9. `تاريخ ميلاد العميل`
10. `تاريخ اصدار البطاقة`
11. `تاريخ انتهاء البطاقة`
12. `كود الفرع`

## Files Changed

- `Areas/Pos/Controllers/ExcelImportController.cs`
- `Areas/Pos/Views/ExcelImport/Index.cshtml`
- `Areas/Pos/Models/PosExcelImportModels.cs`
- `Areas/Pos/Services/PosKycExcelParser.cs`
- `MyERP.csproj`

## Behavior

- Added section: `قسم KYC` inside `/Pos/ExcelImport/Index`.
- User selects `.xlsx` / `.xls`.
- Server parses the first worksheet and displays a preview grid.
- The preview shows token, customer name, national ID, mobile, branch code/name, and dates.
- A second step `حفظ وتفعيل عملاء KYC الجاهزين` saves rows through the existing repository KYC activation flow.
- The commit step requires the current POS user password.
- No invoice is created from this section.
- Existing duplicate/token/stock checks remain enforced by the current KYC save logic.

## Mapping

- Token -> `CardNo`, `CardId`, `VisaNumber`
- Arabic full name -> `Name`, `CustomerName`, Arabic name parts
- English full name -> `NameE`, English name parts
- National ID -> `Tet_NumPoket`
- Arabic address -> `Address`
- English address -> `EnglishName5`
- Mobile -> `PhoneNo2`, `Phone`
- Birth date -> `BirthDate`
- Issue date -> `CardDate`
- Expiry date -> `CardEndDate`
- Sale date -> `OrderDate`
- Branch code -> branch lookup by `TblBranchesData.branch_Code` through existing branch list

## Safety

- Requires existing Excel Import access for the import screen.
- Commit validates required KYC fields before save.
- Duplicate token inside the same uploaded KYC file is rejected.
- Each saved row is wrapped by the existing KYC save transaction and application lock.
- No SQL changes.
- No accounting, save, serial, or voucher logic changed.
- Existing KYC save remains the only operation that writes customer data.
- `AllScripts.sql` was not modified.

## Test Cases

1. Open `/Pos/ExcelImport/Index`.
2. Upload the provided `kyc.xlsx` in `قسم KYC`.
3. Confirm rows appear in the KYC preview grid.
4. Confirm token, name, national ID, phone, branch code/name, and dates are visible.
5. Enter POS user password and run `حفظ وتفعيل عملاء KYC الجاهزين`.
6. Confirm valid rows are imported and rejected rows show clear reasons.
7. Try with a non-Excel file and confirm it is rejected.
