IF OBJECT_ID(N'dbo.MainErpLegacyAttachments', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.MainErpLegacyAttachments
    (
        Id int IDENTITY(1,1) NOT NULL CONSTRAINT PK_MainErpLegacyAttachments PRIMARY KEY,
        ScreenName nvarchar(100) NOT NULL,
        RecordId int NOT NULL,
        FileName nvarchar(255) NOT NULL,
        FilePath nvarchar(500) NOT NULL,
        ContentType nvarchar(100) NULL,
        FileSize bigint NOT NULL CONSTRAINT DF_MainErpLegacyAttachments_FileSize DEFAULT(0),
        IsPrimary bit NOT NULL CONSTRAINT DF_MainErpLegacyAttachments_IsPrimary DEFAULT(0),
        Caption nvarchar(300) NULL,
        CreatedAt datetime NOT NULL CONSTRAINT DF_MainErpLegacyAttachments_CreatedAt DEFAULT(GETDATE()),
        CreatedBy int NULL
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_MainErpLegacyAttachments_Record' AND object_id = OBJECT_ID(N'dbo.MainErpLegacyAttachments'))
BEGIN
    CREATE INDEX IX_MainErpLegacyAttachments_Record
    ON dbo.MainErpLegacyAttachments(ScreenName, RecordId, IsPrimary DESC, CreatedAt DESC);
END
GO
