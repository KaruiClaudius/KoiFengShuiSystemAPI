﻿using KoiFengShuiSystem.Api.Authorization;
using KoiFengShuiSystem.BusinessLogic.Services.Interface;
using KoiFengShuiSystem.DataAccess.Models;
using KoiFengShuiSystem.Shared.Models.Request;
using KoiFengShuiSystem.Shared.Models.Response;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Text.Json;
using KoiFengShuiSystem.Shared.Helpers;
using KoiFengShuiSystem.BusinessLogic.Services.Implement;

namespace KoiFengShuiSystem.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAccountService _accountService;
        private readonly IJwtUtils _jwtUtils;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAccountService accountService, IJwtUtils jwtUtils, IHttpClientFactory httpClientFactory, ILogger<AuthController> logger)
        {
            _accountService = accountService;
            _jwtUtils = jwtUtils;
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

            var response = result.Response;
            return Ok(new
            {
                Token = response.Token,
                Email = response.Email
            });
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
                var account = await _accountService.GetAccountByEmailAsync(request.Email);
                if (account == null)
                {
                    // Consider returning Ok here to prevent email enumeration attacks
                    return Ok("If a user with this email exists, a password reset email has been sent.");
                }

                // Generate new password
                var newPassword = SecurityUtil.GenerateRandomPassword();

                // Update the user's password
                await _accountService.UpdateUserPasswordAsync(account, newPassword);

                // Send email with new password
                var emailSent = await _accountService.SendPasswordResetEmailAsync(request.Email, account.FullName, newPassword);
                if (!emailSent)
                {
                    throw new ApplicationException("Failed to send the email.");
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
        [HttpPost("google-login")]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequest request)
        {
            try
            {
                _logger.LogInformation($"Received Google login request for token: {request.AccessToken[..10]}...");

                var httpClient = _httpClientFactory.CreateClient();
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", request.AccessToken);

                var response = await httpClient.GetAsync("https://www.googleapis.com/oauth2/v3/userinfo");
                _logger.LogInformation($"Google API response status: {response.StatusCode}");

                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();

                var googleUser = JsonSerializer.Deserialize<GoogleUserInfo>(content);
                _logger.LogInformation($"Deserialized Google user info: {JsonSerializer.Serialize(googleUser)}");

                var account = await _accountService.GetAccountByEmailAsync(googleUser.Email);
                if (account == null)
                {
                    _logger.LogInformation($"Creating new account for email: {googleUser.Email}");
                    var defaultPassword = SecurityUtil.GenerateRandomPassword();
                    account = new Account
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
                    _logger.LogInformation($"New account created with ID: {account.AccountId}");

                    // Send email with default password
                    var emailSent = await _accountService.SendDefaultPasswordAsync(googleUser.Email, googleUser.Name, defaultPassword);
                    if (emailSent)
                    {
                        _logger.LogInformation($"Email sent successfully to {googleUser.Email}");
                    }
                    else
                    {
                        _logger.LogWarning($"Failed to send email to {googleUser.Email}");
                    }
                }
                else
                {
                    _logger.LogInformation($"Existing account found for email: {googleUser.Email}");
                }

                var token = _jwtUtils.GenerateJwtToken(account);
                _logger.LogInformation($"JWT token generated successfully");

                return Ok(new AuthenticateResponse(account, token));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during Google login");
                return StatusCode(500, "An unexpected error occurred");
            }
        }
    }
}
