# Journal Migration Status - Adnan / RSMDB - 2026-05-20

## Executive Summary

The journal migration performed in the Adnan pilot must be treated as partial property-linked accounting, not full historical general ledger migration. The toolkit intentionally avoids moving all old general ledger history unless every voucher, account, and journal relationship is proven.

## Adnan Status

Controlled clone execution result:

| Metric | Value |
|---|---:|
| Contracts handled | 283 |
| Receipts handled | 753 |
| Issues handled | 0 |
| Journal headers handled | 754 |
| Journal lines handled | 2,363 |
| Journal lines with `AccountId=NULL` | 0 |
| Unbalanced journals | 0 |
| Critical accounting errors | 0 |

## What These Adnan Journals Represent

The migrated/retained journal scope is property-linked and approved-scope accounting. It is not proof that all Adnan GL history was migrated.

Included scope:

- Approved property-linked receipts.
- Journals linked to approved migrated vouchers/scope.
- Opening-balance/advance-payment supported reconciliation where applicable.

Not considered fully migrated by default:

- All general ledger history.
- Unsafe payment/issues history.
- Owner payments.
- Unclassified NoteType 9088.
- Termination NoteType -1 as full accounting history.
- Contract journal NoteType 60 unless linked and validated by the approved accounting scope.

## Adnan NoteType Status

| NoteType | Candidate Meaning | Migration Status |
|---:|---|---|
| 4 | Receipt | Approved only when linked to migrated contract/installment and account-safe. |
| 5 | Payment / Issue | Not fully migrated; unsafe/owner-related payments are review-only. |
| 60 | Contract journal | Not considered fully migrated unless linked and validated. |
| 9088 | VAT/installment candidate | Not approved; requires interpretation. |
| -1 | Termination/settlement candidate | Not migrated as full accounting history by default. |

## RSMDB Discovery Status

Read-only discovery metrics:

| Metric | Value |
|---|---:|
| `DOUBLE_ENTREY_VOUCHERS` lines | 139,769 |
| Journal lines without source account code | 0 |
| Potential unbalanced grouped vouchers using provisional grouping/direction | 51,968 |
| `Notes` Type 4 receipts | 10,365 |
| `Notes` Type 5 issues/payments | 7,632 |
| `Notes` Type -1 terminations | 754 |
| `Notes` Type 9088 unclassified | 64 |

## RSMDB Journal Decision

RSMDB journals are not ready for migration. They require:

- Confirmed direction semantics in `DOUBLE_ENTREY_VOUCHERS`.
- Confirmed grouping key for voucher balance validation.
- Account mapping from RSMDB account codes to target `ChartOfAccount`.
- Receipt/payment/termination mapping validation from VB6.
- Owner payment/payable review.

## Accounting Safety Decision

No journal may be migrated if:

- It is unbalanced.
- It has an unmapped account.
- It is not linked to an approved migrated voucher or approved opening-balance strategy.
- It represents owner payment without owner/account/property proof.
