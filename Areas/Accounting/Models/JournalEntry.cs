using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using AspNetCoreMvcTemplate.Models;

namespace AspNetCoreMvcTemplate.Areas.Accounting.Models
{
    public class JournalEntry
    {
        public Guid Id { get; set; }
        
        [Required]
        [StringLength(20)]
        public string Number { get; set; }
        
        [Required]
        public DateTime Date { get; set; }
        
        [Required]
        [StringLength(500)]
        public string Description { get; set; }
        
        [StringLength(50)]
        public string Reference { get; set; }
        
        [Required]
        public JournalEntryStatus Status { get; set; }
        
        [Required]
        public decimal TotalDebit { get; set; }
        
        [Required]
        public decimal TotalCredit { get; set; }
        
        [Required]
        public Guid FiscalPeriodId { get; set; }
        public FiscalPeriod FiscalPeriod { get; set; }
        
        public string CreatedById { get; set; }
        public ApplicationUser CreatedBy { get; set; }
        
        [Required]
        public DateTime CreatedAt { get; set; }
        
        public string ApprovedById { get; set; }
        public ApplicationUser ApprovedBy { get; set; }
        
        public DateTime? ApprovedAt { get; set; }
        
        public string RejectionReason { get; set; }
        
        public string IpAddress { get; set; }
        
        public ICollection<JournalEntryLine> Lines { get; set; }
        public ICollection<AuditLog> AuditLogs { get; set; }
    }

    public enum JournalEntryStatus
    {
        Draft = 1,
        Pending = 2,
        Approved = 3,
        Rejected = 4
    }
}
