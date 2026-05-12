using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

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
                FillLookup(connection, "SELECT ProviderId, ProviderNameAr FROM dbo.MedicalInsuranceProviders WITH (NOLOCK) WHERE IsActive = 1 ORDER BY ProviderNameAr", result.MedicalInsuranceProviders);
                FillLookup(connection, "SELECT PlanId, PlanNameAr FROM dbo.MedicalInsurancePlans WITH (NOLOCK) WHERE IsActive = 1 ORDER BY PlanNameAr", result.MedicalInsurancePlans);
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

        public SalaryRunPreview PreviewSalaryRun(SalaryRunRequest request)
        {
            request = NormalizeSalaryRequest(request);
            var preview = new SalaryRunPreview { Request = request };
            var start = new DateTime(request.Year, request.Month, 1);
            var end = start.AddMonths(1).AddDays(-1);
            var sgn = request.Year.ToString() + request.Month.ToString();

            using (var connection = OpenConnection())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = @"
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
            return preview;
        }

        public SalaryRunSaveResult SaveSalaryRun(SalaryRunRequest request, int userId)
        {
            var preview = PreviewSalaryRun(request);
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
    }
}
