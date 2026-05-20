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
