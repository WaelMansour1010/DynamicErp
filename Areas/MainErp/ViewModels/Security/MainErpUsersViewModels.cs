namespace MyERP.Areas.MainErp.ViewModels.Security
{
    public class MainErpUserListRow
    {
        public int UserId { get; set; }
        public string UserName { get; set; }
        public string EmployeeName { get; set; }
        public string BranchName { get; set; }
        public string StoreName { get; set; }
        public string BoxName { get; set; }
        public int? UserType { get; set; }
        public bool IsDeactivated { get; set; }
        public bool IsAdmin { get; set; }
    }
}
