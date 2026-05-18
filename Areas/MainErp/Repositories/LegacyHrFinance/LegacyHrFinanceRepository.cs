using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using MyERP.Areas.MainErp.Interfaces;
using MyERP.Areas.MainErp.ViewModels.LegacyHrFinance;

namespace MyERP.Areas.MainErp.Repositories.LegacyHrFinance
{
    public class LegacyHrFinanceRepository
    {
        private readonly IMainErpDbConnectionFactory _connectionFactory;

        public LegacyHrFinanceRepository(IMainErpDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public LegacyHrFinancePageViewModel Load(string moduleKey, string searchText, int page, int pageSize, string employeeStatus = "active")
        {
            page = page <= 0 ? 1 : page;
            pageSize = pageSize <= 0 || pageSize > 100 ? 40 : pageSize;
            searchText = (searchText ?? string.Empty).Trim();
            employeeStatus = NormalizeEmployeeStatus(employeeStatus);

            using (var connection = _connectionFactory.CreateOpenConnection())
            {
                switch ((moduleKey ?? string.Empty).ToLowerInvariant())
                {
                    case "components": return LoadComponents(connection, searchText, page, pageSize);
                    case "payroll-items": return LoadComponents(connection, searchText, page, pageSize);
                    case "advances": return LoadAdvances(connection, searchText, page, pageSize, employeeStatus);
                    case "leave": return LoadLeaveEntitlements(connection, searchText, page, pageSize, employeeStatus);
                    case "sickleave": return LoadSickLeaves(connection, searchText, page, pageSize, employeeStatus);
                    case "adjustments": return LoadAdjustments(connection, searchText, page, pageSize, employeeStatus);
                    case "allocations": return LoadAllocations(connection, searchText, page, pageSize);
                    case "absences": return LoadAbsences(connection, searchText, page, pageSize, employeeStatus);
                    case "vacations": return LoadVacations(connection, searchText, page, pageSize, employeeStatus);
                    case "allowances": return LoadAllowances(connection, searchText, page, pageSize);
                    case "end-service": return LoadEndOfService(connection, searchText, page, pageSize, employeeStatus);
                    default: return LoadComponents(connection, searchText, page, pageSize);
                }
            }
        }

        public PayrollComponentEditViewModel GetComponent(int id)
        {
            using (var connection = _connectionFactory.CreateOpenConnection())
            using (var command = new SqlCommand("SELECT TOP (1) * FROM dbo.mofrad WITH (NOLOCK) WHERE id = @Id", connection))
            {
                command.Parameters.Add("@Id", SqlDbType.Int).Value = id;
                using (var reader = command.ExecuteReader())
                {
                    return reader.Read() ? MapComponent(reader) : null;
                }
            }
        }

        public LegacyHrFinanceSaveResult SaveComponent(PayrollComponentEditViewModel request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Name))
            {
                return Fail("اسم المكون مطلوب.");
            }

            using (var connection = _connectionFactory.CreateOpenConnection())
            using (var transaction = connection.BeginTransaction())
            {
                var id = request.Id.GetValueOrDefault();
                if (DuplicateExists(connection, transaction, id, request.Name))
                {
                    return Fail("اسم المكون موجود من قبل.");
                }

                if (id <= 0)
                {
                    id = NextId(connection, transaction, "mofrad", "id");
                    using (var command = new SqlCommand(@"
INSERT INTO dbo.mofrad
(id, name, nameE, AddOrDiscount, FixedOrChanged, Unit, Account_Code, Account_code1, ViewComp, Salary, Absence, Late, OverTime, Insurances, Reward, AllowIntrod)
VALUES
(@Id, @Name, @NameE, @AddOrDiscount, @FixedOrChanged, @Unit, @AccountCode, @AccountCode1, @ViewComp, @Salary, @Absence, @Late, @OverTime, @Insurances, @Reward, @AllowIntrod);", connection, transaction))
                    {
                        AddComponentParameters(command, request, id);
                        command.ExecuteNonQuery();
                    }
                }
                else
                {
                    using (var command = new SqlCommand(@"
UPDATE dbo.mofrad
SET name = @Name,
    nameE = @NameE,
    AddOrDiscount = @AddOrDiscount,
    FixedOrChanged = @FixedOrChanged,
    Unit = @Unit,
    Account_Code = @AccountCode,
    Account_code1 = @AccountCode1,
    ViewComp = @ViewComp,
    Salary = @Salary,
    Absence = @Absence,
    Late = @Late,
    OverTime = @OverTime,
    Insurances = @Insurances,
    Reward = @Reward,
    AllowIntrod = @AllowIntrod
WHERE id = @Id;", connection, transaction))
                    {
                        AddComponentParameters(command, request, id);
                        command.ExecuteNonQuery();
                    }
                }

                transaction.Commit();
                return new LegacyHrFinanceSaveResult { Success = true, Id = id, Message = "تم حفظ مكون الراتب." };
            }
        }

        private LegacyHrFinancePageViewModel LoadComponents(SqlConnection connection, string searchText, int page, int pageSize)
        {
            var model = Base("payroll-items", "مفردات الرواتب", "Enterprise HR", "مفردات الرواتب", "mofrad", null, searchText, page, pageSize, "all");
            model.Components = new List<PayrollComponentEditViewModel>();
            using (var command = new SqlCommand(@"
SELECT * FROM (
  SELECT ROW_NUMBER() OVER (ORDER BY id) RowNo, *
  FROM dbo.mofrad WITH (NOLOCK)
  WHERE @Search = N'' OR ISNULL(name, N'') LIKE N'%' + @Search + N'%' OR ISNULL(nameE, N'') LIKE N'%' + @Search + N'%' OR ISNULL(Account_Code, N'') LIKE N'%' + @Search + N'%'
) q WHERE RowNo BETWEEN @Start AND @End ORDER BY RowNo;", connection))
            {
                AddSearch(command, searchText, page, pageSize);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        model.Components.Add(MapComponent(reader));
                    }
                }
            }

            model.Metrics.Add(Metric("إجمالي المكونات", Scalar(connection, "SELECT COUNT(1) FROM dbo.mofrad").ToString(), "MOFRAD"));
            model.Metrics.Add(Metric("إضافات", Scalar(connection, "SELECT COUNT(1) FROM dbo.mofrad WHERE ISNULL(AddOrDiscount,0)=1").ToString(), "AddOrDiscount"));
            model.Metrics.Add(Metric("خصومات", Scalar(connection, "SELECT COUNT(1) FROM dbo.mofrad WHERE ISNULL(Discount,0)=1 OR ISNULL(AddOrDiscount,0)=0").ToString(), "Legacy flags"));
            return model;
        }

        private LegacyHrFinancePageViewModel LoadAdvances(SqlConnection connection, string searchText, int page, int pageSize, string employeeStatus)
        {
            var model = Base("advances", "السلف", "شؤون الموظفين", "السلف", "TblEmpAdvanceRequest", null, searchText, page, pageSize, employeeStatus);
            using (var command = new SqlCommand(@"
SELECT * FROM (
 SELECT ROW_NUMBER() OVER (ORDER BY a.AdvanceID DESC) RowNo, a.AdvanceID, e.Emp_Name, a.AdvanceDate, a.AdvanceValue, a.PaymentCounts, a.Approved, a.Posted, a.AccAproved, a.reason
 FROM dbo.TblEmpAdvanceRequest a WITH (NOLOCK)
 LEFT JOIN dbo.TblEmployee e WITH (NOLOCK) ON e.Emp_ID = CONVERT(INT, a.Emp_id)
 WHERE " + EmployeeStatusPredicate("e") + @"
   AND (@Search = N'' OR ISNULL(e.Emp_Name,N'') LIKE N'%' + @Search + N'%' OR CONVERT(NVARCHAR(30), a.AdvanceID) = @Search)
) q WHERE RowNo BETWEEN @Start AND @End ORDER BY RowNo;", connection))
            {
                AddSearch(command, searchText, page, pageSize);
                AddEmployeeStatus(command, employeeStatus);
                FillRows(command, model, "AdvanceID", "Emp_Name", "reason", "AdvanceValue", "AdvanceDate", "Approved", "Posted", "AccAproved");
            }
            AddCountMetric(connection, model, "TblEmpAdvanceRequest");
            return model;
        }

        private LegacyHrFinancePageViewModel LoadLeaveEntitlements(SqlConnection connection, string searchText, int page, int pageSize, string employeeStatus)
        {
            var model = Base("leave", "مستحقات الإجازات", "شؤون الموظفين", "مستحقات الإجازات", "TblVocationEntitlements", null, searchText, page, pageSize, employeeStatus);
            using (var command = new SqlCommand(@"
SELECT * FROM (
 SELECT ROW_NUMBER() OVER (ORDER BY v.ID DESC) RowNo, v.ID, e.Emp_Name, v.RecordDate, v.SalaryVocation, v.TotalDay, v.Remark, v.Booked, v.Delivery
 FROM dbo.TblVocationEntitlements v WITH (NOLOCK)
 LEFT JOIN dbo.TblEmployee e WITH (NOLOCK) ON e.Emp_ID = v.EmpID
 WHERE " + EmployeeStatusPredicate("e") + @"
   AND (@Search = N'' OR ISNULL(e.Emp_Name,N'') LIKE N'%' + @Search + N'%' OR CONVERT(NVARCHAR(30), v.ID) = @Search)
) q WHERE RowNo BETWEEN @Start AND @End ORDER BY RowNo;", connection))
            {
                AddSearch(command, searchText, page, pageSize);
                AddEmployeeStatus(command, employeeStatus);
                FillRows(command, model, "ID", "Emp_Name", "Remark", "SalaryVocation", "RecordDate", "Booked", "Delivery", null);
            }
            AddCountMetric(connection, model, "TblVocationEntitlements");
            return model;
        }

        private LegacyHrFinancePageViewModel LoadSickLeaves(SqlConnection connection, string searchText, int page, int pageSize, string employeeStatus)
        {
            var model = Base("sickleave", "الإجازات المرضية", "شؤون الموظفين", "الإجازات المرضية", "TblRegsterSickleave", null, searchText, page, pageSize, employeeStatus);
            using (var command = new SqlCommand(@"
SELECT * FROM (
 SELECT ROW_NUMBER() OVER (ORDER BY s.ID DESC) RowNo, s.ID, e.Emp_Name, s.RecordDate, s.LastNoDay, s.Remarks, s.FrmDate, s.ToDate
 FROM dbo.TblRegsterSickleave s WITH (NOLOCK)
 LEFT JOIN dbo.TblEmployee e WITH (NOLOCK) ON e.Emp_ID = s.EmpID
 WHERE " + EmployeeStatusPredicate("e") + @"
   AND (@Search = N'' OR ISNULL(e.Emp_Name,N'') LIKE N'%' + @Search + N'%' OR CONVERT(NVARCHAR(30), s.ID) = @Search)
) q WHERE RowNo BETWEEN @Start AND @End ORDER BY RowNo;", connection))
            {
                AddSearch(command, searchText, page, pageSize);
                AddEmployeeStatus(command, employeeStatus);
                FillRows(command, model, "ID", "Emp_Name", "Remarks", "LastNoDay", "RecordDate", "FrmDate", "ToDate", null);
            }
            AddCountMetric(connection, model, "TblRegsterSickleave");
            return model;
        }

        private LegacyHrFinancePageViewModel LoadAdjustments(SqlConnection connection, string searchText, int page, int pageSize, string employeeStatus)
        {
            var model = Base("adjustments", "تعديلات مفردات الرواتب", "شؤون الموظفين", "تعديلات مفردات الرواتب", "TblChangedComponentRegister / Details", null, searchText, page, pageSize, employeeStatus);
            using (var command = new SqlCommand(@"
SELECT * FROM (
 SELECT ROW_NUMBER() OVER (ORDER BY r.ChangedComponentid DESC) RowNo, r.ChangedComponentid, m.name, r.RecordDate, r.[year], r.[month], r.Reason, COUNT(d.id) DetailCount, SUM(ISNULL(d.value,0)) TotalValue
 FROM dbo.TblChangedComponentRegister r WITH (NOLOCK)
 LEFT JOIN dbo.mofrad m WITH (NOLOCK) ON m.id = r.ComponentID
 LEFT JOIN dbo.TblChangedComponentRegisterDetails d WITH (NOLOCK) ON d.ChangedComponentid = r.ChangedComponentid
 WHERE @Search = N'' OR ISNULL(m.name,N'') LIKE N'%' + @Search + N'%' OR CONVERT(NVARCHAR(30), r.ChangedComponentid) = @Search
 GROUP BY r.ChangedComponentid, m.name, r.RecordDate, r.[year], r.[month], r.Reason
) q WHERE RowNo BETWEEN @Start AND @End ORDER BY RowNo;", connection))
            {
                AddSearch(command, searchText, page, pageSize);
                FillRows(command, model, "ChangedComponentid", "name", "Reason", "TotalValue", "RecordDate", "year", "month", "DetailCount");
            }
            AddCountMetric(connection, model, "TblChangedComponentRegister");
            return model;
        }

        private LegacyHrFinancePageViewModel LoadAllocations(SqlConnection connection, string searchText, int page, int pageSize)
        {
            var model = Base("allocations", "توزيعات واستقطاعات الموظفين", "الموارد البشرية", "توزيعات واستقطاعات الموظفين", "TblEmpAllocations / Details / notes / DOUBLE_ENTREY_VOUCHERS", "هذه الشاشة محمية لأن التنفيذ الكامل يحتاج مراجعة الترحيل قبل فتح الحفظ.", searchText, page, pageSize);
            using (var command = new SqlCommand(@"
SELECT * FROM (
 SELECT ROW_NUMBER() OVER (ORDER BY a.EmpAllocationsid DESC) RowNo, a.EmpAllocationsid, a.RecordDate, a.[year], a.[month], a.AllocationsType, a.NoteSerial, COUNT(d.id) DetailCount, SUM(ISNULL(d.value,0)) TotalValue
 FROM dbo.TblEmpAllocations a WITH (NOLOCK)
 LEFT JOIN dbo.TblEmpAllocationsDetails d WITH (NOLOCK) ON d.EmpAllocationsid = a.EmpAllocationsid
 WHERE @Search = N'' OR ISNULL(a.NoteSerial,N'') LIKE N'%' + @Search + N'%' OR CONVERT(NVARCHAR(30), a.EmpAllocationsid) = @Search
 GROUP BY a.EmpAllocationsid, a.RecordDate, a.[year], a.[month], a.AllocationsType, a.NoteSerial
) q WHERE RowNo BETWEEN @Start AND @End ORDER BY RowNo;", connection))
            {
                AddSearch(command, searchText, page, pageSize);
                FillRows(command, model, "EmpAllocationsid", "NoteSerial", "AllocationsType", "TotalValue", "RecordDate", "year", "month", "DetailCount");
            }
            AddCountMetric(connection, model, "TblEmpAllocations");
            return model;
        }

        private LegacyHrFinancePageViewModel LoadAbsences(SqlConnection connection, string searchText, int page, int pageSize, string employeeStatus)
        {
            var model = Base("absences", "الغياب", "شؤون الموظفين", "الغياب", "tblAbsent / tblJunkAbsent", null, searchText, page, pageSize, employeeStatus);
            using (var command = new SqlCommand(@"
SELECT * FROM (
 SELECT ROW_NUMBER() OVER (ORDER BY a.Abs_Date DESC, a.Abs_ID DESC) RowNo,
        a.Abs_ID, e.Emp_Name, a.Abs_Date, a.Abs_Code, a.UserID
 FROM dbo.tblAbsent a WITH (NOLOCK)
 LEFT JOIN dbo.tblJunkAbsent j WITH (NOLOCK) ON j.Abs_ID = a.Abs_ID
 LEFT JOIN dbo.TblEmployee e WITH (NOLOCK) ON e.Emp_ID = j.Emp_ID
 WHERE " + EmployeeStatusPredicate("e") + @"
   AND (@Search = N'' OR ISNULL(e.Emp_Name,N'') LIKE N'%' + @Search + N'%' OR ISNULL(a.Abs_Code,N'') LIKE N'%' + @Search + N'%' OR CONVERT(NVARCHAR(30), a.Abs_ID) = @Search)
) q WHERE RowNo BETWEEN @Start AND @End ORDER BY RowNo;", connection))
            {
                AddSearch(command, searchText, page, pageSize);
                AddEmployeeStatus(command, employeeStatus);
                FillRows(command, model, "Abs_ID", "Emp_Name", "Abs_Code", "UserID", "Abs_Date", null, null, null);
            }
            AddCountMetric(connection, model, "tblAbsent");
            return model;
        }

        private LegacyHrFinancePageViewModel LoadVacations(SqlConnection connection, string searchText, int page, int pageSize, string employeeStatus)
        {
            var model = Base("vacations", "الإجازات", "شؤون الموظفين", "الإجازات", "TblVocation", null, searchText, page, pageSize, employeeStatus);
            using (var command = new SqlCommand(@"
SELECT * FROM (
 SELECT ROW_NUMBER() OVER (ORDER BY v.ID DESC) RowNo,
        v.ID, e.Emp_Name, v.RecordDate, v.FromDate, v.ToDate, v.Reson, v.Approved, v.posted
 FROM dbo.TblVocation v WITH (NOLOCK)
 LEFT JOIN dbo.TblEmployee e WITH (NOLOCK) ON e.Emp_ID = v.EmpID
 WHERE " + EmployeeStatusPredicate("e") + @"
   AND (@Search = N'' OR ISNULL(e.Emp_Name,N'') LIKE N'%' + @Search + N'%' OR ISNULL(v.Reson,N'') LIKE N'%' + @Search + N'%' OR CONVERT(NVARCHAR(30), v.ID) = @Search)
) q WHERE RowNo BETWEEN @Start AND @End ORDER BY RowNo;", connection))
            {
                AddSearch(command, searchText, page, pageSize);
                AddEmployeeStatus(command, employeeStatus);
                FillRows(command, model, "ID", "Emp_Name", "Reson", "posted", "FromDate", "ToDate", "Approved", null);
            }
            AddCountMetric(connection, model, "TblVocation");
            return model;
        }

        private LegacyHrFinancePageViewModel LoadAllowances(SqlConnection connection, string searchText, int page, int pageSize)
        {
            var model = Base("allowances", "البدلات", "شؤون الموظفين", "البدلات", "mofrad", null, searchText, page, pageSize, "all");
            using (var command = new SqlCommand(@"
SELECT * FROM (
  SELECT ROW_NUMBER() OVER (ORDER BY id) RowNo, id, name, nameE, Account_Code, Account_code1, ViewComp, Salary, FixedOrChanged
  FROM dbo.mofrad WITH (NOLOCK)
  WHERE ISNULL(AddOrDiscount, 0) = 1
    AND (@Search = N'' OR ISNULL(name, N'') LIKE N'%' + @Search + N'%' OR ISNULL(nameE, N'') LIKE N'%' + @Search + N'%' OR ISNULL(Account_Code, N'') LIKE N'%' + @Search + N'%')
) q WHERE RowNo BETWEEN @Start AND @End ORDER BY RowNo;", connection))
            {
                AddSearch(command, searchText, page, pageSize);
                FillRows(command, model, "id", "name", "nameE", "Account_Code", "Account_code1", "ViewComp", "Salary", "FixedOrChanged");
            }
            model.Metrics.Add(Metric("عدد البدلات", Scalar(connection, "SELECT COUNT(1) FROM dbo.mofrad WHERE ISNULL(AddOrDiscount,0)=1").ToString(), "mofrad"));
            return model;
        }

        private LegacyHrFinancePageViewModel LoadEndOfService(SqlConnection connection, string searchText, int page, int pageSize, string employeeStatus)
        {
            var model = Base("end-service", "نهاية الخدمة", "شؤون الموظفين", "نهاية الخدمة", "End_of_service", null, searchText, page, pageSize, employeeStatus);
            using (var command = new SqlCommand(@"
SELECT * FROM (
 SELECT ROW_NUMBER() OVER (ORDER BY es.id DESC) RowNo,
        es.id, ISNULL(e.Emp_Name, es.Emp_name) AS Emp_Name, es.opr_date, es.start_date, es.[end _date] AS EndDate,
        es.net, es.LastTotal, es.Reaons, es.Posted, es.Approved
 FROM dbo.End_of_service es WITH (NOLOCK)
 LEFT JOIN dbo.TblEmployee e WITH (NOLOCK) ON e.Emp_ID = es.EmpID
 WHERE " + EmployeeStatusPredicate("e") + @"
   AND (@Search = N'' OR ISNULL(e.Emp_Name, es.Emp_name) LIKE N'%' + @Search + N'%' OR ISNULL(es.emp_code,N'') LIKE N'%' + @Search + N'%' OR CONVERT(NVARCHAR(30), es.id) = @Search)
) q WHERE RowNo BETWEEN @Start AND @End ORDER BY RowNo;", connection))
            {
                AddSearch(command, searchText, page, pageSize);
                AddEmployeeStatus(command, employeeStatus);
                FillRows(command, model, "id", "Emp_Name", "Reaons", "net", "opr_date", "Posted", "Approved", "EndDate");
            }
            AddCountMetric(connection, model, "End_of_service");
            return model;
        }

        private static LegacyHrFinancePageViewModel Base(string module, string title, string source, string form, string table, string warning, string search, int page, int pageSize, string employeeStatus = "active")
        {
            return new LegacyHrFinancePageViewModel { ModuleKey = module, Title = title, SourceSystem = source, SourceForm = form, LegacyTable = table, Warning = warning, SearchText = search, EmployeeStatus = NormalizeEmployeeStatus(employeeStatus), Page = page, PageSize = pageSize };
        }

        private static PayrollComponentEditViewModel MapComponent(IDataRecord reader)
        {
            return new PayrollComponentEditViewModel
            {
                Id = ReadNullableInt(reader, "id"),
                Name = ReadString(reader, "name"),
                NameEnglish = ReadString(reader, "nameE"),
                AddOrDiscount = ReadBool(reader, "AddOrDiscount"),
                FixedOrChanged = ReadBool(reader, "FixedOrChanged"),
                Unit = ReadNullableInt(reader, "Unit"),
                AccountCode = ReadString(reader, "Account_Code"),
                AccountCode1 = ReadString(reader, "Account_code1"),
                ViewComponent = ReadBool(reader, "ViewComp"),
                Salary = ReadBool(reader, "Salary"),
                Absence = ReadBool(reader, "Absence"),
                Late = ReadBool(reader, "Late"),
                Overtime = ReadBool(reader, "OverTime"),
                Insurance = ReadBool(reader, "Insurances"),
                Reward = ReadBool(reader, "Reward"),
                AllowIntroduction = ReadNullableInt(reader, "AllowIntrod")
            };
        }

        private static void FillRows(SqlCommand command, LegacyHrFinancePageViewModel model, string idCol, string primaryCol, string detailsCol, string amountCol, string dateCol, string tag1Col, string tag2Col, string tag3Col)
        {
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    var row = new LegacyHrFinanceRowViewModel
                    {
                        Id = ReadInt(reader, idCol),
                        Primary = ReadString(reader, primaryCol),
                        Details = ReadString(reader, detailsCol),
                        Amount = ReadString(reader, amountCol),
                        Period = ReadDisplayDate(reader, dateCol)
                    };
                    AddTag(row, reader, tag1Col);
                    AddTag(row, reader, tag2Col);
                    AddTag(row, reader, tag3Col);
                    row.Status = row.Tags.Count > 0 ? row.Tags[0] : "مسودة";
                    model.Rows.Add(row);
                }
            }
        }

        private static void AddTag(LegacyHrFinanceRowViewModel row, IDataRecord reader, string column)
        {
            if (string.IsNullOrWhiteSpace(column)) { return; }
            var value = ReadString(reader, column);
            if (!string.IsNullOrWhiteSpace(value)) { row.Tags.Add(column + ": " + value); }
        }

        private static void AddSearch(SqlCommand command, string searchText, int page, int pageSize)
        {
            command.Parameters.Add("@Search", SqlDbType.NVarChar, 100).Value = searchText;
            command.Parameters.Add("@Start", SqlDbType.Int).Value = ((page - 1) * pageSize) + 1;
            command.Parameters.Add("@End", SqlDbType.Int).Value = page * pageSize;
        }

        private static void AddEmployeeStatus(SqlCommand command, string employeeStatus)
        {
            command.Parameters.Add("@EmployeeStatus", SqlDbType.NVarChar, 20).Value = NormalizeEmployeeStatus(employeeStatus);
        }

        private static string EmployeeStatusPredicate(string alias)
        {
            return @"(@EmployeeStatus = N'all'
        OR (@EmployeeStatus = N'active' AND ISNULL(" + alias + @".chkStop, 0) = 0 AND ISNULL(" + alias + @".workstate, 0) = 1)
        OR (@EmployeeStatus = N'stopped' AND (ISNULL(" + alias + @".chkStop, 0) = 1 OR ISNULL(" + alias + @".workstate, 0) <> 1)))";
        }

        private static string NormalizeEmployeeStatus(string status)
        {
            status = (status ?? "active").Trim().ToLowerInvariant();
            return status == "stopped" || status == "all" ? status : "active";
        }

        private static void AddComponentParameters(SqlCommand command, PayrollComponentEditViewModel request, int id)
        {
            command.Parameters.Add("@Id", SqlDbType.Int).Value = id;
            command.Parameters.Add("@Name", SqlDbType.NVarChar, 255).Value = DbText(request.Name);
            command.Parameters.Add("@NameE", SqlDbType.NVarChar, 255).Value = DbText(request.NameEnglish);
            command.Parameters.Add("@AddOrDiscount", SqlDbType.Bit).Value = request.AddOrDiscount;
            command.Parameters.Add("@FixedOrChanged", SqlDbType.Bit).Value = request.FixedOrChanged;
            command.Parameters.Add("@Unit", SqlDbType.Int).Value = DbInt(request.Unit);
            command.Parameters.Add("@AccountCode", SqlDbType.NVarChar, 50).Value = DbText(request.AccountCode);
            command.Parameters.Add("@AccountCode1", SqlDbType.NVarChar, 50).Value = DbText(request.AccountCode1);
            command.Parameters.Add("@ViewComp", SqlDbType.Bit).Value = request.ViewComponent;
            command.Parameters.Add("@Salary", SqlDbType.Bit).Value = request.Salary;
            command.Parameters.Add("@Absence", SqlDbType.Bit).Value = request.Absence;
            command.Parameters.Add("@Late", SqlDbType.Bit).Value = request.Late;
            command.Parameters.Add("@OverTime", SqlDbType.Bit).Value = request.Overtime;
            command.Parameters.Add("@Insurances", SqlDbType.Bit).Value = request.Insurance;
            command.Parameters.Add("@Reward", SqlDbType.Bit).Value = request.Reward;
            command.Parameters.Add("@AllowIntrod", SqlDbType.Int).Value = DbInt(request.AllowIntroduction);
        }

        private static bool DuplicateExists(SqlConnection connection, SqlTransaction transaction, int currentId, string name)
        {
            using (var command = new SqlCommand("SELECT TOP (1) 1 FROM dbo.mofrad WHERE LTRIM(RTRIM(ISNULL(name, N''))) = @Name AND id <> @Id", connection, transaction))
            {
                command.Parameters.Add("@Name", SqlDbType.NVarChar, 255).Value = name.Trim();
                command.Parameters.Add("@Id", SqlDbType.Int).Value = currentId;
                return command.ExecuteScalar() != null;
            }
        }

        private static int NextId(SqlConnection connection, SqlTransaction transaction, string tableName, string keyColumn)
        {
            using (var command = new SqlCommand("SELECT ISNULL(MAX([" + keyColumn + "]), 0) + 1 FROM dbo.[" + tableName + "] WITH (UPDLOCK, HOLDLOCK)", connection, transaction))
            {
                return Convert.ToInt32(command.ExecuteScalar());
            }
        }

        private static void AddCountMetric(SqlConnection connection, LegacyHrFinancePageViewModel model, string tableName)
        {
            model.Metrics.Add(Metric("عدد السجلات", Scalar(connection, "SELECT COUNT(1) FROM dbo.[" + tableName + "]").ToString(), tableName));
        }

        private static LegacyHrFinanceMetricViewModel Metric(string label, string value, string hint)
        {
            return new LegacyHrFinanceMetricViewModel { Label = label, Value = value, Hint = hint };
        }

        private static int Scalar(SqlConnection connection, string sql)
        {
            using (var command = new SqlCommand(sql, connection))
            {
                var value = command.ExecuteScalar();
                return value == null || value == DBNull.Value ? 0 : Convert.ToInt32(value);
            }
        }

        private static LegacyHrFinanceSaveResult Fail(string message)
        {
            return new LegacyHrFinanceSaveResult { Success = false, Message = message };
        }

        private static object DbText(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? (object)DBNull.Value : value.Trim();
        }

        private static object DbInt(int? value)
        {
            return value.HasValue ? (object)value.Value : DBNull.Value;
        }

        private static string ReadString(IDataRecord reader, string column)
        {
            if (string.IsNullOrWhiteSpace(column)) { return string.Empty; }
            var value = reader[column];
            return value == DBNull.Value ? string.Empty : Convert.ToString(value);
        }

        private static int ReadInt(IDataRecord reader, string column)
        {
            var value = reader[column];
            return value == DBNull.Value ? 0 : Convert.ToInt32(value);
        }

        private static int? ReadNullableInt(IDataRecord reader, string column)
        {
            var value = reader[column];
            return value == DBNull.Value ? (int?)null : Convert.ToInt32(value);
        }

        private static bool ReadBool(IDataRecord reader, string column)
        {
            var value = reader[column];
            return value != DBNull.Value && Convert.ToBoolean(value);
        }

        private static string ReadDisplayDate(IDataRecord reader, string column)
        {
            var value = reader[column];
            if (value == DBNull.Value) { return string.Empty; }
            DateTime date;
            return DateTime.TryParse(Convert.ToString(value), out date) ? date.ToString("yyyy/MM/dd") : Convert.ToString(value);
        }
    }
}
