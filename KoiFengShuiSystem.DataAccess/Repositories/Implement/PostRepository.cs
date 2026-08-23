using KoiFengShuiSystem.DataAccess.Base;
using KoiFengShuiSystem.DataAccess.Models;
using KoiFengShuiSystem.Shared.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
namespace KoiFengShuiSystem.DataAccess.Repositories.Implement
{
    public class PostRepository : GenericRepository<Post>
    {
        public PostRepository(KoiFengShuiContext context) : base(context) { }
        public async Task<IEnumerable<Post>> GetAllWithElementAsync()
        {
            return await _dbSet
                .ToListAsync();

        }
        public async Task<GenericRepository<Post>> GetAllByPostTypeIdAsync(int postTypeId, int pageNumber, int pageSize)
        {
            var posts = await _dbSet
                .Where(p => p.PostCategoryId == postTypeId)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            var totalCount = await _dbSet.CountAsync(p => p.PostCategoryId == postTypeId);

            return new GenericRepository<Post>(posts, totalCount);
        }
    }
}
