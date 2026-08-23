using KoiFengShuiSystem.Modules.Identity.Domain.Entities;
using KoiFengShuiSystem.Modules.Identity.Infrastructure.Persistence;
using KoiFengShuiSystem.Shared.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace UnitTests.Identity;

public class EfIdentityWriteStoreTests
{
    private static DbContextOptions<KoiFengShuiContext> CreateInMemoryOptions(string databaseName)
    {
        return new DbContextOptionsBuilder<KoiFengShuiContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;
    }

    [Fact]
    public async Task CreateAccountAsync_DoesNotPersistUntilSaveChangesAsync()
    {
        await using var writeContext = new KoiFengShuiContext(CreateInMemoryOptions($"EfIdentityWriteStoreTests_{Guid.NewGuid()}"));
        var store = new EfIdentityWriteStore(writeContext);
        var account = new Account
        {
            AccountId = 1,
            Email = "pending@test.com",
            Password = "password",
            FullName = "Pending User",
            RoleId = 2,
            CreateAt = DateTime.UtcNow,
            UpdateAt = DateTime.UtcNow
        };

        await store.CreateAccountAsync(account);

        Assert.Equal(EntityState.Added, writeContext.Entry(account).State);

        await store.SaveChangesAsync();

        Assert.Equal(EntityState.Unchanged, writeContext.Entry(account).State);
    }
}
