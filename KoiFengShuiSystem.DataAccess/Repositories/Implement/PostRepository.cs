using KoiFengShuiSystem.DataAccess.Base;
using KoiFengShuiSystem.DataAccess.DTO;
using KoiFengShuiSystem.DataAccess.Models;
using Microsoft.EntityFrameworkCore;
namespace KoiFengShuiSystem.DataAccess.Repositories.Implement
{
    public class PostRepository : GenericRepository<Post>
    {
        public PostRepository() { }
        public async Task<IEnumerable<PostResponse>> GetAllWithElementAsync()
        {
            var posts = await _dbSet
                .Include(p => p.Element) // Include the Element to access ElementName
                .ToListAsync();

            if (posts != null)
            {
                var postResponses = posts.Select(po => new PostResponse
                {
                    PostId = po.PostId,
                    Description = po.Description,
                    CreateAt = po.CreateAt,
                    AccountId = po.Account.AccountId,
                    UpdateAt = po.UpdateAt,
                    ElementId = po.ElementId,
                    Follows = po.Follows,
                    Id = po.Id,
                    IdNavigation = po.IdNavigation,
                    Name = po.Name,
                    Price = po.Price,
                    ElementName = po.Element.ElementName, // Access ElementName
                    AccountName = po.Account.FullName, // Access ElementName
                }).ToList();

                return postResponses;
            }

            return new List<PostResponse>();
        }
        public async Task<IEnumerable<PostResponse>>GetAllByElementIdAsync(int elementId)
        {
            var posts = await _dbSet
                .Where(p => p.ElementId == elementId)
                .Include(p => p.Element) // Include the Element to access ElementName
                .ToListAsync();

            if (posts != null)
            {
                var postResponses = posts.Select(po => new PostResponse
                {
                    PostId = po.PostId,
                    Description = po.Description,
                    CreateAt = po.CreateAt,
                    AccountId = po.Account.AccountId,
                    UpdateAt = po.UpdateAt,
                    ElementId = po.ElementId,
                    Follows = po.Follows,
                    Id = po.Id,
                    IdNavigation = po.IdNavigation,
                    Name = po.Name,
                    Price = po.Price,
                    ElementName = po.Element.ElementName, // Access ElementName
                    AccountName = po.Account.FullName, // Access ElementName
                }).ToList();

                return postResponses;
            }

            return new List<PostResponse>();
        }
    }
}
