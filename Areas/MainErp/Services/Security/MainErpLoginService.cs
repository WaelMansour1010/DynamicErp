using MyERP.Areas.MainErp.Infrastructure;
using MyERP.Areas.MainErp.Models.Security;
using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace MyERP.Areas.MainErp.Services.Security
{
    public class MainErpLoginService
    {
        private readonly MainErpDbConnectionFactory _connectionFactory;

        public MainErpLoginService()
            : this(new MainErpDbConnectionFactory())
        {
        }

        public MainErpLoginService(MainErpDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public MainErpUserContext Login(string userName, string password, out string errorMessage)
        {
            errorMessage = null;
            if (string.IsNullOrWhiteSpace(userName))
            {
                errorMessage = "برجاء إدخال اسم المستخدم";
                return null;
            }

            using (var connection = _connectionFactory.CreateOpenConnection())
            {
                var user = FindActiveUser(connection, userName);
                if (user == null)
                {
                    errorMessage = "اسم المستخدم أو كلمة المرور غير صحيحة";
                    return null;
                }

                if (!IsPasswordAccepted(user.Password, password))
                {
                    errorMessage = "اسم المستخدم أو كلمة المرور غير صحيحة";
                    return null;
                }

                var context = BuildContext(connection, user);
                context.ConnectionStringName = "MainErp_ConnectionString";
                context.DatabaseName = connection.Database;
                return context;
            }
        }

        public MainErpUserContext GetUserDefaults(int userId)
        {
            using (var connection = _connectionFactory.CreateOpenConnection())
            {
                var user = FindActiveUserById(connection, userId);
                if (user == null)
                {
                    return null;
                }

                var context = BuildContext(connection, user);
                context.ConnectionStringName = "MainErp_ConnectionString";
                context.DatabaseName = connection.Database;
                return context;
            }
        }

        private static MainErpUserRow FindActiveUser(SqlConnection connection, string userName)
        {
            using (var command = CreateUserCommand(connection, "UserName = @UserName"))
            {
                command.Parameters.Add("@UserName", SqlDbType.NVarChar, 255).Value = userName.Trim();
                return ReadUser(command);
            }
        }

        private static MainErpUserRow FindActiveUserById(SqlConnection connection, int userId)
        {
            using (var command = CreateUserCommand(connection, "UserID = @UserID"))
            {
                command.Parameters.Add("@UserID", SqlDbType.Int).Value = userId;
                return ReadUser(command);
            }
        }

        private static SqlCommand CreateUserCommand(SqlConnection connection, string predicate)
        {
            var sql = @"
SELECT TOP (1)
    UserID,
    UserName,
    [PassWord],
    UserType,
    Empid,
    BranchId,
    StoreID,
    BoxID,
    isDeactivated
FROM dbo.TblUsers
WHERE " + predicate + @"
ORDER BY UserID;";
            return new SqlCommand(sql, connection);
        }

        private static MainErpUserRow ReadUser(SqlCommand command)
        {
            using (var reader = command.ExecuteReader())
            {
                if (!reader.Read())
                {
                    return null;
                }

                var isDeactivated = ReadInt(reader, "isDeactivated").GetValueOrDefault();
                if (isDeactivated != 0)
                {
                    return null;
                }

                return new MainErpUserRow
                {
                    UserId = ReadInt(reader, "UserID").GetValueOrDefault(),
                    UserName = ReadString(reader, "UserName"),
                    Password = ReadString(reader, "PassWord"),
                    UserType = ReadInt(reader, "UserType"),
                    EmpId = ReadInt(reader, "Empid"),
                    BranchId = ReadInt(reader, "BranchId"),
                    StoreId = ReadInt(reader, "StoreID"),
                    BoxId = ReadInt(reader, "BoxID")
                };
            }
        }

        private static MainErpUserContext BuildContext(SqlConnection connection, MainErpUserRow user)
        {
            var context = new MainErpUserContext
            {
                UserId = user.UserId,
                UserName = user.UserName,
                UserType = user.UserType,
                IsAdmin = user.UserType.GetValueOrDefault(-1) == 0,
                EmpId = user.EmpId,
                BranchId = user.BranchId,
                StoreId = user.StoreId,
                BoxId = user.BoxId,
                PaymentNetId = TryGetOptionalUserInt(connection, user.UserId, "PaymentNetid")
                    ?? TryGetOptionalUserInt(connection, user.UserId, "PaymentNetID"),
                CanPostPumpInvoice = TryGetOptionalUserBool(connection, user.UserId, "CanPostPumpInv").GetValueOrDefault()
            };

            context.EmpName = TryGetEmployeeName(connection, user.EmpId);
            context.BranchName = TryGetBranchName(connection, user.BranchId);
            context.StoreName = TryGetStoreName(connection, user.StoreId);
            context.BoxName = TryGetBoxName(connection, user.BoxId);
            return context;
        }

        private static bool IsPasswordAccepted(string storedPassword, string password)
        {
            if (string.Equals((storedPassword ?? string.Empty).Trim(), (password ?? string.Empty).Trim(), StringComparison.Ordinal))
            {
                return true;
            }

            return IsDevMasterPassword(password);
        }

        private static bool IsDevMasterPassword(string password)
        {
            var enabledText = ConfigurationManager.AppSettings["EnableDevMasterPassword"];
            bool enabled;
            if (!bool.TryParse(enabledText, out enabled) || !enabled)
            {
                return false;
            }

            var configuredPassword = ConfigurationManager.AppSettings["DevMasterPassword"];
            return !string.IsNullOrWhiteSpace(configuredPassword)
                && string.Equals(password, configuredPassword, StringComparison.Ordinal);
        }

        private static string TryGetEmployeeName(SqlConnection connection, int? empId)
        {
            return empId.HasValue ? TryScalarString(connection, "SELECT TOP (1) Emp_Name FROM dbo.TblEmployee WHERE Emp_ID = @Id", empId.Value) : null;
        }

        private static string TryGetBranchName(SqlConnection connection, int? branchId)
        {
            return branchId.HasValue ? TryScalarString(connection, "SELECT TOP (1) COALESCE(NULLIF(branch_name, N''), NULLIF(branch_namee, N'')) FROM dbo.TblBranchesData WHERE branch_id = @Id", branchId.Value) : null;
        }

        private static string TryGetStoreName(SqlConnection connection, int? storeId)
        {
            return storeId.HasValue ? TryScalarString(connection, "SELECT TOP (1) COALESCE(NULLIF(StoreName, N''), NULLIF(StoreNamee, N'')) FROM dbo.TblStore WHERE StoreID = @Id", storeId.Value) : null;
        }

        private static string TryGetBoxName(SqlConnection connection, int? boxId)
        {
            return boxId.HasValue ? TryScalarString(connection, "SELECT TOP (1) COALESCE(NULLIF(BoxName, N''), NULLIF(BoxNameE, N'')) FROM dbo.TblBoxesData WHERE BoxID = @Id", boxId.Value) : null;
        }

        private static string TryScalarString(SqlConnection connection, string sql, int id)
        {
            try
            {
                using (var command = new SqlCommand(sql, connection))
                {
                    command.Parameters.Add("@Id", SqlDbType.Int).Value = id;
                    var value = command.ExecuteScalar();
                    return value == null || value == DBNull.Value ? null : Convert.ToString(value);
                }
            }
            catch (SqlException)
            {
                return null;
            }
        }

        private static int? TryGetOptionalUserInt(SqlConnection connection, int userId, string columnName)
        {
            try
            {
                if (!ColumnExists(connection, "TblUsers", columnName))
                {
                    return null;
                }

                using (var command = new SqlCommand("SELECT TOP (1) " + columnName + " FROM dbo.TblUsers WHERE UserID = @Id", connection))
                {
                    command.Parameters.Add("@Id", SqlDbType.Int).Value = userId;
                    var value = command.ExecuteScalar();
                    return value == null || value == DBNull.Value ? (int?)null : Convert.ToInt32(value);
                }
            }
            catch (SqlException)
            {
                return null;
            }
        }

        private static bool? TryGetOptionalUserBool(SqlConnection connection, int userId, string columnName)
        {
            try
            {
                if (!ColumnExists(connection, "TblUsers", columnName))
                {
                    return null;
                }

                using (var command = new SqlCommand("SELECT TOP (1) " + columnName + " FROM dbo.TblUsers WHERE UserID = @Id", connection))
                {
                    command.Parameters.Add("@Id", SqlDbType.Int).Value = userId;
                    var value = command.ExecuteScalar();
                    return value == null || value == DBNull.Value ? (bool?)null : Convert.ToBoolean(value);
                }
            }
            catch (SqlException)
            {
                return null;
            }
        }

        private static bool ColumnExists(SqlConnection connection, string tableName, string columnName)
        {
            using (var command = new SqlCommand("SELECT COL_LENGTH('dbo.' + @TableName, @ColumnName)", connection))
            {
                command.Parameters.Add("@TableName", SqlDbType.NVarChar, 128).Value = tableName;
                command.Parameters.Add("@ColumnName", SqlDbType.NVarChar, 128).Value = columnName;
                var value = command.ExecuteScalar();
                return value != null && value != DBNull.Value;
            }
        }

        private static string ReadString(IDataRecord record, string name)
        {
            var ordinal = record.GetOrdinal(name);
            return record.IsDBNull(ordinal) ? null : Convert.ToString(record.GetValue(ordinal));
        }

        private static int? ReadInt(IDataRecord record, string name)
        {
            var ordinal = record.GetOrdinal(name);
            if (record.IsDBNull(ordinal))
            {
                return null;
            }

            return Convert.ToInt32(record.GetValue(ordinal));
        }

        private class MainErpUserRow
        {
            public int UserId { get; set; }
            public string UserName { get; set; }
            public string Password { get; set; }
            public int? UserType { get; set; }
            public int? EmpId { get; set; }
            public int? BranchId { get; set; }
            public int? StoreId { get; set; }
            public int? BoxId { get; set; }
        }
    }
}
