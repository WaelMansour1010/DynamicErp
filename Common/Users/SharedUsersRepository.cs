using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace MyERP.Common.Users
{
    public class SharedUsersRepository
    {
        private readonly Func<SqlConnection> _openConnection;

        public SharedUsersRepository(Func<SqlConnection> openConnection)
        {
            if (openConnection == null) throw new ArgumentNullException("openConnection");
            _openConnection = openConnection;
        }

        public SharedUserSearchResult Search(string searchText, int page, int pageSize)
        {
            page = Math.Max(1, page);
            pageSize = Math.Max(10, Math.Min(pageSize, 100));

            using (var connection = _openConnection())
            {
                var result = new SharedUserSearchResult
                {
                    TotalRows = CountUsers(connection, searchText)
                };

                var skip = (page - 1) * pageSize;
                foreach (var row in ReadUsers(connection, searchText, skip, pageSize))
                {
                    result.Items.Add(row);
                }

                return result;
            }
        }

        private static int CountUsers(SqlConnection connection, string searchText)
        {
            using (var command = new SqlCommand(BuildUserSql(true), connection))
            {
                AddSearch(command, searchText);
                return Convert.ToInt32(command.ExecuteScalar());
            }
        }

        private static IList<SharedUserListRow> ReadUsers(SqlConnection connection, string searchText, int skip, int take)
        {
            using (var command = new SqlCommand(BuildUserSql(false), connection))
            {
                AddSearch(command, searchText);
                command.Parameters.Add("@Skip", SqlDbType.Int).Value = skip;
                command.Parameters.Add("@Take", SqlDbType.Int).Value = take;

                var rows = new List<SharedUserListRow>();
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var userType = ReadInt(reader, "UserType");
                        rows.Add(new SharedUserListRow
                        {
                            UserId = ReadInt(reader, "UserID").GetValueOrDefault(),
                            UserName = ReadString(reader, "UserName"),
                            EmployeeName = ReadString(reader, "EmployeeName"),
                            BranchName = ReadString(reader, "BranchName"),
                            StoreName = ReadString(reader, "StoreName"),
                            BoxName = ReadString(reader, "BoxName"),
                            UserType = userType,
                            IsDeactivated = ReadInt(reader, "isDeactivated").GetValueOrDefault() != 0,
                            IsAdmin = userType.GetValueOrDefault(-1) == 0
                        });
                    }
                }

                return rows;
            }
        }

        private static string BuildUserSql(bool countOnly)
        {
            var select = countOnly
                ? "SELECT COUNT(1)"
                : @"SELECT
    u.UserID,
    u.UserName,
    u.UserType,
    u.isDeactivated,
    e.Emp_Name AS EmployeeName,
    COALESCE(NULLIF(b.branch_name, N''), NULLIF(b.branch_namee, N'')) AS BranchName,
    COALESCE(NULLIF(s.StoreName, N''), NULLIF(s.StoreNamee, N'')) AS StoreName,
    COALESCE(NULLIF(x.BoxName, N''), NULLIF(x.BoxNameE, N'')) AS BoxName";

            return select + @"
FROM dbo.TblUsers u
LEFT JOIN dbo.TblEmployee e ON e.Emp_ID = u.Empid
LEFT JOIN dbo.TblBranchesData b ON b.branch_id = u.BranchId
LEFT JOIN dbo.TblStore s ON s.StoreID = u.StoreID
LEFT JOIN dbo.TblBoxesData x ON x.BoxID = u.BoxID
WHERE (@SearchText = N'' OR u.UserName LIKE N'%' + @SearchText + N'%' OR e.Emp_Name LIKE N'%' + @SearchText + N'%')" +
                (countOnly ? ";" : @"
ORDER BY u.UserID
OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY;");
        }

        private static void AddSearch(SqlCommand command, string searchText)
        {
            command.Parameters.Add("@SearchText", SqlDbType.NVarChar, 100).Value = (searchText ?? string.Empty).Trim();
        }

        private static string ReadString(IDataRecord record, string name)
        {
            var ordinal = record.GetOrdinal(name);
            return record.IsDBNull(ordinal) ? null : Convert.ToString(record.GetValue(ordinal));
        }

        private static int? ReadInt(IDataRecord record, string name)
        {
            var ordinal = record.GetOrdinal(name);
            return record.IsDBNull(ordinal) ? (int?)null : Convert.ToInt32(record.GetValue(ordinal));
        }
    }
}
