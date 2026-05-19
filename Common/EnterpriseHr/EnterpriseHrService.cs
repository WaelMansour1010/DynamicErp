using MyERP.Common.EnterpriseHr;

namespace MyERP.Common.EnterpriseHr
{
    public class EnterpriseHrService
    {
        private readonly EnterpriseHrRepository _repository;

        public EnterpriseHrService(EnterpriseHrRepository repository)
        {
            _repository = repository;
        }

        public LegacyHrFinancePageViewModel Load(string moduleKey, string searchText, int page, int pageSize, string employeeStatus = "active", int? employeeId = null, System.DateTime? dateFrom = null, System.DateTime? dateTo = null, string advanceStatus = null, string vacationStatus = null, string vacationType = null, int? componentId = null, int? branchId = null, int? departmentId = null, int? yearFilter = null, int? monthFilter = null, string componentType = null)
        {
            return _repository.Load(moduleKey, searchText, page, pageSize, employeeStatus, employeeId, dateFrom, dateTo, advanceStatus, vacationStatus, vacationType, componentId, branchId, departmentId, yearFilter, monthFilter, componentType);
        }

        public PayrollComponentEditViewModel GetComponent(int id)
        {
            return _repository.GetComponent(id);
        }

        public LegacyHrFinanceSaveResult SaveComponent(PayrollComponentEditViewModel request)
        {
            return _repository.SaveComponent(request);
        }

        public ChangedComponentEntryViewModel GetChangedComponent(int detailId)
        {
            return _repository.GetChangedComponent(detailId);
        }

        public LegacyHrFinanceSaveResult SaveChangedComponent(ChangedComponentEntryViewModel request, int? userId)
        {
            return _repository.SaveChangedComponent(request, userId);
        }

        public ChangedComponentBulkPreviewViewModel PreviewChangedComponentBulk(ChangedComponentBulkRequestViewModel request)
        {
            return _repository.PreviewChangedComponentBulk(request);
        }

        public LegacyHrFinanceSaveResult SaveChangedComponentBulk(ChangedComponentBulkRequestViewModel request, int? userId)
        {
            return _repository.SaveChangedComponentBulk(request, userId);
        }

        public LegacyHrFinanceSaveResult DeleteChangedComponent(int detailId)
        {
            return _repository.DeleteChangedComponent(detailId);
        }

        public EmployeeAdvanceViewModel GetAdvance(int id)
        {
            return _repository.GetAdvance(id);
        }

        public AdvanceAccountingBoundaryViewModel GetAdvanceAccountingBoundary(int requestId)
        {
            return _repository.GetAdvanceAccountingBoundary(requestId);
        }

        public VacationBalanceViewModel CalculateVacationBalance(VacationBalanceRequestViewModel request)
        {
            return _repository.CalculateVacationBalance(request);
        }

        public EmployeeVacationRequestViewModel GetVacation(int id)
        {
            return _repository.GetVacation(id);
        }

        public LegacyHrFinanceSaveResult SaveVacation(EmployeeVacationRequestViewModel request, int? userId)
        {
            return _repository.SaveVacation(request, userId);
        }

        public LegacyHrFinanceSaveResult ManagerApproveVacation(int id, int? userId, string userName, string remarks)
        {
            return _repository.ManagerApproveVacation(id, userId, userName, remarks);
        }

        public LegacyHrFinanceSaveResult HrApproveVacation(int id, int? userId, string userName, string remarks)
        {
            return _repository.HrApproveVacation(id, userId, userName, remarks);
        }

        public LegacyHrFinanceSaveResult RejectVacation(int id, int? userId, string userName, string remarks)
        {
            return _repository.RejectVacation(id, userId, userName, remarks);
        }

        public LegacyHrFinanceSaveResult CancelVacation(int id, int? userId, string userName, string remarks)
        {
            return _repository.CancelVacation(id, userId, userName, remarks);
        }

        public LegacyHrFinanceSaveResult DeleteVacation(int id)
        {
            return _repository.DeleteVacation(id);
        }

        public LegacyHrFinanceSaveResult CreateVacationEntitlementFromRequest(int vacationId, int? userId)
        {
            return _repository.CreateVacationEntitlementFromRequest(vacationId, userId);
        }

        public LegacyHrFinanceSaveResult DeleteVacationEntitlement(int entitlementId, int? userId)
        {
            return _repository.DeleteVacationEntitlement(entitlementId, userId);
        }

        public LegacyHrFinanceSaveResult SaveVacationReturnToWork(VacationReturnToWorkViewModel request, int? userId)
        {
            return _repository.SaveVacationReturnToWork(request, userId);
        }

        public LegacyHrFinanceSaveResult DeleteVacationReturnToWork(int entitlementId)
        {
            return _repository.DeleteVacationReturnToWork(entitlementId);
        }

        public LegacyHrFinanceSaveResult SaveAdvance(EmployeeAdvanceViewModel request, int? userId)
        {
            return _repository.SaveAdvance(request, userId);
        }

        public LegacyHrFinanceSaveResult DeleteAdvance(int id)
        {
            return _repository.DeleteAdvance(id);
        }

        public LegacyHrFinanceSaveResult DisburseAdvanceRequest(int requestId, int? userId)
        {
            return _repository.DisburseAdvanceRequest(requestId, userId);
        }

        public LegacyHrFinanceSaveResult SendAdvanceForApproval(int requestId, int? userId, string userName, string remarks)
        {
            return _repository.SendAdvanceForApproval(requestId, userId, userName, remarks);
        }

        public LegacyHrFinanceSaveResult ApproveAdvanceRequest(int requestId, int? userId, string userName, string remarks)
        {
            return _repository.ApproveAdvanceRequest(requestId, userId, userName, remarks);
        }

        public LegacyHrFinanceSaveResult CancelAdvanceRequest(int requestId, int? userId, string userName, string remarks)
        {
            return _repository.CancelAdvanceRequest(requestId, userId, userName, remarks);
        }

        public System.Collections.Generic.IList<EnterpriseHrEmployeeLookupViewModel> SearchEmployees(string searchText, string employeeStatus, int take)
        {
            return _repository.SearchEmployees(searchText, employeeStatus, take);
        }
    }
}
