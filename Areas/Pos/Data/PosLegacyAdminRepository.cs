using MyERP.Areas.Pos.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Text;

namespace MyERP.Areas.Pos.Data
{
    public class PosLegacyAdminRepository
    {
        private readonly string _connectionString;

        public PosLegacyAdminRepository()
        {
            var setting = ConfigurationManager.ConnectionStrings["KishnyCashConnection"];
            if (setting == null || string.IsNullOrWhiteSpace(setting.ConnectionString))
            {
                throw new ConfigurationErrorsException("Missing connection string: KishnyCashConnection");
            }

            _connectionString = setting.ConnectionString;
        }

        public PosLegacyUsersIndexModel LoadUsers(string searchText, int? userId)
        {
            using (var connection = OpenConnection())
            {
                var model = new PosLegacyUsersIndexModel
                {
                    SearchText = searchText,
                    Users = SearchUsers(connection, searchText),
                    Branches = GetBranches(connection),
                    Stores = GetStores(connection),
                    Boxes = GetBoxes(connection),
                    Employees = GetEmployees(connection),
                    Accounts = GetAccounts(connection),
                    ProductLines = GetProductLines(connection)
                };

                model.Selected = userId.HasValue ? GetUser(connection, userId.Value) : NewUser(connection);
                return model;
            }
        }

        public PosLegacyUserEditModel GetUser(int userId)
        {
            using (var connection = OpenConnection())
            {
                return GetUser(connection, userId);
            }
        }

        public PosLegacyUserEditModel NewUser()
        {
            using (var connection = OpenConnection())
            {
                return NewUser(connection);
            }
        }

        public PosLegacySaveResult SaveUser(PosLegacyUserEditModel request)
        {
            if (request == null)
            {
                return Fail("لا توجد بيانات للحفظ.");
            }

            if (string.IsNullOrWhiteSpace(request.UserName))
            {
                return Fail("اسم المستخدم مطلوب.");
            }

            if (!string.Equals((request.PassWord ?? "").Trim(), (request.PassConfirm ?? "").Trim(), StringComparison.Ordinal))
            {
                return Fail("كلمة المرور وتأكيدها غير متطابقين.");
            }

            using (var connection = OpenConnection())
            using (var transaction = connection.BeginTransaction())
            {
                var userId = request.UserID.GetValueOrDefault();
                var isNew = userId <= 0;
                if (UserNameExists(connection, transaction, request.UserName, userId))
                {
                    return Fail("اسم المستخدم موجود من قبل.");
                }

                var columns = LoadColumns(connection, transaction, "TblUsers");
                if (isNew)
                {
                    userId = NextId(connection, transaction, "TblUsers", "UserID");
                    InsertUser(connection, transaction, columns, userId, request);
                }
                else
                {
                    UpdateUser(connection, transaction, columns, userId, request);
                }

                ReplaceLinks(connection, transaction, "TblUsersBranches", "BranchID", "userid", userId, request.BranchIds, request.BranchId, request.UserID, request);
                ReplaceLinks(connection, transaction, "TblUsersStores", "StoreID", "userid", userId, request.StoreIds, request.StoreID, null, null);
                ReplaceLinks(connection, transaction, "TblUsersBoxes", "BoxId", "userid", userId, request.BoxIds, request.BoxID, null, null);
                ReplaceLinks(connection, transaction, "TblUserAccount", "Account_ID", "UserID", userId, request.AccountIds, null, null, null);
                ReplaceLinks(connection, transaction, "TblUsersProductLine", "ProductLineId", "UserID", userId, request.ProductLineIds, null, null, null);

                transaction.Commit();
                return new PosLegacySaveResult { Success = true, Message = "تم حفظ المستخدم بنجاح.", Id = userId };
            }
        }

        public PosLegacySaveResult DeleteUser(int userId)
        {
            if (userId == 1)
            {
                return Fail("لا يمكن حذف المستخدم الرئيسي.");
            }

            using (var connection = OpenConnection())
            using (var transaction = connection.BeginTransaction())
            {
                Execute(connection, transaction, "DELETE FROM dbo.TblUsersStores WHERE userid = @Id", userId);
                Execute(connection, transaction, "DELETE FROM dbo.TblUsersBranches WHERE userid = @Id", userId);
                Execute(connection, transaction, "DELETE FROM dbo.TblUsersBoxes WHERE userid = @Id", userId);
                Execute(connection, transaction, "DELETE FROM dbo.TblUserAccount WHERE UserID = @Id", userId);
                Execute(connection, transaction, "DELETE FROM dbo.TblUsersProductLine WHERE UserID = @Id", userId);
                Execute(connection, transaction, "DELETE FROM dbo.TblUsers WHERE UserID = @Id", userId);
                transaction.Commit();
                return new PosLegacySaveResult { Success = true, Message = "تم حذف المستخدم.", Id = userId };
            }
        }

        public PosBranchesDataIndexModel LoadBranchesData(int? activityId)
        {
            using (var connection = OpenConnection())
            {
                var activities = GetActivities(connection);
                var selectedId = activityId ?? (activities.Count > 0 ? (int?)activities[0].Id : null);
                return new PosBranchesDataIndexModel
                {
                    Activities = activities,
                    Selected = selectedId.HasValue ? GetActivity(connection, selectedId.Value) : new PosActivityEditModel(),
                    Branches = selectedId.HasValue ? GetBranchesForActivity(connection, selectedId.Value) : new List<PosBranchDataEditModel>(),
                    Regions = GetRegions(connection),
                    Stores = GetStores(connection)
                };
            }
        }

        public PosLegacySaveResult SaveBranchesData(PosActivityEditModel activity, IList<PosBranchDataEditModel> branches)
        {
            if (activity == null)
            {
                return Fail("بيانات النشاط مطلوبة.");
            }

            if (string.IsNullOrWhiteSpace(activity.Name))
            {
                return Fail("اسم النشاط عربي مطلوب.");
            }

            if (string.IsNullOrWhiteSpace(activity.NameEnglish))
            {
                return Fail("اسم النشاط إنجليزي مطلوب.");
            }

            using (var connection = OpenConnection())
            using (var transaction = connection.BeginTransaction())
            {
                var activityId = activity.Id.GetValueOrDefault();
                if (activityId <= 0)
                {
                    activityId = NextId(connection, transaction, "tblActivitesType", "id");
                    using (var command = new SqlCommand("INSERT INTO dbo.tblActivitesType (id, Name, namee) VALUES (@Id, @Name, @NameE)", connection, transaction))
                    {
                        command.Parameters.Add("@Id", SqlDbType.Int).Value = activityId;
                        command.Parameters.Add("@Name", SqlDbType.NVarChar, 50).Value = (object)activity.Name.Trim() ?? DBNull.Value;
                        command.Parameters.Add("@NameE", SqlDbType.NVarChar, 255).Value = (object)activity.NameEnglish.Trim() ?? DBNull.Value;
                        command.ExecuteNonQuery();
                    }
                }
                else
                {
                    using (var command = new SqlCommand("UPDATE dbo.tblActivitesType SET Name = @Name, namee = @NameE WHERE id = @Id", connection, transaction))
                    {
                        command.Parameters.Add("@Id", SqlDbType.Int).Value = activityId;
                        command.Parameters.Add("@Name", SqlDbType.NVarChar, 50).Value = activity.Name.Trim();
                        command.Parameters.Add("@NameE", SqlDbType.NVarChar, 255).Value = activity.NameEnglish.Trim();
                        command.ExecuteNonQuery();
                    }
                }

                var branchColumns = LoadColumns(connection, transaction, "TblBranchesData");
                foreach (var branch in (branches ?? new List<PosBranchDataEditModel>()).Where(IsRealBranch))
                {
                    ValidateBranch(connection, transaction, branch, activityId);
                    var branchId = branch.BranchId.GetValueOrDefault();
                    if (branchId <= 0)
                    {
                        branchId = NextId(connection, transaction, "TblBranchesData", "branch_id");
                        InsertBranch(connection, transaction, branchColumns, activityId, branchId, branch);
                    }
                    else if (Exists(connection, transaction, "TblBranchesData", "branch_id", branchId))
                    {
                        UpdateBranch(connection, transaction, branchColumns, activityId, branchId, branch);
                    }
                    else
                    {
                        InsertBranch(connection, transaction, branchColumns, activityId, branchId, branch);
                    }
                }

                transaction.Commit();
                return new PosLegacySaveResult { Success = true, Message = "تم حفظ بيانات النشاط والفروع.", Id = activityId };
            }
        }

        private void InsertUser(SqlConnection connection, SqlTransaction transaction, IDictionary<string, OptionColumn> columns, int userId, PosLegacyUserEditModel request)
        {
            var values = UserValues(request);
            values["UserID"] = userId;
            ExecuteInsert(connection, transaction, "TblUsers", columns, values);
        }

        private void UpdateUser(SqlConnection connection, SqlTransaction transaction, IDictionary<string, OptionColumn> columns, int userId, PosLegacyUserEditModel request)
        {
            var values = UserValues(request);
            ExecuteUpdate(connection, transaction, "TblUsers", "UserID", userId, columns, values);
        }

        private static Dictionary<string, object> UserValues(PosLegacyUserEditModel r)
        {
            return new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                { "UserName", Trim(r.UserName) },
                { "PassWord", Trim(r.PassWord) },
                { "PassConfirm", Trim(r.PassConfirm) },
                { "UserType", r.UserType.GetValueOrDefault(2) },
                { "Empid", DbInt(r.Empid) },
                { "BranchId", DbInt(r.BranchId) },
                { "StoreID", DbInt(r.StoreID) },
                { "StoreID1", DbInt(r.StoreID1) },
                { "StoreID2", DbInt(r.StoreID2) },
                { "StoreID3", DbInt(r.StoreID3) },
                { "BoxID", DbInt(r.BoxID) },
                { "BoxID1", DbInt(r.BoxID1) },
                { "BoxID2", DbInt(r.BoxID2) },
                { "BankID", DbInt(r.BankID) },
                { "Custid", DbInt(r.Custid) },
                { "Custid1", DbInt(r.Custid1) },
                { "Account_Code", Trim(r.Account_Code) },
                { "ReportName", Trim(r.ReportName) },
                { "ReportName1", Trim(r.ReportName1) },
                { "ReportName2", Trim(r.ReportName2) },
                { "CreditLimitSalesMan", (object)r.CreditLimitSalesMan ?? DBNull.Value },
                { "ChangePW", r.ChangePW },
                { "CustomerService", r.CustomerService },
                { "HidLowering", r.HidLowering },
                { "AllowSelectEmp", r.AllowSelectEmp },
                { "isDeactivated", r.IsDeactivated ? 1 : 0 },
                { "CanEditKYC", r.CanEditKYC },
                { "IsFullAccsesCustomerService", r.IsFullAccsesCustomerService },
                { "IsReturnAllowed", r.IsReturnAllowed },
                { "CanEditSalesInvoice", r.CanEditSalesInvoice },
                { "CanEditSalesInvoicePos", r.CanEditSalesInvoicePos },
                { "CanCancelClose", r.CanCancelClose },
                { "UserCategory", Trim(r.UserCategory) }
            };
        }

        private void InsertBranch(SqlConnection connection, SqlTransaction transaction, IDictionary<string, OptionColumn> columns, int activityId, int branchId, PosBranchDataEditModel r)
        {
            var values = BranchValues(activityId, branchId, r);
            ExecuteInsert(connection, transaction, "TblBranchesData", columns, values);
        }

        private void UpdateBranch(SqlConnection connection, SqlTransaction transaction, IDictionary<string, OptionColumn> columns, int activityId, int branchId, PosBranchDataEditModel r)
        {
            var values = BranchValues(activityId, branchId, r);
            values.Remove("branch_id");
            ExecuteUpdate(connection, transaction, "TblBranchesData", "branch_id", branchId, columns, values);
        }

        private static Dictionary<string, object> BranchValues(int activityId, int branchId, PosBranchDataEditModel r)
        {
            return new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                { "branch_id", branchId },
                { "ActivityTypeId", activityId },
                { "branch_Code", Trim(r.BranchCode) },
                { "branch_name", Trim(r.BranchName) },
                { "branch_namee", Trim(r.BranchNameEnglish) },
                { "manger", Trim(r.Manager) },
                { "Tel", Trim(r.Telephone) },
                { "Remarks", Trim(r.Remarks) },
                { "Account_Code", Trim(r.AccountCode) },
                { "Users", Trim(r.Users) },
                { "VATNO", Trim(r.VatNo) },
                { "RegionID", DbInt(r.RegionId) },
                { "StoreId", DbInt(r.StoreId) },
                { "ShowlogoInReports", r.ShowLogoInReports },
                { "IsStoped", r.IsStopped }
            };
        }

        private void ExecuteInsert(SqlConnection connection, SqlTransaction transaction, string table, IDictionary<string, OptionColumn> columns, IDictionary<string, object> values)
        {
            var filtered = values.Where(x => columns.ContainsKey(x.Key)).ToList();
            var names = string.Join(", ", filtered.Select(x => Quote(x.Key)));
            var parameters = string.Join(", ", filtered.Select((x, i) => "@p" + i.ToString(CultureInfo.InvariantCulture)));
            using (var command = new SqlCommand("INSERT INTO dbo." + Quote(table) + " (" + names + ") VALUES (" + parameters + ")", connection, transaction))
            {
                for (var i = 0; i < filtered.Count; i++)
                {
                    command.Parameters.AddWithValue("@p" + i.ToString(CultureInfo.InvariantCulture), filtered[i].Value ?? DBNull.Value);
                }

                command.ExecuteNonQuery();
            }
        }

        private void ExecuteUpdate(SqlConnection connection, SqlTransaction transaction, string table, string keyColumn, int keyValue, IDictionary<string, OptionColumn> columns, IDictionary<string, object> values)
        {
            var filtered = values.Where(x => columns.ContainsKey(x.Key) && !string.Equals(x.Key, keyColumn, StringComparison.OrdinalIgnoreCase)).ToList();
            var assignments = string.Join(", ", filtered.Select((x, i) => Quote(x.Key) + " = @p" + i.ToString(CultureInfo.InvariantCulture)));
            using (var command = new SqlCommand("UPDATE dbo." + Quote(table) + " SET " + assignments + " WHERE " + Quote(keyColumn) + " = @Id", connection, transaction))
            {
                for (var i = 0; i < filtered.Count; i++)
                {
                    command.Parameters.AddWithValue("@p" + i.ToString(CultureInfo.InvariantCulture), filtered[i].Value ?? DBNull.Value);
                }

                command.Parameters.Add("@Id", SqlDbType.Int).Value = keyValue;
                command.ExecuteNonQuery();
            }
        }

        private void ReplaceLinks(SqlConnection connection, SqlTransaction transaction, string table, string valueColumn, string userColumn, int userId, IEnumerable<int> ids, int? defaultId, int? requestUserId, PosLegacyUserEditModel request)
        {
            Execute(connection, transaction, "DELETE FROM dbo." + Quote(table) + " WHERE " + Quote(userColumn) + " = @Id", userId);
            var cleanIds = new HashSet<int>((ids ?? new int[0]).Where(x => x > 0));
            if (defaultId.HasValue && defaultId.Value > 0)
            {
                cleanIds.Add(defaultId.Value);
            }

            foreach (var id in cleanIds)
            {
                var columns = LoadColumns(connection, transaction, table);
                var values = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                {
                    { userColumn, userId },
                    { valueColumn, id }
                };

                if (columns.ContainsKey("id"))
                {
                    values["id"] = NextId(connection, transaction, table, "id");
                }

                if (columns.ContainsKey("ActivityTypeId") && request != null)
                {
                    values["ActivityTypeId"] = DBNull.Value;
                }

                ExecuteInsert(connection, transaction, table, columns, values);
            }
        }

        private IList<PosLegacyUserListItem> SearchUsers(SqlConnection connection, string searchText)
        {
            const string sql = @"
SELECT TOP (250)
    u.UserID, u.UserName, u.isDeactivated,
    e.Emp_Name,
    COALESCE(NULLIF(b.branch_name, N''), NULLIF(b.branch_namee, N''), CONVERT(NVARCHAR(20), u.BranchId)) AS BranchName,
    COALESCE(NULLIF(s.StoreName, N''), NULLIF(s.StoreNamee, N''), CONVERT(NVARCHAR(20), u.StoreID)) AS StoreName,
    COALESCE(NULLIF(box.BoxName, N''), NULLIF(box.BoxNameE, N''), CONVERT(NVARCHAR(20), u.BoxID)) AS BoxName
FROM dbo.TblUsers u
LEFT JOIN dbo.TblEmployee e ON e.Emp_ID = u.Empid
LEFT JOIN dbo.TblBranchesData b ON b.branch_id = u.BranchId
LEFT JOIN dbo.TblStore s ON s.StoreID = u.StoreID
LEFT JOIN dbo.TblBoxesData box ON box.BoxID = u.BoxID
WHERE @Search = N''
   OR u.UserName LIKE N'%' + @Search + N'%'
   OR CONVERT(NVARCHAR(20), u.UserID) = @Search
   OR e.Emp_Name LIKE N'%' + @Search + N'%'
ORDER BY u.UserID;";
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.Add("@Search", SqlDbType.NVarChar, 100).Value = (searchText ?? "").Trim();
                using (var reader = command.ExecuteReader())
                {
                    var rows = new List<PosLegacyUserListItem>();
                    while (reader.Read())
                    {
                        rows.Add(new PosLegacyUserListItem
                        {
                            UserId = ReadInt(reader, "UserID").GetValueOrDefault(),
                            UserName = ReadString(reader, "UserName"),
                            EmployeeName = ReadString(reader, "Emp_Name"),
                            BranchName = ReadString(reader, "BranchName"),
                            StoreName = ReadString(reader, "StoreName"),
                            BoxName = ReadString(reader, "BoxName"),
                            IsDeactivated = ReadBool(reader, "isDeactivated")
                        });
                    }

                    return rows;
                }
            }
        }

        private PosLegacyUserEditModel NewUser(SqlConnection connection)
        {
            return new PosLegacyUserEditModel
            {
                UserType = 2,
                PassWord = "",
                PassConfirm = ""
            };
        }

        private PosLegacyUserEditModel GetUser(SqlConnection connection, int userId)
        {
            using (var command = new SqlCommand("SELECT TOP (1) * FROM dbo.TblUsers WHERE UserID = @UserID", connection))
            {
                command.Parameters.Add("@UserID", SqlDbType.Int).Value = userId;
                using (var reader = command.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        return NewUser(connection);
                    }

                    var model = new PosLegacyUserEditModel
                    {
                        UserID = userId,
                        UserName = ReadString(reader, "UserName"),
                        PassWord = ReadString(reader, "PassWord"),
                        PassConfirm = ReadString(reader, "PassConfirm"),
                        UserType = ReadInt(reader, "UserType"),
                        Empid = ReadInt(reader, "Empid"),
                        BranchId = ReadInt(reader, "BranchId"),
                        StoreID = ReadInt(reader, "StoreID"),
                        StoreID1 = ReadInt(reader, "StoreID1"),
                        StoreID2 = ReadInt(reader, "StoreID2"),
                        StoreID3 = ReadInt(reader, "StoreID3"),
                        BoxID = ReadInt(reader, "BoxID"),
                        BoxID1 = ReadInt(reader, "BoxID1"),
                        BoxID2 = ReadInt(reader, "BoxID2"),
                        BankID = ReadInt(reader, "BankID"),
                        Custid = ReadInt(reader, "Custid"),
                        Custid1 = ReadInt(reader, "Custid1"),
                        Account_Code = ReadString(reader, "Account_Code"),
                        ReportName = ReadString(reader, "ReportName"),
                        ReportName1 = ReadString(reader, "ReportName1"),
                        ReportName2 = ReadString(reader, "ReportName2"),
                        CreditLimitSalesMan = ReadDecimal(reader, "CreditLimitSalesMan"),
                        ChangePW = ReadBool(reader, "ChangePW"),
                        CustomerService = ReadBool(reader, "CustomerService"),
                        HidLowering = ReadBool(reader, "HidLowering"),
                        AllowSelectEmp = ReadBool(reader, "AllowSelectEmp"),
                        IsDeactivated = ReadBool(reader, "isDeactivated"),
                        CanEditKYC = ReadBool(reader, "CanEditKYC"),
                        IsFullAccsesCustomerService = ReadBool(reader, "IsFullAccsesCustomerService"),
                        IsReturnAllowed = ReadBool(reader, "IsReturnAllowed"),
                        CanEditSalesInvoice = ReadBool(reader, "CanEditSalesInvoice"),
                        CanEditSalesInvoicePos = ReadBool(reader, "CanEditSalesInvoicePos"),
                        CanCancelClose = ReadBool(reader, "CanCancelClose"),
                        UserCategory = ReadString(reader, "UserCategory")
                    };

                    reader.Close();
                    model.BranchIds = GetLinkedIds(connection, "TblUsersBranches", "BranchID", "userid", userId);
                    model.StoreIds = GetLinkedIds(connection, "TblUsersStores", "StoreID", "userid", userId);
                    model.BoxIds = GetLinkedIds(connection, "TblUsersBoxes", "BoxId", "userid", userId);
                    model.AccountIds = GetLinkedIds(connection, "TblUserAccount", "Account_ID", "UserID", userId);
                    model.ProductLineIds = GetLinkedIds(connection, "TblUsersProductLine", "ProductLineId", "UserID", userId);
                    return model;
                }
            }
        }

        private IList<PosActivityListItem> GetActivities(SqlConnection connection)
        {
            const string sql = @"
SELECT a.id, a.Name, a.namee, COUNT(b.branch_id) AS BranchCount
FROM dbo.tblActivitesType a
LEFT JOIN dbo.TblBranchesData b ON b.ActivityTypeId = a.id
GROUP BY a.id, a.Name, a.namee
ORDER BY a.id;";
            using (var command = new SqlCommand(sql, connection))
            using (var reader = command.ExecuteReader())
            {
                var rows = new List<PosActivityListItem>();
                while (reader.Read())
                {
                    rows.Add(new PosActivityListItem
                    {
                        Id = ReadInt(reader, "id").GetValueOrDefault(),
                        Name = ReadString(reader, "Name"),
                        NameEnglish = ReadString(reader, "namee"),
                        BranchCount = ReadInt(reader, "BranchCount").GetValueOrDefault()
                    });
                }

                return rows;
            }
        }

        private PosActivityEditModel GetActivity(SqlConnection connection, int id)
        {
            using (var command = new SqlCommand("SELECT TOP (1) id, Name, namee FROM dbo.tblActivitesType WHERE id = @Id", connection))
            {
                command.Parameters.Add("@Id", SqlDbType.Int).Value = id;
                using (var reader = command.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        return new PosActivityEditModel();
                    }

                    return new PosActivityEditModel { Id = id, Name = ReadString(reader, "Name"), NameEnglish = ReadString(reader, "namee") };
                }
            }
        }

        private IList<PosBranchDataEditModel> GetBranchesForActivity(SqlConnection connection, int activityId)
        {
            using (var command = new SqlCommand("SELECT * FROM dbo.TblBranchesData WHERE ActivityTypeId = @Id ORDER BY branch_id", connection))
            {
                command.Parameters.Add("@Id", SqlDbType.Int).Value = activityId;
                using (var reader = command.ExecuteReader())
                {
                    var rows = new List<PosBranchDataEditModel>();
                    while (reader.Read())
                    {
                        rows.Add(new PosBranchDataEditModel
                        {
                            BranchId = ReadInt(reader, "branch_id"),
                            ActivityTypeId = ReadInt(reader, "ActivityTypeId"),
                            BranchCode = ReadString(reader, "branch_Code"),
                            BranchName = ReadString(reader, "branch_name"),
                            BranchNameEnglish = ReadString(reader, "branch_namee"),
                            Manager = ReadString(reader, "manger"),
                            Telephone = ReadString(reader, "Tel"),
                            Remarks = ReadString(reader, "Remarks"),
                            AccountCode = ReadString(reader, "Account_Code"),
                            Users = ReadString(reader, "Users"),
                            VatNo = ReadString(reader, "VATNO"),
                            RegionId = ReadInt(reader, "RegionID"),
                            StoreId = ReadInt(reader, "StoreId"),
                            ShowLogoInReports = ReadBool(reader, "ShowlogoInReports"),
                            IsStopped = ReadBool(reader, "IsStoped")
                        });
                    }

                    return rows;
                }
            }
        }

        private void ValidateBranch(SqlConnection connection, SqlTransaction transaction, PosBranchDataEditModel branch, int activityId)
        {
            if (string.IsNullOrWhiteSpace(branch.BranchCode)) { throw new InvalidOperationException("كود الفرع مطلوب."); }
            if (string.IsNullOrWhiteSpace(branch.BranchName)) { throw new InvalidOperationException("اسم الفرع عربي مطلوب."); }

            const string sql = @"
SELECT TOP (1) branch_id
FROM dbo.TblBranchesData
WHERE branch_id <> @BranchId
  AND (branch_Code = @Code OR branch_name = @Name);";
            using (var command = new SqlCommand(sql, connection, transaction))
            {
                command.Parameters.Add("@BranchId", SqlDbType.Int).Value = branch.BranchId.GetValueOrDefault();
                command.Parameters.Add("@Code", SqlDbType.NVarChar, 255).Value = branch.BranchCode.Trim();
                command.Parameters.Add("@Name", SqlDbType.NVarChar, 50).Value = branch.BranchName.Trim();
                var duplicate = command.ExecuteScalar();
                if (duplicate != null && duplicate != DBNull.Value)
                {
                    throw new InvalidOperationException("كود أو اسم الفرع مكرر.");
                }
            }
        }

        private IList<PosLegacyLookupDto> GetBranches(SqlConnection connection)
        {
            return Lookup(connection, "SELECT branch_id AS Id, branch_Code AS Code, COALESCE(NULLIF(branch_name,N''), NULLIF(branch_namee,N''), CONVERT(NVARCHAR(20), branch_id)) AS Name FROM dbo.TblBranchesData ORDER BY branch_id");
        }

        private IList<PosLegacyLookupDto> GetStores(SqlConnection connection)
        {
            return Lookup(connection, "SELECT StoreID AS Id, Code, COALESCE(NULLIF(StoreName,N''), NULLIF(StoreNamee,N''), CONVERT(NVARCHAR(20), StoreID)) AS Name FROM dbo.TblStore ORDER BY StoreID");
        }

        private IList<PosLegacyLookupDto> GetBoxes(SqlConnection connection)
        {
            return Lookup(connection, "SELECT BoxID AS Id, CONVERT(NVARCHAR(50), BoxID) AS Code, COALESCE(NULLIF(BoxName,N''), NULLIF(BoxNameE,N''), CONVERT(NVARCHAR(20), BoxID)) AS Name FROM dbo.TblBoxesData ORDER BY BoxID");
        }

        private IList<PosLegacyLookupDto> GetEmployees(SqlConnection connection)
        {
            return Lookup(connection, "SELECT TOP (1000) Emp_ID AS Id, Fullcode AS Code, COALESCE(NULLIF(Emp_Name,N''), NULLIF(Emp_Namee,N''), CONVERT(NVARCHAR(20), Emp_ID)) AS Name FROM dbo.TblEmployee ORDER BY Emp_ID");
        }

        private IList<PosLegacyLookupDto> GetAccounts(SqlConnection connection)
        {
            return Lookup(connection, "SELECT TOP (1000) Account_ID AS Id, Account_Code AS Code, COALESCE(NULLIF(Account_Name,N''), NULLIF(Account_NameEng,N''), Account_Code) AS Name FROM dbo.ACCOUNTS ORDER BY Account_Code");
        }

        private IList<PosLegacyLookupDto> GetProductLines(SqlConnection connection)
        {
            return Lookup(connection, "SELECT ID AS Id, CONVERT(NVARCHAR(50), ID) AS Code, Name FROM dbo.TblProductLine ORDER BY ID");
        }

        private IList<PosLegacyLookupDto> GetRegions(SqlConnection connection)
        {
            return Lookup(connection, "SELECT Id, CONVERT(NVARCHAR(50), Id) AS Code, COALESCE(NULLIF(Name,N''), NULLIF(NameE,N''), CONVERT(NVARCHAR(20), Id)) AS Name FROM dbo.TblSection ORDER BY Id");
        }

        private IList<PosLegacyLookupDto> Lookup(SqlConnection connection, string sql)
        {
            using (var command = new SqlCommand(sql, connection))
            using (var reader = command.ExecuteReader())
            {
                var rows = new List<PosLegacyLookupDto>();
                while (reader.Read())
                {
                    var code = ReadString(reader, "Code");
                    var name = ReadString(reader, "Name");
                    rows.Add(new PosLegacyLookupDto
                    {
                        Id = ReadInt(reader, "Id").GetValueOrDefault(),
                        Code = code,
                        Name = name,
                        Display = string.IsNullOrWhiteSpace(code) ? name : code + " - " + name
                    });
                }

                return rows;
            }
        }

        private IList<int> GetLinkedIds(SqlConnection connection, string table, string valueColumn, string userColumn, int userId)
        {
            using (var command = new SqlCommand("SELECT " + Quote(valueColumn) + " FROM dbo." + Quote(table) + " WHERE " + Quote(userColumn) + " = @Id ORDER BY 1", connection))
            {
                command.Parameters.Add("@Id", SqlDbType.Int).Value = userId;
                using (var reader = command.ExecuteReader())
                {
                    var rows = new List<int>();
                    while (reader.Read())
                    {
                        if (!reader.IsDBNull(0))
                        {
                            rows.Add(Convert.ToInt32(reader.GetValue(0), CultureInfo.InvariantCulture));
                        }
                    }

                    return rows;
                }
            }
        }

        private bool UserNameExists(SqlConnection connection, SqlTransaction transaction, string userName, int userId)
        {
            using (var command = new SqlCommand("SELECT TOP (1) 1 FROM dbo.TblUsers WHERE UserName = @UserName AND UserID <> @UserID", connection, transaction))
            {
                command.Parameters.Add("@UserName", SqlDbType.NVarChar, 50).Value = userName.Trim();
                command.Parameters.Add("@UserID", SqlDbType.Int).Value = userId;
                return command.ExecuteScalar() != null;
            }
        }

        private bool Exists(SqlConnection connection, SqlTransaction transaction, string table, string keyColumn, int id)
        {
            using (var command = new SqlCommand("SELECT TOP (1) 1 FROM dbo." + Quote(table) + " WHERE " + Quote(keyColumn) + " = @Id", connection, transaction))
            {
                command.Parameters.Add("@Id", SqlDbType.Int).Value = id;
                return command.ExecuteScalar() != null;
            }
        }

        private int NextId(SqlConnection connection, SqlTransaction transaction, string table, string column)
        {
            using (var command = new SqlCommand("SELECT ISNULL(MAX(" + Quote(column) + "), 0) + 1 FROM dbo." + Quote(table) + " WITH (UPDLOCK, HOLDLOCK)", connection, transaction))
            {
                return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture);
            }
        }

        private IDictionary<string, OptionColumn> LoadColumns(SqlConnection connection, SqlTransaction transaction, string table)
        {
            using (var command = new SqlCommand(@"SELECT COLUMN_NAME, DATA_TYPE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = @Table", connection, transaction))
            {
                command.Parameters.Add("@Table", SqlDbType.NVarChar, 128).Value = table;
                using (var reader = command.ExecuteReader())
                {
                    var columns = new Dictionary<string, OptionColumn>(StringComparer.OrdinalIgnoreCase);
                    while (reader.Read())
                    {
                        columns[ReadString(reader, "COLUMN_NAME")] = new OptionColumn { Name = ReadString(reader, "COLUMN_NAME"), DataType = ReadString(reader, "DATA_TYPE") };
                    }

                    return columns;
                }
            }
        }

        private static bool IsRealBranch(PosBranchDataEditModel branch)
        {
            return branch != null && (!string.IsNullOrWhiteSpace(branch.BranchCode) || !string.IsNullOrWhiteSpace(branch.BranchName) || branch.BranchId.GetValueOrDefault() > 0);
        }

        private static void Execute(SqlConnection connection, SqlTransaction transaction, string sql, int id)
        {
            using (var command = new SqlCommand(sql, connection, transaction))
            {
                command.Parameters.Add("@Id", SqlDbType.Int).Value = id;
                command.ExecuteNonQuery();
            }
        }

        private SqlConnection OpenConnection()
        {
            var connection = new SqlConnection(_connectionString);
            connection.Open();
            return connection;
        }

        private static string ReadString(IDataRecord reader, string name)
        {
            var ordinal = SafeOrdinal(reader, name);
            return ordinal < 0 || reader.IsDBNull(ordinal) ? string.Empty : Convert.ToString(reader.GetValue(ordinal), CultureInfo.InvariantCulture);
        }

        private static int? ReadInt(IDataRecord reader, string name)
        {
            var ordinal = SafeOrdinal(reader, name);
            if (ordinal < 0 || reader.IsDBNull(ordinal)) { return null; }
            return Convert.ToInt32(reader.GetValue(ordinal), CultureInfo.InvariantCulture);
        }

        private static decimal? ReadDecimal(IDataRecord reader, string name)
        {
            var ordinal = SafeOrdinal(reader, name);
            if (ordinal < 0 || reader.IsDBNull(ordinal)) { return null; }
            return Convert.ToDecimal(reader.GetValue(ordinal), CultureInfo.InvariantCulture);
        }

        private static bool ReadBool(IDataRecord reader, string name)
        {
            var ordinal = SafeOrdinal(reader, name);
            if (ordinal < 0 || reader.IsDBNull(ordinal)) { return false; }
            var value = reader.GetValue(ordinal);
            if (value is bool) { return (bool)value; }
            return Convert.ToInt32(value, CultureInfo.InvariantCulture) != 0;
        }

        private static int SafeOrdinal(IDataRecord reader, string name)
        {
            for (var i = 0; i < reader.FieldCount; i++)
            {
                if (string.Equals(reader.GetName(i), name, StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }

            return -1;
        }

        private static object DbInt(int? value)
        {
            return value.HasValue && value.Value > 0 ? (object)value.Value : DBNull.Value;
        }

        private static string Trim(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static string Quote(string value)
        {
            return "[" + (value ?? "").Replace("]", "]]") + "]";
        }

        private static PosLegacySaveResult Fail(string message)
        {
            return new PosLegacySaveResult { Success = false, Message = message };
        }

        private class OptionColumn
        {
            public string Name { get; set; }
            public string DataType { get; set; }
        }
    }
}
