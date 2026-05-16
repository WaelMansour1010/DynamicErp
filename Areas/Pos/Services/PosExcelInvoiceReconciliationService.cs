using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using ExcelDataReader;
using MyERP.Areas.Pos.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace MyERP.Areas.Pos.Services
{
    public class PosExcelInvoiceReconciliationService
    {
        private static readonly string[] CanonicalFields =
        {
            "InvoiceDate", "InvoiceNumber", "Token", "Phone", "CustomerName", "NationalId",
            "Amount", "ServiceType", "Branch", "Store", "User", "Notes"
        };

        private readonly string _connectionString;

        public PosExcelInvoiceReconciliationService()
        {
            var connectionString = ConfigurationManager.ConnectionStrings["KishnyCashConnection"];
            if (connectionString == null || string.IsNullOrWhiteSpace(connectionString.ConnectionString))
            {
                throw new ConfigurationErrorsException("Missing connection string: KishnyCashConnection");
            }

            _connectionString = connectionString.ConnectionString;
        }

        public PosInvoiceReconciliationResult Analyze(Stream stream, string fileName, PosInvoiceReconciliationRequest request)
        {
            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }

            request = request ?? new PosInvoiceReconciliationRequest();
            if (request.ToDate < request.FromDate)
            {
                var temp = request.FromDate;
                request.FromDate = request.ToDate;
                request.ToDate = temp;
            }

            var result = new PosInvoiceReconciliationResult
            {
                SourceFileName = Path.GetFileName(fileName ?? string.Empty),
                FromDate = request.FromDate.Date,
                ToDate = request.ToDate.Date,
                BranchId = request.BranchId,
                ServiceType = request.ServiceType
            };

            var rows = ReadExcelRows(stream, result);
            var normalized = rows.Select(NormalizeRow).ToList();
            result.Summary.TotalExcelRows = normalized.Count;
            result.InvalidRows = normalized.Where(x => x.Errors.Count > 0).ToList();
            var validRows = normalized.Where(x => x.Errors.Count == 0).ToList();
            result.Summary.ValidRows = validRows.Count;
            result.Summary.InvalidRows = result.InvalidRows.Count;

            MarkExcelDuplicates(validRows);
            var dbRows = LoadDbCandidates(request);
            var dbDuplicateKeys = BuildDbDuplicateKeys(dbRows);

            foreach (var row in validRows)
            {
                var reconciled = ReconcileRow(row, dbRows, dbDuplicateKeys);
                result.Rows.Add(reconciled);
            }

            FinalizeSummary(result);
            return result;
        }

        public byte[] BuildExcelReport(PosInvoiceReconciliationResult result)
        {
            result = result ?? new PosInvoiceReconciliationResult();
            using (var stream = new MemoryStream())
            {
                using (var document = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook))
                {
                    var workbookPart = document.AddWorkbookPart();
                    workbookPart.Workbook = new Workbook();
                    var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
                    var sheetData = new SheetData();
                    worksheetPart.Worksheet = new Worksheet();
                    worksheetPart.Worksheet.Append(new SheetViews(new SheetView(new Pane { VerticalSplit = 4D, TopLeftCell = "A5", ActivePane = PaneValues.BottomLeft, State = PaneStateValues.Frozen }) { WorkbookViewId = 0, RightToLeft = true }));
                    worksheetPart.Worksheet.Append(new SheetFormatProperties { DefaultColumnWidth = 18D });
                    worksheetPart.Worksheet.Append(new Columns(new Column { Min = 1, Max = 24, Width = 20D, CustomWidth = true }));
                    worksheetPart.Worksheet.Append(sheetData);

                    sheetData.Append(RowFromValues("مراجعة وتسوية فواتير Excel"));
                    sheetData.Append(RowFromValues("File", result.SourceFileName, "From", DateText(result.FromDate), "To", DateText(result.ToDate)));
                    sheetData.Append(RowFromValues("Total", result.Summary.TotalExcelRows.ToString(CultureInfo.InvariantCulture), "Valid", result.Summary.ValidRows.ToString(CultureInfo.InvariantCulture), "Invalid", result.Summary.InvalidRows.ToString(CultureInfo.InvariantCulture)));
                    sheetData.Append(new Row());
                    sheetData.Append(RowFromValues("Excel Row", "Status", "Score", "Reason", "Excel Date", "DB Date", "Excel Invoice", "DB Transaction", "DB Serial", "Token", "Phone", "Excel Customer", "DB Customer", "Excel Amount", "DB Amount", "Difference", "Excel Branch", "DB Branch", "Excel User", "DB User", "Service", "Warnings"));

                    foreach (var row in result.Rows)
                    {
                        sheetData.Append(RowFromValues(
                            row.ExcelRowNumber.ToString(CultureInfo.InvariantCulture),
                            row.Status,
                            row.MatchScore.ToString(CultureInfo.InvariantCulture),
                            row.Reason,
                            DateText(row.ExcelInvoiceDate),
                            DateText(row.DbInvoiceDate),
                            row.ExcelInvoiceNumber,
                            row.DbTransactionId.HasValue ? row.DbTransactionId.Value.ToString(CultureInfo.InvariantCulture) : string.Empty,
                            row.DbSerial,
                            row.Token,
                            row.Phone,
                            row.ExcelCustomerName,
                            row.DbCustomerName,
                            MoneyText(row.ExcelAmount),
                            MoneyText(row.DbAmount),
                            MoneyText(row.Difference),
                            row.ExcelBranch,
                            row.DbBranch,
                            row.ExcelUser,
                            row.DbUser,
                            row.ServiceType,
                            string.Join(" | ", row.Warnings)));
                    }

                    if (result.InvalidRows.Count > 0)
                    {
                        sheetData.Append(new Row());
                        sheetData.Append(RowFromValues("Invalid Rows"));
                        sheetData.Append(RowFromValues("Excel Row", "Sheet", "Errors", "Raw Date", "Invoice", "Token", "Phone", "Customer", "Amount"));
                        foreach (var row in result.InvalidRows)
                        {
                            sheetData.Append(RowFromValues(row.RowNumber.ToString(CultureInfo.InvariantCulture), row.SheetName, string.Join(" | ", row.Errors), row.InvoiceDateText, row.InvoiceNumber, row.Token, row.Phone, row.CustomerName, MoneyText(row.Amount)));
                        }
                    }

                    worksheetPart.Worksheet.Append(new AutoFilter { Reference = "A5:V" + Math.Max(5, result.Rows.Count + 5).ToString(CultureInfo.InvariantCulture) });
                    var sheets = workbookPart.Workbook.AppendChild(new Sheets());
                    sheets.Append(new Sheet { Id = workbookPart.GetIdOfPart(worksheetPart), SheetId = 1, Name = "Reconciliation" });
                    workbookPart.Workbook.Save();
                }

                return stream.ToArray();
            }
        }

        public PosSavedInvoiceReconciliationResult AnalyzeSavedInvoices(PosInvoiceReconciliationRequest request)
        {
            request = request ?? new PosInvoiceReconciliationRequest();
            if (request.ToDate < request.FromDate)
            {
                var temp = request.FromDate;
                request.FromDate = request.ToDate;
                request.ToDate = temp;
            }

            var result = new PosSavedInvoiceReconciliationResult
            {
                FromDate = request.FromDate.Date,
                ToDate = request.ToDate.Date,
                BranchId = request.BranchId,
                TellerUserId = request.TellerUserId,
                ServiceType = request.ServiceType,
                ImportSource = string.IsNullOrWhiteSpace(request.ImportSource) ? "Both" : request.ImportSource,
                Month = request.Month,
                MinAmount = request.MinAmount,
                MaxAmount = request.MaxAmount,
                Token = request.Token,
                Phone = request.Phone,
                NationalId = request.NationalId,
                CustomerName = request.CustomerName,
                RiskLevel = request.RiskLevel,
                SearchTerm = request.SearchTerm,
                OnlyBothSources = request.OnlyBothSources,
                SuspiciousOnly = request.SuspiciousOnly
            };

            var invoices = LoadSavedInvoiceCandidates(request);
            result.HasUncertainSource = invoices.Any(x => x.SourceConfidence != "Reliable");
            if (result.HasUncertainSource)
            {
                result.SourceDetectionWarning = "مصدر بعض الفواتير غير مؤكد لأن الفواتير القديمة لم تكن موسومة كمستوردة من Excel";
            }

            var excelInvoices = invoices.Where(IsExcelSide).ToList();
            var normalInvoices = invoices.Where(IsNormalSide).ToList();
            result.DuplicatePairs = BuildDuplicatePairs(excelInvoices, normalInvoices)
                .OrderByDescending(x => x.MatchScore)
                .ThenBy(x => x.ExcelInvoiceDate)
                .ToList();
            if (request.SuspiciousOnly)
            {
                result.DuplicatePairs = result.DuplicatePairs
                    .Where(x => x.MatchClassification != "No Issue")
                    .ToList();
            }

            if (!string.IsNullOrWhiteSpace(request.RiskLevel))
            {
                result.DuplicatePairs = result.DuplicatePairs
                    .Where(x => string.Equals(PairRiskLevel(x), request.RiskLevel, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            result.TokenConflicts = result.DuplicatePairs
                .Where(x => !string.IsNullOrWhiteSpace(x.ExcelToken) && !string.IsNullOrWhiteSpace(x.NormalToken)
                    && (string.Equals(x.ExcelToken, x.NormalToken, StringComparison.OrdinalIgnoreCase)
                        || x.MatchClassification == "Token Conflict"))
                .ToList();
            ApplyInvoiceRisk(excelInvoices, normalInvoices, result.DuplicatePairs);
            result.ExcelInvoices = excelInvoices.Select(ToListItem).OrderByDescending(x => x.InvoiceDate).ThenByDescending(x => x.TransactionId).ToList();
            result.SystemInvoices = normalInvoices.Select(ToListItem).OrderByDescending(x => x.InvoiceDate).ThenByDescending(x => x.TransactionId).ToList();
            result.PeriodSummary = BuildPeriodSummary(excelInvoices, normalInvoices, result.DuplicatePairs);
            result.BranchSummaries = ApplySummaryFilters(BuildBranchSummaries(excelInvoices, normalInvoices, result.DuplicatePairs), request).ToList();
            result.DaySummaries = ApplySummaryFilters(BuildDaySummaries(excelInvoices, normalInvoices, result.DuplicatePairs), request).ToList();
            result.ServiceTypeSummaries = BuildDimensionSummaries(
                excelInvoices,
                normalInvoices,
                result.DuplicatePairs,
                x => NormalizeServiceType(x.ServiceType),
                x => string.IsNullOrWhiteSpace(x.ServiceType) ? "Unknown" : x.ServiceType,
                p => NormalizeServiceType(p.ServiceType),
                p => string.IsNullOrWhiteSpace(p.ServiceType) ? "Unknown" : p.ServiceType);
            result.TellerSummaries = BuildDimensionSummaries(
                excelInvoices,
                normalInvoices,
                result.DuplicatePairs,
                x => string.IsNullOrWhiteSpace(x.UserName) ? "Unknown" : x.UserName,
                x => string.IsNullOrWhiteSpace(x.UserName) ? "Unknown" : x.UserName,
                p => string.IsNullOrWhiteSpace(p.ExcelUserName) ? "Unknown" : p.ExcelUserName,
                p => string.IsNullOrWhiteSpace(p.ExcelUserName) ? "Unknown" : p.ExcelUserName);
            result.SourceSummaries = BuildSourceSummaries(excelInvoices, normalInvoices, result.DuplicatePairs);
            result.DayBranchSummaries = BuildDayBranchSummaries(excelInvoices, normalInvoices, result.DuplicatePairs);
            return result;
        }

        public byte[] BuildSavedAnalysisExcelReport(PosSavedInvoiceReconciliationResult result)
        {
            result = result ?? new PosSavedInvoiceReconciliationResult();
            using (var stream = new MemoryStream())
            {
                using (var document = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook))
                {
                    var workbookPart = document.AddWorkbookPart();
                    workbookPart.Workbook = new Workbook();
                    AddSavedPeriodSheet(workbookPart, result);
                    AddSavedBranchSummarySheet(workbookPart, result.BranchSummaries);
                    AddSavedDaySummarySheet(workbookPart, result.DaySummaries);
                    AddSavedDimensionSummarySheet(workbookPart, result.ServiceTypeSummaries, "ServiceTypes");
                    AddSavedDimensionSummarySheet(workbookPart, result.TellerSummaries, "Tellers");
                    AddSavedDimensionSummarySheet(workbookPart, result.SourceSummaries, "Sources");
                    AddSavedSummarySheet(workbookPart, result);
                    AddSavedPairsSheet(workbookPart, result.DuplicatePairs, "DuplicatePairs");
                    AddSavedPairsSheet(workbookPart, result.TokenConflicts, "TokenConflicts");
                    AddSavedPairsSheet(workbookPart, result.DuplicatePairs.Where(x => x.MatchClassification != "Confirmed Duplicate").ToList(), "NeedsReview");
                    workbookPart.Workbook.Save();
                }

                return stream.ToArray();
            }
        }

        private IList<SavedDbInvoice> LoadSavedInvoiceCandidates(PosInvoiceReconciliationRequest request)
        {
            var hasAuditTables = HasImportAuditTables();
            var sql = hasAuditTables ? SavedInvoiceSqlWithAudit : SavedInvoiceSqlWithoutAudit;
            var rows = new List<SavedDbInvoice>();
            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                command.CommandTimeout = 180;
                command.Parameters.Add("@fromDate", SqlDbType.DateTime).Value = request.FromDate.Date;
                command.Parameters.Add("@toDate", SqlDbType.DateTime).Value = request.ToDate.Date;
                command.Parameters.Add("@branchId", SqlDbType.Int).Value = request.BranchId.HasValue && request.BranchId.Value > 0 ? (object)request.BranchId.Value : DBNull.Value;
                command.Parameters.Add("@tellerUserId", SqlDbType.Int).Value = request.TellerUserId.HasValue && request.TellerUserId.Value > 0 ? (object)request.TellerUserId.Value : DBNull.Value;
                connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var invoice = new SavedDbInvoice
                        {
                            TransactionId = ReadInt(reader, "Transaction_ID").GetValueOrDefault(),
                            InvoiceDate = ReadDateTime(reader, "Transaction_Date"),
                            InvoiceNumber = ReadString(reader, "NoteSerial1"),
                            ManualNo = ReadString(reader, "ManualNO"),
                            ManualNo2 = ReadString(reader, "ManualNo2"),
                            NoId = ReadString(reader, "NoID"),
                            Token = NormalizeToken(ReadString(reader, "Token")),
                            Phone = NormalizePhone(ReadString(reader, "Phone")),
                            CustomerName = NormalizeText(ReadString(reader, "CustomerName")),
                            NationalId = OnlyDigits(ReadString(reader, "NationalId")),
                            Amount = ReadDecimal(reader, "Amount").GetValueOrDefault(),
                            NetAmount = ReadDecimal(reader, "NetAmount").GetValueOrDefault(),
                            PaidAmount = ReadDecimal(reader, "PaidAmount").GetValueOrDefault(),
                            ServiceType = NormalizeServiceType(ReadString(reader, "ServiceType")),
                            BranchId = ReadInt(reader, "BranchId"),
                            BranchName = ReadString(reader, "BranchName"),
                            UserId = ReadInt(reader, "UserID"),
                            UserName = ReadString(reader, "UserName"),
                            ImportBatchId = hasAuditTables ? ReadLong(reader, "ImportBatchId") : null,
                            SourceFileName = hasAuditTables ? ReadString(reader, "SourceFileName") : null,
                            SourceSheet = hasAuditTables ? ReadString(reader, "SourceSheet") : null,
                            SourceRow = hasAuditTables ? ReadInt(reader, "SourceRow") : null,
                            ImportRowStatus = hasAuditTables ? ReadString(reader, "ImportRowStatus") : null
                        };
                        ApplySource(invoice);
                        rows.Add(invoice);
                    }
                }
            }

            var source = (request.ImportSource ?? "All").Trim();
            return rows
                .Where(x => string.IsNullOrWhiteSpace(request.ServiceType) || string.Equals(x.ServiceType, NormalizeServiceType(request.ServiceType), StringComparison.OrdinalIgnoreCase))
                .Where(x => SourceMatches(x, source))
                .Where(x => FineFilterMatches(x, request))
                .ToList();
        }

        private static bool FineFilterMatches(SavedDbInvoice invoice, PosInvoiceReconciliationRequest request)
        {
            if (request.MinAmount.HasValue && invoice.Amount < request.MinAmount.Value)
            {
                return false;
            }

            if (request.MaxAmount.HasValue && invoice.Amount > request.MaxAmount.Value)
            {
                return false;
            }

            var token = NormalizeToken(request.Token);
            if (!string.IsNullOrWhiteSpace(token) && (invoice.Token ?? string.Empty).IndexOf(token, StringComparison.OrdinalIgnoreCase) < 0)
            {
                return false;
            }

            var phone = NormalizePhone(request.Phone);
            if (!string.IsNullOrWhiteSpace(phone) && (invoice.Phone ?? string.Empty).IndexOf(phone, StringComparison.OrdinalIgnoreCase) < 0)
            {
                return false;
            }

            var nationalId = OnlyDigits(request.NationalId);
            if (!string.IsNullOrWhiteSpace(nationalId) && (invoice.NationalId ?? string.Empty).IndexOf(nationalId, StringComparison.OrdinalIgnoreCase) < 0)
            {
                return false;
            }

            var customer = NormalizeArabicName(request.CustomerName);
            if (!string.IsNullOrWhiteSpace(customer) && NormalizeArabicName(invoice.CustomerName).IndexOf(customer, StringComparison.OrdinalIgnoreCase) < 0)
            {
                return false;
            }

            var search = NormalizeText(request.SearchTerm);
            if (!string.IsNullOrWhiteSpace(search))
            {
                var normalizedSearch = NormalizeToken(search);
                var phoneSearch = NormalizePhone(search);
                var nameSearch = NormalizeArabicName(search);
                var haystack = string.Join(" ", new[]
                {
                    invoice.TransactionId.ToString(CultureInfo.InvariantCulture),
                    invoice.InvoiceNumber,
                    invoice.ManualNo,
                    invoice.ManualNo2,
                    invoice.Token,
                    invoice.Phone,
                    invoice.NationalId,
                    invoice.CustomerName,
                    invoice.BranchName,
                    invoice.UserName
                });
                var normalizedHaystack = NormalizeToken(haystack);
                var nameHaystack = NormalizeArabicName(haystack);
                if (normalizedHaystack.IndexOf(normalizedSearch, StringComparison.OrdinalIgnoreCase) < 0
                    && (string.IsNullOrWhiteSpace(phoneSearch) || (invoice.Phone ?? string.Empty).IndexOf(phoneSearch, StringComparison.OrdinalIgnoreCase) < 0)
                    && nameHaystack.IndexOf(nameSearch, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    return false;
                }
            }

            return true;
        }

        private bool HasImportAuditTables()
        {
            const string sql = @"
SELECT CASE WHEN OBJECT_ID(N'dbo.POS_ImportBatch', N'U') IS NOT NULL
              AND OBJECT_ID(N'dbo.POS_ImportBatchRow', N'U') IS NOT NULL
            THEN 1 ELSE 0 END;";
            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                connection.Open();
                return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture) == 1;
            }
        }

        private static IList<PosSavedInvoiceDuplicatePair> BuildDuplicatePairs(IList<SavedDbInvoice> excelInvoices, IList<SavedDbInvoice> normalInvoices)
        {
            var normalByToken = IndexBy(normalInvoices, x => x.Token);
            var normalByPhone = IndexBy(normalInvoices, x => x.Phone);
            var normalByNational = IndexBy(normalInvoices, x => x.NationalId);
            var normalByCustomerAmount = IndexBy(normalInvoices, x => CustomerAmountKey(x.CustomerName, x.Amount));
            var normalByInvoiceDayBranch = IndexByMany(normalInvoices, InvoiceDayBranchKeys);
            var normalByAmountBranchService = IndexBy(normalInvoices, x => AmountBranchServiceKey(x.Amount, x.BranchId, x.ServiceType));
            var normalByAmountDay = normalInvoices
                .Where(x => x.InvoiceDate.HasValue)
                .GroupBy(x => AmountDayBranchKey(x.Amount, x.InvoiceDate.Value, x.BranchId, x.ServiceType))
                .ToDictionary(x => x.Key, x => x.ToList(), StringComparer.OrdinalIgnoreCase);

            var pairs = new Dictionary<string, PosSavedInvoiceDuplicatePair>(StringComparer.OrdinalIgnoreCase);
            foreach (var excel in excelInvoices)
            {
                var candidates = new Dictionary<int, SavedDbInvoice>();
                AddCandidates(candidates, normalByToken, excel.Token);
                AddCandidates(candidates, normalByPhone, excel.Phone);
                AddCandidates(candidates, normalByNational, excel.NationalId);
                AddCandidates(candidates, normalByCustomerAmount, CustomerAmountKey(excel.CustomerName, excel.Amount));
                foreach (var key in InvoiceDayBranchKeys(excel))
                {
                    AddCandidates(candidates, normalByInvoiceDayBranch, key);
                }

                AddCandidates(candidates, normalByAmountBranchService, AmountBranchServiceKey(excel.Amount, excel.BranchId, excel.ServiceType));
                if (excel.InvoiceDate.HasValue)
                {
                    AddCandidates(candidates, normalByAmountDay, AmountDayBranchKey(excel.Amount, excel.InvoiceDate.Value, excel.BranchId, excel.ServiceType));
                }

                foreach (var normal in candidates.Values)
                {
                    if (excel.TransactionId == normal.TransactionId)
                    {
                        continue;
                    }

                    var score = ScoreSavedPair(excel, normal);
                    if (score.Score < 10)
                    {
                        continue;
                    }

                    var key = excel.TransactionId.ToString(CultureInfo.InvariantCulture) + ":" + normal.TransactionId.ToString(CultureInfo.InvariantCulture);
                    pairs[key] = ToPair(excel, normal, score);
                }
            }

            return pairs.Values.ToList();
        }

        private static IList<PosSavedInvoiceDayBranchSummary> BuildDayBranchSummaries(IList<SavedDbInvoice> excelInvoices, IList<SavedDbInvoice> normalInvoices, IList<PosSavedInvoiceDuplicatePair> pairs)
        {
            var allKeys = excelInvoices.Concat(normalInvoices)
                .Where(x => x.InvoiceDate.HasValue)
                .Select(x => DayBranchServiceKey(x.InvoiceDate.Value, x.BranchId, x.ServiceType))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var summaries = new List<PosSavedInvoiceDayBranchSummary>();
            foreach (var key in allKeys)
            {
                var excel = excelInvoices.Where(x => x.InvoiceDate.HasValue && DayBranchServiceKey(x.InvoiceDate.Value, x.BranchId, x.ServiceType) == key).ToList();
                var normal = normalInvoices.Where(x => x.InvoiceDate.HasValue && DayBranchServiceKey(x.InvoiceDate.Value, x.BranchId, x.ServiceType) == key).ToList();
                if (excel.Count == 0 && normal.Count == 0)
                {
                    continue;
                }

                var sample = excel.FirstOrDefault() ?? normal.First();
                var dayPairs = pairs.Where(x => x.ExcelInvoiceDate.HasValue && x.ExcelInvoiceDate.Value.Date == sample.InvoiceDate.Value.Date
                    && string.Equals(x.BranchName, sample.BranchName, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(x.ServiceType, sample.ServiceType, StringComparison.OrdinalIgnoreCase)).ToList();
                var risk = ResolveRisk(dayPairs, excel, normal);
                summaries.Add(new PosSavedInvoiceDayBranchSummary
                {
                    Date = sample.InvoiceDate.Value.Date,
                    BranchId = sample.BranchId,
                    BranchName = sample.BranchName,
                    ServiceType = sample.ServiceType,
                    ExcelImportedCount = excel.Count,
                    ExcelImportedAmount = excel.Sum(x => x.Amount),
                    NormalPosCount = normal.Count,
                    NormalPosAmount = normal.Sum(x => x.Amount),
                    CountDifference = excel.Count - normal.Count,
                    AmountDifference = excel.Sum(x => x.Amount) - normal.Sum(x => x.Amount),
                    PotentialDuplicateMatches = dayPairs.Count,
                    RiskLevel = risk.Item1,
                    Reason = risk.Item2
                });
            }

            return summaries
                .OrderByDescending(x => RiskRank(x.RiskLevel))
                .ThenBy(x => x.Date)
                .ThenBy(x => x.BranchName)
                .ToList();
        }

        private static PosSavedInvoicePeriodSummary BuildPeriodSummary(IList<SavedDbInvoice> excelInvoices, IList<SavedDbInvoice> normalInvoices, IList<PosSavedInvoiceDuplicatePair> pairs)
        {
            var risk = ResolveRisk(pairs, excelInvoices, normalInvoices);
            var highRiskPairs = pairs.Count(x => x.MatchClassification == "Confirmed Duplicate" || x.MatchClassification == "Probable Duplicate" || x.MatchClassification == "Token Conflict");
            return new PosSavedInvoicePeriodSummary
            {
                ExcelImportedCount = excelInvoices.Count,
                ExcelImportedAmount = excelInvoices.Sum(x => x.Amount),
                NormalPosCount = normalInvoices.Count,
                NormalPosAmount = normalInvoices.Sum(x => x.Amount),
                CountDifference = excelInvoices.Count - normalInvoices.Count,
                AmountDifference = excelInvoices.Sum(x => x.Amount) - normalInvoices.Sum(x => x.Amount),
                SuspiciousDuplicatePairs = pairs.Count,
                HighRiskPairs = highRiskPairs,
                RiskLevel = risk.Item1,
                Reason = risk.Item2
            };
        }

        private static IList<PosSavedInvoiceBranchSummary> BuildBranchSummaries(IList<SavedDbInvoice> excelInvoices, IList<SavedDbInvoice> normalInvoices, IList<PosSavedInvoiceDuplicatePair> pairs)
        {
            var keys = excelInvoices.Concat(normalInvoices)
                .Select(x => x.BranchId.GetValueOrDefault().ToString(CultureInfo.InvariantCulture))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            var summaries = new List<PosSavedInvoiceBranchSummary>();
            foreach (var key in keys)
            {
                var excel = excelInvoices.Where(x => x.BranchId.GetValueOrDefault().ToString(CultureInfo.InvariantCulture) == key).ToList();
                var normal = normalInvoices.Where(x => x.BranchId.GetValueOrDefault().ToString(CultureInfo.InvariantCulture) == key).ToList();
                var sample = excel.FirstOrDefault() ?? normal.FirstOrDefault();
                if (sample == null)
                {
                    continue;
                }

                var branchPairs = pairs.Where(x => string.Equals(x.BranchName, sample.BranchName, StringComparison.OrdinalIgnoreCase)).ToList();
                var risk = ResolveRisk(branchPairs, excel, normal);
                summaries.Add(new PosSavedInvoiceBranchSummary
                {
                    BranchId = sample.BranchId,
                    BranchName = sample.BranchName,
                    ExcelImportedCount = excel.Count,
                    ExcelImportedAmount = excel.Sum(x => x.Amount),
                    NormalPosCount = normal.Count,
                    NormalPosAmount = normal.Sum(x => x.Amount),
                    CountDifference = excel.Count - normal.Count,
                    AmountDifference = excel.Sum(x => x.Amount) - normal.Sum(x => x.Amount),
                    SuspiciousDuplicatePairs = branchPairs.Count,
                    RiskLevel = risk.Item1,
                    Reason = risk.Item2
                });
            }

            return summaries.OrderByDescending(x => RiskRank(x.RiskLevel)).ThenBy(x => x.BranchName).ToList();
        }

        private static IList<PosSavedInvoiceDaySummary> BuildDaySummaries(IList<SavedDbInvoice> excelInvoices, IList<SavedDbInvoice> normalInvoices, IList<PosSavedInvoiceDuplicatePair> pairs)
        {
            var days = excelInvoices.Concat(normalInvoices)
                .Where(x => x.InvoiceDate.HasValue)
                .Select(x => x.InvoiceDate.Value.Date)
                .Distinct()
                .OrderBy(x => x)
                .ToList();
            var summaries = new List<PosSavedInvoiceDaySummary>();
            foreach (var day in days)
            {
                var excel = excelInvoices.Where(x => x.InvoiceDate.HasValue && x.InvoiceDate.Value.Date == day).ToList();
                var normal = normalInvoices.Where(x => x.InvoiceDate.HasValue && x.InvoiceDate.Value.Date == day).ToList();
                var dayPairs = pairs.Where(x => x.ExcelInvoiceDate.HasValue && x.ExcelInvoiceDate.Value.Date == day).ToList();
                var risk = ResolveRisk(dayPairs, excel, normal);
                summaries.Add(new PosSavedInvoiceDaySummary
                {
                    Date = day,
                    ExcelImportedCount = excel.Count,
                    ExcelImportedAmount = excel.Sum(x => x.Amount),
                    NormalPosCount = normal.Count,
                    NormalPosAmount = normal.Sum(x => x.Amount),
                    CountDifference = excel.Count - normal.Count,
                    AmountDifference = excel.Sum(x => x.Amount) - normal.Sum(x => x.Amount),
                    SuspiciousDuplicatePairs = dayPairs.Count,
                    RiskLevel = risk.Item1,
                    Reason = risk.Item2
                });
            }

            return summaries.OrderByDescending(x => RiskRank(x.RiskLevel)).ThenBy(x => x.Date).ToList();
        }

        private static IList<PosSavedInvoiceDimensionSummary> BuildDimensionSummaries(
            IList<SavedDbInvoice> excelInvoices,
            IList<SavedDbInvoice> normalInvoices,
            IList<PosSavedInvoiceDuplicatePair> pairs,
            Func<SavedDbInvoice, string> keySelector,
            Func<SavedDbInvoice, string> nameSelector,
            Func<PosSavedInvoiceDuplicatePair, string> pairKeySelector,
            Func<PosSavedInvoiceDuplicatePair, string> pairNameSelector)
        {
            var keys = excelInvoices.Concat(normalInvoices)
                .Select(x => SafeKey(keySelector(x)))
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            var summaries = new List<PosSavedInvoiceDimensionSummary>();
            foreach (var key in keys)
            {
                var excel = excelInvoices.Where(x => string.Equals(SafeKey(keySelector(x)), key, StringComparison.OrdinalIgnoreCase)).ToList();
                var normal = normalInvoices.Where(x => string.Equals(SafeKey(keySelector(x)), key, StringComparison.OrdinalIgnoreCase)).ToList();
                var groupedPairs = pairs.Where(x => string.Equals(SafeKey(pairKeySelector(x)), key, StringComparison.OrdinalIgnoreCase)).ToList();
                var sample = excel.FirstOrDefault() ?? normal.FirstOrDefault();
                var name = sample == null ? key : nameSelector(sample);
                if (string.IsNullOrWhiteSpace(name) && groupedPairs.Any())
                {
                    name = pairNameSelector(groupedPairs.First());
                }

                var risk = ResolveRisk(groupedPairs, excel, normal);
                summaries.Add(new PosSavedInvoiceDimensionSummary
                {
                    DimensionKey = key,
                    DimensionName = string.IsNullOrWhiteSpace(name) ? "Unknown" : name,
                    ExcelImportedCount = excel.Count,
                    ExcelImportedAmount = excel.Sum(x => x.Amount),
                    NormalPosCount = normal.Count,
                    NormalPosAmount = normal.Sum(x => x.Amount),
                    CountDifference = excel.Count - normal.Count,
                    AmountDifference = excel.Sum(x => x.Amount) - normal.Sum(x => x.Amount),
                    SuspiciousDuplicatePairs = groupedPairs.Count,
                    RiskLevel = risk.Item1,
                    Reason = risk.Item2
                });
            }

            return summaries.OrderByDescending(x => RiskRank(x.RiskLevel)).ThenBy(x => x.DimensionName).ToList();
        }

        private static IList<PosSavedInvoiceDimensionSummary> BuildSourceSummaries(IList<SavedDbInvoice> excelInvoices, IList<SavedDbInvoice> normalInvoices, IList<PosSavedInvoiceDuplicatePair> pairs)
        {
            var risk = ResolveRisk(pairs, excelInvoices, normalInvoices);
            return new List<PosSavedInvoiceDimensionSummary>
            {
                new PosSavedInvoiceDimensionSummary
                {
                    DimensionKey = "Excel",
                    DimensionName = "Excel Imported",
                    ExcelImportedCount = excelInvoices.Count,
                    ExcelImportedAmount = excelInvoices.Sum(x => x.Amount),
                    NormalPosCount = 0,
                    NormalPosAmount = 0m,
                    CountDifference = excelInvoices.Count,
                    AmountDifference = excelInvoices.Sum(x => x.Amount),
                    SuspiciousDuplicatePairs = pairs.Count,
                    RiskLevel = risk.Item1,
                    Reason = risk.Item2
                },
                new PosSavedInvoiceDimensionSummary
                {
                    DimensionKey = "Normal",
                    DimensionName = "Normal POS",
                    ExcelImportedCount = 0,
                    ExcelImportedAmount = 0m,
                    NormalPosCount = normalInvoices.Count,
                    NormalPosAmount = normalInvoices.Sum(x => x.Amount),
                    CountDifference = -normalInvoices.Count,
                    AmountDifference = -normalInvoices.Sum(x => x.Amount),
                    SuspiciousDuplicatePairs = pairs.Count,
                    RiskLevel = risk.Item1,
                    Reason = risk.Item2
                }
            };
        }

        private static string SafeKey(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? "Unknown" : value.Trim();
        }

        private static IEnumerable<PosSavedInvoiceBranchSummary> ApplySummaryFilters(IEnumerable<PosSavedInvoiceBranchSummary> summaries, PosInvoiceReconciliationRequest request)
        {
            var rows = summaries ?? new List<PosSavedInvoiceBranchSummary>();
            if (request.OnlyBothSources)
            {
                rows = rows.Where(x => x.ExcelImportedCount > 0 && x.NormalPosCount > 0);
            }

            if (request.SuspiciousOnly)
            {
                rows = rows.Where(x => x.SuspiciousDuplicatePairs > 0 || RiskRank(x.RiskLevel) >= 3);
            }

            if (!string.IsNullOrWhiteSpace(request.RiskLevel))
            {
                rows = rows.Where(x => string.Equals(x.RiskLevel, request.RiskLevel, StringComparison.OrdinalIgnoreCase));
            }

            return rows;
        }

        private static IEnumerable<PosSavedInvoiceDaySummary> ApplySummaryFilters(IEnumerable<PosSavedInvoiceDaySummary> summaries, PosInvoiceReconciliationRequest request)
        {
            var rows = summaries ?? new List<PosSavedInvoiceDaySummary>();
            if (request.OnlyBothSources)
            {
                rows = rows.Where(x => x.ExcelImportedCount > 0 && x.NormalPosCount > 0);
            }

            if (request.SuspiciousOnly)
            {
                rows = rows.Where(x => x.SuspiciousDuplicatePairs > 0 || RiskRank(x.RiskLevel) >= 3);
            }

            if (!string.IsNullOrWhiteSpace(request.RiskLevel))
            {
                rows = rows.Where(x => string.Equals(x.RiskLevel, request.RiskLevel, StringComparison.OrdinalIgnoreCase));
            }

            return rows;
        }

        private static void ApplyInvoiceRisk(IList<SavedDbInvoice> excelInvoices, IList<SavedDbInvoice> normalInvoices, IList<PosSavedInvoiceDuplicatePair> pairs)
        {
            foreach (var invoice in excelInvoices.Concat(normalInvoices))
            {
                invoice.RiskLevel = "Low";
                invoice.ReviewStatus = "No Issue";
            }

            foreach (var pair in pairs)
            {
                MarkInvoiceRisk(excelInvoices, pair.ExcelTransactionId, PairRiskLevel(pair), pair.MatchClassification);
                MarkInvoiceRisk(normalInvoices, pair.NormalTransactionId, PairRiskLevel(pair), pair.MatchClassification);
            }
        }

        private static void MarkInvoiceRisk(IList<SavedDbInvoice> invoices, int transactionId, string risk, string status)
        {
            var invoice = invoices.FirstOrDefault(x => x.TransactionId == transactionId);
            if (invoice == null)
            {
                return;
            }

            if (RiskRank(risk) >= RiskRank(invoice.RiskLevel))
            {
                invoice.RiskLevel = risk;
                invoice.ReviewStatus = status;
            }
        }

        private static string PairRiskLevel(PosSavedInvoiceDuplicatePair pair)
        {
            if (pair == null) return "Low";
            if (pair.MatchClassification == "Confirmed Duplicate" || pair.MatchClassification == "Token Conflict") return "Critical";
            if (pair.MatchClassification == "Probable Duplicate" || pair.MatchClassification == "Amount Mismatch") return "High";
            if (pair.MatchClassification == "Possible Duplicate" || pair.MatchClassification == "Needs Review" || pair.MatchClassification == "Phone Conflict") return "Medium";
            return "Low";
        }

        private static PosSavedInvoiceListItem ToListItem(SavedDbInvoice invoice)
        {
            return new PosSavedInvoiceListItem
            {
                TransactionId = invoice.TransactionId,
                InvoiceDate = invoice.InvoiceDate,
                Serial = invoice.InvoiceNumber,
                ManualNo = FirstNonEmpty(invoice.ManualNo, invoice.ManualNo2),
                Token = invoice.Token,
                Phone = invoice.Phone,
                CustomerName = invoice.CustomerName,
                NationalId = invoice.NationalId,
                Amount = invoice.Amount,
                ServiceType = invoice.ServiceType,
                BranchId = invoice.BranchId,
                BranchName = invoice.BranchName,
                UserId = invoice.UserId,
                UserName = invoice.UserName,
                Source = invoice.Source,
                SourceConfidence = invoice.SourceConfidence,
                ImportBatchId = invoice.ImportBatchId,
                SourceFileName = invoice.SourceFileName,
                SourceSheet = invoice.SourceSheet,
                SourceRow = invoice.SourceRow,
                RiskLevel = invoice.RiskLevel,
                ReviewStatus = invoice.ReviewStatus
            };
        }

        private static PairScore ScoreSavedPair(SavedDbInvoice excel, SavedDbInvoice normal)
        {
            var score = 0;
            var reasons = new List<string>();
            var warnings = new List<string>();
            var sameDay = SameDate(excel.InvoiceDate, normal.InvoiceDate);
            var closeTime = TimeDiffMinutes(excel.InvoiceDate, normal.InvoiceDate).GetValueOrDefault(999999) <= 30;
            var sameAmount = Math.Abs(excel.Amount - normal.Amount) <= 0.02m;
            var closeAmount = Math.Abs(excel.Amount - normal.Amount) <= 5m;
            var sameInvoiceNumber = InvoiceKeys(excel).Any(x => InvoiceKeys(normal).Any(y => SameKey(x, y)));

            AddScore(!string.IsNullOrWhiteSpace(excel.Token) && excel.Token == normal.Token, 80, "Same token", ref score, reasons);
            AddScore(!string.IsNullOrWhiteSpace(excel.NationalId) && excel.NationalId == normal.NationalId && !string.IsNullOrWhiteSpace(excel.Phone) && excel.Phone == normal.Phone, 75, "Same national ID + phone", ref score, reasons);
            AddScore(sameInvoiceNumber && sameDay && excel.BranchId == normal.BranchId, 70, "Same manual/IPN + date + branch", ref score, reasons);
            AddScore(!string.IsNullOrWhiteSpace(excel.Phone) && excel.Phone == normal.Phone && sameAmount && sameDay, 60, "Same phone + amount + date", ref score, reasons);
            AddScore(!string.IsNullOrWhiteSpace(excel.NationalId) && excel.NationalId == normal.NationalId && sameAmount && sameDay, 58, "Same national ID + amount + date", ref score, reasons);
            AddScore(!string.IsNullOrWhiteSpace(excel.Token) && excel.Token == normal.Token && !sameAmount, 55, "Same token but amount differs", ref score, reasons);
            AddScore(SimilarName(excel.CustomerName, normal.CustomerName) && !string.IsNullOrWhiteSpace(excel.Phone) && excel.Phone == normal.Phone, 35, "Same normalized customer name + phone", ref score, reasons);
            AddScore(!string.IsNullOrWhiteSpace(excel.Phone) && excel.Phone == normal.Phone && sameAmount, 45, "Same phone + amount within selected period", ref score, reasons);
            AddScore(SimilarName(excel.CustomerName, normal.CustomerName) && sameAmount, 42, "Same customer name + amount", ref score, reasons);
            AddScore(sameAmount && SameService(excel.ServiceType, normal.ServiceType) && excel.BranchId == normal.BranchId && closeTime, 30, "Same amount + service + branch + close time", ref score, reasons);
            AddScore(!string.IsNullOrWhiteSpace(excel.Phone) && excel.Phone == normal.Phone && closeAmount, 28, "Same phone + close amount", ref score, reasons);
            AddScore(sameAmount && excel.BranchId == normal.BranchId && sameDay, 12, "Same amount only in same branch/day", ref score, reasons);
            AddScore(SimilarName(excel.CustomerName, normal.CustomerName), 10, "Similar customer name only", ref score, reasons);
            AddScore(excel.UserId.HasValue && excel.UserId == normal.UserId && sameAmount && SameService(excel.ServiceType, normal.ServiceType) && sameDay, 18, "Same teller + amount + service + day", ref score, reasons);

            if (!sameAmount && (!string.IsNullOrWhiteSpace(excel.Token) && excel.Token == normal.Token || !string.IsNullOrWhiteSpace(excel.Phone) && excel.Phone == normal.Phone))
            {
                warnings.Add("Amount differs for a likely same transaction");
            }

            if (!SimilarName(excel.CustomerName, normal.CustomerName) && (!string.IsNullOrWhiteSpace(excel.Token) && excel.Token == normal.Token || !string.IsNullOrWhiteSpace(excel.Phone) && excel.Phone == normal.Phone))
            {
                warnings.Add("Customer name differs for a matching token/phone");
            }

            return new PairScore
            {
                Score = Math.Min(100, score),
                Classification = ClassifySavedPair(excel, normal, score, sameAmount),
                Reasons = reasons,
                Warnings = warnings
            };
        }

        private static string ClassifySavedPair(SavedDbInvoice excel, SavedDbInvoice normal, int score, bool sameAmount)
        {
            if (!string.IsNullOrWhiteSpace(excel.Token) && !string.IsNullOrWhiteSpace(normal.Token) && excel.Token == normal.Token)
            {
                return sameAmount ? "Confirmed Duplicate" : "Token Conflict";
            }

            if (score >= 80) return "Confirmed Duplicate";
            if (score >= 60) return "Probable Duplicate";
            if (score >= 45) return sameAmount ? "Possible Duplicate" : "Amount Mismatch";
            if (!string.IsNullOrWhiteSpace(excel.Phone) && excel.Phone == normal.Phone && !sameAmount) return "Phone Conflict";
            if (score >= 20) return "Needs Review";
            return "No Issue";
        }

        private static PosSavedInvoiceDuplicatePair ToPair(SavedDbInvoice excel, SavedDbInvoice normal, PairScore score)
        {
            return new PosSavedInvoiceDuplicatePair
            {
                ExcelTransactionId = excel.TransactionId,
                ExcelInvoiceDate = excel.InvoiceDate,
                ExcelSerial = FirstNonEmpty(excel.ManualNo, excel.InvoiceNumber),
                ExcelImportBatchId = excel.ImportBatchId,
                ExcelSourceFileName = excel.SourceFileName,
                ExcelSourceSheet = excel.SourceSheet,
                ExcelSourceRow = excel.SourceRow,
                ExcelSource = excel.Source,
                ExcelSourceConfidence = excel.SourceConfidence,
                NormalTransactionId = normal.TransactionId,
                NormalInvoiceDate = normal.InvoiceDate,
                NormalSerial = FirstNonEmpty(normal.ManualNo, normal.InvoiceNumber),
                NormalSource = normal.Source,
                NormalSourceConfidence = normal.SourceConfidence,
                ServiceType = excel.ServiceType,
                BranchName = excel.BranchName,
                ExcelUserName = excel.UserName,
                NormalUserName = normal.UserName,
                ExcelToken = excel.Token,
                NormalToken = normal.Token,
                ExcelPhone = excel.Phone,
                NormalPhone = normal.Phone,
                ExcelNationalId = excel.NationalId,
                NormalNationalId = normal.NationalId,
                ExcelCustomerName = excel.CustomerName,
                NormalCustomerName = normal.CustomerName,
                ExcelAmount = excel.Amount,
                NormalAmount = normal.Amount,
                AmountDifference = excel.Amount - normal.Amount,
                TimeDifferenceMinutes = TimeDiffMinutes(excel.InvoiceDate, normal.InvoiceDate),
                MatchScore = score.Score,
                MatchClassification = score.Classification,
                Reasons = score.Reasons,
                Warnings = score.Warnings
            };
        }

        private IList<PosExcelInvoiceRow> ReadExcelRows(Stream stream, PosInvoiceReconciliationResult result)
        {
            var rows = new List<PosExcelInvoiceRow>();
            using (var reader = ExcelReaderFactory.CreateReader(stream))
            {
                var dataSet = reader.AsDataSet(new ExcelDataSetConfiguration
                {
                    ConfigureDataTable = _ => new ExcelDataTableConfiguration { UseHeaderRow = false }
                });

                foreach (DataTable sheet in dataSet.Tables)
                {
                    if (sheet.Rows.Count == 0)
                    {
                        continue;
                    }

                    var headerRowIndex = DetectHeaderRow(sheet);
                    if (headerRowIndex < 0)
                    {
                        continue;
                    }

                    var headers = ReadHeaders(sheet, headerRowIndex);
                    var mapping = DetectMapping(headers);
                    MergeMappings(result.ColumnMappings, mapping);

                    for (var rowIndex = headerRowIndex + 1; rowIndex < sheet.Rows.Count; rowIndex++)
                    {
                        if (IsEmptyRow(sheet.Rows[rowIndex]))
                        {
                            continue;
                        }

                        var row = new PosExcelInvoiceRow { SheetName = sheet.TableName, RowNumber = rowIndex + 1 };
                        foreach (var field in CanonicalFields)
                        {
                            PosInvoiceReconciliationColumnMapping mapped;
                            if (mapping.TryGetValue(field, out mapped) && mapped.ColumnIndex.HasValue && mapped.ColumnIndex.Value < sheet.Columns.Count)
                            {
                                row.Values[field] = CellText(sheet.Rows[rowIndex][mapped.ColumnIndex.Value]);
                            }
                        }

                        if (row.Values.Values.Any(x => !string.IsNullOrWhiteSpace(x)))
                        {
                            rows.Add(row);
                        }
                    }
                }
            }

            return rows;
        }

        private const string SavedInvoiceSqlWithAudit = @"
SELECT
    t.Transaction_ID,
    t.Transaction_Date,
    t.NoteSerial1,
    t.ManualNO,
    t.ManualNo2,
    t.NoID,
    NULLIF(LTRIM(RTRIM(COALESCE(t.VisaNumber, td.ItemSerial, k.CardNo, N''))), N'') AS Token,
    NULLIF(LTRIM(RTRIM(COALESCE(t.CashCustomerPhone, t.Phone2, k.PhoneNo2, k.tel, N''))), N'') AS Phone,
    NULLIF(LTRIM(RTRIM(COALESCE(t.CashCustomerName, k.name, N''))), N'') AS CustomerName,
    NULLIF(LTRIM(RTRIM(COALESCE(k.Tet_NumPoket, N''))), N'') AS NationalId,
    CAST(CASE
        WHEN ISNULL(t.TrafficViolations, 0) = 1 THEN ISNULL(t.PayedValue, 0)
        WHEN NULLIF(LTRIM(RTRIM(ISNULL(t.VisaNumber, N''))), N'') IS NOT NULL THEN ISNULL(t.PayedValue, 0)
        ELSE COALESCE(NULLIF(t.PayedValue, 0), NULLIF(t.RechargeValue, 0), NULLIF(t.NetValue, 0), NULLIF(t.Transaction_NetValue, 0), 0)
    END AS DECIMAL(18, 4)) AS Amount,
    CAST(COALESCE(t.NetValue, t.Transaction_NetValue, 0) AS DECIMAL(18, 4)) AS NetAmount,
    CAST(COALESCE(t.PayedValue, 0) AS DECIMAL(18, 4)) AS PaidAmount,
    CASE
        WHEN ISNULL(t.TrafficViolations, 0) = 1 THEN N'Violations'
        WHEN NULLIF(LTRIM(RTRIM(ISNULL(t.VisaNumber, N''))), N'') IS NOT NULL OR NULLIF(LTRIM(RTRIM(ISNULL(td.ItemSerial, N''))), N'') IS NOT NULL THEN N'Card'
        WHEN ISNULL(t.IsCashOut, 0) = 1 THEN N'Cash Out'
        ELSE N'Cash In'
    END AS ServiceType,
    t.BranchId,
    COALESCE(NULLIF(b.branch_name, N''), NULLIF(b.branch_namee, N''), CONVERT(NVARCHAR(50), t.BranchId)) AS BranchName,
    t.UserID,
    u.UserName,
    importRow.BatchId AS ImportBatchId,
    importRow.SourceFileName,
    importRow.SourceSheet,
    importRow.SourceRow,
    importRow.Status AS ImportRowStatus
FROM dbo.Transactions t
LEFT JOIN (
    SELECT Transaction_ID, MAX(NULLIF(LTRIM(RTRIM(ItemSerial)), N'')) AS ItemSerial
    FROM dbo.Transaction_Details
    GROUP BY Transaction_ID
) td ON td.Transaction_ID = t.Transaction_ID
LEFT JOIN dbo.TblCusCsh k
    ON (NULLIF(LTRIM(RTRIM(ISNULL(k.CardNo, N''))), N'') = NULLIF(LTRIM(RTRIM(COALESCE(t.VisaNumber, td.ItemSerial, N''))), N''))
    OR (NULLIF(LTRIM(RTRIM(ISNULL(k.PhoneNo2, N''))), N'') = NULLIF(LTRIM(RTRIM(ISNULL(t.CashCustomerPhone, N''))), N''))
LEFT JOIN dbo.TblBranchesData b ON b.branch_id = t.BranchId
LEFT JOIN dbo.TblUsers u ON u.UserID = t.UserID
OUTER APPLY
(
    SELECT TOP (1)
        r.BatchId,
        r.SourceSheet,
        r.SourceRow,
        r.Status,
        b.SourceFileName
    FROM dbo.POS_ImportBatchRow r
    INNER JOIN dbo.POS_ImportBatch b ON b.BatchId = r.BatchId
    WHERE r.TransactionId = t.Transaction_ID
      AND r.Status IN (N'Imported', N'ImportedWithWarnings')
    ORDER BY r.RowId DESC
) importRow
WHERE t.Transaction_Type = 21
  AND ISNULL(t.IsCancelled, 0) = 0
  AND t.Transaction_Date >= @fromDate
  AND t.Transaction_Date < DATEADD(DAY, 1, @toDate)
  AND (@branchId IS NULL OR t.BranchId = @branchId)
  AND (@tellerUserId IS NULL OR t.UserID = @tellerUserId);";

        private const string SavedInvoiceSqlWithoutAudit = @"
SELECT
    t.Transaction_ID,
    t.Transaction_Date,
    t.NoteSerial1,
    t.ManualNO,
    t.ManualNo2,
    t.NoID,
    NULLIF(LTRIM(RTRIM(COALESCE(t.VisaNumber, td.ItemSerial, k.CardNo, N''))), N'') AS Token,
    NULLIF(LTRIM(RTRIM(COALESCE(t.CashCustomerPhone, t.Phone2, k.PhoneNo2, k.tel, N''))), N'') AS Phone,
    NULLIF(LTRIM(RTRIM(COALESCE(t.CashCustomerName, k.name, N''))), N'') AS CustomerName,
    NULLIF(LTRIM(RTRIM(COALESCE(k.Tet_NumPoket, N''))), N'') AS NationalId,
    CAST(COALESCE(NULLIF(t.PayedValue, 0), NULLIF(t.RechargeValue, 0), NULLIF(t.NetValue, 0), NULLIF(t.Transaction_NetValue, 0), 0) AS DECIMAL(18, 4)) AS Amount,
    CAST(COALESCE(t.NetValue, t.Transaction_NetValue, 0) AS DECIMAL(18, 4)) AS NetAmount,
    CAST(COALESCE(t.PayedValue, 0) AS DECIMAL(18, 4)) AS PaidAmount,
    CASE
        WHEN ISNULL(t.TrafficViolations, 0) = 1 THEN N'Violations'
        WHEN NULLIF(LTRIM(RTRIM(ISNULL(t.VisaNumber, N''))), N'') IS NOT NULL OR NULLIF(LTRIM(RTRIM(ISNULL(td.ItemSerial, N''))), N'') IS NOT NULL THEN N'Card'
        WHEN ISNULL(t.IsCashOut, 0) = 1 THEN N'Cash Out'
        ELSE N'Cash In'
    END AS ServiceType,
    t.BranchId,
    COALESCE(NULLIF(b.branch_name, N''), NULLIF(b.branch_namee, N''), CONVERT(NVARCHAR(50), t.BranchId)) AS BranchName,
    t.UserID,
    u.UserName
FROM dbo.Transactions t
LEFT JOIN (
    SELECT Transaction_ID, MAX(NULLIF(LTRIM(RTRIM(ItemSerial)), N'')) AS ItemSerial
    FROM dbo.Transaction_Details
    GROUP BY Transaction_ID
) td ON td.Transaction_ID = t.Transaction_ID
LEFT JOIN dbo.TblCusCsh k
    ON (NULLIF(LTRIM(RTRIM(ISNULL(k.CardNo, N''))), N'') = NULLIF(LTRIM(RTRIM(COALESCE(t.VisaNumber, td.ItemSerial, N''))), N''))
    OR (NULLIF(LTRIM(RTRIM(ISNULL(k.PhoneNo2, N''))), N'') = NULLIF(LTRIM(RTRIM(ISNULL(t.CashCustomerPhone, N''))), N''))
LEFT JOIN dbo.TblBranchesData b ON b.branch_id = t.BranchId
LEFT JOIN dbo.TblUsers u ON u.UserID = t.UserID
WHERE t.Transaction_Type = 21
  AND ISNULL(t.IsCancelled, 0) = 0
  AND t.Transaction_Date >= @fromDate
  AND t.Transaction_Date < DATEADD(DAY, 1, @toDate)
  AND (@branchId IS NULL OR t.BranchId = @branchId)
  AND (@tellerUserId IS NULL OR t.UserID = @tellerUserId);";

        private static int DetectHeaderRow(DataTable sheet)
        {
            var maxRows = Math.Min(sheet.Rows.Count, 15);
            var bestRow = -1;
            var bestScore = 0;
            for (var i = 0; i < maxRows; i++)
            {
                var headers = ReadHeaders(sheet, i);
                var score = DetectMapping(headers).Count(x => x.Value.ColumnIndex.HasValue);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestRow = i;
                }
            }

            return bestScore >= 2 ? bestRow : -1;
        }

        private static IDictionary<string, PosInvoiceReconciliationColumnMapping> DetectMapping(IList<string> headers)
        {
            var result = CanonicalFields.ToDictionary(x => x, x => new PosInvoiceReconciliationColumnMapping { FieldKey = x, FieldTitle = FieldTitle(x), Confidence = "Missing" }, StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < headers.Count; i++)
            {
                var header = NormalizeHeader(headers[i]);
                if (string.IsNullOrWhiteSpace(header))
                {
                    continue;
                }

                var field = DetectField(header);
                if (field == null || result[field].ColumnIndex.HasValue)
                {
                    continue;
                }

                result[field].ColumnIndex = i;
                result[field].HeaderText = headers[i];
                result[field].Confidence = "Auto";
            }

            return result;
        }

        private static string DetectField(string header)
        {
            if (ContainsAny(header, "date", "invoice date", "تاريخ", "التاريخ")) return "InvoiceDate";
            if (ContainsAny(header, "invoice number", "manual", "ipn", "serial", "رقم الفاتورة", "رقم", "فاتورة")) return "InvoiceNumber";
            if (ContainsAny(header, "token", "card no", "card number", "توكن", "التوكن", "كارت", "بطاقة")) return "Token";
            if (ContainsAny(header, "phone", "mobile", "wallet", "هاتف", "الهاتف", "موبايل", "الموبايل", "تليفون")) return "Phone";
            if (ContainsAny(header, "customer", "client", "name", "اسم العميل", "العميل")) return "CustomerName";
            if (ContainsAny(header, "national", "nid", "id number", "رقم قومي", "الرقم القومي", "بطاقة شخصية")) return "NationalId";
            if (ContainsAny(header, "amount", "value", "total", "net", "gross", "مبلغ", "المبلغ", "قيمة", "اجمالي", "إجمالي")) return "Amount";
            if (ContainsAny(header, "service", "type", "operation", "نوع الخدمة", "الخدمة", "نوع العملية")) return "ServiceType";
            if (ContainsAny(header, "branch", "فرع", "الفرع")) return "Branch";
            if (ContainsAny(header, "store", "safe", "box", "خزنة", "الخزنة", "مخزن", "المخزن")) return "Store";
            if (ContainsAny(header, "user", "cashier", "employee", "مستخدم", "المستخدم", "كاشير")) return "User";
            if (ContainsAny(header, "note", "notes", "remark", "ملاحظ", "بيان")) return "Notes";
            return null;
        }

        private static void MergeMappings(IList<PosInvoiceReconciliationColumnMapping> destination, IDictionary<string, PosInvoiceReconciliationColumnMapping> source)
        {
            foreach (var item in source.Values)
            {
                if (destination.Any(x => string.Equals(x.FieldKey, item.FieldKey, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                destination.Add(item);
            }
        }

        private static PosExcelInvoiceNormalizedRow NormalizeRow(PosExcelInvoiceRow row)
        {
            var normalized = new PosExcelInvoiceNormalizedRow
            {
                SheetName = row.SheetName,
                RowNumber = row.RowNumber,
                InvoiceDateText = GetValue(row, "InvoiceDate"),
                InvoiceNumber = NormalizeText(GetValue(row, "InvoiceNumber")),
                Token = NormalizeToken(GetValue(row, "Token")),
                Phone = NormalizePhone(GetValue(row, "Phone")),
                CustomerName = NormalizeText(GetValue(row, "CustomerName")),
                NationalId = OnlyDigits(GetValue(row, "NationalId")),
                ServiceType = NormalizeServiceType(GetValue(row, "ServiceType")),
                Branch = NormalizeText(GetValue(row, "Branch")),
                Store = NormalizeText(GetValue(row, "Store")),
                User = NormalizeText(GetValue(row, "User")),
                Notes = NormalizeText(GetValue(row, "Notes"))
            };

            normalized.InvoiceDate = ParseDate(normalized.InvoiceDateText);
            normalized.Amount = ParseDecimal(GetValue(row, "Amount"));

            if (!normalized.InvoiceDate.HasValue)
            {
                normalized.Errors.Add("Invalid or missing invoice date");
            }

            if (!normalized.Amount.HasValue || normalized.Amount.Value <= 0)
            {
                normalized.Errors.Add("Invalid or missing amount");
            }

            if (string.IsNullOrWhiteSpace(normalized.Token)
                && string.IsNullOrWhiteSpace(normalized.Phone)
                && string.IsNullOrWhiteSpace(normalized.InvoiceNumber)
                && string.IsNullOrWhiteSpace(normalized.NationalId))
            {
                normalized.Errors.Add("No matching key found: token, phone, invoice number, or national ID is required");
            }

            if (string.IsNullOrWhiteSpace(normalized.ServiceType))
            {
                normalized.Warnings.Add("Service type is missing");
            }

            return normalized;
        }

        private IList<PosInvoiceDbMatch> LoadDbCandidates(PosInvoiceReconciliationRequest request)
        {
            var rows = new List<PosInvoiceDbMatch>();
            const string sql = @"
SELECT
    t.Transaction_ID,
    t.Transaction_Date,
    t.NoteSerial1,
    t.ManualNO,
    NULLIF(LTRIM(RTRIM(COALESCE(t.VisaNumber, td.ItemSerial, k.CardNo, N''))), N'') AS Token,
    NULLIF(LTRIM(RTRIM(COALESCE(t.CashCustomerPhone, t.Phone2, k.PhoneNo2, k.tel, N''))), N'') AS Phone,
    NULLIF(LTRIM(RTRIM(COALESCE(t.CashCustomerName, k.name, N''))), N'') AS CustomerName,
    NULLIF(LTRIM(RTRIM(COALESCE(k.Tet_NumPoket, N''))), N'') AS NationalId,
    CAST(COALESCE(NULLIF(t.PayedValue, 0), NULLIF(t.RechargeValue, 0), NULLIF(t.NetValue, 0), NULLIF(t.Transaction_NetValue, 0), 0) AS DECIMAL(18, 4)) AS Amount,
    CAST(COALESCE(t.NetValue, t.Transaction_NetValue, 0) AS DECIMAL(18, 4)) AS NetAmount,
    CAST(COALESCE(t.PayedValue, 0) AS DECIMAL(18, 4)) AS PaidAmount,
    CASE
        WHEN ISNULL(t.TrafficViolations, 0) = 1 THEN N'Violations'
        WHEN NULLIF(LTRIM(RTRIM(ISNULL(t.VisaNumber, N''))), N'') IS NOT NULL OR NULLIF(LTRIM(RTRIM(ISNULL(td.ItemSerial, N''))), N'') IS NOT NULL THEN N'Card'
        WHEN ISNULL(t.IsCashOut, 0) = 1 THEN N'Cash Out'
        ELSE N'Cash In'
    END AS ServiceType,
    t.BranchId,
    COALESCE(NULLIF(b.branch_name, N''), NULLIF(b.branch_namee, N''), CONVERT(NVARCHAR(50), t.BranchId)) AS BranchName,
    COALESCE(NULLIF(s.StoreName, N''), NULLIF(s.StoreNamee, N''), CONVERT(NVARCHAR(50), t.StoreID)) AS StoreName,
    u.UserName,
    ISNULL(td.DetailRows, 0) AS DetailRows,
    ISNULL(td.TokenDetailRows, 0) AS TokenDetailRows,
    CASE WHEN EXISTS (SELECT 1 FROM dbo.Transactions issue WHERE issue.ReturnID = t.Transaction_ID AND issue.Transaction_Type IN (11, 993) AND ISNULL(issue.IsCancelled, 0) = 0) THEN 1 ELSE 0 END AS HasIssueVoucher,
    CASE WHEN t.Transaction_Type IN (11, 993) THEN 1 ELSE 0 END AS IsIssueVoucher
FROM dbo.Transactions t
LEFT JOIN (
    SELECT
        Transaction_ID,
        MAX(NULLIF(LTRIM(RTRIM(ItemSerial)), N'')) AS ItemSerial,
        COUNT(1) AS DetailRows,
        SUM(CASE WHEN NULLIF(LTRIM(RTRIM(ISNULL(ItemSerial, N''))), N'') IS NOT NULL THEN 1 ELSE 0 END) AS TokenDetailRows
    FROM dbo.Transaction_Details
    GROUP BY Transaction_ID
) td ON td.Transaction_ID = t.Transaction_ID
LEFT JOIN dbo.TblCusCsh k
    ON (NULLIF(LTRIM(RTRIM(ISNULL(k.CardNo, N''))), N'') = NULLIF(LTRIM(RTRIM(COALESCE(t.VisaNumber, td.ItemSerial, N''))), N''))
    OR (NULLIF(LTRIM(RTRIM(ISNULL(k.PhoneNo2, N''))), N'') = NULLIF(LTRIM(RTRIM(ISNULL(t.CashCustomerPhone, N''))), N''))
LEFT JOIN dbo.TblBranchesData b ON b.branch_id = t.BranchId
LEFT JOIN dbo.TblStore s ON s.StoreID = t.StoreID
LEFT JOIN dbo.TblUsers u ON u.UserID = t.UserID
WHERE t.Transaction_Type IN (21, 11, 993)
  AND ISNULL(t.IsCancelled, 0) = 0
  AND t.Transaction_Date >= @fromDate
  AND t.Transaction_Date < DATEADD(DAY, 1, @toDate)
  AND (@branchId IS NULL OR t.BranchId = @branchId)
  AND (@canSeeAllBranches = 1 OR t.BranchId = @userBranchId);";

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                command.CommandTimeout = 120;
                command.Parameters.Add("@fromDate", SqlDbType.DateTime).Value = request.FromDate.Date.AddDays(-1);
                command.Parameters.Add("@toDate", SqlDbType.DateTime).Value = request.ToDate.Date.AddDays(1);
                command.Parameters.Add("@branchId", SqlDbType.Int).Value = request.BranchId.HasValue && request.BranchId.Value > 0 ? (object)request.BranchId.Value : DBNull.Value;
                command.Parameters.Add("@canSeeAllBranches", SqlDbType.Bit).Value = request.CanSeeAllBranches;
                command.Parameters.Add("@userBranchId", SqlDbType.Int).Value = request.BranchId.HasValue ? (object)request.BranchId.Value : DBNull.Value;
                connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        rows.Add(new PosInvoiceDbMatch
                        {
                            TransactionId = ReadInt(reader, "Transaction_ID").GetValueOrDefault(),
                            InvoiceDate = ReadDateTime(reader, "Transaction_Date"),
                            InvoiceNumber = ReadString(reader, "NoteSerial1"),
                            ManualNo = ReadString(reader, "ManualNO"),
                            Token = NormalizeToken(ReadString(reader, "Token")),
                            Phone = NormalizePhone(ReadString(reader, "Phone")),
                            CustomerName = NormalizeText(ReadString(reader, "CustomerName")),
                            NationalId = OnlyDigits(ReadString(reader, "NationalId")),
                            Amount = ReadDecimal(reader, "Amount").GetValueOrDefault(),
                            NetAmount = ReadDecimal(reader, "NetAmount").GetValueOrDefault(),
                            PaidAmount = ReadDecimal(reader, "PaidAmount").GetValueOrDefault(),
                            ServiceType = NormalizeServiceType(ReadString(reader, "ServiceType")),
                            BranchId = ReadInt(reader, "BranchId"),
                            BranchName = ReadString(reader, "BranchName"),
                            StoreName = ReadString(reader, "StoreName"),
                            UserName = ReadString(reader, "UserName"),
                            DetailRows = ReadInt(reader, "DetailRows").GetValueOrDefault(),
                            TokenDetailRows = ReadInt(reader, "TokenDetailRows").GetValueOrDefault(),
                            HasIssueVoucher = ReadBoolean(reader, "HasIssueVoucher"),
                            IsIssueVoucher = ReadBoolean(reader, "IsIssueVoucher")
                        });
                    }
                }
            }

            return rows
                .Where(x => string.IsNullOrWhiteSpace(request.ServiceType) || string.Equals(x.ServiceType, NormalizeServiceType(request.ServiceType), StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        private static PosInvoiceReconciliationRow ReconcileRow(PosExcelInvoiceNormalizedRow row, IList<PosInvoiceDbMatch> dbRows, ISet<string> dbDuplicateKeys)
        {
            var matches = dbRows.Select(candidate => new { Db = candidate, Score = Score(row, candidate), Reasons = ScoreReasons(row, candidate) })
                .Where(x => x.Score > 0)
                .OrderByDescending(x => x.Score)
                .ThenBy(x => Math.Abs((x.Db.Amount - row.Amount.GetValueOrDefault())))
                .Take(5)
                .ToList();

            var best = matches.FirstOrDefault();
            var db = best != null ? best.Db : null;
            var result = new PosInvoiceReconciliationRow
            {
                ExcelRowNumber = row.RowNumber,
                SheetName = row.SheetName,
                ExcelInvoiceDate = row.InvoiceDate,
                ExcelInvoiceNumber = row.InvoiceNumber,
                Token = row.Token,
                Phone = row.Phone,
                ExcelCustomerName = row.CustomerName,
                ExcelAmount = row.Amount,
                ExcelBranch = row.Branch,
                ExcelUser = row.User,
                ServiceType = row.ServiceType,
                MatchScore = best != null ? best.Score : 0,
                Reason = best != null ? string.Join(" | ", best.Reasons) : "No candidate found in selected date range"
            };

            foreach (var warning in row.Warnings)
            {
                result.Warnings.Add(warning);
            }

            if (db != null)
            {
                result.DbTransactionId = db.TransactionId;
                result.DbInvoiceDate = db.InvoiceDate;
                result.DbSerial = !string.IsNullOrWhiteSpace(db.ManualNo) ? db.ManualNo : db.InvoiceNumber;
                result.DbCustomerName = db.CustomerName;
                result.DbAmount = db.Amount;
                result.Difference = row.Amount.HasValue ? row.Amount.Value - db.Amount : (decimal?)null;
                result.DbBranch = db.BranchName;
                result.DbUser = db.UserName;

                AddDomainWarnings(row, db, result);
            }

            result.Status = Classify(row, db, best != null ? best.Score : 0, result.Warnings, dbDuplicateKeys);
            return result;
        }

        private static int Score(PosExcelInvoiceNormalizedRow row, PosInvoiceDbMatch db)
        {
            var score = 0;
            var sameDate = SameDate(row.InvoiceDate, db.InvoiceDate);
            var closeDate = CloseDate(row.InvoiceDate, db.InvoiceDate);
            var sameAmount = SameAmount(row.Amount, db.Amount);
            var commissionAmount = row.Amount.HasValue && (SameAmount(row.Amount, db.NetAmount) || SameAmount(row.Amount, db.PaidAmount));

            if (!string.IsNullOrWhiteSpace(row.Token) && row.Token == db.Token && sameAmount && sameDate) score += 60;
            if (!string.IsNullOrWhiteSpace(row.Token) && row.Token == db.Token && row.Phone == db.Phone) score += 45;
            if (!string.IsNullOrWhiteSpace(row.InvoiceNumber) && (SameKey(row.InvoiceNumber, db.InvoiceNumber) || SameKey(row.InvoiceNumber, db.ManualNo)) && sameDate) score += 45;
            if (!string.IsNullOrWhiteSpace(row.NationalId) && row.NationalId == db.NationalId && !string.IsNullOrWhiteSpace(row.Token) && row.Token == db.Token) score += 50;
            if (!string.IsNullOrWhiteSpace(row.Phone) && row.Phone == db.Phone && sameAmount && closeDate) score += 38;
            if (!string.IsNullOrWhiteSpace(row.Phone) && row.Phone == db.Phone && sameAmount && sameDate) score += 30;
            if (!string.IsNullOrWhiteSpace(row.CustomerName) && SimilarName(row.CustomerName, db.CustomerName) && !string.IsNullOrWhiteSpace(row.Phone) && row.Phone == db.Phone) score += 28;
            if (sameAmount && SameService(row.ServiceType, db.ServiceType) && SimilarText(row.Branch, db.BranchName) && sameDate) score += 22;
            if (sameAmount && sameDate) score += 10;
            if (!string.IsNullOrWhiteSpace(row.CustomerName) && SimilarName(row.CustomerName, db.CustomerName)) score += 8;
            if (!string.IsNullOrWhiteSpace(row.Token) && row.Token == db.Token && !sameAmount) score += 35;
            if (!string.IsNullOrWhiteSpace(row.Phone) && row.Phone == db.Phone && !string.IsNullOrWhiteSpace(row.Token) && row.Token != db.Token) score += 18;
            if (commissionAmount) score += 12;

            return Math.Min(100, score);
        }

        private static IList<string> ScoreReasons(PosExcelInvoiceNormalizedRow row, PosInvoiceDbMatch db)
        {
            var reasons = new List<string>();
            if (!string.IsNullOrWhiteSpace(row.Token) && row.Token == db.Token) reasons.Add("Token matched");
            if (!string.IsNullOrWhiteSpace(row.Phone) && row.Phone == db.Phone) reasons.Add("Phone matched");
            if (!string.IsNullOrWhiteSpace(row.InvoiceNumber) && (SameKey(row.InvoiceNumber, db.InvoiceNumber) || SameKey(row.InvoiceNumber, db.ManualNo))) reasons.Add("Invoice/IPN matched");
            if (!string.IsNullOrWhiteSpace(row.NationalId) && row.NationalId == db.NationalId) reasons.Add("National ID matched");
            if (SameAmount(row.Amount, db.Amount)) reasons.Add("Amount matched");
            if (SameDate(row.InvoiceDate, db.InvoiceDate)) reasons.Add("Date matched");
            if (!SameDate(row.InvoiceDate, db.InvoiceDate) && CloseDate(row.InvoiceDate, db.InvoiceDate)) reasons.Add("Date is close, possible time/day shift");
            if (!SameAmount(row.Amount, db.Amount) && row.Amount.HasValue && (SameAmount(row.Amount, db.NetAmount) || SameAmount(row.Amount, db.PaidAmount))) reasons.Add("Amount may be net/base vs commission-inclusive");
            return reasons.Count == 0 ? new List<string> { "Weak candidate" } : reasons;
        }

        private static string Classify(PosExcelInvoiceNormalizedRow row, PosInvoiceDbMatch db, int score, IList<string> warnings, ISet<string> dbDuplicateKeys)
        {
            if (warnings.Any(x => x.IndexOf("Excel duplicate", StringComparison.OrdinalIgnoreCase) >= 0)) return "Excel Duplicate";
            if (db != null && HasDbDuplicate(db, dbDuplicateKeys)) return "Database Duplicate";
            if (db == null) return "Not Found";
            if (!SameDate(row.InvoiceDate, db.InvoiceDate) && (row.Token == db.Token || row.Phone == db.Phone)) return "Date Mismatch";
            if (!SameAmount(row.Amount, db.Amount) && (row.Token == db.Token || row.Phone == db.Phone || SameKey(row.InvoiceNumber, db.InvoiceNumber) || SameKey(row.InvoiceNumber, db.ManualNo))) return "Amount Mismatch";
            if (!string.IsNullOrWhiteSpace(row.CustomerName) && !string.IsNullOrWhiteSpace(db.CustomerName) && !SimilarName(row.CustomerName, db.CustomerName) && (row.Token == db.Token || row.Phone == db.Phone)) return "Customer Mismatch";
            if (score >= 80) return "Exact Match";
            if (score >= 45) return "Probable Duplicate";
            if (score > 0) return "Possible Match";
            return "Not Found";
        }

        private static void AddDomainWarnings(PosExcelInvoiceNormalizedRow row, PosInvoiceDbMatch db, PosInvoiceReconciliationRow result)
        {
            if (SameService(row.ServiceType, "Card") && !db.HasIssueVoucher && !db.IsIssueVoucher)
            {
                result.Warnings.Add("Card invoice exists but related issue voucher was not detected");
            }

            if (db.IsIssueVoucher)
            {
                result.Warnings.Add("Matched an issue voucher; sale invoice may be missing or outside selected range");
            }

            if (!string.IsNullOrWhiteSpace(row.Branch) && !SimilarText(row.Branch, db.BranchName))
            {
                result.Warnings.Add("Excel row may be entered under a different branch");
            }

            if (!string.IsNullOrWhiteSpace(row.User) && !SimilarText(row.User, db.UserName))
            {
                result.Warnings.Add("Excel row may be entered under a different user");
            }

            if (!string.IsNullOrWhiteSpace(row.Token) && row.Token == db.Token && db.TokenDetailRows == 0)
            {
                result.Warnings.Add("Token matched header but was not found in Transaction_Details.ItemSerial");
            }
        }

        private static void MarkExcelDuplicates(IList<PosExcelInvoiceNormalizedRow> rows)
        {
            var duplicateKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var group in rows.SelectMany(ExcelDuplicateKeys).GroupBy(x => x).Where(x => x.Count() > 1))
            {
                duplicateKeys.Add(group.Key);
            }

            foreach (var row in rows)
            {
                if (ExcelDuplicateKeys(row).Any(duplicateKeys.Contains))
                {
                    row.Warnings.Add("Excel duplicate: repeated token/phone+amount+day/national ID+token/invoice number");
                }
            }
        }

        private static IEnumerable<string> ExcelDuplicateKeys(PosExcelInvoiceNormalizedRow row)
        {
            if (!string.IsNullOrWhiteSpace(row.Token)) yield return "T:" + row.Token;
            if (!string.IsNullOrWhiteSpace(row.Phone) && row.Amount.HasValue && row.InvoiceDate.HasValue) yield return "P:" + row.Phone + ":" + row.Amount.Value.ToString("0.00", CultureInfo.InvariantCulture) + ":" + row.InvoiceDate.Value.ToString("yyyyMMdd");
            if (!string.IsNullOrWhiteSpace(row.NationalId) && !string.IsNullOrWhiteSpace(row.Token)) yield return "N:" + row.NationalId + ":" + row.Token;
            if (!string.IsNullOrWhiteSpace(row.InvoiceNumber)) yield return "I:" + NormalizeToken(row.InvoiceNumber);
        }

        private static ISet<string> BuildDbDuplicateKeys(IList<PosInvoiceDbMatch> rows)
        {
            var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var group in rows.SelectMany(DbDuplicateKeys).GroupBy(x => x).Where(x => x.Count() > 1))
            {
                keys.Add(group.Key);
            }

            return keys;
        }

        private static IEnumerable<string> DbDuplicateKeys(PosInvoiceDbMatch row)
        {
            if (!string.IsNullOrWhiteSpace(row.Token)) yield return "T:" + row.Token;
            if (!string.IsNullOrWhiteSpace(row.Phone) && row.InvoiceDate.HasValue) yield return "P:" + row.Phone + ":" + row.Amount.ToString("0.00", CultureInfo.InvariantCulture) + ":" + row.InvoiceDate.Value.ToString("yyyyMMdd");
            if (!string.IsNullOrWhiteSpace(row.NationalId) && !string.IsNullOrWhiteSpace(row.Token)) yield return "N:" + row.NationalId + ":" + row.Token;
            if (!string.IsNullOrWhiteSpace(row.InvoiceNumber)) yield return "I:" + NormalizeToken(row.InvoiceNumber);
            if (!string.IsNullOrWhiteSpace(row.ManualNo)) yield return "I:" + NormalizeToken(row.ManualNo);
        }

        private static bool HasDbDuplicate(PosInvoiceDbMatch row, ISet<string> keys)
        {
            return DbDuplicateKeys(row).Any(keys.Contains);
        }

        private static void FinalizeSummary(PosInvoiceReconciliationResult result)
        {
            result.Summary.ExactMatches = Count(result, "Exact Match");
            result.Summary.ProbableDuplicates = Count(result, "Probable Duplicate");
            result.Summary.PossibleMatches = Count(result, "Possible Match");
            result.Summary.NotFound = Count(result, "Not Found");
            result.Summary.AmountMismatches = Count(result, "Amount Mismatch");
            result.Summary.CustomerMismatches = Count(result, "Customer Mismatch");
            result.Summary.DateMismatches = Count(result, "Date Mismatch");
            result.Summary.DatabaseDuplicates = Count(result, "Database Duplicate");
            result.Summary.ExcelDuplicates = Count(result, "Excel Duplicate");
        }

        private static int Count(PosInvoiceReconciliationResult result, string status)
        {
            return result.Rows.Count(x => string.Equals(x.Status, status, StringComparison.OrdinalIgnoreCase));
        }

        private static IList<string> ReadHeaders(DataTable sheet, int rowIndex)
        {
            var headers = new List<string>();
            for (var i = 0; i < sheet.Columns.Count; i++)
            {
                headers.Add(CellText(sheet.Rows[rowIndex][i]));
            }

            return headers;
        }

        private static bool IsEmptyRow(DataRow row)
        {
            return row.ItemArray.All(x => string.IsNullOrWhiteSpace(CellText(x)));
        }

        private static string CellText(object value)
        {
            return value == null || value == DBNull.Value ? string.Empty : Convert.ToString(value, CultureInfo.InvariantCulture).Trim();
        }

        private static string GetValue(PosExcelInvoiceRow row, string key)
        {
            string value;
            return row.Values.TryGetValue(key, out value) ? value : string.Empty;
        }

        private static string NormalizeText(string value)
        {
            value = ConvertArabicDigits(value);
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            value = Regex.Replace(value.Trim(), @"\s+", " ");
            return value;
        }

        private static string NormalizeHeader(string value)
        {
            value = NormalizeText(value).ToLowerInvariant();
            value = value.Replace("_", " ").Replace("-", " ").Replace("/", " ");
            return Regex.Replace(value, @"\s+", " ").Trim();
        }

        private static string NormalizeToken(string value)
        {
            value = NormalizeText(value);
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            return Regex.Replace(value, @"\s+", string.Empty).ToUpperInvariant();
        }

        private static string NormalizePhone(string value)
        {
            var digits = OnlyDigits(value);
            if (string.IsNullOrWhiteSpace(digits)) return string.Empty;
            if (digits.StartsWith("0020", StringComparison.Ordinal)) digits = digits.Substring(4);
            if (digits.StartsWith("20", StringComparison.Ordinal) && digits.Length == 12) digits = "0" + digits.Substring(2);
            if (digits.Length == 10 && digits.StartsWith("1", StringComparison.Ordinal)) digits = "0" + digits;
            return digits;
        }

        private static string OnlyDigits(string value)
        {
            value = ConvertArabicDigits(value);
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            return new string(value.Where(char.IsDigit).ToArray());
        }

        private static string NormalizeServiceType(string value)
        {
            value = NormalizeText(value).ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            if (ContainsAny(value, "cash out", "كاش اوت", "كاش أوت", "سحب")) return "Cash Out";
            if (ContainsAny(value, "cash in", "كاش ان", "كاش إن", "شحن")) return "Cash In";
            if (ContainsAny(value, "card", "كارت", "بطاقة", "token", "توكن")) return "Card";
            if (ContainsAny(value, "violation", "مخالف")) return "Violations";
            return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(value);
        }

        private static DateTime? ParseDate(string value)
        {
            value = ConvertArabicDigits(value);
            if (string.IsNullOrWhiteSpace(value)) return null;
            double oa;
            if (double.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out oa) && oa > 20000 && oa < 80000)
            {
                try { return DateTime.FromOADate(oa).Date; } catch { return null; }
            }

            DateTime date;
            var cultures = new[] { CultureInfo.InvariantCulture, new CultureInfo("ar-EG"), new CultureInfo("en-US"), new CultureInfo("en-GB") };
            foreach (var culture in cultures)
            {
                if (DateTime.TryParse(value, culture, DateTimeStyles.AllowWhiteSpaces, out date))
                {
                    return date.Date;
                }
            }

            return null;
        }

        private static decimal? ParseDecimal(string value)
        {
            value = ConvertArabicDigits(value);
            if (string.IsNullOrWhiteSpace(value)) return null;
            value = value.Replace(",", string.Empty).Trim();
            decimal amount;
            if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out amount)) return amount;
            if (decimal.TryParse(value, NumberStyles.Any, new CultureInfo("ar-EG"), out amount)) return amount;
            return null;
        }

        private static string ConvertArabicDigits(string value)
        {
            if (string.IsNullOrEmpty(value)) return value;
            var builder = new StringBuilder(value.Length);
            foreach (var ch in value)
            {
                if (ch >= '٠' && ch <= '٩') builder.Append((char)('0' + ch - '٠'));
                else if (ch >= '۰' && ch <= '۹') builder.Append((char)('0' + ch - '۰'));
                else builder.Append(ch);
            }

            return builder.ToString();
        }

        private static bool SameDate(DateTime? left, DateTime? right)
        {
            return left.HasValue && right.HasValue && left.Value.Date == right.Value.Date;
        }

        private static bool CloseDate(DateTime? left, DateTime? right)
        {
            return left.HasValue && right.HasValue && Math.Abs((left.Value.Date - right.Value.Date).TotalDays) <= 1;
        }

        private static bool SameAmount(decimal? left, decimal right)
        {
            return left.HasValue && Math.Abs(left.Value - right) <= 0.02m;
        }

        private static bool SameKey(string left, string right)
        {
            return !string.IsNullOrWhiteSpace(left) && NormalizeToken(left) == NormalizeToken(right);
        }

        private static bool SameService(string left, string right)
        {
            return string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right) || string.Equals(NormalizeServiceType(left), NormalizeServiceType(right), StringComparison.OrdinalIgnoreCase);
        }

        private static bool SimilarText(string left, string right)
        {
            left = NormalizeArabicName(left);
            right = NormalizeArabicName(right);
            return string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right) || left.Contains(right) || right.Contains(left);
        }

        private static bool SimilarName(string left, string right)
        {
            left = NormalizeArabicName(left);
            right = NormalizeArabicName(right);
            if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right)) return false;
            if (left == right || left.Contains(right) || right.Contains(left)) return true;
            var leftParts = left.Split(' ').Where(x => x.Length > 1).ToList();
            var rightParts = right.Split(' ').Where(x => x.Length > 1).ToList();
            return leftParts.Count > 0 && rightParts.Count > 0 && leftParts.Intersect(rightParts).Count() >= Math.Min(2, Math.Min(leftParts.Count, rightParts.Count));
        }

        private static string NormalizeArabicName(string value)
        {
            value = NormalizeText(value).ToLowerInvariant();
            value = value.Replace("أ", "ا").Replace("إ", "ا").Replace("آ", "ا").Replace("ى", "ي").Replace("ة", "ه");
            value = Regex.Replace(value, @"[^\w\u0600-\u06FF ]", string.Empty);
            return Regex.Replace(value, @"\s+", " ").Trim();
        }

        private static bool ContainsAny(string value, params string[] needles)
        {
            return needles.Any(x => value.IndexOf(x, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static string FieldTitle(string field)
        {
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "InvoiceDate", "تاريخ الفاتورة" },
                { "InvoiceNumber", "رقم الفاتورة / IPN" },
                { "Token", "التوكن" },
                { "Phone", "الهاتف" },
                { "CustomerName", "اسم العميل" },
                { "NationalId", "الرقم القومي" },
                { "Amount", "المبلغ" },
                { "ServiceType", "نوع الخدمة" },
                { "Branch", "الفرع" },
                { "Store", "الخزنة / المخزن" },
                { "User", "المستخدم / الكاشير" },
                { "Notes", "ملاحظات" }
            };
            return map[field];
        }

        private static void ApplySource(SavedDbInvoice invoice)
        {
            if (invoice.ImportBatchId.HasValue)
            {
                invoice.Source = "Excel Imported";
                invoice.SourceConfidence = "Reliable";
                return;
            }

            if (string.Equals(invoice.NoId, "WEB_POS", StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(invoice.ManualNo2)
                && invoice.ManualNo2.IndexOf("ExcelImport|", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                invoice.Source = "Estimated Excel";
                invoice.SourceConfidence = "Estimated";
                return;
            }

            if (string.Equals(invoice.NoId, "WEB_POS", StringComparison.OrdinalIgnoreCase))
            {
                invoice.Source = "Unknown";
                invoice.SourceConfidence = "Estimated";
                return;
            }

            invoice.Source = "POS Manual/System";
            invoice.SourceConfidence = "Reliable";
        }

        private static bool SourceMatches(SavedDbInvoice invoice, string source)
        {
            source = (source ?? "All").Trim();
            if (string.Equals(source, "All", StringComparison.OrdinalIgnoreCase)) return true;
            if (string.Equals(source, "Both", StringComparison.OrdinalIgnoreCase)) return true;
            if (string.Equals(source, "Excel", StringComparison.OrdinalIgnoreCase)) return IsExcelSide(invoice);
            if (string.Equals(source, "Excel imported", StringComparison.OrdinalIgnoreCase)) return IsExcelSide(invoice);
            if (string.Equals(source, "Normal", StringComparison.OrdinalIgnoreCase)) return IsNormalSide(invoice);
            if (string.Equals(source, "Normal POS", StringComparison.OrdinalIgnoreCase)) return IsNormalSide(invoice);
            if (string.Equals(source, "Unknown", StringComparison.OrdinalIgnoreCase)) return string.Equals(invoice.Source, "Unknown", StringComparison.OrdinalIgnoreCase);
            return true;
        }

        private static bool IsExcelSide(SavedDbInvoice invoice)
        {
            return invoice != null
                && (string.Equals(invoice.Source, "Excel Imported", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(invoice.Source, "Estimated Excel", StringComparison.OrdinalIgnoreCase));
        }

        private static bool IsNormalSide(SavedDbInvoice invoice)
        {
            return invoice != null
                && (string.Equals(invoice.Source, "POS Manual/System", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(invoice.Source, "Estimated Manual", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(invoice.Source, "Unknown", StringComparison.OrdinalIgnoreCase));
        }

        private static Dictionary<string, List<SavedDbInvoice>> IndexBy(IList<SavedDbInvoice> invoices, Func<SavedDbInvoice, string> keySelector)
        {
            return invoices
                .Select(x => new { Key = keySelector(x), Value = x })
                .Where(x => !string.IsNullOrWhiteSpace(x.Key))
                .GroupBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(x => x.Key, x => x.Select(v => v.Value).ToList(), StringComparer.OrdinalIgnoreCase);
        }

        private static Dictionary<string, List<SavedDbInvoice>> IndexByMany(IList<SavedDbInvoice> invoices, Func<SavedDbInvoice, IEnumerable<string>> keySelector)
        {
            return invoices
                .SelectMany(x => keySelector(x).Where(k => !string.IsNullOrWhiteSpace(k)).Select(k => new { Key = k, Value = x }))
                .GroupBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(x => x.Key, x => x.Select(v => v.Value).ToList(), StringComparer.OrdinalIgnoreCase);
        }

        private static void AddCandidates(Dictionary<int, SavedDbInvoice> candidates, Dictionary<string, List<SavedDbInvoice>> index, string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return;
            }

            List<SavedDbInvoice> rows;
            if (!index.TryGetValue(key, out rows))
            {
                return;
            }

            foreach (var row in rows.Take(200))
            {
                candidates[row.TransactionId] = row;
            }
        }

        private static void AddScore(bool condition, int points, string reason, ref int score, IList<string> reasons)
        {
            if (!condition)
            {
                return;
            }

            score += points;
            reasons.Add(reason);
        }

        private static string AmountDayBranchKey(decimal amount, DateTime date, int? branchId, string serviceType)
        {
            return branchId.GetValueOrDefault().ToString(CultureInfo.InvariantCulture)
                + "|" + NormalizeServiceType(serviceType)
                + "|" + date.Date.ToString("yyyyMMdd", CultureInfo.InvariantCulture)
                + "|" + Math.Round(amount, 2).ToString("0.00", CultureInfo.InvariantCulture);
        }

        private static string DayBranchServiceKey(DateTime date, int? branchId, string serviceType)
        {
            return date.Date.ToString("yyyyMMdd", CultureInfo.InvariantCulture)
                + "|" + branchId.GetValueOrDefault().ToString(CultureInfo.InvariantCulture)
                + "|" + NormalizeServiceType(serviceType);
        }

        private static string CustomerAmountKey(string customerName, decimal amount)
        {
            var name = NormalizeArabicName(customerName);
            if (string.IsNullOrWhiteSpace(name))
            {
                return string.Empty;
            }

            return name + "|" + Math.Round(amount, 2).ToString("0.00", CultureInfo.InvariantCulture);
        }

        private static string AmountBranchServiceKey(decimal amount, int? branchId, string serviceType)
        {
            return branchId.GetValueOrDefault().ToString(CultureInfo.InvariantCulture)
                + "|" + NormalizeServiceType(serviceType)
                + "|" + Math.Round(amount, 2).ToString("0.00", CultureInfo.InvariantCulture);
        }

        private static IEnumerable<string> InvoiceDayBranchKeys(SavedDbInvoice invoice)
        {
            if (invoice == null || !invoice.InvoiceDate.HasValue)
            {
                yield break;
            }

            var branch = invoice.BranchId.GetValueOrDefault().ToString(CultureInfo.InvariantCulture);
            var date = invoice.InvoiceDate.Value.Date.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
            foreach (var key in InvoiceKeys(invoice))
            {
                yield return NormalizeToken(key) + "|" + date + "|" + branch;
            }
        }

        private static IEnumerable<string> InvoiceKeys(SavedDbInvoice invoice)
        {
            if (invoice == null)
            {
                yield break;
            }

            var keys = new[] { invoice.InvoiceNumber, invoice.ManualNo, invoice.ManualNo2 };
            foreach (var key in keys.Select(NormalizeToken).Where(x => !string.IsNullOrWhiteSpace(x)))
            {
                if (key.StartsWith("EXCELIMPORT|", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                yield return key;
            }
        }

        private static Tuple<string, string> ResolveRisk(IList<PosSavedInvoiceDuplicatePair> pairs, IList<SavedDbInvoice> excel, IList<SavedDbInvoice> normal)
        {
            if (pairs.Any(x => x.MatchClassification == "Confirmed Duplicate" && !string.IsNullOrWhiteSpace(x.ExcelToken) && x.ExcelToken == x.NormalToken))
            {
                return System.Tuple.Create("Critical", "Same token exists in Excel-imported and normal invoice");
            }

            if (pairs.Any(x => x.Reasons.Any(r => r.IndexOf("phone + amount + date", StringComparison.OrdinalIgnoreCase) >= 0)))
            {
                return System.Tuple.Create("High", "Same phone + same amount + same date exists in both sources");
            }

            if (pairs.Any(x => x.Reasons.Any(r => r.IndexOf("national ID + amount + date", StringComparison.OrdinalIgnoreCase) >= 0)))
            {
                return System.Tuple.Create("High", "Same national ID + same amount/date exists in both sources");
            }

            if (pairs.Count >= 5 || Math.Abs(excel.Sum(x => x.Amount) - normal.Sum(x => x.Amount)) >= 1000m)
            {
                return System.Tuple.Create("High", "Large amount difference or many similar matches");
            }

            if (pairs.Any())
            {
                return System.Tuple.Create("Medium", "Potential duplicate matches found");
            }

            if (excel.Count > 0 && normal.Count > 0)
            {
                return System.Tuple.Create("Low", "Same day has both Excel and normal entries");
            }

            return System.Tuple.Create("Low", "Single source only in selected filters");
        }

        private static int RiskRank(string risk)
        {
            if (string.Equals(risk, "Critical", StringComparison.OrdinalIgnoreCase)) return 4;
            if (string.Equals(risk, "High", StringComparison.OrdinalIgnoreCase)) return 3;
            if (string.Equals(risk, "Medium", StringComparison.OrdinalIgnoreCase)) return 2;
            return 1;
        }

        private static double? TimeDiffMinutes(DateTime? left, DateTime? right)
        {
            if (!left.HasValue || !right.HasValue)
            {
                return null;
            }

            return Math.Abs((left.Value - right.Value).TotalMinutes);
        }

        private static string FirstNonEmpty(params string[] values)
        {
            return values.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x)) ?? string.Empty;
        }

        private static void AddSavedPeriodSheet(WorkbookPart workbookPart, PosSavedInvoiceReconciliationResult result)
        {
            var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
            var sheetData = new SheetData();
            worksheetPart.Worksheet = new Worksheet(new SheetViews(new SheetView { WorkbookViewId = 0, RightToLeft = true }), sheetData);
            sheetData.Append(RowFromValues("Filter", "Value"));
            sheetData.Append(RowFromValues("From", DateText(result.FromDate)));
            sheetData.Append(RowFromValues("To", DateText(result.ToDate)));
            sheetData.Append(RowFromValues("Month", result.Month));
            sheetData.Append(RowFromValues("Branch ID", result.BranchId.HasValue ? result.BranchId.Value.ToString(CultureInfo.InvariantCulture) : "All"));
            sheetData.Append(RowFromValues("Service type", result.ServiceType));
            sheetData.Append(RowFromValues("Teller user ID", result.TellerUserId.HasValue ? result.TellerUserId.Value.ToString(CultureInfo.InvariantCulture) : "All"));
            sheetData.Append(RowFromValues("Source", result.ImportSource));
            sheetData.Append(RowFromValues("Amount from", result.MinAmount.HasValue ? MoneyText(result.MinAmount.Value) : string.Empty));
            sheetData.Append(RowFromValues("Amount to", result.MaxAmount.HasValue ? MoneyText(result.MaxAmount.Value) : string.Empty));
            sheetData.Append(RowFromValues("Token", result.Token));
            sheetData.Append(RowFromValues("Phone", result.Phone));
            sheetData.Append(RowFromValues("National ID", result.NationalId));
            sheetData.Append(RowFromValues("Customer", result.CustomerName));
            sheetData.Append(RowFromValues(string.Empty, string.Empty));
            sheetData.Append(RowFromValues("Excel count", "Excel amount", "Normal count", "Normal amount", "Count diff", "Amount diff", "Suspicious pairs", "High risk pairs", "Risk", "Reason"));
            var row = result.PeriodSummary ?? new PosSavedInvoicePeriodSummary();
            sheetData.Append(RowFromValues(row.ExcelImportedCount.ToString(CultureInfo.InvariantCulture), MoneyText(row.ExcelImportedAmount), row.NormalPosCount.ToString(CultureInfo.InvariantCulture), MoneyText(row.NormalPosAmount), row.CountDifference.ToString(CultureInfo.InvariantCulture), MoneyText(row.AmountDifference), row.SuspiciousDuplicatePairs.ToString(CultureInfo.InvariantCulture), row.HighRiskPairs.ToString(CultureInfo.InvariantCulture), row.RiskLevel, row.Reason));
            AppendSheet(workbookPart, worksheetPart, "Period");
        }

        private static void AddSavedBranchSummarySheet(WorkbookPart workbookPart, IList<PosSavedInvoiceBranchSummary> summaries)
        {
            var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
            var sheetData = new SheetData();
            worksheetPart.Worksheet = new Worksheet(new SheetViews(new SheetView { WorkbookViewId = 0, RightToLeft = true }), sheetData);
            sheetData.Append(RowFromValues("Branch", "Excel count", "Excel amount", "Normal count", "Normal amount", "Count diff", "Amount diff", "Suspicious pairs", "Risk", "Reason"));
            foreach (var row in summaries ?? new List<PosSavedInvoiceBranchSummary>())
            {
                sheetData.Append(RowFromValues(row.BranchName, row.ExcelImportedCount.ToString(CultureInfo.InvariantCulture), MoneyText(row.ExcelImportedAmount), row.NormalPosCount.ToString(CultureInfo.InvariantCulture), MoneyText(row.NormalPosAmount), row.CountDifference.ToString(CultureInfo.InvariantCulture), MoneyText(row.AmountDifference), row.SuspiciousDuplicatePairs.ToString(CultureInfo.InvariantCulture), row.RiskLevel, row.Reason));
            }

            AppendSheet(workbookPart, worksheetPart, "Branches");
        }

        private static void AddSavedDaySummarySheet(WorkbookPart workbookPart, IList<PosSavedInvoiceDaySummary> summaries)
        {
            var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
            var sheetData = new SheetData();
            worksheetPart.Worksheet = new Worksheet(new SheetViews(new SheetView { WorkbookViewId = 0, RightToLeft = true }), sheetData);
            sheetData.Append(RowFromValues("Date", "Excel count", "Excel amount", "Normal count", "Normal amount", "Count diff", "Amount diff", "Suspicious pairs", "Risk", "Reason"));
            foreach (var row in summaries ?? new List<PosSavedInvoiceDaySummary>())
            {
                sheetData.Append(RowFromValues(DateText(row.Date), row.ExcelImportedCount.ToString(CultureInfo.InvariantCulture), MoneyText(row.ExcelImportedAmount), row.NormalPosCount.ToString(CultureInfo.InvariantCulture), MoneyText(row.NormalPosAmount), row.CountDifference.ToString(CultureInfo.InvariantCulture), MoneyText(row.AmountDifference), row.SuspiciousDuplicatePairs.ToString(CultureInfo.InvariantCulture), row.RiskLevel, row.Reason));
            }

            AppendSheet(workbookPart, worksheetPart, "Days");
        }

        private static void AddSavedDimensionSummarySheet(WorkbookPart workbookPart, IList<PosSavedInvoiceDimensionSummary> summaries, string name)
        {
            var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
            var sheetData = new SheetData();
            worksheetPart.Worksheet = new Worksheet(new SheetViews(new SheetView { WorkbookViewId = 0, RightToLeft = true }), sheetData);
            sheetData.Append(RowFromValues("Name", "Excel count", "Excel amount", "Normal count", "Normal amount", "Count diff", "Amount diff", "Suspicious pairs", "Risk", "Reason"));
            foreach (var row in summaries ?? new List<PosSavedInvoiceDimensionSummary>())
            {
                sheetData.Append(RowFromValues(row.DimensionName, row.ExcelImportedCount.ToString(CultureInfo.InvariantCulture), MoneyText(row.ExcelImportedAmount), row.NormalPosCount.ToString(CultureInfo.InvariantCulture), MoneyText(row.NormalPosAmount), row.CountDifference.ToString(CultureInfo.InvariantCulture), MoneyText(row.AmountDifference), row.SuspiciousDuplicatePairs.ToString(CultureInfo.InvariantCulture), row.RiskLevel, row.Reason));
            }

            AppendSheet(workbookPart, worksheetPart, name);
        }

        private static void AddSavedSummarySheet(WorkbookPart workbookPart, PosSavedInvoiceReconciliationResult result)
        {
            var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
            var sheetData = new SheetData();
            worksheetPart.Worksheet = new Worksheet(new SheetViews(new SheetView { WorkbookViewId = 0, RightToLeft = true }), sheetData);
            sheetData.Append(RowFromValues("Date", "Branch", "Service type", "Excel count", "Excel amount", "Normal count", "Normal amount", "Count diff", "Amount diff", "Potential duplicates", "Risk", "Reason"));
            foreach (var row in result.DayBranchSummaries)
            {
                sheetData.Append(RowFromValues(DateText(row.Date), row.BranchName, row.ServiceType, row.ExcelImportedCount.ToString(CultureInfo.InvariantCulture), MoneyText(row.ExcelImportedAmount), row.NormalPosCount.ToString(CultureInfo.InvariantCulture), MoneyText(row.NormalPosAmount), row.CountDifference.ToString(CultureInfo.InvariantCulture), MoneyText(row.AmountDifference), row.PotentialDuplicateMatches.ToString(CultureInfo.InvariantCulture), row.RiskLevel, row.Reason));
            }

            AppendSheet(workbookPart, worksheetPart, "Summary");
        }

        private static void AddSavedPairsSheet(WorkbookPart workbookPart, IList<PosSavedInvoiceDuplicatePair> pairs, string name)
        {
            var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
            var sheetData = new SheetData();
            worksheetPart.Worksheet = new Worksheet(new SheetViews(new SheetView { WorkbookViewId = 0, RightToLeft = true }), sheetData);
            sheetData.Append(RowFromValues("Excel Transaction", "Excel date", "Excel serial", "Excel batch", "Excel source", "Normal Transaction", "Normal date", "Normal serial", "Service", "Branch", "Excel user", "Normal user", "Excel token", "Normal token", "Excel phone", "Normal phone", "Excel national ID", "Normal national ID", "Excel customer", "Normal customer", "Excel amount", "Normal amount", "Amount diff", "Time diff minutes", "Score", "Classification", "Reasons", "Warnings"));
            foreach (var row in pairs)
            {
                sheetData.Append(RowFromValues(row.ExcelTransactionId.ToString(CultureInfo.InvariantCulture), DateText(row.ExcelInvoiceDate), row.ExcelSerial, row.ExcelImportBatchId.HasValue ? row.ExcelImportBatchId.Value.ToString(CultureInfo.InvariantCulture) : string.Empty, row.ExcelSource, row.NormalTransactionId.ToString(CultureInfo.InvariantCulture), DateText(row.NormalInvoiceDate), row.NormalSerial, row.ServiceType, row.BranchName, row.ExcelUserName, row.NormalUserName, row.ExcelToken, row.NormalToken, row.ExcelPhone, row.NormalPhone, row.ExcelNationalId, row.NormalNationalId, row.ExcelCustomerName, row.NormalCustomerName, MoneyText(row.ExcelAmount), MoneyText(row.NormalAmount), MoneyText(row.AmountDifference), row.TimeDifferenceMinutes.HasValue ? row.TimeDifferenceMinutes.Value.ToString("0.#", CultureInfo.InvariantCulture) : string.Empty, row.MatchScore.ToString(CultureInfo.InvariantCulture), row.MatchClassification, string.Join(" | ", row.Reasons), string.Join(" | ", row.Warnings)));
            }

            AppendSheet(workbookPart, worksheetPart, name);
        }

        private static void AppendSheet(WorkbookPart workbookPart, WorksheetPart worksheetPart, string name)
        {
            var sheets = workbookPart.Workbook.GetFirstChild<Sheets>();
            if (sheets == null)
            {
                sheets = workbookPart.Workbook.AppendChild(new Sheets());
            }

            var sheetId = (uint)(sheets.Count() + 1);
            sheets.Append(new Sheet { Id = workbookPart.GetIdOfPart(worksheetPart), SheetId = sheetId, Name = name });
        }

        private class SavedDbInvoice
        {
            public int TransactionId { get; set; }
            public DateTime? InvoiceDate { get; set; }
            public string InvoiceNumber { get; set; }
            public string ManualNo { get; set; }
            public string ManualNo2 { get; set; }
            public string NoId { get; set; }
            public string Token { get; set; }
            public string Phone { get; set; }
            public string CustomerName { get; set; }
            public string NationalId { get; set; }
            public decimal Amount { get; set; }
            public decimal NetAmount { get; set; }
            public decimal PaidAmount { get; set; }
            public string ServiceType { get; set; }
            public int? BranchId { get; set; }
            public string BranchName { get; set; }
            public int? UserId { get; set; }
            public string UserName { get; set; }
            public long? ImportBatchId { get; set; }
            public string SourceFileName { get; set; }
            public string SourceSheet { get; set; }
            public int? SourceRow { get; set; }
            public string ImportRowStatus { get; set; }
            public string Source { get; set; }
            public string SourceConfidence { get; set; }
            public string RiskLevel { get; set; }
            public string ReviewStatus { get; set; }
        }

        private class PairScore
        {
            public int Score { get; set; }
            public string Classification { get; set; }
            public IList<string> Reasons { get; set; }
            public IList<string> Warnings { get; set; }
        }

        private static Row RowFromValues(params string[] values)
        {
            var row = new Row();
            foreach (var value in values)
            {
                row.Append(new Cell { DataType = CellValues.String, CellValue = new CellValue(value ?? string.Empty) });
            }

            return row;
        }

        private static string DateText(DateTime? value)
        {
            return value.HasValue ? value.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : string.Empty;
        }

        private static string MoneyText(decimal? value)
        {
            return value.HasValue ? value.Value.ToString("0.##", CultureInfo.InvariantCulture) : string.Empty;
        }

        private static int? ReadInt(IDataRecord reader, string name)
        {
            var ordinal = reader.GetOrdinal(name);
            return reader.IsDBNull(ordinal) ? (int?)null : Convert.ToInt32(reader.GetValue(ordinal), CultureInfo.InvariantCulture);
        }

        private static long? ReadLong(IDataRecord reader, string name)
        {
            var ordinal = reader.GetOrdinal(name);
            return reader.IsDBNull(ordinal) ? (long?)null : Convert.ToInt64(reader.GetValue(ordinal), CultureInfo.InvariantCulture);
        }

        private static decimal? ReadDecimal(IDataRecord reader, string name)
        {
            var ordinal = reader.GetOrdinal(name);
            return reader.IsDBNull(ordinal) ? (decimal?)null : Convert.ToDecimal(reader.GetValue(ordinal), CultureInfo.InvariantCulture);
        }

        private static DateTime? ReadDateTime(IDataRecord reader, string name)
        {
            var ordinal = reader.GetOrdinal(name);
            return reader.IsDBNull(ordinal) ? (DateTime?)null : Convert.ToDateTime(reader.GetValue(ordinal), CultureInfo.InvariantCulture);
        }

        private static string ReadString(IDataRecord reader, string name)
        {
            var ordinal = reader.GetOrdinal(name);
            return reader.IsDBNull(ordinal) ? null : Convert.ToString(reader.GetValue(ordinal), CultureInfo.InvariantCulture);
        }

        private static bool ReadBoolean(IDataRecord reader, string name)
        {
            var ordinal = reader.GetOrdinal(name);
            return !reader.IsDBNull(ordinal) && Convert.ToInt32(reader.GetValue(ordinal), CultureInfo.InvariantCulture) != 0;
        }
    }
}
