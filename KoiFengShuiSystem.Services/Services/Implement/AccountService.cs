using KoiFengShuiSystem.Api.Authorization;
using KoiFengShuiSystem.BusinessLogic.Services.Interface;
using KoiFengShuiSystem.DataAccess.Base;
using KoiFengShuiSystem.DataAccess.Models;
using KoiFengShuiSystem.Shared.Helpers;
using KoiFengShuiSystem.Shared.Models.Request;
using KoiFengShuiSystem.Shared.Models.Response;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace KoiFengShuiSystem.BusinessLogic.Services.Implement
{
    public class AccountService : IAccountService
    {
        private readonly IJwtUtils _jwtUtils;
        private readonly GenericRepository<Account> _accountRepository;
        private readonly EmailService _emailService;
        private readonly ILogger<AccountService> _logger;
        private readonly GenericRepository<Element> _elementRepository;

        public AccountService(IJwtUtils jwtUtils, GenericRepository<Account> accountRepository,
               EmailService emailService, ILogger<AccountService> logger, GenericRepository<Element> elementRepository)
        {
            _jwtUtils = jwtUtils;
            _accountRepository = accountRepository;
            _emailService = emailService;
            _logger = logger;
            _elementRepository = elementRepository;
        }

        public async Task<AuthenticationResult> AuthenticateAsync(AuthenticateRequest model)
        {
            var account = await _accountRepository.FindAsync(x => x.Email == model.Email);

            if (account == null)
            {
                return new AuthenticationResult { ErrorMessage = "Email not found." };
            }

            if (account.Password != model.Password)
            {
                return new AuthenticationResult { ErrorMessage = "Incorrect password." };
            }

            // Calculate and update element
            if (account.Dob.HasValue)
            {
                var element = await GetElementFromDateOfBirth(account.Dob.Value.Year);
                account.ElementId = element.ElementId;
                _accountRepository.PrepareUpdate(account);
                await _accountRepository.SaveAsync();
            }

            var token = _jwtUtils.GenerateJwtToken(account);
            var response = new AuthenticateResponse(account, token);

            return new AuthenticationResult { Response = response };
        }

        public async Task<IEnumerable<Account>> GetAllAsync()
        {
            return await _accountRepository.GetAllAsync();
        }

        public async Task<Account?> GetByIdAsync(int id)
        {
            return await _accountRepository.GetByIdAsync(id);
        }

        public async Task<Account> RegisterAsync(RegisterRequest model)
        {
            // Validate
            if (await _accountRepository.FindAsync(x => x.Email == model.Email) != null)
                throw new ApplicationException("Email '" + model.Email + "' is already taken");

            // Map model to new account object
            var account = new Account
            {
                FullName = model.FullName,
                Email = model.Email,
                Password = model.Password, // Note: In a real application, you should hash this password
                Dob = model.Dob.Date,
                Phone = model.Phone,
                CreateAt = DateTime.Now,
                UpdateAt = DateTime.Now,
                Gender = model.Gender,
                RoleId = 2
            };

            // Calculate and set element
            if (account.Dob.HasValue)
            {
                var element = await GetElementFromDateOfBirth(account.Dob.Value.Year);
                account.ElementId = element.ElementId;
            }

            // Save account
            await _accountRepository.CreateAsync(account);

            return account;
        }

        public async Task UpdateAsync(int id, UpdateRequest model)
        {
            var account = await _accountRepository.GetByIdAsync(id);

            // Validate
            if (account == null)
                throw new ApplicationException("Account not found");
            if (!string.IsNullOrEmpty(model.Email) && model.Email != account.Email &&
                await _accountRepository.FindAsync(x => x.Email == model.Email) != null)
                throw new ApplicationException("Email '" + model.Email + "' is already taken");

            // Update account properties
            if (!string.IsNullOrEmpty(model.Email))
                account.Email = model.Email;
            if (!string.IsNullOrEmpty(model.FullName))
                account.FullName = model.FullName;
            if (!string.IsNullOrEmpty(model.Phone))
                account.Phone = model.Phone;
            if (model.Dob.HasValue)
                account.Dob = model.Dob.Value;
            if (!string.IsNullOrEmpty(model.Gender))
                account.Gender = model.Gender;
            account.UpdateAt = DateTime.Now;

            // Calculate and update element
            if (account.Dob.HasValue)
            {
                var element = await GetElementFromDateOfBirth(account.Dob.Value.Year);
                account.ElementId = element.ElementId;
            }

            _accountRepository.PrepareUpdate(account);
            await _accountRepository.SaveAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var account = await _accountRepository.GetByIdAsync(id);
            if (account == null)
                throw new ApplicationException("Account not found");

            _accountRepository.PrepareRemove(account);
            await _accountRepository.SaveAsync();
        }

        public async Task<Account> GetAccountByEmailAsync(string email)
        {
            return await _accountRepository.FindAsync(x => x.Email == email);
        }

        public async Task<bool> SendPasswordResetEmailAsync(string email, string fullName, string newPassword)
        {
            var mailData = new MailData()
            {
                EmailToId = email,
                EmailToName = fullName,
                EmailBody = $@"
<div style=""max-width: 400px; margin: 50px auto; padding: 30px; text-align: center; font-size: 120%; background-color: #f9f9f9; border-radius: 10px; box-shadow: 0 0 20px rgba(0, 0, 0, 0.1); position: relative;"">
    <img src=""https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcTRDn7YDq7gsgIdHOEP2_Mng6Ym3OzmvfUQvQ&usqp=CAU"" alt=""Noto Image"" style=""max-width: 100px```
    // ... height: auto; display: block; margin: 0 auto; border-radius: 50%;"">
    <h2 style=""text-transform: uppercase; color: #3498db; margin-top: 20px; font-size: 28px; font-weight: bold;"">Password Reset</h2>
    <p>Your new password is: <span style=""font-weight: bold; color: #e74c3c;"">{newPassword}</span></p>
    <p>Please log in and change your password.</p>
    <p style=""color: #888; font-size: 14px;"">Powered by KoiFengShui</p>
</div>",
                EmailSubject = "Password Reset"
            };

            return await _emailService.SendEmailAsync(mailData);
        }

        public async Task<bool> SendDefaultPasswordAsync(string email, string fullName, string defaultPassword)
        {
            var mailData = new MailData()
            {
                EmailToId = email,
                EmailToName = fullName,
                EmailBody = $@"
<div style=""max-width: 400px; margin: 50px auto; padding: 30px; text-align: center; font-size: 120%; background-color: #f9f9f9; border-radius: 10px; box-shadow: 0 0 20px rgba(0, 0, 0, 0.1); position: relative;"">
    <img src=""https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcTRDn7YDq7gsgIdHOEP2_Mng6Ym3OzmvfUQvQ&usqp=CAU"" alt=""Noto Image"" style=""max-width: 100px; height: auto; display: block; margin: 0 auto; border-radius: 50%;"">
    <h2 style=""text-transform: uppercase; color: #3498db; margin-top: 20px; font-size: 28px; font-weight: bold;"">Default Password</h2>
    <p>Your default password is: <span style=""font-weight: bold; color: #e74c3c;"">{defaultPassword}</span></p>
    <p>Please log in and change your password.</p>
    <p style=""color: #888; font-size: 14px;"">Powered by KoiFengShui</p>
</div>",
                EmailSubject = "Default Password"
            };

            return await _emailService.SendEmailAsync(mailData);
        }

        public async Task UpdateUserPasswordAsync(Account account, string newPassword)
        {
            if (account == null)
            {
                throw new ArgumentNullException(nameof(account), "Account object is null");
            }
            if (string.IsNullOrEmpty(newPassword))
            {
                throw new ArgumentException("New password is null or empty", nameof(newPassword));
            }

            try
            {
                var existedUser = await _accountRepository.FindAsync(x => x.AccountId == account.AccountId || x.Email == account.Email);
                if (existedUser == null)
                {
                    throw new KeyNotFoundException($"User not found. AccountId: {account.AccountId}, Email: {account.Email}");
                }

                // Hash the new password before storing it
                existedUser.Password = newPassword;
                _accountRepository.PrepareUpdate(existedUser);
                await _accountRepository.SaveAsync();

                _logger.LogInformation($"Password updated successfully for user {existedUser.Email}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating user password. AccountId: {account.AccountId}, Email: {account.Email}");
                throw;
            }
        }

        public async Task<Account> CreateAsync(Account account)
        {
            _accountRepository.PrepareCreate(account);
            await _accountRepository.SaveAsync();
            return account;
        }

        public async Task<AccountResponse> GetAccountResponseByEmailAsync(string email)
        {
            var account = await _accountRepository.FindWithIncludeAsync(
                x => x.Email == email,
                x => x.Element
            );
            if (account == null)
            {
                return null;
            }

            return new AccountResponse
            {
                AccountId = account.AccountId,
                FullName = account.FullName,
                Email = account.Email,
                RoleId = account.RoleId,
                Phone = account.Phone,
                Dob = account.Dob ?? DateTime.MinValue,
                Gender = account.Gender,
                ElementName = account.Element?.ElementName
            };
        }

        private async Task<Element> GetElementFromDateOfBirth(int yearOfBirth)
        {
            string elementName = CalculateElement(yearOfBirth);
            return await _elementRepository.FindAsync(e => e.ElementName == elementName);
        }

        private string CalculateElement(int yearOfBirth)
        {
            if (yearOfBirth <= 0)
            {
                throw new ArgumentException($"Invalid year of birth: {yearOfBirth}. Year must be a positive number.");
            }

            int stem = yearOfBirth % 10;
            int stemValue = stem switch
            {
                0 or 1 => 4, // Canh, Tân
                2 or 3 => 5, // Nhâm, Quý
                4 or 5 => 1, // Giáp, Ất
                6 or 7 => 2, // Bính, Đinh
                8 or 9 => 3, // Mậu, Kỷ
                _ => throw new ArgumentException($"Invalid stem calculation for year: {yearOfBirth}")
            };

            int branch = yearOfBirth % 12;
            int branchValue = branch switch
            {
                4 or 5 or 10 or 11 => 0, // Tý, Sửu, Ngọ, Mùi
                0 or 1 or 6 or 7 => 1, // Dần, Mão, Thân, Dậu
                2 or 3 or 8 or 9 => 2, // Thìn, Tỵ, Tuất, Hợi
                _ => throw new ArgumentException($"Invalid branch calculation for year: {yearOfBirth}")
            };

            int elementIndex = stemValue + branchValue;
            if (elementIndex > 5)
            {
                elementIndex -= 5;
            }

            return elementIndex switch
            {
                1 => "Kim",
                2 => "Thuỷ",
                3 => "Hoả",
                4 => "Thổ",
                5 => "Mộc",
                _ => throw new ArgumentException($"Invalid element calculation for year: {yearOfBirth}")
            };

        }

        public async Task<bool> ChangePasswordAsync(int accountId, string currentPassword, string newPassword)
        {
            try
            {
                var account = await _accountRepository.GetByIdAsync(accountId);
                if (account == null)
                    throw new KeyNotFoundException("Account not found");

                if (account.Password != currentPassword)
                    return false;

                account.Password = newPassword; // In a real-world scenario, you should hash this password
                _accountRepository.PrepareUpdate(account);
                await _accountRepository.SaveAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error changing password for account id: {accountId}");
                throw;
            }
        }
    }
}
