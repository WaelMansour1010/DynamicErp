using MyERP.Areas.Pos.Data;
using MyERP.Areas.Pos.Models;
using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Web.Mvc;

namespace MyERP.Areas.Pos.Controllers
{
    public class SalesRepresentativesPerformanceController : Controller
    {
        private readonly PosSqlRepository _posRepository;
        private readonly PosSalesPerformanceRepository _performanceRepository;

        public SalesRepresentativesPerformanceController()
        {
            _posRepository = new PosSqlRepository();
            _performanceRepository = new PosSalesPerformanceRepository();
        }

        public ActionResult Index(SalesRepresentativesPerformanceFilter filter)
        {
            var model = BuildModel(filter);
            if (model == null)
            {
                return RedirectToAction("Index", "PosLogin", new { area = "Pos" });
            }

            if (!CanOpen(model.Context))
            {
                return new HttpStatusCodeResult(403, "ليست لديك صلاحية عرض تقرير أداء المناديب");
            }

            ViewBag.PosContext = model.Context;
            ViewBag.ActiveScreen = "sales-performance";
            return View(model);
        }

        public ActionResult Export(SalesRepresentativesPerformanceFilter filter)
        {
            var model = BuildModel(filter);
            if (model == null)
            {
                return RedirectToAction("Index", "PosLogin", new { area = "Pos" });
            }

            if (!CanOpen(model.Context))
            {
                return new HttpStatusCodeResult(403, "ليست لديك صلاحية تصدير تقرير أداء المناديب");
            }

            var builder = new StringBuilder();
            builder.AppendLine("<html><head><meta charset=\"utf-8\" /></head><body><table border=\"1\">");
            builder.AppendLine("<tr><th>المندوب</th><th>الفرع</th><th>Cash In</th><th>عدد Cash In</th><th>Cash Out بدون رسوم</th><th>Cash Out بالرسوم</th><th>عدد Cash Out</th><th>المخالفات</th><th>عدد المخالفات</th><th>الرسوم</th><th>الكروت</th><th>عدد الكروت</th><th>التوريد المتوقع</th><th>تارجت الشحنات</th><th>تحقيق الشحنات %</th><th>تارجت الكروت</th><th>تحقيق الكروت %</th><th>النسبة الإجمالية %</th><th>المطلوب يوميًا شحنات</th><th>Projection شحنات</th><th>الحالة</th></tr>");
            foreach (var row in model.Rows)
            {
                builder.Append("<tr>");
                AppendCell(builder, row.RepresentativeName);
                AppendCell(builder, row.BranchName);
                AppendCell(builder, row.CashInTotal);
                AppendCell(builder, row.CashInCount);
                AppendCell(builder, row.CashOutWithoutFees);
                AppendCell(builder, row.CashOutWithFees);
                AppendCell(builder, row.CashOutCount);
                AppendCell(builder, row.ViolationsTotal);
                AppendCell(builder, row.ViolationsCount);
                AppendCell(builder, row.FeesTotal);
                AppendCell(builder, row.CardsTotal);
                AppendCell(builder, row.CardsCount);
                AppendCell(builder, row.ExpectedCashSupply);
                AppendCell(builder, row.RechargeTarget);
                AppendCell(builder, row.RechargeAchievementPercent);
                AppendCell(builder, row.CardTarget);
                AppendCell(builder, row.CardAchievementPercent);
                AppendCell(builder, row.OverallAchievementPercent);
                AppendCell(builder, row.RequiredDailyRecharge);
                AppendCell(builder, row.ProjectedRecharge);
                AppendCell(builder, row.PerformanceStatus);
                builder.AppendLine("</tr>");
            }

            builder.AppendLine("</table></body></html>");
            var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(builder.ToString())).ToArray();
            var fileName = "SalesRepresentativesPerformance_" + DateTime.Now.ToString("yyyyMMdd_HHmm", CultureInfo.InvariantCulture) + ".xls";
            return File(bytes, "application/vnd.ms-excel", fileName);
        }

        private SalesRepresentativesPerformancePageModel BuildModel(SalesRepresentativesPerformanceFilter filter)
        {
            var context = PosLoginController.RestorePosContext(Request, Session, _posRepository);
            if (context == null)
            {
                return null;
            }

            filter = Normalize(filter);
            var lockedBranchId = LockedBranch(context);
            var rows = _performanceRepository.GetPerformance(filter, lockedBranchId);
            return new SalesRepresentativesPerformancePageModel
            {
                Context = context,
                Filter = filter,
                LockedBranchId = lockedBranchId,
                Branches = IsAdmin(context)
                    ? _posRepository.GetBranches().ToList()
                    : _posRepository.GetBranches().Where(x => x.BranchId == context.BranchId.GetValueOrDefault()).ToList(),
                SalesRepresentatives = _performanceRepository.GetSalesRepresentatives(lockedBranchId),
                Rows = rows,
                Summary = _performanceRepository.BuildSummary(rows)
            };
        }

        private static SalesRepresentativesPerformanceFilter Normalize(SalesRepresentativesPerformanceFilter filter)
        {
            filter = filter ?? new SalesRepresentativesPerformanceFilter();
            filter.FromDate = (filter.FromDate ?? DateTime.Today).Date;
            filter.ToDate = (filter.ToDate ?? DateTime.Today).Date;
            if (filter.ToDate.Value < filter.FromDate.Value)
            {
                var from = filter.FromDate;
                filter.FromDate = filter.ToDate;
                filter.ToDate = from;
            }

            return filter;
        }

        private static int? LockedBranch(PosUserContext context)
        {
            return !IsAdmin(context) && context != null && context.BranchId.HasValue ? context.BranchId : null;
        }

        private static bool CanOpen(PosUserContext context)
        {
            return IsAdmin(context) || (context != null && (context.CanViewReports || context.CanReportSalesmen || context.CanReportSalesComplete || context.CanViewAccountingReports));
        }

        private static bool IsAdmin(PosUserContext context)
        {
            return context != null && (context.UserType.GetValueOrDefault(-1) == 0 || context.IsFullAccess);
        }

        private static void AppendCell(StringBuilder builder, object value)
        {
            builder.Append("<td>");
            builder.Append(System.Web.HttpUtility.HtmlEncode(Convert.ToString(value, CultureInfo.InvariantCulture)));
            builder.Append("</td>");
        }
    }
}
