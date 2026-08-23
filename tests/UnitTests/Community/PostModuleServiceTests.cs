using KoiFengShuiSystem.Shared.Kernel;
using KoiFengShuiSystem.DataAccess.Models;
using KoiFengShuiSystem.Modules.Community.Application.Abstractions;
using KoiFengShuiSystem.Modules.Community.Application.Requests;
using KoiFengShuiSystem.Modules.Community.Application.Responses;
using KoiFengShuiSystem.Modules.Community.Application.Services;
using KoiFengShuiSystem.Shared.Kernel.Results;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace UnitTests.Community
{
    /// <summary>
    /// Ports the legacy PostServiceCreateTests / PostServiceFeedNullElementTests /
    /// AdminPostServiceSaveBatchingTests orchestration cases onto the module-owned
    /// services with a strict ICommunityStore mock (FAQ pilot style). Persistence
    /// batching itself is pinned against the real EF store in EfCommunityStorePostTests.
    /// </summary>
    public class PostModuleServiceTests
    {
        private readonly Mock<ICommunityStore> _storeMock = new(MockBehavior.Strict);

        private PostService CreatePostService() =>
            new(_storeMock.Object, NullLogger<PostService>.Instance);

        private AdminPostService CreateAdminPostService() => new(_storeMock.Object);

        // ---- Member create: server-owned overrides ----

        [Fact]
        public async Task CreatePost_ValidRequest_PersistsWithServerSideDefaults()
        {
            var beforeUtc = DateTime.UtcNow.AddSeconds(-1);
            SetupCategoryExists(1);
            SetupEmptyImageLookup();
            Post? captured = null;
            _storeMock
                .Setup(s => s.AddPostAsync(It.IsAny<Post>()))
                .Callback<Post>(p => captured = p)
                .Returns(Task.CompletedTask);

            var service = CreatePostService();

            var result = await service.CreatePost(new CreatePostRequest
            {
                Title = "Koi care",
                Content = "Feed koi twice daily",
                CategoryId = 1,
                ImageIds = new List<int>()
            }, authorAccountId: 42);

            Assert.True(result.Success, result.Message);
            Assert.NotNull(captured);
            Assert.Equal("Feed koi twice daily", captured!.Description);
            Assert.Equal("Koi care", captured.Name);
            Assert.Equal(1, captured.PostCategoryId);
            Assert.Equal(42, captured.AccountId);
            Assert.Equal(PostService.MemberPostDefaultStatus, captured.Status);
            Assert.True(captured.CreateAt >= beforeUtc);
            Assert.True(captured.UpdateAt >= beforeUtc);
        }

        [Fact]
        public async Task CreatePost_MemberPath_LeavesElementIdUnset()
        {
            SetupCategoryExists(1);
            _storeMock
                .Setup(s => s.AddPostAsync(It.IsAny<Post>()))
                .Returns(Task.CompletedTask);

            var service = CreatePostService();

            var result = await service.CreatePost(new CreatePostRequest
            {
                Title = "Fresh member post",
                Content = "Body",
                CategoryId = 1
            }, authorAccountId: 42);

            Assert.True(result.Success, result.Message);
            _storeMock.Verify(s => s.AddPostAsync(It.Is<Post>(p => p.ElementId == null)), Times.Once);
        }

        [Fact]
        public async Task CreatePost_UnknownCategoryId_FailsAndPersistsNothing()
        {
            SetupCategoryExists(999, exists: false);

            var service = CreatePostService();

            var result = await service.CreatePost(new CreatePostRequest
            {
                Title = "Orphan",
                Content = "No such category",
                CategoryId = 999
            }, authorAccountId: 42);

            Assert.False(result.Success);
            _storeMock.Verify(s => s.AddPostAsync(It.IsAny<Post>()), Times.Never);
        }

        [Fact]
        public async Task CreatePost_WithExistingImageId_LinksImageToPost()
        {
            SetupCategoryExists(1);
            _storeMock
                .Setup(s => s.GetImagesByIdsAsync(It.IsAny<IReadOnlyCollection<int>>()))
                .ReturnsAsync(new List<Image> { new() { ImageId = 5, ImageUrl = "https://cdn.example/i5.png" } });
            Post? captured = null;
            _storeMock
                .Setup(s => s.AddPostAsync(It.IsAny<Post>()))
                .Callback<Post>(p => captured = p)
                .Returns(Task.CompletedTask);

            var service = CreatePostService();

            var result = await service.CreatePost(new CreatePostRequest
            {
                Title = "With image",
                Content = "Body",
                CategoryId = 1,
                ImageIds = new List<int> { 5 }
            }, authorAccountId: 42);

            Assert.True(result.Success, result.Message);
            var link = Assert.Single(captured!.PostImages);
            Assert.Equal(5, link.ImageId);
            Assert.Equal("Member upload", link.ImageDescription);
        }

        [Fact]
        public async Task CreatePost_WithUnknownImageId_FailsAndPersistsNothing()
        {
            SetupCategoryExists(1);
            _storeMock
                .Setup(s => s.GetImagesByIdsAsync(It.IsAny<IReadOnlyCollection<int>>()))
                .ReturnsAsync(new List<Image>());

            var service = CreatePostService();

            var result = await service.CreatePost(new CreatePostRequest
            {
                Title = "Bad image",
                Content = "Body",
                CategoryId = 1,
                ImageIds = new List<int> { 777 }
            }, authorAccountId: 42);

            Assert.False(result.Success);
            _storeMock.Verify(s => s.AddPostAsync(It.IsAny<Post>()), Times.Never);
        }

        [Fact]
        public async Task CreatePost_DuplicateImageIds_LinksEachDistinctImageOnce()
        {
            SetupCategoryExists(1);
            var requestedIds = new List<int>();
            _storeMock
                .Setup(s => s.GetImagesByIdsAsync(It.IsAny<IReadOnlyCollection<int>>()))
                .Callback<IReadOnlyCollection<int>>(ids => requestedIds.AddRange(ids))
                .ReturnsAsync((IReadOnlyCollection<int> ids) => ids
                    .Select(id => new Image { ImageId = id, ImageUrl = $"https://cdn.example/{id}.png" })
                    .ToList());
            Post? captured = null;
            _storeMock
                .Setup(s => s.AddPostAsync(It.IsAny<Post>()))
                .Callback<Post>(p => captured = p)
                .Returns(Task.CompletedTask);

            var service = CreatePostService();

            await service.CreatePost(new CreatePostRequest
            {
                Title = "Dedup",
                Content = "Body",
                CategoryId = 1,
                ImageIds = new List<int> { 5, 5, 6 }
            }, authorAccountId: 42);

            Assert.Equal(new[] { 5, 6 }, requestedIds);
            Assert.Equal(2, captured!.PostImages.Count);
        }

        // ---- Public feed: null-element sentinel + element-name join ----

        /// <summary>
        /// Member-created posts legitimately have no ElementId; the public feed
        /// must tolerate them instead of throwing on the nullable cast.
        /// </summary>
        [Fact]
        public async Task GetAll_WithNullAndSetElementIds_ReturnsAllPostsUncategorizedForNull()
        {
            _storeMock
                .Setup(s => s.GetAllPostsAsync())
                .ReturnsAsync(new List<Post>
                {
                    CreateEntity(10, name: "Member post", elementId: null),
                    CreateEntity(11, name: "Curated post", elementId: 1)
                });
            _storeMock
                .Setup(s => s.GetElementNamesAsync())
                .ReturnsAsync(new Dictionary<int, string> { [1] = "Metal" });

            var service = CreatePostService();

            var result = await service.GetAll();

            Assert.True(result.Success, result.Message);
            var responses = Assert.IsType<List<PostResponse>>(result.Data);

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
            _storeMock
                .Setup(s => s.GetPostsByPostTypeAsync(1, 1, 10))
                .ReturnsAsync(new List<Post> { CreateEntity(10, name: "Member post", elementId: null) });
            _storeMock
                .Setup(s => s.GetElementNamesAsync())
                .ReturnsAsync(new Dictionary<int, string>());

            var service = CreatePostService();

            var result = await service.GetPostByPostTypeId(1, 1, 10);

            Assert.True(result.Success, result.Message);
            var responses = Assert.IsType<List<PostResponse>>(result.Data);
            var memberRow = Assert.Single(responses);
            Assert.Null(memberRow.ElementName);
            Assert.Equal(0, memberRow.ElementId);
        }

        [Fact]
        public void GetAll_MapsLegacyFeedContractFields()
        {
            // Compile-time pin of the response contract consumed by clients.
            var response = new PostResponse
            {
                PostId = 3,
                Id = 4,
                Name = "n",
                Description = "d",
                AccountId = 5,
                Status = "Published",
                Follows = new List<Follow>(),
                AccountName = "N/A"
            };

            Assert.Equal(3, response.PostId);
            Assert.Equal(4, response.Id);
        }

        // ---- Details passthrough ----

        [Fact]
        public async Task GetPostById_ExistingId_ReturnsPostResponseInEnvelope()
        {
            // Council D11: public detail maps through the same PostResponse shape
            // as the feed endpoints (raw-entity serialization retired).
            var stored = CreateEntity(7);
            _storeMock
                .Setup(s => s.GetPostByIdAsync(7))
                .ReturnsAsync(stored);
            _storeMock
                .Setup(s => s.GetElementNamesAsync())
                .ReturnsAsync(new Dictionary<int, string> { [1] = "Metal" });

            var service = CreatePostService();

            var result = await service.GetPostById(7);

            Assert.True(result.Success, result.Message);
            var response = Assert.IsType<PostResponse>(result.Data);
            Assert.Equal(7, response.PostId);
            Assert.Equal("Metal", response.ElementName);
            Assert.Empty(response.ImageUrls);
        }

        [Fact]
        public async Task GetPostById_NonExistentId_ReturnsWarningEnvelope()
        {
            _storeMock
                .Setup(s => s.GetPostByIdAsync(999))
                .ReturnsAsync((Post?)null);

            var service = CreatePostService();

            var result = await service.GetPostById(999);

            Assert.False(result.Success);
            Assert.Equal(ResponseCodes.WarningNoDataCode, result.Status);
            Assert.Null(result.Data);
        }

        // ---- My posts (council Q11) ----

        [Fact]
        public async Task GetMyPosts_MapsCallerQueueThroughPostResponse()
        {
            var owned = CreateEntity(70, name: "Mine pending", elementId: null);
            owned.Status = "Pending";
            _storeMock
                .Setup(s => s.GetPostsByAccountIdAsync(42, 1, 50))
                .ReturnsAsync(new List<Post> { owned });
            _storeMock
                .Setup(s => s.GetElementNamesAsync())
                .ReturnsAsync(new Dictionary<int, string> { [1] = "Metal" });

            var service = CreatePostService();

            var result = await service.GetMyPosts(42, page: 1, pageSize: 50);

            Assert.True(result.Success, result.Message);
            var responses = Assert.IsType<List<PostResponse>>(result.Data);
            var row = Assert.Single(responses);
            Assert.Equal(70, row.PostId);
            Assert.Equal("Pending", row.Status);
            Assert.Empty(row.ImageUrls);
        }

        [Fact]
        public async Task GetMyPosts_MissingClaimId_ReturnsEmptySuccessEnvelope()
        {
            // accountId 0 (missing claim path) matches nothing -> empty list, not an error.
            _storeMock
                .Setup(s => s.GetPostsByAccountIdAsync(0, 1, 50))
                .ReturnsAsync(new List<Post>());
            _storeMock
                .Setup(s => s.GetElementNamesAsync())
                .ReturnsAsync(new Dictionary<int, string>());

            var service = CreatePostService();

            var result = await service.GetMyPosts(0, page: 1, pageSize: 50);

            Assert.True(result.Success, result.Message);
            var responses = Assert.IsType<List<PostResponse>>(result.Data);
            Assert.Empty(responses);
        }

        // ---- Delete ----

        [Fact]
        public async Task DeletePost_NonExistentId_ReturnsNoDataWarning()
        {
            _storeMock
                .Setup(s => s.DeletePostAsync(999))
                .ReturnsAsync(false);

            var service = CreatePostService();

            var result = await service.DeletePost(999);

            Assert.False(result.Success);
            Assert.Null(result.Data);
        }

        [Fact]
        public async Task DeletePost_ExistingId_ReturnsSuccessEnvelopeWithoutData()
        {
            _storeMock
                .Setup(s => s.DeletePostAsync(7))
                .ReturnsAsync(true);

            var service = CreatePostService();

            var result = await service.DeletePost(7);

            Assert.True(result.Success, result.Message);
            Assert.Null(result.Data); // controller relies on this to answer Ok(envelope)
        }

        // ---- Admin reads ----

        [Fact]
        public async Task GetAllAdminPostsAsync_MapsImagesToUrlsAndLegacyPlaceholders()
        {
            _storeMock
                .Setup(s => s.GetAllAdminPostsWithImagesAsync())
                .ReturnsAsync(new List<Post> { CreateAdminEntity(9, ("https://cdn.example/a.png", "img-a")) });

            var service = CreateAdminPostService();

            var result = await service.GetAllAdminPostsAsync();

            var row = Assert.Single(result);
            Assert.Equal(9, row.PostId);
            Assert.Equal("N/A", row.AccountName);
            Assert.Equal(new[] { "https://cdn.example/a.png" }, row.ImageUrls);
        }

        [Fact]
        public async Task GetAdminPostByIdAsync_NonExistentId_ReturnsNull()
        {
            _storeMock
                .Setup(s => s.GetAdminPostByIdWithImagesAsync(404))
                .ReturnsAsync((Post?)null);

            var service = CreateAdminPostService();

            var result = await service.GetAdminPostByIdAsync(404);

            Assert.Null(result);
        }

        // ---- Admin update orchestration ----

        [Fact]
        public async Task UpdateAdminPostAsync_NonExistentId_ReturnsNullAndSendsNoCommand()
        {
            _storeMock
                .Setup(s => s.UpdateAdminPostAsync(It.IsAny<AdminPostUpdate>()))
                .ReturnsAsync((Post?)null);

            var service = CreateAdminPostService();

            var result = await service.UpdateAdminPostAsync(404, ValidAdminRequest(), new List<string> { "u" });

            Assert.Null(result);
            _storeMock.Verify(
                s => s.UpdateAdminPostAsync(It.Is<AdminPostUpdate>(u => u.PostId == 404)),
                Times.Once);
        }

        [Fact]
        public async Task UpdateAdminPostAsync_ForwardsRequestFieldsAsDesiredState()
        {
            AdminPostUpdate? command = null;
            _storeMock
                .Setup(s => s.UpdateAdminPostAsync(It.IsAny<AdminPostUpdate>()))
                .Callback<AdminPostUpdate>(u => command = u)
                .ReturnsAsync(CreateAdminEntity(7));

            var service = CreateAdminPostService();

            var result = await service.UpdateAdminPostAsync(7, new AdminPostRequest
            {
                Id = 1,
                Name = "Updated",
                Description = "Updated body",
                Status = "Published"
            }, new List<string> { "new-a", "new-b" });

            Assert.NotNull(result);
            Assert.NotNull(command);
            Assert.Equal(7, command!.PostId);
            Assert.Equal("Updated", command.Name);
            Assert.Equal("Updated body", command.Description);
            Assert.Equal("Published", command.Status);
            Assert.Equal(new[] { "new-a", "new-b" }, command.ImageUrls);
        }

        // ---- Admin create ----

        [Fact]
        public async Task CreatePostWithImagesAsync_UnknownCategory_ThrowsArgumentException()
        {
            // Preserved quirk: invalid category throws (controller catch-all turns it into 500).
            SetupCategoryExists(999, exists: false);

            var service = CreateAdminPostService();

            await Assert.ThrowsAsync<ArgumentException>(() => service.CreatePostWithImagesAsync(new AdminPostRequest
            {
                Id = 999,
                Name = "Orphan",
                Status = "Published"
            }, new List<string>()));

            _storeMock.Verify(s => s.AddAdminPostWithImagesAsync(It.IsAny<Post>()), Times.Never);
        }

        [Fact]
        public async Task CreatePostWithImagesAsync_ZeroImages_PersistsThePost()
        {
            SetupCategoryExists(1);
            Post? captured = null;
            _storeMock
                .Setup(s => s.AddAdminPostWithImagesAsync(It.IsAny<Post>()))
                .Callback<Post>(p => captured = p)
                .Returns(Task.CompletedTask);
            _storeMock
                .Setup(s => s.GetAdminPostByIdWithImagesAsync(It.IsAny<int>()))
                .ReturnsAsync((int id) => { captured!.PostId = id; return captured!; });

            var service = CreateAdminPostService();

            var response = await service.CreatePostWithImagesAsync(new AdminPostRequest
            {
                Id = 1,
                Name = "No images",
                Description = "Body",
                AccountId = 1,
                Status = "Published"
            }, new List<string>());

            Assert.NotNull(captured);
            Assert.Empty(captured!.PostImages); // zero-image creation must not be blocked by image links
            _storeMock.Verify(s => s.AddAdminPostWithImagesAsync(captured), Times.Once);
            Assert.NotNull(response);
            Assert.Equal(captured.PostId, response!.PostId);
            Assert.Equal("Published", response.Status);
        }

        [Fact]
        public async Task CreatePostWithImagesAsync_WithImages_AttachesEveryImageLink()
        {
            SetupCategoryExists(1);
            Post? captured = null;
            _storeMock
                .Setup(s => s.AddAdminPostWithImagesAsync(It.IsAny<Post>()))
                .Callback<Post>(p => captured = p)
                .Returns(Task.CompletedTask);
            _storeMock
                .Setup(s => s.GetAdminPostByIdWithImagesAsync(It.IsAny<int>()))
                .ReturnsAsync((int id) => { captured!.PostId = id; return captured!; });

            var service = CreateAdminPostService();

            await service.CreatePostWithImagesAsync(new AdminPostRequest
            {
                Id = 1,
                Name = "New post",
                Description = "Body",
                AccountId = 1,
                Status = "Published",
                ElementId = 2
            }, new List<string> { "img-a", "img-b" });

            Assert.NotNull(captured);
            Assert.Equal(2, captured!.PostImages.Count);
            Assert.Same(captured, captured.PostImages.First().Post);
            Assert.All(captured.PostImages, pi => Assert.Equal("Default description", pi.ImageDescription));
            Assert.Equal(2, captured.ElementId);
        }

        // ---- Admin delete ----

        [Fact]
        public async Task DeletePostWithAllRelatedAsync_DelegatesToStore()
        {
            _storeMock
                .Setup(s => s.DeletePostWithAllRelatedAsync(7))
                .ReturnsAsync(true);

            var service = CreateAdminPostService();

            var result = await service.DeletePostWithAllRelatedAsync(7);

            Assert.True(result);
            _storeMock.Verify(s => s.DeletePostWithAllRelatedAsync(7), Times.Once);
        }

        // ---- Helpers ----

        private void SetupCategoryExists(int categoryId, bool exists = true)
        {
            _storeMock
                .Setup(s => s.PostCategoryExistsAsync(categoryId))
                .ReturnsAsync(exists);
        }

        private void SetupEmptyImageLookup()
        {
            _storeMock
                .Setup(s => s.GetImagesByIdsAsync(It.IsAny<IReadOnlyCollection<int>>()))
                .ReturnsAsync(new List<Image>());
        }

        private static AdminPostRequest ValidAdminRequest() => new()
        {
            Id = 1,
            Name = "Existing",
            Description = "Existing post",
            Status = "Published"
        };

        private static Post CreateEntity(int id, string? name = null, int? elementId = 1) => new()
        {
            PostId = id,
            PostCategoryId = 1,
            Name = name ?? $"Post {id}",
            Description = $"Description {id}",
            Status = "Pending",
            AccountId = 42,
            ElementId = elementId,
            CreateAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            UpdateAt = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc)
        };

        private static Post CreateAdminEntity(int id, params (string url, string description)[] images)
        {
            var post = CreateEntity(id, name: $"Admin post {id}", elementId: null);
            post.Status = "Published";
            post.AccountId = 1;
            foreach (var (url, description) in images)
            {
                post.PostImages.Add(new PostImage
                {
                    PostImageId = post.PostImages.Count + 1,
                    PostId = post.PostId,
                    Image = new Image { ImageId = post.PostImages.Count + 10, ImageUrl = url },
                    ImageDescription = description
                });
            }
            return post;
        }
    }
}
