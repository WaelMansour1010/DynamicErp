using MyERP.Areas.MainErp.Repositories.LegacyHrFinance;
using MyERP.Areas.MainErp.ViewModels.LegacyHrFinance;

namespace MyERP.Areas.MainErp.Services.LegacyHrFinance
{
    public class LegacyHrFinanceService
    {
        private readonly LegacyHrFinanceRepository _repository;

        public LegacyHrFinanceService(LegacyHrFinanceRepository repository)
        {
            _repository = repository;
        }

        public LegacyHrFinancePageViewModel Load(string moduleKey, string searchText, int page, int pageSize)
        {
            return _repository.Load(moduleKey, searchText, page, pageSize);
        }

        public PayrollComponentEditViewModel GetComponent(int id)
        {
            return _repository.GetComponent(id);
        }

        public LegacyHrFinanceSaveResult SaveComponent(PayrollComponentEditViewModel request)
        {
            return _repository.SaveComponent(request);
        }
    }
}
