using System;
using MyERP.Areas.MainErp.Models.Accounting;

namespace MyERP.Areas.MainErp.Interfaces
{
    public interface INoteNumberingService
    {
        NumberingPreview PreviewNoteSerial(int branchId, DateTime noteDate);
        NumberingPreview PreviewVoucherSerial(int branchId, DateTime voucherDate, int sanadNo, int transactionType, int? billTo);
    }
}
