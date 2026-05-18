SET NOCOUNT ON;

IF COL_LENGTH('dbo.emp_salary', 'TotalVacValue') IS NULL
BEGIN
    ALTER TABLE dbo.emp_salary ADD TotalVacValue FLOAT NULL;
END;

IF COL_LENGTH('dbo.emp_salary', 'vacDay') IS NULL
BEGIN
    ALTER TABLE dbo.emp_salary ADD vacDay FLOAT NULL;
END;

IF COL_LENGTH('dbo.PayrollRunEmployees', 'VacationDays') IS NULL
BEGIN
    ALTER TABLE dbo.PayrollRunEmployees ADD VacationDays MONEY NOT NULL CONSTRAINT DF_PayrollRunEmployees_VacationDays DEFAULT (0);
END;

IF COL_LENGTH('dbo.PayrollRunEmployees', 'VacationDeduction') IS NULL
BEGIN
    ALTER TABLE dbo.PayrollRunEmployees ADD VacationDeduction MONEY NOT NULL CONSTRAINT DF_PayrollRunEmployees_VacationDeduction DEFAULT (0);
END;

IF COL_LENGTH('dbo.PayrollRunEmployees', 'VacationSalaryValue') IS NULL
BEGIN
    ALTER TABLE dbo.PayrollRunEmployees ADD VacationSalaryValue MONEY NOT NULL CONSTRAINT DF_PayrollRunEmployees_VacationSalaryValue DEFAULT (0);
END;

IF COL_LENGTH('dbo.PayrollRunEmployees', 'AbsentDays') IS NULL
BEGIN
    ALTER TABLE dbo.PayrollRunEmployees ADD AbsentDays MONEY NOT NULL CONSTRAINT DF_PayrollRunEmployees_AbsentDays DEFAULT (0);
END;

IF COL_LENGTH('dbo.PayrollRunEmployees', 'CountDays') IS NULL
BEGIN
    ALTER TABLE dbo.PayrollRunEmployees ADD CountDays MONEY NOT NULL CONSTRAINT DF_PayrollRunEmployees_CountDays DEFAULT (0);
END;

IF COL_LENGTH('dbo.PayrollRunEmployees', 'RemainingDays') IS NULL
BEGIN
    ALTER TABLE dbo.PayrollRunEmployees ADD RemainingDays MONEY NOT NULL CONSTRAINT DF_PayrollRunEmployees_RemainingDays DEFAULT (0);
END;

IF OBJECT_ID('dbo.GetAbcentDay2', 'FN') IS NOT NULL
BEGIN
    DROP FUNCTION dbo.GetAbcentDay2;
END;
GO
CREATE FUNCTION dbo.GetAbcentDay2(@EmpID INTEGER, @YearID INTEGER, @MonthID INTEGER)
RETURNS FLOAT
AS
BEGIN
    RETURN (
        SELECT SUM(d.NoofDays) AS SumNoofDays
        FROM dbo.TblChangedComponentRegister r
        LEFT JOIN dbo.TblChangedComponentRegisterDetails d ON r.ChangedComponentid = d.ChangedComponentid
        WHERE r.Actualmonth = @MonthID
          AND r.Actualyear = @YearID
          AND ISNULL(d.value, 0) = 0
        GROUP BY d.Emp_id
        HAVING d.Emp_id = @EmpID
    );
END;
GO
