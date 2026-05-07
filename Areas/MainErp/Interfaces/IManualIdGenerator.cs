using MyERP.Areas.MainErp.Infrastructure;

namespace MyERP.Areas.MainErp.Interfaces
{
    public interface IManualIdGenerator
    {
        ManualIdAllocation Allocate(ManualIdTarget target, IMainErpUnitOfWork unitOfWork);
        ManualIdAllocation Preview(ManualIdTarget target);
    }
}
