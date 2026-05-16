# Medical Insurance Print Experience - 2026-05-14

## Purpose

The printable insurance card is a sales feature. It should feel like a real employee membership artifact, not a system report.

## Card Modes

- Full profile card for demo and HR review.
- Wallet-style compact card for print and operational use.
- Print mode hides unnecessary UI and centers the membership card.

## Print Requirements

- Provider and company identity visible.
- Employee name and code visible.
- Membership number visible.
- Status and renewal date visible.
- QR placeholder visible.
- Clean spacing and premium typography.

## Current Implementation

- POS card supports full and wallet modes.
- Print button calls browser print.
- CSS print rules focus the membership card and hide surrounding UI.
- Dependents remain visible in screen mode for demo impact.

## Screenshots Checklist

- Full membership card.
- Wallet card.
- Print preview.
- Active employee.
- Suspended/expired employee.
- Payroll-linked badge.

## Roadmap

- Real QR generation.
- Provider logo upload.
- Company logo binding.
- Direct PDF export.

