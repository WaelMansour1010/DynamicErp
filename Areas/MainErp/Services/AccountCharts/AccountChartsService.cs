using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using MyERP.Areas.MainErp.Interfaces;
using MyERP.Areas.MainErp.ViewModels.AccountCharts;

namespace MyERP.Areas.MainErp.Services.AccountCharts
{
    public class AccountChartsService
    {
        private readonly IMainErpDbConnectionFactory _connectionFactory;

        public AccountChartsService(IMainErpDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public AccountChartsIndexViewModel GetIndexModel(AccountChartsPermissionsViewModel permissions)
        {
            using (var connection = _connectionFactory.CreateOpenConnection())
            {
                var model = new AccountChartsIndexViewModel
                {
                    Permissions = permissions ?? new AccountChartsPermissionsViewModel()
                };

                model.TotalAccounts = ExecuteScalar<int>(connection, null, "SELECT COUNT(1) FROM dbo.ACCOUNTS WHERE Account_Code <> N'r'");
                model.Currencies = GetLookup(connection, "SELECT CONVERT(NVARCHAR(50), id), ISNULL(code, name) FROM dbo.currency ORDER BY id");
                model.CostCenters = GetLookup(connection, "SELECT CONVERT(NVARCHAR(50), code), account_name FROM dbo.markaas_taklefa WHERE [level] = 3 AND account_no IS NOT NULL ORDER BY account_name");
                model.ActivityTypes = GetLookup(connection, "SELECT CONVERT(NVARCHAR(50), id), name FROM dbo.tblActivitesType ORDER BY name");
                model.Branches = GetLookup(connection, "SELECT CONVERT(NVARCHAR(50), branch_id), branch_name FROM dbo.TblBranchesData ORDER BY branch_name");
                model.Users = GetLookup(connection, "SELECT CONVERT(NVARCHAR(50), UserID), UserName FROM dbo.TblUsers ORDER BY UserName");
                model.Groups = GetLookup(connection, "SELECT CONVERT(NVARCHAR(50), GroupID), GroupName FROM dbo.Groups ORDER BY GroupName");

                return model;
            }
        }

        public IList<AccountTreeNodeViewModel> GetTree()
        {
            using (var connection = _connectionFactory.CreateOpenConnection())
            {
                return GetTree(connection);
            }
        }

        public AccountDetailsViewModel GetAccount(string accountCode)
        {
            using (var connection = _connectionFactory.CreateOpenConnection())
            {
                return GetAccount(connection, accountCode);
            }
        }

        public AccountSaveResult Save(AccountSaveRequest request, bool isCreate)
        {
            if (request == null)
            {
                return Fail("لم تصل بيانات الحساب.");
            }

            request.AccountName = (request.AccountName ?? string.Empty).Trim();
            request.AccountNameEnglish = (request.AccountNameEnglish ?? string.Empty).Trim();
            request.AccountSerial = (request.AccountSerial ?? string.Empty).Trim();
            request.ParentAccountCode = (request.ParentAccountCode ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(request.AccountName))
            {
                return Fail("يجب كتابة اسم الحساب.");
            }

            if (string.IsNullOrWhiteSpace(request.AccountNameEnglish))
            {
                request.AccountNameEnglish = request.AccountName;
            }

            if (isCreate && string.IsNullOrWhiteSpace(request.ParentAccountCode))
            {
                return Fail("يجب اختيار الحساب الأب.");
            }

            if (request.HasCostCenter && request.CostCenterType == 1 && string.IsNullOrWhiteSpace(request.CostCenterId))
            {
                return Fail("يجب اختيار مركز التكلفة عند تحديد نوع مركز محدد.");
            }

            using (var connection = _connectionFactory.CreateOpenConnection())
            using (var transaction = connection.BeginTransaction(IsolationLevel.Serializable))
            {
                try
                {
                    if (isCreate)
                    {
                        var parent = GetAccount(connection, request.ParentAccountCode, transaction);
                        if (parent == null)
                        {
                            transaction.Rollback();
                            return Fail("الحساب الأب غير موجود.");
                        }

                        if (parent.IsLastAccount)
                        {
                            transaction.Rollback();
                            return Fail("لا يمكن إنشاء حساب تحت حساب نهائي.");
                        }

                        if (string.IsNullOrWhiteSpace(request.AccountSerial))
                        {
                            request.AccountSerial = GenerateAccountSerial(connection, transaction, request.ParentAccountCode);
                        }

                        var duplicate = ExecuteScalar<int>(connection, transaction,
                            "SELECT COUNT(1) FROM dbo.ACCOUNTS WHERE Account_Serial = @serial",
                            new SqlParameter("@serial", SqlDbType.NVarChar, 4000) { Value = request.AccountSerial });
                        if (duplicate > 0)
                        {
                            transaction.Rollback();
                            return Fail("رقم الحساب موجود بالفعل.");
                        }

                        var newCode = GenerateAccountCode(connection, transaction, request.ParentAccountCode);
                        InsertAccount(connection, transaction, request, newCode);
                        ReplaceAssignments(connection, transaction, newCode, request.BranchIds, request.UserIds);
                        transaction.Commit();

                        return Ok("تم حفظ الحساب بنجاح.", newCode, GetAccount(newCode));
                    }

                    if (string.IsNullOrWhiteSpace(request.AccountCode))
                    {
                        transaction.Rollback();
                        return Fail("لم يتم تحديد الحساب المطلوب تعديله.");
                    }

                    var current = GetAccount(connection, request.AccountCode, transaction);
                    if (current == null)
                    {
                        transaction.Rollback();
                        return Fail("الحساب غير موجود.");
                    }

                    if (current.IsLastAccount == false && request.IsLastAccount && HasChildren(connection, transaction, request.AccountCode))
                    {
                        transaction.Rollback();
                        return Fail("هذا الحساب رئيسي ويحتوي على حسابات فرعية، ولا يمكن تحويله إلى حساب نهائي.");
                    }

                    if (!string.IsNullOrWhiteSpace(request.AccountSerial))
                    {
                        var duplicate = ExecuteScalar<int>(connection, transaction,
                            "SELECT COUNT(1) FROM dbo.ACCOUNTS WHERE Account_Serial = @serial AND Account_Code <> @code",
                            new SqlParameter("@serial", SqlDbType.NVarChar, 4000) { Value = request.AccountSerial },
                            new SqlParameter("@code", SqlDbType.NVarChar, 50) { Value = request.AccountCode });
                        if (duplicate > 0)
                        {
                            transaction.Rollback();
                            return Fail("رقم الحساب موجود بالفعل.");
                        }
                    }

                    UpdateAccount(connection, transaction, request);
                    ReplaceAssignments(connection, transaction, request.AccountCode, request.BranchIds, request.UserIds);
                    transaction.Commit();

                    return Ok("تم حفظ التعديلات بنجاح.", request.AccountCode, GetAccount(request.AccountCode));
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return Fail("تعذر حفظ الحساب: " + ex.Message);
                }
            }
        }

        public AccountSaveResult Delete(string accountCode)
        {
            if (string.IsNullOrWhiteSpace(accountCode))
            {
                return Fail("لم يتم تحديد الحساب المطلوب حذفه.");
            }

            using (var connection = _connectionFactory.CreateOpenConnection())
            using (var transaction = connection.BeginTransaction(IsolationLevel.Serializable))
            {
                try
                {
                    var account = GetAccount(connection, accountCode, transaction);
                    if (account == null)
                    {
                        transaction.Rollback();
                        return Fail("الحساب غير موجود.");
                    }

                    var reason = GetDeleteBlockReason(connection, transaction, accountCode);
                    if (!string.IsNullOrWhiteSpace(reason))
                    {
                        transaction.Rollback();
                        return Fail(reason);
                    }

                    ExecuteNonQuery(connection, transaction, "DELETE FROM dbo.TblAccountBranch WHERE Account_Code = @code", new SqlParameter("@code", SqlDbType.NVarChar, 55) { Value = accountCode });
                    ExecuteNonQuery(connection, transaction, "DELETE FROM dbo.TblAccountUser WHERE Account_Code = @code", new SqlParameter("@code", SqlDbType.NVarChar, 55) { Value = accountCode });
                    ExecuteNonQuery(connection, transaction, "DELETE FROM dbo.ACCOUNTS WHERE Account_Code = @code", new SqlParameter("@code", SqlDbType.NVarChar, 50) { Value = accountCode });
                    transaction.Commit();
                    return Ok("تم حذف الحساب بنجاح.", accountCode, null);
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return Fail("تعذر حذف الحساب: " + ex.Message);
                }
            }
        }

        private IList<AccountTreeNodeViewModel> GetTree(SqlConnection connection)
        {
            const string sql = @"
SELECT Account_Code, Parent_Account_Code, Account_Serial, Account_Name, Account_NameEng,
       last_account, ISNULL([Level], LEN(Account_Code) - LEN(REPLACE(Account_Code, 'a', ''))) AS [Level],
       ISNULL([Block], 0) AS [Block]
FROM dbo.ACCOUNTS
ORDER BY CASE WHEN Account_Code = N'r' THEN 0 ELSE 1 END, Account_Serial, Account_ID;";

            var items = new List<AccountTreeNodeViewModel>();
            using (var command = new SqlCommand(sql, connection))
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    items.Add(new AccountTreeNodeViewModel
                    {
                        AccountCode = ReadString(reader, "Account_Code"),
                        ParentAccountCode = ReadString(reader, "Parent_Account_Code"),
                        AccountSerial = ReadString(reader, "Account_Serial"),
                        AccountName = ReadString(reader, "Account_Name"),
                        AccountNameEnglish = ReadString(reader, "Account_NameEng"),
                        IsLastAccount = ReadBool(reader, "last_account"),
                        Level = ReadInt(reader, "Level"),
                        IsBlocked = ReadBool(reader, "Block")
                    });
                }
            }

            return items;
        }

        private AccountDetailsViewModel GetAccount(SqlConnection connection, string accountCode, SqlTransaction transaction = null)
        {
            const string sql = @"
SELECT TOP (1) *
FROM dbo.ACCOUNTS
WHERE Account_Code = @code;";

            using (var command = new SqlCommand(sql, connection, transaction))
            {
                command.Parameters.Add("@code", SqlDbType.NVarChar, 50).Value = accountCode ?? string.Empty;
                using (var reader = command.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        return null;
                    }

                    var account = new AccountDetailsViewModel
                    {
                        AccountId = ReadInt(reader, "Account_ID"),
                        AccountCode = ReadString(reader, "Account_Code"),
                        ParentAccountCode = ReadString(reader, "Parent_Account_Code"),
                        AccountSerial = ReadString(reader, "Account_Serial"),
                        AccountName = ReadString(reader, "Account_Name"),
                        AccountNameEnglish = ReadString(reader, "Account_NameEng"),
                        IsLastAccount = ReadBool(reader, "last_account"),
                        HasBudget = ReadBool(reader, "mowazna"),
                        HasCostCenter = ReadBool(reader, "cost_center"),
                        IsSummaryAccount = ReadBool(reader, "Sum_account"),
                        IsBlocked = ReadBool(reader, "Block"),
                        CurrencyCode = ReadString(reader, "currenct_code"),
                        CostCenterId = ReadString(reader, "cost_center_id"),
                        CostCenterType = ReadInt(reader, "cost_center_type"),
                        ActivityTypeId = ReadNullableInt(reader, "ActivityTypeId"),
                        AccountTypes = ReadNullableInt(reader, "AccountTypes"),
                        AccountTab = ReadNullableInt(reader, "AccountTab"),
                        DebitOrCredit = ReadNullableInt(reader, "DepitOrCredit"),
                        DifferentType = ReadNullableInt(reader, "Differenttype"),
                        Authority = ReadNullableInt(reader, "Authority"),
                        UserGroupId = ReadNullableInt(reader, "UserGroupId"),
                        UserId = ReadNullableInt(reader, "UserId"),
                        DateCreated = ReadNullableDate(reader, "DateCreated"),
                        Level = ReadNullableInt(reader, "Level").GetValueOrDefault(CountA(ReadString(reader, "Account_Code")))
                    };

                    reader.Close();
                    account.BranchIds = GetIntList(connection, transaction, "SELECT BranchID FROM dbo.TblAccountBranch WHERE Account_Code = @code", accountCode);
                    account.UserIds = GetIntList(connection, transaction, "SELECT UserID FROM dbo.TblAccountUser WHERE Account_Code = @code", accountCode);
                    account.DeleteBlockReason = GetDeleteBlockReason(connection, transaction, accountCode);
                    account.CanDeleteSafely = string.IsNullOrWhiteSpace(account.DeleteBlockReason);
                    return account;
                }
            }
        }

        private void InsertAccount(SqlConnection connection, SqlTransaction transaction, AccountSaveRequest request, string accountCode)
        {
            const string sql = @"
INSERT INTO dbo.ACCOUNTS
(Account_Code, Account_Name, Parent_Account_Code, last_account, cannot_del, Branch, Account_Serial,
 BasicAccount, DateCreated, Account_NameEng, currenct_code, mowazna, cost_center, Sum_account,
 cost_center_type, cost_center_id, ActivityTypeId, AccountTypes, AccountTab, DepitOrCredit,
 Differenttype, Authority, UserGroupId, Userid, [Block])
VALUES
(@Account_Code, @Account_Name, @Parent_Account_Code, @last_account, 0, @Branch, @Account_Serial,
 0, GETDATE(), @Account_NameEng, @currenct_code, @mowazna, @cost_center, @Sum_account,
 @cost_center_type, @cost_center_id, @ActivityTypeId, @AccountTypes, @AccountTab, @DepitOrCredit,
 @Differenttype, @Authority, @UserGroupId, @Userid, @Block);";

            ExecuteNonQuery(connection, transaction, sql, BuildSaveParameters(request, accountCode).ToArray());
        }

        private void UpdateAccount(SqlConnection connection, SqlTransaction transaction, AccountSaveRequest request)
        {
            const string sql = @"
UPDATE dbo.ACCOUNTS
SET Account_Name = @Account_Name,
    Account_NameEng = @Account_NameEng,
    Account_Serial = @Account_Serial,
    last_account = @last_account,
    mowazna = @mowazna,
    cost_center = @cost_center,
    currenct_code = @currenct_code,
    Sum_account = @Sum_account,
    cost_center_type = @cost_center_type,
    cost_center_id = @cost_center_id,
    ActivityTypeId = @ActivityTypeId,
    AccountTypes = @AccountTypes,
    AccountTab = @AccountTab,
    DepitOrCredit = @DepitOrCredit,
    Differenttype = @Differenttype,
    Authority = @Authority,
    UserGroupId = @UserGroupId,
    Userid = @Userid,
    [Block] = @Block
WHERE Account_Code = @Account_Code;";

            ExecuteNonQuery(connection, transaction, sql, BuildSaveParameters(request, request.AccountCode).ToArray());
        }

        private List<SqlParameter> BuildSaveParameters(AccountSaveRequest request, string accountCode)
        {
            return new List<SqlParameter>
            {
                new SqlParameter("@Account_Code", SqlDbType.NVarChar, 50) { Value = accountCode },
                new SqlParameter("@Account_Name", SqlDbType.NVarChar, 4000) { Value = request.AccountName },
                new SqlParameter("@Parent_Account_Code", SqlDbType.NVarChar, 70) { Value = (object)request.ParentAccountCode ?? DBNull.Value },
                new SqlParameter("@last_account", SqlDbType.Bit) { Value = request.IsLastAccount },
                new SqlParameter("@Branch", SqlDbType.VarChar, 50) { Value = "0" },
                new SqlParameter("@Account_Serial", SqlDbType.NVarChar, 4000) { Value = request.AccountSerial },
                new SqlParameter("@Account_NameEng", SqlDbType.NVarChar, 4000) { Value = request.AccountNameEnglish },
                new SqlParameter("@currenct_code", SqlDbType.NVarChar, 50) { Value = string.IsNullOrWhiteSpace(request.CurrencyCode) ? (object)"1" : request.CurrencyCode },
                new SqlParameter("@mowazna", SqlDbType.Bit) { Value = request.HasBudget },
                new SqlParameter("@cost_center", SqlDbType.Bit) { Value = request.HasCostCenter },
                new SqlParameter("@Sum_account", SqlDbType.Bit) { Value = request.IsSummaryAccount },
                new SqlParameter("@cost_center_type", SqlDbType.Int) { Value = request.CostCenterType },
                new SqlParameter("@cost_center_id", SqlDbType.NVarChar, 255) { Value = string.IsNullOrWhiteSpace(request.CostCenterId) ? (object)DBNull.Value : request.CostCenterId },
                new SqlParameter("@ActivityTypeId", SqlDbType.Int) { Value = (object)request.ActivityTypeId ?? DBNull.Value },
                new SqlParameter("@AccountTypes", SqlDbType.Int) { Value = (object)request.AccountTypes ?? DBNull.Value },
                new SqlParameter("@AccountTab", SqlDbType.Int) { Value = (object)request.AccountTab ?? DBNull.Value },
                new SqlParameter("@DepitOrCredit", SqlDbType.Int) { Value = (object)request.DebitOrCredit ?? DBNull.Value },
                new SqlParameter("@Differenttype", SqlDbType.Int) { Value = (object)request.DifferentType ?? DBNull.Value },
                new SqlParameter("@Authority", SqlDbType.Int) { Value = (object)request.Authority ?? DBNull.Value },
                new SqlParameter("@UserGroupId", SqlDbType.Int) { Value = (object)request.UserGroupId ?? DBNull.Value },
                new SqlParameter("@Userid", SqlDbType.Int) { Value = (object)request.UserId ?? DBNull.Value },
                new SqlParameter("@Block", SqlDbType.Bit) { Value = request.IsBlocked }
            };
        }

        private void ReplaceAssignments(SqlConnection connection, SqlTransaction transaction, string accountCode, int[] branchIds, int[] userIds)
        {
            ExecuteNonQuery(connection, transaction, "DELETE FROM dbo.TblAccountBranch WHERE Account_Code = @code", new SqlParameter("@code", SqlDbType.NVarChar, 55) { Value = accountCode });
            ExecuteNonQuery(connection, transaction, "DELETE FROM dbo.TblAccountUser WHERE Account_Code = @code", new SqlParameter("@code", SqlDbType.NVarChar, 55) { Value = accountCode });

            foreach (var branchId in (branchIds ?? new int[0]).Distinct())
            {
                ExecuteNonQuery(connection, transaction,
                    "INSERT INTO dbo.TblAccountBranch (BranchID, Account_Code) VALUES (@id, @code)",
                    new SqlParameter("@id", SqlDbType.Int) { Value = branchId },
                    new SqlParameter("@code", SqlDbType.NVarChar, 55) { Value = accountCode });
            }

            foreach (var userId in (userIds ?? new int[0]).Distinct())
            {
                ExecuteNonQuery(connection, transaction,
                    "INSERT INTO dbo.TblAccountUser (UserID, Account_Code) VALUES (@id, @code)",
                    new SqlParameter("@id", SqlDbType.Int) { Value = userId },
                    new SqlParameter("@code", SqlDbType.NVarChar, 55) { Value = accountCode });
            }
        }

        private string GenerateAccountCode(SqlConnection connection, SqlTransaction transaction, string parentAccountCode)
        {
            var maxSuffix = 0;
            using (var command = new SqlCommand("SELECT Account_Code FROM dbo.ACCOUNTS WHERE Parent_Account_Code = @parent ORDER BY Account_ID", connection, transaction))
            {
                command.Parameters.Add("@parent", SqlDbType.NVarChar, 70).Value = parentAccountCode;
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var code = ReadString(reader, "Account_Code");
                        var lastA = code.LastIndexOf('a');
                        int suffix;
                        if (lastA >= 0 && int.TryParse(code.Substring(lastA + 1), out suffix) && suffix > maxSuffix)
                        {
                            maxSuffix = suffix;
                        }
                    }
                }
            }

            var next = maxSuffix + 1;
            while (true)
            {
                var candidate = parentAccountCode + "a" + next.ToString(CultureInfo.InvariantCulture);
                var exists = ExecuteScalar<int>(connection, transaction, "SELECT COUNT(1) FROM dbo.ACCOUNTS WHERE Account_Code = @code", new SqlParameter("@code", SqlDbType.NVarChar, 50) { Value = candidate });
                if (exists == 0)
                {
                    return candidate;
                }

                next++;
            }
        }

        private string GenerateAccountSerial(SqlConnection connection, SqlTransaction transaction, string parentAccountCode)
        {
            var parentSerial = ExecuteScalar<string>(connection, transaction,
                "SELECT TOP (1) Account_Serial FROM dbo.ACCOUNTS WHERE Account_Code = @code",
                new SqlParameter("@code", SqlDbType.NVarChar, 50) { Value = parentAccountCode }) ?? string.Empty;

            var level = CountA(parentAccountCode) + 1;
            var digits = ExecuteScalar<int>(connection, transaction,
                "SELECT TOP (1) ISNULL(NoOfDigits, 1) FROM dbo.AccountsLevelsDetails WHERE [Level] = @level ORDER BY id",
                new SqlParameter("@level", SqlDbType.Int) { Value = level });
            if (digits <= 0)
            {
                digits = 1;
            }

            var maxSerial = ExecuteScalar<string>(connection, transaction,
                "SELECT MAX(Account_Serial) FROM dbo.ACCOUNTS WHERE Account_Serial LIKE @prefix AND LEN(Account_Code) - LEN(REPLACE(Account_Code, 'a', '')) = @level",
                new SqlParameter("@prefix", SqlDbType.NVarChar, 4000) { Value = parentSerial + "%" },
                new SqlParameter("@level", SqlDbType.Int) { Value = level });

            var maxNumber = ExtractTrailingNumber(maxSerial, parentSerial);
            return parentSerial + (maxNumber + 1).ToString(new string('0', digits), CultureInfo.InvariantCulture);
        }

        private string GetDeleteBlockReason(SqlConnection connection, SqlTransaction transaction, string accountCode)
        {
            if (HasChildren(connection, transaction, accountCode))
            {
                return "لا يمكن حذف هذا الحساب لأنه يحتوي على حسابات فرعية.";
            }

            var flags = ExecuteScalar<int>(connection, transaction,
                "SELECT COUNT(1) FROM dbo.ACCOUNTS WHERE Account_Code = @code AND (ISNULL(cannot_del, 0) = 1 OR ISNULL(BasicAccount, 0) = 1)",
                new SqlParameter("@code", SqlDbType.NVarChar, 50) { Value = accountCode });
            if (flags > 0)
            {
                return "لا يمكن حذف الحساب لأنه حساب أساسي أو ممنوع الحذف.";
            }

            var entryCount = ExecuteScalar<int>(connection, transaction,
                "SELECT (SELECT COUNT(1) FROM dbo.DOUBLE_ENTREY_VOUCHERS WHERE Account_Code = @code) + (SELECT COUNT(1) FROM dbo.DOUBLE_ENTREY_VOUCHERS1 WHERE Account_Code = @code)",
                new SqlParameter("@code", SqlDbType.NVarChar, 50) { Value = accountCode });
            if (entryCount > 0)
            {
                return "لا يمكن حذف الحساب لأنه مستخدم في قيود أو حركات.";
            }

            var reference = FindAutoReference(connection, transaction, accountCode);
            if (!string.IsNullOrWhiteSpace(reference))
            {
                return "لا يمكن حذف الحساب لأنه مرتبط ببيانات النظام: " + reference;
            }

            return null;
        }

        private bool HasChildren(SqlConnection connection, SqlTransaction transaction, string accountCode)
        {
            return ExecuteScalar<int>(connection, transaction, "SELECT COUNT(1) FROM dbo.ACCOUNTS WHERE Parent_Account_Code = @code", new SqlParameter("@code", SqlDbType.NVarChar, 70) { Value = accountCode }) > 0;
        }

        private string FindAutoReference(SqlConnection connection, SqlTransaction transaction, string accountCode)
        {
            var checks = new Dictionary<string, string[]>
            {
                { "ExpensesType", new[] { "Account_Code" } },
                { "TblStore", new[] { "Account_Code", "Account_Code1", "Account_Code2", "Account_Code3", "ParetnAccount" } },
                { "Tblinvestment", new[] { "ParentAccount", "Account_Code", "Account_Code1", "Account_Code2", "Account_Code3", "Account_Code4", "ParetnAccount", "ParetnAccount1", "RootAccount", "ParentAccountSub", "ParentAccount1", "RootAccount1", "Account_Code5", "Account_Code6", "Account_Code7" } },
                { "TblBuyLanReEst", new[] { "Account_Code" } },
                { "TblRevenuesTypes", new[] { "Account_Code" } },
                { "FixedAssetsGroup", new[] { "ParetnAccount", "Account_Code", "Account_Code1", "Account_Code2", "Account_Code3", "Account_Code4", "ParentExpensesAccount", "ParentEAssetAccount", "Account_Code5" } },
                { "Projects", new[] { "Material_account", "Project_account", "Salary_account", "REVENUE_account", "expanses_account", "sub_contractor_Account" } },
                { "BanksData", new[] { "Account_Code", "Account_Code1", "Account_Code2", "Account_code3", "parent_account" } },
                { "TblBoxesData", new[] { "Account_Code", "Account_Code1", "Account_Code2", "ParentAccount" } },
                { "TblCustemers", new[] { "parent_account", "ParentAccount", "Account_Code2", "Account_Code1", "Account_Code", "Account_Code_As_Client", "Account_Code_As_Supplier" } },
                { "TblEmployee", new[] { "Account_code", "Account_code1", "Account_Code2", "Account_Code3", "Account_Code4", "Account_Code5" } }
            };

            foreach (var check in checks)
            {
                foreach (var column in check.Value)
                {
                    if (!ColumnExists(connection, transaction, check.Key, column))
                    {
                        continue;
                    }

                    var sql = "SELECT TOP (1) 1 FROM dbo." + check.Key + " WHERE " + column + " = @code";
                    if (ExecuteScalar<int>(connection, transaction, sql, new SqlParameter("@code", SqlDbType.NVarChar, 255) { Value = accountCode }) > 0)
                    {
                        return check.Key + "." + column;
                    }
                }
            }

            return null;
        }

        private bool ColumnExists(SqlConnection connection, SqlTransaction transaction, string tableName, string columnName)
        {
            return ExecuteScalar<int>(connection, transaction,
                "SELECT COUNT(1) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = @table AND COLUMN_NAME = @column",
                new SqlParameter("@table", SqlDbType.NVarChar, 128) { Value = tableName },
                new SqlParameter("@column", SqlDbType.NVarChar, 128) { Value = columnName }) > 0;
        }

        private IList<LookupItemViewModel> GetLookup(SqlConnection connection, string sql)
        {
            var items = new List<LookupItemViewModel>();
            using (var command = new SqlCommand(sql, connection))
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    items.Add(new LookupItemViewModel
                    {
                        Id = Convert.ToString(reader.GetValue(0), CultureInfo.InvariantCulture),
                        Text = Convert.ToString(reader.GetValue(1), CultureInfo.InvariantCulture)
                    });
                }
            }

            return items;
        }

        private IList<int> GetIntList(SqlConnection connection, SqlTransaction transaction, string sql, string accountCode)
        {
            var list = new List<int>();
            using (var command = new SqlCommand(sql, connection, transaction))
            {
                command.Parameters.Add("@code", SqlDbType.NVarChar, 55).Value = accountCode ?? string.Empty;
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        if (!reader.IsDBNull(0))
                        {
                            list.Add(Convert.ToInt32(reader.GetValue(0), CultureInfo.InvariantCulture));
                        }
                    }
                }
            }

            return list;
        }

        private static int CountA(string value)
        {
            return string.IsNullOrEmpty(value) ? 0 : value.Count(c => c == 'a');
        }

        private static int ExtractTrailingNumber(string serial, string prefix)
        {
            if (string.IsNullOrWhiteSpace(serial))
            {
                return 0;
            }

            var value = serial.StartsWith(prefix ?? string.Empty, StringComparison.OrdinalIgnoreCase)
                ? serial.Substring((prefix ?? string.Empty).Length)
                : serial;

            int result;
            return int.TryParse(new string(value.Where(char.IsDigit).ToArray()), out result) ? result : 0;
        }

        private static string ReadString(IDataRecord reader, string name)
        {
            var value = reader[name];
            return value == DBNull.Value ? string.Empty : Convert.ToString(value, CultureInfo.InvariantCulture);
        }

        private static int ReadInt(IDataRecord reader, string name)
        {
            var value = reader[name];
            return value == DBNull.Value ? 0 : Convert.ToInt32(value, CultureInfo.InvariantCulture);
        }

        private static int? ReadNullableInt(IDataRecord reader, string name)
        {
            var value = reader[name];
            return value == DBNull.Value ? (int?)null : Convert.ToInt32(value, CultureInfo.InvariantCulture);
        }

        private static bool ReadBool(IDataRecord reader, string name)
        {
            var value = reader[name];
            return value != DBNull.Value && Convert.ToBoolean(value, CultureInfo.InvariantCulture);
        }

        private static DateTime? ReadNullableDate(IDataRecord reader, string name)
        {
            var value = reader[name];
            return value == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(value, CultureInfo.InvariantCulture);
        }

        private static int ExecuteNonQuery(SqlConnection connection, SqlTransaction transaction, string sql, params SqlParameter[] parameters)
        {
            using (var command = new SqlCommand(sql, connection, transaction))
            {
                command.Parameters.AddRange(parameters);
                return command.ExecuteNonQuery();
            }
        }

        private static T ExecuteScalar<T>(SqlConnection connection, SqlTransaction transaction, string sql, params SqlParameter[] parameters)
        {
            using (var command = new SqlCommand(sql, connection, transaction))
            {
                command.Parameters.AddRange(parameters);
                var value = command.ExecuteScalar();
                if (value == null || value == DBNull.Value)
                {
                    return default(T);
                }

                return (T)Convert.ChangeType(value, typeof(T), CultureInfo.InvariantCulture);
            }
        }

        private static AccountSaveResult Ok(string message, string accountCode, AccountDetailsViewModel account)
        {
            return new AccountSaveResult { Success = true, Message = message, AccountCode = accountCode, Account = account };
        }

        private static AccountSaveResult Fail(string message)
        {
            return new AccountSaveResult { Success = false, Message = message };
        }
    }
}
