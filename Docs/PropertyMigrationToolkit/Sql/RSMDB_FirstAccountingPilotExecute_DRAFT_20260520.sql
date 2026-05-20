/* RSMDB First Accounting Pilot Execute - DRAFT guarded script.
   This script intentionally starts with PreValidation. It must not be used until all pilot receipts are linked to contract/installment/renter. */
:r .\RSMDB_FirstAccountingPilot_PreValidation_20260520.sql
-- Future execution body will insert CashReceiptVoucher, JournalEntry, JournalEntryDetail and CrossReference only for validated scope.
