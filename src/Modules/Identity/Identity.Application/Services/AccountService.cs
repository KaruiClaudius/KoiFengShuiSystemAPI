using KoiFengShuiSystem.Modules.Identity.Application.Abstractions;
using KoiFengShuiSystem.Modules.Identity.Application.Requests;
using KoiFengShuiSystem.Modules.Identity.Application.Responses;
using KoiFengShuiSystem.Modules.Identity.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace KoiFengShuiSystem.Modules.Identity.Application.Services;

public class AccountService : IAccountService
{
    private readonly IIdentityReadStore _readStore;
    private readonly IIdentityWriteStore _writeStore;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IIdentityEmailSender _identityEmailSender;
    private readonly ILogger<AccountService> _logger;
    private readonly IIdentityElementLookup _elementLookup;

    public AccountService(
        IIdentityReadStore readStore,
        IIdentityWriteStore writeStore,
        IJwtTokenService jwtTokenService,
        IIdentityEmailSender identityEmailSender,
        ILogger<AccountService> logger,
        IIdentityElementLookup elementLookup)
    {
        ArgumentNullException.ThrowIfNull(readStore);
        ArgumentNullException.ThrowIfNull(writeStore);
        ArgumentNullException.ThrowIfNull(jwtTokenService);
        ArgumentNullException.ThrowIfNull(identityEmailSender);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(elementLookup);

        _readStore = readStore;
        _writeStore = writeStore;
        _jwtTokenService = jwtTokenService;
        _identityEmailSender = identityEmailSender;
        _logger = logger;
        _elementLookup = elementLookup;
    }

    public async Task<AuthenticationResult> AuthenticateAsync(AuthenticateRequest model)
    {
        var account = await _readStore.GetAccountByEmailAsync(model.Email ?? string.Empty);

        if (account == null)
        {
            return new AuthenticationResult { ErrorMessage = "Email not found." };
        }

        if (account.Password != model.Password)
        {
            return new AuthenticationResult { ErrorMessage = "Incorrect password." };
        }

        if (account.Dob.HasValue)
        {
            account.ElementId = await GetElementIdFromDateOfBirth(account.Dob.Value.Year, account.Gender ?? string.Empty);
            await _writeStore.UpdateAccountAsync(account);
            await _writeStore.SaveChangesAsync();
        }

        var token = _jwtTokenService.GenerateJwtToken(account);
        var response = new AuthenticateResponse(account, token);

        return new AuthenticationResult { Response = response };
    }

    public async Task<IEnumerable<Account>> GetAllAsync() => await _readStore.GetAllAccountsAsync();

    public async Task<Account?> GetByIdAsync(int id) => await _readStore.GetAccountByIdAsync(id);

    public async Task<Account> RegisterAsync(RegisterRequest model)
    {
        if (await _readStore.GetAccountByEmailAsync(model.Email ?? string.Empty) != null)
            throw new ApplicationException("Email '" + model.Email + "' is already taken");

        var account = new Account
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

        if (account.Dob.HasValue)
        {
            account.ElementId = await GetElementIdFromDateOfBirth(account.Dob.Value.Year, account.Gender ?? string.Empty);
        }

        await _writeStore.CreateAccountAsync(account);
        await _writeStore.SaveChangesAsync();

        return account;
    }

    public async Task UpdateAsync(int id, UpdateRequest model)
    {
        var account = await _readStore.GetAccountByIdAsync(id);

        if (account == null)
            throw new ApplicationException("Account not found");
        if (!string.IsNullOrEmpty(model.Email) && model.Email != account.Email &&
            await _readStore.GetAccountByEmailAsync(model.Email) != null)
            throw new ApplicationException("Email '" + model.Email + "' is already taken");

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

        account.ElementId = await GetElementIdFromDateOfBirth(
            model.Dob?.Year ?? account.Dob?.Year ?? DateTime.Now.Year,
            model.Gender ?? account.Gender ?? string.Empty);

        await _writeStore.UpdateAccountAsync(account);
        await _writeStore.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var account = await _readStore.GetAccountByIdAsync(id);
        if (account == null)
            throw new ApplicationException("Account not found");

        await _writeStore.DeleteAccountAsync(account);
        await _writeStore.SaveChangesAsync();
    }

    public async Task<Account?> GetAccountByEmailAsync(string email) => await _readStore.GetAccountByEmailAsync(email);

    public async Task<bool> SendPasswordResetEmailAsync(string email, string fullName, string newPassword)
        => await _identityEmailSender.SendPasswordResetEmailAsync(email, fullName, newPassword);

    public async Task<bool> SendDefaultPasswordAsync(string email, string fullName, string defaultPassword)
        => await _identityEmailSender.SendDefaultPasswordAsync(email, fullName, defaultPassword);

    public async Task UpdateUserPasswordAsync(Account account, string newPassword)
    {
        if (account == null)
            throw new ArgumentNullException(nameof(account), "Account object is null");
        if (string.IsNullOrEmpty(newPassword))
            throw new ArgumentException("New password is null or empty", nameof(newPassword));

        try
        {
            var existedUser = await _readStore.GetAccountByEmailAsync(account.Email ?? string.Empty);
            if (existedUser == null || (existedUser.AccountId != account.AccountId && existedUser.Email != account.Email))
            {
                existedUser = await _readStore.GetAccountByIdAsync(account.AccountId);
            }

            if (existedUser == null)
            {
                throw new KeyNotFoundException($"User not found. AccountId: {account.AccountId}, Email: {account.Email}");
            }

            existedUser.Password = newPassword;
            await _writeStore.UpdateAccountAsync(existedUser);
            await _writeStore.SaveChangesAsync();
            _logger.LogInformation("Password updated successfully for user {Email}", existedUser.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user password. AccountId: {AccountId}, Email: {Email}", account.AccountId, account.Email);
            throw;
        }
    }

    public async Task<Account> CreateAsync(Account account)
    {
        await _writeStore.CreateAccountAsync(account);
        await _writeStore.SaveChangesAsync();
        return account;
    }

    public async Task<AccountResponse?> GetAccountResponseByEmailAsync(string email)
    {
        var account = await _readStore.GetAccountByEmailAsync(email);
        if (account == null)
        {
            return null;
        }

        string? elementName = null;
        if (account.ElementId.HasValue)
        {
            elementName = await _elementLookup.GetElementNameByIdAsync(account.ElementId.Value);
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
            ElementName = elementName
        };
    }

    public async Task<bool> ChangePasswordAsync(int accountId, string currentPassword, string newPassword)
    {
        try
        {
            var account = await _readStore.GetAccountByIdAsync(accountId);
            if (account == null)
                throw new KeyNotFoundException("Account not found");

            if (account.Password != currentPassword)
                return false;

            account.Password = newPassword;
            await _writeStore.UpdateAccountAsync(account);
            await _writeStore.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password for account id: {AccountId}", accountId);
            throw;
        }
    }

    private async Task<int> GetElementIdFromDateOfBirth(int yearOfBirth, string gender)
    {
        var elementName = CalculateElement(yearOfBirth, gender);
        var elementId = await _elementLookup.GetElementIdByNameAsync(elementName);

        if (!elementId.HasValue)
        {
            _logger.LogError("Element not found for elementName: {ElementName}", elementName);
            throw new ApplicationException($"Element '{elementName}' not found in the database.");
        }

        return elementId.Value;
    }

    private class CungPhiResult
    {
        public string Cung { get; set; } = string.Empty;
        public string Menh { get; set; } = string.Empty;
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

    // TODO: Replace this duplicate calculation with a shared FengShui calculator port.
    private string CalculateElement(int yearOfBirth, string gender)
    {
        if (yearOfBirth <= 0)
        {
            throw new ArgumentException($"Invalid year of birth: {yearOfBirth}. Year must be a positive number.");
        }

        int lastTwoDigits = yearOfBirth % 100;
        int a = (lastTwoDigits / 10) + (lastTwoDigits % 10);
        if (a > 9)
        {
            a = (a / 10) + (a % 10);
        }

        int resultNumber;
        bool isMale = gender?.ToLower() == "male" || gender?.ToLower() == "nam";

        if (yearOfBirth < 2000)
        {
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
            if (isMale)
            {
                resultNumber = 9 - a;
                if (resultNumber == 0)
                {
                    resultNumber = 9;
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

        if (resultNumber == 5)
        {
            resultNumber = isMale ? 2 : 8;
        }

        var cungPhiResult = _cungPhiMap[resultNumber];
        return cungPhiResult.Menh;
    }
}
