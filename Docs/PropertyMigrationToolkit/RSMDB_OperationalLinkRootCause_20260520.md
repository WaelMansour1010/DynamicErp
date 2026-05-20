# RSMDB Operational Link Root Cause - 2026-05-20

## Why links were zero
- The previous RSMDB clone phase was staging/intelligence only.
- PropertyMigrationEntityMap was empty because no operational migration had been executed.
- PropertyContract in the clone was empty because contracts migration had not been run.
- Receipt matching was source-to-source only; it did not create target DynamicErp entities.

## Confirmed Root Cause
The toolkit reached accounting readiness before executing the operational prerequisite layer:
- Properties
- Units
- Renters
- Active contracts
- Installments

## Additional Finding
Even after creating operational entities, the current 58 receipt candidates do not contain direct ContNo/CusID/akarid evidence in RSMDB.dbo.Notes. Their possible links are inferred only by amount/date, which is not strong enough for posting.
