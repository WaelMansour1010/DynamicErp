SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

IF OBJECT_ID(N'dbo.usp_POS_CustodyFundingRefund_GetLookups', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_POS_CustodyFundingRefund_GetLookups
GO
CREATE PROCEDURE dbo.usp_POS_CustodyFundingRefund_GetLookups
    @BranchId INT,
    @OperationType INT = NULL,
    @SelectedMainAccountCode NVARCHAR(255) = NULL,
    @SelectedBoxID INT = NULL,
    @SelectedBankID INT = NULL,
    @SelectedEmployeeID INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @LegacyCashingType INT = CASE
        WHEN @OperationType IN (0, 1) THEN @OperationType + 5
        WHEN @OperationType IN (5, 6) THEN @OperationType
        ELSE 5
    END;
    DECLARE @MainBoxType INT = CASE WHEN @LegacyCashingType = 6 THEN 0 ELSE 1 END;

    SELECT 5 AS OperationType, 1 AS BoxType, N'استعاضة عهدة' AS OperationName, N'Custody funding/refund' AS OperationNameE
    UNION ALL
    SELECT 6 AS OperationType, 0 AS BoxType, N'تمويل خزينة' AS OperationName, N'Treasury funding' AS OperationNameE;

    SELECT 0 AS PaymentMethod, N'نقدي' AS PaymentMethodName, CAST(1 AS BIT) AS RequiresBox, CAST(0 AS BIT) AS RequiresBank, CAST(0 AS BIT) AS RequiresNumber, CAST(0 AS BIT) AS RequiresDate
    UNION ALL
    SELECT 1, N'شيك', CAST(0 AS BIT), CAST(1 AS BIT), CAST(1 AS BIT), CAST(1 AS BIT)
    UNION ALL
    SELECT 2, N'حوالة', CAST(0 AS BIT), CAST(1 AS BIT), CAST(1 AS BIT), CAST(1 AS BIT)
    UNION ALL
    SELECT 3, N'شيك مسدد', CAST(0 AS BIT), CAST(1 AS BIT), CAST(1 AS BIT), CAST(1 AS BIT);

    SELECT
        b.Account_Code AS AccountCode,
        b.BoxID,
        b.BoxName,
        b.BoxNamee,
        b.BranchId,
        b.Type AS BoxType,
        a.Account_Serial AS AccountSerial,
        a.Account_Name AS AccountName,
        a.Account_NameEng AS AccountNameEng,
        LTRIM(RTRIM(COALESCE(CONVERT(NVARCHAR(255), a.Account_Serial) + N' - ', N'') + COALESCE(a.Account_Name, b.BoxName, N''))) AS DisplayName
    FROM dbo.TblBoxesData b
    LEFT JOIN dbo.ACCOUNTS a ON a.Account_Code = b.Account_Code
    WHERE b.Type = @MainBoxType
      AND ISNULL(b.Account_Code, N'') <> N''
      AND (ISNULL(b.BranchId, 0) = 0 OR b.BranchId = @BranchId)
    ORDER BY b.BoxName;

    SELECT
        b.BoxID,
        b.Account_Code AS AccountCode,
        b.BoxName,
        b.BoxNamee,
        b.BranchId,
        b.Type AS BoxType,
        a.Account_Serial AS AccountSerial,
        a.Account_Name AS AccountName,
        a.Account_NameEng AS AccountNameEng,
        LTRIM(RTRIM(COALESCE(CONVERT(NVARCHAR(255), a.Account_Serial) + N' - ', N'') + COALESCE(a.Account_Name, b.BoxName, N''))) AS DisplayName
    FROM dbo.TblBoxesData b
    LEFT JOIN dbo.ACCOUNTS a ON a.Account_Code = b.Account_Code
    WHERE ISNULL(b.Account_Code, N'') <> N''
      AND (ISNULL(b.BranchId, 0) = 0 OR b.BranchId = @BranchId)
    ORDER BY b.BoxName;

    SELECT
        bank.BankID,
        bank.BankName,
        bank.BankNameE,
        bank.BranchId,
        bank.Account_Code AS AccountCode,
        a.Account_Serial AS AccountSerial,
        a.Account_Name AS AccountName,
        a.Account_NameEng AS AccountNameEng,
        LTRIM(RTRIM(COALESCE(CONVERT(NVARCHAR(255), a.Account_Serial) + N' - ', N'') + COALESCE(a.Account_Name, bank.BankName, N''))) AS DisplayName
    FROM dbo.BanksData bank
    LEFT JOIN dbo.ACCOUNTS a ON a.Account_Code = bank.Account_Code
    WHERE ISNULL(bank.Account_Code, N'') <> N''
      AND (ISNULL(bank.BranchId, 0) = 0 OR bank.BranchId = @BranchId)
    ORDER BY bank.BankName;

    SELECT
        emp.Emp_ID AS EmployeeID,
        emp.Emp_Name AS EmployeeName,
        emp.Emp_Namee AS EmployeeNameE,
        emp.FullCode AS EmployeeCode,
        emp.BranchId,
        emp.Account_Code AS CustodyAccountCode,
        a.Account_Serial AS AccountSerial,
        a.Account_Name AS AccountName,
        a.Account_NameEng AS AccountNameEng,
        CASE WHEN ISNULL(emp.Account_Code, N'') = N'' THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS MissingCustodyAccount,
        LTRIM(RTRIM(COALESCE(CONVERT(NVARCHAR(255), a.Account_Serial) + N' - ', N'') + COALESCE(a.Account_Name, emp.Emp_Name, N''))) AS DisplayName
    FROM dbo.TblEmployee emp
    LEFT JOIN dbo.ACCOUNTS a ON a.Account_Code = emp.Account_Code
    WHERE (ISNULL(emp.BranchId, 0) = 0 OR emp.BranchId = @BranchId)
    ORDER BY emp.Emp_Name;

    SELECT
        CAST(CASE WHEN @SelectedMainAccountCode IS NULL OR EXISTS (
            SELECT 1 FROM dbo.TblBoxesData b
            WHERE b.Account_Code = @SelectedMainAccountCode
              AND b.Type = @MainBoxType
              AND (ISNULL(b.BranchId, 0) = 0 OR b.BranchId = @BranchId)
        ) THEN 1 ELSE 0 END AS BIT) AS IsMainAccountValid,
        CAST(CASE WHEN @SelectedBoxID IS NULL OR EXISTS (
            SELECT 1 FROM dbo.TblBoxesData b
            WHERE b.BoxID = @SelectedBoxID
              AND (ISNULL(b.BranchId, 0) = 0 OR b.BranchId = @BranchId)
        ) THEN 1 ELSE 0 END AS BIT) AS IsBoxValid,
        CAST(CASE WHEN @SelectedBankID IS NULL OR EXISTS (
            SELECT 1 FROM dbo.BanksData bank
            WHERE bank.BankID = @SelectedBankID
              AND (ISNULL(bank.BranchId, 0) = 0 OR bank.BranchId = @BranchId)
        ) THEN 1 ELSE 0 END AS BIT) AS IsBankValid,
        CAST(CASE WHEN @SelectedEmployeeID IS NULL OR EXISTS (
            SELECT 1 FROM dbo.TblEmployee emp
            WHERE emp.Emp_ID = @SelectedEmployeeID
              AND (ISNULL(emp.BranchId, 0) = 0 OR emp.BranchId = @BranchId)
        ) THEN 1 ELSE 0 END AS BIT) AS IsEmployeeValid;
END
GO

IF OBJECT_ID(N'dbo.usp_POS_CustodyFundingRefund_GetAccountBalance', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_POS_CustodyFundingRefund_GetAccountBalance
GO
CREATE PROCEDURE dbo.usp_POS_CustodyFundingRefund_GetAccountBalance
    @BranchId INT,
    @AccountCode NVARCHAR(255),
    @AsOfDate DATETIME = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        a.Account_Code AS AccountCode,
        a.Account_Serial AS AccountSerial,
        a.Account_Name AS AccountName,
        a.Account_NameEng AS AccountNameEng,
        LTRIM(RTRIM(COALESCE(CONVERT(NVARCHAR(255), a.Account_Serial) + N' - ', N'') + COALESCE(a.Account_Name, N''))) AS DisplayName,
        ISNULL(SUM(CASE WHEN dev.Credit_Or_Debit = 0 THEN ISNULL(dev.[Value], 0) ELSE -ISNULL(dev.[Value], 0) END), 0) AS CurrentBalance
    FROM dbo.ACCOUNTS a
    LEFT JOIN dbo.DOUBLE_ENTREY_VOUCHERS dev
        ON dev.Account_Code = a.Account_Code
       AND (@AsOfDate IS NULL OR dev.RecordDate <= @AsOfDate)
       AND (@BranchId IS NULL OR @BranchId = 0 OR dev.branch_id = @BranchId)
    WHERE a.Account_Code = @AccountCode
    GROUP BY a.Account_Code, a.Account_Serial, a.Account_Name, a.Account_NameEng;
END
GO

IF OBJECT_ID(N'dbo.usp_POS_CustodyFundingRefund_GetEmployeeCustodyAccount', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_POS_CustodyFundingRefund_GetEmployeeCustodyAccount
GO
CREATE PROCEDURE dbo.usp_POS_CustodyFundingRefund_GetEmployeeCustodyAccount
    @BranchId INT,
    @EmployeeID INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        requested.EmployeeID,
        emp.Emp_Name AS EmployeeName,
        emp.Emp_Namee AS EmployeeNameE,
        emp.BranchId,
        emp.Account_Code AS CustodyAccountCode,
        a.Account_Serial AS AccountSerial,
        a.Account_Name AS AccountName,
        a.Account_NameEng AS AccountNameEng,
        bal.CurrentBalance,
        CAST(CASE
            WHEN emp.Emp_ID IS NULL THEN 1
            WHEN NOT (ISNULL(emp.BranchId, 0) = 0 OR emp.BranchId = @BranchId) THEN 1
            WHEN ISNULL(emp.Account_Code, N'') = N'' THEN 1
            ELSE 0
        END AS BIT) AS HasWarning,
        CASE
            WHEN emp.Emp_ID IS NULL THEN N'لم يتم العثور على الموظف.'
            WHEN NOT (ISNULL(emp.BranchId, 0) = 0 OR emp.BranchId = @BranchId) THEN N'الموظف غير مرتبط بالفرع المحدد.'
            WHEN ISNULL(emp.Account_Code, N'') = N'' THEN N'لا يوجد حساب عهدة مكوّن لهذا الموظف.'
            ELSE N''
        END AS WarningMessage,
        LTRIM(RTRIM(COALESCE(CONVERT(NVARCHAR(255), a.Account_Serial) + N' - ', N'') + COALESCE(a.Account_Name, emp.Emp_Name, N''))) AS DisplayName
    FROM (SELECT @EmployeeID AS EmployeeID) requested
    LEFT JOIN dbo.TblEmployee emp ON emp.Emp_ID = requested.EmployeeID
    LEFT JOIN dbo.ACCOUNTS a ON a.Account_Code = emp.Account_Code
    OUTER APPLY (
        SELECT ISNULL(SUM(CASE WHEN dev.Credit_Or_Debit = 0 THEN ISNULL(dev.[Value], 0) ELSE -ISNULL(dev.[Value], 0) END), 0) AS CurrentBalance
        FROM dbo.DOUBLE_ENTREY_VOUCHERS dev
        WHERE dev.Account_Code = emp.Account_Code
          AND (@BranchId IS NULL OR @BranchId = 0 OR dev.branch_id = @BranchId)
    ) bal;
END
GO

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

    DECLARE @LegacyCashingType INT = CASE
        WHEN @OperationType IN (0, 1) THEN @OperationType + 5
        WHEN @OperationType IN (5, 6) THEN @OperationType
        ELSE NULL
    END;
    DECLARE @MainBoxType INT = CASE WHEN @LegacyCashingType = 6 THEN 0 ELSE 1 END;
    DECLARE @CreditAccountCode NVARCHAR(255);
    DECLARE @EmployeeAccountCode NVARCHAR(255);
    DECLARE @ValidationMessage NVARCHAR(MAX) = N'';
    DECLARE @DebitTotal DECIMAL(19, 4) = 0;
    DECLARE @CreditTotal DECIMAL(19, 4) = 0;

    SET @BoxValue = ISNULL(@BoxValue, 0);
    SET @EmployeeValue = ISNULL(@EmployeeValue, 0);

    IF @LegacyCashingType NOT IN (5, 6)
        SET @ValidationMessage = @ValidationMessage + N'نوع الحركة غير صحيح. ';
    IF ISNULL(@Amount, 0) <= 0
        SET @ValidationMessage = @ValidationMessage + N'يجب إدخال قيمة أكبر من صفر. ';
    IF NOT EXISTS (
        SELECT 1 FROM dbo.TblBoxesData b
        WHERE b.Account_Code = @MainAccountCode
          AND b.Type = @MainBoxType
          AND (ISNULL(b.BranchId, 0) = 0 OR b.BranchId = @BranchId)
    )
        SET @ValidationMessage = @ValidationMessage + N'حساب الحركة الرئيسي غير مرتبط بالفرع أو نوع الحركة المحدد. ';

    IF @PaymentMethod = 0
    BEGIN
        SELECT @CreditAccountCode = b.Account_Code
        FROM dbo.TblBoxesData b
        WHERE b.BoxID = @BoxID
          AND (ISNULL(b.BranchId, 0) = 0 OR b.BranchId = @BranchId);

        IF ISNULL(@CreditAccountCode, N'') = N''
            SET @ValidationMessage = @ValidationMessage + N'الدفع النقدي يتطلب خزنة مرتبطة بالفرع المحدد. ';
    END
    ELSE IF @PaymentMethod IN (1, 2, 3)
    BEGIN
        SELECT @CreditAccountCode = bank.Account_Code
        FROM dbo.BanksData bank
        WHERE bank.BankID = @BankID
          AND (ISNULL(bank.BranchId, 0) = 0 OR bank.BranchId = @BranchId);

        IF ISNULL(@CreditAccountCode, N'') = N''
            SET @ValidationMessage = @ValidationMessage + N'الدفع البنكي يتطلب بنكاً مرتبطاً بالفرع المحدد. ';
        IF ISNULL(LTRIM(RTRIM(@ChequeOrTransferNumber)), N'') = N''
            SET @ValidationMessage = @ValidationMessage + N'رقم الشيك أو الحوالة مطلوب. ';
        IF @ChequeOrTransferDate IS NULL
            SET @ValidationMessage = @ValidationMessage + N'تاريخ الشيك أو الحوالة مطلوب. ';
    END
    ELSE
        SET @ValidationMessage = @ValidationMessage + N'طريقة الدفع غير صحيحة. ';

    IF @EmployeeID IS NOT NULL
    BEGIN
        SELECT @EmployeeAccountCode = emp.Account_Code
        FROM dbo.TblEmployee emp
        WHERE emp.Emp_ID = @EmployeeID
          AND (ISNULL(emp.BranchId, 0) = 0 OR emp.BranchId = @BranchId);

        IF @EmployeeAccountCode IS NULL
            SET @ValidationMessage = @ValidationMessage + N'الموظف غير مرتبط بالفرع المحدد. ';
        ELSE IF ISNULL(@EmployeeAccountCode, N'') = N''
            SET @ValidationMessage = @ValidationMessage + N'لا يوجد حساب عهدة مكوّن لهذا الموظف. ';
    END

    IF (@BoxValue <> 0 OR @EmployeeValue <> 0) AND ROUND(@BoxValue + @EmployeeValue, 4) <> ROUND(@Amount, 4)
        SET @ValidationMessage = @ValidationMessage + N'مجموع قيمة الخزنة والعهدة يجب أن يساوي قيمة الحركة. ';
    IF @EmployeeValue <> 0 AND ISNULL(@EmployeeAccountCode, N'') = N''
        SET @ValidationMessage = @ValidationMessage + N'حساب عهدة الموظف مطلوب عند إدخال قيمة عهدة. ';

    DECLARE @Rows TABLE (
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
        WHERE a.Account_Code = @MainAccountCode;

        IF @BoxValue = 0 AND @EmployeeValue = 0
        BEGIN
            INSERT INTO @Rows (AccountCode, AccountSerial, AccountName, AccountNameEng, DisplayName, Debit, Credit, CreditOrDebit, Source)
            SELECT a.Account_Code, a.Account_Serial, a.Account_Name, a.Account_NameEng,
                   LTRIM(RTRIM(COALESCE(CONVERT(NVARCHAR(255), a.Account_Serial) + N' - ', N'') + COALESCE(a.Account_Name, N''))),
                   0, @Amount, 1, CASE WHEN @PaymentMethod = 0 THEN N'Treasury' ELSE N'Bank' END
            FROM dbo.ACCOUNTS a
            WHERE a.Account_Code = @CreditAccountCode;
        END
        ELSE
        BEGIN
            IF @BoxValue <> 0
            BEGIN
                INSERT INTO @Rows (AccountCode, AccountSerial, AccountName, AccountNameEng, DisplayName, Debit, Credit, CreditOrDebit, Source)
                SELECT a.Account_Code, a.Account_Serial, a.Account_Name, a.Account_NameEng,
                       LTRIM(RTRIM(COALESCE(CONVERT(NVARCHAR(255), a.Account_Serial) + N' - ', N'') + COALESCE(a.Account_Name, N''))),
                       0, @BoxValue, 1, CASE WHEN @PaymentMethod = 0 THEN N'Treasury' ELSE N'Bank' END
                FROM dbo.ACCOUNTS a
                WHERE a.Account_Code = @CreditAccountCode;
            END

            IF @EmployeeValue <> 0
            BEGIN
                INSERT INTO @Rows (AccountCode, AccountSerial, AccountName, AccountNameEng, DisplayName, Debit, Credit, CreditOrDebit, Source)
                SELECT a.Account_Code, a.Account_Serial, a.Account_Name, a.Account_NameEng,
                       LTRIM(RTRIM(COALESCE(CONVERT(NVARCHAR(255), a.Account_Serial) + N' - ', N'') + COALESCE(a.Account_Name, N''))),
                       0, @EmployeeValue, 1, N'EmployeeCustody'
                FROM dbo.ACCOUNTS a
                WHERE a.Account_Code = @EmployeeAccountCode;
            END
        END
    END

    SELECT @DebitTotal = ISNULL(SUM(Debit), 0), @CreditTotal = ISNULL(SUM(Credit), 0) FROM @Rows;
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
