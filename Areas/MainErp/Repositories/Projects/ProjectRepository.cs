using System;
using System.Data;
using System.Data.SqlClient;
using MyERP.Areas.MainErp.Interfaces;
using MyERP.Areas.MainErp.ViewModels;
using MyERP.Areas.MainErp.ViewModels.Projects;

namespace MyERP.Areas.MainErp.Repositories.Projects
{
    public class ProjectRepository
    {
        private readonly IMainErpDbConnectionFactory _connectionFactory;

        public ProjectRepository(IMainErpDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public PagedReadResult<ProjectListItemViewModel> Search(string searchText, int? statusId, int? branchId, int page, int pageSize)
        {
            var result = new PagedReadResult<ProjectListItemViewModel>();
            page = page < 1 ? 1 : page;
            pageSize = pageSize < 1 ? 20 : pageSize;

            using (var connection = _connectionFactory.CreateOpenConnection())
            using (var command = new SqlCommand(@"
WITH ProjectRows AS (
    SELECT
        ROW_NUMBER() OVER (ORDER BY p.id DESC) AS RowNo,
        COUNT(1) OVER() AS TotalCount,
        p.id, p.Fullcode, p.Project_name, p.Project_nameE,
        cust.CusName AS CustomerName,
        st.name AS StatusName,
        br.branch_name AS BranchName,
        p.project_cost, p.net, p.cost_after_discount, p.StartDate, p.EndDate
    FROM dbo.projects p
    LEFT JOIN dbo.TblCustemers cust ON cust.CusID = CASE WHEN ISNUMERIC(p.End_user_id) = 1 THEN CONVERT(int, p.End_user_id) END
    LEFT JOIN dbo.project_status st ON st.id = CASE WHEN ISNUMERIC(p.Project_status) = 1 THEN CONVERT(int, p.Project_status) END
    LEFT JOIN dbo.TblBranchesData br ON br.branch_id = p.branch_no
    WHERE (@SearchText IS NULL
           OR p.Fullcode LIKE @SearchLike
           OR p.Code LIKE @SearchLike
           OR p.Project_name LIKE @SearchLike
           OR p.Project_nameE LIKE @SearchLike
           OR cust.CusName LIKE @SearchLike)
      AND (@StatusId IS NULL OR CASE WHEN ISNUMERIC(p.Project_status) = 1 THEN CONVERT(int, p.Project_status) END = @StatusId)
      AND (@BranchId IS NULL OR p.branch_no = @BranchId)
)
SELECT * FROM ProjectRows WHERE RowNo BETWEEN @StartRow AND @EndRow ORDER BY RowNo;", connection))
            {
                command.Parameters.AddWithValue("@SearchText", string.IsNullOrWhiteSpace(searchText) ? (object)DBNull.Value : searchText);
                command.Parameters.AddWithValue("@SearchLike", string.IsNullOrWhiteSpace(searchText) ? (object)DBNull.Value : "%" + searchText + "%");
                command.Parameters.AddWithValue("@StatusId", (object)statusId ?? DBNull.Value);
                command.Parameters.AddWithValue("@BranchId", (object)branchId ?? DBNull.Value);
                command.Parameters.AddWithValue("@StartRow", ((page - 1) * pageSize) + 1);
                command.Parameters.AddWithValue("@EndRow", page * pageSize);

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        if (result.TotalCount == 0 && reader["TotalCount"] != DBNull.Value)
                        {
                            result.TotalCount = Convert.ToInt32(reader["TotalCount"]);
                        }

                        result.Items.Add(new ProjectListItemViewModel
                        {
                            Id = ReadInt(reader, "id").GetValueOrDefault(),
                            FullCode = ReadString(reader, "Fullcode"),
                            ProjectName = ReadString(reader, "Project_name"),
                            ProjectNameEnglish = ReadString(reader, "Project_nameE"),
                            CustomerName = ReadString(reader, "CustomerName"),
                            StatusName = ReadString(reader, "StatusName"),
                            BranchName = ReadString(reader, "BranchName"),
                            ProjectCost = ReadDecimal(reader, "project_cost"),
                            NetCost = ReadDecimal(reader, "net") ?? ReadDecimal(reader, "cost_after_discount"),
                            StartDate = ReadDate(reader, "StartDate"),
                            EndDate = ReadDate(reader, "EndDate")
                        });
                    }
                }
            }

            return result;
        }

        public ProjectEditViewModel New()
        {
            using (var connection = _connectionFactory.CreateOpenConnection())
            {
                var model = new ProjectEditViewModel
                {
                    IsNewProject = true,
                    Id = NextId(connection, null, "projects", "id"),
                    BranchNo = FirstInt(connection, "SELECT TOP 1 branch_id FROM dbo.TblBranchesData ORDER BY branch_id"),
                    StatusId = FirstInt(connection, "SELECT TOP 1 id FROM dbo.project_status ORDER BY id"),
                    CurrencyId = FirstInt(connection, "SELECT TOP 1 id FROM dbo.currency ORDER BY id")
                };
                PopulateLookups(connection, model);
                return model;
            }
        }

        public ProjectEditViewModel Get(int id)
        {
            using (var connection = _connectionFactory.CreateOpenConnection())
            using (var command = new SqlCommand(@"
SELECT TOP 1 *
FROM dbo.projects
WHERE id = @Id;", connection))
            {
                command.Parameters.AddWithValue("@Id", id);
                using (var reader = command.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        return null;
                    }

                    var model = new ProjectEditViewModel
                    {
                        IsNewProject = false,
                        Id = ReadInt(reader, "id").GetValueOrDefault(),
                        Prefix = ReadString(reader, "prifix"),
                        Code = ReadString(reader, "Code"),
                        FullCode = ReadString(reader, "Fullcode"),
                        ProjectName = ReadString(reader, "Project_name"),
                        ProjectNameEnglish = ReadString(reader, "Project_nameE"),
                        EndUserId = ReadNullableIntFromString(reader, "End_user_id"),
                        EndUserAccount = ReadString(reader, "End_user_Account"),
                        SubContractorId = ReadNullableIntFromString(reader, "sub_contractor_id"),
                        SubContractorAccount = ReadString(reader, "sub_contractor_Account"),
                        BranchNo = ReadInt(reader, "branch_no"),
                        StatusId = ReadNullableIntFromString(reader, "Project_status"),
                        ContractType = ReadString(reader, "Contract_type"),
                        CurrencyId = ReadInt(reader, "CurrencyID"),
                        ProjectCost = ReadDecimal(reader, "project_cost"),
                        GeneralDiscount = ReadDecimal(reader, "general_discount"),
                        DiscountPercentage = ReadDecimal(reader, "DiscountPercentage"),
                        CostAfterDiscount = ReadDecimal(reader, "cost_after_discount"),
                        StartDate = ReadDate(reader, "StartDate"),
                        EndDate = ReadDate(reader, "EndDate"),
                        NearEndDate = ReadDate(reader, "DpNearEndDate"),
                        ManagerEmployeeId = ReadInt(reader, "EmpId"),
                        SalesEmployeeId = ReadInt(reader, "EmpId1"),
                        DepartmentId = ReadInt(reader, "Dept_ID"),
                        PState = ReadInt(reader, "Pstate"),
                        UnderImplementation = ReadInt(reader, "UnderImp"),
                        ContractNo = ReadString(reader, "ContractNo"),
                        Insurance = ReadDouble(reader, "Insurance"),
                        Remarks = ReadString(reader, "Remarkss")
                    };

                    reader.Close();
                    PopulateLookups(connection, model);
                    return model;
                }
            }
        }

        public int Save(ProjectEditViewModel model)
        {
            using (var connection = _connectionFactory.CreateOpenConnection())
            using (var transaction = connection.BeginTransaction())
            {
                if (model.Id <= 0)
                {
                    model.Id = NextId(connection, transaction, "projects", "id");
                }

                model.Code = string.IsNullOrWhiteSpace(model.Code) ? model.Id.ToString() : model.Code.Trim();
                model.Prefix = string.IsNullOrWhiteSpace(model.Prefix) ? null : model.Prefix.Trim();
                model.FullCode = (model.Prefix ?? string.Empty) + model.Code;
                model.ProjectCost = model.ProjectCost ?? 0m;
                model.GeneralDiscount = model.GeneralDiscount ?? 0m;
                model.CostAfterDiscount = Math.Max(0m, model.ProjectCost.Value - model.GeneralDiscount.Value);

                EnsureUniqueCode(connection, transaction, model.Id, model.FullCode);

                var exists = Exists(connection, transaction, model.Id);
                var sql = exists ? UpdateSql : InsertSql;
                using (var command = new SqlCommand(sql, connection, transaction))
                {
                    AddParameters(command, model);
                    command.ExecuteNonQuery();
                }

                transaction.Commit();
                return model.Id;
            }
        }

        public ProjectExtractCreateViewModel BuildExtractCreateModel(int projectId)
        {
            var project = Get(projectId);
            if (project == null)
            {
                return null;
            }

            return new ProjectExtractCreateViewModel
            {
                ProjectId = project.Id,
                ProjectName = project.ProjectName,
                ProjectFullCode = project.FullCode,
                BranchNo = project.BranchNo,
                Total = 0m,
                VatValue = 0m,
                NetValue = 0m
            };
        }

        public int CreateExtract(ProjectExtractCreateViewModel model, int? userId)
        {
            using (var connection = _connectionFactory.CreateOpenConnection())
            using (var transaction = connection.BeginTransaction())
            {
                var project = GetProjectHeader(connection, transaction, model.ProjectId.GetValueOrDefault());
                if (project == null)
                {
                    throw new InvalidOperationException("المشروع المحدد غير موجود.");
                }

                var id = NextId(connection, transaction, "project_billl", "id");
                var noteSerial = NextId(connection, transaction, "project_billl", "NoteSerial").ToString();
                var total = model.Total.GetValueOrDefault();
                var vat = model.VatValue.GetValueOrDefault();
                var net = model.NetValue.GetValueOrDefault();
                if (net == 0m)
                {
                    net = total + vat;
                }

                using (var command = new SqlCommand(@"
INSERT INTO dbo.project_billl
(
    id, bill_date, project_no, project_name, total, FATValue, NetValue,
    Branch_NO, ManualNO, NoteSerial, Results, UserID, Remarks, StartDateProje
)
VALUES
(
    @Id, @BillDate, @ProjectNo, @ProjectName, @Total, @VatValue, @NetValue,
    @BranchNo, @ManualNo, @NoteSerial, 0, @UserId, @Remarks, @StartDate
);", connection, transaction))
                {
                    command.Parameters.AddWithValue("@Id", id);
                    command.Parameters.AddWithValue("@BillDate", (object)model.BillDate ?? DBNull.Value);
                    command.Parameters.AddWithValue("@ProjectNo", project.Id.ToString());
                    command.Parameters.AddWithValue("@ProjectName", (object)project.Name ?? DBNull.Value);
                    command.Parameters.AddWithValue("@Total", total);
                    command.Parameters.AddWithValue("@VatValue", vat);
                    command.Parameters.AddWithValue("@NetValue", net);
                    command.Parameters.AddWithValue("@BranchNo", (object)(model.BranchNo ?? project.BranchNo) ?? DBNull.Value);
                    command.Parameters.AddWithValue("@ManualNo", (object)model.ManualNo ?? DBNull.Value);
                    command.Parameters.AddWithValue("@NoteSerial", noteSerial);
                    command.Parameters.AddWithValue("@UserId", (object)userId ?? DBNull.Value);
                    command.Parameters.AddWithValue("@Remarks", (object)model.Remarks ?? DBNull.Value);
                    command.Parameters.AddWithValue("@StartDate", (object)project.StartDate ?? DBNull.Value);
                    command.ExecuteNonQuery();
                }

                transaction.Commit();
                return id;
            }
        }

        public void PopulateLookups(ProjectEditViewModel model)
        {
            using (var connection = _connectionFactory.CreateOpenConnection())
            {
                PopulateLookups(connection, model);
            }
        }

        public void PopulateIndexLookups(ProjectsIndexViewModel model)
        {
            using (var connection = _connectionFactory.CreateOpenConnection())
            {
                LoadLookup(connection, "SELECT id, name FROM dbo.project_status ORDER BY id", model.Statuses);
                LoadLookup(connection, "SELECT branch_id, branch_name FROM dbo.TblBranchesData ORDER BY branch_id", model.Branches);
            }
        }

        private void PopulateLookups(SqlConnection connection, ProjectEditViewModel model)
        {
            LoadLookup(connection, "SELECT id, name FROM dbo.project_status ORDER BY id", model.Statuses);
            LoadLookup(connection, "SELECT branch_id, branch_name FROM dbo.TblBranchesData ORDER BY branch_id", model.Branches);
            LoadLookup(connection, "SELECT TOP 200 id, COALESCE(name, nameE, CONVERT(nvarchar(20), id)) FROM dbo.currency ORDER BY id", model.Currencies);
            LoadLookup(connection, "SELECT TOP 200 id, COALESCE(name, namee, CONVERT(nvarchar(20), id)) FROM dbo.contract_type ORDER BY id", model.ContractTypes);
            LoadLookup(connection, "SELECT TOP 300 CusID, COALESCE(CusName, CusNamee, CONVERT(nvarchar(20), CusID)) FROM dbo.TblCustemers ORDER BY CusName", model.Customers);
            LoadLookup(connection, "SELECT TOP 300 Account_Code, COALESCE(Account_Serial + N' - ', N'') + COALESCE(Account_Name, Account_NameEng, Account_Code) FROM dbo.ACCOUNTS WHERE ISNULL(last_account, 0) = 1 ORDER BY Account_Serial", model.Accounts);
            LoadLookup(connection, "SELECT TOP 300 Emp_ID, COALESCE(Emp_Name, Emp_Namee, CONVERT(nvarchar(20), Emp_ID)) FROM dbo.TblEmployee ORDER BY Emp_Name", model.Employees);
        }

        private static void LoadLookup(SqlConnection connection, string sql, System.Collections.Generic.IList<ProjectLookupItem> target)
        {
            using (var command = new SqlCommand(sql, connection))
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    target.Add(new ProjectLookupItem
                    {
                        Value = Convert.ToString(reader.GetValue(0)),
                        Text = reader.IsDBNull(1) ? Convert.ToString(reader.GetValue(0)) : Convert.ToString(reader.GetValue(1))
                    });
                }
            }
        }

        private static bool Exists(SqlConnection connection, SqlTransaction transaction, int id)
        {
            using (var command = new SqlCommand("SELECT COUNT(1) FROM dbo.projects WHERE id = @Id", connection, transaction))
            {
                command.Parameters.AddWithValue("@Id", id);
                return Convert.ToInt32(command.ExecuteScalar()) > 0;
            }
        }

        private static int NextId(SqlConnection connection, SqlTransaction transaction, string table, string column)
        {
            using (var command = new SqlCommand("SELECT ISNULL(MAX(CASE WHEN ISNUMERIC(" + column + ") = 1 THEN CONVERT(int, " + column + ") END), 0) + 1 FROM dbo." + table + " WITH (UPDLOCK, HOLDLOCK)", connection, transaction))
            {
                return Convert.ToInt32(command.ExecuteScalar());
            }
        }

        private static int? FirstInt(SqlConnection connection, string sql)
        {
            using (var command = new SqlCommand(sql, connection))
            {
                var value = command.ExecuteScalar();
                return value == null || value == DBNull.Value ? (int?)null : Convert.ToInt32(value);
            }
        }

        private static void EnsureUniqueCode(SqlConnection connection, SqlTransaction transaction, int id, string fullCode)
        {
            if (string.IsNullOrWhiteSpace(fullCode))
            {
                throw new InvalidOperationException("كود المشروع مطلوب.");
            }

            using (var command = new SqlCommand("SELECT TOP 1 Project_name FROM dbo.projects WHERE id <> @Id AND Fullcode = @FullCode", connection, transaction))
            {
                command.Parameters.AddWithValue("@Id", id);
                command.Parameters.AddWithValue("@FullCode", fullCode);
                var existing = command.ExecuteScalar();
                if (existing != null && existing != DBNull.Value)
                {
                    throw new InvalidOperationException("لا يمكن تكرار كود المشروع. الكود مستخدم في مشروع: " + Convert.ToString(existing));
                }
            }
        }

        private static ProjectHeader GetProjectHeader(SqlConnection connection, SqlTransaction transaction, int id)
        {
            using (var command = new SqlCommand("SELECT TOP 1 id, Project_name, Fullcode, branch_no, StartDate FROM dbo.projects WHERE id = @Id", connection, transaction))
            {
                command.Parameters.AddWithValue("@Id", id);
                using (var reader = command.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        return null;
                    }

                    return new ProjectHeader
                    {
                        Id = ReadInt(reader, "id").GetValueOrDefault(),
                        Name = ReadString(reader, "Project_name"),
                        FullCode = ReadString(reader, "Fullcode"),
                        BranchNo = ReadInt(reader, "branch_no"),
                        StartDate = ReadDate(reader, "StartDate")
                    };
                }
            }
        }

        private static void AddParameters(SqlCommand command, ProjectEditViewModel model)
        {
            command.Parameters.AddWithValue("@Id", model.Id);
            command.Parameters.AddWithValue("@EndUserAccount", (object)model.EndUserAccount ?? DBNull.Value);
            command.Parameters.AddWithValue("@EndUserName", DBNull.Value);
            command.Parameters.AddWithValue("@SubContractorAccount", (object)model.SubContractorAccount ?? DBNull.Value);
            command.Parameters.AddWithValue("@SubContractorName", DBNull.Value);
            command.Parameters.AddWithValue("@FullCode", (object)model.FullCode ?? DBNull.Value);
            command.Parameters.AddWithValue("@Prefix", (object)model.Prefix ?? DBNull.Value);
            command.Parameters.AddWithValue("@Code", (object)model.Code ?? DBNull.Value);
            command.Parameters.AddWithValue("@ProjectName", (object)model.ProjectName ?? DBNull.Value);
            command.Parameters.AddWithValue("@ProjectNameEnglish", (object)model.ProjectNameEnglish ?? DBNull.Value);
            command.Parameters.AddWithValue("@ContractType", (object)model.ContractType ?? DBNull.Value);
            command.Parameters.AddWithValue("@ProjectStatus", (object)model.StatusId ?? DBNull.Value);
            command.Parameters.AddWithValue("@ProjectCost", (object)model.ProjectCost ?? DBNull.Value);
            command.Parameters.AddWithValue("@BranchNo", (object)model.BranchNo ?? DBNull.Value);
            command.Parameters.AddWithValue("@EndUserId", (object)model.EndUserId ?? DBNull.Value);
            command.Parameters.AddWithValue("@SubContractorId", (object)model.SubContractorId ?? DBNull.Value);
            command.Parameters.AddWithValue("@GeneralDiscount", (object)model.GeneralDiscount ?? DBNull.Value);
            command.Parameters.AddWithValue("@CostAfterDiscount", (object)model.CostAfterDiscount ?? DBNull.Value);
            command.Parameters.AddWithValue("@Net", (object)model.CostAfterDiscount ?? DBNull.Value);
            command.Parameters.AddWithValue("@CurrencyId", (object)model.CurrencyId ?? DBNull.Value);
            command.Parameters.AddWithValue("@StartDate", (object)model.StartDate ?? DBNull.Value);
            command.Parameters.AddWithValue("@EndDate", (object)model.EndDate ?? DBNull.Value);
            command.Parameters.AddWithValue("@DiscountPercentage", (object)model.DiscountPercentage ?? DBNull.Value);
            command.Parameters.AddWithValue("@DeptId", (object)model.DepartmentId ?? DBNull.Value);
            command.Parameters.AddWithValue("@Remarks", (object)model.Remarks ?? DBNull.Value);
            command.Parameters.AddWithValue("@NearEndDate", (object)model.NearEndDate ?? DBNull.Value);
            command.Parameters.AddWithValue("@EmpId", (object)model.ManagerEmployeeId ?? DBNull.Value);
            command.Parameters.AddWithValue("@EmpId1", (object)model.SalesEmployeeId ?? DBNull.Value);
            command.Parameters.AddWithValue("@PState", (object)model.PState ?? DBNull.Value);
            command.Parameters.AddWithValue("@ContractNo", (object)model.ContractNo ?? DBNull.Value);
            command.Parameters.AddWithValue("@UnderImp", (object)model.UnderImplementation ?? DBNull.Value);
            command.Parameters.AddWithValue("@Insurance", (object)model.Insurance ?? DBNull.Value);
        }

        private const string InsertSql = @"
INSERT INTO dbo.projects
(
    id, End_user_Account, End_user_name, sub_contractor_Account, sub_contractor_name,
    Fullcode, prifix, Code, Project_name, Project_nameE, Contract_type, Project_status,
    project_cost, branch_no, End_user_id, sub_contractor_id, general_discount,
    cost_after_discount, net, CurrencyID, StartDate, EndDate, DiscountPercentage,
    Dept_ID, Remarkss, DpNearEndDate, EmpId, EmpId1, Pstate, ContractNo, UnderImp, Insurance
)
VALUES
(
    @Id, @EndUserAccount, @EndUserName, @SubContractorAccount, @SubContractorName,
    @FullCode, @Prefix, @Code, @ProjectName, @ProjectNameEnglish, @ContractType, @ProjectStatus,
    @ProjectCost, @BranchNo, @EndUserId, @SubContractorId, @GeneralDiscount,
    @CostAfterDiscount, @Net, @CurrencyId, @StartDate, @EndDate, @DiscountPercentage,
    @DeptId, @Remarks, @NearEndDate, @EmpId, @EmpId1, @PState, @ContractNo, @UnderImp, @Insurance
);";

        private const string UpdateSql = @"
UPDATE dbo.projects SET
    End_user_Account = @EndUserAccount,
    End_user_name = @EndUserName,
    sub_contractor_Account = @SubContractorAccount,
    sub_contractor_name = @SubContractorName,
    Fullcode = @FullCode,
    prifix = @Prefix,
    Code = @Code,
    Project_name = @ProjectName,
    Project_nameE = @ProjectNameEnglish,
    Contract_type = @ContractType,
    Project_status = @ProjectStatus,
    project_cost = @ProjectCost,
    branch_no = @BranchNo,
    End_user_id = @EndUserId,
    sub_contractor_id = @SubContractorId,
    general_discount = @GeneralDiscount,
    cost_after_discount = @CostAfterDiscount,
    net = @Net,
    CurrencyID = @CurrencyId,
    StartDate = @StartDate,
    EndDate = @EndDate,
    DiscountPercentage = @DiscountPercentage,
    Dept_ID = @DeptId,
    Remarkss = @Remarks,
    DpNearEndDate = @NearEndDate,
    EmpId = @EmpId,
    EmpId1 = @EmpId1,
    Pstate = @PState,
    ContractNo = @ContractNo,
    UnderImp = @UnderImp,
    Insurance = @Insurance
WHERE id = @Id;";

        private static string ReadString(IDataRecord reader, string column)
        {
            var ordinal = reader.GetOrdinal(column);
            return reader.IsDBNull(ordinal) ? string.Empty : Convert.ToString(reader.GetValue(ordinal));
        }

        private static int? ReadInt(IDataRecord reader, string column)
        {
            var ordinal = reader.GetOrdinal(column);
            return reader.IsDBNull(ordinal) ? (int?)null : Convert.ToInt32(reader.GetValue(ordinal));
        }

        private static int? ReadNullableIntFromString(IDataRecord reader, string column)
        {
            int value;
            var text = ReadString(reader, column);
            return int.TryParse(text, out value) ? value : (int?)null;
        }

        private static DateTime? ReadDate(IDataRecord reader, string column)
        {
            var ordinal = reader.GetOrdinal(column);
            return reader.IsDBNull(ordinal) ? (DateTime?)null : Convert.ToDateTime(reader.GetValue(ordinal));
        }

        private static decimal? ReadDecimal(IDataRecord reader, string column)
        {
            var ordinal = reader.GetOrdinal(column);
            return reader.IsDBNull(ordinal) ? (decimal?)null : Convert.ToDecimal(reader.GetValue(ordinal));
        }

        private static double? ReadDouble(IDataRecord reader, string column)
        {
            var ordinal = reader.GetOrdinal(column);
            return reader.IsDBNull(ordinal) ? (double?)null : Convert.ToDouble(reader.GetValue(ordinal));
        }

        private class ProjectHeader
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string FullCode { get; set; }
            public int? BranchNo { get; set; }
            public DateTime? StartDate { get; set; }
        }
    }
}
