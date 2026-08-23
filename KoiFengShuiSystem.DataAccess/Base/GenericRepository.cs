using KoiFengShuiSystem.DataAccess.Models;
using KoiFengShuiSystem.Shared.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Data.SqlTypes;
using System.Linq.Expressions;

namespace KoiFengShuiSystem.DataAccess.Base
{
    public class GenericRepository<T> : List<T> where T : class
    {
        protected KoiFengShuiContext _context;
        protected readonly DbSet<T> _dbSet;

        public GenericRepository(KoiFengShuiContext context)
        {
            _context = context;
            _dbSet = _context.Set<T>();
        }

        public async Task<List<T>> GetAllAsync()
        {
            try
            {
                return await _dbSet.AsNoTracking().ToListAsync();
            }
            catch (SqlNullValueException)
            {
                // Log error here if needed
                return new List<T>();
            }
        }
        public async Task<List<T>> GetAllAsync(Expression<Func<T, bool>> predicate)
        {
            try
            {
                return await _dbSet
                    .AsNoTracking()
                    .Where(predicate)
                    .ToListAsync();
            }
            catch (SqlNullValueException ex)
            {
                System.Diagnostics.Debug.WriteLine($"SqlNullValueException in GetAllAsync: {ex.Message}");
                return new List<T>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Exception in GetAllAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<T> FindAsync(Expression<Func<T, bool>> predicate)
        {
            try
            {
                return await _dbSet
                    .AsNoTracking()
                    .FirstOrDefaultAsync(predicate);
            }
            catch (SqlNullValueException ex)
            {
                System.Diagnostics.Debug.WriteLine($"SqlNullValueException in FindAsync: {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Exception in FindAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<T> GetByIdAsync(int id)
        {
            return await _dbSet.FindAsync(id);
        }

        public async Task<List<T>> GetAllWithIncludeAsync(params Expression<Func<T, object>>[] includeProperties)
        {
            try
            {
                IQueryable<T> query = _context.Set<T>().AsNoTracking();

                foreach (var includeProperty in includeProperties)
                {
                    query = query.Include(includeProperty);
                }

                return await query.ToListAsync();
            }
            catch (SqlNullValueException ex)
            {
                System.Diagnostics.Debug.WriteLine($"SqlNullValueException in GetAllWithIncludeAsync: {ex.Message}");
                return new List<T>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Exception in GetAllWithIncludeAsync: {ex.Message}");
                throw;
            }
        }
    }
}
