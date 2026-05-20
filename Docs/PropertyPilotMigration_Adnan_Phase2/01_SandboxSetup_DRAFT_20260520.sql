/*
01_SandboxSetup_DRAFT_20260520.sql
Purpose: Draft-only instructions for creating an isolated sandbox clone of Alromaizan.
Status: DO NOT EXECUTE without explicit approval.

Recommended sandbox DB:
Alromaizan_PropertyPilot_Adnan_20260520

This script is intentionally a draft. Review backup path, data/log paths, and SQL Server permissions first.
*/

/*
STEP 1 - Take a copy-only backup of Alromaizan if a fresh backup is needed.
Manual execution only after approval:

BACKUP DATABASE [Alromaizan]
TO DISK = N'F:\SqlBackups\Alromaizan_PropertyPilot_Source_COPYONLY_20260520.bak'
WITH COPY_ONLY, INIT, COMPRESSION, STATS = 10;
*/

/*
STEP 2 - Restore into a sandbox database name only.
Manual execution only after approval:

RESTORE FILELISTONLY
FROM DISK = N'F:\SqlBackups\Alromaizan_PropertyPilot_Source_COPYONLY_20260520.bak';

RESTORE DATABASE [Alromaizan_PropertyPilot_Adnan_20260520]
FROM DISK = N'F:\SqlBackups\Alromaizan_PropertyPilot_Source_COPYONLY_20260520.bak'
WITH
    MOVE N'Alromaizan' TO N'F:\SQLData\Alromaizan_PropertyPilot_Adnan_20260520.mdf',
    MOVE N'Alromaizan_log' TO N'F:\SQLData\Alromaizan_PropertyPilot_Adnan_20260520_log.ldf',
    REPLACE,
    RECOVERY,
    STATS = 10;
*/

/*
STEP 3 - Verify isolation before any migration script.
Run against the sandbox DB only:

SELECT DB_NAME() AS CurrentDatabase;
SELECT COUNT(*) AS PropertyCount FROM dbo.Property;
SELECT COUNT(*) AS ChartOfAccountCount FROM dbo.ChartOfAccount;
*/

/*
STEP 4 - Application connection string.
Create a separate DynamicErp sandbox connection string pointing to:
Alromaizan_PropertyPilot_Adnan_20260520

Do not reuse production Alromaizan connection string for Pilot execution.
*/
