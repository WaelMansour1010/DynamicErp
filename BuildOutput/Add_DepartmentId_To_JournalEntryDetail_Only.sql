IF OBJECT_ID('dbo.JournalEntryDetail', 'U') IS NOT NULL
   AND COL_LENGTH('dbo.JournalEntryDetail', 'DepartmentId') IS NULL
BEGIN
    ALTER TABLE dbo.JournalEntryDetail ADD DepartmentId int NULL;
END
GO

IF OBJECT_ID('dbo.JournalEntryDetail', 'U') IS NOT NULL
   AND OBJECT_ID('dbo.JournalEntry', 'U') IS NOT NULL
   AND COL_LENGTH('dbo.JournalEntryDetail', 'DepartmentId') IS NOT NULL
BEGIN
    EXEC('
        UPDATE jed
        SET DepartmentId = je.DepartmentId
        FROM dbo.JournalEntryDetail jed
        INNER JOIN dbo.JournalEntry je ON je.Id = jed.JournalEntryId
        WHERE jed.DepartmentId IS NULL;
    ');
END
GO

IF OBJECT_ID('dbo.JournalEntryDetail', 'U') IS NOT NULL
   AND COL_LENGTH('dbo.JournalEntryDetail', 'DepartmentId') IS NOT NULL
   AND NOT EXISTS (
       SELECT 1
       FROM sys.indexes
       WHERE name = 'IX_JournalEntryDetail_DepartmentId'
         AND object_id = OBJECT_ID('dbo.JournalEntryDetail')
   )
BEGIN
    CREATE INDEX IX_JournalEntryDetail_DepartmentId
    ON dbo.JournalEntryDetail(DepartmentId);
END
GO

IF OBJECT_ID('dbo.JournalEntryDetail', 'U') IS NOT NULL
   AND OBJECT_ID('dbo.Department', 'U') IS NOT NULL
   AND COL_LENGTH('dbo.JournalEntryDetail', 'DepartmentId') IS NOT NULL
   AND OBJECT_ID('dbo.FK_JournalEntryDetail_Department', 'F') IS NULL
BEGIN
    ALTER TABLE dbo.JournalEntryDetail WITH NOCHECK
    ADD CONSTRAINT FK_JournalEntryDetail_Department
    FOREIGN KEY (DepartmentId) REFERENCES dbo.Department(Id);
END
GO
