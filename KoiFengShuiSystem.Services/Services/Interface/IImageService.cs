using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using KoiFengShuiSystem.Shared.Models.Request;
using KoiFengShuiSystem.Shared.Models.Response;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KoiFengShuiSystem.BusinessLogic.Services.Interface
{
    public interface IImageService
    {
        Task<List<ImageResponse>> GetAllImagesAsync();
        Task<ImageResponse> GetImageByIdAsync(int imageId);
        Task<ImageResponse> CreateImageAsync(ImageRequest request);
        Task<bool> DeleteImageAsync(int imageId);
        Task<string> SaveImageAsync(IFormFile file); 
        Task<bool> SaveImagesAsync(string imageUrl);
    }
}
