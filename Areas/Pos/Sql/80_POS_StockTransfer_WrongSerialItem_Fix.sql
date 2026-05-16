DECLARE @procedureDefinition NVARCHAR(MAX);

SELECT @procedureDefinition = OBJECT_DEFINITION(OBJECT_ID(N'dbo.usp_POS_SaveStockTransfer'));

IF @procedureDefinition IS NULL
BEGIN
    RAISERROR(N'dbo.usp_POS_SaveStockTransfer was not found. Run 42_POS_StockTransfer.sql first.', 16, 1);
    RETURN;
END;

SET @procedureDefinition = REPLACE(
    @procedureDefinition,
    N'WHERE ISNULL(serialItem.Item_ID, -1) <> it.ItemId',
    N'WHERE serialItem.Item_ID IS NOT NULL
          AND serialItem.Item_ID <> it.ItemId'
);

IF @procedureDefinition NOT LIKE N'%serialItem.Item_ID IS NOT NULL%'
BEGIN
    RAISERROR(N'Unable to apply POS stock transfer wrong-serial-item fix.', 16, 1);
    RETURN;
END;

DROP PROCEDURE dbo.usp_POS_SaveStockTransfer;

EXEC (@procedureDefinition);
GO
