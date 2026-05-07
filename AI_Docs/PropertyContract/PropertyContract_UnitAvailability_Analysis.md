# PropertyContract Unit Availability Analysis

## Problem

In the original DynamicErp web project, the new contract screen `PropertyContract/AddEdit` did not show unit `203` in property `الاربعين` (عمارة ال 40) in database `jewlofinnovation`, although the latest contract for that unit has a valid termination/settlement movement.

## Affected Scenario

1. Open `PropertyContract/AddEdit`.
2. Select property `الاربعين` (`Property.Id = 132`, `Property.Code = 2`).
3. Select unit type `4`.
4. Search for unit `203`.
5. The unit was missing from the available units list.

## Screen Loading Flow

- View: `Views/PropertyContract/AddEdit.cshtml`
- Controller endpoint: `PropertyContractController.GetUnitByUnitTypeId`
- Ajax URL: `/PropertyContract/GetUnitByUnitTypeId?UnitTypeId=...&PropertyId=...&contractId=...`
- Database object: `dbo.GetPropertyUnitsByPropertyAndUnitTypeId`

The JavaScript only reloads the unit dropdown when the property or unit type changes. It does not apply the availability filter locally.

## Database Findings

For database `jewlofinnovation`:

- Property: `Property.Id = 132`, `ArName = الاربعين`
- Unit: `PropertyDetail.Id = 4446`, `PropertyUnitNo = 203`, `PropertyUnitTypeId = 4`
- Unit status: `PropertyDetail.StatusId = 1`
- In code constants:
  - `PropertyDetailsStatus.NotAvailableed = 1`
  - `PropertyDetailsStatus.Available = 2`

Latest contracts for unit `203`:

| ContractId | DocumentNumber | StartDate | EndDate | IsDeleted | TerminationId | Termination IsDeleted |
| --- | --- | --- | --- | --- | --- | --- |
| 6191 | 01-25-12-0727 | 2025-12-15 | 2026-12-15 | 0 | 6858 | 0 |
| 5621 | 01-25-10-0241 | 2025-10-27 | 2026-10-27 | 0 | 3622 | 0 |
| 4650 | 01-25-09-0271 | 2025-10-09 | 2026-10-09 | 0 | 3009 | 0 |

The latest contract has a valid non-deleted termination:

- `PropertyContractTermination.Id = 6858`
- `PropertyContractTermination.PropertyContractId = 6191`
- `PropertyContractTermination.TerminationDate = 2026-04-16`
- `PropertyContractTermination.LastBatchDate = 2026-04-15`
- `PropertyContractTermination.IsDeleted = 0`

## Root Cause

The stored procedure already checks that a unit should not be blocked by a previous contract when that contract has a non-deleted termination. However, it also had this condition:

```sql
AND d.StatusId <> 1
```

Because unit `203` still had `PropertyDetail.StatusId = 1`, the SP excluded it before the termination logic could make it available. This means the filter depended on a stale unit status flag as well as contract existence.

## Tables And Fields Controlling Availability

- `PropertyDetail`
  - `Id`
  - `MainDocId` as property id
  - `PropertyUnitNo`
  - `PropertyUnitTypeId`
  - `StatusId`
  - `IsDeleted`
- `PropertyContract`
  - `Id`
  - `PropertyId`
  - `PropertyUnitId`
  - `ContractStartDate`
  - `ContractEndDate`
  - `IsDeleted`
- `PropertyContractTermination`
  - `Id`
  - `PropertyContractId`
  - `PropertyId`
  - `TerminationDate`
  - `LastBatchDate`
  - `IsDeleted`

## Conclusion

The unit was hidden because `PropertyDetail.StatusId` remained occupied after settlement. The correct availability rule for contract creation should block only units with an active, non-deleted contract that does not have a non-deleted termination/settlement.
