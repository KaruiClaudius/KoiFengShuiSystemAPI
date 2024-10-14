using KoiFengShuiSystem.BusinessLogic.Services.Interface;
using KoiFengShuiSystem.DataAccess.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;

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
                RoleId = 1, // Assuming 1 is the admin role
                Dob = DateTime.Now,
                Gender = "Other",
                CreateAt = DateTime.Now,
                UpdateAt = DateTime.Now,
                Phone = "0379499630"
            };

            await _accountService.CreateAsync(newAdmin);
            await _accountService.UpdateUserPassword(newAdmin, adminPassword);

            Console.WriteLine($"Admin account created with email: {adminEmail}");
        }
    }
}
