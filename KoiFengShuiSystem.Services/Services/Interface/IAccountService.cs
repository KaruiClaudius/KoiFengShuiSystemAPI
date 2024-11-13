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
        Task<AuthenticationResult> AuthenticateAsync(AuthenticateRequest model);
        Task<IEnumerable<Account>> GetAllAsync();
        Task<Account?> GetByIdAsync(int id);
        Task<Account> RegisterAsync(RegisterRequest model);
        Task UpdateAsync(int id, UpdateRequest model);
        Task DeleteAsync(int id);
        Task<Account> GetAccountByEmailAsync(string email);
        Task<bool> SendPasswordResetEmailAsync(string email, string fullName, string newPassword);
        Task UpdateUserPasswordAsync(Account account, string newPassword);
        Task<Account> CreateAsync(Account account);
        Task<AccountResponse> GetAccountResponseByEmailAsync(string email);
        Task<bool> SendDefaultPasswordAsync(string email, string fullName, string defaultPassword);
        Task<bool> ChangePasswordAsync(int accountId, string currentPassword, string newPassword);
        Task<bool> UpdateUserWalletAfterPosted(Account account, decimal amount);

    }
}
