using System;
using System.Linq.Expressions;
using AspNetCoreMvcTemplate.Areas.Accounting.Models;

namespace AspNetCoreMvcTemplate.Areas.Accounting.Data.Specifications
{
    public class JournalLinesByDateSpec : BaseSpecification<JournalEntryLine>
    {
        public JournalLinesByDateSpec(DateTime fromDate, DateTime toDate, Guid? costCenterId)
            : base(jl => jl.JournalEntry.PostingDate >= fromDate 
                && jl.JournalEntry.PostingDate <= toDate 
                && (!costCenterId.HasValue || jl.CostCenterId == costCenterId.Value))
        {
            AddInclude(jl => jl.JournalEntry);
            AddInclude(jl => jl.Account);
        }
    }
}
