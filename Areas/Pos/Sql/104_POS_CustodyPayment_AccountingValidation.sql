/*
    POS custody replenishment / treasury funding accounting validation.
    SQL Server 2012 compatible.
    Recreates only the custody preview/validate procedures with clear Arabic
    account-mapping errors and no fallback/default account behavior.
*/

IF OBJECT_ID(N'dbo.usp_POS_CustodyFundingRefund_Preview', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_POS_CustodyFundingRefund_Preview
GO

CREATE PROCEDURE dbo.usp_POS_CustodyFundingRefund_Preview
    @BranchId INT,
    @OperationType INT,
    @Amount DECIMAL(19, 4),
    @MainAccountCode NVARCHAR(255),
    @PaymentMethod INT,
    @BoxID INT = NULL,
    @BankID INT = NULL,
    @ChequeOrTransferNumber NVARCHAR(100) = NULL,
    @ChequeOrTransferDate DATETIME = NULL,
    @EmployeeID INT = NULL,
    @BoxValue DECIMAL(19, 4) = NULL,
    @EmployeeValue DECIMAL(19, 4) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @LegacyCashingType INT;
    DECLARE @MainBoxType INT;
    DECLARE @CreditAccountCode NVARCHAR(255);
    DECLARE @EmployeeAccountCode NVARCHAR(255);
    DECLARE @ValidationMessage NVARCHAR(MAX);
    DECLARE @DebitTotal DECIMAL(19, 4);
    DECLARE @CreditTotal DECIMAL(19, 4);

    SET @ValidationMessage = N'';
    SET @DebitTotal = 0;
    SET @CreditTotal = 0;
    SET @BoxValue = ISNULL(@BoxValue, 0);
    SET @EmployeeValue = ISNULL(@EmployeeValue, 0);
    SET @LegacyCashingType = CASE
        WHEN @OperationType IN (0, 1) THEN @OperationType + 5
        WHEN @OperationType IN (5, 6) THEN @OperationType
        ELSE NULL
    END;
    SET @MainBoxType = CASE WHEN @LegacyCashingType = 6 THEN 0 ELSE 1 END;

    IF @LegacyCashingType NOT IN (5, 6)
        SET @ValidationMessage = @ValidationMessage + N'نوع الحركة غير صحيح. ';

    IF ISNULL(@Amount, 0) <= 0
        SET @ValidationMessage = @ValidationMessage + N'يجب إدخال قيمة أكبر من صفر. ';

    IF ISNULL(LTRIM(RTRIM(@MainAccountCode)), N'') = N''
        SET @ValidationMessage = @ValidationMessage + N'حساب صاحب العهدة / الخزينة مطلوب. ';

    IF NOT EXISTS (
        SELECT 1
        FROM dbo.TblBoxesData b
        WHERE b.Account_Code = @MainAccountCode
          AND b.[Type] = @MainBoxType
          AND (ISNULL(b.BranchId, 0) = 0 OR b.BranchId = @BranchId)
    )
        SET @ValidationMessage = @ValidationMessage + N'حساب الحركة الرئيسي غير مرتبط بالفرع أو نوع الحركة المحدد. ';

    IF NOT EXISTS (
        SELECT 1
        FROM dbo.ACCOUNTS a
        WHERE a.Account_Code = @MainAccountCode
          AND ISNULL(a.last_account, 0) = 1
    )
        SET @ValidationMessage = @ValidationMessage + N'حساب الحركة الرئيسي غير موجود في دليل الحسابات أو ليس حساباً نهائياً. ';

    IF @PaymentMethod = 0
    BEGIN
        SELECT @CreditAccountCode = NULLIF(LTRIM(RTRIM(b.Account_Code)), N'')
        FROM dbo.TblBoxesData b
        WHERE b.BoxID = @BoxID
          AND (ISNULL(b.BranchId, 0) = 0 OR b.BranchId = @BranchId);

        IF @CreditAccountCode IS NULL
            SET @ValidationMessage = @ValidationMessage + N'الدفع النقدي يتطلب خزنة مرتبطة بالفرع المحدد وبها حساب. ';
    END
    ELSE IF @PaymentMethod IN (1, 2, 3)
    BEGIN
        SELECT @CreditAccountCode = NULLIF(LTRIM(RTRIM(bank.Account_Code)), N'')
        FROM dbo.BanksData bank
        WHERE bank.BankID = @BankID
          AND (ISNULL(bank.BranchId, 0) = 0 OR bank.BranchId = @BranchId);

        IF @CreditAccountCode IS NULL
            SET @ValidationMessage = @ValidationMessage + N'الدفع البنكي يتطلب بنكاً مرتبطاً بالفرع المحدد وبه حساب. ';

        IF ISNULL(LTRIM(RTRIM(@ChequeOrTransferNumber)), N'') = N''
            SET @ValidationMessage = @ValidationMessage + N'رقم الشيك أو الحوالة مطلوب. ';

        IF @ChequeOrTransferDate IS NULL
            SET @ValidationMessage = @ValidationMessage + N'تاريخ الشيك أو الحوالة مطلوب. ';
    END
    ELSE
        SET @ValidationMessage = @ValidationMessage + N'طريقة الدفع غير صحيحة. ';

    IF @CreditAccountCode IS NOT NULL
       AND NOT EXISTS (
           SELECT 1
           FROM dbo.ACCOUNTS a
           WHERE a.Account_Code = @CreditAccountCode
             AND ISNULL(a.last_account, 0) = 1
       )
        SET @ValidationMessage = @ValidationMessage + N'حساب مصدر الدفع غير موجود في دليل الحسابات أو ليس حساباً نهائياً. ';

    IF @EmployeeID IS NOT NULL
    BEGIN
        SELECT @EmployeeAccountCode = NULLIF(LTRIM(RTRIM(emp.Account_Code)), N'')
        FROM dbo.TblEmployee emp
        WHERE emp.Emp_ID = @EmployeeID
          AND (ISNULL(emp.BranchId, 0) = 0 OR emp.BranchId = @BranchId);

        IF @EmployeeAccountCode IS NULL
            SET @ValidationMessage = @ValidationMessage + N'الموظف غير مرتبط بالفرع المحدد أو لا يوجد له حساب عهدة. ';
        ELSE IF NOT EXISTS (
            SELECT 1
            FROM dbo.ACCOUNTS a
            WHERE a.Account_Code = @EmployeeAccountCode
              AND ISNULL(a.last_account, 0) = 1
        )
            SET @ValidationMessage = @ValidationMessage + N'حساب عهدة الموظف غير موجود في دليل الحسابات أو ليس حساباً نهائياً. ';
    END

    IF @BoxValue <> 0 OR @EmployeeValue <> 0
        SET @ValidationMessage = @ValidationMessage + N'استعاضة العهدة تسجل قيداً من طرفين فقط؛ لا تستخدم تقسيم قيمة الخزنة/عهدة الموظف. ';

    DECLARE @Rows TABLE
    (
        [LineNo] INT IDENTITY(1, 1),
        AccountCode NVARCHAR(255),
        AccountSerial NVARCHAR(255),
        AccountName NVARCHAR(255),
        AccountNameEng NVARCHAR(255),
        DisplayName NVARCHAR(700),
        Debit DECIMAL(19, 4),
        Credit DECIMAL(19, 4),
        CreditOrDebit INT,
        Source NVARCHAR(50)
    );

    IF @ValidationMessage = N''
    BEGIN
        INSERT INTO @Rows (AccountCode, AccountSerial, AccountName, AccountNameEng, DisplayName, Debit, Credit, CreditOrDebit, Source)
        SELECT a.Account_Code, a.Account_Serial, a.Account_Name, a.Account_NameEng,
               LTRIM(RTRIM(COALESCE(CONVERT(NVARCHAR(255), a.Account_Serial) + N' - ', N'') + COALESCE(a.Account_Name, N''))),
               @Amount, 0, 0, N'MainAccount'
        FROM dbo.ACCOUNTS a
        WHERE a.Account_Code = @MainAccountCode
          AND ISNULL(a.last_account, 0) = 1;

        INSERT INTO @Rows (AccountCode, AccountSerial, AccountName, AccountNameEng, DisplayName, Debit, Credit, CreditOrDebit, Source)
        SELECT a.Account_Code, a.Account_Serial, a.Account_Name, a.Account_NameEng,
               LTRIM(RTRIM(COALESCE(CONVERT(NVARCHAR(255), a.Account_Serial) + N' - ', N'') + COALESCE(a.Account_Name, N''))),
               0, @Amount, 1, CASE WHEN @PaymentMethod = 0 THEN N'Treasury' ELSE N'Bank' END
        FROM dbo.ACCOUNTS a
        WHERE a.Account_Code = @CreditAccountCode
          AND ISNULL(a.last_account, 0) = 1;
    END

    SELECT @DebitTotal = ISNULL(SUM(Debit), 0), @CreditTotal = ISNULL(SUM(Credit), 0)
    FROM @Rows;

    IF @ValidationMessage = N'' AND ROUND(@DebitTotal, 4) <> ROUND(@CreditTotal, 4)
        SET @ValidationMessage = @ValidationMessage + N'إجمالي المدين يجب أن يساوي إجمالي الدائن. ';

    SELECT
        CAST(CASE WHEN @ValidationMessage = N'' THEN 1 ELSE 0 END AS BIT) AS CanSave,
        @ValidationMessage AS ValidationMessage,
        @DebitTotal AS TotalDebit,
        @CreditTotal AS TotalCredit;

    SELECT [LineNo], AccountCode, AccountSerial, AccountName, AccountNameEng, DisplayName, Debit, Credit, CreditOrDebit, Source
    FROM @Rows
    ORDER BY [LineNo];
END
GO

IF OBJECT_ID(N'dbo.usp_POS_CustodyFundingRefund_ValidateSave', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_POS_CustodyFundingRefund_ValidateSave
GO

CREATE PROCEDURE dbo.usp_POS_CustodyFundingRefund_ValidateSave
    @BranchId INT,
    @OperationType INT,
    @Amount DECIMAL(19, 4),
    @MainAccountCode NVARCHAR(255),
    @PaymentMethod INT,
    @BoxID INT = NULL,
    @BankID INT = NULL,
    @ChequeOrTransferNumber NVARCHAR(100) = NULL,
    @ChequeOrTransferDate DATETIME = NULL,
    @EmployeeID INT = NULL,
    @BoxValue DECIMAL(19, 4) = NULL,
    @EmployeeValue DECIMAL(19, 4) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    EXEC dbo.usp_POS_CustodyFundingRefund_Preview
        @BranchId = @BranchId,
        @OperationType = @OperationType,
        @Amount = @Amount,
        @MainAccountCode = @MainAccountCode,
        @PaymentMethod = @PaymentMethod,
        @BoxID = @BoxID,
        @BankID = @BankID,
        @ChequeOrTransferNumber = @ChequeOrTransferNumber,
        @ChequeOrTransferDate = @ChequeOrTransferDate,
        @EmployeeID = @EmployeeID,
        @BoxValue = @BoxValue,
        @EmployeeValue = @EmployeeValue;
END
GO
