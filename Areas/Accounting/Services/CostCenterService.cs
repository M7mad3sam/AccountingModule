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
    public interface ICostCenterService
    {
        Task<CostCenter> GetCostCenterByIdAsync(Guid id);
        Task<IEnumerable<CostCenter>> GetAllCostCentersAsync();
        Task<IEnumerable<CostCenter>> GetCostCenterHierarchyAsync();
        Task<CostCenter> CreateCostCenterAsync(CostCenter costCenter);
        Task UpdateCostCenterAsync(CostCenter costCenter);
        Task DeleteCostCenterAsync(Guid id);
        Task<bool> IsCostCenterCodeUniqueAsync(string code, Guid? id = null);
        Task<bool> CanDeleteCostCenterAsync(Guid id);
        Task<IEnumerable<AccountCostCenter>> GetCostCenterAccountsAsync(Guid costCenterId);
        Task AddCostCenterAccountAsync(Guid costCenterId, Guid accountId);
        Task RemoveCostCenterAccountAsync(Guid costCenterId, Guid accountId);
        Task<IEnumerable<CostCenter>> GetCostCentersAsync(bool? isActive = null);
        Task<IEnumerable<SelectListItem>> GetCostCenterSelectListAsync(bool? isActive = null);
        Task<AssignAccountsVm> GetAssignAccountsViewModelAsync(Guid costCenterId, int maxAccounts = 500);
        Task AssignAccountsToCostCenterAsync(Guid costCenterId, List<Guid> accountIds);
    }

    public class CostCenterService : ICostCenterService
    {
        private readonly IRepository<CostCenter> _costCenterRepository;
        private readonly IRepository<AccountCostCenter> _accountCostCenterRepository;
        private readonly IRepository<JournalEntryLine> _journalEntryLineRepository;
        private readonly IRepository<Account> _accountRepository;

        public CostCenterService(
            IRepository<CostCenter> costCenterRepository,
            IRepository<AccountCostCenter> accountCostCenterRepository,
            IRepository<JournalEntryLine> journalEntryLineRepository,
            IRepository<Account> accountRepository)
        {
            _costCenterRepository = costCenterRepository;
            _accountCostCenterRepository = accountCostCenterRepository;
            _journalEntryLineRepository = journalEntryLineRepository;
            _accountRepository = accountRepository;
        }

        public async Task<CostCenter> GetCostCenterByIdAsync(Guid id)
        {
            return await _costCenterRepository.GetByIdAsync(id, query => query.Include(cc => cc.Parent));
        }

        public async Task<IEnumerable<CostCenter>> GetAllCostCentersAsync()
        {
            return await _costCenterRepository.GetAllAsync();
        }

        public async Task<IEnumerable<CostCenter>> GetCostCenterHierarchyAsync()
        {
            var spec = new CostCenterHierarchySpecification();
            return await _costCenterRepository.FindAllAsync(spec.Criteria, query => query.Include(cc => cc.Children));
        }

        public async Task<CostCenter> CreateCostCenterAsync(CostCenter costCenter)
        {
            if (costCenter.ParentId.HasValue)
            {
                var parent = await _costCenterRepository.GetByIdAsync(costCenter.ParentId.Value);
                if (parent != null)
                {
                    costCenter.Level = parent.Level + 1;
                }
            }
            else
            {
                costCenter.Level = 1;
            }

            await _costCenterRepository.AddAsync(costCenter);
            await _costCenterRepository.SaveAsync();
            return costCenter;
        }

        public async Task UpdateCostCenterAsync(CostCenter costCenter)
        {
            if (costCenter.ParentId.HasValue)
            {
                var parent = await _costCenterRepository.GetByIdAsync(costCenter.ParentId.Value);
                if (parent != null)
                {
                    costCenter.Level = parent.Level + 1;
                }
            }
            else
            {
                costCenter.Level = 1;
            }

            _costCenterRepository.Update(costCenter);
            await _costCenterRepository.SaveAsync();
        }

        public async Task DeleteCostCenterAsync(Guid id)
        {
            var costCenter = await _costCenterRepository.GetByIdAsync(id);
            if (costCenter != null)
            {
                _costCenterRepository.Delete(costCenter);
                await _costCenterRepository.SaveAsync();
            }
        }

        public async Task<bool> IsCostCenterCodeUniqueAsync(string code, Guid? id = null)
        {
            var spec = new CostCenterByCodeSpecification(code);
            var existingCostCenter = await _costCenterRepository.FindAsync(spec.Criteria);
            
            return existingCostCenter == null || (id.HasValue && existingCostCenter.Id == id.Value);
        }

        public async Task<bool> CanDeleteCostCenterAsync(Guid id)
        {
            // Check if cost center has journal entry lines
            var hasJournalEntryLines = await _journalEntryLineRepository.ExistsAsync(jel => jel.CostCenterId == id);
            if (hasJournalEntryLines)
                return false;

            // Check if cost center has children
            var spec = new CostCenterHierarchySpecification();
            var costCenter = await _costCenterRepository.FindAsync(cc => cc.Id == id, query => query.Include(cc => cc.Children));
            
            return costCenter?.Children == null || !costCenter.Children.Any();
        }

        public async Task<IEnumerable<AccountCostCenter>> GetCostCenterAccountsAsync(Guid costCenterId)
        {
            return await _accountCostCenterRepository.FindAllAsync(acc => acc.CostCenterId == costCenterId, query => query.Include(acc => acc.Account));
        }

        public async Task AddCostCenterAccountAsync(Guid costCenterId, Guid accountId)
        {
            var exists = await _accountCostCenterRepository.ExistsAsync(acc => 
                acc.CostCenterId == costCenterId && acc.AccountId == accountId);
                
            if (!exists)
            {
                var accountCostCenter = new AccountCostCenter
                {
                    CostCenterId = costCenterId,
                    AccountId = accountId
                };
                
                await _accountCostCenterRepository.AddAsync(accountCostCenter);
                await _accountCostCenterRepository.SaveAsync();
            }
        }

        public async Task RemoveCostCenterAccountAsync(Guid costCenterId, Guid accountId)
        {
            var accountCostCenter = await _accountCostCenterRepository.FindAsync(acc => 
                acc.CostCenterId == costCenterId && acc.AccountId == accountId);
                
            if (accountCostCenter != null)
            {
                _accountCostCenterRepository.Delete(accountCostCenter);
                await _accountCostCenterRepository.SaveAsync();
            }
        }

        public async Task<IEnumerable<CostCenter>> GetCostCentersAsync(bool? isActive = null)
        {
            if (isActive.HasValue)
            {
                return await _costCenterRepository.FindAllAsync(cc => cc.IsActive == isActive.Value);
            }
            return await _costCenterRepository.GetAllAsync();
        }

        public async Task<IEnumerable<SelectListItem>> GetCostCenterSelectListAsync(bool? isActive = null)
        {
            var costCenters = await GetCostCentersAsync(isActive);
            return costCenters.Select(cc => new SelectListItem
            {
                Value = cc.Id.ToString(),
                Text = $"{cc.Code} - {cc.NameEn}"
            }).ToList();
        }

        public async Task<AssignAccountsVm> GetAssignAccountsViewModelAsync(Guid costCenterId, int maxAccounts = 500)
        {
            var accounts = await _accountRepository.GetAllAsync();
            var linkedAccountIds = (await GetCostCenterAccountsAsync(costCenterId))
                .Select(acc => acc.AccountId)
                .ToList();

            var allAccounts = accounts
                .Take(maxAccounts)
                .Select(a => new SelectListItem
                {
                    Value = a.Id.ToString(),
                    Text = $"{a.Code} - {a.NameEn}"
                })
                .ToList();

            return new AssignAccountsVm
            {
                CostCenterId = costCenterId,
                AvailableAccounts = allAccounts.Where(a => !linkedAccountIds.Contains(Guid.Parse(a.Value))).ToList(),
                AssignedAccounts = allAccounts.Where(a => linkedAccountIds.Contains(Guid.Parse(a.Value))).ToList(),
                SelectedIds = linkedAccountIds
            };
        }

        public async Task AssignAccountsToCostCenterAsync(Guid costCenterId, List<Guid> accountIds)
        {
            var currentAccounts = await GetCostCenterAccountsAsync(costCenterId);
            var currentAccountIds = currentAccounts.Select(acc => acc.AccountId).ToList();

            // Remove accounts that are no longer selected
            foreach (var currentAccount in currentAccounts)
            {
                if (!accountIds.Contains(currentAccount.AccountId))
                {
                    await RemoveCostCenterAccountAsync(costCenterId, currentAccount.AccountId);
                }
            }

            // Add new accounts
            foreach (var accountId in accountIds)
            {
                if (!currentAccountIds.Contains(accountId))
                {
                    await AddCostCenterAccountAsync(costCenterId, accountId);
                }
            }
        }
    }
}
