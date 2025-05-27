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
    }
}
