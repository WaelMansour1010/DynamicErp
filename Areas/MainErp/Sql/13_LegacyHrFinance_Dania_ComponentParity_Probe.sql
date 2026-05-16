/*
    Legacy HR/Finance payroll component parity probe
    Database: Dania
    Source:
      - Kishny VB6: F:\Source Code\SatriahMain\Cayshny\Frm\New frm\FrmEmpSalary5.frm
      - Kishny VB6 helper: F:\Source Code\SatriahMain\Cayshny\Bas\salarY_component.bas

    Purpose:
      Read-only component-level trace for one employee and salary period.
      This script does not create, update, delete, post, or rebuild anything.

    SQL Server 2012 compatible.
*/

SET NOCOUNT ON;

DECLARE @Sgn varchar(20);
DECLARE @EmpId int;
DECLARE @Year int;
DECLARE @Month int;
DECLARE @PeriodStart date;
DECLARE @PeriodEnd date;

SET @Sgn = '20264';
SET @EmpId = NULL; -- Set explicitly to trace a known employee. NULL picks the highest-net legacy row.

SET @Year = CONVERT(int, LEFT(@Sgn, 4));
SET @Month = CONVERT(int, SUBSTRING(@Sgn, 5, LEN(@Sgn) - 4));
SET @PeriodStart = DATEADD(month, (@Year - 1900) * 12 + @Month - 1, 0);
SET @PeriodEnd = DATEADD(day, -1, DATEADD(month, 1, @PeriodStart));

IF @EmpId IS NULL
BEGIN
    SELECT TOP (1) @EmpId = emp_id
    FROM dbo.emp_salary
    WHERE sgn = @Sgn
    ORDER BY ABS(ISNULL(EmpTotalNet, 0)) DESC, emp_id;
END;

SELECT
    'ProbeContext' AS Section,
    @Sgn AS Sgn,
    @Year AS PayrollYear,
    @Month AS PayrollMonth,
    @PeriodStart AS PeriodStart,
    @PeriodEnd AS PeriodEnd,
    @EmpId AS EmpId;

SELECT
    'LegacySnapshot_emp_salary' AS Section,
    es.*
FROM dbo.emp_salary es
WHERE es.sgn = @Sgn
  AND es.emp_id = @EmpId;

;WITH LegacyComponents AS
(
    SELECT
        es.sgn,
        es.emp_id,
        es.Emp_Code,
        es.Emp_Name,
        v.ComponentNo,
        v.ComponentColumn,
        CONVERT(decimal(18, 4), ISNULL(v.LegacyValue, 0)) AS LegacyValue
    FROM dbo.emp_salary es
    CROSS APPLY
    (
        VALUES
        (1,'Comp1',es.Comp1),(2,'Comp2',es.Comp2),(3,'Comp3',es.Comp3),(4,'Comp4',es.Comp4),(5,'Comp5',es.Comp5),
        (6,'Comp6',es.Comp6),(7,'Comp7',es.Comp7),(8,'Comp8',es.Comp8),(9,'Comp9',es.Comp9),(10,'Comp10',es.Comp10),
        (11,'Comp11',es.Comp11),(12,'Comp12',es.Comp12),(13,'Comp13',es.Comp13),(14,'Comp14',es.Comp14),(15,'Comp15',es.Comp15),
        (16,'Comp16',es.Comp16),(17,'Comp17',es.Comp17),(18,'Comp18',es.Comp18),(19,'Comp19',es.Comp19),(20,'Comp20',es.Comp20),
        (21,'Comp21',es.Comp21),(22,'Comp22',es.Comp22),(23,'Comp23',es.Comp23),(24,'Comp24',es.Comp24),(25,'Comp25',es.Comp25),
        (26,'Comp26',es.Comp26),(27,'Comp27',es.Comp27),(28,'Comp28',es.Comp28),(29,'Comp29',es.Comp29),(30,'Comp30',es.Comp30),
        (31,'Comp31',es.Comp31),(32,'Comp32',es.Comp32),(33,'Comp33',es.Comp33),(34,'Comp34',es.Comp34),(35,'Comp35',es.Comp35),
        (36,'Comp36',es.Comp36),(37,'Comp37',es.Comp37),(38,'Comp38',es.Comp38),(39,'Comp39',es.Comp39),(40,'Comp40',es.Comp40)
    ) v(ComponentNo, ComponentColumn, LegacyValue)
    WHERE es.sgn = @Sgn
      AND es.emp_id = @EmpId
)
SELECT
    'LegacySnapshot_Comp1_Comp40' AS Section,
    lc.ComponentNo,
    lc.ComponentColumn,
    m.name AS MofradNameAr,
    m.nameE AS MofradNameEn,
    m.AddOrDiscount,
    m.FixedOrChanged,
    m.ViewComp,
    m.ZmamAccount,
    m.AdvPaymentdAccount,
    m.Account_Code,
    m.Account_code1,
    m.Insurances,
    m.Salary,
    m.showMofradAll,
    m.culc30orRminder,
    lc.LegacyValue
FROM LegacyComponents lc
LEFT JOIN dbo.mofrad m ON m.id = lc.ComponentNo
ORDER BY lc.ComponentNo;

SELECT
    'LegacySnapshot_Totals' AS Section,
    es.emp_id,
    es.sgn,
    es.CountDays,
    es.AbcentDay,
    es.RemainDay,
    es.TotalAdvance,
    es.ToalInsurance,
    es.VoCation,
    es.VoCation2,
    es.VoCation3,
    es.VoCation4,
    es.Mokafea,
    es.TotalDiscount,
    es.total1,
    es.total2,
    es.EmpTotalNet,
    es.WorkHours,
    es.OverTime,
    es.OverTimePrice,
    es.Payed,
    es.VocEntitID,
    es.RecordDate,
    es.BranchId,
    es.DepartmentID,
    es.project_id,
    es.cost_center_id
FROM dbo.emp_salary es
WHERE es.sgn = @Sgn
  AND es.emp_id = @EmpId;

SELECT
    'Source_EmpSalaryComponent_ByMofrad' AS Section,
    esc.emp_ID,
    esc.AccountCode,
    esc.AccountName,
    esc.mofrad_type,
    m.name AS MofradNameAr,
    m.nameE AS MofradNameEn,
    esc.Monthly,
    esc.Value,
    esc.specific_value,
    esc.EntIncresDataM,
    m.AddOrDiscount,
    m.FixedOrChanged,
    m.ViewComp,
    m.Insurances,
    m.Account_Code,
    m.Account_code1
FROM dbo.EmpSalaryComponent esc
LEFT JOIN dbo.mofrad m ON m.id = esc.mofrad_type
WHERE esc.emp_ID = @EmpId
ORDER BY esc.mofrad_type, esc.AccountCode;

SELECT
    'Source_ChangedComponents_ForPeriod' AS Section,
    r.ChangedComponentid,
    r.RecordDate,
    r.[year],
    r.[month],
    r.Actualyear,
    r.Actualmonth,
    r.ComponentID,
    m.name AS MofradNameAr,
    m.nameE AS MofradNameEn,
    d.id AS DetailId,
    d.Emp_id,
    d.value,
    d.Salary,
    d.HourRate,
    d.NoOfHour,
    d.NoOfMinutes,
    d.NoofDays,
    d.projectid,
    d.KsmID,
    d.Remarks,
    r.BranchId,
    r.LocationID,
    r.Reason,
    r.Flag,
    r.Finger
FROM dbo.TblChangedComponentRegisterDetails d
INNER JOIN dbo.TblChangedComponentRegister r ON r.ChangedComponentid = d.ChangedComponentid
LEFT JOIN dbo.mofrad m ON m.id = r.ComponentID
WHERE d.Emp_id = @EmpId
  AND
  (
      (r.[year] = @Year AND r.[month] = @Month)
      OR (r.Actualyear = @Year AND r.Actualmonth = @Month)
      OR (r.RecordDate >= @PeriodStart AND r.RecordDate < DATEADD(day, 1, @PeriodEnd))
  )
ORDER BY r.RecordDate, r.ChangedComponentid, d.id;

SELECT
    'Source_ComponentYearOverrides' AS Section,
    cyd.*
FROM dbo.TblComponentYearDet cyd
WHERE cyd.EmpID = @EmpId
  AND
  (
      (MONTH(cyd.RecDate1) = @Month AND YEAR(cyd.RecDate1) = @Year)
      OR (MONTH(cyd.RecDate2) = @Month AND YEAR(cyd.RecDate2) = @Year)
  )
ORDER BY cyd.RecDate1, cyd.RecDate2;

SELECT
    'Source_Advances_QryAllEmpAdvance' AS Section,
    a.*
FROM dbo.QryAllEmpAdvance(@Month, @Year) a
WHERE a.Emp_ID = @EmpId
ORDER BY a.PresentDate;

SELECT
    'Source_Attendance_tblPresentTime' AS Section,
    pt.Emp_ID,
    COUNT(*) AS [RowCount],
    SUM(ISNULL(pt.WorkHoursCount, 0)) AS WorkHoursCountMinutes,
    SUM(ISNULL(pt.CurrentWorkMints, 0)) AS CurrentWorkMinutes,
    SUM(ISNULL(pt.WorkHoursCount, 0) - ISNULL(pt.CurrentWorkMints, 0)) AS OvertimeMinutes,
    SUM(ISNULL(pt.LateTimeDiscountValue, 0)) AS LateTimeDiscountValue
FROM dbo.tblPresentTime pt
WHERE pt.Emp_ID = @EmpId
  AND MONTH(pt.PresentDate) = @Month
  AND YEAR(pt.PresentDate) = @Year
GROUP BY pt.Emp_ID;

SELECT
    'Source_RuntimeScalarFunctions' AS Section,
    @EmpId AS Emp_ID,
    dbo.EmpInsurances(@Month - 1, @Year, @EmpId) AS EmpInsurances_PreviousIndex,
    dbo.EmpVoCation(@Month - 1, @Year, @EmpId) AS EmpVoCation_PreviousIndex,
    dbo.EmpVoCation3(@Month, @Year, @EmpId) AS EmpVoCation3_CurrentMonth,
    dbo.EmpPrePaymentID(@EmpId) AS EmpPrePaymentID,
    dbo.EmpPrePaymentValue(dbo.EmpPrePaymentID(@EmpId)) AS EmpPrePaymentValue,
    dbo.GetAbcentDay(@EmpId, @Year, @Month) AS GetAbcentDay;

SELECT
    'Source_VacationSalary' AS Section,
    vs.*
FROM dbo.TblVacationSalary vs
WHERE vs.Emp_ID = @EmpId
  AND vs.RecordDate >= @PeriodStart
  AND vs.RecordDate < DATEADD(day, 1, @PeriodEnd)
ORDER BY vs.RecordDate;

SELECT
    'Source_EmbarkationVacation' AS Section,
    e.*
FROM dbo.TblEmbarkation e
WHERE e.Emp_ID = @EmpId
  AND
  (
      (e.stratDate <= @PeriodEnd AND e.EndDate >= @PeriodStart)
      OR (e.recorddate >= @PeriodStart AND e.recorddate < DATEADD(day, 1, @PeriodEnd))
  )
ORDER BY e.recorddate, e.ID;

SELECT
    'Accounting_Notes_BySalaryPeriod' AS Section,
    n.NoteID,
    n.NoteDate,
    n.NoteType,
    n.NoteSerial,
    n.NoteSerial1,
    n.Note_Value,
    n.Remark,
    n.salary,
    n.PayrollYear,
    n.PayrollMonth,
    n.Emp_ID,
    n.EmpId,
    n.EmployeeID,
    n.AdvanceID,
    n.BoxID,
    n.BankID,
    n.NoteCashingType,
    n.Posted,
    n.LockSalary
FROM dbo.Notes n
WHERE n.salary = CONVERT(int, @Sgn)
   OR (n.PayrollYear = @Year AND n.PayrollMonth = @Month)
   OR n.Emp_ID = @EmpId
   OR n.EmpId = @EmpId
   OR n.EmployeeID = @EmpId
ORDER BY n.NoteDate DESC, n.NoteID DESC;

SELECT
    'Accounting_DOUBLE_ENTREY_VOUCHERS_ForPeriodNotes' AS Section,
    d.Double_Entry_Vouchers_ID,
    d.DEV_ID_Line_No,
    d.Account_Code,
    d.Value,
    d.Credit_Or_Debit,
    d.Double_Entry_Vouchers_Description,
    d.RecordDate,
    d.Notes_ID,
    d.AdvanceID,
    d.UserID,
    d.Posted,
    d.project_id,
    d.branch_id,
    d.Departementid,
    d.NEmpid,
    d.fixedid,
    d.pandid,
    d.operid
FROM dbo.DOUBLE_ENTREY_VOUCHERS d
WHERE d.Notes_ID IN
(
    SELECT n.NoteID
    FROM dbo.Notes n
    WHERE n.salary = CONVERT(int, @Sgn)
       OR (n.PayrollYear = @Year AND n.PayrollMonth = @Month)
       OR n.Emp_ID = @EmpId
       OR n.EmpId = @EmpId
       OR n.EmployeeID = @EmpId
)
ORDER BY d.Double_Entry_Vouchers_ID, d.DEV_ID_Line_No;

SELECT
    'Accounting_VoucherSummary_ByAccount' AS Section,
    d.Account_Code,
    d.Credit_Or_Debit,
    d.branch_id,
    d.Departementid,
    d.project_id,
    COUNT(*) AS LineCount,
    SUM(ISNULL(d.Value, 0)) AS TotalValue
FROM dbo.DOUBLE_ENTREY_VOUCHERS d
WHERE d.Notes_ID IN
(
    SELECT n.NoteID
    FROM dbo.Notes n
    WHERE n.salary = CONVERT(int, @Sgn)
       OR (n.PayrollYear = @Year AND n.PayrollMonth = @Month)
       OR n.Emp_ID = @EmpId
       OR n.EmpId = @EmpId
       OR n.EmployeeID = @EmpId
)
GROUP BY d.Account_Code, d.Credit_Or_Debit, d.branch_id, d.Departementid, d.project_id
ORDER BY d.Account_Code, d.Credit_Or_Debit;
