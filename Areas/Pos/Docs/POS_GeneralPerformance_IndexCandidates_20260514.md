# POS General Performance Index Candidates - 2026-05-14

Scope: `Areas\Pos` only.

No indexes were created in this phase. The local `Cash` schema already contains many POS/report/search indexes on the large write-heavy tables, so every candidate below requires an actual execution plan from production-like data before implementation.

## Existing Large Table Snapshot

| Table | Local rows observed | Notes |
|---|---:|---|
| `DOUBLE_ENTREY_VOUCHERS` | 7,227,939 | Many accounting/report indexes already exist. |
| `Transaction_Details` | 2,014,078 | Clustered on `ItemSerial`; several `Transaction_ID`, item, serial, and POS indexes exist. |
| `Notes` | 1,336,415 | Has indexes on `NoteType`, `NoteDate`, `branch_no`, `Transaction_ID`, `NoteSerial1`, and payment-related columns. |
| `Transactions` | 1,248,549 | Many POS report/search indexes already exist. |
| `TblCusCsh` | 148,294 | Has KYC search indexes for `PhoneNo2`, `CardNo`, `CardId`, `Tet_NumPoket`. |
| `TBLClosePos` | 53,631 | Closing report indexes exist from earlier phase. |
| `TblSalesPayment` | 14,537 | `IX_POS_TblSalesPayment_TransID` exists. |

## Existing Indexes Checked

Important existing indexes found locally:

- `Transactions`: `IX_POS_Transactions_Search_NoteSerial1`, `IX_POS_Transactions_Search_VisaNumber`, `IX_POS_Transactions_Search_ManualNO`, `IX_POS_Transactions_OperationalSales_Report`, `IX_Tx_Branch_Type_Date_Desc`, `IX_Transactions_TypeDate`, `IX_POS_KycAvailableCards_Transactions`, many branch/date/type variants.
- `Transaction_Details`: `IX_Transaction_Details_Item_ID_Transaction_ID`, `IX_Transaction_Details_TransactionID_ID`, `IX_POS_TransactionDetails_StoreSerials_Report`, `IX_POS_KycAvailableCards_Details`.
- `TblCusCsh`: `IX_POS_TblCusCsh_KycSearch_PhoneNo2`, `IX_POS_TblCusCsh_KycSearch_CardNo`, `IX_POS_TblCusCsh_KycSearch_CardId`, `IX_POS_TblCusCsh_KycSearch_National`.
- `Notes`: `IX_Notes_Close`, `IX_Notes_NoteDate_NoteType_branch_no`, `IX_Notes_NoteType_NoteSerial1_64FBFC4C`, `IX_Notes_Transaction_ID`, plus user/box/bank/customer indexes.
- `DOUBLE_ENTREY_VOUCHERS`: `IX_DEV_RecordDate`, `IX_POS_DEV_Transaction_ID`, `IX_POS_DEV_Notes_ID`, `IX_DOUBLE_ENTREY_VOUCHERS_Notes_ID`, `IX_DOUBLE_ENTREY_VOUCHERS_Transaction_ID`.

## Candidates Requiring Execution Plan Confirmation

| Candidate | Exact slow query / endpoint | Table | Existing indexes checked | Proposed index | Why it may help | Estimated write overhead | Execution plan confirms? |
|---|---|---|---|---|---|---|---|
| Sales invoice all-branch/date search | `dbo.usp_POS_SalesInvoices_Search`, default no-term branch/date slice from `PosTransaction.GetTodayInvoices` | `Transactions` | Existing `IX_Tx_Branch_Type_Date_Desc`, `IX_Transactions_TypeDate`, `IX_POS_Transactions_OperationalSales_Report` | No new index yet. If plan scans, evaluate a filtered/order-friendly index on `(Transaction_Type, Transaction_Date DESC, Transaction_ID DESC)` including minimal display fields. | Admin/all-branch date search orders by newest transaction and returns `TOP 50`. | High: `Transactions` is write-heavy for invoices. | No. Capture actual plan first. |
| Sales invoice phone/IPN prefix search | `dbo.usp_POS_SalesInvoices_Search` candidate block using `CashCustomerPhone`, `Phone2`, `IPN` prefix | `Transactions` | Existing `CashCustomerPhoneIndex`, search indexes for serial/token/manual, date/type indexes | Consider separate prefix index only if plan proves phone/IPN is dominant and slow. | Current proc uses prefix for phone/IPN, but not all searched columns have targeted indexes. | Medium/High: every invoice write updates it. | No. |
| Item serial invoice search | `dbo.usp_POS_SalesInvoices_Search` fallback `EXISTS Transaction_Details WHERE ItemSerial LIKE @search + '%'` | `Transaction_Details` | Clustered index on `ItemSerial`; `IX_POS_TransactionDetails_StoreSerials_Report`; `IX_POS_KycAvailableCards_Details` | Likely no new index; clustered `ItemSerial` may already help prefix serial searches. | Only if plan shows expensive lookup back to transaction headers. | Medium. | No. |
| KYC search by phone/card/national | `dbo.usp_POS_KycCustomers_Search` over `TblCusCsh` | `TblCusCsh` | Existing POS KYC indexes on `EasyCashType, PhoneNo2/CardNo/CardId/Tet_NumPoket, BranchID, Id` | No new index recommended now. | Existing indexes match exact/prefix KYC identifiers. Main risk is broad name `%term%`, not fixed by a normal b-tree index. | Medium. | Existing indexes likely sufficient; not plan-confirmed. |
| KYC other-branch hint | `FindKeshniCardCustomerOtherBranchHint` inline query in `PosSqlRepository` | `TblCusCsh` | Same KYC indexes checked | Prefer query rewrite to exact/prefix first, not new index. | Current fallback duplicates broad LIKE logic and runs after no local branch match. | Medium if indexed; lower if rewritten. | No. |
| Available Keshni cards | `dbo.usp_POS_SearchAvailableKeshniCards` through `SearchAvailableKeshniCardTokens` | `Transactions`, `Transaction_Details` | `IX_POS_KycAvailableCards_Transactions`, `IX_POS_KycAvailableCards_Details` exist | No new index until plan. Consider summary/current-stock table instead of more indexes if still slow. | Availability is an aggregate of historical stock movement; indexes may not remove aggregation cost. | High on invoice/stock writes. | No. |
| Payments search without dates | `dbo.usp_POS_Payments_Search` over `Notes` | `Notes` | `IX_Notes_Close`, `IX_Notes_NoteDate_NoteType_branch_no`, `IX_Notes_NoteType_NoteSerial1_64FBFC4C`, user/box/bank indexes | No new index first; require date filters or exact note number search. | Current broad text predicates `%search%` on `person`, `Remark`, etc. are not solved well by normal b-tree indexes. | Medium. | No. |
| Stock transfer index search | `SearchStockTransfers` joins `Transactions` and `Transaction_Details` for transaction types 10/992 | `Transactions`, `Transaction_Details` | General branch/type/date and `Transaction_ID` detail indexes exist | Evaluate `(Transaction_Type, Transaction_Date, BranchId, StoreID)` only if plan scans transfer rows heavily. | Transfer rows are likely much smaller than sales but still in large `Transactions`. | Medium/High. | No. |
| Stock transfer available serials | `SearchStockTransferAvailableSerials` groups historical `Transaction_Details` by serial for one item/store/date | `Transaction_Details`, `Transactions` | Serial/detail/store indexes exist | Prefer stock-balance/serial-balance summary design if slow. | Aggregation over history is the cost; more indexes may have limited benefit. | High. | No. |
| Dashboard live summary | `dbo.usp_POS_Dashboard_Summary` raw aggregation | `Transactions`, `TblCusCsh` | Existing operational/report indexes exist | Prefer snapshot/summary usage over more indexes. | Live dashboard has broad aggregations and KYC activation EXISTS checks. | High if indexing extra display columns. | No. |

## Recommendation

Do not create new indexes yet. Use the timing added in this audit plus actual execution plans to pick the first target. The most likely first wins are query/UI restrictions and snapshot/summary reads, not more nonclustered indexes on `Transactions` or `Transaction_Details`.
