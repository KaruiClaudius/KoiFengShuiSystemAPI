using KoiFengShuiSystem.DataAccess.Models;
using KoiFengShuiSystem.Modules.Community.Application.Abstractions;
using KoiFengShuiSystem.Modules.Community.Application.Requests;
using KoiFengShuiSystem.Modules.Community.Application.Responses;

namespace KoiFengShuiSystem.Modules.Community.Application.Services
{
    public class AdminPostService : IAdminPostService
    {
        private readonly ICommunityStore _store;

        public AdminPostService(ICommunityStore store)
        {
            _store = store;
        }

        public async Task<List<AdminPostResponse>> GetAllAdminPostsAsync()
        {
            var posts = await _store.GetAllAdminPostsWithImagesAsync();
            return posts.Select(MapToAdminPostResponse).ToList();
        }

        public async Task<AdminPostResponse> GetAdminPostByIdAsync(int id)
        {
            var post = await _store.GetAdminPostByIdWithImagesAsync(id);
            return post == null ? null : MapToAdminPostResponse(post);
        }

        public async Task<AdminPostResponse> UpdateAdminPostAsync(int id, AdminPostRequest adminPostRequest, List<string> imageUrls)
        {
            var stored = await _store.UpdateAdminPostAsync(new AdminPostUpdate(
                id,
                adminPostRequest.Name,
                adminPostRequest.Description,
                adminPostRequest.Status,
                imageUrls));

            return stored == null ? null : MapToAdminPostResponse(stored);
        }

        public async Task<AdminPostResponse> CreatePostWithImagesAsync(AdminPostRequest adminPostRequest, List<string> imageUrls)
        {
            var postCategoryExists = await _store.PostCategoryExistsAsync(adminPostRequest.Id);
            if (!postCategoryExists)
            {
                // Preserved legacy quirk: an unknown category surfaces as a thrown
                // ArgumentException here, which the controller's catch-all converts
                // into 500 instead of a 400.
                throw new ArgumentException("The provided Id does not exist in the PostCategory table.");
            }

            var post = new Post
            {
                PostCategoryId = adminPostRequest.Id,
                Name = adminPostRequest.Name,
                Description = adminPostRequest.Description,
                CreateAt = DateTime.UtcNow,
                UpdateAt = DateTime.UtcNow,
                AccountId = adminPostRequest.AccountId,
                Status = adminPostRequest.Status,
                ElementId = adminPostRequest.ElementId
            };

            // The post root is attached to the store before/independently of its images
            // so creation persists even with zero images; PostImage/Image navigations
            // still propagate generated keys on the shared save.
            foreach (var imageUrl in imageUrls)
            {
                post.PostImages.Add(new PostImage
                {
                    Post = post,
                    Image = new Image { ImageUrl = imageUrl },
                    ImageDescription = "Default description" //auto set postimage ImageDescription
                });
            }
            await _store.AddAdminPostWithImagesAsync(post);

            return await GetAdminPostByIdAsync(post.PostId);
        }

        public Task<bool> DeletePostWithAllRelatedAsync(int postId)
            => _store.DeletePostWithAllRelatedAsync(postId);

        private static AdminPostResponse MapToAdminPostResponse(Post post)
        {
            return new AdminPostResponse
            {
                PostId = post.PostId,
                Id = post.PostCategoryId,
                Name = post.Name,
                Description = post.Description,
                CreateAt = post.CreateAt,
                UpdateAt = post.UpdateAt,
                AccountId = post.AccountId,
                Status = post.Status,
                ElementId = post.ElementId,
                AccountName = "N/A", // Account nav removed - use AccountId for lookup
                ImageUrls = post.PostImages.Select(pi => pi.Image.ImageUrl).ToList()
            };
        }
    }
}
