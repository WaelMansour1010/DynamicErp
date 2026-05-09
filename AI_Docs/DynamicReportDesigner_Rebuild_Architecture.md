# Dynamic Report Designer Rebuild Architecture

## Core Layer

- `ReportDefinitionService`: reads and saves report definitions, parameters, and allowed columns.
- `ReportExecutionService`: executes only saved reports from safe StoredProcedure/View definitions.
- `ReportMetadataService`: extracts columns and stored procedure parameters.
- `ReportLayoutService`: stores and deletes user layouts by `ReportId`, `UserId`, and `ProjectScope`.
- `ReportPermissionService`: checks view/design/export capabilities using current report permission rows and admin context.
- `ReportDesignerStateService`: normalizes LayoutJson and owns the current design schema version.

## UI Layer

- Admin screen:
  - Define report code/name/scope/source.
  - Load metadata.
  - Edit parameter captions and required/default values.
  - Edit allowed columns and default behavior.
- User Designer screen:
  - View Mode for running reports.
  - Design Mode for field chooser, grouping, properties, filters, summaries, formatting, and conditional formatting.

## Area Integration

The module remains shared. POS and MainErp only provide wrapper controllers and navigation:

- POS wrapper restores POS context and uses POS login only.
- MainErp wrapper restores MainErp context and uses MainErp login only.
- Shared Reports views/scripts/services are reused by all scopes.

## LayoutJson Schema

Current `designVersion = 2` stores:

- `visibleColumns`
- `columnOrder`
- `captions`
- `widths`
- `alignment`
- `sort`
- `filters`
- `groupBy`
- `summaries`
- `formatting`
- `conditionalFormatting`
- `pageSize`
- `quickFilter`
- `areaScope`
- `reportId`

User layouts never update the original Admin metadata.

## Security Model

- End users never write SQL.
- Users execute only saved active report definitions.
- Filters in the designer are applied client-side on returned rows.
- SourceName is visible only in Admin.
- Layout delete is restricted by `LayoutId`, `UserId`, and `ProjectScope`.

## Compatibility

- No `AllScripts.sql` changes.
- No POS invoice/serial/voucher logic changes.
- Existing Reports tables are reused.
- SQL Server 2012 compatibility is preserved.
