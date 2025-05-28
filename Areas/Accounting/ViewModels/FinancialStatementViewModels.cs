using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using AspNetCoreMvcTemplate.Areas.Accounting.Models;

namespace AspNetCoreMvcTemplate.Areas.Accounting.ViewModels
{
    public class BalanceSheetFilterViewModel
    {
        [Display(Name = "As of Date")]
        [Required(ErrorMessage = "Date is required")]
        [DataType(DataType.Date)]
        public DateTime AsOfDate { get; set; }
        
        [Display(Name = "Cost Center")]
        public Guid? CostCenterId { get; set; }
        
        public IEnumerable<CostCenter> CostCenters { get; set; } = new List<CostCenter>();
    }
    
    public class IncomeStatementFilterViewModel
    {
        [Display(Name = "From Date")]
        [Required(ErrorMessage = "From Date is required")]
        [DataType(DataType.Date)]
        public DateTime FromDate { get; set; }
        
        [Display(Name = "To Date")]
        [Required(ErrorMessage = "To Date is required")]
        [DataType(DataType.Date)]
        public DateTime ToDate { get; set; }
        
        [Display(Name = "Cost Center")]
        public Guid? CostCenterId { get; set; }
        
        public IEnumerable<CostCenter> CostCenters { get; set; } = new List<CostCenter>();
    }
    
    public class CashFlowFilterViewModel
    {
        [Display(Name = "From Date")]
        [Required(ErrorMessage = "From Date is required")]
        [DataType(DataType.Date)]
        public DateTime FromDate { get; set; }
        
        [Display(Name = "To Date")]
        [Required(ErrorMessage = "To Date is required")]
        [DataType(DataType.Date)]
        public DateTime ToDate { get; set; }
        
        [Display(Name = "Cost Center")]
        public Guid? CostCenterId { get; set; }
        
        public IEnumerable<CostCenter> CostCenters { get; set; } = new List<CostCenter>();
    }
}
