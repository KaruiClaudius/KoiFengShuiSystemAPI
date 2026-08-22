using KoiFengShuiSystem.Modules.Identity.Api.Controllers;
using KoiFengShuiSystem.Modules.Identity.Application.Requests;
using KoiFengShuiSystem.Modules.Identity.Application.Services;
using KoiFengShuiSystem.Modules.Identity.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace UnitTests.Identity;

public class AccountControllerTests
{
    [Fact]
    public async Task GetAll_ReturnsResolvedAccounts()
    {
        var accounts = new[] { new Account { AccountId = 1, Email = "a@test.com" } };
        var accountService = new Mock<IAccountService>(MockBehavior.Strict);
        accountService.Setup(service => service.GetAllAsync()).ReturnsAsync(accounts);

        var controller = new AccountController(accountService.Object, Mock.Of<ILogger<KoiFengShuiSystem.Modules.Identity.Application.Services.AccountService>>());

        var result = await controller.GetAll();

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Same(accounts, okResult.Value);
    }

    [Fact]
    public async Task GetById_WhenAccountMissing_ReturnsNotFound()
    {
        var accountService = new Mock<IAccountService>(MockBehavior.Strict);
        accountService.Setup(service => service.GetByIdAsync(42)).ReturnsAsync((Account?)null);

        var controller = new AccountController(accountService.Object, Mock.Of<ILogger<KoiFengShuiSystem.Modules.Identity.Application.Services.AccountService>>());

        var result = await controller.GetById(42);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Update_WhenServiceThrowsApplicationException_ReturnsBadRequest()
    {
        var accountService = new Mock<IAccountService>(MockBehavior.Strict);
        accountService
            .Setup(service => service.UpdateAsync(7, It.IsAny<UpdateRequest>()))
            .Returns(Task.FromException(new ApplicationException("boom")));

        var controller = new AccountController(accountService.Object, Mock.Of<ILogger<KoiFengShuiSystem.Modules.Identity.Application.Services.AccountService>>());

        var result = await controller.Update(7, new UpdateRequest());

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("boom", badRequest.Value?.GetType().GetProperty("message")?.GetValue(badRequest.Value));
    }

    [Fact]
    public async Task Delete_WhenServiceThrowsApplicationException_ReturnsBadRequest()
    {
        var accountService = new Mock<IAccountService>(MockBehavior.Strict);
        accountService
            .Setup(service => service.DeleteAsync(7))
            .Returns(Task.FromException(new ApplicationException("boom")));

        var controller = new AccountController(accountService.Object, Mock.Of<ILogger<KoiFengShuiSystem.Modules.Identity.Application.Services.AccountService>>());

        var result = await controller.Delete(7);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("boom", badRequest.Value?.GetType().GetProperty("message")?.GetValue(badRequest.Value));
    }
}
