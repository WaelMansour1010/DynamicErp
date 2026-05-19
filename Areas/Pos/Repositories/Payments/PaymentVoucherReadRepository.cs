using MyERP.Areas.MainErp.Interfaces;
using MyERP.Common.FinanceVouchers;

namespace MyERP.Areas.Pos.Repositories.Payments
{
    public class PaymentVoucherReadRepository : MyERP.Areas.MainErp.Repositories.Payments.PaymentVoucherReadRepository
    {
        private const string PosFinanceConnectionStringName = "KishnyCashConnection";

        public PaymentVoucherReadRepository()
            : base(new FinanceVoucherDbConnectionFactory(PosFinanceConnectionStringName))
        {
        }

        public PaymentVoucherReadRepository(IMainErpDbConnectionFactory connectionFactory)
            : base(connectionFactory)
        {
        }
    }
}
