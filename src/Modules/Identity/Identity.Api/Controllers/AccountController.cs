using KoiFengShuiSystem.Api.Authorization;
using KoiFengShuiSystem.Modules.Identity.Application.Requests;
using KoiFengShuiSystem.Modules.Identity.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using IdentityAccountService = KoiFengShuiSystem.Modules.Identity.Application.Services.AccountService;

namespace KoiFengShuiSystem.Modules.Identity.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class AccountController : Controller
{
    private readonly IAccountService _accountService;
    private readonly ILogger<IdentityAccountService> _logger;

    public AccountController(IAccountService accountService, ILogger<IdentityAccountService> logger)
    {
        _accountService = accountService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var users = await _accountService.GetAllAsync();
        return Ok(users);
    }

    [Authorize]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var account = await _accountService.GetByIdAsync(id);
        return account == null ? NotFound() : Ok(account);
    }

    [Authorize]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, UpdateRequest model)
    {
        try
        {
            await _accountService.UpdateAsync(id, model);
            return Ok(new { message = "Account updated successfully" });
        }
        catch (ApplicationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _accountService.DeleteAsync(id);
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
            _logger.LogError(ex, "Error retrieving account for email: {Email}", email);
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
            _logger.LogError(ex, "Error changing password for account {AccountId}", id);
            return StatusCode(500, new { message = "An unexpected error occurred while changing the password" });
        }
    }
}
