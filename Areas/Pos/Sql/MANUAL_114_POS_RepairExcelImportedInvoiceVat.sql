/*
    MANUAL_114_POS_RepairExcelImportedInvoiceVat.sql

    Purpose:
      Repair existing POS sales invoices imported from Excel where the VB6 sales form
      shows an empty/zero "القيمة المضافة" because Transactions.VAT does not match
      the sum of Transaction_Details.Vat.

    Safety:
      - Preview-only by default.
      - Targets Transaction_Type = 21 only.
      - Excludes traffic violations because they should not carry VAT.
      - Targets reliable Excel-import audit rows plus older estimated Excel rows
        saved with ExcelImport|... or TokenInvoiceExcel|... source keys.
      - Does not change detail rows, payments, stock, KYC, vouchers, or accounting.

    How to run:
      1. Set @FromDate / @ToDate / @BranchId if you want a narrower scope.
      2. Run with @PreviewOnly = 1 and review the result set.
      3. Change @PreviewOnly = 0 and run once to update.
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @PreviewOnly BIT = 1;
DECLARE @FromDate DATE = NULL; -- Example: '2026-04-01'
DECLARE @ToDate DATE = NULL;   -- Example: '2026-05-01' means before 2026-05-01
DECLARE @BranchId INT = NULL;
DECLARE @IncludeEstimatedExcel BIT = 1;
DECLARE @RunId UNIQUEIDENTIFIER = NEWID();

IF OBJECT_ID(N'dbo.POS_ExcelImportedInvoiceVatRepairAudit', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.POS_ExcelImportedInvoiceVatRepairAudit
    (
        AuditId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_POS_ExcelImportedInvoiceVatRepairAudit PRIMARY KEY,
        RunId UNIQUEIDENTIFIER NOT NULL,
        Transaction_ID INT NOT NULL,
        BranchId INT NULL,
        Transaction_Date DATETIME NULL,
        OldVAT MONEY NULL,
        NewVAT MONEY NOT NULL,
        DetailNet MONEY NULL,
        SourceKind NVARCHAR(50) NOT NULL,
        UpdatedAt DATETIME NOT NULL CONSTRAINT DF_POS_ExcelImportedInvoiceVatRepairAudit_UpdatedAt DEFAULT (GETDATE())
    );
END;

;WITH DetailTotals AS
(
    SELECT
        td.Transaction_ID,
        CAST(ISNULL(SUM(CONVERT(MONEY, ISNULL(td.Vat, 0))), 0) AS MONEY) AS DetailVAT,
        CAST(ISNULL(SUM(CONVERT(MONEY, ISNULL(td.Price, 0)) * CONVERT(MONEY, ISNULL(td.Quantity, 0))), 0) AS MONEY) AS DetailNet
    FROM dbo.Transaction_Details AS td WITH (NOLOCK)
    GROUP BY td.Transaction_ID
),
CandidateInvoices AS
(
    SELECT
        t.Transaction_ID,
        t.BranchId,
        t.Transaction_Date,
        CAST(ISNULL(t.VAT, 0) AS MONEY) AS OldVAT,
        d.DetailVAT AS NewVAT,
        d.DetailNet,
        CASE
            WHEN EXISTS
            (
                SELECT 1
                FROM dbo.POS_ImportBatchRow AS r WITH (NOLOCK)
                WHERE r.TransactionId = t.Transaction_ID
            )
            THEN N'Audited Excel Import'
            ELSE N'Estimated Excel Import'
        END AS SourceKind
    FROM dbo.Transactions AS t WITH (NOLOCK)
    INNER JOIN DetailTotals AS d ON d.Transaction_ID = t.Transaction_ID
    WHERE ISNULL(t.Transaction_Type, 0) = 21
      AND ISNULL(t.TrafficViolations, 0) = 0
      AND ISNULL(t.IsCancelled, 0) = 0
      AND d.DetailVAT > 0
      AND ISNULL(t.VAT, 0) <> d.DetailVAT
      AND (@FromDate IS NULL OR t.Transaction_Date >= @FromDate)
      AND (@ToDate IS NULL OR t.Transaction_Date < @ToDate)
      AND (@BranchId IS NULL OR t.BranchId = @BranchId)
      AND
      (
          EXISTS
          (
              SELECT 1
              FROM dbo.POS_ImportBatchRow AS r WITH (NOLOCK)
              WHERE r.TransactionId = t.Transaction_ID
          )
          OR
          (
              @IncludeEstimatedExcel = 1
              AND ISNULL(t.NoID, N'') = N'WEB_POS'
              AND
              (
                  ISNULL(t.ManualNo2, N'') LIKE N'ExcelImport|%'
                  OR ISNULL(t.ManualNo2, N'') LIKE N'TokenInvoiceExcel|%'
                  OR ISNULL(t.IPN, N'') LIKE N'ExcelImport|%'
                  OR ISNULL(t.IPN, N'') LIKE N'TokenInvoiceExcel|%'
                  OR ISNULL(t.ManualNO, N'') LIKE N'ExcelImport|%'
                  OR ISNULL(t.ManualNO, N'') LIKE N'TokenInvoiceExcel|%'
              )
          )
      )
)
SELECT
    PreviewOnly = @PreviewOnly,
    CandidateCount = COUNT(1),
    TotalOldVAT = SUM(OldVAT),
    TotalNewVAT = SUM(NewVAT),
    TotalDifference = SUM(NewVAT - OldVAT)
FROM CandidateInvoices;

;WITH DetailTotals AS
(
    SELECT
        td.Transaction_ID,
        CAST(ISNULL(SUM(CONVERT(MONEY, ISNULL(td.Vat, 0))), 0) AS MONEY) AS DetailVAT,
        CAST(ISNULL(SUM(CONVERT(MONEY, ISNULL(td.Price, 0)) * CONVERT(MONEY, ISNULL(td.Quantity, 0))), 0) AS MONEY) AS DetailNet
    FROM dbo.Transaction_Details AS td WITH (NOLOCK)
    GROUP BY td.Transaction_ID
),
CandidateInvoices AS
(
    SELECT
        t.Transaction_ID,
        t.NoteSerial1,
        t.ManualNO,
        t.IPN,
        t.BranchId,
        t.Transaction_Date,
        CAST(ISNULL(t.VAT, 0) AS MONEY) AS OldVAT,
        d.DetailVAT AS NewVAT,
        d.DetailNet,
        CASE
            WHEN EXISTS (SELECT 1 FROM dbo.POS_ImportBatchRow AS r WITH (NOLOCK) WHERE r.TransactionId = t.Transaction_ID)
            THEN N'Audited Excel Import'
            ELSE N'Estimated Excel Import'
        END AS SourceKind
    FROM dbo.Transactions AS t WITH (NOLOCK)
    INNER JOIN DetailTotals AS d ON d.Transaction_ID = t.Transaction_ID
    WHERE ISNULL(t.Transaction_Type, 0) = 21
      AND ISNULL(t.TrafficViolations, 0) = 0
      AND ISNULL(t.IsCancelled, 0) = 0
      AND d.DetailVAT > 0
      AND ISNULL(t.VAT, 0) <> d.DetailVAT
      AND (@FromDate IS NULL OR t.Transaction_Date >= @FromDate)
      AND (@ToDate IS NULL OR t.Transaction_Date < @ToDate)
      AND (@BranchId IS NULL OR t.BranchId = @BranchId)
      AND
      (
          EXISTS (SELECT 1 FROM dbo.POS_ImportBatchRow AS r WITH (NOLOCK) WHERE r.TransactionId = t.Transaction_ID)
          OR
          (
              @IncludeEstimatedExcel = 1
              AND ISNULL(t.NoID, N'') = N'WEB_POS'
              AND
              (
                  ISNULL(t.ManualNo2, N'') LIKE N'ExcelImport|%'
                  OR ISNULL(t.ManualNo2, N'') LIKE N'TokenInvoiceExcel|%'
                  OR ISNULL(t.IPN, N'') LIKE N'ExcelImport|%'
                  OR ISNULL(t.IPN, N'') LIKE N'TokenInvoiceExcel|%'
                  OR ISNULL(t.ManualNO, N'') LIKE N'ExcelImport|%'
                  OR ISNULL(t.ManualNO, N'') LIKE N'TokenInvoiceExcel|%'
              )
          )
      )
)
SELECT TOP (500)
    Transaction_ID,
    NoteSerial1,
    ManualNO,
    IPN,
    BranchId,
    Transaction_Date,
    OldVAT,
    NewVAT,
    Difference = NewVAT - OldVAT,
    DetailNet,
    SourceKind
FROM CandidateInvoices
ORDER BY Transaction_Date, Transaction_ID;

IF @PreviewOnly = 0
BEGIN
    BEGIN TRANSACTION;

    ;WITH DetailTotals AS
    (
        SELECT
            td.Transaction_ID,
            CAST(ISNULL(SUM(CONVERT(MONEY, ISNULL(td.Vat, 0))), 0) AS MONEY) AS DetailVAT,
            CAST(ISNULL(SUM(CONVERT(MONEY, ISNULL(td.Price, 0)) * CONVERT(MONEY, ISNULL(td.Quantity, 0))), 0) AS MONEY) AS DetailNet
        FROM dbo.Transaction_Details AS td WITH (NOLOCK)
        GROUP BY td.Transaction_ID
    ),
    CandidateInvoices AS
    (
        SELECT
            t.Transaction_ID,
            t.BranchId,
            t.Transaction_Date,
            CAST(ISNULL(t.VAT, 0) AS MONEY) AS OldVAT,
            d.DetailVAT AS NewVAT,
            d.DetailNet,
            CASE
                WHEN EXISTS (SELECT 1 FROM dbo.POS_ImportBatchRow AS r WITH (NOLOCK) WHERE r.TransactionId = t.Transaction_ID)
                THEN N'Audited Excel Import'
                ELSE N'Estimated Excel Import'
            END AS SourceKind
        FROM dbo.Transactions AS t WITH (UPDLOCK, HOLDLOCK)
        INNER JOIN DetailTotals AS d ON d.Transaction_ID = t.Transaction_ID
        WHERE ISNULL(t.Transaction_Type, 0) = 21
          AND ISNULL(t.TrafficViolations, 0) = 0
          AND ISNULL(t.IsCancelled, 0) = 0
          AND d.DetailVAT > 0
          AND ISNULL(t.VAT, 0) <> d.DetailVAT
          AND (@FromDate IS NULL OR t.Transaction_Date >= @FromDate)
          AND (@ToDate IS NULL OR t.Transaction_Date < @ToDate)
          AND (@BranchId IS NULL OR t.BranchId = @BranchId)
          AND
          (
              EXISTS (SELECT 1 FROM dbo.POS_ImportBatchRow AS r WITH (NOLOCK) WHERE r.TransactionId = t.Transaction_ID)
              OR
              (
                  @IncludeEstimatedExcel = 1
                  AND ISNULL(t.NoID, N'') = N'WEB_POS'
                  AND
                  (
                      ISNULL(t.ManualNo2, N'') LIKE N'ExcelImport|%'
                      OR ISNULL(t.ManualNo2, N'') LIKE N'TokenInvoiceExcel|%'
                      OR ISNULL(t.IPN, N'') LIKE N'ExcelImport|%'
                      OR ISNULL(t.IPN, N'') LIKE N'TokenInvoiceExcel|%'
                      OR ISNULL(t.ManualNO, N'') LIKE N'ExcelImport|%'
                      OR ISNULL(t.ManualNO, N'') LIKE N'TokenInvoiceExcel|%'
                  )
              )
          )
    )
    INSERT INTO dbo.POS_ExcelImportedInvoiceVatRepairAudit
    (
        RunId,
        Transaction_ID,
        BranchId,
        Transaction_Date,
        OldVAT,
        NewVAT,
        DetailNet,
        SourceKind
    )
    SELECT
        @RunId,
        Transaction_ID,
        BranchId,
        Transaction_Date,
        OldVAT,
        NewVAT,
        DetailNet,
        SourceKind
    FROM CandidateInvoices;

    UPDATE t
    SET VAT = a.NewVAT
    FROM dbo.Transactions AS t
    INNER JOIN dbo.POS_ExcelImportedInvoiceVatRepairAudit AS a
        ON a.Transaction_ID = t.Transaction_ID
       AND a.RunId = @RunId;

    COMMIT TRANSACTION;

    SELECT
        RunId = @RunId,
        UpdatedCount = COUNT(1),
        TotalOldVAT = SUM(OldVAT),
        TotalNewVAT = SUM(NewVAT),
        TotalDifference = SUM(NewVAT - OldVAT)
    FROM dbo.POS_ExcelImportedInvoiceVatRepairAudit
    WHERE RunId = @RunId;
END;
