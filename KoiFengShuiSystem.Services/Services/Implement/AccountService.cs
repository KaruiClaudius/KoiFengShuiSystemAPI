using CloudinaryDotNet;
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
        private readonly GenericRepository<DataAccess.Models.Account> _accountRepository;
        private readonly EmailService _emailService;
        private readonly ILogger<AccountService> _logger;
        private readonly GenericRepository<Element> _elementRepository;

        public AccountService(IJwtUtils jwtUtils, GenericRepository<DataAccess.Models.Account> accountRepository,
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
                var element = await GetElementFromDateOfBirth(account.Dob.Value.Year, account.Gender);
                account.ElementId = element.ElementId;
                _accountRepository.PrepareUpdate(account);
                await _accountRepository.SaveAsync();
            }

            var token = _jwtUtils.GenerateJwtToken(account);
            var response = new AuthenticateResponse(account, token);

            return new AuthenticationResult { Response = response };
        }

        public async Task<IEnumerable<DataAccess.Models.Account>> GetAllAsync()
        {
            return await _accountRepository.GetAllAsync();
        }

        public async Task<DataAccess.Models.Account?> GetByIdAsync(int id)
        {
            return await _accountRepository.GetByIdAsync(id);
        }

        public async Task<DataAccess.Models.Account> RegisterAsync(RegisterRequest model)
        {
            // Validate
            if (await _accountRepository.FindAsync(x => x.Email == model.Email) != null)
                throw new ApplicationException("Email '" + model.Email + "' is already taken");

            // Map model to new account object
            var account = new DataAccess.Models.Account
            {
                FullName = model.FullName,
                Email = model.Email,
                Password = model.Password,
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
                var element = await GetElementFromDateOfBirth(account.Dob.Value.Year, account.Gender);
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

            // Always calculate and update element
            var element = await GetElementFromDateOfBirth(
                model.Dob?.Year ?? account.Dob?.Year ?? DateTime.Now.Year,
                model.Gender ?? account.Gender
            );
            account.ElementId = element.ElementId;

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

        public async Task<DataAccess.Models.Account> GetAccountByEmailAsync(string email)
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

        public async Task UpdateUserPasswordAsync(DataAccess.Models.Account account, string newPassword)
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

        public async Task<DataAccess.Models.Account> CreateAsync(DataAccess.Models.Account account)
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

        private async Task<Element> GetElementFromDateOfBirth(int yearOfBirth, string gender)
        {
            string elementName = CalculateElement(yearOfBirth, gender);
            var element = await _elementRepository.FindAsync(e => e.ElementName == elementName);

            if (element == null)
            {
                _logger.LogError($"Element not found for elementName: {elementName}");
                throw new ApplicationException($"Element '{elementName}' not found in the database.");
            }

            return element;
        }

        private class CungPhiResult
        {
            public string Cung { get; set; }
            public string Menh { get; set; }
        }

        private readonly Dictionary<int, CungPhiResult> _cungPhiMap = new Dictionary<int, CungPhiResult>
    {
        { 1, new CungPhiResult { Cung = "Khảm", Menh = "Thủy" } },
        { 2, new CungPhiResult { Cung = "Khôn", Menh = "Thổ" } },
        { 3, new CungPhiResult { Cung = "Chấn", Menh = "Mộc" } },
        { 4, new CungPhiResult { Cung = "Tốn", Menh = "Mộc" } },
        { 5, new CungPhiResult { Cung = "Trung cung", Menh = "Thổ" } },
        { 6, new CungPhiResult { Cung = "Càn", Menh = "Kim" } },
        { 7, new CungPhiResult { Cung = "Đoài", Menh = "Kim" } },
        { 8, new CungPhiResult { Cung = "Cấn", Menh = "Thổ" } },
        { 9, new CungPhiResult { Cung = "Ly", Menh = "Hoả" } }
    };

        private string CalculateElement(int yearOfBirth, string gender)
        {
            if (yearOfBirth <= 0)
            {
                throw new ArgumentException($"Invalid year of birth: {yearOfBirth}. Year must be a positive number.");
            }

            // Lấy 2 số cuối của năm sinh
            int lastTwoDigits = yearOfBirth % 100;

            // Cộng 2 số cuối
            int a = (lastTwoDigits / 10) + (lastTwoDigits % 10);
            if (a > 9)
            {
                a = (a / 10) + (a % 10);
            }

            int resultNumber;
            bool isMale = gender?.ToLower() == "male" || gender?.ToLower() == "nam";

            if (yearOfBirth < 2000)
            {
                // Trước năm 2000
                if (isMale)
                {
                    resultNumber = 10 - a;
                }
                else
                {
                    resultNumber = 5 + a;
                    if (resultNumber > 9)
                    {
                        resultNumber = (resultNumber / 10) + (resultNumber % 10);
                    }
                }
            }
            else
            {
                // Từ năm 2000 trở đi
                if (isMale)
                {
                    resultNumber = 9 - a;
                    if (resultNumber == 0)
                    {
                        resultNumber = 9; // Cung Ly
                    }
                }
                else
                {
                    resultNumber = 6 + a;
                    if (resultNumber > 9)
                    {
                        resultNumber = (resultNumber / 10) + (resultNumber % 10);
                    }
                }
            }

            var cungPhiResult = _cungPhiMap[resultNumber];
            return cungPhiResult.Menh;
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
