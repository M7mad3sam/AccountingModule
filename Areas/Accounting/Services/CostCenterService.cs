using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AspNetCoreMvcTemplate.Areas.Accounting.Models;
using AspNetCoreMvcTemplate.Areas.Accounting.Data.Specifications;
using AspNetCoreMvcTemplate.Data.Repository;
using System.Linq;

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
        Task<IEnumerable<CostCenter>> GetCostCentersAsync(bool? isActive = null);
        Task<IEnumerable<Account>> GetAccountsAsync();
        Task<IEnumerable<AccountCostCenter>> GetCostCenterAccountsAsync(Guid costCenterId);
        Task AssignAccountsToCostCenterAsync(Guid costCenterId, List<Guid> accountIds);
    }

    public class CostCenterService : ICostCenterService
    {
        private readonly IRepository<CostCenter> _costCenterRepository;
        private readonly IRepository<JournalEntryLine> _journalEntryLineRepository;
        private readonly IRepository<AccountCostCenter> _accountCostCenterRepository;
        private readonly IRepository<Account> _accountRepository;

        public CostCenterService(
            IRepository<CostCenter> costCenterRepository,
            IRepository<JournalEntryLine> journalEntryLineRepository,
            IRepository<AccountCostCenter> accountCostCenterRepository,
            IRepository<Account> accountRepository)
        {
            _costCenterRepository = costCenterRepository;
            _journalEntryLineRepository = journalEntryLineRepository;
            _accountCostCenterRepository = accountCostCenterRepository;
            _accountRepository = accountRepository;
        }

        public async Task<CostCenter> GetCostCenterByIdAsync(Guid id)
        {
            return await _costCenterRepository.GetByIdAsync(id, cc => cc.Parent);
        }

        public async Task<IEnumerable<CostCenter>> GetAllCostCentersAsync()
        {
            return await _costCenterRepository.GetAllAsync();
        }

        public async Task<IEnumerable<CostCenter>> GetCostCenterHierarchyAsync()
        {
            var spec = new CostCenterHierarchySpecification();
            return await _costCenterRepository.FindAllAsync(spec.Criteria, cc => cc.Children);
        }

        public async Task<CostCenter> CreateCostCenterAsync(CostCenter costCenter)
        {
            // Check for cycles in hierarchy
            if (costCenter.ParentId.HasValue && await WouldCreateCycleAsync(costCenter.Id, costCenter.ParentId.Value))
            {
                throw new InvalidOperationException("Assigning this parent would create a cycle in the cost center hierarchy");
            }

            // Calculate and set level based on parent's level, and check if parent is active
            if (costCenter.ParentId.HasValue)
            {
                var parent = await _costCenterRepository.GetByIdAsync(costCenter.ParentId.Value);
                if (parent != null)
                {
                    if (!parent.IsActive)
                    {
                        throw new InvalidOperationException("Cannot assign an inactive parent to a cost center");
                    }
                    costCenter.Level = parent.Level + 1;
                    if (costCenter.Level > 5)
                    {
                        throw new InvalidOperationException("Cost center hierarchy depth cannot exceed 5 levels");
                    }
                }
            }
            else
            {
                costCenter.Level = 0; // Root level
            }

            await _costCenterRepository.AddAsync(costCenter);
            await _costCenterRepository.SaveAsync();
            return costCenter;
        }

        public async Task UpdateCostCenterAsync(CostCenter costCenter)
        {
            var existingCostCenter = await _costCenterRepository.GetByIdAsync(costCenter.Id);
            if (existingCostCenter == null)
            {
                throw new ArgumentException("Cost center not found");
            }

            // Check if cost center has journal entry lines and code is being changed
            if (existingCostCenter.Code != costCenter.Code)
            {
                var hasJournalEntryLines = await _journalEntryLineRepository.ExistsAsync(jel => jel.CostCenterId == costCenter.Id);
                if (hasJournalEntryLines)
                {
                    throw new InvalidOperationException("Cannot change cost center code when journal entry lines exist");
                }
            }

            // Check for cycles in hierarchy
            if (costCenter.ParentId.HasValue && await WouldCreateCycleAsync(costCenter.Id, costCenter.ParentId.Value))
            {
                throw new InvalidOperationException("Assigning this parent would create a cycle in the cost center hierarchy");
            }

            // Calculate and set level based on parent's level, and check if parent is active
            if (costCenter.ParentId.HasValue)
            {
                var parent = await _costCenterRepository.GetByIdAsync(costCenter.ParentId.Value);
                if (parent != null)
                {
                    if (!parent.IsActive)
                    {
                        throw new InvalidOperationException("Cannot assign an inactive parent to a cost center");
                    }
                    costCenter.Level = parent.Level + 1;
                    if (costCenter.Level > 5)
                    {
                        throw new InvalidOperationException("Cost center hierarchy depth cannot exceed 5 levels");
                    }
                }
            }
            else
            {
                costCenter.Level = 0; // Root level
            }

            // Check if deactivation is attempted and there are open-period postings
            if (existingCostCenter.IsActive && !costCenter.IsActive)
            {
                var hasOpenPeriodPostings = await _journalEntryLineRepository.ExistsAsync(jel => 
                    jel.CostCenterId == costCenter.Id && 
                    jel.JournalEntry != null && 
                    jel.JournalEntry.FiscalPeriod != null && 
                    !jel.JournalEntry.FiscalPeriod.IsClosed);
                if (hasOpenPeriodPostings)
                {
                    throw new InvalidOperationException("Cannot deactivate cost center with open-period postings");
                }
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

            // Check if cost center has account associations
            var hasAccountAssociations = await _accountCostCenterRepository.ExistsAsync(acc => acc.CostCenterId == id);
            if (hasAccountAssociations)
                return false;

            // Check if cost center has children
            var costCenter = await _costCenterRepository.FindAsync(cc => cc.Id == id, cc => cc.Children);
            return costCenter?.Children == null || !costCenter.Children.Any();
        }

        public async Task<IEnumerable<CostCenter>> GetCostCentersAsync(bool? isActive = null)
        {
            if (isActive.HasValue)
            {
                return await _costCenterRepository.FindAllAsync(cc => cc.IsActive == isActive.Value);
            }
            return await _costCenterRepository.GetAllAsync();
        }

        public async Task<IEnumerable<Account>> GetAccountsAsync()
        {
            return await _accountRepository.GetAllAsync();
        }

        public async Task<IEnumerable<AccountCostCenter>> GetCostCenterAccountsAsync(Guid costCenterId)
        {
            return await _accountCostCenterRepository.FindAllAsync(acc => acc.CostCenterId == costCenterId);
        }

        public async Task AssignAccountsToCostCenterAsync(Guid costCenterId, List<Guid> accountIds)
        {
            var transaction = await _accountCostCenterRepository.BeginTransactionAsync();
            try
            {
                // Remove existing links
                var existingLinks = await _accountCostCenterRepository.FindAllAsync(acc => acc.CostCenterId == costCenterId);
                _accountCostCenterRepository.DeleteRange(existingLinks);

                // Add new links
                var newLinks = accountIds.Select(accountId => new AccountCostCenter
                {
                    AccountId = accountId,
                    CostCenterId = costCenterId
                }).ToList();

                await _accountCostCenterRepository.AddRangeAsync(newLinks);
                await _accountCostCenterRepository.SaveAsync();

                await _accountCostCenterRepository.CommitTransactionAsync();
            }
            catch (Exception)
            {
                _accountCostCenterRepository.RollbackTransaction();
                throw;
            }
        }

        private async Task<bool> WouldCreateCycleAsync(Guid costCenterId, Guid newParentId)
        {
            // If the cost center is being assigned as its own parent, it's a direct cycle
            if (costCenterId == newParentId)
            {
                return true;
            }

            // Check if the new parent is a descendant of the cost center
            var visited = new HashSet<Guid>();
            var currentId = newParentId;

            while (currentId != Guid.Empty)
            {
                if (visited.Contains(currentId))
                {
                    return true; // Cycle detected in the hierarchy
                }

                visited.Add(currentId);

                // If we reach the cost center ID while traversing descendants, there's a cycle
                if (currentId == costCenterId)
                {
                    return true;
                }

                var currentCostCenter = await _costCenterRepository.FindAsync(cc => cc.ParentId == currentId, cc => cc.Children);
                if (currentCostCenter == null || !currentCostCenter.Children.Any())
                {
                    break;
                }

                currentId = currentCostCenter.Children.First().Id; // Traverse down the hierarchy
            }

            return false;
        }
    }
}
