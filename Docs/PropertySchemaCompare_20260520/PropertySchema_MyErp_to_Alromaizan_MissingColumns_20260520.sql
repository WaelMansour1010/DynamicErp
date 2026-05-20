/*
PropertySchema_MyErp_to_Alromaizan_MissingColumns_20260520.sql
Purpose: Add property-module columns that exist in reference schema MyErp but are missing from target schema Alromaizan.
Scope: Property module tables only. Columns only. No data changes.
Compatibility: SQL Server 2012.

Important:
- Do NOT run on MyErp as the reference database.
- Review before running on any customer database.
- This script intentionally has no database context switch, so it runs against the selected target database.
- No data-manipulation statements.
- No FK / Index creation.
- No existing column type changes.
*/

SET NOCOUNT ON;

PRINT 'Property schema missing columns patch started on database: ' + DB_NAME();

IF OBJECT_ID(N'[dbo].[PropertyBatch]', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.PropertyBatch', N'Discount_Backup') IS NULL
BEGIN
    ALTER TABLE [dbo].[PropertyBatch]
        ADD [Discount_Backup] money NULL;

    PRINT 'Added [dbo].[PropertyBatch].[Discount_Backup] money NULL';
END
ELSE
BEGIN
    PRINT 'Skipped [dbo].[PropertyBatch].[Discount_Backup]: table missing or column already exists';
END;

PRINT 'Property schema missing columns patch completed on database: ' + DB_NAME();

