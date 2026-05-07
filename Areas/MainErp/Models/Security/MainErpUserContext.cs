using System;

namespace MyERP.Areas.MainErp.Models.Security
{
    [Serializable]
    public class MainErpUserContext
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
        public int? UserType { get; set; }
        public bool IsAdmin { get; set; }
        public string ConnectionStringName { get; set; }
        public string DatabaseName { get; set; }
        public string DefaultsWarning { get; set; }
    }
}
