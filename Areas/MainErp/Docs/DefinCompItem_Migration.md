# DefinCompItem Migration

This document summarizes the VB6 `FrmDefinCompItem` migration into `Areas/MainErp`.

## VB6 logic captured

- Header table: `TblDefComItem`
- Component rows: `TblDefComItemDet`
- Finished-product rows: `TblDefComItemData`
- Inventory vouchers created on save:
  - `Transaction_Type = 27` issue voucher
  - `Transaction_Type = 28` receipt voucher
- Re-save behavior:
  - Existing linked vouchers are removed first using the collection header link and the generated transactions are recreated.
- Accounting trace:
  - A note row is inserted in `Notes`
  - A double-entry pair is inserted in `DOUBLE_ENTREY_VOUCHERS`

## MainErp artifacts

- Controller: `Areas/MainErp/Controllers/DefinCompItemController.cs`
- Repository: `Areas/MainErp/Repositories/DefinCompItem/DefinCompItemRepository.cs`
- Service: `Areas/MainErp/Services/DefinCompItem/DefinCompItemService.cs`
- ViewModels: `Areas/MainErp/ViewModels/DefinCompItem/DefinCompItemViewModels.cs`
- View: `Areas/MainErp/Views/DefinCompItem/Index.cshtml`
- JS: `Areas/MainErp/Scripts/defin-comp-item.js`
- CSS: `Areas/MainErp/Content/defin-comp-item.css`

## Notes

- The old VB6 helper procedures `CreateNotes`, `CREATE_VOUCHER_GE`, `CREATE_VOUCHER_GE1`, and `UpdateTransactionsCost` were not present in the target database.
- The migration therefore writes the required journal and voucher rows directly, using the current MainErp schema and the current branch/store account codes.
- The transaction link uses `InvoiceOrderNo = TblDefComItem.ID` and `IDDefCIT = TblDefComItem.ID` so rebuild and cancel operations can safely locate the generated movements.
