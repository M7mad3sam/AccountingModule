using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using AspNetCoreMvcTemplate.Areas.Accounting.Services;
using AspNetCoreMvcTemplate.Areas.Accounting.Data.Specifications;
using AspNetCoreMvcTemplate.Data.Repository;
using AspNetCoreMvcTemplate.Areas.Accounting.Models;

namespace AspNetCoreMvcTemplate.Areas.Accounting
{
    public static class AccountingModuleExtensions
    {
        public static IServiceCollection AddAccountingModule(this IServiceCollection services)
        {
            // Register repositories for all accounting models
            services.AddScoped(typeof(IRepository<Account>), typeof(Repository<Account>));
            services.AddScoped(typeof(IRepository<CostCenter>), typeof(Repository<CostCenter>));
            services.AddScoped(typeof(IRepository<AccountCostCenter>), typeof(Repository<AccountCostCenter>));
            services.AddScoped(typeof(IRepository<FiscalYear>), typeof(Repository<FiscalYear>));
            services.AddScoped(typeof(IRepository<FiscalPeriod>), typeof(Repository<FiscalPeriod>));
            services.AddScoped(typeof(IRepository<JournalEntry>), typeof(Repository<JournalEntry>));
            services.AddScoped(typeof(IRepository<JournalEntryLine>), typeof(Repository<JournalEntryLine>));
            services.AddScoped(typeof(IRepository<TaxRate>), typeof(Repository<TaxRate>));
            services.AddScoped(typeof(IRepository<Client>), typeof(Repository<Client>));
            services.AddScoped(typeof(IRepository<Vendor>), typeof(Repository<Vendor>));
            services.AddScoped(typeof(IRepository<AuditLog>), typeof(Repository<AuditLog>));

            // Register services
            services.AddScoped<IChartOfAccountsService, ChartOfAccountsService>();
            services.AddScoped<ICostCenterService, CostCenterService>();
            services.AddScoped<IGeneralLedgerService, GeneralLedgerService>();
            services.AddScoped<IPeriodManagementService, PeriodManagementService>();
            services.AddScoped<ITaxService, TaxService>();
            services.AddScoped<IClientVendorService, ClientVendorService>();
            services.AddScoped<IAuditService, AuditService>();
            services.AddScoped<IFinancialReportingService, FinancialReportingService>();

            return services;
        }
    }
}
