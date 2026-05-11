IF COL_LENGTH(N'dbo.Transactions', N'IsCancelled') IS NULL
BEGIN
    ALTER TABLE dbo.Transactions ADD IsCancelled BIT NOT NULL CONSTRAINT DF_Transactions_IsCancelled DEFAULT(0);
END;
GO

IF COL_LENGTH(N'dbo.Transactions', N'CancelledBy') IS NULL
BEGIN
    ALTER TABLE dbo.Transactions ADD CancelledBy INT NULL;
END;
GO

IF COL_LENGTH(N'dbo.Transactions', N'CancelledDate') IS NULL
BEGIN
    ALTER TABLE dbo.Transactions ADD CancelledDate DATETIME NULL;
END;
GO

IF COL_LENGTH(N'dbo.Transactions', N'CancelReason') IS NULL
BEGIN
    ALTER TABLE dbo.Transactions ADD CancelReason NVARCHAR(500) NULL;
END;
GO

IF COL_LENGTH(N'dbo.Transactions', N'CancelReverseTransaction_ID') IS NULL
BEGIN
    ALTER TABLE dbo.Transactions ADD CancelReverseTransaction_ID FLOAT NULL;
END;
GO

IF OBJECT_ID(N'dbo.usp_POS_CancelInvoice', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_POS_CancelInvoice;
GO

CREATE PROCEDURE dbo.usp_POS_CancelInvoice
    @TransactionId INT,
    @CancelledBy INT,
    @CancelReason NVARCHAR(500) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @Now DATETIME = GETDATE();
    DECLARE @BranchId INT;
    DECLARE @OriginalNoteId INT;

    BEGIN TRY
        BEGIN TRAN;

        SELECT
            @BranchId = t.BranchId
        FROM dbo.Transactions t WITH (UPDLOCK, HOLDLOCK)
        WHERE t.Transaction_ID = @TransactionId
          AND t.Transaction_Type = 21;

        IF @BranchId IS NULL
            THROW 50001, N'الفاتورة غير موجودة', 1;

        IF EXISTS
        (
            SELECT 1
            FROM dbo.Transactions t
            WHERE t.Transaction_ID = @TransactionId
              AND ISNULL(t.IsCancelled, 0) = 1
        )
            THROW 50002, N'تم إلغاء الفاتورة مسبقاً', 1;

        IF EXISTS
        (
            SELECT 1
            FROM dbo.Transactions t
            WHERE t.Transaction_ID = @TransactionId
              AND CAST(t.Transaction_Date AS DATE) <> CAST(@Now AS DATE)
        )
            THROW 50003, N'لا يمكن إلغاء الفاتورة إلا في نفس يوم إصدارها', 1;

        IF EXISTS
        (
            SELECT 1
            FROM dbo.Transactions t
            WHERE t.Transaction_ID = @TransactionId
              AND (NULLIF(LTRIM(RTRIM(ISNULL(t.VisaNumber, N''))), N'') IS NOT NULL OR ISNULL(t.TrafficViolations, 0) = 1)
        )
            THROW 50004, N'إلغاء الفاتورة مسموح فقط لـ Cash In / Cash Out', 1;

        SELECT TOP (1) @OriginalNoteId = n.NoteID
        FROM dbo.Notes n WITH (UPDLOCK, HOLDLOCK)
        WHERE n.Transaction_ID = @TransactionId
        ORDER BY n.NoteID;

        IF @OriginalNoteId IS NOT NULL
        BEGIN
            UPDATE d
            SET d.Credit_Or_Debit = CASE WHEN ISNULL(d.Credit_Or_Debit, 0) = 0 THEN 1 ELSE 0 END,
                d.Double_Entry_Vouchers_Description =
                    N'قيد مقلوب بعد إلغاء فاتورة رقم ' + CONVERT(NVARCHAR(50), @TransactionId),
                d.RecordDate = @Now,
                d.UserID = @CancelledBy,
                d.branch_id = ISNULL(d.branch_id, @BranchId),
                d.DueDate = @Now
            FROM dbo.DOUBLE_ENTREY_VOUCHERS d WITH (UPDLOCK, HOLDLOCK)
            WHERE d.Notes_ID = @OriginalNoteId;

            UPDATE n
            SET n.NoteDate = @Now,
                n.UserID = @CancelledBy,
                n.Remark = N'تم قلب القيد بعد إلغاء فاتورة رقم ' + CONVERT(NVARCHAR(50), @TransactionId),
                n.general_des_notes = N'POS Invoice Canceled - Original Journal Flipped'
            FROM dbo.Notes n WITH (UPDLOCK, HOLDLOCK)
            WHERE n.NoteID = @OriginalNoteId;
        END

        UPDATE dbo.Transactions
        SET IsCancelled = 1,
            CancelledBy = @CancelledBy,
            CancelledDate = @Now,
            CancelReason = NULLIF(LTRIM(RTRIM(@CancelReason)), N''),
            CancelReverseTransaction_ID = NULL
        WHERE Transaction_ID = @TransactionId
          AND Transaction_Type = 21
          AND ISNULL(IsCancelled, 0) = 0;

        IF @@ROWCOUNT = 0
            THROW 50005, N'تعذر إلغاء الفاتورة', 1;

        COMMIT;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        THROW;
    END CATCH
END;
GO


Select DepartmentId from JournalEntryDetail

IF COL_LENGTH('dbo.JournalEntryDetail', 'DepartmentId') IS NULL
IF OBJECT_ID('dbo.JournalEntryDetail', 'U') IS NOT NULL
   AND COL_LENGTH('dbo.JournalEntryDetail', 'DepartmentId') IS NULL
BEGIN
Select * from Department