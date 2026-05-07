using System;
using System.Collections.Generic;

namespace MyERP.Areas.MainErp.Models.Accounting
{
    public class VoucherBatch
    {
        public VoucherBatch()
        {
            Entries = new List<VoucherEntry>();
            CorrelationId = Guid.NewGuid();
        }

        public long? VoucherId { get; set; }
        public bool OpeningBalanceMode { get; set; }
        public double? OpeningBalanceVoucherId { get; set; }
        public bool PreviewOnly { get; set; }
        public string SourceModule { get; set; }
        public string SourceEntity { get; set; }
        public string SourceKey { get; set; }
        public Guid CorrelationId { get; set; }
        public IList<VoucherEntry> Entries { get; private set; }
    }
}
