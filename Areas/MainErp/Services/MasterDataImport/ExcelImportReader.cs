using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using ExcelDataReader;
using MyERP.Areas.MainErp.ViewModels.MasterDataImport;

namespace MyERP.Areas.MainErp.Services.MasterDataImport
{
    public class ExcelImportReader
    {
        private static readonly string[] EmptyValues = new[] { null, "" };

        public IList<MasterDataImportRowViewModel> ReadChartOfAccounts(string filePath)
        {
            var table = ReadFirstWorksheet(filePath);
            var rows = new List<MasterDataImportRowViewModel>();

            var legacyTawjerLayout = LooksLikeLegacyTawjerAccountTree(table);
            var lastCodeByLevel = new Dictionary<int, string>();
            var lastSerialByLevel = new Dictionary<int, string>();

            for (var i = 0; i < table.Rows.Count; i++)
            {
                var dataRow = table.Rows[i];
                if (IsEmptyRow(dataRow))
                {
                    continue;
                }

                MasterDataImportRowViewModel row;
                if (legacyTawjerLayout)
                {
                    row = ReadLegacyTawjerRow(dataRow, i + 2, lastSerialByLevel);
                    if (IsIgnorableLegacyTawjerRow(row))
                    {
                        continue;
                    }
                }
                else
                {
                    row = new MasterDataImportRowViewModel
                    {
                        RowNumber = i + 2,
                        AccountCode = Get(dataRow, "Account_Code", "Account Code", "ظƒظˆط¯ ط§ظ„ط­ط³ط§ط¨ ط§ظ„ط¯ط§ط®ظ„ظٹ"),
                        AccountSerial = Get(dataRow, "Account_Serial", "Account Serial", "ط±ظ‚ظ… ط§ظ„ط­ط³ط§ط¨", "ظƒظˆط¯ ط§ظ„ط­ط³ط§ط¨"),
                        AccountName = Get(dataRow, "Account_Name", "Account Name", "ط§ط³ظ… ط§ظ„ط­ط³ط§ط¨", "ط§ظ„ط§ط³ظ… ط§ظ„ط¹ط±ط¨ظٹ"),
                        AccountNameEnglish = Get(dataRow, "Account_NameEng", "English Name", "ط§ظ„ط§ط³ظ… ط§ظ„ط§ظ†ط¬ظ„ظٹط²ظٹ"),
                        ParentAccountCode = Get(dataRow, "Parent_Account_Code", "Parent Account Code", "ظƒظˆط¯ ط§ظ„ط­ط³ط§ط¨ ط§ظ„ط§ط¨ ط§ظ„ط¯ط§ط®ظ„ظٹ"),
                        ParentAccountSerial = Get(dataRow, "Parent_Account_Serial", "Parent Serial", "ط±ظ‚ظ… ط­ط³ط§ط¨ ط§ظ„ط§ط¨", "ط§ظ„ط­ط³ط§ط¨ ط§ظ„ط§ط¨"),
                        IsFinalAccount = ParseBoolean(Get(dataRow, "last_account", "Is Final", "Final", "ط­ط³ط§ط¨ ظ†ظ‡ط§ط¦ظٹ"), true),
                        Level = ParseInt(Get(dataRow, "Level", "Account Level", "ط§ظ„ظ…ط³طھظˆظ‰")),
                        CurrencyCode = Get(dataRow, "currenct_code", "Currency Code", "ظƒظˆط¯ ط§ظ„ط¹ظ…ظ„ط©")
                    };
                }

                if (!string.IsNullOrWhiteSpace(row.AccountCode) && row.Level.HasValue)
                {
                    lastCodeByLevel[row.Level.Value] = row.AccountCode;
                }

                if (IsBlankChartRow(row))
                {
                    continue;
                }

                rows.Add(row);
            }

            return rows;
        }

        public IList<MasterDataImportRowViewModel> ReadAccountBalanceMasterFile(string filePath, string entityType)
        {
            var table = ReadFirstWorksheet(filePath);
            var rows = new List<MasterDataImportRowViewModel>();
            var accountBalanceLayout = LooksLikeLegacyAccountBalanceReport(table);

            for (var i = 0; i < table.Rows.Count; i++)
            {
                var dataRow = table.Rows[i];
                if (IsEmptyRow(dataRow))
                {
                    continue;
                }

                if (accountBalanceLayout)
                {
                    var serialFromReport = GetByIndex(dataRow, 7);
                    var nameFromReport = GetByIndex(dataRow, 6);
                    if (IsLikelyHeader(serialFromReport, nameFromReport))
                    {
                        continue;
                    }

                    var signedBalance = ParseSignedBalance(GetByIndex(dataRow, 5));
                    rows.Add(new MasterDataImportRowViewModel
                    {
                        RowNumber = i + 2,
                        EntityType = entityType,
                        EntityCode = serialFromReport,
                        EntityName = nameFromReport,
                        AccountSerial = serialFromReport,
                        AccountName = nameFromReport,
                        OpeningBalance = signedBalance.HasValue ? Math.Abs(signedBalance.Value) : (decimal?)null,
                        OpeningBalanceType = ParseBalanceTypeFromLabel(GetByIndex(dataRow, 4), GetByIndex(dataRow, 0), signedBalance),
                        CurrencyCode = "1"
                    });
                    continue;
                }

                var serial = Get(dataRow, "Account_Serial", "Account Serial", "ط±ظ‚ظ… ط§ظ„ط­ط³ط§ط¨", "ظƒظˆط¯ ط§ظ„ط­ط³ط§ط¨");
                var name = Get(dataRow, "Account_Name", "Account Name", "ط§ط³ظ… ط§ظ„ط­ط³ط§ط¨", "ط§ظ„ط§ط³ظ… ط§ظ„ط¹ط±ط¨ظٹ");
                if (string.IsNullOrWhiteSpace(serial) && table.Columns.Count >= 8)
                {
                    serial = GetByIndex(dataRow, 7);
                }

                if (string.IsNullOrWhiteSpace(name) && table.Columns.Count >= 7)
                {
                    name = GetByIndex(dataRow, 6);
                }

                if (IsLikelyHeader(serial, name))
                {
                    continue;
                }

                rows.Add(new MasterDataImportRowViewModel
                {
                    RowNumber = i + 2,
                    EntityType = entityType,
                    EntityCode = serial,
                    EntityName = name,
                    AccountSerial = serial,
                    AccountName = name,
                    OpeningBalance = ParseDecimal(GetByIndex(dataRow, 1)),
                    OpeningBalanceType = ParseBalanceType(GetByIndex(dataRow, 1), GetByIndex(dataRow, 0)),
                    CurrencyCode = "1"
                });
            }

            return rows;
        }

        public IList<JournalEntryImportRowViewModel> ReadJournalEntries(IDictionary<string, string> filePathsByOriginalName, IDictionary<string, string> fileHashes)
        {
            var rows = new List<JournalEntryImportRowViewModel>();
            foreach (var file in filePathsByOriginalName)
            {
                var fileHash = ComputeFileHash(file.Value);
                fileHashes[file.Key] = fileHash;
                foreach (var sheet in ReadWorksheets(file.Value))
                {
                    var hasGroupingColumn = HasAnyColumn(sheet.Table, "Reference No", "ReferenceNo", "Ref No", "Entry No", "EntryNo", "Voucher No", "VoucherNo");
                    for (var i = 0; i < sheet.Table.Rows.Count; i++)
                    {
                        var dataRow = sheet.Table.Rows[i];
                        if (IsEmptyRow(dataRow))
                        {
                            continue;
                        }

                        var entryDateText = Get(dataRow, "Entry Date", "Date", "طھط§ط±ظٹط® ط§ظ„ظ‚ظٹط¯", "طھط§ط±ظٹط®");
                        var referenceNo = Get(dataRow, "Reference No", "ReferenceNo", "Ref No", "ط±ظ‚ظ… ط§ظ„ظ…ط±ط¬ط¹");
                        var entryNo = Get(dataRow, "Entry No", "EntryNo", "ط±ظ‚ظ… ط§ظ„ظ‚ظٹط¯");
                        var voucherNo = Get(dataRow, "Voucher No", "VoucherNo", "ط±ظ‚ظ… ط§ظ„ط³ظ†ط¯", "ط±ظ‚ظ… ط§ظ„ط­ط±ظƒط©");
                        var debitText = Get(dataRow, "Debit", "ظ…ط¯ظٹظ†");
                        var creditText = Get(dataRow, "Credit", "ط¯ط§ط¦ظ†");
                        var row = new JournalEntryImportRowViewModel
                        {
                            FileName = file.Key,
                            SheetName = sheet.Name,
                            RowNumber = i + 2,
                            FileHash = fileHash,
                            EntryDateText = entryDateText,
                            EntryDate = ParseDate(entryDateText),
                            AccountSerial = Get(dataRow, "Account Serial", "Account_Serial", "ظ…ط³ظ„ط³ظ„ ط§ظ„ط­ط³ط§ط¨", "ط±ظ‚ظ… ط§ظ„ط­ط³ط§ط¨", "ظƒظˆط¯ ط§ظ„ط­ط³ط§ط¨"),
                            AccountName = Get(dataRow, "Account Name", "ط§ط³ظ… ط§ظ„ط­ط³ط§ط¨", "ط§ظ„ط­ط³ط§ط¨"),
                            DebitText = debitText,
                            CreditText = creditText,
                            Debit = ParseDecimal(debitText).GetValueOrDefault(),
                            Credit = ParseDecimal(creditText).GetValueOrDefault(),
                            Description = Get(dataRow, "Description", "ط§ظ„ط¨ظٹط§ظ†", "ط¨ظٹط§ظ†", "Remarks", "ظ…ظ„ط§ط­ط¸ط§طھ"),
                            Branch = Get(dataRow, "Branch", "ط§ظ„ظپط±ط¹"),
                            CostCenter = Get(dataRow, "Cost Center", "CostCenter", "ظ…ط±ظƒط² ط§ظ„طھظƒظ„ظپط©"),
                            ReferenceNo = referenceNo,
                            EntryNo = entryNo,
                            VoucherNo = voucherNo
                        };

                        row.GroupKey = BuildJournalGroupKey(file.Key, sheet.Name, hasGroupingColumn, row);
                        rows.Add(row);
                    }
                }
            }

            return rows;
        }

        public IList<JournalEntryImportRowViewModel> ReadOpeningBalances(IDictionary<string, string> filePathsByOriginalName, IDictionary<string, string> fileHashes)
        {
            var rows = new List<JournalEntryImportRowViewModel>();
            foreach (var file in filePathsByOriginalName)
            {
                var fileHash = ComputeFileHash(file.Value);
                fileHashes[file.Key] = fileHash;
                foreach (var sheet in ReadWorksheets(file.Value))
                {
                    if (LooksLikeLegacyAccountBalanceReport(sheet.Table))
                    {
                        ReadLegacyOpeningBalanceSheet(rows, file.Key, fileHash, sheet);
                        continue;
                    }

                    for (var i = 0; i < sheet.Table.Rows.Count; i++)
                    {
                        var dataRow = sheet.Table.Rows[i];
                        if (IsEmptyRow(dataRow))
                        {
                            continue;
                        }

                        var debitText = Get(dataRow, "Debit", "ظ…ط¯ظٹظ†");
                        var creditText = Get(dataRow, "Credit", "ط¯ط§ط¦ظ†");
                        var balanceText = Get(dataRow, "Opening Balance", "OpeningBalance", "Balance", "ط§ظ„ط±طµظٹط¯ ط§ظ„ط§ظپطھطھط§ط­ظٹ", "ط±طµظٹط¯ ط§ظپطھطھط§ط­ظٹ", "ط§ظ„ط±طµظٹط¯");
                        var row = new JournalEntryImportRowViewModel
                        {
                            FileName = file.Key,
                            SheetName = sheet.Name,
                            RowNumber = i + 2,
                            FileHash = fileHash,
                            IsOpeningBalance = true,
                            GroupKey = "OPENING|" + string.Join(",", filePathsByOriginalName.Keys),
                            EntryDateText = Get(dataRow, "Entry Date", "Date", "Opening Date", "طھط§ط±ظٹط® ط§ظ„ظ‚ظٹط¯", "طھط§ط±ظٹط® ط§ظ„ط±طµظٹط¯", "طھط§ط±ظٹط®"),
                            AccountSerial = Get(dataRow, "Account Serial", "Account_Serial", "ظ…ط³ظ„ط³ظ„ ط§ظ„ط­ط³ط§ط¨", "ط±ظ‚ظ… ط§ظ„ط­ط³ط§ط¨", "ظƒظˆط¯ ط§ظ„ط­ط³ط§ط¨"),
                            AccountName = Get(dataRow, "Account Name", "ط§ط³ظ… ط§ظ„ط­ط³ط§ط¨", "ط§ظ„ط­ط³ط§ط¨"),
                            DebitText = debitText,
                            CreditText = creditText,
                            OpeningBalanceText = balanceText,
                            BalanceType = Get(dataRow, "Balance Type", "BalanceType", "ط·ط¨ظٹط¹ط© ط§ظ„ط±طµظٹط¯", "ط·ط¨ظٹط¹ط©"),
                            Description = Get(dataRow, "Notes", "Description", "ط§ظ„ط¨ظٹط§ظ†", "ظ…ظ„ط§ط­ط¸ط§طھ"),
                            Branch = Get(dataRow, "Branch", "ط§ظ„ظپط±ط¹"),
                            CostCenter = Get(dataRow, "Cost Center", "CostCenter", "ظ…ط±ظƒط² ط§ظ„طھظƒظ„ظپط©")
                        };

                        row.EntryDate = ParseDate(row.EntryDateText) ?? GetDefaultOpeningBalanceDate();
                        row.Debit = ParseDecimal(debitText).GetValueOrDefault();
                        row.Credit = ParseDecimal(creditText).GetValueOrDefault();
                        if (row.Debit == 0m && row.Credit == 0m)
                        {
                            ApplyOpeningBalanceSide(row);
                        }

                        rows.Add(row);
                    }
                }
            }

            return rows;
        }

        private static void ReadLegacyOpeningBalanceSheet(IList<JournalEntryImportRowViewModel> rows, string fileName, string fileHash, WorksheetData sheet)
        {
            for (var i = 0; i < sheet.Table.Rows.Count; i++)
            {
                var dataRow = sheet.Table.Rows[i];
                if (IsEmptyRow(dataRow))
                {
                    continue;
                }

                LegacyOpeningBalanceRow legacyRow;
                if (!TryReadLegacyOpeningBalanceRow(dataRow, out legacyRow))
                {
                    continue;
                }

                var row = new JournalEntryImportRowViewModel
                {
                    FileName = fileName,
                    SheetName = sheet.Name,
                    RowNumber = i + 2,
                    FileHash = fileHash,
                    IsOpeningBalance = true,
                    GroupKey = "OPENING|" + fileName,
                    EntryDateText = GetDefaultOpeningBalanceDate().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    EntryDate = GetDefaultOpeningBalanceDate(),
                    AccountSerial = legacyRow.AccountSerial,
                    AccountName = legacyRow.AccountName,
                    OpeningBalanceText = legacyRow.BalanceText,
                    BalanceType = legacyRow.BalanceLabel,
                    Description = "Opening balance import - " + fileName
                };

                ApplyLegacyOpeningBalanceSide(row, legacyRow.BalanceValue, legacyRow.BalanceLabel);
                rows.Add(row);
            }
        }

        private static bool TryReadLegacyOpeningBalanceRow(DataRow dataRow, out LegacyOpeningBalanceRow result)
        {
            result = null;
            var cells = dataRow.ItemArray.Select((value, index) => new
            {
                Index = index,
                Text = Clean(Convert.ToString(value))
            }).Where(c => !string.IsNullOrWhiteSpace(c.Text)).ToList();

            var serialCandidates = cells.Where(c => LooksLikeAccountSerial(c.Text)).ToList();
            var nameCandidates = cells.Where(c => LooksLikeAccountName(c.Text)).ToList();
            if (serialCandidates.Count == 0 || nameCandidates.Count == 0)
            {
                return false;
            }

            var best = serialCandidates
                .Select(s => new
                {
                    Serial = s,
                    Name = nameCandidates.OrderBy(n => Math.Abs(n.Index - s.Index)).ThenBy(n => n.Index).FirstOrDefault()
                })
                .Where(x => x.Name != null)
                .OrderBy(x => Math.Abs(x.Name.Index - x.Serial.Index))
                .ThenByDescending(x => x.Serial.Text.Length)
                .FirstOrDefault();

            if (best == null || IsLikelyHeader(best.Serial.Text, best.Name.Text))
            {
                return false;
            }

            var balance = FindLegacyBalance(dataRow, best.Serial.Index, best.Name.Index);
            if (balance == null || balance.Value == 0m)
            {
                return false;
            }

            result = new LegacyOpeningBalanceRow
            {
                AccountSerial = best.Serial.Text,
                AccountName = best.Name.Text,
                BalanceText = balance.Text,
                BalanceValue = balance.Value,
                BalanceLabel = balance.Label
            };
            return true;
        }

        private static LegacyBalanceCandidate FindLegacyBalance(DataRow row, int serialIndex, int nameIndex)
        {
            for (var i = 0; i < row.Table.Columns.Count; i++)
            {
                var label = GetByIndex(row, i);
                if (!IsDebitLabel(label) && !IsCreditLabel(label))
                {
                    continue;
                }

                var adjacent = new[] { i + 1, i - 1 };
                foreach (var index in adjacent)
                {
                    if (index == serialIndex || index == nameIndex)
                    {
                        continue;
                    }

                    var text = GetByIndex(row, index);
                    var value = ParseSignedBalance(text);
                    if (value.HasValue && value.Value != 0m && !LooksLikeAccountSerial(text))
                    {
                        return new LegacyBalanceCandidate { Text = text, Value = value.Value, Label = label };
                    }
                }
            }

            for (var i = 0; i < row.Table.Columns.Count; i++)
            {
                if (i == serialIndex || i == nameIndex)
                {
                    continue;
                }

                var header = Clean(row.Table.Columns[i].ColumnName);
                if (header.IndexOf("ط±طµظٹط¯", StringComparison.OrdinalIgnoreCase) < 0 && header.IndexOf("balance", StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                var text = GetByIndex(row, i);
                var value = ParseSignedBalance(text);
                if (value.HasValue && value.Value != 0m && !LooksLikeAccountSerial(text))
                {
                    return new LegacyBalanceCandidate { Text = text, Value = value.Value, Label = FindNearestBalanceLabel(row, i) };
                }
            }

            return row.Table.Columns.Cast<DataColumn>()
                .Select((column, index) => new { Index = index, Text = GetByIndex(row, index), Value = ParseSignedBalance(GetByIndex(row, index)) })
                .Where(x => x.Index != serialIndex && x.Index != nameIndex && x.Value.HasValue && x.Value.Value != 0m && !LooksLikeAccountSerial(x.Text))
                .OrderBy(x => Math.Abs(x.Index - nameIndex))
                .Select(x => new LegacyBalanceCandidate { Text = x.Text, Value = x.Value.Value, Label = FindNearestBalanceLabel(row, x.Index) })
                .FirstOrDefault();
        }

        private static string FindNearestBalanceLabel(DataRow row, int amountIndex)
        {
            for (var distance = 1; distance <= 3; distance++)
            {
                var left = GetByIndex(row, amountIndex - distance);
                if (IsDebitLabel(left) || IsCreditLabel(left))
                {
                    return left;
                }

                var right = GetByIndex(row, amountIndex + distance);
                if (IsDebitLabel(right) || IsCreditLabel(right))
                {
                    return right;
                }
            }

            return string.Empty;
        }

        private static void ApplyLegacyOpeningBalanceSide(JournalEntryImportRowViewModel row, decimal signedBalance, string balanceLabel)
        {
            var abs = Math.Abs(signedBalance);
            var label = Clean(balanceLabel).ToLowerInvariant();
            if (IsDebitLabel(label))
            {
                row.Debit = abs;
                row.DebitText = abs.ToString("0.00", CultureInfo.InvariantCulture);
                return;
            }

            if (IsCreditLabel(label))
            {
                row.Credit = abs;
                row.CreditText = abs.ToString("0.00", CultureInfo.InvariantCulture);
                return;
            }

            if (signedBalance > 0m)
            {
                row.Debit = abs;
                row.DebitText = abs.ToString("0.00", CultureInfo.InvariantCulture);
            }
            else
            {
                row.Credit = abs;
                row.CreditText = abs.ToString("0.00", CultureInfo.InvariantCulture);
            }
        }

        private static DataTable ReadFirstWorksheet(string filePath)
        {
            return ReadWorksheets(filePath).First().Table;
        }

        private static IList<WorksheetData> ReadWorksheets(string filePath)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            var worksheets = new List<WorksheetData>();
            using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = ExcelReaderFactory.CreateReader(stream))
            {
                do
                {
                    var table = new DataTable();
                    var headersReady = false;
                    var columnCount = reader.FieldCount;
                    while (reader.Read())
                    {
                        if (!headersReady)
                        {
                            for (var i = 0; i < columnCount; i++)
                            {
                                var header = SafeCellText(reader.GetValue(i));
                                if (string.IsNullOrWhiteSpace(header))
                                {
                                    header = "Column" + (i + 1).ToString(CultureInfo.InvariantCulture);
                                }

                                if (table.Columns.Contains(header))
                                {
                                    header = header + "_" + (i + 1).ToString(CultureInfo.InvariantCulture);
                                }

                                table.Columns.Add(header, typeof(string));
                            }

                            headersReady = true;
                            continue;
                        }

                        var row = table.NewRow();
                        for (var i = 0; i < columnCount; i++)
                        {
                            row[i] = SafeCellText(reader.GetValue(i));
                        }

                        table.Rows.Add(row);
                    }

                    if (headersReady)
                    {
                        worksheets.Add(new WorksheetData { Name = reader.Name, Table = table });
                    }
                } while (reader.NextResult());

                if (worksheets.Count == 0)
                {
                    throw new InvalidOperationException("No worksheet was found in the uploaded Excel file.");
                }

                return worksheets;
            }
        }

        private static string SafeCellText(object value)
        {
            if (value == null || value == DBNull.Value)
            {
                return string.Empty;
            }

            if (value is DateTime)
            {
                return ((DateTime)value).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            }

            if (value is IFormattable)
            {
                return ((IFormattable)value).ToString(null, CultureInfo.InvariantCulture);
            }

            return Convert.ToString(value, CultureInfo.InvariantCulture);
        }

        private static string Get(DataRow row, params string[] names)
        {
            foreach (var name in names)
            {
                var column = row.Table.Columns.Cast<DataColumn>()
                    .FirstOrDefault(c => string.Equals(Normalize(c.ColumnName), Normalize(name), StringComparison.OrdinalIgnoreCase));

                if (column == null)
                {
                    continue;
                }

                return Clean(Convert.ToString(row[column]));
            }

            return string.Empty;
        }

        private static MasterDataImportRowViewModel ReadLegacyTawjerRow(DataRow dataRow, int rowNumber, IDictionary<int, string> lastSerialByLevel)
        {
            var level = ParseInt(GetByIndex(dataRow, 2));
            var accountKind = GetByIndex(dataRow, 3);
            var serial = GetByIndex(dataRow, 6);
            var row = new MasterDataImportRowViewModel
            {
                RowNumber = rowNumber,
                AccountSerial = serial,
                AccountName = GetByIndex(dataRow, 5),
                ParentAccountSerial = level.HasValue && level.Value > 1 && lastSerialByLevel.ContainsKey(level.Value - 1)
                    ? lastSerialByLevel[level.Value - 1]
                    : string.Empty,
                IsFinalAccount = accountKind.IndexOf("ظپط±ط¹ظٹ", StringComparison.OrdinalIgnoreCase) >= 0,
                Level = level,
                CurrencyCode = "1"
            };

            if (level.HasValue && !string.IsNullOrWhiteSpace(serial))
            {
                lastSerialByLevel[level.Value] = serial;
            }

            return row;
        }

        private static bool LooksLikeLegacyTawjerAccountTree(DataTable table)
        {
            if (table.Columns.Count < 7 || table.Rows.Count == 0)
            {
                return false;
            }

            var hits = 0;
            foreach (DataRow row in table.Rows.Cast<DataRow>().Take(10))
            {
                var kind = GetByIndex(row, 3);
                var name = GetByIndex(row, 5);
                var serial = GetByIndex(row, 6);
                if ((kind == "ط±ط¦ظٹط³ظٹ" || kind == "ظپط±ط¹ظٹ") && !string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(serial))
                {
                    hits++;
                }
            }

            return hits >= 2;
        }

        private static bool LooksLikeLegacyAccountBalanceReport(DataTable table)
        {
            if (table.Columns.Count < 4 || table.Rows.Count == 0)
            {
                return false;
            }

            var hits = 0;
            foreach (DataRow row in table.Rows.Cast<DataRow>().Take(20))
            {
                if (LooksLikeLegacyBalanceDataRow(row))
                {
                    hits++;
                }
            }

            return hits >= 2;
        }

        private static bool LooksLikeLegacyBalanceDataRow(DataRow row)
        {
            var cells = row.ItemArray.Select((value, index) => new { Index = index, Text = Clean(Convert.ToString(value)) })
                .Where(c => !string.IsNullOrWhiteSpace(c.Text))
                .ToList();
            var serials = cells.Where(c => LooksLikeAccountSerial(c.Text)).ToList();
            var names = cells.Where(c => LooksLikeAccountName(c.Text)).ToList();
            if (!serials.Any() || !names.Any())
            {
                return false;
            }

            var hasNearbyName = serials.Any(s => names.Any(n => Math.Abs(n.Index - s.Index) <= 2));
            var hasAmount = cells.Any(c => ParseSignedBalance(c.Text).HasValue && !LooksLikeAccountSerial(c.Text));
            return hasNearbyName && hasAmount;
        }

        private static string GetByIndex(DataRow row, int index)
        {
            return row.Table.Columns.Count > index ? Clean(Convert.ToString(row[index])) : string.Empty;
        }

        private static bool IsEmptyRow(DataRow row)
        {
            return row.ItemArray.All(v => EmptyValues.Contains(Clean(Convert.ToString(v))));
        }

        private static bool IsBlankChartRow(MasterDataImportRowViewModel row)
        {
            return string.IsNullOrWhiteSpace(row.AccountSerial)
                && string.IsNullOrWhiteSpace(row.AccountCode)
                && string.IsNullOrWhiteSpace(row.AccountName)
                && string.IsNullOrWhiteSpace(row.ParentAccountSerial)
                && string.IsNullOrWhiteSpace(row.ParentAccountCode);
        }

        private static bool IsIgnorableLegacyTawjerRow(MasterDataImportRowViewModel row)
        {
            return !row.Level.HasValue
                && string.IsNullOrWhiteSpace(row.AccountSerial)
                && string.IsNullOrWhiteSpace(row.AccountCode)
                && string.IsNullOrWhiteSpace(row.ParentAccountSerial)
                && string.IsNullOrWhiteSpace(row.ParentAccountCode);
        }

        private static string Clean(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            return value.Trim().Trim('\u200f', '\u200e');
        }

        private static string Normalize(string value)
        {
            return Clean(value).Replace(" ", string.Empty).Replace("_", string.Empty).Replace("-", string.Empty);
        }

        private static int? ParseInt(string value)
        {
            int result;
            return int.TryParse(value, out result) ? result : (int?)null;
        }

        private static bool ParseBoolean(string value, bool defaultValue)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return defaultValue;
            }

            value = value.Trim().ToLowerInvariant();
            return value == "1" || value == "true" || value == "yes" || value == "y" || value == "ظ†ظ‡ط§ط¦ظٹ" || value == "ط­ط³ط§ط¨ ظ†ظ‡ط§ط¦ظٹ";
        }

        private static decimal? ParseDecimal(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            decimal result;
            return decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out result) ? result : (decimal?)null;
        }

        private static DateTime? ParseDate(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            DateTime result;
            if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
            {
                return result;
            }

            double oaDate;
            if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out oaDate) && oaDate >= 1 && oaDate <= 2958465)
            {
                return DateTime.FromOADate(oaDate);
            }

            return null;
        }

        private static DateTime GetDefaultOpeningBalanceDate()
        {
            return new DateTime(DateTime.Today.Year, 1, 1);
        }

        private static void ApplyOpeningBalanceSide(JournalEntryImportRowViewModel row)
        {
            var balance = ParseDecimal(row.OpeningBalanceText).GetValueOrDefault();
            if (balance == 0m)
            {
                return;
            }

            var type = Clean(row.BalanceType).ToLowerInvariant();
            if (type == "debit" || type == "ظ…ط¯ظٹظ†" || type == "depit")
            {
                row.Debit = Math.Abs(balance);
                row.DebitText = row.Debit.ToString("0.00", CultureInfo.InvariantCulture);
            }
            else if (type == "credit" || type == "ط¯ط§ط¦ظ†")
            {
                row.Credit = Math.Abs(balance);
                row.CreditText = row.Credit.ToString("0.00", CultureInfo.InvariantCulture);
            }
        }

        private static string BuildJournalGroupKey(string fileName, string sheetName, bool hasGroupingColumn, JournalEntryImportRowViewModel row)
        {
            var key = FirstNonEmpty(row.ReferenceNo, row.EntryNo, row.VoucherNo);
            if (!string.IsNullOrWhiteSpace(key))
            {
                return fileName + "|" + sheetName + "|" + key;
            }

            if (hasGroupingColumn && row.EntryDate.HasValue)
            {
                return fileName + "|" + sheetName + "|" + row.EntryDate.Value.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
            }

            return fileName + "|" + sheetName + "|WHOLE_SHEET";
        }

        private static string FirstNonEmpty(params string[] values)
        {
            return values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v)) ?? string.Empty;
        }

        private static bool HasAnyColumn(DataTable table, params string[] names)
        {
            return names.Any(name => table.Columns.Cast<DataColumn>().Any(c => string.Equals(Normalize(c.ColumnName), Normalize(name), StringComparison.OrdinalIgnoreCase)));
        }

        private static string ComputeFileHash(string filePath)
        {
            using (var sha = System.Security.Cryptography.SHA256.Create())
            using (var stream = File.OpenRead(filePath))
            {
                return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", string.Empty);
            }
        }

        private static decimal? ParseSignedBalance(string value)
        {
            return ParseDecimal(value);
        }

        private static int? ParseBalanceType(string debit, string credit)
        {
            var debitValue = ParseDecimal(debit).GetValueOrDefault();
            var creditValue = ParseDecimal(credit).GetValueOrDefault();
            if (debitValue > 0)
            {
                return 0;
            }

            if (creditValue > 0)
            {
                return 1;
            }

            return null;
        }

        private static int? ParseBalanceTypeFromLabel(string primaryLabel, string fallbackLabel, decimal? signedBalance)
        {
            var label = Clean(primaryLabel);
            if (string.IsNullOrWhiteSpace(label))
            {
                label = Clean(fallbackLabel);
            }

            if (IsDebitLabel(label))
            {
                return 0;
            }

            if (IsCreditLabel(label))
            {
                return 1;
            }

            if (signedBalance.HasValue)
            {
                return signedBalance.Value < 0 ? 1 : 0;
            }

            return null;
        }

        private static bool LooksLikeNumericCode(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            return value.All(c => char.IsDigit(c) || c == '.' || c == '-' || c == '+');
        }

        private static bool LooksLikeAccountSerial(string value)
        {
            value = Clean(value).Replace(" ", string.Empty);
            if (value.Length < 3 || value.Length > 30)
            {
                return false;
            }

            if (value.IndexOf('.') >= 0 || value.IndexOf(',') >= 0 || value.IndexOf('-') >= 0 || value.IndexOf('+') >= 0)
            {
                return false;
            }

            if (!value.All(char.IsDigit))
            {
                return false;
            }

            return value.Trim('0').Length > 0;
        }

        private static bool LooksLikeAccountName(string value)
        {
            value = Clean(value);
            if (value.Length < 2 || LooksLikeAccountSerial(value) || ParseSignedBalance(value).HasValue)
            {
                return false;
            }

            if (IsDebitLabel(value) || IsCreditLabel(value))
            {
                return false;
            }

            var normalized = value.ToLowerInvariant();
            return normalized.IndexOf("account", StringComparison.OrdinalIgnoreCase) < 0
                && normalized.IndexOf("serial", StringComparison.OrdinalIgnoreCase) < 0
                && normalized.IndexOf("date", StringComparison.OrdinalIgnoreCase) < 0
                && normalized.IndexOf("\u0631\u0635\u064a\u062f", StringComparison.OrdinalIgnoreCase) < 0
                && normalized.IndexOf("\u0627\u0644\u062d\u0633\u0627\u0628", StringComparison.OrdinalIgnoreCase) < 0;
        }

        private static bool IsDebitLabel(string value)
        {
            value = Clean(value);
            return value == "\u0645\u062f\u064a\u0646" || value == "ظ…ط¯ظٹظ†" || value == "ط¸â€¦ط·آ¯ط¸ظ¹ط¸â€ " || string.Equals(value, "debit", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsCreditLabel(string value)
        {
            value = Clean(value);
            return value == "\u062f\u0627\u0626\u0646" || value == "ط¯ط§ط¦ظ†" || value == "ط·آ¯ط·آ§ط·آ¦ط¸â€ " || string.Equals(value, "credit", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsLikelyHeader(string serial, string name)
        {
            var combined = (serial + " " + name).ToLowerInvariant();
            return combined.Contains("account") || combined.Contains("serial") || combined.Contains("ط§ظ„ط­ط³ط§ط¨") || combined.Contains("ط§ط³ظ…");
        }

        public byte[] BuildTemplate()
        {
            var headers = new[]
            {
                "Account_Serial",
                "Account_Name",
                "Account_NameEng",
                "Parent_Account_Serial",
                "Account_Code",
                "Parent_Account_Code",
                "Level",
                "last_account",
                "currenct_code"
            };

            return BuildExcelHtml(headers, new[]
            {
                new[] { "1000", "ط§ظ„ط£طµظˆظ„", "Assets", "", "a1", "r", "1", "0", "1" },
                new[] { "1010", "ط§ظ„طµظ†ط¯ظˆظ‚", "Cash", "1000", "", "", "2", "1", "1" }
            });
        }

        public byte[] BuildAccountLinkedTemplate(string entityType)
        {
            var headers = new[]
            {
                "Debit",
                "Credit",
                "Phone",
                "Mobile",
                "Email",
                "Notes",
                "Name",
                "Account_Serial"
            };

            var name = entityType == MasterDataImportEntityType.Employees
                ? "ظ…ظˆط¸ظپ طھط¬ط±ظٹط¨ظٹ"
                : entityType == MasterDataImportEntityType.Suppliers
                    ? "ظ…ظˆط±ط¯ طھط¬ط±ظٹط¨ظٹ"
                    : "ط¹ظ…ظٹظ„ طھط¬ط±ظٹط¨ظٹ";

            return BuildExcelHtml(headers, new[]
            {
                new[] { "0", "0", "", "", "", "", name, "110101" }
            });
        }

        public byte[] BuildJournalTemplate()
        {
            var headers = new[]
            {
                "Entry Date",
                "Reference No",
                "Account Serial",
                "Account Name",
                "Debit",
                "Credit",
                "Description",
                "Branch",
                "Cost Center"
            };

            return BuildExcelHtml(headers, new[]
            {
                new[] { DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), "JV-1", "110101", "Cash", "1000", "0", "Imported journal line", "", "" },
                new[] { DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), "JV-1", "210101", "Counter account", "0", "1000", "Imported journal line", "", "" }
            });
        }

        public byte[] BuildOpeningBalanceTemplate()
        {
            var headers = new[]
            {
                "Account Serial",
                "Account Name",
                "Opening Balance",
                "Debit",
                "Credit",
                "Balance Type",
                "Branch",
                "Notes"
            };

            return BuildExcelHtml(headers, new[]
            {
                new[] { "110101", "Cash", "1000", "", "", "Debit", "", "Opening balance" }
            });
        }

        public byte[] BuildJournalErrorReport(IList<JournalEntryImportRowViewModel> rows)
        {
            var headers = new[]
            {
                "File",
                "Sheet",
                "Row",
                "Group",
                "Date",
                "Account Serial",
                "Account Name",
                "Debit",
                "Credit",
                "Opening Balance",
                "Status",
                "Errors"
            };

            var data = rows.Select(r => new[]
            {
                r.FileName,
                r.SheetName,
                r.RowNumber.ToString(CultureInfo.InvariantCulture),
                r.GroupKey,
                r.EntryDateText,
                r.AccountSerial,
                r.AccountName,
                r.DebitText,
                r.CreditText,
                r.OpeningBalanceText,
                r.IsValid ? "Valid" : "Error",
                r.ErrorDetails
            });

            return BuildExcelHtml(headers, data);
        }

        public byte[] BuildErrorReport(IList<MasterDataImportRowViewModel> rows)
        {
            var headers = new[]
            {
                "RowNumber",
                "Account_Serial",
                "EntityCode",
                "EntityName",
                "Account_Name",
                "Parent_Account_Serial",
                "Account_Code",
                "Parent_Account_Code",
                "IsValid",
                "Errors"
            };

            var data = rows.Select(r => new[]
            {
                r.RowNumber.ToString(),
                r.AccountSerial,
                r.EntityCode,
                r.EntityName,
                r.AccountName,
                r.ParentAccountSerial,
                r.AccountCode,
                r.ParentAccountCode,
                r.IsValid ? "Valid" : "Error",
                r.ErrorDetails
            });

            return BuildExcelHtml(headers, data);
        }

        private static byte[] BuildExcelHtml(IEnumerable<string> headers, IEnumerable<IEnumerable<string>> rows)
        {
            var sb = new StringBuilder();
            sb.Append("<html><head><meta charset=\"utf-8\" /></head><body><table border=\"1\"><tr>");
            foreach (var header in headers)
            {
                sb.Append("<th>").Append(Escape(header)).Append("</th>");
            }
            sb.Append("</tr>");
            foreach (var row in rows)
            {
                sb.Append("<tr>");
                foreach (var cell in row)
                {
                    sb.Append("<td style=\"mso-number-format:'\\@';\">").Append(Escape(cell)).Append("</td>");
                }
                sb.Append("</tr>");
            }
            sb.Append("</table></body></html>");
            return Encoding.UTF8.GetBytes(sb.ToString());
        }

        private static string Escape(string value)
        {
            return System.Web.HttpUtility.HtmlEncode(value ?? string.Empty);
        }

        private class WorksheetData
        {
            public string Name { get; set; }
            public DataTable Table { get; set; }
        }

        private class LegacyOpeningBalanceRow
        {
            public string AccountSerial { get; set; }
            public string AccountName { get; set; }
            public string BalanceText { get; set; }
            public decimal BalanceValue { get; set; }
            public string BalanceLabel { get; set; }
        }

        private class LegacyBalanceCandidate
        {
            public string Text { get; set; }
            public decimal Value { get; set; }
            public string Label { get; set; }
        }
    }
}
