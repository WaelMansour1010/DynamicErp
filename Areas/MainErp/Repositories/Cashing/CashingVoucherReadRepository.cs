using System;
using MyERP.Areas.MainErp.Interfaces;
using MyERP.Areas.MainErp.Repositories.Payments;
using MyERP.Areas.MainErp.ViewModels.Payments;

namespace MyERP.Areas.MainErp.Repositories.Cashing
{
    public class CashingVoucherReadRepository : PaymentVoucherReadRepository
    {
        public CashingVoucherReadRepository(IMainErpDbConnectionFactory connectionFactory)
            : base(connectionFactory)
        {
        }

        public new PaymentVoucherSearchViewModel Search(DateTime? fromDate, DateTime? toDate, string serial, string party, int? branchId, string cashboxOrBank, decimal? amount, int page = 1, int pageSize = 50)
        {
            return SearchCore(4, fromDate, toDate, serial, party, branchId, cashboxOrBank, amount, page, pageSize);
        }

        public new PaymentVoucherDetailsViewModel GetDetails(int id)
        {
            return GetDetailsCore(4, id);
        }
    }
}
