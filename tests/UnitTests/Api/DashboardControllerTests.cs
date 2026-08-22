using System.Security.Claims;
using System.Text.Json;
using KoiFengShuiSystem.Api.Controllers;
using KoiFengShuiSystem.API.Controllers;
using KoiFengShuiSystem.BusinessLogic.Services.Interface;
using KoiFengShuiSystem.Modules.Identity.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace UnitTests.Api
{
    public class DashboardControllerTests
    {
        private readonly Mock<IDashboardService> _dashboardServiceMock = new();
        private readonly Mock<ILogger<DashboardController>> _loggerMock = new();

        private DashboardController CreateController() =>
            new(_dashboardServiceMock.Object, _loggerMock.Object);

        [Fact]
        public async Task GetNewUsersCount_UnexpectedException_ReturnsGeneric500WithoutInternalDetails()
        {
            _dashboardServiceMock
                .Setup(s => s.CountNewUsersAsync(It.IsAny<int>()))
                .ThrowsAsync(new Exception("Npgsql: relation \"Accounts\" does not exist; password=hunter2"));

            var controller = CreateController();

            var result = await controller.GetNewUsersCount(30);

            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            var body = Assert.IsType<string>(statusCodeResult.Value);
            Assert.DoesNotContain("hunter2", body);
            Assert.DoesNotContain("Npgsql", body);
        }

        [Fact]
        public async Task GetNewUsersList_UnexpectedException_ReturnsGeneric500WithoutInternalDetails()
        {
            _dashboardServiceMock
                .Setup(s => s.ListNewUsersAsync(It.IsAny<int>()))
                .ThrowsAsync(new Exception("connection refused 10.0.0.42"));

            var controller = CreateController();

            var result = await controller.GetNewUsersList(30);

            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            var body = Assert.IsType<string>(statusCodeResult.Value);
            Assert.DoesNotContain("10.0.0.42", body);
        }

        [Fact]
        public async Task GetTrafficDistribution_ZeroVisitors_ReturnsZeroPercentagesInsteadOfError()
        {
            _dashboardServiceMock.Setup(s => s.GetRegisteredUsersTrafficCount()).ReturnsAsync(0);
            _dashboardServiceMock.Setup(s => s.GetUniqueGuestsTrafficCount()).ReturnsAsync(0);

            var controller = CreateController();

            var result = await controller.GetTrafficDistribution();

            var ok = Assert.IsType<OkObjectResult>(result);
            using var document = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));
            Assert.Equal(0, document.RootElement.GetProperty("TotalVisitors").GetInt32());
            Assert.Equal(0d, document.RootElement.GetProperty("RegisteredUsers").GetDouble());
            Assert.Equal(0d, document.RootElement.GetProperty("UniqueGuests").GetDouble());
        }

        [Fact]
        public async Task GetNewUsersCount_NegativeDays_Returns400()
        {
            _dashboardServiceMock
                .Setup(s => s.CountNewUsersAsync(It.IsAny<int>()))
                .ThrowsAsync(new ArgumentException("Days must be a positive integer.", "days"));

            var controller = CreateController();

            var result = await controller.GetNewUsersCount(-5);

            Assert.IsType<BadRequestObjectResult>(result);
        }
    }
}
