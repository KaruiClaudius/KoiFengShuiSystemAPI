using KoiFengShuiSystem.BusinessLogic.Services.Implement;
using KoiFengShuiSystem.Common;
using KoiFengShuiSystem.DataAccess.Models;
using KoiFengShuiSystem.DataAccess.Repositories.Implement;
using KoiFengShuiSystem.Modules.FengShui.Domain.Entities;
using KoiFengShuiSystem.Shared.Infrastructure.Persistence;
using KoiFengShuiSystem.Shared.Models.Request;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace UnitTests.Admin
{
    /// <summary>
    /// Member-created posts legitimately have no ElementId; the public feed
    /// must tolerate them instead of throwing on the nullable cast.
    /// </summary>
    public class PostServiceFeedNullElementTests : IDisposable
    {
        private readonly KoiFengShuiContext _context;
        private readonly PostService _service;

        public PostServiceFeedNullElementTests()
        {
            var options = new DbContextOptionsBuilder<KoiFengShuiContext>()
                .UseInMemoryDatabase(databaseName: $"PostFeedNullElement_{Guid.NewGuid()}")
                .Options;
            _context = new KoiFengShuiContext(options);

            _context.PostCategories.Add(new PostCategory { Id = 1, PostType = "Blog" });
            _context.Elements.Add(new Element { ElementId = 1, ElementName = "Metal" });
            _context.Posts.AddRange(
                new Post
                {
                    PostId = 10,
                    Id = 1,
                    Name = "Member post",
                    Description = "No element assigned",
                    Status = "Pending",
                    AccountId = 42,
                    ElementId = null,
                    CreateAt = DateTime.UtcNow,
                    UpdateAt = DateTime.UtcNow
                },
                new Post
                {
                    PostId = 11,
                    Id = 1,
                    Name = "Curated post",
                    Description = "Has an element",
                    Status = "Published",
                    AccountId = 1,
                    ElementId = 1,
                    CreateAt = DateTime.UtcNow,
                    UpdateAt = DateTime.UtcNow
                });
            _context.SaveChanges();

            _service = new PostService(
                new UnitOfWorkRepository(_context),
                _context,
                NullLogger<PostService>.Instance);
        }

        public void Dispose() => _context.Dispose();

        [Fact]
        public async Task GetAll_WithNullAndSetElementIds_ReturnsAllPostsUncategorizedForNull()
        {
            var result = await _service.GetAll();

            Assert.True(result.Success, result.Message);
            var responses = Assert.IsType<List<KoiFengShuiSystem.Shared.Models.Response.PostResponse>>(result.Data);

            var memberRow = Assert.Single(responses, r => r.Name == "Member post");
            Assert.Null(memberRow.ElementName);
            Assert.Equal(0, memberRow.ElementId);

            var curatedRow = Assert.Single(responses, r => r.Name == "Curated post");
            Assert.Equal("Metal", curatedRow.ElementName);
            Assert.Equal(1, curatedRow.ElementId);
        }

        [Fact]
        public async Task GetByPostTypeId_WithNullElementPost_ReturnsRowWithoutThrowing()
        {
            var result = await _service.GetPostByPostTypeId(1, 1, 10);

            Assert.True(result.Success, result.Message);
            var responses = Assert.IsType<List<KoiFengShuiSystem.Shared.Models.Response.PostResponse>>(result.Data);
            Assert.Equal(2, responses.Count);
            var memberRow = Assert.Single(responses, r => r.Name == "Member post");
            Assert.Null(memberRow.ElementName);
        }

        [Fact]
        public async Task CreatePost_MemberPath_LeavesElementIdUnset()
        {
            var result = await _service.CreatePost(new CreatePostRequest
            {
                Title = "Fresh member post",
                Content = "Body",
                CategoryId = 1
            }, authorAccountId: 42);

            Assert.True(result.Success, result.Message);
            var saved = await _context.Posts.SingleAsync(p => p.Name == "Fresh member post");
            Assert.Null(saved.ElementId);
        }
    }
}
