using System;
using System.Linq;
using MyERP.Areas.MainErp.Infrastructure;
using MyERP.Areas.MainErp.Interfaces;
using MyERP.Areas.MainErp.Models.Accounting;
using MyERP.Areas.MainErp.Repositories.Accounting;

namespace MyERP.Areas.MainErp.Services.Accounting
{
    public class VoucherPostingService : IVoucherPostingService
    {
        private readonly IManualIdGenerator _manualIdGenerator;
        private readonly VoucherRepository _voucherRepository;

        public VoucherPostingService(IManualIdGenerator manualIdGenerator, VoucherRepository voucherRepository)
        {
            _manualIdGenerator = manualIdGenerator;
            _voucherRepository = voucherRepository;
        }

        public PostingResult Preview(VoucherBatch batch)
        {
            var result = Validate(batch);
            result.PreviewOnly = true;
            if (batch != null)
            {
                result.VoucherId = batch.VoucherId;
            }

            return result;
        }

        public PostingResult Post(VoucherBatch batch, IMainErpUnitOfWork unitOfWork)
        {
            if (unitOfWork == null) throw new ArgumentNullException("unitOfWork");
            if (!unitOfWork.IsTransactionActive)
            {
                throw new InvalidOperationException("Voucher posting must run inside an active MainErp transaction.");
            }

            var result = Validate(batch);
            if (!result.Success)
            {
                return result;
            }

            var voucherId = batch.VoucherId;
            if (!voucherId.HasValue)
            {
                var target = batch.OpeningBalanceMode
                    ? ManualIdTarget.OpeningBalanceVoucherId
                    : ManualIdTarget.VoucherId;
                voucherId = _manualIdGenerator.Allocate(target, unitOfWork).Value;
            }

            foreach (var entry in batch.Entries.OrderBy(e => e.LineNumber))
            {
                _voucherRepository.InsertEntry(voucherId.Value, batch, entry, unitOfWork);
            }

            result.VoucherId = voucherId;
            result.Success = true;
            return result;
        }

        private static PostingResult Validate(VoucherBatch batch)
        {
            var result = new PostingResult();
            if (batch == null)
            {
                result.Errors.Add("Voucher batch is required.");
                return result;
            }

            foreach (var entry in batch.Entries)
            {
                result.Entries.Add(entry);
            }

            if (batch.Entries.Count == 0)
            {
                result.Errors.Add("Voucher batch must contain at least one entry.");
            }

            var lineNo = 1;
            foreach (var entry in batch.Entries)
            {
                if (entry.LineNumber == 0)
                {
                    entry.LineNumber = lineNo;
                }

                if (string.IsNullOrWhiteSpace(entry.AccountCode))
                {
                    result.Errors.Add("Line " + entry.LineNumber + ": account code is required.");
                }

                if (entry.Value == 0)
                {
                    result.Errors.Add("Line " + entry.LineNumber + ": zero-value voucher entries are rejected.");
                }

                if (entry.Value < 0)
                {
                    result.Errors.Add("Line " + entry.LineNumber + ": voucher value must be positive; flip debit/credit semantics instead.");
                }

                if (entry.RecordDate == default(DateTime))
                {
                    result.Warnings.Add("Line " + entry.LineNumber + ": record date was empty and will default to today if posted.");
                }

                lineNo++;
            }

            result.TotalDebit = batch.Entries
                .Where(e => e.EntryType == VoucherEntryType.Debit && e.Value > 0)
                .Sum(e => e.Value);
            result.TotalCredit = batch.Entries
                .Where(e => e.EntryType == VoucherEntryType.Credit && e.Value > 0)
                .Sum(e => e.Value);

            if (result.TotalDebit != result.TotalCredit)
            {
                result.Errors.Add("Voucher batch is not balanced. Debit=" + result.TotalDebit + ", Credit=" + result.TotalCredit + ".");
            }

            result.Success = result.Errors.Count == 0;
            return result;
        }
    }
}
