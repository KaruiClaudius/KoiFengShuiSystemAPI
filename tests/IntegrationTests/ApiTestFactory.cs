using KoiFengShuiSystem.Modules.FengShui.Domain.Entities;
using KoiFengShuiSystem.Shared.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace IntegrationTests;

public class ApiTestFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Server=.;Database=TestDb;Trusted_Connection=true;TrustServerCertificate=true;",
                ["AppSettings:Secret"] = "test-secret-key-that-is-at-least-32-bytes-long-for-hmac",
                ["AppSettings:Issuer"] = "KoiFengShuiSystem",
                ["AppSettings:Audience"] = "KoiFengShuiSystemClients",
                ["AppSettings:AccessTokenMinutes"] = "15",
                ["AppSettings:RefreshTokenDays"] = "30",
                ["CloundSettings:CloundName"] = "test-clound-name",
                ["CloundSettings:CloundKey"] = "test-clound-key",
                ["CloundSettings:CloundSecret"] = "test-clound-secret",
                ["MailSettings:UserName"] = "test-mail-user",
                ["MailSettings:Password"] = "test-mail-password",
                ["RateLimiting:GlobalPermitPerMinute"] = "10000",
                ["RateLimiting:AuthPermitPerMinute"] = "2",
                ["RateLimiting:ComputePermitPerMinute"] = "2"
            });
        });

        builder.ConfigureServices(services =>
        {
            // EF Core 9+/10 defers provider choice via IDbContextOptionsConfiguration<T>;
            // both it and the resolved options must be stripped before swapping Npgsql→InMemory.
            services.RemoveAll(typeof(DbContextOptions<KoiFengShuiContext>));
            services.RemoveAll(typeof(IDbContextOptionsConfiguration<KoiFengShuiContext>));
            services.RemoveAll<KoiFengShuiContext>();

            services.AddDbContext<KoiFengShuiContext>(options =>
                options.UseInMemoryDatabase("TestDb"));
        });
    }
}
