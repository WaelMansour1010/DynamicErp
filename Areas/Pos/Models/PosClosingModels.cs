using System;
using System.Collections.Generic;

namespace MyERP.Areas.Pos.Models
{
    public class PosClosingValuesRequest
    {
        public DateTime? ClosingDate { get; set; }
        public int? BranchId { get; set; }
    }

    public class PosClosingExecuteRequest : PosClosingValuesRequest
    {
        public string Password { get; set; }
        public decimal? ActualValue { get; set; }
    }

    public class PosClosingValuesDto
    {
        public DateTime ClosingDate { get; set; }
        public int BranchId { get; set; }
        public string BranchName { get; set; }
        public int UserId { get; set; }
        public decimal OpenBalance { get; set; }
        public decimal TotalSaleDay { get; set; }
        public decimal LastBalance { get; set; }
        public decimal CountTransaction { get; set; }
        public decimal CountCards { get; set; }
        public decimal TotalRechargeValue { get; set; }
        public decimal TotalVat { get; set; }
        public decimal TotalRev { get; set; }
        public decimal TotalRevVat { get; set; }
        public decimal TotalRev2 { get; set; }
        public decimal TotalSaleDay2 { get; set; }
        public decimal TotalSaleDay2Vat { get; set; }
        public decimal CashOut { get; set; }
        public decimal CashOutTotal { get; set; }
        public decimal CashOutDisc { get; set; }
        public decimal BoxBalance { get; set; }
        public string BoxBalanceAccountSerial { get; set; }
        public string BoxBalanceAccountCode { get; set; }
        public string BoxBalanceSqlSummary { get; set; }
        public decimal TotalWallet { get; set; }
        public decimal TotalSupplyWallet { get; set; }
        public decimal TotalSupply { get; set; }
        public decimal TotalReturn { get; set; }
        public decimal Net { get; set; }
        public decimal ActValue { get; set; }
        public decimal Diff { get; set; }
        public decimal CountTransactionPOS { get; set; }
        public decimal NetPOS { get; set; }
        public decimal TotalRevPOS { get; set; }
        public decimal TotalPOS { get; set; }
        public decimal BankBalanceCharge { get; set; }
        public int? NoteId { get; set; }
        public string NoteSerial { get; set; }
        public bool AlreadyClosed { get; set; }
        public IList<PosClosingRechargeRowDto> RechargeRows { get; set; }
        public IList<PosClosingVoucherDto> Vouchers { get; set; }
        public IList<PosClosingVoucherLineDto> VoucherLines { get; set; }

        public PosClosingValuesDto()
        {
            RechargeRows = new List<PosClosingRechargeRowDto>();
            Vouchers = new List<PosClosingVoucherDto>();
            VoucherLines = new List<PosClosingVoucherLineDto>();
        }
    }

    public class PosClosingRechargeRowDto
    {
        public int? ItemDit { get; set; }
        public string NameShow { get; set; }
        public string Account_Code { get; set; }
        public string Account_Code2 { get; set; }
        public string Account_Code3 { get; set; }
        public string Account_Code4 { get; set; }
        public string BankAccountCode { get; set; }
        public decimal RechargeValue { get; set; }
        public decimal Vat { get; set; }
        public decimal Comm1 { get; set; }
        public decimal Comm2 { get; set; }
        public decimal Comm3 { get; set; }
    }

    public class PosClosingExecuteResult
    {
        public bool Success { get; set; }
        public int NoteId { get; set; }
        public string NoteSerial { get; set; }
        public long NoteSerial1 { get; set; }
        public decimal NoteValue { get; set; }
        public int LinesCreated { get; set; }
        public string Message { get; set; }
        public IList<PosClosingVoucherDto> Vouchers { get; set; }
        public IList<PosClosingVoucherLineDto> VoucherLines { get; set; }

        public PosClosingExecuteResult()
        {
            Vouchers = new List<PosClosingVoucherDto>();
            VoucherLines = new List<PosClosingVoucherLineDto>();
        }
    }

    public class PosClosingVoucherDto
    {
        public string VoucherType { get; set; }
        public int NoteId { get; set; }
        public string NoteSerial { get; set; }
        public DateTime NoteDate { get; set; }
        public decimal NoteValue { get; set; }
    }

    public class PosClosingVoucherLineDto
    {
        public string NoteSerial { get; set; }
        public DateTime? RecordDate { get; set; }
        public string VoucherType { get; set; }
        public string AccountSerial { get; set; }
        public string AccountCode { get; set; }
        public string AccountName { get; set; }
        public string Description { get; set; }
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
    }
}
