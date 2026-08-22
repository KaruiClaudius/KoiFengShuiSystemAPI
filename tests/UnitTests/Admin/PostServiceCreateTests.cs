using KoiFengShuiSystem.BusinessLogic.Services.Implement;
using KoiFengShuiSystem.Common;
using KoiFengShuiSystem.DataAccess.Models;
using KoiFengShuiSystem.DataAccess.Repositories.Implement;
using KoiFengShuiSystem.Modules.FengShui.Domain.Entities;
using KoiFengShuiSystem.Shared.Infrastructure.Persistence;
using KoiFengShuiSystem.Shared.Models.Request;
using Microsoft.EntityFrameworkCore;

namespace UnitTests.Admin
{
    public class PostServiceCreateTests : IDisposable
    {
        private readonly KoiFengShuiContext _context;
        private readonly PostService _service;

        public PostServiceCreateTests()
        {
            var options = new DbContextOptionsBuilder<KoiFengShuiContext>()
                .UseInMemoryDatabase(databaseName: $"PostServiceCreate_{Guid.NewGuid()}")
                .Options;
            _context = new KoiFengShuiContext(options);
            _context.PostCategories.Add(new PostCategory { Id = 1, PostType = "Blog" });
            _context.Images.Add(new Image { ImageId = 5, ImageUrl = "https://cdn.example/i5.png" });
            _context.SaveChanges();
            _service = new PostService(new UnitOfWorkRepository(_context), _context,
                Microsoft.Extensions.Logging.Abstractions.NullLogger<PostService>.Instance);
        }

        public void Dispose() => _context.Dispose();

        [Fact]
        public async Task CreatePost_ValidRequest_PersistsWithServerSideDefaults()
        {
            var beforeUtc = DateTime.UtcNow.AddSeconds(-1);

            var result = await _service.CreatePost(new CreatePostRequest
            {
                Title = "Koi care",
                Content = "Feed koi twice daily",
                CategoryId = 1,
                ImageIds = new List<int>()
            }, authorAccountId: 42);

            Assert.True(result.Success, result.Message);

            var saved = await _context.Posts.SingleAsync(p => p.Name == "Koi care");
            Assert.Equal("Feed koi twice daily", saved.Description);
            Assert.Equal(1, saved.Id);
            Assert.Equal(42, saved.AccountId);
            Assert.Equal(PostService.MemberPostDefaultStatus, saved.Status);
            Assert.True(saved.CreateAt >= beforeUtc);
            Assert.True(saved.UpdateAt >= beforeUtc);
        }

        [Fact]
        public async Task CreatePost_UnknownCategoryId_FailsAndPersistsNothing()
        {
            var result = await _service.CreatePost(new CreatePostRequest
            {
                Title = "Orphan",
                Content = "No such category",
                CategoryId = 999
            }, authorAccountId: 42);

            Assert.False(result.Success);
            Assert.False(await _context.Posts.AnyAsync(p => p.Name == "Orphan"));
        }

        [Fact]
        public async Task CreatePost_WithExistingImageId_LinksImageToPost()
        {
            var result = await _service.CreatePost(new CreatePostRequest
            {
                Title = "With image",
                Content = "Body",
                CategoryId = 1,
                ImageIds = new List<int> { 5 }
            }, authorAccountId: 42);

            Assert.True(result.Success, result.Message);

            var post = await _context.Posts.SingleAsync(p => p.Name == "With image");
            var link = await _context.PostImages.SingleAsync(pi => pi.PostId == post.PostId);
            Assert.Equal(5, link.ImageId);
        }

        [Fact]
        public async Task CreatePost_WithUnknownImageId_FailsAndPersistsNothing()
        {
            var result = await _service.CreatePost(new CreatePostRequest
            {
                Title = "Bad image",
                Content = "Body",
                CategoryId = 1,
                ImageIds = new List<int> { 777 }
            }, authorAccountId: 42);

            Assert.False(result.Success);
            Assert.False(await _context.Posts.AnyAsync(p => p.Name == "Bad image"));
        }
    }
}
