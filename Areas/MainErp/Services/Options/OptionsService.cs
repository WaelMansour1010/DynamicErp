using MyERP.Areas.MainErp.Repositories.Options;
using MyERP.Areas.MainErp.ViewModels.Options;

namespace MyERP.Areas.MainErp.Services.Options
{
    public class OptionsService
    {
        private readonly OptionsRepository _repository;

        public OptionsService(OptionsRepository repository)
        {
            _repository = repository;
        }

        public OptionsIndexViewModel Load()
        {
            return _repository.Load();
        }

        public OptionsSaveResult Save(OptionSaveRequest request)
        {
            return _repository.Save(request);
        }
    }
}
