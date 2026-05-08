# Kishny POS Release Rules

Allowed:
- `Areas/Pos/**`
- `Areas/Pos/Sql/**`
- POS config templates
- POS deployment docs

Blocked unless explicitly approved:
- `Areas/MainErp/**`
- `AI_Docs/MainErp/**`
- `AI_Docs/SharedMigration/**`
- Excel import
- Payment/cashing experiments
- Admin delete features
- Local Excel files
- Backup files
- Debug/run-mode tools

The package must block `/MainErp`, `/DevStart`, and `/RunMode`.
