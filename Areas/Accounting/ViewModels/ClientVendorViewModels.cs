using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using AspNetCoreMvcTemplate.Areas.Accounting.Models;

namespace AspNetCoreMvcTemplate.Areas.Accounting.ViewModels
{
    public class ClientViewModel
    {
        public Guid Id { get; set; }
        
        [Required]
        [StringLength(20)]
        [Display(Name = "Client Code")]
        public string Code { get; set; }
        
        [Required]
        [StringLength(100)]
        [Display(Name = "Name (English)")]
        public string NameEn { get; set; }
        
        [Required]
        [StringLength(100)]
        [Display(Name = "Name (Arabic)")]
        public string NameAr { get; set; }
        
        [StringLength(15)]
        [Display(Name = "Tax Registration Number")]
        public string TaxRegistrationNumber { get; set; }
        
        [StringLength(20)]
        [Display(Name = "Commercial Registration")]
        public string CommercialRegistration { get; set; }
        
        [StringLength(100)]
        [Display(Name = "Contact Person")]
        public string ContactPerson { get; set; }
        
        [StringLength(15)]
        [Display(Name = "Phone")]
        public string Phone { get; set; }
        
        [StringLength(100)]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }
        
        [StringLength(200)]
        [Display(Name = "Address")]
        public string Address { get; set; }
        
        [Display(Name = "Credit Limit")]
        public decimal CreditLimit { get; set; }
        
        [Display(Name = "Credit Period (Days)")]
        public int CreditPeriod { get; set; }
        
        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;
        
        [Display(Name = "Client Type")]
        public ClientType ClientType { get; set; }
        
        [Display(Name = "Notes")]
        public string Notes { get; set; }
    }
    
    public class VendorViewModel
    {
        public Guid Id { get; set; }
        
        [Required]
        [StringLength(20)]
        [Display(Name = "Vendor Code")]
        public string Code { get; set; }
        
        [Required]
        [StringLength(100)]
        [Display(Name = "Name (English)")]
        public string NameEn { get; set; }
        
        [Required]
        [StringLength(100)]
        [Display(Name = "Name (Arabic)")]
        public string NameAr { get; set; }
        
        [StringLength(15)]
        [Display(Name = "Tax Registration Number")]
        public string TaxRegistrationNumber { get; set; }
        
        [StringLength(20)]
        [Display(Name = "Commercial Registration")]
        public string CommercialRegistration { get; set; }
        
        [StringLength(100)]
        [Display(Name = "Contact Person")]
        public string ContactPerson { get; set; }
        
        [StringLength(15)]
        [Display(Name = "Phone")]
        public string Phone { get; set; }
        
        [StringLength(100)]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }
        
        [StringLength(200)]
        [Display(Name = "Address")]
        public string Address { get; set; }
        
        [Display(Name = "Payment Terms")]
        public string PaymentTerms { get; set; }
        
        [Display(Name = "Is Active")]
        public bool IsActive { get; set; } = true;
        
        [Display(Name = "Vendor Type")]
        public string VendorType { get; set; }
        
        [Display(Name = "Subject to Withholding Tax")]
        public bool SubjectToWithholdingTax { get; set; }
        
        [Display(Name = "Notes")]
        public string Notes { get; set; }
        
        [Display(Name = "Bank Name")]
        public string BankName { get; set; }
        
        [Display(Name = "Bank Account Number")]
        public string BankAccountNumber { get; set; }
        
        [Display(Name = "IBAN")]
        public string IBAN { get; set; }
    }
    
    public class ClientListViewModel
    {
        public IEnumerable<Client> Clients { get; set; }
        public string SearchTerm { get; set; }
        public ClientType? ClientType { get; set; }
        public bool? IsActive { get; set; }
        public IEnumerable<SelectListItem> ClientTypes { get; set; }
    }
    
    public class VendorListViewModel
    {
        public IEnumerable<Vendor> Vendors { get; set; }
        public string SearchTerm { get; set; }
        public string VendorType { get; set; }
        public bool? IsActive { get; set; }
        public bool? SubjectToWithholdingTax { get; set; }
        public IEnumerable<SelectListItem> VendorTypes { get; set; }
    }
}
