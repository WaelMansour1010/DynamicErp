# Proposed Main ERP Domain Design

Scope: initial .NET domain structure only. This is not final business logic implementation.

## Area Boundary

All web code belongs under:

`F:\Source Code\DynamicErp\Areas\MainErp\`

LC and Project Extracts must remain independent from `Areas\Pos`.

## Proposed Folders

```text
Areas\MainErp\
  Controllers\
    LCController.cs
    ProjectExtractsController.cs
  Models\
    LC\
    ProjectExtracts\
    Accounting\
  ViewModels\
    LC\
    ProjectExtracts\
  Services\
    LC\
    ProjectExtracts\
    Accounting\
    Shared\
  Repositories\
    LC\
    ProjectExtracts\
    Accounting\
    Shared\
  Permissions\
  Helpers\
  Sql\
```

## LC Domain Objects

Initial entities/view models:

- `LcHeader`: maps `TblLC` business header and account fields.
- `LcAccountLinks`: LC account, margin account, acceptance account, expense account, parent account selections.
- `LcOpeningBalance`: open balance value/type/date/voucher id.
- `LcNoteLink`: note ids, note serials, row ids.
- `LcMarginLine`: margin/history/opening balance grid line abstraction.
- `LcSearchCriteria` and `LcListItem`.

Services:

- `LcService`: validation, header save orchestration, search/read operations.
- `LcAccountService`: account creation/editing rules matching `ModAccounts`.
- `LcAccountingService`: creates note/voucher command model; no direct UI logic.
- `LcReportService`: report parameter/query definitions.

Repositories:

- `LcRepository`: `TblLC` read/write.
- `LcMarginRepository`: `TBLLCMargin`, `TBLLCMargin2`, `TBLLCHistory`, `tblLCOpenB`.
- `AccountRepository`: `ACCOUNTS` read/write and account-code generation.
- `VoucherRepository`: `Notes`, `Notes1`, `DOUBLE_ENTREY_VOUCHERS`, `DOUBLE_ENTREY_VOUCHERS1`.

Atomic transaction boundary:

- LC header save, account create/edit, note deletion/recreation, margin grid persistence, and voucher generation must be a single database transaction in .NET unless a verified business reason preserves VB6's two-transaction shape.

## Project Extract Domain Objects

Initial entities/view models:

- `ProjectExtractHeader`: maps `project_billl`.
- `ProjectExtractLine`: maps `project_bill_details`.
- `ProjectExtractAmounts`: total, VAT, net, discount, retention, advance payment.
- `ProjectExtractParty`: project, end-user, subcontractor, account links.
- `ProjectExtractPrepaymentApplication`: maps `TblPayPrePayed` / `TblProjePayPrePayed`.
- `ProjectExtractPostingResult`: note id, voucher ids, warnings.
- `ProjectExtractSearchCriteria` and `ProjectExtractListItem`.

Services:

- `ProjectExtractService`: save orchestration, validation, read/search.
- `ProjectExtractCalculationService`: totals, previous/current/cumulative quantities, VAT, discount and retention allocations.
- `ProjectExtractAccountingService`: creates voucher posting commands for end-user and subcontractor branches.
- `ProjectExtractPrepaymentService`: advance/prepaid note loading and application.
- `ProjectExtractApprovalService`: boundary around `SendTopost` equivalent.
- `ProjectExtractReportService`: report inventory and parameter contracts.

Repositories:

- `ProjectExtractRepository`: `project_billl` and `project_bill_details`.
- `ProjectRepository`: `projects` and project account links.
- `CustomerRepository`: `TblCustemers` account/VAT links.
- `PrepaymentRepository`: `TblPayPrePayed`, `TblProjePayPrePayed`, source `Notes`.
- `VoucherRepository`: shared accounting rows.
- `VatArtifactRepository`: `transactionsVatDetails`, only after e-invoice behavior is designed.

Atomic transaction boundary:

- Header, note, voucher rows, detail rows, advance-payment applications, and note total updates must be one DB transaction.
- E-invoice send should remain post-commit, with retry/status tracking, not inside the accounting transaction.

## Shared Accounting Services

Proposed shared Main ERP services:

- `VoucherPostingService`: accepts posting commands and writes `DOUBLE_ENTREY_VOUCHERS` / `DOUBLE_ENTREY_VOUCHERS1`.
- `NoteNumberingService`: wraps `Notes_coding`, `Voucher_coding`, and fiscal serial behavior.
- `AccountCodeGenerationService`: mirrors `GetNewAcountCode` and validates parent account rules.
- `BranchAccountResolver`: wraps `get_account_code_branch` behavior.
- `AccountingTransactionScope`: shared unit-of-work wrapper for ADO.NET/EF transaction consistency.

These services should be created under `Areas\MainErp\Services\Accounting` first. Move to a neutral global folder only after another non-POS module reuses them.

## Async Boundaries

Can be async later:

- Report rendering/export.
- E-invoice submission/retry after committed accounting data.
- Attachment indexing.
- Audit log enrichment.

Must remain synchronous/transactional:

- Manual id allocation.
- Account code creation.
- Note creation.
- Voucher posting.
- Header/detail persistence.
- Advance-payment application and source-note `TotalPayed` update.

## Permission Boundaries

Initial permissions should be module-specific:

- `MainErp.LC.View`, `MainErp.LC.Create`, `MainErp.LC.Edit`, `MainErp.LC.Delete`, `MainErp.LC.Post`, `MainErp.LC.Report`.
- `MainErp.ProjectExtracts.View`, `Create`, `Edit`, `Delete`, `Post`, `Approve`, `Report`.

Do not reuse POS/Kishny permission names.

## Implementation Order

1. Read-only list/search models.
2. Detail read model.
3. Accounting dry-run/posting preview service.
4. Save draft without posting.
5. Controlled posting implementation with transaction tests.
6. Reports.
