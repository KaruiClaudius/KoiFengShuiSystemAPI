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
        AuthenticationResult Authenticate(AuthenticateRequest model);
        IEnumerable<Account> GetAll();
        Account? GetById(int id);
        Account Register(RegisterRequest model);
        void Update(int id, UpdateRequest model);
        void Delete(int id);
        Task<Account> GetAccountByEmail(string email);
        Task<bool> SendPasswordResetEmail(string email, string fullName, string newPassword);
        Task UpdateUserPassword(Account account, string newPassword);
        Task<Account> CreateAsync(Account account);
        Task<AccountResponse> GetAccountByEmailAsync(string email);
        Task<bool> SendDefaultPassword(string email, string fullName, string defaultPassword);
        Task<bool> ChangePasswordAsync(int accountId, string currentPassword, string newPassword);

    }
}
