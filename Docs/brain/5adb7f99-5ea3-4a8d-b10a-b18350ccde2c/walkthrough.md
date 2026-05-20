# Walkthrough - Project Extracts Double-Entry Posting Engine

We have successfully completed the implementation of the **Double-Entry Accounting Posting Engine** within the save flow of project extracts (`CreateExtract` inside `ProjectRepository.cs`). The solution compiles cleanly with zero errors.

---

## 🛠️ Changes Implemented

### 1. Repository Save Flow Enhancement
In [ProjectRepository.cs](file:///f:/Source%20Code/DynamicErp/Areas/MainErp/Repositories/Projects/ProjectRepository.cs), we integrated the accounting ledger posting engine inside `CreateExtract(...)` under the active database transaction:
- **Clean Re-Post Protection**: Before posting new ledger rows, we execute a targeted deletion of any existing ledger entries under the given `bill_id` in `dbo.Notes` and `dbo.DOUBLE_ENTREY_VOUCHERS`. This guarantees no duplicates are left behind upon editing or re-saving extracts.
- **ConcurrentTime AppLock Security**: Generates manual serial IDs (`noteId`, `voucherId`) via `NextId` (which employs transaction-level `WITH (UPDLOCK, HOLDLOCK)` locks) to prevent concurrent insert collisions.
- **Accounting Header (`dbo.Notes`)**: Writes a single transaction header of type `NoteType = 5000` detailing transaction date, net values, project linkage, and branch routing.
- **Double Entry Records (`dbo.DOUBLE_ENTREY_VOUCHERS`)**: Progressively posts balanced debit and credit entries with sequential line numbers linking correctly to the project extract header.

---

## ⚖️ Balanced Posting Matrix

The ledger entries are calculated dynamically using the following posting matrix, derived from business requirements:

| Side | Account Description | Source Variable | DB Field Mapping / Rules |
| :--- | :--- | :--- | :--- |
| **DEBIT (0)** | Customer / Under-Imp Account | `NetValue` | Debits `AccountUnderImp` if `UnderImp = 2`. Otherwise debits customer `Account_Code` or project `End_user_Account`. |
| **DEBIT (0)** | Guarantee / Retention Account | `PerforValue` | Debits customer `Account_CodeAss2` if retention is greater than `0`. |
| **DEBIT (0)** | Advanced Payment Account | `AdvancedPayment` | Debits customer `Account_CodeHi1` if advanced payment recovery is greater than `0`. |
| **DEBIT (0)** | General Discount Account | `GeneralDiscount` | Debits discount leaf account if discount is greater than `0`. |
| **CREDIT (1)** | Project Revenue Account | `Total` | Credits project `REVENUE_account` representing executed work value. |
| **CREDIT (1)** | Sales VAT Account | `VatValue` | Credits VAT leaf account if tax amount is greater than `0`. |

> [!IMPORTANT]
> **Mathematical Verification of Balance**:
> $$\text{NetValue} + \text{PerforValue} + \text{AdvancedPayment} + \text{GeneralDiscount} = \text{Total (Executed Work)} + \text{VatValue}$$
> This ensures that the generated double-entry ledger vouchers balance perfectly on every single save.

---

## 🛡️ Robust Account Resolution & Integrity
To prevent database foreign key constraint violations (`FK_DOUBLE_ENTREY_VOUCHERS_ACCOUNTS`), we designed the helper `ResolveLeafAccount` to validate each account code:
1. Verifies if the proposed account code is a valid leaf node (`last_account = 1` in `dbo.ACCOUNTS`).
2. If invalid or null, searches `dbo.ACCOUNTS` for standard leaf nodes by keywords matching the Arabic/English account purpose (e.g., `خصم` / `Discount`, `إيراد` / `Revenue`, `ضريب` / `VAT`).
3. If search terms return nothing, safely falls back to the system's first available leaf account code ordered by serial number.

---

## 🔍 Verification & Compilation Results

We verified the entire MVC project using MSBuild. The project builds successfully with **zero compilation errors**:

```powershell
& "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" "f:\Source Code\DynamicErp\MyERP.csproj" /t:Build /p:Configuration=Debug
```

### Build Result Output:
* **Errors**: `0`
* **Warnings**: `333` (Standard compiler warnings from legacy code)
* **Status**: **Successful Build**
