

namespace MyERP.Models.CustomModels
{
    public class SalaryItemDto
    {
        public int SalaryItemId { get; set; }
        public string Code { get; set; }
        public string ArName { get; set; }
        public decimal Amount { get; set; }
        public byte Type { get; set; }
    }
}