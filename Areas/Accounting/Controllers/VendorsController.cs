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

namespace AspNetCoreMvcTemplate.Areas.Accounting.Controllers
{
    [Area("Accounting")]
    public class VendorsController : Controller
    {
        private readonly IClientVendorService _clientVendorService;
        private readonly IChartOfAccountsService _chartOfAccountsService;
        private readonly IStringLocalizer<VendorsController> _localizer;

        public VendorsController(
            IClientVendorService clientVendorService,
            IChartOfAccountsService chartOfAccountsService,
            IStringLocalizer<VendorsController> localizer)
        {
            _clientVendorService = clientVendorService;
            _chartOfAccountsService = chartOfAccountsService;
            _localizer = localizer;
        }
        
        [HttpGet]
        public async Task<IActionResult> Index(string searchTerm = null, string vendorType = null, bool? isActive = null, bool? subjectToWithholdingTax = null)
        {
            var vendors = await _clientVendorService.GetVendorsAsync(searchTerm, vendorType, isActive, subjectToWithholdingTax);
            var vendorTypes = await _clientVendorService.GetVendorTypesAsync();
            if (vendorTypes == null || !vendorTypes.Any())
            {
                vendorTypes = Enum.GetValues(typeof(VendorType)).Cast<VendorType>().Select(vt => vt.ToString()).ToList();
            }
            
            var viewModel = new VendorListViewModel
            {
                Vendors = vendors,
                SearchTerm = searchTerm,
                VendorType = vendorType,
                SearchIsActive = isActive,
                SubjectToWithholdingTax = subjectToWithholdingTax,
                VendorTypes = vendorTypes.Select(vt => new SelectListItem
                {
                    Value = vt,
                    Text = vt
                }).ToList()
            };
            
            return View(viewModel);
        }
        
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var viewModel = new VendorViewModel();
            
            // Get vendor types and accounts for dropdown lists
            var vendorTypes = await _clientVendorService.GetVendorTypesAsync();
            if (vendorTypes == null || !vendorTypes.Any())
            {
                vendorTypes = Enum.GetValues(typeof(VendorType)).Cast<VendorType>().Select(vt => vt.ToString()).ToList();
            }
            var accounts = await _chartOfAccountsService.GetAccountsAsync();
            
            viewModel.AvailableVendorTypes = vendorTypes.Select(vt => new SelectListItem
            {
                Value = vt,
                Text = vt
            }).ToList();
            
            viewModel.AvailableAccounts = accounts.Select(a => new SelectListItem
            {
                Value = a.Id.ToString(),
                Text = $"{a.Code} - {a.NameEn}"
            }).ToList();
            
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
                    Type = viewModel.Type,
                    TaxRegistrationNumber = viewModel.TaxRegistrationNumber,
                    CommercialRegistration = viewModel.CommercialRegistration,
                    ContactPerson = viewModel.ContactPerson,
                    Phone = viewModel.Phone,
                    Email = viewModel.Email,
                    Address = viewModel.Address,
                    PaymentTerms = viewModel.PaymentTerms,
                    IsActive = viewModel.IsActive,
                    SubjectToWithholdingTax = viewModel.SubjectToWithholdingTax,
                    Notes = viewModel.Notes,
                    BankName = viewModel.BankName,
                    BankAccountNumber = viewModel.BankAccountNumber,
                    IBAN = viewModel.IBAN,
                    AccountId = viewModel.AccountId
                };
                
                await _clientVendorService.CreateVendorAsync(vendor);
                
                return RedirectToAction(nameof(Index));
            }
            
            // If we got this far, something failed, redisplay form
            // Get vendor types and accounts for dropdown lists
            var vendorTypes = await _clientVendorService.GetVendorTypesAsync();
            var accounts = await _chartOfAccountsService.GetAccountsAsync();
            
            viewModel.AvailableVendorTypes = vendorTypes.Select(vt => new SelectListItem
            {
                Value = vt,
                Text = vt
            }).ToList();
            
            viewModel.AvailableAccounts = accounts.Select(a => new SelectListItem
            {
                Value = a.Id.ToString(),
                Text = $"{a.Code} - {a.NameEn}"
            }).ToList();
            
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
                Type = vendor.Type,
                TaxRegistrationNumber = vendor.TaxRegistrationNumber,
                CommercialRegistration = vendor.CommercialRegistration,
                ContactPerson = vendor.ContactPerson,
                Phone = vendor.Phone,
                Email = vendor.Email,
                Address = vendor.Address,
                PaymentTerms = vendor.PaymentTerms,
                IsActive = vendor.IsActive,
                SubjectToWithholdingTax = vendor.SubjectToWithholdingTax,
                Notes = vendor.Notes,
                BankName = vendor.BankName,
                BankAccountNumber = vendor.BankAccountNumber,
                IBAN = vendor.IBAN,
                AccountId = vendor.AccountId
            };
            
            // Get vendor types and accounts for dropdown lists
            var vendorTypes = await _clientVendorService.GetVendorTypesAsync();
            var accounts = await _chartOfAccountsService.GetAccountsAsync();
            
            viewModel.AvailableVendorTypes = vendorTypes.Select(vt => new SelectListItem
            {
                Value = vt,
                Text = vt,
                Selected = vt == viewModel.Type.ToString()
            }).ToList();
            
            viewModel.AvailableAccounts = accounts.Select(a => new SelectListItem
            {
                Value = a.Id.ToString(),
                Text = $"{a.Code} - {a.NameEn}",
                Selected = a.Id == viewModel.AccountId
            }).ToList();
            
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
                vendor.Type = viewModel.Type;
                vendor.TaxRegistrationNumber = viewModel.TaxRegistrationNumber;
                vendor.CommercialRegistration = viewModel.CommercialRegistration;
                vendor.ContactPerson = viewModel.ContactPerson;
                vendor.Phone = viewModel.Phone;
                vendor.Email = viewModel.Email;
                vendor.Address = viewModel.Address;
                vendor.PaymentTerms = viewModel.PaymentTerms;
                vendor.IsActive = viewModel.IsActive;
                vendor.SubjectToWithholdingTax = viewModel.SubjectToWithholdingTax;
                vendor.Notes = viewModel.Notes;
                vendor.BankName = viewModel.BankName;
                vendor.BankAccountNumber = viewModel.BankAccountNumber;
                vendor.IBAN = viewModel.IBAN;
                vendor.AccountId = viewModel.AccountId;
                
                await _clientVendorService.UpdateVendorAsync(vendor);
                
                return RedirectToAction(nameof(Index));
            }
            
            // If we got this far, something failed, redisplay form
            // Get vendor types and accounts for dropdown lists
            var vendorTypes = await _clientVendorService.GetVendorTypesAsync();
            var accounts = await _chartOfAccountsService.GetAccountsAsync();
            
            viewModel.AvailableVendorTypes = vendorTypes.Select(vt => new SelectListItem
            {
                Value = vt,
                Text = vt,
                Selected = vt == viewModel.Type.ToString()
            }).ToList();
            
            viewModel.AvailableAccounts = accounts.Select(a => new SelectListItem
            {
                Value = a.Id.ToString(),
                Text = $"{a.Code} - {a.NameEn}",
                Selected = a.Id == viewModel.AccountId
            }).ToList();
            
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
            
            var viewModel = new VendorViewModel
            {
                Id = vendor.Id,
                Code = vendor.Code,
                NameEn = vendor.NameEn,
                NameAr = vendor.NameAr,
                Type = vendor.Type,
                TaxRegistrationNumber = vendor.TaxRegistrationNumber,
                CommercialRegistration = vendor.CommercialRegistration,
                ContactPerson = vendor.ContactPerson,
                Phone = vendor.Phone,
                Email = vendor.Email,
                Address = vendor.Address,
                PaymentTerms = vendor.PaymentTerms,
                IsActive = vendor.IsActive,
                SubjectToWithholdingTax = vendor.SubjectToWithholdingTax,
                Notes = vendor.Notes,
                BankName = vendor.BankName,
                BankAccountNumber = vendor.BankAccountNumber,
                IBAN = vendor.IBAN,
                AccountId = vendor.AccountId
            };
            
            return View(viewModel);
        }
        
        [HttpGet]
        public async Task<IActionResult> Delete(Guid id)
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
                Type = vendor.Type,
                TaxRegistrationNumber = vendor.TaxRegistrationNumber,
                CommercialRegistration = vendor.CommercialRegistration,
                ContactPerson = vendor.ContactPerson,
                Phone = vendor.Phone,
                Email = vendor.Email,
                Address = vendor.Address,
                PaymentTerms = vendor.PaymentTerms,
                IsActive = vendor.IsActive,
                SubjectToWithholdingTax = vendor.SubjectToWithholdingTax,
                Notes = vendor.Notes,
                BankName = vendor.BankName,
                BankAccountNumber = vendor.BankAccountNumber,
                IBAN = vendor.IBAN,
                AccountId = vendor.AccountId
            };
            
            return View(viewModel);
        }
        
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var vendor = await _clientVendorService.GetVendorByIdAsync(id);
            
            if (vendor == null)
            {
                return NotFound();
            }
            
            await _clientVendorService.DeleteVendorAsync(vendor.Id);
            
            return RedirectToAction(nameof(Index));
        }
    }
}
