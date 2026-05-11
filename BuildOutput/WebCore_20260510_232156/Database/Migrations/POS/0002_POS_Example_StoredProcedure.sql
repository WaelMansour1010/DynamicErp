/*
Migration number: 0002
Module: POS
Purpose: Example of a SQL Server 2012 compatible DROP + CREATE stored procedure migration.
Safe to rerun? Yes
Dependencies: None
Date: 2026-05-09
Author/Agent: Codex
*/

SET NOCOUNT ON;

IF OBJECT_ID(N'dbo.usp_POS_ExampleHealthCheck', N'P') IS NOT NULL
BEGIN
    DROP PROCEDURE dbo.usp_POS_ExampleHealthCheck;
END;
GO

CREATE PROCEDURE dbo.usp_POS_ExampleHealthCheck
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        DB_NAME() AS DatabaseName,
        GETDATE() AS CheckedOn;
END;
GO

