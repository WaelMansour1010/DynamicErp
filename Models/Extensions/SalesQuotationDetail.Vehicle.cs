namespace MyERP.Models
{
    public partial class SalesQuotationDetail
    {
        public int? CarTypeId { get; set; }
        public int? CarModelId { get; set; }
        public int? CarColorId { get; set; }
        public int? VehicleStockId { get; set; }
    }


    public partial class SalesInvoiceDetail
    {

        public int? VehicleStockId { get; set; }
    }
}