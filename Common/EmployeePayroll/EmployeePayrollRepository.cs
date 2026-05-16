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

            return rows;
        }

        public IList<MedicalInsurancePlan> GetMedicalInsurancePlans(bool activeOnly)
        {
            var rows = new List<MedicalInsurancePlan>();
            using (var connection = OpenConnection())
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

            return rows;
        }

        public MedicalInsurancePlan GetMedicalInsurancePlan(int planId)
        {
            using (var connection = OpenConnection())
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

        public int SaveMedicalInsuranceProvider(MedicalInsuranceProvider provider, int userId)
        {
            if (provider == null || string.IsNullOrWhiteSpace(provider.ProviderNameAr))
            {
                throw new InvalidOperationException("Provider name is required.");
            }

            using (var connection = OpenConnection())
            using (var transaction = connection.BeginTransaction())
            {
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
                throw new InvalidOperationException("Provider and plan name are required.");
            }

            ValidateShare("Employee", plan.DefaultMonthlyCost, plan.DefaultEmployeeShareType, plan.DefaultEmployeeShareValue);
            ValidateShare("Company", plan.DefaultMonthlyCost, plan.DefaultCompanyShareType, plan.DefaultCompanyShareValue);
            ValidatePlanLifecycle(plan);

            using (var connection = OpenConnection())
            using (var transaction = connection.BeginTransaction())
            {
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
    e.BignDateWork, e.chkStop, e.Emp_Salary, e.Account_code, e.Account_code1,
    e.Emp_Phone, e.Emp_mobile, e.Emp_Mail, e.EmpNotes
FROM dbo.TblEmployee e WITH (NOLOCK)
LEFT JOIN dbo.TblBranchesData b WITH (NOLOCK) ON b.branch_id = e.BranchId
LEFT JOIN dbo.TblEmpDepartments d WITH (NOLOCK) ON d.DeparmentID = e.DepartmentID
LEFT JOIN dbo.TblEmpJobsTypes j WITH (NOLOCK) ON j.JobTypeID = e.JobTypeID
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
    e.BignDateWork, e.chkStop, e.Emp_Salary, e.Account_code, e.Account_code1,
    e.Emp_Phone, e.Emp_mobile, e.Emp_Mail, e.EmpNotes
FROM dbo.TblEmployee e WITH (NOLOCK)
LEFT JOIN dbo.TblBranchesData b WITH (NOLOCK) ON b.branch_id = e.BranchId
LEFT JOIN dbo.TblEmpDepartments d WITH (NOLOCK) ON d.DeparmentID = e.DepartmentID
LEFT JOIN dbo.TblEmpJobsTypes j WITH (NOLOCK) ON j.JobTypeID = e.JobTypeID
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
(Emp_ID, Emp_Code, Emp_Name, BranchId, DepartmentID, JobTypeID, BignDateWork, chkStop, Emp_Salary, Account_code, Account_code1, Account_Code2, Account_Code3, Account_Code4, Account_Code5, Emp_Phone, Emp_mobile, Emp_Mail, EmpNotes)
VALUES
(@Id, @Code, @Name, @BranchId, @DepartmentId, @JobTypeId, @HiringDate, @Stopped, @Salary, @AccountCode, @AccruedAccountCode, @VacationProvisionAccountCode, @AdvancePaymentAccountCode, @EndOfServiceAccountCode, @TicketProvisionAccountCode, @Phone, @Mobile, @Email, @Notes);"))
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
    EmpNotes = @Notes
WHERE Emp_ID = @Id;"))
                    {
                        AddEmployeeParameters(command, request, employeeId, employeeAccounts);
                        command.ExecuteNonQuery();
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
            return PreviewSalaryRunCompatibility(request);
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
            preview.Message = preview.HasExistingApprovedRows
                ? "VB6-compatible preview built from legacy emp_salary snapshots. Approved rows are read-only."
                : "VB6-compatible preview built from emp_salary snapshots, with component reconstruction fallback where no snapshot exists.";
            ApplySalaryPreviewPayloadLimits(preview, request);
            return preview;
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
    ISNULL(mi.CompanyMonthlyCost, 0) AS CompanyMonthlyCost
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
    ORDER BY x.StartDate DESC, x.Id DESC
) mi
LEFT JOIN dbo.MedicalInsurancePlans pl WITH (NOLOCK) ON pl.PlanId = mi.PlanId
WHERE ISNULL(e.chkStop, 0) = 0
  AND (@BranchId IS NULL OR e.BranchId = @BranchId)
  AND (@DepartmentId IS NULL OR e.DepartmentID = @DepartmentId)
  AND (@EmployeeId IS NULL OR e.Emp_ID = @EmployeeId)
  AND (@IncludeSavedDrafts = 1 OR s.id IS NULL OR ISNULL(s.payed, 0) = 0)
ORDER BY e.Fullcode, e.Emp_Code, e.Emp_ID;" : @"
SELECT
    e.Emp_ID, e.Emp_Code, e.Emp_Name, e.BranchId, b.branch_name, e.DepartmentID, d.DepartmentName,
    ISNULL(e.Emp_Salary, 0) AS Emp_Salary,
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
    CONVERT(money, 0) AS CompanyMonthlyCost
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
  AND (@BranchId IS NULL OR e.BranchId = @BranchId)
  AND (@DepartmentId IS NULL OR e.DepartmentID = @DepartmentId)
  AND (@EmployeeId IS NULL OR e.Emp_ID = @EmployeeId)
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
                command.CommandText = hasLegacyFunctions ? @"
SELECT
    e.Emp_ID, e.Emp_Code, e.Emp_Name, e.BranchId, b.branch_name, e.DepartmentID, d.DepartmentName,
    s.project_id, p.Project_name, p.Salary_account AS ProjectSalaryAccount,
    e.Account_code, e.Account_code1, e.Account_Code2, e.Account_Code3, e.BignDateWork, e.lastHolidaydate, opt.MonthIs30days, opt.EmpSalaryDigts,
    ISNULL(e.Emp_Salary, 0) AS Emp_Salary,
    s.id AS SalaryRowId, s.payed, s.total1, s.total2, s.EmpTotalNet, s.TotalAdvance, s.TotalDiscount,
    s.ToalInsurance, s.CountDays, s.AbcentDay, s.RemainDay, s.VoCation3, s.Mokafea, s.SalesCom,
    ISNULL(a.TotalAdvance, 0) AS RuntimeAdvance,
    dbo.EmpInsurances(@Month - 1, @Year, e.Emp_ID) AS RuntimeInsurance,
    dbo.EmpVoCation3(@Month, @Year, e.Emp_ID) AS RuntimeVacation3,
    dbo.EmpPrePaymentValue(dbo.EmpPrePaymentID(e.Emp_ID)) AS RuntimePrePaymentValue,
    dbo.GetAbcentDay(e.Emp_ID, @Year, @Month) AS RuntimeAbsentDays
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
  AND (@BranchId IS NULL OR e.BranchId = @BranchId)
  AND (@DepartmentId IS NULL OR e.DepartmentID = @DepartmentId)
  AND (@EmployeeId IS NULL OR e.Emp_ID = @EmployeeId)
  AND (@IncludeSavedDrafts = 1 OR s.id IS NULL OR ISNULL(s.payed, 0) = 0)
ORDER BY e.Fullcode, e.Emp_Code, e.Emp_ID;" : @"
SELECT
    e.Emp_ID, e.Emp_Code, e.Emp_Name, e.BranchId, b.branch_name, e.DepartmentID, d.DepartmentName,
    s.project_id, p.Project_name, p.Salary_account AS ProjectSalaryAccount,
    e.Account_code, e.Account_code1, e.Account_Code2, e.Account_Code3, e.BignDateWork, e.lastHolidaydate, opt.MonthIs30days, opt.EmpSalaryDigts,
    ISNULL(e.Emp_Salary, 0) AS Emp_Salary,
    s.id AS SalaryRowId, s.payed, s.total1, s.total2, s.EmpTotalNet, s.TotalAdvance, s.TotalDiscount,
    s.ToalInsurance, s.CountDays, s.AbcentDay, s.RemainDay, s.VoCation3, s.Mokafea, s.SalesCom,
    ISNULL(a.TotalAdvance, 0) AS RuntimeAdvance,
    CONVERT(money, 0) AS RuntimeInsurance,
    CONVERT(money, 0) AS RuntimeVacation3,
    CONVERT(money, 0) AS RuntimePrePaymentValue,
    CONVERT(money, 0) AS RuntimeAbsentDays
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
  AND (@BranchId IS NULL OR e.BranchId = @BranchId)
  AND (@DepartmentId IS NULL OR e.DepartmentID = @DepartmentId)
  AND (@EmployeeId IS NULL OR e.Emp_ID = @EmployeeId)
  AND (@IncludeSavedDrafts = 1 OR s.id IS NULL OR ISNULL(s.payed, 0) = 0)
ORDER BY e.Fullcode, e.Emp_Code, e.Emp_ID;";
                command.Parameters.Add("@Year", SqlDbType.Int).Value = request.Year;
                command.Parameters.Add("@Month", SqlDbType.Int).Value = request.Month;
                command.Parameters.Add("@Sgn", SqlDbType.VarChar, 20).Value = sgn;
                AddNullable(command, "@BranchId", SqlDbType.Int, request.BranchId);
                AddNullable(command, "@DepartmentId", SqlDbType.Int, request.DepartmentId);
                AddNullable(command, "@EmployeeId", SqlDbType.Int, request.EmployeeId);
                command.Parameters.Add("@IncludeSavedDrafts", SqlDbType.Bit).Value = request.IncludeSavedDrafts;

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var salaryRowId = ReadNullableInt(reader, "SalaryRowId");
                        var hasSnapshot = salaryRowId.HasValue;
                        var snapshotTotal1 = ReadDecimal(reader, "total1");
                        var snapshotTotal2 = ReadDecimal(reader, "total2");
                        var snapshotNet = ReadDecimal(reader, "EmpTotalNet");
                        var runtimeAdvance = ReadDecimal(reader, "RuntimeAdvance");
                        var savedAdvance = ReadDecimal(reader, "TotalAdvance");
                        var insurance = ReadDecimal(reader, "ToalInsurance");
                        if (insurance == 0)
                        {
                            insurance = ReadDecimal(reader, "RuntimeInsurance");
                        }

                        var vacation3 = ReadDecimal(reader, "VoCation3");
                        if (vacation3 == 0)
                        {
                            vacation3 = ReadDecimal(reader, "RuntimeVacation3");
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
                            BasicSalary = hasSnapshot ? snapshotTotal1 : ReadDecimal(reader, "Emp_Salary"),
                            TotalBeforeDeductions = hasSnapshot ? snapshotTotal1 : ReadDecimal(reader, "Emp_Salary") + ReadDecimal(reader, "Mokafea") + ReadDecimal(reader, "SalesCom"),
                            AdvanceDeduction = runtimeAdvance == 0 ? savedAdvance : runtimeAdvance,
                            ExistingDiscounts = ReadDecimal(reader, "TotalDiscount"),
                            MedicalInsuranceDeduction = insurance,
                            MedicalInsuranceMonthlyCost = insurance,
                            TotalDeductions = hasSnapshot ? snapshotTotal2 : 0,
                            NetSalary = hasSnapshot ? snapshotNet : 0,
                            ExistingSalaryRowId = salaryRowId,
                            IsApproved = ReadNullableInt(reader, "payed").GetValueOrDefault() == 1,
                            EmployeeAccountCode = ReadString(reader, "Account_code"),
                            AccruedSalaryAccountCode = ReadString(reader, "Account_code1"),
                            VacationProvisionAccountCode = ReadString(reader, "Account_Code2"),
                            AdvancePaymentAccountCode = ReadString(reader, "Account_Code3"),
                            IsLegacySnapshot = hasSnapshot,
                            CountDays = ReadDecimal(reader, "CountDays"),
                            AbsentDays = hasSnapshot ? ReadDecimal(reader, "AbcentDay") : ReadDecimal(reader, "RuntimeAbsentDays"),
                            RemainingDays = ReadDecimal(reader, "RemainDay"),
                            VacationDeduction = vacation3,
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
                var addition = row.Components.Where(x => x.ViewComponent && !x.AddOrDiscount).Sum(x => x.SourceValue);
                var deduction = row.Components.Where(x => x.ViewComponent && x.AddOrDiscount).Sum(x => x.SourceValue);
                row.BasicSalary = addition;
                row.TotalBeforeDeductions = addition + row.VariableAdditions;
                row.TotalDeductions = row.AdvanceDeduction + row.ExistingDiscounts + row.MedicalInsuranceDeduction + row.VacationDeduction + deduction;
                row.NetSalary = row.TotalBeforeDeductions - row.TotalDeductions;
            }
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
                row.RemainingDays = row.CountDays - row.AbsentDays;
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
            var preview = PreviewSalaryRun(request);
            if (preview.Rows.Any(x => !x.IsLegacySnapshot))
            {
                throw new InvalidOperationException("Salary run save is disabled for reconstructed compatibility rows until VB6 component-level parity is approved.");
            }

            var result = new SalaryRunSaveResult();
            var sgn = preview.Request.Year.ToString() + preview.Request.Month.ToString();
            var periodStart = new DateTime(preview.Request.Year, preview.Request.Month, 1);
            var periodEnd = periodStart.AddMonths(1).AddDays(-1);

            using (var connection = OpenConnection())
            using (var transaction = connection.BeginTransaction())
            {
                foreach (var row in preview.Rows)
                {
                    if (row.IsApproved)
                    {
                        continue;
                    }

                    if (row.ExistingSalaryRowId.HasValue)
                    {
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
    BranchId = @BranchId,
    DepartmentID = @DepartmentId,
    RecordDate = GETDATE()
WHERE id = @Id AND ISNULL(payed, 0) = 0;"))
                        {
                            AddSalaryParameters(command, row, preview.Request, sgn);
                            command.Parameters.Add("@Id", SqlDbType.Int).Value = row.ExistingSalaryRowId.Value;
                            result.UpdatedRows += command.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        using (var command = CreateCommand(connection, transaction, @"
INSERT INTO dbo.emp_salary
(emp_id, Emp_Code, Emp_Name, Emp_Salary, total1, TotalAdvance, TotalDiscount, total2, EmpTotalNet, sgn, m_year, m_month, payed, DepartmentID, BranchId, RecordDate)
VALUES
(@EmployeeId, @Code, @Name, @Basic, @TotalBefore, @Advance, @Discounts, @TotalDeductions, @Net, @Sgn, @YearText, @MonthText, 0, @DepartmentId, @BranchId, GETDATE());
SELECT CONVERT(int, SCOPE_IDENTITY());"))
                        {
                            AddSalaryParameters(command, row, preview.Request, sgn);
                            var id = Convert.ToInt32(command.ExecuteScalar());
                            result.InsertedRows++;
                            row.ExistingSalaryRowId = id;
                        }
                    }

                    SaveMedicalDeductionAudit(connection, transaction, row, preview.Request, periodStart, periodEnd, userId);
                    result.TotalNet += row.NetSalary;
                }

                transaction.Commit();
            }

            result.Message = "تم حفظ مسودة المسير. قيد التأمين الطبي يعرض كنصيب موظف ضمن الخصومات، وتكلفة الشركة محفوظة للتقرير دون ترحيل محاسبي تلقائي.";
            return result;
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
SELECT e.Emp_ID, e.Emp_Code, e.Emp_Name, pr.ProviderNameAr, pl.PlanNameAr,
       mi.StartDate, mi.EndDate, mi.IsActive, mi.MonthlyCost,
       mi.EmployeeMonthlyDeduction, mi.CompanyMonthlyCost
FROM dbo.EmployeeMedicalInsurance mi WITH (NOLOCK)
INNER JOIN dbo.TblEmployee e WITH (NOLOCK) ON e.Emp_ID = mi.EmpId
LEFT JOIN dbo.MedicalInsurancePlans pl WITH (NOLOCK) ON pl.PlanId = mi.PlanId
LEFT JOIN dbo.MedicalInsuranceProviders pr WITH (NOLOCK) ON pr.ProviderId = pl.ProviderId
WHERE (@ActiveOnly = 0 OR mi.IsActive = 1)
  AND (@PlanId IS NULL OR mi.PlanId = @PlanId)
  AND (@ProviderId IS NULL OR pl.ProviderId = @ProviderId)
  AND (@From IS NULL OR mi.EndDate IS NULL OR mi.EndDate >= @From)
  AND (@To IS NULL OR mi.StartDate <= @To)
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
                            ProviderName = ReadString(reader, "ProviderNameAr"),
                            PlanName = ReadString(reader, "PlanNameAr"),
                            StartDate = ReadNullableDate(reader, "StartDate"),
                            EndDate = ReadNullableDate(reader, "EndDate"),
                            IsActive = ReadBool(reader, "IsActive"),
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
SELECT e.Emp_ID, e.Emp_Code, e.Emp_Name, pl.PlanNameAr,
       d.PeriodFrom, d.PeriodTo, d.EmployeeDeduction, d.CompanyCost
FROM dbo.PayrollMedicalInsuranceDeduction d WITH (NOLOCK)
INNER JOIN dbo.TblEmployee e WITH (NOLOCK) ON e.Emp_ID = d.EmpId
LEFT JOIN dbo.EmployeeMedicalInsurance mi WITH (NOLOCK) ON mi.Id = d.EmployeeInsuranceId
LEFT JOIN dbo.MedicalInsurancePlans pl WITH (NOLOCK) ON pl.PlanId = mi.PlanId
WHERE (@PlanId IS NULL OR mi.PlanId = @PlanId)
  AND (@ProviderId IS NULL OR pl.ProviderId = @ProviderId)
  AND (@From IS NULL OR d.PeriodTo >= @From)
  AND (@To IS NULL OR d.PeriodFrom <= @To)
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
                            PlanName = ReadString(reader, "PlanNameAr"),
                            PeriodFrom = ReadNullableDate(reader, "PeriodFrom").GetValueOrDefault(),
                            PeriodTo = ReadNullableDate(reader, "PeriodTo").GetValueOrDefault(),
                            EmployeeDeduction = ReadDecimal(reader, "EmployeeDeduction"),
                            CompanyCost = ReadDecimal(reader, "CompanyCost")
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
                    result.Message = "Medical insurance setup is not installed in this database yet. POS shows operational visibility after MainErp setup is deployed.";
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
       mi.CompanyMonthlyCost
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
                    Step = "Employee monthly deduction",
                    DebitAccount = "Employee receivable / Salary deduction",
                    CreditAccount = "Insurance payable",
                    Amount = employeeShare,
                    Explanation = "The employee share is withheld from payroll and moved to the insurance payable balance."
                },
                new MedicalInsuranceAccountingPreviewLine
                {
                    Step = "Company contribution",
                    DebitAccount = "Medical insurance expense",
                    CreditAccount = "Insurance payable",
                    Amount = companyShare,
                    Explanation = "The company share is recognized as an HR benefit expense and accrued to the provider payable."
                },
                new MedicalInsuranceAccountingPreviewLine
                {
                    Step = "Payment to provider",
                    DebitAccount = "Insurance payable",
                    CreditAccount = "Cash / Bank",
                    Amount = payable,
                    Explanation = "When finance pays the insurance provider, the payable is cleared against the selected cash or bank account."
                }
            };
        }

        private static void BuildJournalPreview(SalaryRunPreview preview)
        {
            foreach (var row in preview.Rows)
            {
                if (row.TotalBeforeDeductions > 0)
                {
                    preview.JournalPreview.Add(new SalaryRunJournalLine
                    {
                        AccountCode = row.AccruedSalaryAccountCode,
                        Debit = row.TotalBeforeDeductions,
                        Description = "استحقاق راتب " + row.EmployeeName,
                        BranchId = row.BranchId,
                        DepartmentId = row.DepartmentId,
                        EmployeeId = row.EmployeeId
                    });
                }

                if (row.NetSalary > 0)
                {
                    preview.JournalPreview.Add(new SalaryRunJournalLine
                    {
                        AccountCode = row.AccruedSalaryAccountCode,
                        Credit = row.NetSalary,
                        Description = "صافي راتب مستحق " + row.EmployeeName,
                        BranchId = row.BranchId,
                        DepartmentId = row.DepartmentId,
                        EmployeeId = row.EmployeeId
                    });
                }

                if (row.AdvanceDeduction > 0)
                {
                    preview.JournalPreview.Add(new SalaryRunJournalLine
                    {
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
                        AccountCode = row.EmployeeAccountCode,
                        Credit = row.MedicalInsuranceDeduction,
                        Description = "خصم التأمين الطبي - نصيب الموظف " + row.EmployeeName,
                        BranchId = row.BranchId,
                        DepartmentId = row.DepartmentId,
                        EmployeeId = row.EmployeeId
                    });
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
            if (!TableExists(connection, "EmployeeMedicalInsurance"))
            {
                throw new InvalidOperationException("Employee medical insurance tables are not installed in the current database.");
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

            if (!insurance.Id.HasValue || insurance.Id.Value <= 0)
            {
                insurance.Id = FindMatchingEmployeeMedicalInsuranceId(connection, transaction, insurance, employeeId);
            }

            var calculation = CalculateMedicalInsurance(insurance.MonthlyCost, insurance.EmployeeShareType, insurance.EmployeeShareValue, insurance.CompanyShareType, insurance.CompanyShareValue);
            insurance.EmployeeMonthlyDeduction = calculation.EmployeeDeduction;
            insurance.CompanyMonthlyCost = calculation.CompanyCost;

            if (insurance.Id.HasValue && insurance.Id.Value > 0)
            {
                using (var command = CreateCommand(connection, transaction, @"
UPDATE dbo.EmployeeMedicalInsurance
SET PlanId = @PlanId,
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
                    command.ExecuteNonQuery();
                }
            }
            else if (insurance.IsActive || insurance.PlanId.HasValue || insurance.MonthlyCost > 0)
            {
                if (insurance.IsActive)
                {
                    using (var command = CreateCommand(connection, transaction, @"
UPDATE dbo.EmployeeMedicalInsurance
SET IsActive = 0, EndDate = ISNULL(EndDate, DATEADD(day, -1, @StartDate)), UpdatedAt = GETDATE(), UpdatedBy = @UserId
WHERE EmpId = @EmpId AND IsActive = 1;"))
                    {
                        command.Parameters.Add("@EmpId", SqlDbType.Int).Value = employeeId;
                        command.Parameters.Add("@StartDate", SqlDbType.DateTime).Value = insurance.StartDate.Value;
                        command.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
                        command.ExecuteNonQuery();
                    }
                }

                using (var command = CreateCommand(connection, transaction, @"
INSERT INTO dbo.EmployeeMedicalInsurance
(EmpId, PlanId, StartDate, EndDate, IsMonthly, IsActive,
 MonthlyCost, EmployeeShareType, EmployeeShareValue,
 CompanyShareType, CompanyShareValue, EmployeeMonthlyDeduction, CompanyMonthlyCost,
 Amount, PercentValue, DeductionType, Notes, CreatedBy, CreatedAt)
VALUES
(@EmpId, @PlanId, @StartDate, @EndDate, @IsMonthly, @IsActive,
 @MonthlyCost, @EmployeeShareType, @EmployeeShareValue,
 @CompanyShareType, @CompanyShareValue, @EmployeeMonthlyDeduction, @CompanyMonthlyCost,
 @EmployeeShareValue, CASE WHEN @EmployeeShareType = N'Percent' THEN @EmployeeShareValue ELSE 0 END, @EmployeeShareType, @Notes, @UserId, GETDATE());"))
                {
                    AddInsuranceParameters(command, insurance, employeeId, userId);
                    command.ExecuteNonQuery();
                }
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
                throw new InvalidOperationException(label + " share values cannot be negative.");
            }

            if (string.Equals(shareType, "Percent", StringComparison.OrdinalIgnoreCase) && shareValue > 100)
            {
                throw new InvalidOperationException(label + " percent cannot exceed 100.");
            }
        }

        private static void ValidatePlanLifecycle(MedicalInsurancePlan plan)
        {
            if (plan.StartDate.HasValue && plan.EndDate.HasValue && plan.EndDate.Value.Date < plan.StartDate.Value.Date)
            {
                throw new InvalidOperationException("Plan end date cannot be before start date.");
            }

            if (plan.MaxDependents < 0 || plan.ChildrenMaxAge < 0)
            {
                throw new InvalidOperationException("Dependent rules cannot contain negative values.");
            }

            if (plan.DefaultCoveragePercent < 0 || plan.DefaultCoveragePercent > 100)
            {
                throw new InvalidOperationException("Coverage percent must be between 0 and 100.");
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
            AddNullable(command, "@Notes", SqlDbType.NVarChar, request.Notes);
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

        private static void AddInsuranceParameters(SqlCommand command, EmployeeMedicalInsurance insurance, int employeeId, int userId)
        {
            command.Parameters.Add("@EmpId", SqlDbType.Int).Value = employeeId;
            AddNullable(command, "@PlanId", SqlDbType.Int, insurance.PlanId);
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
            command.Parameters.Add("@ActiveOnly", SqlDbType.Bit).Value = filter.ActiveOnly;
            AddNullable(command, "@ProviderId", SqlDbType.Int, filter.ProviderId);
            AddNullable(command, "@PlanId", SqlDbType.Int, filter.PlanId);
            AddNullable(command, "@From", SqlDbType.DateTime, filter.PeriodFrom);
            AddNullable(command, "@To", SqlDbType.DateTime, filter.PeriodTo);
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

