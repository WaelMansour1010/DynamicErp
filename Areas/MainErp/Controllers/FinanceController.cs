using System;
using System.Web.Mvc;

namespace MyERP.Areas.MainErp.Controllers
{
    public class FinanceController : MainErpControllerBase
    {
        public ActionResult PaymentVoucher(DateTime? fromDate, DateTime? toDate, string serial, string party, int? branchId, string cashboxOrBank, decimal? amount, int page = 1, int pageSize = 50)
        {
            return RedirectToAction("Index", "Payments", new
            {
                area = "MainErp",
                fromDate,
                toDate,
                serial,
                party,
                branchId,
                cashboxOrBank,
                amount,
                page,
                pageSize
            });
        }
    }
}
