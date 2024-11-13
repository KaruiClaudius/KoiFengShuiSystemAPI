using KoiFengShuiSystem.Api.Authorization;
using KoiFengShuiSystem.BusinessLogic.Services.Implement;
using KoiFengShuiSystem.BusinessLogic.Services.Interface;
using KoiFengShuiSystem.Shared.Models.Request;
using KoiFengShuiSystem.Shared.Models.Response;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

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

        [HttpGet]
        public IActionResult GetAll()
        {
            var users = _accountService.GetAllAsync();
            return Ok(users);
        }
        [Authorize]
        [HttpGet("{id}")]
        public IActionResult GetById(int id)
        {
            var account = _accountService.GetByIdAsync(id);
            return account == null ? NotFound() : Ok(account);
        }



        [Authorize]
        [HttpPut("{id}")]
        public IActionResult Update(int id, UpdateRequest model)
        {
            try
            {
                _accountService.UpdateAsync(id, model);
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
                _accountService.DeleteAsync(id);
                return Ok(new { message = "Account deleted successfully" });
            }
            catch (ApplicationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }


        [HttpGet("email/{email}")]
        public async Task<IActionResult> GetByEmail(string email)
        {
            try
            {
                var account = await _accountService.GetAccountByEmailAsync(email);
                if (account == null)
                {
                    return NotFound(new { message = "Account not found" });
                }
                return Ok(account);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving account for email: {email}");
                return StatusCode(500, new { message = "An error occurred while retrieving the account" });
            }
        }

        [HttpPut("{id}/change-password")]
        public async Task<IActionResult> ChangePassword(int id, [FromBody] ChangePasswordRequest model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var success = await _accountService.ChangePasswordAsync(id, model.CurrentPassword, model.NewPassword);
                if (!success)
                {
                    return BadRequest(new { message = "Current password is incorrect" });
                }

                return Ok(new { message = "Password changed successfully" });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "Account not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error changing password for account {id}");
                return StatusCode(500, new { message = "An unexpected error occurred while changing the password" });
            }
        }

        [HttpPost("UpdateWalletAfterPosted")]
        //[Authorize(Roles ="Admin")]
        public async Task<IActionResult> UpdateUserWalletAfterPosted(int accountId, decimal amount)
        {
            try
            {
                if (accountId <= 0)
                {
                    return BadRequest("Invalid user ID");
                }

                var existingUser = await _accountService.GetByIdAsync(accountId);

                if (existingUser == null)
                {
                    return NotFound("User not found");
                }  

                var updatedUser = await _accountService.UpdateUserWalletAfterPosted(existingUser, amount);

                if (updatedUser)
                {
                    return Ok("Update Wallet Success");
                }
                else
                {
                    return BadRequest("Failed to update wallet");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Internal server error: " + ex.Message);
            }
        }
    }



}



