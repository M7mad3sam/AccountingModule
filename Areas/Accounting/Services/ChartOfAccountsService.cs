using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AspNetCoreMvcTemplate.Areas.Accounting.Models;
using AspNetCoreMvcTemplate.Areas.Accounting.Data.Specifications;
using AspNetCoreMvcTemplate.Data.Repository;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Rendering;
using AspNetCoreMvcTemplate.Areas.Accounting.ViewModels;

namespace AspNetCoreMvcTemplate.Areas.Accounting.Services
{
    public interface IChartOfAccountsService
    {
        Task<Account> GetAccountByIdAsync(Guid id);
        Task<IEnumerable<Account>> GetAllAccountsAsync();
        Task<IEnumerable<Account>> GetAccountHierarchyAsync();
        Task<Account> CreateAccountAsync(Account account);
        Task UpdateAccountAsync(Account account);
        Task DeleteAccountAsync(Guid id);
        Task<bool> IsAccountCodeUniqueAsync(string code, Guid? id = null);
        Task<bool> CanDeleteAccountAsync(Guid id);
        Task<IEnumerable<AccountCostCenter>> GetAccountCostCentersAsync(Guid accountId);
        Task AddAccountCostCenterAsync(Guid accountId, Guid costCenterId);
        Task RemoveAccountCostCenterAsync(Guid accountId, Guid costCenterId);
        Task<IEnumerable<Account>> GetAccountsAsync(bool? isActive = null);
        Task<IEnumerable<SelectListItem>> GetAccountSelectListAsync(bool? isActive = null);
        Task<IEnumerable<SelectListItem>> GetParentAccountSelectListAsync(Guid? excludeId = null);
        Task<AccountCostCentersViewModel> GetAccountCostCentersViewModelAsync(Guid accountId, string accountName);
        Task<(bool Success, string ErrorCode)> LinkCostCenterAsync(Guid accountId, Guid costCenterId);
    }

    public class ChartOfAccountsService : IChartOfAccountsService
    {
        private readonly IRepository<Account> _accountRepository;
        private readonly IRepository<AccountCostCenter> _accountCostCenterRepository;
        private readonly IRepository<JournalEntryLine> _journalEntryLineRepository;
        private readonly ICostCenterService _costCenterService;

        public ChartOfAccountsService(
            IRepository<Account> accountRepository,
            IRepository<AccountCostCenter> accountCostCenterRepository,
            IRepository<JournalEntryLine> journalEntryLineRepository,
            ICostCenterService costCenterService)
        {
            _accountRepository = accountRepository;
            _accountCostCenterRepository = accountCostCenterRepository;
            _journalEntryLineRepository = journalEntryLineRepository;
            _costCenterService = costCenterService;
        }

        public async Task<Account> GetAccountByIdAsync(Guid id)
        {
            return await _accountRepository.GetByIdAsync(id, a => a.Parent);
        }

        public async Task<IEnumerable<Account>> GetAllAccountsAsync()
        {
            return await _accountRepository.GetAllAsync();
        }

        public async Task<IEnumerable<Account>> GetAccountHierarchyAsync()
        {
            var spec = new AccountHierarchySpecification();
            return await _accountRepository.FindAllAsync(spec.Criteria, a => a.Children);
        }

        public async Task<Account> CreateAccountAsync(Account account)
        {
            if (account.ParentId.HasValue)
            {
                var parent = await _accountRepository.GetByIdAsync(account.ParentId.Value);
                if (parent != null)
                {
                    account.Level = parent.Level + 1;
                }
            }
            else
            {
                account.Level = 1;
            }

            await _accountRepository.AddAsync(account);
            await _accountRepository.SaveAsync();
            return account;
        }

        public async Task UpdateAccountAsync(Account account)
        {
            if (account.ParentId.HasValue)
            {
                var parent = await _accountRepository.GetByIdAsync(account.ParentId.Value);
                if (parent != null)
                {
                    account.Level = parent.Level + 1;
                }
            }
            else
            {
                account.Level = 1;
            }

            _accountRepository.Update(account);
            await _accountRepository.SaveAsync();
        }

        public async Task DeleteAccountAsync(Guid id)
        {
            var account = await _accountRepository.GetByIdAsync(id);
            if (account != null)
            {
                _accountRepository.Delete(account);
                await _accountRepository.SaveAsync();
            }
        }

        public async Task<bool> IsAccountCodeUniqueAsync(string code, Guid? id = null)
        {
            var spec = new AccountByCodeSpecification(code);
            var existingAccount = await _accountRepository.FindAsync(spec.Criteria);
            
            return existingAccount == null || (id.HasValue && existingAccount.Id == id.Value);
        }

        public async Task<bool> CanDeleteAccountAsync(Guid id)
        {
            // Check if account has journal entry lines
            var hasJournalEntryLines = await _journalEntryLineRepository.ExistsAsync(jel => jel.AccountId == id);
            if (hasJournalEntryLines)
                return false;

            // Check if account has children
            var spec = new AccountWithChildrenSpecification(id);
            var account = await _accountRepository.FindAsync(spec.Criteria, a => a.Children);
            
            return account?.Children == null || !account.Children.Any();
        }

        public async Task<IEnumerable<AccountCostCenter>> GetAccountCostCentersAsync(Guid accountId)
        {
            return await _accountCostCenterRepository.FindAllAsync(acc => acc.AccountId == accountId, acc => acc.CostCenter);
        }

        public async Task AddAccountCostCenterAsync(Guid accountId, Guid costCenterId)
        {
            var exists = await _accountCostCenterRepository.ExistsAsync(acc => 
                acc.AccountId == accountId && acc.CostCenterId == costCenterId);
                
            if (!exists)
            {
                var accountCostCenter = new AccountCostCenter
                {
                    AccountId = accountId,
                    CostCenterId = costCenterId
                };
                
                await _accountCostCenterRepository.AddAsync(accountCostCenter);
                await _accountCostCenterRepository.SaveAsync();
            }
        }

        public async Task RemoveAccountCostCenterAsync(Guid accountId, Guid costCenterId)
        {
            var accountCostCenter = await _accountCostCenterRepository.FindAsync(acc => 
                acc.AccountId == accountId && acc.CostCenterId == costCenterId);
                
            if (accountCostCenter != null)
            {
                _accountCostCenterRepository.Delete(accountCostCenter);
                await _accountCostCenterRepository.SaveAsync();
            }
        }

        public async Task<IEnumerable<Account>> GetAccountsAsync(bool? isActive = null)
        {
            if (isActive.HasValue)
            {
                return await _accountRepository.FindAllAsync(a => a.IsActive == isActive.Value);
            }
            return await _accountRepository.GetAllAsync();
        }

        public async Task<IEnumerable<SelectListItem>> GetAccountSelectListAsync(bool? isActive = null)
        {
            var accounts = await GetAccountsAsync(isActive);
            return accounts.Select(a => new SelectListItem
            {
                Value = a.Id.ToString(),
                Text = $"{a.Code} - {a.NameEn}"
            }).ToList();
        }

        public async Task<IEnumerable<SelectListItem>> GetParentAccountSelectListAsync(Guid? excludeId = null)
        {
            var accounts = await GetAllAccountsAsync();
            var filteredAccounts = excludeId.HasValue 
                ? accounts.Where(a => a.Id != excludeId.Value) 
                : accounts;
                
            return filteredAccounts
                .OrderBy(a => a.Code)
                .Select(a => new SelectListItem
                {
                    Value = a.Id.ToString(),
                    Text = $"{a.Code} - {a.NameEn}"
                }).ToList();
        }

        public async Task<AccountCostCentersViewModel> GetAccountCostCentersViewModelAsync(Guid accountId, string accountName)
        {
            var accountCostCenters = await GetAccountCostCentersAsync(accountId);
            var allCostCenters = await _costCenterService.GetAllCostCentersAsync();
            
            return new AccountCostCentersViewModel
            {
                AccountId = accountId,
                AccountName = accountName,
                AccountCostCenters = accountCostCenters,
                AvailableCostCenters = allCostCenters.Where(cc => 
                    !accountCostCenters.Any(acc => acc.CostCenterId == cc.Id)).ToList()
            };
        }

        public async Task<(bool Success, string ErrorCode)> LinkCostCenterAsync(Guid accountId, Guid costCenterId)
        {
            var account = await GetAccountByIdAsync(accountId);
            if (account == null)
            {
                return (false, "NotFoundAccount");
            }

            var costCenter = await _costCenterService.GetCostCenterByIdAsync(costCenterId);
            if (costCenter == null)
            {
                return (false, "NotFoundCostCenter");
            }

            var existingLinks = await GetAccountCostCentersAsync(accountId);
            if (existingLinks.Any(acc => acc.CostCenterId == costCenterId))
            {
                return (false, "AlreadyLinked");
            }

            await AddAccountCostCenterAsync(accountId, costCenterId);
            return (true, string.Empty);
        }
    }
}
