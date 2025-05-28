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
    public class ClientsController : Controller
    {
        private readonly IClientVendorService _clientVendorService;
        private readonly IStringLocalizer<ClientsController> _localizer;

        public ClientsController(
            IClientVendorService clientVendorService,
            IStringLocalizer<ClientsController> localizer)
        {
            _clientVendorService = clientVendorService;
            _localizer = localizer;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string searchTerm = null, ClientType? clientType = null, bool? isActive = null)
        {
            var clients = await _clientVendorService.GetClientsAsync(searchTerm, clientType, isActive);
            
            // Create SelectListItems for ClientType enum
            var clientTypeItems = Enum.GetValues(typeof(ClientType))
                .Cast<ClientType>()
                .Select(t => new SelectListItem 
                { 
                    Value = ((int)t).ToString(), 
                    Text = t.ToString() 
                });
            
            var viewModel = new ClientListViewModel
            {
                Clients = clients,
                SearchTerm = searchTerm,
                ClientType = clientType,
                IsActive = isActive,
                ClientTypes = clientTypeItems
            };
            
            return View(viewModel);
        }

        [HttpGet]
        public IActionResult Create()
        {
            var viewModel = new ClientViewModel
            {
                IsActive = true,
                ClientType = ClientType.Individual // Default value
            };
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
                    Notes = viewModel.Notes
                };

                await _clientVendorService.AddClientAsync(client);
                TempData["SuccessMessage"] = _localizer["Client created successfully."];
                return RedirectToAction(nameof(Index));
            }
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
                Notes = client.Notes
            };

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

                await _clientVendorService.UpdateClientAsync(client);
                TempData["SuccessMessage"] = _localizer["Client updated successfully."];
                return RedirectToAction(nameof(Index));
            }
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

            return View(client);
        }

        [HttpGet]
        public async Task<IActionResult> Delete(Guid id)
        {
            var client = await _clientVendorService.GetClientByIdAsync(id);
            if (client == null)
            {
                return NotFound();
            }

            return View(client);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            try
            {
                await _clientVendorService.DeleteClientAsync(id);
                TempData["SuccessMessage"] = _localizer["Client deleted successfully."];
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }
            
            return RedirectToAction(nameof(Index));
        }
    }
}
