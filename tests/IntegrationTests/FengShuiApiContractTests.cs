using System.Net;
using System.Text.Json;

namespace IntegrationTests;

public class FengShuiApiContractTests : IClassFixture<ApiTestFactory>
{
    private readonly HttpClient _client;

    public FengShuiApiContractTests(ApiTestFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Swagger_ContainsFengShuiRoutes()
    {
        var response = await _client.GetAsync("/swagger/v1/swagger.json");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var paths = document.RootElement.GetProperty("paths");

        Assert.True(paths.TryGetProperty("/api/Compatibility/lookup", out _));
        Assert.True(paths.TryGetProperty("/api/Consultation/fengshui", out _));
        Assert.True(paths.TryGetProperty("/api/Element/GetAll", out _));
    }
}
