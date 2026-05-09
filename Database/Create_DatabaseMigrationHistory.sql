/*
Migration number: 0000
Module: Shared
Purpose: Create the database migration history table used by DynamicErp migration runner.
Safe to rerun? Yes
Dependencies: None
Date: 2026-05-09
Author/Agent: Codex
*/

SET NOCOUNT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;

IF OBJECT_ID(N'dbo.DatabaseMigrationHistory', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DatabaseMigrationHistory
    (
        MigrationId INT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_DatabaseMigrationHistory PRIMARY KEY,
        ScriptName NVARCHAR(260) NOT NULL,
        ScriptPath NVARCHAR(1000) NOT NULL,
        ScriptHash CHAR(64) NOT NULL,
        ModuleName NVARCHAR(100) NOT NULL,
        AppliedOn DATETIME NOT NULL
            CONSTRAINT DF_DatabaseMigrationHistory_AppliedOn DEFAULT (GETDATE()),
        AppliedBy NVARCHAR(256) NOT NULL,
        MachineName NVARCHAR(128) NOT NULL,
        DatabaseName SYSNAME NOT NULL,
        DurationMs INT NULL,
        Success BIT NOT NULL,
        ErrorMessage NVARCHAR(MAX) NULL,
        BatchNo NVARCHAR(100) NULL,
        ReleaseNo NVARCHAR(100) NULL
    );
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'UX_DatabaseMigrationHistory_ScriptName_ScriptHash_Success'
      AND object_id = OBJECT_ID(N'dbo.DatabaseMigrationHistory', N'U')
)
BEGIN
    CREATE UNIQUE INDEX UX_DatabaseMigrationHistory_ScriptName_ScriptHash_Success
    ON dbo.DatabaseMigrationHistory (ScriptName, ScriptHash, Success)
    WHERE Success = 1;
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_DatabaseMigrationHistory_ModuleName_AppliedOn'
      AND object_id = OBJECT_ID(N'dbo.DatabaseMigrationHistory', N'U')
)
BEGIN
    CREATE INDEX IX_DatabaseMigrationHistory_ModuleName_AppliedOn
    ON dbo.DatabaseMigrationHistory (ModuleName, AppliedOn);
END;
GO

IF OBJECT_ID(N'dbo.DatabaseMigrationRun', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DatabaseMigrationRun
    (
        RunId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_DatabaseMigrationRun PRIMARY KEY,
        StartedAt DATETIME NOT NULL CONSTRAINT DF_DatabaseMigrationRun_StartedAt DEFAULT (GETDATE()),
        FinishedAt DATETIME NULL,
        StartedBy NVARCHAR(256) NOT NULL,
        DatabaseName SYSNAME NOT NULL,
        ServerName NVARCHAR(128) NOT NULL,
        Mode NVARCHAR(20) NOT NULL,
        Status NVARCHAR(30) NOT NULL,
        TotalScripts INT NOT NULL CONSTRAINT DF_DatabaseMigrationRun_TotalScripts DEFAULT (0),
        AppliedCount INT NOT NULL CONSTRAINT DF_DatabaseMigrationRun_AppliedCount DEFAULT (0),
        FailedCount INT NOT NULL CONSTRAINT DF_DatabaseMigrationRun_FailedCount DEFAULT (0),
        WarningCount INT NOT NULL CONSTRAINT DF_DatabaseMigrationRun_WarningCount DEFAULT (0)
    );
END;
GO

IF OBJECT_ID(N'dbo.DatabaseMigrationRunDetail', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DatabaseMigrationRunDetail
    (
        RunDetailId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_DatabaseMigrationRunDetail PRIMARY KEY,
        RunId INT NOT NULL,
        ScriptName NVARCHAR(260) NOT NULL,
        ModuleName NVARCHAR(100) NOT NULL,
        Status NVARCHAR(30) NOT NULL,
        DurationMs INT NULL,
        ErrorMessage NVARCHAR(MAX) NULL,
        ScriptHash CHAR(64) NOT NULL,
        CONSTRAINT FK_DatabaseMigrationRunDetail_Run FOREIGN KEY (RunId) REFERENCES dbo.DatabaseMigrationRun(RunId)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_DatabaseMigrationRunDetail_RunId' AND object_id = OBJECT_ID(N'dbo.DatabaseMigrationRunDetail', N'U'))
BEGIN
    CREATE INDEX IX_DatabaseMigrationRunDetail_RunId ON dbo.DatabaseMigrationRunDetail (RunId);
END;
GO
