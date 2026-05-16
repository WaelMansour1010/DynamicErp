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
            EntityType = MasterDataImportEntityType.ChartOfAccounts;
            StopOnAnyError = true;
            ImportMode = MasterDataImportMode.Merge;
            ImportBatches = new List<MasterDataImportBatchViewModel>();
        }

        public string EntityType { get; set; }
        public string FileName { get; set; }
        public bool StopOnAnyError { get; set; }
        public bool AutoBalanceOpening { get; set; }
        public string ImportMode { get; set; }
        public IList<MasterDataImportEntityType> EntityTypes { get; set; }
        public IList<MasterDataImportRowViewModel> Rows { get; set; }
        public IList<JournalEntryImportRowViewModel> JournalRows { get; set; }
        public IList<MasterDataImportBatchViewModel> ImportBatches { get; set; }
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
                new MasterDataImportEntityType { Value = JournalEntries, Text = "Journal Entries Import / استيراد القيود" },
                new MasterDataImportEntityType { Value = OpeningBalances, Text = "Opening Balances Import / استيراد الأرصدة الافتتاحية" },
                new MasterDataImportEntityType { Value = ChartOfAccounts, Text = "Chart of Accounts / ط¯ظ„ظٹظ„ ط§ظ„ط­ط³ط§ط¨ط§طھ" },
                new MasterDataImportEntityType { Value = Customers, Text = "Customers / ط§ظ„ط¹ظ…ظ„ط§ط،" },
                new MasterDataImportEntityType { Value = Suppliers, Text = "Suppliers / ط§ظ„ظ…ظˆط±ط¯ظٹظ†" },
                new MasterDataImportEntityType { Value = Employees, Text = "Employees / ط§ظ„ظ…ظˆط¸ظپظٹظ†" },
                new MasterDataImportEntityType { Value = "Items", Text = "Items / ط§ظ„ط£طµظ†ط§ظپ (ظ‚ط§ظ„ط¨ ظ‚ظٹط¯ ط§ظ„ط§ط¹طھظ…ط§ط¯)" }
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
            FileHashes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        public string EntityType { get; set; }
        public string FileName { get; set; }
        public string ImportMode { get; set; }
        public bool AutoBalanceOpening { get; set; }
        public IList<MasterDataImportRowViewModel> Rows { get; set; }
        public IList<JournalEntryImportRowViewModel> JournalRows { get; set; }
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
        public string OpeningBalanceText { get; set; }
        public string BalanceType { get; set; }
        public bool IsOpeningBalance { get; set; }
        public string IntermediateAccountCode { get; set; }
        public string IntermediateAccountName { get; set; }
        public bool IsValid { get { return Errors.Count == 0; } }
        public IList<string> Errors { get; set; }
        public string ErrorDetails { get { return string.Join("; ", Errors); } }
    }
}
