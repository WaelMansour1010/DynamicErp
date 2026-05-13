using System.Collections.Generic;
using MyERP.Areas.MainErp.Models.Security;
using MyERP.Areas.MainErp.Repositories.Items;
using MyERP.Areas.MainErp.ViewModels.Items;

namespace MyERP.Areas.MainErp.Services.Items
{
    public class ItemsService
    {
        private readonly ItemsRepository _repository;

        public ItemsService(ItemsRepository repository)
        {
            _repository = repository;
        }

        public ItemsIndexViewModel LoadIndex(string searchText, int? groupId, int page, int pageSize, MainErpUserContext user)
        {
            return _repository.LoadIndex(searchText, groupId, page, pageSize, user);
        }

        public ItemDetailsViewModel GetDetails(int id)
        {
            return _repository.GetDetails(id);
        }

        public IList<ItemLookupViewModel> LoadGroups()
        {
            return _repository.LoadGroups();
        }

        public IList<GroupListItemViewModel> LoadGroupTree()
        {
            return _repository.LoadGroupTree();
        }

        public GroupListItemViewModel GetGroupDetails(int id)
        {
            return _repository.GetGroupDetails(id);
        }

        public GroupSaveResult SaveGroup(GroupSaveRequest request, MainErpUserContext user)
        {
            return _repository.SaveGroup(request, user);
        }

        public GroupSaveResult DeleteGroup(int id)
        {
            return _repository.DeleteGroup(id);
        }

        public IList<ItemLookupViewModel> LoadUnits()
        {
            return _repository.LoadUnits();
        }

        public ItemSaveResult Save(ItemSaveRequest request, MainErpUserContext user)
        {
            return _repository.Save(request, user);
        }

        public ItemSaveResult Delete(int id, MainErpUserContext user)
        {
            return _repository.Delete(id, user);
        }
    }
}
