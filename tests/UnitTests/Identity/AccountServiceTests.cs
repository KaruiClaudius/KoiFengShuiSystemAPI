using KoiFengShuiSystem.DataAccess.Base;
using KoiFengShuiSystem.BusinessLogic.Services.Implement;
using KoiFengShuiSystem.Modules.Identity.Application.Abstractions;
using KoiFengShuiSystem.Modules.Identity.Application.Requests;
using KoiFengShuiSystem.Modules.Identity.Application.Responses;
using KoiFengShuiSystem.Modules.Identity.Application.Services;
using KoiFengShuiSystem.Modules.Identity.Infrastructure.Email;
using KoiFengShuiSystem.Modules.Identity.Infrastructure.Persistence;
using KoiFengShuiSystem.Modules.Identity.Infrastructure.Security;
using KoiFengShuiSystem.Modules.FengShui.Domain.Entities;
using KoiFengShuiSystem.Modules.Identity.Domain.Entities;
using AccountEntity = KoiFengShuiSystem.Modules.Identity.Domain.Entities.Account;
using IdentityAccountService = KoiFengShuiSystem.Modules.Identity.Application.Services.AccountService;
using KoiFengShuiSystem.Shared.Helpers;
using KoiFengShuiSystem.Shared.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace UnitTests.Identity
{
    public class AccountServiceTests
    {
        private const string JwtSecret = "test-secret-key-that-is-at-least-32-bytes-long-for-hmac";

        private static DbContextOptions<KoiFengShuiContext> CreateInMemoryOptions()
        {
            return new DbContextOptionsBuilder<KoiFengShuiContext>()
                .UseInMemoryDatabase(databaseName: $"AccountTestDb_{Guid.NewGuid()}")
                .Options;
        }

        private static KoiFengShuiContext CreateContext()
        {
            return new KoiFengShuiContext(CreateInMemoryOptions());
        }

        private static JwtTokenService CreateJwtTokenService()
        {
            return new JwtTokenService(Options.Create(new AppSettings { Secret = JwtSecret }));
        }

        private static EmailService CreateEmailService()
        {
            var mailSettingsMock = new Mock<IOptions<MailSettings>>();
            mailSettingsMock.Setup(m => m.Value).Returns(new MailSettings
            {
                Server = "localhost",
                Port = 25,
                SenderName = "Test",
                SenderEmail = "test@test.com",
                UserName = "",
                Password = "",
                UseSSL = false,
                UseStartTls = false
            });
            return new EmailService(mailSettingsMock.Object, Mock.Of<ILogger<EmailService>>());
        }

        private static KoiFengShuiContext CreateContextWithSeedData()
        {
            var context = CreateContext();

            context.Elements.Add(new Element
            {
                ElementId = 1,
                ElementName = "Thuy",
                Description = "Water",
                LuckyNumber = "1,6"
            });

            context.Accounts.Add(new AccountEntity
            {
                AccountId = 1,
                FullName = "Test User",
                Email = "test@test.com",
                Password = "password123",
                CreateAt = DateTime.Now,
                UpdateAt = DateTime.Now,
                RoleId = 2
            });

            context.SaveChanges();
            return context;
        }

        private static KoiFengShuiContext CreateContextWithElementAndDob()
        {
            var context = CreateContext();

            var thuyName = KoiFengShuiSystem.Modules.FengShui.Domain.Calculations.CungPhiCalculator.Calculate(1990, KoiFengShuiSystem.Modules.FengShui.Domain.Calculations.Gender.Male).Menh;

            context.Elements.Add(new Element
            {
                ElementId = 1,
                ElementName = thuyName,
                Description = "Water",
                LuckyNumber = "1,6"
            });

            context.Accounts.Add(new AccountEntity
            {
                AccountId = 1,
                FullName = "Test User",
                Email = "test@test.com",
                Password = "password123",
                Dob = new DateTime(1990, 1, 1),
                Gender = "male",
                CreateAt = DateTime.Now,
                UpdateAt = DateTime.Now,
                RoleId = 2
            });

            context.SaveChanges();
            return context;
        }

        private static IdentityAccountService CreateService(
            KoiFengShuiContext? context = null,
            IJwtTokenService? jwtTokenService = null,
            IIdentityEmailSender? identityEmailSender = null,
            IIdentityElementLookup? elementLookup = null,
            IPasswordHasher? passwordHasher = null,
            IPasswordResetTokenProvider? passwordResetTokenProvider = null,
            IConfiguration? configuration = null,
            IRefreshTokenPort? refreshTokenPort = null)
        {
            var ctx = context ?? CreateContext();
            var jwt = jwtTokenService ?? Mock.Of<IJwtTokenService>(j => j.GenerateJwtToken(It.IsAny<AccountEntity>()) == "test-token");
            var email = identityEmailSender ?? Mock.Of<IIdentityEmailSender>();
            var lookup = elementLookup ?? new EfIdentityElementLookup(ctx);
            var hasher = passwordHasher ?? new BcryptPasswordHasher();
            var tokenProvider = passwordResetTokenProvider ?? new SecurePasswordResetTokenProvider();
            var config = configuration ?? new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["AppSettings:FrontendBaseUrl"] = "http://localhost:3000"
                })
                .Build();
            var logger = Mock.Of<ILogger<IdentityAccountService>>();
            var port = refreshTokenPort ?? Mock.Of<IRefreshTokenPort>();

            return new IdentityAccountService(
                new EfIdentityReadStore(ctx),
                new EfIdentityWriteStore(ctx),
                jwt,
                email,
                logger,
                lookup,
                new KoiFengShuiSystem.Modules.Identity.Infrastructure.FengShui.FengShuiElementCalculator(),
                hasher,
                tokenProvider,
                config,
                port);
        }

        private static IConfiguration CreateConfiguration(string baseUrl)
            => new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["AppSettings:FrontendBaseUrl"] = baseUrl
                })
                .Build();

        // --- Constructor ---

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(5)]
        [InlineData(6)]
        [InlineData(7)]
        [InlineData(8)]
        [InlineData(9)]
        [InlineData(10)]
        public void Constructor_NullDependency_ThrowsArgumentNullException(int nullDependencyIndex)
        {
            var ctx = CreateContext();

            IIdentityReadStore readStore = new EfIdentityReadStore(ctx);
            IIdentityWriteStore writeStore = new EfIdentityWriteStore(ctx);
            IJwtTokenService jwtTokenService = CreateJwtTokenService();
            IIdentityEmailSender identityEmailSender = Mock.Of<IIdentityEmailSender>();
            ILogger<IdentityAccountService> logger = Mock.Of<ILogger<IdentityAccountService>>();
            IIdentityElementLookup elementLookup = new EfIdentityElementLookup(ctx);
            KoiFengShuiSystem.Modules.Identity.Infrastructure.FengShui.FengShuiElementCalculator elementCalculator = new();
            IPasswordHasher passwordHasher = new BcryptPasswordHasher();
            IPasswordResetTokenProvider tokenProvider = new SecurePasswordResetTokenProvider();
            IConfiguration configuration = CreateConfiguration("http://localhost:3000");
            IRefreshTokenPort refreshTokenPort = Mock.Of<IRefreshTokenPort>();

            var ex = Assert.Throws<ArgumentNullException>(() => new IdentityAccountService(
                nullDependencyIndex == 0 ? null! : readStore,
                nullDependencyIndex == 1 ? null! : writeStore,
                nullDependencyIndex == 2 ? null! : jwtTokenService,
                nullDependencyIndex == 3 ? null! : identityEmailSender,
                nullDependencyIndex == 4 ? null! : logger,
                nullDependencyIndex == 5 ? null! : elementLookup,
                nullDependencyIndex == 6 ? null! : elementCalculator,
                nullDependencyIndex == 7 ? null! : passwordHasher,
                nullDependencyIndex == 8 ? null! : tokenProvider,
                nullDependencyIndex == 9 ? null! : configuration,
                nullDependencyIndex == 10 ? null! : refreshTokenPort));

            Assert.NotNull(ex.ParamName);
        }

        [Fact]
        public void Constructor_WithValidDependencies_Succeeds()
        {
            var ctx = CreateContext();
            var logger = Mock.Of<ILogger<IdentityAccountService>>();

            var service = new IdentityAccountService(
                new EfIdentityReadStore(ctx),
                new EfIdentityWriteStore(ctx),
                CreateJwtTokenService(),
                Mock.Of<IIdentityEmailSender>(),
                logger,
                new EfIdentityElementLookup(ctx),
                new KoiFengShuiSystem.Modules.Identity.Infrastructure.FengShui.FengShuiElementCalculator(),
                new BcryptPasswordHasher(),
                new SecurePasswordResetTokenProvider(),
                CreateConfiguration("http://localhost:3000"),
                Mock.Of<IRefreshTokenPort>());

            Assert.NotNull(service);
        }

        [Fact]
        public void EfIdentityElementLookup_NullContext_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new EfIdentityElementLookup(null!));
        }

        // --- GetAllAsync ---

        [Fact]
        public async Task GetAllAsync_EmptyDatabase_ReturnsEmptyList()
        {
            var context = CreateContext();
            var service = CreateService(context);

            var result = await service.GetAllAsync();

            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public async Task GetAllAsync_WithSeedData_ReturnsAllAccounts()
        {
            var context = CreateContextWithSeedData();
            var service = CreateService(context);

            var result = await service.GetAllAsync();

            Assert.NotNull(result);
            Assert.Single(result);
        }

        // --- GetByIdAsync ---

        [Fact]
        public async Task GetByIdAsync_NonExistentId_ReturnsNull()
        {
            var context = CreateContext();
            var service = CreateService(context);

            var result = await service.GetByIdAsync(999);

            Assert.Null(result);
        }

        [Fact]
        public async Task GetByIdAsync_ExistingId_ReturnsAccount()
        {
            var context = CreateContextWithSeedData();
            var service = CreateService(context);

            var result = await service.GetByIdAsync(1);

            Assert.NotNull(result);
            Assert.Equal("Test User", result.FullName);
            Assert.Equal("test@test.com", result.Email);
        }

        // --- AuthenticateAsync ---

        [Fact]
        public async Task AuthenticateAsync_EmailNotFound_ReturnsError()
        {
            var context = CreateContext();
            var service = CreateService(context);
            var request = new AuthenticateRequest { Email = "nonexistent@test.com", Password = "pwd" };

            var result = await service.AuthenticateAsync(request);

            Assert.NotNull(result);
            Assert.Null(result.Response);
            Assert.NotNull(result.ErrorMessage);
            Assert.Equal("Email not found.", result.ErrorMessage);
            Assert.False(result.Success);
        }

        [Fact]
        public async Task AuthenticateAsync_WrongPassword_ReturnsError()
        {
            var context = CreateContextWithSeedData();
            var service = CreateService(context);
            var request = new AuthenticateRequest { Email = "test@test.com", Password = "wrongpassword" };

            var result = await service.AuthenticateAsync(request);

            Assert.NotNull(result);
            Assert.Null(result.Response);
            Assert.NotNull(result.ErrorMessage);
            Assert.Equal("Incorrect password.", result.ErrorMessage);
            Assert.False(result.Success);
        }

        [Fact]
        public async Task AuthenticateAsync_ValidCredentials_ReturnsSuccess()
        {
            var context = CreateContextWithSeedData();
            var jwtMock = new Mock<IJwtTokenService>();
            jwtMock
                .Setup(service => service.GenerateJwtToken(It.IsAny<AccountEntity>()))
                .Returns("generated-jwt-token");
            var service = CreateService(context, jwtTokenService: jwtMock.Object);
            var request = new AuthenticateRequest { Email = "test@test.com", Password = "password123" };

            var result = await service.AuthenticateAsync(request);

            Assert.NotNull(result);
            Assert.NotNull(result.Response);
            Assert.Null(result.ErrorMessage);
            Assert.True(result.Success);
            Assert.Equal("Test User", result.Response.FullName);
            Assert.Equal("test@test.com", result.Response.Email);
            Assert.Equal("generated-jwt-token", result.Response.Token);
        }

        [Fact]
        public async Task AuthenticateAsync_NullPassword_ReturnsError()
        {
            var context = CreateContextWithSeedData();
            var service = CreateService(context);
            var request = new AuthenticateRequest { Email = "test@test.com", Password = null };

            var result = await service.AuthenticateAsync(request);

            Assert.False(result.Success);
            Assert.Equal("Incorrect password.", result.ErrorMessage);
        }

        // --- AuthenticateAsync: refresh-token issuance ---

        [Fact]
        public async Task AuthenticateAsync_ValidCredentials_ResponseIncludesRefreshTokenAndExpiresIn()
        {
            var context = CreateContextWithSeedData();
            var jwtMock = new Mock<IJwtTokenService>();
            jwtMock
                .Setup(service => service.GenerateJwtToken(It.IsAny<AccountEntity>()))
                .Returns("generated-jwt-token");
            jwtMock.SetupGet(service => service.AccessTokenLifetimeMinutes).Returns(15);
            var portMock = new Mock<IRefreshTokenPort>();
            portMock.Setup(port => port.CreateForAccountAsync(1)).ReturnsAsync("raw-refresh-token");
            var service = CreateService(context, jwtTokenService: jwtMock.Object, refreshTokenPort: portMock.Object);

            var result = await service.AuthenticateAsync(new AuthenticateRequest { Email = "test@test.com", Password = "password123" });

            Assert.True(result.Success);
            Assert.Equal("generated-jwt-token", result.Response!.Token);
            Assert.Equal("raw-refresh-token", result.Response.RefreshToken);
            Assert.Equal(15, result.Response.ExpiresInMinutes);
        }

        [Fact]
        public async Task AuthenticateAsync_ConfiguredAccessTokenLifetime_AdvertisesSameExpiresIn()
        {
            var context = CreateContextWithSeedData();
            var jwtMock = new Mock<IJwtTokenService>();
            jwtMock
                .Setup(service => service.GenerateJwtToken(It.IsAny<AccountEntity>()))
                .Returns("generated-jwt-token");
            jwtMock.SetupGet(service => service.AccessTokenLifetimeMinutes).Returns(7);
            var service = CreateService(context, jwtTokenService: jwtMock.Object);

            var result = await service.AuthenticateAsync(new AuthenticateRequest { Email = "test@test.com", Password = "password123" });

            Assert.True(result.Success);
            Assert.Equal(7, result.Response!.ExpiresInMinutes);
        }

        [Fact]
        public async Task AuthenticateAsync_FailedLogin_DoesNotCreateRefreshToken()
        {
            var context = CreateContextWithSeedData();
            var portMock = new Mock<IRefreshTokenPort>();
            var service = CreateService(context, refreshTokenPort: portMock.Object);

            var result = await service.AuthenticateAsync(new AuthenticateRequest { Email = "test@test.com", Password = "wrong-password" });

            Assert.False(result.Success);
            portMock.Verify(port => port.CreateForAccountAsync(It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task AuthenticateAsync_BcryptStoredPassword_ValidLogin_Succeeds()
        {
            var hasher = new BcryptPasswordHasher();
            var context = CreateContextWithSeedData();
            context.Accounts.First().Password = hasher.Hash("password123");
            context.SaveChanges();

            var service = CreateService(context, configuration: CreateConfiguration("http://localhost:3000"));
            var request = new AuthenticateRequest { Email = "test@test.com", Password = "password123" };

            var result = await service.AuthenticateAsync(request);

            Assert.True(result.Success);
        }

        [Fact]
        public async Task AuthenticateAsync_LegacyPlaintextCredentials_UpgradesToBcryptOnSuccessfulLogin()
        {
            var hasher = new BcryptPasswordHasher();
            var context = CreateContextWithSeedData();
            context.ChangeTracker.Clear();
            var service = CreateService(context);
            var request = new AuthenticateRequest { Email = "test@test.com", Password = "password123" };

            var result = await service.AuthenticateAsync(request);

            Assert.True(result.Success);
            var stored = await context.Accounts.SingleAsync(a => a.Email == "test@test.com");
            Assert.StartsWith("$2", stored.Password);
            Assert.True(hasher.Verify("password123", stored.Password!));
        }

        [Fact]
        public async Task AuthenticateAsync_LegacyPlaintextCredentials_WrongPassword_DoesNotUpgrade()
        {
            var context = CreateContextWithSeedData();
            context.ChangeTracker.Clear();
            var service = CreateService(context);
            var request = new AuthenticateRequest { Email = "test@test.com", Password = "wrong-password" };

            var result = await service.AuthenticateAsync(request);

            Assert.False(result.Success);
            var stored = await context.Accounts.SingleAsync(a => a.Email == "test@test.com");
            Assert.Equal("password123", stored.Password);
        }

        // --- RegisterAsync ---

        [Fact]
        public async Task RegisterAsync_ValidRequest_CreatesAccount()
        {
            var context = CreateContext();
            var thuyName = KoiFengShuiSystem.Modules.FengShui.Domain.Calculations.CungPhiCalculator.Calculate(1990, KoiFengShuiSystem.Modules.FengShui.Domain.Calculations.Gender.Male).Menh;
            context.Elements.Add(new Element
            {
                ElementId = 1,
                ElementName = thuyName,
                Description = "Water",
                LuckyNumber = "1,6"
            });
            context.SaveChanges();

            var service = CreateService(context);
            var request = new RegisterRequest
            {
                FullName = "New User",
                Email = "newuser@test.com",
                Password = "password123",
                Dob = new DateTime(1990, 1, 1),
                Phone = "123456789",
                Gender = "male"
            };

            var result = await service.RegisterAsync(request);

            Assert.NotNull(result);
            Assert.Equal("New User", result.FullName);
            Assert.Equal("newuser@test.com", result.Email);
            Assert.Equal(2, result.RoleId);
            Assert.NotNull(result.ElementId);

            var stored = await context.Accounts.SingleAsync(a => a.Email == "newuser@test.com");
            Assert.StartsWith("$2", stored.Password);
            Assert.NotEqual("password123", stored.Password);
            Assert.True(new BcryptPasswordHasher().Verify("password123", stored.Password!));
        }

        [Fact]
        public async Task RegisterAsync_DuplicateEmail_ThrowsApplicationException()
        {
            var context = CreateContextWithSeedData();
            var service = CreateService(context);
            var request = new RegisterRequest
            {
                FullName = "Duplicate",
                Email = "test@test.com",
                Password = "password123",
                Dob = new DateTime(1990, 1, 1),
                Phone = "123456789",
                Gender = "male"
            };

            var ex = await Assert.ThrowsAsync<ApplicationException>(() => service.RegisterAsync(request));
            Assert.Contains("already taken", ex.Message);
        }

        // --- GetAccountByEmailAsync ---

        [Fact]
        public async Task GetAccountByEmailAsync_ExistingEmail_ReturnsAccount()
        {
            var context = CreateContextWithSeedData();
            var service = CreateService(context);

            var result = await service.GetAccountByEmailAsync("test@test.com");

            Assert.NotNull(result);
            Assert.Equal("Test User", result.FullName);
        }

        [Fact]
        public async Task GetAccountByEmailAsync_NonExistentEmail_ReturnsNull()
        {
            var context = CreateContext();
            var service = CreateService(context);

            var result = await service.GetAccountByEmailAsync("nonexistent@test.com");

            Assert.Null(result);
        }

        // --- UpdateUserPasswordAsync ---

        [Fact]
        public async Task UpdateUserPasswordAsync_NullAccount_ThrowsArgumentNullException()
        {
            var context = CreateContext();
            var service = CreateService(context);

            var ex = await Assert.ThrowsAsync<ArgumentNullException>(() =>
                service.UpdateUserPasswordAsync(null!, "newpass"));
            Assert.Contains("account", ex.Message);
        }

        [Fact]
        public async Task UpdateUserPasswordAsync_EmptyPassword_ThrowsArgumentException()
        {
            var context = CreateContext();
            var service = CreateService(context);

            var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
                service.UpdateUserPasswordAsync(new AccountEntity { AccountId = 1, Email = "test@test.com" }, ""));
            Assert.Contains("password", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task UpdateUserPasswordAsync_ExistingAccount_StoresHashedPassword()
        {
            var context = CreateContextWithSeedData();
            context.ChangeTracker.Clear();
            var service = CreateService(context);
            var hasher = new BcryptPasswordHasher();

            var AccountEntity = new AccountEntity { AccountId = 1, Email = "test@test.com" };
            await service.UpdateUserPasswordAsync(AccountEntity, "newPassword123");

            var updated = await service.GetByIdAsync(1);
            Assert.NotNull(updated!.Password);
            Assert.StartsWith("$2", updated.Password);
            Assert.True(hasher.Verify("newPassword123", updated.Password!));
        }

        [Fact]
        public async Task UpdateUserPasswordAsync_NonExistentAccount_ThrowsKeyNotFoundException()
        {
            var context = CreateContext();
            var service = CreateService(context);

            var nonExistent = new AccountEntity { AccountId = 999, Email = "ghost@test.com" };

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                service.UpdateUserPasswordAsync(nonExistent, "newpass"));
        }

        // --- UpdateAsync ---

        [Fact]
        public async Task UpdateAsync_ExistingAccount_UpdatesProperties()
        {
            var context = CreateContextWithElementAndDob();
            var service = CreateService(context);

            var request = new UpdateRequest
            {
                FullName = "Updated Name",
                Phone = "0987654321"
            };

            await service.UpdateAsync(1, request);

            var updated = await service.GetByIdAsync(1);
            Assert.Equal("Updated Name", updated!.FullName);
            Assert.Equal("0987654321", updated.Phone);
        }

        [Fact]
        public async Task UpdateAsync_NonExistentId_ThrowsApplicationException()
        {
            var context = CreateContext();
            var service = CreateService(context);

            var request = new UpdateRequest { FullName = "Ghost" };

            var ex = await Assert.ThrowsAsync<ApplicationException>(() => service.UpdateAsync(999, request));
            Assert.Contains("not found", ex.Message);
        }

        [Fact]
        public async Task UpdateAsync_DuplicateEmail_ThrowsApplicationException()
        {
            var context = CreateContext();
            var thuyName = KoiFengShuiSystem.Modules.FengShui.Domain.Calculations.CungPhiCalculator.Calculate(1990, KoiFengShuiSystem.Modules.FengShui.Domain.Calculations.Gender.Male).Menh;
            context.Elements.Add(new Element
            {
                ElementId = 1,
                ElementName = thuyName,
                Description = "Water",
                LuckyNumber = "1,6"
            });
            context.Accounts.AddRange(
                new AccountEntity
                {
                    AccountId = 1,
                    FullName = "User One",
                    Email = "one@test.com",
                    Password = "pass",
                    Dob = new DateTime(1990, 1, 1),
                    Gender = "male",
                    CreateAt = DateTime.Now,
                    UpdateAt = DateTime.Now,
                    RoleId = 2
                },
                new AccountEntity
                {
                    AccountId = 2,
                    FullName = "User Two",
                    Email = "two@test.com",
                    Password = "pass",
                    CreateAt = DateTime.Now,
                    UpdateAt = DateTime.Now,
                    RoleId = 2
                }
            );
            context.SaveChanges();

            var service = CreateService(context);
            var request = new UpdateRequest { Email = "two@test.com" };

            var ex = await Assert.ThrowsAsync<ApplicationException>(() => service.UpdateAsync(1, request));
            Assert.Contains("already taken", ex.Message);
        }

        // --- DeleteAsync ---

        [Fact]
        public async Task DeleteAsync_ExistingId_DeletesAccount()
        {
            var context = CreateContextWithSeedData();
            var service = CreateService(context);

            await service.DeleteAsync(1);

            var deleted = await service.GetByIdAsync(1);
            Assert.Null(deleted);
        }

        [Fact]
        public async Task DeleteAsync_NonExistentId_ThrowsApplicationException()
        {
            var context = CreateContext();
            var service = CreateService(context);

            var ex = await Assert.ThrowsAsync<ApplicationException>(() => service.DeleteAsync(999));
            Assert.Contains("not found", ex.Message);
        }

        // --- ChangePasswordAsync ---

        [Fact]
        public async Task ChangePasswordAsync_ValidCredentials_ReturnsTrue()
        {
            var context = CreateContextWithSeedData();
            var service = CreateService(context);
            var hasher = new BcryptPasswordHasher();

            var result = await service.ChangePasswordAsync(1, "password123", "newPassword456");

            Assert.True(result);
            var updated = await service.GetByIdAsync(1);
            Assert.NotNull(updated!.Password);
            Assert.StartsWith("$2", updated.Password);
            Assert.True(hasher.Verify("newPassword456", updated.Password!));
            Assert.False(hasher.Verify("password123", updated.Password!));
        }

        [Fact]
        public async Task ChangePasswordAsync_LegacyBcryptStoredCurrentPassword_VerifiesAndRehashes()
        {
            var hasher = new BcryptPasswordHasher();
            var context = CreateContextWithSeedData();
            context.Accounts.First().Password = hasher.Hash("oldPassword123");
            context.SaveChanges();
            context.ChangeTracker.Clear();
            var service = CreateService(context);

            var result = await service.ChangePasswordAsync(1, "oldPassword123", "newPassword456");

            Assert.True(result);
            var updated = await service.GetByIdAsync(1);
            Assert.NotNull(updated!.Password);
            Assert.True(hasher.Verify("newPassword456", updated.Password!));
        }

        [Fact]
        public async Task ChangePasswordAsync_WrongCurrentPassword_ReturnsFalse()
        {
            var context = CreateContextWithSeedData();
            var service = CreateService(context);

            var result = await service.ChangePasswordAsync(1, "wrongCurrentPass", "newPassword456");

            Assert.False(result);
            var unchanged = await service.GetByIdAsync(1);
            Assert.Equal("password123", unchanged!.Password);
        }

        [Fact]
        public async Task ChangePasswordAsync_NonExistentId_ThrowsKeyNotFoundException()
        {
            var context = CreateContext();
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                service.ChangePasswordAsync(999, "old", "new"));
        }

        // --- ChangePasswordAsync: refresh-token revocation ---

        [Fact]
        public async Task ChangePasswordAsync_SuccessfulChange_RevokesAllRefreshTokensForAccount()
        {
            var context = CreateContextWithSeedData();
            var portMock = new Mock<IRefreshTokenPort>();
            var service = CreateService(context, refreshTokenPort: portMock.Object);

            var result = await service.ChangePasswordAsync(1, "password123", "newPassword456");

            Assert.True(result);
            portMock.Verify(port => port.RevokeAllForAccountAsync(1), Times.Once);
        }

        [Fact]
        public async Task ChangePasswordAsync_WrongCurrentPassword_DoesNotRevokeRefreshTokens()
        {
            var context = CreateContextWithSeedData();
            var portMock = new Mock<IRefreshTokenPort>();
            var service = CreateService(context, refreshTokenPort: portMock.Object);

            var result = await service.ChangePasswordAsync(1, "wrong-password", "newPassword456");

            Assert.False(result);
            portMock.Verify(port => port.RevokeAllForAccountAsync(It.IsAny<int>()), Times.Never);
        }

        // --- CreateAsync ---

        [Fact]
        public async Task CreateAsync_ValidAccount_ReturnsCreatedAccount()
        {
            var context = CreateContext();
            var service = CreateService(context);

            var newAccount = new AccountEntity
            {
                FullName = "Created User",
                Email = "created@test.com",
                Password = "pass123",
                CreateAt = DateTime.Now,
                UpdateAt = DateTime.Now
            };

            var result = await service.CreateAsync(newAccount);

            Assert.NotNull(result);
            Assert.Equal("Created User", result.FullName);
            var stored = await service.GetByIdAsync(result.AccountId);
            Assert.NotNull(stored);
        }

        [Fact]
        public async Task CreateAsync_PlaintextPassword_StoresBcryptHash()
        {
            var context = CreateContext();
            var service = CreateService(context);
            var hasher = new BcryptPasswordHasher();

            var newAccount = new AccountEntity
            {
                FullName = "Created User",
                Email = "created@test.com",
                Password = "pass123",
                CreateAt = DateTime.Now,
                UpdateAt = DateTime.Now
            };

            var result = await service.CreateAsync(newAccount);

            var stored = await service.GetByIdAsync(result.AccountId);
            Assert.NotNull(stored!.Password);
            Assert.StartsWith("$2", stored.Password);
            Assert.True(hasher.Verify("pass123", stored.Password!));
        }

        // --- ForgotPasswordAsync ---

        [Fact]
        public async Task ForgotPasswordAsync_UnknownEmail_ReturnsTrueWithoutStoringTokens()
        {
            var context = CreateContext();
            var service = CreateService(context);

            var result = await service.ForgotPasswordAsync("ghost@test.com");

            Assert.True(result);
        }

        [Fact]
        public async Task ForgotPasswordAsync_KnownEmail_StoresTokenHashAndExpiryAndSendsLink()
        {
            var context = CreateContextWithSeedData();
            string? capturedLink = null;
            var emailSenderMock = new Mock<IIdentityEmailSender>();
            emailSenderMock
                .Setup(sender => sender.SendPasswordResetEmailAsync("test@test.com", "Test User", It.IsAny<string>()))
                .Callback<string, string, string>((_, __, link) => capturedLink = link)
                .ReturnsAsync(true);
            var tokenProvider = new SecurePasswordResetTokenProvider();
            var service = CreateService(
                context,
                identityEmailSender: emailSenderMock.Object,
                passwordResetTokenProvider: tokenProvider);

            var beforeCall = DateTime.UtcNow;
            var result = await service.ForgotPasswordAsync("test@test.com");
            var afterCall = DateTime.UtcNow;

            Assert.True(result);
            Assert.NotNull(capturedLink);

            var stored = await context.Accounts.SingleAsync(a => a.Email == "test@test.com");
            Assert.NotNull(stored.ResetTokenHash);
            Assert.Equal(64, stored.ResetTokenHash!.Length);
            Assert.NotNull(stored.ResetTokenExpiresAt);
            var expectedLowerBound = new DateTime(beforeCall.AddMinutes(15).Ticks, DateTimeKind.Utc);
            var expectedUpperBound = new DateTime(afterCall.AddMinutes(15).Ticks, DateTimeKind.Utc);
            Assert.InRange(stored.ResetTokenExpiresAt!.Value, expectedLowerBound.AddSeconds(-1), expectedUpperBound.AddSeconds(1));

            var tokenFromLink = ExtractTokenFromResetLink(capturedLink!);
            Assert.False(string.IsNullOrWhiteSpace(tokenFromLink));
            Assert.Equal(tokenProvider.Hash(tokenFromLink!), stored.ResetTokenHash);
            Assert.DoesNotContain("password is", capturedLink!, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task ForgotPasswordAsync_BuildsLinkFromConfiguredBaseUrl()
        {
            var context = CreateContextWithSeedData();
            string? capturedLink = null;
            var emailSenderMock = new Mock<IIdentityEmailSender>();
            emailSenderMock
                .Setup(sender => sender.SendPasswordResetEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Callback<string, string, string>((_, __, link) => capturedLink = link)
                .ReturnsAsync(true);
            var configuration = CreateConfiguration("https://frontend.example.com");
            var service = CreateService(context, identityEmailSender: emailSenderMock.Object, configuration: configuration);

            await service.ForgotPasswordAsync("test@test.com");

            Assert.NotNull(capturedLink);
            Assert.StartsWith("https://frontend.example.com/reset-password?token=", capturedLink);
        }

        [Fact]
        public async Task ForgotPasswordAsync_EmailSendFails_ReturnsFalse()
        {
            var context = CreateContextWithSeedData();
            var emailSenderMock = new Mock<IIdentityEmailSender>();
            emailSenderMock
                .Setup(sender => sender.SendPasswordResetEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(false);
            var service = CreateService(context, identityEmailSender: emailSenderMock.Object);

            var result = await service.ForgotPasswordAsync("test@test.com");

            Assert.False(result);
        }

        // --- ResetPasswordAsync ---

        [Fact]
        public async Task ResetPasswordAsync_ValidToken_ResetsPasswordAndClearsTokenFields()
        {
            var tokenProvider = new SecurePasswordResetTokenProvider();
            var hasher = new BcryptPasswordHasher();
            var token = tokenProvider.Generate();

            var context = CreateContextWithSeedData();
            context.Accounts.First().ResetTokenHash = tokenProvider.Hash(token);
            context.Accounts.First().ResetTokenExpiresAt = DateTime.UtcNow.AddMinutes(10);
            context.SaveChanges();
            context.ChangeTracker.Clear();

            var service = CreateService(context, passwordResetTokenProvider: tokenProvider);
            var request = new ResetPasswordRequest { Token = token, NewPassword = "brandNewPass123" };

            var result = await service.ResetPasswordAsync(request);

            Assert.True(result);
            var stored = await context.Accounts.SingleAsync(a => a.Email == "test@test.com");
            Assert.Null(stored.ResetTokenHash);
            Assert.Null(stored.ResetTokenExpiresAt);
            Assert.NotNull(stored.Password);
            Assert.StartsWith("$2", stored.Password);
            Assert.True(hasher.Verify("brandNewPass123", stored.Password!));
        }

        [Fact]
        public async Task ResetPasswordAsync_ExpiredToken_ReturnsFalseAndKeepsOldPassword()
        {
            var tokenProvider = new SecurePasswordResetTokenProvider();
            var token = tokenProvider.Generate();

            var context = CreateContextWithSeedData();
            context.Accounts.First().ResetTokenHash = tokenProvider.Hash(token);
            context.Accounts.First().ResetTokenExpiresAt = DateTime.UtcNow.AddMinutes(-1);
            context.SaveChanges();
            context.ChangeTracker.Clear();

            var service = CreateService(context, passwordResetTokenProvider: tokenProvider);
            var request = new ResetPasswordRequest { Token = token, NewPassword = "brandNewPass123" };

            var result = await service.ResetPasswordAsync(request);

            Assert.False(result);
            var stored = await context.Accounts.SingleAsync(a => a.Email == "test@test.com");
            Assert.Equal("password123", stored.Password);
        }

        [Fact]
        public async Task ResetPasswordAsync_UnknownToken_ReturnsFalse()
        {
            var context = CreateContextWithSeedData();
            var service = CreateService(context, passwordResetTokenProvider: new SecurePasswordResetTokenProvider());
            var request = new ResetPasswordRequest { Token = "totally-unknown-token-value", NewPassword = "brandNewPass123" };

            var result = await service.ResetPasswordAsync(request);

            Assert.False(result);
        }

        [Fact]
        public async Task ResetPasswordAsync_MissingTokenOrPassword_ReturnsFalse()
        {
            var context = CreateContextWithSeedData();
            var service = CreateService(context);

            Assert.False(await service.ResetPasswordAsync(new ResetPasswordRequest { Token = "", NewPassword = "validPass123" }));
            Assert.False(await service.ResetPasswordAsync(new ResetPasswordRequest { Token = "some-token", NewPassword = "" }));
            Assert.False(await service.ResetPasswordAsync(null!));
        }

        // --- ResetPasswordAsync: refresh-token revocation ---

        [Fact]
        public async Task ResetPasswordAsync_ValidToken_RevokesAllRefreshTokensForAccount()
        {
            var tokenProvider = new SecurePasswordResetTokenProvider();
            var token = tokenProvider.Generate();

            var context = CreateContextWithSeedData();
            context.Accounts.First().ResetTokenHash = tokenProvider.Hash(token);
            context.Accounts.First().ResetTokenExpiresAt = DateTime.UtcNow.AddMinutes(10);
            context.SaveChanges();
            context.ChangeTracker.Clear();

            var portMock = new Mock<IRefreshTokenPort>();
            var service = CreateService(context, passwordResetTokenProvider: tokenProvider, refreshTokenPort: portMock.Object);

            var result = await service.ResetPasswordAsync(new ResetPasswordRequest { Token = token, NewPassword = "brandNewPass123" });

            Assert.True(result);
            portMock.Verify(port => port.RevokeAllForAccountAsync(1), Times.Once);
        }

        [Fact]
        public async Task ResetPasswordAsync_InvalidToken_DoesNotRevokeRefreshTokens()
        {
            var context = CreateContextWithSeedData();
            var portMock = new Mock<IRefreshTokenPort>();
            var service = CreateService(context, refreshTokenPort: portMock.Object);

            await service.ResetPasswordAsync(new ResetPasswordRequest { Token = "unknown-token", NewPassword = "brandNewPass123" });

            portMock.Verify(port => port.RevokeAllForAccountAsync(It.IsAny<int>()), Times.Never);
        }

        private static string? ExtractTokenFromResetLink(string link)
        {
            const string marker = "token=";
            var markerIndex = link.IndexOf(marker, StringComparison.Ordinal);
            if (markerIndex < 0)
            {
                return null;
            }

            return Uri.UnescapeDataString(link[(markerIndex + marker.Length)..]);
        }

        // --- GetAccountResponseByEmailAsync ---

        [Fact]
        public async Task GetAccountResponseByEmailAsync_ExistingEmail_ReturnsResponse()
        {
            var context = CreateContextWithSeedData();
            var service = CreateService(context);

            var result = await service.GetAccountResponseByEmailAsync("test@test.com");

            Assert.NotNull(result);
            Assert.IsType<AccountResponse>(result);
            Assert.Equal("Test User", result.FullName);
            Assert.Equal("test@test.com", result.Email);
        }

        [Fact]
        public async Task GetAccountResponseByEmailAsync_WithElementId_ReturnsElementName()
        {
            var context = CreateContext();
            context.Elements.Add(new Element
            {
                ElementId = 3,
                ElementName = "Hoa",
                Description = "Fire",
                LuckyNumber = "9"
            });
            context.Accounts.Add(new AccountEntity
            {
                AccountId = 1,
                FullName = "Element User",
                Email = "element@test.com",
                Password = "password123",
                ElementId = 3,
                CreateAt = DateTime.Now,
                UpdateAt = DateTime.Now,
                RoleId = 2
            });
            context.SaveChanges();

            var service = CreateService(context);

            var result = await service.GetAccountResponseByEmailAsync("element@test.com");

            Assert.NotNull(result);
            Assert.Equal("Hoa", result.ElementName);
        }

        [Fact]
        public async Task GetAccountResponseByEmailAsync_NonExistentEmail_ReturnsNull()
        {
            var context = CreateContext();
            var service = CreateService(context);

            var result = await service.GetAccountResponseByEmailAsync("nonexistent@test.com");

            Assert.Null(result);
        }
    }
}
