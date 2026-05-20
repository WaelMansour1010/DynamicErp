

CREATE PROCEDURE [dbo].[RPT_CloseReportTotal]

    @FromDate       date,

    @ToDate         date,

    @UserId         int,

    @POSAccountCode nvarchar(50) = NULL   -- „„ﬂ‰  »⁄ Â ›«÷Ì ·Ê „‘ ⁄«Ì“Â

AS

BEGIN

    SET NOCOUNT ON;



    /* ================= 1) «·›—Ê⁄ «·„”„ÊÕ… ================= */

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

        -- «·›—Ê⁄ «·„‘ —ﬂ… »Ì‰ «·„”„ÊÕ Ê» «⁄… «·ÌÊ“—

        SELECT ab.branch_id

        FROM AllowedBranches ab

        INNER JOIN UserBranches ub ON ab.branch_id = ub.BranchID

    ),



    /* =============== 2) „⁄«„·«  «·„»Ì⁄«  (21) ›Ì «·› —… ================= */

    BaseTrans AS (

        SELECT t.*

        FROM dbo.Transactions t

        INNER JOIN TargetBranches tb ON tb.branch_id = t.BranchId

        WHERE t.Transaction_Type IN (21,9)

          AND t.Transaction_Date BETWEEN @FromDate AND @ToDate

    ),



	 /* =============== (2-B) „Œ«·›«  «·„—Ê— œ«Œ· «·› —… ================= */

  ViolationsAgg AS (

    SELECT

        t.BranchId,

        COUNT(*) AS CountViolations,

        SUM(ISNULL(t.ViolationsValue,0)) AS TotalViolationsValue

    FROM dbo.Transactions t

    INNER JOIN TargetBranches tb ON tb.branch_id = t.BranchId

    WHERE t.Transaction_Date BETWEEN @FromDate AND @ToDate

      AND t.Transaction_Type = 21              -- ? Â‰«

      AND ISNULL(t.TrafficViolations,0) = 1

    GROUP BY t.BranchId





    ),



	ViolationsDetailsAgg AS (

    SELECT

        t.BranchId,

        COUNT(*) AS CountViolationsDetails,

        SUM(ISNULL(td.Price,0)) AS TotalViolationsDetailsPrice

    FROM dbo.Transaction_Details td

    INNER JOIN dbo.Transactions t

        ON t.Transaction_ID = td.Transaction_ID

    INNER JOIN dbo.TblItems i

        ON i.ItemID = td.Item_ID

    INNER JOIN TargetBranches tb

        ON tb.branch_id = t.BranchId

    WHERE t.Transaction_Date BETWEEN @FromDate AND @ToDate

      AND t.Transaction_Type = 21

      AND ISNULL(i.TrafficViolations,0) = 1

    GROUP BY t.BranchId

),



    /* =============== 3)  ›«’Ì· «·»‰Êœ ================= */

    BaseTransDetails AS (

        SELECT

            td.Transaction_ID,

            t.BranchId,

            t.Transaction_Date,

            td.Price,

            td.Vat,

            i.ItemType,

            ISNULL(t.IsWallet,0)          AS IsWallet,

            ISNULL(t.HaveGuarantee,0)     AS HaveGuarantee,

            ISNULL(t.IsReturn,0)          AS IsReturn,

            ISNULL(i.HaveSerial,0)        AS HaveSerial,

            ISNULL(t.OtherItems,0)        AS OtherItems,

            ISNULL(t.InstallmentService,0) AS InstallmentService

        FROM dbo.Transaction_Details td

        INNER JOIN BaseTrans t

            ON td.Transaction_ID = t.Transaction_ID

        INNER JOIN dbo.TblItems i

            ON td.Item_ID = i.ItemID

    ),



    /* =============== 4) »‰ﬂ «· Õ’Ì· (“Ì BankBalanceCharge) ================= */

    BankCharge AS (

        SELECT

            d.branch_id,

            SUM(d.Value) AS BankBalanceCharge

        FROM dbo.DOUBLE_ENTREY_VOUCHERS d

        WHERE d.Credit_Or_Debit = 0

          AND d.RecordDate BETWEEN @FromDate AND @ToDate

          AND d.Account_Code = 'a3a1a1a1a6'

          AND d.Posted IS NULL

        GROUP BY d.branch_id

    ),



    /* =============== 5) „»Ì⁄«  «·ﬂ«—  / «·»‰“Ì‰ / ≈·Œ ================= */

    PosValue AS (

        SELECT

            d.branch_id,

            SUM(d.Value) AS PosValue

        FROM dbo.DOUBLE_ENTREY_VOUCHERS d

        WHERE (@POSAccountCode IS NOT NULL AND d.Account_Code = @POSAccountCode)

          AND d.Credit_Or_Debit = 1

          AND d.RecordDate BETWEEN @FromDate AND @ToDate

     GROUP BY d.branch_id

    ),



    /* =============== 6) „Ã„Ê⁄«  «· ›«’Ì· »«·„‰ÿﬁ » «⁄ﬂ ================= */

    DetailsAgg AS (

        SELECT

            btd.BranchId,



            -- TotalSaleDay: ItemType=0, „‘ „Õ›Ÿ…, „‘ ÷„«‰, „‘ „— Ã⁄, „ ”·”·

            SUM(CASE

                    WHEN btd.ItemType = 0

                     AND btd.IsWallet = 0

                     AND btd.HaveGuarantee = 0

                     AND btd.IsReturn = 0

                     AND btd.HaveSerial = 1

					 

                    THEN ISNULL(btd.Price,0)

                    ELSE 0

                END) AS TotalSaleDay,



            -- TotalSaleDay2: ‰›” «··Ì ›Êﬁ + OtherItems = 0

            SUM(CASE

                    WHEN btd.ItemType = 0

                     AND btd.IsWallet = 0

                     AND btd.HaveGuarantee = 0

                     AND btd.IsReturn = 0

                     AND btd.OtherItems = 0

                     AND btd.HaveSerial = 1

                    THEN ISNULL(btd.Price,0)

                    ELSE 0

                END) AS TotalSaleDay2,



            -- TotalSaleDay2Vat

            SUM(CASE

                    WHEN btd.ItemType = 0

                     AND btd.IsWallet = 0

                     AND btd.HaveGuarantee = 0

                     AND btd.IsReturn = 0

                     AND btd.OtherItems = 0

                     AND btd.HaveSerial = 1

                    THEN ISNULL(btd.Vat,0)

                    ELSE 0

                END) AS TotalSaleDay2Vat,



            -- CountTransaction («·Œœ„…): ItemType=1, „‘ „Õ›Ÿ…, „‘ ÷„«‰, OtherItems=0, „‘ „ ”·”·

            SUM(CASE

                    WHEN 1 =1 AND

					btd.ItemType = 1

                     AND btd.IsWallet = 0

                     --AND btd.HaveGuarantee = 0

                     --AND btd.OtherItems = 0

                     --AND btd.HaveSerial = 0

                     AND ISNULL(btd.Price,0) <> 0

                    THEN 1 ELSE 0 END) AS CountTransaction,



					            -- CountCards: ⁄œœ «·ﬂ—Ê  „‰  ›«’Ì· «·›« Ê—… »‰«¡ ⁄·Ï HaveSerial ›Ì „·› «·’‰›

            SUM(CASE

                    WHEN btd.ItemType = 0

                     AND btd.IsWallet = 0

                     AND btd.HaveGuarantee = 0

                     AND btd.IsReturn = 0

                     AND btd.OtherItems = 0

                     AND btd.InstallmentService = 0

                     AND btd.HaveSerial = 1

                    THEN 1 ELSE 0

                END) AS CountCards,



            -- CountTransactionCash (Œœ„«  ·ﬂ‰ „Õ›Ÿ…)

            SUM(CASE

                    WHEN btd.ItemType = 1

                     AND btd.IsWallet = 1

                     AND btd.OtherItems = 0

                     AND btd.InstallmentService = 0

                     AND ISNULL(btd.Price,0) <> 0

                    THEN 1 ELSE 0 END) AS CountTransactionCash,



            -- mTotalSalCard: ItemType=0, „‘ „Õ›Ÿ…, „‘ ÷„«‰, „‘  ﬁ”Ìÿ, OtherItems=0, „ ”·”·

            SUM(CASE

                    WHEN btd.ItemType = 0

                     AND btd.IsWallet = 0

                     AND btd.HaveGuarantee = 0

                     AND btd.InstallmentService = 0

                     AND btd.OtherItems = 0

                     AND btd.HaveSerial = 1

                    THEN ISNULL(btd.Price,0)

                    ELSE 0

                END) AS mTotalSalCard,



            -- TotalRev2: „‘ „Õ›Ÿ…, „‘ ÷„«‰, OtherItems=0, €Ì— „ ”·”·

            SUM(CASE

                    WHEN btd.IsWallet = 0

                     AND btd.HaveGuarantee = 0

                     AND btd.OtherItems = 0

                     AND btd.HaveSerial = 0

                    THEN ISNULL(btd.Price,0)

                    ELSE 0

                END) AS TotalRev2,



            -- TotalRevVat: ‰›” «··Ì ›Êﬁ »” VAT

            SUM(CASE

                    WHEN btd.IsWallet = 0

                     AND btd.HaveGuarantee = 0

                     AND btd.HaveSerial = 0

                    THEN ISNULL(btd.Vat,0)

                    ELSE 0

                END) AS TotalRevVat



        FROM BaseTransDetails btd

        GROUP BY btd.BranchId

    ),



    /* =============== 7) „⁄«„·«  «· —«‰“«ﬂ‘‰ ‰›”Â« (⁄·‘«‰ «·„Õ›Ÿ… Ê»«ﬁÌ «·√—ﬁ«„) ================= */

    TransAgg AS (

        SELECT

            bt.BranchId,



            -- ‘Õ‰ „Õ›Ÿ… (ﬁÌ„… «·‘Õ‰)

            SUM(CASE WHEN ISNULL(bt.IsWallet,0)=1 AND ISNULL(bt.InstallmentService,0)=0 AND ISNULL(bt.OtherItems,0)=0

                     THEN ISNULL(bt.RechargeValue,0) ELSE 0 END) AS CashOutTotal,



            -- ”Õ» „‰ «·„Õ›Ÿ… (’«›Ì «·⁄„·Ì…)

            SUM(CASE WHEN ISNULL(bt.IsWallet,0)=1 AND ISNULL(bt.InstallmentService,0)=0 AND ISNULL(bt.OtherItems,0)=0

                     THEN ISNULL(bt.Transaction_NetValue,0) ELSE 0 END) AS CashOut,



            -- Œ’„ «·„Õ›Ÿ…

            SUM(CASE

                    WHEN ISNULL(bt.IsWallet,0)=1 THEN

                        CASE

                            WHEN bt.Transaction_Date > '2025-06-30' THEN ISNULL(bt.cost,0) - ISNULL(bt.CashBack,0)

                            WHEN bt.Transaction_Date > '2024-08-25' THEN (ISNULL(bt.RechargeValue,0) + ISNULL(bt.Transaction_NetValue,0)) * 0.008

                            ELSE (ISNULL(bt.RechargeValue,0) + ISNULL(bt.Transaction_NetValue,0)) * 0.01

                        END

                    ELSE 0

                END) AS CashOutDisc,



            -- «·„»Ì⁄«  «·⁄«œÌ… („‰ «· —«‰“«ﬂ‘‰ „‘ „‰ «· ›«’Ì·)

            SUM(CASE

                    WHEN ISNULL(bt.IsWallet,0)=0

                     AND ISNULL(bt.HaveGuarantee,0)=0

                     AND ISNULL(bt.OtherItems,0)=0

                    THEN ISNULL(bt.Transaction_NetValue,0)

                    ELSE 0

                END) AS TotalRevSS,



            -- ‘Õ‰ ⁄«œÌ (€Ì— „Õ›Ÿ…)

            SUM(CASE

                    WHEN ISNULL(bt.IsWallet,0)=0

                     AND ISNULL(bt.HaveGuarantee,0)=0

                     AND ISNULL(bt.OtherItems,0)=0

                     AND ISNULL(bt.RechargeValue,0) <> 0

                     AND ISNULL(bt.InstallmentService,0)=0

                    THEN ISNULL(bt.RechargeValue,0)

                    ELSE 0

                END) AS TotalRechargeValue,



            -- ⁄„·Ì«  POS

            SUM(CASE WHEN ISNULL(bt.HaveGuarantee,0)=1 AND ISNULL(bt.RechargeValue,0)<>0 THEN ISNULL(bt.RechargeValue,0) ELSE 0 END) AS NetPOS,

            SUM(CASE WHEN ISNULL(bt.HaveGuarantee,0)=1 AND ISNULL(bt.Transaction_NetValue,0)<>0 THEN ISNULL(bt.Transaction_NetValue,0) ELSE 0 END) AS TotalRevPOS,



            -- «·ﬂ—Ê 

       --     SUM(CASE WHEN ISNULL(bt.IsWallet,0)=0 AND ISNULL(bt.HaveGuarantee,0)=0 AND ISNULL(bt.VisaNumber,'')<>'' THEN 1 ELSE 0 END) AS CountCards,



            -- ﬂ«‘ ¬Ê  ⁄œœ

            SUM(CASE WHEN ISNULL(bt.IsWallet,0)=1 AND ISNULL(bt.InstallmentService,0)=0 AND ISNULL(bt.OtherItems,0)=0 THEN 1 ELSE 0 END) AS txtCountCashOut,



            --  ﬁ”Ìÿ

            SUM(CASE WHEN ISNULL(bt.InstallmentService,0)=1 THEN ISNULL(bt.Transaction_NetValue,0) ELSE 0 END) AS TotalInstallmentRevVat,

            SUM(CASE WHEN ISNULL(bt.InstallmentService,0)=1 THEN ISNULL(bt.RechargeValue,0) + ISNULL(bt.AmountLimit,0) + ISNULL(bt.RatioAMount,0) ELSE 0 END) AS InstallmentTotal,

            SUM(CASE WHEN ISNULL(bt.InstallmentService,0)=1 AND ISNULL(bt.RechargeValue,0)<>0 THEN 1 ELSE 0 END) AS CountInstallment

        FROM BaseTrans bt

        GROUP BY bt.BranchId

    ),



    /* =============== 8) «·„— Ã⁄«  (Transaction_Type=9 + NoteID3) ================= */

    ReturnsAgg AS (

        SELECT

            tr.BranchId,

            COUNT(*) AS CountReturns,

            SUM(ISNULL(tr.Transaction_NetValue,0) + ISNULL(tr.RechargeValue,0)) AS TotalReturns

        FROM dbo.Transactions tr

        WHERE tr.Transaction_Type IN (9)

          AND tr.Transaction_Date BETWEEN @FromDate AND @ToDate

          AND tr.BranchId IN (SELECT branch_id FROM TargetBranches)

          AND ISNULL(tr.NoteID3,0) IN (

                SELECT n.NoteID

                FROM dbo.Notes n

                WHERE n.NoteDate BETWEEN @FromDate AND @ToDate

                  AND n.branch_no = tr.BranchId

          )

        GROUP BY tr.BranchId

    ),



    /* =============== 9) BoxValue „‰ TBLClosePos ================= */

    BoxAgg AS (

        SELECT

            c.BranchID AS BranchId,

            SUM(c.BoxBalance) AS BoxValue

        FROM dbo.TBLClosePos c

        WHERE c.OrderDate BETWEEN @FromDate AND @ToDate

        GROUP BY c.BranchID

    ),



    /* =============== 10) Õ«·… «·≈€·«ﬁ ================= */

    CloseStatus AS (

        SELECT

            b.branch_id AS BranchId,

            CASE WHEN EXISTS (

                    SELECT 1

                    FROM dbo.Notes n

                    WHERE n.NoteType = 29806

                      AND n.branch_no = b.branch_id

                      AND n.NoteDate BETWEEN @FromDate AND @ToDate

                )

                THEN 1 ELSE 0 END AS IsClosed,

            CASE WHEN EXISTS (

                    SELECT 1

                    FROM dbo.Notes n

                    WHERE n.NoteType = 29806

                      AND n.branch_no = b.branch_id

                      AND n.NoteDate BETWEEN @FromDate AND @ToDate

                )

                THEN N' „ «·≈€·«ﬁ' ELSE N'€Ì— „€·ﬁ' END AS CloseStatus

        FROM TargetBranches b

    )



    /* =============== 11) «·≈Œ—«Ã «·‰Â«∆Ì »‰›” √”„«¡ «·√⁄„œ… ================= */

    SELECT

        -- «·√⁄„œ… «·„Õ”Ê»… «··Ì VB ﬂ«‰ »ÌÕÿÂ« ›Ì «·‹ SELECT «·Œ«—ÃÌ

        TotalWallet       = ISNULL(TA.CashOutTotal,0) + ISNULL(TA.CashOut,0),

        TotalSupplyWallet = (ISNULL(TA.CashOutTotal,0) + ISNULL(TA.CashOut,0)) - ISNULL(TA.CashOutDisc,0),

        TotalSupply       = ISNULL(TA.TotalRechargeValue,0)

                            + (ISNULL(TA.TotalRevPOS,0) + ISNULL(TA.NetPOS,0)

                               + ISNULL(TA.TotalRevSS,0)

                               - ISNULL(DA.mTotalSalCard,0)

                               - ISNULL(DA.TotalSaleDay2Vat,0)

                               + ISNULL(TA.CashOut,0)

                               - ISNULL(TA.CashOutDisc,0)),

        Net               = ISNULL(TA.TotalRechargeValue,0)

                            + ISNULL(BA.BoxValue,0)

                            + (ISNULL(TA.TotalRevSS,0) - ISNULL(DA.mTotalSalCard,0) - ISNULL(DA.TotalSaleDay2Vat,0))

                            + ISNULL(DA.mTotalSalCard,0)

                            - ISNULL(TA.CashOutTotal,0),

        ActValue          = ISNULL(TA.TotalRechargeValue,0)

                            + ISNULL(BA.BoxValue,0)

                            + (ISNULL(TA.TotalRevSS,0) - ISNULL(DA.mTotalSalCard,0) - ISNULL(DA.TotalSaleDay2Vat,0))

                            + ISNULL(DA.mTotalSalCard,0)

                            - ISNULL(TA.CashOutTotal,0),

        TotalRev          = ISNULL(TA.TotalRevSS,0) - ISNULL(DA.mTotalSalCard,0) - ISNULL(DA.TotalSaleDay2Vat,0),



        -- ‰›” «·√⁄„œ… «··Ì ﬂ«‰ ÃÊ« «·‹ FROM (...) t

        b.branch_id,

        b.branch_name,

        b.branch_Code,

        b.branch_id AS BranchID,

        BoxBalance = 0,  -- “Ì VB

        BankBalanceCharge = ISNULL(BC.BankBalanceCharge,0),

        TotalSaleDay      = ISNULL(DA.TotalSaleDay,0),

        PosValue          = ISNULL(PV.PosValue,0),

        TotalSaleDay2     = ISNULL(DA.TotalSaleDay2,0),

        TotalReturns      = ISNULL(RA.TotalReturns,0),

        CountReturns      = ISNULL(RA.CountReturns,0),

        TotalSaleDay2Vat  = ISNULL(DA.TotalSaleDay2Vat,0),

        CountTransaction  = ISNULL(DA.CountTransaction,0),

        txtCountCashOut   = ISNULL(TA.txtCountCashOut,0),

        CountTransactionCash = ISNULL(DA.CountTransactionCash,0),

        CountCards        = ISNULL(da.CountCards,0),

        TotalRechargeValue= ISNULL(TA.TotalRechargeValue,0),

        TotalVat          = ISNULL(DA.TotalRevVat,0),  -- ›Ì VB ﬂ«‰ »Ì«ŒœÂ« „‰ transactions ·ﬂ‰ Â‰« „‰ «· ›«’Ì· Õ”» ·ÊÃÌﬂﬂ

        mTotalSalCard     = ISNULL(DA.mTotalSalCard,0),

        TotalRev2         = ISNULL(DA.TotalRev2,0),

        TotalRevVat       = ISNULL(DA.TotalRevVat,0),

        CashOut           = ISNULL(TA.CashOut,0),

        CashOutTotal      = ISNULL(TA.CashOutTotal,0),

        CashOutDisc       = ISNULL(TA.CashOutDisc,0),

        TotalRevSS        = ISNULL(TA.TotalRevSS,0),

        NetPOS            = ISNULL(TA.NetPOS,0),

        TotalRevPOS       = ISNULL(TA.TotalRevPOS,0),

        TotalInstallmentRevVat = ISNULL(TA.TotalInstallmentRevVat,0),

        -- TotalInstallmentRev „‰ «· ›«’Ì· (“Ì VB)

        TotalInstallmentRev = (

            SELECT ISNULL(SUM(ISNULL(td.Price,0)),0)

            FROM BaseTransDetails td

            WHERE td.BranchId = b.branch_id

              AND td.InstallmentService = 1

              AND td.HaveSerial = 0

        ),

        InstallmentTotal  = ISNULL(TA.InstallmentTotal,0),

        CountInstallment  = ISNULL(TA.CountInstallment,0),

        BoxValue          = ISNULL(BA.BoxValue,0),

        CS.IsClosed,

        CS.CloseStatus,

CountViolations               = ISNULL(VA.CountViolations,0),

TotalViolationsValue          = ISNULL(VA.TotalViolationsValue,0),



CountViolationsDetails        = ISNULL(VD.CountViolationsDetails,0),

TotalViolationsDetailsPrice   = ISNULL(VD.TotalViolationsDetailsPrice,0)





    FROM dbo.TblBranchesData b

    INNER JOIN TargetBranches tb ON tb.branch_id = b.branch_id

    LEFT JOIN TransAgg TA   ON TA.BranchId = b.branch_id

    LEFT JOIN DetailsAgg DA ON DA.BranchId = b.branch_id

    LEFT JOIN BankCharge BC ON BC.branch_id = b.branch_id

    LEFT JOIN PosValue  PV  ON PV.branch_id = b.branch_id

    LEFT JOIN ReturnsAgg RA ON RA.BranchId = b.branch_id

    LEFT JOIN BoxAgg    BA  ON BA.BranchId = b.branch_id

    LEFT JOIN CloseStatus CS ON CS.BranchId = b.branch_id

	    LEFT JOIN ViolationsAgg         VA ON VA.BranchId = b.branch_id

LEFT JOIN ViolationsDetailsAgg  VD ON VD.BranchId = b.branch_id





    WHERE b.branch_id IN (

        SELECT DISTINCT BranchId FROM BaseTrans

    )

    ORDER BY b.branch_id;



END





---sELECT * FROM TBLiTEMS WHERE 
