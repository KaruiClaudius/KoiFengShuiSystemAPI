using KoiFengShuiSystem.Api.Authorization;
using KoiFengShuiSystem.BusinessLogic.Services.Interface;
using KoiFengShuiSystem.DataAccess.Base;
using KoiFengShuiSystem.DataAccess.Models;
using KoiFengShuiSystem.Shared.Models.Request;
using KoiFengShuiSystem.Shared.Models.Response;
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

        public AccountService(IJwtUtils jwtUtils, GenericRepository<Account> accountRepository)
        {
            _jwtUtils = jwtUtils;
            _accountRepository = accountRepository;
        }

        public AuthenticateResponse? Authenticate(AuthenticateRequest model)
        {
            var account = _accountRepository.GetAll().SingleOrDefault(x => x.Email == model.Email && x.Password == model.Password);

            if (account == null) return null;

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
            };

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
            if (!string.IsNullOrEmpty(model.Password))
                account.Password = model.Password; // Note: In a real application, you should hash this password

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

    }
}

