using KoiFengShuiSystem.DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Asn1;
using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace KoiFengShuiSystem.DataAccess.Base
{
    public class GenericRepository<T> : List<T> where T : class
    {
        protected KoiFengShuiContext _context;
        protected readonly DbSet<T> _dbSet;

        #region Separating asign entity and save operators

        public GenericRepository()
        {
            _context ??= new KoiFengShuiContext(new DbContextOptions<KoiFengShuiContext>());
            _dbSet = _context.Set<T>();
        }


        public void PrepareCreate(T entity)
        {
            _dbSet.Add(entity);
        }

        public void PrepareUpdate(T entity)
        {
            var tracker = _dbSet.Attach(entity);
            tracker.State = EntityState.Modified;
        }

        public void PrepareRemove(T entity)
        {
            _dbSet.Remove(entity);
        }

        public int Save()
        {
            return _context.SaveChanges();
        }

        public async Task<int> SaveAsync()
        {
            return await _context.SaveChangesAsync();
        }

        #endregion Separating asign entity and save operators


        public List<T> GetAll()
        {
            return _dbSet.ToList();
        }
        public virtual IQueryable<T> GetAllQuery()
        {
            return _context.Set<T>().AsQueryable();
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
                Console.WriteLine($"SqlNullValueException in GetAllAsync: {ex.Message}");
                return new List<T>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in GetAllAsync: {ex.Message}");
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
                // Log error
                Console.WriteLine($"SqlNullValueException in FindAsync: {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                // Log error
                Console.WriteLine($"Exception in FindAsync: {ex.Message}");
                throw;
            }
        }

        public void Create(T entity)
        {
            _dbSet.Add(entity);
            _context.SaveChanges();
        }

        public async Task<int> CreateAsync(T entity)
        {
            _dbSet.Add(entity);
            return await _context.SaveChangesAsync();
        }

        public void Update(T entity)
        {
            var tracker = _context.Attach(entity);
            tracker.State = EntityState.Modified;
            _context.SaveChanges();
        }

        public async Task<int> UpdateAsync(T entity)
        {
            var tracker = _context.Attach(entity);
            tracker.State = EntityState.Modified;

            return await _context.SaveChangesAsync();
        }

        public bool Remove(T entity)
        {
            _dbSet.Remove(entity);
            _context.SaveChanges();
            return true;
        }

        public async Task<bool> RemoveAsync(T entity)
        {
            _dbSet.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }

        public T GetById(int id)
        {
            return _dbSet.Find(id);
        }

        public async Task<T> GetByIdAsync(int id)
        {
            return await _dbSet.FindAsync(id);
        }

        public T GetById(string code)
        {
            return _dbSet.Find(code);
        }

        public async Task<T> GetByIdAsync(string code)
        {
            return await _dbSet.FindAsync(code);
        }

        public T GetById(Guid code)
        {
            return _dbSet.Find(code);
        }

        public async Task<T> GetByIdAsync(Guid code)
        {
            return await _dbSet.FindAsync(code);
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
                // Log error
                Console.WriteLine($"SqlNullValueException in GetAllWithIncludeAsync: {ex.Message}");
                return new List<T>();
            }
            catch (Exception ex)
            {
                // Log error
                Console.WriteLine($"Exception in GetAllWithIncludeAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<T> FindWithIncludeAsync(Expression<Func<T, bool>> predicate, params Expression<Func<T, object>>[] includeProperties)
        {
            try
            {
                IQueryable<T> query = _dbSet.AsNoTracking();

                foreach (var includeProperty in includeProperties)
                {
                    query = query.Include(includeProperty);
                }

                return await query.FirstOrDefaultAsync(predicate);
            }
            catch (SqlNullValueException ex)
            {
                // Log error
                Console.WriteLine($"SqlNullValueException in FindWithIncludeAsync: {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                // Log error
                Console.WriteLine($"Exception in FindWithIncludeAsync: {ex.Message}");
                throw;
            }
        }
        public IQueryable<T> FindByCondition(Expression<Func<T, bool>> expression)
        {
            try
            {
                return _dbSet
                    .AsNoTracking()
                    .Where(expression);
            }
            catch (Exception ex)
            {
                // Log error
                Console.WriteLine($"Exception in FindByCondition: {ex.Message}");
                throw;
            }
        }
        #region Pagination

        public int PageIndex { get; private set; }
        public int PageSize { get; private set; }
        public int TotalCount { get; private set; }

        public GenericRepository(List<T> items, int totalCount)
        {
            this.AddRange(items);
            PageIndex = 1;
            PageSize = items.Count;
            TotalCount = totalCount;
        }

        public static async Task<GenericRepository<T>> CreateAsync(IQueryable<T> source, int pageNumber, int pageSize)
        {
            try
            {
                var count = await source.CountAsync();
                var paginatedItems = await source
                    .AsNoTracking()
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                return new GenericRepository<T>(paginatedItems, count);
            }
            catch (SqlNullValueException ex)
            {
                // Log error
                Console.WriteLine($"SqlNullValueException in CreateAsync: {ex.Message}");
                return new GenericRepository<T>(new List<T>(), 0);
            }
            catch (Exception ex)
            {
                // Log error
                Console.WriteLine($"Exception in CreateAsync: {ex.Message}");
                throw;
            }
        }
        #endregion
    }
}