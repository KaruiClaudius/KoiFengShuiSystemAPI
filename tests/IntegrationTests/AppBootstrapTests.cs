using System.Net;

namespace IntegrationTests;

public class AppBootstrapTests : IClassFixture<ApiTestFactory>
{
    private readonly ApiTestFactory _factory;

    public AppBootstrapTests(ApiTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task SwaggerEndpoint_Returns200()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/swagger/v1/swagger.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
