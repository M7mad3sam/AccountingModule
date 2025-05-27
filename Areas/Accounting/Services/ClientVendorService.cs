using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AspNetCoreMvcTemplate.Areas.Accounting.Models;
using AspNetCoreMvcTemplate.Areas.Accounting.Data.Specifications;
using AspNetCoreMvcTemplate.Data.Repository;
using System.Linq;

namespace AspNetCoreMvcTemplate.Areas.Accounting.Services
{
    public interface IClientVendorService
    {
        Task<Client> GetClientByIdAsync(Guid id);
        Task<IEnumerable<Client>> GetAllClientsAsync();
        Task<Client> CreateClientAsync(Client client);
        Task UpdateClientAsync(Client client);
        Task DeleteClientAsync(Guid id);
        Task<Vendor> GetVendorByIdAsync(Guid id);
        Task<IEnumerable<Vendor>> GetAllVendorsAsync();
        Task<Vendor> CreateVendorAsync(Vendor vendor);
        Task UpdateVendorAsync(Vendor vendor);
        Task DeleteVendorAsync(Guid id);
    }

    public class ClientVendorService : IClientVendorService
    {
        private readonly IRepository<Client> _clientRepository;
        private readonly IRepository<Vendor> _vendorRepository;
        private readonly IRepository<Account> _accountRepository;

        public ClientVendorService(
            IRepository<Client> clientRepository,
            IRepository<Vendor> vendorRepository,
            IRepository<Account> accountRepository)
        {
            _clientRepository = clientRepository;
            _vendorRepository = vendorRepository;
            _accountRepository = accountRepository;
        }

        public async Task<Client> GetClientByIdAsync(Guid id)
        {
            return await _clientRepository.GetByIdAsync(id, c => c.Account);
        }

        public async Task<IEnumerable<Client>> GetAllClientsAsync()
        {
            return await _clientRepository.FindAllAsync(c => c.IsActive, c => c.Account);
        }

        public async Task<Client> CreateClientAsync(Client client)
        {
            // Validate client code uniqueness
            var spec = new ClientByCodeSpecification(client.Code);
            var existingClient = await _clientRepository.FindAsync(spec.Criteria);
            if (existingClient != null)
            {
                throw new InvalidOperationException("Client code already exists");
            }

            // Validate account exists
            var account = await _accountRepository.GetByIdAsync(client.AccountId);
            if (account == null)
            {
                throw new InvalidOperationException("Account not found");
            }

            await _clientRepository.AddAsync(client);
            await _clientRepository.SaveAsync();
            return client;
        }

        public async Task UpdateClientAsync(Client client)
        {
            // Validate client code uniqueness
            var spec = new ClientByCodeSpecification(client.Code);
            var existingClient = await _clientRepository.FindAsync(spec.Criteria);
            if (existingClient != null && existingClient.Id != client.Id)
            {
                throw new InvalidOperationException("Client code already exists");
            }

            // Validate account exists
            var account = await _accountRepository.GetByIdAsync(client.AccountId);
            if (account == null)
            {
                throw new InvalidOperationException("Account not found");
            }

            _clientRepository.Update(client);
            await _clientRepository.SaveAsync();
        }

        public async Task DeleteClientAsync(Guid id)
        {
            var client = await _clientRepository.GetByIdAsync(id);
            if (client != null)
            {
                // Soft delete by marking as inactive
                client.IsActive = false;
                _clientRepository.Update(client);
                await _clientRepository.SaveAsync();
            }
        }

        public async Task<Vendor> GetVendorByIdAsync(Guid id)
        {
            return await _vendorRepository.GetByIdAsync(id, v => v.Account);
        }

        public async Task<IEnumerable<Vendor>> GetAllVendorsAsync()
        {
            return await _vendorRepository.FindAllAsync(v => v.IsActive, v => v.Account);
        }

        public async Task<Vendor> CreateVendorAsync(Vendor vendor)
        {
            // Validate vendor code uniqueness
            var spec = new VendorByCodeSpecification(vendor.Code);
            var existingVendor = await _vendorRepository.FindAsync(spec.Criteria);
            if (existingVendor != null)
            {
                throw new InvalidOperationException("Vendor code already exists");
            }

            // Validate account exists
            var account = await _accountRepository.GetByIdAsync(vendor.AccountId);
            if (account == null)
            {
                throw new InvalidOperationException("Account not found");
            }

            await _vendorRepository.AddAsync(vendor);
            await _vendorRepository.SaveAsync();
            return vendor;
        }

        public async Task UpdateVendorAsync(Vendor vendor)
        {
            // Validate vendor code uniqueness
            var spec = new VendorByCodeSpecification(vendor.Code);
            var existingVendor = await _vendorRepository.FindAsync(spec.Criteria);
            if (existingVendor != null && existingVendor.Id != vendor.Id)
            {
                throw new InvalidOperationException("Vendor code already exists");
            }

            // Validate account exists
            var account = await _accountRepository.GetByIdAsync(vendor.AccountId);
            if (account == null)
            {
                throw new InvalidOperationException("Account not found");
            }

            _vendorRepository.Update(vendor);
            await _vendorRepository.SaveAsync();
        }

        public async Task DeleteVendorAsync(Guid id)
        {
            var vendor = await _vendorRepository.GetByIdAsync(id);
            if (vendor != null)
            {
                // Soft delete by marking as inactive
                vendor.IsActive = false;
                _vendorRepository.Update(vendor);
                await _vendorRepository.SaveAsync();
            }
        }
    }
}
