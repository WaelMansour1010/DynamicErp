using System;
using System.Collections.Generic;

namespace MyERP.Areas.MainErp.ViewModels.Reports
{
    public class JournalEntriesReportViewModel
    {
        public JournalEntriesReportViewModel()
        {
            Rows = new List<JournalEntriesReportRowViewModel>();
        }

        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int? BranchId { get; set; }
        public string AccountCode { get; set; }
        public int? NoteType { get; set; }
        public string Warning { get; set; }
        public decimal TotalDebit { get; set; }
        public decimal TotalCredit { get; set; }
        public IList<JournalEntriesReportRowViewModel> Rows { get; private set; }
    }

    public class JournalEntriesReportRowViewModel
    {
        public DateTime? NoteDate { get; set; }
        public string NoteSerial { get; set; }
        public int? NoteType { get; set; }
        public string AccountCode { get; set; }
        public string AccountName { get; set; }
        public string AccountSerial { get; set; }
        public string AccountDisplay { get; set; }
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public string Description { get; set; }
        public string BranchName { get; set; }
        public string UserName { get; set; }
    }

    public class AccountMovementReportViewModel
    {
        public AccountMovementReportViewModel()
        {
            Rows = new List<AccountMovementReportRowViewModel>();
        }

        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string AccountCode { get; set; }
        public int? BranchId { get; set; }
        public string Warning { get; set; }
        public string AccountName { get; set; }
        public string AccountSerial { get; set; }
        public string AccountDisplay { get; set; }
        public decimal TotalDebit { get; set; }
        public decimal TotalCredit { get; set; }
        public decimal PeriodMovement { get { return TotalDebit - TotalCredit; } }
        public IList<AccountMovementReportRowViewModel> Rows { get; private set; }
    }

    public class AccountMovementReportRowViewModel
    {
        public DateTime? NoteDate { get; set; }
        public string NoteSerial { get; set; }
        public string Description { get; set; }
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public decimal RunningMovement { get; set; }
    }

    public class SalesSummaryReportViewModel
    {
        public SalesSummaryReportViewModel()
        {
            Rows = new List<SalesSummaryReportRowViewModel>();
        }

        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int? BranchId { get; set; }
        public int? UserId { get; set; }
        public int? CustomerId { get; set; }
        public string Warning { get; set; }
        public int InvoiceCount { get; set; }
        public decimal TotalBeforeVat { get; set; }
        public decimal TotalVat { get; set; }
        public decimal TotalAmount { get; set; }
        public IList<SalesSummaryReportRowViewModel> Rows { get; private set; }
    }

    public class SalesSummaryReportRowViewModel
    {
        public DateTime? Date { get; set; }
        public string BranchName { get; set; }
        public string UserName { get; set; }
        public int InvoiceCount { get; set; }
        public decimal TotalBeforeVat { get; set; }
        public decimal Vat { get; set; }
        public decimal Total { get; set; }
    }
}
