using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace IntegrationTests;

// Fixed windows are one-minute scoped, and xUnit does not guarantee fact
// execution order. Every rate-limiting scenario therefore gets its own
// ApiTestFactory (own DI container => own limiter singletons) and performs
// exactly one burst per policy so scenarios never share limiter state.
public abstract class RateLimitingTestBase : IClassFixture<ApiTestFactory>
{
    protected RateLimitingTestBase(ApiTestFactory factory)
    {
        Client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    protected HttpClient Client { get; }

    protected Task<HttpResponseMessage> PostSignInAsync() =>
        Client.PostAsJsonAsync("/api/Auth/SignIn", new
        {
            email = "rate-limit@test.local",
            password = "wrong-password"
        });

    protected Task<HttpResponseMessage> PostFengShuiConsultationAsync() =>
        Client.PostAsJsonAsync("/api/Consultation/fengshui", new
        {
            yearOfBirth = 1990,
            isMale = true
        });
}

public class AuthRateLimitingPolicyTests : RateLimitingTestBase
{
    public AuthRateLimitingPolicyTests(ApiTestFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task ThirdConsecutiveSignInWithinWindow_IsRejectedWith429()
    {
        var first = await PostSignInAsync();
        var second = await PostSignInAsync();

        var third = await PostSignInAsync();

        Assert.NotEqual(HttpStatusCode.TooManyRequests, first.StatusCode);
        Assert.NotEqual(HttpStatusCode.TooManyRequests, second.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, third.StatusCode);
    }
}

public class ComputeRateLimitingPolicyTests : RateLimitingTestBase
{
    public ComputeRateLimitingPolicyTests(ApiTestFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task ThirdConsecutiveFengShuiConsultationWithinWindow_IsRejectedWith429()
    {
        var first = await PostFengShuiConsultationAsync();
        var second = await PostFengShuiConsultationAsync();

        var third = await PostFengShuiConsultationAsync();

        Assert.NotEqual(HttpStatusCode.TooManyRequests, first.StatusCode);
        Assert.NotEqual(HttpStatusCode.TooManyRequests, second.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, third.StatusCode);
    }
}

public class GlobalRateLimitingExemptionTests : RateLimitingTestBase
{
    public GlobalRateLimitingExemptionTests(ApiTestFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task UnrelatedGet_WhileComputePolicyIsExhausted_IsServedNormally()
    {
        for (var attempt = 0; attempt < 3; attempt++)
        {
            await PostFengShuiConsultationAsync();
        }

        var response = await Client.GetAsync("/swagger/v1/swagger.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
