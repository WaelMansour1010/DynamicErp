

CREATE PROCEDURE dbo.RPT_CloseReportTotalMini

    @FromDate date,

    @ToDate   date

AS

BEGIN

    SET NOCOUNT ON;



    /* 1) «·„·«ÕŸ«  „‰ ‰Ê⁄ 220 ›Ì «·› —… */

    WITH N AS (

        SELECT NoteID, NoteDate

        FROM dbo.Notes

        WHERE NoteType = 220

          AND NoteDate BETWEEN @FromDate AND @ToDate

    ),

    /* 2)  ·ŒÌ’ «· —«‰“«ﬂ‘‰ */

    TransactionsSummary AS (

        SELECT

            t.Transaction_Date,

            SUM(CASE WHEN ISNULL(t.IsWallet,0) = 1 THEN ISNULL(t.RechargeValue,0) ELSE 0 END) AS CashOutTotal,

            SUM(CASE WHEN ISNULL(t.IsWallet,0) = 1 THEN ISNULL(t.Transaction_NetValue,0) ELSE 0 END) AS CashOut,

            SUM(

                CASE

                    WHEN ISNULL(t.IsWallet,0) = 1 AND t.Transaction_Date > '2025-06-30'

                        THEN ISNULL(t.cost,0) - ISNULL(t.CashBack,0)

                    WHEN ISNULL(t.IsWallet,0) = 1 AND t.Transaction_Date > '2024-08-25'

                        THEN (ISNULL(t.RechargeValue,0) + ISNULL(t.Transaction_NetValue,0)) * 0.008

                    WHEN ISNULL(t.IsWallet,0) = 1

                        THEN (ISNULL(t.RechargeValue,0) + ISNULL(t.Transaction_NetValue,0)) * 0.01

                    ELSE 0

                END

            ) AS CashOutDisc,

            SUM(CASE WHEN ISNULL(t.IsWallet,0) = 0 THEN ISNULL(t.Transaction_NetValue,0) ELSE 0 END) AS TotalRevSS,

            SUM(CASE WHEN ISNULL(t.IsWallet,0) = 0 THEN ISNULL(t.RechargeValue,0)     ELSE 0 END) AS TotalRechargeValue,

            SUM(CASE WHEN ISNULL(t.HaveGuarantee,0) = 1 THEN ISNULL(t.RechargeValue,0) ELSE 0 END) AS NetPOS,

            SUM(CASE WHEN ISNULL(t.HaveGuarantee,0) = 1 AND ISNULL(t.RechargeValue,0) <> 0

                     THEN ISNULL(t.Transaction_NetValue,0) ELSE 0 END) AS TotalRevPOS,

            COUNT(CASE WHEN ISNULL(t.IsWallet,0) = 0 AND t.VisaNumber IS NOT NULL THEN 1 END) AS CountCards,

            COUNT(CASE WHEN ISNULL(t.IsWallet,0) = 1 THEN 1 END) AS CountTransactionCash,

            COUNT(*) AS CountTransaction,

            (

                SELECT SUM(d.Value)

                FROM dbo.DOUBLE_ENTREY_VOUCHERS AS d

                WHERE d.Credit_Or_Debit = 0

                  AND d.RecordDate = t.Transaction_Date

                  AND d.Account_Code = 'a3a1a1a1a6'

                  AND d.Posted IS NULL

            ) AS BankBalanceCharge

        FROM dbo.Transactions AS t

        WHERE t.Transaction_Type IN (21,9)

          AND t.Transaction_Date BETWEEN @FromDate AND @ToDate

        GROUP BY t.Transaction_Date

    ),

    /* 3)  ·ŒÌ’ «· ›«’Ì· */

    TransactionDetailsSummary AS (

        SELECT

            T.Transaction_Date,

            SUM(CASE

                    WHEN I.ItemType = 0 AND ISNULL(T.IsWallet,0) = 0 AND ISNULL(I.HaveSerial,0) = 1

                    THEN ISNULL(D.Price,0) ELSE 0 END) AS TotalSaleDay,

            SUM(CASE

                    WHEN I.ItemType = 0 AND ISNULL(T.IsWallet,0) = 0 AND ISNULL(I.HaveSerial,0) = 1

                    THEN ISNULL(D.Price,0) ELSE 0 END) AS TotalSaleDay2,

            SUM(CASE

                    WHEN I.ItemType = 0 AND ISNULL(T.IsWallet,0) = 0 AND ISNULL(I.HaveSerial,0) = 1

                    THEN ISNULL(D.Vat,0) ELSE 0 END) AS TotalSaleDay2Vat,

            SUM(CASE

                    WHEN ISNULL(T.IsWallet,0) = 0 AND ISNULL(I.HaveSerial,0) = 0

                    THEN ISNULL(D.Vat,0) ELSE 0 END) AS TotalRevVat,

            SUM(CASE

                    WHEN ISNULL(T.IsWallet,0) = 0 AND ISNULL(I.HaveSerial,0) = 0

                    THEN ISNULL(D.Price,0) ELSE 0 END) AS TotalRev2,

            SUM(CASE

                    WHEN I.ItemType = 0 AND ISNULL(I.HaveSerial,0) = 1

                    THEN ISNULL(D.Price,0) ELSE 0 END) AS mTotalSalCard,

            SUM(ISNULL(D.Price,0)) AS TotalSale

        FROM dbo.Transaction_Details AS D

        INNER JOIN dbo.Transactions AS T

            ON D.Transaction_ID = T.Transaction_ID

       INNER JOIN dbo.TblItems AS I

            ON D.Item_ID = I.ItemID

        WHERE T.Transaction_Type IN (21,9)

          AND T.Transaction_Date BETWEEN @FromDate AND @ToDate

        GROUP BY T.Transaction_Date

    ),

    /* 4) «·„— Ã⁄«  „‰ Notes 220 */

    ReturnsSummary AS (

        SELECT

            N.NoteDate AS ReportDate,

            SUM(ISNULL(T.Transaction_NetValue,0)) - SUM(ISNULL(T.VAT,0)) AS TotalSaleDayReturn,

            SUM(ISNULL(T.VAT,0)) AS TotalSaleDay2VatReturn,

            COUNT(T.Transaction_ID) AS ReturnCount

        FROM N

        INNER JOIN dbo.Transactions AS T

            ON N.NoteID = T.NoteID2

        WHERE T.Transaction_Type = 9

        GROUP BY N.NoteDate

    )

    SELECT

        T.Transaction_Date,

        TotalWallet       = ISNULL(T.CashOutTotal,0) + ISNULL(T.CashOut,0),

        TotalSupplyWallet = (ISNULL(T.CashOutTotal,0) + ISNULL(T.CashOut,0)) - ISNULL(T.CashOutDisc,0),

        TotalSupply       = ISNULL(T.TotalRechargeValue,0)

                            + ISNULL(D.TotalSaleDay2,0)

                            + ISNULL(T.CashOut,0)

                            - ISNULL(T.CashOutDisc,0)

                            - ISNULL(T.NetPOS,0)

                            - ISNULL(T.TotalRevPOS,0),

        Net               = ISNULL(T.TotalRechargeValue,0) + ISNULL(D.mTotalSalCard,0) - ISNULL(T.CashOutTotal,0),

        ActValue          = ISNULL(T.TotalRechargeValue,0) + ISNULL(D.mTotalSalCard,0) - ISNULL(T.CashOutTotal,0),

        TotalRev          = ISNULL(T.TotalRevSS,0) - ISNULL(D.mTotalSalCard,0),

        TotalSaleDay      = ISNULL(D.TotalSaleDay,0),

        TotalSaleDay2     = ISNULL(D.TotalSaleDay2,0),

        TotalSaleDayReturn    = ISNULL(R.TotalSaleDayReturn,0),

        mTotalSalCard         = ISNULL(D.mTotalSalCard,0),

        TotalSaleDay2Vat      = ISNULL(D.TotalSaleDay2Vat,0),

        TotalSaleDay2VatReturn= ISNULL(R.TotalSaleDay2VatReturn,0),

        TotalRev2         = ISNULL(D.TotalRev2,0),

        TotalRevVat       = ISNULL(D.TotalRevVat,0),

        T.BankBalanceCharge,

        T.CountTransaction,

        T.CountTransactionCash,

        T.CountCards,

        T.TotalRechargeValue,

        T.CashOut,

        T.CashOutTotal,

        T.CashOutDisc,

        T.TotalRevSS,

        T.NetPOS,

        T.TotalRevPOS,

        ReturnCount       = ISNULL(R.ReturnCount,0)

    FROM TransactionsSummary T

    LEFT JOIN TransactionDetailsSummary D

        ON T.Transaction_Date = D.Transaction_Date

    LEFT JOIN ReturnsSummary R

        ON T.Transaction_Date = R.ReportDate

    ORDER BY T.Transaction_Date;

END

