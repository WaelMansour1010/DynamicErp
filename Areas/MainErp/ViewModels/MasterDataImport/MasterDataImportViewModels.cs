using System;
using System.Collections.Generic;
using System.Linq;

namespace MyERP.Areas.MainErp.ViewModels.MasterDataImport
{
    public class MasterDataImportIndexViewModel
    {
        public MasterDataImportIndexViewModel()
        {
            EntityTypes = MasterDataImportEntityType.GetAll();
            Rows = new List<MasterDataImportRowViewModel>();
            JournalRows = new List<JournalEntryImportRowViewModel>();
            WorksheetDiagnostics = new List<MasterDataImportWorksheetDiagnosticViewModel>();
            EntityType = MasterDataImportEntityType.ChartOfAccounts;
            StopOnAnyError = true;
            ImportMode = MasterDataImportMode.Merge;
            ImportBatches = new List<MasterDataImportBatchViewModel>();
            CurrentReviewItems = new List<JournalImportReviewItemViewModel>();
        }

        public string EntityType { get; set; }
        public string FileName { get; set; }
        public bool StopOnAnyError { get; set; }
        public bool AutoBalanceOpening { get; set; }
        public string ImportMode { get; set; }
        public IList<MasterDataImportEntityType> EntityTypes { get; set; }
        public IList<MasterDataImportRowViewModel> Rows { get; set; }
        public IList<JournalEntryImportRowViewModel> JournalRows { get; set; }
        public IList<MasterDataImportWorksheetDiagnosticViewModel> WorksheetDiagnostics { get; set; }
        public IList<MasterDataImportBatchViewModel> ImportBatches { get; set; }
        public IList<JournalImportReviewItemViewModel> CurrentReviewItems { get; set; }
        public JournalImportReviewSnapshotViewModel LastImportReview { get; set; }
        public bool IsJournalImport { get { return EntityType == MasterDataImportEntityType.JournalEntries; } }
        public bool IsOpeningBalanceImport { get { return EntityType == MasterDataImportEntityType.OpeningBalances; } }
        public bool UsesJournalGrid { get { return IsJournalImport || IsOpeningBalanceImport; } }
        public int TotalRows { get { return UsesJournalGrid ? JournalRows.Count : Rows.Count; } }
        public int ValidRows { get { return UsesJournalGrid ? JournalRows.Count(r => r.IsValid) : Rows.Count(r => r.IsValid); } }
        public int ErrorRows { get { return UsesJournalGrid ? JournalRows.Count(r => !r.IsValid) : Rows.Count(r => !r.IsValid); } }
        public int JournalCount { get { return UsesJournalGrid ? JournalRows.Select(r => r.GroupKey).Distinct().Count() : 0; } }
        public decimal TotalDebit { get { return UsesJournalGrid ? JournalRows.Sum(r => r.Debit) : 0m; } }
        public decimal TotalCredit { get { return UsesJournalGrid ? JournalRows.Sum(r => r.Credit) : 0m; } }
        public decimal Difference { get { return TotalDebit - TotalCredit; } }
        public bool HasPreview { get { return TotalRows > 0; } }
    }

    public class MasterDataImportEntityType
    {
        public const string ChartOfAccounts = "ChartOfAccounts";
        public const string Customers = "Customers";
        public const string Suppliers = "Suppliers";
        public const string Employees = "Employees";
        public const string JournalEntries = "JournalEntries";
        public const string OpeningBalances = "OpeningBalances";

        public string Value { get; set; }
        public string Text { get; set; }

        public static IList<MasterDataImportEntityType> GetAll()
        {
            return new List<MasterDataImportEntityType>
            {
                new MasterDataImportEntityType { Value = JournalEntries, Text = "Journal Entries Import / \u0627\u0633\u062a\u064a\u0631\u0627\u062f \u0627\u0644\u0642\u064a\u0648\u062f" },
                new MasterDataImportEntityType { Value = OpeningBalances, Text = "Opening Balances Import / \u0627\u0633\u062a\u064a\u0631\u0627\u062f \u0627\u0644\u0623\u0631\u0635\u062f\u0629 \u0627\u0644\u0627\u0641\u062a\u062a\u0627\u062d\u064a\u0629" },
                new MasterDataImportEntityType { Value = ChartOfAccounts, Text = "Chart of Accounts / \u062f\u0644\u064a\u0644 \u0627\u0644\u062d\u0633\u0627\u0628\u0627\u062a" },
                new MasterDataImportEntityType { Value = Customers, Text = "Customers / \u0627\u0644\u0639\u0645\u0644\u0627\u0621" },
                new MasterDataImportEntityType { Value = Suppliers, Text = "Suppliers / \u0627\u0644\u0645\u0648\u0631\u062f\u064a\u0646" },
                new MasterDataImportEntityType { Value = Employees, Text = "Employees / \u0627\u0644\u0645\u0648\u0638\u0641\u064a\u0646" },
                new MasterDataImportEntityType { Value = "Items", Text = "Items / \u0627\u0644\u0623\u0635\u0646\u0627\u0641 (\u0642\u0627\u0644\u0628 \u0642\u064a\u062f \u0627\u0644\u0627\u0639\u062a\u0645\u0627\u062f)" }
            };
        }
    }

    public static class MasterDataImportMode
    {
        public const string Merge = "Merge";
        public const string Replace = "Replace";

        public static bool IsReplace(string value)
        {
            return string.Equals(value, Replace, StringComparison.OrdinalIgnoreCase);
        }

        public static string Normalize(string value)
        {
            return IsReplace(value) ? Replace : Merge;
        }
    }

    [Serializable]
    public class MasterDataImportPreview
    {
        public MasterDataImportPreview()
        {
            Rows = new List<MasterDataImportRowViewModel>();
            JournalRows = new List<JournalEntryImportRowViewModel>();
            WorksheetDiagnostics = new List<MasterDataImportWorksheetDiagnosticViewModel>();
            FileHashes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        public string EntityType { get; set; }
        public string FileName { get; set; }
        public string ImportMode { get; set; }
        public bool AutoBalanceOpening { get; set; }
        public IList<MasterDataImportRowViewModel> Rows { get; set; }
        public IList<JournalEntryImportRowViewModel> JournalRows { get; set; }
        public IList<MasterDataImportWorksheetDiagnosticViewModel> WorksheetDiagnostics { get; set; }
        public IDictionary<string, string> FileHashes { get; set; }
    }

    [Serializable]
    public class MasterDataImportRowViewModel
    {
        public MasterDataImportRowViewModel()
        {
            Errors = new List<string>();
        }

        public int RowNumber { get; set; }
        public string AccountCode { get; set; }
        public string AccountSerial { get; set; }
        public string AccountName { get; set; }
        public string EntityCode { get; set; }
        public string EntityName { get; set; }
        public string EntityType { get; set; }
        public string Phone { get; set; }
        public string Mobile { get; set; }
        public string Email { get; set; }
        public decimal? OpeningBalance { get; set; }
        public int? OpeningBalanceType { get; set; }
        public int? ImportedEntityId { get; set; }
        public string AccountNameEnglish { get; set; }
        public string ParentAccountCode { get; set; }
        public string ParentAccountSerial { get; set; }
        public string ResolvedParentAccountCode { get; set; }
        public bool IsFinalAccount { get; set; }
        public int? Level { get; set; }
        public string CurrencyCode { get; set; }
        public int? AccountTypes { get; set; }
        public int? AccountTab { get; set; }
        public int? DebitOrCredit { get; set; }
        public int? DifferentType { get; set; }
        public int? Authority { get; set; }
        public bool IsValid { get { return Errors.Count == 0; } }
        public IList<string> Errors { get; set; }
        public string ErrorDetails { get { return string.Join("; ", Errors); } }
        public string ImportedAccountCode { get; set; }
    }

    public class MasterDataImportResultViewModel
    {
        public int BatchId { get; set; }
        public int TotalRows { get; set; }
        public int SuccessRows { get; set; }
        public int FailedRows { get; set; }
        public int ImportedJournalCount { get; set; }
        public string Message { get; set; }
        public JournalImportReviewSnapshotViewModel ReviewSnapshot { get; set; }
    }

    public class MasterDataImportBatchViewModel
    {
        public int BatchId { get; set; }
        public string FileName { get; set; }
        public string EntityType { get; set; }
        public DateTime ImportStartedAt { get; set; }
        public int SuccessRows { get; set; }
        public int FailedRows { get; set; }
        public string Status { get; set; }
        public int CreatedAccounts { get; set; }
        public bool HasReview { get; set; }
        public bool CanRollback
        {
            get
            {
                return string.Equals(Status, "Completed", StringComparison.OrdinalIgnoreCase)
                    && string.Equals(EntityType, MasterDataImportEntityType.ChartOfAccounts, StringComparison.OrdinalIgnoreCase)
                    && CreatedAccounts > 0;
            }
        }
        public bool CanReview
        {
            get
            {
                return string.Equals(Status, "Completed", StringComparison.OrdinalIgnoreCase)
                    && (string.Equals(EntityType, MasterDataImportEntityType.JournalEntries, StringComparison.OrdinalIgnoreCase)
                        || string.Equals(EntityType, MasterDataImportEntityType.OpeningBalances, StringComparison.OrdinalIgnoreCase));
            }
        }
    }

    [Serializable]
    public class MasterDataImportWorksheetDiagnosticViewModel
    {
        public string FileName { get; set; }
        public string SheetName { get; set; }
        public string UsedRange { get; set; }
        public int? HeaderRowNumber { get; set; }
        public int DataRowsCount { get; set; }
        public string SkipReason { get; set; }
        public string DetectedAccountSerialColumn { get; set; }
        public string DetectedAccountNameColumn { get; set; }
        public string DetectedBalanceColumn { get; set; }
        public string DetectedDebitColumn { get; set; }
        public string DetectedCreditColumn { get; set; }
        public string DetectedAmountColumns { get; set; }
        public IList<MasterDataImportColumnDiagnosticViewModel> ColumnDiagnostics { get; set; }
        public IList<string> ParsedRowPreview { get; set; }
        public bool IsSkipped { get { return !string.IsNullOrWhiteSpace(SkipReason); } }
    }

    [Serializable]
    public class MasterDataImportColumnDiagnosticViewModel
    {
        public int ColumnIndex { get; set; }
        public string HeaderText { get; set; }
        public string SampleValues { get; set; }
        public decimal DigitOnlyRatio { get; set; }
        public decimal DecimalOrCommaRatio { get; set; }
        public decimal ZeroRatio { get; set; }
        public int DistinctCount { get; set; }
        public int NonEmptyCount { get; set; }
        public decimal FinalScore { get; set; }
        public string AcceptedRole { get; set; }
        public string Decision { get; set; }
        public string Reason { get; set; }
    }

    [Serializable]
    public class JournalEntryImportRowViewModel
    {
        public JournalEntryImportRowViewModel()
        {
            Errors = new List<string>();
        }

        public string FileName { get; set; }
        public string SheetName { get; set; }
        public int RowNumber { get; set; }
        public string GroupKey { get; set; }
        public string ReferenceNo { get; set; }
        public string EntryNo { get; set; }
        public string VoucherNo { get; set; }
        public DateTime? EntryDate { get; set; }
        public string EntryDateText { get; set; }
        public string AccountSerial { get; set; }
        public string AccountName { get; set; }
        public string AccountCode { get; set; }
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public string DebitText { get; set; }
        public string CreditText { get; set; }
        public string Description { get; set; }
        public string Branch { get; set; }
        public int? BranchId { get; set; }
        public string CostCenter { get; set; }
        public string FileHash { get; set; }
        public bool AccountWillBeCreated { get; set; }
        public int? ManagedEntityType { get; set; }
        public string OpeningBalanceText { get; set; }
        public string BalanceType { get; set; }
        public bool IsOpeningBalance { get; set; }
        public string IntermediateAccountCode { get; set; }
        public string IntermediateAccountName { get; set; }
        public bool IsValid { get { return Errors.Count == 0; } }
        public IList<string> Errors { get; set; }
        public string ErrorDetails { get { return string.Join("; ", Errors); } }
    }

    [Serializable]
    public class JournalImportReviewSnapshotViewModel
    {
        public JournalImportReviewSnapshotViewModel()
        {
            Items = new List<JournalImportReviewItemViewModel>();
        }

        public int BatchId { get; set; }
        public string EntityType { get; set; }
        public string Title { get; set; }
        public string SourceFileName { get; set; }
        public int ImportedJournalCount { get; set; }
        public IList<JournalImportReviewItemViewModel> Items { get; set; }
    }

    [Serializable]
    public class JournalImportReviewItemViewModel
    {
        public string FileName { get; set; }
        public string SheetName { get; set; }
        public string GroupKey { get; set; }
        public int RowCount { get; set; }
        public decimal TotalDebit { get; set; }
        public decimal TotalCredit { get; set; }
        public decimal Difference { get; set; }
        public string FirstAccountSerial { get; set; }
        public string LastAccountSerial { get; set; }
        public long? VoucherId { get; set; }
        public long? NoteId { get; set; }
        public long? NoteSerial { get; set; }
        public string ReviewStatus { get; set; }
        public string ReviewSource { get; set; }
    }
}
