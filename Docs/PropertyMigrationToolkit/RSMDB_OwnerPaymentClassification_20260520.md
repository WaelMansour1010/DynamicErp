# RSMDB Owner Payment Classification - 2026-05-20

## Scope
Issue/payment notes were classified for review. No issue/payment vouchers were migrated.

## Results
- Total issue/payment review-only notes: 7,632.
- AccountPaymentOrExpense: 2,284.
- PropertyLinkedIssue: 21.
- CounterpartyPayment weak matches: 222.
- UnknownIssue blocked: 5,105.
- Owner payable schedule candidates from TblAqrOwin: 4 high-confidence schedule candidates.

## Key Finding
Most NoteType=5 payments are not provably owner payments. TblAqrOwin is stronger owner-payable evidence than generic issue notes.

## Decision
Owner payments are not migration-ready. They are now classified, but finance review is required before any owner payable/payment migration.
