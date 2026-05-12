/*
    POS KYC/Card activation duplicate diagnostics
    Date: 2026-05-11

    This script is read-only. Run it before applying any unique index/constraint.
*/

SET NOCOUNT ON;

;WITH Kyc AS
(
    SELECT
        CustomerId = c.Id,
        CustomerName = COALESCE(NULLIF(c.name, N''), NULLIF(c.CustName, N''), NULLIF(c.ArabicName0, N'')),
        NationalId = LTRIM(RTRIM(ISNULL(c.Tet_NumPoket, N''))),
        Mobile = LTRIM(RTRIM(COALESCE(NULLIF(c.PhoneNo2, N''), NULLIF(c.PhoneNo, N''), NULLIF(c.tel, N''), N''))),
        Token = LTRIM(RTRIM(ISNULL(c.CardNo, N''))),
        TokenLength = LEN(LTRIM(RTRIM(ISNULL(c.CardNo, N'')))),
        CardType = CASE WHEN LEN(LTRIM(RTRIM(ISNULL(c.CardNo, N'')))) = 18 THEN N'18' ELSE N'Other' END,
        Branch = COALESCE(NULLIF(b.branch_name, N''), NULLIF(b.branch_namee, N''), CONVERT(NVARCHAR(50), c.BranchID)),
        ActivationDate = COALESCE(c.SaveDate, c.RecordDate, c.OrderDate),
        CreatedByUserId = c.UserID,
        c.BranchID
    FROM dbo.TblCusCsh c
    LEFT JOIN dbo.TblBranchesData b ON b.branch_id = c.BranchID
    WHERE ISNULL(c.EasyCashType, 0) = 0
      AND NULLIF(LTRIM(RTRIM(ISNULL(c.CardNo, N''))), N'') IS NOT NULL
)
SELECT
    Diagnostic = N'1) Duplicate token/card numbers',
    k.CustomerId,
    k.CustomerName,
    k.NationalId,
    k.Mobile,
    k.Token,
    k.TokenLength,
    k.CardType,
    k.Branch,
    k.ActivationDate,
    Transaction_ID = t.Transaction_ID,
    CreatedByUserId = k.CreatedByUserId
FROM Kyc k
LEFT JOIN dbo.Transactions t
    ON t.Transaction_Type = 21
   AND ISNULL(t.IsPOS, 0) = 1
   AND LTRIM(RTRIM(ISNULL(t.VisaNumber, N''))) = k.Token
WHERE k.Token IN
(
    SELECT Token
    FROM Kyc
    GROUP BY Token
    HAVING COUNT(*) > 1
)
ORDER BY k.Token, k.CustomerId;

;WITH Kyc AS
(
    SELECT
        CustomerId = c.Id,
        CustomerName = COALESCE(NULLIF(c.name, N''), NULLIF(c.CustName, N''), NULLIF(c.ArabicName0, N'')),
        NationalId = LTRIM(RTRIM(ISNULL(c.Tet_NumPoket, N''))),
        Mobile = LTRIM(RTRIM(COALESCE(NULLIF(c.PhoneNo2, N''), NULLIF(c.PhoneNo, N''), NULLIF(c.tel, N''), N''))),
        Token = LTRIM(RTRIM(ISNULL(c.CardNo, N''))),
        TokenLength = LEN(LTRIM(RTRIM(ISNULL(c.CardNo, N'')))),
        CardType = CASE WHEN LEN(LTRIM(RTRIM(ISNULL(c.CardNo, N'')))) = 18 THEN N'18' ELSE N'Other' END,
        Branch = COALESCE(NULLIF(b.branch_name, N''), NULLIF(b.branch_namee, N''), CONVERT(NVARCHAR(50), c.BranchID)),
        ActivationDate = COALESCE(c.SaveDate, c.RecordDate, c.OrderDate),
        CreatedByUserId = c.UserID
    FROM dbo.TblCusCsh c
    LEFT JOIN dbo.TblBranchesData b ON b.branch_id = c.BranchID
    WHERE ISNULL(c.EasyCashType, 0) = 0
      AND NULLIF(LTRIM(RTRIM(ISNULL(c.CardNo, N''))), N'') IS NOT NULL
)
SELECT
    Diagnostic = N'2) Same customer with more than one 18-length token',
    k.CustomerId,
    k.CustomerName,
    k.NationalId,
    k.Mobile,
    k.Token,
    k.TokenLength,
    k.CardType,
    k.Branch,
    k.ActivationDate,
    Transaction_ID = t.Transaction_ID,
    CreatedByUserId = k.CreatedByUserId
FROM Kyc k
LEFT JOIN dbo.Transactions t
    ON t.Transaction_Type = 21
   AND ISNULL(t.IsPOS, 0) = 1
   AND LTRIM(RTRIM(ISNULL(t.VisaNumber, N''))) = k.Token
WHERE k.CardType = N'18'
  AND k.NationalId IN
  (
      SELECT NationalId
      FROM Kyc
      WHERE CardType = N'18'
        AND NULLIF(NationalId, N'') IS NOT NULL
      GROUP BY NationalId
      HAVING COUNT(*) > 1
  )
ORDER BY k.NationalId, k.ActivationDate, k.CustomerId;

;WITH Kyc AS
(
    SELECT
        CustomerId = c.Id,
        CustomerName = COALESCE(NULLIF(c.name, N''), NULLIF(c.CustName, N''), NULLIF(c.ArabicName0, N'')),
        NationalId = LTRIM(RTRIM(ISNULL(c.Tet_NumPoket, N''))),
        Mobile = LTRIM(RTRIM(COALESCE(NULLIF(c.PhoneNo2, N''), NULLIF(c.PhoneNo, N''), NULLIF(c.tel, N''), N''))),
        Token = LTRIM(RTRIM(ISNULL(c.CardNo, N''))),
        TokenLength = LEN(LTRIM(RTRIM(ISNULL(c.CardNo, N'')))),
        CardType = CASE WHEN LEN(LTRIM(RTRIM(ISNULL(c.CardNo, N'')))) = 18 THEN N'18' ELSE N'Other' END,
        Branch = COALESCE(NULLIF(b.branch_name, N''), NULLIF(b.branch_namee, N''), CONVERT(NVARCHAR(50), c.BranchID)),
        ActivationDate = COALESCE(c.SaveDate, c.RecordDate, c.OrderDate),
        CreatedByUserId = c.UserID
    FROM dbo.TblCusCsh c
    LEFT JOIN dbo.TblBranchesData b ON b.branch_id = c.BranchID
    WHERE ISNULL(c.EasyCashType, 0) = 0
      AND NULLIF(LTRIM(RTRIM(ISNULL(c.CardNo, N''))), N'') IS NOT NULL
)
SELECT
    Diagnostic = N'3) Same customer with more than one non-18 token',
    k.CustomerId,
    k.CustomerName,
    k.NationalId,
    k.Mobile,
    k.Token,
    k.TokenLength,
    k.CardType,
    k.Branch,
    k.ActivationDate,
    Transaction_ID = t.Transaction_ID,
    CreatedByUserId = k.CreatedByUserId
FROM Kyc k
LEFT JOIN dbo.Transactions t
    ON t.Transaction_Type = 21
   AND ISNULL(t.IsPOS, 0) = 1
   AND LTRIM(RTRIM(ISNULL(t.VisaNumber, N''))) = k.Token
WHERE k.CardType = N'Other'
  AND k.NationalId IN
  (
      SELECT NationalId
      FROM Kyc
      WHERE CardType = N'Other'
        AND NULLIF(NationalId, N'') IS NOT NULL
      GROUP BY NationalId
      HAVING COUNT(*) > 1
  )
ORDER BY k.NationalId, k.ActivationDate, k.CustomerId;

;WITH Kyc AS
(
    SELECT
        CustomerId = c.Id,
        CustomerName = COALESCE(NULLIF(c.name, N''), NULLIF(c.CustName, N''), NULLIF(c.ArabicName0, N'')),
        NationalId = LTRIM(RTRIM(ISNULL(c.Tet_NumPoket, N''))),
        Mobile = LTRIM(RTRIM(COALESCE(NULLIF(c.PhoneNo2, N''), NULLIF(c.PhoneNo, N''), NULLIF(c.tel, N''), N''))),
        Token = LTRIM(RTRIM(ISNULL(c.CardNo, N''))),
        TokenLength = LEN(LTRIM(RTRIM(ISNULL(c.CardNo, N'')))),
        Branch = COALESCE(NULLIF(b.branch_name, N''), NULLIF(b.branch_namee, N''), CONVERT(NVARCHAR(50), c.BranchID)),
        ActivationDate = COALESCE(c.SaveDate, c.RecordDate, c.OrderDate),
        CreatedByUserId = c.UserID
    FROM dbo.TblCusCsh c
    LEFT JOIN dbo.TblBranchesData b ON b.branch_id = c.BranchID
    WHERE ISNULL(c.EasyCashType, 0) = 0
      AND NULLIF(LTRIM(RTRIM(ISNULL(c.CardNo, N''))), N'') IS NOT NULL
)
SELECT
    Diagnostic = N'4) Tokens activated but still appearing as available in stock',
    k.CustomerId,
    k.CustomerName,
    k.NationalId,
    k.Mobile,
    k.Token,
    k.TokenLength,
    CardType = CASE WHEN k.TokenLength = 18 THEN N'18' ELSE N'Other' END,
    k.Branch,
    k.ActivationDate,
    Transaction_ID = sale.Transaction_ID,
    CreatedByUserId = k.CreatedByUserId
FROM Kyc k
OUTER APPLY
(
    SELECT TOP (1) t.Transaction_ID
    FROM dbo.Transactions t
    WHERE t.Transaction_Type = 21
      AND ISNULL(t.IsPOS, 0) = 1
      AND LTRIM(RTRIM(ISNULL(t.VisaNumber, N''))) = k.Token
    ORDER BY t.Transaction_ID DESC
) sale
WHERE EXISTS
(
    SELECT 1
    FROM dbo.Transaction_Details td
    INNER JOIN dbo.Transactions t ON t.Transaction_ID = td.Transaction_ID
    WHERE t.Transaction_Type = 20
      AND LTRIM(RTRIM(ISNULL(td.ItemSerial, N''))) = k.Token
)
AND NOT EXISTS
(
    SELECT 1
    FROM dbo.Transaction_Details td
    INNER JOIN dbo.Transactions t ON t.Transaction_ID = td.Transaction_ID
    WHERE t.Transaction_Type = 19
      AND LTRIM(RTRIM(ISNULL(td.ItemSerial, N''))) = k.Token
)
ORDER BY k.ActivationDate, k.CustomerId;

SELECT
    Diagnostic = N'5) Tokens issued in Transaction_Details.ItemSerial more than once',
    CustomerId = CAST(NULL AS INT),
    CustomerName = CAST(NULL AS NVARCHAR(255)),
    NationalId = CAST(NULL AS NVARCHAR(255)),
    Mobile = CAST(NULL AS NVARCHAR(50)),
    Token = LTRIM(RTRIM(ISNULL(td.ItemSerial, N''))),
    TokenLength = LEN(LTRIM(RTRIM(ISNULL(td.ItemSerial, N'')))),
    CardType = CASE WHEN LEN(LTRIM(RTRIM(ISNULL(td.ItemSerial, N'')))) = 18 THEN N'18' ELSE N'Other' END,
    Branch = COALESCE(NULLIF(b.branch_name, N''), NULLIF(b.branch_namee, N''), CONVERT(NVARCHAR(50), t.BranchId)),
    ActivationDate = t.Transaction_Date,
    Transaction_ID = t.Transaction_ID,
    CreatedByUserId = t.UserID
FROM dbo.Transaction_Details td
INNER JOIN dbo.Transactions t ON t.Transaction_ID = td.Transaction_ID
LEFT JOIN dbo.TblBranchesData b ON b.branch_id = t.BranchId
WHERE t.Transaction_Type = 19
  AND NULLIF(LTRIM(RTRIM(ISNULL(td.ItemSerial, N''))), N'') IS NOT NULL
  AND LTRIM(RTRIM(ISNULL(td.ItemSerial, N''))) IN
  (
      SELECT LTRIM(RTRIM(ISNULL(td2.ItemSerial, N'')))
      FROM dbo.Transaction_Details td2
      INNER JOIN dbo.Transactions t2 ON t2.Transaction_ID = td2.Transaction_ID
      WHERE t2.Transaction_Type = 19
        AND NULLIF(LTRIM(RTRIM(ISNULL(td2.ItemSerial, N''))), N'') IS NOT NULL
      GROUP BY LTRIM(RTRIM(ISNULL(td2.ItemSerial, N'')))
      HAVING COUNT(*) > 1
  )
ORDER BY Token, t.Transaction_ID;

;WITH Kyc AS
(
    SELECT
        CustomerId = c.Id,
        CustomerName = COALESCE(NULLIF(c.name, N''), NULLIF(c.CustName, N''), NULLIF(c.ArabicName0, N'')),
        NationalId = LTRIM(RTRIM(ISNULL(c.Tet_NumPoket, N''))),
        Mobile = LTRIM(RTRIM(COALESCE(NULLIF(c.PhoneNo2, N''), NULLIF(c.PhoneNo, N''), NULLIF(c.tel, N''), N''))),
        Token = LTRIM(RTRIM(ISNULL(c.CardNo, N''))),
        TokenLength = LEN(LTRIM(RTRIM(ISNULL(c.CardNo, N'')))),
        Branch = COALESCE(NULLIF(b.branch_name, N''), NULLIF(b.branch_namee, N''), CONVERT(NVARCHAR(50), c.BranchID)),
        ActivationDate = COALESCE(c.SaveDate, c.RecordDate, c.OrderDate),
        CreatedByUserId = c.UserID
    FROM dbo.TblCusCsh c
    LEFT JOIN dbo.TblBranchesData b ON b.branch_id = c.BranchID
    WHERE ISNULL(c.EasyCashType, 0) = 0
      AND NULLIF(LTRIM(RTRIM(ISNULL(c.CardNo, N''))), N'') IS NOT NULL
)
SELECT
    Diagnostic = N'6) KYC records without matching stock issue movement',
    k.CustomerId,
    k.CustomerName,
    k.NationalId,
    k.Mobile,
    k.Token,
    k.TokenLength,
    CardType = CASE WHEN k.TokenLength = 18 THEN N'18' ELSE N'Other' END,
    k.Branch,
    k.ActivationDate,
    Transaction_ID = sale.Transaction_ID,
    CreatedByUserId = k.CreatedByUserId
FROM Kyc k
OUTER APPLY
(
    SELECT TOP (1) t.Transaction_ID
    FROM dbo.Transactions t
    WHERE t.Transaction_Type = 21
      AND ISNULL(t.IsPOS, 0) = 1
      AND LTRIM(RTRIM(ISNULL(t.VisaNumber, N''))) = k.Token
    ORDER BY t.Transaction_ID DESC
) sale
WHERE NOT EXISTS
(
    SELECT 1
    FROM dbo.Transaction_Details td
    INNER JOIN dbo.Transactions t ON t.Transaction_ID = td.Transaction_ID
    WHERE t.Transaction_Type = 19
      AND LTRIM(RTRIM(ISNULL(td.ItemSerial, N''))) = k.Token
)
ORDER BY k.ActivationDate, k.CustomerId;
