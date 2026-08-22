using System.Net;
using System.Text.Json;
using KoiFengShuiSystem.Api.Controllers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.DependencyInjection;

namespace IntegrationTests;

public class IdentityApiContractTests : IClassFixture<IdentityApiContractTests.IdentityOnlyApiTestFactory>
{
    private readonly HttpClient _client;

    public IdentityApiContractTests(IdentityOnlyApiTestFactory factory)
    {
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
        Assert.True(paths.TryGetProperty("/api/Account", out _));
        Assert.True(paths.TryGetProperty("/api/Account/{id}", out _));
        Assert.True(paths.TryGetProperty("/api/Account/email/{email}", out _));
        Assert.True(paths.TryGetProperty("/api/Account/{id}/change-password", out _));
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
