using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AspNetCoreMvcTemplate.Areas.Accounting.Models;
using AspNetCoreMvcTemplate.Data.Repository;
using System.Linq;
using AspNetCoreMvcTemplate.Areas.Accounting.Data.Specifications;

namespace AspNetCoreMvcTemplate.Areas.Accounting.Services
{
    public interface IClientVendorService
    {
        // Client methods
        Task<IEnumerable<Client>> GetClientsAsync(string searchTerm = null, ClientType? clientType = null, bool? isActive = null);
        Task<Client> GetClientByIdAsync(Guid id);
        Task<Client> GetClientByCodeAsync(string code);
        Task AddClientAsync(Client client);
        Task UpdateClientAsync(Client client);
        Task DeleteClientAsync(Guid id);
        Task<IEnumerable<ClientType>> GetClientTypesAsync();
        
        // Vendor methods
        Task<IEnumerable<Vendor>> GetVendorsAsync(string searchTerm = null, string vendorType = null, bool? isActive = null, bool? subjectToWithholdingTax = null);
        Task<Vendor> GetVendorByIdAsync(Guid id);
        Task<Vendor> GetVendorByCodeAsync(string code);
        Task AddVendorAsync(Vendor vendor);
        Task UpdateVendorAsync(Vendor vendor);
        Task DeleteVendorAsync(Guid id);
        Task<IEnumerable<string>> GetVendorTypesAsync();
    }

    public class ClientVendorService : IClientVendorService
    {
        private readonly IRepository<Client> _clientRepository;
        private readonly IRepository<Vendor> _vendorRepository;
        private readonly IRepository<JournalEntry> _journalEntryRepository;
        private readonly IAuditService _auditService;

        public ClientVendorService(
            IRepository<Client> clientRepository,
            IRepository<Vendor> vendorRepository,
            IRepository<JournalEntry> journalEntryRepository,
            IAuditService auditService)
        {
            _clientRepository = clientRepository;
            _vendorRepository = vendorRepository;
            _journalEntryRepository = journalEntryRepository;
            _auditService = auditService;
        }

        #region Client Methods

        public async Task<IEnumerable<Client>> GetClientsAsync(string searchTerm = null, ClientType? clientType = null, bool? isActive = null)
        {
            var specification = new Specification<Client>(c => true);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                searchTerm = searchTerm.ToLower();
                specification = specification.And(c => 
                    c.Code.ToLower().Contains(searchTerm) || 
                    c.NameEn.ToLower().Contains(searchTerm) || 
                    c.NameAr.ToLower().Contains(searchTerm) ||
                    (c.TaxRegistrationNumber != null && c.TaxRegistrationNumber.ToLower().Contains(searchTerm)));
            }

            if (clientType.HasValue)
            {
                specification = specification.And(c => c.ClientType == clientType.Value);
            }

            if (isActive.HasValue)
            {
                specification = specification.And(c => c.IsActive == isActive.Value);
            }

            return await _clientRepository.FindAllAsync(specification);
        }

        public async Task<Client> GetClientByIdAsync(Guid id)
        {
            return await _clientRepository.GetByIdAsync(id);
        }

        public async Task<Client> GetClientByCodeAsync(string code)
        {
            var clients = await _clientRepository.FindAllAsync(c => c.Code == code);
            return clients.FirstOrDefault();
        }

        public async Task AddClientAsync(Client client)
        {
            // Check if code is unique
            var existingClient = await GetClientByCodeAsync(client.Code);
            if (existingClient != null)
            {
                throw new InvalidOperationException($"A client with code '{client.Code}' already exists");
            }

            await _clientRepository.AddAsync(client);
            await _auditService.LogActivityAsync("Client", "Create", $"Created client: {client.NameEn}");
        }

        public async Task UpdateClientAsync(Client client)
        {
            // Check if code is unique (except for this client)
            var existingClient = await GetClientByCodeAsync(client.Code);
            if (existingClient != null && existingClient.Id != client.Id)
            {
                throw new InvalidOperationException($"A client with code '{client.Code}' already exists");
            }

            await _clientRepository.UpdateAsync(client);
            await _auditService.LogActivityAsync("Client", "Update", $"Updated client: {client.NameEn}");
        }

        public async Task DeleteClientAsync(Guid id)
        {
            var client = await _clientRepository.GetByIdAsync(id);
            if (client == null)
            {
                throw new ArgumentException("Client not found");
            }

            // Check if there are journal entries referencing this client
            var journalEntries = await _journalEntryRepository.FindAllAsync(j => j.ClientId == id);
            if (journalEntries.Any())
            {
                throw new InvalidOperationException("Cannot delete a client with associated journal entries");
            }

            await _clientRepository.DeleteAsync(id);
            await _auditService.LogActivityAsync("Client", "Delete", $"Deleted client: {client.NameEn}");
        }

        public async Task<IEnumerable<ClientType>> GetClientTypesAsync()
        {
            return Enum.GetValues(typeof(ClientType)).Cast<ClientType>();
        }

        #endregion

        #region Vendor Methods

        public async Task<IEnumerable<Vendor>> GetVendorsAsync(string searchTerm = null, string vendorType = null, bool? isActive = null, bool? subjectToWithholdingTax = null)
        {
            var specification = new Specification<Vendor>(v => true);

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                searchTerm = searchTerm.ToLower();
                specification = specification.And(v => 
                    v.Code.ToLower().Contains(searchTerm) || 
                    v.NameEn.ToLower().Contains(searchTerm) || 
                    v.NameAr.ToLower().Contains(searchTerm) ||
                    (v.TaxRegistrationNumber != null && v.TaxRegistrationNumber.ToLower().Contains(searchTerm)));
            }

            if (!string.IsNullOrWhiteSpace(vendorType))
            {
                specification = specification.And(v => v.VendorType == vendorType);
            }

            if (isActive.HasValue)
            {
                specification = specification.And(v => v.IsActive == isActive.Value);
            }

            if (subjectToWithholdingTax.HasValue)
            {
                specification = specification.And(v => v.SubjectToWithholdingTax == subjectToWithholdingTax.Value);
            }

            return await _vendorRepository.FindAllAsync(specification);
        }

        public async Task<Vendor> GetVendorByIdAsync(Guid id)
        {
            return await _vendorRepository.GetByIdAsync(id);
        }

        public async Task<Vendor> GetVendorByCodeAsync(string code)
        {
            var vendors = await _vendorRepository.FindAllAsync(v => v.Code == code);
            return vendors.FirstOrDefault();
        }

        public async Task AddVendorAsync(Vendor vendor)
        {
            // Check if code is unique
            var existingVendor = await GetVendorByCodeAsync(vendor.Code);
            if (existingVendor != null)
            {
                throw new InvalidOperationException($"A vendor with code '{vendor.Code}' already exists");
            }

            await _vendorRepository.AddAsync(vendor);
            await _auditService.LogActivityAsync("Vendor", "Create", $"Created vendor: {vendor.NameEn}");
        }

        public async Task UpdateVendorAsync(Vendor vendor)
        {
            // Check if code is unique (except for this vendor)
            var existingVendor = await GetVendorByCodeAsync(vendor.Code);
            if (existingVendor != null && existingVendor.Id != vendor.Id)
            {
                throw new InvalidOperationException($"A vendor with code '{vendor.Code}' already exists");
            }

            await _vendorRepository.UpdateAsync(vendor);
            await _auditService.LogActivityAsync("Vendor", "Update", $"Updated vendor: {vendor.NameEn}");
        }

        public async Task DeleteVendorAsync(Guid id)
        {
            var vendor = await _vendorRepository.GetByIdAsync(id);
            if (vendor == null)
            {
                throw new ArgumentException("Vendor not found");
            }

            // Check if there are journal entries referencing this vendor
            var journalEntries = await _journalEntryRepository.FindAllAsync(j => j.VendorId == id);
            if (journalEntries.Any())
            {
                throw new InvalidOperationException("Cannot delete a vendor with associated journal entries");
            }

            await _vendorRepository.DeleteAsync(id);
            await _auditService.LogActivityAsync("Vendor", "Delete", $"Deleted vendor: {vendor.NameEn}");
        }

        public async Task<IEnumerable<string>> GetVendorTypesAsync()
        {
            var vendors = await _vendorRepository.GetAllAsync();
            return vendors
                .Where(v => !string.IsNullOrEmpty(v.VendorType))
                .Select(v => v.VendorType)
                .Distinct()
                .OrderBy(t => t);
        }

        #endregion
    }
}
