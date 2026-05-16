using MyERP.Areas.MainErp.Repositories.FinancialAdministration;
using MyERP.Areas.MainErp.ViewModels.FinancialAdministration;

namespace MyERP.Areas.MainErp.Services.FinancialAdministration
{
    public class FinancialAdministrationService
    {
        private readonly FinancialAdministrationRepository _repository;

        public FinancialAdministrationService(FinancialAdministrationRepository repository)
        {
            _repository = repository;
        }

        public FinancialAdministrationIndexViewModel LoadIndex(FinancialAdministrationSearchViewModel search)
        {
            return _repository.LoadIndex(search);
        }

        public FinancialBankEditViewModel GetBank(int bankId)
        {
            return _repository.GetBank(bankId);
        }

        public FinancialBoxEditViewModel GetBox(int boxId)
        {
            return _repository.GetBox(boxId);
        }

        public System.Collections.Generic.IList<FinancialLookupViewModel> SearchAccounts(string term, int limit)
        {
            return _repository.SearchAccounts(term, limit);
        }

        public FinancialAdministrationSaveResult SaveBank(FinancialBankEditViewModel request)
        {
            return _repository.SaveBank(request);
        }

        public FinancialAdministrationSaveResult SaveBox(FinancialBoxEditViewModel request)
        {
            return _repository.SaveBox(request);
        }
    }
}
