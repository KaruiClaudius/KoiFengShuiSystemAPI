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
    public async Task ForgotPassword_EmailSendFails_PersistsPasswordBeforeReturningError()
    {
        var account = new Account { AccountId = 7, Email = "user@example.com", FullName = "Test User" };
        var accountService = new Mock<IAccountService>(MockBehavior.Strict);
        var jwtTokenService = new Mock<IJwtTokenService>(MockBehavior.Strict);
        var httpClientFactory = new Mock<IHttpClientFactory>(MockBehavior.Strict);
        var sequence = new MockSequence();

        accountService
            .InSequence(sequence)
            .Setup(service => service.GetAccountByEmailAsync(account.Email))
            .ReturnsAsync(account);
        accountService
            .InSequence(sequence)
            .Setup(service => service.UpdateUserPasswordAsync(account, It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        accountService
            .InSequence(sequence)
            .Setup(service => service.SendPasswordResetEmailAsync(account.Email, account.FullName, It.IsAny<string>()))
            .ReturnsAsync(false);

        var controller = new AuthController(accountService.Object, jwtTokenService.Object, httpClientFactory.Object, Mock.Of<ILogger<AuthController>>());

        var result = await controller.ForgotPassword(new ForgotPasswordRequest { Email = account.Email });

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal((int)HttpStatusCode.InternalServerError, objectResult.StatusCode);
        Assert.Equal("An unexpected error occurred", objectResult.Value);
        accountService.Verify(service => service.UpdateUserPasswordAsync(account, It.IsAny<string>()), Times.Once);
    }
}
