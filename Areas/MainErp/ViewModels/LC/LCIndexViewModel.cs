namespace MyERP.Areas.MainErp.ViewModels.LC
{
    public class LCIndexViewModel
    {
        public LCIndexViewModel()
        {
            Items = new System.Collections.Generic.List<LCListItemViewModel>();
            Banks = new System.Collections.Generic.List<LCLookupOption>();
            Vendors = new System.Collections.Generic.List<LCLookupOption>();
            Branches = new System.Collections.Generic.List<LCLookupOption>();
        }

        public string Title { get; set; }
        public string ArabicTitle { get; set; }
        public string AnalysisStatus { get; set; }
        public string SearchText { get; set; }
        public int? BankId { get; set; }
        public int? VendorId { get; set; }
        public int? BranchId { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public string Warning { get; set; }
        public int? SelectedId { get; set; }
        public LCDetailsViewModel SelectedDetails { get; set; }
        public System.Collections.Generic.IList<LCListItemViewModel> Items { get; private set; }
        public System.Collections.Generic.IList<LCLookupOption> Banks { get; private set; }
        public System.Collections.Generic.IList<LCLookupOption> Vendors { get; private set; }
        public System.Collections.Generic.IList<LCLookupOption> Branches { get; private set; }
    }

    public class LCListItemViewModel
    {
        public int TblLCID { get; set; }
        public string LCNO { get; set; }
        public string Name { get; set; }
        public string BankName { get; set; }
        public string CurrencyName { get; set; }
        public string VendorName { get; set; }
        public decimal? Value { get; set; }
        public System.DateTime? FromDate { get; set; }
        public System.DateTime? ToDate { get; set; }
        public int? BranchID { get; set; }
        public string AccountCode { get; set; }
    }

    public class LCDetailsViewModel : LCListItemViewModel
    {
        public LCDetailsViewModel()
        {
            LinkedNotes = new System.Collections.Generic.List<LCLinkedNoteViewModel>();
            AccountLinks = new System.Collections.Generic.List<LCAccountLinkViewModel>();
            GridSections = new System.Collections.Generic.List<LCGridSectionViewModel>();
            VoucherLines = new System.Collections.Generic.List<LCVoucherLineViewModel>();
            AuditEntries = new System.Collections.Generic.List<LCAuditEntryViewModel>();
            Warnings = new System.Collections.Generic.List<string>();
        }

        public string Remarks { get; set; }
        public string MarginAccountCode { get; set; }
        public string AcceptAccountCode { get; set; }
        public string ExpenseAccountCode { get; set; }
        public string AccountExpProject { get; set; }
        public string AccountLGParent { get; set; }
        public string AccountMarginParent { get; set; }
        public string AccountAcceptanceParent { get; set; }
        public string AccountExpensParent { get; set; }
        public decimal? OpenValue { get; set; }
        public double? CurrencyRate { get; set; }
        public decimal? PercentV { get; set; }
        public int? LcTypeId { get; set; }
        public int? BankId { get; set; }
        public int? BankId2 { get; set; }
        public int? BoxId { get; set; }
        public int? CurrencyId { get; set; }
        public int? VendorId { get; set; }
        public int? CountryId { get; set; }
        public int? ProjectId { get; set; }
        public string ProjectName { get; set; }
        public int? PaymentTypeId { get; set; }
        public string ChequeNumber { get; set; }
        public System.DateTime? ChequeDueDate { get; set; }
        public double? OpeningBalanceVoucherId { get; set; }
        public System.DateTime? CloseDate { get; set; }
        public System.DateTime? LastParcilDate { get; set; }
        public bool? Locked { get; set; }
        public int? UserId { get; set; }
        public string NoteSerial { get; set; }
        public int? NoteID { get; set; }
        public string NoteSerial2 { get; set; }
        public int? NoteID2 { get; set; }
        public string NoteSerialOpen { get; set; }
        public int? NoteIDOpen { get; set; }
        public double? OpenBalance { get; set; }
        public int? OpenBalanceType { get; set; }
        public string Warning { get; set; }
        public string NotesWarning { get; set; }
        public System.Collections.Generic.IList<LCLinkedNoteViewModel> LinkedNotes { get; private set; }
        public System.Collections.Generic.IList<LCAccountLinkViewModel> AccountLinks { get; private set; }
        public System.Collections.Generic.IList<LCGridSectionViewModel> GridSections { get; private set; }
        public System.Collections.Generic.IList<LCVoucherLineViewModel> VoucherLines { get; private set; }
        public System.Collections.Generic.IList<LCAuditEntryViewModel> AuditEntries { get; private set; }
        public System.Collections.Generic.IList<string> Warnings { get; private set; }
    }

    public class LCEditViewModel
    {
        public LCEditViewModel()
        {
            LcTypes = new System.Collections.Generic.List<LCLookupOption>();
            Banks = new System.Collections.Generic.List<LCLookupOption>();
            Boxes = new System.Collections.Generic.List<LCLookupOption>();
            Currencies = new System.Collections.Generic.List<LCLookupOption>();
            Countries = new System.Collections.Generic.List<LCLookupOption>();
            Vendors = new System.Collections.Generic.List<LCLookupOption>();
            Branches = new System.Collections.Generic.List<LCLookupOption>();
            Accounts = new System.Collections.Generic.List<LCLookupOption>();
            HistoryRows = new System.Collections.Generic.List<LcHistoryEditRowViewModel>();
            MarginRows = new System.Collections.Generic.List<LcMarginEditRowViewModel>();
            Margin2Rows = new System.Collections.Generic.List<LcMarginEditRowViewModel>();
            OpenBalanceRows = new System.Collections.Generic.List<LcOpenBalanceEditRowViewModel>();
        }

        public int? TblLCID { get; set; }
        public string LCNO { get; set; }
        public int? LCTyperId { get; set; }
        public int? BankId { get; set; }
        public int? BankID2 { get; set; }
        public int? BoxID { get; set; }
        public decimal? Value { get; set; }
        public decimal? OpenValue { get; set; }
        public int? CurrencyId { get; set; }
        public decimal? CurrencyRate { get; set; }
        public decimal? PercentV { get; set; }
        public int? VendorId { get; set; }
        public int? CountryId { get; set; }
        public System.DateTime? FromDate { get; set; }
        public System.DateTime? ToDate { get; set; }
        public System.DateTime? CloseDate { get; set; }
        public System.DateTime? LastParcilDate { get; set; }
        public System.DateTime? OpenBalanceDate { get; set; }
        public int? BranchID { get; set; }
        public string Remarks { get; set; }
        public int? ProjectId { get; set; }
        public string ProjectName { get; set; }
        public int? PaymentTypeID { get; set; }
        public string ChequeNumber { get; set; }
        public System.DateTime? ChequeDueDate { get; set; }
        public bool Locked { get; set; }
        public decimal? OpenBalance { get; set; }
        public int? OpenBalanceType { get; set; }
        public double? OpeningBalanceVoucherId { get; set; }
        public string AccountLGParent { get; set; }
        public string AccountMarginParent { get; set; }
        public string AccountAcceptanceParent { get; set; }
        public string AccountExpensParent { get; set; }
        public string LCAccountCode { get; set; }
        public string MarginAccountCode { get; set; }
        public string AcceptanceAccountCode { get; set; }
        public string ExpenseAccountCode { get; set; }
        public string ProjectExpenseAccountCode { get; set; }
        public bool AutoCreateMissingAccounts { get; set; }
        public bool HasPostedVoucher { get; set; }
        public string Warning { get; set; }
        public System.Collections.Generic.IList<LCLookupOption> LcTypes { get; private set; }
        public System.Collections.Generic.IList<LCLookupOption> Banks { get; private set; }
        public System.Collections.Generic.IList<LCLookupOption> Boxes { get; private set; }
        public System.Collections.Generic.IList<LCLookupOption> Currencies { get; private set; }
        public System.Collections.Generic.IList<LCLookupOption> Countries { get; private set; }
        public System.Collections.Generic.IList<LCLookupOption> Vendors { get; private set; }
        public System.Collections.Generic.IList<LCLookupOption> Branches { get; private set; }
        public System.Collections.Generic.IList<LCLookupOption> Accounts { get; private set; }
        public System.Collections.Generic.IList<LcHistoryEditRowViewModel> HistoryRows { get; private set; }
        public System.Collections.Generic.IList<LcMarginEditRowViewModel> MarginRows { get; private set; }
        public System.Collections.Generic.IList<LcMarginEditRowViewModel> Margin2Rows { get; private set; }
        public System.Collections.Generic.IList<LcOpenBalanceEditRowViewModel> OpenBalanceRows { get; private set; }
    }

    public class LCLookupOption
    {
        public string Value { get; set; }
        public string Text { get; set; }
    }

    public class LcHistoryEditRowViewModel
    {
        public int? ID { get; set; }
        public int? Serial { get; set; }
        public decimal? GuaranteeAmount { get; set; }
        public decimal? AmountPlus { get; set; }
        public decimal? AmountMin { get; set; }
        public decimal? Total { get; set; }
        public int? MarginNo { get; set; }
        public int? NoteID { get; set; }
        public int? NoteSerial { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
    }

    public class LcMarginEditRowViewModel
    {
        public int? ID { get; set; }
        public int? Serial { get; set; }
        public int? MarginNo { get; set; }
        public System.DateTime? GuaranteeDate { get; set; }
        public decimal? Amount { get; set; }
        public string MarginAccountCode { get; set; }
        public string BankAccountCode { get; set; }
        public string AccountMargen2 { get; set; }
        public decimal? MargenValue { get; set; }
        public System.DateTime? OrderDate { get; set; }
        public System.DateTime? PayDate { get; set; }
        public int? Type { get; set; }
        public decimal? PayedAmount { get; set; }
        public decimal? StillAmount { get; set; }
        public int? NoteID { get; set; }
        public int? NoteSerial { get; set; }
        public int? NoteID2 { get; set; }
        public int? NoteSerial2 { get; set; }
        public bool IsFullPayed { get; set; }
        public string BankAccountCode2 { get; set; }
        public bool IsOpenBalance { get; set; }
    }

    public class LcOpenBalanceEditRowViewModel
    {
        public int? ID { get; set; }
        public int? Serial { get; set; }
        public int? MarginNo { get; set; }
        public System.DateTime? GuaranteeDate { get; set; }
        public decimal? Amount { get; set; }
        public decimal? AmountP { get; set; }
        public decimal? TotalAmount { get; set; }
        public decimal? ExpAmount { get; set; }
        public decimal? InsuranceAmount { get; set; }
        public decimal? PercentA { get; set; }
        public string MarginAccountCode { get; set; }
        public string BankAccountCode { get; set; }
        public System.DateTime? PayDate { get; set; }
        public int? Type { get; set; }
        public int? NoteID { get; set; }
        public int? NoteSerial { get; set; }
        public int? NoteID2 { get; set; }
        public int? NoteSerial2 { get; set; }
        public bool IsFullPayed { get; set; }
    }

    public class LCPostingResultViewModel
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public int TblLCID { get; set; }
        public int? NoteId { get; set; }
        public long? VoucherId { get; set; }
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
    }

    public class LCLinkedNoteViewModel
    {
        public int NoteID { get; set; }
        public string SourceTable { get; set; }
        public System.DateTime? NoteDate { get; set; }
        public string NoteType { get; set; }
        public string NoteSerial { get; set; }
        public int? DoubleEntryVoucherId { get; set; }
        public decimal? NoteValue { get; set; }
        public string Remark { get; set; }
    }

    public class LCAccountLinkViewModel
    {
        public string Role { get; set; }
        public string AccountCode { get; set; }
        public string AccountSerial { get; set; }
        public string AccountName { get; set; }
        public string AccountDisplay { get; set; }
        public string ParentAccountCode { get; set; }
        public string ParentAccountSerial { get; set; }
        public string ParentAccountName { get; set; }
        public string ParentAccountDisplay { get; set; }
        public bool? ParentIsLastAccount { get; set; }
        public string Warning { get; set; }
    }

    public class LCGridSectionViewModel
    {
        public LCGridSectionViewModel()
        {
            Columns = new System.Collections.Generic.List<string>();
            Rows = new System.Collections.Generic.List<LCGridRowViewModel>();
        }

        public string Title { get; set; }
        public string Vb6GridName { get; set; }
        public string SourceTable { get; set; }
        public string Behavior { get; set; }
        public string Warning { get; set; }
        public System.Collections.Generic.IList<string> Columns { get; private set; }
        public System.Collections.Generic.IList<LCGridRowViewModel> Rows { get; private set; }
    }

    public class LCGridRowViewModel
    {
        public LCGridRowViewModel()
        {
            Values = new System.Collections.Generic.Dictionary<string, string>();
        }

        public int RowNumber { get; set; }
        public int? RowId { get; set; }
        public int? NoteId { get; set; }
        public int? NoteId2 { get; set; }
        public int? NoteId3 { get; set; }
        public double? OpeningBalanceVoucherId { get; set; }
        public System.Collections.Generic.IDictionary<string, string> Values { get; private set; }
    }

    public class LCVoucherLineViewModel
    {
        public string SourceTable { get; set; }
        public int VoucherId { get; set; }
        public int? LineNo { get; set; }
        public int? NoteId { get; set; }
        public string NoteSerial { get; set; }
        public System.DateTime? RecordDate { get; set; }
        public string AccountCode { get; set; }
        public string AccountName { get; set; }
        public string AccountSerial { get; set; }
        public string AccountDisplay { get; set; }
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public string Description { get; set; }
        public int? BranchId { get; set; }
        public int? ProjectId { get; set; }
        public double? OpeningBalanceVoucherId { get; set; }
    }

    public class LCAuditEntryViewModel
    {
        public int AuditId { get; set; }
        public string OperationName { get; set; }
        public string EntityName { get; set; }
        public string EntityKey { get; set; }
        public int? UserId { get; set; }
        public string UserName { get; set; }
        public System.Guid? CorrelationId { get; set; }
        public string Message { get; set; }
        public string BeforeSnapshot { get; set; }
        public string AfterSnapshot { get; set; }
        public System.DateTime? CreatedAt { get; set; }
    }
}
