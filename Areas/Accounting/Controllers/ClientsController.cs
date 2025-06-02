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
    public class ClientsController : Controller
    {
        private readonly IClientVendorService _clientVendorService;
        private readonly IChartOfAccountsService _chartOfAccountsService;
        private readonly IStringLocalizer<ClientsController> _localizer;

        public ClientsController(
            IClientVendorService clientVendorService,
            IChartOfAccountsService chartOfAccountsService,
            IStringLocalizer<ClientsController> localizer)
        {
            _clientVendorService = clientVendorService;
            _chartOfAccountsService = chartOfAccountsService;
            _localizer = localizer;
        }
        
        [HttpGet]
        public async Task<IActionResult> Index(string searchTerm = null, ClientType? clientType = null, bool? isActive = null)
        {
            var clients = await _clientVendorService.GetClientsAsync(searchTerm, clientType, isActive);
            
            // Create SelectListItems for ClientType enum
            var clientTypeItems = Enum.GetValues(typeof(ClientType))
                .Cast<ClientType>()
                .Select(c => new SelectListItem
                {
                    Value = c.ToString(),
                    Text = c.ToString()
                }).ToList();
            
            clientTypeItems.Insert(0, new SelectListItem { Value = "", Text = _localizer["All"] });
            
            var viewModel = new ClientListViewModel
            {
                Clients = clients,
                SearchTerm = searchTerm,
                SearchClientType = clientType,
                SearchIsActive = isActive,
                ClientTypes = clientTypeItems
            };
            
            return View(viewModel);
        }
        
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var viewModel = new ClientViewModel();
            
            // Create SelectListItems for ClientType enum
            viewModel.AvailableClientTypes = Enum.GetValues(typeof(ClientType))
                .Cast<ClientType>()
                .Select(c => new SelectListItem
                {
                    Value = ((int)c).ToString(),
                    Text = c.ToString()
                }).ToList();
            
            // Populate available accounts
            var accounts = await _chartOfAccountsService.GetAllAccountsAsync();
                viewModel.AvailableAccounts = accounts.Select(a => new SelectListItem
                {
                    Value = a.Id.ToString(),
                    Text = $"{a.Code} - {a.NameEn}"
                }).ToList();
            
            return View(viewModel);
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ClientViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                var client = new Client
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
                    CreditLimit = viewModel.CreditLimit,
                    CreditPeriod = viewModel.CreditPeriod,
                    IsActive = viewModel.IsActive,
                    ClientType = viewModel.ClientType,
                    Notes = viewModel.Notes,
                    AccountId = viewModel.AccountId
                };
                
                await _clientVendorService.CreateClientAsync(client);
                
                return RedirectToAction(nameof(Index));
            }
            
            // If we got this far, something failed, redisplay form
            viewModel.AvailableClientTypes = Enum.GetValues(typeof(ClientType))
                .Cast<ClientType>()
                .Select(c => new SelectListItem
                {
                    Value = ((int)c).ToString(),
                    Text = c.ToString()
                }).ToList();
            
            // Repopulate accounts
            var accounts = await _chartOfAccountsService.GetAllAccountsAsync();
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
            var client = await _clientVendorService.GetClientByIdAsync(id);
            
            if (client == null)
            {
                return NotFound();
            }
            
            var viewModel = new ClientViewModel
            {
                Id = client.Id,
                Code = client.Code,
                NameEn = client.NameEn,
                NameAr = client.NameAr,
                TaxRegistrationNumber = client.TaxRegistrationNumber,
                CommercialRegistration = client.CommercialRegistration,
                ContactPerson = client.ContactPerson,
                Phone = client.Phone,
                Email = client.Email,
                Address = client.Address,
                CreditLimit = client.CreditLimit,
                CreditPeriod = client.CreditPeriod,
                IsActive = client.IsActive,
                ClientType = client.ClientType,
                Notes = client.Notes,
                AccountId = client.AccountId
            };
            
            // Create SelectListItems for ClientType enum
            viewModel.AvailableClientTypes = Enum.GetValues(typeof(ClientType))
                .Cast<ClientType>()
                .Select(c => new SelectListItem
                {
                    Value = ((int)c).ToString(),
                    Text = c.ToString(),
                    Selected = c == client.ClientType
                }).ToList();
            
            // Populate available accounts
            var accounts = await _chartOfAccountsService.GetAllAccountsAsync();
            viewModel.AvailableAccounts = accounts.Select(a => new SelectListItem
            {
                Value = a.Id.ToString(),
                Text = $"{a.Code} - {a.NameEn}",
                Selected = a.Id == client.AccountId
            }).ToList();
            
            return View(viewModel);
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, ClientViewModel viewModel)
        {
            if (id != viewModel.Id)
            {
                return NotFound();
            }
            
            if (ModelState.IsValid)
            {
                var client = await _clientVendorService.GetClientByIdAsync(id);
                
                if (client == null)
                {
                    return NotFound();
                }
                
                client.Code = viewModel.Code;
                client.NameEn = viewModel.NameEn;
                client.NameAr = viewModel.NameAr;
                client.TaxRegistrationNumber = viewModel.TaxRegistrationNumber;
                client.CommercialRegistration = viewModel.CommercialRegistration;
                client.ContactPerson = viewModel.ContactPerson;
                client.Phone = viewModel.Phone;
                client.Email = viewModel.Email;
                client.Address = viewModel.Address;
                client.CreditLimit = viewModel.CreditLimit;
                client.CreditPeriod = viewModel.CreditPeriod;
                client.IsActive = viewModel.IsActive;
                client.ClientType = viewModel.ClientType;
                client.Notes = viewModel.Notes;
                client.AccountId = viewModel.AccountId;
                
                await _clientVendorService.UpdateClientAsync(client);
                
                return RedirectToAction(nameof(Index));
            }
            
            // If we got this far, something failed, redisplay form
            viewModel.AvailableClientTypes = Enum.GetValues(typeof(ClientType))
                .Cast<ClientType>()
                .Select(c => new SelectListItem
                {
                    Value = ((int)c).ToString(),
                    Text = c.ToString(),
                    Selected = c == viewModel.ClientType
                }).ToList();
            
            // Repopulate accounts
            var accounts = await _chartOfAccountsService.GetAllAccountsAsync();
            viewModel.AvailableAccounts = accounts.Select(a => new SelectListItem
            {
                Value = a.Id.ToString(),
                Text = $"{a.Code} - {a.NameEn}"
            }).ToList();
            
            return View(viewModel);
        }
        
        [HttpGet]
        public async Task<IActionResult> Details(Guid id)
        {
            var client = await _clientVendorService.GetClientByIdAsync(id);
            
            if (client == null)
            {
                return NotFound();
            }
            
            var viewModel = new ClientViewModel
            {
                Id = client.Id,
                Code = client.Code,
                NameEn = client.NameEn,
                NameAr = client.NameAr,
                TaxRegistrationNumber = client.TaxRegistrationNumber,
                CommercialRegistration = client.CommercialRegistration,
                ContactPerson = client.ContactPerson,
                Phone = client.Phone,
                Email = client.Email,
                Address = client.Address,
                CreditLimit = client.CreditLimit,
                CreditPeriod = client.CreditPeriod,
                IsActive = client.IsActive,
                ClientType = client.ClientType,
                Notes = client.Notes
            };
            
            return View(viewModel);
        }
        
        [HttpGet]
        public async Task<IActionResult> Delete(Guid id)
        {
            var client = await _clientVendorService.GetClientByIdAsync(id);
            
            if (client == null)
            {
                return NotFound();
            }
            
            var viewModel = new ClientViewModel
            {
                Id = client.Id,
                Code = client.Code,
                NameEn = client.NameEn,
                NameAr = client.NameAr,
                TaxRegistrationNumber = client.TaxRegistrationNumber,
                CommercialRegistration = client.CommercialRegistration,
                ContactPerson = client.ContactPerson,
                Phone = client.Phone,
                Email = client.Email,
                Address = client.Address,
                CreditLimit = client.CreditLimit,
                CreditPeriod = client.CreditPeriod,
                IsActive = client.IsActive,
                ClientType = client.ClientType,
                Notes = client.Notes
            };
            
            return View(viewModel);
        }
        
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var client = await _clientVendorService.GetClientByIdAsync(id);
            
            if (client == null)
            {
                return NotFound();
            }
            
            await _clientVendorService.DeleteClientAsync(client.Id);
            
            return RedirectToAction(nameof(Index));
        }
    }
}
