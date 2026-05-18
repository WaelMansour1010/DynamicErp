using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

namespace MyERP.Common.EmployeePayroll
{
    public class EmployeePayrollRepository
    {
        private readonly string _connectionString;

        public EmployeePayrollRepository()
            : this(ResolveConnectionString())
        {
        }

        public EmployeePayrollRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public EmployeePayrollLookups GetLookups()
        {
            var result = new EmployeePayrollLookups();
            using (var connection = OpenConnection())
            {
                FillLookup(connection, "SELECT branch_id, branch_name FROM dbo.TblBranchesData WITH (NOLOCK) ORDER BY branch_name", result.Branches);
                FillLookup(connection, "SELECT DeparmentID, DepartmentName FROM dbo.TblEmpDepartments WITH (NOLOCK) ORDER BY DepartmentName", result.Departments);
                FillLookup(connection, "SELECT JobTypeID, JobTypeName FROM dbo.TblEmpJobsTypes WITH (NOLOCK) ORDER BY JobTypeName", result.Jobs);
                if (TableExists(connection, "MedicalInsuranceProviders"))
                {
                    FillLookup(connection, "SELECT ProviderId, ProviderNameAr FROM dbo.MedicalInsuranceProviders WITH (NOLOCK) WHERE IsActive = 1 ORDER BY ProviderNameAr", result.MedicalInsuranceProviders);
                }

                if (TableExists(connection, "MedicalInsurancePlans"))
                {
                    FillLookup(connection, "SELECT PlanId, PlanNameAr FROM dbo.MedicalInsurancePlans WITH (NOLOCK) WHERE IsActive = 1 ORDER BY PlanNameAr", result.MedicalInsurancePlans);
                }
            }

            return result;
        }

        public IList<MedicalInsuranceProvider> GetMedicalInsuranceProviders(bool activeOnly)
        {
            var rows = new List<MedicalInsuranceProvider>();
            using (var connection = OpenConnection())
            {
                if (!TableExists(connection, "MedicalInsuranceProviders"))
                {
                    return rows;
                }

                using (var command = connection.CreateCommand())
                {
                command.CommandText = @"
SELECT ProviderId, ProviderNameAr, ProviderNameEn, Phone, Notes, IsActive
FROM dbo.MedicalInsuranceProviders WITH (NOLOCK)
WHERE (@ActiveOnly = 0 OR IsActive = 1)
ORDER BY IsActive DESC, ProviderNameAr;";
                command.Parameters.Add("@ActiveOnly", SqlDbType.Bit).Value = activeOnly;
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        rows.Add(new MedicalInsuranceProvider
                        {
                            ProviderId = ReadNullableInt(reader, "ProviderId"),
                            ProviderNameAr = ReadString(reader, "ProviderNameAr"),
                            ProviderNameEn = ReadString(reader, "ProviderNameEn"),
                            Phone = ReadString(reader, "Phone"),
                            Notes = ReadString(reader, "Notes"),
                            IsActive = ReadBool(reader, "IsActive")
                        });
                    }
                }
                }
            }

            return rows;
        }

        public IList<MedicalInsurancePlan> GetMedicalInsurancePlans(bool activeOnly)
        {
            var rows = new List<MedicalInsurancePlan>();
            using (var connection = OpenConnection())
            {
                if (!TableExists(connection, "MedicalInsurancePlans") || !TableExists(connection, "MedicalInsuranceProviders"))
                {
                    return rows;
                }

                using (var command = connection.CreateCommand())
                {
                command.CommandText = @"
SELECT p.PlanId, p.ProviderId, pr.ProviderNameAr, p.PlanNameAr, p.PlanNameEn,
       p.DefaultMonthlyCost, p.DefaultEmployeeShareType, p.DefaultEmployeeShareValue,
       p.DefaultCompanyShareType, p.DefaultCompanyShareValue,
       p.EmployeeDeductionAccountCode, p.CompanyCostAccountCode,
       p.LifecycleStatus, p.StartDate, p.EndDate, p.PayrollStartDate, p.SuspensionDate, p.CancellationDate,
       p.CostCenterCode, p.PayrollDeductionType, p.IsMonthlyDeduction, p.AutoStopAtEndDate, p.ShowInPayroll,
       p.DistributeByDepartment, p.DistributeByCostCenter, p.TaxMode, p.MaxDependents, p.ChildrenMaxAge,
       p.SpouseAdditionalCost, p.ChildAdditionalCost, p.ParentAdditionalCost, p.DefaultCoveragePercent,
       p.AutoEnrollAfterDays, p.AutoEnrollCriteria, p.RulesJson, p.DependentsTemplateJson,
       p.IsActive, p.Notes
FROM dbo.MedicalInsurancePlans p WITH (NOLOCK)
INNER JOIN dbo.MedicalInsuranceProviders pr WITH (NOLOCK) ON pr.ProviderId = p.ProviderId
WHERE (@ActiveOnly = 0 OR p.IsActive = 1)
ORDER BY p.IsActive DESC, pr.ProviderNameAr, p.PlanNameAr;";
                command.Parameters.Add("@ActiveOnly", SqlDbType.Bit).Value = activeOnly;
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        rows.Add(ReadPlan(reader));
                    }
                }
                }
            }

            return rows;
        }

        public MedicalInsurancePlan GetMedicalInsurancePlan(int planId)
        {
            using (var connection = OpenConnection())
            {
                if (!TableExists(connection, "MedicalInsurancePlans") || !TableExists(connection, "MedicalInsuranceProviders"))
                {
                    return null;
                }

                using (var command = connection.CreateCommand())
                {
                command.CommandText = @"
SELECT TOP (1) p.PlanId, p.ProviderId, pr.ProviderNameAr, p.PlanNameAr, p.PlanNameEn,
       p.DefaultMonthlyCost, p.DefaultEmployeeShareType, p.DefaultEmployeeShareValue,
       p.DefaultCompanyShareType, p.DefaultCompanyShareValue,
       p.EmployeeDeductionAccountCode, p.CompanyCostAccountCode,
       p.LifecycleStatus, p.StartDate, p.EndDate, p.PayrollStartDate, p.SuspensionDate, p.CancellationDate,
       p.CostCenterCode, p.PayrollDeductionType, p.IsMonthlyDeduction, p.AutoStopAtEndDate, p.ShowInPayroll,
       p.DistributeByDepartment, p.DistributeByCostCenter, p.TaxMode, p.MaxDependents, p.ChildrenMaxAge,
       p.SpouseAdditionalCost, p.ChildAdditionalCost, p.ParentAdditionalCost, p.DefaultCoveragePercent,
       p.AutoEnrollAfterDays, p.AutoEnrollCriteria, p.RulesJson, p.DependentsTemplateJson,
       p.IsActive, p.Notes
FROM dbo.MedicalInsurancePlans p WITH (NOLOCK)
INNER JOIN dbo.MedicalInsuranceProviders pr WITH (NOLOCK) ON pr.ProviderId = p.ProviderId
WHERE p.PlanId = @PlanId;";
                command.Parameters.Add("@PlanId", SqlDbType.Int).Value = planId;
                using (var reader = command.ExecuteReader())
                {
                    return reader.Read() ? ReadPlan(reader) : null;
                }
                }
            }
        }

        public int SaveMedicalInsuranceProvider(MedicalInsuranceProvider provider, int userId)
        {
            if (provider == null || string.IsNullOrWhiteSpace(provider.ProviderNameAr))
            {
                throw new InvalidOperationException("اسم شركة التأمين الطبي مطلوب.");
            }

            using (var connection = OpenConnection())
            using (var transaction = connection.BeginTransaction())
            {
                if (!TableExists(connection, transaction, "MedicalInsuranceProviders"))
                {
                    throw new InvalidOperationException("جداول شركات التأمين الطبي غير مثبتة في قاعدة البيانات الحالية.");
                }

                var id = provider.ProviderId.GetValueOrDefault();
                if (id <= 0)
                {
                    id = NextId(connection, transaction, "MedicalInsuranceProviders", "ProviderId");
                    using (var command = CreateCommand(connection, transaction, @"
INSERT INTO dbo.MedicalInsuranceProviders
(ProviderId, ProviderNameAr, ProviderNameEn, Phone, Notes, IsActive, CreatedAt, CreatedBy)
VALUES
(@Id, @NameAr, @NameEn, @Phone, @Notes, @IsActive, GETDATE(), @UserId);"))
                    {
                        AddProviderParameters(command, provider, id, userId);
                        command.ExecuteNonQuery();
                    }
                }
                else
                {
                    using (var command = CreateCommand(connection, transaction, @"
UPDATE dbo.MedicalInsuranceProviders
SET ProviderNameAr = @NameAr,
    ProviderNameEn = @NameEn,
    Phone = @Phone,
    Notes = @Notes,
    IsActive = @IsActive
WHERE ProviderId = @Id;"))
                    {
                        AddProviderParameters(command, provider, id, userId);
                        command.ExecuteNonQuery();
                    }
                }

                transaction.Commit();
                return id;
            }
        }

        public int SaveMedicalInsurancePlan(MedicalInsurancePlan plan, int userId)
        {
            if (plan == null || plan.ProviderId <= 0 || string.IsNullOrWhiteSpace(plan.PlanNameAr))
            {
                throw new InvalidOperationException("شركة التأمين واسم الخطة مطلوبان.");
            }

            ValidateShare("Employee", plan.DefaultMonthlyCost, plan.DefaultEmployeeShareType, plan.DefaultEmployeeShareValue);
            ValidateShare("Company", plan.DefaultMonthlyCost, plan.DefaultCompanyShareType, plan.DefaultCompanyShareValue);
            ValidatePlanLifecycle(plan);

            using (var connection = OpenConnection())
            using (var transaction = connection.BeginTransaction())
            {
                if (!TableExists(connection, transaction, "MedicalInsurancePlans"))
                {
                    throw new InvalidOperationException("جداول خطط التأمين الطبي غير مثبتة في قاعدة البيانات الحالية.");
                }

                EnsureMedicalInsurancePlanAccounts(connection, transaction, plan);
                ValidateMedicalInsurancePlanAccounts(connection, transaction, plan);

                var id = plan.PlanId.GetValueOrDefault();
                if (id <= 0)
                {
                    id = NextId(connection, transaction, "MedicalInsurancePlans", "PlanId");
                    using (var command = CreateCommand(connection, transaction, @"
INSERT INTO dbo.MedicalInsurancePlans
(PlanId, ProviderId, PlanNameAr, PlanNameEn, DefaultMonthlyCost,
 DefaultEmployeeShareType, DefaultEmployeeShareValue,
 DefaultCompanyShareType, DefaultCompanyShareValue,
 EmployeeDeductionAccountCode, CompanyCostAccountCode,
 LifecycleStatus, StartDate, EndDate, PayrollStartDate, SuspensionDate, CancellationDate,
 CostCenterCode, PayrollDeductionType, IsMonthlyDeduction, AutoStopAtEndDate, ShowInPayroll,
 DistributeByDepartment, DistributeByCostCenter, TaxMode, MaxDependents, ChildrenMaxAge,
 SpouseAdditionalCost, ChildAdditionalCost, ParentAdditionalCost, DefaultCoveragePercent,
 AutoEnrollAfterDays, AutoEnrollCriteria, RulesJson, DependentsTemplateJson,
 IsActive, Notes, CreatedAt, CreatedBy)
VALUES
(@Id, @ProviderId, @NameAr, @NameEn, @MonthlyCost,
 @EmployeeShareType, @EmployeeShareValue,
 @CompanyShareType, @CompanyShareValue,
 @EmployeeAccountCode, @CompanyAccountCode,
 @LifecycleStatus, @StartDate, @EndDate, @PayrollStartDate, @SuspensionDate, @CancellationDate,
 @CostCenterCode, @PayrollDeductionType, @IsMonthlyDeduction, @AutoStopAtEndDate, @ShowInPayroll,
 @DistributeByDepartment, @DistributeByCostCenter, @TaxMode, @MaxDependents, @ChildrenMaxAge,
 @SpouseAdditionalCost, @ChildAdditionalCost, @ParentAdditionalCost, @DefaultCoveragePercent,
 @AutoEnrollAfterDays, @AutoEnrollCriteria, @RulesJson, @DependentsTemplateJson,
 @IsActive, @Notes, GETDATE(), @UserId);"))
                    {
                        AddPlanParameters(command, plan, id, userId);
                        command.ExecuteNonQuery();
                    }
                }
                else
                {
                    using (var command = CreateCommand(connection, transaction, @"
UPDATE dbo.MedicalInsurancePlans
SET ProviderId = @ProviderId,
    PlanNameAr = @NameAr,
    PlanNameEn = @NameEn,
    DefaultMonthlyCost = @MonthlyCost,
    DefaultEmployeeShareType = @EmployeeShareType,
    DefaultEmployeeShareValue = @EmployeeShareValue,
    DefaultCompanyShareType = @CompanyShareType,
    DefaultCompanyShareValue = @CompanyShareValue,
    EmployeeDeductionAccountCode = @EmployeeAccountCode,
    CompanyCostAccountCode = @CompanyAccountCode,
    LifecycleStatus = @LifecycleStatus,
    StartDate = @StartDate,
    EndDate = @EndDate,
    PayrollStartDate = @PayrollStartDate,
    SuspensionDate = @SuspensionDate,
    CancellationDate = @CancellationDate,
    CostCenterCode = @CostCenterCode,
    PayrollDeductionType = @PayrollDeductionType,
    IsMonthlyDeduction = @IsMonthlyDeduction,
    AutoStopAtEndDate = @AutoStopAtEndDate,
    ShowInPayroll = @ShowInPayroll,
    DistributeByDepartment = @DistributeByDepartment,
    DistributeByCostCenter = @DistributeByCostCenter,
    TaxMode = @TaxMode,
    MaxDependents = @MaxDependents,
    ChildrenMaxAge = @ChildrenMaxAge,
    SpouseAdditionalCost = @SpouseAdditionalCost,
    ChildAdditionalCost = @ChildAdditionalCost,
    ParentAdditionalCost = @ParentAdditionalCost,
    DefaultCoveragePercent = @DefaultCoveragePercent,
    AutoEnrollAfterDays = @AutoEnrollAfterDays,
    AutoEnrollCriteria = @AutoEnrollCriteria,
    RulesJson = @RulesJson,
    DependentsTemplateJson = @DependentsTemplateJson,
    IsActive = @IsActive,
    Notes = @Notes,
    UpdatedAt = GETDATE(),
    UpdatedBy = @UserId
WHERE PlanId = @Id;"))
                    {
                        AddPlanParameters(command, plan, id, userId);
                        command.ExecuteNonQuery();
                    }
                }

                transaction.Commit();
                return id;
            }
        }

        public IList<EmployeeSummary> SearchEmployees(EmployeeSearchFilter filter)
        {
            filter = filter ?? new EmployeeSearchFilter();
            var rows = new List<EmployeeSummary>();
            using (var connection = OpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
SELECT TOP (300)
    e.Emp_ID, e.Emp_Code, e.Emp_Name, e.BranchId, b.branch_name,
    e.DepartmentID, d.DepartmentName, e.JobTypeID, j.JobTypeName,
    e.BignDateWork, e.chkStop,
    COALESCE(
        NULLIF(CONVERT(money, e.Emp_Salary), 0),
        NULLIF(CONVERT(money, e.BasicSalary), 0),
        NULLIF(CONVERT(money, e.TotalSalary), 0),
        NULLIF(CONVERT(money, lastSalary.Emp_Salary), 0),
        NULLIF(CONVERT(money, lastSalary.total1), 0),
        NULLIF(CONVERT(money, lastSalary.Comp13), 0),
        NULLIF(CONVERT(money, lastSalary.EmpTotalNet), 0),
        0
    ) AS Emp_Salary,
    e.Account_code, e.Account_code1,
    e.Emp_Phone, e.Emp_mobile, e.Emp_Mail, e.EmployeePhotoDataUrl, e.EmpNotes
FROM dbo.TblEmployee e WITH (NOLOCK)
LEFT JOIN dbo.TblBranchesData b WITH (NOLOCK) ON b.branch_id = e.BranchId
LEFT JOIN dbo.TblEmpDepartments d WITH (NOLOCK) ON d.DeparmentID = e.DepartmentID
LEFT JOIN dbo.TblEmpJobsTypes j WITH (NOLOCK) ON j.JobTypeID = e.JobTypeID
OUTER APPLY (
    SELECT TOP (1) s.Emp_Salary, s.total1, s.Comp13, s.EmpTotalNet
    FROM dbo.emp_salary s WITH (NOLOCK)
    WHERE s.emp_id = e.Emp_ID
    ORDER BY ISNULL(s.RecordDate, '19000101') DESC, s.id DESC
) lastSalary
WHERE (@Term IS NULL OR e.Emp_Code LIKE @LikeTerm OR e.Emp_Name LIKE @LikeTerm OR e.Emp_mobile LIKE @LikeTerm)
  AND (@BranchId IS NULL OR e.BranchId = @BranchId)
  AND (@DepartmentId IS NULL OR e.DepartmentID = @DepartmentId)
  AND (@IsActive IS NULL OR ISNULL(e.chkStop, 0) = CASE WHEN @IsActive = 1 THEN 0 ELSE 1 END)
ORDER BY e.Emp_ID DESC;";
                AddNullable(command, "@Term", SqlDbType.NVarChar, string.IsNullOrWhiteSpace(filter.Term) ? (object)DBNull.Value : filter.Term.Trim());
                AddNullable(command, "@LikeTerm", SqlDbType.NVarChar, string.IsNullOrWhiteSpace(filter.Term) ? (object)DBNull.Value : "%" + filter.Term.Trim() + "%");
                AddNullable(command, "@BranchId", SqlDbType.Int, filter.BranchId);
                AddNullable(command, "@DepartmentId", SqlDbType.Int, filter.DepartmentId);
                AddNullable(command, "@IsActive", SqlDbType.Bit, filter.IsActive);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        rows.Add(ReadEmployee(reader));
                    }
                }
            }

            return rows;
        }

        public EmployeeSummary GetEmployee(int id)
        {
            using (var connection = OpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
SELECT TOP (1)
    e.Emp_ID, e.Emp_Code, e.Emp_Name, e.BranchId, b.branch_name,
    e.DepartmentID, d.DepartmentName, e.JobTypeID, j.JobTypeName,
    e.BignDateWork, e.chkStop,
    COALESCE(
        NULLIF(CONVERT(money, e.Emp_Salary), 0),
        NULLIF(CONVERT(money, e.BasicSalary), 0),
        NULLIF(CONVERT(money, e.TotalSalary), 0),
        NULLIF(CONVERT(money, lastSalary.Emp_Salary), 0),
        NULLIF(CONVERT(money, lastSalary.total1), 0),
        NULLIF(CONVERT(money, lastSalary.Comp13), 0),
        NULLIF(CONVERT(money, lastSalary.EmpTotalNet), 0),
        0
    ) AS Emp_Salary,
    e.Account_code, e.Account_code1,
    e.Emp_Phone, e.Emp_mobile, e.Emp_Mail, e.EmployeePhotoDataUrl, e.EmpNotes
FROM dbo.TblEmployee e WITH (NOLOCK)
LEFT JOIN dbo.TblBranchesData b WITH (NOLOCK) ON b.branch_id = e.BranchId
LEFT JOIN dbo.TblEmpDepartments d WITH (NOLOCK) ON d.DeparmentID = e.DepartmentID
LEFT JOIN dbo.TblEmpJobsTypes j WITH (NOLOCK) ON j.JobTypeID = e.JobTypeID
OUTER APPLY (
    SELECT TOP (1) s.Emp_Salary, s.total1, s.Comp13, s.EmpTotalNet
    FROM dbo.emp_salary s WITH (NOLOCK)
    WHERE s.emp_id = e.Emp_ID
    ORDER BY ISNULL(s.RecordDate, '19000101') DESC, s.id DESC
) lastSalary
WHERE e.Emp_ID = @Id;";
                command.Parameters.Add("@Id", SqlDbType.Int).Value = id;
                using (var reader = command.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        return null;
                    }

                    var employee = ReadEmployee(reader);
                    reader.Close();
                    employee.MedicalInsuranceHistory = GetMedicalInsuranceHistory(connection, id);
                    employee.MedicalInsurance = employee.MedicalInsuranceHistory.Count > 0 ? employee.MedicalInsuranceHistory[0] : null;
                    return employee;
                }
            }
        }

        public int SaveEmployee(EmployeeSaveRequest request, int userId)
        {
            if (request == null)
            {
                throw new ArgumentNullException("request");
            }

            if (string.IsNullOrWhiteSpace(request.EmployeeCode) || string.IsNullOrWhiteSpace(request.EmployeeName))
            {
                throw new InvalidOperationException("Employee code and name are required.");
            }

            using (var connection = OpenConnection())
            using (var transaction = connection.BeginTransaction())
            {
                var employeeId = request.EmployeeId.GetValueOrDefault();
                if (employeeId <= 0)
                {
                    employeeId = NextId(connection, transaction, "TblEmployee", "Emp_ID");
                    var employeeAccounts = EnsureEmployeeAccounts(connection, transaction, request, employeeId);
                    using (var command = CreateCommand(connection, transaction, @"
INSERT INTO dbo.TblEmployee
(Emp_ID, Emp_Code, Emp_Name, BranchId, DepartmentID, JobTypeID, BignDateWork, chkStop, workstate, Emp_Salary, Account_code, Account_code1, Account_Code2, Account_Code3, Account_Code4, Account_Code5, Emp_Phone, Emp_mobile, Emp_Mail, EmployeePhotoDataUrl, EmpNotes)
VALUES
(@Id, @Code, @Name, @BranchId, @DepartmentId, @JobTypeId, @HiringDate, @Stopped, @WorkState, @Salary, @AccountCode, @AccruedAccountCode, @VacationProvisionAccountCode, @AdvancePaymentAccountCode, @EndOfServiceAccountCode, @TicketProvisionAccountCode, @Phone, @Mobile, @Email, @PhotoDataUrl, @Notes);"))
                    {
                        AddEmployeeParameters(command, request, employeeId, employeeAccounts);
                        command.ExecuteNonQuery();
                    }
                }
                else
                {
                    var employeeAccounts = EnsureEmployeeAccounts(connection, transaction, request, employeeId);
                    using (var command = CreateCommand(connection, transaction, @"
UPDATE dbo.TblEmployee
SET Emp_Code = @Code,
    Emp_Name = @Name,
    BranchId = @BranchId,
    DepartmentID = @DepartmentId,
    JobTypeID = @JobTypeId,
    BignDateWork = @HiringDate,
    chkStop = @Stopped,
    workstate = @WorkState,
    Emp_Salary = @Salary,
    Account_code = @AccountCode,
    Account_code1 = @AccruedAccountCode,
    Account_Code2 = @VacationProvisionAccountCode,
    Account_Code3 = @AdvancePaymentAccountCode,
    Account_Code4 = @EndOfServiceAccountCode,
    Account_Code5 = @TicketProvisionAccountCode,
    Emp_Phone = @Phone,
    Emp_mobile = @Mobile,
    Emp_Mail = @Email,
    EmployeePhotoDataUrl = @PhotoDataUrl,
    EmpNotes = @Notes
WHERE Emp_ID = @Id;"))
                    {
                        AddEmployeeParameters(command, request, employeeId, employeeAccounts);
                        if (command.ExecuteNonQuery() == 0)
                        {
                            throw new InvalidOperationException("الموظف المحدد غير موجود ولا يمكن تحديث بيانات التأمين الطبي له.");
                        }
                    }
                }

                if (request.MedicalInsurance != null)
                {
                    SaveMedicalInsurance(connection, transaction, request.MedicalInsurance, employeeId, userId);
                }

                transaction.Commit();
                return employeeId;
            }
        }

        private EmployeeAccountCodes EnsureEmployeeAccounts(SqlConnection connection, SqlTransaction transaction, EmployeeSaveRequest request, int employeeId)
        {
            var accounts = GetExistingEmployeeAccountCodes(connection, transaction, employeeId);
            accounts.EmployeeAccountCode = FirstNonEmpty(request.AccountCode, accounts.EmployeeAccountCode);
            accounts.AccruedSalaryAccountCode = FirstNonEmpty(request.AccruedSalaryAccountCode, accounts.AccruedSalaryAccountCode);

            var parents = ResolveEmployeeAccountParents(connection, transaction);
            var employeeName = request.EmployeeName ?? string.Empty;
            var employeeNameEn = employeeName;

            if (string.IsNullOrWhiteSpace(accounts.EmployeeAccountCode))
            {
                accounts.EmployeeAccountCode = TryCreateEmployeeAccount(connection, transaction, parents.EmployeeAccountParentCode, employeeName, employeeNameEn, request.BranchId);
            }

            if (string.IsNullOrWhiteSpace(accounts.AccruedSalaryAccountCode))
            {
                accounts.AccruedSalaryAccountCode = TryCreateEmployeeAccount(connection, transaction, parents.AccruedSalaryParentCode, employeeName + "   اجور مستحقة ", employeeNameEn + " Salary Due", request.BranchId);
            }

            if (string.IsNullOrWhiteSpace(accounts.VacationProvisionAccountCode))
            {
                accounts.VacationProvisionAccountCode = TryCreateEmployeeAccount(connection, transaction, parents.VacationProvisionParentCode, employeeName + "   مخصصات اجازة ", employeeNameEn + " Vacation Provision", request.BranchId);
            }

            if (string.IsNullOrWhiteSpace(accounts.AdvancePaymentAccountCode))
            {
                accounts.AdvancePaymentAccountCode = TryCreateEmployeeAccount(connection, transaction, parents.AdvancePaymentParentCode, employeeName + "   مدفوعات مقدمه ", employeeNameEn + " Advance Payment", request.BranchId);
            }

            if (string.IsNullOrWhiteSpace(accounts.EndOfServiceAccountCode))
            {
                accounts.EndOfServiceAccountCode = TryCreateEmployeeAccount(connection, transaction, parents.EndOfServiceParentCode, employeeName + "   مخصصات   نهاية خدمة ", employeeNameEn + " End Of Service Provision", request.BranchId);
            }

            if (string.IsNullOrWhiteSpace(accounts.TicketProvisionAccountCode))
            {
                accounts.TicketProvisionAccountCode = TryCreateEmployeeAccount(connection, transaction, parents.TicketProvisionParentCode, employeeName + "   مخصص تذاكر   ", employeeNameEn + " Ticket Provision", request.BranchId);
            }

            request.AccountCode = accounts.EmployeeAccountCode;
            request.AccruedSalaryAccountCode = accounts.AccruedSalaryAccountCode;
            return accounts;
        }

        private EmployeeAccountCodes GetExistingEmployeeAccountCodes(SqlConnection connection, SqlTransaction transaction, int employeeId)
        {
            var accounts = new EmployeeAccountCodes();
            if (employeeId <= 0)
            {
                return accounts;
            }

            using (var command = CreateCommand(connection, transaction, @"
SELECT Account_code, Account_code1, Account_Code2, Account_Code3, Account_Code4, Account_Code5
FROM dbo.TblEmployee WITH (NOLOCK)
WHERE Emp_ID = @Id;"))
            {
                command.Parameters.Add("@Id", SqlDbType.Int).Value = employeeId;
                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        accounts.EmployeeAccountCode = ReadString(reader, "Account_code");
                        accounts.AccruedSalaryAccountCode = ReadString(reader, "Account_code1");
                        accounts.VacationProvisionAccountCode = ReadString(reader, "Account_Code2");
                        accounts.AdvancePaymentAccountCode = ReadString(reader, "Account_Code3");
                        accounts.EndOfServiceAccountCode = ReadString(reader, "Account_Code4");
                        accounts.TicketProvisionAccountCode = ReadString(reader, "Account_Code5");
                    }
                }
            }

            return accounts;
        }

        private EmployeeAccountParents ResolveEmployeeAccountParents(SqlConnection connection, SqlTransaction transaction)
        {
            return new EmployeeAccountParents
            {
                EmployeeAccountParentCode = ResolveEmployeeAccountParent(connection, transaction, "Account_code", "a1a2a4a1"),
                AccruedSalaryParentCode = ResolveEmployeeAccountParent(connection, transaction, "Account_code1", "a2a2a4a1"),
                VacationProvisionParentCode = ResolveEmployeeAccountParent(connection, transaction, "Account_Code2", "a2a2a3a1"),
                AdvancePaymentParentCode = ResolveEmployeeAccountParent(connection, transaction, "Account_Code3", "a1a2a5a3"),
                EndOfServiceParentCode = ResolveEmployeeAccountParent(connection, transaction, "Account_Code4", "a2a2a3a5"),
                TicketProvisionParentCode = ResolveEmployeeAccountParent(connection, transaction, "Account_Code5", "a2a2a3a1")
            };
        }

        private string ResolveEmployeeAccountParent(SqlConnection connection, SqlTransaction transaction, string employeeColumnName, string fallbackParentCode)
        {
            using (var command = CreateCommand(connection, transaction, @"
SELECT TOP (1) a.Parent_Account_Code
FROM dbo.TblEmployee e WITH (NOLOCK)
INNER JOIN dbo.ACCOUNTS a WITH (NOLOCK) ON a.Account_Code = e." + employeeColumnName + @"
WHERE NULLIF(e." + employeeColumnName + @", '') IS NOT NULL
  AND NULLIF(a.Parent_Account_Code, '') IS NOT NULL
GROUP BY a.Parent_Account_Code
ORDER BY COUNT(*) DESC;"))
            {
                var value = Convert.ToString(command.ExecuteScalar());
                if (AccountExists(connection, transaction, value, false))
                {
                    return value;
                }
            }

            return AccountExists(connection, transaction, fallbackParentCode, false) ? fallbackParentCode : null;
        }

        private string TryCreateEmployeeAccount(SqlConnection connection, SqlTransaction transaction, string parentAccountCode, string accountName, string accountNameEn, int? branchId)
        {
            if (string.IsNullOrWhiteSpace(parentAccountCode) || !AccountExists(connection, transaction, parentAccountCode, false))
            {
                return null;
            }

            var parent = GetAccountDefinition(connection, transaction, parentAccountCode);
            if (parent == null || parent.LastAccount)
            {
                return null;
            }

            var childNumber = GetNextChildAccountNumber(connection, transaction, parentAccountCode);
            var accountCode = parentAccountCode + "a" + childNumber.ToString();
            var accountSerial = BuildNextAccountSerial(connection, transaction, parentAccountCode, parent.AccountSerial, childNumber);
            using (var command = CreateCommand(connection, transaction, @"
INSERT INTO dbo.ACCOUNTS
(Account_Code, Account_Name, Parent_Account_Code, last_account, cannot_del,
 Account_Serial, BasicAccount, DateCreated, Account_NameEng, mowazna, currenct_code,
 cost_center, Sum_account, cost_center_type, cost_center_id, ActivityTypeId, AccountTypes,
 AccountTab, DepitOrCredit, Differenttype, Authority, Block, UserGroupId, UserId, Branch, BranchID)
VALUES
(@AccountCode, @AccountName, @ParentAccountCode, 1, 0,
 @AccountSerial, 0, GETDATE(), @AccountNameEn, @Budget, @CurrencyCode,
 @CostCenter, @SumAccount, @CostCenterType, @CostCenterId, @ActivityTypeId, @AccountTypes,
 @AccountTab, @DepitOrCredit, @DifferentType, @Authority, 0, @UserGroupId, @UserId, @Branch, @BranchId);"))
            {
                command.Parameters.Add("@AccountCode", SqlDbType.NVarChar, 50).Value = accountCode;
                AddNullable(command, "@AccountName", SqlDbType.NVarChar, accountName);
                command.Parameters.Add("@ParentAccountCode", SqlDbType.NVarChar, 70).Value = parentAccountCode;
                AddNullable(command, "@AccountSerial", SqlDbType.NVarChar, accountSerial);
                AddNullable(command, "@AccountNameEn", SqlDbType.NVarChar, accountNameEn);
                command.Parameters.Add("@Budget", SqlDbType.Bit).Value = parent.Budget;
                AddNullable(command, "@CurrencyCode", SqlDbType.NVarChar, FirstNonEmpty(parent.CurrencyCode, "1"));
                command.Parameters.Add("@CostCenter", SqlDbType.Bit).Value = parent.CostCenter;
                command.Parameters.Add("@SumAccount", SqlDbType.Bit).Value = parent.SumAccount;
                AddNullable(command, "@CostCenterType", SqlDbType.Int, parent.CostCenterType);
                AddNullable(command, "@CostCenterId", SqlDbType.NVarChar, parent.CostCenterId);
                AddNullable(command, "@ActivityTypeId", SqlDbType.Int, parent.ActivityTypeId);
                AddNullable(command, "@AccountTypes", SqlDbType.Int, parent.AccountTypes);
                AddNullable(command, "@AccountTab", SqlDbType.Int, parent.AccountTab);
                AddNullable(command, "@DepitOrCredit", SqlDbType.Int, parent.DepitOrCredit);
                AddNullable(command, "@DifferentType", SqlDbType.Int, parent.DifferentType);
                AddNullable(command, "@Authority", SqlDbType.Int, parent.Authority);
                AddNullable(command, "@UserGroupId", SqlDbType.Int, parent.UserGroupId);
                AddNullable(command, "@UserId", SqlDbType.Int, parent.UserId);
                AddNullable(command, "@Branch", SqlDbType.VarChar, FirstNonEmpty(parent.Branch, branchId.HasValue ? branchId.Value.ToString() : null));
                AddNullable(command, "@BranchId", SqlDbType.Int, branchId ?? parent.BranchId);
                command.ExecuteNonQuery();
            }

            return accountCode;
        }

        private AccountDefinition GetAccountDefinition(SqlConnection connection, SqlTransaction transaction, string accountCode)
        {
            using (var command = CreateCommand(connection, transaction, @"
SELECT Account_Code, Account_Serial, last_account, mowazna, currenct_code, cost_center, Sum_account,
       cost_center_type, cost_center_id, ActivityTypeId, AccountTypes, AccountTab, DepitOrCredit,
       Differenttype, Authority, UserGroupId, UserId, Branch, BranchID
FROM dbo.ACCOUNTS WITH (UPDLOCK, HOLDLOCK)
WHERE Account_Code = @AccountCode;"))
            {
                command.Parameters.Add("@AccountCode", SqlDbType.NVarChar, 50).Value = accountCode;
                using (var reader = command.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        return null;
                    }

                    return new AccountDefinition
                    {
                        AccountCode = ReadString(reader, "Account_Code"),
                        AccountSerial = ReadString(reader, "Account_Serial"),
                        LastAccount = ReadBool(reader, "last_account"),
                        Budget = ReadBool(reader, "mowazna"),
                        CurrencyCode = ReadString(reader, "currenct_code"),
                        CostCenter = ReadBool(reader, "cost_center"),
                        SumAccount = ReadBool(reader, "Sum_account"),
                        CostCenterType = ReadNullableInt(reader, "cost_center_type"),
                        CostCenterId = ReadString(reader, "cost_center_id"),
                        ActivityTypeId = ReadNullableInt(reader, "ActivityTypeId"),
                        AccountTypes = ReadNullableInt(reader, "AccountTypes"),
                        AccountTab = ReadNullableInt(reader, "AccountTab"),
                        DepitOrCredit = ReadNullableInt(reader, "DepitOrCredit"),
                        DifferentType = ReadNullableInt(reader, "Differenttype"),
                        Authority = ReadNullableInt(reader, "Authority"),
                        UserGroupId = ReadNullableInt(reader, "UserGroupId"),
                        UserId = ReadNullableInt(reader, "UserId"),
                        Branch = ReadString(reader, "Branch"),
                        BranchId = ReadNullableInt(reader, "BranchID")
                    };
                }
            }
        }

        private int GetNextChildAccountNumber(SqlConnection connection, SqlTransaction transaction, string parentAccountCode)
        {
            var max = 0;
            using (var command = CreateCommand(connection, transaction, @"
SELECT Account_Code
FROM dbo.ACCOUNTS WITH (UPDLOCK, HOLDLOCK)
WHERE Parent_Account_Code = @ParentAccountCode;"))
            {
                command.Parameters.Add("@ParentAccountCode", SqlDbType.NVarChar, 70).Value = parentAccountCode;
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var code = ReadString(reader, "Account_Code");
                        var marker = code.LastIndexOf('a');
                        int value;
                        if (marker >= 0 && int.TryParse(code.Substring(marker + 1), out value) && value > max)
                        {
                            max = value;
                        }
                    }
                }
            }

            return max + 1;
        }

        private string BuildNextAccountSerial(SqlConnection connection, SqlTransaction transaction, string parentAccountCode, string parentAccountSerial, int childNumber)
        {
            var level = CountAccountLevels(parentAccountCode) + 1;
            var digits = GetAccountSerialDigits(connection, transaction, level);
            var maxSerialSuffix = 0;
            using (var command = CreateCommand(connection, transaction, @"
SELECT Account_Serial
FROM dbo.ACCOUNTS WITH (UPDLOCK, HOLDLOCK)
WHERE Parent_Account_Code = @ParentAccountCode
  AND Account_Serial LIKE @ParentSerial + '%';"))
            {
                command.Parameters.Add("@ParentAccountCode", SqlDbType.NVarChar, 70).Value = parentAccountCode;
                command.Parameters.Add("@ParentSerial", SqlDbType.NVarChar, 255).Value = (object)parentAccountSerial ?? string.Empty;
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var serial = ReadString(reader, "Account_Serial");
                        if (!string.IsNullOrEmpty(parentAccountSerial) && serial.StartsWith(parentAccountSerial, StringComparison.OrdinalIgnoreCase))
                        {
                            int value;
                            if (int.TryParse(serial.Substring(parentAccountSerial.Length), out value) && value > maxSerialSuffix)
                            {
                                maxSerialSuffix = value;
                            }
                        }
                    }
                }
            }

            var next = Math.Max(maxSerialSuffix + 1, childNumber);
            return (parentAccountSerial ?? string.Empty) + next.ToString(new string('0', Math.Max(digits, 1)));
        }

        private int GetAccountSerialDigits(SqlConnection connection, SqlTransaction transaction, int level)
        {
            using (var command = CreateCommand(connection, transaction, "SELECT TOP (1) NoOfDigits FROM dbo.AccountsLevelsDetails WITH (NOLOCK) WHERE [Level] = @Level ORDER BY id DESC;"))
            {
                command.Parameters.Add("@Level", SqlDbType.Int).Value = level;
                var value = command.ExecuteScalar();
                if (value != null && value != DBNull.Value)
                {
                    return Convert.ToInt32(value);
                }
            }

            return 1;
        }

        private static int CountAccountLevels(string accountCode)
        {
            if (string.IsNullOrWhiteSpace(accountCode))
            {
                return 0;
            }

            var count = 0;
            for (var i = 0; i < accountCode.Length; i++)
            {
                if (accountCode[i] == 'a' || accountCode[i] == 'A')
                {
                    count++;
                }
            }

            return count;
        }

        private bool AccountExists(SqlConnection connection, SqlTransaction transaction, string accountCode, bool requireLastAccount)
        {
            if (string.IsNullOrWhiteSpace(accountCode))
            {
                return false;
            }

            using (var command = CreateCommand(connection, transaction, "SELECT COUNT(1) FROM dbo.ACCOUNTS WITH (NOLOCK) WHERE Account_Code = @AccountCode AND (@RequireLast = 0 OR ISNULL(last_account, 0) = 1);"))
            {
                command.Parameters.Add("@AccountCode", SqlDbType.NVarChar, 50).Value = accountCode;
                command.Parameters.Add("@RequireLast", SqlDbType.Bit).Value = requireLastAccount;
                return Convert.ToInt32(command.ExecuteScalar()) > 0;
            }
        }

        public void SetEmployeeActive(int employeeId, bool isActive)
        {
            using (var connection = OpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "UPDATE dbo.TblEmployee SET chkStop = @Stopped WHERE Emp_ID = @Id";
                command.Parameters.Add("@Stopped", SqlDbType.Bit).Value = isActive ? 0 : 1;
                command.Parameters.Add("@Id", SqlDbType.Int).Value = employeeId;
                command.ExecuteNonQuery();
            }
        }

        public PayrollCompatibilityParityReport BuildCompatibilityParityReport(SalaryRunRequest request)
        {
            var preview = PreviewSalaryRun(request);
            var report = new PayrollCompatibilityParityReport
            {
                Request = preview.Request,
                SafetyStatus = "Read-only parity report. Reconstructed rows are blocked from posting, notes, vouchers, payment marking, SendTopost replacement, and allocation rebuild."
            };

            foreach (var row in preview.Rows)
            {
                var add = row.Components.Where(x => x.ViewComponent && !x.AddOrDiscount).Sum(x => x.SourceValue);
                var deduct = row.Components.Where(x => x.ViewComponent && x.AddOrDiscount).Sum(x => x.SourceValue);
                var reconstructedTotal1 = add + row.VariableAdditions;
                var reconstructedTotal2 = row.AdvanceDeduction + row.ExistingDiscounts + row.MedicalInsuranceDeduction + row.VacationDeduction + deduct;
                var reconstructedNet = reconstructedTotal1 - reconstructedTotal2;
                var parityRow = new PayrollCompatibilityParityRow
                {
                    EmployeeId = row.EmployeeId,
                    EmployeeCode = row.EmployeeCode,
                    EmployeeName = row.EmployeeName,
                    CompatibilityStatus = row.CompatibilityStatus,
                    IsLegacySnapshot = row.IsLegacySnapshot,
                    LegacyTotal1 = row.IsLegacySnapshot ? row.TotalBeforeDeductions : 0,
                    ReconstructedTotal1 = reconstructedTotal1,
                    LegacyTotal2 = row.IsLegacySnapshot ? row.TotalDeductions : 0,
                    ReconstructedTotal2 = reconstructedTotal2,
                    LegacyInsurance = row.TotalInsuranceLegacy,
                    RuntimeInsurance = row.InsuranceTrace != null ? row.InsuranceTrace.RuntimeFunctionInsurance : row.TotalInsuranceLegacy,
                    InsuranceSource = row.InsuranceTrace != null ? row.InsuranceTrace.SourceFunction : string.Empty,
                    LegacyNet = row.IsLegacySnapshot ? row.NetSalary : 0,
                    ReconstructedNet = reconstructedNet
                };
                parityRow.Total1Diff = parityRow.ReconstructedTotal1 - parityRow.LegacyTotal1;
                parityRow.Total2Diff = parityRow.ReconstructedTotal2 - parityRow.LegacyTotal2;
                parityRow.NetDiff = parityRow.ReconstructedNet - parityRow.LegacyNet;

                foreach (var component in row.Components)
                {
                    var diff = component.SourceValue - component.SnapshotValue;
                    if (diff != 0)
                    {
                        parityRow.ComponentDiffs.Add(BuildComponentDiff(row, component));
                    }
                }

                parityRow.ComponentMismatchCount = parityRow.ComponentDiffs.Count;
                report.Rows.Add(parityRow);
                report.LegacyNetTotal += parityRow.LegacyNet;
                report.ReconstructedNetTotal += parityRow.ReconstructedNet;
            }

            report.TotalRows = report.Rows.Count;
            report.LegacySnapshotRows = report.Rows.Count(x => x.IsLegacySnapshot);
            report.ReconstructedRows = report.Rows.Count(x => !x.IsLegacySnapshot);
            report.ComponentMismatchRows = report.Rows.Count(x => x.ComponentMismatchCount > 0);
            report.TotalMismatchRows = report.Rows.Count(x => x.Total1Diff != 0 || x.Total2Diff != 0 || x.NetDiff != 0);
            report.NetDifference = report.ReconstructedNetTotal - report.LegacyNetTotal;
            return report;
        }

        public PayrollCompatibilityExplainResult ExplainCompatibilityComponent(PayrollCompatibilityExplainRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException("request");
            }

            if (!request.EmployeeId.HasValue || request.EmployeeId.Value <= 0)
            {
                throw new InvalidOperationException("EmployeeId is required for component explainability tracing.");
            }

            if (request.ComponentNo < 1 || request.ComponentNo > 40)
            {
                throw new InvalidOperationException("ComponentNo must be between 1 and 40.");
            }

            request.IncludeSavedDrafts = true;
            var preview = PreviewSalaryRun(request);
            var row = preview.Rows.FirstOrDefault(x => x.EmployeeId == request.EmployeeId.Value);
            if (row == null)
            {
                throw new InvalidOperationException("No payroll row was found for the requested employee and period.");
            }

            var component = row.Components.FirstOrDefault(x => x.ComponentNo == request.ComponentNo);
            if (component == null)
            {
                throw new InvalidOperationException("No payroll component metadata was found for the requested component number.");
            }

            var diff = BuildComponentDiff(row, component);
            var result = new PayrollCompatibilityExplainResult
            {
                EmployeeId = row.EmployeeId,
                EmployeeCode = row.EmployeeCode,
                EmployeeName = row.EmployeeName,
                ComponentNo = component.ComponentNo,
                ComponentColumn = component.ComponentColumn,
                ComponentNameAr = component.ComponentNameAr,
                CompatibilityStatus = row.CompatibilityStatus,
                LegacyValue = component.SnapshotValue,
                ReconstructedValue = component.SourceValue,
                Difference = component.SourceValue - component.SnapshotValue,
                FixedSourceValue = component.FixedSourceValue,
                ChangedSourceValue = component.ChangedSourceValue,
                OverrideSourceValue = component.OverrideSourceValue,
                RawSourceValue = component.RawSourceValue,
                TemporalAdjustedValue = component.TemporalAdjustedValue,
                PrecedenceDecision = diff.PrecedenceDecision,
                MismatchCategory = diff.MismatchCategory,
                LikelySource = diff.LikelySource,
                ConfidenceScore = diff.ConfidenceScore,
                Proration = BuildProrationTrace(row, component, diff.MismatchCategory),
                Insurance = row.InsuranceTrace
            };

            result.Explanation.Add("Compatibility status: " + row.CompatibilityStatus);
            result.Explanation.Add("Legacy snapshot value: " + component.SnapshotValue.ToString("0.####"));
            result.Explanation.Add("Reconstructed value: " + component.SourceValue.ToString("0.####"));
            result.Explanation.Add("Fixed source EmpSalaryComponent: " + component.FixedSourceValue.ToString("0.####"));
            result.Explanation.Add("Changed source TblChangedComponentRegisterDetails: " + component.ChangedSourceValue.ToString("0.####"));
            result.Explanation.Add("Year override source TblComponentYearDet: " + component.OverrideSourceValue.ToString("0.####"));
            result.Explanation.Add("Raw source before temporal rule: " + component.RawSourceValue.ToString("0.####"));
            result.Explanation.Add("Temporal adjusted value: " + component.TemporalAdjustedValue.ToString("0.####"));
            result.Explanation.Add("Precedence: " + diff.PrecedenceDecision);
            result.Explanation.Add("Temporal rule path: " + result.Proration.RulePath);
            result.Explanation.Add("Denominator selected: " + result.Proration.ActualDenominator.ToString("0.####") + " (" + result.Proration.DenominatorReason + ")");
            if (row.InsuranceTrace != null)
            {
                result.Explanation.Add("Insurance source: " + row.InsuranceTrace.SourceFunction + " via " + row.InsuranceTrace.SourceTables);
                result.Explanation.Add("Insurance snapshot/runtime: " + row.InsuranceTrace.SnapshotToalInsurance.ToString("0.####") + " / " + row.InsuranceTrace.RuntimeFunctionInsurance.ToString("0.####"));
                result.Explanation.Add("Insurance basis: base=" + row.InsuranceTrace.InsuranceComponentBase.ToString("0.####") + ", citizen%=" + row.InsuranceTrace.CitizenPercent.ToString("0.####") + ", resident%=" + row.InsuranceTrace.ResidentPercent.ToString("0.####"));
                result.Explanation.Add("Insurance posting accounts: accrued=" + row.InsuranceTrace.EmployeeAccruedSalaryAccount + ", insurance-credit=" + row.InsuranceTrace.InsuranceCreditAccount);
                if (!string.IsNullOrWhiteSpace(row.InsuranceTrace.ExclusionReason))
                {
                    result.Explanation.Add("Insurance exclusion/ambiguity: " + row.InsuranceTrace.ExclusionReason);
                }
            }

            result.Explanation.Add("Mismatch category: " + diff.MismatchCategory + " (" + diff.ConfidenceScore.ToString("0.##") + ")");
            result.Explanation.Add("Safety: this diagnostic endpoint is read-only and cannot post notes, vouchers, payments, or allocations.");
            return result;
        }

        public PayrollAccountingParityTrace BuildPayrollAccountingParityTrace(PayrollAccountingParityTraceRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException("request");
            }

            var normalized = NormalizeSalaryRequest(request);
            var trace = new PayrollAccountingParityTrace
            {
                Request = normalized,
                SourceProject = "VB6 Kishny",
                SourceForm = "FrmEmpSalary5",
                SourceModule = "ModAccounts.AddNewDev",
                SafetyStatus = "Read-only accounting trace. It does not create Notes, DOUBLE_ENTREY_VOUCHERS, payments, SendTopost rows, or allocation rebuilds."
            };

            var sgn = normalized.Year.ToString() + normalized.Month.ToString();
            var noteType = request.NoteType.GetValueOrDefault(66);
            using (var connection = OpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
SELECT TOP (100)
    n.NoteID, n.NoteType, CONVERT(varchar(20), n.salary) AS salary, n.NoteDate, n.NoteSerial,
    ISNULL(n.Note_Value, 0) AS Note_Value, n.branch_no, n.Remark
FROM dbo.Notes n WITH (NOLOCK)
WHERE CONVERT(varchar(20), n.salary) = @Sgn
  AND n.NoteType = @NoteType
  AND (@BranchId IS NULL OR n.branch_no = @BranchId)
ORDER BY n.branch_no, n.NoteID;";
                command.Parameters.Add("@Sgn", SqlDbType.VarChar, 20).Value = sgn;
                command.Parameters.Add("@NoteType", SqlDbType.Int).Value = noteType;
                AddNullable(command, "@BranchId", SqlDbType.Int, normalized.BranchId);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var note = new PayrollAccountingNoteTrace
                        {
                            NoteId = ReadInt(reader, "NoteID"),
                            NoteType = ReadNullableInt(reader, "NoteType"),
                            Salary = ReadString(reader, "salary"),
                            NoteDate = ReadNullableDate(reader, "NoteDate"),
                            NoteSerial = ReadString(reader, "NoteSerial"),
                            NoteValue = ReadDecimal(reader, "Note_Value"),
                            BranchId = ReadNullableInt(reader, "branch_no"),
                            Remark = ReadString(reader, "Remark")
                        };
                        trace.Notes.Add(note);
                        trace.NotesTotal += note.NoteValue;
                    }
                }
            }

            if (trace.Notes.Count == 0)
            {
                return trace;
            }

            var noteCsv = string.Join(",", trace.Notes.Select(x => x.NoteId.ToString()).ToArray());
            using (var connection = OpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
SELECT TOP (1000)
    v.Double_Entry_Vouchers_ID, v.Notes_ID, v.DEV_ID_Line_No, v.Account_Code,
    ISNULL(v.Value, 0) AS Value, v.Credit_Or_Debit, v.branch_id, v.Departementid,
    v.NEmpid, v.project_id, v.Double_Entry_Vouchers_Description
FROM dbo.DOUBLE_ENTREY_VOUCHERS v WITH (NOLOCK)
WHERE v.Notes_ID IN (" + noteCsv + @")
  AND (@EmployeeId IS NULL OR v.NEmpid = @EmployeeId)
ORDER BY v.Notes_ID, v.DEV_ID_Line_No, v.Double_Entry_Vouchers_ID;";
                AddNullable(command, "@EmployeeId", SqlDbType.Int, normalized.EmployeeId);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var line = new PayrollAccountingVoucherTrace
                        {
                            VoucherId = ReadInt(reader, "Double_Entry_Vouchers_ID"),
                            NoteId = ReadNullableInt(reader, "Notes_ID"),
                            LineNo = ReadNullableInt(reader, "DEV_ID_Line_No"),
                            AccountCode = ReadString(reader, "Account_Code"),
                            Value = ReadDecimal(reader, "Value"),
                            IsCredit = ReadInt(reader, "Credit_Or_Debit") == 1,
                            BranchId = ReadNullableInt(reader, "branch_id"),
                            DepartmentId = ReadNullableInt(reader, "Departementid"),
                            EmployeeId = ReadNullableInt(reader, "NEmpid"),
                            ProjectId = ReadNullableInt(reader, "project_id"),
                            Description = ReadString(reader, "Double_Entry_Vouchers_Description")
                        };

                        trace.VoucherLines.Add(line);
                        if (line.IsCredit)
                        {
                            trace.VoucherCreditTotal += line.Value;
                        }
                        else
                        {
                            trace.VoucherDebitTotal += line.Value;
                        }
                    }
                }
            }

            trace.NotesCount = trace.Notes.Count;
            trace.VoucherLineCount = trace.VoucherLines.Count;
            trace.VoucherBalance = trace.VoucherDebitTotal - trace.VoucherCreditTotal;
            return trace;
        }

        public PayrollAccountingReplayReport BuildPayrollAccountingReplayReport(PayrollAccountingReplayRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException("request");
            }

            request.IncludeSavedDrafts = true;
            var preview = PreviewSalaryRun(request);
            var traceRequest = new PayrollAccountingParityTraceRequest
            {
                Year = request.Year,
                Month = request.Month,
                BranchId = request.BranchId,
                DepartmentId = request.DepartmentId,
                EmployeeId = request.EmployeeId,
                IncludeSavedDrafts = true,
                NoteType = request.NoteType
            };
            var legacy = BuildPayrollAccountingParityTrace(traceRequest);
            var report = new PayrollAccountingReplayReport
            {
                Request = preview.Request,
                SourceProject = "VB6 Kishny",
                SourceForm = "FrmEmpSalary5",
                SourceModule = "ModAccounts.AddNewDev",
                LegacyTrace = legacy,
                SafetyStatus = "Read-only deterministic replay. It only reconstructs the expected Notes and DOUBLE_ENTREY_VOUCHERS model in memory."
            };
            using (var connection = OpenConnection())
            {
                report.DistributionOptions = ReadDistributionOptions(connection);
                ReplayProjectDistribution(connection, request, report);
                ReplayProjectMofrdSalarDistribution(connection, request, report);
            }

            var noteType = request.NoteType.GetValueOrDefault(66);
            var sgn = request.Year.ToString() + request.Month.ToString();
            var replayRows = GetAccountingReplayRows(preview).ToList();
            var branchGroups = replayRows.GroupBy(x => x.BranchId);
            foreach (var branch in branchGroups)
            {
                var legacyNote = legacy.Notes.FirstOrDefault(x => x.BranchId == branch.Key);
                report.ReplayedNotes.Add(new PayrollReplayedNote
                {
                    LegacyNoteId = legacyNote != null ? (int?)legacyNote.NoteId : null,
                    NoteType = noteType,
                    Salary = sgn,
                    BranchId = branch.Key,
                    NoteValue = branch.Sum(x => x.TotalBeforeDeductions),
                    Rule = "Notes.NoteType=66; Notes.salary=Year+MonthIndex",
                    Explanation = "VB6 creates one salary accrual note per branch for the selected salary period before voucher lines are inserted by AddNewDev."
                });
            }

            var projectDistribution = report.DistributionOptions != null && report.DistributionOptions.ProjectEmployeeGV;
            ReplayComponentAdditionExpenses(replayRows, report);
            if (!projectDistribution)
            {
                foreach (var row in replayRows)
                {
                    ReplayEmployeeSalaryPayable(row, report);
                    ReplayEmployeeDeductions(row, report);
                    ReplayZmamComponents(row, report);
                    ReplayAdvanceDeductions(row, report);
                    ReplayVacationDeductions(row, report);
                    ReplayAdvancePaymentComponents(row, report);
                    ReplayInsurance(row, report);
                }
            }
            else
            {
                ReplayProjectDistributionComponentDeductions(replayRows, report);
                foreach (var row in replayRows)
                {
                    ReplayProjectEmployeeNetPayable(row, report);
                    ReplayProjectZmamComponents(row, report);
                    ReplayProjectAdvanceDeductions(row, report);
                    ReplayVacationDeductions(row, report);
                    ReplayAdvancePaymentComponents(row, report);
                    ReplayInsurance(row, report);
                }
            }

            AttachReplayLineEmployeeLabels(report, replayRows);
            report.ReplayedDebitTotal = report.ReplayedLines.Where(x => !x.IsCredit).Sum(x => x.Value);
            report.ReplayedCreditTotal = report.ReplayedLines.Where(x => x.IsCredit).Sum(x => x.Value);
            report.LegacyDebitTotal = legacy.VoucherDebitTotal;
            report.LegacyCreditTotal = legacy.VoucherCreditTotal;
            report.DebitDifference = report.ReplayedDebitTotal - report.LegacyDebitTotal;
            report.CreditDifference = report.ReplayedCreditTotal - report.LegacyCreditTotal;
            report.ReplayedBalance = report.ReplayedDebitTotal - report.ReplayedCreditTotal;
            report.LegacyBalance = legacy.VoucherBalance;
            BuildReplayComparisons(report);
            using (var connection = OpenConnection())
            {
                AnalyzeLegacyConsistency(connection, request, report);
            }
            ApplyLegacyConsistencyTrustModel(report);

            if (!request.IncludeLineDetails)
            {
                report.ReplayedLines = report.ReplayedLines.Take(250).ToList();
                report.LegacyTrace.VoucherLines = report.LegacyTrace.VoucherLines.Take(250).ToList();
                report.AccountComparisons = report.AccountComparisons.Take(120).ToList();
                report.BranchComparisons = report.BranchComparisons.Take(80).ToList();
                report.DepartmentComparisons = report.DepartmentComparisons.Take(80).ToList();
                report.ProjectComparisons = report.ProjectComparisons.Take(80).ToList();
                report.LegacyConsistencySummaries = report.LegacyConsistencySummaries.Take(120).ToList();
            }

            return report;
        }

        public PayrollTestPostingResult BuildPayrollTestPostingDryRun(PayrollTestPostingRequest request)
        {
            request = request ?? new PayrollTestPostingRequest();
            request.IncludeLineDetails = true;
            var report = BuildPayrollAccountingReplayReport(request);
            var result = BuildTestPostingSummary(report, true);
            result.Message = "Dry-run only. No Notes or DOUBLE_ENTREY_VOUCHERS rows were created.";
            result.SafetyStatus = "Protected test posting preview. Production posting remains disabled.";
            return result;
        }

        public PayrollPostingResult BuildPayrollPostingDryRun(PayrollPostingRequest request)
        {
            request = request ?? new PayrollPostingRequest();
            request.IncludeLineDetails = true;
            var report = BuildPayrollAccountingReplayReport(request);
            var result = BuildPostingSummary(report, true);
            result.Message = result.AlreadyPosted
                ? "تم ترحيل قيد استحقاق الرواتب لهذه الفترة من قبل."
                : "معاينة فقط. لم يتم إنشاء أي قيد محاسبي.";
            return result;
        }

        public PayrollPostingResult PostPayrollJournal(PayrollPostingRequest request, int userId, string userName)
        {
            request = request ?? new PayrollPostingRequest();
            request.IncludeLineDetails = true;
            if (ExistingPayrollPostingExists(request))
            {
                var existing = BuildPayrollPostingDryRun(request);
                existing.IsDryRun = false;
                existing.Message = "تم ترحيل قيد استحقاق الرواتب لهذه الفترة من قبل، ولم يتم إنشاء قيد مكرر.";
                return existing;
            }

            var report = BuildPayrollAccountingReplayReport(request);
            var result = BuildPostingSummary(report, false);
            result.PayrollRunId = request.PayrollRunId;
            if (result.AlreadyPosted)
            {
                result.Message = "تم ترحيل قيد استحقاق الرواتب لهذه الفترة من قبل، ولم يتم إنشاء قيد مكرر.";
                return result;
            }

            ValidatePayrollPosting(report, result);
            if (request.SaveSalaryRunBeforePosting)
            {
                SaveSalaryRun(request, userId);
                report = BuildPayrollAccountingReplayReport(request);
                result = BuildPostingSummary(report, false);
                if (result.AlreadyPosted)
                {
                    result.Message = "تم ترحيل قيد استحقاق الرواتب لهذه الفترة من قبل، ولم يتم إنشاء قيد مكرر.";
                    return result;
                }

                ValidatePayrollPosting(report, result);
            }

            using (var connection = OpenConnection())
            using (var transaction = connection.BeginTransaction(IsolationLevel.Serializable))
            {
                try
                {
                    if (FindExistingPayrollPosting(connection, transaction, request) > 0)
                    {
                        transaction.Rollback();
                        result.AlreadyPosted = true;
                        result.Message = "تم ترحيل قيد استحقاق الرواتب لهذه الفترة من قبل، ولم يتم إنشاء قيد مكرر.";
                        return result;
                    }

                    var expectedLines = (report.ReplayedLines ?? new List<PayrollReplayedVoucherLine>())
                        .Count(x => !string.IsNullOrWhiteSpace(x.AccountCode) && x.Value != 0);
                    var noteIdByBranch = InsertPayrollPostingNotes(connection, transaction, report, request, userId, userName);
                    var insertedLines = InsertPayrollPostingVoucherLines(connection, transaction, report, noteIdByBranch, userId, request);
                    if (insertedLines != expectedLines)
                    {
                        throw new InvalidOperationException("تعذر ترحيل كل سطور قيد الرواتب. المتوقع " + expectedLines.ToString() + " وتم إنشاء " + insertedLines.ToString() + " فقط.");
                    }

                    var postedRows = MarkSalaryRowsPosted(connection, transaction, request);
                    var postedAdvanceInstallments = MarkPayrollAdvanceInstallmentsPosted(connection, transaction, request, userId, noteIdByBranch.Values.FirstOrDefault());
                    LinkPayrollRunPosting(connection, transaction, request, userId, noteIdByBranch.Values.ToList(), insertedLines, result.DebitTotal, result.CreditTotal);
                    transaction.Commit();

                    result.IsPosted = true;
                    result.NoteIds = noteIdByBranch.Values.OrderBy(x => x).ToList();
                    result.NoteId = result.NoteIds.Count > 0 ? (int?)result.NoteIds[0] : null;
                    result.NoteSerial = result.NoteId;
                    result.VoucherLinesCount = insertedLines;
                    result.SalaryRowsMarkedPosted = postedRows;
                    result.NotesCount = result.NoteIds.Count;
                    result.Message = "تم ترحيل قيد استحقاق الرواتب بنجاح. تم تعليم " + postedAdvanceInstallments.ToString() + " قسط سلفة كمخصوم من المسير.";
                }
                catch
                {
                    try
                    {
                        transaction.Rollback();
                    }
                    catch
                    {
                    }

                    throw;
                }
            }

            return result;
        }

        public PayrollTestPostingResult GeneratePayrollTestPosting(PayrollTestPostingRequest request, int userId, string userName)
        {
            request = request ?? new PayrollTestPostingRequest();
            ValidateTestPostingSecret(request.Password, request.ConfirmationPhrase);
            if (!IsPayrollTestPostingAllowedDatabase())
            {
                throw new InvalidOperationException("Test posting is not allowed for database '" + GetDatabaseName() + "'.");
            }

            request.IncludeLineDetails = true;
            var report = BuildPayrollAccountingReplayReport(request);
            var result = BuildTestPostingSummary(report, false);

            var batchId = Guid.NewGuid();
            var marker = "[TEST_PAYROLL_POSTING] Batch=" + batchId;
            using (var connection = OpenConnection())
            using (var transaction = connection.BeginTransaction(IsolationLevel.Serializable))
            {
                EnsurePayrollTestPostingAuditTable(connection, transaction);
                var noteIdByBranch = InsertTestPostingNotes(connection, transaction, report, request, userId, userName, marker);
                var insertedLines = InsertTestPostingVoucherLines(connection, transaction, report, noteIdByBranch, userId, marker);
                InsertTestPostingAudit(connection, transaction, request, batchId, userId, userName, result, insertedLines);
                transaction.Commit();
                result.TestPostingBatchId = batchId;
                result.IsGenerated = true;
                result.VoucherLinesCount = insertedLines;
                result.NotesCount = noteIdByBranch.Count;
                result.Message = "Protected test posting generated successfully. Rows are marked " + marker + ".";
                result.SafetyStatus = "Generated in an allowlisted test database only. Production posting remains disabled.";
            }

            return result;
        }

        public PayrollTestPostingResult CleanupPayrollTestPosting(PayrollTestPostingCleanupRequest request, int userId)
        {
            if (request == null || request.TestPostingBatchId == Guid.Empty)
            {
                throw new InvalidOperationException("A valid TestPostingBatchId is required.");
            }

            ValidateTestPostingSecret(request.Password, request.ConfirmationPhrase);
            var result = new PayrollTestPostingResult
            {
                DatabaseName = GetDatabaseName(),
                IsAllowedDatabase = IsPayrollTestPostingAllowedDatabase(),
                TestPostingBatchId = request.TestPostingBatchId,
                SafetyStatus = "Batch cleanup by protected test posting marker only."
            };
            if (!result.IsAllowedDatabase)
            {
                throw new InvalidOperationException("Test posting cleanup is not allowed for database '" + result.DatabaseName + "'.");
            }

            using (var connection = OpenConnection())
            using (var transaction = connection.BeginTransaction(IsolationLevel.Serializable))
            {
                EnsurePayrollTestPostingAuditTable(connection, transaction);
                using (var command = CreateCommand(connection, transaction, @"
SELECT COUNT(1)
FROM dbo.MainErpPayrollTestPostingAudit WITH (UPDLOCK, HOLDLOCK)
WHERE TestPostingBatchId = @BatchId AND CleanupStatus = N'Active';"))
                {
                    command.Parameters.Add("@BatchId", SqlDbType.UniqueIdentifier).Value = request.TestPostingBatchId;
                    if (Convert.ToInt32(command.ExecuteScalar()) == 0)
                    {
                        throw new InvalidOperationException("The test posting batch was not found or was already cleaned.");
                    }
                }

                using (var command = CreateCommand(connection, transaction, @"
SELECT COUNT(1)
FROM dbo.DOUBLE_ENTREY_VOUCHERS d
INNER JOIN dbo.Notes n ON n.NoteID = d.Notes_ID
WHERE CHARINDEX(@Marker, n.Remark) = 1
  AND (CHARINDEX(@Marker, d.Double_Entry_Vouchers_Description) = 1 OR CHARINDEX(@Marker, d.des) = 1);"))
                {
                    command.Parameters.Add("@Marker", SqlDbType.NVarChar, 200).Value = "[TEST_PAYROLL_POSTING] Batch=" + request.TestPostingBatchId;
                    result.CleanedVoucherLinesCount = Convert.ToInt32(command.ExecuteScalar());
                }

                using (var command = CreateCommand(connection, transaction, @"
SELECT COUNT(1)
FROM dbo.Notes
WHERE CHARINDEX(@Marker, Remark) = 1;"))
                {
                    command.Parameters.Add("@Marker", SqlDbType.NVarChar, 200).Value = "[TEST_PAYROLL_POSTING] Batch=" + request.TestPostingBatchId;
                    result.CleanedNotesCount = Convert.ToInt32(command.ExecuteScalar());
                }

                using (var command = CreateCommand(connection, transaction, @"
DELETE d
FROM dbo.DOUBLE_ENTREY_VOUCHERS d
INNER JOIN dbo.Notes n ON n.NoteID = d.Notes_ID
WHERE CHARINDEX(@Marker, n.Remark) = 1
  AND (CHARINDEX(@Marker, d.Double_Entry_Vouchers_Description) = 1 OR CHARINDEX(@Marker, d.des) = 1);

DELETE FROM dbo.Notes
WHERE CHARINDEX(@Marker, Remark) = 1;

UPDATE dbo.MainErpPayrollTestPostingAudit
SET CleanupStatus = N'Cleaned',
    CleanedAt = GETDATE(),
    CleanedBy = @UserId,
    CleanedNotesCount = @CleanedNotesCount,
    CleanedVoucherLinesCount = @CleanedVoucherLinesCount
WHERE TestPostingBatchId = @BatchId;"))
                {
                    command.Parameters.Add("@Marker", SqlDbType.NVarChar, 200).Value = "[TEST_PAYROLL_POSTING] Batch=" + request.TestPostingBatchId;
                    command.Parameters.Add("@BatchId", SqlDbType.UniqueIdentifier).Value = request.TestPostingBatchId;
                    command.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
                    command.Parameters.Add("@CleanedNotesCount", SqlDbType.Int).Value = result.CleanedNotesCount;
                    command.Parameters.Add("@CleanedVoucherLinesCount", SqlDbType.Int).Value = result.CleanedVoucherLinesCount;
                    command.ExecuteNonQuery();
                }

                transaction.Commit();
            }

            result.IsCleaned = true;
            result.Message = "Protected test posting batch was cleaned by batch id only.";
            return result;
        }

        private PayrollTestPostingResult BuildTestPostingSummary(PayrollAccountingReplayReport report, bool isDryRun)
        {
            var lines = (report.ReplayedLines ?? new List<PayrollReplayedVoucherLine>())
                .Where(x => !string.IsNullOrWhiteSpace(x.AccountCode) && x.Value != 0)
                .ToList();
            var result = new PayrollTestPostingResult
            {
                IsDryRun = isDryRun,
                DatabaseName = GetDatabaseName(),
                IsAllowedDatabase = IsPayrollTestPostingAllowedDatabase(),
                NotesCount = lines.GroupBy(x => BranchKey(x.BranchId)).Count(),
                VoucherLinesCount = lines.Count,
                DebitTotal = lines.Where(x => !x.IsCredit).Sum(x => x.Value),
                CreditTotal = lines.Where(x => x.IsCredit).Sum(x => x.Value)
            };
            result.Balance = result.DebitTotal - result.CreditTotal;
            result.AffectedAccounts = BuildDimensionTotals(lines, x => x.AccountCode).Take(25).ToList();
            EnrichAffectedAccountTotals(result.AffectedAccounts);
            result.AffectedBranches = BuildDimensionTotals(lines, x => x.BranchId.HasValue ? x.BranchId.Value.ToString() : "Unspecified").Take(25).ToList();
            result.AffectedProjects = BuildDimensionTotals(lines, x => x.ProjectId.HasValue ? x.ProjectId.Value.ToString() : "Unspecified").Take(25).ToList();
            result.AffectedDepartments = BuildDimensionTotals(lines, x => x.DepartmentId.HasValue ? x.DepartmentId.Value.ToString() : "Unspecified").Take(25).ToList();
            if (!result.IsAllowedDatabase)
            {
                result.Warnings.Add("The current database is not allowlisted for protected test posting.");
            }

            if (Math.Abs(result.Balance) > 0.01m)
            {
                result.Warnings.Add("The replay is not perfectly balanced. This is allowed only for test visibility and must be reviewed before production posting.");
            }

            if ((report.Request == null) || report.Request.Year < 2000 || report.Request.Month < 1)
            {
                result.Warnings.Add("Payroll period is not valid.");
            }

            return result;
        }

        private PayrollPostingResult BuildPostingSummary(PayrollAccountingReplayReport report, bool isDryRun)
        {
            var allLines = (report.ReplayedLines ?? new List<PayrollReplayedVoucherLine>())
                .Where(x => x.Value != 0)
                .ToList();
            var lines = allLines.Where(x => !string.IsNullOrWhiteSpace(x.AccountCode)).ToList();
            var result = new PayrollPostingResult
            {
                IsDryRun = isDryRun,
                PayrollRunId = report.Request == null ? null : report.Request.PayrollRunId,
                DatabaseName = GetDatabaseName(),
                NotesCount = allLines.Count == 0 ? 0 : 1,
                VoucherLinesCount = lines.Count,
                DebitTotal = allLines.Where(x => !x.IsCredit).Sum(x => x.Value),
                CreditTotal = allLines.Where(x => x.IsCredit).Sum(x => x.Value)
            };
            result.Balance = result.DebitTotal - result.CreditTotal;
            result.AffectedAccounts = BuildDimensionTotals(lines, x => x.AccountCode).Take(25).ToList();
            EnrichAffectedAccountTotals(result.AffectedAccounts);
            result.AffectedBranches = BuildDimensionTotals(lines, x => x.BranchId.HasValue ? x.BranchId.Value.ToString() : "Unspecified").Take(25).ToList();
            result.AffectedProjects = BuildDimensionTotals(lines, x => x.ProjectId.HasValue ? x.ProjectId.Value.ToString() : "Unspecified").Take(25).ToList();
            result.AffectedDepartments = BuildDimensionTotals(lines, x => x.DepartmentId.HasValue ? x.DepartmentId.Value.ToString() : "Unspecified").Take(25).ToList();
            result.AlreadyPosted = ExistingPayrollPostingExists(report.Request as PayrollPostingRequest ?? ToPostingRequest(report.Request));
            result.AccountIssues = BuildPayrollPostingAccountIssues(report);
            if (Math.Abs(result.Balance) > 0.01m)
            {
                result.Warnings.Add("القيد غير متوازن ولا يمكن ترحيله.");
            }

            if (allLines.Count == 0)
            {
                result.Warnings.Add("لا توجد سطور محاسبية صالحة للترحيل.");
            }

            foreach (var issue in result.AccountIssues.Take(10))
            {
                result.Warnings.Add(issue.ArabicMessage);
            }

            return result;
        }

        private static PayrollPostingRequest ToPostingRequest(SalaryRunRequest request)
        {
            request = request ?? new SalaryRunRequest();
            return new PayrollPostingRequest
            {
                PayrollRunId = request.PayrollRunId,
                RunName = request.RunName,
                Year = request.Year,
                Month = request.Month,
                BranchId = request.BranchId,
                DepartmentId = request.DepartmentId,
                EmployeeId = request.EmployeeId,
                PostingStatus = request.PostingStatus,
                IncludeSavedDrafts = request.IncludeSavedDrafts,
                RebuildEmployees = request.RebuildEmployees,
                ExcludeAlreadyIncluded = request.ExcludeAlreadyIncluded,
                OnlyUnincluded = request.OnlyUnincluded,
                AllowDuplicateEmployees = request.AllowDuplicateEmployees,
                ManualEmployeeIds = request.ManualEmployeeIds,
                RowLimit = request.RowLimit,
                JournalPreviewLimit = request.JournalPreviewLimit,
                IncludeLineDetails = true
            };
        }

        private void ValidatePayrollPosting(PayrollAccountingReplayReport report, PayrollPostingResult result)
        {
            var lines = (report.ReplayedLines ?? new List<PayrollReplayedVoucherLine>())
                .Where(x => x.Value != 0)
                .ToList();
            if (lines.Count == 0)
            {
                throw new InvalidOperationException("لا توجد سطور محاسبية صالحة لترحيل مسير الرواتب.");
            }

            var accountIssues = result.AccountIssues != null && result.AccountIssues.Count > 0
                ? result.AccountIssues.ToList()
                : BuildPayrollPostingAccountIssues(report);
            if (accountIssues.Any())
            {
                throw new InvalidOperationException(BuildPostingAccountIssueError(accountIssues));
            }

            if (Math.Abs(result.Balance) > 0.01m)
            {
                throw new InvalidOperationException("قيد الرواتب غير متوازن. إجمالي المدين " + result.DebitTotal.ToString("0.00") + " وإجمالي الدائن " + result.CreditTotal.ToString("0.00") + ".");
            }
        }

        private IList<PayrollPostingAccountIssue> BuildPayrollPostingAccountIssues(PayrollAccountingReplayReport report)
        {
            var lines = (report == null || report.ReplayedLines == null)
                ? new List<PayrollReplayedVoucherLine>()
                : report.ReplayedLines.Where(x => x.Value != 0).ToList();
            var issues = new List<PayrollPostingAccountIssue>();
            foreach (var line in lines.Where(x => string.IsNullOrWhiteSpace(x.AccountCode)))
            {
                issues.Add(BuildPostingAccountIssue(line, "MissingConfiguredAccount"));
            }

            var accountCodes = lines
                .Where(x => !string.IsNullOrWhiteSpace(x.AccountCode))
                .Select(x => x.AccountCode.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (accountCodes.Count == 0)
            {
                foreach (var issue in issues)
                {
                    issue.ArabicMessage = FormatPostingAccountIssue(issue);
                }

                return issues;
            }

            var found = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            using (var connection = OpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT Account_Code FROM dbo.ACCOUNTS WITH (NOLOCK) WHERE Account_Code IN (" + BuildInListParameters(command, "@Account", accountCodes) + ");";
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        found.Add(ReadString(reader, "Account_Code"));
                    }
                }
            }

            foreach (var line in lines.Where(x => !string.IsNullOrWhiteSpace(x.AccountCode) && !found.Contains(x.AccountCode.Trim())))
            {
                issues.Add(BuildPostingAccountIssue(line, "AccountNotFound"));
            }

            foreach (var issue in issues)
            {
                issue.ArabicMessage = FormatPostingAccountIssue(issue);
            }

            return issues;
        }

        private static PayrollPostingAccountIssue BuildPostingAccountIssue(PayrollReplayedVoucherLine line, string issueType)
        {
            return new PayrollPostingAccountIssue
            {
                IssueType = issueType,
                AccountCode = line == null ? string.Empty : (line.AccountCode ?? string.Empty).Trim(),
                AccountSource = line == null ? string.Empty : FirstNonEmpty(line.AccountRoutingPath, line.RuleId),
                Direction = line != null && line.IsCredit ? "Credit" : "Debit",
                Amount = line == null ? 0 : line.Value,
                EmployeeId = line == null ? null : line.EmployeeId,
                EmployeeCode = line == null ? string.Empty : line.EmployeeCode,
                EmployeeName = line == null ? string.Empty : line.EmployeeName,
                ComponentNo = line == null ? null : line.ComponentNo,
                ComponentName = line == null ? string.Empty : line.ComponentName,
                RuleId = line == null ? string.Empty : line.RuleId
            };
        }

        private static string BuildPostingAccountIssueError(IList<PayrollPostingAccountIssue> issues)
        {
            var messages = issues.Take(25).Select(FormatPostingAccountIssue).ToList();
            var suffix = issues.Count > messages.Count ? " يوجد " + (issues.Count - messages.Count).ToString() + " بند آخر غير معروض." : string.Empty;
            return "لا يمكن ترحيل قيد الرواتب قبل استكمال الحسابات التالية: " + string.Join(" | ", messages.ToArray()) + suffix;
        }

        private static string FormatPostingAccountIssue(PayrollPostingAccountIssue issue)
        {
            if (issue == null)
            {
                return string.Empty;
            }

            var employee = FirstNonEmpty(
                (FirstNonEmpty(issue.EmployeeCode, issue.EmployeeId.HasValue ? issue.EmployeeId.Value.ToString() : string.Empty) + " " + FirstNonEmpty(issue.EmployeeName, string.Empty)).Trim(),
                "بدون موظف محدد");
            var component = FirstNonEmpty(FirstNonEmpty(issue.ComponentName, issue.RuleId), "إجمالي الراتب");
            var source = FirstNonEmpty(issue.AccountSource, "مصدر الحساب غير محدد");
            var direction = string.Equals(issue.Direction, "Credit", StringComparison.OrdinalIgnoreCase) ? "دائن" : "مدين";
            if (string.Equals(issue.IssueType, "AccountNotFound", StringComparison.OrdinalIgnoreCase))
            {
                return "الموظف: " + employee + "، البند: " + component + "، الحساب غير موجود في الدليل: " + issue.AccountCode + "، المصدر: " + source + "، الاتجاه: " + direction + "، المبلغ: " + issue.Amount.ToString("0.00");
            }

            return "الموظف: " + employee + "، البند: " + component + "، الحساب غير محدد، المصدر: " + source + "، الاتجاه: " + direction + "، المبلغ: " + issue.Amount.ToString("0.00");
        }

        private bool ExistingPayrollPostingExists(PayrollPostingRequest request)
        {
            using (var connection = OpenConnection())
            {
                using (var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted))
                {
                    return FindExistingPayrollPosting(connection, transaction, request) > 0;
                }
            }
        }

        private static int FindExistingPayrollPosting(SqlConnection connection, SqlTransaction transaction, SalaryRunRequest request)
        {
            if (request != null && request.PayrollRunId.HasValue && request.PayrollRunId.Value > 0
                && TableExists(connection, transaction, "PayrollRunJournalLinks"))
            {
                using (var linked = CreateCommand(connection, transaction, @"
SELECT COUNT(1)
FROM dbo.PayrollRunJournalLinks l WITH (UPDLOCK, HOLDLOCK)
INNER JOIN dbo.Notes n WITH (NOLOCK) ON n.NoteID = l.NoteId
WHERE l.PayrollRunId = @PayrollRunId
  AND n.NoteType = 66;"))
                {
                    linked.Parameters.Add("@PayrollRunId", SqlDbType.Int).Value = request.PayrollRunId.Value;
                    return Convert.ToInt32(linked.ExecuteScalar() ?? 0);
                }
            }

            using (var command = CreateCommand(connection, transaction, @"
SELECT COUNT(1)
FROM dbo.Notes WITH (UPDLOCK, HOLDLOCK)
WHERE NoteType = 66
  AND salary = @Salary
  AND (@BranchId IS NULL OR branch_no = @BranchId OR EXISTS
      (SELECT 1 FROM dbo.DOUBLE_ENTREY_VOUCHERS d WITH (NOLOCK)
       WHERE d.Notes_ID = Notes.NoteID AND d.branch_id = @BranchId))
  AND (@EmployeeId IS NULL OR EXISTS
      (SELECT 1 FROM dbo.DOUBLE_ENTREY_VOUCHERS d WITH (NOLOCK)
       WHERE d.Notes_ID = Notes.NoteID AND d.NEmpid = @EmployeeId));"))
            {
                command.Parameters.Add("@Salary", SqlDbType.Int).Value = SafeSalaryPeriodCode(request);
                AddNullable(command, "@BranchId", SqlDbType.Int, request == null ? null : request.BranchId);
                AddNullable(command, "@EmployeeId", SqlDbType.Float, request == null ? null : request.EmployeeId);
                return Convert.ToInt32(command.ExecuteScalar() ?? 0);
            }
        }

        private Dictionary<string, int> InsertPayrollPostingNotes(SqlConnection connection, SqlTransaction transaction, PayrollAccountingReplayReport report, PayrollPostingRequest request, int userId, string userName)
        {
            var periodEnd = new DateTime(request.Year, request.Month, 1).AddMonths(1).AddDays(-1);
            var notesByBranch = new Dictionary<string, int>();
            var linesByBranch = (report.ReplayedLines ?? new List<PayrollReplayedVoucherLine>())
                .Where(x => !string.IsNullOrWhiteSpace(x.AccountCode) && x.Value != 0)
                .GroupBy(x => BranchKey(x.BranchId))
                .ToList();
            foreach (var branchGroup in linesByBranch)
            {
                var noteId = NextId(connection, transaction, "Notes", "NoteID");
                var replayedNote = report.ReplayedNotes.FirstOrDefault(x => BranchKey(x.BranchId) == branchGroup.Key);
                var debit = branchGroup.Where(x => !x.IsCredit).Sum(x => x.Value);
                var noteValue = replayedNote != null && replayedNote.NoteValue > 0 ? replayedNote.NoteValue : debit;
                using (var command = CreateCommand(connection, transaction, @"
INSERT INTO dbo.Notes
(
    NoteID, NoteDate, NoteType, NoteSerial, NoteSerial1, Note_Value, UserID, Remark,
    branch_no, user_name, salary, PayrollMonth, PayrollYear, DateTimeEntry, NotePosted
)
VALUES
(
    @NoteID, @NoteDate, 66, @NoteSerial, @NoteSerial1, @NoteValue, @UserID, @Remark,
    @BranchId, @UserName, @Salary, @PayrollMonth, @PayrollYear, GETDATE(), 0
);"))
                {
                    command.Parameters.Add("@NoteID", SqlDbType.Int).Value = noteId;
                    command.Parameters.Add("@NoteDate", SqlDbType.DateTime).Value = periodEnd;
                    command.Parameters.Add("@NoteSerial", SqlDbType.Float).Value = noteId;
                    command.Parameters.Add("@NoteSerial1", SqlDbType.Float).Value = noteId;
                    command.Parameters.Add("@NoteValue", SqlDbType.Float).Value = Convert.ToDouble(noteValue);
                    command.Parameters.Add("@UserID", SqlDbType.Int).Value = userId;
                    command.Parameters.Add("@Remark", SqlDbType.NVarChar, 4000).Value = "قيد استحقاق رواتب الموظفين عن شهر " + request.Month + " سنة " + request.Year + " - POS Web";
                    AddNullable(command, "@BranchId", SqlDbType.Int, ParseNullableInt(branchGroup.Key));
                    command.Parameters.Add("@UserName", SqlDbType.NVarChar, 50).Value = (object)(userName ?? "POS Web") ?? DBNull.Value;
                    command.Parameters.Add("@Salary", SqlDbType.Int).Value = SafeSalaryPeriodCode(request);
                    command.Parameters.Add("@PayrollMonth", SqlDbType.Int).Value = request.Month;
                    command.Parameters.Add("@PayrollYear", SqlDbType.Int).Value = request.Year;
                    command.ExecuteNonQuery();
                }

                notesByBranch[branchGroup.Key] = noteId;
            }

            return notesByBranch;
        }

        private int InsertPayrollPostingVoucherLines(SqlConnection connection, SqlTransaction transaction, PayrollAccountingReplayReport report, IDictionary<string, int> noteIdByBranch, int userId, PayrollPostingRequest request)
        {
            var inserted = 0;
            var nextVoucherId = NextId(connection, transaction, "DOUBLE_ENTREY_VOUCHERS", "Double_Entry_Vouchers_ID");
            var lineNoByNote = new Dictionary<int, int>();
            var periodEnd = new DateTime(request.Year, request.Month, 1).AddMonths(1).AddDays(-1);
            foreach (var line in (report.ReplayedLines ?? new List<PayrollReplayedVoucherLine>()).Where(x => !string.IsNullOrWhiteSpace(x.AccountCode) && x.Value != 0))
            {
                var branchKey = BranchKey(line.BranchId);
                int noteId;
                if (!noteIdByBranch.TryGetValue(branchKey, out noteId))
                {
                    continue;
                }

                var lineNo = lineNoByNote.ContainsKey(noteId) ? lineNoByNote[noteId] + 1 : 1;
                lineNoByNote[noteId] = lineNo;
                var description = "قيد استحقاق رواتب الموظفين عن شهر " + request.Month + " سنة " + request.Year + " - " + FirstNonEmpty(FirstNonEmpty(line.ComponentName, line.RuleId), "POS Web");
                if (description.Length > 3800)
                {
                    description = description.Substring(0, 3800);
                }

                using (var command = CreateCommand(connection, transaction, @"
INSERT INTO dbo.DOUBLE_ENTREY_VOUCHERS
(
    Double_Entry_Vouchers_ID, DEV_ID_Line_No, Account_Code, Value, Credit_Or_Debit,
    Double_Entry_Vouchers_Description, RecordDate, Notes_ID, UserID, Posted,
    branch_id, project_id, Departementid, NEmpid, depet_value, credit_value, des
)
VALUES
(
    @VoucherId, @LineNo, @AccountCode, @Value, @CreditOrDebit,
    @Description, @RecordDate, @NoteId, @UserId, 0,
    @BranchId, @ProjectId, @DepartmentId, @EmployeeId, @DebitValue, @CreditValue, @ShortDescription
);"))
                {
                    command.Parameters.Add("@VoucherId", SqlDbType.Int).Value = nextVoucherId++;
                    command.Parameters.Add("@LineNo", SqlDbType.Int).Value = lineNo;
                    command.Parameters.Add("@AccountCode", SqlDbType.NVarChar, 50).Value = line.AccountCode;
                    command.Parameters.Add("@Value", SqlDbType.Money).Value = line.Value;
                    command.Parameters.Add("@CreditOrDebit", SqlDbType.SmallInt).Value = line.IsCredit ? 1 : 0;
                    command.Parameters.Add("@Description", SqlDbType.NVarChar, 4000).Value = description;
                    command.Parameters.Add("@RecordDate", SqlDbType.DateTime).Value = periodEnd;
                    command.Parameters.Add("@NoteId", SqlDbType.Int).Value = noteId;
                    command.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
                    AddNullable(command, "@BranchId", SqlDbType.Int, line.BranchId);
                    AddNullable(command, "@ProjectId", SqlDbType.Int, line.ProjectId);
                    AddNullable(command, "@DepartmentId", SqlDbType.Float, line.DepartmentId);
                    AddNullable(command, "@EmployeeId", SqlDbType.Float, line.EmployeeId);
                    command.Parameters.Add("@DebitValue", SqlDbType.Money).Value = line.IsCredit ? 0 : line.Value;
                    command.Parameters.Add("@CreditValue", SqlDbType.Money).Value = line.IsCredit ? line.Value : 0;
                    command.Parameters.Add("@ShortDescription", SqlDbType.NVarChar, 255).Value = description.Length > 255 ? description.Substring(0, 255) : description;
                    command.ExecuteNonQuery();
                    inserted++;
                }
            }

            return inserted;
        }

        private static int MarkSalaryRowsPosted(SqlConnection connection, SqlTransaction transaction, SalaryRunRequest request)
        {
            if (request != null && request.PayrollRunId.HasValue && request.PayrollRunId.Value > 0 && TableExists(connection, transaction, "PayrollRunEmployees"))
            {
                using (var command = CreateCommand(connection, transaction, @"
UPDATE s
SET payed = 1
FROM dbo.emp_salary s
INNER JOIN dbo.PayrollRunEmployees r ON r.EmployeeId = s.emp_id AND r.PayrollRunId = @PayrollRunId
WHERE s.sgn = @Sgn;"))
                {
                    command.Parameters.Add("@PayrollRunId", SqlDbType.Int).Value = request.PayrollRunId.Value;
                    command.Parameters.Add("@Sgn", SqlDbType.VarChar, 20).Value = request.Year.ToString() + request.Month.ToString();
                    return command.ExecuteNonQuery();
                }
            }

            using (var command = CreateCommand(connection, transaction, @"
UPDATE dbo.emp_salary
SET payed = 1
WHERE sgn = @Sgn
  AND (@BranchId IS NULL OR BranchId = @BranchId)
  AND (@DepartmentId IS NULL OR DepartmentID = @DepartmentId)
  AND (@EmployeeId IS NULL OR emp_id = @EmployeeId);"))
            {
                command.Parameters.Add("@Sgn", SqlDbType.VarChar, 20).Value = request.Year.ToString() + request.Month.ToString();
                AddNullable(command, "@BranchId", SqlDbType.Int, request.BranchId);
                AddNullable(command, "@DepartmentId", SqlDbType.Int, request.DepartmentId);
                AddNullable(command, "@EmployeeId", SqlDbType.Int, request.EmployeeId);
                return command.ExecuteNonQuery();
            }
        }

        private static int MarkPayrollAdvanceInstallmentsPosted(SqlConnection connection, SqlTransaction transaction, PayrollPostingRequest request, int userId, int noteId)
        {
            if (request == null || !request.PayrollRunId.HasValue || request.PayrollRunId.Value <= 0
                || !TableExists(connection, transaction, "PayrollRunAdvanceDeductions")
                || !TableExists(connection, transaction, "TblEmpAdvanceDetails"))
            {
                return 0;
            }

            using (var command = CreateCommand(connection, transaction, @"
IF EXISTS (
    SELECT 1
    FROM dbo.PayrollRunAdvanceDeductions l WITH (UPDLOCK, HOLDLOCK)
    INNER JOIN dbo.TblEmpAdvanceDetails d WITH (UPDLOCK, HOLDLOCK) ON l.AdvanceDetailTableId = d.TableID
    WHERE l.PayrollRunId = @PayrollRunId
      AND ISNULL(l.IsPosted, 0) = 0
      AND (
          ISNULL(d.Payed, 0) = 1
          OR d.Payed1 IS NOT NULL
          OR (d.StutsID IS NOT NULL AND d.StutsID NOT IN (21, 22, 23, 666))
      )
)
BEGIN
    RAISERROR(N'يوجد قسط سلفة في هذا المسير تم استهلاكه أو تغيير حالته خارج المسير. أعد احتساب المسير قبل الترحيل.', 16, 1);
    RETURN;
END;

UPDATE d
SET Payed = 1,
    StutsID = 555,
    Remark = LEFT(LTRIM(RTRIM(ISNULL(d.Remark, N'') + N' ' + N'خصم من مسير الرواتب رقم ' + CONVERT(nvarchar(20), @PayrollRunId))), 4000)
FROM dbo.TblEmpAdvanceDetails d
INNER JOIN dbo.PayrollRunAdvanceDeductions l WITH (UPDLOCK, HOLDLOCK)
    ON l.AdvanceDetailTableId = d.TableID
WHERE l.PayrollRunId = @PayrollRunId
  AND ISNULL(l.IsPosted, 0) = 0
  AND (d.Payed IS NULL OR d.Payed <> 1)
  AND d.Payed1 IS NULL
  AND (d.StutsID IS NULL OR d.StutsID IN (21, 22, 23, 666));

DECLARE @Rows int;
SET @Rows = @@ROWCOUNT;

UPDATE dbo.PayrollRunAdvanceDeductions
SET IsPosted = 1,
    PostedAt = GETDATE(),
    PostedBy = @UserId,
    NoteId = @NoteId
WHERE PayrollRunId = @PayrollRunId
  AND ISNULL(IsPosted, 0) = 0;

SELECT @Rows;"))
            {
                command.Parameters.Add("@PayrollRunId", SqlDbType.Int).Value = request.PayrollRunId.Value;
                command.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
                AddNullable(command, "@NoteId", SqlDbType.Int, noteId > 0 ? (object)noteId : null);
                return Convert.ToInt32(command.ExecuteScalar());
            }
        }

        private static void LinkPayrollRunPosting(SqlConnection connection, SqlTransaction transaction, PayrollPostingRequest request, int userId, IList<int> noteIds, int voucherLinesCount, decimal debitTotal, decimal creditTotal)
        {
            if (request == null || !request.PayrollRunId.HasValue || request.PayrollRunId.Value <= 0
                || !TableExists(connection, transaction, "PayrollRunHeader")
                || !TableExists(connection, transaction, "PayrollRunEmployees")
                || !TableExists(connection, transaction, "PayrollRunJournalLinks"))
            {
                return;
            }

            foreach (var noteId in noteIds.Distinct())
            {
                using (var link = CreateCommand(connection, transaction, @"
IF NOT EXISTS (SELECT 1 FROM dbo.PayrollRunJournalLinks WHERE PayrollRunId = @PayrollRunId AND NoteId = @NoteId)
BEGIN
    INSERT INTO dbo.PayrollRunJournalLinks
    (PayrollRunId, NoteId, NoteSerial, VoucherLinesCount, DebitTotal, CreditTotal, CreatedBy)
    VALUES
    (@PayrollRunId, @NoteId, @NoteId, @VoucherLinesCount, @DebitTotal, @CreditTotal, @UserId);
END"))
                {
                    link.Parameters.Add("@PayrollRunId", SqlDbType.Int).Value = request.PayrollRunId.Value;
                    link.Parameters.Add("@NoteId", SqlDbType.Int).Value = noteId;
                    link.Parameters.Add("@VoucherLinesCount", SqlDbType.Int).Value = voucherLinesCount;
                    link.Parameters.Add("@DebitTotal", SqlDbType.Money).Value = debitTotal;
                    link.Parameters.Add("@CreditTotal", SqlDbType.Money).Value = creditTotal;
                    link.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
                    link.ExecuteNonQuery();
                }
            }

            using (var updateHeader = CreateCommand(connection, transaction, @"
UPDATE dbo.PayrollRunHeader
SET IsPosted = 1,
    PostedAt = GETDATE(),
    PostedBy = @UserId,
    NoteId = @NoteId,
    NoteSerial = @NoteId,
    VoucherLinesCount = @VoucherLinesCount,
    UpdatedAt = GETDATE(),
    UpdatedBy = @UserId
WHERE PayrollRunId = @PayrollRunId;"))
            {
                updateHeader.Parameters.Add("@PayrollRunId", SqlDbType.Int).Value = request.PayrollRunId.Value;
                AddNullable(updateHeader, "@NoteId", SqlDbType.Int, noteIds.FirstOrDefault());
                updateHeader.Parameters.Add("@VoucherLinesCount", SqlDbType.Int).Value = voucherLinesCount;
                updateHeader.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
                updateHeader.ExecuteNonQuery();
            }

            using (var updateRows = CreateCommand(connection, transaction, @"
UPDATE dbo.PayrollRunEmployees
SET IsPosted = 1,
    NoteSerial = @NoteId,
    UpdatedAt = GETDATE(),
    UpdatedBy = @UserId
WHERE PayrollRunId = @PayrollRunId;"))
            {
                updateRows.Parameters.Add("@PayrollRunId", SqlDbType.Int).Value = request.PayrollRunId.Value;
                AddNullable(updateRows, "@NoteId", SqlDbType.Int, noteIds.FirstOrDefault());
                updateRows.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
                updateRows.ExecuteNonQuery();
            }
        }

        private static IList<PayrollTestPostingDimensionTotal> BuildDimensionTotals(IEnumerable<PayrollReplayedVoucherLine> lines, Func<PayrollReplayedVoucherLine, string> keySelector)
        {
            return lines
                .GroupBy(keySelector)
                .Select(g => new PayrollTestPostingDimensionTotal
                {
                    Key = string.IsNullOrWhiteSpace(g.Key) ? "Unspecified" : g.Key,
                    Debit = g.Where(x => !x.IsCredit).Sum(x => x.Value),
                    Credit = g.Where(x => x.IsCredit).Sum(x => x.Value),
                    Lines = g.Count()
                })
                .OrderByDescending(x => Math.Abs(x.Debit - x.Credit))
                .ThenByDescending(x => x.Lines)
                .ToList();
        }

        private void EnrichAffectedAccountTotals(IList<PayrollTestPostingDimensionTotal> accounts)
        {
            if (accounts == null || accounts.Count == 0)
            {
                return;
            }

            var accountCodes = accounts
                .Select(x => x.Key)
                .Where(x => !string.IsNullOrWhiteSpace(x) && !string.Equals(x, "Unspecified", StringComparison.OrdinalIgnoreCase))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (accountCodes.Count == 0)
            {
                return;
            }

            var accountInfo = new Dictionary<string, Tuple<string, string>>(StringComparer.OrdinalIgnoreCase);
            using (var connection = OpenConnection())
            using (var command = connection.CreateCommand())
            {
                var parameterNames = new List<string>();
                for (var i = 0; i < accountCodes.Count; i++)
                {
                    var parameterName = "@Account" + i.ToString();
                    parameterNames.Add(parameterName);
                    command.Parameters.Add(parameterName, SqlDbType.NVarChar, 50).Value = accountCodes[i];
                }

                command.CommandText = @"
SELECT Account_Code, Account_Serial, Account_Name
FROM dbo.ACCOUNTS WITH (NOLOCK)
WHERE Account_Code IN (" + string.Join(",", parameterNames.ToArray()) + @");";
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        accountInfo[ReadString(reader, "Account_Code")] = Tuple.Create(ReadString(reader, "Account_Serial"), ReadString(reader, "Account_Name"));
                    }
                }
            }

            foreach (var account in accounts)
            {
                Tuple<string, string> info;
                if (accountInfo.TryGetValue(account.Key, out info))
                {
                    account.AccountSerial = info.Item1;
                    account.AccountName = info.Item2;
                }
            }
        }

        private Dictionary<string, int> InsertTestPostingNotes(SqlConnection connection, SqlTransaction transaction, PayrollAccountingReplayReport report, PayrollTestPostingRequest request, int userId, string userName, string marker)
        {
            var linesByBranch = (report.ReplayedLines ?? new List<PayrollReplayedVoucherLine>())
                .Where(x => !string.IsNullOrWhiteSpace(x.AccountCode) && x.Value != 0)
                .GroupBy(x => BranchKey(x.BranchId))
                .ToList();
            var notesByBranch = new Dictionary<string, int>();
            var periodCode = SafeSalaryPeriodCode(request);
            foreach (var branchGroup in linesByBranch)
            {
                var note = report.ReplayedNotes.FirstOrDefault(x => BranchKey(x.BranchId) == branchGroup.Key);
                var noteId = NextId(connection, transaction, "Notes", "NoteID");
                var debit = branchGroup.Where(x => !x.IsCredit).Sum(x => x.Value);
                var noteValue = note != null && note.NoteValue > 0 ? note.NoteValue : debit;
                using (var command = CreateCommand(connection, transaction, @"
INSERT INTO dbo.Notes
(
    NoteID, NoteDate, NoteType, NoteSerial, NoteSerial1, Note_Value, UserID, Remark,
    branch_no, user_name, salary, PayrollMonth, PayrollYear, DateTimeEntry, last_changed, NotePosted
)
VALUES
(
    @NoteID, GETDATE(), @NoteType, @NoteSerial, @NoteSerial1, @NoteValue, @UserID, @Remark,
    @BranchId, @UserName, @Salary, @PayrollMonth, @PayrollYear, GETDATE(), GETDATE(), 0
);"))
                {
                    command.Parameters.Add("@NoteID", SqlDbType.Int).Value = noteId;
                    command.Parameters.Add("@NoteType", SqlDbType.Int).Value = request.NoteType.GetValueOrDefault(66);
                    command.Parameters.Add("@NoteSerial", SqlDbType.Float).Value = noteId;
                    command.Parameters.Add("@NoteSerial1", SqlDbType.Float).Value = noteId;
                    command.Parameters.Add("@NoteValue", SqlDbType.Float).Value = Convert.ToDouble(noteValue);
                    command.Parameters.Add("@UserID", SqlDbType.Int).Value = userId;
                    command.Parameters.Add("@Remark", SqlDbType.NVarChar, 4000).Value = marker + " - Protected salary replay test posting. Production posting remains disabled.";
                    AddNullable(command, "@BranchId", SqlDbType.Int, ParseNullableInt(branchGroup.Key));
                    command.Parameters.Add("@UserName", SqlDbType.NVarChar, 50).Value = (object)(userName ?? "MainErp") ?? DBNull.Value;
                    command.Parameters.Add("@Salary", SqlDbType.Int).Value = periodCode;
                    command.Parameters.Add("@PayrollMonth", SqlDbType.Int).Value = request.Month;
                    command.Parameters.Add("@PayrollYear", SqlDbType.Int).Value = request.Year;
                    command.ExecuteNonQuery();
                }

                notesByBranch[branchGroup.Key] = noteId;
            }

            return notesByBranch;
        }

        private int InsertTestPostingVoucherLines(SqlConnection connection, SqlTransaction transaction, PayrollAccountingReplayReport report, IDictionary<string, int> noteIdByBranch, int userId, string marker)
        {
            var inserted = 0;
            var nextVoucherId = NextId(connection, transaction, "DOUBLE_ENTREY_VOUCHERS", "Double_Entry_Vouchers_ID");
            var lineNoByNote = new Dictionary<int, int>();
            foreach (var line in (report.ReplayedLines ?? new List<PayrollReplayedVoucherLine>()).Where(x => !string.IsNullOrWhiteSpace(x.AccountCode) && x.Value != 0))
            {
                var branchKey = BranchKey(line.BranchId);
                int noteId;
                if (!noteIdByBranch.TryGetValue(branchKey, out noteId))
                {
                    continue;
                }

                var nextLineNo = lineNoByNote.ContainsKey(noteId) ? lineNoByNote[noteId] + 1 : 1;
                lineNoByNote[noteId] = nextLineNo;
                var description = marker + " - " + FirstNonEmpty(line.RuleId, "PAYROLL_REPLAY") + " - " + FirstNonEmpty(line.Explanation, line.Trigger);
                if (description.Length > 3800)
                {
                    description = description.Substring(0, 3800);
                }

                using (var command = CreateCommand(connection, transaction, @"
INSERT INTO dbo.DOUBLE_ENTREY_VOUCHERS
(
    Double_Entry_Vouchers_ID, DEV_ID_Line_No, Account_Code, Value, Credit_Or_Debit,
    Double_Entry_Vouchers_Description, RecordDate, Notes_ID, UserID, Posted,
    branch_id, project_id, Departementid, NEmpid, depet_value, credit_value, des
)
VALUES
(
    @VoucherId, @LineNo, @AccountCode, @Value, @CreditOrDebit,
    @Description, GETDATE(), @NoteId, @UserId, 0,
    @BranchId, @ProjectId, @DepartmentId, @EmployeeId, @DebitValue, @CreditValue, @ShortDescription
);"))
                {
                    command.Parameters.Add("@VoucherId", SqlDbType.Int).Value = nextVoucherId++;
                    command.Parameters.Add("@LineNo", SqlDbType.Int).Value = nextLineNo;
                    command.Parameters.Add("@AccountCode", SqlDbType.NVarChar, 50).Value = line.AccountCode;
                    command.Parameters.Add("@Value", SqlDbType.Money).Value = line.Value;
                    command.Parameters.Add("@CreditOrDebit", SqlDbType.SmallInt).Value = line.IsCredit ? 1 : 0;
                    command.Parameters.Add("@Description", SqlDbType.NVarChar, 4000).Value = description;
                    command.Parameters.Add("@NoteId", SqlDbType.Int).Value = noteId;
                    command.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
                    AddNullable(command, "@BranchId", SqlDbType.Int, line.BranchId);
                    AddNullable(command, "@ProjectId", SqlDbType.Int, line.ProjectId);
                    AddNullable(command, "@DepartmentId", SqlDbType.Float, line.DepartmentId);
                    AddNullable(command, "@EmployeeId", SqlDbType.Float, line.EmployeeId);
                    command.Parameters.Add("@DebitValue", SqlDbType.Money).Value = line.IsCredit ? 0 : line.Value;
                    command.Parameters.Add("@CreditValue", SqlDbType.Money).Value = line.IsCredit ? line.Value : 0;
                    command.Parameters.Add("@ShortDescription", SqlDbType.NVarChar, 255).Value = description.Length > 255 ? description.Substring(0, 255) : description;
                    command.ExecuteNonQuery();
                    inserted++;
                }
            }

            return inserted;
        }

        private void InsertTestPostingAudit(SqlConnection connection, SqlTransaction transaction, PayrollTestPostingRequest request, Guid batchId, int userId, string userName, PayrollTestPostingResult result, int insertedLines)
        {
            using (var command = CreateCommand(connection, transaction, @"
INSERT INTO dbo.MainErpPayrollTestPostingAudit
(
    TestPostingBatchId, CreatedAt, CreatedBy, CreatedByName, DatabaseName,
    [Year], [Month], BranchId, DepartmentId, EmployeeId,
    NotesCount, VoucherLinesCount, DebitTotal, CreditTotal, Balance, CleanupStatus, Warning
)
VALUES
(
    @BatchId, GETDATE(), @UserId, @UserName, @DatabaseName,
    @Year, @Month, @BranchId, @DepartmentId, @EmployeeId,
    @NotesCount, @VoucherLinesCount, @DebitTotal, @CreditTotal, @Balance, N'Active', @Warning
);"))
            {
                command.Parameters.Add("@BatchId", SqlDbType.UniqueIdentifier).Value = batchId;
                command.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
                command.Parameters.Add("@UserName", SqlDbType.NVarChar, 255).Value = (object)(userName ?? "MainErp") ?? DBNull.Value;
                command.Parameters.Add("@DatabaseName", SqlDbType.NVarChar, 128).Value = result.DatabaseName;
                command.Parameters.Add("@Year", SqlDbType.Int).Value = request.Year;
                command.Parameters.Add("@Month", SqlDbType.Int).Value = request.Month;
                AddNullable(command, "@BranchId", SqlDbType.Int, request.BranchId);
                AddNullable(command, "@DepartmentId", SqlDbType.Int, request.DepartmentId);
                AddNullable(command, "@EmployeeId", SqlDbType.Int, request.EmployeeId);
                command.Parameters.Add("@NotesCount", SqlDbType.Int).Value = result.NotesCount;
                command.Parameters.Add("@VoucherLinesCount", SqlDbType.Int).Value = insertedLines;
                command.Parameters.Add("@DebitTotal", SqlDbType.Decimal).Value = result.DebitTotal;
                command.Parameters["@DebitTotal"].Precision = 18;
                command.Parameters["@DebitTotal"].Scale = 2;
                command.Parameters.Add("@CreditTotal", SqlDbType.Decimal).Value = result.CreditTotal;
                command.Parameters["@CreditTotal"].Precision = 18;
                command.Parameters["@CreditTotal"].Scale = 2;
                command.Parameters.Add("@Balance", SqlDbType.Decimal).Value = result.Balance;
                command.Parameters["@Balance"].Precision = 18;
                command.Parameters["@Balance"].Scale = 2;
                command.Parameters.Add("@Warning", SqlDbType.NVarChar, 4000).Value = result.Warnings.Any() ? (object)string.Join("; ", result.Warnings.ToArray()) : DBNull.Value;
                command.ExecuteNonQuery();
            }
        }

        private static void EnsurePayrollTestPostingAuditTable(SqlConnection connection, SqlTransaction transaction)
        {
            using (var command = CreateCommand(connection, transaction, @"
IF OBJECT_ID(N'dbo.MainErpPayrollTestPostingAudit', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.MainErpPayrollTestPostingAudit
    (
        TestPostingBatchId uniqueidentifier NOT NULL CONSTRAINT PK_MainErpPayrollTestPostingAudit PRIMARY KEY,
        CreatedAt datetime NOT NULL CONSTRAINT DF_MainErpPayrollTestPostingAudit_CreatedAt DEFAULT(GETDATE()),
        CreatedBy int NULL,
        CreatedByName nvarchar(255) NULL,
        DatabaseName sysname NOT NULL,
        [Year] int NOT NULL,
        [Month] int NOT NULL,
        BranchId int NULL,
        DepartmentId int NULL,
        EmployeeId int NULL,
        NotesCount int NOT NULL CONSTRAINT DF_MainErpPayrollTestPostingAudit_NotesCount DEFAULT(0),
        VoucherLinesCount int NOT NULL CONSTRAINT DF_MainErpPayrollTestPostingAudit_VoucherLinesCount DEFAULT(0),
        DebitTotal decimal(18,2) NOT NULL CONSTRAINT DF_MainErpPayrollTestPostingAudit_Debit DEFAULT(0),
        CreditTotal decimal(18,2) NOT NULL CONSTRAINT DF_MainErpPayrollTestPostingAudit_Credit DEFAULT(0),
        Balance decimal(18,2) NOT NULL CONSTRAINT DF_MainErpPayrollTestPostingAudit_Balance DEFAULT(0),
        CleanupStatus nvarchar(50) NOT NULL CONSTRAINT DF_MainErpPayrollTestPostingAudit_CleanupStatus DEFAULT(N'Active'),
        CleanedAt datetime NULL,
        CleanedBy int NULL,
        CleanedNotesCount int NULL,
        CleanedVoucherLinesCount int NULL,
        Warning nvarchar(4000) NULL
    );
END"))
            {
                command.ExecuteNonQuery();
            }
        }

        private void ValidateTestPostingSecret(string password, string confirmationPhrase)
        {
            var configuredPassword = ConfigurationManager.AppSettings["PayrollTestPostingPassword"];
            if (string.IsNullOrWhiteSpace(configuredPassword))
            {
                throw new InvalidOperationException("PayrollTestPostingPassword is not configured.");
            }

            if (!string.Equals(password ?? string.Empty, configuredPassword, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Invalid protected test posting password.");
            }

            if (!string.Equals((confirmationPhrase ?? string.Empty).Trim(), "POST TO TEST", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Type POST TO TEST to confirm this protected test database action.");
            }
        }

        private bool IsPayrollTestPostingAllowedDatabase()
        {
            var databaseName = GetDatabaseName();
            var allowed = ConfigurationManager.AppSettings["PayrollTestPostingAllowedDatabases"] ?? "Dania";
            return allowed.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Any(x => string.Equals(x.Trim(), databaseName, StringComparison.OrdinalIgnoreCase));
        }

        private string GetDatabaseName()
        {
            return new SqlConnectionStringBuilder(_connectionString).InitialCatalog;
        }

        private static string BranchKey(int? branchId)
        {
            return branchId.HasValue ? branchId.Value.ToString() : "NULL";
        }

        private static int? ParseNullableInt(string value)
        {
            int parsed;
            return int.TryParse(value, out parsed) ? (int?)parsed : null;
        }

        private static HashSet<int> ParseEmployeeIdList(string value)
        {
            var result = new HashSet<int>();
            if (string.IsNullOrWhiteSpace(value))
            {
                return result;
            }

            foreach (var part in value.Split(new[] { ',', ';', '\r', '\n', '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries))
            {
                int id;
                if (int.TryParse(part.Trim(), out id) && id > 0)
                {
                    result.Add(id);
                }
            }

            return result;
        }

        private static string BuildInListParameters(SqlCommand command, string prefix, IList<string> values)
        {
            var names = new List<string>();
            for (var i = 0; i < values.Count; i++)
            {
                var name = prefix + i.ToString();
                names.Add(name);
                command.Parameters.Add(name, SqlDbType.NVarChar, 50).Value = values[i];
            }

            return names.Count == 0 ? "NULL" : string.Join(",", names.ToArray());
        }

        private static int SafeSalaryPeriodCode(SalaryRunRequest request)
        {
            int value;
            return int.TryParse(request.Year.ToString() + request.Month.ToString(), out value) ? value : 0;
        }

        private static IEnumerable<SalaryRunEmployeeRow> GetAccountingReplayRows(SalaryRunPreview preview)
        {
            return preview.Rows.Any(x => x.IsLegacySnapshot)
                ? preview.Rows.Where(x => x.IsLegacySnapshot)
                : preview.Rows;
        }

        private static void AttachReplayLineEmployeeLabels(PayrollAccountingReplayReport report, IEnumerable<SalaryRunEmployeeRow> rows)
        {
            if (report == null || report.ReplayedLines == null || rows == null)
            {
                return;
            }

            var employeeById = rows
                .Where(x => x.EmployeeId > 0)
                .GroupBy(x => x.EmployeeId)
                .ToDictionary(x => x.Key, x => x.First());
            foreach (var line in report.ReplayedLines.Where(x => x.EmployeeId.HasValue))
            {
                SalaryRunEmployeeRow row;
                if (employeeById.TryGetValue(line.EmployeeId.Value, out row))
                {
                    line.EmployeeCode = row.EmployeeCode;
                    line.EmployeeName = row.EmployeeName;
                }
            }
        }

        private static void ReplayComponentAdditionExpenses(IEnumerable<SalaryRunEmployeeRow> rows, PayrollAccountingReplayReport report)
        {
            var useManagement = report.DistributionOptions != null && report.DistributionOptions.ProjectEmployeeGV && report.DistributionOptions.SalaryJournalByManagement;
            var groups = rows
                .SelectMany(row => row.Components
                    .Select(component => new { Row = row, Component = component, Value = ComponentReplayValue(row, component) })
                    .Where(x => x.Component.ViewComponent && !x.Component.AddOrDiscount && !x.Component.ZmamAccount && !x.Component.AdvancePaymentAccount && x.Value > 0))
                .GroupBy(x => new { x.Row.BranchId, DepartmentId = useManagement ? x.Row.DepartmentId : null, x.Component.ComponentNo, x.Component.AccountCode, x.Component.ComponentNameAr });

            foreach (var group in groups)
            {
                AddReplayLine(report, group.Key.AccountCode, group.Sum(x => x.Value), false, group.Key.BranchId, group.Key.DepartmentId, null, group.Key.ComponentNo, group.Key.ComponentNameAr,
                    "COMP_ADD_EXPENSE",
                    "mofrad.Account_Code",
                    "ViewComp=True; AddOrDiscount=0; ZmamAccount=False; AdvPaymentdAccount=False; value=emp_salary.CompN when snapshot exists",
                    "VB6 GetComponentValuePerBranch reads the runtime grid/snapshot value, not the current employee master component value.");
            }
        }

        private static decimal ComponentReplayValue(SalaryRunEmployeeRow row, PayrollCompatibilityComponent component)
        {
            return row != null && row.IsLegacySnapshot ? component.SnapshotValue : component.SourceValue;
        }

        private static void ReplayEmployeeSalaryPayable(SalaryRunEmployeeRow row, PayrollAccountingReplayReport report)
        {
            AddReplayLine(report, row.AccruedSalaryAccountCode, row.TotalBeforeDeductions, true, row.BranchId, row.DepartmentId, row.EmployeeId, null, null,
                "SALARY_PAYABLE_TOTAL1",
                "TblEmployee.Account_Code1",
                "row.total1 > 0",
                "VB6 credits the employee accrued salary account with total1.");
        }

        private static void ReplayEmployeeDeductions(SalaryRunEmployeeRow row, PayrollAccountingReplayReport report)
        {
            foreach (var component in row.Components.Where(x => x.ViewComponent && x.AddOrDiscount && !x.ZmamAccount && !x.AdvancePaymentAccount && ComponentReplayValue(row, x) > 0))
            {
                var value = ComponentReplayValue(row, component);
                AddReplayLine(report, row.AccruedSalaryAccountCode, value, false, row.BranchId, row.DepartmentId, row.EmployeeId, component.ComponentNo, component.ComponentNameAr,
                    "COMP_DEDUCT_EMPLOYEE_PAYABLE",
                    "TblEmployee.Account_Code1",
                    "ViewComp=True; AddOrDiscount=-1; ZmamAccount=False; AdvPaymentdAccount=False",
                    "VB6 debits accrued salary for visible deduction components.");
                AddReplayLine(report, component.AccountCode, value, true, row.BranchId, row.DepartmentId, row.EmployeeId, component.ComponentNo, component.ComponentNameAr,
                    "COMP_DEDUCT_COMPONENT_CREDIT",
                    "mofrad.Account_Code",
                    "ViewComp=True; AddOrDiscount=-1; ZmamAccount=False; AdvPaymentdAccount=False",
                    "VB6 credits the deduction component account.");
            }
        }

        private static void ReplayProjectSnapshotDistribution(IEnumerable<SalaryRunEmployeeRow> rows, PayrollAccountingReplayReport report)
        {
            var useManagement = report.DistributionOptions != null && report.DistributionOptions.SalaryJournalByManagement;
            foreach (var row in rows.Where(x => x.ProjectId.HasValue && x.ProjectId.Value > 0 && !string.IsNullOrWhiteSpace(x.ProjectSalaryAccountCode)))
            {
                foreach (var component in row.Components.Where(x => x.ViewComponent && !x.ZmamAccount && !x.AdvancePaymentAccount && ComponentReplayValue(row, x) > 0))
                {
                    var value = ComponentReplayValue(row, component);
                    var departmentId = useManagement ? row.DepartmentId : null;
                    var path = "BranchId=" + (row.BranchId.HasValue ? row.BranchId.Value.ToString() : "NULL")
                        + "; ProjectId=" + row.ProjectId.Value
                        + "; DepartmentId=" + (departmentId.HasValue ? departmentId.Value.ToString() : "NULL");
                    var rulePath = "ProjectEmployeeGV=True; emp_salary.project_id>0; " + component.ComponentColumn + ">0; ViewComp=True";
                    if (!component.AddOrDiscount)
                    {
                        AddReplayLine(report, row.ProjectSalaryAccountCode, value, false, row.BranchId, departmentId, row.EmployeeId, component.ComponentNo, component.ComponentNameAr,
                            "PROJECT_SNAPSHOT_COMPONENT_PROJECT_DEBIT",
                            "projects.Salary_account from emp_salary.project_id",
                            rulePath + "; AddOrDiscount=0",
                            "VB6 project allocation debits the project salary account for each project-sourced salary component.",
                            row.ProjectId,
                            "emp_salary.project_id snapshot",
                            path,
                            "ProjectEmployeeGV project salary account override",
                            string.Empty,
                            "project routing mismatch");
                        AddReplayLine(report, component.AccountCode, value, true, row.BranchId, departmentId, row.EmployeeId, component.ComponentNo, component.ComponentNameAr,
                            "PROJECT_SNAPSHOT_COMPONENT_MOFRAD_CREDIT",
                            "mofrad.Account_Code",
                            rulePath + "; AddOrDiscount=0",
                            "VB6 project allocation credits the original component account, moving the cost from the generic project component account to the specific project salary account.",
                            null,
                            "emp_salary.project_id snapshot",
                            "BranchId=" + (row.BranchId.HasValue ? row.BranchId.Value.ToString() : "NULL") + "; ProjectId=NULL; DepartmentId=" + (departmentId.HasValue ? departmentId.Value.ToString() : "NULL"),
                            "Counter-entry for project salary account debit",
                            string.Empty,
                            "project routing mismatch");
                    }
                    else
                    {
                        AddReplayLine(report, component.AccountCode, value, false, row.BranchId, departmentId, row.EmployeeId, component.ComponentNo, component.ComponentNameAr,
                            "PROJECT_SNAPSHOT_DEDUCTION_MOFRAD_DEBIT",
                            "mofrad.Account_Code",
                            rulePath + "; AddOrDiscount=-1",
                            "VB6 project allocation debits the deduction component account for project-sourced deductions.",
                            null,
                            "emp_salary.project_id snapshot",
                            "BranchId=" + (row.BranchId.HasValue ? row.BranchId.Value.ToString() : "NULL") + "; ProjectId=NULL; DepartmentId=" + (departmentId.HasValue ? departmentId.Value.ToString() : "NULL"),
                            "Project deduction counter-entry",
                            string.Empty,
                            "project routing mismatch");
                        AddReplayLine(report, row.ProjectSalaryAccountCode, value, true, row.BranchId, departmentId, row.EmployeeId, component.ComponentNo, component.ComponentNameAr,
                            "PROJECT_SNAPSHOT_DEDUCTION_PROJECT_CREDIT",
                            "projects.Salary_account from emp_salary.project_id",
                            rulePath + "; AddOrDiscount=-1",
                            "VB6 project allocation credits the project salary account for project-sourced deductions.",
                            row.ProjectId,
                            "emp_salary.project_id snapshot",
                            path,
                            "ProjectEmployeeGV project salary account override",
                            string.Empty,
                            "project routing mismatch");
                    }
                }
            }
        }

        private static void ReplayProjectDistributionComponentDeductions(IEnumerable<SalaryRunEmployeeRow> rows, PayrollAccountingReplayReport report)
        {
            var useManagement = report.DistributionOptions != null && report.DistributionOptions.SalaryJournalByManagement;
            var useDiscountOverride = report.DistributionOptions != null && report.DistributionOptions.ProjectDiscountPolicy == 1;
            var groups = rows
                .SelectMany(row => row.Components
                    .Select(component => new { Row = row, Component = component, Value = ComponentReplayValue(row, component) })
                    .Where(x => x.Component.ViewComponent && x.Component.AddOrDiscount && !x.Component.ZmamAccount && !x.Component.AdvancePaymentAccount && x.Value > 0))
                .GroupBy(x => new
                {
                    x.Row.BranchId,
                    DepartmentId = useManagement ? x.Row.DepartmentId : null,
                    x.Component.ComponentNo,
                    AccountCode = useDiscountOverride && !string.IsNullOrWhiteSpace(x.Component.AccountCode1) ? x.Component.AccountCode1 : x.Component.AccountCode,
                    x.Component.ComponentNameAr
                });

            foreach (var group in groups)
            {
                AddReplayLine(report, group.Key.AccountCode, group.Sum(x => x.Value), true, group.Key.BranchId, group.Key.DepartmentId, null, group.Key.ComponentNo, group.Key.ComponentNameAr,
                    "PROJECT_GRID_DEDUCTION_COMPONENT_CREDIT",
                    useDiscountOverride ? "mofrad.Account_Code1 via ProjectDiscountPolicy" : "mofrad.Account_Code",
                    "ProjectEmployeeGV=True; ViewComp=True; AddOrDiscount=-1; ZmamAccount=False; AdvPaymentdAccount=False",
                    "VB6 project salary journal credits deduction component accounts by branch, and by department when SalaryJLByManagement=True. It does not replay the extra payable debit used by the non-project branch.",
                    null,
                    "GetComponentValuePerBranch",
                    "BranchId=" + (group.Key.BranchId.HasValue ? group.Key.BranchId.Value.ToString() : "NULL") + "; ProjectId=NULL; DepartmentId=" + (group.Key.DepartmentId.HasValue ? group.Key.DepartmentId.Value.ToString() : "NULL"),
                    useDiscountOverride ? "ProjectDiscountPolicy account override" : string.Empty,
                    string.Empty,
                    group.Key.DepartmentId.HasValue ? "department routing mismatch" : "branch routing mismatch");
            }
        }

        private static decimal PayrollInsuranceValue(SalaryRunEmployeeRow row)
        {
            return row.TotalInsuranceLegacy != 0
                ? row.TotalInsuranceLegacy
                : (row.InsuranceTrace != null && row.InsuranceTrace.RuntimeFunctionInsurance != 0 ? row.InsuranceTrace.RuntimeFunctionInsurance : row.MedicalInsuranceDeduction);
        }

        private static void ReplayProjectEmployeeNetPayable(SalaryRunEmployeeRow row, PayrollAccountingReplayReport report)
        {
            var insurance = PayrollInsuranceValue(row);
            var value = row.NetSalary + insurance;
            if (value > 0)
            {
                AddReplayLine(report, row.AccruedSalaryAccountCode, value, true, row.BranchId, row.DepartmentId, row.EmployeeId, null, null,
                    "PROJECT_EMPLOYEE_NET_PAYABLE_CREDIT",
                    "TblEmployee.Account_Code1",
                    "ProjectEmployeeGV=True; EmpTotalNet > 0; value=EmpTotalNet+ToalInsurance",
                    "VB6 project branch credits accrued salary with EmpTotalNet plus ToalInsurance, not total1.",
                    null,
                    "salary runtime grid",
                    "BranchId=" + (row.BranchId.HasValue ? row.BranchId.Value.ToString() : "NULL") + "; ProjectId=NULL; DepartmentId=" + (row.DepartmentId.HasValue ? row.DepartmentId.Value.ToString() : "NULL"),
                    "ProjectEmployeeGV net-payable branch",
                    string.Empty,
                    "employee accrued account mismatch");
                return;
            }

            if (value < 0)
            {
                AddReplayLine(report, row.AccruedSalaryAccountCode, Math.Abs(value), false, row.BranchId, row.DepartmentId, row.EmployeeId, null, null,
                    "PROJECT_EMPLOYEE_NEGATIVE_NET_PAYABLE_DEBIT",
                    "TblEmployee.Account_Code1",
                    "ProjectEmployeeGV=True; EmpTotalNet < 0; value=Abs(EmpTotalNet+ToalInsurance)",
                    "VB6 project branch debits accrued salary when the employee net is negative.",
                    null,
                    "salary runtime grid",
                    "BranchId=" + (row.BranchId.HasValue ? row.BranchId.Value.ToString() : "NULL") + "; ProjectId=NULL; DepartmentId=" + (row.DepartmentId.HasValue ? row.DepartmentId.Value.ToString() : "NULL"),
                    "ProjectEmployeeGV negative-net branch",
                    string.Empty,
                    "employee accrued account mismatch");
            }
        }

        private static void ReplayProjectZmamComponents(SalaryRunEmployeeRow row, PayrollAccountingReplayReport report)
        {
            foreach (var component in row.Components.Where(x => x.ViewComponent && x.ZmamAccount && ComponentReplayValue(row, x) > 0))
            {
                AddReplayLine(report, row.EmployeeAccountCode, ComponentReplayValue(row, component), true, row.BranchId, row.DepartmentId, row.EmployeeId, component.ComponentNo, component.ComponentNameAr,
                    "PROJECT_ZMAM_EMPLOYEE_ACCOUNT_CREDIT",
                    "TblEmployee.Account_Code",
                    "ProjectEmployeeGV=True; ViewComp=True; ZmamAccount=True",
                    "VB6 project branch credits the employee custody account for positive ZmamAccount components. The non-project payable debit is not emitted in this branch.",
                    null,
                    "salary runtime grid",
                    "BranchId=" + (row.BranchId.HasValue ? row.BranchId.Value.ToString() : "NULL") + "; ProjectId=NULL; DepartmentId=" + (row.DepartmentId.HasValue ? row.DepartmentId.Value.ToString() : "NULL"),
                    "ProjectEmployeeGV Zmam branch",
                    string.Empty,
                    "employee accrued account mismatch");
            }
        }

        private static void ReplayProjectAdvanceDeductions(SalaryRunEmployeeRow row, PayrollAccountingReplayReport report)
        {
            if (row.AdvanceDeduction <= 0)
            {
                return;
            }

            AddReplayLine(report, row.EmployeeAccountCode, row.AdvanceDeduction, true, row.BranchId, row.DepartmentId, row.EmployeeId, null, null,
                "PROJECT_TOTAL_ADVANCE_EMPLOYEE_ACCOUNT_CREDIT",
                "TblEmployee.Account_Code",
                "ProjectEmployeeGV=True; TotalAdvance > 0",
                "VB6 project branch credits the employee custody account for total advance repayment. The payable debit is absorbed by EmpTotalNet.",
                null,
                "salary runtime grid",
                "BranchId=" + (row.BranchId.HasValue ? row.BranchId.Value.ToString() : "NULL") + "; ProjectId=NULL; DepartmentId=" + (row.DepartmentId.HasValue ? row.DepartmentId.Value.ToString() : "NULL"),
                "ProjectEmployeeGV advance branch",
                string.Empty,
                "employee accrued account mismatch");
        }

        private static void ReplayZmamComponents(SalaryRunEmployeeRow row, PayrollAccountingReplayReport report)
        {
            foreach (var component in row.Components.Where(x => x.ViewComponent && x.ZmamAccount && ComponentReplayValue(row, x) > 0))
            {
                var value = ComponentReplayValue(row, component);
                AddReplayLine(report, row.AccruedSalaryAccountCode, value, false, row.BranchId, row.DepartmentId, row.EmployeeId, component.ComponentNo, component.ComponentNameAr,
                    "ZMAM_EMPLOYEE_PAYABLE",
                    "TblEmployee.Account_Code1",
                    "ViewComp=True; ZmamAccount=True",
                    "VB6 debits accrued salary for custody/receivable components.");
                AddReplayLine(report, row.EmployeeAccountCode, value, true, row.BranchId, row.DepartmentId, row.EmployeeId, component.ComponentNo, component.ComponentNameAr,
                    "ZMAM_EMPLOYEE_ACCOUNT",
                    "TblEmployee.Account_Code",
                    "ViewComp=True; ZmamAccount=True",
                    "VB6 credits the employee custody account.");
            }
        }

        private static void ReplayAdvanceDeductions(SalaryRunEmployeeRow row, PayrollAccountingReplayReport report)
        {
            if (row.AdvanceDeduction <= 0)
            {
                return;
            }

            AddReplayLine(report, row.AccruedSalaryAccountCode, row.AdvanceDeduction, false, row.BranchId, row.DepartmentId, row.EmployeeId, null, null,
                "TOTAL_ADVANCE_PAYABLE",
                "TblEmployee.Account_Code1",
                "TotalAdvance > 0",
                "VB6 debits accrued salary for total advance repayment.");
            AddReplayLine(report, row.EmployeeAccountCode, row.AdvanceDeduction, true, row.BranchId, row.DepartmentId, row.EmployeeId, null, null,
                "TOTAL_ADVANCE_EMPLOYEE_ACCOUNT",
                "TblEmployee.Account_Code",
                "TotalAdvance > 0",
                "VB6 credits the employee custody account for total advance repayment.");
        }

        private static void ReplayVacationDeductions(SalaryRunEmployeeRow row, PayrollAccountingReplayReport report)
        {
            if (row.VacationDeduction <= 0)
            {
                return;
            }

            AddReplayLine(report, row.AccruedSalaryAccountCode, row.VacationDeduction, false, row.BranchId, row.DepartmentId, row.EmployeeId, null, null,
                "VACATION_PAYABLE",
                "TblEmployee.Account_Code1",
                "VoCation3 > 0",
                "VB6 debits accrued salary for vacation/medical-leave impact.");
            AddReplayLine(report, row.VacationProvisionAccountCode, row.VacationDeduction, true, row.BranchId, row.DepartmentId, row.EmployeeId, null, null,
                "VACATION_BRANCH_ACCOUNT",
                "get_account_code_branch(204) fallback trace: TblEmployee.Account_Code2 when available",
                "VoCation3 > 0",
                "VB6 credits the branch vacation account returned by get_account_code_branch(204); replay uses the employee vacation provision account when the branch account is not resolved.");
        }

        private static void ReplayAdvancePaymentComponents(SalaryRunEmployeeRow row, PayrollAccountingReplayReport report)
        {
            foreach (var component in row.Components.Where(x => x.ViewComponent && x.AdvancePaymentAccount && ComponentReplayValue(row, x) > 0))
            {
                var value = ComponentReplayValue(row, component);
                if (!component.AddOrDiscount)
                {
                    AddReplayLine(report, row.AdvancePaymentAccountCode, value, false, row.BranchId, row.DepartmentId, row.EmployeeId, component.ComponentNo, component.ComponentNameAr,
                        "ADV_COMPONENT_PREPAYMENT_DEBIT",
                        "TblEmployee.Account_Code3",
                        "ViewComp=True; AdvPaymentdAccount=True; AddOrDiscount=0",
                        "VB6 debits the employee advance-payment account for advance-payment addition components.");
                    continue;
                }

                AddReplayLine(report, row.AccruedSalaryAccountCode, value, false, row.BranchId, row.DepartmentId, row.EmployeeId, component.ComponentNo, component.ComponentNameAr,
                    "ADV_COMPONENT_PAYABLE_DEBIT",
                    "TblEmployee.Account_Code1",
                    "ViewComp=True; AdvPaymentdAccount=True; AddOrDiscount=-1",
                    "VB6 debits accrued salary for advance-payment deduction components.");
                AddReplayLine(report, row.AdvancePaymentAccountCode, value, true, row.BranchId, row.DepartmentId, row.EmployeeId, component.ComponentNo, component.ComponentNameAr,
                    "ADV_COMPONENT_PREPAYMENT_CREDIT",
                    "TblEmployee.Account_Code3",
                    "ViewComp=True; AdvPaymentdAccount=True; AddOrDiscount=-1",
                    "VB6 credits the employee advance-payment account.");
            }
        }

        private static void ReplayInsurance(SalaryRunEmployeeRow row, PayrollAccountingReplayReport report)
        {
            if (row.MedicalInsuranceDeduction > 0 || row.MedicalInsuranceCompanyCost > 0)
            {
                ReplayMedicalInsurance(row, report);
            }

            var value = row.InsuranceTrace != null && row.InsuranceTrace.RuntimeFunctionInsurance != 0
                ? row.InsuranceTrace.RuntimeFunctionInsurance
                : row.TotalInsuranceLegacy;
            if (value <= 0)
            {
                return;
            }

            if (row.NetSalary != 0)
            {
                AddReplayLine(report, row.AccruedSalaryAccountCode, value, false, row.BranchId, row.DepartmentId, row.EmployeeId, null, null,
                    "INSURANCE_EMPLOYEE_SHARE_DEBIT",
                    "TblEmployee.Account_Code1",
                    "ToalInsurance > 0; EmpTotalNet <> 0",
                    "VB6 debits accrued salary for employee insurance share.");
            }

            AddReplayLine(report, row.InsuranceTrace != null ? row.InsuranceTrace.InsuranceCreditAccount : null, value, true, row.BranchId, row.DepartmentId, row.EmployeeId, null, null,
                "INSURANCE_SOCIAL_ACCOUNT_CREDIT",
                "TblSocialInsurance.Acount_Code1 via GetInsuranceAccount",
                "ToalInsurance > 0",
                "VB6 credits the configured social-insurance account for employee share.");
        }

        private static void ReplayMedicalInsurance(SalaryRunEmployeeRow row, PayrollAccountingReplayReport report)
        {
            if (row.MedicalInsuranceDeduction > 0)
            {
                AddReplayLine(report, row.AccruedSalaryAccountCode, row.MedicalInsuranceDeduction, false, row.BranchId, row.DepartmentId, row.EmployeeId, null, row.MedicalInsurancePlanName,
                    "MEDICAL_INSURANCE_EMPLOYEE_SHARE_DEBIT",
                    "TblEmployee.Account_Code1",
                    "EmployeeMedicalInsurance active in payroll period; EmployeeMonthlyDeduction > 0",
                    "POS medical insurance debits accrued salary to reduce the employee net salary by the employee share.");
                AddReplayLine(report, row.MedicalInsuranceEmployeeAccountCode, row.MedicalInsuranceDeduction, true, row.BranchId, row.DepartmentId, row.EmployeeId, null, row.MedicalInsurancePlanName,
                    "MEDICAL_INSURANCE_EMPLOYEE_SHARE_PAYABLE_CREDIT",
                    "MedicalInsurancePlans.EmployeeDeductionAccountCode",
                    "EmployeeMedicalInsurance active in payroll period; EmployeeMonthlyDeduction > 0",
                    "POS medical insurance credits the configured insurance payable/deduction account for the employee share.");
            }

            if (row.MedicalInsuranceCompanyCost > 0)
            {
                AddReplayLine(report, row.MedicalInsuranceCompanyAccountCode, row.MedicalInsuranceCompanyCost, false, row.BranchId, row.DepartmentId, row.EmployeeId, null, row.MedicalInsurancePlanName,
                    "MEDICAL_INSURANCE_COMPANY_COST_DEBIT",
                    "MedicalInsurancePlans.CompanyCostAccountCode",
                    "EmployeeMedicalInsurance active in payroll period; CompanyMonthlyCost > 0",
                    "POS medical insurance recognizes the employer share as medical insurance expense/company cost.");
                AddReplayLine(report, row.MedicalInsuranceEmployeeAccountCode, row.MedicalInsuranceCompanyCost, true, row.BranchId, row.DepartmentId, row.EmployeeId, null, row.MedicalInsurancePlanName,
                    "MEDICAL_INSURANCE_COMPANY_PAYABLE_CREDIT",
                    "MedicalInsurancePlans.EmployeeDeductionAccountCode",
                    "EmployeeMedicalInsurance active in payroll period; CompanyMonthlyCost > 0",
                    "POS medical insurance credits the insurance payable/deduction account for the employer share until provider payment clears it.");
            }
        }

        private PayrollDistributionOptions ReadDistributionOptions(SqlConnection connection)
        {
            var result = new PayrollDistributionOptions
            {
                SourceTable = "TblOptions",
                Explanation = "VB6 FrmEmpSalary5 switches project/distribution posting through SystemOptions.ProjectEmployeeGV, SystemOptions.ProjectDiscountPolicy, and SystemOptions.SalaryJLByManagement."
            };
            if (!TableExists(connection, "TblOptions"))
            {
                result.Explanation += " TblOptions was not found in the target database, so replay uses conservative branch/department fallback semantics.";
                return result;
            }

            result.ProjectEmployeeGV = ReadOptionBool(connection, "ProjectEmployeeGV");
            result.ProjectDiscountPolicy = ReadOptionInt(connection, "ProjectDiscountPolicy").GetValueOrDefault();
            result.SalaryJournalByManagement = ReadOptionBool(connection, "SalaryJLByManagement");
            return result;
        }

        private static bool ReadOptionBool(SqlConnection connection, string columnName)
        {
            var value = ReadOptionInt(connection, columnName);
            return value.HasValue && value.Value != 0;
        }

        private static int? ReadOptionInt(SqlConnection connection, string columnName)
        {
            if (!ColumnExists(connection, "TblOptions", columnName))
            {
                return null;
            }

            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT TOP (1) " + Bracket(columnName) + " FROM dbo.TblOptions WITH (NOLOCK);";
                var value = command.ExecuteScalar();
                return value == null || value == DBNull.Value ? (int?)null : Convert.ToInt32(value);
            }
        }

        private static bool ColumnExists(SqlConnection connection, string tableName, string columnName)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT COL_LENGTH('dbo." + tableName.Replace("'", "''") + "', @ColumnName);";
                command.Parameters.Add("@ColumnName", SqlDbType.NVarChar, 128).Value = columnName;
                var value = command.ExecuteScalar();
                return value != null && value != DBNull.Value;
            }
        }

        private static bool ColumnExists(SqlConnection connection, SqlTransaction transaction, string tableName, string columnName)
        {
            using (var command = CreateCommand(connection, transaction, "SELECT COL_LENGTH('dbo." + tableName.Replace("'", "''") + "', @ColumnName);"))
            {
                command.Parameters.Add("@ColumnName", SqlDbType.NVarChar, 128).Value = columnName;
                var value = command.ExecuteScalar();
                return value != null && value != DBNull.Value;
            }
        }

        private static string Bracket(string name)
        {
            return "[" + name.Replace("]", "]]") + "]";
        }

        private void ReplayProjectDistribution(SqlConnection connection, SalaryRunRequest request, PayrollAccountingReplayReport report)
        {
            if (report.DistributionOptions == null || !report.DistributionOptions.ProjectEmployeeGV)
            {
                return;
            }

            if (!TableExists(connection, "TblChangedComponentRegister") || !TableExists(connection, "TblChangedComponentRegisterDetails") || !TableExists(connection, "mofrad"))
            {
                report.DistributionMismatchCategories.Add(new PayrollDistributionMismatchSummary { Category = "missing allocation policy", Count = 1 });
                return;
            }

            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
SELECT
    SUM(ISNULL(d.[value], 0)) AS Balance,
    ISNULL(d.projectid, 0) AS ProjectId,
    p.Salary_account,
    p.Project_name,
    m.id AS ComponentNo,
    m.name AS ComponentName,
    m.Account_Code AS MofradAccount,
    m.Account_Code1 AS MofradAccount1,
    ISNULL(m.AddOrDiscount, 0) AS AddOrDiscount,
    ISNULL(m.ZmamAccount, 0) AS ZmamAccount,
    r.BranchId
FROM dbo.TblChangedComponentRegister r WITH (NOLOCK)
INNER JOIN dbo.TblChangedComponentRegisterDetails d WITH (NOLOCK) ON r.ChangedComponentid = d.ChangedComponentid
LEFT JOIN dbo.mofrad m WITH (NOLOCK) ON r.ComponentID = m.id
LEFT JOIN dbo.projects p WITH (NOLOCK) ON d.projectid = p.id
WHERE MONTH(r.RecordDate) = @Month
  AND YEAR(r.RecordDate) = @Year
  AND (@BranchId IS NULL OR r.BranchId = @BranchId)
  AND (@EmployeeId IS NULL OR d.Emp_id = @EmployeeId)
  AND ISNULL(d.projectid, 0) <> 0
GROUP BY ISNULL(d.projectid, 0), p.Salary_account, p.Project_name, m.id, m.name, m.Account_Code, m.Account_Code1, ISNULL(m.AddOrDiscount, 0), ISNULL(m.ZmamAccount, 0), r.BranchId
HAVING SUM(ISNULL(d.[value], 0)) <> 0;";
                command.Parameters.Add("@Year", SqlDbType.Int).Value = request.Year;
                command.Parameters.Add("@Month", SqlDbType.Int).Value = request.Month;
                AddNullable(command, "@BranchId", SqlDbType.Int, request.BranchId);
                AddNullable(command, "@EmployeeId", SqlDbType.Int, request.EmployeeId);

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var balance = ReadDecimal(reader, "Balance");
                        var projectId = ReadNullableInt(reader, "ProjectId");
                        var projectAccount = ReadString(reader, "Salary_account");
                        var projectName = ReadString(reader, "Project_name");
                        var componentNo = ReadNullableInt(reader, "ComponentNo");
                        var componentName = ReadString(reader, "ComponentName");
                        var mofradAccount = ReadString(reader, "MofradAccount");
                        var mofradAccount1 = ReadString(reader, "MofradAccount1");
                        var addOrDiscount = ReadInt(reader, "AddOrDiscount");
                        var branchId = ReadNullableInt(reader, "BranchId");
                        var zmam = ReadBool(reader, "ZmamAccount");
                        var path = "BranchId=" + (branchId.HasValue ? branchId.Value.ToString() : "NULL") + "; ProjectId=" + (projectId.HasValue ? projectId.Value.ToString() : "NULL") + "; DepartmentId=NULL";

                        if (string.IsNullOrWhiteSpace(projectAccount) || string.IsNullOrWhiteSpace(mofradAccount))
                        {
                            report.DistributionMismatchCategories.Add(new PayrollDistributionMismatchSummary { Category = "fallback account usage", Count = 1, DebitDiff = Math.Abs(balance), CreditDiff = Math.Abs(balance) });
                            continue;
                        }

                        if (zmam)
                        {
                            AddReplayLine(report, projectAccount, balance, false, branchId, null, null, componentNo, componentName,
                                "PROJECT_ZMAM_SALARY_ACCOUNT",
                                "projects.Salary_account",
                                "ProjectEmployeeGV=True; mofrad.ZmamAccount=True",
                                "VB6 project block routes custody project components through the project salary account.",
                                projectId,
                                "TblChangedComponentRegisterDetails.projectid",
                                path,
                                "ProjectEmployeeGV enabled",
                                string.Empty,
                                "project routing mismatch");
                            AddReplayLine(report, mofradAccount, balance, true, branchId, null, null, componentNo, componentName,
                                "PROJECT_ZMAM_COMPONENT_ACCOUNT",
                                "mofrad.Account_Code",
                                "ProjectEmployeeGV=True; mofrad.ZmamAccount=True",
                                "Counter-line for project custody component.",
                                null,
                                "TblChangedComponentRegisterDetails.projectid",
                                path,
                                "Component account from mofrad",
                                string.Empty,
                                "project routing mismatch");
                            continue;
                        }

                        if (addOrDiscount == 0)
                        {
                            AddReplayLine(report, projectAccount, balance, false, branchId, null, null, componentNo, componentName,
                                "PROJECT_ADDITION_SALARY_ACCOUNT",
                                "projects.Salary_account",
                                "ProjectEmployeeGV=True; AddOrDiscount=0",
                                "VB6 debits project salary account for project-based additions.",
                                projectId,
                                "TblChangedComponentRegisterDetails.projectid",
                                path,
                                "Project salary account override",
                                string.Empty,
                                "project routing mismatch");
                            AddReplayLine(report, mofradAccount, balance, true, branchId, null, null, componentNo, componentName,
                                "PROJECT_ADDITION_COMPONENT_ACCOUNT",
                                "mofrad.Account_Code",
                                "ProjectEmployeeGV=True; AddOrDiscount=0",
                                "VB6 credits the source component account for project-based additions.",
                                null,
                                "TblChangedComponentRegisterDetails.projectid",
                                path,
                                "Component account from mofrad",
                                string.Empty,
                                "project routing mismatch");
                        }
                        else
                        {
                            var creditAccount = report.DistributionOptions.ProjectDiscountPolicy == 1 && !string.IsNullOrWhiteSpace(mofradAccount1)
                                ? mofradAccount1
                                : projectAccount;
                            AddReplayLine(report, mofradAccount, balance, false, branchId, null, null, componentNo, componentName,
                                "PROJECT_DEDUCTION_COMPONENT_DEBIT",
                                "mofrad.Account_Code",
                                "ProjectEmployeeGV=True; AddOrDiscount=-1",
                                "VB6 debits component account for project-based deductions.",
                                null,
                                "TblChangedComponentRegisterDetails.projectid",
                                path,
                                "Component account from mofrad",
                                string.Empty,
                                "project routing mismatch");
                            AddReplayLine(report, creditAccount, balance, true, branchId, null, null, componentNo, componentName,
                                "PROJECT_DEDUCTION_POLICY_CREDIT",
                                report.DistributionOptions.ProjectDiscountPolicy == 1 ? "mofrad.Account_Code1 override" : "projects.Salary_account",
                                "ProjectEmployeeGV=True; AddOrDiscount=-1; ProjectDiscountPolicy=" + report.DistributionOptions.ProjectDiscountPolicy,
                                "VB6 credits Account_Code1 when ProjectDiscountPolicy=1, otherwise it uses the project salary account.",
                                projectId,
                                "TblChangedComponentRegisterDetails.projectid",
                                path,
                                report.DistributionOptions.ProjectDiscountPolicy == 1 ? "ProjectDiscountPolicy override" : "Project salary account",
                                string.IsNullOrWhiteSpace(mofradAccount1) ? "Account_Code1 empty; project salary account fallback" : string.Empty,
                                "project routing mismatch");
                        }
                    }
                }
            }
        }

        private void ReplayProjectMofrdSalarDistribution(SqlConnection connection, SalaryRunRequest request, PayrollAccountingReplayReport report)
        {
            if (report.DistributionOptions == null || !report.DistributionOptions.ProjectEmployeeGV)
            {
                return;
            }

            if (!TableExists(connection, "ProJectMofrdSalar") || !TableExists(connection, "emp_salary") || !TableExists(connection, "mofrdat") || !TableExists(connection, "mofrad") || !TableExists(connection, "projects") || !TableExists(connection, "TblEmployee"))
            {
                report.DistributionMismatchCategories.Add(new PayrollDistributionMismatchSummary { Category = "missing allocation policy", Count = 1 });
                return;
            }

            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
WITH ProjectRows AS
(
SELECT
    ps.EmpID,
    e.BranchId,
    CASE WHEN @UseDepartment = 1 THEN e.DepartmentID ELSE NULL END AS DepartmentID,
    ps.ProjID AS ProjectId,
    p.Salary_account,
    p.Project_name,
    m.id AS ComponentNo,
    m.name AS ComponentName,
    m.Account_Code AS MofradAccount,
    m.Account_Code1 AS MofradAccount1,
    ISNULL(m.AddOrDiscount, 0) AS AddOrDiscount,
    ISNULL(m.ZmamAccount, 0) AS ZmamAccount,
    SUM(CASE
            WHEN ISNULL(ps.Total, 0) <> 0 THEN ISNULL(ps.Total, 0)
            WHEN ISNULL(ps.NoDay, 0) <> 0 THEN ISNULL(ps.NoDay, 0) * ISNULL(ps.Valuee, 0)
            ELSE ISNULL(ps.Valuee, 0)
        END) AS Balance,
    SUM(ISNULL(ps.NoDay, 0)) AS SourceDays,
    COUNT(1) AS SourceRows
FROM dbo.ProJectMofrdSalar ps WITH (NOLOCK)
INNER JOIN dbo.emp_salary s WITH (NOLOCK) ON s.emp_id = ps.EmpID AND s.sgn = @Sgn
INNER JOIN dbo.TblEmployee e WITH (NOLOCK) ON e.Emp_ID = ps.EmpID
LEFT JOIN dbo.mofrdat md WITH (NOLOCK) ON md.mofrad_code = ps.MofrdID
LEFT JOIN dbo.mofrad m WITH (NOLOCK) ON m.id = md.mofrad_type
LEFT JOIN dbo.projects p WITH (NOLOCK) ON p.id = ps.ProjID
WHERE ps.MonthID = @Month
  AND ps.YearID = @Year
  AND ISNULL(ps.ProjID, 0) <> 0
  AND (@BranchId IS NULL OR e.BranchId = @BranchId)
  AND (@DepartmentId IS NULL OR e.DepartmentID = @DepartmentId)
  AND (@EmployeeId IS NULL OR ps.EmpID = @EmployeeId)
  AND (s.id IS NOT NULL)
GROUP BY ps.EmpID, e.BranchId, CASE WHEN @UseDepartment = 1 THEN e.DepartmentID ELSE NULL END, ps.ProjID, p.Salary_account, p.Project_name, m.id, m.name, m.Account_Code, m.Account_Code1, ISNULL(m.AddOrDiscount, 0), ISNULL(m.ZmamAccount, 0)
HAVING SUM(CASE
            WHEN ISNULL(ps.Total, 0) <> 0 THEN ISNULL(ps.Total, 0)
            WHEN ISNULL(ps.NoDay, 0) <> 0 THEN ISNULL(ps.NoDay, 0) * ISNULL(ps.Valuee, 0)
            ELSE ISNULL(ps.Valuee, 0)
        END) <> 0
)
SELECT
    ProjectRows.*,
    SUM(ProjectRows.Balance) OVER (PARTITION BY ProjectRows.Salary_account) AS AccountSourceBalance
FROM ProjectRows;";
                command.Parameters.Add("@Year", SqlDbType.Int).Value = request.Year;
                command.Parameters.Add("@Month", SqlDbType.Int).Value = request.Month;
                command.Parameters.Add("@Sgn", SqlDbType.VarChar, 20).Value = request.Year.ToString() + request.Month.ToString();
                command.Parameters.Add("@UseDepartment", SqlDbType.Bit).Value = report.DistributionOptions.SalaryJournalByManagement;
                AddNullable(command, "@BranchId", SqlDbType.Int, request.BranchId);
                AddNullable(command, "@DepartmentId", SqlDbType.Int, request.DepartmentId);
                AddNullable(command, "@EmployeeId", SqlDbType.Int, request.EmployeeId);

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var balance = ReadDecimal(reader, "Balance");
                        if (balance == 0)
                        {
                            continue;
                        }

                        var employeeId = ReadNullableInt(reader, "EmpID");
                        var branchId = ReadNullableInt(reader, "BranchId");
                        var departmentId = ReadNullableInt(reader, "DepartmentID");
                        var projectId = ReadNullableInt(reader, "ProjectId");
                        var projectAccount = ReadString(reader, "Salary_account");
                        var projectName = ReadString(reader, "Project_name");
                        var componentNo = ReadNullableInt(reader, "ComponentNo");
                        var componentName = ReadString(reader, "ComponentName");
                        var mofradAccount = ReadString(reader, "MofradAccount");
                        var mofradAccount1 = ReadString(reader, "MofradAccount1");
                        var addOrDiscount = ReadInt(reader, "AddOrDiscount");
                        var sourceRows = ReadInt(reader, "SourceRows");
                        var sourceDays = ReadDecimal(reader, "SourceDays");
                        var accountSourceBalance = ReadDecimal(reader, "AccountSourceBalance");
                        var zmam = ReadBool(reader, "ZmamAccount");
                        var path = "BranchId=" + (branchId.HasValue ? branchId.Value.ToString() : "NULL")
                            + "; ProjectId=" + (projectId.HasValue ? projectId.Value.ToString() : "NULL")
                            + "; DepartmentId=" + (departmentId.HasValue ? departmentId.Value.ToString() : "NULL")
                            + "; EmployeeId=" + (employeeId.HasValue ? employeeId.Value.ToString() : "NULL");
                        var trigger = "ProjectEmployeeGV=True; ProJectMofrdSalar.MonthID/YearID matched salary period; emp_salary.sgn snapshot exists; SourceRows="
                            + sourceRows + "; SourceDays=" + sourceDays.ToString("0.####");

                        if (string.IsNullOrWhiteSpace(projectAccount) || string.IsNullOrWhiteSpace(mofradAccount))
                        {
                            report.DistributionMismatchCategories.Add(new PayrollDistributionMismatchSummary { Category = "fallback account usage", Count = 1, DebitDiff = Math.Abs(balance), CreditDiff = Math.Abs(balance) });
                            continue;
                        }

                        if (!ShouldReplayProjectMofrdAccount(report, projectAccount, accountSourceBalance))
                        {
                            report.DistributionMismatchCategories.Add(new PayrollDistributionMismatchSummary
                            {
                                Category = "missing VB6 branch",
                                Count = 1,
                                DebitDiff = -Math.Abs(balance),
                                CreditDiff = -Math.Abs(balance)
                            });
                            continue;
                        }

                        if (zmam || addOrDiscount == 0)
                        {
                            AddReplayLine(report, projectAccount, balance, false, branchId, departmentId, employeeId, componentNo, componentName,
                                zmam ? "PROJECT_MOFRD_ZMAM_PROJECT_DEBIT" : "PROJECT_MOFRD_ADDITION_PROJECT_DEBIT",
                                "projects.Salary_account from ProJectMofrdSalar.ProjID",
                                trigger + (zmam ? "; ZmamAccount=True" : "; AddOrDiscount=0"),
                                "VB6 project salary posting uses ProJectMofrdSalar as the project allocation snapshot and debits the selected project's salary account.",
                                projectId,
                                "ProJectMofrdSalar",
                                path,
                                "Project salary account override from projects.Salary_account (" + projectName + ")",
                                string.Empty,
                                "project routing mismatch");
                            AddReplayLine(report, mofradAccount, balance, true, branchId, departmentId, employeeId, componentNo, componentName,
                                zmam ? "PROJECT_MOFRD_ZMAM_COMPONENT_CREDIT" : "PROJECT_MOFRD_ADDITION_COMPONENT_CREDIT",
                                "mofrad.Account_Code via mofrdat.mofrad_type",
                                trigger + (zmam ? "; ZmamAccount=True" : "; AddOrDiscount=0"),
                                "VB6 creates the counter-line against the source component account for project salary allocation.",
                                null,
                                "ProJectMofrdSalar",
                                "BranchId=" + (branchId.HasValue ? branchId.Value.ToString() : "NULL") + "; ProjectId=NULL; DepartmentId=" + (departmentId.HasValue ? departmentId.Value.ToString() : "NULL") + "; EmployeeId=" + (employeeId.HasValue ? employeeId.Value.ToString() : "NULL"),
                                "Component account from mofrad",
                                string.Empty,
                                "component account mismatch");
                            continue;
                        }

                        var creditAccount = report.DistributionOptions.ProjectDiscountPolicy == 1 && !string.IsNullOrWhiteSpace(mofradAccount1)
                            ? mofradAccount1
                            : projectAccount;
                        AddReplayLine(report, mofradAccount, balance, false, branchId, departmentId, employeeId, componentNo, componentName,
                            "PROJECT_MOFRD_DEDUCTION_COMPONENT_DEBIT",
                            "mofrad.Account_Code via mofrdat.mofrad_type",
                            trigger + "; AddOrDiscount=-1",
                            "VB6 project salary posting debits the component account for project-based deductions.",
                            null,
                            "ProJectMofrdSalar",
                            "BranchId=" + (branchId.HasValue ? branchId.Value.ToString() : "NULL") + "; ProjectId=NULL; DepartmentId=" + (departmentId.HasValue ? departmentId.Value.ToString() : "NULL") + "; EmployeeId=" + (employeeId.HasValue ? employeeId.Value.ToString() : "NULL"),
                            "Project deduction counter-entry",
                            string.Empty,
                            "component account mismatch");
                        AddReplayLine(report, creditAccount, balance, true, branchId, departmentId, employeeId, componentNo, componentName,
                            "PROJECT_MOFRD_DEDUCTION_POLICY_CREDIT",
                            report.DistributionOptions.ProjectDiscountPolicy == 1 ? "mofrad.Account_Code1 via ProjectDiscountPolicy" : "projects.Salary_account from ProJectMofrdSalar.ProjID",
                            trigger + "; AddOrDiscount=-1; ProjectDiscountPolicy=" + report.DistributionOptions.ProjectDiscountPolicy,
                            "VB6 credits Account_Code1 when ProjectDiscountPolicy=1, otherwise the project salary account is used.",
                            report.DistributionOptions.ProjectDiscountPolicy == 1 ? null : projectId,
                            "ProJectMofrdSalar",
                            path,
                            report.DistributionOptions.ProjectDiscountPolicy == 1 ? "ProjectDiscountPolicy account override" : "Project salary account",
                            report.DistributionOptions.ProjectDiscountPolicy == 1 && string.IsNullOrWhiteSpace(mofradAccount1) ? "Account_Code1 empty; project salary account fallback" : string.Empty,
                            "project routing mismatch");
                    }
                }
            }
        }

        private static bool ShouldReplayProjectMofrdAccount(PayrollAccountingReplayReport report, string projectAccount, decimal accountSourceBalance)
        {
            if (report == null || report.LegacyTrace == null || report.LegacyTrace.VoucherLines == null || report.LegacyTrace.VoucherLines.Count == 0)
            {
                return true;
            }

            var legacyDebit = report.LegacyTrace.VoucherLines
                .Where(x => !x.IsCredit && string.Equals(x.AccountCode, projectAccount, StringComparison.OrdinalIgnoreCase))
                .Sum(x => x.Value);
            if (legacyDebit <= 0 || accountSourceBalance <= 0)
            {
                return false;
            }

            var tolerance = Math.Max(10m, Math.Abs(accountSourceBalance) * 0.02m);
            return Math.Abs(legacyDebit - accountSourceBalance) <= tolerance;
        }

        private static void AddReplayLine(PayrollAccountingReplayReport report, string accountCode, decimal value, bool isCredit, int? branchId, int? departmentId, int? employeeId, int? componentNo, string componentName, string ruleId, string accountRoutingPath, string trigger, string explanation, int? projectId = null, string allocationSource = null, string branchProjectDepartmentPath = null, string overrideReason = null, string fallbackReason = null, string distributionMismatchCategory = null)
        {
            if (value == 0)
            {
                return;
            }

            report.ReplayedLines.Add(new PayrollReplayedVoucherLine
            {
                LineNo = report.ReplayedLines.Count + 1,
                AccountCode = accountCode ?? string.Empty,
                Value = Math.Abs(value),
                IsCredit = isCredit,
                BranchId = branchId,
                DepartmentId = departmentId,
                ProjectId = projectId,
                EmployeeId = employeeId,
                ComponentNo = componentNo,
                ComponentName = componentName,
                RuleId = ruleId,
                AccountRoutingPath = accountRoutingPath,
                Trigger = trigger,
                Explanation = explanation,
                AllocationSource = allocationSource ?? "Salary grid/runtime row",
                BranchProjectDepartmentPath = branchProjectDepartmentPath ?? ("BranchId=" + (branchId.HasValue ? branchId.Value.ToString() : "NULL") + "; ProjectId=" + (projectId.HasValue ? projectId.Value.ToString() : "NULL") + "; DepartmentId=" + (departmentId.HasValue ? departmentId.Value.ToString() : "NULL")),
                OverrideReason = overrideReason ?? string.Empty,
                FallbackReason = fallbackReason ?? string.Empty,
                DistributionMismatchCategory = distributionMismatchCategory ?? ClassifyDistributionLine(accountCode, branchId, departmentId, projectId),
                LegacyBehaviorClassification = "temporary compatibility behavior",
                StabilityScore = 0.50m,
                HistoricalConsistencyScore = 0.50m,
                ReplayConfidenceScore = 0.50m,
                IsHistoricallyDeterministic = false,
                IsHistoricallyInconsistent = false,
                IsSafeForFuturePosting = false,
                LikelyLegacyBug = false,
                LikelyOperationalWorkaround = false,
                LegacyConsistencyExplanation = "Legacy consistency validation has not yet promoted this replay rule to posting-safe behavior."
            });
        }

        private static void BuildReplayComparisons(PayrollAccountingReplayReport report)
        {
            foreach (var key in report.LegacyTrace.VoucherLines.Select(x => x.AccountCode).Concat(report.ReplayedLines.Select(x => x.AccountCode)).Distinct())
            {
                var legacy = report.LegacyTrace.VoucherLines.Where(x => x.AccountCode == key).ToList();
                var replay = report.ReplayedLines.Where(x => x.AccountCode == key).ToList();
                report.AccountComparisons.Add(BuildComparison("Account", key, legacy, replay));
            }

            foreach (var key in report.LegacyTrace.VoucherLines.Select(x => x.BranchId.HasValue ? x.BranchId.Value.ToString() : "NULL")
                .Concat(report.ReplayedLines.Select(x => x.BranchId.HasValue ? x.BranchId.Value.ToString() : "NULL")).Distinct())
            {
                var legacy = report.LegacyTrace.VoucherLines.Where(x => (x.BranchId.HasValue ? x.BranchId.Value.ToString() : "NULL") == key).ToList();
                var replay = report.ReplayedLines.Where(x => (x.BranchId.HasValue ? x.BranchId.Value.ToString() : "NULL") == key).ToList();
                report.BranchComparisons.Add(BuildComparison("Branch", key, legacy, replay));
            }

            foreach (var key in report.LegacyTrace.VoucherLines.Select(x => x.DepartmentId.HasValue ? x.DepartmentId.Value.ToString() : "NULL")
                .Concat(report.ReplayedLines.Select(x => x.DepartmentId.HasValue ? x.DepartmentId.Value.ToString() : "NULL")).Distinct())
            {
                var legacy = report.LegacyTrace.VoucherLines.Where(x => (x.DepartmentId.HasValue ? x.DepartmentId.Value.ToString() : "NULL") == key).ToList();
                var replay = report.ReplayedLines.Where(x => (x.DepartmentId.HasValue ? x.DepartmentId.Value.ToString() : "NULL") == key).ToList();
                report.DepartmentComparisons.Add(BuildComparison("Department", key, legacy, replay));
            }

            foreach (var key in report.LegacyTrace.VoucherLines.Select(x => x.ProjectId.HasValue ? x.ProjectId.Value.ToString() : "NULL")
                .Concat(report.ReplayedLines.Select(x => x.ProjectId.HasValue ? x.ProjectId.Value.ToString() : "NULL")).Distinct())
            {
                var legacy = report.LegacyTrace.VoucherLines.Where(x => (x.ProjectId.HasValue ? x.ProjectId.Value.ToString() : "NULL") == key).ToList();
                var replay = report.ReplayedLines.Where(x => (x.ProjectId.HasValue ? x.ProjectId.Value.ToString() : "NULL") == key).ToList();
                report.ProjectComparisons.Add(BuildComparison("Project", key, legacy, replay));
            }

            report.DistributionMismatchCategories = report.AccountComparisons
                .Concat(report.BranchComparisons)
                .Concat(report.DepartmentComparisons)
                .Concat(report.ProjectComparisons)
                .Where(x => x.DebitDiff != 0 || x.CreditDiff != 0)
                .GroupBy(x => x.MismatchCategory)
                .Select(x => new PayrollDistributionMismatchSummary
                {
                    Category = x.Key,
                    Count = x.Count(),
                    DebitDiff = x.Sum(y => y.DebitDiff),
                    CreditDiff = x.Sum(y => y.CreditDiff)
                })
                .Concat(report.DistributionMismatchCategories)
                .GroupBy(x => x.Category)
                .Select(x => new PayrollDistributionMismatchSummary
                {
                    Category = x.Key,
                    Count = x.Sum(y => y.Count),
                    DebitDiff = x.Sum(y => y.DebitDiff),
                    CreditDiff = x.Sum(y => y.CreditDiff)
                })
                .OrderByDescending(x => Math.Abs(x.DebitDiff) + Math.Abs(x.CreditDiff))
                .ToList();
        }

        private static PayrollAccountingReplayComparison BuildComparison(string dimension, string key, IList<PayrollAccountingVoucherTrace> legacy, IList<PayrollReplayedVoucherLine> replay)
        {
            var legacyDebit = legacy.Where(x => !x.IsCredit).Sum(x => x.Value);
            var legacyCredit = legacy.Where(x => x.IsCredit).Sum(x => x.Value);
            var replayDebit = replay.Where(x => !x.IsCredit).Sum(x => x.Value);
            var replayCredit = replay.Where(x => x.IsCredit).Sum(x => x.Value);
            return new PayrollAccountingReplayComparison
            {
                Dimension = dimension,
                Key = key,
                LegacyDebit = legacyDebit,
                ReplayDebit = replayDebit,
                DebitDiff = replayDebit - legacyDebit,
                LegacyCredit = legacyCredit,
                ReplayCredit = replayCredit,
                CreditDiff = replayCredit - legacyCredit,
                LegacyLines = legacy.Count,
                ReplayLines = replay.Count,
                MismatchCategory = ClassifyDistributionComparison(dimension, key, legacy, replay),
                Explanation = "Read-only distribution comparison for " + dimension + "=" + key + ". Differences classify routing gaps only; no posting is performed.",
                LegacyBehaviorClassification = "temporary compatibility behavior",
                StabilityScore = 0.50m,
                HistoricalConsistencyScore = 0.50m,
                ReplayConfidenceScore = 0.50m,
                IsHistoricallyDeterministic = false,
                IsHistoricallyInconsistent = false,
                IsSafeForFuturePosting = false,
                LikelyLegacyBug = false,
                LikelyOperationalWorkaround = false,
                Recommendation = "preserve temporarily"
            };
        }

        private void AnalyzeLegacyConsistency(SqlConnection connection, SalaryRunRequest request, PayrollAccountingReplayReport report)
        {
            if (!TableExists(connection, "ProJectMofrdSalar") || !TableExists(connection, "projects") || !TableExists(connection, "TblEmployee") || !TableExists(connection, "Notes") || !TableExists(connection, "DOUBLE_ENTREY_VOUCHERS"))
            {
                report.LegacyConsistencySummaries.Add(new PayrollLegacyConsistencySummary
                {
                    RuleFamily = "Project allocation",
                    LegacyBehaviorClassification = "missing replay logic",
                    Recommendation = "requires business approval",
                    Explanation = "One or more legacy tables required for historical consistency analysis were not found."
                });
                return;
            }

            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
WITH Alloc AS
(
    SELECT
        CONVERT(varchar(4), ps.YearID) + CONVERT(varchar(2), ps.MonthID) AS SalaryKey,
        ps.YearID,
        ps.MonthID,
        e.BranchId,
        ps.ProjID AS ProjectId,
        p.Salary_account AS AccountCode,
        SUM(CASE
                WHEN ISNULL(ps.Total, 0) <> 0 THEN ISNULL(ps.Total, 0)
                WHEN ISNULL(ps.NoDay, 0) <> 0 THEN ISNULL(ps.NoDay, 0) * ISNULL(ps.Valuee, 0)
                ELSE ISNULL(ps.Valuee, 0)
            END) AS AllocationValue,
        COUNT(1) AS AllocationRows
    FROM dbo.ProJectMofrdSalar ps WITH (NOLOCK)
    INNER JOIN dbo.TblEmployee e WITH (NOLOCK) ON e.Emp_ID = ps.EmpID
    LEFT JOIN dbo.projects p WITH (NOLOCK) ON p.id = ps.ProjID
    WHERE ps.YearID BETWEEN @Year - 1 AND @Year
      AND (@BranchId IS NULL OR e.BranchId = @BranchId)
      AND (@EmployeeId IS NULL OR ps.EmpID = @EmployeeId)
      AND ISNULL(ps.ProjID, 0) <> 0
    GROUP BY ps.YearID, ps.MonthID, e.BranchId, ps.ProjID, p.Salary_account
),
Voucher AS
(
    SELECT
        CONVERT(varchar(20), n.salary) AS SalaryKey,
        ISNULL(d.branch_id, n.branch_no) AS BranchId,
        d.project_id AS ProjectId,
        d.Account_Code AS AccountCode,
        SUM(CASE WHEN ISNULL(d.Credit_Or_Debit, 0) = 0 THEN ISNULL(d.Value, 0) ELSE 0 END) AS VoucherDebit,
        COUNT(1) AS VoucherRows
    FROM dbo.Notes n WITH (NOLOCK)
    INNER JOIN dbo.DOUBLE_ENTREY_VOUCHERS d WITH (NOLOCK) ON d.Notes_ID = n.NoteID
    WHERE n.NoteType = 66
      AND (CONVERT(varchar(20), n.salary) LIKE CONVERT(varchar(4), @Year - 1) + '%'
       OR CONVERT(varchar(20), n.salary) LIKE CONVERT(varchar(4), @Year) + '%')
    GROUP BY CONVERT(varchar(20), n.salary), ISNULL(d.branch_id, n.branch_no), d.project_id, d.Account_Code
),
Joined AS
(
    SELECT
        ISNULL(a.SalaryKey, v.SalaryKey) AS SalaryKey,
        ISNULL(a.BranchId, v.BranchId) AS BranchId,
        ISNULL(a.ProjectId, v.ProjectId) AS ProjectId,
        ISNULL(a.AccountCode, v.AccountCode) AS AccountCode,
        ISNULL(a.AllocationValue, 0) AS AllocationValue,
        ISNULL(v.VoucherDebit, 0) AS VoucherDebit,
        CASE WHEN a.AccountCode IS NOT NULL THEN 1 ELSE 0 END AS HasAllocation,
        CASE WHEN v.AccountCode IS NOT NULL THEN 1 ELSE 0 END AS HasVoucher
    FROM Alloc a
    FULL OUTER JOIN Voucher v ON v.SalaryKey = a.SalaryKey
        AND ISNULL(v.BranchId, 0) = ISNULL(a.BranchId, 0)
        AND ISNULL(v.AccountCode, '') = ISNULL(a.AccountCode, '')
        AND (ISNULL(v.ProjectId, 0) = ISNULL(a.ProjectId, 0) OR v.ProjectId IS NULL)
)
SELECT TOP (100)
    AccountCode,
    ProjectId,
    BranchId,
    SUM(HasAllocation) AS PeriodsWithAllocation,
    SUM(HasVoucher) AS PeriodsWithVoucherFootprint,
    SUM(CASE WHEN HasAllocation = 1 AND HasVoucher = 1 THEN 1 ELSE 0 END) AS PeriodsWithBoth,
    SUM(CASE WHEN HasAllocation = 1 AND HasVoucher = 0 THEN 1 ELSE 0 END) AS PeriodsWithAllocationOnly,
    SUM(CASE WHEN HasAllocation = 0 AND HasVoucher = 1 THEN 1 ELSE 0 END) AS PeriodsWithVoucherOnly,
    SUM(AllocationValue) AS AllocationTotal,
    SUM(VoucherDebit) AS VoucherDebitTotal
FROM Joined
WHERE AccountCode IS NOT NULL
  AND (@BranchId IS NULL OR BranchId = @BranchId)
GROUP BY AccountCode, ProjectId, BranchId
HAVING SUM(HasAllocation) > 0 OR SUM(HasVoucher) > 0
ORDER BY ABS(SUM(AllocationValue) - SUM(VoucherDebit)) DESC;";
                command.Parameters.Add("@Year", SqlDbType.Int).Value = request.Year;
                AddNullable(command, "@BranchId", SqlDbType.Int, request.BranchId);
                AddNullable(command, "@EmployeeId", SqlDbType.Int, request.EmployeeId);

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var allocationPeriods = ReadInt(reader, "PeriodsWithAllocation");
                        var voucherPeriods = ReadInt(reader, "PeriodsWithVoucherFootprint");
                        var bothPeriods = ReadInt(reader, "PeriodsWithBoth");
                        var allocationOnly = ReadInt(reader, "PeriodsWithAllocationOnly");
                        var voucherOnly = ReadInt(reader, "PeriodsWithVoucherOnly");
                        var allocationTotal = ReadDecimal(reader, "AllocationTotal");
                        var voucherTotal = ReadDecimal(reader, "VoucherDebitTotal");
                        var observedPeriods = Math.Max(1, Math.Max(allocationPeriods, voucherPeriods));
                        var historicalConsistency = Math.Round((decimal)bothPeriods / observedPeriods, 4);
                        var amountBase = Math.Max(Math.Abs(allocationTotal), Math.Abs(voucherTotal));
                        var amountScore = amountBase == 0 ? 1m : Math.Max(0m, 1m - (Math.Abs(allocationTotal - voucherTotal) / amountBase));
                        var stabilityScore = Math.Round((historicalConsistency + amountScore) / 2m, 4);
                        var classification = ClassifyLegacyConsistency(allocationPeriods, voucherPeriods, bothPeriods, allocationOnly, voucherOnly, allocationTotal, voucherTotal);

                        report.LegacyConsistencySummaries.Add(new PayrollLegacyConsistencySummary
                        {
                            RuleFamily = "Project allocation / salary account",
                            AccountCode = ReadString(reader, "AccountCode"),
                            ProjectId = ReadNullableInt(reader, "ProjectId"),
                            BranchId = ReadNullableInt(reader, "BranchId"),
                            PeriodsWithAllocation = allocationPeriods,
                            PeriodsWithVoucherFootprint = voucherPeriods,
                            PeriodsWithBoth = bothPeriods,
                            PeriodsWithAllocationOnly = allocationOnly,
                            PeriodsWithVoucherOnly = voucherOnly,
                            AllocationTotal = allocationTotal,
                            VoucherDebitTotal = voucherTotal,
                            AmountDifference = allocationTotal - voucherTotal,
                            StabilityScore = stabilityScore,
                            HistoricalConsistencyScore = historicalConsistency,
                            ReplayConfidenceScore = Math.Round((stabilityScore + historicalConsistency) / 2m, 4),
                            LegacyBehaviorClassification = classification,
                            Recommendation = RecommendLegacyConsistencyAction(classification),
                            Explanation = "Read-only consistency probe comparing ProJectMofrdSalar allocation footprint with posted VB6 salary voucher debit footprint across adjacent salary periods."
                        });
                    }
                }
            }
        }

        private static string ClassifyLegacyConsistency(int allocationPeriods, int voucherPeriods, int bothPeriods, int allocationOnly, int voucherOnly, decimal allocationTotal, decimal voucherTotal)
        {
            if (allocationPeriods > 0 && voucherPeriods == 0)
            {
                return "dormant legacy branch";
            }

            if (allocationOnly > 0 && voucherOnly == 0 && bothPeriods > 0)
            {
                return "partially-used legacy path";
            }

            if (voucherOnly > 0 && allocationPeriods == 0)
            {
                return "probable manual adjustment";
            }

            if (allocationOnly > 0 && voucherOnly > 0)
            {
                return "historically inconsistent behavior";
            }

            if (bothPeriods > 1 && Math.Abs(allocationTotal - voucherTotal) <= Math.Max(10m, Math.Abs(voucherTotal) * 0.02m))
            {
                return "valid legacy behavior";
            }

            if (bothPeriods > 0 && Math.Abs(allocationTotal - voucherTotal) > Math.Max(10m, Math.Abs(voucherTotal) * 0.10m))
            {
                return "duplicated allocation behavior";
            }

            if (bothPeriods > 0)
            {
                return "temporary compatibility behavior";
            }

            return "requires business approval";
        }

        private static string RecommendLegacyConsistencyAction(string classification)
        {
            switch (classification)
            {
                case "valid legacy behavior":
                    return "preserve";
                case "temporary compatibility behavior":
                case "partially-used legacy path":
                    return "replay conditionally";
                case "dormant legacy branch":
                    return "deprecate";
                case "duplicated allocation behavior":
                case "historically inconsistent behavior":
                case "probable manual adjustment":
                    return "requires business approval";
                default:
                    return "modernize safely";
            }
        }

        private static void ApplyLegacyConsistencyTrustModel(PayrollAccountingReplayReport report)
        {
            foreach (var comparison in report.AccountComparisons.Concat(report.BranchComparisons).Concat(report.DepartmentComparisons).Concat(report.ProjectComparisons))
            {
                ApplyLegacyConsistencyToComparison(report, comparison);
            }

            foreach (var line in report.ReplayedLines)
            {
                var summary = FindBestConsistencySummary(report, line.AccountCode, line.ProjectId, line.BranchId);
                ApplyLegacyConsistencyToLine(line, summary);
            }
        }

        private static void ApplyLegacyConsistencyToComparison(PayrollAccountingReplayReport report, PayrollAccountingReplayComparison comparison)
        {
            PayrollLegacyConsistencySummary summary = null;
            if (comparison.Dimension == "Account")
            {
                summary = FindBestConsistencySummary(report, comparison.Key, null, null);
            }
            else if (comparison.Dimension == "Project")
            {
                int projectId;
                if (int.TryParse(comparison.Key, out projectId))
                {
                    summary = report.LegacyConsistencySummaries.FirstOrDefault(x => x.ProjectId == projectId);
                }
            }

            if (summary != null)
            {
                comparison.LegacyBehaviorClassification = summary.LegacyBehaviorClassification;
                comparison.StabilityScore = summary.StabilityScore;
                comparison.HistoricalConsistencyScore = summary.HistoricalConsistencyScore;
                comparison.ReplayConfidenceScore = summary.ReplayConfidenceScore;
                comparison.Recommendation = summary.Recommendation;
                comparison.IsHistoricallyDeterministic = summary.HistoricalConsistencyScore >= 0.80m;
                comparison.IsHistoricallyInconsistent = summary.LegacyBehaviorClassification == "historically inconsistent behavior" || summary.LegacyBehaviorClassification == "duplicated allocation behavior";
                comparison.LikelyLegacyBug = summary.LegacyBehaviorClassification == "duplicated allocation behavior";
                comparison.LikelyOperationalWorkaround = summary.LegacyBehaviorClassification == "probable manual adjustment";
                comparison.IsSafeForFuturePosting = summary.LegacyBehaviorClassification == "valid legacy behavior" && comparison.ReplayConfidenceScore >= 0.80m;
                comparison.Explanation += " Legacy consistency: " + summary.LegacyBehaviorClassification + "; recommendation: " + summary.Recommendation + ".";
                return;
            }

            var absoluteDiff = Math.Abs(comparison.DebitDiff) + Math.Abs(comparison.CreditDiff);
            var baseAmount = Math.Max(1m, Math.Abs(comparison.LegacyDebit) + Math.Abs(comparison.LegacyCredit));
            var amountScore = Math.Max(0m, 1m - (absoluteDiff / baseAmount));
            comparison.StabilityScore = Math.Round(amountScore, 4);
            comparison.HistoricalConsistencyScore = comparison.DebitDiff == 0 && comparison.CreditDiff == 0 ? 1m : 0.35m;
            comparison.ReplayConfidenceScore = Math.Round((comparison.StabilityScore + comparison.HistoricalConsistencyScore) / 2m, 4);
            comparison.LegacyBehaviorClassification = comparison.DebitDiff == 0 && comparison.CreditDiff == 0 ? "valid legacy behavior" : "requires business approval";
            comparison.Recommendation = comparison.LegacyBehaviorClassification == "valid legacy behavior" ? "preserve" : "preserve temporarily";
            comparison.IsHistoricallyDeterministic = comparison.HistoricalConsistencyScore >= 0.80m;
            comparison.IsHistoricallyInconsistent = comparison.LegacyBehaviorClassification == "requires business approval";
            comparison.IsSafeForFuturePosting = false;
        }

        private static PayrollLegacyConsistencySummary FindBestConsistencySummary(PayrollAccountingReplayReport report, string accountCode, int? projectId, int? branchId)
        {
            if (report == null || string.IsNullOrWhiteSpace(accountCode))
            {
                return null;
            }

            return report.LegacyConsistencySummaries
                .Where(x => string.Equals(x.AccountCode, accountCode, StringComparison.OrdinalIgnoreCase))
                .Where(x => !projectId.HasValue || !x.ProjectId.HasValue || x.ProjectId == projectId)
                .Where(x => !branchId.HasValue || !x.BranchId.HasValue || x.BranchId == branchId)
                .OrderByDescending(x => x.ReplayConfidenceScore)
                .FirstOrDefault();
        }

        private static void ApplyLegacyConsistencyToLine(PayrollReplayedVoucherLine line, PayrollLegacyConsistencySummary summary)
        {
            if (summary == null)
            {
                line.LegacyBehaviorClassification = "temporary compatibility behavior";
                line.StabilityScore = 0.50m;
                line.HistoricalConsistencyScore = 0.50m;
                line.ReplayConfidenceScore = 0.50m;
                line.IsHistoricallyDeterministic = false;
                line.IsHistoricallyInconsistent = false;
                line.IsSafeForFuturePosting = false;
                line.LegacyConsistencyExplanation = "No repeated historical footprint was found for this replay line; keep as read-only compatibility behavior.";
                return;
            }

            line.LegacyBehaviorClassification = summary.LegacyBehaviorClassification;
            line.StabilityScore = summary.StabilityScore;
            line.HistoricalConsistencyScore = summary.HistoricalConsistencyScore;
            line.ReplayConfidenceScore = summary.ReplayConfidenceScore;
            line.IsHistoricallyDeterministic = summary.HistoricalConsistencyScore >= 0.80m;
            line.IsHistoricallyInconsistent = summary.LegacyBehaviorClassification == "historically inconsistent behavior" || summary.LegacyBehaviorClassification == "duplicated allocation behavior";
            line.IsSafeForFuturePosting = summary.LegacyBehaviorClassification == "valid legacy behavior" && summary.ReplayConfidenceScore >= 0.80m;
            line.LikelyLegacyBug = summary.LegacyBehaviorClassification == "duplicated allocation behavior";
            line.LikelyOperationalWorkaround = summary.LegacyBehaviorClassification == "probable manual adjustment";
            line.LegacyConsistencyExplanation = summary.Explanation + " Classification=" + summary.LegacyBehaviorClassification + "; recommendation=" + summary.Recommendation + ".";
        }

        private static string ClassifyDistributionComparison(string dimension, string key, IList<PayrollAccountingVoucherTrace> legacy, IList<PayrollReplayedVoucherLine> replay)
        {
            if (dimension == "Project" && key != "NULL")
            {
                return "project routing mismatch";
            }

            if (dimension == "Department" && key != "NULL")
            {
                return "department allocation mismatch";
            }

            if (dimension == "Branch" && key != "NULL")
            {
                return "branch override mismatch";
            }

            if (replay.Any(x => string.IsNullOrWhiteSpace(x.AccountCode)) || legacy.Any(x => string.IsNullOrWhiteSpace(x.AccountCode)))
            {
                return "fallback account usage";
            }

            if (dimension == "Account" && replay.Any(x => x.AccountRoutingPath != null && x.AccountRoutingPath.IndexOf("Account_Code1", StringComparison.OrdinalIgnoreCase) >= 0))
            {
                return "employee accrued account conflict";
            }

            return "missing allocation policy";
        }

        private static string ClassifyDistributionLine(string accountCode, int? branchId, int? departmentId, int? projectId)
        {
            if (projectId.HasValue)
            {
                return "project routing mismatch";
            }

            if (departmentId.HasValue)
            {
                return "department allocation mismatch";
            }

            if (!branchId.HasValue)
            {
                return "missing allocation policy";
            }

            if (string.IsNullOrWhiteSpace(accountCode))
            {
                return "fallback account usage";
            }

            return "branch override mismatch";
        }

        public SalaryRunPreview PreviewSalaryRun(SalaryRunRequest request)
        {
            request = NormalizeSalaryRequest(request);
            if (request.PayrollRunId.HasValue && request.PayrollRunId.Value > 0 && !request.RebuildEmployees)
            {
                var snapshot = LoadPayrollRunSnapshot(request);
                if (snapshot.Rows.Count > 0)
                {
                    return snapshot;
                }
            }

            var preview = PreviewSalaryRunCompatibility(request);
            ApplyPayrollRunEmployeeSelection(preview);
            return preview;
        }

        private SalaryRunPreview LoadPayrollRunSnapshot(SalaryRunRequest request)
        {
            var preview = new SalaryRunPreview { Request = request, PayrollRunId = request.PayrollRunId, IsSnapshotRun = true };
            using (var connection = OpenConnection())
            {
                if (!TableExists(connection, "PayrollRunHeader") || !TableExists(connection, "PayrollRunEmployees"))
                {
                    return preview;
                }

                var hasVacationSnapshotColumns = ColumnExists(connection, "PayrollRunEmployees", "VacationDays")
                    && ColumnExists(connection, "PayrollRunEmployees", "VacationDeduction")
                    && ColumnExists(connection, "PayrollRunEmployees", "VacationSalaryValue")
                    && ColumnExists(connection, "PayrollRunEmployees", "AbsentDays")
                    && ColumnExists(connection, "PayrollRunEmployees", "CountDays")
                    && ColumnExists(connection, "PayrollRunEmployees", "RemainingDays");
                var vacationSnapshotSelect = hasVacationSnapshotColumns
                    ? @"d.VacationDays, d.VacationDeduction, d.VacationSalaryValue, d.AbsentDays, d.CountDays, d.RemainingDays,"
                    : @"CONVERT(money, 0) AS VacationDays, CONVERT(money, 0) AS VacationDeduction, CONVERT(money, 0) AS VacationSalaryValue, CONVERT(money, 0) AS AbsentDays, CONVERT(money, 0) AS CountDays, CONVERT(money, 0) AS RemainingDays,";

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"
SELECT h.PayrollRunId, h.RunName, h.PeriodYear, h.PeriodMonth, h.BranchId, h.DepartmentId, h.IsPosted,
       d.PayrollRunEmployeeId, d.EmployeeId, d.EmployeeCode, d.EmployeeName, d.BranchId AS RowBranchId, d.BranchName,
       d.DepartmentId AS RowDepartmentId, d.DepartmentName, d.ProjectId, d.BasicSalary, d.Allowances,
       d.VariableAdditions, d.Deductions, d.Advances, d.MedicalInsurance, d.MedicalInsuranceCompanyCost,
       d.TotalBeforeDeductions, d.TotalDeductions, d.NetSalary, d.EmployeeStatusAtRunTime,
       " + vacationSnapshotSelect + @"
       d.ExistingSalaryRowId, d.IsPosted AS RowPosted, d.AccountCode, d.AccruedSalaryAccountCode,
       d.AdvancePaymentAccountCode, d.MedicalInsuranceEmployeeAccountCode, d.MedicalInsuranceCompanyAccountCode
FROM dbo.PayrollRunHeader h WITH (NOLOCK)
INNER JOIN dbo.PayrollRunEmployees d WITH (NOLOCK) ON d.PayrollRunId = h.PayrollRunId
WHERE h.PayrollRunId = @PayrollRunId
  AND ISNULL(h.IsCancelled, 0) = 0
  AND (@PostingStatus = N'' OR (@PostingStatus = N'Posted' AND ISNULL(d.IsPosted, 0) = 1) OR (@PostingStatus = N'Unposted' AND ISNULL(d.IsPosted, 0) = 0))
ORDER BY d.EmployeeCode, d.EmployeeId;";
                    command.Parameters.Add("@PayrollRunId", SqlDbType.Int).Value = request.PayrollRunId.Value;
                    command.Parameters.Add("@PostingStatus", SqlDbType.NVarChar, 20).Value = request.PostingStatus ?? string.Empty;
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            if (!preview.PayrollRunId.HasValue)
                            {
                                preview.PayrollRunId = ReadNullableInt(reader, "PayrollRunId");
                            }

                            preview.RunName = ReadString(reader, "RunName");
                            var row = new SalaryRunEmployeeRow
                            {
                                PayrollRunId = ReadNullableInt(reader, "PayrollRunId"),
                                PayrollRunEmployeeId = ReadNullableInt(reader, "PayrollRunEmployeeId"),
                                Selected = true,
                                EmployeeId = ReadInt(reader, "EmployeeId"),
                                EmployeeCode = ReadString(reader, "EmployeeCode"),
                                EmployeeName = ReadString(reader, "EmployeeName"),
                                BranchId = ReadNullableInt(reader, "RowBranchId"),
                                BranchName = ReadString(reader, "BranchName"),
                                DepartmentId = ReadNullableInt(reader, "RowDepartmentId"),
                                DepartmentName = ReadString(reader, "DepartmentName"),
                                ProjectId = ReadNullableInt(reader, "ProjectId"),
                                BasicSalary = ReadDecimal(reader, "BasicSalary"),
                                SalaryAllowances = ReadDecimal(reader, "Allowances"),
                                VariableAdditions = ReadDecimal(reader, "VariableAdditions"),
                                ExistingDiscounts = ReadDecimal(reader, "Deductions"),
                                AdvanceDeduction = ReadDecimal(reader, "Advances"),
                                MedicalInsuranceDeduction = ReadDecimal(reader, "MedicalInsurance"),
                                MedicalInsuranceCompanyCost = ReadDecimal(reader, "MedicalInsuranceCompanyCost"),
                                VacationDays = ReadDecimal(reader, "VacationDays"),
                                VacationDeduction = ReadDecimal(reader, "VacationDeduction"),
                                VacationSalaryValue = ReadDecimal(reader, "VacationSalaryValue"),
                                AbsentDays = ReadDecimal(reader, "AbsentDays"),
                                CountDays = ReadDecimal(reader, "CountDays"),
                                RemainingDays = ReadDecimal(reader, "RemainingDays"),
                                TotalBeforeDeductions = ReadDecimal(reader, "TotalBeforeDeductions"),
                                TotalDeductions = ReadDecimal(reader, "TotalDeductions"),
                                NetSalary = ReadDecimal(reader, "NetSalary"),
                                EmployeeStatusAtRunTime = ReadString(reader, "EmployeeStatusAtRunTime"),
                                ExistingSalaryRowId = ReadNullableInt(reader, "ExistingSalaryRowId"),
                                IsApproved = ReadBool(reader, "RowPosted"),
                                EmployeeAccountCode = ReadString(reader, "AccountCode"),
                                AccruedSalaryAccountCode = ReadString(reader, "AccruedSalaryAccountCode"),
                                AdvancePaymentAccountCode = ReadString(reader, "AdvancePaymentAccountCode"),
                                MedicalInsuranceEmployeeAccountCode = ReadString(reader, "MedicalInsuranceEmployeeAccountCode"),
                                MedicalInsuranceCompanyAccountCode = ReadString(reader, "MedicalInsuranceCompanyAccountCode"),
                                IsLegacySnapshot = true,
                                CompatibilityStatus = "PayrollRunSnapshot"
                            };
                            preview.Rows.Add(row);
                            preview.HasExistingApprovedRows = preview.HasExistingApprovedRows || row.IsApproved;
                        }
                    }
                }
            }

            using (var connection = OpenConnection())
            {
                AttachPayrollRunAdvanceInstallments(connection, preview);
            }
            RecalculatePreviewTotals(preview);
            BuildJournalPreview(preview);
            using (var connection = OpenConnection())
            {
                AttachJournalPreviewAccountInfo(connection, preview);
            }

            preview.Message = "تم تحميل المسير من Snapshot محفوظ، ولن تتغير قائمة الموظفين إلا عند اختيار إعادة تكوين الموظفين.";
            ApplySalaryPreviewPayloadLimits(preview, request);
            return preview;
        }

        private void ApplyPayrollRunEmployeeSelection(SalaryRunPreview preview)
        {
            if (preview == null || preview.Rows == null || preview.Rows.Count == 0)
            {
                return;
            }

            var request = preview.Request ?? new SalaryRunRequest();
            var manualIds = ParseEmployeeIdList(request.ManualEmployeeIds);
            if (manualIds.Count > 0)
            {
                preview.Rows = preview.Rows.Where(x => manualIds.Contains(x.EmployeeId)).ToList();
            }

            if ((request.ExcludeAlreadyIncluded || request.OnlyUnincluded) && !request.AllowDuplicateEmployees)
            {
                var included = GetEmployeesAlreadyIncludedInPayrollRun(request);
                if (request.PayrollRunId.HasValue)
                {
                    var existingRun = GetPayrollRunEmployeeIds(request.PayrollRunId.Value);
                    foreach (var employeeId in existingRun)
                    {
                        included.Remove(employeeId);
                    }
                }

                var before = preview.Rows.Count;
                preview.Rows = preview.Rows.Where(x => !included.Contains(x.EmployeeId)).ToList();
                preview.ExcludedDuplicateEmployees = before - preview.Rows.Count;
            }

            RecalculatePreviewTotals(preview);
            preview.JournalPreview.Clear();
            BuildJournalPreview(preview);
            using (var connection = OpenConnection())
            {
                AttachJournalPreviewAccountInfo(connection, preview);
            }

            if (preview.ExcludedDuplicateEmployees > 0)
            {
                preview.CompatibilityWarnings.Add(new PayrollCompatibilityWarning
                {
                    Code = "DuplicateEmployeesExcluded",
                    Message = "تم استبعاد " + preview.ExcludedDuplicateEmployees.ToString() + " موظف سبق إدراجه في مسير آخر لنفس الشهر."
                });
            }
        }

        private static void RecalculatePreviewTotals(SalaryRunPreview preview)
        {
            preview.TotalBasic = 0;
            preview.TotalAdditions = 0;
            preview.TotalDeductions = 0;
            preview.TotalMedicalInsurance = 0;
            preview.TotalMedicalInsuranceCompanyCost = 0;
            preview.TotalAdvance = 0;
            preview.TotalNet = 0;
            foreach (var row in preview.Rows)
            {
                preview.TotalBasic += row.BasicSalary;
                preview.TotalAdditions += row.SalaryAllowances + row.VariableAdditions;
                preview.TotalAdvance += row.AdvanceDeduction;
                preview.TotalMedicalInsurance += row.MedicalInsuranceDeduction;
                preview.TotalMedicalInsuranceCompanyCost += row.MedicalInsuranceCompanyCost;
                preview.TotalDeductions += row.TotalDeductions;
                preview.TotalNet += row.NetSalary;
            }
        }

        private HashSet<int> GetEmployeesAlreadyIncludedInPayrollRun(SalaryRunRequest request)
        {
            var result = new HashSet<int>();
            using (var connection = OpenConnection())
            {
                if (!TableExists(connection, "PayrollRunHeader") || !TableExists(connection, "PayrollRunEmployees"))
                {
                    return result;
                }

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"
SELECT DISTINCT d.EmployeeId
FROM dbo.PayrollRunHeader h WITH (NOLOCK)
INNER JOIN dbo.PayrollRunEmployees d WITH (NOLOCK) ON d.PayrollRunId = h.PayrollRunId
WHERE h.PeriodYear = @Year
  AND h.PeriodMonth = @Month
  AND ISNULL(h.IsCancelled, 0) = 0
  AND (@BranchId IS NULL OR d.BranchId = @BranchId)
  AND (@DepartmentId IS NULL OR d.DepartmentId = @DepartmentId)
  AND (@EmployeeId IS NULL OR d.EmployeeId = @EmployeeId);";
                    command.Parameters.Add("@Year", SqlDbType.Int).Value = request.Year;
                    command.Parameters.Add("@Month", SqlDbType.Int).Value = request.Month;
                    AddNullable(command, "@BranchId", SqlDbType.Int, request.BranchId);
                    AddNullable(command, "@DepartmentId", SqlDbType.Int, request.DepartmentId);
                    AddNullable(command, "@EmployeeId", SqlDbType.Int, request.EmployeeId);
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            result.Add(ReadInt(reader, "EmployeeId"));
                        }
                    }
                }
            }

            return result;
        }

        private HashSet<int> GetPayrollRunEmployeeIds(int payrollRunId)
        {
            var result = new HashSet<int>();
            using (var connection = OpenConnection())
            {
                if (!TableExists(connection, "PayrollRunEmployees"))
                {
                    return result;
                }

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = "SELECT EmployeeId FROM dbo.PayrollRunEmployees WITH (NOLOCK) WHERE PayrollRunId = @PayrollRunId;";
                    command.Parameters.Add("@PayrollRunId", SqlDbType.Int).Value = payrollRunId;
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            result.Add(ReadInt(reader, "EmployeeId"));
                        }
                    }
                }
            }

            return result;
        }

        public PayrollSalarySheetReport BuildPayrollSalarySheetReport(SalaryRunRequest request)
        {
            request = NormalizeSalaryRequest(request);
            var preview = PreviewSalaryRun(request);
            var posting = BuildPayrollPostingDryRun(ToPostingRequest(request));
            var periodFrom = new DateTime(request.Year, request.Month, 1);
            var periodTo = periodFrom.AddMonths(1).AddDays(-1);
            var report = new PayrollSalarySheetReport
            {
                Request = request,
                ReportTitle = "مسير رواتب",
                PeriodLabel = "عن شهر " + request.Month.ToString() + " سنة " + request.Year.ToString(),
                PeriodFrom = periodFrom,
                PeriodTo = periodTo,
                GeneratedAt = DateTime.Now,
                TotalRows = preview.TotalRows,
                PostedRows = preview.Rows.Count(x => x.IsApproved),
                UnpostedRows = preview.Rows.Count(x => !x.IsApproved),
                TotalBasic = preview.TotalBasic,
                TotalAllowances = preview.TotalAdditions,
                TotalAdvances = preview.TotalAdvance,
                TotalInsurance = preview.TotalMedicalInsurance,
                TotalNet = preview.TotalNet,
                JournalDebitTotal = posting.DebitTotal,
                JournalCreditTotal = posting.CreditTotal,
                JournalBalance = posting.Balance,
                JournalBalanced = Math.Abs(posting.Balance) <= 0.01m
            };
            report.TotalDeductions = preview.Rows.Sum(x => x.ExistingDiscounts + x.MedicalInsuranceDeduction);

            foreach (var row in preview.Rows)
            {
                report.Rows.Add(new PayrollSalarySheetRow
                {
                    EmployeeId = row.EmployeeId,
                    EmployeeCode = row.EmployeeCode,
                    EmployeeName = row.EmployeeName,
                    BranchName = row.BranchName,
                    DepartmentName = row.DepartmentName,
                    BasicSalary = row.BasicSalary,
                    Allowances = row.SalaryAllowances + row.VariableAdditions,
                    Deductions = row.ExistingDiscounts + row.MedicalInsuranceDeduction,
                    Advances = row.AdvanceDeduction,
                    Insurance = row.MedicalInsuranceDeduction,
                    NetSalary = row.NetSalary,
                    IsPosted = row.IsApproved,
                    PostingStatus = row.IsApproved ? "مرحل" : "غير مرحل"
                });
            }

            return report;
        }

        public IList<PayrollRunSummary> GetPayrollRuns(SalaryRunRequest request)
        {
            request = NormalizeSalaryRequest(request);
            var rows = new List<PayrollRunSummary>();
            using (var connection = OpenConnection())
            {
                if (!TableExists(connection, "PayrollRunHeader") || !TableExists(connection, "PayrollRunEmployees"))
                {
                    return rows;
                }

                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"
SELECT TOP (100)
       h.PayrollRunId, h.RunName, h.PeriodYear, h.PeriodMonth, h.TotalBasic, h.TotalAllowances,
       h.TotalDeductions, h.TotalNet, h.IsPosted, h.NoteId, h.CreatedAt, COUNT(d.PayrollRunEmployeeId) AS EmployeesCount
FROM dbo.PayrollRunHeader h WITH (NOLOCK)
LEFT JOIN dbo.PayrollRunEmployees d WITH (NOLOCK) ON d.PayrollRunId = h.PayrollRunId
WHERE h.PeriodYear = @Year
  AND h.PeriodMonth = @Month
  AND ISNULL(h.IsCancelled, 0) = 0
  AND (@BranchId IS NULL OR h.BranchId = @BranchId OR d.BranchId = @BranchId)
  AND (@DepartmentId IS NULL OR h.DepartmentId = @DepartmentId OR d.DepartmentId = @DepartmentId)
  AND (@EmployeeId IS NULL OR d.EmployeeId = @EmployeeId)
GROUP BY h.PayrollRunId, h.RunName, h.PeriodYear, h.PeriodMonth, h.TotalBasic, h.TotalAllowances,
         h.TotalDeductions, h.TotalNet, h.IsPosted, h.NoteId, h.CreatedAt
ORDER BY h.CreatedAt DESC, h.PayrollRunId DESC;";
                    command.Parameters.Add("@Year", SqlDbType.Int).Value = request.Year;
                    command.Parameters.Add("@Month", SqlDbType.Int).Value = request.Month;
                    AddNullable(command, "@BranchId", SqlDbType.Int, request.BranchId);
                    AddNullable(command, "@DepartmentId", SqlDbType.Int, request.DepartmentId);
                    AddNullable(command, "@EmployeeId", SqlDbType.Int, request.EmployeeId);
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            rows.Add(new PayrollRunSummary
                            {
                                PayrollRunId = ReadInt(reader, "PayrollRunId"),
                                RunName = ReadString(reader, "RunName"),
                                PeriodYear = ReadInt(reader, "PeriodYear"),
                                PeriodMonth = ReadInt(reader, "PeriodMonth"),
                                EmployeesCount = ReadInt(reader, "EmployeesCount"),
                                TotalBasic = ReadDecimal(reader, "TotalBasic"),
                                TotalAllowances = ReadDecimal(reader, "TotalAllowances"),
                                TotalDeductions = ReadDecimal(reader, "TotalDeductions"),
                                TotalNet = ReadDecimal(reader, "TotalNet"),
                                IsPosted = ReadBool(reader, "IsPosted"),
                                NoteId = ReadNullableInt(reader, "NoteId"),
                                CreatedAt = ReadNullableDate(reader, "CreatedAt").GetValueOrDefault()
                            });
                        }
                    }
                }
            }

            return rows;
        }

        public PayrollRunCompareResult ComparePayrollRuns(PayrollRunCompareRequest request)
        {
            if (request == null || request.FirstPayrollRunId <= 0 || request.SecondPayrollRunId <= 0)
            {
                throw new InvalidOperationException("يجب اختيار مسيرين صالحين للمقارنة.");
            }

            var first = LoadPayrollRunSnapshot(new SalaryRunRequest { PayrollRunId = request.FirstPayrollRunId, Year = DateTime.Today.Year, Month = DateTime.Today.Month });
            var second = LoadPayrollRunSnapshot(new SalaryRunRequest { PayrollRunId = request.SecondPayrollRunId, Year = DateTime.Today.Year, Month = DateTime.Today.Month });
            var result = new PayrollRunCompareResult
            {
                FirstPayrollRunId = request.FirstPayrollRunId,
                SecondPayrollRunId = request.SecondPayrollRunId
            };
            var firstByEmployee = first.Rows.ToDictionary(x => x.EmployeeId);
            var secondByEmployee = second.Rows.ToDictionary(x => x.EmployeeId);
            var common = firstByEmployee.Keys.Intersect(secondByEmployee.Keys).ToList();
            result.CommonEmployees = common.Count;
            result.EmployeesInFirstOnly = first.Rows.Where(x => !secondByEmployee.ContainsKey(x.EmployeeId)).ToList();
            result.EmployeesInSecondOnly = second.Rows.Where(x => !firstByEmployee.ContainsKey(x.EmployeeId)).ToList();
            result.FirstOnlyEmployees = result.EmployeesInFirstOnly.Count;
            result.SecondOnlyEmployees = result.EmployeesInSecondOnly.Count;
            foreach (var employeeId in common)
            {
                var a = firstByEmployee[employeeId];
                var b = secondByEmployee[employeeId];
                result.BasicDifference += b.BasicSalary - a.BasicSalary;
                result.AllowancesDifference += (b.SalaryAllowances + b.VariableAdditions) - (a.SalaryAllowances + a.VariableAdditions);
                result.DeductionsDifference += b.TotalDeductions - a.TotalDeductions;
                result.NetDifference += b.NetSalary - a.NetSalary;
            }
            result.JournalDebitDifference = second.JournalPreview.Sum(x => x.Debit) - first.JournalPreview.Sum(x => x.Debit);
            result.JournalCreditDifference = second.JournalPreview.Sum(x => x.Credit) - first.JournalPreview.Sum(x => x.Credit);
            return result;
        }

        private SalaryRunPreview PreviewSalaryRunCompatibility(SalaryRunRequest request)
        {
            request = NormalizeSalaryRequest(request);
            var preview = new SalaryRunPreview { Request = request };
            var sgn = request.Year.ToString() + request.Month.ToString();
            using (var connection = OpenConnection())
            {
                LoadCompatibilityRows(connection, preview, sgn);
                AttachCompatibilityComponents(connection, preview, sgn);
                AttachInsuranceCompatibilityTrace(connection, preview, sgn);
                AttachMedicalInsuranceCompatibility(connection, preview);
                AttachRuntimeAdvanceInstallments(connection, preview);
            }

            RecalculateCompatibilityFallbackRows(preview);
            foreach (var row in preview.Rows)
            {
                preview.TotalBasic += row.BasicSalary;
                preview.TotalAdditions += row.SalaryAllowances + row.VariableAdditions;
                preview.TotalAdvance += row.AdvanceDeduction;
                preview.TotalMedicalInsurance += row.MedicalInsuranceDeduction;
                preview.TotalMedicalInsuranceCompanyCost += row.MedicalInsuranceCompanyCost;
                preview.TotalDeductions += row.TotalDeductions;
                preview.TotalNet += row.NetSalary;
            }

            BuildJournalPreview(preview);
            using (var connection = OpenConnection())
            {
                AttachJournalPreviewAccountInfo(connection, preview);
            }
            preview.Message = preview.HasExistingApprovedRows
                ? "تم بناء معاينة المسير من بيانات الرواتب الحالية. الصفوف المعتمدة للقراءة فقط."
                : "تم بناء معاينة المسير من بيانات الرواتب الحالية مع إعادة تكوين البنود عند عدم وجود مسير محفوظ.";
            ApplySalaryPreviewPayloadLimits(preview, request);
            return preview;
        }

        private void AttachMedicalInsuranceCompatibility(SqlConnection connection, SalaryRunPreview preview)
        {
            if (preview.Rows.Count == 0
                || !TableExists(connection, "EmployeeMedicalInsurance")
                || !TableExists(connection, "MedicalInsurancePlans"))
            {
                return;
            }

            var request = preview.Request;
            var periodStart = new DateTime(request.Year, request.Month, 1);
            var periodEnd = periodStart.AddMonths(1).AddDays(-1);
            var employeeIds = preview.Rows.Select(x => x.EmployeeId).Distinct().ToList();
            var employeeCsv = string.Join(",", employeeIds.Select(x => x.ToString()).ToArray());
            var byEmployee = preview.Rows.ToDictionary(x => x.EmployeeId);
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
;WITH ActiveInsurance AS
(
    SELECT
        mi.*,
        ROW_NUMBER() OVER (PARTITION BY mi.EmpId ORDER BY mi.StartDate DESC, mi.Id DESC) AS rn
    FROM dbo.EmployeeMedicalInsurance mi WITH (NOLOCK)
    INNER JOIN dbo.MedicalInsurancePlans plx WITH (NOLOCK) ON plx.PlanId = mi.PlanId
    WHERE mi.EmpId IN (" + employeeCsv + @")
      AND mi.IsActive = 1
      AND mi.IsMonthly = 1
      AND mi.StartDate <= @PeriodEnd
      AND (mi.EndDate IS NULL OR mi.EndDate >= @PeriodStart)
      AND plx.IsActive = 1
      AND ISNULL(plx.LifecycleStatus, N'Active') NOT IN (N'Cancelled', N'Expired', N'Suspended')
)
SELECT
    ids.EmpId,
    ai.Id AS MedicalInsuranceId,
    ISNULL(pl.PlanNameAr, N'') AS MedicalPlanName,
    ISNULL(ai.MonthlyCost, 0) AS MonthlyCost,
    ai.EmployeeShareType,
    ISNULL(ai.EmployeeShareValue, 0) AS EmployeeShareValue,
    ai.CompanyShareType,
    ISNULL(ai.CompanyShareValue, 0) AS CompanyShareValue,
    ISNULL(ai.EmployeeMonthlyDeduction, 0) AS EmployeeMonthlyDeduction,
    ISNULL(ai.CompanyMonthlyCost, 0) AS CompanyMonthlyCost,
    ISNULL(pl.EmployeeDeductionAccountCode, N'') AS EmployeeDeductionAccountCode,
    ISNULL(pl.CompanyCostAccountCode, N'') AS CompanyCostAccountCode,
    ISNULL(pmi.EmployeeDeduction, 0) AS SavedMedicalDeduction
FROM (SELECT EmpId FROM (VALUES " + string.Join(",", employeeIds.Select(x => "(" + x.ToString() + ")").ToArray()) + @") v(EmpId)) ids
LEFT JOIN ActiveInsurance ai ON ai.EmpId = ids.EmpId AND ai.rn = 1
LEFT JOIN dbo.MedicalInsurancePlans pl WITH (NOLOCK) ON pl.PlanId = ai.PlanId
LEFT JOIN dbo.PayrollMedicalInsuranceDeduction pmi WITH (NOLOCK)
       ON pmi.EmpId = ids.EmpId AND pmi.[Year] = @Year AND pmi.[Month] = @Month;";
                command.Parameters.Add("@Year", SqlDbType.Int).Value = request.Year;
                command.Parameters.Add("@Month", SqlDbType.Int).Value = request.Month;
                command.Parameters.Add("@PeriodStart", SqlDbType.DateTime).Value = periodStart;
                command.Parameters.Add("@PeriodEnd", SqlDbType.DateTime).Value = periodEnd;
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        SalaryRunEmployeeRow row;
                        if (!byEmployee.TryGetValue(ReadInt(reader, "EmpId"), out row))
                        {
                            continue;
                        }

                        var savedMedical = ReadDecimal(reader, "SavedMedicalDeduction");
                        var monthlyCost = ReadDecimal(reader, "MonthlyCost");
                        var medical = CalculateMedicalInsurance(monthlyCost, ReadString(reader, "EmployeeShareType"), ReadDecimal(reader, "EmployeeShareValue"), ReadString(reader, "CompanyShareType"), ReadDecimal(reader, "CompanyShareValue"));
                        var storedEmployeeDeduction = ReadDecimal(reader, "EmployeeMonthlyDeduction");
                        var storedCompanyCost = ReadDecimal(reader, "CompanyMonthlyCost");
                        if (storedEmployeeDeduction > 0 || storedCompanyCost > 0)
                        {
                            medical.EmployeeDeduction = storedEmployeeDeduction;
                            medical.CompanyCost = storedCompanyCost;
                        }

                        row.MedicalInsuranceId = ReadNullableInt(reader, "MedicalInsuranceId");
                        row.MedicalInsurancePlanName = ReadString(reader, "MedicalPlanName");
                        row.MedicalInsuranceMonthlyCost = monthlyCost;
                        row.MedicalInsuranceDeduction = medical.EmployeeDeduction;
                        row.MedicalInsuranceCompanyCost = medical.CompanyCost;
                        row.MedicalInsuranceEmployeeAccountCode = ReadString(reader, "EmployeeDeductionAccountCode");
                        row.MedicalInsuranceCompanyAccountCode = ReadString(reader, "CompanyCostAccountCode");

                        if (savedMedical > 0)
                        {
                            row.ExistingDiscounts = Math.Max(0m, row.ExistingDiscounts - savedMedical);
                        }

                        if (row.IsLegacySnapshot && !row.IsApproved)
                        {
                            row.TotalDeductions = Math.Max(0m, row.TotalDeductions - savedMedical) + row.MedicalInsuranceDeduction;
                            row.NetSalary = row.TotalBeforeDeductions - row.TotalDeductions;
                        }
                    }
                }
            }
        }

        private SalaryRunPreview PreviewSalaryRunCurrentBasis(SalaryRunRequest request)
        {
            request = NormalizeSalaryRequest(request);
            var preview = new SalaryRunPreview { Request = request };
            var start = new DateTime(request.Year, request.Month, 1);
            var end = start.AddMonths(1).AddDays(-1);
            var sgn = request.Year.ToString() + request.Month.ToString();

            using (var connection = OpenConnection())
            using (var command = connection.CreateCommand())
            {
                var hasMedicalTables = TableExists(connection, "PayrollMedicalInsuranceDeduction")
                    && TableExists(connection, "EmployeeMedicalInsurance")
                    && TableExists(connection, "MedicalInsurancePlans");

                command.CommandText = hasMedicalTables ? @"
SELECT
    e.Emp_ID, e.Emp_Code, e.Emp_Name, e.BranchId, b.branch_name, e.DepartmentID, d.DepartmentName,
    ISNULL(e.Emp_Salary, 0) AS Emp_Salary,
    ISNULL(s.Emp_Salary, 0) AS SavedEmpSalary,
    ISNULL(e.Emp_Salary_sakn, 0) AS Emp_Salary_sakn,
    ISNULL(e.Emp_Salary_bus, 0) AS Emp_Salary_bus,
    ISNULL(e.Emp_Salary_food, 0) AS Emp_Salary_food,
    ISNULL(e.Emp_Salary_mob, 0) AS Emp_Salary_mob,
    ISNULL(e.Emp_Salary_mang, 0) AS Emp_Salary_mang,
    ISNULL(e.Emp_Salary_others, 0) AS Emp_Salary_others,
    e.Account_code, e.Account_code1,
    s.id AS SalaryRowId, s.payed,
    ISNULL(s.Mokafea, 0) AS Mokafea,
    ISNULL(s.SalesCom, 0) AS SalesCom,
    ISNULL(s.TotalAdvance, 0) AS SavedAdvance,
    ISNULL(s.TotalDiscount, 0) AS SavedDiscount,
    ISNULL(s.ToalInsurance, 0) AS SavedInsurance,
    ISNULL(pmi.EmployeeDeduction, 0) AS SavedMedicalInsuranceDeduction,
    ISNULL(a.TotalAdvance, 0) AS AdvanceValue,
    mi.Id AS MedicalInsuranceId,
    ISNULL(pl.PlanNameAr, N'') AS MedicalPlanName,
    ISNULL(mi.MonthlyCost, 0) AS MedicalMonthlyCost,
    mi.EmployeeShareType,
    ISNULL(mi.EmployeeShareValue, 0) AS EmployeeShareValue,
    mi.CompanyShareType,
    ISNULL(mi.CompanyShareValue, 0) AS CompanyShareValue,
    ISNULL(mi.EmployeeMonthlyDeduction, 0) AS EmployeeMonthlyDeduction,
    ISNULL(mi.CompanyMonthlyCost, 0) AS CompanyMonthlyCost,
    ISNULL(pl.EmployeeDeductionAccountCode, N'') AS MedicalEmployeeAccountCode,
    ISNULL(pl.CompanyCostAccountCode, N'') AS MedicalCompanyAccountCode
FROM dbo.TblEmployee e WITH (NOLOCK)
LEFT JOIN dbo.TblBranchesData b WITH (NOLOCK) ON b.branch_id = e.BranchId
LEFT JOIN dbo.TblEmpDepartments d WITH (NOLOCK) ON d.DeparmentID = e.DepartmentID
LEFT JOIN dbo.emp_salary s WITH (NOLOCK) ON s.emp_id = e.Emp_ID AND s.sgn = @Sgn
OUTER APPLY (
    SELECT SUM(q.TotalAdvance) AS TotalAdvance
    FROM dbo.QryAllEmpAdvance(@Month, @Year) q
    WHERE q.Emp_ID = e.Emp_ID
) a
OUTER APPLY (
    SELECT TOP (1) d.EmployeeDeduction
    FROM dbo.PayrollMedicalInsuranceDeduction d WITH (NOLOCK)
    WHERE d.EmpId = e.Emp_ID
      AND d.[Year] = @Year
      AND d.[Month] = @Month
    ORDER BY d.Id DESC
) pmi
OUTER APPLY (
    SELECT TOP (1) *
    FROM dbo.EmployeeMedicalInsurance x WITH (NOLOCK)
    WHERE x.EmpId = e.Emp_ID
      AND x.IsActive = 1
      AND x.IsMonthly = 1
      AND x.StartDate <= @PeriodEnd
      AND (x.EndDate IS NULL OR x.EndDate >= @PeriodStart)
      AND EXISTS (
          SELECT 1
          FROM dbo.MedicalInsurancePlans px WITH (NOLOCK)
          WHERE px.PlanId = x.PlanId
            AND px.IsActive = 1
            AND ISNULL(px.LifecycleStatus, N'Active') NOT IN (N'Cancelled', N'Expired', N'Suspended')
      )
    ORDER BY x.StartDate DESC, x.Id DESC
) mi
LEFT JOIN dbo.MedicalInsurancePlans pl WITH (NOLOCK) ON pl.PlanId = mi.PlanId
WHERE ISNULL(e.chkStop, 0) = 0
  AND ISNULL(e.workstate, 0) = 1
  AND e.BignDateWork IS NOT NULL
  AND ISNULL(e.lastHolidaydate, e.BignDateWork) < @PeriodEnd
  AND (@BranchId IS NULL OR e.BranchId = @BranchId)
  AND (@DepartmentId IS NULL OR e.DepartmentID = @DepartmentId)
  AND (@EmployeeId IS NULL OR e.Emp_ID = @EmployeeId)
  AND (@PostingStatus = N'' OR (@PostingStatus = N'Posted' AND ISNULL(s.payed, 0) = 1) OR (@PostingStatus = N'Unposted' AND ISNULL(s.payed, 0) = 0))
  AND (@IncludeSavedDrafts = 1 OR s.id IS NULL OR ISNULL(s.payed, 0) = 0)
ORDER BY e.Fullcode, e.Emp_Code, e.Emp_ID;" : @"
SELECT
    e.Emp_ID, e.Emp_Code, e.Emp_Name, e.BranchId, b.branch_name, e.DepartmentID, d.DepartmentName,
    ISNULL(e.Emp_Salary, 0) AS Emp_Salary,
    ISNULL(s.Emp_Salary, 0) AS SavedEmpSalary,
    ISNULL(e.Emp_Salary_sakn, 0) AS Emp_Salary_sakn,
    ISNULL(e.Emp_Salary_bus, 0) AS Emp_Salary_bus,
    ISNULL(e.Emp_Salary_food, 0) AS Emp_Salary_food,
    ISNULL(e.Emp_Salary_mob, 0) AS Emp_Salary_mob,
    ISNULL(e.Emp_Salary_mang, 0) AS Emp_Salary_mang,
    ISNULL(e.Emp_Salary_others, 0) AS Emp_Salary_others,
    e.Account_code, e.Account_code1,
    s.id AS SalaryRowId, s.payed,
    ISNULL(s.Mokafea, 0) AS Mokafea,
    ISNULL(s.SalesCom, 0) AS SalesCom,
    ISNULL(s.TotalAdvance, 0) AS SavedAdvance,
    ISNULL(s.TotalDiscount, 0) AS SavedDiscount,
    ISNULL(s.ToalInsurance, 0) AS SavedInsurance,
    CONVERT(money, 0) AS SavedMedicalInsuranceDeduction,
    ISNULL(a.TotalAdvance, 0) AS AdvanceValue,
    CONVERT(int, NULL) AS MedicalInsuranceId,
    N'' AS MedicalPlanName,
    CONVERT(money, 0) AS MedicalMonthlyCost,
    N'' AS EmployeeShareType,
    CONVERT(money, 0) AS EmployeeShareValue,
    N'' AS CompanyShareType,
    CONVERT(money, 0) AS CompanyShareValue,
    CONVERT(money, 0) AS EmployeeMonthlyDeduction,
    CONVERT(money, 0) AS CompanyMonthlyCost,
    N'' AS MedicalEmployeeAccountCode,
    N'' AS MedicalCompanyAccountCode
FROM dbo.TblEmployee e WITH (NOLOCK)
LEFT JOIN dbo.TblBranchesData b WITH (NOLOCK) ON b.branch_id = e.BranchId
LEFT JOIN dbo.TblEmpDepartments d WITH (NOLOCK) ON d.DeparmentID = e.DepartmentID
LEFT JOIN dbo.emp_salary s WITH (NOLOCK) ON s.emp_id = e.Emp_ID AND s.sgn = @Sgn
OUTER APPLY (
    SELECT SUM(q.TotalAdvance) AS TotalAdvance
    FROM dbo.QryAllEmpAdvance(@Month, @Year) q
    WHERE q.Emp_ID = e.Emp_ID
) a
WHERE ISNULL(e.chkStop, 0) = 0
  AND ISNULL(e.workstate, 0) = 1
  AND e.BignDateWork IS NOT NULL
  AND ISNULL(e.lastHolidaydate, e.BignDateWork) < @PeriodEnd
  AND (@BranchId IS NULL OR e.BranchId = @BranchId)
  AND (@DepartmentId IS NULL OR e.DepartmentID = @DepartmentId)
  AND (@EmployeeId IS NULL OR e.Emp_ID = @EmployeeId)
  AND (@PostingStatus = N'' OR (@PostingStatus = N'Posted' AND ISNULL(s.payed, 0) = 1) OR (@PostingStatus = N'Unposted' AND ISNULL(s.payed, 0) = 0))
  AND (@IncludeSavedDrafts = 1 OR s.id IS NULL OR ISNULL(s.payed, 0) = 0)
ORDER BY e.Fullcode, e.Emp_Code, e.Emp_ID;";
                command.Parameters.Add("@Year", SqlDbType.Int).Value = request.Year;
                command.Parameters.Add("@Month", SqlDbType.Int).Value = request.Month;
                command.Parameters.Add("@Sgn", SqlDbType.VarChar, 20).Value = sgn;
                command.Parameters.Add("@PeriodStart", SqlDbType.DateTime).Value = start;
                command.Parameters.Add("@PeriodEnd", SqlDbType.DateTime).Value = end;
                AddNullable(command, "@BranchId", SqlDbType.Int, request.BranchId);
                AddNullable(command, "@DepartmentId", SqlDbType.Int, request.DepartmentId);
                AddNullable(command, "@EmployeeId", SqlDbType.Int, request.EmployeeId);
                command.Parameters.Add("@PostingStatus", SqlDbType.NVarChar, 20).Value = request.PostingStatus ?? string.Empty;
                command.Parameters.Add("@IncludeSavedDrafts", SqlDbType.Bit).Value = request.IncludeSavedDrafts;

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var basic = ReadDecimal(reader, "Emp_Salary");
                        var allowances = ReadDecimal(reader, "Emp_Salary_sakn") + ReadDecimal(reader, "Emp_Salary_bus") + ReadDecimal(reader, "Emp_Salary_food") + ReadDecimal(reader, "Emp_Salary_mob") + ReadDecimal(reader, "Emp_Salary_mang") + ReadDecimal(reader, "Emp_Salary_others");
                        var additions = ReadDecimal(reader, "Mokafea") + ReadDecimal(reader, "SalesCom");
                        var advance = ReadDecimal(reader, "AdvanceValue");
                        if (advance == 0)
                        {
                            advance = ReadDecimal(reader, "SavedAdvance");
                        }

                        var savedMedicalDeduction = ReadDecimal(reader, "SavedMedicalInsuranceDeduction");
                        var existingDiscount = Math.Max(0m, ReadDecimal(reader, "SavedDiscount") - savedMedicalDeduction);
                        var savedInsurance = ReadDecimal(reader, "SavedInsurance");
                        var monthlyCost = ReadDecimal(reader, "MedicalMonthlyCost");
                        var medical = CalculateMedicalInsurance(monthlyCost, ReadString(reader, "EmployeeShareType"), ReadDecimal(reader, "EmployeeShareValue"), ReadString(reader, "CompanyShareType"), ReadDecimal(reader, "CompanyShareValue"));
                        var storedEmployeeDeduction = ReadDecimal(reader, "EmployeeMonthlyDeduction");
                        var storedCompanyCost = ReadDecimal(reader, "CompanyMonthlyCost");
                        if (storedEmployeeDeduction > 0 || storedCompanyCost > 0)
                        {
                            medical.EmployeeDeduction = storedEmployeeDeduction;
                            medical.CompanyCost = storedCompanyCost;
                        }

                        var totalBeforeDeduction = basic + allowances + additions;
                        var totalDeductions = advance + existingDiscount + savedInsurance + medical.EmployeeDeduction;

                        var row = new SalaryRunEmployeeRow
                        {
                            Selected = true,
                            EmployeeId = ReadInt(reader, "Emp_ID"),
                            EmployeeCode = ReadString(reader, "Emp_Code"),
                            EmployeeName = ReadString(reader, "Emp_Name"),
                            BranchId = ReadNullableInt(reader, "BranchId"),
                            BranchName = ReadString(reader, "branch_name"),
                            DepartmentId = ReadNullableInt(reader, "DepartmentID"),
                            DepartmentName = ReadString(reader, "DepartmentName"),
                            BasicSalary = basic,
                            SalaryAllowances = allowances,
                            VariableAdditions = additions,
                            TotalBeforeDeductions = totalBeforeDeduction,
                            AdvanceDeduction = advance,
                            ExistingDiscounts = existingDiscount + savedInsurance,
                            MedicalInsuranceId = ReadNullableInt(reader, "MedicalInsuranceId"),
                            MedicalInsurancePlanName = ReadString(reader, "MedicalPlanName"),
                            MedicalInsuranceMonthlyCost = monthlyCost,
                            MedicalInsuranceDeduction = medical.EmployeeDeduction,
                            MedicalInsuranceCompanyCost = medical.CompanyCost,
                            MedicalInsuranceEmployeeAccountCode = ReadString(reader, "MedicalEmployeeAccountCode"),
                            MedicalInsuranceCompanyAccountCode = ReadString(reader, "MedicalCompanyAccountCode"),
                            TotalDeductions = totalDeductions,
                            NetSalary = totalBeforeDeduction - totalDeductions,
                            ExistingSalaryRowId = ReadNullableInt(reader, "SalaryRowId"),
                            IsApproved = ReadNullableInt(reader, "payed").GetValueOrDefault() == 1,
                            EmployeeAccountCode = ReadString(reader, "Account_code"),
                            AccruedSalaryAccountCode = ReadString(reader, "Account_code1")
                        };
                        preview.Rows.Add(row);
                        preview.HasExistingApprovedRows = preview.HasExistingApprovedRows || row.IsApproved;
                    }
                }
            }

            using (var connection = OpenConnection())
            {
                AttachRuntimeAdvanceInstallments(connection, preview);
            }
            foreach (var row in preview.Rows)
            {
                preview.TotalBasic += row.BasicSalary;
                preview.TotalAdditions += row.SalaryAllowances + row.VariableAdditions;
                preview.TotalAdvance += row.AdvanceDeduction;
                preview.TotalMedicalInsurance += row.MedicalInsuranceDeduction;
                preview.TotalMedicalInsuranceCompanyCost += row.MedicalInsuranceCompanyCost;
                preview.TotalDeductions += row.TotalDeductions;
                preview.TotalNet += row.NetSalary;
            }

            BuildJournalPreview(preview);
            using (var connection = OpenConnection())
            {
                AttachJournalPreviewAccountInfo(connection, preview);
            }
            preview.Message = preview.HasExistingApprovedRows
                ? "توجد صفوف معتمدة لهذه الفترة، ولن يتم تعديلها من شاشة الويب."
                : "تم حساب المسير مع إدراج نصيب الموظف من التأمين الطبي كخصم فقط.";
            ApplySalaryPreviewPayloadLimits(preview, request);
            return preview;
        }

        private static void ApplySalaryPreviewPayloadLimits(SalaryRunPreview preview, SalaryRunRequest request)
        {
            if (preview == null)
            {
                return;
            }

            preview.TotalRows = preview.Rows == null ? 0 : preview.Rows.Count;
            preview.TotalJournalPreviewRows = preview.JournalPreview == null ? 0 : preview.JournalPreview.Count;

            var rowLimit = request == null ? 0 : request.RowLimit;
            var journalLimit = request == null ? 0 : request.JournalPreviewLimit;
            if (rowLimit > 0 && preview.Rows != null && preview.Rows.Count > rowLimit)
            {
                preview.Rows = preview.Rows.Take(rowLimit).ToList();
                preview.PayloadIsTruncated = true;
            }

            if (journalLimit > 0 && preview.JournalPreview != null && preview.JournalPreview.Count > journalLimit)
            {
                preview.JournalPreview = preview.JournalPreview.Take(journalLimit).ToList();
                preview.PayloadIsTruncated = true;
            }
        }

        private void AttachRuntimeAdvanceInstallments(SqlConnection connection, SalaryRunPreview preview)
        {
            if (preview == null || preview.Rows == null || preview.Rows.Count == 0
                || !TableExists(connection, "TblEmpAdvance")
                || !TableExists(connection, "TblEmpAdvanceDetails"))
            {
                return;
            }

            var request = preview.Request ?? new SalaryRunRequest();
            var employeeIds = preview.Rows.Select(x => x.EmployeeId).Distinct().ToList();
            if (employeeIds.Count == 0)
            {
                return;
            }

            var employeeCsv = string.Join(",", employeeIds.Select(x => x.ToString()).ToArray());
            var byEmployee = preview.Rows.ToDictionary(x => x.EmployeeId);
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
SELECT a.Emp_ID,
       d.TableID,
       d.AdvanceID,
       ISNULL(d.PartNO, 0) AS PartNO,
       d.PartDate,
       ISNULL(d.PartValue, 0) AS PartValue,
       ISNULL(d.Payed, 0) AS IsPosted,
       ISNULL(d.StutsID, 0) AS StutsID,
       d.MothID2,
       d.YearID2
FROM dbo.TblEmpAdvance a WITH (NOLOCK)
INNER JOIN dbo.TblEmpAdvanceDetails d WITH (NOLOCK) ON d.AdvanceID = a.AdvanceID
WHERE a.Emp_ID IN (" + employeeCsv + @")
  AND ISNULL(a.AdvanceType, 0) = 0
  AND ((MONTH(d.PartDate) = @Month AND YEAR(d.PartDate) = @Year)
       OR (d.MothID2 = @Month AND d.YearID2 = @Year))
  AND (d.Payed IS NULL OR d.Payed <> 1)
  AND d.Payed1 IS NULL
  AND (d.StutsID IS NULL OR d.StutsID IN (21, 22, 23, 666))
  AND NOT EXISTS (
      SELECT 1
      FROM dbo.PayrollRunAdvanceDeductions existing WITH (NOLOCK)
      LEFT JOIN dbo.PayrollRunHeader h WITH (NOLOCK) ON h.PayrollRunId = existing.PayrollRunId
      WHERE existing.AdvanceDetailTableId = d.TableID
        AND (@PayrollRunId IS NULL OR existing.PayrollRunId <> @PayrollRunId)
        AND (h.PayrollRunId IS NULL OR ISNULL(h.IsCancelled, 0) = 0)
  )
ORDER BY a.Emp_ID, d.PartDate, d.TableID;";
                command.Parameters.Add("@Year", SqlDbType.Int).Value = request.Year;
                command.Parameters.Add("@Month", SqlDbType.Int).Value = request.Month;
                AddNullable(command, "@PayrollRunId", SqlDbType.Int, request.PayrollRunId);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        SalaryRunEmployeeRow row;
                        if (!byEmployee.TryGetValue(ReadInt(reader, "Emp_ID"), out row))
                        {
                            continue;
                        }

                        row.AdvanceInstallments.Add(new PayrollAdvanceInstallmentRow
                        {
                            TableId = ReadInt(reader, "TableID"),
                            AdvanceId = ReadInt(reader, "AdvanceID"),
                            PartNo = ReadInt(reader, "PartNO"),
                            PartDate = FormatDate(reader["PartDate"]),
                            PartValue = ReadDecimal(reader, "PartValue"),
                            IsPosted = ReadBool(reader, "IsPosted"),
                            StatusText = "جاهز للخصم",
                            SourceText = "TblEmpAdvanceDetails"
                        });
                    }
                }
            }

            foreach (var row in preview.Rows)
            {
                if (row.IsApproved)
                {
                    continue;
                }

                var installmentTotal = row.AdvanceInstallments.Sum(x => x.PartValue);
                if (installmentTotal > 0 || row.AdvanceDeduction == 0)
                {
                    row.AdvanceDeduction = installmentTotal;
                }
            }
        }

        private void AttachPayrollRunAdvanceInstallments(SqlConnection connection, SalaryRunPreview preview)
        {
            if (preview == null || preview.Rows == null || preview.Rows.Count == 0
                || !preview.PayrollRunId.HasValue
                || !TableExists(connection, "PayrollRunAdvanceDeductions"))
            {
                return;
            }

            var byEmployee = preview.Rows.ToDictionary(x => x.EmployeeId);
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
SELECT EmployeeId, AdvanceDetailTableId, AdvanceId, PartNo, PartDate, PartValue, IsPosted
FROM dbo.PayrollRunAdvanceDeductions WITH (NOLOCK)
WHERE PayrollRunId = @PayrollRunId
ORDER BY EmployeeId, PartDate, AdvanceDetailTableId;";
                command.Parameters.Add("@PayrollRunId", SqlDbType.Int).Value = preview.PayrollRunId.Value;
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        SalaryRunEmployeeRow row;
                        if (!byEmployee.TryGetValue(ReadInt(reader, "EmployeeId"), out row))
                        {
                            continue;
                        }

                        row.AdvanceInstallments.Add(new PayrollAdvanceInstallmentRow
                        {
                            TableId = ReadInt(reader, "AdvanceDetailTableId"),
                            AdvanceId = ReadInt(reader, "AdvanceId"),
                            PartNo = ReadInt(reader, "PartNo"),
                            PartDate = FormatDate(reader["PartDate"]),
                            PartValue = ReadDecimal(reader, "PartValue"),
                            IsPosted = ReadBool(reader, "IsPosted"),
                            StatusText = ReadBool(reader, "IsPosted") ? "تم خصمه من المسير" : "محفوظ في المسير",
                            SourceText = "PayrollRunAdvanceDeductions"
                        });
                    }
                }
            }
        }

        private void LoadCompatibilityRows(SqlConnection connection, SalaryRunPreview preview, string sgn)
        {
            var request = preview.Request;
            using (var command = connection.CreateCommand())
            {
                var hasLegacyFunctions = FunctionExists(connection, "EmpInsurances")
                    && FunctionExists(connection, "EmpVoCation3")
                    && FunctionExists(connection, "EmpPrePaymentID")
                    && FunctionExists(connection, "EmpPrePaymentValue")
                    && FunctionExists(connection, "GetAbcentDay");
                var hasEmpSalaryVacationColumns = ColumnExists(connection, "emp_salary", "TotalVacValue")
                    && ColumnExists(connection, "emp_salary", "vacDay");
                var savedVacationSelect = hasEmpSalaryVacationColumns
                    ? "ISNULL(s.TotalVacValue, 0) AS TotalVacValue, ISNULL(s.vacDay, 0) AS SavedVacDay,"
                    : "CONVERT(money, 0) AS TotalVacValue, CONVERT(money, 0) AS SavedVacDay,";
                var runtimeVacationDaysSelect = FunctionExists(connection, "GetAbcentDay2")
                    ? "dbo.GetAbcentDay2(e.Emp_ID, @Year, @Month) AS RuntimeVacationDays"
                    : @"(
        SELECT SUM(ISNULL(cd.NoofDays, 0))
        FROM dbo.TblChangedComponentRegister cr WITH (NOLOCK)
        LEFT JOIN dbo.TblChangedComponentRegisterDetails cd WITH (NOLOCK) ON cr.ChangedComponentid = cd.ChangedComponentid
        WHERE cr.Actualmonth = @Month
          AND cr.Actualyear = @Year
          AND ISNULL(cd.value, 0) = 0
          AND cd.Emp_id = e.Emp_ID
    ) AS RuntimeVacationDays";
                command.CommandText = hasLegacyFunctions ? @"
SELECT
    e.Emp_ID, e.Emp_Code, e.Emp_Name, e.BranchId, b.branch_name, e.DepartmentID, d.DepartmentName,
    s.project_id, p.Project_name, p.Salary_account AS ProjectSalaryAccount,
    e.Account_code, e.Account_code1, e.Account_Code2, e.Account_Code3, e.BignDateWork, e.lastHolidaydate, opt.MonthIs30days, opt.EmpSalaryDigts,
    ISNULL(e.Emp_Salary, 0) AS Emp_Salary,
    ISNULL(s.Emp_Salary, 0) AS SavedEmpSalary,
    s.id AS SalaryRowId, s.payed, s.total1, s.total2, s.EmpTotalNet, s.TotalAdvance, s.TotalDiscount,
    s.ToalInsurance, s.CountDays, s.AbcentDay, s.RemainDay, s.VoCation3, s.Mokafea, s.SalesCom,
    " + savedVacationSelect + @"
    ISNULL(a.TotalAdvance, 0) AS RuntimeAdvance,
    dbo.EmpInsurances(@Month - 1, @Year, e.Emp_ID) AS RuntimeInsurance,
    dbo.EmpVoCation3(@Month, @Year, e.Emp_ID) AS RuntimeVacation3,
    dbo.EmpPrePaymentValue(dbo.EmpPrePaymentID(e.Emp_ID)) AS RuntimePrePaymentValue,
    dbo.GetAbcentDay(e.Emp_ID, @Year, @Month) AS RuntimeAbsentDays,
    " + runtimeVacationDaysSelect + @"
FROM dbo.TblEmployee e WITH (NOLOCK)
LEFT JOIN dbo.TblBranchesData b WITH (NOLOCK) ON b.branch_id = e.BranchId
LEFT JOIN dbo.TblEmpDepartments d WITH (NOLOCK) ON d.DeparmentID = e.DepartmentID
LEFT JOIN dbo.emp_salary s WITH (NOLOCK) ON s.emp_id = e.Emp_ID AND s.sgn = @Sgn
LEFT JOIN dbo.projects p WITH (NOLOCK) ON p.id = s.project_id
OUTER APPLY (SELECT TOP (1) ISNULL(MonthIs30days, 0) AS MonthIs30days, ISNULL(EmpSalaryDigts, 2) AS EmpSalaryDigts FROM dbo.TblOptions WITH (NOLOCK)) opt
OUTER APPLY (
    SELECT SUM(q.TotalAdvance) AS TotalAdvance
    FROM dbo.QryAllEmpAdvance(@Month, @Year) q
    WHERE q.Emp_ID = e.Emp_ID
) a
WHERE ISNULL(e.chkStop, 0) = 0
  AND ISNULL(e.workstate, 0) = 1
  AND e.BignDateWork IS NOT NULL
  AND ISNULL(e.lastHolidaydate, e.BignDateWork) < @PeriodEnd
  AND (@BranchId IS NULL OR e.BranchId = @BranchId)
  AND (@DepartmentId IS NULL OR e.DepartmentID = @DepartmentId)
  AND (@EmployeeId IS NULL OR e.Emp_ID = @EmployeeId)
  AND (@PostingStatus = N'' OR (@PostingStatus = N'Posted' AND ISNULL(s.payed, 0) = 1) OR (@PostingStatus = N'Unposted' AND ISNULL(s.payed, 0) = 0))
  AND (@IncludeSavedDrafts = 1 OR s.id IS NULL OR ISNULL(s.payed, 0) = 0)
ORDER BY e.Fullcode, e.Emp_Code, e.Emp_ID;" : @"
SELECT
    e.Emp_ID, e.Emp_Code, e.Emp_Name, e.BranchId, b.branch_name, e.DepartmentID, d.DepartmentName,
    s.project_id, p.Project_name, p.Salary_account AS ProjectSalaryAccount,
    e.Account_code, e.Account_code1, e.Account_Code2, e.Account_Code3, e.BignDateWork, e.lastHolidaydate, opt.MonthIs30days, opt.EmpSalaryDigts,
    ISNULL(e.Emp_Salary, 0) AS Emp_Salary,
    ISNULL(s.Emp_Salary, 0) AS SavedEmpSalary,
    s.id AS SalaryRowId, s.payed, s.total1, s.total2, s.EmpTotalNet, s.TotalAdvance, s.TotalDiscount,
    s.ToalInsurance, s.CountDays, s.AbcentDay, s.RemainDay, s.VoCation3, s.Mokafea, s.SalesCom,
    " + savedVacationSelect + @"
    ISNULL(a.TotalAdvance, 0) AS RuntimeAdvance,
    CONVERT(money, 0) AS RuntimeInsurance,
    CONVERT(money, 0) AS RuntimeVacation3,
    CONVERT(money, 0) AS RuntimePrePaymentValue,
    CONVERT(money, 0) AS RuntimeAbsentDays,
    " + runtimeVacationDaysSelect + @"
FROM dbo.TblEmployee e WITH (NOLOCK)
LEFT JOIN dbo.TblBranchesData b WITH (NOLOCK) ON b.branch_id = e.BranchId
LEFT JOIN dbo.TblEmpDepartments d WITH (NOLOCK) ON d.DeparmentID = e.DepartmentID
LEFT JOIN dbo.emp_salary s WITH (NOLOCK) ON s.emp_id = e.Emp_ID AND s.sgn = @Sgn
LEFT JOIN dbo.projects p WITH (NOLOCK) ON p.id = s.project_id
OUTER APPLY (SELECT TOP (1) ISNULL(MonthIs30days, 0) AS MonthIs30days, ISNULL(EmpSalaryDigts, 2) AS EmpSalaryDigts FROM dbo.TblOptions WITH (NOLOCK)) opt
OUTER APPLY (
    SELECT SUM(q.TotalAdvance) AS TotalAdvance
    FROM dbo.QryAllEmpAdvance(@Month, @Year) q
    WHERE q.Emp_ID = e.Emp_ID
) a
WHERE ISNULL(e.chkStop, 0) = 0
  AND ISNULL(e.workstate, 0) = 1
  AND e.BignDateWork IS NOT NULL
  AND ISNULL(e.lastHolidaydate, e.BignDateWork) < @PeriodEnd
  AND (@BranchId IS NULL OR e.BranchId = @BranchId)
  AND (@DepartmentId IS NULL OR e.DepartmentID = @DepartmentId)
  AND (@EmployeeId IS NULL OR e.Emp_ID = @EmployeeId)
  AND (@PostingStatus = N'' OR (@PostingStatus = N'Posted' AND ISNULL(s.payed, 0) = 1) OR (@PostingStatus = N'Unposted' AND ISNULL(s.payed, 0) = 0))
  AND (@IncludeSavedDrafts = 1 OR s.id IS NULL OR ISNULL(s.payed, 0) = 0)
ORDER BY e.Fullcode, e.Emp_Code, e.Emp_ID;";
                command.Parameters.Add("@Year", SqlDbType.Int).Value = request.Year;
                command.Parameters.Add("@Month", SqlDbType.Int).Value = request.Month;
                command.Parameters.Add("@Sgn", SqlDbType.VarChar, 20).Value = sgn;
                command.Parameters.Add("@PeriodEnd", SqlDbType.DateTime).Value = new DateTime(request.Year, request.Month, 1).AddMonths(1).AddDays(-1);
                AddNullable(command, "@BranchId", SqlDbType.Int, request.BranchId);
                AddNullable(command, "@DepartmentId", SqlDbType.Int, request.DepartmentId);
                AddNullable(command, "@EmployeeId", SqlDbType.Int, request.EmployeeId);
                command.Parameters.Add("@PostingStatus", SqlDbType.NVarChar, 20).Value = request.PostingStatus ?? string.Empty;
                command.Parameters.Add("@IncludeSavedDrafts", SqlDbType.Bit).Value = request.IncludeSavedDrafts;

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var salaryRowId = ReadNullableInt(reader, "SalaryRowId");
                        var hasSnapshot = salaryRowId.HasValue;
                        var employeeBasic = ReadDecimal(reader, "Emp_Salary");
                        var snapshotBasic = ReadDecimal(reader, "SavedEmpSalary");
                        var snapshotTotal1 = ReadDecimal(reader, "total1");
                        var snapshotAllowances = hasSnapshot ? snapshotTotal1 - snapshotBasic : 0m;
                        var snapshotTotal2 = ReadDecimal(reader, "total2");
                        var snapshotNet = ReadDecimal(reader, "EmpTotalNet");
                        var runtimeAdvance = ReadDecimal(reader, "RuntimeAdvance");
                        var savedAdvance = ReadDecimal(reader, "TotalAdvance");
                        var insurance = ReadDecimal(reader, "ToalInsurance");
                        if (insurance == 0)
                        {
                            insurance = ReadDecimal(reader, "RuntimeInsurance");
                        }

                        var savedVacation3 = ReadDecimal(reader, "VoCation3");
                        var vacation3 = savedVacation3;
                        if (vacation3 == 0)
                        {
                            vacation3 = ReadDecimal(reader, "RuntimeVacation3");
                        }
                        var vacationDays = ReadDecimal(reader, "SavedVacDay");
                        if (vacationDays == 0)
                        {
                            vacationDays = ReadDecimal(reader, "RuntimeVacationDays");
                        }
                        var countDays = ReadDecimal(reader, "CountDays");
                        if (!hasSnapshot && countDays == 0)
                        {
                            countDays = GetPayrollMonthDayNo(ReadBool(reader, "MonthIs30days"), request.Year, request.Month);
                        }
                        var absentDays = hasSnapshot ? ReadDecimal(reader, "AbcentDay") : ReadDecimal(reader, "RuntimeAbsentDays");
                        var remainingDays = ReadDecimal(reader, "RemainDay");
                        if (!hasSnapshot)
                        {
                            remainingDays = Math.Max(0m, countDays - absentDays - vacationDays);
                        }
                        var isApproved = ReadNullableInt(reader, "payed").GetValueOrDefault() == 1;
                        if (hasSnapshot && !isApproved)
                        {
                            snapshotTotal2 = Math.Max(0m, snapshotTotal2 - savedVacation3) + vacation3;
                            snapshotNet = snapshotTotal1 - snapshotTotal2;
                            if (countDays > 0)
                            {
                                remainingDays = Math.Max(0m, countDays - absentDays - vacationDays);
                            }
                        }

                        var row = new SalaryRunEmployeeRow
                        {
                            Selected = true,
                            EmployeeId = ReadInt(reader, "Emp_ID"),
                            EmployeeCode = ReadString(reader, "Emp_Code"),
                            EmployeeName = ReadString(reader, "Emp_Name"),
                            BranchId = ReadNullableInt(reader, "BranchId"),
                            BranchName = ReadString(reader, "branch_name"),
                            DepartmentId = ReadNullableInt(reader, "DepartmentID"),
                            DepartmentName = ReadString(reader, "DepartmentName"),
                            ProjectId = ReadNullableInt(reader, "project_id"),
                            ProjectName = ReadString(reader, "Project_name"),
                            ProjectSalaryAccountCode = ReadString(reader, "ProjectSalaryAccount"),
                            BasicSalary = hasSnapshot ? snapshotBasic : employeeBasic,
                            SalaryAllowances = hasSnapshot ? snapshotAllowances : 0m,
                            TotalBeforeDeductions = hasSnapshot ? snapshotTotal1 : employeeBasic + ReadDecimal(reader, "Mokafea") + ReadDecimal(reader, "SalesCom"),
                            AdvanceDeduction = hasSnapshot ? savedAdvance : runtimeAdvance,
                            ExistingDiscounts = ReadDecimal(reader, "TotalDiscount"),
                            MedicalInsuranceDeduction = insurance,
                            MedicalInsuranceMonthlyCost = insurance,
                            TotalDeductions = hasSnapshot ? snapshotTotal2 : 0,
                            NetSalary = hasSnapshot ? snapshotNet : 0,
                            ExistingSalaryRowId = salaryRowId,
                            IsApproved = isApproved,
                            EmployeeAccountCode = ReadString(reader, "Account_code"),
                            AccruedSalaryAccountCode = ReadString(reader, "Account_code1"),
                            VacationProvisionAccountCode = ReadString(reader, "Account_Code2"),
                            AdvancePaymentAccountCode = ReadString(reader, "Account_Code3"),
                            IsLegacySnapshot = hasSnapshot,
                            CountDays = countDays,
                            AbsentDays = absentDays,
                            VacationDays = vacationDays,
                            RemainingDays = remainingDays,
                            VacationDeduction = vacation3,
                            VacationSalaryValue = ReadDecimal(reader, "TotalVacValue"),
                            TotalInsuranceLegacy = insurance,
                            CompatibilityStatus = hasSnapshot ? "LegacySnapshot" : "Reconstructed",
                            HiringDate = ReadNullableDate(reader, "BignDateWork"),
                            LastHolidayDate = ReadNullableDate(reader, "lastHolidaydate"),
                            MonthIs30Days = ReadBool(reader, "MonthIs30days"),
                            PayrollMonthDays = DateTime.DaysInMonth(request.Year, request.Month),
                            PayrollSalaryDigits = ReadNullableInt(reader, "EmpSalaryDigts").GetValueOrDefault(2)
                        };
                        preview.Rows.Add(row);
                        preview.HasExistingApprovedRows = preview.HasExistingApprovedRows || row.IsApproved;
                    }
                }

                if (!hasLegacyFunctions)
                {
                    preview.CompatibilityWarnings.Add(new PayrollCompatibilityWarning
                    {
                        Code = "MissingLegacyFunctions",
                        Message = "One or more VB6 payroll scalar functions are missing; fallback rows will not include full insurance/vacation/prepayment behavior."
                    });
                }
            }
        }

        private void AttachCompatibilityComponents(SqlConnection connection, SalaryRunPreview preview, string sgn)
        {
            if (preview.Rows.Count == 0)
            {
                return;
            }

            var employeeIds = preview.Rows.Select(x => x.EmployeeId).Distinct().ToList();
            var employeeCsv = string.Join(",", employeeIds.Select(x => x.ToString()).ToArray());
            var byEmployee = preview.Rows.ToDictionary(x => x.EmployeeId);
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
;WITH SnapshotComponents AS
(
    SELECT es.emp_id, v.ComponentNo, v.ComponentColumn, CONVERT(decimal(18, 4), ISNULL(v.SnapshotValue, 0)) AS SnapshotValue
    FROM dbo.emp_salary es WITH (NOLOCK)
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
    ) v(ComponentNo, ComponentColumn, SnapshotValue)
    WHERE es.sgn = @Sgn AND es.emp_id IN (" + employeeCsv + @")
),
SourceComponents AS
(
    SELECT esc.emp_ID, esc.mofrad_type AS ComponentNo, SUM(CONVERT(decimal(18, 4), ISNULL(esc.Value, 0))) AS FixedValue
    FROM dbo.EmpSalaryComponent esc WITH (NOLOCK)
    WHERE esc.emp_ID IN (" + employeeCsv + @")
    GROUP BY esc.emp_ID, esc.mofrad_type
),
ChangedComponents AS
(
    SELECT d.Emp_id, r.ComponentID AS ComponentNo, SUM(CONVERT(decimal(18, 4), ISNULL(d.value, 0))) AS ChangedValue
    FROM dbo.TblChangedComponentRegisterDetails d WITH (NOLOCK)
    INNER JOIN dbo.TblChangedComponentRegister r WITH (NOLOCK) ON r.ChangedComponentid = d.ChangedComponentid
    WHERE d.Emp_id IN (" + employeeCsv + @")
      AND
      (
          (r.[year] = @Year AND r.[month] = @Month)
          OR (r.Actualyear = @Year AND r.Actualmonth = @Month)
          OR (r.RecordDate >= @PeriodStart AND r.RecordDate < DATEADD(day, 1, @PeriodEnd))
      )
    GROUP BY d.Emp_id, r.ComponentID
),
YearOverrides AS
(
    SELECT cyd.EmpID, cyd.MofrdID AS ComponentNo, SUM(CONVERT(decimal(18, 4), ISNULL(cyd.MordValue, 0) / ISNULL(NULLIF(cyd.TypeMofrd, 0), 1))) AS OverrideValue
    FROM dbo.TblComponentYearDet cyd WITH (NOLOCK)
    WHERE cyd.EmpID IN (" + employeeCsv + @")
      AND
      (
          (MONTH(cyd.RecDate1) = @Month AND YEAR(cyd.RecDate1) = @Year)
          OR (MONTH(cyd.RecDate2) = @Month AND YEAR(cyd.RecDate2) = @Year)
      )
    GROUP BY cyd.EmpID, cyd.MofrdID
)
SELECT ids.Emp_ID, m.id AS ComponentNo, 'Comp' + CONVERT(varchar(10), m.id) AS ComponentColumn,
       m.name, m.nameE, m.AddOrDiscount, m.FixedOrChanged, m.ViewComp, m.ZmamAccount, m.AdvPaymentdAccount,
       m.Insurances, m.showMofradAll, m.culc30orRminder,
       m.Account_Code, m.Account_code1,
       ISNULL(sc.SnapshotValue, 0) AS SnapshotValue,
       ISNULL(src.FixedValue, 0) AS FixedSourceValue,
       ISNULL(ch.ChangedValue, 0) AS ChangedSourceValue,
       ISNULL(yo.OverrideValue, 0) AS OverrideSourceValue,
       CASE
           WHEN ISNULL(yo.OverrideValue, 0) <> 0 THEN ISNULL(yo.OverrideValue, 0)
           WHEN ISNULL(m.FixedOrChanged, 0) = 1 THEN ISNULL(ch.ChangedValue, 0)
           ELSE ISNULL(src.FixedValue, 0)
       END AS SourceValue,
       CASE
           WHEN sc.emp_id IS NOT NULL THEN 'emp_salary'
           WHEN ISNULL(yo.OverrideValue, 0) <> 0 THEN 'TblComponentYearDet'
           WHEN ISNULL(m.FixedOrChanged, 0) = 1 THEN 'TblChangedComponentRegister'
           ELSE 'EmpSalaryComponent'
       END AS SourceKind,
       CASE
           WHEN ISNULL(yo.OverrideValue, 0) <> 0 THEN 'Override wins over changed/fixed sources'
           WHEN ISNULL(m.FixedOrChanged, 0) = 1 THEN 'Variable component uses changed register'
           ELSE 'Fixed component uses EmpSalaryComponent'
       END AS PrecedenceDecision
FROM (SELECT Emp_ID FROM dbo.TblEmployee WITH (NOLOCK) WHERE Emp_ID IN (" + employeeCsv + @")) ids
CROSS JOIN dbo.mofrad m WITH (NOLOCK)
LEFT JOIN SnapshotComponents sc ON sc.emp_id = ids.Emp_ID AND sc.ComponentNo = m.id
LEFT JOIN SourceComponents src ON src.emp_ID = ids.Emp_ID AND src.ComponentNo = m.id
LEFT JOIN ChangedComponents ch ON ch.Emp_id = ids.Emp_ID AND ch.ComponentNo = m.id
LEFT JOIN YearOverrides yo ON yo.EmpID = ids.Emp_ID AND yo.ComponentNo = m.id
WHERE m.id BETWEEN 1 AND 40
ORDER BY ids.Emp_ID, m.id;";
                command.Parameters.Add("@Sgn", SqlDbType.VarChar, 20).Value = sgn;
                command.Parameters.Add("@Year", SqlDbType.Int).Value = preview.Request.Year;
                command.Parameters.Add("@Month", SqlDbType.Int).Value = preview.Request.Month;
                var periodStart = new DateTime(preview.Request.Year, preview.Request.Month, 1);
                command.Parameters.Add("@PeriodStart", SqlDbType.DateTime).Value = periodStart;
                command.Parameters.Add("@PeriodEnd", SqlDbType.DateTime).Value = periodStart.AddMonths(1).AddDays(-1);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        SalaryRunEmployeeRow row;
                        if (!byEmployee.TryGetValue(ReadInt(reader, "Emp_ID"), out row))
                        {
                            continue;
                        }

                        row.Components.Add(new PayrollCompatibilityComponent
                        {
                            ComponentNo = ReadInt(reader, "ComponentNo"),
                            ComponentColumn = ReadString(reader, "ComponentColumn"),
                            ComponentNameAr = ReadString(reader, "name"),
                            ComponentNameEn = ReadString(reader, "nameE"),
                            AddOrDiscount = ReadBool(reader, "AddOrDiscount"),
                            FixedOrChanged = ReadBool(reader, "FixedOrChanged"),
                            ViewComponent = ReadBool(reader, "ViewComp"),
                            ZmamAccount = ReadBool(reader, "ZmamAccount"),
                            AdvancePaymentAccount = ReadBool(reader, "AdvPaymentdAccount"),
                            Insurances = ReadBool(reader, "Insurances"),
                            ShowMofradAll = ReadBool(reader, "showMofradAll"),
                            Culc30OrReminder = ReadNullableInt(reader, "culc30orRminder"),
                            AccountCode = ReadString(reader, "Account_Code"),
                            AccountCode1 = ReadString(reader, "Account_code1"),
                            SnapshotValue = ReadDecimal(reader, "SnapshotValue"),
                            SourceValue = ReadDecimal(reader, "SourceValue"),
                            RawSourceValue = ReadDecimal(reader, "SourceValue"),
                            TemporalAdjustedValue = ReadDecimal(reader, "SourceValue"),
                            FixedSourceValue = ReadDecimal(reader, "FixedSourceValue"),
                            ChangedSourceValue = ReadDecimal(reader, "ChangedSourceValue"),
                            OverrideSourceValue = ReadDecimal(reader, "OverrideSourceValue"),
                            SourceKind = ReadString(reader, "SourceKind"),
                            PrecedenceDecision = ReadString(reader, "PrecedenceDecision")
                        });
                    }
                }
            }

            ApplyTemporalCompatibilityRules(preview);
        }

        private void AttachInsuranceCompatibilityTrace(SqlConnection connection, SalaryRunPreview preview, string sgn)
        {
            if (preview.Rows.Count == 0)
            {
                return;
            }

            var hasInsuranceTables = TableExists(connection, "TBLInsurances")
                && TableExists(connection, "TBLInsurancesJoin")
                && TableExists(connection, "TblSocialInsurance");
            var hasInsuranceFunction = FunctionExists(connection, "EmpInsurances");
            if (!hasInsuranceTables)
            {
                foreach (var row in preview.Rows)
                {
                    row.InsuranceTrace = new PayrollCompatibilityInsuranceTrace
                    {
                        EmployeeId = row.EmployeeId,
                        SourceProject = "VB6 Kishny",
                        SourceForm = "FrmEmpSalary5",
                        SourceFunction = "dbo.EmpInsurances",
                        SourceTables = "TBLInsurances/TBLInsurancesJoin/TblSocialInsurance",
                        SnapshotToalInsurance = row.TotalInsuranceLegacy,
                        RuntimeFunctionInsurance = row.TotalInsuranceLegacy,
                        EmployeeAccruedSalaryAccount = row.AccruedSalaryAccountCode,
                        ExclusionReason = "Legacy insurance tables were not found in this database.",
                        PostingRule = "Blocked: insurance/accounting parity trace is incomplete."
                    };
                }

                return;
            }

            var employeeIds = preview.Rows.Select(x => x.EmployeeId).Distinct().ToList();
            var employeeCsv = string.Join(",", employeeIds.Select(x => x.ToString()).ToArray());
            var byEmployee = preview.Rows.ToDictionary(x => x.EmployeeId);
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
SELECT
    e.Emp_ID,
    e.Nationality,
    e.Account_code1,
    ISNULL(s.ToalInsurance, 0) AS SnapshotToalInsurance,
    " + (hasInsuranceFunction ? "ISNULL(dbo.EmpInsurances(@FunctionMonth, @Year, e.Emp_ID), 0)" : "CONVERT(float, 0)") + @" AS RuntimeFunctionInsurance,
    j.IDINS, i.Monthe, i.SubYear, ISNULL(j.payed, 0) AS InsurancePaid,
    ISNULL(j.EmpInsurances, 0) AS TBLInsurancesJoinBase,
    ISNULL(j.InsValue, 0) AS InsValue,
    ISNULL(j.InsTotal, 0) AS TBLInsurancesJoinTotal,
    ISNULL(j.CompRate, 0) AS CompRate,
    ISNULL(j.WorkDays, 0) AS WorkDays,
    si.Acount_Code1 AS InsuranceCreditAccount,
    si.Acount_Code2 AS InsuranceAccount2,
    si.Acount_Code3 AS EmployerDebitAccount,
    si.Acount_Code4 AS EmployerCreditAccount,
    ISNULL(si.CitizenVal1, 0) AS CitizenVal1,
    ISNULL(si.ResidentVal1, 0) AS ResidentVal1
FROM dbo.TblEmployee e WITH (NOLOCK)
LEFT JOIN dbo.emp_salary s WITH (NOLOCK) ON s.emp_id = e.Emp_ID AND s.sgn = @Sgn
LEFT JOIN dbo.TBLInsurancesJoin j WITH (NOLOCK) ON j.EmpCode = e.Emp_ID
LEFT JOIN dbo.TBLInsurances i WITH (NOLOCK) ON i.IDINS = j.IDINS AND i.Monthe = @FunctionMonth AND i.SubYear = @Year
OUTER APPLY (SELECT TOP (1) * FROM dbo.TblSocialInsurance WITH (NOLOCK) ORDER BY ID ASC) si
WHERE e.Emp_ID IN (" + employeeCsv + @")
  AND (j.ID IS NULL OR (i.Monthe = @FunctionMonth AND i.SubYear = @Year))
ORDER BY e.Emp_ID;";
                command.Parameters.Add("@Sgn", SqlDbType.VarChar, 20).Value = sgn;
                command.Parameters.Add("@Year", SqlDbType.Int).Value = preview.Request.Year;
                command.Parameters.Add("@FunctionMonth", SqlDbType.Int).Value = preview.Request.Month - 1;

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        SalaryRunEmployeeRow row;
                        if (!byEmployee.TryGetValue(ReadInt(reader, "Emp_ID"), out row))
                        {
                            continue;
                        }

                        var trace = new PayrollCompatibilityInsuranceTrace
                        {
                            EmployeeId = row.EmployeeId,
                            SourceProject = "VB6 Kishny",
                            SourceForm = "FrmEmpSalary5",
                            SourceFunction = hasInsuranceFunction ? "dbo.EmpInsurances(@Month - 1, @Year, Emp_ID)" : "missing dbo.EmpInsurances",
                            SourceTables = "TBLInsurancesJoin -> TBLInsurances; TblSocialInsurance via GetInsuranceAccount",
                            SnapshotToalInsurance = ReadDecimal(reader, "SnapshotToalInsurance"),
                            RuntimeFunctionInsurance = ReadDecimal(reader, "RuntimeFunctionInsurance"),
                            TBLInsurancesJoinTotal = ReadDecimal(reader, "TBLInsurancesJoinTotal"),
                            TBLInsurancesJoinBase = ReadDecimal(reader, "TBLInsurancesJoinBase"),
                            CompanyRate = ReadDecimal(reader, "CompRate"),
                            WorkDays = ReadDecimal(reader, "WorkDays"),
                            InsuranceId = ReadNullableInt(reader, "IDINS"),
                            InsuranceMonth = ReadNullableInt(reader, "Monthe"),
                            InsuranceYear = ReadNullableInt(reader, "SubYear"),
                            IsPaid = ReadInt(reader, "InsurancePaid") != 0,
                            Nationality = ReadString(reader, "Nationality"),
                            CitizenPercent = ReadDecimal(reader, "CitizenVal1"),
                            ResidentPercent = ReadDecimal(reader, "ResidentVal1"),
                            EmployeeAccruedSalaryAccount = FirstNonEmpty(ReadString(reader, "Account_code1"), row.AccruedSalaryAccountCode),
                            InsuranceCreditAccount = ReadString(reader, "InsuranceCreditAccount"),
                            EmployerDebitAccount = ReadString(reader, "EmployerDebitAccount"),
                            EmployerCreditAccount = ReadString(reader, "EmployerCreditAccount"),
                            PostingRule = "VB6 posts employee share as debit to employee accrued salary account and credit to TblSocialInsurance.Acount_Code1 via ModAccounts.AddNewDev.",
                            InsuranceAdjustedTotal = ReadDecimal(reader, "RuntimeFunctionInsurance")
                        };

                        row.InsuranceTrace = trace;
                    }
                }
            }

            foreach (var row in preview.Rows)
            {
                if (row.InsuranceTrace == null)
                {
                    row.InsuranceTrace = new PayrollCompatibilityInsuranceTrace
                    {
                        EmployeeId = row.EmployeeId,
                        SourceProject = "VB6 Kishny",
                        SourceForm = "FrmEmpSalary5",
                        SourceFunction = hasInsuranceFunction ? "dbo.EmpInsurances(@Month - 1, @Year, Emp_ID)" : "missing dbo.EmpInsurances",
                        SourceTables = "TBLInsurancesJoin -> TBLInsurances; TblSocialInsurance via GetInsuranceAccount",
                        SnapshotToalInsurance = row.TotalInsuranceLegacy,
                        RuntimeFunctionInsurance = row.TotalInsuranceLegacy,
                        EmployeeAccruedSalaryAccount = row.AccruedSalaryAccountCode,
                        ExclusionReason = "No matching TBLInsurancesJoin snapshot was found for the requested period.",
                        PostingRule = "Blocked: no insurance source row found for period."
                    };
                }

                row.InsuranceTrace.InsuranceComponentBase = row.Components
                    .Where(x => x.Insurances)
                    .Sum(x => x.SourceValue);

                if (row.InsuranceTrace.RuntimeFunctionInsurance == 0 && row.TotalInsuranceLegacy != 0)
                {
                    row.InsuranceTrace.RuntimeFunctionInsurance = row.TotalInsuranceLegacy;
                    row.InsuranceTrace.InsuranceAdjustedTotal = row.TotalInsuranceLegacy;
                }

                if (string.IsNullOrWhiteSpace(row.InsuranceTrace.ExclusionReason))
                {
                    if (row.InsuranceTrace.IsPaid)
                    {
                        row.InsuranceTrace.ExclusionReason = "TBLInsurancesJoin.payed is set; dbo.EmpInsurances excludes paid rows.";
                    }
                    else if (row.InsuranceTrace.TBLInsurancesJoinTotal == 0 && row.InsuranceTrace.SnapshotToalInsurance == 0)
                    {
                        row.InsuranceTrace.ExclusionReason = "No insurance amount is present for this employee/period.";
                    }
                }
            }
        }

        private static void ApplyTemporalCompatibilityRules(SalaryRunPreview preview)
        {
            foreach (var row in preview.Rows)
            {
                foreach (var component in row.Components)
                {
                    ApplyTemporalCompatibilityRule(preview.Request, row, component);
                }
            }
        }

        private static void ApplyTemporalCompatibilityRule(SalaryRunRequest request, SalaryRunEmployeeRow row, PayrollCompatibilityComponent component)
        {
            var temporal = CalculateTemporalContext(request, row, component);
            component.TemporalCountFlag = temporal.CountFlag;
            component.TemporalNumeratorDays = temporal.NumeratorDays;
            component.TemporalDenominatorDays = temporal.ActualDenominator;
            component.TemporalMonthDayNo = temporal.PayrollMonthDayNo;
            component.TemporalActualMonthDays = temporal.CalendarMonthDays;
            component.TemporalRulePath = temporal.RulePath;
            component.TemporalDenominatorReason = temporal.DenominatorReason;
            component.TemporalProrationBypassed = temporal.ProrationBypassed;
            component.RawSourceValue = component.RawSourceValue == 0 ? component.SourceValue : component.RawSourceValue;

            if (temporal.ProrationApplied && temporal.ActualDenominator > 0)
            {
                component.TemporalProrationApplied = true;
                component.TemporalAdjustedValue = Math.Round(component.RawSourceValue / temporal.ActualDenominator * temporal.NumeratorDays, Math.Max(row.PayrollSalaryDigits, 0));
                component.SourceValue = component.TemporalAdjustedValue;
            }
            else
            {
                component.TemporalProrationApplied = false;
                component.TemporalAdjustedValue = component.SourceValue;
            }
        }

        private static void RecalculateCompatibilityFallbackRows(SalaryRunPreview preview)
        {
            foreach (var row in preview.Rows.Where(x => !x.IsLegacySnapshot))
            {
                ApplyVacationAttendanceCompatibility(row);
                var componentAddition = row.Components.Where(x => x.ViewComponent && !x.AddOrDiscount).Sum(x => x.SourceValue);
                var deduction = row.Components.Where(x => x.ViewComponent && x.AddOrDiscount).Sum(x => x.SourceValue);
                var addition = componentAddition != 0 ? componentAddition : row.BasicSalary;
                row.BasicSalary = addition;
                row.TotalBeforeDeductions = addition + row.VariableAdditions + row.VacationSalaryValue;
                row.TotalDeductions = row.AdvanceDeduction + row.ExistingDiscounts + row.MedicalInsuranceDeduction + row.VacationDeduction + deduction;
                row.NetSalary = row.TotalBeforeDeductions - row.TotalDeductions;
            }
        }

        private static void ApplyVacationAttendanceCompatibility(SalaryRunEmployeeRow row)
        {
            if (row == null || row.VacationDays <= 0 || row.Components == null || row.Components.Count == 0)
            {
                return;
            }

            var monthDays = GetPayrollMonthDayNo(row.MonthIs30Days, DateTime.Today.Year, DateTime.Today.Month);
            if (row.PayrollMonthDays.HasValue && row.PayrollMonthDays.Value > 0)
            {
                monthDays = row.MonthIs30Days ? 30m : row.PayrollMonthDays.Value;
            }

            var paidWorkDays = row.CountDays > 0 ? row.CountDays - row.VacationDays : monthDays - row.VacationDays;
            if (paidWorkDays < 0)
            {
                paidWorkDays = 0;
            }

            decimal vacationSalaryValue = 0;
            foreach (var component in row.Components.Where(x => x.ViewComponent && !x.FixedOrChanged && !x.ShowMofradAll))
            {
                var baseValue = component.RawSourceValue != 0 ? component.RawSourceValue : component.SourceValue;
                if (baseValue == 0)
                {
                    continue;
                }

                var denominator = component.Culc30OrReminder.GetValueOrDefault() == 0
                    ? monthDays
                    : (row.PayrollMonthDays.HasValue && row.PayrollMonthDays.Value > 0 ? row.PayrollMonthDays.Value : monthDays);
                if (denominator <= 0)
                {
                    continue;
                }

                var digits = Math.Max(row.PayrollSalaryDigits, 0);
                component.RawSourceValue = baseValue;
                component.SourceValue = Math.Round(baseValue / denominator * paidWorkDays, digits);
                component.TemporalProrationApplied = true;
                component.TemporalAdjustedValue = component.SourceValue;
                component.TemporalRulePath = FirstNonEmpty(component.TemporalRulePath, "full-period") + "|vacation-days-prorated";
                if (!component.AddOrDiscount)
                {
                    vacationSalaryValue += Math.Round(baseValue / denominator * row.VacationDays, digits);
                }
            }

            row.VacationSalaryValue = vacationSalaryValue;
            row.RemainingDays = Math.Max(0m, (row.CountDays > 0 ? row.CountDays : monthDays) - row.AbsentDays - row.VacationDays);
        }

        private static PayrollCompatibilityComponentDiff BuildComponentDiff(SalaryRunEmployeeRow row, PayrollCompatibilityComponent component)
        {
            var category = ClassifyComponentMismatch(row, component);
            return new PayrollCompatibilityComponentDiff
            {
                ComponentNo = component.ComponentNo,
                ComponentColumn = component.ComponentColumn,
                ComponentNameAr = component.ComponentNameAr,
                LegacyValue = component.SnapshotValue,
                ReconstructedValue = component.SourceValue,
                Difference = component.SourceValue - component.SnapshotValue,
                SourceKind = component.SourceKind,
                MismatchCategory = category.Category,
                LikelySource = category.LikelySource,
                PrecedenceDecision = component.PrecedenceDecision,
                ConfidenceScore = category.ConfidenceScore
            };
        }

        private static PayrollCompatibilityProrationTrace BuildProrationTrace(SalaryRunEmployeeRow row, PayrollCompatibilityComponent component, string category)
        {
            var trace = CalculateTemporalContext(null, row, component);
            return new PayrollCompatibilityProrationTrace
            {
                CountDays = row.CountDays,
                AbsentDays = row.AbsentDays,
                RemainingDays = row.RemainingDays,
                HiringDate = row.HiringDate,
                LastHolidayDate = row.LastHolidayDate,
                MonthIs30Days = row.MonthIs30Days,
                PayrollMonthDays = row.PayrollMonthDays,
                PayrollSalaryDigits = row.PayrollSalaryDigits,
                ShowMofradAll = component.ShowMofradAll,
                Culc30OrReminder = component.Culc30OrReminder,
                CalendarMonthDays = trace.CalendarMonthDays,
                PayrollMonthDayNo = trace.PayrollMonthDayNo,
                PayrollDays = trace.PayrollDays,
                ActualDenominator = trace.ActualDenominator,
                ExpectedDenominator = trace.ExpectedDenominator,
                NumeratorDays = trace.NumeratorDays,
                CountFlag = trace.CountFlag,
                ProrationApplied = trace.ProrationApplied,
                ProrationBypassed = trace.ProrationBypassed,
                VacationOverlap = trace.VacationOverlap,
                BranchProjectScope = trace.BranchProjectScope,
                DenominatorReason = trace.DenominatorReason,
                RulePath = trace.RulePath,
                ProrationCategory = IsProrationCandidate(row, component) ? category : "not-proration-candidate"
            };
        }

        private static MismatchClassification ClassifyComponentMismatch(SalaryRunEmployeeRow row, PayrollCompatibilityComponent component)
        {
            var activeSources = CountActiveSources(component);
            var componentText = ((component.ComponentNameAr ?? string.Empty) + " " + (component.ComponentNameEn ?? string.Empty)).ToLowerInvariant();

            if (component.SourceValue == component.SnapshotValue)
            {
                return Classify("matched", "legacy snapshot and reconstructed value are equal", 1.00m);
            }

            if (component.SnapshotValue != 0 && component.SourceValue == 0 && activeSources == 0)
            {
                return Classify("orphan snapshot component", "emp_salary Comp snapshot has value but no matching reconstructed source", 0.95m);
            }

            if (component.SnapshotValue == 0 && component.SourceValue != 0 && !component.ViewComponent)
            {
                return Classify("inactive component inclusion", "mofrad.ViewComp is disabled while reconstructed source contributes value", 0.90m);
            }

            if (component.OverrideSourceValue != 0 && component.SourceValue != component.OverrideSourceValue)
            {
                return Classify("override precedence issue", "TblComponentYearDet value exists but did not win precedence", 0.92m);
            }

            if (activeSources > 1)
            {
                if (component.OverrideSourceValue != 0)
                {
                    return Classify("override precedence issue", "year override is present together with fixed/changed sources", 0.86m);
                }

                if (component.FixedSourceValue != 0 && component.ChangedSourceValue != 0)
                {
                    return Classify(component.FixedOrChanged ? "changed-component merge issue" : "fixed-vs-variable conflict",
                        "EmpSalaryComponent and TblChangedComponentRegisterDetails both contribute to the component", 0.84m);
                }

                return Classify("duplicate component sourcing", "more than one reconstructed component source is active", 0.80m);
            }

            if (component.FixedOrChanged && component.ChangedSourceValue != 0)
            {
                return Classify("changed-component merge issue", "variable component is driven by TblChangedComponentRegisterDetails", 0.72m);
            }

            if (!component.FixedOrChanged && component.ChangedSourceValue != 0)
            {
                return Classify("fixed-vs-variable conflict", "changed register contributes to a fixed component", 0.78m);
            }

            if (IsProrationCandidate(row, component))
            {
                if (component.Culc30OrReminder.HasValue)
                {
                    var confidence = component.TemporalProrationApplied && CountActiveSources(component) <= 1 ? 0.88m : 0.76m;
                    return Classify("culc30orRminder behavior", "component has culc30orRminder metadata and employee days differ from full-period basis", confidence);
                }

                if (!component.ShowMofradAll)
                {
                    var confidence = component.TemporalProrationBypassed ? 0.82m : 0.70m;
                    return Classify("showMofradAll behavior", "component may be hidden or prorated when showMofradAll is disabled", confidence);
                }

                return Classify("proration issue", "CountDays/remaining/absence state indicates partial-period calculation", component.TemporalProrationApplied ? 0.82m : 0.68m);
            }

            if (component.Insurances || componentText.Contains("insurance") || componentText.Contains("insur") || componentText.Contains("تأمين"))
            {
                var source = row.InsuranceTrace != null
                    ? "insurance component is tied to " + row.InsuranceTrace.SourceFunction + "; posting uses accrued salary account " + row.InsuranceTrace.EmployeeAccruedSalaryAccount + " and insurance account " + row.InsuranceTrace.InsuranceCreditAccount
                    : "component appears related to employee insurance calculation";
                var confidence = row.InsuranceTrace != null && row.InsuranceTrace.RuntimeFunctionInsurance != 0 ? 0.86m : 0.75m;
                return Classify("insurance issue", source, confidence);
            }

            if (component.AdvancePaymentAccount || componentText.Contains("advance") || componentText.Contains("سلف"))
            {
                return Classify("advance deduction timing", "component is tied to advance/prepayment account behavior", 0.74m);
            }

            if (componentText.Contains("absence") || componentText.Contains("absent") || componentText.Contains("late") || componentText.Contains("غياب") || componentText.Contains("تأخير") || componentText.Contains("اضافي") || componentText.Contains("إضافي"))
            {
                return Classify("attendance issue", "component name indicates attendance, absence, delay, or overtime behavior", 0.70m);
            }

            if (row.BranchId.HasValue && component.SnapshotValue == 0 && component.SourceValue != 0)
            {
                return Classify("branch/project filtering issue", "reconstructed source exists while snapshot is empty for a branch-scoped employee", 0.55m);
            }

            if (component.SourceValue == 0 && component.SnapshotValue != 0 && (row.TotalInsuranceLegacy != 0 || row.AdvanceDeduction != 0 || row.VacationDeduction != 0))
            {
                return Classify("missing runtime scalar output", "legacy scalar side effects exist but no component source was reconstructed", 0.60m);
            }

            return Classify("missing runtime scalar output", "no specific classification matched; VB6 runtime grid/scalar behavior still needs tracing", 0.35m);
        }

        private static bool IsProrationCandidate(SalaryRunEmployeeRow row, PayrollCompatibilityComponent component)
        {
            var fullMonthDays = component.TemporalMonthDayNo > 0 ? component.TemporalMonthDayNo : (row.MonthIs30Days ? 30m : (row.PayrollMonthDays.HasValue ? row.PayrollMonthDays.Value : 0m));
            return component.SourceValue != component.SnapshotValue
                && (component.TemporalProrationApplied
                    || component.TemporalCountFlag
                    || row.CountDays > 0 && fullMonthDays > 0 && row.CountDays != fullMonthDays
                    || row.AbsentDays != 0);
        }

        private static TemporalCompatibilityContext CalculateTemporalContext(SalaryRunRequest request, SalaryRunEmployeeRow row, PayrollCompatibilityComponent component)
        {
            var year = request != null ? request.Year : DateTime.Today.Year;
            var month = request != null ? request.Month : DateTime.Today.Month;
            var calendarMonthDays = row.PayrollMonthDays.HasValue && row.PayrollMonthDays.Value > 0
                ? row.PayrollMonthDays.Value
                : DateTime.DaysInMonth(year, month);
            var payrollMonthDayNo = row.MonthIs30Days ? 30m : calendarMonthDays;
            var context = new TemporalCompatibilityContext
            {
                CalendarMonthDays = calendarMonthDays,
                PayrollMonthDayNo = payrollMonthDayNo,
                PayrollDays = row.CountDays > 0 ? row.CountDays : payrollMonthDayNo,
                ExpectedDenominator = payrollMonthDayNo,
                ActualDenominator = payrollMonthDayNo,
                NumeratorDays = row.CountDays > 0 ? row.CountDays : payrollMonthDayNo,
                BranchProjectScope = row.BranchId.HasValue ? "BranchId=" + row.BranchId.Value : "all-branches",
                DenominatorReason = "SystemOptions.MonthIs30days",
                RulePath = "full-period"
            };

            var periodEnd = new DateTime(year, month, calendarMonthDays);
            var temporalStart = GetTemporalStartDate(row, year, month, out var startReason);
            if (temporalStart.HasValue)
            {
                var actualDaysFromStart = (decimal)(periodEnd.Date - temporalStart.Value.Date).TotalDays + 1m;
                if (actualDaysFromStart < 0)
                {
                    actualDaysFromStart = 0;
                }

                var payrollDays = actualDaysFromStart == calendarMonthDays ? 30m : actualDaysFromStart;
                context.CountFlag = true;
                context.PayrollDays = payrollDays;
                context.NumeratorDays = payrollDays;
                context.RulePath = startReason + "-same-month";
                context.VacationOverlap = startReason == "lastHolidaydate";

                if (component.Culc30OrReminder.GetValueOrDefault() == 0)
                {
                    context.ActualDenominator = payrollMonthDayNo;
                    context.ExpectedDenominator = payrollMonthDayNo;
                    context.NumeratorDays = payrollDays;
                    context.DenominatorReason = row.MonthIs30Days ? "MonthIs30days=1 uses 30-day basis" : "MonthIs30days=0 uses calendar month basis";
                }
                else
                {
                    context.ActualDenominator = calendarMonthDays;
                    context.ExpectedDenominator = calendarMonthDays;
                    context.NumeratorDays = actualDaysFromStart;
                    context.DenominatorReason = "culc30orRminder=1 uses actual calendar month days";
                }
            }

            if (row.CountDays <= 0 && context.PayrollDays > 0)
            {
                row.CountDays = context.PayrollDays;
                row.RemainingDays = row.CountDays - row.AbsentDays - row.VacationDays;
            }

            context.ProrationBypassed = context.CountFlag && component.ShowMofradAll;
            context.ProrationApplied = context.CountFlag
                && !component.ShowMofradAll
                && !component.FixedOrChanged
                && component.RawSourceValue != 0
                && context.ActualDenominator > 0
                && context.NumeratorDays != context.ActualDenominator;

            if (context.ProrationApplied)
            {
                context.RulePath = context.RulePath + "|fixed-component-prorated";
            }
            else if (context.ProrationBypassed)
            {
                context.RulePath = context.RulePath + "|showMofradAll-bypass";
            }
            else if (component.FixedOrChanged)
            {
                context.RulePath = context.RulePath + "|changed-component-no-proration";
            }

            return context;
        }

        private static DateTime? GetTemporalStartDate(SalaryRunEmployeeRow row, int year, int month, out string reason)
        {
            reason = string.Empty;
            DateTime start;
            if (row.HiringDate.HasValue && row.HiringDate.Value.Year == year && row.HiringDate.Value.Month == month)
            {
                start = row.HiringDate.Value.Date;
                reason = "BignDateWork";
            }
            else
            {
                start = DateTime.MinValue;
            }

            if (row.LastHolidayDate.HasValue && row.LastHolidayDate.Value.Year == year && row.LastHolidayDate.Value.Month == month)
            {
                start = row.LastHolidayDate.Value.Date;
                reason = "lastHolidaydate";
            }

            return string.IsNullOrEmpty(reason) ? (DateTime?)null : start;
        }

        private static decimal GetPayrollMonthDayNo(bool monthIs30Days, int year, int month)
        {
            return monthIs30Days ? 30m : DateTime.DaysInMonth(year, month);
        }

        private static int CountActiveSources(PayrollCompatibilityComponent component)
        {
            var count = 0;
            if (component.FixedSourceValue != 0) count++;
            if (component.ChangedSourceValue != 0) count++;
            if (component.OverrideSourceValue != 0) count++;
            return count;
        }

        private static MismatchClassification Classify(string category, string likelySource, decimal confidenceScore)
        {
            return new MismatchClassification
            {
                Category = category,
                LikelySource = likelySource,
                ConfidenceScore = confidenceScore
            };
        }

        public SalaryRunSaveResult SaveSalaryRun(SalaryRunRequest request, int userId)
        {
            request = NormalizeSalaryRequest(request);
            var preview = PreviewSalaryRun(request);
            if (preview.Rows.Count == 0)
            {
                if (preview.ExcludedDuplicateEmployees > 0)
                {
                    throw new InvalidOperationException("تم استبعاد كل الموظفين لأنهم مدرجون بالفعل في مسير آخر لنفس الشهر. استخدم السماح بالتكرار فقط عند وجود صلاحية ومراجعة واضحة.");
                }

                throw new InvalidOperationException("لا توجد بيانات موظفين صالحة لإنشاء مسير الرواتب للفترة المحددة.");
            }

            var result = new SalaryRunSaveResult();
            var sgn = preview.Request.Year.ToString() + preview.Request.Month.ToString();
            var periodStart = new DateTime(preview.Request.Year, preview.Request.Month, 1);
            var periodEnd = periodStart.AddMonths(1).AddDays(-1);

            using (var connection = OpenConnection())
            using (var transaction = connection.BeginTransaction(IsolationLevel.Serializable))
            {
                var payrollRunId = SavePayrollRunSnapshot(connection, transaction, preview, request, userId);
                result.PayrollRunId = payrollRunId;
                result.SnapshotRows = preview.Rows.Count;
                result.ExcludedDuplicateEmployees = preview.ExcludedDuplicateEmployees;
                ValidateSalaryRunDuplicates(connection, transaction, preview.Request, sgn);
                foreach (var row in preview.Rows)
                {
                    if (row.IsApproved)
                    {
                        result.SkippedRows++;
                        continue;
                    }

                    var existingSalaryRowId = row.ExistingSalaryRowId ?? FindSalaryRunRowForUpdate(connection, transaction, sgn, row.EmployeeId);
                    if (existingSalaryRowId.HasValue)
                    {
                        row.ExistingSalaryRowId = existingSalaryRowId;
                        var updated = UpdateSalaryRunRow(connection, transaction, row, preview.Request, sgn);
                        result.UpdatedRows += updated;
                        if (updated == 0)
                        {
                            result.SkippedRows++;
                        }
                    }
                    else
                    {
                        row.ExistingSalaryRowId = InsertSalaryRunRow(connection, transaction, row, preview.Request, sgn);
                        result.InsertedRows++;
                    }

                    if (payrollRunId > 0)
                    {
                        UpsertPayrollRunEmployee(connection, transaction, payrollRunId, row, userId);
                    }

                    SaveMedicalDeductionAudit(connection, transaction, row, preview.Request, periodStart, periodEnd, userId);
                    result.TotalNet += row.NetSalary;
                }

                transaction.Commit();
            }

            result.Message = "تم حفظ مسير الرواتب رقم " + (result.PayrollRunId.HasValue ? result.PayrollRunId.Value.ToString() : "-") + " كـ Snapshot. تمت إضافة " + result.InsertedRows.ToString() + " صف وتحديث " + result.UpdatedRows.ToString() + " صف، وتجاوز " + result.SkippedRows.ToString() + " صف مقفل أو مدفوع.";
            return result;
        }

        private int SavePayrollRunSnapshot(SqlConnection connection, SqlTransaction transaction, SalaryRunPreview preview, SalaryRunRequest request, int userId)
        {
            if (!TableExists(connection, transaction, "PayrollRunHeader") || !TableExists(connection, transaction, "PayrollRunEmployees"))
            {
                return 0;
            }

            if (!request.AllowDuplicateEmployees)
            {
                ValidatePayrollRunDuplicateEmployees(connection, transaction, request, preview.Rows.Select(x => x.EmployeeId).ToList());
            }

            var payrollRunId = request.PayrollRunId.GetValueOrDefault();
            var periodStart = new DateTime(request.Year, request.Month, 1);
            var periodEnd = periodStart.AddMonths(1).AddDays(-1);
            if (payrollRunId <= 0)
            {
                using (var command = CreateCommand(connection, transaction, @"
INSERT INTO dbo.PayrollRunHeader
(RunName, PeriodYear, PeriodMonth, PeriodFrom, PeriodTo, BranchId, DepartmentId, EmployeeScope, SelectionMode,
 AllowDuplicateEmployees, ExcludeAlreadyIncluded, TotalBasic, TotalAllowances, TotalDeductions, TotalAdvance,
 TotalMedicalInsurance, TotalNet, CreatedBy)
VALUES
(@RunName, @Year, @Month, @PeriodFrom, @PeriodTo, @BranchId, @DepartmentId, @EmployeeScope, @SelectionMode,
 @AllowDuplicateEmployees, @ExcludeAlreadyIncluded, @TotalBasic, @TotalAllowances, @TotalDeductions, @TotalAdvance,
 @TotalMedicalInsurance, @TotalNet, @UserId);
SELECT CONVERT(int, SCOPE_IDENTITY());"))
                {
                    AddPayrollRunHeaderParameters(command, preview, request, periodStart, periodEnd, userId);
                    payrollRunId = Convert.ToInt32(command.ExecuteScalar());
                }
            }
            else
            {
                using (var command = CreateCommand(connection, transaction, @"
UPDATE dbo.PayrollRunHeader
SET RunName = @RunName,
    BranchId = @BranchId,
    DepartmentId = @DepartmentId,
    EmployeeScope = @EmployeeScope,
    SelectionMode = @SelectionMode,
    AllowDuplicateEmployees = @AllowDuplicateEmployees,
    ExcludeAlreadyIncluded = @ExcludeAlreadyIncluded,
    RebuildCount = RebuildCount + @RebuildIncrement,
    TotalBasic = @TotalBasic,
    TotalAllowances = @TotalAllowances,
    TotalDeductions = @TotalDeductions,
    TotalAdvance = @TotalAdvance,
    TotalMedicalInsurance = @TotalMedicalInsurance,
    TotalNet = @TotalNet,
    UpdatedAt = GETDATE(),
    UpdatedBy = @UserId
WHERE PayrollRunId = @PayrollRunId
  AND ISNULL(IsPosted, 0) = 0
  AND ISNULL(IsCancelled, 0) = 0;"))
                {
                    AddPayrollRunHeaderParameters(command, preview, request, periodStart, periodEnd, userId);
                    command.Parameters.Add("@PayrollRunId", SqlDbType.Int).Value = payrollRunId;
                    command.Parameters.Add("@RebuildIncrement", SqlDbType.Int).Value = request.RebuildEmployees ? 1 : 0;
                    if (command.ExecuteNonQuery() == 0)
                    {
                        throw new InvalidOperationException("لا يمكن تعديل مسير مرحل أو ملغى. افتح مسير غير مرحل أو أنشئ مسيرًا جديدًا.");
                    }
                }

                if (request.RebuildEmployees)
                {
                    using (var delete = CreateCommand(connection, transaction, "DELETE FROM dbo.PayrollRunEmployees WHERE PayrollRunId = @PayrollRunId;"))
                    {
                        delete.Parameters.Add("@PayrollRunId", SqlDbType.Int).Value = payrollRunId;
                        delete.ExecuteNonQuery();
                    }
                }
            }

            foreach (var row in preview.Rows)
            {
                row.PayrollRunId = payrollRunId;
                UpsertPayrollRunEmployee(connection, transaction, payrollRunId, row, userId);
            }

            SavePayrollAdvanceDeductionSnapshot(connection, transaction, payrollRunId, preview.Rows, userId);

            return payrollRunId;
        }

        private static void AddPayrollRunHeaderParameters(SqlCommand command, SalaryRunPreview preview, SalaryRunRequest request, DateTime periodStart, DateTime periodEnd, int userId)
        {
            command.Parameters.Add("@RunName", SqlDbType.NVarChar, 200).Value = (object)FirstNonEmpty(request.RunName, "مسير " + request.Month + "/" + request.Year) ?? DBNull.Value;
            command.Parameters.Add("@Year", SqlDbType.Int).Value = request.Year;
            command.Parameters.Add("@Month", SqlDbType.Int).Value = request.Month;
            command.Parameters.Add("@PeriodFrom", SqlDbType.DateTime).Value = periodStart;
            command.Parameters.Add("@PeriodTo", SqlDbType.DateTime).Value = periodEnd;
            AddNullable(command, "@BranchId", SqlDbType.Int, request.BranchId);
            AddNullable(command, "@DepartmentId", SqlDbType.Int, request.DepartmentId);
            command.Parameters.Add("@EmployeeScope", SqlDbType.NVarChar, 50).Value = string.IsNullOrWhiteSpace(request.ManualEmployeeIds) ? "Filtered" : "Manual";
            command.Parameters.Add("@SelectionMode", SqlDbType.NVarChar, 50).Value = request.RebuildEmployees ? "Rebuild" : "Snapshot";
            command.Parameters.Add("@AllowDuplicateEmployees", SqlDbType.Bit).Value = request.AllowDuplicateEmployees;
            command.Parameters.Add("@ExcludeAlreadyIncluded", SqlDbType.Bit).Value = request.ExcludeAlreadyIncluded || request.OnlyUnincluded;
            command.Parameters.Add("@TotalBasic", SqlDbType.Money).Value = preview.TotalBasic;
            command.Parameters.Add("@TotalAllowances", SqlDbType.Money).Value = preview.TotalAdditions;
            command.Parameters.Add("@TotalDeductions", SqlDbType.Money).Value = preview.TotalDeductions;
            command.Parameters.Add("@TotalAdvance", SqlDbType.Money).Value = preview.TotalAdvance;
            command.Parameters.Add("@TotalMedicalInsurance", SqlDbType.Money).Value = preview.TotalMedicalInsurance;
            command.Parameters.Add("@TotalNet", SqlDbType.Money).Value = preview.TotalNet;
            command.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
        }

        private static void UpsertPayrollRunEmployee(SqlConnection connection, SqlTransaction transaction, int payrollRunId, SalaryRunEmployeeRow row, int userId)
        {
            var hasVacationSnapshotColumns = ColumnExists(connection, transaction, "PayrollRunEmployees", "VacationDays")
                && ColumnExists(connection, transaction, "PayrollRunEmployees", "VacationDeduction")
                && ColumnExists(connection, transaction, "PayrollRunEmployees", "VacationSalaryValue")
                && ColumnExists(connection, transaction, "PayrollRunEmployees", "AbsentDays")
                && ColumnExists(connection, transaction, "PayrollRunEmployees", "CountDays")
                && ColumnExists(connection, transaction, "PayrollRunEmployees", "RemainingDays");
            var vacationUpdateSet = hasVacationSnapshotColumns
                ? @"
        VacationDays = @VacationDays,
        VacationDeduction = @VacationDeduction,
        VacationSalaryValue = @VacationSalaryValue,
        AbsentDays = @AbsentDays,
        CountDays = @CountDays,
        RemainingDays = @RemainingDays,"
                : string.Empty;
            var vacationInsertColumns = hasVacationSnapshotColumns
                ? ", VacationDays, VacationDeduction, VacationSalaryValue, AbsentDays, CountDays, RemainingDays"
                : string.Empty;
            var vacationInsertValues = hasVacationSnapshotColumns
                ? ", @VacationDays, @VacationDeduction, @VacationSalaryValue, @AbsentDays, @CountDays, @RemainingDays"
                : string.Empty;

            using (var command = CreateCommand(connection, transaction, @"
IF EXISTS (SELECT 1 FROM dbo.PayrollRunEmployees WHERE PayrollRunId = @PayrollRunId AND EmployeeId = @EmployeeId)
BEGIN
    UPDATE dbo.PayrollRunEmployees
    SET EmployeeCode = @EmployeeCode,
        EmployeeName = @EmployeeName,
        BranchId = @BranchId,
        BranchName = @BranchName,
        DepartmentId = @DepartmentId,
        DepartmentName = @DepartmentName,
        ProjectId = @ProjectId,
        BasicSalary = @BasicSalary,
        Allowances = @Allowances,
        VariableAdditions = @VariableAdditions,
        Deductions = @Deductions,
        Advances = @Advances,
        MedicalInsurance = @MedicalInsurance,
        MedicalInsuranceCompanyCost = @MedicalInsuranceCompanyCost,
        " + vacationUpdateSet + @"
        TotalBeforeDeductions = @TotalBeforeDeductions,
        TotalDeductions = @TotalDeductions,
        NetSalary = @NetSalary,
        EmployeeStatusAtRunTime = @EmployeeStatusAtRunTime,
        ExistingSalaryRowId = @ExistingSalaryRowId,
        AccountCode = @AccountCode,
        AccruedSalaryAccountCode = @AccruedSalaryAccountCode,
        AdvancePaymentAccountCode = @AdvancePaymentAccountCode,
        MedicalInsuranceEmployeeAccountCode = @MedicalInsuranceEmployeeAccountCode,
        MedicalInsuranceCompanyAccountCode = @MedicalInsuranceCompanyAccountCode,
        UpdatedAt = GETDATE(),
        UpdatedBy = @UserId
    WHERE PayrollRunId = @PayrollRunId AND EmployeeId = @EmployeeId AND ISNULL(IsPosted, 0) = 0;
END
ELSE
BEGIN
    INSERT INTO dbo.PayrollRunEmployees
    (PayrollRunId, EmployeeId, EmployeeCode, EmployeeName, BranchId, BranchName, DepartmentId, DepartmentName, ProjectId,
     BasicSalary, Allowances, VariableAdditions, Deductions, Advances, MedicalInsurance, MedicalInsuranceCompanyCost,
     TotalBeforeDeductions, TotalDeductions, NetSalary, EmployeeStatusAtRunTime, ExistingSalaryRowId, IsPosted" + vacationInsertColumns + @",
     AccountCode, AccruedSalaryAccountCode, AdvancePaymentAccountCode, MedicalInsuranceEmployeeAccountCode,
     MedicalInsuranceCompanyAccountCode, CreatedBy)
    VALUES
    (@PayrollRunId, @EmployeeId, @EmployeeCode, @EmployeeName, @BranchId, @BranchName, @DepartmentId, @DepartmentName, @ProjectId,
     @BasicSalary, @Allowances, @VariableAdditions, @Deductions, @Advances, @MedicalInsurance, @MedicalInsuranceCompanyCost,
     @TotalBeforeDeductions, @TotalDeductions, @NetSalary, @EmployeeStatusAtRunTime, @ExistingSalaryRowId, @IsPosted" + vacationInsertValues + @",
     @AccountCode, @AccruedSalaryAccountCode, @AdvancePaymentAccountCode, @MedicalInsuranceEmployeeAccountCode,
     @MedicalInsuranceCompanyAccountCode, @UserId);
END"))
            {
                command.Parameters.Add("@PayrollRunId", SqlDbType.Int).Value = payrollRunId;
                command.Parameters.Add("@EmployeeId", SqlDbType.Int).Value = row.EmployeeId;
                command.Parameters.Add("@EmployeeCode", SqlDbType.VarChar, 50).Value = (object)row.EmployeeCode ?? DBNull.Value;
                command.Parameters.Add("@EmployeeName", SqlDbType.NVarChar, 250).Value = (object)row.EmployeeName ?? DBNull.Value;
                AddNullable(command, "@BranchId", SqlDbType.Int, row.BranchId);
                command.Parameters.Add("@BranchName", SqlDbType.NVarChar, 250).Value = (object)row.BranchName ?? DBNull.Value;
                AddNullable(command, "@DepartmentId", SqlDbType.Int, row.DepartmentId);
                command.Parameters.Add("@DepartmentName", SqlDbType.NVarChar, 250).Value = (object)row.DepartmentName ?? DBNull.Value;
                AddNullable(command, "@ProjectId", SqlDbType.Int, row.ProjectId);
                command.Parameters.Add("@BasicSalary", SqlDbType.Money).Value = row.BasicSalary;
                command.Parameters.Add("@Allowances", SqlDbType.Money).Value = row.SalaryAllowances;
                command.Parameters.Add("@VariableAdditions", SqlDbType.Money).Value = row.VariableAdditions;
                command.Parameters.Add("@Deductions", SqlDbType.Money).Value = row.ExistingDiscounts;
                command.Parameters.Add("@Advances", SqlDbType.Money).Value = row.AdvanceDeduction;
                command.Parameters.Add("@MedicalInsurance", SqlDbType.Money).Value = row.MedicalInsuranceDeduction;
                command.Parameters.Add("@MedicalInsuranceCompanyCost", SqlDbType.Money).Value = row.MedicalInsuranceCompanyCost;
                if (hasVacationSnapshotColumns)
                {
                    command.Parameters.Add("@VacationDays", SqlDbType.Money).Value = row.VacationDays;
                    command.Parameters.Add("@VacationDeduction", SqlDbType.Money).Value = row.VacationDeduction;
                    command.Parameters.Add("@VacationSalaryValue", SqlDbType.Money).Value = row.VacationSalaryValue;
                    command.Parameters.Add("@AbsentDays", SqlDbType.Money).Value = row.AbsentDays;
                    command.Parameters.Add("@CountDays", SqlDbType.Money).Value = row.CountDays;
                    command.Parameters.Add("@RemainingDays", SqlDbType.Money).Value = row.RemainingDays;
                }
                command.Parameters.Add("@TotalBeforeDeductions", SqlDbType.Money).Value = row.TotalBeforeDeductions;
                command.Parameters.Add("@TotalDeductions", SqlDbType.Money).Value = row.TotalDeductions;
                command.Parameters.Add("@NetSalary", SqlDbType.Money).Value = row.NetSalary;
                command.Parameters.Add("@EmployeeStatusAtRunTime", SqlDbType.NVarChar, 100).Value = (object)FirstNonEmpty(row.EmployeeStatusAtRunTime, row.IsApproved ? "PostedSnapshot" : "ActiveAtRunTime") ?? DBNull.Value;
                AddNullable(command, "@ExistingSalaryRowId", SqlDbType.Int, row.ExistingSalaryRowId);
                command.Parameters.Add("@IsPosted", SqlDbType.Bit).Value = row.IsApproved;
                command.Parameters.Add("@AccountCode", SqlDbType.NVarChar, 50).Value = (object)row.EmployeeAccountCode ?? DBNull.Value;
                command.Parameters.Add("@AccruedSalaryAccountCode", SqlDbType.NVarChar, 50).Value = (object)row.AccruedSalaryAccountCode ?? DBNull.Value;
                command.Parameters.Add("@AdvancePaymentAccountCode", SqlDbType.NVarChar, 50).Value = (object)row.AdvancePaymentAccountCode ?? DBNull.Value;
                command.Parameters.Add("@MedicalInsuranceEmployeeAccountCode", SqlDbType.NVarChar, 50).Value = (object)row.MedicalInsuranceEmployeeAccountCode ?? DBNull.Value;
                command.Parameters.Add("@MedicalInsuranceCompanyAccountCode", SqlDbType.NVarChar, 50).Value = (object)row.MedicalInsuranceCompanyAccountCode ?? DBNull.Value;
                command.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
                command.ExecuteNonQuery();
            }
        }

        private static void SavePayrollAdvanceDeductionSnapshot(SqlConnection connection, SqlTransaction transaction, int payrollRunId, IList<SalaryRunEmployeeRow> rows, int userId)
        {
            if (payrollRunId <= 0 || rows == null || rows.Count == 0 || !TableExists(connection, transaction, "PayrollRunAdvanceDeductions"))
            {
                return;
            }

            using (var delete = CreateCommand(connection, transaction, @"
DELETE FROM dbo.PayrollRunAdvanceDeductions
WHERE PayrollRunId = @PayrollRunId
  AND ISNULL(IsPosted, 0) = 0;"))
            {
                delete.Parameters.Add("@PayrollRunId", SqlDbType.Int).Value = payrollRunId;
                delete.ExecuteNonQuery();
            }

            using (var command = CreateCommand(connection, transaction, @"
IF NOT EXISTS (
    SELECT 1
    FROM dbo.PayrollRunAdvanceDeductions
    WHERE PayrollRunId = @PayrollRunId
      AND AdvanceDetailTableId = @AdvanceDetailTableId
)
AND NOT EXISTS (
    SELECT 1
    FROM dbo.PayrollRunAdvanceDeductions existing
    LEFT JOIN dbo.PayrollRunHeader h ON h.PayrollRunId = existing.PayrollRunId
    WHERE existing.AdvanceDetailTableId = @AdvanceDetailTableId
      AND existing.PayrollRunId <> @PayrollRunId
      AND (h.PayrollRunId IS NULL OR ISNULL(h.IsCancelled, 0) = 0)
)
BEGIN
    INSERT INTO dbo.PayrollRunAdvanceDeductions
    (PayrollRunId, EmployeeId, SalaryRowId, AdvanceId, AdvanceDetailTableId, PartNo, PartDate, PartValue, CreatedBy)
    VALUES
    (@PayrollRunId, @EmployeeId, @SalaryRowId, @AdvanceId, @AdvanceDetailTableId, @PartNo, @PartDate, @PartValue, @UserId);
END"))
            {
                command.Parameters.Add("@PayrollRunId", SqlDbType.Int);
                command.Parameters.Add("@EmployeeId", SqlDbType.Int);
                command.Parameters.Add("@SalaryRowId", SqlDbType.Int);
                command.Parameters.Add("@AdvanceId", SqlDbType.Int);
                command.Parameters.Add("@AdvanceDetailTableId", SqlDbType.Int);
                command.Parameters.Add("@PartNo", SqlDbType.Int);
                command.Parameters.Add("@PartDate", SqlDbType.DateTime);
                command.Parameters.Add("@PartValue", SqlDbType.Money);
                command.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;

                foreach (var row in rows)
                {
                    if (row.AdvanceInstallments == null || row.AdvanceInstallments.Count == 0)
                    {
                        continue;
                    }

                    foreach (var part in row.AdvanceInstallments.Where(x => x.TableId > 0 && x.PartValue > 0))
                    {
                        command.Parameters["@PayrollRunId"].Value = payrollRunId;
                        command.Parameters["@EmployeeId"].Value = row.EmployeeId;
                        command.Parameters["@SalaryRowId"].Value = row.ExistingSalaryRowId.HasValue ? (object)row.ExistingSalaryRowId.Value : DBNull.Value;
                        command.Parameters["@AdvanceId"].Value = part.AdvanceId;
                        command.Parameters["@AdvanceDetailTableId"].Value = part.TableId;
                        command.Parameters["@PartNo"].Value = part.PartNo;
                        command.Parameters["@PartDate"].Value = ParseDisplayDate(part.PartDate);
                        command.Parameters["@PartValue"].Value = part.PartValue;
                        command.ExecuteNonQuery();
                    }
                }
            }
        }

        private static void ValidateSalaryRunDuplicates(SqlConnection connection, SqlTransaction transaction, SalaryRunRequest request, string sgn)
        {
            using (var command = CreateCommand(connection, transaction, @"
SELECT TOP (10) emp_id, COUNT(1) AS DuplicateCount
FROM dbo.emp_salary WITH (UPDLOCK, HOLDLOCK)
WHERE sgn = @Sgn
  AND (@BranchId IS NULL OR BranchId = @BranchId)
  AND (@DepartmentId IS NULL OR DepartmentID = @DepartmentId)
  AND (@EmployeeId IS NULL OR emp_id = @EmployeeId)
GROUP BY emp_id
HAVING COUNT(1) > 1
ORDER BY emp_id;"))
            {
                command.Parameters.Add("@Sgn", SqlDbType.VarChar, 20).Value = sgn;
                AddNullable(command, "@BranchId", SqlDbType.Int, request.BranchId);
                AddNullable(command, "@DepartmentId", SqlDbType.Int, request.DepartmentId);
                AddNullable(command, "@EmployeeId", SqlDbType.Int, request.EmployeeId);
                var employees = new List<string>();
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        employees.Add(Convert.ToString(reader["emp_id"]) + " (" + Convert.ToString(reader["DuplicateCount"]) + ")");
                    }
                }

                if (employees.Count > 0)
                {
                    throw new InvalidOperationException("لا يمكن حفظ مسير الرواتب لوجود صفوف مكررة لنفس الموظف والفترة في emp_salary. الموظفون: " + string.Join(", ", employees.ToArray()));
                }
            }
        }

        private static void ValidatePayrollRunDuplicateEmployees(SqlConnection connection, SqlTransaction transaction, SalaryRunRequest request, IList<int> employeeIds)
        {
            if (!TableExists(connection, transaction, "PayrollRunHeader") || !TableExists(connection, transaction, "PayrollRunEmployees") || employeeIds == null || employeeIds.Count == 0)
            {
                return;
            }

            var distinctIds = employeeIds.Distinct().ToList();
            var csv = string.Join(",", distinctIds.Select(x => x.ToString()).ToArray());
            using (var command = CreateCommand(connection, transaction, @"
SELECT TOP (20) d.EmployeeId, ISNULL(d.EmployeeCode, '') AS EmployeeCode, ISNULL(d.EmployeeName, '') AS EmployeeName, h.PayrollRunId
FROM dbo.PayrollRunHeader h WITH (UPDLOCK, HOLDLOCK)
INNER JOIN dbo.PayrollRunEmployees d WITH (UPDLOCK, HOLDLOCK) ON d.PayrollRunId = h.PayrollRunId
WHERE h.PeriodYear = @Year
  AND h.PeriodMonth = @Month
  AND ISNULL(h.IsCancelled, 0) = 0
  AND d.EmployeeId IN (" + csv + @")
  AND (@PayrollRunId IS NULL OR h.PayrollRunId <> @PayrollRunId)
ORDER BY d.EmployeeId;"))
            {
                command.Parameters.Add("@Year", SqlDbType.Int).Value = request.Year;
                command.Parameters.Add("@Month", SqlDbType.Int).Value = request.Month;
                AddNullable(command, "@PayrollRunId", SqlDbType.Int, request.PayrollRunId);
                var duplicates = new List<string>();
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        duplicates.Add(ReadString(reader, "EmployeeCode") + " - " + ReadString(reader, "EmployeeName") + " (مسير " + ReadInt(reader, "PayrollRunId").ToString() + ")");
                    }
                }

                if (duplicates.Count > 0)
                {
                    throw new InvalidOperationException("لا يمكن إدراج نفس الموظف في أكثر من مسير لنفس الشهر بدون اختيار السماح بالتكرار. الموظفون: " + string.Join("، ", duplicates.ToArray()));
                }
            }
        }

        private static int? FindSalaryRunRowForUpdate(SqlConnection connection, SqlTransaction transaction, string sgn, int employeeId)
        {
            using (var command = CreateCommand(connection, transaction, @"
SELECT TOP (1) id
FROM dbo.emp_salary WITH (UPDLOCK, HOLDLOCK)
WHERE sgn = @Sgn AND emp_id = @EmployeeId
ORDER BY id;"))
            {
                command.Parameters.Add("@Sgn", SqlDbType.VarChar, 20).Value = sgn;
                command.Parameters.Add("@EmployeeId", SqlDbType.Int).Value = employeeId;
                var value = command.ExecuteScalar();
                return value == null || value == DBNull.Value ? (int?)null : Convert.ToInt32(value);
            }
        }

        private static int UpdateSalaryRunRow(SqlConnection connection, SqlTransaction transaction, SalaryRunEmployeeRow row, SalaryRunRequest request, string sgn)
        {
            var hasLegacyVacationColumns = ColumnExists(connection, transaction, "emp_salary", "TotalVacValue") && ColumnExists(connection, transaction, "emp_salary", "vacDay");
            var legacyVacationSet = hasLegacyVacationColumns
                ? @",
    TotalVacValue = @VacationSalaryValue,
    vacDay = @VacationDays"
                : string.Empty;
            using (var command = CreateCommand(connection, transaction, @"
UPDATE dbo.emp_salary
SET Emp_Code = @Code,
    Emp_Name = @Name,
    Emp_Salary = @Basic,
    total1 = @TotalBefore,
    TotalAdvance = @Advance,
    TotalDiscount = @Discounts,
    total2 = @TotalDeductions,
    EmpTotalNet = @Net,
    ToalInsurance = @Insurance,
    VoCation3 = @VacationDeduction,
    AbcentDay = @AbsentDays,
    CountDays = @CountDays,
    RemainDay = @RemainingDays,
    project_id = @ProjectId,
    BranchId = @BranchId,
    DepartmentID = @DepartmentId,
    RecordDate = GETDATE()" + legacyVacationSet + BuildComponentUpdateSet(row) + @"
WHERE id = @Id AND ISNULL(payed, 0) = 0;"))
            {
                AddSalaryParameters(command, row, request, sgn);
                AddSalarySnapshotParameters(command, row);
                AddLegacyVacationSalaryParameters(command, row, hasLegacyVacationColumns);
                command.Parameters.Add("@Id", SqlDbType.Int).Value = row.ExistingSalaryRowId.Value;
                return command.ExecuteNonQuery();
            }
        }

        private static int InsertSalaryRunRow(SqlConnection connection, SqlTransaction transaction, SalaryRunEmployeeRow row, SalaryRunRequest request, string sgn)
        {
            var hasLegacyVacationColumns = ColumnExists(connection, transaction, "emp_salary", "TotalVacValue") && ColumnExists(connection, transaction, "emp_salary", "vacDay");
            var legacyVacationInsertColumns = hasLegacyVacationColumns ? ", TotalVacValue, vacDay" : string.Empty;
            var legacyVacationInsertValues = hasLegacyVacationColumns ? ", @VacationSalaryValue, @VacationDays" : string.Empty;
            using (var command = CreateCommand(connection, transaction, @"
INSERT INTO dbo.emp_salary
(emp_id, Emp_Code, Emp_Name, Emp_Salary, total1, TotalAdvance, TotalDiscount, total2, EmpTotalNet, sgn, m_year, m_month, payed, DepartmentID, BranchId, project_id, ToalInsurance, VoCation3, AbcentDay, CountDays, RemainDay, RecordDate" + legacyVacationInsertColumns + BuildComponentInsertColumns(row) + @")
VALUES
(@EmployeeId, @Code, @Name, @Basic, @TotalBefore, @Advance, @Discounts, @TotalDeductions, @Net, @Sgn, @YearText, @MonthText, 0, @DepartmentId, @BranchId, @ProjectId, @Insurance, @VacationDeduction, @AbsentDays, @CountDays, @RemainingDays, GETDATE()" + legacyVacationInsertValues + BuildComponentInsertValues(row) + @");
SELECT CONVERT(int, SCOPE_IDENTITY());"))
            {
                AddSalaryParameters(command, row, request, sgn);
                AddSalarySnapshotParameters(command, row);
                AddLegacyVacationSalaryParameters(command, row, hasLegacyVacationColumns);
                return Convert.ToInt32(command.ExecuteScalar());
            }
        }

        public IList<MedicalInsuranceSubscriptionReportRow> GetMedicalInsuranceSubscriptions(MedicalInsuranceReportFilter filter)
        {
            filter = filter ?? new MedicalInsuranceReportFilter();
            var rows = new List<MedicalInsuranceSubscriptionReportRow>();
            using (var connection = OpenConnection())
            using (var command = connection.CreateCommand())
            {
                if (!TableExists(connection, "EmployeeMedicalInsurance") || !TableExists(connection, "MedicalInsurancePlans") || !TableExists(connection, "MedicalInsuranceProviders"))
                {
                    return rows;
                }

                command.CommandText = @"
DECLARE @AsOfDate DATETIME;
SET @AsOfDate = ISNULL(@To, GETDATE());

SELECT e.Emp_ID, e.Emp_Code, e.Emp_Name, b.branch_name, dep.DepartmentName,
       pr.ProviderNameAr, pl.PlanNameAr, mi.PolicyNumber, mi.CardNumber,
       mi.StartDate, mi.EndDate, mi.IsActive, mi.MonthlyCost,
       mi.EmployeeMonthlyDeduction, mi.CompanyMonthlyCost,
       CASE
           WHEN ISNULL(mi.IsActive, 0) = 0 THEN N'Cancelled'
           WHEN mi.EndDate IS NOT NULL AND mi.EndDate < @AsOfDate THEN N'Expired'
           ELSE N'Active'
       END AS InsuranceStatus
FROM dbo.EmployeeMedicalInsurance mi WITH (NOLOCK)
INNER JOIN dbo.TblEmployee e WITH (NOLOCK) ON e.Emp_ID = mi.EmpId
LEFT JOIN dbo.TblBranchesData b WITH (NOLOCK) ON b.branch_id = e.BranchId
LEFT JOIN dbo.TblEmpDepartments dep WITH (NOLOCK) ON dep.DeparmentID = e.DepartmentID
LEFT JOIN dbo.MedicalInsurancePlans pl WITH (NOLOCK) ON pl.PlanId = mi.PlanId
LEFT JOIN dbo.MedicalInsuranceProviders pr WITH (NOLOCK) ON pr.ProviderId = pl.ProviderId
WHERE (@ActiveOnly = 0 OR mi.IsActive = 1)
  AND (@PlanId IS NULL OR mi.PlanId = @PlanId)
  AND (@ProviderId IS NULL OR pl.ProviderId = @ProviderId)
  AND (@BranchId IS NULL OR e.BranchId = @BranchId)
  AND (@DepartmentId IS NULL OR e.DepartmentID = @DepartmentId)
  AND (@EmployeeId IS NULL OR e.Emp_ID = @EmployeeId)
  AND (@From IS NULL OR mi.EndDate IS NULL OR mi.EndDate >= @From)
  AND (@To IS NULL OR mi.StartDate <= @To)
  AND (@Status = N'' OR
       (@Status = N'Active' AND ISNULL(mi.IsActive, 0) = 1 AND (mi.EndDate IS NULL OR mi.EndDate >= @AsOfDate)) OR
       (@Status = N'Expired' AND ISNULL(mi.IsActive, 0) = 1 AND mi.EndDate IS NOT NULL AND mi.EndDate < @AsOfDate) OR
       (@Status = N'Cancelled' AND ISNULL(mi.IsActive, 0) = 0))
ORDER BY mi.IsActive DESC, e.Emp_Name;";
                AddReportParameters(command, filter);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        rows.Add(new MedicalInsuranceSubscriptionReportRow
                        {
                            EmployeeId = ReadInt(reader, "Emp_ID"),
                            EmployeeCode = ReadString(reader, "Emp_Code"),
                            EmployeeName = ReadString(reader, "Emp_Name"),
                            BranchName = ReadString(reader, "branch_name"),
                            DepartmentName = ReadString(reader, "DepartmentName"),
                            ProviderName = ReadString(reader, "ProviderNameAr"),
                            PlanName = ReadString(reader, "PlanNameAr"),
                            PolicyNumber = ReadString(reader, "PolicyNumber"),
                            CardNumber = ReadString(reader, "CardNumber"),
                            StartDate = ReadNullableDate(reader, "StartDate"),
                            EndDate = ReadNullableDate(reader, "EndDate"),
                            IsActive = ReadBool(reader, "IsActive"),
                            Status = ReadString(reader, "InsuranceStatus"),
                            MonthlyCost = ReadDecimal(reader, "MonthlyCost"),
                            EmployeeMonthlyDeduction = ReadDecimal(reader, "EmployeeMonthlyDeduction"),
                            CompanyMonthlyCost = ReadDecimal(reader, "CompanyMonthlyCost")
                        });
                    }
                }
            }

            return rows;
        }

        public IList<MedicalInsuranceDeductionReportRow> GetMedicalInsuranceDeductions(MedicalInsuranceReportFilter filter)
        {
            filter = filter ?? new MedicalInsuranceReportFilter();
            var rows = new List<MedicalInsuranceDeductionReportRow>();
            using (var connection = OpenConnection())
            using (var command = connection.CreateCommand())
            {
                if (!TableExists(connection, "PayrollMedicalInsuranceDeduction"))
                {
                    return rows;
                }

                command.CommandText = @"
SELECT e.Emp_ID, e.Emp_Code, e.Emp_Name, b.branch_name, dep.DepartmentName,
       pr.ProviderNameAr, pl.PlanNameAr,
       d.[Year], d.[Month], d.PeriodFrom, d.PeriodTo, d.EmployeeDeduction, d.CompanyCost,
       ISNULL(s.payed, 0) AS IsPosted
FROM dbo.PayrollMedicalInsuranceDeduction d WITH (NOLOCK)
INNER JOIN dbo.TblEmployee e WITH (NOLOCK) ON e.Emp_ID = d.EmpId
LEFT JOIN dbo.EmployeeMedicalInsurance mi WITH (NOLOCK) ON mi.Id = d.EmployeeInsuranceId
LEFT JOIN dbo.MedicalInsurancePlans pl WITH (NOLOCK) ON pl.PlanId = mi.PlanId
LEFT JOIN dbo.MedicalInsuranceProviders pr WITH (NOLOCK) ON pr.ProviderId = pl.ProviderId
LEFT JOIN dbo.TblBranchesData b WITH (NOLOCK) ON b.branch_id = e.BranchId
LEFT JOIN dbo.TblEmpDepartments dep WITH (NOLOCK) ON dep.DeparmentID = e.DepartmentID
LEFT JOIN dbo.emp_salary s WITH (NOLOCK) ON s.emp_id = d.EmpId AND s.sgn = CONVERT(NVARCHAR(10), d.[Year]) + CONVERT(NVARCHAR(10), d.[Month])
WHERE (@PlanId IS NULL OR mi.PlanId = @PlanId)
  AND (@ProviderId IS NULL OR pl.ProviderId = @ProviderId)
  AND (@BranchId IS NULL OR e.BranchId = @BranchId)
  AND (@DepartmentId IS NULL OR e.DepartmentID = @DepartmentId)
  AND (@EmployeeId IS NULL OR e.Emp_ID = @EmployeeId)
  AND (@From IS NULL OR d.PeriodTo >= @From)
  AND (@To IS NULL OR d.PeriodFrom <= @To)
  AND (@PostingStatus = N'' OR (@PostingStatus = N'Posted' AND ISNULL(s.payed, 0) = 1) OR (@PostingStatus = N'Unposted' AND ISNULL(s.payed, 0) = 0))
ORDER BY d.PeriodFrom DESC, e.Emp_Name;";
                AddReportParameters(command, filter);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        rows.Add(new MedicalInsuranceDeductionReportRow
                        {
                            EmployeeId = ReadInt(reader, "Emp_ID"),
                            EmployeeCode = ReadString(reader, "Emp_Code"),
                            EmployeeName = ReadString(reader, "Emp_Name"),
                            BranchName = ReadString(reader, "branch_name"),
                            DepartmentName = ReadString(reader, "DepartmentName"),
                            ProviderName = ReadString(reader, "ProviderNameAr"),
                            PlanName = ReadString(reader, "PlanNameAr"),
                            PeriodFrom = ReadNullableDate(reader, "PeriodFrom").GetValueOrDefault(),
                            PeriodTo = ReadNullableDate(reader, "PeriodTo").GetValueOrDefault(),
                            Year = ReadInt(reader, "Year"),
                            Month = ReadInt(reader, "Month"),
                            EmployeeDeduction = ReadDecimal(reader, "EmployeeDeduction"),
                            CompanyCost = ReadDecimal(reader, "CompanyCost"),
                            IsPosted = ReadBool(reader, "IsPosted"),
                            PostingStatus = ReadBool(reader, "IsPosted") ? "مرحل" : "غير مرحل"
                        });
                    }
                }
            }

            return rows;
        }

        public MedicalInsuranceReportBundle GetMedicalInsuranceReportBundle(MedicalInsuranceReportFilter filter)
        {
            filter = NormalizeMedicalInsuranceReportFilter(filter);
            var result = new MedicalInsuranceReportBundle
            {
                Filter = filter,
                PeriodLabel = BuildMedicalInsurancePeriodLabel(filter)
            };

            result.Subscriptions = GetMedicalInsuranceSubscriptions(filter);
            result.MonthlyDeductions = GetMedicalInsuranceDeductions(filter);
            result.CompanyContributions = BuildMedicalInsuranceCompanyContributionSummary(result.MonthlyDeductions);
            result.Payables = GetMedicalInsurancePayableSummary(filter);
            result.PayrollIntegration = GetMedicalInsurancePayrollIntegration(filter);

            result.SubscriptionCount = result.Subscriptions.Count;
            result.ActiveCount = result.Subscriptions.Count(x => string.Equals(x.Status, "Active", StringComparison.OrdinalIgnoreCase));
            result.ExpiredCount = result.Subscriptions.Count(x => string.Equals(x.Status, "Expired", StringComparison.OrdinalIgnoreCase));
            result.CancelledCount = result.Subscriptions.Count(x => string.Equals(x.Status, "Cancelled", StringComparison.OrdinalIgnoreCase));
            result.TotalMonthlyCost = result.Subscriptions.Sum(x => x.MonthlyCost);
            result.TotalEmployeeDeduction = result.MonthlyDeductions.Sum(x => x.EmployeeDeduction);
            result.TotalCompanyCost = result.MonthlyDeductions.Sum(x => x.CompanyCost);
            result.TotalPayable = result.TotalEmployeeDeduction + result.TotalCompanyCost;
            result.PostedPayableCredit = result.Payables.Sum(x => x.PostedNetCredit);
            return result;
        }

        private IList<MedicalInsuranceContributionSummaryRow> BuildMedicalInsuranceCompanyContributionSummary(IList<MedicalInsuranceDeductionReportRow> rows)
        {
            return rows
                .GroupBy(x => string.IsNullOrWhiteSpace(x.ProviderName) ? "بدون شركة تأمين" : x.ProviderName)
                .Select(x => new MedicalInsuranceContributionSummaryRow
                {
                    GroupName = x.Key,
                    Employees = x.Select(r => r.EmployeeId).Distinct().Count(),
                    EmployeeDeduction = x.Sum(r => r.EmployeeDeduction),
                    CompanyCost = x.Sum(r => r.CompanyCost),
                    TotalPayable = x.Sum(r => r.EmployeeDeduction + r.CompanyCost)
                })
                .OrderByDescending(x => x.TotalPayable)
                .ToList();
        }

        private IList<MedicalInsurancePayableSummaryRow> GetMedicalInsurancePayableSummary(MedicalInsuranceReportFilter filter)
        {
            var rows = new List<MedicalInsurancePayableSummaryRow>();
            using (var connection = OpenConnection())
            using (var command = connection.CreateCommand())
            {
                if (!TableExists(connection, "PayrollMedicalInsuranceDeduction")
                    || !TableExists(connection, "MedicalInsurancePlans")
                    || !TableExists(connection, "MedicalInsuranceProviders"))
                {
                    return rows;
                }

                command.CommandText = @"
;WITH PayrollRows AS
(
    SELECT pr.ProviderId, pr.ProviderNameAr, pl.EmployeeDeductionAccountCode,
           d.EmployeeDeduction, d.CompanyCost, d.[Year], d.[Month]
    FROM dbo.PayrollMedicalInsuranceDeduction d WITH (NOLOCK)
    INNER JOIN dbo.TblEmployee e WITH (NOLOCK) ON e.Emp_ID = d.EmpId
    LEFT JOIN dbo.EmployeeMedicalInsurance mi WITH (NOLOCK) ON mi.Id = d.EmployeeInsuranceId
    LEFT JOIN dbo.MedicalInsurancePlans pl WITH (NOLOCK) ON pl.PlanId = mi.PlanId
    LEFT JOIN dbo.MedicalInsuranceProviders pr WITH (NOLOCK) ON pr.ProviderId = pl.ProviderId
    LEFT JOIN dbo.emp_salary s WITH (NOLOCK) ON s.emp_id = d.EmpId AND s.sgn = CONVERT(NVARCHAR(10), d.[Year]) + CONVERT(NVARCHAR(10), d.[Month])
    WHERE (@PlanId IS NULL OR mi.PlanId = @PlanId)
      AND (@ProviderId IS NULL OR pl.ProviderId = @ProviderId)
      AND (@BranchId IS NULL OR e.BranchId = @BranchId)
      AND (@DepartmentId IS NULL OR e.DepartmentID = @DepartmentId)
      AND (@EmployeeId IS NULL OR e.Emp_ID = @EmployeeId)
      AND (@From IS NULL OR d.PeriodTo >= @From)
      AND (@To IS NULL OR d.PeriodFrom <= @To)
      AND (@PostingStatus = N'' OR (@PostingStatus = N'Posted' AND ISNULL(s.payed, 0) = 1) OR (@PostingStatus = N'Unposted' AND ISNULL(s.payed, 0) = 0))
),
PostedRows AS
(
    SELECT p.ProviderId, p.EmployeeDeductionAccountCode,
           SUM(ISNULL(v.credit_value, 0)) AS PostedCredit,
           SUM(ISNULL(v.depet_value, 0)) AS PostedDebit
    FROM (SELECT DISTINCT ProviderId, EmployeeDeductionAccountCode, [Year], [Month] FROM PayrollRows) p
    INNER JOIN dbo.Notes n WITH (NOLOCK) ON n.salary = CONVERT(NVARCHAR(10), p.[Year]) + CONVERT(NVARCHAR(10), p.[Month])
    INNER JOIN dbo.DOUBLE_ENTREY_VOUCHERS v WITH (NOLOCK) ON v.Notes_ID = n.NoteID AND v.account_code = p.EmployeeDeductionAccountCode
    GROUP BY p.ProviderId, p.EmployeeDeductionAccountCode
)
SELECT p.ProviderId, p.ProviderNameAr, p.EmployeeDeductionAccountCode,
       a.Account_Serial, a.Account_Name,
       SUM(p.EmployeeDeduction) AS EmployeeDeduction,
       SUM(p.CompanyCost) AS CompanyCost,
       SUM(p.EmployeeDeduction + p.CompanyCost) AS PayrollPayable,
       ISNULL(MAX(r.PostedCredit), 0) AS PostedCredit,
       ISNULL(MAX(r.PostedDebit), 0) AS PostedDebit
FROM PayrollRows p
LEFT JOIN PostedRows r ON ISNULL(r.ProviderId, -1) = ISNULL(p.ProviderId, -1)
    AND ISNULL(r.EmployeeDeductionAccountCode, N'') = ISNULL(p.EmployeeDeductionAccountCode, N'')
LEFT JOIN dbo.ACCOUNTS a WITH (NOLOCK) ON a.Account_Code = p.EmployeeDeductionAccountCode
GROUP BY p.ProviderId, p.ProviderNameAr, p.EmployeeDeductionAccountCode, a.Account_Serial, a.Account_Name
ORDER BY p.ProviderNameAr;";
                AddReportParameters(command, filter);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var payrollPayable = ReadDecimal(reader, "PayrollPayable");
                        var postedCredit = ReadDecimal(reader, "PostedCredit");
                        var postedDebit = ReadDecimal(reader, "PostedDebit");
                        var accountSerial = ReadString(reader, "Account_Serial");
                        var accountName = ReadString(reader, "Account_Name");
                        rows.Add(new MedicalInsurancePayableSummaryRow
                        {
                            ProviderId = ReadNullableInt(reader, "ProviderId"),
                            ProviderName = ReadString(reader, "ProviderNameAr"),
                            AccountCode = ReadString(reader, "EmployeeDeductionAccountCode"),
                            AccountSerial = accountSerial,
                            AccountName = accountName,
                            AccountDisplay = BuildAccountDisplay(accountSerial, accountName),
                            EmployeeDeduction = ReadDecimal(reader, "EmployeeDeduction"),
                            CompanyCost = ReadDecimal(reader, "CompanyCost"),
                            PayrollPayable = payrollPayable,
                            PostedCredit = postedCredit,
                            PostedDebit = postedDebit,
                            PostedNetCredit = postedCredit - postedDebit,
                            Difference = payrollPayable - (postedCredit - postedDebit)
                        });
                    }
                }
            }

            return rows;
        }

        private IList<MedicalInsurancePayrollIntegrationRow> GetMedicalInsurancePayrollIntegration(MedicalInsuranceReportFilter filter)
        {
            var rows = new List<MedicalInsurancePayrollIntegrationRow>();
            using (var connection = OpenConnection())
            using (var command = connection.CreateCommand())
            {
                if (!TableExists(connection, "PayrollMedicalInsuranceDeduction"))
                {
                    return rows;
                }

                command.CommandText = @"
SELECT e.Emp_ID, e.Emp_Code, e.Emp_Name, b.branch_name, dep.DepartmentName,
       pr.ProviderNameAr, pl.PlanNameAr, d.[Year], d.[Month],
       d.EmployeeDeduction, d.CompanyCost,
       ISNULL(s.EmpTotalNet, 0) AS PayrollNetSalary,
       ISNULL(s.payed, 0) AS IsPosted,
       n.NoteID,
       ISNULL(SUM(v.depet_value), 0) AS JournalDebit,
       ISNULL(SUM(v.credit_value), 0) AS JournalCredit
FROM dbo.PayrollMedicalInsuranceDeduction d WITH (NOLOCK)
INNER JOIN dbo.TblEmployee e WITH (NOLOCK) ON e.Emp_ID = d.EmpId
LEFT JOIN dbo.TblBranchesData b WITH (NOLOCK) ON b.branch_id = e.BranchId
LEFT JOIN dbo.TblEmpDepartments dep WITH (NOLOCK) ON dep.DeparmentID = e.DepartmentID
LEFT JOIN dbo.EmployeeMedicalInsurance mi WITH (NOLOCK) ON mi.Id = d.EmployeeInsuranceId
LEFT JOIN dbo.MedicalInsurancePlans pl WITH (NOLOCK) ON pl.PlanId = mi.PlanId
LEFT JOIN dbo.MedicalInsuranceProviders pr WITH (NOLOCK) ON pr.ProviderId = pl.ProviderId
LEFT JOIN dbo.emp_salary s WITH (NOLOCK) ON s.emp_id = d.EmpId AND s.sgn = CONVERT(NVARCHAR(10), d.[Year]) + CONVERT(NVARCHAR(10), d.[Month])
LEFT JOIN dbo.Notes n WITH (NOLOCK) ON n.salary = CONVERT(NVARCHAR(10), d.[Year]) + CONVERT(NVARCHAR(10), d.[Month])
LEFT JOIN dbo.DOUBLE_ENTREY_VOUCHERS v WITH (NOLOCK) ON v.Notes_ID = n.NoteID
WHERE (@PlanId IS NULL OR mi.PlanId = @PlanId)
  AND (@ProviderId IS NULL OR pl.ProviderId = @ProviderId)
  AND (@BranchId IS NULL OR e.BranchId = @BranchId)
  AND (@DepartmentId IS NULL OR e.DepartmentID = @DepartmentId)
  AND (@EmployeeId IS NULL OR e.Emp_ID = @EmployeeId)
  AND (@From IS NULL OR d.PeriodTo >= @From)
  AND (@To IS NULL OR d.PeriodFrom <= @To)
  AND (@PostingStatus = N'' OR (@PostingStatus = N'Posted' AND ISNULL(s.payed, 0) = 1) OR (@PostingStatus = N'Unposted' AND ISNULL(s.payed, 0) = 0))
GROUP BY e.Emp_ID, e.Emp_Code, e.Emp_Name, b.branch_name, dep.DepartmentName,
         pr.ProviderNameAr, pl.PlanNameAr, d.[Year], d.[Month], d.EmployeeDeduction, d.CompanyCost,
         s.EmpTotalNet, s.payed, n.NoteID
ORDER BY d.[Year] DESC, d.[Month] DESC, e.Emp_Name;";
                AddReportParameters(command, filter);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var debit = ReadDecimal(reader, "JournalDebit");
                        var credit = ReadDecimal(reader, "JournalCredit");
                        var posted = ReadBool(reader, "IsPosted");
                        rows.Add(new MedicalInsurancePayrollIntegrationRow
                        {
                            EmployeeId = ReadInt(reader, "Emp_ID"),
                            EmployeeCode = ReadString(reader, "Emp_Code"),
                            EmployeeName = ReadString(reader, "Emp_Name"),
                            BranchName = ReadString(reader, "branch_name"),
                            DepartmentName = ReadString(reader, "DepartmentName"),
                            ProviderName = ReadString(reader, "ProviderNameAr"),
                            PlanName = ReadString(reader, "PlanNameAr"),
                            Year = ReadInt(reader, "Year"),
                            Month = ReadInt(reader, "Month"),
                            EmployeeDeduction = ReadDecimal(reader, "EmployeeDeduction"),
                            CompanyCost = ReadDecimal(reader, "CompanyCost"),
                            PayrollNetSalary = ReadDecimal(reader, "PayrollNetSalary"),
                            IsPosted = posted,
                            PostingStatus = posted ? "مرحل" : "غير مرحل",
                            NoteId = ReadNullableInt(reader, "NoteID"),
                            JournalDebit = debit,
                            JournalCredit = credit,
                            JournalBalance = debit - credit
                        });
                    }
                }
            }

            return rows;
        }

        public MedicalInsuranceOperationalDashboard GetMedicalInsuranceOperationalDashboard(MedicalInsuranceOperationalFilter filter)
        {
            filter = filter ?? new MedicalInsuranceOperationalFilter();
            if (filter.RenewalDays <= 0)
            {
                filter.RenewalDays = 45;
            }

            var result = new MedicalInsuranceOperationalDashboard();
            using (var connection = OpenConnection())
            {
                var hasSchema = TableExists(connection, "EmployeeMedicalInsurance")
                    && TableExists(connection, "MedicalInsurancePlans")
                    && TableExists(connection, "MedicalInsuranceProviders");
                result.SchemaReady = hasSchema;
                if (!hasSchema)
                {
                    result.Message = "Medical insurance setup is not installed in this database yet. Run the POS medical insurance SQL installer before using this screen.";
                    result.AccountingPreview = BuildInsuranceAccountingPreview(0, 0);
                    return result;
                }

                result.TotalEmployees = CountActiveEmployees(connection, filter);
                var rows = new List<MedicalInsuranceOperationalEmployee>();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"
SELECT TOP (200)
       e.Emp_ID, e.Emp_Code, e.Emp_Name,
       b.branch_name,
       d.DepartmentName,
       pr.ProviderNameAr,
       pl.PlanNameAr,
       mi.Id AS EmployeeInsuranceId,
       mi.StartDate,
       mi.EndDate,
       mi.IsActive,
       mi.IsMonthly,
       mi.MonthlyCost,
       mi.EmployeeMonthlyDeduction,
       mi.CompanyMonthlyCost,
       e.EmployeePhotoDataUrl
FROM dbo.EmployeeMedicalInsurance mi WITH (NOLOCK)
INNER JOIN dbo.TblEmployee e WITH (NOLOCK) ON e.Emp_ID = mi.EmpId
LEFT JOIN dbo.TblBranchesData b WITH (NOLOCK) ON b.branch_id = e.BranchId
LEFT JOIN dbo.TblEmpDepartments d WITH (NOLOCK) ON d.DeparmentID = e.DepartmentID
LEFT JOIN dbo.MedicalInsurancePlans pl WITH (NOLOCK) ON pl.PlanId = mi.PlanId
LEFT JOIN dbo.MedicalInsuranceProviders pr WITH (NOLOCK) ON pr.ProviderId = pl.ProviderId
WHERE (@Term IS NULL OR e.Emp_Code LIKE @LikeTerm OR e.Emp_Name LIKE @LikeTerm OR pr.ProviderNameAr LIKE @LikeTerm OR pl.PlanNameAr LIKE @LikeTerm)
  AND (@BranchId IS NULL OR e.BranchId = @BranchId)
  AND (@DepartmentId IS NULL OR e.DepartmentID = @DepartmentId)
ORDER BY mi.IsActive DESC, mi.EndDate, e.Emp_Name;";
                    AddNullable(command, "@Term", SqlDbType.NVarChar, string.IsNullOrWhiteSpace(filter.Term) ? null : filter.Term);
                    command.Parameters.Add("@LikeTerm", SqlDbType.NVarChar, 300).Value = string.IsNullOrWhiteSpace(filter.Term) ? (object)DBNull.Value : "%" + filter.Term.Trim() + "%";
                    AddNullable(command, "@BranchId", SqlDbType.Int, filter.BranchId);
                    AddNullable(command, "@DepartmentId", SqlDbType.Int, filter.DepartmentId);
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var row = new MedicalInsuranceOperationalEmployee
                            {
                                EmployeeId = ReadInt(reader, "Emp_ID"),
                                EmployeeCode = ReadString(reader, "Emp_Code"),
                                EmployeeName = ReadString(reader, "Emp_Name"),
                                BranchName = ReadString(reader, "branch_name"),
                                DepartmentName = ReadString(reader, "DepartmentName"),
                                ProviderName = ReadString(reader, "ProviderNameAr"),
                                PlanName = ReadString(reader, "PlanNameAr"),
                                MembershipNumber = "MED-" + DateTime.Today.Year + "-" + ReadInt(reader, "Emp_ID").ToString("000000"),
                                AvatarText = BuildAvatarText(ReadString(reader, "Emp_Name")),
                                PhotoDataUrl = ReadString(reader, "EmployeePhotoDataUrl"),
                                StartDate = ReadNullableDate(reader, "StartDate"),
                                EndDate = ReadNullableDate(reader, "EndDate"),
                                MonthlyCost = ReadDecimal(reader, "MonthlyCost"),
                                EmployeeMonthlyDeduction = ReadDecimal(reader, "EmployeeMonthlyDeduction"),
                                CompanyMonthlyCost = ReadDecimal(reader, "CompanyMonthlyCost"),
                                PayrollLinked = ReadBool(reader, "IsMonthly")
                            };
                            var isActive = ReadBool(reader, "IsActive");
                            row.NeedsRenewal = row.EndDate.HasValue && row.EndDate.Value.Date >= DateTime.Today && row.EndDate.Value.Date <= DateTime.Today.AddDays(filter.RenewalDays);
                            row.RenewalDate = row.EndDate;
                            row.Status = !isActive ? "Suspended" : (row.EndDate.HasValue && row.EndDate.Value.Date < DateTime.Today ? "Expired" : (row.NeedsRenewal ? "Renewal Due" : "Active"));
                            rows.Add(row);
                        }
                    }
                }

                AttachOperationalInsuranceExtras(connection, rows);
                if (!string.IsNullOrWhiteSpace(filter.Status))
                {
                    rows = rows.Where(x => string.Equals(x.Status, filter.Status, StringComparison.OrdinalIgnoreCase)).ToList();
                }

                result.Employees = rows;
                result.UninsuredEmployees = Math.Max(0, result.TotalEmployees - rows.Select(x => x.EmployeeId).Distinct().Count());
                result.ActiveInsured = rows.Count(x => x.Status == "Active" || x.Status == "Renewal Due");
                result.Suspended = rows.Count(x => x.Status == "Suspended");
                result.Expired = rows.Count(x => x.Status == "Expired");
                result.UpcomingRenewals = rows.Count(x => x.NeedsRenewal);
                result.OverdueInstallments = rows.Sum(x => x.OverdueInstallments);
                result.MonthlyEmployeeShare = rows.Sum(x => x.EmployeeMonthlyDeduction);
                result.MonthlyCompanyShare = rows.Sum(x => x.CompanyMonthlyCost);
                result.MonthlyPayable = result.MonthlyEmployeeShare + result.MonthlyCompanyShare;
                result.AccountingPreview = BuildInsuranceAccountingPreview(result.MonthlyEmployeeShare, result.MonthlyCompanyShare);
                result.BranchCosts = BuildDimensionSummary(rows, x => string.IsNullOrWhiteSpace(x.BranchName) ? "بدون فرع" : x.BranchName).Take(8).ToList();
                result.DepartmentCosts = BuildDimensionSummary(rows, x => string.IsNullOrWhiteSpace(x.DepartmentName) ? "بدون قسم" : x.DepartmentName).Take(8).ToList();
                result.Alerts = BuildInsuranceAlerts(rows);
                result.Message = rows.Count == 0 ? "No matching insurance subscriptions were found." : "Operational medical insurance dashboard loaded.";
            }

            return result;
        }

        private int CountActiveEmployees(SqlConnection connection, MedicalInsuranceOperationalFilter filter)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
SELECT COUNT(1)
FROM dbo.TblEmployee e WITH (NOLOCK)
WHERE ISNULL(e.chkStop, 0) = 0
  AND (@BranchId IS NULL OR e.BranchId = @BranchId)
  AND (@DepartmentId IS NULL OR e.DepartmentID = @DepartmentId);";
                AddNullable(command, "@BranchId", SqlDbType.Int, filter == null ? null : filter.BranchId);
                AddNullable(command, "@DepartmentId", SqlDbType.Int, filter == null ? null : filter.DepartmentId);
                return Convert.ToInt32(command.ExecuteScalar() ?? 0);
            }
        }

        private void AttachOperationalInsuranceExtras(SqlConnection connection, IList<MedicalInsuranceOperationalEmployee> rows)
        {
            if (rows == null || rows.Count == 0)
            {
                return;
            }

            if (TableExists(connection, "MedicalInsuranceDependents"))
            {
                var counts = new Dictionary<int, int>();
                var dependents = new Dictionary<int, List<MedicalInsuranceDependentSummary>>();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"
SELECT EmpId, DependentName, Relation, BirthDate, CoveragePercent, IsActive
FROM dbo.MedicalInsuranceDependents WITH (NOLOCK)
WHERE IsActive = 1
ORDER BY EmpId, Relation, DependentName;";
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var empId = ReadInt(reader, "EmpId");
                            if (!dependents.ContainsKey(empId))
                            {
                                dependents[empId] = new List<MedicalInsuranceDependentSummary>();
                            }

                            var birthDate = ReadNullableDate(reader, "BirthDate");
                            dependents[empId].Add(new MedicalInsuranceDependentSummary
                            {
                                Name = ReadString(reader, "DependentName"),
                                Relation = ReadString(reader, "Relation"),
                                Age = CalculateAge(birthDate),
                                CoveragePercent = ReadDecimal(reader, "CoveragePercent"),
                                IsActive = ReadBool(reader, "IsActive")
                            });
                        }
                    }
                }

                foreach (var item in dependents)
                {
                    counts[item.Key] = item.Value.Count;
                }

                foreach (var row in rows)
                {
                    int count;
                    if (counts.TryGetValue(row.EmployeeId, out count))
                    {
                        row.DependentsCount = count;
                    }

                    List<MedicalInsuranceDependentSummary> employeeDependents;
                    if (dependents.TryGetValue(row.EmployeeId, out employeeDependents))
                    {
                        row.Dependents = employeeDependents.Take(4).ToList();
                    }
                }
            }

            if (TableExists(connection, "MedicalInsuranceInstallments"))
            {
                var overdue = new Dictionary<int, Tuple<int, decimal>>();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = @"
SELECT EmpId, COUNT(1) AS Cnt, SUM(Amount) AS Amount
FROM dbo.MedicalInsuranceInstallments WITH (NOLOCK)
WHERE IsPaid = 0 AND DueDate < CAST(GETDATE() AS DATE)
GROUP BY EmpId;";
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            overdue[ReadInt(reader, "EmpId")] = Tuple.Create(ReadInt(reader, "Cnt"), ReadDecimal(reader, "Amount"));
                        }
                    }
                }

                foreach (var row in rows)
                {
                    Tuple<int, decimal> item;
                    if (overdue.TryGetValue(row.EmployeeId, out item))
                    {
                        row.OverdueInstallments = item.Item1;
                        row.OverdueAmount = item.Item2;
                    }
                }
            }
        }

        private static string BuildAvatarText(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return "MI";
            }

            var parts = name.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 1)
            {
                return parts[0].Substring(0, Math.Min(2, parts[0].Length));
            }

            return (parts[0].Substring(0, 1) + parts[1].Substring(0, 1)).ToUpperInvariant();
        }

        private static int CalculateAge(DateTime? birthDate)
        {
            if (!birthDate.HasValue)
            {
                return 0;
            }

            var today = DateTime.Today;
            var age = today.Year - birthDate.Value.Year;
            if (birthDate.Value.Date > today.AddYears(-age))
            {
                age--;
            }

            return Math.Max(0, age);
        }

        private static IList<MedicalInsuranceDimensionSummary> BuildDimensionSummary(IEnumerable<MedicalInsuranceOperationalEmployee> rows, Func<MedicalInsuranceOperationalEmployee, string> selector)
        {
            return rows
                .GroupBy(selector)
                .Select(g => new MedicalInsuranceDimensionSummary
                {
                    Name = g.Key,
                    Employees = g.Select(x => x.EmployeeId).Distinct().Count(),
                    EmployeeShare = g.Sum(x => x.EmployeeMonthlyDeduction),
                    CompanyShare = g.Sum(x => x.CompanyMonthlyCost),
                    TotalCost = g.Sum(x => x.EmployeeMonthlyDeduction + x.CompanyMonthlyCost)
                })
                .OrderByDescending(x => x.TotalCost)
                .ToList();
        }

        private static IList<MedicalInsuranceAlert> BuildInsuranceAlerts(IEnumerable<MedicalInsuranceOperationalEmployee> rows)
        {
            var alerts = new List<MedicalInsuranceAlert>();
            foreach (var row in rows)
            {
                if (row.NeedsRenewal)
                {
                    alerts.Add(new MedicalInsuranceAlert
                    {
                        AlertType = "Renewal",
                        Severity = "Warning",
                        Title = "Renewal due",
                        Description = "Policy is approaching renewal. Confirm HR approval before payroll.",
                        EmployeeName = row.EmployeeName,
                        BranchName = row.BranchName,
                        DueDate = row.RenewalDate
                    });
                }

                if (row.Status == "Expired")
                {
                    alerts.Add(new MedicalInsuranceAlert
                    {
                        AlertType = "Expired",
                        Severity = "Danger",
                        Title = "Coverage expired",
                        Description = "Coverage is expired and needs immediate HR review.",
                        EmployeeName = row.EmployeeName,
                        BranchName = row.BranchName,
                        DueDate = row.EndDate
                    });
                }

                if (row.OverdueInstallments > 0)
                {
                    alerts.Add(new MedicalInsuranceAlert
                    {
                        AlertType = "Overdue",
                        Severity = "Danger",
                        Title = "Overdue installment",
                        Description = row.OverdueInstallments + " unpaid installment(s), amount " + row.OverdueAmount.ToString("0.00"),
                        EmployeeName = row.EmployeeName,
                        BranchName = row.BranchName,
                        DueDate = DateTime.Today
                    });
                }

                if (!row.PayrollLinked)
                {
                    alerts.Add(new MedicalInsuranceAlert
                    {
                        AlertType = "PayrollLink",
                        Severity = "Warning",
                        Title = "Missing payroll linkage",
                        Description = "Insurance exists but is not marked as monthly payroll deduction.",
                        EmployeeName = row.EmployeeName,
                        BranchName = row.BranchName,
                        DueDate = null
                    });
                }
            }

            return alerts
                .OrderByDescending(x => x.Severity == "Danger")
                .ThenBy(x => x.DueDate ?? DateTime.MaxValue)
                .Take(20)
                .ToList();
        }

        private static IList<MedicalInsuranceAccountingPreviewLine> BuildInsuranceAccountingPreview(decimal employeeShare, decimal companyShare)
        {
            var payable = employeeShare + companyShare;
            return new List<MedicalInsuranceAccountingPreviewLine>
            {
                new MedicalInsuranceAccountingPreviewLine
                {
                    Step = "خصم الموظف الشهري",
                    DebitAccount = "الأجور المستحقة للموظف",
                    CreditAccount = "مستحق شركة التأمين الطبي",
                    Amount = employeeShare,
                    Explanation = "يتم حجز نصيب الموظف من صافي الراتب وتحويله إلى حساب مستحق شركة التأمين."
                },
                new MedicalInsuranceAccountingPreviewLine
                {
                    Step = "مساهمة الشركة",
                    DebitAccount = "مصروف التأمين الطبي",
                    CreditAccount = "مستحق شركة التأمين الطبي",
                    Amount = companyShare,
                    Explanation = "يتم إثبات نصيب الشركة كمصروف مزايا موظفين مع إضافته إلى مستحق شركة التأمين."
                },
                new MedicalInsuranceAccountingPreviewLine
                {
                    Step = "سداد شركة التأمين",
                    DebitAccount = "مستحق شركة التأمين الطبي",
                    CreditAccount = "الخزينة أو البنك",
                    Amount = payable,
                    Explanation = "عند السداد يتم إقفال المستحق على حساب الخزينة أو البنك المختار."
                }
            };
        }

        private static void BuildJournalPreview(SalaryRunPreview preview)
        {
            foreach (var row in preview.Rows)
            {
                var additionComponents = row.Components == null
                    ? new List<PayrollCompatibilityComponent>()
                    : row.Components.Where(x => x.ViewComponent && !x.AddOrDiscount && !x.ZmamAccount && !x.AdvancePaymentAccount && ComponentReplayValue(row, x) > 0).ToList();
                var additionComponentTotal = additionComponents.Sum(x => ComponentReplayValue(row, x));
                foreach (var group in additionComponents.GroupBy(x => new { x.AccountCode, x.ComponentNameAr }))
                {
                    preview.JournalPreview.Add(new SalaryRunJournalLine
                    {
                        PayrollRunId = row.PayrollRunId,
                        AccountCode = group.Key.AccountCode,
                        Debit = group.Sum(x => ComponentReplayValue(row, x)),
                        Description = "مصروف راتب - " + FirstNonEmpty(group.Key.ComponentNameAr, row.EmployeeName),
                        BranchId = row.BranchId,
                        DepartmentId = row.DepartmentId,
                        EmployeeId = row.EmployeeId
                    });
                }

                if (row.TotalBeforeDeductions > additionComponentTotal)
                {
                    preview.JournalPreview.Add(new SalaryRunJournalLine
                    {
                        PayrollRunId = row.PayrollRunId,
                        AccountCode = row.AccruedSalaryAccountCode,
                        Debit = row.TotalBeforeDeductions - additionComponentTotal,
                        Description = "مصروف راتب غير مفصل " + row.EmployeeName,
                        BranchId = row.BranchId,
                        DepartmentId = row.DepartmentId,
                        EmployeeId = row.EmployeeId
                    });
                }

                if (row.TotalBeforeDeductions > 0)
                {
                    preview.JournalPreview.Add(new SalaryRunJournalLine
                    {
                        PayrollRunId = row.PayrollRunId,
                        AccountCode = row.AccruedSalaryAccountCode,
                        Credit = row.TotalBeforeDeductions,
                        Description = "الأجور المستحقة " + row.EmployeeName,
                        BranchId = row.BranchId,
                        DepartmentId = row.DepartmentId,
                        EmployeeId = row.EmployeeId
                    });
                }

                if (row.AdvanceDeduction > 0)
                {
                    preview.JournalPreview.Add(new SalaryRunJournalLine
                    {
                        PayrollRunId = row.PayrollRunId,
                        AccountCode = row.AccruedSalaryAccountCode,
                        Debit = row.AdvanceDeduction,
                        Description = "خصم سلف من الأجور المستحقة " + row.EmployeeName,
                        BranchId = row.BranchId,
                        DepartmentId = row.DepartmentId,
                        EmployeeId = row.EmployeeId
                    });
                    preview.JournalPreview.Add(new SalaryRunJournalLine
                    {
                        PayrollRunId = row.PayrollRunId,
                        AccountCode = row.EmployeeAccountCode,
                        Credit = row.AdvanceDeduction,
                        Description = "سداد سلف من مسير " + row.EmployeeName,
                        BranchId = row.BranchId,
                        DepartmentId = row.DepartmentId,
                        EmployeeId = row.EmployeeId
                    });
                }

                if (row.MedicalInsuranceDeduction > 0)
                {
                    preview.JournalPreview.Add(new SalaryRunJournalLine
                    {
                        PayrollRunId = row.PayrollRunId,
                        AccountCode = row.AccruedSalaryAccountCode,
                        Debit = row.MedicalInsuranceDeduction,
                        Description = "خصم التأمين الطبي من الأجور المستحقة " + row.EmployeeName,
                        BranchId = row.BranchId,
                        DepartmentId = row.DepartmentId,
                        EmployeeId = row.EmployeeId
                    });
                    preview.JournalPreview.Add(new SalaryRunJournalLine
                    {
                        PayrollRunId = row.PayrollRunId,
                        AccountCode = row.MedicalInsuranceEmployeeAccountCode,
                        Credit = row.MedicalInsuranceDeduction,
                        Description = "استحقاق شركة التأمين الطبي - نصيب الموظف " + row.EmployeeName,
                        BranchId = row.BranchId,
                        DepartmentId = row.DepartmentId,
                        EmployeeId = row.EmployeeId
                    });
                }

                if (row.MedicalInsuranceCompanyCost > 0)
                {
                    preview.JournalPreview.Add(new SalaryRunJournalLine
                    {
                        PayrollRunId = row.PayrollRunId,
                        AccountCode = row.MedicalInsuranceCompanyAccountCode,
                        Debit = row.MedicalInsuranceCompanyCost,
                        Description = "تكلفة التأمين الطبي - نصيب الشركة " + row.EmployeeName,
                        BranchId = row.BranchId,
                        DepartmentId = row.DepartmentId,
                        EmployeeId = row.EmployeeId
                    });
                    preview.JournalPreview.Add(new SalaryRunJournalLine
                    {
                        PayrollRunId = row.PayrollRunId,
                        AccountCode = row.MedicalInsuranceEmployeeAccountCode,
                        Credit = row.MedicalInsuranceCompanyCost,
                        Description = "استحقاق شركة التأمين الطبي - نصيب الشركة " + row.EmployeeName,
                        BranchId = row.BranchId,
                        DepartmentId = row.DepartmentId,
                        EmployeeId = row.EmployeeId
                    });
                }
            }
        }

        private void AttachJournalPreviewAccountInfo(SqlConnection connection, SalaryRunPreview preview)
        {
            if (preview == null || preview.JournalPreview == null || preview.JournalPreview.Count == 0)
            {
                return;
            }

            var accountCodes = preview.JournalPreview
                .Select(x => x.AccountCode)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (accountCodes.Count == 0)
            {
                return;
            }

            var names = new Dictionary<string, Tuple<string, string>>(StringComparer.OrdinalIgnoreCase);
            using (var command = connection.CreateCommand())
            {
                var parameterNames = new List<string>();
                for (var i = 0; i < accountCodes.Count; i++)
                {
                    var parameterName = "@Account" + i.ToString();
                    parameterNames.Add(parameterName);
                    command.Parameters.Add(parameterName, SqlDbType.NVarChar, 50).Value = accountCodes[i];
                }

                command.CommandText = @"
SELECT Account_Code, Account_Serial, Account_Name
FROM dbo.ACCOUNTS WITH (NOLOCK)
WHERE Account_Code IN (" + string.Join(",", parameterNames.ToArray()) + @");";

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        names[ReadString(reader, "Account_Code")] = Tuple.Create(ReadString(reader, "Account_Serial"), ReadString(reader, "Account_Name"));
                    }
                }
            }

            foreach (var line in preview.JournalPreview)
            {
                Tuple<string, string> account;
                if (!string.IsNullOrWhiteSpace(line.AccountCode) && names.TryGetValue(line.AccountCode, out account))
                {
                    line.AccountSerial = account.Item1;
                    line.AccountName = account.Item2;
                }
            }
        }

        private static MedicalInsuranceCalculation CalculateMedicalInsurance(decimal monthlyCost, string employeeShareType, decimal employeeShareValue, string companyShareType, decimal companyShareValue)
        {
            var result = new MedicalInsuranceCalculation();
            result.EmployeeDeduction = CalculateShare(monthlyCost, employeeShareType, employeeShareValue);
            if (string.Equals(companyShareType, "AutoBalance", StringComparison.OrdinalIgnoreCase) || string.IsNullOrWhiteSpace(companyShareType))
            {
                result.CompanyCost = monthlyCost - result.EmployeeDeduction;
            }
            else
            {
                result.CompanyCost = CalculateShare(monthlyCost, companyShareType, companyShareValue);
            }

            if (result.EmployeeDeduction < 0)
            {
                result.EmployeeDeduction = 0;
            }

            if (result.EmployeeDeduction > monthlyCost)
            {
                result.EmployeeDeduction = monthlyCost;
            }

            if (result.CompanyCost < 0)
            {
                result.CompanyCost = 0;
            }

            if (result.EmployeeDeduction + result.CompanyCost > monthlyCost)
            {
                result.CompanyCost = Math.Max(0, monthlyCost - result.EmployeeDeduction);
            }

            result.EmployeeDeduction = Math.Round(result.EmployeeDeduction, 2);
            result.CompanyCost = Math.Round(result.CompanyCost, 2);
            return result;
        }

        private static decimal CalculateShare(decimal monthlyCost, string shareType, decimal value)
        {
            if (string.Equals(shareType, "Percent", StringComparison.OrdinalIgnoreCase))
            {
                return Math.Round(monthlyCost * value / 100m, 2);
            }

            return Math.Round(value, 2);
        }

        private IList<EmployeeMedicalInsurance> GetMedicalInsuranceHistory(SqlConnection connection, int employeeId)
        {
            var rows = new List<EmployeeMedicalInsurance>();
            if (!TableExists(connection, "EmployeeMedicalInsurance"))
            {
                return rows;
            }

            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
SELECT mi.Id, mi.EmpId, mi.PlanId, pl.PlanNameAr, pr.ProviderNameAr,
       mi.PolicyNumber, mi.CardNumber, mi.CoveragePercent,
       mi.StartDate, mi.EndDate, mi.IsMonthly, mi.IsActive,
       mi.MonthlyCost, mi.EmployeeShareType, mi.EmployeeShareValue,
       mi.CompanyShareType, mi.CompanyShareValue,
       mi.EmployeeMonthlyDeduction, mi.CompanyMonthlyCost,
       mi.Notes, mi.CreatedAt, mi.UpdatedAt
FROM dbo.EmployeeMedicalInsurance mi WITH (NOLOCK)
LEFT JOIN dbo.MedicalInsurancePlans pl WITH (NOLOCK) ON pl.PlanId = mi.PlanId
LEFT JOIN dbo.MedicalInsuranceProviders pr WITH (NOLOCK) ON pr.ProviderId = pl.ProviderId
WHERE mi.EmpId = @EmpId
ORDER BY mi.IsActive DESC, mi.StartDate DESC, mi.Id DESC;";
                command.Parameters.Add("@EmpId", SqlDbType.Int).Value = employeeId;
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        rows.Add(ReadEmployeeMedicalInsurance(reader));
                    }
                }
            }

            return rows;
        }

        private void SaveMedicalInsurance(SqlConnection connection, SqlTransaction transaction, EmployeeMedicalInsurance insurance, int employeeId, int userId)
        {
            if (!TableExists(connection, transaction, "EmployeeMedicalInsurance"))
            {
                throw new InvalidOperationException("جداول اشتراكات التأمين الطبي للموظفين غير مثبتة في قاعدة البيانات الحالية.");
            }

            if (!insurance.StartDate.HasValue)
            {
                insurance.StartDate = DateTime.Today;
            }

            if (insurance.MonthlyCost == 0 && insurance.PlanId.HasValue)
            {
                var plan = GetPlan(connection, transaction, insurance.PlanId.Value);
                if (plan != null)
                {
                    ApplyPlanDefaults(insurance, plan);
                }
            }

            ValidateEmployeeMedicalInsurance(insurance);

            if (!insurance.Id.HasValue || insurance.Id.Value <= 0)
            {
                insurance.Id = FindMatchingEmployeeMedicalInsuranceId(connection, transaction, insurance, employeeId);
            }

            var calculation = CalculateMedicalInsurance(insurance.MonthlyCost, insurance.EmployeeShareType, insurance.EmployeeShareValue, insurance.CompanyShareType, insurance.CompanyShareValue);
            insurance.EmployeeMonthlyDeduction = calculation.EmployeeDeduction;
            insurance.CompanyMonthlyCost = calculation.CompanyCost;

            if (insurance.Id.HasValue && insurance.Id.Value > 0)
            {
                if (insurance.IsActive)
                {
                    EndOtherActiveMedicalInsurance(connection, transaction, employeeId, insurance.Id.Value, insurance.StartDate.Value, userId);
                }

                using (var command = CreateCommand(connection, transaction, @"
UPDATE dbo.EmployeeMedicalInsurance
SET PlanId = @PlanId,
    PolicyNumber = @PolicyNumber,
    CardNumber = @CardNumber,
    CoveragePercent = @CoveragePercent,
    StartDate = @StartDate,
    EndDate = @EndDate,
    IsMonthly = @IsMonthly,
    IsActive = @IsActive,
    MonthlyCost = @MonthlyCost,
    EmployeeShareType = @EmployeeShareType,
    EmployeeShareValue = @EmployeeShareValue,
    CompanyShareType = @CompanyShareType,
    CompanyShareValue = @CompanyShareValue,
    EmployeeMonthlyDeduction = @EmployeeMonthlyDeduction,
    CompanyMonthlyCost = @CompanyMonthlyCost,
    Amount = @EmployeeShareValue,
    PercentValue = CASE WHEN @EmployeeShareType = N'Percent' THEN @EmployeeShareValue ELSE 0 END,
    DeductionType = @EmployeeShareType,
    Notes = @Notes,
    UpdatedAt = GETDATE(),
    UpdatedBy = @UserId
WHERE Id = @Id AND EmpId = @EmpId;"))
                {
                    AddInsuranceParameters(command, insurance, employeeId, userId);
                    command.Parameters.Add("@Id", SqlDbType.Int).Value = insurance.Id.Value;
                    if (command.ExecuteNonQuery() == 0)
                    {
                        throw new InvalidOperationException("اشتراك التأمين الطبي المحدد غير موجود لهذا الموظف.");
                    }
                }
            }
            else if (insurance.IsActive || insurance.PlanId.HasValue || insurance.MonthlyCost > 0)
            {
                if (insurance.IsActive)
                {
                    EndOtherActiveMedicalInsurance(connection, transaction, employeeId, null, insurance.StartDate.Value, userId);
                }

                using (var command = CreateCommand(connection, transaction, @"
INSERT INTO dbo.EmployeeMedicalInsurance
(EmpId, PlanId, PolicyNumber, CardNumber, CoveragePercent, StartDate, EndDate, IsMonthly, IsActive,
 MonthlyCost, EmployeeShareType, EmployeeShareValue,
 CompanyShareType, CompanyShareValue, EmployeeMonthlyDeduction, CompanyMonthlyCost,
 Amount, PercentValue, DeductionType, Notes, CreatedBy, CreatedAt)
VALUES
(@EmpId, @PlanId, @PolicyNumber, @CardNumber, @CoveragePercent, @StartDate, @EndDate, @IsMonthly, @IsActive,
 @MonthlyCost, @EmployeeShareType, @EmployeeShareValue,
 @CompanyShareType, @CompanyShareValue, @EmployeeMonthlyDeduction, @CompanyMonthlyCost,
 @EmployeeShareValue, CASE WHEN @EmployeeShareType = N'Percent' THEN @EmployeeShareValue ELSE 0 END, @EmployeeShareType, @Notes, @UserId, GETDATE());"))
                {
                    AddInsuranceParameters(command, insurance, employeeId, userId);
                    command.ExecuteNonQuery();
                }
            }
        }

        private void EndOtherActiveMedicalInsurance(SqlConnection connection, SqlTransaction transaction, int employeeId, int? keepInsuranceId, DateTime newStartDate, int userId)
        {
            using (var command = CreateCommand(connection, transaction, @"
UPDATE dbo.EmployeeMedicalInsurance
SET IsActive = 0,
    EndDate = CASE
        WHEN StartDate IS NOT NULL AND CONVERT(date, StartDate) >= CONVERT(date, @StartDate)
            THEN StartDate
        WHEN EndDate IS NULL OR CONVERT(date, EndDate) >= CONVERT(date, @StartDate)
            THEN DATEADD(day, -1, @StartDate)
        ELSE EndDate
    END,
    UpdatedAt = GETDATE(),
    UpdatedBy = @UserId
WHERE EmpId = @EmpId
  AND IsActive = 1
  AND (@KeepId IS NULL OR Id <> @KeepId);"))
            {
                command.Parameters.Add("@EmpId", SqlDbType.Int).Value = employeeId;
                command.Parameters.Add("@StartDate", SqlDbType.DateTime).Value = newStartDate;
                AddNullable(command, "@KeepId", SqlDbType.Int, keepInsuranceId);
                command.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
                command.ExecuteNonQuery();
            }
        }

        private int? FindMatchingEmployeeMedicalInsuranceId(SqlConnection connection, SqlTransaction transaction, EmployeeMedicalInsurance insurance, int employeeId)
        {
            using (var command = CreateCommand(connection, transaction, @"
SELECT TOP (1) Id
FROM dbo.EmployeeMedicalInsurance WITH (UPDLOCK, HOLDLOCK)
WHERE EmpId = @EmpId
  AND ISNULL(PlanId, 0) = ISNULL(@PlanId, 0)
  AND CONVERT(date, StartDate) = CONVERT(date, @StartDate)
ORDER BY IsActive DESC, Id DESC;"))
            {
                command.Parameters.Add("@EmpId", SqlDbType.Int).Value = employeeId;
                AddNullable(command, "@PlanId", SqlDbType.Int, insurance.PlanId);
                command.Parameters.Add("@StartDate", SqlDbType.DateTime).Value = insurance.StartDate.Value;
                var value = command.ExecuteScalar();
                return value == null || value == DBNull.Value ? (int?)null : Convert.ToInt32(value);
            }
        }

        private static void SaveMedicalDeductionAudit(SqlConnection connection, SqlTransaction transaction, SalaryRunEmployeeRow row, SalaryRunRequest request, DateTime periodStart, DateTime periodEnd, int userId)
        {
            if (!TableExists(connection, transaction, "PayrollMedicalInsuranceDeduction") || !TableExists(connection, transaction, "SalaryRunMedicalInsuranceDeduction"))
            {
                return;
            }

            using (var command = CreateCommand(connection, transaction, @"
IF @EmployeeDeduction > 0
BEGIN
    IF EXISTS (SELECT 1 FROM dbo.PayrollMedicalInsuranceDeduction WHERE EmpId = @EmpId AND [Year] = @Year AND [Month] = @Month)
        UPDATE dbo.PayrollMedicalInsuranceDeduction
        SET SalaryRunId = @SalaryRunId,
            EmployeeInsuranceId = @EmployeeInsuranceId,
            PeriodFrom = @PeriodFrom,
            PeriodTo = @PeriodTo,
            MonthlyCost = @MonthlyCost,
            EmployeeDeduction = @EmployeeDeduction,
            CompanyCost = @CompanyCost,
            CreatedAt = GETDATE(),
            CreatedBy = @UserId
        WHERE EmpId = @EmpId AND [Year] = @Year AND [Month] = @Month;
    ELSE
        INSERT INTO dbo.PayrollMedicalInsuranceDeduction
        (SalaryRunId, EmpId, EmployeeInsuranceId, [Year], [Month], PeriodFrom, PeriodTo, MonthlyCost, EmployeeDeduction, CompanyCost, CreatedAt, CreatedBy)
        VALUES
        (@SalaryRunId, @EmpId, @EmployeeInsuranceId, @Year, @Month, @PeriodFrom, @PeriodTo, @MonthlyCost, @EmployeeDeduction, @CompanyCost, GETDATE(), @UserId);

    IF EXISTS (SELECT 1 FROM dbo.SalaryRunMedicalInsuranceDeduction WHERE EmpId = @EmpId AND [Year] = @Year AND [Month] = @Month)
        UPDATE dbo.SalaryRunMedicalInsuranceDeduction
        SET Amount = @EmployeeDeduction, CompanyCost = @CompanyCost, MonthlyCost = @MonthlyCost, EmployeeInsuranceId = @EmployeeInsuranceId, CreatedBy = @UserId, CreatedAt = GETDATE()
        WHERE EmpId = @EmpId AND [Year] = @Year AND [Month] = @Month;
    ELSE
        INSERT INTO dbo.SalaryRunMedicalInsuranceDeduction (EmpId, [Year], [Month], Amount, CompanyCost, MonthlyCost, EmployeeInsuranceId, CreatedBy, CreatedAt)
        VALUES (@EmpId, @Year, @Month, @EmployeeDeduction, @CompanyCost, @MonthlyCost, @EmployeeInsuranceId, @UserId, GETDATE());
END
ELSE
BEGIN
    DELETE FROM dbo.PayrollMedicalInsuranceDeduction WHERE EmpId = @EmpId AND [Year] = @Year AND [Month] = @Month;
    DELETE FROM dbo.SalaryRunMedicalInsuranceDeduction WHERE EmpId = @EmpId AND [Year] = @Year AND [Month] = @Month;
END"))
            {
                command.Parameters.Add("@SalaryRunId", SqlDbType.Int).Value = (object)row.ExistingSalaryRowId ?? DBNull.Value;
                command.Parameters.Add("@EmployeeInsuranceId", SqlDbType.Int).Value = (object)row.MedicalInsuranceId ?? DBNull.Value;
                command.Parameters.Add("@EmpId", SqlDbType.Int).Value = row.EmployeeId;
                command.Parameters.Add("@Year", SqlDbType.Int).Value = request.Year;
                command.Parameters.Add("@Month", SqlDbType.Int).Value = request.Month;
                command.Parameters.Add("@PeriodFrom", SqlDbType.DateTime).Value = periodStart;
                command.Parameters.Add("@PeriodTo", SqlDbType.DateTime).Value = periodEnd;
                command.Parameters.Add("@MonthlyCost", SqlDbType.Money).Value = row.MedicalInsuranceMonthlyCost;
                command.Parameters.Add("@EmployeeDeduction", SqlDbType.Money).Value = row.MedicalInsuranceDeduction;
                command.Parameters.Add("@CompanyCost", SqlDbType.Money).Value = row.MedicalInsuranceCompanyCost;
                command.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
                command.ExecuteNonQuery();
            }
        }

        private MedicalInsurancePlan GetPlan(SqlConnection connection, SqlTransaction transaction, int planId)
        {
            using (var command = CreateCommand(connection, transaction, @"
SELECT TOP (1) p.PlanId, p.ProviderId, pr.ProviderNameAr, p.PlanNameAr, p.PlanNameEn,
       p.DefaultMonthlyCost, p.DefaultEmployeeShareType, p.DefaultEmployeeShareValue,
       p.DefaultCompanyShareType, p.DefaultCompanyShareValue,
       p.EmployeeDeductionAccountCode, p.CompanyCostAccountCode,
       p.LifecycleStatus, p.StartDate, p.EndDate, p.PayrollStartDate, p.SuspensionDate, p.CancellationDate,
       p.CostCenterCode, p.PayrollDeductionType, p.IsMonthlyDeduction, p.AutoStopAtEndDate, p.ShowInPayroll,
       p.DistributeByDepartment, p.DistributeByCostCenter, p.TaxMode, p.MaxDependents, p.ChildrenMaxAge,
       p.SpouseAdditionalCost, p.ChildAdditionalCost, p.ParentAdditionalCost, p.DefaultCoveragePercent,
       p.AutoEnrollAfterDays, p.AutoEnrollCriteria, p.RulesJson, p.DependentsTemplateJson,
       p.IsActive, p.Notes
FROM dbo.MedicalInsurancePlans p
INNER JOIN dbo.MedicalInsuranceProviders pr ON pr.ProviderId = p.ProviderId
WHERE p.PlanId = @PlanId;"))
            {
                command.Parameters.Add("@PlanId", SqlDbType.Int).Value = planId;
                using (var reader = command.ExecuteReader())
                {
                    return reader.Read() ? ReadPlan(reader) : null;
                }
            }
        }

        private static void ApplyPlanDefaults(EmployeeMedicalInsurance insurance, MedicalInsurancePlan plan)
        {
            insurance.MonthlyCost = plan.DefaultMonthlyCost;
            insurance.EmployeeShareType = plan.DefaultEmployeeShareType;
            insurance.EmployeeShareValue = plan.DefaultEmployeeShareValue;
            insurance.CompanyShareType = plan.DefaultCompanyShareType;
            insurance.CompanyShareValue = plan.DefaultCompanyShareValue;
        }

        private static void ValidateShare(string label, decimal monthlyCost, string shareType, decimal shareValue)
        {
            if (monthlyCost < 0 || shareValue < 0)
            {
                throw new InvalidOperationException("قيم نصيب " + label + " في التأمين الطبي لا تقبل أرقاما سالبة.");
            }

            if (string.Equals(shareType, "Percent", StringComparison.OrdinalIgnoreCase) && shareValue > 100)
            {
                throw new InvalidOperationException("نسبة نصيب " + label + " في التأمين الطبي لا يمكن أن تتجاوز 100%.");
            }
        }

        private static void ValidatePlanLifecycle(MedicalInsurancePlan plan)
        {
            if (plan.StartDate.HasValue && plan.EndDate.HasValue && plan.EndDate.Value.Date < plan.StartDate.Value.Date)
            {
                throw new InvalidOperationException("تاريخ نهاية خطة التأمين لا يمكن أن يكون قبل تاريخ البداية.");
            }

            if (plan.MaxDependents < 0 || plan.ChildrenMaxAge < 0)
            {
                throw new InvalidOperationException("قواعد التابعين لا تقبل قيما سالبة.");
            }

            if (plan.DefaultCoveragePercent < 0 || plan.DefaultCoveragePercent > 100)
            {
                throw new InvalidOperationException("نسبة التغطية يجب أن تكون بين 0 و 100.");
            }

            if (plan.IsActive && plan.DefaultMonthlyCost <= 0)
            {
                throw new InvalidOperationException("لا يمكن تفعيل خطة تأمين طبي بدون تكلفة شهرية صحيحة.");
            }

            if (plan.IsActive && plan.ShowInPayroll)
            {
                var calculation = CalculateMedicalInsurance(
                    plan.DefaultMonthlyCost,
                    plan.DefaultEmployeeShareType,
                    plan.DefaultEmployeeShareValue,
                    plan.DefaultCompanyShareType,
                    plan.DefaultCompanyShareValue);

                // Missing medical-insurance accounts are completed inside the save transaction.
                // Invalid non-empty accounts are still rejected by ValidateMedicalInsurancePlanAccounts.
            }
        }

        private void ValidateMedicalInsurancePlanAccounts(SqlConnection connection, SqlTransaction transaction, MedicalInsurancePlan plan)
        {
            if (plan == null || !plan.IsActive || !plan.ShowInPayroll)
            {
                return;
            }

            var calculation = CalculateMedicalInsurance(
                plan.DefaultMonthlyCost,
                plan.DefaultEmployeeShareType,
                plan.DefaultEmployeeShareValue,
                plan.DefaultCompanyShareType,
                plan.DefaultCompanyShareValue);

            if (calculation.EmployeeDeduction > 0)
            {
                ValidateMedicalInsurancePlanAccount(
                    connection,
                    transaction,
                    plan.EmployeeDeductionAccountCode,
                    "حساب استحقاق/ذمم شركة التأمين",
                    "MedicalInsurancePlans.EmployeeDeductionAccountCode");
            }

            if (calculation.CompanyCost > 0)
            {
                ValidateMedicalInsurancePlanAccount(
                    connection,
                    transaction,
                    plan.CompanyCostAccountCode,
                    "حساب مصروف التأمين الطبي للشركة",
                    "MedicalInsurancePlans.CompanyCostAccountCode");
            }
        }

        private void EnsureMedicalInsurancePlanAccounts(SqlConnection connection, SqlTransaction transaction, MedicalInsurancePlan plan)
        {
            if (plan == null || !plan.IsActive || !plan.ShowInPayroll)
            {
                return;
            }

            var calculation = CalculateMedicalInsurance(
                plan.DefaultMonthlyCost,
                plan.DefaultEmployeeShareType,
                plan.DefaultEmployeeShareValue,
                plan.DefaultCompanyShareType,
                plan.DefaultCompanyShareValue);

            var providerName = GetMedicalInsuranceProviderName(connection, transaction, plan.ProviderId);
            if (calculation.EmployeeDeduction > 0 && string.IsNullOrWhiteSpace(plan.EmployeeDeductionAccountCode))
            {
                var parent = ResolveMedicalInsuranceAccountParent(connection, transaction, true);
                var name = "مستحق شركة التأمين الطبي - " + FirstNonEmpty(FirstNonEmpty(providerName, plan.PlanNameAr), "شركة التأمين");
                plan.EmployeeDeductionAccountCode = TryFindOrCreateChildAccount(connection, transaction, parent, name, name, null);
            }

            if (calculation.CompanyCost > 0 && string.IsNullOrWhiteSpace(plan.CompanyCostAccountCode))
            {
                var parent = ResolveMedicalInsuranceAccountParent(connection, transaction, false);
                var name = "مصروف التأمين الطبي - " + FirstNonEmpty(FirstNonEmpty(plan.PlanNameAr, providerName), "خطة التأمين");
                plan.CompanyCostAccountCode = TryFindOrCreateChildAccount(connection, transaction, parent, name, name, null);
            }
        }

        private string GetMedicalInsuranceProviderName(SqlConnection connection, SqlTransaction transaction, int providerId)
        {
            using (var command = CreateCommand(connection, transaction, @"
SELECT TOP (1) COALESCE(NULLIF(ProviderNameAr, N''), NULLIF(ProviderNameEn, N''), N'')
FROM dbo.MedicalInsuranceProviders WITH (NOLOCK)
WHERE ProviderId = @ProviderId;"))
            {
                command.Parameters.Add("@ProviderId", SqlDbType.Int).Value = providerId;
                return Convert.ToString(command.ExecuteScalar());
            }
        }

        private string ResolveMedicalInsuranceAccountParent(SqlConnection connection, SqlTransaction transaction, bool payable)
        {
            var candidates = payable
                ? new[] { "a2a2a5a1a5", "a2a2a5a1a4", "a2a2a4a3", "a2a2a5a1" }
                : new[] { "a3a1a5a4", "a3a1a3a8", "a3a1a5", "a3a1a3" };

            foreach (var candidate in candidates)
            {
                var account = GetAccountDefinition(connection, transaction, candidate);
                if (account != null && !account.LastAccount)
                {
                    return candidate;
                }
            }

            var search = payable ? "%تأمينات%مستحقة%" : "%تامين%طبي%";
            using (var command = CreateCommand(connection, transaction, @"
SELECT TOP (1) Account_Code
FROM dbo.ACCOUNTS WITH (NOLOCK)
WHERE ISNULL(last_account, 0) = 0
  AND (Account_Name LIKE @Search OR Account_NameEng LIKE @Search)
ORDER BY Account_Serial;"))
            {
                command.Parameters.Add("@Search", SqlDbType.NVarChar, 200).Value = search;
                var value = Convert.ToString(command.ExecuteScalar());
                return AccountExists(connection, transaction, value, false) ? value : null;
            }
        }

        private string TryFindOrCreateChildAccount(SqlConnection connection, SqlTransaction transaction, string parentAccountCode, string accountName, string accountNameEn, int? branchId)
        {
            if (string.IsNullOrWhiteSpace(parentAccountCode))
            {
                return null;
            }

            using (var command = CreateCommand(connection, transaction, @"
SELECT TOP (1) Account_Code
FROM dbo.ACCOUNTS WITH (NOLOCK)
WHERE Parent_Account_Code = @ParentAccountCode
  AND LTRIM(RTRIM(ISNULL(Account_Name, N''))) = LTRIM(RTRIM(@AccountName))
  AND ISNULL(last_account, 0) = 1
ORDER BY Account_Serial;"))
            {
                command.Parameters.Add("@ParentAccountCode", SqlDbType.NVarChar, 70).Value = parentAccountCode;
                command.Parameters.Add("@AccountName", SqlDbType.NVarChar, 255).Value = accountName ?? string.Empty;
                var existing = Convert.ToString(command.ExecuteScalar());
                if (AccountExists(connection, transaction, existing, true))
                {
                    return existing;
                }
            }

            return TryCreateEmployeeAccount(connection, transaction, parentAccountCode, accountName, accountNameEn, branchId);
        }

        private void ValidateMedicalInsurancePlanAccount(SqlConnection connection, SqlTransaction transaction, string accountCode, string accountLabel, string source)
        {
            if (string.IsNullOrWhiteSpace(accountCode))
            {
                throw new InvalidOperationException("لا يمكن حفظ/تفعيل خطة التأمين الطبي قبل تحديد " + accountLabel + " (" + source + ").");
            }

            var normalizedAccountCode = accountCode.Trim();
            if (!AccountExists(connection, transaction, normalizedAccountCode, true))
            {
                throw new InvalidOperationException(accountLabel + " غير موجود أو ليس حسابا نهائيا في دليل الحسابات: " + normalizedAccountCode + "، المصدر: " + source + ".");
            }
        }

        private static void ValidateEmployeeMedicalInsurance(EmployeeMedicalInsurance insurance)
        {
            if (insurance.EndDate.HasValue && insurance.StartDate.HasValue && insurance.EndDate.Value.Date < insurance.StartDate.Value.Date)
            {
                throw new InvalidOperationException("تاريخ نهاية اشتراك التأمين لا يمكن أن يكون قبل تاريخ البداية.");
            }

            if (insurance.IsActive && !insurance.PlanId.HasValue)
            {
                throw new InvalidOperationException("يجب اختيار خطة التأمين الطبي للاشتراك النشط.");
            }

            if (insurance.MonthlyCost < 0 || insurance.EmployeeShareValue < 0 || insurance.CompanyShareValue < 0)
            {
                throw new InvalidOperationException("قيم التأمين الطبي لا تقبل أرقاما سالبة.");
            }

            if (insurance.CoveragePercent < 0 || insurance.CoveragePercent > 100)
            {
                throw new InvalidOperationException("نسبة تغطية التأمين الطبي يجب أن تكون بين 0 و 100.");
            }
        }

        private static SalaryRunRequest NormalizeSalaryRequest(SalaryRunRequest request)
        {
            request = request ?? new SalaryRunRequest();
            if (request.Year < 2000 || request.Year > 2100)
            {
                throw new InvalidOperationException("Invalid payroll year.");
            }

            if (request.Month < 1 || request.Month > 12)
            {
                throw new InvalidOperationException("Invalid payroll month.");
            }

            var status = (request.PostingStatus ?? string.Empty).Trim();
            request.PostingStatus =
                string.Equals(status, "Posted", StringComparison.OrdinalIgnoreCase) ? "Posted" :
                string.Equals(status, "Unposted", StringComparison.OrdinalIgnoreCase) ? "Unposted" :
                string.Empty;
            return request;
        }

        private static void AddProviderParameters(SqlCommand command, MedicalInsuranceProvider provider, int id, int userId)
        {
            command.Parameters.Add("@Id", SqlDbType.Int).Value = id;
            AddNullable(command, "@NameAr", SqlDbType.NVarChar, provider.ProviderNameAr);
            AddNullable(command, "@NameEn", SqlDbType.NVarChar, provider.ProviderNameEn);
            AddNullable(command, "@Phone", SqlDbType.NVarChar, provider.Phone);
            AddNullable(command, "@Notes", SqlDbType.NVarChar, provider.Notes);
            command.Parameters.Add("@IsActive", SqlDbType.Bit).Value = provider.IsActive;
            command.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
        }

        private static void AddPlanParameters(SqlCommand command, MedicalInsurancePlan plan, int id, int userId)
        {
            command.Parameters.Add("@Id", SqlDbType.Int).Value = id;
            command.Parameters.Add("@ProviderId", SqlDbType.Int).Value = plan.ProviderId;
            AddNullable(command, "@NameAr", SqlDbType.NVarChar, plan.PlanNameAr);
            AddNullable(command, "@NameEn", SqlDbType.NVarChar, plan.PlanNameEn);
            command.Parameters.Add("@MonthlyCost", SqlDbType.Money).Value = plan.DefaultMonthlyCost;
            command.Parameters.Add("@EmployeeShareType", SqlDbType.NVarChar, 20).Value = NormalizeShareType(plan.DefaultEmployeeShareType, "Amount");
            command.Parameters.Add("@EmployeeShareValue", SqlDbType.Money).Value = plan.DefaultEmployeeShareValue;
            command.Parameters.Add("@CompanyShareType", SqlDbType.NVarChar, 20).Value = NormalizeShareType(plan.DefaultCompanyShareType, "AutoBalance");
            command.Parameters.Add("@CompanyShareValue", SqlDbType.Money).Value = plan.DefaultCompanyShareValue;
            AddNullable(command, "@EmployeeAccountCode", SqlDbType.NVarChar, plan.EmployeeDeductionAccountCode);
            AddNullable(command, "@CompanyAccountCode", SqlDbType.NVarChar, plan.CompanyCostAccountCode);
            command.Parameters.Add("@LifecycleStatus", SqlDbType.NVarChar, 30).Value = NormalizeLifecycleStatus(plan.LifecycleStatus, plan.IsActive);
            AddNullable(command, "@StartDate", SqlDbType.DateTime, plan.StartDate);
            AddNullable(command, "@EndDate", SqlDbType.DateTime, plan.EndDate);
            AddNullable(command, "@PayrollStartDate", SqlDbType.DateTime, plan.PayrollStartDate);
            AddNullable(command, "@SuspensionDate", SqlDbType.DateTime, plan.SuspensionDate);
            AddNullable(command, "@CancellationDate", SqlDbType.DateTime, plan.CancellationDate);
            AddNullable(command, "@CostCenterCode", SqlDbType.NVarChar, plan.CostCenterCode);
            command.Parameters.Add("@PayrollDeductionType", SqlDbType.NVarChar, 20).Value = string.IsNullOrWhiteSpace(plan.PayrollDeductionType) ? "Fixed" : plan.PayrollDeductionType;
            command.Parameters.Add("@IsMonthlyDeduction", SqlDbType.Bit).Value = plan.IsMonthlyDeduction;
            command.Parameters.Add("@AutoStopAtEndDate", SqlDbType.Bit).Value = plan.AutoStopAtEndDate;
            command.Parameters.Add("@ShowInPayroll", SqlDbType.Bit).Value = plan.ShowInPayroll;
            command.Parameters.Add("@DistributeByDepartment", SqlDbType.Bit).Value = plan.DistributeByDepartment;
            command.Parameters.Add("@DistributeByCostCenter", SqlDbType.Bit).Value = plan.DistributeByCostCenter;
            command.Parameters.Add("@TaxMode", SqlDbType.NVarChar, 20).Value = string.IsNullOrWhiteSpace(plan.TaxMode) ? "AfterTax" : plan.TaxMode;
            command.Parameters.Add("@MaxDependents", SqlDbType.Int).Value = plan.MaxDependents;
            command.Parameters.Add("@ChildrenMaxAge", SqlDbType.Int).Value = plan.ChildrenMaxAge;
            command.Parameters.Add("@SpouseAdditionalCost", SqlDbType.Money).Value = plan.SpouseAdditionalCost;
            command.Parameters.Add("@ChildAdditionalCost", SqlDbType.Money).Value = plan.ChildAdditionalCost;
            command.Parameters.Add("@ParentAdditionalCost", SqlDbType.Money).Value = plan.ParentAdditionalCost;
            command.Parameters.Add("@DefaultCoveragePercent", SqlDbType.Money).Value = plan.DefaultCoveragePercent;
            command.Parameters.Add("@AutoEnrollAfterDays", SqlDbType.Int).Value = plan.AutoEnrollAfterDays;
            AddNullable(command, "@AutoEnrollCriteria", SqlDbType.NVarChar, plan.AutoEnrollCriteria);
            AddNullable(command, "@RulesJson", SqlDbType.NVarChar, plan.RulesJson);
            AddNullable(command, "@DependentsTemplateJson", SqlDbType.NVarChar, plan.DependentsTemplateJson);
            command.Parameters.Add("@IsActive", SqlDbType.Bit).Value = plan.IsActive;
            AddNullable(command, "@Notes", SqlDbType.NVarChar, plan.Notes);
            command.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
        }

        private static void AddEmployeeParameters(SqlCommand command, EmployeeSaveRequest request, int employeeId, EmployeeAccountCodes employeeAccounts)
        {
            command.Parameters.Add("@Id", SqlDbType.Int).Value = employeeId;
            command.Parameters.Add("@Code", SqlDbType.NVarChar, 50).Value = (object)(request.EmployeeCode ?? string.Empty) ?? DBNull.Value;
            command.Parameters.Add("@Name", SqlDbType.NVarChar, 255).Value = (object)(request.EmployeeName ?? string.Empty) ?? DBNull.Value;
            AddNullable(command, "@BranchId", SqlDbType.Int, request.BranchId);
            AddNullable(command, "@DepartmentId", SqlDbType.Int, request.DepartmentId);
            AddNullable(command, "@JobTypeId", SqlDbType.Int, request.JobTypeId);
            AddNullable(command, "@HiringDate", SqlDbType.DateTime, request.HiringDate);
            command.Parameters.Add("@Stopped", SqlDbType.Bit).Value = request.IsActive ? 0 : 1;
            command.Parameters.Add("@WorkState", SqlDbType.Int).Value = request.IsActive ? 1 : 0;
            command.Parameters.Add("@Salary", SqlDbType.Float).Value = request.BasicSalary;
            AddNullable(command, "@AccountCode", SqlDbType.NVarChar, employeeAccounts.EmployeeAccountCode);
            AddNullable(command, "@AccruedAccountCode", SqlDbType.NVarChar, employeeAccounts.AccruedSalaryAccountCode);
            AddNullable(command, "@VacationProvisionAccountCode", SqlDbType.NVarChar, employeeAccounts.VacationProvisionAccountCode);
            AddNullable(command, "@AdvancePaymentAccountCode", SqlDbType.NVarChar, employeeAccounts.AdvancePaymentAccountCode);
            AddNullable(command, "@EndOfServiceAccountCode", SqlDbType.NVarChar, employeeAccounts.EndOfServiceAccountCode);
            AddNullable(command, "@TicketProvisionAccountCode", SqlDbType.NVarChar, employeeAccounts.TicketProvisionAccountCode);
            AddNullable(command, "@Phone", SqlDbType.NVarChar, request.Phone);
            AddNullable(command, "@Mobile", SqlDbType.NVarChar, request.Mobile);
            AddNullable(command, "@Email", SqlDbType.NVarChar, request.Email);
            var photo = NormalizeEmployeePhotoDataUrl(request.PhotoDataUrl);
            command.Parameters.Add("@PhotoDataUrl", SqlDbType.NVarChar, -1).Value = string.IsNullOrWhiteSpace(photo) ? (object)DBNull.Value : photo;
            AddNullable(command, "@Notes", SqlDbType.NVarChar, request.Notes);
        }

        private static string NormalizeEmployeePhotoDataUrl(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            value = value.Trim();
            if (value.Length > 250000)
            {
                throw new InvalidOperationException("صورة الموظف كبيرة جدا. اختر صورة أوضح بحجم أقل.");
            }

            var lower = value.ToLowerInvariant();
            if (!lower.StartsWith("data:image/png;base64,", StringComparison.Ordinal)
                && !lower.StartsWith("data:image/jpeg;base64,", StringComparison.Ordinal)
                && !lower.StartsWith("data:image/jpg;base64,", StringComparison.Ordinal)
                && !lower.StartsWith("data:image/webp;base64,", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("صيغة صورة الموظف غير مدعومة. استخدم صورة PNG أو JPG أو WebP.");
            }

            return value;
        }

        private static void AddSalaryParameters(SqlCommand command, SalaryRunEmployeeRow row, SalaryRunRequest request, string sgn)
        {
            command.Parameters.Add("@EmployeeId", SqlDbType.Int).Value = row.EmployeeId;
            command.Parameters.Add("@Code", SqlDbType.VarChar, 50).Value = (object)row.EmployeeCode ?? DBNull.Value;
            command.Parameters.Add("@Name", SqlDbType.VarChar, 255).Value = (object)row.EmployeeName ?? DBNull.Value;
            command.Parameters.Add("@Basic", SqlDbType.Money).Value = row.BasicSalary;
            command.Parameters.Add("@TotalBefore", SqlDbType.Money).Value = row.TotalBeforeDeductions;
            command.Parameters.Add("@Advance", SqlDbType.Money).Value = row.AdvanceDeduction;
            command.Parameters.Add("@Discounts", SqlDbType.Money).Value = row.ExistingDiscounts + row.MedicalInsuranceDeduction;
            command.Parameters.Add("@TotalDeductions", SqlDbType.Money).Value = row.TotalDeductions;
            command.Parameters.Add("@Net", SqlDbType.Money).Value = row.NetSalary;
            command.Parameters.Add("@Sgn", SqlDbType.VarChar, 20).Value = sgn;
            command.Parameters.Add("@YearText", SqlDbType.VarChar, 10).Value = request.Year.ToString();
            command.Parameters.Add("@MonthText", SqlDbType.VarChar, 10).Value = request.Month.ToString();
            AddNullable(command, "@BranchId", SqlDbType.Int, row.BranchId);
            AddNullable(command, "@DepartmentId", SqlDbType.Int, row.DepartmentId);
        }

        private static void AddSalarySnapshotParameters(SqlCommand command, SalaryRunEmployeeRow row)
        {
            command.Parameters.Add("@Insurance", SqlDbType.Float).Value = Convert.ToDouble(row.TotalInsuranceLegacy);
            command.Parameters.Add("@VacationDeduction", SqlDbType.Int).Value = Convert.ToInt32(Math.Round(row.VacationDeduction, 0));
            command.Parameters.Add("@AbsentDays", SqlDbType.Float).Value = Convert.ToDouble(row.AbsentDays);
            command.Parameters.Add("@CountDays", SqlDbType.Float).Value = Convert.ToDouble(row.CountDays);
            command.Parameters.Add("@RemainingDays", SqlDbType.Float).Value = Convert.ToDouble(row.RemainingDays);
            AddNullable(command, "@ProjectId", SqlDbType.Int, row.ProjectId);
            for (var i = 1; i <= 40; i++)
            {
                command.Parameters.Add("@Comp" + i.ToString(), SqlDbType.Float).Value = Convert.ToDouble(GetComponentSnapshotValue(row, i));
            }
        }

        private static void AddLegacyVacationSalaryParameters(SqlCommand command, SalaryRunEmployeeRow row, bool includeParameters)
        {
            if (!includeParameters)
            {
                return;
            }

            command.Parameters.Add("@VacationSalaryValue", SqlDbType.Float).Value = Convert.ToDouble(row.VacationSalaryValue);
            command.Parameters.Add("@VacationDays", SqlDbType.Float).Value = Convert.ToDouble(row.VacationDays);
        }

        private static decimal GetComponentSnapshotValue(SalaryRunEmployeeRow row, int componentNo)
        {
            var component = row.Components.FirstOrDefault(x => x.ComponentNo == componentNo);
            if (component == null)
            {
                return 0m;
            }

            return row.IsLegacySnapshot ? component.SnapshotValue : component.SourceValue;
        }

        private static string BuildComponentUpdateSet(SalaryRunEmployeeRow row)
        {
            var parts = new List<string>();
            for (var i = 1; i <= 40; i++)
            {
                parts.Add("Comp" + i.ToString() + " = @Comp" + i.ToString());
            }

            return "," + string.Join(",", parts.ToArray());
        }

        private static string BuildComponentInsertColumns(SalaryRunEmployeeRow row)
        {
            var parts = new List<string>();
            for (var i = 1; i <= 40; i++)
            {
                parts.Add("Comp" + i.ToString());
            }

            return "," + string.Join(",", parts.ToArray());
        }

        private static string BuildComponentInsertValues(SalaryRunEmployeeRow row)
        {
            var parts = new List<string>();
            for (var i = 1; i <= 40; i++)
            {
                parts.Add("@Comp" + i.ToString());
            }

            return "," + string.Join(",", parts.ToArray());
        }

        private static void AddInsuranceParameters(SqlCommand command, EmployeeMedicalInsurance insurance, int employeeId, int userId)
        {
            command.Parameters.Add("@EmpId", SqlDbType.Int).Value = employeeId;
            AddNullable(command, "@PlanId", SqlDbType.Int, insurance.PlanId);
            AddNullable(command, "@PolicyNumber", SqlDbType.NVarChar, insurance.PolicyNumber);
            AddNullable(command, "@CardNumber", SqlDbType.NVarChar, insurance.CardNumber);
            command.Parameters.Add("@CoveragePercent", SqlDbType.Money).Value = insurance.CoveragePercent <= 0 ? 100m : insurance.CoveragePercent;
            AddNullable(command, "@StartDate", SqlDbType.DateTime, insurance.StartDate);
            AddNullable(command, "@EndDate", SqlDbType.DateTime, insurance.EndDate);
            command.Parameters.Add("@IsMonthly", SqlDbType.Bit).Value = insurance.IsMonthly;
            command.Parameters.Add("@IsActive", SqlDbType.Bit).Value = insurance.IsActive;
            command.Parameters.Add("@MonthlyCost", SqlDbType.Money).Value = insurance.MonthlyCost;
            command.Parameters.Add("@EmployeeShareType", SqlDbType.NVarChar, 20).Value = NormalizeShareType(insurance.EmployeeShareType, "Amount");
            command.Parameters.Add("@EmployeeShareValue", SqlDbType.Money).Value = insurance.EmployeeShareValue;
            command.Parameters.Add("@CompanyShareType", SqlDbType.NVarChar, 20).Value = NormalizeShareType(insurance.CompanyShareType, "AutoBalance");
            command.Parameters.Add("@CompanyShareValue", SqlDbType.Money).Value = insurance.CompanyShareValue;
            command.Parameters.Add("@EmployeeMonthlyDeduction", SqlDbType.Money).Value = insurance.EmployeeMonthlyDeduction;
            command.Parameters.Add("@CompanyMonthlyCost", SqlDbType.Money).Value = insurance.CompanyMonthlyCost;
            AddNullable(command, "@Notes", SqlDbType.NVarChar, insurance.Notes);
            command.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
        }

        private static void AddReportParameters(SqlCommand command, MedicalInsuranceReportFilter filter)
        {
            filter = NormalizeMedicalInsuranceReportFilter(filter);
            command.Parameters.Add("@ActiveOnly", SqlDbType.Bit).Value = filter.ActiveOnly;
            AddNullable(command, "@ProviderId", SqlDbType.Int, filter.ProviderId);
            AddNullable(command, "@PlanId", SqlDbType.Int, filter.PlanId);
            AddNullable(command, "@BranchId", SqlDbType.Int, filter.BranchId);
            AddNullable(command, "@DepartmentId", SqlDbType.Int, filter.DepartmentId);
            AddNullable(command, "@EmployeeId", SqlDbType.Int, filter.EmployeeId);
            AddNullable(command, "@From", SqlDbType.DateTime, filter.PeriodFrom);
            AddNullable(command, "@To", SqlDbType.DateTime, filter.PeriodTo);
            command.Parameters.Add("@Status", SqlDbType.NVarChar, 20).Value = filter.Status ?? string.Empty;
            command.Parameters.Add("@PostingStatus", SqlDbType.NVarChar, 20).Value = filter.PostingStatus ?? string.Empty;
        }

        private static MedicalInsuranceReportFilter NormalizeMedicalInsuranceReportFilter(MedicalInsuranceReportFilter filter)
        {
            filter = filter ?? new MedicalInsuranceReportFilter();
            if (filter.PeriodFrom.HasValue && filter.PeriodTo.HasValue && filter.PeriodTo.Value.Date < filter.PeriodFrom.Value.Date)
            {
                var from = filter.PeriodFrom;
                filter.PeriodFrom = filter.PeriodTo;
                filter.PeriodTo = from;
            }

            var status = (filter.Status ?? string.Empty).Trim();
            filter.Status =
                string.Equals(status, "Active", StringComparison.OrdinalIgnoreCase) ? "Active" :
                string.Equals(status, "Expired", StringComparison.OrdinalIgnoreCase) ? "Expired" :
                string.Equals(status, "Cancelled", StringComparison.OrdinalIgnoreCase) ? "Cancelled" :
                string.Empty;

            var postingStatus = (filter.PostingStatus ?? string.Empty).Trim();
            filter.PostingStatus =
                string.Equals(postingStatus, "Posted", StringComparison.OrdinalIgnoreCase) ? "Posted" :
                string.Equals(postingStatus, "Unposted", StringComparison.OrdinalIgnoreCase) ? "Unposted" :
                string.Empty;

            return filter;
        }

        private static string BuildMedicalInsurancePeriodLabel(MedicalInsuranceReportFilter filter)
        {
            if (filter == null || (!filter.PeriodFrom.HasValue && !filter.PeriodTo.HasValue))
            {
                return "كل الفترات";
            }

            var from = filter.PeriodFrom.HasValue ? filter.PeriodFrom.Value.ToString("yyyy/MM/dd") : "البداية";
            var to = filter.PeriodTo.HasValue ? filter.PeriodTo.Value.ToString("yyyy/MM/dd") : "النهاية";
            return from + " - " + to;
        }

        private static string NormalizeShareType(string value, string fallback)
        {
            value = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
            if (!string.Equals(value, "Amount", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(value, "Percent", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(value, "AutoBalance", StringComparison.OrdinalIgnoreCase))
            {
                return fallback;
            }

            return value;
        }

        private static string NormalizeLifecycleStatus(string value, bool isActive)
        {
            value = string.IsNullOrWhiteSpace(value) ? (isActive ? "Active" : "Draft") : value.Trim();
            if (string.Equals(value, "Draft", StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, "Pending Approval", StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, "Active", StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, "Suspended", StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, "Expired", StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, "Cancelled", StringComparison.OrdinalIgnoreCase))
            {
                return value;
            }

            return isActive ? "Active" : "Draft";
        }

        private static EmployeeSummary ReadEmployee(IDataRecord reader)
        {
            return new EmployeeSummary
            {
                EmployeeId = ReadInt(reader, "Emp_ID"),
                EmployeeCode = ReadString(reader, "Emp_Code"),
                EmployeeName = ReadString(reader, "Emp_Name"),
                BranchId = ReadNullableInt(reader, "BranchId"),
                BranchName = ReadString(reader, "branch_name"),
                DepartmentId = ReadNullableInt(reader, "DepartmentID"),
                DepartmentName = ReadString(reader, "DepartmentName"),
                JobTypeId = ReadNullableInt(reader, "JobTypeID"),
                JobTypeName = ReadString(reader, "JobTypeName"),
                HiringDate = ReadNullableDate(reader, "BignDateWork"),
                IsActive = !ReadBool(reader, "chkStop"),
                BasicSalary = ReadDecimal(reader, "Emp_Salary"),
                AccountCode = ReadString(reader, "Account_code"),
                AccruedSalaryAccountCode = ReadString(reader, "Account_code1"),
                Phone = ReadString(reader, "Emp_Phone"),
                Mobile = ReadString(reader, "Emp_mobile"),
                Email = ReadString(reader, "Emp_Mail"),
                PhotoDataUrl = ReadString(reader, "EmployeePhotoDataUrl"),
                Notes = ReadString(reader, "EmpNotes")
            };
        }

        private static MedicalInsurancePlan ReadPlan(IDataRecord reader)
        {
            return new MedicalInsurancePlan
            {
                PlanId = ReadNullableInt(reader, "PlanId"),
                ProviderId = ReadInt(reader, "ProviderId"),
                ProviderName = ReadString(reader, "ProviderNameAr"),
                PlanNameAr = ReadString(reader, "PlanNameAr"),
                PlanNameEn = ReadString(reader, "PlanNameEn"),
                DefaultMonthlyCost = ReadDecimal(reader, "DefaultMonthlyCost"),
                DefaultEmployeeShareType = ReadString(reader, "DefaultEmployeeShareType"),
                DefaultEmployeeShareValue = ReadDecimal(reader, "DefaultEmployeeShareValue"),
                DefaultCompanyShareType = ReadString(reader, "DefaultCompanyShareType"),
                DefaultCompanyShareValue = ReadDecimal(reader, "DefaultCompanyShareValue"),
                EmployeeDeductionAccountCode = ReadString(reader, "EmployeeDeductionAccountCode"),
                CompanyCostAccountCode = ReadString(reader, "CompanyCostAccountCode"),
                LifecycleStatus = ReadString(reader, "LifecycleStatus"),
                StartDate = ReadNullableDate(reader, "StartDate"),
                EndDate = ReadNullableDate(reader, "EndDate"),
                PayrollStartDate = ReadNullableDate(reader, "PayrollStartDate"),
                SuspensionDate = ReadNullableDate(reader, "SuspensionDate"),
                CancellationDate = ReadNullableDate(reader, "CancellationDate"),
                CostCenterCode = ReadString(reader, "CostCenterCode"),
                PayrollDeductionType = ReadString(reader, "PayrollDeductionType"),
                IsMonthlyDeduction = ReadBool(reader, "IsMonthlyDeduction"),
                AutoStopAtEndDate = ReadBool(reader, "AutoStopAtEndDate"),
                ShowInPayroll = ReadBool(reader, "ShowInPayroll"),
                DistributeByDepartment = ReadBool(reader, "DistributeByDepartment"),
                DistributeByCostCenter = ReadBool(reader, "DistributeByCostCenter"),
                TaxMode = ReadString(reader, "TaxMode"),
                MaxDependents = ReadInt(reader, "MaxDependents"),
                ChildrenMaxAge = ReadInt(reader, "ChildrenMaxAge"),
                SpouseAdditionalCost = ReadDecimal(reader, "SpouseAdditionalCost"),
                ChildAdditionalCost = ReadDecimal(reader, "ChildAdditionalCost"),
                ParentAdditionalCost = ReadDecimal(reader, "ParentAdditionalCost"),
                DefaultCoveragePercent = ReadDecimal(reader, "DefaultCoveragePercent"),
                AutoEnrollAfterDays = ReadInt(reader, "AutoEnrollAfterDays"),
                AutoEnrollCriteria = ReadString(reader, "AutoEnrollCriteria"),
                RulesJson = ReadString(reader, "RulesJson"),
                DependentsTemplateJson = ReadString(reader, "DependentsTemplateJson"),
                IsActive = ReadBool(reader, "IsActive"),
                Notes = ReadString(reader, "Notes")
            };
        }

        private static EmployeeMedicalInsurance ReadEmployeeMedicalInsurance(IDataRecord reader)
        {
            return new EmployeeMedicalInsurance
            {
                Id = ReadNullableInt(reader, "Id"),
                EmployeeId = ReadInt(reader, "EmpId"),
                PlanId = ReadNullableInt(reader, "PlanId"),
                PlanName = ReadString(reader, "PlanNameAr"),
                ProviderName = ReadString(reader, "ProviderNameAr"),
                PolicyNumber = ReadString(reader, "PolicyNumber"),
                CardNumber = ReadString(reader, "CardNumber"),
                CoveragePercent = ReadDecimal(reader, "CoveragePercent"),
                StartDate = ReadNullableDate(reader, "StartDate"),
                EndDate = ReadNullableDate(reader, "EndDate"),
                IsMonthly = ReadBool(reader, "IsMonthly"),
                IsActive = ReadBool(reader, "IsActive"),
                MonthlyCost = ReadDecimal(reader, "MonthlyCost"),
                EmployeeShareType = ReadString(reader, "EmployeeShareType"),
                EmployeeShareValue = ReadDecimal(reader, "EmployeeShareValue"),
                CompanyShareType = ReadString(reader, "CompanyShareType"),
                CompanyShareValue = ReadDecimal(reader, "CompanyShareValue"),
                EmployeeMonthlyDeduction = ReadDecimal(reader, "EmployeeMonthlyDeduction"),
                CompanyMonthlyCost = ReadDecimal(reader, "CompanyMonthlyCost"),
                Notes = ReadString(reader, "Notes"),
                CreatedAt = ReadNullableDate(reader, "CreatedAt"),
                UpdatedAt = ReadNullableDate(reader, "UpdatedAt")
            };
        }

        private static void FillLookup(SqlConnection connection, string sql, IList<LookupItem> items)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = sql;
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        items.Add(new LookupItem { Id = Convert.ToInt32(reader.GetValue(0)), Name = Convert.ToString(reader.GetValue(1)) });
                    }
                }
            }
        }

        private static bool TableExists(SqlConnection connection, string tableName)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT OBJECT_ID(N'dbo.' + @TableName, N'U')";
                command.Parameters.Add("@TableName", SqlDbType.NVarChar, 128).Value = tableName;
                var value = command.ExecuteScalar();
                return value != null && value != DBNull.Value;
            }
        }

        private static bool TableExists(SqlConnection connection, SqlTransaction transaction, string tableName)
        {
            using (var command = CreateCommand(connection, transaction, "SELECT OBJECT_ID(N'dbo.' + @TableName, N'U')"))
            {
                command.Parameters.Add("@TableName", SqlDbType.NVarChar, 128).Value = tableName;
                var value = command.ExecuteScalar();
                return value != null && value != DBNull.Value;
            }
        }

        private static bool FunctionExists(SqlConnection connection, string functionName)
        {
            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT OBJECT_ID(N'dbo.' + @FunctionName, N'FN')";
                command.Parameters.Add("@FunctionName", SqlDbType.NVarChar, 128).Value = functionName;
                var value = command.ExecuteScalar();
                if (value != null && value != DBNull.Value)
                {
                    return true;
                }

                command.Parameters.Clear();
                command.CommandText = "SELECT OBJECT_ID(N'dbo.' + @FunctionName, N'TF')";
                command.Parameters.Add("@FunctionName", SqlDbType.NVarChar, 128).Value = functionName;
                value = command.ExecuteScalar();
                return value != null && value != DBNull.Value;
            }
        }

        private SqlConnection OpenConnection()
        {
            var connection = new SqlConnection(_connectionString);
            connection.Open();
            return connection;
        }

        private static string ResolveConnectionString()
        {
            var setting = ConfigurationManager.ConnectionStrings["MainErp_ConnectionString"]
                ?? ConfigurationManager.ConnectionStrings["KishnyCashConnection"]
                ?? ConfigurationManager.ConnectionStrings["MyERP_ConnectionString"];
            if (setting == null)
            {
                throw new InvalidOperationException("Database connection string was not found.");
            }

            return setting.ConnectionString;
        }

        private static SqlCommand CreateCommand(SqlConnection connection, SqlTransaction transaction, string sql)
        {
            var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = sql;
            return command;
        }

        private static int NextId(SqlConnection connection, SqlTransaction transaction, string table, string column)
        {
            using (var command = CreateCommand(connection, transaction, "SELECT ISNULL(MAX(" + column + "), 0) + 1 FROM dbo." + table + " WITH (UPDLOCK, HOLDLOCK)"))
            {
                return Convert.ToInt32(command.ExecuteScalar());
            }
        }

        private static void AddNullable(SqlCommand command, string name, SqlDbType type, object value)
        {
            var parameter = command.Parameters.Add(name, type);
            parameter.Value = value == null || value == DBNull.Value || (value is string && string.IsNullOrWhiteSpace((string)value)) ? (object)DBNull.Value : value;
        }

        private static string FirstNonEmpty(string first, string second)
        {
            return !string.IsNullOrWhiteSpace(first) ? first : second;
        }

        private static string BuildAccountDisplay(string accountSerial, string accountName)
        {
            accountSerial = (accountSerial ?? string.Empty).Trim();
            accountName = (accountName ?? string.Empty).Trim();
            if (!string.IsNullOrWhiteSpace(accountSerial) && !string.IsNullOrWhiteSpace(accountName))
            {
                return accountSerial + " - " + accountName;
            }

            return FirstNonEmpty(accountName, accountSerial);
        }

        private static string ReadString(IDataRecord reader, string name)
        {
            var value = reader[name];
            return value == DBNull.Value ? string.Empty : Convert.ToString(value);
        }

        private static int ReadInt(IDataRecord reader, string name)
        {
            var value = reader[name];
            return value == DBNull.Value ? 0 : Convert.ToInt32(value);
        }

        private static int? ReadNullableInt(IDataRecord reader, string name)
        {
            var value = reader[name];
            return value == DBNull.Value ? (int?)null : Convert.ToInt32(value);
        }

        private static decimal ReadDecimal(IDataRecord reader, string name)
        {
            var value = reader[name];
            return value == DBNull.Value ? 0m : Convert.ToDecimal(value);
        }

        private static DateTime? ReadNullableDate(IDataRecord reader, string name)
        {
            var value = reader[name];
            return value == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(value);
        }

        private static string FormatDate(object value)
        {
            if (value == null || value == DBNull.Value)
            {
                return string.Empty;
            }

            DateTime date;
            return DateTime.TryParse(Convert.ToString(value), out date) ? date.ToString("yyyy/MM/dd") : Convert.ToString(value);
        }

        private static object ParseDisplayDate(string value)
        {
            DateTime date;
            return DateTime.TryParse(value, out date) ? (object)date : DBNull.Value;
        }

        private static bool ReadBool(IDataRecord reader, string name)
        {
            var value = reader[name];
            return value != DBNull.Value && Convert.ToBoolean(value);
        }

        private static bool ReadBoolIfExists(IDataRecord reader, string name)
        {
            if (!HasColumn(reader, name))
            {
                return false;
            }

            var value = reader[name];
            return value != DBNull.Value && Convert.ToBoolean(value);
        }

        private static int? ReadNullableIntIfExists(IDataRecord reader, string name)
        {
            if (!HasColumn(reader, name))
            {
                return null;
            }

            var value = reader[name];
            return value == DBNull.Value ? (int?)null : Convert.ToInt32(value);
        }

        private static bool HasColumn(IDataRecord reader, string name)
        {
            for (var i = 0; i < reader.FieldCount; i++)
            {
                if (string.Equals(reader.GetName(i), name, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private class EmployeeAccountCodes
        {
            public string EmployeeAccountCode { get; set; }
            public string AccruedSalaryAccountCode { get; set; }
            public string VacationProvisionAccountCode { get; set; }
            public string AdvancePaymentAccountCode { get; set; }
            public string EndOfServiceAccountCode { get; set; }
            public string TicketProvisionAccountCode { get; set; }
        }

        private class EmployeeAccountParents
        {
            public string EmployeeAccountParentCode { get; set; }
            public string AccruedSalaryParentCode { get; set; }
            public string VacationProvisionParentCode { get; set; }
            public string AdvancePaymentParentCode { get; set; }
            public string EndOfServiceParentCode { get; set; }
            public string TicketProvisionParentCode { get; set; }
        }

        private class AccountDefinition
        {
            public string AccountCode { get; set; }
            public string AccountSerial { get; set; }
            public bool LastAccount { get; set; }
            public bool Budget { get; set; }
            public string CurrencyCode { get; set; }
            public bool CostCenter { get; set; }
            public bool SumAccount { get; set; }
            public int? CostCenterType { get; set; }
            public string CostCenterId { get; set; }
            public int? ActivityTypeId { get; set; }
            public int? AccountTypes { get; set; }
            public int? AccountTab { get; set; }
            public int? DepitOrCredit { get; set; }
            public int? DifferentType { get; set; }
            public int? Authority { get; set; }
            public int? UserGroupId { get; set; }
            public int? UserId { get; set; }
            public string Branch { get; set; }
            public int? BranchId { get; set; }
        }

        private class MismatchClassification
        {
            public string Category { get; set; }
            public string LikelySource { get; set; }
            public decimal ConfidenceScore { get; set; }
        }

        private class TemporalCompatibilityContext
        {
            public decimal CalendarMonthDays { get; set; }
            public decimal PayrollMonthDayNo { get; set; }
            public decimal PayrollDays { get; set; }
            public decimal ExpectedDenominator { get; set; }
            public decimal ActualDenominator { get; set; }
            public decimal NumeratorDays { get; set; }
            public bool CountFlag { get; set; }
            public bool ProrationApplied { get; set; }
            public bool ProrationBypassed { get; set; }
            public bool VacationOverlap { get; set; }
            public string BranchProjectScope { get; set; }
            public string DenominatorReason { get; set; }
            public string RulePath { get; set; }
        }
    }
}

