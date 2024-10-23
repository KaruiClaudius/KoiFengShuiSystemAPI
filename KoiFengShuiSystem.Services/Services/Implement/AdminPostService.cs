using KoiFengShuiSystem.BusinessLogic.Services.Interface;
using KoiFengShuiSystem.DataAccess.Models;
using KoiFengShuiSystem.Shared.Models.Request;
using KoiFengShuiSystem.Shared.Models.Response;
using Microsoft.EntityFrameworkCore;

namespace KoiFengShuiSystem.BusinessLogic.Services.Implement
{
    public class AdminPostService : IAdminPostService
    {
        private readonly KoiFengShuiContext _context;

        public AdminPostService(KoiFengShuiContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<AdminPostResponse>> GetAllPostsAsync()
        {
            var posts = await _context.Posts.Include(p => p.Element).Include(p => p.Account).ToListAsync();
            return posts.Select(p => new AdminPostResponse
            {
                PostId = p.PostId,
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                CreateAt = p.CreateAt,
                UpdateAt = p.UpdateAt,
                AccountId = p.AccountId,
                ElementId = p.ElementId,
                Status = p.Status,
                ElementName = p.Element?.ElementName,
                AccountName = p.Account?.FullName
            });
        }

        public async Task<AdminPostResponse> GetPostByIdAsync(int id)
        {
            var post = await _context.Posts
                .Include(p => p.Element)
                .Include(p => p.Account)
                .FirstOrDefaultAsync(p => p.PostId == id);
            if (post == null) return null;

            return new AdminPostResponse
            {
                PostId = post.PostId,
                Id = post.Id,
                Name = post.Name,
                Description = post.Description,
                CreateAt = post.CreateAt,
                UpdateAt = post.UpdateAt,
                AccountId = post.AccountId,
                ElementId = post.ElementId,
                Status = post.Status,
                ElementName = post.Element?.ElementName,
                AccountName = post.Account?.FullName
            };
        }

        public async Task<AdminPostResponse> CreatePostAsync(PostRequest postRequest)
        {
            var post = new Post
            {
                Id = postRequest.Id,
                Name = postRequest.Name,
                Description = postRequest.Description,
                CreateAt = DateTime.Now,
                UpdateAt = DateTime.Now,
                AccountId = postRequest.AccountId,
                ElementId = postRequest.ElementId,
                Status = postRequest.Status
            };

            _context.Posts.Add(post);
            await _context.SaveChangesAsync();

            return await GetPostByIdAsync(post.PostId);
        }

        public async Task<AdminPostResponse> UpdatePostAsync(int id, PostRequest postRequest)
        {
            var post = await _context.Posts.FindAsync(id);
            if (post == null) return null;

            post.Id = postRequest.Id;
            post.Name = postRequest.Name;
            post.Description = postRequest.Description;
            post.UpdateAt = DateTime.Now;
            post.ElementId = postRequest.ElementId;
            post.Status = postRequest.Status;

            await _context.SaveChangesAsync();

            return await GetPostByIdAsync(id);
        }

        public async Task<bool> DeletePostAsync(int id)
        {
            var post = await _context.Posts.FindAsync(id);
            if (post == null) return false;

            _context.Posts.Remove(post);
            await _context.SaveChangesAsync();

            return true;
        }
        public async Task<IEnumerable<AdminPostResponse>> GetAllAdminPostsAsync()
        {
            var adminPosts = await _context.Posts
                .Include(p => p.Element)
                .Include(p => p.Account)
                .Where(p => p.Account.RoleId == 1)
                .ToListAsync();

            return adminPosts.Select(p => new AdminPostResponse
            {
                PostId = p.PostId,
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                CreateAt = p.CreateAt,
                UpdateAt = p.UpdateAt,
                AccountId = p.AccountId,
                ElementId = p.ElementId,
                Status = p.Status,
                ElementName = p.Element?.ElementName,
                AccountName = p.Account?.FullName
            });
        }
    }
}