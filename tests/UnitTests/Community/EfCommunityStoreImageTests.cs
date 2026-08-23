using KoiFengShuiSystem.Modules.Community.Infrastructure.Persistence;
using KoiFengShuiSystem.Shared.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace UnitTests.Community
{
    /// <summary>
    /// Ports the legacy ImageService.SaveImagesAsync persistence contract onto
    /// the module-owned store: an uploaded url becomes exactly one Image row.
    /// </summary>
    public class EfCommunityStoreImageTests : IDisposable
    {
        private readonly KoiFengShuiContext _context;
        private readonly EfCommunityStore _store;

        public EfCommunityStoreImageTests()
        {
            var options = new DbContextOptionsBuilder<KoiFengShuiContext>()
                .UseInMemoryDatabase(databaseName: $"CommunityStoreImages_{Guid.NewGuid()}")
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;
            _context = new KoiFengShuiContext(options);
            _store = new EfCommunityStore(_context);
        }

        public void Dispose() => _context.Dispose();

        [Fact]
        public async Task AddImageAsync_PersistsRowWithUrl_AndReturnsGeneratedId()
        {
            var result = await _store.AddImageAsync("https://res.cloudinary.com/demo/koi.png");

            Assert.True(result > 0);
            var stored = await _context.Images.SingleAsync();
            Assert.Equal("https://res.cloudinary.com/demo/koi.png", stored.ImageUrl);
            Assert.Equal(stored.ImageId, result);
        }

        [Fact]
        public async Task AddImageAsync_CalledTwice_CreatesTwoIndependentRows_WithDistinctIds()
        {
            var firstId = await _store.AddImageAsync("https://res.cloudinary.com/demo/a.jpg");
            var secondId = await _store.AddImageAsync("https://res.cloudinary.com/demo/b.jpg");

            Assert.Equal(2, await _context.Images.CountAsync());
            Assert.NotEqual(firstId, secondId);
        }
    }
}
