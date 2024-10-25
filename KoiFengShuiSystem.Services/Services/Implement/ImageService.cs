using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using KoiFengShuiSystem.DataAccess;
using KoiFengShuiSystem.DataAccess.Models;
using KoiFengShuiSystem.Shared.Models.Request;
using KoiFengShuiSystem.Shared.Models.Response;
using KoiFengShuiSystem.BusinessLogic.Services.Interface;

namespace KoiFengShuiSystem.BusinessLogic.Services
{
    public class ImageService : IImageService
    {
        private readonly KoiFengShuiContext _context;
        private readonly string _imageBasePath;

        public ImageService(KoiFengShuiContext context, IConfiguration configuration)
        {
            _context = context;
            _imageBasePath = configuration["ImageStorage:BasePath"];
        }

        public async Task<List<ImageResponse>> GetAllImagesAsync()
        {
            var images = await _context.Images.ToListAsync();
            return images.Select(MapToImageResponse).ToList();
        }

        public async Task<ImageResponse> GetImageByIdAsync(int imageId)
        {
            var image = await _context.Images.FindAsync(imageId);
            return image == null ? null : MapToImageResponse(image);
        }

        public async Task<ImageResponse> CreateImageAsync(ImageRequest request)
        {
            var image = new Image
            {
                ImageUrl = request.ImageUrl
            };

            _context.Images.Add(image);
            await _context.SaveChangesAsync();

            return MapToImageResponse(image);
        }

        public async Task<bool> DeleteImageAsync(int imageId)
        {
            var image = await _context.Images.FindAsync(imageId);
            if (image == null)
            {
                return false;
            }

            _context.Images.Remove(image);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<string> SaveImageAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                throw new ArgumentException("Invalid file");
            }

            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var filePath = Path.Combine(_imageBasePath, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return $"/images/{fileName}";
        }

        private ImageResponse MapToImageResponse(Image image)
        {
            return new ImageResponse
            {
                ImageId = image.ImageId,
                ImageUrl = image.ImageUrl
            };
        }
    }
}