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
            EntityType = MasterDataImportEntityType.ChartOfAccounts;
            StopOnAnyError = true;
        }

        public string EntityType { get; set; }
        public string FileName { get; set; }
        public bool StopOnAnyError { get; set; }
        public IList<MasterDataImportEntityType> EntityTypes { get; set; }
        public IList<MasterDataImportRowViewModel> Rows { get; set; }
        public int TotalRows { get { return Rows.Count; } }
        public int ValidRows { get { return Rows.Count(r => r.IsValid); } }
        public int ErrorRows { get { return Rows.Count(r => !r.IsValid); } }
        public bool HasPreview { get { return Rows.Count > 0; } }
    }

    public class MasterDataImportEntityType
    {
        public const string ChartOfAccounts = "ChartOfAccounts";
        public const string Customers = "Customers";
        public const string Suppliers = "Suppliers";
        public const string Employees = "Employees";

        public string Value { get; set; }
        public string Text { get; set; }

        public static IList<MasterDataImportEntityType> GetAll()
        {
            return new List<MasterDataImportEntityType>
            {
                new MasterDataImportEntityType { Value = ChartOfAccounts, Text = "Chart of Accounts / دليل الحسابات" },
                new MasterDataImportEntityType { Value = Customers, Text = "Customers / العملاء" },
                new MasterDataImportEntityType { Value = Suppliers, Text = "Suppliers / الموردين" },
                new MasterDataImportEntityType { Value = Employees, Text = "Employees / الموظفين" },
                new MasterDataImportEntityType { Value = "Items", Text = "Items / الأصناف (template placeholder)" }
            };
        }
    }

    [Serializable]
    public class MasterDataImportPreview
    {
        public MasterDataImportPreview()
        {
            Rows = new List<MasterDataImportRowViewModel>();
        }

        public string EntityType { get; set; }
        public string FileName { get; set; }
        public IList<MasterDataImportRowViewModel> Rows { get; set; }
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
        public string Message { get; set; }
    }
}
