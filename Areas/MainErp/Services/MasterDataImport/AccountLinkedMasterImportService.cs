using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using MyERP.Areas.MainErp.Infrastructure;
using MyERP.Areas.MainErp.Interfaces;
using MyERP.Areas.MainErp.Models.Security;
using MyERP.Areas.MainErp.Repositories.Customers;
using MyERP.Areas.MainErp.ViewModels.Customers;
using MyERP.Areas.MainErp.ViewModels.MasterDataImport;
using MyERP.Common.EmployeePayroll;

namespace MyERP.Areas.MainErp.Services.MasterDataImport
{
    public class AccountLinkedMasterImportService
    {
        private readonly IMainErpDbConnectionFactory _connectionFactory;

        public AccountLinkedMasterImportService(IMainErpDbConnectionFactory connectionFactory)
        {
            if (connectionFactory == null)
            {
                throw new ArgumentNullException("connectionFactory");
            }

            _connectionFactory = connectionFactory;
        }

        public IList<MasterDataImportRowViewModel> Validate(string entityType, IList<MasterDataImportRowViewModel> rows)
        {
            var bySerial = rows.Where(r => !string.IsNullOrWhiteSpace(r.AccountSerial))
                .GroupBy(r => Clean(r.AccountSerial), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);
            var byName = rows.Where(r => !string.IsNullOrWhiteSpace(r.EntityName ?? r.AccountName))
                .GroupBy(r => Clean(r.EntityName ?? r.AccountName), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

            using (var connection = _connectionFactory.CreateOpenConnection())
            {
                foreach (var row in rows)
                {
                    row.Errors.Clear();
                    row.EntityType = entityType;
                    row.AccountSerial = Clean(row.AccountSerial);
                    row.EntityCode = Clean(string.IsNullOrWhiteSpace(row.EntityCode) ? row.AccountSerial : row.EntityCode);
                    row.EntityName = Clean(string.IsNullOrWhiteSpace(row.EntityName) ? row.AccountName : row.EntityName);
                    row.AccountName = row.EntityName;

                    if (string.IsNullOrWhiteSpace(row.EntityName))
                    {
                        row.Errors.Add("Name is required.");
                    }

                    if (string.IsNullOrWhiteSpace(row.AccountSerial))
                    {
                        row.Errors.Add("Account serial/code is required.");
                    }
                    else if (bySerial[row.AccountSerial].Count > 1)
                    {
                        row.Errors.Add("Duplicate account serial inside Excel file.");
                    }

                    if (!string.IsNullOrWhiteSpace(row.EntityName) && byName[row.EntityName].Count > 1)
                    {
                        row.Errors.Add("Duplicate name inside Excel file.");
                    }

                    row.AccountCode = LookupAccountCode(connection, row.AccountSerial);
                    if (string.IsNullOrWhiteSpace(row.AccountCode))
                    {
                        row.Errors.Add("Account serial was not found in ACCOUNTS. Import Chart of Accounts first.");
                    }

                    var duplicate = FindExistingEntity(connection, entityType, row.EntityName, row.AccountCode, row.EntityCode);
                    if (!string.IsNullOrWhiteSpace(duplicate))
                    {
                        row.Errors.Add(duplicate);
                    }
                }
            }

            return rows;
        }

        public MasterDataImportResultViewModel Import(MasterDataImportPreview preview, MainErpUserContext user, bool stopOnAnyError)
        {
            var rows = Validate(preview.EntityType, preview.Rows);
            if (stopOnAnyError && rows.Any(r => !r.IsValid))
            {
                return new MasterDataImportResultViewModel
                {
                    TotalRows = rows.Count,
                    SuccessRows = 0,
                    FailedRows = rows.Count(r => !r.IsValid),
                    Message = "Import stopped because validation errors exist."
                };
            }

            var success = 0;
            foreach (var row in rows.Where(r => r.IsValid).OrderBy(r => r.RowNumber))
            {
                try
                {
                    if (preview.EntityType == MasterDataImportEntityType.Employees)
                    {
                        row.ImportedEntityId = SaveEmployee(row, user);
                    }
                    else
                    {
                        row.ImportedEntityId = SaveCustomerOrSupplier(preview.EntityType, row, user);
                    }

                    success++;
                }
                catch (Exception ex)
                {
                    row.Errors.Add(ex.Message);
                    if (stopOnAnyError)
                    {
                        throw;
                    }
                }
            }

            return new MasterDataImportResultViewModel
            {
                TotalRows = rows.Count,
                SuccessRows = success,
                FailedRows = rows.Count - success,
                Message = "Import completed using the existing MainERP save logic."
            };
        }

        private int? SaveCustomerOrSupplier(string entityType, MasterDataImportRowViewModel row, MainErpUserContext user)
        {
            var repository = new CustomerRepository(_connectionFactory);
            var request = repository.New();
            request.CusName = row.EntityName;
            request.CusNameEnglish = row.EntityName;
            request.Type = entityType == MasterDataImportEntityType.Suppliers ? 2 : 1;
            request.Code = row.EntityCode;
            request.FullCode = row.EntityCode;
            request.AccountCode = row.AccountCode;
            request.OpenBalance = row.OpeningBalance.HasValue ? Convert.ToDouble(row.OpeningBalance.Value, CultureInfo.InvariantCulture) : (double?)null;
            request.OpenBalanceType = row.OpeningBalanceType;
            request.OpenBalanceDate = DateTime.Today;

            var result = repository.Save(request, user);
            if (!result.Success)
            {
                throw new InvalidOperationException(result.Message);
            }

            return result.CusId;
        }

        private int? SaveEmployee(MasterDataImportRowViewModel row, MainErpUserContext user)
        {
            var repository = new EmployeePayrollRepository(GetMainErpConnectionString());
            var request = new EmployeeSaveRequest
            {
                EmployeeCode = row.EntityCode,
                EmployeeName = row.EntityName,
                HiringDate = DateTime.Today,
                IsActive = true,
                BasicSalary = 0,
                AccountCode = row.AccountCode
            };

            return repository.SaveEmployee(request, user == null ? 0 : user.UserId);
        }

        private static string GetMainErpConnectionString()
        {
            return MainErpDbConnectionFactory.ResolveActiveConnectionString();
        }

        private static string LookupAccountCode(SqlConnection connection, string serial)
        {
            using (var command = new SqlCommand("SELECT TOP (1) Account_Code FROM dbo.ACCOUNTS WHERE Account_Serial = @Serial", connection))
            {
                command.Parameters.Add("@Serial", SqlDbType.NVarChar, 4000).Value = serial;
                return Convert.ToString(command.ExecuteScalar());
            }
        }

        private static string FindExistingEntity(SqlConnection connection, string entityType, string name, string accountCode, string code)
        {
            if (entityType == MasterDataImportEntityType.Employees)
            {
                var emp = Exists(connection, "SELECT COUNT(1) FROM dbo.TblEmployee WHERE LTRIM(RTRIM(Emp_Name)) = @Name OR LTRIM(RTRIM(Emp_Code)) = @Code OR Account_code = @AccountCode", name, code, accountCode);
                return emp ? "Employee already exists by name, code, or account." : null;
            }

            var type = entityType == MasterDataImportEntityType.Suppliers ? 2 : 1;
            var exists = Exists(connection, "SELECT COUNT(1) FROM dbo.TblCustemers WHERE [Type] = @Type AND (LTRIM(RTRIM(CusName)) = @Name OR LTRIM(RTRIM(Fullcode)) = @Code OR Account_Code = @AccountCode)", name, code, accountCode, type);
            return exists ? "Customer/supplier already exists by name, code, or account." : null;
        }

        private static bool Exists(SqlConnection connection, string sql, string name, string code, string accountCode, int? type = null)
        {
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.Add("@Name", SqlDbType.NVarChar, 4000).Value = (object)name ?? DBNull.Value;
                command.Parameters.Add("@Code", SqlDbType.NVarChar, 255).Value = (object)code ?? DBNull.Value;
                command.Parameters.Add("@AccountCode", SqlDbType.NVarChar, 50).Value = (object)accountCode ?? DBNull.Value;
                if (type.HasValue)
                {
                    command.Parameters.Add("@Type", SqlDbType.Int).Value = type.Value;
                }

                return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture) > 0;
            }
        }

        private static string Clean(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().Trim('\u200f', '\u200e');
        }
    }
}
