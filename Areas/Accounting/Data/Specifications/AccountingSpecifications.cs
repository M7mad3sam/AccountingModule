using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AspNetCoreMvcTemplate.Areas.Accounting.Models;
using AspNetCoreMvcTemplate.Data.Repository;

namespace AspNetCoreMvcTemplate.Areas.Accounting.Data.Specifications
{
    // Account Specifications
    public class AccountByCodeSpecification : BaseSpecification<Account>
    {
        public AccountByCodeSpecification(string code) : base(a => a.Code == code)
        {
        }
    }

    public class AccountHierarchySpecification : BaseSpecification<Account>
    {
        public AccountHierarchySpecification() : base(a => true)
        {
            AddInclude(a => a.Children);
            ApplyOrderBy(a => a.Code);
        }
    }

    public class AccountWithChildrenSpecification : BaseSpecification<Account>
    {
        public AccountWithChildrenSpecification(Guid accountId) : base(a => a.Id == accountId)
        {
            AddInclude(a => a.Children);
        }
    }

    // Cost Center Specifications
    public class CostCenterByCodeSpecification : BaseSpecification<CostCenter>
    {
        public CostCenterByCodeSpecification(string code) : base(cc => cc.Code == code)
        {
        }
    }

    public class CostCenterHierarchySpecification : BaseSpecification<CostCenter>
    {
        public CostCenterHierarchySpecification() : base(cc => true)
        {
            AddInclude(cc => cc.Children);
            ApplyOrderBy(cc => cc.Code);
        }
    }

    // Journal Entry Specifications
    public class JournalEntryWithLinesSpecification : BaseSpecification<JournalEntry>
    {
        public JournalEntryWithLinesSpecification(Guid journalEntryId) : base(je => je.Id == journalEntryId)
        {
            AddInclude(je => je.Lines);
            AddInclude(je => je.FiscalPeriod);
            AddInclude(je => je.CreatedBy);
            AddInclude(je => je.ApprovedBy);
        }
    }

    public class JournalEntriesByDateRangeSpecification : BaseSpecification<JournalEntry>
    {
        public JournalEntriesByDateRangeSpecification(DateTime? fromDate, DateTime? toDate, JournalEntryStatus? status, int pageIndex, int pageSize)
            : base(je => 
                (!fromDate.HasValue || je.Date >= fromDate) &&
                (!toDate.HasValue || je.Date <= toDate) &&
                (!status.HasValue || je.Status == status))
        {
            AddInclude(je => je.FiscalPeriod);
            AddInclude(je => je.CreatedBy);
            ApplyOrderByDescending(je => je.Date);
            ApplyPaging((pageIndex - 1) * pageSize, pageSize);
        }
    }

    // Fiscal Period Specifications
    public class CurrentFiscalPeriodSpecification : BaseSpecification<FiscalPeriod>
    {
        public CurrentFiscalPeriodSpecification(DateTime date) : base(fp => fp.StartDate <= date && fp.EndDate >= date)
        {
            AddInclude(fp => fp.FiscalYear);
        }
    }

    public class FiscalPeriodsByYearSpecification : BaseSpecification<FiscalPeriod>
    {
        public FiscalPeriodsByYearSpecification(Guid fiscalYearId) : base(fp => fp.FiscalYearId == fiscalYearId)
        {
            ApplyOrderBy(fp => fp.StartDate);
        }
    }

    // Tax Rate Specifications
    public class ActiveTaxRatesSpecification : BaseSpecification<TaxRate>
    {
        public ActiveTaxRatesSpecification() : base(tr => tr.IsActive)
        {
            ApplyOrderBy(tr => tr.Code);
        }
    }

    // Client and Vendor Specifications
    public class ClientByCodeSpecification : BaseSpecification<Client>
    {
        public ClientByCodeSpecification(string code) : base(c => c.Code == code)
        {
        }
    }

    public class VendorByCodeSpecification : BaseSpecification<Vendor>
    {
        public VendorByCodeSpecification(string code) : base(v => v.Code == code)
        {
        }
    }

    // Audit Log Specifications
    public class AuditLogsByEntitySpecification : BaseSpecification<AuditLog>
    {
        public AuditLogsByEntitySpecification(string entityName, string entityId, int pageIndex, int pageSize)
            : base(al => al.EntityName == entityName && al.EntityId == entityId)
        {
            ApplyOrderByDescending(al => al.Timestamp);
            ApplyPaging((pageIndex - 1) * pageSize, pageSize);
        }
    }
}
