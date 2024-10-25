using System.Collections.Generic;
using System.Threading.Tasks;
using KoiFengShuiSystem.Shared.Models.Request;
using KoiFengShuiSystem.Shared.Models.Response;

namespace KoiFengShuiSystem.BusinessLogic.Services.Interface
{
    public interface IAdminPostImageService
    {
        Task<List<AdminPostImageResponse>> GetAdminPostImagesByPostIdAsync(int postId);
        Task<AdminPostImageResponse> CreateAdminPostImageAsync(AdminPostImageRequest request);
        Task<bool> DeleteAdminPostImageAsync(int postImageId);
    }
}