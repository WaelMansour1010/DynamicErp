DynamicErp POS Emergency Rollback
Generated: 2026-05-12 14:52:17

Purpose:
- Restore the previous POS package from 2026-05-12 13:32:18 because the later Excel Import release caused POS not to open at the client.

Deploy:
1. Backup the current client site folder.
2. Copy this package over the web application root.
3. Restart IIS application pool.
4. Open /Pos/PosTransaction/Index.

Notes:
- This rollback uses the previous POS-only patch contents.
- It is intended as immediate service restoration, not the final Excel Import hotfix.
- No legacy/main AllScripts.sql file is included or required.
