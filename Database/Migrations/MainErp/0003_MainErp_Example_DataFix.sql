/*
Migration number: 0003
Module: MainErp
Purpose: Example of a bounded, documented data fix.
Safe to rerun? Yes
Dependencies: None
Date: 2026-05-09
Author/Agent: Codex
*/

SET NOCOUNT ON;

/*
Data fix rule:
- Only touches rows where StatusName is NULL.
- Leaves existing values unchanged.
- Uses an explicit WHERE clause.
*/
IF OBJECT_ID(N'dbo.ExampleStatus', N'U') IS NOT NULL
BEGIN
    UPDATE dbo.ExampleStatus
    SET StatusName = N'Unknown'
    WHERE StatusName IS NULL
      AND IsDeleted = 0;
END;
GO

