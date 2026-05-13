using System;
using System.Collections.Generic;

namespace MyERP.Areas.MainErp.ViewModels.FinancialExpenses
{
    public class FinancialExpensesIndexViewModel
    {
        public FinancialExpensesIndexViewModel()
        {
            Documents = new List<FinancialExpenseListItem>();
            Branches = new List<FinancialExpenseLookupItem>();
            Boxes = new List<FinancialExpenseLookupItem>();
            Banks = new List<FinancialExpenseLookupItem>();
            Vendors = new List<FinancialExpenseLookupItem>();
            ExpensesTypes = new List<FinancialExpenseLookupItem>();
            Accounts = new List<FinancialExpenseLookupItem>();
            Permissions = new FinancialExpensePermissions();
        }

        public string Mode { get; set; }
        public string SearchText { get; set; }
        public IList<FinancialExpenseListItem> Documents { get; set; }
        public IList<FinancialExpenseLookupItem> Branches { get; set; }
        public IList<FinancialExpenseLookupItem> Boxes { get; set; }
        public IList<FinancialExpenseLookupItem> Banks { get; set; }
        public IList<FinancialExpenseLookupItem> Vendors { get; set; }
        public IList<FinancialExpenseLookupItem> ExpensesTypes { get; set; }
        public IList<FinancialExpenseLookupItem> Accounts { get; set; }
        public FinancialExpensePermissions Permissions { get; set; }
    }

    public class FinancialExpensePermissions
    {
        public bool CanView { get; set; }
        public bool CanAdd { get; set; }
        public bool CanEdit { get; set; }
        public bool CanDelete { get; set; }
    }

    public class FinancialExpenseLookupItem
    {
        public string Id { get; set; }
        public string Text { get; set; }
        public string AccountCode { get; set; }
    }

    public class FinancialExpenseListItem
    {
        public int Id { get; set; }
        public int NoteType { get; set; }
        public string JournalSerial { get; set; }
        public string VoucherSerial { get; set; }
        public DateTime? Date { get; set; }
        public decimal Value { get; set; }
        public string BranchName { get; set; }
        public string Remark { get; set; }
    }

    public class FinancialExpenseDetails
    {
        public FinancialExpenseDetails()
        {
            Lines = new List<FinancialExpenseLine>();
        }

        public int? Id { get; set; }
        public int? DetailNoteId { get; set; }
        public string Mode { get; set; }
        public int NoteType { get; set; }
        public DateTime? Date { get; set; }
        public string JournalSerial { get; set; }
        public string VoucherSerial { get; set; }
        public int BillType { get; set; }
        public int PaymentType { get; set; }
        public int? BranchId { get; set; }
        public int? BoxId { get; set; }
        public int? BankId { get; set; }
        public int? VendorId { get; set; }
        public string ChequeNumber { get; set; }
        public DateTime? ChequeDueDate { get; set; }
        public int? ExpensesTypeId { get; set; }
        public decimal Value { get; set; }
        public string PaidTo { get; set; }
        public string GeneralDescription { get; set; }
        public string Remarks { get; set; }
        public string CreditAccountCode { get; set; }
        public IList<FinancialExpenseLine> Lines { get; set; }
    }

    public class FinancialExpenseLine
    {
        public string AccountCode { get; set; }
        public string AccountName { get; set; }
        public decimal Value { get; set; }
        public string Description { get; set; }
        public decimal Vat { get; set; }
        public decimal VatPercent { get; set; }
        public int? BranchId { get; set; }
        public string BillNo { get; set; }
    }

    public class FinancialExpenseSaveRequest : FinancialExpenseDetails
    {
    }

    public class FinancialExpenseSaveResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public int? Id { get; set; }
        public int? DetailNoteId { get; set; }
        public string JournalSerial { get; set; }
        public string VoucherSerial { get; set; }
    }
}
