

CREATE PROCEDURE [dbo].[RPT_CloseReportTotal_VB]

    @FromDate date,

    @ToDate   date,

    @UserId   int

AS

BEGIN

    SET NOCOUNT ON;



    ;WITH AllowedBranches AS (

        SELECT b.branch_id

        FROM dbo.TblBranchesData b

        WHERE ISNULL(b.isStoped,0) = 0

           OR ISNULL(b.IsStopedDate,'9999-12-31') > @FromDate

    ),

    UserBranches AS (

        SELECT ub.BranchID

        FROM dbo.TblUsersBranches ub

        WHERE ub.UserID = @UserId

    ),

    TargetBranches AS (

        SELECT ab.branch_id

        FROM AllowedBranches ab

        INNER JOIN UserBranches ub

            ON ab.branch_id = ub.BranchID

    ),

    -- معاملات الفترة (زي كودك كله 21)

    BaseTrans AS (

        SELECT t.*

        FROM dbo.Transactions t

        INNER JOIN TargetBranches tb

            ON tb.branch_id = t.BranchId

        WHERE t.Transaction_Type = 21

          AND t.Transaction_Date BETWEEN @FromDate AND @ToDate

    ),

    BaseDetails AS (

        SELECT

            td.Transaction_ID,

            td.Item_ID,

            t.BranchId,

            t.Transaction_Date,

            td.Price,

            td.Vat,

            i.ItemType,

            ISNULL(t.IsWallet,0)           AS IsWallet,

            ISNULL(t.HaveGuarantee,0)      AS HaveGuarantee,

            ISNULL(t.IsReturn,0)           AS IsReturn,

            ISNULL(i.HaveSerial,0)         AS HaveSerial,

            ISNULL(t.OtherItems,0)         AS OtherItems,

            ISNULL(t.InstallmentService,0) AS InstallmentService

        FROM dbo.Transaction_Details td

        INNER JOIN BaseTrans t

            ON t.Transaction_ID = td.Transaction_ID

        INNER JOIN dbo.TblItems i

            ON i.ItemID = td.Item_ID

    ),

    -- تجميعات التفاصيل

    DetailsAgg AS (

        SELECT

            BranchId,



            TotalSaleDay = SUM(CASE

                WHEN ItemType = 0

                 AND IsWallet = 0

                 AND HaveGuarantee = 0

                 AND IsReturn = 0

                 AND OtherItems = 0

                 AND HaveSerial = 1

                THEN ISNULL(Price,0) ELSE 0 END),



            TotalSaleDay2 = SUM(CASE

                WHEN ItemType = 0

                 AND IsWallet = 0

                 AND HaveGuarantee = 0

                 AND IsReturn = 0

                 AND OtherItems = 0

                 AND HaveSerial = 1

                THEN ISNULL(Price,0) ELSE 0 END),



            -- الإجمالي العام القديم لكل الكروت

            TotalSaleDay2Vat_All = SUM(CASE

                WHEN ItemType = 0

                 AND IsWallet = 0

                 AND HaveGuarantee = 0

                 AND IsReturn = 0

                 AND OtherItems = 0

                 AND HaveSerial = 1

                THEN ISNULL(Vat,0) ELSE 0 END),



            -- VAT بنك مصر فقط

            TotalSaleDay2Vat = SUM(CASE

                WHEN Item_ID = 1

                 AND ItemType = 0

                 AND IsWallet = 0

                 AND HaveGuarantee = 0

                 AND IsReturn = 0

                 AND OtherItems = 0

                 AND HaveSerial = 1

                THEN ISNULL(Vat,0) ELSE 0 END),



            -- VAT البنك الأهلي فقط

            TotalSaleDay2Vat_NBE = SUM(CASE

                WHEN Item_ID = 19

                 AND ItemType = 0

                 AND IsWallet = 0

                 AND HaveGuarantee = 0

                 AND IsReturn = 0

                 AND OtherItems = 0

                 AND HaveSerial = 1

                THEN ISNULL(Vat,0) ELSE 0 END),



            CountTransaction = SUM(CASE

                WHEN ISNULL(Price,0) <> 0

                 AND ItemType = 1

                 AND IsWallet = 0

                 AND HaveGuarantee = 0

                 AND OtherItems = 0

                 AND HaveSerial = 0

                THEN 1 ELSE 0 END),



            -- الإجمالي العام القديم لكل الكروت

            mTotalSalCard_All = SUM(CASE

 WHEN ItemType = 0

                 AND IsWallet = 0

                 AND HaveGuarantee = 0

                 AND HaveSerial = 1

                THEN ISNULL(Price,0) ELSE 0 END),



            -- قيمة بنك مصر فقط

            mTotalSalCard = SUM(CASE

                WHEN Item_ID = 1

                 AND ItemType = 0

                 AND IsWallet = 0

                 AND HaveGuarantee = 0

                 AND HaveSerial = 1

                THEN ISNULL(Price,0) ELSE 0 END),



            -- قيمة البنك الأهلي فقط

            mTotalSalCard_NBE = SUM(CASE

                WHEN Item_ID = 19

                 AND ItemType = 0

                 AND IsWallet = 0

                 AND HaveGuarantee = 0

                 AND HaveSerial = 1

                THEN ISNULL(Price,0) ELSE 0 END),



            -- عدد عمليات بنك مصر

            CountBankMisrCard = SUM(CASE

                WHEN Item_ID = 1

                 AND ItemType = 0

                 AND IsWallet = 0

                 AND HaveGuarantee = 0

                 AND IsReturn = 0

                 AND OtherItems = 0

                 AND HaveSerial = 1

                THEN 1 ELSE 0 END),



            -- عدد عمليات البنك الأهلي

            CountNBECard = SUM(CASE

                WHEN Item_ID = 19

                 AND ItemType = 0

                 AND IsWallet = 0

                 AND HaveGuarantee = 0

                 AND IsReturn = 0

                 AND OtherItems = 0

                 AND HaveSerial = 1

                THEN 1 ELSE 0 END),



            TotalRev2 = SUM(CASE

                WHEN IsWallet = 0

                 AND HaveGuarantee = 0

                 AND OtherItems = 0

                 AND HaveSerial = 0

                THEN ISNULL(Price,0) ELSE 0 END),



            TotalRevVat = SUM(CASE

                WHEN IsWallet = 0

                 AND HaveGuarantee = 0

                 AND OtherItems = 0

                 AND HaveSerial = 0

                THEN ISNULL(Vat,0) ELSE 0 END)



        FROM BaseDetails

        GROUP BY BranchId

    ),

    -- تجميعات الترانزاكشن

    TransAgg AS (

        SELECT

            BranchId,



            CountCards = SUM(CASE

                WHEN ISNULL(VisaNumber,'') <> ''

                 AND ISNULL(IsWallet,0) = 0

                 AND ISNULL(HaveGuarantee,0) = 0

                THEN 1 ELSE 0 END),



            CountCashOut = SUM(CASE

                WHEN ISNULL(IsWallet,0) = 1

                 AND ISNULL(HaveGuarantee,0) = 0

                 AND ISNULL(OtherItems,0) = 0

                 AND ISNULL(InstallmentService,0) = 0

                THEN 1 ELSE 0 END),



            TotalRechargeValue = SUM(CASE

                WHEN ISNULL(RechargeValue,0) <> 0

                 AND ISNULL(IsWallet,0) = 0

                 AND ISNULL(HaveGuarantee,0) = 0

                 AND ISNULL(OtherItems,0) = 0

                THEN ISNULL(RechargeValue,0) ELSE 0 END),



            TotalVat = SUM(CASE

                WHEN ISNULL(IsWallet,0) = 0

                 AND ISNULL(HaveGuarantee,0) = 0

                 AND ISNULL(OtherItems,0) = 0

                THEN ISNULL(Vat,0) ELSE 0 END),



            CashOut = SUM(CASE

                WHEN ISNULL(IsWallet,0) = 1

                 AND ISNULL(HaveGuarantee,0) = 0

                 AND ISNULL(OtherItems,0) = 0

                 AND ISNULL(InstallmentService,0) = 0

                THEN ISNULL(Transaction_NetValue,0) ELSE 0 END),



            CashOutTotal = SUM(CASE

                WHEN ISNULL(IsWallet,0) = 1

                 AND ISNULL(HaveGuarantee,0) = 0

                 AND ISNULL(OtherItems,0) = 0

                 AND ISNULL(InstallmentService,0) = 0

                THEN ISNULL(RechargeValue,0) ELSE 0 END),



            InstallmentTotal = SUM(CASE

                WHEN ISNULL(InstallmentService,0) = 1

                THEN ISNULL(RechargeValue,0) + ISNULL(AmountLimit,0) + ISNULL(RatioAMount,0)

                ELSE 0 END),



     CountInstallment = SUM(CASE

                WHEN ISNULL(InstallmentService,0) = 1 THEN 1 ELSE 0 END),



            CashOutDisc = SUM(CASE

                WHEN ISNULL(IsWallet,0) = 1

                 AND ISNULL(HaveGuarantee,0) = 0

                 AND ISNULL(OtherItems,0) = 0

                 AND ISNULL(InstallmentService,0) = 0

                THEN

                    CASE

                        WHEN Transaction_Date > '2025-06-30' THEN ISNULL(cost,0) - ISNULL(CashBack,0)

                        WHEN Transaction_Date > '2024-08-25' THEN (ISNULL(RechargeValue,0) + ISNULL(Transaction_NetValue,0)) * 0.008

                        ELSE (ISNULL(RechargeValue,0) + ISNULL(Transaction_NetValue,0)) * 0.01

                    END

                ELSE 0 END),



            TotalRevSS = SUM(CASE

                WHEN ISNULL(IsWallet,0) = 0

                 AND ISNULL(HaveGuarantee,0) = 0

                 AND ISNULL(OtherItems,0) = 0

                 AND ISNULL(InstallmentService,0) = 0

                THEN ISNULL(Transaction_NetValue,0) ELSE 0 END),



            NetPOS = SUM(CASE

                WHEN ISNULL(HaveGuarantee,0) = 1

                 AND ISNULL(RechargeValue,0) <> 0

                THEN ISNULL(RechargeValue,0) ELSE 0 END),



            TotalRevPOS = SUM(CASE

                WHEN ISNULL(HaveGuarantee,0) = 1

                 AND ISNULL(Transaction_NetValue,0) <> 0

                 AND ISNULL(OtherItems,0) = 0

                 AND ISNULL(InstallmentService,0) = 0

                THEN ISNULL(Transaction_NetValue,0) ELSE 0 END),



            CountPOS = SUM(CASE

                WHEN ISNULL(HaveGuarantee,0) = 1

                 AND ISNULL(OtherItems,0) = 0

                 AND ISNULL(InstallmentService,0) = 0

                THEN 1 ELSE 0 END),



            TotalTawki3y = SUM(CASE

                WHEN ISNULL(OtherItems,0) = 1

                THEN ISNULL(Transaction_NetValue,0) ELSE 0 END),



            CountTawki3y = SUM(CASE

                WHEN ISNULL(OtherItems,0) = 1

                THEN 1 ELSE 0 END)



        FROM BaseTrans

        GROUP BY BranchId

    ),

    -- المرتجعات زي VB

    ReturnsAgg AS (

        SELECT

            tr.BranchId,

            SUM(ISNULL(tr.Transaction_NetValue,0) + ISNULL(tr.RechargeValue,0)) AS TotalReturns,

            COUNT(*) AS CountReturns

        FROM dbo.Transactions tr

        INNER JOIN TargetBranches tb

            ON tb.branch_id = tr.BranchId

        WHERE tr.Transaction_Type = 9

          AND tr.Transaction_Date BETWEEN @FromDate AND @ToDate

          AND ISNULL(tr.NoteID3,0) IN (

                SELECT n.NoteID

                FROM dbo.Notes n

                WHERE n.NoteDate BETWEEN @FromDate AND @ToDate

                  AND n.branch_no = tr.BranchId

          )

        GROUP BY tr.BranchId

    ),

    -- المخالفات

    ViolationsAgg AS (

        SELECT

            t.BranchId,

            COUNT(*) AS CountViolations,

            SUM(ISNULL(t.ViolationsValue,0)) AS TotalViolationsValue

        FROM dbo.Transactions t

        INNER JOIN TargetBranches tb

            ON tb.branch_id = t.BranchId

        WHERE t.Transaction_Date BETWEEN @FromDate AND @ToDate

          AND t.Transaction_Type = 21

          AND ISNULL(t.TrafficViolations,0) = 1

        GROUP BY t.BranchId

    ),

    -- مخالفات من التفاصيل

    ViolationsDetailsAgg AS (

        SELECT

            t.BranchId,

            COUNT(*) AS CountViolationsDetails,

            SUM(ISNULL(td.Price,0)) AS TotalViolationsDetailsPrice

        FROM dbo.Transaction_Details td

        INNER JOIN BaseTrans t

            ON t.Transaction_ID = td.Transaction_ID

        INNER JOIN dbo.TblItems i

            ON i.ItemID = td.Item_ID

        WHERE ISNULL(i.TrafficViolations,0) = 1

        GROUP BY t.BranchId

    )



    SELECT

        TotalWallet = ISNULL(TA.CashOutTotal,0) + ISNULL(TA.CashOut,0),



        TotalSupplyWallet = (ISNULL(TA.CashOutTotal,0) + ISNULL(TA.CashOut,0))

                            - ISNULL(TA.CashOutDisc,0),



        TotalSupply = ISNULL(TA.TotalRechargeValue,0)

                      + (ISNULL(TA.TotalRevSS,0) - ISNULL(DA.mTotalSalCard_All,0) - ISNULL(DA.TotalSaleDay2Vat_All,0))

                      + ISNULL(TA.CashOut,0) - ISNULL(TA.CashOutDisc,0),



        Net = ISNULL(TA.TotalRechargeValue,0)

              + 0

              + (ISNULL(TA.TotalRevSS,0) - ISNULL(DA.mTotalSalCard_All,0) - ISNULL(DA.TotalSaleDay2Vat_All,0))

              + ISNULL(DA.mTotalSalCard_All,0) - ISNULL(TA.CashOutTotal,0),



        ActValue = ISNULL(TA.TotalRechargeValue,0)

                   + 0

                   + (ISNULL(TA.TotalRevSS,0) - ISNULL(DA.mTotalSalCard_All,0) - ISNULL(DA.TotalSaleDay2Vat_All,0))

                   + ISNULL(DA.mTotalSalCard_All,0) - ISNULL(TA.CashOutTotal,0),



        TotalRev = ISNULL(TA.TotalRevSS,0)

                   - ISNULL(DA.mTotalSalCard_All,0)

                   - ISNULL(DA.TotalSaleDay2Vat_All,0),



        b.*,

        BranchID   = b.Branch_ID,

        BoxBalance = 0,



        TotalReturns = ISNULL(RA.TotalReturns,0),

        CountReturns = ISNULL(RA.CountReturns,0),



        TotalSaleDay  = ISNULL(DA.TotalSaleDay,0),

        TotalSaleDay2 = ISNULL(DA.TotalSaleDay2,0),



        -- الاسم القديم بقى خاص ببنك مصر

        TotalSaleDay2Vat = ISNULL(DA.TotalSaleDay2Vat,0),



        -- إضافات جديدة

        TotalSaleDay2Vat_All = ISNULL(DA.TotalSaleDay2Vat_All,0),

        TotalSaleDay2Vat_NBE = ISNULL(DA.TotalSaleDay2Vat_NBE,0),



        CountTransaction = ISNULL(DA.CountTransaction,0),



        CountCards   = ISNULL(TA.CountCards,0),

        CountCashOut = ISNULL(TA.CountCashOut,0),



        TotalRechargeValue = ISNULL(TA.TotalRechargeValue,0),

        TotalVat           = ISNULL(TA.TotalVat,0),



        -- الاسم القديم بقى خاص ببنك مصر

        mTotalSalCard = ISNULL(DA.mTotalSalCard,0),



        -- إضافات جديدة

        mTotalSalCard_All = ISNULL(DA.mTotalSalCard_All,0),

        mTotalSalCard_NBE = ISNULL(DA.mTotalSalCard_NBE,0),



        -- عدد العمليات

        CountBankMisrCard = ISNULL(DA.CountBankMisrCard,0),

        CountNBECard      = ISNULL(DA.CountNBECard,0),



        TotalRev2   = ISNULL(DA.TotalRev2,0),

        TotalRevVat = ISNULL(DA.TotalRevVat,0),



        CashOut      = ISNULL(TA.CashOut,0),

        CashOutTotal = ISNULL(TA.CashOutTotal,0),

        CashOutDisc  = ISNULL(TA.CashOutDisc,0),

        TotalRevSS   = ISNULL(TA.TotalRevSS,0),



        NetPOS      = ISNULL(TA.NetPOS,0),

        TotalRevPOS = ISNULL(TA.TotalRevPOS,0),

        CountPOS    = ISNULL(TA.CountPOS,0),



        TotalTawki3y = ISNULL(TA.TotalTawki3y,0),

        CountTawki3y = ISNULL(TA.CountTawki3y,0),



        InstallmentTotal = ISNULL(TA.InstallmentTotal,0),

        CountInstallment = ISNULL(TA.CountInstallment,0),



        -- إجمالي القيم المباشرة

        TotalBankMisrCard = ISNULL(DA.mTotalSalCard,0) + ISNULL(DA.TotalSaleDay2Vat,0),

        TotalNBECard      = ISNULL(DA.mTotalSalCard_NBE,0) + ISNULL(DA.TotalSaleDay2Vat_NBE,0),



        CountViolations             = ISNULL(VA.CountViolations,0),

        TotalViolationsValue        = ISNULL(VA.TotalViolationsValue,0),

        CountViolationsDetails      = ISNULL(VD.CountViolationsDetails,0),

        TotalViolationsDetailsPrice = ISNULL(VD.TotalViolationsDetailsPrice,0)



    FROM dbo.TblBranchesData b

    INNER JOIN TargetBranches tb

        ON tb.branch_id = b.branch_id

    LEFT JOIN DetailsAgg DA

        ON DA.BranchId = b.branch_id

    LEFT JOIN TransAgg TA

        ON TA.BranchId = b.branch_id

    LEFT JOIN ReturnsAgg RA

        ON RA.BranchId = b.branch_id

    LEFT JOIN ViolationsAgg VA

        ON VA.BranchId = b.branch_id

    LEFT JOIN ViolationsDetailsAgg VD

        ON VD.BranchId = b.branch_id

    ORDER BY b.branch_id;

END

