using KoiFengShuiSystem.Modules.Identity.Application.Abstractions;
using KoiFengShuiSystem.Modules.Identity.Application.Requests;
using KoiFengShuiSystem.Modules.Identity.Application.Responses;
using KoiFengShuiSystem.Modules.Identity.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace KoiFengShuiSystem.Modules.Identity.Application.Services;

public class AccountService : IAccountService
{
    private const int ResetTokenLifetimeMinutes = 15;
    private const string DefaultFrontendBaseUrl = "http://localhost:3000";
    private const string BcryptHashPrefix = "$2";

    private readonly IIdentityReadStore _readStore;
    private readonly IIdentityWriteStore _writeStore;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IIdentityEmailSender _identityEmailSender;
    private readonly ILogger<AccountService> _logger;
    private readonly IIdentityElementLookup _elementLookup;
    private readonly IElementCalculator _elementCalculator;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IPasswordResetTokenProvider _passwordResetTokenProvider;
    private readonly IConfiguration _configuration;
    private readonly IRefreshTokenPort _refreshTokenPort;

    public AccountService(
        IIdentityReadStore readStore,
        IIdentityWriteStore writeStore,
        IJwtTokenService jwtTokenService,
        IIdentityEmailSender identityEmailSender,
        ILogger<AccountService> logger,
        IIdentityElementLookup elementLookup,
        IElementCalculator elementCalculator,
        IPasswordHasher passwordHasher,
        IPasswordResetTokenProvider passwordResetTokenProvider,
        IConfiguration configuration,
        IRefreshTokenPort refreshTokenPort)
    {
        ArgumentNullException.ThrowIfNull(readStore);
        ArgumentNullException.ThrowIfNull(writeStore);
        ArgumentNullException.ThrowIfNull(jwtTokenService);
        ArgumentNullException.ThrowIfNull(identityEmailSender);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(elementLookup);
        ArgumentNullException.ThrowIfNull(elementCalculator);
        ArgumentNullException.ThrowIfNull(passwordHasher);
        ArgumentNullException.ThrowIfNull(passwordResetTokenProvider);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(refreshTokenPort);

        _readStore = readStore;
        _writeStore = writeStore;
        _jwtTokenService = jwtTokenService;
        _identityEmailSender = identityEmailSender;
        _logger = logger;
        _elementLookup = elementLookup;
        _elementCalculator = elementCalculator;
        _passwordHasher = passwordHasher;
        _passwordResetTokenProvider = passwordResetTokenProvider;
        _configuration = configuration;
        _refreshTokenPort = refreshTokenPort;
    }

    public async Task<AuthenticationResult> AuthenticateAsync(AuthenticateRequest model)
    {
        var account = await _readStore.GetAccountByEmailAsync(model.Email ?? string.Empty);

        if (account == null)
        {
            return new AuthenticationResult { ErrorMessage = "Email not found." };
        }

        if (!VerifyCurrentPassword(account, model.Password))
        {
            return new AuthenticationResult { ErrorMessage = "Incorrect password." };
        }

        var upgradedToHash = UpgradeLegacyStoredPassword(account, model.Password!);

        if (account.Dob.HasValue)
        {
            // Re-derivation over STORED data: stay lenient so legacy rows cannot break login.
            account.ElementId = await GetElementIdFromDateOfBirth(account.Dob.Value.Year, account.Gender, account.AccountId, fromUserInput: false);
        }

        if (upgradedToHash || account.Dob.HasValue)
        {
            await _writeStore.UpdateAccountAsync(account);
            await _writeStore.SaveChangesAsync();
        }

        var token = _jwtTokenService.GenerateJwtToken(account);
        var refreshToken = await _refreshTokenPort.CreateForAccountAsync(account.AccountId);
        var response = new AuthenticateResponse(account, token)
        {
            RefreshToken = refreshToken,
            ExpiresInMinutes = _jwtTokenService.AccessTokenLifetimeMinutes
        };

        return new AuthenticationResult { Response = response };
    }

    public async Task<IEnumerable<Account>> GetAllAsync() => await _readStore.GetAllAccountsAsync();

    public async Task<Account?> GetByIdAsync(int id) => await _readStore.GetAccountByIdAsync(id);

    public async Task<Account> RegisterAsync(RegisterRequest model)
    {
        if (await _readStore.GetAccountByEmailAsync(model.Email ?? string.Empty) != null)
            throw new ApplicationException("Email '" + model.Email + "' is already taken");
        if (string.IsNullOrEmpty(model.Password))
            throw new ArgumentException("Password is required", nameof(model));

        var account = new Account
        {
            FullName = model.FullName,
            Email = model.Email,
            Password = _passwordHasher.Hash(model.Password),
            Dob = model.Dob.Date,
            Phone = model.Phone,
            CreateAt = DateTime.Now,
            UpdateAt = DateTime.Now,
            Gender = model.Gender,
            RoleId = 2
        };

        if (account.Dob.HasValue)
        {
            // Fresh user input: strict validation applies.
            account.ElementId = await GetElementIdFromDateOfBirth(account.Dob.Value.Year, model.Gender, account.AccountId, fromUserInput: true);
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

        // Fresh input is validated strictly; when the request carries no gender the stored
        // value drives a lenient re-derivation instead.
        var genderFromInput = !string.IsNullOrWhiteSpace(model.Gender);
        account.ElementId = await GetElementIdFromDateOfBirth(
            model.Dob?.Year ?? account.Dob?.Year ?? DateTime.Now.Year,
            genderFromInput ? model.Gender : account.Gender,
            account.AccountId,
            genderFromInput);

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

    public async Task<bool> ForgotPasswordAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return true;
        }

        var account = await _readStore.GetAccountByEmailAsync(email);
        if (account == null)
        {
            _logger.LogInformation("Password reset requested for an unknown email; no action taken");
            return true;
        }

        var token = _passwordResetTokenProvider.Generate();
        account.ResetTokenHash = _passwordResetTokenProvider.Hash(token);
        account.ResetTokenExpiresAt = DateTime.UtcNow.AddMinutes(ResetTokenLifetimeMinutes);

        await _writeStore.UpdateAccountAsync(account);
        await _writeStore.SaveChangesAsync();

        var resetLink = BuildResetLink(token);

        return await _identityEmailSender.SendPasswordResetEmailAsync(email, account.FullName, resetLink);
    }

    public async Task<bool> ResetPasswordAsync(ResetPasswordRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Token) || string.IsNullOrEmpty(request.NewPassword))
        {
            return false;
        }

        var tokenHash = _passwordResetTokenProvider.Hash(request.Token);
        var account = await _readStore.GetAccountByResetTokenHashAsync(tokenHash);

        if (account == null)
        {
            return false;
        }

        if (account.ResetTokenExpiresAt is not { } expiresAt || expiresAt <= DateTime.UtcNow)
        {
            return false;
        }

        account.Password = _passwordHasher.Hash(request.NewPassword);
        account.ResetTokenHash = null;
        account.ResetTokenExpiresAt = null;

        await _writeStore.UpdateAccountAsync(account);
        await _writeStore.SaveChangesAsync();

        // A password reset invalidates every outstanding session of the account.
        await _refreshTokenPort.RevokeAllForAccountAsync(account.AccountId);

        return true;
    }

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

            existedUser.Password = _passwordHasher.Hash(newPassword);
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
        if (!string.IsNullOrEmpty(account.Password) && _passwordHasher.NeedsRehash(account.Password))
        {
            account.Password = _passwordHasher.Hash(account.Password);
        }

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

            if (!VerifyCurrentPassword(account, currentPassword))
                return false;

            account.Password = _passwordHasher.Hash(newPassword);
            await _writeStore.UpdateAccountAsync(account);
            await _writeStore.SaveChangesAsync();

            // A password change invalidates every outstanding session of the account.
            await _refreshTokenPort.RevokeAllForAccountAsync(accountId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password for account id: {AccountId}", accountId);
            throw;
        }
    }

    /// <summary>
    /// Verifies a supplied password against the stored value. Legacy accounts seeded with a
    /// plaintext value (not starting with the bcrypt "$2" marker) fall back to a plaintext
    /// comparison; callers upgrade the stored value on successful match.
    /// </summary>
    private bool VerifyCurrentPassword(Account account, string? suppliedPassword)
    {
        var stored = account.Password;
        if (string.IsNullOrEmpty(stored) || string.IsNullOrEmpty(suppliedPassword))
        {
            return false;
        }

        // Documented legacy-fallback branch: stored values predating bcrypt hashing.
        if (!stored.StartsWith(BcryptHashPrefix, StringComparison.Ordinal))
        {
            return string.Equals(stored, suppliedPassword, StringComparison.Ordinal);
        }

        return _passwordHasher.Verify(suppliedPassword, stored);
    }

    /// <summary>
    /// Re-stores a verified password as a bcrypt hash when the stored value is still legacy
    /// plaintext or was hashed with a weaker work factor. Returns true when a write-through
    /// upgrade happened.
    /// </summary>
    private bool UpgradeLegacyStoredPassword(Account account, string verifiedPassword)
    {
        if (string.IsNullOrEmpty(account.Password) || !_passwordHasher.NeedsRehash(account.Password))
        {
            return false;
        }

        account.Password = _passwordHasher.Hash(verifiedPassword);
        return true;
    }

    private string BuildResetLink(string token)
    {
        var baseUrl = _configuration["AppSettings:FrontendBaseUrl"];
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            _logger.LogWarning(
                "AppSettings:FrontendBaseUrl is not configured; falling back to {DefaultBaseUrl} for password-reset links. Configure it to point at the frontend origin.",
                DefaultFrontendBaseUrl);
            baseUrl = DefaultFrontendBaseUrl;
        }

        return $"{baseUrl.TrimEnd('/')}/reset-password?token={Uri.EscapeDataString(token)}";
    }

    private async Task<int> GetElementIdFromDateOfBirth(int yearOfBirth, string? gender, int accountId, bool fromUserInput)
    {
        var isMale = fromUserInput ? ResolveIsMaleStrict(gender) : ResolveIsMaleLenient(gender, accountId);
        var elementName = _elementCalculator.CalculateElement(yearOfBirth, isMale);
        var elementId = await _elementLookup.GetElementIdByNameAsync(elementName);

        if (!elementId.HasValue)
        {
            _logger.LogError("Element not found for elementName: {ElementName}", elementName);
            throw new ApplicationException($"Element '{elementName}' not found in the database.");
        }

        return elementId.Value;
    }

    /// <summary>
    /// Input-boundary resolution (registration / profile-update payloads): an absent value keeps
    /// the documented legacy female default, recognized aliases map to their branch, and any other
    /// non-empty token is rejected so bad client data cannot persist.
    /// </summary>
    private static bool ResolveIsMaleStrict(string? gender)
    {
        if (string.IsNullOrWhiteSpace(gender))
        {
            return false;
        }

        if (!TryResolveGenderAlias(gender, out var isMale))
        {
            throw new ArgumentException($"Unknown gender value: '{gender}'.", nameof(gender));
        }

        return isMale;
    }

    /// <summary>
    /// Stored-data re-derivation (element refresh over previously persisted rows): unrecognized
    /// legacy values keep deriving via the female branch instead of failing login or update; a
    /// warning lets operations locate the dirty rows for cleanup.
    /// </summary>
    private bool ResolveIsMaleLenient(string? gender, int accountId)
    {
        if (string.IsNullOrWhiteSpace(gender))
        {
            return false;
        }

        if (TryResolveGenderAlias(gender, out var isMale))
        {
            return isMale;
        }

        _logger.LogWarning(
            "Unrecognized stored gender '{Gender}' for account {AccountId}; defaulting element derivation to female branch",
            gender,
            accountId);
        return false;
    }

    /// <summary>Shared alias table for both boundaries. Trim-tolerant and case-insensitive.</summary>
    private static bool TryResolveGenderAlias(string? gender, out bool isMale)
    {
        switch (gender?.Trim().ToLowerInvariant())
        {
            case "male":
            case "nam":
            case "m":
                isMale = true;
                return true;
            case "female":
            case "nữ":
            case "nu":
            case "f":
                isMale = false;
                return true;
            default:
                isMale = false;
                return false;
        }
    }
}