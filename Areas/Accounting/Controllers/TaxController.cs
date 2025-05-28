using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
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
    public class TaxController : Controller
    {
        private readonly ITaxService _taxService;
        private readonly IStringLocalizer<TaxController> _localizer;

        public TaxController(
            ITaxService taxService,
            IStringLocalizer<TaxController> localizer)
        {
            _taxService = taxService;
            _localizer = localizer;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var taxRates = await _taxService.GetAllTaxRatesAsync();
            return View(taxRates);
        }

        [HttpGet]
        public IActionResult Create()
        {
            var viewModel = new TaxRateViewModel
            {
                EffectiveFrom = DateTime.Today,
                IsActive = true
            };
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TaxRateViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                var taxRate = new TaxRate
                {
                    Code = viewModel.Code,
                    NameEn = viewModel.NameEn,
                    NameAr = viewModel.NameAr,
                    Rate = viewModel.Rate,
                    Type = viewModel.Type,
                    Description = viewModel.Description,
                    IsActive = viewModel.IsActive,
                    EffectiveFrom = viewModel.EffectiveFrom,
                    EffectiveTo = viewModel.EffectiveTo
                };

                await _taxService.AddTaxRateAsync(taxRate);
                TempData["SuccessMessage"] = _localizer["Tax rate created successfully."];
                return RedirectToAction(nameof(Index));
            }
            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(Guid id)
        {
            var taxRate = await _taxService.GetTaxRateByIdAsync(id);
            if (taxRate == null)
            {
                return NotFound();
            }

            var viewModel = new TaxRateViewModel
            {
                Id = taxRate.Id,
                Code = taxRate.Code,
                NameEn = taxRate.NameEn,
                NameAr = taxRate.NameAr,
                Rate = taxRate.Rate,
                Type = taxRate.Type,
                Description = taxRate.Description,
                IsActive = taxRate.IsActive,
                EffectiveFrom = taxRate.EffectiveFrom,
                EffectiveTo = taxRate.EffectiveTo
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, TaxRateViewModel viewModel)
        {
            if (id != viewModel.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var taxRate = await _taxService.GetTaxRateByIdAsync(id);
                if (taxRate == null)
                {
                    return NotFound();
                }

                taxRate.Code = viewModel.Code;
                taxRate.NameEn = viewModel.NameEn;
                taxRate.NameAr = viewModel.NameAr;
                taxRate.Rate = viewModel.Rate;
                taxRate.Type = viewModel.Type;
                taxRate.Description = viewModel.Description;
                taxRate.IsActive = viewModel.IsActive;
                taxRate.EffectiveFrom = viewModel.EffectiveFrom;
                taxRate.EffectiveTo = viewModel.EffectiveTo;

                await _taxService.UpdateTaxRateAsync(taxRate);
                TempData["SuccessMessage"] = _localizer["Tax rate updated successfully."];
                return RedirectToAction(nameof(Index));
            }
            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Delete(Guid id)
        {
            var taxRate = await _taxService.GetTaxRateByIdAsync(id);
            if (taxRate == null)
            {
                return NotFound();
            }

            return View(taxRate);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            await _taxService.DeleteTaxRateAsync(id);
            TempData["SuccessMessage"] = _localizer["Tax rate deleted successfully."];
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Calculator()
        {
            var viewModel = new TaxCalculationViewModel
            {
                AvailableTaxRates = (await _taxService.GetActiveTaxRatesAsync())
                    .Select(t => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                    {
                        Value = t.Id.ToString(),
                        Text = $"{t.NameEn} ({t.Rate}%)"
                    }).ToList()
            };
            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Calculator(TaxCalculationViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                var taxRate = await _taxService.GetTaxRateByIdAsync(viewModel.TaxRateId);
                if (taxRate == null)
                {
                    ModelState.AddModelError("TaxRateId", _localizer["Invalid tax rate selected."]);
                }
                else
                {
                    var result = await _taxService.CalculateTaxAsync(viewModel.AmountBeforeTax, taxRate.Id);
                    viewModel.TaxAmount = result.TaxAmount;
                    viewModel.AmountAfterTax = result.AmountAfterTax;
                }
            }

            viewModel.AvailableTaxRates = (await _taxService.GetActiveTaxRatesAsync())
                .Select(t => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                    Value = t.Id.ToString(),
                    Text = $"{t.NameEn} ({t.Rate}%)",
                    Selected = t.Id == viewModel.TaxRateId
                }).ToList();

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> WithholdingTaxes()
        {
            var withholdingTaxes = await _taxService.GetAllWithholdingTaxesAsync();
            return View(withholdingTaxes);
        }

        [HttpGet]
        public IActionResult CreateWithholdingTax()
        {
            var viewModel = new WithholdingTaxViewModel
            {
                EffectiveFrom = DateTime.Today,
                IsActive = true
            };
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateWithholdingTax(WithholdingTaxViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                var withholdingTax = new WithholdingTax
                {
                    Code = viewModel.Code,
                    NameEn = viewModel.NameEn,
                    NameAr = viewModel.NameAr,
                    Rate = viewModel.Rate,
                    Description = viewModel.Description,
                    IsActive = viewModel.IsActive,
                    EffectiveFrom = viewModel.EffectiveFrom,
                    EffectiveTo = viewModel.EffectiveTo,
                    ApplicableVendorTypes = string.Join(",", viewModel.ApplicableVendorTypes),
                    MinimumThreshold = viewModel.MinimumThreshold
                };

                await _taxService.AddWithholdingTaxAsync(withholdingTax);
                TempData["SuccessMessage"] = _localizer["Withholding tax created successfully."];
                return RedirectToAction(nameof(WithholdingTaxes));
            }
            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> EditWithholdingTax(Guid id)
        {
            var withholdingTax = await _taxService.GetWithholdingTaxByIdAsync(id);
            if (withholdingTax == null)
            {
                return NotFound();
            }

            var viewModel = new WithholdingTaxViewModel
            {
                Id = withholdingTax.Id,
                Code = withholdingTax.Code,
                NameEn = withholdingTax.NameEn,
                NameAr = withholdingTax.NameAr,
                Rate = withholdingTax.Rate,
                Description = withholdingTax.Description,
                IsActive = withholdingTax.IsActive,
                EffectiveFrom = withholdingTax.EffectiveFrom,
                EffectiveTo = withholdingTax.EffectiveTo,
                ApplicableVendorTypes = withholdingTax.ApplicableVendorTypes?.Split(',').ToList() ?? new List<string>(),
                MinimumThreshold = withholdingTax.MinimumThreshold
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditWithholdingTax(Guid id, WithholdingTaxViewModel viewModel)
        {
            if (id != viewModel.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var withholdingTax = await _taxService.GetWithholdingTaxByIdAsync(id);
                if (withholdingTax == null)
                {
                    return NotFound();
                }

                withholdingTax.Code = viewModel.Code;
                withholdingTax.NameEn = viewModel.NameEn;
                withholdingTax.NameAr = viewModel.NameAr;
                withholdingTax.Rate = viewModel.Rate;
                withholdingTax.Description = viewModel.Description;
                withholdingTax.IsActive = viewModel.IsActive;
                withholdingTax.EffectiveFrom = viewModel.EffectiveFrom;
                withholdingTax.EffectiveTo = viewModel.EffectiveTo;
                withholdingTax.ApplicableVendorTypes = string.Join(",", viewModel.ApplicableVendorTypes);
                withholdingTax.MinimumThreshold = viewModel.MinimumThreshold;

                await _taxService.UpdateWithholdingTaxAsync(withholdingTax);
                TempData["SuccessMessage"] = _localizer["Withholding tax updated successfully."];
                return RedirectToAction(nameof(WithholdingTaxes));
            }
            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> DeleteWithholdingTax(Guid id)
        {
            var withholdingTax = await _taxService.GetWithholdingTaxByIdAsync(id);
            if (withholdingTax == null)
            {
                return NotFound();
            }

            return View(withholdingTax);
        }

        [HttpPost, ActionName("DeleteWithholdingTax")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteWithholdingTaxConfirmed(Guid id)
        {
            await _taxService.DeleteWithholdingTaxAsync(id);
            TempData["SuccessMessage"] = _localizer["Withholding tax deleted successfully."];
            return RedirectToAction(nameof(WithholdingTaxes));
        }
    }
}
