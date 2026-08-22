using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using KoiFengShuiSystem.Modules.Identity.Api.Controllers;
using KoiFengShuiSystem.Modules.Identity.Application.Abstractions;
using KoiFengShuiSystem.Modules.Identity.Application.Requests;
using KoiFengShuiSystem.Modules.Identity.Application.Responses;
using KoiFengShuiSystem.Modules.Identity.Application.Services;
using KoiFengShuiSystem.Modules.Identity.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace UnitTests.Identity;

public class AuthControllerTests
{
    private static Mock<IRefreshTokenPort> CreateDefaultRefreshTokenPort() => new();

    private static AuthController CreateController(
        Mock<IAccountService> accountService,
        Mock<IJwtTokenService> jwtTokenService,
        Mock<IRefreshTokenPort>? refreshTokenPort = null,
        Mock<IHttpClientFactory>? httpClientFactory = null)
        => new(
            accountService.Object,
            jwtTokenService.Object,
            (refreshTokenPort ?? CreateDefaultRefreshTokenPort()).Object,
            (httpClientFactory ?? new Mock<IHttpClientFactory>(MockBehavior.Strict)).Object,
            Mock.Of<ILogger<AuthController>>());

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GoogleLogin_InvalidAccessToken_ReturnsServerError(string? accessToken)
    {
        var accountService = new Mock<IAccountService>(MockBehavior.Strict);
        var jwtTokenService = new Mock<IJwtTokenService>(MockBehavior.Strict);
        var httpClientFactory = new Mock<IHttpClientFactory>(MockBehavior.Strict);
        var controller = CreateController(accountService, jwtTokenService, httpClientFactory: httpClientFactory);

        var result = await controller.GoogleLogin(new GoogleLoginRequest { AccessToken = accessToken });

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal((int)HttpStatusCode.InternalServerError, objectResult.StatusCode);
        Assert.Equal("An unexpected error occurred", objectResult.Value);
    }

    [Fact]
    public async Task ForgotPassword_EmailSendFails_ReturnsServerError()
    {
        var accountService = new Mock<IAccountService>(MockBehavior.Strict);
        var jwtTokenService = new Mock<IJwtTokenService>(MockBehavior.Strict);
        var httpClientFactory = new Mock<IHttpClientFactory>(MockBehavior.Strict);

        accountService
            .Setup(service => service.ForgotPasswordAsync("user@example.com"))
            .ReturnsAsync(false);

        var controller = CreateController(accountService, jwtTokenService, httpClientFactory: httpClientFactory);

        var result = await controller.ForgotPassword(new ForgotPasswordRequest { Email = "user@example.com" });

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal((int)HttpStatusCode.InternalServerError, objectResult.StatusCode);
        Assert.Equal("An unexpected error occurred", objectResult.Value);
        accountService.Verify(service => service.ForgotPasswordAsync("user@example.com"), Times.Once);
    }

    [Fact]
    public async Task ForgotPassword_Success_ReturnsNeutralMessageWithoutRevealingAccountExistence()
    {
        var accountService = new Mock<IAccountService>(MockBehavior.Strict);
        var jwtTokenService = new Mock<IJwtTokenService>(MockBehavior.Strict);
        var httpClientFactory = new Mock<IHttpClientFactory>(MockBehavior.Strict);

        accountService
            .Setup(service => service.ForgotPasswordAsync("user@example.com"))
            .ReturnsAsync(true);

        var controller = CreateController(accountService, jwtTokenService, httpClientFactory: httpClientFactory);

        var result = await controller.ForgotPassword(new ForgotPasswordRequest { Email = "user@example.com" });

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Contains("If a user with this email exists", okResult.Value!.ToString());
    }

    [Fact]
    public async Task ResetPassword_InvalidOrExpiredToken_ReturnsBadRequest()
    {
        var accountService = new Mock<IAccountService>(MockBehavior.Strict);
        var jwtTokenService = new Mock<IJwtTokenService>(MockBehavior.Strict);
        var httpClientFactory = new Mock<IHttpClientFactory>(MockBehavior.Strict);
        var request = new ResetPasswordRequest { Token = "bad-or-expired-token", NewPassword = "newSecret123" };

        accountService
            .Setup(service => service.ResetPasswordAsync(request))
            .ReturnsAsync(false);

        var controller = CreateController(accountService, jwtTokenService, httpClientFactory: httpClientFactory);

        var result = await controller.ResetPassword(request);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("Invalid or expired reset token", badRequestResult.Value!.ToString());
        Assert.Equal((int)HttpStatusCode.BadRequest, badRequestResult.StatusCode);
    }

    [Fact]
    public async Task ResetPassword_ValidToken_ReturnsSuccessMessage()
    {
        var accountService = new Mock<IAccountService>(MockBehavior.Strict);
        var jwtTokenService = new Mock<IJwtTokenService>(MockBehavior.Strict);
        var httpClientFactory = new Mock<IHttpClientFactory>(MockBehavior.Strict);
        var request = new ResetPasswordRequest { Token = "valid-token", NewPassword = "newSecret123" };

        accountService
            .Setup(service => service.ResetPasswordAsync(request))
            .ReturnsAsync(true);

        var controller = CreateController(accountService, jwtTokenService, httpClientFactory: httpClientFactory);

        var result = await controller.ResetPassword(request);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Contains("reset successfully", okResult.Value!.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ResetPassword_InvalidModelState_ReturnsBadRequest()
    {
        var accountService = new Mock<IAccountService>(MockBehavior.Strict);
        var jwtTokenService = new Mock<IJwtTokenService>(MockBehavior.Strict);
        var httpClientFactory = new Mock<IHttpClientFactory>(MockBehavior.Strict);
        var controller = CreateController(accountService, jwtTokenService, httpClientFactory: httpClientFactory);
        controller.ModelState.AddModelError("Token", "The Token field is required.");

        var result = await controller.ResetPassword(new ResetPasswordRequest());

        Assert.IsType<BadRequestObjectResult>(result);
        accountService.Verify(service => service.ResetPasswordAsync(It.IsAny<ResetPasswordRequest>()), Times.Never);
    }

    // --- SignIn response shape ---

    [Fact]
    public async Task SignIn_ValidCredentials_ReturnsResponseIncludingRefreshTokenAndExpiresIn()
    {
        var authenticationResult = new AuthenticationResult
        {
            Response = new AuthenticateResponse(
                new Account { AccountId = 7, Email = "user@test.com", FullName = "User Seven" },
                "access-token")
            {
                RefreshToken = "raw-refresh-token",
                ExpiresInMinutes = 15
            }
        };
        var accountService = new Mock<IAccountService>(MockBehavior.Strict);
        accountService
            .Setup(service => service.AuthenticateAsync(It.IsAny<AuthenticateRequest>()))
            .ReturnsAsync(authenticationResult);
        var controller = CreateController(accountService, new Mock<IJwtTokenService>(MockBehavior.Strict));

        var result = await controller.Authenticate(new AuthenticateRequest { Email = "user@test.com", Password = "password123" });

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<AuthenticateResponse>(okResult.Value);
        Assert.Equal("access-token", response.Token);
        Assert.Equal("raw-refresh-token", response.RefreshToken);
        Assert.Equal(15, response.ExpiresInMinutes);
        Assert.Equal(7, response.Id);
    }

    // --- POST api/Auth/refresh ---

    [Fact]
    public async Task Refresh_WithValidRefreshToken_ReturnsRotatedTokenPairAndExpiresIn()
    {
        var accountService = new Mock<IAccountService>(MockBehavior.Strict);
        accountService
            .Setup(service => service.GetByIdAsync(7))
            .ReturnsAsync(new Account { AccountId = 7, Email = "user@test.com", RoleId = 2 });
        var jwtTokenService = new Mock<IJwtTokenService>(MockBehavior.Strict);
        jwtTokenService
            .Setup(service => service.GenerateJwtToken(It.IsAny<Account>()))
            .Returns("new-access-token");
        jwtTokenService.SetupGet(service => service.AccessTokenLifetimeMinutes).Returns(15);
        var refreshTokenPort = new Mock<IRefreshTokenPort>();
        refreshTokenPort
            .Setup(port => port.RotateAsync("presented-raw-token"))
            .ReturnsAsync(RotateResult.Successful(7, "rotated-raw-token"));
        var controller = CreateController(accountService, jwtTokenService, refreshTokenPort);

        var result = await controller.Refresh(new RefreshTokenRequest { RefreshToken = "presented-raw-token" });

        var okResult = Assert.IsType<OkObjectResult>(result);
        using var document = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var root = document.RootElement;
        Assert.Equal("new-access-token", root.GetProperty("token").GetString());
        Assert.Equal("rotated-raw-token", root.GetProperty("refreshToken").GetString());
        Assert.Equal(15, root.GetProperty("expiresIn").GetInt32());
    }

    [Fact]
    public async Task Refresh_WithRejectedRefreshToken_ReturnsUnauthorized()
    {
        var refreshTokenPort = new Mock<IRefreshTokenPort>();
        refreshTokenPort
            .Setup(port => port.RotateAsync("revoked-or-unknown-token"))
            .ReturnsAsync(RotateResult.Failed(RotateResult.ReuseDetectedReason));
        var controller = CreateController(
            new Mock<IAccountService>(MockBehavior.Strict),
            new Mock<IJwtTokenService>(MockBehavior.Strict),
            refreshTokenPort);

        var result = await controller.Refresh(new RefreshTokenRequest { RefreshToken = "revoked-or-unknown-token" });

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Refresh_MissingToken_ReturnsUnauthorized(string? refreshToken)
    {
        var controller = CreateController(
            new Mock<IAccountService>(MockBehavior.Strict),
            new Mock<IJwtTokenService>(MockBehavior.Strict));

        var result = await controller.Refresh(new RefreshTokenRequest { RefreshToken = refreshToken });

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task Refresh_WhenAccountNoLongerExists_ReturnsUnauthorizedWithoutMintingAccessToken()
    {
        var accountService = new Mock<IAccountService>(MockBehavior.Strict);
        accountService
            .Setup(service => service.GetByIdAsync(7))
            .ReturnsAsync((Account?)null);
        var jwtTokenService = new Mock<IJwtTokenService>(MockBehavior.Strict);
        var refreshTokenPort = new Mock<IRefreshTokenPort>();
        refreshTokenPort
            .Setup(port => port.RotateAsync("valid-but-orphaned-token"))
            .ReturnsAsync(RotateResult.Successful(7, "rotated-raw-token"));
        var controller = CreateController(accountService, jwtTokenService, refreshTokenPort);

        var result = await controller.Refresh(new RefreshTokenRequest { RefreshToken = "valid-but-orphaned-token" });

        Assert.IsType<UnauthorizedResult>(result);
        jwtTokenService.Verify(service => service.GenerateJwtToken(It.IsAny<Account>()), Times.Never);
    }

    // --- POST api/Auth/logout ---

    [Fact]
    public async Task Logout_ResolvesAccountIdFromNameIdentifierClaim_AndRevokesAllTokens()
    {
        var refreshTokenPort = new Mock<IRefreshTokenPort>(MockBehavior.Strict);
        refreshTokenPort.Setup(port => port.RevokeAllForAccountAsync(7)).Returns(Task.CompletedTask);
        var controller = CreateController(
            new Mock<IAccountService>(MockBehavior.Strict),
            new Mock<IJwtTokenService>(MockBehavior.Strict),
            refreshTokenPort);
        SetAuthenticatedUser(controller, new Claim(ClaimTypes.NameIdentifier, "7"));

        var result = await controller.Logout();

        Assert.IsType<NoContentResult>(result);
        refreshTokenPort.Verify(port => port.RevokeAllForAccountAsync(7), Times.Once);
    }

    [Fact]
    public async Task Logout_FallsBackToIdClaim_WhenNameIdentifierIsMissing()
    {
        var refreshTokenPort = new Mock<IRefreshTokenPort>(MockBehavior.Strict);
        refreshTokenPort.Setup(port => port.RevokeAllForAccountAsync(8)).Returns(Task.CompletedTask);
        var controller = CreateController(
            new Mock<IAccountService>(MockBehavior.Strict),
            new Mock<IJwtTokenService>(MockBehavior.Strict),
            refreshTokenPort);
        SetAuthenticatedUser(controller, new Claim("id", "8"));

        var result = await controller.Logout();

        Assert.IsType<NoContentResult>(result);
        refreshTokenPort.Verify(port => port.RevokeAllForAccountAsync(8), Times.Once);
    }

    [Theory]
    [InlineData("not-a-number")]
    public async Task Logout_WithUnparseableAccountClaim_ReturnsUnauthorizedWithoutRevoking(string claimValue)
    {
        var refreshTokenPort = new Mock<IRefreshTokenPort>(MockBehavior.Strict);
        var controller = CreateController(
            new Mock<IAccountService>(MockBehavior.Strict),
            new Mock<IJwtTokenService>(MockBehavior.Strict),
            refreshTokenPort);
        SetAuthenticatedUser(controller, new Claim(ClaimTypes.NameIdentifier, claimValue));

        var result = await controller.Logout();

        Assert.IsType<UnauthorizedResult>(result);
        refreshTokenPort.Verify(port => port.RevokeAllForAccountAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task Logout_WithoutAnyAccountClaim_ReturnsUnauthorized()
    {
        var refreshTokenPort = new Mock<IRefreshTokenPort>(MockBehavior.Strict);
        var controller = CreateController(
            new Mock<IAccountService>(MockBehavior.Strict),
            new Mock<IJwtTokenService>(MockBehavior.Strict),
            refreshTokenPort);
        SetAuthenticatedUser(controller);

        var result = await controller.Logout();

        Assert.IsType<UnauthorizedResult>(result);
        refreshTokenPort.Verify(port => port.RevokeAllForAccountAsync(It.IsAny<int>()), Times.Never);
    }

    private static void SetAuthenticatedUser(AuthController controller, params Claim[] claims)
    {
        controller.ControllerContext.HttpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "TestAuth"))
        };
    }
}
