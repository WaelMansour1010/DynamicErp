using System;
using System.Collections.Generic;
using System.Linq;
using MyERP.Areas.MainErp.Models.Security;
using MyERP.Areas.MainErp.Repositories.LegacyOperations;
using MyERP.Areas.MainErp.ViewModels.LegacyOperations;

namespace MyERP.Areas.MainErp.Services.LegacyOperations
{
    public class LegacyOperationsService
    {
        private readonly LegacyOperationsRepository _repository;

        public LegacyOperationsService(LegacyOperationsRepository repository)
        {
            _repository = repository;
        }

        public LegacyOperationsIndexViewModel LoadIndex() { return _repository.LoadIndex(); }
        public IList<CashBoxListItem> SearchBoxes(string search) { return _repository.SearchBoxes(search); }
        public CashBoxDetails GetBox(int id) { return _repository.GetBox(id); }
        public IList<GeneralCashingDetails> SearchCashing(string search) { return _repository.SearchCashing(search); }
        public GeneralCashingDetails GetCashing(int id) { return _repository.GetCashing(id); }
        public IList<CarMaintenanceDetails> SearchCarMaintenance(string search) { return _repository.SearchCarMaintenance(search); }
        public CarMaintenanceDetails GetCarMaintenance(int id) { return _repository.GetCarMaintenance(id); }
        public IList<CarDataListItem> SearchCars(string search) { return _repository.SearchCars(search); }
        public CarDataDetails GetCarData(int id) { return _repository.GetCarData(id); }
        public IList<CarAuthorizationDetails> SearchCarAuthorizations(string search) { return _repository.SearchCarAuthorizations(search); }
        public CarAuthorizationDetails GetCarAuthorization(int id) { return _repository.GetCarAuthorization(id); }
        public IList<LegacyAttachmentItem> GetAttachments(string screenName, int recordId) { return _repository.GetAttachments(screenName, recordId); }
        public LegacyAttachmentItem AddAttachment(string screenName, int recordId, string fileName, string filePath, string contentType, long fileSize, string caption, int? userId) { return _repository.AddAttachment(screenName, recordId, fileName, filePath, contentType, fileSize, caption, userId); }
        public LegacyAttachmentItem GetAttachment(int id) { return _repository.GetAttachment(id); }
        public LegacySaveResult DeleteAttachment(int id) { return _repository.DeleteAttachment(id); }
        public LegacySaveResult SetPrimaryAttachment(int id) { return _repository.SetPrimaryAttachment(id); }

        public LegacySaveResult SaveBox(CashBoxDetails request)
        {
            var errors = new List<string>();
            if (request == null) errors.Add("بيانات الخزنة غير مكتملة.");
            else
            {
                if (string.IsNullOrWhiteSpace(request.Name)) errors.Add("اسم الخزنة مطلوب.");
                if (string.IsNullOrWhiteSpace(request.AccountCode) && string.IsNullOrWhiteSpace(request.ParentAccountCode)) errors.Add("حساب الأب مطلوب عند إنشاء خزنة جديدة.");
            }
            return errors.Count > 0 ? Fail(errors) : _repository.SaveBox(request);
        }

        public LegacySaveResult SaveCashing(GeneralCashingDetails request, MainErpUserContext user)
        {
            var errors = new List<string>();
            if (request == null) errors.Add("بيانات سند الإيداع غير مكتملة.");
            else
            {
                if (!request.RecordDate.HasValue) errors.Add("تاريخ السند مطلوب.");
                if (!request.BranchId.HasValue) errors.Add("الفرع مطلوب.");
                if (!request.GeneralBoxId.HasValue) errors.Add("الصندوق العام مطلوب.");
                if (!request.SubBoxId.HasValue) errors.Add("الصندوق الفرعي مطلوب.");
                if (request.Lines == null || !request.Lines.Any(x => x != null && x.CollectedValue > 0)) errors.Add("أدخل سطر تحصيل واحد على الأقل.");
            }
            return errors.Count > 0 ? Fail(errors) : _repository.SaveCashing(request, user);
        }

        public LegacySaveResult SaveCarMaintenance(CarMaintenanceDetails request, MainErpUserContext user)
        {
            var errors = new List<string>();
            if (request == null) errors.Add("بيانات فاتورة الصيانة غير مكتملة.");
            else
            {
                if (!request.RecordDate.HasValue) errors.Add("تاريخ الفاتورة مطلوب.");
                if (!request.BranchId.HasValue) errors.Add("الفرع مطلوب.");
                if (!request.CarTypeId.HasValue) errors.Add("نوع السيارة/المعدة مطلوب.");
                if (request.PaymentType == 0 && !request.BoxId.HasValue) errors.Add("الخزنة مطلوبة عند الدفع نقدا.");
                if (request.Lines == null || !request.Lines.Any(x => x != null && x.MainteId.HasValue && x.Value > 0)) errors.Add("أدخل سطر صيانة أو مصروف واحد على الأقل.");
            }
            return errors.Count > 0 ? Fail(errors) : _repository.SaveCarMaintenance(request, user);
        }

        public LegacySaveResult SaveCarData(CarDataDetails request)
        {
            var errors = new List<string>();
            if (request == null) errors.Add("بيانات السيارة/المعدة غير مكتملة.");
            else
            {
                if (string.IsNullOrWhiteSpace(request.BoardNo)) errors.Add("رقم اللوحة مطلوب.");
                if (!request.BranchId.HasValue) errors.Add("الفرع مطلوب.");
                if (!request.CarTypeId.HasValue) errors.Add("نوع السيارة/المعدة مطلوب.");
            }
            return errors.Count > 0 ? Fail(errors) : _repository.SaveCarData(request);
        }

        public LegacySaveResult SaveCarAuthorization(CarAuthorizationDetails request, MainErpUserContext user)
        {
            var errors = new List<string>();
            if (request == null) errors.Add("بيانات كارت التفويض غير مكتملة.");
            else
            {
                if (!request.RecordDate.HasValue) errors.Add("تاريخ الكارت مطلوب.");
                if (!request.BranchId.HasValue) errors.Add("الفرع مطلوب.");
                if (!request.CarTypeId.HasValue) errors.Add("نوع السيارة/المعدة مطلوب.");
                if (string.IsNullOrWhiteSpace(request.ClientName) && string.IsNullOrWhiteSpace(request.PlateNo)) errors.Add("اسم العميل أو رقم اللوحة مطلوب.");
            }
            return errors.Count > 0 ? Fail(errors) : _repository.SaveCarAuthorization(request, user);
        }

        public LegacySaveResult DeleteBox(int id) { return _repository.DeleteBox(id); }
        public LegacySaveResult DeleteCashing(int id) { return _repository.DeleteCashing(id); }
        public LegacySaveResult DeleteCarMaintenance(int id) { return _repository.DeleteCarMaintenance(id); }
        public LegacySaveResult DeleteCarData(int id) { return _repository.DeleteCarData(id); }
        public LegacySaveResult DeleteCarAuthorization(int id) { return _repository.DeleteCarAuthorization(id); }

        private static LegacySaveResult Fail(IEnumerable<string> errors)
        {
            return new LegacySaveResult { Success = false, Message = string.Join(Environment.NewLine, errors) };
        }
    }
}
