using System;
using System.Web.Mvc;

namespace MyERP.Areas.Pos.Controllers
{
    public class FinanceController : Controller
    {
        public ActionResult PaymentVoucher(DateTime? fromDate, DateTime? toDate, string serial, string party, int? branchId, string cashboxOrBank, decimal? amount, int page = 1, int pageSize = 50)
        {
            return RedirectToAction("Vouchers", "Payments", new
            {
                area = "Pos",
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
