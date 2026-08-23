using KoiFengShuiSystem.BusinessLogic.Services;
using KoiFengShuiSystem.BusinessLogic.Services.Implement;
using KoiFengShuiSystem.BusinessLogic.Services.Interface;
using KoiFengShuiSystem.DataAccess.Models;
using KoiFengShuiSystem.Modules.FengShui.Domain.Entities;
using KoiFengShuiSystem.Modules.Identity.Domain.Entities;
using KoiFengShuiSystem.Shared.Infrastructure.Persistence;
using KoiFengShuiSystem.Shared.Models.Request;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace UnitTests.Admin
{
    public class AdminPostServiceSaveBatchingTests : IDisposable
    {
        private readonly CountingContext _context;
        private readonly AdminPostService _service;

        public AdminPostServiceSaveBatchingTests()
        {
            var options = new DbContextOptionsBuilder<KoiFengShuiContext>()
                .UseInMemoryDatabase(databaseName: $"AdminPostBatching_{Guid.NewGuid()}")
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;
            _context = new CountingContext(options);

            _context.PostCategories.Add(new PostCategory { Id = 1 });
            _context.Accounts.Add(new Account
            {
                AccountId = 1,
                FullName = "Admin",
                Email = "admin@test.com",
                RoleId = 1,
                CreateAt = DateTime.Now,
                UpdateAt = DateTime.Now
            });
            _context.Posts.Add(new Post
            {
                PostId = 7,
                PostCategoryId = 1,
                Name = "Existing",
                Description = "Existing post",
                Status = "Published",
                AccountId = 1,
                CreateAt = DateTime.UtcNow,
                UpdateAt = DateTime.UtcNow
            });
            _context.SaveChanges();
            _context.ResetSaveCount();
        }

        public void Dispose() => _context.Dispose();

        [Fact]
        public async Task CreatePostWithImagesAsync_WithTwoImages_SavesEverythingInOneBatch()
        {
            var service = new AdminPostService(_context, ImageServiceStub());
            var request = new AdminPostRequest
            {
                Id = 1,
                Name = "New post",
                Description = "Body",
                AccountId = 1,
                Status = "Published"
            };

            var response = await service.CreatePostWithImagesAsync(request, new List<string> { "img-a", "img-b" });

            Assert.Equal(1, _context.SaveChangesAsyncCallCount);
            Assert.NotNull(response);
            Assert.True(response.PostId > 0, "post must be persisted with a generated key");
            Assert.Equal("New post", await _context.Posts.Where(p => p.PostId == response.PostId).Select(p => p.Name).SingleAsync());
            Assert.Equal(2, await _context.PostImages.CountAsync(pi => pi.PostId == response.PostId));
        }

        [Fact]
        public async Task UpdateAdminPostAsync_AddingTwoNewImages_SavesOnce()
        {
            var service = new AdminPostService(_context, ImageServiceStub());

            await service.UpdateAdminPostAsync(7, new AdminPostRequest
            {
                Id = 1,
                Name = "Updated",
                Description = "Updated body",
                Status = "Published"
            }, new List<string> { "new-a", "new-b" });

            Assert.Equal(1, _context.SaveChangesAsyncCallCount);
        }

        [Fact]
        public async Task CreatePostWithImagesAsync_WithNoImages_PersistsThePost()
        {
            var service = new AdminPostService(_context, ImageServiceStub());
            var request = new AdminPostRequest
            {
                Id = 1,
                Name = "No images",
                Description = "Body",
                AccountId = 1,
                Status = "Published"
            };

            var response = await service.CreatePostWithImagesAsync(request, new List<string>());

            Assert.Equal(1, _context.SaveChangesAsyncCallCount);
            Assert.NotNull(response);
            Assert.True(response.PostId > 0, "zero-image creation must still persist the post with a generated key");

            var saved = await _context.Posts.SingleAsync(p => p.Name == "No images");
            Assert.Equal(response.PostId, saved.PostId);
            Assert.Equal("Body", saved.Description);
            Assert.Equal("Published", saved.Status);
        }

        private static IImageService ImageServiceStub()
        {
            var stub = new Moq.Mock<IImageService>();
            return stub.Object;
        }

        private class CountingContext : KoiFengShuiContext
        {
            public CountingContext(DbContextOptions<KoiFengShuiContext> options) : base(options)
            {
            }

            public int SaveChangesAsyncCallCount { get; private set; }

            public void ResetSaveCount() => SaveChangesAsyncCallCount = 0;

            // Only the core (bool, CancellationToken) overload is overridden:
            // the parameterless overload delegates to it virtually, so counting
            // both would double-count a single logical save.
            public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
            {
                SaveChangesAsyncCallCount++;
                return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
            }
        }
    }
}
