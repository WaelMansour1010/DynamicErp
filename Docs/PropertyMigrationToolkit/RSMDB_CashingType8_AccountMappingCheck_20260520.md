# RSMDB CashingType=8 Account Mapping Check - 2026-05-20

## Finance Approval Rule
A candidate can be `ReadyForAccountingPilot` only when every `DOUBLE_ENTREY_VOUCHERS.Account_Code` line has an approved mapping in:
`dbo.PropertyMigrationAccountResolution`

with:

- `ResolutionStatus = Approved`
- `TargetAccountId IS NOT NULL`
- no Suspense unless explicitly approved

## Results

| Metric | Count |
|---|---:|
| Candidate receipts checked | 8,083 |
| Candidates with all accounts finance-approved | 0 |
| Linked + balanced candidates waiting for finance approval | 505 |
| Finance review accounts generated | 733 |

## Top Finance Review Accounts Affecting Linked Candidates

| SourceAccountCode | SourceAccountName | UsageReceiptCount | Debit | Credit | Impact |
|---|---|---:|---:|---:|---|
| `a1a2a1a2a12a1` | rawda collection account - 258 | 200 | 2,831,906.5700 | 0.0000 | Can unlock linked balanced receipts |
| `a1a2a2a1a91` | تحصيلات غير معلومة من العملاء | 128 | 2,635,737.0000 | 0.0000 | Can unlock linked balanced receipts |
| `a1a2a2a4a1` | مدفوعات تأمين محتجزة على المنصة | 81 | 256,000.0000 | 0.0000 | Can unlock linked balanced receipts |
| `a1a2a2a2a271` | غيث بسيم عصام القدومي | 78 | 0.0000 | 288,739.0800 | Can unlock linked balanced receipts |
| `a1a2a2a2a320` | سوزان محمد مزهر | 74 | 0.0000 | 211,572.3500 | Can unlock linked balanced receipts |

## Decision
Additional finance approval is required before any accounting pilot execute. This is now a focused review pack for real property receipts only, not mixed general accounts.
