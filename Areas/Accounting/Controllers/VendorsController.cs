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
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AspNetCoreMvcTemplate.Areas.Accounting.Controllers
{
    [Area("Accounting")]
    [Authorize(Roles = "Admin,Accountant")]
    public class VendorsController : Controller
    {
        private readonly IClientVendorService _clientVendorService;
        private readonly IStringLocalizer<VendorsController> _localizer;

        public VendorsController(
            IClientVendorService clientVendorService,
            IStringLocalizer<VendorsController> localizer)
        {
            _clientVendorService = clientVendorService;
            _localizer = localizer;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string searchTerm = null, string vendorType = null, bool? isActive = null, bool? subjectToWithholdingTax = null)
        {
            var vendors = await _clientVendorService.GetVendorsAsync(searchTerm, vendorType, isActive, subjectToWithholdingTax);
            var vendorTypes = await _clientVendorService.GetVendorTypesAsync();
            
            var viewModel = new VendorListViewModel
            {
                Vendors = vendors,
                SearchTerm = searchTerm,
                VendorType = vendorType,
                IsActive = isActive,
                SubjectToWithholdingTax = subjectToWithholdingTax,
                VendorTypes = vendorTypes.Select(t => new SelectListItem { Value = t, Text = t })
            };
            
            return View(viewModel);
        }

        [HttpGet]
        public IActionResult Create()
        {
            var viewModel = new VendorViewModel
            {
                IsActive = true
            };
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(VendorViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                var vendor = new Vendor
                {
                    Code = viewModel.Code,
                    NameEn = viewModel.NameEn,
                    NameAr = viewModel.NameAr,
                    TaxRegistrationNumber = viewModel.TaxRegistrationNumber,
                    CommercialRegistration = viewModel.CommercialRegistration,
                    ContactPerson = viewModel.ContactPerson,
                    Phone = viewModel.Phone,
                    Email = viewModel.Email,
                    Address = viewModel.Address,
                    PaymentTerms = viewModel.PaymentTerms,
                    IsActive = viewModel.IsActive,
                    VendorType = viewModel.VendorType,
                    SubjectToWithholdingTax = viewModel.SubjectToWithholdingTax,
                    Notes = viewModel.Notes,
                    BankName = viewModel.BankName,
                    BankAccountNumber = viewModel.BankAccountNumber,
                    IBAN = viewModel.IBAN
                };

                await _clientVendorService.AddVendorAsync(vendor);
                TempData["SuccessMessage"] = _localizer["Vendor created successfully."];
                return RedirectToAction(nameof(Index));
            }
            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(Guid id)
        {
            var vendor = await _clientVendorService.GetVendorByIdAsync(id);
            if (vendor == null)
            {
                return NotFound();
            }

            var viewModel = new VendorViewModel
            {
                Id = vendor.Id,
                Code = vendor.Code,
                NameEn = vendor.NameEn,
                NameAr = vendor.NameAr,
                TaxRegistrationNumber = vendor.TaxRegistrationNumber,
                CommercialRegistration = vendor.CommercialRegistration,
                ContactPerson = vendor.ContactPerson,
                Phone = vendor.Phone,
                Email = vendor.Email,
                Address = vendor.Address,
                PaymentTerms = vendor.PaymentTerms,
                IsActive = vendor.IsActive,
                VendorType = vendor.VendorType,
                SubjectToWithholdingTax = vendor.SubjectToWithholdingTax,
                Notes = vendor.Notes,
                BankName = vendor.BankName,
                BankAccountNumber = vendor.BankAccountNumber,
                IBAN = vendor.IBAN
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, VendorViewModel viewModel)
        {
            if (id != viewModel.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var vendor = await _clientVendorService.GetVendorByIdAsync(id);
                if (vendor == null)
                {
                    return NotFound();
                }

                vendor.Code = viewModel.Code;
                vendor.NameEn = viewModel.NameEn;
                vendor.NameAr = viewModel.NameAr;
                vendor.TaxRegistrationNumber = viewModel.TaxRegistrationNumber;
                vendor.CommercialRegistration = viewModel.CommercialRegistration;
                vendor.ContactPerson = viewModel.ContactPerson;
                vendor.Phone = viewModel.Phone;
                vendor.Email = viewModel.Email;
                vendor.Address = viewModel.Address;
                vendor.PaymentTerms = viewModel.PaymentTerms;
                vendor.IsActive = viewModel.IsActive;
                vendor.VendorType = viewModel.VendorType;
                vendor.SubjectToWithholdingTax = viewModel.SubjectToWithholdingTax;
                vendor.Notes = viewModel.Notes;
                vendor.BankName = viewModel.BankName;
                vendor.BankAccountNumber = viewModel.BankAccountNumber;
                vendor.IBAN = viewModel.IBAN;

                await _clientVendorService.UpdateVendorAsync(vendor);
                TempData["SuccessMessage"] = _localizer["Vendor updated successfully."];
                return RedirectToAction(nameof(Index));
            }
            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Details(Guid id)
        {
            var vendor = await _clientVendorService.GetVendorByIdAsync(id);
            if (vendor == null)
            {
                return NotFound();
            }

            return View(vendor);
        }

        [HttpGet]
        public async Task<IActionResult> Delete(Guid id)
        {
            var vendor = await _clientVendorService.GetVendorByIdAsync(id);
            if (vendor == null)
            {
                return NotFound();
            }

            return View(vendor);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            try
            {
                await _clientVendorService.DeleteVendorAsync(id);
                TempData["SuccessMessage"] = _localizer["Vendor deleted successfully."];
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }
            
            return RedirectToAction(nameof(Index));
        }
    }
}
