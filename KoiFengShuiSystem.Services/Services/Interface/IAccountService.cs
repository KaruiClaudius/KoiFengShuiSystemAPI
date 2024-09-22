using KoiFengShuiSystem.DataAccess.Models;
using KoiFengShuiSystem.Shared.Models.Request;
using KoiFengShuiSystem.Shared.Models.Response;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KoiFengShuiSystem.BusinessLogic.Services.Interface
{
    public interface IAccountService
    {
        AuthenticateResponse? Authenticate(AuthenticateRequest model);
        IEnumerable<Account> GetAll();
        Account? GetById(int id);
        Account Register(RegisterRequest model);
        void Update(int id, UpdateRequest model);
        void Delete(int id);

    }
}
