using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace AspNetCoreMvcTemplate.Data.Repository
{
    public interface IRepository<T> where T : class
    {
        // Get methods
        Task<T?> GetByIdAsync(Guid id, Func<IQueryable<T>, IQueryable<T>>? includeBuilder = null, params Expression<Func<T, object>>[] includeProperties);
        Task<IEnumerable<T>> GetAllAsync(Func<IQueryable<T>, IQueryable<T>>? includeBuilder = null, params Expression<Func<T, object>>[] includeProperties);
        Task<T?> FindAsync(Expression<Func<T, bool>> predicate, Func<IQueryable<T>, IQueryable<T>>? includeBuilder = null, params Expression<Func<T, object>>[] includeProperties);
        Task<IEnumerable<T>> FindAllAsync(Expression<Func<T, bool>> predicate, Func<IQueryable<T>, IQueryable<T>>? includeBuilder = null, params Expression<Func<T, object>>[] includeProperties);
        
        // Paging methods
        Task<(IEnumerable<T> Items, int TotalCount, int TotalPages)> GetPagedAsync(
            int pageIndex, 
            int pageSize, 
            Expression<Func<T, bool>> filter = null,
            Func<IQueryable<T>, IOrderedQueryable<T>> orderBy = null);
        Task<int> CountAsync(Expression<Func<T, bool>> predicate);
        Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate);

        // Add methods
        Task AddAsync(T entity);
        Task AddRangeAsync(IEnumerable<T> entities);
        
        // Update methods
        void Update(T entity);
        void UpdateRange(IEnumerable<T> entities);
        Task UpdateAsync(T entity);
        
        // Delete methods
        void Delete(T entity);
        void Delete(Guid id);
        void DeleteRange(IEnumerable<T> entities);
        Task DeleteAsync(Guid id);

        // Transaction management
        Task<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction> BeginTransactionAsync();
        Task CommitTransactionAsync();
        void RollbackTransaction();
        Task SaveAsync();
        
    }

    public class Repository<T> : IRepository<T> where T : class
    {
        protected readonly DbContext _context;
        protected readonly DbSet<T> _dbSet;

        public Repository(ApplicationDbContext context)
        {
            _context = context;
            _dbSet = context.Set<T>();
        }

        public async Task<T?> GetByIdAsync(Guid id, Func<IQueryable<T>, IQueryable<T>>? includeBuilder = null, params Expression<Func<T, object>>[] includeProperties)
        {
            var query = _dbSet.AsQueryable();
            
            foreach (var include in includeProperties)
            {
                query = query.Include(include);
            }
            
            if (includeBuilder != null)
            {
                query = includeBuilder(query);
            }
            
            return await query.FirstOrDefaultAsync(e => EF.Property<Guid>(e, "Id") == id);
        }

        public async Task<IEnumerable<T>> GetAllAsync(Func<IQueryable<T>, IQueryable<T>>? includeBuilder = null, params Expression<Func<T, object>>[] includeProperties)
        {
            var query = _dbSet.AsQueryable();
            
            foreach (var include in includeProperties)
            {
                query = query.Include(include);
            }
            
            if (includeBuilder != null)
            {
                query = includeBuilder(query);
            }
            
            return await query.ToListAsync();
        }

        public async Task<T?> FindAsync(Expression<Func<T, bool>> predicate, Func<IQueryable<T>, IQueryable<T>>? includeBuilder = null, params Expression<Func<T, object>>[] includeProperties)
        {
            var query = _dbSet.AsQueryable();
            
            foreach (var include in includeProperties)
            {
                query = query.Include(include);
            }
            
            if (includeBuilder != null)
            {
                query = includeBuilder(query);
            }
            
            return await query.FirstOrDefaultAsync(predicate);
        }

        public async Task<IEnumerable<T>> FindAllAsync(Expression<Func<T, bool>> predicate, Func<IQueryable<T>, IQueryable<T>>? includeBuilder = null, params Expression<Func<T, object>>[] includeProperties)
        {
            var query = _dbSet.AsQueryable();
            
            foreach (var include in includeProperties)
            {
                query = query.Include(include);
            }
            
            if (includeBuilder != null)
            {
                query = includeBuilder(query);
            }
            
            return await query.Where(predicate).ToListAsync();
        }

        public async Task<int> CountAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.CountAsync(predicate);
        }

        public async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.AnyAsync(predicate);
        }

        public async Task<(IEnumerable<T> Items, int TotalCount, int TotalPages)> GetPagedAsync(
            int pageIndex, 
            int pageSize, 
            Expression<Func<T, bool>> filter = null,
            Func<IQueryable<T>, IOrderedQueryable<T>> orderBy = null)
        {
            // Start with all items
            IQueryable<T> query = _dbSet;
            
            // Apply filter if provided
            if (filter != null)
            {
                query = query.Where(filter);
            }
            
            // Get total count for pagination
            var totalCount = await query.CountAsync();
            
            // Calculate total pages
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            
            // Apply ordering if provided
            if (orderBy != null)
            {
                query = orderBy(query);
            }
            
            // Apply pagination
            var items = await query
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
                
            return (items, totalCount, totalPages);
        }

        public async Task AddAsync(T entity)
        {
            await _dbSet.AddAsync(entity);
        }

        public async Task AddRangeAsync(IEnumerable<T> entities)
        {
            await _dbSet.AddRangeAsync(entities);
        }

        public void Update(T entity)
        {
            _dbSet.Attach(entity);
            _context.Entry(entity).State = EntityState.Modified;
        }

        public void UpdateRange(IEnumerable<T> entities)
        {
            foreach (var entity in entities)
            {
                Update(entity);
            }
        }
        
        public async Task UpdateAsync(T entity)
        {
            Update(entity);
            await SaveAsync();
        }

        public void Delete(T entity)
        {
            if (_context.Entry(entity).State == EntityState.Detached)
            {
                _dbSet.Attach(entity);
            }
            _dbSet.Remove(entity);
        }

        public void Delete(Guid id)
        {
            var entity = _dbSet.Find(id);
            if (entity != null)
            {
                Delete(entity);
            }
        }
        
        public async Task DeleteAsync(Guid id)
        {
            Delete(id);
            await SaveAsync();
        }

        public void DeleteRange(IEnumerable<T> entities)
        {
            _dbSet.RemoveRange(entities);
        }

        public async Task<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction> BeginTransactionAsync()
        {
            return await _context.Database.BeginTransactionAsync();
        }

        public async Task CommitTransactionAsync()
        {
            await _context.Database.CommitTransactionAsync();
        }

        public void RollbackTransaction()
        {
            _context.Database.RollbackTransaction();
        }

        public async Task SaveAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
