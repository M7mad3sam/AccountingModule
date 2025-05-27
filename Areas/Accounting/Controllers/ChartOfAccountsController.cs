using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Localization;
using AspNetCoreMvcTemplate.Areas.Accounting.Models;
using AspNetCoreMvcTemplate.Areas.Accounting.Services;
using AspNetCoreMvcTemplate.Areas.Accounting.ViewModels;
using System.Linq;

namespace AspNetCoreMvcTemplate.Areas.Accounting.Controllers
{
    [Area("Accounting")]
    [Authorize(Roles = "Admin,Accountant")]
    public class ChartOfAccountsController : Controller
    {
        private readonly IChartOfAccountsService _chartOfAccountsService;
        private readonly ICostCenterService _costCenterService;
        private readonly IStringLocalizer<ChartOfAccountsController> _localizer;

        public ChartOfAccountsController(
            IChartOfAccountsService chartOfAccountsService,
            ICostCenterService costCenterService,
            IStringLocalizer<ChartOfAccountsController> localizer)
        {
            _chartOfAccountsService = chartOfAccountsService;
            _costCenterService = costCenterService;
            _localizer = localizer;
        }

        public async Task<IActionResult> Index()
        {
            var accounts = await _chartOfAccountsService.GetAccountHierarchyAsync();
            return View(accounts);
        }

        public async Task<IActionResult> Details(Guid id)
        {
            var account = await _chartOfAccountsService.GetAccountByIdAsync(id);
            if (account == null)
                return NotFound();

            return View(account);
        }

        public async Task<IActionResult> Create()
        {
            var viewModel = new AccountViewModel
            {
                IsActive = true,
                AccountTypes = GetAccountTypeSelectList(),
                ParentAccounts = await GetParentAccountSelectListAsync()
            };
            
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AccountViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                // Check if code is unique
                if (!await _chartOfAccountsService.IsAccountCodeUniqueAsync(viewModel.Code))
                {
                    ModelState.AddModelError("Code", _localizer["Account code must be unique"]);
                    viewModel.AccountTypes = GetAccountTypeSelectList();
                    viewModel.ParentAccounts = await GetParentAccountSelectListAsync();
                    return View(viewModel);
                }

                var account = new Account
                {
                    Code = viewModel.Code,
                    NameEn = viewModel.NameEn,
                    NameAr = viewModel.NameAr,
                    Type = viewModel.Type,
                    ParentId = viewModel.ParentId,
                    IsActive = viewModel.IsActive
                };

                await _chartOfAccountsService.CreateAccountAsync(account);
                return RedirectToAction(nameof(Index));
            }

            viewModel.AccountTypes = GetAccountTypeSelectList();
            viewModel.ParentAccounts = await GetParentAccountSelectListAsync();
            return View(viewModel);
        }

        public async Task<IActionResult> Edit(Guid id)
        {
            var account = await _chartOfAccountsService.GetAccountByIdAsync(id);
            if (account == null)
                return NotFound();

            var viewModel = new AccountViewModel
            {
                Id = account.Id,
                Code = account.Code,
                NameEn = account.NameEn,
                NameAr = account.NameAr,
                Type = account.Type,
                ParentId = account.ParentId,
                IsActive = account.IsActive,
                AccountTypes = GetAccountTypeSelectList(),
                ParentAccounts = await GetParentAccountSelectListAsync(id)
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, AccountViewModel viewModel)
        {
            if (id != viewModel.Id)
                return NotFound();

            if (ModelState.IsValid)
            {
                // Check if code is unique
                if (!await _chartOfAccountsService.IsAccountCodeUniqueAsync(viewModel.Code, viewModel.Id))
                {
                    ModelState.AddModelError("Code", _localizer["Account code must be unique"]);
                    viewModel.AccountTypes = GetAccountTypeSelectList();
                    viewModel.ParentAccounts = await GetParentAccountSelectListAsync(id);
                    return View(viewModel);
                }

                var account = await _chartOfAccountsService.GetAccountByIdAsync(id);
                if (account == null)
                    return NotFound();

                account.Code = viewModel.Code;
                account.NameEn = viewModel.NameEn;
                account.NameAr = viewModel.NameAr;
                account.Type = viewModel.Type;
                account.ParentId = viewModel.ParentId;
                account.IsActive = viewModel.IsActive;

                await _chartOfAccountsService.UpdateAccountAsync(account);
                return RedirectToAction(nameof(Index));
            }

            viewModel.AccountTypes = GetAccountTypeSelectList();
            viewModel.ParentAccounts = await GetParentAccountSelectListAsync(id);
            return View(viewModel);
        }

        public async Task<IActionResult> Delete(Guid id)
        {
            var account = await _chartOfAccountsService.GetAccountByIdAsync(id);
            if (account == null)
                return NotFound();

            return View(account);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var canDelete = await _chartOfAccountsService.CanDeleteAccountAsync(id);
            if (!canDelete)
            {
                return RedirectToAction(nameof(Index), new { error = _localizer["Cannot delete account with postings or children"] });
            }

            await _chartOfAccountsService.DeleteAccountAsync(id);
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> CostCenters(Guid id)
        {
            var account = await _chartOfAccountsService.GetAccountByIdAsync(id);
            if (account == null)
                return NotFound();

            var accountCostCenters = await _chartOfAccountsService.GetAccountCostCentersAsync(id);
            var allCostCenters = await _costCenterService.GetAllCostCentersAsync();
            
            var viewModel = new AccountCostCentersViewModel
            {
                AccountId = id,
                AccountName = account.NameEn,
                AccountCostCenters = accountCostCenters,
                AvailableCostCenters = allCostCenters.Where(cc => 
                    !accountCostCenters.Any(acc => acc.CostCenterId == cc.Id)).ToList()
            };
            
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddCostCenter(Guid accountId, Guid costCenterId)
        {
            await _chartOfAccountsService.AddAccountCostCenterAsync(accountId, costCenterId);
            return RedirectToAction(nameof(CostCenters), new { id = accountId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveCostCenter(Guid accountId, Guid costCenterId)
        {
            await _chartOfAccountsService.RemoveAccountCostCenterAsync(accountId, costCenterId);
            return RedirectToAction(nameof(CostCenters), new { id = accountId });
        }

        private IEnumerable<SelectListItem> GetAccountTypeSelectList()
        {
            return Enum.GetValues(typeof(AccountType))
                .Cast<AccountType>()
                .Select(t => new SelectListItem
                {
                    Value = ((int)t).ToString(),
                    Text = t.ToString()
                });
        }

        private async Task<IEnumerable<SelectListItem>> GetParentAccountSelectListAsync(Guid? excludeId = null)
        {
            var accounts = await _chartOfAccountsService.GetAllAccountsAsync();
            var filteredAccounts = excludeId.HasValue 
                ? accounts.Where(a => a.Id != excludeId.Value) 
                : accounts;
                
            return filteredAccounts
                .OrderBy(a => a.Code)
                .Select(a => new SelectListItem
                {
                    Value = a.Id.ToString(),
                    Text = $"{a.Code} - {a.NameEn}"
                });
        }
    }
}
