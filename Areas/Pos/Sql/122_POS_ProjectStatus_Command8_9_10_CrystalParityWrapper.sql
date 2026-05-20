
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
IF OBJECT_ID(N'dbo.usp_POS_ProjectStatus_Report_Run', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_POS_ProjectStatus_Report_Run;
GO
CREATE PROCEDURE dbo.usp_POS_ProjectStatus_Report_Run
    @reportKey NVARCHAR(80),
    @fromDate DATETIME,
    @toDate DATETIME,
    @branchId INT,
    @userId INT,
    @canChangeDefaults BIT,
    @branchFromId INT = NULL,
    @branchToId INT = NULL,
    @showEmptyBranches BIT = 0,
    @serviceSearch NVARCHAR(100) = NULL,
    @serviceType NVARCHAR(30) = NULL,
    @storeId INT = NULL,
    @filterUserId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @from DATE = CONVERT(DATE, @fromDate);
    DECLARE @to DATE = CONVERT(DATE, @toDate);
    DECLARE @key NVARCHAR(80) = LOWER(LTRIM(RTRIM(ISNULL(@reportKey, N''))));
    IF @to < @from BEGIN DECLARE @swap DATE = @from; SET @from = @to; SET @to = @swap; END;

    IF @key = N'revenues'
    BEGIN
        CREATE TABLE #Mini
        (
            Transaction_Date SMALLDATETIME NULL, TotalWallet FLOAT NULL, TotalSupplyWallet FLOAT NULL,
            TotalSupply FLOAT NULL, Net FLOAT NULL, ActValue FLOAT NULL, TotalRev FLOAT NULL,
            TotalSaleDay FLOAT NULL, TotalSaleDay2 FLOAT NULL, TotalSaleDayReturn FLOAT NULL,
            mTotalSalCard FLOAT NULL, TotalSaleDay2Vat FLOAT NULL, TotalSaleDay2VatReturn FLOAT NULL,
            TotalRev2 FLOAT NULL, TotalRevVat FLOAT NULL, BankBalanceCharge MONEY NULL,
            CountTransaction INT NULL, CountTransactionCash INT NULL, CountCards INT NULL,
            TotalRechargeValue FLOAT NULL, CashOut FLOAT NULL, CashOutTotal FLOAT NULL, CashOutDisc FLOAT NULL,
            TotalRevSS FLOAT NULL, NetPOS FLOAT NULL, TotalRevPOS FLOAT NULL, ReturnCount INT NULL
        );
        INSERT INTO #Mini EXEC dbo.RPT_CloseReportTotalMini @FromDate = @from, @ToDate = @to;
        SELECT ROW_NUMBER() OVER (ORDER BY Transaction_Date) AS RowNo,
               0 AS BranchID,
               CONVERT(NVARCHAR(10), Transaction_Date, 120) AS BranchName,
               CAST(ISNULL(TotalSupply,0) AS DECIMAL(18,3)) AS TotalSupply,
               CAST(ISNULL(CountCards,0) AS DECIMAL(18,0)) AS CountCards,
               CAST(ISNULL(TotalSaleDay2Vat,0) AS DECIMAL(18,3)) AS TotalSaleDay2Vat,
               CAST(ISNULL(TotalSaleDay2,0)+ISNULL(TotalSaleDay2Vat,0) AS DECIMAL(18,3)) AS CardValue,
               CAST(ISNULL(CountTransaction,0) AS DECIMAL(18,0)) AS CountTransaction,
               CAST(ISNULL(TotalWallet,0) AS DECIMAL(18,3)) AS WalletBalance,
               CAST(ISNULL(TotalSupplyWallet,0) AS DECIMAL(18,3)) AS WalletSupply,
               CAST(ISNULL(BankBalanceCharge,0) AS DECIMAL(18,3)) AS BankBalanceCharge,
               CAST(ISNULL(TotalRechargeValue,0) AS DECIMAL(18,3)) AS TotalRechargeValue,
               CAST(ISNULL(TotalRev2,0) AS DECIMAL(18,3)) AS TotalRev2,
               CAST(ISNULL(TotalRevVat,0) AS DECIMAL(18,3)) AS TotalRevVat,
               CAST(ISNULL(TotalRev2,0)+ISNULL(TotalRevVat,0) AS DECIMAL(18,3)) AS TotalRevWithVat,
               CAST(ISNULL(ReturnCount,0) AS DECIMAL(18,0)) AS ReturnsCount,
               CAST(ISNULL(TotalSaleDayReturn,0)+ISNULL(TotalSaleDay2VatReturn,0) AS DECIMAL(18,3)) AS TotalReturns,
               CAST(ISNULL(CashOutTotal,0) AS DECIMAL(18,3)) AS NetCashOut,
               CAST(0 AS DECIMAL(18,3)) AS BoxValue,
               N'' AS ClosingStatus,
               CAST(ISNULL(TotalSaleDay2,0) AS DECIMAL(18,3)) AS TotalSaleDay2,
               CAST(0 AS DECIMAL(18,0)) AS CountTransactionIn,
               CAST(ISNULL(CountTransactionCash,0) AS DECIMAL(18,0)) AS CountTransactionOut,
               CAST(ISNULL(CashOut,0) AS DECIMAL(18,3)) AS WalletNet,
               CAST(ISNULL(TotalRevPOS,0)+ISNULL(NetPOS,0) AS DECIMAL(18,3)) AS PosTotal,
               CAST(0 AS DECIMAL(18,3)) AS TawqeefiTotal,
               CAST(0 AS DECIMAL(18,3)) AS TotalInstallmentRevVat,
               CAST(0 AS DECIMAL(18,3)) AS InstallmentTotal,
               CAST(0 AS DECIMAL(18,0)) AS CountInstallment,
               CAST(0 AS DECIMAL(18,0)) AS CountTransactionPOS,
               CAST(0 AS DECIMAL(18,3)) AS NetPosFinalProfit,
               CAST(0 AS DECIMAL(18,3)) AS Bank9995,
               CAST(0 AS DECIMAL(18,3)) AS Bank8846,
               CAST(ISNULL(TotalWallet,0) AS DECIMAL(18,3)) AS TotalWallet,
               CAST(ISNULL(TotalSupplyWallet,0) AS DECIMAL(18,3)) AS TotalSupplyWallet,
               CAST(ISNULL(TotalRev,0) AS DECIMAL(18,3)) AS TotalRev,
               CAST(ISNULL(Net,0) AS DECIMAL(18,3)) AS Net,
               CAST(ISNULL(ActValue,0) AS DECIMAL(18,3)) AS ActValue,
               CONVERT(DATE, Transaction_Date) AS ReportDate
        FROM #Mini
        WHERE @showEmptyBranches = 1 OR ISNULL(TotalSupply,0) <> 0 OR ISNULL(TotalRechargeValue,0) <> 0
           OR ISNULL(TotalWallet,0) <> 0 OR ISNULL(TotalSaleDay2,0) <> 0 OR ISNULL(TotalRev2,0) <> 0 OR ISNULL(BankBalanceCharge,0) <> 0
        ORDER BY Transaction_Date;
        RETURN;
    END;

    IF @key = N'general-sales'
    BEGIN
        CREATE TABLE #G
        (
            TotalWallet FLOAT NULL, TotalSupplyWallet FLOAT NULL, TotalSupply FLOAT NULL, Net FLOAT NULL, ActValue FLOAT NULL, TotalRev FLOAT NULL,
            branch_id INT NULL, ActivityTypeId INT NULL, branch_name NVARCHAR(50) NULL, branch_namee NVARCHAR(50) NULL, manger NVARCHAR(50) NULL,
            Tel NVARCHAR(50) NULL, Remarks NVARCHAR(50) NULL, branch_Code NVARCHAR(255) NULL, Account_Code NVARCHAR(255) NULL, Users NVARCHAR(255) NULL,
            branchLogo IMAGE NULL, ShowlogoInReports BIT NULL, VATNO NVARCHAR(255) NULL, RegionID INT NULL, Beauty INT NULL, StoreId INT NULL,
            Account_Code2 NVARCHAR(250) NULL, GovernmentID INT NULL, ItemIDB INT NULL, BankBalanceCharge FLOAT NULL, BankBalance FLOAT NULL,
            IsStoped BIT NULL, IsStopedDate DATETIME NULL, BranchID INT NULL, BoxBalance INT NULL, TotalReturns FLOAT NULL, CountReturns INT NULL,
            TotalSaleDay FLOAT NULL, TotalSaleDay2 FLOAT NULL, TotalSaleDay2Vat FLOAT NULL, TotalSaleDay2Vat_All FLOAT NULL, TotalSaleDay2Vat_NBE FLOAT NULL,
            CountTransaction INT NULL, CountCards INT NULL, CountCashOut INT NULL, TotalRechargeValue FLOAT NULL, TotalVat FLOAT NULL,
            mTotalSalCard FLOAT NULL, mTotalSalCard_All FLOAT NULL, mTotalSalCard_NBE FLOAT NULL, CountBankMisrCard INT NULL, CountNBECard INT NULL,
            TotalRev2 FLOAT NULL, TotalRevVat FLOAT NULL, CashOut FLOAT NULL, CashOutTotal FLOAT NULL, CashOutDisc FLOAT NULL, TotalRevSS FLOAT NULL,
            NetPOS FLOAT NULL, TotalRevPOS FLOAT NULL, CountPOS INT NULL, TotalTawki3y FLOAT NULL, CountTawki3y INT NULL, InstallmentTotal FLOAT NULL,
            CountInstallment INT NULL, TotalBankMisrCard FLOAT NULL, TotalNBECard FLOAT NULL, CountViolations INT NULL, TotalViolationsValue FLOAT NULL,
            CountViolationsDetails INT NULL, TotalViolationsDetailsPrice FLOAT NULL
        );
        INSERT INTO #G EXEC dbo.RPT_CloseReportTotal_VB @FromDate = @from, @ToDate = @to, @UserId = @userId;
        SELECT ROW_NUMBER() OVER (ORDER BY ISNULL(branch_Code,N''), ISNULL(branch_name,N''), ISNULL(BranchID, branch_id)) AS RowNo,
               ISNULL(BranchID, branch_id) AS BranchID,
               LTRIM(RTRIM(COALESCE(NULLIF(branch_Code,N'')+N' ',N'') + COALESCE(NULLIF(branch_name,N''), NULLIF(branch_namee,N''), N'??? '+CONVERT(NVARCHAR(20), ISNULL(BranchID, branch_id))))) AS BranchName,
               CAST(ISNULL(TotalSupply,0) AS DECIMAL(18,3)) AS TotalSupply,
               CAST(ISNULL(CountCards,0) AS DECIMAL(18,0)) AS CountCards,
               CAST(ISNULL(TotalSaleDay2Vat,0) AS DECIMAL(18,3)) AS TotalSaleDay2Vat,
               CAST(ISNULL(TotalBankMisrCard, ISNULL(mTotalSalCard,0)+ISNULL(TotalSaleDay2Vat,0)) AS DECIMAL(18,3)) AS CardValue,
               CAST(ISNULL(CountTransaction,0) AS DECIMAL(18,0)) AS CountTransaction,
               CAST(ISNULL(TotalWallet,0) AS DECIMAL(18,3)) AS WalletBalance,
               CAST(ISNULL(TotalSupplyWallet,0) AS DECIMAL(18,3)) AS WalletSupply,
               CAST(ISNULL(BankBalanceCharge,0) AS DECIMAL(18,3)) AS BankBalanceCharge,
               CAST(ISNULL(TotalRechargeValue,0) AS DECIMAL(18,3)) AS TotalRechargeValue,
               CAST(ISNULL(TotalRev2,0) AS DECIMAL(18,3)) AS TotalRev2,
               CAST(ISNULL(TotalRevVat,0) AS DECIMAL(18,3)) AS TotalRevVat,
               CAST(ISNULL(TotalRev2,0)+ISNULL(TotalRevVat,0) AS DECIMAL(18,3)) AS TotalRevWithVat,
               CAST(ISNULL(CountReturns,0) AS DECIMAL(18,0)) AS ReturnsCount,
               CAST(ISNULL(TotalReturns,0) AS DECIMAL(18,3)) AS TotalReturns,
               CAST(ISNULL(CashOutTotal,0) AS DECIMAL(18,3)) AS NetCashOut,
               CAST(ISNULL(BoxBalance,0) AS DECIMAL(18,3)) AS BoxValue,
               N'' AS ClosingStatus,
               CAST(ISNULL(TotalSaleDay2,0) AS DECIMAL(18,3)) AS TotalSaleDay2,
               CAST(0 AS DECIMAL(18,0)) AS CountTransactionIn,
               CAST(ISNULL(CountCashOut,0) AS DECIMAL(18,0)) AS CountTransactionOut,
               CAST(ISNULL(CashOut,0) AS DECIMAL(18,3)) AS WalletNet,
               CAST(ISNULL(TotalRevPOS,0)+ISNULL(NetPOS,0) AS DECIMAL(18,3)) AS PosTotal,
               CAST(ISNULL(TotalTawki3y,0) AS DECIMAL(18,3)) AS TawqeefiTotal,
               CAST(0 AS DECIMAL(18,3)) AS TotalInstallmentRevVat,
               CAST(ISNULL(InstallmentTotal,0) AS DECIMAL(18,3)) AS InstallmentTotal,
               CAST(ISNULL(CountInstallment,0) AS DECIMAL(18,0)) AS CountInstallment,
               CAST(ISNULL(CountPOS,0) AS DECIMAL(18,0)) AS CountTransactionPOS,
               CAST(0 AS DECIMAL(18,3)) AS NetPosFinalProfit, CAST(0 AS DECIMAL(18,3)) AS Bank9995, CAST(0 AS DECIMAL(18,3)) AS Bank8846,
               CAST(ISNULL(TotalWallet,0) AS DECIMAL(18,3)) AS TotalWallet, CAST(ISNULL(TotalSupplyWallet,0) AS DECIMAL(18,3)) AS TotalSupplyWallet,
               CAST(ISNULL(TotalRev,0) AS DECIMAL(18,3)) AS TotalRev, CAST(ISNULL(Net,0) AS DECIMAL(18,3)) AS Net, CAST(ISNULL(ActValue,0) AS DECIMAL(18,3)) AS ActValue,
               CAST(ISNULL(TotalSaleDay2Vat_All,0) AS DECIMAL(18,3)) AS TotalSaleDay2Vat_All, CAST(ISNULL(TotalSaleDay2Vat_NBE,0) AS DECIMAL(18,3)) AS TotalSaleDay2Vat_NBE,
               CAST(ISNULL(mTotalSalCard_All,0) AS DECIMAL(18,3)) AS mTotalSalCard_All, CAST(ISNULL(mTotalSalCard_NBE,0) AS DECIMAL(18,3)) AS mTotalSalCard_NBE,
               CAST(ISNULL(TotalNBECard,0) AS DECIMAL(18,3)) AS TotalNBECard, CAST(ISNULL(CountBankMisrCard,0) AS DECIMAL(18,0)) AS CountBankMisrCard, CAST(ISNULL(CountNBECard,0) AS DECIMAL(18,0)) AS CountNBECard
        FROM #G
        WHERE (@branchId <= 0 OR ISNULL(BranchID, branch_id) = @branchId)
          AND (@branchId > 0 OR @branchFromId IS NULL OR ISNULL(BranchID, branch_id) >= @branchFromId)
          AND (@branchId > 0 OR @branchToId IS NULL OR ISNULL(BranchID, branch_id) <= @branchToId)
          AND (@storeId IS NULL OR StoreId = @storeId)
          AND (@showEmptyBranches = 1 OR ISNULL(TotalSupply,0) <> 0 OR ISNULL(TotalRechargeValue,0) <> 0 OR ISNULL(TotalWallet,0) <> 0 OR ISNULL(TotalSaleDay2,0) <> 0 OR ISNULL(TotalRev2,0) <> 0 OR ISNULL(TotalReturns,0) <> 0 OR ISNULL(BankBalanceCharge,0) <> 0)
        ORDER BY ISNULL(branch_Code,N''), ISNULL(branch_name,N''), ISNULL(BranchID, branch_id);
        RETURN;
    END;

    CREATE TABLE #F
    (
        TotalWallet FLOAT NULL, TotalSupplyWallet FLOAT NULL, TotalSupply FLOAT NULL, Net FLOAT NULL, ActValue FLOAT NULL, TotalRev FLOAT NULL,
        branch_id INT NULL, branch_name NVARCHAR(50) NULL, branch_Code NVARCHAR(255) NULL, BranchID INT NULL, BoxBalance INT NULL, BankBalanceCharge MONEY NULL,
        TotalSaleDay FLOAT NULL, PosValue MONEY NULL, TotalSaleDay2 FLOAT NULL, TotalReturns FLOAT NULL, CountReturns INT NULL, TotalSaleDay2Vat FLOAT NULL,
        CountTransaction INT NULL, txtCountCashOut INT NULL, CountTransactionCash INT NULL, CountCards INT NULL, TotalRechargeValue FLOAT NULL, TotalVat FLOAT NULL,
        mTotalSalCard FLOAT NULL, TotalRev2 FLOAT NULL, TotalRevVat FLOAT NULL, CashOut FLOAT NULL, CashOutTotal FLOAT NULL, CashOutDisc FLOAT NULL,
        TotalRevSS FLOAT NULL, NetPOS FLOAT NULL, TotalRevPOS FLOAT NULL, TotalInstallmentRevVat FLOAT NULL, TotalInstallmentRev FLOAT NULL, InstallmentTotal FLOAT NULL,
        CountInstallment INT NULL, BoxValue FLOAT NULL, IsClosed INT NULL, CloseStatus NVARCHAR(10) NULL, CountViolations INT NULL, TotalViolationsValue FLOAT NULL,
        CountViolationsDetails INT NULL, TotalViolationsDetailsPrice FLOAT NULL
    );
    INSERT INTO #F EXEC dbo.RPT_CloseReportTotal @FromDate = @from, @ToDate = @to, @UserId = @userId, @POSAccountCode = N'a3a1a1a1a6';
    SELECT ROW_NUMBER() OVER (ORDER BY ISNULL(branch_Code,N''), ISNULL(branch_name,N''), ISNULL(BranchID, branch_id)) AS RowNo,
           ISNULL(BranchID, branch_id) AS BranchID,
           LTRIM(RTRIM(COALESCE(NULLIF(branch_Code,N'')+N' ',N'') + COALESCE(NULLIF(branch_name,N''), N'??? '+CONVERT(NVARCHAR(20), ISNULL(BranchID, branch_id))))) AS BranchName,
           CAST(ISNULL(TotalSupply,0) AS DECIMAL(18,3)) AS TotalSupply,
           CAST(ISNULL(CountCards,0) AS DECIMAL(18,0)) AS CountCards,
           CAST(ISNULL(TotalSaleDay2Vat,0) AS DECIMAL(18,3)) AS TotalSaleDay2Vat,
           CAST(ISNULL(mTotalSalCard,0)+ISNULL(TotalSaleDay2Vat,0) AS DECIMAL(18,3)) AS CardValue,
           CAST(ISNULL(CountTransaction,0) AS DECIMAL(18,0)) AS CountTransaction,
           CAST(ISNULL(TotalWallet,0) AS DECIMAL(18,3)) AS WalletBalance,
           CAST(ISNULL(TotalSupplyWallet,0) AS DECIMAL(18,3)) AS WalletSupply,
           CAST(ISNULL(BankBalanceCharge,0) AS DECIMAL(18,3)) AS BankBalanceCharge,
           CAST(ISNULL(TotalRechargeValue,0) AS DECIMAL(18,3)) AS TotalRechargeValue,
           CAST(ISNULL(TotalRev2,0) AS DECIMAL(18,3)) AS TotalRev2,
           CAST(ISNULL(TotalRevVat,0) AS DECIMAL(18,3)) AS TotalRevVat,
           CAST(ISNULL(TotalRev2,0)+ISNULL(TotalRevVat,0) AS DECIMAL(18,3)) AS TotalRevWithVat,
           CAST(ISNULL(CountReturns,0) AS DECIMAL(18,0)) AS ReturnsCount,
           CAST(ISNULL(TotalReturns,0) AS DECIMAL(18,3)) AS TotalReturns,
           CAST(ISNULL(CashOutTotal,0) AS DECIMAL(18,3)) AS NetCashOut,
           CAST(ISNULL(BoxValue,0) AS DECIMAL(18,3)) AS BoxValue,
           ISNULL(CloseStatus,N'') AS ClosingStatus,
           CAST(ISNULL(TotalSaleDay2,0) AS DECIMAL(18,3)) AS TotalSaleDay2,
           CAST(0 AS DECIMAL(18,0)) AS CountTransactionIn,
           CAST(ISNULL(txtCountCashOut,0) AS DECIMAL(18,0)) AS CountTransactionOut,
           CAST(ISNULL(CashOut,0) AS DECIMAL(18,3)) AS WalletNet,
           CAST(ISNULL(TotalRevPOS,0)+ISNULL(NetPOS,0) AS DECIMAL(18,3)) AS PosTotal,
           CAST(0 AS DECIMAL(18,3)) AS TawqeefiTotal,
           CAST(ISNULL(TotalInstallmentRevVat,0) AS DECIMAL(18,3)) AS TotalInstallmentRevVat,
           CAST(ISNULL(InstallmentTotal,0) AS DECIMAL(18,3)) AS InstallmentTotal,
           CAST(ISNULL(CountInstallment,0) AS DECIMAL(18,0)) AS CountInstallment,
           CAST(0 AS DECIMAL(18,0)) AS CountTransactionPOS,
           CAST(0 AS DECIMAL(18,3)) AS NetPosFinalProfit, CAST(0 AS DECIMAL(18,3)) AS Bank9995, CAST(0 AS DECIMAL(18,3)) AS Bank8846,
           CAST(ISNULL(TotalWallet,0) AS DECIMAL(18,3)) AS TotalWallet, CAST(ISNULL(TotalSupplyWallet,0) AS DECIMAL(18,3)) AS TotalSupplyWallet,
           CAST(ISNULL(TotalRev,0) AS DECIMAL(18,3)) AS TotalRev, CAST(ISNULL(Net,0) AS DECIMAL(18,3)) AS Net, CAST(ISNULL(ActValue,0) AS DECIMAL(18,3)) AS ActValue,
           CAST(ISNULL(PosValue,0) AS DECIMAL(18,3)) AS PosValue, CAST(ISNULL(TotalInstallmentRev,0) AS DECIMAL(18,3)) AS TotalInstallmentRev,
           CAST(ISNULL(CountViolations,0) AS DECIMAL(18,0)) AS CountViolations, CAST(ISNULL(TotalViolationsValue,0) AS DECIMAL(18,3)) AS TotalViolationsValue,
           CAST(ISNULL(CountViolationsDetails,0) AS DECIMAL(18,0)) AS CountViolationsDetails, CAST(ISNULL(TotalViolationsDetailsPrice,0) AS DECIMAL(18,3)) AS TotalViolationsDetailsPrice
    FROM #F
    WHERE (@branchId <= 0 OR ISNULL(BranchID, branch_id) = @branchId)
      AND (@branchId > 0 OR @branchFromId IS NULL OR ISNULL(BranchID, branch_id) >= @branchFromId)
      AND (@branchId > 0 OR @branchToId IS NULL OR ISNULL(BranchID, branch_id) <= @branchToId)
      AND (@showEmptyBranches = 1 OR ISNULL(TotalSupply,0) <> 0 OR ISNULL(TotalRechargeValue,0) <> 0 OR ISNULL(TotalWallet,0) <> 0 OR ISNULL(TotalSaleDay2,0) <> 0 OR ISNULL(TotalRev2,0) <> 0 OR ISNULL(TotalReturns,0) <> 0 OR ISNULL(BankBalanceCharge,0) <> 0 OR ISNULL(BoxValue,0) <> 0)
    ORDER BY ISNULL(branch_Code,N''), ISNULL(branch_name,N''), ISNULL(BranchID, branch_id);
END;
GO

