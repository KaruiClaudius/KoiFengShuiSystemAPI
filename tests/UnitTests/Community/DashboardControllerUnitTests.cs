using System.Text.Json;
using KoiFengShuiSystem.Modules.Community.Api.Controllers;
using KoiFengShuiSystem.Modules.Community.Application.Abstractions;
using KoiFengShuiSystem.Modules.Community.Application.Responses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace UnitTests.Community
{
    /// <summary>
    /// Ports the legacy DashboardControllerTests onto the module-owned controller:
    /// identical status codes, generic 500 bodies without internal detail, the
    /// zero-visitor divide guard, plus coverage for the new content summary.
    /// </summary>
    public class DashboardControllerUnitTests
    {
        private readonly Mock<ICommunityStore> _storeMock = new();
        private readonly Mock<ILogger<DashboardController>> _loggerMock = new();

        private DashboardController CreateController() =>
            new(_storeMock.Object, _loggerMock.Object);

        // ---- Ported legacy behaviors ----

        [Fact]
        public async Task GetNewUsersCount_UnexpectedException_ReturnsGeneric500WithoutInternalDetails()
        {
            _storeMock
                .Setup(s => s.GetAccountsCreatedSinceAsync(It.IsAny<DateTime>()))
                .ThrowsAsync(new Exception("Npgsql: relation \"Accounts\" does not exist; password=hunter2"));

            var result = await CreateController().GetNewUsersCount(30);

            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            var body = Assert.IsType<string>(statusCodeResult.Value);
            Assert.DoesNotContain("hunter2", body);
            Assert.DoesNotContain("Npgsql", body);
        }

        [Fact]
        public async Task GetNewUsersList_UnexpectedException_ReturnsGeneric500WithoutInternalDetails()
        {
            _storeMock
                .Setup(s => s.GetAccountsCreatedSinceAsync(It.IsAny<DateTime>()))
                .ThrowsAsync(new Exception("connection refused 10.0.0.42"));

            var result = await CreateController().GetNewUsersList(30);

            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
            var body = Assert.IsType<string>(statusCodeResult.Value);
            Assert.DoesNotContain("10.0.0.42", body);
        }

        [Fact]
        public async Task GetNewUsersCount_NonPositiveDays_Returns400WithoutTouchingStore()
        {
            var controller = CreateController();

            foreach (var days in new[] { 0, -5 })
            {
                var result = await controller.GetNewUsersCount(days);

                var badRequest = Assert.IsType<BadRequestObjectResult>(result);
                Assert.Equal("Days must be a positive integer.", badRequest.Value);
            }

            _storeMock.Verify(s => s.GetAccountsCreatedSinceAsync(It.IsAny<DateTime>()), Times.Never);
        }

        [Fact]
        public async Task GetNewUsersList_NonPositiveDays_Returns400WithoutTouchingStore()
        {
            var result = await CreateController().GetNewUsersList(0);

            Assert.IsType<BadRequestObjectResult>(result);
            _storeMock.Verify(s => s.GetAccountsCreatedSinceAsync(It.IsAny<DateTime>()), Times.Never);
        }

        [Fact]
        public async Task GetTrafficDistribution_ZeroVisitors_ReturnsZeroPercentagesInsteadOfError()
        {
            SeedTraffic(registered: 0, guests: 0);

            var ok = await GetTrafficDistributionBodyAsync();

            Assert.Equal(0, ok.GetProperty("TotalVisitors").GetInt32());
            Assert.Equal(0d, ok.GetProperty("RegisteredUsers").GetDouble());
            Assert.Equal(0d, ok.GetProperty("UniqueGuests").GetDouble());
        }

        [Fact]
        public async Task GetNewUsersCount_ReturnsMaterializedUserTotal()
        {
            _storeMock
                .Setup(s => s.GetAccountsCreatedSinceAsync(It.IsAny<DateTime>()))
                .ReturnsAsync(new List<RecentAccountSummary> { ValidSummary(1), ValidSummary(2), ValidSummary(3) });

            var result = await CreateController().GetNewUsersCount(30);

            var ok = Assert.IsType<OkObjectResult>(result);
            using var document = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value));
            Assert.Equal(3, document.RootElement.GetProperty("Count").GetInt32());
        }

        [Fact]
        public async Task GetNewUsersList_PassesWindowCutoffDerivedFromDays()
        {
            DateTime? capturedCutoff = null;
            _storeMock
                .Setup(s => s.GetAccountsCreatedSinceAsync(It.IsAny<DateTime>()))
                .Callback<DateTime>(cutoff => capturedCutoff = cutoff)
                .ReturnsAsync(new List<RecentAccountSummary>());

            await CreateController().GetNewUsersList(days: 7);

            Assert.NotNull(capturedCutoff);
            var expectedEarliest = DateTime.UtcNow.AddDays(-7).AddMinutes(-1);
            var expectedLatest = DateTime.UtcNow.AddDays(-7).AddMinutes(1);
            Assert.InRange(capturedCutoff!.Value, expectedEarliest, expectedLatest);
        }

        [Fact]
        public async Task GetTrafficDistribution_MixedVisitors_RoundsPercentagesToTwoDecimals()
        {
            SeedTraffic(registered: 1, guests: 3); // 25% / 75%

            var ok = await GetTrafficDistributionBodyAsync();

            Assert.Equal(25d, ok.GetProperty("RegisteredUsers").GetDouble());
            Assert.Equal(75d, ok.GetProperty("UniqueGuests").GetDouble());
            Assert.Equal(4, ok.GetProperty("TotalVisitors").GetInt32());
        }

        // ---- New content-aware endpoint ----

        [Fact]
        public async Task GetContentSummary_ComposesStoreTotalsIntoReport()
        {
            _storeMock.Setup(s => s.CountPostsAsync()).ReturnsAsync(12);
            _storeMock.Setup(s => s.CountPostsByCategoryAsync()).ReturnsAsync(new List<CategoryPostCount>
            {
                new(1, "Blog", 7),
                new(2, "Koi Care", 5)
            });
            _storeMock.Setup(s => s.CountPendingPostsAsync()).ReturnsAsync(4);

            var result = await CreateController().GetContentSummary();

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var report = Assert.IsType<ContentSummaryResponse>(ok.Value);
            Assert.Equal(12, report.TotalPosts);
            Assert.Equal(4, report.PendingCount);
            Assert.Collection(report.ByCategory,
                category =>
                {
                    Assert.Equal(1, category.CategoryId);
                    Assert.Equal("Blog", category.CategoryName);
                    Assert.Equal(7, category.Count);
                },
                category =>
                {
                    Assert.Equal(2, category.CategoryId);
                    Assert.Equal("Koi Care", category.CategoryName);
                    Assert.Equal(5, category.Count);
                });
        }

        [Fact]
        public async Task GetContentSummary_EmptyDatabase_ReturnsZeroedReport()
        {
            _storeMock.Setup(s => s.CountPostsAsync()).ReturnsAsync(0);
            _storeMock.Setup(s => s.CountPostsByCategoryAsync()).ReturnsAsync(new List<CategoryPostCount>());
            _storeMock.Setup(s => s.CountPendingPostsAsync()).ReturnsAsync(0);

            var result = await CreateController().GetContentSummary();

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var report = Assert.IsType<ContentSummaryResponse>(ok.Value);
            Assert.Equal(0, report.TotalPosts);
            Assert.Empty(report.ByCategory);
            Assert.Equal(0, report.PendingCount);
        }

        // ---- Helpers ----

        private void SeedTraffic(int registered, int guests)
        {
            _storeMock
                .Setup(s => s.CountDistinctRegisteredTrafficSinceAsync(It.IsAny<DateTime>()))
                .ReturnsAsync(registered);
            _storeMock
                .Setup(s => s.CountDistinctGuestTrafficSinceAsync(It.IsAny<DateTime>()))
                .ReturnsAsync(guests);
        }

        private async Task<JsonElement> GetTrafficDistributionBodyAsync()
        {
            var result = await CreateController().GetTrafficDistribution();
            var ok = Assert.IsType<OkObjectResult>(result);
            return JsonDocument.Parse(JsonSerializer.Serialize(ok.Value)).RootElement.Clone();
        }

        private static RecentAccountSummary ValidSummary(int id) => new(
            id,
            $"User {id}",
            $"user{id}@test.local",
            null,
            null,
            null,
            null,
            2,
            DateTime.UtcNow.AddDays(-1),
            DateTime.UtcNow);
    }
}
