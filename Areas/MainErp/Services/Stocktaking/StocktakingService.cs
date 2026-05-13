using System;
using System.Collections.Generic;
using System.Linq;
using MyERP.Areas.MainErp.Models.Security;
using MyERP.Areas.MainErp.Repositories.Stocktaking;
using MyERP.Areas.MainErp.ViewModels.Stocktaking;

namespace MyERP.Areas.MainErp.Services.Stocktaking
{
    public class StocktakingService
    {
        private readonly StocktakingRepository _repository;

        public StocktakingService(StocktakingRepository repository)
        {
            _repository = repository;
        }

        public StocktakingIndexViewModel LoadIndex(string searchText, int? storeId, int? branchId, string mode)
        {
            return new StocktakingIndexViewModel
            {
                SearchText = searchText,
                StoreId = storeId,
                BranchId = branchId,
                Mode = NormalizeMode(mode),
                Transactions = _repository.Search(searchText, storeId, branchId),
                Stores = _repository.LoadStores(),
                Branches = _repository.LoadBranches(),
                Items = _repository.LoadItems(),
                Units = _repository.LoadUnits()
            };
        }

        public StocktakingDetailsViewModel GetDetails(int id)
        {
            return _repository.GetDetails(id);
        }

        public StocktakingSaveResult Save(StocktakingSaveRequest request, MainErpUserContext user)
        {
            var validation = Validate(request);
            if (validation.Count > 0)
            {
                return new StocktakingSaveResult { Success = false, Message = string.Join(Environment.NewLine, validation) };
            }

            request.GardEntryType = request.GardEntryType < 0 || request.GardEntryType > 2 ? 2 : request.GardEntryType;
            request.Lines = request.Lines.Where(x => x != null && x.ItemId.HasValue && x.ItemId.Value > 0).ToList();
            return _repository.Save(request, user);
        }

        public StocktakingSaveResult Delete(int id)
        {
            if (id <= 0)
            {
                return new StocktakingSaveResult { Success = false, Message = "اختر مستند الجرد أولا." };
            }

            return _repository.Delete(id);
        }

        private static IList<string> Validate(StocktakingSaveRequest request)
        {
            var errors = new List<string>();
            if (request == null)
            {
                errors.Add("بيانات الجرد غير مكتملة.");
                return errors;
            }

            if (!request.StoreId.HasValue || request.StoreId.Value <= 0)
            {
                errors.Add("يجب اختيار المخزن.");
            }

            if (!request.Date.HasValue)
            {
                errors.Add("يجب إدخال تاريخ الجرد.");
            }

            if (request.Lines == null || !request.Lines.Any(x => x != null && x.ItemId.HasValue && x.ItemId.Value > 0))
            {
                errors.Add("يجب إدخال صنف واحد على الأقل.");
            }

            if (request.Lines != null)
            {
                var repeatedSerial = request.Lines
                    .Where(x => x != null && x.ItemId.HasValue && !string.IsNullOrWhiteSpace(x.Serial))
                    .GroupBy(x => x.ItemId.Value + "|" + x.Serial.Trim(), StringComparer.OrdinalIgnoreCase)
                    .Any(g => g.Count() > 1);
                if (repeatedSerial)
                {
                    errors.Add("يوجد تكرار في أرقام السيريال المدخلة.");
                }

                if (request.Lines.Any(x => x != null && x.ItemId.HasValue && x.Count < 0))
                {
                    errors.Add("كمية الجرد لا يمكن أن تكون سالبة.");
                }
            }

            return errors;
        }

        private static string NormalizeMode(string mode)
        {
            return string.Equals(mode, "FrmNewGard1", StringComparison.OrdinalIgnoreCase) ? "FrmNewGard1" : "FrmNewGard";
        }
    }
}
