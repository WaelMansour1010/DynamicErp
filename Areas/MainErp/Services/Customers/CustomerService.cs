using System.Collections.Generic;
using MyERP.Areas.MainErp.Models.Security;
using MyERP.Areas.MainErp.Repositories.Customers;
using MyERP.Areas.MainErp.ViewModels.Customers;

namespace MyERP.Areas.MainErp.Services.Customers
{
    public class CustomerService
    {
        private readonly CustomerRepository _repository;

        public CustomerService(CustomerRepository repository)
        {
            _repository = repository;
        }

        public CustomersIndexViewModel LoadIndex(string searchText, int? customerType, int? branchId, int page, int pageSize)
        {
            return _repository.LoadIndex(searchText, customerType, branchId, page, pageSize);
        }

        public CustomerEditViewModel New()
        {
            return _repository.New();
        }

        public CustomerEditViewModel Get(int id)
        {
            return _repository.Get(id);
        }

        public CustomerSaveResult Save(CustomerEditViewModel request, MainErpUserContext user)
        {
            return _repository.Save(request, user);
        }

        public CustomerSaveResult Delete(int id)
        {
            return _repository.Delete(id);
        }

        public IList<CustomerLookupItemViewModel> LoadBranches()
        {
            return _repository.LoadBranches();
        }
    }
}
