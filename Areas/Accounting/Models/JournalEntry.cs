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
        
        /// <summary>
        /// Timestamp when the entry was first saved (local time).
        /// </summary>
        [Required]
        public DateTime EntryDate { get; set; }
        
        /// <summary>
        /// Ledger-effective date; used for period posting & reports.
        /// </summary>
        [Required]
        [DataType(DataType.Date)]
        public DateTime PostingDate { get; set; }
        
        /// <summary>
        /// Calendar date of the underlying business event (e.g., invoice date).
        /// </summary>
        [DataType(DataType.Date)]
        public DateTime TransactionDate { get; set; }
        
        [StringLength(500)]
        public string? Description { get; set; }
        
        /// <summary>
        /// Auto-generated internal document number.
        /// </summary>
        [StringLength(50)]
        public string Reference { get; set; }
        
        [Required]
        public JournalEntryStatus Status { get; set; }
        
        [Required]
        public decimal TotalDebit { get; set; }
        
        [Required]
        public decimal TotalCredit { get; set; }
        
        public Guid? ClientId { get; set; }
        public Client Client { get; set; }
        
        public Guid? VendorId { get; set; }
        public Vendor Vendor { get; set; }
        
        [StringLength(10)]
        public string Currency { get; set; } = "EGP";
        
        [Range(0.01, 1000)]
        public decimal ExchangeRate { get; set; } = 1;
        
        public bool IsRecurring { get; set; }
        
        public string? RecurrencePattern { get; set; }
        
        public DateTime? NextRecurrenceDate { get; set; }
        
        public DateTime? EndRecurrenceDate { get; set; }
        
        public bool IsSystemGenerated { get; set; }
        
        public string? SourceDocument { get; set; }
        
        public string? AttachmentUrl { get; set; }
        
        [StringLength(500)]
        public string? Notes { get; set; }
        
        [Required]
        public Guid FiscalPeriodId { get; set; }
        public FiscalPeriod FiscalPeriod { get; set; }
        
        public string CreatedById { get; set; }
        public ApplicationUser CreatedBy { get; set; }
        
        /// <summary>
        /// Record-creation audit stamp.
        /// </summary>
        [Required]
        public DateTime CreatedUtc { get; set; }
        
        // Renamed to match service usage
        public DateTime CreatedDate { get => CreatedUtc; set => CreatedUtc = value; }
        
        public string ModifiedById { get; set; }
        public ApplicationUser ModifiedBy { get; set; }
        
        public DateTime? ModifiedDate { get; set; }
        
        public string? ApprovedById { get; set; }
        public ApplicationUser ApprovedBy { get; set; }
        
        /// <summary>
        /// UTC timestamp when Manager approved the entry.
        /// </summary>
        public DateTime? ApprovedUtc { get; set; }
        
        // Renamed to match service usage
        public DateTime? ApprovedDate { get => ApprovedUtc; set => ApprovedUtc = value; }
        
        [StringLength(500)]
        public string? ApprovalNotes { get; set; }
        
        public string? PostedById { get; set; }
        public ApplicationUser PostedBy { get; set; }
        
        public DateTime? PostedDate { get; set; }
        
        public string? RejectionReason { get; set; }
        
        public string? IpAddress { get; set; }
        
        public ICollection<JournalEntryLine> Lines { get; set; }
        public ICollection<AuditLog> AuditLogs { get; set; }
    }

    public enum JournalEntryStatus
    {
        Draft = 1,
        Pending = 2,
        PendingApproval = 2, // Alias for Pending to maintain compatibility
        Approved = 3,
        Rejected = 4,
        Posted = 5
    }
}
