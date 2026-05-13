IF OBJECT_ID(N'dbo.POS_ImportBatch', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.POS_ImportBatch', N'BranchId') IS NULL
BEGIN
    ALTER TABLE dbo.POS_ImportBatch ADD BranchId INT NULL;
END;
