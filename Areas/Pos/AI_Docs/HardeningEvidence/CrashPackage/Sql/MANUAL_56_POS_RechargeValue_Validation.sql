/*
    Diagnostic helper for RechargeValue overflow incidents.
    The application fix is in:
      - Areas/Pos/Controllers/PosTransactionController.cs
      - Areas/Pos/Data/PosSqlRepository.cs
      - Areas/Pos/Scripts/pos-transaction.js

    Read-only script. SQL Server 2012 compatible.
*/
SET NOCOUNT ON;

DECLARE @MaxAllowed DECIMAL(18, 2);
SET @MaxAllowed = 1000000.00;

SELECT TOP (100)
    Transaction_ID,
    Transaction_Date,
    BranchId,
    UserID,
    RechargeValue,
    NetValue,
    VAT,
    ManualNO,
    IPN
FROM dbo.Transactions WITH (READCOMMITTEDLOCK)
WHERE ISNULL(RechargeValue, 0) > @MaxAllowed
   OR ISNULL(RechargeValue, 0) > 2147483647
ORDER BY Transaction_ID DESC;

IF OBJECT_ID(N'dbo.POS_SystemErrorLog', N'U') IS NOT NULL
BEGIN
    SELECT TOP (100)
        CreatedAt,
        ActionName,
        BranchId,
        OperationType,
        TransactionId,
        ErrorMessage,
        RequestSummary
    FROM dbo.POS_SystemErrorLog WITH (READCOMMITTEDLOCK)
    WHERE CreatedAt >= DATEADD(DAY, -7, GETDATE())
      AND
      (
          ActionName LIKE N'%CalculateCommission%'
          OR RequestSummary LIKE N'%RechargeValue=%'
          OR ErrorMessage LIKE N'%Int32%'
      )
    ORDER BY CreatedAt DESC;
END;
