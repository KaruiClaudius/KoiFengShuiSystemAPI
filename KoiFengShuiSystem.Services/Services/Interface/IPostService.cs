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
        IEnumerable<Post> GetAll();
        Task<Post> CreateAsync(Post post);
        Post? GetById(int id);
        /*void Update(int id, UpdateRequest model);*/
        void Delete(int id);
        
    }
}
