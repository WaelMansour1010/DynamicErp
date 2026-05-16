using System;
using MyERP.Areas.MainErp.Repositories.JournalEntries;
using MyERP.Areas.MainErp.ViewModels.JournalEntries;
using MyERP.Common.JournalEntries;

namespace MyERP.Areas.MainErp.Services.JournalEntries
{
    public class MainErpJournalEntryService
    {
        private readonly JournalEntryReadRepository _repository;
        private readonly SharedJournalEntryCore _sharedCore;

        public MainErpJournalEntryService(JournalEntryReadRepository repository)
            : this(repository, new SharedJournalEntryCore())
        {
        }

        public MainErpJournalEntryService(JournalEntryReadRepository repository, SharedJournalEntryCore sharedCore)
        {
            _repository = repository;
            _sharedCore = sharedCore;
        }

        public JournalEntriesIndexViewModel BuildIndexModel(
            SharedJournalEntryMode mode,
            string searchText,
            int? branchId,
            DateTime? fromDate,
            DateTime? toDate,
            int page,
            int pageSize)
        {
            var context = _sharedCore.BuildContext("MainErp", mode);
            var criteria = _sharedCore.BuildSearchCriteria(mode, searchText, branchId, fromDate, toDate, page, pageSize);
            var data = mode == SharedJournalEntryMode.OpeningBalance
                ? _repository.SearchOpeningBalance(criteria.SearchText, criteria.BranchId, criteria.FromDate, criteria.ToDate, criteria.Page, criteria.PageSize)
                : _repository.Search(criteria.SearchText, criteria.BranchId, criteria.FromDate, criteria.ToDate, criteria.Page, criteria.PageSize);

            var model = new JournalEntriesIndexViewModel
            {
                SearchText = criteria.SearchText,
                BranchId = criteria.BranchId,
                FromDate = criteria.FromDate,
                ToDate = criteria.ToDate,
                Page = criteria.Page,
                PageSize = criteria.PageSize,
                TotalCount = data.TotalCount,
                Warning = data.Warning,
                Mode = mode.ToString(),
                IsOpeningBalance = mode == SharedJournalEntryMode.OpeningBalance,
                IsPostingEnabled = context.IsPostingEnabled,
                ScreenTitle = context.ScreenTitle,
                ScreenIntro = context.ScreenIntro,
                ModeBadge = context.ModeBadge,
                SearchActionName = mode == SharedJournalEntryMode.OpeningBalance ? "OpeningBalance" : "Index"
            };

            foreach (var message in context.Messages)
            {
                model.ModeMessages.Add(message);
            }

            foreach (var item in data.Items)
            {
                model.Items.Add(item);
            }

            return model;
        }

        public JournalEntryDetailsViewModel GetDetailsByNote(int noteId)
        {
            return _repository.GetDetailsByNote(noteId);
        }

        public JournalEntryDetailsViewModel GetDetailsByVoucher(int voucherId)
        {
            return _repository.GetDetailsByVoucher(voucherId);
        }
    }
}
