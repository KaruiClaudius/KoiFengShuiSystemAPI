using KoiFengShuiSystem.Modules.Identity.Application.Abstractions;
using KoiFengShuiSystem.Modules.Identity.Application.Requests;
using KoiFengShuiSystem.Modules.Identity.Application.Services;
using KoiFengShuiSystem.Modules.Identity.Domain.Entities;
using KoiFengShuiSystem.Modules.Identity.Infrastructure.FengShui;
using Microsoft.Extensions.Logging;
using Moq;

namespace UnitTests.Identity
{
    /// <summary>
    /// Pins the shared element calculator behind the Identity port to the exact outputs of the
    /// legacy private <c>AccountService.CalculateElement</c> implementation (vectors extracted
    /// from the original private map and algorithm before it was deleted).
    /// </summary>
    public class ElementCalculatorPortTests
    {
        public static IEnumerable<object[]> LegacyParityVectors => new[]
        {
            // Pre-2000 male branch
            new object[] { 1990, true, "Thủy" },
            new object[] { 1984, true, "Kim" },
            new object[] { 1980, true, "Thổ" },
            new object[] { 1995, true, "Thổ" },   // trung cung -> male maps to 2
            new object[] { 1999, true, "Thủy" },
            new object[] { 1956, true, "Thổ" },
            new object[] { 1962, true, "Thổ" },
            new object[] { 1977, true, "Thổ" },   // trung cung -> male maps to 2
            new object[] { 1993, true, "Kim" },
            // Pre-2000 female branch
            new object[] { 1990, false, "Thổ" },
            new object[] { 1984, false, "Thổ" },
            new object[] { 1980, false, "Mộc" },
            new object[] { 1995, false, "Thủy" },
            new object[] { 1999, false, "Thổ" },
            new object[] { 1956, false, "Kim" },
            new object[] { 1962, false, "Mộc" },
            new object[] { 1977, false, "Thủy" },
            new object[] { 1993, false, "Thổ" },
            // Post-2000 male branch
            new object[] { 2000, true, "Hoả" },
            new object[] { 2025, true, "Thổ" },
            new object[] { 2009, true, "Hoả" },   // zero result wraps to 9
            new object[] { 2030, true, "Kim" },
            new object[] { 2024, true, "Mộc" },
            new object[] { 2011, true, "Kim" },
            new object[] { 2005, true, "Mộc" },
            // Post-2000 female branch
            new object[] { 2000, false, "Kim" },
            new object[] { 2025, false, "Mộc" },
            new object[] { 2009, false, "Kim" },
            new object[] { 2030, false, "Hoả" },
            new object[] { 2024, false, "Mộc" },
            new object[] { 2011, false, "Thổ" },
            new object[] { 2005, false, "Thổ" }
        };

        [Theory]
        [MemberData(nameof(LegacyParityVectors))]
        public void CalculateElement_MatchesLegacyAccountServiceOutputs(int yearOfBirth, bool isMale, string expectedMenh)
        {
            IElementCalculator calculator = new FengShuiElementCalculator();

            var elementName = calculator.CalculateElement(yearOfBirth, isMale);

            Assert.Equal(expectedMenh, elementName);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void CalculateElement_InvalidYear_ThrowsArgumentException(int yearOfBirth)
        {
            IElementCalculator calculator = new FengShuiElementCalculator();

            Assert.Throws<ArgumentException>(() => calculator.CalculateElement(yearOfBirth, true));
        }

        // --- Gender string normalization at the Application boundary ---

        private static (AccountService Service, Mock<IIdentityWriteStore> WriteStore, Mock<IIdentityElementLookup> Lookup) CreateService(
            IElementCalculator? elementCalculator = null)
        {
            var readStore = new Mock<IIdentityReadStore>();
            readStore.Setup(r => r.GetAccountByEmailAsync(It.IsAny<string>())).ReturnsAsync((Account?)null);

            var writeStore = new Mock<IIdentityWriteStore>();
            Account? created = null;
            writeStore.Setup(w => w.CreateAccountAsync(It.IsAny<Account>()))
                .Callback<Account>(account => created = account)
                .ReturnsAsync((Account account) => account);

            var lookup = new Mock<IIdentityElementLookup>();
            var idsByName = new Dictionary<string, int>
            {
                ["Thủy"] = 1,
                ["Thổ"] = 2,
                ["Mộc"] = 3,
                ["Kim"] = 4,
                ["Hoả"] = 5
            };
            lookup.Setup(l => l.GetElementIdByNameAsync(It.IsAny<string>()))
                .ReturnsAsync((string name) => idsByName.TryGetValue(name, out var id) ? id : (int?)null);
            lookup.Setup(l => l.GetElementNameByIdAsync(It.IsAny<int>())).ReturnsAsync((string?)null);

            var service = new AccountService(
                readStore.Object,
                writeStore.Object,
                Mock.Of<IJwtTokenService>(),
                Mock.Of<IIdentityEmailSender>(),
                Mock.Of<ILogger<AccountService>>(),
                lookup.Object,
                elementCalculator ?? new FengShuiElementCalculator(),
                Mock.Of<IPasswordHasher>(h => h.Hash(It.IsAny<string>()) == "hashed"),
                Mock.Of<IPasswordResetTokenProvider>(),
                new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build(),
                Mock.Of<IRefreshTokenPort>());

            return (service, writeStore, lookup);
        }

        private static RegisterRequest CreateRegisterRequest(int yearOfBirth, string? gender)
        {
            return new RegisterRequest
            {
                FullName = "Test User",
                Email = $"{Guid.NewGuid():N}@test.com",
                Password = "secret123",
                Dob = new DateTime(yearOfBirth, 6, 15),
                Phone = "0123456789",
                Gender = gender
            };
        }

        [Theory]
        [InlineData("male")]
        [InlineData("nam")]
        [InlineData("m")]
        [InlineData("MALE")]
        [InlineData("NAM")]
        [InlineData(" Male ")]
        public async Task Register_MaleGenderAliases_AssignsMalePathElement(string gender)
        {
            var (service, writeStore, _) = CreateService();

            await service.RegisterAsync(CreateRegisterRequest(1990, gender));

            var created = writeStore.Invocations
                .Where(i => i.Method.Name == nameof(IIdentityWriteStore.CreateAccountAsync))
                .Select(i => (Account)i.Arguments[0])
                .Single();
            Assert.Equal(1, created.ElementId); // 1990 male -> Thủy
        }

        [Theory]
        [InlineData("female")]
        [InlineData("nữ")]
        [InlineData("nu")]
        [InlineData("f")]
        [InlineData("FEMALE")]
        [InlineData("NỮ")]
        public async Task Register_FemaleGenderAliases_AssignsFemalePathElement(string gender)
        {
            var (service, writeStore, _) = CreateService();

            await service.RegisterAsync(CreateRegisterRequest(1990, gender));

            var created = writeStore.Invocations
                .Where(i => i.Method.Name == nameof(IIdentityWriteStore.CreateAccountAsync))
                .Select(i => (Account)i.Arguments[0])
                .Single();
            Assert.Equal(2, created.ElementId); // 1990 female -> Cấn/Thổ
        }

        [Fact]
        public async Task Register_AbsentGender_KeepsLegacyFemaleDefault()
        {
            var (service, writeStore, _) = CreateService();

            await service.RegisterAsync(CreateRegisterRequest(1990, null));

            var created = writeStore.Invocations
                .Where(i => i.Method.Name == nameof(IIdentityWriteStore.CreateAccountAsync))
                .Select(i => (Account)i.Arguments[0])
                .Single();
            Assert.Equal(2, created.ElementId); // legacy default: absent gender follows the female path
        }

        [Theory]
        [InlineData("robot")]
        [InlineData("other")]
        [InlineData("namemale")]
        public async Task Register_UnrecognizedGender_ThrowsArgumentException(string gender)
        {
            var (service, _, _) = CreateService();

            await Assert.ThrowsAsync<ArgumentException>(() => service.RegisterAsync(CreateRegisterRequest(1990, gender)));
        }

        [Fact]
        public async Task Register_ResolvesElementIdThroughInjectedPort()
        {
            var port = new Mock<IElementCalculator>();
            port.Setup(p => p.CalculateElement(It.IsAny<int>(), It.IsAny<bool>())).Returns("Sentinel");
            var (service, writeStore, lookup) = CreateService(elementCalculator: port.Object);
            lookup.Setup(l => l.GetElementIdByNameAsync("Sentinel")).ReturnsAsync(99);

            await service.RegisterAsync(CreateRegisterRequest(1990, "male"));

            var created = writeStore.Invocations
                .Where(i => i.Method.Name == nameof(IIdentityWriteStore.CreateAccountAsync))
                .Select(i => (Account)i.Arguments[0])
                .Single();
            Assert.Equal(99, created.ElementId);
        }
    }
}
