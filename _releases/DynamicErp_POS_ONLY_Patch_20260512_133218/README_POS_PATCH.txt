DynamicErp POS-only patch
Generated: 2026-05-12 13:32:18

Deploy contents:
- bin\MyERP.dll
- Areas\Pos\Views
- Areas\Pos\Scripts
- Areas\Pos\Content
- Areas\Pos\Sql
- App_Data\PrintTemplates if present

Notes:
- No DevExpress assemblies are included.
- POS save retry now handles SQL deadlocks and sys.sp_getapplock DEV_Serial lock contention.
- SearchAvailableKeshniCards uses current store stock only and returns a limited 20-card result set.
