namespace MyERP.Common.Users
{
    public class SharedUserListRow
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

    public class SharedUserSearchResult
    {
        public SharedUserSearchResult()
        {
            Items = new System.Collections.Generic.List<SharedUserListRow>();
        }

        public int TotalRows { get; set; }
        public System.Collections.Generic.IList<SharedUserListRow> Items { get; private set; }
    }
}
