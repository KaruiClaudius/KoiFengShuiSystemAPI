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

    // --- POST api/Auth/google-login ---

    [Fact]
    public async Task GoogleLogin_NewUser_CreatesPasswordlessAccountWithoutFabricatedData()
    {
        const string googlePayload = """{"sub":"google-subject-id","email":"new.user@gmail.com","name":"New User"}""";
        var accountService = new Mock<IAccountService>(MockBehavior.Strict);
        Account? createdAccount = null;
        accountService
            .Setup(service => service.GetAccountByEmailAsync("new.user@gmail.com"))
            .ReturnsAsync((Account?)null);
        accountService
            .Setup(service => service.CreateAsync(It.IsAny<Account>()))
            .Callback<Account>(account =>
            {
                account.AccountId = 55;
                createdAccount = account;
            })
            .ReturnsAsync((Account account) => account);
        var jwtTokenService = new Mock<IJwtTokenService>(MockBehavior.Strict);
        jwtTokenService
            .Setup(service => service.GenerateJwtToken(It.IsAny<Account>()))
            .Returns("access-token");
        jwtTokenService.SetupGet(service => service.AccessTokenLifetimeMinutes).Returns(15);
        var refreshTokenPort = new Mock<IRefreshTokenPort>(MockBehavior.Strict);
        refreshTokenPort
            .Setup(port => port.CreateForAccountAsync(55))
            .ReturnsAsync("raw-refresh-token");
        var controller = CreateController(
            accountService,
            jwtTokenService,
            refreshTokenPort,
            CreateGoogleHttpClientFactory(googlePayload));

        var result = await controller.GoogleLogin(new GoogleLoginRequest { AccessToken = "valid-google-access-token" });

        Assert.NotNull(createdAccount);
        Assert.Equal("new.user@gmail.com", createdAccount!.Email);
        Assert.Equal("New User", createdAccount.FullName);
        Assert.Equal(2, createdAccount.RoleId);
        Assert.Null(createdAccount.Password);
        Assert.Null(createdAccount.Gender);
        Assert.Null(createdAccount.Dob);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<AuthenticateResponse>(okResult.Value);
        Assert.Equal("access-token", response.Token);
        Assert.Equal("raw-refresh-token", response.RefreshToken);
        Assert.Equal(15, response.ExpiresInMinutes);

        accountService.Verify(service => service.GetAccountByEmailAsync("new.user@gmail.com"), Times.Once);
        accountService.Verify(service => service.CreateAsync(It.IsAny<Account>()), Times.Once);
        accountService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GoogleLogin_NewUser_SkipsElementAssignment()
    {
        const string googlePayload = """{"sub":"google-subject-id","email":"element.skip@gmail.com","name":"Element Skip"}""";
        var accountService = new Mock<IAccountService>(MockBehavior.Strict);
        Account? createdAccount = null;
        accountService
            .Setup(service => service.GetAccountByEmailAsync("element.skip@gmail.com"))
            .ReturnsAsync((Account?)null);
        accountService
            .Setup(service => service.CreateAsync(It.IsAny<Account>()))
            .Callback<Account>(account =>
            {
                account.AccountId = 56;
                createdAccount = account;
            })
            .ReturnsAsync((Account account) => account);
        var jwtTokenService = new Mock<IJwtTokenService>(MockBehavior.Strict);
        jwtTokenService
            .Setup(service => service.GenerateJwtToken(It.IsAny<Account>()))
            .Returns("access-token");
        jwtTokenService.SetupGet(service => service.AccessTokenLifetimeMinutes).Returns(15);
        var controller = CreateController(
            accountService,
            jwtTokenService,
            httpClientFactory: CreateGoogleHttpClientFactory(googlePayload));

        await controller.GoogleLogin(new GoogleLoginRequest { AccessToken = "valid-google-access-token" });

        Assert.NotNull(createdAccount);
        Assert.Null(createdAccount!.ElementId);
    }

    [Fact]
    public async Task GoogleLogin_ExistingUser_MatchesByEmailWithoutCreatingNewAccount()
    {
        const string googlePayload = """{"sub":"google-subject-id","email":"existing@test.com","name":"Existing User"}""";
        var existingAccount = new Account
        {
            AccountId = 9,
            Email = "existing@test.com",
            FullName = "Existing User",
            RoleId = 2
        };
        var accountService = new Mock<IAccountService>(MockBehavior.Strict);
        accountService
            .Setup(service => service.GetAccountByEmailAsync("existing@test.com"))
            .ReturnsAsync(existingAccount);
        var jwtTokenService = new Mock<IJwtTokenService>(MockBehavior.Strict);
        jwtTokenService
            .Setup(service => service.GenerateJwtToken(It.IsAny<Account>()))
            .Returns("access-token");
        jwtTokenService.SetupGet(service => service.AccessTokenLifetimeMinutes).Returns(15);
        var refreshTokenPort = new Mock<IRefreshTokenPort>(MockBehavior.Strict);
        refreshTokenPort
            .Setup(port => port.CreateForAccountAsync(9))
            .ReturnsAsync("raw-refresh-token");
        var controller = CreateController(
            accountService,
            jwtTokenService,
            refreshTokenPort,
            CreateGoogleHttpClientFactory(googlePayload));

        var result = await controller.GoogleLogin(new GoogleLoginRequest { AccessToken = "valid-google-access-token" });

        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<AuthenticateResponse>(okResult.Value);
        Assert.Equal(9, response.Id);
        Assert.Equal("access-token", response.Token);
        Assert.Equal("raw-refresh-token", response.RefreshToken);
        accountService.Verify(service => service.CreateAsync(It.IsAny<Account>()), Times.Never);
    }

    // --- GET api/Auth/profile-status ---
    //
    // Response shape: { "requiresProfileCompletion": true|false }
    // True while the signed-in account's date of birth or gender is still missing
    // (e.g. accounts created through Google login start without both).

    [Fact]
    public async Task ProfileStatus_IncompleteProfile_RequiresCompletion()
    {
        var accountService = new Mock<IAccountService>(MockBehavior.Strict);
        accountService
            .Setup(service => service.GetByIdAsync(7))
            .ReturnsAsync(new Account { AccountId = 7, Email = "incomplete@test.com" });
        var controller = CreateController(accountService, new Mock<IJwtTokenService>(MockBehavior.Strict));
        SetAuthenticatedUser(controller, new Claim(ClaimTypes.NameIdentifier, "7"));

        var result = await controller.GetProfileStatus();

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.True(ReadRequiresProfileCompletion(okResult.Value));
    }

    [Fact]
    public async Task ProfileStatus_CompleteProfile_DoesNotRequireCompletion()
    {
        var accountService = new Mock<IAccountService>(MockBehavior.Strict);
        accountService
            .Setup(service => service.GetByIdAsync(7))
            .ReturnsAsync(new Account
            {
                AccountId = 7,
                Email = "complete@test.com",
                Dob = new DateTime(1990, 1, 1),
                Gender = "male"
            });
        var controller = CreateController(accountService, new Mock<IJwtTokenService>(MockBehavior.Strict));
        SetAuthenticatedUser(controller, new Claim(ClaimTypes.NameIdentifier, "7"));

        var result = await controller.GetProfileStatus();

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.False(ReadRequiresProfileCompletion(okResult.Value));
    }

    [Fact]
    public async Task ProfileStatus_Unauthenticated_ReturnsUnauthorized()
    {
        var controller = CreateController(
            new Mock<IAccountService>(MockBehavior.Strict),
            new Mock<IJwtTokenService>(MockBehavior.Strict));
        SetAuthenticatedUser(controller);

        var result = await controller.GetProfileStatus();

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task ProfileStatus_UnknownAccount_ReturnsUnauthorized()
    {
        var accountService = new Mock<IAccountService>(MockBehavior.Strict);
        accountService
            .Setup(service => service.GetByIdAsync(7))
            .ReturnsAsync((Account?)null);
        var controller = CreateController(accountService, new Mock<IJwtTokenService>(MockBehavior.Strict));
        SetAuthenticatedUser(controller, new Claim(ClaimTypes.NameIdentifier, "7"));

        var result = await controller.GetProfileStatus();

        Assert.IsType<UnauthorizedResult>(result);
    }

    private static bool ReadRequiresProfileCompletion(object value)
    {
        using var document = JsonDocument.Parse(JsonSerializer.Serialize(value));
        return document.RootElement.GetProperty("requiresProfileCompletion").GetBoolean();
    }

    private static Mock<IHttpClientFactory> CreateGoogleHttpClientFactory(string jsonPayload)
    {
        var httpClient = new HttpClient(new StubGoogleUserInfoHandler(jsonPayload));
        var factory = new Mock<IHttpClientFactory>(MockBehavior.Strict);
        factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);
        return factory;
    }

    private sealed class StubGoogleUserInfoHandler : HttpMessageHandler
    {
        private readonly string _jsonPayload;

        public StubGoogleUserInfoHandler(string jsonPayload)
        {
            _jsonPayload = jsonPayload;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_jsonPayload, Encoding.UTF8, "application/json")
            });
    }

    private static void SetAuthenticatedUser(AuthController controller, params Claim[] claims)
    {
        controller.ControllerContext.HttpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "TestAuth"))
        };
    }
}
