using System;
using System.Collections.Generic;
using MyERP.Areas.MainErp.Models.Security;
using MyERP.Areas.MainErp.Repositories.DefinCompItem;
using MyERP.Areas.MainErp.ViewModels.DefinCompItem;

namespace MyERP.Areas.MainErp.Services.DefinCompItem
{
    public class DefinCompItemService
    {
        private readonly DefinCompItemRepository _repository;

        public DefinCompItemService(DefinCompItemRepository repository)
        {
            _repository = repository;
        }

        public DefinCompItemIndexViewModel LoadIndex(string searchText, DateTime? fromDate, DateTime? toDate, int? branchId, int? storeId, int page, int pageSize, MainErpUserContext user)
        {
            return _repository.LoadIndex(searchText, fromDate, toDate, branchId, storeId, page, pageSize, user);
        }

        public DefinCompItemDetailsViewModel GetDetails(int id)
        {
            return _repository.GetDetails(id);
        }

        public IList<DefinCompItemLookupItemViewModel> SearchItems(string term, int maxRows)
        {
            return _repository.SearchItems(term, maxRows);
        }

        public IList<DefinCompItemLookupItemViewModel> SearchUnits(int itemId)
        {
            return _repository.SearchUnits(itemId);
        }

        public IList<DefinCompItemLookupItemViewModel> LoadBranches()
        {
            return _repository.LoadBranches();
        }

        public IList<DefinCompItemLookupItemViewModel> LoadStores(int? branchId)
        {
            return _repository.LoadStores(branchId);
        }

        public IList<DefinCompItemLookupItemViewModel> LoadCustomers()
        {
            return _repository.LoadCustomers();
        }

        public DefinCompItemSaveResult Save(DefinCompItemSaveRequest request, MainErpUserContext user)
        {
            return _repository.Save(request, user);
        }

        public DefinCompItemSaveResult Delete(int id, MainErpUserContext user)
        {
            return _repository.Delete(id, user);
        }
    }
}
