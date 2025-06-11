using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Localization;
using AspNetCoreMvcTemplate.Areas.Accounting.Models;
using AspNetCoreMvcTemplate.Areas.Accounting.Services;
using AspNetCoreMvcTemplate.Areas.Accounting.ViewModels;
using AspNetCoreMvcTemplate.Resources;
using Microsoft.AspNetCore.Authorization;

namespace AspNetCoreMvcTemplate.Areas.Accounting.Controllers
{
    [Area("Accounting")]
    [Authorize(Roles = "Admin,Accountant,Manager,Auditor")]
    public class CostCentersController : Controller
    {
        private readonly ICostCenterService _costCenterService;
        private readonly IStringLocalizer<SharedResource> _localizer;

        public CostCentersController(
            ICostCenterService costCenterService,
            IStringLocalizer<SharedResource> localizer)
        {
            _costCenterService = costCenterService;
            _localizer = localizer;
        }

        // GET: Accounting/CostCenters
        public async Task<IActionResult> Index(bool? isActive)
        {
            var costCenters = await _costCenterService.GetCostCentersAsync(isActive);
            
            ViewBag.IsActive = isActive;
            ViewBag.ActiveFilter = new List<SelectListItem>
            {
                new SelectListItem { Value = "", Text = _localizer["All"], Selected = !isActive.HasValue },
                new SelectListItem { Value = "true", Text = _localizer["Active"], Selected = isActive == true },
                new SelectListItem { Value = "false", Text = _localizer["Inactive"], Selected = isActive == false }
            };
            
            return View(costCenters);
        }

        // GET: Accounting/CostCenters/Details/5
        public async Task<IActionResult> Details(Guid id)
        {
            var costCenter = await _costCenterService.GetCostCenterByIdAsync(id);
            if (costCenter == null)
            {
                return NotFound();
            }

            return View(costCenter);
        }

        // GET: Accounting/CostCenters/Create
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Create()
        {
            await PopulateParentDropdown();
            return View();
        }

        // POST: Accounting/CostCenters/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Create(CostCenter costCenter)
        {
            // Check for unique code
            if (!await _costCenterService.IsCostCenterCodeUniqueAsync(costCenter.Code))
            {
                ModelState.AddModelError("Code", _localizer["CodeAlreadyExists"]);
            }

            // Check for cycles in hierarchy
            if (costCenter.Id == costCenter.ParentId)
            {
                ModelState.AddModelError("ParentId", _localizer["CostCenterCannotBeItsOwnParent"]);
            }

            if (ModelState.IsValid)
            {
                costCenter.Id = Guid.NewGuid();
                await _costCenterService.CreateCostCenterAsync(costCenter);
                return RedirectToAction(nameof(Index));
            }

            await PopulateParentDropdown(costCenter.ParentId);
            return View(costCenter);
        }

        // GET: Accounting/CostCenters/Edit/5
        [Authorize(Roles = "Admin,Manager,Accountant")]
        public async Task<IActionResult> Edit(Guid id)
        {
            var costCenter = await _costCenterService.GetCostCenterByIdAsync(id);
            if (costCenter == null)
            {
                return NotFound();
            }

            // Check if cost center has journal entries (for UI to disable code field)
            var hasJournalEntries = costCenter.JournalEntryLines?.Any() ?? false;
            ViewBag.HasJournalEntries = hasJournalEntries;

            await PopulateParentDropdown(costCenter.ParentId, costCenter.Id);
            return View(costCenter);
        }

        // POST: Accounting/CostCenters/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Manager,Accountant")]
        public async Task<IActionResult> Edit(Guid id, CostCenter costCenter)
        {
            if (id != costCenter.Id)
            {
                return NotFound();
            }

            // Check for unique code
            if (!await _costCenterService.IsCostCenterCodeUniqueAsync(costCenter.Code, costCenter.Id))
            {
                ModelState.AddModelError("Code", _localizer["CodeAlreadyExists"]);
            }

            // Check for cycles in hierarchy
            if (costCenter.Id == costCenter.ParentId)
            {
                ModelState.AddModelError("ParentId", _localizer["CostCenterCannotBeItsOwnParent"]);
            }

            // Get existing cost center to check if it has journal entries
            var existingCostCenter = await _costCenterService.GetCostCenterByIdAsync(id);
            var hasJournalEntries = existingCostCenter.JournalEntryLines?.Any() ?? false;
            ViewBag.HasJournalEntries = hasJournalEntries;

            // If it has journal entries, code cannot be changed
            if (hasJournalEntries && existingCostCenter.Code != costCenter.Code)
            {
                ModelState.AddModelError("Code", _localizer["CodeCannotBeChangedAfterPostings"]);
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Preserve values that shouldn't be changed by the form
                    if (hasJournalEntries)
                    {
                        costCenter.Code = existingCostCenter.Code;
                    }

                    await _costCenterService.UpdateCostCenterAsync(costCenter);
                }
                catch (Exception)
                {
                    if (!await CostCenterExists(costCenter.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }

            await PopulateParentDropdown(costCenter.ParentId, costCenter.Id);
            return View(costCenter);
        }

        // GET: Accounting/CostCenters/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var costCenter = await _costCenterService.GetCostCenterByIdAsync(id);
            if (costCenter == null)
            {
                return NotFound();
            }

            // Check if cost center can be deleted
            ViewBag.CanDelete = await _costCenterService.CanDeleteCostCenterAsync(id);

            return View(costCenter);
        }

        // POST: Accounting/CostCenters/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            // Check if cost center can be deleted
            if (!await _costCenterService.CanDeleteCostCenterAsync(id))
            {
                return RedirectToAction(nameof(Index));
            }

            await _costCenterService.DeleteCostCenterAsync(id);
            return RedirectToAction(nameof(Index));
        }

        // GET: Accounting/CostCenters/AssignAccounts/5
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> AssignAccounts(Guid id)
        {
            var costCenter = await _costCenterService.GetCostCenterByIdAsync(id);
            if (costCenter == null)
            {
                return NotFound();
            }

            var accounts = await _costCenterService.GetAccountsAsync();
            var linkedAccountIds = (await _costCenterService.GetCostCenterAccountsAsync(id))
                .Select(acc => acc.AccountId)
                .ToList();

            var allAccounts = accounts
                .Take(500)
                .Select(a => new SelectListItem
                {
                    Value = a.Id.ToString(),
                    Text = $"{a.Code} - {a.NameEn}"
                })
                .ToList();

            var viewModel = new AssignAccountsVm
            {
                CostCenterId = id,
                AvailableAccounts = allAccounts.Where(a => !linkedAccountIds.Contains(Guid.Parse(a.Value))).ToList(),
                AssignedAccounts = allAccounts.Where(a => linkedAccountIds.Contains(Guid.Parse(a.Value))).ToList(),
                SelectedIds = linkedAccountIds
            };

            return View("AssignAccounts", viewModel);
        }

        // POST: Accounting/CostCenters/AssignAccounts/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> AssignAccounts(Guid id, List<Guid> selectedIds)
        {
            var costCenter = await _costCenterService.GetCostCenterByIdAsync(id);
            if (costCenter == null)
            {
                return NotFound();
            }

            if (selectedIds == null)
            {
                selectedIds = new List<Guid>();
            }

            try
            {
                await _costCenterService.AssignAccountsToCostCenterAsync(id, selectedIds);
                return Json(new { success = true, message = _localizer["Accounts assigned successfully"] });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = _localizer["Error assigning accounts"] + ": " + ex.Message });
            }
        }

        // Helper methods
        private async Task<bool> CostCenterExists(Guid id)
        {
            var costCenter = await _costCenterService.GetCostCenterByIdAsync(id);
            return costCenter != null;
        }

        private async Task PopulateParentDropdown(Guid? selectedId = null, Guid? excludeId = null)
        {
            var costCenters = await _costCenterService.GetAllCostCentersAsync();
            
            // Exclude the current cost center from the parent dropdown to prevent cycles
            if (excludeId.HasValue)
            {
                costCenters = costCenters.Where(cc => cc.Id != excludeId.Value);
            }

            var isRtl = System.Threading.Thread.CurrentThread.CurrentUICulture.TextInfo.IsRightToLeft;
            
            ViewBag.ParentId = new SelectList(
                costCenters.Select(cc => new
                {
                    Id = cc.Id,
                    Name = isRtl ? cc.NameAr : cc.NameEn
                }),
                "Id",
                "Name",
                selectedId
            );
        }
    }
}
