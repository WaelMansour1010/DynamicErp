using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MyERP.Models.CustomModels
{
    public class ServiceInvoice
    {
        public int Id { get; set; }
        public string DocumentNumber { get; set; }
        public System.DateTime VoucherDate { get; set; }
        public int DepartmentId { get; set; }
        public decimal? Total { get; set; }
        public decimal? TotalItemsDiscount { get; set; }
        public decimal? SalesTaxes { get; set; }
        public decimal? TotalAfterTaxes { get; set; }
        public decimal? VoucherDiscountValue { get; set; }
        public float? VoucherDiscountPercentage { get; set; }
        public decimal? NetTotal { get; set; }
        public decimal? Paid { get; set; }
        public decimal? TotalCostPrice { get; set; }
        public decimal? TotalItemDirectExpenses { get; set; }
        public int? UserId { get; set; }
        public bool IsDeleted { get; set; }
        public string Notes { get; set; }
        public string Image { get; set; }
        public decimal? CommercialRevenueTaxAmount { get; set; }
        public IEnumerable<ServiceInvoiceDetail> serviceInvoiceDetails { get; set; }
    }
    public class ServiceInvoiceDetail
    {
        public int Id { get; set; }
        public int MainDocId { get; set; }
        public int? ItemId { get; set; }
        public string ItemName { get; set; }
        public float? Qty { get; set; }
        public int? ItemPriceId { get; set; }
        public decimal? ItemPrice { get; set; }
        public int? ItemUnitId { get; set; }
        public float? UnitEquivalent { get; set; }
        public decimal? Price { get; set; }
        public decimal? CostPrice { get; set; }
        public decimal? DiscountValue { get; set; }
        public float? DiscountPerc { get; set; }
        public decimal? ItemDirectExpenses { get; set; }
        public bool IsDeleted { get; set; }
        public string Notes { get; set; }
        public ServiceInvoice serviceInvoice { get; set; }
    }
}