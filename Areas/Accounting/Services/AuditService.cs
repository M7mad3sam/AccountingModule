using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AspNetCoreMvcTemplate.Areas.Accounting.Models;
using AspNetCoreMvcTemplate.Data.Repository;
using System.Linq;
using Microsoft.AspNetCore.Http;

namespace AspNetCoreMvcTemplate.Areas.Accounting.Services
{
    public interface IAuditService
    {
        Task LogAuditAsync(string entityName, string entityId, string action, string oldValues, string newValues, string userId, string ipAddress);
        Task<IEnumerable<AuditLog>> GetAuditLogsAsync(string entityName = null, string entityId = null, string userId = null, DateTime? fromDate = null, DateTime? toDate = null, int pageIndex = 1, int pageSize = 50);
        Task<byte[]> ExportAuditLogsToCsvAsync(IEnumerable<AuditLog> logs);
        Task LogActivityAsync(string entityName, string action, string description);
    }

    public class AuditService : IAuditService
    {
        private readonly IRepository<AuditLog> _auditLogRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuditService(
            IRepository<AuditLog> auditLogRepository,
            IHttpContextAccessor httpContextAccessor)
        {
            _auditLogRepository = auditLogRepository;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task LogAuditAsync(string entityName, string entityId, string action, string oldValues, string newValues, string userId, string ipAddress)
        {
            var auditLog = new AuditLog
            {
                EntityName = entityName,
                EntityId = entityId,
                Action = action,
                OldValues = oldValues,
                NewValues = newValues,
                UserId = userId,
                Timestamp = DateTime.UtcNow,
                IpAddress = ipAddress ?? _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString()
            };

            await _auditLogRepository.AddAsync(auditLog);
            await _auditLogRepository.SaveAsync();
        }

        public async Task LogActivityAsync(string entityName, string action, string description)
        {
            var userId = _httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var ipAddress = _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString();
            
            var auditLog = new AuditLog
            {
                EntityName = entityName,
                EntityId = string.Empty,
                Action = action,
                OldValues = string.Empty,
                NewValues = description,
                UserId = userId,
                Timestamp = DateTime.UtcNow,
                IpAddress = ipAddress
            };

            await _auditLogRepository.AddAsync(auditLog);
            await _auditLogRepository.SaveAsync();
        }

        public async Task<IEnumerable<AuditLog>> GetAuditLogsAsync(
            string entityName = null, 
            string entityId = null, 
            string userId = null, 
            DateTime? fromDate = null, 
            DateTime? toDate = null, 
            int pageIndex = 1, 
            int pageSize = 50)
        {
            var filter = BuildAuditLogFilter(entityName, entityId, userId, fromDate, toDate);
            var orderBy = BuildAuditLogOrderBy();
            
            var result = await _auditLogRepository.GetPagedAsync(
                pageIndex,
                pageSize,
                filter,
                orderBy);
                
            return result.Items;
        }

        private System.Linq.Expressions.Expression<Func<AuditLog, bool>> BuildAuditLogFilter(
            string entityName, 
            string entityId, 
            string userId, 
            DateTime? fromDate, 
            DateTime? toDate)
        {
            return al => 
                (string.IsNullOrEmpty(entityName) || al.EntityName == entityName) &&
                (string.IsNullOrEmpty(entityId) || al.EntityId == entityId) &&
                (string.IsNullOrEmpty(userId) || al.UserId == userId) &&
                (!fromDate.HasValue || al.Timestamp >= fromDate) &&
                (!toDate.HasValue || al.Timestamp <= toDate);
        }

        private Func<IQueryable<AuditLog>, IOrderedQueryable<AuditLog>> BuildAuditLogOrderBy()
        {
            return query => query.OrderByDescending(al => al.Timestamp);
        }

        public async Task<byte[]> ExportAuditLogsToCsvAsync(IEnumerable<AuditLog> logs)
        {
            var csv = new System.Text.StringBuilder();
            
            // Add header
            csv.AppendLine("Entity Name,Entity ID,Action,User ID,Timestamp,IP Address");
            
            // Add items
            foreach (var log in logs)
            {
                csv.AppendLine($"{log.EntityName},{log.EntityId},{log.Action},{log.UserId},{log.Timestamp},{log.IpAddress}");
            }
            
            return System.Text.Encoding.UTF8.GetBytes(csv.ToString());
        }
    }
}
