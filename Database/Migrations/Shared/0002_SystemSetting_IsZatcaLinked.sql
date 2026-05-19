/*
Migration number: 0002
Module: Shared
Purpose: Add the ZATCA integration completion flag used by the main web warning modal.
Safe to rerun? Yes
Dependencies: dbo.SystemSetting
Date: 2026-05-19
Author/Agent: Codex
*/

SET NOCOUNT ON;

IF COL_LENGTH('dbo.SystemSetting', 'IsZatcaLinked') IS NULL
BEGIN
    ALTER TABLE dbo.SystemSetting
        ADD IsZatcaLinked bit NOT NULL
            CONSTRAINT DF_SystemSetting_IsZatcaLinked DEFAULT (0);
END
GO
