# PropertyContract Unit Availability Fix

## Problem

`PropertyContract/AddEdit` did not show unit `203` in property `الاربعين` in database `jewlofinnovation`, even though the unit's latest contract has a valid termination/settlement.

## Fix Summary

Added SQL script:

`Scripts/PropertyContract_UnitAvailability_Filter_Fix.sql`

The script drops and recreates:

`dbo.GetPropertyUnitsByPropertyAndUnitTypeId`

The new logic is SQL Server 2012 compatible.

## Responsible Endpoint And SP

- Endpoint: `PropertyContractController.GetUnitByUnitTypeId`
- Ajax caller: `Views/PropertyContract/AddEdit.cshtml`
- Stored procedure: `dbo.GetPropertyUnitsByPropertyAndUnitTypeId`

## What Changed

The stored procedure no longer excludes a unit only because `PropertyDetail.StatusId = 1`.

Instead, a unit is excluded only when it has an active, non-deleted contract with no non-deleted row in `PropertyContractTermination`.

The current contract id is still allowed when editing an existing contract, so the selected unit remains visible in edit mode.

## Why This Is General

No unit number is hardcoded.

The fix applies to any unit where:

- the unit belongs to the selected property and unit type;
- the unit is not deleted;
- all previous non-deleted contracts for the unit have non-deleted termination/settlement records.

Units with actual active contracts remain hidden.

## Validation Queries

Before the fix, this returned only unit `305` for property `132`, unit type `4`:

```sql
EXEC dbo.GetPropertyUnitsByPropertyAndUnitTypeId
    @PropertyId = 132,
    @PropertyUnitTypeId = 4,
    @ContractId = 0;
```

After the fix, the result includes:

| Id | PropertyUnitNo |
| --- | --- |
| 4446 | 203 |
| 4456 | 305 |

Occupied units with active contracts, such as `101`, `102`, `201`, and `202`, remain excluded because they have non-deleted contracts without non-deleted termination rows.

## Test Steps

1. Apply `Scripts/PropertyContract_UnitAvailability_Filter_Fix.sql` to database `jewlofinnovation`.
2. Open `PropertyContract/AddEdit`.
3. Select property `الاربعين`.
4. Select the relevant unit type.
5. Search for unit `203`.
6. Confirm unit `203` appears in the available units dropdown.
7. Select unit `203` and confirm the screen accepts the selection for a new contract.
8. Confirm units with active contracts and no termination do not appear.
9. Repeat with another property to confirm the unit loading behavior is unchanged for normal available and occupied units.

## Notes

This fix handles stale `PropertyDetail.StatusId` values left after settlement without creating a workaround for unit `203`. The contract/termination records become the source of truth for availability in the contract unit dropdown.
