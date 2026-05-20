# Property Migration Assessment - VB6 Property Databases to DynamicErp Web

Date: 2026-05-20  
Scope: Read-only assessment only. No migration execution. No database changes.  
Target project: `F:\Source Code\DynamicErp`  
Reference database: `Alromaizan`  
Legacy databases: `RSMDB`, `Adnan`

## 1. Executive Summary

Both `RSMDB` and `Adnan` are transferable to the main DynamicErp web property module, but **not by direct table copy**. The correct approach is a controlled transformation with accounting reconciliation gates.

Recommended strategy: **Hybrid: Historical Archive + Active Contracts Only + Opening Balance Style**.

| Database | Feasible? | Confidence | Risk | Recommended style |
|---|---:|---:|---|---|
| `RSMDB` | Yes, with transformation | 72% | High | Hybrid + opening balances + active contracts + archive |
| `Adnan` | Yes, with transformation | 78% | Medium-High | Hybrid + opening balances + active contracts + archive |

Key conclusion: master data and active contracts can be migrated safely after mapping, but historical vouchers, settlement, due rent, revenue proof, and journal entries are high-risk unless a full accounting rule matrix is approved.

## 2. Methodology And Safety

Only read operations were used:

- Schema inspection from `sys.tables`, `sys.columns`, `sys.key_constraints`, `sys.foreign_keys`, `sys.procedures`, `sys.views`
- `SELECT TOP` samples from candidate tables
- Aggregate diagnostic `SELECT` queries
- Code inspection in `F:\Source Code\DynamicErp`

No `UPDATE`, `DELETE`, `INSERT`, `ALTER`, or data-changing procedure execution was performed.

Generated evidence files:

- `Docs\PropertyMigrationAssessment_Artifacts_20260520\*_tables.csv`
- `Docs\PropertyMigrationAssessment_Artifacts_20260520\*_columns.csv`
- `Docs\PropertyMigrationAssessment_Artifacts_20260520\*_primary_keys.csv`
- `Docs\PropertyMigrationAssessment_Artifacts_20260520\*_foreign_keys.csv`
- `Docs\PropertyMigrationAssessment_Artifacts_20260520\*_procedures.csv`
- `Docs\PropertyMigrationAssessment_Artifacts_20260520\samples\*.csv`
- `Docs\PropertyMigrationAssessment_Artifacts_20260520\diagnostics_summary.csv`

## 3. Alromaizan Reference Architecture

`Alromaizan` is the target/reference architecture, but it currently has very little property transactional data:

| Table | Purpose | Rows |
|---|---|---:|
| `Property` | Property/building header | 1 |
| `PropertyDetail` | Actual rentable property units | 0 |
| `PropertyRenter` | Tenant/renter master | 1 |
| `PropertyOwner` | Owner master | 1 |
| `PropertyContract` | Contract header | 0 |
| `PropertyContractBatch` | Contract installment/batch rows | 0 |
| `CashReceiptVoucher` | Receipt voucher header | 0 |
| `CashReceiptVoucherPropertyContractBatch` | Receipt allocation to contract batches | 0 |
| `PropertyContractTermination` | Contract termination/settlement | 0 |
| `PropertyDueBatch` | Due rent/accrual document | 1 |
| `PropertyRevenueProof` | Revenue recognition document | 0 |
| `JournalEntry` / `JournalEntryDetail` | Accounting entries | 1 / 3 |
| `ChartOfAccount` | Chart of accounts | 189 |

Important target relationships:

| Relationship | Meaning |
|---|---|
| `Property.PropertyOwnerId -> PropertyOwner.Id` | Property owner |
| `PropertyDetail.MainDocId -> Property.Id` | Unit belongs to property |
| `PropertyContract.PropertyId -> Property.Id` | Contract property |
| `PropertyContract.PropertyRenterId -> PropertyRenter.Id` | Contract renter |
| `PropertyContract.PropertyUnitId -> PropertyDetail.Id` | Main rented unit |
| `PropertyContractBatch.MainDocId -> PropertyContract.Id` | Contract installment |
| `CashReceiptVoucher.PropertyContractId -> PropertyContract.Id` | Receipt linked to contract |
| `CashReceiptVoucherPropertyContractBatch.PropertyContractBatchId -> PropertyContractBatch.Id` | Paid batch |
| `PropertyDueBatchDetail.PropertyContractBatchId -> PropertyContractBatch.Id` | Accrued batch |
| `PropertyRevenueProofDetail.PropertyContractBatchId -> PropertyContractBatch.Id` | Revenue-recognized batch |
| `PropertyContractTermination.PropertyContractId -> PropertyContract.Id` | Settlement contract |
| `JournalEntryDetail.AccountId -> ChartOfAccount.Id` | Accounting posting account |

Operational sequence in the new web system:

1. Create owner/renter and link to accounts where accounting is required.
2. Create property and units in `Property` / `PropertyDetail`.
3. Create contract in `PropertyContract`.
4. Generate/import batches in `PropertyContractBatch`.
5. Allocate receipts through `CashReceiptVoucherPropertyContractBatch`.
6. Recognize dues through `PropertyDueBatch`.
7. Recognize revenue through `PropertyRevenueProof`.
8. End contracts through `PropertyContractTermination`, which also returns units to available status.
9. Accounting depends heavily on `Department` account mappings such as due rent, rent revenue, VAT, owner, renter, insurance, commission, and property expense accounts.

DynamicErp components inspected:

| Layer | Files / procedures |
|---|---|
| Controllers | `PropertyController`, `PropertyContractController`, `PropertyDueBatchController`, `PropertyRevenueProofController`, `PropertyContractTerminationController`, `PropertyRenterController`, `PropertyOwnerController`, `CashReceiptVoucherController`, `CashIssueVoucherController`, `JournalEntriesController` |
| Views | `Views\Property`, `Views\PropertyContract`, `Views\PropertyContractTermination`, `Views\PropertyDueBatch`, `Views\PropertyRevenueProof`, `Views\CashReceiptVoucher`, `Views\CashIssueVoucher`, `Views\Report\Property*.cshtml` |
| Models | `Property`, `PropertyDetail`, `PropertyRenter`, `PropertyOwner`, `PropertyContract`, `PropertyContractBatch`, `CashReceiptVoucher`, `CashReceiptVoucherPropertyContractBatch`, `PropertyDueBatch`, `PropertyRevenueProof`, `PropertyContractTermination`, `JournalEntry`, `JournalEntryDetail`, `ChartOfAccount`, `Department` |
| Stored procedures | `PropertyContract_Insert`, `PropertyContract_Update`, `PropertyContractTerminate_Insert`, `PropertyContractTerminate_Update`, `PropertyDueBatch_Insert`, `PropertyDueBatch_Update`, `PropertyRevenueProof_Insert`, `PropertyRevenueProof_Update`, `CashReceiptVoucher_Insert`, `CashReceiptVoucher_Update`, `GetPropertyDueBatchDetails`, `GetPropertyRevenueProofDetails`, `sp_GetPropertyPaymentSummary`, `sp_GetPropertyReport` |

## 4. RSMDB Analysis

Main legacy tables discovered:

| Business area | Table | Key | Rows | Migration meaning |
|---|---|---|---:|---|
| Properties | `TblAqar` | `Aqarid` | 16 | Maps to `Property` |
| Units | `TblAqarDetai` | `Id` | 629 | Maps to `PropertyDetail` |
| Unit types | `TblAkarUnit` | `id` | 18 | Maps to `PropertyUnitType` |
| Renters/customers | `TblCustemers` | `CusID` | 969 | Maps to `PropertyRenter`; account via `Account_Code` |
| Contracts | `TblContract` | `ContNo` | 2,813 | Maps to `PropertyContract` |
| Installments | `TblContractInstallments` | `id` | 7,478 | Maps to `PropertyContractBatch`; payment state must transform |
| Old installments | `TblContractInstallmentsHist`, `TblContractInstallmentsOld` | `id` | 803 / 1,346 | Archive or special history |
| Payment allocations | `tblContractInsAllocations`, `tblContractInsAllocationsDetails` | `transID` / `id` | 227 / 4,438 | Needs interpretation before native migration |
| Vouchers | `Notes` | `NoteID` | 39,257 | Multi-purpose receipt/payment table |
| Voucher details | `TblNotesSales` | `ID` | 8,835 | Detail/value rows linked to notes |
| Settlement | `TblFiterWaiver` | `ID` | 574 | Maps to termination only with transformation |
| Settlement details | `TblFiterWaiverDe`, `TblFiterWaiverDet2` | `ID` | 102 / 1,585 | Settlement component/damage candidates |
| Commissions | `TblAqarCommissions` | `ID` | 10,530 | Commission tracking |
| Unit movement | `TblUnitNoInformation` | `ID` | 12,199 | Unit status/history log |
| Accounts | `ACCOUNTS` | `Account_ID`; code `Account_Code` | 2,550 | Maps to `ChartOfAccount` by code |
| Cash boxes | `TblBoxesData` | `BoxID` | 10 | Maps to cash boxes/accounts |

Diagnostics:

| Metric | Value |
|---|---:|
| Contracts missing property/unit/renter | 0 / 0 / 0 |
| Installments missing contract | 0 |
| Installments with negative remain | 44 |
| Installments paid over value | 45 |
| Contracts invalid dates | 0 |
| Duplicate contract numbers | 0 |
| Duplicate unit per property/unitno | 5 |
| Settlements missing contract | 1 |
| Customers without account code | 3 |
| Customers account code missing in `ACCOUNTS` | 0 |

RSMDB risk summary:

- Good master-contract referential consistency.
- High accounting risk because `Notes` is broad and multi-purpose.
- `TblContractInstallments` has real reconciliation anomalies: negative remaining and overpaid rows.
- Settlement requires special handling because `TblFiterWaiver` stores insurance, renter refund/payable values, remaining rent/water/service, VAT, daily calculations, and net result.

## 5. Adnan Analysis

Main legacy tables discovered:

| Business area | Table | Key | Rows | Migration meaning |
|---|---|---|---:|---|
| Properties | `TblAqar` | `Aqarid` | 28 | Maps to `Property` |
| Units | `TblAqarDetai` | `Id` | 367 | Maps to `PropertyDetail` |
| Unit types | `TblAkarUnit` | `id` | 13 | Maps to `PropertyUnitType` |
| Renters/customers | `TblCustemers` | `CusID` | 817 | Maps to `PropertyRenter`; account via `Account_Code` |
| Contracts | `TblContract` | `ContNo` | 2,127 | Maps to `PropertyContract` |
| Installments | `TblContractInstallments` | `id` | 7,878 | Maps to `PropertyContractBatch` |
| Old installments | `TblContractInstallmentsHist`, `TblContractInstallmentsOld` | `id` | 127 / 245 | Archive or special history |
| Payment allocations | `tblContractInsAllocations`, `tblContractInsAllocationsDetails` | `transID` / `id` | 259 / 5,060 | Needs interpretation |
| Vouchers | `Notes` | `NoteID` | 18,927 | Multi-purpose receipt/payment table |
| Voucher details | `TblNotesSales` | `ID` | 8,328 | Detail/value rows |
| Settlement | `TblFiterWaiver` | `ID` | 470 | Maps to termination only with transformation |
| Settlement details | `TblFiterWaiverDe`, `TblFiterWaiverDet2` | `ID` | 252 / 939 | Settlement component/damage candidates |
| Commissions | `TblAqarCommissions` | `ID` | 9,869 | Commission tracking |
| Unit movement | `TblUnitNoInformation` | `ID` | 10,792 | Unit status/history log |
| Accounts | `ACCOUNTS` | `Account_ID`; code `Account_Code` | 2,123 | Maps to `ChartOfAccount` |
| Cash boxes | `TblBoxesData` | `BoxID` | 6 | Maps to cash boxes/accounts |

Diagnostics:

| Metric | Value |
|---|---:|
| Contracts missing property/unit/renter | 0 / 0 / 0 |
| Installments missing contract | 0 |
| Installments with negative remain | 0 |
| Installments paid over value | 0 |
| Contracts invalid dates | 0 |
| Duplicate contract numbers | 0 |
| Duplicate unit per property/unitno | 57 |
| Settlements missing contract | 0 |
| Customers without account code | 2 |
| Customers account code missing in `ACCOUNTS` | 0 |

Adnan risk summary:

- Cleaner installment balances than RSMDB in tested diagnostics.
- Higher unit identity risk: 57 duplicate `(Aqarid, unitno)` combinations.
- `Notes.NoteType = 4` is the dominant property-linked receipt population.
- Settlement and vouchers still require a classification/sign matrix.

## 6. Comparison Between Old And New

| Topic | DynamicErp / Alromaizan | VB6 `RSMDB` / `Adnan` | Migration implication |
|---|---|---|---|
| Property | `Property` | `TblAqar` | Mostly transformable |
| Unit | `PropertyDetail` | `TblAqarDetai` | Transformable; use old `Id`, not display unit number |
| Renter | `PropertyRenter.AccountId` | `TblCustemers.Account_Code` | Requires account mapping first |
| Contract | `PropertyContract` | `TblContract` | Transformable with lookup/date/value rules |
| Installment | `PropertyContractBatch` | `TblContractInstallments` | Transformable, but paid/remain is not direct |
| Receipt | `CashReceiptVoucher` + allocation table | `Notes` + details | High-risk transform by `NoteType` |
| Settlement | `PropertyContractTermination` + details/damages | `TblFiterWaiver` + details | High-risk transform |
| Accrual/revenue | `PropertyDueBatch`, `PropertyRevenueProof` | No exact single equivalent | Prefer recompute/opening balance |
| Journal | `JournalEntry`, `JournalEntryDetail` | `DOUBLE_ENTREY_VOUCHERS`, `Notes`, `ACCOUNTS` | Do not direct-copy without reconciliation |
| Accounts | integer FK to `ChartOfAccount.Id` | string `Account_Code` | Requires code-to-id mapping |

## 7. Core Mapping

| Old DB | Old Table | Old Field | Business Meaning | New Table | New Field | Mapping Type | Risk | Notes |
|---|---|---|---|---|---|---|---|---|
| Both | `TblAqar` | `Aqarid` | Old property id | `Property` | external cross-ref | Missing In New | Medium | Need migration reference |
| Both | `TblAqar` | `aqarNo` | Property code | `Property` | `Code` | Transform Required | Medium | Blank values need fallback |
| Both | `TblAqar` | `aqarname` | Property name | `Property` | `ArName` | Direct | Low | Required |
| Both | `TblAqar` | `ownerid` | Owner | `Property` | `PropertyOwnerId` | Transform Required | High | Owner source must be confirmed |
| Both | `TblAqar` | `BranchId` | Branch/department | `Property` | `DepartmentId` | Transform Required | High | Drives accounting |
| Both | `TblAqarDetai` | `Id` | Old unit id | `PropertyDetail` | external cross-ref | Missing In New | Medium | Required for traceability |
| Both | `TblAqarDetai` | `Aqarid` | Property id | `PropertyDetail` | `MainDocId` | Transform Required | Low | Map via property cross-ref |
| Both | `TblAqarDetai` | `unitno` | Unit number | `PropertyDetail` | `PropertyUnitNo` | Direct | High | Duplicates exist |
| Both | `TblAqarDetai` | `unittype` | Unit type | `PropertyDetail` | `PropertyUnitTypeId` | Transform Required | Medium | Map via `TblAkarUnit` |
| Both | `TblAqarDetai` | `Status` | Unit status | `PropertyDetail` | `StatusId` | Derived | High | Derive from active/ended contracts |
| Both | `TblCustemers` | `CusID` | Old renter id | `PropertyRenter` | external cross-ref | Missing In New | Medium | Required |
| Both | `TblCustemers` | `Account_Code` | Renter account | `PropertyRenter` | `AccountId` | Transform Required | Critical | Map to `ChartOfAccount.Id` |
| Both | `ACCOUNTS` | `Account_Code` | Account code | `ChartOfAccount` | `Code` | Transform Required | Critical | Hierarchy reconciliation needed |
| Both | `TblContract` | `ContNo` | Contract id/no | `PropertyContract` | `DocumentNumber` or legacy ref | Transform Required | High | Preserve original separately |
| Both | `TblContract` | `ContDate` | Contract date | `PropertyContract` | `VoucherDate` | Direct | Low | Date conversion |
| Both | `TblContract` | `Iqar` | Property | `PropertyContract` | `PropertyId` | Transform Required | Low | Cross-ref |
| Both | `TblContract` | `CusID` | Renter | `PropertyContract` | `PropertyRenterId` | Transform Required | Low | Cross-ref |
| Both | `TblContract` | `UnitNo` | Unit | `PropertyContract` | `PropertyUnitId` | Transform Required | Medium | Appears to reference `TblAqarDetai.Id` |
| Both | `TblContract` | `StrDate` / `EndDate` | Contract period | `PropertyContract` | `ContractStartDate` / `ContractEndDate` | Direct | Low | Validate cutover |
| Both | `TblContract` | `TotalContract` | Contract total | `PropertyContract` | `RentValue` / `NetTotal` / `TotalAfterTaxes` | Ambiguous | High | Component split required |
| Both | `TblContract` | `FATYou`, `FATValue` | VAT percent/value | `PropertyContract` | `VATPercentage`, `VATValue` | Transform Required | High | Validate scale/components |
| Both | `TblContractInstallments` | `id` | Installment id | `PropertyContractBatch` | external cross-ref | Missing In New | Medium | Required |
| Both | `TblContractInstallments` | `ContNo` | Contract | `PropertyContractBatch` | `MainDocId` | Transform Required | Low | Cross-ref |
| Both | `TblContractInstallments` | `InstallNo` | Batch number | `PropertyContractBatch` | `BatchNo` | Direct | Low | Numeric |
| Both | `TblContractInstallments` | `Installdate` | Due date | `PropertyContractBatch` | `BatchDate` | Direct | Low | Date conversion |
| Both | `TblContractInstallments` | `RentValue` | Rent portion | `PropertyContractBatch` | `BatchRentValue` | Direct | Low | Numeric |
| Both | `TblContractInstallments` | `Water`, `Electric` | Utility portions | `PropertyContractBatch` | `BatchWaterValue`, `BatchElectricityValue` | Direct | Low | Numeric |
| Both | `TblContractInstallments` | `Commissions`, `Insurance` | Commission/insurance | `PropertyContractBatch` | `BatchCommissionValue`, `BatchInsuranceValue` | Transform Required | Medium | Account treatment required |
| Both | `TblContractInstallments` | `installValue` | Batch total | `PropertyContractBatch` | `BatchTotal` | Direct/Derived | Medium | Validate against components |
| Both | `TblContractInstallments` | `payed`, `Remains` | Paid/remain | `CashReceiptVoucherPropertyContractBatch` | `Paid`, `Remain` | Derived | High | Not batch header in new system |
| Both | `Notes` | `NoteID`, `NoteType` | Voucher id/type | `CashReceiptVoucher` / `CashIssueVoucher` / archive | multiple | Ambiguous | Critical | Needs `NoteType` matrix |
| Both | `Notes` | `Note_Value` | Voucher amount | voucher amount | `MoneyAmount` | Transform Required | High | Depends receipt/payment sign |
| Both | `Notes` | `ContNo`, `CusID` | Contract/renter link | `CashReceiptVoucher` | `PropertyContractId`, `RenterId` | Transform Required | High | Cross-ref |
| Both | `TblFiterWaiver` | `ID` | Settlement id | `PropertyContractTermination` | external cross-ref | Missing In New | Medium | Required |
| Both | `TblFiterWaiver` | `ContNo`, `RenterID`, `BulidID` | Contract/renter/property | `PropertyContractTermination` | FK fields | Transform Required | Medium | Cross-ref |
| Both | `TblFiterWaiver` | `Insurance`, `ForRenter`, `OFRenter`, `net` | Settlement result | `PropertyContractTermination` / journal | Transform Required | Critical | Sign/rule approval required |

## 8. Business Logic Impact

Will work after proper transformation:

- Property list/search
- Unit list and unit selection
- Renter/owner master screens
- Contract index/edit display
- Basic reports based on property, unit, contract, and batch data

May break after naive migration:

- Contract edit if `PropertyUnitId` does not point to `PropertyDetail.Id`
- Receipt screen if paid/remain is copied without `CashReceiptVoucherPropertyContractBatch`
- Termination if `TblFiterWaiver` signs/components are not transformed correctly
- Due/revenue proof if historical accruals are imported and then posted again
- Journal reports if old `Notes` are imported without balanced `JournalEntryDetail`

Needs special migration logic:

- Account mapping by `Account_Code`
- Department account mappings before accounting documents
- Unit status derivation
- Active vs ended contract classification
- Receipt allocation from notes/installments
- Settlement sign and detail classification
- VAT component reconstruction
- Opening balances

## 9. Critical Risks

| Risk | Severity | Comment |
|---|---|---|
| Double-posting history and opening balances | Critical | Must choose one strategy |
| Wrong renter/customer account mapping | Critical | Affects balances and statements |
| Missing department account mapping | Critical | New procedures depend on it |
| `Notes` classification ambiguity | Critical | Same table stores many voucher types |
| Settlement sign inversion | High | `net` can represent payable/refund direction |
| RSMDB negative/overpaid installments | High | Must be reconciled |
| Adnan duplicate unit numbers | High | Use source unit id, not display number |
| VAT mismatch | High | Legacy VAT fields are inconsistent/partial |

## 10. Recommendations

1. Do not attempt full native historical migration now.
2. Approve active-contract migration plus archive strategy first.
3. Build account-code to `ChartOfAccount.Id` mapping before renters/owners/contracts.
4. Build lookup mappings for departments, property types, unit types, rent types, cash boxes, banks, and payment methods.
5. Preserve legacy ids/document numbers through a migration cross-reference design.
6. Reconcile RSMDB negative remain and overpaid installments before any dry run.
7. Resolve Adnan duplicate unit display strategy before import.
8. Keep old `Notes`, old journals, and old settlement details as read-only archive unless a separate full accounting conversion is approved.

## 11. Proposed Migration Plan

1. Decision phase: cutover date, history strategy, opening balance policy, NoteType policy.
2. Read-only profiling: run diagnostics script and export anomaly lists.
3. Master dry run: accounts, departments, cash boxes, owners, renters, properties, units.
4. Active contract dry run: contracts and future/outstanding batches.
5. Balance dry run: calculate outstanding by renter/contract and compare to approved trial balance.
6. Optional archive: expose old details read-only with cross-reference to new records.
7. UAT: validate screens, reports, balances, and settlement samples.
8. Only after approval: execute real migration in a separate migration task.

## 12. Open Questions

1. Should historical `Notes` become native web vouchers, or stay archive-only?
2. What is the cutover date?
3. Should ended contracts be migrated as terminated contracts or archived only?
4. Where should old numbers be preserved: `DocumentNumber`, `Notes`, or dedicated reference tables?
5. If the same renter/account appears in both `RSMDB` and `Adnan`, which source wins?
6. What is the authoritative source for owners behind `TblAqar.ownerid`?
7. Should balances be reconciled against `ACCOUNTS`, `DOUBLE_ENTREY_VOUCHERS`, `Notes`, or an external trial balance?
8. Should VAT be reconstructed by component or preserved as legacy totals?

## 13. Final Recommendation

Start with `Adnan` as the pilot because its tested installment balances are cleaner. Then apply the same pipeline to `RSMDB` after resolving installment anomalies.

Best production approach:

1. Migrate/match accounts and master data.
2. Migrate active contracts and outstanding/future batches.
3. Carry approved balances as opening balances.
4. Keep historical VB6 vouchers and settlements as archive.
5. Move to full native historical voucher conversion only if a separate accounting rules project is approved.

## 14. VB6 Source Code Addendum - RealEstateMnag

After the first database/schema assessment, the old VB6 property module was also inspected at:

`F:\Source Code\SatriahMain\Frm\New frm\RealEstateMnag`

This source-code review is important because it confirms that the old property system cannot be understood safely from table names alone. Several critical values are calculated, rebuilt, deleted, or posted dynamically by the VB6 screens.

### 14.1 Main VB6 Forms And Business Role

| VB6 Form | Business Role | Main Tables / Evidence |
|---|---|---|
| `RSAkar.frm` | Property/building master | `TblAqar` |
| `RsApartement.frm`, `RSRoom.frm`, `RSStores.frm`, `RSTradingCenter.frm`, `RSWorkshop.frm`, `rsvila.frm`, `RsLand.frm` | Unit and property-type entry screens | `TblAqarDetai`, unit status fields |
| `RsCustomers.frm` | Renters/customers and linked accounts | `TblCustemers`, `ACCOUNTS` |
| `RSOwner.frm` | Owner master | owner/account relations |
| `RsContarct.frm` | Main rent contract creation/editing, installment generation, unit reservation, accounting voucher generation | `TblContract`, `TblContractInstallments`, `TblIqrMerg`, `TblUnitNoInformation`, `Notes`, `DOUBLE_ENTREY_VOUCHERS` |
| `RSContractInstallments.frm` | Contract installment display/report support | `TblContractInstallments` |
| `RsCashing.frm` | Receipt voucher for rents/contract payments | `Notes.NoteType = 4`, `DOUBLE_ENTREY_VOUCHERS` |
| `FrmCashing1.frm` | Receipt helper/alternate receipt workflow | `Notes`, customer/account lookups |
| `FrmPayments2.frm` | Payment voucher, settlement payment, earnest/commission/rent-related payments | `Notes.NoteType = 5`, `DOUBLE_ENTREY_VOUCHERS` |
| `FrmWaiverSettlement.frm` | Contract settlement/termination/waiver | `TblFiterWaiver`, `TblFiterWaiverDe`, `TblFiterWaiverDet2`, `Notes.NoteType = -1`, `TblUnitNoInformation` |
| `FrmSanadatOFContract.frm` | Contract voucher inquiry, receipts and payments by contract | `Notes.NoteType = 4`, `Notes.NoteType = 5`, `Notes.ContNo` |
| `FrmSearchUnitEmpty.frm` | Empty unit search | `TblAqarDetai.Status` |
| `FrmIqarUnitNo.frm`, `FrmUnitInfoReports.frm` | Unit history/information reports | `TblUnitNoInformation` |
| `FrmOtheExpensAqar.frm`, `FrmExpenses301.frm` | Property expense workflows | `Notes`, account/payment tables |
| `frmAqarInstallAlert.frm`, `FrmRentsOwendReports.frm`, `FrmExpiredContract.frm`, `ReRentAlarm.frm`, `FrmContractReport.frm`, `FrmAqarReport.frm` | Alerts and property reports | contract/installment/report queries |

### 14.2 Confirmed Legacy Operational Flow

1. Master data is created first: properties, units, owners, renters, accounts, banks/cash boxes, branches, and supporting codes.
2. Contract creation is handled mainly by `RsContarct.frm`.
3. Contract installments are written to `TblContractInstallments`.
4. Multi-unit contracts are supported through `TblIqrMerg`.
5. Unit status/history is maintained through `TblAqarDetai` and `TblUnitNoInformation`.
6. Receipt vouchers are stored in `Notes` with `NoteType = 4`.
7. Payment vouchers are stored in `Notes` with `NoteType = 5`.
8. Contract/accounting accrual notes are generated from contract logic with `NoteType = 60`.
9. VAT or installment-related accounting notes can be generated with `NoteType = 9088`.
10. Settlement/waiver/termination creates records in `TblFiterWaiver` and writes settlement accounting notes with `NoteType = -1`.
11. The settlement screen releases the unit by updating the old unit status, clears active contract references, and sets `TblContract.EndContract = 1`.

### 14.3 Legacy NoteType Meanings Confirmed From Code

| NoteType | Meaning From VB6 Code | Migration Impact |
|---|---|---|
| `4` | Receipt voucher, especially rent/contract collection | Candidate for mapping to `CashReceiptVoucher`, but allocation must be reconstructed |
| `5` | Payment voucher, including settlement and property-related outgoing payments | Candidate for mapping to payment voucher logic, but `CashingType` is mandatory to classify |
| `60` | Contract/accounting accrual voucher generated by contract creation | High risk for native conversion because it may duplicate opening balances or due batches |
| `9088` | VAT/contract installment-related accounting note | High risk; must be reconciled with VAT fields before native migration |
| `-1` | Settlement/waiver/termination accounting note | Must be transformed carefully to `PropertyContractTermination` and accounting impact |
| `2000` | Auxiliary receipt-linked note deleted with receipt workflows | Ambiguous; should be profiled before any conversion |

### 14.4 Contract Logic Findings From `RsContarct.frm`

`RsContarct.frm` confirms that the legacy contract process is not a simple insert into `TblContract`.

Key findings:

- Contract numbers use `ContNo`, while displayed/manual contract coding can use `NoteSerial1`.
- The screen rebuilds child records during edit: installments, merged units, allocation details, contract sales rows, and voucher rows can be deleted and regenerated.
- Installments include separate components: rent, commission, insurance, water, electricity, telephone/internet, service/other amounts, VAT, paid values, and remaining values.
- `TblContractInstallments.payed` and `TblContractInstallments.Remains` should not be treated as fully authoritative. The code recalculates paid/remain using payment allocation logic such as `getinsttPayedTocontract(...)`.
- The screen validates that installment totals equal the contract financial total after discounts, commissions, insurance, utilities, service amounts, non-paid values, and VAT.
- Contract creation can generate accounting vouchers through `createVoucher()` using `NoteType = 60`.
- VAT/extra installment accounting can be generated through `createVoucher2()` using `NoteType = 9088`.
- Some contracts can be configured not to create journal entries and instead rely on opening-balance style records.

Migration conclusion:

The safest migration path is to treat old installment paid/remain fields as reference values, then recompute outstanding balances from receipts, allocations, and approved financial reconciliation. Copying `payed` and `Remains` directly into `PropertyContractBatch.PaidAmount` and `RemainAmount` is not safe without reconciliation.

### 14.5 Receipt Logic Findings From `RsCashing.frm`

The receipt screen writes to `Notes` using `NoteType = 4`.

Key findings:

- `NoteCashingType = 0` represents cash-box payment.
- `NoteCashingType = 1` represents bank/cheque workflow and uses `BankID`, `ChqueNum`, and `DueDate`.
- Receipt numbering is generated from `sanad_numbering` and existing `Notes` rows by date/month/year configuration.
- If complete accounting is enabled, the screen creates debit/credit rows in `DOUBLE_ENTREY_VOUCHERS`, linked by `Notes_ID`.
- The screen can call allocation logic such as `FIFO_FUNCTION(CusID)` or `Distribute_to_bills(...)` after saving.
- Receipt deletion logic deletes related note/details/voucher records, which means historical state may not be append-only.

Migration conclusion:

Receipts can be migrated only after classifying `NoteType = 4` by `CashingType`, `NoteCashingType`, `ContNo`, `CusID`, and installment/allocation tables. Directly importing total receipt amounts without allocation rows will likely break paid/remain behavior in the DynamicErp web screens.

### 14.6 Payment Logic Findings From `FrmPayments2.frm`

The payment screen writes to `Notes` using `NoteType = 5`.

Key findings:

- The payment screen supports cash, cheque/bank, transfer, settlement payment, earnest receipt linkage, commission/rent/water/insurance fields, and opening-balance related payment fields.
- Settlement-related payment mode requires a settlement number and stores fields like `FilterID` and `FIlterTotal`.
- Property fields such as `IqarID2`, `TotalInsurances`, period fields, `RenterValue`, `ExpValue`, `OfficeValue`, and `TotalPayments` can affect interpretation.
- The screen generates accounting rows through dynamic account-code functions such as `get_account_code_branch(...)`.
- Customer account codes are pulled from `TblCustemers` and then posted to `DOUBLE_ENTREY_VOUCHERS`.

Migration conclusion:

`Notes.NoteType = 5` is not one uniform payment type. It must be split into business categories before migration. Some records may map to cash/payment vouchers, some to settlement accounting, and some should remain archive-only unless the accounting rule is explicitly approved.

### 14.7 Settlement And Termination Logic From `FrmWaiverSettlement.frm`

The settlement screen confirms that old settlement is a full business workflow, not only a status flag.

Key findings:

- Main settlement rows are stored in `TblFiterWaiver`.
- Detail rows are stored in `TblFiterWaiverDe` and `TblFiterWaiverDet2`.
- The screen creates settlement note rows with `NoteType = -1`.
- Settlement saves values such as insurance, net, renter receivable/payable direction, old rent, remaining rent, remaining commission, remaining water/service, bill price, VAT percentage, incomplete days, legal flags, and old/new renter/unit/property references.
- The screen updates unit status/history through `TblUnitNoInformation`.
- After settlement, it releases the main unit and any merged units, clears customer/contract references, and marks `TblContract.EndContract = 1`.
- Settlement accounting uses branch-specific account mapping functions and customer account codes.

Migration conclusion:

Settlement migration requires a dedicated transformation. Mapping `TblFiterWaiver` only to `PropertyContractTermination` header is insufficient. The migration must also transform financial components, damages/dues/refunds, unit release state, contract ended state, and accounting impact.

### 14.8 Updated Risks After VB6 Source Review

| Risk | Updated Severity | Why It Matters |
|---|---|---|
| Copying installment `payed`/`Remains` directly | Critical | VB6 recalculates paid/remain from allocation/payment logic |
| Misclassifying `Notes` rows | Critical | `Notes` stores receipts, payments, accruals, VAT, settlement, and auxiliary records |
| Double-posting accounting | Critical | Contract creation, receipts, payments, VAT, and settlement all create voucher rows |
| Settlement transformation | Critical | Settlement changes financial balance, contract end state, and unit availability |
| Unit status after migration | High | VB6 updates `TblAqarDetai`, merged units, and `TblUnitNoInformation` together |
| Account-code mapping | High | VB6 relies on dynamic branch account-code functions |
| Historical edit/delete behavior | High | Some child records and vouchers are rebuilt by screens, so old history may not be immutable |

### 14.9 Updated Migration Strategy After Source Review

The VB6 source review strengthens the recommendation to avoid a direct full native historical migration as the first step.

Recommended strategy remains:

1. Hybrid migration.
2. Opening-balance style accounting cutover.
3. Active contracts and outstanding/future installments migrated natively.
4. Historical VB6 receipts, payments, journals, and settlements kept as read-only archive at first.
5. Native historical conversion only after separate approval of accounting rules for `NoteType = 4`, `5`, `60`, `9088`, `-1`, and `2000`.

Updated confidence:

| Database | Transfer Feasibility | Confidence | Risk |
|---|---:|---:|---|
| `RSMDB` | Feasible with transformation, reconciliation, and archive strategy | 72% | High |
| `Adnan` | Feasible with transformation and archive strategy | 78% | Medium-High |

The code review improves confidence in understanding the old business logic, but it also confirms that financial migration risk is real. The highest-risk area is not master data; it is the combination of contracts, installment allocation, settlement, receipts/payments, and journal posting.
