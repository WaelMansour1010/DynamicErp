using System;
using System.Collections.Generic;

namespace MyERP.Areas.Pos.Models
{
    public class PosSaveTransactionRequest
    {
        public int? Transaction_ID { get; set; }
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
        // Legacy POS mapping:
        // UI field "ID" binds here and persists to Transactions.IPN.
        public string IPN { get; set; }

        // Legacy POS mapping:
        // UI field "IPN" binds here and persists to Transactions.ManualNO.
        public string ManualNO { get; set; }
        public string NoID { get; set; }
        public string ManualNo2 { get; set; }
        public string VisaNumber { get; set; }
        public string CardSerial { get; set; }
        public decimal? RechargeValue { get; set; }
        public decimal? CommissionValue { get; set; }
        public decimal? VatValue { get; set; }
        public decimal? TotalFees { get; set; }
        public decimal BankMachineCommission { get; set; }
        public decimal CashOutMachineWithdrawalAmount { get; set; }
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
        public string EditPassword { get; set; }
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
        public int? UserType { get; set; }
        public string UserCategory { get; set; }
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
        public bool CanViewReports { get; set; }
        public bool CanViewAdminDashboard { get; set; }
        public bool CanViewJournalEntry { get; set; }
        public bool CanOpenClosing { get; set; }
        public bool CanExecuteClosing { get; set; }
        public bool CanOpenSales { get; set; }
        public bool CanEditInvoice { get; set; }
        public bool CanCancelOrReturn { get; set; }
        public bool CanEditKyc { get; set; }
        public bool CustomerService { get; set; }
        public bool IsFullAccessCustomerService { get; set; }
        public bool CanPrintKycAcknowledgment { get; set; }
        public bool CanPrintKycCard { get; set; }
        public bool CanReportSalesmen { get; set; }
        public bool CanReportClosings { get; set; }
        public bool CanReportFinanceClosing { get; set; }
        public bool CanReportDiscounts { get; set; }
        public bool CanReportIndicators { get; set; }
        public bool CanReportDailyTransactions { get; set; }
        public bool CanReportSalesComplete { get; set; }
        public bool CanReportDailyTransactions2 { get; set; }
        public bool CanReportDailyTransactionsSectors { get; set; }
        public bool CanReportAllSales { get; set; }
        public bool CanReportSalesComplete2 { get; set; }
        public bool CanReportSalesCompleteGovernorates { get; set; }
        public bool CanReportSalesCompleteDepartments { get; set; }
        public bool CanReportSalesCompleteAnalytical { get; set; }
        public bool CanReportStoreSerials { get; set; }
        public bool CanTeller { get; set; }
        public bool CanOpenPayments { get; set; }
        public bool CanExecutePayments { get; set; }
        public bool CanEditPayments { get; set; }
        public bool CanViewAccountingReports { get; set; }
        public bool CanViewAccountStatement { get; set; }
        public bool CanViewTrialBalance { get; set; }
        public bool CanViewIncomeStatement { get; set; }
        public bool CanViewGeneralLedgerAssistant { get; set; }
        public bool CanCreateJournalEntry { get; set; }
        public bool CanEditJournalEntry { get; set; }
        public bool CanDeleteJournalEntry { get; set; }
        public bool IsFullAccess { get; set; }
        public bool CanChangeDefaults { get; set; }
        public bool CanManagePrintTemplates { get; set; }
        public bool CanImportExcel { get; set; }
        public bool CanCancelInvoice { get; set; }
        public PosSystemOptionsDto SystemOptions { get; set; }
    }

    public class PosJournalSearchRequest
    {
        public string VoucherNo { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string AccountCode { get; set; }
        public string AccountCodes { get; set; }
        public string Description { get; set; }
        public int? BranchId { get; set; }
    }

    public class PosAccountTreeDto
    {
        public string AccountCode { get; set; }
        public string AccountSerial { get; set; }
        public string AccountName { get; set; }
        public string ParentAccountCode { get; set; }
        public bool IsLastAccount { get; set; }
        public bool HasChildren { get; set; }
        public IList<PosAccountTreeDto> Children { get; set; }

        public PosAccountTreeDto()
        {
            Children = new List<PosAccountTreeDto>();
        }
    }

    public class PosManualJournalSaveRequest
    {
        public int? NoteId { get; set; }
        public DateTime NoteDate { get; set; }
        public int? BranchId { get; set; }
        public string Description { get; set; }
        public string AdminPassword { get; set; }
        public IList<PosManualJournalLineDto> Lines { get; set; }

        public PosManualJournalSaveRequest()
        {
            Lines = new List<PosManualJournalLineDto>();
        }
    }

    public class PosManualJournalLineDto
    {
        public string AccountCode { get; set; }
        public string AccountSerial { get; set; }
        public string AccountName { get; set; }
        public string Description { get; set; }
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
    }

    public class PosJournalHeaderDto
    {
        public int NoteId { get; set; }
        public string NoteSerial { get; set; }
        public string NoteSerial1 { get; set; }
        public DateTime? NoteDate { get; set; }
        public int? BranchId { get; set; }
        public string BranchName { get; set; }
        public string Description { get; set; }
        public bool IsManual { get; set; }
        public string EntryKind { get; set; }
        public int? NoteType { get; set; }
        public string NoteTypeName { get; set; }
        public int? AutoSourceId { get; set; }
        public string AutoSourceName { get; set; }
        public string AutoSourceUrl { get; set; }
        public decimal DebitTotal { get; set; }
        public decimal CreditTotal { get; set; }
        public IList<PosManualJournalLineDto> Lines { get; set; }

        public PosJournalHeaderDto()
        {
            Lines = new List<PosManualJournalLineDto>();
        }
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
        public decimal BankMachineCommission { get; set; }
        public decimal CashOutMachineWithdrawalAmount { get; set; }
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

    public class PosKycCardAvailabilityDto
    {
        public bool Success { get; set; }
        public bool Available { get; set; }
        public string Status { get; set; }
        public string Message { get; set; }
        public string CardType { get; set; }
        public int TokenLength { get; set; }
        public int? ExistingCustomerId { get; set; }
        public string ExistingBranchName { get; set; }
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
        public bool HaveSerial { get; set; }
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
        public string BranchCode { get; set; }
        public string BranchName { get; set; }
        public string BranchNameEnglish { get; set; }
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
        public DateTime? CreatedDate { get; set; }
    }

    public class PosKycBranchHintDto
    {
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
        public PosSellerRankDto SellerRank { get; set; }
        public PosTodayTargetAchievementDto TargetAchievement { get; set; }

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

    public class PosSellerRankDto
    {
        public int SellerId { get; set; }
        public decimal NetValue { get; set; }
        public int? RankNo { get; set; }
        public int ActiveSellersCount { get; set; }
        public string PercentileBucket { get; set; }
        public decimal AmountToNextRank { get; set; }
        public string Message { get; set; }
        public bool IsLeading { get; set; }
        public string RankIcon { get; set; }
        public string RankBadgeText { get; set; }
        public string RankCssClass { get; set; }
        public string MotivationMessage { get; set; }
        public decimal ProgressPercent { get; set; }
    }

    public class PosTodayTargetAchievementDto
    {
        public bool IsConfigured { get; set; }
        public decimal DailyRechargeTarget { get; set; }
        public decimal DailyCardTarget { get; set; }
        public decimal RechargeAchievement { get; set; }
        public decimal CardAchievement { get; set; }
        public decimal RechargeAchievementPercent { get; set; }
        public decimal CardAchievementPercent { get; set; }
        public decimal OverallAchievementPercent { get; set; }
        public string PerformanceClass { get; set; }
        public string StatusText { get; set; }
        public string Message { get; set; }
    }

    public class PosTodayInvoiceDto
    {
        public int Transaction_ID { get; set; }
        public string NoteSerial1 { get; set; }
        public string TransactionDate { get; set; }
        public string TransactionTime { get; set; }
        public string CustomerName { get; set; }
        public string CustomerPhone { get; set; }
        public decimal PayedValue { get; set; }
        public decimal NetValue { get; set; }
        public string ServiceType { get; set; }
        public bool IsExcelImported { get; set; }
        public long? ExcelImportBatchId { get; set; }
        public bool IsCancelled { get; set; }
    }

    public class PosCancelInvoiceRequest
    {
        public int TransactionId { get; set; }
        public string Password { get; set; }
        public string CancelReason { get; set; }
    }

    public class PosDeleteInvoiceRequest
    {
        public int TransactionId { get; set; }
        public string AdminPassword { get; set; }
    }

    public class PosDeleteExcelInvoicesRequest
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int? BranchId { get; set; }
        public string OperationType { get; set; }
        public string AdminPassword { get; set; }
    }

    public class PosDeleteInvoiceResult
    {
        public int DeletedCount { get; set; }
        public int SkippedCount { get; set; }
        public IList<string> Messages { get; set; }

        public PosDeleteInvoiceResult()
        {
            Messages = new List<string>();
        }
    }

    public class PosInvoiceReviewDto
    {
        public int Transaction_ID { get; set; }
        public string NoteSerial1 { get; set; }
        public DateTime? TransactionDate { get; set; }
        public string TransactionType { get; set; }
        public string IPN { get; set; }
        public string ManualNO { get; set; }
        public string NoID { get; set; }
        public string CashCustomerPhone { get; set; }
        public string CashCustomerName { get; set; }
        public string Phone2 { get; set; }
        public string VisaNumber { get; set; }
        public int? CreatedUserId { get; set; }
        public string CreatedUserName { get; set; }
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
        public decimal BankMachineCommission { get; set; }
        public decimal CashOutMachineWithdrawalAmount { get; set; }
        public int? ItemIDService { get; set; }
        public int? ItemIDService2 { get; set; }
        public string ItemIDServiceName { get; set; }
        public string ItemIDService2Name { get; set; }
        public decimal? ViolationsValue { get; set; }
        public string Tet_NumPoket { get; set; }
        public bool IsCancelled { get; set; }
        public int? CancelledBy { get; set; }
        public DateTime? CancelledDate { get; set; }
        public string CancelReason { get; set; }
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

    public class PosDashboardSummaryDto
    {
        public PosDashboardKpiDto Kpis { get; set; }
        public PosDashboardKpiDto PreviousKpis { get; set; }
        public IList<PosDashboardKpiComparisonDto> KpiComparisons { get; set; }
        public IList<PosDashboardBranchDto> BranchComparison { get; set; }
        public IList<PosDashboardBranchDto> WorstBranches { get; set; }
        public IList<PosDashboardServiceDto> TopServices { get; set; }
        public IList<PosDashboardSellerDto> TopSellers { get; set; }
        public IList<PosDashboardOperationDto> OperationTypeSummary { get; set; }
        public IList<PosDashboardTrendDto> DailyTrend { get; set; }
        public IList<string> Insights { get; set; }
        public IList<string> Recommendations { get; set; }
        public IList<PosDashboardSmartInsightDto> SmartInsights { get; set; }
        public string SnapshotStatus { get; set; }
        public DateTime? SnapshotGeneratedAt { get; set; }
        public string SnapshotMessage { get; set; }
        public bool IsSnapshotData { get; set; }

        public PosDashboardSummaryDto()
        {
            Kpis = new PosDashboardKpiDto();
            PreviousKpis = new PosDashboardKpiDto();
            KpiComparisons = new List<PosDashboardKpiComparisonDto>();
            BranchComparison = new List<PosDashboardBranchDto>();
            WorstBranches = new List<PosDashboardBranchDto>();
            TopServices = new List<PosDashboardServiceDto>();
            TopSellers = new List<PosDashboardSellerDto>();
            OperationTypeSummary = new List<PosDashboardOperationDto>();
            DailyTrend = new List<PosDashboardTrendDto>();
            Insights = new List<string>();
            Recommendations = new List<string>();
            SmartInsights = new List<PosDashboardSmartInsightDto>();
        }
    }

    public class PosDashboardSmartInsightDto
    {
        public string Type { get; set; }
        public string Severity { get; set; }
        public string Icon { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Action { get; set; }
        public string Metric { get; set; }
    }

    public class PosDashboardKpiDto
    {
        public int TransactionCount { get; set; }
        public decimal SalesTotal { get; set; }
        public decimal FeesTotal { get; set; }
        public decimal VatTotal { get; set; }
        public decimal NetCollection { get; set; }
        public int ActivatedKycCards { get; set; }
        public int CancelledOrReturnedCount { get; set; }
    }

    public class PosDashboardKpiComparisonDto
    {
        public string Key { get; set; }
        public string Title { get; set; }
        public decimal CurrentValue { get; set; }
        public decimal PreviousValue { get; set; }
        public decimal ChangePercent { get; set; }
        public bool IsPositive { get; set; }
        public string Format { get; set; }
    }

    public class PosDashboardBranchDto
    {
        public int? BranchId { get; set; }
        public string BranchName { get; set; }
        public int TransactionCount { get; set; }
        public decimal TotalValue { get; set; }
        public decimal FeesTotal { get; set; }
    }

    public class PosDashboardServiceDto
    {
        public int? ItemId { get; set; }
        public string ItemName { get; set; }
        public int SaleCount { get; set; }
        public decimal TotalValue { get; set; }
        public decimal FeesTotal { get; set; }
    }

    public class PosDashboardSellerDto
    {
        public int RankNo { get; set; }
        public string RankIcon { get; set; }
        public int? SellerId { get; set; }
        public string SellerName { get; set; }
        public int TransactionCount { get; set; }
        public decimal NetValue { get; set; }
    }

    public class PosDashboardOperationDto
    {
        public string OperationType { get; set; }
        public int TransactionCount { get; set; }
        public decimal RechargeTotal { get; set; }
        public decimal FeesTotal { get; set; }
        public decimal VatTotal { get; set; }
        public decimal NetCollection { get; set; }
    }

    public class PosDashboardTrendDto
    {
        public string Day { get; set; }
        public int TransactionCount { get; set; }
        public decimal NetCollection { get; set; }
    }

    public class PosJournalEntryDto
    {
        public string NoteSerial { get; set; }
        public DateTime? RecordDate { get; set; }
        public string RecordDateText { get; set; }
        public string AccountSerial { get; set; }
        public string AccountCode { get; set; }
        public string AccountName { get; set; }
        public string Description { get; set; }
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
    }

    public class PosPermissionUserDto
    {
        public int UserId { get; set; }
        public string UserName { get; set; }
        public string EmpName { get; set; }
        public string UserCategory { get; set; }
    }

    public class PosPermissionItemDto
    {
        public string Key { get; set; }
        public string Title { get; set; }
        public bool IsAllowed { get; set; }
    }

    public class PosPermissionSaveRequest
    {
        public int UserId { get; set; }
        public IList<PosPermissionItemDto> Permissions { get; set; }

        public PosPermissionSaveRequest()
        {
            Permissions = new List<PosPermissionItemDto>();
        }
    }

    public class PosUserCategorySaveRequest
    {
        public int UserId { get; set; }
        public string UserCategory { get; set; }
    }

    public class PosBulkPermissionRequest
    {
        public string UserCategory { get; set; }
        public string PermissionKey { get; set; }
        public bool IsAllowed { get; set; }
    }

    public class PosLookupDto
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Extra { get; set; }
    }

    public class PosPaymentRequestDto
    {
        public int? NoteId { get; set; }
        public int BranchId { get; set; }
        public DateTime PaymentDate { get; set; }
        public int CashingType { get; set; }
        public string NameAccountCode { get; set; }
        public string NameText { get; set; }
        public int PaymentMethod { get; set; }
        public decimal Value { get; set; }
        public int? BoxId { get; set; }
        public int? BankId { get; set; }
        public string ReferenceNo { get; set; }
        public DateTime? ReferenceDate { get; set; }
        public int? EmpId { get; set; }
        public string EmpAccountCode { get; set; }
        public decimal BoxValue { get; set; }
        public decimal EmpValue { get; set; }
        public string Remarks { get; set; }
        public string GeneralDescription { get; set; }
    }

    public class PosPaymentLineDto
    {
        public string AccountCode { get; set; }
        public string AccountName { get; set; }
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public string Description { get; set; }
    }

    public class PosPaymentResultDto
    {
        public int NoteId { get; set; }
        public string NoteSerial { get; set; }
        public string NoteSerial1 { get; set; }
        public IList<PosPaymentLineDto> Lines { get; set; }

        public PosPaymentResultDto()
        {
            Lines = new List<PosPaymentLineDto>();
        }
    }

    public class PosPurchaseInvoiceRequestDto
    {
        public string InvoiceNumber { get; set; }
        public DateTime InvoiceDate { get; set; }
        public int SupplierId { get; set; }
        public int BranchId { get; set; }
        public int StoreId { get; set; }
        public int PaymentType { get; set; }
        public int? BoxId { get; set; }
        public int? BankId { get; set; }
        public decimal DiscountValue { get; set; }
        public decimal VatValue { get; set; }
        public string ManualNo { get; set; }
        public string Remarks { get; set; }
        public IList<PosPurchaseInvoiceLineDto> Items { get; set; }

        public PosPurchaseInvoiceRequestDto()
        {
            Items = new List<PosPurchaseInvoiceLineDto>();
        }
    }

    public class PosPurchaseInvoiceSearchRequestDto
    {
        public string InvoiceNumber { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string SupplierTerm { get; set; }
        public int? BranchId { get; set; }
        public int? StoreId { get; set; }
        public int? PaymentType { get; set; }
    }

    public class PosPurchaseInvoiceIndexRowDto
    {
        public int TransactionId { get; set; }
        public string InvoiceNumber { get; set; }
        public DateTime? InvoiceDate { get; set; }
        public string SupplierName { get; set; }
        public string BranchName { get; set; }
        public string StoreName { get; set; }
        public decimal NetTotal { get; set; }
        public string Status { get; set; }
    }

    public class PosPurchaseInvoiceDetailDto : PosPurchaseInvoiceRequestDto
    {
        public int TransactionId { get; set; }
        public string SupplierName { get; set; }
    }

    public class PosPurchaseInvoiceLineDto
    {
        public int ItemId { get; set; }
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public int? UnitId { get; set; }
        public string UnitName { get; set; }
        public decimal Quantity { get; set; }
        public decimal PurchasePrice { get; set; }
        public decimal DiscountValue { get; set; }
        public decimal VatValue { get; set; }
        public decimal VatPercent { get; set; }
        public decimal LineTotal { get; set; }
        public bool HaveSerial { get; set; }
        public string ItemSerial { get; set; }
    }

    public class PosPurchaseInvoiceResultDto
    {
        public int TransactionId { get; set; }
        public string InvoiceNumber { get; set; }
        public int NoteId { get; set; }
        public int? ReceiveTransactionId { get; set; }
        public string ReceiveVoucherNumber { get; set; }
    }

    public class PosPurchaseItemUnitDto
    {
        public int UnitId { get; set; }
        public string UnitName { get; set; }
        public decimal UnitFactor { get; set; }
        public decimal PurchasePrice { get; set; }
        public bool IsDefault { get; set; }
    }

    public class PosPurchaseImportResultDto
    {
        public IList<PosPurchaseInvoiceLineDto> Accepted { get; set; }
        public IList<PosPurchaseImportRejectedRowDto> Rejected { get; set; }

        public PosPurchaseImportResultDto()
        {
            Accepted = new List<PosPurchaseInvoiceLineDto>();
            Rejected = new List<PosPurchaseImportRejectedRowDto>();
        }
    }

    public class PosPurchaseImportRejectedRowDto
    {
        public int RowNumber { get; set; }
        public string ItemText { get; set; }
        public string Serial { get; set; }
        public string Reason { get; set; }
    }

    public class PosStockTransferRequestDto
    {
        public string VoucherNumber { get; set; }
        public DateTime TransferDate { get; set; }
        public int BranchId { get; set; }
        public int SourceStoreId { get; set; }
        public int DestinationStoreId { get; set; }
        public string Remarks { get; set; }
        public IList<PosStockTransferLineDto> Items { get; set; }

        public PosStockTransferRequestDto()
        {
            Items = new List<PosStockTransferLineDto>();
        }
    }

    public class PosStockTransferSearchRequestDto
    {
        public string VoucherNumber { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int? BranchId { get; set; }
        public int? SourceStoreId { get; set; }
        public int? DestinationStoreId { get; set; }
        public string ItemOrSerialTerm { get; set; }
    }

    public class PosStockTransferSerialSearchRequestDto
    {
        public int BranchId { get; set; }
        public int SourceStoreId { get; set; }
        public int ItemId { get; set; }
        public DateTime TransferDate { get; set; }
        public string SerialFrom { get; set; }
        public string SerialTo { get; set; }
        public string SerialTerm { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }

    public class PosStockTransferAvailableSerialDto : PosStockTransferLineDto
    {
        public decimal AvailableQty { get; set; }
        public int TotalRows { get; set; }
    }

    public class PosStockTransferIndexRowDto
    {
        public int SourceTransactionId { get; set; }
        public int DestinationTransactionId { get; set; }
        public string VoucherNumber { get; set; }
        public DateTime? TransferDate { get; set; }
        public string SourceStoreName { get; set; }
        public string DestinationStoreName { get; set; }
        public int ItemCount { get; set; }
        public decimal TotalQuantity { get; set; }
    }

    public class PosStockTransferDetailDto : PosStockTransferRequestDto
    {
        public int SourceTransactionId { get; set; }
        public int? DestinationTransactionId { get; set; }
    }

    public class PosStockTransferLineDto
    {
        public int ItemId { get; set; }
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public int? UnitId { get; set; }
        public string UnitName { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitFactor { get; set; }
        public decimal Price { get; set; }
        public bool HaveSerial { get; set; }
        public string Serial { get; set; }
    }

    public class PosStockTransferResultDto
    {
        public int SourceTransactionId { get; set; }
        public int DestinationTransactionId { get; set; }
        public int NoteId { get; set; }
        public string VoucherNumber { get; set; }
        public string NoteSerial { get; set; }
        public int DoubleEntryVouchersId { get; set; }
    }

    public class PosStockTransferImportRequestDto
    {
        public int BranchId { get; set; }
        public int SourceStoreId { get; set; }
        public DateTime TransferDate { get; set; }
        public IList<string> Serials { get; set; }

        public PosStockTransferImportRequestDto()
        {
            Serials = new List<string>();
        }
    }

    public class PosStockTransferImportResultDto
    {
        public IList<PosStockTransferLineDto> Accepted { get; set; }
        public IList<PosStockTransferRejectedRowDto> Rejected { get; set; }

        public PosStockTransferImportResultDto()
        {
            Accepted = new List<PosStockTransferLineDto>();
            Rejected = new List<PosStockTransferRejectedRowDto>();
        }
    }

    public class PosStockTransferRejectedRowDto
    {
        public int RowNumber { get; set; }
        public string Serial { get; set; }
        public string Reason { get; set; }
    }

    public class PosSupplierLookupDto
    {
        public int SupplierId { get; set; }
        public string SupplierName { get; set; }
        public string AccountCode { get; set; }
    }

    public class PosPaymentSearchRequestDto
    {
        public string SearchText { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int? BranchId { get; set; }
        public int? EmpId { get; set; }
    }

    public class PosPaymentMovementDto
    {
        public int NoteId { get; set; }
        public string NoteSerial { get; set; }
        public string NoteSerial1 { get; set; }
        public DateTime? NoteDate { get; set; }
        public int BranchId { get; set; }
        public string BranchName { get; set; }
        public int CashingType { get; set; }
        public string CashingTypeName { get; set; }
        public string NameAccountCode { get; set; }
        public string NameText { get; set; }
        public int PaymentMethod { get; set; }
        public decimal Value { get; set; }
        public int? BoxId { get; set; }
        public string BoxName { get; set; }
        public int? BankId { get; set; }
        public string BankName { get; set; }
        public string ReferenceNo { get; set; }
        public DateTime? ReferenceDate { get; set; }
        public int? EmpId { get; set; }
        public string EmpName { get; set; }
        public string EmpAccountCode { get; set; }
        public decimal BoxValue { get; set; }
        public decimal EmpValue { get; set; }
        public string Remarks { get; set; }
        public string GeneralDescription { get; set; }
        public int? CreatedUserId { get; set; }
        public string CreatedUserName { get; set; }
        public int? LastModifiedByUserId { get; set; }
        public string LastModifiedByUserName { get; set; }
        public DateTime? LastModifiedDate { get; set; }
        public IList<PosPaymentLineDto> Lines { get; set; }

        public PosPaymentMovementDto()
        {
            Lines = new List<PosPaymentLineDto>();
        }
    }
}
