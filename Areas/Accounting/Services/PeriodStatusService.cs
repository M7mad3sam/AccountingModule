using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AspNetCoreMvcTemplate.Areas.Accounting.Models;
using AspNetCoreMvcTemplate.Data.Repository;
using System.Linq;
using AspNetCoreMvcTemplate.Areas.Accounting.Data.Specifications;

namespace AspNetCoreMvcTemplate.Areas.Accounting.Services
{
    /// <summary>
    /// Service for checking period status without creating circular dependencies
    /// </summary>
    public class PeriodStatusService : IPeriodValidator
    {
        private readonly IRepository<FiscalPeriod> _fiscalPeriodRepository;

        public PeriodStatusService(IRepository<FiscalPeriod> fiscalPeriodRepository)
        {
            _fiscalPeriodRepository = fiscalPeriodRepository;
        }

        /// <summary>
        /// Checks if a fiscal period is closed for a given date
        /// </summary>
        /// <param name="date">The date to check</param>
        /// <returns>True if the period is closed, false otherwise</returns>
        public async Task<bool> IsPeriodClosedAsync(DateTime date)
        {
            // Find the fiscal period that contains this date
            var specification = new Specification<FiscalPeriod>(p => 
                p.StartDate <= date && p.EndDate >= date);
                
            var fiscalPeriod = (await _fiscalPeriodRepository.FindAllAsync(specification)).FirstOrDefault();
            
            // If no period is defined for this date, consider it closed
            if (fiscalPeriod == null)
            {
                return true;
            }
            
            // Return the IsClosed status
            return fiscalPeriod.IsClosed;
        }
    }
}
