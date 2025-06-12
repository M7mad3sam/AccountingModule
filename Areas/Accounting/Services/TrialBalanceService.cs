using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AspNetCoreMvcTemplate.Areas.Accounting.Models;
using AspNetCoreMvcTemplate.Data.Repository;
using AspNetCoreMvcTemplate.Areas.Accounting.Data.Specifications;

namespace AspNetCoreMvcTemplate.Areas.Accounting.Services
{
    public interface ITrialBalanceService
    {
        Task<List<TrialBalanceRow>> GetTrialBalanceAsync(DateOnly from, DateOnly to, Guid? costCenterId);
    }

    public class TrialBalanceService : ITrialBalanceService
    {
        private readonly IRepository<JournalEntryLine> _journalEntryLineRepository;
        private readonly IRepository<Account> _accountRepository;

        public TrialBalanceService(
            IRepository<JournalEntryLine> journalEntryLineRepository,
            IRepository<Account> accountRepository)
        {
            _journalEntryLineRepository = journalEntryLineRepository;
            _accountRepository = accountRepository;
        }

        public async Task<List<TrialBalanceRow>> GetTrialBalanceAsync(DateOnly from, DateOnly to, Guid? costCenterId)
        {
            var fromDate = DateTime.SpecifyKind(new DateTime(from.Year, from.Month, from.Day, 0, 0, 0), DateTimeKind.Utc);
            var toDate = DateTime.SpecifyKind(new DateTime(to.Year, to.Month, to.Day, 23, 59, 59), DateTimeKind.Utc);
            //var spec = new JournalLinesByDateSpec(fromDate, toDate, costCenterId);
            
            var journalLines = await _journalEntryLineRepository.FindAllAsync(jl => jl.JournalEntry.PostingDate >= fromDate && jl.JournalEntry.PostingDate <= toDate
                && (!costCenterId.HasValue || jl.CostCenterId == costCenterId.Value), jl => jl.JournalEntry, jl => jl.Account);
            var accounts = await _accountRepository.GetAllAsync();

            var trialBalance = journalLines
                .GroupBy(jl => jl.AccountId)
                .Select(g => new
                {
                    AccountId = g.Key,
                    Debit = g.Sum(jl => jl.Debit),
                    Credit = g.Sum(jl => jl.Credit)
                })
                .Join(accounts,
                    tb => tb.AccountId,
                    a => a.Id,
                    (tb, a) => new TrialBalanceRow
                    {
                        AccountCode = a.Code,
                        AccountName = a.NameEn,
                        Debit = tb.Debit,
                        Credit = tb.Credit,
                        Net = tb.Debit - tb.Credit
                    })
                .OrderBy(r => r.AccountCode)
                .ToList();

            return trialBalance;
        }
    }

    public class TrialBalanceRow
    {
        public string AccountCode { get; set; }
        public string AccountName { get; set; }
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public decimal Net { get; set; }
    }
}
