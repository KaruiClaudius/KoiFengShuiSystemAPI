using KoiFengShuiSystem.DataAccess.Models;
using KoiFengShuiSystem.Modules.Community.Application.Abstractions;
using KoiFengShuiSystem.Modules.Community.Infrastructure.Persistence;
using KoiFengShuiSystem.Modules.Identity.Domain.Entities;
using KoiFengShuiSystem.Shared.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace UnitTests.Community
{
    /// <summary>
    /// Ports the legacy AdminPostServiceSaveBatchingTests onto the module-owned
    /// EF store: image/post persistence must stay batched into a single save and
    /// zero-image admin creation must still persist the post.
    /// </summary>
    public class EfCommunityStorePostTests : IDisposable
    {
        private readonly CountingContext _context;
        private readonly EfCommunityStore _store;

        public EfCommunityStorePostTests()
        {
            var options = new DbContextOptionsBuilder<KoiFengShuiContext>()
                .UseInMemoryDatabase(databaseName: $"CommunityStorePosts_{Guid.NewGuid()}")
                .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;
            _context = new CountingContext(options);
            _store = new EfCommunityStore(_context);

            _context.PostCategories.Add(new PostCategory { Id = 1, PostType = "Blog" });
            _context.Elements.Add(new KoiFengShuiSystem.Modules.FengShui.Domain.Entities.Element { ElementId = 1, ElementName = "Metal" });
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
        public async Task AddAdminPostWithImagesAsync_WithTwoImages_SavesEverythingInOneBatch()
        {
            var post = NewAdminPost("New post");
            AttachImages(post, "img-a", "img-b");

            await _store.AddAdminPostWithImagesAsync(post);

            Assert.Equal(1, _context.SaveChangesAsyncCallCount);
            Assert.True(post.PostId > 0, "post must be persisted with a generated key");
            Assert.Equal(2, await _context.PostImages.CountAsync(pi => pi.PostId == post.PostId));
            Assert.Equal("New post", await _context.Posts.Where(p => p.PostId == post.PostId).Select(p => p.Name).SingleAsync());
        }

        [Fact]
        public async Task AddAdminPostWithImagesAsync_WithNoImages_PersistsThePost()
        {
            var post = NewAdminPost("No images");

            await _store.AddAdminPostWithImagesAsync(post);
            var response = await _store.GetAdminPostByIdWithImagesAsync(post.PostId);

            Assert.Equal(1, _context.SaveChangesAsyncCallCount);
            Assert.NotNull(response);
            Assert.True(response!.PostId > 0, "zero-image creation must still persist the post with a generated key");

            var saved = await _context.Posts.SingleAsync(p => p.Name == "No images");
            Assert.Equal(response.PostId, saved.PostId);
            Assert.Equal("Body", saved.Description);
            Assert.Equal("Published", saved.Status);
        }

        [Fact]
        public async Task AddPostAsync_PersistsMemberPostWithImageLinksInOneSave()
        {
            var image = new Image { ImageId = 5, ImageUrl = "https://cdn.example/i5.png" };
            _context.Images.Add(image);
            _context.SaveChanges();
            _context.ResetSaveCount();

            var post = new Post
            {
                Name = "Koi care",
                Description = "Feed koi twice daily",
                PostCategoryId = 1,
                AccountId = 42,
                Status = "Pending",
                CreateAt = DateTime.UtcNow,
                UpdateAt = DateTime.UtcNow
            };
            post.PostImages.Add(new PostImage { ImageId = 5, ImageDescription = "Member upload" });

            await _store.AddPostAsync(post);

            Assert.Equal(1, _context.SaveChangesAsyncCallCount);
            var link = await _context.PostImages.SingleAsync(pi => pi.PostId == post.PostId);
            Assert.Equal(5, link.ImageId);
        }

        [Fact]
        public async Task UpdateAdminPostAsync_AddingTwoNewImages_SavesOnce()
        {
            var update = new AdminPostUpdate(7, "Updated", "Updated body", "Published", new List<string> { "new-a", "new-b" });

            var stored = await _store.UpdateAdminPostAsync(update);

            Assert.Equal(1, _context.SaveChangesAsyncCallCount);
            Assert.NotNull(stored);
            Assert.Equal(new[] { "new-a", "new-b" }.OrderBy(u => u), stored!.PostImages.Select(pi => pi.Image.ImageUrl).OrderBy(u => u));
        }

        [Fact]
        public async Task UpdateAdminPostAsync_RemovesDroppedImagesAndKeepsExistingOnes()
        {
            SeedImageLinks(7, ("keep-me", "kept"), ("drop-me", "dropped"));

            var update = new AdminPostUpdate(7, "Updated", "Updated body", "Published", new List<string> { "keep-me", "added" });

            var stored = await _store.UpdateAdminPostAsync(update);

            Assert.NotNull(stored);
            Assert.Equal(new[] { "keep-me", "added" }.OrderBy(u => u), stored!.PostImages.Select(pi => pi.Image.ImageUrl).OrderBy(u => u));
            Assert.False(await _context.Images.AnyAsync(i => i.ImageUrl == "drop-me"), "dropped url's Image row must be deleted with its link");
            Assert.True(await _context.Images.AnyAsync(i => i.ImageUrl == "keep-me"), "kept url must keep its Image row untouched");
            Assert.Equal("Updated", stored.Name);
            Assert.Equal("Published", stored.Status);
        }

        [Fact]
        public async Task UpdateAdminPostAsync_NonExistentId_ReturnsNullWithoutSaving()
        {
            var savedBefore = _context.SaveChangesAsyncCallCount;

            var stored = await _store.UpdateAdminPostAsync(new AdminPostUpdate(404, "x", "y", "z", new List<string>()));

            Assert.Null(stored);
            Assert.Equal(savedBefore, _context.SaveChangesAsyncCallCount);
        }

        [Fact]
        public async Task DeletePostAsync_MissingId_ReturnsFalse_ExistingId_ReturnsTrueAndDeletes()
        {
            Assert.False(await _store.DeletePostAsync(999));

            Assert.True(await _store.DeletePostAsync(7));
            Assert.False(await _context.Posts.AnyAsync(p => p.PostId == 7));
        }

        [Fact]
        public async Task GetElementNamesAsync_ReturnsJoinTable()
        {
            var names = await _store.GetElementNamesAsync();

            Assert.Equal("Metal", names[1]);
        }

        [Fact]
        public async Task GetPostsByPostTypeAsync_PaginatesWithinCategory()
        {
            for (var i = 0; i < 15; i++)
            {
                _context.Posts.Add(new Post
                {
                    PostCategoryId = 2,
                    Name = $"Bulk {i}",
                    Description = "d",
                    Status = "Pending",
                    AccountId = 1,
                    CreateAt = DateTime.UtcNow,
                    UpdateAt = DateTime.UtcNow
                });
            }
            _context.SaveChanges();

            var pageOne = await _store.GetPostsByPostTypeAsync(2, page: 1, pageSize: 10);
            var pageTwo = await _store.GetPostsByPostTypeAsync(2, page: 2, pageSize: 10);

            Assert.Equal(10, pageOne.Count);
            Assert.Equal(5, pageTwo.Count);
            Assert.Empty(pageOne.Select(p => p.PostId).Intersect(pageTwo.Select(p => p.PostId)));
        }

        [Fact]
        public async Task GetPostByIdAsync_ReturnsRawEntityForDetailsEndpoint()
        {
            var found = await _store.GetPostByIdAsync(7);

            Assert.NotNull(found);
            Assert.Equal("Existing", found!.Name);
            Assert.Null(await _store.GetPostByIdAsync(404));
        }

        private Post NewAdminPost(string name) => new()
        {
            PostCategoryId = 1,
            Name = name,
            Description = "Body",
            AccountId = 1,
            Status = "Published",
            CreateAt = DateTime.UtcNow,
            UpdateAt = DateTime.UtcNow
        };

        private static void AttachImages(Post post, params string[] urls)
        {
            foreach (var url in urls)
            {
                post.PostImages.Add(new PostImage
                {
                    Post = post,
                    Image = new Image { ImageUrl = url },
                    ImageDescription = "Default description"
                });
            }
        }

        private void SeedImageLinks(int postId, params (string url, string description)[] images)
        {
            foreach (var (url, description) in images)
            {
                _context.PostImages.Add(new PostImage
                {
                    PostId = postId,
                    Image = new Image { ImageUrl = url },
                    ImageDescription = description
                });
            }
            _context.SaveChanges();
            _context.ResetSaveCount();
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
