namespace MyERP.Areas.MainErp.Infrastructure
{
    public class ManualIdAllocation
    {
        public string TableName { get; set; }
        public string ColumnName { get; set; }
        public long Value { get; set; }
        public bool IsPreview { get; set; }
        public string Strategy { get; set; }
        public string Warning { get; set; }
    }
}
