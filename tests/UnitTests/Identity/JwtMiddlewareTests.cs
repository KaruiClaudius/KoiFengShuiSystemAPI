using KoiFengShuiSystem.Modules.Identity.Application.Abstractions;
using KoiFengShuiSystem.Modules.Identity.Application.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;

namespace UnitTests.Identity;

public class JwtMiddlewareTests
{
    [Fact]
    public async Task ApiJwtMiddleware_InvalidToken_ReturnsUnauthorized()
    {
        var context = CreateContext();
        var accountService = new Mock<IAccountService>(MockBehavior.Strict);
        var jwtTokenService = new Mock<IJwtTokenService>(MockBehavior.Strict);
        var nextCalled = false;
        var middleware = new KoiFengShuiSystem.Host.Middleware.JwtMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        }, Mock.Of<ILogger<KoiFengShuiSystem.Host.Middleware.JwtMiddleware>>());

        jwtTokenService
            .Setup(service => service.ValidateJwtToken("bad-token"))
            .Throws(new UnauthorizedAccessException("invalid token"));

        await middleware.Invoke(context, accountService.Object, jwtTokenService.Object);

        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
        Assert.False(nextCalled);
    }

    [Fact]
    public async Task ApiJwtMiddleware_AccountLoadFailure_IsNotReportedAsInvalidToken()
    {
        var context = CreateContext();
        var accountService = new Mock<IAccountService>(MockBehavior.Strict);
        var jwtTokenService = new Mock<IJwtTokenService>(MockBehavior.Strict);
        var middleware = new KoiFengShuiSystem.Host.Middleware.JwtMiddleware(_ => Task.CompletedTask, Mock.Of<ILogger<KoiFengShuiSystem.Host.Middleware.JwtMiddleware>>());

        jwtTokenService
            .Setup(service => service.ValidateJwtToken("bad-token"))
            .Returns(7);
        accountService
            .Setup(service => service.GetByIdAsync(7))
            .ThrowsAsync(new InvalidOperationException("database offline"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => middleware.Invoke(context, accountService.Object, jwtTokenService.Object));
        Assert.NotEqual(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
    }

    [Fact]
    public async Task HostJwtMiddleware_InvalidToken_ReturnsUnauthorized()
    {
        var context = CreateContext();
        var accountService = new Mock<IAccountService>(MockBehavior.Strict);
        var jwtTokenService = new Mock<IJwtTokenService>(MockBehavior.Strict);
        var nextCalled = false;
        var middleware = new KoiFengShuiSystem.Host.Middleware.JwtMiddleware(_ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        }, Mock.Of<ILogger<KoiFengShuiSystem.Host.Middleware.JwtMiddleware>>());

        jwtTokenService
            .Setup(service => service.ValidateJwtToken("bad-token"))
            .Throws(new UnauthorizedAccessException("invalid token"));

        await middleware.Invoke(context, accountService.Object, jwtTokenService.Object);

        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
        Assert.False(nextCalled);
    }

    [Fact]
    public async Task HostJwtMiddleware_AccountLoadFailure_IsNotReportedAsInvalidToken()
    {
        var context = CreateContext();
        var accountService = new Mock<IAccountService>(MockBehavior.Strict);
        var jwtTokenService = new Mock<IJwtTokenService>(MockBehavior.Strict);
        var middleware = new KoiFengShuiSystem.Host.Middleware.JwtMiddleware(_ => Task.CompletedTask, Mock.Of<ILogger<KoiFengShuiSystem.Host.Middleware.JwtMiddleware>>());

        jwtTokenService
            .Setup(service => service.ValidateJwtToken("bad-token"))
            .Returns(7);
        accountService
            .Setup(service => service.GetByIdAsync(7))
            .ThrowsAsync(new InvalidOperationException("database offline"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => middleware.Invoke(context, accountService.Object, jwtTokenService.Object));
        Assert.NotEqual(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
    }

    private static DefaultHttpContext CreateContext()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.Authorization = "Bearer bad-token";
        context.Response.Body = new MemoryStream();
        return context;
    }
}
