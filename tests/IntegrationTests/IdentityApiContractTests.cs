using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using KoiFengShuiSystem.Api.Controllers;
using KoiFengShuiSystem.Modules.Identity.Application.Abstractions;
using KoiFengShuiSystem.Modules.Identity.Domain.Entities;
using KoiFengShuiSystem.Shared.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.DependencyInjection;

namespace IntegrationTests;

public class IdentityApiContractTests : IClassFixture<IdentityApiContractTests.IdentityOnlyApiTestFactory>
{
    private readonly HttpClient _client;
    private readonly IdentityOnlyApiTestFactory _factory;

    public IdentityApiContractTests(IdentityOnlyApiTestFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Swagger_ContainsIdentityRoutes_WhenLegacyApiControllersAreRemoved()
    {
        var response = await _client.GetAsync("/swagger/v1/swagger.json");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var paths = document.RootElement.GetProperty("paths");

        Assert.True(paths.TryGetProperty("/api/Auth/SignIn", out _));
        Assert.True(paths.TryGetProperty("/api/Auth/SignUp", out _));
        Assert.True(paths.TryGetProperty("/api/Auth/ForgotPassword", out _));
        Assert.True(paths.TryGetProperty("/api/Auth/google-login", out _));
        Assert.True(paths.TryGetProperty("/api/Auth/profile-status", out _));
        Assert.True(paths.TryGetProperty("/api/Auth/refresh", out _));
        Assert.True(paths.TryGetProperty("/api/Auth/logout", out _));
        Assert.True(paths.TryGetProperty("/api/Account", out _));
        Assert.True(paths.TryGetProperty("/api/Account/{id}", out _));
        Assert.True(paths.TryGetProperty("/api/Account/email/{email}", out _));
        Assert.True(paths.TryGetProperty("/api/Account/{id}/change-password", out _));
    }

    // --- GET api/Auth/profile-status ---
    //
    // Contract: { "requiresProfileCompletion": bool } — true while date of birth or
    // gender is missing on the signed-in account (fresh Google accounts start there).

    [Fact]
    public async Task ProfileStatus_AnonymousRequest_IsUnauthorized()
    {
        var response = await _client.GetAsync("/api/Auth/profile-status");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProfileStatus_AccountWithoutDobOrGender_ReportsCompletionRequired()
    {
        SeedProfileStatusAccounts();
        var response = await SendProfileStatusRequestAsync(accountId: 910001);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(payload.GetProperty("requiresProfileCompletion").GetBoolean());
    }

    [Fact]
    public async Task ProfileStatus_AccountWithDobAndGender_ReportsNoCompletionRequired()
    {
        SeedProfileStatusAccounts();
        var response = await SendProfileStatusRequestAsync(accountId: 910002);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(payload.GetProperty("requiresProfileCompletion").GetBoolean());
    }

    [Fact]
    public async Task ProfileStatus_TokenForUnknownAccount_IsUnauthorized()
    {
        var response = await SendProfileStatusRequestAsync(accountId: 919999);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private async Task<HttpResponseMessage> SendProfileStatusRequestAsync(int accountId)
    {
        using var scope = _factory.Services.CreateScope();
        var tokenService = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
        var token = tokenService.GenerateJwtToken(new Account
        {
            AccountId = accountId,
            Email = $"profile-status.{accountId}@test.local",
            RoleId = 2
        });

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/Auth/profile-status");
        request.Headers.Authorization = new("Bearer", token);
        return await _client.SendAsync(request);
    }

    private void SeedProfileStatusAccounts()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<KoiFengShuiContext>();

        if (!context.Accounts.Any(account => account.AccountId == 910001))
        {
            context.Accounts.Add(new Account
            {
                AccountId = 910001,
                FullName = "Google Incomplete",
                Email = "profile-status.incomplete@test.local",
                CreateAt = DateTime.UtcNow,
                UpdateAt = DateTime.UtcNow,
                RoleId = 2
            });
        }

        if (!context.Accounts.Any(account => account.AccountId == 910002))
        {
            context.Accounts.Add(new Account
            {
                AccountId = 910002,
                FullName = "Native Complete",
                Email = "profile-status.complete@test.local",
                Dob = new DateTime(1990, 6, 15),
                Gender = "female",
                CreateAt = DateTime.UtcNow,
                UpdateAt = DateTime.UtcNow,
                RoleId = 2
            });
        }

        context.SaveChanges();
    }

    public class IdentityOnlyApiTestFactory : ApiTestFactory
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);

            var legacyApiAssemblyName = typeof(FAQController).Assembly.GetName().Name;

            builder.ConfigureServices(services =>
            {
                services.AddControllers().ConfigureApplicationPartManager(manager =>
                {
                    var legacyParts = manager.ApplicationParts
                        .Where(part => string.Equals(part.Name, legacyApiAssemblyName, StringComparison.Ordinal))
                        .ToArray();

                    foreach (var legacyPart in legacyParts)
                    {
                        manager.ApplicationParts.Remove(legacyPart);
                    }
                });
            });
        }
    }
}
