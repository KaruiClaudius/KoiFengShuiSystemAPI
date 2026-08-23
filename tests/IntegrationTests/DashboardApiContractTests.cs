using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using KoiFengShuiSystem.DataAccess.Models;
using KoiFengShuiSystem.Modules.Identity.Application.Abstractions;
using KoiFengShuiSystem.Modules.Identity.Domain.Entities;
using KoiFengShuiSystem.Shared.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace IntegrationTests;

/// <summary>
/// Pins the authorization matrix and response contract of the dashboard surface
/// after its port into the Community module: legacy routes stay admin-only, and
/// the new content-summary endpoint follows the same matrix.
/// </summary>
public class DashboardApiContractTests : IClassFixture<DashboardApiContractTests.DashboardApiFactory>
{
    private readonly DashboardApiFactory _factory;

    public DashboardApiContractTests(DashboardApiFactory factory)
    {
        _factory = factory;
        SeedContent();
    }

    [Fact]
    public async Task ContentSummary_AnonymousRequest_IsUnauthorized()
    {
        var response = await NewClient().GetAsync("/api/Dashboard/content-summary");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ContentSummary_MemberToken_IsForbidden()
    {
        using var client = NewClient();
        AuthorizeAs(client, roleId: 2);

        var response = await client.GetAsync("/api/Dashboard/content-summary");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ContentSummary_AdminToken_ReturnsContentReport()
    {
        using var client = NewClient();
        AuthorizeAs(client, roleId: 1);

        var response = await client.GetAsync("/api/Dashboard/content-summary");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(2, payload.GetProperty("totalPosts").GetInt32());
        Assert.Equal(1, payload.GetProperty("pendingCount").GetInt32());

        var byCategory = payload.GetProperty("byCategory");
        Assert.Equal(1, byCategory.GetArrayLength());
        var entry = byCategory[0];
        Assert.Equal(7700, entry.GetProperty("categoryId").GetInt32());
        Assert.Equal("Dashboard Seed", entry.GetProperty("categoryName").GetString());
        Assert.Equal(2, entry.GetProperty("count").GetInt32());
    }

    // The port must not disturb routing of the original three endpoints.
    [Fact]
    public async Task AdminToken_CanReachAllLegacyDashboardRoutes()
    {
        using var client = NewClient();
        AuthorizeAs(client, roleId: 1);

        foreach (var url in new[]
                 {
                     "/api/Dashboard/new-users-count?days=30",
                     "/api/Dashboard/new-users-list?days=30",
                     "/api/Dashboard/traffic-distribution"
                 })
        {
            var response = await client.GetAsync(url);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }

    private HttpClient NewClient() => _factory.CreateClient();

    private void AuthorizeAs(HttpClient client, int roleId)
    {
        using var scope = _factory.Services.CreateScope();
        var tokenService = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
        var token = tokenService.GenerateJwtToken(new Account
        {
            AccountId = 300 + roleId,
            Email = $"dashboard-user{roleId}@test.local",
            RoleId = roleId
        });
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);
    }

    // Idempotent seed on high ids so collisions with other fixtures stay impossible.
    private void SeedContent()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<KoiFengShuiContext>();

        if (!context.PostCategories.Any(category => category.Id == 7700))
        {
            context.PostCategories.Add(new PostCategory { Id = 7700, PostType = "Dashboard Seed" });
            context.SaveChanges();
        }

        if (!context.Posts.Any(post => post.PostId >= 7701 && post.PostId <= 7702))
        {
            context.Posts.AddRange(
                new Post
                {
                    PostId = 7701,
                    PostCategoryId = 7700,
                    Name = "Seed published",
                    Description = "Body",
                    Status = "Published",
                    AccountId = 301,
                    CreateAt = DateTime.UtcNow,
                    UpdateAt = DateTime.UtcNow
                },
                new Post
                {
                    PostId = 7702,
                    PostCategoryId = 7700,
                    Name = "Seed pending",
                    Description = "Body",
                    Status = "Pending",
                    AccountId = 302,
                    CreateAt = DateTime.UtcNow,
                    UpdateAt = DateTime.UtcNow
                });
            context.SaveChanges();
        }
    }

    // Own database name: EF InMemory stores are process-global and name-keyed,
    // so reusing ApiTestFactory's "TestDb" would let other fixtures' posts leak
    // into the absolute content counts asserted above.
    public class DashboardApiFactory : ApiTestFactory
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);

            builder.ConfigureServices(services =>
            {
                for (var i = services.Count - 1; i >= 0; i--)
                {
                    var descriptor = services[i];
                    if (descriptor.ServiceType == typeof(DbContextOptions<KoiFengShuiContext>) ||
                        descriptor.ServiceType == typeof(KoiFengShuiContext))
                    {
                        services.RemoveAt(i);
                    }
                }

                services.AddDbContext<KoiFengShuiContext>(options =>
                    options.UseInMemoryDatabase("DashboardApiContractTests"));
            });
        }
    }
}
