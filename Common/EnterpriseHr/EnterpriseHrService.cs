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

        public LegacyHrFinancePageViewModel Load(string moduleKey, string searchText, int page, int pageSize, string employeeStatus = "active", int? employeeId = null, System.DateTime? dateFrom = null, System.DateTime? dateTo = null, string advanceStatus = null)
        {
            return _repository.Load(moduleKey, searchText, page, pageSize, employeeStatus, employeeId, dateFrom, dateTo, advanceStatus);
        }

        public PayrollComponentEditViewModel GetComponent(int id)
        {
            return _repository.GetComponent(id);
        }

        public LegacyHrFinanceSaveResult SaveComponent(PayrollComponentEditViewModel request)
        {
            return _repository.SaveComponent(request);
        }

        public EmployeeAdvanceViewModel GetAdvance(int id)
        {
            return _repository.GetAdvance(id);
        }

        public LegacyHrFinanceSaveResult SaveAdvance(EmployeeAdvanceViewModel request, int? userId)
        {
            return _repository.SaveAdvance(request, userId);
        }

        public LegacyHrFinanceSaveResult DeleteAdvance(int id)
        {
            return _repository.DeleteAdvance(id);
        }

        public System.Collections.Generic.IList<EnterpriseHrEmployeeLookupViewModel> SearchEmployees(string searchText, string employeeStatus, int take)
        {
            return _repository.SearchEmployees(searchText, employeeStatus, take);
        }
    }
}
