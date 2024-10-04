using KoiFengShuiSystem.Api.Authorization;
using KoiFengShuiSystem.BusinessLogic.Services.Interface;
using KoiFengShuiSystem.DataAccess.Base;
using KoiFengShuiSystem.DataAccess.Models;
using KoiFengShuiSystem.Shared.Helpers;
using KoiFengShuiSystem.Shared.Models.Request;
using KoiFengShuiSystem.Shared.Models.Response;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Org.BouncyCastle.Crypto.Generators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
               EmailService emailService, ILogger<AccountService> logger, GenericRepository<Element> elementRepository
              )
        {
            _jwtUtils = jwtUtils;
            _accountRepository = accountRepository;
            _emailService = emailService;
            _logger = logger;
            _elementRepository = elementRepository;
        }

        public AuthenticateResponse? Authenticate(AuthenticateRequest model)
        {
            var account = _accountRepository.GetAll().SingleOrDefault(x => x.Email == model.Email && x.Password == model.Password);

            if (account == null) return null;

            // Calculate and update element
            if (account.Dob.HasValue)
            {
                var element = GetElementFromDateOfBirth(account.Dob.Value.Year).Result;
                account.ElementId = element.ElementId;
                _accountRepository.PrepareUpdate(account);
                _accountRepository.Save();
            }

            var token = _jwtUtils.GenerateJwtToken(account);
            return new AuthenticateResponse(account, token);
        }

        public IEnumerable<Account> GetAll()
        {
            return _accountRepository.GetAll();
        }

        public Account? GetById(int id)
        {
            return _accountRepository.GetById(id);
        }

        public Account Register(RegisterRequest model)
        {
            // Validate
            if (_accountRepository.GetAll().Any(x => x.Email == model.Email))
                throw new ApplicationException("Email '" + model.Email + "' is already taken");

            // Map model to new account object
            var account = new Account
            {
                FullName = model.FullName,
                Email = model.Email,
                Password = model.Password, // Note: In a real application, you should hash this password
                Dob = model.Dob.Date,
                Phone = model.Phone,
                Gender = model.Gender,
                CreateAt = DateTime.Now,
                RoleId = 2
            };

            // Calculate and set element
            if (account.Dob.HasValue)
            {
                var element = GetElementFromDateOfBirth(account.Dob.Value.Year).Result;
                account.ElementId = element.ElementId;
            }

            // Save account
            _accountRepository.PrepareCreate(account);
            _accountRepository.Save();

            return account;
        }

        public void Update(int id, UpdateRequest model)
        {
            var account = _accountRepository.GetById(id);

            // Validate
            if (account == null)
                throw new ApplicationException("Account not found");
            if (!string.IsNullOrEmpty(model.Email) && model.Email != account.Email && _accountRepository.GetAll().Any(x => x.Email == model.Email))
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

            // Calculate and update element
            if (account.Dob.HasValue)
            {
                var element = GetElementFromDateOfBirth(account.Dob.Value.Year).Result;
                account.ElementId = element.ElementId;
            }

            _accountRepository.PrepareUpdate(account);
            _accountRepository.Save();
        }

        public void Delete(int id)
        {
            var account = _accountRepository.GetById(id);
            if (account == null)
                throw new ApplicationException("Account not found");

            _accountRepository.PrepareRemove(account);
            _accountRepository.Save();
        }

        public async Task<Account> GetAccountByEmail(string email)
        {
            return await _accountRepository.FindAsync(x => x.Email == email);
        }

        public async Task<bool> SendPasswordResetEmail(string email, string fullName, string newPassword)
        {
            var mailData = new MailData()
            {
                EmailToId = email,
                EmailToName = fullName,
                EmailBody = $@"
<div style=""max-width: 400px; margin: 50px auto; padding: 30px; text-align: center; font-size: 120%; background-color: #f9f9f9; border-radius: 10px; box-shadow: 0 0 20px rgba(0, 0, 0, 0.1); position: relative;"">
    <img src=""https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcTRDn7YDq7gsgIdHOEP2_Mng6Ym3OzmvfUQvQ&usqp=CAU"" alt=""Noto Image"" style=""max-width: 100px; height: auto; display: block; margin: 0 auto; border-radius: 50%;"">
    <h2 style=""text-transform: uppercase; color: #3498db; margin-top: 20px; font-size: 28px; font-weight: bold;"">Password Reset</h2>
    <p>Your new password is: <span style=""font-weight: bold; color: #e74c3c;"">{newPassword}</span></p>
    <p>Please log in and change your password.</p>
    <p style=""color: #888; font-size: 14px;"">Powered by KoiFengShui</p>
</div>",
                EmailSubject = "Password Reset"
            };

            return await _emailService.SendEmailAsync(mailData);
        }

        public async Task<bool> SendDefaultPassword(string email, string fullName, string defaultPassword)
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
                EmailSubject = "Password Reset"
            };

            return await _emailService.SendEmailAsync(mailData);
        }

        public async Task UpdateUserPassword(Account account, string newPassword)
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
                _accountRepository.Update(existedUser);
                try
                {
                    _accountRepository.Save();
                }
                catch (DbUpdateException ex)
                {
                    _logger.LogError(ex, "Failed to save account to database.");
                    throw; // Optionally, rethrow the exception if you need to handle it further up
                }

                _logger.LogInformation($"Password updated successfully for user {existedUser.Email}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating user password. AccountId: {account.AccountId}, Email: {account.Email}");
                throw; // Re-throw the exception to be handled by the calling method
            }
        }

        public async Task<Account> CreateAsync(Account account)
        {
            // Prepare the account entity for creation
            _accountRepository.PrepareCreate(account);

            // Save changes asynchronously
            await _accountRepository.SaveAsync();

            // Return the newly created account
            return account;
        }

        public async Task<AccountResponse> GetAccountByEmailAsync(string email)
        {
            var account = await _accountRepository.FindAsync(x => x.Email == email);
            if (account == null)
            {
                return null;
            }

            return new AccountResponse
            {
                AccountId = account.AccountId,
                FullName = account.FullName,
                Email = account.Email,
                RoleId = account.RoleId
                // Add any other properties you need
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
            int branch = yearOfBirth % 12;

            int elementIndex = (stem + branch) % 5;
            if (elementIndex == 0) elementIndex = 5;

            string element = elementIndex switch
            {
                1 => "Kim",
                2 => "Thuỷ",
                3 => "Hoả",
                4 => "Thổ",
                5 => "Mộc",
                _ => throw new ArgumentException($"Invalid element calculation for year: {yearOfBirth}")
            };

            return element;
        }

        public async Task<bool> ChangePasswordAsync(int accountId, string currentPassword, string newPassword)
        {
            try
            {
                var account = await _accountRepository.GetByIdAsync(accountId);
                if (account == null)
                    throw new KeyNotFoundException("Account not found");

                // Verify current password
                if (account.Password != currentPassword)
                    return false;

                // Update password
                account.Password = newPassword; // In a real-world scenario, you should hash this password
                _accountRepository.PrepareUpdate(account);
                await _accountRepository.SaveAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error changing password for account id: {accountId}");
                throw; // Rethrow the exception to be caught in the controller
            }
        }




    }
}

