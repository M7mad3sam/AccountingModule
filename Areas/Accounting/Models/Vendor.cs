using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AspNetCoreMvcTemplate.Areas.Accounting.Models
{
    public class Vendor
    {
        public Guid Id { get; set; }
        
        [Required]
        [StringLength(20)]
        public string Code { get; set; }
        
        [Required]
        [StringLength(100)]
        public string NameEn { get; set; }
        
        [Required]
        [StringLength(100)]
        public string NameAr { get; set; }
        
        [Required]
        public VendorType Type { get; set; }
        
        [StringLength(50)]
        public string TaxRegistrationNumber { get; set; }
        
        [StringLength(200)]
        public string Address { get; set; }
        
        [StringLength(20)]
        public string Phone { get; set; }
        
        [StringLength(100)]
        [EmailAddress]
        public string Email { get; set; }
        
        [StringLength(50)]
        public string CommercialRegistration { get; set; }
        
        [StringLength(100)]
        public string ContactPerson { get; set; }
        
        [StringLength(100)]
        public string PaymentTerms { get; set; }
        
        public string VendorType { get; set; }
        
        public bool SubjectToWithholdingTax { get; set; }
        
        [StringLength(500)]
        public string Notes { get; set; }
        
        [StringLength(100)]
        public string BankName { get; set; }
        
        [StringLength(50)]
        public string BankAccountNumber { get; set; }
        
        [StringLength(50)]
        public string IBAN { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        [Required]
        public Guid AccountId { get; set; }
        public Account Account { get; set; }
    }
    
    public enum VendorType
    {
        Individual = 1,
        Company = 2,
        Government = 3
    }
}
