using System.Security.Cryptography;
using System.Text;
using KoiFengShuiSystem.Modules.Identity.Application.Abstractions;
using KoiFengShuiSystem.Modules.Identity.Domain.Entities;
using KoiFengShuiSystem.Modules.Identity.Infrastructure.Persistence;
using KoiFengShuiSystem.Shared.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace UnitTests.Identity.Security;

public class EfRefreshTokenPortTests
{
    private static DbContextOptions<KoiFengShuiContext> CreateOptions()
        => new DbContextOptionsBuilder<KoiFengShuiContext>()
            .UseInMemoryDatabase($"RefreshTokenTestDb_{Guid.NewGuid()}")
            .Options;

    private static IConfiguration CreateConfiguration(int refreshTokenDays = 30)
        => new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AppSettings:RefreshTokenDays"] = refreshTokenDays.ToString()
            })
            .Build();

    private static EfRefreshTokenPort CreatePort(KoiFengShuiContext context, IConfiguration? configuration = null, Mock<ILogger<EfRefreshTokenPort>>? logger = null)
        => new(context, configuration ?? CreateConfiguration(), (logger?.Object) ?? Mock.Of<ILogger<EfRefreshTokenPort>>());

    private static async Task<KoiFengShuiContext> CreateSeededContextAsync(params int[] accountIds)
    {
        var context = new KoiFengShuiContext(CreateOptions());
        foreach (var accountId in accountIds)
        {
            context.Accounts.Add(new Account
            {
                AccountId = accountId,
                FullName = $"User {accountId}",
                Email = $"user{accountId}@test.com",
                Password = "$2$hashed",
                CreateAt = DateTime.Now,
                UpdateAt = DateTime.Now,
                RoleId = 2
            });
        }

        await context.SaveChangesAsync();
        return context;
    }

    private static string HashOf(string rawToken)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken))).ToLowerInvariant();

    // --- CreateForAccountAsync ---

    [Fact]
    public async Task CreateForAccountAsync_ReturnsNonEmptyBase64UrlRawToken()
    {
        var context = await CreateSeededContextAsync(1);
        var port = CreatePort(context);

        var rawToken = await port.CreateForAccountAsync(1);

        Assert.False(string.IsNullOrWhiteSpace(rawToken));
        Assert.Matches("^[A-Za-z0-9_-]+$", rawToken);
    }

    [Fact]
    public async Task CreateForAccountAsync_PersistsHashNotRawToken()
    {
        var context = await CreateSeededContextAsync(1);
        var port = CreatePort(context);

        var rawToken = await port.CreateForAccountAsync(1);
        context.ChangeTracker.Clear();

        var stored = await context.RefreshTokens.SingleAsync();
        Assert.Equal(HashOf(rawToken), stored.TokenHash);
        Assert.NotEqual(rawToken, stored.TokenHash);
        Assert.Equal(64, stored.TokenHash.Length);
        Assert.Matches("^[a-f0-9]{64}$", stored.TokenHash);
    }

    [Fact]
    public async Task CreateForAccountAsync_SetsAccountIdAndExpiryFromConfiguredLifetime()
    {
        var context = await CreateSeededContextAsync(1);
        var port = CreatePort(context, CreateConfiguration(refreshTokenDays: 30));

        var beforeCall = DateTime.UtcNow;
        await port.CreateForAccountAsync(1);
        context.ChangeTracker.Clear();

        var stored = await context.RefreshTokens.SingleAsync();
        Assert.Equal(1, stored.AccountId);
        Assert.Null(stored.RevokedAt);
        Assert.Null(stored.ReplacedByTokenHash);
        Assert.InRange(stored.ExpiresAt,
            beforeCall.AddDays(30).AddSeconds(-5),
            DateTime.UtcNow.AddDays(30).AddSeconds(5));
    }

    [Fact]
    public async Task CreateForAccountAsync_RepeatedCalls_ProduceUniqueTokenHashes()
    {
        var context = await CreateSeededContextAsync(1);
        var port = CreatePort(context);

        var first = await port.CreateForAccountAsync(1);
        var second = await port.CreateForAccountAsync(1);
        context.ChangeTracker.Clear();

        Assert.NotEqual(first, second);
        Assert.Equal(2, await context.RefreshTokens.CountAsync());
    }

    // --- RotateAsync: happy path ---

    [Fact]
    public async Task RotateAsync_ValidToken_ReturnsSuccessWithNewDifferentRawTokenAndAccountId()
    {
        var context = await CreateSeededContextAsync(1);
        var port = CreatePort(context);
        var originalRaw = await port.CreateForAccountAsync(1);
        context.ChangeTracker.Clear();

        var result = await port.RotateAsync(originalRaw);

        Assert.True(result.Success);
        Assert.Equal(1, result.AccountId);
        Assert.False(string.IsNullOrWhiteSpace(result.NewRawToken));
        Assert.NotEqual(originalRaw, result.NewRawToken);
    }

    [Fact]
    public async Task RotateAsync_ValidToken_MarksOldRowRevokedLinkedToNewRow()
    {
        var context = await CreateSeededContextAsync(1);
        var port = CreatePort(context);
        var originalRaw = await port.CreateForAccountAsync(1);
        context.ChangeTracker.Clear();

        var result = await port.RotateAsync(originalRaw);
        context.ChangeTracker.Clear();

        var oldRow = await context.RefreshTokens.SingleAsync(t => t.TokenHash == HashOf(originalRaw));
        Assert.NotNull(oldRow.RevokedAt);
        Assert.InRange(oldRow.RevokedAt!.Value, DateTime.UtcNow.AddSeconds(-10), DateTime.UtcNow.AddSeconds(10));
        Assert.Equal(HashOf(result.NewRawToken!), oldRow.ReplacedByTokenHash);

        var newRow = await context.RefreshTokens.SingleAsync(t => t.TokenHash == HashOf(result.NewRawToken!));
        Assert.Equal(1, newRow.AccountId);
        Assert.Null(newRow.RevokedAt);
    }

    // --- RotateAsync: reuse detection (breach) ---

    [Fact]
    public async Task RotateAsync_AlreadyRevokedToken_TriggersFamilyRevocationAndFails()
    {
        var context = await CreateSeededContextAsync(1);
        var port = CreatePort(context);
        var originalRaw = await port.CreateForAccountAsync(1);
        var rotated = await port.RotateAsync(originalRaw);
        var extraSibling = await port.CreateForAccountAsync(1);
        context.ChangeTracker.Clear();

        var replayResult = await port.RotateAsync(originalRaw);
        context.ChangeTracker.Clear();

        Assert.False(replayResult.Success);
        Assert.Equal(RotateResult.ReuseDetectedReason, replayResult.FailureReason);

        var remainingActive = await context.RefreshTokens
            .Where(t => t.AccountId == 1 && t.RevokedAt == null)
            .CountAsync();
        Assert.Equal(0, remainingActive);

        var siblingRow = await context.RefreshTokens.SingleAsync(t => t.TokenHash == HashOf(extraSibling));
        Assert.NotNull(siblingRow.RevokedAt);
        var successorRow = await context.RefreshTokens.SingleAsync(t => t.TokenHash == HashOf(rotated.NewRawToken!));
        Assert.NotNull(successorRow.RevokedAt);
    }

    [Fact]
    public async Task RotateAsync_ClaimLostToConcurrentRotation_FailsRevokesFamilyAndPersistsNoOrphanSuccessor()
    {
        // Shared InMemory store across two contexts simulates two concurrent requests:
        // the port's context keeps a stale tracked copy of the token while another
        // request's rotation has already consumed it at store level.
        var options = new DbContextOptionsBuilder<KoiFengShuiContext>()
            .UseInMemoryDatabase($"RefreshTokenRaceDb_{Guid.NewGuid()}")
            .Options;

        var seedContext = new KoiFengShuiContext(options);
        seedContext.Accounts.Add(new Account
        {
            AccountId = 1,
            FullName = "User 1",
            Email = "user1@test.com",
            Password = "$2$hashed",
            CreateAt = DateTime.Now,
            UpdateAt = DateTime.Now,
            RoleId = 2
        });
        await seedContext.SaveChangesAsync();

        var context = new KoiFengShuiContext(options);
        var port = CreatePort(context);
        var rawToken = await port.CreateForAccountAsync(1);

        var concurrentRequestContext = new KoiFengShuiContext(options);
        var consumedRow = await concurrentRequestContext.RefreshTokens
            .SingleAsync(rt => rt.TokenHash == HashOf(rawToken));
        consumedRow.RevokedAt = DateTime.UtcNow;
        await concurrentRequestContext.SaveChangesAsync();

        var replayResult = await port.RotateAsync(rawToken);

        Assert.False(replayResult.Success);
        Assert.Equal(RotateResult.ReuseDetectedReason, replayResult.FailureReason);

        context.ChangeTracker.Clear();
        var rows = await context.RefreshTokens.ToListAsync();
        Assert.Single(rows); // no orphaned successor row may survive a lost claim
        Assert.All(rows, row => Assert.NotNull(row.RevokedAt));
    }

    // --- RotateAsync: rejection paths ---

    [Fact]
    public async Task RotateAsync_UnknownToken_FailsWithoutSideEffects()
    {
        var context = await CreateSeededContextAsync(1);
        var port = CreatePort(context);
        await port.CreateForAccountAsync(1);
        context.ChangeTracker.Clear();

        var result = await port.RotateAsync("totally-unknown-raw-token");

        Assert.False(result.Success);
        Assert.Null(result.AccountId);
        Assert.NotEmpty(result.FailureReason);
        Assert.Equal(1, await context.RefreshTokens.CountAsync());
    }

    [Fact]
    public async Task RotateAsync_MissingToken_Fails()
    {
        var context = await CreateSeededContextAsync(1);
        var port = CreatePort(context);

        var result = await port.RotateAsync(string.Empty);

        Assert.False(result.Success);
    }

    [Fact]
    public async Task RotateAsync_ExpiredToken_FailsAndClearsRow()
    {
        var context = await CreateSeededContextAsync(1);
        var port = CreatePort(context);
        var rawToken = await port.CreateForAccountAsync(1);

        var row = await context.RefreshTokens.SingleAsync();
        row.ExpiresAt = DateTime.UtcNow.AddMinutes(-1);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var result = await port.RotateAsync(rawToken);

        Assert.False(result.Success);
        Assert.Empty(await context.RefreshTokens.ToListAsync());
    }

    // --- Security event logging ---

    [Fact]
    public async Task RotateAsync_ReuseDetected_LogsErrorWithAccountIdAndRevokedCount()
    {
        var context = await CreateSeededContextAsync(1);
        var logger = new Mock<ILogger<EfRefreshTokenPort>>();
        var port = CreatePort(context, logger: logger);
        var originalRaw = await port.CreateForAccountAsync(1);
        await port.RotateAsync(originalRaw);          // successor token, active
        await port.CreateForAccountAsync(1);          // sibling token, active
        context.ChangeTracker.Clear();

        await port.RotateAsync(originalRaw);

        logger.Verify(
            l => l.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) =>
                    state.ToString()!.Contains("reuse detected", StringComparison.OrdinalIgnoreCase) &&
                    state.ToString()!.Contains("revoking all 2 active tokens")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task RotateAsync_ExpiredToken_LogsWarning()
    {
        var context = await CreateSeededContextAsync(1);
        var logger = new Mock<ILogger<EfRefreshTokenPort>>();
        var port = CreatePort(context, logger: logger);
        var rawToken = await port.CreateForAccountAsync(1);

        var row = await context.RefreshTokens.SingleAsync();
        row.ExpiresAt = DateTime.UtcNow.AddMinutes(-1);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        await port.RotateAsync(rawToken);

        logger.Verify(
            l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) => state.ToString()!.Contains("expired")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    // --- RevokeAllForAccountAsync ---

    [Fact]
    public async Task RevokeAllForAccountAsync_RevokesOnlyTargetAccountsTokens()
    {
        var context = await CreateSeededContextAsync(1, 2);
        var port = CreatePort(context);
        await port.CreateForAccountAsync(1);
        await port.CreateForAccountAsync(1);
        var otherAccountToken = await port.CreateForAccountAsync(2);
        context.ChangeTracker.Clear();

        await port.RevokeAllForAccountAsync(1);
        context.ChangeTracker.Clear();

        var accountOneRows = await context.RefreshTokens.Where(t => t.AccountId == 1).ToListAsync();
        Assert.All(accountOneRows, row => Assert.NotNull(row.RevokedAt));

        var otherRow = await context.RefreshTokens.SingleAsync(t => t.TokenHash == HashOf(otherAccountToken));
        Assert.Null(otherRow.RevokedAt);
    }
}
