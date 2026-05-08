# SQL Release Rules

Every SQL release must include:
- numbered scripts,
- apply order,
- backup script,
- rollback notes,
- target module/database,
- SQL Server compatibility,
- no cross-module scripts.

Kishny POS releases may include only POS SQL. MainErp SQL must never be applied to the Kishny POS customer database.
