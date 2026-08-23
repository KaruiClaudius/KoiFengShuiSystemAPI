using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using KoiFengShuiSystem.Api.Authorization;
using KoiFengShuiSystem.BusinessLogic.Services.Implement;
using KoiFengShuiSystem.Modules.Identity.Application.Requests;
using KoiFengShuiSystem.Modules.Identity.Application.Services;
using KoiFengShuiSystem.Shared.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;
using AccountEntity = KoiFengShuiSystem.Modules.Identity.Domain.Entities.Account;

namespace KoiFengShuiSystem.Modules.Identity.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAccountService _accountService;
    private readonly SessionIssuer _sessionIssuer;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IAccountService accountService,
        SessionIssuer sessionIssuer,
        IHttpClientFactory httpClientFactory,
        ILogger<AuthController> logger)
    {
        _accountService = accountService;
        _sessionIssuer = sessionIssuer;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    [EnableRateLimiting("auth")]
    [AllowAnonymous]
    [HttpPost("SignIn")]
    public async Task<IActionResult> Authenticate(AuthenticateRequest model)
    {
        var result = await _accountService.AuthenticateAsync(model);

        if (!result.Success)
        {
            return BadRequest(new { message = result.ErrorMessage });
        }

        return Ok(result.Response);
    }

    [EnableRateLimiting("auth")]
    [AllowAnonymous]
    [HttpPost("SignUp")]
    public async Task<IActionResult> Register(RegisterRequest model)
    {
        try
        {
            var account = await _accountService.RegisterAsync(model);
            return Ok(account);
        }
        catch (ApplicationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [EnableRateLimiting("auth")]
    [AllowAnonymous]
    [HttpPost("ForgotPassword")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request)
    {
        if (request == null || string.IsNullOrEmpty(request.Email))
        {
            return BadRequest("Email is required.");
        }

        try
        {
            var emailSent = await _accountService.ForgotPasswordAsync(request.Email);
            if (!emailSent)
            {
                return StatusCode(500, "An unexpected error occurred");
            }

            return Ok("If a user with this email exists, a password reset email has been sent.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ForgotPassword");
            return StatusCode(500, "An unexpected error occurred");
        }
    }

    [EnableRateLimiting("auth")]
    [AllowAnonymous]
    [HttpPost("ResetPassword")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var success = await _accountService.ResetPasswordAsync(request);
            if (!success)
            {
                return BadRequest(new { message = "Invalid or expired reset token." });
            }

            return Ok(new { message = "Password has been reset successfully." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ResetPassword");
            return StatusCode(500, "An unexpected error occurred");
        }
    }

    [EnableRateLimiting("auth")]
    [AllowAnonymous]
    [HttpPost("google-login")]
    public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequest request)
    {
        try
        {
            _logger.LogInformation("Received Google login request.");

            var httpClient = _httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", request.AccessToken);

            var response = await httpClient.GetAsync("https://www.googleapis.com/oauth2/v3/userinfo");
            _logger.LogInformation("Google userinfo request completed with status code {StatusCode}", response.StatusCode);

            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();

            var googleUser = JsonSerializer.Deserialize<GoogleUserInfo>(content);

            var account = await _accountService.GetAccountByEmailAsync(googleUser.Email);
            if (account == null)
            {
                _logger.LogInformation("Creating new Google login account.");
                account = new AccountEntity
                {
                    Email = googleUser.Email,
                    FullName = googleUser.Name,
                    // Passwordless by design: no default password is generated, stored or emailed.
                    // Password stays null so password sign-in cannot succeed for these accounts;
                    // users authenticate through Google and receive the standard token pair.
                    // Gender/Dob stay null until the user completes their profile (see profile-status),
                    // which also keeps element derivation skipped until a real date of birth exists.
                    CreateAt = DateTime.Now,
                    UpdateAt = DateTime.Now,
                    RoleId = 2,
                };
                await _accountService.CreateAsync(account);
                _logger.LogInformation("Created new Google login account with account ID {AccountId}", account.AccountId);
            }
            else
            {
                _logger.LogInformation("Matched existing Google login account with account ID {AccountId}", account.AccountId);
            }

            var session = await _sessionIssuer.IssueForAccountAsync(account);
            _logger.LogInformation("Issued session for Google login account ID {AccountId}.", account.AccountId);

            return Ok(session);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during Google login");
            return StatusCode(500, "An unexpected error occurred");
        }
    }

    /// <summary>
    /// Exchanges a valid refresh token for a new access/refresh pair. Reuse of an
    /// already-consumed refresh token is rejected with 401 (and revokes the account's
    /// remaining tokens inside the port).
    /// </summary>
    [EnableRateLimiting("auth")]
    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
    {
        if (string.IsNullOrWhiteSpace(request?.RefreshToken))
        {
            return Unauthorized();
        }

        var refreshed = await _sessionIssuer.RotateAndIssueAsync(
            request.RefreshToken,
            accountId => _accountService.GetByIdAsync(accountId));
        if (refreshed == null)
        {
            return Unauthorized();
        }

        return Ok(refreshed);
    }

    /// <summary>
    /// Revokes every outstanding refresh token of the signed-in account.
    /// </summary>
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var accountId = ResolveAccountId(User);
        if (accountId == null)
        {
            return Unauthorized();
        }

        await _sessionIssuer.RevokeAllForAccountAsync(accountId.Value);

        return NoContent();
    }

    /// <summary>
    /// Reports whether the signed-in account still needs to complete required profile data.
    /// Response shape: <c>{ "requiresProfileCompletion": true|false }</c>.
    /// Completion is required while date of birth or gender has not been provided yet —
    /// accounts created through Google login start without both and are passwordless.
    /// The frontend should drive completion through the existing profile-update endpoint
    /// before treating onboarding as finished.
    /// </summary>
    [HttpGet("profile-status")]
    public async Task<IActionResult> GetProfileStatus()
    {
        var accountId = ResolveAccountId(User);
        if (accountId == null)
        {
            return Unauthorized();
        }

        var account = await _accountService.GetByIdAsync(accountId.Value);
        if (account == null)
        {
            return Unauthorized();
        }

        return Ok(new
        {
            requiresProfileCompletion = account.Dob == null || account.Gender == null
        });
    }

    private static int? ResolveAccountId(ClaimsPrincipal user)
    {
        var value = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? user.FindFirst("id")?.Value;

        return int.TryParse(value, out var accountId) ? accountId : null;
    }
}
