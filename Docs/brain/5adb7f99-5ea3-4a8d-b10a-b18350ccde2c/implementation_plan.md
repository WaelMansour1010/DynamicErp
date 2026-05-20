# Implementation Plan - Project Extracts Double-Entry Posting Engine

We are implementing the **Double-Entry Accounting Posting Engine** within the save flow of project extracts (`CreateExtract` inside [ProjectRepository.cs](file:///f:/Source%20Code/DynamicErp/Areas/MainErp/Repositories/Projects/ProjectRepository.cs)). All operations strictly target the `Eng` database catalog.

## User Review Required

> [!IMPORTANT]
> The double-entry posting engine generates financial ledger rows in `dbo.Notes` and `dbo.DOUBLE_ENTREY_VOUCHERS` that must balance exactly: **Debits = Credits**.
> 
> We have designed a robust Posting Matrix based on the VB6 `projectsbill.frm` ledger rules:
> - **DEBITS (0)**:
>   - Customer / Under-Implementation Account: `NetValue`
>   - Performance Guarantee / Retention Account: `PerforValue` (if > 0)
>   - Advanced Payment Prepaid Account: `AdvancedPayment` (if > 0)
>   - General Discount Account: `GeneralDiscount` (if > 0)
> - **CREDITS (1)**:
>   - Project Revenue Account: `Total` (Executed Work)
>   - Sales VAT Account: `VatValue` (if > 0)
> 
> **Mathematical Balance**:
> `NetValue + PerforValue + AdvancedPayment + GeneralDiscount = Total + VatValue` (perfectly balanced).
> 
> To prevent database foreign key constraint violations (`FK_DOUBLE_ENTREY_VOUCHERS_ACCOUNTS`), we will query the leaf account codes directly from the database and apply robust fallback leaf accounts if the project or customer accounts are null.

## Proposed Changes

### Projects Repository

#### [MODIFY] [ProjectRepository.cs](file:///f:/Source%20Code/DynamicErp/Areas/MainErp/Repositories/Projects/ProjectRepository.cs)

We will integrate the ledger posting engine inside `CreateExtract(...)`:
1. **Query Project & Customer Accounts**: Inside the active transaction, query the project details (`UnderImp`, `AccountUnderImp`, `REVENUE_account`, `End_user_id`, `End_user_Account`) and customer account codes (`Account_Code`, `Account_CodeHi1`, `Account_CodeAss2`, `Account_VAT` from `dbo.TblCustemers`).
2. **Resolve Accounts & Fallbacks**: Apply fallback leaf accounts for general discounts, sales VAT, revenue, and customer accounts using leaf-level query fallbacks (`last_account = 1`).
3. **AppLock / Concurrency Protection**: Use `NextId(...)` (which applies `WITH (UPDLOCK, HOLDLOCK)`) inside the transaction to generate manual serial IDs:
   - `noteId` from `dbo.Notes.NoteID`
   - `voucherId` from `dbo.DOUBLE_ENTREY_VOUCHERS.Double_Entry_Vouchers_ID`
4. **Clean Deletion**: Safely delete any existing ledger records under the generated `noteId` to prevent duplication (for robust support of any future edits/re-saves).
5. **Insert Note Header**: Write the main transaction record to `dbo.Notes` with `NoteType = 5000` (represents project extracts).
6. **Insert Voucher Lines**: Sequentially write Debit/Credit ledger rows into `dbo.DOUBLE_ENTREY_VOUCHERS` with sequential line numbers (`DEV_ID_Line_No` = 1, 2, 3...) referencing `project_id`, `bill_id`, and `project_bill_no` to ensure correct integration with the details pages.
7. **Update Header Linkage**: Set `project_billl.note_id` to the generated `noteId` and update `NoteSerial` and `NoteSerial1` to link the transaction records.

## Verification Plan

### Automated Verification
We will run `msbuild` inside the MVC project to ensure everything builds successfully without any compile-time errors or missing namespace issues:
- Command: `msbuild MyERP.csproj /t:Build /p:Configuration=Debug`

### Manual Verification
Reviewing ledger balance inside database after extract save using visual inspection tools.
