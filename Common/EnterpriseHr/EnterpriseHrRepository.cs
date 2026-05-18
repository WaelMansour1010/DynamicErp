using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
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

        public LegacyHrFinancePageViewModel Load(string moduleKey, string searchText, int page, int pageSize, string employeeStatus = "active", int? employeeId = null, DateTime? dateFrom = null, DateTime? dateTo = null, string advanceStatus = null, string vacationStatus = null, string vacationType = null, int? componentId = null, int? branchId = null, int? departmentId = null, int? yearFilter = null, int? monthFilter = null, string componentType = null)
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
                    case "advances": return LoadAdvances(connection, searchText, page, pageSize, employeeStatus, employeeId, dateFrom, dateTo, advanceStatus, branchId, departmentId);
                    case "leave": return LoadLeaveEntitlements(connection, searchText, page, pageSize, employeeStatus);
                    case "sickleave": return LoadSickLeaves(connection, searchText, page, pageSize, employeeStatus);
                    case "adjustments": return LoadAdjustments(connection, searchText, page, pageSize, employeeStatus);
                    case "changed-components": return LoadChangedComponents(connection, searchText, page, pageSize, employeeStatus, employeeId, dateFrom, dateTo, advanceStatus, componentId, branchId, departmentId, yearFilter, monthFilter, componentType);
                    case "allocations": return LoadAllocations(connection, searchText, page, pageSize);
                    case "absences": return LoadAbsences(connection, searchText, page, pageSize, employeeStatus);
                    case "vacations": return LoadVacations(connection, searchText, page, pageSize, employeeStatus, employeeId, dateFrom, dateTo, vacationStatus, vacationType, branchId, departmentId);
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
                    LoadAdvanceApprovalHistory(connection, null, advance);
                }
                return advance;
            }
        }

        public AdvanceAccountingBoundaryViewModel GetAdvanceAccountingBoundary(int requestId)
        {
            using (var connection = _connectionFactory.CreateOpenConnection())
            {
                return BuildAdvanceAccountingBoundary(connection, null, requestId);
            }
        }

        public VacationBalanceViewModel CalculateVacationBalance(VacationBalanceRequestViewModel request)
        {
            if (request == null || request.EmployeeId <= 0)
            {
                return new VacationBalanceViewModel
                {
                    NegativeBalancePrevented = true,
                    CanPostPaidVacation = false,
                    EmployeeStatusMessage = "يجب اختيار الموظف قبل حساب رصيد الإجازات.",
                    AsOfDate = FormatDate(DateTime.Today),
                    Errors = { "يجب اختيار الموظف قبل حساب رصيد الإجازات." }
                };
            }

            using (var connection = _connectionFactory.CreateOpenConnection())
            {
                return CalculateVacationBalance(connection, null, request);
            }
        }

        public EmployeeVacationRequestViewModel GetVacation(int id)
        {
            using (var connection = _connectionFactory.CreateOpenConnection())
            {
                var vacation = LoadVacationById(connection, null, id);
                if (vacation != null)
                {
                    LoadVacationApprovalHistory(connection, null, vacation);
                }
                return vacation;
            }
        }

        public LegacyHrFinanceSaveResult SaveVacation(EmployeeVacationRequestViewModel request, int? userId)
        {
            var validation = ValidateVacationRequest(request);
            if (!validation.Success) { return validation; }

            using (var connection = _connectionFactory.CreateOpenConnection())
            using (var transaction = connection.BeginTransaction())
            {
                try
                {
                    var employee = GetEmployee(connection, transaction, request.EmployeeId.GetValueOrDefault(), true);
                    if (employee == null) { transaction.Rollback(); return Fail("الموظف غير موجود أو موقوف. لا يمكن تسجيل إجازة لموظف غير نشط."); }

                    var fromDate = ParseDate(request.FromDate).GetValueOrDefault().Date;
                    var toDate = ParseDate(request.ToDate).GetValueOrDefault().Date;
                    var resumeWork = ParseDate(request.ResumeWork).GetValueOrDefault(toDate.AddDays(1)).Date;
                    var id = request.Id.GetValueOrDefault();
                    if (id > 0)
                    {
                        var current = LoadVacationById(connection, transaction, id);
                        if (current == null) { transaction.Rollback(); return Fail("لم يتم العثور على طلب الإجازة المطلوب تعديله."); }
                        if (!current.CanEdit) { transaction.Rollback(); return Fail(current.LockReason); }
                    }
                    else
                    {
                        id = NextId(connection, transaction, "TblVocation", "ID");
                    }

                    var requestedDays = RoundDays((decimal)(toDate - fromDate).TotalDays + 1m);
                    if (request.WithSalary)
                    {
                        var balance = CalculateVacationBalance(connection, transaction, new VacationBalanceRequestViewModel
                        {
                            EmployeeId = employee.Id,
                            AsOfDate = fromDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                            VacationStartDate = fromDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                            VacationEndDate = toDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                            RequestedDays = requestedDays,
                            ExcludeVacationId = id
                        });
                        if (!balance.CanPostPaidVacation)
                        {
                            transaction.Rollback();
                            return Fail(balance.Errors.Any() ? string.Join(" ", balance.Errors) : "رصيد الإجازات لا يسمح بتسجيل إجازة مدفوعة لهذه الفترة.");
                        }
                    }

                    if (request.Id.GetValueOrDefault() <= 0)
                    {
                        using (var command = new SqlCommand(@"
INSERT INTO dbo.TblVocation
(ID, RecordDate, BranchID, EmpID, ManagerID, ProjectID, FromDate, ToDate, JobID, Reson, UserID, posted, TypeVocation, VocationType, PostedDate, Approved, ManagerApprove, WithSalary, WithoutSalary, ResumeWork, ok, notok, NoVacation, TypeVacation, DeptID, FlagPayed, BignDate, TotalDay)
VALUES
(@ID, @RecordDate, @BranchID, @EmpID, @ManagerID, @ProjectID, @FromDate, @ToDate, @JobID, @Reason, @UserID, 1, @TypeVocation, @VocationType, GETDATE(), 0, 0, @WithSalary, @WithoutSalary, @ResumeWork, 0, 0, @NoVacation, @TypeVacation, @DeptID, NULL, @BeginDate, @TotalDay);", connection, transaction))
                        {
                            AddVacationParameters(command, request, employee, id, userId, fromDate, toDate, resumeWork, requestedDays);
                            command.ExecuteNonQuery();
                        }
                        SendVacationToApproval(connection, transaction, employee, id, userId, null, "تم إرسال طلب الإجازة للمراجعة.");
                    }
                    else
                    {
                        using (var command = new SqlCommand(@"
UPDATE dbo.TblVocation
SET RecordDate = @RecordDate,
    BranchID = @BranchID,
    EmpID = @EmpID,
    ManagerID = @ManagerID,
    ProjectID = @ProjectID,
    FromDate = @FromDate,
    ToDate = @ToDate,
    JobID = @JobID,
    Reson = @Reason,
    UserID = @UserID,
    posted = 1,
    TypeVocation = @TypeVocation,
    VocationType = @VocationType,
    PostedDate = GETDATE(),
    WithSalary = @WithSalary,
    WithoutSalary = @WithoutSalary,
    ResumeWork = @ResumeWork,
    NoVacation = @NoVacation,
    TypeVacation = @TypeVacation,
    DeptID = @DeptID,
    BignDate = @BeginDate,
    TotalDay = @TotalDay
WHERE ID = @ID;", connection, transaction))
                        {
                            AddVacationParameters(command, request, employee, id, userId, fromDate, toDate, resumeWork, requestedDays);
                            command.ExecuteNonQuery();
                        }
                        SendVacationToApproval(connection, transaction, employee, id, userId, null, "تم تعديل طلب الإجازة وإعادة إرساله للمراجعة.");
                    }

                    transaction.Commit();
                    return new LegacyHrFinanceSaveResult { Success = true, Id = id, Message = "تم حفظ وإرسال طلب الإجازة للمراجعة بنجاح." };
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        public LegacyHrFinanceSaveResult ManagerApproveVacation(int id, int? userId, string userName, string remarks)
        {
            return ChangeVacationApproval(id, userId, userName, remarks, "MANAGER_APPROVE");
        }

        public LegacyHrFinanceSaveResult HrApproveVacation(int id, int? userId, string userName, string remarks)
        {
            return ChangeVacationApproval(id, userId, userName, remarks, "HR_APPROVE");
        }

        public LegacyHrFinanceSaveResult RejectVacation(int id, int? userId, string userName, string remarks)
        {
            return ChangeVacationApproval(id, userId, userName, remarks, "REJECT");
        }

        public LegacyHrFinanceSaveResult CancelVacation(int id, int? userId, string userName, string remarks)
        {
            return ChangeVacationApproval(id, userId, userName, remarks, "CANCEL");
        }

        public LegacyHrFinanceSaveResult CreateVacationEntitlementFromRequest(int vacationId, int? userId)
        {
            if (vacationId <= 0) { return Fail("رقم طلب الإجازة غير صحيح."); }

            using (var connection = _connectionFactory.CreateOpenConnection())
            using (var transaction = connection.BeginTransaction())
            {
                try
                {
                    var vacation = LoadVacationById(connection, transaction, vacationId);
                    if (vacation == null) { transaction.Rollback(); return Fail("لم يتم العثور على طلب الإجازة."); }
                    if (vacation.Rejected) { transaction.Rollback(); return Fail("لا يمكن إنشاء مستحقات لطلب إجازة مرفوض أو ملغى."); }
                    if (!vacation.HrApproved) { transaction.Rollback(); return Fail("يجب اعتماد طلب الإجازة نهائياً قبل إنشاء مستند المستحقات."); }
                    if (vacation.LinkedToEntitlement || vacation.PaidOrSettled)
                    {
                        transaction.Rollback();
                        return Fail("تم إنشاء مستند مستحقات لهذا الطلب من قبل. لا يمكن تكرار المستند لنفس طلب الإجازة.");
                    }

                    if (VacationEntitlementExists(connection, transaction, vacationId, null))
                    {
                        transaction.Rollback();
                        return Fail("يوجد مستند مستحقات مرتبط بنفس طلب الإجازة بالفعل.");
                    }

                    var employee = GetEmployee(connection, transaction, vacation.EmployeeId.GetValueOrDefault(), false);
                    if (employee == null) { transaction.Rollback(); return Fail("بيانات الموظف غير موجودة."); }

                    var entitlementId = NextId(connection, transaction, "TblVocationEntitlements", "ID");
                    var fromDate = ParseDate(vacation.FromDate).GetValueOrDefault(DateTime.Today).Date;
                    var toDate = ParseDate(vacation.ToDate).GetValueOrDefault(fromDate).Date;
                    var recordDate = DateTime.Today;
                    var days = vacation.NoVacation > 0 ? vacation.NoVacation : RoundDays((decimal)(toDate - fromDate).TotalDays + 1m);
                    var balance = CalculateVacationBalance(connection, transaction, new VacationBalanceRequestViewModel
                    {
                        EmployeeId = employee.Id,
                        AsOfDate = fromDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                        VacationStartDate = fromDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                        VacationEndDate = toDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                        RequestedDays = days,
                        ExcludeVacationId = vacationId
                    });

                    using (var command = new SqlCommand(@"
INSERT INTO dbo.TblVocationEntitlements
(ID, RecordDate, DateSta, OpretotID, UserID, EmpID, BranchID, JobID, DeptID, BignDate, LastVocatinDate,
 ContDay, LastDayVoc, TotalDay, NoDay, NoMonth, NoYear, Remark, DaySalary, Salary, DayIncrease, Increase,
 DaySalVocation, SalaryVocation, DayEntitOther, SalEntitOther, Other, Advance, ValueTickt, Booked, Delivery,
 DayAbs, MoAbs, YearAbs, ToalAbsent, DuVocation, Chekk, stratDate, EndDate, LastTotal, WithoutSala1,
 BasedOn, NoOrder, NoVacation, NewAbsent, Flag, ch0, ch1, ch2, ch3, ch4, ch5, ch6, ch7, ch8,
 TotalDue, NetDue, TotalCut, NetTotal, decrease, Vact_Work, GetInsurance, LastBalanceMonth, Approved)
VALUES
(@ID, @RecordDate, @DateSta, @UserID, @UserID, @EmpID, @BranchID, @JobID, @DeptID, @BeginDate, @LastVacationDate,
 @ContDay, @LastDayVoc, @TotalDay, @NoDay, 0, 0, @Remark, 0, 0, 0, 0,
 0, 0, 0, 0, 0, 0, 0, 0, 0,
 0, 0, 0, 0, 0, 0, @StartDate, @EndDate, @LastTotal, 0,
 1, @NoOrder, @NoVacation, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
 0, 0, 0, 0, 0, 1, 0, @LastBalanceMonth, 0);", connection, transaction))
                    {
                        command.Parameters.Add("@ID", SqlDbType.Int).Value = entitlementId;
                        command.Parameters.Add("@RecordDate", SqlDbType.DateTime).Value = recordDate;
                        command.Parameters.Add("@DateSta", SqlDbType.DateTime).Value = fromDate;
                        command.Parameters.Add("@UserID", SqlDbType.Int).Value = userId.HasValue ? (object)userId.Value : DBNull.Value;
                        command.Parameters.Add("@EmpID", SqlDbType.Int).Value = employee.Id;
                        command.Parameters.Add("@BranchID", SqlDbType.Int).Value = employee.BranchId.HasValue ? (object)employee.BranchId.Value : DBNull.Value;
                        command.Parameters.Add("@JobID", SqlDbType.Int).Value = vacation.JobId.HasValue ? (object)vacation.JobId.Value : DBNull.Value;
                        command.Parameters.Add("@DeptID", SqlDbType.Int).Value = employee.DepartmentId.HasValue ? (object)employee.DepartmentId.Value : DBNull.Value;
                        command.Parameters.Add("@BeginDate", SqlDbType.DateTime).Value = fromDate;
                        command.Parameters.Add("@LastVacationDate", SqlDbType.DateTime).Value = toDate;
                        command.Parameters.Add("@ContDay", SqlDbType.Float).Value = Convert.ToDouble(balance.AccruedDays);
                        command.Parameters.Add("@LastDayVoc", SqlDbType.Float).Value = Convert.ToDouble(balance.AvailableBeforeRequest);
                        command.Parameters.Add("@TotalDay", SqlDbType.Float).Value = Convert.ToDouble(days);
                        command.Parameters.Add("@NoDay", SqlDbType.Float).Value = Convert.ToDouble(days);
                        command.Parameters.Add("@Remark", SqlDbType.NVarChar, 4000).Value = DbText("مستحقات إجازة من طلب رقم " + vacationId + ". " + (vacation.Reason ?? string.Empty));
                        command.Parameters.Add("@StartDate", SqlDbType.DateTime).Value = fromDate;
                        command.Parameters.Add("@EndDate", SqlDbType.DateTime).Value = toDate;
                        command.Parameters.Add("@LastTotal", SqlDbType.Float).Value = Convert.ToDouble(balance.AvailableAfterRequest);
                        command.Parameters.Add("@NoOrder", SqlDbType.Int).Value = vacationId;
                        command.Parameters.Add("@NoVacation", SqlDbType.Float).Value = Convert.ToDouble(days);
                        command.Parameters.Add("@LastBalanceMonth", SqlDbType.Float).Value = Convert.ToDouble(balance.AvailableAfterRequest / 30m);
                        command.ExecuteNonQuery();
                    }

                    using (var flag = new SqlCommand("UPDATE dbo.TblVocation SET FlagPayed = 1 WHERE ID = @VacationID;", connection, transaction))
                    {
                        flag.Parameters.Add("@VacationID", SqlDbType.Int).Value = vacationId;
                        flag.ExecuteNonQuery();
                    }

                    transaction.Commit();
                    return new LegacyHrFinanceSaveResult { Success = true, Id = entitlementId, Message = "تم إنشاء مستند مستحقات الإجازة وربطه بطلب الإجازة بنجاح." };
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        public LegacyHrFinanceSaveResult DeleteVacationEntitlement(int entitlementId, int? userId)
        {
            if (entitlementId <= 0) { return Fail("رقم مستند مستحقات الإجازة غير صحيح."); }

            using (var connection = _connectionFactory.CreateOpenConnection())
            using (var transaction = connection.BeginTransaction())
            {
                try
                {
                    var boundary = GetVacationEntitlementDeleteBoundary(connection, transaction, entitlementId);
                    if (!boundary.Exists) { transaction.Rollback(); return Fail("لم يتم العثور على مستند مستحقات الإجازة."); }
                    if (boundary.IsLocked)
                    {
                        transaction.Rollback();
                        return Fail(boundary.Message);
                    }

                    using (var command = new SqlCommand(@"
DELETE FROM dbo.ApprovalData WHERE ScreenName = N'FrmVocationEntitlements' AND CONVERT(INT, Transaction_ID) = @ID;
DELETE FROM dbo.TblVocationEntitlementsDet WHERE VoEntID = @ID;
DELETE FROM dbo.TblInforVacatiom WHERE VacatioID = @ID;
IF OBJECT_ID(N'dbo.TblVacationSalary', N'U') IS NOT NULL
BEGIN
    DELETE FROM dbo.TblVacationSalary WHERE VacationID = @ID;
END
DELETE FROM dbo.TblVocationEntitlements WHERE ID = @ID;", connection, transaction))
                    {
                        command.Parameters.Add("@ID", SqlDbType.Int).Value = entitlementId;
                        command.ExecuteNonQuery();
                    }

                    if (boundary.NoOrder.HasValue && boundary.NoOrder.Value > 0)
                    {
                        using (var flag = new SqlCommand(@"
UPDATE dbo.TblVocation
SET FlagPayed = NULL
WHERE ID = @VacationID
  AND NOT EXISTS (SELECT 1 FROM dbo.TblVocationEntitlements WHERE ISNULL(NoOrder,0) = @VacationID);", connection, transaction))
                        {
                            flag.Parameters.Add("@VacationID", SqlDbType.Int).Value = boundary.NoOrder.Value;
                            flag.ExecuteNonQuery();
                        }
                    }

                    transaction.Commit();
                    return new LegacyHrFinanceSaveResult { Success = true, Id = entitlementId, Message = "تم حذف مستند مستحقات الإجازة وعكس الربط مع طلب الإجازة." };
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        public LegacyHrFinanceSaveResult SaveVacationReturnToWork(VacationReturnToWorkViewModel request, int? userId)
        {
            if (request == null || !request.EntitlementId.HasValue || request.EntitlementId.Value <= 0) { return Fail("يجب تحديد مستند مستحقات الإجازة."); }
            var actualReturnDate = ParseDate(request.ActualReturnDate);
            if (!actualReturnDate.HasValue) { return Fail("تاريخ المباشرة الفعلي غير صحيح."); }
            if (request.ActualVacationDays.HasValue && request.ActualVacationDays.Value < 0) { return Fail("أيام الإجازة الفعلية لا تقبل قيماً سالبة."); }
            if (request.DelayDays.HasValue && request.DelayDays.Value < 0) { return Fail("أيام التأخير لا تقبل قيماً سالبة."); }

            using (var connection = _connectionFactory.CreateOpenConnection())
            using (var transaction = connection.BeginTransaction())
            {
                try
                {
                    var entitlement = GetVacationEntitlement(connection, transaction, request.EntitlementId.Value);
                    if (entitlement == null) { transaction.Rollback(); return Fail("لم يتم العثور على مستند مستحقات الإجازة."); }
                    var lockMessage = GetVacationReturnPaymentLockMessage(connection, transaction, request.EntitlementId.Value);
                    if (!string.IsNullOrWhiteSpace(lockMessage)) { transaction.Rollback(); return Fail(lockMessage); }

                    var actualDate = actualReturnDate.Value.Date;
                    var approvedDays = entitlement.ApprovedDays > 0 ? entitlement.ApprovedDays : DateDiffInclusive(entitlement.StartDate, entitlement.EndDate);
                    var actualDays = request.ActualVacationDays.HasValue && request.ActualVacationDays.Value > 0
                        ? request.ActualVacationDays.Value
                        : DateDiffInclusive(entitlement.StartDate, actualDate);
                    var delayDays = request.DelayDays.HasValue
                        ? request.DelayDays.Value
                        : Math.Max(0m, RoundDays((decimal)(actualDate - entitlement.EndDate).TotalDays));
                    var treatment = NormalizeDelayTreatment(request.DelayTreatment);
                    if (delayDays > 0 && treatment == "none") { transaction.Rollback(); return Fail("يجب تحديد طريقة معالجة أيام التأخير: بدون راتب أو خصم من الرصيد."); }

                    var embarkationId = GetEmbarkationIdByEntitlement(connection, transaction, entitlement.Id);
                    if (!embarkationId.HasValue)
                    {
                        embarkationId = NextId(connection, transaction, "TblEmbarkation", "id");
                        using (var command = new SqlCommand(@"
INSERT INTO dbo.TblEmbarkation
(id, branch_no, Emp_ID, recorddate, DeparmentID, JobTypeID, Indate, indateH, workdate, workdateH, UserID, Remark,
 Vac_new, vac_Bak, ApprovVacPeriod, ActiveVacPeriod, MoveVacBalance, Join_Work, stratDateH, EndDateH, stratDate,
 EndDate, UnPaid_Dis, OrderVocation, NoVationUnPaed, TypeVacation, RdTypeVaction)
VALUES
(@ID, @BranchID, @EmpID, @RecordDate, @DeptID, @JobID, @StartDate, @StartDateH, @WorkDate, @WorkDateH, @UserID, @Remark,
 0, 1, @ApprovedDays, @ActualDays, @DelayDays, 0, @StartDateH, @EndDateH, @StartDate,
 @EndDate, @UnPaidDis, @OrderVocation, 0, 1, 0);", connection, transaction))
                        {
                            AddEmbarkationParameters(command, embarkationId.Value, entitlement, actualDate, approvedDays, actualDays, delayDays, treatment, userId, request.Remarks);
                            command.ExecuteNonQuery();
                        }
                    }
                    else
                    {
                        using (var cleanup = new SqlCommand("DELETE FROM dbo.TblInforVacatiom WHERE PrkID = @ID; DELETE FROM dbo.tblVacationData WHERE EmbracID = @ID;", connection, transaction))
                        {
                            cleanup.Parameters.Add("@ID", SqlDbType.Int).Value = embarkationId.Value;
                            cleanup.ExecuteNonQuery();
                        }

                        using (var command = new SqlCommand(@"
UPDATE dbo.TblEmbarkation
SET branch_no = @BranchID,
    Emp_ID = @EmpID,
    recorddate = @RecordDate,
    DeparmentID = @DeptID,
    JobTypeID = @JobID,
    Indate = @StartDate,
    indateH = @StartDateH,
    workdate = @WorkDate,
    workdateH = @WorkDateH,
    UserID = @UserID,
    Remark = @Remark,
    Vac_new = 0,
    vac_Bak = 1,
    ApprovVacPeriod = @ApprovedDays,
    ActiveVacPeriod = @ActualDays,
    MoveVacBalance = @DelayDays,
    Join_Work = 0,
    stratDateH = @StartDateH,
    EndDateH = @EndDateH,
    stratDate = @StartDate,
    EndDate = @EndDate,
    UnPaid_Dis = @UnPaidDis,
    OrderVocation = @OrderVocation,
    NoVationUnPaed = 0,
    TypeVacation = 1,
    RdTypeVaction = 0
WHERE id = @ID;", connection, transaction))
                        {
                            AddEmbarkationParameters(command, embarkationId.Value, entitlement, actualDate, approvedDays, actualDays, delayDays, treatment, userId, request.Remarks);
                            command.ExecuteNonQuery();
                        }
                    }

                    SaveReturnDelayMovement(connection, transaction, embarkationId.Value, entitlement.EmployeeId, actualDate, delayDays, treatment);

                    using (var update = new SqlCommand(@"
UPDATE dbo.TblVocationEntitlements
SET AcuDate = @ActualDate,
    AcuDateH = @ActualDateH,
    NoVacation = @ApprovedDays,
    NoDayAct = @ActualDays,
    NoDayDelay = @DelayDays
WHERE ID = @ID;", connection, transaction))
                    {
                        update.Parameters.Add("@ActualDate", SqlDbType.DateTime).Value = actualDate;
                        update.Parameters.Add("@ActualDateH", SqlDbType.NVarChar, 40).Value = DbText(ToHijriDate(actualDate));
                        update.Parameters.Add("@ApprovedDays", SqlDbType.Float).Value = Convert.ToDouble(approvedDays);
                        update.Parameters.Add("@ActualDays", SqlDbType.Float).Value = Convert.ToDouble(actualDays);
                        update.Parameters.Add("@DelayDays", SqlDbType.Float).Value = Convert.ToDouble(delayDays);
                        update.Parameters.Add("@ID", SqlDbType.Int).Value = entitlement.Id;
                        update.ExecuteNonQuery();
                    }

                    transaction.Commit();
                    return new LegacyHrFinanceSaveResult { Success = true, Id = embarkationId.Value, Message = "تم تسجيل مباشرة العمل وتحديث مستند مستحقات الإجازة بنجاح." };
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        public LegacyHrFinanceSaveResult DeleteVacationReturnToWork(int entitlementId)
        {
            if (entitlementId <= 0) { return Fail("رقم مستند مستحقات الإجازة غير صحيح."); }

            using (var connection = _connectionFactory.CreateOpenConnection())
            using (var transaction = connection.BeginTransaction())
            {
                try
                {
                    var lockMessage = GetVacationReturnPaymentLockMessage(connection, transaction, entitlementId);
                    if (!string.IsNullOrWhiteSpace(lockMessage)) { transaction.Rollback(); return Fail(lockMessage); }

                    var embarkationId = GetEmbarkationIdByEntitlement(connection, transaction, entitlementId);
                    if (!embarkationId.HasValue) { transaction.Rollback(); return Fail("لا توجد مباشرة عمل مرتبطة بهذا المستند."); }

                    using (var command = new SqlCommand(@"
UPDATE dbo.TblVocationEntitlements
SET AcuDate = NULL,
    AcuDateH = NULL,
    NoVacation = 0,
    NoDayAct = 0,
    NoDayDelay = 0
WHERE ID = @EntitlementID;
DELETE FROM dbo.TblInforVacatiom WHERE PrkID = @EmbarkationID;
DELETE FROM dbo.tblVacationData WHERE EmbracID = @EmbarkationID;
DELETE FROM dbo.TblEmbarkation WHERE id = @EmbarkationID;", connection, transaction))
                    {
                        command.Parameters.Add("@EntitlementID", SqlDbType.Int).Value = entitlementId;
                        command.Parameters.Add("@EmbarkationID", SqlDbType.Int).Value = embarkationId.Value;
                        command.ExecuteNonQuery();
                    }

                    transaction.Commit();
                    return new LegacyHrFinanceSaveResult { Success = true, Id = embarkationId.Value, Message = "تم حذف مباشرة العمل وعكس بيانات العودة من مستند المستحقات." };
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
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

        public LegacyHrFinanceSaveResult DisburseAdvanceRequest(int requestId, int? userId)
        {
            if (requestId <= 0) { return Fail("رقم طلب السلفة غير صحيح."); }
            using (var connection = _connectionFactory.CreateOpenConnection())
            using (var transaction = connection.BeginTransaction())
            {
                try
                {
                    LockAdvanceRequestForDisbursement(connection, transaction, requestId);
                    var request = LoadAdvanceById(connection, transaction, requestId);
                    if (request == null) { transaction.Rollback(); return Fail("لم يتم العثور على طلب السلفة."); }
                    if (!request.Approved) { transaction.Rollback(); return Fail("لا يمكن صرف السلفة قبل اعتماد طلب السلفة."); }

                    var existingActualId = GetActualAdvanceIdForRequest(connection, transaction, requestId);
                    if (existingActualId.HasValue)
                    {
                        var duplicateBoundary = BuildAdvanceAccountingBoundary(connection, transaction, requestId);
                        transaction.Rollback();
                        return Fail("تم صرف هذا الطلب من قبل برقم سلفة فعلية " + existingActualId.Value + ". لا يمكن تكرار الصرف. " + duplicateBoundary.BoundaryMessage);
                    }

                    var employee = GetEmployee(connection, transaction, request.EmployeeId.GetValueOrDefault(), true);
                    if (employee == null) { transaction.Rollback(); return Fail("الموظف غير موجود أو موقوف. لا يمكن صرف السلفة."); }

                    LoadAdvanceParts(connection, transaction, request);
                    if (!request.Parts.Any()) { transaction.Rollback(); return Fail("لا توجد أقساط في طلب السلفة يمكن صرفها."); }

                    var actualId = NextId(connection, transaction, "TblEmpAdvance", "AdvanceID");
                    using (var command = new SqlCommand(@"
INSERT INTO dbo.TblEmpAdvance
(AdvanceID, AdvanceDate, Emp_ID, AdvanceValue, BoxID, PaymentCounts, AutoDiscount, FirstMonthPayment, FirstYearPayment, UserID, AdvanceType, RetrunID, branch_no, orderNO, MethodDeci, DifValue)
VALUES
(@AdvanceID, @AdvanceDate, @EmployeeId, @AdvanceValue, NULL, @PaymentCounts, @AutoDiscount, @FirstMonthPayment, @FirstYearPayment, @UserID, 0, NULL, @BranchNo, @RequestId, @MethodDeci, @DiffValue);", connection, transaction))
                    {
                        command.Parameters.Add("@AdvanceID", SqlDbType.Int).Value = actualId;
                        command.Parameters.Add("@AdvanceDate", SqlDbType.DateTime).Value = ParseDate(request.AdvanceDate).GetValueOrDefault(DateTime.Today);
                        command.Parameters.Add("@EmployeeId", SqlDbType.Int).Value = request.EmployeeId.GetValueOrDefault();
                        command.Parameters.Add("@AdvanceValue", SqlDbType.Money).Value = request.AdvanceValue;
                        command.Parameters.Add("@PaymentCounts", SqlDbType.Int).Value = request.PaymentCounts;
                        command.Parameters.Add("@AutoDiscount", SqlDbType.Bit).Value = request.AutoDiscount;
                        command.Parameters.Add("@FirstMonthPayment", SqlDbType.Int).Value = request.FirstMonthPayment.GetValueOrDefault();
                        command.Parameters.Add("@FirstYearPayment", SqlDbType.Int).Value = request.FirstYearPayment.GetValueOrDefault();
                        command.Parameters.Add("@UserID", SqlDbType.Int).Value = userId.HasValue ? (object)userId.Value : DBNull.Value;
                        command.Parameters.Add("@BranchNo", SqlDbType.Int).Value = request.BranchId.HasValue ? (object)request.BranchId.Value : DBNull.Value;
                        command.Parameters.Add("@RequestId", SqlDbType.Float).Value = requestId;
                        command.Parameters.Add("@MethodDeci", SqlDbType.Int).Value = 2;
                        command.Parameters.Add("@DiffValue", SqlDbType.Float).Value = 0;
                        command.ExecuteNonQuery();
                    }

                    using (var command = new SqlCommand(@"
INSERT INTO dbo.TblEmpAdvanceDetails (AdvanceID, PartNO, PartValue, PartDate)
VALUES (@AdvanceID, @PartNo, @PartValue, @PartDate);", connection, transaction))
                    {
                        command.Parameters.Add("@AdvanceID", SqlDbType.Int);
                        command.Parameters.Add("@PartNo", SqlDbType.Int);
                        command.Parameters.Add("@PartValue", SqlDbType.Money);
                        command.Parameters.Add("@PartDate", SqlDbType.DateTime);
                        foreach (var part in request.Parts)
                        {
                            command.Parameters["@AdvanceID"].Value = actualId;
                            command.Parameters["@PartNo"].Value = part.PartNo;
                            command.Parameters["@PartValue"].Value = part.PartValue;
                            command.Parameters["@PartDate"].Value = ParseDate(part.PartDate).GetValueOrDefault(DateTime.Today);
                            command.ExecuteNonQuery();
                        }
                    }

                    var boundary = BuildAdvanceAccountingBoundary(connection, transaction, requestId);
                    transaction.Commit();
                    return new LegacyHrFinanceSaveResult
                    {
                        Success = true,
                        Message = "تم صرف السلفة وإنشاء السلفة الفعلية رقم " + actualId + ". " + boundary.BoundaryMessage,
                        Id = actualId
                    };
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        public LegacyHrFinanceSaveResult SendAdvanceForApproval(int requestId, int? userId, string userName, string remarks)
        {
            if (requestId <= 0) { return Fail("رقم طلب السلفة غير صحيح."); }

            using (var connection = _connectionFactory.CreateOpenConnection())
            using (var transaction = connection.BeginTransaction())
            {
                try
                {
                    var request = LoadAdvanceById(connection, transaction, requestId);
                    if (request == null) { transaction.Rollback(); return Fail("لم يتم العثور على طلب السلفة."); }
                    if (request.Rejected) { transaction.Rollback(); return Fail("لا يمكن إرسال طلب سلفة مرفوض أو ملغى للاعتماد."); }
                    if (request.IsDisbursed) { transaction.Rollback(); return Fail("تم صرف الطلب بالفعل ولا يمكن إعادة إرساله للاعتماد."); }
                    if (request.Approved) { transaction.Rollback(); return Fail("طلب السلفة معتمد بالفعل."); }
                    if (request.Submitted) { transaction.Rollback(); return Fail("تم إرسال طلب السلفة للاعتماد بالفعل."); }

                    var employee = GetEmployee(connection, transaction, request.EmployeeId.GetValueOrDefault(), false);
                    if (employee == null)
                    {
                        transaction.Rollback();
                        return Fail("لا يمكن إرسال الطلب للاعتماد لأن بيانات الموظف غير موجودة.");
                    }

                    SendAdvanceToApproval(connection, transaction, employee, requestId, userId, userName, remarks);

                    using (var command = new SqlCommand(@"
UPDATE dbo.TblEmpAdvanceRequest
SET Posted = 1,
    PostedDate = GETDATE(),
    Approved = 0,
    ok = 0,
    notok = 0,
    ManagerID = ISNULL(ManagerID, @UserID),
    jobID_approve = ISNULL(jobID_approve, @UserID)
WHERE AdvanceID = @AdvanceID;", connection, transaction))
                    {
                        command.Parameters.Add("@AdvanceID", SqlDbType.Int).Value = requestId;
                        command.Parameters.Add("@UserID", SqlDbType.Int).Value = userId.HasValue ? (object)userId.Value : DBNull.Value;
                        command.ExecuteNonQuery();
                    }

                    transaction.Commit();
                    return new LegacyHrFinanceSaveResult { Success = true, Id = requestId, Message = "تم إرسال طلب السلفة للاعتماد بنجاح." };
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        public LegacyHrFinanceSaveResult ApproveAdvanceRequest(int requestId, int? userId, string userName, string remarks)
        {
            if (requestId <= 0) { return Fail("رقم طلب السلفة غير صحيح."); }

            using (var connection = _connectionFactory.CreateOpenConnection())
            using (var transaction = connection.BeginTransaction())
            {
                try
                {
                    var request = LoadAdvanceById(connection, transaction, requestId);
                    if (request == null) { transaction.Rollback(); return Fail("لم يتم العثور على طلب السلفة."); }
                    if (request.Rejected) { transaction.Rollback(); return Fail("لا يمكن اعتماد طلب سلفة ملغى أو مرفوض."); }
                    if (request.IsDisbursed) { transaction.Rollback(); return Fail("تم صرف الطلب بالفعل ولا يحتاج إلى اعتماد جديد."); }
                    if (request.Approved) { transaction.Rollback(); return Fail("طلب السلفة معتمد بالفعل."); }
                    if (!request.Submitted) { transaction.Rollback(); return Fail("يجب إرسال طلب السلفة للاعتماد قبل اعتماده."); }

                    var currentApprovalId = GetAdvanceCurrentApprovalId(connection, transaction, requestId);
                    if (!currentApprovalId.HasValue)
                    {
                        if (HasAdvanceApprovalRows(connection, transaction, requestId))
                        {
                            transaction.Rollback();
                            return Fail("لا يوجد مستوى موافقة حالي لهذا الطلب. يرجى مراجعة إعدادات الموافقات أو إعادة إرسال الطلب للمراجعة.");
                        }

                        var employee = GetEmployee(connection, transaction, request.EmployeeId.GetValueOrDefault(), false);
                        if (employee == null)
                        {
                            transaction.Rollback();
                            return Fail("لا يمكن إنشاء مسار الموافقة لأن بيانات الموظف غير موجودة.");
                        }

                        SendAdvanceToApproval(connection, transaction, employee, requestId, userId, userName, "تم إنشاء مسار موافقة لطلب السلفة من الويب.");
                        currentApprovalId = GetAdvanceCurrentApprovalId(connection, transaction, requestId);
                        if (!currentApprovalId.HasValue)
                        {
                            transaction.Rollback();
                            return Fail("تعذر تحديد مستوى الموافقة الحالي بعد إرسال الطلب.");
                        }
                    }

                    using (var approve = new SqlCommand(@"
UPDATE dbo.ApprovalData
SET Currcursor = NULL,
    ApprovDate = GETDATE(),
    CancelApprove = NULL,
    Remarks = @Remarks,
    FromUser = @FromUser
WHERE id = @ApprovalID
  AND ScreenName = N'FrmEmpsAdvanceRequest'
  AND ApprovDate IS NULL
  AND CancelApprove IS NULL;", connection, transaction))
                    {
                        approve.Parameters.Add("@ApprovalID", SqlDbType.Int).Value = currentApprovalId.Value;
                        approve.Parameters.Add("@Remarks", SqlDbType.NVarChar, 4000).Value = DbText(string.IsNullOrWhiteSpace(remarks) ? "تم اعتماد مستوى الموافقة من الويب." : remarks);
                        approve.Parameters.Add("@FromUser", SqlDbType.NVarChar, 200).Value = DbText(userName);
                        if (approve.ExecuteNonQuery() == 0)
                        {
                            transaction.Rollback();
                            return Fail("تعذر اعتماد مستوى الموافقة الحالي، قد يكون تم اعتماده أو إلغاؤه من مستخدم آخر.");
                        }
                    }

                    var nextApprovalId = GetAdvanceNextApprovalId(connection, transaction, requestId);
                    if (nextApprovalId.HasValue)
                    {
                        using (var next = new SqlCommand(@"
UPDATE dbo.ApprovalData
SET Currcursor = 1,
    SendTime = GETDATE(),
    FromUser = @FromUser
WHERE id = @ApprovalID;", connection, transaction))
                        {
                            next.Parameters.Add("@ApprovalID", SqlDbType.Int).Value = nextApprovalId.Value;
                            next.Parameters.Add("@FromUser", SqlDbType.NVarChar, 200).Value = DbText(userName);
                            next.ExecuteNonQuery();
                        }

                        transaction.Commit();
                        return new LegacyHrFinanceSaveResult { Success = true, Id = requestId, Message = "تم اعتماد المستوى الحالي وإرسال الطلب للمستوى التالي." };
                    }

                    using (var command = new SqlCommand(@"
UPDATE dbo.TblEmpAdvanceRequest
SET Approved = 1,
    ok = 1,
    notok = 0,
    ManagerID = @UserID,
    jobID_approve = ISNULL(jobID_approve, @UserID)
WHERE AdvanceID = @AdvanceID;", connection, transaction))
                    {
                        command.Parameters.Add("@AdvanceID", SqlDbType.Int).Value = requestId;
                        command.Parameters.Add("@UserID", SqlDbType.Int).Value = userId.HasValue ? (object)userId.Value : DBNull.Value;
                        command.ExecuteNonQuery();
                    }

                    transaction.Commit();
                    return new LegacyHrFinanceSaveResult { Success = true, Id = requestId, Message = "تم اعتماد طلب السلفة نهائياً." };
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        public LegacyHrFinanceSaveResult CancelAdvanceRequest(int requestId, int? userId, string userName, string remarks)
        {
            if (requestId <= 0) { return Fail("رقم طلب السلفة غير صحيح."); }

            using (var connection = _connectionFactory.CreateOpenConnection())
            using (var transaction = connection.BeginTransaction())
            {
                try
                {
                    var request = LoadAdvanceById(connection, transaction, requestId);
                    if (request == null) { transaction.Rollback(); return Fail("لم يتم العثور على طلب السلفة."); }
                    if (request.Rejected) { transaction.Rollback(); return Fail("طلب السلفة ملغى بالفعل."); }
                    var cancellationBlock = GetAdvanceCancellationBlockReason(connection, transaction, requestId, request);
                    if (!string.IsNullOrWhiteSpace(cancellationBlock)) { transaction.Rollback(); return Fail(cancellationBlock); }

                    using (var cancel = new SqlCommand(@"
UPDATE dbo.ApprovalData
SET Currcursor = NULL,
    CancelApprove = CASE WHEN ApprovDate IS NULL AND CancelApprove IS NULL THEN GETDATE() ELSE CancelApprove END,
    Remarks = CASE WHEN ApprovDate IS NULL AND CancelApprove IS NULL THEN @Remarks ELSE Remarks END,
    FromUser = CASE WHEN ApprovDate IS NULL AND CancelApprove IS NULL THEN @FromUser ELSE FromUser END
WHERE ScreenName = N'FrmEmpsAdvanceRequest'
  AND CONVERT(INT, Transaction_ID) = @AdvanceID;", connection, transaction))
                    {
                        cancel.Parameters.Add("@AdvanceID", SqlDbType.Int).Value = requestId;
                        cancel.Parameters.Add("@Remarks", SqlDbType.NVarChar, 4000).Value = DbText(string.IsNullOrWhiteSpace(remarks) ? "تم إلغاء طلب السلفة من الويب." : remarks);
                        cancel.Parameters.Add("@FromUser", SqlDbType.NVarChar, 200).Value = DbText(userName);
                        cancel.ExecuteNonQuery();
                    }

                    using (var command = new SqlCommand(@"
UPDATE dbo.TblEmpAdvanceRequest
SET Approved = 0,
    ok = 0,
    notok = 1,
    AccAproved = 0,
    Posted = NULL,
    PostedDate = NULL,
    jobID_approve = NULL
WHERE AdvanceID = @AdvanceID;", connection, transaction))
                    {
                        command.Parameters.Add("@AdvanceID", SqlDbType.Int).Value = requestId;
                        command.ExecuteNonQuery();
                    }

                    transaction.Commit();
                    return new LegacyHrFinanceSaveResult { Success = true, Id = requestId, Message = "تم إلغاء طلب السلفة بأمان." };
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
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
            {
                return GetComponent(connection, null, id);
            }
        }

        private PayrollComponentEditViewModel GetComponent(SqlConnection connection, SqlTransaction transaction, int id)
        {
            using (var command = new SqlCommand("SELECT TOP (1) * FROM dbo.mofrad WITH (NOLOCK) WHERE id = @Id", connection, transaction))
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

        public ChangedComponentEntryViewModel GetChangedComponent(int detailId)
        {
            using (var connection = _connectionFactory.CreateOpenConnection())
            {
                return LoadChangedComponentByDetailId(connection, null, detailId);
            }
        }

        public LegacyHrFinanceSaveResult SaveChangedComponent(ChangedComponentEntryViewModel request, int? userId)
        {
            using (var connection = _connectionFactory.CreateOpenConnection())
            using (var transaction = connection.BeginTransaction())
            {
                try
                {
                    var result = SaveChangedComponentCore(connection, transaction, request, userId);
                    if (!result.Success)
                    {
                        transaction.Rollback();
                        return result;
                    }
                    transaction.Commit();
                    return result;
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        public ChangedComponentBulkPreviewViewModel PreviewChangedComponentBulk(ChangedComponentBulkRequestViewModel request)
        {
            using (var connection = _connectionFactory.CreateOpenConnection())
            {
                return BuildChangedComponentBulkPreview(connection, null, request);
            }
        }

        public LegacyHrFinanceSaveResult SaveChangedComponentBulk(ChangedComponentBulkRequestViewModel request, int? userId)
        {
            using (var connection = _connectionFactory.CreateOpenConnection())
            using (var transaction = connection.BeginTransaction())
            {
                try
                {
                    var preview = BuildChangedComponentBulkPreview(connection, transaction, request);
                    if (!preview.Success || preview.ValidRows == 0)
                    {
                        transaction.Rollback();
                        return Fail(preview.Message ?? "لا توجد سطور صالحة للحفظ.");
                    }

                    if (preview.InvalidRows > 0)
                    {
                        transaction.Rollback();
                        return Fail("يوجد أخطاء في المعاينة. صحح السطور غير الصالحة قبل الحفظ.");
                    }

                    var saved = 0;
                    var lastId = 0;
                    foreach (var entry in preview.Entries)
                    {
                        var result = SaveChangedComponentCore(connection, transaction, entry, userId);
                        if (!result.Success)
                        {
                            transaction.Rollback();
                            return Fail(result.Message);
                        }

                        saved++;
                        lastId = result.Id.GetValueOrDefault();
                    }

                    transaction.Commit();
                    return new LegacyHrFinanceSaveResult { Success = true, Id = lastId, Message = "تم حفظ " + saved.ToString(CultureInfo.InvariantCulture) + " مفردة متغيرة بعد المعاينة." };
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        private LegacyHrFinanceSaveResult SaveChangedComponentCore(SqlConnection connection, SqlTransaction transaction, ChangedComponentEntryViewModel request, int? userId)
        {
            var validation = ValidateChangedComponent(request);
            if (!validation.Success) { return validation; }

            var employee = GetEmployee(connection, transaction, request.EmployeeId.GetValueOrDefault(), true);
            if (employee == null)
            {
                return Fail("الموظف غير موجود أو موقوف. لا يمكن تسجيل مفردة متغيرة لموظف غير نشط.");
            }

            var component = GetComponent(connection, transaction, request.ComponentId.GetValueOrDefault());
            if (component == null || !component.FixedOrChanged || !component.ViewComponent)
            {
                return Fail("المفردة المختارة ليست مفردة متغيرة ظاهرة في مسير الرواتب.");
            }

            var componentValidation = ValidateChangedComponentForComponent(request, component);
            if (!componentValidation.Success) { return componentValidation; }

            var detailId = request.Id.GetValueOrDefault();
            var current = detailId > 0 ? LoadChangedComponentByDetailId(connection, transaction, detailId) : null;
            if (detailId > 0)
            {
                if (current == null) { return Fail("لم يتم العثور على سجل المفردة المطلوب تعديله."); }
                if (!current.CanEdit) { return Fail(current.LockReason); }
            }

            if (ChangedComponentDuplicateExists(connection, transaction, detailId, employee.Id, request.ComponentId.GetValueOrDefault(), request.Year, request.Month))
            {
                return Fail("توجد مفردة متغيرة لنفس الموظف ونفس الشهر ونفس المفردة. لا يمكن تكرارها من شاشة الويب.");
            }

            if (ChangedComponentPeriodLocked(connection, transaction, request.Year, request.Month, employee.BranchId))
            {
                return Fail("تم استخدام هذه الفترة في مسير الرواتب أو سند رواتب. لا يمكن إضافة أو تعديل مفردة متغيرة عليها من شاشة الويب.");
            }

            var recordDate = ParseDate(request.RecordDate).GetValueOrDefault(new DateTime(request.Year, request.Month, 1)).Date;
            var headerId = detailId > 0 ? current.HeaderId.GetValueOrDefault() : NextId(connection, transaction, "TblChangedComponentRegister", "ChangedComponentid");

            if (detailId <= 0)
            {
                using (var command = new SqlCommand(@"
INSERT INTO dbo.TblChangedComponentRegister
(ChangedComponentid, RecordDate, [year], [month], ComponentID, All_Or_SelectedEmployee, Actualyear, Actualmonth, BranchId, LocationID, Reason, BrnchID1, SelectBranch, DeptID1, SelectDept, ProjectID, SelectProj1, SelectEmp, SelectAll, EmpID1, RdTyp, Remarks)
VALUES
(@HeaderId, @RecordDate, @YearIndex, @MonthIndex, @ComponentId, NULL, @Year, @Month, @BranchId, @DepartmentId, @Remarks, @BranchId, 0, @DepartmentId, 0, @ProjectId, 0, 1, 0, @EmployeeId, @Unit, @Remarks);", connection, transaction))
                {
                    AddChangedHeaderParameters(command, request, employee, component, headerId, recordDate);
                    command.ExecuteNonQuery();
                }

                using (var command = new SqlCommand(@"
INSERT INTO dbo.TblChangedComponentRegisterDetails
(ChangedComponentid, Emp_id, Remarks, [value], HourRate, NoOfHour, NoOfMinutes, NoofDays, Salary, projectid)
VALUES
(@HeaderId, @EmployeeId, @Remarks, @Value, @HourRate, @NoOfHours, @NoOfMinutes, @NoOfDays, @Salary, @ProjectId);
SELECT CONVERT(INT, SCOPE_IDENTITY());", connection, transaction))
                {
                    AddChangedDetailParameters(command, request, employee, 0, headerId);
                    detailId = Convert.ToInt32(command.ExecuteScalar());
                }
            }
            else
            {
                using (var command = new SqlCommand(@"
UPDATE dbo.TblChangedComponentRegister
SET RecordDate = @RecordDate,
    [year] = @YearIndex,
    [month] = @MonthIndex,
    ComponentID = @ComponentId,
    Actualyear = @Year,
    Actualmonth = @Month,
    BranchId = @BranchId,
    LocationID = @DepartmentId,
    Reason = @Remarks,
    BrnchID1 = @BranchId,
    DeptID1 = @DepartmentId,
    ProjectID = @ProjectId,
    SelectEmp = 1,
    SelectAll = 0,
    EmpID1 = @EmployeeId,
    RdTyp = @Unit,
    Remarks = @Remarks
WHERE ChangedComponentid = @HeaderId;", connection, transaction))
                {
                    AddChangedHeaderParameters(command, request, employee, component, headerId, recordDate);
                    command.ExecuteNonQuery();
                }

                using (var command = new SqlCommand(@"
UPDATE dbo.TblChangedComponentRegisterDetails
SET Emp_id = @EmployeeId,
    Remarks = @Remarks,
    [value] = @Value,
    HourRate = @HourRate,
    NoOfHour = @NoOfHours,
    NoOfMinutes = @NoOfMinutes,
    NoofDays = @NoOfDays,
    Salary = @Salary,
    projectid = @ProjectId
WHERE id = @DetailId AND ChangedComponentid = @HeaderId;", connection, transaction))
                {
                    AddChangedDetailParameters(command, request, employee, detailId, headerId);
                    command.ExecuteNonQuery();
                }
            }

            return new LegacyHrFinanceSaveResult { Success = true, Id = detailId, Message = "تم حفظ المفردة المتغيرة بنجاح." };
        }

        public LegacyHrFinanceSaveResult DeleteChangedComponent(int detailId)
        {
            using (var connection = _connectionFactory.CreateOpenConnection())
            using (var transaction = connection.BeginTransaction())
            {
                try
                {
                    var current = LoadChangedComponentByDetailId(connection, transaction, detailId);
                    if (current == null) { transaction.Rollback(); return Fail("لم يتم العثور على سجل المفردة المطلوب حذفه."); }
                    if (!current.CanDelete) { transaction.Rollback(); return Fail(current.LockReason); }

                    using (var command = new SqlCommand("DELETE FROM dbo.TblChangedComponentRegisterDetails WHERE id = @DetailId;", connection, transaction))
                    {
                        command.Parameters.Add("@DetailId", SqlDbType.Int).Value = detailId;
                        command.ExecuteNonQuery();
                    }

                    if (Scalar(connection, transaction, "SELECT COUNT(1) FROM dbo.TblChangedComponentRegisterDetails WHERE ChangedComponentid = @HeaderId", new SqlParameter("@HeaderId", current.HeaderId.GetValueOrDefault())) == 0)
                    {
                        using (var command = new SqlCommand("DELETE FROM dbo.TblChangedComponentRegister WHERE ChangedComponentid = @HeaderId;", connection, transaction))
                        {
                            command.Parameters.Add("@HeaderId", SqlDbType.Int).Value = current.HeaderId.GetValueOrDefault();
                            command.ExecuteNonQuery();
                        }
                    }

                    transaction.Commit();
                    return new LegacyHrFinanceSaveResult { Success = true, Id = detailId, Message = "تم حذف المفردة المتغيرة بنجاح." };
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
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

        private LegacyHrFinancePageViewModel LoadAdvances(SqlConnection connection, string searchText, int page, int pageSize, string employeeStatus, int? employeeId, DateTime? dateFrom, DateTime? dateTo, string advanceStatus, int? branchId, int? departmentId)
        {
            advanceStatus = NormalizeAdvanceStatus(advanceStatus);
            var model = Base("advances", "السلف", "شؤون الموظفين", "طلب سلفة موظف", "TblEmpAdvanceRequest / TblEmpAdvanceRequestDetails", null, searchText, page, pageSize, employeeStatus);
            model.EmployeeId = employeeId;
            model.BranchId = branchId;
            model.DepartmentId = departmentId;
            model.DateFrom = FormatDate(dateFrom);
            model.DateTo = FormatDate(dateTo);
            model.AdvanceStatus = advanceStatus;
            model.Branches = LoadBranches(connection);
            model.Departments = LoadDepartments(connection);
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
        CASE WHEN actual.ActualAdvanceId IS NULL THEN ISNULL(px.PartsCount,0) ELSE ISNULL(actualpx.PartsCount,0) END AS PartsCount,
        CASE WHEN actual.ActualAdvanceId IS NULL THEN ISNULL(px.PaidPartsCount,0) ELSE ISNULL(actualpx.PaidPartsCount,0) END AS PaidPartsCount,
        CASE WHEN actual.ActualAdvanceId IS NULL THEN ISNULL(px.PaidAmount,0) ELSE ISNULL(actualpx.PaidAmount,0) END AS PaidAmount,
        ISNULL(a.AdvanceValue,0) - CASE WHEN actual.ActualAdvanceId IS NULL THEN ISNULL(px.PaidAmount,0) ELSE ISNULL(actualpx.PaidAmount,0) END AS RemainingAmount,
        actual.ActualAdvanceId
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
 LEFT JOIN (
     SELECT CONVERT(INT, orderNO) AS RequestId, MIN(AdvanceID) AS ActualAdvanceId
     FROM dbo.TblEmpAdvance WITH (NOLOCK)
     WHERE ISNULL(orderNO,0) <> 0
     GROUP BY CONVERT(INT, orderNO)
 ) actual ON actual.RequestId = a.AdvanceID
 LEFT JOIN (
     SELECT AdvanceID,
            COUNT(1) AS PartsCount,
            SUM(CASE WHEN ISNULL(Payed,0)=1 OR Payed1 IS NOT NULL OR EmpAdPaID IS NOT NULL THEN 1 ELSE 0 END) AS PaidPartsCount,
            SUM(CASE WHEN ISNULL(Payed,0)=1 OR Payed1 IS NOT NULL OR EmpAdPaID IS NOT NULL THEN ISNULL(PartValue,0) ELSE 0 END) AS PaidAmount
     FROM dbo.TblEmpAdvanceDetails WITH (NOLOCK)
     GROUP BY AdvanceID
 ) actualpx ON actualpx.AdvanceID = actual.ActualAdvanceId
 WHERE " + EmployeeStatusPredicate("e") + @"
   AND (@EmployeeId IS NULL OR CONVERT(INT, a.Emp_id) = @EmployeeId)
   AND (@BranchId IS NULL OR CONVERT(INT, a.Branch_NO) = @BranchId)
   AND (@DepartmentId IS NULL OR CONVERT(INT, a.DeparmentID) = @DepartmentId)
   AND (@DateFrom IS NULL OR a.AdvanceDate >= @DateFrom)
   AND (@DateTo IS NULL OR a.AdvanceDate < DATEADD(DAY, 1, @DateTo))
   AND (
        @AdvanceStatus = N'all'
        OR (@AdvanceStatus = N'draft' AND ISNULL(a.Approved,0)=0 AND a.Posted IS NULL AND ISNULL(a.AccAproved,0)=0 AND ISNULL(a.notok,0)=0)
        OR (@AdvanceStatus = N'pending' AND ISNULL(a.Approved,0)=0 AND a.Posted IS NOT NULL AND ISNULL(a.AccAproved,0)=0 AND ISNULL(a.notok,0)=0)
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
                command.Parameters.Add("@BranchId", SqlDbType.Int).Value = branchId.HasValue ? (object)branchId.Value : DBNull.Value;
                command.Parameters.Add("@DepartmentId", SqlDbType.Int).Value = departmentId.HasValue ? (object)departmentId.Value : DBNull.Value;
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
            AddAdvanceMetrics(connection, model, employeeStatus, employeeId, dateFrom, dateTo, advanceStatus, searchText, branchId, departmentId);
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

        private LegacyHrFinancePageViewModel LoadChangedComponents(SqlConnection connection, string searchText, int page, int pageSize, string employeeStatus, int? employeeId, DateTime? dateFrom, DateTime? dateTo, string status, int? componentId, int? branchId, int? departmentId, int? yearFilter, int? monthFilter, string componentType)
        {
            var model = Base("changed-components", "تسجيل المفردات المتغيرة", "شؤون الموظفين", "FrmChangedComponentData", "TblChangedComponentRegister / TblChangedComponentRegisterDetails", null, searchText, page, pageSize, employeeStatus);
            model.Employees = LoadEmployees(connection, null, searchText, employeeStatus, employeeId, 80);
            model.Components = LoadVariableComponents(connection);
            model.Branches = LoadBranches(connection);
            model.Departments = LoadDepartments(connection);
            model.EmployeeId = employeeId;
            model.ComponentId = componentId;
            model.BranchId = branchId;
            model.DepartmentId = departmentId;
            model.YearFilter = yearFilter;
            model.MonthFilter = monthFilter;
            model.ComponentType = NormalizeChangedComponentType(componentType);
            model.StatusFilter = NormalizeChangedComponentStatus(status);
            model.DateFrom = dateFrom.HasValue ? dateFrom.Value.ToString("yyyy-MM-dd") : string.Empty;
            model.DateTo = dateTo.HasValue ? dateTo.Value.ToString("yyyy-MM-dd") : string.Empty;
            model.AdvanceStatus = model.StatusFilter;
            var payrollUsageSql = BuildChangedComponentPayrollUsageSql(connection, null);
            var payrollRunSql = BuildChangedComponentPayrollRunSql(connection, null);

            using (var command = new SqlCommand(@"
SELECT * FROM (
 SELECT ROW_NUMBER() OVER (ORDER BY r.Actualyear DESC, r.Actualmonth DESC, r.ChangedComponentid DESC, d.id DESC) RowNo,
        d.id AS DetailId, r.ChangedComponentid, d.Emp_id, e.Emp_Code, e.Fullcode, e.Emp_Name,
        e.BranchId AS EmployeeBranchId, b.branch_name AS BranchName, e.DepartmentID, dep.DepartmentName,
        d.projectid, p.Project_name, r.ComponentID, m.name AS ComponentName, CASE WHEN ISNULL(m.AddOrDiscount,0)=0 THEN 1 ELSE 0 END AS AddOrDiscount,
        m.Unit, r.RecordDate, r.Actualyear, r.Actualmonth, d.[value], d.NoofDays, d.NoOfHour, d.NoOfMinutes,
        d.HourRate, d.Salary, d.Remarks, detailCounts.DetailCount,
        CASE WHEN payroll.UsedCount > 0 OR runTrace.PayrollRunId IS NOT NULL THEN 1 ELSE 0 END AS PayrollUsed,
        runTrace.PayrollRunId, runTrace.PayrollRunName, runTrace.PayrollRunPosted, runTrace.PayrollUsageSource
 FROM dbo.TblChangedComponentRegister r WITH (NOLOCK)
 INNER JOIN dbo.TblChangedComponentRegisterDetails d WITH (NOLOCK) ON d.ChangedComponentid = r.ChangedComponentid
 LEFT JOIN dbo.TblEmployee e WITH (NOLOCK) ON e.Emp_ID = d.Emp_id
 LEFT JOIN dbo.TblBranchesData b WITH (NOLOCK) ON b.branch_id = ISNULL(NULLIF(r.BranchId,0), e.BranchId)
 LEFT JOIN dbo.TblEmpDepartments dep WITH (NOLOCK) ON dep.DeparmentID = e.DepartmentID
 LEFT JOIN dbo.projects p WITH (NOLOCK) ON p.id = d.projectid
 LEFT JOIN dbo.mofrad m WITH (NOLOCK) ON m.id = r.ComponentID
 OUTER APPLY (SELECT COUNT(1) AS DetailCount FROM dbo.TblChangedComponentRegisterDetails x WITH (NOLOCK) WHERE x.ChangedComponentid = r.ChangedComponentid) detailCounts
 OUTER APPLY (" + payrollUsageSql + @") payroll
 OUTER APPLY (" + payrollRunSql + @") runTrace
 WHERE " + EmployeeStatusPredicate("e") + @"
   AND (@EmployeeId IS NULL OR d.Emp_id = @EmployeeId)
   AND (@ComponentId IS NULL OR r.ComponentID = @ComponentId)
   AND (@BranchId IS NULL OR ISNULL(NULLIF(r.BranchId,0), e.BranchId) = @BranchId)
   AND (@DepartmentId IS NULL OR e.DepartmentID = @DepartmentId)
   AND (@DateFrom IS NULL OR r.RecordDate >= @DateFrom)
   AND (@DateTo IS NULL OR r.RecordDate < DATEADD(day, 1, @DateTo))
   AND (@YearFilter IS NULL OR r.Actualyear = @YearFilter)
   AND (@MonthFilter IS NULL OR r.Actualmonth = @MonthFilter)
   AND (@ComponentType = N'all' OR (@ComponentType = N'addition' AND ISNULL(m.AddOrDiscount,0)=0) OR (@ComponentType = N'deduction' AND ISNULL(m.AddOrDiscount,0)=1))
   AND (@Status = N'all' OR (@Status = N'used' AND (payroll.UsedCount > 0 OR runTrace.PayrollRunId IS NOT NULL)) OR (@Status = N'open' AND payroll.UsedCount = 0 AND runTrace.PayrollRunId IS NULL))
   AND (@Search = N'' OR ISNULL(e.Emp_Name,N'') LIKE N'%' + @Search + N'%' OR ISNULL(e.Fullcode,N'') LIKE N'%' + @Search + N'%' OR ISNULL(e.Emp_Code,N'') LIKE N'%' + @Search + N'%' OR ISNULL(m.name,N'') LIKE N'%' + @Search + N'%' OR ISNULL(d.Remarks,N'') LIKE N'%' + @Search + N'%' OR CONVERT(NVARCHAR(30), r.ChangedComponentid) = @Search)
) q WHERE RowNo BETWEEN @Start AND @End ORDER BY RowNo;", connection))
            {
                AddSearch(command, searchText, page, pageSize);
                AddEmployeeStatus(command, employeeStatus);
                AddChangedComponentFilters(command, employeeId, componentId, branchId, departmentId, dateFrom, dateTo, yearFilter, monthFilter, status, componentType, searchText);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var row = MapChangedComponent(reader);
                        model.ChangedComponents.Add(row);
                    }
                }
            }

            using (var command = new SqlCommand(@"
SELECT COUNT(1) AS TotalRows,
       ISNULL(SUM(CASE WHEN ISNULL(m.AddOrDiscount,0)=0 THEN ISNULL(d.[value],0) ELSE 0 END),0) AS Additions,
       ISNULL(SUM(CASE WHEN ISNULL(m.AddOrDiscount,0)=1 THEN ISNULL(d.[value],0) ELSE 0 END),0) AS Deductions,
       SUM(CASE WHEN payroll.UsedCount > 0 OR runTrace.PayrollRunId IS NOT NULL THEN 1 ELSE 0 END) AS UsedRows,
       SUM(CASE WHEN payroll.UsedCount = 0 AND runTrace.PayrollRunId IS NULL THEN 1 ELSE 0 END) AS OpenRows
FROM dbo.TblChangedComponentRegister r WITH (NOLOCK)
INNER JOIN dbo.TblChangedComponentRegisterDetails d WITH (NOLOCK) ON d.ChangedComponentid = r.ChangedComponentid
LEFT JOIN dbo.TblEmployee e WITH (NOLOCK) ON e.Emp_ID = d.Emp_id
LEFT JOIN dbo.mofrad m WITH (NOLOCK) ON m.id = r.ComponentID
OUTER APPLY (" + payrollUsageSql + @") payroll
OUTER APPLY (" + payrollRunSql + @") runTrace
WHERE " + EmployeeStatusPredicate("e") + @"
  AND (@EmployeeId IS NULL OR d.Emp_id = @EmployeeId)
  AND (@ComponentId IS NULL OR r.ComponentID = @ComponentId)
  AND (@BranchId IS NULL OR ISNULL(NULLIF(r.BranchId,0), e.BranchId) = @BranchId)
  AND (@DepartmentId IS NULL OR e.DepartmentID = @DepartmentId)
  AND (@DateFrom IS NULL OR r.RecordDate >= @DateFrom)
  AND (@DateTo IS NULL OR r.RecordDate < DATEADD(day, 1, @DateTo))
  AND (@YearFilter IS NULL OR r.Actualyear = @YearFilter)
  AND (@MonthFilter IS NULL OR r.Actualmonth = @MonthFilter)
  AND (@ComponentType = N'all' OR (@ComponentType = N'addition' AND ISNULL(m.AddOrDiscount,0)=0) OR (@ComponentType = N'deduction' AND ISNULL(m.AddOrDiscount,0)=1))
  AND (@Status = N'all' OR (@Status = N'used' AND (payroll.UsedCount > 0 OR runTrace.PayrollRunId IS NOT NULL)) OR (@Status = N'open' AND payroll.UsedCount = 0 AND runTrace.PayrollRunId IS NULL))
  AND (@Search = N'' OR ISNULL(e.Emp_Name,N'') LIKE N'%' + @Search + N'%' OR ISNULL(e.Fullcode,N'') LIKE N'%' + @Search + N'%' OR ISNULL(e.Emp_Code,N'') LIKE N'%' + @Search + N'%' OR ISNULL(m.name,N'') LIKE N'%' + @Search + N'%' OR ISNULL(d.Remarks,N'') LIKE N'%' + @Search + N'%');", connection))
            {
                AddEmployeeStatus(command, employeeStatus);
                AddChangedComponentFilters(command, employeeId, componentId, branchId, departmentId, dateFrom, dateTo, yearFilter, monthFilter, status, componentType, searchText);
                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        model.Metrics.Add(Metric("عدد السطور", ReadInt(reader, "TotalRows").ToString("N0"), "حسب الفلاتر الحالية"));
                        model.Metrics.Add(Metric("إجمالي الإضافات", ReadDecimal(reader, "Additions").ToString("N2"), "مفردات موجبة"));
                        model.Metrics.Add(Metric("إجمالي الخصومات", ReadDecimal(reader, "Deductions").ToString("N2"), "مفردات سالبة"));
                        model.Metrics.Add(Metric("مستخدم في المسير", ReadInt(reader, "UsedRows").ToString("N0"), "مقفلة عن التعديل والحذف"));
                        model.Metrics.Add(Metric("مفتوح", ReadInt(reader, "OpenRows").ToString("N0"), "قابل للمراجعة حسب الصلاحية"));
                    }
                }
            }

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

        private LegacyHrFinancePageViewModel LoadVacations(SqlConnection connection, string searchText, int page, int pageSize, string employeeStatus, int? employeeId, DateTime? dateFrom, DateTime? dateTo, string vacationStatus, string vacationType, int? branchId, int? departmentId)
        {
            var model = Base("vacations", "الإجازات", "شؤون الموظفين", "الإجازات", "TblVocation", null, searchText, page, pageSize, employeeStatus);
            model.EmployeeId = employeeId;
            model.BranchId = branchId;
            model.DepartmentId = departmentId;
            model.DateFrom = FormatIsoDate(dateFrom);
            model.DateTo = FormatIsoDate(dateTo);
            model.VacationStatus = NormalizeVacationStatus(vacationStatus);
            model.VacationType = (vacationType ?? string.Empty).Trim();
            model.Employees = LoadEmployees(connection, null, searchText, employeeStatus, employeeId, 50);
            model.Branches = LoadBranches(connection);
            model.Departments = LoadDepartments(connection);
            model.VacationTypes = LoadVacationTypes(connection, null);
            using (var command = new SqlCommand(@"
SELECT * FROM (
 SELECT ROW_NUMBER() OVER (ORDER BY v.ID DESC) RowNo,
        v.ID, e.Emp_Name, v.RecordDate, v.FromDate, v.ToDate, v.Reson, v.Approved, v.posted
  FROM dbo.TblVocation v WITH (NOLOCK)
  LEFT JOIN dbo.TblEmployee e WITH (NOLOCK) ON e.Emp_ID = v.EmpID
  WHERE " + EmployeeStatusPredicate("e") + @"
    AND (@EmployeeId IS NULL OR v.EmpID = @EmployeeId)
    AND (@BranchId IS NULL OR v.BranchID = @BranchId)
    AND (@DepartmentId IS NULL OR v.DeptID = @DepartmentId)
    AND (@DateFrom IS NULL OR v.FromDate >= @DateFrom)
    AND (@DateTo IS NULL OR v.FromDate <= @DateTo)
    AND (@VacationType = N'' OR ISNULL(v.VocationType,N'') = @VacationType)
    AND (@VacationStatus = N'all'
         OR (@VacationStatus = N'draft' AND ISNULL(v.posted,0)=0 AND ISNULL(v.ManagerApprove,0)=0 AND ISNULL(v.Approved,0)=0 AND ISNULL(v.notok,0)=0)
         OR (@VacationStatus = N'pending-manager' AND ISNULL(v.posted,0)=1 AND ISNULL(v.ManagerApprove,0)=0 AND ISNULL(v.notok,0)=0)
         OR (@VacationStatus = N'pending-hr' AND ISNULL(v.ManagerApprove,0)=1 AND ISNULL(v.Approved,0)=0 AND ISNULL(v.notok,0)=0)
         OR (@VacationStatus = N'approved' AND ISNULL(v.Approved,0)=1 AND ISNULL(v.notok,0)=0)
         OR (@VacationStatus = N'rejected' AND ISNULL(v.notok,0)=1)
         OR (@VacationStatus = N'paid' AND ISNULL(v.FlagPayed,0)<>0))
    AND (@Search = N'' OR ISNULL(e.Emp_Name,N'') LIKE N'%' + @Search + N'%' OR ISNULL(v.Reson,N'') LIKE N'%' + @Search + N'%' OR CONVERT(NVARCHAR(30), v.ID) = @Search)
) q WHERE RowNo BETWEEN @Start AND @End ORDER BY RowNo;", connection))
            {
                AddSearch(command, searchText, page, pageSize);
                AddEmployeeStatus(command, employeeStatus);
                AddVacationFilters(command, employeeId, dateFrom, dateTo, model.VacationStatus, model.VacationType, branchId, departmentId);
                FillRows(command, model, "ID", "Emp_Name", "Reson", "posted", "FromDate", "ToDate", "Approved", null);
            }
            using (var command = new SqlCommand(@"
SELECT * FROM (
 SELECT ROW_NUMBER() OVER (ORDER BY v.ID DESC) RowNo,
        v.ID, v.EmpID,
        COALESCE(NULLIF(e.Fullcode,N''), NULLIF(e.Emp_Code,N''), CONVERT(NVARCHAR(30), e.Emp_ID)) AS EmployeeCode,
        e.Emp_Name, v.RecordDate, v.FromDate, v.ToDate, v.ResumeWork, v.Reson, v.VocationType,
        v.BranchID, b.branch_name AS BranchName, v.DeptID, d.DepartmentName,
        v.ManagerID, m.Emp_Name AS ManagerName, v.JobID, j.JobTypeName,
        ISNULL(v.WithSalary,0) AS WithSalary, ISNULL(v.WithoutSalary,0) AS WithoutSalary,
        ISNULL(v.ManagerApprove,0) AS ManagerApprove, ISNULL(v.Approved,0) AS Approved,
        ISNULL(v.posted,0) AS posted, ISNULL(v.notok,0) AS notok, ISNULL(v.FlagPayed,0) AS FlagPayed,
        ISNULL(v.NoVacation, CASE WHEN v.FromDate IS NULL OR v.ToDate IS NULL THEN 0 ELSE DATEDIFF(DAY, v.FromDate, v.ToDate) + 1 END) AS NoVacation,
        ent.EntitlementId,
        ent.AcuDate,
        ent.NoDayAct,
        ent.NoDayDelay,
        emb.EmbarkationId,
        CASE WHEN ent.EntitlementId IS NULL THEN 0 ELSE 1 END AS LinkedToEntitlement
 FROM dbo.TblVocation v WITH (NOLOCK)
 LEFT JOIN dbo.TblEmployee e WITH (NOLOCK) ON e.Emp_ID = v.EmpID
 LEFT JOIN dbo.TblEmployee m WITH (NOLOCK) ON m.Emp_ID = v.ManagerID
 LEFT JOIN dbo.TblBranchesData b WITH (NOLOCK) ON b.branch_id = v.BranchID
 LEFT JOIN dbo.TblEmpDepartments d WITH (NOLOCK) ON d.DeparmentID = v.DeptID
  LEFT JOIN dbo.TblEmpJobsTypes j WITH (NOLOCK) ON j.JobTypeID = v.JobID
  OUTER APPLY (SELECT TOP (1) ve.ID AS EntitlementId, ve.AcuDate, ve.NoDayAct, ve.NoDayDelay FROM dbo.TblVocationEntitlements ve WITH (NOLOCK) WHERE ISNULL(ve.NoOrder,0) = v.ID ORDER BY ve.ID DESC) ent
  OUTER APPLY (SELECT TOP (1) eb.id AS EmbarkationId FROM dbo.TblEmbarkation eb WITH (NOLOCK) WHERE eb.OrderVocation = ent.EntitlementId ORDER BY eb.id DESC) emb
  WHERE " + EmployeeStatusPredicate("e") + @"
    AND (@EmployeeId IS NULL OR v.EmpID = @EmployeeId)
    AND (@BranchId IS NULL OR v.BranchID = @BranchId)
    AND (@DepartmentId IS NULL OR v.DeptID = @DepartmentId)
    AND (@DateFrom IS NULL OR v.FromDate >= @DateFrom)
    AND (@DateTo IS NULL OR v.FromDate <= @DateTo)
    AND (@VacationType = N'' OR ISNULL(v.VocationType,N'') = @VacationType)
    AND (@VacationStatus = N'all'
         OR (@VacationStatus = N'draft' AND ISNULL(v.posted,0)=0 AND ISNULL(v.ManagerApprove,0)=0 AND ISNULL(v.Approved,0)=0 AND ISNULL(v.notok,0)=0)
         OR (@VacationStatus = N'pending-manager' AND ISNULL(v.posted,0)=1 AND ISNULL(v.ManagerApprove,0)=0 AND ISNULL(v.notok,0)=0)
         OR (@VacationStatus = N'pending-hr' AND ISNULL(v.ManagerApprove,0)=1 AND ISNULL(v.Approved,0)=0 AND ISNULL(v.notok,0)=0)
         OR (@VacationStatus = N'approved' AND ISNULL(v.Approved,0)=1 AND ISNULL(v.notok,0)=0)
         OR (@VacationStatus = N'rejected' AND ISNULL(v.notok,0)=1)
         OR (@VacationStatus = N'paid' AND ISNULL(v.FlagPayed,0)<>0))
    AND (@Search = N'' OR ISNULL(e.Emp_Name,N'') LIKE N'%' + @Search + N'%' OR ISNULL(v.Reson,N'') LIKE N'%' + @Search + N'%' OR CONVERT(NVARCHAR(30), v.ID) = @Search)
) q WHERE RowNo BETWEEN @Start AND @End ORDER BY RowNo;", connection))
            {
                AddSearch(command, searchText, page, pageSize);
                AddEmployeeStatus(command, employeeStatus);
                AddVacationFilters(command, employeeId, dateFrom, dateTo, model.VacationStatus, model.VacationType, branchId, departmentId);
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var vacation = MapVacation(reader);
                        ApplyVacationLockState(vacation);
                        model.Vacations.Add(vacation);
                    }
                }
            }
            AddCountMetric(connection, model, "TblVocation");
            model.Metrics.Add(Metric("قيد المراجعة", model.Vacations.Count(x => x.Submitted && !x.ManagerApproved && !x.Rejected).ToString("N0"), "طلبات مرسلة ولم تعتمد من المدير"));
            model.Metrics.Add(Metric("معتمدة", model.Vacations.Count(x => x.HrApproved && !x.Rejected).ToString("N0"), "اعتماد الموارد البشرية"));
            model.Metrics.Add(Metric("مقفلة", model.Vacations.Count(x => !x.CanEdit).ToString("N0"), "مرتبطة أو معتمدة أو مرفوضة"));
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
                Submitted = ReadNullableInt(reader, "Posted").GetValueOrDefault() != 0,
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
                RemainingAmount = ReadDecimal(reader, "RemainingAmount"),
                ActualAdvanceId = ReadNullableInt(reader, "ActualAdvanceId")
            };

            if (advance.RemainingAmount < 0) { advance.RemainingAmount = 0; }
            advance.MonthlyDueAmount = advance.PaymentCounts > 0 ? Math.Round(advance.AdvanceValue / advance.PaymentCounts, 2) : 0;
            advance.InstallmentsValid = true;
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
        CASE WHEN actual.ActualAdvanceId IS NULL THEN ISNULL(px.PartsCount,0) ELSE ISNULL(actualpx.PartsCount,0) END AS PartsCount,
        CASE WHEN actual.ActualAdvanceId IS NULL THEN ISNULL(px.PaidPartsCount,0) ELSE ISNULL(actualpx.PaidPartsCount,0) END AS PaidPartsCount,
        CASE WHEN actual.ActualAdvanceId IS NULL THEN ISNULL(px.PaidAmount,0) ELSE ISNULL(actualpx.PaidAmount,0) END AS PaidAmount,
        ISNULL(a.AdvanceValue,0) - CASE WHEN actual.ActualAdvanceId IS NULL THEN ISNULL(px.PaidAmount,0) ELSE ISNULL(actualpx.PaidAmount,0) END AS RemainingAmount,
        actual.ActualAdvanceId
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
 LEFT JOIN (
     SELECT CONVERT(INT, orderNO) AS RequestId, MIN(AdvanceID) AS ActualAdvanceId
     FROM dbo.TblEmpAdvance WITH (NOLOCK)
     WHERE ISNULL(orderNO,0) <> 0
     GROUP BY CONVERT(INT, orderNO)
 ) actual ON actual.RequestId = a.AdvanceID
 LEFT JOIN (
     SELECT AdvanceID,
            COUNT(1) AS PartsCount,
            SUM(CASE WHEN ISNULL(Payed,0)=1 OR Payed1 IS NOT NULL OR EmpAdPaID IS NOT NULL THEN 1 ELSE 0 END) AS PaidPartsCount,
            SUM(CASE WHEN ISNULL(Payed,0)=1 OR Payed1 IS NOT NULL OR EmpAdPaID IS NOT NULL THEN ISNULL(PartValue,0) ELSE 0 END) AS PaidAmount
     FROM dbo.TblEmpAdvanceDetails WITH (NOLOCK)
     GROUP BY AdvanceID
 ) actualpx ON actualpx.AdvanceID = actual.ActualAdvanceId
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
            advance.Parts.Clear();
            var sourceAdvanceId = advance.ActualAdvanceId.GetValueOrDefault(advance.Id.GetValueOrDefault());
            var tableName = advance.ActualAdvanceId.HasValue ? "dbo.TblEmpAdvanceDetails" : "dbo.TblEmpAdvanceRequestDetails";
            var tableIdColumn = advance.ActualAdvanceId.HasValue ? "TableID" : "id";
            var payrollJoin = advance.ActualAdvanceId.HasValue && TableExists(connection, transaction, "PayrollRunAdvanceDeductions")
                ? @"
OUTER APPLY (
    SELECT TOP (1)
           p.PayrollRunAdvanceDeductionId,
           p.PayrollRunId,
           p.IsPosted,
           p.PostedAt,
           p.NoteId
    FROM dbo.PayrollRunAdvanceDeductions p WITH (NOLOCK)
    WHERE p.AdvanceId = d.AdvanceID
      AND (
           p.AdvanceDetailTableId = d.TableID
           OR (p.AdvanceDetailTableId IS NULL AND p.PartNo = d.PartNO)
      )
    ORDER BY p.IsPosted DESC, p.PayrollRunAdvanceDeductionId DESC
) payroll"
                : @"
OUTER APPLY (
    SELECT CAST(NULL AS INT) AS PayrollRunAdvanceDeductionId,
           CAST(NULL AS INT) AS PayrollRunId,
           CAST(NULL AS BIT) AS IsPosted,
           CAST(NULL AS DATETIME) AS PostedAt,
           CAST(NULL AS INT) AS NoteId
) payroll";

            using (var command = new SqlCommand(@"
SELECT d." + tableIdColumn + @" AS TableId,
       d.PartNo,
       d.PartValue,
       d.PartDate,
       d.Payed,
       d.Payed1,
       d.EmpAdPaID,
       d.Remark,
       payroll.PayrollRunAdvanceDeductionId,
       payroll.PayrollRunId,
       payroll.IsPosted,
       payroll.PostedAt,
       payroll.NoteId
FROM " + tableName + @" d WITH (NOLOCK)
" + payrollJoin + @"
WHERE d.AdvanceID = @AdvanceID
ORDER BY d.PartNo;", connection, transaction))
            {
                command.Parameters.Add("@AdvanceID", SqlDbType.Int).Value = sourceAdvanceId;
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var payrollDeductionId = ReadNullableInt(reader, "PayrollRunAdvanceDeductionId");
                        var payrollPosted = ReadBool(reader, "IsPosted");
                        var paid = ReadNullableInt(reader, "Payed").HasValue
                            || ReadNullableInt(reader, "Payed1").HasValue
                            || ReadNullableInt(reader, "EmpAdPaID").HasValue
                            || payrollPosted;
                        var partValue = ReadDecimal(reader, "PartValue");
                        advance.Parts.Add(new EmployeeAdvancePartViewModel
                        {
                            TableId = ReadNullableInt(reader, "TableId"),
                            PartNo = ReadInt(reader, "PartNo"),
                            PartValue = partValue,
                            PartDate = ReadDisplayDate(reader, "PartDate"),
                            Payed = paid,
                            PaidDate = ReadDisplayDateTime(reader, "PostedAt"),
                            RemainingValue = paid ? 0 : partValue,
                            PayrollDeductionId = payrollDeductionId,
                            PayrollRunId = ReadNullableInt(reader, "PayrollRunId"),
                            PayrollLinked = payrollDeductionId.HasValue,
                            PayrollPosted = payrollPosted,
                            PayrollPostedAt = ReadDisplayDateTime(reader, "PostedAt"),
                            Locked = paid || payrollDeductionId.HasValue,
                            StatusText = payrollPosted ? "مرحل في المسير" : payrollDeductionId.HasValue ? "مرتبط بمسير" : paid ? "مسدد" : "مفتوح",
                            Remark = ReadString(reader, "Remark")
                        });
                    }
                }
            }

            ValidateAdvanceInstallments(advance);
        }

        private static void ValidateAdvanceInstallments(EmployeeAdvanceViewModel advance)
        {
            advance.InstallmentsTotal = Math.Round(advance.Parts.Sum(x => x.PartValue), 2);
            advance.MonthlyDueAmount = advance.Parts.Count > 0
                ? Math.Round(advance.Parts.Where(x => !x.Payed).DefaultIfEmpty().Sum(x => x == null ? 0 : x.PartValue) / Math.Max(1, advance.Parts.Count(x => !x.Payed)), 2)
                : (advance.PaymentCounts > 0 ? Math.Round(advance.AdvanceValue / advance.PaymentCounts, 2) : 0);
            advance.PaidAmount = Math.Round(advance.Parts.Where(x => x.Payed).Sum(x => x.PartValue), 2);
            advance.RemainingAmount = Math.Max(0, Math.Round(advance.AdvanceValue - advance.PaidAmount, 2));

            var errors = new List<string>();
            if (advance.Parts.GroupBy(x => x.PartNo).Any(g => g.Key > 0 && g.Count() > 1))
            {
                errors.Add("يوجد تكرار في أرقام الأقساط.");
            }

            if (advance.Parts.Any(x => x.PartValue < 0))
            {
                errors.Add("يوجد قسط بقيمة سالبة.");
            }

            if (Math.Abs(advance.InstallmentsTotal - advance.AdvanceValue) > 0.05m)
            {
                errors.Add("إجمالي الأقساط لا يساوي قيمة السلفة.");
            }

            if (advance.AdvanceValue - advance.PaidAmount < -0.05m)
            {
                errors.Add("قيمة الأقساط المسددة أكبر من قيمة السلفة.");
            }

            advance.InstallmentsValid = errors.Count == 0;
            advance.InstallmentValidationMessage = errors.Count == 0 ? "الأقساط متوازنة مع قيمة السلفة." : string.Join(" ", errors);
        }

        private static int? GetActualAdvanceIdForRequest(SqlConnection connection, SqlTransaction transaction, int requestId)
        {
            using (var command = new SqlCommand(@"
SELECT TOP (1) AdvanceID
FROM dbo.TblEmpAdvance WITH (UPDLOCK, HOLDLOCK)
WHERE CONVERT(INT, ISNULL(orderNO,0)) = @RequestId
ORDER BY AdvanceID;", connection, transaction))
            {
                command.Parameters.Add("@RequestId", SqlDbType.Int).Value = requestId;
                var value = command.ExecuteScalar();
                return value == null || value == DBNull.Value ? (int?)null : Convert.ToInt32(value);
            }
        }

        private static string GetAdvanceCancellationBlockReason(SqlConnection connection, SqlTransaction transaction, int requestId, EmployeeAdvanceViewModel request)
        {
            if (request == null) { return "لم يتم العثور على طلب السلفة."; }
            if (request.AccountingApproved) { return "لا يمكن إلغاء طلب سلفة مرتبط باعتماد محاسبي."; }

            var actualAdvanceId = GetActualAdvanceIdForRequest(connection, transaction, requestId);
            if (!actualAdvanceId.HasValue)
            {
                return string.Empty;
            }

            if (TableExists(connection, transaction, "PayrollRunAdvanceDeductions"))
            {
                using (var command = new SqlCommand(@"
SELECT COUNT(1) AS LinkCount,
       SUM(CASE WHEN ISNULL(l.IsPosted, 0) = 1 THEN 1 ELSE 0 END) AS PostedLinkCount,
       MAX(l.PayrollRunId) AS LastPayrollRunId,
       MAX(l.NoteId) AS LastNoteId
FROM dbo.PayrollRunAdvanceDeductions l WITH (NOLOCK)
WHERE l.AdvanceId = @ActualAdvanceId;", connection, transaction))
                {
                    command.Parameters.Add("@ActualAdvanceId", SqlDbType.Int).Value = actualAdvanceId.Value;
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            var postedLinks = ReadInt(reader, "PostedLinkCount");
                            var linkCount = ReadInt(reader, "LinkCount");
                            var payrollRunId = ReadNullableInt(reader, "LastPayrollRunId");
                            var noteId = ReadNullableInt(reader, "LastNoteId");
                            if (postedLinks > 0)
                            {
                                return "لا يمكن إلغاء طلب السلفة لأن له أقساطاً تم ترحيلها في مسير الرواتب"
                                    + (payrollRunId.HasValue ? " رقم " + payrollRunId.Value : string.Empty)
                                    + (noteId.HasValue ? " وقيد رقم " + noteId.Value : string.Empty)
                                    + ". يجب عكس/إلغاء مسير الرواتب من شاشة المسير أولاً.";
                            }

                            if (linkCount > 0)
                            {
                                return "لا يمكن إلغاء طلب السلفة لأنه مرتبط بمسير رواتب غير مرحل. احذف أو أعد احتساب المسير المرتبط أولاً.";
                            }
                        }
                    }
                }
            }

            if (TableExists(connection, transaction, "TblEmpAdvanceDetails"))
            {
                using (var command = new SqlCommand(@"
SELECT COUNT(1) AS PaidParts
FROM dbo.TblEmpAdvanceDetails WITH (NOLOCK)
WHERE AdvanceID = @ActualAdvanceId
  AND (ISNULL(Payed, 0) = 1 OR Payed1 IS NOT NULL OR EmpAdPaID IS NOT NULL);", connection, transaction))
                {
                    command.Parameters.Add("@ActualAdvanceId", SqlDbType.Int).Value = actualAdvanceId.Value;
                    var paidParts = Convert.ToInt32(command.ExecuteScalar());
                    if (paidParts > 0)
                    {
                        return "لا يمكن إلغاء طلب السلفة لأن السلفة الفعلية رقم " + actualAdvanceId.Value + " تحتوي على أقساط مسددة أو مرتبطة بتسوية.";
                    }
                }
            }

            return "لا يمكن إلغاء طلب السلفة لأنه تم صرفه كسلفة فعلية رقم " + actualAdvanceId.Value + ". يجب معالجة السلفة الفعلية من مسار السلف/التسويات وليس من طلب السلفة.";
        }

        private static void LockAdvanceRequestForDisbursement(SqlConnection connection, SqlTransaction transaction, int requestId)
        {
            using (var command = new SqlCommand(@"
SELECT AdvanceID
FROM dbo.TblEmpAdvanceRequest WITH (UPDLOCK, HOLDLOCK)
WHERE AdvanceID = @RequestId;", connection, transaction))
            {
                command.Parameters.Add("@RequestId", SqlDbType.Int).Value = requestId;
                command.ExecuteScalar();
            }
        }

        private static AdvanceAccountingBoundaryViewModel BuildAdvanceAccountingBoundary(SqlConnection connection, SqlTransaction transaction, int requestId)
        {
            var model = new AdvanceAccountingBoundaryViewModel
            {
                RequestId = requestId,
                CreatesJournalOnDisbursement = false,
                CreatesPaymentVoucherOnDisbursement = false,
                CanCreateFinancePaymentVoucher = false,
                BoundaryStatus = "HR_ACTUAL_ADVANCE_ONLY",
                BoundaryMessage = "حدود المحاسبة المؤكدة: صرف طلب السلفة في الويب ينشئ السلفة الفعلية وأقساطها فقط، ولا ينشئ سند صرف أو قيد محاسبي مباشر. يظهر الأثر المحاسبي المؤكد عند خصم الأقساط داخل مسير الرواتب المرحل.",
                UnsupportedReason = "في FrmEmpsAdvance.frm الأصلي، كود إنشاء Notes/DOUBLE_ENTREY_VOUCHERS لصرف السلفة نفسه موجود كتعليق وليس مساراً تنفيذياً مؤكداً. لذلك لا يتم توليد سند صرف أو قيد تخميني من شاشة السلف."
            };

            if (requestId <= 0)
            {
                model.BoundaryStatus = "INVALID_REQUEST";
                model.BoundaryMessage = "رقم طلب السلفة غير صحيح.";
                return model;
            }

            if (!TableExists(connection, transaction, "TblEmpAdvance"))
            {
                model.BoundaryStatus = "MISSING_ADVANCE_TABLE";
                model.BoundaryMessage = "جدول السلف الفعلية غير موجود، لا يمكن فحص الحدود المحاسبية.";
                model.HasUnsupportedAccountingTrace = true;
                return model;
            }

            using (var command = new SqlCommand(@"
SELECT TOP (1)
       AdvanceID,
       NoteID,
       NoteSerial,
       NoteSerial1,
       opening_balance_voucher_id
FROM dbo.TblEmpAdvance WITH (NOLOCK)
WHERE CONVERT(INT, ISNULL(orderNO,0)) = @RequestId
ORDER BY AdvanceID;", connection, transaction))
            {
                command.Parameters.Add("@RequestId", SqlDbType.Int).Value = requestId;
                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        model.ActualAdvanceId = ReadNullableInt(reader, "AdvanceID");
                        model.HasActualAdvance = model.ActualAdvanceId.HasValue;
                        model.NoteId = ReadNullableInt(reader, "NoteID");
                        model.NoteSerial = ReadString(reader, "NoteSerial");
                        model.VoucherSerial = ReadString(reader, "NoteSerial1");
                        var openingVoucherId = ReadNullableInt(reader, "opening_balance_voucher_id");
                        reader.Close();

                        if (model.ActualAdvanceId.HasValue && TableExists(connection, transaction, "DOUBLE_ENTREY_VOUCHERS"))
                        {
                            model.NormalJournalLineCount = Scalar(connection, transaction, @"
SELECT COUNT(1)
FROM dbo.DOUBLE_ENTREY_VOUCHERS WITH (NOLOCK)
WHERE Notes_ID = @NoteId OR AdvanceID = @ActualAdvanceId;",
                                new SqlParameter("@NoteId", model.NoteId.HasValue ? (object)model.NoteId.Value : DBNull.Value),
                                new SqlParameter("@ActualAdvanceId", model.ActualAdvanceId.GetValueOrDefault()));

                            using (var journal = new SqlCommand(@"
SELECT TOP (20)
       d.Notes_ID,
       d.DEV_Serial,
       a.Account_Serial,
       a.Account_Name,
       CASE WHEN ISNULL(d.Credit_Or_Debit, 0) = 0 THEN ISNULL(d.Value, ISNULL(d.depet_value, 0)) ELSE ISNULL(d.depet_value, 0) END AS DebitValue,
       CASE WHEN ISNULL(d.Credit_Or_Debit, 0) = 1 THEN ISNULL(d.Value, ISNULL(d.credit_value, 0)) ELSE ISNULL(d.credit_value, 0) END AS CreditValue,
       d.Double_Entry_Vouchers_Description
FROM dbo.DOUBLE_ENTREY_VOUCHERS d WITH (NOLOCK)
LEFT JOIN dbo.ACCOUNTS a WITH (NOLOCK) ON a.Account_Code = d.Account_Code
WHERE d.Notes_ID = @NoteId OR d.AdvanceID = @ActualAdvanceId
ORDER BY d.Double_Entry_Vouchers_ID;", connection, transaction))
                            {
                                journal.Parameters.Add("@NoteId", SqlDbType.Int).Value = model.NoteId.HasValue ? (object)model.NoteId.Value : DBNull.Value;
                                journal.Parameters.Add("@ActualAdvanceId", SqlDbType.Int).Value = model.ActualAdvanceId.GetValueOrDefault();
                                using (var traceReader = journal.ExecuteReader())
                                {
                                    while (traceReader.Read())
                                    {
                                        model.DirectJournalTraces.Add(new AdvanceJournalTraceViewModel
                                        {
                                            NoteId = ReadNullableInt(traceReader, "Notes_ID"),
                                            NoteSerial = ReadString(traceReader, "DEV_Serial"),
                                            AccountSerial = ReadString(traceReader, "Account_Serial"),
                                            AccountName = ReadString(traceReader, "Account_Name"),
                                            Debit = ReadDecimal(traceReader, "DebitValue"),
                                            Credit = ReadDecimal(traceReader, "CreditValue"),
                                            Description = ReadString(traceReader, "Double_Entry_Vouchers_Description")
                                        });
                                    }
                                }
                            }
                        }

                        if (openingVoucherId.HasValue && TableExists(connection, transaction, "DOUBLE_ENTREY_VOUCHERS1"))
                        {
                            model.OpeningJournalLineCount = Scalar(connection, transaction, @"
SELECT COUNT(1)
FROM dbo.DOUBLE_ENTREY_VOUCHERS1 WITH (NOLOCK)
WHERE opening_balance_voucher_id = @OpeningVoucherId;",
                                new SqlParameter("@OpeningVoucherId", openingVoucherId.Value));

                            using (var opening = new SqlCommand(@"
SELECT TOP (20)
       d.Notes_ID,
       d.DEV_Serial,
       a.Account_Serial,
       a.Account_Name,
       CASE WHEN ISNULL(d.Credit_Or_Debit, 0) = 0 THEN ISNULL(d.Value, ISNULL(d.depet_value, 0)) ELSE ISNULL(d.depet_value, 0) END AS DebitValue,
       CASE WHEN ISNULL(d.Credit_Or_Debit, 0) = 1 THEN ISNULL(d.Value, ISNULL(d.credit_value, 0)) ELSE ISNULL(d.credit_value, 0) END AS CreditValue,
       d.Double_Entry_Vouchers_Description
FROM dbo.DOUBLE_ENTREY_VOUCHERS1 d WITH (NOLOCK)
LEFT JOIN dbo.ACCOUNTS a WITH (NOLOCK) ON a.Account_Code = d.Account_Code
WHERE d.opening_balance_voucher_id = @OpeningVoucherId
ORDER BY d.Double_Entry_Vouchers_ID;", connection, transaction))
                            {
                                opening.Parameters.Add("@OpeningVoucherId", SqlDbType.Int).Value = openingVoucherId.Value;
                                using (var traceReader = opening.ExecuteReader())
                                {
                                    while (traceReader.Read())
                                    {
                                        model.OpeningBalanceTraces.Add(new AdvanceJournalTraceViewModel
                                        {
                                            NoteId = ReadNullableInt(traceReader, "Notes_ID"),
                                            NoteSerial = ReadString(traceReader, "DEV_Serial"),
                                            AccountSerial = ReadString(traceReader, "Account_Serial"),
                                            AccountName = ReadString(traceReader, "Account_Name"),
                                            Debit = ReadDecimal(traceReader, "DebitValue"),
                                            Credit = ReadDecimal(traceReader, "CreditValue"),
                                            Description = ReadString(traceReader, "Double_Entry_Vouchers_Description")
                                        });
                                    }
                                }
                            }
                        }
                    }
                }
            }

            if (TableExists(connection, transaction, "PayrollRunAdvanceDeductions"))
            {
                var payrollWhere = ColumnExists(connection, transaction, "PayrollRunAdvanceDeductions", "RequestAdvanceId")
                    ? "WHERE RequestAdvanceId = @RequestId"
                    : "WHERE AdvanceId = @ActualAdvanceId";
                using (var command = new SqlCommand(@"
SELECT COUNT(1) AS LineCount,
       SUM(CASE WHEN IsPosted = 1 THEN 1 ELSE 0 END) AS PostedLineCount,
       ISNULL(SUM(PartValue),0) AS TotalValue
FROM dbo.PayrollRunAdvanceDeductions WITH (NOLOCK)
" + payrollWhere + @";", connection, transaction))
                {
                    command.Parameters.Add("@RequestId", SqlDbType.Int).Value = requestId;
                    command.Parameters.Add("@ActualAdvanceId", SqlDbType.Int).Value = model.ActualAdvanceId.HasValue ? (object)model.ActualAdvanceId.Value : -1;
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            model.PayrollDeductionLineCount = ReadInt(reader, "LineCount");
                            model.PostedPayrollDeductionLineCount = ReadInt(reader, "PostedLineCount");
                            model.PayrollDeductionTotal = ReadDecimal(reader, "TotalValue");
                            model.HasPayrollDeduction = model.PayrollDeductionLineCount > 0;
                        }
                    }
                }

                using (var command = new SqlCommand(@"
SELECT TOP (50)
       l.PayrollRunId,
       h.RunName,
       h.PeriodYear,
       h.PeriodMonth,
       l.PartNo,
       l.PartDate,
       l.PartValue,
       l.IsPosted,
       l.PostedAt,
       l.NoteId
FROM dbo.PayrollRunAdvanceDeductions l WITH (NOLOCK)
LEFT JOIN dbo.PayrollRunHeader h WITH (NOLOCK) ON h.PayrollRunId = l.PayrollRunId
" + payrollWhere.Replace("RequestAdvanceId", "l.RequestAdvanceId").Replace("AdvanceId", "l.AdvanceId") + @"
ORDER BY l.IsPosted DESC, h.PeriodYear DESC, h.PeriodMonth DESC, l.PartDate, l.PayrollRunAdvanceDeductionId;", connection, transaction))
                {
                    command.Parameters.Add("@RequestId", SqlDbType.Int).Value = requestId;
                    command.Parameters.Add("@ActualAdvanceId", SqlDbType.Int).Value = model.ActualAdvanceId.HasValue ? (object)model.ActualAdvanceId.Value : -1;
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var posted = ReadBool(reader, "IsPosted");
                            model.PayrollDeductionLinks.Add(new AdvancePayrollDeductionTraceViewModel
                            {
                                PayrollRunId = ReadInt(reader, "PayrollRunId"),
                                RunName = ReadString(reader, "RunName"),
                                PeriodYear = ReadNullableInt(reader, "PeriodYear"),
                                PeriodMonth = ReadNullableInt(reader, "PeriodMonth"),
                                PartNo = ReadInt(reader, "PartNo"),
                                PartDate = ReadDisplayDate(reader, "PartDate"),
                                PartValue = ReadDecimal(reader, "PartValue"),
                                IsPosted = posted,
                                PostedAt = ReadDisplayDateTime(reader, "PostedAt"),
                                NoteId = ReadNullableInt(reader, "NoteId"),
                                StatusText = posted ? "مرحل في المسير" : "محفوظ في مسير غير مرحل"
                            });
                        }
                    }
                }
            }

            model.HasAnyAccountingTrace = model.NormalJournalLineCount > 0 || model.OpeningJournalLineCount > 0 || model.PostedPayrollDeductionLineCount > 0;
            if (model.NormalJournalLineCount > 0)
            {
                model.HasUnsupportedAccountingTrace = true;
                model.BoundaryStatus = "UNEXPECTED_DIRECT_JOURNAL_TRACE";
                model.BoundaryMessage = "تم العثور على قيود مباشرة مرتبطة بالسلفة الفعلية. لن يقوم مسار الويب بتوليد قيد آخر، ويجب مراجعة هذه القيود من شاشة القيود/السندات قبل أي عكس أو إعادة ترحيل.";
            }
            else if (model.OpeningJournalLineCount > 0)
            {
                model.BoundaryStatus = "OPENING_BALANCE_TRACE";
                model.BoundaryMessage = "يوجد أثر قيد افتتاحي فقط مرتبط بالسلفة الفعلية. هذا ليس سند صرف للسلفة، ولا يغير قاعدة عدم إنشاء قيد صرف مباشر من شاشة السلف.";
            }
            else if (model.HasPayrollDeduction)
            {
                model.BoundaryStatus = "PAYROLL_DEDUCTION_TRACE";
                model.BoundaryMessage = "تم ربط أقساط السلفة بمسير الرواتب. الأثر المحاسبي يتم من قيد مسير الرواتب، وليس من شاشة السلف.";
            }

            return model;
        }

        private static void LoadAdvanceApprovalHistory(SqlConnection connection, SqlTransaction transaction, EmployeeAdvanceViewModel advance)
        {
            advance.ApprovalHistory.Clear();
            using (var command = new SqlCommand(@"
SELECT a.id, a.ScreenName, a.levelo, a.EmpID, u.UserName, a.ApprovDate, a.CancelApprove, a.Remarks, a.FromUser, a.Currcursor
FROM dbo.ApprovalData a WITH (NOLOCK)
LEFT JOIN dbo.TblUsers u WITH (NOLOCK) ON u.UserID = CONVERT(INT, a.EmpID)
WHERE a.ScreenName = N'FrmEmpsAdvanceRequest'
  AND CONVERT(INT, a.Transaction_ID) = @AdvanceID
ORDER BY a.levelorder, a.id;", connection, transaction))
            {
                command.Parameters.Add("@AdvanceID", SqlDbType.Int).Value = advance.Id.GetValueOrDefault();
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        advance.ApprovalHistory.Add(new EmployeeAdvanceApprovalHistoryViewModel
                        {
                            Id = ReadInt(reader, "id"),
                            ScreenName = ReadString(reader, "ScreenName"),
                            Level = ReadNullableInt(reader, "levelo"),
                            EmployeeId = ReadNullableInt(reader, "EmpID"),
                            EmployeeName = ReadString(reader, "UserName"),
                            ApprovedAt = ReadDisplayDateTime(reader, "ApprovDate"),
                            CancelledAt = ReadDisplayDateTime(reader, "CancelApprove"),
                            Remarks = ReadString(reader, "Remarks"),
                            FromUser = ReadString(reader, "FromUser"),
                            IsCurrentCursor = ReadNullableInt(reader, "Currcursor").GetValueOrDefault() == 1
                        });
                    }
                }
            }
        }

        private static void AddAdvanceApprovalHistory(SqlConnection connection, SqlTransaction transaction, int requestId, int? userId, string userName, string remarks, bool approved)
        {
            using (var command = new SqlCommand(@"
INSERT INTO dbo.ApprovalData
(ScreenName, levelo, EmpID, levelorder, currorder, Transaction_ID, NoteID, Currcursor, ApprovDate, CancelApprove, Remarks, FromUser, SendTime, Transaction_Date)
VALUES
(N'FrmEmpsAdvanceRequest', 1, @UserID, 1, 1, @TransactionID, NULL, NULL, @ApprovDate, @CancelApprove, @Remarks, @FromUser, GETDATE(), GETDATE());", connection, transaction))
            {
                command.Parameters.Add("@UserID", SqlDbType.Float).Value = userId.HasValue ? (object)userId.Value : DBNull.Value;
                command.Parameters.Add("@TransactionID", SqlDbType.Float).Value = requestId;
                command.Parameters.Add("@ApprovDate", SqlDbType.DateTime).Value = approved ? (object)DateTime.Now : DBNull.Value;
                command.Parameters.Add("@CancelApprove", SqlDbType.DateTime).Value = approved ? (object)DBNull.Value : DateTime.Now;
                command.Parameters.Add("@Remarks", SqlDbType.NVarChar, 4000).Value = DbText(string.IsNullOrWhiteSpace(remarks) ? (approved ? "اعتماد طلب السلفة من الويب" : "إلغاء طلب السلفة من الويب") : remarks);
                command.Parameters.Add("@FromUser", SqlDbType.NVarChar, 255).Value = DbText(string.IsNullOrWhiteSpace(userName) ? "Web" : userName);
                command.ExecuteNonQuery();
            }
        }

        private static void SendAdvanceToApproval(SqlConnection connection, SqlTransaction transaction, EnterpriseHrEmployeeLookupViewModel employee, int requestId, int? userId, string userName, string remarks)
        {
            using (var delete = new SqlCommand(@"
DELETE FROM dbo.ApprovalData
WHERE ScreenName = N'FrmEmpsAdvanceRequest'
  AND CONVERT(INT, Transaction_ID) = @RequestID;", connection, transaction))
            {
                delete.Parameters.Add("@RequestID", SqlDbType.Int).Value = requestId;
                delete.ExecuteNonQuery();
            }

            var inserted = 0;
            using (var command = new SqlCommand(@"
INSERT INTO dbo.ApprovalData
(ScreenName, levelo, EmpID, levelorder, currorder, Transaction_ID, NoteID, Currcursor, Remarks, NoteSerial, Transaction_Date, FromUser, SendTime)
SELECT N'FrmEmpsAdvanceRequest',
       d.PlainMessageID,
       COALESCE(NULLIF(w.EmpID,0), NULLIF(w.EmpID1,0)),
       d.id,
       w.id,
       @RequestID,
       NULL,
       CASE WHEN ROW_NUMBER() OVER (ORDER BY d.id, w.id) = 1 THEN 1 ELSE NULL END,
       @Remarks,
       CONVERT(NVARCHAR(50), @RequestID),
       GETDATE(),
       @FromUser,
       GETDATE()
FROM dbo.TblApprovalDef a WITH (NOLOCK)
INNER JOIN dbo.TblApprovalDefDetails d WITH (NOLOCK) ON d.lMessageDefID = a.id
INNER JOIN dbo.TbllevelWorker w WITH (NOLOCK) ON w.LevelID = d.PlainMessageID
WHERE a.ScreenName = N'FrmEmpsAdvanceRequest'
  AND (@BranchID IS NULL OR ISNULL(a.BranchId, @BranchID) = @BranchID)
  AND (@DepartmentID IS NULL OR ISNULL(a.DepartmentID, @DepartmentID) = @DepartmentID)
  AND COALESCE(NULLIF(w.EmpID,0), NULLIF(w.EmpID1,0)) IS NOT NULL;", connection, transaction))
            {
                command.Parameters.Add("@RequestID", SqlDbType.Int).Value = requestId;
                command.Parameters.Add("@BranchID", SqlDbType.Int).Value = employee.BranchId.HasValue ? (object)employee.BranchId.Value : DBNull.Value;
                command.Parameters.Add("@DepartmentID", SqlDbType.Int).Value = employee.DepartmentId.HasValue ? (object)employee.DepartmentId.Value : DBNull.Value;
                command.Parameters.Add("@Remarks", SqlDbType.NVarChar, 4000).Value = DbText(string.IsNullOrWhiteSpace(remarks) ? "تم إرسال طلب السلفة للاعتماد من الويب." : remarks);
                command.Parameters.Add("@FromUser", SqlDbType.NVarChar, 200).Value = DbText(userName);
                inserted = command.ExecuteNonQuery();
            }

            if (inserted == 0)
            {
                using (var fallback = new SqlCommand(@"
INSERT INTO dbo.ApprovalData
(ScreenName, levelo, EmpID, levelorder, currorder, Transaction_ID, NoteID, Currcursor, Remarks, NoteSerial, Transaction_Date, FromUser, SendTime)
VALUES
(N'FrmEmpsAdvanceRequest', 0, @UserID, 0, 0, @RequestID, NULL, 1, @Remarks, CONVERT(NVARCHAR(50), @RequestID), GETDATE(), @FromUser, GETDATE());", connection, transaction))
                {
                    fallback.Parameters.Add("@RequestID", SqlDbType.Int).Value = requestId;
                    fallback.Parameters.Add("@UserID", SqlDbType.Float).Value = userId.HasValue ? (object)userId.Value : DBNull.Value;
                    fallback.Parameters.Add("@Remarks", SqlDbType.NVarChar, 4000).Value = DbText(string.IsNullOrWhiteSpace(remarks) ? "تم إرسال طلب السلفة للاعتماد من الويب." : remarks);
                    fallback.Parameters.Add("@FromUser", SqlDbType.NVarChar, 200).Value = DbText(userName);
                    fallback.ExecuteNonQuery();
                }
            }
        }

        private static void ApplyAdvanceLockState(EmployeeAdvanceViewModel advance)
        {
            advance.IsDisbursed = advance.ActualAdvanceId.HasValue;
            advance.StatusText = "مسودة";
            if (advance.IsDisbursed)
            {
                advance.StatusText = "مصروفة";
                advance.LockReason = "تم صرف الطلب كسلفة فعلية رقم " + advance.ActualAdvanceId.Value + ".";
            }
            else if (advance.AccountingApproved)
            {
                advance.StatusText = "معتمدة محاسبياً";
                advance.LockReason = "لا يمكن تعديل أو حذف طلب سلفة تم اعتماده محاسبياً.";
            }
            else if (advance.Rejected)
            {
                advance.StatusText = "مرفوضة / ملغاة";
                advance.LockReason = "طلب السلفة مرفوض أو ملغى ولا يمكن تعديله.";
            }
            else if (advance.Approved)
            {
                advance.StatusText = "معتمدة";
                advance.LockReason = "لا يمكن تعديل أو حذف طلب سلفة معتمد.";
            }
            else if (advance.Submitted || advance.Posted)
            {
                advance.StatusText = "قيد الاعتماد";
                advance.LockReason = "لا يمكن تعديل أو حذف طلب سلفة تم إرساله للاعتماد.";
            }
            else if (advance.PaidPartsCount > 0)
            {
                advance.StatusText = "مرتبطة بأقساط";
                advance.LockReason = "لا يمكن تعديل أو حذف طلب سلفة له أقساط مسددة.";
            }

            advance.CanEdit = string.IsNullOrWhiteSpace(advance.LockReason);
            advance.CanDelete = advance.CanEdit;
            advance.CanDisburse = advance.Approved && !advance.IsDisbursed && !advance.Rejected;
            advance.CanSendApproval = !advance.Submitted && !advance.Approved && !advance.Rejected && !advance.IsDisbursed && !advance.AccountingApproved;
            advance.CanApprove = advance.Submitted && !advance.Approved && !advance.Rejected && !advance.IsDisbursed && !advance.AccountingApproved;
            advance.CanCancel = !advance.Rejected && !advance.IsDisbursed && advance.PaidPartsCount == 0 && !advance.AccountingApproved;
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

        private IList<EnterpriseHrLookupViewModel> LoadBranches(SqlConnection connection)
        {
            var items = new List<EnterpriseHrLookupViewModel>();
            if (!TableExists(connection, null, "TblBranchesData")) { return items; }

            using (var command = new SqlCommand(@"
SELECT TOP (500) branch_id, branch_name
FROM dbo.TblBranchesData WITH (NOLOCK)
WHERE branch_id IS NOT NULL
ORDER BY branch_name;", connection))
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    items.Add(new EnterpriseHrLookupViewModel
                    {
                        Id = ReadInt(reader, "branch_id"),
                        Name = ReadString(reader, "branch_name")
                    });
                }
            }

            return items;
        }

        private IList<EnterpriseHrLookupViewModel> LoadDepartments(SqlConnection connection)
        {
            var items = new List<EnterpriseHrLookupViewModel>();
            if (!TableExists(connection, null, "TblEmpDepartments")) { return items; }

            using (var command = new SqlCommand(@"
SELECT TOP (500) DeparmentID, DepartmentName
FROM dbo.TblEmpDepartments WITH (NOLOCK)
WHERE DeparmentID IS NOT NULL
ORDER BY DepartmentName;", connection))
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    items.Add(new EnterpriseHrLookupViewModel
                    {
                        Id = ReadInt(reader, "DeparmentID"),
                        Name = ReadString(reader, "DepartmentName")
                    });
                }
            }

            return items;
        }

        private static IList<EnterpriseHrLookupItemViewModel> LoadVacationTypes(SqlConnection connection, SqlTransaction transaction)
        {
            var items = new List<EnterpriseHrLookupItemViewModel>();
            using (var command = new SqlCommand(@"
IF OBJECT_ID(N'dbo.TblVacationTypes', N'U') IS NOT NULL
BEGIN
    SELECT TOP (100) NULLIF(LTRIM(RTRIM(ISNULL(Name,N''))), N'') AS VacationType
    FROM dbo.TblVacationTypes WITH (NOLOCK)
    WHERE NULLIF(LTRIM(RTRIM(ISNULL(Name,N''))), N'') IS NOT NULL
    ORDER BY Name;
END
ELSE
BEGIN
    SELECT TOP (0) CAST(NULL AS NVARCHAR(200)) AS VacationType;
END", connection, transaction))
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    var value = ReadString(reader, "VacationType");
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        items.Add(new EnterpriseHrLookupItemViewModel { Value = value, Text = value });
                    }
                }
            }

            if (items.Count == 0)
            {
                using (var command = new SqlCommand(@"
SELECT DISTINCT TOP (100) NULLIF(LTRIM(RTRIM(ISNULL(VocationType,N''))), N'') AS VacationType
FROM dbo.TblVocation WITH (NOLOCK)
WHERE NULLIF(LTRIM(RTRIM(ISNULL(VocationType,N''))), N'') IS NOT NULL
ORDER BY VacationType;", connection, transaction))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var value = ReadString(reader, "VacationType");
                        if (!string.IsNullOrWhiteSpace(value))
                        {
                            items.Add(new EnterpriseHrLookupItemViewModel { Value = value, Text = value });
                        }
                    }
                }
            }

            if (items.All(x => !string.Equals(x.Value, "إجازة سنوية", StringComparison.OrdinalIgnoreCase)))
            {
                items.Insert(0, new EnterpriseHrLookupItemViewModel { Value = "إجازة سنوية", Text = "إجازة سنوية" });
            }

            return items;
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

        private static LegacyHrFinanceSaveResult ValidateVacationRequest(EmployeeVacationRequestViewModel request)
        {
            if (request == null) { return Fail("بيانات طلب الإجازة غير مكتملة."); }
            if (!request.EmployeeId.HasValue || request.EmployeeId.Value <= 0) { return Fail("يجب اختيار الموظف."); }
            var fromDate = ParseDate(request.FromDate);
            var toDate = ParseDate(request.ToDate);
            if (!fromDate.HasValue) { return Fail("تاريخ بداية الإجازة غير صحيح."); }
            if (!toDate.HasValue) { return Fail("تاريخ نهاية الإجازة غير صحيح."); }
            if (toDate.Value.Date < fromDate.Value.Date) { return Fail("تاريخ نهاية الإجازة لا يجوز أن يكون قبل تاريخ البداية."); }
            if (request.WithSalary && request.WithoutSalary) { return Fail("اختر نوعا واحدا فقط: إجازة براتب أو بدون راتب."); }
            if (!request.WithSalary && !request.WithoutSalary) { request.WithSalary = true; }
            return new LegacyHrFinanceSaveResult { Success = true };
        }

        private EmployeeVacationRequestViewModel LoadVacationById(SqlConnection connection, SqlTransaction transaction, int id)
        {
            using (var command = new SqlCommand(@"
SELECT TOP (1)
       v.ID, v.EmpID,
       COALESCE(NULLIF(e.Fullcode,N''), NULLIF(e.Emp_Code,N''), CONVERT(NVARCHAR(30), e.Emp_ID)) AS EmployeeCode,
       e.Emp_Name, v.RecordDate, v.FromDate, v.ToDate, v.ResumeWork, v.Reson, v.VocationType,
       v.BranchID, b.branch_name AS BranchName, v.DeptID, d.DepartmentName,
       v.ManagerID, m.Emp_Name AS ManagerName, v.JobID, j.JobTypeName,
       ISNULL(v.WithSalary,0) AS WithSalary, ISNULL(v.WithoutSalary,0) AS WithoutSalary,
       ISNULL(v.ManagerApprove,0) AS ManagerApprove, ISNULL(v.Approved,0) AS Approved,
       ISNULL(v.posted,0) AS posted, ISNULL(v.notok,0) AS notok, ISNULL(v.FlagPayed,0) AS FlagPayed,
       ISNULL(v.NoVacation, CASE WHEN v.FromDate IS NULL OR v.ToDate IS NULL THEN 0 ELSE DATEDIFF(DAY, v.FromDate, v.ToDate) + 1 END) AS NoVacation,
       ent.EntitlementId,
       ent.AcuDate,
       ent.NoDayAct,
       ent.NoDayDelay,
       emb.EmbarkationId,
       CASE WHEN ent.EntitlementId IS NULL THEN 0 ELSE 1 END AS LinkedToEntitlement
FROM dbo.TblVocation v WITH (NOLOCK)
LEFT JOIN dbo.TblEmployee e WITH (NOLOCK) ON e.Emp_ID = v.EmpID
LEFT JOIN dbo.TblEmployee m WITH (NOLOCK) ON m.Emp_ID = v.ManagerID
LEFT JOIN dbo.TblBranchesData b WITH (NOLOCK) ON b.branch_id = v.BranchID
LEFT JOIN dbo.TblEmpDepartments d WITH (NOLOCK) ON d.DeparmentID = v.DeptID
LEFT JOIN dbo.TblEmpJobsTypes j WITH (NOLOCK) ON j.JobTypeID = v.JobID
OUTER APPLY (SELECT TOP (1) ve.ID AS EntitlementId, ve.AcuDate, ve.NoDayAct, ve.NoDayDelay FROM dbo.TblVocationEntitlements ve WITH (NOLOCK) WHERE ISNULL(ve.NoOrder,0) = v.ID ORDER BY ve.ID DESC) ent
OUTER APPLY (SELECT TOP (1) eb.id AS EmbarkationId FROM dbo.TblEmbarkation eb WITH (NOLOCK) WHERE eb.OrderVocation = ent.EntitlementId ORDER BY eb.id DESC) emb
WHERE v.ID = @ID;", connection, transaction))
            {
                command.Parameters.Add("@ID", SqlDbType.Int).Value = id;
                using (var reader = command.ExecuteReader())
                {
                    if (!reader.Read()) { return null; }
                    var vacation = MapVacation(reader);
                    ApplyVacationLockState(vacation);
                    return vacation;
                }
            }
        }

        private static EmployeeVacationRequestViewModel MapVacation(IDataRecord reader)
        {
            var vacation = new EmployeeVacationRequestViewModel
            {
                Id = ReadNullableInt(reader, "ID"),
                EmployeeId = ReadNullableInt(reader, "EmpID"),
                EmployeeCode = ReadString(reader, "EmployeeCode"),
                EmployeeName = ReadString(reader, "Emp_Name"),
                BranchId = ReadNullableInt(reader, "BranchID"),
                BranchName = ReadString(reader, "BranchName"),
                DepartmentId = ReadNullableInt(reader, "DeptID"),
                DepartmentName = ReadString(reader, "DepartmentName"),
                ManagerId = ReadNullableInt(reader, "ManagerID"),
                ManagerName = ReadString(reader, "ManagerName"),
                JobId = ReadNullableInt(reader, "JobID"),
                JobName = ReadString(reader, "JobTypeName"),
                RecordDate = ReadDisplayDate(reader, "RecordDate"),
                FromDate = ReadDisplayDate(reader, "FromDate"),
                ToDate = ReadDisplayDate(reader, "ToDate"),
                ResumeWork = ReadDisplayDate(reader, "ResumeWork"),
                Reason = ReadString(reader, "Reson"),
                VacationType = ReadString(reader, "VocationType"),
                WithSalary = ReadBool(reader, "WithSalary"),
                WithoutSalary = ReadBool(reader, "WithoutSalary"),
                ManagerApproved = ReadBool(reader, "ManagerApprove"),
                HrApproved = ReadBool(reader, "Approved"),
                Submitted = ReadBool(reader, "posted"),
                Rejected = ReadBool(reader, "notok"),
                PaidOrSettled = ReadNullableInt(reader, "FlagPayed").GetValueOrDefault() != 0,
                EntitlementId = ReadNullableInt(reader, "EntitlementId"),
                EmbarkationId = ReadNullableInt(reader, "EmbarkationId"),
                ActualReturnDate = ReadDisplayDate(reader, "AcuDate"),
                ActualVacationDays = ReadDecimal(reader, "NoDayAct"),
                DelayDays = ReadDecimal(reader, "NoDayDelay"),
                LinkedToEntitlement = ReadNullableInt(reader, "LinkedToEntitlement").GetValueOrDefault() != 0,
                NoVacation = ReadDecimal(reader, "NoVacation")
            };
            return vacation;
        }

        private static void ApplyVacationLockState(EmployeeVacationRequestViewModel vacation)
        {
            vacation.StatusText = "مسودة";
            vacation.CanEdit = true;
            vacation.CanDelete = true;
            vacation.CanManagerApprove = false;
            vacation.CanHrApprove = false;
            vacation.CanReject = false;
            vacation.CanCancel = false;

            if (vacation.LinkedToEntitlement || vacation.PaidOrSettled)
            {
                vacation.StatusText = "مرتبطة بمستحقات الإجازة";
                vacation.LockReason = "طلب الإجازة مرتبط بمستحقات/تسوية إجازة ولا يمكن تعديله أو إلغاؤه من شاشة الطلبات.";
                vacation.CanEdit = false;
                vacation.CanDelete = false;
                vacation.CanDeleteEntitlement = vacation.EntitlementId.HasValue;
                vacation.CanRecordReturnToWork = vacation.EntitlementId.HasValue && !vacation.EmbarkationId.HasValue;
                vacation.CanDeleteReturnToWork = vacation.EmbarkationId.HasValue;
                return;
            }

            if (vacation.Rejected)
            {
                vacation.StatusText = "مرفوضة / ملغاة";
                vacation.LockReason = "تم رفض أو إلغاء الطلب.";
                vacation.CanEdit = false;
                vacation.CanDelete = true;
                return;
            }

            if (vacation.HrApproved)
            {
                vacation.StatusText = "معتمدة من الموارد البشرية";
                vacation.LockReason = "تم اعتماد الطلب من الموارد البشرية ولا يمكن تعديله أو إلغاؤه من شاشة الطلبات.";
                vacation.CanEdit = false;
                vacation.CanDelete = false;
                vacation.CanCancel = false;
                vacation.CanCreateEntitlement = true;
                return;
            }

            if (vacation.ManagerApproved)
            {
                vacation.StatusText = "معتمدة من المدير";
                vacation.LockReason = "تم اعتماد الطلب من المدير، ولا يمكن تعديله إلا بعد إلغاء الاعتماد.";
                vacation.CanEdit = false;
                vacation.CanDelete = false;
                vacation.CanHrApprove = true;
                vacation.CanReject = true;
                vacation.CanCancel = false;
                return;
            }

            if (vacation.Submitted)
            {
                vacation.StatusText = "قيد مراجعة المدير";
                vacation.CanManagerApprove = true;
                vacation.CanReject = true;
                vacation.CanCancel = true;
                return;
            }
        }

        private static void AddVacationParameters(SqlCommand command, EmployeeVacationRequestViewModel request, EnterpriseHrEmployeeLookupViewModel employee, int id, int? userId, DateTime fromDate, DateTime toDate, DateTime resumeWork, decimal requestedDays)
        {
            command.Parameters.Add("@ID", SqlDbType.Int).Value = id;
            command.Parameters.Add("@RecordDate", SqlDbType.DateTime).Value = DateTime.Today;
            command.Parameters.Add("@BranchID", SqlDbType.Int).Value = employee.BranchId.HasValue ? (object)employee.BranchId.Value : DBNull.Value;
            command.Parameters.Add("@EmpID", SqlDbType.Int).Value = employee.Id;
            command.Parameters.Add("@ManagerID", SqlDbType.Int).Value = request.ManagerId.HasValue ? (object)request.ManagerId.Value : DBNull.Value;
            command.Parameters.Add("@ProjectID", SqlDbType.Int).Value = DBNull.Value;
            command.Parameters.Add("@FromDate", SqlDbType.DateTime).Value = fromDate;
            command.Parameters.Add("@ToDate", SqlDbType.DateTime).Value = toDate;
            command.Parameters.Add("@JobID", SqlDbType.Int).Value = request.JobId.HasValue ? (object)request.JobId.Value : DBNull.Value;
            command.Parameters.Add("@Reason", SqlDbType.NVarChar, 4000).Value = DbText(request.Reason);
            command.Parameters.Add("@UserID", SqlDbType.Int).Value = userId.HasValue ? (object)userId.Value : DBNull.Value;
            command.Parameters.Add("@TypeVocation", SqlDbType.Int).Value = request.WithoutSalary ? 1 : 0;
            command.Parameters.Add("@VocationType", SqlDbType.NVarChar, 200).Value = DbText(string.IsNullOrWhiteSpace(request.VacationType) ? (request.WithoutSalary ? "إجازة بدون راتب" : "إجازة سنوية") : request.VacationType);
            command.Parameters.Add("@WithSalary", SqlDbType.Bit).Value = request.WithSalary;
            command.Parameters.Add("@WithoutSalary", SqlDbType.Bit).Value = request.WithoutSalary;
            command.Parameters.Add("@ResumeWork", SqlDbType.DateTime).Value = resumeWork;
            command.Parameters.Add("@NoVacation", SqlDbType.Float).Value = Convert.ToDouble(requestedDays);
            command.Parameters.Add("@DeptID", SqlDbType.Int).Value = employee.DepartmentId.HasValue ? (object)employee.DepartmentId.Value : DBNull.Value;
            command.Parameters.Add("@BeginDate", SqlDbType.DateTime).Value = fromDate;
            command.Parameters.Add("@TotalDay", SqlDbType.Float).Value = Convert.ToDouble(requestedDays);
        }

        private static string FindVacationOverlap(SqlConnection connection, SqlTransaction transaction, int employeeId, DateTime fromDate, DateTime toDate, int excludeId)
        {
            using (var command = new SqlCommand(@"
SELECT TOP (1) ID, FromDate, ToDate
FROM dbo.TblVocation WITH (NOLOCK)
WHERE EmpID = @EmployeeId
  AND ID <> @ExcludeId
  AND ISNULL(notok,0) = 0
  AND FromDate IS NOT NULL
  AND ToDate IS NOT NULL
  AND FromDate <= @ToDate
  AND ToDate >= @FromDate
ORDER BY FromDate DESC;", connection, transaction))
            {
                command.Parameters.Add("@EmployeeId", SqlDbType.Int).Value = employeeId;
                command.Parameters.Add("@ExcludeId", SqlDbType.Int).Value = excludeId;
                command.Parameters.Add("@FromDate", SqlDbType.DateTime).Value = fromDate;
                command.Parameters.Add("@ToDate", SqlDbType.DateTime).Value = toDate;
                using (var reader = command.ExecuteReader())
                {
                    if (!reader.Read()) { return null; }
                    return "يوجد طلب إجازة متداخل لنفس الموظف رقم " + ReadInt(reader, "ID") + " خلال الفترة من " + ReadDisplayDate(reader, "FromDate") + " إلى " + ReadDisplayDate(reader, "ToDate") + ".";
                }
            }
        }

        private static void SendVacationToApproval(SqlConnection connection, SqlTransaction transaction, EnterpriseHrEmployeeLookupViewModel employee, int vacationId, int? userId, string userName, string remarks)
        {
            using (var delete = new SqlCommand(@"
DELETE FROM dbo.ApprovalData
WHERE ScreenName IN (N'formvocatinl', N'FrmEmpVacations')
  AND CONVERT(INT, Transaction_ID) = @VacationID;", connection, transaction))
            {
                delete.Parameters.Add("@VacationID", SqlDbType.Int).Value = vacationId;
                delete.ExecuteNonQuery();
            }

            var inserted = 0;
            using (var command = new SqlCommand(@"
INSERT INTO dbo.ApprovalData
(ScreenName, levelo, EmpID, levelorder, currorder, Transaction_ID, NoteID, Currcursor, Remarks, NoteSerial, Transaction_Date, FromUser, SendTime)
SELECT N'formvocatinl',
       d.PlainMessageID,
       COALESCE(NULLIF(w.EmpID,0), NULLIF(w.EmpID1,0)),
       d.id,
       w.id,
       @VacationID,
       NULL,
       CASE WHEN ROW_NUMBER() OVER (ORDER BY d.id, w.id) = 1 THEN 1 ELSE NULL END,
       @Remarks,
       CONVERT(NVARCHAR(50), @VacationID),
       GETDATE(),
       @FromUser,
       GETDATE()
FROM dbo.TblApprovalDef a WITH (NOLOCK)
INNER JOIN dbo.TblApprovalDefDetails d WITH (NOLOCK) ON d.lMessageDefID = a.id
INNER JOIN dbo.TbllevelWorker w WITH (NOLOCK) ON w.LevelID = d.PlainMessageID
WHERE a.ScreenName = N'formvocatinl'
  AND (@BranchID IS NULL OR ISNULL(a.BranchId, @BranchID) = @BranchID)
  AND (@DepartmentID IS NULL OR ISNULL(a.DepartmentID, @DepartmentID) = @DepartmentID)
  AND COALESCE(NULLIF(w.EmpID,0), NULLIF(w.EmpID1,0)) IS NOT NULL;", connection, transaction))
            {
                command.Parameters.Add("@VacationID", SqlDbType.Int).Value = vacationId;
                command.Parameters.Add("@BranchID", SqlDbType.Int).Value = employee.BranchId.HasValue ? (object)employee.BranchId.Value : DBNull.Value;
                command.Parameters.Add("@DepartmentID", SqlDbType.Int).Value = employee.DepartmentId.HasValue ? (object)employee.DepartmentId.Value : DBNull.Value;
                command.Parameters.Add("@Remarks", SqlDbType.NVarChar, 4000).Value = DbText(remarks);
                command.Parameters.Add("@FromUser", SqlDbType.NVarChar, 200).Value = DbText(userName);
                inserted = command.ExecuteNonQuery();
            }

            if (inserted == 0)
            {
                using (var fallback = new SqlCommand(@"
INSERT INTO dbo.ApprovalData
(ScreenName, levelo, EmpID, levelorder, currorder, Transaction_ID, NoteID, Currcursor, Remarks, NoteSerial, Transaction_Date, FromUser, SendTime)
VALUES
(N'formvocatinl', 0, @UserID, 0, 0, @VacationID, NULL, 1, @Remarks, CONVERT(NVARCHAR(50), @VacationID), GETDATE(), @FromUser, GETDATE());", connection, transaction))
                {
                    fallback.Parameters.Add("@VacationID", SqlDbType.Int).Value = vacationId;
                    fallback.Parameters.Add("@UserID", SqlDbType.Float).Value = userId.HasValue ? (object)userId.Value : DBNull.Value;
                    fallback.Parameters.Add("@Remarks", SqlDbType.NVarChar, 4000).Value = DbText(remarks);
                    fallback.Parameters.Add("@FromUser", SqlDbType.NVarChar, 200).Value = DbText(userName);
                    fallback.ExecuteNonQuery();
                }
            }
        }

        private LegacyHrFinanceSaveResult ChangeVacationApproval(int id, int? userId, string userName, string remarks, string action)
        {
            if (id <= 0) { return Fail("رقم طلب الإجازة غير صحيح."); }
            using (var connection = _connectionFactory.CreateOpenConnection())
            using (var transaction = connection.BeginTransaction())
            {
                try
                {
                    var vacation = LoadVacationById(connection, transaction, id);
                    if (vacation == null) { transaction.Rollback(); return Fail("لم يتم العثور على طلب الإجازة."); }
                    if (vacation.LinkedToEntitlement || vacation.PaidOrSettled)
                    {
                        transaction.Rollback();
                        return Fail(vacation.LockReason);
                    }

                    switch (action)
                    {
                        case "MANAGER_APPROVE":
                        case "HR_APPROVE":
                            return ApproveVacationCurrentStep(connection, transaction, vacation, userId, userName, remarks, action);
                        case "REJECT":
                            return RejectVacationCurrentStep(connection, transaction, vacation, userId, userName, remarks);
                        case "CANCEL":
                            if (!vacation.CanCancel && !vacation.CanDelete) { transaction.Rollback(); return Fail("لا يمكن إلغاء هذا الطلب في حالته الحالية."); }
                            return CancelVacationRequest(connection, transaction, vacation, userId, userName, remarks);
                        default:
                            transaction.Rollback();
                            return Fail("إجراء غير معروف على طلب الإجازة.");
                    }
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        private LegacyHrFinanceSaveResult ApproveVacationCurrentStep(SqlConnection connection, SqlTransaction transaction, EmployeeVacationRequestViewModel vacation, int? userId, string userName, string remarks, string action)
        {
            if (vacation.HrApproved)
            {
                transaction.Rollback();
                return Fail("طلب الإجازة معتمد بالفعل ولا يحتاج إلى اعتماد آخر.");
            }

            if (!vacation.Submitted)
            {
                transaction.Rollback();
                return Fail("يجب إرسال طلب الإجازة للموافقة قبل اعتماده.");
            }

            var currentApprovalId = GetVacationCurrentApprovalId(connection, transaction, vacation.Id.GetValueOrDefault());
            if (!currentApprovalId.HasValue)
            {
                if (HasVacationApprovalRows(connection, transaction, vacation.Id.GetValueOrDefault()))
                {
                    transaction.Rollback();
                    return Fail("لا يوجد مستوى موافقة حالي لهذا الطلب. يرجى مراجعة إعدادات الموافقات أو إعادة إرسال الطلب للمراجعة.");
                }

                var employee = GetEmployee(connection, transaction, vacation.EmployeeId.GetValueOrDefault(), false);
                if (employee == null)
                {
                    transaction.Rollback();
                    return Fail("لا يمكن إنشاء مسار الموافقة لأن بيانات الموظف غير موجودة.");
                }

                SendVacationToApproval(connection, transaction, employee, vacation.Id.GetValueOrDefault(), userId, userName, "تم إنشاء مسار موافقة لطلب الإجازة من الويب.");
                currentApprovalId = GetVacationCurrentApprovalId(connection, transaction, vacation.Id.GetValueOrDefault());
                if (!currentApprovalId.HasValue)
                {
                    transaction.Rollback();
                    return Fail("تعذر تحديد مستوى الموافقة الحالي بعد إرسال الطلب.");
                }
            }

            using (var approve = new SqlCommand(@"
UPDATE dbo.ApprovalData
SET Currcursor = NULL,
    ApprovDate = GETDATE(),
    CancelApprove = NULL,
    Remarks = @Remarks,
    FromUser = @FromUser
WHERE id = @ApprovalID
  AND ScreenName = N'formvocatinl'
  AND ApprovDate IS NULL
  AND CancelApprove IS NULL;", connection, transaction))
            {
                approve.Parameters.Add("@ApprovalID", SqlDbType.Int).Value = currentApprovalId.Value;
                approve.Parameters.Add("@Remarks", SqlDbType.NVarChar, 4000).Value = DbText(string.IsNullOrWhiteSpace(remarks) ? "تم اعتماد مستوى الموافقة من الويب." : remarks);
                approve.Parameters.Add("@FromUser", SqlDbType.NVarChar, 200).Value = DbText(userName);
                if (approve.ExecuteNonQuery() == 0)
                {
                    transaction.Rollback();
                    return Fail("تعذر اعتماد مستوى الموافقة الحالي، قد يكون تم اعتماده أو إلغاؤه من مستخدم آخر.");
                }
            }

            var nextApprovalId = GetVacationNextApprovalId(connection, transaction, vacation.Id.GetValueOrDefault());
            if (nextApprovalId.HasValue)
            {
                using (var next = new SqlCommand(@"
UPDATE dbo.ApprovalData
SET Currcursor = 1,
    SendTime = GETDATE(),
    FromUser = @FromUser
WHERE id = @ApprovalID;", connection, transaction))
                {
                    next.Parameters.Add("@ApprovalID", SqlDbType.Int).Value = nextApprovalId.Value;
                    next.Parameters.Add("@FromUser", SqlDbType.NVarChar, 200).Value = DbText(userName);
                    next.ExecuteNonQuery();
                }

                using (var command = new SqlCommand(@"
UPDATE dbo.TblVocation
SET ManagerApprove = 1,
    notok = 0,
    ok = 0
WHERE ID = @ID;", connection, transaction))
                {
                    command.Parameters.Add("@ID", SqlDbType.Int).Value = vacation.Id.GetValueOrDefault();
                    command.ExecuteNonQuery();
                }

                transaction.Commit();
                return new LegacyHrFinanceSaveResult { Success = true, Id = vacation.Id, Message = action == "MANAGER_APPROVE" ? "تم اعتماد المستوى الحالي وإرسال الطلب للمستوى التالي." : "تم اعتماد المستوى الحالي وما زال الطلب في انتظار مستوى موافقة آخر." };
            }

            using (var final = new SqlCommand(@"
UPDATE dbo.TblVocation
SET ManagerApprove = 1,
    Approved = 1,
    ok = 1,
    notok = 0
WHERE ID = @ID;", connection, transaction))
            {
                final.Parameters.Add("@ID", SqlDbType.Int).Value = vacation.Id.GetValueOrDefault();
                final.ExecuteNonQuery();
            }

            transaction.Commit();
            return new LegacyHrFinanceSaveResult { Success = true, Id = vacation.Id, Message = "تم اعتماد طلب الإجازة نهائياً." };
        }

        private LegacyHrFinanceSaveResult RejectVacationCurrentStep(SqlConnection connection, SqlTransaction transaction, EmployeeVacationRequestViewModel vacation, int? userId, string userName, string remarks)
        {
            if (!vacation.CanReject)
            {
                transaction.Rollback();
                return Fail("لا يمكن رفض هذا الطلب في حالته الحالية.");
            }

            var currentApprovalId = GetVacationCurrentApprovalId(connection, transaction, vacation.Id.GetValueOrDefault());
            if (currentApprovalId.HasValue)
            {
                using (var reject = new SqlCommand(@"
UPDATE dbo.ApprovalData
SET Currcursor = NULL,
    CancelApprove = GETDATE(),
    Remarks = @Remarks,
    FromUser = @FromUser
WHERE id = @ApprovalID
  AND ScreenName = N'formvocatinl'
  AND ApprovDate IS NULL;", connection, transaction))
                {
                    reject.Parameters.Add("@ApprovalID", SqlDbType.Int).Value = currentApprovalId.Value;
                    reject.Parameters.Add("@Remarks", SqlDbType.NVarChar, 4000).Value = DbText(string.IsNullOrWhiteSpace(remarks) ? "تم رفض اعتماد طلب الإجازة من الويب." : remarks);
                    reject.Parameters.Add("@FromUser", SqlDbType.NVarChar, 200).Value = DbText(userName);
                    reject.ExecuteNonQuery();
                }
            }

            using (var command = new SqlCommand(@"
UPDATE dbo.TblVocation
SET notok = 1,
    ok = 0,
    Approved = 0
WHERE ID = @ID;", connection, transaction))
            {
                command.Parameters.Add("@ID", SqlDbType.Int).Value = vacation.Id.GetValueOrDefault();
                command.ExecuteNonQuery();
            }

            transaction.Commit();
            return new LegacyHrFinanceSaveResult { Success = true, Id = vacation.Id, Message = "تم رفض طلب الإجازة." };
        }

        private LegacyHrFinanceSaveResult CancelVacationRequest(SqlConnection connection, SqlTransaction transaction, EmployeeVacationRequestViewModel vacation, int? userId, string userName, string remarks)
        {
            using (var cancel = new SqlCommand(@"
UPDATE dbo.ApprovalData
SET Currcursor = NULL,
    CancelApprove = CASE WHEN ApprovDate IS NULL AND CancelApprove IS NULL THEN GETDATE() ELSE CancelApprove END,
    Remarks = CASE WHEN ApprovDate IS NULL AND CancelApprove IS NULL THEN @Remarks ELSE Remarks END,
    FromUser = CASE WHEN ApprovDate IS NULL AND CancelApprove IS NULL THEN @FromUser ELSE FromUser END
WHERE ScreenName = N'formvocatinl'
  AND CONVERT(INT, Transaction_ID) = @VacationID;", connection, transaction))
            {
                cancel.Parameters.Add("@VacationID", SqlDbType.Int).Value = vacation.Id.GetValueOrDefault();
                cancel.Parameters.Add("@Remarks", SqlDbType.NVarChar, 4000).Value = DbText(string.IsNullOrWhiteSpace(remarks) ? "تم إلغاء طلب الإجازة من الويب." : remarks);
                cancel.Parameters.Add("@FromUser", SqlDbType.NVarChar, 200).Value = DbText(userName);
                cancel.ExecuteNonQuery();
            }

            using (var command = new SqlCommand(@"
UPDATE dbo.TblVocation
SET notok = 1,
    ok = 0,
    Approved = 0,
    ManagerApprove = 0,
    posted = 0
WHERE ID = @ID;", connection, transaction))
            {
                command.Parameters.Add("@ID", SqlDbType.Int).Value = vacation.Id.GetValueOrDefault();
                command.ExecuteNonQuery();
            }

            transaction.Commit();
            return new LegacyHrFinanceSaveResult { Success = true, Id = vacation.Id, Message = "تم إلغاء طلب الإجازة بأمان." };
        }

        private static void LoadVacationApprovalHistory(SqlConnection connection, SqlTransaction transaction, EmployeeVacationRequestViewModel vacation)
        {
            vacation.ApprovalHistory.Clear();
            using (var command = new SqlCommand(@"
SELECT a.id, a.ScreenName, a.levelo, a.EmpID, u.UserName, a.ApprovDate, a.CancelApprove, a.Remarks, a.FromUser, a.Currcursor
FROM dbo.ApprovalData a WITH (NOLOCK)
LEFT JOIN dbo.TblUsers u WITH (NOLOCK) ON u.UserID = CONVERT(INT, a.EmpID)
WHERE a.ScreenName IN (N'formvocatinl', N'FrmEmpVacations')
  AND CONVERT(INT, a.Transaction_ID) = @VacationID
ORDER BY a.id;", connection, transaction))
            {
                command.Parameters.Add("@VacationID", SqlDbType.Int).Value = vacation.Id.GetValueOrDefault();
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        vacation.ApprovalHistory.Add(new EmployeeAdvanceApprovalHistoryViewModel
                        {
                            Id = ReadInt(reader, "id"),
                            ScreenName = ReadString(reader, "ScreenName"),
                            Level = ReadNullableInt(reader, "levelo"),
                            EmployeeId = ReadNullableInt(reader, "EmpID"),
                            EmployeeName = ReadString(reader, "UserName"),
                            ApprovedAt = ReadDisplayDateTime(reader, "ApprovDate"),
                            CancelledAt = ReadDisplayDateTime(reader, "CancelApprove"),
                            Remarks = ReadString(reader, "Remarks"),
                            FromUser = ReadString(reader, "FromUser"),
                            IsCurrentCursor = ReadNullableInt(reader, "Currcursor").GetValueOrDefault() == 1
                        });
                    }
                }
            }
        }

        private static void AddVacationApprovalHistory(SqlConnection connection, SqlTransaction transaction, int vacationId, int? userId, string userName, string remarks, string action, int level)
        {
            var isNegativeAction = action == "REJECT" || action == "CANCEL";
            using (var command = new SqlCommand(@"
INSERT INTO dbo.ApprovalData
(ScreenName, levelo, EmpID, levelorder, currorder, Transaction_ID, NoteID, Currcursor, ApprovDate, CancelApprove, Remarks, FromUser, SendTime, Transaction_Date)
VALUES
(N'formvocatinl', @Level, @UserID, @LevelOrder, @CurrentOrder, @TransactionID, NULL, NULL, @ApprovDate, @CancelApprove, @Remarks, @FromUser, GETDATE(), GETDATE());", connection, transaction))
            {
                command.Parameters.Add("@Level", SqlDbType.Float).Value = level;
                command.Parameters.Add("@UserID", SqlDbType.Float).Value = userId.HasValue ? (object)userId.Value : DBNull.Value;
                command.Parameters.Add("@LevelOrder", SqlDbType.Float).Value = level;
                command.Parameters.Add("@CurrentOrder", SqlDbType.Float).Value = level;
                command.Parameters.Add("@TransactionID", SqlDbType.Float).Value = vacationId;
                command.Parameters.Add("@ApprovDate", SqlDbType.DateTime).Value = isNegativeAction ? (object)DBNull.Value : DateTime.Now;
                command.Parameters.Add("@CancelApprove", SqlDbType.DateTime).Value = isNegativeAction ? (object)DateTime.Now : DBNull.Value;
                command.Parameters.Add("@Remarks", SqlDbType.NVarChar, 4000).Value = DbText((string.IsNullOrWhiteSpace(remarks) ? action : remarks));
                command.Parameters.Add("@FromUser", SqlDbType.NVarChar, 200).Value = DbText(userName);
                command.ExecuteNonQuery();
            }
        }

        private static bool VacationEntitlementExists(SqlConnection connection, SqlTransaction transaction, int vacationId, int? excludeEntitlementId)
        {
            using (var command = new SqlCommand(@"
SELECT COUNT(1)
FROM dbo.TblVocationEntitlements WITH (NOLOCK)
WHERE ISNULL(NoOrder,0) = @VacationID
  AND (@ExcludeID IS NULL OR ID <> @ExcludeID);", connection, transaction))
            {
                command.Parameters.Add("@VacationID", SqlDbType.Int).Value = vacationId;
                command.Parameters.Add("@ExcludeID", SqlDbType.Int).Value = excludeEntitlementId.HasValue ? (object)excludeEntitlementId.Value : DBNull.Value;
                return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture) > 0;
            }
        }

        private sealed class VacationEntitlementSnapshot
        {
            public int Id { get; set; }
            public int EmployeeId { get; set; }
            public int? BranchId { get; set; }
            public int? DepartmentId { get; set; }
            public int? JobId { get; set; }
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
            public decimal ApprovedDays { get; set; }
        }

        private static VacationEntitlementSnapshot GetVacationEntitlement(SqlConnection connection, SqlTransaction transaction, int entitlementId)
        {
            using (var command = new SqlCommand(@"
SELECT TOP (1)
       ID,
       EmpID,
       BranchID,
       DeptID,
       JobID,
       stratDate,
       EndDate,
       NoVacation
FROM dbo.TblVocationEntitlements WITH (NOLOCK)
WHERE ID = @ID;", connection, transaction))
            {
                command.Parameters.Add("@ID", SqlDbType.Int).Value = entitlementId;
                using (var reader = command.ExecuteReader())
                {
                    if (!reader.Read()) { return null; }
                    var startDate = ReadNullableDate(reader, "stratDate").GetValueOrDefault(DateTime.Today).Date;
                    var endDate = ReadNullableDate(reader, "EndDate").GetValueOrDefault(startDate).Date;
                    return new VacationEntitlementSnapshot
                    {
                        Id = ReadInt(reader, "ID"),
                        EmployeeId = ReadNullableInt(reader, "EmpID").GetValueOrDefault(),
                        BranchId = ReadNullableInt(reader, "BranchID"),
                        DepartmentId = ReadNullableInt(reader, "DeptID"),
                        JobId = ReadNullableInt(reader, "JobID"),
                        StartDate = startDate,
                        EndDate = endDate,
                        ApprovedDays = ReadDecimal(reader, "NoVacation")
                    };
                }
            }
        }

        private static int? GetEmbarkationIdByEntitlement(SqlConnection connection, SqlTransaction transaction, int entitlementId)
        {
            using (var command = new SqlCommand("SELECT TOP (1) id FROM dbo.TblEmbarkation WITH (UPDLOCK, HOLDLOCK) WHERE OrderVocation = @ID ORDER BY id DESC;", connection, transaction))
            {
                command.Parameters.Add("@ID", SqlDbType.Int).Value = entitlementId;
                var value = command.ExecuteScalar();
                return value == null || value == DBNull.Value ? (int?)null : Convert.ToInt32(value, CultureInfo.InvariantCulture);
            }
        }

        private static string GetVacationReturnPaymentLockMessage(SqlConnection connection, SqlTransaction transaction, int entitlementId)
        {
            using (var command = new SqlCommand(@"
SELECT TOP (1)
       ISNULL(Posted,0) AS Posted,
       ISNULL(PayedPayment,0) AS PayedPayment,
       NoteID,
       NoteSerial
FROM dbo.TblVocationEntitlements WITH (NOLOCK)
WHERE ID = @ID;", connection, transaction))
            {
                command.Parameters.Add("@ID", SqlDbType.Int).Value = entitlementId;
                using (var reader = command.ExecuteReader())
                {
                    if (!reader.Read()) { return "لم يتم العثور على مستند مستحقات الإجازة."; }
                    if (ReadNullableInt(reader, "Posted").GetValueOrDefault() != 0
                        || ReadBool(reader, "PayedPayment")
                        || ReadNullableInt(reader, "NoteID").HasValue
                        || !string.IsNullOrWhiteSpace(ReadString(reader, "NoteSerial")))
                    {
                        return "لا يمكن تعديل مباشرة العمل لأن مستند المستحقات مرتبط بدفع أو ترحيل.";
                    }
                }
            }

            if (TableExists(connection, transaction, "Notes"))
            {
                using (var command = new SqlCommand(@"
SELECT COUNT(1)
FROM dbo.Notes WITH (NOLOCK)
WHERE ISNULL(Due,0) = @ID
  AND ISNULL(NoteType,0) = 5
  AND ISNULL(CashingType,0) = 8;", connection, transaction))
                {
                    command.Parameters.Add("@ID", SqlDbType.Int).Value = entitlementId;
                    if (Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture) > 0)
                    {
                        return "لا يمكن تعديل مباشرة العمل لأنها مرتبطة بسند مدفوعات.";
                    }
                }
            }

            if (TableExists(connection, transaction, "emp_salary") && ColumnExists(connection, transaction, "emp_salary", "VocEntitID"))
            {
                using (var command = new SqlCommand("SELECT COUNT(1) FROM dbo.emp_salary WITH (NOLOCK) WHERE VocEntitID = @ID;", connection, transaction))
                {
                    command.Parameters.Add("@ID", SqlDbType.Int).Value = entitlementId;
                    if (Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture) > 0)
                    {
                        return "لا يمكن تعديل مباشرة العمل لأنها مرتبطة بمسير/راتب موظف.";
                    }
                }
            }

            return null;
        }

        private static void AddEmbarkationParameters(SqlCommand command, int embarkationId, VacationEntitlementSnapshot entitlement, DateTime actualDate, decimal approvedDays, decimal actualDays, decimal delayDays, string treatment, int? userId, string remarks)
        {
            command.Parameters.Add("@ID", SqlDbType.Int).Value = embarkationId;
            command.Parameters.Add("@BranchID", SqlDbType.Int).Value = entitlement.BranchId.HasValue ? (object)entitlement.BranchId.Value : DBNull.Value;
            command.Parameters.Add("@EmpID", SqlDbType.Int).Value = entitlement.EmployeeId;
            command.Parameters.Add("@RecordDate", SqlDbType.DateTime).Value = DateTime.Today;
            command.Parameters.Add("@DeptID", SqlDbType.Int).Value = entitlement.DepartmentId.HasValue ? (object)entitlement.DepartmentId.Value : DBNull.Value;
            command.Parameters.Add("@JobID", SqlDbType.Int).Value = entitlement.JobId.HasValue ? (object)entitlement.JobId.Value : DBNull.Value;
            command.Parameters.Add("@StartDate", SqlDbType.DateTime).Value = entitlement.StartDate;
            command.Parameters.Add("@EndDate", SqlDbType.DateTime).Value = entitlement.EndDate;
            command.Parameters.Add("@StartDateH", SqlDbType.NVarChar, 40).Value = DbText(ToHijriDate(entitlement.StartDate));
            command.Parameters.Add("@EndDateH", SqlDbType.NVarChar, 40).Value = DbText(ToHijriDate(entitlement.EndDate));
            command.Parameters.Add("@WorkDate", SqlDbType.DateTime).Value = actualDate;
            command.Parameters.Add("@WorkDateH", SqlDbType.NVarChar, 20).Value = DbText(ToHijriDate(actualDate));
            command.Parameters.Add("@UserID", SqlDbType.Int).Value = userId.HasValue ? (object)userId.Value : DBNull.Value;
            command.Parameters.Add("@Remark", SqlDbType.NVarChar, 4000).Value = DbText(string.IsNullOrWhiteSpace(remarks) ? "مباشرة عمل من الويب مرتبطة بمستحقات إجازة." : remarks);
            command.Parameters.Add("@ApprovedDays", SqlDbType.Int).Value = Convert.ToInt32(Math.Round(approvedDays, 0, MidpointRounding.AwayFromZero));
            command.Parameters.Add("@ActualDays", SqlDbType.Int).Value = Convert.ToInt32(Math.Round(actualDays, 0, MidpointRounding.AwayFromZero));
            command.Parameters.Add("@DelayDays", SqlDbType.Int).Value = Convert.ToInt32(Math.Round(delayDays, 0, MidpointRounding.AwayFromZero));
            command.Parameters.Add("@UnPaidDis", SqlDbType.Int).Value = treatment == "unpaid" ? 0 : treatment == "balance" ? 1 : (object)DBNull.Value;
            command.Parameters.Add("@OrderVocation", SqlDbType.Int).Value = entitlement.Id;
        }

        private static void SaveReturnDelayMovement(SqlConnection connection, SqlTransaction transaction, int embarkationId, int employeeId, DateTime actualDate, decimal delayDays, string treatment)
        {
            if (delayDays <= 0 || treatment == "none") { return; }

            if (treatment == "unpaid")
            {
                using (var command = new SqlCommand(@"
INSERT INTO dbo.TblInforVacatiom
(PrkID, EmpID, NoDay, RecordDate, RecordDateH, TypeVacation, Remarks)
VALUES
(@EmbarkationID, @EmpID, @NoDay, @RecordDate, @RecordDateH, 0, N'مباشرة موظف - أيام تأخير بدون راتب');", connection, transaction))
                {
                    command.Parameters.Add("@EmbarkationID", SqlDbType.Int).Value = embarkationId;
                    command.Parameters.Add("@EmpID", SqlDbType.Int).Value = employeeId;
                    command.Parameters.Add("@NoDay", SqlDbType.Float).Value = Convert.ToDouble(delayDays);
                    command.Parameters.Add("@RecordDate", SqlDbType.DateTime).Value = actualDate;
                    command.Parameters.Add("@RecordDateH", SqlDbType.NVarChar, 40).Value = DbText(ToHijriDate(actualDate));
                    command.ExecuteNonQuery();
                }
                return;
            }

            if (treatment == "balance")
            {
                using (var command = new SqlCommand(@"
INSERT INTO dbo.tblVacationData
(EmbracID, EmpID, Value, ExpectedacationDate, ExpectedacationDateH, Remark)
VALUES
(@EmbarkationID, @EmpID, @Value, @RecordDate, @RecordDateH, N'مباشرة موظف - خصم أيام تأخير من الرصيد');", connection, transaction))
                {
                    command.Parameters.Add("@EmbarkationID", SqlDbType.Int).Value = embarkationId;
                    command.Parameters.Add("@EmpID", SqlDbType.Int).Value = employeeId;
                    command.Parameters.Add("@Value", SqlDbType.Float).Value = Convert.ToDouble(delayDays * -1m);
                    command.Parameters.Add("@RecordDate", SqlDbType.DateTime).Value = actualDate;
                    command.Parameters.Add("@RecordDateH", SqlDbType.NVarChar, 100).Value = DbText(ToHijriDate(actualDate));
                    command.ExecuteNonQuery();
                }
            }
        }

        private static string NormalizeDelayTreatment(string treatment)
        {
            var value = (treatment ?? string.Empty).Trim().ToLowerInvariant();
            if (value == "unpaid" || value == "without-salary") { return "unpaid"; }
            if (value == "balance" || value == "deduct-balance") { return "balance"; }
            return "none";
        }

        private static decimal DateDiffInclusive(DateTime startDate, DateTime endDate)
        {
            return Math.Max(0m, RoundDays((decimal)(endDate.Date - startDate.Date).TotalDays + 1m));
        }

        private sealed class VacationEntitlementDeleteBoundary
        {
            public bool Exists { get; set; }
            public bool IsLocked { get; set; }
            public string Message { get; set; }
            public int? NoOrder { get; set; }
        }

        private static VacationEntitlementDeleteBoundary GetVacationEntitlementDeleteBoundary(SqlConnection connection, SqlTransaction transaction, int entitlementId)
        {
            var boundary = new VacationEntitlementDeleteBoundary();
            using (var command = new SqlCommand(@"
SELECT TOP (1)
       ID,
       NoOrder,
       ISNULL(Posted,0) AS Posted,
       ISNULL(Approved,0) AS Approved,
       ISNULL(PayedPayment,0) AS PayedPayment,
       NoteID,
       NoteSerial
FROM dbo.TblVocationEntitlements WITH (NOLOCK)
WHERE ID = @ID;", connection, transaction))
            {
                command.Parameters.Add("@ID", SqlDbType.Int).Value = entitlementId;
                using (var reader = command.ExecuteReader())
                {
                    if (!reader.Read()) { return boundary; }
                    boundary.Exists = true;
                    boundary.NoOrder = ReadNullableInt(reader, "NoOrder");
                    if (ReadNullableInt(reader, "Posted").GetValueOrDefault() != 0
                        || ReadBool(reader, "Approved")
                        || ReadBool(reader, "PayedPayment")
                        || ReadNullableInt(reader, "NoteID").HasValue
                        || !string.IsNullOrWhiteSpace(ReadString(reader, "NoteSerial")))
                    {
                        boundary.IsLocked = true;
                        boundary.Message = "لا يمكن حذف مستند مستحقات الإجازة لأنه معتمد أو مرحل أو مرتبط بسند/دفع.";
                    }
                }
            }

            if (!boundary.Exists || boundary.IsLocked) { return boundary; }

            if (TableExists(connection, transaction, "Notes"))
            {
                using (var command = new SqlCommand(@"
SELECT COUNT(1)
FROM dbo.Notes WITH (NOLOCK)
WHERE ISNULL(Due,0) = @ID
  AND ISNULL(NoteType,0) = 5
  AND ISNULL(CashingType,0) = 8;", connection, transaction))
                {
                    command.Parameters.Add("@ID", SqlDbType.Int).Value = entitlementId;
                    if (Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture) > 0)
                    {
                        boundary.IsLocked = true;
                        boundary.Message = "لا يمكن حذف مستند مستحقات الإجازة لأنه مرتبط بسند مدفوعات.";
                        return boundary;
                    }
                }
            }

            if (TableExists(connection, transaction, "emp_salary") && ColumnExists(connection, transaction, "emp_salary", "VocEntitID"))
            {
                using (var command = new SqlCommand("SELECT COUNT(1) FROM dbo.emp_salary WITH (NOLOCK) WHERE VocEntitID = @ID;", connection, transaction))
                {
                    command.Parameters.Add("@ID", SqlDbType.Int).Value = entitlementId;
                    if (Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture) > 0)
                    {
                        boundary.IsLocked = true;
                        boundary.Message = "لا يمكن حذف مستند مستحقات الإجازة لأنه مرتبط بمسير/راتب موظف.";
                        return boundary;
                    }
                }
            }

            if (TableExists(connection, transaction, "TblEmbarkation") && ColumnExists(connection, transaction, "TblEmbarkation", "VacationPaied"))
            {
                using (var command = new SqlCommand(@"
SELECT COUNT(1)
FROM dbo.TblEmbarkation WITH (NOLOCK)
WHERE ISNULL(VacationPaied,0) = 1
  AND ISNULL(Emp_ID,0) = (SELECT TOP (1) ISNULL(EmpID,0) FROM dbo.TblVocationEntitlements WITH (NOLOCK) WHERE ID = @ID);", connection, transaction))
                {
                    command.Parameters.Add("@ID", SqlDbType.Int).Value = entitlementId;
                    if (Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture) > 0)
                    {
                        boundary.IsLocked = true;
                        boundary.Message = "لا يمكن حذف مستند مستحقات الإجازة لوجود مباشرة/عودة عمل مدفوعة مرتبطة بالموظف.";
                    }
                }
            }

            return boundary;
        }

        private static int? GetVacationCurrentApprovalId(SqlConnection connection, SqlTransaction transaction, int vacationId)
        {
            using (var command = new SqlCommand(@"
SELECT TOP (1) id
FROM dbo.ApprovalData WITH (UPDLOCK, HOLDLOCK)
WHERE ScreenName = N'formvocatinl'
  AND CONVERT(INT, Transaction_ID) = @VacationID
  AND Currcursor = 1
  AND ApprovDate IS NULL
  AND CancelApprove IS NULL
ORDER BY ISNULL(levelorder, 2147483647), ISNULL(currorder, 2147483647), id;", connection, transaction))
            {
                command.Parameters.Add("@VacationID", SqlDbType.Int).Value = vacationId;
                var value = command.ExecuteScalar();
                return value == null || value == DBNull.Value ? (int?)null : Convert.ToInt32(value, CultureInfo.InvariantCulture);
            }
        }

        private static int? GetVacationNextApprovalId(SqlConnection connection, SqlTransaction transaction, int vacationId)
        {
            using (var command = new SqlCommand(@"
SELECT TOP (1) id
FROM dbo.ApprovalData WITH (UPDLOCK, HOLDLOCK)
WHERE ScreenName = N'formvocatinl'
  AND CONVERT(INT, Transaction_ID) = @VacationID
  AND ApprovDate IS NULL
  AND CancelApprove IS NULL
  AND ISNULL(Currcursor, 0) <> 1
ORDER BY ISNULL(levelorder, 2147483647), ISNULL(currorder, 2147483647), id;", connection, transaction))
            {
                command.Parameters.Add("@VacationID", SqlDbType.Int).Value = vacationId;
                var value = command.ExecuteScalar();
                return value == null || value == DBNull.Value ? (int?)null : Convert.ToInt32(value, CultureInfo.InvariantCulture);
            }
        }

        private static bool HasVacationApprovalRows(SqlConnection connection, SqlTransaction transaction, int vacationId)
        {
            using (var command = new SqlCommand(@"
SELECT COUNT(1)
FROM dbo.ApprovalData WITH (NOLOCK)
WHERE ScreenName = N'formvocatinl'
  AND CONVERT(INT, Transaction_ID) = @VacationID;", connection, transaction))
            {
                command.Parameters.Add("@VacationID", SqlDbType.Int).Value = vacationId;
                return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture) > 0;
            }
        }

        private VacationBalanceViewModel CalculateVacationBalance(SqlConnection connection, SqlTransaction transaction, VacationBalanceRequestViewModel request)
        {
            var asOfDate = ParseDate(request.AsOfDate).GetValueOrDefault(DateTime.Today).Date;
            var requestedDays = ResolveRequestedVacationDays(request);
            var employee = GetEmployee(connection, transaction, request.EmployeeId, false);
            var model = new VacationBalanceViewModel
            {
                EmployeeId = request.EmployeeId,
                AsOfDate = FormatDate(asOfDate),
                RequestedDays = requestedDays
            };

            if (employee == null)
            {
                model.EmployeeStatusMessage = "الموظف غير موجود.";
                model.Errors.Add("الموظف غير موجود.");
                model.NegativeBalancePrevented = true;
                model.CanPostPaidVacation = false;
                return model;
            }

            model.EmployeeCode = employee.Code;
            model.EmployeeName = employee.Name;
            model.IsEmployeeActive = IsEmployeeActive(connection, transaction, request.EmployeeId);
            model.EmployeeStatusMessage = model.IsEmployeeActive ? "موظف نشط" : "الموظف موقوف أو غير نشط.";
            if (!model.IsEmployeeActive)
            {
                model.Warnings.Add("الموظف موقوف أو غير نشط؛ لا يجب إنشاء إجازة مدفوعة إلا بقرار واضح.");
            }

            var contract = LoadVacationContract(connection, transaction, request.EmployeeId);
            var useContractSettings = GetVacationSettingsUseContract(connection, transaction);
            model.CalculationMode = useContractSettings ? "ContractSettings" : "ScheduledEntitlements";
            ValidateVacationSettingsWindow(connection, transaction, request, asOfDate, model);

            if (contract.Exists)
            {
                model.AnnualEntitlementDays = RoundDays(contract.AnnualVacationDays);
            }
            else
            {
                model.Warnings.Add("لا يوجد عقد موظف واضح لحساب الاستحقاق السنوي، تم الاعتماد على الحركات المسجلة فقط.");
            }

            model.OpeningBalanceDays = SumVacationOpeningBalance(connection, transaction, request.EmployeeId, asOfDate);
            model.CarryOverDays = GetLastBalanceMonth(connection, transaction, request.EmployeeId, request.ExcludeEntitlementId.GetValueOrDefault()) * 30m;
            AddBalanceLine(model, "OpeningBalance", null, "رصيد افتتاحي مؤكد من أرصدة الإجازات الافتتاحية", model.OpeningBalanceDays, "رصيد افتتاحي");

            if (useContractSettings && contract.Exists)
            {
                var contractAccruedDays = CalculateContractAccrual(connection, transaction, request.EmployeeId, asOfDate, contract, model.CarryOverDays);
                model.AccruedDays = RoundDays(contractAccruedDays + model.OpeningBalanceDays);
                model.Lines.Add(new VacationBalanceLineViewModel
                {
                    Source = "Contract",
                    SourceDate = FormatDate(contract.AccrualStartDate),
                    Description = "استحقاق محسوب من عقد الموظف وإعدادات الإجازات",
                    Days = contractAccruedDays,
                    Effect = "استحقاق"
                });
            }
            else
            {
                model.ScheduledDueDays = SumScheduledVacationDue(connection, transaction, request.EmployeeId, asOfDate);
                model.AccruedDays = model.ScheduledDueDays + model.OpeningBalanceDays;
                model.Lines.Add(new VacationBalanceLineViewModel
                {
                    Source = "tblVacationData",
                    SourceDate = FormatDate(asOfDate),
                    Description = "استحقاقات مجدولة وغير مستخدمة حتى تاريخ الحساب",
                    Days = model.ScheduledDueDays,
                    Effect = "استحقاق"
                });
            }

            model.UnpaidLeaveDays = SumVacationInfo(connection, transaction, request.EmployeeId, 0, asOfDate, request.ExcludeEntitlementId);
            model.AbsenceDeductionDays = SumVacationInfo(connection, transaction, request.EmployeeId, 1, asOfDate, request.ExcludeEntitlementId);
            model.PaidVacationConsumedDays = SumConsumedPaidVacation(connection, transaction, request.EmployeeId, asOfDate, request.ExcludeVacationId, request.ExcludeEntitlementId);
            model.PendingApprovedDays = SumPendingApprovedVacationRequests(connection, transaction, request.EmployeeId, asOfDate, request.ExcludeVacationId);

            AddBalanceLine(model, "TblInforVacatiom", null, "إجازات بدون راتب / أرصدة غير مدفوعة", model.UnpaidLeaveDays, "خصم");
            AddBalanceLine(model, "TblInforVacatiom", null, "غياب مؤثر على رصيد الإجازات", model.AbsenceDeductionDays, "خصم");
            AddBalanceLine(model, "TblVocationEntitlements", null, "إجازات مدفوعة تم تسويتها", model.PaidVacationConsumedDays, "استهلاك");
            AddBalanceLine(model, "TblVocation", null, "طلبات معتمدة لم تسو بعد", model.PendingApprovedDays, "محجوز");

            model.AvailableBeforeRequest = RoundDays(model.AccruedDays - model.UnpaidLeaveDays - model.AbsenceDeductionDays - model.PaidVacationConsumedDays - model.PendingApprovedDays);
            model.AvailableAfterRequest = RoundDays(model.AvailableBeforeRequest - requestedDays);
            model.NegativeBalancePrevented = model.AvailableAfterRequest < 0;
            model.CanPostPaidVacation = model.IsEmployeeActive && !model.NegativeBalancePrevented && requestedDays >= 0;

            if (requestedDays < 0)
            {
                model.Errors.Add("عدد أيام الإجازة المطلوب غير صحيح.");
                model.CanPostPaidVacation = false;
            }

            if (model.NegativeBalancePrevented)
            {
                model.Errors.Add("رصيد الإجازات لا يكفي. لا يسمح المحرك بإنشاء رصيد سالب للإجازة المدفوعة.");
            }

            if (model.Errors.Count > 0)
            {
                model.CanPostPaidVacation = false;
            }

            return model;
        }

        private static decimal ResolveRequestedVacationDays(VacationBalanceRequestViewModel request)
        {
            if (request.RequestedDays.HasValue)
            {
                return RoundDays(request.RequestedDays.Value);
            }

            var start = ParseDate(request.VacationStartDate);
            var end = ParseDate(request.VacationEndDate);
            if (start.HasValue && end.HasValue)
            {
                return RoundDays((decimal)(end.Value.Date - start.Value.Date).TotalDays + 1m);
            }

            return 0;
        }

        private static void AddBalanceLine(VacationBalanceViewModel model, string source, string sourceId, string description, decimal days, string effect)
        {
            if (days == 0) { return; }
            model.Lines.Add(new VacationBalanceLineViewModel
            {
                Source = source,
                SourceId = sourceId,
                Description = description,
                Days = RoundDays(days),
                Effect = effect
            });
        }

        private static bool IsEmployeeActive(SqlConnection connection, SqlTransaction transaction, int employeeId)
        {
            using (var command = new SqlCommand(@"
SELECT CASE WHEN ISNULL(chkStop,0)=0 AND ISNULL(workstate,0)=1 THEN 1 ELSE 0 END
FROM dbo.TblEmployee WITH (NOLOCK)
WHERE Emp_ID = @EmployeeId;", connection, transaction))
            {
                command.Parameters.Add("@EmployeeId", SqlDbType.Int).Value = employeeId;
                return Convert.ToInt32(command.ExecuteScalar() ?? 0) == 1;
            }
        }

        private sealed class VacationContractSnapshot
        {
            public bool Exists { get; set; }
            public DateTime AccrualStartDate { get; set; }
            public DateTime? LastVacationDate { get; set; }
            public decimal DuePeriodMonths { get; set; }
            public decimal AnnualVacationDays { get; set; }
        }

        private static VacationContractSnapshot LoadVacationContract(SqlConnection connection, SqlTransaction transaction, int employeeId)
        {
            using (var command = new SqlCommand(@"
SELECT TOP (1)
       ISNULL(c.Contract_date, e.BignDateWork) AS ContractDate,
       e.lastHolidaydate,
       ISNULL(c.Due_period_no, 0) AS DuePeriodNo,
       ISNULL(c.due_period, 0) AS DuePeriodType,
       ISNULL(c.Holiday_period_no, 0) AS HolidayPeriodNo,
       ISNULL(c.Holiday_period, 0) AS HolidayPeriodType
FROM dbo.TblEmployee e WITH (NOLOCK)
LEFT JOIN dbo.Contract c WITH (NOLOCK) ON c.Emp_id = e.Emp_ID
WHERE e.Emp_ID = @EmployeeId
ORDER BY ISNULL(c.Contract_ID, 0) DESC;", connection, transaction))
            {
                command.Parameters.Add("@EmployeeId", SqlDbType.Int).Value = employeeId;
                using (var reader = command.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        return new VacationContractSnapshot();
                    }

                    var duePeriodNo = ReadDecimal(reader, "DuePeriodNo");
                    var duePeriodType = ReadInt(reader, "DuePeriodType");
                    var holidayPeriodNo = ReadDecimal(reader, "HolidayPeriodNo");
                    var holidayPeriodType = ReadInt(reader, "HolidayPeriodType");
                    var dueMonths = duePeriodNo;
                    if (duePeriodType == 1) { dueMonths = duePeriodNo * 12m; }
                    else if (duePeriodType == 2) { dueMonths = duePeriodNo / 30m; }
                    if (dueMonths <= 0) { dueMonths = 12m; }

                    var holidayDays = holidayPeriodType == 1 ? holidayPeriodNo * 30m : holidayPeriodNo;
                    if (holidayDays < 0) { holidayDays = 0; }

                    var contractDate = ReadNullableDate(reader, "ContractDate") ?? DateTime.Today;
                    var lastVacationDate = ReadNullableDate(reader, "lastHolidaydate");
                    return new VacationContractSnapshot
                    {
                        Exists = true,
                        AccrualStartDate = lastVacationDate ?? contractDate,
                        LastVacationDate = lastVacationDate,
                        DuePeriodMonths = dueMonths,
                        AnnualVacationDays = RoundDays(holidayDays * (12m / dueMonths))
                    };
                }
            }
        }

        private static bool GetVacationSettingsUseContract(SqlConnection connection, SqlTransaction transaction)
        {
            using (var command = new SqlCommand("SELECT TOP (1) 1 FROM dbo.TblVacationSettings WITH (NOLOCK) WHERE ISNULL(Typ,0)=1", connection, transaction))
            {
                return command.ExecuteScalar() != null;
            }
        }

        private static void ValidateVacationSettingsWindow(SqlConnection connection, SqlTransaction transaction, VacationBalanceRequestViewModel request, DateTime asOfDate, VacationBalanceViewModel model)
        {
            var requestDate = ParseDate(request.VacationStartDate).GetValueOrDefault(asOfDate).Date;
            using (var count = new SqlCommand("SELECT COUNT(1) FROM dbo.TblVacationSettingsDet WITH (NOLOCK)", connection, transaction))
            {
                if (Convert.ToInt32(count.ExecuteScalar(), CultureInfo.InvariantCulture) == 0)
                {
                    model.Warnings.Add("لا توجد فترات إعدادات إجازات معرفة في TblVacationSettingsDet؛ تم الحساب من الحركات الفعلية بدون تقييد فترة الإعدادات.");
                    return;
                }
            }

            using (var command = new SqlCommand(@"
SELECT TOP (1) ID, FrmDate, ToDate, AlowDate
FROM dbo.TblVacationSettingsDet WITH (NOLOCK)
WHERE @RequestDate >= ISNULL(FrmDate, @RequestDate)
  AND @RequestDate <= ISNULL(ToDate, @RequestDate)
ORDER BY ID DESC;", connection, transaction))
            {
                command.Parameters.Add("@RequestDate", SqlDbType.DateTime).Value = requestDate;
                using (var reader = command.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        model.Errors.Add("تاريخ بداية الإجازة خارج فترات إعدادات الإجازات المسموح بها.");
                        return;
                    }

                    var allowDate = ReadNullableDate(reader, "AlowDate");
                    if (allowDate.HasValue && requestDate > allowDate.Value.Date)
                    {
                        model.Errors.Add("تاريخ بداية الإجازة بعد آخر تاريخ مسموح به في إعدادات الإجازات.");
                    }
                }
            }
        }

        private static decimal CalculateContractAccrual(SqlConnection connection, SqlTransaction transaction, int employeeId, DateTime asOfDate, VacationContractSnapshot contract, decimal carryOverDays)
        {
            var start = contract.AccrualStartDate.Date;
            if (asOfDate < start) { return 0; }

            var daysSinceStart = (decimal)(asOfDate.Date - start).TotalDays;
            var unpaidDeductedDays = SumUnpaidReturnBalance(connection, transaction, employeeId);
            var netDays = daysSinceStart + carryOverDays - unpaidDeductedDays;
            if (netDays <= 0 || contract.AnnualVacationDays <= 0) { return 0; }
            return RoundDays((contract.AnnualVacationDays / 360m) * netDays);
        }

        private static decimal SumScheduledVacationDue(SqlConnection connection, SqlTransaction transaction, int employeeId, DateTime asOfDate)
        {
            using (var command = new SqlCommand(@"
SELECT ISNULL(SUM(ISNULL([Value],0)),0)
FROM dbo.tblVacationData WITH (NOLOCK)
WHERE EmpID = @EmployeeId
  AND ExpectedacationDate <= @AsOfDate
  AND InstVacaID IS NULL
  AND Status1 IS NULL;", connection, transaction))
            {
                command.Parameters.Add("@EmployeeId", SqlDbType.Int).Value = employeeId;
                command.Parameters.Add("@AsOfDate", SqlDbType.DateTime).Value = asOfDate.Date;
                return ToDecimal(command.ExecuteScalar());
            }
        }

        private static decimal SumVacationOpeningBalance(SqlConnection connection, SqlTransaction transaction, int employeeId, DateTime asOfDate)
        {
            var scheduledOpening = 0m;
            using (var command = new SqlCommand(@"
SELECT ISNULL(SUM(ISNULL([Value],0)),0)
FROM dbo.tblVacationData WITH (NOLOCK)
WHERE EmpID = @EmployeeId
  AND InstVacaID IS NOT NULL
  AND ExpectedacationDate <= @AsOfDate
  AND Status1 IS NULL;", connection, transaction))
            {
                command.Parameters.Add("@EmployeeId", SqlDbType.Int).Value = employeeId;
                command.Parameters.Add("@AsOfDate", SqlDbType.DateTime).Value = asOfDate.Date;
                scheduledOpening = ToDecimal(command.ExecuteScalar());
            }

            if (scheduledOpening != 0)
            {
                return RoundDays(scheduledOpening);
            }

            using (var command = new SqlCommand(@"
SELECT ISNULL(SUM(ISNULL(VacBalance,0)),0)
FROM dbo.TblInstalVacationDet WITH (NOLOCK)
WHERE EmpID = @EmployeeId
  AND ISNULL(BeginDate, @AsOfDate) <= @AsOfDate;", connection, transaction))
            {
                command.Parameters.Add("@EmployeeId", SqlDbType.Int).Value = employeeId;
                command.Parameters.Add("@AsOfDate", SqlDbType.DateTime).Value = asOfDate.Date;
                return RoundDays(ToDecimal(command.ExecuteScalar()));
            }
        }

        private static decimal SumVacationInfo(SqlConnection connection, SqlTransaction transaction, int employeeId, int typeVacation, DateTime asOfDate, int? excludeEntitlementId)
        {
            using (var command = new SqlCommand(@"
SELECT ISNULL(SUM(ABS(ISNULL(NoDay,0))),0)
FROM dbo.TblInforVacatiom WITH (NOLOCK)
WHERE EmpID = @EmployeeId
  AND TypeVacation = @TypeVacation
  AND RecordDate <= @AsOfDate
  AND (@ExcludeEntitlementId IS NULL OR ISNULL(VacatioID,0) <> @ExcludeEntitlementId);", connection, transaction))
            {
                command.Parameters.Add("@EmployeeId", SqlDbType.Int).Value = employeeId;
                command.Parameters.Add("@TypeVacation", SqlDbType.Int).Value = typeVacation;
                command.Parameters.Add("@AsOfDate", SqlDbType.DateTime).Value = asOfDate.Date;
                command.Parameters.Add("@ExcludeEntitlementId", SqlDbType.Int).Value = excludeEntitlementId.HasValue ? (object)excludeEntitlementId.Value : DBNull.Value;
                return ToDecimal(command.ExecuteScalar());
            }
        }

        private static decimal SumUnpaidReturnBalance(SqlConnection connection, SqlTransaction transaction, int employeeId)
        {
            using (var command = new SqlCommand(@"
SELECT ISNULL(SUM(ISNULL(MoveVacBalance,0)),0)
FROM dbo.TblEmbarkation WITH (NOLOCK)
WHERE Emp_ID = @EmployeeId
  AND TypeVacation = 1
  AND (VacationPaied IS NULL OR VacationPaied = 0);", connection, transaction))
            {
                command.Parameters.Add("@EmployeeId", SqlDbType.Int).Value = employeeId;
                return ToDecimal(command.ExecuteScalar());
            }
        }

        private static decimal GetLastBalanceMonth(SqlConnection connection, SqlTransaction transaction, int employeeId, int excludeEntitlementId)
        {
            decimal balance = 0;
            using (var command = new SqlCommand(@"
SELECT TOP (1) ISNULL(LastBalanceMonth,0)
FROM dbo.TblVocationEntitlements WITH (NOLOCK)
WHERE EmpID = @EmployeeId
  AND ID <> @ExcludeEntitlementId
ORDER BY ID DESC;", connection, transaction))
            {
                command.Parameters.Add("@EmployeeId", SqlDbType.Int).Value = employeeId;
                command.Parameters.Add("@ExcludeEntitlementId", SqlDbType.Int).Value = excludeEntitlementId;
                balance += ToDecimal(command.ExecuteScalar());
            }

            return RoundDays(balance);
        }

        private static decimal SumConsumedPaidVacation(SqlConnection connection, SqlTransaction transaction, int employeeId, DateTime asOfDate, int? excludeVacationId, int? excludeEntitlementId)
        {
            using (var command = new SqlCommand(@"
SELECT ISNULL(SUM(CASE WHEN ISNULL(NoVacation,0) > 0 THEN ISNULL(NoVacation,0) ELSE ISNULL(NoDay,0) END),0)
FROM dbo.TblVocationEntitlements WITH (NOLOCK)
WHERE EmpID = @EmployeeId
  AND ISNULL(Chekk,0) = 0
  AND ISNULL(Flag,0) = 0
  AND ISNULL(NoVacation, ISNULL(NoDay,0)) > 0
  AND ISNULL(RecordDate, ISNULL(stratDate, @AsOfDate)) <= @AsOfDate
  AND (@ExcludeEntitlementId IS NULL OR ID <> @ExcludeEntitlementId)
  AND (@ExcludeVacationId IS NULL OR ISNULL(NoOrder,0) <> @ExcludeVacationId);", connection, transaction))
            {
                command.Parameters.Add("@EmployeeId", SqlDbType.Int).Value = employeeId;
                command.Parameters.Add("@AsOfDate", SqlDbType.DateTime).Value = asOfDate.Date;
                command.Parameters.Add("@ExcludeEntitlementId", SqlDbType.Int).Value = excludeEntitlementId.HasValue ? (object)excludeEntitlementId.Value : DBNull.Value;
                command.Parameters.Add("@ExcludeVacationId", SqlDbType.Int).Value = excludeVacationId.HasValue ? (object)excludeVacationId.Value : DBNull.Value;
                return RoundDays(ToDecimal(command.ExecuteScalar()));
            }
        }

        private static decimal SumPendingApprovedVacationRequests(SqlConnection connection, SqlTransaction transaction, int employeeId, DateTime asOfDate, int? excludeVacationId)
        {
            using (var command = new SqlCommand(@"
SELECT ISNULL(SUM(CASE
    WHEN ISNULL(v.NoVacation,0) > 0 THEN ISNULL(v.NoVacation,0)
    WHEN ISNULL(v.NoDay,0) > 0 THEN ISNULL(v.NoDay,0)
    ELSE DATEDIFF(DAY, v.FromDate, v.ToDate) + 1
END),0)
FROM dbo.TblVocation v WITH (NOLOCK)
WHERE v.EmpID = @EmployeeId
  AND ISNULL(v.WithSalary,0) = 1
  AND (ISNULL(v.Approved,0)=1 OR ISNULL(v.ManagerApprove,0)=1)
  AND ISNULL(v.FlagPayed,0) = 0
  AND v.FromDate <= @AsOfDate
  AND (@ExcludeVacationId IS NULL OR v.ID <> @ExcludeVacationId);", connection, transaction))
            {
                command.Parameters.Add("@EmployeeId", SqlDbType.Int).Value = employeeId;
                command.Parameters.Add("@AsOfDate", SqlDbType.DateTime).Value = asOfDate.Date;
                command.Parameters.Add("@ExcludeVacationId", SqlDbType.Int).Value = excludeVacationId.HasValue ? (object)excludeVacationId.Value : DBNull.Value;
                return RoundDays(ToDecimal(command.ExecuteScalar()));
            }
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

        private static void AddAdvanceMetrics(SqlConnection connection, LegacyHrFinancePageViewModel model, string employeeStatus, int? employeeId, DateTime? dateFrom, DateTime? dateTo, string advanceStatus, string searchText, int? branchId, int? departmentId)
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
  AND (@BranchId IS NULL OR CONVERT(INT, a.Branch_NO) = @BranchId)
  AND (@DepartmentId IS NULL OR CONVERT(INT, a.DeparmentID) = @DepartmentId)
  AND (@DateFrom IS NULL OR a.AdvanceDate >= @DateFrom)
  AND (@DateTo IS NULL OR a.AdvanceDate < DATEADD(DAY, 1, @DateTo))
  AND (
       @AdvanceStatus = N'all'
       OR (@AdvanceStatus = N'draft' AND ISNULL(a.Approved,0)=0 AND a.Posted IS NULL AND ISNULL(a.AccAproved,0)=0 AND ISNULL(a.notok,0)=0)
       OR (@AdvanceStatus = N'pending' AND ISNULL(a.Approved,0)=0 AND a.Posted IS NOT NULL AND ISNULL(a.AccAproved,0)=0 AND ISNULL(a.notok,0)=0)
       OR (@AdvanceStatus = N'approved' AND ISNULL(a.Approved,0)=1)
       OR (@AdvanceStatus = N'posted' AND a.Posted IS NOT NULL)
       OR (@AdvanceStatus = N'accounting-approved' AND ISNULL(a.AccAproved,0)=1)
       OR (@AdvanceStatus = N'rejected' AND ISNULL(a.notok,0)=1)
  )
  AND (@Search = N'' OR ISNULL(e.Emp_Name,N'') LIKE N'%' + @Search + N'%' OR ISNULL(e.Fullcode,N'') LIKE N'%' + @Search + N'%' OR ISNULL(e.Emp_Code,N'') LIKE N'%' + @Search + N'%' OR ISNULL(a.reason,N'') LIKE N'%' + @Search + N'%' OR CONVERT(NVARCHAR(30), a.AdvanceID) = @Search);", connection))
            {
                AddEmployeeStatus(command, employeeStatus);
                command.Parameters.Add("@EmployeeId", SqlDbType.Int).Value = employeeId.HasValue ? (object)employeeId.Value : DBNull.Value;
                command.Parameters.Add("@BranchId", SqlDbType.Int).Value = branchId.HasValue ? (object)branchId.Value : DBNull.Value;
                command.Parameters.Add("@DepartmentId", SqlDbType.Int).Value = departmentId.HasValue ? (object)departmentId.Value : DBNull.Value;
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

        private IList<PayrollComponentEditViewModel> LoadVariableComponents(SqlConnection connection)
        {
            var items = new List<PayrollComponentEditViewModel>();
            using (var command = new SqlCommand(@"
SELECT TOP (200)
       id, name, nameE,
       CASE WHEN ISNULL(AddOrDiscount,0)=0 THEN 1 ELSE 0 END AS AddOrDiscount,
       FixedOrChanged, Unit, Account_Code, Account_code1, ViewComp, Salary, Absence, Late, OverTime, Insurances, Reward, AllowIntrod
FROM dbo.mofrad WITH (NOLOCK)
WHERE ISNULL(FixedOrChanged,0)=1 AND ISNULL(ViewComp,0)=1
ORDER BY name;", connection))
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    items.Add(MapComponent(reader));
                }
            }
            return items;
        }

        private static ChangedComponentEntryViewModel MapChangedComponent(IDataRecord reader)
        {
            var detailCount = ReadInt(reader, "DetailCount");
            var payrollUsed = ReadBool(reader, "PayrollUsed");
            var isBulk = detailCount > 1;
            var payrollRunId = ReadNullableInt(reader, "PayrollRunId");
            var payrollSource = ReadString(reader, "PayrollUsageSource");
            var lockReason = payrollUsed
                ? (payrollRunId.HasValue
                    ? "تم ربط هذه الفترة بمسير رواتب رقم " + payrollRunId.Value.ToString(CultureInfo.InvariantCulture) + "، لذلك لا يمكن التعديل أو الحذف من هذه الشاشة."
                    : "تم استخدام هذا الشهر في مسير أو سند رواتب، لذلك لا يمكن التعديل أو الحذف.")
                : isBulk ? "هذا السجل جزء من إدخال جماعي قديم. يعرض من الويب ولا يعدل إلا من شاشة تفصيل جماعي معتمدة." : string.Empty;

            return new ChangedComponentEntryViewModel
            {
                Id = ReadNullableInt(reader, "DetailId"),
                HeaderId = ReadNullableInt(reader, "ChangedComponentid"),
                EmployeeId = ReadNullableInt(reader, "Emp_id"),
                EmployeeCode = string.IsNullOrWhiteSpace(ReadString(reader, "Fullcode")) ? ReadString(reader, "Emp_Code") : ReadString(reader, "Fullcode"),
                EmployeeName = ReadString(reader, "Emp_Name"),
                BranchId = ReadNullableInt(reader, "EmployeeBranchId"),
                BranchName = ReadString(reader, "BranchName"),
                DepartmentId = ReadNullableInt(reader, "DepartmentID"),
                DepartmentName = ReadString(reader, "DepartmentName"),
                ProjectId = ReadNullableInt(reader, "projectid"),
                ProjectName = ReadString(reader, "Project_name"),
                ComponentId = ReadNullableInt(reader, "ComponentID"),
                ComponentName = ReadString(reader, "ComponentName"),
                AddOrDiscount = ReadBool(reader, "AddOrDiscount"),
                Unit = ReadNullableInt(reader, "Unit"),
                RecordDate = FormatDate(ReadNullableDate(reader, "RecordDate")),
                Year = ReadInt(reader, "Actualyear"),
                Month = ReadInt(reader, "Actualmonth"),
                Value = ReadDecimal(reader, "value"),
                NoOfDays = ReadDecimal(reader, "NoofDays"),
                NoOfHours = ReadDecimal(reader, "NoOfHour"),
                NoOfMinutes = ReadDecimal(reader, "NoOfMinutes"),
                HourRate = ReadDecimal(reader, "HourRate"),
                Salary = ReadDecimal(reader, "Salary"),
                Remarks = ReadString(reader, "Remarks"),
                DetailCount = detailCount,
                PayrollUsed = payrollUsed,
                PayrollRunId = payrollRunId,
                PayrollRunName = ReadString(reader, "PayrollRunName"),
                PayrollRunPosted = ReadBool(reader, "PayrollRunPosted"),
                PayrollUsageSource = string.IsNullOrWhiteSpace(payrollSource) && payrollUsed ? "Legacy payroll tables" : payrollSource,
                IsLegacyBulk = isBulk,
                CanEdit = !payrollUsed && !isBulk,
                CanDelete = !payrollUsed && !isBulk,
                LockReason = lockReason
            };
        }

        private ChangedComponentEntryViewModel LoadChangedComponentByDetailId(SqlConnection connection, SqlTransaction transaction, int detailId)
        {
            var payrollUsageSql = BuildChangedComponentPayrollUsageSql(connection, transaction);
            var payrollRunSql = BuildChangedComponentPayrollRunSql(connection, transaction);
            using (var command = new SqlCommand(@"
SELECT TOP (1)
        d.id AS DetailId, r.ChangedComponentid, d.Emp_id, e.Emp_Code, e.Fullcode, e.Emp_Name,
        e.BranchId AS EmployeeBranchId, b.branch_name AS BranchName, e.DepartmentID, dep.DepartmentName,
        d.projectid, p.Project_name, r.ComponentID, m.name AS ComponentName, CASE WHEN ISNULL(m.AddOrDiscount,0)=0 THEN 1 ELSE 0 END AS AddOrDiscount,
        m.Unit, r.RecordDate, r.Actualyear, r.Actualmonth, d.[value], d.NoofDays, d.NoOfHour, d.NoOfMinutes,
        d.HourRate, d.Salary, d.Remarks, detailCounts.DetailCount,
        CASE WHEN payroll.UsedCount > 0 OR runTrace.PayrollRunId IS NOT NULL THEN 1 ELSE 0 END AS PayrollUsed,
        runTrace.PayrollRunId, runTrace.PayrollRunName, runTrace.PayrollRunPosted, runTrace.PayrollUsageSource
FROM dbo.TblChangedComponentRegisterDetails d WITH (NOLOCK)
INNER JOIN dbo.TblChangedComponentRegister r WITH (NOLOCK) ON r.ChangedComponentid = d.ChangedComponentid
LEFT JOIN dbo.TblEmployee e WITH (NOLOCK) ON e.Emp_ID = d.Emp_id
LEFT JOIN dbo.TblBranchesData b WITH (NOLOCK) ON b.branch_id = ISNULL(NULLIF(r.BranchId,0), e.BranchId)
LEFT JOIN dbo.TblEmpDepartments dep WITH (NOLOCK) ON dep.DeparmentID = e.DepartmentID
LEFT JOIN dbo.projects p WITH (NOLOCK) ON p.id = d.projectid
LEFT JOIN dbo.mofrad m WITH (NOLOCK) ON m.id = r.ComponentID
OUTER APPLY (SELECT COUNT(1) AS DetailCount FROM dbo.TblChangedComponentRegisterDetails x WITH (NOLOCK) WHERE x.ChangedComponentid = r.ChangedComponentid) detailCounts
OUTER APPLY (" + payrollUsageSql + @") payroll
OUTER APPLY (" + payrollRunSql + @") runTrace
WHERE d.id = @DetailId;", connection, transaction))
            {
                command.Parameters.Add("@DetailId", SqlDbType.Int).Value = detailId;
                using (var reader = command.ExecuteReader())
                {
                    return reader.Read() ? MapChangedComponent(reader) : null;
                }
            }
        }

        private static void AddChangedComponentFilters(SqlCommand command, int? employeeId, int? componentId, int? branchId, int? departmentId, DateTime? dateFrom, DateTime? dateTo, int? yearFilter, int? monthFilter, string status, string componentType, string searchText)
        {
            command.Parameters.Add("@EmployeeId", SqlDbType.Int).Value = employeeId.HasValue ? (object)employeeId.Value : DBNull.Value;
            command.Parameters.Add("@ComponentId", SqlDbType.Int).Value = componentId.HasValue ? (object)componentId.Value : DBNull.Value;
            command.Parameters.Add("@BranchId", SqlDbType.Int).Value = branchId.HasValue ? (object)branchId.Value : DBNull.Value;
            command.Parameters.Add("@DepartmentId", SqlDbType.Int).Value = departmentId.HasValue ? (object)departmentId.Value : DBNull.Value;
            command.Parameters.Add("@DateFrom", SqlDbType.DateTime).Value = dateFrom.HasValue ? (object)dateFrom.Value.Date : DBNull.Value;
            command.Parameters.Add("@DateTo", SqlDbType.DateTime).Value = dateTo.HasValue ? (object)dateTo.Value.Date : DBNull.Value;
            command.Parameters.Add("@YearFilter", SqlDbType.Int).Value = yearFilter.HasValue ? (object)yearFilter.Value : DBNull.Value;
            command.Parameters.Add("@MonthFilter", SqlDbType.Int).Value = monthFilter.HasValue ? (object)monthFilter.Value : DBNull.Value;
            command.Parameters.Add("@Status", SqlDbType.NVarChar, 20).Value = NormalizeChangedComponentStatus(status);
            command.Parameters.Add("@ComponentType", SqlDbType.NVarChar, 20).Value = NormalizeChangedComponentType(componentType);
            if (!command.Parameters.Contains("@Search"))
            {
                command.Parameters.Add("@Search", SqlDbType.NVarChar, 100).Value = (searchText ?? string.Empty).Trim();
            }
        }

        private static string NormalizeChangedComponentStatus(string status)
        {
            status = (status ?? "all").Trim().ToLowerInvariant();
            return status == "used" || status == "open" ? status : "all";
        }

        private static string NormalizeChangedComponentType(string componentType)
        {
            componentType = (componentType ?? "all").Trim().ToLowerInvariant();
            return componentType == "addition" || componentType == "deduction" ? componentType : "all";
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
                case "pending":
                case "approved":
                case "posted":
                case "accounting-approved":
                case "rejected":
                    return status;
                default:
                    return "all";
            }
        }

        private static int? GetAdvanceCurrentApprovalId(SqlConnection connection, SqlTransaction transaction, int requestId)
        {
            using (var command = new SqlCommand(@"
SELECT TOP (1) id
FROM dbo.ApprovalData WITH (UPDLOCK, HOLDLOCK)
WHERE ScreenName = N'FrmEmpsAdvanceRequest'
  AND CONVERT(INT, Transaction_ID) = @RequestID
  AND ISNULL(Currcursor,0) = 1
  AND ApprovDate IS NULL
  AND CancelApprove IS NULL
ORDER BY levelorder, currorder, id;", connection, transaction))
            {
                command.Parameters.Add("@RequestID", SqlDbType.Int).Value = requestId;
                var value = command.ExecuteScalar();
                return value == null || value == DBNull.Value ? (int?)null : Convert.ToInt32(value);
            }
        }

        private static int? GetAdvanceNextApprovalId(SqlConnection connection, SqlTransaction transaction, int requestId)
        {
            using (var command = new SqlCommand(@"
SELECT TOP (1) id
FROM dbo.ApprovalData WITH (UPDLOCK, HOLDLOCK)
WHERE ScreenName = N'FrmEmpsAdvanceRequest'
  AND CONVERT(INT, Transaction_ID) = @RequestID
  AND ApprovDate IS NULL
  AND CancelApprove IS NULL
ORDER BY levelorder, currorder, id;", connection, transaction))
            {
                command.Parameters.Add("@RequestID", SqlDbType.Int).Value = requestId;
                var value = command.ExecuteScalar();
                return value == null || value == DBNull.Value ? (int?)null : Convert.ToInt32(value);
            }
        }

        private static bool HasAdvanceApprovalRows(SqlConnection connection, SqlTransaction transaction, int requestId)
        {
            using (var command = new SqlCommand(@"
SELECT COUNT(1)
FROM dbo.ApprovalData WITH (NOLOCK)
WHERE ScreenName = N'FrmEmpsAdvanceRequest'
  AND CONVERT(INT, Transaction_ID) = @RequestID
  AND CancelApprove IS NULL;", connection, transaction))
            {
                command.Parameters.Add("@RequestID", SqlDbType.Int).Value = requestId;
                return Convert.ToInt32(command.ExecuteScalar()) > 0;
            }
        }

        private static string NormalizeVacationStatus(string status)
        {
            status = (status ?? "all").Trim().ToLowerInvariant();
            switch (status)
            {
                case "draft":
                case "pending-manager":
                case "pending-hr":
                case "approved":
                case "rejected":
                case "paid":
                    return status;
                default:
                    return "all";
            }
        }

        private static void AddVacationFilters(SqlCommand command, int? employeeId, DateTime? dateFrom, DateTime? dateTo, string vacationStatus, string vacationType, int? branchId, int? departmentId)
        {
            command.Parameters.Add("@EmployeeId", SqlDbType.Int).Value = employeeId.HasValue ? (object)employeeId.Value : DBNull.Value;
            command.Parameters.Add("@BranchId", SqlDbType.Int).Value = branchId.HasValue ? (object)branchId.Value : DBNull.Value;
            command.Parameters.Add("@DepartmentId", SqlDbType.Int).Value = departmentId.HasValue ? (object)departmentId.Value : DBNull.Value;
            command.Parameters.Add("@DateFrom", SqlDbType.DateTime).Value = dateFrom.HasValue ? (object)dateFrom.Value.Date : DBNull.Value;
            command.Parameters.Add("@DateTo", SqlDbType.DateTime).Value = dateTo.HasValue ? (object)dateTo.Value.Date : DBNull.Value;
            command.Parameters.Add("@VacationStatus", SqlDbType.NVarChar, 30).Value = NormalizeVacationStatus(vacationStatus);
            command.Parameters.Add("@VacationType", SqlDbType.NVarChar, 200).Value = (vacationType ?? string.Empty).Trim();
        }

        private static string FormatIsoDate(DateTime? value)
        {
            return value.HasValue ? value.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : string.Empty;
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

        private ChangedComponentBulkPreviewViewModel BuildChangedComponentBulkPreview(SqlConnection connection, SqlTransaction transaction, ChangedComponentBulkRequestViewModel request)
        {
            var result = new ChangedComponentBulkPreviewViewModel();
            var entries = BuildChangedComponentBulkEntries(connection, transaction, request);
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var rowNo = 0;

            foreach (var entry in entries)
            {
                rowNo++;
                var row = new ChangedComponentBulkPreviewRowViewModel
                {
                    RowNo = rowNo,
                    EmployeeId = entry.EmployeeId,
                    EmployeeCode = entry.EmployeeCode,
                    EmployeeName = entry.EmployeeName,
                    ComponentId = entry.ComponentId,
                    ComponentName = entry.ComponentName,
                    Year = entry.Year,
                    Month = entry.Month,
                    Value = entry.Value
                };

                var message = ValidateChangedComponentCandidate(connection, transaction, entry, seen, row);
                row.IsValid = string.IsNullOrWhiteSpace(message);
                row.Status = row.IsValid ? "جاهز للحفظ" : "مرفوض";
                row.Message = row.IsValid ? "يمر من قواعد التحقق الحالية." : message;
                result.Rows.Add(row);
                if (row.IsValid) { result.Entries.Add(entry); }
            }

            result.TotalRows = result.Rows.Count;
            result.ValidRows = result.Rows.Count(x => x.IsValid);
            result.InvalidRows = result.TotalRows - result.ValidRows;
            result.TotalValue = result.Rows.Where(x => x.IsValid).Sum(x => x.Value);
            result.Success = result.TotalRows > 0;
            result.Message = result.TotalRows == 0
                ? "لا توجد سطور للمعاينة."
                : "تمت معاينة " + result.TotalRows.ToString(CultureInfo.InvariantCulture) + " سطر. الصالح: " + result.ValidRows.ToString(CultureInfo.InvariantCulture) + "، المرفوض: " + result.InvalidRows.ToString(CultureInfo.InvariantCulture) + ".";
            return result;
        }

        private IList<ChangedComponentEntryViewModel> BuildChangedComponentBulkEntries(SqlConnection connection, SqlTransaction transaction, ChangedComponentBulkRequestViewModel request)
        {
            request = request ?? new ChangedComponentBulkRequestViewModel();
            var mode = (request.Mode ?? "bulk").Trim().ToLowerInvariant();
            if (mode == "copy")
            {
                return LoadChangedComponentCopyEntries(connection, transaction, request);
            }

            var entries = new List<ChangedComponentEntryViewModel>();
            var seenTokens = new HashSet<int>();
            foreach (var token in SplitEmployeeTokens(request.EmployeeTokens))
            {
                var employee = FindEmployeeByToken(connection, transaction, token);
                if (employee == null)
                {
                    entries.Add(new ChangedComponentEntryViewModel
                    {
                        EmployeeCode = token,
                        ComponentId = request.ComponentId,
                        RecordDate = NormalizeChangedRecordDate(request.RecordDate, request.Year, request.Month),
                        Year = request.Year,
                        Month = request.Month,
                        Value = request.Value,
                        NoOfDays = request.NoOfDays,
                        NoOfHours = request.NoOfHours,
                        NoOfMinutes = request.NoOfMinutes,
                        HourRate = request.HourRate,
                        Salary = request.Salary,
                        ProjectId = request.ProjectId,
                        Remarks = request.Remarks
                    });
                    continue;
                }

                if (!seenTokens.Add(employee.Id)) { continue; }
                entries.Add(new ChangedComponentEntryViewModel
                {
                    EmployeeId = employee.Id,
                    ComponentId = request.ComponentId,
                    RecordDate = NormalizeChangedRecordDate(request.RecordDate, request.Year, request.Month),
                    Year = request.Year,
                    Month = request.Month,
                    Value = request.Value,
                    NoOfDays = request.NoOfDays,
                    NoOfHours = request.NoOfHours,
                    NoOfMinutes = request.NoOfMinutes,
                    HourRate = request.HourRate,
                    Salary = request.Salary,
                    ProjectId = request.ProjectId,
                    Remarks = request.Remarks
                });
            }

            return entries;
        }

        private IList<ChangedComponentEntryViewModel> LoadChangedComponentCopyEntries(SqlConnection connection, SqlTransaction transaction, ChangedComponentBulkRequestViewModel request)
        {
            var entries = new List<ChangedComponentEntryViewModel>();
            if (!request.SourceYear.HasValue || !request.SourceMonth.HasValue || request.Year <= 0 || request.Month <= 0)
            {
                return entries;
            }

            var employeeIds = new List<int>();
            var missingTokens = new List<string>();
            foreach (var token in SplitEmployeeTokens(request.EmployeeTokens))
            {
                var employee = FindEmployeeByToken(connection, transaction, token);
                if (employee == null) { missingTokens.Add(token); }
                else if (!employeeIds.Contains(employee.Id)) { employeeIds.Add(employee.Id); }
            }

            if (missingTokens.Count > 0)
            {
                foreach (var token in missingTokens)
                {
                    entries.Add(new ChangedComponentEntryViewModel
                    {
                        EmployeeCode = token,
                        ComponentId = request.SourceComponentId,
                        RecordDate = NormalizeChangedRecordDate(request.RecordDate, request.Year, request.Month),
                        Year = request.Year,
                        Month = request.Month,
                        Value = 0,
                        Remarks = "موظف غير معروف في طلب النسخ."
                    });
                }
                return entries;
            }

            using (var command = new SqlCommand(@"
SELECT TOP (500)
       d.Emp_id, r.ComponentID, d.[value], d.NoofDays, d.NoOfHour, d.NoOfMinutes, d.HourRate, d.Salary, d.projectid, d.Remarks
FROM dbo.TblChangedComponentRegister r WITH (NOLOCK)
INNER JOIN dbo.TblChangedComponentRegisterDetails d WITH (NOLOCK) ON d.ChangedComponentid = r.ChangedComponentid
INNER JOIN dbo.TblEmployee e WITH (NOLOCK) ON e.Emp_ID = d.Emp_id
INNER JOIN dbo.mofrad m WITH (NOLOCK) ON m.id = r.ComponentID
WHERE r.Actualyear = @SourceYear
  AND r.Actualmonth = @SourceMonth
  AND ISNULL(m.FixedOrChanged,0)=1
  AND ISNULL(m.ViewComp,0)=1
  AND ISNULL(e.chkStop,0)=0
  AND ISNULL(e.workstate,0)=1
  AND (@SourceComponentId IS NULL OR r.ComponentID = @SourceComponentId)
  AND (@HasEmployees = 0 OR d.Emp_id IN (" + BuildInList(employeeIds, "Emp") + @"))
ORDER BY d.Emp_id, r.ComponentID, d.id;", connection, transaction))
            {
                command.Parameters.Add("@SourceYear", SqlDbType.Int).Value = request.SourceYear.Value;
                command.Parameters.Add("@SourceMonth", SqlDbType.Int).Value = request.SourceMonth.Value;
                command.Parameters.Add("@SourceComponentId", SqlDbType.Int).Value = request.SourceComponentId.HasValue && request.SourceComponentId.Value > 0 ? (object)request.SourceComponentId.Value : DBNull.Value;
                command.Parameters.Add("@HasEmployees", SqlDbType.Bit).Value = employeeIds.Count > 0;
                AddInListParameters(command, employeeIds, "Emp");
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        entries.Add(new ChangedComponentEntryViewModel
                        {
                            EmployeeId = ReadNullableInt(reader, "Emp_id"),
                            ComponentId = ReadNullableInt(reader, "ComponentID"),
                            RecordDate = NormalizeChangedRecordDate(request.RecordDate, request.Year, request.Month),
                            Year = request.Year,
                            Month = request.Month,
                            Value = ReadDecimal(reader, "value"),
                            NoOfDays = ReadDecimal(reader, "NoofDays"),
                            NoOfHours = ReadDecimal(reader, "NoOfHour"),
                            NoOfMinutes = ReadDecimal(reader, "NoOfMinutes"),
                            HourRate = ReadDecimal(reader, "HourRate"),
                            Salary = ReadDecimal(reader, "Salary"),
                            ProjectId = ReadNullableInt(reader, "projectid"),
                            Remarks = string.IsNullOrWhiteSpace(request.Remarks) ? ReadString(reader, "Remarks") : request.Remarks
                        });
                    }
                }
            }

            return entries;
        }

        private string ValidateChangedComponentCandidate(SqlConnection connection, SqlTransaction transaction, ChangedComponentEntryViewModel entry, ISet<string> seen, ChangedComponentBulkPreviewRowViewModel row)
        {
            var validation = ValidateChangedComponent(entry);
            if (!validation.Success) { return validation.Message; }

            var employee = GetEmployee(connection, transaction, entry.EmployeeId.GetValueOrDefault(), true);
            if (employee == null) { return "الموظف غير موجود أو موقوف."; }
            row.EmployeeCode = employee.Code;
            row.EmployeeName = employee.Name;

            var component = GetComponent(connection, transaction, entry.ComponentId.GetValueOrDefault());
            if (component == null || !component.FixedOrChanged || !component.ViewComponent) { return "المفردة غير متغيرة أو غير ظاهرة في المسير."; }
            row.ComponentName = component.Name;
            row.ComponentType = component.AddOrDiscount ? "إضافة" : "خصم";

            validation = ValidateChangedComponentForComponent(entry, component);
            if (!validation.Success) { return validation.Message; }

            var key = employee.Id.ToString(CultureInfo.InvariantCulture) + "|" + entry.ComponentId.GetValueOrDefault().ToString(CultureInfo.InvariantCulture) + "|" + entry.Year.ToString(CultureInfo.InvariantCulture) + "|" + entry.Month.ToString(CultureInfo.InvariantCulture);
            if (!seen.Add(key)) { return "السطر مكرر داخل نفس الدفعة."; }
            if (ChangedComponentDuplicateExists(connection, transaction, 0, employee.Id, entry.ComponentId.GetValueOrDefault(), entry.Year, entry.Month)) { return "توجد مفردة مسجلة مسبقاً لنفس الموظف والشهر والمفردة."; }
            if (ChangedComponentPeriodLocked(connection, transaction, entry.Year, entry.Month, employee.BranchId)) { return "الفترة مستخدمة في مسير الرواتب أو سند رواتب."; }

            return string.Empty;
        }

        private IList<EnterpriseHrEmployeeLookupViewModel> ResolveChangedComponentEmployees(SqlConnection connection, SqlTransaction transaction, string tokensText)
        {
            var employees = new List<EnterpriseHrEmployeeLookupViewModel>();
            var seen = new HashSet<int>();
            foreach (var token in SplitEmployeeTokens(tokensText))
            {
                var employee = FindEmployeeByToken(connection, transaction, token);
                if (employee != null && seen.Add(employee.Id))
                {
                    employees.Add(employee);
                }
            }

            return employees;
        }

        private EnterpriseHrEmployeeLookupViewModel FindEmployeeByToken(SqlConnection connection, SqlTransaction transaction, string token)
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
WHERE (CONVERT(NVARCHAR(30), e.Emp_ID) = @Token OR ISNULL(e.Emp_Code,N'') = @Token OR ISNULL(e.Fullcode,N'') = @Token)
  AND ISNULL(e.chkStop,0)=0
  AND ISNULL(e.workstate,0)=1
ORDER BY e.Emp_ID;", connection, transaction))
            {
                command.Parameters.Add("@Token", SqlDbType.NVarChar, 100).Value = token;
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

        private static IEnumerable<string> SplitEmployeeTokens(string text)
        {
            return (text ?? string.Empty)
                .Split(new[] { ',', ';', '\n', '\r', '\t', ' ', '،' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(x => x.Length > 0)
                .Take(500);
        }

        private static string NormalizeChangedRecordDate(string recordDate, int year, int month)
        {
            var parsed = ParseDate(recordDate);
            if (parsed.HasValue) { return parsed.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture); }
            if (year >= 2006 && month >= 1 && month <= 12) { return new DateTime(year, month, 1).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture); }
            return string.Empty;
        }

        private static string BuildInList(IList<int> values, string prefix)
        {
            if (values == null || values.Count == 0) { return "NULL"; }
            return string.Join(",", values.Select((x, i) => "@" + prefix + i.ToString(CultureInfo.InvariantCulture)));
        }

        private static void AddInListParameters(SqlCommand command, IList<int> values, string prefix)
        {
            if (values == null) { return; }
            for (var i = 0; i < values.Count; i++)
            {
                command.Parameters.Add("@" + prefix + i.ToString(CultureInfo.InvariantCulture), SqlDbType.Int).Value = values[i];
            }
        }

        private static void AddChangedHeaderParameters(SqlCommand command, ChangedComponentEntryViewModel request, EnterpriseHrEmployeeLookupViewModel employee, PayrollComponentEditViewModel component, int headerId, DateTime recordDate)
        {
            command.Parameters.Add("@HeaderId", SqlDbType.Int).Value = headerId;
            command.Parameters.Add("@RecordDate", SqlDbType.DateTime).Value = recordDate;
            command.Parameters.Add("@YearIndex", SqlDbType.Int).Value = request.Year - 2006;
            command.Parameters.Add("@MonthIndex", SqlDbType.Int).Value = request.Month - 1;
            command.Parameters.Add("@Year", SqlDbType.Int).Value = request.Year;
            command.Parameters.Add("@Month", SqlDbType.Int).Value = request.Month;
            command.Parameters.Add("@ComponentId", SqlDbType.Int).Value = request.ComponentId.GetValueOrDefault();
            command.Parameters.Add("@EmployeeId", SqlDbType.Int).Value = employee.Id;
            command.Parameters.Add("@BranchId", SqlDbType.Int).Value = employee.BranchId.HasValue ? (object)employee.BranchId.Value : DBNull.Value;
            command.Parameters.Add("@DepartmentId", SqlDbType.Int).Value = employee.DepartmentId.HasValue ? (object)employee.DepartmentId.Value : DBNull.Value;
            command.Parameters.Add("@ProjectId", SqlDbType.Int).Value = request.ProjectId.HasValue && request.ProjectId.Value > 0 ? (object)request.ProjectId.Value : DBNull.Value;
            command.Parameters.Add("@Unit", SqlDbType.Int).Value = component.Unit.HasValue ? (object)component.Unit.Value : 0;
            command.Parameters.Add("@Remarks", SqlDbType.NVarChar, 800).Value = DbText(request.Remarks);
        }

        private static void AddChangedDetailParameters(SqlCommand command, ChangedComponentEntryViewModel request, EnterpriseHrEmployeeLookupViewModel employee, int detailId, int headerId)
        {
            command.Parameters.Add("@DetailId", SqlDbType.Int).Value = detailId;
            command.Parameters.Add("@HeaderId", SqlDbType.Int).Value = headerId;
            command.Parameters.Add("@EmployeeId", SqlDbType.Int).Value = employee.Id;
            command.Parameters.Add("@Remarks", SqlDbType.NVarChar, 510).Value = DbText(request.Remarks);
            command.Parameters.Add("@Value", SqlDbType.Money).Value = request.Value;
            command.Parameters.Add("@HourRate", SqlDbType.Float).Value = (double)request.HourRate;
            command.Parameters.Add("@NoOfHours", SqlDbType.Float).Value = (double)request.NoOfHours;
            command.Parameters.Add("@NoOfMinutes", SqlDbType.Float).Value = (double)request.NoOfMinutes;
            command.Parameters.Add("@NoOfDays", SqlDbType.Float).Value = (double)request.NoOfDays;
            command.Parameters.Add("@Salary", SqlDbType.Float).Value = request.Salary > 0 ? (double)request.Salary : (double)employee.BasicSalary;
            command.Parameters.Add("@ProjectId", SqlDbType.Int).Value = request.ProjectId.HasValue && request.ProjectId.Value > 0 ? (object)request.ProjectId.Value : DBNull.Value;
        }

        private static LegacyHrFinanceSaveResult ValidateChangedComponent(ChangedComponentEntryViewModel request)
        {
            if (request == null) { return Fail("بيانات المفردة المتغيرة غير مكتملة."); }
            if (!request.EmployeeId.HasValue || request.EmployeeId.Value <= 0) { return Fail("يجب اختيار الموظف."); }
            if (!request.ComponentId.HasValue || request.ComponentId.Value <= 0) { return Fail("يجب اختيار المفردة."); }
            if (request.Year < 2006 || request.Year > 3000) { return Fail("سنة المسير غير صحيحة."); }
            if (request.Month < 1 || request.Month > 12) { return Fail("شهر المسير غير صحيح."); }
            if (!ParseDate(request.RecordDate).HasValue) { return Fail("تاريخ التسجيل غير صحيح."); }
            if (request.Value <= 0) { return Fail("يجب إدخال قيمة أكبر من صفر."); }
            if (request.Value > 999999999) { return Fail("قيمة المفردة كبيرة بشكل غير منطقي. راجع المبلغ قبل الحفظ."); }
            if (request.NoOfDays < 0 || request.NoOfHours < 0 || request.NoOfMinutes < 0 || request.HourRate < 0 || request.Salary < 0) { return Fail("بيانات القياس لا تقبل قيماً سالبة."); }
            if (request.NoOfMinutes >= 60) { return Fail("الدقائق يجب أن تكون أقل من 60."); }
            if ((request.Remarks ?? string.Empty).Length > 510) { return Fail("الملاحظات طويلة جداً. الحد الأقصى 510 حرف."); }
            return new LegacyHrFinanceSaveResult { Success = true };
        }

        private static LegacyHrFinanceSaveResult ValidateChangedComponentForComponent(ChangedComponentEntryViewModel request, PayrollComponentEditViewModel component)
        {
            var unit = component.Unit.GetValueOrDefault(0);
            if (unit == 1 && request.NoOfHours > 0)
            {
                return Fail("هذه المفردة محسوبة بالأيام. لا تسجل ساعات عليها.");
            }

            if (unit == 2 && request.NoOfDays > 0)
            {
                return Fail("هذه المفردة محسوبة بالساعات. لا تسجل أيام عليها.");
            }

            if (unit != 1 && unit != 2 && (request.NoOfDays > 0 || request.NoOfHours > 0 || request.NoOfMinutes > 0 || request.HourRate > 0))
            {
                return Fail("هذه المفردة قيمتها مباشرة. لا تحتاج أيام أو ساعات أو معدل.");
            }

            return new LegacyHrFinanceSaveResult { Success = true };
        }

        private static bool ChangedComponentDuplicateExists(SqlConnection connection, SqlTransaction transaction, int currentDetailId, int employeeId, int componentId, int year, int month)
        {
            using (var command = new SqlCommand(@"
SELECT TOP (1) 1
FROM dbo.TblChangedComponentRegister r
INNER JOIN dbo.TblChangedComponentRegisterDetails d ON d.ChangedComponentid = r.ChangedComponentid
WHERE d.Emp_id = @EmployeeId
  AND r.ComponentID = @ComponentId
  AND r.Actualyear = @Year
  AND r.Actualmonth = @Month
  AND d.id <> @DetailId;", connection, transaction))
            {
                command.Parameters.Add("@EmployeeId", SqlDbType.Int).Value = employeeId;
                command.Parameters.Add("@ComponentId", SqlDbType.Int).Value = componentId;
                command.Parameters.Add("@Year", SqlDbType.Int).Value = year;
                command.Parameters.Add("@Month", SqlDbType.Int).Value = month;
                command.Parameters.Add("@DetailId", SqlDbType.Int).Value = currentDetailId;
                return command.ExecuteScalar() != null;
            }
        }

        private static bool ChangedComponentPeriodLocked(SqlConnection connection, SqlTransaction transaction, int year, int month, int? branchId)
        {
            var payrollUsageSql = BuildChangedComponentPayrollUsageSql(connection, transaction);
            using (var command = new SqlCommand(@"
SELECT payroll.UsedCount
FROM (SELECT @Year AS Actualyear, @Month AS Actualmonth, @BranchId AS BranchId) r
OUTER APPLY (" + payrollUsageSql + @") payroll;", connection, transaction))
            {
                command.Parameters.Add("@Year", SqlDbType.Int).Value = year;
                command.Parameters.Add("@Month", SqlDbType.Int).Value = month;
                command.Parameters.Add("@BranchId", SqlDbType.Int).Value = branchId.HasValue ? (object)branchId.Value : DBNull.Value;
                var value = command.ExecuteScalar();
                if (value != null && value != DBNull.Value && Convert.ToInt32(value) > 0) { return true; }
            }

            if (!TableExists(connection, transaction, "PayrollRunHeader")
                || !ColumnExists(connection, transaction, "PayrollRunHeader", "PeriodYear")
                || !ColumnExists(connection, transaction, "PayrollRunHeader", "PeriodMonth"))
            {
                return false;
            }

            var branchPredicate = ColumnExists(connection, transaction, "PayrollRunHeader", "BranchId")
                ? " AND (@BranchId IS NULL OR h.BranchId IS NULL OR h.BranchId = @BranchId)"
                : string.Empty;
            var cancelledPredicate = ColumnExists(connection, transaction, "PayrollRunHeader", "IsCancelled")
                ? " AND ISNULL(h.IsCancelled, 0) = 0"
                : string.Empty;

            using (var command = new SqlCommand(@"
SELECT TOP (1) 1
FROM dbo.PayrollRunHeader h WITH (NOLOCK)
WHERE h.PeriodYear = @Year
  AND h.PeriodMonth = @Month" + branchPredicate + cancelledPredicate + ";", connection, transaction))
            {
                command.Parameters.Add("@Year", SqlDbType.Int).Value = year;
                command.Parameters.Add("@Month", SqlDbType.Int).Value = month;
                command.Parameters.Add("@BranchId", SqlDbType.Int).Value = branchId.HasValue ? (object)branchId.Value : DBNull.Value;
                return command.ExecuteScalar() != null;
            }
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

        private static int Scalar(SqlConnection connection, SqlTransaction transaction, string sql, params SqlParameter[] parameters)
        {
            using (var command = new SqlCommand(sql, connection, transaction))
            {
                if (parameters != null && parameters.Length > 0)
                {
                    command.Parameters.AddRange(parameters);
                }

                var value = command.ExecuteScalar();
                return value == null || value == DBNull.Value ? 0 : Convert.ToInt32(value);
            }
        }

        private static bool TableExists(SqlConnection connection, SqlTransaction transaction, string tableName)
        {
            using (var command = new SqlCommand("SELECT OBJECT_ID(N'dbo.' + @TableName, N'U')", connection, transaction))
            {
                command.Parameters.Add("@TableName", SqlDbType.NVarChar, 128).Value = tableName;
                var value = command.ExecuteScalar();
                return value != null && value != DBNull.Value;
            }
        }

        private static bool ColumnExists(SqlConnection connection, SqlTransaction transaction, string tableName, string columnName)
        {
            using (var command = new SqlCommand(@"
SELECT 1
FROM sys.columns c
WHERE c.object_id = OBJECT_ID(N'dbo.' + @TableName)
  AND c.name = @ColumnName;", connection, transaction))
            {
                command.Parameters.Add("@TableName", SqlDbType.NVarChar, 128).Value = tableName;
                command.Parameters.Add("@ColumnName", SqlDbType.NVarChar, 128).Value = columnName;
                return command.ExecuteScalar() != null;
            }
        }

        private static string BuildChangedComponentPayrollUsageSql(SqlConnection connection, SqlTransaction transaction)
        {
            var checks = new List<string>();

            if (TableExists(connection, transaction, "salary_voucher")
                && ColumnExists(connection, transaction, "salary_voucher", "m_year")
                && ColumnExists(connection, transaction, "salary_voucher", "m_month"))
            {
                checks.Add(@"(SELECT COUNT(1)
FROM dbo.salary_voucher sv WITH (NOLOCK)
WHERE CONVERT(NVARCHAR(20), sv.m_year) = CONVERT(NVARCHAR(20), r.Actualyear)
  AND CONVERT(NVARCHAR(20), sv.m_month) = CONVERT(NVARCHAR(20), r.Actualmonth))");
            }

            if (TableExists(connection, transaction, "Notes"))
            {
                var notePredicates = new List<string>();
                if (ColumnExists(connection, transaction, "Notes", "PayrollYear")
                    && ColumnExists(connection, transaction, "Notes", "PayrollMonth"))
                {
                    notePredicates.Add("(n.PayrollYear = r.Actualyear AND n.PayrollMonth = r.Actualmonth)");
                }

                if (ColumnExists(connection, transaction, "Notes", "salary"))
                {
                    notePredicates.Add("(CONVERT(NVARCHAR(30), n.salary) = CONVERT(NVARCHAR(10), r.Actualyear) + CONVERT(NVARCHAR(10), r.Actualmonth))");
                }

                if (notePredicates.Count > 0)
                {
                    checks.Add(@"(SELECT COUNT(1)
FROM dbo.Notes n WITH (NOLOCK)
WHERE " + string.Join(" OR ", notePredicates) + ")");
                }
            }

            if (TableExists(connection, transaction, "emp_salary"))
            {
                var periodPredicates = new List<string>();
                if (ColumnExists(connection, transaction, "emp_salary", "m_year")
                    && ColumnExists(connection, transaction, "emp_salary", "m_month"))
                {
                    periodPredicates.Add("(CONVERT(NVARCHAR(20), es.m_year) = CONVERT(NVARCHAR(20), r.Actualyear) AND CONVERT(NVARCHAR(20), es.m_month) = CONVERT(NVARCHAR(20), r.Actualmonth))");
                }

                if (ColumnExists(connection, transaction, "emp_salary", "RecordDate"))
                {
                    periodPredicates.Add("(YEAR(es.RecordDate) = r.Actualyear AND MONTH(es.RecordDate) = r.Actualmonth)");
                }

                if (periodPredicates.Count > 0)
                {
                    var branchPredicate = ColumnExists(connection, transaction, "emp_salary", "BranchId")
                        ? " AND (r.BranchId IS NULL OR r.BranchId = 0 OR es.BranchId = r.BranchId)"
                        : string.Empty;

                    checks.Add(@"(SELECT COUNT(1)
FROM dbo.emp_salary es WITH (NOLOCK)
WHERE (" + string.Join(" OR ", periodPredicates) + ")" + branchPredicate + ")");
                }
            }

            if (checks.Count == 0)
            {
                return "SELECT 0 AS UsedCount";
            }

            return "SELECT " + string.Join(" + ", checks) + " AS UsedCount";
        }

        private static string BuildChangedComponentPayrollRunSql(SqlConnection connection, SqlTransaction transaction)
        {
            if (!TableExists(connection, transaction, "PayrollRunHeader")
                || !TableExists(connection, transaction, "PayrollRunEmployees")
                || !ColumnExists(connection, transaction, "PayrollRunHeader", "PayrollRunId")
                || !ColumnExists(connection, transaction, "PayrollRunHeader", "PeriodYear")
                || !ColumnExists(connection, transaction, "PayrollRunHeader", "PeriodMonth")
                || !ColumnExists(connection, transaction, "PayrollRunEmployees", "PayrollRunId")
                || !ColumnExists(connection, transaction, "PayrollRunEmployees", "EmployeeId"))
            {
                return @"SELECT CONVERT(INT, NULL) AS PayrollRunId,
       CONVERT(NVARCHAR(200), NULL) AS PayrollRunName,
       CONVERT(BIT, 0) AS PayrollRunPosted,
       CONVERT(NVARCHAR(80), NULL) AS PayrollUsageSource";
            }

            var runNameSelect = ColumnExists(connection, transaction, "PayrollRunHeader", "RunName")
                ? "h.RunName"
                : "N'مسير رواتب ' + CONVERT(NVARCHAR(20), h.PeriodMonth) + N'/' + CONVERT(NVARCHAR(20), h.PeriodYear)";
            var postedSelect = ColumnExists(connection, transaction, "PayrollRunHeader", "IsPosted")
                ? "ISNULL(h.IsPosted, 0)"
                : "CONVERT(BIT, 0)";
            var cancelledPredicate = ColumnExists(connection, transaction, "PayrollRunHeader", "IsCancelled")
                ? " AND ISNULL(h.IsCancelled, 0) = 0"
                : string.Empty;
            var branchPredicate = ColumnExists(connection, transaction, "PayrollRunHeader", "BranchId")
                ? " AND (h.BranchId IS NULL OR r.BranchId IS NULL OR r.BranchId = 0 OR h.BranchId = r.BranchId)"
                : string.Empty;

            return @"
SELECT TOP (1)
       h.PayrollRunId,
       " + runNameSelect + @" AS PayrollRunName,
       " + postedSelect + @" AS PayrollRunPosted,
       N'PayrollRun' AS PayrollUsageSource
FROM dbo.PayrollRunHeader h WITH (NOLOCK)
INNER JOIN dbo.PayrollRunEmployees pe WITH (NOLOCK) ON pe.PayrollRunId = h.PayrollRunId
WHERE h.PeriodYear = r.Actualyear
  AND h.PeriodMonth = r.Actualmonth
  AND pe.EmployeeId = d.Emp_id" + cancelledPredicate + branchPredicate + @"
ORDER BY " + postedSelect + @" DESC, h.PayrollRunId DESC";
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

        private static string ToHijriDate(DateTime value)
        {
            try
            {
                var calendar = new UmAlQuraCalendar();
                return calendar.GetYear(value).ToString("0000", CultureInfo.InvariantCulture) + "/"
                    + calendar.GetMonth(value).ToString("00", CultureInfo.InvariantCulture) + "/"
                    + calendar.GetDayOfMonth(value).ToString("00", CultureInfo.InvariantCulture);
            }
            catch
            {
                return value.ToString("yyyy/MM/dd", CultureInfo.InvariantCulture);
            }
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

        private static DateTime? ReadNullableDate(IDataRecord reader, string column)
        {
            var value = reader[column];
            return value == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(value);
        }

        private static decimal ToDecimal(object value)
        {
            return value == null || value == DBNull.Value ? 0 : Convert.ToDecimal(value);
        }

        private static decimal RoundDays(decimal value)
        {
            return Math.Round(value, 2, MidpointRounding.AwayFromZero);
        }

        private static string ReadDisplayDate(IDataRecord reader, string column)
        {
            var value = reader[column];
            if (value == DBNull.Value) { return string.Empty; }
            DateTime date;
            return DateTime.TryParse(Convert.ToString(value), out date) ? date.ToString("yyyy/MM/dd") : Convert.ToString(value);
        }

        private static string ReadDisplayDateTime(IDataRecord reader, string column)
        {
            var value = reader[column];
            if (value == DBNull.Value) { return string.Empty; }
            DateTime date;
            return DateTime.TryParse(Convert.ToString(value), out date) ? date.ToString("yyyy/MM/dd HH:mm") : Convert.ToString(value);
        }
    }
}
