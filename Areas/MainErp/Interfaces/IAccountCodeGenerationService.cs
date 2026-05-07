using MyERP.Areas.MainErp.Models.Accounting;

namespace MyERP.Areas.MainErp.Interfaces
{
    public interface IAccountCodeGenerationService
    {
        AccountCodePreview PreviewNextChildCode(string parentAccountCode);
        bool IsLastAccount(string accountCode);
    }
}
