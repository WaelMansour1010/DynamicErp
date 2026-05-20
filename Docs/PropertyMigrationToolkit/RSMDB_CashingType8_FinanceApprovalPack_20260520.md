# RSMDB CashingType=8 Finance Approval Pack - 2026-05-20

## Scope

This pack is scoped only to real property receipt candidates:

- Source: RSMDB read-only
- Target clone: Alromaizan_PropertyPilot_RSMDB_StagingClone_20260520
- BatchId: 1B5D8EB5-DD7E-4B63-8DDB-E7B00AAD558B
- ScopeName: RSMDB_CashingType8
- Receipt filter: NoteType=4 + CashingType=8 + ContracttBillInstallmentsDone allocation evidence
- No migration, no receipts, no journals, and no posting were executed.

## Finance Pack Summary

| Metric | Value |
|---|---:|
| Accounts in CashingType=8 finance pack | 733 |
| Accounts with suggested target requiring finance approval | 97 |
| Accounts needing more information / no safe target | 636 |
| Candidate receipts needing finance approval | 505 |
| Candidate receipt value | 14,115,520.2900 |
| Current ReadyForAccountingPilot before approval | 0 |

## Priority Accounts

| Rank | Source Account | Name | Suggested Target | Family | Confidence | Usage | Debit | Credit | Decision |
|---:|---|---|---|---|---:|---:|---:|---:|---|
| 1 | a1a2a1a2a12a1 | rawda collection account - 258 | 110201001 - العملاء | Receivables | 65 | 200 | 2,831,906.5700 | 0.0000 | ApproveAfterFinanceReview |
| 2 | a1a2a2a1a91 | تحصيلات غير معلومة من العملاء | 1102 - العملاء | RenterReceivable | 72 | 128 | 2,635,737.0000 | 0.0000 | ApproveAfterFinanceReview |
| 3 | a1a2a2a4a1 | مدفوعات تأمين محتجزة على المنصة | 110201001 - العملاء | Receivables | 65 | 81 | 256,000.0000 | 0.0000 | ApproveAfterFinanceReview |
| 4 | a1a2a2a2a271 | غيث بسيم عصام القدومي | Not suggested | Unknown | 25 | 78 | 0.0000 | 288,739.0800 | NeedsMoreInfo |
| 5 | a1a2a2a2a320 | سوزان محمد مزهر | Not suggested | Unknown | 25 | 74 | 0.0000 | 211,572.3500 | NeedsMoreInfo |
| 6 | a1a2a2a2a220 | سامر خالد شقير | Not suggested | Unknown | 25 | 73 | 0.0000 | 539,510.7200 | NeedsMoreInfo |
| 7 | a1a2a2a2a407 | يوسف احمد حوت | Not suggested | Unknown | 25 | 63 | 0.0000 | 345,864.9900 | NeedsMoreInfo |
| 8 | a1a2a2a2a392 | محمد عبدالفتاح امين شاهين | Not suggested | Unknown | 25 | 61 | 0.0000 | 231,381.6410 | NeedsMoreInfo |
| 9 | a1a2a2a2a410 | فارس محمود محمود | Not suggested | Unknown | 25 | 60 | 0.0000 | 386,400.0000 | NeedsMoreInfo |
| 10 | a1a2a2a2a424 | مصطفى شريف خيري | Not suggested | Unknown | 25 | 58 | 0.0000 | 191,114.9700 | NeedsMoreInfo |

## Decision Meaning

- ApproveAfterFinanceReview: the engine found a candidate target, but finance must approve it before any pilot.
- NeedsMoreInfo: the engine did not find a safe target account; these must be manually mapped or intentionally blocked.
- No SuspenseApproved was applied.

## Safety Result

No approvals were applied in this phase because no explicit finance sign-off was provided for a Top N set. The pack is ready for accountant review.
