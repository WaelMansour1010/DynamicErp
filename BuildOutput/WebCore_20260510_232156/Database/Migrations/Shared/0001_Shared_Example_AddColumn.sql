/*
Migration number: 0001
Module: Shared
Purpose: Example of an idempotent table/column migration.
Safe to rerun? Yes
Dependencies: None
Date: 2026-05-09
Author/Agent: Codex
*/

SET NOCOUNT ON;

IF OBJECT_ID(N'dbo.ExampleCustomer', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.ExampleCustomer', N'ExternalReference') IS NULL
BEGIN
    ALTER TABLE dbo.ExampleCustomer
    ADD ExternalReference NVARCHAR(100) NULL;
END;
GO

