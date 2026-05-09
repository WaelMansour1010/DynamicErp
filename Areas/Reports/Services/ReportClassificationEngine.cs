using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using MyERP.Areas.Reports.Models;

namespace MyERP.Areas.Reports.Services
{
    public class ReportClassificationEngine
    {
        private static readonly Regex SelectRegex = new Regex(@"\bSELECT\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex InsertRegex = new Regex(@"\bINSERT\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex UpdateRegex = new Regex(@"\bUPDATE\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex DeleteRegex = new Regex(@"\bDELETE\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex MergeRegex = new Regex(@"\bMERGE\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex TruncateRegex = new Regex(@"\bTRUNCATE\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex DropRegex = new Regex(@"\bDROP\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex AlterRegex = new Regex(@"\bALTER\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex CreateRegex = new Regex(@"\bCREATE\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex ExecRegex = new Regex(@"\b(EXEC|EXECUTE)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex DynamicSqlRegex = new Regex(@"sp_executesql|\b(EXEC|EXECUTE)\s*\(", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex SensitiveNameRegex = new Regex(@"(password|login|audit|encrypt|token|secret|sys_)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex BadNameRegex = new Regex(@"(test_|tmp_|backup_|old_|deleteme|_v\d+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex ThreePartNameRegex = new Regex(@"\b[A-Za-z_][A-Za-z0-9_]*\.[A-Za-z_][A-Za-z0-9_]*\.[A-Za-z_][A-Za-z0-9_]*\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex GoodNameRegex = new Regex(@"^(rpt_|Report_|Get|List|Select|Print|View)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly ISet<string> SimpleParameterTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "int", "bigint", "smallint", "tinyint", "nvarchar", "varchar", "nchar", "char",
            "date", "datetime", "datetime2", "bit", "decimal", "numeric", "money"
        };

        private static readonly ISet<string> RiskyColumnTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "xml", "image", "geography", "geometry", "hierarchyid", "sql_variant"
        };

        /*
        Risk flags:
        Auto-reject: HasInsert, HasUpdate, HasDelete, HasMerge, HasTruncate,
        HasDrop, HasAlter, HasCreate, HasExec, HasDynamicSql, HasOutputParam,
        NameSensitive, EmptyBody.
        Risky: CrossDb, UntypedColumn, TooManyParams, MultiResultSet,
        NoStandardSelect.
        Score: +30 good report-like name, +25 SELECT, +10 1..10 params,
        +15 simple param types, +10 date/branch/company params, +10 View,
        -15 per risky flag, -20 bad/test/tmp/backup/old name.
        */

        public ClassificationResult ClassifyView(CatalogSourceMeta viewMeta)
        {
            return Classify(viewMeta, true);
        }

        public ClassificationResult ClassifyProc(CatalogSourceMeta procMeta)
        {
            return Classify(procMeta, false);
        }

        private static ClassificationResult Classify(CatalogSourceMeta meta, bool isView)
        {
            var flags = new List<string>();
            var body = StripObjectHeader(meta.Body ?? string.Empty);
            var name = meta.Name ?? string.Empty;

            AddIf(flags, "HasInsert", InsertRegex.IsMatch(body));
            AddIf(flags, "HasUpdate", UpdateRegex.IsMatch(body));
            AddIf(flags, "HasDelete", DeleteRegex.IsMatch(body));
            AddIf(flags, "HasMerge", MergeRegex.IsMatch(body));
            AddIf(flags, "HasTruncate", TruncateRegex.IsMatch(body));
            AddIf(flags, "HasDrop", DropRegex.IsMatch(body));
            AddIf(flags, "HasAlter", AlterRegex.IsMatch(body));
            AddIf(flags, "HasCreate", CreateRegex.IsMatch(body));
            AddIf(flags, "HasExec", ExecRegex.IsMatch(body));
            AddIf(flags, "HasDynamicSql", DynamicSqlRegex.IsMatch(body));
            AddIf(flags, "HasOutputParam", meta.Parameters.Any(p => p.IsOutput));
            AddIf(flags, "NameSensitive", SensitiveNameRegex.IsMatch(name));
            AddIf(flags, "CrossDb", ThreePartNameRegex.IsMatch(body));
            AddIf(flags, "UntypedColumn", meta.Columns.Any(c => RiskyColumnTypes.Contains(c.SqlType ?? string.Empty)));
            AddIf(flags, "TooManyParams", meta.Parameters.Count > 10);
            AddIf(flags, "MultiResultSet", SelectRegex.Matches(body).Count > 1);
            AddIf(flags, "NoStandardSelect", !meta.Columns.Any());
            AddIf(flags, "EmptyBody", !SelectRegex.IsMatch(body));

            var score = 0;
            if (GoodNameRegex.IsMatch(name)) score += 30;
            if (SelectRegex.IsMatch(body)) score += 25;
            if (meta.Parameters.Count >= 1 && meta.Parameters.Count <= 10) score += 10;
            if (meta.Parameters.All(p => SimpleParameterTypes.Contains(p.SqlType ?? string.Empty))) score += 15;
            if (meta.Parameters.Any(IsCommonReportParameter)) score += 10;
            if (isView) score += 10;
            if (BadNameRegex.IsMatch(name)) score -= 20;

            score -= flags.Count(IsRiskOnlyFlag) * 15;
            score = Math.Max(0, Math.Min(100, score));

            var status = DynamicReportCatalogStatus.Risky;
            if (flags.Any(IsAutoRejectFlag))
            {
                status = DynamicReportCatalogStatus.Rejected;
            }
            else if (score >= 80)
            {
                status = DynamicReportCatalogStatus.Approved;
            }
            else if (score >= 50)
            {
                status = DynamicReportCatalogStatus.Pending;
            }

            return new ClassificationResult
            {
                Status = status,
                Score = score,
                RiskFlags = flags
            };
        }

        private static bool IsCommonReportParameter(CatalogParameterMeta parameter)
        {
            var name = (parameter.Name ?? string.Empty).TrimStart('@');
            return name.Equals("FromDate", StringComparison.OrdinalIgnoreCase) ||
                   name.Equals("ToDate", StringComparison.OrdinalIgnoreCase) ||
                   name.Equals("BranchId", StringComparison.OrdinalIgnoreCase) ||
                   name.Equals("CompanyId", StringComparison.OrdinalIgnoreCase);
        }

        private static string StripObjectHeader(string body)
        {
            if (string.IsNullOrWhiteSpace(body)) return string.Empty;
            var match = Regex.Match(
                body,
                @"\bCREATE\s+(PROCEDURE|PROC|VIEW)\b[\s\S]*?\bAS\b",
                RegexOptions.IgnoreCase);
            return match.Success ? body.Substring(match.Index + match.Length) : body;
        }

        private static void AddIf(ICollection<string> flags, string flag, bool condition)
        {
            if (condition) flags.Add(flag);
        }

        private static bool IsRiskOnlyFlag(string flag)
        {
            return flag == "CrossDb" || flag == "UntypedColumn" || flag == "TooManyParams" ||
                   flag == "MultiResultSet" || flag == "NoStandardSelect";
        }

        private static bool IsAutoRejectFlag(string flag)
        {
            return !IsRiskOnlyFlag(flag);
        }
    }

    public class CatalogSourceMeta
    {
        public CatalogSourceMeta()
        {
            Parameters = new List<CatalogParameterMeta>();
            Columns = new List<CatalogColumnMeta>();
        }

        public string Schema { get; set; }
        public string Name { get; set; }
        public string Body { get; set; }
        public IList<CatalogParameterMeta> Parameters { get; set; }
        public IList<CatalogColumnMeta> Columns { get; set; }
    }

    public class CatalogParameterMeta
    {
        public string Name { get; set; }
        public string SqlType { get; set; }
        public bool IsOutput { get; set; }
    }

    public class CatalogColumnMeta
    {
        public string Name { get; set; }
        public string SqlType { get; set; }
    }
}
