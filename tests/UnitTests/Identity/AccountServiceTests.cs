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

            var thuyName = KoiFengShuiSystem.Common.FengShui.CungPhiCalculator.Calculate(1990, true).Menh;

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
            IIdentityElementLookup? elementLookup = null)
        {
            var ctx = context ?? CreateContext();
            var jwt = jwtTokenService ?? Mock.Of<IJwtTokenService>(j => j.GenerateJwtToken(It.IsAny<AccountEntity>()) == "test-token");
            var email = identityEmailSender ?? new LegacyIdentityEmailSender(CreateEmailService());
            var lookup = elementLookup ?? new EfIdentityElementLookup(ctx);
            var logger = Mock.Of<ILogger<IdentityAccountService>>();

            return new IdentityAccountService(
                new EfIdentityReadStore(ctx),
                new EfIdentityWriteStore(ctx),
                jwt,
                email,
                logger,
                lookup);
        }

        // --- Constructor ---

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(5)]
        public void Constructor_NullDependency_ThrowsArgumentNullException(int nullDependencyIndex)
        {
            var ctx = CreateContext();

            IIdentityReadStore readStore = new EfIdentityReadStore(ctx);
            IIdentityWriteStore writeStore = new EfIdentityWriteStore(ctx);
            IJwtTokenService jwtTokenService = CreateJwtTokenService();
            IIdentityEmailSender identityEmailSender = new LegacyIdentityEmailSender(CreateEmailService());
            ILogger<IdentityAccountService> logger = Mock.Of<ILogger<IdentityAccountService>>();
            IIdentityElementLookup elementLookup = new EfIdentityElementLookup(ctx);

            var ex = Assert.Throws<ArgumentNullException>(() => new IdentityAccountService(
                nullDependencyIndex == 0 ? null! : readStore,
                nullDependencyIndex == 1 ? null! : writeStore,
                nullDependencyIndex == 2 ? null! : jwtTokenService,
                nullDependencyIndex == 3 ? null! : identityEmailSender,
                nullDependencyIndex == 4 ? null! : logger,
                nullDependencyIndex == 5 ? null! : elementLookup));

            Assert.NotNull(ex.ParamName);
        }

        [Fact]
        public void Constructor_WithValidDependencies_Succeeds()
        {
            var ctx = CreateContext();
            var email = CreateEmailService();
            var logger = Mock.Of<ILogger<IdentityAccountService>>();

            var service = new IdentityAccountService(
                new EfIdentityReadStore(ctx),
                new EfIdentityWriteStore(ctx),
                CreateJwtTokenService(),
                new LegacyIdentityEmailSender(email),
                logger,
                new EfIdentityElementLookup(ctx));

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

        // --- RegisterAsync ---

        [Fact]
        public async Task RegisterAsync_ValidRequest_CreatesAccount()
        {
            var context = CreateContext();
            var thuyName = KoiFengShuiSystem.Common.FengShui.CungPhiCalculator.Calculate(1990, true).Menh;
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
        public async Task UpdateUserPasswordAsync_ExistingAccount_UpdatesPassword()
        {
            var context = CreateContextWithSeedData();
            context.ChangeTracker.Clear();
            var service = CreateService(context);

            var AccountEntity = new AccountEntity { AccountId = 1, Email = "test@test.com" };
            await service.UpdateUserPasswordAsync(AccountEntity, "newPassword123");

            var updated = await service.GetByIdAsync(1);
            Assert.Equal("newPassword123", updated!.Password);
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
            var thuyName = KoiFengShuiSystem.Common.FengShui.CungPhiCalculator.Calculate(1990, true).Menh;
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

            var result = await service.ChangePasswordAsync(1, "password123", "newPassword456");

            Assert.True(result);
            var updated = await service.GetByIdAsync(1);
            Assert.Equal("newPassword456", updated!.Password);
        }

        [Fact]
        public async Task ChangePasswordAsync_WrongCurrentPassword_ReturnsFalse()
        {
            var context = CreateContextWithSeedData();
            var service = CreateService(context);

            var result = await service.ChangePasswordAsync(1, "wrongCurrentPass", "newPassword456");

            Assert.False(result);
        }

        [Fact]
        public async Task ChangePasswordAsync_NonExistentId_ThrowsKeyNotFoundException()
        {
            var context = CreateContext();
            var service = CreateService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                service.ChangePasswordAsync(999, "old", "new"));
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

        [Fact]
        public async Task UpdateUserWalletAfterPosted_WhenPersistenceFails_RethrowsException()
        {
            var account = new AccountEntity
            {
                AccountId = 1,
                Email = "wallet@test.com",
                Wallet = 100m
            };

            var writeStoreMock = new Mock<IIdentityWriteStore>();
            writeStoreMock
                .Setup(store => store.UpdateAccountAsync(account))
                .ThrowsAsync(new InvalidOperationException("save failed"));

            var service = new IdentityAccountService(
                Mock.Of<IIdentityReadStore>(),
                writeStoreMock.Object,
                Mock.Of<IJwtTokenService>(),
                Mock.Of<IIdentityEmailSender>(),
                Mock.Of<ILogger<IdentityAccountService>>(),
                Mock.Of<IIdentityElementLookup>());

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.UpdateUserWalletAfterPosted(account, 10m));
        }
    }
}
