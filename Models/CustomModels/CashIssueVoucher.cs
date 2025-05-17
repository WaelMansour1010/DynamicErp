using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace MyERP.Models 
{
    public partial class CashIssueVoucher
    {
        [NotMapped]
        public List<BatchData> PropertyBatches { get; set; }
    }

    public class BatchData
    {
        public int Id { get; set; }
        public decimal Paid { get; set; }
    }
}