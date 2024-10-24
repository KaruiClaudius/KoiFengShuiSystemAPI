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
            var posts = await _context.Posts
                .Include(p => p.Element)
                .Include(p => p.Account)
                .Include(p => p.PostImages)
                    .ThenInclude(pi => pi.Image)
                .ToListAsync();

            return posts.Select(MapPostToAdminPostResponse);
        }

        public async Task<AdminPostResponse> GetPostByIdAsync(int id)
        {
            var post = await _context.Posts
                .Include(p => p.Element)
                .Include(p => p.Account)
                .Include(p => p.PostImages)
                    .ThenInclude(pi => pi.Image)
                .FirstOrDefaultAsync(p => p.PostId == id);

            return post == null ? null : MapPostToAdminPostResponse(post);
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

            if (postRequest.Images != null && postRequest.Images.Any())
            {
                foreach (var imageRequest in postRequest.Images)
                {
                    var image = new Image { ImageUrl = imageRequest.ImageUrl };
                    _context.Images.Add(image);
                    await _context.SaveChangesAsync();

                    var postImage = new PostImage
                    {
                        PostId = post.PostId,
                        ImageId = image.ImageId,
                        ImageDescription = imageRequest.ImageDescription
                    };
                    _context.PostImages.Add(postImage);
                }
                await _context.SaveChangesAsync();
            }

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

            if (postRequest.Images != null)
            {
                var existingPostImages = await _context.PostImages.Where(pi => pi.PostId == id).ToListAsync();
                _context.PostImages.RemoveRange(existingPostImages);

                foreach (var imageRequest in postRequest.Images)
                {
                    var image = new Image { ImageUrl = imageRequest.ImageUrl };
                    _context.Images.Add(image);
                    await _context.SaveChangesAsync();

                    var postImage = new PostImage
                    {
                        PostId = post.PostId,
                        ImageId = image.ImageId,
                        ImageDescription = imageRequest.ImageDescription
                    };
                    _context.PostImages.Add(postImage);
                }
            }

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
                .Include(p => p.PostImages)
                    .ThenInclude(pi => pi.Image)
                .Where(p => p.Account.RoleId == 1) // Assuming RoleId 1 is for admin
                .ToListAsync();

            return adminPosts.Select(MapPostToAdminPostResponse);
        }

        private AdminPostResponse MapPostToAdminPostResponse(Post post)
        {
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
                AccountName = post.Account?.FullName,
                Images = post.PostImages.Select(pi => new PostImageResponse
                {
                    PostImageId = pi.PostImageId,
                    ImageId = pi.ImageId,
                    ImageUrl = pi.Image.ImageUrl,
                    ImageDescription = pi.ImageDescription
                }).ToList()
            };
        }
    }
}