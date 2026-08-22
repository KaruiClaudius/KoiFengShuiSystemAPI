using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using KoiFengShuiSystem.Api.Authorization;
using KoiFengShuiSystem.BusinessLogic.Services.Implement;
using KoiFengShuiSystem.Modules.Identity.Application.Abstractions;
using KoiFengShuiSystem.Modules.Identity.Application.Requests;
using KoiFengShuiSystem.Modules.Identity.Application.Responses;
using KoiFengShuiSystem.Modules.Identity.Application.Services;
using KoiFengShuiSystem.Modules.Identity.Domain.Entities;
using KoiFengShuiSystem.Shared.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using AccountEntity = KoiFengShuiSystem.Modules.Identity.Domain.Entities.Account;

namespace KoiFengShuiSystem.Modules.Identity.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAccountService _accountService;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IRefreshTokenPort _refreshTokenPort;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IAccountService accountService,
        IJwtTokenService jwtTokenService,
        IRefreshTokenPort refreshTokenPort,
        IHttpClientFactory httpClientFactory,
        ILogger<AuthController> logger)
    {
        _accountService = accountService;
        _jwtTokenService = jwtTokenService;
        _refreshTokenPort = refreshTokenPort;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

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
                var defaultPassword = SecurityUtil.GenerateRandomPassword();
                account = new AccountEntity
                {
                    Email = googleUser.Email,
                    FullName = googleUser.Name,
                    Password = defaultPassword,
                    Dob = DateTime.Now,
                    Gender = "male",
                    CreateAt = DateTime.Now,
                    UpdateAt = DateTime.Now,
                    RoleId = 2,
                };
                await _accountService.CreateAsync(account);
                _logger.LogInformation("Created new Google login account with account ID {AccountId}", account.AccountId);

                var emailSent = await _accountService.SendDefaultPasswordAsync(googleUser.Email, googleUser.Name, defaultPassword);
                if (emailSent)
                {
                    _logger.LogInformation("Sent default password email for Google login account ID {AccountId}", account.AccountId);
                }
                else
                {
                    _logger.LogWarning("Failed to send default password email for Google login account ID {AccountId}", account.AccountId);
                }
            }
            else
            {
                _logger.LogInformation("Matched existing Google login account with account ID {AccountId}", account.AccountId);
            }

            var token = _jwtTokenService.GenerateJwtToken(account);
            _logger.LogInformation("JWT token generated successfully.");

            var refreshToken = await _refreshTokenPort.CreateForAccountAsync(account.AccountId);

            return Ok(new AuthenticateResponse(account, token)
            {
                RefreshToken = refreshToken,
                ExpiresInMinutes = _jwtTokenService.AccessTokenLifetimeMinutes
            });
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
    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
    {
        if (string.IsNullOrWhiteSpace(request?.RefreshToken))
        {
            return Unauthorized();
        }

        var rotation = await _refreshTokenPort.RotateAsync(request.RefreshToken);
        if (!rotation.Success || rotation.AccountId is not { } accountId)
        {
            return Unauthorized();
        }

        var account = await _accountService.GetByIdAsync(accountId);
        if (account == null)
        {
            return Unauthorized();
        }

        var accessToken = _jwtTokenService.GenerateJwtToken(account);

        return Ok(new
        {
            token = accessToken,
            refreshToken = rotation.NewRawToken,
            expiresIn = _jwtTokenService.AccessTokenLifetimeMinutes
        });
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

        await _refreshTokenPort.RevokeAllForAccountAsync(accountId.Value);

        return NoContent();
    }

    private static int? ResolveAccountId(ClaimsPrincipal user)
    {
        var value = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? user.FindFirst("id")?.Value;

        return int.TryParse(value, out var accountId) ? accountId : null;
    }
}
