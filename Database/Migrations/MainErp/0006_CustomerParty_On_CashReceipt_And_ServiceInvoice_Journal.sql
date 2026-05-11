IF OBJECT_ID('dbo.JournalEntryDetail', 'U') IS NOT NULL
   AND OBJECT_ID('dbo.JournalEntry', 'U') IS NOT NULL
   AND OBJECT_ID('dbo.CashReceiptVoucher', 'U') IS NOT NULL
   AND OBJECT_ID('dbo.Department', 'U') IS NOT NULL
   AND OBJECT_ID('dbo.SystemPage', 'U') IS NOT NULL
BEGIN
    UPDATE jed
    SET jed.PartyType = 1,
        jed.PartyId = crv.CustomerId
    FROM dbo.JournalEntryDetail jed
    INNER JOIN dbo.JournalEntry je
        ON je.Id = jed.JournalEntryId
    INNER JOIN dbo.CashReceiptVoucher crv
        ON crv.Id = je.SourceId
    INNER JOIN dbo.Department dep
        ON dep.Id = crv.DepartmentId
    WHERE je.SourcePageId = (
            SELECT TOP 1 Id
            FROM dbo.SystemPage
            WHERE ControllerName = 'CashReceiptVoucher'
               OR TableName = 'CashReceiptVoucher'
        )
      AND crv.SourceTypeId = 1
      AND ISNULL(je.IsDeleted, 0) = 0
      AND ISNULL(jed.IsDeleted, 0) = 0
      AND ISNULL(crv.IsDeleted, 0) = 0
      AND crv.CustomerId IS NOT NULL
      AND jed.AccountId = dep.CustomersAccountId
      AND ISNULL(jed.Credit, 0) > 0
      AND (jed.PartyType IS NULL OR jed.PartyType <> 1 OR jed.PartyId IS NULL OR jed.PartyId <> crv.CustomerId);
END
GO

IF OBJECT_ID('dbo.JournalEntryDetail', 'U') IS NOT NULL
   AND OBJECT_ID('dbo.JournalEntry', 'U') IS NOT NULL
   AND OBJECT_ID('dbo.ServiceInvoice', 'U') IS NOT NULL
   AND OBJECT_ID('dbo.Department', 'U') IS NOT NULL
   AND OBJECT_ID('dbo.SystemPage', 'U') IS NOT NULL
BEGIN
    UPDATE jed
    SET jed.PartyType = 1,
        jed.PartyId = si.CustomerId
    FROM dbo.JournalEntryDetail jed
    INNER JOIN dbo.JournalEntry je
        ON je.Id = jed.JournalEntryId
    INNER JOIN dbo.ServiceInvoice si
        ON si.Id = je.SourceId
    INNER JOIN dbo.Department dep
        ON dep.Id = si.DepartmentId
    WHERE je.SourcePageId = (
            SELECT TOP 1 Id
            FROM dbo.SystemPage
            WHERE ControllerName = 'ServiceInvoice'
               OR TableName = 'ServiceInvoice'
        )
      AND ISNULL(je.IsDeleted, 0) = 0
      AND ISNULL(jed.IsDeleted, 0) = 0
      AND ISNULL(si.IsDeleted, 0) = 0
      AND si.CustomerId IS NOT NULL
      AND jed.AccountId = dep.CustomersAccountId
      AND ISNULL(jed.Debit, 0) > 0
      AND (jed.PartyType IS NULL OR jed.PartyType <> 1 OR jed.PartyId IS NULL OR jed.PartyId <> si.CustomerId);
END
GO
