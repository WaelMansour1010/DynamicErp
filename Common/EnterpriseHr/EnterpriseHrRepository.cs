using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using MyERP.Common.EnterpriseHr;

namespace MyERP.Common.EnterpriseHr
{
    public interface IEnterpriseHrDbConnectionFactory
    {
        SqlConnection CreateOpenConnection();
    }

    public class EnterpriseHrRepository
    {
        private readonly IEnterpriseHrDbConnectionFactory _connectionFactory;

        public EnterpriseHrRepository(IEnterpriseHrDbConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public LegacyHrFinancePageViewModel Load(string moduleKey, string searchText, int page, int pageSize, string employeeStatus = "active", int? employeeId = null, DateTime? dateFrom = null, DateTime? dateTo = null, string advanceStatus = null)
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
                    case "advances": return LoadAdvances(connection, searchText, page, pageSize, employeeStatus, employeeId, dateFrom, dateTo, advanceStatus);
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

        public EmployeeAdvanceViewModel GetAdvance(int id)
        {
            using (var connection = _connectionFactory.CreateOpenConnection())
            {
                var advance = LoadAdvanceById(connection, null, id);
                if (advance != null)
                {
                    LoadAdvanceParts(connection, null, advance);
                }
                return advance;
            }
        }

        public LegacyHrFinanceSaveResult SaveAdvance(EmployeeAdvanceViewModel request, int? userId)
        {
            var validation = ValidateAdvanceRequest(request);
            if (!validation.Success) { return validation; }

            using (var connection = _connectionFactory.CreateOpenConnection())
            using (var transaction = connection.BeginTransaction())
            {
                var employee = GetEmployee(connection, transaction, request.EmployeeId.GetValueOrDefault(), true);
                if (employee == null)
                {
                    return Fail("الموظف غير موجود أو موقوف. لا يمكن تسجيل سلفة لموظف موقوف.");
                }

                var id = request.Id.GetValueOrDefault();
                if (id > 0)
                {
                    var current = LoadAdvanceById(connection, transaction, id);
                    if (current == null) { return Fail("لم يتم العثور على طلب السلفة المطلوب تعديله."); }
                    if (!current.CanEdit) { return Fail(current.LockReason); }
                }
                else
                {
                    id = NextId(connection, transaction, "TblEmpAdvanceRequest", "AdvanceID");
                }

                var advanceDate = ParseDate(request.AdvanceDate).GetValueOrDefault(DateTime.Today);
                var firstMonth = request.FirstMonthPayment.GetValueOrDefault();
                var firstYear = request.FirstYearPayment.GetValueOrDefault();
                var firstDate = new DateTime(firstYear, firstMonth, 1);
                var parts = BuildAdvanceParts(request.AdvanceValue, request.PaymentCounts, firstDate);
                var oldAdvance = GetOpenAdvanceTotal(connection, transaction, request.EmployeeId.GetValueOrDefault(), id);

                if (request.Id.GetValueOrDefault() <= 0)
                {
                    using (var command = new SqlCommand(@"
INSERT INTO dbo.TblEmpAdvanceRequest
(AdvanceID, Branch_NO, Emp_id, AdvanceValue, PaymentCounts, FirstDate, UserID, AdvanceDate, DeparmentID, gradeID, JobTypeID, basicSalary, oldAdvance, FirstMonthPayment, FirstYearPayment, AutoDiscount, reason, Balance, DBIssueDate, MethodDeci, DiffVal)
VALUES
(@AdvanceID, @BranchNo, @EmployeeId, @AdvanceValue, @PaymentCounts, @FirstDate, @UserID, @AdvanceDate, @DepartmentId, @GradeId, @JobTypeId, @BasicSalary, @OldAdvance, @FirstMonthPayment, @FirstYearPayment, @AutoDiscount, @Reason, @Balance, @DBIssueDate, @MethodDeci, @DiffVal);", connection, transaction))
                    {
                        AddAdvanceParameters(command, request, employee, id, userId, advanceDate, firstDate, oldAdvance);
                        command.ExecuteNonQuery();
                    }
                }
                else
                {
                    using (var command = new SqlCommand(@"
UPDATE dbo.TblEmpAdvanceRequest
SET Branch_NO = @BranchNo,
    Emp_id = @EmployeeId,
    AdvanceValue = @AdvanceValue,
    PaymentCounts = @PaymentCounts,
    FirstDate = @FirstDate,
    UserID = @UserID,
    AdvanceDate = @AdvanceDate,
    DeparmentID = @DepartmentId,
    gradeID = @GradeId,
    JobTypeID = @JobTypeId,
    basicSalary = @BasicSalary,
    oldAdvance = @OldAdvance,
    FirstMonthPayment = @FirstMonthPayment,
    FirstYearPayment = @FirstYearPayment,
    AutoDiscount = @AutoDiscount,
    reason = @Reason,
    Balance = @Balance,
    DBIssueDate = @DBIssueDate,
    MethodDeci = @MethodDeci,
    DiffVal = @DiffVal
WHERE AdvanceID = @AdvanceID;", connection, transaction))
                    {
                        AddAdvanceParameters(command, request, employee, id, userId, advanceDate, firstDate, oldAdvance);
                        command.ExecuteNonQuery();
                    }

                    using (var command = new SqlCommand("DELETE FROM dbo.TblEmpAdvanceRequestDetails WHERE AdvanceID = @AdvanceID; DELETE FROM dbo.TblEmpAdvanceRequestDetails2 WHERE AdvanceID = @AdvanceID;", connection, transaction))
                    {
                        command.Parameters.Add("@AdvanceID", SqlDbType.Int).Value = id;
                        command.ExecuteNonQuery();
                    }
                }

                InsertAdvanceParts(connection, transaction, id, parts);
                transaction.Commit();
                return new LegacyHrFinanceSaveResult { Success = true, Id = id, Message = "تم حفظ طلب السلفة بنجاح." };
            }
        }

        public LegacyHrFinanceSaveResult DeleteAdvance(int id)
        {
            if (id <= 0) { return Fail("رقم طلب السلفة غير صحيح."); }
            using (var connection = _connectionFactory.CreateOpenConnection())
            using (var transaction = connection.BeginTransaction())
            {
                var current = LoadAdvanceById(connection, transaction, id);
                if (current == null) { return Fail("لم يتم العثور على طلب السلفة."); }
                if (!current.CanDelete) { return Fail(current.LockReason); }

                using (var command = new SqlCommand(@"
DELETE FROM dbo.TblEmpAdvanceRequestDetails WHERE AdvanceID = @AdvanceID;
DELETE FROM dbo.TblEmpAdvanceRequestDetails2 WHERE AdvanceID = @AdvanceID;
DELETE FROM dbo.TblEmpAdvanceRequest WHERE AdvanceID = @AdvanceID;", connection, transaction))
                {
                    command.Parameters.Add("@AdvanceID", SqlDbType.Int).Value = id;
                    command.ExecuteNonQuery();
                }

                transaction.Commit();
                return new LegacyHrFinanceSaveResult { Success = true, Id = id, Message = "تم حذف طلب السلفة بنجاح." };
            }
        }

        public IList<EnterpriseHrEmployeeLookupViewModel> SearchEmployees(string searchText, string employeeStatus, int take)
        {
            using (var connection = _connectionFactory.CreateOpenConnection())
            {
                return LoadEmployees(connection, null, searchText, employeeStatus, null, take <= 0 ? 20 : take);
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

        private LegacyHrFinancePageViewModel LoadAdvances(SqlConnection connection, string searchText, int page, int pageSize, string employeeStatus, int? employeeId, DateTime? dateFrom, DateTime? dateTo, string advanceStatus)
        {
            advanceStatus = NormalizeAdvanceStatus(advanceStatus);
            var model = Base("advances", "السلف", "شؤون الموظفين", "طلب سلفة موظف", "TblEmpAdvanceRequest / TblEmpAdvanceRequestDetails", null, searchText, page, pageSize, employeeStatus);
            model.EmployeeId = employeeId;
            model.DateFrom = FormatDate(dateFrom);
            model.DateTo = FormatDate(dateTo);
            model.AdvanceStatus = advanceStatus;
            model.Employees = LoadEmployees(connection, null, searchText, employeeStatus, employeeId, 50);
            using (var command = new SqlCommand(@"
SELECT * FROM (
 SELECT ROW_NUMBER() OVER (ORDER BY a.AdvanceID DESC) RowNo,
        a.AdvanceID,
        CONVERT(INT, a.Emp_id) AS EmployeeId,
        COALESCE(NULLIF(e.Fullcode,N''), NULLIF(e.Emp_Code,N''), CONVERT(NVARCHAR(30), e.Emp_ID)) AS EmployeeCode,
        e.Emp_Name,
        CONVERT(INT, a.Branch_NO) AS BranchId,
        b.branch_name AS BranchName,
        CONVERT(INT, a.DeparmentID) AS DepartmentId,
        d.DepartmentName,
        a.AdvanceDate,
        a.AdvanceValue,
        a.PaymentCounts,
        a.FirstMonthPayment,
        a.FirstYearPayment,
        a.FirstDate,
        a.AutoDiscount,
        a.Approved,
        a.Posted,
        a.AccAproved,
        a.notok,
        a.reason,
        a.basicSalary,
        a.oldAdvance,
        a.Balance,
        ISNULL(px.PartsCount,0) AS PartsCount,
        ISNULL(px.PaidPartsCount,0) AS PaidPartsCount,
        ISNULL(px.PaidAmount,0) AS PaidAmount,
        ISNULL(a.AdvanceValue,0) - ISNULL(px.PaidAmount,0) AS RemainingAmount
 FROM dbo.TblEmpAdvanceRequest a WITH (NOLOCK)
 LEFT JOIN dbo.TblEmployee e WITH (NOLOCK) ON e.Emp_ID = CONVERT(INT, a.Emp_id)
 LEFT JOIN dbo.TblBranchesData b WITH (NOLOCK) ON b.branch_id = CONVERT(INT, a.Branch_NO)
 LEFT JOIN dbo.TblEmpDepartments d WITH (NOLOCK) ON d.DeparmentID = CONVERT(INT, a.DeparmentID)
 LEFT JOIN (
     SELECT AdvanceID,
            COUNT(1) AS PartsCount,
            SUM(CASE WHEN Payed IS NOT NULL OR Payed1 IS NOT NULL OR EmpAdPaID IS NOT NULL THEN 1 ELSE 0 END) AS PaidPartsCount,
            SUM(CASE WHEN Payed IS NOT NULL OR Payed1 IS NOT NULL OR EmpAdPaID IS NOT NULL THEN ISNULL(PartValue,0) ELSE 0 END) AS PaidAmount
     FROM dbo.TblEmpAdvanceRequestDetails WITH (NOLOCK)
     GROUP BY AdvanceID
 ) px ON px.AdvanceID = a.AdvanceID
 WHERE " + EmployeeStatusPredicate("e") + @"
   AND (@EmployeeId IS NULL OR CONVERT(INT, a.Emp_id) = @EmployeeId)
   AND (@DateFrom IS NULL OR a.AdvanceDate >= @DateFrom)
   AND (@DateTo IS NULL OR a.AdvanceDate < DATEADD(DAY, 1, @DateTo))
   AND (
        @AdvanceStatus = N'all'
        OR (@AdvanceStatus = N'draft' AND ISNULL(a.Approved,0)=0 AND a.Posted IS NULL AND ISNULL(a.AccAproved,0)=0 AND ISNULL(a.notok,0)=0)
        OR (@AdvanceStatus = N'approved' AND ISNULL(a.Approved,0)=1)
        OR (@AdvanceStatus = N'posted' AND a.Posted IS NOT NULL)
        OR (@AdvanceStatus = N'accounting-approved' AND ISNULL(a.AccAproved,0)=1)
        OR (@AdvanceStatus = N'rejected' AND ISNULL(a.notok,0)=1)
   )
   AND (@Search = N'' OR ISNULL(e.Emp_Name,N'') LIKE N'%' + @Search + N'%' OR ISNULL(e.Fullcode,N'') LIKE N'%' + @Search + N'%' OR ISNULL(e.Emp_Code,N'') LIKE N'%' + @Search + N'%' OR ISNULL(a.reason,N'') LIKE N'%' + @Search + N'%' OR CONVERT(NVARCHAR(30), a.AdvanceID) = @Search)
) q WHERE RowNo BETWEEN @Start AND @End ORDER BY RowNo;", connection))
            {
                AddSearch(command, searchText, page, pageSize);
                AddEmployeeStatus(command, employeeStatus);
                command.Parameters.Add("@EmployeeId", SqlDbType.Int).Value = employeeId.HasValue ? (object)employeeId.Value : DBNull.Value;
                command.Parameters.Add("@DateFrom", SqlDbType.DateTime).Value = dateFrom.HasValue ? (object)dateFrom.Value.Date : DBNull.Value;
                command.Parameters.Add("@DateTo", SqlDbType.DateTime).Value = dateTo.HasValue ? (object)dateTo.Value.Date : DBNull.Value;
                command.Parameters.Add("@AdvanceStatus", SqlDbType.NVarChar, 30).Value = advanceStatus;
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        model.Advances.Add(MapAdvance(reader));
                    }
                }
            }
            AddAdvanceMetrics(connection, model, employeeStatus, employeeId, dateFrom, dateTo, advanceStatus, searchText);
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

        private static EmployeeAdvanceViewModel MapAdvance(IDataRecord reader)
        {
            var advance = new EmployeeAdvanceViewModel
            {
                Id = ReadNullableInt(reader, "AdvanceID"),
                EmployeeId = ReadNullableInt(reader, "EmployeeId"),
                EmployeeCode = ReadString(reader, "EmployeeCode"),
                EmployeeName = ReadString(reader, "Emp_Name"),
                BranchId = ReadNullableInt(reader, "BranchId"),
                BranchName = ReadString(reader, "BranchName"),
                DepartmentId = ReadNullableInt(reader, "DepartmentId"),
                DepartmentName = ReadString(reader, "DepartmentName"),
                AdvanceDate = ReadDisplayDate(reader, "AdvanceDate"),
                AdvanceValue = ReadDecimal(reader, "AdvanceValue"),
                PaymentCounts = ReadInt(reader, "PaymentCounts"),
                FirstMonthPayment = ReadNullableInt(reader, "FirstMonthPayment"),
                FirstYearPayment = ReadNullableInt(reader, "FirstYearPayment"),
                FirstDate = ReadDisplayDate(reader, "FirstDate"),
                AutoDiscount = ReadBool(reader, "AutoDiscount"),
                Approved = ReadBool(reader, "Approved"),
                Posted = ReadNullableInt(reader, "Posted").GetValueOrDefault() != 0,
                AccountingApproved = ReadNullableInt(reader, "AccAproved").GetValueOrDefault() != 0,
                Rejected = ReadBool(reader, "notok"),
                Reason = ReadString(reader, "reason"),
                BasicSalary = ReadDecimal(reader, "basicSalary"),
                OldAdvance = ReadDecimal(reader, "oldAdvance"),
                Balance = ReadDecimal(reader, "Balance"),
                PartsCount = ReadInt(reader, "PartsCount"),
                PaidPartsCount = ReadInt(reader, "PaidPartsCount"),
                PaidAmount = ReadDecimal(reader, "PaidAmount"),
                RemainingAmount = ReadDecimal(reader, "RemainingAmount")
            };

            ApplyAdvanceLockState(advance);
            return advance;
        }

        private EmployeeAdvanceViewModel LoadAdvanceById(SqlConnection connection, SqlTransaction transaction, int id)
        {
            using (var command = new SqlCommand(@"
SELECT TOP (1)
        a.AdvanceID,
        CONVERT(INT, a.Emp_id) AS EmployeeId,
        COALESCE(NULLIF(e.Fullcode,N''), NULLIF(e.Emp_Code,N''), CONVERT(NVARCHAR(30), e.Emp_ID)) AS EmployeeCode,
        e.Emp_Name,
        CONVERT(INT, a.Branch_NO) AS BranchId,
        b.branch_name AS BranchName,
        CONVERT(INT, a.DeparmentID) AS DepartmentId,
        d.DepartmentName,
        a.AdvanceDate,
        a.AdvanceValue,
        a.PaymentCounts,
        a.FirstMonthPayment,
        a.FirstYearPayment,
        a.FirstDate,
        a.AutoDiscount,
        a.Approved,
        a.Posted,
        a.AccAproved,
        a.notok,
        a.reason,
        a.basicSalary,
        a.oldAdvance,
        a.Balance,
        ISNULL(px.PartsCount,0) AS PartsCount,
        ISNULL(px.PaidPartsCount,0) AS PaidPartsCount,
        ISNULL(px.PaidAmount,0) AS PaidAmount,
        ISNULL(a.AdvanceValue,0) - ISNULL(px.PaidAmount,0) AS RemainingAmount
 FROM dbo.TblEmpAdvanceRequest a WITH (NOLOCK)
 LEFT JOIN dbo.TblEmployee e WITH (NOLOCK) ON e.Emp_ID = CONVERT(INT, a.Emp_id)
 LEFT JOIN dbo.TblBranchesData b WITH (NOLOCK) ON b.branch_id = CONVERT(INT, a.Branch_NO)
 LEFT JOIN dbo.TblEmpDepartments d WITH (NOLOCK) ON d.DeparmentID = CONVERT(INT, a.DeparmentID)
 LEFT JOIN (
     SELECT AdvanceID,
            COUNT(1) AS PartsCount,
            SUM(CASE WHEN Payed IS NOT NULL OR Payed1 IS NOT NULL OR EmpAdPaID IS NOT NULL THEN 1 ELSE 0 END) AS PaidPartsCount,
            SUM(CASE WHEN Payed IS NOT NULL OR Payed1 IS NOT NULL OR EmpAdPaID IS NOT NULL THEN ISNULL(PartValue,0) ELSE 0 END) AS PaidAmount
     FROM dbo.TblEmpAdvanceRequestDetails WITH (NOLOCK)
     GROUP BY AdvanceID
 ) px ON px.AdvanceID = a.AdvanceID
 WHERE a.AdvanceID = @AdvanceID;", connection, transaction))
            {
                command.Parameters.Add("@AdvanceID", SqlDbType.Int).Value = id;
                using (var reader = command.ExecuteReader())
                {
                    return reader.Read() ? MapAdvance(reader) : null;
                }
            }
        }

        private static void LoadAdvanceParts(SqlConnection connection, SqlTransaction transaction, EmployeeAdvanceViewModel advance)
        {
            using (var command = new SqlCommand(@"
SELECT PartNo, PartValue, PartDate, Payed, Payed1, EmpAdPaID, Remark
FROM dbo.TblEmpAdvanceRequestDetails WITH (NOLOCK)
WHERE AdvanceID = @AdvanceID
ORDER BY PartNo;", connection, transaction))
            {
                command.Parameters.Add("@AdvanceID", SqlDbType.Int).Value = advance.Id.GetValueOrDefault();
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        advance.Parts.Add(new EmployeeAdvancePartViewModel
                        {
                            PartNo = ReadInt(reader, "PartNo"),
                            PartValue = ReadDecimal(reader, "PartValue"),
                            PartDate = ReadDisplayDate(reader, "PartDate"),
                            Payed = ReadNullableInt(reader, "Payed").HasValue || ReadNullableInt(reader, "Payed1").HasValue || ReadNullableInt(reader, "EmpAdPaID").HasValue,
                            Remark = ReadString(reader, "Remark")
                        });
                    }
                }
            }
        }

        private static void ApplyAdvanceLockState(EmployeeAdvanceViewModel advance)
        {
            if (advance.AccountingApproved)
            {
                advance.LockReason = "لا يمكن تعديل أو حذف طلب سلفة تم اعتماده محاسبياً.";
            }
            else if (advance.Posted)
            {
                advance.LockReason = "لا يمكن تعديل أو حذف طلب سلفة مرسل/مرحل.";
            }
            else if (advance.Approved)
            {
                advance.LockReason = "لا يمكن تعديل أو حذف طلب سلفة معتمد.";
            }
            else if (advance.PaidPartsCount > 0)
            {
                advance.LockReason = "لا يمكن تعديل أو حذف طلب سلفة له أقساط مسددة.";
            }

            advance.CanEdit = string.IsNullOrWhiteSpace(advance.LockReason);
            advance.CanDelete = advance.CanEdit;
        }

        private EnterpriseHrEmployeeLookupViewModel GetEmployee(SqlConnection connection, SqlTransaction transaction, int employeeId, bool activeOnly)
        {
            using (var command = new SqlCommand(@"
SELECT TOP (1)
       e.Emp_ID,
       COALESCE(NULLIF(e.Fullcode,N''), NULLIF(e.Emp_Code,N''), CONVERT(NVARCHAR(30), e.Emp_ID)) AS EmployeeCode,
       e.Emp_Name,
       e.BranchId,
       b.branch_name AS BranchName,
       e.DepartmentID,
       d.DepartmentName,
       e.Emp_Salary
FROM dbo.TblEmployee e WITH (NOLOCK)
LEFT JOIN dbo.TblBranchesData b WITH (NOLOCK) ON b.branch_id = e.BranchId
LEFT JOIN dbo.TblEmpDepartments d WITH (NOLOCK) ON d.DeparmentID = e.DepartmentID
WHERE e.Emp_ID = @EmployeeId
  AND (@ActiveOnly = 0 OR (ISNULL(e.chkStop,0)=0 AND ISNULL(e.workstate,0)=1));", connection, transaction))
            {
                command.Parameters.Add("@EmployeeId", SqlDbType.Int).Value = employeeId;
                command.Parameters.Add("@ActiveOnly", SqlDbType.Bit).Value = activeOnly;
                using (var reader = command.ExecuteReader())
                {
                    if (!reader.Read()) { return null; }
                    return new EnterpriseHrEmployeeLookupViewModel
                    {
                        Id = ReadInt(reader, "Emp_ID"),
                        Code = ReadString(reader, "EmployeeCode"),
                        Name = ReadString(reader, "Emp_Name"),
                        BranchId = ReadNullableInt(reader, "BranchId"),
                        BranchName = ReadString(reader, "BranchName"),
                        DepartmentId = ReadNullableInt(reader, "DepartmentID"),
                        DepartmentName = ReadString(reader, "DepartmentName"),
                        BasicSalary = ReadDecimal(reader, "Emp_Salary")
                    };
                }
            }
        }

        private IList<EnterpriseHrEmployeeLookupViewModel> LoadEmployees(SqlConnection connection, SqlTransaction transaction, string searchText, string employeeStatus, int? selectedEmployeeId, int take)
        {
            var employees = new List<EnterpriseHrEmployeeLookupViewModel>();
            using (var command = new SqlCommand(@"
SELECT TOP (@Take)
       e.Emp_ID,
       COALESCE(NULLIF(e.Fullcode,N''), NULLIF(e.Emp_Code,N''), CONVERT(NVARCHAR(30), e.Emp_ID)) AS EmployeeCode,
       e.Emp_Name,
       e.BranchId,
       b.branch_name AS BranchName,
       e.DepartmentID,
       d.DepartmentName,
       e.Emp_Salary
FROM dbo.TblEmployee e WITH (NOLOCK)
LEFT JOIN dbo.TblBranchesData b WITH (NOLOCK) ON b.branch_id = e.BranchId
LEFT JOIN dbo.TblEmpDepartments d WITH (NOLOCK) ON d.DeparmentID = e.DepartmentID
WHERE " + EmployeeStatusPredicate("e") + @"
  AND (@SelectedEmployeeId IS NULL OR e.Emp_ID = @SelectedEmployeeId OR @Search <> N'')
  AND (@Search = N'' OR ISNULL(e.Emp_Name,N'') LIKE N'%' + @Search + N'%' OR ISNULL(e.Fullcode,N'') LIKE N'%' + @Search + N'%' OR ISNULL(e.Emp_Code,N'') LIKE N'%' + @Search + N'%' OR CONVERT(NVARCHAR(30), e.Emp_ID) = @Search)
ORDER BY e.Emp_Name;", connection, transaction))
            {
                command.Parameters.Add("@Take", SqlDbType.Int).Value = take;
                command.Parameters.Add("@Search", SqlDbType.NVarChar, 100).Value = (searchText ?? string.Empty).Trim();
                command.Parameters.Add("@SelectedEmployeeId", SqlDbType.Int).Value = selectedEmployeeId.HasValue ? (object)selectedEmployeeId.Value : DBNull.Value;
                AddEmployeeStatus(command, employeeStatus);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        employees.Add(new EnterpriseHrEmployeeLookupViewModel
                        {
                            Id = ReadInt(reader, "Emp_ID"),
                            Code = ReadString(reader, "EmployeeCode"),
                            Name = ReadString(reader, "Emp_Name"),
                            BranchId = ReadNullableInt(reader, "BranchId"),
                            BranchName = ReadString(reader, "BranchName"),
                            DepartmentId = ReadNullableInt(reader, "DepartmentID"),
                            DepartmentName = ReadString(reader, "DepartmentName"),
                            BasicSalary = ReadDecimal(reader, "Emp_Salary")
                        });
                    }
                }
            }
            return employees;
        }

        private static LegacyHrFinanceSaveResult ValidateAdvanceRequest(EmployeeAdvanceViewModel request)
        {
            if (request == null) { return Fail("بيانات طلب السلفة غير مكتملة."); }
            if (!request.EmployeeId.HasValue || request.EmployeeId.Value <= 0) { return Fail("يجب اختيار الموظف."); }
            if (!ParseDate(request.AdvanceDate).HasValue) { return Fail("تاريخ طلب السلفة غير صحيح."); }
            if (request.AdvanceValue <= 0) { return Fail("يجب إدخال قيمة السلفة أكبر من صفر."); }
            if (request.PaymentCounts <= 0) { return Fail("يجب إدخال عدد أقساط السداد."); }
            if (request.PaymentCounts > 84) { return Fail("عدد الأقساط لا يجب أن يزيد عن 84 قسطاً."); }
            if (!request.FirstMonthPayment.HasValue || request.FirstMonthPayment.Value < 1 || request.FirstMonthPayment.Value > 12) { return Fail("يجب تحديد أول شهر للسداد."); }
            if (!request.FirstYearPayment.HasValue || request.FirstYearPayment.Value < 1900 || request.FirstYearPayment.Value > 2100) { return Fail("يجب تحديد سنة السداد بشكل صحيح."); }
            return new LegacyHrFinanceSaveResult { Success = true };
        }

        private static IList<EmployeeAdvancePartViewModel> BuildAdvanceParts(decimal totalValue, int paymentCounts, DateTime firstDate)
        {
            var parts = new List<EmployeeAdvancePartViewModel>();
            var normalPart = Math.Round(totalValue / paymentCounts, 2, MidpointRounding.AwayFromZero);
            decimal accumulated = 0;
            for (var i = 1; i <= paymentCounts; i++)
            {
                var value = i == paymentCounts ? totalValue - accumulated : normalPart;
                accumulated += value;
                parts.Add(new EmployeeAdvancePartViewModel
                {
                    PartNo = i,
                    PartValue = value,
                    PartDate = firstDate.AddMonths(i - 1).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                });
            }
            return parts;
        }

        private static void InsertAdvanceParts(SqlConnection connection, SqlTransaction transaction, int advanceId, IList<EmployeeAdvancePartViewModel> parts)
        {
            foreach (var part in parts)
            {
                using (var command = new SqlCommand(@"
INSERT INTO dbo.TblEmpAdvanceRequestDetails (AdvanceID, PartNo, PartValue, PartDate)
VALUES (@AdvanceID, @PartNo, @PartValue, @PartDate);", connection, transaction))
                {
                    command.Parameters.Add("@AdvanceID", SqlDbType.Float).Value = advanceId;
                    command.Parameters.Add("@PartNo", SqlDbType.Float).Value = part.PartNo;
                    command.Parameters.Add("@PartValue", SqlDbType.Float).Value = Convert.ToDouble(part.PartValue);
                    command.Parameters.Add("@PartDate", SqlDbType.DateTime).Value = ParseDate(part.PartDate).GetValueOrDefault();
                    command.ExecuteNonQuery();
                }
            }
        }

        private static void AddAdvanceParameters(SqlCommand command, EmployeeAdvanceViewModel request, EnterpriseHrEmployeeLookupViewModel employee, int id, int? userId, DateTime advanceDate, DateTime firstDate, decimal oldAdvance)
        {
            command.Parameters.Add("@AdvanceID", SqlDbType.Int).Value = id;
            command.Parameters.Add("@BranchNo", SqlDbType.Float).Value = employee.BranchId.HasValue ? (object)employee.BranchId.Value : DBNull.Value;
            command.Parameters.Add("@EmployeeId", SqlDbType.Float).Value = employee.Id;
            command.Parameters.Add("@AdvanceValue", SqlDbType.Float).Value = Convert.ToDouble(request.AdvanceValue);
            command.Parameters.Add("@PaymentCounts", SqlDbType.Float).Value = request.PaymentCounts;
            command.Parameters.Add("@FirstDate", SqlDbType.DateTime).Value = firstDate;
            command.Parameters.Add("@UserID", SqlDbType.Float).Value = userId.HasValue ? (object)userId.Value : DBNull.Value;
            command.Parameters.Add("@AdvanceDate", SqlDbType.DateTime).Value = advanceDate;
            command.Parameters.Add("@DepartmentId", SqlDbType.Float).Value = employee.DepartmentId.HasValue ? (object)employee.DepartmentId.Value : DBNull.Value;
            command.Parameters.Add("@GradeId", SqlDbType.Float).Value = DBNull.Value;
            command.Parameters.Add("@JobTypeId", SqlDbType.Float).Value = DBNull.Value;
            command.Parameters.Add("@BasicSalary", SqlDbType.Float).Value = Convert.ToDouble(employee.BasicSalary);
            command.Parameters.Add("@OldAdvance", SqlDbType.Float).Value = Convert.ToDouble(oldAdvance);
            command.Parameters.Add("@FirstMonthPayment", SqlDbType.Float).Value = request.FirstMonthPayment.GetValueOrDefault();
            command.Parameters.Add("@FirstYearPayment", SqlDbType.Float).Value = request.FirstYearPayment.GetValueOrDefault();
            command.Parameters.Add("@AutoDiscount", SqlDbType.Bit).Value = request.AutoDiscount;
            command.Parameters.Add("@Reason", SqlDbType.NVarChar, 4000).Value = DbText(request.Reason);
            command.Parameters.Add("@Balance", SqlDbType.Float).Value = Convert.ToDouble(employee.BasicSalary - oldAdvance);
            command.Parameters.Add("@DBIssueDate", SqlDbType.DateTime).Value = DBNull.Value;
            command.Parameters.Add("@MethodDeci", SqlDbType.Int).Value = 1;
            command.Parameters.Add("@DiffVal", SqlDbType.Float).Value = 0;
        }

        private decimal GetOpenAdvanceTotal(SqlConnection connection, SqlTransaction transaction, int employeeId, int excludeAdvanceId)
        {
            using (var command = new SqlCommand(@"
SELECT SUM(ISNULL(d.PartValue,0))
FROM dbo.TblEmpAdvanceRequest r
INNER JOIN dbo.TblEmpAdvanceRequestDetails d ON d.AdvanceID = r.AdvanceID
WHERE d.Payed IS NULL
  AND CONVERT(INT, r.Emp_id) = @EmployeeId
  AND r.AdvanceID <> @ExcludeAdvanceID;", connection, transaction))
            {
                command.Parameters.Add("@EmployeeId", SqlDbType.Int).Value = employeeId;
                command.Parameters.Add("@ExcludeAdvanceID", SqlDbType.Int).Value = excludeAdvanceId;
                var value = command.ExecuteScalar();
                return value == null || value == DBNull.Value ? 0 : Convert.ToDecimal(value);
            }
        }

        private static void AddAdvanceMetrics(SqlConnection connection, LegacyHrFinancePageViewModel model, string employeeStatus, int? employeeId, DateTime? dateFrom, DateTime? dateTo, string advanceStatus, string searchText)
        {
            using (var command = new SqlCommand(@"
SELECT COUNT(1) AS TotalCount,
       ISNULL(SUM(ISNULL(a.AdvanceValue,0)),0) AS TotalValue,
       SUM(CASE WHEN ISNULL(a.Approved,0)=1 THEN 1 ELSE 0 END) AS ApprovedCount,
       SUM(CASE WHEN ISNULL(a.AccAproved,0)=1 OR a.Posted IS NOT NULL THEN 1 ELSE 0 END) AS LockedCount
FROM dbo.TblEmpAdvanceRequest a WITH (NOLOCK)
LEFT JOIN dbo.TblEmployee e WITH (NOLOCK) ON e.Emp_ID = CONVERT(INT, a.Emp_id)
WHERE " + EmployeeStatusPredicate("e") + @"
  AND (@EmployeeId IS NULL OR CONVERT(INT, a.Emp_id) = @EmployeeId)
  AND (@DateFrom IS NULL OR a.AdvanceDate >= @DateFrom)
  AND (@DateTo IS NULL OR a.AdvanceDate < DATEADD(DAY, 1, @DateTo))
  AND (
       @AdvanceStatus = N'all'
       OR (@AdvanceStatus = N'draft' AND ISNULL(a.Approved,0)=0 AND a.Posted IS NULL AND ISNULL(a.AccAproved,0)=0 AND ISNULL(a.notok,0)=0)
       OR (@AdvanceStatus = N'approved' AND ISNULL(a.Approved,0)=1)
       OR (@AdvanceStatus = N'posted' AND a.Posted IS NOT NULL)
       OR (@AdvanceStatus = N'accounting-approved' AND ISNULL(a.AccAproved,0)=1)
       OR (@AdvanceStatus = N'rejected' AND ISNULL(a.notok,0)=1)
  )
  AND (@Search = N'' OR ISNULL(e.Emp_Name,N'') LIKE N'%' + @Search + N'%' OR ISNULL(e.Fullcode,N'') LIKE N'%' + @Search + N'%' OR ISNULL(e.Emp_Code,N'') LIKE N'%' + @Search + N'%' OR ISNULL(a.reason,N'') LIKE N'%' + @Search + N'%' OR CONVERT(NVARCHAR(30), a.AdvanceID) = @Search);", connection))
            {
                AddEmployeeStatus(command, employeeStatus);
                command.Parameters.Add("@EmployeeId", SqlDbType.Int).Value = employeeId.HasValue ? (object)employeeId.Value : DBNull.Value;
                command.Parameters.Add("@DateFrom", SqlDbType.DateTime).Value = dateFrom.HasValue ? (object)dateFrom.Value.Date : DBNull.Value;
                command.Parameters.Add("@DateTo", SqlDbType.DateTime).Value = dateTo.HasValue ? (object)dateTo.Value.Date : DBNull.Value;
                command.Parameters.Add("@AdvanceStatus", SqlDbType.NVarChar, 30).Value = NormalizeAdvanceStatus(advanceStatus);
                command.Parameters.Add("@Search", SqlDbType.NVarChar, 100).Value = (searchText ?? string.Empty).Trim();
                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        model.Metrics.Add(Metric("عدد الطلبات", Convert.ToString(ReadInt(reader, "TotalCount")), "حسب الفلاتر الحالية"));
                        model.Metrics.Add(Metric("إجمالي السلف", ReadDecimal(reader, "TotalValue").ToString("N2"), "طلبات السلف"));
                        model.Metrics.Add(Metric("طلبات معتمدة", Convert.ToString(ReadInt(reader, "ApprovedCount")), "معتمدة إداريا"));
                        model.Metrics.Add(Metric("طلبات مقفلة", Convert.ToString(ReadInt(reader, "LockedCount")), "مرحل/معتمد محاسبيا"));
                    }
                }
            }
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

        private static string NormalizeAdvanceStatus(string status)
        {
            status = (status ?? "all").Trim().ToLowerInvariant();
            switch (status)
            {
                case "draft":
                case "approved":
                case "posted":
                case "accounting-approved":
                case "rejected":
                    return status;
                default:
                    return "all";
            }
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

        private static DateTime? ParseDate(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) { return null; }
            DateTime date;
            if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out date)) { return date; }
            if (DateTime.TryParse(value, out date)) { return date; }
            return null;
        }

        private static string FormatDate(DateTime? value)
        {
            return value.HasValue ? value.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : string.Empty;
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

        private static decimal ReadDecimal(IDataRecord reader, string column)
        {
            var value = reader[column];
            return value == DBNull.Value ? 0 : Convert.ToDecimal(value);
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
