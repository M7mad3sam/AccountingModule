using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AspNetCoreMvcTemplate.Areas.Accounting.Models
{
    public class AccountCostCenter
    {
        public Guid AccountId { get; set; }
        public Account Account { get; set; }
        
        public Guid CostCenterId { get; set; }
        public CostCenter CostCenter { get; set; }
    }
}
