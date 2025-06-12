using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AspNetCoreMvcTemplate.Areas.Accounting.Models;
using AspNetCoreMvcTemplate.Areas.Accounting.Data.Specifications;
using AspNetCoreMvcTemplate.Data.Repository;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AspNetCoreMvcTemplate.Areas.Accounting.Services
{
    public interface IClientVendorService
    {
        // Client methods
        Task<Client> GetClientByIdAsync(Guid id);
        Task<IEnumerable<Client>> GetClientsAsync(bool? isActive = null);
        Task<IEnumerable<Client>> GetClientsAsync(string searchTerm = null, ClientType? clientType = null, bool? isActive = null);
        Task<Client> CreateClientAsync(Client client);
        Task UpdateClientAsync(Client client);
        Task DeleteClientAsync(Guid id);
        Task<bool> IsClientCodeUniqueAsync(string code, Guid? id = null);
        Task<bool> CanDeleteClientAsync(Guid id);
        Task<IEnumerable<SelectListItem>> GetClientSelectListAsync(bool? isActive = null);

        // Vendor methods
        Task<Vendor> GetVendorByIdAsync(Guid id);
        Task<IEnumerable<Vendor>> GetVendorsAsync(bool? isActive = null);
        Task<IEnumerable<Vendor>> GetVendorsAsync(string searchTerm = null, string vendorType = null, bool? isActive = null, bool? subjectToWithholdingTax = null);
        Task<Vendor> CreateVendorAsync(Vendor vendor);
        Task UpdateVendorAsync(Vendor vendor);
        Task DeleteVendorAsync(Guid id);
        Task<bool> IsVendorCodeUniqueAsync(string code, Guid? id = null);
        Task<bool> CanDeleteVendorAsync(Guid id);
        Task<IEnumerable<SelectListItem>> GetVendorSelectListAsync(bool? isActive = null);
        Task<IEnumerable<string>> GetVendorTypesAsync();
    }

    public class ClientVendorService : IClientVendorService
    {
        private readonly IRepository<Client> _clientRepository;
        private readonly IRepository<Vendor> _vendorRepository;
        private readonly IRepository<JournalEntry> _journalEntryRepository;

        public ClientVendorService(
            IRepository<Client> clientRepository,
            IRepository<Vendor> vendorRepository,
            IRepository<JournalEntry> journalEntryRepository)
        {
            _clientRepository = clientRepository;
            _vendorRepository = vendorRepository;
            _journalEntryRepository = journalEntryRepository;
        }

        // Client methods
        public async Task<Client> GetClientByIdAsync(Guid id)
        {
            return await _clientRepository.GetByIdAsync(id);
        }

        public async Task<IEnumerable<Client>> GetClientsAsync(bool? isActive = null)
        {
            if (isActive.HasValue)
            {
                return await _clientRepository.FindAllAsync(c => c.IsActive == isActive.Value);
            }
            return await _clientRepository.GetAllAsync();
        }

        public async Task<IEnumerable<Client>> GetClientsAsync(string searchTerm = null, ClientType? clientType = null, bool? isActive = null)
        {
            var query = _clientRepository.GetAllAsync().Result.AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(c => c.Code.Contains(searchTerm) || c.NameEn.Contains(searchTerm) || c.NameAr.Contains(searchTerm));
            }

            if (clientType.HasValue)
            {
                query = query.Where(c => c.ClientType == clientType.Value);
            }

            if (isActive.HasValue)
            {
                query = query.Where(c => c.IsActive == isActive.Value);
            }

            return await Task.FromResult(query.ToList());
        }

        public async Task<Client> CreateClientAsync(Client client)
        {
            await _clientRepository.AddAsync(client);
            await _clientRepository.SaveAsync();
            return client;
        }

        public async Task UpdateClientAsync(Client client)
        {
            _clientRepository.Update(client);
            await _clientRepository.SaveAsync();
        }

        public async Task DeleteClientAsync(Guid id)
        {
            var client = await _clientRepository.GetByIdAsync(id);
            if (client != null)
            {
                _clientRepository.Delete(client);
                await _clientRepository.SaveAsync();
            }
        }

        public async Task<bool> IsClientCodeUniqueAsync(string code, Guid? id = null)
        {
            var spec = new ClientByCodeSpecification(code);
            var existingClient = await _clientRepository.FindAsync(spec.Criteria);
            
            return existingClient == null || (id.HasValue && existingClient.Id == id.Value);
        }

        public async Task<bool> CanDeleteClientAsync(Guid id)
        {
            return !await _journalEntryRepository.ExistsAsync(je => je.ClientId == id);
        }

        public async Task<IEnumerable<SelectListItem>> GetClientSelectListAsync(bool? isActive = null)
        {
            var clients = await GetClientsAsync(isActive);
            return clients.Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = $"{c.Code} - {c.NameEn}"
            }).ToList();
        }

        // Vendor methods
        public async Task<Vendor> GetVendorByIdAsync(Guid id)
        {
            return await _vendorRepository.GetByIdAsync(id);
        }

        public async Task<IEnumerable<Vendor>> GetVendorsAsync(bool? isActive = null)
        {
            if (isActive.HasValue)
            {
                return await _vendorRepository.FindAllAsync(v => v.IsActive == isActive.Value);
            }
            return await _vendorRepository.GetAllAsync();
        }

        public async Task<IEnumerable<Vendor>> GetVendorsAsync(string searchTerm = null, string vendorType = null, bool? isActive = null, bool? subjectToWithholdingTax = null)
        {
            var query = _vendorRepository.GetAllAsync().Result.AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(v => v.Code.Contains(searchTerm) || v.NameEn.Contains(searchTerm) || v.NameAr.Contains(searchTerm));
            }

            if (!string.IsNullOrEmpty(vendorType))
            {
                if (Enum.TryParse<VendorType>(vendorType, out var typeEnum))
                {
                    query = query.Where(v => v.Type == typeEnum);
                }
            }

            if (isActive.HasValue)
            {
                query = query.Where(v => v.IsActive == isActive.Value);
            }

            if (subjectToWithholdingTax.HasValue)
            {
                query = query.Where(v => v.SubjectToWithholdingTax == subjectToWithholdingTax.Value);
            }

            return await Task.FromResult(query.ToList());
        }

        public async Task<Vendor> CreateVendorAsync(Vendor vendor)
        {
            await _vendorRepository.AddAsync(vendor);
            await _vendorRepository.SaveAsync();
            return vendor;
        }

        public async Task UpdateVendorAsync(Vendor vendor)
        {
            _vendorRepository.Update(vendor);
            await _vendorRepository.SaveAsync();
        }

        public async Task DeleteVendorAsync(Guid id)
        {
            var vendor = await _vendorRepository.GetByIdAsync(id);
            if (vendor != null)
            {
                _vendorRepository.Delete(vendor);
                await _vendorRepository.SaveAsync();
            }
        }

        public async Task<bool> IsVendorCodeUniqueAsync(string code, Guid? id = null)
        {
            var spec = new VendorByCodeSpecification(code);
            var existingVendor = await _vendorRepository.FindAsync(spec.Criteria);
            
            return existingVendor == null || (id.HasValue && existingVendor.Id == id.Value);
        }

        public async Task<bool> CanDeleteVendorAsync(Guid id)
        {
            return !await _journalEntryRepository.ExistsAsync(je => je.VendorId == id);
        }

        public async Task<IEnumerable<SelectListItem>> GetVendorSelectListAsync(bool? isActive = null)
        {
            var vendors = await GetVendorsAsync(isActive);
            return vendors.Select(v => new SelectListItem
            {
                Value = v.Id.ToString(),
                Text = $"{v.Code} - {v.NameEn}"
            }).ToList();
        }

        public async Task<IEnumerable<string>> GetVendorTypesAsync()
        {
            return new List<string> { "Supplier", "Contractor", "ServiceProvider" };
        }
    }
}
