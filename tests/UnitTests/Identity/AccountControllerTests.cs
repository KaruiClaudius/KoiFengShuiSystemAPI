using System.Security.Claims;
using KoiFengShuiSystem.Modules.Identity.Api.Controllers;
using KoiFengShuiSystem.Modules.Identity.Application.Requests;
using KoiFengShuiSystem.Modules.Identity.Application.Services;
using KoiFengShuiSystem.Modules.Identity.Domain.Entities;
using KoiFengShuiSystem.Shared.Kernel.Security;
using Microsoft.AspNetCore.Http;
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

        var controller = CreateController(accountService.Object, accountId: 101, role: AuthorizationDefaults.Roles.Admin);

        var result = await controller.GetAll();

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Same(accounts, okResult.Value);
    }

    [Fact]
    public async Task GetById_WhenAccountMissing_ReturnsNotFound()
    {
        var accountService = new Mock<IAccountService>(MockBehavior.Strict);
        accountService.Setup(service => service.GetByIdAsync(42)).ReturnsAsync((Account?)null);

        var controller = CreateController(accountService.Object, accountId: 101, role: AuthorizationDefaults.Roles.Admin);

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

        var controller = CreateController(accountService.Object, accountId: 101, role: AuthorizationDefaults.Roles.Admin);

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

        var controller = CreateController(accountService.Object, accountId: 101, role: AuthorizationDefaults.Roles.Admin);

        var result = await controller.Delete(7);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("boom", badRequest.Value?.GetType().GetProperty("message")?.GetValue(badRequest.Value));
    }

    [Fact]
    public async Task SelfService_WhenMemberTargetsForeignAccount_IsForbiddenWithoutServiceCall()
    {
        var accountService = new Mock<IAccountService>(MockBehavior.Strict);

        var controller = CreateController(accountService.Object, accountId: 102, role: AuthorizationDefaults.Roles.Member);

        var result = await controller.Update(101, new UpdateRequest());

        var forbidden = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, forbidden.StatusCode);
        accountService.Verify(service => service.UpdateAsync(It.IsAny<int>(), It.IsAny<UpdateRequest>()), Times.Never);
    }

    [Fact]
    public async Task SelfService_WhenMemberTargetsOwnAccount_ReachesService()
    {
        var accountService = new Mock<IAccountService>(MockBehavior.Strict);
        accountService
            .Setup(service => service.UpdateAsync(102, It.IsAny<UpdateRequest>()))
            .Returns(Task.CompletedTask);

        var controller = CreateController(accountService.Object, accountId: 102, role: AuthorizationDefaults.Roles.Member);

        var result = await controller.Update(102, new UpdateRequest());

        Assert.IsType<OkObjectResult>(result);
        accountService.Verify(service => service.UpdateAsync(102, It.IsAny<UpdateRequest>()), Times.Once);
    }

    private static AccountController CreateController(
        IAccountService accountService,
        int accountId,
        string role)
    {
        var controller = new AccountController(
            accountService,
            Mock.Of<ILogger<KoiFengShuiSystem.Modules.Identity.Application.Services.AccountService>>());

        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, accountId.ToString()),
            new Claim("id", accountId.ToString()),
            new Claim(ClaimTypes.Role, role)
        }, "TestAuthentication"));

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };

        return controller;
    }
}
