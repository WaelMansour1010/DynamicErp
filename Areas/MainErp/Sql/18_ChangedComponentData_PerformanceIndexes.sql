/*
  Optional performance indexes for the shared HR variable components screen.
  Safe to run repeatedly. Do not add uniqueness here because legacy customer data
  may already contain duplicate employee/component/period rows.
*/

IF OBJECT_ID(N'dbo.TblChangedComponentRegister', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.TblChangedComponentRegister', N'Actualyear') IS NOT NULL
   AND COL_LENGTH(N'dbo.TblChangedComponentRegister', N'Actualmonth') IS NOT NULL
   AND COL_LENGTH(N'dbo.TblChangedComponentRegister', N'ComponentID') IS NOT NULL
   AND COL_LENGTH(N'dbo.TblChangedComponentRegister', N'BranchId') IS NOT NULL
   AND COL_LENGTH(N'dbo.TblChangedComponentRegister', N'RecordDate') IS NOT NULL
   AND NOT EXISTS (
       SELECT 1
       FROM sys.indexes
       WHERE object_id = OBJECT_ID(N'dbo.TblChangedComponentRegister')
         AND name = N'IX_TblChangedComponentRegister_Period_Component_Branch')
BEGIN
    CREATE INDEX IX_TblChangedComponentRegister_Period_Component_Branch
    ON dbo.TblChangedComponentRegister (Actualyear, Actualmonth, ComponentID, BranchId)
    INCLUDE (ChangedComponentid, RecordDate);
END;

IF OBJECT_ID(N'dbo.TblChangedComponentRegisterDetails', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.TblChangedComponentRegisterDetails', N'Emp_id') IS NOT NULL
   AND COL_LENGTH(N'dbo.TblChangedComponentRegisterDetails', N'ChangedComponentid') IS NOT NULL
   AND COL_LENGTH(N'dbo.TblChangedComponentRegisterDetails', N'value') IS NOT NULL
   AND COL_LENGTH(N'dbo.TblChangedComponentRegisterDetails', N'Remarks') IS NOT NULL
   AND COL_LENGTH(N'dbo.TblChangedComponentRegisterDetails', N'Salary') IS NOT NULL
   AND COL_LENGTH(N'dbo.TblChangedComponentRegisterDetails', N'projectid') IS NOT NULL
   AND NOT EXISTS (
       SELECT 1
       FROM sys.indexes
       WHERE object_id = OBJECT_ID(N'dbo.TblChangedComponentRegisterDetails')
         AND name = N'IX_TblChangedComponentRegisterDetails_Emp_Header')
BEGIN
    CREATE INDEX IX_TblChangedComponentRegisterDetails_Emp_Header
    ON dbo.TblChangedComponentRegisterDetails (Emp_id, ChangedComponentid)
    INCLUDE ([value], Remarks, Salary, projectid);
END;

IF OBJECT_ID(N'dbo.emp_salary', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.emp_salary', N'm_year') IS NOT NULL
   AND COL_LENGTH(N'dbo.emp_salary', N'm_month') IS NOT NULL
   AND COL_LENGTH(N'dbo.emp_salary', N'BranchId') IS NOT NULL
   AND NOT EXISTS (
       SELECT 1
       FROM sys.indexes
       WHERE object_id = OBJECT_ID(N'dbo.emp_salary')
         AND name = N'IX_emp_salary_Period_Branch')
BEGIN
    CREATE INDEX IX_emp_salary_Period_Branch
    ON dbo.emp_salary (m_year, m_month, BranchId);
END;
