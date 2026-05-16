using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using MyERP.Areas.MainErp.Interfaces;
using MyERP.Areas.MainErp.ViewModels.Branches;

namespace MyERP.Areas.MainErp.Repositories.Branches
{
    public class BranchesRepository
    {
        private readonly IMainErpDbConnectionFactory _connectionFactory;

        public BranchesRepository(IMainErpDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public BranchesIndexViewModel LoadIndex(string searchText, int? id)
        {
            var model = new BranchesIndexViewModel
            {
                SearchText = searchText,
                Results = Search(searchText),
                Accounts = LoadAccounts(),
                Employees = LoadEmployees(),
                ActivityTypes = LoadActivityTypes()
            };

            model.Selected = id.HasValue && id.Value > 0 ? Get(id.Value) : New();
            return model;
        }

        public BranchEditViewModel New()
        {
            return new BranchEditViewModel { BranchId = NextBranchId() };
        }

        public BranchEditViewModel Get(int id)
        {
            const string sql = "SELECT * FROM dbo.branches WHERE branch_id = @id;";
            using (var connection = _connectionFactory.CreateOpenConnection())
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.Add("@id", SqlDbType.Int).Value = id;
                using (var reader = command.ExecuteReader())
                {
                    return reader.Read() ? MapEdit(reader) : null;
                }
            }
        }

        public IList<BranchListItemViewModel> Search(string searchText)
        {
            const string sql = @"
SELECT TOP (200)
    b.branch_id,
    b.branch_name,
    b.branch_namee,
    b.tel,
    e.Emp_Name AS ManagerName,
    a.name AS ActivityName
FROM dbo.branches b
LEFT JOIN dbo.TblEmployee e ON e.Emp_ID = b.manger_id
LEFT JOIN dbo.tblActivitesType a ON a.id = b.ActivityTypeId
WHERE (@search IS NULL OR @search = N''
   OR CONVERT(NVARCHAR(50), b.branch_id) LIKE N'%' + @search + N'%'
   OR ISNULL(b.branch_name, N'') LIKE N'%' + @search + N'%'
   OR ISNULL(b.branch_namee, N'') LIKE N'%' + @search + N'%'
   OR ISNULL(b.tel, N'') LIKE N'%' + @search + N'%')
ORDER BY b.branch_id;";

            var rows = new List<BranchListItemViewModel>();
            using (var connection = _connectionFactory.CreateOpenConnection())
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.Add("@search", SqlDbType.NVarChar, 100).Value = (object)searchText ?? DBNull.Value;
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        rows.Add(new BranchListItemViewModel
                        {
                            Id = ReadInt(reader, "branch_id"),
                            NameAr = ReadString(reader, "branch_name"),
                            NameEn = ReadString(reader, "branch_namee"),
                            Phone = ReadString(reader, "tel"),
                            ManagerName = ReadString(reader, "ManagerName"),
                            ActivityName = ReadString(reader, "ActivityName")
                        });
                    }
                }
            }

            return rows;
        }

        public BranchSaveResult Save(BranchEditViewModel request)
        {
            Validate(request);
            var id = request.BranchId.GetValueOrDefault();
            var exists = Exists(id);
            var fields = BranchAccountDefinition.All.Select(x => x.FieldName).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

            using (var connection = _connectionFactory.CreateOpenConnection())
            using (var transaction = connection.BeginTransaction())
            {
                try
                {
                    if (exists)
                    {
                        var setList = new List<string>
                        {
                            "branch_name = @branch_name",
                            "branch_namee = @branch_namee",
                            "tel = @tel",
                            "address = @address",
                            "manger_id = @manger_id",
                            "ActivityTypeId = @ActivityTypeId"
                        };
                        setList.AddRange(fields.Select(x => x + " = @" + x));
                        var sql = "UPDATE dbo.branches SET " + string.Join(", ", setList) + " WHERE branch_id = @branch_id;";
                        ExecuteSave(connection, transaction, sql, request, fields);
                    }
                    else
                    {
                        var columns = new List<string> { "branch_id", "branch_name", "branch_namee", "tel", "address", "manger_id", "ActivityTypeId" };
                        columns.AddRange(fields);
                        var sql = "INSERT INTO dbo.branches (" + string.Join(", ", columns) + ") VALUES (@" + string.Join(", @", columns) + ");";
                        ExecuteSave(connection, transaction, sql, request, fields);
                    }

                    transaction.Commit();
                    return new BranchSaveResult { Success = true, Id = id, Message = "تم حفظ بيانات الفرع وربط الحسابات" };
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        public BranchSaveResult Delete(int id)
        {
            using (var connection = _connectionFactory.CreateOpenConnection())
            using (var command = new SqlCommand("DELETE FROM dbo.branches WHERE branch_id = @id;", connection))
            {
                command.Parameters.Add("@id", SqlDbType.Int).Value = id;
                command.ExecuteNonQuery();
            }

            return new BranchSaveResult { Success = true, Id = id, Message = "تم حذف الفرع" };
        }

        public IList<BranchAccountLookupViewModel> LoadAccounts()
        {
            const string sql = @"
SELECT TOP (5000)
    Account_Code,
    Account_Name,
    ISNULL(last_account, 0) AS last_account
FROM dbo.ACCOUNTS
WHERE ISNULL(Account_Code, N'') <> N''
ORDER BY Account_Code;";

            var rows = new List<BranchAccountLookupViewModel>();
            using (var connection = _connectionFactory.CreateOpenConnection())
            using (var command = new SqlCommand(sql, connection))
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    rows.Add(new BranchAccountLookupViewModel
                    {
                        Code = ReadString(reader, "Account_Code"),
                        Name = ReadString(reader, "Account_Name"),
                        IsLastAccount = ReadBool(reader, "last_account")
                    });
                }
            }

            return rows;
        }

        private IList<BranchLookupViewModel> LoadEmployees()
        {
            return LoadLookup("SELECT TOP (1000) Emp_ID AS Id, Emp_Name AS Text FROM dbo.TblEmployee ORDER BY Emp_Name;");
        }

        private IList<BranchLookupViewModel> LoadActivityTypes()
        {
            return LoadLookup("SELECT id AS Id, name AS Text FROM dbo.tblActivitesType ORDER BY name;");
        }

        private IList<BranchLookupViewModel> LoadLookup(string sql)
        {
            var rows = new List<BranchLookupViewModel>();
            using (var connection = _connectionFactory.CreateOpenConnection())
            using (var command = new SqlCommand(sql, connection))
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    rows.Add(new BranchLookupViewModel { Id = Convert.ToString(reader["Id"]), Text = Convert.ToString(reader["Text"]) });
                }
            }

            return rows;
        }

        private void ExecuteSave(SqlConnection connection, SqlTransaction transaction, string sql, BranchEditViewModel request, IList<string> fields)
        {
            using (var command = new SqlCommand(sql, connection, transaction))
            {
                command.Parameters.Add("@branch_id", SqlDbType.Int).Value = request.BranchId.Value;
                command.Parameters.Add("@branch_name", SqlDbType.NVarChar, 50).Value = DbText(request.NameAr);
                command.Parameters.Add("@branch_namee", SqlDbType.NVarChar, 50).Value = DbText(request.NameEn);
                command.Parameters.Add("@tel", SqlDbType.NVarChar, 50).Value = DbText(request.Phone);
                command.Parameters.Add("@address", SqlDbType.NVarChar, 50).Value = DbText(request.Address);
                command.Parameters.Add("@manger_id", SqlDbType.Int).Value = (object)request.ManagerId ?? DBNull.Value;
                command.Parameters.Add("@ActivityTypeId", SqlDbType.Int).Value = (object)request.ActivityTypeId ?? DBNull.Value;

                foreach (var field in fields)
                {
                    var value = request.Accounts != null && request.Accounts.ContainsKey(field) ? request.Accounts[field] : null;
                    command.Parameters.Add("@" + field, SqlDbType.NVarChar, 50).Value = DbText(value);
                }

                command.ExecuteNonQuery();
            }
        }

        private BranchEditViewModel MapEdit(IDataRecord reader)
        {
            var model = new BranchEditViewModel
            {
                BranchId = ReadInt(reader, "branch_id"),
                NameAr = ReadString(reader, "branch_name"),
                NameEn = ReadString(reader, "branch_namee"),
                Phone = ReadString(reader, "tel"),
                Address = ReadString(reader, "address"),
                ManagerId = ReadNullableInt(reader, "manger_id"),
                ActivityTypeId = ReadNullableInt(reader, "ActivityTypeId")
            };

            foreach (var definition in BranchAccountDefinition.All)
            {
                model.Accounts[definition.FieldName] = ReadString(reader, definition.FieldName);
            }

            return model;
        }

        private bool Exists(int id)
        {
            using (var connection = _connectionFactory.CreateOpenConnection())
            using (var command = new SqlCommand("SELECT 1 FROM dbo.branches WHERE branch_id = @id;", connection))
            {
                command.Parameters.Add("@id", SqlDbType.Int).Value = id;
                return command.ExecuteScalar() != null;
            }
        }

        private int NextBranchId()
        {
            using (var connection = _connectionFactory.CreateOpenConnection())
            using (var command = new SqlCommand("SELECT ISNULL(MAX(branch_id), 0) + 1 FROM dbo.branches;", connection))
            {
                return Convert.ToInt32(command.ExecuteScalar());
            }
        }

        private static void Validate(BranchEditViewModel request)
        {
            if (request == null) throw new InvalidOperationException("بيانات الفرع غير مكتملة");
            if (!request.BranchId.HasValue || request.BranchId.Value <= 0) throw new InvalidOperationException("رقم الفرع مطلوب");
            if (string.IsNullOrWhiteSpace(request.NameAr)) throw new InvalidOperationException("اسم الفرع مطلوب");
        }

        private static object DbText(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? (object)DBNull.Value : value.Trim();
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

        private static bool ReadBool(IDataRecord reader, string name)
        {
            var value = reader[name];
            return value != DBNull.Value && Convert.ToBoolean(value);
        }
    }
}
