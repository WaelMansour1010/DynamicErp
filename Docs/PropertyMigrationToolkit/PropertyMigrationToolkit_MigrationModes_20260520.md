# Property Migration Toolkit - Migration Modes
Date: 2026-05-20

## Overview
The Enterprise Engine supports three modes: `Strict`, `Tolerant`, and `Hybrid`.

## Strict Mode
Used for final GoLive rehearsal and production-ready validation.

Allowed:
- Exact mapped records only.
- No fallback entities.
- No suspense account unless already approved and cleared.
- Exclude incomplete records.

Blocked:
- Unknown unit/property/renter.
- Missing accounts.
- Unknown payment method.
- Any accounting ambiguity.

## Tolerant Mode
Used for large legacy conversions and early ReadyToTest builds.

Allowed:
- Placeholder property/unit/renter.
- Temporary renter account.
- Fallback cashbox/bank/payment method.
- Review Queue for incomplete records.
- Suspense account only for non-final testing and with full logging.

Blocked:
- Unbalanced journals.
- `AccountId=NULL`.
- Unknown journal direction.
- Same debit/credit account by accident.

## Hybrid Mode
Recommended default.

Rules:
- Master data is tolerant.
- Contracts/installments are tolerant with placeholders.
- Accounting remains strict except approved suspense/holding flows.
- Owner payments default to Manual Review.

## Mode Matrix
| Problem | Strict | Tolerant | Hybrid |
|---|---|---|---|
| Contract missing unit | Exclude | Link unknown unit + review | Link unknown unit + review |
| Unit missing property | Exclude | Link unknown property + review | Link unknown property + review |
| Tenant missing account | Exclude | Temporary/suspense renter account + review | Temporary renter account; accounting review |
| Payment method unknown | Exclude voucher | Default method + review | Default for non-accounting preview; strict before posting |
| Journal account missing | Exclude journal | Suspense only if allowed | Suspense only if explicitly allowed |
| Unbalanced journal | Block | Block | Block |
