using System.Net;
using System.Text;
using KoiFengShuiSystem.Modules.Identity.Api.Controllers;
using KoiFengShuiSystem.Modules.Identity.Application.Abstractions;
using KoiFengShuiSystem.Modules.Identity.Application.Requests;
using KoiFengShuiSystem.Modules.Identity.Application.Services;
using KoiFengShuiSystem.Modules.Identity.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace UnitTests.Identity;

public class AuthControllerTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GoogleLogin_InvalidAccessToken_ReturnsServerError(string? accessToken)
    {
        var accountService = new Mock<IAccountService>(MockBehavior.Strict);
        var jwtTokenService = new Mock<IJwtTokenService>(MockBehavior.Strict);
        var httpClientFactory = new Mock<IHttpClientFactory>(MockBehavior.Strict);
        var controller = new AuthController(accountService.Object, jwtTokenService.Object, httpClientFactory.Object, Mock.Of<ILogger<AuthController>>());

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

        var controller = new AuthController(accountService.Object, jwtTokenService.Object, httpClientFactory.Object, Mock.Of<ILogger<AuthController>>());

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

        var controller = new AuthController(accountService.Object, jwtTokenService.Object, httpClientFactory.Object, Mock.Of<ILogger<AuthController>>());

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

        var controller = new AuthController(accountService.Object, jwtTokenService.Object, httpClientFactory.Object, Mock.Of<ILogger<AuthController>>());

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

        var controller = new AuthController(accountService.Object, jwtTokenService.Object, httpClientFactory.Object, Mock.Of<ILogger<AuthController>>());

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
        var controller = new AuthController(accountService.Object, jwtTokenService.Object, httpClientFactory.Object, Mock.Of<ILogger<AuthController>>());
        controller.ModelState.AddModelError("Token", "The Token field is required.");

        var result = await controller.ResetPassword(new ResetPasswordRequest());

        Assert.IsType<BadRequestObjectResult>(result);
        accountService.Verify(service => service.ResetPasswordAsync(It.IsAny<ResetPasswordRequest>()), Times.Never);
    }
}
