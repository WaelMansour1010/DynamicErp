# DefinCompItem Test Plan

## Functional checks

1. Open `MainErp/DefinCompItem/Index`.
2. Verify filters and result list load.
3. Create a new collection voucher.
4. Add at least one component row.
5. Add at least one finished-product row.
6. Save and verify:
   - `TblDefComItem`
   - `TblDefComItemDet`
   - `TblDefComItemData`
   - `Transactions`
   - `Transaction_Details`
   - `Notes`
   - `DOUBLE_ENTREY_VOUCHERS`
7. Re-open the voucher and confirm the linked transactions are shown.
8. Modify quantities and save again.
9. Confirm the previous linked transactions were removed before the new ones were generated.
10. Delete the voucher if no posted documents exist.
11. Search by serial and by date range.
12. Verify server-side permission blocks:
   - view
   - add
   - edit
   - delete

## Build checks

- Compile the MVC project.
- Verify the new controller, repository, service, viewmodels, JS, and CSS are included in the project file.
