using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MyERP.ViewModels
{
    public class PropertyBillRegisterationViewModel
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Transaction Number")]
        public int TransactionNumber { get; set; }

        [Display(Name = "Bill Registration Date")]
        public DateTime? BillRegDate { get; set; }

        [Required]
        [Display(Name = "Contract")]
        public int ContractId { get; set; }

        [Required]
        [Display(Name = "PropertyId")]
        public int PropertyId { get; set; }

        [Required]
        [Display(Name = "Renter")]
        public int RenterId { get; set; }

        [Required]
        [Display(Name = "Property Detail")]
        public int PropertyDetailId { get; set; }

        [Display(Name = "Gas Bill Value")]
        public decimal? GasBillValue { get; set; }

        [Display(Name = "Electricity Bill Value")]
        public decimal? ElectricityBillValue { get; set; }

        [Display(Name = "Violation Bill Value")]
        public decimal? ViolationBillValue { get; set; }

        // Dropdown data
        public SelectList Contracts { get; set; }
        public string ContractDocumentNumber { get; set; }
        public string RenterArName { get; set; }
        public string PropertyArName { get; set; }
        public string PropertyUnitNo { get; set; }
    }
}