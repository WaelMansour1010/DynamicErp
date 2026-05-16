/*
Legacy HR/Finance Dania payroll parity check
Date: 2026-05-14

Purpose:
Highlights the current payroll parity gap before enabling production posting.
It compares real Dania historical salary rows in emp_salary with the current
web preview basis that derives salary primarily from TblEmployee master salary.

Important:
- This is a QA comparison script, not a posting script.
- It does not modify data.
- SQL Server 2012 compatible.
- True sign-off still requires running the original VB6 salary workflow for
  the same period and comparing notes/DOUBLE_ENTREY_VOUCHERS.
*/

DECLARE @Sgn varchar(20) = '20264';
DECLARE @Tolerance money = 0.05;

IF OBJECT_ID('tempdb..#LegacyActual') IS NOT NULL DROP TABLE #LegacyActual;
IF OBJECT_ID('tempdb..#WebCurrentBasis') IS NOT NULL DROP TABLE #WebCurrentBasis;

SELECT
    s.emp_id,
    MAX(s.Emp_Code) AS Emp_Code,
    MAX(s.Emp_Name) AS Emp_Name,
    SUM(ISNULL(s.Emp_Salary, 0)) AS LegacyBasicSalary,
    SUM(ISNULL(s.total1, 0)) AS LegacyTotalBeforeDeductions,
    SUM(ISNULL(s.TotalAdvance, 0)) AS LegacyAdvance,
    SUM(ISNULL(s.TotalDiscount, 0)) AS LegacyDiscount,
    SUM(ISNULL(s.total2, 0)) AS LegacyTotalDeductions,
    SUM(ISNULL(s.EmpTotalNet, 0)) AS LegacyNetSalary,
    COUNT(1) AS LegacyRows
INTO #LegacyActual
FROM dbo.emp_salary s WITH (NOLOCK)
WHERE s.sgn = @Sgn
GROUP BY s.emp_id;

SELECT
    e.Emp_ID AS emp_id,
    e.Emp_Code,
    e.Emp_Name,
    ISNULL(e.Emp_Salary, 0) AS WebBasicSalary,
    ISNULL(e.Emp_Salary, 0) AS WebTotalBeforeDeductions,
    ISNULL(a.LegacyAdvance, 0) AS WebAdvance,
    ISNULL(a.LegacyDiscount, 0) AS WebDiscount,
    ISNULL(a.LegacyTotalDeductions, 0) AS WebTotalDeductions,
    ISNULL(e.Emp_Salary, 0) - ISNULL(a.LegacyTotalDeductions, 0) AS WebNetSalary
INTO #WebCurrentBasis
FROM dbo.TblEmployee e WITH (NOLOCK)
INNER JOIN #LegacyActual a ON a.emp_id = e.Emp_ID;

SELECT TOP (100)
    a.emp_id,
    a.Emp_Code,
    a.Emp_Name,
    a.LegacyBasicSalary,
    w.WebBasicSalary,
    a.LegacyTotalBeforeDeductions,
    w.WebTotalBeforeDeductions,
    a.LegacyAdvance,
    w.WebAdvance,
    a.LegacyDiscount,
    w.WebDiscount,
    a.LegacyTotalDeductions,
    w.WebTotalDeductions,
    a.LegacyNetSalary,
    w.WebNetSalary,
    a.LegacyNetSalary - w.WebNetSalary AS NetDifference,
    CASE
        WHEN ABS(a.LegacyNetSalary - w.WebNetSalary) <= @Tolerance THEN 'MATCH'
        ELSE 'MISMATCH'
    END AS ParityStatus
FROM #LegacyActual a
INNER JOIN #WebCurrentBasis w ON w.emp_id = a.emp_id
WHERE ABS(a.LegacyNetSalary - w.WebNetSalary) > @Tolerance
ORDER BY ABS(a.LegacyNetSalary - w.WebNetSalary) DESC, a.emp_id;

SELECT
    @Sgn AS SalaryPeriod,
    COUNT(1) AS EmployeesCompared,
    SUM(CASE WHEN ABS(a.LegacyNetSalary - w.WebNetSalary) <= @Tolerance THEN 1 ELSE 0 END) AS MatchedEmployees,
    SUM(CASE WHEN ABS(a.LegacyNetSalary - w.WebNetSalary) > @Tolerance THEN 1 ELSE 0 END) AS MismatchedEmployees,
    SUM(a.LegacyNetSalary) AS LegacyNetTotal,
    SUM(w.WebNetSalary) AS WebCurrentBasisNetTotal,
    SUM(a.LegacyNetSalary - w.WebNetSalary) AS TotalNetDifference
FROM #LegacyActual a
INNER JOIN #WebCurrentBasis w ON w.emp_id = a.emp_id;

SELECT
    'PostingParityDependency' AS CheckName,
    (SELECT COUNT(1) FROM dbo.notes WITH (NOLOCK)) AS NotesRows,
    (SELECT COUNT(1) FROM dbo.DOUBLE_ENTREY_VOUCHERS WITH (NOLOCK)) AS DoubleEntryVoucherRows,
    (SELECT COUNT(1) FROM dbo.emp_salary WITH (NOLOCK) WHERE sgn = @Sgn) AS EmpSalaryRowsForPeriod;
