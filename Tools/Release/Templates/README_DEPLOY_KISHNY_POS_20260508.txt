Kishny POS Deadlock/Save Hardening Release - 2026-05-08

Do not deploy until GO/NO-GO is marked GO.

Deploy order:
1. Backup web folder.
2. Backup Web.config.
3. Backup database.
4. Run Sql\00_BACKUP_BEFORE_APPLY.sql and save output.
5. Apply SQL in Sql\SQL_APPLY_ORDER.md.
6. Copy Package contents to staging/customer web root.
7. Apply real production Web.config values from Config\Web.config.production-ready.
8. Recycle app pool.
9. Smoke test /Pos/Login, /Pos, /Pos/PosTransaction/Index.
10. Confirm /MainErp, /DevStart, /RunMode are blocked.
11. Monitor POS save attempts, SQL errors, and App_Data logs.
