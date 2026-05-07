using System.Collections.Generic;

namespace MyERP.Areas.MainErp.ViewModels
{
    public class PagedReadResult<T>
    {
        public PagedReadResult()
        {
            Items = new List<T>();
        }

        public IList<T> Items { get; private set; }
        public int TotalCount { get; set; }
        public string Warning { get; set; }
        public object Diagnostics { get; set; }
    }
}
