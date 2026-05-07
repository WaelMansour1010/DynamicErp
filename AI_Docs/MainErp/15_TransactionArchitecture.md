# MainErp Transaction Architecture

Implemented files:

- `Areas\MainErp\Interfaces\IMainErpDbConnectionFactory.cs`
- `Areas\MainErp\Interfaces\IMainErpUnitOfWork.cs`
- `Areas\MainErp\Infrastructure\MainErpDbConnectionFactory.cs`
- `Areas\MainErp\Infrastructure\MainErpUnitOfWork.cs`

## Design

MainErp uses explicit ADO.NET `SqlConnection` and `SqlTransaction` ownership. This avoids EF assumptions and keeps behavior close to the VB6 transaction model while improving rollback safety.

`MainErpUnitOfWork` creates an open SQL Server connection from `MyERP_ConnectionString`, starts an explicit `ReadCommitted` transaction, blocks nested transactions, rolls back automatically on dispose if not committed, and exposes a transaction correlation id for audit/logging.

## Safety Rules

- Services that allocate IDs or post vouchers must require an active transaction.
- Nested transaction attempts throw an exception.
- Rollback is idempotent and safe during disposal.
- Future save/post workflows should create one unit of work at the orchestration boundary.

## Open Risk

VB6 sometimes uses two transaction scopes for LC save and voucher creation. The .NET target should prefer one atomic transaction unless business testing proves a two-phase shape is required.
