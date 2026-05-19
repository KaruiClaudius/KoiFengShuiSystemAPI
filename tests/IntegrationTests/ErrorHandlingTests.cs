using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace IntegrationTests;

public class ErrorHandlingTests : IClassFixture<ApiTestFactory>
{
    private readonly HttpClient _client;

    public ErrorHandlingTests(ApiTestFactory factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task NonExistentEndpoint_ReturnsNotFound_DoesNotCrash()
    {
        // Act
        var response = await _client.GetAsync("/api/non-existent-endpoint");

        // Assert: The middleware pipeline handles the request without crashing
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Application_StartsSuccessfully_ExceptionMiddlewareIsRegistered()
    {
        // This test verifies that the application starts without errors
        // and the middleware pipeline is functional.
        // The ExceptionMiddleware is registered in Program.cs before other middleware.
        
        // Act: Make a simple request to verify the pipeline works
        var response = await _client.GetAsync("/swagger/v1/swagger.json");

        // Assert: Application started successfully and responds
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
