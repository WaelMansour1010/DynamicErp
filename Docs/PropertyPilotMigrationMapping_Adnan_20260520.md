# Adnan Pilot Migration Mapping - 2026-05-20

Scope: strict active contracts only from `Adnan` as of `2026-05-20`, excluding contracts with settlement rows in `TblFiterWaiver` and excluding contracts marked `EndContract = 1`.

| Old Database | Old Table | Old Field | Business Meaning | New Table | New Field | Mapping Type | Risk Level | Notes |
|---|---|---|---|---|---|---|---|---|
| Adnan | `TblAqar` | `Aqarid` | Legacy property id | `Property` | external cross-reference / `Notes` | Missing In New | High | Must be preserved in sandbox mapping file or dedicated cross-reference before real migration. |
| Adnan | `TblAqar` | `aqarNo` | Property code/number | `Property` | `Code` / `PropertySequence` | Transform Required | Medium | Must avoid collision with existing `Alromaizan` codes. |
| Adnan | `TblAqar` | `aqartypeid` | Property type | `Property` | `PropertyTypeId` | Transform Required | Medium | Requires lookup mapping. |
| Adnan | `TblAqar` | `cityid`, `heyid`, `CountryID` | Location | `Property` | `CountryId`, `CityId`, `NeighborhoodName` | Transform Required | Medium | Neighborhood may be lookup/name mismatch. |
| Adnan | `TblAqar` | `ownerid` | Owner | `Property` | `PropertyOwnerId` | Transform Required | High | Requires owner migration/mapping if owner is needed in pilot. |
| Adnan | `TblAqarDetai` | `Id` | Legacy unit id | `PropertyDetail` | external cross-reference / `Notes` | Missing In New | High | Required because display `unitno` has duplicates. |
| Adnan | `TblAqarDetai` | `Aqarid` | Unit property | `PropertyDetail` | `MainDocId` | Transform Required | Low | Map through property cross-reference. |
| Adnan | `TblAqarDetai` | `unitno` | Unit display number | `PropertyDetail` | `PropertyUnitNo` | Direct | High | Use with old `Id`; do not use as unique key. |
| Adnan | `TblAqarDetai` | `unittype` | Unit type | `PropertyDetail` | `PropertyUnitTypeId` | Transform Required | Medium | Requires lookup mapping. |
| Adnan | `TblAqarDetai` | `rentType` | Rent method/type | `PropertyDetail` / `PropertyContract` | `RentMethod` / `RentTypeId` | Transform Required | Medium | Validate against DynamicErp lookup values. |
| Adnan | `TblAqarDetai` | `RentValue` | Default unit rent | `PropertyDetail` | `RentalValue` | Direct | Low | Reference only; contract value is authoritative for active contracts. |
| Adnan | `TblAqarDetai` | `Status`, `customerid`, `ContID` | Occupancy/status | `PropertyDetail` | `StatusId` | Derived | High | Derive from strict active contracts and settlement state, not stored status alone. |
| Adnan | `TblCustemers` | `CusID` | Legacy renter id | `PropertyRenter` | external cross-reference / `Notes` | Missing In New | High | Required for reconciliation. |
| Adnan | `TblCustemers` | `CusName` | Renter Arabic name | `PropertyRenter` | `ArName` | Direct | Low | Mandatory in target. |
| Adnan | `TblCustemers` | `CusNamee` | Renter English name | `PropertyRenter` | `EnName` | Direct | Low | Optional. |
| Adnan | `TblCustemers` | `Cus_Phone`, `Cus_mobile` | Contact | `PropertyRenter` | `Phone`, `Mobile` | Direct | Low | Clean formatting if needed. |
| Adnan | `TblCustemers` | `VATNO`, `NationalNo` | Tax/national identifiers | `PropertyRenter` | `VATNo`, `NationalNo` | Direct | Medium | Validate duplicates and invalid formats. |
| Adnan | `TblCustemers` | `Account_Code` | Renter account code | `PropertyRenter` | `AccountId` | Transform Required | Critical | 256 active renter codes exist in Adnan; 0 currently match `Alromaizan.ChartOfAccount.Code`. |
| Adnan | `ACCOUNTS` | `Account_Code` | Legacy chart code | `ChartOfAccount` | `Code` | Transform Required | Critical | Must seed/map accounts in sandbox before pilot inserts. |
| Adnan | `ACCOUNTS` | `Account_Name` | Account name | `ChartOfAccount` | `ArName` | Direct | High | Needs parent/type/classification decisions. |
| Adnan | `TblContract` | `ContNo` | Legacy contract id | `PropertyContract` | `DocumentNumber` / external cross-reference | Transform Required | High | Preserve original separately; target `Id` must remain new identity. |
| Adnan | `TblContract` | `NoteSerial1` | Display/manual contract number | `PropertyContract` | `DocumentNumber` / `UnifiedContractNumber` | Transform Required | Medium | Candidate display number. Must resolve duplicates. |
| Adnan | `TblContract` | `ContDate` | Contract voucher/date | `PropertyContract` | `VoucherDate` | Direct | Low | Required in target. |
| Adnan | `TblContract` | `Iqar` | Property link | `PropertyContract` | `PropertyId` | Transform Required | Low | Cross-reference from `TblAqar.Aqarid`. |
| Adnan | `TblContract` | `UnitNo` | Unit link, actually `TblAqarDetai.Id` | `PropertyContract` | `PropertyUnitId` | Transform Required | Medium | 10 strict-active contracts have missing unit/property links and must be blocked/fixed. |
| Adnan | `TblContract` | `CusID` | Renter link | `PropertyContract` | `PropertyRenterId` | Transform Required | Medium | 10 strict-active contracts have missing renter links and must be blocked/fixed. |
| Adnan | `TblContract` | `StrDate`, `EndDate` | Contract period | `PropertyContract` | `ContractStartDate`, `ContractEndDate` | Direct | Low | Strict active filter uses `EndDate >= 2026-05-20` or null. |
| Adnan | `TblContract` | `RentType` | Rent type | `PropertyContract` | `RentTypeId` | Transform Required | Medium | Lookup mapping required. |
| Adnan | `TblContract` | `TotalContract`, `NetValue`, `TotalValue` | Contract financial totals | `PropertyContract` | `RentValue`, `NetTotal`, `TotalAfterTaxes` | Ambiguous | High | Use installment component totals as validation. |
| Adnan | `TblContract` | `CommiValue`, `InsuranceValue`, `Water`, `Electricity`, `Servce` | Contract components | `PropertyContract` | component value fields | Transform Required | Medium | Component alignment required. |
| Adnan | `TblContract` | `FATYou`, `FATValue`, `FATYou2`, `FATValue2` | VAT percent/value | `PropertyContract` | `VATPercentage`, `VATValue` | Transform Required | High | Must match batch VAT and target tax behavior. |
| Adnan | `TblContract` | `CommiValueInVAT`, `WaterElecValueInVAT`, `InsurValueInVAT` | VAT inclusion flags | `PropertyContract` | `Include...InVAT` fields | Transform Required | High | Critical for future billing. |
| Adnan | `TblContractInstallments` | `id` | Legacy installment id | `PropertyContractBatch` | external cross-reference / `Notes` | Missing In New | High | Required to allocate opening paid/remain. |
| Adnan | `TblContractInstallments` | `ContNo` | Contract link | `PropertyContractBatch` | `MainDocId` | Transform Required | Low | Cross-reference from contract. |
| Adnan | `TblContractInstallments` | `InstallNo` | Installment number | `PropertyContractBatch` | `BatchNo` | Direct | Low | Validate sequence. |
| Adnan | `TblContractInstallments` | `Installdate` | Due date | `PropertyContractBatch` | `BatchDate` | Direct | Low | Split due/past vs future at cutover. |
| Adnan | `TblContractInstallments` | `RentValue`, `Water`, `Electric`, `Commissions`, `Insurance`, `TelandNet`, `VATValue` | Installment components | `PropertyContractBatch` | batch component fields | Transform Required | Medium | `TelandNet` has no exact target; map to services/gas/notes by decision. |
| Adnan | `TblContractInstallments` | `installValue` | Installment total | `PropertyContractBatch` | `BatchTotal` | Direct/Derived | Medium | Must equal components within tolerance. |
| Adnan | `TblContractInstallments` | `payed`, `Remains` | Stored paid/remain | none directly | Derived | Critical | Do not trust directly. Recompute from `ContracttBillInstallmentsDone`. |
| Adnan | `ContracttBillInstallmentsDone` | `istallid` | Paid installment id | `CashReceiptVoucherPropertyContractBatch` | `PropertyContractBatchId` | Transform Required | Critical | Primary source for true paid allocation. |
| Adnan | `ContracttBillInstallmentsDone` | `NoteID` | Receipt note link | `CashReceiptVoucher` | external receipt link | Transform Required | High | Only `NoteType=4` for pilot receipt evidence. |
| Adnan | `ContracttBillInstallmentsDone` | paid component fields | Actual paid amounts | `CashReceiptVoucherPropertyContractBatch` / opening balance layer | Derived | Critical | Used for reconciliation and opening balance calculation. |
| Adnan | `Notes` | `NoteID` | Legacy voucher id | archive / optional voucher cross-reference | Missing In New | High | Historical archive in pilot, not native full migration. |
| Adnan | `Notes` | `NoteType=4` | Receipt voucher | `CashReceiptVoucher` | optional modern receipt | Transform Required | High | For pilot, use as evidence for opening balance; do not full-import history. |
| Adnan | `Notes` | `CashingType`, `NoteCashingType` | Receipt/payment method classification | `CashReceiptVoucher` | `CashReceiptPaymentMethodId`, cash/bank fields | Transform Required | High | Strict-active receipts show `CashingType=8`, `NoteCashingType=0/2/4`. |
| Adnan | `Notes` | `Note_Value` | Receipt amount | `CashReceiptVoucher` | `MoneyAmount` | Transform Required | High | Must reconcile with allocated paid components. |
| Adnan | `Notes` | `ContNo`, `CusID`, `akarid`, `unitno` | Business links | `CashReceiptVoucher` | `PropertyContractId`, `RenterId` | Transform Required | High | Use cross-reference maps. |
| Adnan | `DOUBLE_ENTREY_VOUCHERS` | `Notes_ID` | Voucher accounting lines | archive / reconciliation only | Historical Archive | Critical | Do not import as native journals in pilot to avoid double posting. |
| Adnan | `TblFiterWaiver` | `ContNo`, `ID` | Settlement/termination | archive / excluded from active pilot | Historical Archive | Critical | Strict active contracts exclude settlement rows. |
| Adnan | `TblIqrMerg` | `Cont`, `UntID` | Merged units for contract | `PropertyContractMergedUnit` | unit link | Transform Required | High | Include only if strict-active contract has merged units. |
| Adnan | `TblUnitNoInformation` | `UnitNo`, `ContNo`, `FilterNo`, `Des` | Unit status history | archive / reconciliation | Historical Archive | Medium | Useful for audit, not required for first pilot operation. |
