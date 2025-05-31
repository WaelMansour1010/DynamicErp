using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace MyERP.Models
{
    public partial class PropertyBatch
    {
        /// <summary>
        /// إجمالي قيمة الدفعة شامل الضريبة
        /// </summary>
        [NotMapped]
        public decimal TotalBatchValue
        {
            get
            {
                var valueAfterDiscount = BatchValueAfterDiscount ?? 0;
                var taxValue = BatchTaxValue ?? 0;
                return valueAfterDiscount + taxValue;
            }
        }

        /// <summary>
        /// المتبقي من الدفعة = إجمالي القيمة - المسدد
        /// </summary>
        [NotMapped]
        public decimal Remain
        {
            get
            {
                var totalValue = TotalBatchValue;
                var paidAmount = Paid ?? 0;
                return totalValue - paidAmount;
            }
        }

        /// <summary>
        /// المسدد من قبل (إجمالي السداد السابق لكل الدفعات قبل هذه الدفعة)
        /// </summary>
        [NotMapped]
        public decimal TotalPaidBefore { get; set; }

        /// <summary>
        /// نسبة السداد للدفعة الحالية
        /// </summary>
        [NotMapped]
        public decimal PaymentPercentage
        {
            get
            {
                if (TotalBatchValue == 0) return 0;
                var paidAmount = Paid ?? 0;
                return Math.Round((paidAmount / TotalBatchValue) * 100, 2);
            }
        }

        /// <summary>
        /// حالة السداد
        /// </summary>
        [NotMapped]
        public string PaymentStatus
        {
            get
            {
                var paidAmount = Paid ?? 0;
                var totalValue = TotalBatchValue;

                if (paidAmount == 0)
                    return "غير مسدد";
                else if (paidAmount >= totalValue)
                    return "مسدد بالكامل";
                else
                    return "مسدد جزئياً";
            }
        }

        /// <summary>
        /// هل الدفعة مسددة بالكامل؟
        /// </summary>
        [NotMapped]
        public bool IsFullyPaid
        {
            get
            {
                var paidAmount = Paid ?? 0;
                return paidAmount >= TotalBatchValue;
            }
        }

        /// <summary>
        /// هل يمكن تعديل قيمة السداد؟
        /// </summary>
        [NotMapped]
        public bool CanEditPayment
        {
            get
            {
                return !(IsDelivered == true || IsFullyPaid);
            }
        }
    }
}