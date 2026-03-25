using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MyERP.Models.MyModels
{
    public class BatchPaidModel
    {
        public int Id { get; set; }
        public decimal Paid { get; set; }
    }

    public class UniteModel
    {
        public int   Id { get; set; }
        public string PropertyUnitNo { get; set; }
    }
}