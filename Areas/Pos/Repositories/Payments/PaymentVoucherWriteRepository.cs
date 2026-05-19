using MyERP.Areas.MainErp.Interfaces;
using MyERP.Common.FinanceVouchers;

namespace MyERP.Areas.Pos.Repositories.Payments
{
    public class PaymentVoucherWriteRepository : MyERP.Areas.MainErp.Repositories.Payments.PaymentVoucherWriteRepository
    {
        private const string PosFinanceConnectionStringName = "KishnyCashConnection";

        public PaymentVoucherWriteRepository()
            : base(new FinanceVoucherDbConnectionFactory(PosFinanceConnectionStringName))
        {
        }

        public PaymentVoucherWriteRepository(IMainErpDbConnectionFactory connectionFactory)
            : base(connectionFactory)
        {
        }
    }
}
