using KoiFengShuiSystem.BusinessLogic.ViewModel;
using KoiFengShuiSystem.Common;
using KoiFengShuiSystem.DataAccess.Models;
using KoiFengShuiSystem.Shared.Models.Request;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoiFengShuiSystem.BusinessLogic.Services.Interface
{
    public interface IPostService
    {
        Task<IBusinessResult> GetAll();
        Task<IBusinessResult> GetPostById(int id);
        Task<IBusinessResult> GetPostByPostTypeId(int postTypeId, int page, int pageSize);
        Task<IBusinessResult> CreatePost(Post post);
        // Helper method to compare two payments
        Task<IBusinessResult> DeletePost(int id);
        Task<IBusinessResult> Save();
    }
}
