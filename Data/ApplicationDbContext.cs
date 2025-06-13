using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using AspNetCoreMvcTemplate.Models;
using AspNetCoreMvcTemplate.Areas.Accounting.Models;

namespace AspNetCoreMvcTemplate.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // Core Accounting Engine Models
    public DbSet<Account> Accounts { get; set; }
    public DbSet<CostCenter> CostCenters { get; set; }
    public DbSet<AccountCostCenter> AccountCostCenters { get; set; }
    public DbSet<FiscalYear> FiscalYears { get; set; }
    public DbSet<FiscalPeriod> FiscalPeriods { get; set; }
    public DbSet<JournalEntry> JournalEntries { get; set; }
    public DbSet<JournalEntryLine> JournalEntryLines { get; set; }
    public DbSet<TaxRate> TaxRates { get; set; }
    public DbSet<Client> Clients { get; set; }
    public DbSet<Vendor> Vendors { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Account relationships
        modelBuilder.Entity<Account>()
            .HasMany(a => a.Children)
            .WithOne(a => a.Parent)
            .HasForeignKey(a => a.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        // Account-CostCenter many-to-many relationship
        modelBuilder.Entity<AccountCostCenter>()
            .HasKey(ac => new { ac.AccountId, ac.CostCenterId });

        modelBuilder.Entity<AccountCostCenter>()
            .HasOne(ac => ac.Account)
            .WithMany(a => a.AccountCostCenters)
            .HasForeignKey(ac => ac.AccountId);

        modelBuilder.Entity<AccountCostCenter>()
            .HasOne(ac => ac.CostCenter)
            .WithMany(c => c.AccountCostCenters)
            .HasForeignKey(ac => ac.CostCenterId);

        // CostCenter relationships
        modelBuilder.Entity<CostCenter>()
            .HasMany(c => c.Children)
            .WithOne(c => c.Parent)
            .HasForeignKey(c => c.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        // FiscalYear-FiscalPeriod relationship
        modelBuilder.Entity<FiscalPeriod>()
            .HasOne(fp => fp.FiscalYear)
            .WithMany(fy => fy.FiscalPeriods)
            .HasForeignKey(fp => fp.FiscalYearId);

        // JournalEntry-JournalEntryLine relationship
        modelBuilder.Entity<JournalEntryLine>()
            .HasOne(jel => jel.JournalEntry)
            .WithMany(je => je.Lines)
            .HasForeignKey(jel => jel.JournalEntryId);

        // JournalEntryLine-Account relationship
        modelBuilder.Entity<JournalEntryLine>()
            .HasOne(jel => jel.Account)
            .WithMany(a => a.JournalEntryLines)
            .HasForeignKey(jel => jel.AccountId);

        // JournalEntryLine-CostCenter relationship
        modelBuilder.Entity<JournalEntryLine>()
            .HasOne(jel => jel.CostCenter)
            .WithMany(c => c.JournalEntryLines)
            .HasForeignKey(jel => jel.CostCenterId)
            .IsRequired(false);

        // JournalEntry-FiscalPeriod relationship
        modelBuilder.Entity<JournalEntry>()
            .HasOne(je => je.FiscalPeriod)
            .WithMany(fp => fp.JournalEntries)
            .HasForeignKey(je => je.FiscalPeriodId);

        // AuditLog relationships
        modelBuilder.Entity<AuditLog>()
            .HasOne(al => al.User)
            .WithMany()
            .HasForeignKey(al => al.UserId);

        // Client relationships
        modelBuilder.Entity<Client>()
            .HasOne(c => c.Account)
            .WithMany()
            .HasForeignKey(c => c.AccountId);

        // Vendor relationships
        modelBuilder.Entity<Vendor>()
            .HasOne(v => v.Account)
            .WithMany()
            .HasForeignKey(v => v.AccountId);

        // FiscalPeriod relationships
        modelBuilder.Entity<FiscalPeriod>()
            .HasOne(fp => fp.CreatedBy)
            .WithMany()
            .HasForeignKey(fp => fp.CreatedById);

        modelBuilder.Entity<FiscalPeriod>()
            .HasOne(fp => fp.ModifiedBy)
            .WithMany()
            .HasForeignKey(fp => fp.ModifiedById)
            .IsRequired(false);

        modelBuilder.Entity<FiscalPeriod>()
            .HasOne(fp => fp.ClosedBy)
            .WithMany()
            .HasForeignKey(fp => fp.ClosedById)
            .IsRequired(false);

        // FiscalYear relationships
        modelBuilder.Entity<FiscalYear>()
            .HasOne(fy => fy.CreatedBy)
            .WithMany()
            .HasForeignKey(fy => fy.CreatedById);

        modelBuilder.Entity<FiscalYear>()
            .HasOne(fy => fy.ModifiedBy)
            .WithMany()
            .HasForeignKey(fy => fy.ModifiedById)
            .IsRequired(false);

        modelBuilder.Entity<FiscalYear>()
            .HasOne(fy => fy.ClosedBy)
            .WithMany()
            .HasForeignKey(fy => fy.ClosedById)
            .IsRequired(false);

        // JournalEntry relationships
        modelBuilder.Entity<JournalEntry>()
            .HasOne(je => je.Client)
            .WithMany()
            .HasForeignKey(je => je.ClientId)
            .IsRequired(false);

        modelBuilder.Entity<JournalEntry>()
            .HasIndex(j => j.Reference)
            .IsUnique();

        modelBuilder.Entity<JournalEntry>()
            .HasOne(je => je.Vendor)
            .WithMany()
            .HasForeignKey(je => je.VendorId)
            .IsRequired(false);

        modelBuilder.Entity<JournalEntry>()
            .HasOne(je => je.CreatedBy)
            .WithMany()
            .HasForeignKey(je => je.CreatedById);

        modelBuilder.Entity<JournalEntry>()
            .HasOne(je => je.ModifiedBy)
            .WithMany()
            .HasForeignKey(je => je.ModifiedById)
            .IsRequired(false);

        modelBuilder.Entity<JournalEntry>()
            .HasOne(je => je.ApprovedBy)
            .WithMany()
            .HasForeignKey(je => je.ApprovedById)
            .IsRequired(false);

        modelBuilder.Entity<JournalEntry>()
            .HasOne(je => je.PostedBy)
            .WithMany()
            .HasForeignKey(je => je.PostedById)
            .IsRequired(false);

        // JournalEntryLine relationships
        modelBuilder.Entity<JournalEntryLine>()
            .HasOne(jel => jel.TaxRate)
            .WithMany(tr => tr.JournalEntryLines)
            .HasForeignKey(jel => jel.TaxRateId)
            .IsRequired(false);

        modelBuilder.Entity<JournalEntryLine>()
            .HasOne(jel => jel.WithholdingTax)
            .WithMany(wt => wt.JournalEntryLines)
            .HasForeignKey(jel => jel.WithholdingTaxId)
            .IsRequired(false);
    }
}
