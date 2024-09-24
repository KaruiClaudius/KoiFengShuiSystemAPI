using KoiFengShuiSystem.Api.Authorization;
using KoiFengShuiSystem.BusinessLogic.Services.Implement;
using KoiFengShuiSystem.BusinessLogic.Services.Interface;
using KoiFengShuiSystem.Shared.Models.Request;
using KoiFengShuiSystem.Shared.Models.Response;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KoiFengShuiSystem.Api.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class AccountController : Controller
    {
        private IAccountService _accountService;
        private readonly ILogger<AccountService> _logger;

        public AccountController(IAccountService accountService, ILogger<AccountService> logger)
        {
            _accountService = accountService;
            _logger = logger;
        }

        [AllowAnonymous]
        [HttpPost("authenticate")]
        public IActionResult Authenticate(AuthenticateRequest model)
        {
            var response = _accountService.Authenticate(model);

            if (response == null)
                return BadRequest(new { message = "Username or password is incorrect" });

            return Ok(response);
        }

        [HttpGet]
        public IActionResult GetAll()
        {
            var users = _accountService.GetAll();
            return Ok(users);
        }
        [Authorize]
        [HttpGet("{id}")]
        public IActionResult GetById(int id)
        {
            var account = _accountService.GetById(id);
            return account == null ? NotFound() : Ok(account);
        }

        [AllowAnonymous]
        [HttpPost("register")]
        public IActionResult Register(Shared.Models.Request.RegisterRequest model)
        {
            try
            {
                var account = _accountService.Register(model);
                return Ok(account);
            }
            catch (ApplicationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [Authorize]
        [HttpPut("{id}")]
        public IActionResult Update(int id, UpdateRequest model)
        {
            try
            {
                _accountService.Update(id, model);
                return Ok(new { message = "Account updated successfully" });
            }
            catch (ApplicationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [Authorize]
        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            try
            {
                _accountService.Delete(id);
                return Ok(new { message = "Account deleted successfully" });
            }
            catch (ApplicationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("logout")]
        public IActionResult Logout()
        {
            // Clear the user's session data
            HttpContext.Session.Clear();

            // Return a success response
            return Ok(new { message = "Logged out successfully" });
        }

        [AllowAnonymous]
        [HttpPost("ForgotPassword")]
        public async Task<IActionResult> ForgotPassword(Shared.Models.Request.ForgotPasswordRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.Email))
            {
                return BadRequest("Email is required.");
            }

            try
            {
                var account = await _accountService.GetAccountByEmail(request.Email);
                if (account == null)
                {
                    // Consider returning Ok here to prevent email enumeration attacks
                    return Ok("If a user with this email exists, a password reset email has been sent.");
                }

                // Generate new password
                var newPassword = SecurityUtil.GenerateRandomPassword();

                // Update the user's password
                await _accountService.UpdateUserPassword(account, newPassword);

                // Send email with new password
                var emailSent = await _accountService.SendPasswordResetEmail(request.Email, account.FullName, newPassword);
                if (!emailSent)
                {
                    throw new ApplicationException("Failed to send the email.");
                }

                return Ok("If a user with this email exists, a password reset email has been sent.");
            }
            catch (KeyNotFoundException)
            {
                // User not found, but we don't want to reveal this information
                return Ok("If a user with this email exists, a password reset email has been sent.");
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error occurred while resetting password");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
            }
            catch (ApplicationException ex)
            {
                _logger.LogError(ex, "Application error occurred while resetting password");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error occurred while resetting password");
                return StatusCode(StatusCodes.Status500InternalServerError, "An unexpected error occurred.");
            }
        }

    }
}


