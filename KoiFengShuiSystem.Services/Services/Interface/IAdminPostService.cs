using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KoiFengShuiSystem.Shared.Models.Request;
using KoiFengShuiSystem.Shared.Models.Response;
namespace KoiFengShuiSystem.BusinessLogic.Services.Interface
{
    public interface IAdminPostService
    {
        Task<IEnumerable<AdminPostResponse>> GetAllPostsAsync();
        Task<AdminPostResponse> GetPostByIdAsync(int id);
        Task<AdminPostResponse> CreatePostAsync(PostRequest postRequest);
        Task<AdminPostResponse> UpdatePostAsync(int id, PostRequest postRequest);
        Task<bool> DeletePostAsync(int id);
        Task<IEnumerable<AdminPostResponse>> GetAllAdminPostsAsync();
    }
}
