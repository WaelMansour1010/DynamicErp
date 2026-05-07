using MyERP.Areas.MainErp.Models.Accounting;

namespace MyERP.Areas.MainErp.Interfaces
{
    public interface IVoucherPostingService
    {
        PostingResult Preview(VoucherBatch batch);
        PostingResult Post(VoucherBatch batch, IMainErpUnitOfWork unitOfWork);
    }
}
