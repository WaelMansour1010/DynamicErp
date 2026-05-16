using System;
using System.Collections.Generic;

namespace MyERP.Areas.Pos.Models
{
    public class PosTokenInvoiceLookupUploadResult
    {
        public IList<PosTokenUploadItem> Tokens { get; set; }
        public PosTokenInvoiceLookupSummary Summary { get; set; }

        public PosTokenInvoiceLookupUploadResult()
        {
            Tokens = new List<PosTokenUploadItem>();
            Summary = new PosTokenInvoiceLookupSummary();
        }
    }

    public class PosTokenUploadItem
    {
        public string Token { get; set; }
        public int FirstRowNumber { get; set; }
        public int UploadedCount { get; set; }
    }

    public class PosTokenInvoiceLookupSummary
    {
        public int UploadedTokensCount { get; set; }
        public int UniqueTokensCount { get; set; }
        public int FoundCount { get; set; }
        public int NotFoundCount { get; set; }
        public int DuplicatedInUploadedFileCount { get; set; }
        public int DuplicatedInDatabaseCount { get; set; }
    }

    public class PosTokenInvoiceLookupRow
    {
        public string Token { get; set; }
        public string SearchStatus { get; set; }
        public int UploadedDuplicateCount { get; set; }
        public int DatabaseMatchCount { get; set; }
        public int? Transaction_ID { get; set; }
        public DateTime? InvoiceDate { get; set; }
        public string InvoiceNumber { get; set; }
        public string CustomerName { get; set; }
        public string CustomerPhone { get; set; }
        public string IdIpn { get; set; }
        public string ManualNo { get; set; }
        public string NationalId { get; set; }
        public string Branch { get; set; }
        public string Store { get; set; }
        public string Cashier { get; set; }
        public string ServiceType { get; set; }
        public string Notes { get; set; }
        public int? TransactionType { get; set; }
    }

    public class PosTokenInvoiceLookupResult
    {
        public IList<PosTokenInvoiceLookupRow> Rows { get; set; }
        public PosTokenInvoiceLookupSummary Summary { get; set; }

        public PosTokenInvoiceLookupResult()
        {
            Rows = new List<PosTokenInvoiceLookupRow>();
            Summary = new PosTokenInvoiceLookupSummary();
        }
    }

    public class PosTokenLifeStoryResult
    {
        public string Token { get; set; }
        public string CurrentStatus { get; set; }
        public string SummaryText { get; set; }
        public bool IsRestrictedByBranch { get; set; }
        public IList<PosTokenLifeCustomerRow> Customers { get; set; }
        public IList<PosTokenLifeStockRow> CurrentStock { get; set; }
        public IList<PosTokenLifeMovementRow> Movements { get; set; }
        public IList<PosTokenLifeMovementRow> SalesReferences { get; set; }

        public PosTokenLifeStoryResult()
        {
            Customers = new List<PosTokenLifeCustomerRow>();
            CurrentStock = new List<PosTokenLifeStockRow>();
            Movements = new List<PosTokenLifeMovementRow>();
            SalesReferences = new List<PosTokenLifeMovementRow>();
        }
    }

    public class PosTokenLifeCustomerRow
    {
        public int? CustomerId { get; set; }
        public string CardNo { get; set; }
        public string CardId { get; set; }
        public string CustomerName { get; set; }
        public string CustomerPhone { get; set; }
        public string NationalId { get; set; }
        public int? BranchId { get; set; }
        public string BranchName { get; set; }
        public DateTime? OrderDate { get; set; }
        public DateTime? SaveDate { get; set; }
        public string EasyCashType { get; set; }
    }

    public class PosTokenLifeStockRow
    {
        public int? StoreId { get; set; }
        public string StoreName { get; set; }
        public int? ItemId { get; set; }
        public string ItemName { get; set; }
        public decimal CurrentQty { get; set; }
        public int? LastTransactionId { get; set; }
        public DateTime? LastTransactionDate { get; set; }
    }

    public class PosTokenLifeMovementRow
    {
        public int? TransactionId { get; set; }
        public DateTime? TransactionDate { get; set; }
        public int? TransactionTypeId { get; set; }
        public string TransactionTypeName { get; set; }
        public string MovementKind { get; set; }
        public decimal StockEffect { get; set; }
        public decimal Quantity { get; set; }
        public decimal SignedQuantity { get; set; }
        public string InvoiceNumber { get; set; }
        public int? BranchId { get; set; }
        public string BranchName { get; set; }
        public int? StoreId { get; set; }
        public string StoreName { get; set; }
        public int? ItemId { get; set; }
        public string ItemName { get; set; }
        public string CustomerName { get; set; }
        public string CustomerPhone { get; set; }
        public int? UserId { get; set; }
        public string UserName { get; set; }
        public int? LinkedTransactionId { get; set; }
        public bool IsCancelled { get; set; }
        public string Notes { get; set; }
    }
}
