using KoiFengShuiSystem.Modules.Community.Application.Requests;
using KoiFengShuiSystem.Shared.Kernel.Results;

namespace KoiFengShuiSystem.Modules.Community.Application.Services
{
    public interface IPostService
    {
        Task<IBusinessResult> GetAll();
        Task<IBusinessResult> GetPostById(int id);
        Task<IBusinessResult> GetPostByPostTypeId(int postTypeId, int page, int pageSize);
        Task<IBusinessResult> CreatePost(CreatePostRequest request, int authorAccountId);
        Task<IBusinessResult> DeletePost(int id);
        Task<IBusinessResult> Save();
    }
}
