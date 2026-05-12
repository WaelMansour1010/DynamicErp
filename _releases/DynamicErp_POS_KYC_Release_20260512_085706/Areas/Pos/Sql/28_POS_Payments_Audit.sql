IF COL_LENGTH('dbo.Notes', 'LastModifiedByUserId') IS NULL
BEGIN
    ALTER TABLE dbo.Notes ADD LastModifiedByUserId INT NULL;
END;
GO

IF COL_LENGTH('dbo.Notes', 'LastModifiedDate') IS NULL
BEGIN
    ALTER TABLE dbo.Notes ADD LastModifiedDate DATETIME NULL;
END;
GO
