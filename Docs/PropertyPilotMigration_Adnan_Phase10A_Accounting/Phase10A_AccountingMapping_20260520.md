# Phase 10A - Accounting Mapping
Date: 2026-05-20

## Receipt Mapping
| Adnan | DynamicErp Clone | Mapping |
|---|---|---|
| `Notes.NoteID` | `CashReceiptVoucher.TransactionNo = ADNAN-{NoteID}` | Direct/Transform |
| `Notes.NoteSerial1` | `CashReceiptVoucher.DocumentNumber = ADNAN-R-{NoteSerial1}` | Transform |
| `Notes.NoteDate` | `CashReceiptVoucher.Date` | Direct |
| `Notes.NoteType=4` | `CashReceiptVoucher.SourceTypeId=11` | Transform to PropertyContract source |
| Detail paid components in `ContracttBillInstallmentsDone` | `CashReceiptVoucher.MoneyAmount` | Derived sum |
| `ContracttBillInstallmentsDone.istallid` | `PropertyContractBatch.Id` | Cross Reference |
| `PropertyContractBatch.MainDocId` | `CashReceiptVoucher.PropertyContractId` | Derived |
| `PropertyContract.PropertyRenterId` | `CashReceiptVoucher.RenterId` | Derived |
| `Notes.NoteCashingType` | `CashReceiptPaymentMethodId` | 0 => cash, else bank pilot |

## Receipt Detail Mapping
| Adnan | DynamicErp Clone | Mapping |
|---|---|---|
| `ContracttBillInstallmentsDone` paid components | `CashReceiptVoucherPropertyContractBatch.Paid` | Derived sum |
| Target batch total minus cumulative paid | `CashReceiptVoucherPropertyContractBatch.Remain` | Derived |
| Remaining <= 0.01 | `IsDelivered=1` | Derived |

## Journal Mapping
| Adnan | DynamicErp Clone | Mapping |
|---|---|---|
| `DOUBLE_ENTREY_VOUCHERS.Notes_ID` | `JournalEntry.OriginalNoteId` and `SourceId` via receipt | Direct/Transform |
| `DOUBLE_ENTREY_VOUCHERS.Account_Code` | `JournalEntryDetail.AccountId` | Account code lookup / seed |
| `Credit_Or_Debit=0` | `JournalEntryDetail.Debit` | Direct |
| `Credit_Or_Debit=1` | `JournalEntryDetail.Credit` | Direct |
| `Value` | Debit/Credit amount | Direct |
| `Double_Entry_Vouchers_Description` | Detail notes | Direct |

## Excluded Mapping
| Source | Decision |
|---|---|
| `Notes.NoteType=5` cash issue/payment candidates | Excluded with validation issues for manual review |
| `TblNotesOwnerPayment` | No linked migrated-property rows found |
| General journals without migrated receipt `NoteID` | Not migrated |
