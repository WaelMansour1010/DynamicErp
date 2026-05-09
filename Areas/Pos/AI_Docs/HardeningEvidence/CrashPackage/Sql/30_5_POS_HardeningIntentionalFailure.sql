-- Hardening-only intentional failure script.
-- Verifies that the updater rolls back the whole script transaction and records failure.
IF OBJECT_ID(N'dbo.POS_HardeningRollbackProbe', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.POS_HardeningRollbackProbe
    (
        ProbeId INT NOT NULL CONSTRAINT PK_POS_HardeningRollbackProbe PRIMARY KEY,
        CreatedAt DATETIME NOT NULL CONSTRAINT DF_POS_HardeningRollbackProbe_CreatedAt DEFAULT (GETDATE())
    );
END;
RAISERROR('Intentional hardening failure after DDL to verify transaction rollback.', 16, 1);
