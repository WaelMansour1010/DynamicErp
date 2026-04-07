using System.Text.Json.Serialization;

namespace EazyCash.Models
{
    public class ResModel
    {
        public bool isok { get; set; }
        public string msg { get; set; }
        public bool isclean { get; set; }
        public ClientLighResualt data { get; set; }

    }
    class InvoiceViewModel
    { 
        [JsonPropertyName("orderId")]
        public Guid OrderId { get; set; }
        [JsonPropertyName("id")]
        public Guid RowId { get; set; }
        [JsonPropertyName("orderno")]
        public int OrderNo { get; set; }
        [JsonPropertyName("employeeid")]
        public int EmployeeId { get; set; }
        [JsonPropertyName("em[loyeecode")]
        public string EmployeeCode { get; set; }
        [JsonPropertyName("employeename")]
        public string EmployeeName { get; set; }
        [JsonPropertyName("orderdate")]
        public DateTime? OrderDate { get; set; }
        [JsonPropertyName("itemid")]
        public int ItemId { get; set; }
        [JsonPropertyName("itemcode")]
        public string ItemCode { get; set; }
        [JsonPropertyName("itemname")]
        public string ItemName { get; set; }
        [JsonPropertyName("qty")]
        public int Qty { get; set; }
        [JsonPropertyName("notes")]
        public string Notes { get; set; }
         [JsonPropertyName("managerid")]

        public int ManagerId { get; set; }
        [JsonPropertyName("itemnamee")]

        public string ItemNamee { get; set; }
    }

}
