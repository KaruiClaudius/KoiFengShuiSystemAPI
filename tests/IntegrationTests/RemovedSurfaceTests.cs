using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace IntegrationTests;

/// <summary>
/// Pins the removal of the payment-gateway and marketplace surfaces: their routes must
/// stay gone, not resurface accidentally via a future regression or copy-paste port.
/// </summary>
public class RemovedSurfaceTests : IClassFixture<ApiTestFactory>
{
    private readonly HttpClient _client;

    public RemovedSurfaceTests(ApiTestFactory factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Theory]
    [InlineData("/api/Transaction")]
    [InlineData("/api/MarketplaceListings")]
    [InlineData("/api/SubcriptionTiers")]
    [InlineData("/api/MarketCategory")]
    // AccountController carries class-level authorization, so this one walls at 401
    // before route resolution; any authenticated probe would 404.
    [InlineData("/api/Account/UpdateWalletAfterPosted")]
    public async Task RemovedPaymentAndShopRoutes_AreGone(string route)
    {
        var response = await _client.GetAsync(route);

        Assert.True(
            response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Unauthorized,
            $"Expected 404/401 for removed surface {route} but got {response.StatusCode}");
    }
}
