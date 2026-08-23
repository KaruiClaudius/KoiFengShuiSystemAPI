using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using KoiFengShuiSystem.DataAccess.Models;
using KoiFengShuiSystem.Modules.FengShui.Domain.Entities;
using KoiFengShuiSystem.Modules.Identity.Application.Abstractions;
using KoiFengShuiSystem.Modules.Identity.Domain.Entities;
using KoiFengShuiSystem.Shared.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace IntegrationTests;

/// <summary>
/// Pins the public authorization matrix of the legacy API surface:
/// every admin-only or authenticated endpoint must reject anonymous
/// requests before reaching business logic, while public reads stay open.
/// </summary>
public class SecurityMatrixTests : IClassFixture<SecurityMatrixTests.SecurityMatrixFactory>
{
    private readonly SecurityMatrixFactory _factory;

    public SecurityMatrixTests(SecurityMatrixFactory factory)
    {
        _factory = factory;
        SeedCategory(1);
        SeedFaq(1);
        SeedPost(1);
    }

    private HttpClient NewClient() => _factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false
    });

    public static TheoryData<HttpMethod, string> ProtectedEndpoints => new()
    {
        { HttpMethod.Get, "/api/Dashboard/new-users-count" },
        { HttpMethod.Get, "/api/Dashboard/new-users-list" },
        { HttpMethod.Get, "/api/Dashboard/traffic-distribution" },
        { HttpMethod.Get, "/api/AdminPost/GetAllPosts" },
        { HttpMethod.Post, "/api/AdminPost/CreatePostWithImages" },
        { HttpMethod.Put, "/api/AdminPost/UpdatePost/1" },
        { HttpMethod.Delete, "/api/AdminPost/DeletePostWithAllRelated/1" },
        { HttpMethod.Post, "/api/FAQ/Create" },
        { HttpMethod.Put, "/api/FAQ/Update/1" },
        { HttpMethod.Delete, "/api/FAQ/Delete/1" },
        { HttpMethod.Post, "/api/Post/Create" },
        { HttpMethod.Delete, "/api/Post/Delete/1" },
        { HttpMethod.Post, "/api/UploadImage/UploadFile" }
    };

    [Theory]
    [MemberData(nameof(ProtectedEndpoints))]
    public async Task Anonymous_Request_ToProtectedEndpoint_IsUnauthorized(HttpMethod method, string url)
    {
        using var client = NewClient();
        using var request = new HttpRequestMessage(method, url);
        if (method == HttpMethod.Post || method == HttpMethod.Put)
        {
            request.Content = JsonContent.Create(new { });
        }

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Admin_Token_CanAccessDashboardEndpoints()
    {
        using var client = NewClient();
        AuthorizeAs(client, roleId: 1);

        var response = await client.GetAsync("/api/Dashboard/new-users-count?days=30");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Member_Token_IsForbidden_OnDashboardEndpoints()
    {
        using var client = NewClient();
        AuthorizeAs(client, roleId: 2);

        var response = await client.GetAsync("/api/Dashboard/new-users-count?days=30");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Member_Token_CanCreatePost_WithServerSideDefaults()
    {
        SeedCategory(1);

        using var client = NewClient();
        AuthorizeAs(client, roleId: 2);

        var response = await client.PostAsJsonAsync("/api/Post/Create", new
        {
            title = "Member post",
            content = "Body",
            categoryId = 1
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Member_Token_IsForbidden_OnAdminPostManagement()
    {
        using var client = NewClient();
        AuthorizeAs(client, roleId: 2);

        var response = await client.GetAsync("/api/AdminPost/GetAllPosts");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Member_Token_IsForbidden_OnFaqCreate()
    {
        using var client = NewClient();
        AuthorizeAs(client, roleId: 2);

        var response = await client.PostAsJsonAsync("/api/FAQ/Create", new
        {
            question = "Member question?",
            answer = "Should not be created"
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Member_Token_IsForbidden_OnPostDelete()
    {
        using var client = NewClient();
        AuthorizeAs(client, roleId: 2);

        var response = await client.DeleteAsync("/api/Post/Delete/1");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // --- Identity account management: admin-only surface ---

    [Fact]
    public async Task Anonymous_Request_ToAccountGetAll_IsUnauthorized()
    {
        using var client = NewClient();

        var response = await client.GetAsync("/api/Account");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Member_Token_IsForbidden_OnAccountGetAll()
    {
        using var client = NewClient();
        AuthorizeAs(client, roleId: 2);

        var response = await client.GetAsync("/api/Account");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Admin_Token_CanListAccounts()
    {
        using var client = NewClient();
        AuthorizeAs(client, roleId: 1);

        var response = await client.GetAsync("/api/Account");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Member_Token_IsForbidden_OnAccountDelete()
    {
        using var client = NewClient();
        AuthorizeAs(client, roleId: 2);

        var response = await client.DeleteAsync("/api/Account/999999");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Member_Token_IsForbidden_OnAccountGetByEmail()
    {
        using var client = NewClient();
        AuthorizeAs(client, roleId: 2);

        var response = await client.GetAsync("/api/Account/email/other@test.local");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // --- Identity account management: self-service ownership guard ---

    [Fact]
    public async Task Member_Token_IsForbidden_OnUpdatingOtherAccounts()
    {
        using var client = NewClient();
        AuthorizeAs(client, roleId: 2); // mints AccountId 102

        var response = await client.PutAsJsonAsync("/api/Account/101", new { });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    public static TheoryData<string> PublicReadEndpoints => new()
    {
        "/api/FAQ/GetAll",
        "/api/FAQ/Details/1",
        "/api/Post/GetAll",
        "/api/Post/Details/1",
        "/api/Element/GetAll"
    };

    [Theory]
    [MemberData(nameof(PublicReadEndpoints))]
    public async Task Anonymous_PublicReads_RemainAccessible(string url)
    {
        using var client = NewClient();

        var response = await client.GetAsync(url);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private void AuthorizeAs(HttpClient client, int roleId)
    {
        using var scope = _factory.Services.CreateScope();
        var tokenService = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
        var token = tokenService.GenerateJwtToken(new Account
        {
            AccountId = 100 + roleId,
            Email = $"user{roleId}@test.local",
            RoleId = roleId
        });
        client.DefaultRequestHeaders.Authorization = new("Bearer", token);
    }

    private void SeedCategory(int id)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<KoiFengShuiContext>();
        if (!context.PostCategories.Any(c => c.Id == id))
        {
            context.PostCategories.Add(new PostCategory { Id = id, PostType = "Blog" });
            context.SaveChanges();
        }
    }

    private void SeedFaq(int id)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<KoiFengShuiContext>();
        if (!context.FAQs.Any(f => f.FAQId == id))
        {
            context.FAQs.Add(new FAQ { FAQId = id, Question = "Seed?", Answer = "Seed answer", CreateAt = DateTime.UtcNow, AccountId = 1 });
            context.SaveChanges();
        }
    }

    private void SeedPost(int id)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<KoiFengShuiContext>();
        if (!context.Posts.Any(p => p.PostId == id))
        {
            context.Posts.Add(new Post
            {
                PostId = id,
                PostCategoryId = 1,
                Name = "Seed post",
                Description = "Seed body",
                Status = "Published",
                AccountId = 1,
                ElementId = 1,
                CreateAt = DateTime.UtcNow,
                UpdateAt = DateTime.UtcNow
            });
            context.SaveChanges();
        }
    }

    public class SecurityMatrixFactory : ApiTestFactory
    {
    }
}
