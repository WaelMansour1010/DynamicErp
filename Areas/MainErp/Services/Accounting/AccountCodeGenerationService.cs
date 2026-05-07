using System;
using System.Data.SqlClient;
using System.Text.RegularExpressions;
using MyERP.Areas.MainErp.Interfaces;
using MyERP.Areas.MainErp.Models.Accounting;

namespace MyERP.Areas.MainErp.Services.Accounting
{
    public class AccountCodeGenerationService : IAccountCodeGenerationService
    {
        private readonly IMainErpDbConnectionFactory _connectionFactory;

        public AccountCodeGenerationService(IMainErpDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public AccountCodePreview PreviewNextChildCode(string parentAccountCode)
        {
            var preview = new AccountCodePreview { ParentAccountCode = parentAccountCode };
            if (string.IsNullOrWhiteSpace(parentAccountCode))
            {
                preview.Warning = "Parent account code is required.";
                return preview;
            }

            using (var connection = _connectionFactory.CreateOpenConnection())
            {
                preview.ParentExists = AccountExists(connection, parentAccountCode);
                if (!preview.ParentExists)
                {
                    preview.Warning = "Parent account was not found.";
                    return preview;
                }

                preview.ParentIsLastAccount = IsLastAccount(connection, parentAccountCode);
                if (preview.ParentIsLastAccount)
                {
                    preview.Warning = "Parent account is marked as last account; child generation is blocked.";
                    return preview;
                }

                preview.NextAccountCode = GenerateNextChildCode(connection, parentAccountCode);
                preview.Success = !string.IsNullOrWhiteSpace(preview.NextAccountCode);
                return preview;
            }
        }

        public bool IsLastAccount(string accountCode)
        {
            using (var connection = _connectionFactory.CreateOpenConnection())
            {
                return IsLastAccount(connection, accountCode);
            }
        }

        private static bool AccountExists(SqlConnection connection, string accountCode)
        {
            using (var command = new SqlCommand("SELECT COUNT(1) FROM ACCOUNTS WHERE Account_Code = @AccountCode", connection))
            {
                command.Parameters.AddWithValue("@AccountCode", accountCode);
                return Convert.ToInt32(command.ExecuteScalar()) > 0;
            }
        }

        private static bool IsLastAccount(SqlConnection connection, string accountCode)
        {
            using (var command = new SqlCommand("SELECT ISNULL(last_account, 0) FROM ACCOUNTS WHERE Account_Code = @AccountCode", connection))
            {
                command.Parameters.AddWithValue("@AccountCode", accountCode);
                var raw = command.ExecuteScalar();
                return raw != null && raw != DBNull.Value && Convert.ToBoolean(raw);
            }
        }

        private static string GenerateNextChildCode(SqlConnection connection, string parentAccountCode)
        {
            var maxSuffix = 0;
            using (var command = new SqlCommand("SELECT Account_Code FROM ACCOUNTS WITH (UPDLOCK, HOLDLOCK) WHERE Parent_Account_Code = @Parent", connection))
            {
                command.Parameters.AddWithValue("@Parent", parentAccountCode);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var accountCode = Convert.ToString(reader["Account_Code"]);
                        var suffix = ExtractTrailingChildSuffix(parentAccountCode, accountCode);
                        if (suffix > maxSuffix)
                        {
                            maxSuffix = suffix;
                        }
                    }
                }
            }

            var candidate = parentAccountCode + "a" + (maxSuffix + 1);
            while (AccountExists(connection, candidate))
            {
                maxSuffix++;
                candidate = parentAccountCode + "a" + (maxSuffix + 1);
            }

            return candidate;
        }

        private static int ExtractTrailingChildSuffix(string parentAccountCode, string accountCode)
        {
            if (string.IsNullOrWhiteSpace(accountCode) || !accountCode.StartsWith(parentAccountCode + "a", StringComparison.OrdinalIgnoreCase))
            {
                return 0;
            }

            var tail = accountCode.Substring((parentAccountCode + "a").Length);
            return Regex.IsMatch(tail, "^[0-9]+$") ? Convert.ToInt32(tail) : 0;
        }
    }
}
