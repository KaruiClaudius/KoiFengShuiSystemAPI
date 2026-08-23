using KoiFengShuiSystem.Modules.Community.Application.Requests;
using KoiFengShuiSystem.Shared.Kernel.Results;

namespace KoiFengShuiSystem.Modules.Community.Application.Services
{
    public interface IPostService
    {
        Task<IBusinessResult> GetAll();
        Task<IBusinessResult> GetPostById(int id);
        Task<IBusinessResult> GetPostByPostTypeId(int postTypeId, int page, int pageSize);

        // Council D10: category rows for the public GET /api/Post/categories surface.
        Task<IBusinessResult> GetCategories();

        // Council Q11: caller-owned posts across all statuses for the member
        // "my submissions" view. Identity comes from the token, never a param.
        Task<IBusinessResult> GetMyPosts(int accountId, int page, int pageSize);

        // Council D2: admin-role bypass for Details/{id} - reads the full queue
        // (any status) with images, unlike the Approved-only public path.
        Task<IBusinessResult> GetPostByIdForAdmin(int id);

        Task<IBusinessResult> CreatePost(CreatePostRequest request, int authorAccountId);
        Task<IBusinessResult> DeletePost(int id);
        Task<IBusinessResult> Save();
    }
}
