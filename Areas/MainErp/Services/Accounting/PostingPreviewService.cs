using System;
using MyERP.Areas.MainErp.Interfaces;
using MyERP.Areas.MainErp.Models.Accounting;
using MyERP.Areas.MainErp.ViewModels.Accounting;

namespace MyERP.Areas.MainErp.Services.Accounting
{
    public class PostingPreviewService
    {
        private readonly IVoucherPostingService _voucherPostingService;

        public PostingPreviewService(IVoucherPostingService voucherPostingService)
        {
            _voucherPostingService = voucherPostingService;
        }

        public PostingPreviewViewModel BuildDemoPreview()
        {
            var batch = new VoucherBatch
            {
                PreviewOnly = true,
                SourceModule = "MainErp.Accounting",
                SourceEntity = "PreviewTest",
                SourceKey = "DEMO"
            };

            batch.Entries.Add(new VoucherEntry
            {
                LineNumber = 1,
                AccountCode = "Preview-Debit",
                EntryType = VoucherEntryType.Debit,
                Value = 100m,
                Description = "Preview debit line",
                DescriptionEnglish = "Preview debit line",
                RecordDate = DateTime.Today,
                BranchId = 1
            });

            batch.Entries.Add(new VoucherEntry
            {
                LineNumber = 2,
                AccountCode = "Preview-Credit",
                EntryType = VoucherEntryType.Credit,
                Value = 100m,
                Description = "Preview credit line",
                DescriptionEnglish = "Preview credit line",
                RecordDate = DateTime.Today,
                BranchId = 1
            });

            var result = _voucherPostingService.Preview(batch);
            var model = new PostingPreviewViewModel
            {
                Title = "Posting Preview",
                ArabicTitle = "معاينة القيد المحاسبي",
                TotalDebit = result.TotalDebit,
                TotalCredit = result.TotalCredit,
                IsBalanced = result.Success
            };

            foreach (var entry in result.Entries)
            {
                model.Entries.Add(entry);
            }

            foreach (var warning in result.Warnings)
            {
                model.Warnings.Add(warning);
            }

            foreach (var error in result.Errors)
            {
                model.Errors.Add(error);
            }

            return model;
        }
    }
}
