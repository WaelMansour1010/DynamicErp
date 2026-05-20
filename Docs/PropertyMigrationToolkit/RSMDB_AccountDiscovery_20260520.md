# RSMDB Account Discovery - 2026-05-20

## Scope
Account discovery ran on clone Alromaizan_PropertyPilot_RSMDB_StagingClone_20260520 only. Source RSMDB was read-only.

## Sources
- RSMDB.dbo.ACCOUNTS
- RSMDB.dbo.DOUBLE_ENTREY_VOUCHERS.Account_Code
- property-related note account columns such as AccountPaym, DebitSide, CreditSide, Account_Code1, Account_Code2, AccountsCode
- target dbo.ChartOfAccount in the clone

## Results
- Accounts discovered: 1,599
- Target ChartOfAccount active accounts: 172
- Distinct source account code style: VB6 symbolic codes like 1a2a1a2a7a1
- Target account code style: DynamicErp numeric codes like 110501001

## Finding
The source and target account code systems are structurally different, so direct code matching is not enough.
