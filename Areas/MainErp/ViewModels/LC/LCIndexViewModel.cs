namespace MyERP.Areas.MainErp.ViewModels.LC
{
    public class LCIndexViewModel
    {
        public LCIndexViewModel()
        {
            Items = new System.Collections.Generic.List<LCListItemViewModel>();
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
        public string NoteSerial2 { get; set; }
        public string NoteSerialOpen { get; set; }
        public double? OpenBalance { get; set; }
        public int? OpenBalanceType { get; set; }
        public string Warning { get; set; }
        public string NotesWarning { get; set; }
        public System.Collections.Generic.IList<LCLinkedNoteViewModel> LinkedNotes { get; private set; }
    }

    public class LCLinkedNoteViewModel
    {
        public int NoteID { get; set; }
        public System.DateTime? NoteDate { get; set; }
        public string NoteType { get; set; }
        public string NoteSerial { get; set; }
        public decimal? NoteValue { get; set; }
        public string Remark { get; set; }
    }
}
