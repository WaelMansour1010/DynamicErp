using System.Collections.Generic;
using System.Linq;
using MyERP.Models.Reports;

namespace MyERP.Common.AccountingReports
{
    public static class SharedAccountingReportCatalog
    {
        private static readonly IList<HtmlReportDefinition> Reports = new List<HtmlReportDefinition>
        {
            Report("opt-0-account-statement", "كشف حساب", "حركة ورصيد حساب خلال فترة", "OptAccount(0)", "Reports\\Sub-Masster.rpt / Sub-MassterEng.rpt / Sub-MassterE.rpt / Sub-MassterdetailedTransactions.rpt"),
            Report("opt-35-trial-balance-2", "ميزان مراجعة 2", "أرصدة الحسابات حسب الفلاتر", "OptAccount(35)", "Reports\\TrialBalanceNew.rpt / TrialBalanceNewEng.rpt"),
            Report("opt-39-trial-balance-levels-2", "ميزان مراجعة بالمستويات 2", "ميزان مراجعة مجمع حسب مستوى الحساب", "OptAccount(39)", "Reports\\TrialBalanceNew1.rpt / TrialBalanceNewEng1.rpt"),
            Report("opt-38-trial-balance-for-account-2", "ميزان مراجعة لحساب 2", "ميزان مراجعة لنطاق حسابات محدد", "OptAccount(38)", "Reports\\TrialBalanceNew.rpt / TrialBalanceNewEng.rpt"),
            Report("opt-3-income-statement-com", "قائمة الدخل مجمعة", "إيرادات ومصروفات مجمعة", "OptAccount(3)", "Reports\\IncomeStatementNew.rpt / IncomeStatementNewEng.rpt"),
            Report("opt-28-income-statement-int", "قائمة الدخل لفترة محددة", "قائمة دخل حسب الفترة المختارة", "OptAccount(28)", "Reports\\IncomeStatementNew1.rpt / IncomeStatementNewEng1.rpt"),
            Report("opt-41-income-statement-by-level", "قائمة الدخل بالمستوى", "قائمة دخل مجمعة حسب مستوى الحساب", "OptAccount(41)", "Reports\\IncomeStatementNew.rpt / IncomeStatementNewEng.rpt"),
            Report("opt-1-general-ledger-for", "أستاذ عام بالأرصدة", "دفتر أستاذ مع أرصدة افتتاحية وختامية", "OptAccount(1)", "Reports\\GenrealLedger.rpt / GenrealLedgerEng.rpt"),
            Report("opt-12-general-ledger-by-trans", "أستاذ عام بالحركات", "دفتر أستاذ تفصيلي بالحركات", "OptAccount(12)", "Reports\\TrialBalanceNew.rpt / TrialBalanceNewEng.rpt"),
            Report("opt-9-cost-center-transactions", "حركات مراكز التكلفة", "تحليل الحركات حسب مركز التكلفة", "OptAccount(9)", "Reports\\Transactions_with_cost_center*.rpt"),
            Report("opt-10-project-transactions-details", "حركات المشاريع تفصيلي", "حركات تفصيلية حسب المشروع", "OptAccount(10)", "Reports\\GL _with_projects.rpt / GL _with_projectse.rpt"),
            Report("opt-26-project-transaction", "كشف حساب مشروع", "حركة ورصيد مشروع خلال فترة", "OptAccount(26)", "Reports\\Sub-Masster.rpt / Sub-MassterEng.rpt"),
            Report("opt-20-project-account", "حسابات مشروع", "أرصدة حسابات المشروع", "OptAccount(20)", "Reports\\construction\\TrialBalanceNew.rpt / EmployeeStatement.rpt"),
            Report("opt-30-project-transactions-totals", "حركات المشاريع إجمالي", "إجماليات الحركات حسب المشروع", "OptAccount(30)", "Reports\\GL _with_projects1.rpt / GL _with_projectse1.rpt"),
            Report("opt-42-project-account-new", "حساب مشروع", "تحليل حسابات المشروع", "OptAccount(42)", "Reports\\GL _with_projectsAcc.rpt")
        };

        public static IList<HtmlReportDefinition> GetAll()
        {
            return Reports.Select(Clone).ToList();
        }

        public static HtmlReportDefinition Find(string key)
        {
            return Reports.FirstOrDefault(x => string.Equals(x.Key, key, System.StringComparison.OrdinalIgnoreCase));
        }

        private static HtmlReportDefinition Report(string key, string title, string description, string sourceName, string crystalFile)
        {
            return new HtmlReportDefinition
            {
                Key = key,
                Title = title,
                Description = description,
                SourceName = sourceName + " | " + crystalFile
            };
        }

        private static HtmlReportDefinition Clone(HtmlReportDefinition source)
        {
            return new HtmlReportDefinition
            {
                Key = source.Key,
                Title = source.Title,
                Description = source.Description,
                SourceName = source.SourceName,
                SupportsStoreFilter = source.SupportsStoreFilter
            };
        }
    }
}
