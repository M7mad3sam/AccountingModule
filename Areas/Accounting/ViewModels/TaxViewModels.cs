using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using AspNetCoreMvcTemplate.Areas.Accounting.Models;

namespace AspNetCoreMvcTemplate.Areas.Accounting.ViewModels
{
    public class TaxRateViewModel
    {
        public Guid Id { get; set; }
        
        [Required]
        [StringLength(50)]
        [Display(Name = "Code")]
        public string Code { get; set; }
        
        [Required]
        [StringLength(100)]
        [Display(Name = "Name (English)")]
        public string NameEn { get; set; }
        
        [Required]
        [StringLength(100)]
        [Display(Name = "Name (Arabic)")]
        public string NameAr { get; set; }
        
        [Required]
        [Display(Name = "Rate (%)")]
        [Range(0, 100, ErrorMessage = "Rate must be between 0 and 100")]
        public decimal Rate { get; set; }
        
        [Required]
        [Display(Name = "Type")]
        public TaxType Type { get; set; }
        
        [Display(Name = "Description")]
        public string Description { get; set; }
        
        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;
        
        [Display(Name = "Effective From")]
        [DataType(DataType.Date)]
        public DateTime EffectiveFrom { get; set; } = DateTime.Today;
        
        [Display(Name = "Effective To")]
        [DataType(DataType.Date)]
        public DateTime? EffectiveTo { get; set; }
    }
    
    public class TaxCalculationViewModel
    {
        [Required]
        [Display(Name = "Amount Before Tax")]
        public decimal AmountBeforeTax { get; set; }
        
        [Required]
        [Display(Name = "Tax Rate")]
        public Guid TaxRateId { get; set; }
        
        public IEnumerable<SelectListItem> AvailableTaxRates { get; set; } = new List<SelectListItem>();
        
        [Display(Name = "Tax Amount")]
        public decimal TaxAmount { get; set; }
        
        [Display(Name = "Amount After Tax")]
        public decimal AmountAfterTax { get; set; }
    }
    
    public class WithholdingTaxViewModel
    {
        public Guid Id { get; set; }
        
        [Required]
        [StringLength(50)]
        [Display(Name = "Code")]
        public string Code { get; set; }
        
        [Required]
        [StringLength(100)]
        [Display(Name = "Name (English)")]
        public string NameEn { get; set; }
        
        [Required]
        [StringLength(100)]
        [Display(Name = "Name (Arabic)")]
        public string NameAr { get; set; }
        
        [Required]
        [Display(Name = "Rate (%)")]
        [Range(0, 100, ErrorMessage = "Rate must be between 0 and 100")]
        public decimal Rate { get; set; }
        
        [Display(Name = "Description")]
        public string Description { get; set; }
        
        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;
        
        [Display(Name = "Effective From")]
        [DataType(DataType.Date)]
        public DateTime EffectiveFrom { get; set; } = DateTime.Today;
        
        [Display(Name = "Effective To")]
        [DataType(DataType.Date)]
        public DateTime? EffectiveTo { get; set; }
        
        [Display(Name = "Applies To Vendor Types")]
        public List<string> ApplicableVendorTypes { get; set; } = new List<string>();
        
        [Display(Name = "Minimum Threshold")]
        public decimal? MinimumThreshold { get; set; }
    }
}
