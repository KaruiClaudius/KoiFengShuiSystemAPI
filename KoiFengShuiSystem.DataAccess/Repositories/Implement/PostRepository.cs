using KoiFengShuiSystem.DataAccess.Base;
using KoiFengShuiSystem.DataAccess.Models;
using Microsoft.EntityFrameworkCore;
namespace KoiFengShuiSystem.DataAccess.Repositories.Implement
{
    public class PostRepository : GenericRepository<Post>
    {
        public PostRepository() { }
        public async Task<IEnumerable<Post>> GetAllWithElementAsync()
        {
            return await _dbSet
                .Include(p => p.Element) // Include the Element to access ElementName
                .Include(p => p.Account)
                .ToListAsync();

        }
        public async Task<GenericRepository<Post>> GetAllByPostTypeIdAsync(int postTypeId, int pageNumber, int pageSize)
        {
            var posts = await _dbSet
                .Where(p => p.Id == postTypeId)
                .Include(p => p.Element) // Include the Element to access ElementName
                .Include(p => p.Account)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            var totalCount = await _dbSet.CountAsync(p => p.Id == postTypeId);

            return new GenericRepository<Post>(posts, totalCount);
        }
    }
}
