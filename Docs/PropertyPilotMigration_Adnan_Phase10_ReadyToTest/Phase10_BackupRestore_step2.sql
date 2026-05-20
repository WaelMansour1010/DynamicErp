IF DB_ID(N'Alromaizan_PropertyPilot_Adnan_ReadyToTest_20260520') IS NOT NULL
BEGIN
    RAISERROR('Target DB already exists. Restore cancelled.', 16, 1);
    RETURN;
END;
RESTORE DATABASE [Alromaizan_PropertyPilot_Adnan_ReadyToTest_20260520]
FROM DISK = N'F:\Source Code\DynamicErp\Docs\PropertyPilotMigration_Adnan_Phase10_ReadyToTest\Alromaizan_PropertyPilot_Adnan_PilotClone_20260520_to_Alromaizan_PropertyPilot_Adnan_ReadyToTest_20260520.bak'
WITH MOVE N'MyERP' TO N'F:\DataBase\MyErp\Alromaizan_PropertyPilot_Adnan_ReadyToTest_20260520\Alromaizan_PropertyPilot_Adnan_ReadyToTest_20260520.mdf',
     MOVE N'MyERP_log' TO N'F:\DataBase\MyErp\Alromaizan_PropertyPilot_Adnan_ReadyToTest_20260520\Alromaizan_PropertyPilot_Adnan_ReadyToTest_20260520_log.ldf',
     RECOVERY, STATS = 10;
SELECT name, state_desc, create_date FROM sys.databases WHERE name=N'Alromaizan_PropertyPilot_Adnan_ReadyToTest_20260520';
