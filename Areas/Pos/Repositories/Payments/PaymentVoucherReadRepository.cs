using MyERP.Areas.MainErp.Interfaces;
using MyERP.Common.FinanceVouchers;

namespace MyERP.Areas.Pos.Repositories.Payments
{
    public class PaymentVoucherReadRepository : MyERP.Areas.MainErp.Repositories.Payments.PaymentVoucherReadRepository
    {
        public PaymentVoucherReadRepository()
            : base(new FinanceVoucherDbConnectionFactory())
        {
        }

        public PaymentVoucherReadRepository(IMainErpDbConnectionFactory connectionFactory)
            : base(connectionFactory)
        {
        }
    }
}
