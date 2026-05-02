using System;
using System.Collections.Generic;

namespace MyERP.Models
{
    public class PurchaseRequestHistory
    {
        public string DocumentNumber { get; set; }
        public System.DateTime VoucherDate { get; set; }
        public string User{ get; set; }
        public Nullable<System.DateTime> ApprovalDate { get; set; }
        public Nullable<int> ApprovalUserId { get; set; }
        public string ApprovalUserName { get; set; }
        public bool IsApproved { get; set; }
        public virtual IEnumerable<ManufacturingRequest> ManufacturingRequests { get; set; }
    }

    public partial class ManufacturingOrder
    {
        public string CreatedByUser { get; set; }
        public IEnumerable<string> StockIssueDocNum { get; set; }
        public IEnumerable<string> StockReceiptDocNum { get; set; }

    }
}