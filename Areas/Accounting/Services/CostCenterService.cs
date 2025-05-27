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
    }

    public class CostCenterService : ICostCenterService
    {
        private readonly IRepository<CostCenter> _costCenterRepository;
        private readonly IRepository<JournalEntryLine> _journalEntryLineRepository;
        private readonly IRepository<AccountCostCenter> _accountCostCenterRepository;

        public CostCenterService(
            IRepository<CostCenter> costCenterRepository,
            IRepository<JournalEntryLine> journalEntryLineRepository,
            IRepository<AccountCostCenter> accountCostCenterRepository)
        {
            _costCenterRepository = costCenterRepository;
            _journalEntryLineRepository = journalEntryLineRepository;
            _accountCostCenterRepository = accountCostCenterRepository;
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
            await _costCenterRepository.AddAsync(costCenter);
            await _costCenterRepository.SaveAsync();
            return costCenter;
        }

        public async Task UpdateCostCenterAsync(CostCenter costCenter)
        {
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
    }
}
