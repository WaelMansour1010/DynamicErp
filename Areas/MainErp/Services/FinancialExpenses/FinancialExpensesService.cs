using System;
using System.Collections.Generic;
using System.Linq;
using MyERP.Areas.MainErp.Models.Security;
using MyERP.Areas.MainErp.Repositories.FinancialExpenses;
using MyERP.Areas.MainErp.ViewModels.FinancialExpenses;

namespace MyERP.Areas.MainErp.Services.FinancialExpenses
{
    public class FinancialExpensesService
    {
        private readonly FinancialExpensesRepository _repository;

        public FinancialExpensesService(FinancialExpensesRepository repository)
        {
            _repository = repository;
        }

        public FinancialExpensesIndexViewModel LoadIndex(string mode, string searchText)
        {
            var normalized = NormalizeMode(mode);
            return new FinancialExpensesIndexViewModel
            {
                Mode = normalized,
                SearchText = searchText,
                Documents = _repository.Search(NoteType(normalized), searchText),
                Branches = _repository.LoadBranches(),
                Boxes = _repository.LoadBoxes(),
                Banks = _repository.LoadBanks(),
                Vendors = _repository.LoadVendors(),
                ExpensesTypes = _repository.LoadExpensesTypes(),
                Accounts = _repository.LoadAccounts()
            };
        }

        public FinancialExpenseDetails GetDetails(int id)
        {
            return _repository.GetDetails(id);
        }

        public FinancialExpenseSaveResult Save(FinancialExpenseSaveRequest request, MainErpUserContext user)
        {
            var errors = Validate(request);
            if (errors.Count > 0)
            {
                return new FinancialExpenseSaveResult { Success = false, Message = string.Join(Environment.NewLine, errors) };
            }

            request.Mode = NormalizeMode(request.Mode);
            request.NoteType = NoteType(request.Mode);
            request.Value = request.Lines.Where(x => x != null && !string.IsNullOrWhiteSpace(x.AccountCode)).Sum(x => x.Value);
            return _repository.Save(request, user);
        }

        public FinancialExpenseSaveResult Delete(int id)
        {
            return id <= 0
                ? new FinancialExpenseSaveResult { Success = false, Message = "اختر المستند أولا." }
                : _repository.Delete(id);
        }

        private static IList<string> Validate(FinancialExpenseSaveRequest request)
        {
            var errors = new List<string>();
            if (request == null)
            {
                errors.Add("بيانات المستند غير مكتملة.");
                return errors;
            }

            if (!request.Date.HasValue)
            {
                errors.Add("تاريخ المستند مطلوب.");
            }

            if (!request.BranchId.HasValue || request.BranchId.Value <= 0)
            {
                errors.Add("الفرع مطلوب.");
            }

            if (request.PaymentType == 0 && (!request.BoxId.HasValue || request.BoxId.Value <= 0))
            {
                errors.Add("الخزنة مطلوبة عند الدفع نقدا.");
            }

            if ((request.PaymentType == 1 || request.PaymentType == 3) && (!request.BankId.HasValue || request.BankId.Value <= 0))
            {
                errors.Add("البنك مطلوب عند الدفع البنكي أو الشيك.");
            }

            if (request.PaymentType == 2 && (!request.VendorId.HasValue || request.VendorId.Value <= 0))
            {
                errors.Add("المورد/صاحب العهدة مطلوب.");
            }

            if (string.IsNullOrWhiteSpace(request.CreditAccountCode))
            {
                errors.Add("حساب الطرف الدائن مطلوب.");
            }

            if (request.Lines == null || !request.Lines.Any(x => x != null && !string.IsNullOrWhiteSpace(x.AccountCode) && x.Value > 0))
            {
                errors.Add("أدخل سطر مصروف/حساب واحد على الأقل.");
            }

            return errors;
        }

        private static string NormalizeMode(string mode)
        {
            return string.Equals(mode, "FrmExpenses30", StringComparison.OrdinalIgnoreCase) ? "FrmExpenses30" : "FrmExpenses3";
        }

        private static int NoteType(string mode)
        {
            return string.Equals(mode, "FrmExpenses30", StringComparison.OrdinalIgnoreCase) ? 350 : 80;
        }
    }
}
