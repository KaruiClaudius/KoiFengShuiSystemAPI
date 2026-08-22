using KoiFengShuiSystem.Modules.Identity.Domain.Entities;
using Microsoft.Extensions.Configuration;

namespace KoiFengShuiSystem.Modules.Identity.Application.Services;

public class AdminAccountService
{
    private readonly IAccountService _accountService;
    private readonly IConfiguration _configuration;

    public AdminAccountService(IAccountService accountService, IConfiguration configuration)
    {
        _accountService = accountService;
        _configuration = configuration;
    }

    public async Task EnsureAdminAccountExistsAsync()
    {
        var adminEmail = _configuration["AdminAccount:Email"];
        var adminPassword = _configuration["AdminAccount:Password"];

        if (string.IsNullOrEmpty(adminEmail) || string.IsNullOrEmpty(adminPassword))
        {
            throw new InvalidOperationException("Admin credentials are not properly configured.");
        }

        var existingAdmin = await _accountService.GetAccountByEmailAsync(adminEmail);

        if (existingAdmin == null)
        {
            var newAdmin = new Account
            {
                Email = adminEmail,
                Password = adminPassword,
                FullName = "System Administrator",
                RoleId = 1,
                Dob = DateTime.Now,
                Gender = "male",
                CreateAt = DateTime.Now,
                UpdateAt = DateTime.Now,
                Phone = "0379499630"
            };

            await _accountService.CreateAsync(newAdmin);
            await _accountService.UpdateUserPasswordAsync(newAdmin, adminPassword);
        }
    }
}
