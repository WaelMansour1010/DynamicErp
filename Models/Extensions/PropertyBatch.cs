using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace MyERP.Models
{
    public partial class PropertyBatch
    {
        /// <summary>
        /// إجمالي قيمة الدفعة شامل الضريبة والخصم والاضافة
        /// </summary>
        [NotMapped]
        public decimal TotalBatchValue
        {
            get
            {
                var valueAfterDiscount = (BatchValueBeforeDiscountAddtionAndTax ?? 0) - (Discount ?? 0);
                var valueAfterDiscountAndAddvalue = valueAfterDiscount + (AddValue ?? 0) ;
                
                return valueAfterDiscountAndAddvalue + (decimal)BatchTaxValue;
            }
        }

        [NotMapped]
        public double BatchTaxValue
        {
            get
            {
                var valueAfterDiscount = (BatchValueBeforeDiscountAddtionAndTax ?? 0) - (Discount ?? 0);
                var valueAfterDiscountAndAddvalue = valueAfterDiscount + (AddValue ?? 0);


                return (double)(valueAfterDiscountAndAddvalue) * (BatchTaxPercentage ?? 0) / 100;
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
                return TotalBatchValue - (TotalPaid ?? 0);
            }
        }

        /// <summary>
        /// نسبة السداد للدفعة الحالية
        /// </summary>
        [NotMapped]
        public decimal PaymentPercentage
        {
            get
            {
                if (TotalBatchValue == 0) return 0;
                var paidAmount = TotalPaid ?? 0;
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
                var paidAmount = TotalPaid ?? 0;
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
                var paidAmount = TotalPaid ?? 0;
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