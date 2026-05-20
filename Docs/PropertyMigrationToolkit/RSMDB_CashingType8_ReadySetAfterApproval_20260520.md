# RSMDB CashingType=8 ReadySet After Approval - 2026-05-20

## Actual ReadySet

Because no finance approvals were applied, the actual ReadyForAccountingPilot set remains unchanged.

| Classification | Receipts | Value |
|---|---:|---:|
| NeedsFinanceApproval | 505 | 14,115,520.2900 |
| NeedsLinkReview | 7,578 | 184,248,146.5350 |
| ReadyForAccountingPilot | 0 | 0.0000 |

## Simulated ReadySet If Approved

| Approval Scenario | Ready Receipts | Ready Journals | Ready Lines | Ready Value |
|---|---:|---:|---:|---:|
| Top 25 | 4 | 4 | 8 | 49,168.2500 |
| Top 50 | 4 | 4 | 8 | 49,168.2500 |
| Top 100 | 5 | 5 | 10 | 73,168.2500 |
| Score >= 60 | 32 | 32 | 64 | 966,568.2500 |

## Blocking Reason

The blocker is no longer receipt allocation or journal balance. It is finance account resolution for the CashingType=8 scope, especially tenant/customer accounts with no mapped DynamicErp account.
