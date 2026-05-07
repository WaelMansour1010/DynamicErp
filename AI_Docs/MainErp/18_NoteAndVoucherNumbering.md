# Note and Voucher Numbering

Implemented files:

- `Areas\MainErp\Interfaces\INoteNumberingService.cs`
- `Areas\MainErp\Models\Accounting\NumberingPreview.cs`
- `Areas\MainErp\Services\Accounting\NoteNumberingService.cs`

## Current Scope

The service provides preview abstractions for `Notes_coding`, `Voucher_coding`, branch-aware numbering, and year-aware numbering.

It does not reserve numbers and does not replace VB6 numbering yet.

## Placeholder Behavior

The current preview reads the next candidate from `Notes.NoteSerial` filtered by branch and fiscal year. It warns that full `sanad_numbering`, user-based serials, and branch numbering rules still need mapping.

## Required Before Final Use

- Map `Voucher_coding` from `Class\registry.bas`.
- Map `Notes_coding`.
- Map `sanad_numbering`.
- Decide whether numbering is per branch, per user, per transaction type, or mixed by system option.
- Add duplicate prevention under transaction when reservation is implemented.
