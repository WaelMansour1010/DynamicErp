using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MyERP.Models.CustomModels
{
    public class CashReceiptArrear
    {
        public int Id { get; set; }
        public string ArName { get; set; }
        public Nullable<decimal> MonthlySubscription { get; set; }
        public Nullable<DateTime> SubscriptionStartDate { get; set; }
        public string DocumentNumber { get; set; }
        public Nullable<decimal> MoneyAmount { get; set; }
        public Nullable<int> NoOfMonthsOverdue { get; set; }
        public Nullable<decimal> TotalOverdue { get; set; }
        public List<CashReceiptArrearDetail> cashReceiptArrearDetails { get; set; }
    }

    public class CashReceiptArrearDetail
    {
        public int CashReceiptVoucherId { get; set; }
        public Nullable<DateTime> Date { get; set; }
        public Nullable<int> Month { get; set; }

        public Nullable<int> Year { get; set; }

    }
}