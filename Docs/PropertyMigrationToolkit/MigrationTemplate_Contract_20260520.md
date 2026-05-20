# Migration Template Contract - 2026-05-20

## Standard Inputs

Every executable migration template must use:

- `@MigrationBatchId`
- `@CustomerCode`
- `@SourceDatabaseName`
- `@CutoffDate`
- `@MigrationMode`
- `PropertyMigrationConfig`
- `PropertyMigrationEntityMap`
- `PropertyMigrationWarning`
- `PropertyMigrationError`
- `PropertyMigrationExcludedRecord`
- `PropertyMigrationReviewQueue`

## Source Data Contract

Generic templates must not query old VB6 tables directly. Customer-specific mapping scripts populate these tables first:

- `PropertyMigrationSourceProperty`
- `PropertyMigrationSourceUnit`
- `PropertyMigrationSourceRenter`
- `PropertyMigrationSourceContract`
- `PropertyMigrationSourceInstallment`
- `PropertyMigrationSourceOpeningBalance`
- `PropertyMigrationSourceAdvancePayment`
- `PropertyMigrationSourceReceipt`
- `PropertyMigrationSourceIssue`
- `PropertyMigrationSourceJournal`
- `PropertyMigrationSourceJournalLine`
- `PropertyMigrationSourceTermination`
- `PropertyMigrationSourceOwner`
- `PropertyMigrationSourcePropertyOwner`
- `PropertyMigrationSourceOwnerBalance`
- `PropertyMigrationSourceOwnerPayment`


## Owner/Landlord Contract

Owners are first-class property entities, not renters and not generic suppliers. Customer-specific staging scripts must populate owner data separately when the source system contains owner or landlord relationships.

Required owner semantics:

- `PropertyMigrationSourceOwner` stores owner master data and optional owner account code.
- `PropertyMigrationSourcePropertyOwner` stores the property-to-owner relationship and ownership percentage when available.
- `PropertyMigrationSourceOwnerBalance` stores owner payable/receivable balances as staging/review data only until finance sign-off.
- `PropertyMigrationSourceOwnerPayment` stores owner payment vouchers as manual-review by default.

Owner safety rules:

- Do not infer owner = renter.
- Do not migrate owner payments unless owner, property/contract link, payment source, and account mapping are all proven.
- If multiple owners or ownership percentages exist but the target schema supports only a single `PropertyOwnerId`, create Review Queue items before updating target ownership.
- Owner payment journals must follow the same accounting safety gates as all other journals: no `AccountId=NULL`, balanced debit/credit, and no unsafe same-account debit/credit.
- SourceType-based owner payments, including `SourceTypeId=13`, remain Manual Review until validated against source code and data for that customer.

## Outputs

Each template must produce one or more of:

- Target records in DynamicErp tables.
- Cross-reference rows in `PropertyMigrationEntityMap`.
- Warnings in `PropertyMigrationWarning`.
- Critical issues in `PropertyMigrationError`.
- Manual review items in `PropertyMigrationReviewQueue`.
- Excluded records in `PropertyMigrationExcludedRecord`.
- Summary SELECT output for Runner/report logging.

## Safety Rules

- Target DB guard is mandatory.
- Source DB must never be updated.
- BatchId is mandatory.
- CrossReference is mandatory for every migrated entity.
- Idempotency is required using `PropertyMigrationEntityMap` checks.
- Accounting templates must reject `AccountId=NULL`.
- Journal templates must reject unbalanced journals.
- Owner payments and terminations remain manual-review unless explicitly approved.

## Template Modes

- `Strict`: missing critical links are excluded.
- `Tolerant`: configured fallbacks may be used and must be logged.
- `Hybrid`: master data may use fallbacks; accounting remains strict.
