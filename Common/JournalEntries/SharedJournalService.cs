using System;
using System.Collections.Generic;
using System.Linq;

namespace MyERP.Common.JournalEntries
{
    public sealed class SharedJournalSaveResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string Details { get; set; }
        public SharedJournalHeaderDto Entry { get; set; }
    }

    public sealed class SharedJournalService
    {
        private readonly SharedJournalSqlRepository _repository;

        public SharedJournalService(SharedJournalSqlRepository repository)
        {
            if (repository == null)
            {
                throw new ArgumentNullException("repository");
            }

            _repository = repository;
        }

        public IList<SharedBranchDto> GetBranches()
        {
            return _repository.GetBranches();
        }

        public IList<SharedLookupDto> SearchAccounts(string term)
        {
            return _repository.SearchAccounts(term);
        }

        public IList<SharedAccountTreeDto> GetChartOfAccountsChildren(string parentCode, string term)
        {
            return _repository.GetChartOfAccountsChildren(parentCode, term);
        }

        public IList<SharedJournalHeaderDto> Search(SharedJournalEntryMode mode, SharedJournalSearchRequest request, int userId, bool canChangeDefaults)
        {
            return _repository.SearchJournalEntries(SharedJournalProfile.ForMode(mode), request, userId, canChangeDefaults);
        }

        public SharedJournalHeaderDto Get(SharedJournalEntryMode mode, int noteId, int userId, bool canChangeDefaults)
        {
            return _repository.GetJournalEntryByNoteId(SharedJournalProfile.ForMode(mode), noteId, userId, canChangeDefaults);
        }

        public SharedJournalSaveResult Save(
            SharedJournalEntryMode mode,
            SharedManualJournalSaveRequest request,
            int userId,
            bool canChangeDefaults,
            bool allowAutomaticOverride,
            string successMessage,
            string automaticEditDeniedMessage,
            bool validateAccountsExist,
            bool validateBranchExists,
            bool rejectZeroValueLines)
        {
            var validation = ValidateSave(mode, request, validateAccountsExist, validateBranchExists, rejectZeroValueLines);
            if (!string.IsNullOrWhiteSpace(validation))
            {
                return Fail(validation);
            }

            var profile = SharedJournalProfile.ForMode(mode);
            if (request.NoteId.GetValueOrDefault() > 0 && _repository.IsPosted(profile, request.NoteId.Value))
            {
                return Fail("لا يمكن تعديل قيد مرحل أو مغلق.");
            }

            var existing = request.NoteId.GetValueOrDefault() > 0
                ? _repository.GetJournalEntryByNoteId(profile, request.NoteId.Value, userId, canChangeDefaults)
                : null;

            if (existing != null && !existing.IsManual && !allowAutomaticOverride)
            {
                return Fail(automaticEditDeniedMessage ?? "لا يمكن تعديل قيد آلي بدون صلاحية المدير.");
            }

            try
            {
                var entry = _repository.SaveManualJournalEntry(profile, request, userId, canChangeDefaults, allowAutomaticOverride);
                return new SharedJournalSaveResult
                {
                    Success = true,
                    Message = successMessage ?? "تم حفظ القيد بنجاح",
                    Entry = entry
                };
            }
            catch (Exception ex)
            {
                return new SharedJournalSaveResult
                {
                    Success = false,
                    Message = "تعذر حفظ القيد",
                    Details = ex.Message
                };
            }
        }

        public string ValidateSave(SharedJournalEntryMode mode, SharedManualJournalSaveRequest request, bool validateAccountsExist, bool validateBranchExists, bool rejectZeroValueLines)
        {
            if (request == null)
            {
                return "بيانات القيد غير مكتملة";
            }

            if (request.NoteDate < new DateTime(1900, 1, 1))
            {
                return "تاريخ القيد غير صحيح";
            }

            if (!request.BranchId.HasValue || request.BranchId.Value <= 0)
            {
                return "الفرع مطلوب";
            }

            if (validateBranchExists && !_repository.BranchExists(request.BranchId.Value))
            {
                return "الفرع غير صحيح";
            }

            var lines = (request.Lines ?? new List<SharedManualJournalLineDto>())
                .Where(x => x != null && (!string.IsNullOrWhiteSpace(x.AccountCode) || x.Debit != 0 || x.Credit != 0))
                .ToList();
            request.Lines = lines;

            if (lines.Count < 2)
            {
                return mode == SharedJournalEntryMode.OpeningBalance
                    ? "يجب إدخال طرفين على الأقل للقيد."
                    : "يجب إدخال سطرين على الأقل";
            }

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line.AccountCode))
                {
                    return "لا يوجد حساب في أحد سطور القيد";
                }

                if (validateAccountsExist && !_repository.AccountExists(line.AccountCode))
                {
                    return "يوجد حساب غير صحيح في سطور القيد.";
                }

                if (line.Debit < 0 || line.Credit < 0)
                {
                    return "لا يسمح بقيم سالبة في القيد";
                }

                if (line.Debit > 0 && line.Credit > 0)
                {
                    return "السطر لا يمكن أن يكون مدين ودائن في نفس الوقت";
                }

                if (rejectZeroValueLines && line.Debit == 0 && line.Credit == 0)
                {
                    return "يوجد سطر بدون قيمة مدين أو دائن.";
                }
            }

            var debit = lines.Sum(x => x.Debit);
            var credit = lines.Sum(x => x.Credit);
            if (debit <= 0 || credit <= 0 || Math.Abs(debit - credit) > 0.01m)
            {
                return mode == SharedJournalEntryMode.OpeningBalance
                    ? "القيد الافتتاحي غير متوازن - يحتاج مراجعة."
                    : "إجمالي المدين يجب أن يساوي إجمالي الدائن";
            }

            return null;
        }

        private static SharedJournalSaveResult Fail(string message)
        {
            return new SharedJournalSaveResult
            {
                Success = false,
                Message = message
            };
        }
    }
}
