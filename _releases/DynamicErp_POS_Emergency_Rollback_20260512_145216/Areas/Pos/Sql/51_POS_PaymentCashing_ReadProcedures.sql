SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

IF OBJECT_ID(N'dbo.usp_DynamicErpVoucher_Search', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_DynamicErpVoucher_Search;
GO
CREATE PROCEDURE dbo.usp_DynamicErpVoucher_Search
    @noteType int,
    @fromDate datetime = NULL,
    @toDate datetime = NULL,
    @serial nvarchar(50) = N'',
    @party nvarchar(200) = N'',
    @branchId int = NULL,
    @cashboxOrBank nvarchar(200) = N'',
    @amount decimal(18, 2) = NULL,
    @pageNumber int = 1,
    @pageSize int = 50
AS
BEGIN
    SET NOCOUNT ON;
    SET @pageNumber = CASE WHEN ISNULL(@pageNumber, 1) < 1 THEN 1 ELSE @pageNumber END;
    SET @pageSize = CASE WHEN ISNULL(@pageSize, 50) < 10 THEN 10 WHEN @pageSize > 200 THEN 200 ELSE @pageSize END;

    ;WITH VoucherRows AS (
    SELECT
        n.NoteID,
        n.NoteDate,
        CONVERT(nvarchar(50), ISNULL(n.NoteSerial1, n.NoteSerial)) AS NoteSerial,
        COALESCE(NULLIF(c.CusNamee, N''), NULLIF(c.CusName, N''), NULLIF(n.person, N''), NULLIF(n.too, N''), NULLIF(n.renterName, N''), N'') AS PartyDisplay,
        COALESCE(NULLIF(b.branch_name, N''), CONVERT(nvarchar(30), n.branch_no)) AS BranchDisplay,
        COALESCE(NULLIF(box.BoxName, N''), NULLIF(bank.BankName, N''), NULLIF(n.BankName, N''), N'') AS CashboxOrBankDisplay,
        CONVERT(decimal(18,2), ISNULL(n.Note_Value, 0)) AS Amount,
        CONVERT(decimal(18,2), ISNULL(n.VAT, 0)) AS Vat,
        CONVERT(decimal(18,2), ISNULL(n.TotalValue, ISNULL(n.Note_Value, 0) + ISNULL(n.VAT, 0))) AS Total,
        CONVERT(decimal(18,2), ISNULL(SUM(CASE WHEN dev.Credit_Or_Debit = 0 THEN dev.Value ELSE 0 END), 0)) AS DebitTotal,
        CONVERT(decimal(18,2), ISNULL(SUM(CASE WHEN dev.Credit_Or_Debit = 1 THEN dev.Value ELSE 0 END), 0)) AS CreditTotal
    FROM dbo.Notes n
    LEFT JOIN dbo.TblCustemers c ON c.CusID = n.CusID
    LEFT JOIN dbo.TblBranchesData b ON b.branch_id = n.branch_no
    LEFT JOIN dbo.TblBoxesData box ON box.BoxID = n.BoxID
    LEFT JOIN dbo.BanksData bank ON bank.BankID = n.BankID
    LEFT JOIN dbo.DOUBLE_ENTREY_VOUCHERS dev ON dev.Notes_ID = n.NoteID
    WHERE n.NoteType = @noteType
      AND (@fromDate IS NULL OR n.NoteDate >= @fromDate)
      AND (@toDate IS NULL OR n.NoteDate < DATEADD(day, 1, @toDate))
      AND (ISNULL(@serial, N'') = N'' OR CONVERT(nvarchar(50), ISNULL(n.NoteSerial1, n.NoteSerial)) LIKE N'%' + @serial + N'%')
      AND (ISNULL(@party, N'') = N'' OR COALESCE(c.CusNamee, c.CusName, n.person, n.too, n.renterName, N'') LIKE N'%' + @party + N'%')
      AND (@branchId IS NULL OR n.branch_no = @branchId)
      AND (ISNULL(@cashboxOrBank, N'') = N'' OR COALESCE(box.BoxName, bank.BankName, n.BankName, N'') LIKE N'%' + @cashboxOrBank + N'%')
      AND (@amount IS NULL OR ABS(ISNULL(n.Note_Value, 0) - @amount) < 0.01)
    GROUP BY n.NoteID, n.NoteDate, n.NoteSerial1, n.NoteSerial, c.CusNamee, c.CusName, n.person, n.too, n.renterName, b.branch_name, n.branch_no, box.BoxName, bank.BankName, n.BankName, n.Note_Value, n.VAT, n.TotalValue
    )
    SELECT *, COUNT(1) OVER() AS TotalRows
    FROM VoucherRows
    ORDER BY NoteDate DESC, NoteID DESC
    OFFSET (@pageNumber - 1) * @pageSize ROWS FETCH NEXT @pageSize ROWS ONLY;
END
GO

IF OBJECT_ID(N'dbo.usp_DynamicErpVoucher_Header', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_DynamicErpVoucher_Header;
GO
CREATE PROCEDURE dbo.usp_DynamicErpVoucher_Header
    @noteType int,
    @id int
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (1)
        n.NoteID,
        CONVERT(nvarchar(50), ISNULL(n.NoteSerial1, n.NoteSerial)) AS NoteSerial,
        n.NoteDate,
        n.ManualNo,
        n.ORDER_NO AS OrderNo,
        COALESCE(NULLIF(c.CusNamee, N''), NULLIF(c.CusName, N''), NULLIF(n.person, N''), NULLIF(n.too, N''), NULLIF(n.renterName, N''), N'') AS PartyDisplay,
        COALESCE(NULLIF(a.Account_Serial, N'') + N' - ' + NULLIF(a.Account_Name, N''), NULLIF(a.Account_Name, N''), N'') AS AccountDisplay,
        COALESCE(NULLIF(b.branch_name, N''), CONVERT(nvarchar(30), n.branch_no)) AS BranchDisplay,
        box.BoxName AS CashboxDisplay,
        bank.BankName AS BankDisplay,
        n.TxtChequeNumber1 AS ChequeNumber,
        n.DtpChequeDueDate1 AS ChequeDueDate,
        CONVERT(decimal(18,6), 0) AS CurrencyRate,
        cur.name AS CurrencyDisplay,
        COALESCE(NULLIF(cost.Account_Serial, N'') + N' - ' + NULLIF(cost.Account_Name, N''), NULLIF(cost.Account_Name, N''), NULLIF(n.general_cost_center, N''), N'') AS CostCenterDisplay,
        COALESCE(NULLIF(pr.Project_name, N''), NULLIF(pr.Project_nameE, N''), CONVERT(nvarchar(30), NULLIF(n.ProjectID, 0)), CONVERT(nvarchar(30), NULLIF(n.project_id, 0)), N'') AS ProjectDisplay,
        n.IncludVAT,
        n.PayDes,
        n.PayDes1,
        n.PaymentType,
        n.CashingType,
        n.NoteCashingType,
        n.NCashingType,
        n.Remark,
        CONVERT(decimal(18,2), ISNULL(n.Note_Value, 0)) AS Amount,
        CONVERT(decimal(18,2), ISNULL(n.VAT, 0)) AS Vat,
        CONVERT(decimal(18,2), ISNULL(n.TotalValue, ISNULL(n.Note_Value, 0) + ISNULL(n.VAT, 0))) AS Total,
        n.Double_Entry_Vouchers_ID,
        n.ReportName
    FROM dbo.Notes n
    LEFT JOIN dbo.TblCustemers c ON c.CusID = n.CusID
    LEFT JOIN dbo.TblBranchesData b ON b.branch_id = n.branch_no
    LEFT JOIN dbo.TblBoxesData box ON box.BoxID = n.BoxID
    LEFT JOIN dbo.BanksData bank ON bank.BankID = n.BankID
    LEFT JOIN dbo.ACCOUNTS a ON a.Account_Code = COALESCE(NULLIF(n.AccountPaym, N''), NULLIF(n.Account_DebitSide, N''), NULLIF(n.Account_CreditSide, N''), NULLIF(n.Account_Code1, N''), NULLIF(n.Account_Code2, N''))
    LEFT JOIN dbo.Currency cur ON cur.id = NULLIF(n.PaymentType1, 0)
    LEFT JOIN dbo.ACCOUNTS cost ON cost.Account_Code = NULLIF(n.general_cost_center, N'')
    LEFT JOIN dbo.projects pr ON pr.id = COALESCE(NULLIF(n.ProjectID, 0), NULLIF(n.project_id, 0))
    WHERE n.NoteType = @noteType AND n.NoteID = @id;
END
GO

IF OBJECT_ID(N'dbo.usp_DynamicErpVoucher_Accounting', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_DynamicErpVoucher_Accounting;
GO
CREATE PROCEDURE dbo.usp_DynamicErpVoucher_Accounting
    @id int
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        dev.Double_Entry_Vouchers_ID,
        dev.DEV_ID_Line_No,
        dev.RecordDate,
        COALESCE(NULLIF(a.Account_Serial, N'') + N' - ' + NULLIF(a.Account_Name, N''), NULLIF(a.Account_Name, N''), N'') AS AccountDisplay,
        CONVERT(decimal(18,2), CASE WHEN dev.Credit_Or_Debit = 0 THEN ISNULL(dev.Value, 0) ELSE 0 END) AS Debit,
        CONVERT(decimal(18,2), CASE WHEN dev.Credit_Or_Debit = 1 THEN ISNULL(dev.Value, 0) ELSE 0 END) AS Credit,
        dev.Double_Entry_Vouchers_Description,
        dev.branch_id
    FROM dbo.DOUBLE_ENTREY_VOUCHERS dev
    LEFT JOIN dbo.ACCOUNTS a ON a.Account_Code = dev.Account_Code
    WHERE dev.Notes_ID = @id
    ORDER BY dev.Double_Entry_Vouchers_ID, dev.DEV_ID_Line_No;
END
GO

IF OBJECT_ID(N'dbo.usp_DynamicErpVoucher_RelatedNotes', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_DynamicErpVoucher_RelatedNotes;
GO
CREATE PROCEDURE dbo.usp_DynamicErpVoucher_RelatedNotes
    @id int
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        NoteID,
        CONVERT(nvarchar(50), ISNULL(NoteSerial1, NoteSerial)) AS NoteSerial,
        NoteType,
        NoteDate,
        CONVERT(decimal(18,2), ISNULL(Note_Value, 0)) AS Amount,
        Remark
    FROM dbo.Notes
    WHERE NoteID = @id OR NoteId2 = CONVERT(nvarchar(50), @id) OR NoteOrBonID = @id
    ORDER BY NoteDate, NoteID;
END
GO

IF OBJECT_ID(N'dbo.usp_DynamicErpVoucher_Allocations', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_DynamicErpVoucher_Allocations;
GO
CREATE PROCEDURE dbo.usp_DynamicErpVoucher_Allocations
    @noteType int,
    @id int
AS
BEGIN
    SET NOCOUNT ON;

    IF @noteType = 4
    BEGIN
        SELECT Source, Serial, [Date], OriginalValue, PaidValue, RemainingValue, Description
        FROM (
            SELECT N'ContracttBillInstallmentsDone' AS Source,
                   CONVERT(nvarchar(50), d.InstallNo) AS Serial,
                   d.RecordDate AS [Date],
                   CONVERT(decimal(18,2), ISNULL(d.[Value], 0)) AS OriginalValue,
                   CONVERT(decimal(18,2), ISNULL(d.total, 0)) AS PaidValue,
                   CONVERT(decimal(18,2), 0) AS RemainingValue,
                   CONCAT(N'installment id ', CONVERT(nvarchar(50), d.istallid)) AS Description
            FROM dbo.ContracttBillInstallmentsDone d
            WHERE d.NoteID = @id
            UNION ALL
            SELECT N'TblContractInstallments by contract' AS Source,
                   CONVERT(nvarchar(50), ci.InstallNo) AS Serial,
                   ci.Installdate AS [Date],
                   CONVERT(decimal(18,2), ISNULL(ci.installValue, 0) + ISNULL(ci.VATValue, 0)) AS OriginalValue,
                   CONVERT(decimal(18,2), ISNULL(ci.RentValuePayed, 0) + ISNULL(ci.CommissionsPayed, 0) + ISNULL(ci.InsurancePayed, 0) + ISNULL(ci.WaterPayed, 0) + ISNULL(ci.ElectricPayed, 0) + ISNULL(ci.TelandNetPayed, 0) + ISNULL(ci.OldValuePayed, 0) + ISNULL(ci.VATPayed, 0)) AS PaidValue,
                   CONVERT(decimal(18,2), (ISNULL(ci.installValue, 0) + ISNULL(ci.VATValue, 0)) - (ISNULL(ci.RentValuePayed, 0) + ISNULL(ci.CommissionsPayed, 0) + ISNULL(ci.InsurancePayed, 0) + ISNULL(ci.WaterPayed, 0) + ISNULL(ci.ElectricPayed, 0) + ISNULL(ci.TelandNetPayed, 0) + ISNULL(ci.OldValuePayed, 0) + ISNULL(ci.VATPayed, 0))) AS RemainingValue,
                   ci.des AS Description
            FROM dbo.TblContractInstallments ci
            INNER JOIN dbo.Notes n ON n.ContNo = ci.ContNo
            WHERE n.NoteID = @id
              AND (ISNULL(ci.RentValuePayed, 0) + ISNULL(ci.CommissionsPayed, 0) + ISNULL(ci.InsurancePayed, 0) + ISNULL(ci.WaterPayed, 0) + ISNULL(ci.ElectricPayed, 0) + ISNULL(ci.TelandNetPayed, 0) + ISNULL(ci.OldValuePayed, 0) + ISNULL(ci.VATPayed, 0)) > 0
            UNION ALL
            SELECT N'TblAqarCommissions' AS Source,
                   CONVERT(nvarchar(50), PymentNo) AS Serial,
                   NULL AS [Date],
                   CONVERT(decimal(18,2), ISNULL(Amount, 0)) AS OriginalValue,
                   CONVERT(decimal(18,2), ISNULL(Amount, 0)) AS PaidValue,
                   CONVERT(decimal(18,2), 0) AS RemainingValue,
                   Remarks AS Description
            FROM dbo.TblAqarCommissions
            WHERE NoteID = @id
            UNION ALL
            SELECT N'TblNotesSales' AS Source,
                   CONVERT(nvarchar(50), ID) AS Serial,
                   NULL AS [Date],
                   CONVERT(decimal(18,2), ISNULL(valu, 0)) AS OriginalValue,
                   CONVERT(decimal(18,2), ISNULL(valu, 0)) AS PaidValue,
                   CONVERT(decimal(18,2), 0) AS RemainingValue,
                   CONCAT(N'sales type ', CONVERT(nvarchar(20), Type), N' emp ', CONVERT(nvarchar(20), EmpID)) AS Description
            FROM dbo.TblNotesSales
            WHERE NoteID = @id
            UNION ALL
            SELECT N'TblUnitNoInformation' AS Source,
                   CONVERT(nvarchar(50), ID) AS Serial,
                   RecDate AS [Date],
                   CONVERT(decimal(18,2), 0) AS OriginalValue,
                   CONVERT(decimal(18,2), 0) AS PaidValue,
                   CONVERT(decimal(18,2), 0) AS RemainingValue,
                   Des AS Description
            FROM dbo.TblUnitNoInformation
            WHERE NoteID = @id
            UNION ALL
            SELECT N'TblAqrEarnest' AS Source,
                   CONVERT(nvarchar(50), ID) AS Serial,
                   RecordDate AS [Date],
                   CONVERT(decimal(18,2), ISNULL(Earnest, 0)) AS OriginalValue,
                   CONVERT(decimal(18,2), ISNULL(Earnest, 0)) AS PaidValue,
                   CONVERT(decimal(18,2), 0) AS RemainingValue,
                   CoustomerName AS Description
            FROM dbo.TblAqrEarnest
            WHERE NoteID = @id
            UNION ALL
            SELECT N'TblOtheExpensAqar' AS Source,
                   CONVERT(nvarchar(50), ID) AS Serial,
                   RecordDate AS [Date],
                   CONVERT(decimal(18,2), ISNULL(Total, ISNULL(Valuee, 0))) AS OriginalValue,
                   CONVERT(decimal(18,2), ISNULL(Net, ISNULL(Valuee, 0))) AS PaidValue,
                   CONVERT(decimal(18,2), ISNULL(Total, 0) - ISNULL(Net, 0)) AS RemainingValue,
                   Remarks AS Description
            FROM dbo.TblOtheExpensAqar
            WHERE NoteID = @id
               OR NoteSerial1 = (SELECT TOP (1) CONVERT(nvarchar(50), NoteSerial1) FROM dbo.Notes WHERE NoteID = @id)
            UNION ALL
            SELECT N'TblFiterWaiver' AS Source,
                   CONVERT(nvarchar(50), ID) AS Serial,
                   RecordDate AS [Date],
                   CONVERT(decimal(18,2), ISNULL(BillPrice, 0)) AS OriginalValue,
                   CONVERT(decimal(18,2), ISNULL(totalpayed, 0)) AS PaidValue,
                   CONVERT(decimal(18,2), ISNULL(net, 0)) AS RemainingValue,
                   CONCAT(N'contract ', CONVERT(nvarchar(50), ContNo)) AS Description
            FROM dbo.TblFiterWaiver
            WHERE NoteID = @id
        ) x
        ORDER BY [Date], Serial;

        RETURN;
    END

    SELECT Source, Serial, [Date], OriginalValue, PaidValue, RemainingValue, Description
    FROM (
        SELECT N'TblNotesBillBuyPayment' AS Source, CONVERT(nvarchar(50), NoteSerial1) AS Serial, NoteDate AS [Date], CONVERT(decimal(18,2), ISNULL(Note_Value, 0)) AS OriginalValue, CONVERT(decimal(18,2), ISNULL(PayedValue, 0)) AS PaidValue, CONVERT(decimal(18,2), ISNULL(RemainingValue, 0)) AS RemainingValue, too AS Description FROM dbo.TblNotesBillBuyPayment WHERE NoteID1 = @id
        UNION ALL
        SELECT N'TblNotesBillProjectPayment', CONVERT(nvarchar(50), NoteSerial1), NoteDate, CONVERT(decimal(18,2), ISNULL(Note_Value, 0)), CONVERT(decimal(18,2), ISNULL(PayedValue, 0)), CONVERT(decimal(18,2), ISNULL(RemainingValue, 0)), too FROM dbo.TblNotesBillProjectPayment WHERE NoteID1 = @id
        UNION ALL
        SELECT N'TblNotesBillVindorPayment', CONVERT(nvarchar(50), NoteSerial1), NoteDate, CONVERT(decimal(18,2), ISNULL(Note_Value, 0)), CONVERT(decimal(18,2), ISNULL(PayedValue, 0)), CONVERT(decimal(18,2), ISNULL(RemainingValue, 0)), too FROM dbo.TblNotesBillVindorPayment WHERE NoteID1 = @id
    ) x
    ORDER BY [Date], Serial;
END
GO

IF OBJECT_ID(N'dbo.usp_DynamicErpVoucher_EditLookups', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_DynamicErpVoucher_EditLookups;
GO
CREATE PROCEDURE dbo.usp_DynamicErpVoucher_EditLookups
AS
BEGIN
    SET NOCOUNT ON;

    SELECT CONVERT(nvarchar(50), branch_id) AS [Value], COALESCE(NULLIF(branch_name, N''), CONVERT(nvarchar(50), branch_id)) AS [Text]
    FROM dbo.TblBranchesData
    ORDER BY branch_name, branch_id;

    SELECT CONVERT(nvarchar(50), BoxID) AS [Value], COALESCE(NULLIF(BoxName, N''), CONVERT(nvarchar(50), BoxID)) AS [Text]
    FROM dbo.TblBoxesData
    WHERE NULLIF(Account_Code, N'') IS NOT NULL
    ORDER BY BoxName, BoxID;

    SELECT CONVERT(nvarchar(50), BankID) AS [Value], COALESCE(NULLIF(BankName, N''), CONVERT(nvarchar(50), BankID)) AS [Text]
    FROM dbo.BanksData
    WHERE NULLIF(Account_Code, N'') IS NOT NULL
    ORDER BY BankName, BankID;

    SELECT TOP (500)
        Account_Code AS [Value],
        COALESCE(NULLIF(Account_Serial, N'') + N' - ' + NULLIF(Account_Name, N''), NULLIF(Account_Name, N''), Account_Code) AS [Text]
    FROM dbo.ACCOUNTS
    WHERE ISNULL(last_account, 0) = 1
      AND ISNULL(Block, 0) = 0
    ORDER BY Account_Serial, Account_Name;
END
GO

IF OBJECT_ID(N'dbo.usp_DynamicErpVoucher_EditHeader', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_DynamicErpVoucher_EditHeader;
GO
CREATE PROCEDURE dbo.usp_DynamicErpVoucher_EditHeader
    @noteType int,
    @id int
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP (1)
        COALESCE(NULLIF(n.AccountPaym, N''), NULLIF(n.Account_DebitSide, N''), NULLIF(n.Account_CreditSide, N''), NULLIF(n.Account_Code1, N''), NULLIF(n.Account_Code2, N'')) AS PartyAccountCode,
        n.branch_no AS BranchId,
        n.BoxID AS BoxId,
        n.BankID AS BankId,
        n.NoteCashingType AS PaymentMethod,
        n.CashingType,
        n.NCashingType AS ReceiptClass,
        ISNULL(n.NotePosted, 0) AS IsPosted
    FROM dbo.Notes n
    WHERE n.NoteType = @noteType AND n.NoteID = @id;
END
GO

IF OBJECT_ID(N'dbo.usp_DynamicErpVoucher_Save', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_DynamicErpVoucher_Save;
GO
CREATE PROCEDURE dbo.usp_DynamicErpVoucher_Save
    @noteType int,
    @noteId int = NULL,
    @noteDate datetime,
    @manualNo nvarchar(255) = NULL,
    @orderNo nvarchar(50) = NULL,
    @partyAccountCode nvarchar(55),
    @partyDisplay nvarchar(4000) = NULL,
    @branchId int = NULL,
    @boxId int = NULL,
    @bankId int = NULL,
    @paymentMethod int = NULL,
    @cashingType int = NULL,
    @receiptClass int = NULL,
    @chequeNumber nvarchar(255) = NULL,
    @chequeDueDate datetime = NULL,
    @amount decimal(18,2),
    @vat decimal(18,2) = 0,
    @includeVat int = 0,
    @remark nvarchar(4000) = NULL,
    @payDes nvarchar(4000) = NULL,
    @payDes1 nvarchar(4000) = NULL,
    @userId int
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @noteType NOT IN (4, 5) BEGIN RAISERROR('Unsupported voucher type.', 16, 1); RETURN; END
    IF ISNULL(@amount, 0) <= 0 BEGIN RAISERROR('Voucher amount must be greater than zero.', 16, 1); RETURN; END
    IF NULLIF(LTRIM(RTRIM(ISNULL(@partyAccountCode, N''))), N'') IS NULL BEGIN RAISERROR('Party account is required.', 16, 1); RETURN; END
    IF (@boxId IS NULL AND @bankId IS NULL) OR (@boxId IS NOT NULL AND @bankId IS NOT NULL) BEGIN RAISERROR('Select either cashbox or bank.', 16, 1); RETURN; END

    DECLARE @partyAccount nvarchar(55) = LTRIM(RTRIM(@partyAccountCode));
    DECLARE @cashAccount nvarchar(55);
    DECLARE @noteSerial decimal(38,0);
    DECLARE @noteSerial1 decimal(38,0);
    DECLARE @voucherId int;
    DECLARE @total decimal(18,2) = ISNULL(@amount, 0) + ISNULL(@vat, 0);
    DECLARE @isEdit bit = CASE WHEN ISNULL(@noteId, 0) > 0 THEN 1 ELSE 0 END;
    DECLARE @description nvarchar(4000) = COALESCE(NULLIF(@remark, N''), NULLIF(@payDes, N''), CASE WHEN @noteType = 5 THEN N'سند صرف' ELSE N'سند قبض' END);

    IF NOT EXISTS (SELECT 1 FROM dbo.ACCOUNTS WHERE Account_Code = @partyAccount AND ISNULL(last_account, 0) = 1)
    BEGIN RAISERROR('Party account was not found or is not a leaf account.', 16, 1); RETURN; END

    IF @boxId IS NOT NULL
        SELECT @cashAccount = NULLIF(Account_Code, N'') FROM dbo.TblBoxesData WHERE BoxID = @boxId;
    ELSE
        SELECT @cashAccount = NULLIF(Account_Code, N'') FROM dbo.BanksData WHERE BankID = @bankId;

    IF @cashAccount IS NULL OR NOT EXISTS (SELECT 1 FROM dbo.ACCOUNTS WHERE Account_Code = @cashAccount)
    BEGIN RAISERROR('Cashbox/bank account was not found.', 16, 1); RETURN; END

    BEGIN TRANSACTION;

    IF @isEdit = 1
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM dbo.Notes WITH (UPDLOCK, HOLDLOCK) WHERE NoteID = @noteId AND NoteType = @noteType)
        BEGIN RAISERROR('Voucher was not found.', 16, 1); ROLLBACK TRANSACTION; RETURN; END
        IF EXISTS (SELECT 1 FROM dbo.Notes WHERE NoteID = @noteId AND ISNULL(NotePosted, 0) = 1)
        BEGIN RAISERROR('Posted voucher cannot be edited.', 16, 1); ROLLBACK TRANSACTION; RETURN; END
        IF @noteType = 5 AND (
            EXISTS (SELECT 1 FROM dbo.TblNotesBillBuyPayment WHERE NoteID1 = @noteId)
            OR EXISTS (SELECT 1 FROM dbo.TblNotesBillProjectPayment WHERE NoteID1 = @noteId)
            OR EXISTS (SELECT 1 FROM dbo.TblNotesBillVindorPayment WHERE NoteID1 = @noteId)
        ) BEGIN RAISERROR('Allocated payment vouchers must be edited from the allocation workflow.', 16, 1); ROLLBACK TRANSACTION; RETURN; END
        IF @noteType = 4 AND EXISTS (SELECT 1 FROM dbo.ContracttBillInstallmentsDone WHERE NoteID = @noteId)
        BEGIN RAISERROR('Allocated receipt vouchers must be edited from the allocation workflow.', 16, 1); ROLLBACK TRANSACTION; RETURN; END

        SELECT @noteSerial = CONVERT(decimal(38,0), ISNULL(NoteSerial, NoteID)),
               @noteSerial1 = CONVERT(decimal(38,0), ISNULL(NoteSerial1, NoteSerial))
        FROM dbo.Notes WHERE NoteID = @noteId;
    END
    ELSE
    BEGIN
        SELECT @noteId = ISNULL(MAX(NoteID), 0) + 1 FROM dbo.Notes WITH (TABLOCKX, HOLDLOCK);
        SELECT @noteSerial = ISNULL(MAX(CASE WHEN ISNUMERIC(NoteSerial) = 1 THEN CONVERT(decimal(38,0), NoteSerial) ELSE 0 END), 0) + 1
        FROM dbo.Notes WITH (TABLOCKX, HOLDLOCK) WHERE NoteType = @noteType;
        SELECT @noteSerial1 = ISNULL(MAX(CASE WHEN ISNUMERIC(NoteSerial1) = 1 THEN CONVERT(decimal(38,0), NoteSerial1) ELSE 0 END), 0) + 1
        FROM dbo.Notes WITH (TABLOCKX, HOLDLOCK) WHERE NoteType = @noteType;
    END

    IF @isEdit = 1
    BEGIN
        UPDATE dbo.Notes
        SET NoteDate = @noteDate,
            Note_Value = CONVERT(float, @amount),
            Note_Value2 = CONVERT(float, @amount),
            BankID = @bankId,
            BoxID = @boxId,
            UserID = @userId,
            Remark = @remark,
            CashingType = @cashingType,
            NoteCashingType = @paymentMethod,
            branch_no = @branchId,
            user_name = CONVERT(nvarchar(50), @userId),
            person = @partyDisplay,
            too = @partyDisplay,
            ORDER_NO = @orderNo,
            PaymentType = @paymentMethod,
            TxtChequeNumber1 = @chequeNumber,
            DtpChequeDueDate1 = @chequeDueDate,
            ManualNo = @manualNo,
            NCashingType = @receiptClass,
            PayDes = @payDes,
            PayDes1 = @payDes1,
            VAT = CONVERT(float, ISNULL(@vat, 0)),
            TotalValue = CONVERT(float, @total),
            IncludVAT = @includeVat,
            AccountPaym = @partyAccount,
            Account_DebitSide = CASE WHEN @noteType = 5 THEN @partyAccount ELSE @cashAccount END,
            Account_CreditSide = CASE WHEN @noteType = 5 THEN @cashAccount ELSE @partyAccount END
        WHERE NoteID = @noteId AND NoteType = @noteType;
    END
    ELSE
    BEGIN
        INSERT INTO dbo.Notes
        (
            NoteID, NoteDate, NoteType, NoteSerial, NoteSerial1, Note_Value, Note_Value2,
            BankID, BoxID, UserID, Remark, CashingType, NoteCashingType, NotePosted,
            numbering_type, numbering_type1, sanad_year, sanad_month, branch_no, user_name,
            person, too, ORDER_NO, PaymentType, TxtChequeNumber1, DtpChequeDueDate1,
            ManualNo, NCashingType, PayDes, PayDes1, VAT, TotalValue, IncludVAT,
            AccountPaym, Account_DebitSide, Account_CreditSide
        )
        VALUES
        (
            @noteId, @noteDate, @noteType, CONVERT(float, @noteSerial), CONVERT(float, @noteSerial1), CONVERT(float, @amount), CONVERT(float, @amount),
            @bankId, @boxId, @userId, @remark, @cashingType, @paymentMethod, 0,
            @noteType, @noteType, YEAR(@noteDate), MONTH(@noteDate), @branchId, CONVERT(nvarchar(50), @userId),
            @partyDisplay, @partyDisplay, @orderNo, @paymentMethod, @chequeNumber, @chequeDueDate,
            @manualNo, @receiptClass, @payDes, @payDes1, CONVERT(float, ISNULL(@vat, 0)), CONVERT(float, @total), @includeVat,
            @partyAccount, CASE WHEN @noteType = 5 THEN @partyAccount ELSE @cashAccount END, CASE WHEN @noteType = 5 THEN @cashAccount ELSE @partyAccount END
        );
    END

    DELETE FROM dbo.DOUBLE_ENTREY_VOUCHERS WHERE Notes_ID = @noteId;

    SELECT @voucherId = ISNULL(MAX(Double_Entry_Vouchers_ID), 0) + 1
    FROM dbo.DOUBLE_ENTREY_VOUCHERS WITH (TABLOCKX, HOLDLOCK);

    INSERT INTO dbo.DOUBLE_ENTREY_VOUCHERS
    (
        Double_Entry_Vouchers_ID, DEV_ID_Line_No, Account_Code, Value, Credit_Or_Debit,
        Double_Entry_Vouchers_Description, RecordDate, Notes_ID, UserID, Posted, branch_id, DEV_Serial
    )
    VALUES
    (
        @voucherId, 1, CASE WHEN @noteType = 5 THEN @partyAccount ELSE @cashAccount END, @total, 0,
        @description, @noteDate, @noteId, @userId, 0, @branchId, CONVERT(nvarchar(50), @noteSerial1)
    ),
    (
        @voucherId, 2, CASE WHEN @noteType = 5 THEN @cashAccount ELSE @partyAccount END, @total, 1,
        @description, @noteDate, @noteId, @userId, 0, @branchId, CONVERT(nvarchar(50), @noteSerial1)
    );

    UPDATE dbo.Notes
    SET Double_Entry_Vouchers_ID = @voucherId
    WHERE NoteID = @noteId;

    COMMIT TRANSACTION;

    SELECT @noteId AS NoteID,
           CONVERT(nvarchar(50), @noteSerial1) AS NoteSerial,
           @voucherId AS Double_Entry_Vouchers_ID,
           CAST(CASE WHEN @isEdit = 1 THEN N'تم تعديل السند والقيد المحاسبي.' ELSE N'تم إنشاء السند والقيد المحاسبي.' END AS nvarchar(300)) AS ResultMessage;
END
GO

IF OBJECT_ID(N'dbo.usp_DynamicErpVoucher_Post', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_DynamicErpVoucher_Post;
GO
CREATE PROCEDURE dbo.usp_DynamicErpVoucher_Post
    @noteType int,
    @noteId int,
    @userId int
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION;
    IF NOT EXISTS (SELECT 1 FROM dbo.Notes WITH (UPDLOCK, HOLDLOCK) WHERE NoteID = @noteId AND NoteType = @noteType)
    BEGIN RAISERROR('Voucher was not found.', 16, 1); ROLLBACK TRANSACTION; RETURN; END

    UPDATE dbo.Notes
    SET NotePosted = 1, PostedBy = @userId, PostDate = GETDATE()
    WHERE NoteID = @noteId AND NoteType = @noteType;

    UPDATE dbo.DOUBLE_ENTREY_VOUCHERS
    SET Posted = 1, PostedUserID = @userId, PostedDate = GETDATE()
    WHERE Notes_ID = @noteId;
    COMMIT TRANSACTION;
END
GO

IF OBJECT_ID(N'dbo.usp_DynamicErpVoucher_Delete', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_DynamicErpVoucher_Delete;
GO
CREATE PROCEDURE dbo.usp_DynamicErpVoucher_Delete
    @noteType int,
    @noteId int,
    @userId int
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION;
    IF NOT EXISTS (SELECT 1 FROM dbo.Notes WITH (UPDLOCK, HOLDLOCK) WHERE NoteID = @noteId AND NoteType = @noteType)
    BEGIN RAISERROR('Voucher was not found.', 16, 1); ROLLBACK TRANSACTION; RETURN; END
    IF EXISTS (SELECT 1 FROM dbo.Notes WHERE NoteID = @noteId AND ISNULL(NotePosted, 0) = 1)
    BEGIN RAISERROR('Posted voucher cannot be deleted.', 16, 1); ROLLBACK TRANSACTION; RETURN; END
    IF @noteType = 5 AND (
        EXISTS (SELECT 1 FROM dbo.TblNotesBillBuyPayment WHERE NoteID1 = @noteId)
        OR EXISTS (SELECT 1 FROM dbo.TblNotesBillProjectPayment WHERE NoteID1 = @noteId)
        OR EXISTS (SELECT 1 FROM dbo.TblNotesBillVindorPayment WHERE NoteID1 = @noteId)
    ) BEGIN RAISERROR('Allocated payment vouchers cannot be deleted from this screen.', 16, 1); ROLLBACK TRANSACTION; RETURN; END
    IF @noteType = 4 AND EXISTS (SELECT 1 FROM dbo.ContracttBillInstallmentsDone WHERE NoteID = @noteId)
    BEGIN RAISERROR('Allocated receipt vouchers cannot be deleted from this screen.', 16, 1); ROLLBACK TRANSACTION; RETURN; END

    DELETE FROM dbo.DOUBLE_ENTREY_VOUCHERS WHERE Notes_ID = @noteId;
    DELETE FROM dbo.Notes WHERE NoteID = @noteId AND NoteType = @noteType;
    COMMIT TRANSACTION;
END
GO
