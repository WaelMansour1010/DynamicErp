using MyERP.Areas.MainErp.Repositories.Branches;
using MyERP.Areas.MainErp.ViewModels.Branches;

namespace MyERP.Areas.MainErp.Services.Branches
{
    public class BranchesService
    {
        private readonly BranchesRepository _repository;

        public BranchesService(BranchesRepository repository)
        {
            _repository = repository;
        }

        public BranchesIndexViewModel LoadIndex(string searchText, int? id)
        {
            return _repository.LoadIndex(searchText, id);
        }

        public BranchEditViewModel New()
        {
            return _repository.New();
        }

        public BranchEditViewModel Get(int id)
        {
            return _repository.Get(id);
        }

        public BranchSaveResult Save(BranchEditViewModel request)
        {
            return _repository.Save(request);
        }

        public BranchSaveResult Delete(int id)
        {
            return _repository.Delete(id);
        }
    }
}
