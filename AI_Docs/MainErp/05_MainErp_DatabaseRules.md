# MainErp Database Rules

## Authoritative Legacy Reference

`F:\Source Code\SatriahMain\Main Script\AllScripts.sql`

## New SQL Location

`F:\Source Code\DynamicErp\Areas\MainErp\Sql\`

## Rules

- SQL Server 2012 compatibility is required.
- Stored procedure changes must use DROP + CREATE.
- Inspect AllScripts.sql before adding or changing stored procedures or tables.
- Inspect the live DB schema where possible before guessing table shapes.
- Do not add MainErp scripts under `Areas\Pos\Sql`.
- Do not modify Kishny SQL logic for MainErp work.
- Do not append generated SQL to AllScripts.sql without an explicit, minimal, documented reason.

## Phase 1 Database Status

No database objects were created. The SQL files under MainErp are placeholders that document future script ownership.
