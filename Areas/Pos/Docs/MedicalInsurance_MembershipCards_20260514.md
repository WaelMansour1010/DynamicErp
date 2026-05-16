# Medical Insurance Membership Cards - 2026-05-14

## Purpose

The membership card is a client-facing WOW feature. It makes medical insurance feel real, sellable, and operational.

## Card Contents

- Employee avatar initials.
- Employee name.
- Employee code.
- Membership number.
- Provider.
- Plan.
- Coverage status badge.
- Payroll-linked state.
- Renewal date.
- Family/dependent count.
- Coverage cost summary.
- QR placeholder.
- Company branding area.
- Provider logo placeholder.

## Modes

- Full profile mode.
- Compact wallet-style mode.
- Printable mode.

## Print Behavior

The print stylesheet hides the rest of the POS page and prints the membership card only.

## Data Notes

Membership numbers are generated as:

`MED-{year}-{employeeId}`

This is suitable for demo and can later be replaced by a persisted card-number policy.

## Remaining Roadmap

- Persist card number.
- Upload employee photos.
- Upload provider logos.
- Generate QR code linked to verification endpoint.
- Add card expiry validation screen for branch operators.
