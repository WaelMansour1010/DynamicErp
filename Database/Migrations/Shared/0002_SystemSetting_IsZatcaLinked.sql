IF COL_LENGTH('dbo.SystemSetting', 'IsZatcaLinked') IS NULL
BEGIN
    ALTER TABLE dbo.SystemSetting
        ADD IsZatcaLinked bit NOT NULL
            CONSTRAINT DF_SystemSetting_IsZatcaLinked DEFAULT (0);
END
GO
