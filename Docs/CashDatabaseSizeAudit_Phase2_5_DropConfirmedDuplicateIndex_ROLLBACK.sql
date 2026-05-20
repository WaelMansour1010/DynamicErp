/*
Phase 2.5 - Rollback Script
Target DB: Cash
Purpose: Recreate dropped duplicate index if rollback is needed
*/
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
USE [Cash];

PRINT 'ROLLBACK CHECK: recreate IX_POS_Notes_Transaction_ID only if missing';
IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID('dbo.Notes')
      AND name = 'IX_POS_Notes_Transaction_ID'
)
BEGIN
    CREATE NONCLUSTERED INDEX [IX_POS_Notes_Transaction_ID]
    ON [dbo].[Notes] ([Transaction_ID] ASC)
    INCLUDE ([NoteID])
    WITH (
        PAD_INDEX = OFF,
        STATISTICS_NORECOMPUTE = OFF,
        SORT_IN_TEMPDB = OFF,
        DROP_EXISTING = OFF,
        ONLINE = OFF,
        ALLOW_ROW_LOCKS = ON,
        ALLOW_PAGE_LOCKS = ON,
        FILLFACTOR = 0
    )
    ON [PRIMARY];

    PRINT 'Recreated: dbo.Notes.IX_POS_Notes_Transaction_ID';
END
ELSE
BEGIN
    PRINT 'Index already exists; rollback creation skipped.';
END

SELECT i.name AS index_name, i.index_id, i.type_desc
FROM sys.indexes i
WHERE i.object_id = OBJECT_ID('dbo.Notes')
  AND i.name IN ('IX_Notes_Transaction_ID','IX_POS_Notes_Transaction_ID')
ORDER BY i.name;
