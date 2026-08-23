using KoiFengShuiSystem.Modules.Identity.Domain.Entities;
using KoiFengShuiSystem.Shared.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace UnitTests.Identity
{
    /// <summary>
    /// Guards data-model integrity at the EF metadata level: Account.Email must
    /// carry a database-level unique index so duplicate registration races fail
    /// in the store instead of relying on check-then-insert app code.
    /// </summary>
    public class AccountModelIntegrityTests
    {
        private static IModel BuildModel()
        {
            var options = new DbContextOptionsBuilder<KoiFengShuiContext>()
                .UseInMemoryDatabase(databaseName: $"AccountModel_{Guid.NewGuid()}")
                .Options;

            using var context = new KoiFengShuiContext(options);
            return context.Model;
        }

        [Fact]
        public void Account_Email_HasUniqueDatabaseIndex()
        {
            var model = BuildModel();

            var accountEntity = model.FindEntityType(typeof(Account));
            Assert.NotNull(accountEntity);

            var emailIndex = Assert.Single(
                accountEntity.GetIndexes(),
                index => index.Properties.Count == 1
                    && index.Properties[0].Name == nameof(Account.Email));

            Assert.True(emailIndex.IsUnique);
        }
    }
}
