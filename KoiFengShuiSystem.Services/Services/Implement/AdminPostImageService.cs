using Microsoft.EntityFrameworkCore;
using KoiFengShuiSystem.DataAccess;
using KoiFengShuiSystem.DataAccess.Models;
using KoiFengShuiSystem.Shared.Models.Request;
using KoiFengShuiSystem.Shared.Models.Response;
using KoiFengShuiSystem.BusinessLogic.Services.Interface;

namespace KoiFengShuiSystem.BusinessLogic.Services
{
    public class AdminPostImageService : IAdminPostImageService
    {
        private readonly KoiFengShuiContext _context;

        public AdminPostImageService(KoiFengShuiContext context)
        {
            _context = context;
        }

        public async Task<List<AdminPostImageResponse>> GetAdminPostImagesByPostIdAsync(int postId)
        {
            var postImages = await _context.PostImages
                .Include(pi => pi.Image)
                .Where(pi => pi.PostId == postId)
                .ToListAsync();

            return postImages.Select(MapToAdminPostImageResponse).ToList();
        }

        public async Task<AdminPostImageResponse> CreateAdminPostImageAsync(AdminPostImageRequest request)
        {
            var postImage = new PostImage
            {
                PostId = request.PostId,
                ImageId = request.ImageId,
                ImageDescription = request.ImageDescription
            };

            _context.PostImages.Add(postImage);
            await _context.SaveChangesAsync();

            return await GetAdminPostImageByIdAsync(postImage.PostImageId);
        }

        public async Task<bool> DeleteAdminPostImageAsync(int postImageId)
        {
            var postImage = await _context.PostImages.FindAsync(postImageId);
            if (postImage == null)
            {
                return false;
            }

            _context.PostImages.Remove(postImage);
            await _context.SaveChangesAsync();

            return true;
        }

        private async Task<AdminPostImageResponse> GetAdminPostImageByIdAsync(int postImageId)
        {
            var postImage = await _context.PostImages
                .Include(pi => pi.Image)
                .FirstOrDefaultAsync(pi => pi.PostImageId == postImageId);

            return postImage == null ? null : MapToAdminPostImageResponse(postImage);
        }

        private AdminPostImageResponse MapToAdminPostImageResponse(PostImage postImage)
        {
            return new AdminPostImageResponse
            {
                PostImageId = postImage.PostImageId,
                PostId = postImage.PostId,
                ImageId = postImage.ImageId,
                ImageDescription = postImage.ImageDescription,
                Image = new ImageResponse
                {
                    ImageId = postImage.Image.ImageId,
                    ImageUrl = postImage.Image.ImageUrl
                }
            };
        }
    }
}