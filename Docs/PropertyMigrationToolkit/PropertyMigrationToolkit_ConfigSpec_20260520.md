# Property Migration Toolkit - Config Spec
Date: 2026-05-20

## Required Config Fields
| Field | Purpose |
|---|---|
| SourceDatabaseName | Legacy VB6 source DB |
| TargetCloneDatabaseName | DynamicErp clone target |
| CustomerCode | Short customer key |
| CutoffDate | Migration balance cutoff |
| ActiveContractRule | Customer-specific active contract condition |
| IncludeHistoricalReceipts | Whether safe historical receipts may migrate |
| IncludeHistoricalIssues | Whether safe historical payments may migrate |
| IncludeJournalEntries | Whether linked journals may migrate |
| IncludeAdvancePayments | Whether advances are staged |
| IncludeTerminations | Whether old terminations migrate |
| ExcludeBrokenContracts | Exclude contracts without unit/renter/property |
| BranchMappingMode | Pilot branch vs mapped branches |
| CashBoxMappingMode | Pilot cashbox vs source mapping |
| BankMappingMode | Pilot bank vs source mapping |
| AccountMappingMode | Seed/mapped/manual |
| DefaultPilotBranch | Branch id/code for pilot |
| DefaultCashBox | CashBox id/code |
| DefaultBankAccount | BankAccount id/code |
| PaymentMethodStrategy | Hybrid resolver, fixed seed, or strict code |
| OwnerPaymentStrategy | ManualReview, Exclude, MigrateAfterApproval |
| OpeningBalanceStrategy | ComputedAsOfCutoff, SourceBalance, None |
| ArchiveHistoryMode | How to preserve unsafe history |

## Customer-Specific Mapping Extension
For each customer, document:
- Property table and key.
- Unit table and key.
- Tenant table and key.
- Contract table and key.
- Installment table and key.
- Receipt table/type/link path.
- Issue table/type/link path.
- Journal table/header/line behavior.
- Account code format.
- Cashbox/bank tables.
- Known note types.

## Approval
`IsApproved=1` should be set only after discovery and diagnostics are reviewed.
