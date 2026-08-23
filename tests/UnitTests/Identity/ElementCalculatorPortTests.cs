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

        private sealed record ServiceHarness(
            AccountService Service,
            Mock<IIdentityReadStore> ReadStore,
            Mock<IIdentityWriteStore> WriteStore,
            Mock<IIdentityElementLookup> Lookup,
            Mock<ILogger<AccountService>> Logger);

        private static ServiceHarness CreateService(IElementCalculator? elementCalculator = null)
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

            var logger = new Mock<ILogger<AccountService>>();

            var passwordHasher = Mock.Of<IPasswordHasher>(h => h.Hash(It.IsAny<string>()) == "hashed");
            var sessionIssuer = new SessionIssuer(Mock.Of<IJwtTokenService>(), Mock.Of<IRefreshTokenPort>());
            var passwordResetService = new PasswordResetService(
                readStore.Object,
                writeStore.Object,
                Mock.Of<IPasswordResetTokenProvider>(),
                passwordHasher,
                Mock.Of<IIdentityEmailSender>(),
                sessionIssuer,
                new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build(),
                Mock.Of<ILogger<PasswordResetService>>());

            var service = new AccountService(
                readStore.Object,
                writeStore.Object,
                logger.Object,
                lookup.Object,
                elementCalculator ?? new FengShuiElementCalculator(),
                passwordHasher,
                passwordResetService,
                sessionIssuer);

            return new ServiceHarness(service, readStore, writeStore, lookup, logger);
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
            var harness = CreateService();

            await harness.Service.RegisterAsync(CreateRegisterRequest(1990, gender));

            var created = harness.WriteStore.Invocations
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
            var harness = CreateService();

            await harness.Service.RegisterAsync(CreateRegisterRequest(1990, gender));

            var created = harness.WriteStore.Invocations
                .Where(i => i.Method.Name == nameof(IIdentityWriteStore.CreateAccountAsync))
                .Select(i => (Account)i.Arguments[0])
                .Single();
            Assert.Equal(2, created.ElementId); // 1990 female -> Cấn/Thổ
        }

        [Fact]
        public async Task Register_AbsentGender_KeepsLegacyFemaleDefault()
        {
            var harness = CreateService();

            await harness.Service.RegisterAsync(CreateRegisterRequest(1990, null));

            var created = harness.WriteStore.Invocations
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
            var harness = CreateService();

            var ex = await Assert.ThrowsAsync<ArgumentException>(
                () => harness.Service.RegisterAsync(CreateRegisterRequest(1990, gender)));

            Assert.Equal("gender", ex.ParamName);
        }

        [Fact]
        public async Task Register_ResolvesElementIdThroughInjectedPort()
        {
            var port = new Mock<IElementCalculator>();
            port.Setup(p => p.CalculateElement(It.IsAny<int>(), It.IsAny<bool>())).Returns("Sentinel");
            var harness = CreateService(elementCalculator: port.Object);
            harness.Lookup.Setup(l => l.GetElementIdByNameAsync("Sentinel")).ReturnsAsync(99);

            await harness.Service.RegisterAsync(CreateRegisterRequest(1990, "male"));

            var created = harness.WriteStore.Invocations
                .Where(i => i.Method.Name == nameof(IIdentityWriteStore.CreateAccountAsync))
                .Select(i => (Account)i.Arguments[0])
                .Single();
            Assert.Equal(99, created.ElementId);
        }

        // --- Stored-data re-derivation boundary (login / profile-update element refresh) ---
        // Stored rows may hold legacy or unnormalized gender values; re-derivation must stay
        // lenient (female-branch fallback + warning) so authentication never hard-fails.

        private static Account CreateStoredAccount(int accountId, string? storedGender)
        {
            return new Account
            {
                AccountId = accountId,
                Email = $"stored{accountId}@test.local",
                Password = "password123", // legacy plaintext: verifies via the documented fallback
                Dob = new DateTime(1990, 1, 1),
                Gender = storedGender,
                RoleId = 2
            };
        }

        private static Account? CapturedUpdatedAccount(ServiceHarness harness)
        {
            return harness.WriteStore.Invocations
                .Where(i => i.Method.Name == nameof(IIdentityWriteStore.UpdateAccountAsync))
                .Select(i => (Account)i.Arguments[0])
                .SingleOrDefault();
        }

        private static void VerifyWarningLogged(Mock<ILogger<AccountService>> logger, Times times)
        {
            logger.Verify(
                l => l.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((state, type) => true),
                    It.IsAny<Exception?>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                times);
        }

        [Theory]
        [InlineData("Other")]
        [InlineData("Khác")]
        public async Task Authenticate_StoredUnrecognizedGender_DerivesElementViaFemaleBranch_AndWarns(string storedGender)
        {
            var harness = CreateService();
            var account = CreateStoredAccount(7, storedGender);
            harness.ReadStore.Setup(r => r.GetAccountByEmailAsync(account.Email)).ReturnsAsync(account);

            var result = await harness.Service.AuthenticateAsync(new AuthenticateRequest { Email = account.Email, Password = "password123" });

            Assert.Null(result.ErrorMessage);
            Assert.NotNull(result.Response);
            var updated = CapturedUpdatedAccount(harness);
            Assert.NotNull(updated);
            Assert.Equal(2, updated!.ElementId); // 1990 female branch, not an ArgumentException
            VerifyWarningLogged(harness.Logger, Times.Once());
        }

        [Fact]
        public async Task Update_StoredUnrecognizedGenderWithNoFreshInput_RefreshesElementLeniently()
        {
            var harness = CreateService();
            var account = CreateStoredAccount(5, "Khác");
            harness.ReadStore.Setup(r => r.GetAccountByIdAsync(account.AccountId)).ReturnsAsync(account);

            await harness.Service.UpdateAsync(account.AccountId, new UpdateRequest());

            Assert.Equal(2, account.ElementId); // lenient refresh from stored value, no throw
            VerifyWarningLogged(harness.Logger, Times.Once());
        }

        [Fact]
        public async Task Authenticate_AbsentStoredGender_DefaultsFemaleSilently()
        {
            var harness = CreateService();
            var account = CreateStoredAccount(7, null);
            harness.ReadStore.Setup(r => r.GetAccountByEmailAsync(account.Email)).ReturnsAsync(account);

            var result = await harness.Service.AuthenticateAsync(new AuthenticateRequest { Email = account.Email, Password = "password123" });

            Assert.Null(result.ErrorMessage);
            Assert.Equal(2, CapturedUpdatedAccount(harness)?.ElementId); // legacy default branch
            VerifyWarningLogged(harness.Logger, Times.Never()); // absent data is not dirty data: no ops noise
        }

        [Fact]
        public async Task Update_FreshUnrecognizedGender_StillThrowsArgumentException()
        {
            var harness = CreateService();
            var account = CreateStoredAccount(5, "Nam");
            harness.ReadStore.Setup(r => r.GetAccountByIdAsync(account.AccountId)).ReturnsAsync(account);

            var ex = await Assert.ThrowsAsync<ArgumentException>(
                () => harness.Service.UpdateAsync(account.AccountId, new UpdateRequest { Gender = "robot" }));

            Assert.Equal("gender", ex.ParamName);
        }

        [Theory]
        [InlineData(" Male ", 1)] // pre-widening this row derived via the female branch; now corrected to male
        [InlineData("M", 1)]
        [InlineData("Nam", 1)]
        [InlineData("nữ", 2)]
        [InlineData("f", 2)]
        public async Task Authenticate_StoredLegacyGenderAliases_AreDataCorrectedToMatchingBranch(string storedGender, int expectedElementId)
        {
            var harness = CreateService();
            var account = CreateStoredAccount(7, storedGender);
            harness.ReadStore.Setup(r => r.GetAccountByEmailAsync(account.Email)).ReturnsAsync(account);

            var result = await harness.Service.AuthenticateAsync(new AuthenticateRequest { Email = account.Email, Password = "password123" });

            Assert.Null(result.ErrorMessage);
            var updated = CapturedUpdatedAccount(harness);
            Assert.NotNull(updated);
            Assert.Equal(expectedElementId, updated!.ElementId); // recognized alias wins over the female default
            VerifyWarningLogged(harness.Logger, Times.Never()); // alias matched: not flagged as dirty data
        }
    }
}
