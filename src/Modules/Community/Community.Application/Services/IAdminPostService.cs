using KoiFengShuiSystem.Modules.Community.Application.Requests;
using KoiFengShuiSystem.Modules.Community.Application.Responses;

namespace KoiFengShuiSystem.Modules.Community.Application.Services
{
    public interface IAdminPostService
    {
        Task<List<AdminPostResponse>> GetAllAdminPostsAsync();
        Task<AdminPostResponse> GetAdminPostByIdAsync(int id);
        Task<AdminPostResponse> UpdateAdminPostAsync(int id, AdminPostRequest adminPostRequest, List<string> imageUrls);
        Task<AdminPostResponse> CreatePostWithImagesAsync(AdminPostRequest adminPostRequest, List<string> imageUrls);
        Task<bool> DeletePostWithAllRelatedAsync(int postId);
    }
}
