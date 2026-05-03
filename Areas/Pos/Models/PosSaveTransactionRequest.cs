using System;
using System.Collections.Generic;

namespace MyERP.Areas.Pos.Models
{
    public class PosSaveTransactionRequest
    {
        public string TransactionType { get; set; }
        public string TransactionDate { get; set; }
        public int? BranchId { get; set; }
        public int? StoreID { get; set; }
        public int? UserID { get; set; }
        public int? Emp_ID { get; set; }
        public int? CustomerID { get; set; }
        public int? TblCusCshId { get; set; }
        public int? DefaultCustomerId { get; set; }
        public int PaymentType { get; set; }
        public int? BoxID { get; set; }
        public decimal PayedValue { get; set; }
        public decimal NetValue { get; set; }
        public decimal RemainValue { get; set; }
        public int? PaymentNetid { get; set; }
        public bool IsCashOut { get; set; }
        public bool IsPOS { get; set; }
        public bool OtherItems { get; set; }
        public int? PayType { get; set; }
        public int? POSBillType { get; set; }
        public int? STableID { get; set; }
        public int? SessionD { get; set; }
        public int? BillBasedOn { get; set; }
        public string CashCustomerName { get; set; }
        public string CashCustomerPhone { get; set; }
        public string Phone2 { get; set; }
        public string IPN { get; set; }
        public string ManualNO { get; set; }
        public string NoID { get; set; }
        public string ManualNo2 { get; set; }
        public string VisaNumber { get; set; }
        public string CardSerial { get; set; }
        public decimal? RechargeValue { get; set; }
        public decimal? CommissionValue { get; set; }
        public decimal? VatValue { get; set; }
        public decimal? TotalFees { get; set; }
        public string RechargeType { get; set; }
        public bool TrafficViolations { get; set; }
        public decimal? ViolationsValue { get; set; }
        public int? ItemIDService { get; set; }
        public int? ItemIDService2 { get; set; }
        public int? ViolationPayType { get; set; }
        public string Tet_NumPoket { get; set; }
        public bool IsRecharg { get; set; }
        public bool IsWallet { get; set; }
        public bool HaveGuarantee { get; set; }
        public List<PosTransactionItemDto> Items { get; set; }
        public List<PosSalesPaymentDto> SalesPayments { get; set; }

        public PosSaveTransactionRequest()
        {
            Items = new List<PosTransactionItemDto>();
            SalesPayments = new List<PosSalesPaymentDto>();
        }
    }

    public class PosLoginRequest
    {
        public string UserName { get; set; }
        public string Password { get; set; }
    }

    [Serializable]
    public class PosUserContext
    {
        public int UserId { get; set; }
        public string UserName { get; set; }
        public int? EmpId { get; set; }
        public string EmpName { get; set; }
        public int? BranchId { get; set; }
        public string BranchName { get; set; }
        public int? StoreId { get; set; }
        public string StoreName { get; set; }
        public int? BoxId { get; set; }
        public string BoxName { get; set; }
        public int? PaymentNetId { get; set; }
        public int? PaymentTypeId { get; set; }
        public string PaymentName { get; set; }
        public int? BankId { get; set; }
        public string BankName { get; set; }
        public bool CanSave { get; set; }
        public bool CanPrint { get; set; }
        public bool CanReturn { get; set; }
        public bool CanOpenCashCustomer { get; set; }
        public bool IsFullAccess { get; set; }
        public bool CanChangeDefaults { get; set; }
        public PosSystemOptionsDto SystemOptions { get; set; }
    }

    [Serializable]
    public class PosSystemOptionsDto
    {
        public bool CashCustomerNameMustEnter { get; set; }
        public bool TradingPOS { get; set; }
        public bool PosShape2 { get; set; }
        public decimal PercentVisa { get; set; }
        public decimal MinVisa { get; set; }
        public decimal MaxVisa { get; set; }
        public decimal PercentVisaPur { get; set; }
        public decimal MinVisaPur { get; set; }
        public decimal MaxVisaPur { get; set; }
        public string Todo { get; set; }
    }

    public class PosCommissionRequest
    {
        public string ServiceType { get; set; }
        public int? ItemID { get; set; }
        public int? BranchId { get; set; }
        public decimal RechargeValue { get; set; }
        public decimal? Vatyo { get; set; }
        public bool IsWallet { get; set; }
        public bool HaveGuarantee { get; set; }
    }

    public class PosCommissionResult
    {
        public int? ItemID { get; set; }
        public decimal RechargeValue { get; set; }
        public decimal CommissionValue { get; set; }
        public decimal ServiceFee { get; set; }
        public decimal VatValue { get; set; }
        public decimal VatPercent { get; set; }
        public decimal TotalFees { get; set; }
        public decimal TotalValue { get; set; }
        public string Source { get; set; }
        public decimal Percent { get; set; }
        public decimal Min { get; set; }
        public decimal Max { get; set; }
    }

    public class PosItemCommissionDto
    {
        public int ItemID { get; set; }
        public decimal? PercentVisa { get; set; }
        public decimal? MinVisa { get; set; }
        public decimal? MaxVisa { get; set; }
        public decimal? PercentVisaPur { get; set; }
        public decimal? MinVisaPur { get; set; }
        public decimal? MaxVisaPur { get; set; }
    }

    public class PosStoreDto
    {
        public int StoreID { get; set; }
        public string StoreName { get; set; }
        public int? BranchId { get; set; }
    }

    public class PosCashCustomerSaveRequest
    {
        public int? CustomerID { get; set; }
        public string Name { get; set; }
        public string NameE { get; set; }
        public string ArabicName0 { get; set; }
        public string ArabicName1 { get; set; }
        public string ArabicName2 { get; set; }
        public string ArabicName3 { get; set; }
        public string EnglishName0 { get; set; }
        public string EnglishName1 { get; set; }
        public string EnglishName2 { get; set; }
        public string EnglishName3 { get; set; }
        public string EnglishName5 { get; set; }
        public string EnglishName6 { get; set; }
        public string EnglishName7 { get; set; }
        public string PhoneNo2 { get; set; }
        public string PhoneNo { get; set; }
        public string CardNo { get; set; }
        public string CardId { get; set; }
        public string CardSource { get; set; }
        public string Tet_NumPoket { get; set; }
        public string Address { get; set; }
        public string MailAdress { get; set; }
        public int? Nationality { get; set; }
        public DateTime? BirthDate { get; set; }
        public DateTime? CardDate { get; set; }
        public DateTime? CardEndDate { get; set; }
        public DateTime? OrderDate { get; set; }
        public int? EasyCashType { get; set; }
        public int? EmpId { get; set; }
        public string Tel { get; set; }
        public string Card { get; set; }
        public int? BranchId { get; set; }
        public int? UserId { get; set; }
    }

    public class PosTransactionItemDto
    {
        public int? Item_ID { get; set; }
        public string ItemName { get; set; }
        public int? UnitId { get; set; }
        public decimal Quantity { get; set; }
        public decimal? ShowQty { get; set; }
        public decimal? QtyBySmalltUnit { get; set; }
        public decimal Price { get; set; }
        public decimal? ShowPrice { get; set; }
        public decimal TotalPrice { get; set; }
        public decimal? Vat { get; set; }
        public decimal? Vatyo { get; set; }
        public decimal? DiscountValue { get; set; }
        public decimal? TotalDiscountPerLine { get; set; }
        public int? StoreID2 { get; set; }
        public int? ItemCase { get; set; }
        public decimal? CostPrice { get; set; }
        public int? SavedItemType { get; set; }
    }

    public class PosSalesPaymentDto
    {
        public int? PaymentID { get; set; }
        public string PaymentName { get; set; }
        public decimal Value { get; set; }
        public string CardNo { get; set; }
        public decimal? MaxValue { get; set; }
    }

    public class PosItemLookupDto
    {
        public int Item_ID { get; set; }
        public string ItemName { get; set; }
        public decimal Quantity { get; set; }
        public string ItemCode { get; set; }
        public int? UnitId { get; set; }
        public string UnitName { get; set; }
        public decimal Price { get; set; }
        public decimal ShowPrice { get; set; }
        public decimal TotalPrice { get; set; }
        public decimal QtyBySmalltUnit { get; set; }
        public decimal Vat { get; set; }
        public decimal Vatyo { get; set; }
        public decimal DiscountValue { get; set; }
        public decimal TotalDiscountPerLine { get; set; }
        public int? StoreID2 { get; set; }
        public int? BranchId { get; set; }
        public int ItemCase { get; set; }
        public decimal CostPrice { get; set; }
        public int SavedItemType { get; set; }
    }

    public class PosPaymentTypeDto
    {
        public int PaymentID { get; set; }
        public string PaymentName { get; set; }
        public int? BankId { get; set; }
        public string BankName { get; set; }
        public decimal? MaxValue { get; set; }
    }

    public class PosServiceOptionDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class PosCashBoxDto
    {
        public int BoxID { get; set; }
        public string BoxName { get; set; }
        public int? BranchId { get; set; }
        public bool IsWallet { get; set; }
        public bool IsTerminalPOS { get; set; }
    }

    public class PosBranchDto
    {
        public int BranchId { get; set; }
        public string BranchName { get; set; }
    }

    public class PosCustomerLookupDto
    {
        public int CustomerID { get; set; }
        public string Name { get; set; }
        public string NameE { get; set; }
        public string ArabicName0 { get; set; }
        public string ArabicName1 { get; set; }
        public string ArabicName2 { get; set; }
        public string ArabicName3 { get; set; }
        public string EnglishName0 { get; set; }
        public string EnglishName1 { get; set; }
        public string EnglishName2 { get; set; }
        public string EnglishName3 { get; set; }
        public string EnglishName5 { get; set; }
        public string EnglishName6 { get; set; }
        public string EnglishName7 { get; set; }
        public string CustomerName { get; set; }
        public string Phone { get; set; }
        public string Phone2 { get; set; }
        public string IPN { get; set; }
        public string VisaNumber { get; set; }
        public string CardSerial { get; set; }
        public string CardNo { get; set; }
        public string CardId { get; set; }
        public string CardSource { get; set; }
        public string Tel { get; set; }
        public string Tet_NumPoket { get; set; }
        public string Address { get; set; }
        public string MailAdress { get; set; }
        public int? Nationality { get; set; }
        public DateTime? BirthDate { get; set; }
        public DateTime? CardDate { get; set; }
        public DateTime? CardEndDate { get; set; }
        public DateTime? OrderDate { get; set; }
        public int? EasyCashType { get; set; }
        public int? BranchId { get; set; }
        public string BranchName { get; set; }
    }

    public class PosKycAttachmentDto
    {
        public int Id { get; set; }
        public string SubjectNo { get; set; }
        public string FileName { get; set; }
        public string ImageTitle { get; set; }
        public string Department { get; set; }
        public string OperationType { get; set; }
        public DateTime? ImageDate { get; set; }
    }

    public class PosSaveTransactionResult
    {
        public int Transaction_ID { get; set; }
        public string NoteSerial1 { get; set; }
    }

    public class PosEmployeeBalancesDto
    {
        public string EmployeeBalanceText { get; set; }
        public string BoxBalanceText { get; set; }
        public string EmployeeAccountCode { get; set; }
        public string BoxAccountCode { get; set; }
        public decimal EmployeeBalance { get; set; }
        public decimal BoxBalance { get; set; }
    }

    public class PosTodaySummaryDto
    {
        public string GeneratedAt { get; set; }
        public IList<PosTodaySummaryItemDto> Items { get; set; }

        public PosTodaySummaryDto()
        {
            Items = new List<PosTodaySummaryItemDto>();
        }
    }

    public class PosTodaySummaryItemDto
    {
        public string ServiceType { get; set; }
        public int Count { get; set; }
        public decimal NetValue { get; set; }
        public decimal PayedValue { get; set; }
    }

    public class PosTodayInvoiceDto
    {
        public int Transaction_ID { get; set; }
        public string NoteSerial1 { get; set; }
        public string TransactionTime { get; set; }
        public string CustomerName { get; set; }
        public string CustomerPhone { get; set; }
        public decimal PayedValue { get; set; }
        public decimal NetValue { get; set; }
        public string ServiceType { get; set; }
    }

    public class PosInvoiceReviewDto
    {
        public int Transaction_ID { get; set; }
        public string NoteSerial1 { get; set; }
        public DateTime? TransactionDate { get; set; }
        public string TransactionType { get; set; }
        public string IPN { get; set; }
        public string ManualNO { get; set; }
        public string CashCustomerPhone { get; set; }
        public string CashCustomerName { get; set; }
        public string Phone2 { get; set; }
        public string VisaNumber { get; set; }
        public int? BranchId { get; set; }
        public string BranchName { get; set; }
        public string BranchCode { get; set; }
        public string BranchAddress { get; set; }
        public string BranchPhone { get; set; }
        public int? StoreID { get; set; }
        public string StoreName { get; set; }
        public int? BoxID { get; set; }
        public string BoxName { get; set; }
        public int? Emp_ID { get; set; }
        public string EmpName { get; set; }
        public decimal RechargeValue { get; set; }
        public decimal PayedValue { get; set; }
        public decimal NetValue { get; set; }
        public decimal RemainValue { get; set; }
        public decimal VatValue { get; set; }
        public decimal TotalFees { get; set; }
        public int? ItemIDService { get; set; }
        public int? ItemIDService2 { get; set; }
        public decimal? ViolationsValue { get; set; }
        public string Tet_NumPoket { get; set; }
        public IList<PosTransactionItemDto> Items { get; set; }
        public PosCustomerLookupDto KycCustomer { get; set; }

        public PosInvoiceReviewDto()
        {
            Items = new List<PosTransactionItemDto>();
        }
    }

    public class PosReceiptDto
    {
        public string CompanyName { get; set; }
        public PosInvoiceReviewDto Invoice { get; set; }
    }
}
