namespace MyERP.Areas.MainErp.Infrastructure
{
    public sealed class ManualIdTarget
    {
        public static readonly ManualIdTarget NotesNoteId = new ManualIdTarget("Notes", "NoteID");
        public static readonly ManualIdTarget TblLcId = new ManualIdTarget("TblLC", "TblLCID");
        public static readonly ManualIdTarget ProjectBillId = new ManualIdTarget("project_billl", "id");
        public static readonly ManualIdTarget VoucherId = new ManualIdTarget("DOUBLE_ENTREY_VOUCHERS", "Double_Entry_Vouchers_ID");
        public static readonly ManualIdTarget OpeningBalanceVoucherId = new ManualIdTarget("DOUBLE_ENTREY_VOUCHERS1", "Double_Entry_Vouchers_ID");

        private ManualIdTarget(string tableName, string columnName)
        {
            TableName = tableName;
            ColumnName = columnName;
            ApplicationLockName = "MainErp.ManualId." + tableName + "." + columnName;
        }

        public string TableName { get; private set; }
        public string ColumnName { get; private set; }
        public string ApplicationLockName { get; private set; }
    }
}
