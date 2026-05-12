/*
    POS Sales Representatives Performance Dashboard
    SQL Server 2012 compatible. Read-only report procedure.

    Verified source tables:
    - dbo.Transactions: Transaction_Type, Transaction_Date, UserID, Emp_ID, empid, BranchId,
      RechargeValue, NetValue, VAT, Transaction_NetValue, PayedValue, IsCashOut, IsPOS,
      TrafficViolations, ViolationsValue, VisaNumber.
    - dbo.TblUsers: UserID, UserName, Empid, BranchId, isDeactivated.
    - dbo.TblEmployee: Emp_ID, Emp_Name.
    - dbo.TblBranchesData: branch_id, branch_name, branch_namee.
    - dbo.POS_UserPermissions: PermissionKey = CanTeller.
    - dbo.TBLSalesRepData/TBLSalesRepData2/TBLSalesRepData3: registered sales representatives by EmpID.
*/

IF OBJECT_ID(N'dbo.usp_POS_SalesRepresentativesPerformanceDashboard', N'P') IS NOT NULL
    DROP PROCEDURE dbo.usp_POS_SalesRepresentativesPerformanceDashboard;
GO

CREATE PROCEDURE dbo.usp_POS_SalesRepresentativesPerformanceDashboard
    @FromDate DATETIME,
    @ToDate DATETIME,
    @BranchId INT = NULL,
    @UserId INT = NULL,
    @ServiceType NVARCHAR(30) = NULL,
    @MonthlyRechargeTarget DECIMAL(18, 2) = NULL,
    @MonthlyCardTarget INT = NULL,
    @WorkingDaysInMonth INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @From DATETIME = CONVERT(DATE, ISNULL(@FromDate, GETDATE()));
    DECLARE @To DATETIME = CONVERT(DATE, ISNULL(@ToDate, @From));
    IF @To < @From
    BEGIN
        DECLARE @Swap DATETIME = @From;
        SET @From = @To;
        SET @To = @Swap;
    END;

    DECLARE @ToExclusive DATETIME = DATEADD(DAY, 1, @To);
    DECLARE @MonthStart DATETIME = DATEADD(DAY, 1 - DAY(@To), CONVERT(DATE, @To));
    DECLARE @NextMonthStart DATETIME = DATEADD(MONTH, 1, @MonthStart);
    DECLARE @DaysInMonth INT = DAY(DATEADD(DAY, -1, @NextMonthStart));
    DECLARE @ElapsedDays INT = DATEDIFF(DAY, @MonthStart, @To) + 1;
    DECLARE @WorkingDays INT = ISNULL(NULLIF(@WorkingDaysInMonth, 0), @DaysInMonth);
    DECLARE @RemainingDays INT;

    IF @ElapsedDays < 1 SET @ElapsedDays = 1;
    IF @ElapsedDays > @WorkingDays SET @ElapsedDays = @WorkingDays;
    SET @RemainingDays = @WorkingDays - @ElapsedDays;
    IF @RemainingDays < 1 SET @RemainingDays = 1;

    SET @ServiceType = LOWER(LTRIM(RTRIM(ISNULL(@ServiceType, N''))));

    ;WITH SourceTransactions AS
    (
        SELECT
            t.Transaction_ID,
            t.Transaction_Date,
            t.UserID,
            EmpId = COALESCE(t.Emp_ID, t.empid, u.Empid),
            t.BranchId,
            BranchName = COALESCE(NULLIF(b.branch_name, N''), NULLIF(b.branch_namee, N''), N'فرع ' + CONVERT(NVARCHAR(20), t.BranchId)),
            RepresentativeName = COALESCE(NULLIF(e.Emp_Name, N''), NULLIF(u.UserName, N''), N'User ' + CONVERT(NVARCHAR(20), t.UserID)),
            ServiceType =
                CASE
                    WHEN ISNULL(t.TrafficViolations, 0) = 1 THEN N'violations'
                    WHEN ISNULL(t.IsPOS, 0) = 1 OR NULLIF(LTRIM(RTRIM(ISNULL(t.VisaNumber, N''))), N'') IS NOT NULL THEN N'card'
                    WHEN ISNULL(t.IsCashOut, 0) = 1 THEN N'cash-out'
                    ELSE N'cash-in'
                END,
            RechargeValue = CONVERT(DECIMAL(18, 2), ISNULL(t.RechargeValue, 0)),
            FeesValue = CONVERT(DECIMAL(18, 2), ISNULL(t.NetValue, 0)),
            VatValue = CONVERT(DECIMAL(18, 2), ISNULL(t.VAT, 0)),
            TotalValue = CONVERT(DECIMAL(18, 2), ISNULL(t.Transaction_NetValue, ISNULL(t.PayedValue, 0))),
            ViolationsValue = CONVERT(DECIMAL(18, 2), ISNULL(t.ViolationsValue, 0))
        FROM dbo.Transactions t WITH (NOLOCK)
        LEFT JOIN dbo.TblUsers u WITH (NOLOCK) ON u.UserID = t.UserID
        LEFT JOIN dbo.TblEmployee e WITH (NOLOCK) ON e.Emp_ID = COALESCE(t.Emp_ID, t.empid, u.Empid)
        LEFT JOIN dbo.TblBranchesData b WITH (NOLOCK) ON b.branch_id = t.BranchId
        WHERE t.Transaction_Type = 21
          AND t.Transaction_Date >= @From
          AND t.Transaction_Date < @ToExclusive
          AND (@BranchId IS NULL OR t.BranchId = @BranchId)
          AND (@UserId IS NULL OR t.UserID = @UserId)
          AND
          (
                LTRIM(RTRIM(ISNULL(u.UserCategory, N''))) IN (N'تلر', N'Teller')
                OR EXISTS
                (
                    SELECT 1
                    FROM dbo.POS_UserPermissions p WITH (NOLOCK)
                    WHERE p.UserID = u.UserID
                      AND p.PermissionKey = N'CanTeller'
                      AND ISNULL(p.IsAllowed, 0) = 1
                )
                OR EXISTS (SELECT 1 FROM dbo.TBLSalesRepData sr WITH (NOLOCK) WHERE sr.EmpID = COALESCE(t.Emp_ID, t.empid, u.Empid))
                OR EXISTS (SELECT 1 FROM dbo.TBLSalesRepData2 sr WITH (NOLOCK) WHERE sr.EmpID = COALESCE(t.Emp_ID, t.empid, u.Empid))
                OR EXISTS (SELECT 1 FROM dbo.TBLSalesRepData3 sr WITH (NOLOCK) WHERE sr.EmpID = COALESCE(t.Emp_ID, t.empid, u.Empid))
          )
    ),
    Filtered AS
    (
        SELECT *
        FROM SourceTransactions
        WHERE @ServiceType = N''
           OR ServiceType = @ServiceType
    ),
    Agg AS
    (
        SELECT
            UserId = ISNULL(UserID, 0),
            EmpId = MAX(EmpId),
            RepresentativeName = MAX(RepresentativeName),
            BranchId = MAX(BranchId),
            BranchName = MAX(BranchName),
            CashInTotal = SUM(CASE WHEN ServiceType = N'cash-in' THEN RechargeValue ELSE 0 END),
            CashInCount = SUM(CASE WHEN ServiceType = N'cash-in' THEN 1 ELSE 0 END),
            CashInFees = SUM(CASE WHEN ServiceType = N'cash-in' THEN FeesValue + VatValue ELSE 0 END),
            CashOutWithoutFees = SUM(CASE WHEN ServiceType = N'cash-out' THEN RechargeValue ELSE 0 END),
            CashOutWithFees = SUM(CASE WHEN ServiceType = N'cash-out' THEN RechargeValue + FeesValue + VatValue ELSE 0 END),
            CashOutFees = SUM(CASE WHEN ServiceType = N'cash-out' THEN FeesValue + VatValue ELSE 0 END),
            CashOutCount = SUM(CASE WHEN ServiceType = N'cash-out' THEN 1 ELSE 0 END),
            ViolationsTotal = SUM(CASE WHEN ServiceType = N'violations' THEN ViolationsValue ELSE 0 END),
            ViolationsFees = SUM(CASE WHEN ServiceType = N'violations' THEN FeesValue + VatValue ELSE 0 END),
            ViolationsCount = SUM(CASE WHEN ServiceType = N'violations' THEN 1 ELSE 0 END),
            CardsTotal = SUM(CASE WHEN ServiceType = N'card' THEN TotalValue ELSE 0 END),
            CardsCount = SUM(CASE WHEN ServiceType = N'card' THEN 1 ELSE 0 END)
        FROM Filtered
        GROUP BY ISNULL(UserID, 0)
    ),
    Calc AS
    (
        SELECT
            a.*,
            FeesTotal = CashInFees + CashOutFees + ViolationsFees,
            ExpectedCashSupply = CashInTotal + CardsTotal + CashInFees + ViolationsFees - CashOutWithoutFees,
            RechargeTarget = CONVERT(DECIMAL(18, 2), ISNULL(@MonthlyRechargeTarget, ISNULL(tg.MonthlyRechargeTarget, 0))),
            CardTarget = CONVERT(DECIMAL(18, 2), ISNULL(@MonthlyCardTarget, ISNULL(tg.MonthlyCardTarget, 0))),
            WorkingDaysForProjection = ISNULL(NULLIF(@WorkingDaysInMonth, 0), ISNULL(NULLIF(tg.WorkingDaysInMonth, 0), @DaysInMonth)),
            AchievedRecharge = CashInTotal + CashInFees + CashOutWithFees + ViolationsFees,
            AchievedCards = CONVERT(DECIMAL(18, 2), CardsCount)
        FROM Agg a
        OUTER APPLY
        (
            SELECT TOP (1)
                MonthlyRechargeTarget,
                MonthlyCardTarget,
                WorkingDaysInMonth
            FROM dbo.POS_SalesRepresentativeTargets st WITH (NOLOCK)
            WHERE st.IsActive = 1
              AND st.FromDate <= @To
              AND st.ToDate >= @From
              AND (st.UserID = a.UserId OR st.UserID IS NULL)
              AND (@BranchId IS NULL OR st.BranchId IS NULL OR st.BranchId = @BranchId)
            ORDER BY
                CASE WHEN st.UserID = a.UserId THEN 0 ELSE 1 END,
                CASE WHEN @BranchId IS NOT NULL AND st.BranchId = @BranchId THEN 0 WHEN st.BranchId IS NULL THEN 1 ELSE 2 END,
                st.TargetId DESC
        ) tg
    ),
    Calc2 AS
    (
        SELECT
            *,
            RowElapsedDays =
                CASE
                    WHEN @ElapsedDays < 1 THEN 1
                    WHEN @ElapsedDays > WorkingDaysForProjection THEN WorkingDaysForProjection
                    ELSE @ElapsedDays
                END,
            RowRemainingDays =
                CASE
                    WHEN WorkingDaysForProjection -
                        CASE
                            WHEN @ElapsedDays < 1 THEN 1
                            WHEN @ElapsedDays > WorkingDaysForProjection THEN WorkingDaysForProjection
                            ELSE @ElapsedDays
                        END < 1 THEN 1
                    ELSE WorkingDaysForProjection -
                        CASE
                            WHEN @ElapsedDays < 1 THEN 1
                            WHEN @ElapsedDays > WorkingDaysForProjection THEN WorkingDaysForProjection
                            ELSE @ElapsedDays
                        END
                END
        FROM Calc
    )
    SELECT
        UserId,
        EmpId,
        RepresentativeName,
        BranchId,
        BranchName,
        FromDate = @From,
        ToDate = @To,
        CashInTotal,
        CashInCount,
        CashOutWithoutFees,
        CashOutWithFees,
        CashOutCount,
        ViolationsTotal,
        ViolationsFees,
        ViolationsCount,
        FeesTotal,
        CardsTotal,
        CardsCount,
        ExpectedCashSupply,
        RechargeTarget,
        CardTarget,
        AchievedRecharge,
        AchievedCards,
        RechargeAchievementPercent = CASE WHEN RechargeTarget > 0 THEN (AchievedRecharge / RechargeTarget) * 100 ELSE 0 END,
        CardAchievementPercent = CASE WHEN CardTarget > 0 THEN (AchievedCards / CardTarget) * 100 ELSE 0 END,
        OverallAchievementPercent =
            CASE
                WHEN RechargeTarget > 0 AND CardTarget > 0 THEN ((AchievedRecharge / RechargeTarget) + (AchievedCards / CardTarget)) * 50
                WHEN RechargeTarget > 0 THEN (AchievedRecharge / RechargeTarget) * 100
                WHEN CardTarget > 0 THEN (AchievedCards / CardTarget) * 100
                ELSE 0
            END,
        RequiredDailyRecharge = CASE WHEN RechargeTarget > AchievedRecharge THEN (RechargeTarget - AchievedRecharge) / RowRemainingDays ELSE 0 END,
        RequiredDailyCards = CASE WHEN CardTarget > AchievedCards THEN (CardTarget - AchievedCards) / RowRemainingDays ELSE 0 END,
        ProjectedRecharge = CASE WHEN RowElapsedDays > 0 THEN (AchievedRecharge / RowElapsedDays) * WorkingDaysForProjection ELSE 0 END,
        ProjectedCards = CASE WHEN RowElapsedDays > 0 THEN (AchievedCards / RowElapsedDays) * WorkingDaysForProjection ELSE 0 END,
        PerformanceStatus =
            CASE
                WHEN CASE
                        WHEN RechargeTarget > 0 AND CardTarget > 0 THEN ((AchievedRecharge / RechargeTarget) + (AchievedCards / CardTarget)) * 50
                        WHEN RechargeTarget > 0 THEN (AchievedRecharge / RechargeTarget) * 100
                        WHEN CardTarget > 0 THEN (AchievedCards / CardTarget) * 100
                        ELSE 0
                     END >= 100 THEN N'ممتاز'
                WHEN CASE
                        WHEN RechargeTarget > 0 AND CardTarget > 0 THEN ((AchievedRecharge / RechargeTarget) + (AchievedCards / CardTarget)) * 50
                        WHEN RechargeTarget > 0 THEN (AchievedRecharge / RechargeTarget) * 100
                        WHEN CardTarget > 0 THEN (AchievedCards / CardTarget) * 100
                        ELSE 0
                     END >= 80 THEN N'جيد'
                WHEN CASE
                        WHEN RechargeTarget > 0 AND CardTarget > 0 THEN ((AchievedRecharge / RechargeTarget) + (AchievedCards / CardTarget)) * 50
                        WHEN RechargeTarget > 0 THEN (AchievedRecharge / RechargeTarget) * 100
                        WHEN CardTarget > 0 THEN (AchievedCards / CardTarget) * 100
                        ELSE 0
                     END > 0 THEN N'يحتاج متابعة'
                ELSE N'خطر'
            END,
        PerformanceClass =
            CASE
                WHEN CASE
                        WHEN RechargeTarget > 0 AND CardTarget > 0 THEN ((AchievedRecharge / RechargeTarget) + (AchievedCards / CardTarget)) * 50
                        WHEN RechargeTarget > 0 THEN (AchievedRecharge / RechargeTarget) * 100
                        WHEN CardTarget > 0 THEN (AchievedCards / CardTarget) * 100
                        ELSE 0
                     END >= 100 THEN N'success'
                WHEN CASE
                        WHEN RechargeTarget > 0 AND CardTarget > 0 THEN ((AchievedRecharge / RechargeTarget) + (AchievedCards / CardTarget)) * 50
                        WHEN RechargeTarget > 0 THEN (AchievedRecharge / RechargeTarget) * 100
                        WHEN CardTarget > 0 THEN (AchievedCards / CardTarget) * 100
                        ELSE 0
                     END >= 80 THEN N'warning'
                ELSE N'danger'
            END
    FROM Calc2
    ORDER BY OverallAchievementPercent DESC, AchievedRecharge DESC, RepresentativeName;
END;
GO
