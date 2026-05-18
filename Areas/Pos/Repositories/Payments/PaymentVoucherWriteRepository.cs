using MyERP.Areas.MainErp.Interfaces;
using MyERP.Common.FinanceVouchers;

namespace MyERP.Areas.Pos.Repositories.Payments
{
    public class PaymentVoucherWriteRepository : MyERP.Areas.MainErp.Repositories.Payments.PaymentVoucherWriteRepository
    {
        public PaymentVoucherWriteRepository()
            : base(new FinanceVoucherDbConnectionFactory())
        {
        }

        public PaymentVoucherWriteRepository(IMainErpDbConnectionFactory connectionFactory)
            : base(connectionFactory)
        {
        }
    }
}
